import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
export const TIME_REGEX = /^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/;

export const UNAVAILABILITY_TYPES = [
  'medical_leave',
  'official_training',
  'personal_leave',
  'blackout',
] as const;

export const AVAILABILITY_STATUSES = [
  'active',
  'revoked',
  'superseded',
] as const;

export const DELIVERY_MODES = [
  'All',
  'in_person',
  'online',
  'hybrid',
] as const;

/**
 * Validates that an optional date string is in YYYY-MM-DD format.
 */
function isValidOptionalIsoDate(val?: string | null): boolean {
  if (!val || val.trim() === '') return true;
  return ISO_DATE_REGEX.test(val.trim());
}

/**
 * Validates that an optional time string is in HH:mm or HH:mm:ss format.
 */
function isValidOptionalTime(val?: string | null): boolean {
  if (!val || val.trim() === '') return true;
  return TIME_REGEX.test(val.trim());
}

/**
 * Zod validation schema for declaring recurring weekly teacher availability (P7-S23, SCR-TCH-04).
 * Aligns with DeclareTeacherAvailabilityValidator on backend.
 */
export const declareAvailabilitySchema = z
  .object({
    academicPeriodId: z
      .string()
      .min(1, 'Academic period ID is required.')
      .uuid('Academic period ID must be a valid UUID.'),
    maxAvailableHours: z.coerce
      .number({ invalid_type_error: 'Maximum available hours must be a number.' })
      .positive('Maximum available hours must be greater than 0.')
      .max(168, 'Maximum available hours cannot exceed 168 hours per week.'),
    effectiveStartDate: z
      .string()
      .min(1, 'Effective start date is required.')
      .regex(ISO_DATE_REGEX, 'Effective start date must be a valid date in YYYY-MM-DD format.'),
    effectiveEndDate: z
      .string()
      .min(1, 'Effective end date is required.')
      .regex(ISO_DATE_REGEX, 'Effective end date must be a valid date in YYYY-MM-DD format.'),
    campusScope: z
      .string()
      .max(100, 'Campus scope cannot exceed 100 characters.')
      .nullish()
      .transform((val) => val ?? undefined),
    availableDeliveryModes: z
      .string()
      .max(100, 'Available delivery modes cannot exceed 100 characters.')
      .nullish()
      .transform((val) => val ?? undefined),
    recurringAvailableSchedule: z
      .string()
      .nullish()
      .transform((val) => val ?? undefined),
    notes: z
      .string()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .nullish()
      .transform((val) => val ?? undefined),
  })
  .refine(
    (data) => {
      if (!data.effectiveStartDate || !data.effectiveEndDate) return true;
      return data.effectiveEndDate >= data.effectiveStartDate;
    },
    {
      message: 'Effective end date must be on or after effective start date.',
      path: ['effectiveEndDate'],
    },
  );

export type DeclareAvailabilityFormValues = z.infer<typeof declareAvailabilitySchema>;

/**
 * Zod validation schema for recording a temporary leave or blackout window (P7-S23, SCR-TCH-04).
 * Aligns with CreateUnavailabilityWindowValidator on backend.
 */
export const createUnavailabilityWindowSchema = z
  .object({
    startDate: z
      .string()
      .min(1, 'Start date is required.')
      .regex(ISO_DATE_REGEX, 'Start date must be a valid date in YYYY-MM-DD format.'),
    endDate: z
      .string()
      .min(1, 'End date is required.')
      .regex(ISO_DATE_REGEX, 'End date must be a valid date in YYYY-MM-DD format.'),
    unavailabilityType: z.enum(UNAVAILABILITY_TYPES, {
      errorMap: () => ({
        message:
          "Unavailability type must be one of: 'medical_leave', 'official_training', 'personal_leave', 'blackout'.",
      }),
    }),
    reason: z
      .string()
      .min(1, 'Reason is required.')
      .max(2000, 'Reason cannot exceed 2000 characters.'),
    startTime: z
      .string()
      .nullish()
      .refine(isValidOptionalTime, 'Start time must be in HH:mm format.')
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    endTime: z
      .string()
      .nullish()
      .refine(isValidOptionalTime, 'End time must be in HH:mm format.')
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    externalSourceReference: z
      .string()
      .max(100, 'External source reference cannot exceed 100 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    emergencyCoverageRequired: z
      .boolean()
      .optional()
      .default(false),
    notes: z
      .string()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  })
  .refine(
    (data) => {
      if (!data.startDate || !data.endDate) return true;
      return data.endDate >= data.startDate;
    },
    {
      message: 'End date must be on or after start date.',
      path: ['endDate'],
    },
  )
  .refine(
    (data) => {
      if (!data.startTime || !data.endTime) return true;
      return data.endTime >= data.startTime;
    },
    {
      message: 'End time must be on or after start time.',
      path: ['endTime'],
    },
  );

export type CreateUnavailabilityWindowFormValues = z.infer<typeof createUnavailabilityWindowSchema>;
