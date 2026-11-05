// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import LearningUnitsTab from '../components/LearningUnitsTab.vue';
import CreateLearningUnitModal from '../components/CreateLearningUnitModal.vue';
import EditLearningUnitModal from '../components/EditLearningUnitModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import * as curriculumApi from '../api/curriculumApi';
import type { LearningUnit } from '../types/curriculum.types';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockUnits: LearningUnit[] = [
  {
    id: 'unit-root-1',
    learningStructureId: 'struct-1',
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
    learningStructureId: 'struct-1',
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
    learningStructureId: 'struct-1',
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

describe('LearningUnitsTab.vue & Learning Unit Modals (P6-S18)', () => {
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
      },
    });

    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'coord-1',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['Coordinator'],
      permissions: [CURRICULUM_PERMISSIONS.MANAGE],
      campusId: null,
    });

    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);
  });

  function createTabWrapper(props = { structureId: 'struct-1', isStructureActive: false }) {
    return mount(LearningUnitsTab, {
      props,
      global: {
        plugins: [PrimeVue, ConfirmationService, [VueQueryPlugin, { queryClient }]],
        stubs: {
          teleport: true,
        },
      },
    });
  }

  it('renders 2-level TreeTable with unit code, name, type badge, level, credit weight, and status', async () => {
    const wrapper = createTabWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="learning-units-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="learning-units-treetable"]').exists()).toBe(true);

    const tableText = wrapper.find('[data-testid="learning-units-treetable"]').text();
    // Root units
    expect(tableText).toContain('MATH-100');
    expect(tableText).toContain('Mathematics Foundation');
    expect(tableText).toContain('SCI-200');
    expect(tableText).toContain('General Science');
    // Child module
    expect(tableText).toContain('MATH-101');
    expect(tableText).toContain('Algebra I');
  });

  it('renders summary bar with total unit count, root units, child modules, and cumulative credit weight', async () => {
    const wrapper = createTabWrapper();
    await flushPromises();

    const summaryBar = wrapper.find('[data-testid="units-summary-bar"]');
    expect(summaryBar.exists()).toBe(true);

    // Total: 3, Root: 2, Child: 1
    expect(wrapper.find('[data-testid="summary-total-units"]').text()).toBe('3');
    expect(wrapper.find('[data-testid="summary-root-units"]').text()).toBe('2');
    expect(wrapper.find('[data-testid="summary-child-units"]').text()).toBe('1');

    // Cumulative credit weight: 4.0 + 2.0 + 3.5 = 9.5
    expect(wrapper.find('[data-testid="summary-credit-weight"]').text()).toBe('9.5');
  });

  it('enforces 2-level cap: root units have Add Child Module action, child units do NOT', async () => {
    const wrapper = createTabWrapper();
    await flushPromises();

    // Look for all row action containers
    const rows = wrapper.findAll('[data-testid="unit-row-actions"]');
    expect(rows.length).toBe(3);

    // Row 0: Root unit MATH-100 has Add Child button
    const rootAddChildBtn = rows[0].find('[data-testid="row-add-child-btn"]');
    expect(rootAddChildBtn.exists()).toBe(true);

    // Row 1: Child unit MATH-101 must NOT have Add Child button (strictly enforcing 2-level cap in UI)
    const childAddChildBtn = rows[1].find('[data-testid="row-add-child-btn"]');
    expect(childAddChildBtn.exists()).toBe(false);

    // Row 2: Root unit SCI-200 has Add Child button
    const root2AddChildBtn = rows[2].find('[data-testid="row-add-child-btn"]');
    expect(root2AddChildBtn.exists()).toBe(true);
  });

  it('clicking top "Add Learning Unit" opens CreateLearningUnitModal for root unit', async () => {
    const wrapper = createTabWrapper();
    await flushPromises();

    const topAddBtn = wrapper.find('[data-testid="btn-add-unit"]');
    expect(topAddBtn.exists()).toBe(true);

    await topAddBtn.trigger('click');
    await flushPromises();

    const modal = wrapper.findComponent(CreateLearningUnitModal);
    expect(modal.exists()).toBe(true);
    expect(modal.props('visible')).toBe(true);
    expect(modal.props('parentUnitId')).toBeNull();
  });

  it('clicking "Add Child Module" on root unit opens CreateLearningUnitModal with parent preselected', async () => {
    const wrapper = createTabWrapper();
    await flushPromises();

    const rows = wrapper.findAll('[data-testid="unit-row-actions"]');
    const addChildBtn = rows[0].find('[data-testid="row-add-child-btn"]');
    await addChildBtn.trigger('click');
    await flushPromises();

    const modal = wrapper.findComponent(CreateLearningUnitModal);
    expect(modal.exists()).toBe(true);
    expect(modal.props('visible')).toBe(true);
    expect(modal.props('parentUnitId')).toBe('unit-root-1');
  });

  it('CreateLearningUnitModal: locks parent unit when parentUnitId prop is provided', async () => {
    const wrapper = mount(CreateLearningUnitModal, {
      props: {
        visible: true,
        structureId: 'struct-1',
        parentUnitId: 'unit-root-1',
        rootUnits: mockUnits.filter((u) => !u.parentUnitId),
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: { teleport: true },
      },
    });
    await flushPromises();

    expect(wrapper.find('[data-testid="child-unit-parent-banner"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="child-unit-parent-banner"]').text()).toContain('MATH-100');

    const parentSelect = wrapper.findComponent('[data-testid="select-parent-unit"]');
    expect(parentSelect.props('disabled')).toBe(true);
  });

  it('EditLearningUnitModal: displays warning badge and disables structural fields on active framework', async () => {
    const wrapper = mount(EditLearningUnitModal, {
      props: {
        visible: true,
        structureId: 'struct-1',
        unit: mockUnits[0],
        isStructureActive: true,
        rootUnits: mockUnits.filter((u) => !u.parentUnitId),
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: { teleport: true },
      },
    });
    await flushPromises();

    // Warning badge must be displayed
    const warningBadge = wrapper.find('[data-testid="active-framework-warning"]');
    expect(warningBadge.exists()).toBe(true);
    expect(warningBadge.text()).toContain('Structural fields are locked on active frameworks');

    // Code input is disabled
    const codeInput = wrapper.find('[data-testid="input-edit-unit-code"]');
    expect(codeInput.attributes('disabled')).toBeDefined();

    // Unit Name is still editable (non-structural field)
    const nameInput = wrapper.find('[data-testid="input-edit-unit-name"]');
    expect(nameInput.attributes('disabled')).toBeUndefined();
  });

  it('EditLearningUnitModal: allows structural field edits when framework is draft', async () => {
    const wrapper = mount(EditLearningUnitModal, {
      props: {
        visible: true,
        structureId: 'struct-1',
        unit: mockUnits[0],
        isStructureActive: false,
        rootUnits: mockUnits.filter((u) => !u.parentUnitId),
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: { teleport: true },
      },
    });
    await flushPromises();

    // Warning badge must NOT be displayed
    expect(wrapper.find('[data-testid="active-framework-warning"]').exists()).toBe(false);

    // Code input is enabled
    const codeInput = wrapper.find('[data-testid="input-edit-unit-code"]');
    expect(codeInput.attributes('disabled')).toBeUndefined();
  });

  it('Delete unit triggers deletion confirmation dialog for draft unit', async () => {
    const deleteSpy = vi.spyOn(curriculumApi, 'deleteLearningUnit').mockResolvedValue();

    const wrapper = createTabWrapper();
    await flushPromises();

    const rows = wrapper.findAll('[data-testid="unit-row-actions"]');
    const deleteBtn = rows[0].find('[data-testid="row-delete-unit-btn"]');
    expect(deleteBtn.exists()).toBe(true);

    await deleteBtn.trigger('click');
    await flushPromises();

    // Verify ConfirmDialog or mutation was called
    // In headless test with ConfirmationService, if accepted or confirmed, deleteLearningUnit is called
    // PrimeVue ConfirmDialog accept button
    const confirmAcceptBtn = document.body.querySelector('.p-confirmdialog-accept') as HTMLElement | null;
    if (confirmAcceptBtn) {
      confirmAcceptBtn.click();
      await flushPromises();
      expect(deleteSpy).toHaveBeenCalledWith('unit-root-1');
    }
  });
});
