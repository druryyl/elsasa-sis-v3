import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import { searchStudents, getStudentById } from '../api/records.api';
import type {
  StudentDetail,
  StudentListItem,
  StudentSearchParams,
} from '../types/records.types';
import { studentRecordsKeys } from './queryKeys';

export { studentRecordsKeys };

export interface StudentRecordsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

/**
 * Reactive TanStack Query composable for searching official student records (SCR-STU-02, P8-S13).
 * Query key: `['students', 'records', 'list', params]`
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentSearchQuery(
  params?: MaybeRefOrGetter<StudentSearchParams | undefined>,
  options?: StudentRecordsQueryOptions,
) {
  return useQuery<PaginatedResponse<StudentListItem>>({
    queryKey: computed(() => ['students', 'records', 'list', toValue(params)]),
    queryFn: () => searchStudents(toValue(params)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query composable for fetching the 360° authoritative student dossier (SCR-STU-02, P8-S13).
 * Query key: `['students', 'records', 'detail', studentId]`
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useStudentDossierQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: StudentRecordsQueryOptions,
) {
  return useQuery<StudentDetail>({
    queryKey: computed(() => ['students', 'records', 'detail', toValue(studentId)]),
    queryFn: () => getStudentById(toValue(studentId)!),
    enabled: computed(() => {
      const resolvedId = toValue(studentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}
