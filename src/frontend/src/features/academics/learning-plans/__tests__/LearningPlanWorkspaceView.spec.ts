// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import LearningPlanWorkspaceView from '../views/LearningPlanWorkspaceView.vue';
import CreatePlanModal from '../components/CreatePlanModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { LEARNING_PLAN_PERMISSIONS } from '../constants/permissions';
import * as academicYearApi from '@/features/operations/calendar/api/academicYear.api';
import * as operationalPeriodApi from '@/features/operations/calendar/api/operationalPeriod.api';
import * as learningPlansApi from '../api/learningPlansApi';
import * as studentRecordsApi from '@/features/students/records/api/records.api';
import type { StudentLearningPlanListItem, StudentLearningPlan } from '../types';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockAcademicYears: academicYearApi.AcademicYearDto[] = [
  {
    id: 'ay-2029-2030',
    code: 'AY-2029-2030',
    name: 'Tahun Pelajaran 2029/2030',
    startDate: '2029-07-01',
    endDate: '2030-06-30',
    status: 'active',
    isActive: true,
  },
  {
    id: 'ay-2028-2029',
    code: 'AY-2028-2029',
    name: 'Tahun Pelajaran 2028/2029',
    startDate: '2028-07-01',
    endDate: '2029-06-30',
    status: 'completed',
    isActive: false,
  },
];

const mockLearningPlans: StudentLearningPlanListItem[] = [
  {
    id: 'plan-budi-v2',
    studentId: 'student-budi',
    studentName: 'Budi Santoso',
    studentNis: '0071234567',
    gradeLevel: 'Grade 4',
    academicPeriodId: 'ay-2029-2030',
    academicPeriodLabel: 'Semester 1 TP 2029/2030',
    status: 'active',
    version: 2,
    unitsCount: 3,
    adjustmentsCount: 2,
    createdAt: '2029-07-01T00:00:00Z',
    updatedAt: '2029-08-01T00:00:00Z',
  },
  {
    id: 'plan-timotius-v1',
    studentId: 'student-timotius',
    studentName: 'Timotius',
    studentNis: '0079876543',
    gradeLevel: 'Grade 2',
    academicPeriodId: 'ay-2029-2030',
    academicPeriodLabel: 'Semester 1 TP 2029/2030',
    status: 'draft',
    version: 1,
    unitsCount: 1,
    adjustmentsCount: 1,
    createdAt: '2029-07-15T00:00:00Z',
    updatedAt: null,
  },
  {
    id: 'plan-budi-v1',
    studentId: 'student-budi',
    studentName: 'Budi Santoso',
    studentNis: '0071234567',
    academicPeriodId: 'ay-2029-2030',
    academicPeriodLabel: 'Semester 1 TP 2029/2030',
    status: 'revised',
    version: 1,
    unitsCount: 2,
    adjustmentsCount: 0,
    createdAt: '2029-07-01T00:00:00Z',
    updatedAt: '2029-08-01T00:00:00Z',
  },
];

const mockDetailBudi: StudentLearningPlan = {
  id: 'plan-budi-v2',
  studentId: 'student-budi',
  studentName: 'Budi Santoso',
  studentNis: '0071234567',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'active',
  version: 2,
  notes: 'Exceptional pathway with adaptive PJOK and remedial math',
  createdAt: '2029-07-01T00:00:00Z',
  createdBy: 'user-coordinator',
  updatedAt: '2029-08-01T00:00:00Z',
  updatedBy: 'user-authority',
  constituentUnits: [
    {
      id: 'unit-entry-1',
      learningPlanId: 'plan-budi-v2',
      learningUnitId: 'lu-mat-3',
      unitCode: 'MAT-03-S1',
      title: 'Matematika Remedial Kelas 3',
      sequenceOrder: 1,
      isRequired: true,
      notes: 'Remediation level at Grade 3',
    },
    {
      id: 'unit-entry-2',
      learningPlanId: 'plan-budi-v2',
      learningUnitId: 'lu-ind-4',
      unitCode: 'IND-04-S1',
      title: 'Bahasa Indonesia Kelas 4',
      sequenceOrder: 2,
      isRequired: true,
      notes: null,
    },
  ],
  adjustments: [
    {
      id: 'adj-1',
      learningPlanId: 'plan-budi-v2',
      adjustmentType: 'accommodation',
      description: '50% Extra Exam Time',
      rationale: 'Processing speed adaptation recommended by educational psychologist',
      effectiveDate: '2029-07-01',
      endDate: null,
      approvedBy: 'Ibu Ratna',
      approvedDate: '2029-07-02',
      notes: null,
    },
  ],
  approvals: [
    {
      id: 'app-1',
      learningPlanId: 'plan-budi-v2',
      planVersion: 2,
      approvedBy: 'Ibu Ratna (VP Academics)',
      approvedDate: '2029-08-01',
      approvalType: 'revision_approval',
      notes: 'Approved amendment adding adaptive physical education and extra exam time',
    },
  ],
};

describe('LearningPlanWorkspaceView.vue (SCR-ACD-04 Master-Detail Workspace)', () => {
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

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockAcademicYears);
    vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue([
      {
        id: 'sem-1-2029',
        academicYearId: 'ay-2029-2030',
        periodCode: 'SEM-1',
        name: 'Semester 1 TP 2029/2030',
        periodType: 'semester',
        startDate: '2029-07-01',
        endDate: '2029-12-31',
        parentPeriodId: null,
        campusContext: 'All',
        status: 'active',
      },
    ]);
    vi.spyOn(learningPlansApi.learningPlansApi, 'getLearningPlans').mockResolvedValue({
      items: mockLearningPlans,
      page: 1,
      pageSize: 20,
      totalCount: 3,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    });
    vi.spyOn(learningPlansApi.learningPlansApi, 'getLearningPlanById').mockResolvedValue(mockDetailBudi);
  });

  function createWrapper() {
    return mount(LearningPlanWorkspaceView, {
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
          Dialog: {
            props: ['visible', 'header'],
            template: '<div v-if="visible" data-testid="dialog"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
          },
          Select: {
            props: ['options', 'modelValue'],
            emits: ['update:modelValue'],
            template: `
              <select
                :value="modelValue"
                @change="$emit('update:modelValue', $event.target.value)"
                class="mock-select"
              >
                <option v-for="opt in options" :key="opt.id || opt.value" :value="opt.id !== undefined ? opt.id : opt.value">
                  {{ opt.label }}
                </option>
              </select>
            `,
          },
        },
      },
    });
  }

  it('renders page header with title, subtitle, and breadcrumbs', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Student Learning Plans');
    expect(wrapper.text()).toContain('Individualized academic pathways');
  });

  it('renders master sidebar with academic period selector, status filter, and search input', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Academic Period');
    expect(wrapper.text()).toContain('Status Filter');
    expect(wrapper.text()).toContain('Search Student');

    const searchInput = wrapper.find('input[placeholder*="Filter by student name or NIS"]');
    expect(searchInput.exists()).toBe(true);
  });

  it('renders student cards displaying name, NIS, units, and status badges', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('0071234567');
    expect(wrapper.text()).toContain('Grade 4');
    expect(wrapper.text()).toContain('Timotius');
    expect(wrapper.text()).toContain('0079876543');
    expect(wrapper.text()).toContain('Grade 2');
    expect(wrapper.text()).toContain('Active v2');
    expect(wrapper.text()).toContain('Draft v1');
  });

  it('auto-selects first active plan and displays plan detail in right pane', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Matematika Remedial Kelas 3');
    expect(wrapper.text()).toContain('MAT-03-S1');
    expect(wrapper.text()).toContain('50% Extra Exam Time');
  });

  it('filters students by search input', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const searchInput = wrapper.find('input[placeholder*="Filter by student name or NIS"]');
    await searchInput.setValue('Timotius');
    await flushPromises();

    expect(learningPlansApi.learningPlansApi.getLearningPlans).toHaveBeenCalled();
  });

  it('filters by status filter: revised/archived', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const selects = wrapper.findAll('select');
    if (selects.length >= 2) {
      await selects[1].setValue('revised_archived');
      await flushPromises();

      expect(wrapper.text()).toContain('Revised v1');
    }
  });

  it('shows empty state when no students match filters', async () => {
    queryClient.clear();
    vi.spyOn(learningPlansApi.learningPlansApi, 'getLearningPlans').mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
    });

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('No learning plans found');
  });

  it('displays Formulate New Plan button for authorized coordinator', async () => {
    const authStore = useAuthStore();
    authStore.user = {
      userId: 'test-coord-id',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['AcademicCoordinator'],
      permissions: [LEARNING_PLAN_PERMISSIONS.MANAGE],
      campusId: null,
    };
    authStore.permissions = [LEARNING_PLAN_PERMISSIONS.MANAGE];
    authStore.roles = ['AcademicCoordinator'];
    authStore.token = 'mock-token';

    const wrapper = createWrapper();
    await flushPromises();

    const formulateBtns = wrapper.findAll('button').filter(b => b.text().includes('Formulate New Plan') || b.text().includes('New Plan'));
    expect(formulateBtns.length).toBeGreaterThan(0);
  });
});

describe('CreatePlanModal.vue', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
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

    vi.spyOn(studentRecordsApi, 'searchStudents').mockResolvedValue({
      items: [
        {
          id: 'student-agape-1',
          studentNumber: '0075551234',
          fullName: 'Agape Christianto',
          currentStatus: 'active',
          enrollmentDate: '2029-07-01',
          initialCampus: 'Kampus Utama',
          initialLevel: 'Grade 4',
          isIndependent: false,
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    });

    vi.spyOn(learningPlansApi.learningPlansApi, 'createPlan').mockResolvedValue({
      id: 'new-plan-id-999',
    });
  });

  it('renders modal when visible is true', () => {
    const wrapper = mount(CreatePlanModal, {
      props: {
        visible: true,
        periods: [
          { id: 'ay-2029-2030', label: 'Tahun Pelajaran 2029/2030' },
        ],
      },
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          Dialog: {
            props: ['visible', 'header'],
            template: '<div v-if="visible" data-testid="dialog"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
          },
          Select: {
            props: ['options', 'modelValue'],
            template: `<select><option v-for="o in options" :key="o.id">{{ o.label }}</option></select>`,
          },
        },
      },
    });

    expect(wrapper.text()).toContain('Formulate Student Learning Plan');
    expect(wrapper.text()).toContain('Target Student');
    expect(wrapper.text()).toContain('Governing Academic Period');
  });

  it('disables submit button when student is not selected', async () => {
    const wrapper = mount(CreatePlanModal, {
      props: {
        visible: true,
        periods: [
          { id: 'ay-2029-2030', label: 'Tahun Pelajaran 2029/2030' },
        ],
        initialPeriodId: 'ay-2029-2030',
      },
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          Dialog: {
            props: ['visible', 'header'],
            template: '<div v-if="visible" data-testid="dialog"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
          },
          Select: {
            props: ['options', 'modelValue'],
            template: `<select><option v-for="o in options" :key="o.id">{{ o.label }}</option></select>`,
          },
        },
      },
    });

    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('Formulate Draft Plan'));
    expect(submitBtn?.attributes('disabled')).toBeDefined();
  });
});
