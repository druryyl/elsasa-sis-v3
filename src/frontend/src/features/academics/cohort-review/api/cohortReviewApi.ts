import { apiClient } from '@/shared/services/http';
import type {
  AcademicAchievement,
  BatchDetermineStandingRequest,
  BatchDetermineStandingResponse,
  BatchIssueProgressionRequest,
  BatchIssueProgressionResponse,
  CohortProgressionProposal,
  CohortStandingProposal,
  ConferAchievementRequest,
  CorrectProgressionRequest,
  CorrectStandingRequest,
  EvaluateCohortParams,
  GetRecommendationsParams,
  GetStandingsParams,
  ProgressionItem,
  StandingItem,
} from '../types';

/**
 * REST API client module for Cohort Academic Review, Standings & Progression (SCR-ACD-09, P7-S19).
 */

/**
 * Executes dynamic cohort standing evaluation applying Completion & Passing Threshold Rule.
 * POST /api/v1/academics/cohort-review/evaluate-standing
 */
export async function evaluateStanding(
  params: EvaluateCohortParams,
): Promise<CohortStandingProposal[]> {
  const response = (await apiClient.post(
    '/v1/academics/cohort-review/evaluate-standing',
    params,
  )) as unknown as CohortStandingProposal[];
  return response;
}

/**
 * Executes dynamic cohort progression recommendation evaluation.
 * POST /api/v1/academics/cohort-review/evaluate-progression
 */
export async function evaluateProgression(
  params: EvaluateCohortParams,
): Promise<CohortProgressionProposal[]> {
  const response = (await apiClient.post(
    '/v1/academics/cohort-review/evaluate-progression',
    params,
  )) as unknown as CohortProgressionProposal[];
  return response;
}

/**
 * Executes atomic batch determination of academic standings for a cohort.
 * POST /api/v1/academics/academic-standings/batch
 */
export async function batchDetermineStanding(
  request: BatchDetermineStandingRequest,
): Promise<BatchDetermineStandingResponse> {
  const response = (await apiClient.post(
    '/v1/academics/academic-standings/batch',
    request,
  )) as unknown as BatchDetermineStandingResponse;
  return response;
}

/**
 * Executes atomic batch issuance of progression recommendations for a cohort.
 * POST /api/v1/academics/promotion-recommendations/batch
 */
export async function batchIssueProgression(
  request: BatchIssueProgressionRequest,
): Promise<BatchIssueProgressionResponse> {
  const response = (await apiClient.post(
    '/v1/academics/promotion-recommendations/batch',
    request,
  )) as unknown as BatchIssueProgressionResponse;
  return response;
}

/**
 * Queries committed academic standings for a cohort or individual student.
 * GET /api/v1/academics/cohort-review/standings OR /api/v1/academics/students/{studentId}/standings
 */
export async function getStandings(
  params: GetStandingsParams,
): Promise<StandingItem[]> {
  if (params.studentId) {
    const response = (await apiClient.get(
      `/v1/academics/students/${encodeURIComponent(params.studentId)}/standings`,
      {
        params: {
          academicPeriodId: params.academicPeriodId,
        },
      },
    )) as unknown as StandingItem[];
    return response;
  }

  const response = (await apiClient.get(
    '/v1/academics/cohort-review/standings',
    {
      params,
    },
  )) as unknown as StandingItem[];
  return response;
}

/**
 * Queries committed progression recommendations for a cohort or individual student.
 * GET /api/v1/academics/cohort-review/promotion-recommendations OR /api/v1/academics/students/{studentId}/promotion-recommendations
 */
export async function getRecommendations(
  params: GetRecommendationsParams,
): Promise<ProgressionItem[]> {
  if (params.studentId) {
    const response = (await apiClient.get(
      `/v1/academics/students/${encodeURIComponent(params.studentId)}/promotion-recommendations`,
      {
        params: {
          academicPeriodId: params.academicPeriodId,
        },
      },
    )) as unknown as ProgressionItem[];
    return response;
  }

  const response = (await apiClient.get(
    '/v1/academics/cohort-review/promotion-recommendations',
    {
      params,
    },
  )) as unknown as ProgressionItem[];
  return response;
}

/**
 * Formally confers an academic honor, subject distinction, or competition award.
 * POST /api/v1/academics/achievements
 */
export async function conferAchievement(
  request: ConferAchievementRequest,
): Promise<AcademicAchievement> {
  const response = (await apiClient.post(
    '/v1/academics/achievements',
    request,
  )) as unknown as AcademicAchievement;
  return response;
}

/**
 * Queries academic achievements conferred within a specific academic period.
 * GET /api/v1/academics/cohort-review/achievements
 */
export async function getPeriodAchievements(
  academicPeriodId: string,
): Promise<AcademicAchievement[]> {
  const response = (await apiClient.get(
    '/v1/academics/cohort-review/achievements',
    {
      params: { academicPeriodId },
    },
  )) as unknown as AcademicAchievement[];
  return response;
}

/**
 * Executes an auditable in-place correction to a certified academic standing record.
 * POST /api/v1/academics/academic-standings/{id}/correct
 */
export async function correctStanding(
  id: string,
  request: CorrectStandingRequest,
): Promise<StandingItem> {
  const response = (await apiClient.post(
    `/v1/academics/academic-standings/${encodeURIComponent(id)}/correct`,
    request,
  )) as unknown as StandingItem;
  return response;
}

/**
 * Executes an auditable in-place correction to a certified progression recommendation record.
 * POST /api/v1/academics/promotion-recommendations/{id}/correct
 */
export async function correctProgression(
  id: string,
  request: CorrectProgressionRequest,
): Promise<ProgressionItem> {
  const response = (await apiClient.post(
    `/v1/academics/promotion-recommendations/${encodeURIComponent(id)}/correct`,
    request,
  )) as unknown as ProgressionItem;
  return response;
}

export const cohortReviewApi = {
  evaluateStanding,
  evaluateProgression,
  batchDetermineStanding,
  batchIssueProgression,
  getStandings,
  getRecommendations,
  conferAchievement,
  getPeriodAchievements,
  correctStanding,
  correctProgression,
};
