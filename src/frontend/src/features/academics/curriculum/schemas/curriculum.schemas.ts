import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

export const LEARNING_UNIT_TYPES = [
  'subject',
  'course',
  'module',
  'pace',
  'project',
  'competency',
  'unit',
  'other',
] as const;

export const ACADEMIC_REQUIREMENT_SCOPES = [
  'program',
  'level',
  'learning_unit',
] as const;

export const ACADEMIC_REQUIREMENT_TYPES = [
  'completion',
  'prerequisite',
  'credit',
  'progression',
  'graduation',
  'assessment_policy',
] as const;

// ============================================================================
// Learning Structure Validation Schemas
// ============================================================================

/**
 * Validation schema for drafting a new Learning Structure framework.
 */
export const createLearningStructureSchema = z
  .object({
    name: z
      .string({ required_error: 'Name is required.' })
      .trim()
      .min(1, 'Name is required.')
      .max(200, 'Name cannot exceed 200 characters.'),
    academicPeriodId: z
      .string({ required_error: 'Academic period ID is required.' })
      .trim()
      .min(1, 'Academic period ID is required.'),
    version: z
      .string({ required_error: 'Version is required.' })
      .trim()
      .min(1, 'Version is required.')
      .max(50, 'Version cannot exceed 50 characters.'),
    effectiveDate: z
      .string({ required_error: 'Effective date is required.' })
      .trim()
      .min(1, 'Effective date is required.')
      .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.')
      .refine((val) => !Number.isNaN(Date.parse(val)), {
        message: 'Effective date must be a valid calendar date.',
      }),
    endDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.')
      .refine((val) => !Number.isNaN(Date.parse(val)), {
        message: 'End date must be a valid calendar date.',
      })
      .optional()
      .nullable(),
    description: z
      .string()
      .max(4000, 'Description cannot exceed 4000 characters.')
      .optional()
      .nullable(),
    notes: z
      .string()
      .max(4000, 'Notes cannot exceed 4000 characters.')
      .optional()
      .nullable(),
  })
  .refine(
    (data) => {
      if (!data.effectiveDate || !data.endDate) return true;
      return data.endDate >= data.effectiveDate;
    },
    {
      message: 'End date must be greater than or equal to effective date.',
      path: ['endDate'],
    },
  );

export type CreateLearningStructureFormData = z.infer<typeof createLearningStructureSchema>;

/**
 * Validation schema for updating Learning Structure metadata.
 */
export const updateLearningStructureSchema = z.object({
  name: z
    .string({ required_error: 'Name is required.' })
    .trim()
    .min(1, 'Name is required.')
    .max(200, 'Name cannot exceed 200 characters.'),
  description: z
    .string()
    .max(4000, 'Description cannot exceed 4000 characters.')
    .optional()
    .nullable(),
  notes: z
    .string()
    .max(4000, 'Notes cannot exceed 4000 characters.')
    .optional()
    .nullable(),
});

export type UpdateLearningStructureFormData = z.infer<typeof updateLearningStructureSchema>;

/**
 * Validation schema for branching an active structure into a new draft successor version.
 */
export const supersedeStructureSchema = z.object({
  newVersion: z
    .string({ required_error: 'New version string is required.' })
    .trim()
    .min(1, 'New version string is required.')
    .max(50, 'Version must not exceed 50 characters.'),
  effectiveDate: z
    .string({ required_error: 'Effective date is required.' })
    .trim()
    .min(1, 'Effective date is required.')
    .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.')
    .refine((val) => !Number.isNaN(Date.parse(val)), {
      message: 'Effective date must be a valid calendar date.',
    }),
  academicPeriodId: z
    .string()
    .trim()
    .optional()
    .nullable(),
  academicPeriodLabel: z
    .string()
    .max(100, 'Academic period label must not exceed 100 characters.')
    .optional()
    .nullable(),
});

export type SupersedeStructureFormData = z.infer<typeof supersedeStructureSchema>;

// ============================================================================
// Learning Unit Validation Schemas
// ============================================================================

/**
 * Validation schema for creating a learning unit within a learning structure.
 */
export const createLearningUnitSchema = z.object({
  code: z
    .string({ required_error: 'Unit code is required.' })
    .trim()
    .min(1, 'Unit code is required.')
    .max(50, 'Unit code cannot exceed 50 characters.'),
  name: z
    .string({ required_error: 'Unit name is required.' })
    .trim()
    .min(1, 'Unit name is required.')
    .max(200, 'Unit name cannot exceed 200 characters.'),
  unitType: z.enum(LEARNING_UNIT_TYPES, {
    errorMap: (issue) => {
      if (issue.code === 'invalid_type' && issue.received === 'undefined') {
        return { message: 'Unit type is required.' };
      }
      return {
        message:
          'Unit type must be one of: subject, course, module, pace, project, competency, unit, other.',
      };
    },
  }),
  parentUnitId: z
    .string()
    .trim()
    .optional()
    .nullable(),
  level: z
    .string()
    .max(50, 'Level cannot exceed 50 characters.')
    .optional()
    .nullable(),
  description: z.string().optional().nullable(),
  creditWeight: z
    .number()
    .min(0, 'Credit weight must be greater than or equal to 0.')
    .optional()
    .nullable(),
  notes: z.string().optional().nullable(),
});

export type CreateLearningUnitFormData = z.infer<typeof createLearningUnitSchema>;

/**
 * Validation schema for updating a learning unit.
 */
export const updateLearningUnitSchema = z.object({
  code: z
    .string({ required_error: 'Unit code is required.' })
    .trim()
    .min(1, 'Unit code is required.')
    .max(50, 'Unit code cannot exceed 50 characters.'),
  name: z
    .string({ required_error: 'Unit name is required.' })
    .trim()
    .min(1, 'Unit name is required.')
    .max(200, 'Unit name cannot exceed 200 characters.'),
  unitType: z.enum(LEARNING_UNIT_TYPES, {
    errorMap: (issue) => {
      if (issue.code === 'invalid_type' && issue.received === 'undefined') {
        return { message: 'Unit type is required.' };
      }
      return {
        message:
          'Unit type must be one of: subject, course, module, pace, project, competency, unit, other.',
      };
    },
  }),
  parentUnitId: z
    .string()
    .trim()
    .optional()
    .nullable(),
  level: z
    .string()
    .max(50, 'Level cannot exceed 50 characters.')
    .optional()
    .nullable(),
  description: z.string().optional().nullable(),
  creditWeight: z
    .number()
    .min(0, 'Credit weight must be greater than or equal to 0.')
    .optional()
    .nullable(),
  notes: z.string().optional().nullable(),
});

export type UpdateLearningUnitFormData = z.infer<typeof updateLearningUnitSchema>;

// ============================================================================
// Academic Requirement Validation Schemas
// ============================================================================

/**
 * Validation schema for drafting an academic requirement.
 */
export const createAcademicRequirementSchema = z
  .object({
    name: z
      .string({ required_error: 'Name is required.' })
      .trim()
      .min(1, 'Name is required.')
      .max(200, 'Name cannot exceed 200 characters.'),
    requirementScope: z.enum(ACADEMIC_REQUIREMENT_SCOPES, {
      errorMap: (issue) => {
        if (issue.code === 'invalid_type' && issue.received === 'undefined') {
          return { message: 'Requirement scope is required.' };
        }
        return {
          message: "Requirement scope must be 'program', 'level', or 'learning_unit'.",
        };
      },
    }),
    learningUnitId: z
      .string()
      .trim()
      .optional()
      .nullable(),
    level: z
      .string()
      .max(50, 'Level cannot exceed 50 characters.')
      .optional()
      .nullable(),
    requirementType: z.enum(ACADEMIC_REQUIREMENT_TYPES, {
      errorMap: (issue) => {
        if (issue.code === 'invalid_type' && issue.received === 'undefined') {
          return { message: 'Requirement type is required.' };
        }
        return {
          message:
            "Requirement type must be 'completion', 'prerequisite', 'credit', 'progression', 'graduation', or 'assessment_policy'.",
        };
      },
    }),
    description: z
      .string({ required_error: 'Description is required.' })
      .trim()
      .min(1, 'Description is required.')
      .max(4000, 'Description cannot exceed 4000 characters.'),
    version: z
      .string({ required_error: 'Version is required.' })
      .trim()
      .min(1, 'Version is required.')
      .max(50, 'Version cannot exceed 50 characters.'),
    effectiveDate: z
      .string({ required_error: 'Effective date is required.' })
      .trim()
      .min(1, 'Effective date is required.')
      .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.')
      .refine((val) => !Number.isNaN(Date.parse(val)), {
        message: 'Effective date must be a valid calendar date.',
      }),
    endDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.')
      .refine((val) => !Number.isNaN(Date.parse(val)), {
        message: 'End date must be a valid calendar date.',
      })
      .optional()
      .nullable(),
    notes: z
      .string()
      .max(4000, 'Notes cannot exceed 4000 characters.')
      .optional()
      .nullable(),
  })
  .refine(
    (data) => {
      if (!data.effectiveDate || !data.endDate) return true;
      return data.endDate >= data.effectiveDate;
    },
    {
      message: 'End date must be greater than or equal to effective date.',
      path: ['endDate'],
    },
  )
  .refine(
    (data) => {
      if (data.requirementScope === 'learning_unit') {
        return Boolean(data.learningUnitId && data.learningUnitId.trim().length > 0);
      }
      return true;
    },
    {
      message: "Learning unit ID is required when requirement scope is 'learning_unit'.",
      path: ['learningUnitId'],
    },
  )
  .refine(
    (data) => {
      if (data.requirementScope === 'level') {
        return Boolean(data.level && data.level.trim().length > 0);
      }
      return true;
    },
    {
      message: "Level is required when requirement scope is 'level'.",
      path: ['level'],
    },
  );

export type CreateAcademicRequirementFormData = z.infer<typeof createAcademicRequirementSchema>;

/**
 * Validation schema for updating an academic requirement.
 */
export const updateAcademicRequirementSchema = z
  .object({
    name: z
      .string({ required_error: 'Name is required.' })
      .trim()
      .min(1, 'Name is required.')
      .max(200, 'Name cannot exceed 200 characters.'),
    description: z
      .string({ required_error: 'Description is required.' })
      .trim()
      .min(1, 'Description is required.')
      .max(4000, 'Description cannot exceed 4000 characters.'),
    notes: z
      .string()
      .max(4000, 'Notes cannot exceed 4000 characters.')
      .optional()
      .nullable(),
    requirementScope: z
      .enum(ACADEMIC_REQUIREMENT_SCOPES, {
        errorMap: () => ({
          message: "Requirement scope must be 'program', 'level', or 'learning_unit'.",
        }),
      })
      .optional()
      .nullable(),
    learningUnitId: z
      .string()
      .trim()
      .optional()
      .nullable(),
    level: z
      .string()
      .max(50, 'Level cannot exceed 50 characters.')
      .optional()
      .nullable(),
    requirementType: z
      .enum(ACADEMIC_REQUIREMENT_TYPES, {
        errorMap: () => ({
          message:
            "Requirement type must be 'completion', 'prerequisite', 'credit', 'progression', 'graduation', or 'assessment_policy'.",
        }),
      })
      .optional()
      .nullable(),
    version: z
      .string()
      .max(50, 'Version cannot exceed 50 characters.')
      .optional()
      .nullable(),
    effectiveDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.')
      .refine((val) => !Number.isNaN(Date.parse(val)), {
        message: 'Effective date must be a valid calendar date.',
      })
      .optional()
      .nullable(),
    endDate: z
      .string()
      .trim()
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.')
      .refine((val) => !Number.isNaN(Date.parse(val)), {
        message: 'End date must be a valid calendar date.',
      })
      .optional()
      .nullable(),
  })
  .refine(
    (data) => {
      if (!data.effectiveDate || !data.endDate) return true;
      return data.endDate >= data.effectiveDate;
    },
    {
      message: 'End date must be greater than or equal to effective date.',
      path: ['endDate'],
    },
  );

export type UpdateAcademicRequirementFormData = z.infer<typeof updateAcademicRequirementSchema>;
