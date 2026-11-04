/**
 * Authoritative client permission constants for Finance Payments, Allocations, and Payers (ADR-009, GAP-008, TD-07).
 * Mirrors backend permission constants defined in Elsasa.Sis.Finance.Permissions.
 */

export const FINANCE_PAYMENT_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Payments.View',
  RECORD: 'Permissions.Finance.Payments.Record',
  CONFIRM: 'Permissions.Finance.Payments.Confirm',
  REJECT: 'Permissions.Finance.Payments.Reject',
} as const;

export const FINANCE_ALLOCATION_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Allocations.View',
  EXECUTE: 'Permissions.Finance.Allocations.Execute',
  REALLOCATE: 'Permissions.Finance.Allocations.Reallocate',
} as const;

export const FINANCE_PAYER_PERMISSIONS = {
  VIEW: 'Permissions.Finance.Payers.View',
  MANAGE: 'Permissions.Finance.Payers.Manage',
} as const;
