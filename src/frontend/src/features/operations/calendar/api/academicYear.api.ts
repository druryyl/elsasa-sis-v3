import { apiClient } from '@/shared/services/http';

export type AcademicYearStatus = 'draft' | 'approved' | 'active' | 'completed' | 'archived';

export interface AcademicYearDto {
  id: string;
  code: string;
  name: string;
  startDate: string;
  endDate: string;
  status?: AcademicYearStatus | string | undefined;
  approvedBy?: string | null | undefined;
  approvedDate?: string | null | undefined;
  notes?: string | null | undefined;
  createdAt?: string | undefined;
  createdBy?: string | null | undefined;
  updatedAt?: string | null | undefined;
  updatedBy?: string | null | undefined;
  isActive?: boolean | undefined;
}

export interface CreateAcademicYearPayload {
  code: string;
  name: string;
  startDate: string;
  endDate: string;
  notes?: string | null | undefined;
}

export interface UpdateAcademicYearPayload {
  name: string;
  code?: string | undefined;
  startDate: string;
  endDate: string;
  notes?: string | null | undefined;
}

export interface ApproveAcademicYearPayload {
  approvedBy?: string | null | undefined;
  notes?: string | null | undefined;
}

export interface ApplyPresetResultDto {
  academicYearId: string;
  presetName: string;
  periodsCreated: number;
}

export interface HealthCheckResult {
  status: string;
}

/**
 * Normalizes an AcademicYearDto to guarantee backward compatibility with legacy boolean flags.
 */
function normalizeAcademicYear(dto: AcademicYearDto): AcademicYearDto {
  if (dto && typeof dto === 'object') {
    return {
      ...dto,
      isActive: dto.isActive !== undefined ? dto.isActive : dto.status === 'active',
    };
  }
  return dto;
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
 * Retrieves list of academic years, optionally filtered by status.
 * GET /api/operations/academic-years
 */
export async function getAcademicYears(params?: {
  status?: string | undefined;
}): Promise<AcademicYearDto[]> {
  const response = (await apiClient.get('/operations/academic-years', {
    params,
  })) as unknown as AcademicYearDto[];

  return Array.isArray(response) ? response.map(normalizeAcademicYear) : [];
}

/**
 * Retrieves a single academic year by ID.
 * GET /api/operations/academic-years/{id}
 */
export async function getAcademicYearById(id: string): Promise<AcademicYearDto> {
  const response = (await apiClient.get(
    `/operations/academic-years/${encodeURIComponent(id)}`,
  )) as unknown as AcademicYearDto;

  return normalizeAcademicYear(response);
}

/**
 * Creates a new academic year in draft status.
 * POST /api/operations/academic-years
 */
export async function createAcademicYear(
  payload: CreateAcademicYearPayload,
): Promise<AcademicYearDto> {
  const response = (await apiClient.post(
    '/operations/academic-years',
    payload,
  )) as unknown as AcademicYearDto;

  return normalizeAcademicYear(response);
}

/**
 * Updates an existing draft academic year.
 * PUT /api/operations/academic-years/{id}
 */
export async function updateAcademicYear(
  id: string,
  payload: UpdateAcademicYearPayload,
): Promise<AcademicYearDto> {
  const response = (await apiClient.put(
    `/operations/academic-years/${encodeURIComponent(id)}`,
    payload,
  )) as unknown as AcademicYearDto;

  return normalizeAcademicYear(response);
}

/**
 * Transitions an academic year from draft to approved status.
 * POST /api/operations/academic-years/{id}/approve
 */
export async function approveAcademicYear(
  id: string,
  payload?: ApproveAcademicYearPayload | undefined,
): Promise<AcademicYearDto> {
  const response = (await apiClient.post(
    `/operations/academic-years/${encodeURIComponent(id)}/approve`,
    payload ?? {},
  )) as unknown as AcademicYearDto;

  return normalizeAcademicYear(response);
}

/**
 * Transitions an approved academic year to active status, archiving previously active years.
 * POST /api/operations/academic-years/{id}/activate
 */
export async function activateAcademicYear(id: string): Promise<AcademicYearDto> {
  const response = (await apiClient.post(
    `/operations/academic-years/${encodeURIComponent(id)}/activate`,
    {},
  )) as unknown as AcademicYearDto;

  return normalizeAcademicYear(response);
}

/**
 * Applies the standard Indonesian 2-semester preset scaffolding to an academic year.
 * POST /api/operations/academic-years/{id}/apply-preset
 */
export async function applyPreset(id: string): Promise<ApplyPresetResultDto> {
  const response = (await apiClient.post(
    `/operations/academic-years/${encodeURIComponent(id)}/apply-preset`,
    {},
  )) as unknown as ApplyPresetResultDto;

  return response;
}
