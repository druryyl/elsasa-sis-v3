// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import { useAuthStore } from '@/app/stores/auth.store';
import BillingCycleWorkspaceView from '../billing/views/BillingCycleWorkspaceView.vue';
import BillingCycleStepper from '../billing/components/BillingCycleStepper.vue';
import CandidatePreviewTable from '../billing/components/CandidatePreviewTable.vue';
import * as obligationsApi from '../obligations/api/obligations.api';
import * as policiesApi from '../policies/api/policies.api';
import * as academicYearApi from '@/features/operations/calendar/api/academicYear.api';
import * as operationalPeriodApi from '@/features/operations/calendar/api/operationalPeriod.api';
import * as billingApi from '../billing/api/billing.api';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockPush = vi.fn();
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRouter: () => ({
      push: mockPush,
    }),
  };
});

const mockAcademicYears: academicYearApi.AcademicYearDto[] = [
  {
    id: 'ay-2026',
    code: 'AY-2026-2027',
    name: 'Tahun Pelajaran 2026/2027',
    startDate: '2026-07-01',
    endDate: '2027-06-30',
    status: 'active',
    isActive: true,
  },
];

const mockPeriods: operationalPeriodApi.OperationalPeriodDto[] = [
  {
    id: 'op-period-1',
    academicYearId: 'ay-2026',
    periodCode: 'TP26-AUG',
    name: 'Agustus 2026',
    periodType: 'month',
    startDate: '2026-08-01',
    endDate: '2026-08-31',
    parentPeriodId: null,
    campusContext: 'All',
    status: 'active',
  },
];

const mockPolicies = [
  {
    id: 'pol-1',
    policyCode: 'FP-2026-001',
    name: 'Kebijakan Biaya 2026',
    category: 'tuition',
    businessPurpose: 'Tuition governance',
    applicableContext: null,
    effectiveDate: '2026-07-01',
    endDate: null,
    policyVersion: '1.0',
    status: 'active',
    approvingAuthority: 'Direktur',
    approvedDate: '2026-06-15',
    supersedesPolicyId: null,
    notes: null,
    createdAt: '2026-06-15T00:00:00Z',
    createdBy: 'system',
    rules: [
      {
        id: 'rule-spp-sd',
        ruleCode: 'CR-2026-SPP-SD',
        policyId: 'pol-1',
        name: 'SPP Bulanan SD',
        chargeType: 'tuition',
        isMandatory: true,
        determinationMethod: 'fixed_amount',
        baseAmount: 750000,
        currencyCode: 'IDR',
        recurrencePattern: 'monthly',
        dueRuleType: 'days_after_issuance',
        dueRuleValue: '10',
        applicableGrade: 'Grade 1',
        applicableProgram: 'Primary',
        status: 'active',
      },
    ],
  },
];

const mockCandidates = [
  {
    studentId: 'stu-001',
    studentNumber: 'ELS-2026-0001',
    studentFullName: 'Ahmad Dahlan',
    campus: 'Main Campus (Kampus A)',
    grade: 'Grade 1',
    accountId: 'acc-001',
    accountNumber: 'ACC-2026-0001',
    baseAmount: 750000,
    dueDate: '2026-08-10',
    isDuplicate: false,
  },
  {
    studentId: 'stu-002',
    studentNumber: 'ELS-2026-0002',
    studentFullName: 'Budi Santoso',
    campus: 'Main Campus (Kampus A)',
    grade: 'Grade 1',
    accountId: 'acc-002',
    accountNumber: 'ACC-2026-0002',
    baseAmount: 750000,
    dueDate: '2026-08-10',
    isDuplicate: true, // duplicate student
  },
];

const mockPreviewResponse = {
  totalCandidatesCount: 2,
  eligibleCandidatesCount: 1,
  duplicateCount: 1,
  grossAssessedAmount: 750000,
  candidates: mockCandidates,
  createdObligationIds: [],
};

const mockCommitResponse = {
  totalCandidatesCount: 2,
  eligibleCandidatesCount: 1,
  duplicateCount: 1,
  grossAssessedAmount: 750000,
  candidates: mockCandidates,
  createdObligationIds: ['obl-uuid-001'],
};

describe('Billing Cycle Workspace (SCR-FIN-01)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      window.matchMedia = (query: string) =>
        ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }) as unknown as MediaQueryList;

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }

    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockAcademicYears);
    vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue(mockPeriods);
    vi.spyOn(policiesApi, 'getPolicies').mockResolvedValue(mockPolicies);
    vi.spyOn(obligationsApi, 'batchAssessObligations').mockImplementation((req) => {
      if (req.mode === 'commit') {
        return Promise.resolve(mockCommitResponse);
      }
      return Promise.resolve(mockPreviewResponse);
    });
    vi.spyOn(billingApi, 'generateBill').mockResolvedValue({
      id: 'doc-1',
      documentNumber: 'BIL-2026-000001',
      studentAccountId: 'acc-001',
      studentId: 'stu-001',
      documentType: 'bill',
      recipientName: 'Santoso Parent',
      recipientRelationship: 'Father',
      recipientContact: '+62812345678',
      billingPeriodStart: '2026-08-01',
      billingPeriodEnd: '2026-08-31',
      issueDate: '2026-08-01',
      paymentDueDate: '2026-08-15',
      totalBilledAmount: 750000,
      currencyCode: 'IDR',
      deliveryChannel: 'portal',
      deliveryStatus: 'issued',
      sentAt: null,
      notes: null,
      items: [],
      createdAt: '2026-08-01T00:00:00Z',
      createdBy: 'FinanceOfficer',
    });

    const authStore = useAuthStore();
    authStore.setSession('token-xyz', {
      userId: 'usr-1',
      username: 'fofficer',
      fullName: 'Finance Officer',
      roles: ['FinanceOfficer'],
      permissions: ['Permissions.Finance.Obligations.Assess', 'Permissions.Finance.Billing.Generate'],
      campusId: 'campus-1',
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function createViewWrapper() {
    return mount(BillingCycleWorkspaceView, {
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: {
            template: '<a :href="to"><slot /></a>',
            props: ['to'],
          },
        },
      },
    });
  }

  describe('BillingCycleStepper.vue', () => {
    it('renders all 4 steps with titles and step indicators', () => {
      const wrapper = mount(BillingCycleStepper, {
        props: { currentStep: 2 },
      });

      expect(wrapper.find('[data-testid="step-indicator-1"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="step-indicator-2"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="step-indicator-3"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="step-indicator-4"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Scope & Parameters');
      expect(wrapper.text()).toContain('Candidate Preview');
      expect(wrapper.text()).toContain('Confirm & Commit');
      expect(wrapper.text()).toContain('Completion & Next Actions');
    });

    it('emits stepSelect when clicking a previously completed step', async () => {
      const wrapper = mount(BillingCycleStepper, {
        props: { currentStep: 3, canNavigateBack: true },
      });

      await wrapper.find('[data-testid="step-indicator-1"]').trigger('click');
      expect(wrapper.emitted('update:currentStep')).toBeTruthy();
      expect(wrapper.emitted('update:currentStep')![0]).toEqual([1]);
    });
  });

  describe('CandidatePreviewTable.vue', () => {
    it('renders preview candidates with formatted IDR amount and duplicate badge', () => {
      const wrapper = mount(CandidatePreviewTable, {
        props: {
          candidates: mockCandidates,
          loading: false,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      expect(wrapper.find('[data-testid="candidate-preview-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Ahmad Dahlan');
      expect(wrapper.text()).toContain('ELS-2026-0001');
      expect(wrapper.text()).toContain('Rp 750.000');

      // Duplicate badge verification
      const dupBadges = wrapper.findAll('[data-testid="duplicate-badge"]');
      expect(dupBadges.length).toBe(1);
      expect(dupBadges[0].text()).toContain('Duplicate (Excluded)');

      // Eligible badge verification
      const eligibleBadges = wrapper.findAll('[data-testid="eligible-badge"]');
      expect(eligibleBadges.length).toBe(1);
      expect(eligibleBadges[0].text()).toContain('Eligible');
    });

    it('filters candidate rows based on search input', async () => {
      const wrapper = mount(CandidatePreviewTable, {
        props: {
          candidates: mockCandidates,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      const searchInput = wrapper.find('[data-testid="candidate-search-input"]');
      await searchInput.setValue('Ahmad');

      expect(wrapper.text()).toContain('Ahmad Dahlan');
      expect(wrapper.text()).not.toContain('Budi Santoso');
    });
  });

  describe('BillingCycleWorkspaceView.vue Full Flow', () => {
    it('renders Step 1 form fields and loads initial data', async () => {
      const wrapper = createViewWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="billing-cycle-workspace-view"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="step-1-container"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="charge-rule-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="billing-cycle-label-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="evaluate-preview-btn"]').exists()).toBe(true);
    });

    it('validates required fields before executing preview mutation', async () => {
      const wrapper = createViewWrapper();
      await flushPromises();

      // Trigger preview without selecting rule or period
      await wrapper.find('[data-testid="evaluate-preview-btn"]').trigger('click');
      await flushPromises();

      // Should remain on Step 1 due to validation errors
      expect(wrapper.find('[data-testid="step-1-container"]').exists()).toBe(true);
      expect(obligationsApi.batchAssessObligations).not.toHaveBeenCalled();
    });

    it('executes preview mutation and advances to Step 2 displaying statistics', async () => {
      const wrapper = createViewWrapper();
      await flushPromises();

      // Set input values
      const vm = wrapper.vm as any;
      vm.selectedAcademicYearId = 'ay-2026';
      vm.selectedPeriodId = 'op-period-1';
      vm.selectedChargeRuleId = 'rule-spp-sd';
      vm.billingCycleLabel = 'Batch: SPP Bulanan SD - Agustus 2026';
      await wrapper.vm.$nextTick();

      // Click evaluate & preview
      await wrapper.find('[data-testid="evaluate-preview-btn"]').trigger('click');
      await flushPromises();

      // Advanced to Step 2
      expect(wrapper.find('[data-testid="step-2-container"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="stat-total-candidates"]').text()).toBe('2');
      expect(wrapper.find('[data-testid="stat-eligible-candidates"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="stat-duplicate-count"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="stat-gross-amount"]').text()).toContain('Rp 750.000');
      expect(wrapper.find('[data-testid="duplicate-warning-banner"]').exists()).toBe(true);
    });

    it('navigates from Step 2 to Step 3 Confirmation and executes commit to Step 4', async () => {
      const wrapper = createViewWrapper();
      await flushPromises();

      const vm = wrapper.vm as any;
      vm.selectedAcademicYearId = 'ay-2026';
      vm.selectedPeriodId = 'op-period-1';
      vm.selectedChargeRuleId = 'rule-spp-sd';
      vm.billingCycleLabel = 'Batch: SPP Bulanan SD - Agustus 2026';
      vm.previewResult = mockPreviewResponse;
      vm.currentStep = 2;
      await wrapper.vm.$nextTick();

      // Proceed to Step 3
      const proceedBtn = wrapper.find('[data-testid="proceed-to-step3-btn"]');
      expect(proceedBtn.exists()).toBe(true);
      await proceedBtn.trigger('click');
      await flushPromises();

      // Step 3 Confirmation
      expect(wrapper.find('[data-testid="step-3-container"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="confirm-gross-amount"]').text()).toContain('Rp 750.000');

      // Click Commit & Post Obligations
      const commitBtn = wrapper.find('[data-testid="commit-assessment-btn"]');
      await commitBtn.trigger('click');
      await flushPromises();

      expect(obligationsApi.batchAssessObligations).toHaveBeenCalledWith(
        expect.objectContaining({
          mode: 'commit',
          chargeRuleId: 'rule-spp-sd',
          academicPeriodId: 'op-period-1',
          billingCycleLabel: expect.stringContaining('Batch: SPP Bulanan SD'),
        }),
      );

      // Step 4 Completion Summary
      expect(wrapper.find('[data-testid="step-4-container"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="summary-eligible-count"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="summary-gross-amount"]').text()).toContain('Rp 750.000');
      expect(wrapper.find('[data-testid="summary-duplicate-count"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="trigger-bill-generation-btn"]').exists()).toBe(true);
    });

    it('resets wizard back to Step 1 on reset click', async () => {
      const wrapper = createViewWrapper();
      await flushPromises();

      const vm = wrapper.vm as any;
      vm.currentStep = 4;
      vm.commitResult = mockCommitResponse;
      await wrapper.vm.$nextTick();

      const resetBtn = wrapper.find('[data-testid="start-new-cycle-btn"]');
      await resetBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="step-1-container"]').exists()).toBe(true);
    });

    it('triggers batch bill generation dialog and executes bill compiling', async () => {
      const wrapper = createViewWrapper();
      await flushPromises();

      const vm = wrapper.vm as any;
      vm.currentStep = 4;
      vm.commitResult = mockCommitResponse;
      vm.billingCycleLabel = 'Batch: SPP Bulanan';
      await wrapper.vm.$nextTick();

      const openBillModalBtn = wrapper.find('[data-testid="trigger-bill-generation-btn"]');
      await openBillModalBtn.trigger('click');
      await flushPromises();

      const dialogEl = document.body.querySelector('[data-testid="batch-bill-dialog"]');
      expect(dialogEl !== null).toBe(true);

      const confirmBillBtn = document.body.querySelector(
        '[data-testid="confirm-generate-bills-btn"]',
      ) as HTMLButtonElement;
      expect(confirmBillBtn !== null).toBe(true);
      confirmBillBtn.click();
      await flushPromises();

      expect(billingApi.generateBill).toHaveBeenCalledWith(
        expect.objectContaining({
          studentId: 'stu-001',
        }),
      );
      expect(wrapper.find('[data-testid="bills-generated-badge"]').exists()).toBe(true);
    });
  });
});
