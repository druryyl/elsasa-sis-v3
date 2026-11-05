/**
 * Authoritative TypeScript interfaces and types for Student Portal Learning Workspace (SCR-SELF-03, P7-S17).
 * Strictly mirrors backend vertical slice DTOs, domain models, and SQL Server projections.
 */

// ============================================================================
// Lifecycle & Domain Enums / Type Unions
// ============================================================================

export type CourseworkStatusFilter =
  | 'all'
  | 'upcoming'
  | 'due_soon'
  | 'submitted'
  | 'missing';

export type SubmissionType = 'link' | 'text' | 'file';

// ============================================================================
// Core Student Workspace Models & Projections
// ============================================================================

/**
 * Coursework task item projected for the student self-service workspace (SCR-SELF-03, TD-06, P5-S12).
 */
export interface StudentCourseworkTask {
  assignmentId: string;
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  assignmentStatus: string;
  learningActivityId: string;
  learningActivityTitle: string;
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitName: string;
  sectionLabel?: string | null;
  displayStatus: string;
  countdownDays?: number | null;
  submissionId?: string | null;
  submittedDate?: string | null;
  submissionReference?: string | null;
  isResubmission: boolean;
  submissionNotes?: string | null;
  previousSubmissionId?: string | null;
}

// ============================================================================
// Command Requests
// ============================================================================

/**
 * Request contract for submitting coursework (TD-07, UC-ACD-01, P5-S11).
 */
export interface SubmitCourseworkRequest {
  studentId: string;
  submissionReference: string;
  notes?: string | null;
}

// ============================================================================
// Command Responses
// ============================================================================

/**
 * Response contract returned upon uploading a submission file (TD-07, P5-S11).
 */
export interface FileUploadResponse {
  storagePath: string;
  fileName: string;
  originalFileName?: string;
  filePath?: string;
  fileSizeBytes: number;
  contentType: string;
}

/**
 * Response contract returned upon submitting coursework (P5-S11, UC-ACD-01).
 */
export interface SubmitCourseworkResponse {
  id: string;
  assignmentId: string;
  studentId: string;
  status: string;
  submissionStatus?: string;
  submittedDate: string;
  submissionReference?: string | null;
  isResubmission: boolean;
  previousSubmissionId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}
