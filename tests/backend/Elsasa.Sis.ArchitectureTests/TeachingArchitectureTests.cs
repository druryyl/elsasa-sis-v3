namespace Elsasa.Sis.ArchitectureTests;

using System.Reflection;
using System.Text.RegularExpressions;
using Elsasa.Sis.Contracts.Common;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Endpoints;
using NetArchTest.Rules;
using Xunit;

/// <summary>
/// Architecture and domain boundary verification tests for Stage 2 Teaching Teacher Profile &amp; Authorization (P6-S17).
/// Enforces ADR-001 (Modular Monolith compile-time boundaries), ADR-002 (Pragmatic SQL Server schema isolation),
/// ADR-003 (In-memory MediatR domain events in Contracts), ADR-004 (Explicit Dapper direct data access),
/// ADR-005 (Native Minimal APIs with direct slice handlers), and ADR-010 (Hybrid Cross-Domain Communication Model).
/// </summary>
public sealed class TeachingArchitectureTests
{
    private static readonly Assembly _teachingAssembly = TestAssemblies.Get(TestAssemblies.Teaching);
    private static readonly Assembly _contractsAssembly = TestAssemblies.Get(TestAssemblies.Contracts);
    private static readonly Assembly _databaseAssembly = TestAssemblies.Get(TestAssemblies.Database);

    private const string _requestHandlerInterfacePrefix = "MediatR.IRequestHandler`";
    private const string _streamRequestHandlerInterfacePrefix = "MediatR.IStreamRequestHandler`";
    private const string _requestInterfaceName = "MediatR.IRequest";
    private const string _requestInterfacePrefix = "MediatR.IRequest`";
    private const string _senderInterfaceName = "MediatR.ISender";

    #region 1. Compile-Time Assembly Isolation & Boundaries (ADR-001)

    [Fact]
    public void TeachingAssembly_Should_NotReferenceSiblingDomains_InAssemblyMetadata()
    {
        // Act
        var references = TestAssemblies.References(_teachingAssembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(
            references,
            TestAssemblies.Students,
            TestAssemblies.Operations,
            TestAssemblies.Finance,
            TestAssemblies.Academics);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void TeachingAssembly_Should_NotReferenceInfrastructureAssemblies_InAssemblyMetadata()
    {
        // Act
        var references = TestAssemblies.References(_teachingAssembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(
            references,
            TestAssemblies.Api,
            TestAssemblies.Database);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void TeachingAssembly_Should_OnlyReferenceSharedAndContracts_AmongSisAssemblies()
    {
        // Act
        var sisReferences = TestAssemblies.SisReferences(_teachingAssembly);

        // Assert
        Assert.NotEmpty(sisReferences);
        Assert.All(
            sisReferences,
            name => Assert.True(
                string.Equals(name, TestAssemblies.Shared, StringComparison.Ordinal) ||
                string.Equals(name, TestAssemblies.Contracts, StringComparison.Ordinal),
                $"Elsasa.Sis.Teaching must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{name}'."));
    }

    [Fact]
    public void TeachingAssembly_Should_NotHaveTypeLevelDependencies_OnSiblingDomains()
    {
        // NetArchTest verification of type-level dependencies (Mono.Cecil)
        TestAssemblies.AssertNoTypeLevelDependency(
            _teachingAssembly,
            TestAssemblies.Operations,
            TestAssemblies.Students,
            TestAssemblies.Finance,
            TestAssemblies.Academics,
            TestAssemblies.Api,
            TestAssemblies.Database);
    }

    #endregion

    #region 2. Schema Write Boundaries & Zero Cross-Schema Foreign Keys (ADR-001, ADR-002)

    [Fact]
    public void TeachingCommandHandlers_ShouldOnlyWriteToTeachingSchema()
    {
        // ADR-002: Write operations (INSERT, UPDATE, DELETE, MERGE) must mutate ONLY tables
        // in the owning schema (teaching.*) and are strictly prohibited from writing to external schemas.
        var types = GetAllTypes(_teachingAssembly);
        var writeKeywords = new[] { "INSERT INTO ", "UPDATE ", "DELETE FROM ", "MERGE INTO " };
        var forbiddenSchemas = new[] { "academics.", "students.", "operations.", "finance.", "dbo." };

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
    public void TeachingDbUpMigration_Should_NotDeclareCrossSchemaForeignKeys()
    {
        // ADR-001, ADR-002: Relational tables must have zero cross-schema physical foreign keys.
        const string scriptResourceName = "Elsasa.Sis.Database.Scripts.20261110_0501_Teaching_TeacherProfile_Tables.sql";
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
                targetTable.StartsWith("teaching.", StringComparison.OrdinalIgnoreCase),
                $"Teaching migration script must only declare intra-schema foreign keys referencing 'teaching.*', but references '{targetTable}'.");

            Assert.False(
                targetTable.StartsWith("students.", StringComparison.OrdinalIgnoreCase) ||
                targetTable.StartsWith("operations.", StringComparison.OrdinalIgnoreCase) ||
                targetTable.StartsWith("finance.", StringComparison.OrdinalIgnoreCase) ||
                targetTable.StartsWith("academics.", StringComparison.OrdinalIgnoreCase) ||
                targetTable.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase),
                $"Forbidden cross-schema physical foreign key detected in migration script: '{targetTable}'.");
        }
    }

    #endregion

    #region 3. Domain Events Reside in Contracts and Implement IDomainEvent (ADR-003)

    [Theory]
    [InlineData("TeacherProfileCreatedEvent")]
    [InlineData("TeacherProfileApprovedEvent")]
    [InlineData("TeacherStandingChangedEvent")]
    [InlineData("QualificationEvidenceVerifiedEvent")]
    [InlineData("TeachingAuthorizationGrantedEvent")]
    [InlineData("TeachingAuthorizationStatusChangedEvent")]
    [InlineData("TeachingRecordCorrectedEvent")]
    public void TeachingDomainEvents_Should_ImplementIDomainEvent_AndResideInContractsTeachingEvents(string eventName)
    {
        // Arrange
        const string expectedNamespace = "Elsasa.Sis.Contracts.Teaching.Events";
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
    public void AllTeachingDomainEvents_InContracts_Should_ImplementIDomainEvent_AndEndWithEvent()
    {
        // Act
        var eventTypes = GetAllTypes(_contractsAssembly)
            .Where(t => t.Namespace == "Elsasa.Sis.Contracts.Teaching.Events" && !t.IsNested)
            .ToArray();

        // Assert
        Assert.NotEmpty(eventTypes);
        foreach (var eventType in eventTypes)
        {
            Assert.True(
                eventType.Name.EndsWith("Event", StringComparison.Ordinal),
                $"Domain event '{eventType.FullName}' must have name ending with 'Event'.");

            Assert.True(
                typeof(IDomainEvent).IsAssignableFrom(eventType),
                $"Domain event '{eventType.FullName}' must implement IDomainEvent.");

            Assert.True(
                eventType.IsSealed,
                $"Domain event '{eventType.FullName}' must be sealed.");
        }
    }

    [Fact]
    public void TeachingAssembly_Should_NotContainDomainEventDefinitions()
    {
        // ADR-001, ADR-003: Domain events are shared contract records and must live in Elsasa.Sis.Contracts,
        // never inside domain implementation assemblies.
        var eventTypesInTeaching = GetAllTypes(_teachingAssembly)
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t))
            .ToArray();

        Assert.Empty(eventTypesInTeaching);
    }

    #endregion

    #region 4. Vertical Slice Conventions for Handlers and Endpoints (ADR-004, ADR-005)

    [Fact]
    public void TeachingHandlers_Should_FollowVerticalSliceConventions()
    {
        // Act
        var handlerTypes = GetAllTypes(_teachingAssembly)
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();

        // Assert
        Assert.NotEmpty(handlerTypes);

        foreach (var handler in handlerTypes)
        {
            Assert.True(
                handler.IsSealed,
                $"Handler class '{handler.FullName}' must be sealed.");

            Assert.True(
                handler.Namespace is not null && Regex.IsMatch(handler.Namespace, @"^Elsasa\.Sis\.Teaching\.Features\..+"),
                $"Handler '{handler.FullName}' must reside within a feature slice namespace under 'Elsasa.Sis.Teaching.Features'.");

            var handleAsyncMethod = handler.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(handleAsyncMethod);
            Assert.True(
                typeof(Task).IsAssignableFrom(handleAsyncMethod.ReturnType),
                $"Handler '{handler.FullName}.HandleAsync' must return a Task.");
        }
    }

    [Fact]
    public void TeachingEndpoints_Should_ImplementIEndpoint_BeSealed_AndResideInFeatureSlices()
    {
        // Act: All feature endpoint classes (excluding the static module router class)
        var endpointTypes = GetAllTypes(_teachingAssembly)
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t))
            .ToArray();

        // Assert
        Assert.NotEmpty(endpointTypes);

        foreach (var endpoint in endpointTypes)
        {
            Assert.True(
                endpoint.IsSealed,
                $"Endpoint class '{endpoint.FullName}' must be sealed.");

            Assert.True(
                endpoint.Name.EndsWith("Endpoint", StringComparison.Ordinal),
                $"Endpoint class '{endpoint.FullName}' must have name ending with 'Endpoint'.");

            Assert.True(
                endpoint.Namespace is not null && Regex.IsMatch(endpoint.Namespace, @"^Elsasa\.Sis\.Teaching\.Features\..+"),
                $"Endpoint '{endpoint.FullName}' must reside within a feature slice namespace under 'Elsasa.Sis.Teaching.Features'.");
        }
    }

    [Theory]
    [InlineData("RegisterTeacherProfileHandler")]
    [InlineData("UpdateTeacherProfileHandler")]
    [InlineData("ApproveTeacherProfileHandler")]
    [InlineData("ChangeTeacherStandingHandler")]
    [InlineData("GetTeacherProfileByIdHandler")]
    [InlineData("GetTeacherProfilesHandler")]
    [InlineData("GetTeachingRolesHandler")]
    [InlineData("AddTeachingCapabilityHandler")]
    [InlineData("RemoveTeachingCapabilityHandler")]
    [InlineData("GetCapabilityTypesHandler")]
    [InlineData("SubmitQualificationEvidenceHandler")]
    [InlineData("VerifyQualificationEvidenceHandler")]
    [InlineData("RejectQualificationEvidenceHandler")]
    [InlineData("GetQualificationTypesHandler")]
    [InlineData("GrantTeachingAuthorizationHandler")]
    [InlineData("UpdateTeachingAuthorizationStatusHandler")]
    [InlineData("AddSupervisionRequirementHandler")]
    [InlineData("ResolveSupervisionRequirementHandler")]
    [InlineData("ApplyRecordCorrectionHandler")]
    [InlineData("ApproveRecordCorrectionHandler")]
    [InlineData("GetRecordCorrectionsHandler")]
    public void TeachingFeatureHandlers_Should_FollowVerticalSliceConventions(string handlerName)
    {
        var handlerType = GetAllTypes(_teachingAssembly)
            .FirstOrDefault(t => t.Name == handlerName);

        Assert.NotNull(handlerType);
        Assert.True(handlerType.IsSealed, $"Handler '{handlerName}' must be sealed.");
        Assert.True(
            handlerType.Namespace is not null && handlerType.Namespace.StartsWith("Elsasa.Sis.Teaching.Features.", StringComparison.Ordinal),
            $"Handler '{handlerName}' must reside in a Feature slice namespace.");

        var handleAsyncMethod = handlerType.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(handleAsyncMethod);
        Assert.True(
            typeof(Task).IsAssignableFrom(handleAsyncMethod.ReturnType),
            $"Handler '{handlerName}.HandleAsync' must return a Task.");
    }

    #endregion

    #region 5. MediatR Usage Restricted Strictly to Domain Events (ADR-004, ADR-005)

    [Fact]
    public void TeachingAssembly_Should_NotUseMediatRForRequestsOrInternalHandlers()
    {
        // ADR-004, ADR-005: Commands and queries execute directly via Dapper and Native Minimal APIs.
        // MediatR requests (IRequest, IRequest<T>) and handlers (IRequestHandler<...>) are prohibited.
        var types = GetAllTypes(_teachingAssembly);

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
    public void TeachingHandlers_Should_NotInjectISenderForRequestDispatch()
    {
        // ADR-004, ADR-005: Feature handlers must NOT inject MediatR ISender for internal dispatch.
        // Only IPublisher (or IMediator solely for domain event publishing) is allowed.
        var handlerTypes = GetAllTypes(_teachingAssembly)
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Handler", StringComparison.Ordinal))
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

    #region 6. Cross-Domain Communication & ITeachingModule Rules (ADR-010)

    [Fact]
    public void ITeachingModule_Should_ResideInContractsTeachingNamespace()
    {
        // ADR-010: Synchronous inquiry interface must reside in Elsasa.Sis.Contracts.Teaching
        var moduleInterface = typeof(ITeachingModule);
        Assert.Equal("Elsasa.Sis.Contracts.Teaching", moduleInterface.Namespace);
    }

    [Fact]
    public void ITeachingModule_Should_ContainOnlyInquiryMethods_ReturningTaskResult()
    {
        // ADR-010: Synchronous cross-domain calls must be strictly inquiry-only (read operations).
        // Command/write operations cross boundaries asynchronously via domain events.
        var interfaceType = typeof(ITeachingModule);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(methods);

        var forbiddenVerbs = new[]
        {
            "Create", "Add", "Insert", "Update", "Modify", "Delete",
            "Remove", "Change", "Apply", "Approve", "Reject", "Grant",
            "Revoke", "Set", "Save", "Register", "Post", "Put", "Patch"
        };

        foreach (var method in methods)
        {
            // Method name must not begin with mutating/write verbs
            foreach (var verb in forbiddenVerbs)
            {
                Assert.False(
                    method.Name.StartsWith(verb, StringComparison.OrdinalIgnoreCase),
                    $"ITeachingModule must not contain write/mutating method: '{method.Name}'. Cross-domain contracts must be read/inquiry only (ADR-010).");
            }

            // Return type must be Task<Result<...>>
            Assert.True(
                typeof(Task).IsAssignableFrom(method.ReturnType),
                $"ITeachingModule method '{method.Name}' must return a Task.");

            Assert.True(
                method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                $"ITeachingModule method '{method.Name}' must return a generic Task<T>.");

            var taskTypeArgument = method.ReturnType.GetGenericArguments()[0];
            Assert.True(
                taskTypeArgument.Name.StartsWith("Result", StringComparison.Ordinal),
                $"ITeachingModule method '{method.Name}' must return Task<Result<...>>, but returns Task<{taskTypeArgument.Name}>.");
        }
    }

    [Fact]
    public void TeachingModuleImpl_Should_ImplementITeachingModule_BeSealed_AndResideInServices()
    {
        var implType = _teachingAssembly.GetType("Elsasa.Sis.Teaching.Services.TeachingModuleImpl");
        Assert.NotNull(implType);
        Assert.True(implType.IsSealed, "TeachingModuleImpl must be sealed.");
        Assert.True(typeof(ITeachingModule).IsAssignableFrom(implType), "TeachingModuleImpl must implement ITeachingModule.");
        Assert.Equal("Elsasa.Sis.Teaching.Services", implType.Namespace);
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
}
