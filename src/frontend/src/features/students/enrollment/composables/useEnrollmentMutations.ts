import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { enrollAcceptedApplicant } from '../api/enrollment.api';
import type {
  EnrollAcceptedApplicantPayload,
  EnrollAcceptedApplicantResponse,
} from '../types/enrollment.types';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export interface EnrollMutationOptions<TData = EnrollAcceptedApplicantResponse, TVariables = EnrollAcceptedApplicantPayload>
  extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Executes official enrollment of an accepted applicant into the authoritative student directory (SCR-STU-01, P8-S13).
 *
 * Invalidation Pipeline:
 * - Invalidates `['students', 'admission', 'queue']` to update intake workbench status.
 * - Invalidates `['students', 'admission', 'dossier']` to update applicant dossier details.
 * - Invalidates `['students', 'records']` to refresh student search lists.
 * - Displays success toast containing the newly assigned student number.
 */
export function useEnrollAcceptedApplicantMutation(
  options?: EnrollMutationOptions<EnrollAcceptedApplicantResponse, EnrollAcceptedApplicantPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: EnrollAcceptedApplicantPayload) => enrollAcceptedApplicant(payload),
    onSuccess: (data, variables) => {
      // Invalidate intake queues and dossier caches
      queryClient.invalidateQueries({ queryKey: ['students', 'admission', 'queue'] });
      queryClient.invalidateQueries({ queryKey: ['students', 'admission', 'dossier'] });
      // Invalidate student records queries
      queryClient.invalidateQueries({ queryKey: ['students', 'records'] });

      notifySuccess(
        toast,
        'Enrollment Successful',
        `Applicant successfully enrolled as student ${data.studentNumber}.`,
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Enrollment Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
