/**
 * Authoritative permission and role constants for Student Learning Plans (SCR-ACD-04, P1-S03, P7-S19).
 */

export const LEARNING_PLAN_PERMISSIONS = {
  VIEW: 'academics:learning-plans:view',
  MANAGE: 'academics:learning-plans:manage',
  APPROVE: 'academics:learning-plans:approve',
  REVISE: 'academics:learning-plans:revise',
} as const;

export const LEARNING_PLAN_ROLES = {
  COORDINATOR: 'AcademicCoordinator',
  AUTHORITY: 'AcademicAuthority',
  TEACHER: 'Teacher',
  SDO: 'StudentDevelopmentOfficer',
} as const;
