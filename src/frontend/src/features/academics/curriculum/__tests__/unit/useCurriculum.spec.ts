// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as curriculumApi from '../../api/curriculumApi';
import {
  buildLearningUnitTree,
  useLearningStructures,
  useLearningStructureById,
  useLearningUnits,
  useAcademicRequirements,
  useCreateStructure,
  useUpdateStructure,
  useSubmitStructure,
  useApproveStructure,
  useSupersedeStructure,
  useArchiveStructure,
  useCreateUnit,
  useUpdateUnit,
  useDeleteUnit,
  useCreateRequirement,
  useUpdateRequirement,
  useDeleteRequirement,
} from '../../composables/useCurriculum';
import type {
  AcademicRequirement,
  ApproveLearningStructureResponse,
  ArchiveLearningStructureResponse,
  LearningStructure,
  LearningStructureSummary,
  LearningUnit,
  SubmitLearningStructureResponse,
} from '../../types/curriculum.types';
import type { PaginatedResponse } from '@/shared/types/common';

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

describe('Curriculum Composables & Tree Assembly Unit Tests (P7-S21)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ==========================================================================
  // 1. Tree Assembling Logic Tests (TD-04, ADR-005)
  // ==========================================================================
  describe('Tree Assembling Logic (buildLearningUnitTree)', () => {
    it('returns empty array when units collection is null, undefined, or empty', () => {
      expect(buildLearningUnitTree([])).toEqual([]);
      // @ts-expect-error verifying null safety
      expect(buildLearningUnitTree(null)).toEqual([]);
      // @ts-expect-error verifying undefined safety
      expect(buildLearningUnitTree(undefined)).toEqual([]);
    });

    it('assembles a flat list of root units into tree nodes with empty children', () => {
      const units: LearningUnit[] = [
        {
          id: 'u-1',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'MATH-100',
          name: 'General Mathematics',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-2',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'ENG-100',
          name: 'General English',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];

      const tree = buildLearningUnitTree(units);

      expect(tree).toHaveLength(2);
      expect(tree[0].id).toBe('u-1');
      expect(tree[0].key).toBe('u-1');
      expect(tree[0].data?.code).toBe('MATH-100');
      expect(tree[0].children).toEqual([]);

      expect(tree[1].id).toBe('u-2');
      expect(tree[1].key).toBe('u-2');
      expect(tree[1].data?.code).toBe('ENG-100');
      expect(tree[1].children).toEqual([]);
    });

    it('correctly nests child units under their respective parent unit', () => {
      const units: LearningUnit[] = [
        {
          id: 'u-root-1',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'MATH-100',
          name: 'Mathematics',
          unitType: 'subject',
          status: 'active',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-child-1',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-root-1',
          code: 'MATH-101',
          name: 'Algebra Sub-Unit',
          unitType: 'module',
          status: 'active',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-child-2',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-root-1',
          code: 'MATH-102',
          name: 'Geometry Sub-Unit',
          unitType: 'module',
          status: 'active',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-root-2',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'SCI-100',
          name: 'Science',
          unitType: 'course',
          status: 'active',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];

      const tree = buildLearningUnitTree(units);

      expect(tree).toHaveLength(2);

      // Root unit 1 contains 2 nested children
      const root1 = tree.find((node) => node.id === 'u-root-1');
      expect(root1).toBeDefined();
      expect(root1?.children).toHaveLength(2);
      expect(root1?.children?.[0].id).toBe('u-child-1');
      expect(root1?.children?.[0].key).toBe('u-child-1');
      expect(root1?.children?.[0].children).toEqual([]);
      expect(root1?.children?.[1].id).toBe('u-child-2');
      expect(root1?.children?.[1].key).toBe('u-child-2');
      expect(root1?.children?.[1].children).toEqual([]);

      // Root unit 2 contains 0 children
      const root2 = tree.find((node) => node.id === 'u-root-2');
      expect(root2).toBeDefined();
      expect(root2?.children).toHaveLength(0);
    });

    it('safely retains orphaned child units at the root level', () => {
      const units: LearningUnit[] = [
        {
          id: 'u-orphan-1',
          learningStructureId: 'ls-1',
          parentUnitId: 'missing-parent-id',
          code: 'ORPHAN-01',
          name: 'Orphaned Module',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];

      const tree = buildLearningUnitTree(units);

      expect(tree).toHaveLength(1);
      expect(tree[0].id).toBe('u-orphan-1');
      expect(tree[0].key).toBe('u-orphan-1');
      expect(tree[0].children).toEqual([]);
    });

    it('enforces 2-level depth cap preventing sub-children from being nested deeper than depth 1', () => {
      const units: LearningUnit[] = [
        {
          id: 'u-grandparent',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'GP-100',
          name: 'Grandparent Unit',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-parent',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-grandparent',
          code: 'P-101',
          name: 'Parent Unit',
          unitType: 'course',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-grandchild',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-parent',
          code: 'GC-102',
          name: 'Grandchild Unit (Invalid Depth)',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];

      const tree = buildLearningUnitTree(units);

      // Grandparent has child u-parent
      expect(tree).toHaveLength(1);
      expect(tree[0].id).toBe('u-grandparent');
      expect(tree[0].children).toHaveLength(1);
      expect(tree[0].children?.[0].id).toBe('u-parent');
      // In the 2-level hierarchy, child units at level 1 always have empty children arrays
      expect(tree[0].children?.[0].children).toEqual([]);
    });
  });

  // ==========================================================================
  // 2. Query Hooks & Cache Keys Verification
  // ==========================================================================
  describe('Query Hooks & Cache Keys', () => {
    it('useLearningStructures generates correct cache key and executes query', async () => {
      const mockResult: PaginatedResponse<LearningStructureSummary> = {
        items: [
          {
            id: 'ls-1',
            name: 'Cambridge 2026',
            academicPeriodId: 'per-1',
            version: '1.0',
            effectiveDate: '2026-07-01',
            status: 'draft',
            unitCount: 3,
            requirementCount: 1,
            hasPredecessor: false,
            createdAt: '2026-01-01',
            createdBy: 'admin',
          },
        ],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue(mockResult);

      const filters = ref({ academicPeriodId: 'per-1', status: 'draft' as const });
      const { result, queryClient } = withSetup(() => useLearningStructures(filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockResult);

      // Verify cached entry under cache key: ['academics', 'structures', { academicPeriodId: 'per-1', status: 'draft' }]
      const cached = queryClient.getQueryData(['academics', 'structures', filters.value]);
      expect(cached).toEqual(mockResult);
    });

    it('useLearningStructureById creates disabled query when id is null and caches on valid id', async () => {
      const mockDetail: LearningStructure = {
        id: 'ls-1',
        name: 'Curriculum Detail',
        academicPeriodId: 'per-1',
        version: '1.0',
        effectiveDate: '2026-07-01',
        status: 'draft',
        createdAt: '2026-01-01',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'getLearningStructureById').mockResolvedValue(mockDetail);

      const idRef = ref<string | null>(null);
      const { result, queryClient } = withSetup(() => useLearningStructureById(idRef));

      // Query should be inactive when id is null
      expect(result.isFetching.value).toBe(false);
      expect(curriculumApi.getLearningStructureById).not.toHaveBeenCalled();

      // Trigger query by providing valid id
      idRef.value = 'ls-1';
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getLearningStructureById).toHaveBeenCalledWith('ls-1');
      expect(queryClient.getQueryData(['academics', 'structure', 'ls-1'])).toEqual(mockDetail);
    });

    it('useLearningUnits caches units under structure key and provides reactive computed tree', async () => {
      const mockUnits: LearningUnit[] = [
        {
          id: 'u-1',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'BIO-101',
          name: 'Biology',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-2',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-1',
          code: 'BIO-101-LAB',
          name: 'Biology Practical Lab',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];
      vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);

      const structureIdRef = ref('ls-1');
      const { result, queryClient } = withSetup(() => useLearningUnits(structureIdRef));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getLearningUnits).toHaveBeenCalledWith('ls-1', undefined);
      expect(queryClient.getQueryData(['academics', 'units', 'ls-1'])).toEqual(mockUnits);

      // Verify computed learningUnitTree
      expect(result.learningUnitTree.value).toHaveLength(1);
      expect(result.learningUnitTree.value[0].id).toBe('u-1');
      expect(result.learningUnitTree.value[0].children).toHaveLength(1);
      expect(result.learningUnitTree.value[0].children?.[0].id).toBe('u-2');
    });

    it('useAcademicRequirements queries with structureId and filter cache key', async () => {
      const mockReqs: AcademicRequirement[] = [
        {
          id: 'req-1',
          learningStructureId: 'ls-1',
          name: 'Minimum Passing Grade',
          requirementScope: 'program',
          requirementType: 'completion',
          description: 'Must achieve minimum grade C',
          version: '1.0',
          effectiveDate: '2026-07-01',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];
      vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue(mockReqs);

      const structureIdRef = ref('ls-1');
      const filters = ref({ scope: 'program' as const, type: 'completion' as const });
      const { result, queryClient } = withSetup(() =>
        useAcademicRequirements(structureIdRef, filters),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getAcademicRequirements).toHaveBeenCalledWith('ls-1', filters.value);
      expect(
        queryClient.getQueryData(['academics', 'requirements', 'ls-1', filters.value]),
      ).toEqual(mockReqs);
    });
  });

  // ==========================================================================
  // 3. Mutation Hooks & Cache Invalidation Verification
  // ==========================================================================
  describe('Mutation Hooks & Cache Invalidation', () => {
    describe('Structure Mutations', () => {
      it('useCreateStructure invalidates ["academics", "structures"] on success', async () => {
        const mockCreated: LearningStructure = {
          id: 'ls-created',
          name: 'New Structure',
          academicPeriodId: 'per-1',
          version: '1.0',
          effectiveDate: '2026-07-01',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'createLearningStructure').mockResolvedValue(mockCreated);

        const { result, queryClient } = withSetup(() => useCreateStructure());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          name: 'New Structure',
          academicPeriodId: 'per-1',
          version: '1.0',
          effectiveDate: '2026-07-01',
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useUpdateStructure invalidates ["academics", "structures"] and specific structure detail', async () => {
        const mockUpdated: LearningStructure = {
          id: 'ls-1',
          name: 'Updated Name',
          academicPeriodId: 'per-1',
          version: '1.0',
          effectiveDate: '2026-07-01',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'updateLearningStructure').mockResolvedValue(mockUpdated);

        const { result, queryClient } = withSetup(() => useUpdateStructure());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          id: 'ls-1',
          request: { name: 'Updated Name' },
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structure', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useSubmitStructure invalidates structure cache on transition to submitted', async () => {
        const mockResponse: SubmitLearningStructureResponse = {
          id: 'ls-1',
          status: 'submitted',
          updatedAt: '2026-01-01',
          updatedBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'submitLearningStructure').mockResolvedValue(mockResponse);

        const { result, queryClient } = withSetup(() => useSubmitStructure());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate('ls-1');

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structure', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useApproveStructure invalidates structures, specific structure, units, and requirements', async () => {
        const mockResponse: ApproveLearningStructureResponse = {
          id: 'ls-1',
          status: 'active',
          approvedBy: 'authority',
          approvedDate: '2026-01-01',
        };
        vi.spyOn(curriculumApi, 'approveLearningStructure').mockResolvedValue(mockResponse);

        const { result, queryClient } = withSetup(() => useApproveStructure());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate('ls-1');

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structure', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'units'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'requirements'],
        });
      });

      it('useSupersedeStructure invalidates structures cache on deep clone', async () => {
        const mockCloned: LearningStructure = {
          id: 'ls-cloned',
          name: 'Cloned Structure',
          academicPeriodId: 'per-2',
          version: '2025.1',
          effectiveDate: '2025-07-01',
          status: 'draft',
          supersedesId: 'ls-1',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'supersedeLearningStructure').mockResolvedValue(mockCloned);

        const { result, queryClient } = withSetup(() => useSupersedeStructure());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          id: 'ls-1',
          request: { newVersion: '2025.1', effectiveDate: '2025-07-01' },
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useArchiveStructure invalidates structure and structures caches', async () => {
        const mockResponse: ArchiveLearningStructureResponse = {
          id: 'ls-1',
          status: 'archived',
          archivedBy: 'authority',
          archivedDate: '2026-01-01',
        };
        vi.spyOn(curriculumApi, 'archiveLearningStructure').mockResolvedValue(mockResponse);

        const { result, queryClient } = withSetup(() => useArchiveStructure());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate('ls-1');

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structure', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });
    });

    describe('Learning Unit Mutations', () => {
      it('useCreateUnit invalidates units and structures cache', async () => {
        const mockUnit: LearningUnit = {
          id: 'u-new',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'ART-100',
          name: 'Art & Design',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'createLearningUnit').mockResolvedValue(mockUnit);

        const { result, queryClient } = withSetup(() => useCreateUnit());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          structureId: 'ls-1',
          request: { code: 'ART-100', name: 'Art & Design', unitType: 'subject' },
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'units', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useUpdateUnit invalidates units and structures cache', async () => {
        const mockUnit: LearningUnit = {
          id: 'u-1',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'ART-100-REV',
          name: 'Fine Arts',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'updateLearningUnit').mockResolvedValue(mockUnit);

        const { result, queryClient } = withSetup(() => useUpdateUnit());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          id: 'u-1',
          structureId: 'ls-1',
          request: { code: 'ART-100-REV', name: 'Fine Arts', unitType: 'subject' },
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'units', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useDeleteUnit invalidates units and structures cache', async () => {
        vi.spyOn(curriculumApi, 'deleteLearningUnit').mockResolvedValue(undefined);

        const { result, queryClient } = withSetup(() => useDeleteUnit());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          id: 'u-1',
          structureId: 'ls-1',
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'units', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });
    });

    describe('Academic Requirement Mutations', () => {
      it('useCreateRequirement invalidates requirements and structures cache', async () => {
        const mockReq: AcademicRequirement = {
          id: 'req-new',
          learningStructureId: 'ls-1',
          name: 'Community Service',
          requirementScope: 'program',
          requirementType: 'completion',
          description: '30 hours community service',
          version: '1.0',
          effectiveDate: '2026-07-01',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'createAcademicRequirement').mockResolvedValue(mockReq);

        const { result, queryClient } = withSetup(() => useCreateRequirement());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          structureId: 'ls-1',
          request: {
            name: 'Community Service',
            requirementScope: 'program',
            requirementType: 'completion',
            description: '30 hours community service',
            version: '1.0',
            effectiveDate: '2026-07-01',
          },
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'requirements', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useUpdateRequirement invalidates requirements and structures cache', async () => {
        const mockReq: AcademicRequirement = {
          id: 'req-1',
          learningStructureId: 'ls-1',
          name: 'Updated Req',
          requirementScope: 'program',
          requirementType: 'completion',
          description: 'Updated requirement description',
          version: '1.0',
          effectiveDate: '2026-07-01',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        };
        vi.spyOn(curriculumApi, 'updateAcademicRequirement').mockResolvedValue(mockReq);

        const { result, queryClient } = withSetup(() => useUpdateRequirement());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          id: 'req-1',
          structureId: 'ls-1',
          request: {
            name: 'Updated Req',
            description: 'Updated requirement description',
          },
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'requirements', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });

      it('useDeleteRequirement invalidates requirements and structures cache', async () => {
        vi.spyOn(curriculumApi, 'deleteAcademicRequirement').mockResolvedValue(undefined);

        const { result, queryClient } = withSetup(() => useDeleteRequirement());
        const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

        result.mutate({
          id: 'req-1',
          structureId: 'ls-1',
        });

        await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'requirements', 'ls-1'],
        });
        expect(invalidateSpy).toHaveBeenCalledWith({
          queryKey: ['academics', 'structures'],
        });
      });
    });
  });
});
