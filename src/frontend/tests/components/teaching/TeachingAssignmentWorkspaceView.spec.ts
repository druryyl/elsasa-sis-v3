// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import type * as VueRouter from 'vue-router';
import TeachingAssignmentWorkspaceView from '@/features/teaching/assignments/views/TeachingAssignmentWorkspaceView.vue';
import * as assignmentsApi from '@/features/teaching/assignments/api/assignmentsApi';
import { useAuthStore } from '@/app/stores/auth.store';
import type {
  TeachingDemandDto,
  TeachingAssignmentDto,
  TeacherEligibilityEvaluationDto,
  CapacityConflictDto,
  TeachingSubstitutionDto,
  TeachingReassignmentDto,
  AssignmentCompletionDto,
  AssignmentRoleDto,
} from '@/features/teaching/assignments/types';

// Mock vue-router
const mockRouteQuery = ref<Record<string, string>>({
  tab: 'demands',
  periodId: '00000000-0000-0000-0002-000000000002',
  campus: 'all',
});
const mockReplace = vi.fn().mockImplementation((target: { query?: Record<string, string> }) => {
  if (target.query) {
    mockRouteQuery.value = { ...target.query };
  }
});

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof VueRouter>();
  return {
    ...actual,
    useRoute: () => ({
      query: mockRouteQuery.value,
    }),
    useRouter: () => ({
      replace: mockReplace,
    }),
  };
});

const commonStubs = {
  RouterLink: {
    props: ['to'],
    template: '<a :href="typeof to === \'string\' ? to : \'#\'"><slot /></a>',
  },
  Dialog: {
    props: ['visible', 'header'],
    emits: ['update:visible'],
    template: `
      <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid']">
        <div class="p-dialog-header">{{ header }}</div>
        <div class="p-dialog-content"><slot /></div>
        <div class="p-dialog-footer"><slot name="footer" /></div>
      </div>
    `,
  },
  Drawer: {
    props: ['visible'],
    emits: ['update:visible'],
    template: `
      <div v-if="visible" class="p-drawer" :data-testid="$attrs['data-testid']">
        <div class="p-drawer-header"><slot name="header" /></div>
        <div class="p-drawer-content"><slot /></div>
        <div class="p-drawer-footer"><slot name="footer" /></div>
      </div>
    `,
  },
};

const sampleRoles: AssignmentRoleDto[] = [
  {
    id: '00000000-0000-0000-0004-000000000001',
    code: 'LEAD_TEACHER',
    name: 'Guru Utama',
    isLeadRole: true,
    countsTowardWorkload: true,
    isActive: true,
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0004-000000000002',
    code: 'CO_TEACHER',
    name: 'Guru Pendamping',
    isLeadRole: false,
    countsTowardWorkload: true,
    isActive: true,
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0004-000000000005',
    code: 'HOMEROOM_ADVISOR',
    name: 'Wali Kelas',
    isLeadRole: true,
    countsTowardWorkload: true,
    isActive: true,
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleDemands: TeachingDemandDto[] = [
  {
    id: '00000000-0000-0000-0003-000000000001',
    demandCode: 'DMD-2027-001',
    sourceDomain: 'Academics',
    sourceWorkReference: '00000000-0000-0000-0001-000000000001',
    sourceWorkLabel: 'Matematika SD Kelas 4A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    requiredCapabilityCode: 'CAP-MATH-PRI',
    requiredAssignmentRoleId: '00000000-0000-0000-0004-000000000001',
    requiredAssignmentRoleName: 'Guru Utama',
    expectedWorkloadHours: 5.0,
    workloadUnit: 'Teaching Hours',
    requiresAssessmentAuthority: true,
    requestedStartDate: '2027-01-05',
    requestedEndDate: '2027-06-25',
    campusScope: 'Kampus Utama',
    requiredTeachersCount: 1,
    status: 'accepted_for_staffing',
    requestedBy: 'coordinator@hagios.sch.id',
    requestedAt: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0003-000000000002',
    demandCode: 'DMD-2027-002',
    sourceDomain: 'Academics',
    sourceWorkReference: '00000000-0000-0000-0001-000000000002',
    sourceWorkLabel: 'Bahasa Indonesia SD Kelas 4A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    requiredCapabilityCode: 'CAP-INDO-PRI',
    requiredAssignmentRoleId: '00000000-0000-0000-0004-000000000001',
    requiredAssignmentRoleName: 'Guru Utama',
    expectedWorkloadHours: 4.0,
    workloadUnit: 'Teaching Hours',
    requiresAssessmentAuthority: true,
    requestedStartDate: '2027-01-05',
    requestedEndDate: '2027-06-25',
    campusScope: 'Kampus Utama',
    requiredTeachersCount: 1,
    status: 'received',
    requestedBy: 'coordinator@hagios.sch.id',
    requestedAt: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleAssignments: TeachingAssignmentDto[] = [
  {
    id: '00000000-0000-0000-0005-000000000001',
    assignmentNumber: 'ASN-2027-001',
    teachingDemandId: '00000000-0000-0000-0003-000000000001',
    teacherProfileId: '00000000-0000-0000-0006-000000000001',
    assignmentRoleId: '00000000-0000-0000-0004-000000000001',
    assignmentRoleName: 'Guru Utama',
    assignmentScope: 'learning_unit',
    scopeReferenceId: '00000000-0000-0000-0001-000000000001',
    scopeLabel: 'Matematika SD Kelas 4A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    effectiveStartDate: '2027-01-05',
    effectiveEndDate: '2027-06-25',
    workloadCommitmentValue: 5.0,
    workloadUnit: 'Teaching Hours',
    status: 'approved',
    teacherAcknowledged: true,
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0005-000000000002',
    assignmentNumber: 'ASN-2027-002',
    teachingDemandId: '00000000-0000-0000-0003-000000000001',
    teacherProfileId: '00000000-0000-0000-0006-000000000002',
    assignmentRoleId: '00000000-0000-0000-0004-000000000002',
    assignmentRoleName: 'Guru Pendamping',
    assignmentScope: 'learning_unit',
    scopeReferenceId: '00000000-0000-0000-0001-000000000001',
    scopeLabel: 'Matematika SD Kelas 4A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    effectiveStartDate: '2027-01-05',
    effectiveEndDate: '2027-06-25',
    workloadCommitmentValue: 4.0,
    workloadUnit: 'Teaching Hours',
    status: 'proposed',
    teacherAcknowledged: false,
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleConflicts: CapacityConflictDto[] = [];

const sampleSubstitutions: TeachingSubstitutionDto[] = [
  {
    id: '00000000-0000-0000-0009-000000000001',
    substitutionNumber: 'SUB-2027-001',
    originalAssignmentId: '00000000-0000-0000-0005-000000000001',
    originalTeacherId: '00000000-0000-0000-0006-000000000001',
    substituteTeacherId: '00000000-0000-0000-0006-000000000002',
    coverageStartDate: '2027-02-01',
    coverageEndDate: '2027-02-14',
    coverageScopeType: 'full',
    reason: 'medical_leave',
    workloadImpactHours: 5.0,
    status: 'approved',
    approvedBy: 'coordinator@hagios.sch.id',
    approvedDate: '2026-11-10',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleReassignments: TeachingReassignmentDto[] = [
  {
    id: '00000000-0000-0000-0010-000000000001',
    reassignmentNumber: 'REA-2027-001',
    originalAssignmentId: '00000000-0000-0000-0005-000000000001',
    predecessorTeacherId: '00000000-0000-0000-0006-000000000001',
    newTeacherId: '00000000-0000-0000-0006-000000000003',
    effectiveDate: '2027-03-15',
    reassignmentType: 'permanent_transfer',
    reason: 'Instructional restructuring',
    requiresHandover: true,
    handoverStatus: 'pending',
    status: 'proposed',
    approvedBy: '',
    approvedDate: '',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleCompletions: AssignmentCompletionDto[] = [
  {
    id: '00000000-0000-0000-0011-000000000001',
    teachingAssignmentId: '00000000-0000-0000-0005-000000000001',
    completionDate: '2026-12-20',
    academicConfirmationReceived: true,
    academicConfirmationSource: 'SIAKAD Grade Submission Ref #GK-2026-01',
    handoverConfirmed: true,
    unresolvedItemsStatus: 'none',
    capacityReleasedDate: '2026-12-20',
    completedBy: 'director@hagios.sch.id',
    status: 'completed',
    createdAt: '2026-12-20T00:00:00Z',
  },
];

const sampleEvaluation: TeacherEligibilityEvaluationDto = {
  id: '00000000-0000-0000-0007-000000000001',
  teachingDemandId: '00000000-0000-0000-0003-000000000001',
  teacherProfileId: '00000000-0000-0000-0006-000000000001',
  evaluationDate: '2026-11-10',
  isEligible: true,
  eligibilityStatus: 'eligible',
  profileActiveCheck: true,
  authorizationCheckPassed: true,
  qualificationCheckPassed: true,
  availabilityCheckPassed: true,
  capacityCheckPassed: true,
  explanation: 'Guru Budi Hartono memenuhi seluruh 5 kriteria kebijakan institusi (Standing, Otorisasi, Kualifikasi, Ketersediaan Jadwal, dan Batas Beban Mengajar). Rekomendasi: Layak Ditugaskan.',
  evaluatedBy: 'coordinator@hagios.sch.id',
  notes: 'Recommended based on previous high student satisfaction scores.',
  createdAt: '2026-11-10T00:00:00Z',
};

describe('TeachingAssignmentWorkspaceView.vue (SCR-TCH-02 Workbench)', () => {
  let queryClient: QueryClient;

  function createWrapper(props = {}) {
    return mount(TeachingAssignmentWorkspaceView, {
      props,
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });
  }

  beforeEach(() => {
    setActivePinia(createPinia());
    const authStore = useAuthStore();
    authStore.setSession('mock-token', {
      userId: '00000000-0000-0000-0006-000000000001',
      username: 'coordinator',
      fullName: 'Teaching Coordinator',
      roles: ['SuperAdmin', 'AcademicDirector', 'TeachingCoordinator'],
      permissions: [
        'teaching:assignments:view',
        'teaching:assignments:manage',
        'teaching:assignments:approve',
        'teaching:substitutions:arrange',
        'teaching:substitutions:approve_exception',
      ],
      campusId: null,
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
    mockRouteQuery.value = {
      tab: 'demands',
      periodId: '00000000-0000-0000-0002-000000000002',
      campus: 'all',
    };

    if (typeof window !== 'undefined') {
      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
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
    }

    // Default API mocks
    vi.spyOn(assignmentsApi, 'getTeachingDemands').mockResolvedValue(sampleDemands);
    vi.spyOn(assignmentsApi, 'getTeachingAssignments').mockResolvedValue(sampleAssignments);
    vi.spyOn(assignmentsApi, 'getAssignmentRoles').mockResolvedValue(sampleRoles);
    vi.spyOn(assignmentsApi, 'getCapacityConflicts').mockResolvedValue(sampleConflicts);
    vi.spyOn(assignmentsApi, 'getTeachingSubstitutions').mockResolvedValue(sampleSubstitutions);
    vi.spyOn(assignmentsApi, 'getTeachingReassignments').mockResolvedValue(sampleReassignments);
    vi.spyOn(assignmentsApi, 'getAssignmentCompletions').mockResolvedValue(sampleCompletions);
    vi.spyOn(assignmentsApi, 'getCandidateEvaluations').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'evaluateCandidate').mockResolvedValue(sampleEvaluation);
  });

  // ---------------------------------------------------------------------------
  // 1. 5-Tab Navigation & Switching
  // ---------------------------------------------------------------------------
  it('renders workbench with global filters and 5 workspace tabs', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="teaching-assignment-workspace"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="header-period-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="header-campus-select"]').exists()).toBe(true);

    // 5 Tabs exist
    expect(wrapper.find('[data-testid="tab-nav-demands"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-nav-eligibility"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-nav-assignments"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-nav-coverage"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-nav-conclusions"]').exists()).toBe(true);

    // Initial tab panel
    expect(wrapper.find('[data-testid="panel-demands"]').exists()).toBe(true);
  });

  it('switches between all 5 tabs and updates router query', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // 1 -> 2: Switch to Candidate Eligibility
    await wrapper.find('[data-testid="tab-nav-eligibility"]').trigger('click');
    await flushPromises();
    expect(mockReplace).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ tab: 'eligibility' }),
      }),
    );

    // 2 -> 3: Switch to Assignments & Homerooms
    await wrapper.find('[data-testid="tab-nav-assignments"]').trigger('click');
    await flushPromises();
    expect(mockReplace).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ tab: 'assignments' }),
      }),
    );

    // 3 -> 4: Switch to Coverage & Substitutions
    await wrapper.find('[data-testid="tab-nav-coverage"]').trigger('click');
    await flushPromises();
    expect(mockReplace).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ tab: 'coverage' }),
      }),
    );

    // 4 -> 5: Switch to Conclusions
    await wrapper.find('[data-testid="tab-nav-conclusions"]').trigger('click');
    await flushPromises();
    expect(mockReplace).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ tab: 'conclusions' }),
      }),
    );
  });

  // ---------------------------------------------------------------------------
  // 2. Demands Listing & Candidate Eligibility Evaluation
  // ---------------------------------------------------------------------------
  it('renders demands listing and switches to candidate eligibility on evaluate action', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Demands Intake tab is active
    expect(wrapper.text()).toContain('DMD-2027-001');
    expect(wrapper.text()).toContain('Matematika SD Kelas 4A');
    expect(wrapper.text()).toContain('CAP-MATH-PRI');

    // Trigger evaluate action
    const evaluateBtn = wrapper.find('[data-testid="btn-evaluate-candidates"]');
    expect(evaluateBtn.exists()).toBe(true);
    await evaluateBtn.trigger('click');
    await flushPromises();

    // Switched to eligibility tab and synchronized demand
    expect(mockReplace).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({
          tab: 'eligibility',
          demandId: '00000000-0000-0000-0003-000000000001',
        }),
      }),
    );
  });

  it('evaluates candidate eligibility, renders 5-check breakdown cards, and displays natural language explanation', async () => {
    mockRouteQuery.value = {
      tab: 'eligibility',
      periodId: '00000000-0000-0000-0002-000000000002',
      campus: 'all',
      demandId: '00000000-0000-0000-0003-000000000001',
    };

    const evalSpy = vi.spyOn(assignmentsApi, 'evaluateCandidate').mockResolvedValue(sampleEvaluation);

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="candidate-eligibility-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="select-target-demand"]').exists()).toBe(true);

    // Click Run Evaluation Engine
    const runBtn = wrapper.find('[data-testid="btn-run-evaluation"]');
    expect(runBtn.exists()).toBe(true);
    await runBtn.trigger('click');
    await flushPromises();

    expect(evalSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        teachingDemandId: '00000000-0000-0000-0003-000000000001',
        teacherProfileId: expect.any(String),
      }),
    );

    // 5-Check Breakdown Cards are rendered
    expect(wrapper.find('[data-testid="check-cards-grid"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="check-card-standing"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="check-card-authorization"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="check-card-qualification"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="check-card-availability"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="check-card-capacity"]').exists()).toBe(true);

    // Explanation card
    const explanationCard = wrapper.find('[data-testid="natural-language-explanation-card"]');
    expect(explanationCard.exists()).toBe(true);
    expect(explanationCard.text()).toContain(
      'Guru Budi Hartono memenuhi seluruh 5 kriteria kebijakan institusi',
    );

    // Propose assignment shortcut button is present
    expect(wrapper.find('[data-testid="btn-propose-assignment-shortcut"]').exists()).toBe(true);
  });

  // ---------------------------------------------------------------------------
  // 3. ProposeAssignmentModal & ApproveAssignmentModal
  // ---------------------------------------------------------------------------
  it('opens and submits ProposeAssignmentModal in ActiveAssignmentsTab', async () => {
    mockRouteQuery.value = {
      tab: 'assignments',
      periodId: '00000000-0000-0000-0002-000000000002',
      campus: 'all',
    };

    const proposeSpy = vi.spyOn(assignmentsApi, 'proposeTeachingAssignment').mockResolvedValue({
      id: 'new-asn-id',
      assignmentNumber: 'ASN-2027-099',
      status: 'proposed',
      teachingDemandId: '00000000-0000-0000-0003-000000000001',
      teacherProfileId: '00000000-0000-0000-0006-000000000001',
      workloadCommitmentValue: 5,
      createdAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    // ActiveAssignmentsTab is rendered
    expect(wrapper.find('[data-testid="active-assignments-tab"]').exists()).toBe(true);

    // Open Propose Assignment Modal
    const openProposeBtn = wrapper.find('[data-testid="btn-open-propose-assignment"]');
    expect(openProposeBtn.exists()).toBe(true);
    await openProposeBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="propose-assignment-modal"]').exists()).toBe(true);

    // Fill and submit
    const submitBtn = wrapper.find('[data-testid="btn-submit-propose"]');
    await submitBtn.trigger('click');
    await flushPromises();

    expect(proposeSpy).toHaveBeenCalledTimes(1);
  });

  it('opens and confirms ApproveAssignmentModal for proposed assignments', async () => {
    mockRouteQuery.value = {
      tab: 'assignments',
      periodId: '00000000-0000-0000-0002-000000000002',
      campus: 'all',
    };

    const approveSpy = vi.spyOn(assignmentsApi, 'approveTeachingAssignment').mockResolvedValue({
      id: sampleAssignments[1].id,
      assignmentNumber: 'ASN-2027-002',
      status: 'approved',
      approvedDate: '2026-11-10',
      approvedBy: 'coordinator@hagios.sch.id',
      approvedAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Trigger approve on the proposed assignment
    const approveBtn = wrapper.find('[data-testid="btn-approve-assignment"]');
    expect(approveBtn.exists()).toBe(true);
    await approveBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="approve-assignment-modal"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('ASN-2027-002');

    // Confirm authority approval
    const confirmApproveBtn = wrapper.find('[data-testid="btn-confirm-approve"]');
    await confirmApproveBtn.trigger('click');
    await flushPromises();

    expect(approveSpy).toHaveBeenCalledWith(sampleAssignments[1].id, expect.any(Object));
  });

  // ---------------------------------------------------------------------------
  // 4. ArrangeSubstituteModal & ReassignmentModal
  // ---------------------------------------------------------------------------
  it('interacts with ArrangeSubstituteModal and ReassignmentModal in Coverage & Substitutions tab', async () => {
    mockRouteQuery.value = {
      tab: 'coverage',
      periodId: '00000000-0000-0000-0002-000000000002',
      campus: 'all',
    };

    const subSpy = vi.spyOn(assignmentsApi, 'arrangeEmergencySubstitution').mockResolvedValue({
      id: 'sub-new-id',
      substitutionNumber: 'SUB-2027-099',
      status: 'approved',
      workloadImpactHours: 5,
      createdAt: '2026-11-10T00:00:00Z',
    });

    const reasSpy = vi.spyOn(assignmentsApi, 'proposePermanentReassignment').mockResolvedValue({
      id: 'reas-new-id',
      reassignmentNumber: 'REA-2027-099',
      status: 'proposed',
      effectiveDate: '2027-03-01',
      requiresHandover: true,
      createdAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="coverage-substitutions-tab"]').exists()).toBe(true);

    // 1. Open ArrangeSubstituteModal
    const openSubBtn = wrapper.find('[data-testid="btn-open-arrange-substitute"]');
    expect(openSubBtn.exists()).toBe(true);
    await openSubBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="arrange-substitute-modal"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Resolution 3.8 Pre-Authorized Pool Delegation');

    // Submit substitution
    await wrapper.find('[data-testid="btn-submit-substitute"]').trigger('click');
    await flushPromises();
    expect(subSpy).toHaveBeenCalledTimes(1);

    // 2. Open ReassignmentModal
    const openReasBtn = wrapper.find('[data-testid="btn-open-propose-reassignment"]');
    expect(openReasBtn.exists()).toBe(true);
    await openReasBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="reassignment-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="cb-requires-handover"]').exists()).toBe(true);

    // Submit reassignment proposal
    await wrapper.find('[data-testid="input-reassignment-reason"]').setValue('Transfer to grade 5 curriculum leadership.');
    await wrapper.find('[data-testid="btn-submit-reassignment"]').trigger('click');
    await flushPromises();
    expect(reasSpy).toHaveBeenCalledTimes(1);
  });

  // ---------------------------------------------------------------------------
  // 5. ConcludeAssignmentModal & Completion Metrics
  // ---------------------------------------------------------------------------
  it('displays completion metrics and submits ConcludeAssignmentModal in Conclusions tab', async () => {
    mockRouteQuery.value = {
      tab: 'conclusions',
      periodId: '00000000-0000-0000-0002-000000000002',
      campus: 'all',
    };

    const concludeSpy = vi.spyOn(assignmentsApi, 'concludeTeachingAssignment').mockResolvedValue({
      id: 'completion-new-id',
      teachingAssignmentId: sampleAssignments[0].id,
      completionDate: '2027-06-25',
      academicConfirmationReceived: true,
      handoverConfirmed: true,
      status: 'completed',
      createdAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="conclusions-tab"]').exists()).toBe(true);

    // Verify Completion Metrics cards
    expect(wrapper.find('[data-testid="conclusions-metrics"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="metric-total-concluded"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="metric-verification-rate"]').text()).toBe('100%');
    expect(wrapper.find('[data-testid="metric-total-handover"]').text()).toBe('1');

    // Open Conclude Assignment Modal
    const openConcludeBtn = wrapper.find('[data-testid="btn-open-conclude-modal"]');
    expect(openConcludeBtn.exists()).toBe(true);
    await openConcludeBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="conclude-assignment-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="cb-academic-confirm"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="cb-handover-confirm"]').exists()).toBe(true);

    // Submit conclusion
    await wrapper.find('[data-testid="btn-confirm-conclude"]').trigger('click');
    await flushPromises();

    expect(concludeSpy).toHaveBeenCalledTimes(1);
  });
});
