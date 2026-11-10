namespace Elsasa.Sis.IntegrationTests.UnitTests.Teaching;

using System.Reflection;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Teaching;
using Elsasa.Sis.Teaching.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class TeachingModuleUnitTests
{
    private sealed class FaultyConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<System.Data.Common.DbConnection> CreateConnectionAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated connection failure.");
        }
    }

    [Fact]
    public void AddTeachingModule_Should_RegisterITeachingModule()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new FaultyConnectionFactory());

        // Act
        services.AddTeachingModule();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var teachingModule = scope.ServiceProvider.GetService<ITeachingModule>();

        // Assert
        teachingModule.Should().NotBeNull();
        teachingModule.Should().BeOfType<TeachingModuleImpl>();
    }

    [Fact]
    public void TeachingModule_Should_ExposeStrictlyReadOnlyMethods_ZeroCrossDomainWriteMethods()
    {
        // Arrange
        var moduleType = typeof(TeachingModuleImpl);
        var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert - Only the 3 synchronous contract read methods
        methods.Should().OnlyContain(m =>
            m.Name == nameof(ITeachingModule.GetTeacherProfileAsync) ||
            m.Name == nameof(ITeachingModule.IsTeacherAuthorizedAsync) ||
            m.Name == nameof(ITeachingModule.GetActiveTeachersAsync));
    }

    [Fact]
    public async Task GetTeacherProfileAsync_Should_ReturnSuccessNull_When_TeacherIdIsEmpty()
    {
        // Arrange
        var module = new TeachingModuleImpl(new FaultyConnectionFactory());

        // Act
        var result = await module.GetTeacherProfileAsync(Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetTeacherProfileAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new TeachingModuleImpl(new FaultyConnectionFactory());

        // Act
        var result = await module.GetTeacherProfileAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Teaching.TeacherProfileQueryFailed");
    }

    [Fact]
    public async Task IsTeacherAuthorizedAsync_Should_ReturnSuccessFalse_When_TeacherIdIsEmpty()
    {
        // Arrange
        var module = new TeachingModuleImpl(new FaultyConnectionFactory());

        // Act
        var result = await module.IsTeacherAuthorizedAsync(Guid.Empty, "MATH_PRIMARY", "Kampus Utama", new DateOnly(2026, 8, 1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task IsTeacherAuthorizedAsync_Should_ReturnSuccessFalse_When_SubjectOrCapabilityIsBlank(string? subjectOrCapability)
    {
        // Arrange
        var module = new TeachingModuleImpl(new FaultyConnectionFactory());

        // Act
        var result = await module.IsTeacherAuthorizedAsync(Guid.NewGuid(), subjectOrCapability!, "Kampus Utama", new DateOnly(2026, 8, 1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsTeacherAuthorizedAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new TeachingModuleImpl(new FaultyConnectionFactory());

        // Act
        var result = await module.IsTeacherAuthorizedAsync(Guid.NewGuid(), "MATH_PRIMARY", "Kampus Utama", new DateOnly(2026, 8, 1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Teaching.AuthorizationCheckFailed");
    }

    [Fact]
    public async Task GetActiveTeachersAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new TeachingModuleImpl(new FaultyConnectionFactory());

        // Act
        var result = await module.GetActiveTeachersAsync("Kampus Utama");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Teaching.ActiveTeachersQueryFailed");
    }
}
