// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import AcademicRequirementsTab from '../components/AcademicRequirementsTab.vue';
import CreateRequirementModal from '../components/CreateRequirementModal.vue';
import EditRequirementModal from '../components/EditRequirementModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import * as curriculumApi from '../api/curriculumApi';
import type { AcademicRequirement, LearningUnit } from '../types/curriculum.types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockRequirements: AcademicRequirement[] = [
  {
    id: 'req-prog-1',
    name: 'Total Minimum Credits for Diploma',
    requirementScope: 'program',
    learningStructureId: 'struct-100',
    requirementType: 'credit',
    description: 'Students must earn at least 120 credits across 4 years to qualify for graduation.',
    version: '1.0',
    effectiveDate: '2024-07-01',
    endDate: null,
    status: 'draft',
    notes: 'National curriculum baseline.',
    createdAt: '2024-05-01T08:00:00Z',
    createdBy: 'coordinator@elsasa.sch.id',
    updatedAt: null,
    updatedBy: null,
  },
  {
    id: 'req-lvl-1',
    name: 'Grade 10 Progression Threshold',
    requirementScope: 'level',
    level: 'Grade 10',
    learningStructureId: 'struct-100',
    requirementType: 'progression',
    description: 'Must pass at least 30 credits in Grade 10 to advance to Grade 11.',
    version: '1.0',
    effectiveDate: '2024-07-01',
    endDate: null,
    status: 'draft',
    notes: 'Annual stage advancement rule.',
    createdAt: '2024-05-01T08:00:00Z',
    createdBy: 'coordinator@elsasa.sch.id',
    updatedAt: null,
    updatedBy: null,
  },
  {
    id: 'req-unit-1',
    name: 'Calculus I Prerequisite for Advanced Physics',
    requirementScope: 'learning_unit',
    learningUnitId: 'unit-phys-101',
    learningUnitCode: 'PHYS-101',
    learningUnitName: 'Fisika Lanjutan',
    learningStructureId: 'struct-100',
    requirementType: 'prerequisite',
    description: 'Must complete MAT-101 (Matematika Wajib) with minimum score 75 before enrolling.',
    version: '1.0',
    effectiveDate: '2024-07-01',
    endDate: null,
    status: 'draft',
    notes: 'Departmental prerequisite rule.',
    createdAt: '2024-05-01T08:00:00Z',
    createdBy: 'coordinator@elsasa.sch.id',
    updatedAt: null,
    updatedBy: null,
  },
];

const mockUnits: LearningUnit[] = [
  {
    id: 'unit-mat-101',
    learningStructureId: 'struct-100',
    parentUnitId: null,
    code: 'MAT-101',
    name: 'Matematika Dasar',
    unitType: 'subject',
    level: 'Grade 10',
    creditWeight: 4,
    status: 'draft',
    createdAt: '2024-05-01T08:00:00Z',
    createdBy: 'coordinator@elsasa.sch.id',
  },
  {
    id: 'unit-phys-101',
    learningStructureId: 'struct-100',
    parentUnitId: null,
    code: 'PHYS-101',
    name: 'Fisika Lanjutan',
    unitType: 'subject',
    level: 'Grade 10',
    creditWeight: 3,
    status: 'draft',
    createdAt: '2024-05-01T08:00:00Z',
    createdBy: 'coordinator@elsasa.sch.id',
  },
];

describe('Academic Requirements Grouped Tab & Modals (P6-S19)', () => {
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

    vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue(mockRequirements);
    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);
    mockToastAdd.mockClear();
  });

  // --------------------------------------------------------------------------
  // 1. AcademicRequirementsTab.vue
  // --------------------------------------------------------------------------
  describe('AcademicRequirementsTab.vue', () => {
    function createTabWrapper(props = {}) {
      const authStore = useAuthStore();
      authStore.setSession('test-token', {
        userId: 'coord-1',
        username: 'coordinator',
        fullName: 'Curriculum Coordinator',
        roles: ['AcademicCoordinator'],
        permissions: [CURRICULUM_PERMISSIONS.VIEW, CURRICULUM_PERMISSIONS.MANAGE],
        campusId: null,
      });

      return mount(AcademicRequirementsTab, {
        props: {
          structureId: 'struct-100',
          isStructureActive: false,
          ...props,
        },
        global: {
          plugins: [PrimeVue, ConfirmationService, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Tag: {
              props: ['value', 'severity'],
              template: `<span class="p-tag" :data-severity="severity">{{ value }}</span>`,
            },
          },
        },
      });
    }

    it('renders categorized requirements grouped by scope: program, level, and learning_unit', async () => {
      const wrapper = createTabWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="academic-requirements-tab"]').exists()).toBe(true);

      // Verify the 3 groups exist
      expect(wrapper.find('[data-testid="group-program-requirements"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="group-level-requirements"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="group-unit-requirements"]').exists()).toBe(true);

      // Verify requirement cards
      expect(wrapper.find('[data-testid="requirement-card-req-prog-1"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="requirement-card-req-lvl-1"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="requirement-card-req-unit-1"]').exists()).toBe(true);
    });

    it('renders requirement card attributes: Name, Type, Scope, Description, Effective Date, Status', async () => {
      const wrapper = createTabWrapper();
      await flushPromises();

      const card = wrapper.find('[data-testid="requirement-card-req-prog-1"]');
      expect(card.text()).toContain('Total Minimum Credits for Diploma');
      expect(card.text()).toContain('CREDIT');
      expect(card.text()).toContain('PROGRAM');
      expect(card.text()).toContain('Students must earn at least 120 credits');
      expect(card.text()).toContain('Effective: 2024-07-01');
      expect(card.text()).toContain('DRAFT');
    });

    it('emits addRequirement when top action button is clicked', async () => {
      const wrapper = createTabWrapper();
      await flushPromises();

      const addBtn = wrapper.find('[data-testid="btn-add-requirement"]');
      expect(addBtn.exists()).toBe(true);
      await addBtn.trigger('click');

      expect(wrapper.emitted('addRequirement')).toHaveLength(1);
    });

    it('emits editRequirement when row edit button is clicked', async () => {
      const wrapper = createTabWrapper();
      await flushPromises();

      const editBtn = wrapper.find('[data-testid="edit-req-btn-req-prog-1"]');
      expect(editBtn.exists()).toBe(true);
      await editBtn.trigger('click');

      expect(wrapper.emitted('editRequirement')).toHaveLength(1);
      expect(wrapper.emitted('editRequirement')?.[0]?.[0]).toMatchObject({
        id: 'req-prog-1',
        name: 'Total Minimum Credits for Diploma',
      });
    });

    it('renders empty state when no requirements exist for framework', async () => {
      vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue([]);
      const wrapper = createTabWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="requirements-empty"]').exists()).toBe(true);
    });
  });

  // --------------------------------------------------------------------------
  // 2. CreateRequirementModal.vue
  // --------------------------------------------------------------------------
  describe('CreateRequirementModal.vue', () => {
    function createModalWrapper(visible = true) {
      return mount(CreateRequirementModal, {
        props: {
          visible,
          structureId: 'struct-100',
        },
        global: {
          plugins: [PrimeVue, ConfirmationService, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible', 'header'],
              template: `
                <div v-if="visible" class="p-dialog" role="dialog">
                  <div class="p-dialog-header"><slot name="header">{{ header }}</slot></div>
                  <div class="p-dialog-content"><slot /></div>
                </div>
              `,
            },
            Select: {
              props: ['modelValue', 'options', 'placeholder', 'disabled', 'loading'],
              emits: ['update:modelValue'],
              template: `
                <div class="p-select" :data-loading="loading">
                  <select
                    :value="modelValue"
                    :disabled="disabled"
                    @change="$emit('update:modelValue', $event.target.value)"
                  >
                    <option v-for="opt in options" :key="opt.value" :value="opt.value">
                      {{ opt.label }}
                    </option>
                  </select>
                </div>
              `,
            },
          },
        },
      });
    }

    it('renders dialog and form inputs for drafting requirements', async () => {
      const wrapper = createModalWrapper(true);
      await flushPromises();

      expect(wrapper.find('[data-testid="create-requirement-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="req-name-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="req-scope-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="req-type-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="req-version-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="req-effective-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="req-description-input"]').exists()).toBe(true);
    });

    it('submits valid requirement payload and invokes createAcademicRequirement API', async () => {
      const createSpy = vi.spyOn(curriculumApi, 'createAcademicRequirement').mockResolvedValue({
        id: 'req-new-1',
        name: 'Minimum Passing Threshold',
        requirementScope: 'program',
        learningStructureId: 'struct-100',
        requirementType: 'assessment_policy',
        description: 'Minimum grade of 70 in all core competencies.',
        version: '1.0',
        effectiveDate: '2024-07-01',
        status: 'draft',
        createdAt: '2024-05-01T08:00:00Z',
        createdBy: 'coordinator',
      });

      const wrapper = createModalWrapper(true);
      await flushPromises();

      // Fill in required form inputs
      await wrapper.find('[data-testid="req-name-input"]').setValue('Minimum Passing Threshold');
      await wrapper.find('[data-testid="req-description-input"]').setValue(
        'Minimum grade of 70 in all core competencies.',
      );

      // Submit form
      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(createSpy).toHaveBeenCalledWith(
        'struct-100',
        expect.objectContaining({
          name: 'Minimum Passing Threshold',
          requirementScope: 'program',
          description: 'Minimum grade of 70 in all core competencies.',
        }),
      );
    });
  });

  // --------------------------------------------------------------------------
  // 3. EditRequirementModal.vue
  // --------------------------------------------------------------------------
  describe('EditRequirementModal.vue', () => {
    function createEditModalWrapper(props = {}) {
      return mount(EditRequirementModal, {
        props: {
          visible: true,
          requirement: mockRequirements[0],
          structureId: 'struct-100',
          isStructureActive: false,
          ...props,
        },
        global: {
          plugins: [PrimeVue, ConfirmationService, [VueQueryPlugin, { queryClient }]],
          stubs: {
            Dialog: {
              props: ['visible', 'header'],
              template: `
                <div v-if="visible" class="p-dialog" role="dialog">
                  <div class="p-dialog-header"><slot name="header">{{ header }}</slot></div>
                  <div class="p-dialog-content"><slot /></div>
                </div>
              `,
            },
            Tag: {
              props: ['value'],
              template: `<span class="p-tag">{{ value }}</span>`,
            },
            Select: {
              props: ['modelValue', 'options', 'placeholder', 'disabled'],
              emits: ['update:modelValue'],
              template: `
                <div class="p-select">
                  <select
                    :value="modelValue"
                    :disabled="disabled"
                    @change="$emit('update:modelValue', $event.target.value)"
                  >
                    <option v-for="opt in options" :key="opt.value" :value="opt.value">
                      {{ opt.label }}
                    </option>
                  </select>
                </div>
              `,
            },
          },
        },
      });
    }

    it('pre-populates existing requirement fields in the modal', async () => {
      const wrapper = createEditModalWrapper();
      await flushPromises();

      const nameInput = wrapper.find('[data-testid="edit-req-name-input"]')
        .element as HTMLInputElement;
      expect(nameInput.value).toBe('Total Minimum Credits for Diploma');

      const descInput = wrapper.find('[data-testid="edit-req-description-input"]')
        .element as HTMLTextAreaElement;
      expect(descInput.value).toBe(
        'Students must earn at least 120 credits across 4 years to qualify for graduation.',
      );
    });

    it('locks structural fields and displays immutability notice when structure is active', async () => {
      const wrapper = createEditModalWrapper({
        isStructureActive: true,
      });
      await flushPromises();

      // Lock banner should be visible
      expect(wrapper.find('[data-testid="requirement-locked-banner"]').exists()).toBe(true);

      // Structural fields are disabled
      const scopeSelect = wrapper.find('[data-testid="edit-req-scope-select"] select')
        .element as HTMLSelectElement;
      expect(scopeSelect.disabled).toBe(true);

      const typeSelect = wrapper.find('[data-testid="edit-req-type-select"] select')
        .element as HTMLSelectElement;
      expect(typeSelect.disabled).toBe(true);

      const versionInput = wrapper.find('[data-testid="edit-req-version-input"]')
        .element as HTMLInputElement;
      expect(versionInput.disabled).toBe(true);
    });

    it('submits updated requirement metadata calling updateAcademicRequirement API', async () => {
      const updateSpy = vi.spyOn(curriculumApi, 'updateAcademicRequirement').mockResolvedValue({
        ...mockRequirements[0],
        name: 'Updated Minimum Credits Rule',
        description: 'Updated criteria description.',
      });

      const wrapper = createEditModalWrapper({
        isStructureActive: true,
      });
      await flushPromises();

      await wrapper.find('[data-testid="edit-req-name-input"]').setValue('Updated Minimum Credits Rule');
      await wrapper.find('[data-testid="edit-req-description-input"]').setValue('Updated criteria description.');

      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(updateSpy).toHaveBeenCalledWith(
        'req-prog-1',
        expect.objectContaining({
          name: 'Updated Minimum Credits Rule',
          description: 'Updated criteria description.',
        }),
      );
    });
  });
});
