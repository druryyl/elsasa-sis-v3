// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as closureApi from '../../api/closure.api';
import {
  useSchoolClosuresQuery,
  useDeclareSchoolClosureMutation,
  useLiftSchoolClosureMutation,
} from '../useSchoolClosuresQuery';
import { schoolClosureKeys, calendarKeys, amendmentKeys } from '../queryKeys';

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

describe('School Closures Composables (useSchoolClosuresQuery.ts)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useSchoolClosuresQuery', () => {
    it('fetches closures for an academic year reactively', async () => {
      const mockClosures: closureApi.SchoolClosureDto[] = [
        {
          id: 'closure-1',
          academicYearId: 'ay-1',
          closureNumber: 'CLS-2026-001',
          reasonCategory: 'weather',
          reasonDetails: 'Severe flood warning',
          startDate: '2026-09-10',
          endDate: '2026-09-12',
          affectedCampus: 'All',
          affectedPopulation: 'all',
          status: 'authorized',
          authorizedBy: 'Authority',
          authorizedDate: '2026-09-09',
          communicationStatus: 'dispatched',
        },
      ];
      vi.spyOn(closureApi, 'getClosures').mockResolvedValue(mockClosures);

      const ayId = ref<string | null>(null);
      const { result } = withSetup(() => useSchoolClosuresQuery(ayId));

      expect(result.isFetching.value).toBe(false);

      ayId.value = 'ay-1';
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(closureApi.getClosures).toHaveBeenCalledWith('ay-1');
      expect(result.data.value).toEqual(mockClosures);
    });
  });

  describe('useDeclareSchoolClosureMutation', () => {
    it('declares closure and automatically invalidates closures, matrix projection, amendments, and calendars', async () => {
      const declaredClosure: closureApi.SchoolClosureDto = {
        id: 'closure-1',
        academicYearId: 'ay-1',
        closureNumber: 'CLS-2026-001',
        reasonCategory: 'health_emergency',
        reasonDetails: 'Campus sanitation deep clean',
        startDate: '2026-10-01',
        endDate: '2026-10-02',
        affectedCampus: 'Main',
        affectedPopulation: 'all',
        status: 'authorized',
        authorizedBy: 'Operations Authority',
        authorizedDate: '2026-09-30',
        communicationStatus: 'draft',
      };
      vi.spyOn(closureApi, 'declareClosure').mockResolvedValue(declaredClosure);

      const { result, queryClient } = withSetup(() => useDeclareSchoolClosureMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        academicYearId: 'ay-1',
        reasonCategory: 'health_emergency',
        reasonDetails: 'Campus sanitation deep clean',
        startDate: '2026-10-01',
        endDate: '2026-10-02',
        affectedCampus: 'Main',
      });

      expect(closureApi.declareClosure).toHaveBeenCalled();

      // Verify complete cache invalidation pipeline
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: schoolClosureKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: amendmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Campus Closure Declared',
          detail: 'Closure CLS-2026-001 declared successfully.',
        }),
      );
    });
  });

  describe('useLiftSchoolClosureMutation', () => {
    it('lifts closure and triggers complete invalidation pipeline', async () => {
      const liftedClosure: closureApi.SchoolClosureDto = {
        id: 'closure-1',
        academicYearId: 'ay-1',
        closureNumber: 'CLS-2026-001',
        reasonCategory: 'weather',
        reasonDetails: 'Weather cleared',
        startDate: '2026-09-10',
        endDate: '2026-09-12',
        affectedCampus: 'All',
        affectedPopulation: 'all',
        status: 'lifted',
        authorizedBy: 'Authority',
        authorizedDate: '2026-09-09',
        communicationStatus: 'dispatched',
      };
      vi.spyOn(closureApi, 'liftClosure').mockResolvedValue(liftedClosure);

      const { result, queryClient } = withSetup(() => useLiftSchoolClosureMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('closure-1');

      expect(closureApi.liftClosure).toHaveBeenCalledWith('closure-1');

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: schoolClosureKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: amendmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Closure Lifted',
        }),
      );
    });
  });
});
