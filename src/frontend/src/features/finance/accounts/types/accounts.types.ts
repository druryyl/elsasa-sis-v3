/**
 * TypeScript types and DTOs for Student Financial Accounts & Ledger Projections (TD-02, SCR-FIN-03).
 */

export type AccountStatus = 'active' | 'suspended' | 'closed';

/**
 * Derived ledger balance snapshot for a student financial account.
 */
export interface AccountBalanceDto {
  totalAssessed: number;
  totalSettled: number;
  currentBalance: number;
  currencyCode?: string;
}

/**
 * Open obligation summary item embedded in account dossier.
 */
export interface OpenObligationDto {
  id: string;
  obligationNumber: string;
  studentAccountId: string;
  studentId: string;
  chargeRuleId: string | null;
  policyId: string;
  description: string;
  chargeType: string;
  isMandatory: boolean;
  originalAmount: number;
  currencyCode: string;
  issueDate: string;
  dueDate: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  status: string;
  authorizedBy: string;
  createdAt: string;
}

/**
 * Recent billing document item embedded in account dossier.
 */
export interface RecentBillingDocumentDto {
  id: string;
  documentNumber: string;
  studentAccountId: string;
  studentId: string;
  documentType: string;
  recipientName: string;
  recipientRelationship: string;
  recipientContact: string;
  billingPeriodStart: string | null;
  billingPeriodEnd: string | null;
  issueDate: string;
  paymentDueDate: string | null;
  totalBilledAmount: number;
  currencyCode: string;
  deliveryChannel: string;
  deliveryStatus: string;
  sentAt: string | null;
  createdAt: string;
}

/**
 * Complete 360-degree student financial account dossier with derived balance (TD-02).
 */
export interface AccountSummaryDto {
  accountId: string;
  accountNumber: string;
  studentId: string;
  studentNumber: string | null;
  studentFullName: string | null;
  currencyCode: string;
  accountStatus: AccountStatus | string;
  openedDate: string;
  closedDate: string | null;
  notes: string | null;
  createdAt: string;
  totalAssessed: number;
  totalSettled: number;
  currentBalance: number;
  openObligations: OpenObligationDto[];
  recentBillingDocuments: RecentBillingDocumentDto[];
}

/**
 * Summary item projecting student financial account joined with student demographics and derived open balance.
 */
export interface StudentFinancialAccountDto {
  accountId: string;
  accountNumber: string;
  studentId: string;
  studentNumber: string;
  studentFullName: string;
  campus: string;
  grade: string;
  currencyCode: string;
  accountStatus: AccountStatus | string;
  openedDate: string;
  totalAssessed: number;
  totalSettled: number;
  currentBalance: number;
}

/**
 * Query parameters for searching student financial accounts.
 */
export interface SearchStudentAccountsParams {
  searchTerm?: string;
  campus?: string;
  grade?: string;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
}
