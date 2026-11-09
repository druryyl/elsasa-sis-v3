import { z } from 'zod';
import {
  ISO_DATE_REGEX,
  isValidCalendarDate,
} from '@/features/academics/assessment/schemas';
import {
  REPORT_TYPES,
  TRANSCRIPT_TYPES,
} from '../types';

// ============================================================================
// Batch Compile Report Cards Schema (P8-S19, TD-07, UC-ACD-13)
// ============================================================================

export const batchCompileReportCardsSchema = z.object({
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .trim()
    .min(1, 'Academic period is required.'),
  academicPeriodLabel: z
    .string()
    .trim()
    .max(100, 'Period label cannot exceed 100 characters.')
    .optional()
    .default(''),
  reportType: z.enum(REPORT_TYPES as unknown as [string, ...string[]], {
    required_error: 'Report type is required.',
  }),
  templateName: z
    .string({ required_error: 'Template name is required.' })
    .trim()
    .min(1, 'Template name is required.')
    .max(100, 'Template name cannot exceed 100 characters.')
    .default('Buku Rapor Merdeka'),
  sectionLabel: z
    .string()
    .trim()
    .max(50, 'Section label cannot exceed 50 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type BatchCompileReportCardsFormValues = z.infer<
  typeof batchCompileReportCardsSchema
>;

// ============================================================================
// Single Compile Report Card Schema (UC-ACD-13)
// ============================================================================

export const compileReportCardSchema = z.object({
  studentId: z
    .string({ required_error: 'Student is required.' })
    .trim()
    .min(1, 'Student is required.'),
  academicPeriodId: z
    .string({ required_error: 'Academic period is required.' })
    .trim()
    .min(1, 'Academic period is required.'),
  academicPeriodLabel: z
    .string({ required_error: 'Academic period label is required.' })
    .trim()
    .min(1, 'Academic period label is required.')
    .max(100, 'Period label cannot exceed 100 characters.'),
  reportType: z.enum(REPORT_TYPES as unknown as [string, ...string[]], {
    required_error: 'Report type is required.',
  }),
  templateName: z
    .string({ required_error: 'Template name is required.' })
    .trim()
    .min(1, 'Template name is required.')
    .max(100, 'Template name cannot exceed 100 characters.')
    .default('Buku Rapor Merdeka'),
  narrativeComments: z
    .string()
    .trim()
    .max(4000, 'Narrative comments cannot exceed 4000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type CompileReportCardFormValues = z.infer<
  typeof compileReportCardSchema
>;

// ============================================================================
// Correct Result Schema (P8-S19, UC-ACD-19, TD-06)
// ============================================================================

export const correctResultSchema = z
  .object({
    score_numeric: z
      .number({ invalid_type_error: 'Score must be a number.' })
      .min(0, 'Score cannot be negative.')
      .optional()
      .nullable(),
    score_max: z
      .number({ invalid_type_error: 'Max score must be a number.' })
      .positive('Max score must be positive.')
      .optional()
      .nullable(),
    grade_letter: z
      .string()
      .trim()
      .max(10, 'Grade letter cannot exceed 10 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    grade_label: z
      .string()
      .trim()
      .max(100, 'Grade label cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    narrative_feedback: z
      .string()
      .trim()
      .max(4000, 'Narrative feedback cannot exceed 4000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    reason: z
      .string({ required_error: 'Justification reason is required.' })
      .trim()
      .min(10, 'Reason must be at least 10 characters long.')
      .max(1000, 'Reason cannot exceed 1000 characters.'),
    evidence: z
      .string()
      .trim()
      .max(500, 'Evidence cannot exceed 500 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    is_material: z.boolean().default(false),
  })
  .refine(
    (data) => {
      const hasScore = data.score_numeric !== undefined && data.score_numeric !== null;
      const hasScoreMax = data.score_max !== undefined && data.score_max !== null;
      const hasLetter = Boolean(data.grade_letter && data.grade_letter.trim().length > 0);
      const hasLabel = Boolean(data.grade_label && data.grade_label.trim().length > 0);
      const hasFeedback = Boolean(data.narrative_feedback && data.narrative_feedback.trim().length > 0);
      return hasScore || hasScoreMax || hasLetter || hasLabel || hasFeedback;
    },
    {
      message: 'At least one corrected value field (score, grade letter, grade label, feedback) must be provided.',
      path: ['score_numeric'],
    },
  )
  .refine(
    (data) => {
      if (
        data.score_numeric !== undefined &&
        data.score_numeric !== null &&
        data.score_max !== undefined &&
        data.score_max !== null
      ) {
        return data.score_numeric <= data.score_max;
      }
      return true;
    },
    {
      message: 'Score cannot exceed maximum possible score.',
      path: ['score_numeric'],
    },
  );

export type CorrectResultFormValues = z.infer<typeof correctResultSchema>;

// ============================================================================
// Generate Interim Transcript Schema (UC-ACD-13, P5-S13)
// ============================================================================

export const generateTranscriptSchema = z
  .object({
    studentId: z
      .string({ required_error: 'Student is required.' })
      .trim()
      .min(1, 'Student is required.'),
    transcriptType: z.enum(TRANSCRIPT_TYPES as unknown as [string, ...string[]], {
      required_error: 'Transcript type is required.',
    }),
    periodStart: z
      .string({ required_error: 'Period start date is required.' })
      .trim()
      .regex(ISO_DATE_REGEX, 'Period start date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Period start date must be a valid calendar date.',
      }),
    periodEnd: z
      .string({ required_error: 'Period end date is required.' })
      .trim()
      .regex(ISO_DATE_REGEX, 'Period end date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Period end date must be a valid calendar date.',
      }),
    purpose: z
      .string()
      .trim()
      .max(200, 'Purpose cannot exceed 200 characters.')
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
    (data) => data.periodStart <= data.periodEnd,
    {
      message: 'Period end date must be on or after period start date.',
      path: ['periodEnd'],
    },
  );

export type GenerateTranscriptFormValues = z.infer<
  typeof generateTranscriptSchema
>;
