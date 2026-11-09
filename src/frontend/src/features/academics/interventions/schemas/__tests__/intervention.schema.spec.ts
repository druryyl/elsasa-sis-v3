import { describe, expect, it } from 'vitest';
import {
  initiateInterventionSchema,
  updateInterventionStatusSchema,
} from '../intervention.schema';
import {
  INTERVENTION_TYPES,
  TRIGGER_TYPES,
  INTERVENTION_STATUSES,
} from '../../types';

describe('intervention.schema (P9-S26)', () => {
  const validUUID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

  describe('initiateInterventionSchema - Plan Creation & Milestone Dates', () => {
    it('validates a complete valid initiate intervention payload with milestone date', () => {
      const valid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'remediation',
        triggerType: 'assessment_below_threshold',
        triggerDescription: 'Score 52.00 on Midterm Math below passing score 70.00',
        planDescription: 'Provide 3 weeks of targeted remedial worksheets and extra tutoring.',
        targetCompletionDate: '2026-12-15',
      };

      const result = initiateInterventionSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.targetCompletionDate).toBe('2026-12-15');
      }
    });

    it('accepts null, empty string, or undefined for targetCompletionDate', () => {
      const base = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'tutoring_recommendation',
        triggerType: 'teacher_referral',
        triggerDescription: 'Student requested assistance with linear equations.',
        planDescription: 'Assign peer tutor twice a week for one month.',
      };

      expect(initiateInterventionSchema.safeParse({ ...base, targetCompletionDate: null }).success).toBe(true);
      expect(initiateInterventionSchema.safeParse({ ...base, targetCompletionDate: '' }).success).toBe(true);
      expect(initiateInterventionSchema.safeParse(base).success).toBe(true);
    });

    it('rejects invalid milestone date formats', () => {
      const invalid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'remediation',
        triggerType: 'assessment_below_threshold',
        triggerDescription: 'Below threshold score',
        planDescription: 'Targeted remediation plan.',
        targetCompletionDate: '15-12-2026', // non-ISO format
      };

      const result = initiateInterventionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('targetCompletionDate');
      }
    });

    it('accepts all discrete intervention types', () => {
      INTERVENTION_TYPES.forEach((type) => {
        const payload = {
          studentId: validUUID,
          learningUnitId: validUUID,
          academicPeriodId: validUUID,
          interventionType: type,
          triggerType: 'teacher_referral',
          triggerDescription: 'Referral for type check',
          planDescription: 'Valid plan description for type validation',
        };
        const result = initiateInterventionSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });
    });

    it('rejects invalid intervention type', () => {
      const invalid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'invalid_type',
        triggerType: 'teacher_referral',
        triggerDescription: 'Trigger narrative',
        planDescription: 'Valid plan description.',
      };

      const result = initiateInterventionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('interventionType');
      }
    });

    it('accepts all discrete trigger types', () => {
      TRIGGER_TYPES.forEach((type) => {
        const payload = {
          studentId: validUUID,
          learningUnitId: validUUID,
          academicPeriodId: validUUID,
          interventionType: 'academic_warning',
          triggerType: type,
          triggerDescription: `Trigger check for ${type}`,
          planDescription: 'Structured action plan details.',
        };
        const result = initiateInterventionSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });
    });

    it('fails if studentId, learningUnitId, or academicPeriodId is not a valid UUID', () => {
      const invalid = {
        studentId: 'not-a-uuid',
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'remediation',
        triggerType: 'attendance_concern',
        triggerDescription: 'Attendance 65%',
        planDescription: 'Attendance improvement plan.',
      };

      const result = initiateInterventionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('studentId');
      }
    });

    it('fails if plan description is shorter than 5 characters or empty', () => {
      const invalid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'remediation',
        triggerType: 'standing_concern',
        triggerDescription: 'Standing warning',
        planDescription: 'Tiny', // < 5 chars
      };

      const result = initiateInterventionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('planDescription');
      }
    });

    it('fails if trigger description is missing or empty', () => {
      const invalid = {
        studentId: validUUID,
        learningUnitId: validUUID,
        academicPeriodId: validUUID,
        interventionType: 'remediation',
        triggerType: 'attendance_concern',
        triggerDescription: '   ',
        planDescription: 'Provide targeted support.',
      };

      const result = initiateInterventionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });
  });

  describe('updateInterventionStatusSchema - Outcome Narrative Requirements', () => {
    it('validates status transition to in_progress without outcome notes', () => {
      const valid = {
        status: 'in_progress',
        outcomeNotes: '',
      };

      const result = updateInterventionStatusSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates status transition to planned with null outcome notes', () => {
      const valid = {
        status: 'planned',
        outcomeNotes: null,
      };

      const result = updateInterventionStatusSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('fails update intervention status to completed when outcome notes are missing or whitespace', () => {
      const missing = {
        status: 'completed',
        outcomeNotes: '',
      };
      const whitespace = {
        status: 'completed',
        outcomeNotes: '   ',
      };

      expect(updateInterventionStatusSchema.safeParse(missing).success).toBe(false);
      expect(updateInterventionStatusSchema.safeParse(whitespace).success).toBe(false);

      const res = updateInterventionStatusSchema.safeParse(missing);
      if (!res.success) {
        expect(res.error.issues[0].path).toContain('outcomeNotes');
        expect(res.error.issues[0].message).toContain('Outcome notes are required');
      }
    });

    it('fails update intervention status to cancelled when outcome notes are missing', () => {
      const invalid = {
        status: 'cancelled',
        outcomeNotes: null,
      };

      const result = updateInterventionStatusSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('outcomeNotes');
        expect(result.error.issues[0].message).toContain('Outcome notes are required');
      }
    });

    it('passes update intervention status to completed with substantive outcome notes', () => {
      const valid = {
        status: 'completed',
        outcomeNotes: 'Student successfully completed remediation and re-tested with 82.00.',
      };

      const result = updateInterventionStatusSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('passes update intervention status to cancelled with recorded cancellation rationale', () => {
      const valid = {
        status: 'cancelled',
        outcomeNotes: 'Intervention cancelled due to student withdrawal/transfer to another campus.',
      };

      const result = updateInterventionStatusSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects outcome notes exceeding 2000 characters', () => {
      const invalid = {
        status: 'completed',
        outcomeNotes: 'A'.repeat(2001),
      };

      const result = updateInterventionStatusSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('outcomeNotes');
      }
    });
  });
});
