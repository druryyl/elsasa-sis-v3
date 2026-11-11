namespace Elsasa.Sis.Teaching.Features.TeachingDemands.CreateTeachingDemand;

using FluentValidation;

/// <summary>
/// FluentValidation validator enforcing demand intake invariants (TD-02, P2-S04).
/// </summary>
public sealed class CreateTeachingDemandValidator : AbstractValidator<CreateTeachingDemandRequest>
{
    private const string _demandCodePattern = @"^[A-Za-z0-9\-_]{3,50}$";

    public CreateTeachingDemandValidator()
    {
        RuleFor(x => x.SourceDomain)
            .NotEmpty().WithMessage("Source domain is required.")
            .MaximumLength(50).WithMessage("Source domain cannot exceed 50 characters.");

        RuleFor(x => x.SourceWorkReference)
            .NotEmpty().WithMessage("Source work reference is required.");

        RuleFor(x => x.SourceWorkLabel)
            .NotEmpty().WithMessage("Source work label is required.")
            .MaximumLength(255).WithMessage("Source work label cannot exceed 255 characters.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.RequiredCapabilityCode)
            .NotEmpty().WithMessage("Required capability code is required.")
            .MaximumLength(100).WithMessage("Required capability code cannot exceed 100 characters.");

        RuleFor(x => x.RequiredAssignmentRoleId)
            .NotEmpty().WithMessage("Required assignment role ID is required.");

        RuleFor(x => x.ExpectedWorkloadHours)
            .GreaterThan(0).WithMessage("Expected workload hours must be greater than zero.")
            .LessThanOrEqualTo(999.99m).WithMessage("Expected workload hours cannot exceed 999.99.");

        RuleFor(x => x.RequestedStartDate)
            .NotEmpty().WithMessage("Requested start date is required.")
            .Must(d => d != default).WithMessage("Requested start date must be a valid date.");

        RuleFor(x => x.RequestedEndDate)
            .NotEmpty().WithMessage("Requested end date is required.")
            .Must(d => d != default).WithMessage("Requested end date must be a valid date.")
            .GreaterThanOrEqualTo(x => x.RequestedStartDate)
            .WithMessage("Requested end date must be on or after requested start date.");

        RuleFor(x => x.RequiredTeachersCount)
            .GreaterThan(0).WithMessage("Required teachers count must be at least 1.")
            .LessThanOrEqualTo(50).WithMessage("Required teachers count cannot exceed 50.");

        RuleFor(x => x.CampusScope)
            .MaximumLength(100).WithMessage("Campus scope cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CampusScope));

        RuleFor(x => x.WorkloadUnit)
            .MaximumLength(50).WithMessage("Workload unit cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.WorkloadUnit));

        RuleFor(x => x.DemandCode)
            .MaximumLength(50).WithMessage("Demand code cannot exceed 50 characters.")
            .Matches(_demandCodePattern).WithMessage("Demand code format is invalid. Must contain 3 to 50 alphanumeric characters, hyphens, or underscores.")
            .When(x => !string.IsNullOrWhiteSpace(x.DemandCode));
    }
}
