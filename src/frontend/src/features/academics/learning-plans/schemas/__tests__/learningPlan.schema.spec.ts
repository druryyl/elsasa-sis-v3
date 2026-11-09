import { describe, it, expect } from 'vitest';
import {
  createPlanSchema,
  addUnitSchema,
  addAdjustmentSchema,
  approvePlanSchema,
  revisePlanSchema,
} from '../learningPlan.schema';

describe('Student Learning Plan Zod Schemas (P6-S17)', () => {
  const validUuid = '11111111-1111-1111-1111-111111111111';
  const validPeriodUuid = '22222222-2222-2222-2222-222222222222';
  const validUnitUuid = '33333333-3333-3333-3333-333333333333';

  describe('createPlanSchema', () => {
    it('validates a valid create plan payload with studentId and periodId', () => {
      const result = createPlanSchema.safeParse({
        studentId: validUuid,
        periodId: validPeriodUuid,
        academicPeriodLabel: 'Academic Year 2026/2027 Odd Term',
        notes: 'Individualized remediation pathway',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.studentId).toBe(validUuid);
        expect(result.data.periodId).toBe(validPeriodUuid);
      }
    });

    it('rejects missing studentId', () => {
      const result = createPlanSchema.safeParse({
        periodId: validPeriodUuid,
      });
      expect(result.success).toBe(false);
    });

    it('rejects invalid studentId UUID', () => {
      const result = createPlanSchema.safeParse({
        studentId: 'not-a-uuid',
        periodId: validPeriodUuid,
      });
      expect(result.success).toBe(false);
    });

    it('rejects missing periodId', () => {
      const result = createPlanSchema.safeParse({
        studentId: validUuid,
      });
      expect(result.success).toBe(false);
    });

    it('rejects invalid periodId UUID', () => {
      const result = createPlanSchema.safeParse({
        studentId: validUuid,
        periodId: 'invalid-uuid',
      });
      expect(result.success).toBe(false);
    });
    it('rejects notes exceeding 2000 characters', () => {
      const result = createPlanSchema.safeParse({
        studentId: validUuid,
        periodId: validPeriodUuid,
        notes: 'a'.repeat(2001),
      });
      expect(result.success).toBe(false);
    });

    it('rejects academicPeriodLabel exceeding 100 characters', () => {
      const result = createPlanSchema.safeParse({
        studentId: validUuid,
        periodId: validPeriodUuid,
        academicPeriodLabel: 'a'.repeat(101),
      });
      expect(result.success).toBe(false);
    });

    it('accepts null or empty notes and optional academicPeriodId', () => {
      const result = createPlanSchema.safeParse({
        studentId: validUuid,
        periodId: validPeriodUuid,
        academicPeriodId: validPeriodUuid,
        notes: null,
      });
      expect(result.success).toBe(true);
    });
  });

  describe('addUnitSchema', () => {
    it('validates a valid learning unit payload', () => {
      const result = addUnitSchema.safeParse({
        learningUnitId: validUnitUuid,
        sequenceOrder: 1,
        isRequired: true,
        notes: 'Remedial Math at Grade 3 level',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.learningUnitId).toBe(validUnitUuid);
        expect(result.data.sequenceOrder).toBe(1);
        expect(result.data.isRequired).toBe(true);
      }
    });

    it('defaults isRequired to true when omitted', () => {
      const result = addUnitSchema.safeParse({
        learningUnitId: validUnitUuid,
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.isRequired).toBe(true);
      }
    });

    it('coerces string sequenceOrder to number', () => {
      const result = addUnitSchema.safeParse({
        learningUnitId: validUnitUuid,
        sequenceOrder: '2',
        isRequired: false,
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.sequenceOrder).toBe(2);
        expect(result.data.isRequired).toBe(false);
      }
    });

    it('rejects missing learningUnitId', () => {
      const result = addUnitSchema.safeParse({
        sequenceOrder: 1,
        isRequired: true,
      });
      expect(result.success).toBe(false);
    });

    it('rejects invalid learningUnitId UUID', () => {
      const result = addUnitSchema.safeParse({
        learningUnitId: 'invalid-uuid',
      });
      expect(result.success).toBe(false);
    });

    it('rejects non-positive sequenceOrder (zero or negative)', () => {
      const zeroResult = addUnitSchema.safeParse({
        learningUnitId: validUnitUuid,
        sequenceOrder: 0,
        isRequired: true,
      });
      expect(zeroResult.success).toBe(false);

      const negResult = addUnitSchema.safeParse({
        learningUnitId: validUnitUuid,
        sequenceOrder: -3,
        isRequired: true,
      });
      expect(negResult.success).toBe(false);
    });

    it('rejects notes exceeding 1000 characters', () => {
      const result = addUnitSchema.safeParse({
        learningUnitId: validUnitUuid,
        notes: 'a'.repeat(1001),
      });
      expect(result.success).toBe(false);
    });
  });

  describe('addAdjustmentSchema', () => {
    it('validates a valid accommodation adjustment payload', () => {
      const result = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: '50% additional time on all written summative assessments',
        rationale: 'Formal psychological evaluation indicates processing delay requiring accommodation.',
        effectiveDate: '2026-08-01',
        endDate: '2026-12-15',
        notes: 'Review at mid-semester review session',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.adjustmentType).toBe('accommodation');
        expect(result.data.effectiveDate).toBe('2026-08-01');
      }
    });

    it('validates substitution and exemption adjustment types', () => {
      const subResult = addAdjustmentSchema.safeParse({
        adjustmentType: 'substitution',
        description: 'Substitute Physical Education with Adaptive Movement',
        rationale: 'Orthopedic specialist recommendation on file with school nurse.',
        effectiveDate: '2026-08-01',
      });
      expect(subResult.success).toBe(true);

      const exResult = addAdjustmentSchema.safeParse({
        adjustmentType: 'exemption',
        description: 'Exemption from foreign language requirement',
        rationale: 'Speech therapy documentation supported by academic authority.',
        effectiveDate: '2026-08-01',
      });
      expect(exResult.success).toBe(true);
    });

    it('rejects missing required fields (adjustmentType, description, rationale, effectiveDate)', () => {
      expect(
        addAdjustmentSchema.safeParse({
          description: 'Valid description',
          rationale: 'Valid rationale >= 10 chars',
          effectiveDate: '2026-08-01',
        }).success,
      ).toBe(false);

      expect(
        addAdjustmentSchema.safeParse({
          adjustmentType: 'accommodation',
          rationale: 'Valid rationale >= 10 chars',
          effectiveDate: '2026-08-01',
        }).success,
      ).toBe(false);

      expect(
        addAdjustmentSchema.safeParse({
          adjustmentType: 'accommodation',
          description: 'Valid description',
          effectiveDate: '2026-08-01',
        }).success,
      ).toBe(false);

      expect(
        addAdjustmentSchema.safeParse({
          adjustmentType: 'accommodation',
          description: 'Valid description',
          rationale: 'Valid rationale >= 10 chars',
        }).success,
      ).toBe(false);
    });

    it('rejects invalid adjustmentType', () => {
      const result = addAdjustmentSchema.safeParse({
        adjustmentType: 'unknown_type',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-01',
      });
      expect(result.success).toBe(false);
    });

    it('rejects empty description', () => {
      const result = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: '',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-01',
      });
      expect(result.success).toBe(false);
    });

    it('rejects rationale with fewer than 10 characters and accepts exactly 10 characters', () => {
      const tooShort = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: '123456789', // 9 chars
        effectiveDate: '2026-08-01',
      });
      expect(tooShort.success).toBe(false);

      const exactTen = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: '1234567890', // exactly 10 chars
        effectiveDate: '2026-08-01',
      });
      expect(exactTen.success).toBe(true);
    });

    it('rejects description exceeding 2000 characters and rationale exceeding 2000 characters', () => {
      const longDesc = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'a'.repeat(2001),
        rationale: 'Valid rationale >= 10 chars',
        effectiveDate: '2026-08-01',
      });
      expect(longDesc.success).toBe(false);

      const longRat = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Valid description',
        rationale: 'a'.repeat(2001),
        effectiveDate: '2026-08-01',
      });
      expect(longRat.success).toBe(false);
    });

    it('rejects invalid date formats', () => {
      const invalidDate = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026/08/01',
      });
      expect(invalidDate.success).toBe(false);

      const invalidEndDate = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-01',
        endDate: '01-12-2026',
      });
      expect(invalidEndDate.success).toBe(false);
    });

    it('validates date ordering: endDate strictly before effectiveDate fails', () => {
      const result = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-15',
        endDate: '2026-08-01',
      });
      expect(result.success).toBe(false);
    });

    it('validates date ordering: endDate equal to or after effectiveDate passes', () => {
      const sameDay = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-15',
        endDate: '2026-08-15',
      });
      expect(sameDay.success).toBe(true);

      const laterDay = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-15',
        endDate: '2026-12-15',
      });
      expect(laterDay.success).toBe(true);
    });

    it('accepts omitted, empty string or null endDate', () => {
      const omitted = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-15',
      });
      expect(omitted.success).toBe(true);

      const empty = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-15',
        endDate: '',
      });
      expect(empty.success).toBe(true);

      const nullEnd = addAdjustmentSchema.safeParse({
        adjustmentType: 'accommodation',
        description: 'Test description',
        rationale: 'Valid rationale with more than 10 characters',
        effectiveDate: '2026-08-15',
        endDate: null,
      });
      expect(nullEnd.success).toBe(true);
    });
  });

  describe('approvePlanSchema', () => {
    it('validates initial_approval', () => {
      const result = approvePlanSchema.safeParse({
        approvalType: 'initial_approval',
        notes: 'Formally approved by VP Academics',
      });
      expect(result.success).toBe(true);
    });

    it('validates revision_approval', () => {
      const result = approvePlanSchema.safeParse({
        approvalType: 'revision_approval',
        notes: 'Approved amended course load',
      });
      expect(result.success).toBe(true);
    });

    it('rejects invalid approvalType', () => {
      const result = approvePlanSchema.safeParse({
        approvalType: 'pending_approval',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('revisePlanSchema', () => {
    it('validates valid revision notes', () => {
      const result = revisePlanSchema.safeParse({
        notes: 'Student demonstrated mastery; adjusting learning units for next term.',
      });
      expect(result.success).toBe(true);
    });

    it('rejects empty revision notes', () => {
      const result = revisePlanSchema.safeParse({
        notes: '',
      });
      expect(result.success).toBe(false);
    });
  });
});
