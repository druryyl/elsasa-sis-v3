import { z } from 'zod';
import { ISO_DATE_REGEX } from './academicYear.schema';

export const OPERATIONAL_PERIOD_TYPES = [
  'semester',
  'term',
  'quarter',
  'admission_window',
  'assessment_window',
  'reporting_period',
  'event_period',
  'custom',
] as const;

export type OperationalPeriodType = (typeof OPERATIONAL_PERIOD_TYPES)[number];

/**
 * Validation schema for creating an Operational Period.
 */
export const createOperationalPeriodSchema = z
  .object({
    periodCode: z
      .string({ required_error: 'Period code is required.' })
      .trim()
      .min(1, 'Period code is required.')
      .min(2, 'Period code must be at least 2 characters.')
      .max(64, 'Period code cannot exceed 64 characters.'),
    name: z
      .string({ required_error: 'Period name is required.' })
      .trim()
      .min(1, 'Period name is required.')
      .min(2, 'Period name must be at least 2 characters.')
      .max(200, 'Period name cannot exceed 200 characters.'),
    periodType: z.enum(OPERATIONAL_PERIOD_TYPES, {
      errorMap: () => ({ message: 'A valid period type is required.' }),
    }),
    startDate: z
      .string({ required_error: 'Start date is required.' })
      .trim()
      .min(1, 'Start date is required.')
      .regex(ISO_DATE_REGEX, 'Start date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'Start date must be a valid calendar date.',
      }),
    endDate: z
      .string({ required_error: 'End date is required.' })
      .trim()
      .min(1, 'End date is required.')
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'End date must be a valid calendar date.',
      }),
    parentPeriodId: z
      .string()
      .uuid('Parent period ID must be a valid UUID.')
      .optional()
      .nullable()
      .or(z.literal('')),
    campusContext: z
      .string()
      .trim()
      .max(100, 'Campus context cannot exceed 100 characters.')
      .default('All'),
    notes: z.string().max(1000, 'Notes cannot exceed 1000 characters.').optional().nullable(),
  })
  .refine(
    (data) => {
      if (!data.startDate || !data.endDate) {
        return true;
      }
      if (!ISO_DATE_REGEX.test(data.startDate) || !ISO_DATE_REGEX.test(data.endDate)) {
        return true;
      }
      return data.endDate >= data.startDate;
    },
    {
      message: 'End date must be greater than or equal to start date.',
      path: ['endDate'],
    },
  );

export type CreateOperationalPeriodFormData = z.infer<typeof createOperationalPeriodSchema>;

/**
 * Validation schema for updating an existing Operational Period.
 */
export const updateOperationalPeriodSchema = z
  .object({
    name: z
      .string({ required_error: 'Period name is required.' })
      .trim()
      .min(1, 'Period name is required.')
      .min(2, 'Period name must be at least 2 characters.')
      .max(200, 'Period name cannot exceed 200 characters.'),
    startDate: z
      .string({ required_error: 'Start date is required.' })
      .trim()
      .min(1, 'Start date is required.')
      .regex(ISO_DATE_REGEX, 'Start date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'Start date must be a valid calendar date.',
      }),
    endDate: z
      .string({ required_error: 'End date is required.' })
      .trim()
      .min(1, 'End date is required.')
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'End date must be a valid calendar date.',
      }),
    campusContext: z
      .string()
      .trim()
      .max(100, 'Campus context cannot exceed 100 characters.')
      .optional(),
    notes: z.string().max(1000, 'Notes cannot exceed 1000 characters.').optional().nullable(),
  })
  .refine(
    (data) => {
      if (!data.startDate || !data.endDate) {
        return true;
      }
      if (!ISO_DATE_REGEX.test(data.startDate) || !ISO_DATE_REGEX.test(data.endDate)) {
        return true;
      }
      return data.endDate >= data.startDate;
    },
    {
      message: 'End date must be greater than or equal to start date.',
      path: ['endDate'],
    },
  );

export type UpdateOperationalPeriodFormData = z.infer<typeof updateOperationalPeriodSchema>;
