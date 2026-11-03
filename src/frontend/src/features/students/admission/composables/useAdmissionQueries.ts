import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getAdmissionApplicationsList,
  getAdmissionApplicationById,
  getApplicationStatusByNumber,
  getActiveAdmissionPeriods,
} from '../api/admission.api';
import type {
  AdmissionQueueFilterParams,
  AdmissionApplicationListItemDto,
  AdmissionApplicationDetailDto,
  PublicApplicationStatusDto,
  AdmissionPeriodOptionDto,
} from '../types/admission.types';
import { admissionKeys } from './queryKeys';

export { admissionKeys };

export interface AdmissionQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

/**
 * Reactive TanStack Query hook fetching the staff intake work queue (SCR-STU-01).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useAdmissionQueueQuery(
  filters?: MaybeRefOrGetter<AdmissionQueueFilterParams | undefined>,
  options?: AdmissionQueryOptions,
) {
  return useQuery<AdmissionApplicationListItemDto[]>({
    queryKey: computed(() => admissionKeys.list(toValue(filters))),
    queryFn: () => getAdmissionApplicationsList(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching a complete intake dossier by application ID (SCR-STU-01).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useAdmissionDossierQuery(
  applicationId: MaybeRefOrGetter<string | null | undefined>,
  options?: AdmissionQueryOptions,
) {
  return useQuery<AdmissionApplicationDetailDto>({
    queryKey: computed(() => admissionKeys.detail(toValue(applicationId) || '')),
    queryFn: () => getAdmissionApplicationById(toValue(applicationId)!),
    enabled: computed(() => {
      const resolvedId = toValue(applicationId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook for public self-service status tracking by number and birth date (SCR-SELF-01).
 * Uses TRANSACTIONAL cache tier (stale immediately, 5m gc, refetch on window focus).
 */
export function useApplicationStatusQuery(
  number: MaybeRefOrGetter<string | null | undefined>,
  dob: MaybeRefOrGetter<string | null | undefined>,
  options?: AdmissionQueryOptions,
) {
  return useQuery<PublicApplicationStatusDto>({
    queryKey: computed(() => admissionKeys.status(toValue(number) || '', toValue(dob) || '')),
    queryFn: () =>
      getApplicationStatusByNumber({
        applicationNumber: toValue(number)!,
        dateOfBirth: toValue(dob)!,
      }),
    enabled: computed(() => {
      const num = toValue(number)?.trim();
      const birth = toValue(dob)?.trim();
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(num) && Boolean(birth) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching active admission periods from Operations.
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus).
 */
export function useActiveAdmissionPeriodsQuery(options?: AdmissionQueryOptions) {
  return useQuery<AdmissionPeriodOptionDto[]>({
    queryKey: computed(() => admissionKeys.activePeriods()),
    queryFn: () => getActiveAdmissionPeriods(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}
