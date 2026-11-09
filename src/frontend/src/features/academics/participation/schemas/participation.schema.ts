import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

export function isValidCalendarDate(val: string): boolean {
  if (!ISO_DATE_REGEX.test(val)) return false;
  const parts = val.split('-').map(Number);
  const year = parts[0];
  const month = parts[1];
  const day = parts[2];
  if (month < 1 || month > 12) return false;
  const date = new Date(Date.UTC(year, month - 1, day));
  return (
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day
  );
}

export const PARTICIPATION_TYPES = [
  'standard',
  'repeat',
  'transferred_in',
  'recognized_prior',
] as const;

export const PARTICIPATION_SOURCES = [
  'assigned',
  'student_selected',
  'learning_plan',
] as const;

export const PARTICIPATION_STATUSES = [
  'in_progress',
  'paused',
  'withdrawn',
  'completed',
] as const;

// ============================================================================
// Batch Register Cohort Schema
// ============================================================================

export const batchRegisterCohortSchema = z.object({
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .trim()
    .min(1, 'Academic period is required.'),
  academicPeriodLabel: z
    .string({ required_error: 'Academic period label is required.' })
    .trim()
    .min(1, 'Academic period label is required.')
    .max(100, 'Academic period label cannot exceed 100 characters.'),
  learningUnitIds: z
    .array(z.string().trim().min(1, 'Learning unit ID cannot be empty.'), {
      required_error: 'At least one learning unit must be selected.',
    })
    .min(1, 'At least one learning unit must be selected.'),
  sectionLabel: z
    .string({ required_error: 'Section label is required.' })
    .trim()
    .min(1, 'Section label is required.')
    .max(50, 'Section label cannot exceed 50 characters.'),
  startDate: z
    .string({ required_error: 'Start date is required.' })
    .trim()
    .min(1, 'Start date is required.')
    .regex(ISO_DATE_REGEX, 'Start date must be in YYYY-MM-DD format.')
    .refine(isValidCalendarDate, {
      message: 'Start date must be a valid calendar date.',
    }),
  studentIds: z
    .array(z.string().trim().min(1, 'Student ID cannot be empty.'))
    .optional(),
});

export type BatchRegisterCohortFormValues = z.infer<typeof batchRegisterCohortSchema>;

// ============================================================================
// Register Single Participation Schema
// ============================================================================

export const registerParticipationSchema = z.object({
  studentId: z
    .string({ required_error: 'Student is required.' })
    .trim()
    .min(1, 'Student is required.'),
  learningUnitId: z
    .string({ required_error: 'Learning unit is required.' })
    .trim()
    .min(1, 'Learning unit is required.'),
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .trim()
    .min(1, 'Academic period is required.'),
  academicPeriodLabel: z
    .string({ required_error: 'Academic period label is required.' })
    .trim()
    .min(1, 'Academic period label is required.')
    .max(100, 'Academic period label cannot exceed 100 characters.'),
  sectionLabel: z
    .string()
    .trim()
    .max(50, 'Section label cannot exceed 50 characters.')
    .optional()
    .nullable(),
  participationType: z.enum(PARTICIPATION_TYPES, {
    required_error: 'Participation type is required.',
  }),
  source: z.enum(PARTICIPATION_SOURCES, {
    required_error: 'Registration source is required.',
  }),
  startDate: z
    .string({ required_error: 'Start date is required.' })
    .trim()
    .min(1, 'Start date is required.')
    .regex(ISO_DATE_REGEX, 'Start date must be in YYYY-MM-DD format.')
    .refine(isValidCalendarDate, {
      message: 'Start date must be a valid calendar date.',
    }),
  studentLearningPlanId: z
    .string()
    .trim()
    .optional()
    .nullable(),
});

export type RegisterParticipationFormValues = z.infer<typeof registerParticipationSchema>;

// ============================================================================
// Recognize Prior Credit Schema
// ============================================================================

export const recognizePriorCreditSchema = z
  .object({
    studentId: z
      .string({ required_error: 'Student is required.' })
      .trim()
      .min(1, 'Student is required.'),
    learningUnitId: z
      .string({ required_error: 'Learning unit is required.' })
      .trim()
      .min(1, 'Learning unit is required.'),
    academicPeriodId: z
      .string({ required_error: 'Academic period is required.' })
      .trim()
      .min(1, 'Academic period is required.'),
    academicPeriodLabel: z
      .string({ required_error: 'Academic period label is required.' })
      .trim()
      .min(1, 'Academic period label is required.')
      .max(100, 'Academic period label cannot exceed 100 characters.'),
    participationType: z.enum(['transferred_in', 'recognized_prior'] as const, {
      required_error: 'Participation type must be transferred_in or recognized_prior.',
    }),
    sourceInstitution: z
      .string()
      .trim()
      .max(200, 'Source institution cannot exceed 200 characters.')
      .optional()
      .nullable(),
    externalGradeOrCredit: z
      .string()
      .trim()
      .max(100, 'External grade or credit cannot exceed 100 characters.')
      .optional()
      .nullable(),
    recognitionBasis: z
      .string()
      .trim()
      .max(2000, 'Recognition basis cannot exceed 2000 characters.')
      .optional()
      .nullable(),
    startDate: z
      .string({ required_error: 'Start date is required.' })
      .trim()
      .min(1, 'Start date is required.')
      .regex(ISO_DATE_REGEX, 'Start date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Start date must be a valid calendar date.',
      }),
    completionDate: z
      .string({ required_error: 'Completion date is required.' })
      .trim()
      .min(1, 'Completion date is required.')
      .regex(ISO_DATE_REGEX, 'Completion date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Completion date must be a valid calendar date.',
      }),
    studentLearningPlanId: z.string().trim().optional().nullable(),
    notes: z
      .string()
      .trim()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .optional()
      .nullable(),
  })
  .refine(
    (data) => {
      if (!data.startDate || !data.completionDate) return true;
      return data.completionDate >= data.startDate;
    },
    {
      message: 'Completion date must be on or after the start date.',
      path: ['completionDate'],
    },
  );

export type RecognizePriorCreditFormValues = z.infer<typeof recognizePriorCreditSchema>;

// ============================================================================
// Update Participation Status Schema
// ============================================================================

export const updateParticipationStatusSchema = z.object({
  newStatus: z.enum(['in_progress', 'paused', 'withdrawn'] as const, {
    required_error: 'New status must be in_progress, paused, or withdrawn.',
  }),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable(),
});

export type UpdateParticipationStatusFormValues = z.infer<
  typeof updateParticipationStatusSchema
>;

// ============================================================================
// Conclude Participation Schema
// ============================================================================

export const concludeParticipationSchema = z.object({
  completionDate: z
    .string({ required_error: 'Completion date is required.' })
    .trim()
    .min(1, 'Completion date is required.')
    .regex(ISO_DATE_REGEX, 'Completion date must be in YYYY-MM-DD format.')
    .refine(isValidCalendarDate, {
      message: 'Completion date must be a valid calendar date.',
    }),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable(),
});

export type ConcludeParticipationFormValues = z.infer<typeof concludeParticipationSchema>;
