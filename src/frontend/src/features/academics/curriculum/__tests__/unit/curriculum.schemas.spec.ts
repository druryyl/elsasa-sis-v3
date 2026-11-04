// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  createLearningStructureSchema,
  updateLearningStructureSchema,
  supersedeStructureSchema,
  createLearningUnitSchema,
  updateLearningUnitSchema,
  createAcademicRequirementSchema,
  updateAcademicRequirementSchema,
  LEARNING_UNIT_TYPES,
  ACADEMIC_REQUIREMENT_SCOPES,
  ACADEMIC_REQUIREMENT_TYPES,
} from '../../schemas/curriculum.schemas';

describe('Curriculum Zod Validation Schemas (P7-S21)', () => {
  // ==========================================================================
  // 1. Learning Structure Schemas
  // ==========================================================================
  describe('createLearningStructureSchema', () => {
    const validPayload = {
      name: 'Kurikulum Merdeka SMA 2024',
      academicPeriodId: '00000000-0000-0000-0000-000000000001',
      version: '2024.1',
      effectiveDate: '2024-07-01',
      endDate: '2025-06-30',
      description: 'Senior High School curriculum framework',
      notes: 'Approved for pilot run',
    };

    it('passes for a fully populated valid payload', () => {
      const result = createLearningStructureSchema.safeParse(validPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.name).toBe('Kurikulum Merdeka SMA 2024');
        expect(result.data.version).toBe('2024.1');
      }
    });

    it('passes for a minimal valid payload omitting optional fields', () => {
      const minimalPayload = {
        name: 'Kurikulum Minimal',
        academicPeriodId: '00000000-0000-0000-0000-000000000001',
        version: '1.0',
        effectiveDate: '2024-07-01',
      };
      const result = createLearningStructureSchema.safeParse(minimalPayload);
      expect(result.success).toBe(true);
    });

    it('passes when endDate equals effectiveDate', () => {
      const payload = {
        ...validPayload,
        effectiveDate: '2024-07-01',
        endDate: '2024-07-01',
      };
      const result = createLearningStructureSchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('passes when endDate is null or undefined', () => {
      const payloadNull = { ...validPayload, endDate: null };
      const payloadUndef = { ...validPayload, endDate: undefined };
      expect(createLearningStructureSchema.safeParse(payloadNull).success).toBe(true);
      expect(createLearningStructureSchema.safeParse(payloadUndef).success).toBe(true);
    });

    it('fails when endDate is strictly before effectiveDate', () => {
      const payload = {
        ...validPayload,
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

    it('fails when required fields are missing', () => {
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

    it('fails when required strings are empty or whitespace-only', () => {
      const result = createLearningStructureSchema.safeParse({
        name: '   ',
        academicPeriodId: '   ',
        version: '   ',
        effectiveDate: '   ',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.name).toBeDefined();
        expect(errors.academicPeriodId).toBeDefined();
        expect(errors.version).toBeDefined();
        expect(errors.effectiveDate).toBeDefined();
      }
    });

    it('fails when string lengths exceed maximum constraints', () => {
      const result = createLearningStructureSchema.safeParse({
        ...validPayload,
        name: 'a'.repeat(201),
        version: 'v'.repeat(51),
        description: 'd'.repeat(4001),
        notes: 'n'.repeat(4001),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.name).toContain('Name cannot exceed 200 characters.');
        expect(errors.version).toContain('Version cannot exceed 50 characters.');
        expect(errors.description).toContain('Description cannot exceed 4000 characters.');
        expect(errors.notes).toContain('Notes cannot exceed 4000 characters.');
      }
    });

    it('fails for invalid ISO date formats or non-existent calendar dates', () => {
      const invalidRegex = createLearningStructureSchema.safeParse({
        ...validPayload,
        effectiveDate: '01/07/2024',
      });
      expect(invalidRegex.success).toBe(false);

      const nonExistentDate = createLearningStructureSchema.safeParse({
        ...validPayload,
        effectiveDate: '2024-99-99',
      });
      expect(nonExistentDate.success).toBe(false);
    });
  });

  describe('updateLearningStructureSchema', () => {
    it('passes for a valid update payload', () => {
      const result = updateLearningStructureSchema.safeParse({
        name: 'Updated Curriculum Title',
        description: 'New revised curriculum overview',
        notes: 'Updated notes',
      });
      expect(result.success).toBe(true);
    });

    it('passes when description and notes are null or undefined', () => {
      const result = updateLearningStructureSchema.safeParse({
        name: 'Valid Name',
        description: null,
        notes: undefined,
      });
      expect(result.success).toBe(true);
    });

    it('fails when name is missing or empty', () => {
      expect(updateLearningStructureSchema.safeParse({}).success).toBe(false);
      expect(updateLearningStructureSchema.safeParse({ name: '' }).success).toBe(false);
      expect(updateLearningStructureSchema.safeParse({ name: '   ' }).success).toBe(false);
    });

    it('fails when field lengths exceed constraints', () => {
      const result = updateLearningStructureSchema.safeParse({
        name: 'n'.repeat(201),
        description: 'd'.repeat(4001),
        notes: 'n'.repeat(4001),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.name).toBeDefined();
        expect(errors.description).toBeDefined();
        expect(errors.notes).toBeDefined();
      }
    });
  });

  describe('supersedeStructureSchema', () => {
    it('passes for a valid supersede payload with period linkage', () => {
      const result = supersedeStructureSchema.safeParse({
        newVersion: '2025.1',
        effectiveDate: '2025-07-01',
        academicPeriodId: '00000000-0000-0000-0000-000000000002',
        academicPeriodLabel: 'Academic Year 2025/2026',
      });
      expect(result.success).toBe(true);
    });

    it('passes for a minimal supersede payload omitting period linkage', () => {
      const result = supersedeStructureSchema.safeParse({
        newVersion: '2025.2',
        effectiveDate: '2025-08-01',
      });
      expect(result.success).toBe(true);
    });

    it('fails when newVersion or effectiveDate is missing or empty', () => {
      expect(supersedeStructureSchema.safeParse({}).success).toBe(false);
      expect(
        supersedeStructureSchema.safeParse({ newVersion: '', effectiveDate: '2025-07-01' }).success,
      ).toBe(false);
      expect(
        supersedeStructureSchema.safeParse({ newVersion: '2025.1', effectiveDate: '' }).success,
      ).toBe(false);
    });

    it('fails when newVersion exceeds 50 chars or periodLabel exceeds 100 chars', () => {
      const result = supersedeStructureSchema.safeParse({
        newVersion: 'v'.repeat(51),
        effectiveDate: '2025-07-01',
        academicPeriodLabel: 'p'.repeat(101),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.newVersion).toBeDefined();
        expect(errors.academicPeriodLabel).toBeDefined();
      }
    });

    it('fails for invalid date format in effectiveDate', () => {
      const result = supersedeStructureSchema.safeParse({
        newVersion: '2025.1',
        effectiveDate: 'invalid-date',
      });
      expect(result.success).toBe(false);
    });
  });

  // ==========================================================================
  // 2. Learning Unit Schemas
  // ==========================================================================
  describe('createLearningUnitSchema', () => {
    const validRootUnit = {
      code: 'MATH-101',
      name: 'Mathematics I',
      unitType: 'subject' as const,
      level: 'Grade 10',
      creditWeight: 4,
      description: 'Foundational mathematics course',
      notes: 'Required for all science majors',
    };

    it('passes for a valid root learning unit without parentUnitId', () => {
      const result = createLearningUnitSchema.safeParse(validRootUnit);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.code).toBe('MATH-101');
        expect(result.data.parentUnitId).toBeUndefined();
      }
    });

    it('passes for a valid child learning unit with parentUnitId', () => {
      const childUnit = {
        code: 'MATH-101-A',
        name: 'Algebra Module',
        unitType: 'module' as const,
        parentUnitId: '00000000-0000-0000-0000-000000000010',
        creditWeight: 2,
      };
      const result = createLearningUnitSchema.safeParse(childUnit);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.parentUnitId).toBe('00000000-0000-0000-0000-000000000010');
      }
    });

    it('passes for all permitted unitType enum values', () => {
      for (const type of LEARNING_UNIT_TYPES) {
        const result = createLearningUnitSchema.safeParse({
          ...validRootUnit,
          unitType: type,
        });
        expect(result.success).toBe(true);
      }
    });

    it('fails when unitType is invalid or missing', () => {
      expect(
        createLearningUnitSchema.safeParse({
          ...validRootUnit,
          unitType: 'invalid_type',
        }).success,
      ).toBe(false);

      const missingTypeResult = createLearningUnitSchema.safeParse({
        code: 'ENG-1',
        name: 'English',
      });
      expect(missingTypeResult.success).toBe(false);
      if (!missingTypeResult.success) {
        const errors = missingTypeResult.error.flatten().fieldErrors;
        expect(errors.unitType).toContain('Unit type is required.');
      }
    });

    it('fails when code or name is missing or empty', () => {
      expect(createLearningUnitSchema.safeParse({}).success).toBe(false);
      expect(
        createLearningUnitSchema.safeParse({
          code: '',
          name: 'Name',
          unitType: 'subject',
        }).success,
      ).toBe(false);
      expect(
        createLearningUnitSchema.safeParse({
          code: 'CODE',
          name: '   ',
          unitType: 'subject',
        }).success,
      ).toBe(false);
    });

    it('validates creditWeight numeric bounds correctly', () => {
      // Negative credit weight rejected
      const negResult = createLearningUnitSchema.safeParse({
        ...validRootUnit,
        creditWeight: -0.5,
      });
      expect(negResult.success).toBe(false);
      if (!negResult.success) {
        const errors = negResult.error.flatten().fieldErrors;
        expect(errors.creditWeight).toContain('Credit weight must be greater than or equal to 0.');
      }

      // Zero credit weight is allowed (e.g. non-credit extracurricular)
      expect(
        createLearningUnitSchema.safeParse({
          ...validRootUnit,
          creditWeight: 0,
        }).success,
      ).toBe(true);

      // Fractional / decimal credit weight is allowed
      expect(
        createLearningUnitSchema.safeParse({
          ...validRootUnit,
          creditWeight: 1.5,
        }).success,
      ).toBe(true);

      // Null or undefined credit weight is allowed
      expect(
        createLearningUnitSchema.safeParse({
          ...validRootUnit,
          creditWeight: null,
        }).success,
      ).toBe(true);
      expect(
        createLearningUnitSchema.safeParse({
          ...validRootUnit,
          creditWeight: undefined,
        }).success,
      ).toBe(true);
    });

    it('fails when code exceeds 50 chars or name exceeds 200 chars', () => {
      const result = createLearningUnitSchema.safeParse({
        ...validRootUnit,
        code: 'c'.repeat(51),
        name: 'n'.repeat(201),
        level: 'l'.repeat(51),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.code).toBeDefined();
        expect(errors.name).toBeDefined();
        expect(errors.level).toBeDefined();
      }
    });
  });

  describe('updateLearningUnitSchema', () => {
    it('passes for a valid update learning unit payload', () => {
      const result = updateLearningUnitSchema.safeParse({
        code: 'MATH-101-REV',
        name: 'Mathematics I (Revised)',
        unitType: 'course',
        level: 'Grade 10',
        creditWeight: 3.5,
        description: 'Updated syllabus',
      });
      expect(result.success).toBe(true);
    });

    it('fails when required code, name, or unitType is missing', () => {
      expect(updateLearningUnitSchema.safeParse({}).success).toBe(false);
      expect(
        updateLearningUnitSchema.safeParse({
          code: 'CODE',
          name: 'Name',
          unitType: 'invalid',
        }).success,
      ).toBe(false);
    });

    it('fails when creditWeight is negative', () => {
      const result = updateLearningUnitSchema.safeParse({
        code: 'CODE',
        name: 'Name',
        unitType: 'subject',
        creditWeight: -2,
      });
      expect(result.success).toBe(false);
    });
  });

  // ==========================================================================
  // 3. Academic Requirement Schemas
  // ==========================================================================
  describe('createAcademicRequirementSchema', () => {
    const validProgramReq = {
      name: 'Minimum Graduation Credits',
      requirementScope: 'program' as const,
      requirementType: 'credit' as const,
      description: 'Candidate must complete at least 140 cumulative credits.',
      version: '1.0',
      effectiveDate: '2024-07-01',
      endDate: '2028-06-30',
      notes: 'Policy applies to 2024 cohort onwards',
    };

    it('passes for a valid program-scoped requirement without unit or level', () => {
      const result = createAcademicRequirementSchema.safeParse(validProgramReq);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.requirementScope).toBe('program');
        expect(result.data.requirementType).toBe('credit');
      }
    });

    it('passes for all allowed requirementScope and requirementType enums', () => {
      for (const scope of ACADEMIC_REQUIREMENT_SCOPES) {
        for (const type of ACADEMIC_REQUIREMENT_TYPES) {
          const payload = {
            ...validProgramReq,
            requirementScope: scope,
            requirementType: type,
            learningUnitId:
              scope === 'learning_unit' ? '00000000-0000-0000-0000-000000000099' : undefined,
            level: scope === 'level' ? 'Grade 12' : undefined,
          };
          const result = createAcademicRequirementSchema.safeParse(payload);
          expect(result.success).toBe(true);
        }
      }
    });

    it('enforces conditional validation: learningUnitId required for learning_unit scope', () => {
      // Missing learningUnitId
      const missingUnitResult = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        requirementScope: 'learning_unit',
        learningUnitId: undefined,
      });
      expect(missingUnitResult.success).toBe(false);
      if (!missingUnitResult.success) {
        const errors = missingUnitResult.error.flatten().fieldErrors;
        expect(errors.learningUnitId).toContain(
          "Learning unit ID is required when requirement scope is 'learning_unit'.",
        );
      }

      // Empty/whitespace learningUnitId
      const emptyUnitResult = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        requirementScope: 'learning_unit',
        learningUnitId: '   ',
      });
      expect(emptyUnitResult.success).toBe(false);
      if (!emptyUnitResult.success) {
        const errors = emptyUnitResult.error.flatten().fieldErrors;
        expect(errors.learningUnitId).toContain(
          "Learning unit ID is required when requirement scope is 'learning_unit'.",
        );
      }

      // Valid when supplied
      const validUnitResult = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        requirementScope: 'learning_unit',
        learningUnitId: '00000000-0000-0000-0000-000000000010',
      });
      expect(validUnitResult.success).toBe(true);
    });

    it('enforces conditional validation: level required for level scope', () => {
      // Missing level
      const missingLevelResult = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        requirementScope: 'level',
        level: undefined,
      });
      expect(missingLevelResult.success).toBe(false);
      if (!missingLevelResult.success) {
        const errors = missingLevelResult.error.flatten().fieldErrors;
        expect(errors.level).toContain("Level is required when requirement scope is 'level'.");
      }

      // Empty/whitespace level
      const emptyLevelResult = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        requirementScope: 'level',
        level: '   ',
      });
      expect(emptyLevelResult.success).toBe(false);
      if (!emptyLevelResult.success) {
        const errors = emptyLevelResult.error.flatten().fieldErrors;
        expect(errors.level).toContain("Level is required when requirement scope is 'level'.");
      }

      // Valid when supplied
      const validLevelResult = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        requirementScope: 'level',
        level: 'Grade 11',
      });
      expect(validLevelResult.success).toBe(true);
    });

    it('fails when endDate is strictly before effectiveDate', () => {
      const result = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        effectiveDate: '2024-07-01',
        endDate: '2024-06-30',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.endDate).toContain(
          'End date must be greater than or equal to effective date.',
        );
      }
    });

    it('fails when required fields (name, description, version, effectiveDate) are missing', () => {
      const result = createAcademicRequirementSchema.safeParse({});
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.name).toBeDefined();
        expect(errors.requirementScope).toBeDefined();
        expect(errors.requirementType).toBeDefined();
        expect(errors.description).toBeDefined();
        expect(errors.version).toBeDefined();
        expect(errors.effectiveDate).toBeDefined();
      }
    });

    it('fails when string lengths exceed constraints', () => {
      const result = createAcademicRequirementSchema.safeParse({
        ...validProgramReq,
        name: 'n'.repeat(201),
        description: 'd'.repeat(4001),
        version: 'v'.repeat(51),
        notes: 'n'.repeat(4001),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.name).toBeDefined();
        expect(errors.description).toBeDefined();
        expect(errors.version).toBeDefined();
        expect(errors.notes).toBeDefined();
      }
    });
  });

  describe('updateAcademicRequirementSchema', () => {
    it('passes for a valid update payload', () => {
      const result = updateAcademicRequirementSchema.safeParse({
        name: 'Revised Prerequisite Policy',
        description: 'Students must achieve minimum score of 75 in prerequisite module.',
        notes: 'Updated according to 2026 academic standards',
      });
      expect(result.success).toBe(true);
    });

    it('fails when name or description is empty', () => {
      expect(
        updateAcademicRequirementSchema.safeParse({
          name: '',
          description: 'Valid description',
        }).success,
      ).toBe(false);

      expect(
        updateAcademicRequirementSchema.safeParse({
          name: 'Valid Name',
          description: '',
        }).success,
      ).toBe(false);
    });

    it('fails when endDate is strictly before effectiveDate', () => {
      const result = updateAcademicRequirementSchema.safeParse({
        name: 'Requirement',
        description: 'Valid description',
        effectiveDate: '2025-01-01',
        endDate: '2024-12-31',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const errors = result.error.flatten().fieldErrors;
        expect(errors.endDate).toContain(
          'End date must be greater than or equal to effective date.',
        );
      }
    });

    it('passes when effectiveDate equals endDate or endDate is omitted', () => {
      expect(
        updateAcademicRequirementSchema.safeParse({
          name: 'Requirement',
          description: 'Valid description',
          effectiveDate: '2025-01-01',
          endDate: '2025-01-01',
        }).success,
      ).toBe(true);

      expect(
        updateAcademicRequirementSchema.safeParse({
          name: 'Requirement',
          description: 'Valid description',
          effectiveDate: '2025-01-01',
        }).success,
      ).toBe(true);
    });
  });
});
