import type {
  GetAssignmentCompletionsParams,
  GetAssignmentRolesParams,
  GetCandidateEvaluationsParams,
  GetCapacityConflictsParams,
  GetTeacherHistoryParams,
  GetTeachingAssignmentsParams,
  GetTeachingDemandsParams,
  GetTeachingReassignmentsParams,
  GetTeachingSubstitutionsParams,
} from './types';

/**
 * Standardized TanStack Query key factories for Teaching Demands (P7-S21, P2-S04).
 */
export const demandsKeys = {
  all: ['teaching', 'demands'] as const,
  lists: () => [...demandsKeys.all, 'list'] as const,
  list: (params?: GetTeachingDemandsParams) =>
    params !== undefined
      ? ([...demandsKeys.lists(), params] as const)
      : ([...demandsKeys.lists()] as const),
  details: () => [...demandsKeys.all, 'detail'] as const,
  detail: (id?: string | null) =>
    id !== undefined && id !== null && id !== ''
      ? ([...demandsKeys.details(), id] as const)
      : ([...demandsKeys.details()] as const),
};

/**
 * Standardized TanStack Query key factories for Candidate Eligibility Evaluations (P7-S21, P2-S05).
 */
export const evaluationsKeys = {
  all: ['teaching', 'evaluations'] as const,
  lists: () => [...evaluationsKeys.all, 'list'] as const,
  list: (params?: GetCandidateEvaluationsParams) =>
    params !== undefined
      ? ([...evaluationsKeys.lists(), params] as const)
      : ([...evaluationsKeys.lists()] as const),
};

/**
 * Standardized TanStack Query key factories for Teaching Assignments (P7-S21, P3-S06).
 */
export const assignmentsKeys = {
  all: ['teaching', 'assignments'] as const,
  lists: () => [...assignmentsKeys.all, 'list'] as const,
  list: (params?: GetTeachingAssignmentsParams) =>
    params !== undefined
      ? ([...assignmentsKeys.lists(), params] as const)
      : ([...assignmentsKeys.lists()] as const),
  details: () => [...assignmentsKeys.all, 'detail'] as const,
  detail: (id?: string | null) =>
    id !== undefined && id !== null && id !== ''
      ? ([...assignmentsKeys.details(), id] as const)
      : ([...assignmentsKeys.details()] as const),
};

/**
 * Standardized TanStack Query key factories for Assignment Roles catalog (P7-S21, P3-S06).
 */
export const assignmentRolesKeys = {
  all: ['teaching', 'assignment-roles'] as const,
  lists: () => [...assignmentRolesKeys.all, 'list'] as const,
  list: (params?: GetAssignmentRolesParams) =>
    params !== undefined
      ? ([...assignmentRolesKeys.lists(), params] as const)
      : ([...assignmentRolesKeys.lists()] as const),
};

/**
 * Standardized TanStack Query key factories for Capacity Conflicts (P7-S21, P3-S07).
 */
export const conflictsKeys = {
  all: ['teaching', 'conflicts'] as const,
  lists: () => [...conflictsKeys.all, 'list'] as const,
  list: (params?: GetCapacityConflictsParams) =>
    params !== undefined
      ? ([...conflictsKeys.lists(), params] as const)
      : ([...conflictsKeys.lists()] as const),
};

/**
 * Standardized TanStack Query key factories for Teaching Substitutions (P7-S21, P4-S11).
 */
export const substitutionsKeys = {
  all: ['teaching', 'substitutions'] as const,
  lists: () => [...substitutionsKeys.all, 'list'] as const,
  list: (params?: GetTeachingSubstitutionsParams) =>
    params !== undefined
      ? ([...substitutionsKeys.lists(), params] as const)
      : ([...substitutionsKeys.lists()] as const),
};

/**
 * Standardized TanStack Query key factories for Teaching Reassignments (P7-S21, P4-S12).
 */
export const reassignmentsKeys = {
  all: ['teaching', 'reassignments'] as const,
  lists: () => [...reassignmentsKeys.all, 'list'] as const,
  list: (params?: GetTeachingReassignmentsParams) =>
    params !== undefined
      ? ([...reassignmentsKeys.lists(), params] as const)
      : ([...reassignmentsKeys.lists()] as const),
  details: () => [...reassignmentsKeys.all, 'detail'] as const,
  detail: (id?: string | null) =>
    id !== undefined && id !== null && id !== ''
      ? ([...reassignmentsKeys.details(), id] as const)
      : ([...reassignmentsKeys.details()] as const),
};

/**
 * Standardized TanStack Query key factories for Assignment Conclusions (P7-S21, P4-S13).
 */
export const completionsKeys = {
  all: ['teaching', 'completions'] as const,
  lists: () => [...completionsKeys.all, 'list'] as const,
  list: (params?: GetAssignmentCompletionsParams) =>
    params !== undefined
      ? ([...completionsKeys.lists(), params] as const)
      : ([...completionsKeys.lists()] as const),
};

/**
 * Standardized TanStack Query key factories for Teaching History (P7-S21, P4-S14).
 */
export const historyKeys = {
  all: ['teaching', 'history'] as const,
  teachers: () => [...historyKeys.all, 'teacher'] as const,
  teacher: (teacherId?: string | null, params?: GetTeacherHistoryParams) => {
    const base =
      teacherId !== undefined && teacherId !== null && teacherId !== ''
        ? ([...historyKeys.teachers(), teacherId] as const)
        : ([...historyKeys.teachers()] as const);
    return params !== undefined ? ([...base, params] as const) : base;
  },
};

/**
 * Unified assignments query keys structure grouping all sub-domains.
 */
export const teachingAssignmentsKeys = {
  all: ['teaching'] as const,
  demands: demandsKeys,
  evaluations: evaluationsKeys,
  assignments: assignmentsKeys,
  roles: assignmentRolesKeys,
  conflicts: conflictsKeys,
  substitutions: substitutionsKeys,
  reassignments: reassignmentsKeys,
  completions: completionsKeys,
  history: historyKeys,
};

export const assignmentKeys = teachingAssignmentsKeys;
