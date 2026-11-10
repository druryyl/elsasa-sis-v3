// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  registerTeacherSchema,
  updateTeacherSchema,
  changeStandingSchema,
  addCapabilitySchema,
  submitEvidenceSchema,
  verifyEvidenceSchema,
  rejectEvidenceSchema,
  grantAuthorizationSchema,
  updateAuthorizationStatusSchema,
  addSupervisionSchema,
  resolveSupervisionSchema,
  recordCorrectionSchema,
} from '../api/teachingSchemas';

describe('Teaching Zod Schemas Validation', () => {
  describe('registerTeacherSchema', () => {
    const validBase = {
      fullName: 'Budi Hartono',
      displayName: 'Dr. Budi Hartono, M.Pd.',
      teacherNumber: 'TCH-2026-001',
      employeeRef: 'EMP-001',
      nationalIdOrExternalRef: '3201010101010001',
      email: 'budi.hartono@hagios.sch.id',
      phone: '+62 812-3456-7890',
      teachingRoleId: 'role-123',
      campusAffiliation: 'Main Campus',
      department: 'Mathematics',
      effectiveFrom: '2026-07-01',
      effectiveTo: '2027-06-30',
      notes: 'Initial registration candidate',
    };

    it('validates a complete valid teacher registration payload', () => {
      const result = registerTeacherSchema.safeParse(validBase);
      expect(result.success).toBe(true);
    });

    it('validates a minimal valid payload with optional fields omitted or empty', () => {
      const minimal = {
        fullName: 'Jane Doe',
        teachingRoleId: 'role-456',
        campusAffiliation: 'North Campus',
        effectiveFrom: '2026-07-01',
      };
      const result = registerTeacherSchema.safeParse(minimal);
      expect(result.success).toBe(true);
    });

    it('rejects missing or empty full name', () => {
      const invalid = { ...validBase, fullName: '   ' };
      const result = registerTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('fullName'))).toBe(true);
      }
    });

    it('rejects invalid email format', () => {
      const invalid = { ...validBase, email: 'not-an-email-address' };
      const result = registerTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('email'))).toBe(true);
      }
    });

    it('allows empty string or null for email and phone', () => {
      const withNulls = {
        ...validBase,
        email: '',
        phone: null,
      };
      const result = registerTeacherSchema.safeParse(withNulls);
      expect(result.success).toBe(true);
    });

    it('rejects invalid phone characters', () => {
      const invalid = { ...validBase, phone: 'INVALID_PHONE_#123' };
      const result = registerTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('phone'))).toBe(true);
      }
    });

    it('rejects teacherNumber format when containing forbidden characters', () => {
      const invalid = { ...validBase, teacherNumber: 'TCH@2026!#$' };
      const result = registerTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('teacherNumber'))).toBe(true);
      }
    });

    it('rejects invalid effectiveFrom date format', () => {
      const invalid = { ...validBase, effectiveFrom: '01/07/2026' };
      const result = registerTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('effectiveFrom'))).toBe(true);
      }
    });

    it('rejects effectiveTo earlier than effectiveFrom', () => {
      const invalid = {
        ...validBase,
        effectiveFrom: '2026-07-01',
        effectiveTo: '2026-06-30',
      };
      const result = registerTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('effectiveTo'))).toBe(true);
      }
    });
  });

  describe('updateTeacherSchema', () => {
    it('validates partial updates with valid data', () => {
      const payload = {
        displayName: 'Prof. Budi Hartono',
        email: 'budi.new@hagios.sch.id',
        department: 'Advanced Sciences',
      };
      const result = updateTeacherSchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('rejects invalid email address on update', () => {
      const invalid = { email: 'bad-email' };
      const result = updateTeacherSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('email'))).toBe(true);
      }
    });
  });

  describe('changeStandingSchema', () => {
    it('validates all allowable operational standings', () => {
      const allowable = ['active', 'restricted', 'suspended', 'inactive'];
      for (const standing of allowable) {
        const result = changeStandingSchema.safeParse({
          standing,
          standingReason: 'Valid administrative rationale provided.',
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects pending_approval or unknown standings for manual change', () => {
      const invalidStanding = changeStandingSchema.safeParse({
        standing: 'pending_approval',
        standingReason: 'Reverting state',
      });
      expect(invalidStanding.success).toBe(false);

      const unknownStanding = changeStandingSchema.safeParse({
        standing: 'unknown_state',
        standingReason: 'Invalid',
      });
      expect(unknownStanding.success).toBe(false);
    });

    it('rejects empty or whitespace standingReason', () => {
      const result = changeStandingSchema.safeParse({
        standing: 'restricted',
        standingReason: '   ',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('standingReason'))).toBe(true);
      }
    });
  });

  describe('addCapabilitySchema', () => {
    it('validates a complete capability definition', () => {
      const payload = {
        capabilityTypeId: 'cap-type-01',
        capabilityCode: 'MATH_SEC',
        capabilityName: 'Secondary Mathematics',
        applicableLevel: 'Grade 10-12',
        deliveryMode: 'classroom_onsite',
        languageOfInstruction: 'English',
        notes: 'Cambridge curriculum accredited',
      };
      const result = addCapabilitySchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('rejects missing required identifiers', () => {
      const result = addCapabilitySchema.safeParse({
        capabilityTypeId: '',
        capabilityCode: '',
        capabilityName: '',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.length).toBeGreaterThanOrEqual(3);
      }
    });
  });

  describe('submitEvidenceSchema', () => {
    const validEvidence = {
      qualificationTypeId: 'qtype-01',
      title: 'Sertifikat Pendidik (Serdik)',
      issuingAuthority: 'Kementerian Pendidikan',
      credentialIdentifier: 'SERDIK-2022-9988',
      issueDate: '2022-05-15',
      expiryDate: '2027-05-14',
      documentReference: 'https://archive.hagios.sch.id/serdik/budi.pdf',
    };

    it('validates a valid qualification documentary submission', () => {
      const result = submitEvidenceSchema.safeParse(validEvidence);
      expect(result.success).toBe(true);
    });

    it('rejects missing title or issuing authority', () => {
      const invalid = { ...validEvidence, title: '', issuingAuthority: '' };
      const result = submitEvidenceSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects expiryDate earlier than issueDate', () => {
      const invalid = {
        ...validEvidence,
        issueDate: '2024-01-01',
        expiryDate: '2023-12-31',
      };
      const result = submitEvidenceSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('expiryDate'))).toBe(true);
      }
    });
  });

  describe('verifyEvidenceSchema and rejectEvidenceSchema', () => {
    it('verifyEvidenceSchema allows optional notes or empty string', () => {
      expect(verifyEvidenceSchema.safeParse({}).success).toBe(true);
      expect(verifyEvidenceSchema.safeParse({ verificationNotes: 'Authenticated against PDDIKTI' }).success).toBe(true);
      expect(verifyEvidenceSchema.safeParse({ verificationNotes: '' }).success).toBe(true);
    });

    it('rejectEvidenceSchema enforces non-empty rejectionReason', () => {
      const valid = rejectEvidenceSchema.safeParse({
        rejectionReason: 'Scan resolution too low; text unreadable.',
      });
      expect(valid.success).toBe(true);

      const invalid = rejectEvidenceSchema.safeParse({ rejectionReason: '   ' });
      expect(invalid.success).toBe(false);
      if (!invalid.success) {
        expect(invalid.error.issues.some((i) => i.path.includes('rejectionReason'))).toBe(true);
      }
    });
  });

  describe('grantAuthorizationSchema', () => {
    const validActiveAuth = {
      scopeDescription: 'Senior Physics and Mathematics Grade 10-12',
      campusScope: 'Main Campus',
      effectiveStartDate: '2026-07-01',
      effectiveEndDate: '2027-06-30',
      status: 'active' as const,
      linkedEvidenceIds: ['ev-01', 'ev-02'],
    };

    it('validates active authorization with scope and dates', () => {
      const result = grantAuthorizationSchema.safeParse(validActiveAuth);
      expect(result.success).toBe(true);
    });

    it('accepts authorizationScope and effectiveFrom / effectiveTo aliases', () => {
      const aliasPayload = {
        authorizationScope: 'Junior Science Grade 7-9',
        campusBoundary: 'North Campus',
        effectiveFrom: '2026-07-01',
        effectiveTo: '2027-06-30',
        status: 'active' as const,
      };
      const result = grantAuthorizationSchema.safeParse(aliasPayload);
      expect(result.success).toBe(true);
    });

    it('rejects payload when both scopeDescription and authorizationScope are empty', () => {
      const invalid = {
        ...validActiveAuth,
        scopeDescription: '   ',
        authorizationScope: '',
      };
      const result = grantAuthorizationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('scopeDescription'))).toBe(true);
      }
    });

    it('rejects effectiveEndDate earlier than effectiveStartDate', () => {
      const invalid = {
        ...validActiveAuth,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-06-30',
      };
      const result = grantAuthorizationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('effectiveEndDate'))).toBe(true);
      }
    });

    it('requires supervision requirement when status is authorized_under_supervision', () => {
      const invalid = {
        ...validActiveAuth,
        status: 'authorized_under_supervision' as const,
      };
      const result = grantAuthorizationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('supervision'))).toBe(true);
      }
    });

    it('accepts authorized_under_supervision when nested supervision object is provided', () => {
      const validUnderSupervision = {
        ...validActiveAuth,
        status: 'authorized_under_supervision' as const,
        supervision: {
          supervisionType: 'co_teaching' as const,
          description: 'Weekly co-teaching sessions with Senior Mentor',
          startDate: '2026-07-01',
          endDate: '2026-12-31',
          supervisingTeacherId: 'mentor-001',
        },
      };
      const result = grantAuthorizationSchema.safeParse(validUnderSupervision);
      expect(result.success).toBe(true);
    });

    it('accepts authorized_under_supervision when flat supervision fields are provided', () => {
      const validWithFlatSupervision = {
        ...validActiveAuth,
        status: 'authorized_under_supervision' as const,
        supervisionType: 'periodic_observation',
        supervisionDescription: 'Bi-weekly lesson plan review and observation',
        supervisionStartDate: '2026-07-01',
        supervisionEndDate: '2026-12-31',
      };
      const result = grantAuthorizationSchema.safeParse(validWithFlatSupervision);
      expect(result.success).toBe(true);
    });

    it('rejects nested supervision when endDate is earlier than startDate', () => {
      const invalid = {
        ...validActiveAuth,
        status: 'authorized_under_supervision' as const,
        supervision: {
          supervisionType: 'co_teaching' as const,
          description: 'Weekly co-teaching',
          startDate: '2026-07-01',
          endDate: '2026-06-01',
        },
      };
      const result = grantAuthorizationSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });
  });

  describe('updateAuthorizationStatusSchema', () => {
    it('validates supported lifecycle status transitions with mandatory reason', () => {
      const allowable = [
        'active',
        'authorized_under_supervision',
        'restricted',
        'suspended',
        'revoked',
        'expired',
      ];
      for (const status of allowable) {
        const result = updateAuthorizationStatusSchema.safeParse({
          status,
          statusReason: 'Administrative adjustment per Academic Council decision',
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects missing or empty statusReason', () => {
      const invalid = updateAuthorizationStatusSchema.safeParse({
        status: 'suspended',
        statusReason: '',
      });
      expect(invalid.success).toBe(false);
      if (!invalid.success) {
        expect(invalid.error.issues.some((i) => i.path.includes('statusReason'))).toBe(true);
      }
    });
  });

  describe('addSupervisionSchema and resolveSupervisionSchema', () => {
    it('validates addSupervisionSchema payload', () => {
      const payload = {
        supervisionType: 'lesson_plan_review',
        description: 'Review lesson plans weekly before delivery',
        startDate: '2026-08-01',
        endDate: '2026-12-20',
        supervisingTeacherId: 'mentor-123',
      };
      const result = addSupervisionSchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('validates resolveSupervisionSchema with mandatory notes', () => {
      const valid = resolveSupervisionSchema.safeParse({
        resolutionStatus: 'completed',
        resolutionNotes: 'Successfully completed the 6-month mentoring cycle with distinction.',
        updateAuthorizationToActive: true,
      });
      expect(valid.success).toBe(true);

      const invalid = resolveSupervisionSchema.safeParse({
        resolutionStatus: 'completed',
        resolutionNotes: '  ',
      });
      expect(invalid.success).toBe(false);
    });
  });

  describe('recordCorrectionSchema', () => {
    const validBase = {
      targetTableName: 'teacher_profiles',
      targetRecordId: 'tch-profile-001',
      fieldName: 'email',
      originalValue: 'old.address@hagios.sch.id',
      correctedValue: 'new.address@hagios.sch.id',
      reasonForCorrection: 'Updated official email per IT domain migration.',
      downstreamImpactReviewed: true,
    };

    it('validates a valid record correction delta', () => {
      const result = recordCorrectionSchema.safeParse(validBase);
      expect(result.success).toBe(true);
    });

    it('accepts schema-prefixed target table names (e.g., teaching.teacher_profiles)', () => {
      const prefixed = {
        ...validBase,
        targetTableName: 'teaching.teacher_profiles',
      };
      const result = recordCorrectionSchema.safeParse(prefixed);
      expect(result.success).toBe(true);
    });

    it('rejects unauthorized target tables outside audit scope', () => {
      const invalidTable = {
        ...validBase,
        targetTableName: 'students_master',
      };
      const result = recordCorrectionSchema.safeParse(invalidTable);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('targetTableName'))).toBe(true);
      }
    });

    it('rejects identical corrected value and original value (no-op delta)', () => {
      const invalid = {
        ...validBase,
        originalValue: 'same@sch.id',
        correctedValue: 'same@sch.id',
      };
      const result = recordCorrectionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('correctedValue'))).toBe(true);
      }
    });

    it('allows empty or null originalValue when setting a previously null field', () => {
      const validEmptyOrig = {
        ...validBase,
        originalValue: null,
        correctedValue: 'brand.new@hagios.sch.id',
      };
      const result = recordCorrectionSchema.safeParse(validEmptyOrig);
      expect(result.success).toBe(true);
    });

    it('rejects missing or empty reasonForCorrection', () => {
      const invalid = {
        ...validBase,
        reasonForCorrection: '   ',
      };
      const result = recordCorrectionSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path.includes('reasonForCorrection'))).toBe(true);
      }
    });
  });
});
