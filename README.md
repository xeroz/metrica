# Metrica — Mass Upload Challenge

This repository contains the .NET 10 foundation for the [senior backend challenge](https://github.com/desarrollo-acity/reto_tecnico_backend_senior/blob/main/README.md).

## Technology stack

| Area | Technology | Use in this project |
| --- | --- | --- |
| Backend | C#, .NET 10, ASP.NET Core | HTTP APIs and background workers |
| Data | PostgreSQL 17, Entity Framework Core 10, Npgsql | Persistence and database migrations |
| Messaging | RabbitMQ 4, RabbitMQ.Client | Upload and completion queues |
| File storage | SeaweedFS | Uploaded Excel workbooks |
| Email | MailKit, Mailpit (local) | Completion notifications and local SMTP testing |
| API validation and documentation | FluentValidation, OpenAPI, Scalar | Upload validation and the public gateway API reference |
| Deployment | Docker, Docker Compose | Local service and infrastructure containers |

## Current scope

The solution contains five independent ASP.NET Core processes:

| Project | Intended responsibility |
| --- | --- |
| `Gateway` | Public API entry point and routing |
| `Auth.Api` | Authentication and JWT issuance |
| `Control.Api` | Uploads, load history, and status queries |
| `Processing.Worker` | Excel processing and persistence |
| `Notifications.Worker` | Completion emails and final state update |

`Auth.Api` stores users and issues JWTs. `Gateway` validates JWTs and routes client requests to Auth or Control. `Control.Api` accepts Excel uploads and exposes load status. `Processing.Worker` consumes `carga_masiva`, loads the workbook from SeaweedFS, writes products and failures to PostgreSQL, and publishes completion messages to `notificaciones`. `Notifications.Worker` sends the result by email and marks the load as `Notified`. Auth and Control apply their EF Core migrations on startup.

`src/Shared/LoadStatus.cs` defines the load states compiled into Control.Api and both workers. Their numeric values are fixed for persistence and messaging:

| Value | State |
| --- | --- |
| 1 | `Pending` |
| 2 | `InProgress` |
| 3 | `Loaded` |
| 4 | `Completed` |
| 5 | `Notified` |

Processing outcomes such as success or rejection are stored separately from these states.

## Requirements

- .NET 10 SDK
- Docker with Docker Compose, for container execution

## Build and run

```bash
dotnet build Metrica.slnx
docker compose up --build
```

To stop and remove the containers created by Compose:

```bash
docker compose down
```

The gateway is available at `http://localhost:8080`. PostgreSQL is exposed on port `5432`, RabbitMQ on `5672`, its management UI on `15672`, and the SeaweedFS filer on `8888`. Auth and Control are reached through the gateway, not through published host ports.

The local SMTP server is Mailpit. Open `http://localhost:8025` to inspect sent emails. To use another SMTP server, set `SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURITY` (`None`, `StartTls`, or `SslOnConnect`), `SMTP_USER`, `SMTP_PASSWORD`, and `SMTP_FROM` in `.env`. The defaults route messages to Mailpit and do not deliver to external inboxes.

For local development, copy `.env.example` to `.env` and edit the credentials before starting Compose. The defaults in `compose.yaml` are for local development only.

The default database is `metrica_app`. Existing data in the earlier `metrica` database is left untouched. On a fresh Docker volume, PostgreSQL creates `metrica_app`; on an existing volume, create that database once before starting Control.

To run the gateway without Docker, supply the same signing key used by Auth and Control, and run Auth and Control at the addresses configured in `src/Gateway/appsettings.json`:

```bash
Jwt__SigningKey='metrica-local-only-signing-key-at-least-32-bytes' \
  dotnet run --project src/Gateway --no-launch-profile --urls http://localhost:5000
```

## Next implementation steps

1. Add authenticated ownership to periods and products if the API needs user-specific catalog views.

## Authentication and gateway

`Auth.Api` creates a `users` table with `id`, unique `email`, and `password`. The `password` column contains an ASP.NET Core password hash, never the original password. Users are created explicitly through `POST /auth/register`; startup only applies the database migration. Set `JWT_SIGNING_KEY` in `.env` for your environment. A user created by the earlier bootstrap implementation remains in an existing database until removed separately.

Register a user through the gateway:

```bash
curl -X POST http://localhost:8080/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"user@example.com","password":"StrongPass123!"}'
```

Registration returns HTTP 201 with `id` and `email`, HTTP 409 for an existing email, or HTTP 400 for an invalid email or a password outside 8–128 characters.

Request a one-hour access token through the gateway:

```bash
curl -X POST http://localhost:8080/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"user@example.com","password":"StrongPass123!"}'
```

The response contains `accessToken`, `tokenType`, and `expiresIn`. Send `Authorization: Bearer <accessToken>` on `POST /cargas` and `GET /cargas/{uniqueIdentifier}`. The gateway validates the JWT signature, issuer, audience, and expiry before forwarding. `GET /health`, `POST /auth/register`, and `POST /auth/login` are public. To check this against a running Compose stack, run `python3 tests/auth_gateway_checks.py`; it creates or reuses `auth-check@metrica.local`.

In local Compose, open `http://localhost:8080/scalar/` to browse both API documents. Both services are accessed through the gateway:

| Service | API base URL | OpenAPI document |
| --- | --- | --- |
| `Auth.Api` | `http://localhost:8080/auth` | `http://localhost:8080/auth/openapi/v1.json` |
| `Control.Api` | `http://localhost:8080/cargas` | `http://localhost:8080/control/openapi/v1.json` |

Registration and login appear in the Autenticación document.
For uploads in Scalar, select **Cargas**, choose **Bearer** under **Authentication**, and paste the login response's `accessToken` into **Bearer Token** (without the `Bearer ` prefix). Use `http://localhost:8080` as the server.

## Control upload API

`POST /cargas` accepts a `multipart/form-data` field named `field`. The notification email and owner ID come from the `email` and `sub` claims of the authenticated JWT; a token missing either claim returns HTTP 401. The file must be a non-empty `.xlsx` of at most 5 MiB (5,242,880 bytes). The API checks the workbook container, stores it in SeaweedFS, and inserts a `StatusFile` row with state `Pending`. The upload and an outbox message are committed together; the background publisher retries delivery to RabbitMQ's durable `carga_masiva` queue.

On acceptance, the response is HTTP 200:

```json
{
  "message": "Processing file",
  "status": "Pending",
  "uniqueIdentifier": "8e4cf489-82d1-4c71-bb29-89cc15ef5674"
}
```

`GET /cargas/{uniqueIdentifier}` returns the current state and original file name only to the user who uploaded it. Unknown identifiers, other users' loads, and loads created before ownership was recorded return HTTP 404. Both upload and status requests require a valid JWT with `sub` and `email` claims. Tokens remain valid until they expire, even if the user is later removed.

The Control code is split into HTTP endpoints, application logic, and infrastructure. The EF Core context, models, and migrations live in `src/Control.Api/Infrastructure/Persistence`; `Program.cs` only wires dependencies and routes. Run the focused upload checks with:

```bash
dotnet run --project tests/Control.Api.Checks
```

## Processing worker

The worker reads the four columns in the first worksheet of an `.xlsx` file: `Periodo` (`AAAA-MM`), `CodigoProducto`, `NombreProducto`, and `Precio`. It trims values, normalizes product codes to uppercase, uses `Sin nombre` for an empty name and `0` for an empty price, and ignores blank rows. Distinct periods are stored in `period` and distinct codes in `code`; later loads reuse either catalog entry. Each valid combination is stored in `product` with foreign keys to both catalogs. A code may occur in multiple periods or products. Only rows identical in all four normalized fields are skipped and recorded in `processing_failure`, along with invalid rows. This row-level rule differs from the challenge's explicit duplicate-code rule.

For an accepted load, the `StatusFile` state moves through `InProgress`, `Loaded`, and `Completed`. Invalid workbooks move from `InProgress` to `Completed` with a `Rechazado` result. The outcome and counts are stored in `processing_result`, separately from the state. The worker uses EF Core models and applies its own migration after the Control migration is available; it maps Control's `StatusFile` without recreating it. It retries transient failures and uses a PostgreSQL notification outbox so a RabbitMQ outage does not lose completion messages. The notification consumer changes the state to `Notified` only after SMTP accepts the email.

The durable `notificaciones` queue receives JSON with `idCarga`, `fechaFin`, `resultado`, `insertados`, and `fallidos`. The consumer retrieves the recipient from `StatusFile`, sends a plain-text email with MailKit, and acknowledges the message after updating the state. Delivery is at least once: a crash after SMTP accepts a message but before the database update can send a duplicate. Loads created before the email migration have no recipient and cannot be emailed automatically.

Run the focused workbook checks with:

```bash
dotnet run --project tests/Processing.Worker.Checks
```
