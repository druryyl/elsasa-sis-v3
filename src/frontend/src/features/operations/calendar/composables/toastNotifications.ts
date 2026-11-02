import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';

export interface ToastOptions {
  showToast?: boolean;
}

/**
 * Safely resolves the PrimeVue toast instance, handling execution outside Vue setup
 * or in headless test environments gracefully.
 */
export function tryGetToast(): { add: (options: Record<string, unknown>) => void } | null {
  try {
    const toast = useToast();
    if (toast && typeof toast.add === 'function') {
      return toast;
    }
    return null;
  } catch {
    return null;
  }
}

/**
 * Displays a standard PrimeVue success toast.
 */
export function notifySuccess(
  toast: ReturnType<typeof tryGetToast>,
  summary: string,
  detail: string,
  options?: ToastOptions,
): void {
  if (options?.showToast === false) return;
  toast?.add({
    severity: 'success',
    summary,
    detail,
    life: 4000,
  });
}

/**
 * Displays a standard PrimeVue error toast mapping RFC 7807 problem details or ApiError.
 */
export function notifyError(
  toast: ReturnType<typeof tryGetToast>,
  error: unknown,
  fallbackSummary = 'Operation Failed',
  options?: ToastOptions,
): void {
  if (options?.showToast === false) return;

  if (error instanceof ApiError) {
    const detail =
      error.detail ||
      (error.hasValidationErrors() ? error.getValidationMessages().join('; ') : undefined) ||
      error.title ||
      'Validation or business rule failure.';

    toast?.add({
      severity: 'error',
      summary: error.title || fallbackSummary,
      detail,
      life: 5000,
    });
  } else {
    const message = error instanceof Error ? error.message : 'An unexpected error occurred.';
    toast?.add({
      severity: 'error',
      summary: fallbackSummary,
      detail: message,
      life: 5000,
    });
  }
}
