import { apiClient } from '@/shared/services/http';
import type {
  AssessmentEvidence,
  RecordEvidenceRequest,
} from '../types';

/**
 * REST API client module for assessment evidence collection (ACD-WF-08, UC-ACD-07, P3-S06).
 */

/**
 * Retrieves collected evidence roster for an assessment, optionally filtered by candidate.
 * GET /api/v1/academics/assessments/{assessmentId}/evidence
 */
export async function listByAssessment(
  assessmentId: string,
  params?: { studentId?: string; applicantId?: string },
): Promise<AssessmentEvidence[]> {
  const queryParams: Record<string, string> = {};
  if (params?.studentId) queryParams.studentId = params.studentId;
  if (params?.applicantId) queryParams.applicantId = params.applicantId;

  const response = (await apiClient.get(
    `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/evidence`,
    { params: queryParams },
  )) as unknown as AssessmentEvidence[];
  return response;
}

/**
 * Records raw student or applicant learning evidence linked to an assessment.
 * POST /api/v1/academics/assessments/{assessmentId}/evidence
 */
export async function record(
  assessmentId: string,
  request: RecordEvidenceRequest,
): Promise<AssessmentEvidence> {
  const response = (await apiClient.post(
    `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/evidence`,
    request,
  )) as unknown as AssessmentEvidence;
  return response;
}

export const evidenceApi = {
  listByAssessment,
  record,
};
