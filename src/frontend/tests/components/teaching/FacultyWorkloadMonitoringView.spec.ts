// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import type * as VueRouter from 'vue-router';
import FacultyWorkloadMonitoringView from '@/features/teaching/workload/views/FacultyWorkloadMonitoringView.vue';
import * as workloadApi from '@/features/teaching/workload/api/workloadApi';
import { apiClient } from '@/shared/services/http';
import { useAuthStore } from '@/app/stores/auth.store';
import type { TeacherWorkloadCommitmentDto, WorkloadPolicyDto, WorkloadExceptionDto } from '@/features/teaching/workload/types';

// Mock vue-router
const mockRouteQuery = ref<Record<string, string>>({
  periodId: '00000000-0000-0000-0002-000000000002',
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

const samplePolicy: WorkloadPolicyDto = {
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
  calculationRulesSummary: 'Standard rules for primary division',
  effectiveStartDate: '2026-07-01',
  status: 'active',
  approvedBy: 'director@hagios.sch.id',
  approvedDate: '2026-06-15',
  createdAt: '2026-06-15T00:00:00Z',
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
    committedWorkloadHours: 21,
    maxCapacityLimitHours: 24,
    remainingCapacityHours: 3,
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
    fullName: 'John Miller, B.Ed.',
    standing: 'active',
    campusAffiliation: 'Campus B',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    workloadPolicyId: 'pol-1',
    maxStandardCapacity: 18,
    warningThreshold: 20,
    prohibitedThreshold: 24,
    plannedWorkloadHours: 0,
    committedWorkloadHours: 26,
    maxCapacityLimitHours: 28,
    remainingCapacityHours: 2,
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
    fullName: 'Dewi Sartika, S.Si.',
    standing: 'active',
    campusAffiliation: 'Campus A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    workloadPolicyId: 'pol-1',
    maxStandardCapacity: 18,
    warningThreshold: 20,
    prohibitedThreshold: 24,
    plannedWorkloadHours: 1,
    committedWorkloadHours: 25,
    maxCapacityLimitHours: 24,
    remainingCapacityHours: -1,
    status: 'breached',
    activeAssignmentCount: 5,
    approvedExceptionCount: 0,
    asOfTimestamp: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleExceptions: WorkloadExceptionDto[] = [
  {
    id: 'exc-101',
    exceptionNumber: 'EXC-2026-0001',
    teacherProfileId: '00000000-0000-0000-0006-000000000003',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    requestedHours: 4,
    exceededHours: 2,
    newProhibitedThresholdHours: 28,
    effectiveStartDate: '2026-07-01',
    effectiveEndDate: '2026-12-31',
    status: 'recommended',
    recommendationStatus: 'recommended',
    coordinatorJustification: 'Covering senior mathematics during sabbatical.',
    requestedBy: 'coordinator@hagios.sch.id',
    requestedAt: '2026-11-01T00:00:00Z',
    createdAt: '2026-11-01T00:00:00Z',
  },
];

describe('FacultyWorkloadMonitoringView.vue (SCR-TCH-03 Workbench)', () => {
  let queryClient: QueryClient;

  function createWrapper() {
    return mount(FacultyWorkloadMonitoringView, {
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
      username: 'director',
      fullName: 'Academic Director',
      roles: ['SuperAdmin', 'AcademicDirector'],
      permissions: [
        'teaching:workload:view',
        'teaching:workload:recalculate',
        'teaching:workload:request_exception',
        'teaching:workload:approve_exception',
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
      periodId: '00000000-0000-0000-0002-000000000002',
    };

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

    vi.spyOn(workloadApi, 'getWorkloadPolicies').mockResolvedValue([samplePolicy]);
    vi.spyOn(workloadApi, 'getWorkloadCommitments').mockResolvedValue(sampleCommitments);
    vi.spyOn(workloadApi, 'getWorkloadExceptions').mockResolvedValue(sampleExceptions);
    vi.spyOn(apiClient, 'get').mockResolvedValue([
      {
        id: 'asn-101',
        assignmentNumber: 'ASN-2027-001',
        scopeLabel: 'Matematika SD Kelas 4A',
        assignmentRoleName: 'Guru Utama',
        workloadCommitmentValue: 5,
        status: 'approved',
      },
    ]);
  });

  // ---------------------------------------------------------------------------
  // 1. Academic Period Selection & Policy Bar
  // ---------------------------------------------------------------------------
  it('renders period selector, active workload policy chip, and synchronizes period changes', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="faculty-workload-workbench"]').exists()).toBe(true);

    // Policy chip displays institutional policy name & limits
    const policyChip = wrapper.find('[data-testid="active-policy-chip"]');
    expect(policyChip.exists()).toBe(true);
    expect(policyChip.text()).toContain('Primary Faculty Workload Policy');
    expect(policyChip.text()).toContain('WPOL-2026-PRIMARY');
    expect(policyChip.text()).toContain('Standard: 18h');
    expect(policyChip.text()).toContain('Warning: 20h');
    expect(policyChip.text()).toContain('Ceiling: 24h');

    // Period selector dropdown
    const periodDropdown = wrapper.findComponent('[data-testid="period-selector-dropdown"]');
    expect(periodDropdown.exists()).toBe(true);

    // Trigger period change
    const newPeriodId = '00000000-0000-0000-0002-000000000001';
    periodDropdown.vm.$emit('update:modelValue', newPeriodId);
    await flushPromises();

    expect(mockReplace).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ periodId: newPeriodId }),
      }),
    );
  });

  // ---------------------------------------------------------------------------
  // 2. KPI Threshold Cards & Interactive Filtering
  // ---------------------------------------------------------------------------
  it('renders KPI threshold cards with accurate counts and supports interactive filtering', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify KPI counts
    expect(wrapper.find('[data-testid="kpi-count-total"]').text()).toBe('4');
    expect(wrapper.find('[data-testid="kpi-count-normal"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="kpi-count-approaching"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="kpi-count-exception"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="kpi-count-breached"]').text()).toBe('1');

    // Click Warning KPI card to filter table to approaching_limit
    const warningCard = wrapper.find('[data-testid="kpi-card-approaching"]');
    expect(warningCard.exists()).toBe(true);
    await warningCard.trigger('click');
    await flushPromises();

    // Table now filters commitments
    expect(wrapper.text()).toContain('Siti Nurhaliza, S.Pd.');
    expect(wrapper.text()).not.toContain('Budi Hartono, M.Pd.');
  });

  // ---------------------------------------------------------------------------
  // 3. Faculty Capacity Progress Bars & Color Indicators
  // ---------------------------------------------------------------------------
  it('renders capacity utilization progress bars with proper width and semantic color scale', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const progressBars = wrapper.findAll('[data-testid="capacity-progress-bar"]');
    expect(progressBars.length).toBe(4);

    // 1st row (Budi Hartono: 16/24 hrs = 67% -> normal/green bg-emerald-500)
    expect(progressBars[0].attributes('class')).toContain('bg-emerald-500');

    // 2nd row (Siti Nurhaliza: 21/24 hrs = 88% -> warning/amber bg-amber-500)
    expect(progressBars[1].attributes('class')).toContain('bg-amber-500');

    // 3rd row (John Miller: 26/28 hrs = 93% -> exception/purple bg-purple-600)
    expect(progressBars[2].attributes('class')).toContain('bg-purple-600');

    // 4th row (Dewi Sartika: 25/24 hrs = 104% -> breached/red bg-rose-600)
    expect(progressBars[3].attributes('class')).toContain('bg-rose-600');
  });

  // ---------------------------------------------------------------------------
  // 4. Drill-Down Drawer Opening
  // ---------------------------------------------------------------------------
  it('opens line-item assignment drill-down drawer when clicking details action', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const detailsButtons = wrapper.findAll('[data-testid="workload-drilldown-button"]');
    expect(detailsButtons.length).toBeGreaterThan(0);

    await detailsButtons[0].trigger('click');
    await flushPromises();

    // Drawer is visible
    expect(wrapper.find('[data-testid="workload-drilldown-drawer"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Capacity Snapshot');
  });

  // ---------------------------------------------------------------------------
  // 5. RequestExceptionModal Interaction
  // ---------------------------------------------------------------------------
  it('opens and submits RequestExceptionModal for coordinator recommendation', async () => {
    const requestSpy = vi.spyOn(workloadApi, 'requestWorkloadException').mockResolvedValue({
      id: 'exc-new',
      exceptionNumber: 'EXC-2026-0009',
      teacherProfileId: sampleCommitments[1].teacherProfileId,
      academicPeriodId: sampleCommitments[1].academicPeriodId,
      requestedHours: 4,
      status: 'recommended',
      recommendationStatus: 'recommended',
      createdAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    const requestButtons = wrapper.findAll('[data-testid="workload-request-exception-button"]');
    expect(requestButtons.length).toBeGreaterThan(0);

    // Open Request Exception Modal
    await requestButtons[1].trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="request-exception-modal"]').exists()).toBe(true);

    // Enter justification and submit
    await wrapper
      .find('[data-testid="coordinator-justification-input"]')
      .setValue('Curriculum restructuring requires temporary additional 4 weekly hours.');

    await wrapper.find('[data-testid="submit-exception-btn"]').trigger('click');
    await flushPromises();

    expect(requestSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        teacherProfileId: sampleCommitments[1].teacherProfileId,
        academicPeriodId: sampleCommitments[1].academicPeriodId,
        requestedOverloadHours: 4,
      }),
    );
  });

  // ---------------------------------------------------------------------------
  // 6. ApproveExceptionModal Interaction
  // ---------------------------------------------------------------------------
  it('opens and confirms ApproveExceptionModal for authority decision', async () => {
    const approveSpy = vi.spyOn(workloadApi, 'approveWorkloadException').mockResolvedValue({
      id: 'exc-101',
      exceptionNumber: 'EXC-2026-0001',
      teachingAssignmentId: null,
      teacherProfileId: sampleCommitments[2].teacherProfileId,
      academicPeriodId: sampleCommitments[2].academicPeriodId,
      exceededHours: 4,
      status: 'approved',
      approvedBy: 'Academic Director',
      approvedDate: '2026-11-10',
      decisionRemarks: 'Approved for international visiting faculty assignment.',
      approvedExceptionCount: 1,
      commitmentStatus: 'overloaded_with_exception',
      resolvedConflictsCount: 1,
      approvedAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    const approveButtons = wrapper.findAll('[data-testid="workload-approve-exception-button"]');
    expect(approveButtons.length).toBeGreaterThan(0);

    // Click Approve button on John Miller (row with overloaded status)
    await approveButtons[0].trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="approve-exception-modal"]').exists()).toBe(true);

    // Enter decision remarks and confirm
    await wrapper
      .find('[data-testid="decision-remarks-input"]')
      .setValue('Approved for international visiting faculty assignment.');

    await wrapper.find('[data-testid="confirm-approve-btn"]').trigger('click');
    await flushPromises();

    expect(approveSpy).toHaveBeenCalledWith(
      'exc-101',
      expect.objectContaining({
        decisionRemarks: 'Approved for international visiting faculty assignment.',
      }),
    );
  });

  // ---------------------------------------------------------------------------
  // 7. On-Demand Recalculate Workload Button Trigger
  // ---------------------------------------------------------------------------
  it('triggers on-demand workload recalculation when clicking Recalculate Workload', async () => {
    const recalcSpy = vi.spyOn(workloadApi, 'recalculateWorkload').mockResolvedValue({
      academicPeriodId: '00000000-0000-0000-0002-000000000002',
      recalculatedCount: 4,
      recalculatedAt: '2026-11-10T00:00:00Z',
      message: 'Successfully recalculated 4 faculty commitments.',
    });

    const wrapper = createWrapper();
    await flushPromises();

    const recalcBtn = wrapper.find('[data-testid="recalculate-workload-btn"]');
    expect(recalcBtn.exists()).toBe(true);
    expect(recalcBtn.attributes('disabled')).toBeUndefined();

    await recalcBtn.trigger('click');
    await flushPromises();

    expect(recalcSpy).toHaveBeenCalledWith('00000000-0000-0000-0002-000000000002');
  });
});
