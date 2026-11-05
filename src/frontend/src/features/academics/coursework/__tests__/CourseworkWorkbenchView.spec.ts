// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import CourseworkWorkbenchView from '../views/CourseworkWorkbenchView.vue';
import GrantExceptionModal from '../components/GrantExceptionModal.vue';
import CreateAssignmentDrawer from '../components/CreateAssignmentDrawer.vue';
import SubmissionDetailDrawer from '../components/SubmissionDetailDrawer.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { COURSEWORK_PERMISSIONS } from '../constants/permissions';
import * as curriculumApi from '@/features/academics/curriculum/api/curriculumApi';
import * as courseworkApi from '../api/coursework.api';
import type { Assignment, AssignmentSubmission } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

const mockStructures = [
  {
    id: 'struct-1',
    name: 'Kurikulum Merdeka 2024',
    academicPeriodId: 'ay-2024-2025',
    academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
    version: '2024.1',
    status: 'active',
    effectiveDate: '2024-07-01',
    endDate: null,
    supersedesId: null,
    learningUnitsCount: 2,
    academicRequirementsCount: 0,
    createdAt: '2024-07-01T00:00:00Z',
    updatedAt: null,
  },
];

const mockUnits = [
  {
    id: 'unit-1',
    learningStructureId: 'struct-1',
    code: 'MATH-101',
    name: 'Algebra I',
    unitType: 'subject',
    parentUnitId: null,
    level: 'Grade 10',
    creditWeight: 4.0,
    status: 'active',
    description: 'Foundational algebra',
    notes: null,
    createdAt: '2024-07-01T00:00:00Z',
    createdBy: 'coord-1',
    updatedAt: null,
    updatedBy: null,
  },
];

const mockAssignments: Assignment[] = [
  {
    id: 'assign-1',
    learningActivityId: 'act-1',
    learningActivityTitle: 'Linear Equations Lesson 1',
    learningActivityType: 'lecture',
    learningActivityDate: '2026-11-01',
    title: 'Homework 1: Solving Systems',
    description: 'Complete problems 1 through 10 in workbook.',
    releaseDate: '2026-11-01',
    dueDate: '2026-11-04',
    status: 'released',
    notes: null,
    createdAt: '2026-11-01T08:00:00Z',
    createdBy: 'teacher-1',
    updatedAt: null,
    updatedBy: null,
  },
  {
    id: 'assign-2',
    learningActivityId: 'act-1',
    learningActivityTitle: 'Linear Equations Lesson 1',
    learningActivityType: 'lecture',
    learningActivityDate: '2026-11-01',
    title: 'Homework 2: Quadratic Equations (Draft)',
    description: 'Draft assignment for next week.',
    releaseDate: '2026-11-10',
    dueDate: '2026-11-15',
    status: 'draft',
    notes: null,
    createdAt: '2026-11-01T08:00:00Z',
    createdBy: 'teacher-1',
    updatedAt: null,
    updatedBy: null,
  },
];

const mockSubmissions: AssignmentSubmission[] = [
  {
    studentId: 'student-1',
    studentNumber: 'STD-1001',
    studentName: 'Ali Reza',
    sectionLabel: '10-A',
    submissionId: null,
    status: null,
    displayStatus: 'missing',
    submissionReference: null,
    submittedDate: null,
    isResubmission: false,
    notes: null,
  },
  {
    studentId: 'student-2',
    studentNumber: 'STD-1002',
    studentName: 'Bima Sakti',
    sectionLabel: '10-A',
    submissionId: 'sub-2',
    status: 'submitted',
    displayStatus: 'submitted',
    submissionReference: 'https://docs.google.com/document/d/xyz',
    submittedDate: '2026-11-03T14:30:00Z',
    isResubmission: false,
    notes: 'Submitted early',
  },
  {
    studentId: 'student-3',
    studentNumber: 'STD-1003',
    studentName: 'Citra Dewi',
    sectionLabel: '10-A',
    submissionId: 'sub-3',
    status: 'excused',
    displayStatus: 'excused',
    submissionReference: null,
    submittedDate: null,
    isResubmission: false,
    notes: 'Medical exemption granted by coordinator',
  },
];

describe('CourseworkWorkbenchView.spec.ts (SCR-ACD-02)', () => {
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

    vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue({
      items: mockStructures,
      totalCount: 1,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });
    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);
    vi.spyOn(courseworkApi, 'getAssignmentsByUnit').mockResolvedValue(mockAssignments);
    vi.spyOn(courseworkApi, 'getAssignmentSubmissions').mockResolvedValue(mockSubmissions);
    vi.spyOn(courseworkApi, 'releaseAssignment').mockResolvedValue({
      ...mockAssignments[1],
      status: 'released',
    });
    vi.spyOn(courseworkApi, 'closeAssignment').mockResolvedValue({
      ...mockAssignments[0],
      status: 'closed',
    });

    const authStore = useAuthStore();
    authStore.setSession('token-teacher', {
      userId: 'teacher-1',
      username: 'teacher',
      fullName: 'Teacher Jane',
      roles: ['Teacher'],
      permissions: [
        COURSEWORK_PERMISSIONS.ASSIGNMENTS_VIEW,
        COURSEWORK_PERMISSIONS.ASSIGNMENTS_MANAGE,
        COURSEWORK_PERMISSIONS.ASSIGNMENTS_RELEASE,
        COURSEWORK_PERMISSIONS.ASSIGNMENTS_CLOSE,
        COURSEWORK_PERMISSIONS.SUBMISSIONS_VIEW,
        COURSEWORK_PERMISSIONS.SUBMISSIONS_EXCEPTION,
      ],
      campusId: null,
    });
  });

  function createWrapper() {
    return mount(CourseworkWorkbenchView, {
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
          Dialog: {
            props: ['visible', 'header'],
            emits: ['update:visible'],
            template: `
              <div v-if="visible" class="p-dialog" data-testid="p-dialog">
                <div class="p-dialog-header">{{ header }}</div>
                <slot />
                <slot name="footer" />
              </div>
            `,
          },
          Select: {
            props: ['modelValue', 'options'],
            emits: ['update:modelValue'],
            template: `
              <select
                :value="modelValue"
                @change="$emit('update:modelValue', $event.target.value)"
              >
                <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
              </select>
            `,
          },
          CreateAssignmentDrawer: {
            props: ['visible', 'unitId', 'unitName'],
            emits: ['update:visible', 'created'],
            template: '<div v-if="visible" data-testid="mock-create-drawer"><slot /></div>',
          },
          SubmissionDetailDrawer: {
            props: ['visible', 'submission', 'assignmentTitle'],
            emits: ['update:visible', 'grantException'],
            template: '<div v-if="visible" data-testid="mock-detail-drawer"><slot /></div>',
          },
          GrantExceptionModal: {
            props: ['visible', 'assignmentId', 'assignmentTitle', 'students', 'initialStudentId'],
            emits: ['update:visible', 'saved'],
            template: '<div v-if="visible" data-testid="mock-exception-modal"><slot /></div>',
          },
        },
      },
    });
  }

  it('renders learning units, assignments list, and selected assignment detail', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify main workbench
    expect(wrapper.find('[data-testid="coursework-workbench-view"]').exists()).toBe(true);

    // Verify learning unit rendered
    expect(wrapper.text()).toContain('MATH-101');
    expect(wrapper.text()).toContain('Algebra I');

    // Verify assignments rendered in middle pane
    expect(wrapper.text()).toContain('Homework 1: Solving Systems');
    expect(wrapper.text()).toContain('Homework 2: Quadratic Equations (Draft)');

    // Verify selected assignment header in right pane
    const header = wrapper.find('[data-testid="selected-assignment-header"]');
    expect(header.exists()).toBe(true);
    expect(header.text()).toContain('Homework 1: Solving Systems');
    expect(header.text()).toContain('Activity: Linear Equations Lesson 1');
  });

  it('verifies submission roster displays dynamic missing status badge in red', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify summary metrics bar in SubmissionsReviewTable
    expect(wrapper.text()).toContain('Total Roster');
    expect(wrapper.text()).toContain('Missing (Overdue)');

    // Table should contain student names
    expect(wrapper.text()).toContain('Ali Reza');
    expect(wrapper.text()).toContain('Bima Sakti');
    expect(wrapper.text()).toContain('Citra Dewi');

    // Verify dynamic missing badge rendered for overdue unsubmitted student Ali Reza
    const missingBadge = wrapper.findAll('.status-pill--rose, [data-severity="rose"], .bg-rose-50');
    expect(missingBadge.length).toBeGreaterThan(0);
    expect(wrapper.text()).toContain('Missing');
    expect(wrapper.text()).toContain('Submitted');
    expect(wrapper.text()).toContain('Excused');
  });

  it('launches Grant Exception modal from action buttons', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Click "Grant Exception" in assignment header
    const grantExceptionBtn = wrapper.find('[data-testid="assignment-grant-exception-btn"]');
    expect(grantExceptionBtn.exists()).toBe(true);
    await grantExceptionBtn.trigger('click');
    await flushPromises();

    const modal = wrapper.findComponent(GrantExceptionModal);
    expect(modal.props('visible')).toBe(true);
    expect(modal.props('assignmentId')).toBe('assign-1');
  });

  it('opens Create Assignment drawer when Create Assignment is clicked', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const createBtn = wrapper.find('[data-testid="header-create-assignment-btn"]');
    expect(createBtn.exists()).toBe(true);
    await createBtn.trigger('click');
    await flushPromises();

    const drawer = wrapper.findComponent(CreateAssignmentDrawer);
    expect(drawer.props('visible')).toBe(true);
    expect(drawer.props('unitId')).toBe('unit-1');
  });

  it('switches assignment selection and manages Release and Close confirmation dialogs', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // assign-1 is released -> "Close" button is present in header
    const closeBtn = wrapper.find('[data-testid="header-close-btn"]');
    expect(closeBtn.exists()).toBe(true);
    await closeBtn.trigger('click');
    await flushPromises();

    // Close confirmation dialog should be open
    expect(wrapper.text()).toContain('Close Assignment');
    expect(wrapper.text()).toContain('Closing will lock the assignment against further standard submissions');

    // Click cancel on close dialog
    const cancelBtn = wrapper.findAll('button').find((b) => b.text().trim() === 'Cancel');
    if (cancelBtn) {
      await cancelBtn.trigger('click');
      await flushPromises();
    }

    // Now select assign-2 (which is draft)
    const cardAssign2 = wrapper.find('[data-testid="assignment-card-assign-2"]');
    expect(cardAssign2.exists()).toBe(true);
    await cardAssign2.trigger('click');
    await flushPromises();

    // Header now shows "Release" button
    const releaseBtn = wrapper.find('[data-testid="header-release-btn"]');
    expect(releaseBtn.exists()).toBe(true);
    await releaseBtn.trigger('click');
    await flushPromises();

    // Release dialog open
    expect(wrapper.text()).toContain('Release Assignment to Students');
    expect(wrapper.text()).toContain('Once released, enrolled students can view the assignment');
  });
});
