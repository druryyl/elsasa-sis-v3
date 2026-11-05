namespace Elsasa.Sis.IntegrationTests.UnitTests.Fixtures;

using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

public sealed class TestStudentsModuleTests
{
    [Fact]
    public async Task IsStudentEnrolledAsync_Should_ReturnTrueByDefault_When_AlwaysEnrolledIsTrue()
    {
        // Arrange
        var module = new TestStudentsModule();
        var studentId = Guid.NewGuid();

        // Act
        var result = await module.IsStudentEnrolledAsync(studentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsStudentEnrolledAsync_Should_RespectExplicitEnrolledAndUnenrolledMarks()
    {
        // Arrange
        var module = new TestStudentsModule();
        var enrolledId = Guid.NewGuid();
        var unenrolledId = Guid.NewGuid();

        module.MarkStudentEnrolled(enrolledId);
        module.MarkStudentUnenrolled(unenrolledId);

        // Act & Assert
        var r1 = await module.IsStudentEnrolledAsync(enrolledId);
        r1.IsSuccess.Should().BeTrue();
        r1.Value.Should().BeTrue();

        var r2 = await module.IsStudentEnrolledAsync(unenrolledId);
        r2.IsSuccess.Should().BeTrue();
        r2.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Reset_Should_RestoreDefaultAlwaysEnrolledBehavior()
    {
        // Arrange
        var module = new TestStudentsModule();
        var studentId = Guid.NewGuid();
        module.MarkStudentUnenrolled(studentId);

        // Act
        module.Reset();
        var result = await module.IsStudentEnrolledAsync(studentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
