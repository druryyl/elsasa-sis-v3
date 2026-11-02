// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as operationalPeriodApi from '../../api/operationalPeriod.api';
import {
  useOperationalPeriodsQuery,
  useCreateOperationalPeriodMutation,
  useUpdateOperationalPeriodMutation,
  useCloseOperationalPeriodMutation,
} from '../useOperationalPeriodsQuery';
import { operationalPeriodKeys, calendarKeys } from '../queryKeys';

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

describe('Operational Periods Composables (useOperationalPeriodsQuery.ts)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useOperationalPeriodsQuery', () => {
    it('fetches periods for academicYearId when provided and updates reactively', async () => {
      const mockPeriods: operationalPeriodApi.OperationalPeriodDto[] = [
        {
          id: 'p-1',
          academicYearId: 'ay-1',
          periodCode: 'SEM-1-2026',
          name: 'Semester 1',
          periodType: 'semester',
          startDate: '2026-07-15',
          endDate: '2026-12-20',
          parentPeriodId: null,
          campusContext: 'All',
          status: 'active',
        },
      ];
      vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue(mockPeriods);

      const ayId = ref<string | null>(null);
      const { result } = withSetup(() => useOperationalPeriodsQuery(ayId));

      expect(result.isFetching.value).toBe(false);

      ayId.value = 'ay-1';
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(operationalPeriodApi.getPeriods).toHaveBeenCalledWith('ay-1');
      expect(result.data.value).toEqual(mockPeriods);
    });
  });

  describe('useCreateOperationalPeriodMutation', () => {
    it('creates period, invalidates operationalPeriods and calendarMatrix caches, and displays toast', async () => {
      const createdPeriod: operationalPeriodApi.OperationalPeriodDto = {
        id: 'p-2',
        academicYearId: 'ay-1',
        periodCode: 'TERM-1-2026',
        name: 'Term 1',
        periodType: 'term',
        startDate: '2026-07-15',
        endDate: '2026-09-30',
        parentPeriodId: 'p-1',
        campusContext: 'All',
        status: 'draft',
      };
      vi.spyOn(operationalPeriodApi, 'createPeriod').mockResolvedValue(createdPeriod);

      const onSuccessSpy = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useCreateOperationalPeriodMutation({ onSuccess: onSuccessSpy }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        academicYearId: 'ay-1',
        payload: {
          periodCode: 'TERM-1-2026',
          name: 'Term 1',
          periodType: 'term',
          startDate: '2026-07-15',
          endDate: '2026-09-30',
        },
      });

      expect(operationalPeriodApi.createPeriod).toHaveBeenCalledWith('ay-1', expect.anything());
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: operationalPeriodKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Period Created',
        }),
      );
      expect(onSuccessSpy).toHaveBeenCalledWith(createdPeriod, expect.anything());
    });
  });

  describe('useUpdateOperationalPeriodMutation', () => {
    it('updates period and invalidates operationalPeriods and calendarMatrix caches', async () => {
      const updatedPeriod: operationalPeriodApi.OperationalPeriodDto = {
        id: 'p-1',
        academicYearId: 'ay-1',
        periodCode: 'SEM-1-2026',
        name: 'Semester 1 Renamed',
        periodType: 'semester',
        startDate: '2026-07-15',
        endDate: '2026-12-20',
        parentPeriodId: null,
        campusContext: 'All',
        status: 'draft',
      };
      vi.spyOn(operationalPeriodApi, 'updatePeriod').mockResolvedValue(updatedPeriod);

      const { result, queryClient } = withSetup(() => useUpdateOperationalPeriodMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'p-1',
        payload: {
          name: 'Semester 1 Renamed',
          startDate: '2026-07-15',
          endDate: '2026-12-20',
        },
      });

      expect(operationalPeriodApi.updatePeriod).toHaveBeenCalledWith('p-1', expect.anything());
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: operationalPeriodKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Period Updated',
        }),
      );
    });
  });

  describe('useCloseOperationalPeriodMutation', () => {
    it('closes period and triggers cache invalidations and toast', async () => {
      const closedPeriod: operationalPeriodApi.OperationalPeriodDto = {
        id: 'p-1',
        academicYearId: 'ay-1',
        periodCode: 'SEM-1-2026',
        name: 'Semester 1',
        periodType: 'semester',
        startDate: '2026-07-15',
        endDate: '2026-12-20',
        parentPeriodId: null,
        campusContext: 'All',
        status: 'closed',
      };
      vi.spyOn(operationalPeriodApi, 'closePeriod').mockResolvedValue(closedPeriod);

      const { result, queryClient } = withSetup(() => useCloseOperationalPeriodMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('p-1');

      expect(operationalPeriodApi.closePeriod).toHaveBeenCalledWith('p-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: operationalPeriodKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Period Closed',
        }),
      );
    });
  });
});
