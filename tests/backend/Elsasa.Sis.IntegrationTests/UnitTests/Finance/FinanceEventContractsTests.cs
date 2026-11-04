namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Contracts.Students.Events;

public sealed class FinanceEventContractsTests
{
    [Fact]
    public void ObligationAssessedEvent_PrimaryConstructor_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var obligationId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var amount = 750000L;
        var occurredOn = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new ObligationAssessedEvent(obligationId, studentId, amount, occurredOn);

        // Assert
        Assert.Equal(obligationId, domainEvent.ObligationId);
        Assert.Equal(studentId, domainEvent.StudentId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(occurredOn, domainEvent.OccurredOn);
    }

    [Fact]
    public void ObligationAssessedEvent_ConvenienceConstructor_WithLong_ShouldInitializeUtcOccurredOn()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var obligationId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var amount = 15000000L;

        // Act
        var domainEvent = new ObligationAssessedEvent(obligationId, studentId, amount);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(obligationId, domainEvent.ObligationId);
        Assert.Equal(studentId, domainEvent.StudentId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredOn.Offset);
        Assert.InRange(domainEvent.OccurredOn, before, after);
    }

    [Fact]
    public void ObligationAssessedEvent_DecimalConstructor_ShouldCastToLongAndInitializeUtcOccurredOn()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var obligationId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var decimalAmount = 250000.75m;

        // Act
        var domainEvent = new ObligationAssessedEvent(obligationId, studentId, decimalAmount);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(obligationId, domainEvent.ObligationId);
        Assert.Equal(studentId, domainEvent.StudentId);
        Assert.Equal(250000L, domainEvent.Amount);
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredOn.Offset);
        Assert.InRange(domainEvent.OccurredOn, before, after);
    }

    [Fact]
    public void StudentEnrolledEvent_PrimaryConstructor_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new StudentEnrolledEvent(studentId, applicantId, academicYearId, occurredOn);

        // Assert
        Assert.Equal(studentId, domainEvent.StudentId);
        Assert.Equal(applicantId, domainEvent.ApplicantId);
        Assert.Equal(academicYearId, domainEvent.AcademicYearId);
        Assert.Equal(occurredOn, domainEvent.OccurredOn);
    }

    [Fact]
    public void StudentEnrolledEvent_ThreeArgConstructor_ShouldInitializeUtcOccurredOn()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var studentId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        // Act
        var domainEvent = new StudentEnrolledEvent(studentId, applicantId, academicYearId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(studentId, domainEvent.StudentId);
        Assert.Equal(applicantId, domainEvent.ApplicantId);
        Assert.Equal(academicYearId, domainEvent.AcademicYearId);
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredOn.Offset);
        Assert.InRange(domainEvent.OccurredOn, before, after);
    }

    [Fact]
    public void StudentEnrolledEvent_TwoArgConstructor_ShouldDefaultApplicantIdAndInitializeUtcOccurredOn()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var studentId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        // Act
        var domainEvent = new StudentEnrolledEvent(studentId, academicYearId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(studentId, domainEvent.StudentId);
        Assert.Equal(Guid.Empty, domainEvent.ApplicantId);
        Assert.Equal(academicYearId, domainEvent.AcademicYearId);
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredOn.Offset);
        Assert.InRange(domainEvent.OccurredOn, before, after);
    }
}
