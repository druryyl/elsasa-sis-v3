// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ClassSessionWorkbenchView from '../views/ClassSessionWorkbenchView.vue';
import PlanActivityModal from '../components/PlanActivityModal.vue';
import ConductActivityModal from '../components/ConductActivityModal.vue';
import SessionAttendanceGrid from '../components/SessionAttendanceGrid.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as sessionsApi from '../api/sessions.api';
import * as participationApi from '@/features/academics/participation/api/participation.api';
import type { TeacherDailyActivity } from '../types';

const mockRouterPush = vi.fn();
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<Record<string, any>>();
  return {
    ...actual,
    useRoute: () => ({ params: {}, query: {} }),
    useRouter: () => ({ push: mockRouterPush }),
  };
});

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

function getTodayIso(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const todayIso = getTodayIso();

const mockDailyActivities: TeacherDailyActivity[] = [
  {
    id: 'act-1',
    learningUnitId: 'unit-1',
    learningUnitCode: 'MATH-101',
    learningUnitName: 'Algebra I',
    activityType: 'lecture',
    title: 'Introduction to Linear Equations',
    sectionLabel: '10-A',
    activityDate: todayIso,
    startTime: '08:00',
    endTime: '09:30',
    status: 'planned',
    teacherName: 'Teacher Jane',
    isAttendanceRequired: true,
    deliveryNotes: null,
    attendanceSummary: null,
  },
  {
    id: 'act-2',
    learningUnitId: 'unit-hr',
    learningUnitCode: 'HR-10',
    learningUnitName: 'Homeroom Grade 10',
    activityType: 'homeroom_session',
    title: 'Daily Homeroom Roll-Call',
    sectionLabel: '10-A',
    activityDate: todayIso,
    startTime: '07:30',
    endTime: '08:00',
    status: 'conducted',
    teacherName: 'Homeroom Advisor',
    isAttendanceRequired: true,
    deliveryNotes: 'Morning attendance recorded and announcements given.',
    attendanceSummary: {
      totalRecorded: 25,
      presentCount: 23,
      absentCount: 1,
      lateCount: 1,
      excusedCount: 0,
    },
  },
];

const mockCohortParticipants = [
  {
    id: 'part-1',
    studentId: 'student-1',
    studentNumber: 'STD-1001',
    studentName: 'Ali Reza',
    learningUnitId: 'unit-1',
    academicPeriodId: 'ay-2026',
    sectionLabel: '10-A',
    attemptNumber: 1,
    status: 'in_progress',
    participationType: 'standard',
    source: 'assigned',
    startDate: todayIso,
    completionDate: null,
    endDate: null,
    sourceInstitution: null,
    externalGradeOrCredit: null,
    notes: null,
    createdAt: '2026-07-01T00:00:00Z',
    updatedAt: null,
  },
  {
    id: 'part-2',
    studentId: 'student-2',
    studentNumber: 'STD-1002',
    studentName: 'Bima Sakti',
    learningUnitId: 'unit-1',
    academicPeriodId: 'ay-2026',
    sectionLabel: '10-A',
    attemptNumber: 1,
    status: 'in_progress',
    participationType: 'standard',
    source: 'assigned',
    startDate: todayIso,
    completionDate: null,
    endDate: null,
    sourceInstitution: null,
    externalGradeOrCredit: null,
    notes: null,
    createdAt: '2026-07-01T00:00:00Z',
    updatedAt: null,
  },
];

describe('ClassSessionWorkbenchView.spec.ts (SCR-ACD-01)', () => {
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

    vi.spyOn(sessionsApi, 'getTeacherDailyActivities').mockResolvedValue(mockDailyActivities);
    vi.spyOn(sessionsApi, 'getActivityAttendance').mockResolvedValue([]);
    vi.spyOn(sessionsApi, 'recordSessionAttendance').mockResolvedValue({
      activityId: 'act-1',
      totalRecorded: 2,
      totalPresent: 2,
      totalAbsent: 0,
      totalLate: 0,
      totalExcused: 0,
      totalPartial: 0,
      recordedDate: todayIso,
      recordedBy: 'teacher',
    });
    vi.spyOn(participationApi, 'getParticipationsByUnit').mockResolvedValue(mockCohortParticipants as any);

    const authStore = useAuthStore();
    authStore.setSession('token-teacher', {
      userId: 'teacher-1',
      username: 'teacher',
      fullName: 'Teacher User',
      roles: ['Teacher'],
      permissions: [
        'academics.activities.view',
        'academics.activities.conduct',
        'academics.activities.manage',
        'academics.attendance.record',
        'academics.attendance.view',
      ],
      campusId: null,
    });
  });

  function createWrapper() {
    return mount(ClassSessionWorkbenchView, {
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
          PlanActivityModal: {
            props: ['visible', 'initialDate'],
            emits: ['update:visible', 'planned'],
            template: '<div v-if="visible" data-testid="mock-plan-activity-modal"><slot /></div>',
          },
          ConductActivityModal: {
            props: ['visible', 'activity'],
            emits: ['update:visible', 'conducted'],
            template: '<div v-if="visible" data-testid="mock-conduct-activity-modal"><slot /></div>',
          },
        },
      },
    });
  }

  it('renders daily activity cards with title, unit code, time, teacher, and status', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify main view rendered
    expect(wrapper.text()).toContain('Class Session Workbench');
    expect(wrapper.text()).toContain('Introduction to Linear Equations');
    expect(wrapper.text()).toContain('MATH-101 — Algebra I');
    expect(wrapper.text()).toContain('Teacher Jane');
    expect(wrapper.text()).toContain('08:00 – 09:30');
    expect(wrapper.text()).toContain('Section 10-A');

    // Verify homeroom session card display
    expect(wrapper.text()).toContain('Daily Homeroom Roll-Call');
    expect(wrapper.text()).toContain('HR-10 — Homeroom Grade 10');
    expect(wrapper.text()).toContain('Homeroom Session');
    expect(wrapper.text()).toContain('Morning attendance recorded and announcements given.');
  });

  it('opens delivery logging modal when Conduct Session is clicked', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Find "Conduct Session" button for the planned activity
    const conductButtons = wrapper.findAll('button').filter((b) => b.text().includes('Conduct Session'));
    expect(conductButtons.length).toBeGreaterThan(0);

    await conductButtons[0].trigger('click');
    await flushPromises();

    const conductModal = wrapper.findComponent(ConductActivityModal);
    expect(conductModal.props('visible')).toBe(true);
    expect(conductModal.props('activity')?.title).toBe('Introduction to Linear Equations');
  });

  it('opens roll-call panel and handles attendance grid status clicks', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Find "Take Attendance" button for the first activity
    const takeAttendanceButtons = wrapper.findAll('button').filter((b) => b.text().includes('Take Attendance'));
    expect(takeAttendanceButtons.length).toBeGreaterThan(0);

    await takeAttendanceButtons[0].trigger('click');
    await flushPromises();

    // Verify active attendance session panel opens
    expect(wrapper.text()).toContain('ROLL-CALL ATTENDANCE WORKBENCH');
    const attendanceGrid = wrapper.findComponent(SessionAttendanceGrid);
    expect(attendanceGrid.exists()).toBe(true);
    expect(attendanceGrid.props('activity').id).toBe('act-1');

    // Verify cohort participants appear in roster
    expect(attendanceGrid.text()).toContain('Ali Reza');
    expect(attendanceGrid.text()).toContain('Bima Sakti');

    // Click "Absent" button on student Ali Reza
    const absentButtons = attendanceGrid.findAll('button').filter((b) => b.text().trim() === 'Absent');
    expect(absentButtons.length).toBeGreaterThan(0);
    await absentButtons[0].trigger('click');
    await flushPromises();

    // Verify Save button is visible and click it
    const saveButton = attendanceGrid.findAll('button').find((b) => b.text().includes('Save'));
    expect(saveButton).toBeDefined();
    await saveButton!.trigger('click');
    await flushPromises();

    // Verify recordSessionAttendance API was called with absent status for Ali Reza
    expect(sessionsApi.recordSessionAttendance).toHaveBeenCalledWith(
      'act-1',
      expect.objectContaining({
        entries: expect.arrayContaining([
          expect.objectContaining({ studentId: 'student-1', status: 'absent' }),
        ]),
      }),
    );
  });

  it('navigates dates using Next Day, Previous Day, and Today buttons', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const initialDateInput = wrapper.find('input[type="date"]');
    expect(initialDateInput.exists()).toBe(true);
    const initialDate = (initialDateInput.element as HTMLInputElement).value;

    // Click Next Day button
    const nextDayBtn = wrapper.find('button[title="Next Day"]');
    expect(nextDayBtn.exists()).toBe(true);
    await nextDayBtn.trigger('click');
    await flushPromises();

    const nextDate = (wrapper.find('input[type="date"]').element as HTMLInputElement).value;
    expect(nextDate).not.toBe(initialDate);

    // Click Today button
    const todayBtn = wrapper.findAll('button').find((b) => b.text().trim() === 'Today');
    expect(todayBtn).toBeDefined();
    await todayBtn!.trigger('click');
    await flushPromises();

    const resetDate = (wrapper.find('input[type="date"]').element as HTMLInputElement).value;
    expect(resetDate).toBe(initialDate);
  });
});
