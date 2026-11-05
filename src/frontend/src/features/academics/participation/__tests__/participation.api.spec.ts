// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import * as participationApi from '../api/participation.api';
import type {
  BatchRegisterCohortRequest,
  ConcludeParticipationRequest,
  RecognizePriorCreditRequest,
  RegisterParticipationRequest,
  UpdateParticipationStatusRequest,
} from '../types';

describe('Academic Participation API Client (P9-S24)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const participationId = '11111111-1111-1111-1111-111111111111';
  const unitId = '22222222-2222-2222-2222-222222222222';
  const studentId = '33333333-3333-3333-3333-333333333333';
  const periodId = '44444444-4444-4444-4444-444444444444';

  describe('batchRegisterCohortParticipation', () => {
    it('issues POST /v1/academics/participations/batch-cohort with payload', async () => {
      const mockRequest: BatchRegisterCohortRequest = {
        academicPeriodId: periodId,
        academicPeriodLabel: '2026/2027 Odd',
        learningUnitIds: [unitId],
        sectionLabel: '10-A',
        startDate: '2026-08-01',
      };
      const mockResponse = { registeredCount: 25, skippedCount: 0 };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.batchRegisterCohortParticipation(mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/participations/batch-cohort', mockRequest);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('registerParticipation', () => {
    it('issues POST /v1/academics/participations with single registration payload', async () => {
      const mockRequest: RegisterParticipationRequest = {
        studentId,
        learningUnitId: unitId,
        academicPeriodId: periodId,
        academicPeriodLabel: '2026/2027 Odd',
        sectionLabel: '10-A',
        participationType: 'standard',
        source: 'assigned',
        startDate: '2026-08-01',
      };
      const mockResponse = { id: participationId, attemptNumber: 1, status: 'in_progress' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.registerParticipation(mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/participations', mockRequest);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('updateParticipationStatus', () => {
    it('issues PUT /v1/academics/participations/{id}/status with payload', async () => {
      const mockRequest: UpdateParticipationStatusRequest = {
        newStatus: 'paused',
        notes: 'Medical leave',
      };
      const mockResponse = { id: participationId, status: 'paused', updatedAt: '2026-09-01T00:00:00Z' };
      const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.updateParticipationStatus(participationId, mockRequest);

      expect(putSpy).toHaveBeenCalledTimes(1);
      expect(putSpy).toHaveBeenCalledWith(
        `/v1/academics/participations/${encodeURIComponent(participationId)}/status`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('recognizePriorCredit', () => {
    it('issues POST /v1/academics/participations/recognize-prior with payload', async () => {
      const mockRequest: RecognizePriorCreditRequest = {
        studentId,
        learningUnitId: unitId,
        academicPeriodId: periodId,
        academicPeriodLabel: '2026/2027 Odd',
        participationType: 'recognized_prior',
        sourceInstitution: 'SMA Jakarta',
        externalGradeOrCredit: 'A',
        recognitionBasis: 'Senate Approval',
        startDate: '2026-01-01',
        completionDate: '2026-06-01',
      };
      const mockResponse = { id: participationId, status: 'completed' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.recognizePriorCredit(mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/participations/recognize-prior', mockRequest);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('concludeParticipation', () => {
    it('issues POST /v1/academics/participations/{id}/conclude with completion date', async () => {
      const mockRequest: ConcludeParticipationRequest = {
        completionDate: '2026-11-01',
        notes: 'All requirements met',
      };
      const mockResponse = { id: participationId, status: 'completed', completionDate: '2026-11-01' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.concludeParticipation(participationId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/participations/${encodeURIComponent(participationId)}/conclude`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getParticipationsByUnit', () => {
    it('issues GET /v1/academics/learning-units/{id}/participations without query params when filter is omitted', async () => {
      const mockResponse = [{ id: participationId, studentId }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.getParticipationsByUnit(unitId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(unitId)}/participations`,
        { params: {} },
      );
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/academics/learning-units/{id}/participations with populated filter params', async () => {
      const mockResponse = [{ id: participationId, studentId }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.getParticipationsByUnit(unitId, {
        academicPeriodId: periodId,
        sectionLabel: '10-A',
        status: 'in_progress',
      });

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(unitId)}/participations`,
        {
          params: {
            academicPeriodId: periodId,
            sectionLabel: '10-A',
            status: 'in_progress',
          },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getStudentParticipations', () => {
    it('issues GET /v1/academics/students/{id}/participations with optional period filter', async () => {
      const mockResponse = [{ id: participationId, learningUnitName: 'Calculus I' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await participationApi.getStudentParticipations(studentId, {
        academicPeriodId: periodId,
      });

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/participations`,
        {
          params: {
            academicPeriodId: periodId,
          },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
