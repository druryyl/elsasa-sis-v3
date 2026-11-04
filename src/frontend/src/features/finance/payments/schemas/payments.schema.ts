import { z } from 'zod';
import { toTypedSchema } from '@vee-validate/zod';

const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
const PHONE_REGEX = /^[+0-9\s\-()]{7,30}$/;

export const PAYMENT_METHODS = [
  'bank_transfer',
  'cash',
  'pos_card',
  'qris',
  'virtual_account',
  'cheque',
  'other',
] as const;

/**
 * Zod validation schema for self-service payment intake (SCR-SELF-02, UC-FIN-01).
 */
export const submitPaymentSchema = z
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
    studentAccountId: z
      .string()
      .uuid('Invalid student account ID format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    amount: z.coerce
      .number({ required_error: 'Payment amount is required.' })
      .int('Payment amount must be an exact integer IDR value.')
      .positive('Payment amount must be strictly greater than zero IDR.'),
    paymentMethod: z
      .string({ required_error: 'Payment method is required.' })
      .trim()
      .min(1, 'Payment method is required.')
      .max(64, 'Payment method cannot exceed 64 characters.'),
    paymentDate: z
      .string()
      .regex(ISO_DATE_REGEX, 'Payment date must follow YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    externalReference: z
      .string()
      .trim()
      .max(100, 'External reference cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerName: z
      .string({ required_error: 'Payer name is required.' })
      .trim()
      .min(1, 'Payer name is required.')
      .max(200, 'Payer name cannot exceed 200 characters.'),
    payerIdentificationNumber: z
      .string()
      .trim()
      .max(64, 'Identification number cannot exceed 64 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerEmail: z
      .string()
      .trim()
      .email('Payer email is not a valid email address.')
      .max(200, 'Payer email cannot exceed 200 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerPhone: z
      .string()
      .trim()
      .regex(PHONE_REGEX, 'Payer phone must contain at least 7 valid digits/symbols.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerType: z
      .string()
      .trim()
      .max(64, 'Payer type cannot exceed 64 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerId: z
      .string()
      .uuid('Invalid payer ID format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    notes: z
      .string()
      .trim()
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      const hasStudent = !!data.studentId && data.studentId.trim().length > 0;
      const hasApplicant = !!data.applicantId && data.applicantId.trim().length > 0;
      const hasAccount = !!data.studentAccountId && data.studentAccountId.trim().length > 0;
      return hasStudent || hasApplicant || hasAccount;
    },
    {
      message: 'At least one candidate identifier (Student, Applicant, or Student Account) must be specified.',
      path: ['studentId'],
    },
  );

export type SubmitPaymentSchemaInput = z.infer<typeof submitPaymentSchema>;

/**
 * Typed VeeValidate schema for self-service payment submission form integration.
 */
export const submitPaymentTypedSchema = toTypedSchema(submitPaymentSchema);

/**
 * Zod validation schema for cashier counter recording with atomic confirmation (SCR-FIN-02, UC-FIN-04).
 */
export const recordPaymentSchema = z
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
    studentAccountId: z
      .string()
      .uuid('Invalid student account ID format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    amount: z.coerce
      .number({ required_error: 'Payment amount is required.' })
      .int('Payment amount must be an exact integer IDR value.')
      .positive('Payment amount must be strictly greater than zero IDR.'),
    paymentMethod: z
      .string({ required_error: 'Payment method is required.' })
      .trim()
      .min(1, 'Payment method is required.')
      .max(64, 'Payment method cannot exceed 64 characters.'),
    paymentDate: z
      .string()
      .regex(ISO_DATE_REGEX, 'Payment date must follow YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    externalReference: z
      .string()
      .trim()
      .max(100, 'External reference cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerName: z
      .string({ required_error: 'Payer name is required.' })
      .trim()
      .min(1, 'Payer name is required.')
      .max(200, 'Payer name cannot exceed 200 characters.'),
    payerIdentificationNumber: z
      .string()
      .trim()
      .max(64, 'Identification number cannot exceed 64 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerEmail: z
      .string()
      .trim()
      .email('Payer email is not a valid email address.')
      .max(200, 'Payer email cannot exceed 200 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerPhone: z
      .string()
      .trim()
      .regex(PHONE_REGEX, 'Payer phone must contain at least 7 valid digits/symbols.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerType: z
      .string()
      .trim()
      .max(64, 'Payer type cannot exceed 64 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    payerId: z
      .string()
      .uuid('Invalid payer ID format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    notes: z
      .string()
      .trim()
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      const hasStudent = !!data.studentId && data.studentId.trim().length > 0;
      const hasApplicant = !!data.applicantId && data.applicantId.trim().length > 0;
      const hasAccount = !!data.studentAccountId && data.studentAccountId.trim().length > 0;
      return hasStudent || hasApplicant || hasAccount;
    },
    {
      message: 'At least one candidate identifier (Student, Applicant, or Student Account) must be specified.',
      path: ['studentId'],
    },
  );

export type RecordPaymentSchemaInput = z.infer<typeof recordPaymentSchema>;

/**
 * Typed VeeValidate schema for cashier counter recording form integration.
 */
export const recordPaymentTypedSchema = toTypedSchema(recordPaymentSchema);

/**
 * Zod validation schema for confirming a payment in the verification queue.
 */
export const confirmPaymentSchema = z.object({
  paymentId: z
    .string({ required_error: 'Payment ID is required.' })
    .uuid('Invalid payment ID format.'),
});

export type ConfirmPaymentSchemaInput = z.infer<typeof confirmPaymentSchema>;

/**
 * Typed VeeValidate schema for confirm payment action.
 */
export const confirmPaymentTypedSchema = toTypedSchema(confirmPaymentSchema);

/**
 * Zod validation schema for cashier rejection of an invalid payment submission.
 */
export const rejectPaymentSchema = z.object({
  rejectionReason: z
    .string({ required_error: 'Rejection reason is mandatory.' })
    .trim()
    .min(1, 'Rejection reason is mandatory.')
    .max(1000, 'Rejection reason cannot exceed 1000 characters.'),
  notes: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type RejectPaymentSchemaInput = z.infer<typeof rejectPaymentSchema>;

/**
 * Typed VeeValidate schema for reject payment modal form integration.
 */
export const rejectPaymentTypedSchema = toTypedSchema(rejectPaymentSchema);

/**
 * Zod validation schema for querying payments and verification queue worklists.
 */
export const getPaymentsParamsSchema = z.object({
  status: z.string().trim().optional(),
  studentId: z.string().uuid().optional(),
  applicantId: z.string().uuid().optional(),
  payerId: z.string().uuid().optional(),
  startDate: z.string().regex(ISO_DATE_REGEX).optional(),
  endDate: z.string().regex(ISO_DATE_REGEX).optional(),
  searchTerm: z.string().trim().optional(),
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(1).max(100).default(20),
});

export type GetPaymentsParamsSchemaInput = z.infer<typeof getPaymentsParamsSchema>;

/**
 * Typed VeeValidate schema for payments queue query filters.
 */
export const getPaymentsParamsTypedSchema = toTypedSchema(getPaymentsParamsSchema);
