// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  batchCompileReportCardsSchema,
  compileReportCardSchema,
  correctResultSchema,
  generateTranscriptSchema,
} from '../schemas/records.schema';
import {
  REPORT_STATUSES,
  REPORT_TYPES,
  TRANSCRIPT_STATUSES,
  TRANSCRIPT_TYPES,
} from '../types';

describe('Records Zod Validation Schemas (P8-S19)', () => {
  describe('Constants and Enums', () => {
    it('defines expected enums', () => {
      expect(REPORT_TYPES).toContain('mid_term');
      expect(REPORT_TYPES).toContain('end_of_term');
      expect(REPORT_STATUSES).toContain('draft');
      expect(REPORT_STATUSES).toContain('approved');
      expect(REPORT_STATUSES).toContain('published');
      expect(TRANSCRIPT_TYPES).toContain('official');
      expect(TRANSCRIPT_TYPES).toContain('unofficial');
      expect(TRANSCRIPT_STATUSES).toContain('draft');
      expect(TRANSCRIPT_STATUSES).toContain('approved');
      expect(TRANSCRIPT_STATUSES).toContain('issued');
    });
  });

  describe('batchCompileReportCardsSchema', () => {
    const validBatchPayload = {
      academicPeriodId: '11111111-1111-1111-1111-111111111111',
      reportType: 'end_of_term',
      templateName: 'Buku Rapor Merdeka',
      sectionLabel: '4A',
    };

    it('validates a valid batch compilation payload', () => {
      const result = batchCompileReportCardsSchema.safeParse(validBatchPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.academicPeriodId).toBe('11111111-1111-1111-1111-111111111111');
        expect(result.data.sectionLabel).toBe('4A');
      }
    });

    it('applies default templateName if not supplied', () => {
      const result = batchCompileReportCardsSchema.safeParse({
        academicPeriodId: '11111111-1111-1111-1111-111111111111',
        reportType: 'mid_term',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.templateName).toBe('Buku Rapor Merdeka');
      }
    });

    it('rejects empty academicPeriodId', () => {
      const result = batchCompileReportCardsSchema.safeParse({
        ...validBatchPayload,
        academicPeriodId: '',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const err = result.error.issues.find((i) => i.path.includes('academicPeriodId'));
        expect(err?.message).toMatch(/academic period is required/i);
      }
    });

    it('rejects invalid reportType', () => {
      const result = batchCompileReportCardsSchema.safeParse({
        ...validBatchPayload,
        reportType: 'annual',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('compileReportCardSchema', () => {
    it('validates a single student compile report card request', () => {
      const result = compileReportCardSchema.safeParse({
        studentId: '11111111-1111-1111-1111-111111111111',
        academicPeriodId: '22222222-2222-2222-2222-222222222222',
        academicPeriodLabel: 'Term 1 2026/2027',
        reportType: 'mid_term',
        templateName: 'Buku Rapor Merdeka',
        narrativeComments: 'Solid progress',
      });
      expect(result.success).toBe(true);
    });
  });

  describe('correctResultSchema', () => {
    const validCorrection = {
      score_numeric: 82,
      score_max: 100,
      reason: 'Recalculated essay rubric score according to teacher appeal',
      evidence: 'https://storage.elsasa.sch.id/appeals/123.pdf',
      is_material: true,
    };

    it('validates a valid correction payload', () => {
      const result = correctResultSchema.safeParse(validCorrection);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.score_numeric).toBe(82);
        expect(result.data.is_material).toBe(true);
      }
    });

    it('rejects reason shorter than 10 characters', () => {
      const tenCharResult = correctResultSchema.safeParse({
        ...validCorrection,
        reason: 'Typo fixed', // 10 chars is minimum
      });
      expect(tenCharResult.success).toBe(true);

      const shortResult = correctResultSchema.safeParse({
        ...validCorrection,
        reason: 'Fixed it',
      });
      expect(shortResult.success).toBe(false);
      if (!shortResult.success) {
        const err = shortResult.error.issues.find((i) => i.path.includes('reason'));
        expect(err?.message).toMatch(/at least 10 characters long/i);
      }
    });

    it('rejects when no altered score or remark field is provided', () => {
      const result = correctResultSchema.safeParse({
        reason: 'Recalculated essay rubric score according to appeal',
        is_material: false,
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const err = result.error.issues.find((i) => i.path.includes('score_numeric'));
        expect(err?.message).toMatch(/at least one corrected value field/i);
      }
    });

    it('accepts letter grade or narrative feedback as the altered field', () => {
      const result = correctResultSchema.safeParse({
        grade_letter: 'A',
        narrative_feedback: 'Outstanding mastery demonstrated in retake.',
        reason: 'Authorized grade adjustment by Academic Authority',
      });
      expect(result.success).toBe(true);
    });

    it('rejects score_numeric exceeding score_max', () => {
      const result = correctResultSchema.safeParse({
        score_numeric: 110,
        score_max: 100,
        reason: 'Recalculated essay rubric score according to appeal',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const err = result.error.issues.find((i) => i.path.includes('score_numeric'));
        expect(err?.message).toMatch(/cannot exceed maximum/i);
      }
    });
  });

  describe('generateTranscriptSchema', () => {
    it('validates a valid transcript request payload', () => {
      const payload = {
        studentId: '11111111-1111-1111-1111-111111111111',
        transcriptType: 'official',
        periodStart: '2024-08-01',
        periodEnd: '2026-06-30',
        purpose: 'University Entrance Application',
      };
      const result = generateTranscriptSchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('rejects when periodEnd is earlier than periodStart', () => {
      const payload = {
        studentId: '11111111-1111-1111-1111-111111111111',
        transcriptType: 'official',
        periodStart: '2026-08-01',
        periodEnd: '2025-06-30',
      };
      const result = generateTranscriptSchema.safeParse(payload);
      expect(result.success).toBe(false);
      if (!result.success) {
        const err = result.error.issues.find((i) => i.path.includes('periodEnd'));
        expect(err?.message).toMatch(/period end date must be on or after/i);
      }
    });
  });
});
