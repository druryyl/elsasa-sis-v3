// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { learningPlansApi } from '../../api/learningPlansApi';
import {
  learningPlanKeys,
  useLearningPlans,
  useLearningPlanDetail,
  useActiveLearningPlan,
  useCreatePlanMutation,
  useUpdatePlanMutation,
  useAddUnitMutation,
  useRemoveUnitMutation,
  useAddAdjustmentMutation,
  useRemoveAdjustmentMutation,
  useSubmitPlanMutation,
  useApprovePlanMutation,
  useRevisePlanMutation,
  useSyncParticipationsMutation,
  useArchivePlanMutation,
  useLearningPlanFeature,
} from '../useLearningPlans';
import type {
  ActiveStudentLearningPlan,
  PaginatedResponse,
  SlpApproval,
  StudentLearningPlan,
  StudentLearningPlanListItem,
  SyncParticipationsResponse,
} from '../../types';

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

describe('useLearningPlans Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('learningPlanKeys', () => {
    it('produces exact cache keys specified in criteria', () => {
      const filters = { academicPeriodId: 'period-2026-s1', status: 'active' as const };
      expect(learningPlanKeys.all).toEqual(['learning-plans']);
      expect(learningPlanKeys.lists()).toEqual(['learning-plans']);
      expect(learningPlanKeys.list(filters)).toEqual(['learning-plans', filters]);
      expect(learningPlanKeys.details()).toEqual(['learning-plan']);
      expect(learningPlanKeys.detail('plan-101')).toEqual(['learning-plan', 'plan-101']);
      expect(learningPlanKeys.activePlans()).toEqual(['active-learning-plan']);
      expect(learningPlanKeys.activePlan('student-1', 'period-1')).toEqual([
        'active-learning-plan',
        'student-1',
        'period-1',
      ]);
    });
  });

  describe('useLearningPlans', () => {
    it('queries paginated learning plans roster with filters', async () => {
      const mockResponse: PaginatedResponse<StudentLearningPlanListItem> = {
        items: [
          {
            id: 'plan-1',
            studentId: 'student-1',
            studentName: 'Budi Santoso',
            studentNis: 'NIS-1001',
            academicPeriodId: 'period-1',
            academicPeriodName: '2026/2027 Semester 1',
            status: 'active',
            version: 2,
            unitsCount: 3,
            adjustmentsCount: 2,
            hasActiveAdjustments: true,
            createdAt: '2026-11-01T08:00:00Z',
            updatedAt: '2026-11-05T09:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      };

      vi.spyOn(learningPlansApi, 'getLearningPlans').mockResolvedValue(mockResponse);

      const filters = ref({ academicPeriodId: 'period-1', status: 'active' as const });
      const { result } = withSetup(() => useLearningPlans(filters));

      expect(result.isLoading.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(learningPlansApi.getLearningPlans).toHaveBeenCalledWith(filters.value);
      expect(result.data.value).toEqual(mockResponse);
    });
  });

  describe('useLearningPlanDetail', () => {
    it('is disabled when planId is null or empty', () => {
      const planId = ref<string | null>(null);
      const { result } = withSetup(() => useLearningPlanDetail(planId));
      expect(result.isEnabled.value).toBe(false);
    });

    it('fetches detailed plan when planId is provided', async () => {
      const mockPlan: StudentLearningPlan = {
        id: 'plan-101',
        studentId: 'student-1',
        studentName: 'Budi Santoso',
        studentNis: 'NIS-1001',
        academicPeriodId: 'period-1',
        academicPeriodName: '2026/2027 Semester 1',
        status: 'active',
        version: 2,
        notes: 'Remedial math and adaptive physical education',
        createdAt: '2026-11-01T08:00:00Z',
        updatedAt: '2026-11-05T09:00:00Z',
        learningUnits: [],
        adjustments: [],
        approvals: [],
      };

      vi.spyOn(learningPlansApi, 'getLearningPlanById').mockResolvedValue(mockPlan);

      const planId = ref<string | null>('plan-101');
      const { result } = withSetup(() => useLearningPlanDetail(planId));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(learningPlansApi.getLearningPlanById).toHaveBeenCalledWith('plan-101');
      expect(result.data.value).toEqual(mockPlan);
    });
  });

  describe('useActiveLearningPlan', () => {
    it('is disabled when studentId or periodId is missing', () => {
      const studentId = ref<string | null>(null);
      const periodId = ref<string | null>('period-1');
      const { result } = withSetup(() => useActiveLearningPlan(studentId, periodId));
      expect(result.isEnabled.value).toBe(false);
    });

    it('fetches active plan when both studentId and periodId are present', async () => {
      const mockActivePlan: ActiveStudentLearningPlan = {
        id: 'plan-101',
        studentId: 'student-1',
        academicPeriodId: 'period-1',
        status: 'active',
        version: 2,
        notes: null,
        learningUnits: [],
        adjustments: [],
      };

      vi.spyOn(learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(mockActivePlan);

      const studentId = ref('student-1');
      const periodId = ref('period-1');
      const { result } = withSetup(() => useActiveLearningPlan(studentId, periodId));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(learningPlansApi.getActiveLearningPlan).toHaveBeenCalledWith('student-1', 'period-1');
      expect(result.data.value).toEqual(mockActivePlan);
    });
  });

  describe('Mutation Composables', () => {
    it('useCreatePlanMutation invalidates queries and displays success toast', async () => {
      vi.spyOn(learningPlansApi, 'createPlan').mockResolvedValue({ id: 'new-plan-id' });
      const onSuccessSpy = vi.fn();

      const { result, queryClient } = withSetup(() =>
        useCreatePlanMutation({ onSuccess: onSuccessSpy }),
      );

      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        studentId: 'student-1',
        academicPeriodId: 'period-1',
        notes: 'Initial plan',
      });

      expect(learningPlansApi.createPlan).toHaveBeenCalledWith({
        studentId: 'student-1',
        academicPeriodId: 'period-1',
        notes: 'Initial plan',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Created',
        }),
      );
      expect(onSuccessSpy).toHaveBeenCalledWith(
        { id: 'new-plan-id' },
        expect.objectContaining({ studentId: 'student-1' }),
      );
    });

    it('useCreatePlanMutation handles errors and displays error toast', async () => {
      const error = new Error('Creation rejected');
      vi.spyOn(learningPlansApi, 'createPlan').mockRejectedValue(error);
      const onErrorSpy = vi.fn();

      const { result } = withSetup(() =>
        useCreatePlanMutation({ onError: onErrorSpy }),
      );

      await expect(
        result.mutateAsync({
          studentId: 'student-1',
          academicPeriodId: 'period-1',
        }),
      ).rejects.toThrow('Creation rejected');

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Failed to Create Learning Plan',
          detail: 'Creation rejected',
        }),
      );
      expect(onErrorSpy).toHaveBeenCalledWith(error, expect.anything());
    });

    it('useUpdatePlanMutation updates draft plan and invalidates detail cache', async () => {
      vi.spyOn(learningPlansApi, 'updatePlan').mockResolvedValue();

      const { result, queryClient } = withSetup(() => useUpdatePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'plan-101',
        request: { notes: 'Updated notes' },
      });

      expect(learningPlansApi.updatePlan).toHaveBeenCalledWith('plan-101', {
        notes: 'Updated notes',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Updated',
        }),
      );
    });

    it('useAddUnitMutation adds unit and invalidates plan detail', async () => {
      vi.spyOn(learningPlansApi, 'addUnit').mockResolvedValue({ id: 'unit-link-1' });

      const { result, queryClient } = withSetup(() => useAddUnitMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        planId: 'plan-101',
        request: {
          learningUnitId: 'unit-math-3',
          sequenceOrder: 1,
          isRequired: true,
          adaptationNotes: 'Grade 3 Remedial',
        },
      });

      expect(learningPlansApi.addUnit).toHaveBeenCalledWith('plan-101', {
        learningUnitId: 'unit-math-3',
        sequenceOrder: 1,
        isRequired: true,
        adaptationNotes: 'Grade 3 Remedial',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Added',
        }),
      );
    });

    it('useRemoveUnitMutation removes unit and invalidates plan detail', async () => {
      vi.spyOn(learningPlansApi, 'removeUnit').mockResolvedValue();

      const { result, queryClient } = withSetup(() => useRemoveUnitMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        planId: 'plan-101',
        unitId: 'unit-link-1',
      });

      expect(learningPlansApi.removeUnit).toHaveBeenCalledWith('plan-101', 'unit-link-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Removed',
        }),
      );
    });

    it('useAddAdjustmentMutation adds academic adjustment and invalidates detail', async () => {
      vi.spyOn(learningPlansApi, 'addAdjustment').mockResolvedValue({ id: 'adj-1' });

      const { result, queryClient } = withSetup(() => useAddAdjustmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        planId: 'plan-101',
        request: {
          adjustmentType: 'accommodation',
          description: '50% extra exam time',
          rationale: 'Diagnosed dyscalculia requiring paced reading and working.',
          effectiveDate: '2026-07-15',
        },
      });

      expect(learningPlansApi.addAdjustment).toHaveBeenCalledWith('plan-101', {
        adjustmentType: 'accommodation',
        description: '50% extra exam time',
        rationale: 'Diagnosed dyscalculia requiring paced reading and working.',
        effectiveDate: '2026-07-15',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Adjustment Added',
        }),
      );
    });

    it('useRemoveAdjustmentMutation removes adjustment and invalidates detail', async () => {
      vi.spyOn(learningPlansApi, 'removeAdjustment').mockResolvedValue();

      const { result, queryClient } = withSetup(() => useRemoveAdjustmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        planId: 'plan-101',
        adjustmentId: 'adj-1',
      });

      expect(learningPlansApi.removeAdjustment).toHaveBeenCalledWith('plan-101', 'adj-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Adjustment Removed',
        }),
      );
    });

    it('useSubmitPlanMutation submits plan for review and invalidates caches', async () => {
      vi.spyOn(learningPlansApi, 'submitPlan').mockResolvedValue();

      const { result, queryClient } = withSetup(() => useSubmitPlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('plan-101');

      expect(learningPlansApi.submitPlan).toHaveBeenCalledWith('plan-101');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Submitted',
        }),
      );
    });

    it('useApprovePlanMutation approves plan, activates it and invalidates caches', async () => {
      const mockApproval: SlpApproval = {
        id: 'appr-1',
        studentLearningPlanId: 'plan-101',
        planVersion: 2,
        approvedBy: 'authority-1',
        approvedByName: 'Dr. Academic Dean',
        approvedDate: '2026-11-06T10:00:00Z',
        approvalType: 'revision_approval',
        notes: 'Approved after remedial curriculum review',
      };

      vi.spyOn(learningPlansApi, 'approvePlan').mockResolvedValue(mockApproval);

      const { result, queryClient } = withSetup(() => useApprovePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'plan-101',
        request: {
          approvalType: 'revision_approval',
          notes: 'Approved after remedial curriculum review',
        },
      });

      expect(learningPlansApi.approvePlan).toHaveBeenCalledWith('plan-101', {
        approvalType: 'revision_approval',
        notes: 'Approved after remedial curriculum review',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Approved',
        }),
      );
    });

    it('useRevisePlanMutation clones active plan into draft revision', async () => {
      vi.spyOn(learningPlansApi, 'revisePlan').mockResolvedValue({ id: 'plan-v2-draft-id' });

      const { result, queryClient } = withSetup(() => useRevisePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'plan-101',
        request: { notes: 'Mid-term adjustment revision' },
      });

      expect(learningPlansApi.revisePlan).toHaveBeenCalledWith('plan-101', {
        notes: 'Mid-term adjustment revision',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Plan Revision Initiated',
        }),
      );
    });

    it('useSyncParticipationsMutation syncs participations and invalidates academics.participations', async () => {
      const mockSyncResp: SyncParticipationsResponse = {
        studentLearningPlanId: 'plan-101',
        syncedCount: 3,
        participations: [],
      };

      vi.spyOn(learningPlansApi, 'syncParticipations').mockResolvedValue(mockSyncResp);

      const { result, queryClient } = withSetup(() => useSyncParticipationsMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('plan-101');

      expect(learningPlansApi.syncParticipations).toHaveBeenCalledWith('plan-101');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'participations'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Participations Synchronized',
          detail: 'Synchronized 3 student academic participations.',
        }),
      );
    });

    it('useArchivePlanMutation archives plan and invalidates caches', async () => {
      vi.spyOn(learningPlansApi, 'archivePlan').mockResolvedValue();

      const { result, queryClient } = withSetup(() => useArchivePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('plan-101');

      expect(learningPlansApi.archivePlan).toHaveBeenCalledWith('plan-101', undefined);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', 'plan-101'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Archived',
        }),
      );
    });

    it('suppresses toasts when showToast is false', async () => {
      vi.spyOn(learningPlansApi, 'submitPlan').mockResolvedValue();

      const { result } = withSetup(() =>
        useSubmitPlanMutation({ showToast: false }),
      );

      await result.mutateAsync('plan-101');
      expect(mockToast.add).not.toHaveBeenCalled();
    });
  });

  describe('useLearningPlanFeature unified composable', () => {
    it('initializes all query and mutation handles together', () => {
      const { result } = withSetup(() =>
        useLearningPlanFeature('plan-101', 'student-1', 'period-1'),
      );

      expect(result.detailQuery).toBeDefined();
      expect(result.activePlanQuery).toBeDefined();
      expect(result.createPlanMutation).toBeDefined();
      expect(result.updatePlanMutation).toBeDefined();
      expect(result.addUnitMutation).toBeDefined();
      expect(result.removeUnitMutation).toBeDefined();
      expect(result.addAdjustmentMutation).toBeDefined();
      expect(result.removeAdjustmentMutation).toBeDefined();
      expect(result.submitPlanMutation).toBeDefined();
      expect(result.approvePlanMutation).toBeDefined();
      expect(result.revisePlanMutation).toBeDefined();
      expect(result.syncParticipationsMutation).toBeDefined();
      expect(result.archivePlanMutation).toBeDefined();
    });
  });
});
