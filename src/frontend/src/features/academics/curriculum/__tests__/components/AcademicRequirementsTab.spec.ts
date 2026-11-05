// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import AcademicRequirementsTab from '../../components/AcademicRequirementsTab.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../../constants/permissions';
import * as curriculumApi from '../../api/curriculumApi';
import type { AcademicRequirement } from '../../types/curriculum.types';

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
    description: 'Must complete MAT-101 with minimum score 75 before enrolling.',
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

describe('AcademicRequirementsTab.spec.ts (Grouped Requirements)', () => {
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

    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'coord-1',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['AcademicCoordinator'],
      permissions: [CURRICULUM_PERMISSIONS.VIEW, CURRICULUM_PERMISSIONS.MANAGE],
      campusId: null,
    });

    vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue(mockRequirements);
  });

  function createWrapper(props = {}) {
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

  it('verifies grouping into Program, Level, and Unit sections', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify presence of all three architectural scope containers
    const programGroup = wrapper.find('[data-testid="group-program-requirements"]');
    const levelGroup = wrapper.find('[data-testid="group-level-requirements"]');
    const unitGroup = wrapper.find('[data-testid="group-unit-requirements"]');

    expect(programGroup.exists()).toBe(true);
    expect(levelGroup.exists()).toBe(true);
    expect(unitGroup.exists()).toBe(true);

    // Verify headers contain correct titles and counts
    expect(programGroup.text()).toContain('Program-Wide Requirements (1)');
    expect(levelGroup.text()).toContain('Level-Specific Requirements (1)');
    expect(unitGroup.text()).toContain('Learning Unit Prerequisites & Rules (1)');

    // Verify card rendered in respective scope containers
    expect(programGroup.find('[data-testid="requirement-card-req-prog-1"]').exists()).toBe(true);
    expect(levelGroup.find('[data-testid="requirement-card-req-lvl-1"]').exists()).toBe(true);
    expect(unitGroup.find('[data-testid="requirement-card-req-unit-1"]').exists()).toBe(true);
  });

  it('verifies requirement card renders metadata, badges, and criteria description', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const progCard = wrapper.find('[data-testid="requirement-card-req-prog-1"]');
    expect(progCard.exists()).toBe(true);

    // Card title
    expect(progCard.text()).toContain('Total Minimum Credits for Diploma');

    // Type and scope tags
    expect(progCard.text()).toContain('PROGRAM');
    expect(progCard.text()).toContain('CREDIT');
    expect(progCard.text()).toContain('v1.0');
    expect(progCard.text()).toContain('DRAFT');

    // Description
    expect(progCard.text()).toContain('Students must earn at least 120 credits across 4 years');

    // Effective date
    expect(progCard.text()).toContain('Effective: 2024-07-01');
  });

  it('verifies clicking "Add Academic Requirement" emits addRequirement', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const addBtn = wrapper.find('[data-testid="btn-add-requirement"]');
    expect(addBtn.exists()).toBe(true);
    await addBtn.trigger('click');

    expect(wrapper.emitted('addRequirement')).toBeTruthy();
  });

  it('verifies clicking edit button emits editRequirement with target requirement', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const editBtn = wrapper.find('[data-testid="edit-req-btn-req-prog-1"]');
    expect(editBtn.exists()).toBe(true);
    await editBtn.trigger('click');

    expect(wrapper.emitted('editRequirement')).toBeTruthy();
    expect(wrapper.emitted('editRequirement')?.[0]).toEqual([mockRequirements[0]]);
  });

  it('verifies empty state when no requirements exist for framework', async () => {
    vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue([]);

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="requirements-empty"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No Academic Requirements Defined');
  });
});
