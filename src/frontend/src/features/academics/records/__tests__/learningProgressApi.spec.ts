// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getStudentProgress,
  getUnitProgress,
  recordProgress,
  concludeParticipation,
  learningProgressApi,
} from '../api/learningProgressApi';
import type {
  ConcludeParticipationRequest,
  ConcludeParticipationResponse,
  LearningProgress,
  RecordProgressRequest,
} from '../types';

describe('Learning Progress API Client (P9-S26)', () => {
  const studentId = '11111111-1111-1111-1111-111111111111';
  const learningUnitId = '22222222-2222-2222-2222-222222222222';
  const academicPeriodId = '33333333-3333-3333-3333-333333333333';
  const progressId = '44444444-4444-4444-4444-444444444444';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getStudentProgress', () => {
    it('calls GET /v1/academics/students/{studentId}/learning-progress with academicPeriodId param', async () => {
      const mockProgress: LearningProgress[] = [
        {
          id: progressId,
          studentId,
          learningUnitId,
          learningUnitName: 'Mathematics 10',
          learningUnitCode: 'MATH-10',
          academicPeriodId,
          status: 'in_progress',
          basis: 'learning_work',
          completionDate: null,
          updatedBy: 'Teacher User',
          updatedAt: '2026-11-01T00:00:00Z',
        },
      ];

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockProgress as never);

      const result = await getStudentProgress(studentId, academicPeriodId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/learning-progress`,
        {
          params: { academicPeriodId },
        },
      );
      expect(result).toEqual(mockProgress);
    });

    it('calls GET /v1/academics/students/{studentId}/learning-progress without period param when omitted', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([] as never);

      const result = await getStudentProgress(studentId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/learning-progress`,
        {
          params: undefined,
        },
      );
      expect(result).toEqual([]);
    });
  });

  describe('getUnitProgress', () => {
    it('calls GET /v1/academics/learning-units/{unitId}/progress with academicPeriodId param', async () => {
      const mockProgress: LearningProgress[] = [
        {
          id: progressId,
          studentId,
          studentName: 'Budi Santoso',
          learningUnitId,
          academicPeriodId,
          status: 'completed',
          basis: 'assessment_evidence',
          completionDate: '2026-11-05',
          updatedBy: 'Teacher User',
          updatedAt: '2026-11-05T00:00:00Z',
        },
      ];

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockProgress as never);

      const result = await getUnitProgress(learningUnitId, academicPeriodId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/progress`,
        {
          params: { academicPeriodId },
        },
      );
      expect(result).toEqual(mockProgress);
    });

    it('calls GET /v1/academics/learning-units/{unitId}/progress without period param when omitted', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([] as never);

      const result = await getUnitProgress(learningUnitId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/progress`,
        {
          params: undefined,
        },
      );
      expect(result).toEqual([]);
    });
  });

  describe('recordProgress', () => {
    it('calls POST /v1/academics/learning-progress with payload', async () => {
      const request: RecordProgressRequest = {
        studentId,
        learningUnitId,
        academicPeriodId,
        status: 'completed',
        basis: 'assessment_evidence',
        completionDate: '2026-11-07',
        notes: 'Passed summative evaluation',
      };

      const mockResponse: LearningProgress = {
        id: progressId,
        ...request,
        updatedBy: 'Academic Coordinator',
        updatedAt: '2026-11-07T00:00:00Z',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await recordProgress(request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/learning-progress', request);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('concludeParticipation', () => {
    it('calls POST /v1/academics/learning-progress/conclude with conclusion payload', async () => {
      const request: ConcludeParticipationRequest = {
        studentId,
        learningUnitId,
        academicPeriodId,
        notes: 'Student satisfied all learning objectives and competencies.',
      };

      const mockResponse: ConcludeParticipationResponse = {
        studentId,
        learningUnitId,
        academicPeriodId,
        participationStatus: 'completed',
        concludedAt: '2026-11-07T00:00:00Z',
        concludedBy: 'Academic Authority',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await concludeParticipation(request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        '/v1/academics/learning-progress/conclude',
        request,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('learningProgressApi object barrel', () => {
    it('exposes all client methods via unified object', () => {
      expect(learningProgressApi.getStudentProgress).toBe(getStudentProgress);
      expect(learningProgressApi.getUnitProgress).toBe(getUnitProgress);
      expect(learningProgressApi.recordProgress).toBe(recordProgress);
      expect(learningProgressApi.concludeParticipation).toBe(concludeParticipation);
    });
  });
});
