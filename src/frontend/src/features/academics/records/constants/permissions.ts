/**
 * Authoritative client permission constants for Academic Records Desk (ADR-009, TD-07).
 * Mirrors backend permission constants defined in Elsasa.Sis.Academics.Permissions.
 */
export const RECORDS_PERMISSIONS = {
  RESULTS_VIEW: 'academics.results.view',
  RESULTS_EVALUATE: 'academics.results.evaluate',
  RESULTS_APPROVE: 'academics.results.approve',
  RESULTS_PUBLISH: 'academics.results.publish',

  REPORT_CARDS_VIEW: 'academics.reportcards.view',
  REPORT_CARDS_COMPILE: 'academics.reportcards.compile',
  REPORT_CARDS_APPROVE: 'academics.reportcards.approve',
  REPORT_CARDS_PUBLISH: 'academics.reportcards.publish',

  TRANSCRIPTS_VIEW: 'academics.transcripts.view',
  TRANSCRIPTS_GENERATE: 'academics.transcripts.generate',
  TRANSCRIPTS_ISSUE: 'academics.transcripts.issue',

  CORRECTIONS_CORRECT: 'academics.corrections.correct',
  CORRECTIONS_VIEW_HISTORY: 'academics.corrections.viewhistory',

  PROGRESS_VIEW: 'academics.progress.view',
  PROGRESS_RECORD: 'academics.progress.record',
  PROGRESS_CONCLUDE: 'academics.progress.conclude',
} as const;

export const AUTHORITY_ROLES = [
  'SuperAdmin',
  'AcademicAuthority',
  'Principal',
  'Headmaster',
] as const;
