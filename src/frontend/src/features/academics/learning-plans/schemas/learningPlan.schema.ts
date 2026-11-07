import { z } from 'zod';
import {
  ADJUSTMENT_TYPES,
  APPROVAL_TYPES,
} from '../types';

// ============================================================================
// Create Student Learning Plan Schema (SCR-ACD-04, UC-ACD-09, P6-S17)
// ============================================================================

export const createPlanSchema = z.object({
  studentId: z
    .string({ required_error: 'Student is required.' })
    .trim()
    .uuid('Student ID must be a valid UUID.'),
  periodId: z
    .string({ required_error: 'Academic period is required.' })
    .trim()
    .uuid('Academic period ID must be a valid UUID.'),
  academicPeriodId: z
    .string()
    .trim()
    .uuid('Academic period ID must be a valid UUID.')
    .optional(),
  academicPeriodLabel: z
    .string()
    .trim()
    .max(100, 'Academic period label cannot exceed 100 characters.')
    .optional()
    .or(z.literal('')),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type CreatePlanFormValues = z.infer<typeof createPlanSchema>;

// ============================================================================
// Add Constituent Learning Unit Schema (SCR-ACD-04, P6-S17)
// ============================================================================

export const addUnitSchema = z.object({
  learningUnitId: z
    .string({ required_error: 'Learning unit is required.' })
    .trim()
    .uuid('Learning unit ID must be a valid UUID.'),
  sequenceOrder: z
    .union([
      z.number().int().positive('Sequence order must be a positive integer.'),
      z
        .string()
        .regex(/^\d+$/, 'Sequence order must be a positive integer.')
        .transform((val) => Number(val)),
    ])
    .optional()
    .nullable()
    .or(z.literal('')),
  isRequired: z.boolean().default(true),
  notes: z
    .string()
    .trim()
    .max(1000, 'Notes cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type AddUnitFormValues = z.infer<typeof addUnitSchema>;

// ============================================================================
// Add Academic Plan Adjustment Schema (SCR-ACD-04, P6-S17)
// ============================================================================

export const addAdjustmentSchema = z
  .object({
    adjustmentType: z.enum(ADJUSTMENT_TYPES as unknown as [string, ...string[]], {
      required_error: 'Adjustment type is required.',
    }),
    description: z
      .string({ required_error: 'Description is required.' })
      .trim()
      .min(1, 'Description is required.')
      .max(2000, 'Description cannot exceed 2000 characters.'),
    rationale: z
      .string({ required_error: 'Rationale is required.' })
      .trim()
      .min(10, 'Rationale must be at least 10 characters.')
      .max(2000, 'Rationale cannot exceed 2000 characters.'),
    effectiveDate: z
      .string({ required_error: 'Effective date is required.' })
      .trim()
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Effective date must be in YYYY-MM-DD format.'),
    endDate: z
      .string()
      .trim()
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'End date must be in YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    notes: z
      .string()
      .trim()
      .max(1000, 'Notes cannot exceed 1000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      if (!data.endDate || data.endDate.trim() === '') return true;
      return data.endDate >= data.effectiveDate;
    },
    {
      message: 'End date must be on or after effective date.',
      path: ['endDate'],
    },
  );

export type AddAdjustmentFormValues = z.infer<typeof addAdjustmentSchema>;

// ============================================================================
// Approve Student Learning Plan Schema (SCR-ACD-04, UC-ACD-09, P6-S17)
// ============================================================================

export const approvePlanSchema = z.object({
  approvalType: z.enum(APPROVAL_TYPES as unknown as [string, ...string[]], {
    required_error: 'Approval type is required.',
  }),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type ApprovePlanFormValues = z.infer<typeof approvePlanSchema>;

// ============================================================================
// Revise Student Learning Plan Schema (SCR-ACD-04, UC-ACD-09, P6-S17)
// ============================================================================

export const revisePlanSchema = z.object({
  notes: z
    .string({ required_error: 'Revision notes are required.' })
    .trim()
    .min(1, 'Revision notes are required.')
    .max(2000, 'Revision notes cannot exceed 2000 characters.'),
});

export type RevisePlanFormValues = z.infer<typeof revisePlanSchema>;
