/**
 * Strongly-typed domain models and DTO interfaces for the Teaching domain (Stage 2).
 * Strictly mirrors Elsasa.Sis.Contracts.Teaching DTO records and Minimal API payloads.
 */

import type { PaginatedResponse } from '@/shared/types/common';

/**
 * Teacher standing states matching CK_teaching_profiles_standing (TD-03).
 */
export type TeacherStanding =
  | 'pending_approval'
  | 'active'
  | 'restricted'
  | 'suspended'
  | 'inactive';

/**
 * Verification lifecycle states for qualification documentary evidence (TD-05).
 */
export type VerificationState =
  | 'claimed'
  | 'submitted'
  | 'verified'
  | 'rejected'
  | 'expired'
  | 'revoked';

/**
 * Authorization lifecycle statuses matching CK_teaching_auth_status (TD-05).
 */
export type AuthorizationStatus =
  | 'active'
  | 'authorized_under_supervision'
  | 'restricted'
  | 'suspended'
  | 'revoked'
  | 'expired';

/**
 * Mentoring supervision types matching CK_teaching_sup_type (TD-05).
 */
export type SupervisionType =
  | 'co_teaching'
  | 'periodic_observation'
  | 'lesson_plan_review'
  | 'general_mentorship';

/**
 * Supervision lifecycle statuses matching CK_teaching_sup_status (TD-05).
 */
export type SupervisionStatus =
  | 'active'
  | 'completed'
  | 'waived'
  | 'escalated';

/**
 * Auditable record correction statuses matching CK_teaching_trc_status (TD-06).
 */
export type CorrectionStatus =
  | 'requested'
  | 'approved'
  | 'rejected'
  | 'applied';

/**
 * Unified faculty work queue filter categories (SCR-TCH-01).
 */
export type FacultyQueueFilter =
  | 'all'
  | 'pending_approval'
  | 'pending_verification'
  | 'pending_authorization'
  | 'corrections';

/**
 * Query parameters for filtering and paginating the faculty work queue.
 */
export interface FacultyQueryParams {
  queueFilter?: FacultyQueueFilter | string;
  standing?: TeacherStanding | string;
  campus?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

// ============================================================================
// Core Contract DTOs (Elsasa.Sis.Contracts.Teaching)
// ============================================================================

/**
 * Strongly-typed contract DTO representing a recognized teaching role.
 */
export interface TeachingRoleDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  requiresQualificationCheck: boolean;
  isActive: boolean;
}

/**
 * Taxonomy capability type DTO.
 */
export interface CapabilityTypeDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

/**
 * Qualification type category and renewal policy DTO.
 */
export interface QualificationTypeResponse {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  requiresRenewal: boolean;
  defaultValidityMonths?: number | null;
  isActive: boolean;
}

/**
 * Strongly-typed contract DTO representing a potential teaching capability.
 */
export interface TeachingCapabilityDto {
  id: string;
  teacherProfileId: string;
  capabilityTypeId: string;
  capabilityCode: string;
  capabilityName: string;
  applicableLevel?: string | null;
  deliveryMode?: string | null;
  languageOfInstruction?: string | null;
  status: 'active' | 'inactive' | string;
  notes?: string | null;
}

/**
 * Strongly-typed contract DTO representing qualification documentary evidence.
 */
export interface QualificationEvidenceDto {
  id: string;
  teacherProfileId: string;
  qualificationTypeId: string;
  title: string;
  issuingAuthority: string;
  credentialIdentifier?: string | null;
  issueDate: string; // ISO-8601 YYYY-MM-DD
  expiryDate?: string | null;
  documentReference?: string | null;
  verificationState: VerificationState;
  verifiedBy?: string | null;
  verifiedDate?: string | null;
  verificationNotes?: string | null;
  rejectionReason?: string | null;
  restrictionsOrConditions?: string | null;
}

/**
 * Strongly-typed contract DTO representing a mentoring or supervision requirement.
 */
export interface SupervisionRequirementDto {
  id: string;
  authorizationId: string;
  supervisingTeacherId?: string | null;
  supervisingTeacherName?: string | null;
  supervisionType: SupervisionType | string;
  description: string;
  startDate: string; // ISO-8601 YYYY-MM-DD
  endDate?: string | null;
  status: SupervisionStatus | string;
  resolutionNotes?: string | null;
}

/**
 * Strongly-typed contract DTO representing an authoritative school permission to teach.
 */
export interface TeachingAuthorizationDto {
  id: string;
  authorizationNumber: string;
  teacherProfileId: string;
  authorizedCapabilityId?: string | null;
  scopeDescription: string;
  campusScope: string;
  programScope?: string | null;
  levelScope?: string | null;
  deliveryModeScope?: string | null;
  status: AuthorizationStatus;
  unverifiedCredentialWarning: boolean;
  effectiveStartDate: string; // ISO-8601 YYYY-MM-DD
  effectiveEndDate?: string | null;
  conditionsOrRestrictions?: string | null;
  decisionAuthority: string;
  approvedDate: string; // ISO-8601 YYYY-MM-DD
  suspensionOrRevocationReason?: string | null;
  notes?: string | null;
  linkedEvidenceIds?: string[] | null;
  supervisionRequirements?: SupervisionRequirementDto[] | null;
}

/**
 * Strongly-typed contract DTO representing an auditable record correction.
 */
export interface TeachingRecordCorrectionDto {
  id: string;
  correctionNumber: string;
  targetTableName: 'teacher_profiles' | 'teaching_authorizations' | string;
  targetRecordId: string;
  teacherProfileId?: string | null;
  fieldName: string;
  originalValue: string;
  correctedValue: string;
  reasonForCorrection: string;
  supportingEvidence?: string | null;
  downstreamImpactReviewed: boolean;
  effectiveDateImplications?: string | null;
  requestedBy: string;
  requestedDate: string; // ISO-8601 YYYY-MM-DD
  approvedBy?: string | null;
  approvedDate?: string | null;
  status: CorrectionStatus;
}

/**
 * Concise summary of a teacher profile for directories, work queues, and badges.
 */
export interface TeacherSummaryDto {
  id: string;
  teacherNumber: string;
  fullName: string;
  displayName?: string | null;
  teachingRoleId: string;
  teachingRoleName: string;
  campusAffiliation: string;
  department?: string | null;
  standing: TeacherStanding;
  email?: string | null;
  phone?: string | null;
  hasUnverifiedWarning: boolean;
  activeAuthorizationsCount: number;
}

/**
 * Complete authoritative faculty dossier of a teacher resource.
 */
export interface TeacherProfileDto {
  id: string;
  teacherNumber: string;
  fullName: string;
  displayName?: string | null;
  employeeRef?: string | null;
  nationalIdOrExternalRef?: string | null;
  email?: string | null;
  phone?: string | null;
  teachingRoleId: string;
  teachingRoleName: string;
  campusAffiliation: string;
  department?: string | null;
  standing: TeacherStanding;
  effectiveFrom: string; // ISO-8601 YYYY-MM-DD
  effectiveTo?: string | null;
  standingReason?: string | null;
  approvedBy?: string | null;
  approvedDate?: string | null;
  notes?: string | null;
  capabilities?: TeachingCapabilityDto[] | null;
  qualifications?: QualificationEvidenceDto[] | null;
  authorizations?: TeachingAuthorizationDto[] | null;
  recentCorrections?: TeachingRecordCorrectionDto[] | null;
}

// ============================================================================
// Request & Response Command Payloads
// ============================================================================

export interface RegisterTeacherProfileRequest {
  teacherNumber?: string | null;
  fullName: string;
  displayName?: string | null;
  employeeRef?: string | null;
  nationalIdOrExternalRef?: string | null;
  email?: string | null;
  phone?: string | null;
  teachingRoleId: string;
  campusAffiliation: string;
  department?: string | null;
  effectiveFrom: string; // ISO-8601 YYYY-MM-DD
  effectiveTo?: string | null;
  notes?: string | null;
}

export interface RegisterTeacherProfileResponse {
  id: string;
  teacherNumber: string;
  fullName: string;
  standing: string;
}

export interface UpdateTeacherProfileRequest {
  displayName?: string | null;
  email?: string | null;
  phone?: string | null;
  campusAffiliation?: string | null;
  department?: string | null;
  notes?: string | null;
}

export interface ApproveTeacherProfileResponse {
  id: string;
  teacherNumber: string;
  standing: string;
  approvedBy: string;
  approvedDate: string;
}

export interface ChangeTeacherStandingRequest {
  standing: TeacherStanding | string;
  standingReason: string;
}

export interface ChangeTeacherStandingResponse {
  id: string;
  previousStanding: string;
  standing: string;
  standingReason: string;
  updatedBy: string;
  updatedAt: string;
}

export interface AddTeachingCapabilityRequest {
  capabilityTypeId: string;
  capabilityCode: string;
  capabilityName: string;
  applicableLevel?: string | null;
  deliveryMode?: string | null;
  languageOfInstruction?: string | null;
  notes?: string | null;
}

export interface AddTeachingCapabilityResponse {
  id: string;
}

export interface SubmitQualificationEvidenceRequest {
  qualificationTypeId: string;
  title: string;
  issuingAuthority: string;
  credentialIdentifier?: string | null;
  issueDate: string; // ISO-8601 YYYY-MM-DD
  expiryDate?: string | null;
  documentReference?: string | null;
  restrictionsOrConditions?: string | null;
}

export interface SubmitQualificationEvidenceResponse {
  id: string;
  teacherProfileId: string;
  qualificationTypeId: string;
  title: string;
  verificationState: string;
}

export interface VerifyQualificationEvidenceRequest {
  verificationNotes?: string | null;
}

export interface VerifyQualificationEvidenceResponse {
  id: string;
  teacherProfileId: string;
  title: string;
  verificationState: string;
  verifiedBy: string;
  verifiedDate: string;
  verificationNotes?: string | null;
}

export interface RejectQualificationEvidenceRequest {
  rejectionReason: string;
}

export interface RejectQualificationEvidenceResponse {
  id: string;
  teacherProfileId: string;
  title: string;
  verificationState: string;
  verifiedBy: string;
  verifiedDate: string;
  rejectionReason: string;
}

export interface GrantSupervisionPayload {
  supervisionType: SupervisionType | string;
  description: string;
  startDate: string; // ISO-8601 YYYY-MM-DD
  endDate?: string | null;
  supervisingTeacherId?: string | null;
}

export interface GrantTeachingAuthorizationRequest {
  scopeDescription?: string | null;
  authorizationScope?: string | null;
  campusScope?: string | null;
  campusBoundary?: string | null;
  programScope?: string | null;
  educationalProgramScope?: string | null;
  levelScope?: string | null;
  gradeLevelScope?: string | null;
  deliveryModeScope?: string | null;
  effectiveStartDate?: string | null;
  effectiveFrom?: string | null;
  effectiveEndDate?: string | null;
  effectiveTo?: string | null;
  status?: AuthorizationStatus | string | null;
  authorizationNumber?: string | null;
  authorizedCapabilityId?: string | null;
  conditionsOrRestrictions?: string | null;
  notes?: string | null;
  linkedEvidenceIds?: string[] | null;
  supervision?: GrantSupervisionPayload | null;
  supervisionType?: string | null;
  supervisionDescription?: string | null;
  supervisionStartDate?: string | null;
  supervisionEndDate?: string | null;
  supervisingTeacherId?: string | null;
  mentorTeacherId?: string | null;
  targetResolutionDate?: string | null;
}

export interface GrantTeachingAuthorizationResponse {
  id: string;
  authorizationNumber: string;
  teacherProfileId: string;
  authorizedCapabilityId?: string | null;
  scopeDescription: string;
  campusScope: string;
  programScope?: string | null;
  levelScope?: string | null;
  deliveryModeScope?: string | null;
  status: string;
  unverifiedCredentialWarning: boolean;
  effectiveStartDate: string;
  effectiveEndDate?: string | null;
  decisionAuthority: string;
  approvedDate: string;
  linkedEvidenceIds: string[];
  supervisionRequirementId?: string | null;
}

export interface UpdateTeachingAuthorizationStatusRequest {
  status: AuthorizationStatus | string;
  statusReason: string;
  reason?: string | null;
  suspensionOrRevocationReason?: string | null;
}

export interface UpdateTeachingAuthorizationStatusResponse {
  id: string;
  teacherProfileId: string;
  authorizationNumber: string;
  previousStatus: string;
  status: string;
  statusReason: string;
  updatedBy: string;
  updatedAt: string;
}

export interface AddSupervisionRequirementRequest {
  supervisionType: SupervisionType | string;
  description: string;
  startDate: string; // ISO-8601 YYYY-MM-DD
  endDate?: string | null;
  supervisingTeacherId?: string | null;
}

export interface AddSupervisionRequirementResponse {
  id: string;
  authorizationId: string;
  supervisingTeacherId?: string | null;
  supervisionType: string;
  description: string;
  startDate: string;
  endDate?: string | null;
  status: string;
  createdAt: string;
}

export interface ResolveSupervisionRequirementRequest {
  resolutionStatus: 'completed' | 'waived' | 'escalated' | string;
  resolutionNotes: string;
  updateAuthorizationToActive?: boolean | null;
  status?: string | null;
}

export interface ResolveSupervisionRequirementResponse {
  id: string;
  authorizationId: string;
  status: string;
  resolutionNotes: string;
  resolvedDate: string;
  resolvedBy: string;
  authorizationStatus: string;
  authorizationUpdatedToActive: boolean;
}

export interface ApplyRecordCorrectionRequest {
  targetTableName: 'teacher_profiles' | 'teaching_authorizations' | string;
  targetRecordId: string;
  fieldName: string;
  correctedValue: string;
  reasonForCorrection: string;
  originalValue?: string | null;
  supportingEvidence?: string | null;
  downstreamImpactReviewed?: boolean | null;
  effectiveDateImplications?: string | null;
  teacherProfileId?: string | null;
}

export interface GetRecordCorrectionsParams {
  teacherProfileId?: string;
  targetTableName?: string;
  status?: CorrectionStatus | string;
}

// ============================================================================
// Semantic Aliases for Ergonomics & Plan Criteria Parity
// ============================================================================

export type TeacherProfile = TeacherProfileDto;
export type TeacherSummary = TeacherSummaryDto;
export type TeachingRole = TeachingRoleDto;
export type TeachingCapability = TeachingCapabilityDto;
export type CapabilityType = CapabilityTypeDto;
export type QualificationEvidence = QualificationEvidenceDto;
export type QualificationType = QualificationTypeResponse;
export type TeachingAuthorization = TeachingAuthorizationDto;
export type SupervisionRequirement = SupervisionRequirementDto;
export type TeachingRecordCorrection = TeachingRecordCorrectionDto;

export type RegisterTeacherRequest = RegisterTeacherProfileRequest;
export type UpdateTeacherRequest = UpdateTeacherProfileRequest;
