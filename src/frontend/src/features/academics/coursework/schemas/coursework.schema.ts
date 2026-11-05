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

export const ASSIGNMENT_STATUSES = ['draft', 'released', 'closed'] as const;

export const SUBMISSION_EXCEPTION_TYPES = ['excused', 'exempted'] as const;

// ============================================================================
// Create Assignment Schema
// ============================================================================

export const createAssignmentSchema = z
  .object({
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(1, 'Title is required.')
      .max(200, 'Title cannot exceed 200 characters.'),
    description: z
      .string()
      .trim()
      .max(2000, 'Description cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    releaseDate: z
      .string({ required_error: 'Release date is required.' })
      .trim()
      .min(1, 'Release date is required.')
      .regex(ISO_DATE_REGEX, 'Release date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Release date must be a valid calendar date.',
      }),
    dueDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Due date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Due date must be a valid calendar date.',
      })
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
    (data) => {
      if (data.releaseDate && data.dueDate && data.dueDate.length > 0) {
        return data.dueDate >= data.releaseDate;
      }
      return true;
    },
    {
      message: 'Due date must be on or after the release date.',
      path: ['dueDate'],
    },
  );

export type CreateAssignmentFormValues = z.infer<typeof createAssignmentSchema>;

// ============================================================================
// Record Submission Policy Exception Schema (P5-S11, TD-06)
// ============================================================================

export const recordExceptionSchema = z.object({
  studentId: z
    .string({ required_error: 'Student ID is required.' })
    .trim()
    .min(1, 'Student ID is required.'),
  exceptionType: z.enum(SUBMISSION_EXCEPTION_TYPES, {
    required_error: 'Exception type must be excused or exempted.',
  }),
  reason: z
    .string({ required_error: 'Mandatory justification reason is required.' })
    .trim()
    .min(3, 'Reason must be at least 3 characters.')
    .max(2000, 'Reason cannot exceed 2000 characters.'),
});

export type RecordExceptionFormValues = z.infer<typeof recordExceptionSchema>;
