import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  BillingDocumentDto,
  GenerateBillingDocumentResponse,
  GenerateBillRequest,
  GenerateStatementRequest,
  GenerateStatementResponse,
  GetBillingDocumentsParams,
} from '../types/billing.types';

/**
 * Queries billing documents with filters and pagination (FIN-WF-04, SCR-FIN-03).
 * GET /api/finance/billing-documents
 */
export async function getBillingDocuments(
  params?: GetBillingDocumentsParams,
): Promise<PaginatedResponse<BillingDocumentDto>> {
  const cleanParams: Record<string, string | number> = {
    page: params?.page ?? 1,
    pageSize: params?.pageSize ?? 20,
  };

  if (params?.studentId) cleanParams.studentId = params.studentId;
  if (params?.documentType && params.documentType !== 'all') {
    cleanParams.documentType = params.documentType;
  }
  if (params?.deliveryStatus && params.deliveryStatus !== 'all') {
    cleanParams.deliveryStatus = params.deliveryStatus;
  }

  const response = (await apiClient.get('/finance/billing-documents', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<BillingDocumentDto>;

  return response;
}

/**
 * Retrieves a detailed billing document snapshot by ID including frozen line items.
 * GET /api/finance/billing-documents/{id}
 */
export async function getBillingDocumentById(id: string): Promise<BillingDocumentDto> {
  const response = (await apiClient.get(
    `/finance/billing-documents/${encodeURIComponent(id)}`,
  )) as unknown as BillingDocumentDto;

  return response;
}

/**
 * Compiles open student obligations into an authorized bill/invoice (FIN-WF-04, UC-FIN-03).
 * POST /api/finance/billing-documents/generate
 */
export async function generateBill(
  request: GenerateBillRequest,
): Promise<GenerateBillingDocumentResponse> {
  const response = (await apiClient.post(
    '/finance/billing-documents/generate',
    request,
  )) as unknown as GenerateBillingDocumentResponse;

  return response;
}

/**
 * Compiles a periodic financial statement across a specified date range.
 * POST /api/finance/billing-documents/statement
 */
export async function generateStatement(
  request: GenerateStatementRequest,
): Promise<GenerateStatementResponse> {
  const response = (await apiClient.post(
    '/finance/billing-documents/statement',
    request,
  )) as unknown as GenerateStatementResponse;

  return response;
}
