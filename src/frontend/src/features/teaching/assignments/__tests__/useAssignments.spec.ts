// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as assignmentsApi from '../api/assignmentsApi';
import {
  useApproveAssignmentMutation,
  useApproveSubstituteExceptionMutation,
  useArrangeSubstituteMutation,
  useAssignmentById,
  useAssignmentRoles,
  useAssignments,
  useCapacityConflicts,
  useCompletions,
  useCreateDemandMutation,
  useDemandById,
  useDemands,
  useEvaluateCandidateMutation,
  useEvaluations,
  useProposeAssignmentMutation,
  useReassignmentById,
  useReassignments,
  useResolveConflictMutation,
  useSubstitutions,
  useTeacherHistory,
  useUpdateDemandStatusMutation,
  useConcludeAssignmentMutation,
} from '../composables/useAssignments';

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

describe('Teaching Assignments Composables (P7-S21)', () => {
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const demandId = '22222222-2222-2222-2222-222222222222';
  const assignmentId = '33333333-3333-3333-3333-333333333333';
  const roleId = '44444444-4444-4444-4444-444444444444';
  const conflictId = '55555555-5555-5555-5555-555555555555';
  const substitutionId = '77777777-7777-7777-7777-777777777777';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ==========================================================================
  // Query Hooks Tests
  // ==========================================================================
  describe('Query Hooks', () => {
    it('useDemands queries demands list successfully', async () => {
      const mockDemands = {
        items: [{ id: demandId, demandCode: 'DEM-2026-0001' }],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(assignmentsApi, 'getTeachingDemands').mockResolvedValueOnce(mockDemands as never);

      const params = ref({ status: 'received' });
      const { result } = withSetup(() => useDemands(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockDemands);
      expect(assignmentsApi.getTeachingDemands).toHaveBeenCalledWith({ status: 'received' });
    });

    it('useDemandById queries a single demand by ID', async () => {
      const mockDemand = { id: demandId, demandCode: 'DEM-2026-0001' };
      vi.spyOn(assignmentsApi, 'getTeachingDemandById').mockResolvedValueOnce(mockDemand as never);

      const idRef = ref(demandId);
      const { result } = withSetup(() => useDemandById(idRef));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockDemand);
      expect(assignmentsApi.getTeachingDemandById).toHaveBeenCalledWith(demandId);
    });

    it('useEvaluations queries candidate evaluations', async () => {
      const mockEvaluations = [{ id: 'eval-1', demandCode: 'DEM-2026-0001' }];
      vi.spyOn(assignmentsApi, 'getCandidateEvaluations').mockResolvedValueOnce(mockEvaluations as never);

      const params = ref({ teachingDemandId: demandId });
      const { result } = withSetup(() => useEvaluations(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockEvaluations);
      expect(assignmentsApi.getCandidateEvaluations).toHaveBeenCalledWith({ teachingDemandId: demandId });
    });

    it('useAssignments queries assignments list', async () => {
      const mockAssignments = { items: [{ id: assignmentId }], totalCount: 1 };
      vi.spyOn(assignmentsApi, 'getTeachingAssignments').mockResolvedValueOnce(mockAssignments as never);

      const { result } = withSetup(() => useAssignments());

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockAssignments);
    });

    it('useAssignmentById queries assignment by ID', async () => {
      const mockAssignment = { id: assignmentId, assignmentNumber: 'ASN-2026-0001' };
      vi.spyOn(assignmentsApi, 'getTeachingAssignmentById').mockResolvedValueOnce(mockAssignment as never);

      const { result } = withSetup(() => useAssignmentById(assignmentId));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockAssignment);
    });

    it('useAssignmentRoles queries roles catalog', async () => {
      const mockRoles = [{ id: roleId, code: 'LEAD_TEACHER', name: 'Guru Utama' }];
      vi.spyOn(assignmentsApi, 'getAssignmentRoles').mockResolvedValueOnce(mockRoles as never);

      const { result } = withSetup(() => useAssignmentRoles({ activeOnly: true }));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockRoles);
    });

    it('useCapacityConflicts queries conflicts list', async () => {
      const mockConflicts = { items: [{ id: conflictId }], totalCount: 1 };
      vi.spyOn(assignmentsApi, 'getCapacityConflicts').mockResolvedValueOnce(mockConflicts as never);

      const { result } = withSetup(() => useCapacityConflicts());

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockConflicts);
    });

    it('useSubstitutions queries substitutions list', async () => {
      const mockSubstitutions = { items: [{ id: substitutionId }], totalCount: 1 };
      vi.spyOn(assignmentsApi, 'getTeachingSubstitutions').mockResolvedValueOnce(mockSubstitutions as never);

      const { result } = withSetup(() => useSubstitutions());

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockSubstitutions);
    });

    it('useReassignments queries reassignments list', async () => {
      const mockReassignments = { items: [{ id: 'rea-1' }], totalCount: 1 };
      vi.spyOn(assignmentsApi, 'getTeachingReassignments').mockResolvedValueOnce(mockReassignments as never);

      const { result } = withSetup(() => useReassignments());

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockReassignments);
    });

    it('useReassignmentById queries a specific reassignment by ID', async () => {
      const mockReassignment = { id: 'rea-1', reassignmentNumber: 'REA-2026-0001' };
      vi.spyOn(assignmentsApi, 'getTeachingReassignmentById').mockResolvedValueOnce(mockReassignment as never);

      const { result } = withSetup(() => useReassignmentById('rea-1'));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockReassignment);
    });

    it('useCompletions queries completed assignments list', async () => {
      const mockCompletions = [{ id: 'comp-1', teachingAssignmentId: assignmentId }];
      vi.spyOn(assignmentsApi, 'getAssignmentCompletions').mockResolvedValueOnce(mockCompletions as never);

      const { result } = withSetup(() => useCompletions());

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockCompletions);
    });

    it('useTeacherHistory queries immutable history log', async () => {
      const mockHistory = [{ id: 'hist-1', eventType: 'AssignmentApproved' }];
      vi.spyOn(assignmentsApi, 'getTeacherHistory').mockResolvedValueOnce(mockHistory as never);

      const { result } = withSetup(() => useTeacherHistory(teacherId));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });

      expect(result.data.value).toEqual(mockHistory);
    });
  });

  // ==========================================================================
  // Mutation Hooks Tests
  // ==========================================================================
  describe('Mutation Hooks', () => {
    it('useCreateDemandMutation triggers API, invalidates demands cache, and toasts success', async () => {
      const payload = {
        sourceDomain: 'Academics',
        sourceWorkReference: '99999999-9999-9999-9999-999999999999',
        sourceWorkLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        requiredCapabilityCode: 'CAP-MATH-PRI',
        requiredAssignmentRoleId: roleId,
        expectedWorkloadHours: 24,
        requestedStartDate: '2026-07-01',
        requestedEndDate: '2026-12-31',
      };
      const mockResponse = {
        id: demandId,
        demandCode: 'DEM-2026-0001',
        status: 'received',
        sourceDomain: 'Academics',
        sourceWorkLabel: 'Matematika SD Kelas 4',
        createdAt: '2026-11-10T00:00:00Z',
      };
      vi.spyOn(assignmentsApi, 'createTeachingDemand').mockResolvedValueOnce(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() => useCreateDemandMutation({ onSuccess }));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(payload);

      expect(assignmentsApi.createTeachingDemand).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'demands'] });
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, payload);
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Demand Created',
        }),
      );
    });

    it('useUpdateDemandStatusMutation triggers API, invalidates demands cache, and toasts success', async () => {
      const mockResponse = {
        id: demandId,
        demandCode: 'DEM-2026-0001',
        status: 'accepted_for_staffing',
      };
      vi.spyOn(assignmentsApi, 'updateTeachingDemandStatus').mockResolvedValueOnce(mockResponse as never);

      const { result, queryClient } = withSetup(() => useUpdateDemandStatusMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: demandId,
        payload: { status: 'accepted_for_staffing' },
      });

      expect(assignmentsApi.updateTeachingDemandStatus).toHaveBeenCalledWith(demandId, {
        status: 'accepted_for_staffing',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'demands'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Demand Status Updated',
        }),
      );
    });

    it('useEvaluateCandidateMutation triggers API, invalidates evaluations & demands, and toasts determination', async () => {
      const payload = { teachingDemandId: demandId, teacherProfileId: teacherId };
      const mockResponse = {
        id: 'eval-1',
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        demandCode: 'DEM-2026-0001',
        teacherName: 'Budi Hartono',
        teacherNumber: 'TCH-001',
        evaluationDate: '2026-11-10',
        evaluatedBy: 'coordinator',
        isEligible: true,
        eligibilityStatus: 'eligible',
        profileActiveCheck: true,
        authorizationCheckPassed: true,
        qualificationCheckPassed: true,
        availabilityCheckPassed: true,
        capacityCheckPassed: true,
        explanation: 'All 5 eligibility criteria satisfied.',
        createdAt: '2026-11-10T00:00:00Z',
      };
      vi.spyOn(assignmentsApi, 'evaluateCandidate').mockResolvedValueOnce(mockResponse);

      const { result, queryClient } = withSetup(() => useEvaluateCandidateMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(payload);

      expect(assignmentsApi.evaluateCandidate).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'evaluations'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'demands'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Candidate Eligible',
        }),
      );
    });

    it('useProposeAssignmentMutation triggers API, invalidates assignments, demands, and workload', async () => {
      const payload = {
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        assignmentRoleId: roleId,
        assignmentScope: 'learning_unit',
        scopeReferenceId: '99999999-9999-9999-9999-999999999999',
        scopeLabel: 'Matematika SD Kelas 4',
        academicPeriodId: periodId,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        workloadCommitmentValue: 18,
      };
      const mockResponse = {
        id: assignmentId,
        assignmentNumber: 'ASN-2026-0001',
        status: 'proposed',
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        workloadCommitmentValue: 18,
        createdAt: '2026-11-10T00:00:00Z',
      };
      vi.spyOn(assignmentsApi, 'proposeTeachingAssignment').mockResolvedValueOnce(mockResponse);

      const { result, queryClient } = withSetup(() => useProposeAssignmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(payload);

      expect(assignmentsApi.proposeTeachingAssignment).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'assignments'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'demands'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'workload'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assignment Proposed',
        }),
      );
    });

    it('useApproveAssignmentMutation triggers API, invalidates assignments, demands, conflicts, and workload', async () => {
      const mockResponse = {
        id: assignmentId,
        assignmentNumber: 'ASN-2026-0001',
        status: 'approved',
        approvedDate: '2026-11-10',
        approvedBy: 'Director',
        approvedAt: '2026-11-10T00:00:00Z',
      };
      vi.spyOn(assignmentsApi, 'approveTeachingAssignment').mockResolvedValueOnce(mockResponse);

      const { result, queryClient } = withSetup(() => useApproveAssignmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ id: assignmentId, payload: { notes: 'Approved' } });

      expect(assignmentsApi.approveTeachingAssignment).toHaveBeenCalledWith(assignmentId, {
        notes: 'Approved',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'assignments'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'demands'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'conflicts'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'workload'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assignment Approved',
        }),
      );
    });

    it('useResolveConflictMutation triggers API, invalidates conflicts, assignments, and workload', async () => {
      const mockResponse = {
        id: conflictId,
        status: 'resolved',
        resolutionAction: 'workload_exception_granted',
        resolvedBy: 'coordinator',
        resolvedDate: '2026-11-10',
        resolvedAt: '2026-11-10T00:00:00Z',
        resolutionNotes: 'Resolved with exception',
      };
      vi.spyOn(assignmentsApi, 'resolveCapacityConflict').mockResolvedValueOnce(mockResponse);

      const { result, queryClient } = withSetup(() => useResolveConflictMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: conflictId,
        payload: { status: 'resolved', resolutionNotes: 'Resolved with exception' },
      });

      expect(assignmentsApi.resolveCapacityConflict).toHaveBeenCalledWith(conflictId, {
        status: 'resolved',
        resolutionNotes: 'Resolved with exception',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'conflicts'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'assignments'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'workload'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Conflict Resolved',
        }),
      );
    });

    it('useArrangeSubstituteMutation triggers API and invalidates substitutions, assignments, conflicts, workload', async () => {
      const payload = {
        originalAssignmentId: assignmentId,
        substituteTeacherId: '44444444-4444-4444-4444-444444444444',
        startDate: '2026-09-01',
        endDate: '2026-09-14',
        coverageScopeType: 'full',
        reason: 'medical_leave',
        workloadImpactHours: 18,
      };
      const mockResponse = {
        id: substitutionId,
        substitutionNumber: 'SUB-2026-0001',
        originalAssignmentId: assignmentId,
        originalTeacherId: teacherId,
        substituteTeacherId: '44444444-4444-4444-4444-444444444444',
        coverageStartDate: '2026-09-01',
        coverageEndDate: '2026-09-14',
        coverageScopeType: 'full',
        reason: 'medical_leave',
        workloadImpactHours: 18,
        status: 'approved',
        approvedBy: 'coordinator',
        approvedDate: '2026-09-01',
        isDelegatedApproval: true,
        requiresAuthorityApproval: false,
        createdAt: '2026-09-01T00:00:00Z',
      };
      vi.spyOn(assignmentsApi, 'arrangeEmergencySubstitution').mockResolvedValueOnce(mockResponse);

      const { result, queryClient } = withSetup(() => useArrangeSubstituteMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(payload);

      expect(assignmentsApi.arrangeEmergencySubstitution).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'substitutions'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'assignments'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'conflicts'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'workload'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Substitute Assigned',
        }),
      );
    });

    it('useApproveSubstituteExceptionMutation triggers API, invalidates substitutions, conflicts, workload', async () => {
      const mockResponse = {
        id: substitutionId,
        substitutionNumber: 'SUB-2026-0001',
        status: 'approved',
      };
      vi.spyOn(assignmentsApi, 'approveSubstitutionException').mockResolvedValueOnce(mockResponse as never);

      const { result, queryClient } = withSetup(() => useApproveSubstituteExceptionMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ id: substitutionId, payload: { decisionRemarks: 'Approved' } });

      expect(assignmentsApi.approveSubstitutionException).toHaveBeenCalledWith(substitutionId, {
        decisionRemarks: 'Approved',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'substitutions'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'conflicts'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'workload'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Substitute Exception Approved',
        }),
      );
    });

    it('useConcludeAssignmentMutation triggers API, invalidates completions, assignments, workload', async () => {
      const payload = {
        completionDate: '2026-12-20',
        academicConfirmationReceived: true,
        handoverConfirmed: true,
      };
      const mockResponse = {
        id: 'comp-1',
        teachingAssignmentId: assignmentId,
        assignmentNumber: 'ASN-2026-0001',
        completionDate: '2026-12-20',
        academicConfirmationReceived: true,
        handoverConfirmed: true,
        unresolvedItemsStatus: 'none',
        capacityReleasedDate: '2026-12-20',
        releasedWorkloadHours: 18,
        completedBy: 'coordinator',
        status: 'completed',
        createdAt: '2026-12-20T00:00:00Z',
      };
      vi.spyOn(assignmentsApi, 'concludeTeachingAssignment').mockResolvedValueOnce(mockResponse);

      const { result, queryClient } = withSetup(() => useConcludeAssignmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ id: assignmentId, payload });

      expect(assignmentsApi.concludeTeachingAssignment).toHaveBeenCalledWith(assignmentId, payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'completions'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'assignments'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['teaching', 'workload'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Assignment Concluded',
        }),
      );
    });
  });
});
