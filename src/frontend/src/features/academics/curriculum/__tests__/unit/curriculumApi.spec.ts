// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import * as curriculumApi from '../../api/curriculumApi';
import type {
  CreateAcademicRequirementRequest,
  CreateLearningStructureRequest,
  CreateLearningUnitRequest,
  SupersedeStructureRequest,
  UpdateAcademicRequirementRequest,
  UpdateLearningStructureRequest,
  UpdateLearningUnitRequest,
} from '../../types/curriculum.types';

describe('Curriculum API Client (P7-S21)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const structureId = '11111111-1111-1111-1111-111111111111';
  const unitId = '22222222-2222-2222-2222-222222222222';
  const requirementId = '33333333-3333-3333-3333-333333333333';

  // ==========================================================================
  // 1. Learning Structures API Methods
  // ==========================================================================
  describe('Learning Structures Management API', () => {
    describe('getLearningStructures', () => {
      it('issues GET /v1/academics/learning-structures with empty params when filters omitted', async () => {
        const mockResponse = {
          items: [],
          pageNumber: 1,
          pageSize: 20,
          totalCount: 0,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
        };
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

        const result = await curriculumApi.getLearningStructures();

        expect(getSpy).toHaveBeenCalledTimes(1);
        expect(getSpy).toHaveBeenCalledWith('/v1/academics/learning-structures', {
          params: {},
        });
        expect(result).toEqual(mockResponse);
      });

      it('issues GET with correct query parameters when filters are provided', async () => {
        const mockResponse = {
          items: [{ id: structureId, name: 'Curriculum 2024' }],
          pageNumber: 2,
          pageSize: 15,
          totalCount: 25,
          totalPages: 2,
          hasPreviousPage: true,
          hasNextPage: false,
        };
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

        const result = await curriculumApi.getLearningStructures({
          academicPeriodId: 'period-2024',
          status: 'draft',
          search: '  Merdeka SD  ',
          page: 2,
          pageSize: 15,
        });

        expect(getSpy).toHaveBeenCalledWith('/v1/academics/learning-structures', {
          params: {
            academicPeriodId: 'period-2024',
            status: 'draft',
            search: 'Merdeka SD',
            page: 2,
            pageSize: 15,
          },
        });
        expect(result).toEqual(mockResponse);
      });

      it('omits invalid or empty filter values from query parameters', async () => {
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ items: [] } as never);

        await curriculumApi.getLearningStructures({
          search: '   ',
          page: 0,
          pageSize: -1,
        });

        expect(getSpy).toHaveBeenCalledWith('/v1/academics/learning-structures', {
          params: {},
        });
      });
    });

    describe('getLearningStructureById', () => {
      it('issues GET /v1/academics/learning-structures/{id} with encoded URL', async () => {
        const mockStructure = {
          id: structureId,
          name: 'Curriculum SMA',
          version: '2024.1',
          status: 'active' as const,
        };
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockStructure as never);

        const result = await curriculumApi.getLearningStructureById(structureId);

        expect(getSpy).toHaveBeenCalledTimes(1);
        expect(getSpy).toHaveBeenCalledWith(`/v1/academics/learning-structures/${structureId}`);
        expect(result).toEqual(mockStructure);
      });
    });

    describe('createLearningStructure', () => {
      it('issues POST /v1/academics/learning-structures with request body', async () => {
        const request: CreateLearningStructureRequest = {
          name: 'New Primary Framework',
          academicPeriodId: 'period-uuid',
          version: '1.0',
          effectiveDate: '2024-07-01',
          description: 'Primary school curriculum',
        };
        const mockCreated = { id: structureId, ...request, status: 'draft' as const };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

        const result = await curriculumApi.createLearningStructure(request);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith('/v1/academics/learning-structures', request);
        expect(result).toEqual(mockCreated);
      });
    });

    describe('updateLearningStructure', () => {
      it('issues PUT /v1/academics/learning-structures/{id} with encoded URL and request body', async () => {
        const request: UpdateLearningStructureRequest = {
          name: 'Updated Framework Title',
          description: 'Updated description notes',
          notes: 'Revision 2',
        };
        const mockUpdated = { id: structureId, ...request, status: 'draft' as const };
        const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(mockUpdated as never);

        const result = await curriculumApi.updateLearningStructure(structureId, request);

        expect(putSpy).toHaveBeenCalledTimes(1);
        expect(putSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}`,
          request,
        );
        expect(result).toEqual(mockUpdated);
      });
    });
  });

  // ==========================================================================
  // 2. Learning Structures Governance Lifecycle API Methods
  // ==========================================================================
  describe('Learning Structures Governance Lifecycle API', () => {
    describe('submitLearningStructure', () => {
      it('issues POST /v1/academics/learning-structures/{id}/submit with empty payload', async () => {
        const mockResponse = {
          id: structureId,
          status: 'submitted' as const,
          submittedBy: 'coordinator',
          submittedDate: '2026-11-05T01:00:00Z',
        };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

        const result = await curriculumApi.submitLearningStructure(structureId);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/submit`,
          {},
        );
        expect(result).toEqual(mockResponse);
      });
    });

    describe('approveLearningStructure', () => {
      it('issues POST /v1/academics/learning-structures/{id}/approve with empty payload', async () => {
        const mockResponse = {
          id: structureId,
          status: 'active' as const,
          approvedBy: 'authority',
          approvedDate: '2026-11-05T01:00:00Z',
        };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

        const result = await curriculumApi.approveLearningStructure(structureId);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/approve`,
          {},
        );
        expect(result).toEqual(mockResponse);
      });
    });

    describe('supersedeLearningStructure', () => {
      it('issues POST /v1/academics/learning-structures/{id}/supersede with request payload', async () => {
        const request: SupersedeStructureRequest = {
          newVersion: '2025.1',
          effectiveDate: '2025-07-01',
          academicPeriodId: 'period-2025',
          academicPeriodLabel: 'TP 2025/2026',
        };
        const mockCloned = {
          id: 'new-cloned-id',
          name: 'Cloned Structure',
          version: '2025.1',
          status: 'draft' as const,
          supersedesId: structureId,
        };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCloned as never);

        const result = await curriculumApi.supersedeLearningStructure(structureId, request);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/supersede`,
          request,
        );
        expect(result).toEqual(mockCloned);
      });
    });

    describe('archiveLearningStructure', () => {
      it('issues POST /v1/academics/learning-structures/{id}/archive with empty payload', async () => {
        const mockResponse = {
          id: structureId,
          status: 'archived' as const,
          archivedBy: 'authority',
          archivedDate: '2026-11-05T01:00:00Z',
        };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

        const result = await curriculumApi.archiveLearningStructure(structureId);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/archive`,
          {},
        );
        expect(result).toEqual(mockResponse);
      });
    });
  });

  // ==========================================================================
  // 3. Learning Units API Methods
  // ==========================================================================
  describe('Learning Units API', () => {
    describe('getLearningUnits', () => {
      it('issues GET /v1/academics/learning-structures/{structureId}/units with empty params when filters omitted', async () => {
        const mockUnits = [
          { id: unitId, code: 'MATH-101', name: 'Mathematics', unitType: 'subject' },
        ];
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockUnits as never);

        const result = await curriculumApi.getLearningUnits(structureId);

        expect(getSpy).toHaveBeenCalledTimes(1);
        expect(getSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/units`,
          { params: {} },
        );
        expect(result).toEqual(mockUnits);
      });

      it('issues GET with sanitized filter params when provided', async () => {
        const mockUnits = [{ id: unitId, code: 'MATH-101-A' }];
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockUnits as never);

        const result = await curriculumApi.getLearningUnits(structureId, {
          unitType: 'module',
          parentUnitId: 'parent-unit-uuid',
          level: 'Grade 10',
          search: '  algebra  ',
        });

        expect(getSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/units`,
          {
            params: {
              unitType: 'module',
              parentUnitId: 'parent-unit-uuid',
              level: 'Grade 10',
              search: 'algebra',
            },
          },
        );
        expect(result).toEqual(mockUnits);
      });

      it('safely handles non-array responses by returning an empty array', async () => {
        vi.spyOn(apiClient, 'get').mockResolvedValueOnce(null as never);
        const result = await curriculumApi.getLearningUnits(structureId);
        expect(result).toEqual([]);
      });
    });

    describe('getLearningUnitById', () => {
      it('issues GET /v1/academics/learning-units/{id} with encoded URL', async () => {
        const mockUnit = { id: unitId, code: 'MATH-101', name: 'Mathematics' };
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockUnit as never);

        const result = await curriculumApi.getLearningUnitById(unitId);

        expect(getSpy).toHaveBeenCalledTimes(1);
        expect(getSpy).toHaveBeenCalledWith(`/v1/academics/learning-units/${unitId}`);
        expect(result).toEqual(mockUnit);
      });
    });

    describe('createLearningUnit', () => {
      it('issues POST /v1/academics/learning-structures/{structureId}/units with request body', async () => {
        const request: CreateLearningUnitRequest = {
          code: 'SCI-100',
          name: 'Natural Sciences',
          unitType: 'subject',
          level: 'Grade 7',
          creditWeight: 3,
        };
        const mockCreated = { id: unitId, ...request, status: 'draft' as const };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

        const result = await curriculumApi.createLearningUnit(structureId, request);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/units`,
          request,
        );
        expect(result).toEqual(mockCreated);
      });
    });

    describe('updateLearningUnit', () => {
      it('issues PUT /v1/academics/learning-units/{id} with request body', async () => {
        const request: UpdateLearningUnitRequest = {
          code: 'SCI-100-ADV',
          name: 'Advanced Natural Sciences',
          unitType: 'subject',
          level: 'Grade 7',
          creditWeight: 4,
        };
        const mockUpdated = { id: unitId, ...request, status: 'draft' as const };
        const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(mockUpdated as never);

        const result = await curriculumApi.updateLearningUnit(unitId, request);

        expect(putSpy).toHaveBeenCalledTimes(1);
        expect(putSpy).toHaveBeenCalledWith(`/v1/academics/learning-units/${unitId}`, request);
        expect(result).toEqual(mockUpdated);
      });
    });

    describe('deleteLearningUnit', () => {
      it('issues DELETE /v1/academics/learning-units/{id} with encoded URL', async () => {
        const deleteSpy = vi.spyOn(apiClient, 'delete').mockResolvedValueOnce(undefined as never);

        await curriculumApi.deleteLearningUnit(unitId);

        expect(deleteSpy).toHaveBeenCalledTimes(1);
        expect(deleteSpy).toHaveBeenCalledWith(`/v1/academics/learning-units/${unitId}`);
      });
    });
  });

  // ==========================================================================
  // 4. Academic Requirements API Methods
  // ==========================================================================
  describe('Academic Requirements API', () => {
    describe('getAcademicRequirements', () => {
      it('issues GET /v1/academics/learning-structures/{structureId}/requirements and unwrap requirements array', async () => {
        const mockRequirements = [
          { id: requirementId, name: 'Min 120 Credits', requirementScope: 'program' },
        ];
        const mockWrapper = {
          learningStructureId: structureId,
          requirements: mockRequirements,
        };
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockWrapper as never);

        const result = await curriculumApi.getAcademicRequirements(structureId, {
          scope: 'program',
          type: 'credit',
        });

        expect(getSpy).toHaveBeenCalledTimes(1);
        expect(getSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/requirements`,
          {
            params: { scope: 'program', type: 'credit' },
          },
        );
        expect(result).toEqual(mockRequirements);
      });

      it('handles unwrapped raw array responses directly', async () => {
        const rawArray = [{ id: requirementId, name: 'Direct Array' }];
        vi.spyOn(apiClient, 'get').mockResolvedValueOnce(rawArray as never);

        const result = await curriculumApi.getAcademicRequirements(structureId);
        expect(result).toEqual(rawArray);
      });

      it('handles null, undefined, or empty responses by returning empty array', async () => {
        vi.spyOn(apiClient, 'get').mockResolvedValueOnce(null as never);
        const result = await curriculumApi.getAcademicRequirements(structureId);
        expect(result).toEqual([]);
      });
    });

    describe('getAcademicRequirementById', () => {
      it('issues GET /v1/academics/academic-requirements/{id} with encoded URL', async () => {
        const mockReq = {
          id: requirementId,
          name: 'Grade 9 Promotion',
          requirementScope: 'level' as const,
        };
        const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockReq as never);

        const result = await curriculumApi.getAcademicRequirementById(requirementId);

        expect(getSpy).toHaveBeenCalledTimes(1);
        expect(getSpy).toHaveBeenCalledWith(`/v1/academics/academic-requirements/${requirementId}`);
        expect(result).toEqual(mockReq);
      });
    });

    describe('createAcademicRequirement', () => {
      it('issues POST /v1/academics/learning-structures/{structureId}/requirements with request body', async () => {
        const request: CreateAcademicRequirementRequest = {
          name: 'English Language Competency',
          requirementScope: 'program',
          requirementType: 'completion',
          description: 'Must achieve minimum grade B in English',
          version: '1.0',
          effectiveDate: '2024-07-01',
        };
        const mockCreated = { id: requirementId, ...request, status: 'draft' as const };
        const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

        const result = await curriculumApi.createAcademicRequirement(structureId, request);

        expect(postSpy).toHaveBeenCalledTimes(1);
        expect(postSpy).toHaveBeenCalledWith(
          `/v1/academics/learning-structures/${structureId}/requirements`,
          request,
        );
        expect(result).toEqual(mockCreated);
      });
    });

    describe('updateAcademicRequirement', () => {
      it('issues PUT /v1/academics/academic-requirements/{id} with request body', async () => {
        const request: UpdateAcademicRequirementRequest = {
          name: 'English Competency Updated',
          description: 'Revised description criteria',
          notes: 'Applicable starting next term',
        };
        const mockUpdated = { id: requirementId, ...request, status: 'draft' as const };
        const putSpy = vi.spyOn(apiClient, 'put').mockResolvedValueOnce(mockUpdated as never);

        const result = await curriculumApi.updateAcademicRequirement(requirementId, request);

        expect(putSpy).toHaveBeenCalledTimes(1);
        expect(putSpy).toHaveBeenCalledWith(
          `/v1/academics/academic-requirements/${requirementId}`,
          request,
        );
        expect(result).toEqual(mockUpdated);
      });
    });

    describe('deleteAcademicRequirement', () => {
      it('issues DELETE /v1/academics/academic-requirements/{id} with encoded URL', async () => {
        const deleteSpy = vi.spyOn(apiClient, 'delete').mockResolvedValueOnce(undefined as never);

        await curriculumApi.deleteAcademicRequirement(requirementId);

        expect(deleteSpy).toHaveBeenCalledTimes(1);
        expect(deleteSpy).toHaveBeenCalledWith(
          `/v1/academics/academic-requirements/${requirementId}`,
        );
      });
    });
  });
});
