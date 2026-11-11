// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as workloadApi from '../api/workloadApi';
import {
  useWorkloadPolicies,
  useWorkloadCommitments,
  useWorkloadExceptions,
  useRecalculateWorkloadMutation,
  useRequestExceptionMutation,
  useApproveExceptionMutation,
} from '../composables/useWorkload';

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

describe('Teaching Workload Composables (P7-S22)', () => {
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const exceptionId = '33333333-3333-3333-3333-333333333333';
  const assignmentId = '44444444-4444-4444-4444-444444444444';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Query Hooks', () => {
    it('useWorkloadPolicies queries policies successfully', async () => {
      const mockPolicies = [
        {
          id: '55555555-5555-5555-5555-555555555555',
          code: 'WPOL-2026',
          name: 'Standard Workload Policy',
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
      vi.spyOn(workloadApi, 'getWorkloadPolicies').mockResolvedValueOnce(mockPolicies);

      const params = ref({ status: 'active' });
      const { result } = withSetup(() => useWorkloadPolicies(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockPolicies);
      expect(workloadApi.getWorkloadPolicies).toHaveBeenCalledWith({ status: 'active' });
    });

    it('useWorkloadCommitments queries materialized snapshots successfully', async () => {
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
      vi.spyOn(workloadApi, 'getWorkloadCommitments').mockResolvedValueOnce(mockCommitments);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useWorkloadCommitments(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockCommitments);
      expect(workloadApi.getWorkloadCommitments).toHaveBeenCalledWith({ academicPeriodId: periodId });
    });

    it('useWorkloadExceptions queries exceptions successfully', async () => {
      const mockExceptions = [
        {
          id: exceptionId,
          exceptionNumber: 'WEX-2026-0001',
          teachingAssignmentId: assignmentId,
          teacherProfileId: teacherId,
          workloadPolicyId: '55555555-5555-5555-5555-555555555555',
          exceededHours: 4,
          justification: 'Overload justification',
          recommendedBy: 'coordinator',
          recommendedDate: '2026-11-10',
          effectiveStartDate: '2026-11-10',
          effectiveEndDate: '2026-12-31',
          status: 'requested',
          createdAt: '2026-11-10T00:00:00Z',
        },
      ];
      vi.spyOn(workloadApi, 'getWorkloadExceptions').mockResolvedValueOnce(mockExceptions);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useWorkloadExceptions(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockExceptions);
      expect(workloadApi.getWorkloadExceptions).toHaveBeenCalledWith({ academicPeriodId: periodId });
    });
  });

  describe('Mutation Hooks', () => {
    it('useRecalculateWorkloadMutation triggers API call, invalidates cache, and notifies success', async () => {
      const mockResponse = {
        academicPeriodId: periodId,
        recalculatedCount: 12,
        recalculatedAt: '2026-11-10T12:00:00Z',
        message: 'Successfully reconciled 12 faculty commitment records.',
      };
      vi.spyOn(workloadApi, 'recalculateWorkload').mockResolvedValueOnce(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useRecalculateWorkloadMutation({ onSuccess }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(periodId);

      expect(workloadApi.recalculateWorkload).toHaveBeenCalledWith(periodId);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['teaching', 'workload', 'commitments'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['teaching', 'workload'],
      });
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, periodId);
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Workload Recalculated',
        }),
      );
    });

    it('useRequestExceptionMutation triggers API call, invalidates cache, and notifies success', async () => {
      const payload = {
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        teachingAssignmentId: assignmentId,
        requestedOverloadHours: 4,
        coordinatorJustification: 'Staff shortage coverage',
      };
      const mockResponse = {
        id: exceptionId,
        exceptionNumber: 'WEX-2026-0001',
        teachingAssignmentId: assignmentId,
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        workloadPolicyId: '55555555-5555-5555-5555-555555555555',
        exceededHours: 4,
        justification: payload.coordinatorJustification,
        recommendedBy: 'coordinator',
        recommendedDate: '2026-11-10',
        effectiveStartDate: '2026-11-10',
        effectiveEndDate: '2026-12-31',
        status: 'requested',
        createdAt: '2026-11-10T00:00:00Z',
      };
      vi.spyOn(workloadApi, 'requestWorkloadException').mockResolvedValueOnce(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useRequestExceptionMutation({ onSuccess }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ payload });

      expect(workloadApi.requestWorkloadException).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['teaching', 'workload', 'exceptions'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['teaching', 'workload', 'commitments'],
      });
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, { payload });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Exception Requested',
        }),
      );
    });

    it('useApproveExceptionMutation triggers API call, invalidates cache, and notifies success', async () => {
      const mockResponse = {
        id: exceptionId,
        exceptionNumber: 'WEX-2026-0001',
        teachingAssignmentId: assignmentId,
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        exceededHours: 4,
        status: 'approved',
        approvedBy: 'headmaster',
        approvedDate: '2026-11-10',
        decisionRemarks: 'Approved',
        approvedExceptionCount: 1,
        commitmentStatus: 'overloaded_with_exception',
        resolvedConflictsCount: 1,
        approvedAt: '2026-11-10T00:00:00Z',
      };
      vi.spyOn(workloadApi, 'approveWorkloadException').mockResolvedValueOnce(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useApproveExceptionMutation({ onSuccess }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: exceptionId,
        payload: { decisionRemarks: 'Approved' },
      });

      expect(workloadApi.approveWorkloadException).toHaveBeenCalledWith(exceptionId, {
        decisionRemarks: 'Approved',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['teaching', 'workload', 'exceptions'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['teaching', 'workload', 'commitments'],
      });
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, {
        id: exceptionId,
        payload: { decisionRemarks: 'Approved' },
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Exception Approved',
        }),
      );
    });
  });
});
