// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as sessionsApi from '../../api/sessions.api';
import {
  learningActivityKeys,
  useActivityDetailQuery,
  useCancelActivityMutation,
  useConductActivityMutation,
  usePlanActivityMutation,
  useTeacherDailyActivitiesQuery,
  useUnitActivitiesQuery,
} from '../useLearningActivities';

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

describe('useLearningActivities Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('learningActivityKeys', () => {
    it('builds standard query keys', () => {
      expect(learningActivityKeys.all).toEqual(['academics', 'activities']);
      expect(
        learningActivityKeys.unitActivities('unit-1', { sectionLabel: 'A' }),
      ).toEqual(['academics', 'activities', 'unit', 'unit-1', { sectionLabel: 'A' }]);
      expect(learningActivityKeys.teacherDaily('2026-11-05')).toEqual([
        'academics',
        'activities',
        'teacher',
        '2026-11-05',
      ]);
      expect(learningActivityKeys.detail('act-1')).toEqual([
        'academics',
        'activities',
        'detail',
        'act-1',
      ]);
    });
  });

  describe('queries', () => {
    it('useUnitActivitiesQuery fetches activities for learning unit', async () => {
      const mockActivities = [
        {
          id: 'act-1',
          learningUnitId: 'unit-1',
          title: 'Cell Mitosis Lecture',
          activityType: 'instructional_session' as const,
          activityDate: '2026-11-05',
          startTime: '09:00:00',
          endTime: '10:00:00',
          status: 'scheduled' as const,
          sectionLabel: 'A',
          teacherName: 'Dr. Jane Smith',
        },
      ];
      vi.spyOn(sessionsApi, 'getActivitiesByUnit').mockResolvedValue(mockActivities);

      const unitId = ref('unit-1');
      const { result } = withSetup(() => useUnitActivitiesQuery(unitId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockActivities);
      expect(sessionsApi.getActivitiesByUnit).toHaveBeenCalledWith('unit-1', undefined);
    });

    it('useTeacherDailyActivitiesQuery fetches teacher daily schedule', async () => {
      const mockTeacherDaily = [
        {
          id: 'act-1',
          learningUnitId: 'unit-1',
          learningUnitName: 'Biology 101',
          learningUnitCode: 'BIO-101',
          title: 'Cell Mitosis',
          activityType: 'instructional_session' as const,
          activityDate: '2026-11-05',
          startTime: '09:00:00',
          endTime: '10:00:00',
          status: 'scheduled' as const,
          sectionLabel: 'A',
          attendanceCount: 0,
          assignmentCount: 1,
        },
      ];
      vi.spyOn(sessionsApi, 'getTeacherDailyActivities').mockResolvedValue(mockTeacherDaily);

      const date = ref('2026-11-05');
      const { result } = withSetup(() => useTeacherDailyActivitiesQuery(date));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockTeacherDaily);
      expect(sessionsApi.getTeacherDailyActivities).toHaveBeenCalledWith('2026-11-05');
    });

    it('useActivityDetailQuery fetches detailed activity inspection', async () => {
      const mockDetail = {
        id: 'act-1',
        learningUnitId: 'unit-1',
        title: 'Cell Mitosis Lecture',
        activityType: 'instructional_session' as const,
        activityDate: '2026-11-05',
        startTime: '09:00:00',
        endTime: '10:00:00',
        status: 'conducted' as const,
        sectionLabel: 'A',
        teacherName: 'Dr. Jane Smith',
        conductedAt: '2026-11-05T10:05:00Z',
        deliveryNotes: 'Completed mitosis phases 1-3.',
        attendanceCount: 24,
        assignments: [],
      };
      vi.spyOn(sessionsApi, 'getActivityById').mockResolvedValue(mockDetail);

      const id = ref('act-1');
      const { result } = withSetup(() => useActivityDetailQuery(id));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockDetail);
      expect(sessionsApi.getActivityById).toHaveBeenCalledWith('act-1');
    });
  });

  describe('mutations and invalidation', () => {
    it('usePlanActivityMutation schedules activity, invalidates cache and shows toast', async () => {
      const mockResponse = {
        id: 'act-new',
        title: 'Chemistry Lab 1',
        activityType: 'instructional_session' as const,
        activityDate: '2026-11-06',
        status: 'scheduled' as const,
      };
      vi.spyOn(sessionsApi, 'planLearningActivity').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => usePlanActivityMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        learningUnitId: 'unit-1',
        title: 'Chemistry Lab 1',
        activityType: 'instructional_session',
        activityDate: '2026-11-06',
        startTime: '10:00',
        endTime: '11:00',
        sectionLabel: 'A',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: learningActivityKeys.all,
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Activity Scheduled',
        }),
      );
    });

    it('useConductActivityMutation conducts session and invalidates detail cache', async () => {
      const mockResponse = {
        id: 'act-1',
        status: 'conducted' as const,
        conductedAt: '2026-11-05T10:00:00Z',
      };
      vi.spyOn(sessionsApi, 'conductLearningActivity').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useConductActivityMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        id: 'act-1',
        request: { deliveryNotes: 'Finished discussion.', curriculumMilestones: ['M1'] },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: learningActivityKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: learningActivityKeys.detail('act-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Activity Conducted',
        }),
      );
    });

    it('useCancelActivityMutation cancels activity and invalidates caches', async () => {
      const mockResponse = {
        id: 'act-1',
        status: 'cancelled' as const,
      };
      vi.spyOn(sessionsApi, 'cancelLearningActivity').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useCancelActivityMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        id: 'act-1',
        request: { cancellationReason: 'Inclement weather.' },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: learningActivityKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: learningActivityKeys.detail('act-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Activity Cancelled',
        }),
      );
    });
  });
});
