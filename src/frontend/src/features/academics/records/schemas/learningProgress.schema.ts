import { z } from 'zod';
import { isValidCalendarDate } from '@/features/academics/assessment/schemas';
import {
  LEARNING_PROGRESS_BASES,
  LEARNING_PROGRESS_STATUSES,
} from '../types';

// ============================================================================
// Record Learning Progress Schema (SCR-ACD-07, ACD-WF-11, UC-ACD-12, P7-S19)
// ============================================================================

export const recordProgressSchema = z
  .object({
    studentId: z
      .string({ required_error: 'Student is required.' })
      .trim()
      .uuid('Student ID must be a valid UUID.'),
    learningUnitId: z
      .string({ required_error: 'Learning unit is required.' })
      .trim()
      .uuid('Learning unit ID must be a valid UUID.'),
    academicPeriodId: z
      .string({ required_error: 'Academic period is required.' })
      .trim()
      .uuid('Academic period ID must be a valid UUID.'),
    status: z.enum(
      LEARNING_PROGRESS_STATUSES as unknown as [string, ...string[]],
      {
        required_error: 'Progress status is required.',
      },
    ),
    basis: z.enum(
      LEARNING_PROGRESS_BASES as unknown as [string, ...string[]],
      {
        required_error: 'Operational basis is required.',
      },
    ),
    completionDate: z
      .string()
      .optional()
      .nullable()
      .refine(
        (val) => !val || isValidCalendarDate(val),
        'Completion date must be a valid calendar date in YYYY-MM-DD format.',
      ),
    notes: z
      .string()
      .trim()
      .max(1000, 'Notes cannot exceed 1000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .superRefine((data, ctx) => {
    if (data.status === 'completed' && (!data.completionDate || data.completionDate.trim().length === 0)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Completion date is mandatory when status is completed.',
        path: ['completionDate'],
      });
    }

    if (
      data.basis === 'authorized_decision' &&
      (!data.notes || data.notes.trim().length === 0)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message:
          'Explanatory notes are required when basis is authorized decision (e.g. transfer/RPL recognition).',
        path: ['notes'],
      });
    }
  });

export type RecordProgressFormValues = z.infer<typeof recordProgressSchema>;

// ============================================================================
// Conclude Participation Schema (SCR-ACD-07, UC-ACD-12, P7-S19)
// ============================================================================

export const concludeParticipationSchema = z.object({
  studentId: z
    .string({ required_error: 'Student is required.' })
    .trim()
    .uuid('Student ID must be a valid UUID.'),
  learningUnitId: z
    .string({ required_error: 'Learning unit is required.' })
    .trim()
    .uuid('Learning unit ID must be a valid UUID.'),
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .trim()
    .uuid('Academic period ID must be a valid UUID.'),
  notes: z
    .string()
    .trim()
    .max(1000, 'Notes cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type ConcludeParticipationFormValues = z.infer<
  typeof concludeParticipationSchema
>;
