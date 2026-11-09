// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  evaluateStanding,
  evaluateProgression,
  batchDetermineStanding,
  batchIssueProgression,
  getStandings,
  getRecommendations,
  conferAchievement,
  getPeriodAchievements,
  correctStanding,
  correctProgression,
  cohortReviewApi,
} from '../api/cohortReviewApi';
import type {
  AcademicAchievement,
  BatchDetermineStandingRequest,
  BatchDetermineStandingResponse,
  BatchIssueProgressionRequest,
  BatchIssueProgressionResponse,
  CohortProgressionProposal,
  CohortStandingProposal,
  ConferAchievementRequest,
  CorrectProgressionRequest,
  CorrectStandingRequest,
  EvaluateCohortParams,
  GetRecommendationsParams,
  GetStandingsParams,
  ProgressionItem,
  StandingItem,
} from '../types';

describe('Cohort Academic Review API Client (P9-S26)', () => {
  const periodId = '11111111-1111-1111-1111-111111111111';
  const studentId = '22222222-2222-2222-2222-222222222222';
  const learningUnitId = '33333333-3333-3333-3333-333333333333';
  const standingId = '44444444-4444-4444-4444-444444444444';
  const recommendationId = '55555555-5555-5555-5555-555555555555';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('evaluateStanding', () => {
    it('calls POST /v1/academics/cohort-review/evaluate-standing with parameters', async () => {
      const mockProposals: CohortStandingProposal[] = [
        {
          studentId,
          studentName: 'Budi Santoso',
          gradeLevel: 'Grade 10',
          academicPeriodId: periodId,
          totalUnits: 8,
          passedUnits: 8,
          failedUnits: 0,
          attendancePercentage: 94.5,
          proposedStanding: 'good_standing',
          policyReference: 'Academic Standing Policy 2026',
        },
      ];

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockProposals as never);
      const params: EvaluateCohortParams = {
        academicPeriodId: periodId,
        gradeLevel: 'Grade 10',
      };

      const result = await evaluateStanding(params);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        '/v1/academics/cohort-review/evaluate-standing',
        params,
      );
      expect(result).toEqual(mockProposals);
    });
  });

  describe('evaluateProgression', () => {
    it('calls POST /v1/academics/cohort-review/evaluate-progression with parameters', async () => {
      const mockProposals: CohortProgressionProposal[] = [
        {
          studentId,
          studentName: 'Budi Santoso',
          currentLevel: 'Grade 10',
          academicPeriodId: periodId,
          standing: 'good_standing',
          proposedRecommendation: 'promotion_eligible',
          recommendedToLevel: 'Grade 11',
          supportingEvidence: 'Satisfied credit and attendance thresholds',
        },
      ];

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockProposals as never);
      const params: EvaluateCohortParams = {
        academicPeriodId: periodId,
        gradeLevel: 'Grade 10',
      };

      const result = await evaluateProgression(params);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        '/v1/academics/cohort-review/evaluate-progression',
        params,
      );
      expect(result).toEqual(mockProposals);
    });
  });

  describe('batchDetermineStanding', () => {
    it('calls POST /v1/academics/academic-standings/batch with commit payload', async () => {
      const request: BatchDetermineStandingRequest = {
        academicPeriodId: periodId,
        items: [
          {
            studentId,
            academicPeriodId: periodId,
            standing: 'good_standing',
            policyReference: 'Policy 2026 Art. 5',
            isException: false,
          },
        ],
      };

      const mockResponse: BatchDetermineStandingResponse = {
        determinedCount: 1,
        academicPeriodId: periodId,
        determinedAt: '2026-11-07T00:00:00Z',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await batchDetermineStanding(request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        '/v1/academics/academic-standings/batch',
        request,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('batchIssueProgression', () => {
    it('calls POST /v1/academics/promotion-recommendations/batch with commit payload', async () => {
      const request: BatchIssueProgressionRequest = {
        academicPeriodId: periodId,
        items: [
          {
            studentId,
            academicPeriodId: periodId,
            recommendation: 'promotion_eligible',
            fromLevel: 'Grade 10',
            recommendedToLevel: 'Grade 11',
            supportingEvidence: 'Completed all core requirements',
            isException: false,
          },
        ],
      };

      const mockResponse: BatchIssueProgressionResponse = {
        issuedCount: 1,
        academicPeriodId: periodId,
        issuedAt: '2026-11-07T00:00:00Z',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await batchIssueProgression(request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        '/v1/academics/promotion-recommendations/batch',
        request,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getStandings', () => {
    it('calls GET /v1/academics/students/{studentId}/standings when studentId is supplied', async () => {
      const mockStandings: StandingItem[] = [
        {
          id: standingId,
          studentId,
          academicPeriodId: periodId,
          standing: 'good_standing',
          policyReference: 'Policy 2026',
          isException: false,
          authorizedBy: 'Authority User',
          determinedAt: '2026-11-07T00:00:00Z',
        },
      ];

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockStandings as never);
      const params: GetStandingsParams = {
        academicPeriodId: periodId,
        studentId,
      };

      const result = await getStandings(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/standings`,
        {
          params: { academicPeriodId: periodId },
        },
      );
      expect(result).toEqual(mockStandings);
    });

    it('calls GET /v1/academics/cohort-review/standings when studentId is omitted', async () => {
      const mockStandings: StandingItem[] = [];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockStandings as never);
      const params: GetStandingsParams = {
        academicPeriodId: periodId,
        gradeLevel: 'Grade 10',
      };

      const result = await getStandings(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/cohort-review/standings', {
        params,
      });
      expect(result).toEqual(mockStandings);
    });
  });

  describe('getRecommendations', () => {
    it('calls GET /v1/academics/students/{studentId}/promotion-recommendations when studentId is supplied', async () => {
      const mockRecs: ProgressionItem[] = [
        {
          id: recommendationId,
          studentId,
          academicPeriodId: periodId,
          recommendation: 'promotion_eligible',
          fromLevel: 'Grade 10',
          recommendedToLevel: 'Grade 11',
          supportingEvidence: 'All requirements met',
          isException: false,
          authorizedBy: 'Authority User',
          issuedAt: '2026-11-07T00:00:00Z',
        },
      ];

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRecs as never);
      const params: GetRecommendationsParams = {
        academicPeriodId: periodId,
        studentId,
      };

      const result = await getRecommendations(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/promotion-recommendations`,
        {
          params: { academicPeriodId: periodId },
        },
      );
      expect(result).toEqual(mockRecs);
    });

    it('calls GET /v1/academics/cohort-review/promotion-recommendations when studentId is omitted', async () => {
      const mockRecs: ProgressionItem[] = [];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRecs as never);
      const params: GetRecommendationsParams = {
        academicPeriodId: periodId,
        gradeLevel: 'Grade 10',
      };

      const result = await getRecommendations(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        '/v1/academics/cohort-review/promotion-recommendations',
        { params },
      );
      expect(result).toEqual(mockRecs);
    });
  });

  describe('conferAchievement', () => {
    it('calls POST /v1/academics/achievements with conferral payload', async () => {
      const request: ConferAchievementRequest = {
        studentId,
        academicPeriodId: periodId,
        learningUnitId,
        achievementType: 'subject_distinction',
        title: 'Excellence in Mathematics',
        criteriaMet: 'Grade average > 95%',
        recognizedAt: '2026-11-07',
      };

      const mockResponse: AcademicAchievement = {
        id: 'achieve-1',
        ...request,
        recognizedBy: 'Principal Jane',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await conferAchievement(request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/achievements', request);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getPeriodAchievements', () => {
    it('calls GET /v1/academics/cohort-review/achievements with period parameter', async () => {
      const mockAchievements: AcademicAchievement[] = [];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockAchievements as never);

      const result = await getPeriodAchievements(periodId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/cohort-review/achievements', {
        params: { academicPeriodId: periodId },
      });
      expect(result).toEqual(mockAchievements);
    });
  });

  describe('correctStanding', () => {
    it('calls POST /v1/academics/academic-standings/{id}/correct with reason and corrected fields', async () => {
      const request: CorrectStandingRequest = {
        standing: 'conditional_standing',
        conditions: 'Updated per re-evaluation of grade evidence',
        reason: 'Authorized correction following administrative moderation review.',
      };

      const mockResponse: StandingItem = {
        id: standingId,
        studentId,
        academicPeriodId: periodId,
        standing: 'conditional_standing',
        conditions: request.conditions,
        policyReference: 'Policy 2026',
        isException: false,
        authorizedBy: 'Academic Authority',
        determinedAt: '2026-11-07T00:00:00Z',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await correctStanding(standingId, request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/academic-standings/${encodeURIComponent(standingId)}/correct`,
        request,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('correctProgression', () => {
    it('calls POST /v1/academics/promotion-recommendations/{id}/correct with reason and corrected fields', async () => {
      const request: CorrectProgressionRequest = {
        recommendation: 'conditional_promotion',
        conditions: 'Pass supplementary assignment by end of break',
        reason: 'Authorized correction per Academic Council appeal outcome.',
      };

      const mockResponse: ProgressionItem = {
        id: recommendationId,
        studentId,
        academicPeriodId: periodId,
        recommendation: 'conditional_promotion',
        fromLevel: 'Grade 10',
        recommendedToLevel: 'Grade 11',
        conditions: request.conditions,
        supportingEvidence: 'Board decision on appeal',
        isException: false,
        authorizedBy: 'Academic Authority',
        issuedAt: '2026-11-07T00:00:00Z',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await correctProgression(recommendationId, request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/promotion-recommendations/${encodeURIComponent(recommendationId)}/correct`,
        request,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('cohortReviewApi object barrel', () => {
    it('exposes all client methods via unified object', () => {
      expect(cohortReviewApi.evaluateStanding).toBe(evaluateStanding);
      expect(cohortReviewApi.evaluateProgression).toBe(evaluateProgression);
      expect(cohortReviewApi.batchDetermineStanding).toBe(batchDetermineStanding);
      expect(cohortReviewApi.batchIssueProgression).toBe(batchIssueProgression);
      expect(cohortReviewApi.getStandings).toBe(getStandings);
      expect(cohortReviewApi.getRecommendations).toBe(getRecommendations);
      expect(cohortReviewApi.conferAchievement).toBe(conferAchievement);
      expect(cohortReviewApi.getPeriodAchievements).toBe(getPeriodAchievements);
      expect(cohortReviewApi.correctStanding).toBe(correctStanding);
      expect(cohortReviewApi.correctProgression).toBe(correctProgression);
    });
  });
});
