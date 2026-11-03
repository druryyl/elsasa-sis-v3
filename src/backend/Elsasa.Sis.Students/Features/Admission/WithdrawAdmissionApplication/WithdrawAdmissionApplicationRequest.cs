namespace Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;

/// <summary>
/// Request payload for withdrawing an admission application (ADR-005).
/// </summary>
public sealed record WithdrawAdmissionApplicationRequest(
    string WithdrawalReason);
