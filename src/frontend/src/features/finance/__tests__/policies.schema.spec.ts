import { describe, it, expect } from 'vitest';
import {
  createFinancialPolicySchema,
  createChargeRuleSchema,
  approveFinancialPolicySchema,
} from '../policies/schemas/policies.schema';

describe('policies.schema.spec.ts', () => {
  describe('createFinancialPolicySchema', () => {
    it('validates a correct policy draft payload', () => {
      const valid = {
        policyCode: 'FP-2026-001',
        name: 'Kebijakan Biaya Pendidikan TP 2026/2027',
        category: 'tuition',
        businessPurpose: 'Menetapkan standar biaya SPP dan operasional sekolah.',
        applicableContext: 'Seluruh siswa aktif',
        effectiveDate: '2026-07-01',
        endDate: '2027-06-30',
        policyVersion: '1.0',
      };

      const result = createFinancialPolicySchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.policyCode).toBe('FP-2026-001');
        expect(result.data.policyVersion).toBe('1.0');
      }
    });

    it('rejects when endDate is earlier than effectiveDate', () => {
      const invalid = {
        policyCode: 'FP-2026-001',
        name: 'Kebijakan Keuangan',
        category: 'tuition',
        businessPurpose: 'Standar SPP',
        effectiveDate: '2026-07-01',
        endDate: '2026-06-30',
      };

      const result = createFinancialPolicySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('End date must be greater than or equal');
      }
    });

    it('rejects invalid policy code format', () => {
      const invalid = {
        policyCode: 'FP 2026 001 with spaces!',
        name: 'Test',
        category: 'tuition',
        businessPurpose: 'Purpose',
        effectiveDate: '2026-07-01',
      };

      const result = createFinancialPolicySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('FP-YYYY-NNN');
      }
    });

    it('rejects missing required fields', () => {
      const invalid = {
        policyCode: 'FP-2026-001',
      };

      const result = createFinancialPolicySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const fields = result.error.issues.map((i) => i.path[0]);
        expect(fields).toContain('name');
        expect(fields).toContain('category');
        expect(fields).toContain('businessPurpose');
        expect(fields).toContain('effectiveDate');
      }
    });

    it('rejects policy name exceeding 200 characters', () => {
      const invalid = {
        policyCode: 'FP-2026-001',
        name: 'A'.repeat(201),
        category: 'tuition',
        businessPurpose: 'Purpose',
        effectiveDate: '2026-07-01',
      };

      const result = createFinancialPolicySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('cannot exceed 200 characters');
      }
    });
  });

  describe('createChargeRuleSchema', () => {
    it('validates a correct charge rule payload', () => {
      const valid = {
        ruleCode: 'CR-2026-SPP-SD',
        name: 'SPP Bulanan SD',
        chargeType: 'tuition',
        baseAmount: 750000,
        currencyCode: 'IDR',
        recurrencePattern: 'monthly',
        dueRuleType: 'fixed_day_of_month',
        dueRuleValue: '10',
        isMandatory: true,
      };

      const result = createChargeRuleSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.currencyCode).toBe('IDR');
        expect(result.data.baseAmount).toBe(750000);
      }
    });

    it('rejects negative base amount', () => {
      const invalid = {
        ruleCode: 'CR-2026-TEST',
        name: 'Test Rule',
        chargeType: 'tuition',
        baseAmount: -100,
        dueRuleType: 'days_after_issuance',
        dueRuleValue: '14',
        recurrencePattern: 'one_time',
      };

      const result = createChargeRuleSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Base amount cannot be negative');
      }
    });

    it('rejects non-integer base amount', () => {
      const invalid = {
        ruleCode: 'CR-2026-TEST',
        name: 'Test Rule',
        chargeType: 'tuition',
        baseAmount: 750000.5,
        dueRuleType: 'days_after_issuance',
        dueRuleValue: '14',
        recurrencePattern: 'one_time',
      };

      const result = createChargeRuleSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('must be an exact integer');
      }
    });

    it('rejects invalid recurrence pattern', () => {
      const invalid = {
        ruleCode: 'CR-2026-TEST',
        name: 'Test Rule',
        chargeType: 'tuition',
        baseAmount: 100000,
        dueRuleType: 'days_after_issuance',
        dueRuleValue: '14',
        recurrencePattern: 'biweekly', // not in enum
      };

      const result = createChargeRuleSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects invalid due rule type', () => {
      const invalid = {
        ruleCode: 'CR-2026-TEST',
        name: 'Test Rule',
        chargeType: 'tuition',
        baseAmount: 100000,
        dueRuleType: 'end_of_year', // not in enum
        dueRuleValue: '14',
        recurrencePattern: 'one_time',
      };

      const result = createChargeRuleSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('defaults currencyCode to IDR when omitted', () => {
      const payloadWithoutCurrency = {
        ruleCode: 'CR-2026-REG',
        name: 'Biaya Pendaftaran',
        chargeType: 'admission',
        baseAmount: 500000,
        recurrencePattern: 'one_time',
        dueRuleType: 'days_after_issuance',
        dueRuleValue: '7',
      };

      const result = createChargeRuleSchema.safeParse(payloadWithoutCurrency);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.currencyCode).toBe('IDR');
      }
    });
  });

  describe('approveFinancialPolicySchema', () => {
    it('accepts valid approval parameters or empty object', () => {
      expect(approveFinancialPolicySchema.safeParse({}).success).toBe(true);
      expect(
        approveFinancialPolicySchema.safeParse({
          approvingAuthority: 'Dewan Pembina Yayasan',
          notes: 'Disetujui rapat pleno',
        }).success,
      ).toBe(true);
    });

    it('rejects notes exceeding 1000 characters', () => {
      const result = approveFinancialPolicySchema.safeParse({
        notes: 'X'.repeat(1001),
      });
      expect(result.success).toBe(false);
    });
  });
});
