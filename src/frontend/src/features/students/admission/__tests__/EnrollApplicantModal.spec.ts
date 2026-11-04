// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import EnrollApplicantModal from '../components/EnrollApplicantModal.vue';
import * as allocationsApi from '@/features/finance/allocations/api/allocations.api';
import * as paymentsApi from '@/features/finance/payments/api/payments.api';
import * as enrollmentApi from '../../enrollment/api/enrollment.api';
import type { AdmissionApplicationDetailDto } from '../types/admission.types';
import type { GetStudentReceivablesResponse } from '@/features/finance/allocations/types/allocations.types';

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

const mockSettledReceivables: GetStudentReceivablesResponse = {
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
      description: 'Biaya Masuk & Uang Pangkal',
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

const mockUnpaidReceivables: GetStudentReceivablesResponse = {
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

const mockPeriods = [
  {
    id: 'period-1',
    academicYearId: '00000000-0000-0000-0000-000000002026',
    periodCode: 'PPDB-2026-G1',
    name: 'PPDB 2026/2027 Gelombang 1',
    periodType: 'admission',
    startDate: '2026-01-01',
    endDate: '2026-06-30',
    status: 'active',
  },
];

describe('EnrollApplicantModal.vue (Fee Gate Verification, SCR-STU-01)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant').mockResolvedValue({
      studentId: 'stu-999',
      studentNumber: 'STD-2026-0099',
      enrollmentId: 'enr-999',
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.body.innerHTML = '';
  });

  function mountModal(propsData: Record<string, any> = {}) {
    return mount(EnrollApplicantModal, {
      attachTo: document.body,
      props: {
        visible: true,
        application: mockCandidateApplication,
        periods: mockPeriods,
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

  it('displays prominent green "Payment Verified & Settled" clearance badge when mandatory enrollment fees are settled', async () => {
    vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue(mockSettledReceivables);
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
      pageNumber: 1,
      pageSize: 10,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountModal();
    await flushPromises();

    // Assert clearance badge is visible
    const verifiedBadge = wrapper.find('[data-testid="payment-verified-badge"]');
    expect(verifiedBadge.exists()).toBe(true);
    expect(verifiedBadge.text()).toContain('Payment Verified & Settled');

    // Assert receipt reference is rendered and pre-filled
    const receiptRefBadge = wrapper.find('[data-testid="verified-receipt-reference"]');
    expect(receiptRefBadge.exists()).toBe(true);
    expect(receiptRefBadge.text()).toBe('REC-2026-000099');

    // Assert gate status pill indicates settled
    const statusPill = wrapper.find('[data-testid="gate-status-pill"]');
    expect(statusPill.exists()).toBe(true);
    expect(statusPill.text()).toContain('Payment Verified & Settled');

    // Assert Payment Confirmed mode is preselected and reference input holds the receipt number
    const confirmedRadio = wrapper.find('[data-testid="payment-mode-confirmed"]').element as HTMLInputElement;
    expect(confirmedRadio.checked).toBe(true);

    const refInput = wrapper.find('[data-testid="payment-reference-input"]').element as HTMLInputElement;
    expect(refInput.value).toBe('REC-2026-000099');
  });

  it('displays amber warning when mandatory enrollment fees are unpaid and prompts for cashier verification or fee waiver', async () => {
    vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue(mockUnpaidReceivables);
    vi.spyOn(paymentsApi, 'getPayments').mockResolvedValue({
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 10,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountModal();
    await flushPromises();

    // Assert amber warning banner is visible
    const warningBanner = wrapper.find('[data-testid="payment-outstanding-warning"]');
    expect(warningBanner.exists()).toBe(true);
    expect(warningBanner.text()).toContain('Enrollment Fees Outstanding: Rp 5.000.000');
    expect(warningBanner.text()).toContain('SCR-FIN-02: Payment Verification Queue');

    // Assert gate status pill indicates fees outstanding
    const statusPill = wrapper.find('[data-testid="gate-status-pill"]');
    expect(statusPill.exists()).toBe(true);
    expect(statusPill.text()).toContain('Fees Outstanding');

    // Switch to Authorized Fee Waiver
    const waiverRadio = wrapper.find('[data-testid="payment-mode-waived"]');
    expect(waiverRadio.exists()).toBe(true);
    await waiverRadio.trigger('change');
    await flushPromises();

    // Waiver reference input should now be displayed
    const waiverInput = wrapper.find('[data-testid="waiver-reference-input"]');
    expect(waiverInput.exists()).toBe(true);
  });

  it('submits enrollment form successfully and emits success result with student number', async () => {
    vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue(mockSettledReceivables);
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
      pageNumber: 1,
      pageSize: 10,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountModal();
    await flushPromises();

    // Fill primary guardian government ID if needed and submit
    const form = wrapper.find('form');
    expect(form.exists()).toBe(true);
    await form.trigger('submit');
    await flushPromises();

    expect(enrollmentApi.enrollAcceptedApplicant).toHaveBeenCalledWith(
      expect.objectContaining({
        applicantId: 'applicant-cand-1',
        paymentWaived: false,
        paymentReference: 'REC-2026-000099',
      }),
    );

    expect(wrapper.emitted('success')).toBeTruthy();
    expect(wrapper.emitted('success')![0][0]).toEqual({
      studentId: 'stu-999',
      studentNumber: 'STD-2026-0099',
      enrollmentId: 'enr-999',
    });
  });
});
