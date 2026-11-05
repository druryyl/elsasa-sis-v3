import { apiClient } from '@/shared/services/http';
import type {
  AcademicRecordCorrection,
  AcademicResult,
  CorrectResultRequest,
} from '../types';

/**
 * REST API client module for auditable academic record corrections (UC-ACD-19, TD-06, P6-S14).
 */

/**
 * Authorizes an in-place correction to an approved/published academic result with audit logging.
 * POST /api/v1/academics/academic-results/{id}/correct
 */
export async function correctResult(
  id: string,
  request: CorrectResultRequest,
): Promise<AcademicResult> {
  const response = (await apiClient.post(
    `/v1/academics/academic-results/${encodeURIComponent(id)}/correct`,
    request,
  )) as unknown as AcademicResult;
  return response;
}

/**
 * Retrieves the complete chronological audit trail of corrections for an academic result.
 * GET /api/v1/academics/academic-results/{id}/corrections
 */
export async function getCorrectionHistory(
  id: string,
): Promise<AcademicRecordCorrection[]> {
  const response = (await apiClient.get(
    `/v1/academics/academic-results/${encodeURIComponent(id)}/corrections`,
  )) as unknown as AcademicRecordCorrection[];
  return response;
}

export const correctionsApi = {
  correctResult,
  getCorrectionHistory,
};
