/**
 * Authoritative TypeScript interfaces and types for the Student Records module (ADR-004, ADR-005).
 * Reflects backend vertical slice DTOs, domain models, and SQL Server projections.
 */

export type StudentStatus = 'enrolled' | 'active' | 'on_leave' | 'withdrawn' | 'graduated';

export type GuardianRelationshipType =
  | 'mother'
  | 'father'
  | 'legal_guardian'
  | 'grandparent'
  | 'sibling'
  | 'other';

export type DocumentVerificationStatus = 'submitted' | 'verified' | 'rejected';

/**
 * Lightweight student summary card for list projections and search workbench (SCR-STU-02, P4-S05).
 */
export interface StudentListItem {
  id: string;
  studentNumber: string;
  fullName: string;
  preferredName?: string | null;
  currentStatus: string;
  enrollmentDate: string;
  initialCampus: string;
  initialLevel: string;
  isIndependent: boolean;
  primaryGuardianName?: string | null;
  primaryGuardianPhone?: string | null;
}

/**
 * Historical enrollment placement and payment audit snapshot.
 */
export interface EnrollmentHistoryItem {
  enrollmentDate: string;
  campus: string;
  level: string;
  academicYear: string;
  program?: string | null;
  cohort?: string | null;
  approvedBy: string;
  paymentConfirmed?: boolean | null;
  paymentReference?: string | null;
  paymentWaived: boolean;
  waiverReference?: string | null;
}

/**
 * Associated guardian relationship details with designated legal/operational authorities.
 */
export interface GuardianRelationshipItem {
  relationshipId: string;
  guardianId: string;
  fullName: string;
  relationshipType: string;
  governmentIdType?: string | null;
  governmentIdNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  occupation?: string | null;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  isFinancialContact: boolean;
  isAuthorizedPickup: boolean;
  isActive: boolean;
}

/**
 * Official certified student document record with metadata and verification status.
 */
export interface StudentDocumentItem {
  id: string;
  documentType: string;
  submissionStatus: string;
  submittedDate?: string | null;
  verifiedDate?: string | null;
  verifiedBy?: string | null;
  fileName?: string | null;
  fileSizeBytes?: number | null;
  contentType?: string | null;
  fileReference?: string | null;
}

/**
 * Authoritative 360-degree student dossier projection response (SCR-STU-02, P4-S06).
 */
export interface StudentDetail {
  id: string;
  studentNumber: string;
  applicantId: string;
  fullName: string;
  preferredName?: string | null;
  dateOfBirth?: string | null;
  placeOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  governmentIdType?: string | null;
  governmentIdNumber?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isIndependent: boolean;
  currentStatus: string;
  enrollmentDate: string;
  notes?: string | null;
  createdAt: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
  enrollmentHistory: EnrollmentHistoryItem[];
  guardianRelationships: GuardianRelationshipItem[];
  documents: StudentDocumentItem[];
}

/**
 * Query search filter parameters for GET /api/students.
 */
export interface StudentSearchParams {
  searchTerm?: string;
  campus?: string;
  level?: string;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
}

/**
 * Request payload for updating certified student identity information (PUT /api/students/{id}/identity).
 */
export interface UpdateIdentityPayload {
  fullName: string;
  preferredName?: string | null;
  dateOfBirth?: string | null;
  placeOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  governmentIdType?: string | null;
  governmentIdNumber?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isIndependent?: boolean;
  notes?: string | null;
}

/**
 * Request payload to link a new guardian relationship (POST /api/students/{id}/guardians).
 */
export interface AddGuardianPayload {
  fullName: string;
  relationshipType: string;
  governmentIdType?: string | null;
  governmentIdNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  occupation?: string | null;
  notes?: string | null;
  guardianId?: string | null;
  isPrimaryContact?: boolean;
  isEmergencyContact?: boolean;
  isFinancialContact?: boolean;
  isAuthorizedPickup?: boolean;
}

/**
 * Response payload returned when adding a guardian relationship.
 */
export interface AddGuardianResponse {
  relationshipId: string;
  guardianId: string;
  id?: string;
}

/**
 * Request payload to update an existing guardian relationship (PUT /api/students/{id}/guardians/{relationshipId}).
 */
export interface UpdateGuardianPayload {
  relationshipType: string;
  isPrimaryContact?: boolean;
  isEmergencyContact?: boolean;
  isFinancialContact?: boolean;
  isAuthorizedPickup?: boolean;
  isActive?: boolean;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  occupation?: string | null;
  notes?: string | null;
}

/**
 * Response payload returned when updating a guardian relationship.
 */
export interface UpdateGuardianResponse {
  relationshipId: string;
  guardianId: string;
  isPrimaryContact: boolean;
  isActive: boolean;
}

/**
 * Request payload for verifying or rejecting a student document.
 */
export interface VerifyDocumentPayload {
  submissionStatus: string;
  notes?: string | null;
}

/**
 * Response payload returned when uploading a student document.
 */
export interface UploadStudentDocumentResponse {
  id: string;
  studentId: string;
  documentType: string;
  submissionStatus: string;
  fileReference?: string | null;
  fileName?: string | null;
  fileSizeBytes?: number | null;
  contentType?: string | null;
  submittedDate?: string | null;
  documentId?: string;
}

/**
 * Response payload returned when verifying a student document.
 */
export interface VerifyStudentDocumentResponse {
  id: string;
  studentId: string;
  documentType: string;
  submissionStatus: string;
  verifiedBy?: string | null;
  verifiedDate?: string | null;
  notes?: string | null;
}
