// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import SupersedeStructureModal from '../../components/SupersedeStructureModal.vue';
import * as curriculumApi from '../../api/curriculumApi';
import type { LearningStructure } from '../../types/curriculum.types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockActiveStructure: LearningStructure = {
  id: 'struct-100',
  name: 'Kurikulum Merdeka SMA 2024',
  description: 'Senior High curriculum framework for national accreditation.',
  academicPeriodId: 'ay-2024',
  academicPeriodLabel: 'TP 2024/2025',
  version: '2024.1',
  effectiveDate: '2024-07-01',
  endDate: null,
  status: 'active',
  approvedBy: 'curriculum-authority@elsasa.sch.id',
  approvedDate: '2024-06-15',
  supersedesId: null,
  notes: 'Accreditation compliant framework.',
  createdAt: '2024-05-01T08:00:00Z',
  createdBy: 'coordinator@elsasa.sch.id',
  updatedAt: null,
  updatedBy: null,
};

const mockPeriodOptions = [
  { id: 'ay-2024', label: 'TP 2024/2025', code: 'AY-2425' },
  { id: 'ay-2025', label: 'TP 2025/2026', code: 'AY-2526' },
];

describe('SupersedeStructureModal.spec.ts (Deep Clone Mutation)', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      window.matchMedia = (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => {},
        removeListener: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
        dispatchEvent: () => false,
      });
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
    mockToastAdd.mockClear();
  });

  function createWrapper(props = {}) {
    return mount(SupersedeStructureModal, {
      props: {
        visible: true,
        structure: mockActiveStructure,
        periods: mockPeriodOptions,
        ...props,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          Dialog: {
            props: ['visible'],
            template: '<div v-if="visible" data-testid="dialog-container"><slot name="header"/><slot /></div>',
          },
          Select: {
            props: ['modelValue', 'options'],
            emits: ['update:modelValue'],
            template: `
              <select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)">
                <option v-for="opt in options" :key="opt.id || opt.value" :value="opt.id || opt.value">{{ opt.label }}</option>
              </select>
            `,
          },
        },
      },
    });
  }

  it('renders deep-clone explanation card, predecessor context, and version inputs', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify explanation card exists
    const explanationCard = wrapper.find('[data-testid="supersede-explanation-card"]');
    expect(explanationCard.exists()).toBe(true);
    expect(explanationCard.text()).toContain('Automated Deep Clone Engine');

    // Predecessor name and version displayed
    expect(wrapper.text()).toContain('Kurikulum Merdeka SMA 2024');
    expect(wrapper.text()).toContain('v2024.1');

    // Inputs rendered
    expect(wrapper.find('[data-testid="supersede-version-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="supersede-effective-date-input"]').exists()).toBe(true);
  });

  it('verifies entering new version and submitting triggers supersede mutation', async () => {
    const clonedResponse: LearningStructure = {
      ...mockActiveStructure,
      id: 'struct-101',
      version: '2024.2',
      status: 'draft',
      supersedesId: 'struct-100',
    };

    const supersedeSpy = vi.spyOn(curriculumApi, 'supersedeLearningStructure').mockResolvedValue(clonedResponse);

    const wrapper = createWrapper();
    await flushPromises();

    // Enter new version and effective date
    await wrapper.find('[data-testid="supersede-version-input"]').setValue('2024.2');
    await wrapper.find('[data-testid="supersede-effective-date-input"]').setValue('2025-01-01');

    // Trigger submit via form method
    await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
    await flushPromises();

    // Assert API client was invoked with structure ID and payload
    expect(supersedeSpy).toHaveBeenCalledWith(
      'struct-100',
      expect.objectContaining({
        newVersion: '2024.2',
        effectiveDate: '2025-01-01',
      }),
    );

    // Emits superseded and closes dialog
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    expect(wrapper.emitted('superseded')?.[0]).toEqual([clonedResponse]);
  });
});
