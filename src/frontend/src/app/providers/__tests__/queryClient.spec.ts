import { describe, it, expect, vi } from 'vitest';
import {
  queryClient,
  createQueryClient,
  defaultQueryClientConfig,
  CACHE_TIERS,
} from '../queryClient';

describe('QueryClient Configuration and Cache Infrastructure', () => {
  it('configures default query options matching Elsasa SIS architecture requirements', () => {
    const defaultQueries = queryClient.getDefaultOptions().queries;
    const defaultMutations = queryClient.getDefaultOptions().mutations;

    expect(defaultQueries?.staleTime).toBe(30000);
    expect(defaultQueries?.gcTime).toBe(300000);
    expect(defaultQueries?.retry).toBe(1);
    expect(defaultQueries?.refetchOnWindowFocus).toBe(true);
    expect(defaultMutations?.retry).toBe(0);
  });

  it('exports defaultQueryClientConfig with deterministic policies', () => {
    expect(defaultQueryClientConfig.defaultOptions?.queries?.staleTime).toBe(30000);
    expect(defaultQueryClientConfig.defaultOptions?.queries?.gcTime).toBe(300000);
    expect(defaultQueryClientConfig.defaultOptions?.queries?.retry).toBe(1);
    expect(defaultQueryClientConfig.defaultOptions?.queries?.refetchOnWindowFocus).toBe(true);
    expect(defaultQueryClientConfig.defaultOptions?.mutations?.retry).toBe(0);
  });

  it('defines deterministic cache tiers matching Section 8.1 of frontend architecture', () => {
    expect(CACHE_TIERS.STATIC).toEqual({
      staleTime: 1800000,
      gcTime: 3600000,
      refetchOnWindowFocus: false,
    });

    expect(CACHE_TIERS.OPERATIONAL).toEqual({
      staleTime: 300000,
      gcTime: 900000,
      refetchOnWindowFocus: true,
    });

    expect(CACHE_TIERS.ACTIVE_QUEUE).toEqual({
      staleTime: 30000,
      gcTime: 300000,
      refetchOnWindowFocus: true,
    });

    expect(CACHE_TIERS.TRANSACTIONAL).toEqual({
      staleTime: 0,
      gcTime: 300000,
      refetchOnWindowFocus: true,
    });
  });

  it('creates isolated QueryClient instances with default config via factory', () => {
    const clientA = createQueryClient();
    const clientB = createQueryClient();

    expect(clientA).not.toBe(clientB);
    expect(clientA.getDefaultOptions().queries?.staleTime).toBe(30000);
    expect(clientB.getDefaultOptions().queries?.staleTime).toBe(30000);
  });

  it('allows overriding options in createQueryClient factory', () => {
    const customClient = createQueryClient({
      defaultOptions: {
        queries: {
          staleTime: CACHE_TIERS.STATIC.staleTime,
          gcTime: CACHE_TIERS.STATIC.gcTime,
          refetchOnWindowFocus: CACHE_TIERS.STATIC.refetchOnWindowFocus,
        },
      },
    });

    const queries = customClient.getDefaultOptions().queries;
    expect(queries?.staleTime).toBe(1800000);
    expect(queries?.gcTime).toBe(3600000);
    expect(queries?.refetchOnWindowFocus).toBe(false);
  });

  it('executes queries, caches results, and reuses cache within stale time', async () => {
    const client = createQueryClient();
    const queryFn = vi.fn().mockResolvedValue({ id: 'ay-2026', name: '2026/2027' });

    const result1 = await client.fetchQuery({
      queryKey: ['academic-years', 'current'],
      queryFn,
    });

    expect(result1).toEqual({ id: 'ay-2026', name: '2026/2027' });
    expect(queryFn).toHaveBeenCalledTimes(1);

    // Second fetch within staleTime should return cached result without calling queryFn
    const result2 = await client.fetchQuery({
      queryKey: ['academic-years', 'current'],
      queryFn,
    });

    expect(result2).toEqual({ id: 'ay-2026', name: '2026/2027' });
    expect(queryFn).toHaveBeenCalledTimes(1);
  });

  it('invalidates queries causing them to refetch upon next access', async () => {
    const client = createQueryClient();
    const queryFn = vi
      .fn()
      .mockResolvedValueOnce({ count: 10 })
      .mockResolvedValueOnce({ count: 11 });

    await client.fetchQuery({
      queryKey: ['admissions', 'queue-count'],
      queryFn,
    });
    expect(queryFn).toHaveBeenCalledTimes(1);

    // Invalidate the query
    await client.invalidateQueries({ queryKey: ['admissions'] });

    // Next fetch must re-invoke queryFn because cache was invalidated
    const refreshed = await client.fetchQuery({
      queryKey: ['admissions', 'queue-count'],
      queryFn,
    });

    expect(refreshed).toEqual({ count: 11 });
    expect(queryFn).toHaveBeenCalledTimes(2);
  });

  it('supports direct query data mutation and cache clearance', () => {
    const client = createQueryClient();
    const testData = { id: 'usr-1', username: 'admin' };

    client.setQueryData(['session', 'user'], testData);
    expect(client.getQueryData(['session', 'user'])).toEqual(testData);

    client.clear();
    expect(client.getQueryData(['session', 'user'])).toBeUndefined();
  });
});
