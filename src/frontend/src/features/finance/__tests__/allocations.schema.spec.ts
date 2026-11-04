import { describe, it, expect } from 'vitest';
import {
  allocatePaymentSchema,
  reallocatePaymentSchema,
  suggestAllocationsParamsSchema,
  allocatePaymentTypedSchema,
  reallocatePaymentTypedSchema,
  suggestAllocationsTypedSchema,
} from '../allocations/schemas/allocations.schema';

describe('allocations.schema.spec.ts', () => {
  describe('allocatePaymentSchema', () => {
    it('validates a valid allocation payload', () => {
      const valid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        allocations: [
          {
            obligationId: '22222222-2222-2222-2222-222222222222',
            amount: 2500000,
            notes: 'Tuition installment 1',
          },
          {
            obligationId: '33333333-3333-3333-3333-333333333333',
            amount: 2500000,
          },
        ],
        notes: 'Full allocation',
      };

      const result = allocatePaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects allocation payload with empty allocations list', () => {
      const invalid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        allocations: [],
      };

      const result = allocatePaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects allocation line with negative or zero amount', () => {
      const invalid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        allocations: [
          {
            obligationId: '22222222-2222-2222-2222-222222222222',
            amount: 0,
          },
        ],
      };

      const result = allocatePaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects allocation line with non-integer amount', () => {
      const invalid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        allocations: [
          {
            obligationId: '22222222-2222-2222-2222-222222222222',
            amount: 2500000.5,
          },
        ],
      };

      expect(allocatePaymentSchema.safeParse(invalid).success).toBe(false);
    });

    it('rejects invalid UUID paymentId or obligationId', () => {
      expect(
        allocatePaymentSchema.safeParse({
          paymentId: 'invalid-uuid',
          allocations: [
            {
              obligationId: '22222222-2222-2222-2222-222222222222',
              amount: 100000,
            },
          ],
        }).success,
      ).toBe(false);

      expect(
        allocatePaymentSchema.safeParse({
          paymentId: '11111111-1111-1111-1111-111111111111',
          allocations: [
            {
              obligationId: 'not-a-uuid',
              amount: 100000,
            },
          ],
        }).success,
      ).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(allocatePaymentTypedSchema).toBeDefined();
    });
  });

  describe('reallocatePaymentSchema', () => {
    it('validates a valid reallocation payload with replacement allocations', () => {
      const valid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        priorAllocationIds: ['22222222-2222-2222-2222-222222222222'],
        replacementAllocations: [
          {
            obligationId: '33333333-3333-3333-3333-333333333333',
            amount: 5000000,
          },
        ],
        reversalReason: 'Wrong fee obligation originally selected by cashier',
      };

      const result = reallocatePaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates a pure reversal without replacement allocations', () => {
      const valid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        priorAllocationIds: ['22222222-2222-2222-2222-222222222222'],
        reversalReason: 'Reversing allocation to credit balance',
      };

      const result = reallocatePaymentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects reallocation without mandatory reversal reason', () => {
      const invalid = {
        paymentId: '11111111-1111-1111-1111-111111111111',
        priorAllocationIds: ['22222222-2222-2222-2222-222222222222'],
        reversalReason: '',
      };

      const result = reallocatePaymentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Reversal reason is mandatory');
      }
    });

    it('rejects empty or invalid priorAllocationIds', () => {
      expect(
        reallocatePaymentSchema.safeParse({
          paymentId: '11111111-1111-1111-1111-111111111111',
          priorAllocationIds: [],
          reversalReason: 'Test reason',
        }).success,
      ).toBe(false);

      expect(
        reallocatePaymentSchema.safeParse({
          paymentId: '11111111-1111-1111-1111-111111111111',
          priorAllocationIds: ['not-a-uuid'],
          reversalReason: 'Test reason',
        }).success,
      ).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(reallocatePaymentTypedSchema).toBeDefined();
    });
  });

  describe('suggestAllocationsParamsSchema', () => {
    it('validates suggest params with studentId and positive amount', () => {
      const valid = {
        studentId: '11111111-1111-1111-1111-111111111111',
        paymentAmount: 5000000,
      };

      const result = suggestAllocationsParamsSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates suggest params with applicantId and positive amount', () => {
      const valid = {
        applicantId: '22222222-2222-2222-2222-222222222222',
        paymentAmount: 15000000,
      };

      const result = suggestAllocationsParamsSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects suggest params without studentId or applicantId', () => {
      const invalid = {
        paymentAmount: 5000000,
      };

      const result = suggestAllocationsParamsSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects non-positive or non-integer paymentAmount', () => {
      expect(
        suggestAllocationsParamsSchema.safeParse({
          studentId: '11111111-1111-1111-1111-111111111111',
          paymentAmount: 0,
        }).success,
      ).toBe(false);

      expect(
        suggestAllocationsParamsSchema.safeParse({
          studentId: '11111111-1111-1111-1111-111111111111',
          paymentAmount: -500,
        }).success,
      ).toBe(false);

      expect(
        suggestAllocationsParamsSchema.safeParse({
          studentId: '11111111-1111-1111-1111-111111111111',
          paymentAmount: 5000.5,
        }).success,
      ).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(suggestAllocationsTypedSchema).toBeDefined();
    });
  });
});
