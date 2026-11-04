// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import { useAuthStore } from '@/app/stores/auth.store';
import StudentAccountsWorkbenchView from '../accounts/views/StudentAccountsWorkbenchView.vue';
import AccountBalanceCard from '../accounts/components/AccountBalanceCard.vue';
import ObligationsTable from '../accounts/components/ObligationsTable.vue';
import BillingDocumentsTable from '../accounts/components/BillingDocumentsTable.vue';
import AssessAdHocFeeModal from '../accounts/components/AssessAdHocFeeModal.vue';
import InvoiceDetailModal from '../accounts/components/InvoiceDetailModal.vue';
import FamilyBillingTab from '@/features/portal/FamilyBillingTab.vue';
import FamilyTasksView from '@/features/portal/FamilyTasksView.vue';
import * as accountsApi from '../accounts/api/accounts.api';
import * as obligationsApi from '../obligations/api/obligations.api';
import * as billingApi from '../billing/api/billing.api';
import * as academicYearApi from '@/features/operations/calendar/api/academicYear.api';
import * as operationalPeriodApi from '@/features/operations/calendar/api/operationalPeriod.api';
import * as paymentsApi from '../payments/api/payments.api';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockPush = vi.fn().mockResolvedValue(true);
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRouter: () => ({
      push: mockPush,
    }),
    useRoute: () => ({
      params: {},
      query: {},
    }),
  };
});

const mockStudentAccount: accountsApi.StudentFinancialAccountDto = {
  accountId: '11111111-1111-1111-1111-111111111111',
  accountNumber: 'ACC-2026-STUDENT1',
  studentId: '22222222-2222-2222-2222-222222222222',
  studentNumber: '2026-001',
  studentFullName: 'Ahmad Fauzi',
  campus: 'Kampus Utama',
  grade: 'SD 1A',
  currencyCode: 'IDR',
  accountStatus: 'active',
  openedDate: '2026-01-10',
  totalAssessed: 15750000,
  totalSettled: 15000000,
  currentBalance: 750000,
};

const mockAccountDossier: accountsApi.AccountSummaryDto = {
  accountId: '11111111-1111-1111-1111-111111111111',
  accountNumber: 'ACC-2026-STUDENT1',
  studentId: '22222222-2222-2222-2222-222222222222',
  studentNumber: '2026-001',
  studentFullName: 'Ahmad Fauzi',
  currencyCode: 'IDR',
  accountStatus: 'active',
  openedDate: '2026-01-10',
  closedDate: null,
  notes: null,
  createdAt: '2026-01-10T08:00:00Z',
  totalAssessed: 15750000,
  totalSettled: 15000000,
  currentBalance: 750000,
  openObligations: [
    {
      id: '33333333-3333-3333-3333-333333333333',
      obligationNumber: 'OBL-2026-000001',
      studentAccountId: '11111111-1111-1111-1111-111111111111',
      studentId: '22222222-2222-2222-2222-222222222222',
      chargeRuleId: '44444444-4444-4444-4444-444444444444',
      policyId: '55555555-5555-5555-5555-555555555555',
      description: 'SPP Bulanan Agustus 2026',
      chargeType: 'tuition',
      isMandatory: true,
      originalAmount: 750000,
      currencyCode: 'IDR',
      issueDate: '2026-08-01',
      dueDate: '2026-08-10',
      academicPeriodId: '66666666-6666-6666-6666-666666666666',
      academicPeriodLabel: 'Agustus 2026',
      status: 'assessed',
      authorizedBy: 'SYSTEM_CYCLE',
      createdAt: '2026-08-01T00:00:00Z',
    },
  ],
  recentBillingDocuments: [
    {
      id: '77777777-7777-7777-7777-777777777777',
      documentNumber: 'BIL-2026-00001',
      studentAccountId: '11111111-1111-1111-1111-111111111111',
      studentId: '22222222-2222-2222-2222-222222222222',
      documentType: 'bill',
      recipientName: 'Haji Sulaiman',
      recipientRelationship: 'ayah',
      recipientContact: '081234567890',
      billingPeriodStart: null,
      billingPeriodEnd: null,
      issueDate: '2026-08-02',
      paymentDueDate: '2026-08-15',
      totalBilledAmount: 750000,
      currencyCode: 'IDR',
      deliveryChannel: 'portal',
      deliveryStatus: 'issued',
      sentAt: null,
      createdAt: '2026-08-02T08:00:00Z',
    },
  ],
};

const mockBillingDocumentDetail: billingApi.BillingDocumentDto = {
  id: '77777777-7777-7777-7777-777777777777',
  documentNumber: 'BIL-2026-00001',
  studentAccountId: '11111111-1111-1111-1111-111111111111',
  studentId: '22222222-2222-2222-2222-222222222222',
  documentType: 'bill',
  recipientName: 'Haji Sulaiman',
  recipientRelationship: 'ayah',
  recipientContact: '081234567890',
  billingPeriodStart: null,
  billingPeriodEnd: null,
  issueDate: '2026-08-02',
  paymentDueDate: '2026-08-15',
  totalBilledAmount: 750000,
  currencyCode: 'IDR',
  deliveryChannel: 'portal',
  deliveryStatus: 'issued',
  sentAt: null,
  notes: 'Invoice SPP Agustus',
  createdAt: '2026-08-02T08:00:00Z',
  items: [
    {
      id: '88888888-8888-8888-8888-888888888888',
      billingDocumentId: '77777777-7777-7777-7777-777777777777',
      lineSequence: 1,
      itemType: 'obligation',
      referenceEntityType: 'financial_obligations',
      referenceEntityId: '33333333-3333-3333-3333-333333333333',
      description: 'SPP Bulanan Agustus 2026',
      amount: 750000,
      currencyCode: 'IDR',
      createdAt: '2026-08-02T08:00:00Z',
    },
  ],
};

const mockStudentPayment: paymentsApi.PaymentListItemDto = {
  id: 'pay-001',
  paymentNumber: 'PAY-2026-000001',
  receiptNumber: 'REC-2026-000099',
  studentAccountId: '11111111-1111-1111-1111-111111111111',
  accountNumber: 'ACC-2026-STUDENT1',
  studentId: '22222222-2222-2222-2222-222222222222',
  studentNumber: '2026-001',
  studentFullName: 'Ahmad Fauzi',
  applicantId: null,
  payerId: 'pyr-001',
  payerNumber: 'PYR-2026-0001',
  payerName: 'Bapak Ahmad',
  paymentAmount: 1500000,
  currencyCode: 'IDR',
  paymentMethod: 'bank_transfer',
  externalReference: 'TRF-BCA-001',
  paymentDate: '2026-08-05',
  submissionDate: '2026-08-05T08:00:00Z',
  confirmationStatus: 'confirmed',
  confirmationDate: '2026-08-05T08:30:00Z',
  confirmedBy: 'Cashier Siti',
  rejectionReason: null,
  notes: null,
  totalAllocated: 1500000,
  unallocatedAmount: 0,
};

const mockPaymentDetail: paymentsApi.PaymentDto = {
  id: 'pay-001',
  paymentNumber: 'PAY-2026-000001',
  receiptNumber: 'REC-2026-000099',
  studentAccountId: '11111111-1111-1111-1111-111111111111',
  accountNumber: 'ACC-2026-STUDENT1',
  studentId: '22222222-2222-2222-2222-222222222222',
  studentNumber: '2026-001',
  studentFullName: 'Ahmad Fauzi',
  applicantId: null,
  payerId: 'pyr-001',
  payerNumber: 'PYR-2026-0001',
  payerName: 'Bapak Ahmad',
  paymentAmount: 1500000,
  currencyCode: 'IDR',
  paymentMethod: 'bank_transfer',
  externalReference: 'TRF-BCA-001',
  paymentDate: '2026-08-05',
  submissionDate: '2026-08-05T08:00:00Z',
  confirmationStatus: 'confirmed',
  confirmationDate: '2026-08-05T08:30:00Z',
  confirmedBy: 'Cashier Siti',
  rejectionReason: null,
  notes: null,
  totalAllocated: 1500000,
  unallocatedAmount: 0,
  allocations: [
    {
      id: 'alc-001',
      allocationNumber: 'ALC-2026-000001',
      paymentId: 'pay-001',
      obligationId: '33333333-3333-3333-3333-333333333333',
      obligationDescription: 'SPP Bulanan Agustus 2026',
      allocatedAmount: 750000,
      currencyCode: 'IDR',
      allocatedAt: '2026-08-05T08:30:00Z',
      status: 'active',
      supersededById: null,
      reversalReason: null,
    },
  ],
};

describe('Student Accounts Workbench (SCR-FIN-03) & Family Portal (SCR-SELF-02)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    document.body.innerHTML = '';
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

    const authStore = useAuthStore();
    authStore.setSession('token-finance-officer', {
      userId: 'user-finance-officer',
      username: 'finance.officer',
      fullName: 'Finance Officer',
      roles: ['FinanceOfficer'],
      permissions: [
        'Permissions.Finance.Accounts.View',
        'Permissions.Finance.Obligations.View',
        'Permissions.Finance.Obligations.Assess',
        'Permissions.Finance.Billing.View',
        'Permissions.Finance.Billing.Generate',
        'Permissions.Finance.Payments.View',
        'Permissions.Finance.Allocations.View',
        'Permissions.Finance.Allocations.Reallocate',
      ],
      campusId: null,
    });

    vi.spyOn(paymentsApi, 'getPayments').mockResolvedValue({
      items: [mockStudentPayment],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    vi.spyOn(paymentsApi, 'getPaymentById').mockResolvedValue(mockPaymentDetail);

    vi.spyOn(accountsApi, 'searchStudentAccounts').mockResolvedValue({
      items: [mockStudentAccount],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    vi.spyOn(accountsApi, 'getStudentAccount').mockResolvedValue(mockAccountDossier);
    vi.spyOn(billingApi, 'getBillingDocumentById').mockResolvedValue(mockBillingDocumentDetail);
    vi.spyOn(billingApi, 'getBillingDocuments').mockResolvedValue({
      items: [mockBillingDocumentDetail],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue([
      {
        id: 'ay-2026',
        code: 'AY-2026-2027',
        name: 'Tahun Pelajaran 2026/2027',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'active',
        isActive: true,
      },
    ]);

    vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue([
      {
        id: '66666666-6666-6666-6666-666666666666',
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
    ]);

    vi.spyOn(obligationsApi, 'assessAdHocObligation').mockResolvedValue({
      id: 'obl-adhoc-1',
      obligationNumber: 'OBL-2026-000099',
      studentAccountId: '11111111-1111-1111-1111-111111111111',
      studentId: '22222222-2222-2222-2222-222222222222',
      chargeRuleId: null,
      policyId: null,
      chargeType: 'general',
      description: 'Penggantian Kartu Pelajar Hilang',
      originalAmount: 50000,
      currencyCode: 'IDR',
      dueDate: '2026-08-20',
      academicPeriodId: '66666666-6666-6666-6666-666666666666',
      academicPeriodLabel: 'Agustus 2026',
      status: 'assessed',
      initialScheduleId: 'sch-001',
      createdAt: '2026-08-01T00:00:00Z',
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.body.innerHTML = '';
  });

  function mountComponent(Component: any, props = {}) {
    return mount(Component, {
      attachTo: document.body,
      props,
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
        },
      },
    });
  }

  // ---------------------------------------------------------------------------
  // 1. AccountBalanceCard.vue
  // ---------------------------------------------------------------------------
  describe('AccountBalanceCard.vue', () => {
    it('renders account number, status badge, and derived financial ledger amounts', () => {
      const wrapper = mountComponent(AccountBalanceCard, {
        account: mockAccountDossier,
      });

      expect(wrapper.find('[data-testid="account-number"]').text()).toBe('ACC-2026-STUDENT1');
      expect(wrapper.find('[data-testid="account-status-badge"]').text()).toContain('Active');
      expect(wrapper.find('[data-testid="total-assessed-amount"]').text()).toBe('Rp 15.750.000');
      expect(wrapper.find('[data-testid="total-settled-amount"]').text()).toBe('Rp 15.000.000');
      expect(wrapper.find('[data-testid="current-balance-amount"]').text()).toBe('Rp 750.000');
    });

    it('emits assessFee and generateBill events when action buttons are clicked', async () => {
      const wrapper = mountComponent(AccountBalanceCard, {
        account: mockAccountDossier,
      });

      await wrapper.find('[data-testid="assess-fee-btn"]').trigger('click');
      expect(wrapper.emitted('assessFee')).toBeTruthy();

      await wrapper.find('[data-testid="generate-bill-btn"]').trigger('click');
      expect(wrapper.emitted('generateBill')).toBeTruthy();
    });
  });

  // ---------------------------------------------------------------------------
  // 2. ObligationsTable.vue
  // ---------------------------------------------------------------------------
  describe('ObligationsTable.vue', () => {
    it('renders obligations list with formatted amount, due date, and status', () => {
      const wrapper = mountComponent(ObligationsTable, {
        obligations: mockAccountDossier.openObligations,
      });

      expect(wrapper.text()).toContain('OBL-2026-000001');
      expect(wrapper.text()).toContain('SPP Bulanan Agustus 2026');
      expect(wrapper.text()).toContain('Rp 750.000');
      expect(wrapper.text()).toContain('2026-08-10');
      expect(wrapper.text()).toContain('Assessed');
    });

    it('triggers generateBill when compile all button is clicked', async () => {
      const wrapper = mountComponent(ObligationsTable, {
        obligations: mockAccountDossier.openObligations,
      });

      const compileBtn = wrapper.find('[data-testid="table-generate-bill-btn"]');
      expect(compileBtn.exists()).toBe(true);
      await compileBtn.trigger('click');

      expect(wrapper.emitted('generateBill')).toBeTruthy();
      expect(wrapper.emitted('generateBill')![0][0]).toEqual([
        '33333333-3333-3333-3333-333333333333',
      ]);
    });
  });

  // ---------------------------------------------------------------------------
  // 3. BillingDocumentsTable.vue
  // ---------------------------------------------------------------------------
  describe('BillingDocumentsTable.vue', () => {
    it('renders recent billing documents and emits viewInvoice on action click', async () => {
      const wrapper = mountComponent(BillingDocumentsTable, {
        documents: mockAccountDossier.recentBillingDocuments,
      });

      expect(wrapper.text()).toContain('BIL-2026-00001');
      expect(wrapper.text()).toContain('Haji Sulaiman');
      expect(wrapper.text()).toContain('Rp 750.000');

      const viewBtn = wrapper.find('[data-testid="view-invoice-action-btn"]');
      expect(viewBtn.exists()).toBe(true);
      await viewBtn.trigger('click');

      expect(wrapper.emitted('viewInvoice')).toBeTruthy();
      expect(wrapper.emitted('viewInvoice')![0][0]).toBe('77777777-7777-7777-7777-777777777777');
    });
  });

  // ---------------------------------------------------------------------------
  // 4. AssessAdHocFeeModal.vue
  // ---------------------------------------------------------------------------
  describe('AssessAdHocFeeModal.vue', () => {
    it('renders student header info and form inputs for ad-hoc assessment', async () => {
      mountComponent(AssessAdHocFeeModal, {
        visible: true,
        studentId: '22222222-2222-2222-2222-222222222222',
        studentName: 'Ahmad Fauzi',
        studentNumber: '2026-001',
        defaultAcademicPeriodId: '66666666-6666-6666-6666-666666666666',
      });

      await flushPromises();

      expect(document.querySelector('[data-testid="assess-ad-hoc-fee-modal"]')).not.toBeNull();
      expect(document.querySelector('[data-testid="modal-student-name"]')?.textContent).toContain('Ahmad Fauzi');
      expect(document.querySelector('[data-testid="ad-hoc-description-input"]')).not.toBeNull();
      expect(document.querySelector('[data-testid="ad-hoc-amount-input"]')).not.toBeNull();
      expect(document.querySelector('[data-testid="submit-ad-hoc-fee-btn"]')).not.toBeNull();
    });

    it('validates and submits ad-hoc charge modal, invoking mutation and emitting assessed event', async () => {
      const wrapper = mountComponent(AssessAdHocFeeModal, {
        visible: true,
        studentId: '22222222-2222-2222-2222-222222222222',
        studentName: 'Ahmad Fauzi',
        studentNumber: '2026-001',
        defaultAcademicPeriodId: '66666666-6666-6666-6666-666666666666',
      });

      await flushPromises();

      const descInput = document.querySelector('[data-testid="ad-hoc-description-input"]') as HTMLInputElement;
      const amountInput = document.querySelector('[data-testid="ad-hoc-amount-input"]') as HTMLInputElement;

      descInput.value = 'Penggantian Kartu Pelajar Hilang';
      descInput.dispatchEvent(new Event('input'));
      amountInput.value = '50000';
      amountInput.dispatchEvent(new Event('input'));
      await flushPromises();

      const submitBtn = document.querySelector('[data-testid="submit-ad-hoc-fee-btn"]') as HTMLButtonElement;
      submitBtn.click();
      await flushPromises();
      await new Promise((resolve) => setTimeout(resolve, 100));
      await flushPromises();

      expect(obligationsApi.assessAdHocObligation).toHaveBeenCalledWith(
        expect.objectContaining({
          studentId: '22222222-2222-2222-2222-222222222222',
          description: 'Penggantian Kartu Pelajar Hilang',
          amount: 50000,
          academicPeriodId: '66666666-6666-6666-6666-666666666666',
        }),
      );
      expect(wrapper.emitted('assessed')).toBeTruthy();
    });
  });

  // ---------------------------------------------------------------------------
  // 5. InvoiceDetailModal.vue
  // ---------------------------------------------------------------------------
  describe('InvoiceDetailModal.vue', () => {
    it('fetches document detail and renders recipient and frozen itemized table', async () => {
      mountComponent(InvoiceDetailModal, {
        visible: true,
        documentId: '77777777-7777-7777-7777-777777777777',
      });

      await flushPromises();

      expect(document.querySelector('[data-testid="invoice-document-number"]')?.textContent).toBe('BIL-2026-00001');
      expect(document.querySelector('[data-testid="invoice-recipient-name"]')?.textContent).toBe('Haji Sulaiman');
      expect(document.querySelector('[data-testid="invoice-total-amount"]')?.textContent).toBe('Rp 750.000');
      expect(document.querySelector('[data-testid="invoice-items-table"]')?.textContent).toContain(
        'SPP Bulanan Agustus 2026',
      );
    });
  });

  // ---------------------------------------------------------------------------
  // 6. StudentAccountsWorkbenchView.vue Full Flow
  // ---------------------------------------------------------------------------
  describe('StudentAccountsWorkbenchView.vue Full View Flow', () => {
    it('renders workbench directory table and search autocomplete', async () => {
      const wrapper = mountComponent(StudentAccountsWorkbenchView);
      await flushPromises();

      expect(wrapper.find('[data-testid="student-search-autocomplete"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="accounts-directory-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Ahmad Fauzi');
      expect(wrapper.text()).toContain('ACC-2026-STUDENT1');
      expect(wrapper.text()).toContain('Rp 750.000');
    });

    it('selecting a student from directory switches view to 360-degree ledger dossier', async () => {
      const wrapper = mountComponent(StudentAccountsWorkbenchView);
      await flushPromises();

      // Click "Inspect" button
      const inspectBtn = wrapper.find('[data-testid="select-student-action-btn"]');
      expect(inspectBtn.exists()).toBe(true);
      await inspectBtn.trigger('click');
      await flushPromises();

      // Should now render dossier components
      expect(wrapper.find('[data-testid="account-balance-card"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="obligations-table-container"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="current-balance-amount"]').text()).toBe('Rp 750.000');
    });

    it('opens ad-hoc fee modal when Assess Fee button is clicked on the account balance card', async () => {
      const wrapper = mountComponent(StudentAccountsWorkbenchView);
      await flushPromises();

      // Inspect student
      await wrapper.find('[data-testid="select-student-action-btn"]').trigger('click');
      await flushPromises();

      // Click assess fee on AccountBalanceCard
      const assessBtn = wrapper.find('[data-testid="assess-fee-btn"]');
      expect(assessBtn.exists()).toBe(true);
      await assessBtn.trigger('click');
      await flushPromises();

      expect(document.querySelector('[data-testid="assess-ad-hoc-fee-modal"]')).not.toBeNull();
    });

    it('renders Payments history table with correct receipt number and status pill when switching to Payments tab', async () => {
      const wrapper = mountComponent(StudentAccountsWorkbenchView);
      await flushPromises();

      // Inspect student
      await wrapper.find('[data-testid="select-student-action-btn"]').trigger('click');
      await flushPromises();

      // Switch to Payments tab
      const paymentsTab = wrapper.find('[data-testid="tab-payments"]');
      expect(paymentsTab.exists()).toBe(true);
      await paymentsTab.trigger('click');
      await flushPromises();

      // Assert payments table container is rendered
      expect(wrapper.find('[data-testid="payments-history-table-container"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('REC-2026-000099');
      expect(wrapper.text()).toContain('PAY-2026-000001');
      expect(wrapper.text()).toContain('Confirmed');
      expect(wrapper.text()).toContain('Rp 1.500.000');
    });

    it('renders Allocations table with correct allocation number, obligation description, and status pill when switching to Allocations tab', async () => {
      const wrapper = mountComponent(StudentAccountsWorkbenchView);
      await flushPromises();

      // Inspect student
      await wrapper.find('[data-testid="select-student-action-btn"]').trigger('click');
      await flushPromises();

      // Switch to Allocations tab
      const allocationsTab = wrapper.find('[data-testid="tab-allocations"]');
      expect(allocationsTab.exists()).toBe(true);
      await allocationsTab.trigger('click');
      await flushPromises();

      // Assert allocations table container is rendered
      expect(wrapper.find('[data-testid="allocations-table-container"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('ALC-2026-000001');
      expect(wrapper.text()).toContain('SPP Bulanan Agustus 2026');
      expect(wrapper.text()).toContain('Active');
      expect(wrapper.text()).toContain('Rp 750.000');
    });
  });

  // ---------------------------------------------------------------------------
  // 7. Family Portal Extension (FamilyBillingTab.vue & FamilyTasksView.vue)
  // ---------------------------------------------------------------------------
  describe('Family Portal Extension (SCR-SELF-02)', () => {
    it('renders total outstanding balance and list of bills in FamilyBillingTab', async () => {
      const wrapper = mountComponent(FamilyBillingTab, {
        studentId: '22222222-2222-2222-2222-222222222222',
        initialDocuments: [mockBillingDocumentDetail],
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="family-outstanding-amount"]').text()).toBe('Rp 750.000');
      expect(wrapper.find('[data-testid="family-bills-list"]').text()).toContain('BIL-2026-00001');
      expect(wrapper.find('[data-testid="bill-amount-display"]').text()).toBe('Rp 750.000');
    });

    it('renders FamilyTasksView with embedded FamilyBillingTab', async () => {
      const wrapper = mountComponent(FamilyTasksView);
      await flushPromises();

      expect(wrapper.find('[data-testid="family-tasks-view"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Family Tasks & Requests Portal');
      expect(wrapper.find('[data-testid="family-billing-tab"]').exists()).toBe(true);
    });
  });
});
