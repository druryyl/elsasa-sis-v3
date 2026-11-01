import { QueryClient, type QueryClientConfig } from '@tanstack/vue-query';

/**
 * Cache staleness and garbage collection tiers defined in Section 8.1
 * of docs/architecture/frontend-architecture.md.
 *
 * - STATIC: Static / Configuration data (stale: 30m, gc: 60m, refetchOnWindowFocus: false)
 * - OPERATIONAL: Operational Reference data (stale: 5m, gc: 15m, refetchOnWindowFocus: true)
 * - ACTIVE_QUEUE: Active Work Queues (stale: 30s, gc: 5m, refetchOnWindowFocus: true)
 * - TRANSACTIONAL: Transactional Details (stale: 0s, gc: 5m, refetchOnWindowFocus: true)
 */
export const CACHE_TIERS = {
  STATIC: {
    staleTime: 30 * 60 * 1000, // 30 minutes (1,800,000 ms)
    gcTime: 60 * 60 * 1000, // 60 minutes (3,600,000 ms)
    refetchOnWindowFocus: false,
  },
  OPERATIONAL: {
    staleTime: 5 * 60 * 1000, // 5 minutes (300,000 ms)
    gcTime: 15 * 60 * 1000, // 15 minutes (900,000 ms)
    refetchOnWindowFocus: true,
  },
  ACTIVE_QUEUE: {
    staleTime: 30 * 1000, // 30 seconds (30,000 ms)
    gcTime: 5 * 60 * 1000, // 5 minutes (300,000 ms)
    refetchOnWindowFocus: true,
  },
  TRANSACTIONAL: {
    staleTime: 0, // stale immediately
    gcTime: 5 * 60 * 1000, // 5 minutes (300,000 ms)
    refetchOnWindowFocus: true,
  },
} as const;

export type CacheTier = keyof typeof CACHE_TIERS;

/**
 * Default QueryClient configuration for Elsasa SIS V3.
 * Defaults align with the Active Work Queues policy:
 * - staleTime: 30000 (30 seconds)
 * - gcTime: 300000 (5 minutes)
 * - retry: 1
 * - refetchOnWindowFocus: true
 * Mutations:
 * - retry: 0 (mutations are not automatically retried to prevent duplicate side effects)
 */
export const defaultQueryClientConfig: QueryClientConfig = {
  defaultOptions: {
    queries: {
      staleTime: 30000,
      gcTime: 300000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
    mutations: {
      retry: 0,
    },
  },
};

/**
 * Factory creating a new QueryClient instance with default or customized configuration.
 */
export function createQueryClient(
  config: QueryClientConfig = defaultQueryClientConfig,
): QueryClient {
  return new QueryClient(config);
}

/**
 * Root QueryClient singleton instance for application-wide query caching.
 */
export const queryClient: QueryClient = createQueryClient();

export default queryClient;
