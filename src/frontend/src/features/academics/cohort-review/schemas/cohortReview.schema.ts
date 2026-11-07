import { z } from 'zod';
import { isValidCalendarDate } from '@/features/academics/assessment/schemas';
import {
  ACADEMIC_STANDINGS,
  ACHIEVEMENT_TYPES,
  PROMOTION_RECOMMENDATIONS,
} from '../types';

// ============================================================================
// Standing Override / Exception Schema (SCR-ACD-09, UC-ACD-17, P7-S19)
// ============================================================================

export const standingOverrideSchema = z
  .object({
    standing: z.enum(ACADEMIC_STANDINGS as unknown as [string, ...string[]], {
      required_error: 'Standing determination is required.',
    }),
    policyReference: z
      .string({ required_error: 'Policy reference is required.' })
      .trim()
      .min(1, 'Policy reference is required.')
      .max(100, 'Policy reference cannot exceed 100 characters.'),
    conditions: z
      .string()
      .trim()
      .max(2000, 'Conditions cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    isException: z.boolean().default(false),
    exceptionReason: z
      .string()
      .trim()
      .max(1000, 'Exception reason cannot exceed 1000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    committeeReference: z
      .string()
      .trim()
      .max(100, 'Committee reference cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .superRefine((data, ctx) => {
    if (
      data.standing === 'conditional_standing' &&
      (!data.conditions || data.conditions.trim().length === 0)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Conditions are mandatory when standing is conditional.',
        path: ['conditions'],
      });
    }

    if (data.isException) {
      if (!data.exceptionReason || data.exceptionReason.trim().length < 5) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message:
            'A descriptive exception reason (min 5 characters) is required for exceptions.',
          path: ['exceptionReason'],
        });
      }
      if (!data.committeeReference || data.committeeReference.trim().length === 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message:
            'Committee reference (e.g. SK Dewan Guru decree) is required for exceptions.',
          path: ['committeeReference'],
        });
      }
    }
  });

export type StandingOverrideFormValues = z.infer<typeof standingOverrideSchema>;

// ============================================================================
// Progression Override / Exception Schema (SCR-ACD-09, UC-ACD-17, P7-S19)
// ============================================================================

export const progressionOverrideSchema = z
  .object({
    recommendation: z.enum(
      PROMOTION_RECOMMENDATIONS as unknown as [string, ...string[]],
      {
        required_error: 'Progression recommendation is required.',
      },
    ),
    fromLevel: z
      .string({ required_error: 'Current level is required.' })
      .trim()
      .min(1, 'Current level is required.')
      .max(50, 'Current level cannot exceed 50 characters.'),
    recommendedToLevel: z
      .string()
      .trim()
      .max(50, 'Recommended level cannot exceed 50 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    conditions: z
      .string()
      .trim()
      .max(2000, 'Conditions cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    supportingEvidence: z
      .string({ required_error: 'Supporting evidence is required.' })
      .trim()
      .min(1, 'Supporting evidence summary is required.')
      .max(2000, 'Supporting evidence cannot exceed 2000 characters.'),
    isException: z.boolean().default(false),
    exceptionReason: z
      .string()
      .trim()
      .max(1000, 'Exception reason cannot exceed 1000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    committeeReference: z
      .string()
      .trim()
      .max(100, 'Committee reference cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .superRefine((data, ctx) => {
    if (
      data.recommendation === 'conditional_promotion' &&
      (!data.conditions || data.conditions.trim().length === 0)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message:
          'Stipulated conditions are mandatory for conditional promotion.',
        path: ['conditions'],
      });
    }

    if (
      data.recommendation === 'promotion_eligible' &&
      (!data.recommendedToLevel || data.recommendedToLevel.trim().length === 0)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Destination level is required when eligible for promotion.',
        path: ['recommendedToLevel'],
      });
    }

    if (data.isException) {
      if (!data.exceptionReason || data.exceptionReason.trim().length < 5) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message:
            'A descriptive exception reason (min 5 characters) is required for exceptions.',
          path: ['exceptionReason'],
        });
      }
      if (!data.committeeReference || data.committeeReference.trim().length === 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message:
            'Committee reference (e.g. SK Dewan Guru decree) is required for exceptions.',
          path: ['committeeReference'],
        });
      }
    }
  });

export type ProgressionOverrideFormValues = z.infer<
  typeof progressionOverrideSchema
>;

// ============================================================================
// Batch Standing & Progression Commitment Schemas (SCR-ACD-09, TD-03)
// ============================================================================

export const singleStandingCommitItemSchema = z.object({
  id: z.string().uuid().optional(),
  studentId: z.string().uuid('Invalid student ID.'),
  academicPeriodId: z.string().uuid('Invalid academic period ID.'),
  standing: z.enum(ACADEMIC_STANDINGS as unknown as [string, ...string[]]),
  policyReference: z.string().trim().min(1).max(100),
  conditions: z.string().max(2000).optional().nullable(),
  isException: z.boolean().default(false),
  exceptionReason: z.string().max(1000).optional().nullable(),
  committeeReference: z.string().max(100).optional().nullable(),
});

export const batchDetermineStandingSchema = z.object({
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .uuid('Academic period ID must be a valid UUID.'),
  items: z
    .array(singleStandingCommitItemSchema)
    .min(1, 'At least one student standing determination is required.'),
});

export type BatchDetermineStandingFormValues = z.infer<
  typeof batchDetermineStandingSchema
>;

export const singleProgressionCommitItemSchema = z.object({
  id: z.string().uuid().optional(),
  studentId: z.string().uuid('Invalid student ID.'),
  academicPeriodId: z.string().uuid('Invalid academic period ID.'),
  recommendation: z.enum(
    PROMOTION_RECOMMENDATIONS as unknown as [string, ...string[]],
  ),
  fromLevel: z.string().trim().min(1).max(50),
  recommendedToLevel: z.string().trim().max(50).optional().nullable(),
  conditions: z.string().max(2000).optional().nullable(),
  supportingEvidence: z.string().trim().min(1).max(2000),
  academicStandingId: z.string().uuid().optional().nullable(),
  isException: z.boolean().default(false),
  exceptionReason: z.string().max(1000).optional().nullable(),
  committeeReference: z.string().max(100).optional().nullable(),
});

export const batchIssueProgressionSchema = z.object({
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .uuid('Academic period ID must be a valid UUID.'),
  items: z
    .array(singleProgressionCommitItemSchema)
    .min(1, 'At least one progression recommendation is required.'),
});

export type BatchIssueProgressionFormValues = z.infer<
  typeof batchIssueProgressionSchema
>;

// ============================================================================
// Academic Achievement Conferral Schema (SCR-ACD-09, UC-ACD-21, P7-S19)
// ============================================================================

export const conferAchievementSchema = z.object({
  studentId: z
    .string({ required_error: 'Student is required.' })
    .uuid('Student ID must be a valid UUID.'),
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .uuid('Academic period ID must be a valid UUID.'),
  learningUnitId: z
    .string()
    .uuid('Learning unit ID must be a valid UUID.')
    .optional()
    .nullable()
    .or(z.literal('')),
  achievementType: z.enum(
    ACHIEVEMENT_TYPES as unknown as [string, ...string[]],
    {
      required_error: 'Achievement type is required.',
    },
  ),
  title: z
    .string({ required_error: 'Title is required.' })
    .trim()
    .min(3, 'Title must be at least 3 characters.')
    .max(200, 'Title cannot exceed 200 characters.'),
  description: z
    .string()
    .trim()
    .max(1000, 'Description cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  criteriaMet: z
    .string({ required_error: 'Criteria met summary is required.' })
    .trim()
    .min(3, 'Criteria met summary must be at least 3 characters.')
    .max(1000, 'Criteria met summary cannot exceed 1000 characters.'),
  supportingEvidence: z
    .string()
    .trim()
    .max(2000, 'Supporting evidence cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  recognizedAt: z
    .string({ required_error: 'Recognition date is required.' })
    .refine(
      isValidCalendarDate,
      'Recognition date must be a valid calendar date in YYYY-MM-DD format.',
    ),
});

export type ConferAchievementFormValues = z.infer<
  typeof conferAchievementSchema
>;

// ============================================================================
// Auditable Non-Grade Record Corrections Schemas (UC-ACD-15, TD-05, P7-S19)
// ============================================================================

export const correctStandingSchema = z.object({
  standing: z.enum(ACADEMIC_STANDINGS as unknown as [string, ...string[]], {
    required_error: 'Corrected standing is required.',
  }),
  policyReference: z
    .string()
    .trim()
    .max(100, 'Policy reference cannot exceed 100 characters.')
    .optional(),
  conditions: z
    .string()
    .trim()
    .max(2000, 'Conditions cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  isException: z.boolean().optional(),
  exceptionReason: z
    .string()
    .trim()
    .max(1000, 'Exception reason cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  committeeReference: z
    .string()
    .trim()
    .max(100, 'Committee reference cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  reason: z
    .string({ required_error: 'Correction reason is mandatory.' })
    .trim()
    .min(10, 'Correction reason must be at least 10 characters for audit compliance.')
    .max(1000, 'Correction reason cannot exceed 1000 characters.'),
});

export type CorrectStandingFormValues = z.infer<typeof correctStandingSchema>;

export const correctProgressionSchema = z.object({
  recommendation: z.enum(
    PROMOTION_RECOMMENDATIONS as unknown as [string, ...string[]],
    {
      required_error: 'Corrected progression recommendation is required.',
    },
  ),
  fromLevel: z
    .string()
    .trim()
    .max(50, 'From level cannot exceed 50 characters.')
    .optional(),
  recommendedToLevel: z
    .string()
    .trim()
    .max(50, 'Recommended level cannot exceed 50 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  conditions: z
    .string()
    .trim()
    .max(2000, 'Conditions cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  supportingEvidence: z
    .string()
    .trim()
    .max(2000, 'Supporting evidence cannot exceed 2000 characters.')
    .optional(),
  isException: z.boolean().optional(),
  exceptionReason: z
    .string()
    .trim()
    .max(1000, 'Exception reason cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  committeeReference: z
    .string()
    .trim()
    .max(100, 'Committee reference cannot exceed 100 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  reason: z
    .string({ required_error: 'Correction reason is mandatory.' })
    .trim()
    .min(10, 'Correction reason must be at least 10 characters for audit compliance.')
    .max(1000, 'Correction reason cannot exceed 1000 characters.'),
});

export type CorrectProgressionFormValues = z.infer<
  typeof correctProgressionSchema
>;
