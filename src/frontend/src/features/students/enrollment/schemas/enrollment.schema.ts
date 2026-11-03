import { z } from 'zod';

export const PHONE_REGEX = /^[+0-9\s\-()]+$/;

export const GUARDIAN_RELATIONSHIP_TYPES = [
  'mother',
  'father',
  'legal_guardian',
  'grandparent',
  'sibling',
  'other',
] as const;

/**
 * Zod schema validating a guardian attached during enrollment.
 */
export const enrollGuardianSchema = z.object({
  fullName: z
    .string({ required_error: 'Guardian full name is required.' })
    .trim()
    .min(1, 'Guardian full name is required.')
    .max(200, 'Guardian full name cannot exceed 200 characters.'),
  relationshipType: z
    .string({ required_error: 'Relationship type is required.' })
    .trim()
    .min(1, 'Relationship type is required.')
    .max(50, 'Relationship type cannot exceed 50 characters.'),
  governmentIdType: z.string().trim().max(32, 'Government ID type cannot exceed 32 characters.').optional().nullable().or(z.literal('')),
  governmentIdNumber: z.string().trim().max(64, 'Government ID number cannot exceed 64 characters.').optional().nullable().or(z.literal('')),
  phone: z
    .string()
    .trim()
    .max(32, 'Phone number cannot exceed 32 characters.')
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
  address: z.string().trim().max(1000, 'Address cannot exceed 1000 characters.').optional().nullable().or(z.literal('')),
  occupation: z.string().trim().max(100, 'Occupation cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
  notes: z.string().trim().max(2000, 'Notes cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
  guardianId: z.string().uuid('Invalid guardian ID.').optional().nullable().or(z.literal('')),
  isPrimaryContact: z.boolean().default(false),
  isEmergencyContact: z.boolean().default(false),
  isFinancialContact: z.boolean().default(false),
  isAuthorizedPickup: z.boolean().default(false),
});

/**
 * Zod schema validating applicant enrollment execution (POST /api/students/enrollments).
 * Enforces initial campus, level, academic year, and the dual-mode payment gate (P8-S12, GAP-01).
 */
export const enrollAcceptedApplicantSchema = z
  .object({
    applicantId: z
      .string({ required_error: 'Applicant ID is required.' })
      .trim()
      .min(1, 'Applicant ID is required.'),
    academicYearId: z
      .string({ required_error: 'Academic year ID is required.' })
      .trim()
      .min(1, 'Academic year ID is required.'),
    initialCampus: z
      .string({ required_error: 'Initial campus is required.' })
      .trim()
      .min(1, 'Initial campus is required.')
      .max(100, 'Initial campus cannot exceed 100 characters.'),
    initialLevel: z
      .string({ required_error: 'Initial level is required.' })
      .trim()
      .min(1, 'Initial level is required.')
      .max(50, 'Initial level cannot exceed 50 characters.'),
    initialAcademicYear: z
      .string({ required_error: 'Initial academic year is required.' })
      .trim()
      .min(1, 'Initial academic year is required.')
      .max(20, 'Initial academic year cannot exceed 20 characters.'),
    initialProgram: z.string().trim().max(100, 'Initial program cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    initialCohort: z.string().trim().max(50, 'Initial cohort cannot exceed 50 characters.').optional().nullable().or(z.literal('')),
    paymentWaived: z.boolean().default(false),
    paymentReference: z.string().trim().max(100, 'Payment reference cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    waiverReference: z.string().trim().max(100, 'Waiver reference cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    checkFinanceModule: z.boolean().optional().nullable(),
    guardians: z.array(enrollGuardianSchema).optional().nullable(),
    notes: z.string().trim().max(2000, 'Notes cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
    applicationId: z.string().trim().optional().nullable().or(z.literal('')),
  })
  .refine(
    (data) => {
      if (data.paymentWaived) {
        return Boolean(data.waiverReference && data.waiverReference.trim().length > 0);
      }
      return true;
    },
    {
      message: 'Waiver reference is required when payment is waived.',
      path: ['waiverReference'],
    },
  )
  .refine(
    (data) => {
      if (!data.paymentWaived && data.checkFinanceModule !== true) {
        return Boolean(data.paymentReference && data.paymentReference.trim().length > 0);
      }
      return true;
    },
    {
      message: 'Payment reference is required when payment is not waived.',
      path: ['paymentReference'],
    },
  );

/**
 * Alias for enrollAcceptedApplicantSchema.
 */
export const enrollmentSchema = enrollAcceptedApplicantSchema;

export type EnrollAcceptedApplicantFormValues = z.infer<typeof enrollAcceptedApplicantSchema>;
export type EnrollGuardianFormValues = z.infer<typeof enrollGuardianSchema>;
export type EnrollmentFormValues = EnrollAcceptedApplicantFormValues;
