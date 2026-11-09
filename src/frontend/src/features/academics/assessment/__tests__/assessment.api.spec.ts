// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import { assessmentsApi } from '../api/assessmentsApi';
import { evidenceApi } from '../api/evidenceApi';
import { resultsApi } from '../api/resultsApi';
import type {
  CreateAssessmentRequest,
  EvaluateLearningRequest,
  RecordEvidenceRequest,
  UpdateAssessmentRequest,
} from '../types';

describe('Assessment & Results API Clients (P8-S19)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const assessmentId = '11111111-1111-1111-1111-111111111111';
  const unitId = '22222222-2222-2222-2222-222222222222';
  const studentId = '33333333-3333-3333-3333-333333333333';
  const periodId = '44444444-4444-4444-4444-444444444444';

  describe('assessmentsApi', () => {
    it('listByUnit calls GET /v1/academics/learning-units/{unitId}/assessments with filters', async () => {
      const mockAssessments = [{ id: assessmentId, title: 'Exam 1' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockAssessments as never);

      const result = await assessmentsApi.listByUnit(unitId, { status: 'planned', assessmentType: 'summative' });

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(unitId)}/assessments`,
        { params: { status: 'planned', assessmentType: 'summative' } },
      );
      expect(result).toEqual(mockAssessments);
    });

    it('getById calls GET /v1/academics/assessments/{id}', async () => {
      const mockAssessment = { id: assessmentId, title: 'Exam 1' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockAssessment as never);

      const result = await assessmentsApi.getById(assessmentId);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}`,
      );
      expect(result).toEqual(mockAssessment);
    });

    it('create calls POST /v1/academics/assessments with request', async () => {
      const req: CreateAssessmentRequest = {
        learningUnitId: unitId,
        title: 'Diagnostic Test',
        assessmentType: 'diagnostic',
        assessmentMethod: 'written_exam',
        maxScore: 100,
        passingScore: 60,
      };
      const mockRes = { id: assessmentId, title: 'Diagnostic Test', status: 'planned' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await assessmentsApi.create(req);

      expect(postSpy).toHaveBeenCalledWith('/v1/academics/assessments', req);
      expect(result).toEqual(mockRes);
    });

    it('update calls PUT /v1/academics/assessments/{id} with request', async () => {
      const req: UpdateAssessmentRequest = {
        title: 'Updated Test',
        maxScore: 100,
        passingScore: 70,
      };
      const mockRes = { id: assessmentId, title: 'Updated Test', status: 'planned' };
      const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(mockRes as never);

      const result = await assessmentsApi.update(assessmentId, req);

      expect(putSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}`,
        req,
      );
      expect(result).toEqual(mockRes);
    });

    it('conduct calls POST /v1/academics/assessments/{id}/conduct', async () => {
      const mockRes = { id: assessmentId, status: 'conducted' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await assessmentsApi.conduct(assessmentId, { notes: 'All present' });

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/conduct`,
        { notes: 'All present' },
      );
      expect(result).toEqual(mockRes);
    });

    it('cancel calls POST /v1/academics/assessments/{id}/cancel with reason', async () => {
      const mockRes = { id: assessmentId, status: 'cancelled' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await assessmentsApi.cancel(assessmentId, 'Inclement weather');

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/cancel`,
        { reason: 'Inclement weather' },
      );
      expect(result).toEqual(mockRes);
    });
  });

  describe('evidenceApi', () => {
    it('listByAssessment calls GET /v1/academics/assessments/{id}/evidence', async () => {
      const mockEvidence = [{ id: 'ev-1', evidenceType: 'submission' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockEvidence as never);

      const result = await evidenceApi.listByAssessment(assessmentId, { studentId });

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/evidence`,
        { params: { studentId } },
      );
      expect(result).toEqual(mockEvidence);
    });

    it('record calls POST /v1/academics/assessments/{id}/evidence', async () => {
      const req: RecordEvidenceRequest = {
        studentId,
        evidenceType: 'submission',
        collectedDate: '2026-10-15',
        evidenceReference: 'link-to-doc',
      };
      const mockRes = { id: 'ev-1', ...req };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await evidenceApi.record(assessmentId, req);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/evidence`,
        req,
      );
      expect(result).toEqual(mockRes);
    });
  });

  describe('resultsApi', () => {
    it('listByAssessment calls GET /v1/academics/assessments/{id}/results with params', async () => {
      const mockResults = [{ id: 'res-1', scoreNumeric: 85, isFinal: true }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResults as never);

      const result = await resultsApi.listByAssessment(assessmentId, { status: 'approved', onlyFinal: true });

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results`,
        { params: { status: 'approved', onlyFinal: true } },
      );
      expect(result).toEqual(mockResults);
    });

    it('evaluate calls POST /v1/academics/assessments/{id}/results', async () => {
      const req: EvaluateLearningRequest = {
        studentId,
        scoreNumeric: 90,
        evaluationModel: 'numeric',
        publishImmediately: true,
      };
      const mockRes = { id: 'res-1', resultStatus: 'published', scoreNumeric: 90 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await resultsApi.evaluate(assessmentId, req);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results`,
        req,
      );
      expect(result).toEqual(mockRes);
    });

    it('approve calls POST /v1/academics/assessments/{id}/results/approve', async () => {
      const mockRes = { approvedCount: 15 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await resultsApi.approve(assessmentId, { resultIds: ['res-1', 'res-2'] });

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results/approve`,
        { resultIds: ['res-1', 'res-2'] },
      );
      expect(result).toEqual(mockRes);
    });

    it('publish calls POST /v1/academics/assessments/{id}/results/publish', async () => {
      const mockRes = { publishedCount: 15 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await resultsApi.publish(assessmentId);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assessments/${encodeURIComponent(assessmentId)}/results/publish`,
        {},
      );
      expect(result).toEqual(mockRes);
    });

    it('getStudentResults calls GET /v1/portal/learning/results', async () => {
      const mockPublished = [{ resultId: 'res-1', assessmentTitle: 'Midterm', scoreNumeric: 95 }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPublished as never);

      const result = await resultsApi.getStudentResults(periodId, studentId);

      expect(getSpy).toHaveBeenCalledWith(
        '/v1/portal/learning/results',
        { params: { academicPeriodId: periodId, studentId } },
      );
      expect(result).toEqual(mockPublished);
    });
  });
});
