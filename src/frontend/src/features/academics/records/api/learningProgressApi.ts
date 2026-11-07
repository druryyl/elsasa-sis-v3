import { apiClient } from '@/shared/services/http';
import type {
  ConcludeParticipationRequest,
  ConcludeParticipationResponse,
  LearningProgress,
  RecordProgressRequest,
} from '../types';

/**
 * REST API client module for student learning progress and participation conclusion (SCR-ACD-07, ACD-WF-11, UC-ACD-12, P7-S19).
 */

/**
 * Queries the learning progress ledger for a student across units in an academic period.
 * GET /api/v1/academics/students/{studentId}/learning-progress
 */
export async function getStudentProgress(
  studentId: string,
  academicPeriodId?: string,
): Promise<LearningProgress[]> {
  const response = (await apiClient.get(
    `/v1/academics/students/${encodeURIComponent(studentId)}/learning-progress`,
    {
      params: academicPeriodId ? { academicPeriodId } : undefined,
    },
  )) as unknown as LearningProgress[];
  return response;
}

/**
 * Queries the participant progress roster for a specific learning unit.
 * GET /api/v1/academics/learning-units/{unitId}/progress
 */
export async function getUnitProgress(
  learningUnitId: string,
  academicPeriodId?: string,
): Promise<LearningProgress[]> {
  const response = (await apiClient.get(
    `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/progress`,
    {
      params: academicPeriodId ? { academicPeriodId } : undefined,
    },
  )) as unknown as LearningProgress[];
  return response;
}

/**
 * Records or updates student advancement, supporting out-of-period completion date stamping and RPL.
 * POST /api/v1/academics/learning-progress
 */
export async function recordProgress(
  request: RecordProgressRequest,
): Promise<LearningProgress> {
  const response = (await apiClient.post(
    '/v1/academics/learning-progress',
    request,
  )) as unknown as LearningProgress;
  return response;
}

/**
 * Formally concludes unit participation for a student, verifying completion criteria.
 * POST /api/v1/academics/learning-progress/conclude
 */
export async function concludeParticipation(
  request: ConcludeParticipationRequest,
): Promise<ConcludeParticipationResponse> {
  const response = (await apiClient.post(
    '/v1/academics/learning-progress/conclude',
    request,
  )) as unknown as ConcludeParticipationResponse;
  return response;
}

export const learningProgressApi = {
  getStudentProgress,
  getUnitProgress,
  recordProgress,
  concludeParticipation,
};
