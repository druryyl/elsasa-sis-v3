// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { learningProgressApi } from '../../api/learningProgressApi';
import {
  concludeParticipationMutation,
  learningProgressKeys,
  recordProgressMutation,
  useConcludeParticipationMutation,
  useLearningProgress,
  useRecordProgressMutation,
  useStudentProgressQuery,
  useUnitProgressQuery,
} from '../useLearningProgress';
import { reportCardKeys } from '../../constants/queryKeys';
import type {
  ConcludeParticipationResponse,
  LearningProgress,
} from '../../types';

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

describe('useLearningProgress Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('learningProgressKeys', () => {
    it('builds standard query keys matching completion criteria', () => {
      expect(learningProgressKeys.all).toEqual(['academics', 'learning-progress']);
      expect(
        learningProgressKeys.byStudent('stu-1', 'period-2026-s1'),
      ).toEqual([
        'academics',
        'learning-progress',
        'stu-1',
        'period-2026-s1',
      ]);
      expect(learningProgressKeys.byUnit('unit-101')).toEqual([
        'academics',
        'learning-progress',
        'unit',
        'unit-101',
      ]);
    });
  });

  describe('useStudentProgressQuery', () => {
    it('disables query when studentId is empty or null', () => {
      const studentId = ref<string | null>(null);
      const { result } = withSetup(() => useStudentProgressQuery(studentId));
      expect(result.isEnabled.value).toBe(false);
    });

    it('enables query and fetches student learning progress records', async () => {
      const mockRecords: LearningProgress[] = [
        {
          id: 'prog-1',
          studentId: 'student-1',
          studentName: 'Alice Smith',
          learningUnitId: 'unit-1',
          learningUnitCode: 'MATH101',
          learningUnitTitle: 'Mathematics 10',
          academicPeriodId: 'period-1',
          progressStatus: 'completed',
          basis: 'assessment_evidence',
          completionDate: '2026-06-15',
          notes: null,
          recordedAt: '2026-06-15T10:00:00Z',
          recordedBy: 'teacher-1',
        },
      ];

      vi.spyOn(learningProgressApi, 'getStudentProgress').mockResolvedValue(mockRecords);

      const studentId = ref('student-1');
      const periodId = ref('period-1');
      const { result } = withSetup(() => useStudentProgressQuery(studentId, periodId));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockRecords);
      expect(learningProgressApi.getStudentProgress).toHaveBeenCalledWith(
        'student-1',
        'period-1',
      );
    });
  });

  describe('useUnitProgressQuery', () => {
    it('disables query when unitId is null', () => {
      const unitId = ref<string | null>(null);
      const { result } = withSetup(() => useUnitProgressQuery(unitId));
      expect(result.isEnabled.value).toBe(false);
    });

    it('enables query and fetches unit progress roster', async () => {
      const mockRecords: LearningProgress[] = [
        {
          id: 'prog-1',
          studentId: 'student-1',
          studentName: 'Alice Smith',
          learningUnitId: 'unit-1',
          learningUnitCode: 'MATH101',
          learningUnitTitle: 'Mathematics 10',
          academicPeriodId: 'period-1',
          progressStatus: 'in_progress',
          basis: 'learning_work',
          completionDate: null,
          notes: null,
          recordedAt: '2026-03-01T10:00:00Z',
          recordedBy: 'teacher-1',
        },
      ];

      vi.spyOn(learningProgressApi, 'getUnitProgress').mockResolvedValue(mockRecords);

      const unitId = ref('unit-1');
      const { result } = withSetup(() => useUnitProgressQuery(unitId));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockRecords);
      expect(learningProgressApi.getUnitProgress).toHaveBeenCalledWith('unit-1', undefined);
    });
  });

  describe('useRecordProgressMutation', () => {
    it('executes mutation, invalidates progress/records/participation queries, and shows toast', async () => {
      const mockProgress: LearningProgress = {
        id: 'prog-new',
        studentId: 'student-1',
        studentName: 'Alice Smith',
        learningUnitId: 'unit-1',
        learningUnitCode: 'MATH101',
        learningUnitTitle: 'Mathematics 10',
        academicPeriodId: 'period-1',
        progressStatus: 'completed',
        basis: 'assessment_evidence',
        completionDate: '2026-06-25',
        notes: 'PACE module completed out-of-period',
        recordedAt: '2026-06-25T11:00:00Z',
        recordedBy: 'teacher-1',
      };

      vi.spyOn(learningProgressApi, 'recordProgress').mockResolvedValue(mockProgress);

      const { result, queryClient } = withSetup(() => useRecordProgressMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        studentId: 'student-1',
        learningUnitId: 'unit-1',
        academicPeriodId: 'period-1',
        progressStatus: 'completed',
        basis: 'assessment_evidence',
        completionDate: '2026-06-25',
        notes: 'PACE module completed out-of-period',
      });

      expect(learningProgressApi.recordProgress).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: learningProgressKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'records'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: reportCardKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'participations'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Progress Recorded',
        }),
      );
    });
  });

  describe('useConcludeParticipationMutation', () => {
    it('executes conclude participation, invalidates progress/records/participation queries, and shows toast', async () => {
      const mockResponse: ConcludeParticipationResponse = {
        academicParticipationId: 'part-1',
        studentId: 'student-1',
        learningUnitId: 'unit-1',
        status: 'completed',
        concludedAt: '2026-06-25T12:00:00Z',
        passingCriteriaMet: true,
        verificationSummary: 'All criteria satisfied.',
      };

      vi.spyOn(learningProgressApi, 'concludeParticipation').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useConcludeParticipationMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        academicParticipationId: 'part-1',
        notes: 'Formally concluded after final review',
      });

      expect(learningProgressApi.concludeParticipation).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: learningProgressKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'records'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: reportCardKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'participations'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Participation Concluded',
        }),
      );
    });
  });

  describe('useLearningProgress unified composable and aliases', () => {
    it('exposes queries and mutations', () => {
      const studentId = ref('student-1');
      const periodId = ref('period-1');
      const { result } = withSetup(() => useLearningProgress(studentId, periodId));

      expect(result.studentProgressQuery).toBeDefined();
      expect(result.recordProgressMutation).toBeDefined();
      expect(result.concludeParticipationMutation).toBeDefined();
    });

    it('exports alias functions matching criteria naming', () => {
      expect(recordProgressMutation).toBe(useRecordProgressMutation);
      expect(concludeParticipationMutation).toBe(useConcludeParticipationMutation);
    });
  });
});
