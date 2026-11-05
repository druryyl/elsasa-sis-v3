/**
 * Authoritative TypeScript interfaces and types for Learning Activities and Attendance (SCR-ACD-01, P7-S17).
 * Strictly mirrors backend vertical slice DTOs, domain models, and SQL Server projections.
 */

// ============================================================================
// Lifecycle & Domain Enums / Type Unions
// ============================================================================

export type ActivityType =
  | 'lesson'
  | 'tutorial'
  | 'laboratory'
  | 'independent_work'
  | 'project'
  | 'discussion'
  | 'practice'
  | 'examination'
  | 'remediation'
  | 'homeroom_session'
  | 'other';

export type ActivityStatus = 'planned' | 'conducted' | 'cancelled';

export type AttendanceStatus =
  | 'present'
  | 'absent'
  | 'late'
  | 'excused_absence'
  | 'partial';

// ============================================================================
// Core Activity & Attendance Models & Projections
// ============================================================================

/**
 * Standard learning activity entity projection (P3-S07, TD-03).
 */
export interface LearningActivity {
  id: string;
  learningUnitId: string;
  activityType: ActivityType | string;
  title: string;
  description?: string | null;
  sectionLabel?: string | null;
  activityDate: string;
  startTime?: string | null;
  endTime?: string | null;
  teacherAssignmentId?: string | null;
  teacherName?: string | null;
  status: ActivityStatus | string;
  isAttendanceRequired: boolean;
  deliveryNotes?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Daily schedule activity projection for teachers (SCR-ACD-01, P3-S07).
 */
export interface TeacherDailyActivity {
  id: string;
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitName: string;
  activityType: ActivityType | string;
  title: string;
  description?: string | null;
  sectionLabel?: string | null;
  activityDate: string;
  startTime?: string | null;
  endTime?: string | null;
  teacherAssignmentId?: string | null;
  teacherName?: string | null;
  status: ActivityStatus | string;
  isAttendanceRequired: boolean;
  deliveryNotes?: string | null;
}

/**
 * Summary counts of attendance for an activity (P3-S07).
 */
export interface ActivityAttendanceSummary {
  totalCount: number;
  presentCount: number;
  absentCount: number;
  lateCount: number;
  excusedCount: number;
  partialCount: number;
}

/**
 * Assignment preview item associated with a learning activity (P3-S07).
 */
export interface ActivityAssignmentItem {
  id: string;
  title: string;
  description?: string | null;
  releaseDate: string;
  dueDate?: string | null;
  maxScore?: number | null;
  weightPercentage?: number | null;
  status: string;
  createdAt: string;
}

/**
 * Detailed activity inspection response including attendance summary and assignments (P3-S07).
 */
export interface ActivityDetail {
  id: string;
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitName: string;
  activityType: ActivityType | string;
  title: string;
  description?: string | null;
  sectionLabel?: string | null;
  activityDate: string;
  startTime?: string | null;
  endTime?: string | null;
  teacherAssignmentId?: string | null;
  teacherName?: string | null;
  status: ActivityStatus | string;
  isAttendanceRequired: boolean;
  deliveryNotes?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
  attendanceSummary: ActivityAttendanceSummary;
  assignments: ActivityAssignmentItem[];
}

/**
 * Immutable audit log entry representing a historical attendance correction (P4-S09, UC-ACD-15, TD-04).
 */
export interface AttendanceCorrection {
  id: string;
  attendanceRecordId: string;
  previousStatus: string;
  newStatus: string;
  reason: string;
  correctedBy: string;
  correctedDate: string;
  evidence?: string | null;
  createdAt: string;
}

/**
 * Roster item representing a student attendance record and full audit trail (P4-S09, TD-04).
 */
export interface AttendanceRecord {
  id: string;
  learningActivityId: string;
  studentId: string;
  studentNumber?: string | null;
  studentName?: string | null;
  status: AttendanceStatus | string;
  recordedBy: string;
  recordedDate: string;
  evidence?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
  corrections: AttendanceCorrection[];
}

/**
 * Aggregate student attendance summary response (P4-S09, TD-04).
 */
export interface StudentAttendanceSummary {
  studentId: string;
  academicPeriodId?: string | null;
  totalSessions: number;
  totalPresent: number;
  totalAbsent: number;
  totalLate: number;
  totalExcused: number;
  totalPartial: number;
  attendanceRatePercentage: number;
}

// ============================================================================
// Command Requests
// ============================================================================

/**
 * Request payload for planning an instructional activity or homeroom session (P3-S07, TD-03).
 */
export interface PlanActivityRequest {
  learningUnitId: string;
  activityType: ActivityType | string;
  title: string;
  description?: string | null;
  sectionLabel?: string | null;
  activityDate: string;
  startTime?: string | null;
  endTime?: string | null;
  teacherAssignmentId?: string | null;
  teacherName?: string | null;
  isAttendanceRequired?: boolean;
}

/**
 * Request payload for conducting a scheduled activity and recording delivery notes (UC-ACD-02, P3-S07).
 */
export interface ConductActivityRequest {
  deliveryNotes?: string | null;
  actualTeacherName?: string | null;
}

/**
 * Request payload for cancelling a scheduled learning activity (P3-S07).
 */
export interface CancelActivityRequest {
  cancellationReason: string;
}

/**
 * Student attendance entry for roll-call submission (P4-S08, UC-ACD-03).
 */
export interface StudentAttendanceEntry {
  studentId: string;
  status: AttendanceStatus | string;
  evidence?: string | null;
  notes?: string | null;
}

/**
 * Request payload for roll-call attendance recording (P4-S08, UC-ACD-03).
 */
export interface RecordAttendanceRequest {
  entries: StudentAttendanceEntry[];
}

/**
 * Request payload for auditable historical attendance correction (P4-S09, UC-ACD-15, TD-04).
 */
export interface CorrectAttendanceRequest {
  newStatus: AttendanceStatus | string;
  reason: string;
  evidence?: string | null;
}

// ============================================================================
// Command Responses
// ============================================================================

export interface PlanActivityResponse {
  id: string;
  learningUnitId: string;
  activityType: string;
  title: string;
  description?: string | null;
  sectionLabel?: string | null;
  activityDate: string;
  startTime?: string | null;
  endTime?: string | null;
  teacherAssignmentId?: string | null;
  teacherName?: string | null;
  status: string;
  isAttendanceRequired: boolean;
  deliveryNotes?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}

export interface ConductActivityResponse {
  id: string;
  learningUnitId: string;
  activityType: string;
  title: string;
  sectionLabel?: string | null;
  activityDate: string;
  status: string;
  deliveryNotes?: string | null;
  teacherName?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface CancelActivityResponse {
  id: string;
  learningUnitId: string;
  title: string;
  status: string;
  notes?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface RecordAttendanceResponse {
  learningActivityId: string;
  totalRecorded: number;
  totalPresent: number;
  totalAbsent: number;
  totalLate: number;
  totalExcused: number;
  totalPartial: number;
  recordedDate: string;
  recordedBy: string;
}

export interface CorrectAttendanceResponse {
  id: string;
  learningActivityId: string;
  studentId: string;
  status: string;
  recordedBy: string;
  recordedDate: string;
  evidence?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
  latestCorrection: AttendanceCorrection;
}

// ============================================================================
// Query Filter Parameters
// ============================================================================

export interface UnitActivitiesFilter {
  fromDate?: string;
  toDate?: string;
  sectionLabel?: string;
  status?: string;
}

export interface TeacherActivitiesFilter {
  date?: string;
}
