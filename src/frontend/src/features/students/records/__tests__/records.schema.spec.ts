// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  updateIdentitySchema,
  addGuardianSchema,
  updateGuardianSchema,
  verifyDocumentSchema,
  changePlacementSchema,
  changeStatusSchema,
} from '../schemas/records.schema';

describe('Student Records Zod Schemas', () => {
  describe('updateIdentitySchema', () => {
    it('validates a complete valid student identity payload', () => {
      const valid = {
        fullName: 'Budi Santoso',
        preferredName: 'Budi',
        dateOfBirth: '2010-08-20',
        placeOfBirth: 'Jakarta',
        gender: 'Male',
        nationality: 'Indonesian',
        governmentIdType: 'NIK',
        governmentIdNumber: '3171012345670001',
        address: 'Jl. Sudirman No. 12',
        phone: '+6281234567890',
        email: 'budi@example.com',
        isIndependent: false,
      };

      const result = updateIdentitySchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects empty full legal name', () => {
      const invalid = {
        fullName: '',
      };

      const result = updateIdentitySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors[0]?.message).toContain('Full legal name is required');
      }
    });

    it('rejects future date of birth', () => {
      const futureDate = new Date();
      futureDate.setFullYear(futureDate.getFullYear() + 2);
      const isoFuture = futureDate.toISOString().slice(0, 10);

      const invalid = {
        fullName: 'Budi Santoso',
        dateOfBirth: isoFuture,
      };

      const result = updateIdentitySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const dobError = result.error.errors.find((e) => e.path.includes('dateOfBirth'));
        expect(dobError?.message).toBe('Date of birth cannot be in the future.');
      }
    });

    it('rejects invalid email address', () => {
      const invalid = {
        fullName: 'Budi Santoso',
        email: 'invalid-email-address',
      };

      const result = updateIdentitySchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const emailError = result.error.errors.find((e) => e.path.includes('email'));
        expect(emailError?.message).toBe('Email must be a valid email address.');
      }
    });
  });

  describe('addGuardianSchema', () => {
    it('validates a valid guardian relationship creation payload', () => {
      const valid = {
        fullName: 'Siti Rahma',
        relationshipType: 'mother',
        phone: '+62811223344',
        email: 'siti@example.com',
        isPrimaryContact: true,
      };

      const result = addGuardianSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects an unsupported relationship type', () => {
      const invalid = {
        fullName: 'Siti Rahma',
        relationshipType: 'neighbor',
      };

      const result = addGuardianSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const relError = result.error.errors.find((e) => e.path.includes('relationshipType'));
        expect(relError?.message).toContain('Relationship type must be one of');
      }
    });

    it('rejects empty guardian name', () => {
      const invalid = {
        fullName: '',
        relationshipType: 'father',
      };

      const result = addGuardianSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors[0]?.message).toContain('Guardian full name is required');
      }
    });
  });

  describe('updateGuardianSchema', () => {
    it('validates a valid guardian update payload', () => {
      const valid = {
        relationshipType: 'father',
        isPrimaryContact: false,
        isEmergencyContact: true,
        isFinancialContact: true,
        isAuthorizedPickup: true,
        isActive: true,
        phone: '+62811998877',
      };

      const result = updateGuardianSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects an invalid relationship type during update', () => {
      const invalid = {
        relationshipType: 'cousin',
      };

      const result = updateGuardianSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const relError = result.error.errors.find((e) => e.path.includes('relationshipType'));
        expect(relError?.message).toContain('Relationship type must be one of');
      }
    });
  });

  describe('verifyDocumentSchema', () => {
    it('accepts verified status', () => {
      const valid = {
        submissionStatus: 'verified',
        notes: 'Original seen and verified.',
      };

      const result = verifyDocumentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('accepts rejected status', () => {
      const valid = {
        submissionStatus: 'rejected',
        notes: 'Illegible photocopy.',
      };

      const result = verifyDocumentSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects invalid status', () => {
      const invalid = {
        submissionStatus: 'pending',
      };

      const result = verifyDocumentSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors[0]?.message).toContain(
          "Submission status must be either 'verified' or 'rejected'",
        );
      }
    });
  });

  describe('changePlacementSchema', () => {
    const validPayload = {
      campus: 'North Campus',
      schoolLevel: 'Secondary',
      grade: 'Grade 8',
      academicYear: '2026/2027',
      effectiveDate: '2026-08-01',
      program: 'Regular',
      stream: 'Science',
      cohort: '2026',
      homeroom: 'Room 8-B',
      reason: 'Mid-year campus reassignment',
      authorizedBy: 'principal@elsasa.sch.id',
      decisionReference: 'DIR-2026-012',
    };

    it('validates a complete valid placement payload', () => {
      const result = changePlacementSchema.safeParse(validPayload);
      expect(result.success).toBe(true);
    });

    it('validates minimal required fields with optional fields omitted or null/empty', () => {
      const minimalPayload = {
        campus: 'North Campus',
        schoolLevel: 'Secondary',
        grade: 'Grade 8',
        academicYear: '2026/2027',
        effectiveDate: '2026-08-01',
        program: '',
        stream: null,
        cohort: undefined,
        homeroom: '',
        reason: null,
        authorizedBy: '',
        decisionReference: null,
      };

      const result = changePlacementSchema.safeParse(minimalPayload);
      expect(result.success).toBe(true);
    });

    it('rejects missing or empty required fields', () => {
      const requiredFields: Array<keyof typeof validPayload> = [
        'campus',
        'schoolLevel',
        'grade',
        'academicYear',
        'effectiveDate',
      ];

      for (const field of requiredFields) {
        const payloadMissing = { ...validPayload };
        delete (payloadMissing as Record<string, unknown>)[field];

        const missingResult = changePlacementSchema.safeParse(payloadMissing);
        expect(missingResult.success).toBe(false);

        const payloadEmpty = { ...validPayload, [field]: '   ' };
        const emptyResult = changePlacementSchema.safeParse(payloadEmpty);
        expect(emptyResult.success).toBe(false);
      }
    });

    it('rejects malformed effective date', () => {
      const invalidDates = ['2026/08/01', '01-08-2026', '2026-8-1', 'not-a-date'];

      for (const date of invalidDates) {
        const result = changePlacementSchema.safeParse({
          ...validPayload,
          effectiveDate: date,
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const dateError = result.error.errors.find((e) => e.path.includes('effectiveDate'));
          expect(dateError?.message).toBe('Effective date must be in YYYY-MM-DD format.');
        }
      }
    });

    it('enforces string length boundaries', () => {
      // campus > 100
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          campus: 'a'.repeat(101),
        }).success,
      ).toBe(false);

      // schoolLevel > 64
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          schoolLevel: 'a'.repeat(65),
        }).success,
      ).toBe(false);

      // grade > 64
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          grade: 'a'.repeat(65),
        }).success,
      ).toBe(false);

      // academicYear > 32
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          academicYear: 'a'.repeat(33),
        }).success,
      ).toBe(false);

      // program > 100
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          program: 'a'.repeat(101),
        }).success,
      ).toBe(false);

      // stream > 100
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          stream: 'a'.repeat(101),
        }).success,
      ).toBe(false);

      // cohort > 50
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          cohort: 'a'.repeat(51),
        }).success,
      ).toBe(false);

      // homeroom > 100
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          homeroom: 'a'.repeat(101),
        }).success,
      ).toBe(false);

      // reason > 500
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          reason: 'a'.repeat(501),
        }).success,
      ).toBe(false);

      // authorizedBy > 100
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          authorizedBy: 'a'.repeat(101),
        }).success,
      ).toBe(false);

      // decisionReference > 100
      expect(
        changePlacementSchema.safeParse({
          ...validPayload,
          decisionReference: 'a'.repeat(101),
        }).success,
      ).toBe(false);
    });
  });

  describe('changeStatusSchema', () => {
    it('accepts valid statuses: active and enrolled', () => {
      const activeResult = changeStatusSchema.safeParse({
        status: 'active',
        effectiveDate: '2026-08-01',
        reason: 'Student activated upon academic session start',
        authorizedBy: 'admin@elsasa.sch.id',
        decisionReference: 'DEC-001',
      });
      expect(activeResult.success).toBe(true);

      const enrolledResult = changeStatusSchema.safeParse({
        status: 'enrolled',
        effectiveDate: '2026-08-01',
      });
      expect(enrolledResult.success).toBe(true);
    });

    it('rejects unsupported or invalid statuses', () => {
      const unsupported = ['withdrawn', 'graduated', 'on_leave', 'pending', 'inactive', ''];

      for (const st of unsupported) {
        const result = changeStatusSchema.safeParse({
          status: st,
          effectiveDate: '2026-08-01',
        });
        expect(result.success).toBe(false);
        if (!result.success) {
          const statusError = result.error.errors.find((e) => e.path.includes('status'));
          expect(statusError?.message).toBe("Status must be either 'active' or 'enrolled'.");
        }
      }
    });

    it('rejects empty or malformed effective date', () => {
      const emptyResult = changeStatusSchema.safeParse({
        status: 'active',
        effectiveDate: '',
      });
      expect(emptyResult.success).toBe(false);

      const missingResult = changeStatusSchema.safeParse({
        status: 'active',
      });
      expect(missingResult.success).toBe(false);

      const malformedResult = changeStatusSchema.safeParse({
        status: 'active',
        effectiveDate: '01/08/2026',
      });
      expect(malformedResult.success).toBe(false);
      if (!malformedResult.success) {
        const dateError = malformedResult.error.errors.find((e) => e.path.includes('effectiveDate'));
        expect(dateError?.message).toBe('Effective date must be in YYYY-MM-DD format.');
      }
    });

    it('enforces string length boundaries on optional fields', () => {
      // reason > 1000
      expect(
        changeStatusSchema.safeParse({
          status: 'active',
          effectiveDate: '2026-08-01',
          reason: 'a'.repeat(1001),
        }).success,
      ).toBe(false);

      // authorizedBy > 100
      expect(
        changeStatusSchema.safeParse({
          status: 'active',
          effectiveDate: '2026-08-01',
          authorizedBy: 'a'.repeat(101),
        }).success,
      ).toBe(false);

      // decisionReference > 100
      expect(
        changeStatusSchema.safeParse({
          status: 'active',
          effectiveDate: '2026-08-01',
          decisionReference: 'a'.repeat(101),
        }).success,
      ).toBe(false);
    });
  });
});
