# Coding Conventions Guide — Elsasa SIS V3 Backend

**Document Version:** 1.0  
**Language / Framework:** C# 12 / .NET 8 / ASP.NET Core  
**Data Access:** Dapper & Microsoft SQL Server 2022  
**Status:** Approved  

---

## 1. General C# 12 & .NET 8 Idioms

- **File-Scoped Namespaces:** Always use file-scoped namespaces (`namespace Elsasa.Sis.Students.Features.Admission;`).
- **Primary Constructors:** Use primary constructors for dependency injection in classes and handlers (`public sealed class RegisterApplicationHandler(ISqlConnectionFactory connectionFactory, ICurrentUser currentUser)`).
- **Nullability:** Treat nullable warnings as errors. Enable `<Nullable>enable</Nullable>` in all projects.
- **Records for Data Contracts:** Use `public sealed record` for all DTOs, requests, responses, and events.
- **Async All the Way:** Use asynchronous APIs for all I/O bound operations (`Task`, `ValueTask`). Pass `CancellationToken` explicitly down to all Dapper and database calls.

---

## 2. Naming Conventions

| Element | Convention | Example |
|---|---|---|
| **Class / Struct / Record** | PascalCase | `RegisterApplicationHandler`, `ApplicantDto` |
| **Interface** | `I` + PascalCase | `ISqlConnectionFactory`, `ICurrentUser` |
| **Method** | PascalCase, verb-first | `HandleAsync`, `CreateConnectionAsync` |
| **Private Field** | `_` + camelCase (if not primary ctor) | `_connectionFactory` |
| **Parameter** | camelCase | `cancellationToken`, `applicantId` |
| **Local Variable** | camelCase | `connection`, `transaction` |
| **SQL Column** | snake_case (in DB) | `created_at`, `student_number`, `admission_period_id` |
| **SQL Parameter** | `@` + snake_case or camelCase | `@applicant_id`, `@full_name` |
| **SQL Table** | snake_case, pluralized | `students.admission_applications` |
| **REST Route** | lowercase kebab-case, pluralized nouns | `/api/students/admission-applications` |

---

## 3. Vertical Slice Structure

Each feature slice lives in `Features/<Submodule>/<Action>/`. Slices must remain self-contained.

### Preferred File Layout
```
Features/Admission/RegisterApplication/
├── RegisterApplicationEndpoint.cs  # Implements IEndpoint
├── RegisterApplicationRequest.cs   # Public request record
├── RegisterApplicationResponse.cs  # Public response record
├── RegisterApplicationValidator.cs # FluentValidation AbstractValidator
└── RegisterApplicationHandler.cs   # Internal handler executing Dapper SQL
```

*Note:* For simple slices (e.g. basic lookups or simple status updates), Request, Response, and Handler may be placed in a single file `RegisterApplication.cs` if total lines remain under 150 lines.

---

## 4. Feature Handler Implementation Pattern

Every handler must:
1. Accept an explicit request record and a `CancellationToken`.
2. Return a `Task<Result<TResponse>>` or `Task<Result>`.
3. Never throw business exceptions; return `Result.Failure(...)`.
4. Be marked `public sealed` or `internal sealed`.

```csharp
namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

using System.Data;
using System.Data.SqlClient;
using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

public sealed class RegisterApplicationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    public async Task<Result<RegisterApplicationResponse>> HandleAsync(
        RegisterApplicationRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Check logical business constraints (e.g. single active application per period)
        const string checkSql = """
            SELECT COUNT(1)
            FROM students.admission_applications
            WHERE applicant_id = @ApplicantId
              AND admission_period_id = @AdmissionPeriodId
              AND status NOT IN ('withdrawn', 'decided');
            """;

        var existingCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(checkSql, new { request.ApplicantId, request.AdmissionPeriodId }, cancellationToken: cancellationToken));

        if (existingCount > 0)
        {
            return Result.Failure<RegisterApplicationResponse>(
                DomainError.Conflict("Admission.DuplicateActiveApplication", "An active application already exists for this applicant in the specified admission period."));
        }

        // 2. Perform atomic insert
        var applicationId = Guid.NewGuid();
        var applicationNumber = $"ADM-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}";

        const string insertSql = """
            INSERT INTO students.admission_applications (
                id,
                application_number,
                applicant_id,
                admission_period_id,
                application_type,
                requested_entry_level,
                requested_campus,
                requested_program,
                registration_date,
                status,
                notes,
                created_at,
                created_by
            ) VALUES (
                @Id,
                @ApplicationNumber,
                @ApplicantId,
                @AdmissionPeriodId,
                @ApplicationType,
                @RequestedEntryLevel,
                @RequestedCampus,
                @RequestedProgram,
                @RegistrationDate,
                @Status,
                @Notes,
                SYSUTCDATETIME(),
                @CreatedBy
            );
            """;

        var parameters = new
        {
            Id = applicationId,
            ApplicationNumber = applicationNumber,
            request.ApplicantId,
            request.AdmissionPeriodId,
            ApplicationType = request.ApplicationType ?? "new",
            request.RequestedEntryLevel,
            request.RequestedCampus,
            request.RequestedProgram,
            RegistrationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "registered",
            request.Notes,
            CreatedBy = currentUser.Username
        };

        await connection.ExecuteAsync(new CommandDefinition(insertSql, parameters, cancellationToken: cancellationToken));

        return Result.Success(new RegisterApplicationResponse(applicationId, applicationNumber, "registered"));
    }
}
```

---

## 5. Dapper & T-SQL Conventions

1. **Multiline Raw String Literals:** Always use C# 11+ raw string literals (`""" ... """`) for SQL.
2. **Uppercase T-SQL Keywords:** Write SQL keywords in uppercase (`SELECT`, `FROM`, `WHERE`, `INSERT INTO`, `VALUES`, `UPDATE`, `SET`, `DELETE`, `JOIN`, `ON`, `GROUP BY`, `ORDER BY`).
3. **Explicit Schema Qualification:** **Always** qualify tables with their schema: `students.applicants`, `finance.obligations`, `operations.academic_years`. Unqualified table references (e.g. `SELECT * FROM applicants`) are strictly rejected.
4. **No SELECT \*:** Always enumerate the exact columns required in queries.
5. **Dapper Parameterization:** Always use parameterized SQL (`@param`). Never concatenate or interpolate strings into SQL.
6. **CommandDefinition:** Prefer passing `CommandDefinition` with `cancellationToken` to ensure database operations abort when client cancels.

---

## 6. Database Connection & Transaction Guidelines

- **Acquisition:** Always acquire connection via `await connectionFactory.CreateConnectionAsync(cancellationToken)`.
- **Disposal:** Always wrap connections and transactions in `await using`.
- **Transaction Discipline:**
  - **Single Table Writes:** Do **not** open a transaction. Standard SQL Server auto-commit is faster and reduces lock duration.
  - **Multi-Table Writes:** Wrap writes in an explicit transaction:
    ```csharp
    await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
    try
    {
        await connection.ExecuteAsync(sql1, param1, transaction);
        await connection.ExecuteAsync(sql2, param2, transaction);
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
    ```
  - **Cross-Domain Transactions:** Cross-schema distributed transactions (`TransactionScope`) are **prohibited**. Use the post-commit domain event pattern for cross-domain side-effects.

---

## 7. Endpoint & Validation Conventions

### Minimal API Endpoint (`IEndpoint`)
```csharp
namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public sealed class RegisterApplicationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/students/admission/applications", HandleAsync)
           .WithName("RegisterAdmissionApplication")
           .WithTags("Admission")
           .RequireAuthorization("Permissions.Students.Admission.Register")
           .AddEndpointFilter<ValidationFilter<RegisterApplicationRequest>>()
           .Produces<RegisterApplicationResponse>(StatusCodes.Status201Created)
           .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
           .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        RegisterApplicationRequest request,
        RegisterApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/admission/applications/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
```

### FluentValidation Validator
```csharp
namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

using FluentValidation;

public sealed class RegisterApplicationValidator : AbstractValidator<RegisterApplicationRequest>
{
    public RegisterApplicationValidator()
    {
        RuleFor(x => x.ApplicantId)
            .NotEmpty().WithMessage("Applicant ID is required.");

        RuleFor(x => x.AdmissionPeriodId)
            .NotEmpty().WithMessage("Admission period ID is required.");

        RuleFor(x => x.RequestedEntryLevel)
            .NotEmpty().WithMessage("Requested entry level is required.")
            .MaximumLength(50).WithMessage("Requested entry level cannot exceed 50 characters.");

        RuleFor(x => x.ApplicationType)
            .Must(t => t == null || t == "new" || t == "transfer_in")
            .WithMessage("Application type must be either 'new' or 'transfer_in'.");
    }
}
```

---

## 8. Dependency Injection Registration

Each project provides a clean `DependencyInjection.cs` extension method on `IServiceCollection`:

```csharp
namespace Elsasa.Sis.Students;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddStudentsDomain(this IServiceCollection services)
    {
        // Scan and register all slice handlers
        services.Scan(scan => scan
            .FromAssemblyOf<IStudentsAssemblyMarker>()
            .AddClasses(classes => classes.Where(c => c.Name.EndsWith("Handler")))
            .AsSelf()
            .WithScopedLifetime());

        // Register domain contracts
        services.AddScoped<IStudentsModule, StudentsModuleImpl>();

        return services;
    }
}
```

---

## 9. Testing Conventions

- **Frameworks:** xUnit, FluentAssertions, Testcontainers.MsSql, Respawn.
- **Naming Pattern:** `MethodUnderTest_Should_ExpectedBehavior_When_Condition`
- **AAA Structure:** Arrange, Act, Assert clearly demarcated with comments.

```csharp
[Fact]
public async Task RegisterApplication_Should_ReturnConflict_When_ActiveApplicationAlreadyExists()
{
    // Arrange
    var applicantId = await Fixture.CreateApplicantAsync();
    var periodId = await Fixture.CreateAdmissionPeriodAsync();
    await Fixture.CreateActiveApplicationAsync(applicantId, periodId);

    var client = Factory.CreateClient();
    var request = new RegisterApplicationRequest(applicantId, periodId, "Grade 1", "Campus Utara", "Reguler", null, null);

    // Act
    var response = await client.PostAsJsonAsync("/api/students/admission/applications", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem!.Extensions["errorCode"]!.ToString().Should().Be("Admission.DuplicateActiveApplication");
}
```
