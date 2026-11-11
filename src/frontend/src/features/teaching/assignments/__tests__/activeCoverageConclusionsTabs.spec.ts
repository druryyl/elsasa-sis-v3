// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import ActiveAssignmentsTab from '../components/ActiveAssignmentsTab.vue';
import CoverageSubstitutionsTab from '../components/CoverageSubstitutionsTab.vue';
import ConclusionsTab from '../components/ConclusionsTab.vue';
import ProposeAssignmentModal from '../components/ProposeAssignmentModal.vue';
import ApproveAssignmentModal from '../components/ApproveAssignmentModal.vue';
import ArrangeSubstituteModal from '../components/ArrangeSubstituteModal.vue';
import ReassignmentModal from '../components/ReassignmentModal.vue';
import ConcludeAssignmentModal from '../components/ConcludeAssignmentModal.vue';
import TeachingAssignmentWorkspaceView from '../views/TeachingAssignmentWorkspaceView.vue';
import * as assignmentsApi from '../api/assignmentsApi';
import type {
  TeachingAssignmentDto,
  CapacityConflictDto,
  TeachingSubstitutionDto,
  TeachingReassignmentDto,
  AssignmentCompletionDto,
  AssignmentRoleDto,
  TeachingDemandDto,
} from '../types';

// Mock vue-router
const mockReplace = vi.fn();
let currentTabQuery = 'assignments';

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRoute: () => ({
      query: {
        tab: currentTabQuery,
        periodId: '00000000-0000-0000-0002-000000000002',
        campus: 'all',
      },
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

const sampleAssignments: TeachingAssignmentDto[] = [
  {
    id: '00000000-0000-0000-0005-000000000001',
    assignmentNumber: 'ASN-2027-001',
    teachingDemandId: '00000000-0000-0000-0003-000000000001',
    teacherProfileId: '00000000-0000-0000-0006-000000000001', // Budi Hartono
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
    teachingDemandId: '00000000-0000-0000-0003-000000000001', // Shared demand/scope: Co-teaching with Budi
    teacherProfileId: '00000000-0000-0000-0006-000000000002', // Siti Nurhaliza
    assignmentRoleId: '00000000-0000-0000-0004-000000000002',
    assignmentRoleName: 'Guru Pendamping',
    assignmentScope: 'learning_unit',
    scopeReferenceId: '00000000-0000-0000-0001-000000000001', // Same scope
    scopeLabel: 'Matematika SD Kelas 4A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    effectiveStartDate: '2027-01-05',
    effectiveEndDate: '2027-06-25',
    workloadCommitmentValue: 4.0,
    workloadUnit: 'Teaching Hours',
    status: 'approved',
    teacherAcknowledged: false,
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0005-000000000003',
    assignmentNumber: 'ASN-2027-003',
    teachingDemandId: '00000000-0000-0000-0003-000000000003',
    teacherProfileId: '00000000-0000-0000-0006-000000000001', // Budi Hartono Homeroom
    assignmentRoleId: '00000000-0000-0000-0004-000000000005',
    assignmentRoleName: 'Wali Kelas',
    assignmentScope: 'cohort_group',
    scopeReferenceId: '00000000-0000-0000-0007-000000000001',
    scopeLabel: 'Kelas 4A (Wali Kelas)',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    effectiveStartDate: '2027-01-05',
    effectiveEndDate: '2027-06-25',
    workloadCommitmentValue: 2.0,
    workloadUnit: 'Teaching Hours',
    status: 'proposed',
    teacherAcknowledged: false,
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleConflicts: CapacityConflictDto[] = [
  {
    id: '00000000-0000-0000-0008-000000000001',
    teachingAssignmentId: '00000000-0000-0000-0005-000000000003',
    teacherProfileId: '00000000-0000-0000-0006-000000000001',
    conflictType: 'workload_approaching_limit',
    conflictClassification: 'capacity_warning',
    severity: 'advisory',
    description: 'Budi Hartono committed hours reach 19.0 hrs/wk, approaching 20.0 policy warning limit.',
    detectedDate: '2026-11-10',
    status: 'unresolved',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleSubstitutions: TeachingSubstitutionDto[] = [
  {
    id: '00000000-0000-0000-0009-000000000001',
    substitutionNumber: 'SUB-2027-001',
    originalAssignmentId: '00000000-0000-0000-0005-000000000001',
    originalTeacherId: '00000000-0000-0000-0006-000000000001', // Budi Hartono
    substituteTeacherId: '00000000-0000-0000-0006-000000000002', // Siti Nurhaliza
    coverageStartDate: '2027-02-01',
    coverageEndDate: '2027-02-14',
    coverageScopeType: 'full',
    reason: 'medical_leave',
    workloadImpactHours: 5.0,
    status: 'approved', // Pre-Authorized direct
    approvedBy: 'coordinator@hagios.sch.id',
    approvedDate: '2026-11-10',
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0009-000000000002',
    substitutionNumber: 'SUB-2027-002',
    originalAssignmentId: '00000000-0000-0000-0005-000000000001',
    originalTeacherId: '00000000-0000-0000-0006-000000000001',
    substituteTeacherId: '00000000-0000-0000-0006-000000000003', // John Miller
    coverageStartDate: '2027-03-01',
    coverageEndDate: '2027-03-07',
    coverageScopeType: 'partial',
    reason: 'official_training',
    workloadImpactHours: 3.0,
    status: 'proposed', // Escalated exception
    approvedBy: '',
    approvedDate: '',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleReassignments: TeachingReassignmentDto[] = [
  {
    id: '00000000-0000-0000-0010-000000000001',
    reassignmentNumber: 'REA-2027-001',
    originalAssignmentId: '00000000-0000-0000-0005-000000000001',
    predecessorTeacherId: '00000000-0000-0000-0006-000000000001', // Budi
    newTeacherId: '00000000-0000-0000-0006-000000000003', // John Miller
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

describe('SCR-TCH-02 Active, Coverage & Conclusions Tabs (P8-S25)', () => {
  let queryClient: QueryClient;

  function createWrapper(Component: any, props = {}) {
    return mount(Component, {
      props,
      global: {
        plugins: [
          PrimeVue,
          ToastService,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: commonStubs,
      },
    });
  }

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();

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

      window.ResizeObserver =
        window.ResizeObserver ||
        vi.fn().mockImplementation(() => ({
          observe: vi.fn(),
          unobserve: vi.fn(),
          disconnect: vi.fn(),
        }));
    }

    // Default mock returns
    vi.spyOn(assignmentsApi, 'getTeachingDemands').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'getCandidateEvaluations').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'getTeachingAssignments').mockResolvedValue(sampleAssignments as any);
    vi.spyOn(assignmentsApi, 'getAssignmentRoles').mockResolvedValue(sampleRoles);
    vi.spyOn(assignmentsApi, 'getCapacityConflicts').mockResolvedValue(sampleConflicts as any);
    vi.spyOn(assignmentsApi, 'getTeachingSubstitutions').mockResolvedValue(sampleSubstitutions as any);
    vi.spyOn(assignmentsApi, 'getTeachingReassignments').mockResolvedValue(sampleReassignments as any);
    vi.spyOn(assignmentsApi, 'getAssignmentCompletions').mockResolvedValue(sampleCompletions);
  });

  // ---------------------------------------------------------------------------
  // 1. ActiveAssignmentsTab.vue
  // ---------------------------------------------------------------------------
  describe('ActiveAssignmentsTab.vue', () => {
    it('renders active assignments with role badges, co-teaching grouping, and homeroom badges', async () => {
      const wrapper = createWrapper(ActiveAssignmentsTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        canManage: true,
        canApprove: true,
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="active-assignments-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="active-assignments-table"]').exists()).toBe(true);

      // Verify assignment numbers rendered
      expect(wrapper.text()).toContain('ASN-2027-001');
      expect(wrapper.text()).toContain('ASN-2027-002');
      expect(wrapper.text()).toContain('ASN-2027-003');

      // Verify Homeroom advisory badge (Wali Kelas, TD-06)
      expect(wrapper.text()).toContain('Wali Kelas');

      // Verify Co-teaching grouping badge (ASN-2027-001 and 002 share same scope)
      expect(wrapper.text()).toContain('Co-teaching');

      // Verify Capacity warning alert indicator from sampleConflicts
      expect(wrapper.text()).toContain('advisory');
    });

    it('filters assignments by homeroom advisory filter', async () => {
      const wrapper = createWrapper(ActiveAssignmentsTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
      });
      await flushPromises();

      // Initially all 3 rendered
      expect(wrapper.text()).toContain('ASN-2027-001');
      expect(wrapper.text()).toContain('ASN-2027-003');

      // Select 'homeroom_only'
      const selects = wrapper.findAllComponents({ name: 'Select' });
      const homeroomSelect = selects.find((c) => c.attributes('data-testid') === 'select-homeroom-filter') || selects[2];
      await homeroomSelect.vm.$emit('update:modelValue', 'homeroom_only');
      await flushPromises();

      // Only ASN-2027-003 (Wali Kelas) should be visible
      expect(wrapper.text()).toContain('ASN-2027-003');
      expect(wrapper.text()).not.toContain('ASN-2027-001');
    });

    it('triggers approve assignment modal on proposed assignment', async () => {
      const wrapper = createWrapper(ActiveAssignmentsTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        canApprove: true,
      });
      await flushPromises();

      const approveBtn = wrapper.find('[data-testid="btn-approve-assignment"]');
      expect(approveBtn.exists()).toBe(true);
      await approveBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="approve-assignment-modal"]').exists()).toBe(true);
    });

    it('opens propose assignment modal when clicking Propose Assignment', async () => {
      const wrapper = createWrapper(ActiveAssignmentsTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        canManage: true,
      });
      await flushPromises();

      const proposeBtn = wrapper.find('[data-testid="btn-open-propose-assignment"]');
      expect(proposeBtn.exists()).toBe(true);
      await proposeBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="propose-assignment-modal"]').exists()).toBe(true);
    });
  });

  // ---------------------------------------------------------------------------
  // 2. CoverageSubstitutionsTab.vue
  // ---------------------------------------------------------------------------
  describe('CoverageSubstitutionsTab.vue', () => {
    it('renders leave summary and emergency substitutions with Resolution 3.8 delegation indicator', async () => {
      const wrapper = createWrapper(CoverageSubstitutionsTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        canManage: true,
        canApprove: true,
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="coverage-substitutions-tab"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Active Faculty Leave');
      expect(wrapper.text()).toContain('Budi Hartono, M.Pd.');

      // Substitutions list
      expect(wrapper.text()).toContain('SUB-2027-001');
      expect(wrapper.text()).toContain('Pre-Authorized (Direct)');
      expect(wrapper.text()).toContain('SUB-2027-002');
      expect(wrapper.text()).toContain('Exception Escalation');
    });

    it('switches between substitutions and permanent reassignments views', async () => {
      const wrapper = createWrapper(CoverageSubstitutionsTab);
      await flushPromises();

      // Click Permanent Reassignments tab button
      const reasBtn = wrapper.find('[data-testid="btn-view-reassignments"]');
      await reasBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="reassignments-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('REA-2027-001');
      expect(wrapper.text()).toContain('Handover Pending');
    });

    it('opens arrange substitute modal when clicking trigger', async () => {
      const wrapper = createWrapper(CoverageSubstitutionsTab, { canManage: true });
      await flushPromises();

      const arrangeBtn = wrapper.find('[data-testid="btn-open-arrange-substitute"]');
      await arrangeBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="arrange-substitute-modal"]').exists()).toBe(true);
    });
  });

  // ---------------------------------------------------------------------------
  // 3. ConclusionsTab.vue
  // ---------------------------------------------------------------------------
  describe('ConclusionsTab.vue', () => {
    it('renders conclusion metrics and completed assignments archive', async () => {
      const wrapper = createWrapper(ConclusionsTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        canManage: true,
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="conclusions-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="conclusions-metrics"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="metric-total-concluded"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="metric-verification-rate"]').text()).toBe('100%');

      // Table contains SIAKAD confirmation protocol
      expect(wrapper.text()).toContain('SIAKAD Grade Submission Ref #GK-2026-01');
      expect(wrapper.text()).toContain('Confirmed');
    });

    it('opens conclude assignment modal when action triggered', async () => {
      const wrapper = createWrapper(ConclusionsTab, { canManage: true });
      await flushPromises();

      const concludeBtn = wrapper.find('[data-testid="btn-open-conclude-modal"]');
      expect(concludeBtn.exists()).toBe(true);
      await concludeBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="conclude-assignment-modal"]').exists()).toBe(true);
    });
  });

  // ---------------------------------------------------------------------------
  // 4. Modals Verification
  // ---------------------------------------------------------------------------
  describe('Feature Modals', () => {
    it('ProposeAssignmentModal renders fields and handles submission', async () => {
      const proposeSpy = vi.spyOn(assignmentsApi, 'proposeTeachingAssignment').mockResolvedValue({
        id: 'new-id',
        assignmentNumber: 'ASN-2027-099',
        status: 'proposed',
        teachingDemandId: '00000000-0000-0000-0003-000000000001',
        teacherProfileId: '00000000-0000-0000-0006-000000000001',
        workloadCommitmentValue: 5,
        createdAt: '2026-11-10',
      });

      const sampleDemand: TeachingDemandDto = {
        id: '00000000-0000-0000-0003-000000000001',
        demandCode: 'DMD-2027-001',
        sourceDomain: 'Academics',
        sourceWorkReference: '00000000-0000-0000-0001-000000000001',
        sourceWorkLabel: 'Matematika SD Kelas 4A',
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        requiredCapabilityCode: 'CAP-MATH-PRI',
        requiredAssignmentRoleId: '00000000-0000-0000-0004-000000000001',
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
      };

      const wrapper = createWrapper(ProposeAssignmentModal, {
        visible: true,
        preselectedDemand: sampleDemand,
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="propose-assignment-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-scope-label"]').exists()).toBe(true);

      // Submit
      await wrapper.find('[data-testid="btn-submit-propose"]').trigger('click');
      await flushPromises();

      expect(proposeSpy).toHaveBeenCalledTimes(1);
    });

    it('ApproveAssignmentModal confirms assignment commitment', async () => {
      const approveSpy = vi.spyOn(assignmentsApi, 'approveTeachingAssignment').mockResolvedValue({
        id: sampleAssignments[2].id,
        assignmentNumber: 'ASN-2027-003',
        status: 'approved',
        approvedDate: '2026-11-10',
        approvedBy: 'principal@hagios.sch.id',
        approvedAt: '2026-11-10T00:00:00Z',
      });

      const wrapper = createWrapper(ApproveAssignmentModal, {
        visible: true,
        assignment: sampleAssignments[2],
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="approve-assignment-modal"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('ASN-2027-003');

      await wrapper.find('[data-testid="btn-confirm-approve"]').trigger('click');
      await flushPromises();

      expect(approveSpy).toHaveBeenCalledWith(sampleAssignments[2].id, expect.any(Object));
    });

    it('ArrangeSubstituteModal validates Resolution 3.8 delegation callout', async () => {
      const wrapper = createWrapper(ArrangeSubstituteModal, {
        visible: true,
        preselectedAssignment: sampleAssignments[0],
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="arrange-substitute-modal"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Resolution 3.8 Pre-Authorized Pool Delegation');
      expect(wrapper.find('[data-testid="btn-submit-substitute"]').exists()).toBe(true);
    });

    it('ConcludeAssignmentModal validates academic confirmation and capacity release', async () => {
      const concludeSpy = vi.spyOn(assignmentsApi, 'concludeTeachingAssignment').mockResolvedValue({
        id: 'comp-1',
        teachingAssignmentId: sampleAssignments[0].id,
        assignmentNumber: 'ASN-2027-001',
        completionDate: '2027-06-25',
        academicConfirmationReceived: true,
        academicConfirmationSource: 'SIAKAD',
        handoverConfirmed: true,
        unresolvedItemsStatus: 'none',
        capacityReleasedDate: '2027-06-25',
        releasedWorkloadHours: 5.0,
        completedBy: 'director',
        status: 'completed',
        createdAt: '2027-06-25',
      });

      const wrapper = createWrapper(ConcludeAssignmentModal, {
        visible: true,
        assignment: sampleAssignments[0],
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="conclude-assignment-modal"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('5 hours');

      await wrapper.find('[data-testid="btn-confirm-conclude"]').trigger('click');
      await flushPromises();

      expect(concludeSpy).toHaveBeenCalledTimes(1);
    });
  });

  // ---------------------------------------------------------------------------
  // 5. Workspace View Integration with Tabs 3, 4, 5
  // ---------------------------------------------------------------------------
  describe('TeachingAssignmentWorkspaceView Integration', () => {
    it('renders workspace with active assignments tab when selected', async () => {
      currentTabQuery = 'assignments';
      const wrapper = createWrapper(TeachingAssignmentWorkspaceView);
      await flushPromises();

      expect(wrapper.find('[data-testid="panel-assignments"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="active-assignments-tab"]').exists()).toBe(true);
    });
  });
});
