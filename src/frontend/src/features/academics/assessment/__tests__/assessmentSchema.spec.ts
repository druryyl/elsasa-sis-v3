// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  createAssessmentSchema,
  updateAssessmentSchema,
} from '../schemas/assessment.schema';
import {
  ASSESSMENT_METHODS,
  ASSESSMENT_STATUSES,
  ASSESSMENT_TYPES,
  REASSESSMENT_POLICIES,
} from '../types';

describe('assessmentSchema.spec.ts - Zod Validation for Assessments (P10-S25, UC-ACD-05)', () => {
  describe('Enum Literals & Constants', () => {
    it('contains all valid assessment type enum literals', () => {
      expect(ASSESSMENT_TYPES).toEqual([
        'formative',
        'summative',
        'diagnostic',
        'portfolio',
        'project',
        'other',
      ]);
    });

    it('contains all valid assessment method enum literals', () => {
      expect(ASSESSMENT_METHODS).toEqual([
        'written_exam',
        'oral_exam',
        'practical',
        'observation',
        'assignment',
        'presentation',
        'portfolio',
        'peer_review',
        'self_evaluation',
        'other',
      ]);
    });

    it('contains all valid reassessment policy enum literals', () => {
      expect(REASSESSMENT_POLICIES).toEqual([
        'none',
        'capped_at_passing',
        'highest_score',
        'latest_attempt',
        'average',
      ]);
    });

    it('contains all valid assessment status literals', () => {
      expect(ASSESSMENT_STATUSES).toEqual(['planned', 'conducted', 'cancelled']);
    });
  });

  describe('createAssessmentSchema validation rules', () => {
    const validBase = {
      learningUnitId: 'a1b2c3d4-e5f6-7890-1234-56789abcdef0',
      title: 'Matematika Dasar Semester 1',
      assessmentType: 'summative',
      assessmentMethod: 'written_exam',
      reassessmentPolicy: 'capped_at_passing',
      maxScore: 100,
      passingScore: 75,
      assessmentDate: '2026-11-15',
    };

    it('successfully parses a complete valid payload', () => {
      const result = createAssessmentSchema.safeParse(validBase);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.title).toBe('Matematika Dasar Semester 1');
        expect(result.data.maxScore).toBe(100);
        expect(result.data.passingScore).toBe(75);
        expect(result.data.reassessmentPolicy).toBe('capped_at_passing');
      }
    });

    it('enforces title validation rules: minimum 3 characters', () => {
      const shortTitle = createAssessmentSchema.safeParse({
        ...validBase,
        title: 'AB',
      });
      expect(shortTitle.success).toBe(false);
      if (!shortTitle.success) {
        const titleErr = shortTitle.error.issues.find((i) => i.path.includes('title'));
        expect(titleErr?.message).toMatch(/at least 3 characters/i);
      }

      const emptyTitle = createAssessmentSchema.safeParse({
        ...validBase,
        title: '   ',
      });
      expect(emptyTitle.success).toBe(false);
    });

    it('enforces score limit rule: passing score must be <= max score', () => {
      const invalidScores = createAssessmentSchema.safeParse({
        ...validBase,
        maxScore: 100,
        passingScore: 101,
      });
      expect(invalidScores.success).toBe(false);
      if (!invalidScores.success) {
        const scoreErr = invalidScores.error.issues.find((i) => i.path.includes('passingScore'));
        expect(scoreErr?.message).toMatch(/less than or equal to maximum score/i);
      }
    });

    it('permits passing score exactly equal to max score', () => {
      const equalScores = createAssessmentSchema.safeParse({
        ...validBase,
        maxScore: 100,
        passingScore: 100,
      });
      expect(equalScores.success).toBe(true);
    });

    it('permits null or undefined scores', () => {
      const noScores = createAssessmentSchema.safeParse({
        learningUnitId: validBase.learningUnitId,
        title: 'Observation Assessment',
        assessmentType: 'formative',
        assessmentMethod: 'observation',
      });
      expect(noScores.success).toBe(true);
      if (noScores.success) {
        expect(noScores.data.reassessmentPolicy).toBe('none'); // Default policy
      }
    });

    it('rejects invalid enum literals for assessmentType and reassessmentPolicy', () => {
      const badType = createAssessmentSchema.safeParse({
        ...validBase,
        assessmentType: 'invalid_type',
      });
      expect(badType.success).toBe(false);

      const badPolicy = createAssessmentSchema.safeParse({
        ...validBase,
        reassessmentPolicy: 'unsupported_policy',
      });
      expect(badPolicy.success).toBe(false);
    });
  });

  describe('updateAssessmentSchema validation rules', () => {
    it('validates partial update payloads with valid title and limits', () => {
      const result = updateAssessmentSchema.safeParse({
        title: 'Updated Science Project',
        maxScore: 50,
        passingScore: 35,
        reassessmentPolicy: 'highest_score',
      });
      expect(result.success).toBe(true);
    });

    it('rejects passing score > max score during update', () => {
      const result = updateAssessmentSchema.safeParse({
        title: 'Valid Update Assessment Title',
        maxScore: 50,
        passingScore: 55,
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const scoreErr = result.error.issues.find((i) => i.path.includes('passingScore'));
        expect(scoreErr?.message).toMatch(/less than or equal to maximum score/i);
      }
    });
  });
});
