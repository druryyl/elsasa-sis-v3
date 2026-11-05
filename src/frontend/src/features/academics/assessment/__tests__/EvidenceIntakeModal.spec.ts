// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import EvidenceIntakeModal from '../components/EvidenceIntakeModal.vue';
import * as evidenceApiModule from '../api/evidenceApi';
import type { AssessmentEvidence } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

describe('EvidenceIntakeModal', () => {
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

  const enrolledStudents = [
    { id: 'student-1', name: 'Alice Smith', studentNumber: 'STD-1001' },
    { id: 'student-2', name: 'Bob Jones', studentNumber: 'STD-1002' },
  ];

  const commonStubs = {
    Dialog: {
      props: ['visible', 'header'],
      emits: ['update:visible'],
      template: `
        <div v-if="visible" class="p-dialog" data-testid="evidence-intake-modal">
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
    SelectButton: {
      props: ['modelValue', 'options'],
      emits: ['update:modelValue'],
      template: `
        <div data-testid="candidate-type-toggle">
          <button
            v-for="opt in options"
            :key="opt.value"
            type="button"
            @click="$emit('update:modelValue', opt.value)"
          >
            {{ opt.label }}
          </button>
        </div>
      `,
    },
  };

  it('renders modal with student options by default', async () => {
    const wrapper = mount(EvidenceIntakeModal, {
      props: {
        visible: true,
        assessmentId: 'assess-1',
        assessmentTitle: 'Algebra Midterm',
        enrolledStudents,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    expect(wrapper.find('[data-testid="evidence-intake-modal"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Algebra Midterm');
    expect(wrapper.find('[data-testid="candidate-type-toggle"]').exists()).toBe(true);
  });

  it('submits student evidence recording correctly', async () => {
    const recordedEvidence: AssessmentEvidence = {
      id: 'ev-1',
      assessmentId: 'assess-1',
      studentId: 'student-1',
      evidenceType: 'test_paper',
      collectedDate: '2026-11-05',
      evidenceReference: 'Shelf 3 - Box A',
      createdAt: '2026-11-05T00:00:00Z',
      createdBy: 'teacher-1',
    };

    const recordSpy = vi.spyOn(evidenceApiModule.evidenceApi, 'record').mockResolvedValue(recordedEvidence);

    const wrapper = mount(EvidenceIntakeModal, {
      props: {
        visible: true,
        assessmentId: 'assess-1',
        assessmentTitle: 'Algebra Midterm',
        enrolledStudents,
        preselectedStudentId: 'student-1',
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const refInput = wrapper.find('[data-testid="evidence-reference-input"]');
    await refInput.setValue('Shelf 3 - Box A');

    const form = wrapper.find('form');
    await form.trigger('submit.prevent');
    await flushPromises();

    expect(recordSpy).toHaveBeenCalledWith(
      'assess-1',
      expect.objectContaining({
        studentId: 'student-1',
        evidenceType: 'test_paper',
        evidenceReference: 'Shelf 3 - Box A',
      }),
    );
    expect(wrapper.emitted('saved')).toBeTruthy();
  });
});
