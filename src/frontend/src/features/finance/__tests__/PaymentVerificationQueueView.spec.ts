// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Tooltip from 'primevue/tooltip';
import ToastService from 'primevue/toastservice';
import PaymentVerificationQueueView from '../payments/views/PaymentVerificationQueueView.vue';
import ConfirmPaymentModal from '../payments/components/ConfirmPaymentModal.vue';
import RejectPaymentModal from '../payments/components/RejectPaymentModal.vue';
import AllocatePaymentModal from '../payments/components/AllocatePaymentModal.vue';
import ReceiptVoucherViewer from '../payments/components/ReceiptVoucherViewer.vue';
import RecordPaymentModal from '../payments/components/RecordPaymentModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { FINANCE_PAYMENT_PERMISSIONS, FINANCE_ALLOCATION_PERMISSIONS } from '../payments/constants/permissions';
import type { PaymentListItemDto } from '../payments/types/payments.types';

// Mock API clients and composables
const mockPayments: PaymentListItemDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    paymentNumber: 'PAY-2026-000001',
    receiptNumber: null,
    studentAccountId: 'acc-1',
    accountNumber: 'ACC-2026-0001',
    studentId: 'stu-1',
    studentNumber: '2026001',
    studentFullName: 'Ahmad Dahlan',
    applicantId: null,
    payerId: 'pyr-1',
    payerNumber: 'PYR-2026-0001',
    payerName: 'Budi Santoso',
    paymentAmount: 1500000,
    currencyCode: 'IDR',
    paymentMethod: 'bank_transfer',
    externalReference: 'TRF-BCA-981',
    paymentDate: '2026-11-04',
    submissionDate: '2026-11-04T08:00:00Z',
    confirmationStatus: 'submitted',
    confirmationDate: null,
    confirmedBy: null,
    rejectionReason: null,
    notes: 'Payment via mobile banking',
    totalAllocated: 0,
    unallocatedAmount: 1500000,
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    paymentNumber: 'PAY-2026-000002',
    receiptNumber: 'REC-2026-000042',
    studentAccountId: 'acc-2',
    accountNumber: 'ACC-2026-0002',
    studentId: null,
    studentNumber: null,
    studentFullName: null,
    applicantId: 'app-1',
    payerId: 'pyr-2',
    payerNumber: 'PYR-2026-0002',
    payerName: 'Siti Aminah',
    paymentAmount: 2500000,
    currencyCode: 'IDR',
    paymentMethod: 'cash',
    externalReference: 'CASH-COUNTER-01',
    paymentDate: '2026-11-04',
    submissionDate: '2026-11-04T09:00:00Z',
    confirmationStatus: 'confirmed',
    confirmationDate: '2026-11-04T09:01:00Z',
    confirmedBy: 'Cashier User',
    rejectionReason: null,
    notes: 'Walk-in cash settlement',
    totalAllocated: 1500000,
    unallocatedAmount: 1000000,
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    paymentNumber: 'PAY-2026-000003',
    receiptNumber: null,
    studentAccountId: 'acc-3',
    accountNumber: 'ACC-2026-0003',
    studentId: 'stu-3',
    studentNumber: '2026003',
    studentFullName: 'Dewi Sartika',
    applicantId: null,
    payerId: 'pyr-3',
    payerNumber: 'PYR-2026-0003',
    payerName: 'Yayasan Peduli',
    paymentAmount: 5000000,
    currencyCode: 'IDR',
    paymentMethod: 'bank_transfer',
    externalReference: 'SLIP-INVALID',
    paymentDate: '2026-11-03',
    submissionDate: '2026-11-03T10:00:00Z',
    confirmationStatus: 'rejected',
    confirmationDate: null,
    confirmedBy: null,
    rejectionReason: 'Bank transfer slip unreadable and funds not found on statement',
    notes: null,
    totalAllocated: 0,
    unallocatedAmount: 0,
  },
];

vi.mock('../payments/composables/useFinancePayments', () => ({
  usePaymentsQuery: (filtersRef: any) => ({
    data: {
      value: {
        items: mockPayments,
        totalCount: mockPayments.length,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 1,
      },
    },
    isPending: { value: false },
    isFetching: { value: false },
    isError: { value: false },
    refetch: vi.fn(),
  }),
  usePaymentByIdQuery: () => ({
    data: { value: null },
    isPending: { value: false },
  }),
  usePaymentReceiptQuery: () => ({
    data: { value: null },
    isPending: { value: false },
    isError: { value: false },
    error: { value: null },
  }),
  useSubmitPaymentMutation: () => ({
    mutate: vi.fn(),
    isPending: { value: false },
  }),
  useRecordPaymentMutation: () => ({
    mutate: vi.fn(),
    isPending: { value: false },
  }),
  useConfirmPaymentMutation: () => ({
    mutate: vi.fn(),
    isPending: { value: false },
  }),
  useRejectPaymentMutation: () => ({
    mutate: vi.fn(),
    isPending: { value: false },
  }),
}));

vi.mock('../payers/composables/useFinancePayers', () => ({
  usePayersQuery: () => ({
    data: { value: { items: [], totalCount: 0 } },
    isPending: { value: false },
  }),
}));

vi.mock('../allocations/composables/useFinanceAllocations', () => ({
  useStudentReceivablesQuery: () => ({
    data: { value: { receivables: [] } },
    isPending: { value: false },
  }),
  useSuggestAllocationsQuery: () => ({
    data: { value: null },
    isPending: { value: false },
  }),
  useAllocatePaymentMutation: () => ({
    mutate: vi.fn(),
    isPending: { value: false },
  }),
  useReallocatePaymentMutation: () => ({
    mutate: vi.fn(),
    isPending: { value: false },
  }),
}));

describe('PaymentVerificationQueueView.vue (SCR-FIN-02)', () => {
  beforeEach(() => {
    window.matchMedia =
      window.matchMedia ||
      ((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })) as any;

    setActivePinia(createPinia());
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      id: 'cashier-user-id',
      email: 'cashier@elsasa.sch.id',
      fullName: 'Finance Cashier',
      roles: ['FinanceOfficer'],
      permissions: [
        FINANCE_PAYMENT_PERMISSIONS.VIEW,
        FINANCE_PAYMENT_PERMISSIONS.RECORD,
        FINANCE_PAYMENT_PERMISSIONS.CONFIRM,
        FINANCE_PAYMENT_PERMISSIONS.REJECT,
        FINANCE_ALLOCATION_PERMISSIONS.VIEW,
        FINANCE_ALLOCATION_PERMISSIONS.EXECUTE,
        FINANCE_ALLOCATION_PERMISSIONS.REALLOCATE,
      ],
      createdAt: '2026-01-01',
    });
  });

  function createWrapper() {
    return mount(PaymentVerificationQueueView, {
      global: {
        plugins: [PrimeVue, ToastService],
        directives: {
          tooltip: Tooltip,
        },
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
          teleport: true,
        },
      },
    });
  }

  it('renders queue header with title and action buttons', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Payment Verification Queue');
    expect(wrapper.find('[data-testid="record-walkin-button"]').exists()).toBe(true);
  });

  it('displays summary metrics cards (Pending Submissions, Confirmed Today, Unallocated Total)', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="queue-metrics-strip"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="pending-count"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="confirmed-today-count"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="unallocated-total"]').text()).toBe('Rp 1.000.000');
  });

  it('renders data table showing submitted, confirmed, and rejected records', () => {
    const wrapper = createWrapper();
    const table = wrapper.find('[data-testid="payments-table"]');
    expect(table.exists()).toBe(true);

    // Verify row contents
    expect(wrapper.text()).toContain('PAY-2026-000001');
    expect(wrapper.text()).toContain('Ahmad Dahlan');
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('REC-2026-000042');
    expect(wrapper.text()).toContain('Unallocated: Rp 1.000.000');
  });

  it('renders status filter tabs and toggles between submitted, confirmed, and rejected', async () => {
    const wrapper = createWrapper();
    const allTab = wrapper.find('[data-testid="status-tab-all"]');
    const submittedTab = wrapper.find('[data-testid="status-tab-submitted"]');
    const confirmedTab = wrapper.find('[data-testid="status-tab-confirmed"]');
    const rejectedTab = wrapper.find('[data-testid="status-tab-rejected"]');

    expect(allTab.exists()).toBe(true);
    expect(submittedTab.exists()).toBe(true);
    expect(confirmedTab.exists()).toBe(true);
    expect(rejectedTab.exists()).toBe(true);

    // Switch to Submitted
    await submittedTab.trigger('click');
    await flushPromises();
    expect(submittedTab.classes()).toContain('font-semibold');

    // Switch to Confirmed
    await confirmedTab.trigger('click');
    await flushPromises();
    expect(confirmedTab.classes()).toContain('font-semibold');

    // Switch to Rejected
    await rejectedTab.trigger('click');
    await flushPromises();
    expect(rejectedTab.classes()).toContain('font-semibold');

    // Reset filters restores all tab
    await wrapper.find('[data-testid="reset-filters-button"]').trigger('click');
    await flushPromises();
    expect(allTab.classes()).toContain('font-semibold');
  });

  it('opens RecordPaymentModal when Record Walk-in Payment button is clicked', async () => {
    const wrapper = createWrapper();
    const button = wrapper.find('[data-testid="record-walkin-button"]');
    await button.trigger('click');
    await flushPromises();

    expect(wrapper.findComponent(RecordPaymentModal).props('visible')).toBe(true);
  });

  it('opens ConfirmPaymentModal with selected payment when confirm action is clicked on submitted row', async () => {
    const wrapper = createWrapper();
    const confirmBtn = wrapper.find('[data-testid="confirm-action-button-11111111-1111-1111-1111-111111111111"]');
    expect(confirmBtn.exists()).toBe(true);

    await confirmBtn.trigger('click');
    await flushPromises();

    const confirmModal = wrapper.findComponent(ConfirmPaymentModal);
    expect(confirmModal.exists()).toBe(true);
    expect(confirmModal.props('visible')).toBe(true);
    expect((confirmModal.props('payment') as PaymentListItemDto).id).toBe('11111111-1111-1111-1111-111111111111');
  });

  it('opens RejectPaymentModal with selected payment when reject action is clicked on submitted row', async () => {
    const wrapper = createWrapper();
    const rejectBtn = wrapper.find('[data-testid="reject-action-button-11111111-1111-1111-1111-111111111111"]');
    expect(rejectBtn.exists()).toBe(true);

    await rejectBtn.trigger('click');
    await flushPromises();

    const rejectModal = wrapper.findComponent(RejectPaymentModal);
    expect(rejectModal.exists()).toBe(true);
    expect(rejectModal.props('visible')).toBe(true);
    expect((rejectModal.props('payment') as PaymentListItemDto).id).toBe('11111111-1111-1111-1111-111111111111');
  });

  it('launches AllocatePaymentModal when allocate action is clicked on confirmed row with unallocated funds', async () => {
    const wrapper = createWrapper();
    const allocateBtn = wrapper.find('[data-testid="allocate-action-button-22222222-2222-2222-2222-222222222222"]');
    expect(allocateBtn.exists()).toBe(true);

    await allocateBtn.trigger('click');
    await flushPromises();

    const allocateModal = wrapper.findComponent(AllocatePaymentModal);
    expect(allocateModal.exists()).toBe(true);
    expect(allocateModal.props('visible')).toBe(true);
    expect((allocateModal.props('payment') as PaymentListItemDto).id).toBe('22222222-2222-2222-2222-222222222222');
  });

  it('launches ReceiptVoucherViewer modal when view receipt button is clicked on confirmed row', async () => {
    const wrapper = createWrapper();
    const viewReceiptBtn = wrapper.find('[data-testid="view-receipt-button-22222222-2222-2222-2222-222222222222"]');
    expect(viewReceiptBtn.exists()).toBe(true);

    await viewReceiptBtn.trigger('click');
    await flushPromises();

    const receiptViewer = wrapper.findComponent(ReceiptVoucherViewer);
    expect(receiptViewer.exists()).toBe(true);
    expect(receiptViewer.props('visible')).toBe(true);
    expect(receiptViewer.props('paymentId')).toBe('22222222-2222-2222-2222-222222222222');
  });
});
