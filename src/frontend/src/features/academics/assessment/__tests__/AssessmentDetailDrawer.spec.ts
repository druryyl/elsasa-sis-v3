// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import AssessmentDetailDrawer from '../components/AssessmentDetailDrawer.vue';
import * as assessmentsApiModule from '../api/assessmentsApi';
import type { Assessment } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

describe('AssessmentDetailDrawer', () => {
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
    title: 'Mid-Term Comprehensive Exam',
    assessmentType: 'summative',
    assessmentMethod: 'written_exam',
    maxScore: 100,
    passingScore: 75,
    reassessmentPolicy: 'capped_at_passing',
    criteria: 'Algebra concepts 50%, Trigonometry 50%',
    assessmentDate: '2026-11-10',
    assessorReference: 'Dr. Drury',
    status: 'planned',
    notes: 'Calculators permitted.',
    evidenceCount: 5,
    resultCount: 20,
    participantCount: 25,
    createdAt: '2026-11-01T00:00:00Z',
    createdBy: 'teacher-1',
  };

  const commonStubs = {
    Drawer: {
      props: ['visible'],
      emits: ['update:visible'],
      template: `
        <div v-if="visible" class="p-drawer" data-testid="assessment-detail-drawer">
          <slot name="header" />
          <slot />
        </div>
      `,
    },
    Dialog: {
      props: ['visible', 'header'],
      emits: ['update:visible'],
      template: `
        <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid'] || 'p-dialog'">
          <div class="p-dialog-header">{{ header }}</div>
          <slot />
        </div>
      `,
    },
  };

  it('renders drawer with assessment metadata, rubric descriptors, and metrics', async () => {
    const wrapper = mount(AssessmentDetailDrawer, {
      props: {
        visible: true,
        assessment: sampleAssessment,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    expect(wrapper.find('[data-testid="assessment-detail-drawer"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Mid-Term Comprehensive Exam');
    expect(wrapper.text()).toContain('MATH-10 Mathematics 10');
    expect(wrapper.text()).toContain('Algebra concepts 50%, Trigonometry 50%');
    expect(wrapper.text()).toContain('25'); // participants
    expect(wrapper.text()).toContain('75 pts'); // KKM
    expect(wrapper.text().toLowerCase()).toContain('capped at passing');
  });

  it('opens conduct dialog and submits conduct mutation', async () => {
    const conductSpy = vi.spyOn(assessmentsApiModule.assessmentsApi, 'conduct').mockResolvedValue({
      id: 'assess-101',
      status: 'conducted',
      conductedDate: '2026-11-10',
    });

    const wrapper = mount(AssessmentDetailDrawer, {
      props: {
        visible: true,
        assessment: sampleAssessment,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const conductBtn = wrapper.find('[data-testid="drawer-conduct-btn"]');
    expect(conductBtn.exists()).toBe(true);
    await conductBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="conduct-dialog"]').exists()).toBe(true);

    const confirmBtn = wrapper.find('[data-testid="confirm-conduct-btn"]');
    await confirmBtn.trigger('click');
    await flushPromises();

    expect(conductSpy).toHaveBeenCalledWith('assess-101', expect.anything());
    expect(wrapper.emitted('conducted')).toBeTruthy();
  });

  it('opens cancel dialog and submits cancel mutation with mandatory reason', async () => {
    const cancelSpy = vi.spyOn(assessmentsApiModule.assessmentsApi, 'cancel').mockResolvedValue({
      id: 'assess-101',
      status: 'cancelled',
    });

    const wrapper = mount(AssessmentDetailDrawer, {
      props: {
        visible: true,
        assessment: sampleAssessment,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const cancelBtn = wrapper.find('[data-testid="drawer-cancel-btn"]');
    expect(cancelBtn.exists()).toBe(true);
    await cancelBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="cancel-dialog"]').exists()).toBe(true);

    const reasonInput = wrapper.find('[data-testid="cancel-reason-textarea"]');
    await reasonInput.setValue('Campus closed due to weather conditions.');

    const confirmBtn = wrapper.find('[data-testid="confirm-cancel-btn"]');
    await confirmBtn.trigger('click');
    await flushPromises();

    expect(cancelSpy).toHaveBeenCalledWith('assess-101', 'Campus closed due to weather conditions.');
    expect(wrapper.emitted('cancelled')).toBeTruthy();
  });
});
