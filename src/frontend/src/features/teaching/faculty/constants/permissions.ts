/**
 * Authoritative permission constants for the Teaching domain (ADR-009, P8-S23).
 * Mirrors Elsasa.Sis.Teaching.Permissions on backend.
 */
export const TEACHING_PERMISSIONS = {
  VIEW: 'Permissions.Teaching.Faculty.View',
  MANAGE: 'Permissions.Teaching.Faculty.Manage',
  VERIFY: 'Permissions.Teaching.Faculty.Verify',
  AUTHORIZE: 'Permissions.Teaching.Faculty.Authorize',
  CORRECT: 'Permissions.Teaching.Faculty.Correct',
} as const;

export type TeachingPermission =
  (typeof TEACHING_PERMISSIONS)[keyof typeof TEACHING_PERMISSIONS];
