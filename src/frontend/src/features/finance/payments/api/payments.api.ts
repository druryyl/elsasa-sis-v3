import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  ConfirmPaymentResponse,
  GetPaymentsParams,
  PaymentDto,
  PaymentListItemDto,
  PaymentReceiptDto,
  RecordPaymentPayload,
  RecordPaymentResponse,
  RejectPaymentPayload,
  RejectPaymentResponse,
  SubmitPaymentPayload,
  SubmitPaymentResponse,
} from '../types/payments.types';

/**
 * Queries payments with filters and pagination for worklists and verification queues (SCR-FIN-02).
 * GET /api/finance/payments
 */
export async function getPayments(
  params?: GetPaymentsParams,
): Promise<PaginatedResponse<PaymentListItemDto>> {
  const cleanParams: Record<string, string | number> = {
    page: params?.page ?? params?.pageNumber ?? 1,
    pageSize: params?.pageSize ?? 20,
  };

  if (params?.status && params.status !== 'all') cleanParams.status = params.status;
  if (params?.studentId) cleanParams.studentId = params.studentId;
  if (params?.applicantId) cleanParams.applicantId = params.applicantId;
  if (params?.payerId) cleanParams.payerId = params.payerId;
  if (params?.startDate) cleanParams.startDate = params.startDate;
  if (params?.endDate) cleanParams.endDate = params.endDate;
  if (params?.searchTerm) cleanParams.searchTerm = params.searchTerm;

  const response = (await apiClient.get('/finance/payments', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<PaymentListItemDto>;

  return response;
}

/**
 * Retrieves comprehensive details of a payment by ID including allocations breakdown.
 * GET /api/finance/payments/{id}
 */
export async function getPaymentById(id: string): Promise<PaymentDto> {
  const response = (await apiClient.get(
    `/finance/payments/${encodeURIComponent(id)}`,
  )) as unknown as PaymentDto;

  return response;
}

/**
 * Retrieves structured receipt voucher details for official printing or PDF export.
 * GET /api/finance/payments/{id}/receipt
 */
export async function getPaymentReceipt(id: string): Promise<PaymentReceiptDto> {
  const response = (await apiClient.get(
    `/finance/payments/${encodeURIComponent(id)}/receipt`,
  )) as unknown as PaymentReceiptDto;

  return response;
}

/**
 * Submits a payment via self-service portal, entering in submitted status (SCR-SELF-02).
 * POST /api/finance/payments/submit
 */
export async function submitPayment(
  payload: SubmitPaymentPayload,
): Promise<SubmitPaymentResponse> {
  const response = (await apiClient.post(
    '/finance/payments/submit',
    payload,
  )) as unknown as SubmitPaymentResponse;

  return response;
}

/**
 * Records walk-in counter cash/POS payment with atomic receipt issuance (SCR-FIN-02).
 * POST /api/finance/payments
 */
export async function recordAndConfirmPayment(
  payload: RecordPaymentPayload,
): Promise<RecordPaymentResponse> {
  const response = (await apiClient.post(
    '/finance/payments',
    payload,
  )) as unknown as RecordPaymentResponse;

  return response;
}

/**
 * Confirms a submitted payment in the verification queue, issuing an official receipt (SCR-FIN-02).
 * POST /api/finance/payments/{id}/confirm
 */
export async function confirmPayment(id: string): Promise<ConfirmPaymentResponse> {
  const response = (await apiClient.post(
    `/finance/payments/${encodeURIComponent(id)}/confirm`,
  )) as unknown as ConfirmPaymentResponse;

  return response;
}

/**
 * Rejects an invalid submitted payment with a mandatory justification reason (SCR-FIN-02).
 * POST /api/finance/payments/{id}/reject
 */
export async function rejectPayment(
  id: string,
  payload: RejectPaymentPayload,
): Promise<RejectPaymentResponse> {
  const response = (await apiClient.post(
    `/finance/payments/${encodeURIComponent(id)}/reject`,
    payload,
  )) as unknown as RejectPaymentResponse;

  return response;
}
