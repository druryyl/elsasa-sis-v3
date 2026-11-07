import { describe, expect, it } from 'vitest';
import {
  concludeParticipationSchema,
  recordProgressSchema,
} from '../learningProgress.schema';
import {
  LEARNING_PROGRESS_BASES,
  LEARNING_PROGRESS_STATUSES,
} from '../../types/learningProgress.types';

describe('learningProgress.schema (P9-S26)', () => {
  const validUUID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

  describe('recordProgressSchema - Minimal States, Bases & Explicit Completion Dates', () => {
    it('validates a standard in-progress record payload', () => {
      const valid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'in_progress',
        basis: 'learning_work',
        completionDate: null,
        notes: 'Actively participating in laboratory sessions',
      };

      const result = recordProgressSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.status).toBe('in_progress');
        expect(result.data.basis).toBe('learning_work');
      }
    });

    it('validates all Phase-2 minimal progress states', () => {
      LEARNING_PROGRESS_STATUSES.forEach((status) => {
        const payload = {
          studentId: validUUID,
          learningUnitId: validUUID,
          academicPeriodId: validUUID,
          status,
          basis: 'learning_work',
          completionDate: status === 'completed' ? '2026-11-07' : null,
          notes: status === 'exempted' ? 'Coursework waived' : undefined,
        };

        const result = recordProgressSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });
    });

    it('validates all operational bases', () => {
      LEARNING_PROGRESS_BASES.forEach((basis) => {
        const payload = {
          studentId: validUUID,
          learningUnitId: validUUID,
          academicPeriodId: validUUID,
          status: 'in_progress',
          basis,
          notes: basis === 'authorized_decision' ? 'RPL transfer credit approved' : undefined,
        };

        const result = recordProgressSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });
    });

    it('requires explicit completionDate when status is completed', () => {
      const missingDate = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'assessment_evidence',
        completionDate: null,
      };

      const emptyDate = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'assessment_evidence',
        completionDate: '   ',
      };

      expect(recordProgressSchema.safeParse(missingDate).success).toBe(false);
      expect(recordProgressSchema.safeParse(emptyDate).success).toBe(false);

      const res = recordProgressSchema.safeParse(missingDate);
      if (!res.success) {
        expect(res.error.issues[0].path).toContain('completionDate');
        expect(res.error.issues[0].message).toContain('Completion date is mandatory');
      }
    });

    it('allows out-of-period explicit completion date stamping when status is completed', () => {
      const outOfPeriodCompleted = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'assessment_evidence',
        completionDate: '2026-08-20', // Summer holiday / out-of-period explicit completion
        notes: 'PACE module completed during holiday remediation packet',
      };

      const result = recordProgressSchema.safeParse(outOfPeriodCompleted);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.completionDate).toBe('2026-08-20');
      }
    });

    it('rejects invalid completion date formats or impossible calendar dates', () => {
      const invalidFormat = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'assessment_evidence',
        completionDate: '07/11/2026', // non-ISO
      };

      const impossibleDate = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'assessment_evidence',
        completionDate: '2026-02-30', // Feb 30 does not exist
      };

      expect(recordProgressSchema.safeParse(invalidFormat).success).toBe(false);
      expect(recordProgressSchema.safeParse(impossibleDate).success).toBe(false);
    });

    it('requires explanatory notes when basis is authorized_decision (RPL / Transfer Learning)', () => {
      const missingNotes = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'authorized_decision',
        completionDate: '2026-11-07',
        notes: '',
      };

      const whitespaceNotes = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'completed',
        basis: 'authorized_decision',
        completionDate: '2026-11-07',
        notes: '     ',
      };

      expect(recordProgressSchema.safeParse(missingNotes).success).toBe(false);
      expect(recordProgressSchema.safeParse(whitespaceNotes).success).toBe(false);

      const res = recordProgressSchema.safeParse(missingNotes);
      if (!res.success) {
        expect(res.error.issues[0].path).toContain('notes');
        expect(res.error.issues[0].message).toContain('Explanatory notes are required');
      }

      const validRpl = {
        ...missingNotes,
        notes: 'Recognized prior learning from Surabaya International School grade transcript.',
      };
      expect(recordProgressSchema.safeParse(validRpl).success).toBe(true);
    });

    it('fails if studentId, learningUnitId, or academicPeriodId is not a valid UUID', () => {
      const invalid = {
        studentId: 'bad-uuid',
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        status: 'in_progress',
        basis: 'learning_work',
      };

      const result = recordProgressSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('studentId');
      }
    });
  });

  describe('concludeParticipationSchema - Formal Conclusion', () => {
    it('validates conclude participation payload with all required UUIDs', () => {
      const valid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        notes: 'Completed with exemplary distinction',
      };

      const result = concludeParticipationSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates conclude participation with omitted or null notes', () => {
      const validWithoutNotes = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        notes: null,
      };

      expect(concludeParticipationSchema.safeParse(validWithoutNotes).success).toBe(true);
    });

    it('fails conclude participation if mandatory UUIDs are missing or invalid', () => {
      const invalid = {
        studentId: validUUID,
        learningUnitId: 'invalid-unit-id',
        academicPeriodId: validUUID,
      };

      const result = concludeParticipationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('learningUnitId');
      }
    });
  });
});
