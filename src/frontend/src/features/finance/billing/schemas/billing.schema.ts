import { z } from 'zod';

const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Zod validation schema for compiling an authorized bill / invoice (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
 */
export const generateBillSchema = z.object({
  studentId: z
    .string({ required_error: 'Student selection is required.' })
    .uuid('Invalid student ID format.'),
  obligationIds: z
    .array(z.string().uuid('Invalid obligation ID format.'))
    .optional()
    .nullable(),
  paymentDueDate: z
    .string()
    .regex(ISO_DATE_REGEX, 'Payment due date must follow YYYY-MM-DD format.')
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
});

export type GenerateBillSchemaInput = z.infer<typeof generateBillSchema>;

/**
 * Zod validation schema for generating a periodic financial statement.
 */
export const generateStatementSchema = z
  .object({
    studentId: z
      .string({ required_error: 'Student selection is required.' })
      .uuid('Invalid student ID format.'),
    billingPeriodStart: z
      .string({ required_error: 'Billing period start date is required.' })
      .regex(ISO_DATE_REGEX, 'Period start date must follow YYYY-MM-DD format.'),
    billingPeriodEnd: z
      .string({ required_error: 'Billing period end date is required.' })
      .regex(ISO_DATE_REGEX, 'Period end date must follow YYYY-MM-DD format.'),
    notes: z
      .string()
      .trim()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine((data) => data.billingPeriodStart <= data.billingPeriodEnd, {
    message: 'Billing period end date must be greater than or equal to start date.',
    path: ['billingPeriodEnd'],
  });

export type GenerateStatementSchemaInput = z.infer<typeof generateStatementSchema>;
