import { describe, it, expect } from 'vitest';
import {
  createAcademicYearSchema,
  updateAcademicYearSchema,
  approveAcademicYearSchema,
} from '../academicYear.schema';

describe('academicYear.schema', () => {
  describe('createAcademicYearSchema', () => {
    const validData = {
      code: 'AY-2026-2027',
      name: 'Academic Year 2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      notes: 'Initial policy draft',
    };

    it('validates a correct academic year payload', () => {
      const result = createAcademicYearSchema.safeParse(validData);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data).toEqual(validData);
      }
    });

    it('fails when code is missing or too short', () => {
      const result = createAcademicYearSchema.safeParse({ ...validData, code: 'AY' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Code must be at least 3 characters.');
      }
    });

    it('fails when endDate is before or equal to startDate', () => {
      const result = createAcademicYearSchema.safeParse({
        ...validData,
        startDate: '2026-07-01',
        endDate: '2026-07-01',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.message === 'End date must be greater than start date.')).toBe(true);
      }
    });
  });

  describe('updateAcademicYearSchema', () => {
    const validUpdate = {
      name: 'Academic Year 2026/2027 Updated',
      startDate: '2026-07-15',
      endDate: '2027-06-25',
      notes: 'Adjusted after committee review',
    };

    it('validates a valid update payload', () => {
      const result = updateAcademicYearSchema.safeParse(validUpdate);
      expect(result.success).toBe(true);
    });

    it('fails when name is shorter than 3 characters', () => {
      const result = updateAcademicYearSchema.safeParse({ ...validUpdate, name: 'A' });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Name must be at least 3 characters.');
      }
    });

    it('fails when endDate is not greater than startDate', () => {
      const result = updateAcademicYearSchema.safeParse({
        ...validUpdate,
        startDate: '2027-01-01',
        endDate: '2026-12-31',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('approveAcademicYearSchema', () => {
    it('validates an approval payload with optional fields', () => {
      const result = approveAcademicYearSchema.safeParse({
        approvedBy: 'Academic Director',
        notes: 'Approved during board meeting',
      });
      expect(result.success).toBe(true);
    });

    it('validates empty object for approve', () => {
      const result = approveAcademicYearSchema.safeParse({});
      expect(result.success).toBe(true);
    });
  });
});
