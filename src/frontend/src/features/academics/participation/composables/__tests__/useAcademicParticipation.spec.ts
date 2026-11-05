// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as participationApi from '../../api/participation.api';
import {
  academicParticipationKeys,
  useAcademicParticipation,
  useBatchRegisterCohortMutation,
  useConcludeParticipationMutation,
  useRecognizePriorCreditMutation,
  useRegisterParticipationMutation,
  useStudentParticipationsQuery,
  useUnitParticipationsQuery,
  useUpdateParticipationStatusMutation,
} from '../useAcademicParticipation';

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

describe('useAcademicParticipation Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('academicParticipationKeys', () => {
    it('builds standard query keys', () => {
      expect(academicParticipationKeys.all).toEqual(['academics', 'participations']);
      expect(
        academicParticipationKeys.unitParticipations('unit-1', 'period-1', 'A', 'in_progress'),
      ).toEqual([
        'academics',
        'participations',
        'unit',
        'unit-1',
        { periodId: 'period-1', sectionLabel: 'A', status: 'in_progress' },
      ]);
      expect(
        academicParticipationKeys.studentParticipations('student-1', 'period-1'),
      ).toEqual([
        'academics',
        'participations',
        'student',
        'student-1',
        { academicPeriodId: 'period-1' },
      ]);
    });
  });

  describe('useUnitParticipationsQuery', () => {
    it('disables query when unitId is empty or null', () => {
      const unitId = ref<string | null>(null);
      const { result } = withSetup(() => useUnitParticipationsQuery(unitId));
      expect(result.isEnabled.value).toBe(false);
    });

    it('enables query when unitId is provided and fetches unit participations', async () => {
      const mockData = [
        {
          id: 'part-1',
          studentId: 'stu-1',
          learningUnitId: 'unit-1',
          academicPeriodId: 'period-1',
          participationType: 'standard' as const,
          participationSource: 'direct_enrollment' as const,
          attemptNumber: 1,
          status: 'in_progress' as const,
          startDate: '2026-09-01',
          sectionLabel: 'A',
          studentName: 'Alice',
          studentIdentifier: 'STU-001',
          learningUnitName: 'Biology',
        },
      ];
      vi.spyOn(participationApi, 'getParticipationsByUnit').mockResolvedValue(mockData);

      const unitId = ref('unit-1');
      const { result } = withSetup(() =>
        useUnitParticipationsQuery(unitId, 'period-1', 'A'),
      );

      expect(result.isEnabled.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockData);
      expect(participationApi.getParticipationsByUnit).toHaveBeenCalledWith('unit-1', {
        academicPeriodId: 'period-1',
        sectionLabel: 'A',
      });
    });
  });

  describe('useStudentParticipationsQuery', () => {
    it('fetches student participations', async () => {
      const mockData = [
        {
          id: 'part-1',
          learningUnitId: 'unit-1',
          academicPeriodId: 'period-1',
          learningUnitName: 'Biology',
          unitType: 'subject',
          participationType: 'standard' as const,
          status: 'in_progress' as const,
          attemptNumber: 1,
          startDate: '2026-09-01',
          sectionLabel: 'A',
        },
      ];
      vi.spyOn(participationApi, 'getStudentParticipations').mockResolvedValue(mockData);

      const studentId = ref('stu-1');
      const { result } = withSetup(() =>
        useStudentParticipationsQuery(studentId, 'period-1'),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockData);
      expect(participationApi.getStudentParticipations).toHaveBeenCalledWith('stu-1', {
        academicPeriodId: 'period-1',
      });
    });
  });

  describe('mutations and cache invalidation', () => {
    it('useBatchRegisterCohortMutation registers cohort, invalidates cache and triggers toast', async () => {
      const mockResponse = {
        participationsCreated: 25,
        academicPeriodId: 'period-1',
        learningUnitIds: ['unit-1'],
        sectionLabel: 'Grade 10A',
        enrolledStudentIds: ['stu-1', 'stu-2'],
      };
      vi.spyOn(participationApi, 'batchRegisterCohortParticipation').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useBatchRegisterCohortMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        academicPeriodId: 'period-1',
        learningUnitIds: ['unit-1'],
        sectionLabel: 'Grade 10A',
        startDate: '2026-09-01',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: academicParticipationKeys.all,
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Cohort Registered',
        }),
      );
    });

    it('useRegisterParticipationMutation registers individual student participation', async () => {
      const mockResponse = {
        id: 'part-new',
        studentId: 'stu-1',
        learningUnitId: 'unit-1',
        attemptNumber: 2,
        status: 'in_progress' as const,
      };
      vi.spyOn(participationApi, 'registerParticipation').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useRegisterParticipationMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        studentId: 'stu-1',
        learningUnitId: 'unit-1',
        academicPeriodId: 'period-1',
        participationType: 'repeat',
        startDate: '2026-09-01',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: academicParticipationKeys.all,
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Participation Registered',
        }),
      );
    });

    it('useRecognizePriorCreditMutation recognizes prior learning credit', async () => {
      const mockResponse = {
        id: 'part-credit',
        studentId: 'stu-1',
        learningUnitId: 'unit-1',
        status: 'completed' as const,
        completionDate: '2026-09-01',
      };
      vi.spyOn(participationApi, 'recognizePriorCredit').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useRecognizePriorCreditMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        studentId: 'stu-1',
        learningUnitId: 'unit-1',
        academicPeriodId: 'period-1',
        source: 'recognized_prior',
        transferInstitution: 'International Academy',
        creditOrGrade: 'A',
        recognitionBasis: 'Accredited transcript evaluation',
        approvedByAuthority: 'Dean of Studies',
        completionDate: '2026-09-01',
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: academicParticipationKeys.all,
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Prior Credit Recognized',
        }),
      );
    });

    it('useConcludeParticipationMutation concludes participation upon period end', async () => {
      const mockResponse = {
        id: 'part-1',
        status: 'completed' as const,
        completionDate: '2026-12-15',
      };
      vi.spyOn(participationApi, 'concludeParticipation').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useConcludeParticipationMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        id: 'part-1',
        request: { completionDate: '2026-12-15' },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: academicParticipationKeys.all,
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Participation Concluded',
        }),
      );
    });

    it('useUpdateParticipationStatusMutation updates participation status', async () => {
      const mockResponse = {
        id: 'part-1',
        status: 'paused' as const,
      };
      vi.spyOn(participationApi, 'updateParticipationStatus').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useUpdateParticipationStatusMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        id: 'part-1',
        request: { status: 'paused', reason: 'Medical leave' },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: academicParticipationKeys.all,
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Status Updated',
        }),
      );
    });
  });
});
