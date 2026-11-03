import { apiClient } from '@/shared/services/http';
import type {
  EnrollAcceptedApplicantPayload,
  EnrollAcceptedApplicantResponse,
} from '../types/enrollment.types';

/**
 * Enrolls an accepted applicant as an authoritative student (ADR-004, ADR-005, P3-S04).
 * POST /api/students/enrollments
 */
export async function enrollAcceptedApplicant(
  payload: EnrollAcceptedApplicantPayload,
): Promise<EnrollAcceptedApplicantResponse> {
  const response = (await apiClient.post(
    '/students/enrollments',
    payload,
  )) as unknown as EnrollAcceptedApplicantResponse;

  return response;
}
