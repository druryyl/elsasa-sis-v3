import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  ActiveStudentLearningPlan,
  AddAdjustmentRequest,
  AddUnitRequest,
  ApprovePlanRequest,
  ArchivePlanRequest,
  CreatePlanRequest,
  GetLearningPlansParams,
  RevisePlanRequest,
  SlpApproval,
  StudentLearningPlan,
  StudentLearningPlanListItem,
  SyncParticipationsResponse,
  UpdatePlanRequest,
} from '../types';

/**
 * REST API client module for Student Learning Plans (SCR-ACD-04, UC-ACD-09, UC-ACD-10, P6-S17).
 */

/**
 * Queries paginated and filterable learning plan roster for master sidebar navigation.
 * GET /api/v1/academics/learning-plans
 */
export async function getLearningPlans(
  params?: GetLearningPlansParams,
): Promise<PaginatedResponse<StudentLearningPlanListItem>> {
  const queryParams: Record<string, string | number> = {};

  if (params?.academicPeriodId) {
    queryParams.academicPeriodId = params.academicPeriodId;
  }
  if (params?.status) {
    queryParams.status = params.status;
  }
  if (params?.studentSearch?.trim()) {
    queryParams.studentSearch = params.studentSearch.trim();
  }
  if (typeof params?.page === 'number' && params.page > 0) {
    queryParams.page = params.page;
  }
  if (typeof params?.pageSize === 'number' && params.pageSize > 0) {
    queryParams.pageSize = params.pageSize;
  }

  const response = (await apiClient.get('/v1/academics/learning-plans', {
    params: queryParams,
  })) as unknown as PaginatedResponse<StudentLearningPlanListItem>;

  return response;
}

/**
 * Retrieves the full detail of a student learning plan including constituent units, adjustments, and approvals.
 * GET /api/v1/academics/learning-plans/{id}
 */
export async function getLearningPlanById(
  id: string,
): Promise<StudentLearningPlan> {
  const response = (await apiClient.get(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}`,
  )) as unknown as StudentLearningPlan;
  return response;
}

/**
 * Retrieves the currently active learning plan for a student in a specific academic period.
 * Returns null if no active plan exists (HTTP 404).
 * GET /api/v1/academics/students/{studentId}/learning-plans/active
 */
export async function getActiveLearningPlan(
  studentId: string,
  academicPeriodId: string,
): Promise<ActiveStudentLearningPlan | null> {
  try {
    const response = (await apiClient.get(
      `/v1/academics/students/${encodeURIComponent(studentId)}/learning-plans/active`,
      {
        params: { academicPeriodId },
      },
    )) as unknown as ActiveStudentLearningPlan;
    return response ?? null;
  } catch (error: unknown) {
    if (
      typeof error === 'object' &&
      error !== null &&
      'statusCode' in error &&
      (error as { statusCode: number }).statusCode === 404
    ) {
      return null;
    }
    throw error;
  }
}

/**
 * Formulates a new initial draft Student Learning Plan (v1) for an enrolled student.
 * POST /api/v1/academics/learning-plans
 */
export async function createPlan(
  request: CreatePlanRequest,
): Promise<{ id: string }> {
  const response = (await apiClient.post(
    '/v1/academics/learning-plans',
    request,
  )) as unknown as { id: string };
  return response;
}

/**
 * Modifies plan-level notes and metadata on a draft plan.
 * PUT /api/v1/academics/learning-plans/{id}
 */
export async function updatePlan(
  id: string,
  request: UpdatePlanRequest,
): Promise<void> {
  await apiClient.put(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}`,
    request,
  );
}

/**
 * Retires an active or revised plan to 'archived' or marks a completed plan as 'completed'.
 * POST /api/v1/academics/learning-plans/{id}/archive
 */
export async function archivePlan(
  id: string,
  request?: ArchivePlanRequest,
): Promise<void> {
  await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}/archive`,
    request ?? {},
  );
}

/**
 * Links a constituent learning unit with sequence ordering and adaptation notes to a draft plan.
 * POST /api/v1/academics/learning-plans/{id}/units
 */
export async function addUnit(
  planId: string,
  request: AddUnitRequest,
): Promise<{ id: string }> {
  const response = (await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(planId)}/units`,
    request,
  )) as unknown as { id: string };
  return response;
}

/**
 * Removes a constituent learning unit from a draft plan.
 * DELETE /api/v1/academics/learning-plans/{id}/units/{unitId}
 */
export async function removeUnit(
  planId: string,
  unitId: string,
): Promise<void> {
  await apiClient.delete(
    `/v1/academics/learning-plans/${encodeURIComponent(planId)}/units/${encodeURIComponent(unitId)}`,
  );
}

/**
 * Records an academic plan adjustment (accommodation, substitution, exemption) on a draft plan.
 * POST /api/v1/academics/learning-plans/{id}/adjustments
 */
export async function addAdjustment(
  planId: string,
  request: AddAdjustmentRequest,
): Promise<{ id: string }> {
  const response = (await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(planId)}/adjustments`,
    request,
  )) as unknown as { id: string };
  return response;
}

/**
 * Removes an academic plan adjustment from a draft plan.
 * DELETE /api/v1/academics/learning-plans/{id}/adjustments/{adjustmentId}
 */
export async function removeAdjustment(
  planId: string,
  adjustmentId: string,
): Promise<void> {
  await apiClient.delete(
    `/v1/academics/learning-plans/${encodeURIComponent(planId)}/adjustments/${encodeURIComponent(adjustmentId)}`,
  );
}

/**
 * Submits a draft plan for Academic Authority review.
 * POST /api/v1/academics/learning-plans/{id}/submit
 */
export async function submitPlan(
  id: string,
): Promise<void> {
  await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}/submit`,
  );
}

/**
 * Executes formal Academic Authority approval (initial or revision approval).
 * POST /api/v1/academics/learning-plans/{id}/approve
 */
export async function approvePlan(
  id: string,
  request: ApprovePlanRequest,
): Promise<SlpApproval> {
  const response = (await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}/approve`,
    request,
  )) as unknown as SlpApproval;
  return response;
}

/**
 * Clones an active plan into a new draft revision (v+1) leaving active plan operative.
 * POST /api/v1/academics/learning-plans/{id}/revise
 */
export async function revisePlan(
  id: string,
  request: RevisePlanRequest,
): Promise<{ id: string }> {
  const response = (await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}/revise`,
    request,
  )) as unknown as { id: string };
  return response;
}

/**
 * Explicitly synchronizes approved SLP constituent units into academic_participations.
 * POST /api/v1/academics/learning-plans/{id}/sync-participations
 */
export async function syncParticipations(
  id: string,
): Promise<SyncParticipationsResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-plans/${encodeURIComponent(id)}/sync-participations`,
  )) as unknown as SyncParticipationsResponse;
  return response;
}

// Aliases matching specific domain naming conventions
export const createStudentLearningPlan = createPlan;
export const updateStudentLearningPlan = updatePlan;
export const archiveStudentLearningPlan = archivePlan;
export const addSlpLearningUnit = addUnit;
export const removeSlpLearningUnit = removeUnit;
export const addAcademicPlanAdjustment = addAdjustment;
export const removeAcademicPlanAdjustment = removeAdjustment;
export const submitStudentLearningPlan = submitPlan;
export const approveStudentLearningPlan = approvePlan;
export const reviseStudentLearningPlan = revisePlan;
export const syncSlpParticipations = syncParticipations;

export const learningPlansApi = {
  getLearningPlans,
  getLearningPlanById,
  getActiveLearningPlan,
  createPlan,
  createStudentLearningPlan,
  updatePlan,
  updateStudentLearningPlan,
  archivePlan,
  archiveStudentLearningPlan,
  addUnit,
  addSlpLearningUnit,
  removeUnit,
  removeSlpLearningUnit,
  addAdjustment,
  addAcademicPlanAdjustment,
  removeAdjustment,
  removeAcademicPlanAdjustment,
  submitPlan,
  submitStudentLearningPlan,
  approvePlan,
  approveStudentLearningPlan,
  revisePlan,
  reviseStudentLearningPlan,
  syncParticipations,
  syncSlpParticipations,
};
