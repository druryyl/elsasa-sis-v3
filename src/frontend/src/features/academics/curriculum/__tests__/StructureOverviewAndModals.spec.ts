// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import StructureOverviewTab from '../components/StructureOverviewTab.vue';
import CreateStructureModal from '../components/CreateStructureModal.vue';
import EditStructureModal from '../components/EditStructureModal.vue';
import SupersedeStructureModal from '../components/SupersedeStructureModal.vue';
import * as curriculumApi from '../api/curriculumApi';
import type { LearningStructure } from '../types/curriculum.types';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockStructure: LearningStructure = {
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
  supersedesId: 'struct-090',
  predecessorName: 'Kurikulum 2013 Revisi',
  predecessorVersion: '2023.2',
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

describe('Structure Overview & Lineage Tab and Modals (P6-S17)', () => {
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
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    mockToastAdd.mockClear();
  });

  // --------------------------------------------------------------------------
  // 1. StructureOverviewTab.vue
  // --------------------------------------------------------------------------
  describe('StructureOverviewTab.vue', () => {
    it('renders framework metadata, effective dates, and approval audit info', () => {
      const wrapper = mount(StructureOverviewTab, {
        props: {
          structure: mockStructure,
        },
        global: {
          plugins: [PrimeVue],
          stubs: {
            Tag: {
              props: ['value', 'severity'],
              template: `<span class="p-tag" :data-severity="severity">{{ value }}</span>`,
            },
          },
        },
      });

      expect(wrapper.find('[data-testid="overview-name"]').text()).toBe('Kurikulum Merdeka SMA 2024');
      expect(wrapper.find('[data-testid="overview-period"]').text()).toBe('TP 2024/2025');
      expect(wrapper.find('[data-testid="overview-description"]').text()).toContain('Senior High curriculum framework');
      expect(wrapper.text()).toContain('v2024.1');
      expect(wrapper.text()).toContain('ACTIVE');
      expect(wrapper.text()).toContain('2024-07-01');
      expect(wrapper.text()).toContain('Active Indefinitely');
      expect(wrapper.text()).toContain('curriculum-authority@elsasa.sch.id');
      expect(wrapper.text()).toContain('Accreditation compliant framework.');
    });

    it('renders predecessor lineage card and emits selectPredecessor on click', async () => {
      const wrapper = mount(StructureOverviewTab, {
        props: {
          structure: mockStructure,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      const predecessorBtn = wrapper.find('[data-testid="link-view-predecessor"]');
      expect(predecessorBtn.exists()).toBe(true);
      expect(wrapper.text()).toContain('Kurikulum 2013 Revisi (v2023.2)');

      await predecessorBtn.trigger('click');
      expect(wrapper.emitted('selectPredecessor')?.[0]).toEqual(['struct-090']);
    });

    it('renders baseline framework text when structure has no predecessor', () => {
      const baselineStructure: LearningStructure = {
        ...mockStructure,
        supersedesId: null,
        predecessorName: null,
      };

      const wrapper = mount(StructureOverviewTab, {
        props: {
          structure: baselineStructure,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      expect(wrapper.find('[data-testid="link-view-predecessor"]').exists()).toBe(false);
      expect(wrapper.text()).toContain('This is an initial baseline framework (no predecessor lineage).');
    });
  });

  // --------------------------------------------------------------------------
  // 2. CreateStructureModal.vue
  // --------------------------------------------------------------------------
  describe('CreateStructureModal.vue', () => {
    it('renders modal dialog form fields and pre-selects academic period', async () => {
      const wrapper = mount(CreateStructureModal, {
        props: {
          visible: true,
          periodId: 'ay-2024',
          periods: mockPeriodOptions,
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible'],
              template: '<div v-if="visible" data-testid="dialog"><slot name="header"/><slot /></div>',
            },
            Select: {
              props: ['modelValue', 'options'],
              emits: ['update:modelValue'],
              template: `
                <select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)" data-testid="select">
                  <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
              `,
            },
          },
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="structure-name-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="structure-version-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="structure-effective-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="structure-description-input"]').exists()).toBe(true);
    });

    it('submits createLearningStructure mutation on valid form input and emits created and update:visible', async () => {
      const createSpy = vi.spyOn(curriculumApi, 'createLearningStructure').mockResolvedValue(mockStructure);

      const wrapper = mount(CreateStructureModal, {
        props: {
          visible: true,
          periodId: 'ay-2024',
          periods: mockPeriodOptions,
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible'],
              template: '<div v-if="visible"><slot name="header"/><slot /></div>',
            },
            Select: {
              props: ['modelValue', 'options'],
              emits: ['update:modelValue'],
              template: `
                <select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)">
                  <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
              `,
            },
          },
        },
      });

      await flushPromises();

      await wrapper.find('[data-testid="structure-name-input"]').setValue('New Framework 2025');
      await wrapper.find('[data-testid="structure-version-input"]').setValue('2025.1');
      await wrapper.find('[data-testid="structure-effective-date-input"]').setValue('2025-07-01');

      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(createSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'New Framework 2025',
          academicPeriodId: 'ay-2024',
          version: '2025.1',
          effectiveDate: '2025-07-01',
        }),
      );

      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
      expect(wrapper.emitted('created')?.[0]).toEqual([mockStructure]);
    });
  });

  // --------------------------------------------------------------------------
  // 3. EditStructureModal.vue
  // --------------------------------------------------------------------------
  describe('EditStructureModal.vue', () => {
    it('pre-populates form fields and displays active lock banner when structure is active', async () => {
      const wrapper = mount(EditStructureModal, {
        props: {
          visible: true,
          structure: mockStructure,
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible'],
              template: '<div v-if="visible"><slot name="header"/><slot /></div>',
            },
          },
        },
      });

      await flushPromises();

      // Active locked alert banner is visible
      expect(wrapper.find('[data-testid="edit-locked-alert"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="edit-locked-alert"]').text()).toContain('Structural Fields Locked');

      // Pre-populated name input
      const nameInput = wrapper.find<HTMLInputElement>('[data-testid="edit-structure-name-input"]');
      expect(nameInput.element.value).toBe('Kurikulum Merdeka SMA 2024');
    });

    it('submits updateLearningStructure mutation on valid form input and emits updated', async () => {
      const updateSpy = vi.spyOn(curriculumApi, 'updateLearningStructure').mockResolvedValue(mockStructure);

      const wrapper = mount(EditStructureModal, {
        props: {
          visible: true,
          structure: mockStructure,
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible'],
              template: '<div v-if="visible"><slot name="header"/><slot /></div>',
            },
          },
        },
      });

      await flushPromises();

      await wrapper.find('[data-testid="edit-structure-name-input"]').setValue('Updated Curriculum Title');
      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(updateSpy).toHaveBeenCalledWith(
        'struct-100',
        expect.objectContaining({
          name: 'Updated Curriculum Title',
        }),
      );

      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
      expect(wrapper.emitted('updated')?.[0]).toEqual([mockStructure]);
    });
  });

  // --------------------------------------------------------------------------
  // 4. SupersedeStructureModal.vue
  // --------------------------------------------------------------------------
  describe('SupersedeStructureModal.vue', () => {
    it('renders deep-clone explanation card, predecessor context, and version inputs', async () => {
      const wrapper = mount(SupersedeStructureModal, {
        props: {
          visible: true,
          structure: mockStructure,
          periods: mockPeriodOptions,
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible'],
              template: '<div v-if="visible"><slot name="header"/><slot /></div>',
            },
            Select: {
              props: ['modelValue', 'options'],
              emits: ['update:modelValue'],
              template: `
                <select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)">
                  <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
              `,
            },
          },
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="supersede-explanation-card"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="supersede-explanation-card"]').text()).toContain('Automated Deep Clone Engine');
      expect(wrapper.text()).toContain('Kurikulum Merdeka SMA 2024');
      expect(wrapper.find('[data-testid="supersede-version-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="supersede-effective-date-input"]').exists()).toBe(true);
    });

    it('submits supersedeLearningStructure mutation and emits superseded on success', async () => {
      const clonedStructure: LearningStructure = {
        ...mockStructure,
        id: 'struct-101',
        version: '2024.2',
        status: 'draft',
        supersedesId: 'struct-100',
      };

      const supersedeSpy = vi.spyOn(curriculumApi, 'supersedeLearningStructure').mockResolvedValue(clonedStructure);

      const wrapper = mount(SupersedeStructureModal, {
        props: {
          visible: true,
          structure: mockStructure,
          periods: mockPeriodOptions,
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible'],
              template: '<div v-if="visible"><slot name="header"/><slot /></div>',
            },
            Select: {
              props: ['modelValue', 'options'],
              emits: ['update:modelValue'],
              template: `
                <select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)">
                  <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
              `,
            },
          },
        },
      });

      await flushPromises();

      await wrapper.find('[data-testid="supersede-version-input"]').setValue('2024.2');
      await wrapper.find('[data-testid="supersede-effective-date-input"]').setValue('2025-01-01');

      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(supersedeSpy).toHaveBeenCalledWith(
        'struct-100',
        expect.objectContaining({
          newVersion: '2024.2',
          effectiveDate: '2025-01-01',
        }),
      );

      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
      expect(wrapper.emitted('superseded')?.[0]).toEqual([clonedStructure]);
    });
  });
});
