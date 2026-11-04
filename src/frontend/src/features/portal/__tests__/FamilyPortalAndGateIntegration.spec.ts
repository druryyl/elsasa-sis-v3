// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import SubmitPaymentModal from '../components/SubmitPaymentModal.vue';
import FamilyBillingTab from '../FamilyBillingTab.vue';
import ReceiptVoucherViewer from '@/features/finance/payments/components/ReceiptVoucherViewer.vue';
import EnrollApplicantModal from '@/features/students/admission/components/EnrollApplicantModal.vue';
import * as paymentsApi from '@/features/finance/payments/api/payments.api';
import * as billingApi from '@/features/finance/billing/api/billing.api';
import * as allocationsApi from '@/features/finance/allocations/api/allocations.api';
import { financeKeys } from '@/features/finance/shared/queryKeys';
import { useAuthStore } from '@/app/stores/auth.store';
import type { BillingDocumentDto } from '@/features/finance/billing/types/billing.types';
import type { PaymentReceiptDto } from '@/features/finance/payments/types/payments.types';
import type { GetStudentReceivablesResponse } from '@/features/finance/allocations/types/allocations.types';
import type { AdmissionApplicationDetailDto } from '@/features/students/admission/types/admission.types';

const mockReceipt: PaymentReceiptDto = {
  header: {
    schoolName: 'ELSASA INTEGRATED ISLAMIC SCHOOL',
    schoolAddress: 'Jl. Terusan Hang Tuah No. 12, Jakarta',
    contactInfo: 'Tel: 021-720-1923',
    documentTitle: 'BUKTI PENERIMAAN PEMBAYARAN RESMI / OFFICIAL RECEIPT',
  },
  paymentId: 'pay-001',
  paymentNumber: 'PAY-2026-000042',
  receiptNumber: 'REC-2026-000010',
  paymentDate: '2026-11-04',
  receiptIssuedAt: '2026-11-04 10:00:00 UTC',
  paymentMethod: 'bank_transfer',
  externalReference: 'TRX-BCA-889911',
  amount: 5000000,
  currencyCode: 'IDR',
  amountInWords: 'Lima Juta Rupiah',
  payer: {
    id: 'pyr-001',
    payerNumber: 'PYR-2026-0001',
    payerName: 'Budi Santoso',
    identificationNumber: '3171012345670001',
    email: 'budi@example.com',
    phoneNumber: '08123456789',
  },
  candidate: {
    studentId: 'stu-100',
    studentNumber: 'STD-2026-0001',
    studentFullName: 'Ahmad Fauzi',
    applicantId: 'app-100',
    accountNumber: 'ACC-2026-0001',
  },
  allocations: [
    {
      allocationNumber: 'ALC-2026-000001',
      obligationNumber: 'OBL-2026-000001',
      chargeType: 'enrollment_fee',
      description: 'Mandatory Enrollment Fee 2026/2027',
      amount: 5000000,
    },
  ],
  cashier: {
    confirmedBy: 'Cashier Siti',
    confirmationDate: '2026-11-04',
    title: 'Finance Cashier Officer',
    officialStampText: '[ LUNAS / SETTLED ]',
  },
  notes: 'Settled via BCA Bank Transfer',
};

const mockBillingDocuments: BillingDocumentDto[] = [
  {
    id: 'doc-unpaid',
    documentNumber: 'BIL-2026-00001',
    studentAccountId: 'acc-001',
    studentId: '00000000-0000-0000-0000-000000000001',
    studentFullName: 'Ahmad Fauzi',
    documentType: 'bill',
    recipientName: 'Budi Santoso',
    recipientRelationship: 'Father',
    recipientContact: '081234567890',
    billingPeriodStart: null,
    billingPeriodEnd: null,
    issueDate: '2026-08-01',
    paymentDueDate: '2026-08-15',
    totalBilledAmount: 2500000,
    currencyCode: 'IDR',
    deliveryChannel: 'portal',
    deliveryStatus: 'issued',
    sentAt: null,
    notes: 'Tagihan Uang Gedung',
    createdAt: '2026-08-01T08:00:00Z',
    items: [],
  },
  {
    id: 'doc-paid',
    documentNumber: 'BIL-2026-00002',
    studentAccountId: 'acc-001',
    studentId: '00000000-0000-0000-0000-000000000001',
    studentFullName: 'Ahmad Fauzi',
    documentType: 'bill',
    recipientName: 'Budi Santoso',
    recipientRelationship: 'Father',
    recipientContact: '081234567890',
    billingPeriodStart: null,
    billingPeriodEnd: null,
    issueDate: '2026-07-01',
    paymentDueDate: '2026-07-15',
    totalBilledAmount: 1500000,
    currencyCode: 'IDR',
    deliveryChannel: 'portal',
    deliveryStatus: 'paid',
    sentAt: null,
    notes: 'Tagihan SPP Juli 2026',
    createdAt: '2026-07-01T08:00:00Z',
    items: [],
  },
];

const mockCandidateApplication: AdmissionApplicationDetailDto = {
  id: 'app-999',
  applicationNumber: 'APP-2026-0099',
  applicantId: 'applicant-cand-1',
  admissionPeriodId: 'period-1',
  academicYearId: '00000000-0000-0000-0000-000000002026',
  applicationType: 'regular',
  requestedEntryLevel: 'Grade 1',
  requestedCampus: 'Main Campus',
  requestedProgram: 'Reguler',
  registrationDate: '2026-03-01',
  status: 'decided',
  createdAt: '2026-03-01T00:00:00Z',
  applicant: {
    id: 'applicant-cand-1',
    fullName: 'Fathir Ahmad',
    gender: 'male',
    email: 'fathir@example.com',
    phone: '08123456789',
    address: 'Jakarta',
  },
  contacts: [
    {
      id: 'cont-1',
      applicationId: 'app-999',
      fullName: 'Ahmad Fauzi Sr.',
      relationship: 'father',
      phone: '0812999888',
      email: 'ahmad.sr@example.com',
      isPrimary: true,
    },
  ],
  documents: [],
  assessmentEvidence: [],
  decision: {
    id: 'dec-1',
    applicationId: 'app-999',
    decisionType: 'accepted',
    decisionDate: '2026-03-10',
    effectiveDate: '2026-03-15',
    decidedBy: 'officer-1',
  },
};

describe('P6-S17: SCR-SELF-02 Family Portal Payment Submission & SCR-STU-01 Real-Time Gate Integration', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-parent-1',
      username: 'budi.santoso',
      fullName: 'Budi Santoso',
      roles: ['Parent'],
      permissions: [],
      campusId: null,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.body.innerHTML = '';
  });

  const dialogStub = {
    template: `
      <div class="dialog-stub" :data-testid="$attrs['data-testid']">
        <slot name="header" />
        <slot />
        <slot name="footer" />
      </div>
    `,
    props: ['visible'],
  };

  describe('SubmitPaymentModal.vue (SCR-SELF-02)', () => {
    it('renders input fields for student, bill, amount, payment method, reference, and transfer date', async () => {
      const wrapper = mount(SubmitPaymentModal, {
        props: {
          visible: true,
          studentId: 'stu-001',
          studentName: 'Ahmad Fauzi',
          payerName: 'Budi Santoso',
          bill: mockBillingDocuments[0],
          openBills: [mockBillingDocuments[0]],
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="submit-payment-form"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="payment-student-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="payment-amount-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="payment-method-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="external-reference-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="payment-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="proof-attachment-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="payer-name-input"]').exists()).toBe(true);

      // Value prefilling check
      const amountEl = wrapper.find('[data-testid="payment-amount-input"]').element as HTMLInputElement;
      expect(amountEl.value).toBe('2500000');
    });

    it('submits payment via useSubmitPaymentMutation and displays pending cashier verification notice', async () => {
      const submitSpy = vi.spyOn(paymentsApi, 'submitPayment').mockResolvedValue({
        paymentId: 'pay-new-1',
        paymentNumber: 'PAY-2026-000100',
        confirmationStatus: 'submitted',
        amount: 2500000,
        paymentMethod: 'bank_transfer',
        paymentDate: '2026-11-04',
        submissionDate: '2026-11-04T10:00:00Z',
        payerId: 'pyr-1',
        studentAccountId: 'acc-1',
        studentId: '00000000-0000-0000-0000-000000000001',
        applicantId: null,
      });

      const wrapper = mount(SubmitPaymentModal, {
        props: {
          visible: true,
          studentId: '00000000-0000-0000-0000-000000000001',
          studentName: 'Ahmad Fauzi',
          payerName: 'Budi Santoso',
          bill: mockBillingDocuments[0],
          openBills: [mockBillingDocuments[0]],
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      // Enter external reference and proof filename
      const refInput = wrapper.find('[data-testid="external-reference-input"]');
      await refInput.setValue('TRX-BCA-992211');

      const proofInput = wrapper.find('[data-testid="proof-attachment-input"]');
      await proofInput.setValue('transfer_slip_nov.jpg');

      // Submit form
      await wrapper.find('[data-testid="submit-payment-form"]').trigger('submit');
      await flushPromises();

      expect(submitSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          studentId: '00000000-0000-0000-0000-000000000001',
          amount: 2500000,
          paymentMethod: 'bank_transfer',
          externalReference: 'TRX-BCA-992211',
          payerName: 'Budi Santoso',
          notes: expect.stringContaining('transfer_slip_nov.jpg'),
        }),
      );

      // Verify success screen & cashier queue notification
      expect(wrapper.find('[data-testid="payment-submission-success"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Payment Successfully Submitted!');
      expect(wrapper.text()).toContain('Pending Cashier Verification');
      expect(wrapper.emitted('submitted')).toBeTruthy();
    });
  });

  describe('FamilyBillingTab.vue (SCR-SELF-02 Extensions)', () => {
    beforeEach(() => {
      vi.spyOn(billingApi, 'getBillingDocuments').mockResolvedValue({
        items: mockBillingDocuments,
        totalCount: 2,
        page: 1,
        pageSize: 50,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      });

      vi.spyOn(paymentsApi, 'getPayments').mockResolvedValue({
        items: [
          {
            id: 'pay-001',
            paymentNumber: 'PAY-2026-000042',
            receiptNumber: 'REC-2026-000010',
            studentAccountId: 'acc-001',
            accountNumber: 'ACC-2026-0001',
            studentId: '00000000-0000-0000-0000-000000000001',
            studentNumber: 'STD-2026-0001',
            studentFullName: 'Ahmad Fauzi',
            applicantId: null,
            payerId: 'pyr-001',
            payerNumber: 'PYR-2026-0001',
            payerName: 'Budi Santoso',
            paymentAmount: 1500000,
            currencyCode: 'IDR',
            paymentMethod: 'bank_transfer',
            externalReference: 'TRX-BCA-889911',
            paymentDate: '2026-11-04',
            submissionDate: '2026-11-04T10:00:00Z',
            confirmationStatus: 'confirmed',
            confirmationDate: '2026-11-04T10:00:00Z',
            confirmedBy: 'Cashier Siti',
            rejectionReason: null,
            notes: null,
            totalAllocated: 1500000,
            unallocatedAmount: 0,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      });
    });

    it('renders "Pay Now" button on open bills and opens SubmitPaymentModal', async () => {
      const wrapper = mount(FamilyBillingTab, {
        props: {
          studentId: '00000000-0000-0000-0000-000000000001',
          studentName: 'Ahmad Fauzi',
          payerName: 'Budi Santoso',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      const payNowBtn = wrapper.find('[data-testid="pay-now-btn"]');
      expect(payNowBtn.exists()).toBe(true);
      expect(payNowBtn.text()).toContain('Pay Now');

      await payNowBtn.trigger('click');
      await flushPromises();

      expect(wrapper.emitted('payNow')).toBeTruthy();
      expect(wrapper.find('[data-testid="submit-payment-modal"]').exists()).toBe(true);
    });

    it('renders "View / Download Receipt" button on settled bills and opens ReceiptVoucherViewer', async () => {
      vi.spyOn(paymentsApi, 'getPaymentReceipt').mockResolvedValue(mockReceipt);

      const wrapper = mount(FamilyBillingTab, {
        props: {
          studentId: '00000000-0000-0000-0000-000000000001',
          studentName: 'Ahmad Fauzi',
          payerName: 'Budi Santoso',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      const viewReceiptBtn = wrapper.find('[data-testid="view-receipt-btn"]');
      expect(viewReceiptBtn.exists()).toBe(true);
      expect(viewReceiptBtn.text()).toContain('View / Download Receipt');

      await viewReceiptBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="receipt-voucher-modal"]').exists()).toBe(true);
    });
  });

  describe('ReceiptVoucherViewer.vue (SCR-SELF-02 Receipt Voucher Layout)', () => {
    beforeEach(() => {
      vi.spyOn(paymentsApi, 'getPaymentReceipt').mockResolvedValue(mockReceipt);
      queryClient.setQueryData(financeKeys.payments.receipt('pay-001'), mockReceipt);
    });

    it('renders school letterhead, receipt number, IDR amounts, terbilang words, and cashier signature', async () => {
      const wrapper = mount(ReceiptVoucherViewer, {
        props: {
          visible: true,
          paymentId: 'pay-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="printable-receipt-content"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="receipt-number"]').text()).toBe('REC-2026-000010');
      expect(wrapper.text()).toContain('Budi Santoso');
      expect(wrapper.text()).toContain('Ahmad Fauzi');
      expect(wrapper.find('[data-testid="receipt-amount"]').text()).toBe('Rp 5.000.000');
      expect(wrapper.text()).toContain('Lima Juta Rupiah');
      expect(wrapper.text()).toContain('[ LUNAS / SETTLED ]');
      expect(wrapper.text()).toContain('Cashier Siti');
    });

    it('triggers window.print when print button is clicked', async () => {
      const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});

      const wrapper = mount(ReceiptVoucherViewer, {
        props: {
          visible: true,
          paymentId: 'pay-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      const printBtn = wrapper.find('[data-testid="print-receipt-button"]');
      expect(printBtn.exists()).toBe(true);

      await printBtn.trigger('click');
      expect(printSpy).toHaveBeenCalled();
    });
  });

  describe('EnrollApplicantModal.vue Real-Time Gate Integration (SCR-STU-01)', () => {
    it('displays prominent green "Payment Verified & Settled" clearance badge and preselects Payment Confirmed when mandatory enrollment fees are settled', async () => {
      const settledReceivables: GetStudentReceivablesResponse = {
        studentId: 'applicant-cand-1',
        totalNetObligation: 5000000,
        totalPaid: 5000000,
        totalOutstanding: 0,
        totalOverdueCount: 0,
        receivables: [
          {
            obligationId: 'obl-10',
            obligationNumber: 'OBL-2026-001',
            studentAccountId: 'acc-10',
            studentId: null,
            applicantId: 'applicant-cand-1',
            chargeType: 'enrollment_fee',
            description: 'Biaya Masuk & SPP Awal',
            isMandatory: true,
            dueDate: '2026-04-01',
            originalAmount: 5000000,
            currencyCode: 'IDR',
            academicPeriodId: 'period-1',
            academicPeriodLabel: '2026/2027',
            obligationStatus: 'settled',
            netObligation: 5000000,
            totalPaid: 5000000,
            outstandingAmount: 0,
            isOverdue: false,
            daysOverdue: 0,
            agingBracket: 'Current',
            receivableStatus: 'settled',
          },
        ],
        openObligations: [],
      };

      vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue(settledReceivables);
      vi.spyOn(paymentsApi, 'getPayments').mockResolvedValue({
        items: [
          {
            id: 'pay-001',
            paymentNumber: 'PAY-2026-000042',
            receiptNumber: 'REC-2026-000099',
            studentAccountId: 'acc-10',
            accountNumber: 'ACC-2026-001',
            studentId: null,
            studentNumber: null,
            studentFullName: 'Fathir Ahmad',
            applicantId: 'applicant-cand-1',
            payerId: 'pyr-1',
            payerNumber: 'PYR-1',
            payerName: 'Ahmad Fauzi Sr.',
            paymentAmount: 5000000,
            currencyCode: 'IDR',
            paymentMethod: 'bank_transfer',
            externalReference: 'TRX-BCA-5544',
            paymentDate: '2026-03-12',
            submissionDate: '2026-03-12T10:00:00Z',
            confirmationStatus: 'confirmed',
            confirmationDate: '2026-03-12T10:00:00Z',
            confirmedBy: 'Cashier Siti',
            rejectionReason: null,
            notes: null,
            totalAllocated: 5000000,
            unallocatedAmount: 0,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      });

      const wrapper = mount(EnrollApplicantModal, {
        props: {
          visible: true,
          application: mockCandidateApplication,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      // Assert clearance badge is visible
      const verifiedBadge = wrapper.find('[data-testid="payment-verified-badge"]');
      expect(verifiedBadge.exists()).toBe(true);
      expect(verifiedBadge.text()).toContain('Payment Verified & Settled');

      // Assert receipt reference is rendered and pre-filled
      const receiptRefBadge = wrapper.find('[data-testid="verified-receipt-reference"]');
      expect(receiptRefBadge.exists()).toBe(true);
      expect(receiptRefBadge.text()).toBe('REC-2026-000099');

      // Assert Payment Confirmed mode is preselected and reference input holds the receipt number
      const confirmedRadio = wrapper.find('[data-testid="payment-mode-confirmed"]').element as HTMLInputElement;
      expect(confirmedRadio.checked).toBe(true);

      const refInput = wrapper.find('[data-testid="payment-reference-input"]').element as HTMLInputElement;
      expect(refInput.value).toBe('REC-2026-000099');
    });

    it('displays amber warning when mandatory enrollment fees are unpaid and prompts for cashier verification or fee waiver', async () => {
      const unpaidReceivables: GetStudentReceivablesResponse = {
        studentId: 'applicant-cand-1',
        totalNetObligation: 5000000,
        totalPaid: 0,
        totalOutstanding: 5000000,
        totalOverdueCount: 0,
        receivables: [
          {
            obligationId: 'obl-10',
            obligationNumber: 'OBL-2026-001',
            studentAccountId: 'acc-10',
            studentId: null,
            applicantId: 'applicant-cand-1',
            chargeType: 'enrollment_fee',
            description: 'Mandatory Enrollment Fee',
            isMandatory: true,
            dueDate: '2026-04-01',
            originalAmount: 5000000,
            currencyCode: 'IDR',
            academicPeriodId: 'period-1',
            academicPeriodLabel: '2026/2027',
            obligationStatus: 'assessed',
            netObligation: 5000000,
            totalPaid: 0,
            outstandingAmount: 5000000,
            isOverdue: false,
            daysOverdue: 0,
            agingBracket: 'Current',
            receivableStatus: 'current',
          },
        ],
        openObligations: [],
      };

      vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue(unpaidReceivables);
      vi.spyOn(paymentsApi, 'getPayments').mockResolvedValue({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 10,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      });

      const wrapper = mount(EnrollApplicantModal, {
        props: {
          visible: true,
          application: mockCandidateApplication,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      // Assert amber warning is visible
      const warningBanner = wrapper.find('[data-testid="payment-outstanding-warning"]');
      expect(warningBanner.exists()).toBe(true);
      expect(warningBanner.text()).toContain('Enrollment Fees Outstanding: Rp 5.000.000');
      expect(warningBanner.text()).toContain('SCR-FIN-02: Payment Verification Queue');

      // Switch to Authorized Fee Waiver
      const waiverRadio = wrapper.find('[data-testid="payment-mode-waived"]');
      await waiverRadio.trigger('change');
      await flushPromises();

      expect(wrapper.find('[data-testid="waiver-reference-input"]').exists()).toBe(true);
    });
  });
});
