/**
 * Authoritative client permission constants for Student Financial Accounts (ADR-009, GAP-006).
 * Mirrors backend permission constants defined in Elsasa.Sis.Finance.Permissions.
 */
export const FINANCE_ACCOUNT_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Accounts.View',
} as const;

export const FINANCE_OBLIGATION_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Obligations.View',
  ASSESS: 'Permissions.Finance.Obligations.Assess',
} as const;

export const FINANCE_BILLING_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Billing.View',
  GENERATE: 'Permissions.Finance.Billing.Generate',
} as const;
