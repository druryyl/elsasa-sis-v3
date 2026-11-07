import { z } from 'zod';
import {
  INTERVENTION_STATUSES,
  INTERVENTION_TYPES,
  TRIGGER_TYPES,
} from '../types';

// ============================================================================
// Initiate Intervention Schema (SCR-ACD-06, UC-ACD-11, P7-S19)
// ============================================================================

export const initiateInterventionSchema = z.object({
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
  interventionType: z.enum(
    INTERVENTION_TYPES as unknown as [string, ...string[]],
    {
      required_error: 'Intervention type is required.',
    },
  ),
  triggerType: z.enum(TRIGGER_TYPES as unknown as [string, ...string[]], {
    required_error: 'Trigger type is required.',
  }),
  triggerDescription: z
    .string({ required_error: 'Trigger description is required.' })
    .trim()
    .min(1, 'Trigger description is required.')
    .max(1000, 'Trigger description cannot exceed 1000 characters.'),
  planDescription: z
    .string({ required_error: 'Plan description is required.' })
    .trim()
    .min(5, 'Plan description must be at least 5 characters.')
    .max(2000, 'Plan description cannot exceed 2000 characters.'),
  targetCompletionDate: z
    .string()
    .trim()
    .regex(/^\d{4}-\d{2}-\d{2}$/, 'Target milestone date must be in YYYY-MM-DD format.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type InitiateInterventionFormValues = z.infer<
  typeof initiateInterventionSchema
>;

// ============================================================================
// Update Intervention Status Schema (SCR-ACD-06, UC-ACD-11, P7-S19)
// ============================================================================

export const updateInterventionStatusSchema = z
  .object({
    status: z.enum(INTERVENTION_STATUSES as unknown as [string, ...string[]], {
      required_error: 'Status is required.',
    }),
    outcomeNotes: z
      .string()
      .trim()
      .max(2000, 'Outcome notes cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .superRefine((data, ctx) => {
    if (
      (data.status === 'completed' || data.status === 'cancelled') &&
      (!data.outcomeNotes || data.outcomeNotes.trim().length === 0)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message:
          'Outcome notes are required when completing or cancelling an intervention.',
        path: ['outcomeNotes'],
      });
    }
  });

export type UpdateInterventionStatusFormValues = z.infer<
  typeof updateInterventionStatusSchema
>;
