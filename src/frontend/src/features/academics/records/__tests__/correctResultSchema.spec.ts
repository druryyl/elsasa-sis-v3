// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { correctResultSchema } from '../schemas/records.schema';

describe('correctResultSchema.spec.ts - Academic Record Correction Schema Validation (P10-S25, UC-ACD-19, ACD-WF-15, GAP-007, TD-06)', () => {
  const validBaseCorrection = {
    score_numeric: 87.5,
    score_max: 100,
    reason: 'Recalculated essay rubric score according to teacher appeal',
    is_material: true,
  };

  describe('Mandatory Reason Length (min 10 characters)', () => {
    it('accepts reasons that meet or exceed 10 characters', () => {
      // Exactly 10 characters: "1234567890"
      const tenCharResult = correctResultSchema.safeParse({
        ...validBaseCorrection,
        reason: '1234567890',
      });
      expect(tenCharResult.success).toBe(true);

      // Longer justification
      const detailedResult = correctResultSchema.safeParse({
        ...validBaseCorrection,
        reason: 'Approved recalculation following formal student committee review and rubric regrade',
      });
      expect(detailedResult.success).toBe(true);
      if (detailedResult.success) {
        expect(detailedResult.data.reason).toBe(
          'Approved recalculation following formal student committee review and rubric regrade',
        );
      }
    });

    it('rejects reasons shorter than 10 characters', () => {
      const nineCharResult = correctResultSchema.safeParse({
        ...validBaseCorrection,
        reason: 'Typo fixed', // 10 chars, wait: "Typo fixed" is 10 chars ("Typo" 4 + " " 1 + "fixed" 5 = 10)
      });
      expect(nineCharResult.success).toBe(true);

      const shortResult = correctResultSchema.safeParse({
        ...validBaseCorrection,
        reason: 'Typo fix', // 8 chars
      });
      expect(shortResult.success).toBe(false);
      if (!shortResult.success) {
        const reasonErr = shortResult.error.issues.find((i) => i.path.includes('reason'));
        expect(reasonErr?.message).toMatch(/at least 10 characters long/i);
      }
    });

    it('rejects empty or whitespace-only reason', () => {
      const emptyResult = correctResultSchema.safeParse({
        ...validBaseCorrection,
        reason: '',
      });
      expect(emptyResult.success).toBe(false);

      const whitespaceResult = correctResultSchema.safeParse({
        ...validBaseCorrection,
        reason: '         ', // 9 spaces
      });
      expect(whitespaceResult.success).toBe(false);
    });
  });

  describe('At Least One Altered Field Requirement', () => {
    it('rejects correction payloads that provide only reason without any altered score/remark field', () => {
      const result = correctResultSchema.safeParse({
        reason: 'Legitimate explanation exceeding ten characters',
        is_material: true,
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        const fieldErr = result.error.issues.find((i) => i.path.includes('score_numeric'));
        expect(fieldErr?.message).toMatch(/at least one corrected value field/i);
      }
    });

    it('accepts correction when only grade_letter is altered', () => {
      const result = correctResultSchema.safeParse({
        grade_letter: 'A',
        reason: 'Recalibrated letter grade conversion standard',
      });
      expect(result.success).toBe(true);
    });

    it('accepts correction when only grade_label is altered', () => {
      const result = correctResultSchema.safeParse({
        grade_label: 'Sangat Kompeten',
        reason: 'Updated Merdeka competency level descriptor',
      });
      expect(result.success).toBe(true);
    });

    it('accepts correction when only narrative_feedback is altered', () => {
      const result = correctResultSchema.safeParse({
        narrative_feedback: 'Student showed remarkable improvement during remediation exercises.',
        reason: 'Teacher expanded qualitative diagnostic remarks',
      });
      expect(result.success).toBe(true);
    });
  });

  describe('Numeric Score Limit & Materiality Defaults', () => {
    it('rejects score_numeric exceeding score_max', () => {
      const result = correctResultSchema.safeParse({
        score_numeric: 105,
        score_max: 100,
        reason: 'Invalid score adjustment exceeding maximum ceiling',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const scoreErr = result.error.issues.find((i) => i.path.includes('score_numeric'));
        expect(scoreErr?.message).toMatch(/cannot exceed maximum/i);
      }
    });

    it('defaults is_material to false when omitted', () => {
      const result = correctResultSchema.safeParse({
        score_numeric: 80,
        reason: 'Minor adjustment per teacher clarification',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.is_material).toBe(false);
      }
    });
  });
});
