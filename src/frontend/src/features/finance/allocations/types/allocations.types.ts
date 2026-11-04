/**
 * TypeScript interfaces and DTOs for Payment Allocations, Reallocations & Student Receivables (TD-05, OQ-003, SCR-FIN-02, SCR-FIN-03).
 */

export type AllocationStatus = 'active' | 'superseded' | 'reversed' | (string & {});

export type AgingBracket =
  | 'Current'
  | '1-30 Days'
  | '31-60 Days'
  | '61-90 Days'
  | '90+ Days'
  | (string & {});

export type ReceivableStatus = 'settled' | 'overdue' | 'current' | (string & {});

/**
 * Data transfer object representing an allocation applied from a payment to an obligation.
 */
export interface PaymentAllocationDto {
  id?: string;
  allocationId?: string;
  allocationNumber: string;
  obligationId: string;
  obligationNumber: string;
  chargeType?: string;
  description?: string;
  allocatedAmount: number; // Zero-decimal integer IDR
  allocationDate: string;
  allocatedBy?: string;
  status: AllocationStatus | string;
  supersededById?: string | null;
  reversalReason?: string | null;
}

/**
 * Data transfer object representing a simulated suggested allocation to an open obligation.
 */
export interface AllocationSuggestionDto {
  obligationId: string;
  obligationNumber: string;
  chargeType: string;
  description: string;
  isMandatory: boolean;
  dueDate: string;
  originalAmount: number; // Zero-decimal integer IDR
  totalPaid: number; // Zero-decimal integer IDR
  outstandingAmount: number; // Zero-decimal integer IDR
  suggestedAmount: number; // Zero-decimal integer IDR
  remainingObligationBalance: number; // Zero-decimal integer IDR
}

/**
 * Filter parameters for querying smart payment allocation auto-suggestions.
 */
export interface SuggestAllocationsParams {
  studentId?: string;
  applicantId?: string;
  paymentAmount: number; // Zero-decimal integer IDR
}

/**
 * Response payload representing simulated allocation recommendations and remaining unallocated balance.
 */
export interface SuggestAllocationsResponse {
  studentId: string | null;
  applicantId: string | null;
  paymentAmount: number; // Zero-decimal integer IDR
  totalSuggestedAllocated: number; // Zero-decimal integer IDR
  remainingUnallocatedAmount: number; // Zero-decimal integer IDR
  suggestions: AllocationSuggestionDto[];
}

/**
 * Individual line item requested within an allocation command.
 */
export interface AllocationItemPayload {
  obligationId: string;
  amount: number; // Zero-decimal integer IDR
  notes?: string | null;
}

/**
 * Command payload to explicitly allocate confirmed payment funds across obligations.
 */
export interface AllocatePaymentPayload {
  paymentId: string;
  allocations: AllocationItemPayload[];
  notes?: string | null;
}

/**
 * Response payload emitted after committing payment allocations.
 */
export interface AllocatePaymentResponse {
  paymentId: string;
  paymentNumber: string;
  paymentAmount: number; // Zero-decimal integer IDR
  totalAllocatedInRequest: number; // Zero-decimal integer IDR
  cumulativeAllocatedAmount: number; // Zero-decimal integer IDR
  remainingUnallocatedAmount: number; // Zero-decimal integer IDR
  allocations: PaymentAllocationDto[];
}

/**
 * Individual replacement allocation item requested during reallocation.
 */
export interface ReplacementAllocationItemPayload {
  obligationId: string;
  amount: number; // Zero-decimal integer IDR
  notes?: string | null;
}

/**
 * Command payload to reallocate or reverse payment allocations with full audit lineage.
 */
export interface ReallocatePaymentPayload {
  paymentId: string;
  priorAllocationIds: string[];
  replacementAllocations?: ReplacementAllocationItemPayload[] | null;
  reversalReason: string;
  notes?: string | null;
}

/**
 * Response payload representing the outcome of a reallocation or reversal.
 */
export interface ReallocatePaymentResponse {
  paymentId: string;
  paymentNumber: string;
  reversalReason: string;
  supersededAllocationIds: string[];
  reversedAllocationIds: string[];
  paymentAmount: number; // Zero-decimal integer IDR
  activeAllocatedAmount: number; // Zero-decimal integer IDR
  remainingUnallocatedAmount: number; // Zero-decimal integer IDR
  replacementAllocations: PaymentAllocationDto[];
}

/**
 * Projected DTO matching finance.v_student_receivables with aging brackets and outstanding balances.
 */
export interface StudentReceivableDto {
  obligationId: string;
  obligationNumber: string;
  studentAccountId: string;
  studentId: string | null;
  applicantId: string | null;
  chargeType: string;
  description: string;
  isMandatory: boolean;
  dueDate: string;
  originalAmount: number; // Zero-decimal integer IDR
  currencyCode: string;
  academicPeriodId: string | null;
  academicPeriodLabel: string | null;
  obligationStatus: string;
  netObligation: number; // Zero-decimal integer IDR
  totalPaid: number; // Zero-decimal integer IDR
  outstandingAmount: number; // Zero-decimal integer IDR
  isOverdue: boolean;
  daysOverdue: number;
  agingBracket: AgingBracket | string;
  receivableStatus: ReceivableStatus | string;
}

/**
 * Aggregated response container for student receivables query.
 */
export interface GetStudentReceivablesResponse {
  studentId: string;
  totalNetObligation: number; // Zero-decimal integer IDR
  totalPaid: number; // Zero-decimal integer IDR
  totalOutstanding: number; // Zero-decimal integer IDR
  totalOverdueCount: number;
  receivables: StudentReceivableDto[];
  openObligations: StudentReceivableDto[];
}

/**
 * Query parameters for student receivables query.
 */
export interface GetStudentReceivablesParams {
  openOnly?: boolean;
}
