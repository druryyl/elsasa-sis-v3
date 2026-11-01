namespace Elsasa.Sis.IntegrationTests.UnitTests.Contracts;

using System.Reflection;
using Elsasa.Sis.Contracts.Common;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Shared.Common;
using MediatR;

public sealed class ContractTests
{
    [Fact]
    public void IDomainEvent_Should_ExtendMediatRINotification_And_ExposeOccurredOn()
    {
        // Assert
        Assert.True(typeof(INotification).IsAssignableFrom(typeof(IDomainEvent)));
        var prop = typeof(IDomainEvent).GetProperty(nameof(IDomainEvent.OccurredOn));
        Assert.NotNull(prop);
        Assert.Equal(typeof(DateTimeOffset), prop.PropertyType);
    }

    [Theory]
    [InlineData(typeof(ApplicantAcceptedEvent))]
    [InlineData(typeof(StudentEnrolledEvent))]
    [InlineData(typeof(ObligationAssessedEvent))]
    [InlineData(typeof(PaymentConfirmedEvent))]
    public void DomainEvents_Should_BeSealedRecordsImplementingIDomainEvent(Type eventType)
    {
        // Assert
        Assert.True(eventType.IsSealed, $"{eventType.Name} must be sealed.");
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(eventType), $"{eventType.Name} must implement IDomainEvent.");
        Assert.True(typeof(INotification).IsAssignableFrom(eventType), $"{eventType.Name} must implement MediatR.INotification.");

        // Record verification via synthesized EqualityContract property
        var equalityContract = eventType.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void ApplicantAcceptedEvent_Should_InitializeCorrectly()
    {
        // Arrange
        var applicantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act - secondary constructor
        var @event = new ApplicantAcceptedEvent(applicantId, applicationId, academicYearId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(applicantId, @event.ApplicantId);
        Assert.Equal(applicationId, @event.ApplicationId);
        Assert.Equal(academicYearId, @event.AcademicYearId);
        Assert.InRange(@event.OccurredOn, before, after);

        // Act - primary constructor
        var specificDate = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var explicitEvent = new ApplicantAcceptedEvent(applicantId, applicationId, academicYearId, specificDate);
        Assert.Equal(specificDate, explicitEvent.OccurredOn);
    }

    [Fact]
    public void StudentEnrolledEvent_Should_InitializeCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act - secondary constructor
        var @event = new StudentEnrolledEvent(studentId, applicantId, academicYearId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(studentId, @event.StudentId);
        Assert.Equal(applicantId, @event.ApplicantId);
        Assert.Equal(academicYearId, @event.AcademicYearId);
        Assert.InRange(@event.OccurredOn, before, after);

        // Act - two-arg constructor
        var twoArg = new StudentEnrolledEvent(studentId, academicYearId);
        Assert.Equal(studentId, twoArg.StudentId);
        Assert.Equal(academicYearId, twoArg.AcademicYearId);
    }

    [Fact]
    public void ObligationAssessedEvent_Should_InitializeCorrectly()
    {
        // Arrange
        var obligationId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var amount = 1500000m;
        var before = DateTimeOffset.UtcNow;

        // Act
        var @event = new ObligationAssessedEvent(obligationId, studentId, amount);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(obligationId, @event.ObligationId);
        Assert.Equal(studentId, @event.StudentId);
        Assert.Equal(amount, @event.Amount);
        Assert.InRange(@event.OccurredOn, before, after);
    }

    [Fact]
    public void PaymentConfirmedEvent_Should_InitializeCorrectly()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var amount = 500000m;
        var before = DateTimeOffset.UtcNow;

        // Act
        var @event = new PaymentConfirmedEvent(paymentId, studentId, amount);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(paymentId, @event.PaymentId);
        Assert.Equal(studentId, @event.StudentId);
        Assert.Equal(amount, @event.Amount);
        Assert.InRange(@event.OccurredOn, before, after);
    }

    [Fact]
    public void OperationsModule_Should_DeclareRequiredSignatures()
    {
        // Assert
        var iface = typeof(IOperationsModule);
        Assert.True(iface.IsInterface);

        var isActiveMethod = iface.GetMethod(nameof(IOperationsModule.IsActiveAcademicPeriodAsync));
        Assert.NotNull(isActiveMethod);
        Assert.Equal(typeof(Task<Result<bool>>), isActiveMethod.ReturnType);
        var isActiveParams = isActiveMethod.GetParameters();
        Assert.Equal(2, isActiveParams.Length);
        Assert.Equal(typeof(Guid), isActiveParams[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), isActiveParams[1].ParameterType);

        var getYearMethod = iface.GetMethod(nameof(IOperationsModule.GetActiveAcademicYearIdAsync));
        Assert.NotNull(getYearMethod);
        Assert.Equal(typeof(Task<Result<Guid?>>), getYearMethod.ReturnType);
        var getYearParams = getYearMethod.GetParameters();
        Assert.Single(getYearParams);
        Assert.Equal(typeof(CancellationToken), getYearParams[0].ParameterType);
    }

    [Fact]
    public void StudentsModule_Should_DeclareRequiredSignatures()
    {
        // Assert
        var iface = typeof(IStudentsModule);
        Assert.True(iface.IsInterface);

        var isEnrolledMethod = iface.GetMethod(nameof(IStudentsModule.IsStudentEnrolledAsync));
        Assert.NotNull(isEnrolledMethod);
        Assert.Equal(typeof(Task<Result<bool>>), isEnrolledMethod.ReturnType);
        var isEnrolledParams = isEnrolledMethod.GetParameters();
        Assert.Equal(2, isEnrolledParams.Length);
        Assert.Equal(typeof(Guid), isEnrolledParams[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), isEnrolledParams[1].ParameterType);
    }

    [Fact]
    public void FinanceModule_Should_DeclareRequiredSignatures()
    {
        // Assert
        var iface = typeof(IFinanceModule);
        Assert.True(iface.IsInterface);

        var isFeePaidMethod = iface.GetMethod(nameof(IFinanceModule.IsEnrollmentFeePaidAsync));
        Assert.NotNull(isFeePaidMethod);
        Assert.Equal(typeof(Task<Result<bool>>), isFeePaidMethod.ReturnType);
        var isFeePaidParams = isFeePaidMethod.GetParameters();
        Assert.Equal(3, isFeePaidParams.Length);
        Assert.Equal(typeof(Guid), isFeePaidParams[0].ParameterType);
        Assert.Equal(typeof(Guid), isFeePaidParams[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), isFeePaidParams[2].ParameterType);
    }
}
