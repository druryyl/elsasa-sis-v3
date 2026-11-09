// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { assessmentsApi } from '../../api/assessmentsApi';
import { evidenceApi } from '../../api/evidenceApi';
import { resultsApi } from '../../api/resultsApi';
import { assessmentKeys, evidenceKeys, resultKeys } from '../../constants/queryKeys';
import {
  useAssessments,
  useAssessmentsQuery,
  useAssessmentDetailQuery,
  useCreateAssessmentMutation,
  useUpdateAssessmentMutation,
  useConductAssessmentMutation,
  useCancelAssessmentMutation,
} from '../useAssessments';
import {
  useAssessmentEvidence,
  useAssessmentEvidenceQuery,
  useRecordEvidenceMutation,
} from '../useAssessmentEvidence';
import {
  useAcademicResults,
  useAcademicResultsQuery,
  useEvaluateMutation,
  useApproveSummativeMutation,
  usePublishResultsMutation,
} from '../useAcademicResults';
import {
  useStudentResults,
  useStudentResultsQuery,
} from '../useStudentResults';

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

describe('Assessment Composables Suite (P8-S20)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useAssessments & Queries', () => {
    it('queries assessments list and provides computed assessments and mutation actions', async () => {
      const mockList = [
        {
          id: 'asm-1',
          learningUnitId: 'unit-1',
          title: 'Midterm Exam',
          assessmentType: 'summative' as const,
          assessmentMethod: 'written_exam' as const,
          reassessmentPolicy: 'highest_score' as const,
          maxScore: 100,
          passingScore: 75,
          status: 'planned' as const,
          totalAssessedCandidates: 30,
          evaluatedCandidatesCount: 25,
          createdAt: '2026-11-05T00:00:00Z',
        },
      ];
      vi.spyOn(assessmentsApi, 'listByUnit').mockResolvedValue(mockList);

      const unitId = ref('unit-1');
      const { result } = withSetup(() => useAssessments(unitId));

      expect(result.isLoading.value).toBe(true);
      await vi.waitFor(() => expect(result.assessmentsQuery.isSuccess.value).toBe(true));

      expect(result.assessments.value).toEqual(mockList);
      expect(assessmentsApi.listByUnit).toHaveBeenCalledWith('unit-1', undefined);
      expect(result.createAssessment).toBeDefined();
      expect(result.updateAssessment).toBeDefined();
      expect(result.conductAssessment).toBeDefined();
      expect(result.cancelAssessment).toBeDefined();

      const { result: standaloneQuery } = withSetup(() => useAssessmentsQuery(unitId));
      await vi.waitFor(() => expect(standaloneQuery.isSuccess.value).toBe(true));
      expect(standaloneQuery.data.value).toEqual(mockList);
    });

    it('queries assessment details by ID', async () => {
      const mockDetail = {
        id: 'asm-1',
        learningUnitId: 'unit-1',
        title: 'Midterm Exam',
        assessmentType: 'summative' as const,
        assessmentMethod: 'written_exam' as const,
        reassessmentPolicy: 'highest_score' as const,
        maxScore: 100,
        passingScore: 75,
        status: 'planned' as const,
        totalAssessedCandidates: 30,
        evaluatedCandidatesCount: 25,
        createdAt: '2026-11-05T00:00:00Z',
      };
      vi.spyOn(assessmentsApi, 'getById').mockResolvedValue(mockDetail);

      const { result } = withSetup(() => useAssessmentDetailQuery('asm-1'));
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(result.data.value).toEqual(mockDetail);
      expect(assessmentsApi.getById).toHaveBeenCalledWith('asm-1');
    });

    it('creates assessment and invalidates assessment query caches', async () => {
      const mockCreateResponse = {
        id: 'asm-new',
        title: 'Quiz 1',
        status: 'planned' as const,
      };
      vi.spyOn(assessmentsApi, 'create').mockResolvedValue(mockCreateResponse);

      const { result, queryClient } = withSetup(() => useCreateAssessmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        learningUnitId: 'unit-1',
        title: 'Quiz 1',
        assessmentType: 'formative',
        assessmentMethod: 'written_exam',
        reassessmentPolicy: 'none',
        maxScore: 100,
        passingScore: 70,
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: assessmentKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assessment Created',
        }),
      );
    });

    it('updates assessment and invalidates detail and all keys', async () => {
      const mockUpdateResponse = {
        id: 'asm-1',
        title: 'Quiz 1 Revised',
        status: 'planned' as const,
      };
      vi.spyOn(assessmentsApi, 'update').mockResolvedValue(mockUpdateResponse);

      const { result, queryClient } = withSetup(() => useUpdateAssessmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        id: 'asm-1',
        request: { title: 'Quiz 1 Revised' },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: assessmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: assessmentKeys.detail('asm-1') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assessment Updated',
        }),
      );
    });

    it('conducts assessment and cancels assessment with invalidation', async () => {
      vi.spyOn(assessmentsApi, 'conduct').mockResolvedValue({
        id: 'asm-1',
        status: 'conducted' as const,
        conductedDate: '2026-11-05',
      });
      vi.spyOn(assessmentsApi, 'cancel').mockResolvedValue({
        id: 'asm-1',
        status: 'cancelled' as const,
      });

      const { result: conductRes, queryClient } = withSetup(() => useConductAssessmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      conductRes.mutate('asm-1');
      await vi.waitFor(() => expect(conductRes.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: assessmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: assessmentKeys.detail('asm-1') });

      const { result: cancelRes } = withSetup(() => useCancelAssessmentMutation());
      cancelRes.mutate({ id: 'asm-1', reason: 'Inclement weather cancellation' });
      await vi.waitFor(() => expect(cancelRes.isSuccess.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assessment Cancelled',
        }),
      );
    });
  });

  describe('useAssessmentEvidence & Mutations', () => {
    it('queries evidence roster and records new evidence', async () => {
      const mockEvidence = [
        {
          id: 'ev-1',
          assessmentId: 'asm-1',
          studentId: 'stu-1',
          applicantId: null,
          submissionId: null,
          evidenceType: 'test_paper' as const,
          collectedDate: '2026-11-05',
          evidenceReference: 's3://evidence/paper.pdf',
          notes: 'Completed in room 204',
          createdAt: '2026-11-05T00:00:00Z',
        },
      ];
      vi.spyOn(evidenceApi, 'listByAssessment').mockResolvedValue(mockEvidence);
      vi.spyOn(evidenceApi, 'record').mockResolvedValue(mockEvidence[0]);

      const assessmentId = ref('asm-1');
      const { result, queryClient } = withSetup(() => useAssessmentEvidence(assessmentId));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await vi.waitFor(() => expect(result.evidenceQuery.isSuccess.value).toBe(true));
      expect(result.evidence.value).toEqual(mockEvidence);

      const { result: standaloneEvQuery } = withSetup(() => useAssessmentEvidenceQuery(assessmentId));
      await vi.waitFor(() => expect(standaloneEvQuery.isSuccess.value).toBe(true));
      expect(standaloneEvQuery.data.value).toEqual(mockEvidence);

      const { result: standaloneRecordMut } = withSetup(() => useRecordEvidenceMutation());
      standaloneRecordMut.mutate({
        assessmentId: 'asm-1',
        request: {
          studentId: 'stu-1',
          evidenceType: 'test_paper',
          collectedDate: '2026-11-05',
        },
      });
      await vi.waitFor(() => expect(standaloneRecordMut.isSuccess.value).toBe(true));

      result.recordEvidence.mutate({
        assessmentId: 'asm-1',
        request: {
          studentId: 'stu-1',
          evidenceType: 'test_paper',
          collectedDate: '2026-11-05',
        },
      });

      await vi.waitFor(() => expect(result.recordEvidence.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: evidenceKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: evidenceKeys.byAssessment('asm-1') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Evidence Recorded',
        }),
      );
    });
  });

  describe('useAcademicResults & Moderation Mutations', () => {
    it('queries results and mutates evaluate, approveSummative, and publishResults', async () => {
      const mockResults = [
        {
          id: 'res-1',
          assessmentId: 'asm-1',
          studentId: 'stu-1',
          applicantId: null,
          attemptNumber: 1,
          scoreNumeric: 88,
          scoreMax: 100,
          gradeLetter: 'B+',
          gradeLabel: 'Good',
          narrativeFeedback: 'Well structured arguments',
          evaluationModel: 'numeric' as const,
          status: 'provisional' as const,
          isFinal: true,
          evaluatedBy: 'usr-teacher-1',
          evaluatedAt: '2026-11-05T10:00:00Z',
          approvedBy: null,
          approvedAt: null,
          publishedAt: null,
        },
      ];
      vi.spyOn(resultsApi, 'listByAssessment').mockResolvedValue(mockResults);
      vi.spyOn(resultsApi, 'evaluate').mockResolvedValue(mockResults[0]);
      vi.spyOn(resultsApi, 'approve').mockResolvedValue({ approvedCount: 1 });
      vi.spyOn(resultsApi, 'publish').mockResolvedValue({ publishedCount: 1 });

      const assessmentId = ref('asm-1');
      const { result, queryClient } = withSetup(() => useAcademicResults(assessmentId));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await vi.waitFor(() => expect(result.resultsQuery.isSuccess.value).toBe(true));
      expect(result.results.value).toEqual(mockResults);

      const { result: standaloneResultsQuery } = withSetup(() => useAcademicResultsQuery(assessmentId));
      await vi.waitFor(() => expect(standaloneResultsQuery.isSuccess.value).toBe(true));
      expect(standaloneResultsQuery.data.value).toEqual(mockResults);

      const { result: standaloneEval } = withSetup(() => useEvaluateMutation());
      const { result: standaloneApprove } = withSetup(() => useApproveSummativeMutation());
      const { result: standalonePublish } = withSetup(() => usePublishResultsMutation());
      expect(standaloneEval.mutate).toBeDefined();
      expect(standaloneApprove.mutate).toBeDefined();
      expect(standalonePublish.mutate).toBeDefined();

      // Evaluate
      result.evaluate.mutate({
        assessmentId: 'asm-1',
        request: {
          studentId: 'stu-1',
          scoreNumeric: 88,
          evaluationModel: 'numeric',
        },
      });
      await vi.waitFor(() => expect(result.evaluate.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: resultKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: resultKeys.byAssessment('asm-1') });

      // Approve Summative
      result.approveSummative.mutate('asm-1');
      await vi.waitFor(() => expect(result.approveSummative.isSuccess.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Results Approved',
        }),
      );

      // Publish
      result.publishResults.mutate('asm-1');
      await vi.waitFor(() => expect(result.publishResults.isSuccess.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Results Published',
        }),
      );
    });

    it('handles server errors gracefully and triggers error toast', async () => {
      vi.spyOn(assessmentsApi, 'create').mockRejectedValue(new Error('Validation failed'));

      const { result } = withSetup(() => useCreateAssessmentMutation());
      result.mutate({
        learningUnitId: 'unit-1',
        title: 'Invalid',
        assessmentType: 'formative',
        assessmentMethod: 'written_exam',
        reassessmentPolicy: 'none',
        maxScore: 50,
        passingScore: 60, // Invalid: passing > max
      });

      await vi.waitFor(() => expect(result.isError.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Assessment Creation Failed',
        }),
      );
    });
  });

  describe('useStudentResults', () => {
    it('queries published grades for portal self-service', async () => {
      const mockPublished = [
        {
          resultId: 'res-1',
          assessmentId: 'asm-1',
          assessmentTitle: 'Final Exam',
          assessmentType: 'summative' as const,
          learningUnitName: 'Chemistry',
          scoreNumeric: 92,
          scoreMax: 100,
          gradeLetter: 'A',
          gradeLabel: 'Excellent',
          narrativeFeedback: 'Outstanding lab work',
          attemptNumber: 1,
          publishedAt: '2026-11-05T12:00:00Z',
        },
      ];
      vi.spyOn(resultsApi, 'getStudentResults').mockResolvedValue(mockPublished);

      const periodId = ref('period-1');
      const { result } = withSetup(() => useStudentResults(periodId));

      await vi.waitFor(() => expect(result.studentResultsQuery.isSuccess.value).toBe(true));
      expect(result.studentResults.value).toEqual(mockPublished);
      expect(resultsApi.getStudentResults).toHaveBeenCalledWith('period-1', undefined);

      const { result: standaloneStudentQuery } = withSetup(() => useStudentResultsQuery(periodId));
      await vi.waitFor(() => expect(standaloneStudentQuery.isSuccess.value).toBe(true));
      expect(standaloneStudentQuery.data.value).toEqual(mockPublished);
    });
  });
});
