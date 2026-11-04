// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import CurriculumWorkspaceView from '../../views/CurriculumWorkspaceView.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../../constants/permissions';
import * as academicYearApi from '@/features/operations/calendar/api/academicYear.api';
import * as curriculumApi from '../../api/curriculumApi';
import type { LearningStructure, LearningStructureSummary } from '../../types/curriculum.types';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockAcademicYears: academicYearApi.AcademicYearDto[] = [
  {
    id: 'ay-2024-2025',
    code: 'AY-2024-2025',
    name: 'Tahun Pelajaran 2024/2025',
    startDate: '2024-07-01',
    endDate: '2025-06-30',
    status: 'active',
    isActive: true,
  },
  {
    id: 'ay-2025-2026',
    code: 'AY-2025-2026',
    name: 'Tahun Pelajaran 2025/2026',
    startDate: '2025-07-01',
    endDate: '2026-06-30',
    status: 'draft',
    isActive: false,
  },
];

const mockActiveStructureSummary: LearningStructureSummary = {
  id: 'struct-active-1',
  name: 'Kurikulum Merdeka SD 2024',
  academicPeriodId: 'ay-2024-2025',
  academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
  version: '2024.1',
  status: 'active',
  effectiveDate: '2024-07-01',
  endDate: null,
  supersedesId: null,
  learningUnitsCount: 5,
  academicRequirementsCount: 2,
  createdAt: '2024-06-01T00:00:00Z',
  updatedAt: null,
};

const mockDraftStructureSummary: LearningStructureSummary = {
  id: 'struct-draft-2',
  name: 'Kurikulum Merdeka SD 2024 Draft',
  academicPeriodId: 'ay-2024-2025',
  academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
  version: '2024.2-draft',
  status: 'draft',
  effectiveDate: '2024-07-01',
  endDate: null,
  supersedesId: 'struct-active-1',
  learningUnitsCount: 2,
  academicRequirementsCount: 1,
  createdAt: '2024-06-15T00:00:00Z',
  updatedAt: null,
};

const mockActiveStructureDetail: LearningStructure = {
  id: 'struct-active-1',
  name: 'Kurikulum Merdeka SD 2024',
  description: 'Primary curriculum framework for academic year 2024/2025',
  academicPeriodId: 'ay-2024-2025',
  academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
  version: '2024.1',
  effectiveDate: '2024-07-01',
  endDate: null,
  status: 'active',
  approvedBy: 'authority-user',
  approvedDate: '2024-06-20',
  supersedesId: null,
  notes: 'Approved framework.',
  createdAt: '2024-06-01T00:00:00Z',
  createdBy: 'coord-user',
  updatedAt: null,
  updatedBy: null,
};

const mockDraftStructureDetail: LearningStructure = {
  id: 'struct-draft-2',
  name: 'Kurikulum Merdeka SD 2024 Draft',
  description: 'Draft revision of primary curriculum framework',
  academicPeriodId: 'ay-2024-2025',
  academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
  version: '2024.2-draft',
  effectiveDate: '2024-07-01',
  endDate: null,
  status: 'draft',
  approvedBy: null,
  approvedDate: null,
  supersedesId: 'struct-active-1',
  predecessorName: 'Kurikulum Merdeka SD 2024',
  predecessorVersion: '2024.1',
  notes: 'Working draft.',
  createdAt: '2024-06-15T00:00:00Z',
  createdBy: 'coord-user',
  updatedAt: null,
  updatedBy: null,
};

describe('CurriculumWorkspaceView.spec.ts (Integration)', () => {
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
        queries: {
          retry: false,
          gcTime: 0,
        },
      },
    });

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockAcademicYears);
    vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue({
      items: [mockActiveStructureSummary, mockDraftStructureSummary],
      totalCount: 2,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });
    vi.spyOn(curriculumApi, 'getLearningStructureById').mockImplementation(async (id: string) => {
      if (id === 'struct-draft-2') return mockDraftStructureDetail;
      return mockActiveStructureDetail;
    });
    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue([]);
    vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue([]);
  });

  function createWrapper() {
    return mount(CurriculumWorkspaceView, {
      global: {
        plugins: [
          PrimeVue,
          ConfirmationService,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
          Select: {
            props: ['modelValue', 'options', 'placeholder', 'loading', 'disabled'],
            emits: ['update:modelValue'],
            template: `
              <div class="p-select" :data-loading="loading">
                <select
                  :value="modelValue"
                  @change="$emit('update:modelValue', $event.target.value)"
                  :disabled="disabled"
                >
                  <option v-for="opt in options" :key="opt.id" :value="opt.id">{{ opt.label }}</option>
                </select>
              </div>
            `,
          },
          Tag: {
            props: ['value', 'severity'],
            template: `<span class="p-tag" :data-severity="severity">{{ value }}</span>`,
          },
        },
      },
    });
  }

  it('verifies rendering of context bar, period selector, structure selector, version badge, and status pill', async () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-coord',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['AcademicCoordinator'],
      permissions: [
        CURRICULUM_PERMISSIONS.VIEW,
        CURRICULUM_PERMISSIONS.MANAGE,
        CURRICULUM_PERMISSIONS.SUBMIT,
      ],
      campusId: null,
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Verify context bar container
    expect(wrapper.find('[data-testid="curriculum-context-bar"]').exists()).toBe(true);

    // Verify period selector and structure selector
    expect(wrapper.find('[data-testid="academic-period-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="learning-structure-select"]').exists()).toBe(true);

    // Verify version badge and status pill
    const versionPill = wrapper.find('[data-testid="version-pill"]');
    expect(versionPill.exists()).toBe(true);
    expect(versionPill.text()).toBe('v2024.1');

    const statusBadge = wrapper.find('[data-testid="status-badge"]');
    expect(statusBadge.exists()).toBe(true);
    expect(statusBadge.text()).toBe('ACTIVE');
  });

  it('verifies action buttons visibility based on framework status (draft vs active) and user roles', async () => {
    const authStore = useAuthStore();

    // 1. Role: Academic Coordinator on Active Framework
    authStore.setSession('coord-token', {
      userId: 'coord-1',
      username: 'coordinator',
      fullName: 'Coordinator User',
      roles: ['AcademicCoordinator'],
      permissions: [CURRICULUM_PERMISSIONS.VIEW, CURRICULUM_PERMISSIONS.MANAGE, CURRICULUM_PERMISSIONS.SUBMIT],
      campusId: null,
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Coordinator actions
    expect(wrapper.find('[data-testid="btn-new-structure"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-edit-metadata"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-submit-approval"]').exists()).toBe(true);

    // Active status: Submit is disabled (cannot submit active framework)
    const submitBtn = wrapper.find('[data-testid="btn-submit-approval"]');
    expect(submitBtn.attributes('disabled')).toBeDefined();

    // Coordinator does NOT have approve or archive permissions
    expect(wrapper.find('[data-testid="btn-approve-activate"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="btn-archive-structure"]').exists()).toBe(false);

    // 2. Switch to Academic Authority Role
    authStore.setSession('auth-token', {
      userId: 'auth-1',
      username: 'authority',
      fullName: 'Academic Authority',
      roles: ['AcademicAuthority'],
      permissions: [
        CURRICULUM_PERMISSIONS.VIEW,
        CURRICULUM_PERMISSIONS.APPROVE,
        CURRICULUM_PERMISSIONS.ARCHIVE,
      ],
      campusId: null,
    });
    await flushPromises();

    // Authority buttons visible
    expect(wrapper.find('[data-testid="btn-approve-activate"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-archive-structure"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-supersede-structure"]').exists()).toBe(true);

    // In active status: Supersede is enabled; Approve is disabled (already active)
    expect(wrapper.find('[data-testid="btn-supersede-structure"]').attributes('disabled')).toBeUndefined();
    expect(wrapper.find('[data-testid="btn-approve-activate"]').attributes('disabled')).toBeDefined();

    // 3. Switch framework to DRAFT status
    const structureSelect = wrapper.find('[data-testid="learning-structure-select"] select');
    await structureSelect.setValue('struct-draft-2');
    await flushPromises();

    // In draft status: Approve is enabled; Supersede is disabled (only active can be superseded)
    expect(wrapper.find('[data-testid="btn-approve-activate"]').attributes('disabled')).toBeUndefined();
    expect(wrapper.find('[data-testid="btn-supersede-structure"]').attributes('disabled')).toBeDefined();
  });

  it('verifies tab switching between Overview, Learning Units, and Academic Requirements', async () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-1',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['AcademicCoordinator'],
      permissions: [CURRICULUM_PERMISSIONS.VIEW, CURRICULUM_PERMISSIONS.MANAGE],
      campusId: null,
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Verify all 3 master tabs exist
    const tabOverview = wrapper.find('[data-testid="tab-overview"]');
    const tabUnits = wrapper.find('[data-testid="tab-units"]');
    const tabRequirements = wrapper.find('[data-testid="tab-requirements"]');

    expect(tabOverview.exists()).toBe(true);
    expect(tabUnits.exists()).toBe(true);
    expect(tabRequirements.exists()).toBe(true);

    // Initial tab is overview
    expect(wrapper.find('[data-testid="panel-overview"]').exists()).toBe(true);

    // Switch to Learning Units tab
    await tabUnits.trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="panel-units"]').exists()).toBe(true);

    // Switch to Academic Requirements tab
    await tabRequirements.trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="panel-requirements"]').exists()).toBe(true);

    // Switch back to Overview tab
    await tabOverview.trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="panel-overview"]').exists()).toBe(true);
  });
});
