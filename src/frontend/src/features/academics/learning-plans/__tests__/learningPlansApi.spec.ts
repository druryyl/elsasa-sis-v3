// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getLearningPlans,
  getLearningPlanById,
  getActiveLearningPlan,
  createPlan,
  updatePlan,
  archivePlan,
  addUnit,
  removeUnit,
  addAdjustment,
  removeAdjustment,
  submitPlan,
  approvePlan,
  revisePlan,
  syncParticipations,
  learningPlansApi,
} from '../api/learningPlansApi';
import type {
  ActiveStudentLearningPlan,
  AddAdjustmentRequest,
  AddUnitRequest,
  ApprovePlanRequest,
  CreatePlanRequest,
  GetLearningPlansParams,
  RevisePlanRequest,
  SlpApproval,
  StudentLearningPlan,
  StudentLearningPlanListItem,
  SyncParticipationsResponse,
  UpdatePlanRequest,
} from '../types';

describe('Student Learning Plans API Client (P6-S17)', () => {
  const planId = '11111111-1111-1111-1111-111111111111';
  const studentId = '22222222-2222-2222-2222-222222222222';
  const periodId = '33333333-3333-3333-3333-333333333333';
  const unitId = '44444444-4444-4444-4444-444444444444';
  const adjustmentId = '55555555-5555-5555-5555-555555555555';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getLearningPlans', () => {
    it('calls GET /v1/academics/learning-plans with query parameters', async () => {
      const mockResult = {
        items: [
          {
            id: planId,
            studentId,
            studentName: 'Budi Santoso',
            studentNis: 'HAG-2026-001',
            academicPeriodId: periodId,
            academicPeriodLabel: '2026/2027 Odd',
            status: 'active',
            version: 2,
            unitsCount: 3,
            adjustmentsCount: 2,
            createdAt: '2026-08-01T00:00:00Z',
          } as StudentLearningPlanListItem,
        ],
        pageNumber: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResult as never);
      const params: GetLearningPlansParams = {
        academicPeriodId: periodId,
        status: 'active',
        studentSearch: 'Budi',
        page: 1,
        pageSize: 20,
      };

      const result = await getLearningPlans(params);

      expect(getSpy).toHaveBeenCalledWith('/v1/academics/learning-plans', {
        params: {
          academicPeriodId: periodId,
          status: 'active',
          studentSearch: 'Budi',
          page: 1,
          pageSize: 20,
        },
      });
      expect(result).toEqual(mockResult);
    });
  });

  describe('getLearningPlanById', () => {
    it('calls GET /v1/academics/learning-plans/{id}', async () => {
      const mockPlan: StudentLearningPlan = {
        id: planId,
        studentId,
        academicPeriodId: periodId,
        academicPeriodLabel: '2026/2027 Odd',
        status: 'active',
        version: 1,
        notes: 'Initial plan',
        createdAt: '2026-08-01T00:00:00Z',
        createdBy: 'coordinator@hagios.sch.id',
        constituentUnits: [],
        adjustments: [],
        approvals: [],
      };

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPlan as never);

      const result = await getLearningPlanById(planId);

      expect(getSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}`);
      expect(result).toEqual(mockPlan);
    });
  });

  describe('getActiveLearningPlan', () => {
    it('calls GET /v1/academics/students/{studentId}/learning-plans/active with periodId', async () => {
      const mockActivePlan: ActiveStudentLearningPlan = {
        planId,
        studentId,
        academicPeriodId: periodId,
        academicPeriodLabel: '2026/2027 Odd',
        version: 1,
        status: 'active',
        notes: 'Active plan',
        constituentUnits: [],
        adjustments: [],
      };

      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockActivePlan as never);

      const result = await getActiveLearningPlan(studentId, periodId);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${studentId}/learning-plans/active`,
        { params: { academicPeriodId: periodId } },
      );
      expect(result).toEqual(mockActivePlan);
    });

    it('returns null when 404 is returned', async () => {
      vi.spyOn(apiClient, 'get').mockRejectedValueOnce({ statusCode: 404 });

      const result = await getActiveLearningPlan(studentId, periodId);
      expect(result).toBeNull();
    });
  });

  describe('createPlan', () => {
    it('calls POST /v1/academics/learning-plans', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: planId } as never);
      const req: CreatePlanRequest = {
        studentId,
        academicPeriodId: periodId,
        notes: 'New plan notes',
      };

      const result = await createPlan(req);

      expect(postSpy).toHaveBeenCalledWith('/v1/academics/learning-plans', req);
      expect(result).toEqual({ id: planId });
    });
  });

  describe('updatePlan', () => {
    it('calls PUT /v1/academics/learning-plans/{id}', async () => {
      const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(undefined as never);
      const req: UpdatePlanRequest = {
        notes: 'Updated notes',
      };

      await updatePlan(planId, req);

      expect(putSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}`, req);
    });
  });

  describe('archivePlan', () => {
    it('calls POST /v1/academics/learning-plans/{id}/archive', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(undefined as never);

      await archivePlan(planId);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/archive`, {});
    });
  });

  describe('addUnit & removeUnit', () => {
    it('calls POST /v1/academics/learning-plans/{id}/units', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: unitId } as never);
      const req: AddUnitRequest = {
        learningUnitId: unitId,
        sequenceOrder: 1,
        isRequired: true,
        notes: 'Pacing notes',
      };

      const result = await addUnit(planId, req);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/units`, req);
      expect(result).toEqual({ id: unitId });
    });

    it('calls DELETE /v1/academics/learning-plans/{id}/units/{unitId}', async () => {
      const delSpy = vi.spyOn(apiClient, 'delete').mockResolvedValueOnce(undefined as never);

      await removeUnit(planId, unitId);

      expect(delSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/units/${unitId}`);
    });
  });

  describe('addAdjustment & removeAdjustment', () => {
    it('calls POST /v1/academics/learning-plans/{id}/adjustments', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: adjustmentId } as never);
      const req: AddAdjustmentRequest = {
        adjustmentType: 'accommodation',
        description: '50% extra exam time',
        rationale: 'Processing delay verified by psychologist',
        effectiveDate: '2026-08-01',
      };

      const result = await addAdjustment(planId, req);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/adjustments`, req);
      expect(result).toEqual({ id: adjustmentId });
    });

    it('calls DELETE /v1/academics/learning-plans/{id}/adjustments/{adjustmentId}', async () => {
      const delSpy = vi.spyOn(apiClient, 'delete').mockResolvedValueOnce(undefined as never);

      await removeAdjustment(planId, adjustmentId);

      expect(delSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/adjustments/${adjustmentId}`);
    });
  });

  describe('submitPlan', () => {
    it('calls POST /v1/academics/learning-plans/{id}/submit', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(undefined as never);

      await submitPlan(planId);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/submit`);
    });
  });

  describe('approvePlan', () => {
    it('calls POST /v1/academics/learning-plans/{id}/approve', async () => {
      const mockApproval: SlpApproval = {
        id: 'app-1',
        planVersion: 1,
        approvedBy: 'authority@hagios.sch.id',
        approvedDate: '2026-08-01',
        approvalType: 'initial_approval',
        notes: 'Approved as submitted',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockApproval as never);
      const req: ApprovePlanRequest = {
        approvalType: 'initial_approval',
        notes: 'Approved as submitted',
      };

      const result = await approvePlan(planId, req);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/approve`, req);
      expect(result).toEqual(mockApproval);
    });
  });

  describe('revisePlan', () => {
    it('calls POST /v1/academics/learning-plans/{id}/revise', async () => {
      const newPlanId = '66666666-6666-6666-6666-666666666666';
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: newPlanId } as never);
      const req: RevisePlanRequest = {
        notes: 'Adding adaptive movement unit',
      };

      const result = await revisePlan(planId, req);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/revise`, req);
      expect(result).toEqual({ id: newPlanId });
    });
  });

  describe('syncParticipations', () => {
    it('calls POST /v1/academics/learning-plans/{id}/sync-participations', async () => {
      const mockSync: SyncParticipationsResponse = {
        planId,
        studentId,
        syncedCount: 2,
        updatedCount: 1,
        totalUnits: 3,
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockSync as never);

      const result = await syncParticipations(planId);

      expect(postSpy).toHaveBeenCalledWith(`/v1/academics/learning-plans/${planId}/sync-participations`);
      expect(result).toEqual(mockSync);
    });
  });

  describe('learningPlansApi object', () => {
    it('exports all methods under the learningPlansApi object', () => {
      expect(learningPlansApi.getLearningPlans).toBeDefined();
      expect(learningPlansApi.getLearningPlanById).toBeDefined();
      expect(learningPlansApi.getActiveLearningPlan).toBeDefined();
      expect(learningPlansApi.createPlan).toBeDefined();
      expect(learningPlansApi.updatePlan).toBeDefined();
      expect(learningPlansApi.archivePlan).toBeDefined();
      expect(learningPlansApi.addUnit).toBeDefined();
      expect(learningPlansApi.removeUnit).toBeDefined();
      expect(learningPlansApi.addAdjustment).toBeDefined();
      expect(learningPlansApi.removeAdjustment).toBeDefined();
      expect(learningPlansApi.submitPlan).toBeDefined();
      expect(learningPlansApi.approvePlan).toBeDefined();
      expect(learningPlansApi.revisePlan).toBeDefined();
      expect(learningPlansApi.syncParticipations).toBeDefined();
    });
  });
});
