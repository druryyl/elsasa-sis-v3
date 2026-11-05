// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as courseworkApi from '../../api/coursework.api';
import {
  assignmentKeys,
  useActivityAssignmentsQuery,
  useAssignmentSubmissionsQuery,
  useCloseAssignmentMutation,
  useCreateAssignmentMutation,
  useRecordExceptionMutation,
  useReleaseAssignmentMutation,
  useUnitAssignmentsQuery,
} from '../useAssignments';

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

describe('useAssignments Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('assignmentKeys', () => {
    it('builds standard assignment query keys', () => {
      expect(assignmentKeys.all).toEqual(['academics', 'assignments']);
      expect(assignmentKeys.activityAssignments('act-1')).toEqual([
        'academics',
        'assignments',
        'activity',
        'act-1',
      ]);
      expect(assignmentKeys.unitAssignments('unit-1')).toEqual([
        'academics',
        'assignments',
        'unit',
        'unit-1',
      ]);
      expect(assignmentKeys.submissions('asg-1')).toEqual([
        'academics',
        'assignments',
        'submissions',
        'asg-1',
      ]);
    });
  });

  describe('queries', () => {
    it('useActivityAssignmentsQuery fetches assignments under activity', async () => {
      const mockAssignments = [
        {
          id: 'asg-1',
          learningActivityId: 'act-1',
          learningUnitId: 'unit-1',
          title: 'Essay 1',
          status: 'released' as const,
          maxScore: 100,
          createdAt: '2026-11-05',
          activityTitle: 'Class Lecture',
          learningUnitName: 'Biology',
        },
      ];
      vi.spyOn(courseworkApi, 'getAssignmentsByActivity').mockResolvedValue(mockAssignments);

      const activityId = ref('act-1');
      const { result } = withSetup(() => useActivityAssignmentsQuery(activityId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockAssignments);
      expect(courseworkApi.getAssignmentsByActivity).toHaveBeenCalledWith('act-1');
    });

    it('useUnitAssignmentsQuery fetches assignments under unit', async () => {
      const mockAssignments = [
        {
          id: 'asg-1',
          learningActivityId: 'act-1',
          learningUnitId: 'unit-1',
          title: 'Essay 1',
          status: 'released' as const,
          maxScore: 100,
          createdAt: '2026-11-05',
          activityTitle: 'Class Lecture',
          learningUnitName: 'Biology',
        },
      ];
      vi.spyOn(courseworkApi, 'getAssignmentsByUnit').mockResolvedValue(mockAssignments);

      const unitId = ref('unit-1');
      const { result } = withSetup(() => useUnitAssignmentsQuery(unitId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockAssignments);
      expect(courseworkApi.getAssignmentsByUnit).toHaveBeenCalledWith('unit-1');
    });

    it('useAssignmentSubmissionsQuery fetches submissions with dynamic missing projection', async () => {
      const mockSubmissions = [
        {
          studentId: 'stu-1',
          studentName: 'Alice Johnson',
          studentIdentifier: 'STU-001',
          submissionId: null,
          assignmentId: 'asg-1',
          status: null,
          displayStatus: 'missing' as const,
          submissionReference: null,
          submittedAt: null,
          versionNumber: 0,
          isLate: false,
          previousSubmissionId: null,
        },
      ];
      vi.spyOn(courseworkApi, 'getAssignmentSubmissions').mockResolvedValue(mockSubmissions);

      const assignmentId = ref('asg-1');
      const { result } = withSetup(() => useAssignmentSubmissionsQuery(assignmentId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockSubmissions);
      expect(courseworkApi.getAssignmentSubmissions).toHaveBeenCalledWith('asg-1');
    });
  });

  describe('mutations and invalidation', () => {
    it('useCreateAssignmentMutation creates draft assignment and invalidates caches', async () => {
      const mockResponse = {
        id: 'asg-new',
        title: 'Project Proposal',
        status: 'draft' as const,
      };
      vi.spyOn(courseworkApi, 'createAssignment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useCreateAssignmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        activityId: 'act-1',
        request: {
          title: 'Project Proposal',
          maxScore: 100,
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.activityAssignments('act-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assignment Created',
        }),
      );
    });

    it('useReleaseAssignmentMutation publishes assignment and invalidates caches', async () => {
      const mockResponse = {
        id: 'asg-1',
        status: 'released' as const,
        releasedAt: '2026-11-05T12:00:00Z',
      };
      vi.spyOn(courseworkApi, 'releaseAssignment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useReleaseAssignmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate('asg-1');

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.submissions('asg-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assignment Released',
        }),
      );
    });

    it('useCloseAssignmentMutation closes assignment and invalidates caches', async () => {
      const mockResponse = {
        id: 'asg-1',
        status: 'closed' as const,
        closedAt: '2026-11-05T18:00:00Z',
      };
      vi.spyOn(courseworkApi, 'closeAssignment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useCloseAssignmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate('asg-1');

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.submissions('asg-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assignment Closed',
        }),
      );
    });

    it('useRecordExceptionMutation records exception and invalidates submissions cache', async () => {
      const mockResponse = {
        id: 'sub-exc',
        assignmentId: 'asg-1',
        studentId: 'stu-1',
        exceptionStatus: 'excused' as const,
      };
      vi.spyOn(courseworkApi, 'recordSubmissionException').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useRecordExceptionMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        assignmentId: 'asg-1',
        request: {
          studentId: 'stu-1',
          exceptionStatus: 'excused',
          reason: 'Medical exemption approved.',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: assignmentKeys.submissions('asg-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Exception Recorded',
        }),
      );
    });
  });
});
