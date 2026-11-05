import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  approveLearningStructure,
  archiveLearningStructure,
  createAcademicRequirement,
  createLearningStructure,
  createLearningUnit,
  deleteAcademicRequirement,
  deleteLearningUnit,
  getAcademicRequirements,
  getLearningStructureById,
  getLearningStructures,
  getLearningUnits,
  submitLearningStructure,
  supersedeLearningStructure,
  updateAcademicRequirement,
  updateLearningStructure,
  updateLearningUnit,
} from '../api/curriculumApi';
import type {
  AcademicRequirement,
  AcademicRequirementFilters,
  AcademicRequirementScope,
  AcademicRequirementType,
  ApproveLearningStructureResponse,
  ArchiveLearningStructureResponse,
  CreateAcademicRequirementRequest,
  CreateLearningStructureRequest,
  CreateLearningUnitRequest,
  LearningStructure,
  LearningStructureFilters,
  LearningStructureStatus,
  LearningStructureSummary,
  LearningUnit,
  LearningUnitFilters,
  LearningUnitTreeNode,
  SubmitLearningStructureResponse,
  SupersedeStructureRequest,
  UpdateAcademicRequirementRequest,
  UpdateLearningStructureRequest,
  UpdateLearningUnitRequest,
} from '../types/curriculum.types';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Query Key Constants & Builders
// ============================================================================

export const curriculumKeys = {
  all: ['academics'] as const,
  structures: {
    all: ['academics', 'structures'] as const,
    list: (filters?: LearningStructureFilters) =>
      filters !== undefined
        ? (['academics', 'structures', filters] as const)
        : (['academics', 'structures'] as const),
    detail: (id: string | null | undefined) => ['academics', 'structure', id] as const,
  },
  units: {
    all: ['academics', 'units'] as const,
    list: (structureId: string | null | undefined, filters?: LearningUnitFilters) =>
      filters !== undefined
        ? (['academics', 'units', structureId, filters] as const)
        : (['academics', 'units', structureId] as const),
  },
  requirements: {
    all: ['academics', 'requirements'] as const,
    list: (
      structureId: string | null | undefined,
      filters?: AcademicRequirementFilters,
    ) =>
      filters !== undefined
        ? (['academics', 'requirements', structureId, filters] as const)
        : (['academics', 'requirements', structureId] as const),
  },
};

// ============================================================================
// Query & Mutation Option Types
// ============================================================================

export interface CurriculumQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface CurriculumMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Tree Hierarchy Assembly Helper (2-Level Cap, ADR-005, TD-04)
// ============================================================================

/**
 * Assembles a flat collection of LearningUnit entities into a 2-level parent-child tree
 * compatible with PrimeVue TreeTable and nested components.
 */
export function buildLearningUnitTree(units: LearningUnit[]): LearningUnitTreeNode[] {
  if (!Array.isArray(units) || units.length === 0) {
    return [];
  }

  const rootNodes: LearningUnitTreeNode[] = [];
  const childrenByParentId = new Map<string, LearningUnitTreeNode[]>();
  const unitMap = new Map<string, LearningUnit>();

  // Index units for fast lookup
  for (const unit of units) {
    unitMap.set(unit.id, unit);
  }

  // Group child units by parentUnitId
  for (const unit of units) {
    if (unit.parentUnitId) {
      const childNode: LearningUnitTreeNode = {
        ...unit,
        key: unit.id,
        data: unit,
        children: [],
      };
      const existing = childrenByParentId.get(unit.parentUnitId) ?? [];
      existing.push(childNode);
      childrenByParentId.set(unit.parentUnitId, existing);
    }
  }

  // Construct root nodes with children
  for (const unit of units) {
    if (!unit.parentUnitId) {
      const children = childrenByParentId.get(unit.id) ?? [];
      rootNodes.push({
        ...unit,
        key: unit.id,
        data: unit,
        children,
      });
    } else if (!unitMap.has(unit.parentUnitId)) {
      // Orphaned child unit fallback: surface at root level so data is accessible
      rootNodes.push({
        ...unit,
        key: unit.id,
        data: unit,
        children: [],
      });
    }
  }

  return rootNodes;
}

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Reactive TanStack Query hook fetching paginated learning structures (SCR-ACD-03, P5-S15).
 * Cache key: `['academics', 'structures', filters]`.
 */
export function useLearningStructures(
  filtersOrPeriodId?: MaybeRefOrGetter<
    LearningStructureFilters | string | null | undefined
  >,
  status?: MaybeRefOrGetter<LearningStructureStatus | string | null | undefined>,
  search?: MaybeRefOrGetter<string | null | undefined>,
  page?: MaybeRefOrGetter<number | undefined>,
  pageSize?: MaybeRefOrGetter<number | undefined>,
  options?: CurriculumQueryOptions,
) {
  const resolvedFilters = computed<LearningStructureFilters | undefined>(() => {
    const first = toValue(filtersOrPeriodId);
    if (typeof first === 'object' && first !== null) {
      return first;
    }
    if (
      typeof first === 'string' ||
      status !== undefined ||
      search !== undefined ||
      page !== undefined ||
      pageSize !== undefined
    ) {
      const filterObj: LearningStructureFilters = {};
      if (typeof first === 'string') {
        filterObj.academicPeriodId = first;
      }
      const resolvedStatus = toValue(status);
      if (resolvedStatus !== undefined) {
        filterObj.status = resolvedStatus;
      }
      const resolvedSearch = toValue(search);
      if (resolvedSearch !== undefined) {
        filterObj.search = resolvedSearch;
      }
      const resolvedPage = toValue(page);
      if (resolvedPage !== undefined) {
        filterObj.page = resolvedPage;
      }
      const resolvedPageSize = toValue(pageSize);
      if (resolvedPageSize !== undefined) {
        filterObj.pageSize = resolvedPageSize;
      }
      return filterObj;
    }
    return undefined;
  });

  return useQuery({
    queryKey: computed(() => {
      const filters = resolvedFilters.value;
      return filters !== undefined
        ? ['academics', 'structures', filters]
        : ['academics', 'structures'];
    }),
    queryFn: () => getLearningStructures(resolvedFilters.value),
    enabled: computed(() =>
      options?.enabled !== undefined ? toValue(options.enabled) : true,
    ),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching a learning structure by identifier (SCR-ACD-03, P5-S15).
 * Cache key: `['academics', 'structure', structureId]`.
 */
export function useLearningStructureById(
  structureId: MaybeRefOrGetter<string | null | undefined>,
  options?: CurriculumQueryOptions,
) {
  return useQuery<LearningStructure>({
    queryKey: computed(() => ['academics', 'structure', toValue(structureId)]),
    queryFn: () => getLearningStructureById(toValue(structureId)!),
    enabled: computed(() => {
      const id = toValue(structureId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(id) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching learning units for a framework (TD-04, P5-S15).
 * Cache key: `['academics', 'units', structureId]`.
 * Assembles a 2-level hierarchy tree accessible through `learningUnitTree`.
 */
export function useLearningUnits(
  structureId: MaybeRefOrGetter<string | null | undefined>,
  filters?: MaybeRefOrGetter<LearningUnitFilters | undefined>,
  options?: CurriculumQueryOptions,
) {
  const query = useQuery<LearningUnit[]>({
    queryKey: computed(() => {
      const id = toValue(structureId);
      const f = toValue(filters);
      return f !== undefined
        ? ['academics', 'units', id, f]
        : ['academics', 'units', id];
    }),
    queryFn: () => getLearningUnits(toValue(structureId)!, toValue(filters)),
    enabled: computed(() => {
      const id = toValue(structureId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(id) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });

  const learningUnitTree = computed<LearningUnitTreeNode[]>(() => {
    return buildLearningUnitTree(query.data.value ?? []);
  });

  return {
    ...query,
    learningUnitTree,
  };
}

/**
 * Reactive TanStack Query hook fetching academic requirements (TD-05, P5-S15).
 * Cache key: `['academics', 'requirements', structureId, filters]`.
 */
export function useAcademicRequirements(
  structureId: MaybeRefOrGetter<string | null | undefined>,
  filtersOrScope?: MaybeRefOrGetter<
    AcademicRequirementFilters | AcademicRequirementScope | string | null | undefined
  >,
  type?: MaybeRefOrGetter<AcademicRequirementType | string | null | undefined>,
  options?: CurriculumQueryOptions,
) {
  const resolvedFilters = computed<AcademicRequirementFilters | undefined>(() => {
    const first = toValue(filtersOrScope);
    if (typeof first === 'object' && first !== null) {
      return first;
    }
    const t = toValue(type);
    if (typeof first === 'string' || typeof t === 'string') {
      return {
        scope: (first as AcademicRequirementScope) || undefined,
        type: (t as AcademicRequirementType) || undefined,
      };
    }
    return undefined;
  });

  return useQuery<AcademicRequirement[]>({
    queryKey: computed(() => {
      const id = toValue(structureId);
      const f = resolvedFilters.value;
      return f !== undefined
        ? ['academics', 'requirements', id, f]
        : ['academics', 'requirements', id];
    }),
    queryFn: () => getAcademicRequirements(toValue(structureId)!, resolvedFilters.value),
    enabled: computed(() => {
      const id = toValue(structureId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(id) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

// Aliases for query hooks matching query naming conventions
export const useLearningStructuresQuery = useLearningStructures;
export const useLearningStructureByIdQuery = useLearningStructureById;
export const useLearningUnitsQuery = useLearningUnits;
export const useAcademicRequirementsQuery = useAcademicRequirements;

// ============================================================================
// Mutation Variable Interfaces & Helpers
// ============================================================================

export type UpdateStructureVariables =
  | { id: string; request: UpdateLearningStructureRequest }
  | { id: string; data: UpdateLearningStructureRequest }
  | ({ id: string } & UpdateLearningStructureRequest);

function resolveUpdateStructure(variables: UpdateStructureVariables): {
  id: string;
  request: UpdateLearningStructureRequest;
} {
  const { id } = variables;
  if ('request' in variables && variables.request) {
    return { id, request: variables.request };
  }
  if ('data' in variables && variables.data) {
    return { id, request: variables.data };
  }
  const { id: _ignored, ...rest } = variables as { id: string } & UpdateLearningStructureRequest;
  return { id, request: rest };
}

export type SupersedeStructureVariables =
  | { id: string; request: SupersedeStructureRequest }
  | { id: string; data: SupersedeStructureRequest }
  | ({ id: string } & SupersedeStructureRequest);

function resolveSupersedeStructure(variables: SupersedeStructureVariables): {
  id: string;
  request: SupersedeStructureRequest;
} {
  const { id } = variables;
  if ('request' in variables && variables.request) {
    return { id, request: variables.request };
  }
  if ('data' in variables && variables.data) {
    return { id, request: variables.data };
  }
  const { id: _ignored, ...rest } = variables as { id: string } & SupersedeStructureRequest;
  return { id, request: rest };
}

export type CreateUnitVariables =
  | { structureId: string; request: CreateLearningUnitRequest }
  | { structureId: string; data: CreateLearningUnitRequest }
  | ({ structureId: string } & CreateLearningUnitRequest);

function resolveCreateUnit(variables: CreateUnitVariables): {
  structureId: string;
  request: CreateLearningUnitRequest;
} {
  const { structureId } = variables;
  if ('request' in variables && variables.request) {
    return { structureId, request: variables.request };
  }
  if ('data' in variables && variables.data) {
    return { structureId, request: variables.data };
  }
  const { structureId: _ignored, ...rest } = variables as {
    structureId: string;
  } & CreateLearningUnitRequest;
  return { structureId, request: rest };
}

export type UpdateUnitVariables =
  | { id: string; structureId?: string | undefined; request: UpdateLearningUnitRequest }
  | { id: string; structureId?: string | undefined; data: UpdateLearningUnitRequest }
  | ({ id: string; structureId?: string | undefined } & UpdateLearningUnitRequest);

function resolveUpdateUnit(variables: UpdateUnitVariables): {
  id: string;
  structureId?: string | undefined;
  request: UpdateLearningUnitRequest;
} {
  const { id, structureId } = variables;
  if ('request' in variables && variables.request) {
    return { id, structureId, request: variables.request };
  }
  if ('data' in variables && variables.data) {
    return { id, structureId, request: variables.data };
  }
  const {
    id: _ignoredId,
    structureId: _ignoredSid,
    ...rest
  } = variables as { id: string; structureId?: string | undefined } & UpdateLearningUnitRequest;
  return { id, structureId, request: rest };
}

export type DeleteUnitVariables = string | { id: string; structureId?: string | undefined };

function resolveDeleteUnit(variables: DeleteUnitVariables): {
  id: string;
  structureId?: string | undefined;
} {
  return typeof variables === 'string'
    ? { id: variables }
    : { id: variables.id, structureId: variables.structureId };
}

export type CreateRequirementVariables =
  | { structureId: string; request: CreateAcademicRequirementRequest }
  | { structureId: string; data: CreateAcademicRequirementRequest }
  | ({ structureId: string } & CreateAcademicRequirementRequest);

function resolveCreateRequirement(variables: CreateRequirementVariables): {
  structureId: string;
  request: CreateAcademicRequirementRequest;
} {
  const { structureId } = variables;
  if ('request' in variables && variables.request) {
    return { structureId, request: variables.request };
  }
  if ('data' in variables && variables.data) {
    return { structureId, request: variables.data };
  }
  const { structureId: _ignored, ...rest } = variables as {
    structureId: string;
  } & CreateAcademicRequirementRequest;
  return { structureId, request: rest };
}

export type UpdateRequirementVariables =
  | { id: string; structureId?: string | undefined; request: UpdateAcademicRequirementRequest }
  | { id: string; structureId?: string | undefined; data: UpdateAcademicRequirementRequest }
  | ({ id: string; structureId?: string | undefined } & UpdateAcademicRequirementRequest);

function resolveUpdateRequirement(variables: UpdateRequirementVariables): {
  id: string;
  structureId?: string | undefined;
  request: UpdateAcademicRequirementRequest;
} {
  const { id, structureId } = variables;
  if ('request' in variables && variables.request) {
    return { id, structureId, request: variables.request };
  }
  if ('data' in variables && variables.data) {
    return { id, structureId, request: variables.data };
  }
  const {
    id: _ignoredId,
    structureId: _ignoredSid,
    ...rest
  } = variables as { id: string; structureId?: string | undefined } & UpdateAcademicRequirementRequest;
  return { id, structureId, request: rest };
}

export type DeleteRequirementVariables = string | { id: string; structureId?: string | undefined };

function resolveDeleteRequirement(variables: DeleteRequirementVariables): {
  id: string;
  structureId?: string | undefined;
} {
  return typeof variables === 'string'
    ? { id: variables }
    : { id: variables.id, structureId: variables.structureId };
}

function resolveStructureId(variables: string | { id: string }): string {
  return typeof variables === 'string' ? variables : variables.id;
}

// ============================================================================
// Learning Structure Framework Mutation Hooks
// ============================================================================

/**
 * Mutation hook creating a draft learning structure framework (SCR-ACD-03, P5-S15).
 * Invalidation pipeline automatically invalidates `['academics', 'structures']`.
 */
export function useCreateStructure(
  options?: CurriculumMutationOptions<LearningStructure, CreateLearningStructureRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: CreateLearningStructureRequest) =>
      createLearningStructure(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      notifySuccess(
        toast,
        'Curriculum Framework Created',
        `Framework '${data.name}' (v${data.version}) drafted successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Framework Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook updating metadata of a learning structure framework (SCR-ACD-03, P5-S15).
 */
export function useUpdateStructure(
  options?: CurriculumMutationOptions<LearningStructure, UpdateStructureVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: UpdateStructureVariables) => {
      const { id, request } = resolveUpdateStructure(variables);
      return updateLearningStructure(id, request);
    },
    onSuccess: (data, variables) => {
      const { id } = resolveUpdateStructure(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.detail(id) });
      notifySuccess(
        toast,
        'Curriculum Framework Updated',
        `Framework '${data.name}' metadata updated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Framework Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook submitting a draft framework for formal authority review (Criterion 1, P5-S15).
 */
export function useSubmitStructure(
  options?: CurriculumMutationOptions<
    SubmitLearningStructureResponse,
    string | { id: string }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = resolveStructureId(variables);
      return submitLearningStructure(id);
    },
    onSuccess: (data, variables) => {
      const id = resolveStructureId(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.detail(id) });
      notifySuccess(
        toast,
        'Curriculum Framework Submitted',
        'Curriculum framework submitted for formal review.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Framework Submission Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook formally approving and activating a learning structure framework (Criterion 1, P5-S15).
 */
export function useApproveStructure(
  options?: CurriculumMutationOptions<
    ApproveLearningStructureResponse,
    string | { id: string }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = resolveStructureId(variables);
      return approveLearningStructure(id);
    },
    onSuccess: (data, variables) => {
      const id = resolveStructureId(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.detail(id) });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.units.all });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.requirements.all });
      notifySuccess(
        toast,
        'Curriculum Framework Approved',
        'Curriculum framework formally approved and activated.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Framework Approval Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook branching an active structure into a new draft via deep clone (TD-03, P5-S15).
 */
export function useSupersedeStructure(
  options?: CurriculumMutationOptions<LearningStructure, SupersedeStructureVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: SupersedeStructureVariables) => {
      const { id, request } = resolveSupersedeStructure(variables);
      return supersedeLearningStructure(id, request);
    },
    onSuccess: (data, variables) => {
      const { id } = resolveSupersedeStructure(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.detail(id) });
      notifySuccess(
        toast,
        'Curriculum Framework Superseded',
        `New draft framework '${data.name}' (v${data.version}) created via deep clone.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Framework Supersede Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook archiving an inactive or superseded learning structure (TD-06, P5-S15).
 */
export function useArchiveStructure(
  options?: CurriculumMutationOptions<
    ArchiveLearningStructureResponse,
    string | { id: string }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = resolveStructureId(variables);
      return archiveLearningStructure(id);
    },
    onSuccess: (data, variables) => {
      const id = resolveStructureId(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.detail(id) });
      notifySuccess(
        toast,
        'Curriculum Framework Archived',
        'Curriculum framework archived successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Framework Archive Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Learning Units Mutation Hooks
// ============================================================================

/**
 * Mutation hook creating a learning unit within a framework (TD-04, P5-S15).
 */
export function useCreateUnit(
  options?: CurriculumMutationOptions<LearningUnit, CreateUnitVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: CreateUnitVariables) => {
      const { structureId, request } = resolveCreateUnit(variables);
      return createLearningUnit(structureId, request);
    },
    onSuccess: (data, variables) => {
      const { structureId } = resolveCreateUnit(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.units.list(structureId) });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({
        queryKey: curriculumKeys.structures.detail(structureId),
      });
      notifySuccess(
        toast,
        'Learning Unit Created',
        `Learning unit '${data.code} - ${data.name}' added successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Unit Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook updating an existing learning unit (TD-04, P5-S15).
 */
export function useUpdateUnit(
  options?: CurriculumMutationOptions<LearningUnit, UpdateUnitVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: UpdateUnitVariables) => {
      const { id, request } = resolveUpdateUnit(variables);
      return updateLearningUnit(id, request);
    },
    onSuccess: (data, variables) => {
      const { structureId } = resolveUpdateUnit(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.units.all });
      const targetStructureId = structureId || data.learningStructureId;
      if (targetStructureId) {
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.units.list(targetStructureId),
        });
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.structures.detail(targetStructureId),
        });
      }
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      notifySuccess(
        toast,
        'Learning Unit Updated',
        `Learning unit '${data.code} - ${data.name}' updated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Unit Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook removing a learning unit from a draft structure (TD-04, P5-S15).
 */
export function useDeleteUnit(
  options?: CurriculumMutationOptions<void, DeleteUnitVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: DeleteUnitVariables) => {
      const { id } = resolveDeleteUnit(variables);
      return deleteLearningUnit(id);
    },
    onSuccess: (_, variables) => {
      const { structureId } = resolveDeleteUnit(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.units.all });
      if (structureId) {
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.units.list(structureId),
        });
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.structures.detail(structureId),
        });
      }
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      notifySuccess(
        toast,
        'Learning Unit Deleted',
        'Learning unit removed successfully.',
        options,
      );
      options?.onSuccess?.(_, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Unit Deletion Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Academic Requirements Mutation Hooks
// ============================================================================

/**
 * Mutation hook creating an academic requirement policy rule (TD-05, P5-S15).
 */
export function useCreateRequirement(
  options?: CurriculumMutationOptions<
    AcademicRequirement,
    CreateRequirementVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: CreateRequirementVariables) => {
      const { structureId, request } = resolveCreateRequirement(variables);
      return createAcademicRequirement(structureId, request);
    },
    onSuccess: (data, variables) => {
      const { structureId } = resolveCreateRequirement(variables);
      queryClient.invalidateQueries({
        queryKey: curriculumKeys.requirements.list(structureId),
      });
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      queryClient.invalidateQueries({
        queryKey: curriculumKeys.structures.detail(structureId),
      });
      notifySuccess(
        toast,
        'Academic Requirement Created',
        `Requirement '${data.name}' created successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Requirement Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook updating an academic requirement rule (TD-05, P5-S15).
 */
export function useUpdateRequirement(
  options?: CurriculumMutationOptions<
    AcademicRequirement,
    UpdateRequirementVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: UpdateRequirementVariables) => {
      const { id, request } = resolveUpdateRequirement(variables);
      return updateAcademicRequirement(id, request);
    },
    onSuccess: (data, variables) => {
      const { structureId } = resolveUpdateRequirement(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.requirements.all });
      const targetStructureId = structureId || data.learningStructureId;
      if (targetStructureId) {
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.requirements.list(targetStructureId),
        });
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.structures.detail(targetStructureId),
        });
      }
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      notifySuccess(
        toast,
        'Academic Requirement Updated',
        `Requirement '${data.name}' updated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Requirement Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook removing an academic requirement from a draft framework (TD-05, P5-S15).
 */
export function useDeleteRequirement(
  options?: CurriculumMutationOptions<void, DeleteRequirementVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: DeleteRequirementVariables) => {
      const { id } = resolveDeleteRequirement(variables);
      return deleteAcademicRequirement(id);
    },
    onSuccess: (_, variables) => {
      const { structureId } = resolveDeleteRequirement(variables);
      queryClient.invalidateQueries({ queryKey: curriculumKeys.requirements.all });
      if (structureId) {
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.requirements.list(structureId),
        });
        queryClient.invalidateQueries({
          queryKey: curriculumKeys.structures.detail(structureId),
        });
      }
      queryClient.invalidateQueries({ queryKey: curriculumKeys.structures.all });
      notifySuccess(
        toast,
        'Academic Requirement Deleted',
        'Academic requirement removed successfully.',
        options,
      );
      options?.onSuccess?.(_, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Requirement Deletion Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// Aliases for mutation hooks matching mutation naming conventions
export const useCreateStructureMutation = useCreateStructure;
export const useUpdateStructureMutation = useUpdateStructure;
export const useSubmitStructureMutation = useSubmitStructure;
export const useApproveStructureMutation = useApproveStructure;
export const useSupersedeStructureMutation = useSupersedeStructure;
export const useArchiveStructureMutation = useArchiveStructure;
export const useCreateUnitMutation = useCreateUnit;
export const useUpdateUnitMutation = useUpdateUnit;
export const useDeleteUnitMutation = useDeleteUnit;
export const useCreateRequirementMutation = useCreateRequirement;
export const useUpdateRequirementMutation = useUpdateRequirement;
export const useDeleteRequirementMutation = useDeleteRequirement;
