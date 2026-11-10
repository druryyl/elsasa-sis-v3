import type { FacultyQueryParams, GetRecordCorrectionsParams } from '../api/teachingTypes';

/**
 * Standardized TanStack Query key factories for Teaching Faculty Management (SCR-TCH-01, P7-S22).
 * Follows hierarchical pattern to enable precise targeting or broad hierarchy cache invalidation.
 */
export const teachingFacultyKeys = {
  all: ['teaching', 'faculty'] as const,
  lists: () => [...teachingFacultyKeys.all, 'list'] as const,
  list: (params?: FacultyQueryParams | string) =>
    params !== undefined
      ? ([...teachingFacultyKeys.lists(), params] as const)
      : ([...teachingFacultyKeys.lists()] as const),
  details: () => [...teachingFacultyKeys.all, 'detail'] as const,
  detail: (id?: string | null) =>
    id !== undefined && id !== null && id !== ''
      ? ([...teachingFacultyKeys.details(), id] as const)
      : ([...teachingFacultyKeys.details()] as const),
  roles: () => [...teachingFacultyKeys.all, 'roles'] as const,
  capabilityTypes: () => [...teachingFacultyKeys.all, 'capability-types'] as const,
  qualificationTypes: () => [...teachingFacultyKeys.all, 'qualification-types'] as const,
  correctionsList: () => [...teachingFacultyKeys.all, 'corrections'] as const,
  corrections: (params?: GetRecordCorrectionsParams) =>
    params !== undefined
      ? ([...teachingFacultyKeys.correctionsList(), params] as const)
      : ([...teachingFacultyKeys.correctionsList()] as const),
};

export const facultyKeys = teachingFacultyKeys;
