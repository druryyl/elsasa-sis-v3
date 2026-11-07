// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import SlpAdjustmentsTab from '../SlpAdjustmentsTab.vue';
import AddAdjustmentModal from '../AddAdjustmentModal.vue';
import * as learningPlansApi from '../../api/learningPlansApi';
import type { AcademicPlanAdjustment, StudentLearningPlan } from '../../types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockAdjustments: AcademicPlanAdjustment[] = [
  {
    id: 'adj-01',
    adjustmentType: 'accommodation',
    description: '50% extra exam time for written assessments',
    rationale: 'Diagnosed slow processing speed documented in developmental psychological report.',
    effectiveDate: '2029-07-01',
    endDate: '2029-12-31',
    approvedBy: 'Dr. Sarah Curriculum Lead',
    approvedDate: '2029-07-10',
    notes: 'To be provided in separate quiet testing room.',
  },
  {
    id: 'adj-02',
    adjustmentType: 'substitution',
    description: 'Adaptive PJOK in place of standard Physical Education',
    rationale: 'Orthopedic adaptation required following pediatric surgery rehabilitation.',
    effectiveDate: '2029-07-01',
    endDate: null,
    approvedBy: 'Dr. Sarah Curriculum Lead',
    approvedDate: '2029-07-10',
    notes: null,
  },
  {
    id: 'adj-03',
    adjustmentType: 'exemption',
    description: 'Exemption from foreign language requirement',
    rationale: 'Speech therapy protocol and dual language cognitive exemption granted by school committee.',
    effectiveDate: '2029-08-01',
    endDate: '2030-06-30',
    approvedBy: '',
    approvedDate: '1900-01-01',
    notes: 'Pending committee re-evaluation.',
  },
];

const mockDraftPlan: StudentLearningPlan = {
  id: 'plan-draft-01',
  studentId: 'student-timotius',
  studentName: 'Timotius',
  studentNis: '0079876543',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'draft',
  version: 1,
  notes: 'Formulating draft adaptations',
  createdAt: '2029-07-01T00:00:00Z',
  createdBy: 'coordinator',
  constituentUnits: [],
  adjustments: mockAdjustments,
  approvals: [],
};

const mockActivePlan: StudentLearningPlan = {
  ...mockDraftPlan,
  id: 'plan-active-01',
  status: 'active',
  version: 2,
};

describe('SlpAdjustmentsTab.vue & AddAdjustmentModal.vue (SCR-ACD-04, P7-S22)', () => {
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
    vi.clearAllMocks();
  });

  function createWrapper(props = {}) {
    return mount(SlpAdjustmentsTab, {
      props: {
        plan: mockDraftPlan,
        ...props,
      },
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          ConfirmDialog: true,
        },
      },
    });
  }

  describe('SlpAdjustmentsTab.vue', () => {
    it('renders adjustments DataTable with all adjustment rows', () => {
      const wrapper = createWrapper();
      expect(wrapper.find('[data-testid="slp-adjustments-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="adjustments-datatable"]').exists()).toBe(true);

      const text = wrapper.text();
      expect(text).toContain('50% extra exam time for written assessments');
      expect(text).toContain('Adaptive PJOK in place of standard Physical Education');
      expect(text).toContain('Exemption from foreign language requirement');
    });

    it('renders distinct type badges for Accommodation (blue), Substitution (purple), and Exemption (orange)', () => {
      const wrapper = createWrapper();

      const accommodationBadge = wrapper.find('[data-testid="badge-accommodation"]');
      expect(accommodationBadge.exists()).toBe(true);
      expect(accommodationBadge.text().toLowerCase()).toContain('accommodation');

      const substitutionBadge = wrapper.find('[data-testid="badge-substitution"]');
      expect(substitutionBadge.exists()).toBe(true);
      expect(substitutionBadge.text().toLowerCase()).toContain('substitution');

      const exemptionBadge = wrapper.find('[data-testid="badge-exemption"]');
      expect(exemptionBadge.exists()).toBe(true);
      expect(exemptionBadge.text().toLowerCase()).toContain('exemption');
    });

    it('formats effective date and end date via date-fns', () => {
      const wrapper = createWrapper();
      const text = wrapper.text();

      // Effective range: 01 Jul 2029 – 31 Dec 2029
      expect(text).toContain('01 Jul 2029 – 31 Dec 2029');
      // Ongoing range: 01 Jul 2029 – Ongoing
      expect(text).toContain('01 Jul 2029 – Ongoing');
    });

    it('displays approving authority and date stamp when approved, and "Pending Plan Approval" when unapproved', () => {
      const wrapper = createWrapper();
      const text = wrapper.text();

      // Approved rows
      expect(text).toContain('Dr. Sarah Curriculum Lead');
      expect(text).toContain('Approved 10 Jul 2029');

      // Unapproved row (approvedDate 1900-01-01)
      expect(text).toContain('Pending Plan Approval');
    });

    it('shows "+ Add Adjustment" button and Actions column when plan status is draft', () => {
      const wrapper = createWrapper({ plan: mockDraftPlan });
      expect(wrapper.find('[data-testid="open-add-adjustment-btn"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="remove-adjustment-adj-01"]').exists()).toBe(true);
    });

    it('hides "+ Add Adjustment" button and remove action buttons when plan is active', () => {
      const wrapper = createWrapper({ plan: mockActivePlan });
      expect(wrapper.find('[data-testid="open-add-adjustment-btn"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="remove-adjustment-adj-01"]').exists()).toBe(false);
    });

    it('renders empty state when plan has no adjustments', () => {
      const emptyPlan: StudentLearningPlan = {
        ...mockDraftPlan,
        adjustments: [],
      };
      const wrapper = createWrapper({ plan: emptyPlan });

      expect(wrapper.find('[data-testid="adjustments-empty-state"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No academic adjustments recorded');
      expect(wrapper.find('[data-testid="empty-add-adjustment-btn"]').exists()).toBe(true);
    });

    it('triggers removal mutation when remove button is clicked', async () => {
      const spyRemove = vi.spyOn(learningPlansApi.learningPlansApi, 'removeAdjustment').mockResolvedValue();
      const wrapper = createWrapper();

      const removeBtn = wrapper.find('[data-testid="remove-adjustment-adj-01"]');
      expect(removeBtn.exists()).toBe(true);
      await removeBtn.trigger('click');
      await flushPromises();

      expect(spyRemove).toHaveBeenCalledWith('plan-draft-01', 'adj-01');
    });
  });

  describe('AddAdjustmentModal.vue', () => {
    function createModalWrapper(props = {}) {
      return mount(AddAdjustmentModal, {
        props: {
          visible: true,
          planId: 'plan-draft-01',
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
              template: '<div v-if="visible" data-testid="add-adjustment-modal"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
            },
            Select: {
              props: ['options', 'modelValue'],
              template: '<select :value="modelValue" @change="$emit(\'update:modelValue\', $event.target.value)"><option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option></select>',
            },
            DatePicker: {
              props: ['modelValue'],
              template: '<input type="text" :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />',
            },
          },
        },
      });
    }

    it('renders category selector, description, rationale, and date picker fields', () => {
      const wrapper = createModalWrapper();
      expect(wrapper.find('[data-testid="add-adjustment-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="adjustment-type-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="adjustment-description-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="adjustment-rationale-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="adjustment-effective-date"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="adjustment-end-date"]').exists()).toBe(true);
    });

    it('submits valid adjustment and emits added event', async () => {
      const spyAdd = vi.spyOn(learningPlansApi.learningPlansApi, 'addAdjustment').mockResolvedValue({
        id: 'new-adj-99',
      });

      const wrapper = createModalWrapper();

      const descInput = wrapper.find('[data-testid="adjustment-description-input"]');
      await descInput.setValue('Enlarged print examination materials (24pt font)');

      const rationaleInput = wrapper.find('[data-testid="adjustment-rationale-input"]');
      await rationaleInput.setValue('Documented pediatric visual impairment and ophthalmology prescription.');

      const dateInput = wrapper.find('[data-testid="adjustment-effective-date"] input');
      if (dateInput.exists()) {
        await dateInput.setValue('2029-08-01');
      }

      const form = wrapper.find('form');
      await form.trigger('submit.prevent');
      await flushPromises();

      expect(spyAdd).toHaveBeenCalledWith('plan-draft-01', expect.objectContaining({
        adjustmentType: 'accommodation',
        description: 'Enlarged print examination materials (24pt font)',
        rationale: 'Documented pediatric visual impairment and ophthalmology prescription.',
      }));

      expect(wrapper.emitted('added')).toBeTruthy();
      expect(wrapper.emitted('added')![0]).toEqual(['new-adj-99']);
    });

    it('validates minimum 10 characters for rationale', async () => {
      const spyAdd = vi.spyOn(learningPlansApi.learningPlansApi, 'addAdjustment');
      const wrapper = createModalWrapper();

      const descInput = wrapper.find('[data-testid="adjustment-description-input"]');
      await descInput.setValue('Short adjustment');

      const rationaleInput = wrapper.find('[data-testid="adjustment-rationale-input"]');
      await rationaleInput.setValue('Too short'); // only 9 chars

      const form = wrapper.find('form');
      await form.trigger('submit.prevent');
      await flushPromises();

      expect(spyAdd).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Rationale must be at least 10 characters.');
    });
  });
});
