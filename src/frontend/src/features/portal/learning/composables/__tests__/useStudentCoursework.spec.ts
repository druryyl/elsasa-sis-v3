// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as learningApi from '../../api/learning.api';
import {
  studentCourseworkKeys,
  useStudentCourseworkWorkspaceQuery,
  useSubmitCourseworkMutation,
  useUploadSubmissionFileMutation,
} from '../useStudentCoursework';

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

describe('useStudentCoursework Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('studentCourseworkKeys', () => {
    it('builds standard portal coursework query keys', () => {
      expect(studentCourseworkKeys.all).toEqual(['portal', 'coursework']);
      expect(studentCourseworkKeys.workspace('assigned')).toEqual([
        'portal',
        'coursework',
        'workspace',
        'assigned',
      ]);
      expect(studentCourseworkKeys.workspace()).toEqual([
        'portal',
        'coursework',
        'workspace',
      ]);
    });
  });

  describe('queries', () => {
    it('useStudentCourseworkWorkspaceQuery fetches coursework tasks', async () => {
      const mockTasks = [
        {
          assignmentId: 'asg-1',
          title: 'Math Problem Set',
          learningUnitName: 'Calculus I',
          learningUnitCode: 'MATH-201',
          dueDate: '2026-11-10T23:59:59Z',
          maxScore: 100,
          assignmentStatus: 'released' as const,
          submissionId: null,
          submissionStatus: null,
          displayStatus: 'missing' as const,
          submissionReference: null,
          submittedAt: null,
          versionNumber: 0,
          isLate: false,
        },
      ];
      vi.spyOn(learningApi, 'getStudentCourseworkWorkspace').mockResolvedValue(mockTasks);

      const statusFilter = ref('missing');
      const { result } = withSetup(() =>
        useStudentCourseworkWorkspaceQuery(statusFilter),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockTasks);
      expect(learningApi.getStudentCourseworkWorkspace).toHaveBeenCalledWith('missing', undefined);
    });
  });

  describe('mutations and invalidation', () => {
    it('useSubmitCourseworkMutation submits response and invalidates workspace and assignments cache', async () => {
      const mockResponse = {
        submissionId: 'sub-new',
        assignmentId: 'asg-1',
        studentId: 'stu-1',
        submissionStatus: 'submitted' as const,
        submittedAt: '2026-11-05T15:00:00Z',
        versionNumber: 1,
        isLate: false,
      };
      vi.spyOn(learningApi, 'submitCoursework').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useSubmitCourseworkMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        assignmentId: 'asg-1',
        request: {
          submissionReference: 'https://github.com/student/repo',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: studentCourseworkKeys.all,
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'assignments'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Coursework Submitted',
        }),
      );
    });

    it('useUploadSubmissionFileMutation uploads file and shows success toast', async () => {
      const mockResponse = {
        filePath: 'uploads/202611/test.pdf',
        originalFileName: 'test.pdf',
        fileSizeBytes: 1024,
        contentType: 'application/pdf',
      };
      vi.spyOn(learningApi, 'uploadSubmissionFile').mockResolvedValue(mockResponse);

      const { result } = withSetup(() => useUploadSubmissionFileMutation());

      const dummyFile = new File(['content'], 'test.pdf', { type: 'application/pdf' });
      result.mutate({
        file: dummyFile,
        studentId: 'stu-1',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'File Uploaded',
        }),
      );
    });
  });
});
