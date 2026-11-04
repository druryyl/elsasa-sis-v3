import { z } from 'zod';
import { toTypedSchema } from '@vee-validate/zod';

const PHONE_REGEX = /^[+0-9\s\-()]{7,30}$/;

/**
 * Zod validation schema for creating a new payer entity (TD-03, SCR-FIN-02).
 */
export const createPayerSchema = z.object({
  name: z
    .string({ required_error: 'Payer name is required.' })
    .trim()
    .min(1, 'Payer name is required.')
    .max(200, 'Payer name cannot exceed 200 characters.'),
  type: z
    .string()
    .trim()
    .max(64, 'Payer type cannot exceed 64 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  identificationNumber: z
    .string()
    .trim()
    .max(64, 'Identification number cannot exceed 64 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  email: z
    .string()
    .trim()
    .email('Email address is not in a valid format.')
    .max(200, 'Email cannot exceed 200 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  phone: z
    .string()
    .trim()
    .regex(PHONE_REGEX, 'Phone number must contain at least 7 valid digits/symbols.')
    .optional()
    .nullable()
    .or(z.literal('')),
  billingAddress: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
  bankAccountName: z
    .string()
    .trim()
    .max(200, 'Bank account name cannot exceed 200 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  bankAccountNumber: z
    .string()
    .trim()
    .max(100, 'Bank account number cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  bankName: z
    .string()
    .trim()
    .max(100, 'Bank name cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  notes: z
    .string()
    .trim()
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type CreatePayerSchemaInput = z.infer<typeof createPayerSchema>;

/**
 * Typed VeeValidate schema for create payer form integration.
 */
export const createPayerTypedSchema = toTypedSchema(createPayerSchema);

/**
 * Zod validation schema for searching and filtering payers.
 */
export const searchPayersSchema = z.object({
  searchTerm: z.string().trim().optional(),
  payerType: z.string().trim().optional(),
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(1).max(100).default(20),
});

export type SearchPayersSchemaInput = z.infer<typeof searchPayersSchema>;

/**
 * Typed VeeValidate schema for search payers form integration.
 */
export const searchPayersTypedSchema = toTypedSchema(searchPayersSchema);
