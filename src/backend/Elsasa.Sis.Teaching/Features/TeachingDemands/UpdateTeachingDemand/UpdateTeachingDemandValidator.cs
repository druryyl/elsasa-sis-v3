namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;

using FluentValidation;

/// <summary>
/// FluentValidation validator enforcing demand modification invariants (TD-02, P2-S04).
/// </summary>
public sealed class UpdateTeachingDemandValidator : AbstractValidator<UpdateTeachingDemandRequest>
{
    public UpdateTeachingDemandValidator()
    {
        RuleFor(x => x.SourceWorkLabel)
            .NotEmpty().WithMessage("Source work label is required.")
            .MaximumLength(255).WithMessage("Source work label cannot exceed 255 characters.");

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
    }
}
