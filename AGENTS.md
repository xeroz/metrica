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

## Architectural Principles (SOLID)

When designing, refactoring, or writing code across this multi-project repository, you must strictly adhere to the SOLID principles and clean architecture best practices:

1. **S - Single Responsibility Principle (SRP):**
* Every class, service, or component must have one, and only one, reason to change.
* Strictly separate concerns: business logic, data access (EF Core 10), validation (FluentValidation), messaging (RabbitMQ), and HTTP request handling (Controllers/Endpoints) must reside in distinct components. Avoid "God Objects".


2. **O - Open/Closed Principle (OCP):**
* Entities, modules, and classes must be open for extension, but closed for modification.
* Use interfaces, abstract classes, and design patterns (such as Strategy or Factory) to allow adding new features by writing new code rather than altering existing, working code.


3. **L - Liskov Substitution Principle (LSP):**
* Subtypes or class implementations must be substitutable for their base types/interfaces without altering system correctness.
* Never throw `NotImplementedException` when implementing interface contracts, and do not alter expected preconditions or postconditions.


4. **I - Interface Segregation Principle (ISP):**
* Prefer multiple small, client-specific interfaces over a single large, monolithic interface.
* No class should be forced to depend on methods, properties, or contracts it does not use.


5. **D - Dependency Inversion Principle (DIP):**
* High-level modules (e.g., Domain or Application layers) must not depend on low-level modules (e.g., EF Core, SeaweedFS, or RabbitMQ implementations); both must depend on abstractions.
* Always code against interfaces. Leverage .NET 10's native Dependency Injection container (`IServiceCollection`) to register services with appropriate lifetimes (`Transient`, `Scoped`, or `Singleton`).


6. **Technical & Modern C# Best Practices:**
* Utilize modern C# features effectively (e.g., primary constructors, records, pattern matching, and strict nullable reference types).
* Maintain clean layer boundaries across projects (e.g., Domain, Application, Infrastructure, and API/UI layers).
* Ensure all code remains highly testable through explicit dependency injection and clear mocking boundaries.



## Knowledge Graph

* **File:** `graphify-out/graph.json` (generated and ignored by Git).
* **Instruction:** Consult this graph to trace code structure, dependencies, and relationships between modules when it is available.
* **Update:** Run `graphify update .` after significant structural changes. This updates the code graph without an LLM API key.