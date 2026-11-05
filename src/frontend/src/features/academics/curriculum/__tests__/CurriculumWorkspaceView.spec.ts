// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import CurriculumWorkspaceView from '../views/CurriculumWorkspaceView.vue';
import CurriculumContextBar from '../components/CurriculumContextBar.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import * as academicYearApi from '@/features/operations/calendar/api/academicYear.api';
import * as curriculumApi from '../api/curriculumApi';
import type { LearningStructure, LearningStructureSummary } from '../types/curriculum.types';

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

const mockStructures: LearningStructureSummary[] = [
  {
    id: 'struct-1',
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
  },
  {
    id: 'struct-2',
    name: 'Kurikulum Merdeka SD 2024 Draft',
    academicPeriodId: 'ay-2024-2025',
    academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
    version: '2024.2-draft',
    status: 'draft',
    effectiveDate: '2024-07-01',
    endDate: null,
    supersedesId: 'struct-1',
    learningUnitsCount: 2,
    academicRequirementsCount: 1,
    createdAt: '2024-06-15T00:00:00Z',
    updatedAt: null,
  },
];

const mockStructureDetail: LearningStructure = {
  id: 'struct-1',
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
  notes: 'Approved without amendments.',
  createdAt: '2024-06-01T00:00:00Z',
  createdBy: 'coord-user',
  updatedAt: null,
  updatedBy: null,
};

describe('CurriculumWorkspaceView.vue (SCR-ACD-03 Master View & Context Bar)', () => {
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
      items: mockStructures,
      totalCount: mockStructures.length,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });
    vi.spyOn(curriculumApi, 'getLearningStructureById').mockResolvedValue(mockStructureDetail);
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

  it('renders header, context bar, and selects first period and active structure automatically', async () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-1',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['AcademicCoordinator'],
      permissions: [CURRICULUM_PERMISSIONS.VIEW, CURRICULUM_PERMISSIONS.MANAGE, CURRICULUM_PERMISSIONS.SUBMIT],
      campusId: null,
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Verify context bar and header
    expect(wrapper.find('[data-testid="curriculum-workspace-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="curriculum-context-bar"]').exists()).toBe(true);

    // Verify badges (version pill and status badge)
    const versionPill = wrapper.find('[data-testid="version-pill"]');
    expect(versionPill.exists()).toBe(true);
    expect(versionPill.text()).toBe('v2024.1');

    const statusBadge = wrapper.find('[data-testid="status-badge"]');
    expect(statusBadge.exists()).toBe(true);
    expect(statusBadge.text()).toBe('ACTIVE');

    // Verify master tabs exist
    expect(wrapper.find('[data-testid="tab-overview"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-units"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-requirements"]').exists()).toBe(true);
  });

  it('enforces coordinator vs authority button visibility & states', async () => {
    const authStore = useAuthStore();
    // Coordinator session
    authStore.setSession('test-token', {
      userId: 'coord-1',
      username: 'coordinator',
      fullName: 'Coordinator User',
      roles: ['Coordinator'],
      permissions: [CURRICULUM_PERMISSIONS.MANAGE, CURRICULUM_PERMISSIONS.SUBMIT],
      campusId: null,
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Coordinator actions are visible
    expect(wrapper.find('[data-testid="btn-new-structure"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-edit-metadata"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-submit-approval"]').exists()).toBe(true);

    // Authority Approve button should NOT be visible for coordinator without approve permission
    expect(wrapper.find('[data-testid="btn-approve-activate"]').exists()).toBe(false);
    // Archive button should NOT be visible without archive permission
    expect(wrapper.find('[data-testid="btn-archive-structure"]').exists()).toBe(false);

    // Now switch session to Academic Authority
    authStore.setSession('authority-token', {
      userId: 'auth-1',
      username: 'authority',
      fullName: 'Academic Authority',
      roles: ['Authority'],
      permissions: [CURRICULUM_PERMISSIONS.APPROVE, CURRICULUM_PERMISSIONS.ARCHIVE],
      campusId: null,
    });

    await flushPromises();

    // Authority buttons now visible
    expect(wrapper.find('[data-testid="btn-approve-activate"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-archive-structure"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-supersede-structure"]').exists()).toBe(true);

    // Active structure: Submit button is disabled (can only submit drafts)
    // Supersede button is enabled because structure is active
    const supersedeBtn = wrapper.find('[data-testid="btn-supersede-structure"]');
    expect(supersedeBtn.attributes('disabled')).toBeUndefined();
  });

  it('renders empty state when no structures exist for period', async () => {
    vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      totalPages: 0,
    });

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="workspace-no-structures-empty"]').exists()).toBe(true);
  });
});
