/**
 * TypeScript types and DTOs for Financial Obligations & Assessment Engine (GAP-003, GAP-004, TD-04).
 */

export type ObligationStatus =
  | 'assessed'
  | 'issued'
  | 'partially_settled'
  | 'settled'
  | 'waived'
  | 'cancelled';

export type DueScheduleStatus = 'pending' | 'invoiced' | 'partially_paid' | 'paid' | 'overdue';

export type AssessmentMode = 'preview' | 'commit';

/**
 * Installment due schedule attached to an obligation.
 */
export interface DueScheduleDto {
  id: string;
  obligationId: string;
  installmentSequence: number;
  dueDate: string;
  scheduledAmount: number;
  currencyCode: string;
  status: DueScheduleStatus | string;
  notes?: string | null;
}

/**
 * Authoritative liability record for a student.
 */
export interface FinancialObligationDto {
  id: string;
  obligationNumber: string;
  studentAccountId: string;
  studentId: string;
  studentNumber: string;
  studentFullName: string;
  policyId: string;
  policyCode: string | null;
  chargeRuleId: string | null;
  chargeRuleCode: string | null;
  description: string;
  chargeType: string;
  isMandatory: boolean;
  originalAmount: number;
  currencyCode: string;
  issueDate: string;
  dueDate: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  status: ObligationStatus | string;
  authorizedBy: string;
  notes: string | null;
  createdAt: string;
  schedules: DueScheduleDto[];
}

/**
 * Candidate student item evaluated during a batch billing assessment cycle.
 */
export interface BatchAssessCandidateDto {
  studentId: string;
  studentNumber: string;
  studentFullName: string;
  campus: string;
  grade: string;
  accountId: string | null;
  accountNumber: string | null;
  baseAmount: number;
  dueDate: string;
  isDuplicate: boolean;
  notes?: string | null;
}

/**
 * Command payload for executing a batch assessment billing cycle (preview or commit).
 */
export interface BatchAssessRequest {
  chargeRuleId: string;
  academicPeriodId: string;
  billingCycleLabel: string;
  targetCampus?: string | null;
  targetGrade?: string | null;
  mode: AssessmentMode;
}

/**
 * Response payload for dry-run preview evaluation of candidate assessments.
 */
export interface BatchAssessPreviewResponse {
  totalCandidatesCount: number;
  eligibleCandidatesCount: number;
  duplicateCount: number;
  grossAssessedAmount: number;
  candidates: BatchAssessCandidateDto[];
  createdObligationIds: string[];
}

/**
 * Response payload for committed batch assessment billing cycle execution.
 */
export interface BatchAssessCommitResponse {
  totalCandidatesCount: number;
  eligibleCandidatesCount: number;
  duplicateCount: number;
  grossAssessedAmount: number;
  candidates: BatchAssessCandidateDto[];
  createdObligationIds: string[];
}

/**
 * Command payload for single-student ad-hoc obligation assessment.
 */
export interface AssessAdHocObligationRequest {
  studentId: string;
  chargeType: string;
  description: string;
  amount: number;
  dueDate: string;
  academicPeriodId: string;
  notes?: string | null;
  policyId?: string | null;
}

/**
 * Response summary produced after assessing an ad-hoc student obligation.
 */
export interface AssessAdHocObligationResponse {
  id: string;
  obligationNumber: string;
  studentAccountId: string;
  studentId: string;
  policyId: string;
  chargeRuleId: string | null;
  description: string;
  chargeType: string;
  isMandatory: boolean;
  originalAmount: number;
  currencyCode: string;
  issueDate: string;
  dueDate: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  status: string;
  authorizedBy: string;
  notes: string | null;
  initialScheduleId: string;
  createdAt: string;
  createdBy: string | null;
}

/**
 * Filter parameters for querying financial obligations.
 */
export interface GetObligationsParams {
  studentId?: string;
  status?: string;
  academicPeriodId?: string;
  chargeType?: string;
  page?: number;
  pageNumber?: number;
  pageSize?: number;
}
