/**
 * TypeScript types and DTOs for Billing Documents & Statements Pipeline (GAP-005, TD-05).
 */

export type BillingDocumentType = 'bill' | 'statement';

export type DeliveryChannel = 'portal' | 'email' | 'printed';

export type DeliveryStatus = 'draft' | 'issued' | 'sent' | 'cancelled' | 'paid' | 'overdue';

/**
 * Frozen itemized line item inside a billing document or statement.
 */
export interface BillingDocumentItemDto {
  id: string;
  billingDocumentId?: string;
  lineSequence: number;
  itemType: string;
  referenceEntityType: string;
  referenceEntityId: string;
  description: string;
  amount: number;
  currencyCode: string;
  createdAt?: string;
}

/**
 * Authoritative billing document aggregate snapshot (bill or statement).
 */
export interface BillingDocumentDto {
  id: string;
  documentNumber: string;
  studentAccountId: string;
  studentId: string;
  studentNumber?: string;
  studentFullName?: string;
  documentType: BillingDocumentType | string;
  recipientName: string;
  recipientRelationship: string;
  recipientContact: string;
  billingPeriodStart: string | null;
  billingPeriodEnd: string | null;
  issueDate: string;
  paymentDueDate: string | null;
  totalBilledAmount: number;
  currencyCode: string;
  deliveryChannel: DeliveryChannel | string;
  deliveryStatus: DeliveryStatus | string;
  sentAt: string | null;
  notes: string | null;
  items?: BillingDocumentItemDto[];
  createdAt: string;
  createdBy?: string | null;
}

/**
 * Command payload for generating a point-in-time bill compiling open obligations (FIN-WF-04, UC-FIN-03).
 */
export interface GenerateBillRequest {
  studentId: string;
  obligationIds?: string[] | null;
  paymentDueDate?: string | null;
  notes?: string | null;
}

/**
 * Response payload returned after successfully compiling a bill.
 */
export interface GenerateBillingDocumentResponse {
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
  notes: string | null;
  items: BillingDocumentItemDto[];
  createdAt: string;
  createdBy: string | null;
}

/**
 * Command payload for generating a periodic financial statement across a date range.
 */
export interface GenerateStatementRequest {
  studentId: string;
  billingPeriodStart: string;
  billingPeriodEnd: string;
  notes?: string | null;
}

/**
 * Response payload returned after successfully compiling a periodic statement.
 */
export interface GenerateStatementResponse {
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
  notes: string | null;
  items: BillingDocumentItemDto[];
  createdAt: string;
  createdBy: string | null;
}

/**
 * Filter parameters for querying billing documents.
 */
export interface GetBillingDocumentsParams {
  studentId?: string;
  documentType?: string;
  deliveryStatus?: string;
  page?: number;
  pageSize?: number;
}
