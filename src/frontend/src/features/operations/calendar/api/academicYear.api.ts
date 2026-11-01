import { apiClient } from '@/shared/services/http';

export interface CreateAcademicYearPayload {
  code: string;
  name: string;
  startDate: string;
  endDate: string;
}

export interface AcademicYearDto {
  id: string;
  code: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface HealthCheckResult {
  status: string;
}

/**
 * Checks backend API connectivity via GET /health.
 */
export async function checkBackendHealth(): Promise<{ status: string }> {
  const response = (await apiClient.get('/health', {
    baseURL: '',
  })) as unknown;

  if (typeof response === 'string') {
    return { status: response };
  }

  if (typeof response === 'object' && response !== null && 'status' in response) {
    return { status: String((response as Record<string, unknown>).status) };
  }

  return { status: 'Healthy' };
}

/**
 * Submits payload to create a new academic year via POST /api/operations/academic-years.
 *
 * @param payload - Academic year creation payload matching CreateAcademicYearRequest
 * @returns Created academic year entity data (AcademicYearDto)
 * @throws ApiError on HTTP 422 ProblemDetails or other failure responses
 */
export async function createAcademicYear(
  payload: CreateAcademicYearPayload,
): Promise<AcademicYearDto> {
  const response = (await apiClient.post(
    '/operations/academic-years',
    payload,
  )) as unknown as AcademicYearDto;

  return response;
}
