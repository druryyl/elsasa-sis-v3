// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { recordEvidenceSchema } from '../schemas/assessment.schema';
import { EVIDENCE_TYPES } from '../types';

describe('recordEvidenceSchema.spec.ts - Candidate Exclusivity & Evidence Validation (P10-S25, ACD-WF-08, UC-ACD-07, GAP-003, TD-02)', () => {
  const studentGuid = '11111111-1111-1111-1111-111111111111';
  const applicantGuid = '22222222-2222-2222-2222-222222222222';

  describe('Candidate Exclusivity (XOR Rule)', () => {
    it('accepts evidence when ONLY studentId is provided', () => {
      const result = recordEvidenceSchema.safeParse({
        studentId: studentGuid,
        evidenceType: 'submission',
        collectedDate: '2026-11-05',
        evidenceReference: 'https://storage.elsasa.sch.id/sub/001.pdf',
      });

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.studentId).toBe(studentGuid);
        expect(result.data.applicantId).toBeUndefined();
      }
    });

    it('accepts evidence when ONLY applicantId is provided', () => {
      const result = recordEvidenceSchema.safeParse({
        applicantId: applicantGuid,
        evidenceType: 'test_paper',
        collectedDate: '2026-11-05',
      });

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.applicantId).toBe(applicantGuid);
        expect(result.data.studentId).toBeUndefined();
      }
    });

    it('rejects evidence when BOTH studentId and applicantId are provided (Dual Reference Violation)', () => {
      const result = recordEvidenceSchema.safeParse({
        studentId: studentGuid,
        applicantId: applicantGuid,
        evidenceType: 'observation',
        collectedDate: '2026-11-05',
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.length).toBeGreaterThan(0);
        expect(result.error.issues[0].message).toMatch(/specify either student or applicant, but not both/i);
      }
    });

    it('rejects evidence when NEITHER studentId nor applicantId is provided', () => {
      const result = recordEvidenceSchema.safeParse({
        evidenceType: 'interview',
        collectedDate: '2026-11-05',
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.length).toBeGreaterThan(0);
        expect(result.error.issues[0].message).toMatch(/specify either student or applicant, but not both/i);
      }
    });

    it('rejects evidence when both studentId and applicantId are empty strings or null', () => {
      const result = recordEvidenceSchema.safeParse({
        studentId: '',
        applicantId: null,
        evidenceType: 'submission',
        collectedDate: '2026-11-05',
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toMatch(/specify either student or applicant, but not both/i);
      }
    });
  });

  describe('Evidence Type & Date Validation', () => {
    it('accepts all registered evidence type literals', () => {
      for (const type of EVIDENCE_TYPES) {
        const result = recordEvidenceSchema.safeParse({
          studentId: studentGuid,
          evidenceType: type,
          collectedDate: '2026-11-05',
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects invalid evidence type', () => {
      const result = recordEvidenceSchema.safeParse({
        studentId: studentGuid,
        evidenceType: 'telepathic_transmission',
        collectedDate: '2026-11-05',
      });
      expect(result.success).toBe(false);
    });

    it('rejects malformed collected date', () => {
      const result = recordEvidenceSchema.safeParse({
        studentId: studentGuid,
        evidenceType: 'submission',
        collectedDate: '05/11/2026', // must be YYYY-MM-DD
      });
      expect(result.success).toBe(false);
    });
  });
});
