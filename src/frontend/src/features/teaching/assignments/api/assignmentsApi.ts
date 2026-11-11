import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  AcknowledgeTeachingAssignmentResponse,
  ApprovePermanentReassignmentRequest,
  ApproveSubstitutionExceptionRequest,
  ApproveTeachingAssignmentRequest,
  ApproveTeachingAssignmentResponse,
  ArrangeEmergencySubstitutionRequest,
  ArrangeEmergencySubstitutionResponse,
  AssignmentCompletionDto,
  AssignmentRoleDto,
  CancelSubstitutionRequest,
  CandidateEvaluationSummaryDto,
  CapacityConflictDto,
  ConcludeTeachingAssignmentRequest,
  ConcludeTeachingAssignmentResponse,
  ConfirmReassignmentHandoverRequest,
  CreateTeachingDemandRequest,
  CreateTeachingDemandResponse,
  EvaluateCandidateEligibilityRequest,
  EvaluateCandidateEligibilityResponse,
  GetAssignmentCompletionsParams,
  GetAssignmentRolesParams,
  GetCandidateEvaluationsParams,
  GetCapacityConflictsParams,
  GetTeacherHistoryParams,
  GetTeachingAssignmentsParams,
  GetTeachingDemandsParams,
  GetTeachingReassignmentsParams,
  GetTeachingSubstitutionsParams,
  ProposePermanentReassignmentRequest,
  ProposePermanentReassignmentResponse,
  ProposeTeachingAssignmentRequest,
  ProposeTeachingAssignmentResponse,
  ResolveCapacityConflictRequest,
  ResolveCapacityConflictResponse,
  TeachingAssignmentDto,
  TeachingDemandDto,
  TeachingHistoryEntryDto,
  TeachingReassignmentDto,
  TeachingSubstitutionDto,
  UpdateTeachingAssignmentStatusRequest,
  UpdateTeachingDemandRequest,
  UpdateTeachingDemandStatusRequest,
} from './types';

/**
 * Teaching Assignments API Client module (P7-S21, SCR-TCH-02).
 * Wraps Minimal API endpoints across Demands, Eligibility, Assignments, Substitutions,
 * Reassignments, Completions, and History.
 */

// ============================================================================
// 1. Demands Intake (P2-S04, SCR-TCH-02)
// ============================================================================

export async function getTeachingDemands(
  params?: GetTeachingDemandsParams,
): Promise<PaginatedResponse<TeachingDemandDto>> {
  const cleanParams: Record<string, unknown> = {};

  if (params?.academicPeriodId?.trim()) cleanParams.academicPeriodId = params.academicPeriodId.trim();
  if (params?.sourceDomain?.trim()) cleanParams.sourceDomain = params.sourceDomain.trim();
  if (params?.status?.trim() && params.status !== 'all') cleanParams.status = params.status.trim();
  if (params?.campusScope?.trim()) cleanParams.campusScope = params.campusScope.trim();
  if (params?.search?.trim()) cleanParams.search = params.search.trim();
  if (params?.page !== undefined) cleanParams.page = params.page;
  if (params?.pageSize !== undefined) cleanParams.pageSize = params.pageSize;

  const response = (await apiClient.get(
    '/v1/teaching/demands',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as PaginatedResponse<TeachingDemandDto>;

  return response;
}

export async function getTeachingDemandById(id: string): Promise<TeachingDemandDto> {
  const response = (await apiClient.get(
    `/v1/teaching/demands/${encodeURIComponent(id)}`,
  )) as unknown as TeachingDemandDto;

  return response;
}

export async function createTeachingDemand(
  payload: CreateTeachingDemandRequest,
): Promise<CreateTeachingDemandResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/demands',
    payload,
  )) as unknown as CreateTeachingDemandResponse;

  return response;
}

export async function updateTeachingDemand(
  id: string,
  payload: UpdateTeachingDemandRequest,
): Promise<TeachingDemandDto> {
  const response = (await apiClient.put(
    `/v1/teaching/demands/${encodeURIComponent(id)}`,
    payload,
  )) as unknown as TeachingDemandDto;

  return response;
}

export async function updateTeachingDemandStatus(
  id: string,
  payload: UpdateTeachingDemandStatusRequest,
): Promise<TeachingDemandDto> {
  const response = (await apiClient.post(
    `/v1/teaching/demands/${encodeURIComponent(id)}/status`,
    payload,
  )) as unknown as TeachingDemandDto;

  return response;
}

// ============================================================================
// 2. Eligibility Evaluation Engine (P2-S05, SCR-TCH-02)
// ============================================================================

export async function evaluateCandidate(
  payload: EvaluateCandidateEligibilityRequest,
): Promise<EvaluateCandidateEligibilityResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/eligibility/evaluate',
    payload,
  )) as unknown as EvaluateCandidateEligibilityResponse;

  return response;
}

export async function getCandidateEvaluations(
  params?: GetCandidateEvaluationsParams,
): Promise<CandidateEvaluationSummaryDto[]> {
  const cleanParams: Record<string, string> = {};

  if (params?.teachingDemandId?.trim()) cleanParams.teachingDemandId = params.teachingDemandId.trim();
  if (params?.teacherProfileId?.trim()) cleanParams.teacherProfileId = params.teacherProfileId.trim();

  const response = (await apiClient.get(
    '/v1/teaching/eligibility/evaluations',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as CandidateEvaluationSummaryDto[];

  return response;
}

// ============================================================================
// 3. Teaching Assignments & Roles (P3-S06, SCR-TCH-02)
// ============================================================================

export async function getTeachingAssignments(
  params?: GetTeachingAssignmentsParams,
): Promise<PaginatedResponse<TeachingAssignmentDto>> {
  const cleanParams: Record<string, unknown> = {};

  if (params?.academicPeriodId?.trim()) cleanParams.academicPeriodId = params.academicPeriodId.trim();
  if (params?.status?.trim() && params.status !== 'all') cleanParams.status = params.status.trim();
  if (params?.teacherProfileId?.trim()) cleanParams.teacherProfileId = params.teacherProfileId.trim();
  if (params?.scopeType?.trim() && params.scopeType !== 'all') cleanParams.scopeType = params.scopeType.trim();
  if (params?.assignmentRoleId?.trim()) cleanParams.assignmentRoleId = params.assignmentRoleId.trim();
  if (params?.search?.trim()) cleanParams.search = params.search.trim();
  if (params?.page !== undefined) cleanParams.page = params.page;
  if (params?.pageSize !== undefined) cleanParams.pageSize = params.pageSize;

  const response = (await apiClient.get(
    '/v1/teaching/assignments',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as PaginatedResponse<TeachingAssignmentDto>;

  return response;
}

export async function getTeachingAssignmentById(id: string): Promise<TeachingAssignmentDto> {
  const response = (await apiClient.get(
    `/v1/teaching/assignments/${encodeURIComponent(id)}`,
  )) as unknown as TeachingAssignmentDto;

  return response;
}

export async function proposeTeachingAssignment(
  payload: ProposeTeachingAssignmentRequest,
): Promise<ProposeTeachingAssignmentResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/assignments/propose',
    payload,
  )) as unknown as ProposeTeachingAssignmentResponse;

  return response;
}

export async function approveTeachingAssignment(
  id: string,
  payload?: ApproveTeachingAssignmentRequest,
): Promise<ApproveTeachingAssignmentResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/assignments/${encodeURIComponent(id)}/approve`,
    payload ?? {},
  )) as unknown as ApproveTeachingAssignmentResponse;

  return response;
}

export async function acknowledgeTeachingAssignment(
  id: string,
): Promise<AcknowledgeTeachingAssignmentResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/assignments/${encodeURIComponent(id)}/acknowledge`,
    {},
  )) as unknown as AcknowledgeTeachingAssignmentResponse;

  return response;
}

export async function updateTeachingAssignmentStatus(
  id: string,
  payload: UpdateTeachingAssignmentStatusRequest,
): Promise<TeachingAssignmentDto> {
  const response = (await apiClient.post(
    `/v1/teaching/assignments/${encodeURIComponent(id)}/status`,
    payload,
  )) as unknown as TeachingAssignmentDto;

  return response;
}

export async function getAssignmentRoles(
  params?: GetAssignmentRolesParams,
): Promise<AssignmentRoleDto[]> {
  const cleanParams: Record<string, unknown> = {};
  if (params?.activeOnly !== undefined) cleanParams.activeOnly = params.activeOnly;

  const response = (await apiClient.get(
    '/v1/teaching/assignment-roles',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as AssignmentRoleDto[];

  return response;
}

// ============================================================================
// 4. Capacity Conflicts (P3-S07, SCR-TCH-02)
// ============================================================================

export async function getCapacityConflicts(
  params?: GetCapacityConflictsParams,
): Promise<PaginatedResponse<CapacityConflictDto>> {
  const cleanParams: Record<string, unknown> = {};

  if (params?.academicPeriodId?.trim()) cleanParams.academicPeriodId = params.academicPeriodId.trim();
  if (params?.severity?.trim() && params.severity !== 'all') cleanParams.severity = params.severity.trim();
  if (params?.status?.trim() && params.status !== 'all') cleanParams.status = params.status.trim();
  if (params?.teacherProfileId?.trim()) cleanParams.teacherProfileId = params.teacherProfileId.trim();
  if (params?.page !== undefined) cleanParams.page = params.page;
  if (params?.pageSize !== undefined) cleanParams.pageSize = params.pageSize;

  const response = (await apiClient.get(
    '/v1/teaching/conflicts',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as PaginatedResponse<CapacityConflictDto>;

  return response;
}

export async function resolveCapacityConflict(
  id: string,
  payload: ResolveCapacityConflictRequest,
): Promise<ResolveCapacityConflictResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/conflicts/${encodeURIComponent(id)}/resolve`,
    payload,
  )) as unknown as ResolveCapacityConflictResponse;

  return response;
}

// ============================================================================
// 5. Substitutions (P4-S11, SCR-TCH-02, Resolution 3.8)
// ============================================================================

export async function getTeachingSubstitutions(
  params?: GetTeachingSubstitutionsParams,
): Promise<PaginatedResponse<TeachingSubstitutionDto>> {
  const cleanParams: Record<string, unknown> = {};

  if (params?.originalAssignmentId?.trim()) cleanParams.originalAssignmentId = params.originalAssignmentId.trim();
  if (params?.substituteTeacherId?.trim()) cleanParams.substituteTeacherId = params.substituteTeacherId.trim();
  if (params?.status?.trim() && params.status !== 'all') cleanParams.status = params.status.trim();
  if (params?.page !== undefined) cleanParams.page = params.page;
  if (params?.pageSize !== undefined) cleanParams.pageSize = params.pageSize;

  const response = (await apiClient.get(
    '/v1/teaching/substitutions',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as PaginatedResponse<TeachingSubstitutionDto>;

  return response;
}

export async function arrangeEmergencySubstitution(
  payload: ArrangeEmergencySubstitutionRequest,
): Promise<ArrangeEmergencySubstitutionResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/substitutions',
    payload,
  )) as unknown as ArrangeEmergencySubstitutionResponse;

  return response;
}

export async function approveSubstitutionException(
  id: string,
  payload?: ApproveSubstitutionExceptionRequest,
): Promise<TeachingSubstitutionDto> {
  const response = (await apiClient.post(
    `/v1/teaching/substitutions/${encodeURIComponent(id)}/approve`,
    payload ?? {},
  )) as unknown as TeachingSubstitutionDto;

  return response;
}

export async function cancelSubstitution(
  id: string,
  payload?: CancelSubstitutionRequest,
): Promise<TeachingSubstitutionDto> {
  const response = (await apiClient.post(
    `/v1/teaching/substitutions/${encodeURIComponent(id)}/cancel`,
    payload ?? {},
  )) as unknown as TeachingSubstitutionDto;

  return response;
}

// ============================================================================
// 6. Permanent Reassignments (P4-S12, SCR-TCH-02)
// ============================================================================

export async function getTeachingReassignments(
  params?: GetTeachingReassignmentsParams,
): Promise<PaginatedResponse<TeachingReassignmentDto>> {
  const cleanParams: Record<string, unknown> = {};

  if (params?.originalAssignmentId?.trim()) cleanParams.originalAssignmentId = params.originalAssignmentId.trim();
  if (params?.predecessorTeacherId?.trim()) cleanParams.predecessorTeacherId = params.predecessorTeacherId.trim();
  if (params?.replacementTeacherId?.trim()) cleanParams.replacementTeacherId = params.replacementTeacherId.trim();
  if (params?.newTeacherId?.trim()) cleanParams.newTeacherId = params.newTeacherId.trim();
  if (params?.status?.trim() && params.status !== 'all') cleanParams.status = params.status.trim();
  if (params?.page !== undefined) cleanParams.page = params.page;
  if (params?.pageSize !== undefined) cleanParams.pageSize = params.pageSize;

  const response = (await apiClient.get(
    '/v1/teaching/reassignments',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as PaginatedResponse<TeachingReassignmentDto>;

  return response;
}

export async function getTeachingReassignmentById(id: string): Promise<TeachingReassignmentDto> {
  const response = (await apiClient.get(
    `/v1/teaching/reassignments/${encodeURIComponent(id)}`,
  )) as unknown as TeachingReassignmentDto;

  return response;
}

export async function proposePermanentReassignment(
  payload: ProposePermanentReassignmentRequest,
): Promise<ProposePermanentReassignmentResponse> {
  const response = (await apiClient.post(
    '/v1/teaching/reassignments',
    payload,
  )) as unknown as ProposePermanentReassignmentResponse;

  return response;
}

export async function approvePermanentReassignment(
  id: string,
  payload?: ApprovePermanentReassignmentRequest,
): Promise<TeachingReassignmentDto> {
  const response = (await apiClient.post(
    `/v1/teaching/reassignments/${encodeURIComponent(id)}/approve`,
    payload ?? {},
  )) as unknown as TeachingReassignmentDto;

  return response;
}

export async function confirmReassignmentHandover(
  id: string,
  payload?: ConfirmReassignmentHandoverRequest,
): Promise<TeachingReassignmentDto> {
  const response = (await apiClient.post(
    `/v1/teaching/reassignments/${encodeURIComponent(id)}/handover`,
    payload ?? {},
  )) as unknown as TeachingReassignmentDto;

  return response;
}

// ============================================================================
// 7. Assignment Completions & Conclusions (P4-S13, SCR-TCH-02)
// ============================================================================

export async function getAssignmentCompletions(
  params?: GetAssignmentCompletionsParams,
): Promise<AssignmentCompletionDto[]> {
  const cleanParams: Record<string, string> = {};

  if (params?.academicPeriodId?.trim()) cleanParams.academicPeriodId = params.academicPeriodId.trim();
  if (params?.teacherProfileId?.trim()) cleanParams.teacherProfileId = params.teacherProfileId.trim();
  if (params?.search?.trim()) cleanParams.search = params.search.trim();

  const response = (await apiClient.get(
    '/v1/teaching/completions',
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as AssignmentCompletionDto[];

  return response;
}

export async function concludeTeachingAssignment(
  id: string,
  payload: ConcludeTeachingAssignmentRequest,
): Promise<ConcludeTeachingAssignmentResponse> {
  const response = (await apiClient.post(
    `/v1/teaching/assignments/${encodeURIComponent(id)}/conclude`,
    payload,
  )) as unknown as ConcludeTeachingAssignmentResponse;

  return response;
}

// ============================================================================
// 8. Teaching History Audit Log (P4-S14, SCR-TCH-02)
// ============================================================================

export async function getTeacherHistory(
  teacherId: string,
  params?: GetTeacherHistoryParams,
): Promise<TeachingHistoryEntryDto[]> {
  const cleanParams: Record<string, string> = {};
  if (params?.eventType?.trim() && params.eventType !== 'all') {
    cleanParams.eventType = params.eventType.trim();
  }

  const response = (await apiClient.get(
    `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/history`,
    Object.keys(cleanParams).length > 0 ? { params: cleanParams } : undefined,
  )) as unknown as TeachingHistoryEntryDto[];

  return response;
}

export const assignmentsApi = {
  // Demands
  getTeachingDemands,
  getTeachingDemandById,
  createTeachingDemand,
  updateTeachingDemand,
  updateTeachingDemandStatus,
  // Evaluations
  evaluateCandidate,
  getCandidateEvaluations,
  // Assignments
  getTeachingAssignments,
  getTeachingAssignmentById,
  proposeTeachingAssignment,
  approveTeachingAssignment,
  acknowledgeTeachingAssignment,
  updateTeachingAssignmentStatus,
  getAssignmentRoles,
  // Conflicts
  getCapacityConflicts,
  resolveCapacityConflict,
  // Substitutions
  getTeachingSubstitutions,
  arrangeEmergencySubstitution,
  approveSubstitutionException,
  cancelSubstitution,
  // Reassignments
  getTeachingReassignments,
  getTeachingReassignmentById,
  proposePermanentReassignment,
  approvePermanentReassignment,
  confirmReassignmentHandover,
  // Completions
  getAssignmentCompletions,
  concludeTeachingAssignment,
  // History
  getTeacherHistory,
};

export default assignmentsApi;
