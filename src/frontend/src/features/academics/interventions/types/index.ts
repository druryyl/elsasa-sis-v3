/**
 * TypeScript Interfaces, Enums and DTOs for Academic Interventions (SCR-ACD-06, ACD-WF-10, UC-ACD-11, P7-S19)
 */

// ============================================================================
// Enums / Const Literal Types
// ============================================================================

export type InterventionType =
  | 'remediation'
  | 'reassessment'
  | 'tutoring_recommendation'
  | 'extension_work'
  | 'recovery_plan'
  | 'learning_plan_adjustment'
  | 'academic_warning';

export const INTERVENTION_TYPES: readonly InterventionType[] = [
  'remediation',
  'reassessment',
  'tutoring_recommendation',
  'extension_work',
  'recovery_plan',
  'learning_plan_adjustment',
  'academic_warning',
] as const;

export type TriggerType =
  | 'assessment_below_threshold'
  | 'missing_work'
  | 'attendance_concern'
  | 'standing_concern'
  | 'teacher_referral';

export const TRIGGER_TYPES: readonly TriggerType[] = [
  'assessment_below_threshold',
  'missing_work',
  'attendance_concern',
  'standing_concern',
  'teacher_referral',
] as const;

export type InterventionStatus =
  | 'planned'
  | 'in_progress'
  | 'completed'
  | 'cancelled';

export const INTERVENTION_STATUSES: readonly InterventionStatus[] = [
  'planned',
  'in_progress',
  'completed',
  'cancelled',
] as const;

// ============================================================================
// Core Entities & DTOs
// ============================================================================

/**
 * Authoritative record of an academic intervention plan and lifecycle (academics.academic_interventions).
 */
export interface AcademicIntervention {
  id: string;
  studentId: string;
  studentName?: string;
  studentIdentifier?: string;
  learningUnitId: string;
  learningUnitName?: string;
  learningUnitCode?: string;
  academicPeriodId: string;
  academicPeriodLabel?: string;
  interventionType: InterventionType;
  triggerType: TriggerType;
  triggerDescription: string;
  planDescription: string;
  status: InterventionStatus;
  initiatedBy: string;
  initiatedAt: string;
  concludedAt?: string | null;
  outcomeNotes?: string | null;
  createdAt?: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

/**
 * Dynamic at-risk student surfaced by the real-time On-Demand At-Risk Query Engine (TD-04, GAP-004).
 */
export interface AtRiskLearner {
  studentId: string;
  studentName: string;
  studentIdentifier?: string;
  learningUnitId: string;
  learningUnitName: string;
  learningUnitCode?: string;
  academicPeriodId: string;
  gradeLevel: string;
  triggerType: TriggerType;
  triggerDescription: string;
  currentScore?: number | null;
  passingScore?: number | null;
  attendancePercentage?: number | null;
  missingAssignmentsCount?: number | null;
  currentStanding?: string | null;
  detectedAt?: string;
}

// ============================================================================
// Request / Query Payloads
// ============================================================================

/**
 * Payload for formulating and initiating a structured academic intervention (UC-ACD-11).
 */
export interface InitiateInterventionRequest {
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  interventionType: InterventionType;
  triggerType: TriggerType;
  triggerDescription: string;
  planDescription: string;
  targetCompletionDate?: string | null;
}

/**
 * Payload for transitioning intervention status with recorded outcome notes (UC-ACD-11).
 */
export interface UpdateInterventionStatusRequest {
  status: InterventionStatus;
  outcomeNotes?: string | null;
}

/**
 * Filter parameters for querying live at-risk learners.
 */
export interface GetAtRiskLearnersParams {
  academicPeriodId: string;
  gradeLevel?: string;
}

/**
 * Filter parameters for querying active or historical interventions.
 */
export interface GetInterventionsParams {
  academicPeriodId?: string;
  status?: InterventionStatus | string;
  studentId?: string;
  learningUnitId?: string;
}
