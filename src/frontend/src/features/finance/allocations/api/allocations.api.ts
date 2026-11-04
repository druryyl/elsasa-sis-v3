import { apiClient } from '@/shared/services/http';
import type {
  AllocatePaymentPayload,
  AllocatePaymentResponse,
  GetStudentReceivablesParams,
  GetStudentReceivablesResponse,
  ReallocatePaymentPayload,
  ReallocatePaymentResponse,
  SuggestAllocationsParams,
  SuggestAllocationsResponse,
} from '../types/allocations.types';

/**
 * Computes rule-based distribution recommendation for a payment across open obligations (TD-05, OQ-003).
 * GET /api/finance/allocations/suggest
 */
export async function suggestAllocations(
  params: SuggestAllocationsParams,
): Promise<SuggestAllocationsResponse> {
  const queryParams: Record<string, string | number> = {
    paymentAmount: params.paymentAmount,
  };

  if (params.studentId) queryParams.studentId = params.studentId;
  if (params.applicantId) queryParams.applicantId = params.applicantId;

  const response = (await apiClient.get('/finance/allocations/suggest', {
    params: queryParams,
  })) as unknown as SuggestAllocationsResponse;

  return response;
}

/**
 * Commits explicit payment fund allocation across one or more student obligations (SCR-FIN-02).
 * POST /api/finance/allocations
 */
export async function allocatePayment(
  payload: AllocatePaymentPayload,
): Promise<AllocatePaymentResponse> {
  const response = (await apiClient.post(
    '/finance/allocations',
    payload,
  )) as unknown as AllocatePaymentResponse;

  return response;
}

/**
 * Corrects an erroneous allocation by superseding previous records and establishing replacement lineage (SCR-FIN-03).
 * POST /api/finance/allocations/reallocate
 */
export async function reallocatePayment(
  payload: ReallocatePaymentPayload,
): Promise<ReallocatePaymentResponse> {
  const response = (await apiClient.post(
    '/finance/allocations/reallocate',
    payload,
  )) as unknown as ReallocatePaymentResponse;

  return response;
}

/**
 * Queries real-time student receivables, aging brackets, and outstanding balances (SCR-FIN-03).
 * GET /api/finance/accounts/{studentId}/receivables
 */
export async function getStudentReceivables(
  studentId: string,
  params?: GetStudentReceivablesParams,
): Promise<GetStudentReceivablesResponse> {
  const queryParams: Record<string, boolean> = {};
  if (params?.openOnly !== undefined) {
    queryParams.openOnly = params.openOnly;
  }

  const response = (await apiClient.get(
    `/finance/accounts/${encodeURIComponent(studentId)}/receivables`,
    {
      params: queryParams,
    },
  )) as unknown as GetStudentReceivablesResponse;

  return response;
}
