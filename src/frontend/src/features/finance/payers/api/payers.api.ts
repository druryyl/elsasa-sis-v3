import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  CreatePayerPayload,
  CreatePayerResponse,
  GetPayersParams,
  PayerDto,
  PayerListItemDto,
} from '../types/payers.types';

/**
 * Queries payers with optional search term, type filtering, and pagination.
 * GET /api/finance/payers
 */
export async function getPayers(
  params?: GetPayersParams,
): Promise<PaginatedResponse<PayerListItemDto>> {
  const cleanParams: Record<string, string | number> = {
    page: params?.page ?? params?.pageNumber ?? 1,
    pageSize: params?.pageSize ?? 20,
  };

  if (params?.searchTerm) cleanParams.searchTerm = params.searchTerm;
  if (params?.payerType && params.payerType !== 'all') {
    cleanParams.payerType = params.payerType;
  }

  const response = (await apiClient.get('/finance/payers', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<PayerListItemDto>;

  return response;
}

/**
 * Retrieves comprehensive details and payment history for a single payer by ID.
 * GET /api/finance/payers/{id}
 */
export async function getPayerById(id: string): Promise<PayerDto> {
  const response = (await apiClient.get(
    `/finance/payers/${encodeURIComponent(id)}`,
  )) as unknown as PayerDto;

  return response;
}

/**
 * Registers a new explicit payer entity (sponsor, embassy, foundation, or guardian).
 * POST /api/finance/payers
 */
export async function createPayer(payload: CreatePayerPayload): Promise<CreatePayerResponse> {
  const response = (await apiClient.post(
    '/finance/payers',
    payload,
  )) as unknown as CreatePayerResponse;

  return response;
}
