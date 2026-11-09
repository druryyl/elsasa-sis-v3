/**
 * Authoritative client permission constants for Academic Interventions (SCR-ACD-06, ACD-WF-10, UC-ACD-11, P8-S21).
 * Mirrors backend permission constants in Elsasa.Sis.Academics.Permissions.Academics.Interventions.
 */
export const INTERVENTION_PERMISSIONS = {
  VIEW: 'academics.interventions.view',
  INITIATE: 'academics.interventions.initiate',
  UPDATE_STATUS: 'academics.interventions.update_status',
} as const;

export const INTERVENTION_ROLES = [
  'SuperAdmin',
  'Admin',
  'AcademicAuthority',
  'AcademicCoordinator',
  'Teacher',
] as const;
