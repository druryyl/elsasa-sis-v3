import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import {
  getTeachers,
  getTeacherById,
  getRoles,
  getCapabilityTypes,
  getQualificationTypes,
  getCorrections,
} from '../api/teachingApiClient';
import type {
  CapabilityTypeDto,
  FacultyQueryParams,
  GetRecordCorrectionsParams,
  QualificationTypeResponse,
  TeacherProfileDto,
  TeacherSummaryDto,
  TeachingRecordCorrectionDto,
  TeachingRoleDto,
} from '../api/teachingTypes';
import { facultyKeys, teachingFacultyKeys } from './queryKeys';

export { facultyKeys, teachingFacultyKeys };

export interface FacultyQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

/**
 * Reactive TanStack Query hook fetching faculty work queue with filters and pagination (SCR-TCH-01, P2-S07).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useTeacherProfilesQuery(
  filters?: MaybeRefOrGetter<FacultyQueryParams | undefined>,
  options?: FacultyQueryOptions,
) {
  return useQuery<PaginatedResponse<TeacherSummaryDto>>({
    queryKey: computed(() => facultyKeys.list(toValue(filters))),
    queryFn: () => getTeachers(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Alias for useTeacherProfilesQuery adhering to plan naming convention.
 */
export const useFacultyWorkQueue = useTeacherProfilesQuery;

/**
 * Reactive TanStack Query hook fetching a complete authoritative teacher dossier by ID (SCR-TCH-01, P2-S07).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useTeacherProfileQuery(
  teacherId: MaybeRefOrGetter<string | null | undefined>,
  options?: FacultyQueryOptions,
) {
  return useQuery<TeacherProfileDto>({
    queryKey: computed(() => facultyKeys.detail(toValue(teacherId))),
    queryFn: () => getTeacherById(toValue(teacherId)!),
    enabled: computed(() => {
      const resolvedId = toValue(teacherId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Alias for useTeacherProfileQuery adhering to plan naming convention.
 */
export const useTeacherDetail = useTeacherProfileQuery;

/**
 * Reactive TanStack Query hook fetching active recognized teaching roles (P2-S07).
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus).
 */
export function useTeachingRolesQuery(options?: FacultyQueryOptions) {
  return useQuery<TeachingRoleDto[]>({
    queryKey: computed(() => facultyKeys.roles()),
    queryFn: () => getRoles(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Alias for useTeachingRolesQuery adhering to plan naming convention.
 */
export const useTeachingRoles = useTeachingRolesQuery;

/**
 * Reactive TanStack Query hook fetching capability taxonomy categories (P2-S08).
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus).
 */
export function useCapabilityTypesQuery(options?: FacultyQueryOptions) {
  return useQuery<CapabilityTypeDto[]>({
    queryKey: computed(() => facultyKeys.capabilityTypes()),
    queryFn: () => getCapabilityTypes(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Alias for useCapabilityTypesQuery adhering to plan naming convention.
 */
export const useCapabilityTypes = useCapabilityTypesQuery;

/**
 * Reactive TanStack Query hook fetching qualification types and renewal rules (P3-S09).
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus).
 */
export function useQualificationTypesQuery(options?: FacultyQueryOptions) {
  return useQuery<QualificationTypeResponse[]>({
    queryKey: computed(() => facultyKeys.qualificationTypes()),
    queryFn: () => getQualificationTypes(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Alias for useQualificationTypesQuery adhering to plan naming convention.
 */
export const useQualificationTypes = useQualificationTypesQuery;

/**
 * Reactive TanStack Query hook querying chronological audit trail of faculty record corrections (P5-S14).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useRecordCorrectionsQuery(
  params?: MaybeRefOrGetter<GetRecordCorrectionsParams | undefined>,
  options?: FacultyQueryOptions,
) {
  return useQuery<TeachingRecordCorrectionDto[]>({
    queryKey: computed(() => facultyKeys.corrections(toValue(params))),
    queryFn: () => getCorrections(toValue(params)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}
