import { apiClient } from '@/shared/services/http';
import type {
  AcademicResult,
  ApproveSummativeResultsRequest,
  ApproveSummativeResultsResponse,
  EvaluateLearningRequest,
  PublishAcademicResultsRequest,
  PublishAcademicResultsResponse,
  StudentPublishedResult,
} from '../types';

/**
 * REST API client module for academic evaluation, summative approval, and publication (UC-ACD-06, UC-ACD-16, P4-S08, P4-S09, P4-S10).
 */

/**
 * Retrieves academic results for an assessment, including attempt lineage and scores.
 * GET /api/v1/academics/assessments/{assessmentId}/results
 */
export async function listByAssessment(
  assessmentId: string,
  params?: { status?: string; onlyFinal?: boolean },
): Promise<AcademicResult[]> {
  const queryParams: Record<string, unknown> = {};
  if (params?.status) queryParams.status = params.status;
  if (params?.onlyFinal !== undefined) queryParams.onlyFinal = params.onlyFinal;

  const response = (await apiClient.get(
    `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results`,
    { params: queryParams },
  )) as unknown as AcademicResult[];
  return response;
}

/**
 * Evaluates student or applicant learning and optionally publishes formative feedback autonomously.
 * POST /api/v1/academics/assessments/{assessmentId}/results
 */
export async function evaluate(
  assessmentId: string,
  request: EvaluateLearningRequest,
): Promise<AcademicResult> {
  const response = (await apiClient.post(
    `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results`,
    request,
  )) as unknown as AcademicResult;
  return response;
}

/**
 * Certifies provisional summative results to approved status (Academic Authority / Coordinator).
 * POST /api/v1/academics/assessments/{assessmentId}/results/approve
 */
export async function approve(
  assessmentId: string,
  request?: ApproveSummativeResultsRequest,
): Promise<ApproveSummativeResultsResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results/approve`,
    request ?? {},
  )) as unknown as ApproveSummativeResultsResponse;
  return response;
}

/**
 * Releases approved academic results to published status for portal visibility.
 * POST /api/v1/academics/assessments/{assessmentId}/results/publish
 */
export async function publish(
  assessmentId: string,
  request?: PublishAcademicResultsRequest,
): Promise<PublishAcademicResultsResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results/publish`,
    request ?? {},
  )) as unknown as PublishAcademicResultsResponse;
  return response;
}

/**
 * Retrieves certified published academic results for student/family self-service portals.
 * GET /api/v1/portal/learning/results
 */
export async function getStudentResults(
  academicPeriodId: string,
  studentId?: string,
): Promise<StudentPublishedResult[]> {
  const queryParams: Record<string, string> = {
    academicPeriodId,
  };
  if (studentId) queryParams.studentId = studentId;

  const response = (await apiClient.get(
    '/v1/portal/learning/results',
    { params: queryParams },
  )) as unknown as StudentPublishedResult[];
  return response;
}

export const resultsApi = {
  listByAssessment,
  evaluate,
  approve,
  publish,
  getStudentResults,
};
