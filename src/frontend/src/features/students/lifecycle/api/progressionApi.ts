import { apiClient } from '@/shared/services/http';
import type { ProgressionRecommendationDto } from '../types';

/**
 * Cross-domain API client querying authoritative progression recommendations from Academics (SCR-STU-03, P8-S24).
 * Enforces domain boundary: Academics provides recommendation; Students consumes and executes administrative transitions.
 */

/**
 * Queries all progression recommendations for a student.
 * GET /api/v1/academics/students/{studentId}/promotion-recommendations
 */
export async function getStudentProgressionRecommendations(
  studentId: string,
): Promise<ProgressionRecommendationDto[]> {
  try {
    const response = (await apiClient.get(
      `/v1/academics/students/${encodeURIComponent(studentId)}/promotion-recommendations`,
    )) as unknown as ProgressionRecommendationDto[];

    return Array.isArray(response) ? response : [];
  } catch {
    return [];
  }
}

export const progressionApi = {
  getStudentProgressionRecommendations,
};
