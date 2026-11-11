namespace Elsasa.Sis.Teaching.Features.Workload.ApproveWorkloadException;

/// <summary>
/// Request payload to approve a workload exception (TD-04, P3-S10).
/// </summary>
/// <param name="DecisionRemarks">Optional commentary or conditions accompanying approval from the Teaching Authority.</param>
public sealed record ApproveWorkloadExceptionRequest(
    string? DecisionRemarks = null);
