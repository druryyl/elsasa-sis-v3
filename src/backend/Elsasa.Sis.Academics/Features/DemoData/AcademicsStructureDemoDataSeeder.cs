namespace Elsasa.Sis.Academics.Features.DemoData;

using System.Data;
using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous seeder for the Academics domain curriculum structure (ADR-002, ADR-004, TD-02, TD-05).
/// Seeds the two-tier ACE PACE curriculum framework:
/// - Learning Structure: "Hagios ACE Individualized Curriculum" with ACE_PACE framework
/// - 5 core parent subjects (unit_type = 'subject', parent_unit_id IS NULL)
/// - Child PACE units per subject (unit_type = 'pace', parent_unit_id linked to parent) with passing_score = 80.0
/// - Standard grading requirements with numeric scale and 80.0 KKM
/// </summary>
public sealed class AcademicsStructureDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : IAcademicsStructureDemoDataSeeder
{
    private const string _createdBy = "demo-data-seeder";

    private const string _insertLearningStructureSql = """
        INSERT INTO academics.learning_structures (
            id, name, description, academic_period_id, academic_period_label,
            version, effective_date, end_date, status, approved_by, approved_date,
            supersedes_id, notes, created_at, created_by
        )
        VALUES (
            @Id, @Name, @Description, @AcademicPeriodId, @AcademicPeriodLabel,
            @Version, @EffectiveDate, @EndDate, @Status, @ApprovedBy, @ApprovedDate,
            @SupersedesId, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertLearningUnitSql = """
        INSERT INTO academics.learning_units (
            id, learning_structure_id, parent_unit_id, code, name, unit_type,
            level, description, credit_weight, status, notes,
            created_at, created_by
        )
        VALUES (
            @Id, @LearningStructureId, @ParentUnitId, @Code, @Name, @UnitType,
            @Level, @Description, @CreditWeight, @Status, @Notes,
            @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertAcademicRequirementSql = """
        INSERT INTO academics.academic_requirements (
            id, name, requirement_scope, learning_structure_id, learning_unit_id,
            level, requirement_type, description, version, effective_date, end_date,
            status, approved_by, approved_date, supersedes_id, notes,
            created_at, created_by
        )
        VALUES (
            @Id, @Name, @RequirementScope, @LearningStructureId, @LearningUnitId,
            @Level, @RequirementType, @Description, @Version, @EffectiveDate, @EndDate,
            @Status, @ApprovedBy, @ApprovedDate, @SupersedesId, @Notes,
            @CreatedAt, @CreatedBy
        );
        """;

    public async Task<AcademicsStructureSeedResult> SeedAcademicsStructureAsync(
        OperationsSeedResult operationsContext,
        CancellationToken cancellationToken = default)
    {
        var academicYearId = operationsContext.AcademicYearId;
        var academicPeriodLabel = operationsContext.AcademicYearCode; // e.g., "AY-2026-2027"
        var effectiveDate = DateOnly.FromDateTime(operationsContext.BaseDate.AddDays(-180)); // Academic year start
        var now = DateTimeOffset.UtcNow;
        var learningStructureId = Guid.NewGuid();

        var subjectUnitIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var paceUnitIds = new List<Guid>();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Insert Learning Structure
            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningStructureSql,
                new
                {
                    Id = learningStructureId,
                    Name = "Hagios ACE Individualized Curriculum",
                    Description = "Accelerated Christian Education (ACE) PACE individualized learning curriculum for Hagios School of Life across ELC, JHLC, and HSLC",
                    AcademicPeriodId = academicYearId,
                    AcademicPeriodLabel = academicPeriodLabel,
                    Version = "2026.1",
                    EffectiveDate = effectiveDate,
                    EndDate = (DateOnly?)null,
                    Status = "active",
                    ApprovedBy = "Academic Authority",
                    ApprovedDate = effectiveDate,
                    SupersedesId = (Guid?)null,
                    Notes = "Two-tier curriculum: 5 core parent subjects with child PACE units. 80% passing threshold (KKM) enforced.",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 2. Insert 5 Core Parent Subjects
            var parentSubjects = new[]
            {
                new { Code = "MATH", Name = "Math", Level = "All Grades", Description = "Mathematics - ACE PACE curriculum" },
                new { Code = "ENG", Name = "English", Level = "All Grades", Description = "English Language Arts - ACE PACE curriculum" },
                new { Code = "SCI", Name = "Science", Level = "All Grades", Description = "Science - ACE PACE curriculum" },
                new { Code = "SS", Name = "Social Studies", Level = "All Grades", Description = "Social Studies - ACE PACE curriculum" },
                new { Code = "BIBLE", Name = "Word Building / Bible Reading", Level = "All Grades", Description = "Word Building and Bible Reading - ACE PACE curriculum" }
            };

            foreach (var subject in parentSubjects)
            {
                var subjectId = Guid.NewGuid();
                subjectUnitIds[subject.Code] = subjectId;

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertLearningUnitSql,
                    new
                    {
                        Id = subjectId,
                        LearningStructureId = learningStructureId,
                        ParentUnitId = (Guid?)null,
                        Code = subject.Code,
                        Name = subject.Name,
                        UnitType = "subject",
                        Level = subject.Level,
                        Description = subject.Description,
                        CreditWeight = 5.0m,
                        Status = "active",
                        Notes = $"Parent subject unit for {subject.Name} - ACE PACE framework",
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // 3. Insert Child PACE Units for each parent subject
            var paceDefinitions = new[]
            {
                new { ParentCode = "MATH", Prefix = "M", Start = 1037, End = 1042, SubjectName = "Math" },
                new { ParentCode = "ENG", Prefix = "E", Start = 1037, End = 1042, SubjectName = "English" },
                new { ParentCode = "SCI", Prefix = "SC", Start = 1037, End = 1042, SubjectName = "Science" },
                new { ParentCode = "SS", Prefix = "SS", Start = 1037, End = 1042, SubjectName = "Social Studies" },
                new { ParentCode = "BIBLE", Prefix = "B", Start = 1037, End = 1042, SubjectName = "Word Building / Bible Reading" }
            };

            foreach (var def in paceDefinitions)
            {
                var parentId = subjectUnitIds[def.ParentCode];

                for (int i = def.Start; i <= def.End; i++)
                {
                    var paceCode = $"{def.Prefix}{i}";
                    var paceName = $"{def.SubjectName} PACE {paceCode}";
                    var paceId = Guid.NewGuid();
                    paceUnitIds.Add(paceId);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertLearningUnitSql,
                        new
                        {
                            Id = paceId,
                            LearningStructureId = learningStructureId,
                            ParentUnitId = parentId,
                            Code = paceCode,
                            Name = paceName,
                            UnitType = "pace",
                            Level = "All Grades",
                            Description = $"{def.SubjectName} PACE unit {paceCode} - individualized mastery learning",
                            CreditWeight = 1.0m,
                            Status = "active",
                                Notes = $"Child PACE unit under {def.SubjectName} - 80% mastery threshold (KKM)",
                            CreatedAt = now,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // 4. Insert Standard Grading Requirements
            // 4a. Program-level graduation requirements
            var gradReqId = Guid.NewGuid();
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAcademicRequirementSql,
                new
                {
                    Id = gradReqId,
                    Name = "ACE PACE Graduation Requirements — TP 2026/2027",
                    RequirementScope = "program",
                    LearningStructureId = learningStructureId,
                    LearningUnitId = (Guid?)null,
                    Level = (string?)null,
                    RequirementType = "graduation",
                    Description = "Complete all required PACE units across 5 core subjects with minimum 80% mastery score (KKM) per unit. Minimum 12 PACE units per subject per academic year for progression.",
                    Version = "2026.1",
                    EffectiveDate = effectiveDate,
                    EndDate = (DateOnly?)null,
                    Status = "active",
                    ApprovedBy = "Academic Authority",
                    ApprovedDate = effectiveDate,
                    SupersedesId = (Guid?)null,
                    Notes = "ACE PACE graduation policy: numeric evaluation model, 80.0 KKM threshold, capped reassessment at passing score",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 4b. Level progression requirements for each learning center tier
            var levels = new[]
            {
                new { Level = "ELC (Grades 1-6)", Code = "ELC" },
                new { Level = "JHLC (Grades 7-9)", Code = "JHLC" },
                new { Level = "HSLC (Grades 10-12)", Code = "HSLC" }
            };

            foreach (var level in levels)
            {
                var levelReqId = Guid.NewGuid();
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertAcademicRequirementSql,
                    new
                    {
                        Id = levelReqId,
                        Name = $"{level.Level} Progression Requirements — TP 2026/2027",
                        RequirementScope = "level",
                        LearningStructureId = learningStructureId,
                        LearningUnitId = (Guid?)null,
                        Level = level.Level,
                        RequirementType = "progression",
                        Description = $"Complete minimum 12 PACE units per core subject ({level.Code} level) with 80% mastery (KKM) for promotion to next grade level. All 5 core subjects must meet threshold.",
                        Version = "2026.1",
                        EffectiveDate = effectiveDate,
                        EndDate = (DateOnly?)null,
                        Status = "active",
                        ApprovedBy = "Academic Coordinator",
                        ApprovedDate = effectiveDate,
                        SupersedesId = (Guid?)null,
                        Notes = $"{level.Code} progression: numeric scale, 80.0 KKM, reassessment capped at passing score",
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // 4c. Assessment policy requirement (applies to all PACE units)
            var assessmentPolicyId = Guid.NewGuid();
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAcademicRequirementSql,
                new
                {
                    Id = assessmentPolicyId,
                    Name = "ACE PACE Assessment Policy — Numeric Scale with 80% KKM",
                    RequirementScope = "learning_unit",
                    LearningStructureId = learningStructureId,
                    LearningUnitId = (Guid?)null,
                    Level = (string?)null,
                    RequirementType = "assessment_policy",
                    Description = "All PACE units use numeric evaluation model (0-100 scale) with 80.0 minimum passing score (KKM). Reassessment policy: capped_at_passing (retake scores capped at 80.0). Formative checkups and self-tests do not count toward final mastery; only PACE Test scores are final.",
                    Version = "2026.1",
                    EffectiveDate = effectiveDate,
                    EndDate = (DateOnly?)null,
                    Status = "active",
                    ApprovedBy = "Academic Authority",
                    ApprovedDate = effectiveDate,
                    SupersedesId = (Guid?)null,
                    Notes = "Applies to all unit_type = 'pace' learning units. Evaluation model: numeric. Reassessment: capped_at_passing.",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 4d. Subject-specific completion requirements for each parent subject
            foreach (var subject in parentSubjects)
            {
                var subjectReqId = Guid.NewGuid();
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertAcademicRequirementSql,
                    new
                    {
                        Id = subjectReqId,
                        Name = $"{subject.Name} Subject Completion Criteria",
                        RequirementScope = "learning_unit",
                        LearningStructureId = learningStructureId,
                        LearningUnitId = subjectUnitIds[subject.Code],
                        Level = (string?)null,
                        RequirementType = "completion",
                        Description = $"Complete all 6 PACE units (1037-1042) for {subject.Name} with minimum 80% score on each PACE Test. Mastery demonstrated through sequential PACE completion.",
                        Version = "2026.1",
                        EffectiveDate = effectiveDate,
                        EndDate = (DateOnly?)null,
                        Status = "active",
                        ApprovedBy = "Academic Coordinator",
                        ApprovedDate = effectiveDate,
                        SupersedesId = (Guid?)null,
                        Notes = $"{subject.Name} subject: 6 PACE units, numeric evaluation, 80.0 KKM per unit",
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);

            return new AcademicsStructureSeedResult(
                LearningStructureId: learningStructureId,
                SubjectUnitIds: subjectUnitIds,
                PaceUnitIds: paceUnitIds);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}