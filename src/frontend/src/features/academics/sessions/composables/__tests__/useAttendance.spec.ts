// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as sessionsApi from '../../api/sessions.api';
import {
  attendanceKeys,
  useActivityAttendanceQuery,
  useCorrectAttendanceMutation,
  useRecordAttendanceMutation,
  useStudentAttendanceSummaryQuery,
} from '../useAttendance';
import { learningActivityKeys } from '../useLearningActivities';

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

describe('useAttendance Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('attendanceKeys', () => {
    it('builds standard attendance query keys', () => {
      expect(attendanceKeys.all).toEqual(['academics', 'attendance']);
      expect(attendanceKeys.activityAttendance('act-1')).toEqual([
        'academics',
        'attendance',
        'activity',
        'act-1',
      ]);
      expect(attendanceKeys.studentSummary('stu-1', 'period-1')).toEqual([
        'academics',
        'attendance',
        'student-summary',
        'stu-1',
        { academicPeriodId: 'period-1' },
      ]);
    });
  });

  describe('queries', () => {
    it('useActivityAttendanceQuery fetches session attendance roster', async () => {
      const mockRecords = [
        {
          id: 'att-1',
          learningActivityId: 'act-1',
          studentId: 'stu-1',
          studentName: 'Alice Johnson',
          studentIdentifier: 'STU-001',
          status: 'present' as const,
          recordedAt: '2026-11-05T09:05:00Z',
          recordedBy: 'teacher-1',
        },
      ];
      vi.spyOn(sessionsApi, 'getActivityAttendance').mockResolvedValue(mockRecords);

      const activityId = ref('act-1');
      const { result } = withSetup(() => useActivityAttendanceQuery(activityId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockRecords);
      expect(sessionsApi.getActivityAttendance).toHaveBeenCalledWith('act-1');
    });

    it('useStudentAttendanceSummaryQuery fetches summary statistics', async () => {
      const mockSummary = {
        studentId: 'stu-1',
        presentCount: 20,
        absentCount: 1,
        tardyCount: 2,
        excusedCount: 1,
        totalSessions: 24,
        attendancePercentage: 83.33,
      };
      vi.spyOn(sessionsApi, 'getStudentAttendanceSummary').mockResolvedValue(mockSummary);

      const studentId = ref('stu-1');
      const { result } = withSetup(() =>
        useStudentAttendanceSummaryQuery(studentId, 'period-1'),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockSummary);
      expect(sessionsApi.getStudentAttendanceSummary).toHaveBeenCalledWith('stu-1', 'period-1');
    });
  });

  describe('mutations and invalidation', () => {
    it('useRecordAttendanceMutation records live roll call and invalidates caches', async () => {
      const mockResponse = {
        activityId: 'act-1',
        recordsUpserted: 25,
        recordedAt: '2026-11-05T09:10:00Z',
      };
      vi.spyOn(sessionsApi, 'recordSessionAttendance').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useRecordAttendanceMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        activityId: 'act-1',
        request: {
          attendance: [
            { studentId: 'stu-1', status: 'present' },
            { studentId: 'stu-2', status: 'absent' },
          ],
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: attendanceKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: attendanceKeys.activityAttendance('act-1'),
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: learningActivityKeys.detail('act-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Attendance Recorded',
        }),
      );
    });

    it('useCorrectAttendanceMutation submits correction and invalidates audit caches', async () => {
      const mockResponse = {
        id: 'cor-1',
        attendanceRecordId: 'att-1',
        previousStatus: 'absent' as const,
        newStatus: 'excused' as const,
        justificationReason: 'Medical note submitted by parent.',
        correctedBy: 'admin-1',
        correctedAt: '2026-11-05T14:00:00Z',
      };
      vi.spyOn(sessionsApi, 'correctAttendance').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useCorrectAttendanceMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        attendanceRecordId: 'att-1',
        activityId: 'act-1',
        studentId: 'stu-1',
        request: {
          newStatus: 'excused',
          justificationReason: 'Medical note submitted by parent.',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: attendanceKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: attendanceKeys.activityAttendance('act-1'),
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: attendanceKeys.studentSummary('stu-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Attendance Corrected',
        }),
      );
    });
  });
});
