import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { resetDemoData, type DemoResetSummary } from '../services/demoDataApi';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export interface ResetDemoDataMutationOptions extends ToastOptions {
  onSuccess?: (data: DemoResetSummary) => void;
  onError?: (error: unknown) => void;
}

/**
 * Mutation composable for resetting demo data (P6-S11).
 * Follows TanStack Query mutation pattern per ADR-010.
 */
export function useResetDemoData(options?: ResetDemoDataMutationOptions) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  const mutation = useMutation({
    mutationFn: () => resetDemoData(),
    onSuccess: (data) => {
      // Refetch logic to refresh data after successful reset
      queryClient.invalidateQueries();

      notifySuccess(
        toast,
        'Demo Data Reset',
        'All demo data has been purged and re-seeded successfully.',
        options,
      );
      options?.onSuccess?.(data);
    },
    onError: (error) => {
      notifyError(toast, error, 'Demo Data Reset Failed', options);
      options?.onError?.(error);
    },
  });

  return {
    mutate: mutation.mutate,
    mutateAsync: mutation.mutateAsync,
    resetDemoData: mutation.mutate,
    resetDemoDataAsync: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    isSuccess: mutation.isSuccess,
  };
}
