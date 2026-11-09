/**
 * TypeScript Interfaces, Enums and DTOs for Student Learning Plans (SCR-ACD-04, UC-ACD-09, UC-ACD-10, P6-S17).
 */

// ============================================================================
// Enums / Const Literal Types
// ============================================================================

export type StudentLearningPlanStatus =
  | 'draft'
  | 'active'
  | 'revised'
  | 'completed'
  | 'archived';

export const STUDENT_LEARNING_PLAN_STATUSES: readonly StudentLearningPlanStatus[] = [
  'draft',
  'active',
  'revised',
  'completed',
  'archived',
] as const;

export type AdjustmentType =
  | 'accommodation'
  | 'substitution'
  | 'exemption';

export const ADJUSTMENT_TYPES: readonly AdjustmentType[] = [
  'accommodation',
  'substitution',
  'exemption',
] as const;

export type ApprovalType =
  | 'initial_approval'
  | 'revision_approval';

export const APPROVAL_TYPES: readonly ApprovalType[] = [
  'initial_approval',
  'revision_approval',
] as const;

// ============================================================================
// Core Entities & DTOs
// ============================================================================

/**
 * Constituent learning unit within a student learning plan (academics.slp_learning_units).
 */
export interface SlpLearningUnit {
  id: string;
  learningPlanId?: string;
  learningUnitId: string;
  unitCode: string;
  title: string;
  sequenceOrder?: number | null;
  isRequired: boolean;
  notes?: string | null;
  createdAt?: string;
  createdBy?: string;
}

/**
 * Formal academic plan adjustment (accommodation, substitution, or exemption) (academics.academic_plan_adjustments).
 */
export interface AcademicPlanAdjustment {
  id: string;
  learningPlanId?: string;
  adjustmentType: AdjustmentType;
  description: string;
  rationale: string;
  effectiveDate: string; // YYYY-MM-DD
  endDate?: string | null; // YYYY-MM-DD
  approvedBy?: string | null;
  approvedDate?: string | null; // YYYY-MM-DD
  notes?: string | null;
  createdAt?: string;
  createdBy?: string;
}

/**
 * Immutable audit log record of a formal plan approval decision (academics.slp_approvals).
 */
export interface SlpApproval {
  id: string;
  learningPlanId?: string;
  planVersion: number;
  approvedBy: string;
  approvedDate: string; // YYYY-MM-DD
  approvalType: ApprovalType;
  notes?: string | null;
  createdAt?: string;
}

/**
 * Authoritative record of a complete Student Learning Plan with constituent units, adjustments, and approval history (academics.student_learning_plans).
 */
export interface StudentLearningPlan {
  id: string;
  studentId: string;
  studentName?: string;
  studentNis?: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  status: StudentLearningPlanStatus;
  version: number;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
  constituentUnits: SlpLearningUnit[];
  adjustments: AcademicPlanAdjustment[];
  approvals: SlpApproval[];
}

/**
 * Summary item for paginated or filterable learning plan roster navigation (SCR-ACD-04 Master Sidebar).
 */
export interface StudentLearningPlanListItem {
  id: string;
  studentId: string;
  studentName?: string;
  studentNis?: string;
  gradeLevel?: string | null;
  academicPeriodId: string;
  academicPeriodLabel: string;
  status: StudentLearningPlanStatus;
  version: number;
  unitsCount: number;
  adjustmentsCount: number;
  createdAt?: string;
  updatedAt?: string | null;
}

/**
 * Contract representation of an active student learning plan and its active constituents (IAcademicsModule contract query).
 */
export interface ActiveStudentLearningPlan {
  planId: string;
  studentId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  version: number;
  status: StudentLearningPlanStatus;
  notes?: string | null;
  constituentUnits: SlpLearningUnit[];
  adjustments: AcademicPlanAdjustment[];
}

// ============================================================================
// Request & Response Types
// ============================================================================

export interface CreatePlanRequest {
  studentId: string;
  academicPeriodId: string;
  academicPeriodLabel?: string;
  notes?: string | null;
}

export interface UpdatePlanRequest {
  notes?: string | null;
}

export interface AddUnitRequest {
  learningUnitId: string;
  sequenceOrder?: number | null;
  isRequired: boolean;
  notes?: string | null;
}

export interface AddAdjustmentRequest {
  adjustmentType: AdjustmentType;
  description: string;
  rationale: string;
  effectiveDate: string;
  endDate?: string | null;
  notes?: string | null;
}

export interface ApprovePlanRequest {
  approvalType: ApprovalType;
  notes?: string | null;
}

export interface RevisePlanRequest {
  notes?: string | null;
}

export interface ArchivePlanRequest {
  status?: 'archived' | 'completed';
}

export interface SyncParticipationsResponse {
  planId: string;
  studentId: string;
  syncedCount: number;
  updatedCount: number;
  totalUnits: number;
}

export interface GetLearningPlansParams {
  academicPeriodId?: string;
  status?: StudentLearningPlanStatus | string;
  studentSearch?: string;
  page?: number;
  pageSize?: number;
}

export interface GetActiveLearningPlanParams {
  academicPeriodId: string;
}
