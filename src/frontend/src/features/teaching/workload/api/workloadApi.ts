import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  ApproveWorkloadExceptionRequest,
  ApproveWorkloadExceptionResponse,
  CapacityConflictDto,
  GetCapacityConflictsParams,
  GetWorkloadCommitmentsParams,
  GetWorkloadExceptionsParams,
  GetWorkloadPoliciesParams,
  RecalculatePeriodWorkloadRequest,
  RecalculatePeriodWorkloadResponse,
  RequestWorkloadExceptionRequest,
  RequestWorkloadExceptionResponse,
  TeacherWorkloadCommitmentDto,
  WorkloadExceptionDto,
  WorkloadPolicyDto,
} from './types';

/**
 * Teaching Workload API Client module (P7-S22, SCR-TCH-03).
 * Wraps Minimal API endpoints under /api/v1/teaching/workload.
 */

/**
 * Retrieves active institutional workload policies (P3-S09, SCR-TCH-03).
 * GET /api/v1/teaching/workload/policies
 */
export async function getWorkloadPolicies(
  params?: GetWorkloadPoliciesParams,
): Promise<WorkloadPolicyDto[]> {
  const cleanParams: Record<string, string> = {};

  if (params?.academicYearId?.trim()) {
    cleanParams.academicYearId = params.academicYearId.trim();
  }
  if (params?.status?.trim() && params.status !== 'all') {
    cleanParams.status = params.status.trim();
  }

  const response = (await apiClient.get(
    '/v1/teaching/workload/policies',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as WorkloadPolicyDto[];

  return response;
}

/**
 * Retrieves faculty workload capacity monitoring snapshots (P3-S09, SCR-TCH-03).
 * GET /api/v1/teaching/workload/commitments
 */
export async function getWorkloadCommitments(
  params?: GetWorkloadCommitmentsParams,
): Promise<TeacherWorkloadCommitmentDto[]> {
  const cleanParams: Record<string, string> = {};

  if (params?.academicPeriodId?.trim()) {
    cleanParams.academicPeriodId = params.academicPeriodId.trim();
  }
  if (params?.teacherProfileId?.trim()) {
    cleanParams.teacherProfileId = params.teacherProfileId.trim();
  }
  if (params?.status?.trim() && params.status !== 'all') {
    cleanParams.status = params.status.trim();
  }
  if (params?.search?.trim()) {
    cleanParams.search = params.search.trim();
  }

  const response = (await apiClient.get(
    '/v1/teaching/workload/commitments',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as TeacherWorkloadCommitmentDto[];

  return response;
}

/**
 * Recalculates and reconciles workload commitments for an academic period (P3-S09, SCR-TCH-03).
 * POST /api/v1/teaching/workload/recalculate
 */
export async function recalculateWorkload(
  academicPeriodIdOrPayload: string | RecalculatePeriodWorkloadRequest,
): Promise<RecalculatePeriodWorkloadResponse> {
  const payload: RecalculatePeriodWorkloadRequest =
    typeof academicPeriodIdOrPayload === 'string'
      ? { academicPeriodId: academicPeriodIdOrPayload }
      : academicPeriodIdOrPayload;

  const response = (await apiClient.post(
    '/v1/teaching/workload/recalculate',
    payload,
  )) as unknown as RecalculatePeriodWorkloadResponse;

  return response;
}

/**
 * Retrieves formal workload exceptions (P3-S10, SCR-TCH-03).
 * GET /api/v1/teaching/workload/exceptions
 */
export async function getWorkloadExceptions(
  params?: GetWorkloadExceptionsParams,
): Promise<WorkloadExceptionDto[]> {
  const cleanParams: Record<string, string> = {};

  if (params?.academicPeriodId?.trim()) {
    cleanParams.academicPeriodId = params.academicPeriodId.trim();
  }
  if (params?.teacherProfileId?.trim()) {
    cleanParams.teacherProfileId = params.teacherProfileId.trim();
  }
  if (params?.status?.trim() && params.status !== 'all') {
    cleanParams.status = params.status.trim();
  }

  const response = (await apiClient.get(
    '/v1/teaching/workload/exceptions',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as WorkloadExceptionDto[];

  return response;
}

/**
 * Requests or recommends a formal workload exception (P3-S10, SCR-TCH-03).
 * POST /api/v1/teaching/workload/exceptions
 */
export async function requestWorkloadException(
  payload: RequestWorkloadExceptionRequest,
): Promise<RequestWorkloadExceptionResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/workload/exceptions',
    payload,
  )) as unknown as RequestWorkloadExceptionResponse;

  return response;
}

/**
 * Approves a formal workload exception (P3-S10, SCR-TCH-03).
 * POST /api/v1/teaching/workload/exceptions/{id}/approve
 */
export async function approveWorkloadException(
  id: string,
  payload?: ApproveWorkloadExceptionRequest,
): Promise<ApproveWorkloadExceptionResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/workload/exceptions/${encodeURIComponent(id)}/approve`,
    payload ?? {},
  )) as unknown as ApproveWorkloadExceptionResponse;

  return response;
}

/**
 * Retrieves unresolved or historical faculty capacity conflicts (P5-S12, SCR-TCH-03).
 * GET /api/v1/teaching/conflicts
 */
export async function getCapacityConflicts(
  params?: GetCapacityConflictsParams,
): Promise<PaginatedResponse<CapacityConflictDto>> {
  const cleanParams: Record<string, unknown> = {};

  if (params?.academicPeriodId?.trim()) {
    cleanParams.academicPeriodId = params.academicPeriodId.trim();
  }
  if (params?.severity?.trim() && params.severity !== 'all') {
    cleanParams.severity = params.severity.trim();
  }
  if (params?.status?.trim() && params.status !== 'all') {
    cleanParams.status = params.status.trim();
  }
  if (params?.teacherProfileId?.trim()) {
    cleanParams.teacherProfileId = params.teacherProfileId.trim();
  }
  if (params?.page !== undefined) {
    cleanParams.page = params.page;
  }
  if (params?.pageSize !== undefined) {
    cleanParams.pageSize = params.pageSize;
  }

  const response = (await apiClient.get(
    '/v1/teaching/conflicts',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as PaginatedResponse<CapacityConflictDto>;

  return response;
}

export const workloadApi = {
  getWorkloadPolicies,
  getWorkloadCommitments,
  recalculateWorkload,
  getWorkloadExceptions,
  requestWorkloadException,
  approveWorkloadException,
  getCapacityConflicts,
};

export default workloadApi;
