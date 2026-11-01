using System.Reflection;
using NetArchTest.Rules;

namespace Elsasa.Sis.ArchitectureTests;

/// <summary>
/// Enforces the slice naming conventions across the Stage 1 domain assemblies.
/// </summary>
/// <remarks>
/// The first handler, request, response and validator are introduced by the
/// P7-S12 vertical slice. Until then no type matches the candidate filters, so
/// each rule passes vacuously. The candidate sets are enumerated explicitly and
/// the assertions are guarded when a set is empty: NetArchTest and xUnit both
/// behave unexpectedly on empty type sets, and adding a fake handler purely to
/// make these tests non-vacuous would pollute the production assemblies.
/// </remarks>
public sealed class NamingConventionTests
{
    // FluentValidation is intentionally not a package reference of this test
    // project. The validator base type is detected by its full name, so this
    // rule compiles today and becomes meaningful as soon as P2-S05 adds
    // FluentValidation to Shared and it flows transitively to the domains.
    private const string _abstractValidatorTypeName = "FluentValidation.AbstractValidator`1";

    private const string _requestHandlerInterfacePrefix = "MediatR.IRequestHandler`";
    private const string _streamRequestHandlerInterfacePrefix = "MediatR.IStreamRequestHandler`";
    private const string _notificationHandlerInterfaceName = "MediatR.INotificationHandler`1";

    private const string _requestInterfaceName = "MediatR.IRequest";
    private const string _requestInterfacePrefix = "MediatR.IRequest`";

    [Fact]
    public void HandlerClasses_Should_BeSealed_When_NamedWithHandlerSuffix()
    {
        // NetArchTest rule: all classes ending with 'Handler' across domain assemblies must be sealed.
        foreach (var assemblyName in TestAssemblies.DomainAssemblyNames)
        {
            var assembly = TestAssemblies.Get(assemblyName);
            var handlerTypes = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Handler")
                .GetTypes()
                .ToArray();

            if (handlerTypes.Length == 0)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Handler")
                .Should()
                .BeSealed()
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"Handler classes in {assemblyName} must be sealed. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Handlers_Should_BeSealedAndSuffixedHandler_When_MediatRHandlerInterfaceIsImplemented()
    {
        // Arrange
        var handlers = DomainTypes().Where(IsMediatRHandler).ToArray();

        // Act
        var invalidHandlers = handlers
            .Where(handler => !handler.IsSealed || !handler.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();

        // Assert
        // Guard: no handlers exist until P7-S12 introduces the first slice.
        if (handlers.Length == 0)
        {
            return;
        }

        Assert.Empty(invalidHandlers);
    }

    [Fact]
    public void FeatureHandlers_Should_AdhereToNamingAndBeSealed_When_DeclaringHandleAsync()
    {
        // Arrange (ADR-005 direct slice handlers)
        var handlers = DomainTypes()
            .Where(type => type.IsClass && !type.IsAbstract && HasHandleAsyncMethod(type))
            .ToArray();

        // Act
        var invalidHandlers = handlers
            .Where(handler => !handler.IsSealed || !handler.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();

        // Assert
        if (handlers.Length == 0)
        {
            return;
        }

        Assert.Empty(invalidHandlers);
    }

    [Fact]
    public void RequestTypes_Should_BeSuffixedRequest_When_MediatRRequestInterfaceIsImplemented()
    {
        // Arrange
        var requestTypes = DomainTypes().Where(IsMediatRRequest).ToArray();

        // Act
        var invalidRequests = requestTypes
            .Where(request => !request.Name.EndsWith("Request", StringComparison.Ordinal))
            .ToArray();

        // Assert
        // Guard: no requests exist until P7-S12 introduces the first slice.
        if (requestTypes.Length == 0)
        {
            return;
        }

        Assert.Empty(invalidRequests);
    }

    [Fact]
    public void RequestAndResponseTypes_Should_BeSealedRecords_When_TypeNameUsesRequestOrResponseSuffix()
    {
        // Arrange
        var payloadTypes = DomainTypes()
            .Where(type => type.Name.EndsWith("Request", StringComparison.Ordinal)
                || type.Name.EndsWith("Response", StringComparison.Ordinal))
            .ToArray();

        // Act
        var nonRecordOrUnsealedPayloadTypes = payloadTypes
            .Where(type => !IsRecord(type) || !type.IsSealed)
            .ToArray();

        // Assert
        // Guard: request/response records arrive with P7-S12.
        if (payloadTypes.Length == 0)
        {
            return;
        }

        Assert.Empty(nonRecordOrUnsealedPayloadTypes);
    }

    [Fact]
    public void Validators_Should_BeSealedAndSuffixedValidator_When_AbstractValidatorIsDerived()
    {
        // Arrange
        var validators = DomainTypes().Where(IsFluentValidationValidator).ToArray();

        // Act
        var invalidValidators = validators
            .Where(validator => !validator.IsSealed || !validator.Name.EndsWith("Validator", StringComparison.Ordinal))
            .ToArray();

        // Assert
        // Guard: no validators exist until P7-S12 introduces the first slice.
        if (validators.Length == 0)
        {
            return;
        }

        Assert.Empty(invalidValidators);
    }

    [Fact]
    public void ValidatorClasses_Should_ExtendAbstractValidator_When_NamedWithValidatorSuffix()
    {
        // Arrange
        var validatorNamedClasses = DomainTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Validator", StringComparison.Ordinal))
            .ToArray();

        // Act
        var invalidValidators = validatorNamedClasses
            .Where(validator => !IsFluentValidationValidator(validator))
            .ToArray();

        // Assert
        if (validatorNamedClasses.Length == 0)
        {
            return;
        }

        Assert.Empty(invalidValidators);
    }

    private static IEnumerable<Type> DomainTypes() =>
        TestAssemblies.DomainAssemblyNames.SelectMany(name => GetAllTypes(TestAssemblies.Get(name)));

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

    private static bool IsMediatRHandler(Type type) =>
        type.GetInterfaces().Any(IsMediatRHandlerInterface);

    private static bool IsMediatRHandlerInterface(Type type) =>
        type.FullName is not null
        && (type.FullName.StartsWith(_requestHandlerInterfacePrefix, StringComparison.Ordinal)
            || type.FullName.StartsWith(_streamRequestHandlerInterfacePrefix, StringComparison.Ordinal)
            || string.Equals(type.FullName, _notificationHandlerInterfaceName, StringComparison.Ordinal));

    private static bool HasHandleAsyncMethod(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => string.Equals(m.Name, "HandleAsync", StringComparison.Ordinal));

    private static bool IsMediatRRequest(Type type) =>
        type.GetInterfaces().Any(
            candidate => candidate.FullName is not null
                && (string.Equals(candidate.FullName, _requestInterfaceName, StringComparison.Ordinal)
                    || candidate.FullName.StartsWith(_requestInterfacePrefix, StringComparison.Ordinal)));

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

    /// <summary>
    /// Record classes synthesise a protected <c>EqualityContract</c> property;
    /// regular classes do not.
    /// </summary>
    private static bool IsRecord(Type type) =>
        type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic) is not null;
}
