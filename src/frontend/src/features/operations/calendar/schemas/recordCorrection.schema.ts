import { z } from 'zod';

export const CORRECTION_TARGET_TABLES = ['academic_years', 'operational_periods'] as const;

export type CorrectionTargetTable = (typeof CORRECTION_TARGET_TABLES)[number];

/**
 * Validation schema for submitting an auditable record correction (UC-OPS-13).
 */
export const recordCorrectionSchema = z.object({
  academicYearId: z.string().uuid('Academic Year ID must be a valid UUID.').optional(),
  targetTableName: z.enum(CORRECTION_TARGET_TABLES, {
    errorMap: () => ({ message: 'Target table must be academic_years or operational_periods.' }),
  }),
  targetRecordId: z
    .string({ required_error: 'Target record ID is required.' })
    .uuid('Target record ID must be a valid UUID.'),
  fieldName: z
    .string({ required_error: 'Field name is required.' })
    .trim()
    .min(1, 'Field name is required.')
    .max(64, 'Field name cannot exceed 64 characters.'),
  newValue: z
    .string({ required_error: 'New value is required.' })
    .max(2000, 'New value cannot exceed 2000 characters.'),
  justificationReason: z
    .string({ required_error: 'Justification reason is required.' })
    .trim()
    .min(1, 'Justification reason is required.')
    .min(5, 'Justification reason must be at least 5 characters.')
    .max(1000, 'Justification reason cannot exceed 1000 characters.'),
  authorizedBy: z.string().max(100, 'Authorized by cannot exceed 100 characters.' ).optional().nullable(),
});

export type RecordCorrectionFormData = z.infer<typeof recordCorrectionSchema>;
