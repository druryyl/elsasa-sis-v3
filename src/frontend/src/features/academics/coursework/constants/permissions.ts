/**
 * Authoritative client permission constants for Coursework & Submissions (ADR-009, TD-07).
 * Mirrors backend permission constants defined in Elsasa.Sis.Academics.Permissions.
 */
export const COURSEWORK_PERMISSIONS = {
  ASSIGNMENTS_VIEW: 'academics.assignments.view',
  ASSIGNMENTS_MANAGE: 'academics.assignments.manage',
  ASSIGNMENTS_RELEASE: 'academics.assignments.release',
  ASSIGNMENTS_CLOSE: 'academics.assignments.close',
  SUBMISSIONS_VIEW: 'academics.submissions.view',
  SUBMISSIONS_EXCEPTION: 'academics.submissions.exception',
} as const;
