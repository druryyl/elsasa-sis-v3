/**
 * TypeScript Interfaces, Enums and DTOs for Academics Assessment & Results (P8-S19, UC-ACD-05, UC-ACD-06, UC-ACD-07, GAP-008, TD-10)
 */

// ============================================================================
// Enums / Const Literal Types
// ============================================================================

export type AssessmentType =
  | 'formative'
  | 'summative'
  | 'diagnostic'
  | 'portfolio'
  | 'project'
  | 'other';

export const ASSESSMENT_TYPES: readonly AssessmentType[] = [
  'formative',
  'summative',
  'diagnostic',
  'portfolio',
  'project',
  'other',
] as const;

export type AssessmentMethod =
  | 'written_exam'
  | 'oral_exam'
  | 'practical'
  | 'observation'
  | 'assignment'
  | 'presentation'
  | 'portfolio'
  | 'peer_review'
  | 'self_evaluation'
  | 'other';

export const ASSESSMENT_METHODS: readonly AssessmentMethod[] = [
  'written_exam',
  'oral_exam',
  'practical',
  'observation',
  'assignment',
  'presentation',
  'portfolio',
  'peer_review',
  'self_evaluation',
  'other',
] as const;

export type ReassessmentPolicy =
  | 'none'
  | 'capped_at_passing'
  | 'highest_score'
  | 'latest_attempt'
  | 'average';

export const REASSESSMENT_POLICIES: readonly ReassessmentPolicy[] = [
  'none',
  'capped_at_passing',
  'highest_score',
  'latest_attempt',
  'average',
] as const;

export type AssessmentStatus = 'planned' | 'conducted' | 'cancelled';

export const ASSESSMENT_STATUSES: readonly AssessmentStatus[] = [
  'planned',
  'conducted',
  'cancelled',
] as const;

export type EvidenceType =
  | 'submission'
  | 'test_paper'
  | 'observation'
  | 'interview'
  | 'portfolio_piece'
  | 'project_artifact'
  | 'other';

export const EVIDENCE_TYPES: readonly EvidenceType[] = [
  'submission',
  'test_paper',
  'observation',
  'interview',
  'portfolio_piece',
  'project_artifact',
  'other',
] as const;

export type EvaluationModel =
  | 'numeric'
  | 'letter_grade'
  | 'pass_fail'
  | 'competency'
  | 'rubric'
  | 'narrative'
  | 'completion';

export const EVALUATION_MODELS: readonly EvaluationModel[] = [
  'numeric',
  'letter_grade',
  'pass_fail',
  'competency',
  'rubric',
  'narrative',
  'completion',
] as const;

export type ResultStatus = 'provisional' | 'approved' | 'published';

export const RESULT_STATUSES: readonly ResultStatus[] = [
  'provisional',
  'approved',
  'published',
] as const;

export type ReportType = 'mid_term' | 'end_of_term';

export const REPORT_TYPES: readonly ReportType[] = [
  'mid_term',
  'end_of_term',
] as const;

export type ReportStatus = 'draft' | 'approved' | 'published';

export const REPORT_STATUSES: readonly ReportStatus[] = [
  'draft',
  'approved',
  'published',
] as const;

export type TranscriptType = 'official' | 'unofficial';

export const TRANSCRIPT_TYPES: readonly TranscriptType[] = [
  'official',
  'unofficial',
] as const;

export type TranscriptStatus = 'draft' | 'approved' | 'issued';

export const TRANSCRIPT_STATUSES: readonly TranscriptStatus[] = [
  'draft',
  'approved',
  'issued',
] as const;

// ============================================================================
// Core Entities
// ============================================================================

/**
 * Assessment evaluation event tied to a learning unit (P2-S04, P2-S05).
 */
export interface Assessment {
  id: string;
  learningUnitId: string;
  learningUnitTitle?: string;
  learningUnitCode?: string;
  learningActivityId?: string | null;
  learningActivityTitle?: string | null;
  title: string;
  assessmentType: AssessmentType;
  assessmentMethod: AssessmentMethod;
  maxScore?: number | null;
  passingScore?: number | null;
  reassessmentPolicy: ReassessmentPolicy;
  criteria?: string | null;
  assessmentDate?: string | null;
  assessorReference?: string | null;
  status: AssessmentStatus;
  notes?: string | null;
  evidenceCount: number;
  resultCount: number;
  participantCount?: number;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Raw collected learning evidence for student or applicant (ACD-WF-08, UC-ACD-07, P3-S06).
 */
export interface AssessmentEvidence {
  id: string;
  assessmentId: string;
  studentId?: string | null;
  applicantId?: string | null;
  submissionId?: string | null;
  evidenceType: EvidenceType;
  collectedDate: string;
  evidenceReference?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Evaluated academic result with scoring and multi-attempt lineage (P4-S08, P4-S10, TD-03, TD-04).
 */
export interface AcademicResult {
  id: string;
  assessmentId: string;
  studentId?: string | null;
  applicantId?: string | null;
  studentNumber?: string | null;
  candidateName?: string | null;
  attemptNumber: number;
  replacesResultId?: string | null;
  isFinal: boolean;
  scoreNumeric?: number | null;
  scoreMax?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  narrativeFeedback?: string | null;
  evaluationModel: EvaluationModel;
  evaluatedBy: string;
  evaluatedDate: string;
  resultStatus: ResultStatus;
  approvedBy?: string | null;
  approvedDate?: string | null;
  publishedDate?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Published student result for self-service portal (SCR-SELF-03, P4-S10).
 */
export interface StudentPublishedResult {
  resultId: string;
  assessmentId: string;
  assessmentTitle: string;
  assessmentType: AssessmentType;
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitTitle: string;
  attemptNumber: number;
  scoreNumeric?: number | null;
  scoreMax?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  narrativeFeedback?: string | null;
  evaluationModel: EvaluationModel;
  evaluatedDate: string;
  publishedDate?: string | null;
}

/**
 * Report card subject-level entry (UC-ACD-13, P5-S11, P5-S12).
 */
export interface ReportCardEntry {
  id: string;
  reportCardId: string;
  learningUnitId: string;
  subjectCode?: string | null;
  subjectTitle?: string | null;
  academicResultId?: string | null;
  scoreNumeric?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  displayOrder: number;
  teacherComment?: string | null;
}

/**
 * Report card attendance summary tallies (P5-S11, TD-05).
 */
export interface ReportCardAttendanceSummary {
  presentDays: number;
  absentDays: number;
  lateDays: number;
  excusedDays: number;
}

/**
 * Subject entry inside frozen JSON report card snapshot (GAP-006, TD-05).
 */
export interface ReportCardSubjectEntrySnapshot {
  learningUnitId: string;
  subjectCode: string;
  subjectTitle: string;
  scoreNumeric?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  teacherComment?: string | null;
  displayOrder: number;
}

/**
 * Frozen structured JSON snapshot contract for certified report card (GAP-006, TD-05).
 */
export interface ReportCardSnapshot {
  reportCardId: string;
  studentId: string;
  studentName: string;
  studentNisn?: string | null;
  academicPeriodId: string;
  periodLabel: string;
  reportType: ReportType;
  templateName: string;
  status: ReportStatus;
  approvedBy?: string | null;
  approvedDate?: string | null;
  narrativeComments?: string | null;
  subjectEntries: ReportCardSubjectEntrySnapshot[];
  attendanceSummary?: ReportCardAttendanceSummary | null;
}

/**
 * Certified periodic report card (UC-ACD-13, ACD-WF-12, P5-S11, P5-S12).
 */
export interface ReportCard {
  id: string;
  studentId: string;
  studentName?: string;
  studentNisn?: string | null;
  academicPeriodId: string;
  periodLabel?: string;
  academicPeriodLabel?: string;
  reportType: ReportType;
  templateName: string;
  status: ReportStatus;
  approvedBy?: string | null;
  approvedDate?: string | null;
  publishedDate?: string | null;
  narrativeComments?: string | null;
  contentSnapshot?: string | null;
  snapshot?: ReportCardSnapshot | null;
  entries?: ReportCardEntry[];
  attendanceSummary?: ReportCardAttendanceSummary | null;
  createdAt: string;
  createdBy: string;
}

/**
 * Completed course entry inside transcript period section (P5-S13).
 */
export interface TranscriptCourseEntry {
  learningUnitId: string;
  subjectCode: string;
  subjectTitle: string;
  credits: number;
  scoreNumeric?: number | null;
  gradeLetter?: string | null;
  isTransferred: boolean;
}

/**
 * Completed academic period section within a transcript (P5-S13).
 */
export interface TranscriptPeriodSummary {
  academicPeriodId: string;
  periodLabel: string;
  courses: TranscriptCourseEntry[];
  gpa?: number | null;
}

/**
 * Frozen structured JSON snapshot contract for certified transcript (UC-ACD-13, TD-05).
 */
export interface TranscriptSnapshot {
  transcriptId: string;
  studentId: string;
  studentName: string;
  studentNisn?: string | null;
  transcriptType: TranscriptType;
  periodStart: string;
  periodEnd: string;
  status: TranscriptStatus;
  purpose?: string | null;
  completedPeriods: TranscriptPeriodSummary[];
}

/**
 * Academic transcript record (UC-ACD-13, P5-S13).
 */
export interface Transcript {
  id: string;
  studentId: string;
  studentName?: string;
  studentNisn?: string | null;
  transcriptType: TranscriptType;
  periodStart: string;
  periodEnd: string;
  status: TranscriptStatus;
  purpose?: string | null;
  approvedBy?: string | null;
  approvedDate?: string | null;
  issuedDate?: string | null;
  contentSnapshot?: string | null;
  snapshot?: TranscriptSnapshot | null;
  completedPeriods?: TranscriptPeriodSummary[];
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}

/**
 * Auditable immutable academic record correction log entry (UC-ACD-19, TD-06, P6-S14).
 */
export interface AcademicRecordCorrection {
  id: string;
  recordType: string;
  recordId: string;
  originalValue: string;
  correctedValue: string;
  reason: string;
  evidence?: string | null;
  correctedBy: string;
  correctedDate: string;
  isMaterial: boolean;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
}

// ============================================================================
// Request / Response Payloads
// ============================================================================

export interface CreateAssessmentRequest {
  learningUnitId: string;
  learningActivityId?: string | null;
  title: string;
  assessmentType: AssessmentType;
  assessmentMethod: AssessmentMethod;
  maxScore?: number | null;
  passingScore?: number | null;
  reassessmentPolicy?: ReassessmentPolicy;
  criteria?: string | null;
  assessmentDate?: string | null;
  assessorReference?: string | null;
  notes?: string | null;
}

export interface CreateAssessmentResponse {
  id: string;
  learningUnitId: string;
  title: string;
  status: AssessmentStatus;
}

export interface UpdateAssessmentRequest {
  title: string;
  criteria?: string | null;
  maxScore?: number | null;
  passingScore?: number | null;
  reassessmentPolicy?: ReassessmentPolicy;
  assessmentDate?: string | null;
  assessorReference?: string | null;
  notes?: string | null;
}

export interface UpdateAssessmentResponse {
  id: string;
  title: string;
  status: AssessmentStatus;
}

export interface ConductAssessmentRequest {
  assessmentDate?: string | null;
  notes?: string | null;
}

export interface ConductAssessmentResponse {
  id: string;
  status: AssessmentStatus;
  conductedDate?: string | null;
}

export interface CancelAssessmentRequest {
  reason: string;
}

export interface CancelAssessmentResponse {
  id: string;
  status: AssessmentStatus;
}

export interface GetAssessmentsFilter {
  status?: AssessmentStatus;
  assessmentType?: AssessmentType;
  fromDate?: string;
  toDate?: string;
}

export interface RecordEvidenceRequest {
  studentId?: string | null;
  applicantId?: string | null;
  submissionId?: string | null;
  evidenceType: EvidenceType;
  collectedDate: string;
  evidenceReference?: string | null;
  notes?: string | null;
}

export interface EvaluateLearningRequest {
  studentId?: string | null;
  applicantId?: string | null;
  scoreNumeric?: number | null;
  scoreMax?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  narrativeFeedback?: string | null;
  evaluationModel: EvaluationModel;
  publishImmediately?: boolean;
  notes?: string | null;
}

export interface ApproveSummativeResultsRequest {
  resultIds?: string[];
}

export interface ApproveSummativeResultsResponse {
  approvedCount: number;
}

export interface PublishAcademicResultsRequest {
  resultIds?: string[];
}

export interface PublishAcademicResultsResponse {
  publishedCount: number;
}

export interface CompileReportCardRequest {
  studentId: string;
  academicPeriodId: string;
  academicPeriodLabel: string;
  reportType: ReportType;
  templateName: string;
  narrativeComments?: string | null;
}

export interface BatchCompileReportCardsRequest {
  academicPeriodId: string;
  academicPeriodLabel: string;
  reportType: ReportType;
  templateName: string;
  sectionLabel?: string | null;
}

export interface BatchCompilationSummary {
  compiledCount: number;
  skippedExistingCount: number;
}

export interface ApproveReportCardRequest {
  narrativeComments?: string | null;
}

export interface GenerateInterimTranscriptRequest {
  studentId: string;
  transcriptType: TranscriptType;
  periodStart: string;
  periodEnd: string;
  purpose?: string | null;
  notes?: string | null;
}

export interface IssueTranscriptResponse {
  id: string;
  studentId: string;
  transcriptType: TranscriptType;
  status: TranscriptStatus;
  issuedDate: string;
  approvedBy: string;
  snapshot: TranscriptSnapshot;
}

export interface CorrectResultRequest {
  score_numeric?: number | null;
  score_max?: number | null;
  grade_letter?: string | null;
  grade_label?: string | null;
  narrative_feedback?: string | null;
  reason: string;
  evidence?: string | null;
  is_material?: boolean;
  new_score_numeric?: number | null;
  new_score_max?: number | null;
  new_grade_letter?: string | null;
  new_grade_label?: string | null;
  new_narrative_feedback?: string | null;
}
