import { apiClient } from '@/shared/services/http';

export type OperationalPeriodStatus = 'draft' | 'approved' | 'active' | 'closed';

export interface OperationalPeriodDto {
  id: string;
  academicYearId: string;
  periodCode: string;
  name: string;
  periodType: string;
  startDate: string;
  endDate: string;
  parentPeriodId: string | null;
  campusContext: string;
  status: OperationalPeriodStatus | string;
  notes?: string | null | undefined;
  createdAt?: string | undefined;
  createdBy?: string | null | undefined;
  updatedAt?: string | null | undefined;
  updatedBy?: string | null | undefined;
  children?: OperationalPeriodDto[] | undefined;
}

export interface CreateOperationalPeriodPayload {
  periodCode: string;
  name: string;
  periodType: string;
  startDate: string;
  endDate: string;
  parentPeriodId?: string | null | undefined;
  campusContext?: string | undefined;
  notes?: string | null | undefined;
}

export interface UpdateOperationalPeriodPayload {
  name: string;
  startDate: string;
  endDate: string;
  campusContext?: string | undefined;
  notes?: string | null | undefined;
}

/**
 * Retrieves all operational periods for an academic year.
 * GET /api/operations/academic-years/{academicYearId}/periods
 */
export async function getPeriods(academicYearId: string): Promise<OperationalPeriodDto[]> {
  const response = (await apiClient.get(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/periods`,
  )) as unknown as OperationalPeriodDto[] | { periods?: OperationalPeriodDto[] };

  if (Array.isArray(response)) {
    return response;
  }
  if (
    response &&
    typeof response === 'object' &&
    Array.isArray((response as { periods?: OperationalPeriodDto[] }).periods)
  ) {
    return (response as { periods: OperationalPeriodDto[] }).periods;
  }
  return [];
}

/**
 * Creates a new operational period within an academic year.
 * POST /api/operations/academic-years/{academicYearId}/periods
 */
export async function createPeriod(
  academicYearId: string,
  payload: CreateOperationalPeriodPayload,
): Promise<OperationalPeriodDto> {
  const response = (await apiClient.post(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/periods`,
    payload,
  )) as unknown as OperationalPeriodDto;

  return response;
}

/**
 * Updates an operational period's details.
 * PUT /api/operations/periods/{id}
 */
export async function updatePeriod(
  id: string,
  payload: UpdateOperationalPeriodPayload,
): Promise<OperationalPeriodDto> {
  const response = (await apiClient.put(
    `/operations/periods/${encodeURIComponent(id)}`,
    payload,
  )) as unknown as OperationalPeriodDto;

  return response;
}

/**
 * Formally closes an operational period.
 * POST /api/operations/periods/{id}/close
 */
export async function closePeriod(id: string): Promise<OperationalPeriodDto> {
  const response = (await apiClient.post(
    `/operations/periods/${encodeURIComponent(id)}/close`,
    {},
  )) as unknown as OperationalPeriodDto;

  return response;
}

/**
 * Formally activates an operational period.
 * POST /api/operations/periods/{id}/activate
 */
export async function activatePeriod(id: string): Promise<OperationalPeriodDto> {
  const response = (await apiClient.post(
    `/operations/periods/${encodeURIComponent(id)}/activate`,
    {},
  )) as unknown as OperationalPeriodDto;

  return response;
}
