import { z } from 'zod';
import { ISO_DATE_REGEX } from './academicYear.schema';

export const CLOSURE_REASON_CATEGORIES = [
  'weather_disaster',
  'infrastructure_hazard',
  'health_safety',
  'government_mandate',
  'institutional_policy',
  'emergency',
] as const;

export type ClosureReasonCategory = (typeof CLOSURE_REASON_CATEGORIES)[number];

export const AFFECTED_POPULATIONS = ['all', 'students_only', 'staff_and_students'] as const;

export type AffectedPopulation = (typeof AFFECTED_POPULATIONS)[number];

/**
 * Validation schema for declaring a School Closure (UC-OPS-12).
 */
export const declareSchoolClosureSchema = z
  .object({
    academicYearId: z.string().uuid('Academic Year ID must be a valid UUID.').optional(),
    reasonCategory: z.enum(CLOSURE_REASON_CATEGORIES, {
      errorMap: () => ({ message: 'A valid closure reason category is required.' }),
    }),
    reasonDetails: z
      .string({ required_error: 'Reason details are required.' })
      .trim()
      .min(1, 'Reason details are required.')
      .min(5, 'Reason details must be at least 5 characters.')
      .max(2000, 'Reason details cannot exceed 2000 characters.'),
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
    affectedCampus: z
      .string()
      .trim()
      .max(100, 'Affected campus cannot exceed 100 characters.')
      .default('All'),
    affectedPopulation: z
      .enum(AFFECTED_POPULATIONS, {
        errorMap: () => ({ message: 'A valid affected population option is required.' }),
      })
      .default('all'),
    authorizedBy: z.string().max(100, 'Authorized by cannot exceed 100 characters.').optional().nullable(),
    recoveryPlanNotes: z.string().max(2000, 'Recovery plan notes cannot exceed 2000 characters.').optional().nullable(),
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

export type DeclareSchoolClosureFormData = z.infer<typeof declareSchoolClosureSchema>;
