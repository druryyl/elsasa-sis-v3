/**
 * Authoritative TypeScript interfaces and types for Coursework Assignments and Submissions (SCR-ACD-02, P7-S17).
 * Strictly mirrors backend vertical slice DTOs, domain models, and SQL Server projections.
 */

// ============================================================================
// Lifecycle & Domain Enums / Type Unions
// ============================================================================

export type AssignmentStatus = 'draft' | 'released' | 'closed';

export type SubmissionStatus =
  | 'submitted'
  | 'late'
  | 'missing'
  | 'excused'
  | 'exempted';

export type SubmissionDisplayStatus =
  | 'pending'
  | 'submitted'
  | 'late'
  | 'missing'
  | 'excused'
  | 'exempted';

export type SubmissionExceptionType = 'excused' | 'exempted';

// ============================================================================
// Core Models & Projections
// ============================================================================

/**
 * Coursework assignment projection (P5-S10, TD-05).
 */
export interface Assignment {
  id: string;
  learningActivityId: string;
  learningActivityTitle?: string;
  learningActivityType?: string;
  learningActivityDate?: string;
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  status: AssignmentStatus | string;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Coursework submission roster item projecting student participation and on-the-fly status (SCR-ACD-02, TD-06, P5-S12).
 */
export interface AssignmentSubmission {
  studentId: string;
  studentNumber?: string | null;
  studentName?: string | null;
  sectionLabel?: string | null;
  displayStatus: SubmissionDisplayStatus | string;
  submissionId?: string | null;
  submittedDate?: string | null;
  submissionReference?: string | null;
  isResubmission: boolean;
  notes?: string | null;
  createdAt?: string | null;
  createdBy?: string | null;
}

// ============================================================================
// Command Requests
// ============================================================================

/**
 * Request payload for creating an assignment under a learning activity (P5-S10, UC-ACD-04).
 */
export interface CreateAssignmentRequest {
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  notes?: string | null;
}

/**
 * Request payload for recording an administrative policy exception for a student (P5-S11, TD-06).
 */
export interface RecordExceptionRequest {
  studentId: string;
  exceptionType: SubmissionExceptionType | string;
  reason: string;
}

/**
 * Request payload for student coursework submission (P5-S11, UC-ACD-01).
 */
export interface SubmitCourseworkRequest {
  studentId: string;
  submissionReference: string;
  notes?: string | null;
}

// ============================================================================
// Command Responses
// ============================================================================

export interface CreateAssignmentResponse {
  id: string;
  learningActivityId: string;
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  status: string;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}

export interface ReleaseAssignmentResponse {
  id: string;
  learningActivityId: string;
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  status: string;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface CloseAssignmentResponse {
  id: string;
  learningActivityId: string;
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  status: string;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface RecordExceptionResponse {
  id: string;
  assignmentId: string;
  studentId: string;
  status: string;
  submittedDate?: string | null;
  submissionReference?: string | null;
  isResubmission: boolean;
  previousSubmissionId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}

export interface SubmitCourseworkResponse {
  id: string;
  assignmentId: string;
  studentId: string;
  status: string;
  submittedDate: string;
  submissionReference?: string | null;
  isResubmission: boolean;
  previousSubmissionId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}
