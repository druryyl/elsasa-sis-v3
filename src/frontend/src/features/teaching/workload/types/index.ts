/**
 * Workload Feature TypeScript Types and API Contracts (P7-S22, SCR-TCH-03).
 * Strictly typed against backend contract DTOs and endpoints in Elsasa.Sis.Contracts.Teaching and Elsasa.Sis.Teaching.Features.Workload.
 */

export type WorkloadPolicyStatus = 'draft' | 'active' | 'superseded' | 'archived';

export type WorkloadCommitmentStatus =
  | 'normal'
  | 'under_allocated'
  | 'approaching_limit'
  | 'overloaded_with_exception'
  | 'breached';

export type WorkloadExceptionStatus =
  | 'requested'
  | 'recommended'
  | 'approved'
  | 'rejected';

export type CapacityConflictSeverity = 'advisory' | 'blocking';

export type CapacityConflictStatus = 'unresolved' | 'resolved' | 'waived_with_exception';

/**
 * Strongly-typed DTO representing an institutional workload policy (WorkloadPolicyDto).
 */
export interface WorkloadPolicyDto {
  id: string;
  code: string;
  name: string;
  academicYearId: string;
  version: string;
  workloadUnit: string;
  maxStandardCapacity: number;
  warningThreshold: number;
  prohibitedThreshold: number;
  includesAssessmentEffort: boolean;
  includesAdvisingEffort: boolean;
  includesPreparationTime: boolean;
  calculationRulesSummary: string;
  effectiveStartDate: string;
  effectiveEndDate?: string | null;
  status: WorkloadPolicyStatus | string;
  approvedBy: string;
  approvedDate: string;
  createdAt: string;
}

/**
 * Strongly-typed DTO representing a base materialized workload commitment snapshot (WorkloadCommitmentDto).
 */
export interface WorkloadCommitmentDto {
  id: string;
  teacherProfileId: string;
  academicPeriodId: string;
  workloadPolicyId: string;
  plannedWorkloadHours: number;
  committedWorkloadHours: number;
  maxCapacityLimitHours: number;
  remainingCapacityHours: number;
  status: WorkloadCommitmentStatus | string;
  activeAssignmentCount: number;
  approvedExceptionCount: number;
  asOfTimestamp: string;
  createdAt: string;
}

/**
 * Strongly-typed response DTO representing a faculty member's materialized capacity snapshot
 * joined with teacher profile and governing policy information (TeacherWorkloadCommitmentDto).
 */
export interface TeacherWorkloadCommitmentDto {
  id: string;
  teacherProfileId: string;
  teacherNumber: string;
  fullName: string;
  displayName?: string | null;
  campusAffiliation?: string | null;
  standing: string;
  academicPeriodId: string;
  workloadPolicyId: string;
  policyCode?: string | null;
  policyName?: string | null;
  maxStandardCapacity: number;
  warningThreshold: number;
  prohibitedThreshold: number;
  plannedWorkloadHours: number;
  committedWorkloadHours: number;
  maxCapacityLimitHours: number;
  remainingCapacityHours: number;
  status: WorkloadCommitmentStatus | string;
  activeAssignmentCount: number;
  approvedExceptionCount: number;
  asOfTimestamp: string;
  createdAt: string;
}

/**
 * Strongly-typed contract DTO representing an authorized workload exception (WorkloadExceptionDto).
 */
export interface WorkloadExceptionDto {
  id: string;
  exceptionNumber: string;
  teachingAssignmentId: string;
  teacherProfileId: string;
  workloadPolicyId: string;
  exceededHours: number;
  justification: string;
  recommendedBy: string;
  recommendedDate: string;
  approvedBy?: string | null;
  approvedDate?: string | null;
  effectiveStartDate: string;
  effectiveEndDate: string;
  status: WorkloadExceptionStatus | string;
  decisionRemarks?: string | null;
  createdAt: string;
}

/**
 * Strongly-typed DTO representing a capacity/schedule conflict condition (CapacityConflictDto).
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

// ============================================================================
// Query Parameters
// ============================================================================

export interface GetWorkloadPoliciesParams {
  academicYearId?: string | null;
  status?: string | null;
}

export interface GetWorkloadCommitmentsParams {
  academicPeriodId?: string | null;
  teacherProfileId?: string | null;
  status?: string | null;
  search?: string | null;
}

export interface GetWorkloadExceptionsParams {
  academicPeriodId?: string | null;
  teacherProfileId?: string | null;
  status?: string | null;
}

export interface GetCapacityConflictsParams {
  academicPeriodId?: string | null;
  severity?: string | null;
  status?: string | null;
  teacherProfileId?: string | null;
  page?: number;
  pageSize?: number;
}

// ============================================================================
// Mutation Request & Response Types
// ============================================================================

export interface RecalculatePeriodWorkloadRequest {
  academicPeriodId: string;
}

export interface RecalculatePeriodWorkloadResponse {
  academicPeriodId: string;
  recalculatedCount: number;
  recalculatedAt: string;
  message: string;
}

export interface RequestWorkloadExceptionRequest {
  teacherProfileId: string;
  academicPeriodId: string;
  teachingAssignmentId?: string | null;
  requestedOverloadHours: number;
  coordinatorJustification: string;
  effectiveStartDate?: string | null;
  effectiveEndDate?: string | null;
  status?: string | null;
}

export interface RequestWorkloadExceptionResponse {
  id: string;
  exceptionNumber: string;
  teachingAssignmentId: string;
  teacherProfileId: string;
  academicPeriodId: string;
  workloadPolicyId: string;
  exceededHours: number;
  justification: string;
  recommendedBy: string;
  recommendedDate: string;
  effectiveStartDate: string;
  effectiveEndDate: string;
  status: string;
  createdAt: string;
}

export interface ApproveWorkloadExceptionRequest {
  decisionRemarks?: string | null;
}

export interface ApproveWorkloadExceptionResponse {
  id: string;
  exceptionNumber: string;
  teachingAssignmentId: string;
  teacherProfileId: string;
  academicPeriodId: string;
  exceededHours: number;
  status: string;
  approvedBy: string;
  approvedDate: string;
  decisionRemarks?: string | null;
  approvedExceptionCount: number;
  commitmentStatus: string;
  resolvedConflictsCount: number;
  approvedAt: string;
}
