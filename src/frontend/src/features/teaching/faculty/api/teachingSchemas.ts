import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
export const PHONE_REGEX = /^[+0-9\s\-()]+$/;
export const TEACHER_NUMBER_REGEX = /^[A-Za-z0-9\-_]{3,50}$/;

export const TEACHER_STANDINGS = [
  'pending_approval',
  'active',
  'restricted',
  'suspended',
  'inactive',
] as const;

export const CHANGEABLE_STANDINGS = [
  'active',
  'restricted',
  'suspended',
  'inactive',
] as const;

export const AUTHORIZATION_STATUSES = [
  'active',
  'authorized_under_supervision',
  'restricted',
  'suspended',
  'revoked',
  'expired',
] as const;

export const GRANTABLE_AUTHORIZATION_STATUSES = [
  'active',
  'authorized_under_supervision',
] as const;

export const SUPERVISION_TYPES = [
  'co_teaching',
  'periodic_observation',
  'lesson_plan_review',
  'general_mentorship',
] as const;

export const SUPERVISION_RESOLUTION_STATUSES = [
  'completed',
  'waived',
  'escalated',
] as const;

export const CORRECTION_TABLES = [
  'teacher_profiles',
  'teaching_authorizations',
] as const;

/**
 * Validates that an optional date string is in YYYY-MM-DD format.
 */
function isValidOptionalIsoDate(val?: string | null): boolean {
  if (!val || val.trim() === '') return true;
  return ISO_DATE_REGEX.test(val.trim());
}

/**
 * Validates that end date is on or after start date.
 */
function isEndDateOnOrAfterStartDate(
  startDate?: string | null,
  endDate?: string | null,
): boolean {
  if (!startDate || !endDate || startDate.trim() === '' || endDate.trim() === '') {
    return true;
  }
  return endDate.trim() >= startDate.trim();
}

// ============================================================================
// 1. Teacher Profile Schemas
// ============================================================================

/**
 * Zod validation schema for candidate teacher intake (POST /api/v1/teaching/teachers).
 */
export const registerTeacherSchema = z
  .object({
    teacherNumber: z
      .string()
      .trim()
      .max(50, 'Teacher number cannot exceed 50 characters.')
      .regex(
        TEACHER_NUMBER_REGEX,
        'Teacher number format is invalid. Must contain 3 to 50 alphanumeric characters, hyphens, or underscores.',
      )
      .optional()
      .nullable()
      .or(z.literal('')),
    fullName: z
      .string({ required_error: 'Full name is required.' })
      .trim()
      .min(1, 'Full name is required.')
      .max(200, 'Full name cannot exceed 200 characters.'),
    displayName: z
      .string()
      .trim()
      .max(200, 'Display name cannot exceed 200 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    employeeRef: z
      .string()
      .trim()
      .max(100, 'Employee reference cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    nationalIdOrExternalRef: z
      .string()
      .trim()
      .max(100, 'National ID or external reference cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    email: z
      .string()
      .trim()
      .max(256, 'Email cannot exceed 256 characters.')
      .email('Email must be a valid email address.')
      .optional()
      .nullable()
      .or(z.literal('')),
    phone: z
      .string()
      .trim()
      .max(50, 'Phone cannot exceed 50 characters.')
      .regex(PHONE_REGEX, 'Phone number format is invalid.')
      .optional()
      .nullable()
      .or(z.literal('')),
    teachingRoleId: z
      .string({ required_error: 'Teaching role ID is required.' })
      .trim()
      .min(1, 'Teaching role ID is required.'),
    campusAffiliation: z
      .string({ required_error: 'Campus affiliation is required.' })
      .trim()
      .min(1, 'Campus affiliation is required.')
      .max(100, 'Campus affiliation cannot exceed 100 characters.'),
    department: z
      .string()
      .trim()
      .max(100, 'Department cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    effectiveFrom: z
      .string({ required_error: 'Effective from date is required.' })
      .trim()
      .min(1, 'Effective from date is required.')
      .regex(ISO_DATE_REGEX, 'Effective from date must be in YYYY-MM-DD format.'),
    effectiveTo: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Effective to date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    notes: z
      .string()
      .trim()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => isEndDateOnOrAfterStartDate(data.effectiveFrom, data.effectiveTo),
    {
      message: 'Effective to date must be on or after effective from date.',
      path: ['effectiveTo'],
    },
  );

export type RegisterTeacherFormData = z.infer<typeof registerTeacherSchema>;

/**
 * Zod validation schema for updating mutable profile attributes (PUT /api/v1/teaching/teachers/{id}).
 */
export const updateTeacherSchema = z.object({
  displayName: z
    .string()
    .trim()
    .max(200, 'Display name cannot exceed 200 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  email: z
    .string()
    .trim()
    .max(256, 'Email cannot exceed 256 characters.')
    .email('Email must be a valid email address.')
    .optional()
    .nullable()
    .or(z.literal('')),
  phone: z
    .string()
    .trim()
    .max(50, 'Phone cannot exceed 50 characters.')
    .regex(PHONE_REGEX, 'Phone number format is invalid.')
    .optional()
    .nullable()
    .or(z.literal('')),
  campusAffiliation: z
    .string()
    .trim()
    .max(100, 'Campus affiliation cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  department: z
    .string()
    .trim()
    .max(100, 'Department cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type UpdateTeacherFormData = z.infer<typeof updateTeacherSchema>;

/**
 * Zod validation schema for transitioning teacher profile standing (POST /api/v1/teaching/teachers/{id}/standing).
 */
export const changeStandingSchema = z.object({
  standing: z.enum(CHANGEABLE_STANDINGS, {
    errorMap: () => ({
      message:
        "Standing must be one of: 'active', 'restricted', 'suspended', 'inactive'.",
    }),
  }),
  standingReason: z
    .string({ required_error: 'Standing justification reason is mandatory.' })
    .trim()
    .min(1, 'Standing justification reason is mandatory.')
    .max(2000, 'Standing reason must not exceed 2000 characters.'),
});

export type ChangeStandingFormData = z.infer<typeof changeStandingSchema>;

// ============================================================================
// 2. Teaching Capabilities Schemas
// ============================================================================

/**
 * Zod validation schema for adding potential teaching capability (POST /api/v1/teaching/teachers/{id}/capabilities).
 */
export const addCapabilitySchema = z.object({
  capabilityTypeId: z
    .string({ required_error: 'Capability type ID is required.' })
    .trim()
    .min(1, 'Capability type ID is required.'),
  capabilityCode: z
    .string({ required_error: 'Capability code is required.' })
    .trim()
    .min(1, 'Capability code is required.')
    .max(100, 'Capability code must not exceed 100 characters.'),
  capabilityName: z
    .string({ required_error: 'Capability name is required.' })
    .trim()
    .min(1, 'Capability name is required.')
    .max(200, 'Capability name must not exceed 200 characters.'),
  applicableLevel: z
    .string()
    .trim()
    .max(100, 'Applicable level must not exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  deliveryMode: z
    .string()
    .trim()
    .max(50, 'Delivery mode must not exceed 50 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  languageOfInstruction: z
    .string()
    .trim()
    .max(50, 'Language of instruction must not exceed 50 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes must not exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type AddCapabilityFormData = z.infer<typeof addCapabilitySchema>;

// ============================================================================
// 3. Qualification Evidence Schemas
// ============================================================================

/**
 * Zod validation schema for submitting qualification documentary evidence (POST /api/v1/teaching/teachers/{id}/qualifications).
 */
export const submitEvidenceSchema = z
  .object({
    qualificationTypeId: z
      .string({ required_error: 'Qualification type identifier is required.' })
      .trim()
      .min(1, 'Qualification type identifier is required.'),
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(1, 'Title is required.')
      .max(200, 'Title cannot exceed 200 characters.'),
    issuingAuthority: z
      .string({ required_error: 'Issuing authority is required.' })
      .trim()
      .min(1, 'Issuing authority is required.')
      .max(200, 'Issuing authority cannot exceed 200 characters.'),
    credentialIdentifier: z
      .string()
      .trim()
      .max(100, 'Credential identifier cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    issueDate: z
      .string({ required_error: 'Issue date is required.' })
      .trim()
      .min(1, 'Issue date is required.')
      .regex(ISO_DATE_REGEX, 'Issue date must be in YYYY-MM-DD format.'),
    expiryDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Expiry date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    documentReference: z
      .string()
      .trim()
      .max(500, 'Document reference cannot exceed 500 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    restrictionsOrConditions: z
      .string()
      .trim()
      .max(1000, 'Restrictions or conditions cannot exceed 1000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => isEndDateOnOrAfterStartDate(data.issueDate, data.expiryDate),
    {
      message: 'Expiry date must be on or after the issue date.',
      path: ['expiryDate'],
    },
  );

export type SubmitEvidenceFormData = z.infer<typeof submitEvidenceSchema>;

/**
 * Zod validation schema for verifying qualification documentary evidence (POST /api/v1/teaching/qualifications/{id}/verify).
 */
export const verifyEvidenceSchema = z.object({
  verificationNotes: z
    .string()
    .trim()
    .max(4000, 'Verification notes cannot exceed 4000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type VerifyEvidenceFormData = z.infer<typeof verifyEvidenceSchema>;

/**
 * Zod validation schema for rejecting qualification evidence (POST /api/v1/teaching/qualifications/{id}/reject).
 */
export const rejectEvidenceSchema = z.object({
  rejectionReason: z
    .string({ required_error: 'Rejection reason is mandatory.' })
    .trim()
    .min(1, 'Rejection reason is mandatory.')
    .max(4000, 'Rejection reason cannot exceed 4000 characters.'),
});

export type RejectEvidenceFormData = z.infer<typeof rejectEvidenceSchema>;

// ============================================================================
// 4. Teaching Authorization & Supervision Schemas
// ============================================================================

const supervisionPayloadSchema = z
  .object({
    supervisionType: z
      .enum(SUPERVISION_TYPES, {
        errorMap: () => ({
          message:
            "Supervision type must be one of: 'co_teaching', 'periodic_observation', 'lesson_plan_review', 'general_mentorship'.",
        }),
      })
      .or(z.string().min(1, 'Supervision type is required.')),
    description: z
      .string({ required_error: 'Supervision description is required.' })
      .trim()
      .min(1, 'Supervision description is required.')
      .max(2000, 'Supervision description cannot exceed 2000 characters.'),
    startDate: z
      .string({ required_error: 'Supervision start date is required.' })
      .trim()
      .min(1, 'Supervision start date is required.')
      .regex(ISO_DATE_REGEX, 'Supervision start date must be in YYYY-MM-DD format.'),
    endDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Supervision end date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    supervisingTeacherId: z
      .string()
      .trim()
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => isEndDateOnOrAfterStartDate(data.startDate, data.endDate),
    {
      message: 'Supervision end date must be on or after the supervision start date.',
      path: ['endDate'],
    },
  );

/**
 * Zod validation schema for granting formal teaching authorization (POST /api/v1/teaching/teachers/{id}/authorizations).
 */
export const grantAuthorizationSchema = z
  .object({
    scopeDescription: z
      .string()
      .trim()
      .max(4000, 'Authorization scope description cannot exceed 4000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    authorizationScope: z
      .string()
      .trim()
      .max(4000, 'Authorization scope cannot exceed 4000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    campusScope: z
      .string()
      .trim()
      .max(100, 'Campus scope boundary cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    campusBoundary: z
      .string()
      .trim()
      .max(100, 'Campus boundary cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    programScope: z
      .string()
      .trim()
      .max(100, 'Educational program scope cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    educationalProgramScope: z
      .string()
      .trim()
      .max(100, 'Educational program scope cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    levelScope: z
      .string()
      .trim()
      .max(100, 'Grade level scope cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    gradeLevelScope: z
      .string()
      .trim()
      .max(100, 'Grade level scope cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    deliveryModeScope: z
      .string()
      .trim()
      .max(50, 'Delivery mode scope cannot exceed 50 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    effectiveStartDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Effective start date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    effectiveFrom: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Effective start date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    effectiveEndDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Effective end date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    effectiveTo: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Effective end date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    status: z
      .enum(GRANTABLE_AUTHORIZATION_STATUSES, {
        errorMap: () => ({
          message:
            "Authorization status must be either 'active' or 'authorized_under_supervision'.",
        }),
      })
      .default('active'),
    authorizedCapabilityId: z
      .string()
      .trim()
      .optional()
      .nullable()
      .or(z.literal('')),
    conditionsOrRestrictions: z
      .string()
      .trim()
      .max(2000, 'Conditions or restrictions cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    notes: z
      .string()
      .trim()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    linkedEvidenceIds: z.array(z.string()).optional().default([]),
    supervision: supervisionPayloadSchema.optional().nullable(),
    supervisionType: z.string().optional().nullable().or(z.literal('')),
    supervisionDescription: z.string().optional().nullable().or(z.literal('')),
    supervisionStartDate: z.string().optional().nullable().or(z.literal('')),
    supervisionEndDate: z.string().optional().nullable().or(z.literal('')),
    supervisingTeacherId: z.string().optional().nullable().or(z.literal('')),
  })
  .refine(
    (data) => {
      const scope = (data.scopeDescription || data.authorizationScope || '').trim();
      return scope.length > 0;
    },
    {
      message: 'Authorization scope description is required.',
      path: ['scopeDescription'],
    },
  )
  .refine(
    (data) => {
      const start = (data.effectiveStartDate || data.effectiveFrom || '').trim();
      const end = (data.effectiveEndDate || data.effectiveTo || '').trim();
      return isEndDateOnOrAfterStartDate(start, end);
    },
    {
      message: 'Effective end date must be on or after the effective start date.',
      path: ['effectiveEndDate'],
    },
  )
  .refine(
    (data) => {
      if (data.status !== 'authorized_under_supervision') return true;

      // When under supervision, either supervision object or flat supervision fields must be present
      if (data.supervision) return true;

      const type = (data.supervisionType || '').trim();
      const desc = (data.supervisionDescription || '').trim();
      const start = (data.supervisionStartDate || '').trim();

      return type.length > 0 && desc.length > 0 && start.length > 0;
    },
    {
      message:
        "Supervision requirement details are mandatory when authorization status is 'authorized_under_supervision'.",
      path: ['supervision'],
    },
  );

export type GrantAuthorizationFormData = z.infer<typeof grantAuthorizationSchema>;

/**
 * Zod validation schema for transitioning authorization status (POST /api/v1/teaching/authorizations/{id}/status).
 */
export const updateAuthorizationStatusSchema = z.object({
  status: z.enum(AUTHORIZATION_STATUSES, {
    errorMap: () => ({
      message:
        "Status must be one of: 'active', 'authorized_under_supervision', 'restricted', 'suspended', 'revoked', 'expired'.",
    }),
  }),
  statusReason: z
    .string({ required_error: 'Status justification reason is mandatory.' })
    .trim()
    .min(1, 'Status justification reason is mandatory.')
    .max(2000, 'Status reason must not exceed 2000 characters.'),
});

export type UpdateAuthorizationStatusFormData = z.infer<
  typeof updateAuthorizationStatusSchema
>;

/**
 * Zod validation schema for attaching a supervision requirement (POST /api/v1/teaching/authorizations/{id}/supervision).
 */
export const addSupervisionSchema = supervisionPayloadSchema;

export type AddSupervisionFormData = z.infer<typeof addSupervisionSchema>;

/**
 * Zod validation schema for concluding or resolving a supervision requirement (POST /api/v1/teaching/supervision/{id}/resolve).
 */
export const resolveSupervisionSchema = z.object({
  resolutionStatus: z.enum(SUPERVISION_RESOLUTION_STATUSES, {
    errorMap: () => ({
      message:
        "Resolution status must be one of: 'completed', 'waived', 'escalated'.",
    }),
  }),
  resolutionNotes: z
    .string({ required_error: 'Resolution notes are mandatory.' })
    .trim()
    .min(1, 'Resolution notes are mandatory.')
    .max(4000, 'Resolution notes cannot exceed 4000 characters.'),
  updateAuthorizationToActive: z.boolean().optional().default(true),
});

export type ResolveSupervisionFormData = z.infer<typeof resolveSupervisionSchema>;

// ============================================================================
// 5. Auditable Record Corrections Schema
// ============================================================================

/**
 * Zod validation schema for applying or requesting an auditable record correction (POST /api/v1/teaching/corrections).
 */
export const recordCorrectionSchema = z
  .object({
    targetTableName: z
      .string({ required_error: 'Target table name is required.' })
      .trim()
      .min(1, 'Target table name is required.')
      .refine(
        (tbl) => {
          const lower = tbl.toLowerCase();
          return (
            lower === 'teacher_profiles' ||
            lower === 'teaching_authorizations' ||
            lower === 'teaching.teacher_profiles' ||
            lower === 'teaching.teaching_authorizations'
          );
        },
        {
          message:
            "Target table must be 'teacher_profiles' or 'teaching_authorizations'.",
        },
      ),
    targetRecordId: z
      .string({ required_error: 'Target record ID is required.' })
      .trim()
      .min(1, 'Target record ID is required.'),
    fieldName: z
      .string({ required_error: 'Field name is required.' })
      .trim()
      .min(1, 'Field name is required.')
      .max(100, 'Field name cannot exceed 100 characters.'),
    originalValue: z
      .string()
      .optional()
      .nullable()
      .or(z.literal('')),
    correctedValue: z
      .string({ required_error: 'Corrected value is required.' })
      .trim()
      .min(1, 'Corrected value is required.'),
    reasonForCorrection: z
      .string({ required_error: 'Reason for correction is mandatory.' })
      .trim()
      .min(1, 'Reason for correction is mandatory.')
      .max(2000, 'Reason for correction must not exceed 2000 characters.'),
    supportingEvidence: z
      .string()
      .trim()
      .max(500, 'Supporting evidence cannot exceed 500 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    downstreamImpactReviewed: z.boolean().optional().default(true),
    effectiveDateImplications: z
      .string()
      .trim()
      .max(1000, 'Effective date implications cannot exceed 1000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    teacherProfileId: z
      .string()
      .trim()
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      // Must represent a non-empty delta when originalValue is provided
      if (typeof data.originalValue === 'string' && data.originalValue.trim().length > 0) {
        return data.originalValue.trim() !== data.correctedValue.trim();
      }
      return true;
    },
    {
      message: 'Corrected value must differ from the original value.',
      path: ['correctedValue'],
    },
  );

export type RecordCorrectionFormData = z.infer<typeof recordCorrectionSchema>;
