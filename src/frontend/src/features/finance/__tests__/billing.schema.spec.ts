import { describe, it, expect } from 'vitest';
import {
  generateBillSchema,
  generateStatementSchema,
} from '../billing/schemas/billing.schema';

describe('billing.schema.spec.ts', () => {
  describe('generateBillSchema', () => {
    it('validates a valid bill generation payload with optional obligation IDs', () => {
      const valid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        obligationIds: ['22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333'],
        paymentDueDate: '2026-08-10',
        notes: 'Tagihan SPP dan Uang Gedung',
      };

      const result = generateBillSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates payload without obligationIds and paymentDueDate', () => {
      const minimal = {
        studentId: '11111111-1111-1111-1111-111111111111',
      };

      const result = generateBillSchema.safeParse(minimal);
      expect(result.success).toBe(true);
    });

    it('rejects invalid studentId UUID format', () => {
      const invalid = {
        studentId: 'not-a-valid-uuid',
      };

      const result = generateBillSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Invalid student ID format');
      }
    });

    it('rejects invalid obligation ID in array', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        obligationIds: ['invalid-uuid'],
      };

      const result = generateBillSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Invalid obligation ID format');
      }
    });

    it('rejects invalid paymentDueDate format', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        paymentDueDate: '2026/08/10',
      };

      const result = generateBillSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('YYYY-MM-DD');
      }
    });

    it('rejects notes exceeding 2000 characters', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        notes: 'A'.repeat(2001),
      };

      const result = generateBillSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('cannot exceed 2000 characters');
      }
    });
  });

  describe('generateStatementSchema', () => {
    it('validates generateStatementSchema across valid date range', () => {
      const valid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        billingPeriodStart: '2026-07-01',
        billingPeriodEnd: '2026-07-31',
        notes: 'Laporan Rekening Koran',
      };

      const result = generateStatementSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates generateStatementSchema when start and end dates are the same', () => {
      const sameDay = {
        studentId: '11111111-1111-1111-1111-111111111111',
        billingPeriodStart: '2026-07-01',
        billingPeriodEnd: '2026-07-01',
      };

      const result = generateStatementSchema.safeParse(sameDay);
      expect(result.success).toBe(true);
    });

    it('rejects statement where start date is after end date', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        billingPeriodStart: '2026-08-01',
        billingPeriodEnd: '2026-07-31',
      };

      const result = generateStatementSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('greater than or equal to start date');
      }
    });

    it('rejects invalid date format', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        billingPeriodStart: '01-07-2026',
        billingPeriodEnd: '2026-07-31',
      };

      const result = generateStatementSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('YYYY-MM-DD');
      }
    });

    it('rejects missing studentId', () => {
      const invalid = {
        billingPeriodStart: '2026-07-01',
        billingPeriodEnd: '2026-07-31',
      };

      const result = generateStatementSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });
  });
});
