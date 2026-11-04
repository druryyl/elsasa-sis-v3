// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import AllocatePaymentModal from '../payments/components/AllocatePaymentModal.vue';
import type { PaymentListItemDto } from '../payments/types/payments.types';
import type { StudentReceivableDto } from '../allocations/types/allocations.types';

const mockPayment: PaymentListItemDto = {
  id: 'pay-00000000-0000-0000-0000-000000000001',
  paymentNumber: 'PAY-2026-000001',
  receiptNumber: 'REC-2026-000001',
  studentAccountId: 'acc-1',
  accountNumber: 'ACC-2026-0001',
  studentId: 'stu-1',
  studentNumber: '2026001',
  studentFullName: 'Ahmad Fauzi',
  applicantId: null,
  payerId: 'pyr-1',
  payerNumber: 'PYR-2026-0001',
  payerName: 'Budi Santoso',
  paymentAmount: 2500000,
  currencyCode: 'IDR',
  paymentMethod: 'bank_transfer',
  externalReference: 'TRF-BCA-12345',
  paymentDate: '2026-11-04',
  submissionDate: '2026-11-04T08:00:00Z',
  confirmationStatus: 'confirmed',
  confirmationDate: '2026-11-04T08:30:00Z',
  confirmedBy: 'Cashier User',
  rejectionReason: null,
  notes: null,
  totalAllocated: 0,
  unallocatedAmount: 2500000,
};

const mockReceivables: StudentReceivableDto[] = [
  {
    obligationId: 'obl-00000000-0000-0000-0000-000000000001',
    obligationNumber: 'OBL-2026-000001',
    studentAccountId: 'acc-1',
    studentId: 'stu-1',
    applicantId: null,
    chargeType: 'enrollment_fee',
    description: 'Mandatory Admission & Enrollment Fee',
    isMandatory: true,
    dueDate: '2026-08-01',
    originalAmount: 1500000,
    currencyCode: 'IDR',
    academicPeriodId: 'ay-2026',
    academicPeriodLabel: '2026/2027',
    obligationStatus: 'assessed',
    netObligation: 1500000,
    totalPaid: 0,
    outstandingAmount: 1500000,
    isOverdue: false,
    daysOverdue: 0,
    agingBracket: 'Current',
    receivableStatus: 'current',
  },
  {
    obligationId: 'obl-00000000-0000-0000-0000-000000000002',
    obligationNumber: 'OBL-2026-000002',
    studentAccountId: 'acc-1',
    studentId: 'stu-1',
    applicantId: null,
    chargeType: 'tuition',
    description: 'SPP Bulanan Agustus 2026',
    isMandatory: true,
    dueDate: '2026-08-10',
    originalAmount: 1000000,
    currencyCode: 'IDR',
    academicPeriodId: 'ay-2026',
    academicPeriodLabel: '2026/2027',
    obligationStatus: 'assessed',
    netObligation: 1000000,
    totalPaid: 0,
    outstandingAmount: 1000000,
    isOverdue: false,
    daysOverdue: 0,
    agingBracket: 'Current',
    receivableStatus: 'current',
  },
  {
    obligationId: 'obl-00000000-0000-0000-0000-000000000003',
    obligationNumber: 'OBL-2026-000003',
    studentAccountId: 'acc-1',
    studentId: 'stu-1',
    applicantId: null,
    chargeType: 'activity',
    description: 'Ekstrakurikuler Robotika',
    isMandatory: false,
    dueDate: '2026-08-20',
    originalAmount: 500000,
    currencyCode: 'IDR',
    academicPeriodId: 'ay-2026',
    academicPeriodLabel: '2026/2027',
    obligationStatus: 'assessed',
    netObligation: 500000,
    totalPaid: 0,
    outstandingAmount: 500000,
    isOverdue: false,
    daysOverdue: 0,
    agingBracket: 'Current',
    receivableStatus: 'current',
  },
];

const mockMutate = vi.fn();
let mockReceivablesData = { receivables: mockReceivables };

vi.mock('../allocations/composables/useFinanceAllocations', () => ({
  useStudentReceivablesQuery: () => ({
    data: { value: mockReceivablesData },
    isPending: { value: false },
    isError: { value: false },
    refetch: vi.fn(),
  }),
  useAllocatePaymentMutation: (options?: { onSuccess?: (data: any) => void }) => ({
    mutate: (payload: any) => {
      mockMutate(payload);
      options?.onSuccess?.({
        paymentId: payload.paymentId,
        totalAllocated: 2500000,
        unallocatedAmount: 0,
        allocations: [],
      });
    },
    isPending: { value: false },
  }),
}));

describe('AllocatePaymentModal.vue (SCR-FIN-02)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    mockMutate.mockClear();
    mockReceivablesData = { receivables: mockReceivables };
  });

  function createWrapper(propsData: Record<string, any> = {}) {
    return mount(AllocatePaymentModal, {
      props: {
        visible: true,
        payment: mockPayment,
        ...propsData,
      },
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: {
          teleport: true,
        },
      },
    });
  }

  it('renders modal header metrics: payment gross, currently allocating, and remaining available funds', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="allocate-payment-modal"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Allocate Payment Funds');
    expect(wrapper.text()).toContain('Payment Gross');
    expect(wrapper.text()).toContain('Rp 2.500.000');
    expect(wrapper.text()).toContain('REC-2026-000001');
    expect(wrapper.text()).toContain('Currently Allocating');
    expect(wrapper.text()).toContain('Remaining Available');
  });

  it('renders open obligations table with charge descriptions and outstanding balances', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const table = wrapper.find('[data-testid="open-obligations-table"]');
    expect(table.exists()).toBe(true);
    expect(wrapper.text()).toContain('OBL-2026-000001');
    expect(wrapper.text()).toContain('Mandatory Admission & Enrollment Fee');
    expect(wrapper.text()).toContain('Rp 1.500.000');
    expect(wrapper.text()).toContain('SPP Bulanan Agustus 2026');
    expect(wrapper.text()).toContain('Rp 1.000.000');
    expect(wrapper.text()).toContain('Ekstrakurikuler Robotika');
    expect(wrapper.text()).toContain('Rp 500.000');
  });

  it('clicks "Auto-Suggest" populating line allocations by priority rule and calculating unallocated balance', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const autoSuggestBtn = wrapper.find('[data-testid="auto-suggest-button"]');
    expect(autoSuggestBtn.exists()).toBe(true);

    // Click Auto-Suggest
    await autoSuggestBtn.trigger('click');
    await flushPromises();

    // Line 1: Mandatory enrollment fee (1,500,000)
    const line1Input = wrapper.find(
      '[data-testid="line-allocation-input-obl-00000000-0000-0000-0000-000000000001"]',
    ).element as HTMLInputElement;
    expect(line1Input.value).toBe('1500000');

    // Line 2: Mandatory tuition fee (1,000,000)
    const line2Input = wrapper.find(
      '[data-testid="line-allocation-input-obl-00000000-0000-0000-0000-000000000002"]',
    ).element as HTMLInputElement;
    expect(line2Input.value).toBe('1000000');

    // Line 3: Non-mandatory activity fee (should have 0 because payment was 2,500,000)
    const line3Input = wrapper.find(
      '[data-testid="line-allocation-input-obl-00000000-0000-0000-0000-000000000003"]',
    ).element as HTMLInputElement;
    expect(line3Input.value).toBe('');

    // Verify calculated metrics: total allocating = 2,500,000, remaining available = 0
    expect(wrapper.text()).toContain('2 line(s) selected');
    expect(wrapper.text()).toContain('Rp 0');
  });

  it('sets line allocation to maximum available when "Max" button is clicked', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const maxBtnLine1 = wrapper.find(
      '[data-testid="line-max-button-obl-00000000-0000-0000-0000-000000000001"]',
    );
    expect(maxBtnLine1.exists()).toBe(true);

    await maxBtnLine1.trigger('click');
    await flushPromises();

    const line1Input = wrapper.find(
      '[data-testid="line-allocation-input-obl-00000000-0000-0000-0000-000000000001"]',
    ).element as HTMLInputElement;
    expect(line1Input.value).toBe('1500000');

    // Remaining available is now 2,500,000 - 1,500,000 = 1,000,000
    // Click max on line 3 (outstanding: 500,000, available: 1,000,000 -> allocates 500,000)
    const maxBtnLine3 = wrapper.find(
      '[data-testid="line-max-button-obl-00000000-0000-0000-0000-000000000003"]',
    );
    await maxBtnLine3.trigger('click');
    await flushPromises();

    const line3Input = wrapper.find(
      '[data-testid="line-allocation-input-obl-00000000-0000-0000-0000-000000000003"]',
    ).element as HTMLInputElement;
    expect(line3Input.value).toBe('500000');
  });

  it('disables commit button when no allocations are entered', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const commitBtn = wrapper.find('[data-testid="commit-allocations-button"]');
    expect(commitBtn.attributes('disabled')).toBeDefined();
  });

  it('submits allocation payload and emits success event on commit', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Trigger auto-suggest to fill allocations
    await wrapper.find('[data-testid="auto-suggest-button"]').trigger('click');
    await flushPromises();

    const commitBtn = wrapper.find('[data-testid="commit-allocations-button"]');
    expect(commitBtn.attributes('disabled')).toBeUndefined();

    await commitBtn.trigger('click');
    await flushPromises();

    expect(mockMutate).toHaveBeenCalledWith({
      paymentId: 'pay-00000000-0000-0000-0000-000000000001',
      allocations: [
        {
          obligationId: 'obl-00000000-0000-0000-0000-000000000001',
          amount: 1500000,
          notes: null,
        },
        {
          obligationId: 'obl-00000000-0000-0000-0000-000000000002',
          amount: 1000000,
          notes: null,
        },
      ],
      notes: null,
    });

    expect(wrapper.emitted('success')).toBeTruthy();
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
  });
});
