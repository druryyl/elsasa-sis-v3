// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { interventionsApi } from '../../api/interventionsApi';
import {
  interventionKeys,
  useAcademicInterventions,
  useActiveInterventionsQuery,
  useAtRiskLearnersQuery,
  useInitiateInterventionMutation,
  useInterventionDetailQuery,
  useUpdateInterventionStatusMutation,
} from '../useAcademicInterventions';
import type { AcademicIntervention, AtRiskLearner } from '../../types';

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

describe('useAcademicInterventions Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('interventionKeys', () => {
    it('builds standard query keys matching completion criteria', () => {
      expect(interventionKeys.all).toEqual(['academics', 'interventions']);
      expect(interventionKeys.atRisk('period-2026-s1', 'Grade 10')).toEqual([
        'academics',
        'interventions',
        'at-risk',
        'period-2026-s1',
        'Grade 10',
      ]);
      expect(interventionKeys.active('period-2026-s1')).toEqual([
        'academics',
        'interventions',
        'active',
        'period-2026-s1',
      ]);
      expect(interventionKeys.detail('int-101')).toEqual([
        'academics',
        'interventions',
        'detail',
        'int-101',
      ]);
    });
  });

  describe('useAtRiskLearnersQuery', () => {
    it('disables query when periodId is empty or undefined', () => {
      const periodId = ref<string | null>(null);
      const { result } = withSetup(() => useAtRiskLearnersQuery(periodId));
      expect(result.isEnabled.value).toBe(false);
    });

    it('enables query and fetches at-risk learners when periodId is provided', async () => {
      const mockLearners: AtRiskLearner[] = [
        {
          studentId: 'student-1',
          studentName: 'Alice Smith',
          gradeLevel: 'Grade 10',
          academicPeriodId: 'period-1',
          riskLevel: 'high',
          triggerType: 'failing_grades',
          triggerSummary: 'Score 55 in Mathematics',
          failingUnitsCount: 2,
          attendanceRate: 72.5,
          activeInterventionId: null,
          interventionStatus: null,
        },
      ];

      vi.spyOn(interventionsApi, 'getAtRiskLearners').mockResolvedValue(mockLearners);

      const periodId = ref('period-1');
      const gradeLevel = ref('Grade 10');
      const { result } = withSetup(() => useAtRiskLearnersQuery(periodId, gradeLevel));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockLearners);
      expect(interventionsApi.getAtRiskLearners).toHaveBeenCalledWith({
        academicPeriodId: 'period-1',
        gradeLevel: 'Grade 10',
      });
    });
  });

  describe('useActiveInterventionsQuery', () => {
    it('fetches active interventions for an academic period', async () => {
      const mockInterventions: AcademicIntervention[] = [
        {
          id: 'int-1',
          studentId: 'student-1',
          studentName: 'Alice Smith',
          academicPeriodId: 'period-1',
          learningUnitId: 'unit-1',
          learningUnitTitle: 'Mathematics 10',
          interventionType: 'tutoring',
          triggerType: 'failing_grades',
          triggerDescription: 'Score below passing',
          actionPlan: 'Weekly remedial tutoring sessions',
          assignedStaffId: 'staff-1',
          assignedStaffName: 'Mr. Teacher',
          targetMilestoneDate: '2026-05-01',
          status: 'in_progress',
          outcomeNarrative: null,
          initiatedAt: '2026-02-01T08:00:00Z',
          completedAt: null,
        },
      ];

      vi.spyOn(interventionsApi, 'getInterventions').mockResolvedValue(mockInterventions);

      const periodId = ref('period-1');
      const { result } = withSetup(() => useActiveInterventionsQuery(periodId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockInterventions);
      expect(interventionsApi.getInterventions).toHaveBeenCalledWith({
        academicPeriodId: 'period-1',
      });
    });
  });

  describe('useInterventionDetailQuery', () => {
    it('disables query when id is null', () => {
      const id = ref<string | null>(null);
      const { result } = withSetup(() => useInterventionDetailQuery(id));
      expect(result.isEnabled.value).toBe(false);
    });

    it('fetches single intervention details when id is provided', async () => {
      const mockIntervention: AcademicIntervention = {
        id: 'int-101',
        studentId: 'student-1',
        studentName: 'Alice Smith',
        academicPeriodId: 'period-1',
        learningUnitId: 'unit-1',
        learningUnitTitle: 'Mathematics 10',
        interventionType: 'academic_probation',
        triggerType: 'academic_probation',
        triggerDescription: 'Placed on probation',
        actionPlan: 'Bi-weekly checks',
        assignedStaffId: 'staff-1',
        assignedStaffName: 'Mr. Teacher',
        targetMilestoneDate: '2026-06-01',
        status: 'planned',
        outcomeNarrative: null,
        initiatedAt: '2026-02-01T08:00:00Z',
        completedAt: null,
      };

      vi.spyOn(interventionsApi, 'getInterventionById').mockResolvedValue(mockIntervention);

      const id = ref('int-101');
      const { result } = withSetup(() => useInterventionDetailQuery(id));

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockIntervention);
      expect(interventionsApi.getInterventionById).toHaveBeenCalledWith('int-101');
    });
  });

  describe('useInitiateInterventionMutation', () => {
    it('executes mutation, invalidates cache, and notifies success toast', async () => {
      const mockCreated: AcademicIntervention = {
        id: 'int-new',
        studentId: 'student-2',
        studentName: 'Bob Jones',
        academicPeriodId: 'period-1',
        learningUnitId: 'unit-2',
        learningUnitTitle: 'Science 10',
        interventionType: 'counseling',
        triggerType: 'chronic_absenteeism',
        triggerDescription: 'Attendance dropped under 75%',
        actionPlan: 'Counselor consultation and study contract',
        assignedStaffId: 'staff-2',
        assignedStaffName: 'Ms. Counselor',
        targetMilestoneDate: '2026-04-15',
        status: 'planned',
        outcomeNarrative: null,
        initiatedAt: '2026-03-01T09:00:00Z',
        completedAt: null,
      };

      vi.spyOn(interventionsApi, 'initiateIntervention').mockResolvedValue(mockCreated);

      const { result, queryClient } = withSetup(() => useInitiateInterventionMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        studentId: 'student-2',
        academicPeriodId: 'period-1',
        learningUnitId: 'unit-2',
        interventionType: 'counseling',
        triggerType: 'chronic_absenteeism',
        triggerDescription: 'Attendance dropped under 75%',
        actionPlan: 'Counselor consultation and study contract',
        assignedStaffId: 'staff-2',
        targetMilestoneDate: '2026-04-15',
      });

      expect(interventionsApi.initiateIntervention).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: interventionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Intervention Initiated',
        }),
      );
    });
  });

  describe('useUpdateInterventionStatusMutation', () => {
    it('updates status, invalidates all and detail cache, and notifies toast', async () => {
      const mockUpdated: AcademicIntervention = {
        id: 'int-1',
        studentId: 'student-1',
        studentName: 'Alice Smith',
        academicPeriodId: 'period-1',
        learningUnitId: 'unit-1',
        learningUnitTitle: 'Mathematics 10',
        interventionType: 'tutoring',
        triggerType: 'failing_grades',
        triggerDescription: 'Score below passing',
        actionPlan: 'Weekly remedial tutoring sessions',
        assignedStaffId: 'staff-1',
        assignedStaffName: 'Mr. Teacher',
        targetMilestoneDate: '2026-05-01',
        status: 'completed',
        outcomeNarrative: 'Successfully raised score above passing KKM.',
        initiatedAt: '2026-02-01T08:00:00Z',
        completedAt: '2026-05-01T10:00:00Z',
      };

      vi.spyOn(interventionsApi, 'updateInterventionStatus').mockResolvedValue(mockUpdated);

      const { result, queryClient } = withSetup(() => useUpdateInterventionStatusMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'int-1',
        request: {
          status: 'completed',
          outcomeNarrative: 'Successfully raised score above passing KKM.',
        },
      });

      expect(interventionsApi.updateInterventionStatus).toHaveBeenCalledWith('int-1', {
        status: 'completed',
        outcomeNarrative: 'Successfully raised score above passing KKM.',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: interventionKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: interventionKeys.detail('int-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Intervention Status Updated',
        }),
      );
    });
  });

  describe('useAcademicInterventions unified composable', () => {
    it('exposes queries and mutations', () => {
      const periodId = ref('period-1');
      const gradeLevel = ref('Grade 10');
      const { result } = withSetup(() => useAcademicInterventions(periodId, gradeLevel));

      expect(result.atRiskQuery).toBeDefined();
      expect(result.activeInterventionsQuery).toBeDefined();
      expect(result.initiateInterventionMutation).toBeDefined();
      expect(result.updateStatusMutation).toBeDefined();
    });
  });
});
