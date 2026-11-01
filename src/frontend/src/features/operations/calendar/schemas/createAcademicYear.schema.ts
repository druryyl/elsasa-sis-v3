import { z } from 'zod';

const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Validation schema for creating an Academic Year.
 * Aligns client-side constraints with backend FluentValidation rules (CreateAcademicYearValidator.cs).
 */
export const createAcademicYearSchema = z
  .object({
    code: z
      .string({ required_error: 'Code is required.' })
      .trim()
      .min(1, 'Code is required.')
      .min(3, 'Code must be at least 3 characters.')
      .max(50, 'Code cannot exceed 50 characters.'),
    name: z
      .string({ required_error: 'Name is required.' })
      .trim()
      .min(1, 'Name is required.')
      .min(3, 'Name must be at least 3 characters.')
      .max(100, 'Name cannot exceed 100 characters.'),
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
  })
  .refine(
    (data) => {
      if (!data.startDate || !data.endDate) {
        return true;
      }
      if (!ISO_DATE_REGEX.test(data.startDate) || !ISO_DATE_REGEX.test(data.endDate)) {
        return true;
      }
      return data.endDate > data.startDate;
    },
    {
      message: 'End date must be greater than start date.',
      path: ['endDate'],
    },
  );

export type CreateAcademicYearFormData = z.infer<typeof createAcademicYearSchema>;
