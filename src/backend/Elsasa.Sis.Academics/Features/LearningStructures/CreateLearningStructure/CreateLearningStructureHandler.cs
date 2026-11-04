namespace Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;

using Dapper;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler creating a draft curriculum framework container (ADR-004, ADR-005, P3-S06).
/// Validates target academic period existence via <see cref="IOperationsCalendarQueryService"/>
/// and persists the initial draft record in <c>academics.learning_structures</c>.
/// </summary>
public sealed class CreateLearningStructureHandler(
    ISqlConnectionFactory connectionFactory,
    IOperationsCalendarQueryService calendarQueryService,
    ICurrentUser currentUser)
{
    private const string _insertStructureSql = """
        INSERT INTO academics.learning_structures (
            id,
            name,
            description,
            academic_period_id,
            academic_period_label,
            version,
            effective_date,
            end_date,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @Name,
            @Description,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            @Version,
            @EffectiveDate,
            @EndDate,
            @Status,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes validation and draft framework creation.
    /// </summary>
    /// <param name="request">Creation request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the created structure metadata or a domain error.</returns>
    public async Task<Result<CreateLearningStructureResponse>> HandleAsync(
        CreateLearningStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Verify target academic period exists via cross-domain query contract
        var periodResult = await calendarQueryService.GetOperationalPeriodByIdAsync(
            request.AcademicPeriodId,
            cancellationToken);

        if (!periodResult.IsSuccess || periodResult.Value is null)
        {
            return Result.Failure<CreateLearningStructureResponse>(
                DomainError.NotFound(
                    "Academics.AcademicPeriodNotFound",
                    $"Operational academic period with ID '{request.AcademicPeriodId}' was not found."));
        }

        var period = periodResult.Value;
        var periodLabel = !string.IsNullOrWhiteSpace(period.Name) ? period.Name : period.PeriodCode;

        // 2. Prepare framework entity attributes
        var structureId = Guid.NewGuid();
        const string initialStatus = "draft";
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";

        var parameters = new
        {
            Id = structureId,
            request.Name,
            request.Description,
            AcademicPeriodId = request.AcademicPeriodId,
            AcademicPeriodLabel = periodLabel,
            request.Version,
            request.EffectiveDate,
            request.EndDate,
            Status = initialStatus,
            request.Notes,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };

        // 3. Persist record in academics schema
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            _insertStructureSql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success(new CreateLearningStructureResponse(
            structureId,
            request.Name,
            request.Description,
            request.AcademicPeriodId,
            periodLabel,
            request.Version,
            request.EffectiveDate,
            request.EndDate,
            initialStatus,
            request.Notes,
            createdAt,
            createdBy));
    }
}
