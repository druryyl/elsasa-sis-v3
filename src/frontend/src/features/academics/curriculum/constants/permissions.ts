/**
 * Authoritative client permission constants for the Academics domain (ADR-009, TD-07).
 * Mirrors backend permission constants defined in Elsasa.Sis.Academics.Permissions.
 */
export const CURRICULUM_PERMISSIONS = {
  VIEW: 'academics.curriculum.view',
  MANAGE: 'academics.curriculum.manage',
  SUBMIT: 'academics.curriculum.submit',
  APPROVE: 'academics.curriculum.approve',
  ARCHIVE: 'academics.curriculum.archive',
} as const;
