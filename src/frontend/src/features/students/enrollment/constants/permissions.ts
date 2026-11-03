/**
 * Authoritative client permission constants for the Students Enrollment module (ADR-009).
 * Mirrors backend permission constants defined in Elsasa.Sis.Students.Permissions.Students.Enrollment.
 */
export const ENROLLMENT_PERMISSIONS = {
  EXECUTE: 'Permissions.Students.Enrollment.Execute',
} as const;

export type EnrollmentPermission =
  (typeof ENROLLMENT_PERMISSIONS)[keyof typeof ENROLLMENT_PERMISSIONS];
