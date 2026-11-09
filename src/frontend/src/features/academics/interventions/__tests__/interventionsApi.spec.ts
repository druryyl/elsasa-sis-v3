// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getAtRiskLearners,
  getInterventions,
  getInterventionById,
  initiateIntervention,
  updateInterventionStatus,
  interventionsApi,
} from '../api/interventionsApi';
import type {
  AcademicIntervention,
  AtRiskLearner,
  GetAtRiskLearnersParams,
  GetInterventionsParams,
  InitiateInterventionRequest,
  UpdateInterventionStatusRequest,
} from '../types';

describe('Academic Interventions API Client (P9-S26)', () => {
  const interventionId = '11111111-1111-1111-1111-111111111111';
  const studentId = '22222222-2222-2222-2222-222222222222';
  const learningUnitId = '33333333-3333-3333-3333-333333333333';
  const academicPeriodId = '44444444-4444-4444-4444-444444444444';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getAtRiskLearners', () => {
    it('calls GET /v1/academics/interventions/at-risk with period and grade parameters', async () => {
      const mockLearners: AtRiskLearner[] = [
        {
          studentId,
          studentName: 'Budi Santoso',
          learningUnitId,
          learningUnitName: 'Mathematics 10',
          academicPeriodId,
          gradeLevel: 'Grade 10',
          triggerType: 'assessment_below_threshold',
          triggerDescription: 'Score 52.00 is below passing score 70.00',
          currentScore: 52,
          passingScore: 70,
        },
      ];

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockLearners as never);
      const params: GetAtRiskLearnersParams = {
        academicPeriodId,
        gradeLevel: 'Grade 10',
      };

      const result = await getAtRiskLearners(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/interventions/at-risk', {
        params,
      });
      expect(result).toEqual(mockLearners);
    });
  });

  describe('getInterventions', () => {
    it('calls GET /v1/academics/interventions with filter parameters', async () => {
      const mockInterventions: AcademicIntervention[] = [
        {
          id: interventionId,
          studentId,
          studentName: 'Budi Santoso',
          learningUnitId,
          learningUnitName: 'Mathematics 10',
          academicPeriodId,
          interventionType: 'remediation',
          triggerType: 'assessment_below_threshold',
          triggerDescription: 'Midterm test failure',
          planDescription: '3-week structured homework remediation',
          status: 'in_progress',
          initiatedBy: 'Teacher John',
          initiatedAt: '2026-11-01T08:00:00Z',
        },
      ];

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockInterventions as never);
      const params: GetInterventionsParams = {
        academicPeriodId,
        status: 'in_progress',
        studentId,
      };

      const result = await getInterventions(params);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/interventions', {
        params,
      });
      expect(result).toEqual(mockInterventions);
    });

    it('calls GET /v1/academics/interventions without params when omitted', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([] as never);

      const result = await getInterventions();

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/interventions', {
        params: undefined,
      });
      expect(result).toEqual([]);
    });
  });

  describe('getInterventionById', () => {
    it('calls GET /v1/academics/interventions/{id} with encoded URL', async () => {
      const mockIntervention: AcademicIntervention = {
        id: interventionId,
        studentId,
        learningUnitId,
        academicPeriodId,
        interventionType: 'tutoring_recommendation',
        triggerType: 'attendance_concern',
        triggerDescription: 'Attendance dropped to 68%',
        planDescription: 'Peer tutoring sessions twice weekly',
        status: 'planned',
        initiatedBy: 'Coordinator Alice',
        initiatedAt: '2026-11-02T10:00:00Z',
      };

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockIntervention as never);

      const result = await getInterventionById(interventionId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/interventions/${encodeURIComponent(interventionId)}`,
      );
      expect(result).toEqual(mockIntervention);
    });
  });

  describe('initiateIntervention', () => {
    it('calls POST /v1/academics/interventions with formulation payload', async () => {
      const request: InitiateInterventionRequest = {
        studentId,
        learningUnitId,
        academicPeriodId,
        interventionType: 'remediation',
        triggerType: 'assessment_below_threshold',
        triggerDescription: 'Score 52.00 on Midterm Math below passing score 70.00',
        planDescription: 'Provide 3 weeks of targeted remedial worksheets and extra tutoring.',
      };

      const mockResponse: AcademicIntervention = {
        id: interventionId,
        ...request,
        status: 'planned',
        initiatedBy: 'Teacher User',
        initiatedAt: '2026-11-07T00:00:00Z',
      };

      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await initiateIntervention(request);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/interventions', request);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('updateInterventionStatus', () => {
    it('calls PATCH /v1/academics/interventions/{id}/status with status transition and outcome notes', async () => {
      const request: UpdateInterventionStatusRequest = {
        status: 'completed',
        outcomeNotes: 'Student achieved 84.00 on re-assessment; remediation completed successfully.',
      };

      const mockResponse: AcademicIntervention = {
        id: interventionId,
        studentId,
        learningUnitId,
        academicPeriodId,
        interventionType: 'remediation',
        triggerType: 'assessment_below_threshold',
        triggerDescription: 'Failed initial quiz',
        planDescription: 'Worksheet packet',
        status: 'completed',
        initiatedBy: 'Teacher User',
        initiatedAt: '2026-11-01T00:00:00Z',
        concludedAt: '2026-11-07T00:00:00Z',
        outcomeNotes: request.outcomeNotes,
      };

      const patchSpy = vi.spyOn(apiClient, 'patch').mockResolvedValueOnce(mockResponse as never);

      const result = await updateInterventionStatus(interventionId, request);

      expect(patchSpy).toHaveBeenCalledTimes(1);
      expect(patchSpy).toHaveBeenCalledWith(
        `/v1/academics/interventions/${encodeURIComponent(interventionId)}/status`,
        request,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('interventionsApi object barrel', () => {
    it('exposes all client methods via unified object', () => {
      expect(interventionsApi.getAtRiskLearners).toBe(getAtRiskLearners);
      expect(interventionsApi.getInterventions).toBe(getInterventions);
      expect(interventionsApi.getInterventionById).toBe(getInterventionById);
      expect(interventionsApi.initiateIntervention).toBe(initiateIntervention);
      expect(interventionsApi.updateInterventionStatus).toBe(updateInterventionStatus);
    });
  });
});
