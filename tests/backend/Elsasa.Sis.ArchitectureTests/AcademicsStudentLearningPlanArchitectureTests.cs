namespace Elsasa.Sis.ArchitectureTests;

using System.Reflection;
using System.Text.RegularExpressions;
using Elsasa.Sis.Contracts.Common;
using Elsasa.Sis.Shared.Endpoints;
using NetArchTest.Rules;
using Xunit;

/// <summary>
/// Architecture and domain boundary verification tests for Stage 2 Academic Student Learning Plan (Submodule 5, P5-S16).
/// Enforces ADR-001 (Modular Monolith compile-time boundaries), ADR-002 (Pragmatic SQL Server schema isolation),
/// ADR-003 (In-memory MediatR domain events in Contracts), ADR-004 (Explicit Dapper direct data access),
/// and ADR-005 (Native Minimal APIs with direct slice handlers).
/// </summary>
public sealed class AcademicsStudentLearningPlanArchitectureTests
{
    private static readonly Assembly _academicsAssembly = TestAssemblies.Get(TestAssemblies.Academics);
    private static readonly Assembly _contractsAssembly = TestAssemblies.Get(TestAssemblies.Contracts);
    private static readonly Assembly _databaseAssembly = TestAssemblies.Get(TestAssemblies.Database);

    private const string _slpFeatureNamespacePrefix = "Elsasa.Sis.Academics.Features.StudentLearningPlans";
    private const string _requestHandlerInterfacePrefix = "MediatR.IRequestHandler`";
    private const string _streamRequestHandlerInterfacePrefix = "MediatR.IStreamRequestHandler`";
    private const string _requestInterfaceName = "MediatR.IRequest";
    private const string _requestInterfacePrefix = "MediatR.IRequest`";
    private const string _senderInterfaceName = "MediatR.ISender";
    private const string _abstractValidatorTypeName = "FluentValidation.AbstractValidator`1";

    #region 1. Compile-Time Assembly Isolation & Boundaries (ADR-001)

    [Fact]
    public void AcademicsAssembly_Should_NotReferenceSiblingDomains_InAssemblyMetadata()
    {
        // Act
        var references = TestAssemblies.References(_academicsAssembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(
            references,
            TestAssemblies.Students,
            TestAssemblies.Operations,
            TestAssemblies.Finance);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void AcademicsAssembly_Should_NotReferenceInfrastructureAssemblies_InAssemblyMetadata()
    {
        // Act
        var references = TestAssemblies.References(_academicsAssembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(
            references,
            TestAssemblies.Api,
            TestAssemblies.Database);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void AcademicsAssembly_Should_OnlyReferenceSharedAndContracts_AmongSisAssemblies()
    {
        // Act
        var sisReferences = TestAssemblies.SisReferences(_academicsAssembly);

        // Assert
        Assert.NotEmpty(sisReferences);
        Assert.All(
            sisReferences,
            name => Assert.True(
                string.Equals(name, TestAssemblies.Shared, StringComparison.Ordinal) ||
                string.Equals(name, TestAssemblies.Contracts, StringComparison.Ordinal),
                $"Elsasa.Sis.Academics must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{name}'."));
    }

    [Fact]
    public void AcademicsAssembly_Should_NotHaveTypeLevelDependencies_OnSiblingDomains()
    {
        // NetArchTest verification of type-level dependencies (Mono.Cecil)
        TestAssemblies.AssertNoTypeLevelDependency(
            _academicsAssembly,
            TestAssemblies.Operations,
            TestAssemblies.Students,
            TestAssemblies.Finance,
            TestAssemblies.Api);
    }

    #endregion

    #region 2. Schema Write Boundaries & Zero Cross-Schema Foreign Keys (ADR-001, ADR-002)

    [Fact]
    public void StudentLearningPlansCommandHandlers_ShouldOnlyWriteToAcademicsSchema()
    {
        // ADR-002: Write operations (INSERT, UPDATE, DELETE, MERGE) must mutate ONLY tables
        // in the owning schema (academics.*) and are strictly prohibited from writing to external schemas.
        var types = GetAllTypes(_academicsAssembly)
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal));

        var writeKeywords = new[] { "INSERT INTO ", "UPDATE ", "DELETE FROM ", "MERGE INTO " };
        var forbiddenSchemas = new[] { "students.", "operations.", "finance.", "dbo." };

        var violations = new List<string>();

        foreach (var type in types)
        {
            var stringFields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => (Name: f.Name, Value: f.GetValue(null) as string))
                .Where(f => !string.IsNullOrEmpty(f.Value));

            foreach (var (fieldName, sql) in stringFields)
            {
                if (sql is null)
                {
                    continue;
                }

                foreach (var writeKeyword in writeKeywords)
                {
                    var index = sql.IndexOf(writeKeyword, StringComparison.OrdinalIgnoreCase);
                    while (index >= 0)
                    {
                        var snippet = sql.Substring(index, Math.Min(80, sql.Length - index));
                        foreach (var forbiddenSchema in forbiddenSchemas)
                        {
                            if (snippet.Contains(forbiddenSchema, StringComparison.OrdinalIgnoreCase))
                            {
                                violations.Add($"{type.FullName}.{fieldName}: Forbidden cross-schema write detected: '{snippet.Trim()}'");
                            }
                        }

                        index = sql.IndexOf(writeKeyword, index + writeKeyword.Length, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Submodule5DbUpMigration_Should_NotDeclareCrossSchemaForeignKeys()
    {
        // ADR-001, ADR-002: Relational tables must have zero cross-schema physical foreign keys.
        const string scriptResourceName = "Elsasa.Sis.Database.Scripts.20261107_0402_Academics_StudentLearningPlan_Tables.sql";
        using var stream = _databaseAssembly.GetManifestResourceStream(scriptResourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var scriptContent = reader.ReadToEnd();

        // Match all REFERENCES <target> patterns
        var referencesMatches = Regex.Matches(
            scriptContent,
            @"REFERENCES\s+([a-zA-Z0-9_\.\[\]]+)",
            RegexOptions.IgnoreCase);

        Assert.NotEmpty(referencesMatches);

        foreach (Match match in referencesMatches)
        {
            var targetTable = match.Groups[1].Value.Trim().Replace("[", string.Empty).Replace("]", string.Empty);

            Assert.True(
                targetTable.StartsWith("academics.", StringComparison.OrdinalIgnoreCase),
                $"Submodule 5 migration script must only declare intra-schema foreign keys referencing 'academics.*', but references '{targetTable}'.");

            Assert.False(
                targetTable.StartsWith("students.", StringComparison.OrdinalIgnoreCase) ||
                targetTable.StartsWith("operations.", StringComparison.OrdinalIgnoreCase) ||
                targetTable.StartsWith("finance.", StringComparison.OrdinalIgnoreCase),
                $"Forbidden cross-schema physical foreign key detected in migration script: '{targetTable}'.");
        }
    }

    [Fact]
    public void Submodule5DbUpMigration_Should_EnforceFilteredUniqueIndexOnActiveSlp()
    {
        // ADR-002, TD-01: At most one active SLP per student per academic period
        const string scriptResourceName = "Elsasa.Sis.Database.Scripts.20261107_0402_Academics_StudentLearningPlan_Tables.sql";
        using var stream = _databaseAssembly.GetManifestResourceStream(scriptResourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var scriptContent = reader.ReadToEnd();

        Assert.Contains("UQ_academics_slp_active", scriptContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE [status] = 'active'", scriptContent, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 3. Domain Events Reside in Contracts and Implement IDomainEvent (ADR-003)

    [Theory]
    [InlineData("StudentLearningPlanCreatedEvent")]
    [InlineData("StudentLearningPlanApprovedEvent")]
    [InlineData("StudentLearningPlanRevisedEvent")]
    [InlineData("StudentLearningPlanParticipationsSyncedEvent")]
    public void StudentLearningPlanDomainEvents_Should_ImplementIDomainEvent_AndResideInContractsAcademicsEvents(string eventName)
    {
        // Arrange
        const string expectedNamespace = "Elsasa.Sis.Contracts.Academics.Events";
        var eventType = _contractsAssembly.GetType($"{expectedNamespace}.{eventName}");

        // Assert
        Assert.NotNull(eventType);
        Assert.True(eventType.IsSealed, $"Domain event '{eventName}' must be sealed.");
        Assert.True(
            typeof(IDomainEvent).IsAssignableFrom(eventType),
            $"Domain event '{eventName}' must implement IDomainEvent.");

        var occurredOnProperty = eventType.GetProperty("OccurredOn", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(occurredOnProperty);
        Assert.Equal(typeof(DateTimeOffset), occurredOnProperty.PropertyType);
    }

    [Fact]
    public void AcademicsAssembly_Should_NotContainDomainEventDefinitions()
    {
        // ADR-001, ADR-003: Domain events are shared contract records and must live in Elsasa.Sis.Contracts,
        // never inside domain implementation assemblies.
        var eventTypesInAcademics = GetAllTypes(_academicsAssembly)
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t))
            .ToArray();

        Assert.Empty(eventTypesInAcademics);
    }

    #endregion

    #region 4. Vertical Slice Conventions for Handlers and Endpoints (ADR-004, ADR-005)

    [Fact]
    public void StudentLearningPlansHandlers_Should_FollowVerticalSliceConventions()
    {
        // Act
        var handlerTypes = GetAllTypes(_academicsAssembly)
            .Where(t => t.IsClass && !t.IsAbstract
                && t.Namespace is not null
                && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal)
                && t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();

        // Assert
        Assert.NotEmpty(handlerTypes);
        Assert.Equal(14, handlerTypes.Length);

        foreach (var handler in handlerTypes)
        {
            Assert.True(
                handler.IsSealed,
                $"Handler class '{handler.FullName}' must be sealed.");

            Assert.True(
                handler.Namespace is not null && Regex.IsMatch(handler.Namespace, @"^Elsasa\.Sis\.Academics\.Features\.StudentLearningPlans\..+"),
                $"Handler '{handler.FullName}' must reside within a feature slice sub-namespace under '{_slpFeatureNamespacePrefix}'.");

            var handleAsyncMethod = handler.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(handleAsyncMethod);
            Assert.True(
                typeof(Task).IsAssignableFrom(handleAsyncMethod.ReturnType),
                $"Handler '{handler.FullName}.HandleAsync' must return a Task.");
        }
    }

    [Theory]
    [InlineData("CreateStudentLearningPlanHandler")]
    [InlineData("UpdateStudentLearningPlanHandler")]
    [InlineData("ArchiveStudentLearningPlanHandler")]
    [InlineData("GetStudentLearningPlanByIdHandler")]
    [InlineData("GetStudentLearningPlansHandler")]
    [InlineData("GetActiveStudentLearningPlanHandler")]
    [InlineData("AddSlpLearningUnitHandler")]
    [InlineData("RemoveSlpLearningUnitHandler")]
    [InlineData("AddAcademicPlanAdjustmentHandler")]
    [InlineData("RemoveAcademicPlanAdjustmentHandler")]
    [InlineData("SubmitStudentLearningPlanHandler")]
    [InlineData("ApproveStudentLearningPlanHandler")]
    [InlineData("ReviseStudentLearningPlanHandler")]
    [InlineData("SyncSlpParticipationsHandler")]
    public void StudentLearningPlansFeatureHandlers_Should_FollowVerticalSliceConventions(string handlerName)
    {
        var handlerType = GetAllTypes(_academicsAssembly)
            .FirstOrDefault(t => t.Name == handlerName);

        Assert.NotNull(handlerType);
        Assert.True(handlerType.IsSealed, $"Handler '{handlerName}' must be sealed.");
        Assert.True(
            handlerType.Namespace is not null && handlerType.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal),
            $"Handler '{handlerName}' must reside in a StudentLearningPlans feature slice namespace.");

        var handleAsyncMethod = handlerType.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(handleAsyncMethod);
        Assert.True(
            typeof(Task).IsAssignableFrom(handleAsyncMethod.ReturnType),
            $"Handler '{handlerName}.HandleAsync' must return a Task.");
    }

    [Fact]
    public void StudentLearningPlansEndpoints_Should_ImplementIEndpoint_BeSealed_AndResideInFeatureSlices()
    {
        // Act
        var endpointTypes = GetAllTypes(_academicsAssembly)
            .Where(t => t.IsClass && !t.IsAbstract
                && t.Namespace is not null
                && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal)
                && t.Name.EndsWith("Endpoint", StringComparison.Ordinal))
            .ToArray();

        // Assert
        Assert.NotEmpty(endpointTypes);
        Assert.Equal(14, endpointTypes.Length);

        foreach (var endpoint in endpointTypes)
        {
            Assert.True(
                endpoint.IsSealed,
                $"Endpoint class '{endpoint.FullName}' must be sealed.");

            Assert.True(
                typeof(IEndpoint).IsAssignableFrom(endpoint),
                $"Endpoint class '{endpoint.FullName}' must implement IEndpoint.");

            Assert.True(
                endpoint.Namespace is not null && Regex.IsMatch(endpoint.Namespace, @"^Elsasa\.Sis\.Academics\.Features\.StudentLearningPlans\..+"),
                $"Endpoint '{endpoint.FullName}' must reside within a feature slice sub-namespace under '{_slpFeatureNamespacePrefix}'.");
        }
    }

    [Theory]
    [InlineData("CreateStudentLearningPlanEndpoint")]
    [InlineData("UpdateStudentLearningPlanEndpoint")]
    [InlineData("ArchiveStudentLearningPlanEndpoint")]
    [InlineData("GetStudentLearningPlanByIdEndpoint")]
    [InlineData("GetStudentLearningPlansEndpoint")]
    [InlineData("GetActiveStudentLearningPlanEndpoint")]
    [InlineData("AddSlpLearningUnitEndpoint")]
    [InlineData("RemoveSlpLearningUnitEndpoint")]
    [InlineData("AddAcademicPlanAdjustmentEndpoint")]
    [InlineData("RemoveAcademicPlanAdjustmentEndpoint")]
    [InlineData("SubmitStudentLearningPlanEndpoint")]
    [InlineData("ApproveStudentLearningPlanEndpoint")]
    [InlineData("ReviseStudentLearningPlanEndpoint")]
    [InlineData("SyncSlpParticipationsEndpoint")]
    public void StudentLearningPlansFeatureEndpoints_Should_FollowVerticalSliceConventions(string endpointName)
    {
        var endpointType = GetAllTypes(_academicsAssembly)
            .FirstOrDefault(t => t.Name == endpointName);

        Assert.NotNull(endpointType);
        Assert.True(endpointType.IsSealed, $"Endpoint '{endpointName}' must be sealed.");
        Assert.True(
            typeof(IEndpoint).IsAssignableFrom(endpointType),
            $"Endpoint '{endpointName}' must implement IEndpoint.");
        Assert.True(
            endpointType.Namespace is not null && endpointType.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal),
            $"Endpoint '{endpointName}' must reside in a StudentLearningPlans feature slice namespace.");
    }

    [Fact]
    public void StudentLearningPlansEndpoints_GroupRegistration_Should_ExposeBaseRouteAndTag()
    {
        // Act
        var groupClass = GetAllTypes(_academicsAssembly)
            .FirstOrDefault(t => t.FullName == "Elsasa.Sis.Academics.Features.StudentLearningPlans.StudentLearningPlansEndpoints");

        // Assert
        Assert.NotNull(groupClass);
        Assert.True(groupClass.IsAbstract && groupClass.IsSealed, "Static class must be abstract and sealed.");

        var baseRouteField = groupClass.GetField("BaseRoute", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(baseRouteField);
        Assert.Equal("/api/v1/academics/learning-plans", baseRouteField.GetValue(null));

        var tagField = groupClass.GetField("EndpointTag", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(tagField);
        Assert.Equal("StudentLearningPlans", tagField.GetValue(null));

        var mapMethod = groupClass.GetMethod("MapStudentLearningPlansEndpoints", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(mapMethod);
    }

    #endregion

    #region 5. MediatR Usage Restricted Strictly to Domain Events (ADR-004, ADR-005)

    [Fact]
    public void StudentLearningPlans_Should_NotUseMediatRForRequestsOrInternalHandlers()
    {
        // ADR-004, ADR-005: Commands and queries execute directly via Dapper and Native Minimal APIs.
        // MediatR requests (IRequest, IRequest<T>) and handlers (IRequestHandler<...>) are prohibited.
        var types = GetAllTypes(_academicsAssembly)
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal));

        var violatingTypes = types
            .Where(t => t.GetInterfaces().Any(i =>
                string.Equals(i.FullName, _requestInterfaceName, StringComparison.Ordinal) ||
                (i.FullName is not null && i.FullName.StartsWith(_requestInterfacePrefix, StringComparison.Ordinal)) ||
                (i.FullName is not null && i.FullName.StartsWith(_requestHandlerInterfacePrefix, StringComparison.Ordinal)) ||
                (i.FullName is not null && i.FullName.StartsWith(_streamRequestHandlerInterfacePrefix, StringComparison.Ordinal))))
            .Select(t => t.FullName)
            .ToArray();

        Assert.Empty(violatingTypes);
    }

    [Fact]
    public void StudentLearningPlansHandlers_Should_NotInjectISenderForRequestDispatch()
    {
        // ADR-004, ADR-005: Feature handlers must NOT inject MediatR ISender for internal dispatch.
        // Only IPublisher (or IMediator solely for domain event publishing) is allowed.
        var handlerTypes = GetAllTypes(_academicsAssembly)
            .Where(t => t.IsClass && !t.IsAbstract
                && t.Namespace is not null
                && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal)
                && t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();

        var violatingHandlers = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var constructors = handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    if (string.Equals(param.ParameterType.FullName, _senderInterfaceName, StringComparison.Ordinal))
                    {
                        violatingHandlers.Add($"{handler.FullName} injects forbidden MediatR ISender.");
                    }
                }
            }
        }

        Assert.Empty(violatingHandlers);
    }

    #endregion

    #region 6. Request, Response, and Validator Type Conventions (ADR-004)

    [Fact]
    public void StudentLearningPlansRequestsAndResponses_Should_BeSealedRecords()
    {
        var payloadTypes = GetAllTypes(_academicsAssembly)
            .Where(t => t.Namespace is not null
                && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal)
                && (t.Name.EndsWith("Request", StringComparison.Ordinal) || t.Name.EndsWith("Response", StringComparison.Ordinal)))
            .ToArray();

        Assert.NotEmpty(payloadTypes);

        var nonRecordOrUnsealed = payloadTypes
            .Where(t => !IsRecord(t) || !t.IsSealed)
            .Select(t => t.FullName)
            .ToArray();

        Assert.Empty(nonRecordOrUnsealed);
    }

    [Fact]
    public void StudentLearningPlansValidators_Should_BeSealedAndExtendAbstractValidator()
    {
        var validatorTypes = GetAllTypes(_academicsAssembly)
            .Where(t => t.Namespace is not null
                && t.Namespace.StartsWith(_slpFeatureNamespacePrefix, StringComparison.Ordinal)
                && t.Name.EndsWith("Validator", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(validatorTypes);

        var invalidValidators = validatorTypes
            .Where(v => !v.IsSealed || !IsFluentValidationValidator(v))
            .Select(v => v.FullName)
            .ToArray();

        Assert.Empty(invalidValidators);
    }

    #endregion

    private static IEnumerable<Type> GetAllTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static bool IsRecord(Type type) =>
        type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic) is not null;

    private static bool IsFluentValidationValidator(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType
                && string.Equals(baseType.GetGenericTypeDefinition().FullName, _abstractValidatorTypeName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
