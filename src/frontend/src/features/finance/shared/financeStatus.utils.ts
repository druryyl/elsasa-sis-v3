/**
 * Status tags, labels, and PrimeVue badge severity helpers for Finance entities (GAP-006, TD-08).
 */

export type StatusSeverity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

export interface StatusDescriptor {
  label: string;
  severity: StatusSeverity;
}

/**
 * Returns label and badge severity for Financial Policy lifecycle statuses.
 */
export function getPolicyStatusDescriptor(status?: string | null): StatusDescriptor {
  switch (status?.toLowerCase()) {
    case 'active':
      return { label: 'Active', severity: 'success' };
    case 'draft':
      return { label: 'Draft', severity: 'warn' };
    case 'superseded':
      return { label: 'Superseded', severity: 'secondary' };
    case 'archived':
      return { label: 'Archived', severity: 'contrast' };
    default:
      return { label: status ?? 'Unknown', severity: 'secondary' };
  }
}

export function getPolicyStatusSeverity(status?: string | null): StatusSeverity {
  return getPolicyStatusDescriptor(status).severity;
}

export function getPolicyStatusLabel(status?: string | null): string {
  return getPolicyStatusDescriptor(status).label;
}

/**
 * Returns label and badge severity for Financial Obligation statuses.
 */
export function getObligationStatusDescriptor(status?: string | null): StatusDescriptor {
  switch (status?.toLowerCase()) {
    case 'assessed':
      return { label: 'Assessed', severity: 'info' };
    case 'issued':
      return { label: 'Issued', severity: 'warn' };
    case 'partially_settled':
      return { label: 'Partially Settled', severity: 'warn' };
    case 'settled':
      return { label: 'Settled', severity: 'success' };
    case 'waived':
      return { label: 'Waived', severity: 'secondary' };
    case 'cancelled':
      return { label: 'Cancelled', severity: 'danger' };
    default:
      return { label: status ?? 'Unknown', severity: 'secondary' };
  }
}

export function getObligationStatusSeverity(status?: string | null): StatusSeverity {
  return getObligationStatusDescriptor(status).severity;
}

export function getObligationStatusLabel(status?: string | null): string {
  return getObligationStatusDescriptor(status).label;
}

/**
 * Returns label and badge severity for Billing Document delivery/payment statuses.
 */
export function getBillingStatusDescriptor(status?: string | null): StatusDescriptor {
  switch (status?.toLowerCase()) {
    case 'issued':
      return { label: 'Issued', severity: 'info' };
    case 'sent':
      return { label: 'Sent', severity: 'success' };
    case 'draft':
      return { label: 'Draft', severity: 'secondary' };
    case 'cancelled':
      return { label: 'Cancelled', severity: 'danger' };
    case 'paid':
      return { label: 'Paid', severity: 'success' };
    case 'overdue':
      return { label: 'Overdue', severity: 'danger' };
    default:
      return { label: status ?? 'Unknown', severity: 'secondary' };
  }
}

export function getBillingStatusSeverity(status?: string | null): StatusSeverity {
  return getBillingStatusDescriptor(status).severity;
}

export function getBillingStatusLabel(status?: string | null): string {
  return getBillingStatusDescriptor(status).label;
}

/**
 * Returns label and badge severity for Student Financial Account statuses.
 */
export function getAccountStatusDescriptor(status?: string | null): StatusDescriptor {
  switch (status?.toLowerCase()) {
    case 'active':
      return { label: 'Active', severity: 'success' };
    case 'suspended':
      return { label: 'Suspended', severity: 'warn' };
    case 'closed':
      return { label: 'Closed', severity: 'secondary' };
    default:
      return { label: status ?? 'Unknown', severity: 'secondary' };
  }
}

export function getAccountStatusSeverity(status?: string | null): StatusSeverity {
  return getAccountStatusDescriptor(status).severity;
}

export function getAccountStatusLabel(status?: string | null): string {
  return getAccountStatusDescriptor(status).label;
}
