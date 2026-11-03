/**
 * Authoritative TypeScript interfaces and types for the Students Admission module.
 * Reflects backend vertical slice DTOs and database schema (ADR-004, ADR-005).
 */

export type ApplicationStatus = 'registered' | 'under_review' | 'decided' | 'withdrawn';

export type DecisionType =
  | 'accepted'
  | 'rejected'
  | 'waitlisted'
  | 'withdrawn'
  | 'expired'
  | 'deferred';

export type DocumentSubmissionStatus =
  | 'not_submitted'
  | 'submitted'
  | 'verified'
  | 'rejected';

/**
 * Projection item for the admission queue worklist (SCR-STU-01).
 */
export interface AdmissionApplicationListItemDto {
  id: string;
  applicationNumber: string;
  applicantName: string;
  governmentIdNumber?: string | null;
  admissionPeriodId: string;
  requestedEntryLevel: string;
  status: ApplicationStatus | string;
  registrationDate: string;
}

/**
 * Biographical information of an applicant (students.applicants).
 */
export interface ApplicantBioDto {
  id: string;
  fullName: string;
  preferredName?: string | null;
  dateOfBirth?: string | null;
  placeOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  governmentIdType?: string | null;
  governmentIdNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  previousSchoolName?: string | null;
  previousSchoolAddress?: string | null;
  notes?: string | null;
}

/**
 * Guardian or contact person attached to an application (students.admission_application_contacts).
 */
export interface GuardianContactDto {
  id: string;
  applicationId: string;
  fullName: string;
  relationship: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  isPrimary: boolean;
  createdAt?: string;
  createdBy?: string | null;
}

/**
 * Uploaded credential document metadata (students.applicant_documents).
 */
export interface ApplicantDocumentDto {
  id: string;
  applicationId: string;
  documentType: string;
  submissionStatus: DocumentSubmissionStatus | string;
  submittedDate?: string | null;
  verifiedDate?: string | null;
  verifiedBy?: string | null;
  fileReference?: string | null;
  fileName?: string | null;
  fileSizeBytes?: number | null;
  contentType?: string | null;
  notes?: string | null;
  createdAt?: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Manual external entrance assessment diagnostic evidence (students.entrance_assessment_evidence).
 */
export interface AssessmentEvidenceDto {
  id: string;
  applicationId: string;
  assessmentType: string;
  score?: string | null;
  source: string;
  assessmentDate?: string | null;
  notes?: string | null;
  createdAt?: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Official admission decision record (students.admission_decisions).
 */
export interface AdmissionDecisionDto {
  id: string;
  applicationId: string;
  decisionType: DecisionType | string;
  decisionDate: string;
  effectiveDate: string;
  decidedBy: string;
  reason?: string | null;
  supportingReference?: string | null;
  deferredAdmissionPeriodId?: string | null;
  createdAt?: string;
  createdBy?: string | null;
}

/**
 * Complete admission aggregate dossier projection (SCR-STU-01).
 */
export interface AdmissionApplicationDetailDto {
  id: string;
  applicationNumber: string;
  applicantId: string;
  admissionPeriodId: string;
  academicYearId: string;
  applicationType: string;
  requestedEntryLevel: string;
  requestedCampus?: string | null;
  requestedProgram?: string | null;
  registrationDate: string;
  status: ApplicationStatus | string;
  submittedAt?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
  applicant: ApplicantBioDto;
  contacts: GuardianContactDto[];
  documents: ApplicantDocumentDto[];
  assessmentEvidence: AssessmentEvidenceDto[];
  decision?: AdmissionDecisionDto | null;
}

/**
 * Document status summary item for public self-service status tracker.
 */
export interface PublicDocumentStatusDto {
  documentType: string;
  submissionStatus: DocumentSubmissionStatus | string;
}

/**
 * Official decision notice item for public self-service status tracker.
 */
export interface PublicDecisionNoticeDto {
  decisionType: DecisionType | string;
  effectiveDate: string;
}

/**
 * Sanitized public status projection for self-service applicant tracking (SCR-SELF-01).
 */
export interface PublicApplicationStatusDto {
  applicationNumber: string;
  applicantFullName: string;
  requestedEntryLevel: string;
  status: ApplicationStatus | string;
  registrationDate: string;
  documentStatuses: PublicDocumentStatusDto[];
  decisionNotice?: PublicDecisionNoticeDto | null;
}

// ---------------------------------------------------------------------------
// Request Payloads and Command Result DTOs
// ---------------------------------------------------------------------------

export interface RegisterApplicationContactPayload {
  fullName: string;
  relationship: string;
  phone?: string | null | undefined;
  email?: string | null | undefined;
  address?: string | null | undefined;
  isPrimary?: boolean;
}

export interface RegisterApplicationPayload {
  applicantId?: string | null | undefined;
  fullName: string;
  preferredName?: string | null | undefined;
  dateOfBirth?: string | null | undefined;
  placeOfBirth?: string | null | undefined;
  gender?: string | null | undefined;
  nationality?: string | null | undefined;
  governmentIdType?: string | null | undefined;
  governmentIdNumber?: string | null | undefined;
  email?: string | null | undefined;
  phone?: string | null | undefined;
  address?: string | null | undefined;
  previousSchoolName?: string | null | undefined;
  previousSchoolAddress?: string | null | undefined;
  admissionPeriodId: string;
  applicationType?: string | null | undefined;
  requestedEntryLevel: string;
  requestedCampus?: string | null | undefined;
  requestedProgram?: string | null | undefined;
  contacts?: RegisterApplicationContactPayload[] | null | undefined;
  bypassWindowCheck?: boolean | null | undefined;
  notes?: string | null | undefined;
}

export interface RegisterApplicationResultDto {
  id: string;
  applicationNumber: string;
  applicantId: string;
  status: ApplicationStatus | string;
  registrationDate: string;
}

export interface StartApplicationReviewResultDto {
  id: string;
  status: ApplicationStatus | string;
  updatedAt?: string | null;
}

export interface RecordAdmissionDecisionPayload {
  decisionType: DecisionType;
  decisionDate: string;
  effectiveDate: string;
  reason?: string | null;
  supportingReference?: string | null;
  deferredAdmissionPeriodId?: string | null;
}

export interface RecordAdmissionDecisionResultDto {
  decisionId: string;
  applicationId: string;
  decisionType: DecisionType | string;
  effectiveDate: string;
}

export interface WithdrawApplicationPayload {
  withdrawalReason: string;
}

export interface WithdrawApplicationResultDto {
  id: string;
  status: ApplicationStatus | string;
  notes?: string | null;
  updatedAt?: string | null;
}

export interface UploadApplicantDocumentResultDto {
  id: string;
  applicationId: string;
  documentType: string;
  submissionStatus: DocumentSubmissionStatus | string;
  fileReference: string;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
  submittedDate?: string | null;
}

export interface VerifyApplicantDocumentPayload {
  submissionStatus: 'verified' | 'rejected';
  notes?: string | null;
}

export interface VerifyApplicantDocumentResultDto {
  id: string;
  applicationId: string;
  documentType: string;
  submissionStatus: DocumentSubmissionStatus | string;
  verifiedDate?: string | null;
  verifiedBy?: string | null;
  notes?: string | null;
}

export interface AddAssessmentEvidencePayload {
  assessmentType: string;
  score?: string | null;
  source?: string | null;
  assessmentDate?: string | null;
  notes?: string | null;
}

export interface AddAssessmentEvidenceResultDto {
  id: string;
  applicationId: string;
  assessmentType: string;
  score?: string | null;
  source: string;
  assessmentDate?: string | null;
  notes?: string | null;
  createdAt: string;
}

export interface UpdateAssessmentEvidencePayload {
  assessmentType: string;
  score?: string | null;
  source?: string | null;
  assessmentDate?: string | null;
  notes?: string | null;
}

export interface UpdateAssessmentEvidenceResultDto {
  id: string;
  applicationId: string;
  assessmentType: string;
  score?: string | null;
  source: string;
  assessmentDate?: string | null;
  notes?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface AdmissionQueueFilterParams {
  admissionPeriodId?: string | null;
  status?: string | null;
  requestedEntryLevel?: string | null;
  searchTerm?: string | null;
  page?: number | null;
  pageSize?: number | null;
}

/**
 * Operational period option retrieved from operations calendar for admission targeting.
 */
export interface AdmissionPeriodOptionDto {
  id: string;
  academicYearId: string;
  periodCode: string;
  name: string;
  periodType: string;
  startDate: string;
  endDate: string;
  parentPeriodId?: string | null;
  campusContext?: string;
  status: string;
}

