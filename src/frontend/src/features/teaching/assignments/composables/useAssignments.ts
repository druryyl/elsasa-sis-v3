import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import {
  acknowledgeTeachingAssignment,
  approvePermanentReassignment,
  approveSubstitutionException,
  approveTeachingAssignment,
  arrangeEmergencySubstitution,
  cancelSubstitution,
  concludeTeachingAssignment,
  confirmReassignmentHandover,
  createTeachingDemand,
  evaluateCandidate,
  getAssignmentCompletions,
  getAssignmentRoles,
  getCandidateEvaluations,
  getCapacityConflicts,
  getTeacherHistory,
  getTeachingAssignmentById,
  getTeachingAssignments,
  getTeachingDemandById,
  getTeachingDemands,
  getTeachingReassignmentById,
  getTeachingReassignments,
  getTeachingSubstitutions,
  proposePermanentReassignment,
  proposeTeachingAssignment,
  resolveCapacityConflict,
  updateTeachingAssignmentStatus,
  updateTeachingDemand,
  updateTeachingDemandStatus,
} from '../api/assignmentsApi';
import {
  assignmentRolesKeys,
  assignmentsKeys,
  completionsKeys,
  conflictsKeys,
  demandsKeys,
  evaluationsKeys,
  historyKeys,
  reassignmentsKeys,
  substitutionsKeys,
  teachingAssignmentsKeys,
} from '../api/queryKeys';
import type {
  AcknowledgeTeachingAssignmentResponse,
  ApprovePermanentReassignmentRequest,
  ApproveSubstitutionExceptionRequest,
  ApproveTeachingAssignmentRequest,
  ApproveTeachingAssignmentResponse,
  ArrangeEmergencySubstitutionRequest,
  ArrangeEmergencySubstitutionResponse,
  AssignmentCompletionDto,
  AssignmentRoleDto,
  CancelSubstitutionRequest,
  CandidateEvaluationSummaryDto,
  CapacityConflictDto,
  ConcludeTeachingAssignmentRequest,
  ConcludeTeachingAssignmentResponse,
  ConfirmReassignmentHandoverRequest,
  CreateTeachingDemandRequest,
  CreateTeachingDemandResponse,
  EvaluateCandidateEligibilityRequest,
  EvaluateCandidateEligibilityResponse,
  GetAssignmentCompletionsParams,
  GetAssignmentRolesParams,
  GetCandidateEvaluationsParams,
  GetCapacityConflictsParams,
  GetTeacherHistoryParams,
  GetTeachingAssignmentsParams,
  GetTeachingDemandsParams,
  GetTeachingReassignmentsParams,
  GetTeachingSubstitutionsParams,
  ProposePermanentReassignmentRequest,
  ProposePermanentReassignmentResponse,
  ProposeTeachingAssignmentRequest,
  ProposeTeachingAssignmentResponse,
  ResolveCapacityConflictRequest,
  ResolveCapacityConflictResponse,
  TeachingAssignmentDto,
  TeachingDemandDto,
  TeachingHistoryEntryDto,
  TeachingReassignmentDto,
  TeachingSubstitutionDto,
  UpdateTeachingAssignmentStatusRequest,
  UpdateTeachingDemandRequest,
  UpdateTeachingDemandStatusRequest,
} from '../api/types';
import { notifyError, notifySuccess, tryGetToast, type ToastOptions } from './toastNotifications';

export {
  demandsKeys,
  evaluationsKeys,
  assignmentsKeys,
  assignmentRolesKeys,
  conflictsKeys,
  substitutionsKeys,
  reassignmentsKeys,
  completionsKeys,
  historyKeys,
  teachingAssignmentsKeys,
};

export interface AssignmentsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AssignmentsMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Reactive TanStack Query hook fetching teaching demands list (P7-S21, P2-S04, SCR-TCH-02).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useDemands(
  params?: MaybeRefOrGetter<GetTeachingDemandsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<PaginatedResponse<TeachingDemandDto>>({
    queryKey: computed(() => demandsKeys.list(resolvedParams.value)),
    queryFn: () => getTeachingDemands(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching a specific teaching demand by ID (P7-S21, P2-S04).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc).
 */
export function useDemandById(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedId = computed(() => toValue(id));

  return useQuery<TeachingDemandDto>({
    queryKey: computed(() => demandsKeys.detail(resolvedId.value)),
    queryFn: () => getTeachingDemandById(resolvedId.value!),
    enabled: computed(() => {
      const hasId = Boolean(resolvedId.value && resolvedId.value.trim() !== '');
      return hasId && (options?.enabled !== undefined ? toValue(options.enabled) : true);
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching candidate eligibility evaluations (P7-S21, P2-S05, SCR-TCH-02).
 * Uses TRANSACTIONAL cache tier.
 */
export function useEvaluations(
  params?: MaybeRefOrGetter<GetCandidateEvaluationsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<CandidateEvaluationSummaryDto[]>({
    queryKey: computed(() => evaluationsKeys.list(resolvedParams.value)),
    queryFn: () => getCandidateEvaluations(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching teaching assignments list (P7-S21, P3-S06, SCR-TCH-02).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useAssignments(
  params?: MaybeRefOrGetter<GetTeachingAssignmentsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<PaginatedResponse<TeachingAssignmentDto>>({
    queryKey: computed(() => assignmentsKeys.list(resolvedParams.value)),
    queryFn: () => getTeachingAssignments(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

export const useTeachingAssignments = useAssignments;

/**
 * Reactive TanStack Query hook fetching a specific teaching assignment by ID (P7-S21, P3-S06).
 * Uses TRANSACTIONAL cache tier.
 */
export function useAssignmentById(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedId = computed(() => toValue(id));

  return useQuery<TeachingAssignmentDto>({
    queryKey: computed(() => assignmentsKeys.detail(resolvedId.value)),
    queryFn: () => getTeachingAssignmentById(resolvedId.value!),
    enabled: computed(() => {
      const hasId = Boolean(resolvedId.value && resolvedId.value.trim() !== '');
      return hasId && (options?.enabled !== undefined ? toValue(options.enabled) : true);
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching configurable assignment roles catalog (P7-S21, P3-S06, SCR-TCH-02).
 * Uses STATIC cache tier (30m stale, 60m gc, no refetch on focus).
 */
export function useAssignmentRoles(
  params?: MaybeRefOrGetter<GetAssignmentRolesParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<AssignmentRoleDto[]>({
    queryKey: computed(() => assignmentRolesKeys.list(resolvedParams.value)),
    queryFn: () => getAssignmentRoles(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.STATIC.staleTime,
    gcTime: CACHE_TIERS.STATIC.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.STATIC.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching capacity conflicts worklist (P7-S21, P3-S07, SCR-TCH-02).
 * Uses ACTIVE_QUEUE cache tier.
 */
export function useCapacityConflicts(
  params?: MaybeRefOrGetter<GetCapacityConflictsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<PaginatedResponse<CapacityConflictDto>>({
    queryKey: computed(() => conflictsKeys.list(resolvedParams.value)),
    queryFn: () => getCapacityConflicts(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching emergency substitutions worklist (P7-S21, P4-S11, SCR-TCH-02).
 * Uses ACTIVE_QUEUE cache tier.
 */
export function useSubstitutions(
  params?: MaybeRefOrGetter<GetTeachingSubstitutionsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<PaginatedResponse<TeachingSubstitutionDto>>({
    queryKey: computed(() => substitutionsKeys.list(resolvedParams.value)),
    queryFn: () => getTeachingSubstitutions(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching permanent reassignments worklist (P7-S21, P4-S12, SCR-TCH-02).
 * Uses ACTIVE_QUEUE cache tier.
 */
export function useReassignments(
  params?: MaybeRefOrGetter<GetTeachingReassignmentsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<PaginatedResponse<TeachingReassignmentDto>>({
    queryKey: computed(() => reassignmentsKeys.list(resolvedParams.value)),
    queryFn: () => getTeachingReassignments(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching a specific reassignment by ID (P7-S21, P4-S12).
 * Uses TRANSACTIONAL cache tier.
 */
export function useReassignmentById(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedId = computed(() => toValue(id));

  return useQuery<TeachingReassignmentDto>({
    queryKey: computed(() => reassignmentsKeys.detail(resolvedId.value)),
    queryFn: () => getTeachingReassignmentById(resolvedId.value!),
    enabled: computed(() => {
      const hasId = Boolean(resolvedId.value && resolvedId.value.trim() !== '');
      return hasId && (options?.enabled !== undefined ? toValue(options.enabled) : true);
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching assignment completions and capacity release records (P7-S21, P4-S13, SCR-TCH-02).
 * Uses TRANSACTIONAL cache tier.
 */
export function useCompletions(
  params?: MaybeRefOrGetter<GetAssignmentCompletionsParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<AssignmentCompletionDto[]>({
    queryKey: computed(() => completionsKeys.list(resolvedParams.value)),
    queryFn: () => getAssignmentCompletions(resolvedParams.value),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching teacher immutable history audit log (P7-S21, P4-S14, SCR-TCH-02).
 * Uses TRANSACTIONAL cache tier.
 */
export function useTeacherHistory(
  teacherId: MaybeRefOrGetter<string | null | undefined>,
  params?: MaybeRefOrGetter<GetTeacherHistoryParams | undefined>,
  options?: AssignmentsQueryOptions,
) {
  const resolvedTeacherId = computed(() => toValue(teacherId));
  const resolvedParams = computed(() => toValue(params));

  return useQuery<TeachingHistoryEntryDto[]>({
    queryKey: computed(() => historyKeys.teacher(resolvedTeacherId.value, resolvedParams.value)),
    queryFn: () => getTeacherHistory(resolvedTeacherId.value!, resolvedParams.value),
    enabled: computed(() => {
      const hasTeacherId = Boolean(resolvedTeacherId.value && resolvedTeacherId.value.trim() !== '');
      return hasTeacherId && (options?.enabled !== undefined ? toValue(options.enabled) : true);
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Mutation hook for creating/ingesting a teaching demand (P7-S21, P2-S04).
 */
export function useCreateDemandMutation(
  options?: AssignmentsMutationOptions<CreateTeachingDemandResponse, CreateTeachingDemandRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<CreateTeachingDemandResponse, unknown, CreateTeachingDemandRequest>({
    mutationFn: (payload) => createTeachingDemand(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      notifySuccess(
        toast,
        'Demand Created',
        `Teaching demand ${data.demandCode} has been submitted.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Create Teaching Demand', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for updating mutable teaching demand parameters (P7-S21, P2-S04).
 */
export function useUpdateDemandMutation(
  options?: AssignmentsMutationOptions<
    TeachingDemandDto,
    { id: string; payload: UpdateTeachingDemandRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingDemandDto,
    unknown,
    { id: string; payload: UpdateTeachingDemandRequest }
  >({
    mutationFn: ({ id, payload }) => updateTeachingDemand(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      notifySuccess(
        toast,
        'Demand Updated',
        `Teaching demand ${data.demandCode} has been updated.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Update Demand', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for transitioning teaching demand lifecycle status (P7-S21, P2-S04).
 */
export function useUpdateDemandStatusMutation(
  options?: AssignmentsMutationOptions<
    TeachingDemandDto,
    { id: string; payload: UpdateTeachingDemandStatusRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingDemandDto,
    unknown,
    { id: string; payload: UpdateTeachingDemandStatusRequest }
  >({
    mutationFn: ({ id, payload }) => updateTeachingDemandStatus(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      notifySuccess(
        toast,
        'Demand Status Updated',
        `Demand status changed to ${data.status}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Update Demand Status', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for evaluating candidate teacher eligibility (P7-S21, P2-S05).
 */
export function useEvaluateCandidateMutation(
  options?: AssignmentsMutationOptions<
    EvaluateCandidateEligibilityResponse,
    EvaluateCandidateEligibilityRequest
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    EvaluateCandidateEligibilityResponse,
    unknown,
    EvaluateCandidateEligibilityRequest
  >({
    mutationFn: (payload) => evaluateCandidate(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: evaluationsKeys.all });
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      const summary = data.isEligible ? 'Candidate Eligible' : 'Candidate Ineligible';
      notifySuccess(toast, summary, data.explanation, options);
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Candidate Evaluation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for proposing a teaching assignment (P7-S21, P3-S06).
 */
export function useProposeAssignmentMutation(
  options?: AssignmentsMutationOptions<
    ProposeTeachingAssignmentResponse,
    ProposeTeachingAssignmentRequest
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ProposeTeachingAssignmentResponse,
    unknown,
    ProposeTeachingAssignmentRequest
  >({
    mutationFn: (payload) => proposeTeachingAssignment(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Assignment Proposed',
        `Teaching assignment ${data.assignmentNumber} proposed successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Propose Assignment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for approving a proposed teaching assignment (P7-S21, P3-S06).
 */
export function useApproveAssignmentMutation(
  options?: AssignmentsMutationOptions<
    ApproveTeachingAssignmentResponse,
    { id: string; payload?: ApproveTeachingAssignmentRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ApproveTeachingAssignmentResponse,
    unknown,
    { id: string; payload?: ApproveTeachingAssignmentRequest }
  >({
    mutationFn: ({ id, payload }) => approveTeachingAssignment(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      queryClient.invalidateQueries({ queryKey: conflictsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Assignment Approved',
        `Assignment ${data.assignmentNumber} has been approved and capacity committed.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Approve Assignment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for teacher acknowledging an assignment (P7-S21, P3-S06).
 */
export function useAcknowledgeAssignmentMutation(
  options?: AssignmentsMutationOptions<AcknowledgeTeachingAssignmentResponse, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<AcknowledgeTeachingAssignmentResponse, unknown, string>({
    mutationFn: (id) => acknowledgeTeachingAssignment(id),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      notifySuccess(
        toast,
        'Assignment Acknowledged',
        `Assignment ${data.assignmentNumber} acknowledged.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Acknowledge Assignment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for updating teaching assignment status (P7-S21, P3-S06).
 */
export function useUpdateAssignmentStatusMutation(
  options?: AssignmentsMutationOptions<
    TeachingAssignmentDto,
    { id: string; payload: UpdateTeachingAssignmentStatusRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingAssignmentDto,
    unknown,
    { id: string; payload: UpdateTeachingAssignmentStatusRequest }
  >({
    mutationFn: ({ id, payload }) => updateTeachingAssignmentStatus(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: demandsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Assignment Status Updated',
        `Assignment ${data.assignmentNumber} status changed to ${data.status}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Update Assignment Status', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for resolving a capacity conflict (P7-S21, P3-S07).
 */
export function useResolveConflictMutation(
  options?: AssignmentsMutationOptions<
    ResolveCapacityConflictResponse,
    { id: string; payload: ResolveCapacityConflictRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ResolveCapacityConflictResponse,
    unknown,
    { id: string; payload: ResolveCapacityConflictRequest }
  >({
    mutationFn: ({ id, payload }) => resolveCapacityConflict(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: conflictsKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Conflict Resolved',
        `Capacity conflict marked as ${data.status}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Resolve Capacity Conflict', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for arranging emergency substitution (P7-S21, P4-S11, Resolution 3.8).
 */
export function useArrangeSubstituteMutation(
  options?: AssignmentsMutationOptions<
    ArrangeEmergencySubstitutionResponse,
    ArrangeEmergencySubstitutionRequest
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ArrangeEmergencySubstitutionResponse,
    unknown,
    ArrangeEmergencySubstitutionRequest
  >({
    mutationFn: (payload) => arrangeEmergencySubstitution(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: substitutionsKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: conflictsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      const summary = data.isDelegatedApproval
        ? 'Substitute Assigned'
        : 'Substitute Exception Escalated';
      const detail = data.isDelegatedApproval
        ? `Substitution ${data.substitutionNumber} directly committed under delegated authority.`
        : `Substitution ${data.substitutionNumber} requires Teaching Authority approval.`;
      notifySuccess(toast, summary, detail, options);
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Arrange Emergency Substitution', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for approving escalated substitute exception (P7-S21, P4-S11).
 */
export function useApproveSubstituteExceptionMutation(
  options?: AssignmentsMutationOptions<
    TeachingSubstitutionDto,
    { id: string; payload?: ApproveSubstitutionExceptionRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingSubstitutionDto,
    unknown,
    { id: string; payload?: ApproveSubstitutionExceptionRequest }
  >({
    mutationFn: ({ id, payload }) => approveSubstitutionException(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: substitutionsKeys.all });
      queryClient.invalidateQueries({ queryKey: conflictsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Substitute Exception Approved',
        `Substitution ${data.substitutionNumber} approved by Teaching Authority.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Approve Substitution Exception', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for cancelling a substitution arrangement (P7-S21, P4-S11).
 */
export function useCancelSubstitutionMutation(
  options?: AssignmentsMutationOptions<
    TeachingSubstitutionDto,
    { id: string; payload?: CancelSubstitutionRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingSubstitutionDto,
    unknown,
    { id: string; payload?: CancelSubstitutionRequest }
  >({
    mutationFn: ({ id, payload }) => cancelSubstitution(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: substitutionsKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Substitution Cancelled',
        `Substitution ${data.substitutionNumber} cancelled.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Cancel Substitution', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for proposing permanent reassignment (P7-S21, P4-S12).
 */
export function useProposeReassignmentMutation(
  options?: AssignmentsMutationOptions<
    ProposePermanentReassignmentResponse,
    ProposePermanentReassignmentRequest
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ProposePermanentReassignmentResponse,
    unknown,
    ProposePermanentReassignmentRequest
  >({
    mutationFn: (payload) => proposePermanentReassignment(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: reassignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      notifySuccess(
        toast,
        'Reassignment Proposed',
        `Reassignment ${data.reassignment.reassignmentNumber} proposed.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Propose Permanent Reassignment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for approving permanent reassignment (P7-S21, P4-S12).
 */
export function useApproveReassignmentMutation(
  options?: AssignmentsMutationOptions<
    TeachingReassignmentDto,
    { id: string; payload?: ApprovePermanentReassignmentRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingReassignmentDto,
    unknown,
    { id: string; payload?: ApprovePermanentReassignmentRequest }
  >({
    mutationFn: ({ id, payload }) => approvePermanentReassignment(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: reassignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Reassignment Approved',
        `Reassignment ${data.reassignmentNumber} approved.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Approve Permanent Reassignment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for confirming reassignment handover (P7-S21, P4-S12).
 */
export function useConfirmReassignmentHandoverMutation(
  options?: AssignmentsMutationOptions<
    TeachingReassignmentDto,
    { id: string; payload?: ConfirmReassignmentHandoverRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    TeachingReassignmentDto,
    unknown,
    { id: string; payload?: ConfirmReassignmentHandoverRequest }
  >({
    mutationFn: ({ id, payload }) => confirmReassignmentHandover(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: reassignmentsKeys.all });
      notifySuccess(
        toast,
        'Handover Confirmed',
        `Reassignment ${data.reassignmentNumber} handover marked as ${data.handoverStatus}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Confirm Reassignment Handover', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for concluding teaching assignment and releasing capacity (P7-S21, P4-S13).
 */
export function useConcludeAssignmentMutation(
  options?: AssignmentsMutationOptions<
    ConcludeTeachingAssignmentResponse,
    { id: string; payload: ConcludeTeachingAssignmentRequest }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ConcludeTeachingAssignmentResponse,
    unknown,
    { id: string; payload: ConcludeTeachingAssignmentRequest }
  >({
    mutationFn: ({ id, payload }) => concludeTeachingAssignment(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: completionsKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentsKeys.all });
      queryClient.invalidateQueries({ queryKey: ['teaching', 'workload'] });
      notifySuccess(
        toast,
        'Assignment Concluded',
        `Assignment ${data.assignmentNumber} concluded. Released ${data.releasedWorkloadHours} teaching hours.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Conclude Assignment', options);
      options?.onError?.(error, variables);
    },
  });
}
