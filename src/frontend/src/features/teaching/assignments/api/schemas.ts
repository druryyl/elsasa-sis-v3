import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
export const DEMAND_CODE_REGEX = /^[A-Za-z0-9\-_]{3,50}$/;

export const TEACHING_DEMAND_STATUSES = [
  'received',
  'accepted_for_staffing',
  'returned_for_clarification',
  'changed',
  'cancelled',
  'declined',
  'staffed',
] as const;

export const TEACHING_DEMAND_LIFECYCLE_TARGETS = [
  'accepted_for_staffing',
  'returned_for_clarification',
  'declined',
  'cancelled',
] as const;

export const ASSIGNMENT_SCOPE_CATEGORIES = [
  'learning_unit',
  'cohort_group',
  'individual_student',
  'extracurricular_group',
] as const;

export const CAPACITY_CONFLICT_RESOLUTIONS = ['resolved', 'waived_with_exception'] as const;

export const COVERAGE_SCOPE_TYPES = ['full', 'partial'] as const;

export const UNRESOLVED_ITEMS_STATUSES = [
  'none',
  'minor_pending',
  'escalated_defect',
  'resolved',
  'escalated',
] as const;

/**
 * Zod validation schema for creating/ingesting a teaching demand (TD-02, P2-S04).
 * Aligns strictly with CreateTeachingDemandValidator on backend.
 */
export const createDemandSchema = z
  .object({
    sourceDomain: z
      .string()
      .min(1, 'Source domain is required.')
      .max(50, 'Source domain cannot exceed 50 characters.'),
    sourceWorkReference: z
      .string()
      .min(1, 'Source work reference is required.')
      .uuid('Source work reference must be a valid UUID.'),
    sourceWorkLabel: z
      .string()
      .min(1, 'Source work label is required.')
      .max(255, 'Source work label cannot exceed 255 characters.'),
    academicPeriodId: z
      .string()
      .min(1, 'Academic period ID is required.')
      .uuid('Academic period ID must be a valid UUID.'),
    requiredCapabilityCode: z
      .string()
      .min(1, 'Required capability code is required.')
      .max(100, 'Required capability code cannot exceed 100 characters.'),
    requiredAssignmentRoleId: z
      .string()
      .min(1, 'Required assignment role ID is required.')
      .uuid('Required assignment role ID must be a valid UUID.'),
    expectedWorkloadHours: z.coerce
      .number({ invalid_type_error: 'Expected workload hours must be a number.' })
      .positive('Expected workload hours must be greater than zero.')
      .max(999.99, 'Expected workload hours cannot exceed 999.99.'),
    requestedStartDate: z
      .string()
      .min(1, 'Requested start date is required.')
      .regex(ISO_DATE_REGEX, 'Requested start date must be in YYYY-MM-DD format.'),
    requestedEndDate: z
      .string()
      .min(1, 'Requested end date is required.')
      .regex(ISO_DATE_REGEX, 'Requested end date must be in YYYY-MM-DD format.'),
    workloadUnit: z
      .string()
      .max(50, 'Workload unit cannot exceed 50 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : 'Teaching Hours')),
    requiresAssessmentAuthority: z.boolean().default(true),
    campusScope: z
      .string()
      .max(100, 'Campus scope cannot exceed 100 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : 'Kampus Utama')),
    requiredTeachersCount: z.coerce
      .number({ invalid_type_error: 'Required teachers count must be a number.' })
      .int('Required teachers count must be an integer.')
      .min(1, 'Required teachers count must be at least 1.')
      .max(50, 'Required teachers count cannot exceed 50.')
      .default(1),
    demandCode: z
      .string()
      .max(50, 'Demand code cannot exceed 50 characters.')
      .regex(
        DEMAND_CODE_REGEX,
        'Demand code format is invalid. Must contain 3 to 50 alphanumeric characters, hyphens, or underscores.',
      )
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    clarificationNotes: z
      .string()
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  })
  .refine(
    (data) => {
      if (!data.requestedStartDate || !data.requestedEndDate) return true;
      return data.requestedEndDate >= data.requestedStartDate;
    },
    {
      message: 'Requested end date must be on or after requested start date.',
      path: ['requestedEndDate'],
    },
  );

export type CreateDemandFormValues = z.infer<typeof createDemandSchema>;

/**
 * Zod validation schema for updating a demand's lifecycle status (P2-S04).
 * Aligns strictly with UpdateTeachingDemandStatusValidator on backend.
 */
export const updateDemandStatusSchema = z
  .object({
    status: z.enum(TEACHING_DEMAND_LIFECYCLE_TARGETS, {
      errorMap: () => ({
        message:
          'Invalid target status. Allowed transitions: accepted_for_staffing, returned_for_clarification, declined, cancelled.',
      }),
    }),
    declineReason: z
      .string()
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    clarificationNotes: z
      .string()
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  })
  .refine(
    (data) => {
      if (data.status === 'declined') {
        return Boolean(data.declineReason && data.declineReason.trim().length > 0);
      }
      return true;
    },
    {
      message: 'Decline reason is mandatory when declining a teaching demand.',
      path: ['declineReason'],
    },
  );

export type UpdateDemandStatusFormValues = z.infer<typeof updateDemandStatusSchema>;

/**
 * Zod validation schema for evaluating candidate teacher eligibility (P2-S05).
 * Aligns strictly with EvaluateCandidateEligibilityValidator on backend.
 */
export const evaluateCandidateSchema = z.object({
  teachingDemandId: z
    .string()
    .min(1, 'Teaching demand ID is required.')
    .uuid('Teaching demand ID must be a valid UUID.'),
  teacherProfileId: z
    .string()
    .min(1, 'Teacher profile ID is required.')
    .uuid('Teacher profile ID must be a valid UUID.'),
  coordinatorRemarks: z
    .string()
    .max(2000, 'Coordinator remarks cannot exceed 2000 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
});

export type EvaluateCandidateFormValues = z.infer<typeof evaluateCandidateSchema>;

/**
 * Zod validation schema for proposing a teaching assignment (P3-S06).
 * Aligns strictly with ProposeTeachingAssignmentValidator on backend.
 */
export const proposeAssignmentSchema = z
  .object({
    teachingDemandId: z
      .string()
      .min(1, 'Teaching demand ID is required.')
      .uuid('Teaching demand ID must be a valid UUID.'),
    teacherProfileId: z
      .string()
      .min(1, 'Teacher profile ID is required.')
      .uuid('Teacher profile ID must be a valid UUID.'),
    assignmentRoleId: z
      .string()
      .min(1, 'Assignment role ID is required.')
      .uuid('Assignment role ID must be a valid UUID.'),
    assignmentScope: z.enum(ASSIGNMENT_SCOPE_CATEGORIES, {
      errorMap: () => ({
        message:
          'Assignment scope must be one of: learning_unit, cohort_group, individual_student, extracurricular_group.',
      }),
    }),
    scopeReferenceId: z
      .string()
      .min(1, 'Scope reference ID is required.')
      .uuid('Scope reference ID must be a valid UUID.'),
    scopeLabel: z
      .string()
      .min(1, 'Scope label is required.')
      .max(255, 'Scope label must not exceed 255 characters.'),
    academicPeriodId: z
      .string()
      .min(1, 'Academic period ID is required.')
      .uuid('Academic period ID must be a valid UUID.'),
    effectiveStartDate: z
      .string()
      .min(1, 'Effective start date is required.')
      .regex(ISO_DATE_REGEX, 'Effective start date must be in YYYY-MM-DD format.'),
    effectiveEndDate: z
      .string()
      .min(1, 'Effective end date is required.')
      .regex(ISO_DATE_REGEX, 'Effective end date must be in YYYY-MM-DD format.'),
    workloadCommitmentValue: z.coerce
      .number({ invalid_type_error: 'Workload commitment value must be a number.' })
      .min(0, 'Workload commitment value must be greater than or equal to 0.')
      .max(100, 'Workload commitment value must not exceed 100 hours.'),
    workloadUnit: z
      .string()
      .max(50, 'Workload unit must not exceed 50 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : 'Teaching Hours')),
    assignmentNumber: z
      .string()
      .max(50, 'Assignment number must not exceed 50 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    predecessorAssignmentId: z
      .string()
      .uuid('Predecessor assignment ID must be a valid UUID.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    notes: z
      .string()
      .max(4000, 'Notes must not exceed 4000 characters.')
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  })
  .refine(
    (data) => {
      if (!data.effectiveStartDate || !data.effectiveEndDate) return true;
      return data.effectiveEndDate >= data.effectiveStartDate;
    },
    {
      message: 'Effective start date must be before or equal to effective end date.',
      path: ['effectiveEndDate'],
    },
  );

export type ProposeAssignmentFormValues = z.infer<typeof proposeAssignmentSchema>;

/**
 * Zod validation schema for resolving or waiving a capacity conflict (P3-S07).
 * Aligns strictly with ResolveCapacityConflictValidator on backend.
 */
export const resolveConflictSchema = z.object({
  status: z.enum(CAPACITY_CONFLICT_RESOLUTIONS, {
    errorMap: () => ({
      message: "Status must be either 'resolved' or 'waived_with_exception'.",
    }),
  }),
  resolutionNotes: z
    .string()
    .max(4000, 'Resolution notes must not exceed 4000 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  resolutionAction: z
    .string()
    .max(50, 'Resolution action must not exceed 50 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
});

export type ResolveConflictFormValues = z.infer<typeof resolveConflictSchema>;

/**
 * Zod validation schema for arranging an emergency substitution (P4-S11, Resolution 3.8).
 * Aligns strictly with ArrangeEmergencySubstitutionValidator on backend.
 */
export const arrangeSubstituteSchema = z
  .object({
    originalAssignmentId: z
      .string()
      .min(1, 'Original assignment ID is required.')
      .uuid('Original assignment ID must be a valid UUID.'),
    substituteTeacherId: z
      .string()
      .min(1, 'Substitute teacher ID is required.')
      .uuid('Substitute teacher ID must be a valid UUID.'),
    startDate: z
      .string()
      .min(1, 'Start date is required.')
      .regex(ISO_DATE_REGEX, 'Start date must be in YYYY-MM-DD format.'),
    endDate: z
      .string()
      .min(1, 'End date is required.')
      .regex(ISO_DATE_REGEX, 'End date must be in YYYY-MM-DD format.'),
    coverageScopeType: z.enum(COVERAGE_SCOPE_TYPES, {
      errorMap: () => ({
        message: "Coverage scope type must be 'full' or 'partial'.",
      }),
    }),
    coveredHoursPerWeek: z.coerce
      .number({ invalid_type_error: 'Covered hours must be a number.' })
      .min(0, 'Covered workload hours must be non-negative.')
      .nullish()
      .transform((val) => val ?? undefined),
    workloadImpactHours: z.coerce
      .number({ invalid_type_error: 'Workload impact hours must be a number.' })
      .min(0, 'Workload impact hours must be non-negative.')
      .nullish()
      .transform((val) => val ?? undefined),
    reason: z
      .string()
      .min(1, 'Reason is required.')
      .max(50, 'Reason must not exceed 50 characters.'),
    partialScopeDetails: z
      .string()
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
    handoverNotes: z
      .string()
      .nullish()
      .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  })
  .refine(
    (data) => {
      if (!data.startDate || !data.endDate) return true;
      return data.endDate >= data.startDate;
    },
    {
      message: 'End date must be on or after start date.',
      path: ['endDate'],
    },
  );

export type ArrangeSubstituteFormValues = z.infer<typeof arrangeSubstituteSchema>;

/**
 * Zod validation schema for proposing a permanent teaching reassignment (P4-S12).
 * Aligns strictly with ProposePermanentReassignmentValidator on backend.
 */
export const reassignmentSchema = z.object({
  originalAssignmentId: z
    .string()
    .min(1, 'Original assignment ID is required.')
    .uuid('Original assignment ID must be a valid UUID.'),
  newTeacherId: z
    .string()
    .min(1, 'New teacher profile ID is required.')
    .uuid('New teacher profile ID must be a valid UUID.'),
  effectiveDate: z
    .string()
    .min(1, 'Effective date is required.')
    .regex(ISO_DATE_REGEX, 'Effective date must be in YYYY-MM-DD format.'),
  reason: z
    .string()
    .min(1, 'Reason for reassignment is required.')
    .max(2000, 'Reason must not exceed 2000 characters.'),
  requiresHandover: z.boolean().default(true),
  reassignmentType: z
    .string()
    .max(50, 'Reassignment type must not exceed 50 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : 'permanent_transfer')),
});

export type ReassignmentFormValues = z.infer<typeof reassignmentSchema>;

/**
 * Zod validation schema for concluding a teaching assignment and releasing capacity (P4-S13).
 * Aligns strictly with ConcludeTeachingAssignmentValidator on backend.
 */
export const concludeAssignmentSchema = z.object({
  completionDate: z
    .string()
    .min(1, 'Completion date is required.')
    .regex(ISO_DATE_REGEX, 'Completion date must be in YYYY-MM-DD format.'),
  academicConfirmationReceived: z.boolean().default(true),
  academicConfirmationSource: z
    .string()
    .max(256, 'Academic confirmation source must not exceed 256 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  handoverConfirmed: z.boolean().default(true),
  unresolvedItemsStatus: z
    .enum(UNRESOLVED_ITEMS_STATUSES, {
      errorMap: () => ({
        message:
          "Unresolved items status must be one of: 'none', 'minor_pending', 'escalated_defect', 'resolved', 'escalated'.",
      }),
    })
    .default('none'),
  unresolvedItemsDescription: z
    .string()
    .max(2000, 'Unresolved items description must not exceed 2000 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
  notes: z
    .string()
    .max(2000, 'Notes must not exceed 2000 characters.')
    .nullish()
    .transform((val) => (val && val.trim() !== '' ? val.trim() : undefined)),
});

export type ConcludeAssignmentFormValues = z.infer<typeof concludeAssignmentSchema>;
