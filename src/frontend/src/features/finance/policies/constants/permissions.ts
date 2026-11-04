/**
 * Authoritative client permission constants for Financial Policies domain (ADR-009, GAP-006).
 * Mirrors backend permission constants defined in Elsasa.Sis.Finance.Permissions.
 */
export const FINANCE_POLICY_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Policies.View',
  MANAGE: 'Permissions.Finance.Policies.Manage',
  APPROVE: 'Permissions.Finance.Policies.Approve',
} as const;
