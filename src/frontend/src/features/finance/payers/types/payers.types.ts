/**
 * TypeScript interfaces and DTOs for Finance Payer Management (GAP-005, OQ-002, TD-03, SCR-FIN-02).
 */

export type PayerType =
  | 'parent_guardian'
  | 'sponsor'
  | 'embassy'
  | 'foundation'
  | 'other'
  | (string & {});

/**
 * Summary DTO of a payment record linked to a specific payer.
 */
export interface PayerPaymentSummaryDto {
  id: string;
  paymentNumber: string;
  studentAccountId: string;
  studentId: string | null;
  applicantId: string | null;
  paymentAmount: number; // Zero-decimal integer IDR
  currencyCode: string;
  paymentMethod: string;
  externalReference: string | null;
  paymentDate: string;
  confirmationStatus: string;
  receiptNumber: string | null;
  submissionDate: string;
}

/**
 * Detailed DTO for inspecting a single payer identity including transaction history.
 */
export interface PayerDto {
  id: string;
  payerNumber: string;
  payerName: string;
  payerType: string;
  identificationNumber: string | null;
  email: string | null;
  phoneNumber: string | null;
  billingAddress: string | null;
  bankAccountName: string | null;
  bankAccountNumber: string | null;
  bankName: string | null;
  notes: string | null;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  totalPaymentsCount: number;
  totalPaidAmount: number; // Zero-decimal integer IDR
  payments: PayerPaymentSummaryDto[];
}

/**
 * Summary item for paginated payer list views and lookup dropdowns.
 */
export interface PayerListItemDto {
  id: string;
  payerNumber: string;
  payerName: string;
  payerType: string;
  identificationNumber: string | null;
  email: string | null;
  phoneNumber: string | null;
  createdAt: string;
}

/**
 * Command payload for explicitly registering a new payer (sponsor, embassy, guardian).
 */
export interface CreatePayerPayload {
  name: string;
  type?: string | null;
  identificationNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  billingAddress?: string | null;
  bankAccountName?: string | null;
  bankAccountNumber?: string | null;
  bankName?: string | null;
  notes?: string | null;
}

/**
 * Response emitted after successfully registering a new payer.
 */
export interface CreatePayerResponse {
  id: string;
  payerNumber: string;
  payerName: string;
  payerType: string;
}

/**
 * Query filter parameters for searching and paginating payers.
 */
export interface GetPayersParams {
  searchTerm?: string;
  payerType?: string;
  page?: number;
  pageNumber?: number;
  pageSize?: number;
}
