/**
 * Teaching Assignments Feature TypeScript Types & DTO Contracts (P7-S21, SCR-TCH-02).
 * Strictly typed against backend contract DTOs in Elsasa.Sis.Contracts.Teaching
 * and Minimal API vertical slices under Elsasa.Sis.Teaching.Features.
 */

// ============================================================================
// Core Status & Enum Unions
// ============================================================================

export type TeachingDemandStatus =
  | 'received'
  | 'accepted_for_staffing'
  | 'returned_for_clarification'
  | 'changed'
  | 'cancelled'
  | 'declined'
  | 'staffed';

export type TeacherEligibilityStatus =
  | 'eligible'
  | 'eligible_under_supervision'
  | 'ineligible_unqualified'
  | 'ineligible_conflicted'
  | 'ineligible_capacity_exceeded';

export type TeachingAssignmentStatus =
  | 'proposed'
  | 'approved'
  | 'rejected'
  | 'withdrawn'
  | 'returned_for_revision'
  | 'suspended'
  | 'cancelled'
  | 'completed';

export type AssignmentScopeCategory =
  | 'learning_unit'
  | 'cohort_group'
  | 'individual_student'
  | 'extracurricular_group';

export type CapacityConflictSeverity = 'advisory' | 'blocking';

export type CapacityConflictStatus = 'unresolved' | 'resolved' | 'waived_with_exception';

export type CoverageScopeType = 'full' | 'partial';

export type TeachingSubstitutionStatus =
  | 'proposed'
  | 'approved'
  | 'active'
  | 'completed'
  | 'cancelled';

export type ReassignmentHandoverStatus = 'pending' | 'completed' | 'waived';

export type TeachingReassignmentStatus = 'proposed' | 'approved' | 'executed' | 'cancelled';

export type UnresolvedItemsStatus =
  | 'none'
  | 'minor_pending'
  | 'escalated_defect'
  | 'resolved'
  | 'escalated';

export type AssignmentCompletionStatus = 'completed' | 'completed_with_exceptions';

// ============================================================================
// Authoritative Contract DTOs
// ============================================================================

/**
 * Strongly-typed contract DTO representing a formal teaching capacity demand (TeachingDemandDto).
 */
export interface TeachingDemandDto {
  id: string;
  demandCode: string;
  sourceDomain: string;
  sourceWorkReference: string;
  sourceWorkLabel: string;
  academicPeriodId: string;
  requiredCapabilityCode: string;
  requiredAssignmentRoleId: string;
  requiredAssignmentRoleName?: string | null;
  expectedWorkloadHours: number;
  workloadUnit: string;
  requiresAssessmentAuthority: boolean;
  requestedStartDate: string;
  requestedEndDate: string;
  campusScope: string;
  requiredTeachersCount: number;
  status: TeachingDemandStatus | string;
  requestedBy: string;
  requestedAt: string;
  declineReason?: string | null;
  clarificationNotes?: string | null;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing an explainable eligibility evaluation (TeacherEligibilityEvaluationDto).
 */
export interface TeacherEligibilityEvaluationDto {
  id: string;
  teachingDemandId: string;
  teacherProfileId: string;
  evaluationDate: string;
  evaluatedBy: string;
  isEligible: boolean;
  eligibilityStatus: TeacherEligibilityStatus | string;
  profileActiveCheck: boolean;
  authorizationCheckPassed: boolean;
  qualificationCheckPassed: boolean;
  availabilityCheckPassed: boolean;
  capacityCheckPassed: boolean;
  explanation: string;
  notes?: string | null;
  createdAt: string;
}

/**
 * Summary DTO representing an evaluation item when querying candidate evaluations list (CandidateEvaluationSummaryDto).
 */
export interface CandidateEvaluationSummaryDto {
  id: string;
  teachingDemandId: string;
  demandCode: string;
  teacherProfileId: string;
  teacherName: string;
  teacherNumber: string;
  evaluationDate: string;
  evaluatedBy: string;
  isEligible: boolean;
  eligibilityStatus: TeacherEligibilityStatus | string;
  profileActiveCheck: boolean;
  authorizationCheckPassed: boolean;
  qualificationCheckPassed: boolean;
  availabilityCheckPassed: boolean;
  capacityCheckPassed: boolean;
  explanation: string;
  notes?: string | null;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing an authoritative teaching assignment (TeachingAssignmentDto).
 */
export interface TeachingAssignmentDto {
  id: string;
  assignmentNumber: string;
  teachingDemandId: string;
  teacherProfileId: string;
  assignmentRoleId: string;
  assignmentRoleName?: string | null;
  assignmentScope: AssignmentScopeCategory | string;
  scopeReferenceId: string;
  scopeLabel: string;
  academicPeriodId: string;
  effectiveStartDate: string;
  effectiveEndDate: string;
  workloadCommitmentValue: number;
  workloadUnit: string;
  status: TeachingAssignmentStatus | string;
  approvedBy?: string | null;
  approvedDate?: string | null;
  teacherAcknowledged: boolean;
  teacherAcknowledgedDate?: string | null;
  predecessorAssignmentId?: string | null;
  notes?: string | null;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing an assignment role catalog entry (AssignmentRoleDto).
 */
export interface AssignmentRoleDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isLeadRole: boolean;
  countsTowardWorkload: boolean;
  isActive: boolean;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing a capacity/schedule conflict condition (CapacityConflictDto).
 */
export interface CapacityConflictDto {
  id: string;
  teachingAssignmentId?: string | null;
  teachingDemandId?: string | null;
  teacherProfileId: string;
  conflictType: string;
  conflictClassification: string;
  severity: CapacityConflictSeverity | string;
  description: string;
  detectedDate: string;
  status: CapacityConflictStatus | string;
  resolutionAction?: string | null;
  resolvedBy?: string | null;
  resolvedDate?: string | null;
  resolutionNotes?: string | null;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing an emergency substitution arrangement (TeachingSubstitutionDto).
 */
export interface TeachingSubstitutionDto {
  id: string;
  substitutionNumber: string;
  originalAssignmentId: string;
  originalTeacherId: string;
  substituteTeacherId: string;
  coverageStartDate: string;
  coverageEndDate: string;
  coverageScopeType: CoverageScopeType | string;
  partialScopeDetails?: string | null;
  reason: string;
  workloadImpactHours: number;
  status: TeachingSubstitutionStatus | string;
  handoverNotes?: string | null;
  approvedBy: string;
  approvedDate: string;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing a permanent responsibility transfer (TeachingReassignmentDto).
 */
export interface TeachingReassignmentDto {
  id: string;
  reassignmentNumber: string;
  originalAssignmentId: string;
  predecessorTeacherId: string;
  newTeacherId: string;
  replacementAssignmentId?: string | null;
  effectiveDate: string;
  reassignmentType: string;
  reason: string;
  requiresHandover: boolean;
  handoverStatus: ReassignmentHandoverStatus | string;
  handoverCompletedDate?: string | null;
  handoverNotes?: string | null;
  status: TeachingReassignmentStatus | string;
  approvedBy: string;
  approvedDate: string;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing formal conclusion of an assignment (AssignmentCompletionDto).
 */
export interface AssignmentCompletionDto {
  id: string;
  teachingAssignmentId: string;
  completionDate: string;
  academicConfirmationReceived: boolean;
  academicConfirmationSource?: string | null;
  handoverConfirmed: boolean;
  unresolvedItemsStatus: UnresolvedItemsStatus | string;
  unresolvedItemsDescription?: string | null;
  capacityReleasedDate: string;
  completedBy: string;
  status: AssignmentCompletionStatus | string;
  notes?: string | null;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing an immutable history audit log entry (TeachingHistoryEntryDto).
 */
export interface TeachingHistoryEntryDto {
  id: string;
  teacherProfileId: string;
  eventType: string;
  entityType: string;
  entityId: string;
  effectiveDate: string;
  eventTimestamp: string;
  actorRole: string;
  actorId: string;
  eventSummary: string;
  changePayload?: string | null;
  notes?: string | null;
  createdAt: string;
}

// ============================================================================
// Query Parameters
// ============================================================================

export interface GetTeachingDemandsParams {
  academicPeriodId?: string | null;
  sourceDomain?: string | null;
  status?: string | null;
  campusScope?: string | null;
  search?: string | null;
  page?: number;
  pageSize?: number;
}

export interface GetCandidateEvaluationsParams {
  teachingDemandId?: string | null;
  teacherProfileId?: string | null;
}

export interface GetTeachingAssignmentsParams {
  academicPeriodId?: string | null;
  status?: string | null;
  teacherProfileId?: string | null;
  scopeType?: string | null;
  assignmentRoleId?: string | null;
  search?: string | null;
  page?: number;
  pageSize?: number;
}

export interface GetAssignmentRolesParams {
  activeOnly?: boolean;
}

export interface GetCapacityConflictsParams {
  academicPeriodId?: string | null;
  severity?: string | null;
  status?: string | null;
  teacherProfileId?: string | null;
  page?: number;
  pageSize?: number;
}

export interface GetTeachingSubstitutionsParams {
  originalAssignmentId?: string | null;
  substituteTeacherId?: string | null;
  status?: string | null;
  page?: number;
  pageSize?: number;
}

export interface GetTeachingReassignmentsParams {
  originalAssignmentId?: string | null;
  predecessorTeacherId?: string | null;
  replacementTeacherId?: string | null;
  newTeacherId?: string | null;
  status?: string | null;
  page?: number;
  pageSize?: number;
}

export interface GetAssignmentCompletionsParams {
  academicPeriodId?: string | null;
  teacherProfileId?: string | null;
  search?: string | null;
}

export interface GetTeacherHistoryParams {
  eventType?: string | null;
}

// ============================================================================
// Mutation Request & Response Payloads
// ============================================================================

export interface CreateTeachingDemandRequest {
  sourceDomain: string;
  sourceWorkReference: string;
  sourceWorkLabel: string;
  academicPeriodId: string;
  requiredCapabilityCode: string;
  requiredAssignmentRoleId: string;
  expectedWorkloadHours: number;
  requestedStartDate: string;
  requestedEndDate: string;
  workloadUnit?: string | null;
  requiresAssessmentAuthority?: boolean;
  campusScope?: string | null;
  requiredTeachersCount?: number;
  demandCode?: string | null;
  clarificationNotes?: string | null;
}

export interface CreateTeachingDemandResponse {
  id: string;
  demandCode: string;
  status: string;
  sourceDomain: string;
  sourceWorkLabel: string;
  createdAt: string;
}

export interface UpdateTeachingDemandRequest {
  sourceWorkLabel: string;
  requiredCapabilityCode: string;
  requiredAssignmentRoleId: string;
  expectedWorkloadHours: number;
  requestedStartDate: string;
  requestedEndDate: string;
  workloadUnit?: string | null;
  requiresAssessmentAuthority?: boolean;
  campusScope?: string | null;
  requiredTeachersCount?: number;
  clarificationNotes?: string | null;
}

export interface UpdateTeachingDemandStatusRequest {
  status: string;
  declineReason?: string | null;
  clarificationNotes?: string | null;
}

export interface EvaluateCandidateEligibilityRequest {
  teachingDemandId: string;
  teacherProfileId: string;
  coordinatorRemarks?: string | null;
}

export interface EvaluateCandidateEligibilityResponse {
  id: string;
  teachingDemandId: string;
  teacherProfileId: string;
  demandCode: string;
  teacherName: string;
  teacherNumber: string;
  evaluationDate: string;
  evaluatedBy: string;
  isEligible: boolean;
  eligibilityStatus: string;
  profileActiveCheck: boolean;
  authorizationCheckPassed: boolean;
  qualificationCheckPassed: boolean;
  availabilityCheckPassed: boolean;
  capacityCheckPassed: boolean;
  explanation: string;
  notes?: string | null;
  createdAt: string;
}

export interface ProposeTeachingAssignmentRequest {
  teachingDemandId: string;
  teacherProfileId: string;
  assignmentRoleId: string;
  assignmentScope: string;
  scopeReferenceId: string;
  scopeLabel: string;
  academicPeriodId: string;
  effectiveStartDate: string;
  effectiveEndDate: string;
  workloadCommitmentValue: number;
  workloadUnit?: string | null;
  assignmentNumber?: string | null;
  predecessorAssignmentId?: string | null;
  notes?: string | null;
}

export interface ProposeTeachingAssignmentResponse {
  id: string;
  assignmentNumber: string;
  status: string;
  teachingDemandId: string;
  teacherProfileId: string;
  workloadCommitmentValue: number;
  createdAt: string;
}

export interface ApproveTeachingAssignmentRequest {
  approvedDate?: string | null;
  notes?: string | null;
}

export interface ApproveTeachingAssignmentResponse {
  id: string;
  assignmentNumber: string;
  status: string;
  approvedDate: string;
  approvedBy: string;
  approvedAt: string;
}

export interface AcknowledgeTeachingAssignmentResponse {
  id: string;
  assignmentNumber: string;
  teacherAcknowledged: boolean;
  teacherAcknowledgedDate: string;
  acknowledgedAt: string;
}

export interface UpdateTeachingAssignmentStatusRequest {
  status: string;
  reason?: string | null;
}

export interface ResolveCapacityConflictRequest {
  status: string;
  resolutionNotes?: string | null;
  resolutionAction?: string | null;
}

export interface ResolveCapacityConflictResponse {
  id: string;
  status: string;
  resolutionAction?: string | null;
  resolvedBy?: string | null;
  resolvedDate?: string | null;
  resolvedAt?: string | null;
  resolutionNotes?: string | null;
}

export interface ArrangeEmergencySubstitutionRequest {
  originalAssignmentId: string;
  substituteTeacherId: string;
  startDate: string;
  endDate: string;
  coverageScopeType: string;
  coveredHoursPerWeek?: number | null;
  reason: string;
  workloadImpactHours?: number | null;
  partialScopeDetails?: string | null;
  handoverNotes?: string | null;
}

export interface ArrangeEmergencySubstitutionResponse {
  id: string;
  substitutionNumber: string;
  originalAssignmentId: string;
  originalTeacherId: string;
  substituteTeacherId: string;
  coverageStartDate: string;
  coverageEndDate: string;
  coverageScopeType: string;
  partialScopeDetails?: string | null;
  reason: string;
  workloadImpactHours: number;
  status: string;
  handoverNotes?: string | null;
  approvedBy: string;
  approvedDate: string;
  isDelegatedApproval: boolean;
  requiresAuthorityApproval: boolean;
  capacityConflictId?: string | null;
  createdAt: string;
}

export interface ApproveSubstitutionExceptionRequest {
  decisionRemarks?: string | null;
}

export interface CancelSubstitutionRequest {
  reason?: string | null;
}

export interface ProposePermanentReassignmentRequest {
  originalAssignmentId: string;
  newTeacherId: string;
  effectiveDate: string;
  reason: string;
  requiresHandover?: boolean;
  reassignmentType?: string | null;
}

export interface ProposePermanentReassignmentResponse {
  reassignment: TeachingReassignmentDto;
}

export interface ApprovePermanentReassignmentRequest {
  notes?: string | null;
}

export interface ConfirmReassignmentHandoverRequest {
  handoverStatus?: string | null;
  notes?: string | null;
}

export interface ConcludeTeachingAssignmentRequest {
  completionDate: string;
  academicConfirmationReceived: boolean;
  academicConfirmationSource?: string | null;
  handoverConfirmed: boolean;
  unresolvedItemsStatus?: string | null;
  unresolvedItemsDescription?: string | null;
  notes?: string | null;
}

export interface ConcludeTeachingAssignmentResponse {
  id: string;
  teachingAssignmentId: string;
  assignmentNumber: string;
  completionDate: string;
  academicConfirmationReceived: boolean;
  academicConfirmationSource?: string | null;
  handoverConfirmed: boolean;
  unresolvedItemsStatus: string;
  unresolvedItemsDescription?: string | null;
  capacityReleasedDate: string;
  releasedWorkloadHours: number;
  completedBy: string;
  status: string;
  notes?: string | null;
  createdAt: string;
}
