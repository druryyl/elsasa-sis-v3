// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { reportCardsApi } from '../../api/reportCardsApi';
import { transcriptsApi } from '../../api/transcriptsApi';
import { correctionsApi } from '../../api/correctionsApi';
import { reportCardKeys, transcriptKeys, correctionKeys, resultKeys } from '../../constants/queryKeys';
import {
  useReportCards,
  useReportCardsQuery,
  useReportCardDetailQuery,
  useCompileReportCardMutation,
  useBatchCompileReportCardsMutation,
  useApproveReportCardMutation,
  usePublishReportCardMutation,
} from '../useReportCards';
import {
  useTranscripts,
  useStudentTranscriptQuery,
  useTranscriptDetailQuery,
  useGenerateTranscriptMutation,
  useIssueTranscriptMutation,
} from '../useTranscripts';
import {
  useRecordCorrections,
  useCorrectionHistoryQuery,
  useCorrectResultMutation,
} from '../useRecordCorrections';

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

describe('Records Composables Suite (P8-S20)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useReportCards & Mutations', () => {
    const mockReportCard = {
      id: 'rc-1',
      studentId: 'stu-1',
      studentName: 'Budi Santoso',
      academicPeriodId: 'per-1',
      academicPeriodLabel: '2026/2027 Ganjil',
      reportType: 'mid_term' as const,
      templateName: 'kemenag_standard',
      status: 'draft' as const,
      compiledAt: '2026-11-05T08:00:00Z',
      approvedAt: null,
      approvedBy: null,
      publishedAt: null,
      narrativeComments: null,
      contentSnapshot: null,
      entries: [],
    };

    it('queries report cards list and provides computed reportCards', async () => {
      vi.spyOn(reportCardsApi, 'getStudentReportCards').mockResolvedValue([mockReportCard]);

      const periodId = ref('per-1');
      const { result } = withSetup(() => useReportCards(periodId));

      await vi.waitFor(() => expect(result.reportCardsQuery.isSuccess.value).toBe(true));
      expect(result.reportCards.value).toEqual([mockReportCard]);
      expect(reportCardsApi.getStudentReportCards).toHaveBeenCalledWith({
        academicPeriodId: 'per-1',
      });
      expect(result.compile).toBeDefined();
      expect(result.batchCompile).toBeDefined();
      expect(result.approve).toBeDefined();
      expect(result.publish).toBeDefined();

      const { result: standaloneRcQuery } = withSetup(() => useReportCardsQuery(periodId));
      await vi.waitFor(() => expect(standaloneRcQuery.isSuccess.value).toBe(true));
      expect(standaloneRcQuery.data.value).toEqual([mockReportCard]);
    });

    it('queries report card details by ID', async () => {
      vi.spyOn(reportCardsApi, 'getById').mockResolvedValue(mockReportCard);

      const { result } = withSetup(() => useReportCardDetailQuery('rc-1'));
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockReportCard);
      expect(reportCardsApi.getById).toHaveBeenCalledWith('rc-1');
    });

    it('compiles individual report card and invalidates caches', async () => {
      vi.spyOn(reportCardsApi, 'compile').mockResolvedValue(mockReportCard);

      const { result, queryClient } = withSetup(() => useCompileReportCardMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        studentId: 'stu-1',
        academicPeriodId: 'per-1',
        academicPeriodLabel: '2026/2027 Ganjil',
        reportType: 'mid_term',
        templateName: 'kemenag_standard',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: reportCardKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Report Card Compiled',
        }),
      );
    });

    it('executes batch compilation and invalidates caches', async () => {
      const mockBatchSummary = {
        compiledCount: 32,
        skippedExistingCount: 3,
      };
      vi.spyOn(reportCardsApi, 'batchCompile').mockResolvedValue(mockBatchSummary);

      const { result, queryClient } = withSetup(() => useBatchCompileReportCardsMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        academicPeriodId: 'per-1',
        academicPeriodLabel: '2026/2027 Ganjil',
        reportType: 'final',
        templateName: 'merdeka_rapor',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: reportCardKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Batch Compilation Completed',
        }),
      );
    });

    it('approves and publishes report card, verifying cache invalidation', async () => {
      const approvedRc = { ...mockReportCard, status: 'approved' as const };
      const publishedRc = { ...mockReportCard, status: 'published' as const };
      vi.spyOn(reportCardsApi, 'approve').mockResolvedValue(approvedRc);
      vi.spyOn(reportCardsApi, 'publish').mockResolvedValue(publishedRc);

      const { result: approveRes, queryClient } = withSetup(() => useApproveReportCardMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      approveRes.mutate('rc-1');
      await vi.waitFor(() => expect(approveRes.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: reportCardKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: reportCardKeys.detail('rc-1') });

      const { result: publishRes } = withSetup(() => usePublishReportCardMutation());
      publishRes.mutate('rc-1');
      await vi.waitFor(() => expect(publishRes.isSuccess.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Report Card Published',
        }),
      );
    });
  });

  describe('useTranscripts & Mutations', () => {
    const mockTranscript = {
      id: 'tr-1',
      studentId: 'stu-1',
      studentName: 'Siti Rahma',
      studentIdentifier: 'STU-1002',
      transcriptType: 'interim' as const,
      status: 'draft' as const,
      periodStart: '2025-07-01',
      periodEnd: '2026-06-30',
      cumulativeGpa: 3.85,
      totalCreditsAttempted: 40,
      totalCreditsEarned: 40,
      issuedDate: null,
      approvedBy: null,
      contentSnapshot: null,
      entries: [],
      createdAt: '2026-11-05T00:00:00Z',
    };

    it('queries student transcript and executes generate and issue', async () => {
      vi.spyOn(transcriptsApi, 'getByStudent').mockResolvedValue(mockTranscript);
      vi.spyOn(transcriptsApi, 'generate').mockResolvedValue(mockTranscript);
      vi.spyOn(transcriptsApi, 'issue').mockResolvedValue({
        id: 'tr-1',
        studentId: 'stu-1',
        transcriptType: 'official' as const,
        status: 'official' as const,
        issuedDate: '2026-11-05',
        approvedBy: 'usr-auth-1',
        snapshot: {
          transcriptId: 'tr-1',
          studentId: 'stu-1',
          studentName: 'Siti Rahma',
          studentIdentifier: 'STU-1002',
          transcriptType: 'official',
          cumulativeGpa: 3.85,
          totalCreditsEarned: 40,
          issuedDate: '2026-11-05',
          periods: [],
        },
      });

      const studentId = ref('stu-1');
      const { result, queryClient } = withSetup(() => useTranscripts(studentId));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await vi.waitFor(() => expect(result.transcriptQuery.isSuccess.value).toBe(true));
      expect(result.transcript.value).toEqual(mockTranscript);

      const { result: standaloneTranscriptQuery } = withSetup(() => useStudentTranscriptQuery(studentId));
      await vi.waitFor(() => expect(standaloneTranscriptQuery.isSuccess.value).toBe(true));
      expect(standaloneTranscriptQuery.data.value).toEqual(mockTranscript);

      vi.spyOn(transcriptsApi, 'getById').mockResolvedValue(mockTranscript);
      const { result: standaloneTranscriptDetail } = withSetup(() => useTranscriptDetailQuery('tr-1'));
      await vi.waitFor(() => expect(standaloneTranscriptDetail.isSuccess.value).toBe(true));
      expect(standaloneTranscriptDetail.data.value).toEqual(mockTranscript);

      const { result: standaloneGenMut } = withSetup(() => useGenerateTranscriptMutation());
      const { result: standaloneIssueMut } = withSetup(() => useIssueTranscriptMutation());
      expect(standaloneGenMut.mutate).toBeDefined();
      expect(standaloneIssueMut.mutate).toBeDefined();

      result.generate.mutate({
        studentId: 'stu-1',
        transcriptType: 'interim',
        periodStart: '2025-07-01',
        periodEnd: '2026-06-30',
      });
      await vi.waitFor(() => expect(result.generate.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: transcriptKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: transcriptKeys.byStudent('stu-1') });

      result.issue.mutate('tr-1');
      await vi.waitFor(() => expect(result.issue.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: transcriptKeys.detail('tr-1') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Transcript Issued',
        }),
      );
    });
  });

  describe('useRecordCorrections & Mutations', () => {
    it('queries correction history and mutates correctResult with full cache invalidation', async () => {
      const mockHistory = [
        {
          id: 'cor-1',
          academicResultId: 'res-1',
          correctedBy: 'usr-auth-1',
          correctedAt: '2026-11-05T11:00:00Z',
          previousScoreNumeric: 65,
          newScoreNumeric: 80,
          previousScoreMax: 100,
          newScoreMax: 100,
          previousGradeLetter: 'C',
          newGradeLetter: 'A-',
          previousGradeLabel: 'Satisfactory',
          newGradeLabel: 'Very Good',
          previousNarrativeFeedback: 'Needs improvement',
          newNarrativeFeedback: 'Recalculated with laboratory component',
          reason: 'Typographical error in lab rubric score calculation',
          evidenceReference: 'doc://corrections/res-1.pdf',
          isMaterial: true,
        },
      ];
      const mockResult = {
        id: 'res-1',
        assessmentId: 'asm-1',
        studentId: 'stu-1',
        applicantId: null,
        attemptNumber: 1,
        scoreNumeric: 80,
        scoreMax: 100,
        gradeLetter: 'A-',
        gradeLabel: 'Very Good',
        narrativeFeedback: 'Recalculated with laboratory component',
        evaluationModel: 'numeric' as const,
        status: 'approved' as const,
        isFinal: true,
        evaluatedBy: 'usr-teacher-1',
        evaluatedAt: '2026-11-05T09:00:00Z',
        approvedBy: 'usr-auth-1',
        approvedAt: '2026-11-05T10:00:00Z',
        publishedAt: null,
      };

      vi.spyOn(correctionsApi, 'getCorrectionHistory').mockResolvedValue(mockHistory);
      vi.spyOn(correctionsApi, 'correctResult').mockResolvedValue(mockResult);

      const resultId = ref('res-1');
      const { result, queryClient } = withSetup(() => useRecordCorrections(resultId));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await vi.waitFor(() => expect(result.correctionsQuery.isSuccess.value).toBe(true));
      expect(result.corrections.value).toEqual(mockHistory);
      expect(correctionsApi.getCorrectionHistory).toHaveBeenCalledWith('res-1');

      const { result: standaloneHistQuery } = withSetup(() => useCorrectionHistoryQuery(resultId));
      await vi.waitFor(() => expect(standaloneHistQuery.isSuccess.value).toBe(true));
      expect(standaloneHistQuery.data.value).toEqual(mockHistory);

      result.correctResult.mutate({
        id: 'res-1',
        request: {
          score_numeric: 80,
          reason: 'Typographical error in lab rubric score calculation',
          is_material: true,
        },
      });

      await vi.waitFor(() => expect(result.correctResult.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: resultKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: correctionKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: correctionKeys.history('res-1') });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: correctionKeys.detail('res-1') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Record Corrected',
        }),
      );
    });

    it('surfaces error toast on correction mutation failure', async () => {
      vi.spyOn(correctionsApi, 'correctResult').mockRejectedValue(new Error('Unauthorized'));

      const { result } = withSetup(() => useCorrectResultMutation());
      result.mutate({
        id: 'res-1',
        request: {
          reason: 'Short', // Will fail
        },
      });

      await vi.waitFor(() => expect(result.isError.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Record Correction Failed',
        }),
      );
    });
  });
});
