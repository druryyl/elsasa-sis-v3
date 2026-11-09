/**
 * Authoritative TypeScript interfaces and types for the Academic Participation module (SCR-ACD-05, P7-S17).
 * Strictly mirrors backend vertical slice DTOs, domain models, and SQL Server projections.
 */

// ============================================================================
// Lifecycle & Domain Enums / Type Unions
// ============================================================================

export type ParticipationType =
  | 'standard'
  | 'repeat'
  | 'transferred_in'
  | 'recognized_prior';

export type ParticipationSource =
  | 'assigned'
  | 'student_selected'
  | 'learning_plan';

export type ParticipationStatus =
  | 'in_progress'
  | 'paused'
  | 'withdrawn'
  | 'completed';

// ============================================================================
// Core Academic Participation Models & Projections
// ============================================================================

/**
 * Unit roster participation projection (P2-S06, TD-08, SCR-ACD-05).
 */
export interface AcademicParticipation {
  id: string;
  studentId: string;
  studentNumber?: string | null;
  studentName?: string | null;
  learningUnitId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  participationType: ParticipationType | string;
  attemptNumber: number;
  source: ParticipationSource | string;
  sectionLabel?: string | null;
  status: ParticipationStatus | string;
  startDate: string;
  endDate?: string | null;
  completionDate?: string | null;
  sourceInstitution?: string | null;
  externalGradeOrCredit?: string | null;
  recognitionBasis?: string | null;
  approvedBy?: string | null;
  studentLearningPlanId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Student-centric participation projection including unit metadata (P2-S06).
 */
export interface StudentParticipation {
  id: string;
  studentId: string;
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitName: string;
  creditWeight?: number | null;
  academicPeriodId: string;
  academicPeriodLabel: string;
  participationType: ParticipationType | string;
  attemptNumber: number;
  source: ParticipationSource | string;
  sectionLabel?: string | null;
  status: ParticipationStatus | string;
  startDate: string;
  endDate?: string | null;
  completionDate?: string | null;
  sourceInstitution?: string | null;
  externalGradeOrCredit?: string | null;
  recognitionBasis?: string | null;
  approvedBy?: string | null;
  studentLearningPlanId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

// ============================================================================
// Command Requests
// ============================================================================

/**
 * Request payload for atomic batch cohort registration (P2-S04, TD-02).
 */
export interface BatchRegisterCohortRequest {
  academicPeriodId: string;
  academicPeriodLabel: string;
  learningUnitIds: string[];
  sectionLabel: string;
  startDate: string;
  studentIds?: string[];
}

/**
 * Request payload for individual student participation registration (P2-S04, TD-02).
 */
export interface RegisterParticipationRequest {
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  sectionLabel?: string | null;
  participationType: ParticipationType | string;
  source: ParticipationSource | string;
  startDate: string;
  studentLearningPlanId?: string | null;
}

/**
 * Request payload for recognizing transfer or prior learning credit (P2-S05, GAP-08, UC-ACD-11).
 */
export interface RecognizePriorCreditRequest {
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  participationType: 'transferred_in' | 'recognized_prior' | string;
  sourceInstitution?: string | null;
  externalGradeOrCredit?: string | null;
  recognitionBasis?: string | null;
  startDate: string;
  completionDate: string;
  studentLearningPlanId?: string | null;
  notes?: string | null;
}

/**
 * Request payload for transitioning participation status (P2-S05).
 */
export interface UpdateParticipationStatusRequest {
  newStatus: 'in_progress' | 'paused' | 'withdrawn' | string;
  notes?: string | null;
}

/**
 * Request payload for concluding a unit participation (P2-S05, UC-ACD-12).
 */
export interface ConcludeParticipationRequest {
  completionDate: string;
  notes?: string | null;
}

// ============================================================================
// Command Responses
// ============================================================================

export interface BatchRegisterCohortResponse {
  registeredCount: number;
  skippedCount: number;
}

export interface RegisterParticipationResponse {
  id: string;
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  participationType: string;
  attemptNumber: number;
  source: string;
  sectionLabel?: string | null;
  status: string;
  startDate: string;
  createdAt: string;
  createdBy: string;
}

export interface RecognizePriorCreditResponse {
  id: string;
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  participationType: string;
  attemptNumber: number;
  status: string;
  startDate: string;
  endDate: string;
  completionDate: string;
  sourceInstitution?: string | null;
  externalGradeOrCredit?: string | null;
  recognitionBasis?: string | null;
  approvedBy: string;
  studentLearningPlanId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}

export interface UpdateParticipationStatusResponse {
  id: string;
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  status: string;
  startDate: string;
  endDate?: string | null;
  notes?: string | null;
  updatedAt: string;
  updatedBy: string;
}

export interface ConcludeParticipationResponse {
  id: string;
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  status: string;
  startDate: string;
  endDate: string;
  completionDate: string;
  notes?: string | null;
  updatedAt: string;
  updatedBy: string;
}

// ============================================================================
// Query Filter Parameters
// ============================================================================

export interface UnitParticipationsFilter {
  academicPeriodId?: string;
  sectionLabel?: string;
  status?: string;
}

export interface StudentParticipationsFilter {
  academicPeriodId?: string;
}
