import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  AddSupervisionRequirementRequest,
  AddSupervisionRequirementResponse,
  AddTeachingCapabilityRequest,
  AddTeachingCapabilityResponse,
  ApplyRecordCorrectionRequest,
  ApproveTeacherProfileResponse,
  CapabilityTypeDto,
  ChangeTeacherStandingRequest,
  ChangeTeacherStandingResponse,
  FacultyQueryParams,
  GetRecordCorrectionsParams,
  GrantTeachingAuthorizationRequest,
  GrantTeachingAuthorizationResponse,
  QualificationTypeResponse,
  RegisterTeacherProfileRequest,
  RegisterTeacherProfileResponse,
  RejectQualificationEvidenceRequest,
  RejectQualificationEvidenceResponse,
  ResolveSupervisionRequirementRequest,
  ResolveSupervisionRequirementResponse,
  SubmitQualificationEvidenceRequest,
  SubmitQualificationEvidenceResponse,
  TeacherProfileDto,
  TeacherSummaryDto,
  TeachingRecordCorrectionDto,
  TeachingRoleDto,
  UpdateTeachingAuthorizationStatusRequest,
  UpdateTeachingAuthorizationStatusResponse,
  UpdateTeacherProfileRequest,
  VerifyQualificationEvidenceRequest,
  VerifyQualificationEvidenceResponse,
} from './teachingTypes';

/**
 * Teaching Faculty API Client module.
 * Wraps all Minimal API endpoints under /api/v1/teaching.
 */

/**
 * Queries faculty list with work queue filters, search, and pagination (SCR-TCH-01, P2-S07).
 * GET /api/v1/teaching/teachers
 */
export async function getTeachers(
  params?: FacultyQueryParams,
): Promise<PaginatedResponse<TeacherSummaryDto>> {
  const cleanParams: Record<string, string | number> = {};

  if (params?.queueFilter && params.queueFilter !== 'all') {
    cleanParams.queueFilter = params.queueFilter;
  }
  if (params?.standing && params.standing !== 'all') {
    cleanParams.standing = params.standing;
  }
  if (params?.campus && params.campus !== 'all') {
    cleanParams.campus = params.campus;
  }
  if (params?.search?.trim()) {
    cleanParams.search = params.search.trim();
  }
  if (typeof params?.page === 'number') {
    cleanParams.page = params.page;
  }
  if (typeof params?.pageSize === 'number') {
    cleanParams.pageSize = params.pageSize;
  }

  const response = (await apiClient.get('/v1/teaching/teachers', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<TeacherSummaryDto>;

  return response;
}

/**
 * Retrieves the complete authoritative faculty dossier by teacher profile ID (P2-S07).
 * GET /api/v1/teaching/teachers/{id}
 */
export async function getTeacherById(id: string): Promise<TeacherProfileDto> {
  const response = (await apiClient.get(
    `/v1/teaching/teachers/${encodeURIComponent(id)}`,
  )) as unknown as TeacherProfileDto;

  return response;
}

/**
 * Registers a new candidate teacher profile into pending_approval standing (P2-S05).
 * POST /api/v1/teaching/teachers
 */
export async function registerTeacher(
  payload: RegisterTeacherProfileRequest,
): Promise<RegisterTeacherProfileResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/teachers',
    payload,
  )) as unknown as RegisterTeacherProfileResponse;

  return response;
}

/**
 * Updates mutable profile attributes for a teacher (P2-S05).
 * PUT /api/v1/teaching/teachers/{id}
 */
export async function updateTeacher(
  id: string,
  payload: UpdateTeacherProfileRequest,
): Promise<void> {
  await apiClient.put(`/v1/teaching/teachers/${encodeURIComponent(id)}`, payload);
}

/**
 * Formally approves a candidate teacher profile, transitioning standing to active (P2-S06).
 * POST /api/v1/teaching/teachers/{id}/approve
 */
export async function approveTeacher(
  id: string,
): Promise<ApproveTeacherProfileResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(id)}/approve`,
  )) as unknown as ApproveTeacherProfileResponse;

  return response;
}

/**
 * Modifies teacher standing state with mandatory justification reason (P2-S06).
 * POST /api/v1/teaching/teachers/{id}/standing
 */
export async function changeStanding(
  id: string,
  payload: ChangeTeacherStandingRequest,
): Promise<ChangeTeacherStandingResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(id)}/standing`,
    payload,
  )) as unknown as ChangeTeacherStandingResponse;

  return response;
}

/**
 * Retrieves all active recognized teaching roles (P2-S07).
 * GET /api/v1/teaching/roles
 */
export async function getRoles(): Promise<TeachingRoleDto[]> {
  const response = (await apiClient.get(
    '/v1/teaching/roles',
  )) as unknown as TeachingRoleDto[];

  return response;
}

/**
 * Retrieves active capability taxonomy categories (P2-S08).
 * GET /api/v1/teaching/capability-types
 */
export async function getCapabilityTypes(): Promise<CapabilityTypeDto[]> {
  const response = (await apiClient.get(
    '/v1/teaching/capability-types',
  )) as unknown as CapabilityTypeDto[];

  return response;
}

/**
 * Records a potential teaching capability for a teacher profile (P2-S08).
 * POST /api/v1/teaching/teachers/{id}/capabilities
 */
export async function addCapability(
  teacherId: string,
  payload: AddTeachingCapabilityRequest,
): Promise<AddTeachingCapabilityResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/capabilities`,
    payload,
  )) as unknown as AddTeachingCapabilityResponse;

  return response;
}

/**
 * Retires or removes a teaching capability from a teacher profile (P2-S08).
 * DELETE /api/v1/teaching/teachers/{id}/capabilities/{capabilityId}
 */
export async function removeCapability(
  teacherId: string,
  capabilityId: string,
): Promise<void> {
  await apiClient.delete(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/capabilities/${encodeURIComponent(capabilityId)}`,
  );
}

/**
 * Retrieves recognized qualification categories and renewal policies (P3-S09).
 * GET /api/v1/teaching/qualification-types
 */
export async function getQualificationTypes(): Promise<QualificationTypeResponse[]> {
  const response = (await apiClient.get(
    '/v1/teaching/qualification-types',
  )) as unknown as QualificationTypeResponse[];

  return response;
}

/**
 * Submits documentary evidence supporting teacher credentials (P3-S09).
 * POST /api/v1/teaching/teachers/{id}/qualifications
 */
export async function submitEvidence(
  teacherId: string,
  payload: SubmitQualificationEvidenceRequest,
): Promise<SubmitQualificationEvidenceResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/qualifications`,
    payload,
  )) as unknown as SubmitQualificationEvidenceResponse;

  return response;
}

/**
 * Records verification outcome for submitted credential evidence (P3-S10).
 * POST /api/v1/teaching/qualifications/{id}/verify
 */
export async function verifyEvidence(
  id: string,
  payload?: VerifyQualificationEvidenceRequest,
): Promise<VerifyQualificationEvidenceResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/qualifications/${encodeURIComponent(id)}/verify`,
    payload ?? {},
  )) as unknown as VerifyQualificationEvidenceResponse;

  return response;
}

/**
 * Records rejection of submitted credential evidence with mandatory reason (P3-S10).
 * POST /api/v1/teaching/qualifications/{id}/reject
 */
export async function rejectEvidence(
  id: string,
  payload: RejectQualificationEvidenceRequest,
): Promise<RejectQualificationEvidenceResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/qualifications/${encodeURIComponent(id)}/reject`,
    payload,
  )) as unknown as RejectQualificationEvidenceResponse;

  return response;
}

/**
 * Formally grants a teaching authorization with scopes and permissive warning support (P4-S11).
 * POST /api/v1/teaching/teachers/{id}/authorizations
 */
export async function grantAuthorization(
  teacherId: string,
  payload: GrantTeachingAuthorizationRequest,
): Promise<GrantTeachingAuthorizationResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/authorizations`,
    payload,
  )) as unknown as GrantTeachingAuthorizationResponse;

  return response;
}

/**
 * Updates lifecycle status of a teaching authorization with mandatory justification (P4-S12).
 * POST /api/v1/teaching/authorizations/{id}/status
 */
export async function updateAuthorizationStatus(
  id: string,
  payload: UpdateTeachingAuthorizationStatusRequest,
): Promise<UpdateTeachingAuthorizationStatusResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/authorizations/${encodeURIComponent(id)}/status`,
    payload,
  )) as unknown as UpdateTeachingAuthorizationStatusResponse;

  return response;
}

/**
 * Attaches a mentoring or supervision requirement to a teaching authorization (P4-S13).
 * POST /api/v1/teaching/authorizations/{id}/supervision
 */
export async function addSupervision(
  authorizationId: string,
  payload: AddSupervisionRequirementRequest,
): Promise<AddSupervisionRequirementResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/authorizations/${encodeURIComponent(authorizationId)}/supervision`,
    payload,
  )) as unknown as AddSupervisionRequirementResponse;

  return response;
}

/**
 * Resolves (completes, waives, escalates) a supervision condition (P4-S13).
 * POST /api/v1/teaching/supervision/{id}/resolve
 */
export async function resolveSupervision(
  id: string,
  payload: ResolveSupervisionRequirementRequest,
): Promise<ResolveSupervisionRequirementResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/supervision/${encodeURIComponent(id)}/resolve`,
    payload,
  )) as unknown as ResolveSupervisionRequirementResponse;

  return response;
}

/**
 * Applies or requests an auditable record correction on a finalized record (P5-S14).
 * POST /api/v1/teaching/corrections
 */
export async function applyCorrection(
  payload: ApplyRecordCorrectionRequest,
): Promise<TeachingRecordCorrectionDto> {
  const response = (await apiClient.post(
    '/v1/teaching/corrections',
    payload,
  )) as unknown as TeachingRecordCorrectionDto;

  return response;
}

/**
 * Approves and atomically applies a requested record correction (P5-S14).
 * POST /api/v1/teaching/corrections/{id}/approve
 */
export async function approveCorrection(
  id: string,
): Promise<TeachingRecordCorrectionDto> {
  const response = (await apiClient.post(
    `/v1/teaching/corrections/${encodeURIComponent(id)}/approve`,
  )) as unknown as TeachingRecordCorrectionDto;

  return response;
}

/**
 * Queries chronological audit trail of faculty record corrections (P5-S14).
 * GET /api/v1/teaching/corrections
 */
export async function getCorrections(
  params?: GetRecordCorrectionsParams,
): Promise<TeachingRecordCorrectionDto[]> {
  const cleanParams: Record<string, string> = {};
  if (params?.teacherProfileId) {
    cleanParams.teacherProfileId = params.teacherProfileId;
  }
  if (params?.targetTableName) {
    cleanParams.targetTableName = params.targetTableName;
  }
  if (params?.status) {
    cleanParams.status = params.status;
  }

  const response = (await apiClient.get('/v1/teaching/corrections', {
    params: cleanParams,
  })) as unknown as TeachingRecordCorrectionDto[];

  return response;
}

/**
 * Centralized faculty API client object.
 */
export const teachingApiClient = {
  getTeachers,
  getTeacherById,
  getRoles,
  getCapabilityTypes,
  getQualificationTypes,
  registerTeacher,
  updateTeacher,
  approveTeacher,
  changeStanding,
  addCapability,
  removeCapability,
  submitEvidence,
  verifyEvidence,
  rejectEvidence,
  grantAuthorization,
  updateAuthorizationStatus,
  addSupervision,
  resolveSupervision,
  applyCorrection,
  approveCorrection,
  getCorrections,
};

export default teachingApiClient;
