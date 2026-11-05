// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  createAssessmentSchema,
  updateAssessmentSchema,
  recordEvidenceSchema,
  evaluateLearningSchema,
} from '../schemas/assessment.schema';
import {
  ASSESSMENT_METHODS,
  ASSESSMENT_TYPES,
  EVALUATION_MODELS,
  EVIDENCE_TYPES,
  REASSESSMENT_POLICIES,
} from '../types';

describe('Assessment Zod Validation Schemas (P8-S19)', () => {
  describe('Constants and Enums', () => {
    it('defines expected enums', () => {
      expect(ASSESSMENT_TYPES).toContain('formative');
      expect(ASSESSMENT_TYPES).toContain('summative');
      expect(ASSESSMENT_METHODS).toContain('written_exam');
      expect(REASSESSMENT_POLICIES).toContain('capped_at_passing');
      expect(REASSESSMENT_POLICIES).toContain('highest_score');
      expect(EVIDENCE_TYPES).toContain('submission');
      expect(EVALUATION_MODELS).toContain('numeric');
      expect(EVALUATION_MODELS).toContain('rubric');
    });
  });

  describe('createAssessmentSchema', () => {
    const validPayload = {
      learningUnitId: '11111111-1111-1111-1111-111111111111',
      title: 'Midterm Evaluation',
      assessmentType: 'summative',
      assessmentMethod: 'written_exam',
      reassessmentPolicy: 'capped_at_passing',
      maxScore: 100,
      passingScore: 75,
      criteria: 'Passes if score >= 75',
      assessmentDate: '2026-10-15',
    };

    it('validates a complete valid payload', () => {
      const result = createAssessmentSchema.safeParse(validPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.title).toBe('Midterm Evaluation');
        expect(result.data.passingScore).toBe(75);
      }
    });

    it('rejects title shorter than 3 characters', () => {
      const result = createAssessmentSchema.safeParse({
        ...validPayload,
        title: 'AB',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const titleErr = result.error.issues.find((i) => i.path.includes('title'));
        expect(titleErr?.message).toMatch(/at least 3 characters/i);
      }
    });

    it('rejects passingScore greater than maxScore', () => {
      const result = createAssessmentSchema.safeParse({
        ...validPayload,
        maxScore: 100,
        passingScore: 105,
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const scoreErr = result.error.issues.find((i) => i.path.includes('passingScore'));
        expect(scoreErr?.message).toMatch(/less than or equal to maximum score/i);
      }
    });

    it('accepts passingScore equal to maxScore', () => {
      const result = createAssessmentSchema.safeParse({
        ...validPayload,
        maxScore: 100,
        passingScore: 100,
      });
      expect(result.success).toBe(true);
    });
  });

  describe('updateAssessmentSchema', () => {
    it('validates a valid update payload', () => {
      const result = updateAssessmentSchema.safeParse({
        title: 'Updated Final Examination',
        maxScore: 100,
        passingScore: 75,
        reassessmentPolicy: 'highest_score',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.title).toBe('Updated Final Examination');
      }
    });

    it('rejects title shorter than 3 characters', () => {
      const result = updateAssessmentSchema.safeParse({
        title: 'X',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('recordEvidenceSchema', () => {
    const validStudentPayload = {
      studentId: '22222222-2222-2222-2222-222222222222',
      evidenceType: 'submission',
      collectedDate: '2026-10-15',
      evidenceReference: 'https://storage.elsasa.sch.id/sub/123.pdf',
    };

    const validApplicantPayload = {
      applicantId: '33333333-3333-3333-3333-333333333333',
      evidenceType: 'test_paper',
      collectedDate: '2026-10-15',
    };

    it('accepts student evidence without applicant', () => {
      const result = recordEvidenceSchema.safeParse(validStudentPayload);
      expect(result.success).toBe(true);
    });

    it('accepts applicant evidence without student', () => {
      const result = recordEvidenceSchema.safeParse(validApplicantPayload);
      expect(result.success).toBe(true);
    });

    it('rejects when both student and applicant are provided (XOR violation)', () => {
      const result = recordEvidenceSchema.safeParse({
        studentId: '22222222-2222-2222-2222-222222222222',
        applicantId: '33333333-3333-3333-3333-333333333333',
        evidenceType: 'submission',
        collectedDate: '2026-10-15',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toMatch(/specify either student or applicant, but not both/i);
      }
    });

    it('rejects when neither student nor applicant is provided', () => {
      const result = recordEvidenceSchema.safeParse({
        evidenceType: 'submission',
        collectedDate: '2026-10-15',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toMatch(/specify either student or applicant, but not both/i);
      }
    });

    it('rejects invalid collectedDate format', () => {
      const result = recordEvidenceSchema.safeParse({
        ...validStudentPayload,
        collectedDate: '15-10-2026',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('evaluateLearningSchema', () => {
    const validNumericPayload = {
      studentId: '22222222-2222-2222-2222-222222222222',
      scoreNumeric: 88,
      scoreMax: 100,
      evaluationModel: 'numeric',
      publishImmediately: false,
    };

    it('accepts valid numeric evaluation', () => {
      const result = evaluateLearningSchema.safeParse(validNumericPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.scoreNumeric).toBe(88);
      }
    });

    it('rejects numeric evaluation when scoreNumeric is missing', () => {
      const result = evaluateLearningSchema.safeParse({
        studentId: '22222222-2222-2222-2222-222222222222',
        evaluationModel: 'numeric',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const scoreErr = result.error.issues.find((i) => i.path.includes('scoreNumeric'));
        expect(scoreErr?.message).toMatch(/numeric score is required/i);
      }
    });

    it('rejects score exceeding scoreMax', () => {
      const result = evaluateLearningSchema.safeParse({
        ...validNumericPayload,
        scoreNumeric: 105,
        scoreMax: 100,
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const scoreErr = result.error.issues.find((i) => i.path.includes('scoreNumeric'));
        expect(scoreErr?.message).toMatch(/cannot exceed maximum/i);
      }
    });

    it('accepts rubric / qualitative evaluation without numeric score', () => {
      const result = evaluateLearningSchema.safeParse({
        studentId: '22222222-2222-2222-2222-222222222222',
        gradeLabel: 'Kompeten',
        narrativeFeedback: 'Demonstrated thorough mastery of subject competencies.',
        evaluationModel: 'competency',
      });
      expect(result.success).toBe(true);
    });
  });
});
