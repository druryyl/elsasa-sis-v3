// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { cohortReviewApi } from '../../api/cohortReviewApi';
import {
  cohortReviewKeys,
  useBatchCommitCohortMutation,
  useCohortAchievementsQuery,
  useCohortRecommendationsQuery,
  useCohortReview,
  useCohortStandingsQuery,
  useConferAchievementMutation,
  useCorrectProgressionMutation,
  useCorrectStandingMutation,
  useEvaluateProgressionQuery,
  useEvaluateStandingQuery,
} from '../useCohortReview';
import type {
  AcademicAchievement,
  BatchDetermineStandingResponse,
  BatchIssueProgressionResponse,
  CohortProgressionProposal,
  CohortStandingProposal,
  ProgressionItem,
  StandingItem,
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

describe('useCohortReview Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('cohortReviewKeys', () => {
    it('builds standard query keys matching completion criteria', () => {
      expect(cohortReviewKeys.all).toEqual(['academics', 'cohort-review']);
      expect(
        cohortReviewKeys.evaluateStanding('period-2026-s1', 'Grade 10'),
      ).toEqual([
        'academics',
        'cohort-review',
        'evaluate-standing',
        'period-2026-s1',
        'Grade 10',
      ]);
      expect(
        cohortReviewKeys.evaluateProgression('period-2026-s1', 'Grade 10'),
      ).toEqual([
        'academics',
        'cohort-review',
        'evaluate-progression',
        'period-2026-s1',
        'Grade 10',
      ]);
      expect(cohortReviewKeys.standings()).toEqual([
        'academics',
        'cohort-review',
        'standings',
      ]);
      expect(cohortReviewKeys.achievements()).toEqual([
        'academics',
        'cohort-review',
        'achievements',
      ]);
    });
  });

  describe('useEvaluateStandingQuery', () => {
    it('disables query when periodId or gradeLevel is missing', () => {
      const periodId = ref<string | null>(null);
      const gradeLevel = ref<string | null>(null);
      const { result } = withSetup(() => useEvaluateStandingQuery(periodId, gradeLevel));
      expect(result.isEnabled.value).toBe(false);
    });

    it('enables query and evaluates cohort standing proposals', async () => {
      const mockProposals: CohortStandingProposal[] = [
        {
          studentId: 'student-1',
          studentName: 'Alice Smith',
          gradeLevel: 'Grade 10',
          passedUnits: 8,
          failedUnits: 0,
          attendanceRate: 95.0,
          proposedStanding: 'good_standing',
          policyReference: 'SK-AKAD/2026/POLICY-01',
          suggestedConditions: null,
          existingStanding: null,
        },
      ];

      vi.spyOn(cohortReviewApi, 'evaluateStanding').mockResolvedValue(mockProposals);

      const periodId = ref('period-1');
      const gradeLevel = ref('Grade 10');
      const { result } = withSetup(() => useEvaluateStandingQuery(periodId, gradeLevel));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockProposals);
      expect(cohortReviewApi.evaluateStanding).toHaveBeenCalledWith({
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
      });
    });
  });

  describe('useEvaluateProgressionQuery', () => {
    it('enables query and evaluates cohort progression recommendations', async () => {
      const mockProposals: CohortProgressionProposal[] = [
        {
          studentId: 'student-1',
          studentName: 'Alice Smith',
          currentGradeLevel: 'Grade 10',
          proposedProgression: 'promotion_eligible',
          recommendedGradeLevel: 'Grade 11',
          conditions: null,
          supportingEvidenceSummary: '8/8 units completed. Attendance 95%.',
          standingClassification: 'good_standing',
        },
      ];

      vi.spyOn(cohortReviewApi, 'evaluateProgression').mockResolvedValue(mockProposals);

      const periodId = ref('period-1');
      const gradeLevel = ref('Grade 10');
      const { result } = withSetup(() => useEvaluateProgressionQuery(periodId, gradeLevel));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockProposals);
      expect(cohortReviewApi.evaluateProgression).toHaveBeenCalledWith({
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
      });
    });
  });

  describe('useCohortStandingsQuery', () => {
    it('fetches committed cohort standings', async () => {
      const mockStandings: StandingItem[] = [
        {
          id: 'standing-1',
          studentId: 'student-1',
          studentName: 'Alice Smith',
          academicPeriodId: 'period-1',
          gradeLevel: 'Grade 10',
          standing: 'good_standing',
          policyReference: 'SK-AKAD/2026/POLICY-01',
          conditions: null,
          isException: false,
          exceptionReason: null,
          committeeReference: null,
          determinedAt: '2026-06-15T10:00:00Z',
          determinedBy: 'user-coord',
        },
      ];

      vi.spyOn(cohortReviewApi, 'getStandings').mockResolvedValue(mockStandings);

      const { result } = withSetup(() =>
        useCohortStandingsQuery({ academicPeriodId: 'period-1', gradeLevel: 'Grade 10' }),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockStandings);
      expect(cohortReviewApi.getStandings).toHaveBeenCalledWith({
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
      });
    });
  });

  describe('useCohortAchievementsQuery', () => {
    it('fetches achievements for academic period', async () => {
      const mockAchievements: AcademicAchievement[] = [
        {
          id: 'ach-1',
          studentId: 'student-1',
          studentName: 'Alice Smith',
          academicPeriodId: 'period-1',
          achievementType: 'academic_honor',
          title: 'Juara Umum Grade 10',
          conferredDate: '2026-06-20',
          conferredBy: 'headmaster-1',
          linkedAcademicResultId: null,
          notes: 'Top cumulative GPA',
        },
      ];

      vi.spyOn(cohortReviewApi, 'getPeriodAchievements').mockResolvedValue(mockAchievements);

      const periodId = ref('period-1');
      const { result } = withSetup(() => useCohortAchievementsQuery(periodId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockAchievements);
      expect(cohortReviewApi.getPeriodAchievements).toHaveBeenCalledWith('period-1');
    });
  });

  describe('useBatchCommitCohortMutation', () => {
    it('executes batch standing and progression commitments and invalidates caches', async () => {
      const mockStandingRes: BatchDetermineStandingResponse = {
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
        determinationsCount: 25,
        committedAt: '2026-06-20T12:00:00Z',
      };
      const mockProgressionRes: BatchIssueProgressionResponse = {
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
        recommendationsCount: 25,
        committedAt: '2026-06-20T12:00:00Z',
      };

      vi.spyOn(cohortReviewApi, 'batchDetermineStanding').mockResolvedValue(mockStandingRes);
      vi.spyOn(cohortReviewApi, 'batchIssueProgression').mockResolvedValue(mockProgressionRes);

      const { result, queryClient } = withSetup(() => useBatchCommitCohortMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const outcome = await result.mutateAsync({
        standingRequest: {
          academicPeriodId: 'period-1',
          gradeLevel: 'Grade 10',
          standings: [
            {
              studentId: 'student-1',
              standing: 'good_standing',
              policyReference: 'SK-AKAD/2026/POLICY-01',
              conditions: null,
              isException: false,
              exceptionReason: null,
              committeeReference: null,
            },
          ],
        },
        progressionRequest: {
          academicPeriodId: 'period-1',
          gradeLevel: 'Grade 10',
          recommendations: [
            {
              studentId: 'student-1',
              currentGradeLevel: 'Grade 10',
              recommendedGradeLevel: 'Grade 11',
              recommendation: 'promotion_eligible',
              conditions: null,
              supportingEvidenceSummary: 'All criteria passed',
              isException: false,
              exceptionReason: null,
              committeeReference: null,
            },
          ],
        },
      });

      expect(cohortReviewApi.batchDetermineStanding).toHaveBeenCalled();
      expect(cohortReviewApi.batchIssueProgression).toHaveBeenCalled();
      expect(outcome.standingResponse).toEqual(mockStandingRes);
      expect(outcome.progressionResponse).toEqual(mockProgressionRes);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'standings'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'records'] });

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Cohort Committed',
        }),
      );
    });
  });

  describe('useConferAchievementMutation', () => {
    it('confers achievement and invalidates achievement caches', async () => {
      const mockAchievement: AcademicAchievement = {
        id: 'ach-new',
        studentId: 'student-1',
        studentName: 'Alice Smith',
        academicPeriodId: 'period-1',
        achievementType: 'subject_distinction',
        title: 'Highest Score in Advanced Physics',
        conferredDate: '2026-06-20',
        conferredBy: 'headmaster-1',
        linkedAcademicResultId: null,
        notes: null,
      };

      vi.spyOn(cohortReviewApi, 'conferAchievement').mockResolvedValue(mockAchievement);

      const { result, queryClient } = withSetup(() => useConferAchievementMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        studentId: 'student-1',
        academicPeriodId: 'period-1',
        achievementType: 'subject_distinction',
        title: 'Highest Score in Advanced Physics',
        conferredDate: '2026-06-20',
      });

      expect(cohortReviewApi.conferAchievement).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.achievements() });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Achievement Conferred',
        }),
      );
    });
  });

  describe('useCorrectStandingMutation', () => {
    it('corrects standing and invalidates standing caches', async () => {
      const mockUpdatedStanding: StandingItem = {
        id: 'standing-1',
        studentId: 'student-1',
        studentName: 'Alice Smith',
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
        standing: 'conditional_standing',
        policyReference: 'SK-AKAD/2026/POLICY-01',
        conditions: 'Remediation completed',
        isException: true,
        exceptionReason: 'Approved committee decree',
        committeeReference: 'SK-DG/2026/06-01',
        determinedAt: '2026-06-15T10:00:00Z',
        determinedBy: 'user-coord',
      };

      vi.spyOn(cohortReviewApi, 'correctStanding').mockResolvedValue(mockUpdatedStanding);

      const { result, queryClient } = withSetup(() => useCorrectStandingMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'standing-1',
        request: {
          standing: 'conditional_standing',
          policyReference: 'SK-AKAD/2026/POLICY-01',
          conditions: 'Remediation completed',
          isException: true,
          exceptionReason: 'Approved committee decree',
          committeeReference: 'SK-DG/2026/06-01',
          reason: 'Correction of standing post committee meeting',
        },
      });

      expect(cohortReviewApi.correctStanding).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.standings() });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'records'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Standing Corrected',
        }),
      );
    });
  });

  describe('useCorrectProgressionMutation', () => {
    it('corrects progression and invalidates recommendation caches', async () => {
      const mockUpdatedProg: ProgressionItem = {
        id: 'prog-1',
        studentId: 'student-1',
        studentName: 'Alice Smith',
        academicPeriodId: 'period-1',
        fromGradeLevel: 'Grade 10',
        toGradeLevel: 'Grade 11',
        recommendedProgression: 'conditional_promotion',
        conditions: 'Must clear PACE module in vacation',
        isException: true,
        exceptionReason: 'Approved by principal',
        committeeReference: 'SK-DG/2026/06-01',
        issuedAt: '2026-06-15T10:00:00Z',
        issuedBy: 'user-coord',
      };

      vi.spyOn(cohortReviewApi, 'correctProgression').mockResolvedValue(mockUpdatedProg);

      const { result, queryClient } = withSetup(() => useCorrectProgressionMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'prog-1',
        request: {
          recommendedProgression: 'conditional_promotion',
          toGradeLevel: 'Grade 11',
          conditions: 'Must clear PACE module in vacation',
          isException: true,
          exceptionReason: 'Approved by principal',
          committeeReference: 'SK-DG/2026/06-01',
          reason: 'Correcting to conditional promotion per decree',
        },
      });

      expect(cohortReviewApi.correctProgression).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.recommendations() });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: cohortReviewKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Progression Corrected',
        }),
      );
    });
  });

  describe('useCohortReview unified composable', () => {
    it('exposes queries and mutations', () => {
      const periodId = ref('period-1');
      const gradeLevel = ref('Grade 10');
      const { result } = withSetup(() => useCohortReview(periodId, gradeLevel));

      expect(result.evaluateStandingQuery).toBeDefined();
      expect(result.evaluateProgressionQuery).toBeDefined();
      expect(result.standingsQuery).toBeDefined();
      expect(result.achievementsQuery).toBeDefined();
      expect(result.batchCommitCohortMutation).toBeDefined();
      expect(result.conferAchievementMutation).toBeDefined();
      expect(result.correctStandingMutation).toBeDefined();
      expect(result.correctProgressionMutation).toBeDefined();
    });
  });
});
