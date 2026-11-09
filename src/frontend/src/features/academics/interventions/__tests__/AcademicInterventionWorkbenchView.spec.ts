// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { ref } from 'vue';
import AcademicInterventionWorkbenchView from '../views/AcademicInterventionWorkbenchView.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import * as interventionComposables from '../composables/useAcademicInterventions';
import type { AcademicIntervention, AtRiskLearner } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

// Mock child modal & drawer components to spy on props and events
const MockInterventionPlanModal = {
  name: 'InterventionPlanModal',
  props: ['visible', 'initialData', 'activePeriodId'],
  emits: ['update:visible', 'created'],
  template: `
    <div v-if="visible" data-testid="mock-plan-modal">
      <span data-testid="modal-student-name">{{ initialData?.studentName || 'New Referral' }}</span>
      <span data-testid="modal-trigger-type">{{ initialData?.triggerType }}</span>
      <button data-testid="btn-modal-close" @click="$emit('update:visible', false)">Close</button>
      <button data-testid="btn-modal-submit" @click="$emit('created', { id: 'int-new', status: 'planned' })">Submit</button>
    </div>
  `,
};

const MockInterventionDetailDrawer = {
  name: 'InterventionDetailDrawer',
  props: ['visible', 'intervention', 'canUpdateStatus'],
  emits: ['update:visible', 'updated'],
  template: `
    <div v-if="visible" data-testid="mock-detail-drawer">
      <span data-testid="drawer-intervention-id">{{ intervention?.id }}</span>
      <span data-testid="drawer-student-name">{{ intervention?.studentName }}</span>
      <button data-testid="btn-drawer-close" @click="$emit('update:visible', false)">Close</button>
    </div>
  `,
};

const mockInterventions: AcademicIntervention[] = [
  {
    id: 'int-001',
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentIdentifier: 'STD-4002',
    learningUnitId: 'unit-math-04',
    learningUnitName: 'Grade 4 Mathematics',
    learningUnitCode: 'MATH-04',
    academicPeriodId: '2026-ganjil',
    interventionType: 'remediation',
    triggerType: 'assessment_below_threshold',
    triggerDescription: 'Score 58.0 falls below passing threshold 65.0',
    planDescription: 'Targeted fractions and multiplication remediation package.',
    status: 'in_progress',
    initiatedBy: 'Academic Coordinator',
    initiatedAt: '2026-11-01T08:00:00Z',
    concludedAt: null,
  },
  {
    id: 'int-002',
    studentId: 'stu-rina',
    studentName: 'Rina Kusuma',
    studentIdentifier: 'STD-4001',
    learningUnitId: 'unit-math-04',
    learningUnitName: 'Grade 4 Mathematics',
    learningUnitCode: 'MATH-04',
    academicPeriodId: '2026-ganjil',
    interventionType: 'extension_work',
    triggerType: 'teacher_referral',
    triggerDescription: 'Candidate exhibits exceptional aptitude; Olympiad preparation assigned.',
    planDescription: 'Independent advanced problem-solving modules.',
    status: 'planned',
    initiatedBy: 'Teacher User',
    initiatedAt: '2026-11-03T10:00:00Z',
    concludedAt: null,
  },
];

const mockAtRiskLearners: AtRiskLearner[] = [
  {
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentIdentifier: 'STD-4002',
    gradeLevel: 'Grade 4',
    learningUnitId: 'unit-math-04',
    learningUnitName: 'Grade 4 Mathematics',
    learningUnitCode: 'MATH-04',
    triggerType: 'assessment_below_threshold',
    triggerDescription: 'Operative assessment score 58.0 < 65.0 passing score',
    currentScore: 58.0,
    passingScore: 65.0,
    attendancePercentage: 88.0,
    missingAssignmentsCount: 0,
    currentStanding: 'conditional_standing',
    suggestedInterventionType: 'remediation',
  },
  {
    studentId: 'stu-timotius',
    studentName: 'Timotius',
    studentIdentifier: 'STD-4003',
    gradeLevel: 'Grade 4',
    learningUnitId: 'unit-math-04',
    learningUnitName: 'Grade 4 Mathematics',
    learningUnitCode: 'MATH-04',
    triggerType: 'attendance_concern',
    triggerDescription: 'Attendance presence rate 68.0% < 75.0% threshold',
    currentScore: 62.0,
    passingScore: 65.0,
    attendancePercentage: 68.0,
    missingAssignmentsCount: 3,
    currentStanding: 'not_in_good_standing',
    suggestedInterventionType: 'remedial_instruction',
  },
];

describe('AcademicInterventionWorkbenchView.vue (SCR-ACD-06, ACD-WF-10, UC-ACD-11)', () => {
  let authStore: ReturnType<typeof useAuthStore>;
  let appContextStore: ReturnType<typeof useAppContextStore>;
  let mockRefetchInterventions: ReturnType<typeof vi.fn>;
  let mockRefetchAtRisk: ReturnType<typeof vi.fn>;

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
    vi.clearAllMocks();

    mockRefetchInterventions = vi.fn();
    mockRefetchAtRisk = vi.fn();

    authStore = useAuthStore();
    authStore.setSession('mock-token', {
      id: 'coord-1',
      username: 'coordinator',
      email: 'coord@school.test',
      fullName: 'Academic Coordinator',
      isActive: true,
      roles: ['AcademicCoordinator'],
      permissions: [
        'academics.interventions.view',
        'academics.interventions.initiate',
        'academics.interventions.update_status',
      ],
      campusId: null,
    });

    appContextStore = useAppContextStore();
    appContextStore.activeAcademicPeriodId = '2026-ganjil';

    vi.spyOn(interventionComposables, 'useActiveInterventionsQuery').mockReturnValue({
      data: ref(mockInterventions),
      isLoading: ref(false),
      isFetching: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: mockRefetchInterventions,
    } as any);

    vi.spyOn(interventionComposables, 'useAtRiskLearnersQuery').mockReturnValue({
      data: ref(mockAtRiskLearners),
      isLoading: ref(false),
      isFetching: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: mockRefetchAtRisk,
    } as any);
  });

  function createWrapper() {
    return mount(AcademicInterventionWorkbenchView, {
      global: {
        plugins: [PrimeVue, ToastService],
        stubs: {
          'router-link': true,
          InterventionPlanModal: MockInterventionPlanModal,
          InterventionDetailDrawer: MockInterventionDetailDrawer,
        },
      },
    });
  }

  it('renders workbench title, tab list, and initial work queue metrics', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Academic Intervention Workbench');
    expect(wrapper.find('[data-testid="interventions-tabs"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-active-interventions"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-at-risk-learners"]').exists()).toBe(true);

    // Initial tab is active interventions
    expect(wrapper.find('[data-testid="panel-active-interventions"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="table-active-interventions"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('Rina Kusuma');
  });

  it('switches tabs to At-Risk Learners and displays dynamic early-warning metrics', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Switch to At-Risk Learners tab
    const tabAtRisk = wrapper.find('[data-testid="tab-at-risk-learners"]');
    await tabAtRisk.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="panel-at-risk-learners"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="table-at-risk-learners"]').exists()).toBe(true);

    // Check at-risk roster rows & metrics
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('Timotius');
    expect(wrapper.text()).toContain('Score: 58 / 65');
    expect(wrapper.text()).toContain('Att: 68%');
    expect(wrapper.text()).toContain('Missing: 3 tasks');
    expect(wrapper.text()).toContain('Standing: not_in_good_standing');
  });

  it('triggers intervention formulation modal when selecting an at-risk learner row', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Switch to At-Risk tab
    await wrapper.find('[data-testid="tab-at-risk-learners"]').trigger('click');
    await flushPromises();

    // Click 1-click "Initiate Intervention" on Budi Santoso's row
    const initiateBtn = wrapper.find('[data-testid="btn-initiate-stu-budi"]');
    expect(initiateBtn.exists()).toBe(true);
    await initiateBtn.trigger('click');
    await flushPromises();

    // Verify modal is open with pre-populated data for Budi
    const modal = wrapper.find('[data-testid="mock-plan-modal"]');
    expect(modal.exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-student-name"]').text()).toBe('Budi Santoso');
    expect(wrapper.find('[data-testid="modal-trigger-type"]').text()).toBe('assessment_below_threshold');

    // Submitting formulation closes modal and refreshes queries
    await wrapper.find('[data-testid="btn-modal-submit"]').trigger('click');
    await flushPromises();

    expect(mockRefetchInterventions).toHaveBeenCalled();
    expect(mockRefetchAtRisk).toHaveBeenCalled();
  });

  it('triggers intervention formulation modal with teacher referral defaults from header action', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const referralBtn = wrapper.find('[data-testid="btn-new-referral"]');
    expect(referralBtn.exists()).toBe(true);
    await referralBtn.trigger('click');
    await flushPromises();

    const modal = wrapper.find('[data-testid="mock-plan-modal"]');
    expect(modal.exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-student-name"]').text()).toBe('New Referral');
    expect(wrapper.find('[data-testid="modal-trigger-type"]').text()).toBe('teacher_referral');
  });

  it('triggers intervention detail drawer when Inspect / Update is clicked on active intervention', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Click Inspect / Update on first active intervention (Budi Santoso)
    const inspectBtn = wrapper.find('[data-testid="btn-inspect-int-001"]');
    expect(inspectBtn.exists()).toBe(true);
    await inspectBtn.trigger('click');
    await flushPromises();

    const drawer = wrapper.find('[data-testid="mock-detail-drawer"]');
    expect(drawer.exists()).toBe(true);
    expect(wrapper.find('[data-testid="drawer-intervention-id"]').text()).toBe('int-001');
    expect(wrapper.find('[data-testid="drawer-student-name"]').text()).toBe('Budi Santoso');
  });

  it('refreshes both active interventions and at-risk queries when Refresh is clicked', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const refreshBtn = wrapper.find('[data-testid="btn-refresh-interventions"]');
    expect(refreshBtn.exists()).toBe(true);
    await refreshBtn.trigger('click');
    await flushPromises();

    expect(mockRefetchInterventions).toHaveBeenCalled();
    expect(mockRefetchAtRisk).toHaveBeenCalled();
  });
});
