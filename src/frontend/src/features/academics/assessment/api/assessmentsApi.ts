import { apiClient } from '@/shared/services/http';
import type {
  Assessment,
  CancelAssessmentResponse,
  ConductAssessmentRequest,
  ConductAssessmentResponse,
  CreateAssessmentRequest,
  CreateAssessmentResponse,
  GetAssessmentsFilter,
  UpdateAssessmentRequest,
  UpdateAssessmentResponse,
} from '../types';

/**
 * REST API client module for assessment planning, conduct, and management (UC-ACD-05, P2-S04, P2-S05).
 */

/**
 * Retrieves assessments organized under a specific learning unit.
 * GET /api/v1/academics/learning-units/{unitId}/assessments
 */
export async function listByUnit(
  unitId: string,
  filter?: GetAssessmentsFilter,
): Promise<Assessment[]> {
  const params: Record<string, string> = {};
  if (filter?.status) params.status = filter.status;
  if (filter?.assessmentType) params.assessmentType = filter.assessmentType;
  if (filter?.fromDate) params.fromDate = filter.fromDate;
  if (filter?.toDate) params.toDate = filter.toDate;

  const response = (await apiClient.get(
    `/v1/academics/learning-units/${encodeURIComponent(unitId)}/assessments`,
    { params },
  )) as unknown as Assessment[];
  return response;
}

/**
 * Retrieves detailed assessment specification and participant counts by ID.
 * GET /api/v1/academics/assessments/{id}
 */
export async function getById(id: string): Promise<Assessment> {
  const response = (await apiClient.get(
    `/v1/academics/assessments/${encodeURIComponent(id)}`,
  )) as unknown as Assessment;
  return response;
}

/**
 * Plans and creates a new assessment event under a learning unit.
 * POST /api/v1/academics/assessments
 */
export async function create(
  request: CreateAssessmentRequest,
): Promise<CreateAssessmentResponse> {
  const response = (await apiClient.post(
    '/v1/academics/assessments',
    request,
  )) as unknown as CreateAssessmentResponse;
  return response;
}

/**
 * Updates an existing planned assessment.
 * PUT /api/v1/academics/assessments/{id}
 */
export async function update(
  id: string,
  request: UpdateAssessmentRequest,
): Promise<UpdateAssessmentResponse> {
  const response = (await apiClient.put(
    `/v1/academics/assessments/${encodeURIComponent(id)}`,
    request,
  )) as unknown as UpdateAssessmentResponse;
  return response;
}

/**
 * Marks a planned assessment as conducted.
 * POST /api/v1/academics/assessments/{id}/conduct
 */
export async function conduct(
  id: string,
  request?: ConductAssessmentRequest,
): Promise<ConductAssessmentResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assessments/${encodeURIComponent(id)}/conduct`,
    request ?? {},
  )) as unknown as ConductAssessmentResponse;
  return response;
}

/**
 * Cancels a planned assessment with mandatory rationale.
 * POST /api/v1/academics/assessments/{id}/cancel
 */
export async function cancel(
  id: string,
  reason: string,
): Promise<CancelAssessmentResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assessments/${encodeURIComponent(id)}/cancel`,
    { reason },
  )) as unknown as CancelAssessmentResponse;
  return response;
}

export const assessmentsApi = {
  listByUnit,
  getById,
  create,
  update,
  conduct,
  cancel,
};
