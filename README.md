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
| API validation and documentation | FluentValidation, OpenAPI, Scalar | Upload validation and the Control API reference |
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

`Control.Api` accepts Excel uploads and exposes load status. `Processing.Worker` consumes `carga_masiva`, loads the workbook from SeaweedFS, writes products and failures to PostgreSQL, and publishes completion messages to `notificaciones`. `Notifications.Worker` sends the result by email and marks the load as `Notified`. Gateway routing and authentication remain for later phases. Control applies EF Core migrations on startup to create its `StatusFile` and outbox tables.

`Metrica.Contracts` defines the shared load states. Their numeric values are fixed for persistence and messaging:

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

The gateway health endpoint is available at `http://localhost:8080/health`. PostgreSQL is exposed on port `5432`, RabbitMQ on `5672`, its management UI on `15672`, and the SeaweedFS filer on `8888`.

The local SMTP server is Mailpit. Open `http://localhost:8025` to inspect sent emails. To use another SMTP server, set `SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURITY` (`None`, `StartTls`, or `SslOnConnect`), `SMTP_USER`, `SMTP_PASSWORD`, and `SMTP_FROM` in `.env`. The defaults route messages to Mailpit and do not deliver to external inboxes.

For local development, copy `.env.example` to `.env` and edit the credentials before starting Compose. The defaults in `compose.yaml` are for local development only.

The default database is `metrica_app`. Existing data in the earlier `metrica` database is left untouched. On a fresh Docker volume, PostgreSQL creates `metrica_app`; on an existing volume, create that database once before starting Control.

To run a single service without Docker:

```bash
dotnet run --project src/Gateway --no-launch-profile --urls http://localhost:5000
```

## Next implementation steps

1. Add authentication and gateway routing.
2. Agree on period reservation rules: the sample workbook contains multiple periods in one file.
3. Connect the authenticated user to each load instead of accepting a caller-provided email.

## Control upload API

`POST /cargas` accepts `multipart/form-data` fields named `field` and `email`. The email is the notification recipient until authentication is implemented. The file must be a non-empty `.xlsx` of at most 5 MiB (5,242,880 bytes). The API checks the workbook container, stores it in SeaweedFS, and inserts a `StatusFile` row with state `Pending`. The upload and an outbox message are committed together; the background publisher retries delivery to RabbitMQ's durable `carga_masiva` queue.

On acceptance, the response is HTTP 200:

```json
{
  "message": "Processing file",
  "status": "Pending",
  "uniqueIdentifier": "8e4cf489-82d1-4c71-bb29-89cc15ef5674"
}
```

`GET /cargas/{uniqueIdentifier}` returns the current state and original file name, or HTTP 404 when the identifier is unknown. These endpoints currently belong to `Control.Api`; Gateway and JWT integration have not been added yet.

With Compose, the Control API is available locally at `http://localhost:8081`. In Development, its OpenAPI document is at `http://localhost:8081/openapi/v1.json` and the Scalar API reference is at `http://localhost:8081/scalar`.

The Control code is split into HTTP endpoints, application logic, and infrastructure. The EF Core context, models, and migrations live in `src/Control.Api/Infrastructure/Persistence`; `Program.cs` only wires dependencies and routes. Run the focused upload checks with:

```bash
dotnet run --project tests/Control.Api.Checks
```

## Processing worker

The worker reads the four columns in the first worksheet of an `.xlsx` file: `Periodo` (`AAAA-MM`), `CodigoProducto`, `NombreProducto`, and `Precio`. It trims values, normalizes product codes to uppercase, uses `Sin nombre` for an empty name and `0` for an empty price, and ignores blank rows. Invalid rows and existing product codes are recorded in `processing_failure`; products are stored in `processed_product`. Product codes are unique across all loads. The reference workbook contains multiple periods, so the worker reserves every distinct period in one load. A later load containing any reserved period is rejected and audited.

For an accepted load, the `StatusFile` state moves through `InProgress`, `Loaded`, and `Completed`. Invalid workbooks and period conflicts move from `InProgress` to `Completed` with a `Rechazado` result. The outcome and counts are stored in `processing_result`, separately from the state. The worker uses EF Core models and applies its own migration after the Control migration is available; it maps Control's `StatusFile` without recreating it. It retries transient failures and uses a PostgreSQL notification outbox so a RabbitMQ outage does not lose completion messages. The notification consumer changes the state to `Notified` only after SMTP accepts the email.

The durable `notificaciones` queue receives JSON with `idCarga`, `fechaFin`, `resultado`, `insertados`, and `fallidos`. The consumer retrieves the recipient from `StatusFile`, sends a plain-text email with MailKit, and acknowledges the message after updating the state. Delivery is at least once: a crash after SMTP accepts a message but before the database update can send a duplicate. Loads created before the email migration have no recipient and cannot be emailed automatically.

Run the focused workbook checks with:

```bash
dotnet run --project tests/Processing.Worker.Checks
```
