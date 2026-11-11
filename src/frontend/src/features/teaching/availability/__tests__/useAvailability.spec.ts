// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as availabilityApi from '../api/availabilityApi';
import {
  useTeacherAvailability,
  useUnavailabilityWindows,
  useDeclareAvailabilityMutation,
  useCreateUnavailabilityMutation,
  useCancelUnavailabilityMutation,
} from '../composables/useAvailability';

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

describe('Teaching Availability Composables (P7-S23)', () => {
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const windowId = '22222222-2222-2222-2222-222222222222';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Query Hooks', () => {
    it('useTeacherAvailability queries teacher availability successfully', async () => {
      const mockAvailability = {
        id: '33333333-3333-3333-3333-333333333333',
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        maxAvailableHours: 24,
        campusScope: 'All',
        availableDeliveryModes: 'in_person',
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        status: 'active',
        createdAt: '2026-07-01T00:00:00Z',
      };

      const spy = vi
        .spyOn(availabilityApi, 'getTeacherAvailability')
        .mockResolvedValueOnce(mockAvailability as never);

      const { result } = withSetup(() => useTeacherAvailability(teacherId, periodId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockAvailability);
      expect(spy).toHaveBeenCalledWith(teacherId, periodId);
    });

    it('useTeacherAvailability remains disabled when teacherId is null or empty', () => {
      const emptyId = ref<string | null>(null);
      const { result } = withSetup(() => useTeacherAvailability(emptyId, periodId));

      expect(result.fetchStatus.value).toBe('idle');
    });

    it('useUnavailabilityWindows queries leave windows successfully', async () => {
      const mockWindows = [
        {
          id: windowId,
          teacherProfileId: teacherId,
          startDate: '2026-09-01',
          endDate: '2026-09-05',
          unavailabilityType: 'medical_leave',
          reason: 'Recovery',
          status: 'active',
          createdAt: '2026-08-30T00:00:00Z',
        },
      ];

      const spy = vi
        .spyOn(availabilityApi, 'getTeacherUnavailabilityWindows')
        .mockResolvedValueOnce(mockWindows as never);

      const params = { status: 'active' };
      const { result } = withSetup(() => useUnavailabilityWindows(teacherId, params));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockWindows);
      expect(spy).toHaveBeenCalledWith(teacherId, params);
    });
  });

  describe('Mutation Hooks', () => {
    it('useDeclareAvailabilityMutation executes and invalidates cache', async () => {
      const mockResponse = {
        id: '44444444-4444-4444-4444-444444444444',
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        maxAvailableHours: 20,
        status: 'active',
        supersededPreviousCount: 0,
        createdAt: '2026-07-01T00:00:00Z',
      };

      const spy = vi
        .spyOn(availabilityApi, 'declareTeacherAvailability')
        .mockResolvedValueOnce(mockResponse as never);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useDeclareAvailabilityMutation({ onSuccess }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        academicPeriodId: periodId,
        maxAvailableHours: 20,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
      };

      await result.mutateAsync({ teacherId, payload });

      expect(spy).toHaveBeenCalledWith(teacherId, payload);
      expect(invalidateSpy).toHaveBeenCalled();
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, { teacherId, payload });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Availability Declared',
        }),
      );
    });

    it('useCreateUnavailabilityMutation executes and notifies success', async () => {
      const mockResponse = {
        id: windowId,
        teacherProfileId: teacherId,
        startDate: '2026-10-01',
        endDate: '2026-10-03',
        unavailabilityType: 'official_training',
        status: 'active',
        affectedActiveAssignmentCount: 1,
        createdAt: '2026-09-25T00:00:00Z',
      };

      const spy = vi
        .spyOn(availabilityApi, 'createUnavailabilityWindow')
        .mockResolvedValueOnce(mockResponse as never);

      const { result, queryClient } = withSetup(() => useCreateUnavailabilityMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        startDate: '2026-10-01',
        endDate: '2026-10-03',
        unavailabilityType: 'official_training',
        reason: 'Conference',
      };

      await result.mutateAsync({ teacherId, payload });

      expect(spy).toHaveBeenCalledWith(teacherId, payload);
      expect(invalidateSpy).toHaveBeenCalled();
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Leave Window Recorded',
        }),
      );
    });

    it('useCancelUnavailabilityMutation executes and invalidates cache', async () => {
      const mockResponse = {
        id: windowId,
        teacherProfileId: teacherId,
        status: 'cancelled',
        cancelledAt: '2026-10-01T12:00:00Z',
      };

      const spy = vi
        .spyOn(availabilityApi, 'cancelUnavailabilityWindow')
        .mockResolvedValueOnce(mockResponse as never);

      const { result, queryClient } = withSetup(() => useCancelUnavailabilityMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ windowId, teacherId });

      expect(spy).toHaveBeenCalledWith(windowId);
      expect(invalidateSpy).toHaveBeenCalled();
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Window Cancelled',
        }),
      );
    });
  });
});
