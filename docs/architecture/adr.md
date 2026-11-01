# Architecture Decision Records (ADRs) — Elsasa SIS V3 Backend

**Document Status:** Approved  
**Date:** November 2026  
**Context:** Elsasa SIS V3 Backend Architecture & Project Initialization  

---

## Index of Architecture Decision Records

- [ADR-001: Modular Monolith with Domain Assemblies and Compile-Time Encapsulation](#adr-001-modular-monolith-with-domain-assemblies-and-compile-time-encapsulation)
- [ADR-002: Pragmatic SQL Server Schema Isolation with Logical UUID Keys and Selective Read JOINs](#adr-002-pragmatic-sql-server-schema-isolation-with-logical-uuid-keys-and-selective-read-joins)
- [ADR-003: Hybrid Cross-Domain Communication (Strongly-Typed Contracts + In-Memory Domain Events)](#adr-003-hybrid-cross-domain-communication-strongly-typed-contracts--in-memory-domain-events)
- [ADR-004: Explicit Data Access with Dapper, C# Raw String Literals, and ISqlConnectionFactory](#adr-004-explicit-data-access-with-dapper-c-raw-string-literals-and-isqlconnectionfactory)
- [ADR-005: Native Minimal APIs with Direct Slice Handlers and FluentValidation Endpoint Filters](#adr-005-native-minimal-apis-with-direct-slice-handlers-and-fluentvalidation-endpoint-filters)
- [ADR-006: Database Schema Migrations via DbUp with Deterministic T-SQL Scripts](#adr-006-database-schema-migrations-via-dbup-with-deterministic-t-sql-scripts)
- [ADR-007: Domain Outcome Modeling via Result<T> and RFC 7807 Problem Details](#adr-007-domain-outcome-modeling-via-resultt-and-rfc-7807-problem-details)
- [ADR-008: Real SQL Server Integration Testing via Testcontainers, Respawn, and WebApplicationFactory](#adr-008-real-sql-server-integration-testing-via-testcontainers-respawn-and-webapplicationfactory)
- [ADR-009: JWT Bearer Authentication, Scoped ICurrentUser Context, and Policy-Based Authorization](#adr-009-jwt-bearer-authentication-scoped-icurrentuser-context-and-policy-based-authorization)
- [ADR-010: Phased Solution Scaffolding Restricted to Stage 1 Active Domains](#adr-010-phased-solution-scaffolding-restricted-to-stage-1-active-domains)

---

## ADR-001: Modular Monolith with Domain Assemblies and Compile-Time Encapsulation

### Status
Accepted

### Context & Problem Statement
Elsasa SIS V3 encompasses 6 broad bounded contexts (`Students`, `Academics`, `Finance`, `Operations`, `Teaching`, `Development`) divided into 20 modules and 87 discrete workflows. We must choose an architecture style and solution project layout that enforces strict domain boundaries without incurring premature distributed system overhead.

### Considered Options
1. **Microservices Architecture:** Independent deployable services per domain with independent databases and network APIs.
2. **Single Web Project with Folder Slices:** Single assembly containing all domains, relying on linting/conventions.
3. **Modular Monolith with Domain Assemblies (Selected):** A single deployable web application host (`Elsasa.Sis.Api`) referencing independent domain assemblies (`Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, etc.) with zero compile-time references between domain assemblies.

### Decision Outcome
Adopt the **Modular Monolith with Domain Assemblies**.
- Slices reside in their respective domain class library projects.
- Domain assemblies reference only `Elsasa.Sis.Shared` (foundational abstractions) and `Elsasa.Sis.Contracts` (public interfaces and event records).
- Domain assemblies **never** reference each other (`Students` cannot reference `Finance` and vice versa).
- The web host (`Elsasa.Sis.Api`) aggregates all domain assemblies exclusively for dependency injection and endpoint routing at application startup.

### Consequences
- **Positive:** Compile-time enforcement guarantees that developers cannot accidentally create tight code coupling between domains. Build times remain fast, deployment is a simple single artifact, and future extraction into independent microservices remains viable if institutional scale ever requires it.
- **Negative:** Requires maintaining multiple `.csproj` files and managing cross-domain interactions through formal contract abstractions.

---

## ADR-002: Pragmatic SQL Server Schema Isolation with Logical UUID Keys and Selective Read JOINs

### Status
Accepted

### Context & Problem Statement
Each domain requires clear data ownership. In SQL Server, we must decide how schemas (`operations`, `students`, `finance`) interact regarding foreign key constraints, transactional boundaries, and cross-domain data querying.

### Considered Options
1. **Strict Database Isolation (Shared-Nothing):** No cross-schema FKs and zero cross-schema SQL JOINs. Cross-domain data composition must occur in-memory via application-level APIs.
2. **Full Relational Coupling:** Physical SQL Server `FOREIGN KEY` constraints across schemas; unrestricted cross-schema joins and writes in shared transactions.
3. **Pragmatic Schema Isolation (Selected):** Logical UUID foreign keys without physical cross-schema database FK constraints. Strict write isolation to the owning schema. Selective cross-schema SQL JOINs permitted strictly for read-only query slices.

### Decision Outcome
Adopt **Pragmatic Schema Isolation**:
- Each domain has an isolated SQL Server schema: `operations`, `students`, `finance` (and later `academics`, `teaching`, `development`).
- **Referential Integrity:** Cross-schema references are stored as `UUID` (`uniqueidentifier`) logical keys. Physical foreign keys across schema boundaries are **prohibited**. Intra-schema foreign keys (within the same domain) are fully enforced.
- **Write Operations:** Command handlers are strictly prohibited from executing `INSERT`, `UPDATE`, or `DELETE` statements on tables outside their owning schema.
- **Read Operations:** Query handlers (CQRS read slices) in Dapper are permitted to selectively `JOIN` against tables in external schemas for projections and reporting (e.g. displaying student names on billing invoices), leveraging SQL Server's native query engine, indexing, sorting, and pagination without in-memory N+1 loops.

### Consequences
- **Positive:** Preserves clean write boundaries and independent schema migration rollbacks. Eliminates distributed join overhead and memory stitching.
- **Negative:** Application logic must ensure logical referential integrity when inserting external references. Cascade deletes across domains do not exist (aligning with SIS audit and historical preservation rules).

---

## ADR-003: Hybrid Cross-Domain Communication (Strongly-Typed Contracts + In-Memory Domain Events)

### Status
Accepted

### Context & Problem Statement
Business workflows often require collaboration between domains (e.g., student enrollment requiring payment confirmation from Finance; graduation requiring academic and financial clearances). With domain assemblies isolated at compile time, we must establish a consistent in-process communication mechanism.

### Considered Options
1. **Pure Event-Driven (In-Memory Pub/Sub):** All cross-domain interactions use in-memory events; modules never query each other directly.
2. **Pure Direct Service Interfaces (Synchronous Only):** All interactions occur through synchronous service method calls.
3. **Hybrid Model (Selected):** Strongly-typed C# contract interfaces for synchronous queries/validations + In-memory Domain Events for post-commit side-effects.

### Decision Outcome
Adopt the **Hybrid Cross-Domain Communication Model**:
- **Synchronous Queries & Checks:** Declared as clean interfaces in `Elsasa.Sis.Contracts` (e.g., `IFinanceModule.IsEnrollmentFeePaidAsync(Guid applicantId, CancellationToken ct)`). Implemented by the target domain assembly and registered in DI. Consumed directly by calling domains.
- **Decoupled Side-Effects / Reactions:** When a domain completes a state transition (e.g., `ApplicantAcceptedEvent`, `StudentEnrolledEvent`), it publishes an in-memory domain event (`INotification` via MediatR) **after** the primary database transaction commits. Other domains register handlers to asynchronously react within the same process.

### Consequences
- **Positive:** Clear distinction between data consumption (explicit, synchronous, type-safe) and workflow side-effects (decoupled, non-blocking). Avoids distributed messaging complexity while maintaining domain decoupling.
- **Negative:** Developers must observe discipline not to publish domain events inside uncommitted transaction blocks.

---

## ADR-004: Explicit Data Access with Dapper, C# Raw String Literals, and ISqlConnectionFactory

### Status
Accepted

### Context & Problem Statement
We require high-performance, maintainable data access with explicit SQL Server queries. Entity Framework Core, ORMs, and generic repository abstractions are explicitly disallowed.

### Considered Options
1. **External `.sql` Resource Files:** Storing SQL queries in standalone `.sql` files loaded as embedded resources.
2. **Stored Procedures Only:** Moving all queries, logic, and transactions into SQL Server stored procedures.
3. **C# Raw String Literals in Slice Handlers + ISqlConnectionFactory (Selected):** Inline SQL using multiline raw string literals (`""" ... """`) directly inside slice handlers, executed via Dapper.

### Decision Outcome
Adopt **C# Raw String Literals in Slice Handlers with Dapper**:
- SQL statements reside directly inside the feature slice handler or adjacent query class using C# raw string literals (`""" ... """`).
- Queries are executed using Dapper extension methods with strongly-typed or anonymous parameter objects to guarantee parameterized SQL injection prevention.
- Database connections are created per operation using an injected `ISqlConnectionFactory.CreateConnectionAsync(cancellationToken)` and disposed via `await using`.
- Transactions are explicit (`await using var tx = await conn.BeginTransactionAsync(ct)`) and utilized **only** when a command mutates multiple tables. Single-statement mutations execute without explicit transaction ceremony.

### Consequences
- **Positive:** Extreme locality of behavior (Vertical Slice purity); complete visibility of executed SQL; zero ORM abstraction tax; type-safe parameterization; zero friction with external SQL file loading.
- **Negative:** SQL syntax validation is deferred to integration testing; requires discipline in authoring consistent T-SQL queries.

---

## ADR-005: Native Minimal APIs with Direct Slice Handlers and FluentValidation Endpoint Filters

### Status
Accepted

### Context & Problem Statement
We need an API architecture that exposes HTTP endpoints aligned with Vertical Slice Architecture, eliminating controller bloat and unnecessary dispatch indirection.

### Considered Options
1. **ASP.NET Core Controllers:** Traditional controller classes grouping actions.
2. **FastEndpoints:** Third-party REPR framework.
3. **Native Minimal APIs with Direct Slice Handlers (Selected):** Native ASP.NET Core .NET 8 Minimal APIs with an `IEndpoint` auto-discovery interface, directly invoking slice handlers.

### Decision Outcome
Adopt **Native Minimal APIs with Direct Feature Handlers**:
- Each vertical slice defines an `IEndpoint` implementation (`MapEndpoint(IEndpointRouteBuilder app)`) mapped automatically at application startup.
- Endpoint delegates inject and directly call the slice's specific `Handler` class (e.g., `await handler.HandleAsync(request, ct)`).
- MediatR is **not** used for commands or queries (eliminating runtime dispatch reflection and preserving direct F12 navigation).
- Validation is declared using FluentValidation (`AbstractValidator<TRequest>`) and executed automatically via an endpoint filter (`ValidationFilter<TRequest>`) before invoking the handler.

### Consequences
- **Positive:** 100% native .NET 8; zero external routing dependencies; top-tier runtime performance; crystal-clear code navigation; automatic OpenAPI/Swagger integration.
- **Negative:** Requires an assembly-scanning helper to discover and invoke `IEndpoint` implementations across assemblies at startup.

---

## ADR-006: Database Schema Migrations via DbUp with Deterministic T-SQL Scripts

### Status
Accepted

### Context & Problem Statement
With over 80 database tables across multiple schemas and no ORM migrations, we need an automated, reliable database schema migration tool that executes pure T-SQL scripts.

### Considered Options
1. **FluentMigrator:** C# code-based migration DSL.
2. **Manual External SQL Scripts:** Manual execution via `sqlcmd` or external DBA pipelines.
3. **DbUp with Sequential T-SQL Scripts (Selected):** Lightweight .NET migration runner executing raw `.sql` files tracked in a `_SchemaVersions` table.

### Decision Outcome
Adopt **DbUp-SqlServer**:
- Plain T-SQL scripts are authored and stored in `Elsasa.Sis.Database/Scripts/`, organized by domain folder (`01_Operations`, `02_Students`, `03_Finance`).
- Scripts follow a strict deterministic naming convention: `YYYYMMDD_HHMM_Schema_Description.sql`.
- DbUp tracks applied scripts in `_SchemaVersions` table.
- Migrations execute automatically on application startup in Development and Testing environments, and via a dedicated CLI/startup flag (`--migrate`) in staging and production environments.

### Consequences
- **Positive:** Pure T-SQL DDL (indexes, schemas, partial unique constraints); zero C# migration abstractions; instantaneous execution; seamless integration with Testcontainers.
- **Negative:** Migrations are forward-only; rollbacks must be authored as explicit forward-correcting migration scripts.

---

## ADR-007: Domain Outcome Modeling via Result<T> and RFC 7807 Problem Details

### Status
Accepted

### Context & Problem Statement
We must establish a clean, predictable error handling strategy that avoids using expensive exceptions for standard business rule failures, while providing consistent API error responses.

### Considered Options
1. **Exception-Driven Flow:** Throwing custom domain exceptions for all validation and rule violations, caught by middleware.
2. **HTTP-Coupled Handlers:** Handlers directly returning ASP.NET Core `IResult` / `TypedResults`.
3. **Explicit Result Pattern + RFC 7807 Problem Details (Selected):** Handlers return a typed `Result<T>` with structured domain errors; endpoints translate errors into standard RFC 7807 `ProblemDetails`.

### Decision Outcome
Adopt the **Explicit Result Pattern with RFC 7807 Problem Details**:
- Slice handlers return `Task<Result<TResponse>>` or `Task<Result>`.
- Failures return a structured `DomainError` categorized by error type: `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`.
- Endpoints translate `DomainError` into standard HTTP status codes (400, 404, 409, 422) returning an RFC 7807 `ProblemDetails` payload.
- Exceptions are strictly reserved for unhandled infrastructure crashes (network failures, DB connection drop, disk failure), caught globally by ASP.NET Core's `IExceptionHandler` returning HTTP 500.

### Consequences
- **Positive:** Predictable, explicit business control flow; zero exception-handling performance penalty; handlers remain pure C# classes decoupled from ASP.NET Core HTTP abstractions; standard API error contracts.
- **Negative:** Requires wrapping handler return values in `Result<T>`.

---

## ADR-008: Real SQL Server Integration Testing via Testcontainers, Respawn, and WebApplicationFactory

### Status
Accepted

### Context & Problem Statement
Dapper queries rely heavily on SQL Server-specific syntax (schemas, partial unique indexes, UUID functions, `DATEADD`, table joins). In-memory providers (SQLite, EF In-Memory) cannot test these capabilities faithfully.

### Considered Options
1. **In-Memory / SQLite Unit Testing:** Fast tests that do not execute real T-SQL against SQL Server.
2. **Dedicated Pre-Provisioned SQL Server Instance:** Tests connecting to a shared local or CI SQL Server instance.
3. **Testcontainers + Respawn + WebApplicationFactory (Selected):** Disposable Docker SQL Server container with Respawn database cleanup and full HTTP API integration testing.

### Decision Outcome
Adopt **Testcontainers with Microsoft SQL Server**:
- Integration tests launch a real, disposable Microsoft SQL Server 2022 container via `Testcontainers.MsSql`.
- DbUp executes all migration scripts upon test suite initialization.
- Individual tests execute against endpoints using `WebApplicationFactory<Program>` or directly against slice handlers.
- `Respawn` resets table data between test runs in milliseconds, preventing test interference without restarting the container.
- `NetArchTest` runs as an automated architecture test suite in CI to verify that domain assemblies never reference forbidden dependencies.

### Consequences
- **Positive:** 100% production database parity; reliable testing of actual T-SQL queries, transactions, and schema constraints; zero host machine SQL Server installation requirement; perfectly reproducible in CI pipelines.
- **Negative:** Requires Docker engine on developer workstations and CI runners; initial test run takes a few seconds to boot the container.

---

## ADR-009: JWT Bearer Authentication, Scoped ICurrentUser Context, and Policy-Based Authorization

### Status
Accepted

### Context & Problem Statement
Elsasa SIS defines 16 distinct business actors with specific operational authorities. The database requires audit tracking (`created_at`, `updated_at`, `created_by`) across all tables.

### Considered Options
1. **Deferred / Mock Authentication:** Passing plain custom headers (`X-User-Id`) during initial foundation.
2. **Heavy ASP.NET Identity Framework:** Database-coupled identity tables with cookie authentication.
3. **Standard JWT Bearer + Scoped ICurrentUser + Policy-Based Authorization (Selected):** Industry-standard JWT validation, scoped context service for audit trail propagation, and fine-grained authorization policies.

### Decision Outcome
Adopt **Standard JWT Bearer with `ICurrentUser` Context**:
- Authentication validates standard JWT Bearer tokens containing `sub` (User ID), username, roles, permissions, and optional `campus_id`.
- A scoped service `ICurrentUser` extracts these claims from `HttpContext.User`.
- Slice handlers inject `ICurrentUser` to automatically populate `created_by` in Dapper insert/update statements.
- Endpoints enforce permissions using `.RequireAuthorization("permission_policy")`.
- Integration tests can effortlessly swap `ICurrentUser` with a `TestCurrentUser` fixture to simulate any actor without token minting ceremony.

### Consequences
- **Positive:** Decoupled, production-ready security model; clean audit column population; seamless testability.
- **Negative:** Initial setup requires configuring JWT options and claim-to-policy mappings.

---

## ADR-010: Phased Solution Scaffolding Restricted to Stage 1 Active Domains

### Status
Accepted

### Context & Problem Statement
The SIS development roadmap spans three distinct stages. We must determine whether to scaffold all 6 domain assemblies immediately or restrict the initial foundation to Stage 1 active domains.

### Considered Options
1. **All 6 Domains Upfront:** Scaffold all 6 domain projects immediately in the solution, leaving Stage 2 and Stage 3 projects as empty placeholders.
2. **Stage 1 Scaffolding Only (Selected):** Scaffold only the Stage 1 active domains (`Operations`, `Students`, `Finance`) along with core infrastructure, deferring Stage 2 and Stage 3 projects until their respective roadmap stages begin.

### Decision Outcome
Adopt **Stage 1 Scaffolding Only**:
- The initial solution scaffolds:
  - Infrastructure: `Elsasa.Sis.Api`, `Elsasa.Sis.Shared`, `Elsasa.Sis.Contracts`, `Elsasa.Sis.Database`.
  - Active Domains: `Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, `Elsasa.Sis.Finance`.
  - Test Projects: `Elsasa.Sis.IntegrationTests`, `Elsasa.Sis.ArchitectureTests`.
- Domain assemblies for `Academics`, `Teaching`, and `Development` will be introduced during Stage 2 and Stage 3 initialization when their business modules are scheduled for implementation.

### Consequences
- **Positive:** Clean, focused solution without phantom projects; fast compilation; no premature empty assembly maintenance.
- **Negative:** Adding future domains requires creating new project files when Stage 2 and Stage 3 commence.
