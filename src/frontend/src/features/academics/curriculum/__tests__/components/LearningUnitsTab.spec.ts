// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import LearningUnitsTab from '../../components/LearningUnitsTab.vue';
import CreateLearningUnitModal from '../../components/CreateLearningUnitModal.vue';
import EditLearningUnitModal from '../../components/EditLearningUnitModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../../constants/permissions';
import * as curriculumApi from '../../api/curriculumApi';
import type { LearningUnit } from '../../types/curriculum.types';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockUnits: LearningUnit[] = [
  {
    id: 'unit-root-1',
    learningStructureId: 'struct-100',
    code: 'MATH-100',
    name: 'Mathematics Foundation',
    unitType: 'subject',
    parentUnitId: null,
    level: 'Grade 10',
    creditWeight: 4.0,
    status: 'draft',
    description: 'Core mathematics curriculum',
    notes: 'Required for all students',
    createdAt: '2024-06-01T00:00:00Z',
    createdBy: 'coord-1',
    updatedAt: null,
    updatedBy: null,
  },
  {
    id: 'unit-child-1',
    learningStructureId: 'struct-100',
    code: 'MATH-101',
    name: 'Algebra I',
    unitType: 'module',
    parentUnitId: 'unit-root-1',
    parentCode: 'MATH-100',
    parentName: 'Mathematics Foundation',
    parentUnitType: 'subject',
    level: 'Grade 10',
    creditWeight: 2.0,
    status: 'draft',
    description: 'Introductory algebra module',
    notes: null,
    createdAt: '2024-06-02T00:00:00Z',
    createdBy: 'coord-1',
    updatedAt: null,
    updatedBy: null,
  },
  {
    id: 'unit-root-2',
    learningStructureId: 'struct-100',
    code: 'SCI-200',
    name: 'General Science',
    unitType: 'course',
    parentUnitId: null,
    level: 'Grade 10',
    creditWeight: 3.5,
    status: 'draft',
    description: 'Integrated science curriculum',
    notes: null,
    createdAt: '2024-06-03T00:00:00Z',
    createdBy: 'coord-1',
    updatedAt: null,
    updatedBy: null,
  },
];

describe('LearningUnitsTab.spec.ts (Component & Hierarchy)', () => {
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

    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);
  });

  function createWrapper(props = {}) {
    return mount(LearningUnitsTab, {
      props: {
        structureId: 'struct-100',
        isStructureActive: false,
        ...props,
      },
      global: {
        plugins: [PrimeVue, ConfirmationService, [VueQueryPlugin, { queryClient }]],
        stubs: {
          CreateLearningUnitModal: true,
          EditLearningUnitModal: true,
        },
      },
    });
  }

  it('verifies rendering of 2-level hierarchy: root units display expanders, child units render under parents', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify treetable exists
    const treeTable = wrapper.find('[data-testid="learning-units-treetable"]');
    expect(treeTable.exists()).toBe(true);

    // Root unit codes rendered
    const unitCodes = wrapper.findAll('[data-testid="unit-code"]').map((el) => el.text());
    expect(unitCodes).toContain('MATH-100');
    expect(unitCodes).toContain('MATH-101');
    expect(unitCodes).toContain('SCI-200');

    // Root units have expander icons in TreeTable
    const expanderIcons = wrapper.findAll('.p-treetable-node-toggle-button');
    expect(expanderIcons.length).toBeGreaterThanOrEqual(1);

    // Verify child module has (child) marker
    const childMarker = wrapper.find('span[title="Child Module"]');
    expect(childMarker.exists()).toBe(true);
    expect(childMarker.text()).toBe('(child)');
  });

  it('verifies "Add Child Module" button is visible for root units only (child units cannot add children)', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // The dataset contains 2 root units (MATH-100, SCI-200) and 1 child module (MATH-101).
    // Root units should have the Add Child Module button; child units must not.
    const addChildButtons = wrapper.findAll('[data-testid="row-add-child-btn"]');
    expect(addChildButtons.length).toBe(2);

    // Verify actions container for rows
    const actionBars = wrapper.findAll('[data-testid="unit-row-actions"]');
    expect(actionBars.length).toBe(3);

    // The first row (root MATH-100) has row-add-child-btn
    expect(actionBars[0].find('[data-testid="row-add-child-btn"]').exists()).toBe(true);

    // The second row (child MATH-101) does NOT have row-add-child-btn
    expect(actionBars[1].find('[data-testid="row-add-child-btn"]').exists()).toBe(false);

    // The third row (root SCI-200) has row-add-child-btn
    expect(actionBars[2].find('[data-testid="row-add-child-btn"]').exists()).toBe(true);
  });

  it('verifies summary metrics bar renders total units, root units, child modules, and cumulative credit weight', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="summary-total-units"]').text()).toBe('3');
    expect(wrapper.find('[data-testid="summary-root-units"]').text()).toBe('2');
    expect(wrapper.find('[data-testid="summary-child-units"]').text()).toBe('1');
    // 4.0 + 2.0 + 3.5 = 9.5
    expect(wrapper.find('[data-testid="summary-credit-weight"]').text()).toBe('9.5');
  });

  it('verifies clicking top "Add Learning Unit" opens create modal for root unit', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    await wrapper.find('[data-testid="btn-add-unit"]').trigger('click');
    await flushPromises();

    const createModal = wrapper.findComponent(CreateLearningUnitModal);
    expect(createModal.props('visible')).toBe(true);
    expect(createModal.props('parentUnitId')).toBeNull();
  });

  it('verifies clicking "Add Child Module" on root unit opens create modal with parent preselected', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const addChildBtns = wrapper.findAll('[data-testid="row-add-child-btn"]');
    await addChildBtns[0].trigger('click');
    await flushPromises();

    const createModal = wrapper.findComponent(CreateLearningUnitModal);
    expect(createModal.props('visible')).toBe(true);
    expect(createModal.props('parentUnitId')).toBe('unit-root-1');
  });

  it('verifies draft unit deletion triggers confirmation dialog', async () => {
    const deleteSpy = vi.spyOn(curriculumApi, 'deleteLearningUnit').mockResolvedValue();

    const wrapper = createWrapper();
    await flushPromises();

    const deleteBtns = wrapper.findAll('[data-testid="row-delete-unit-btn"]');
    expect(deleteBtns.length).toBeGreaterThan(0);

    // Trigger delete on first unit
    await deleteBtns[0].trigger('click');
    await flushPromises();

    // With ConfirmationService, confirm dialog is queued
    expect(wrapper.find('.p-confirmdialog').exists() || wrapper.findComponent({ name: 'ConfirmDialog' }).exists()).toBe(true);
  });
});
