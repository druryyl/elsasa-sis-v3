import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  batchRegisterCohortParticipation,
  concludeParticipation,
  getParticipationsByUnit,
  getStudentParticipations,
  recognizePriorCredit,
  registerParticipation,
  updateParticipationStatus,
} from '../api/participation.api';
import type {
  AcademicParticipation,
  BatchRegisterCohortRequest,
  BatchRegisterCohortResponse,
  ConcludeParticipationRequest,
  ConcludeParticipationResponse,
  RecognizePriorCreditRequest,
  RecognizePriorCreditResponse,
  RegisterParticipationRequest,
  RegisterParticipationResponse,
  StudentParticipation,
  StudentParticipationsFilter,
  UnitParticipationsFilter,
  UpdateParticipationStatusRequest,
  UpdateParticipationStatusResponse,
} from '../types';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Query Key Constants & Factories
// ============================================================================

export const academicParticipationKeys = {
  all: ['academics', 'participations'] as const,
  unitParticipations: (
    unitId: string | null | undefined,
    periodId?: string | null | undefined,
    sectionLabel?: string | null | undefined,
    status?: string | null | undefined,
  ) =>
    ['academics', 'participations', 'unit', unitId, { periodId, sectionLabel, status }] as const,
  studentParticipations: (
    studentId: string | null | undefined,
    academicPeriodId?: string | null | undefined,
  ) =>
    ['academics', 'participations', 'student', studentId, { academicPeriodId }] as const,
};

// ============================================================================
// Composable Options Types
// ============================================================================

export interface AcademicParticipationQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AcademicParticipationMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface ConcludeParticipationVariables {
  id: string;
  request?: ConcludeParticipationRequest;
}

export interface UpdateParticipationStatusVariables {
  id: string;
  request: UpdateParticipationStatusRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching student participations enrolled in a learning unit (SCR-ACD-05, P7-S18).
 * Supports passing unitId, periodId, and sectionLabel directly or as a filter object.
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useUnitParticipationsQuery(
  unitId: MaybeRefOrGetter<string | null | undefined>,
  periodIdOrFilter?: MaybeRefOrGetter<string | UnitParticipationsFilter | null | undefined>,
  sectionLabel?: MaybeRefOrGetter<string | null | undefined>,
  status?: MaybeRefOrGetter<string | null | undefined>,
  options?: AcademicParticipationQueryOptions,
) {
  const resolvedFilter = computed<UnitParticipationsFilter | undefined>(() => {
    const secondArg = toValue(periodIdOrFilter);
    if (typeof secondArg === 'object' && secondArg !== null) {
      return secondArg;
    }

    const filterObj: UnitParticipationsFilter = {};
    if (typeof secondArg === 'string' && secondArg.length > 0) {
      filterObj.academicPeriodId = secondArg;
    }
    const resolvedSection = toValue(sectionLabel);
    if (resolvedSection) {
      filterObj.sectionLabel = resolvedSection;
    }
    const resolvedStatus = toValue(status);
    if (resolvedStatus) {
      filterObj.status = resolvedStatus as UnitParticipationsFilter['status'];
    }

    return Object.keys(filterObj).length > 0 ? filterObj : undefined;
  });

  return useQuery<AcademicParticipation[]>({
    queryKey: computed(() => {
      const resolvedUnitId = toValue(unitId);
      const filter = resolvedFilter.value;
      return academicParticipationKeys.unitParticipations(
        resolvedUnitId,
        filter?.academicPeriodId,
        filter?.sectionLabel,
        filter?.status,
      );
    }),
    queryFn: () => {
      const resolvedUnitId = toValue(unitId);
      if (!resolvedUnitId) {
        throw new Error('Learning Unit ID is required to fetch participations.');
      }
      return getParticipationsByUnit(resolvedUnitId, resolvedFilter.value);
    },
    enabled: computed(() => {
      const resolvedUnitId = toValue(unitId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedUnitId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching all learning participations for a student (P7-S18).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentParticipationsQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  academicPeriodIdOrFilter?: MaybeRefOrGetter<string | StudentParticipationsFilter | null | undefined>,
  options?: AcademicParticipationQueryOptions,
) {
  const resolvedFilter = computed<StudentParticipationsFilter | undefined>(() => {
    const val = toValue(academicPeriodIdOrFilter);
    if (typeof val === 'object' && val !== null) {
      return val;
    }
    if (typeof val === 'string' && val.length > 0) {
      return { academicPeriodId: val };
    }
    return undefined;
  });

  return useQuery<StudentParticipation[]>({
    queryKey: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const filter = resolvedFilter.value;
      return academicParticipationKeys.studentParticipations(
        resolvedStudentId,
        filter?.academicPeriodId,
      );
    }),
    queryFn: () => {
      const resolvedStudentId = toValue(studentId);
      if (!resolvedStudentId) {
        throw new Error('Student ID is required to fetch student participations.');
      }
      return getStudentParticipations(resolvedStudentId, resolvedFilter.value);
    },
    enabled: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedStudentId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutations
// ============================================================================

/**
 * Mutation hook for registering an active cohort of students into learning units (P2-S04, TD-02).
 * Invalidates unit participations and roster queries upon success.
 */
export function useBatchRegisterCohortMutation(
  options?: AcademicParticipationMutationOptions<BatchRegisterCohortResponse, BatchRegisterCohortRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: BatchRegisterCohortRequest) => batchRegisterCohortParticipation(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicParticipationKeys.all });
      notifySuccess(
        toast,
        'Cohort Registered',
        `Successfully registered ${data.participationsCreated} student participations for cohort '${data.sectionLabel}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Batch Registration Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for registering an individual student academic participation (P2-S04, TD-02).
 * Invalidates unit participations and roster queries upon success.
 */
export function useRegisterParticipationMutation(
  options?: AcademicParticipationMutationOptions<RegisterParticipationResponse, RegisterParticipationRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: RegisterParticipationRequest) => registerParticipation(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicParticipationKeys.all });
      notifySuccess(
        toast,
        'Participation Registered',
        `Student participation registered successfully (Attempt #${data.attemptNumber}).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Participation Registration Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for recognizing prior or transfer learning credit (P2-S05, UC-ACD-10).
 * Invalidates unit participations and roster queries upon success.
 */
export function useRecognizePriorCreditMutation(
  options?: AcademicParticipationMutationOptions<RecognizePriorCreditResponse, RecognizePriorCreditRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: RecognizePriorCreditRequest) => recognizePriorCredit(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicParticipationKeys.all });
      notifySuccess(
        toast,
        'Prior Credit Recognized',
        `Prior credit recognized successfully with status '${data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Prior Credit Recognition Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for formally concluding an academic participation upon period end (P2-S05, UC-ACD-12).
 * Invalidates unit participations and roster queries upon success.
 */
export function useConcludeParticipationMutation(
  options?: AcademicParticipationMutationOptions<ConcludeParticipationResponse, ConcludeParticipationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: ConcludeParticipationVariables) =>
      concludeParticipation(id, request ?? {}),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicParticipationKeys.all });
      notifySuccess(
        toast,
        'Participation Concluded',
        `Academic participation completed successfully on ${data.completionDate}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Participation Conclusion Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for transitioning participation status (in_progress, paused, withdrawn) (P2-S05).
 * Invalidates unit participations and roster queries upon success.
 */
export function useUpdateParticipationStatusMutation(
  options?: AcademicParticipationMutationOptions<UpdateParticipationStatusResponse, UpdateParticipationStatusVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: UpdateParticipationStatusVariables) =>
      updateParticipationStatus(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: academicParticipationKeys.all });
      notifySuccess(
        toast,
        'Status Updated',
        `Academic participation status transitioned to '${data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Status Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
