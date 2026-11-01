import { describe, it, expect } from 'vitest';
import { createAcademicYearSchema } from '../createAcademicYear.schema';

describe('createAcademicYearSchema', () => {
  const validData = {
    code: 'AY-2026-2027',
    name: 'Academic Year 2026/2027',
    startDate: '2026-07-01',
    endDate: '2027-06-30',
  };

  it('validates a correct academic year payload', () => {
    const result = createAcademicYearSchema.safeParse(validData);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data).toEqual(validData);
    }
  });

  describe('code validation', () => {
    it('fails when code is missing or empty', () => {
      const result = createAcademicYearSchema.safeParse({ ...validData, code: '' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Code is required.');
      }
    });

    it('fails when code is shorter than 3 characters', () => {
      const result = createAcademicYearSchema.safeParse({ ...validData, code: 'AY' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Code must be at least 3 characters.');
      }
    });

    it('fails when code exceeds 50 characters', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        code: 'A'.repeat(51),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Code cannot exceed 50 characters.');
      }
    });
  });

  describe('name validation', () => {
    it('fails when name is missing or empty', () => {
      const result = createAcademicYearSchema.safeParse({ ...validData, name: '' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Name is required.');
      }
    });

    it('fails when name is shorter than 3 characters', () => {
      const result = createAcademicYearSchema.safeParse({ ...validData, name: 'AY' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Name must be at least 3 characters.');
      }
    });

    it('fails when name exceeds 100 characters', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        name: 'N'.repeat(101),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Name cannot exceed 100 characters.');
      }
    });
  });

  describe('date range validation', () => {
    it('fails when startDate has invalid format', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        startDate: '2026/07/01',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Start date must be in YYYY-MM-DD format.');
      }
    });

    it('fails when endDate has invalid format', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        endDate: '01-07-2027',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('End date must be in YYYY-MM-DD format.');
      }
    });

    it('fails when endDate is identical to startDate', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        startDate: '2026-07-01',
        endDate: '2026-07-01',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.issues.some(
            (i) => i.message === 'End date must be greater than start date.',
          ),
        ).toBe(true);
      }
    });

    it('fails when endDate is strictly before startDate', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        startDate: '2027-01-01',
        endDate: '2026-01-01',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const issue = result.error.issues.find((i) => i.path.includes('endDate'));
        expect(issue?.message).toBe('End date must be greater than start date.');
      }
    });
  });
});
