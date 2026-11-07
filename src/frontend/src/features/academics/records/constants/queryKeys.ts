import type { GetAssessmentsFilter } from '../../assessment/types';

/**
 * Standardized TanStack Query key factories for Academics Assessment & Records features
 * (P8-S20, UC-ACD-05, UC-ACD-06, UC-ACD-13, UC-ACD-16, UC-ACD-19, TD-10).
 *
 * Provides granular cache invalidation keys for assessments, evidence, results,
 * report cards, transcripts, and record corrections.
 */

export const assessmentKeys = {
  all: ['academics', 'assessments'] as const,
  lists: () => [...assessmentKeys.all, 'list'] as const,
  list: (unitId?: string | null, filter?: GetAssessmentsFilter) =>
    [...assessmentKeys.lists(), unitId, filter] as const,
  details: () => [...assessmentKeys.all, 'detail'] as const,
  detail: (id: string | null | undefined) => [...assessmentKeys.details(), id] as const,
} as const;

export const evidenceKeys = {
  all: ['academics', 'evidence'] as const,
  lists: () => [...evidenceKeys.all, 'list'] as const,
  byAssessment: (
    assessmentId: string | null | undefined,
    params?: { studentId?: string; applicantId?: string },
  ) => [...evidenceKeys.lists(), assessmentId, params] as const,
} as const;

export const resultKeys = {
  all: ['academics', 'results'] as const,
  lists: () => [...resultKeys.all, 'list'] as const,
  byAssessment: (
    assessmentId: string | null | undefined,
    params?: { status?: string; onlyFinal?: boolean },
  ) => [...resultKeys.lists(), assessmentId, params] as const,
  studentResults: (
    academicPeriodId: string | null | undefined,
    studentId?: string | null,
  ) => [...resultKeys.all, 'student', academicPeriodId, studentId] as const,
} as const;

export const reportCardKeys = {
  all: ['academics', 'report-cards'] as const,
  lists: () => [...reportCardKeys.all, 'list'] as const,
  list: (params?: { studentId?: string; academicPeriodId?: string }) =>
    [...reportCardKeys.lists(), params] as const,
  byPeriod: (periodId?: string | null | undefined, studentId?: string | null) =>
    [...reportCardKeys.lists(), { periodId, studentId }] as const,
  details: () => [...reportCardKeys.all, 'detail'] as const,
  detail: (id: string | null | undefined) => [...reportCardKeys.details(), id] as const,
  student: (params?: { studentId?: string; academicPeriodId?: string }) =>
    [...reportCardKeys.all, 'student', params] as const,
} as const;

export const transcriptKeys = {
  all: ['academics', 'transcripts'] as const,
  lists: () => [...transcriptKeys.all, 'list'] as const,
  byStudent: (
    studentId: string | null | undefined,
    transcriptType?: string | null,
  ) => [...transcriptKeys.lists(), 'student', studentId, transcriptType] as const,
  details: () => [...transcriptKeys.all, 'detail'] as const,
  detail: (id: string | null | undefined) => [...transcriptKeys.details(), id] as const,
} as const;

export const correctionKeys = {
  all: ['academics', 'corrections'] as const,
  histories: () => [...correctionKeys.all, 'history'] as const,
  history: (resultId: string | null | undefined) =>
    [...correctionKeys.histories(), resultId] as const,
  detail: (resultId: string | null | undefined) =>
    [...correctionKeys.histories(), resultId] as const,
} as const;

export const learningProgressKeys = {
  all: ['academics', 'learning-progress'] as const,
  byStudent: (studentId?: string | null | undefined, periodId?: string | null | undefined) =>
    ['academics', 'learning-progress', studentId, periodId] as const,
  byUnit: (unitId?: string | null | undefined) =>
    ['academics', 'learning-progress', 'unit', unitId] as const,
} as const;

