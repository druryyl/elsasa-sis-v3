import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
export const PHONE_REGEX = /^[+0-9\s\-()]+$/;

export const DECISION_TYPES = [
  'accepted',
  'rejected',
  'waitlisted',
  'withdrawn',
  'expired',
  'deferred',
] as const;

export const DOCUMENT_VERIFICATION_STATUSES = ['verified', 'rejected'] as const;

/**
 * Validates guardian or family contact details.
 */
export const contactSchema = z
  .object({
    fullName: z
      .string({ required_error: 'Contact full name is required.' })
      .trim()
      .min(1, 'Contact full name is required.')
      .max(200, 'Contact full name cannot exceed 200 characters.'),
    relationship: z
      .string({ required_error: 'Relationship is required.' })
      .trim()
      .min(1, 'Relationship is required.')
      .max(50, 'Relationship cannot exceed 50 characters.'),
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
    isPrimary: z.boolean().default(true),
  })
  .refine(
    (contact) => {
      const hasPhone = Boolean(contact.phone && contact.phone.trim().length > 0);
      const hasEmail = Boolean(contact.email && contact.email.trim().length > 0);
      return hasPhone || hasEmail;
    },
    {
      message: 'A contact must have either a phone number or an email address.',
      path: ['phone'],
    },
  );

export type ContactFormData = z.infer<typeof contactSchema>;

/**
 * Validation schema for registering an admission application (SCR-STU-01, SCR-SELF-01).
 */
export const registerApplicationSchema = z
  .object({
    applicantId: z
      .string()
      .uuid('Applicant ID must be a valid UUID.')
      .optional()
      .nullable()
      .or(z.literal('')),
    fullName: z
      .string({ required_error: 'Applicant full name is required.' })
      .trim()
      .min(1, 'Applicant full name is required.')
      .max(200, 'Applicant full name cannot exceed 200 characters.'),
    preferredName: z.string().trim().max(100, 'Preferred name cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    dateOfBirth: z
      .string({ required_error: 'Date of birth is required.' })
      .trim()
      .min(1, 'Date of birth is required.')
      .regex(ISO_DATE_REGEX, 'Date of birth must be in YYYY-MM-DD format.')
      .refine((dob) => !Number.isNaN(Date.parse(dob)), {
        message: 'Date of birth must be a valid calendar date.',
      })
      .refine((dob) => {
        const parsed = new Date(dob);
        const today = new Date();
        return parsed <= today;
      }, {
        message: 'Date of birth cannot be in the future.',
      }),
    placeOfBirth: z.string().trim().max(100, 'Place of birth cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    gender: z.string().trim().max(20, 'Gender cannot exceed 20 characters.').optional().nullable().or(z.literal('')),
    nationality: z.string().trim().max(100, 'Nationality cannot exceed 100 characters.').default('Indonesian'),
    governmentIdType: z.string().trim().max(32, 'Government ID type cannot exceed 32 characters.').optional().nullable().or(z.literal('')),
    governmentIdNumber: z.string().trim().max(64, 'Government ID number cannot exceed 64 characters.').optional().nullable().or(z.literal('')),
    email: z
      .string()
      .trim()
      .max(254, 'Email cannot exceed 254 characters.')
      .email('Email must be a valid email address.')
      .optional()
      .nullable()
      .or(z.literal('')),
    phone: z
      .string()
      .trim()
      .max(32, 'Phone number cannot exceed 32 characters.')
      .regex(PHONE_REGEX, 'Phone number format is invalid.')
      .optional()
      .nullable()
      .or(z.literal('')),
    address: z.string().trim().max(2000, 'Address cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
    previousSchoolName: z.string().trim().max(200, 'Previous school name cannot exceed 200 characters.').optional().nullable().or(z.literal('')),
    previousSchoolAddress: z.string().trim().max(2000, 'Previous school address cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
    admissionPeriodId: z
      .string({ required_error: 'Admission period is required.' })
      .uuid('Admission period ID must be a valid UUID.'),
    applicationType: z.string().trim().max(32, 'Application type cannot exceed 32 characters.').default('new'),
    requestedEntryLevel: z
      .string({ required_error: 'Requested entry level is required.' })
      .trim()
      .min(1, 'Requested entry level is required.')
      .max(64, 'Requested entry level cannot exceed 64 characters.'),
    requestedCampus: z.string().trim().max(100, 'Requested campus cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    requestedProgram: z.string().trim().max(100, 'Requested program cannot exceed 100 characters.').optional().nullable().or(z.literal('')),
    contacts: z
      .array(contactSchema)
      .min(1, 'At least one contact is required.')
      .refine(
        (contactsList) => contactsList.some((c) => c.isPrimary),
        { message: 'At least one primary guardian contact is required.', path: ['contacts'] },
      ),
    bypassWindowCheck: z.boolean().default(false),
    notes: z.string().trim().max(2000, 'Notes cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
  });

export type RegisterApplicationFormData = z.infer<typeof registerApplicationSchema>;

/**
 * Validation schema for recording an official admission decision.
 */
export const recordDecisionSchema = z
  .object({
    decisionType: z.enum(DECISION_TYPES, {
      errorMap: () => ({
        message: 'Decision type must be one of: accepted, rejected, waitlisted, withdrawn, expired, deferred.',
      }),
    }),
    decisionDate: z
      .string({ required_error: 'Decision date is required.' })
      .trim()
      .min(1, 'Decision date is required.')
      .regex(ISO_DATE_REGEX, 'Decision date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'Decision date must be a valid calendar date.',
      }),
    effectiveDate: z
      .string({ required_error: 'Effective date is required.' })
      .trim()
      .min(1, 'Effective date is required.')
      .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'Effective date must be a valid calendar date.',
      }),
    reason: z
      .string()
      .trim()
      .max(2000, 'Reason cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    supportingReference: z
      .string()
      .trim()
      .max(200, 'Supporting reference cannot exceed 200 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    deferredAdmissionPeriodId: z
      .string()
      .uuid('Deferred admission period ID must be a valid UUID.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      if (data.decisionType === 'deferred') {
        return Boolean(
          data.deferredAdmissionPeriodId &&
            data.deferredAdmissionPeriodId.trim().length > 0,
        );
      }
      return true;
    },
    {
      message: "Deferred admission period is required when decision type is 'deferred'.",
      path: ['deferredAdmissionPeriodId'],
    },
  );

export type RecordDecisionFormData = z.infer<typeof recordDecisionSchema>;

/**
 * Validation schema for adding or updating manual entrance assessment diagnostic evidence.
 */
export const assessmentEvidenceSchema = z.object({
  assessmentType: z
    .string({ required_error: 'Assessment type is required.' })
    .trim()
    .min(1, 'Assessment type is required.')
    .max(100, 'Assessment type cannot exceed 100 characters.'),
  score: z
    .string()
    .trim()
    .max(50, 'Score cannot exceed 50 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  source: z
    .string()
    .trim()
    .max(100, 'Source cannot exceed 100 characters.')
    .default('External Assessment Center'),
  assessmentDate: z
    .string()
    .trim()
    .regex(ISO_DATE_REGEX, 'Assessment date must be in YYYY-MM-DD format.')
    .refine((date) => !Number.isNaN(Date.parse(date)), {
      message: 'Assessment date must be a valid calendar date.',
    })
    .refine(
      (date) => {
        const parsed = new Date(date);
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        return parsed <= tomorrow;
      },
      {
        message: 'Assessment date cannot be in the future.',
      },
    )
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

export type AssessmentEvidenceFormData = z.infer<typeof assessmentEvidenceSchema>;

/**
 * Validation schema for verifying or rejecting an applicant credential document.
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

export type VerifyDocumentFormData = z.infer<typeof verifyDocumentSchema>;

/**
 * Validation schema for public self-service status tracking inquiry.
 */
export const trackStatusSchema = z.object({
  applicationNumber: z
    .string({ required_error: 'Application number is required.' })
    .trim()
    .min(1, 'Application number is required.')
    .max(50, 'Application number cannot exceed 50 characters.'),
  dateOfBirth: z
    .string({ required_error: 'Date of birth is required.' })
    .trim()
    .min(1, 'Date of birth is required.')
    .regex(ISO_DATE_REGEX, 'Date of birth must be in YYYY-MM-DD format.')
    .refine((dob) => !Number.isNaN(Date.parse(dob)), {
      message: 'Date of birth must be a valid calendar date.',
    }),
});

export type TrackStatusFormData = z.infer<typeof trackStatusSchema>;

/**
 * Validation schema for Step 1 of public admission wizard (Period & Grade Selection).
 */
export const wizardStep1Schema = z.object({
  admissionPeriodId: z
    .string({ required_error: 'Admission period is required.' })
    .trim()
    .min(1, 'Admission period is required.')
    .pipe(z.string().uuid('Admission period ID must be a valid UUID.')),
  requestedEntryLevel: z
    .string({ required_error: 'Requested entry level is required.' })
    .trim()
    .min(1, 'Requested entry level is required.')
    .max(64, 'Requested entry level cannot exceed 64 characters.'),
  applicationType: z.string().trim().max(32).default('new'),
  requestedCampus: z.string().trim().max(100).optional().nullable().or(z.literal('')),
  requestedProgram: z.string().trim().max(100).optional().nullable().or(z.literal('')),
});

export type WizardStep1FormData = z.infer<typeof wizardStep1Schema>;

/**
 * Validation schema for Step 2 of public admission wizard (Applicant Biographical Info).
 */
export const wizardStep2Schema = z.object({
  fullName: z
    .string({ required_error: 'Applicant full name is required.' })
    .trim()
    .min(1, 'Applicant full name is required.')
    .max(200, 'Applicant full name cannot exceed 200 characters.'),
  preferredName: z.string().trim().max(100).optional().nullable().or(z.literal('')),
  dateOfBirth: z
    .string({ required_error: 'Date of birth is required.' })
    .trim()
    .min(1, 'Date of birth is required.')
    .regex(ISO_DATE_REGEX, 'Date of birth must be in YYYY-MM-DD format.')
    .refine((dob) => !Number.isNaN(Date.parse(dob)), {
      message: 'Date of birth must be a valid calendar date.',
    })
    .refine(
      (dob) => {
        const parsed = new Date(dob);
        const today = new Date();
        return parsed <= today;
      },
      {
        message: 'Date of birth cannot be in the future.',
      },
    ),
  placeOfBirth: z.string().trim().max(100).optional().nullable().or(z.literal('')),
  gender: z.string().trim().max(20).optional().nullable().or(z.literal('')),
  nationality: z.string().trim().max(100).default('Indonesian'),
  governmentIdType: z.string().trim().max(32).optional().nullable().or(z.literal('')),
  governmentIdNumber: z.string().trim().max(64).optional().nullable().or(z.literal('')),
  email: z
    .string()
    .trim()
    .max(254)
    .email('Email must be a valid email address.')
    .optional()
    .nullable()
    .or(z.literal('')),
  phone: z
    .string()
    .trim()
    .max(32)
    .regex(PHONE_REGEX, 'Phone number format is invalid.')
    .optional()
    .nullable()
    .or(z.literal('')),
  address: z.string().trim().max(2000).optional().nullable().or(z.literal('')),
  previousSchoolName: z.string().trim().max(200).optional().nullable().or(z.literal('')),
  previousSchoolAddress: z.string().trim().max(2000).optional().nullable().or(z.literal('')),
});

export type WizardStep2FormData = z.infer<typeof wizardStep2Schema>;

/**
 * Validation schema for Step 3 of public admission wizard (Guardian & Family Contacts).
 */
export const wizardStep3Schema = z.object({
  contacts: z
    .array(contactSchema)
    .min(1, 'At least one contact is required.')
    .refine(
      (contactsList) => contactsList.some((c) => c.isPrimary),
      { message: 'At least one primary guardian contact is required.', path: ['contacts'] },
    ),
});

export type WizardStep3FormData = z.infer<typeof wizardStep3Schema>;

/**
 * Validation schema for Step 5 of public admission wizard (Summary & Confirmation).
 */
export const wizardStep5Schema = z.object({
  confirmation: z.literal(true, {
    errorMap: () => ({ message: 'You must confirm and certify the accuracy of all submitted details.' }),
  }),
});

export type WizardStep5FormData = z.infer<typeof wizardStep5Schema>;
