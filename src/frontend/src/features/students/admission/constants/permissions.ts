/**
 * Authoritative client permission constants for the Students Admission module (ADR-009).
 * Mirrors backend permission constants defined in Elsasa.Sis.Students.Permissions.Students.Admission.
 */
export const ADMISSION_PERMISSIONS = {
  VIEW: 'Permissions.Students.Admission.View',
  REGISTER: 'Permissions.Students.Admission.Register',
  REVIEW: 'Permissions.Students.Admission.Review',
  VERIFY_DOCUMENT: 'Permissions.Students.Admission.VerifyDocument',
  DECIDE: 'Permissions.Students.Admission.Decide',
} as const;

export type AdmissionPermission =
  (typeof ADMISSION_PERMISSIONS)[keyof typeof ADMISSION_PERMISSIONS];
