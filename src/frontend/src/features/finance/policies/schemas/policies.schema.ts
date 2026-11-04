import { z } from 'zod';

export const POLICY_CATEGORIES = [
  'tuition',
  'admission',
  'facilities',
  'activities',
  'general',
] as const;

export const RECURRENCE_PATTERNS = [
  'one_time',
  'monthly',
  'term',
  'annual',
] as const;

export const DUE_RULE_TYPES = [
  'days_after_issuance',
  'fixed_day_of_month',
  'fixed_calendar_date',
] as const;

export const DETERMINATION_METHODS = [
  'fixed_amount',
  'custom_formula',
] as const;

const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Zod validation schema for creating a Financial Policy draft (GAP-006, TD-06).
 */
export const createFinancialPolicySchema = z
  .object({
    policyCode: z
      .string({ required_error: 'Policy code is required.' })
      .trim()
      .min(1, 'Policy code is required.')
      .max(64, 'Policy code cannot exceed 64 characters.')
      .regex(/^[A-Za-z0-9_-]+$/, 'Policy code must follow format (e.g. FP-YYYY-NNN) and contain only alphanumeric characters or hyphens.'),
    name: z
      .string({ required_error: 'Policy name is required.' })
      .trim()
      .min(1, 'Policy name is required.')
      .max(200, 'Policy name cannot exceed 200 characters.'),
    category: z
      .string({ required_error: 'Category is required.' })
      .trim()
      .min(1, 'Category is required.')
      .max(64, 'Category cannot exceed 64 characters.'),
    businessPurpose: z
      .string({ required_error: 'Business purpose is required.' })
      .trim()
      .min(1, 'Business purpose is required.')
      .max(1000, 'Business purpose cannot exceed 1000 characters.'),
    applicableContext: z
      .string()
      .trim()
      .max(200, 'Applicable context cannot exceed 200 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    effectiveDate: z
      .string({ required_error: 'Effective date is required.' })
      .regex(ISO_DATE_REGEX, 'Effective date must follow YYYY-MM-DD format.'),
    endDate: z
      .string()
      .regex(ISO_DATE_REGEX, 'End date must follow YYYY-MM-DD format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    policyVersion: z
      .string()
      .trim()
      .max(32, 'Policy version cannot exceed 32 characters.')
      .default('1.0'),
    supersedesPolicyId: z
      .string()
      .uuid('Supersedes policy ID must be a valid UUID.')
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
      if (!data.endDate || data.endDate === '') return true;
      return data.effectiveDate <= data.endDate;
    },
    {
      message: 'End date must be greater than or equal to effective date.',
      path: ['endDate'],
    },
  );

export type CreateFinancialPolicySchemaInput = z.infer<typeof createFinancialPolicySchema>;

/**
 * Zod validation schema for creating a Charge Rule.
 */
export const createChargeRuleSchema = z.object({
  ruleCode: z
    .string({ required_error: 'Rule code is required.' })
    .trim()
    .min(1, 'Rule code is required.')
    .max(64, 'Rule code cannot exceed 64 characters.')
    .regex(/^[A-Za-z0-9_-]+$/, 'Rule code must contain only letters, numbers, underscores, and dashes.'),
  name: z
    .string({ required_error: 'Rule name is required.' })
    .trim()
    .min(1, 'Rule name is required.')
    .max(200, 'Rule name cannot exceed 200 characters.'),
  chargeType: z
    .string({ required_error: 'Charge type is required.' })
    .trim()
    .min(1, 'Charge type is required.')
    .max(64, 'Charge type cannot exceed 64 characters.'),
  baseAmount: z
    .number({ required_error: 'Base amount is required.' })
    .int('Base amount must be an exact integer IDR value.')
    .min(0, 'Base amount cannot be negative.'),
  currencyCode: z
    .string()
    .trim()
    .length(3, 'Currency code must be 3 characters.')
    .default('IDR'),
  recurrencePattern: z.enum(RECURRENCE_PATTERNS, {
    errorMap: () => ({ message: 'Recurrence pattern must be one of: one_time, monthly, term, annual.' }),
  }),
  dueRuleType: z.enum(DUE_RULE_TYPES, {
    errorMap: () => ({ message: 'Due rule type must be one of: days_after_issuance, fixed_day_of_month, fixed_calendar_date.' }),
  }),
  dueRuleValue: z
    .string({ required_error: 'Due rule value is required.' })
    .trim()
    .min(1, 'Due rule value is required.')
    .max(64, 'Due rule value cannot exceed 64 characters.'),
  isMandatory: z.boolean().default(true),
  determinationMethod: z
    .string()
    .trim()
    .default('fixed_amount'),
  applicableGrade: z
    .string()
    .trim()
    .max(32, 'Applicable grade cannot exceed 32 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  applicableProgram: z
    .string()
    .trim()
    .max(64, 'Applicable program cannot exceed 64 characters.')
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

export type CreateChargeRuleSchemaInput = z.infer<typeof createChargeRuleSchema>;

/**
 * Zod validation schema for formally approving a Financial Policy.
 */
export const approveFinancialPolicySchema = z.object({
  approvingAuthority: z
    .string()
    .trim()
    .max(100, 'Approving authority cannot exceed 100 characters.')
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
});

export type ApproveFinancialPolicySchemaInput = z.infer<typeof approveFinancialPolicySchema>;
