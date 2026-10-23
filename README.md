# Metrica

> 📹 **DEMO VIDEO:** The video: https://drive.google.com/file/d/16FdTCiqOkA7MXu8-yfeFm6ug0zsqDMNZ/view?usp=sharing

Mass upload service for Excel workbooks. The gateway exposes authentication and upload APIs; background workers process workbooks and send completion emails.

## Technology stack

| Area | Technology |
| --- | --- |
| APIs and workers | C#, .NET 10, ASP.NET Core |
| Database | PostgreSQL 17, Entity Framework Core 10, Npgsql |
| Messaging | RabbitMQ 4, RabbitMQ.Client |
| File storage | SeaweedFS |
| Email | MailKit, Mailpit for local testing |
| Validation and API docs | FluentValidation, OpenAPI, Scalar |
| Local deployment | Docker, Docker Compose |

## Run locally

Install Docker with the Compose plugin. From the repository root:

```bash
cp .env.example .env
```

Edit `.env` to set your local database and RabbitMQ credentials and a `JWT_SIGNING_KEY` of at least 32 UTF-8 bytes. The example values are for local development only. Then start the infrastructure and wait for PostgreSQL:

```bash
docker compose up -d postgres rabbitmq seaweedfs mailpit
until docker compose exec -T postgres sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"'; do sleep 2; done
docker compose up --build -d
```

Auth and Control apply EF Core migrations on startup; the processing worker applies its migrations when it starts. Check the gateway and inspect service logs with:

```bash
curl -f http://localhost:8080/health
docker compose logs -f
```

Local URLs:

| Service | URL |
| --- | --- |
| Gateway API | `http://localhost:8080` |
| API documentation | `http://localhost:8080/scalar/` |
| Mailpit inbox | `http://localhost:8025` |
| RabbitMQ management | `http://localhost:15672` |

To use the API endpoints in Postman, choose **Import → File** and select [`metrica.postman_collection.json`](metrica.postman_collection.json).

## Project structure

```text
.
├── src/
│   ├── Gateway/                # Public API, JWT validation, and routing
│   ├── Auth.Api/               # Registration, login, and token issuance
│   ├── Control.Api/            # Workbook uploads and load status
│   ├── Processing.Worker/      # Excel processing and database writes
│   ├── Notifications.Worker/   # Completion emails and status updates
│   └── Shared/                 # Shared load status definition
├── compose.yaml                # Application and infrastructure services
├── Dockerfile                  # Build image for each .NET project
├── .env.example                # Local configuration template
└── Metrica.slnx                # .NET solution
```

Within Auth, Control, and the worker projects, `Application/` contains use cases and contracts, `Infrastructure/` contains database and external service adapters, and `Endpoints/` contains HTTP routes where applicable.
