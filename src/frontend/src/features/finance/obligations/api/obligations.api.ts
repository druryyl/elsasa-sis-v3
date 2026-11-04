import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  AssessAdHocObligationRequest,
  AssessAdHocObligationResponse,
  BatchAssessCommitResponse,
  BatchAssessPreviewResponse,
  BatchAssessRequest,
  FinancialObligationDto,
  GetObligationsParams,
} from '../types/obligations.types';

/**
 * Queries financial obligations with optional student, status, academic period, and charge type filters.
 * GET /api/finance/obligations
 */
export async function getObligations(
  params?: GetObligationsParams,
): Promise<PaginatedResponse<FinancialObligationDto>> {
  const cleanParams: Record<string, string | number> = {
    pageNumber: params?.pageNumber ?? params?.page ?? 1,
    pageSize: params?.pageSize ?? 20,
  };

  if (params?.studentId) cleanParams.studentId = params.studentId;
  if (params?.status && params.status !== 'all') cleanParams.status = params.status;
  if (params?.academicPeriodId) cleanParams.academicPeriodId = params.academicPeriodId;
  if (params?.chargeType && params.chargeType !== 'all') cleanParams.chargeType = params.chargeType;

  const response = (await apiClient.get('/finance/obligations', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<FinancialObligationDto>;

  return response;
}

/**
 * Executes a batch billing cycle assessment supporting dry-run preview and bulk obligation commit (SCR-FIN-01).
 * POST /api/finance/obligations/batch-assess
 */
export async function batchAssessObligations(
  request: BatchAssessRequest,
): Promise<BatchAssessPreviewResponse | BatchAssessCommitResponse> {
  const response = (await apiClient.post(
    '/finance/obligations/batch-assess',
    request,
  )) as unknown as BatchAssessPreviewResponse | BatchAssessCommitResponse;

  return response;
}

/**
 * Assesses an individual single-student ad-hoc obligation and attaches initial sequence-1 schedule (SCR-FIN-03).
 * POST /api/finance/obligations
 */
export async function assessAdHocObligation(
  request: AssessAdHocObligationRequest,
): Promise<AssessAdHocObligationResponse> {
  const response = (await apiClient.post(
    '/finance/obligations',
    request,
  )) as unknown as AssessAdHocObligationResponse;

  return response;
}
