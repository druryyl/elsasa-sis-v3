import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { getDomainKpiDashboard, getExecutiveDashboard } from './dashboard.api';
import { dashboardKeys } from './dashboard.keys';
import type {
  DomainKpiDashboardResponse,
  ExecutiveDashboardResponse,
  GlobalKpiFilterDto,
  OperationalKpiDomainKey,
} from '../types/dashboard.types';

export { dashboardKeys };

export interface DashboardQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

/**
 * Reactive TanStack Query composable fetching the Level 1 Executive Overview dashboard.
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useExecutiveDashboardQuery(
  filter?: MaybeRefOrGetter<GlobalKpiFilterDto | undefined>,
  options?: DashboardQueryOptions,
) {
  return useQuery<ExecutiveDashboardResponse>({
    queryKey: computed(() => dashboardKeys.executive(toValue(filter))),
    queryFn: () => getExecutiveDashboard(toValue(filter)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query composable fetching a Level 2 Domain KPI dashboard.
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useDomainKpiDashboardQuery(
  domain: MaybeRefOrGetter<OperationalKpiDomainKey | string>,
  filter?: MaybeRefOrGetter<GlobalKpiFilterDto | undefined>,
  options?: DashboardQueryOptions,
) {
  return useQuery<DomainKpiDashboardResponse>({
    queryKey: computed(() => dashboardKeys.domain(toValue(domain), toValue(filter))),
    queryFn: () => getDomainKpiDashboard(toValue(domain), toValue(filter)),
    enabled: computed(() => {
      const resolvedDomain = toValue(domain);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedDomain) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}
