// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getWorkloadPolicies,
  getWorkloadCommitments,
  recalculateWorkload,
  getWorkloadExceptions,
  requestWorkloadException,
  approveWorkloadException,
  workloadApi,
} from '../api/workloadApi';
import {
  requestExceptionSchema,
  approveExceptionSchema,
  recalculateWorkloadSchema,
} from '../api/schemas';
import { workloadKeys } from '../api/queryKeys';

describe('Teaching Workload API Client & Schemas (P7-S22)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const exceptionId = '33333333-3333-3333-3333-333333333333';
  const assignmentId = '44444444-4444-4444-4444-444444444444';

  describe('workloadApi Client Functions', () => {
    it('getWorkloadPolicies invokes GET /v1/teaching/workload/policies with query parameters', async () => {
      const mockPolicies = [
        {
          id: '55555555-5555-5555-5555-555555555555',
          code: 'WPOL-2026-PRIMARY',
          name: 'Primary Workload Policy 2026/2027',
          academicYearId: '66666666-6666-6666-6666-666666666666',
          version: '1.0',
          workloadUnit: 'Teaching Hours',
          maxStandardCapacity: 24,
          warningThreshold: 20,
          prohibitedThreshold: 28,
          includesAssessmentEffort: true,
          includesAdvisingEffort: true,
          includesPreparationTime: true,
          calculationRulesSummary: 'Standard rules',
          effectiveStartDate: '2026-07-01',
          status: 'active',
          approvedBy: 'Director',
          approvedDate: '2026-06-15',
          createdAt: '2026-06-15T00:00:00Z',
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPolicies as never);

      const res = await getWorkloadPolicies({
        academicYearId: '66666666-6666-6666-6666-666666666666',
        status: 'active',
      });

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/workload/policies', {
        params: {
          academicYearId: '66666666-6666-6666-6666-666666666666',
          status: 'active',
        },
      });
      expect(res).toEqual(mockPolicies);
    });

    it('getWorkloadCommitments invokes GET /v1/teaching/workload/commitments with query parameters', async () => {
      const mockCommitments = [
        {
          id: '77777777-7777-7777-7777-777777777777',
          teacherProfileId: teacherId,
          teacherNumber: 'TCH-001',
          fullName: 'Budi Hartono',
          standing: 'active',
          academicPeriodId: periodId,
          workloadPolicyId: '55555555-5555-5555-5555-555555555555',
          maxStandardCapacity: 24,
          warningThreshold: 20,
          prohibitedThreshold: 28,
          plannedWorkloadHours: 0,
          committedWorkloadHours: 18,
          maxCapacityLimitHours: 24,
          remainingCapacityHours: 6,
          status: 'normal',
          activeAssignmentCount: 3,
          approvedExceptionCount: 0,
          asOfTimestamp: '2026-11-10T00:00:00Z',
          createdAt: '2026-11-10T00:00:00Z',
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockCommitments as never);

      const res = await getWorkloadCommitments({
        academicPeriodId: periodId,
        status: 'normal',
        search: 'Budi',
      });

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/workload/commitments', {
        params: {
          academicPeriodId: periodId,
          status: 'normal',
          search: 'Budi',
        },
      });
      expect(res).toEqual(mockCommitments);
    });

    it('recalculateWorkload invokes POST /v1/teaching/workload/recalculate with string periodId', async () => {
      const mockResponse = {
        academicPeriodId: periodId,
        recalculatedCount: 15,
        recalculatedAt: '2026-11-10T12:00:00Z',
        message: 'Successfully recalculated 15 faculty commitment records.',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await recalculateWorkload(periodId);

      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/workload/recalculate', {
        academicPeriodId: periodId,
      });
      expect(res).toEqual(mockResponse);
    });

    it('recalculateWorkload accepts request object payload', async () => {
      const mockResponse = {
        academicPeriodId: periodId,
        recalculatedCount: 5,
        recalculatedAt: '2026-11-10T12:00:00Z',
        message: 'Recalculated 5 commitments.',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const res = await recalculateWorkload({ academicPeriodId: periodId });

      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/workload/recalculate', {
        academicPeriodId: periodId,
      });
      expect(res).toEqual(mockResponse);
    });

    it('getWorkloadExceptions invokes GET /v1/teaching/workload/exceptions with query parameters', async () => {
      const mockExceptions = [
        {
          id: exceptionId,
          exceptionNumber: 'WEX-2026-0001',
          teachingAssignmentId: assignmentId,
          teacherProfileId: teacherId,
          workloadPolicyId: '55555555-5555-5555-5555-555555555555',
          exceededHours: 4,
          justification: 'Critical coverage for senior teacher',
          recommendedBy: 'coordinator',
          recommendedDate: '2026-11-10',
          effectiveStartDate: '2026-11-10',
          effectiveEndDate: '2026-12-31',
          status: 'requested',
          createdAt: '2026-11-10T00:00:00Z',
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockExceptions as never);

      const res = await getWorkloadExceptions({
        academicPeriodId: periodId,
        teacherProfileId: teacherId,
        status: 'requested',
      });

      expect(getSpy).toHaveBeenCalledWith('/v1/teaching/workload/exceptions', {
        params: {
          academicPeriodId: periodId,
          teacherProfileId: teacherId,
          status: 'requested',
        },
      });
      expect(res).toEqual(mockExceptions);
    });

    it('requestWorkloadException invokes POST /v1/teaching/workload/exceptions', async () => {
      const payload = {
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        teachingAssignmentId: assignmentId,
        requestedOverloadHours: 4,
        coordinatorJustification: 'Specialist teacher unavailable; John Miller stepping in.',
        effectiveStartDate: '2026-11-10',
        effectiveEndDate: '2026-12-31',
        status: 'requested',
      };
      const mockResult = {
        id: exceptionId,
        exceptionNumber: 'WEX-2026-0001',
        ...payload,
        workloadPolicyId: '55555555-5555-5555-5555-555555555555',
        exceededHours: 4,
        justification: payload.coordinatorJustification,
        recommendedBy: 'coordinator',
        recommendedDate: '2026-11-10',
        createdAt: '2026-11-10T00:00:00Z',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResult as never);

      const res = await requestWorkloadException(payload);

      expect(postSpy).toHaveBeenCalledWith('/v1/teaching/workload/exceptions', payload);
      expect(res).toEqual(mockResult);
    });

    it('approveWorkloadException invokes POST /v1/teaching/workload/exceptions/{id}/approve', async () => {
      const mockResult = {
        id: exceptionId,
        exceptionNumber: 'WEX-2026-0001',
        teachingAssignmentId: assignmentId,
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        exceededHours: 4,
        status: 'approved',
        approvedBy: 'headmaster',
        approvedDate: '2026-11-10',
        decisionRemarks: 'Approved due to urgent shortage.',
        approvedExceptionCount: 1,
        commitmentStatus: 'overloaded_with_exception',
        resolvedConflictsCount: 1,
        approvedAt: '2026-11-10T00:00:00Z',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResult as never);

      const res = await approveWorkloadException(exceptionId, {
        decisionRemarks: 'Approved due to urgent shortage.',
      });

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/teaching/workload/exceptions/${encodeURIComponent(exceptionId)}/approve`,
        { decisionRemarks: 'Approved due to urgent shortage.' },
      );
      expect(res).toEqual(mockResult);
    });

    it('workloadApi object exposes all functions correctly', () => {
      expect(workloadApi.getWorkloadPolicies).toBe(getWorkloadPolicies);
      expect(workloadApi.getWorkloadCommitments).toBe(getWorkloadCommitments);
      expect(workloadApi.recalculateWorkload).toBe(recalculateWorkload);
      expect(workloadApi.getWorkloadExceptions).toBe(getWorkloadExceptions);
      expect(workloadApi.requestWorkloadException).toBe(requestWorkloadException);
      expect(workloadApi.approveWorkloadException).toBe(approveWorkloadException);
    });
  });

  describe('Zod Validation Schemas', () => {
    describe('requestExceptionSchema', () => {
      it('validates a valid workload exception request', () => {
        const validData = {
          teacherProfileId: teacherId,
          academicPeriodId: periodId,
          teachingAssignmentId: assignmentId,
          requestedOverloadHours: 4,
          coordinatorJustification: 'Exceeding standard limit for AP Calculus section.',
          effectiveStartDate: '2026-11-10',
          effectiveEndDate: '2026-12-31',
          status: 'recommended',
        };

        const result = requestExceptionSchema.safeParse(validData);
        expect(result.success).toBe(true);
        if (result.success) {
          expect(result.data.teacherProfileId).toBe(teacherId);
          expect(result.data.requestedOverloadHours).toBe(4);
          expect(result.data.status).toBe('recommended');
        }
      });

      it('rejects invalid or missing required fields', () => {
        const invalidData = {
          teacherProfileId: 'not-a-uuid',
          academicPeriodId: '',
          requestedOverloadHours: 0,
          coordinatorJustification: '',
        };

        const result = requestExceptionSchema.safeParse(invalidData);
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.teacherProfileId).toBeDefined();
          expect(errors.academicPeriodId).toBeDefined();
          expect(errors.requestedOverloadHours).toBeDefined();
          expect(errors.coordinatorJustification).toBeDefined();
        }
      });

      it('rejects requestedOverloadHours exceeding 80 hours', () => {
        const result = requestExceptionSchema.safeParse({
          teacherProfileId: teacherId,
          academicPeriodId: periodId,
          requestedOverloadHours: 85,
          coordinatorJustification: 'Excessive overload',
        });

        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.flatten().fieldErrors.requestedOverloadHours).toContain(
            'Requested overload hours cannot exceed 80 hours per week.',
          );
        }
      });

      it('rejects date range where end date precedes start date', () => {
        const result = requestExceptionSchema.safeParse({
          teacherProfileId: teacherId,
          academicPeriodId: periodId,
          requestedOverloadHours: 4,
          coordinatorJustification: 'Invalid dates',
          effectiveStartDate: '2026-12-31',
          effectiveEndDate: '2026-11-10',
        });

        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.flatten().fieldErrors.effectiveEndDate).toBeDefined();
        }
      });
    });

    describe('approveExceptionSchema', () => {
      it('validates valid decision remarks', () => {
        const result = approveExceptionSchema.safeParse({
          decisionRemarks: 'Approved with condition of bi-weekly check-in.',
        });
        expect(result.success).toBe(true);
        if (result.success) {
          expect(result.data.decisionRemarks).toBe(
            'Approved with condition of bi-weekly check-in.',
          );
        }
      });

      it('allows empty or null decision remarks', () => {
        const result = approveExceptionSchema.safeParse({});
        expect(result.success).toBe(true);
        if (result.success) {
          expect(result.data.decisionRemarks).toBeUndefined();
        }
      });

      it('rejects decision remarks exceeding 2000 characters', () => {
        const result = approveExceptionSchema.safeParse({
          decisionRemarks: 'a'.repeat(2001),
        });
        expect(result.success).toBe(false);
      });
    });

    describe('recalculateWorkloadSchema', () => {
      it('validates a valid UUID academicPeriodId', () => {
        const result = recalculateWorkloadSchema.safeParse({
          academicPeriodId: periodId,
        });
        expect(result.success).toBe(true);
      });

      it('rejects invalid UUID', () => {
        const result = recalculateWorkloadSchema.safeParse({
          academicPeriodId: 'invalid-id',
        });
        expect(result.success).toBe(false);
      });
    });
  });

  describe('queryKeys', () => {
    it('produces hierarchical query keys', () => {
      expect(workloadKeys.all).toEqual(['teaching', 'workload']);
      expect(workloadKeys.policies()).toEqual(['teaching', 'workload', 'policies']);
      expect(workloadKeys.policyList({ status: 'active' })).toEqual([
        'teaching',
        'workload',
        'policies',
        { status: 'active' },
      ]);
      expect(workloadKeys.commitments()).toEqual(['teaching', 'workload', 'commitments']);
      expect(workloadKeys.commitmentList({ academicPeriodId: periodId })).toEqual([
        'teaching',
        'workload',
        'commitments',
        { academicPeriodId: periodId },
      ]);
      expect(workloadKeys.exceptions()).toEqual(['teaching', 'workload', 'exceptions']);
      expect(workloadKeys.exceptionList({ status: 'requested' })).toEqual([
        'teaching',
        'workload',
        'exceptions',
        { status: 'requested' },
      ]);
      expect(workloadKeys.exception(exceptionId)).toEqual([
        'teaching',
        'workload',
        'exceptions',
        exceptionId,
      ]);
    });
  });
});
