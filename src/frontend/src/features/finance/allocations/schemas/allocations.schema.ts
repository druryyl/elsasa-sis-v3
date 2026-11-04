import { z } from 'zod';
import { toTypedSchema } from '@vee-validate/zod';

/**
 * Zod validation schema for an individual line item in an allocation request.
 */
export const allocationItemSchema = z.object({
  obligationId: z
    .string({ required_error: 'Obligation selection is required.' })
    .uuid('Invalid obligation ID format.'),
  amount: z.coerce
    .number({ required_error: 'Allocated amount is required.' })
    .int('Allocated amount must be an exact integer IDR value.')
    .positive('Allocated amount must be strictly greater than zero IDR.'),
  notes: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type AllocationItemSchemaInput = z.infer<typeof allocationItemSchema>;

/**
 * Zod validation schema for committing payment allocations across obligations (TD-05, SCR-FIN-02).
 */
export const allocatePaymentSchema = z.object({
  paymentId: z
    .string({ required_error: 'Payment selection is required.' })
    .uuid('Invalid payment ID format.'),
  allocations: z
    .array(allocationItemSchema, { required_error: 'At least one allocation item is required.' })
    .min(1, 'At least one obligation allocation is required.'),
  notes: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type AllocatePaymentSchemaInput = z.infer<typeof allocatePaymentSchema>;

/**
 * Typed VeeValidate schema for allocate payment modal form integration.
 */
export const allocatePaymentTypedSchema = toTypedSchema(allocatePaymentSchema);

/**
 * Zod validation schema for an individual line item in a replacement allocation request.
 */
export const replacementAllocationItemSchema = z.object({
  obligationId: z
    .string({ required_error: 'Obligation selection is required.' })
    .uuid('Invalid obligation ID format.'),
  amount: z.coerce
    .number({ required_error: 'Replacement allocated amount is required.' })
    .int('Allocated amount must be an exact integer IDR value.')
    .positive('Allocated amount must be strictly greater than zero IDR.'),
  notes: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type ReplacementAllocationItemSchemaInput = z.infer<typeof replacementAllocationItemSchema>;

/**
 * Zod validation schema for reallocating or reversing payment allocations (TD-05, SCR-FIN-03).
 */
export const reallocatePaymentSchema = z.object({
  paymentId: z
    .string({ required_error: 'Payment selection is required.' })
    .uuid('Invalid payment ID format.'),
  priorAllocationIds: z
    .array(z.string().uuid('Invalid prior allocation ID format.'), {
      required_error: 'At least one prior allocation ID is required to supersede/reverse.',
    })
    .min(1, 'At least one prior allocation must be targeted for reallocation/reversal.'),
  replacementAllocations: z.array(replacementAllocationItemSchema).optional().nullable(),
  reversalReason: z
    .string({ required_error: 'Reversal / correction reason is mandatory.' })
    .trim()
    .min(1, 'Reversal reason is mandatory.')
    .max(1000, 'Reversal reason cannot exceed 1000 characters.'),
  notes: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type ReallocatePaymentSchemaInput = z.infer<typeof reallocatePaymentSchema>;

/**
 * Typed VeeValidate schema for reallocate payment modal form integration.
 */
export const reallocatePaymentTypedSchema = toTypedSchema(reallocatePaymentSchema);

/**
 * Zod validation schema for querying smart allocation suggestions.
 */
export const suggestAllocationsParamsSchema = z
  .object({
    studentId: z
      .string()
      .uuid('Invalid student ID format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    applicantId: z
      .string()
      .uuid('Invalid applicant ID format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    paymentAmount: z.coerce
      .number({ required_error: 'Payment amount is required.' })
      .int('Payment amount must be an integer IDR value.')
      .positive('Payment amount must be greater than zero IDR.'),
  })
  .refine(
    (data) => {
      const hasStudent = !!data.studentId && data.studentId.trim().length > 0;
      const hasApplicant = !!data.applicantId && data.applicantId.trim().length > 0;
      return hasStudent || hasApplicant;
    },
    {
      message: 'Either Student ID or Applicant ID must be provided for auto-suggest.',
      path: ['studentId'],
    },
  );

export type SuggestAllocationsParamsSchemaInput = z.infer<typeof suggestAllocationsParamsSchema>;

/**
 * Typed VeeValidate schema for suggest allocations query params.
 */
export const suggestAllocationsTypedSchema = toTypedSchema(suggestAllocationsParamsSchema);
