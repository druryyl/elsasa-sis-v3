import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
export const PHONE_REGEX = /^[+0-9\s\-()]+$/;

export const GUARDIAN_RELATIONSHIP_TYPES = [
  'mother',
  'father',
  'legal_guardian',
  'grandparent',
  'sibling',
  'other',
] as const;

export const STUDENT_STATUSES = [
  'enrolled',
  'active',
  'on_leave',
  'withdrawn',
  'graduated',
] as const;

export const DOCUMENT_VERIFICATION_STATUSES = ['verified', 'rejected'] as const;

/**
 * Validates whether an ISO date string (YYYY-MM-DD) is not in the future.
 */
function isDateNotInFuture(dateStr: string): boolean {
  if (!dateStr || !ISO_DATE_REGEX.test(dateStr)) return true;
  const parsed = new Date(dateStr);
  if (isNaN(parsed.getTime())) return false;
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  return parsed <= today;
}

/**
 * Zod schema for updating certified student identity information (PUT /api/students/{id}/identity).
 */
export const updateIdentitySchema = z.object({
  fullName: z
    .string({ required_error: 'Full legal name is required.' })
    .trim()
    .min(1, 'Full legal name is required.')
    .max(200, 'Full name cannot exceed 200 characters.'),
  preferredName: z
    .string()
    .trim()
    .max(100, 'Preferred name cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  dateOfBirth: z
    .string()
    .trim()
    .regex(ISO_DATE_REGEX, 'Date of birth must be in YYYY-MM-DD format.')
    .refine(isDateNotInFuture, 'Date of birth cannot be in the future.')
    .optional()
    .nullable()
    .or(z.literal('')),
  placeOfBirth: z
    .string()
    .trim()
    .max(100, 'Place of birth cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  gender: z
    .string()
    .trim()
    .max(20, 'Gender cannot exceed 20 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  nationality: z
    .string()
    .trim()
    .max(100, 'Nationality cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  governmentIdType: z
    .string()
    .trim()
    .max(32, 'Government ID type cannot exceed 32 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  governmentIdNumber: z
    .string()
    .trim()
    .max(64, 'Government ID number cannot exceed 64 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  address: z
    .string()
    .trim()
    .max(1000, 'Address cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  phone: z
    .string()
    .trim()
    .max(32, 'Phone cannot exceed 32 characters.')
    .regex(PHONE_REGEX, 'Phone number format is invalid.')
    .optional()
    .nullable()
    .or(z.literal('')),
  email: z
    .string()
    .trim()
    .max(254, 'Email cannot exceed 254 characters.')
    .email('Email must be a valid email address.')
    .optional()
    .nullable()
    .or(z.literal('')),
  isIndependent: z.boolean().default(false),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

/**
 * Alias for updateIdentitySchema.
 */
export const identitySchema = updateIdentitySchema;

/**
 * Zod schema for linking a new guardian relationship to a student (POST /api/students/{id}/guardians).
 */
export const addGuardianSchema = z.object({
  fullName: z
    .string({ required_error: 'Guardian full name is required.' })
    .trim()
    .min(1, 'Guardian full name is required.')
    .max(200, 'Guardian full name cannot exceed 200 characters.'),
  relationshipType: z
    .string({ required_error: 'Relationship type is required.' })
    .trim()
    .min(1, 'Relationship type is required.')
    .max(50, 'Relationship type cannot exceed 50 characters.')
    .refine(
      (val) => GUARDIAN_RELATIONSHIP_TYPES.includes(val as (typeof GUARDIAN_RELATIONSHIP_TYPES)[number]),
      {
        message:
          'Relationship type must be one of: mother, father, legal_guardian, grandparent, sibling, other.',
      },
    ),
  governmentIdType: z
    .string()
    .trim()
    .max(32, 'Government ID type cannot exceed 32 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  governmentIdNumber: z
    .string()
    .trim()
    .max(64, 'Government ID number cannot exceed 64 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  phone: z
    .string()
    .trim()
    .max(32, 'Guardian phone cannot exceed 32 characters.')
    .regex(PHONE_REGEX, 'Guardian phone number format is invalid.')
    .optional()
    .nullable()
    .or(z.literal('')),
  email: z
    .string()
    .trim()
    .max(254, 'Guardian email cannot exceed 254 characters.')
    .email('Guardian email must be a valid email address.')
    .optional()
    .nullable()
    .or(z.literal('')),
  address: z
    .string()
    .trim()
    .max(1000, 'Address cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  occupation: z
    .string()
    .trim()
    .max(100, 'Occupation cannot exceed 100 characters.')
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
  guardianId: z.string().uuid('Invalid guardian ID.').optional().nullable().or(z.literal('')),
  isPrimaryContact: z.boolean().default(false),
  isEmergencyContact: z.boolean().default(false),
  isFinancialContact: z.boolean().default(false),
  isAuthorizedPickup: z.boolean().default(false),
});

/**
 * Zod schema for updating an existing guardian relationship (PUT /api/students/{id}/guardians/{relationshipId}).
 */
export const updateGuardianSchema = z.object({
  relationshipType: z
    .string({ required_error: 'Relationship type is required.' })
    .trim()
    .min(1, 'Relationship type is required.')
    .max(50, 'Relationship type cannot exceed 50 characters.')
    .refine(
      (val) => GUARDIAN_RELATIONSHIP_TYPES.includes(val as (typeof GUARDIAN_RELATIONSHIP_TYPES)[number]),
      {
        message:
          'Relationship type must be one of: mother, father, legal_guardian, grandparent, sibling, other.',
      },
    ),
  isPrimaryContact: z.boolean().default(false),
  isEmergencyContact: z.boolean().default(false),
  isFinancialContact: z.boolean().default(false),
  isAuthorizedPickup: z.boolean().default(false),
  isActive: z.boolean().default(true),
  phone: z
    .string()
    .trim()
    .max(32, 'Guardian phone cannot exceed 32 characters.')
    .regex(PHONE_REGEX, 'Guardian phone number format is invalid.')
    .optional()
    .nullable()
    .or(z.literal('')),
  email: z
    .string()
    .trim()
    .max(254, 'Guardian email cannot exceed 254 characters.')
    .email('Guardian email must be a valid email address.')
    .optional()
    .nullable()
    .or(z.literal('')),
  address: z
    .string()
    .trim()
    .max(1000, 'Address cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  occupation: z
    .string()
    .trim()
    .max(100, 'Occupation cannot exceed 100 characters.')
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

/**
 * Zod schema for verifying or rejecting an official student document (PUT /api/students/{id}/documents/{docId}/verify).
 */
export const verifyDocumentSchema = z.object({
  submissionStatus: z.enum(DOCUMENT_VERIFICATION_STATUSES, {
    errorMap: () => ({
      message: "Submission status must be either 'verified' or 'rejected'.",
    }),
  }),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

/**
 * Zod schema for student search query parameters.
 */
export const studentSearchSchema = z.object({
  searchTerm: z.string().trim().optional(),
  campus: z.string().trim().optional(),
  level: z.string().trim().optional(),
  status: z.string().trim().optional(),
  pageNumber: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(1).max(100).default(20),
});

export type UpdateIdentityFormValues = z.infer<typeof updateIdentitySchema>;
export type AddGuardianFormValues = z.infer<typeof addGuardianSchema>;
export type UpdateGuardianFormValues = z.infer<typeof updateGuardianSchema>;
export type VerifyDocumentFormValues = z.infer<typeof verifyDocumentSchema>;
export type StudentSearchFormValues = z.infer<typeof studentSearchSchema>;

/**
 * Zod schema for changing student administrative placement (POST /api/students/{id}/placements).
 */
export const changePlacementSchema = z.object({
  campus: z
    .string({ required_error: 'Campus is required.' })
    .trim()
    .min(1, 'Campus is required.')
    .max(100, 'Campus cannot exceed 100 characters.'),
  schoolLevel: z
    .string({ required_error: 'School level is required.' })
    .trim()
    .min(1, 'School level is required.')
    .max(64, 'School level cannot exceed 64 characters.'),
  grade: z
    .string({ required_error: 'Grade is required.' })
    .trim()
    .min(1, 'Grade is required.')
    .max(64, 'Grade cannot exceed 64 characters.'),
  academicYear: z
    .string({ required_error: 'Academic year is required.' })
    .trim()
    .min(1, 'Academic year is required.')
    .max(32, 'Academic year cannot exceed 32 characters.'),
  effectiveDate: z
    .string({ required_error: 'Effective date is required.' })
    .trim()
    .min(1, 'Effective date is required.')
    .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.'),
  program: z
    .string()
    .trim()
    .max(100, 'Program cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  stream: z
    .string()
    .trim()
    .max(100, 'Stream cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  cohort: z
    .string()
    .trim()
    .max(50, 'Cohort cannot exceed 50 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  homeroom: z
    .string()
    .trim()
    .max(100, 'Homeroom cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  reason: z
    .string()
    .trim()
    .max(500, 'Reason cannot exceed 500 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  authorizedBy: z
    .string()
    .trim()
    .max(100, 'Authorized by cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  decisionReference: z
    .string()
    .trim()
    .max(100, 'Decision reference cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export const CHANGE_STATUS_ENUM = ['active', 'enrolled'] as const;

/**
 * Zod schema for changing student operational status (POST /api/students/{id}/status).
 */
export const changeStatusSchema = z.object({
  status: z.enum(CHANGE_STATUS_ENUM, {
    errorMap: () => ({ message: "Status must be either 'active' or 'enrolled'." }),
  }),
  effectiveDate: z
    .string({ required_error: 'Effective date is required.' })
    .trim()
    .min(1, 'Effective date is required.')
    .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.'),
  reason: z
    .string()
    .trim()
    .max(1000, 'Reason cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  authorizedBy: z
    .string()
    .trim()
    .max(100, 'Authorized by cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  decisionReference: z
    .string()
    .trim()
    .max(100, 'Decision reference cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type ChangePlacementFormData = z.infer<typeof changePlacementSchema>;
export type ChangeStatusFormData = z.infer<typeof changeStatusSchema>;
export type ChangePlacementFormValues = ChangePlacementFormData;
export type ChangeStatusFormValues = ChangeStatusFormData;
