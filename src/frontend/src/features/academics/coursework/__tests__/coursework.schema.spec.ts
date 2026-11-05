// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  createAssignmentSchema,
  recordExceptionSchema,
  ASSIGNMENT_STATUSES,
  SUBMISSION_EXCEPTION_TYPES,
} from '../schemas/coursework.schema';

describe('Coursework Zod Validation Schemas (P9-S24)', () => {
  // ==========================================================================
  // 1. Constants
  // ==========================================================================
  describe('Constants', () => {
    it('defines expected ASSIGNMENT_STATUSES', () => {
      expect(ASSIGNMENT_STATUSES).toEqual(['draft', 'released', 'closed']);
    });

    it('defines expected SUBMISSION_EXCEPTION_TYPES', () => {
      expect(SUBMISSION_EXCEPTION_TYPES).toEqual(['excused', 'exempted']);
    });
  });

  // ==========================================================================
  // 2. createAssignmentSchema
  // ==========================================================================
  describe('createAssignmentSchema', () => {
    const validAssignmentPayload = {
      title: 'Thermodynamics Problem Set 1',
      description: 'Solve questions 1 through 10 in chapter 4.',
      releaseDate: '2026-09-01',
      dueDate: '2026-09-15',
      notes: 'Late submissions will incur a 10% penalty per day.',
    };

    it('passes for a fully populated valid payload', () => {
      const result = createAssignmentSchema.safeParse(validAssignmentPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.title).toBe('Thermodynamics Problem Set 1');
        expect(result.data.releaseDate).toBe('2026-09-01');
        expect(result.data.dueDate).toBe('2026-09-15');
      }
    });

    it('passes for minimal payload omitting optional description, dueDate, and notes', () => {
      const minimalPayload = {
        title: 'Pop Quiz 1',
        releaseDate: '2026-09-01',
      };
      const result = createAssignmentSchema.safeParse(minimalPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.dueDate).toBeUndefined();
      }
    });

    it('passes when optional fields are null or empty strings', () => {
      const payloadWithEmptyOptionals = {
        title: 'In-Class Workshop',
        description: '',
        releaseDate: '2026-09-01',
        dueDate: '',
        notes: null,
      };
      const result = createAssignmentSchema.safeParse(payloadWithEmptyOptionals);
      expect(result.success).toBe(true);
    });

    it('passes when dueDate equals releaseDate (same-day assignment)', () => {
      const sameDayPayload = {
        ...validAssignmentPayload,
        releaseDate: '2026-09-01',
        dueDate: '2026-09-01',
      };
      const result = createAssignmentSchema.safeParse(sameDayPayload);
      expect(result.success).toBe(true);
    });

    it('rejects when dueDate is strictly before releaseDate', () => {
      const invalidDateOrder = {
        ...validAssignmentPayload,
        releaseDate: '2026-09-15',
        dueDate: '2026-09-01',
      };
      const result = createAssignmentSchema.safeParse(invalidDateOrder);
      expect(result.success).toBe(false);
      if (!result.success) {
        const issues = result.error.issues;
        expect(issues.some((i) => i.message.includes('Due date must be on or after the release date'))).toBe(true);
      }
    });

    it('rejects missing, empty, or whitespace-only title', () => {
      const noTitle = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        title: undefined,
      });
      expect(noTitle.success).toBe(false);

      const emptyTitle = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        title: '   ',
      });
      expect(emptyTitle.success).toBe(false);
      if (!emptyTitle.success) {
        expect(emptyTitle.error.issues[0].message).toContain('Title is required');
      }
    });

    it('rejects title exceeding 200 characters', () => {
      const tooLongTitle = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        title: 'A'.repeat(201),
      });
      expect(tooLongTitle.success).toBe(false);
      if (!tooLongTitle.success) {
        expect(tooLongTitle.error.issues[0].message).toContain('cannot exceed 200 characters');
      }
    });

    it('rejects description exceeding 2000 characters', () => {
      const tooLongDesc = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        description: 'D'.repeat(2001),
      });
      expect(tooLongDesc.success).toBe(false);
    });

    it('rejects notes exceeding 2000 characters', () => {
      const tooLongNotes = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        notes: 'N'.repeat(2001),
      });
      expect(tooLongNotes.success).toBe(false);
    });

    it('rejects non-ISO format for releaseDate and dueDate', () => {
      const badReleaseFormat = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        releaseDate: '01/09/2026',
      });
      expect(badReleaseFormat.success).toBe(false);

      const badDueFormat = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        dueDate: '15.09.2026',
      });
      expect(badDueFormat.success).toBe(false);
    });

    it('rejects impossible calendar dates for releaseDate and dueDate', () => {
      const impossibleRelease = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        releaseDate: '2026-02-30',
      });
      expect(impossibleRelease.success).toBe(false);
      if (!impossibleRelease.success) {
        expect(impossibleRelease.error.issues[0].message).toContain('valid calendar date');
      }

      const impossibleDue = createAssignmentSchema.safeParse({
        ...validAssignmentPayload,
        dueDate: '2026-13-45',
      });
      expect(impossibleDue.success).toBe(false);
    });
  });

  // ==========================================================================
  // 3. recordExceptionSchema (P5-S11, TD-06)
  // ==========================================================================
  describe('recordExceptionSchema', () => {
    const validExceptionPayload = {
      studentId: '11111111-1111-1111-1111-111111111111',
      exceptionType: 'excused' as const,
      reason: 'Official participation in National Science Olympiad.',
    };

    it('passes for valid excused exception payload', () => {
      const result = recordExceptionSchema.safeParse(validExceptionPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.exceptionType).toBe('excused');
      }
    });

    it('passes for valid exempted exception payload', () => {
      const result = recordExceptionSchema.safeParse({
        ...validExceptionPayload,
        exceptionType: 'exempted',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.exceptionType).toBe('exempted');
      }
    });

    it('rejects missing or empty studentId', () => {
      const emptyStudent = recordExceptionSchema.safeParse({
        ...validExceptionPayload,
        studentId: '   ',
      });
      expect(emptyStudent.success).toBe(false);
      if (!emptyStudent.success) {
        expect(emptyStudent.error.issues[0].message).toContain('Student ID is required');
      }
    });

    it('rejects invalid exceptionType', () => {
      const invalidType = recordExceptionSchema.safeParse({
        ...validExceptionPayload,
        exceptionType: 'waived',
      });
      expect(invalidType.success).toBe(false);
    });

    it('rejects reason shorter than 3 characters or whitespace-only', () => {
      const shortReason = recordExceptionSchema.safeParse({
        ...validExceptionPayload,
        reason: 'Ok',
      });
      expect(shortReason.success).toBe(false);
      if (!shortReason.success) {
        expect(shortReason.error.issues[0].message).toContain('Reason must be at least 3 characters');
      }

      const emptyReason = recordExceptionSchema.safeParse({
        ...validExceptionPayload,
        reason: '   ',
      });
      expect(emptyReason.success).toBe(false);
    });

    it('rejects reason exceeding 2000 characters', () => {
      const tooLongReason = recordExceptionSchema.safeParse({
        ...validExceptionPayload,
        reason: 'R'.repeat(2001),
      });
      expect(tooLongReason.success).toBe(false);
    });
  });
});
