/**
 * Authoritative client permission constants for the Academic Participation module (ADR-009, TD-07).
 * Strictly mirrors backend permission constants in Elsasa.Sis.Academics.Permissions.Academics.Participations.
 */
export const PARTICIPATION_PERMISSIONS = {
  VIEW: 'academics.participations.view',
  MANAGE: 'academics.participations.manage',
  RECOGNIZE_PRIOR: 'academics.participations.recognize_prior',
  CONCLUDE: 'academics.participations.conclude',
} as const;

export type ParticipationPermission =
  (typeof PARTICIPATION_PERMISSIONS)[keyof typeof PARTICIPATION_PERMISSIONS];
