import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import {
  getPayments,
  getPaymentById,
  getPaymentReceipt,
  submitPayment,
  recordAndConfirmPayment,
  confirmPayment,
  rejectPayment,
} from '../api/payments.api';
import type {
  ConfirmPaymentResponse,
  GetPaymentsParams,
  PaymentDto,
  PaymentListItemDto,
  PaymentReceiptDto,
  RecordPaymentPayload,
  RecordPaymentResponse,
  RejectPaymentPayload,
  RejectPaymentResponse,
  SubmitPaymentPayload,
  SubmitPaymentResponse,
} from '../types/payments.types';
import { financeKeys } from '../../shared/queryKeys';
import {
  tryGetToast,
  notifySuccess,
  notifyError,
  type FinanceMutationOptions,
  type FinanceQueryOptions,
} from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook fetching payment transactions and verification queue worklists (SCR-FIN-02).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function usePaymentsQuery(
  filters?: MaybeRefOrGetter<GetPaymentsParams | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaginatedResponse<PaymentListItemDto>>({
    queryKey: computed(() => financeKeys.payments.list(toValue(filters))),
    queryFn: () => getPayments(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching detailed payment dossier with payer and allocation breakdown (SCR-FIN-02, SCR-FIN-03).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function usePaymentByIdQuery(
  paymentId: MaybeRefOrGetter<string | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaymentDto>({
    queryKey: computed(() => financeKeys.payments.detail(toValue(paymentId) || '')),
    queryFn: () => getPaymentById(toValue(paymentId)!),
    enabled: computed(() => {
      const resolvedId = toValue(paymentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching official receipt voucher details for print/download (SCR-FIN-02, SCR-SELF-02).
 * Uses TRANSACTIONAL cache tier.
 */
export function usePaymentReceiptQuery(
  paymentId: MaybeRefOrGetter<string | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaymentReceiptDto>({
    queryKey: computed(() => financeKeys.payments.receipt(toValue(paymentId) || '')),
    queryFn: () => getPaymentReceipt(toValue(paymentId)!),
    enabled: computed(() => {
      const resolvedId = toValue(paymentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook submitting a payment via self-service portal in submitted status (SCR-SELF-02).
 * Invalidates payments, student accounts, and receivables query caches upon success.
 */
export function useSubmitPaymentMutation(
  options?: FinanceMutationOptions<SubmitPaymentResponse, SubmitPaymentPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: SubmitPaymentPayload) => submitPayment(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.payments.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.receivables.all });

      notifySuccess(
        toast,
        'Payment Submitted',
        `Payment '${data.paymentNumber}' submitted successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Payment Submission Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook recording walk-in counter cash/POS payment with atomic receipt issuance (SCR-FIN-02).
 * Invalidates payments, accounts, receivables, and obligations caches upon success.
 */
export function useRecordPaymentMutation(
  options?: FinanceMutationOptions<RecordPaymentResponse, RecordPaymentPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: RecordPaymentPayload) => recordAndConfirmPayment(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.payments.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.receivables.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });

      notifySuccess(
        toast,
        'Payment Recorded',
        `Payment recorded successfully with receipt '${data.receiptNumber}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Record Payment Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export type ConfirmPaymentVariables = string | { id: string };

/**
 * Mutation hook confirming a submitted payment in the verification queue and issuing official receipt (SCR-FIN-02).
 * Invalidates payments, accounts, receivables, and obligations caches upon success.
 */
export function useConfirmPaymentMutation(
  options?: FinanceMutationOptions<ConfirmPaymentResponse, ConfirmPaymentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ConfirmPaymentVariables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return confirmPayment(id);
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.payments.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.receivables.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });

      notifySuccess(
        toast,
        'Payment Confirmed',
        `Payment confirmed successfully with receipt '${data.receiptNumber}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Payment Confirmation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface RejectPaymentVariables {
  id: string;
  payload: RejectPaymentPayload;
}

/**
 * Mutation hook rejecting an unverified submitted payment with mandatory justification reason (SCR-FIN-02).
 * Invalidates payments, accounts, and receivables caches upon success.
 */
export function useRejectPaymentMutation(
  options?: FinanceMutationOptions<RejectPaymentResponse, RejectPaymentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, payload }: RejectPaymentVariables) => rejectPayment(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.payments.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.receivables.all });

      notifySuccess(
        toast,
        'Payment Rejected',
        `Payment '${data.paymentNumber}' has been rejected.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Payment Rejection Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
