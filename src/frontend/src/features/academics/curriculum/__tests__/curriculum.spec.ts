// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  createLearningStructureSchema,
  updateLearningStructureSchema,
  supersedeStructureSchema,
  createLearningUnitSchema,
  updateLearningUnitSchema,
  createAcademicRequirementSchema,
  updateAcademicRequirementSchema,
} from '../schemas/curriculum.schemas';
import * as curriculumApi from '../api/curriculumApi';
import { apiClient } from '@/shared/services/http';

vi.mock('@/shared/services/http', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Curriculum Module (P5-S14)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ==========================================================================
  // 1. Zod Schemas Validation
  // ==========================================================================
  describe('Zod Validation Schemas', () => {
    describe('createLearningStructureSchema', () => {
      it('validates a valid create structure payload', () => {
        const payload = {
          name: 'Kurikulum Merdeka SD 2024',
          academicPeriodId: '11111111-1111-1111-1111-111111111111',
          version: '2024.1',
          effectiveDate: '2024-07-01',
          endDate: '2025-06-30',
          description: 'Standard SD curriculum',
          notes: 'Initial draft',
        };

        const result = createLearningStructureSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });

      it('rejects payload when endDate is before effectiveDate', () => {
        const payload = {
          name: 'Kurikulum Merdeka SD 2024',
          academicPeriodId: '11111111-1111-1111-1111-111111111111',
          version: '2024.1',
          effectiveDate: '2024-07-01',
          endDate: '2024-06-30',
        };

        const result = createLearningStructureSchema.safeParse(payload);
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.endDate).toContain(
            'End date must be greater than or equal to effective date.',
          );
        }
      });

      it('rejects missing required fields', () => {
        const result = createLearningStructureSchema.safeParse({});
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.name).toBeDefined();
          expect(errors.academicPeriodId).toBeDefined();
          expect(errors.version).toBeDefined();
          expect(errors.effectiveDate).toBeDefined();
        }
      });
    });

    describe('updateLearningStructureSchema', () => {
      it('validates a valid update payload', () => {
        const result = updateLearningStructureSchema.safeParse({
          name: 'Updated Curriculum Title',
          description: 'Updated description',
          notes: 'Updated notes',
        });
        expect(result.success).toBe(true);
      });

      it('rejects empty name', () => {
        const result = updateLearningStructureSchema.safeParse({
          name: '',
        });
        expect(result.success).toBe(false);
      });
    });

    describe('supersedeStructureSchema', () => {
      it('validates a valid supersede request', () => {
        const result = supersedeStructureSchema.safeParse({
          newVersion: '2025.1',
          effectiveDate: '2025-07-01',
          academicPeriodId: '22222222-2222-2222-2222-222222222222',
          academicPeriodLabel: 'TP 2025/2026',
        });
        expect(result.success).toBe(true);
      });

      it('rejects missing newVersion or effectiveDate', () => {
        const result = supersedeStructureSchema.safeParse({});
        expect(result.success).toBe(false);
      });
    });

    describe('createLearningUnitSchema', () => {
      it('validates a valid root learning unit', () => {
        const result = createLearningUnitSchema.safeParse({
          code: 'MATH-01',
          name: 'Mathematics',
          unitType: 'subject',
          level: 'Grade 1',
          creditWeight: 4,
          notes: 'Core subject',
        });
        expect(result.success).toBe(true);
      });

      it('validates a child unit with parentUnitId', () => {
        const result = createLearningUnitSchema.safeParse({
          code: 'MATH-01-A',
          name: 'Arithmetic Module',
          unitType: 'module',
          parentUnitId: '33333333-3333-3333-3333-333333333333',
          creditWeight: 2,
        });
        expect(result.success).toBe(true);
      });

      it('rejects invalid unit type', () => {
        const result = createLearningUnitSchema.safeParse({
          code: 'MATH-01',
          name: 'Mathematics',
          unitType: 'invalid_type',
        });
        expect(result.success).toBe(false);
      });

      it('rejects negative credit weight', () => {
        const result = createLearningUnitSchema.safeParse({
          code: 'MATH-01',
          name: 'Mathematics',
          unitType: 'subject',
          creditWeight: -1,
        });
        expect(result.success).toBe(false);
      });
    });

    describe('updateLearningUnitSchema', () => {
      it('validates a valid update unit payload', () => {
        const result = updateLearningUnitSchema.safeParse({
          code: 'MATH-01-V2',
          name: 'Advanced Mathematics',
          unitType: 'course',
          creditWeight: 3.5,
        });
        expect(result.success).toBe(true);
      });
    });

    describe('createAcademicRequirementSchema', () => {
      it('validates a valid program-scoped requirement', () => {
        const result = createAcademicRequirementSchema.safeParse({
          name: 'Total Credit Threshold',
          requirementScope: 'program',
          requirementType: 'credit',
          description: 'Students must earn at least 120 total credits.',
          version: '1.0',
          effectiveDate: '2024-07-01',
        });
        expect(result.success).toBe(true);
      });

      it('requires learningUnitId when requirementScope is learning_unit', () => {
        const result = createAcademicRequirementSchema.safeParse({
          name: 'Prerequisite Algebra',
          requirementScope: 'learning_unit',
          requirementType: 'prerequisite',
          description: 'Must pass Algebra 1 before Calculus.',
          version: '1.0',
          effectiveDate: '2024-07-01',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.learningUnitId).toContain(
            "Learning unit ID is required when requirement scope is 'learning_unit'.",
          );
        }
      });

      it('requires level when requirementScope is level', () => {
        const result = createAcademicRequirementSchema.safeParse({
          name: 'Grade 4 Promotion',
          requirementScope: 'level',
          requirementType: 'progression',
          description: 'Must maintain minimum 70 average.',
          version: '1.0',
          effectiveDate: '2024-07-01',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          expect(errors.level).toContain("Level is required when requirement scope is 'level'.");
        }
      });

      it('passes when learningUnitId is supplied for learning_unit scope', () => {
        const result = createAcademicRequirementSchema.safeParse({
          name: 'Prerequisite Unit',
          requirementScope: 'learning_unit',
          learningUnitId: '44444444-4444-4444-4444-444444444444',
          requirementType: 'prerequisite',
          description: 'Must complete unit 4.',
          version: '1.0',
          effectiveDate: '2024-07-01',
        });
        expect(result.success).toBe(true);
      });
    });

    describe('updateAcademicRequirementSchema', () => {
      it('validates valid update requirement payload', () => {
        const result = updateAcademicRequirementSchema.safeParse({
          name: 'Updated Requirement Title',
          description: 'Updated criteria description',
          notes: 'Updated notes',
        });
        expect(result.success).toBe(true);
      });
    });
  });

  // ==========================================================================
  // 2. Axios API Client Functions
  // ==========================================================================
  describe('Curriculum API Client', () => {
    const structureId = '11111111-1111-1111-1111-111111111111';
    const unitId = '22222222-2222-2222-2222-222222222222';
    const requirementId = '33333333-3333-3333-3333-333333333333';

    it('getLearningStructures passes filter params and returns paginated response', async () => {
      const mockResponse = {
        items: [{ id: structureId, name: 'Curriculum 2024' }],
        pageNumber: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.mocked(apiClient.get).mockResolvedValueOnce(mockResponse);

      const result = await curriculumApi.getLearningStructures({
        academicPeriodId: 'period-123',
        status: 'draft',
        search: '2024',
        page: 1,
        pageSize: 10,
      });

      expect(apiClient.get).toHaveBeenCalledWith('/v1/academics/learning-structures', {
        params: {
          academicPeriodId: 'period-123',
          status: 'draft',
          search: '2024',
          page: 1,
          pageSize: 10,
        },
      });
      expect(result).toEqual(mockResponse);
    });

    it('getLearningStructureById calls GET with id', async () => {
      const mockDetail = { id: structureId, name: 'Structure Detail' };
      vi.mocked(apiClient.get).mockResolvedValueOnce(mockDetail);

      const result = await curriculumApi.getLearningStructureById(structureId);
      expect(apiClient.get).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}`,
      );
      expect(result).toEqual(mockDetail);
    });

    it('createLearningStructure calls POST with payload', async () => {
      const payload = {
        name: 'New Structure',
        academicPeriodId: 'period-1',
        version: '1.0',
        effectiveDate: '2024-07-01',
      };
      const mockCreated = { id: structureId, ...payload, status: 'draft' };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockCreated);

      const result = await curriculumApi.createLearningStructure(payload);
      expect(apiClient.post).toHaveBeenCalledWith('/v1/academics/learning-structures', payload);
      expect(result).toEqual(mockCreated);
    });

    it('updateLearningStructure calls PUT with id and payload', async () => {
      const payload = { name: 'Updated Title' };
      const mockUpdated = { id: structureId, name: 'Updated Title' };
      vi.mocked(apiClient.put).mockResolvedValueOnce(mockUpdated);

      const result = await curriculumApi.updateLearningStructure(structureId, payload);
      expect(apiClient.put).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}`,
        payload,
      );
      expect(result).toEqual(mockUpdated);
    });

    it('submitLearningStructure calls POST submit endpoint', async () => {
      const mockResponse = { id: structureId, status: 'submitted' };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockResponse);

      const result = await curriculumApi.submitLearningStructure(structureId);
      expect(apiClient.post).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/submit`,
        {},
      );
      expect(result).toEqual(mockResponse);
    });

    it('approveLearningStructure calls POST approve endpoint', async () => {
      const mockResponse = { id: structureId, status: 'active', approvedBy: 'Authority' };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockResponse);

      const result = await curriculumApi.approveLearningStructure(structureId);
      expect(apiClient.post).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/approve`,
        {},
      );
      expect(result).toEqual(mockResponse);
    });

    it('supersedeLearningStructure calls POST supersede endpoint with payload', async () => {
      const payload = { newVersion: '2025.1', effectiveDate: '2025-07-01' };
      const mockCloned = { id: 'new-id', version: '2025.1', status: 'draft' };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockCloned);

      const result = await curriculumApi.supersedeLearningStructure(structureId, payload);
      expect(apiClient.post).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/supersede`,
        payload,
      );
      expect(result).toEqual(mockCloned);
    });

    it('archiveLearningStructure calls POST archive endpoint', async () => {
      const mockResponse = { id: structureId, status: 'archived' };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockResponse);

      const result = await curriculumApi.archiveLearningStructure(structureId);
      expect(apiClient.post).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/archive`,
        {},
      );
      expect(result).toEqual(mockResponse);
    });

    it('getLearningUnits calls GET with structureId and filters', async () => {
      const mockUnits = [{ id: unitId, code: 'MATH-1' }];
      vi.mocked(apiClient.get).mockResolvedValueOnce(mockUnits);

      const result = await curriculumApi.getLearningUnits(structureId, { unitType: 'subject' });
      expect(apiClient.get).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/units`,
        { params: { unitType: 'subject' } },
      );
      expect(result).toEqual(mockUnits);
    });

    it('getLearningUnitById calls GET with unit id', async () => {
      const mockUnit = { id: unitId, code: 'MATH-1' };
      vi.mocked(apiClient.get).mockResolvedValueOnce(mockUnit);

      const result = await curriculumApi.getLearningUnitById(unitId);
      expect(apiClient.get).toHaveBeenCalledWith(`/v1/academics/learning-units/${unitId}`);
      expect(result).toEqual(mockUnit);
    });

    it('createLearningUnit calls POST with structureId and payload', async () => {
      const payload = { code: 'BIO-1', name: 'Biology', unitType: 'subject' };
      const mockUnit = { id: unitId, ...payload };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockUnit);

      const result = await curriculumApi.createLearningUnit(structureId, payload);
      expect(apiClient.post).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/units`,
        payload,
      );
      expect(result).toEqual(mockUnit);
    });

    it('updateLearningUnit calls PUT with unit id and payload', async () => {
      const payload = { code: 'BIO-1', name: 'Biology I', unitType: 'subject' };
      const mockUnit = { id: unitId, ...payload };
      vi.mocked(apiClient.put).mockResolvedValueOnce(mockUnit);

      const result = await curriculumApi.updateLearningUnit(unitId, payload);
      expect(apiClient.put).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${unitId}`,
        payload,
      );
      expect(result).toEqual(mockUnit);
    });

    it('deleteLearningUnit calls DELETE with unit id', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce(undefined);

      await curriculumApi.deleteLearningUnit(unitId);
      expect(apiClient.delete).toHaveBeenCalledWith(`/v1/academics/learning-units/${unitId}`);
    });

    it('getAcademicRequirements calls GET and unwraps requirements array', async () => {
      const mockRequirements = [{ id: requirementId, name: 'Min GPA' }];
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        learningStructureId: structureId,
        requirements: mockRequirements,
      });

      const result = await curriculumApi.getAcademicRequirements(structureId, {
        scope: 'program',
        type: 'progression',
      });
      expect(apiClient.get).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/requirements`,
        { params: { scope: 'program', type: 'progression' } },
      );
      expect(result).toEqual(mockRequirements);
    });

    it('getAcademicRequirementById calls GET with requirement id', async () => {
      const mockReq = { id: requirementId, name: 'Req Detail' };
      vi.mocked(apiClient.get).mockResolvedValueOnce(mockReq);

      const result = await curriculumApi.getAcademicRequirementById(requirementId);
      expect(apiClient.get).toHaveBeenCalledWith(
        `/v1/academics/academic-requirements/${requirementId}`,
      );
      expect(result).toEqual(mockReq);
    });

    it('createAcademicRequirement calls POST with structureId and payload', async () => {
      const payload = {
        name: 'Req 1',
        requirementScope: 'program',
        requirementType: 'credit',
        description: 'Description',
        version: '1.0',
        effectiveDate: '2024-07-01',
      };
      const mockReq = { id: requirementId, ...payload };
      vi.mocked(apiClient.post).mockResolvedValueOnce(mockReq);

      const result = await curriculumApi.createAcademicRequirement(structureId, payload);
      expect(apiClient.post).toHaveBeenCalledWith(
        `/v1/academics/learning-structures/${structureId}/requirements`,
        payload,
      );
      expect(result).toEqual(mockReq);
    });

    it('updateAcademicRequirement calls PUT with requirement id and payload', async () => {
      const payload = { name: 'Updated Req', description: 'Updated Desc' };
      const mockReq = { id: requirementId, ...payload };
      vi.mocked(apiClient.put).mockResolvedValueOnce(mockReq);

      const result = await curriculumApi.updateAcademicRequirement(requirementId, payload);
      expect(apiClient.put).toHaveBeenCalledWith(
        `/v1/academics/academic-requirements/${requirementId}`,
        payload,
      );
      expect(result).toEqual(mockReq);
    });

    it('deleteAcademicRequirement calls DELETE with requirement id', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce(undefined);

      await curriculumApi.deleteAcademicRequirement(requirementId);
      expect(apiClient.delete).toHaveBeenCalledWith(
        `/v1/academics/academic-requirements/${requirementId}`,
      );
    });
  });
});
