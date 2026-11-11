import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

export const WORKLOAD_COMMITMENT_STATUSES = [
  'normal',
  'under_allocated',
  'approaching_limit',
  'overloaded_with_exception',
  'breached',
] as const;

export const WORKLOAD_EXCEPTION_STATUSES = [
  'requested',
  'recommended',
  'approved',
  'rejected',
] as const;

/**
 * Zod validation schema for submitting a workload exception recommendation (P7-S22, P3-S10, SCR-TCH-03).
 * Aligns strictly with RequestWorkloadExceptionValidator on backend.
 */
export const requestExceptionSchema = z
  .object({
    teacherProfileId: z
      .string()
      .min(1, 'Teacher profile ID is required.')
      .uuid('Teacher profile ID must be a valid UUID.'),
    academicPeriodId: z
      .string()
      .min(1, 'Academic period ID is required.')
      .uuid('Academic period ID must be a valid UUID.'),
    teachingAssignmentId: z
      .string()
      .uuid('Teaching assignment ID must be a valid UUID.')
      .nullish()
      .transform((val) => val ?? undefined),
    requestedOverloadHours: z.coerce
      .number({ invalid_type_error: 'Requested overload hours must be a number.' })
      .positive('Requested overload hours must be greater than zero.')
      .max(80, 'Requested overload hours cannot exceed 80 hours per week.'),
    coordinatorJustification: z
      .string()
      .min(1, 'Coordinator justification is required.')
      .max(2000, 'Coordinator justification cannot exceed 2000 characters.'),
    effectiveStartDate: z
      .string()
      .regex(ISO_DATE_REGEX, 'Effective start date must be in YYYY-MM-DD format.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    effectiveEndDate: z
      .string()
      .regex(ISO_DATE_REGEX, 'Effective end date must be in YYYY-MM-DD format.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    status: z
      .enum(['requested', 'recommended'])
      .nullish()
      .transform((val) => val ?? 'requested'),
  })
  .refine(
    (data) => {
      if (!data.effectiveStartDate || !data.effectiveEndDate) return true;
      return data.effectiveEndDate >= data.effectiveStartDate;
    },
    {
      message: 'Effective end date cannot precede effective start date.',
      path: ['effectiveEndDate'],
    },
  );

export type RequestExceptionFormValues = z.infer<typeof requestExceptionSchema>;

/**
 * Zod validation schema for approving a workload exception (P7-S22, P3-S10, SCR-TCH-03).
 * Aligns with ApproveWorkloadExceptionRequest on backend.
 */
export const approveExceptionSchema = z.object({
  decisionRemarks: z
    .string()
    .max(2000, 'Decision remarks cannot exceed 2000 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
});

export type ApproveExceptionFormValues = z.infer<typeof approveExceptionSchema>;

/**
 * Zod validation schema for period workload recalculation request (P7-S22, P3-S09).
 */
export const recalculateWorkloadSchema = z.object({
  academicPeriodId: z
    .string()
    .min(1, 'Academic period ID is required.')
    .uuid('Academic period ID must be a valid UUID.'),
});

export type RecalculateWorkloadFormValues = z.infer<typeof recalculateWorkloadSchema>;
