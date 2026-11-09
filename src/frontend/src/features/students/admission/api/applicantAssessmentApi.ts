import { apiClient } from '@/shared/services/http';

/**
 * Diagnostic entrance assessment score from Academics module for admissions review (SCR-STU-01, GAP-07, P9-S23).
 */
export interface ApplicantEntranceAssessmentScoreDto {
  resultId: string;
  assessmentId: string;
  assessmentTitle: string;
  assessmentType?: string;
  applicantId: string;
  scoreNumeric?: number | null;
  scoreMax?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  evaluationModel: string;
  resultStatus: string;
  evaluatedBy: string;
  evaluatedDate: string;
  assessorComments?: string | null;
  notes?: string | null;
}

/**
 * Queries authorized diagnostic entrance assessment results for an admissions applicant from the Academics module.
 * GET /api/v1/academics/applicants/{applicantId}/assessment-score
 */
export async function getApplicantAssessmentScores(
  applicantId: string,
): Promise<ApplicantEntranceAssessmentScoreDto[]> {
  try {
    const response = (await apiClient.get(
      `/v1/academics/applicants/${encodeURIComponent(applicantId)}/assessment-score`,
    )) as unknown as ApplicantEntranceAssessmentScoreDto | ApplicantEntranceAssessmentScoreDto[];

    if (Array.isArray(response)) {
      return response;
    }
    if (response && typeof response === 'object' && 'resultId' in response) {
      return [response];
    }
    return [];
  } catch {
    return [];
  }
}

export const applicantAssessmentApi = {
  getApplicantAssessmentScores,
};
