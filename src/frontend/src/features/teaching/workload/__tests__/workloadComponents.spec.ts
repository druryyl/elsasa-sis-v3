// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import WorkloadKpiCards from '../components/WorkloadKpiCards.vue';
import FacultyWorkloadTable from '../components/FacultyWorkloadTable.vue';
import WorkloadDrillDownDrawer from '../components/WorkloadDrillDownDrawer.vue';
import RequestExceptionModal from '../components/RequestExceptionModal.vue';
import ApproveExceptionModal from '../components/ApproveExceptionModal.vue';
import FacultyWorkloadMonitoringView from '../views/FacultyWorkloadMonitoringView.vue';
import type { TeacherWorkloadCommitmentDto, WorkloadExceptionDto } from '../types';
import * as workloadApi from '../api/workloadApi';
import { apiClient } from '@/shared/services/http';
import { useAuthStore } from '@/app/stores/auth.store';

// Mock vue-router
const mockReplace = vi.fn();
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRoute: () => ({
      query: { periodId: '00000000-0000-0000-0002-000000000002' },
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

const sampleCommitments: TeacherWorkloadCommitmentDto[] = [
  {
    id: 'com-1',
    teacherProfileId: '00000000-0000-0000-0006-000000000001',
    teacherNumber: 'TCH-001',
    fullName: 'Budi Hartono, M.Pd.',
    standing: 'active',
    campusAffiliation: 'Campus A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    workloadPolicyId: 'pol-1',
    maxStandardCapacity: 18,
    warningThreshold: 20,
    prohibitedThreshold: 24,
    plannedWorkloadHours: 2,
    committedWorkloadHours: 16,
    maxCapacityLimitHours: 24,
    remainingCapacityHours: 8,
    status: 'normal',
    activeAssignmentCount: 3,
    approvedExceptionCount: 0,
    asOfTimestamp: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: 'com-2',
    teacherProfileId: '00000000-0000-0000-0006-000000000002',
    teacherNumber: 'TCH-002',
    fullName: 'Siti Nurhaliza, S.Pd.',
    standing: 'active',
    campusAffiliation: 'Campus A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    workloadPolicyId: 'pol-1',
    maxStandardCapacity: 18,
    warningThreshold: 20,
    prohibitedThreshold: 24,
    plannedWorkloadHours: 0,
    committedWorkloadHours: 22,
    maxCapacityLimitHours: 24,
    remainingCapacityHours: 2,
    status: 'approaching_limit',
    activeAssignmentCount: 4,
    approvedExceptionCount: 0,
    asOfTimestamp: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: 'com-3',
    teacherProfileId: '00000000-0000-0000-0006-000000000003',
    teacherNumber: 'TCH-003',
    fullName: 'John Miller, Ph.D.',
    standing: 'active',
    campusAffiliation: 'Campus B',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    workloadPolicyId: 'pol-1',
    maxStandardCapacity: 18,
    warningThreshold: 20,
    prohibitedThreshold: 24,
    plannedWorkloadHours: 0,
    committedWorkloadHours: 28,
    maxCapacityLimitHours: 28,
    remainingCapacityHours: 0,
    status: 'overloaded_with_exception',
    activeAssignmentCount: 5,
    approvedExceptionCount: 1,
    asOfTimestamp: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: 'com-4',
    teacherProfileId: '00000000-0000-0000-0006-000000000004',
    teacherNumber: 'TCH-004',
    fullName: 'Hendra Setiawan, S.Si.',
    standing: 'active',
    campusAffiliation: 'Campus A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    workloadPolicyId: 'pol-1',
    maxStandardCapacity: 18,
    warningThreshold: 20,
    prohibitedThreshold: 24,
    plannedWorkloadHours: 4,
    committedWorkloadHours: 26,
    maxCapacityLimitHours: 24,
    remainingCapacityHours: -2,
    status: 'breached',
    activeAssignmentCount: 5,
    approvedExceptionCount: 0,
    asOfTimestamp: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

describe('SCR-TCH-03 Faculty Workload Monitoring Workbench (P8-S26)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    const pinia = createPinia();
    setActivePinia(pinia);

    // Seed authStore with SuperAdmin privileges
    const authStore = useAuthStore();
    authStore.$patch({
      user: {
        userId: '00000000-0000-0000-0001-000000000001',
        username: 'admin',
        fullName: 'System Administrator',
        roles: ['SuperAdmin'],
        permissions: ['*'],
        campusId: null,
      },
      roles: ['SuperAdmin'],
      permissions: ['*'],
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

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
    }
  });

  describe('WorkloadKpiCards.vue', () => {
    it('calculates counts correctly across threshold states', () => {
      const wrapper = mount(WorkloadKpiCards, {
        props: {
          commitments: sampleCommitments,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      expect(wrapper.find('[data-testid="kpi-count-total"]').text()).toBe('4');
      expect(wrapper.find('[data-testid="kpi-count-normal"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="kpi-count-approaching"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="kpi-count-exception"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="kpi-count-breached"]').text()).toBe('1');
    });

    it('emits selectStatus when cards are clicked', async () => {
      const wrapper = mount(WorkloadKpiCards, {
        props: {
          commitments: sampleCommitments,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      await wrapper.find('[data-testid="kpi-card-normal"]').trigger('click');
      expect(wrapper.emitted('selectStatus')?.[0]).toEqual(['normal']);

      await wrapper.find('[data-testid="kpi-card-breached"]').trigger('click');
      expect(wrapper.emitted('selectStatus')?.[1]).toEqual(['breached']);

      await wrapper.find('[data-testid="kpi-card-total"]').trigger('click');
      expect(wrapper.emitted('selectStatus')?.[2]).toEqual(['all']);
    });
  });

  describe('FacultyWorkloadTable.vue', () => {
    it('renders commitments with capacity progress bars', () => {
      const wrapper = mount(FacultyWorkloadTable, {
        props: {
          commitments: sampleCommitments,
          isLoading: false,
        },
        global: {
          plugins: [PrimeVue],
          stubs: commonStubs,
        },
      });

      expect(wrapper.text()).toContain('Budi Hartono');
      expect(wrapper.text()).toContain('Siti Nurhaliza');
      expect(wrapper.text()).toContain('John Miller');
      expect(wrapper.text()).toContain('Hendra Setiawan');

      const progressBars = wrapper.findAll('[data-testid="capacity-progress-bar"]');
      expect(progressBars.length).toBe(4);
    });

    it('emits drillDown event when details button is clicked', async () => {
      const wrapper = mount(FacultyWorkloadTable, {
        props: {
          commitments: sampleCommitments,
        },
        global: {
          plugins: [PrimeVue],
          stubs: commonStubs,
        },
      });

      const drillDownBtns = wrapper.findAll('[data-testid="workload-drilldown-button"]');
      expect(drillDownBtns.length).toBeGreaterThan(0);

      await drillDownBtns[0].trigger('click');
      expect(wrapper.emitted('drillDown')?.[0]).toEqual([sampleCommitments[0]]);
    });

    it('emits requestException event when request exception button is clicked', async () => {
      const wrapper = mount(FacultyWorkloadTable, {
        props: {
          commitments: sampleCommitments,
          canRequestException: true,
        },
        global: {
          plugins: [PrimeVue],
          stubs: commonStubs,
        },
      });

      const requestBtns = wrapper.findAll('[data-testid="workload-request-exception-button"]');
      expect(requestBtns.length).toBeGreaterThan(0);

      await requestBtns[0].trigger('click');
      expect(wrapper.emitted('requestException')?.[0]).toEqual([sampleCommitments[0]]);
    });

    it('emits approveException event when approve button is clicked for overloaded faculty', async () => {
      const wrapper = mount(FacultyWorkloadTable, {
        props: {
          commitments: sampleCommitments,
          canApproveException: true,
        },
        global: {
          plugins: [PrimeVue],
          stubs: commonStubs,
        },
      });

      const approveBtns = wrapper.findAll('[data-testid="workload-approve-exception-button"]');
      expect(approveBtns.length).toBeGreaterThan(0);

      await approveBtns[0].trigger('click');
      expect(wrapper.emitted('approveException')).toBeDefined();
    });

    it('filters commitments by search query', async () => {
      const wrapper = mount(FacultyWorkloadTable, {
        props: {
          commitments: sampleCommitments,
        },
        global: {
          plugins: [PrimeVue],
          stubs: commonStubs,
        },
      });

      const searchInput = wrapper.find('[data-testid="workload-search-input"]');
      await searchInput.setValue('Miller');

      expect(wrapper.text()).toContain('John Miller');
      expect(wrapper.text()).not.toContain('Budi Hartono');
    });
  });

  describe('WorkloadDrillDownDrawer.vue', () => {
    it('renders teacher capacity overview and fetches assignments', async () => {
      const mockAssignments = [
        {
          id: 'asn-1',
          assignmentNumber: 'ASN-2026-0001',
          teachingDemandId: 'dem-1',
          teacherProfileId: 'teach-1',
          assignmentRoleId: 'role-1',
          assignmentRoleName: 'Guru Utama',
          assignmentScope: 'learning_unit',
          scopeReferenceId: 'unit-1',
          scopeLabel: 'Matematika SD Kelas 4',
          academicPeriodId: '00000000-0000-0000-0002-000000000002',
          effectiveStartDate: '2027-01-05',
          effectiveEndDate: '2027-06-25',
          workloadCommitmentValue: 12,
          workloadUnit: 'Teaching Hours',
          status: 'approved',
          approvedBy: 'Academic Director',
          approvedDate: '2026-12-15',
        },
      ];

      vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ items: mockAssignments } as never);

      const wrapper = mount(WorkloadDrillDownDrawer, {
        props: {
          visible: true,
          commitment: sampleCommitments[0],
          academicPeriodId: '00000000-0000-0000-0002-000000000002',
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="drawer-committed-hours"]').text()).toContain('16 hrs');
      expect(wrapper.text()).toContain('Matematika SD Kelas 4');
      expect(wrapper.text()).toContain('ASN-2026-0001');
      expect(wrapper.text()).toContain('12 hrs/wk');
    });
  });

  describe('RequestExceptionModal.vue', () => {
    it('validates and submits exception recommendation', async () => {
      const requestSpy = vi
        .spyOn(workloadApi, 'requestWorkloadException')
        .mockResolvedValueOnce({
          id: 'exc-101',
          exceptionNumber: 'EXC-2026-0001',
          teachingAssignmentId: '',
          teacherProfileId: 'teach-4',
          academicPeriodId: '00000000-0000-0000-0002-000000000002',
          workloadPolicyId: 'pol-1',
          exceededHours: 4,
          justification: 'Critical curriculum staffing coverage required.',
          recommendedBy: 'Coordinator',
          recommendedDate: '2026-11-10',
          effectiveStartDate: '2027-01-05',
          effectiveEndDate: '2027-06-25',
          status: 'recommended',
          createdAt: '2026-11-10T00:00:00Z',
        });

      const wrapper = mount(RequestExceptionModal, {
        props: {
          visible: true,
          commitment: sampleCommitments[3],
          academicPeriodId: '00000000-0000-0000-0002-000000000002',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      await wrapper
        .find('[data-testid="coordinator-justification-input"]')
        .setValue('Critical curriculum staffing coverage required.');

      await wrapper.find('[data-testid="submit-exception-btn"]').trigger('click');
      await flushPromises();

      expect(requestSpy).toHaveBeenCalled();
      expect(wrapper.emitted('submitted')).toBeDefined();
    });
  });

  describe('ApproveExceptionModal.vue', () => {
    it('renders exception summary and approves with decision remarks', async () => {
      const sampleException: WorkloadExceptionDto = {
        id: 'exc-101',
        exceptionNumber: 'EXC-2026-0001',
        teachingAssignmentId: 'asn-1',
        teacherProfileId: 'teach-3',
        workloadPolicyId: 'pol-1',
        exceededHours: 4,
        justification: 'Visiting lecturer overload authorized.',
        recommendedBy: 'Coordinator',
        recommendedDate: '2026-11-10',
        effectiveStartDate: '2027-01-05',
        effectiveEndDate: '2027-06-25',
        status: 'recommended',
        createdAt: '2026-11-10T00:00:00Z',
      };

      const approveSpy = vi
        .spyOn(workloadApi, 'approveWorkloadException')
        .mockResolvedValueOnce({
          id: 'exc-101',
          exceptionNumber: 'EXC-2026-0001',
          teachingAssignmentId: 'asn-1',
          teacherProfileId: 'teach-3',
          academicPeriodId: '00000000-0000-0000-0002-000000000002',
          exceededHours: 4,
          status: 'approved',
          approvedBy: 'Director',
          approvedDate: '2026-11-10',
          decisionRemarks: 'Approved for international visiting faculty.',
          approvedExceptionCount: 1,
          commitmentStatus: 'overloaded_with_exception',
          resolvedConflictsCount: 1,
          approvedAt: '2026-11-10T00:00:00Z',
        });

      const wrapper = mount(ApproveExceptionModal, {
        props: {
          visible: true,
          commitment: sampleCommitments[2],
          exception: sampleException,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="exception-number"]').text()).toBe('EXC-2026-0001');

      await wrapper
        .find('[data-testid="decision-remarks-input"]')
        .setValue('Approved for international visiting faculty.');

      await wrapper.find('[data-testid="confirm-approve-btn"]').trigger('click');
      await flushPromises();

      expect(approveSpy).toHaveBeenCalledWith('exc-101', {
        decisionRemarks: 'Approved for international visiting faculty.',
      });
      expect(wrapper.emitted('approved')).toBeDefined();
    });
  });

  describe('FacultyWorkloadMonitoringView.vue', () => {
    it('renders workbench with period selector, policy chip, and triggers recalculation', async () => {
      vi.spyOn(workloadApi, 'getWorkloadPolicies').mockResolvedValueOnce([
        {
          id: 'pol-1',
          code: 'WPOL-2026-PRIMARY',
          name: 'Primary Faculty Workload Policy',
          academicYearId: 'ay-1',
          version: '1.0',
          workloadUnit: 'Teaching Hours',
          maxStandardCapacity: 18,
          warningThreshold: 20,
          prohibitedThreshold: 24,
          includesAssessmentEffort: true,
          includesAdvisingEffort: true,
          includesPreparationTime: true,
          calculationRulesSummary: 'Standard rules',
          effectiveStartDate: '2026-07-01',
          status: 'active',
          approvedBy: 'Director',
          approvedDate: '2026-06-15',
          createdAt: '2026-06-15T00:00:00Z',
        },
      ]);

      vi.spyOn(workloadApi, 'getWorkloadCommitments').mockResolvedValueOnce(sampleCommitments);

      const recalculateSpy = vi
        .spyOn(workloadApi, 'recalculateWorkload')
        .mockResolvedValueOnce({
          academicPeriodId: '00000000-0000-0000-0002-000000000002',
          recalculatedCount: 4,
          recalculatedAt: '2026-11-10T00:00:00Z',
          message: 'Successfully reconciled 4 faculty commitment records.',
        });

      const wrapper = mount(FacultyWorkloadMonitoringView, {
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="faculty-workload-workbench"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="active-policy-chip"]').text()).toContain('WPOL-2026-PRIMARY');
      expect(wrapper.find('[data-testid="kpi-count-total"]').text()).toBe('4');

      // Click Recalculate button
      const recalculateBtn = wrapper.find('[data-testid="recalculate-workload-btn"]');
      expect(recalculateBtn.exists()).toBe(true);

      await recalculateBtn.trigger('click');
      await flushPromises();

      expect(recalculateSpy).toHaveBeenCalledWith('00000000-0000-0000-0002-000000000002');
    });
  });
});
