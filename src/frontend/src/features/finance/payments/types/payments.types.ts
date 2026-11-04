/**
 * TypeScript interfaces and DTOs for Finance Payments Intake & Verification Queue (TD-04, OQ-004, SCR-FIN-02, SCR-SELF-02).
 */

export type PaymentConfirmationStatus =
  | 'submitted'
  | 'confirmed'
  | 'rejected'
  | 'reversed'
  | (string & {});

export type PaymentMethodType =
  | 'bank_transfer'
  | 'cash'
  | 'pos_card'
  | 'qris'
  | 'virtual_account'
  | 'cheque'
  | 'other'
  | (string & {});

/**
 * Summary DTO representing an allocation line item attached to a payment.
 */
export interface PaymentAllocationSummaryDto {
  id: string;
  allocationNumber: string;
  obligationId: string;
  obligationNumber: string;
  chargeType: string;
  description: string;
  allocatedAmount: number; // Zero-decimal integer IDR
  allocationDate: string;
  allocatedBy: string;
  status: string;
  supersededById: string | null;
  reversalReason: string | null;
}

/**
 * Detailed DTO for inspecting a single payment transaction.
 */
export interface PaymentDto {
  id: string;
  paymentNumber: string;
  receiptNumber: string | null;
  studentAccountId: string;
  accountNumber: string;
  studentId: string | null;
  studentNumber: string | null;
  studentFullName: string | null;
  applicantId: string | null;
  payerId: string;
  payerNumber: string;
  payerName: string;
  payerType: string;
  payerIdentificationNumber: string | null;
  payerEmail: string | null;
  payerPhone: string | null;
  paymentAmount: number; // Zero-decimal integer IDR
  currencyCode: string;
  paymentMethod: PaymentMethodType | string;
  externalReference: string | null;
  paymentDate: string;
  submissionDate: string;
  confirmationStatus: PaymentConfirmationStatus | string;
  confirmationDate: string | null;
  confirmedBy: string | null;
  receiptIssuedAt: string | null;
  rejectionReason: string | null;
  notes: string | null;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  totalAllocated: number; // Zero-decimal integer IDR
  unallocatedAmount: number; // Zero-decimal integer IDR
  allocations: PaymentAllocationSummaryDto[];
}

/**
 * Summary DTO for payment worklist records and verification queues.
 */
export interface PaymentListItemDto {
  id: string;
  paymentNumber: string;
  receiptNumber: string | null;
  studentAccountId: string;
  accountNumber: string;
  studentId: string | null;
  studentNumber: string | null;
  studentFullName: string | null;
  applicantId: string | null;
  payerId: string;
  payerNumber: string;
  payerName: string;
  paymentAmount: number; // Zero-decimal integer IDR
  currencyCode: string;
  paymentMethod: PaymentMethodType | string;
  externalReference: string | null;
  paymentDate: string;
  submissionDate: string;
  confirmationStatus: PaymentConfirmationStatus | string;
  confirmationDate: string | null;
  confirmedBy: string | null;
  rejectionReason: string | null;
  notes: string | null;
  totalAllocated: number; // Zero-decimal integer IDR
  unallocatedAmount: number; // Zero-decimal integer IDR
}

/**
 * Command payload for self-service payment submission (SCR-SELF-02).
 */
export interface SubmitPaymentPayload {
  studentId?: string | null;
  applicantId?: string | null;
  studentAccountId?: string | null;
  amount: number; // Zero-decimal integer IDR
  paymentMethod: PaymentMethodType | string;
  paymentDate?: string | null;
  externalReference?: string | null;
  payerName: string;
  payerIdentificationNumber?: string | null;
  payerEmail?: string | null;
  payerPhone?: string | null;
  payerType?: string | null;
  payerId?: string | null;
  notes?: string | null;
}

/**
 * Response payload emitted after self-service payment intake.
 */
export interface SubmitPaymentResponse {
  paymentId: string;
  paymentNumber: string;
  confirmationStatus: string;
  amount: number;
  paymentMethod: string;
  paymentDate: string;
  submissionDate: string;
  payerId: string;
  studentAccountId: string;
  studentId: string | null;
  applicantId: string | null;
}

/**
 * Command payload for cashier counter recording with atomic confirmation (SCR-FIN-02).
 */
export interface RecordPaymentPayload {
  studentId?: string | null;
  applicantId?: string | null;
  studentAccountId?: string | null;
  amount: number; // Zero-decimal integer IDR
  paymentMethod: PaymentMethodType | string;
  paymentDate?: string | null;
  externalReference?: string | null;
  payerName: string;
  payerIdentificationNumber?: string | null;
  payerEmail?: string | null;
  payerPhone?: string | null;
  payerType?: string | null;
  payerId?: string | null;
  notes?: string | null;
}

/**
 * Response payload emitted after cashier counter payment recording.
 */
export interface RecordPaymentResponse {
  paymentId: string;
  paymentNumber: string;
  receiptNumber: string;
  confirmationStatus: string;
  amount: number;
  paymentMethod: string;
  paymentDate: string;
  confirmationDate: string;
  confirmedBy: string;
  payerId: string;
  studentAccountId: string;
  studentId: string | null;
  applicantId: string | null;
}

/**
 * Command payload for confirming a submitted payment in verification queue.
 */
export interface ConfirmPaymentPayload {
  paymentId?: string;
}

/**
 * Response payload returned after confirming a payment in the verification queue.
 */
export interface ConfirmPaymentResponse {
  paymentId: string;
  paymentNumber: string;
  receiptNumber: string;
  confirmationStatus: string;
  amount: number;
  confirmationDate: string;
  confirmedBy: string;
  studentId: string | null;
  applicantId: string | null;
}

/**
 * Command payload for rejecting an invalid payment submission.
 */
export interface RejectPaymentPayload {
  rejectionReason: string;
  notes?: string | null;
}

/**
 * Response payload returned after rejecting a payment submission.
 */
export interface RejectPaymentResponse {
  paymentId: string;
  paymentNumber: string;
  confirmationStatus: string;
  rejectionReason: string;
  updatedAt: string;
  updatedBy: string;
}

/**
 * Official school letterhead and branding details on receipt vouchers.
 */
export interface SchoolHeaderDto {
  schoolName: string;
  schoolAddress: string;
  contactInfo: string;
  documentTitle: string;
}

/**
 * Payer details on receipt voucher.
 */
export interface ReceiptPayerDto {
  id: string;
  payerNumber: string;
  payerName: string;
  identificationNumber: string | null;
  email: string | null;
  phoneNumber: string | null;
}

/**
 * Student / applicant beneficiary details on receipt voucher.
 */
export interface ReceiptCandidateDto {
  studentId: string | null;
  studentNumber: string | null;
  studentFullName: string | null;
  applicantId: string | null;
  accountNumber: string;
}

/**
 * Line-item allocation distribution on receipt voucher.
 */
export interface ReceiptAllocationLineDto {
  allocationNumber: string;
  obligationNumber: string;
  chargeType: string;
  description: string;
  amount: number; // Zero-decimal integer IDR
}

/**
 * Cashier counter verification and authorization signature metadata.
 */
export interface CashierSignatureDto {
  confirmedBy: string;
  confirmationDate: string;
  title: string;
  officialStampText: string;
}

/**
 * Complete structured receipt voucher projection suitable for counter printout or PDF rendering.
 */
export interface PaymentReceiptDto {
  header: SchoolHeaderDto;
  paymentId: string;
  paymentNumber: string;
  receiptNumber: string;
  paymentDate: string;
  receiptIssuedAt: string;
  paymentMethod: string;
  externalReference: string | null;
  amount: number; // Zero-decimal integer IDR
  currencyCode: string;
  amountInWords: string;
  payer: ReceiptPayerDto;
  candidate: ReceiptCandidateDto;
  allocations: ReceiptAllocationLineDto[];
  cashier: CashierSignatureDto;
  notes: string | null;
}

/**
 * Query filter parameters for payments worklists and verification queues.
 */
export interface GetPaymentsParams {
  status?: string;
  studentId?: string;
  applicantId?: string;
  payerId?: string;
  startDate?: string;
  endDate?: string;
  searchTerm?: string;
  page?: number;
  pageNumber?: number;
  pageSize?: number;
}
