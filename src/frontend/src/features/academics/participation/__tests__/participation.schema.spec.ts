// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  batchRegisterCohortSchema,
  registerParticipationSchema,
  recognizePriorCreditSchema,
  updateParticipationStatusSchema,
  concludeParticipationSchema,
  PARTICIPATION_TYPES,
  PARTICIPATION_SOURCES,
  PARTICIPATION_STATUSES,
} from '../schemas/participation.schema';

describe('Academic Participation Zod Schemas (P9-S24)', () => {
  // ==========================================================================
  // 1. batchRegisterCohortSchema
  // ==========================================================================
  describe('batchRegisterCohortSchema', () => {
    const validCohortPayload = {
      academicPeriodId: '00000000-0000-0000-0000-000000000001',
      academicPeriodLabel: '2026/2027 Odd Semester',
      learningUnitIds: ['11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222'],
      sectionLabel: 'Grade 10 - IPA 1',
      startDate: '2026-08-01',
      studentIds: ['33333333-3333-3333-3333-333333333333'],
    };

    it('passes for a fully populated valid payload', () => {
      const result = batchRegisterCohortSchema.safeParse(validCohortPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.academicPeriodId).toBe('00000000-0000-0000-0000-000000000001');
        expect(result.data.sectionLabel).toBe('Grade 10 - IPA 1');
        expect(result.data.learningUnitIds).toHaveLength(2);
      }
    });

    it('passes when optional studentIds array is omitted', () => {
      const { studentIds, ...minimalPayload } = validCohortPayload;
      const result = batchRegisterCohortSchema.safeParse(minimalPayload);
      expect(result.success).toBe(true);
    });

    it('rejects empty or whitespace academicPeriodId', () => {
      const payload = { ...validCohortPayload, academicPeriodId: '   ' };
      const result = batchRegisterCohortSchema.safeParse(payload);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Academic period is required');
      }
    });

    it('rejects empty academicPeriodLabel and label exceeding 100 characters', () => {
      const emptyResult = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        academicPeriodLabel: '',
      });
      expect(emptyResult.success).toBe(false);

      const tooLongResult = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        academicPeriodLabel: 'A'.repeat(101),
      });
      expect(tooLongResult.success).toBe(false);
      if (!tooLongResult.success) {
        expect(tooLongResult.error.issues[0].message).toContain('cannot exceed 100 characters');
      }
    });

    it('rejects empty learningUnitIds array', () => {
      const result = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        learningUnitIds: [],
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('At least one learning unit must be selected');
      }
    });

    it('rejects empty strings inside learningUnitIds array', () => {
      const result = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        learningUnitIds: ['   '],
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('cannot be empty');
      }
    });

    it('rejects empty sectionLabel and sectionLabel exceeding 50 characters', () => {
      const emptyResult = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        sectionLabel: '   ',
      });
      expect(emptyResult.success).toBe(false);

      const tooLongResult = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        sectionLabel: 'S'.repeat(51),
      });
      expect(tooLongResult.success).toBe(false);
      if (!tooLongResult.success) {
        expect(tooLongResult.error.issues[0].message).toContain('cannot exceed 50 characters');
      }
    });

    it('rejects non-ISO startDate format and invalid calendar date', () => {
      const nonIsoResult = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        startDate: '01/08/2026',
      });
      expect(nonIsoResult.success).toBe(false);

      const invalidDateResult = batchRegisterCohortSchema.safeParse({
        ...validCohortPayload,
        startDate: '2026-02-31',
      });
      expect(invalidDateResult.success).toBe(false);
    });
  });

  // ==========================================================================
  // 2. registerParticipationSchema
  // ==========================================================================
  describe('registerParticipationSchema', () => {
    const validSinglePayload = {
      studentId: '11111111-1111-1111-1111-111111111111',
      learningUnitId: '22222222-2222-2222-2222-222222222222',
      academicPeriodId: '33333333-3333-3333-3333-333333333333',
      academicPeriodLabel: '2026/2027 Odd Semester',
      sectionLabel: 'Section A',
      participationType: 'standard' as const,
      source: 'assigned' as const,
      startDate: '2026-08-01',
    };

    it('passes for a valid single participation payload', () => {
      const result = registerParticipationSchema.safeParse(validSinglePayload);
      expect(result.success).toBe(true);
    });

    it('passes when optional sectionLabel is null or undefined', () => {
      const resultNull = registerParticipationSchema.safeParse({
        ...validSinglePayload,
        sectionLabel: null,
      });
      expect(resultNull.success).toBe(true);

      const { sectionLabel, ...withoutSection } = validSinglePayload;
      const resultUndefined = registerParticipationSchema.safeParse(withoutSection);
      expect(resultUndefined.success).toBe(true);
    });

    it('accepts all valid PARTICIPATION_TYPES and PARTICIPATION_SOURCES literals', () => {
      for (const type of PARTICIPATION_TYPES) {
        for (const src of PARTICIPATION_SOURCES) {
          const result = registerParticipationSchema.safeParse({
            ...validSinglePayload,
            participationType: type,
            source: src,
          });
          expect(result.success).toBe(true);
        }
      }
    });

    it('rejects invalid participationType and source literals', () => {
      const invalidTypeResult = registerParticipationSchema.safeParse({
        ...validSinglePayload,
        participationType: 'invalid_type',
      });
      expect(invalidTypeResult.success).toBe(false);

      const invalidSourceResult = registerParticipationSchema.safeParse({
        ...validSinglePayload,
        source: 'unauthorized_source',
      });
      expect(invalidSourceResult.success).toBe(false);
    });

    it('rejects missing or empty studentId and learningUnitId', () => {
      const noStudent = registerParticipationSchema.safeParse({
        ...validSinglePayload,
        studentId: '',
      });
      expect(noStudent.success).toBe(false);

      const noUnit = registerParticipationSchema.safeParse({
        ...validSinglePayload,
        learningUnitId: '   ',
      });
      expect(noUnit.success).toBe(false);
    });
  });

  // ==========================================================================
  // 3. recognizePriorCreditSchema
  // ==========================================================================
  describe('recognizePriorCreditSchema', () => {
    const validPriorPayload = {
      studentId: '11111111-1111-1111-1111-111111111111',
      learningUnitId: '22222222-2222-2222-2222-222222222222',
      academicPeriodId: '33333333-3333-3333-3333-333333333333',
      academicPeriodLabel: '2026/2027 Odd Semester',
      participationType: 'transferred_in' as const,
      sourceInstitution: 'SMA Negeri 1 Jakarta',
      externalGradeOrCredit: 'Grade A (4.00)',
      recognitionBasis: 'Curriculum equivalency approved by Academic Senate',
      startDate: '2026-01-10',
      completionDate: '2026-06-20',
      studentLearningPlanId: 'plan-123',
      notes: 'Transfer verified by Admissions office',
    };

    it('passes for valid transferred_in and recognized_prior payloads', () => {
      const resultTransfer = recognizePriorCreditSchema.safeParse(validPriorPayload);
      expect(resultTransfer.success).toBe(true);

      const resultPrior = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        participationType: 'recognized_prior',
      });
      expect(resultPrior.success).toBe(true);
    });

    it('rejects participationType other than transferred_in or recognized_prior', () => {
      const result = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        participationType: 'standard',
      });
      expect(result.success).toBe(false);
    });

    it('passes when completionDate equals startDate', () => {
      const result = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        startDate: '2026-06-20',
        completionDate: '2026-06-20',
      });
      expect(result.success).toBe(true);
    });

    it('rejects when completionDate is before startDate', () => {
      const result = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        startDate: '2026-06-20',
        completionDate: '2026-01-10',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Completion date must be on or after the start date');
      }
    });

    it('enforces string length constraints on optional fields', () => {
      const tooLongInstitution = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        sourceInstitution: 'I'.repeat(201),
      });
      expect(tooLongInstitution.success).toBe(false);

      const tooLongGrade = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        externalGradeOrCredit: 'G'.repeat(101),
      });
      expect(tooLongGrade.success).toBe(false);

      const tooLongBasis = recognizePriorCreditSchema.safeParse({
        ...validPriorPayload,
        recognitionBasis: 'B'.repeat(2001),
      });
      expect(tooLongBasis.success).toBe(false);
    });
  });

  // ==========================================================================
  // 4. updateParticipationStatusSchema & concludeParticipationSchema
  // ==========================================================================
  describe('updateParticipationStatusSchema', () => {
    it('passes for allowed non-terminal statuses', () => {
      for (const status of ['in_progress', 'paused', 'withdrawn'] as const) {
        const result = updateParticipationStatusSchema.safeParse({
          newStatus: status,
          notes: 'Status update rationale',
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects invalid status or terminal completed status', () => {
      const result = updateParticipationStatusSchema.safeParse({
        newStatus: 'completed',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('concludeParticipationSchema', () => {
    it('passes for valid completionDate and notes', () => {
      const result = concludeParticipationSchema.safeParse({
        completionDate: '2026-11-01',
        notes: 'Passed all exams and assessments',
      });
      expect(result.success).toBe(true);
    });

    it('rejects invalid calendar date for completionDate', () => {
      const result = concludeParticipationSchema.safeParse({
        completionDate: 'not-a-date',
      });
      expect(result.success).toBe(false);
    });
  });
});
