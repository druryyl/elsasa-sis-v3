import { describe, expect, it } from 'vitest';
import {
  batchDetermineStandingSchema,
  batchIssueProgressionSchema,
  conferAchievementSchema,
  correctProgressionSchema,
  correctStandingSchema,
  progressionOverrideSchema,
  standingOverrideSchema,
} from '../cohortReview.schema';
import {
  ACADEMIC_STANDINGS,
  ACHIEVEMENT_TYPES,
  PROMOTION_RECOMMENDATIONS,
} from '../../types';

describe('cohortReview.schema (P9-S26)', () => {
  const validUUID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

  describe('standingOverrideSchema - Overrides, Decrees & Condition Text', () => {
    it('validates a standard good standing determination without conditions or exception', () => {
      const valid = {
        standing: 'good_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        conditions: null,
        isException: false,
      };

      const result = standingOverrideSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.standing).toBe('good_standing');
        expect(result.data.isException).toBe(false);
      }
    });

    it('validates all discrete academic standing enums', () => {
      ACADEMIC_STANDINGS.forEach((standing) => {
        const payload = {
          standing,
          policyReference: 'Academic Standing Guidelines 2026',
          conditions: standing === 'conditional_standing' ? 'Maintain minimum 75% attendance.' : undefined,
          isException: false,
        };

        const result = standingOverrideSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });
    });

    it('fails when conditional standing is missing mandatory condition text', () => {
      const emptyConditions = {
        standing: 'conditional_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        conditions: '',
        isException: false,
      };

      const nullConditions = {
        standing: 'conditional_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        conditions: null,
        isException: false,
      };

      const whitespaceConditions = {
        standing: 'conditional_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        conditions: '    ',
        isException: false,
      };

      expect(standingOverrideSchema.safeParse(emptyConditions).success).toBe(false);
      expect(standingOverrideSchema.safeParse(nullConditions).success).toBe(false);
      expect(standingOverrideSchema.safeParse(whitespaceConditions).success).toBe(false);

      const result = standingOverrideSchema.safeParse(emptyConditions);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('conditions');
        expect(result.error.issues[0].message).toContain('Conditions are mandatory');
      }
    });

    it('requires exceptionReason (min 5 chars) and committee decree reference when isException is true', () => {
      const missingBoth = {
        standing: 'good_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        isException: true,
        exceptionReason: '',
        committeeReference: '',
      };

      const result = standingOverrideSchema.safeParse(missingBoth);
      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.issues.map((i) => i.path[0]);
        expect(paths).toContain('exceptionReason');
        expect(paths).toContain('committeeReference');
      }

      const shortReason = {
        standing: 'good_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        isException: true,
        exceptionReason: 'Tiny', // < 5 chars
        committeeReference: 'SK-DG/2026/06-01',
      };

      const shortRes = standingOverrideSchema.safeParse(shortReason);
      expect(shortRes.success).toBe(false);
      if (!shortRes.success) {
        expect(shortRes.error.issues[0].path).toContain('exceptionReason');
        expect(shortRes.error.issues[0].message).toContain('min 5 characters');
      }

      const validException = {
        standing: 'good_standing',
        policyReference: 'Academic Policy 2026 Art. 12',
        isException: true,
        exceptionReason: 'Committee grant of medical indulgence for prolonged hospitalization.',
        committeeReference: 'SK-DG/2026/06-01 (SK Dewan Guru)',
      };

      const validRes = standingOverrideSchema.safeParse(validException);
      expect(validRes.success).toBe(true);
    });

    it('fails when policy reference is empty or whitespace', () => {
      const invalid = {
        standing: 'good_standing',
        policyReference: '   ',
        isException: false,
      };

      const result = standingOverrideSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('policyReference');
      }
    });
  });

  describe('progressionOverrideSchema - Destination Levels, Conditions & Decrees', () => {
    it('validates standard promotion_eligible requiring destination level', () => {
      const valid = {
        recommendation: 'promotion_eligible',
        fromLevel: 'Grade 4',
        recommendedToLevel: 'Grade 5',
        supportingEvidence: 'Passed all 10 learning units with GPA > 3.0.',
        isException: false,
      };

      const result = progressionOverrideSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('fails when promotion_eligible is missing recommendedToLevel', () => {
      const invalid = {
        recommendation: 'promotion_eligible',
        fromLevel: 'Grade 4',
        recommendedToLevel: '',
        supportingEvidence: 'Passed all units.',
        isException: false,
      };

      const result = progressionOverrideSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('recommendedToLevel');
      }
    });

    it('validates conditional_promotion requiring stipulated conditions', () => {
      const invalid = {
        recommendation: 'conditional_promotion',
        fromLevel: 'Grade 4',
        recommendedToLevel: 'Grade 5',
        conditions: '',
        supportingEvidence: 'Passed 8 out of 9 units',
        isException: false,
      };

      const result = progressionOverrideSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('conditions');
        expect(result.error.issues[0].message).toContain('Stipulated conditions are mandatory');
      }

      const valid = {
        ...invalid,
        conditions: 'Must clear remedial science module by end of Term 1.',
      };
      expect(progressionOverrideSchema.safeParse(valid).success).toBe(true);
    });

    it('validates retention_recommended without requiring destination level or conditions', () => {
      const valid = {
        recommendation: 'retention_recommended',
        fromLevel: 'Grade 4',
        recommendedToLevel: null,
        conditions: null,
        supportingEvidence: 'Cumulative credits below retention threshold; 4 failed units.',
        isException: false,
      };

      const result = progressionOverrideSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('requires exceptionReason and committee decree reference for progression exceptions', () => {
      const exceptionWithoutDecree = {
        recommendation: 'conditional_promotion',
        fromLevel: 'Grade 4',
        recommendedToLevel: 'Grade 5',
        conditions: 'Attend holiday booster program.',
        supportingEvidence: 'Board deliberation',
        isException: true,
        exceptionReason: 'Approved by board after review of extenuating circumstances.',
        committeeReference: '',
      };

      const result = progressionOverrideSchema.safeParse(exceptionWithoutDecree);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('committeeReference');
      }

      const validDecree = {
        ...exceptionWithoutDecree,
        committeeReference: 'SK Dewan Guru No. 88/SK-DG/2026',
      };
      expect(progressionOverrideSchema.safeParse(validDecree).success).toBe(true);
    });
  });

  describe('conferAchievementSchema - Honors & Subject Distinctions', () => {
    it('validates achievement conferral payload with valid calendar date', () => {
      const valid = {
        studentId: validUUID,
        academicPeriodId: validUUID,
        learningUnitId: validUUID,
        achievementType: 'subject_distinction',
        title: 'Nilai Tertinggi Matematika',
        description: 'Highest summative score in Mathematics Grade 4',
        criteriaMet: 'Average score >= 95.00',
        supportingEvidence: 'Final score 98.50',
        recognizedAt: '2026-11-07',
      };

      const result = conferAchievementSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('accepts all discrete achievement types', () => {
      ACHIEVEMENT_TYPES.forEach((type) => {
        const payload = {
          studentId: validUUID,
          academicPeriodId: validUUID,
          achievementType: type,
          title: `Award for ${type}`,
          criteriaMet: 'Criteria satisfied per handbook',
          recognizedAt: '2026-11-07',
        };
        const result = conferAchievementSchema.safeParse(payload);
        expect(result.success).toBe(true);
      });
    });

    it('rejects invalid recognition date formats', () => {
      const invalid = {
        studentId: validUUID,
        academicPeriodId: validUUID,
        achievementType: 'academic_honors',
        title: 'Juara Umum',
        criteriaMet: 'GPA 4.0',
        recognizedAt: '2026-02-31', // non-existent date
      };

      const result = conferAchievementSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });
  });

  describe('correctStandingSchema & correctProgressionSchema - Non-Grade Record Corrections', () => {
    it('requires reason with at least 10 characters for forensic compliance on standing correction', () => {
      const tooShort = {
        standing: 'good_standing',
        reason: 'Too short', // 9 chars
      };

      const result = correctStandingSchema.safeParse(tooShort);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].path).toContain('reason');
        expect(result.error.issues[0].message).toContain('at least 10 characters');
      }

      const emptyReason = {
        standing: 'good_standing',
        reason: '          ', // 10 spaces
      };
      expect(correctStandingSchema.safeParse(emptyReason).success).toBe(false);

      const valid = {
        standing: 'good_standing',
        reason: 'Corrected per re-moderation of unit math-01 results',
      };

      const validResult = correctStandingSchema.safeParse(valid);
      expect(validResult.success).toBe(true);
    });

    it('requires min 10 characters reason on progression correction', () => {
      const tooShort = {
        recommendation: 'promotion_eligible',
        reason: 'Typo fixed', // 10 chars, but let's test 8 chars
      };
      expect(correctProgressionSchema.safeParse({ ...tooShort, reason: 'Short' }).success).toBe(false);

      const valid = {
        recommendation: 'promotion_eligible',
        fromLevel: 'Grade 4',
        recommendedToLevel: 'Grade 5',
        reason: 'Corrected following official approval of grade remediation packet',
      };

      const result = correctProgressionSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });
  });

  describe('batch commitment schemas', () => {
    it('validates batch determine standing schema requiring non-empty items', () => {
      const emptyItems = {
        academicPeriodId: validUUID,
        items: [],
      };
      expect(batchDetermineStandingSchema.safeParse(emptyItems).success).toBe(false);

      const valid = {
        academicPeriodId: validUUID,
        items: [
          {
            studentId: validUUID,
            academicPeriodId: validUUID,
            standing: 'good_standing',
            policyReference: 'Policy 2026',
            isException: false,
          },
        ],
      };

      const result = batchDetermineStandingSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates batch issue progression schema requiring non-empty items', () => {
      const emptyItems = {
        academicPeriodId: validUUID,
        items: [],
      };
      expect(batchIssueProgressionSchema.safeParse(emptyItems).success).toBe(false);

      const valid = {
        academicPeriodId: validUUID,
        items: [
          {
            studentId: validUUID,
            academicPeriodId: validUUID,
            recommendation: 'promotion_eligible',
            fromLevel: 'Grade 4',
            recommendedToLevel: 'Grade 5',
            supportingEvidence: 'All units passed',
            isException: false,
          },
        ],
      };

      const result = batchIssueProgressionSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });
  });
});
