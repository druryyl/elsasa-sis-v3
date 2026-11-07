/**
 * Authoritative client permission constants for Cohort Academic Review (SCR-ACD-09, ACD-WF-13, UC-ACD-17, UC-ACD-21, UC-ACD-15, P8-S22).
 * Mirrors backend permission constants in Elsasa.Sis.Academics.Permissions.
 */
export const COHORT_REVIEW_PERMISSIONS = {
  STANDINGS_VIEW: 'academics.standings.view',
  STANDINGS_EVALUATE: 'academics.standings.evaluate',
  STANDINGS_DETERMINE: 'academics.standings.determine',
  STANDINGS_BATCH_DETERMINE: 'academics.standings.batch_determine',

  PROGRESSION_VIEW: 'academics.progression.view',
  PROGRESSION_EVALUATE: 'academics.progression.evaluate',
  PROGRESSION_ISSUE: 'academics.progression.issue',
  PROGRESSION_BATCH_ISSUE: 'academics.progression.batch_issue',

  ACHIEVEMENTS_VIEW: 'academics.achievements.view',
  ACHIEVEMENTS_CONFER: 'academics.achievements.confer',

  CORRECTIONS_STANDING: 'academics.corrections.standing',
  CORRECTIONS_PROGRESSION: 'academics.corrections.progression',
} as const;

export const COHORT_REVIEW_ROLES = [
  'SuperAdmin',
  'Admin',
  'AcademicAuthority',
] as const;
