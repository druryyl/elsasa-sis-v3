// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import PaymentsHistoryTable from '../accounts/components/PaymentsHistoryTable.vue';
import AllocationsTable from '../accounts/components/AllocationsTable.vue';
import ReceiptVoucherViewer from '../payments/components/ReceiptVoucherViewer.vue';
import ReallocatePaymentModal from '../payments/components/ReallocatePaymentModal.vue';
import * as paymentsApi from '../payments/api/payments.api';
import * as allocationsApi from '../allocations/api/allocations.api';
import type { PaymentListItemDto, PaymentReceiptDto } from '../payments/types/payments.types';
import type { PaymentAllocationDto } from '../allocations/types/allocations.types';

const mockPaymentItem: PaymentListItemDto = {
  id: 'pay-001',
  paymentNumber: 'PAY-2026-000001',
  receiptNumber: 'REC-2026-000001',
  studentAccountId: 'acc-001',
  accountNumber: 'ACC-2026-0001',
  studentId: 'stu-001',
  studentNumber: '2026-001',
  studentFullName: 'Ahmad Fauzi',
  applicantId: null,
  payerId: 'pyr-001',
  payerNumber: 'PYR-2026-0001',
  payerName: 'Haji Sulaiman',
  paymentAmount: 1500000,
  currencyCode: 'IDR',
  paymentMethod: 'bank_transfer',
  externalReference: 'TRX-BCA-98765',
  paymentDate: '2026-08-05',
  submissionDate: '2026-08-05T08:30:00Z',
  confirmationStatus: 'confirmed',
  confirmationDate: '2026-08-05T09:00:00Z',
  confirmedBy: 'cashier.finance',
  rejectionReason: null,
  notes: 'Pembayaran SPP dan Kegiatan',
  totalAllocated: 1500000,
  unallocatedAmount: 0,
};

const mockSubmittedPaymentItem: PaymentListItemDto = {
  id: 'pay-002',
  paymentNumber: 'PAY-2026-000002',
  receiptNumber: null,
  studentAccountId: 'acc-001',
  accountNumber: 'ACC-2026-0001',
  studentId: 'stu-001',
  studentNumber: '2026-001',
  studentFullName: 'Ahmad Fauzi',
  applicantId: null,
  payerId: 'pyr-001',
  payerNumber: 'PYR-2026-0001',
  payerName: 'Haji Sulaiman',
  paymentAmount: 500000,
  currencyCode: 'IDR',
  paymentMethod: 'cash',
  externalReference: null,
  paymentDate: '2026-08-06',
  submissionDate: '2026-08-06T10:00:00Z',
  confirmationStatus: 'submitted',
  confirmationDate: null,
  confirmedBy: null,
  rejectionReason: null,
  notes: null,
  totalAllocated: 0,
  unallocatedAmount: 500000,
};

const mockAllocationItem: PaymentAllocationDto & { dueDate?: string; paymentId?: string } = {
  id: 'alloc-001',
  allocationId: 'alloc-001',
  allocationNumber: 'ALC-2026-000001',
  obligationId: 'obl-001',
  obligationNumber: 'OBL-2026-000001',
  chargeType: 'tuition',
  description: 'SPP Bulanan Agustus 2026',
  allocatedAmount: 750000,
  allocationDate: '2026-08-05T09:00:00Z',
  allocatedBy: 'cashier.finance',
  status: 'active',
  supersededById: null,
  reversalReason: null,
  dueDate: '2026-08-10',
  paymentId: 'pay-001',
};

const mockSupersededAllocationItem: PaymentAllocationDto & { dueDate?: string; paymentId?: string } = {
  id: 'alloc-002',
  allocationId: 'alloc-002',
  allocationNumber: 'ALC-2026-000002',
  obligationId: 'obl-002',
  obligationNumber: 'OBL-2026-000002',
  chargeType: 'building_fee',
  description: 'Uang Gedung Tahap 1',
  allocatedAmount: 750000,
  allocationDate: '2026-08-05T09:00:00Z',
  allocatedBy: 'cashier.finance',
  status: 'superseded',
  supersededById: 'alloc-003',
  reversalReason: 'Alokasi keliru pada akun penerima',
  dueDate: '2026-08-15',
  paymentId: 'pay-001',
};

const mockReceiptVoucher: PaymentReceiptDto = {
  header: {
    schoolName: 'ELSASA ISLAMIC SCHOOL',
    schoolAddress: 'Kampus Utama, Jl. Pendidikan No. 1, Jakarta',
    schoolPhone: '021-1234567',
    schoolEmail: 'finance@elsasa.sch.id',
    schoolWebsite: 'https://elsasa.sch.id',
    logoUrl: null,
  },
  paymentId: 'pay-001',
  paymentNumber: 'PAY-2026-000001',
  receiptNumber: 'REC-2026-000001',
  paymentDate: '2026-08-05',
  receiptIssuedAt: '2026-08-05T09:00:00Z',
  paymentMethod: 'bank_transfer',
  externalReference: 'TRX-BCA-98765',
  amount: 1500000,
  currencyCode: 'IDR',
  amountInWords: 'Satu Juta Lima Ratus Ribu Rupiah',
  payer: {
    payerId: 'pyr-001',
    payerNumber: 'PYR-2026-0001',
    payerName: 'Haji Sulaiman',
    payerType: 'parent_guardian',
    identificationNumber: '3171012345670001',
  },
  candidate: {
    studentId: 'stu-001',
    applicantId: null,
    accountNumber: 'ACC-2026-0001',
    identifier: '2026-001',
    fullName: 'Ahmad Fauzi',
    cohortOrGrade: 'SD 1A',
  },
  allocations: [
    {
      allocationNumber: 'ALC-2026-000001',
      obligationNumber: 'OBL-2026-000001',
      chargeType: 'tuition',
      description: 'SPP Bulanan Agustus 2026',
      amount: 750000,
    },
    {
      allocationNumber: 'ALC-2026-000003',
      obligationNumber: 'OBL-2026-000002',
      chargeType: 'building_fee',
      description: 'Uang Gedung Tahap 1',
      amount: 750000,
    },
  ],
  cashier: {
    confirmedBy: 'cashier.finance',
    confirmationDate: '2026-08-05T09:00:00Z',
    title: 'Finance Officer',
    officialStampText: 'BENDOHARA KEUANGAN SEKOLAH',
  },
  notes: 'Terima kasih atas pembayaran tepat waktu.',
};

describe('Payments & Allocations Extensions (SCR-FIN-03, P6-S16)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.spyOn(paymentsApi, 'getPaymentReceipt').mockResolvedValue(mockReceiptVoucher);
    vi.spyOn(allocationsApi, 'reallocatePayment').mockResolvedValue({
      paymentId: 'pay-001',
      paymentNumber: 'PAY-2026-000001',
      reversalReason: 'Alokasi keliru pada akun penerima',
      supersededAllocationIds: ['alloc-001'],
      reversedAllocationIds: [],
      paymentAmount: 1500000,
      activeAllocatedAmount: 750000,
      remainingUnallocatedAmount: 750000,
      replacementAllocations: [],
    });
    vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue({
      studentId: 'stu-001',
      totalNetObligation: 1500000,
      totalPaid: 750000,
      totalOutstanding: 750000,
      totalOverdueCount: 0,
      receivables: [],
      openObligations: [
        {
          obligationId: 'obl-003',
          obligationNumber: 'OBL-2026-000003',
          studentAccountId: 'acc-001',
          studentId: 'stu-001',
          applicantId: null,
          chargeType: 'activity_fee',
          description: 'Uang Kegiatan Semester 1',
          isMandatory: false,
          dueDate: '2026-08-25',
          originalAmount: 500000,
          currencyCode: 'IDR',
          academicPeriodId: null,
          academicPeriodLabel: null,
          obligationStatus: 'assessed',
          netObligation: 500000,
          totalPaid: 0,
          outstandingAmount: 500000,
          isOverdue: false,
          daysOverdue: 0,
          agingBracket: 'Current',
          receivableStatus: 'current',
        },
      ],
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function mountComponent(Component: any, props = {}) {
    return mount(Component, {
      attachTo: document.body,
      props,
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: { template: '<a><slot /></a>' },
        },
      },
    });
  }

  // ---------------------------------------------------------------------------
  // 1. PaymentsHistoryTable.vue (Criterion 1)
  // ---------------------------------------------------------------------------
  describe('PaymentsHistoryTable.vue', () => {
    it('renders payments list with date, receipt/payment number, payer, method, amount, and status', () => {
      const wrapper = mountComponent(PaymentsHistoryTable, {
        payments: [mockPaymentItem, mockSubmittedPaymentItem],
      });

      expect(wrapper.find('[data-testid="payments-history-table-container"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('2 recorded');

      // Check confirmed payment with receipt
      expect(wrapper.text()).toContain('REC-2026-000001');
      expect(wrapper.text()).toContain('PAY-2026-000001');
      expect(wrapper.text()).toContain('Haji Sulaiman');
      expect(wrapper.text()).toContain('Bank Transfer');
      expect(wrapper.text()).toContain('Rp 1.500.000');
      expect(wrapper.text()).toContain('Confirmed');

      // Check submitted payment without receipt
      expect(wrapper.text()).toContain('PAY-2026-000002');
      expect(wrapper.text()).toContain('(Pending Receipt)');
      expect(wrapper.text()).toContain('Rp 500.000');
      expect(wrapper.text()).toContain('Submitted');
    });

    it('enables View Receipt button for confirmed payments and emits viewReceipt event', async () => {
      const wrapper = mountComponent(PaymentsHistoryTable, {
        payments: [mockPaymentItem, mockSubmittedPaymentItem],
      });

      const viewReceiptButtons = wrapper.findAll('[data-testid="view-receipt-btn"]');
      expect(viewReceiptButtons.length).toBe(2);

      // First button is for confirmed payment -> should be enabled
      const confirmedBtn = viewReceiptButtons[0];
      expect(confirmedBtn.attributes('disabled')).toBeUndefined();

      // Click view receipt
      await confirmedBtn.trigger('click');
      expect(wrapper.emitted('viewReceipt')).toBeTruthy();
      expect(wrapper.emitted('viewReceipt')![0][0]).toBe('pay-001');

      // Second button is for submitted payment without receipt -> should be disabled
      const submittedBtn = viewReceiptButtons[1];
      expect(submittedBtn.attributes('disabled')).toBeDefined();
    });

    it('filters payments by quick search and confirmation status filter', async () => {
      const wrapper = mountComponent(PaymentsHistoryTable, {
        payments: [mockPaymentItem, mockSubmittedPaymentItem],
      });

      // Filter by search term
      const searchInput = wrapper.find('[data-testid="payments-search-input"]');
      await searchInput.setValue('TRX-BCA');
      expect(wrapper.text()).toContain('REC-2026-000001');
      expect(wrapper.text()).not.toContain('PAY-2026-000002');

      // Clear search
      await searchInput.setValue('');

      // Filter by status: Submitted
      const filterButtons = wrapper.findAll('button');
      const submittedTab = filterButtons.find((b) => b.text().includes('Submitted'));
      expect(submittedTab).toBeDefined();
      await submittedTab!.trigger('click');

      expect(wrapper.text()).toContain('PAY-2026-000002');
      expect(wrapper.text()).not.toContain('REC-2026-000001');
    });
  });

  // ---------------------------------------------------------------------------
  // 2. AllocationsTable.vue (Criterion 2)
  // ---------------------------------------------------------------------------
  describe('AllocationsTable.vue', () => {
    it('renders allocations list with allocation number, obligation description, due date, allocated amount, status, and reversal reason', () => {
      const wrapper = mountComponent(AllocationsTable, {
        allocations: [mockAllocationItem, mockSupersededAllocationItem],
      });

      expect(wrapper.find('[data-testid="allocations-table-container"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('2 applied');

      // Check active allocation
      expect(wrapper.text()).toContain('ALC-2026-000001');
      expect(wrapper.text()).toContain('SPP Bulanan Agustus 2026');
      expect(wrapper.text()).toContain('Rp 750.000');
      expect(wrapper.text()).toContain('Active');
      expect(wrapper.text()).toContain('2026-08-10');

      // Check superseded allocation
      expect(wrapper.text()).toContain('ALC-2026-000002');
      expect(wrapper.text()).toContain('Uang Gedung Tahap 1');
      expect(wrapper.text()).toContain('Superseded');
      expect(wrapper.text()).toContain('Alokasi keliru pada akun penerima');
    });

    it('emits reallocate event when Reallocate button is clicked on active allocation', async () => {
      const wrapper = mountComponent(AllocationsTable, {
        allocations: [mockAllocationItem, mockSupersededAllocationItem],
        canReallocate: true,
      });

      const reallocateButtons = wrapper.findAll('[data-testid="reallocate-action-btn"]');
      expect(reallocateButtons.length).toBe(2);

      // Active allocation reallocate button is enabled
      const activeBtn = reallocateButtons[0];
      expect(activeBtn.attributes('disabled')).toBeUndefined();
      await activeBtn.trigger('click');

      expect(wrapper.emitted('reallocate')).toBeTruthy();
      expect(wrapper.emitted('reallocate')![0][0]).toEqual(mockAllocationItem);

      // Superseded allocation reallocate button is disabled
      const supersededBtn = reallocateButtons[1];
      expect(supersededBtn.attributes('disabled')).toBeDefined();
    });

    it('disables Reallocate button when canReallocate prop is false', () => {
      const wrapper = mountComponent(AllocationsTable, {
        allocations: [mockAllocationItem],
        canReallocate: false,
      });

      const btn = wrapper.find('[data-testid="reallocate-action-btn"]');
      expect(btn.attributes('disabled')).toBeDefined();
    });
  });

  // ---------------------------------------------------------------------------
  // 3. ReceiptVoucherViewer.vue
  // ---------------------------------------------------------------------------
  describe('ReceiptVoucherViewer.vue', () => {
    it('fetches and renders receipt voucher details when visible is true', async () => {
      mountComponent(ReceiptVoucherViewer, {
        visible: true,
        paymentId: 'pay-001',
      });

      await flushPromises();

      expect(paymentsApi.getPaymentReceipt).toHaveBeenCalledWith('pay-001');
      expect(document.querySelector('[data-testid="receipt-number"]')?.textContent).toContain('REC-2026-000001');
      expect(document.querySelector('[data-testid="receipt-amount"]')?.textContent).toBe('Rp 1.500.000');
      expect(document.body.textContent).toContain('Ahmad Fauzi');
      expect(document.body.textContent).toContain('Haji Sulaiman');
      expect(document.body.textContent).toContain('Satu Juta Lima Ratus Ribu Rupiah');
      expect(document.body.textContent).toContain('BENDOHARA KEUANGAN SEKOLAH');
    });
  });

  // ---------------------------------------------------------------------------
  // 4. ReallocatePaymentModal.vue
  // ---------------------------------------------------------------------------
  describe('ReallocatePaymentModal.vue', () => {
    it('renders target allocation and submits reallocation with reversal reason', async () => {
      const wrapper = mountComponent(ReallocatePaymentModal, {
        visible: true,
        allocation: mockAllocationItem,
        paymentId: 'pay-001',
        studentId: 'stu-001',
      });

      await flushPromises();

      expect(document.body.textContent).toContain('ALC-2026-000001');
      expect(document.body.textContent).toContain('SPP Bulanan Agustus 2026');

      const reasonInput = document.querySelector('[data-testid="reversal-reason-input"]') as HTMLTextAreaElement;
      expect(reasonInput).not.toBeNull();
      reasonInput.value = 'Koreksi alokasi dana karena salah kode SPP';
      reasonInput.dispatchEvent(new Event('input'));
      await flushPromises();

      const confirmBtn = document.querySelector('[data-testid="confirm-reallocate-btn"]') as HTMLButtonElement;
      expect(confirmBtn).not.toBeNull();
      expect(confirmBtn.disabled).toBe(false);

      confirmBtn.click();
      await flushPromises();

      expect(allocationsApi.reallocatePayment).toHaveBeenCalledWith(
        expect.objectContaining({
          paymentId: 'pay-001',
          priorAllocationIds: ['alloc-001'],
          reversalReason: 'Koreksi alokasi dana karena salah kode SPP',
        }),
      );
      expect(wrapper.emitted('reallocated')).toBeTruthy();
    });
  });
});
