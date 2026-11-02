import { z } from 'zod';
import { ISO_DATE_REGEX } from './academicYear.schema';

export const CALENDAR_DESIGNATIONS = [
  'instructional_day',
  'holiday',
  'school_closure',
  'staff_only_day',
  'exam_day',
  'significant_date',
  'domain_window',
] as const;

export type CalendarDesignation = (typeof CALENDAR_DESIGNATIONS)[number];

/**
 * Validation schema for creating or updating a Calendar Entry.
 */
export const calendarEntrySchema = z
  .object({
    entryDate: z
      .string({ required_error: 'Entry date is required.' })
      .trim()
      .min(1, 'Entry date is required.')
      .regex(ISO_DATE_REGEX, 'Entry date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'Entry date must be a valid calendar date.',
      }),
    endDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.')
      .refine((date) => !Number.isNaN(Date.parse(date)), {
        message: 'End date must be a valid calendar date.',
      })
      .optional()
      .nullable()
      .or(z.literal('')),
    designation: z.enum(CALENDAR_DESIGNATIONS, {
      errorMap: () => ({ message: 'A valid calendar designation is required.' }),
    }),
    name: z
      .string({ required_error: 'Name is required.' })
      .trim()
      .min(1, 'Name is required.')
      .min(2, 'Name must be at least 2 characters.')
      .max(200, 'Name cannot exceed 200 characters.'),
    description: z.string().max(1000, 'Description cannot exceed 1000 characters.').optional().nullable(),
    isInstructional: z.boolean().default(false),
    appliesTo: z
      .string()
      .trim()
      .max(100, 'Applies-to filter cannot exceed 100 characters.')
      .default('All'),
  })
  .refine(
    (data) => {
      if (!data.entryDate || !data.endDate) {
        return true;
      }
      if (!ISO_DATE_REGEX.test(data.entryDate) || !ISO_DATE_REGEX.test(data.endDate)) {
        return true;
      }
      return data.endDate >= data.entryDate;
    },
    {
      message: 'End date must be greater than or equal to entry date.',
      path: ['endDate'],
    },
  );

export type CalendarEntryFormData = z.infer<typeof calendarEntrySchema>;
