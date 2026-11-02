// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { ApiError } from '@/shared/types/common';
import * as academicYearApi from '../../api/academicYear.api';
import {
  useAcademicYearsQuery,
  useAcademicYearDetailQuery,
  useCreateAcademicYearMutation,
  useUpdateAcademicYearMutation,
  useApproveAcademicYearMutation,
  useActivateAcademicYearMutation,
  useApplyPresetMutation,
} from '../useAcademicYearsQuery';
import { academicYearKeys, operationalPeriodKeys, calendarKeys } from '../queryKeys';

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

describe('Academic Years Composables (useAcademicYearsQuery.ts)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useAcademicYearsQuery', () => {
    it('executes getAcademicYears query with reactive parameters', async () => {
      const mockList: academicYearApi.AcademicYearDto[] = [
        {
          id: 'ay-1',
          code: 'AY-2026-2027',
          name: 'Academic Year 2026/2027',
          startDate: '2026-07-01',
          endDate: '2027-06-30',
          status: 'active',
          isActive: true,
        },
      ];
      vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockList);

      const statusFilter = ref<{ status?: string }>({ status: 'active' });
      const { result } = withSetup(() => useAcademicYearsQuery(statusFilter));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(academicYearApi.getAcademicYears).toHaveBeenCalledWith({ status: 'active' });
      expect(result.data.value).toEqual(mockList);
    });
  });

  describe('useAcademicYearDetailQuery', () => {
    it('fetches single academic year when ID is present and respects enabled state', async () => {
      const mockDetail: academicYearApi.AcademicYearDto = {
        id: 'ay-1',
        code: 'AY-2026-2027',
        name: 'Academic Year 2026/2027',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'draft',
      };
      vi.spyOn(academicYearApi, 'getAcademicYearById').mockResolvedValue(mockDetail);

      const targetId = ref<string | null>(null);
      const { result } = withSetup(() => useAcademicYearDetailQuery(targetId));

      expect(result.isFetching.value).toBe(false);
      expect(academicYearApi.getAcademicYearById).not.toHaveBeenCalled();

      targetId.value = 'ay-1';
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(academicYearApi.getAcademicYearById).toHaveBeenCalledWith('ay-1');
      expect(result.data.value).toEqual(mockDetail);
    });
  });

  describe('useCreateAcademicYearMutation', () => {
    it('creates academic year, invalidates academicYears query cache and displays success toast', async () => {
      const createdItem: academicYearApi.AcademicYearDto = {
        id: 'ay-new',
        code: 'AY-2027-2028',
        name: 'Academic Year 2027/2028',
        startDate: '2027-07-01',
        endDate: '2028-06-30',
        status: 'draft',
      };
      vi.spyOn(academicYearApi, 'createAcademicYear').mockResolvedValue(createdItem);

      const onSuccessSpy = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useCreateAcademicYearMutation({ onSuccess: onSuccessSpy }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        code: 'AY-2027-2028',
        name: 'Academic Year 2027/2028',
        startDate: '2027-07-01',
        endDate: '2028-06-30',
      });

      expect(academicYearApi.createAcademicYear).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: academicYearKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Year Created',
        }),
      );
      expect(onSuccessSpy).toHaveBeenCalledWith(
        createdItem,
        expect.objectContaining({ code: 'AY-2027-2028' }),
      );
    });

    it('handles ApiError with validation errors and displays error toast', async () => {
      const apiError = new ApiError(422, 'Validation Error', 'Start date cannot precede end date', 'VAL_01', {
        startDate: ['Invalid date sequence'],
      });
      vi.spyOn(academicYearApi, 'createAcademicYear').mockRejectedValue(apiError);

      const onErrorSpy = vi.fn();
      const { result } = withSetup(() => useCreateAcademicYearMutation({ onError: onErrorSpy }));

      await expect(
        result.mutateAsync({
          code: 'AY-INV',
          name: 'Invalid Year',
          startDate: '2027-07-01',
          endDate: '2026-06-30',
        }),
      ).rejects.toThrow();

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Validation Error',
        }),
      );
      expect(onErrorSpy).toHaveBeenCalledWith(apiError, expect.anything());
    });
  });

  describe('useUpdateAcademicYearMutation', () => {
    it('updates academic year and invalidates academicYears query cache', async () => {
      const updatedItem: academicYearApi.AcademicYearDto = {
        id: 'ay-1',
        code: 'AY-2026-2027',
        name: 'Updated Name',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'draft',
      };
      vi.spyOn(academicYearApi, 'updateAcademicYear').mockResolvedValue(updatedItem);

      const { result, queryClient } = withSetup(() => useUpdateAcademicYearMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'ay-1',
        payload: { name: 'Updated Name', startDate: '2026-07-01', endDate: '2027-06-30' },
      });

      expect(academicYearApi.updateAcademicYear).toHaveBeenCalledWith('ay-1', {
        name: 'Updated Name',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: academicYearKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Year Updated',
        }),
      );
    });
  });

  describe('useApproveAcademicYearMutation', () => {
    it('approves academic year and invalidates cache', async () => {
      const approvedItem: academicYearApi.AcademicYearDto = {
        id: 'ay-1',
        code: 'AY-2026-2027',
        name: 'AY',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'approved',
      };
      vi.spyOn(academicYearApi, 'approveAcademicYear').mockResolvedValue(approvedItem);

      const { result, queryClient } = withSetup(() => useApproveAcademicYearMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ id: 'ay-1', payload: { approvedBy: 'Authority' } });

      expect(academicYearApi.approveAcademicYear).toHaveBeenCalledWith('ay-1', {
        approvedBy: 'Authority',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: academicYearKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Year Approved',
        }),
      );
    });
  });

  describe('useActivateAcademicYearMutation', () => {
    it('activates academic year and invalidates cache', async () => {
      const activatedItem: academicYearApi.AcademicYearDto = {
        id: 'ay-1',
        code: 'AY-2026-2027',
        name: 'AY',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'active',
      };
      vi.spyOn(academicYearApi, 'activateAcademicYear').mockResolvedValue(activatedItem);

      const { result, queryClient } = withSetup(() => useActivateAcademicYearMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('ay-1');

      expect(academicYearApi.activateAcademicYear).toHaveBeenCalledWith('ay-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: academicYearKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Year Activated',
        }),
      );
    });
  });

  describe('useApplyPresetMutation', () => {
    it('applies preset and invalidates academicYears, operationalPeriods, and calendarMatrix caches', async () => {
      const presetResult: academicYearApi.ApplyPresetResultDto = {
        academicYearId: 'ay-1',
        presetName: 'Indonesian 2-Semester',
        periodsCreated: 2,
      };
      vi.spyOn(academicYearApi, 'applyPreset').mockResolvedValue(presetResult);

      const { result, queryClient } = withSetup(() => useApplyPresetMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('ay-1');

      expect(academicYearApi.applyPreset).toHaveBeenCalledWith('ay-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: academicYearKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: operationalPeriodKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Preset Applied',
        }),
      );
    });
  });
});
