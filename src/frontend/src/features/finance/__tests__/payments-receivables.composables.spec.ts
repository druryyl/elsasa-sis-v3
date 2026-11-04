// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import {
  financeKeys,
  usePayersQuery,
  usePayerByIdQuery,
  useCreatePayerMutation,
  usePaymentsQuery,
  usePaymentByIdQuery,
  usePaymentReceiptQuery,
  useSubmitPaymentMutation,
  useRecordPaymentMutation,
  useConfirmPaymentMutation,
  useRejectPaymentMutation,
  useSuggestAllocationsQuery,
  useStudentReceivablesQuery,
  useAllocatePaymentMutation,
  useReallocatePaymentMutation,
} from '../index';
import * as payersApi from '../payers/api/payers.api';
import * as paymentsApi from '../payments/api/payments.api';
import * as allocationsApi from '../allocations/api/allocations.api';
import type { PaginatedResponse } from '@/shared/types/common';
import type { PayerDto, PayerListItemDto, CreatePayerResponse } from '../payers/types/payers.types';
import type {
  PaymentDto,
  PaymentListItemDto,
  PaymentReceiptDto,
  SubmitPaymentResponse,
  RecordPaymentResponse,
  ConfirmPaymentResponse,
  RejectPaymentResponse,
} from '../payments/types/payments.types';
import type {
  SuggestAllocationsResponse,
  GetStudentReceivablesResponse,
  AllocatePaymentResponse,
  ReallocatePaymentResponse,
} from '../allocations/types/allocations.types';

const mockToast = {
  add: vi.fn(),
  removeGroup: vi.fn(),
  removeAllGroups: vi.fn(),
};

vi.mock('primevue/usetoast', () => ({
  useToast: () => mockToast,
}));

function withSetup<T>(composable: () => T) {
  let result!: T;
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const app = createApp({
    setup() {
      result = composable();
      return () => {};
    },
  });

  app.use(VueQueryPlugin, { queryClient });
  app.mount(document.createElement('div'));

  return { result, queryClient, app };
}

describe('Finance Payments & Receivables Composables (P5-S14)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Payers Composables (useFinancePayers)', () => {
    it('usePayersQuery fetches paginated payers with filters', async () => {
      const mockResult: PaginatedResponse<PayerListItemDto> = {
        items: [
          {
            id: 'payer-1',
            payerNumber: 'PAYER-2026-0001',
            payerName: 'PT Shell Indonesia',
            payerType: 'sponsor',
            identificationNumber: '01.234.567.8-901.000',
            email: 'csr@shell.co.id',
            phoneNumber: '+628111223344',
            createdAt: '2026-01-01T00:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(payersApi, 'getPayers').mockResolvedValue(mockResult);

      const filters = ref({ searchTerm: 'Shell', payerType: 'sponsor' });
      const { result } = withSetup(() => usePayersQuery(filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockResult);
      expect(payersApi.getPayers).toHaveBeenCalledWith({
        searchTerm: 'Shell',
        payerType: 'sponsor',
      });
    });

    it('usePayersQuery respects enabled option', async () => {
      vi.spyOn(payersApi, 'getPayers');
      const { result } = withSetup(() => usePayersQuery(undefined, { enabled: false }));

      expect(result.fetchStatus.value).toBe('idle');
      expect(payersApi.getPayers).not.toHaveBeenCalled();
    });

    it('usePayerByIdQuery fetches detailed payer dossier and payment history', async () => {
      const mockPayer: PayerDto = {
        id: 'payer-1',
        payerNumber: 'PAYER-2026-0001',
        payerName: 'PT Shell Indonesia',
        payerType: 'sponsor',
        identificationNumber: '01.234.567.8-901.000',
        email: 'csr@shell.co.id',
        phoneNumber: '+628111223344',
        billingAddress: 'Jakarta',
        bankAccountName: 'PT Shell Indonesia',
        bankAccountNumber: '1234567890',
        bankName: 'Bank Mandiri',
        notes: null,
        createdAt: '2026-01-01T00:00:00Z',
        createdBy: 'admin',
        updatedAt: null,
        updatedBy: null,
        totalPaymentsCount: 1,
        totalPaidAmount: 5000000,
        payments: [],
      };
      vi.spyOn(payersApi, 'getPayerById').mockResolvedValue(mockPayer);

      const { result } = withSetup(() => usePayerByIdQuery('payer-1'));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockPayer);
      expect(payersApi.getPayerById).toHaveBeenCalledWith('payer-1');
    });

    it('useCreatePayerMutation registers payer and invalidates payers cache', async () => {
      const mockResponse: CreatePayerResponse = {
        id: 'payer-new',
        payerNumber: 'PAYER-2026-0002',
        payerName: 'Yayasan Beasiswa Indonesia',
        payerType: 'foundation',
      };
      vi.spyOn(payersApi, 'createPayer').mockResolvedValue(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() => useCreatePayerMutation({ onSuccess }));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        name: 'Yayasan Beasiswa Indonesia',
        type: 'foundation',
      });

      expect(payersApi.createPayer).toHaveBeenCalledWith({
        name: 'Yayasan Beasiswa Indonesia',
        type: 'foundation',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payers.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Payer Registered',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, expect.anything());
    });
  });

  describe('Payments Composables (useFinancePayments)', () => {
    it('usePaymentsQuery fetches worklist payments with active filters', async () => {
      const mockResult: PaginatedResponse<PaymentListItemDto> = {
        items: [
          {
            id: 'pay-1',
            paymentNumber: 'PAY-2026-000001',
            studentAccountId: 'acc-1',
            studentId: 'stu-1',
            applicantId: null,
            studentName: 'Ahmad Dahlan',
            studentNumber: '2026001',
            payerId: 'pyr-1',
            payerName: 'Dahlan Parent',
            paymentAmount: 5000000,
            currencyCode: 'IDR',
            paymentMethod: 'bank_transfer',
            externalReference: 'TRX-101',
            paymentDate: '2026-02-01',
            submissionDate: '2026-02-01T10:00:00Z',
            confirmationStatus: 'submitted',
            confirmationDate: null,
            confirmedBy: null,
            receiptNumber: null,
            totalAllocated: 0,
            unallocatedAmount: 5000000,
            hasReceipt: false,
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(paymentsApi, 'getPayments').mockResolvedValue(mockResult);

      const filters = ref({ status: 'submitted' as const });
      const { result } = withSetup(() => usePaymentsQuery(filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockResult);
      expect(paymentsApi.getPayments).toHaveBeenCalledWith({ status: 'submitted' });
    });

    it('usePaymentByIdQuery fetches detailed payment record', async () => {
      const mockPayment: PaymentDto = {
        id: 'pay-1',
        paymentNumber: 'PAY-2026-000001',
        studentAccountId: 'acc-1',
        studentId: 'stu-1',
        applicantId: null,
        studentName: 'Ahmad Dahlan',
        studentNumber: '2026001',
        payerId: 'pyr-1',
        payerName: 'Dahlan Parent',
        payerType: 'parent_guardian',
        paymentAmount: 5000000,
        currencyCode: 'IDR',
        paymentMethod: 'bank_transfer',
        externalReference: 'TRX-101',
        paymentDate: '2026-02-01',
        submissionDate: '2026-02-01T10:00:00Z',
        confirmationStatus: 'submitted',
        confirmationDate: null,
        confirmedBy: null,
        receiptNumber: null,
        receiptIssuedAt: null,
        rejectionReason: null,
        notes: null,
        totalAllocated: 0,
        unallocatedAmount: 5000000,
        allocations: [],
      };
      vi.spyOn(paymentsApi, 'getPaymentById').mockResolvedValue(mockPayment);

      const { result } = withSetup(() => usePaymentByIdQuery('pay-1'));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockPayment);
      expect(paymentsApi.getPaymentById).toHaveBeenCalledWith('pay-1');
    });

    it('usePaymentReceiptQuery fetches official receipt voucher', async () => {
      const mockReceipt: PaymentReceiptDto = {
        receiptNumber: 'REC-2026-000001',
        receiptIssuedAt: '2026-02-01T12:00:00Z',
        paymentNumber: 'PAY-2026-000001',
        paymentDate: '2026-02-01',
        paymentMethod: 'cash',
        amountInFigures: 5000000,
        currencyCode: 'IDR',
        amountInWords: 'Lima Juta Rupiah',
        notes: null,
        schoolHeader: {
          schoolName: 'ELSASA International Islamic School',
          schoolAddress: 'Jl. Raya Pendidikan No. 1',
          schoolPhone: '+62-21-5551234',
          schoolEmail: 'finance@elsasa.sch.id',
          logoUrl: null,
        },
        payer: {
          payerName: 'Dahlan Parent',
          payerType: 'parent_guardian',
          identificationNumber: '3201010101010001',
          email: 'dahlan@parent.com',
          phoneNumber: '+62812345678',
        },
        candidate: {
          candidateName: 'Ahmad Dahlan',
          candidateNumber: '2026001',
          candidateType: 'enrolled_student',
          studentId: 'stu-1',
          applicantId: null,
        },
        allocations: [],
        cashierSignature: {
          cashierName: 'Cashier Staff',
          cashierUsername: 'cashier',
          confirmationDate: '2026-02-01T12:00:00Z',
        },
      };
      vi.spyOn(paymentsApi, 'getPaymentReceipt').mockResolvedValue(mockReceipt);

      const { result } = withSetup(() => usePaymentReceiptQuery('pay-1'));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockReceipt);
      expect(paymentsApi.getPaymentReceipt).toHaveBeenCalledWith('pay-1');
    });

    it('useSubmitPaymentMutation submits payment and invalidates payments, accounts, and receivables', async () => {
      const mockResponse: SubmitPaymentResponse = {
        paymentId: 'pay-new',
        paymentNumber: 'PAY-2026-000002',
        confirmationStatus: 'submitted',
        amount: 2500000,
        submissionDate: '2026-02-01T14:00:00Z',
      };
      vi.spyOn(paymentsApi, 'submitPayment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useSubmitPaymentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        studentId: 'stu-1',
        paymentAmount: 2500000,
        paymentMethod: 'bank_transfer',
        paymentDate: '2026-02-01',
        payerName: 'Parent Name',
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payments.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.receivables.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Payment Submitted',
        }),
      );
    });

    it('useRecordPaymentMutation records cashier payment and invalidates payments, accounts, receivables, and obligations', async () => {
      const mockResponse: RecordPaymentResponse = {
        paymentId: 'pay-pos',
        paymentNumber: 'PAY-2026-000003',
        receiptNumber: 'REC-2026-000002',
        confirmationStatus: 'confirmed',
        amount: 3000000,
        confirmationDate: '2026-02-01T15:00:00Z',
        confirmedBy: 'cashier',
      };
      vi.spyOn(paymentsApi, 'recordAndConfirmPayment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useRecordPaymentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        studentId: 'stu-1',
        paymentAmount: 3000000,
        paymentMethod: 'cash',
        paymentDate: '2026-02-01',
        payerName: 'Parent Cashier',
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payments.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.receivables.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Payment Recorded',
        }),
      );
    });

    it('useConfirmPaymentMutation confirms queue payment and invalidates payments, accounts, receivables, and obligations', async () => {
      const mockResponse: ConfirmPaymentResponse = {
        paymentId: 'pay-1',
        paymentNumber: 'PAY-2026-000001',
        receiptNumber: 'REC-2026-000003',
        confirmationStatus: 'confirmed',
        confirmationDate: '2026-02-01T16:00:00Z',
        confirmedBy: 'cashier',
      };
      vi.spyOn(paymentsApi, 'confirmPayment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useConfirmPaymentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('pay-1');

      expect(paymentsApi.confirmPayment).toHaveBeenCalledWith('pay-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payments.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.receivables.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Payment Confirmed',
        }),
      );
    });

    it('useRejectPaymentMutation rejects payment with reason and invalidates caches', async () => {
      const mockResponse: RejectPaymentResponse = {
        paymentId: 'pay-1',
        paymentNumber: 'PAY-2026-000001',
        confirmationStatus: 'rejected',
        rejectionReason: 'Invalid bank transfer slip',
      };
      vi.spyOn(paymentsApi, 'rejectPayment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useRejectPaymentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        id: 'pay-1',
        payload: { rejectionReason: 'Invalid bank transfer slip' },
      });

      expect(paymentsApi.rejectPayment).toHaveBeenCalledWith('pay-1', {
        rejectionReason: 'Invalid bank transfer slip',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payments.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.receivables.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Payment Rejected',
        }),
      );
    });
  });

  describe('Allocations Composables (useFinanceAllocations)', () => {
    it('useSuggestAllocationsQuery is disabled when paymentAmount is 0 or target missing', async () => {
      vi.spyOn(allocationsApi, 'suggestAllocations');

      const params = ref({ studentId: 'stu-1', paymentAmount: 0 });
      const { result } = withSetup(() => useSuggestAllocationsQuery(params));

      expect(result.fetchStatus.value).toBe('idle');
      expect(allocationsApi.suggestAllocations).not.toHaveBeenCalled();
    });

    it('useSuggestAllocationsQuery fetches distribution suggestions when parameters are valid', async () => {
      const mockSuggestion: SuggestAllocationsResponse = {
        studentAccountId: 'acc-1',
        studentId: 'stu-1',
        applicantId: null,
        paymentAmount: 5000000,
        totalSuggestedAllocated: 5000000,
        unallocatedAmount: 0,
        suggestions: [
          {
            obligationId: 'ob-1',
            obligationNumber: 'OBL-2026-000001',
            chargeType: 'enrollment_fee',
            description: 'Enrollment Fee',
            isMandatory: true,
            dueDate: '2026-02-01',
            netObligation: 5000000,
            alreadyPaid: 0,
            outstandingAmount: 5000000,
            suggestedAmount: 5000000,
            remainingBalanceAfter: 0,
            priorityTier: 1,
          },
        ],
      };
      vi.spyOn(allocationsApi, 'suggestAllocations').mockResolvedValue(mockSuggestion);

      const params = ref({ studentId: 'stu-1', paymentAmount: 5000000 });
      const { result } = withSetup(() => useSuggestAllocationsQuery(params));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockSuggestion);
      expect(allocationsApi.suggestAllocations).toHaveBeenCalledWith({
        studentId: 'stu-1',
        paymentAmount: 5000000,
      });
    });

    it('useStudentReceivablesQuery fetches student receivables dossier', async () => {
      const mockReceivables: GetStudentReceivablesResponse = {
        studentAccountId: 'acc-1',
        studentId: 'stu-1',
        applicantId: null,
        totalNetObligation: 10000000,
        totalPaid: 5000000,
        totalOutstanding: 5000000,
        currentAmount: 5000000,
        aging1To30Days: 0,
        aging31To60Days: 0,
        aging61To90Days: 0,
        aging90PlusDays: 0,
        receivables: [],
      };
      vi.spyOn(allocationsApi, 'getStudentReceivables').mockResolvedValue(mockReceivables);

      const { result } = withSetup(() => useStudentReceivablesQuery('stu-1', { openOnly: true }));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockReceivables);
      expect(allocationsApi.getStudentReceivables).toHaveBeenCalledWith('stu-1', { openOnly: true });
    });

    it('useAllocatePaymentMutation commits allocations and invalidates all related caches', async () => {
      const mockResponse: AllocatePaymentResponse = {
        paymentId: 'pay-1',
        paymentAmount: 5000000,
        totalAllocatedAmount: 5000000,
        remainingUnallocatedAmount: 0,
        allocatedItemsCount: 1,
        results: [
          {
            allocationId: 'alc-1',
            allocationNumber: 'ALC-2026-000001',
            obligationId: 'ob-1',
            obligationNumber: 'OBL-2026-000001',
            allocatedAmount: 5000000,
            obligationStatus: 'settled',
          },
        ],
      };
      vi.spyOn(allocationsApi, 'allocatePayment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useAllocatePaymentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        paymentId: 'pay-1',
        allocations: [{ obligationId: 'ob-1', amount: 5000000 }],
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.allocations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payments.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.receivables.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Allocations Committed',
        }),
      );
    });

    it('useReallocatePaymentMutation commits reallocation lineage and invalidates all related caches', async () => {
      const mockResponse: ReallocatePaymentResponse = {
        paymentId: 'pay-1',
        supersededAllocationsCount: 1,
        newAllocationsCount: 1,
        totalReallocatedAmount: 5000000,
        remainingPaymentBalance: 0,
        supersededAllocationIds: ['alc-old'],
        newAllocations: [
          {
            allocationId: 'alc-new',
            allocationNumber: 'ALC-2026-000002',
            obligationId: 'ob-2',
            obligationNumber: 'OBL-2026-000002',
            allocatedAmount: 5000000,
            obligationStatus: 'settled',
          },
        ],
      };
      vi.spyOn(allocationsApi, 'reallocatePayment').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useReallocatePaymentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        paymentId: 'pay-1',
        targetPriorAllocationIds: ['alc-old'],
        reversalReason: 'Wrong obligation applied by cashier',
        replacementAllocations: [{ obligationId: 'ob-2', amount: 5000000 }],
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.allocations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.payments.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.receivables.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Reallocation Committed',
        }),
      );
    });
  });
});
