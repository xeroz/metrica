Here is the updated guide with the SOLID principles and architectural guidelines seamlessly integrated into your existing document:

---

# Guide for AI Agents

## Technology Stack

* **Backend:** C# and .NET 10 with ASP.NET Core for APIs and background workers.
* **Persistence:** PostgreSQL 17 with Entity Framework Core 10 and Npgsql for data access and migrations.
* **Messaging:** RabbitMQ 4 with RabbitMQ.Client for the `carga_masiva` and `notificaciones` queues.
* **File storage:** SeaweedFS for uploaded Excel workbooks.
* **Email:** MailKit for delivery and Mailpit for local SMTP testing.
* **API validation and documentation:** FluentValidation, OpenAPI, and Scalar.
* **Local deployment:** Docker and Docker Compose.

## Architectural Principles

6. **Technical & Modern C# Best Practices:**
* Utilize modern C# features effectively (e.g., primary constructors, records, pattern matching, and strict nullable reference types).
* Maintain clean layer boundaries across projects (e.g., Domain, Application, Infrastructure, and API/UI layers).
* Ensure all code remains highly testable through explicit dependency injection and clear mocking boundaries.



## Knowledge Graph

* **File:** `graphify-out/graph.json` (generated and ignored by Git).
* **Instruction:** Consult this graph to trace code structure, dependencies, and relationships between modules when it is available.
* **Update:** Run `graphify update .` after significant structural changes. This updates the code graph without an LLM API key.