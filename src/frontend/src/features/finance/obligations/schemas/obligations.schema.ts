import { z } from 'zod';

const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Zod validation schema for executing or previewing a batch billing cycle assessment (GAP-003, TD-04, SCR-FIN-01).
 */
export const batchAssessObligationsSchema = z.object({
  chargeRuleId: z
    .string({ required_error: 'Charge rule selection is required.' })
    .uuid('Invalid charge rule ID format.'),
  academicPeriodId: z
    .string({ required_error: 'Academic period selection is required.' })
    .uuid('Invalid academic period ID format.'),
  billingCycleLabel: z
    .string({ required_error: 'Billing cycle label is required.' })
    .trim()
    .min(1, 'Billing cycle label is required.')
    .max(100, 'Billing cycle label cannot exceed 100 characters.'),
  targetCampus: z
    .string()
    .trim()
    .max(64, 'Target campus cannot exceed 64 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  targetGrade: z
    .string()
    .trim()
    .max(32, 'Target grade cannot exceed 32 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  mode: z.enum(['preview', 'commit'], {
    required_error: 'Mode must be either preview or commit.',
  }),
});

export type BatchAssessObligationsSchemaInput = z.infer<typeof batchAssessObligationsSchema>;

/**
 * Zod validation schema for single-student ad-hoc fee assessment (GAP-003, TD-04, SCR-FIN-03).
 */
export const assessAdHocObligationSchema = z.object({
  studentId: z
    .string({ required_error: 'Student selection is required.' })
    .uuid('Invalid student ID format.'),
  chargeType: z
    .string({ required_error: 'Charge type is required.' })
    .trim()
    .min(1, 'Charge type is required.')
    .max(64, 'Charge type cannot exceed 64 characters.'),
  description: z
    .string({ required_error: 'Description is required.' })
    .trim()
    .min(1, 'Description is required.')
    .max(255, 'Description cannot exceed 255 characters.'),
  amount: z.coerce
    .number({ required_error: 'Amount is required.' })
    .int('Amount must be an integer IDR value.')
    .positive('Assessed amount must be strictly greater than zero.'),
  dueDate: z
    .string({ required_error: 'Due date is required.' })
    .regex(ISO_DATE_REGEX, 'Due date must follow YYYY-MM-DD format.'),
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .uuid('Invalid academic period ID format.'),
  notes: z
    .string()
    .trim()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  policyId: z
    .string()
    .uuid('Invalid policy ID format.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type AssessAdHocObligationSchemaInput = z.infer<typeof assessAdHocObligationSchema>;
