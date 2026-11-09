// @vitest-environment jsdom
import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import {
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
} from '../useLearningPlans';
import type {
  ActiveStudentLearningPlan,
  PaginatedResponse,
  SlpApproval,
  StudentLearningPlan,
  StudentLearningPlanListItem,
  SyncParticipationsResponse,
} from '../../types';

// Mock PrimeVue useToast
const mockToast = {
  add: vi.fn(),
  removeGroup: vi.fn(),
  removeAllGroups: vi.fn(),
};

vi.mock('primevue/usetoast', () => ({
  useToast: () => mockToast,
}));

// Test mock data
const mockPlanId = '11111111-1111-1111-1111-111111111111';
const mockStudentId = '22222222-2222-2222-2222-222222222222';
const mockPeriodId = '33333333-3333-3333-3333-333333333333';
const mockUnitId = '44444444-4444-4444-4444-444444444444';
const mockAdjustmentId = '55555555-5555-5555-5555-555555555555';

const mockRosterResponse: PaginatedResponse<StudentLearningPlanListItem> = {
  items: [
    {
      id: mockPlanId,
      studentId: mockStudentId,
      studentName: 'Ahmad Dahlan',
      studentNis: 'HAG-2026-001',
      academicPeriodId: mockPeriodId,
      academicPeriodLabel: '2026/2027 Odd',
      academicPeriodName: '2026/2027 Odd',
      status: 'draft',
      version: 1,
      unitsCount: 2,
      adjustmentsCount: 1,
      hasActiveAdjustments: true,
      createdAt: '2026-08-01T00:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
};

const mockDetailResponse: StudentLearningPlan = {
  id: mockPlanId,
  studentId: mockStudentId,
  studentName: 'Ahmad Dahlan',
  studentNis: 'HAG-2026-001',
  academicPeriodId: mockPeriodId,
  academicPeriodLabel: '2026/2027 Odd',
  academicPeriodName: '2026/2027 Odd',
  status: 'draft',
  version: 1,
  notes: 'Individualized remediation roadmap',
  createdAt: '2026-08-01T00:00:00Z',
  createdBy: 'coordinator@hagios.sch.id',
  learningUnits: [
    {
      id: 'entry-1',
      learningUnitId: mockUnitId,
      unitCode: 'MATH-101',
      unitTitle: 'Foundational Mathematics',
      sequenceOrder: 1,
      isRequired: true,
      adaptationNotes: 'Grade 3 pace',
      status: 'active',
    },
  ],
  adjustments: [
    {
      id: mockAdjustmentId,
      adjustmentType: 'accommodation',
      description: '50% extra exam time',
      rationale: 'Processing delay documentation',
      effectiveDate: '2026-08-01',
    },
  ],
  approvals: [],
};

const mockActiveResponse: ActiveStudentLearningPlan = {
  id: mockPlanId,
  planId: mockPlanId,
  studentId: mockStudentId,
  academicPeriodId: mockPeriodId,
  academicPeriodLabel: '2026/2027 Odd',
  version: 1,
  status: 'active',
  notes: 'Active plan',
  learningUnits: [],
  adjustments: [],
};

// Define MSW server handlers
const handlers = [
  // 1. GET /v1/academics/learning-plans
  http.get('*/v1/academics/learning-plans', () => {
    return HttpResponse.json(mockRosterResponse);
  }),

  // 2. GET /v1/academics/learning-plans/:id
  http.get('*/v1/academics/learning-plans/:id', ({ params }) => {
    const { id } = params;
    if (id === mockPlanId) {
      return HttpResponse.json(mockDetailResponse);
    }
    return HttpResponse.json({ title: 'Not Found', status: 404 }, { status: 404 });
  }),

  // 3. GET /v1/academics/students/:studentId/learning-plans/active
  http.get('*/v1/academics/students/:studentId/learning-plans/active', ({ params, request }) => {
    const url = new URL(request.url);
    const periodId = url.searchParams.get('academicPeriodId');
    if (params.studentId === mockStudentId && periodId === mockPeriodId) {
      return HttpResponse.json(mockActiveResponse);
    }
    return HttpResponse.json({ title: 'No active plan found', status: 404 }, { status: 404 });
  }),

  // 4. POST /v1/academics/learning-plans
  http.post('*/v1/academics/learning-plans', async ({ request }) => {
    const body = (await request.json()) as { studentId?: string; periodId?: string };
    if (!body.studentId) {
      return HttpResponse.json(
        { title: 'Validation Error', status: 422, detail: 'Student is required' },
        { status: 422 },
      );
    }
    return HttpResponse.json({ id: 'new-created-plan-id' }, { status: 201 });
  }),

  // 5. PUT /v1/academics/learning-plans/:id
  http.put('*/v1/academics/learning-plans/:id', async ({ params }) => {
    if (params.id === 'invalid-plan') {
      return HttpResponse.json({ title: 'Not Found', status: 404 }, { status: 404 });
    }
    return new HttpResponse(null, { status: 200 });
  }),

  // 6. POST /v1/academics/learning-plans/:id/archive
  http.post('*/v1/academics/learning-plans/:id/archive', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // 7. POST /v1/academics/learning-plans/:id/units
  http.post('*/v1/academics/learning-plans/:id/units', () => {
    return HttpResponse.json({ id: 'new-unit-entry-id' }, { status: 201 });
  }),

  // 8. DELETE /v1/academics/learning-plans/:id/units/:unitId
  http.delete('*/v1/academics/learning-plans/:id/units/:unitId', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // 9. POST /v1/academics/learning-plans/:id/adjustments
  http.post('*/v1/academics/learning-plans/:id/adjustments', () => {
    return HttpResponse.json({ id: 'new-adjustment-entry-id' }, { status: 201 });
  }),

  // 10. DELETE /v1/academics/learning-plans/:id/adjustments/:adjustmentId
  http.delete('*/v1/academics/learning-plans/:id/adjustments/:adjustmentId', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // 11. POST /v1/academics/learning-plans/:id/submit
  http.post('*/v1/academics/learning-plans/:id/submit', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // 12. POST /v1/academics/learning-plans/:id/approve
  http.post('*/v1/academics/learning-plans/:id/approve', async ({ request }) => {
    const body = (await request.json()) as { approvalType?: string; notes?: string };
    const approval: SlpApproval = {
      id: 'appr-msw-1',
      studentLearningPlanId: mockPlanId,
      planVersion: 1,
      approvedBy: 'authority-1',
      approvedByName: 'Curriculum Lead',
      approvedDate: '2026-08-01T10:00:00Z',
      approvalType: (body.approvalType as 'initial_approval') || 'initial_approval',
      notes: body.notes ?? null,
    };
    return HttpResponse.json(approval, { status: 200 });
  }),

  // 13. POST /v1/academics/learning-plans/:id/revise
  http.post('*/v1/academics/learning-plans/:id/revise', () => {
    return HttpResponse.json({ id: 'plan-v2-cloned-id' }, { status: 201 });
  }),

  // 14. POST /v1/academics/learning-plans/:id/sync-participations
  http.post('*/v1/academics/learning-plans/:id/sync-participations', () => {
    const syncResp: SyncParticipationsResponse = {
      studentLearningPlanId: mockPlanId,
      planId: mockPlanId,
      syncedCount: 2,
      updatedCount: 0,
      totalUnits: 2,
      participations: [],
    };
    return HttpResponse.json(syncResp, { status: 200 });
  }),
];

const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  server.resetHandlers();
  vi.clearAllMocks();
});
afterAll(() => server.close());

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

describe('Student Learning Plans Composables with MSW Mocking (P8-S26)', () => {
  describe('Query Composables over HTTP (MSW)', () => {
    it('useLearningPlans fetches roster data through MSW network mock', async () => {
      const filters = ref({ academicPeriodId: mockPeriodId, status: 'draft' as const });
      const { result } = withSetup(() => useLearningPlans(filters));

      expect(result.isLoading.value).toBe(true);
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(result.data.value).toEqual(mockRosterResponse);
      expect(result.data.value?.items[0].studentName).toBe('Ahmad Dahlan');
    });

    it('useLearningPlanDetail retrieves full plan detail via MSW', async () => {
      const planId = ref<string | null>(mockPlanId);
      const { result } = withSetup(() => useLearningPlanDetail(planId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(result.data.value).toEqual(mockDetailResponse);
      expect(result.data.value?.learningUnits).toHaveLength(1);
      expect(result.data.value?.adjustments).toHaveLength(1);
    });

    it('useActiveLearningPlan returns active plan for student and period via MSW', async () => {
      const studentId = ref(mockStudentId);
      const periodId = ref(mockPeriodId);
      const { result } = withSetup(() => useActiveLearningPlan(studentId, periodId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(result.data.value).toEqual(mockActiveResponse);
      expect(result.data.value?.status).toBe('active');
    });

    it('useActiveLearningPlan handles 404 smoothly returning null', async () => {
      const studentId = ref('student-without-plan');
      const periodId = ref(mockPeriodId);
      const { result } = withSetup(() => useActiveLearningPlan(studentId, periodId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(result.data.value).toBeNull();
    });
  });

  describe('Mutation Composables over HTTP (MSW)', () => {
    it('useCreatePlanMutation posts creation request and invalidates query cache', async () => {
      const { result, queryClient } = withSetup(() => useCreatePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync({
        studentId: mockStudentId,
        academicPeriodId: mockPeriodId,
        notes: 'Remedial formulation',
      });

      expect(response).toEqual({ id: 'new-created-plan-id' });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Created',
        }),
      );
    });

    it('useUpdatePlanMutation puts notes update through MSW', async () => {
      const { result, queryClient } = withSetup(() => useUpdatePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: mockPlanId,
        request: { notes: 'Updated notes via MSW' },
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Updated',
        }),
      );
    });

    it('useAddUnitMutation posts constituent unit to MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useAddUnitMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync({
        planId: mockPlanId,
        request: {
          learningUnitId: mockUnitId,
          sequenceOrder: 2,
          isRequired: true,
          adaptationNotes: 'Adaptive pacing',
        },
      });

      expect(response).toEqual({ id: 'new-unit-entry-id' });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Added',
        }),
      );
    });

    it('useRemoveUnitMutation deletes unit through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useRemoveUnitMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        planId: mockPlanId,
        unitId: 'entry-1',
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Removed',
        }),
      );
    });

    it('useAddAdjustmentMutation posts adjustment through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useAddAdjustmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync({
        planId: mockPlanId,
        request: {
          adjustmentType: 'substitution',
          description: 'Adaptive Physical Education',
          rationale: 'Medical certificate on file with sports medical clinic',
          effectiveDate: '2026-08-01',
        },
      });

      expect(response).toEqual({ id: 'new-adjustment-entry-id' });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Adjustment Added',
        }),
      );
    });

    it('useRemoveAdjustmentMutation deletes adjustment through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useRemoveAdjustmentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        planId: mockPlanId,
        adjustmentId: mockAdjustmentId,
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Adjustment Removed',
        }),
      );
    });

    it('useSubmitPlanMutation posts submit through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useSubmitPlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(mockPlanId);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Submitted',
        }),
      );
    });

    it('useApprovePlanMutation executes approval through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useApprovePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const approval = await result.mutateAsync({
        id: mockPlanId,
        request: {
          approvalType: 'initial_approval',
          notes: 'Formally sanctioned by curriculum team',
        },
      });

      expect(approval.id).toBe('appr-msw-1');
      expect(approval.approvalType).toBe('initial_approval');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Approved',
        }),
      );
    });

    it('useRevisePlanMutation initiates plan revision through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useRevisePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync({
        id: mockPlanId,
        request: { notes: 'Amending unit list for next term' },
      });

      expect(response).toEqual({ id: 'plan-v2-cloned-id' });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Plan Revision Initiated',
        }),
      );
    });

    it('useSyncParticipationsMutation triggers explicit sync through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useSyncParticipationsMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync(mockPlanId);

      expect(response.syncedCount).toBe(2);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['academics', 'participations'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Participations Synchronized',
          detail: 'Synchronized 2 student academic participations.',
        }),
      );
    });

    it('useArchivePlanMutation posts archive through MSW mock', async () => {
      const { result, queryClient } = withSetup(() => useArchivePlanMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(mockPlanId);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plan', mockPlanId] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['learning-plans'] });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['active-learning-plan'] });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Plan Archived',
        }),
      );
    });

    it('handles server errors from MSW with RFC 7807 ProblemDetails mapping and error toast', async () => {
      server.use(
        http.post('*/v1/academics/learning-plans', () => {
          return HttpResponse.json(
            {
              title: 'Active Plan Conflict',
              status: 409,
              detail: 'Student already has an active plan for this academic period.',
            },
            { status: 409 },
          );
        }),
      );

      const { result } = withSetup(() => useCreatePlanMutation());

      await expect(
        result.mutateAsync({
          studentId: mockStudentId,
          academicPeriodId: mockPeriodId,
        }),
      ).rejects.toThrow();

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Active Plan Conflict',
          detail: 'Student already has an active plan for this academic period.',
        }),
      );
    });

    it('falls back to default summary when error title is absent', async () => {
      server.use(
        http.post('*/v1/academics/learning-plans', () => {
          return new HttpResponse('Network failed without problem details', { status: 500 });
        }),
      );

      const { result } = withSetup(() => useCreatePlanMutation());

      await expect(
        result.mutateAsync({
          studentId: mockStudentId,
          academicPeriodId: mockPeriodId,
        }),
      ).rejects.toThrow();

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Internal Server Error',
        }),
      );
    });
  });
});
