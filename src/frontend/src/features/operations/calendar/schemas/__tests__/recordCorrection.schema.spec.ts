import { describe, it, expect } from 'vitest';
import { recordCorrectionSchema } from '../recordCorrection.schema';

describe('recordCorrection.schema', () => {
  const validCorrection = {
    targetTableName: 'academic_years' as const,
    targetRecordId: '33333333-3333-4333-8333-333333333333',
    fieldName: 'name',
    newValue: 'Academic Year 2026/2027 Official',
    justificationReason: 'Correcting typographic oversight approved by rectorate.',
    authorizedBy: 'Registrar Authority',
  };

  it('validates a correct record correction payload', () => {
    const result = recordCorrectionSchema.safeParse(validCorrection);
    expect(result.success).toBe(true);
  });

  it('fails when targetTableName is not academic_years or operational_periods', () => {
    const result = recordCorrectionSchema.safeParse({
      ...validCorrection,
      targetTableName: 'users',
    });
    expect(result.success).toBe(false);
  });

  it('fails when targetRecordId is not a UUID', () => {
    const result = recordCorrectionSchema.safeParse({
      ...validCorrection,
      targetRecordId: 'abc-123',
    });
    expect(result.success).toBe(false);
  });

  it('fails when justificationReason is shorter than 5 characters', () => {
    const result = recordCorrectionSchema.safeParse({
      ...validCorrection,
      justificationReason: 'Fix',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        'Justification reason must be at least 5 characters.',
      );
    }
  });
});
