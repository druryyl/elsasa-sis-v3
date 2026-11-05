/**
 * Authoritative client permission constants for Academics Assessment & Results (ADR-009, TD-09, TD-10).
 * Mirrors backend permission constants defined in Elsasa.Sis.Academics.Permissions.
 */
export const ASSESSMENT_PERMISSIONS = {
  ASSESSMENTS_VIEW: 'academics.assessments.view',
  ASSESSMENTS_MANAGE: 'academics.assessments.manage',
  ASSESSMENTS_CONDUCT: 'academics.assessments.conduct',
  EVIDENCE_RECORD: 'academics.evidence.record',
  EVIDENCE_VIEW: 'academics.evidence.view',
  RESULTS_EVALUATE: 'academics.results.evaluate',
  RESULTS_APPROVE: 'academics.results.approve',
  RESULTS_PUBLISH: 'academics.results.publish',
  RESULTS_VIEW: 'academics.results.view',
} as const;
