// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { useResetDemoData } from '../useDemoData';
import * as demoDataApi from '../../services/demoDataApi';

const mockToast = {
  add: vi.fn(),
  removeGroup: vi.fn(),
  removeAllGroups: vi.fn(),
};

vi.mock('primevue/usetoast', () => ({
  useToast: () => mockToast,
}));

function withSetup<T>(composable: () => T) {
  let result!: T;
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const app = createApp({
    setup() {
      result = composable();
      return () => {};
    },
  });

  app.use(VueQueryPlugin, { queryClient });
  app.mount(document.createElement('div'));

  return { result, queryClient, app };
}

describe('useResetDemoData composable', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exposes mutate, mutateAsync, resetDemoData, resetDemoDataAsync, and reactive state', () => {
    const { result } = withSetup(() => useResetDemoData());

    expect(typeof result.mutate).toBe('function');
    expect(typeof result.mutateAsync).toBe('function');
    expect(typeof result.resetDemoData).toBe('function');
    expect(typeof result.resetDemoDataAsync).toBe('function');
    expect(result.isPending).toBeDefined();
    expect(result.error).toBeDefined();
    expect(result.isSuccess).toBeDefined();
  });

  it('triggers resetDemoData API when mutate is invoked', async () => {
    const mockSummary: demoDataApi.DemoResetSummary = {
      purgedSchemas: 5,
      seededDomains: 4,
      elapsedMilliseconds: 350,
      anchoredDate: '2026-11-01',
      baseAcademicYearCode: '2026/2027',
      totalStudentsSeeded: 50,
      totalInvoicesSeeded: 10,
      totalAttendanceSeeded: 100,
      totalResultsSeeded: 80,
      totalReportCardsSeeded: 20,
    };

    const resetSpy = vi.spyOn(demoDataApi, 'resetDemoData').mockResolvedValue(mockSummary);
    const onSuccess = vi.fn();

    const { result, queryClient } = withSetup(() => useResetDemoData({ onSuccess }));
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    result.mutate();

    await vi.waitFor(() => {
      expect(resetSpy).toHaveBeenCalledTimes(1);
      expect(onSuccess).toHaveBeenCalledWith(mockSummary);
      expect(invalidateSpy).toHaveBeenCalled();
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Demo Data Reset',
        }),
      );
    });
  });

  it('handles error when resetDemoData API fails', async () => {
    const mockError = new Error('Reset failed');
    vi.spyOn(demoDataApi, 'resetDemoData').mockRejectedValue(mockError);
    const onError = vi.fn();

    const { result } = withSetup(() => useResetDemoData({ onError }));

    result.mutate();

    await vi.waitFor(() => {
      expect(onError).toHaveBeenCalledWith(mockError);
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Demo Data Reset Failed',
        }),
      );
    });
  });
});
