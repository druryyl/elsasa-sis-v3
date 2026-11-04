import { describe, it, expect } from 'vitest';
import {
  submitPaymentSchema,
  recordPaymentSchema,
  confirmPaymentSchema,
  rejectPaymentSchema,
  getPaymentsParamsSchema,
  submitPaymentTypedSchema,
  recordPaymentTypedSchema,
  confirmPaymentTypedSchema,
  rejectPaymentTypedSchema,
} from '../payments/schemas/payments.schema';

describe('payments.schema.spec.ts', () => {
  describe('submitPaymentSchema', () => {
    it('validates a valid student payment submission payload', () => {
      const valid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 5000000,
        paymentMethod: 'bank_transfer',
        paymentDate: '2026-11-04',
        externalReference: 'TRX-BCA-987654321',
        payerName: 'Budi Santoso',
        payerEmail: 'budi.santoso@example.com',
        payerPhone: '+62 812 3456 7890',
        notes: 'Monthly tuition November',
      };

      const result = submitPaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates an applicant payment submission without studentId', () => {
      const valid = {
        applicantId: '22222222-2222-2222-2222-222222222222',
        amount: 15000000,
        paymentMethod: 'bank_transfer',
        payerName: 'Siti Aminah',
      };

      const result = submitPaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects submission with neither studentId, applicantId, nor studentAccountId', () => {
      const invalid = {
        amount: 5000000,
        paymentMethod: 'bank_transfer',
        payerName: 'Budi Santoso',
      };

      const result = submitPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('At least one candidate identifier');
      }
    });

    it('rejects zero or negative amount', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 0,
        paymentMethod: 'bank_transfer',
        payerName: 'Budi Santoso',
      };

      const result = submitPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects non-integer amount', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 5000000.5,
        paymentMethod: 'bank_transfer',
        payerName: 'Budi Santoso',
      };

      const result = submitPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('validates a studentAccountId submission without studentId or applicantId', () => {
      const valid = {
        studentAccountId: '33333333-3333-3333-3333-333333333333',
        amount: 3000000,
        paymentMethod: 'pos_card',
        payerName: 'Ratna Dewi',
      };

      const result = submitPaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects invalid email format', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 5000000,
        paymentMethod: 'bank_transfer',
        payerName: 'Budi Santoso',
        payerEmail: 'not-an-email',
      };

      const result = submitPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects invalid phone format', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 5000000,
        paymentMethod: 'bank_transfer',
        payerName: 'Budi Santoso',
        payerPhone: '123', // less than 7 chars
      };

      const result = submitPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects invalid paymentDate format', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 5000000,
        paymentMethod: 'bank_transfer',
        payerName: 'Budi Santoso',
        paymentDate: '04-11-2026', // Not YYYY-MM-DD
      };

      const result = submitPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(submitPaymentTypedSchema).toBeDefined();
    });
  });

  describe('recordPaymentSchema', () => {
    it('validates valid cashier counter recording payload', () => {
      const valid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 750000,
        paymentMethod: 'cash',
        payerName: 'Walk-in Parent',
      };

      const result = recordPaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects counter recording with neither studentId, applicantId, nor studentAccountId', () => {
      const invalid = {
        amount: 750000,
        paymentMethod: 'cash',
        payerName: 'Walk-in Parent',
      };

      const result = recordPaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects zero or negative amount', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 0,
        paymentMethod: 'cash',
        payerName: 'Walk-in Parent',
      };

      expect(recordPaymentSchema.safeParse(invalid).success).toBe(false);

      const negative = {
        ...invalid,
        amount: -100000,
      };
      expect(recordPaymentSchema.safeParse(negative).success).toBe(false);
    });

    it('rejects non-integer amount', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 750000.75,
        paymentMethod: 'cash',
        payerName: 'Walk-in Parent',
      };

      expect(recordPaymentSchema.safeParse(invalid).success).toBe(false);
    });

    it('rejects missing paymentMethod', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 750000,
        paymentMethod: '',
        payerName: 'Walk-in Parent',
      };

      expect(recordPaymentSchema.safeParse(invalid).success).toBe(false);
    });

    it('rejects missing payerName', () => {
      const invalid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        amount: 750000,
        paymentMethod: 'cash',
        payerName: '',
      };

      expect(recordPaymentSchema.safeParse(invalid).success).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(recordPaymentTypedSchema).toBeDefined();
    });
  });

  describe('confirmPaymentSchema', () => {
    it('validates valid UUID paymentId', () => {
      const valid = { paymentId: '11111111-1111-1111-1111-111111111111' };
      expect(confirmPaymentSchema.safeParse(valid).success).toBe(true);
    });

    it('rejects invalid or empty paymentId', () => {
      expect(confirmPaymentSchema.safeParse({ paymentId: 'invalid-id' }).success).toBe(false);
      expect(confirmPaymentSchema.safeParse({ paymentId: '' }).success).toBe(false);
      expect(confirmPaymentSchema.safeParse({}).success).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(confirmPaymentTypedSchema).toBeDefined();
    });
  });

  describe('rejectPaymentSchema', () => {
    it('validates mandatory rejection reason', () => {
      const valid = {
        rejectionReason: 'Invalid bank reference code provided',
        notes: 'Called parent to re-upload clear slip',
      };
      expect(rejectPaymentSchema.safeParse(valid).success).toBe(true);
    });

    it('rejects empty or whitespace rejection reason', () => {
      expect(rejectPaymentSchema.safeParse({ rejectionReason: '' }).success).toBe(false);
      expect(rejectPaymentSchema.safeParse({ rejectionReason: '   ' }).success).toBe(false);
      expect(rejectPaymentSchema.safeParse({}).success).toBe(false);
    });

    it('rejects rejection reason exceeding 1000 characters', () => {
      const invalid = {
        rejectionReason: 'x'.repeat(1001),
      };
      expect(rejectPaymentSchema.safeParse(invalid).success).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(rejectPaymentTypedSchema).toBeDefined();
    });
  });

  describe('getPaymentsParamsSchema', () => {
    it('validates payments list query parameters with defaults', () => {
      const result = getPaymentsParamsSchema.safeParse({});
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.page).toBe(1);
        expect(result.data.pageSize).toBe(20);
      }
    });

    it('validates payments list query parameters with custom filters', () => {
      const valid = {
        status: 'confirmed',
        studentId: '11111111-1111-1111-1111-111111111111',
        startDate: '2026-11-01',
        endDate: '2026-11-30',
        searchTerm: 'Budi',
        page: 2,
        pageSize: 50,
      };

      const result = getPaymentsParamsSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.status).toBe('confirmed');
        expect(result.data.page).toBe(2);
        expect(result.data.pageSize).toBe(50);
      }
    });

    it('rejects page less than 1 or pageSize greater than 100', () => {
      expect(getPaymentsParamsSchema.safeParse({ page: 0 }).success).toBe(false);
      expect(getPaymentsParamsSchema.safeParse({ pageSize: 101 }).success).toBe(false);
    });
  });
});
