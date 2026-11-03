/**
 * Authoritative client permission constants for the Students Records module (ADR-009).
 * Mirrors backend permission constants defined in Elsasa.Sis.Students.Permissions.Students.Records.
 */
export const RECORDS_PERMISSIONS = {
  VIEW: 'Permissions.Students.Records.View',
  EDIT: 'Permissions.Students.Records.Edit',
} as const;

export type RecordsPermission =
  (typeof RECORDS_PERMISSIONS)[keyof typeof RECORDS_PERMISSIONS];
