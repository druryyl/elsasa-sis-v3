/**
 * TypeScript Interfaces, Enums and DTOs for Learning Progress & Completion Tracking (SCR-ACD-07, ACD-WF-11, UC-ACD-12, P7-S19)
 */

// ============================================================================
// Enums / Const Literal Types
// ============================================================================

export type LearningProgressStatus =
  | 'not_started'
  | 'in_progress'
  | 'completed'
  | 'needs_remediation'
  | 'exempted';

export const LEARNING_PROGRESS_STATUSES: readonly LearningProgressStatus[] = [
  'not_started',
  'in_progress',
  'completed',
  'needs_remediation',
  'exempted',
] as const;

export type LearningProgressBasis =
  | 'assessment_evidence'
  | 'learning_work'
  | 'authorized_decision';

export const LEARNING_PROGRESS_BASES: readonly LearningProgressBasis[] = [
  'assessment_evidence',
  'learning_work',
  'authorized_decision',
] as const;

// ============================================================================
// Core Entities & DTOs
// ============================================================================

/**
 * Authoritative record of student advancement toward unit expectations (academics.learning_progress).
 */
export interface LearningProgress {
  id?: string;
  progressId?: string | null;
  studentId: string;
  studentName?: string | null;
  studentIdentifier?: string | null;
  studentNumber?: string | null;
  learningUnitId: string;
  learningUnitName?: string | null;
  learningUnitCode?: string | null;
  academicPeriodId: string;
  academicPeriodLabel?: string | null;
  status?: LearningProgressStatus;
  progressStatus?: LearningProgressStatus;
  basis?: LearningProgressBasis | string | null;
  participationId?: string | null;
  participationStatus?: string | null;
  sectionLabel?: string | null;
  operativeGrade?: string | null;
  operativeScore?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  completionDate?: string | null;
  notes?: string | null;
  updatedBy?: string | null;
  updatedAt?: string | null;
  createdAt?: string | null;
  createdBy?: string | null;
}

// ============================================================================
// Request / Response Payloads
// ============================================================================

/**
 * Payload for recording or updating student advancement with explicit completion date or RPL (UC-ACD-12).
 */
export interface RecordProgressRequest {
  studentId: string;
  learningUnitId: string;
  academicPeriodId: string;
  status: LearningProgressStatus;
  basis: LearningProgressBasis;
  completionDate?: string | null;
  notes?: string | null;
}

/**
 * Payload for formal conclusion of learning unit participation (UC-ACD-12).
 */
export interface ConcludeParticipationRequest {
  participationId?: string | null;
  studentId?: string;
  learningUnitId?: string;
  academicPeriodId?: string;
  completionDate?: string | null;
  notes?: string | null;
}

/**
 * Result of formal conclusion of learning unit participation.
 */
export interface ConcludeParticipationResponse {
  progressId: string;
  participationStatus: string;
  concludedAt: string;
}
