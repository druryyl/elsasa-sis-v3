import { z } from 'zod';
import {
  ASSESSMENT_METHODS,
  ASSESSMENT_TYPES,
  EVALUATION_MODELS,
  EVIDENCE_TYPES,
  REASSESSMENT_POLICIES,
} from '../types';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

export function isValidCalendarDate(val: string): boolean {
  if (!ISO_DATE_REGEX.test(val)) return false;
  const parts = val.split('-').map(Number);
  const year = parts[0];
  const month = parts[1];
  const day = parts[2];
  if (month < 1 || month > 12) return false;
  const date = new Date(Date.UTC(year, month - 1, day));
  return (
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day
  );
}

// ============================================================================
// Create Assessment Schema (P8-S19, UC-ACD-05)
// ============================================================================

export const createAssessmentSchema = z
  .object({
    learningUnitId: z
      .string({ required_error: 'Learning unit is required.' })
      .trim()
      .min(1, 'Learning unit is required.'),
    learningActivityId: z.string().trim().optional().nullable().or(z.literal('')),
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(3, 'Title must be at least 3 characters.')
      .max(200, 'Title cannot exceed 200 characters.'),
    assessmentType: z.enum(ASSESSMENT_TYPES as unknown as [string, ...string[]], {
      required_error: 'Assessment type is required.',
    }),
    assessmentMethod: z.enum(ASSESSMENT_METHODS as unknown as [string, ...string[]], {
      required_error: 'Assessment method is required.',
    }),
    reassessmentPolicy: z
      .enum(REASSESSMENT_POLICIES as unknown as [string, ...string[]])
      .default('none'),
    maxScore: z
      .number({ invalid_type_error: 'Max score must be a number.' })
      .positive('Max score must be greater than zero.')
      .optional()
      .nullable(),
    passingScore: z
      .number({ invalid_type_error: 'Passing score must be a number.' })
      .min(0, 'Passing score cannot be negative.')
      .optional()
      .nullable(),
    criteria: z
      .string()
      .trim()
      .max(2000, 'Criteria cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    assessmentDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Assessment date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Assessment date must be a valid calendar date.',
      })
      .optional()
      .nullable()
      .or(z.literal('')),
    assessorReference: z
      .string()
      .trim()
      .max(200, 'Assessor reference cannot exceed 200 characters.')
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
      if (
        data.maxScore !== undefined &&
        data.maxScore !== null &&
        data.passingScore !== undefined &&
        data.passingScore !== null
      ) {
        return data.passingScore <= data.maxScore;
      }
      return true;
    },
    {
      message: 'Passing score must be less than or equal to maximum score.',
      path: ['passingScore'],
    },
  );

export type CreateAssessmentFormValues = z.infer<typeof createAssessmentSchema>;

// ============================================================================
// Update Assessment Schema
// ============================================================================

export const updateAssessmentSchema = z
  .object({
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(3, 'Title must be at least 3 characters.')
      .max(200, 'Title cannot exceed 200 characters.'),
    criteria: z
      .string()
      .trim()
      .max(2000, 'Criteria cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    maxScore: z
      .number({ invalid_type_error: 'Max score must be a number.' })
      .positive('Max score must be greater than zero.')
      .optional()
      .nullable(),
    passingScore: z
      .number({ invalid_type_error: 'Passing score must be a number.' })
      .min(0, 'Passing score cannot be negative.')
      .optional()
      .nullable(),
    reassessmentPolicy: z
      .enum(REASSESSMENT_POLICIES as unknown as [string, ...string[]])
      .default('none'),
    assessmentDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Assessment date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Assessment date must be a valid calendar date.',
      })
      .optional()
      .nullable()
      .or(z.literal('')),
    assessorReference: z
      .string()
      .trim()
      .max(200, 'Assessor reference cannot exceed 200 characters.')
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
      if (
        data.maxScore !== undefined &&
        data.maxScore !== null &&
        data.passingScore !== undefined &&
        data.passingScore !== null
      ) {
        return data.passingScore <= data.maxScore;
      }
      return true;
    },
    {
      message: 'Passing score must be less than or equal to maximum score.',
      path: ['passingScore'],
    },
  );

export type UpdateAssessmentFormValues = z.infer<typeof updateAssessmentSchema>;

// ============================================================================
// Record Evidence Schema (P8-S19, UC-ACD-07, GAP-003)
// ============================================================================

export const recordEvidenceSchema = z
  .object({
    studentId: z.string().trim().optional().nullable().or(z.literal('')),
    applicantId: z.string().trim().optional().nullable().or(z.literal('')),
    submissionId: z.string().trim().optional().nullable().or(z.literal('')),
    evidenceType: z.enum(EVIDENCE_TYPES as unknown as [string, ...string[]], {
      required_error: 'Evidence type is required.',
    }),
    collectedDate: z
      .string({ required_error: 'Collected date is required.' })
      .trim()
      .regex(ISO_DATE_REGEX, 'Collected date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Collected date must be a valid calendar date.',
      }),
    evidenceReference: z
      .string()
      .trim()
      .max(500, 'Evidence reference cannot exceed 500 characters.')
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
      const hasStudent = Boolean(data.studentId && data.studentId.trim().length > 0);
      const hasApplicant = Boolean(data.applicantId && data.applicantId.trim().length > 0);
      // Candidate selection must be exclusive XOR: exactly one
      return (hasStudent && !hasApplicant) || (!hasStudent && hasApplicant);
    },
    {
      message: 'Candidate selection is required: specify either Student or Applicant, but not both.',
      path: ['studentId'],
    },
  );

export type RecordEvidenceFormValues = z.infer<typeof recordEvidenceSchema>;

// ============================================================================
// Evaluate Learning Schema (P8-S19, UC-ACD-06, GAP-005)
// ============================================================================

export const evaluateLearningSchema = z
  .object({
    studentId: z.string().trim().optional().nullable().or(z.literal('')),
    applicantId: z.string().trim().optional().nullable().or(z.literal('')),
    scoreNumeric: z
      .number({ invalid_type_error: 'Score must be a number.' })
      .min(0, 'Score cannot be negative.')
      .optional()
      .nullable(),
    scoreMax: z
      .number({ invalid_type_error: 'Max score must be a number.' })
      .positive('Max score must be positive.')
      .optional()
      .nullable(),
    gradeLetter: z
      .string()
      .trim()
      .max(10, 'Grade letter cannot exceed 10 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    gradeLabel: z
      .string()
      .trim()
      .max(100, 'Grade label cannot exceed 100 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    narrativeFeedback: z
      .string()
      .trim()
      .max(4000, 'Feedback cannot exceed 4000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    evaluationModel: z.enum(EVALUATION_MODELS as unknown as [string, ...string[]], {
      required_error: 'Evaluation model is required.',
    }),
    publishImmediately: z.boolean().default(false).optional(),
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
      const hasStudent = Boolean(data.studentId && data.studentId.trim().length > 0);
      const hasApplicant = Boolean(data.applicantId && data.applicantId.trim().length > 0);
      return (hasStudent && !hasApplicant) || (!hasStudent && hasApplicant);
    },
    {
      message: 'Specify either Student or Applicant for evaluation, but not both.',
      path: ['studentId'],
    },
  )
  .refine(
    (data) => {
      if (data.evaluationModel === 'numeric') {
        return data.scoreNumeric !== undefined && data.scoreNumeric !== null;
      }
      return true;
    },
    {
      message: 'Numeric score is required when evaluation model is numeric.',
      path: ['scoreNumeric'],
    },
  )
  .refine(
    (data) => {
      if (
        data.scoreNumeric !== undefined &&
        data.scoreNumeric !== null &&
        data.scoreMax !== undefined &&
        data.scoreMax !== null
      ) {
        return data.scoreNumeric <= data.scoreMax;
      }
      return true;
    },
    {
      message: 'Score cannot exceed maximum possible score.',
      path: ['scoreNumeric'],
    },
  );

export type EvaluateLearningFormValues = z.infer<typeof evaluateLearningSchema>;
