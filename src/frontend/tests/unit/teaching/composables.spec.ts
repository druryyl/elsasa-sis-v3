// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as assignmentsApi from '@/features/teaching/assignments/api/assignmentsApi';
import * as workloadApi from '@/features/teaching/workload/api/workloadApi';
import * as availabilityApi from '@/features/teaching/availability/api/availabilityApi';
import {
  useDemands,
  useDemandById,
  useEvaluations,
  useAssignments,
  useAssignmentById,
  useAssignmentRoles,
  useCapacityConflicts,
  useSubstitutions,
  useReassignments,
  useReassignmentById,
  useCompletions,
  useTeacherHistory,
  useCreateDemandMutation,
  useUpdateDemandMutation,
  useUpdateDemandStatusMutation,
  useEvaluateCandidateMutation,
  useProposeAssignmentMutation,
  useApproveAssignmentMutation,
  useAcknowledgeAssignmentMutation,
  useUpdateAssignmentStatusMutation,
  useResolveConflictMutation,
  useArrangeSubstituteMutation,
  useApproveSubstituteExceptionMutation,
  useCancelSubstitutionMutation,
  useProposeReassignmentMutation,
  useApproveReassignmentMutation,
  useConfirmReassignmentHandoverMutation,
  useConcludeAssignmentMutation,
} from '@/features/teaching/assignments/composables/useAssignments';
import {
  useWorkloadPolicies,
  useWorkloadCommitments,
  useWorkloadExceptions,
  useRecalculateWorkloadMutation,
  useRequestExceptionMutation,
  useApproveExceptionMutation,
} from '@/features/teaching/workload/composables/useWorkload';
import {
  useTeacherAvailability,
  useUnavailabilityWindows,
  useDeclareAvailabilityMutation,
  useCreateUnavailabilityMutation,
  useCancelUnavailabilityMutation,
} from '@/features/teaching/availability/composables/useAvailability';
import {
  demandsKeys,
  evaluationsKeys,
  assignmentsKeys,
  conflictsKeys,
  substitutionsKeys,
  reassignmentsKeys,
  completionsKeys,
} from '@/features/teaching/assignments/api/queryKeys';
import { workloadKeys } from '@/features/teaching/workload/api/queryKeys';
import { availabilityKeys } from '@/features/teaching/availability/api/queryKeys';

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

describe('Teaching Query Composables & Cache Invalidation (P9-S29)', () => {
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const teacherId = '11111111-1111-1111-1111-111111111111';
  const demandId = '22222222-2222-2222-2222-222222222222';
  const assignmentId = '33333333-3333-3333-3333-333333333333';
  const conflictId = '44444444-4444-4444-4444-444444444444';
  const substitutionId = '55555555-5555-5555-5555-555555555555';
  const reassignmentId = '66666666-6666-6666-6666-666666666666';
  const exceptionId = '77777777-7777-7777-7777-777777777777';
  const windowId = '88888888-8888-8888-8888-888888888888';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ==========================================================================
  // 1. Assignments Module Composables
  // ==========================================================================
  describe('useAssignments Query Hooks', () => {
    it('useDemands queries paginated teaching demands', async () => {
      const mockData = { items: [{ id: demandId, demandCode: 'DEM-001' }], totalCount: 1 };
      vi.spyOn(assignmentsApi, 'getTeachingDemands').mockResolvedValueOnce(mockData as never);

      const params = ref({ status: 'received' as const });
      const { result } = withSetup(() => useDemands(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockData);
      expect(assignmentsApi.getTeachingDemands).toHaveBeenCalledWith({ status: 'received' });
    });

    it('useDemandById queries a specific demand by ID', async () => {
      const mockDemand = { id: demandId, demandCode: 'DEM-001' };
      vi.spyOn(assignmentsApi, 'getTeachingDemandById').mockResolvedValueOnce(mockDemand as never);

      const idRef = ref(demandId);
      const { result } = withSetup(() => useDemandById(idRef));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockDemand);
    });

    it('useEvaluations queries candidate evaluations', async () => {
      const mockEvals = [{ evaluationId: 'eval-1', isEligible: true }];
      vi.spyOn(assignmentsApi, 'getCandidateEvaluations').mockResolvedValueOnce(mockEvals as never);

      const params = ref({ teachingDemandId: demandId });
      const { result } = withSetup(() => useEvaluations(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockEvals);
    });

    it('useAssignments queries assignments list', async () => {
      const mockAssignments = { items: [{ id: assignmentId, status: 'approved' }] };
      vi.spyOn(assignmentsApi, 'getTeachingAssignments').mockResolvedValueOnce(mockAssignments as never);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useAssignments(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockAssignments);
    });

    it('useAssignmentById queries single assignment', async () => {
      const mockAssignment = { id: assignmentId, status: 'approved' };
      vi.spyOn(assignmentsApi, 'getTeachingAssignmentById').mockResolvedValueOnce(mockAssignment as never);

      const idRef = ref(assignmentId);
      const { result } = withSetup(() => useAssignmentById(idRef));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockAssignment);
    });

    it('useAssignmentRoles queries catalog of roles', async () => {
      const mockRoles = [{ id: 'role-1', code: 'GURU_UTAMA' }];
      vi.spyOn(assignmentsApi, 'getAssignmentRoles').mockResolvedValueOnce(mockRoles as never);

      const { result } = withSetup(() => useAssignmentRoles());

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockRoles);
    });

    it('useCapacityConflicts queries conflicts list', async () => {
      const mockConflicts = { items: [{ id: conflictId, severity: 'blocking' }] };
      vi.spyOn(assignmentsApi, 'getCapacityConflicts').mockResolvedValueOnce(mockConflicts as never);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useCapacityConflicts(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockConflicts);
    });

    it('useSubstitutions queries coverage substitutions', async () => {
      const mockSubs = [{ id: substitutionId, coverageScopeType: 'full' }];
      vi.spyOn(assignmentsApi, 'getTeachingSubstitutions').mockResolvedValueOnce(mockSubs as never);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useSubstitutions(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockSubs);
    });

    it('useReassignments and useReassignmentById query reassignment data', async () => {
      const mockReassignments = [{ id: reassignmentId, status: 'approved' }];
      vi.spyOn(assignmentsApi, 'getTeachingReassignments').mockResolvedValueOnce(mockReassignments as never);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useReassignments(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockReassignments);

      vi.spyOn(assignmentsApi, 'getTeachingReassignmentById').mockResolvedValueOnce(mockReassignments[0] as never);
      const idRef = ref(reassignmentId);
      const { result: detailResult } = withSetup(() => useReassignmentById(idRef));

      await vi.waitFor(() => {
        expect(detailResult.isSuccess.value).toBe(true);
      });
      expect(detailResult.data.value).toEqual(mockReassignments[0]);
    });

    it('useCompletions queries concluded assignments list', async () => {
      const mockCompletions = [{ id: 'comp-1', assignmentId }];
      vi.spyOn(assignmentsApi, 'getAssignmentCompletions').mockResolvedValueOnce(mockCompletions as never);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useCompletions(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockCompletions);
    });

    it('useTeacherHistory queries audit logs for a teacher', async () => {
      const mockHistory = [{ id: 'hist-1', eventType: 'ASSIGNMENT_APPROVED' }];
      vi.spyOn(assignmentsApi, 'getTeacherHistory').mockResolvedValueOnce(mockHistory as never);

      const tRef = ref(teacherId);
      const { result } = withSetup(() => useTeacherHistory(tRef));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockHistory);
    });
  });

  describe('useAssignments Mutation Hooks & Cache Invalidation', () => {
    it('useCreateDemandMutation, useUpdateDemandMutation, and useUpdateDemandStatusMutation invalidate demands cache', async () => {
      const mockRes = { id: demandId, demandCode: 'DEM-001' };
      vi.spyOn(assignmentsApi, 'createTeachingDemand').mockResolvedValueOnce(mockRes as never);

      const { result: createMut, queryClient: qc1 } = withSetup(() => useCreateDemandMutation());
      const invSpy1 = vi.spyOn(qc1, 'invalidateQueries');

      await createMut.mutateAsync({
        sourceDomain: 'Academics',
        sourceWorkReference: 'ref-1',
        sourceWorkLabel: 'Math',
        academicPeriodId: periodId,
        requiredCapabilityCode: 'CAP-MATH',
        requiredAssignmentRoleId: 'role-1',
        expectedWorkloadHours: 24,
        requestedStartDate: '2026-07-01',
        requestedEndDate: '2026-12-31',
      });

      expect(invSpy1).toHaveBeenCalledWith({ queryKey: demandsKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

      vi.spyOn(assignmentsApi, 'updateTeachingDemand').mockResolvedValueOnce(mockRes as never);
      const { result: updateMut, queryClient: qc2 } = withSetup(() => useUpdateDemandMutation());
      const invSpy2 = vi.spyOn(qc2, 'invalidateQueries');

      await updateMut.mutateAsync({ id: demandId, payload: { sourceWorkLabel: 'Updated Math' } });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: demandsKeys.all });

      vi.spyOn(assignmentsApi, 'updateTeachingDemandStatus').mockResolvedValueOnce(mockRes as never);
      const { result: statusMut, queryClient: qc3 } = withSetup(() => useUpdateDemandStatusMutation());
      const invSpy3 = vi.spyOn(qc3, 'invalidateQueries');

      await statusMut.mutateAsync({ id: demandId, payload: { status: 'accepted_for_staffing' } });
      expect(invSpy3).toHaveBeenCalledWith({ queryKey: demandsKeys.all });
    });

    it('useEvaluateCandidateMutation invalidates evaluations cache and notifies toast', async () => {
      const mockRes = { evaluationId: 'eval-1', isEligible: true, determinationStatus: 'eligible' };
      vi.spyOn(assignmentsApi, 'evaluateCandidate').mockResolvedValueOnce(mockRes as never);

      const { result, queryClient } = withSetup(() => useEvaluateCandidateMutation());
      const invSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ teachingDemandId: demandId, teacherProfileId: teacherId });

      expect(invSpy).toHaveBeenCalledWith({ queryKey: evaluationsKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('useProposeAssignmentMutation, useApproveAssignmentMutation, useAcknowledgeAssignmentMutation, and useUpdateAssignmentStatusMutation invalidate cache', async () => {
      vi.spyOn(assignmentsApi, 'proposeTeachingAssignment').mockResolvedValueOnce({ id: assignmentId, status: 'proposed' } as never);
      const { result: propMut, queryClient: qc1 } = withSetup(() => useProposeAssignmentMutation());
      const invSpy1 = vi.spyOn(qc1, 'invalidateQueries');

      await propMut.mutateAsync({
        teachingDemandId: demandId,
        teacherProfileId: teacherId,
        assignmentRoleId: 'role-1',
        assignmentScope: 'learning_unit',
        scopeReferenceId: 'ref-1',
        scopeLabel: 'Math',
        academicPeriodId: periodId,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        workloadCommitmentValue: 12,
      });

      expect(invSpy1).toHaveBeenCalledWith({ queryKey: assignmentsKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

      vi.spyOn(assignmentsApi, 'approveTeachingAssignment').mockResolvedValueOnce({ id: assignmentId, status: 'approved' } as never);
      const { result: apprMut, queryClient: qc2 } = withSetup(() => useApproveAssignmentMutation());
      const invSpy2 = vi.spyOn(qc2, 'invalidateQueries');

      await apprMut.mutateAsync({ id: assignmentId, payload: { approvalNotes: 'All good' } });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: assignmentsKeys.all });

      vi.spyOn(assignmentsApi, 'acknowledgeTeachingAssignment').mockResolvedValueOnce({ id: assignmentId, acknowledged: true } as never);
      const { result: ackMut, queryClient: qc3 } = withSetup(() => useAcknowledgeAssignmentMutation());
      const invSpy3 = vi.spyOn(qc3, 'invalidateQueries');
      await ackMut.mutateAsync({ id: assignmentId });
      expect(invSpy3).toHaveBeenCalledWith({ queryKey: assignmentsKeys.all });

      vi.spyOn(assignmentsApi, 'updateTeachingAssignmentStatus').mockResolvedValueOnce({ id: assignmentId, status: 'suspended' } as never);
      const { result: statMut, queryClient: qc4 } = withSetup(() => useUpdateAssignmentStatusMutation());
      const invSpy4 = vi.spyOn(qc4, 'invalidateQueries');
      await statMut.mutateAsync({ id: assignmentId, payload: { status: 'suspended', reason: 'Medical' } });
      expect(invSpy4).toHaveBeenCalledWith({ queryKey: assignmentsKeys.all });
    });

    it('useResolveConflictMutation invalidates conflicts cache', async () => {
      vi.spyOn(assignmentsApi, 'resolveCapacityConflict').mockResolvedValueOnce({ id: conflictId, status: 'resolved' } as never);
      const { result, queryClient } = withSetup(() => useResolveConflictMutation());
      const invSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ id: conflictId, payload: { status: 'resolved' } });
      expect(invSpy).toHaveBeenCalledWith({ queryKey: conflictsKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('useArrangeSubstituteMutation, useApproveSubstituteExceptionMutation, and useCancelSubstitutionMutation invalidate substitutions cache', async () => {
      vi.spyOn(assignmentsApi, 'arrangeEmergencySubstitution').mockResolvedValueOnce({ id: substitutionId, status: 'active' } as never);
      const { result: arrMut, queryClient: qc1 } = withSetup(() => useArrangeSubstituteMutation());
      const invSpy1 = vi.spyOn(qc1, 'invalidateQueries');

      await arrMut.mutateAsync({
        originalAssignmentId: assignmentId,
        substituteTeacherId: teacherId,
        startDate: '2026-09-01',
        endDate: '2026-09-15',
        coverageScopeType: 'full',
        reason: 'Emergency coverage',
      });

      expect(invSpy1).toHaveBeenCalledWith({ queryKey: substitutionsKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

      vi.spyOn(assignmentsApi, 'approveSubstitutionException').mockResolvedValueOnce({ id: substitutionId, status: 'approved' } as never);
      const { result: apprSubMut, queryClient: qc2 } = withSetup(() => useApproveSubstituteExceptionMutation());
      const invSpy2 = vi.spyOn(qc2, 'invalidateQueries');
      await apprSubMut.mutateAsync({ id: substitutionId, payload: { approvalNotes: 'Allowed' } });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: substitutionsKeys.all });

      vi.spyOn(assignmentsApi, 'cancelSubstitution').mockResolvedValueOnce({ id: substitutionId, status: 'cancelled' } as never);
      const { result: cancelSubMut, queryClient: qc3 } = withSetup(() => useCancelSubstitutionMutation());
      const invSpy3 = vi.spyOn(qc3, 'invalidateQueries');
      await cancelSubMut.mutateAsync({ id: substitutionId, payload: { reason: 'Returned' } });
      expect(invSpy3).toHaveBeenCalledWith({ queryKey: substitutionsKeys.all });
    });

    it('useProposeReassignmentMutation, useApproveReassignmentMutation, and useConfirmReassignmentHandoverMutation invalidate reassignments cache', async () => {
      vi.spyOn(assignmentsApi, 'proposePermanentReassignment').mockResolvedValueOnce({
        reassignment: { id: reassignmentId, reassignmentNumber: 'REA-2026-0001', status: 'proposed' },
      } as never);
      const { result: propReaMut, queryClient: qc1 } = withSetup(() => useProposeReassignmentMutation());
      const invSpy1 = vi.spyOn(qc1, 'invalidateQueries');

      await propReaMut.mutateAsync({
        originalAssignmentId: assignmentId,
        newTeacherId: teacherId,
        effectiveDate: '2026-10-01',
        reason: 'Transfer',
      });
      expect(invSpy1).toHaveBeenCalledWith({ queryKey: reassignmentsKeys.all });

      vi.spyOn(assignmentsApi, 'approvePermanentReassignment').mockResolvedValueOnce({ id: reassignmentId, status: 'approved' } as never);
      const { result: apprReaMut, queryClient: qc2 } = withSetup(() => useApproveReassignmentMutation());
      const invSpy2 = vi.spyOn(qc2, 'invalidateQueries');

      await apprReaMut.mutateAsync({ id: reassignmentId, payload: { approvalNotes: 'Approved' } });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: reassignmentsKeys.all });

      vi.spyOn(assignmentsApi, 'confirmReassignmentHandover').mockResolvedValueOnce({ id: reassignmentId, handoverStatus: 'confirmed' } as never);
      const { result: handoverMut, queryClient: qc3 } = withSetup(() => useConfirmReassignmentHandoverMutation());
      const invSpy3 = vi.spyOn(qc3, 'invalidateQueries');

      await handoverMut.mutateAsync({ id: reassignmentId, payload: { handoverNotes: 'Done' } });
      expect(invSpy3).toHaveBeenCalledWith({ queryKey: reassignmentsKeys.all });
    });

    it('useConcludeAssignmentMutation invalidates completions and assignments cache', async () => {
      vi.spyOn(assignmentsApi, 'concludeTeachingAssignment').mockResolvedValueOnce({ id: 'comp-1', assignmentId } as never);
      const { result, queryClient } = withSetup(() => useConcludeAssignmentMutation());
      const invSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: assignmentId,
        payload: {
          completionDate: '2026-12-31',
          academicConfirmationReceived: true,
          handoverConfirmed: true,
        },
      });

      expect(invSpy).toHaveBeenCalledWith({ queryKey: completionsKeys.all });
      expect(invSpy).toHaveBeenCalledWith({ queryKey: assignmentsKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('handles mutation error by displaying error toast', async () => {
      vi.spyOn(assignmentsApi, 'createTeachingDemand').mockRejectedValueOnce(new Error('Validation failed'));

      const { result } = withSetup(() => useCreateDemandMutation());
      await expect(
        result.mutateAsync({
          sourceDomain: 'Academics',
          sourceWorkReference: 'ref-1',
          sourceWorkLabel: 'Math',
          academicPeriodId: periodId,
          requiredCapabilityCode: 'CAP-MATH',
          requiredAssignmentRoleId: 'role-1',
          expectedWorkloadHours: 24,
          requestedStartDate: '2026-07-01',
          requestedEndDate: '2026-12-31',
        }),
      ).rejects.toThrow('Validation failed');

      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    });
  });

  // ==========================================================================
  // 2. Workload Module Composables
  // ==========================================================================
  describe('useWorkload Query and Mutation Hooks', () => {
    it('useWorkloadPolicies and useWorkloadExceptions query policies and exceptions', async () => {
      const mockPolicies = [{ id: 'pol-1', code: 'WPOL-2026' }];
      vi.spyOn(workloadApi, 'getWorkloadPolicies').mockResolvedValueOnce(mockPolicies as never);

      const params = ref({ status: 'active' });
      const { result: polResult } = withSetup(() => useWorkloadPolicies(params));

      await vi.waitFor(() => {
        expect(polResult.isSuccess.value).toBe(true);
      });
      expect(polResult.data.value).toEqual(mockPolicies);

      const mockExceptions = [{ id: exceptionId, status: 'requested' }];
      vi.spyOn(workloadApi, 'getWorkloadExceptions').mockResolvedValueOnce(mockExceptions as never);
      const excParams = ref({ academicPeriodId: periodId });
      const { result: excResult } = withSetup(() => useWorkloadExceptions(excParams));

      await vi.waitFor(() => {
        expect(excResult.isSuccess.value).toBe(true);
      });
      expect(excResult.data.value).toEqual(mockExceptions);
    });

    it('useWorkloadCommitments queries capacity snapshot commitments', async () => {
      const mockCommitments = [{ id: 'comm-1', status: 'normal' }];
      vi.spyOn(workloadApi, 'getWorkloadCommitments').mockResolvedValueOnce(mockCommitments as never);

      const params = ref({ academicPeriodId: periodId });
      const { result } = withSetup(() => useWorkloadCommitments(params));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockCommitments);
    });

    it('useRecalculateWorkloadMutation invalidates workload cache and alerts toast', async () => {
      vi.spyOn(workloadApi, 'recalculateWorkload').mockResolvedValueOnce({
        academicPeriodId: periodId,
        recalculatedFacultyCount: 42,
      } as never);

      const { result, queryClient } = withSetup(() => useRecalculateWorkloadMutation());
      const invSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ academicPeriodId: periodId });

      expect(invSpy).toHaveBeenCalledWith({ queryKey: workloadKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('useRequestExceptionMutation and useApproveExceptionMutation invalidate exceptions cache', async () => {
      vi.spyOn(workloadApi, 'requestWorkloadException').mockResolvedValueOnce({ id: exceptionId, status: 'requested' } as never);
      const { result: reqMut, queryClient: qc1 } = withSetup(() => useRequestExceptionMutation());
      const invSpy1 = vi.spyOn(qc1, 'invalidateQueries');

      await reqMut.mutateAsync({
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        requestedOverloadHours: 4,
        coordinatorJustification: 'Cambridge bilingual syllabus',
      });

      expect(invSpy1).toHaveBeenCalledWith({ queryKey: workloadKeys.exceptions() });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

      vi.spyOn(workloadApi, 'approveWorkloadException').mockResolvedValueOnce({ id: exceptionId, status: 'approved' } as never);
      const { result: apprMut, queryClient: qc2 } = withSetup(() => useApproveExceptionMutation());
      const invSpy2 = vi.spyOn(qc2, 'invalidateQueries');

      await apprMut.mutateAsync({ id: exceptionId, payload: { decisionRemarks: 'Approved' } });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: workloadKeys.exceptions() });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: workloadKeys.commitments() });
    });
  });

  // ==========================================================================
  // 3. Availability Module Composables
  // ==========================================================================
  describe('useAvailability Query and Mutation Hooks', () => {
    it('useTeacherAvailability queries weekly schedule declaration', async () => {
      const mockAvail = { id: 'avail-1', teacherProfileId: teacherId, maxAvailableHours: 24 };
      vi.spyOn(availabilityApi, 'getTeacherAvailability').mockResolvedValueOnce(mockAvail as never);

      const tRef = ref(teacherId);
      const pRef = ref(periodId);
      const { result } = withSetup(() => useTeacherAvailability(tRef, pRef));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockAvail);
    });

    it('useUnavailabilityWindows queries leave windows for a teacher', async () => {
      const mockWindows = [{ id: windowId, leaveType: 'medical_leave' }];
      vi.spyOn(availabilityApi, 'getTeacherUnavailabilityWindows').mockResolvedValueOnce(mockWindows as never);

      const tRef = ref(teacherId);
      const { result } = withSetup(() => useUnavailabilityWindows(tRef));

      await vi.waitFor(() => {
        expect(result.isSuccess.value).toBe(true);
      });
      expect(result.data.value).toEqual(mockWindows);
    });

    it('useDeclareAvailabilityMutation invalidates availability cache and notifies toast', async () => {
      const mockSaved = { id: 'avail-1', maxAvailableHours: 20 };
      vi.spyOn(availabilityApi, 'declareTeacherAvailability').mockResolvedValueOnce(mockSaved as never);

      const { result, queryClient } = withSetup(() => useDeclareAvailabilityMutation());
      const invSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        teacherId,
        payload: {
          academicPeriodId: periodId,
          maxAvailableHours: 20,
          effectiveStartDate: '2026-07-01',
          effectiveEndDate: '2026-12-31',
        },
      });

      expect(invSpy).toHaveBeenCalledWith({ queryKey: availabilityKeys.teacher(teacherId) });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });

    it('useCreateUnavailabilityMutation and useCancelUnavailabilityMutation invalidate leave windows cache', async () => {
      const mockCreatedWindow = {
        id: windowId,
        teacherProfileId: teacherId,
        unavailabilityType: 'medical_leave',
        startDate: '2026-09-01',
        endDate: '2026-09-05',
        affectedActiveAssignmentCount: 0,
      };
      vi.spyOn(availabilityApi, 'createUnavailabilityWindow').mockResolvedValueOnce(mockCreatedWindow as never);
      const { result: createMut, queryClient: qc1 } = withSetup(() => useCreateUnavailabilityMutation());
      const invSpy1 = vi.spyOn(qc1, 'invalidateQueries');

      await createMut.mutateAsync({
        teacherId,
        payload: {
          startDate: '2026-09-01',
          endDate: '2026-09-05',
          unavailabilityType: 'medical_leave',
          reason: 'Medical recovery',
        },
      });

      expect(invSpy1).toHaveBeenCalledWith({ queryKey: availabilityKeys.teacher(teacherId) });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

      const mockCancelResponse = {
        id: windowId,
        status: 'cancelled',
        cancelledAt: '2026-09-01T00:00:00Z',
      };
      vi.spyOn(availabilityApi, 'cancelUnavailabilityWindow').mockResolvedValueOnce(mockCancelResponse as never);
      const { result: cancelMut, queryClient: qc2 } = withSetup(() => useCancelUnavailabilityMutation());
      const invSpy2 = vi.spyOn(qc2, 'invalidateQueries');

      await cancelMut.mutateAsync({ windowId, teacherId });
      expect(invSpy2).toHaveBeenCalledWith({ queryKey: availabilityKeys.teacher(teacherId) });
      expect(mockToast.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    });
  });
});
