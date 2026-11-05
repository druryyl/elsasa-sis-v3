import { apiClient } from '@/shared/services/http';
import type {
  AcademicParticipation,
  BatchRegisterCohortRequest,
  BatchRegisterCohortResponse,
  ConcludeParticipationRequest,
  ConcludeParticipationResponse,
  RecognizePriorCreditRequest,
  RecognizePriorCreditResponse,
  RegisterParticipationRequest,
  RegisterParticipationResponse,
  StudentParticipation,
  StudentParticipationsFilter,
  UnitParticipationsFilter,
  UpdateParticipationStatusRequest,
  UpdateParticipationStatusResponse,
} from '../types';

/**
 * Registers a cohort batch of students into learning units (P2-S04, TD-02).
 * POST /api/v1/academics/participations/batch-cohort
 */
export async function batchRegisterCohortParticipation(
  request: BatchRegisterCohortRequest,
): Promise<BatchRegisterCohortResponse> {
  const response = (await apiClient.post(
    '/v1/academics/participations/batch-cohort',
    request,
  )) as unknown as BatchRegisterCohortResponse;
  return response;
}

/**
 * Registers an individual student academic participation (P2-S04, TD-02).
 * POST /api/v1/academics/participations
 */
export async function registerParticipation(
  request: RegisterParticipationRequest,
): Promise<RegisterParticipationResponse> {
  const response = (await apiClient.post(
    '/v1/academics/participations',
    request,
  )) as unknown as RegisterParticipationResponse;
  return response;
}

/**
 * Transitions an academic participation status (P2-S05).
 * PUT /api/v1/academics/participations/{id}/status
 */
export async function updateParticipationStatus(
  id: string,
  request: UpdateParticipationStatusRequest,
): Promise<UpdateParticipationStatusResponse> {
  const response = (await apiClient.put(
    `/v1/academics/participations/${encodeURIComponent(id)}/status`,
    request,
  )) as unknown as UpdateParticipationStatusResponse;
  return response;
}

/**
 * Recognizes transfer or prior learning credit (P2-S05, GAP-08, UC-ACD-11).
 * POST /api/v1/academics/participations/recognize-prior
 */
export async function recognizePriorCredit(
  request: RecognizePriorCreditRequest,
): Promise<RecognizePriorCreditResponse> {
  const response = (await apiClient.post(
    '/v1/academics/participations/recognize-prior',
    request,
  )) as unknown as RecognizePriorCreditResponse;
  return response;
}

/**
 * Concludes an active learning unit participation upon completion (P2-S05, UC-ACD-12).
 * POST /api/v1/academics/participations/{id}/conclude
 */
export async function concludeParticipation(
  id: string,
  request: ConcludeParticipationRequest,
): Promise<ConcludeParticipationResponse> {
  const response = (await apiClient.post(
    `/v1/academics/participations/${encodeURIComponent(id)}/conclude`,
    request,
  )) as unknown as ConcludeParticipationResponse;
  return response;
}

/**
 * Retrieves unit roster participations with optional filters (P2-S06, TD-08).
 * GET /api/v1/academics/learning-units/{learningUnitId}/participations
 */
export async function getParticipationsByUnit(
  learningUnitId: string,
  filter?: UnitParticipationsFilter,
): Promise<AcademicParticipation[]> {
  const params: Record<string, string> = {};
  if (filter?.academicPeriodId) {
    params.academicPeriodId = filter.academicPeriodId;
  }
  if (filter?.sectionLabel) {
    params.sectionLabel = filter.sectionLabel;
  }
  if (filter?.status) {
    params.status = filter.status;
  }

  const response = (await apiClient.get(
    `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/participations`,
    { params },
  )) as unknown as AcademicParticipation[];
  return response;
}

/**
 * Retrieves student-centric unit participations with optional period filter (P2-S06).
 * GET /api/v1/academics/students/{studentId}/participations
 */
export async function getStudentParticipations(
  studentId: string,
  filter?: StudentParticipationsFilter,
): Promise<StudentParticipation[]> {
  const params: Record<string, string> = {};
  if (filter?.academicPeriodId) {
    params.academicPeriodId = filter.academicPeriodId;
  }

  const response = (await apiClient.get(
    `/v1/academics/students/${encodeURIComponent(studentId)}/participations`,
    { params },
  )) as unknown as StudentParticipation[];
  return response;
}
