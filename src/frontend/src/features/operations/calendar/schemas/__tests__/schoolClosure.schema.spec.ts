import { describe, it, expect } from 'vitest';
import { declareSchoolClosureSchema } from '../schoolClosure.schema';

describe('schoolClosure.schema', () => {
  const validClosure = {
    reasonCategory: 'weather_disaster' as const,
    reasonDetails: 'Severe flooding across primary access roads preventing safe transport.',
    startDate: '2026-11-10',
    endDate: '2026-11-12',
    affectedCampus: 'Main Campus',
    affectedPopulation: 'all' as const,
    authorizedBy: 'District Education Board',
    recoveryPlanNotes: 'Make-up classes scheduled for subsequent Saturdays.',
  };

  it('validates a correct closure declaration payload', () => {
    const result = declareSchoolClosureSchema.safeParse(validClosure);
    expect(result.success).toBe(true);
  });

  it('fails when reasonDetails is shorter than 5 characters', () => {
    const result = declareSchoolClosureSchema.safeParse({
      ...validClosure,
      reasonDetails: 'Rain',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe('Reason details must be at least 5 characters.');
    }
  });

  it('fails when endDate is before startDate', () => {
    const result = declareSchoolClosureSchema.safeParse({
      ...validClosure,
      startDate: '2026-11-12',
      endDate: '2026-11-10',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'End date must be greater than or equal to start date.')).toBe(true);
    }
  });

  it('fails on invalid reasonCategory', () => {
    const result = declareSchoolClosureSchema.safeParse({
      ...validClosure,
      reasonCategory: 'personal_holiday',
    });
    expect(result.success).toBe(false);
  });
});
