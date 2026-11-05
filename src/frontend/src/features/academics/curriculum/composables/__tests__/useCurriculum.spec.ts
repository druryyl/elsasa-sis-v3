// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as curriculumApi from '../../api/curriculumApi';
import {
  buildLearningUnitTree,
  useAcademicRequirements,
  useApproveStructure,
  useArchiveStructure,
  useCreateRequirement,
  useCreateStructure,
  useCreateUnit,
  useDeleteRequirement,
  useDeleteUnit,
  useLearningStructureById,
  useLearningStructures,
  useLearningUnits,
  useSubmitStructure,
  useSupersedeStructure,
  useUpdateRequirement,
  useUpdateStructure,
  useUpdateUnit,
} from '../useCurriculum';
import type {
  AcademicRequirement,
  ApproveLearningStructureResponse,
  ArchiveLearningStructureResponse,
  LearningStructure,
  LearningStructureSummary,
  LearningUnit,
  SubmitLearningStructureResponse,
} from '../../../curriculum';
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

describe('useCurriculum Composables', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ==========================================================================
  // In-Memory 2-Level Tree Assembly Tests (TD-04, ADR-005)
  // ==========================================================================
  describe('buildLearningUnitTree', () => {
    it('returns empty array when units collection is empty or null', () => {
      expect(buildLearningUnitTree([])).toEqual([]);
      // @ts-expect-error testing null safety
      expect(buildLearningUnitTree(null)).toEqual([]);
    });

    it('assembles root units and child units into 2-level hierarchy with children', () => {
      const units: LearningUnit[] = [
        {
          id: 'u-root-1',
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
          id: 'u-child-1',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-root-1',
          code: 'MATH-101',
          name: 'Algebra I Module',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-child-2',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-root-1',
          code: 'MATH-102',
          name: 'Geometry Module',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-root-2',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'ENG-100',
          name: 'English Literature',
          unitType: 'course',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];

      const tree = buildLearningUnitTree(units);

      expect(tree).toHaveLength(2);
      expect(tree[0].id).toBe('u-root-1');
      expect(tree[0].key).toBe('u-root-1');
      expect(tree[0].data?.code).toBe('MATH-100');
      expect(tree[0].children).toHaveLength(2);
      expect(tree[0].children![0].id).toBe('u-child-1');
      expect(tree[0].children![0].key).toBe('u-child-1');
      expect(tree[0].children![0].children).toEqual([]);
      expect(tree[0].children![1].id).toBe('u-child-2');

      expect(tree[1].id).toBe('u-root-2');
      expect(tree[1].children).toEqual([]);
    });

    it('retains orphaned child units at top level to prevent data loss', () => {
      const units: LearningUnit[] = [
        {
          id: 'u-orphan-1',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-nonexistent',
          code: 'ORPHAN-01',
          name: 'Orphan Unit',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];

      const tree = buildLearningUnitTree(units);
      expect(tree).toHaveLength(1);
      expect(tree[0].id).toBe('u-orphan-1');
      expect(tree[0].children).toEqual([]);
    });
  });

  // ==========================================================================
  // Query Hooks Tests
  // ==========================================================================
  describe('useLearningStructures', () => {
    it('queries paginated learning structures with reactive filters and cache key', async () => {
      const mockResult: PaginatedResponse<LearningStructureSummary> = {
        items: [
          {
            id: 'ls-1',
            name: 'Cambridge Secondary 2026/2027',
            academicPeriodId: 'per-1',
            academicPeriodLabel: '2026/2027 Odd Semester',
            version: '1.0.0',
            effectiveDate: '2026-07-01',
            status: 'draft',
            unitCount: 5,
            requirementCount: 2,
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
      const { result } = withSetup(() => useLearningStructures(filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getLearningStructures).toHaveBeenCalledWith({
        academicPeriodId: 'per-1',
        status: 'draft',
      });
      expect(result.data.value).toEqual(mockResult);
    });

    it('supports individual positional filter arguments', async () => {
      vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue({
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      });

      const { result } = withSetup(() =>
        useLearningStructures('per-1', 'active', 'Math', 1, 10),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getLearningStructures).toHaveBeenCalledWith({
        academicPeriodId: 'per-1',
        status: 'active',
        search: 'Math',
        page: 1,
        pageSize: 10,
      });
    });
  });

  describe('useLearningStructureById', () => {
    it('does not execute when structureId is null or undefined', () => {
      vi.spyOn(curriculumApi, 'getLearningStructureById').mockResolvedValue(
        {} as LearningStructure,
      );
      const id = ref<string | null>(null);
      const { result } = withSetup(() => useLearningStructureById(id));

      expect(result.isFetching.value).toBe(false);
      expect(curriculumApi.getLearningStructureById).not.toHaveBeenCalled();
    });

    it('fetches complete structure details when structureId is provided', async () => {
      const mockDetail: LearningStructure = {
        id: 'ls-10',
        name: 'IB Diploma Framework',
        academicPeriodId: 'per-1',
        academicPeriodLabel: '2026/2027',
        version: '1.0.0',
        effectiveDate: '2026-07-01',
        status: 'active',
        createdAt: '2026-01-01',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'getLearningStructureById').mockResolvedValue(mockDetail);

      const id = ref('ls-10');
      const { result } = withSetup(() => useLearningStructureById(id));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getLearningStructureById).toHaveBeenCalledWith('ls-10');
      expect(result.data.value).toEqual(mockDetail);
    });
  });

  describe('useLearningUnits', () => {
    it('fetches units and provides reactive computed learningUnitTree', async () => {
      const mockUnits: LearningUnit[] = [
        {
          id: 'u-1',
          learningStructureId: 'ls-1',
          parentUnitId: null,
          code: 'SCI-100',
          name: 'General Science',
          unitType: 'subject',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
        {
          id: 'u-2',
          learningStructureId: 'ls-1',
          parentUnitId: 'u-1',
          code: 'BIO-101',
          name: 'Biology Sub-module',
          unitType: 'module',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];
      vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);

      const id = ref('ls-1');
      const { result } = withSetup(() => useLearningUnits(id));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getLearningUnits).toHaveBeenCalledWith('ls-1', undefined);
      expect(result.data.value).toEqual(mockUnits);

      // Verify computed learningUnitTree
      expect(result.learningUnitTree.value).toHaveLength(1);
      expect(result.learningUnitTree.value[0].id).toBe('u-1');
      expect(result.learningUnitTree.value[0].children).toHaveLength(1);
      expect(result.learningUnitTree.value[0].children![0].id).toBe('u-2');
    });
  });

  describe('useAcademicRequirements', () => {
    it('fetches requirements with structureId and filters', async () => {
      const mockReqs: AcademicRequirement[] = [
        {
          id: 'req-1',
          name: 'Minimum Passing Grade',
          requirementScope: 'program',
          learningStructureId: 'ls-1',
          requirementType: 'completion',
          description: 'Students must achieve minimum 75% score',
          version: '1.0.0',
          effectiveDate: '2026-07-01',
          status: 'draft',
          createdAt: '2026-01-01',
          createdBy: 'admin',
        },
      ];
      vi.spyOn(curriculumApi, 'getAcademicRequirements').mockResolvedValue(mockReqs);

      const id = ref('ls-1');
      const filters = ref({ scope: 'program' as const });
      const { result } = withSetup(() => useAcademicRequirements(id, filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.getAcademicRequirements).toHaveBeenCalledWith('ls-1', {
        scope: 'program',
      });
      expect(result.data.value).toEqual(mockReqs);
    });
  });

  // ==========================================================================
  // Learning Structure Framework Mutation Tests
  // ==========================================================================
  describe('Framework Mutations', () => {
    it('useCreateStructure creates framework, invalidates cache, and notifies success', async () => {
      const mockCreated: LearningStructure = {
        id: 'ls-new',
        name: 'National Curriculum 2026',
        academicPeriodId: 'per-1',
        academicPeriodLabel: '2026/2027',
        version: '1.0.0',
        effectiveDate: '2026-07-01',
        status: 'draft',
        createdAt: '2026-01-01',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'createLearningStructure').mockResolvedValue(mockCreated);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() => useCreateStructure({ onSuccess }));
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        name: 'National Curriculum 2026',
        academicPeriodId: 'per-1',
        version: '1.0.0',
        effectiveDate: '2026-07-01',
      };
      result.mutate(payload);

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.createLearningStructure).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'structures'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Curriculum Framework Created',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockCreated, payload);
    });

    it('useUpdateStructure updates framework and invalidates structure detail cache', async () => {
      const mockUpdated: LearningStructure = {
        id: 'ls-1',
        name: 'Updated Curriculum',
        academicPeriodId: 'per-1',
        academicPeriodLabel: '2026/2027',
        version: '1.0.0',
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
        request: { name: 'Updated Curriculum', description: 'Updated' },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.updateLearningStructure).toHaveBeenCalledWith('ls-1', {
        name: 'Updated Curriculum',
        description: 'Updated',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'structure', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Curriculum Framework Updated',
        }),
      );
    });

    it('useSubmitStructure submits framework and triggers toast', async () => {
      const mockResponse: SubmitLearningStructureResponse = {
        id: 'ls-1',
        status: 'submitted',
        updatedAt: '2026-01-02',
        updatedBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'submitLearningStructure').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useSubmitStructure());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate('ls-1');

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.submitLearningStructure).toHaveBeenCalledWith('ls-1');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'structure', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Curriculum Framework Submitted',
        }),
      );
    });

    it('useApproveStructure approves framework and invalidates units and requirements', async () => {
      const mockResponse: ApproveLearningStructureResponse = {
        id: 'ls-1',
        status: 'active',
        approvedBy: 'principal',
        approvedDate: '2026-01-02',
        updatedAt: '2026-01-02',
        updatedBy: 'principal',
      };
      vi.spyOn(curriculumApi, 'approveLearningStructure').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useApproveStructure());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({ id: 'ls-1' });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.approveLearningStructure).toHaveBeenCalledWith('ls-1');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'units'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'requirements'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Curriculum Framework Approved',
        }),
      );
    });

    it('useSupersedeStructure branches structure and invalidates cache', async () => {
      const mockBranch: LearningStructure = {
        id: 'ls-2',
        name: 'Cambridge Secondary',
        academicPeriodId: 'per-2',
        academicPeriodLabel: '2027/2028',
        version: '2.0.0',
        effectiveDate: '2027-07-01',
        status: 'draft',
        supersedesId: 'ls-1',
        createdAt: '2026-01-02',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'supersedeLearningStructure').mockResolvedValue(mockBranch);

      const { result, queryClient } = withSetup(() => useSupersedeStructure());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        id: 'ls-1',
        request: { newVersion: '2.0.0', effectiveDate: '2027-07-01' },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.supersedeLearningStructure).toHaveBeenCalledWith('ls-1', {
        newVersion: '2.0.0',
        effectiveDate: '2027-07-01',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'structure', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Curriculum Framework Superseded',
        }),
      );
    });

    it('useArchiveStructure archives framework and triggers toast', async () => {
      const mockResponse: ArchiveLearningStructureResponse = {
        id: 'ls-old',
        status: 'archived',
        updatedAt: '2026-01-02',
        updatedBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'archiveLearningStructure').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useArchiveStructure());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate('ls-old');

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.archiveLearningStructure).toHaveBeenCalledWith('ls-old');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'structure', 'ls-old'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Curriculum Framework Archived',
        }),
      );
    });
  });

  // ==========================================================================
  // Learning Units Mutation Tests
  // ==========================================================================
  describe('Unit Mutations', () => {
    it('useCreateUnit creates unit and invalidates units list', async () => {
      const mockCreated: LearningUnit = {
        id: 'u-new',
        learningStructureId: 'ls-1',
        code: 'PHY-101',
        name: 'Physics I',
        unitType: 'subject',
        status: 'draft',
        createdAt: '2026-01-01',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'createLearningUnit').mockResolvedValue(mockCreated);

      const { result, queryClient } = withSetup(() => useCreateUnit());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({
        structureId: 'ls-1',
        request: {
          code: 'PHY-101',
          name: 'Physics I',
          unitType: 'subject',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.createLearningUnit).toHaveBeenCalledWith('ls-1', {
        code: 'PHY-101',
        name: 'Physics I',
        unitType: 'subject',
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'units', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Created',
        }),
      );
    });

    it('useUpdateUnit updates unit and triggers toast', async () => {
      const mockUpdated: LearningUnit = {
        id: 'u-1',
        learningStructureId: 'ls-1',
        code: 'PHY-101-MOD',
        name: 'Physics I Advanced',
        unitType: 'subject',
        status: 'draft',
        createdAt: '2026-01-01',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'updateLearningUnit').mockResolvedValue(mockUpdated);

      const { result } = withSetup(() => useUpdateUnit());

      result.mutate({
        id: 'u-1',
        structureId: 'ls-1',
        request: {
          code: 'PHY-101-MOD',
          name: 'Physics I Advanced',
          unitType: 'subject',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.updateLearningUnit).toHaveBeenCalledWith('u-1', {
        code: 'PHY-101-MOD',
        name: 'Physics I Advanced',
        unitType: 'subject',
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Updated',
        }),
      );
    });

    it('useDeleteUnit deletes unit and invalidates cache', async () => {
      vi.spyOn(curriculumApi, 'deleteLearningUnit').mockResolvedValue(undefined);

      const { result, queryClient } = withSetup(() => useDeleteUnit());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({ id: 'u-1', structureId: 'ls-1' });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.deleteLearningUnit).toHaveBeenCalledWith('u-1');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'units', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Learning Unit Deleted',
        }),
      );
    });
  });

  // ==========================================================================
  // Academic Requirement Mutation Tests
  // ==========================================================================
  describe('Requirement Mutations', () => {
    it('useCreateRequirement creates requirement and invalidates cache', async () => {
      const mockReq: AcademicRequirement = {
        id: 'req-new',
        name: 'Prerequisite Algebra',
        requirementScope: 'learning_unit',
        learningStructureId: 'ls-1',
        learningUnitId: 'u-1',
        requirementType: 'prerequisite',
        description: 'Complete MATH-100 prior to enrollment',
        version: '1.0.0',
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
          name: 'Prerequisite Algebra',
          requirementScope: 'learning_unit',
          learningUnitId: 'u-1',
          requirementType: 'prerequisite',
          description: 'Complete MATH-100 prior to enrollment',
          version: '1.0.0',
          effectiveDate: '2026-07-01',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.createAcademicRequirement).toHaveBeenCalledWith(
        'ls-1',
        expect.objectContaining({ name: 'Prerequisite Algebra' }),
      );
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'requirements', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Requirement Created',
        }),
      );
    });

    it('useUpdateRequirement updates requirement and notifies success', async () => {
      const mockReq: AcademicRequirement = {
        id: 'req-1',
        name: 'Prerequisite Algebra Updated',
        requirementScope: 'learning_unit',
        learningStructureId: 'ls-1',
        requirementType: 'prerequisite',
        description: 'Updated criteria description',
        version: '1.0.0',
        effectiveDate: '2026-07-01',
        status: 'draft',
        createdAt: '2026-01-01',
        createdBy: 'admin',
      };
      vi.spyOn(curriculumApi, 'updateAcademicRequirement').mockResolvedValue(mockReq);

      const { result } = withSetup(() => useUpdateRequirement());

      result.mutate({
        id: 'req-1',
        structureId: 'ls-1',
        request: {
          name: 'Prerequisite Algebra Updated',
          description: 'Updated criteria description',
        },
      });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.updateAcademicRequirement).toHaveBeenCalledWith('req-1', {
        name: 'Prerequisite Algebra Updated',
        description: 'Updated criteria description',
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Requirement Updated',
        }),
      );
    });

    it('useDeleteRequirement removes requirement and triggers toast', async () => {
      vi.spyOn(curriculumApi, 'deleteAcademicRequirement').mockResolvedValue(undefined);

      const { result, queryClient } = withSetup(() => useDeleteRequirement());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      result.mutate({ id: 'req-1', structureId: 'ls-1' });

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(curriculumApi.deleteAcademicRequirement).toHaveBeenCalledWith('req-1');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['academics', 'requirements', 'ls-1'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Requirement Deleted',
        }),
      );
    });

    it('handles error gracefully and displays error toast on mutation failure', async () => {
      vi.spyOn(curriculumApi, 'deleteAcademicRequirement').mockRejectedValue(
        new Error('Cannot delete active requirement'),
      );

      const onError = vi.fn();
      const { result } = withSetup(() => useDeleteRequirement({ onError }));

      result.mutate('req-1');

      await vi.waitFor(() => expect(result.isError.value).toBe(true));
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Requirement Deletion Failed',
          detail: 'Cannot delete active requirement',
        }),
      );
      expect(onError).toHaveBeenCalled();
    });
  });
});
