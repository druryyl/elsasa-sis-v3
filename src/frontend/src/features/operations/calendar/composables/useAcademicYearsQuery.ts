import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getAcademicYears,
  getAcademicYearById,
  createAcademicYear,
  updateAcademicYear,
  approveAcademicYear,
  activateAcademicYear,
  applyPreset,
  type AcademicYearDto,
  type CreateAcademicYearPayload,
  type UpdateAcademicYearPayload,
  type ApproveAcademicYearPayload,
  type ApplyPresetResultDto,
} from '../api/academicYear.api';
import { academicYearKeys, operationalPeriodKeys, calendarKeys } from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export { academicYearKeys };
export type { AcademicYearDto, ApplyPresetResultDto, CreateAcademicYearPayload, ApproveAcademicYearPayload };

export interface AcademicYearsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AcademicYearMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Reactive TanStack Query hook fetching all academic years, optionally filtered by status.
 * Uses STATIC cache tier (30m stale, 60m gc, no refetch on window focus).
 */
export function useAcademicYearsQuery(
  params?: MaybeRefOrGetter<{ status?: string } | undefined>,
  options?: AcademicYearsQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => academicYearKeys.list(toValue(params))),
    queryFn: () => getAcademicYears(toValue(params)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.STATIC.staleTime,
    gcTime: CACHE_TIERS.STATIC.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.STATIC.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching a single academic year by UUID.
 */
export function useAcademicYearDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: AcademicYearsQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => academicYearKeys.detail(toValue(id) || '')),
    queryFn: () => getAcademicYearById(toValue(id)!),
    enabled: computed(() => {
      const resolvedId = toValue(id);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.STATIC.staleTime,
    gcTime: CACHE_TIERS.STATIC.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.STATIC.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook creating a new academic year in draft status.
 * Automatically invalidates academic years cache and emits PrimeVue success/error toasts.
 */
export function useCreateAcademicYearMutation(
  options?: AcademicYearMutationOptions<AcademicYearDto, CreateAcademicYearPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: CreateAcademicYearPayload) => createAcademicYear(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      notifySuccess(
        toast,
        'Academic Year Created',
        `Academic year '${data.code}' created successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook updating an existing academic year in draft status.
 */
export function useUpdateAcademicYearMutation(
  options?: AcademicYearMutationOptions<
    AcademicYearDto,
    { id: string; payload: UpdateAcademicYearPayload }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateAcademicYearPayload }) =>
      updateAcademicYear(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      notifySuccess(
        toast,
        'Academic Year Updated',
        `Academic year '${data.code}' updated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook approving an academic year (draft -> approved).
 */
export function useApproveAcademicYearMutation(
  options?: AcademicYearMutationOptions<
    AcademicYearDto,
    { id: string; payload?: ApproveAcademicYearPayload }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload?: ApproveAcademicYearPayload }) =>
      approveAcademicYear(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      notifySuccess(
        toast,
        'Academic Year Approved',
        `Academic year '${data.code}' has been approved.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Approval Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook activating an academic year (approved -> active).
 * Concludes or archives any previously active academic year.
 */
export function useActivateAcademicYearMutation(
  options?: AcademicYearMutationOptions<AcademicYearDto, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (id: string) => activateAcademicYear(id),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      notifySuccess(
        toast,
        'Academic Year Activated',
        `Academic year '${data.code}' is now active.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Activation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook applying the institutional preset (Indonesian 2-semester baseline).
 * Automatically invalidates academic years, operational periods, and calendar matrix caches.
 */
export function useApplyPresetMutation(
  options?: AcademicYearMutationOptions<ApplyPresetResultDto, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (id: string) => applyPreset(id),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      queryClient.invalidateQueries({ queryKey: operationalPeriodKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      notifySuccess(
        toast,
        'Preset Applied',
        `Created ${data.periodsCreated} periods using preset '${data.presetName}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Preset Application Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
