// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import RegisterStudentModal from '../components/RegisterStudentModal.vue';
import * as learningPlansApi from '@/features/academics/learning-plans/api/learningPlansApi';
import * as participationApi from '../api/participation.api';

const DialogStub = {
  props: ['visible', 'header', 'style', 'modal'],
  emits: ['update:visible'],
  template: `
    <div v-if="visible" class="p-dialog">
      <div class="p-dialog-header">{{ header }}</div>
      <div class="p-dialog-content"><slot /></div>
      <div class="p-dialog-footer"><slot name="footer" /></div>
    </div>
  `,
};

describe('RegisterStudentModal.vue (SCR-ACD-05, P7-S24)', () => {
  let queryClient: QueryClient;

  const mockPeriods = [
    { id: 'period-1', label: '2026/2027 Odd Semester' },
  ];

  const mockUnits = [
    { id: 'unit-1', code: 'MATH-101', name: 'Mathematics 101' },
  ];

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        })),
      });

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  function createWrapper(props = {}) {
    return mount(RegisterStudentModal, {
      props: {
        visible: true,
        periods: mockPeriods,
        units: mockUnits,
        initialPeriodId: 'period-1',
        initialUnitId: 'unit-1',
        ...props,
      },
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          Dialog: DialogStub,
        },
      },
    });
  }

  it('pre-selects source = learning_plan and pre-links active plan ID when student has active SLP', async () => {
    const mockActivePlan = {
      planId: 'plan-uuid-budi',
      studentId: 'student-budi',
      academicPeriodId: 'period-1',
      academicPeriodLabel: '2026/2027 Odd Semester',
      version: 2,
      status: 'active' as const,
      constituentUnits: [],
      adjustments: [
        {
          id: 'adj-1',
          adjustmentType: 'accommodation' as const,
          description: '50% Extra Exam Time',
          rationale: 'Extended time required for written assessments',
          effectiveDate: '2026-08-01',
          approvedBy: 'Ibu Ratna (VP Academics)',
          approvedDate: '2026-08-05',
        },
      ],
    };

    vi.spyOn(learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(mockActivePlan);
    vi.spyOn(learningPlansApi.learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(mockActivePlan);

    const wrapper = createWrapper();
    await flushPromises();

    // Enter student UUID directly to select the student
    const uuidInput = wrapper.find('input[placeholder*="Student UUID directly"]');
    expect(uuidInput.exists()).toBe(true);
    await uuidInput.setValue('student-budi');
    await flushPromises();

    // Active learning plan banner should be displayed
    const slpCallout = wrapper.find('[data-testid="modal-active-slp-callout"]');
    expect(slpCallout.exists()).toBe(true);
    expect(slpCallout.text()).toContain('Active Student Learning Plan (v2)');
    expect(slpCallout.text()).toContain('learning_plan');
  });

  it('submits registration with studentLearningPlanId and source = learning_plan', async () => {
    const mockActivePlan = {
      planId: 'plan-uuid-budi',
      studentId: 'student-budi',
      academicPeriodId: 'period-1',
      academicPeriodLabel: '2026/2027 Odd Semester',
      version: 2,
      status: 'active' as const,
      constituentUnits: [],
      adjustments: [],
    };

    vi.spyOn(learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(mockActivePlan);
    vi.spyOn(learningPlansApi.learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(mockActivePlan);
    const registerSpy = vi.spyOn(participationApi, 'registerParticipation').mockResolvedValue({
      id: 'part-new',
      studentId: 'student-budi',
      learningUnitId: 'unit-1',
      academicPeriodId: 'period-1',
      academicPeriodLabel: '2026/2027 Odd Semester',
      participationType: 'standard',
      attemptNumber: 1,
      source: 'learning_plan',
      status: 'in_progress',
      startDate: '2026-08-01',
      createdAt: '2026-08-01T00:00:00Z',
      createdBy: 'coord-1',
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Set student UUID
    const uuidInput = wrapper.find('input[placeholder*="Student UUID directly"]');
    expect(uuidInput.exists()).toBe(true);
    await uuidInput.setValue('student-budi');
    await flushPromises();

    // Submit form
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    await new Promise((r) => setTimeout(r, 20));
    await flushPromises();

    expect(registerSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        studentId: 'student-budi',
        source: 'learning_plan',
        studentLearningPlanId: 'plan-uuid-budi',
      }),
    );
  });
});
