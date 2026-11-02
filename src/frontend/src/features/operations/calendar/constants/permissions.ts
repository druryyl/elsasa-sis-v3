/**
 * Authoritative client permission constants for the Operations Calendar domain (ADR-009).
 * Mirrors backend permission constants defined in Elsasa.Sis.Operations.Permissions.
 */
export const CALENDAR_PERMISSIONS = {
  VIEW: 'Permissions.Operations.Calendar.View',
  MANAGE: 'Permissions.Operations.Calendar.Manage',
  APPROVE: 'Permissions.Operations.Calendar.Approve',
  PUBLISH: 'Permissions.Operations.Calendar.Publish',
  DECLARE_CLOSURE: 'Permissions.Operations.Closure.Declare',
  CORRECT_RECORD: 'Permissions.Operations.Records.Correct',
} as const;
