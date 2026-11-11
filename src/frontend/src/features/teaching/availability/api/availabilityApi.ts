import { apiClient } from '@/shared/services/http';
import type {
  CancelUnavailabilityWindowResponse,
  CreateUnavailabilityWindowRequest,
  CreateUnavailabilityWindowResponse,
  DeclareTeacherAvailabilityRequest,
  DeclareTeacherAvailabilityResponse,
  GetTeacherUnavailabilityWindowsParams,
  TeacherAvailabilityDto,
  TeacherUnavailabilityWindowDto,
} from './types';

/**
 * Teaching Availability API Client module (P7-S23, SCR-TCH-04).
 * Wraps Minimal API endpoints under /api/v1/teaching.
 */

/**
 * Retrieves declared baseline weekly availability and recurring blocks for a teacher (P3-S08, SCR-TCH-04).
 * GET /api/v1/teaching/teachers/{teacherId}/availability
 */
export async function getTeacherAvailability(
  teacherId: string,
  academicPeriodId?: string,
): Promise<TeacherAvailabilityDto> {
  const params: Record<string, string> = {};
  if (academicPeriodId) {
    params.academicPeriodId = academicPeriodId;
  }

  const response = (await apiClient.get(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/availability`,
    Object.keys(params).length > 0 ? { params } : undefined,
  )) as unknown as TeacherAvailabilityDto;

  return response;
}

/**
 * Declares or supersedes weekly recurring teaching availability and baseline capacity (P3-S08, SCR-TCH-04).
 * POST /api/v1/teaching/teachers/{teacherId}/availability
 */
export async function declareTeacherAvailability(
  teacherId: string,
  payload: DeclareTeacherAvailabilityRequest,
): Promise<DeclareTeacherAvailabilityResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/availability`,
    payload,
  )) as unknown as DeclareTeacherAvailabilityResponse;

  return response;
}

/**
 * Retrieves a list of temporary leave, training, and blackout windows for a teacher (P3-S08, SCR-TCH-04).
 * GET /api/v1/teaching/teachers/{teacherId}/unavailability
 */
export async function getTeacherUnavailabilityWindows(
  teacherId: string,
  params?: GetTeacherUnavailabilityWindowsParams,
): Promise<TeacherUnavailabilityWindowDto[]> {
  const cleanParams: Record<string, string> = {};

  if (params?.status && params.status !== 'all') {
    cleanParams.status = params.status;
  }
  if (params?.fromDate?.trim()) {
    cleanParams.fromDate = params.fromDate.trim();
  }
  if (params?.toDate?.trim()) {
    cleanParams.toDate = params.toDate.trim();
  }

  const response = (await apiClient.get(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as TeacherUnavailabilityWindowDto[];

  return response;
}

/**
 * Alias for getTeacherUnavailabilityWindows.
 */
export const getUnavailabilityWindows = getTeacherUnavailabilityWindows;

/**
 * Records a temporary leave or blackout window restricting teacher availability (P3-S08, SCR-TCH-04).
 * POST /api/v1/teaching/teachers/{teacherId}/unavailability
 */
export async function createUnavailabilityWindow(
  teacherId: string,
  payload: CreateUnavailabilityWindowRequest,
): Promise<CreateUnavailabilityWindowResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
    payload,
  )) as unknown as CreateUnavailabilityWindowResponse;

  return response;
}

/**
 * Cancels an active temporary leave or blackout window (P3-S08, SCR-TCH-04).
 * POST /api/v1/teaching/unavailability/{id}/cancel
 */
export async function cancelUnavailabilityWindow(
  windowId: string,
): Promise<CancelUnavailabilityWindowResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/unavailability/${encodeURIComponent(windowId)}/cancel`,
  )) as unknown as CancelUnavailabilityWindowResponse;

  return response;
}

export const availabilityApi = {
  getTeacherAvailability,
  declareTeacherAvailability,
  getTeacherUnavailabilityWindows,
  getUnavailabilityWindows,
  createUnavailabilityWindow,
  cancelUnavailabilityWindow,
};

export default availabilityApi;
