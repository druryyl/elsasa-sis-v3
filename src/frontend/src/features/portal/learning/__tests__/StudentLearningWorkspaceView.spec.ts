// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import StudentLearningWorkspaceView from '../views/StudentLearningWorkspaceView.vue';
import CourseworkSubmissionModal from '../components/CourseworkSubmissionModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as learningApi from '../api/learning.api';
import type { StudentCourseworkTask, SubmitCourseworkResponse } from '../types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

const mockTasks: StudentCourseworkTask[] = [
  {
    assignmentId: 'assign-due-soon',
    title: 'Calculus Problem Set 1',
    description: 'Derivative exercises 1-20',
    learningUnitId: 'unit-math',
    learningUnitCode: 'MATH-201',
    learningUnitName: 'Advanced Calculus',
    learningActivityId: 'act-1',
    learningActivityTitle: 'Derivatives Lecture',
    sectionLabel: '12-A',
    releaseDate: '2026-11-01',
    dueDate: '2026-11-08',
    countdownDays: 3,
    displayStatus: 'pending',
    submissionId: null,
    isResubmission: false,
    submissionReference: null,
    submittedDate: null,
  },
  {
    assignmentId: 'assign-today',
    title: 'Physics Lab Report',
    description: 'Kinematics lab report',
    learningUnitId: 'unit-phys',
    learningUnitCode: 'PHYS-101',
    learningUnitName: 'General Physics',
    learningActivityId: 'act-2',
    learningActivityTitle: 'Lab Session 1',
    sectionLabel: '12-A',
    releaseDate: '2026-10-25',
    dueDate: '2026-11-05',
    countdownDays: 0,
    displayStatus: 'pending',
    submissionId: null,
    isResubmission: false,
    submissionReference: null,
    submittedDate: null,
  },
  {
    assignmentId: 'assign-overdue',
    title: 'Chemistry Essay',
    description: 'Periodic table historical essay',
    learningUnitId: 'unit-chem',
    learningUnitCode: 'CHEM-101',
    learningUnitName: 'Introductory Chemistry',
    learningActivityId: 'act-3',
    learningActivityTitle: 'Historical Seminar',
    sectionLabel: '12-A',
    releaseDate: '2026-10-20',
    dueDate: '2026-11-01',
    countdownDays: -4,
    displayStatus: 'missing',
    submissionId: null,
    isResubmission: false,
    submissionReference: null,
    submittedDate: null,
  },
  {
    assignmentId: 'assign-submitted',
    title: 'Literature Reading Journal',
    description: 'Weekly reflection notes',
    learningUnitId: 'unit-lit',
    learningUnitCode: 'LIT-101',
    learningUnitName: 'World Literature',
    learningActivityId: 'act-4',
    learningActivityTitle: 'Reading Circle',
    sectionLabel: '12-A',
    releaseDate: '2026-10-15',
    dueDate: '2026-11-10',
    countdownDays: 5,
    displayStatus: 'submitted',
    submissionId: 'sub-lit-1',
    isResubmission: false,
    submissionReference: 'https://docs.google.com/document/d/lit-journal',
    submittedDate: '2026-11-02T10:00:00Z',
  },
];

const mockSubmitResponse: SubmitCourseworkResponse = {
  id: 'sub-new-1',
  assignmentId: 'assign-due-soon',
  studentId: 'student-1',
  submittedDate: '2026-11-05T12:00:00Z',
  status: 'submitted',
  submissionReference: 'https://github.com/student/calculus-hw',
  isResubmission: false,
  previousSubmissionId: null,
  notes: 'Done',
};

describe('StudentLearningWorkspaceView.spec.ts (SCR-SELF-03)', () => {
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

    vi.spyOn(learningApi, 'getStudentCourseworkWorkspace').mockImplementation(async (filter) => {
      if (filter === 'due_soon') {
        return mockTasks.filter((t) => t.countdownDays !== null && t.countdownDays >= 0 && t.countdownDays <= 7 && t.displayStatus === 'pending');
      }
      if (filter === 'submitted') {
        return mockTasks.filter((t) => t.displayStatus === 'submitted');
      }
      if (filter === 'missing') {
        return mockTasks.filter((t) => t.displayStatus === 'missing');
      }
      if (filter === 'upcoming') {
        return mockTasks.filter((t) => t.displayStatus === 'pending');
      }
      return mockTasks;
    });

    vi.spyOn(learningApi, 'submitCoursework').mockResolvedValue(mockSubmitResponse);

    const authStore = useAuthStore();
    authStore.setSession('token-student', {
      userId: 'student-1',
      username: 'student.faiz',
      fullName: 'Ahmad Faiz',
      roles: ['Student'],
      permissions: ['academics.submissions.submit'],
      campusId: null,
    });
  });

  function createWrapper() {
    return mount(StudentLearningWorkspaceView, {
      props: {
        studentId: 'student-1',
      },
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
            props: ['visible'],
            emits: ['update:visible'],
            template: `
              <div v-if="visible" class="p-dialog" data-testid="mock-submission-dialog">
                <slot name="header" />
                <slot />
                <slot name="footer" />
              </div>
            `,
          },
          FileUpload: {
            props: ['mode', 'name', 'url', 'maxFileSize', 'accept'],
            emits: ['uploader', 'select'],
            template: `
              <div class="p-fileupload" data-testid="prime-file-upload">
                <slot name="empty" />
              </div>
            `,
          },
          SubmissionHistoryDrawer: {
            props: ['visible', 'task'],
            emits: ['update:visible', 'resubmit'],
            template: '<div v-if="visible" data-testid="mock-history-drawer"><slot /></div>',
          },
        },
      },
    });
  }

  it('renders workspace metrics bar and assignment task cards', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify view container
    expect(wrapper.find('[data-testid="student-learning-workspace-view"]').exists()).toBe(true);

    // Verify summary metric cards
    const metricsBar = wrapper.find('[data-testid="workspace-metrics-bar"]');
    expect(metricsBar.exists()).toBe(true);
    expect(metricsBar.text()).toContain('Due Soon (7 Days)');
    expect(metricsBar.text()).toContain('Missing Tasks');
    expect(metricsBar.text()).toContain('Completed / Submitted');
    expect(metricsBar.text()).toContain('Active Assignments');

    // Verify rendered task cards
    expect(wrapper.text()).toContain('Calculus Problem Set 1');
    expect(wrapper.text()).toContain('MATH-201');
    expect(wrapper.text()).toContain('Physics Lab Report');
    expect(wrapper.text()).toContain('PHYS-101');
  });

  it('displays accurate color-coded deadline countdown badges', async () => {
    // Mount with 'all' filter so all test tasks are rendered
    vi.spyOn(learningApi, 'getStudentCourseworkWorkspace').mockResolvedValue(mockTasks);

    const wrapper = createWrapper();
    await flushPromises();

    // Task 1: countdownDays = 3 -> "Due in 3 days"
    const badge1 = wrapper.find('[data-testid="countdown-badge-assign-due-soon"]');
    expect(badge1.exists()).toBe(true);
    expect(badge1.text()).toBe('Due in 3 days');

    // Task 2: countdownDays = 0 -> "Due today"
    const badge2 = wrapper.find('[data-testid="countdown-badge-assign-today"]');
    expect(badge2.exists()).toBe(true);
    expect(badge2.text()).toBe('Due today');

    // Task 3: countdownDays = -4 -> "Overdue by 4 days"
    const badge3 = wrapper.find('[data-testid="countdown-badge-assign-overdue"]');
    expect(badge3.exists()).toBe(true);
    expect(badge3.text()).toBe('Overdue by 4 days');
  });

  it('filters worklist tasks using filter tabs', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify tabs rendered
    expect(wrapper.find('[data-testid="tab-upcoming"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-due-soon"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-submitted"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-missing"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-all"]').exists()).toBe(true);

    // Click Missing tab
    const missingTab = wrapper.find('[data-testid="tab-missing"]');
    await missingTab.trigger('click');
    await flushPromises();

    // In missing tab, Chemistry Essay is shown
    expect(learningApi.getStudentCourseworkWorkspace).toHaveBeenCalledWith('missing', 'student-1');
  });

  it('filters tasks via search input field', async () => {
    vi.spyOn(learningApi, 'getStudentCourseworkWorkspace').mockResolvedValue(mockTasks);

    const wrapper = createWrapper();
    await flushPromises();

    const searchInput = wrapper.find('[data-testid="input-task-search"]');
    expect(searchInput.exists()).toBe(true);

    // Search for "Calculus"
    await searchInput.setValue('Calculus');
    await flushPromises();

    expect(wrapper.text()).toContain('Calculus Problem Set 1');
    expect(wrapper.text()).not.toContain('Physics Lab Report');

    // Clear search
    await searchInput.setValue('');
    await flushPromises();
    expect(wrapper.text()).toContain('Physics Lab Report');
  });

  it('launches submission modal with file upload dropzone and submits deliverable', async () => {
    vi.spyOn(learningApi, 'getStudentCourseworkWorkspace').mockResolvedValue(mockTasks);

    const wrapper = createWrapper();
    await flushPromises();

    // Click "Submit Work" on Calculus task
    const submitBtn = wrapper.find('[data-testid="btn-submit-work-assign-due-soon"]');
    expect(submitBtn.exists()).toBe(true);
    await submitBtn.trigger('click');
    await flushPromises();

    // Submission modal opens
    const modal = wrapper.findComponent(CourseworkSubmissionModal);
    expect(modal.props('visible')).toBe(true);
    expect(modal.props('task')?.assignmentId).toBe('assign-due-soon');

    // Switch to File Upload submission method
    const fileMethodBtn = wrapper.find('[data-testid="select-method-file"]');
    expect(fileMethodBtn.exists()).toBe(true);
    await fileMethodBtn.trigger('click');
    await flushPromises();

    // File upload dropzone is displayed
    const dropzone = wrapper.find('[data-testid="prime-file-upload"]');
    expect(dropzone.exists()).toBe(true);
    expect(dropzone.text()).toContain('Drag and drop coursework file here, or click to browse');

    // Switch to URL link method and enter valid link
    const linkMethodBtn = wrapper.find('[data-testid="select-method-link"]');
    await linkMethodBtn.trigger('click');
    await flushPromises();

    const linkInput = wrapper.find('[data-testid="input-submission-link"]');
    await linkInput.setValue('https://github.com/student/calculus-hw');

    // Click Submit Coursework button
    const submitActionBtn = wrapper.find('[data-testid="btn-submit-coursework"]');
    expect(submitActionBtn.exists()).toBe(true);
    await submitActionBtn.trigger('click');
    await flushPromises();

    // Verify submitCoursework API called
    expect(learningApi.submitCoursework).toHaveBeenCalledWith(
      'assign-due-soon',
      expect.objectContaining({
        studentId: 'student-1',
        submissionReference: 'https://github.com/student/calculus-hw',
      }),
    );

    // Verify toast notification dispatched
    expect(mockToastAdd).toHaveBeenCalledWith(
      expect.objectContaining({
        severity: 'success',
        summary: 'Deliverable Received',
      }),
    );
  });
});
