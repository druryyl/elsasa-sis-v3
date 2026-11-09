// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import AssessmentPlannerModal from '../components/AssessmentPlannerModal.vue';
import * as assessmentsApiModule from '../api/assessmentsApi';
import type { Assessment } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

describe('AssessmentPlannerModal', () => {
  let queryClient: QueryClient;

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
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const sampleAssessment: Assessment = {
    id: 'assess-101',
    learningUnitId: 'unit-1',
    learningUnitTitle: 'Mathematics 10',
    learningUnitCode: 'MATH-10',
    title: 'Mid-Term Exam',
    assessmentType: 'summative',
    assessmentMethod: 'written_exam',
    maxScore: 100,
    passingScore: 75,
    reassessmentPolicy: 'capped_at_passing',
    criteria: 'Algebra concepts 50%, Trigonometry 50%',
    assessmentDate: '2026-11-10',
    assessorReference: 'Dr. Drury',
    status: 'planned',
    notes: 'Calculators allowed',
    evidenceCount: 0,
    resultCount: 0,
    createdAt: '2026-11-01T00:00:00Z',
    createdBy: 'teacher-1',
  };

  const commonStubs = {
    Dialog: {
      props: ['visible', 'header'],
      emits: ['update:visible'],
      template: `
        <div v-if="visible" class="p-dialog" data-testid="assessment-planner-modal">
          <div class="p-dialog-header">{{ header }}</div>
          <slot />
        </div>
      `,
    },
    Select: {
      props: ['modelValue', 'options'],
      emits: ['update:modelValue'],
      template: `
        <select
          :value="modelValue"
          @change="$emit('update:modelValue', $event.target.value)"
        >
          <option v-for="opt in options" :key="opt.value" :value="opt.value">
            {{ opt.label }}
          </option>
        </select>
      `,
    },
  };

  it('renders modal in create mode with default fields', async () => {
    const wrapper = mount(AssessmentPlannerModal, {
      props: {
        visible: true,
        learningUnitId: 'unit-1',
        learningUnitTitle: 'Mathematics 10',
        unitOptions: [{ id: 'unit-1', name: 'Mathematics 10', code: 'MATH-10' }],
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    expect(wrapper.find('[data-testid="assessment-planner-modal"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Plan New Assessment');
    expect(wrapper.find('[data-testid="planner-title-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="planner-max-score-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="planner-passing-score-input"]').exists()).toBe(true);
  });

  it('populates fields in edit mode from assessment prop', async () => {
    const wrapper = mount(AssessmentPlannerModal, {
      props: {
        visible: true,
        assessment: sampleAssessment,
        learningUnitId: 'unit-1',
        learningUnitTitle: 'Mathematics 10',
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('Edit Assessment Configuration');
    const titleInput = wrapper.find('[data-testid="planner-title-input"]').element as HTMLInputElement;
    expect(titleInput.value).toBe('Mid-Term Exam');

    const maxScoreInput = wrapper.find('[data-testid="planner-max-score-input"]').element as HTMLInputElement;
    expect(maxScoreInput.value).toBe('100');

    const passingScoreInput = wrapper.find('[data-testid="planner-passing-score-input"]').element as HTMLInputElement;
    expect(passingScoreInput.value).toBe('75');
  });

  it('submits create assessment request via mutation', async () => {
    const createSpy = vi.spyOn(assessmentsApiModule.assessmentsApi, 'create').mockResolvedValue({
      id: 'assess-new-1',
      learningUnitId: 'unit-1',
      title: 'Practical Lab Test',
      status: 'planned',
    });

    const wrapper = mount(AssessmentPlannerModal, {
      props: {
        visible: true,
        learningUnitId: 'unit-1',
        learningUnitTitle: 'Mathematics 10',
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const titleInput = wrapper.find('[data-testid="planner-title-input"]');
    await titleInput.setValue('Practical Lab Test');

    const form = wrapper.find('form');
    await form.trigger('submit.prevent');
    await flushPromises();

    expect(createSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        learningUnitId: 'unit-1',
        title: 'Practical Lab Test',
        assessmentType: 'formative',
      }),
    );
    expect(wrapper.emitted('created')).toBeTruthy();
  });

  it('submits update assessment request when editing', async () => {
    const updateSpy = vi.spyOn(assessmentsApiModule.assessmentsApi, 'update').mockResolvedValue({
      id: 'assess-101',
      title: 'Updated Mid-Term Exam',
      status: 'planned',
    });

    const wrapper = mount(AssessmentPlannerModal, {
      props: {
        visible: true,
        assessment: sampleAssessment,
        learningUnitId: 'unit-1',
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const titleInput = wrapper.find('[data-testid="planner-title-input"]');
    await titleInput.setValue('Updated Mid-Term Exam');

    const form = wrapper.find('form');
    await form.trigger('submit.prevent');
    await flushPromises();

    expect(updateSpy).toHaveBeenCalledWith(
      'assess-101',
      expect.objectContaining({
        title: 'Updated Mid-Term Exam',
      }),
    );
    expect(wrapper.emitted('updated')).toBeTruthy();
  });
});
