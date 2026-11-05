import { apiClient } from '@/shared/services/http';
import type {
  Assignment,
  AssignmentSubmission,
  CloseAssignmentResponse,
  CreateAssignmentRequest,
  CreateAssignmentResponse,
  RecordExceptionRequest,
  RecordExceptionResponse,
  ReleaseAssignmentResponse,
} from '../types';

/**
 * Creates a coursework assignment under a learning activity (P5-S10, UC-ACD-04).
 * POST /api/v1/academics/learning-activities/{activityId}/assignments
 */
export async function createAssignment(
  activityId: string,
  request: CreateAssignmentRequest,
): Promise<CreateAssignmentResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/assignments`,
    request,
  )) as unknown as CreateAssignmentResponse;
  return response;
}

/**
 * Releases a coursework assignment, publishing it to students (P5-S10).
 * POST /api/v1/academics/assignments/{id}/release
 */
export async function releaseAssignment(
  id: string,
): Promise<ReleaseAssignmentResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assignments/${encodeURIComponent(id)}/release`,
    {},
  )) as unknown as ReleaseAssignmentResponse;
  return response;
}

/**
 * Closes an assignment preventing further standard submissions (P5-S10).
 * POST /api/v1/academics/assignments/{id}/close
 */
export async function closeAssignment(
  id: string,
): Promise<CloseAssignmentResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assignments/${encodeURIComponent(id)}/close`,
    {},
  )) as unknown as CloseAssignmentResponse;
  return response;
}

/**
 * Retrieves coursework assignments issued under a specific learning activity (P5-S10).
 * GET /api/v1/academics/learning-activities/{activityId}/assignments
 */
export async function getAssignmentsByActivity(
  activityId: string,
): Promise<Assignment[]> {
  const response = (await apiClient.get(
    `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/assignments`,
  )) as unknown as Assignment[];
  return response;
}

/**
 * Retrieves coursework assignments across a learning unit (P5-S10).
 * GET /api/v1/academics/learning-units/{unitId}/assignments
 */
export async function getAssignmentsByUnit(
  unitId: string,
): Promise<Assignment[]> {
  const response = (await apiClient.get(
    `/v1/academics/learning-units/${encodeURIComponent(unitId)}/assignments`,
  )) as unknown as Assignment[];
  return response;
}

/**
 * Retrieves submissions roster for an assignment with on-the-fly missing projection (SCR-ACD-02, TD-06, P5-S12).
 * GET /api/v1/academics/assignments/{assignmentId}/submissions
 */
export async function getAssignmentSubmissions(
  assignmentId: string,
): Promise<AssignmentSubmission[]> {
  const response = (await apiClient.get(
    `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/submissions`,
  )) as unknown as AssignmentSubmission[];
  return response;
}

/**
 * Records an administrative policy exception ('excused' or 'exempted') for a student (P5-S11, TD-06).
 * POST /api/v1/academics/assignments/{assignmentId}/submissions/exception
 */
export async function recordSubmissionException(
  assignmentId: string,
  request: RecordExceptionRequest,
): Promise<RecordExceptionResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/submissions/exception`,
    request,
  )) as unknown as RecordExceptionResponse;
  return response;
}
