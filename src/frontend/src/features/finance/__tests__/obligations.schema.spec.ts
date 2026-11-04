import { describe, it, expect } from 'vitest';
import {
  batchAssessObligationsSchema,
  assessAdHocObligationSchema,
} from '../obligations/schemas/obligations.schema';

describe('obligations.schema.spec.ts', () => {
  describe('batchAssessObligationsSchema', () => {
    it('validates a valid preview and commit payload', () => {
      const validPreview = {
        chargeRuleId: '11111111-1111-1111-1111-111111111111',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
        billingCycleLabel: 'Billing Cycle SPP Agustus 2026',
        targetCampus: 'Main Campus',
        targetGrade: 'Grade 10',
        mode: 'preview' as const,
      };

      const validCommit = {
        ...validPreview,
        mode: 'commit' as const,
      };

      expect(batchAssessObligationsSchema.safeParse(validPreview).success).toBe(true);
      expect(batchAssessObligationsSchema.safeParse(validCommit).success).toBe(true);
    });

    it('rejects invalid mode or malformed UUID', () => {
      const invalid = {
        chargeRuleId: 'not-a-uuid',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
        billingCycleLabel: 'Cycle Label',
        mode: 'execute',
      };

      const result = batchAssessObligationsSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const issues = result.error.issues;
        expect(issues.some((i) => i.path.includes('chargeRuleId'))).toBe(true);
        expect(issues.some((i) => i.path.includes('mode'))).toBe(true);
      }
    });

    it('rejects empty billingCycleLabel', () => {
      const invalid = {
        chargeRuleId: '11111111-1111-1111-1111-111111111111',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
        billingCycleLabel: '   ',
        mode: 'preview',
      };

      const result = batchAssessObligationsSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Billing cycle label is required');
      }
    });

    it('allows optional targetCampus and targetGrade to be null, empty, or omitted', () => {
      const minimal = {
        chargeRuleId: '11111111-1111-1111-1111-111111111111',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
        billingCycleLabel: 'Cycle 1',
        mode: 'preview',
      };

      expect(batchAssessObligationsSchema.safeParse(minimal).success).toBe(true);
      expect(
        batchAssessObligationsSchema.safeParse({ ...minimal, targetCampus: '', targetGrade: null }).success,
      ).toBe(true);
    });
  });

  describe('assessAdHocObligationSchema', () => {
    it('validates a valid ad-hoc charge assessment payload', () => {
      const valid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        chargeType: 'library_fine',
        description: 'Denda Keterlambatan Buku Perpustakaan',
        amount: 25000,
        dueDate: '2026-08-15',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
        notes: 'Catatan tambahan',
        policyId: '33333333-3333-3333-3333-333333333333',
      };

      const result = assessAdHocObligationSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects zero or negative amount', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        chargeType: 'library_fine',
        description: 'Denda',
        amount: 0,
        dueDate: '2026-08-15',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
      };

      const result = assessAdHocObligationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('greater than zero');
      }

      const negativeResult = assessAdHocObligationSchema.safeParse({
        ...invalid,
        amount: -5000,
      });
      expect(negativeResult.success).toBe(false);
    });

    it('rejects non-integer amount', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        chargeType: 'fine',
        description: 'Fee',
        amount: 15000.75,
        dueDate: '2026-08-15',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
      };

      const result = assessAdHocObligationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('integer IDR value');
      }
    });

    it('rejects invalid dueDate format', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        chargeType: 'fine',
        description: 'Fee',
        amount: 50000,
        dueDate: '15/08/2026', // non-ISO format
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
      };

      const result = assessAdHocObligationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('YYYY-MM-DD');
      }
    });

    it('rejects invalid studentId or academicPeriodId UUID', () => {
      const invalid = {
        studentId: 'not-a-uuid',
        chargeType: 'fine',
        description: 'Fee',
        amount: 50000,
        dueDate: '2026-08-15',
        academicPeriodId: 'also-not-a-uuid',
      };

      const result = assessAdHocObligationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const fields = result.error.issues.map((i) => i.path[0]);
        expect(fields).toContain('studentId');
        expect(fields).toContain('academicPeriodId');
      }
    });
  });
});
