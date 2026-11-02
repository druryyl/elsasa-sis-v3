import { describe, it, expect } from 'vitest';
import {
  createOperationalPeriodSchema,
  updateOperationalPeriodSchema,
} from '../operationalPeriod.schema';

describe('operationalPeriod.schema', () => {
  describe('createOperationalPeriodSchema', () => {
    const validData = {
      periodCode: 'SEM-1-2026',
      name: 'Semester 1 2026/2027',
      periodType: 'semester' as const,
      startDate: '2026-07-15',
      endDate: '2026-12-20',
      campusContext: 'All',
      notes: 'First academic term',
    };

    it('validates a valid period payload', () => {
      const result = createOperationalPeriodSchema.safeParse(validData);
      expect(result.success).toBe(true);
    });

    it('accepts optional valid UUID parentPeriodId', () => {
      const result = createOperationalPeriodSchema.safeParse({
        ...validData,
        parentPeriodId: '11111111-1111-4111-8111-111111111111',
      });
      expect(result.success).toBe(true);
    });

    it('rejects invalid parentPeriodId format', () => {
      const result = createOperationalPeriodSchema.safeParse({
        ...validData,
        parentPeriodId: 'not-a-uuid',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0]?.message).toBe('Parent period ID must be a valid UUID.');
      }
    });

    it('fails when periodCode is too short', () => {
      const result = createOperationalPeriodSchema.safeParse({
        ...validData,
        periodCode: 'S',
      });
      expect(result.success).toBe(false);
    });

    it('fails on invalid periodType', () => {
      const result = createOperationalPeriodSchema.safeParse({
        ...validData,
        periodType: 'invalid_type',
      });
      expect(result.success).toBe(false);
    });

    it('fails when endDate is strictly before startDate', () => {
      const result = createOperationalPeriodSchema.safeParse({
        ...validData,
        startDate: '2026-12-01',
        endDate: '2026-11-30',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.message === 'End date must be greater than or equal to start date.')).toBe(true);
      }
    });

    it('allows single-day period where startDate equals endDate', () => {
      const result = createOperationalPeriodSchema.safeParse({
        ...validData,
        periodCode: 'EXAM-DAY-1',
        name: 'Midterm Exam Day 1',
        periodType: 'assessment_window' as const,
        startDate: '2026-10-10',
        endDate: '2026-10-10',
      });
      expect(result.success).toBe(true);
    });
  });

  describe('updateOperationalPeriodSchema', () => {
    it('validates valid update data', () => {
      const result = updateOperationalPeriodSchema.safeParse({
        name: 'Semester 1 Revised',
        startDate: '2026-07-20',
        endDate: '2026-12-23',
      });
      expect(result.success).toBe(true);
    });
  });
});
