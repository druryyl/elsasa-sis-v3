// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import SlpLearningUnitsTab from '../SlpLearningUnitsTab.vue';
import AddSlpUnitModal, { type LearningUnitOption } from '../AddSlpUnitModal.vue';
import * as learningPlansApi from '../../api/learningPlansApi';
import * as curriculumApi from '@/features/academics/curriculum/api/curriculumApi';
import type { SlpLearningUnit, StudentLearningPlan } from '../../types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockUnits: SlpLearningUnit[] = [
  {
    id: 'unit-entry-1',
    learningPlanId: 'plan-budi-01',
    learningUnitId: 'lu-mat-3',
    unitCode: 'MAT-03-S1',
    title: 'Matematika Remedial Kelas 3',
    sequenceOrder: 1,
    isRequired: true,
    notes: 'Remediation level at Grade 3 pacing',
  },
  {
    id: 'unit-entry-2',
    learningPlanId: 'plan-budi-01',
    learningUnitId: 'lu-ind-4',
    unitCode: 'IND-04-S1',
    title: 'Bahasa Indonesia Kelas 4',
    sequenceOrder: 2,
    isRequired: false,
    notes: null,
  },
];

const mockDraftPlan: StudentLearningPlan = {
  id: 'plan-budi-01',
  studentId: 'student-budi',
  studentName: 'Budi Santoso',
  studentNis: '0071234567',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'draft',
  version: 1,
  notes: 'Individualized plan in draft status',
  createdAt: '2029-07-01T00:00:00Z',
  createdBy: 'coordinator',
  constituentUnits: mockUnits,
  adjustments: [],
  approvals: [],
};

const mockActivePlan: StudentLearningPlan = {
  ...mockDraftPlan,
  id: 'plan-budi-active',
  status: 'active',
  version: 2,
};

const mockAvailableUnits: LearningUnitOption[] = [
  {
    id: '00000000-0000-0000-0000-000000000001',
    code: 'MAT-03-S1',
    name: 'Matematika Remedial Kelas 3',
    status: 'active',
  },
  {
    id: '00000000-0000-0000-0000-000000000002',
    code: 'IND-04-S1',
    name: 'Bahasa Indonesia Kelas 4',
    status: 'active',
  },
  {
    id: '00000000-0000-0000-0000-000000000003',
    code: 'OLD-ARCHIVED-01',
    name: 'Archived Course Unit',
    status: 'archived',
  },
];

describe('SlpLearningUnitsTab.vue & AddSlpUnitModal.vue (SCR-ACD-04, P7-S21)', () => {
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

  // Mock curriculumApi methods to prevent XHR errors in jsdom
  beforeEach(() => {
  setActivePinia(createPinia());
  queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });
  vi.clearAllMocks();

  vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue({
    items: [
      {
        id: 'ls-default',
        academicPeriodId: 'ay-2029-2030',
        academicPeriodName: 'Tahun Pelajaran 2029/2030',
        status: 'active',
        code: 'LS-2029',
        name: 'Primary Framework 2029',
        version: 1,
        createdAt: '2029-01-01',
      },
    ],
    total: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
  } as any);
  vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockAvailableUnits as any);
});

  function createTabWrapper(props = {}) {
    return mount(SlpLearningUnitsTab, {
      props: {
        plan: mockDraftPlan,
        availableUnits: mockAvailableUnits,
        ...props,
      },
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
      },
    });
  }

  describe('SlpLearningUnitsTab.vue', () => {
    it('renders DataTable showing sequence order, unit code, title, requirement tag, and adaptation notes', () => {
      const wrapper = createTabWrapper();
      expect(wrapper.find('[data-testid="slp-learning-units-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="slp-learning-units-table"]').exists()).toBe(true);

      const text = wrapper.text();
      expect(text).toContain('MAT-03-S1');
      expect(text).toContain('Matematika Remedial Kelas 3');
      expect(text).toContain('IND-04-S1');
      expect(text).toContain('Bahasa Indonesia Kelas 4');
      expect(text).toContain('Remediation level at Grade 3 pacing');
    });

    it('displays requirement tags distinguishing Required vs Elective units', () => {
      const wrapper = createTabWrapper();

      const requiredTag = wrapper.find('[data-testid="unit-requirement-unit-entry-1"]');
      expect(requiredTag.exists()).toBe(true);
      expect(requiredTag.text()).toBe('Required');

      const electiveTag = wrapper.find('[data-testid="unit-requirement-unit-entry-2"]');
      expect(electiveTag.exists()).toBe(true);
      expect(electiveTag.text()).toBe('Elective');
    });

    it('shows "+ Add Learning Unit" button and inline remove buttons when plan is draft', () => {
      const wrapper = createTabWrapper({ plan: mockDraftPlan });

      const addBtn = wrapper.find('[data-testid="add-learning-unit-btn"]');
      expect(addBtn.exists()).toBe(true);

      const removeBtn1 = wrapper.find('[data-testid="remove-unit-btn-unit-entry-1"]');
      expect(removeBtn1.exists()).toBe(true);
      const removeBtn2 = wrapper.find('[data-testid="remove-unit-btn-unit-entry-2"]');
      expect(removeBtn2.exists()).toBe(true);
    });

    it('hides "+ Add Learning Unit" button and remove action buttons when plan is active', () => {
      const wrapper = createTabWrapper({ plan: mockActivePlan });

      const addBtn = wrapper.find('[data-testid="add-learning-unit-btn"]');
      expect(addBtn.exists()).toBe(false);

      const removeBtn1 = wrapper.find('[data-testid="remove-unit-btn-unit-entry-1"]');
      expect(removeBtn1.exists()).toBe(false);
    });

    it('renders empty state with guidance when no constituent units exist on plan', () => {
      const emptyPlan: StudentLearningPlan = {
        ...mockDraftPlan,
        constituentUnits: [],
      };
      const wrapper = createTabWrapper({ plan: emptyPlan });

      expect(wrapper.find('[data-testid="slp-units-empty-state"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No constituent learning units configured');
      expect(wrapper.text()).toContain('This learning plan has no linked curriculum units.');
      expect(wrapper.find('[data-testid="empty-add-unit-btn"]').exists()).toBe(true);
    });

    it('triggers remove unit mutation when inline remove button is clicked', async () => {
      const spyRemove = vi
        .spyOn(learningPlansApi.learningPlansApi, 'removeUnit')
        .mockResolvedValue();

      const wrapper = createTabWrapper({ plan: mockDraftPlan });
      const removeBtn = wrapper.find('[data-testid="remove-unit-btn-unit-entry-1"]');
      expect(removeBtn.exists()).toBe(true);

      await removeBtn.trigger('click');
      await flushPromises();

      expect(spyRemove).toHaveBeenCalledWith('plan-budi-01', 'unit-entry-1');
      expect(wrapper.emitted('unitRemoved')).toBeTruthy();
      expect(wrapper.emitted('unitRemoved')![0]).toEqual(['unit-entry-1']);
    });
  });

  describe('AddSlpUnitModal.vue', () => {
    function createModalWrapper(props = {}) {
      return mount(AddSlpUnitModal, {
        props: {
          visible: true,
          planId: 'plan-budi-01',
          units: mockAvailableUnits,
          ...props,
        },
        global: {
          plugins: [
            PrimeVue,
            [VueQueryPlugin, { queryClient }],
          ],
          stubs: {
            Dialog: {
              props: ['visible', 'header'],
              template: '<div v-if="visible" data-testid="add-slp-unit-modal"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
            },
          },
        },
      });
    }

    it('renders form inputs for unit selector, sequence order, isRequired toggle, and adaptation notes', () => {
      const wrapper = createModalWrapper();

      expect(wrapper.find('[data-testid="add-slp-unit-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="slp-unit-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="slp-sequence-order-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="slp-is-required-switch"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="slp-unit-notes-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="submit-add-unit-btn"]').exists()).toBe(true);
    });

    it('filters unit dropdown to active learning units, omitting archived units', () => {
      const wrapper = createModalWrapper();
      const vm = wrapper.vm as any;

      // Filtered to active only: mockAvailableUnits has 2 active and 1 archived
      expect(vm.unitDropdownOptions.length).toBe(2);
      expect(vm.unitDropdownOptions.some((u: any) => u.code === 'OLD-ARCHIVED-01')).toBe(false);
      expect(vm.unitDropdownOptions.some((u: any) => u.code === 'MAT-03-S1')).toBe(true);
      expect(vm.unitDropdownOptions.some((u: any) => u.code === 'IND-04-S1')).toBe(true);
    });

    it('defaults isRequired toggle to true (Mandatory Core Requirement)', () => {
      const wrapper = createModalWrapper();
      const vm = wrapper.vm as any;

      expect(vm.isRequired).toBe(true);
      expect(wrapper.text()).toContain('Mandatory Core Requirement');
    });

    it('submits valid learning unit payload through API mutation', async () => {
      const spyAdd = vi
        .spyOn(learningPlansApi.learningPlansApi, 'addUnit')
        .mockResolvedValue({ id: 'new-unit-entry-id' });

      const wrapper = createModalWrapper();
      const vm = wrapper.vm as any;

      // Set valid values
      vm.learningUnitId = '00000000-0000-0000-0000-000000000001';
      vm.sequenceOrder = 3;
      vm.isRequired = true;
      vm.notes = 'Remedial adaptation notes for math';

      await wrapper.find('form').trigger('submit.prevent');
      await flushPromises();

      expect(spyAdd).toHaveBeenCalledWith('plan-budi-01', {
        learningUnitId: '00000000-0000-0000-0000-000000000001',
        sequenceOrder: 3,
        isRequired: true,
        notes: 'Remedial adaptation notes for math',
      });

      expect(wrapper.emitted('added')).toBeTruthy();
      expect(wrapper.emitted('added')![0]).toEqual(['new-unit-entry-id']);
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
    });
  });
});
