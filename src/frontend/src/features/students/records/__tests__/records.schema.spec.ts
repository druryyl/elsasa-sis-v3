// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  updateIdentitySchema,
  addGuardianSchema,
  updateGuardianSchema,
  verifyDocumentSchema,
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
});
