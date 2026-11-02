import { apiClient } from '@/shared/services/http';

export type SchoolClosureStatus =
  | 'proposed'
  | 'authorized'
  | 'in_effect'
  | 'lifted'
  | 'cancelled';

export interface SchoolClosureDto {
  id: string;
  academicYearId: string;
  closureNumber: string;
  reasonCategory: string;
  reasonDetails: string;
  startDate: string;
  endDate: string;
  affectedCampus: string;
  affectedPopulation: string;
  status: SchoolClosureStatus | string;
  authorizedBy: string;
  authorizedDate: string;
  communicationStatus: string;
  recoveryPlanNotes?: string | null | undefined;
  createdAt?: string | undefined;
  createdBy?: string | null | undefined;
  updatedAt?: string | null | undefined;
  updatedBy?: string | null | undefined;
}

export interface DeclareSchoolClosurePayload {
  academicYearId: string;
  reasonCategory: string;
  reasonDetails: string;
  startDate: string;
  endDate: string;
  affectedCampus?: string | undefined;
  affectedPopulation?: string | undefined;
  authorizedBy?: string | undefined;
  recoveryPlanNotes?: string | null | undefined;
}

/**
 * Retrieves all campus closures declared for an academic year.
 * GET /api/operations/academic-years/{academicYearId}/closures
 */
export async function getClosures(academicYearId: string): Promise<SchoolClosureDto[]> {
  const response = (await apiClient.get(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/closures`,
  )) as unknown as SchoolClosureDto[];

  return Array.isArray(response) ? response : [];
}

/**
 * Declares a school closure atomically across closures, calendar entries, and amendments.
 * POST /api/operations/closures (UC-OPS-12)
 */
export async function declareClosure(
  payload: DeclareSchoolClosurePayload,
): Promise<SchoolClosureDto> {
  const response = (await apiClient.post(
    '/operations/closures',
    payload,
  )) as unknown as SchoolClosureDto;

  return response;
}

/**
 * Lifts an existing school closure.
 * POST /api/operations/closures/{id}/lift
 */
export async function liftClosure(id: string): Promise<SchoolClosureDto> {
  const response = (await apiClient.post(
    `/operations/closures/${encodeURIComponent(id)}/lift`,
    {},
  )) as unknown as SchoolClosureDto;

  return response;
}
