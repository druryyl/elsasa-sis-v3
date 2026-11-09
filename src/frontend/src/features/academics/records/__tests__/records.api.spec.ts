// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import { reportCardsApi } from '../api/reportCardsApi';
import { transcriptsApi } from '../api/transcriptsApi';
import { correctionsApi } from '../api/correctionsApi';
import type {
  BatchCompileReportCardsRequest,
  CompileReportCardRequest,
  CorrectResultRequest,
  GenerateInterimTranscriptRequest,
} from '../types';

describe('Records API Clients (P8-S19)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const reportCardId = '11111111-1111-1111-1111-111111111111';
  const transcriptId = '22222222-2222-2222-2222-222222222222';
  const resultId = '33333333-3333-3333-3333-333333333333';
  const studentId = '44444444-4444-4444-4444-444444444444';
  const periodId = '55555555-5555-5555-5555-555555555555';

  describe('reportCardsApi', () => {
    it('compile calls POST /v1/academics/report-cards/compile', async () => {
      const req: CompileReportCardRequest = {
        studentId,
        academicPeriodId: periodId,
        academicPeriodLabel: 'Term 1 2026/2027',
        reportType: 'mid_term',
        templateName: 'Buku Rapor Merdeka',
      };
      const mockRes = { id: reportCardId, status: 'draft', ...req };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await reportCardsApi.compile(req);

      expect(postSpy).toHaveBeenCalledWith('/v1/academics/report-cards/compile', req);
      expect(result).toEqual(mockRes);
    });

    it('batchCompile calls POST /v1/academics/report-cards/batch-compile', async () => {
      const req: BatchCompileReportCardsRequest = {
        academicPeriodId: periodId,
        academicPeriodLabel: 'Term 1 2026/2027',
        reportType: 'end_of_term',
        templateName: 'Buku Rapor Merdeka',
        sectionLabel: '4A',
      };
      const mockSummary = { compiledCount: 28, skippedExistingCount: 2 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockSummary as never);

      const result = await reportCardsApi.batchCompile(req);

      expect(postSpy).toHaveBeenCalledWith('/v1/academics/report-cards/batch-compile', req);
      expect(result).toEqual(mockSummary);
    });

    it('approve calls POST /v1/academics/report-cards/{id}/approve', async () => {
      const mockRes = { id: reportCardId, status: 'approved' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await reportCardsApi.approve(reportCardId, { narrativeComments: 'Excellent progress' });

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/report-cards/${encodeURIComponent(reportCardId)}/approve`,
        { narrativeComments: 'Excellent progress' },
      );
      expect(result).toEqual(mockRes);
    });

    it('publish calls POST /v1/academics/report-cards/{id}/publish', async () => {
      const mockRes = { id: reportCardId, status: 'published' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await reportCardsApi.publish(reportCardId);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/report-cards/${encodeURIComponent(reportCardId)}/publish`,
        {},
      );
      expect(result).toEqual(mockRes);
    });

    it('getById calls GET /v1/academics/report-cards/{id}', async () => {
      const mockRes = { id: reportCardId, status: 'approved' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRes as never);

      const result = await reportCardsApi.getById(reportCardId);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/report-cards/${encodeURIComponent(reportCardId)}`,
      );
      expect(result).toEqual(mockRes);
    });

    it('getStudentReportCards calls GET /v1/portal/learning/report-cards', async () => {
      const mockList = [{ id: reportCardId, status: 'published' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockList as never);

      const result = await reportCardsApi.getStudentReportCards({ studentId, academicPeriodId: periodId });

      expect(getSpy).toHaveBeenCalledWith(
        '/v1/portal/learning/report-cards',
        { params: { studentId, academicPeriodId: periodId } },
      );
      expect(result).toEqual(mockList);
    });
  });

  describe('transcriptsApi', () => {
    it('generate calls POST /v1/academics/transcripts/generate', async () => {
      const req: GenerateInterimTranscriptRequest = {
        studentId,
        transcriptType: 'official',
        periodStart: '2024-08-01',
        periodEnd: '2026-06-30',
        purpose: 'Advising',
      };
      const mockRes = { id: transcriptId, status: 'draft', ...req };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await transcriptsApi.generate(req);

      expect(postSpy).toHaveBeenCalledWith('/v1/academics/transcripts/generate', req);
      expect(result).toEqual(mockRes);
    });

    it('issue calls POST /v1/academics/transcripts/{id}/issue', async () => {
      const mockRes = {
        id: transcriptId,
        studentId,
        transcriptType: 'official',
        status: 'issued',
        issuedDate: '2026-11-05',
        approvedBy: 'admin',
        snapshot: { transcriptId, studentId, studentName: 'Test Student' },
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await transcriptsApi.issue(transcriptId);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/transcripts/${encodeURIComponent(transcriptId)}/issue`,
        {},
      );
      expect(result).toEqual(mockRes);
    });

    it('getByStudent calls GET /v1/academics/students/{studentId}/transcript', async () => {
      const mockRes = { id: transcriptId, studentId, status: 'issued' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRes as never);

      const result = await transcriptsApi.getByStudent(studentId, 'official');

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/transcript`,
        { params: { transcriptType: 'official' } },
      );
      expect(result).toEqual(mockRes);
    });

    it('getById calls GET /v1/academics/transcripts/{id}', async () => {
      const mockRes = { id: transcriptId, status: 'approved' };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockRes as never);

      const result = await transcriptsApi.getById(transcriptId);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/transcripts/${encodeURIComponent(transcriptId)}`,
      );
      expect(result).toEqual(mockRes);
    });
  });

  describe('correctionsApi', () => {
    it('correctResult calls POST /v1/academics/academic-results/{id}/correct', async () => {
      const req: CorrectResultRequest = {
        score_numeric: 85,
        reason: 'Recalculated essay rubric score according to teacher appeal',
        is_material: true,
      };
      const mockRes = { id: resultId, scoreNumeric: 85 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockRes as never);

      const result = await correctionsApi.correctResult(resultId, req);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/academic-results/${encodeURIComponent(resultId)}/correct`,
        req,
      );
      expect(result).toEqual(mockRes);
    });

    it('getCorrectionHistory calls GET /v1/academics/academic-results/{id}/corrections', async () => {
      const mockHistory = [
        {
          id: 'corr-1',
          recordType: 'academic_result',
          recordId: resultId,
          reason: 'Recalculated essay rubric score according to teacher appeal',
          isMaterial: true,
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockHistory as never);

      const result = await correctionsApi.getCorrectionHistory(resultId);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/academic-results/${encodeURIComponent(resultId)}/corrections`,
      );
      expect(result).toEqual(mockHistory);
    });
  });
});
