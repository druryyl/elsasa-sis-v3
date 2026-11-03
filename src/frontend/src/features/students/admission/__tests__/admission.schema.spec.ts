// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  contactSchema,
  registerApplicationSchema,
  recordDecisionSchema,
  assessmentEvidenceSchema,
  verifyDocumentSchema,
  trackStatusSchema,
} from '../schemas/admission.schema';

describe('Admission Module Zod Schemas', () => {
  describe('contactSchema', () => {
    it('validates a valid primary contact with phone', () => {
      const valid = {
        fullName: 'Jane Doe',
        relationship: 'Mother',
        phone: '+628123456789',
        email: '',
        isPrimary: true,
      };

      const result = contactSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates a valid contact with email and no phone', () => {
      const valid = {
        fullName: 'John Doe',
        relationship: 'Father',
        email: 'john.doe@example.com',
        isPrimary: false,
      };

      const result = contactSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects contact missing both phone and email', () => {
      const invalid = {
        fullName: 'John Doe',
        relationship: 'Father',
        phone: '',
        email: '',
        isPrimary: true,
      };

      const result = contactSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors[0]?.message).toContain(
          'A contact must have either a phone number or an email address',
        );
      }
    });

    it('rejects missing full name or relationship', () => {
      const invalid = {
        fullName: '',
        relationship: '',
        phone: '+6281111111',
      };

      const result = contactSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });
  });

  describe('registerApplicationSchema', () => {
    const validPayload = {
      fullName: 'Ahmad Dahlan',
      dateOfBirth: '2015-05-20',
      gender: 'Male',
      governmentIdType: 'NIK',
      governmentIdNumber: '3201234567890001',
      admissionPeriodId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      requestedEntryLevel: 'Grade 1',
      contacts: [
        {
          fullName: 'Siti Walidah',
          relationship: 'Mother',
          phone: '+6281234567890',
          email: 'siti@example.com',
          isPrimary: true,
        },
      ],
    };

    it('validates a complete and correct registration payload', () => {
      const result = registerApplicationSchema.safeParse(validPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.nationality).toBe('Indonesian');
        expect(result.data.applicationType).toBe('new');
      }
    });

    it('rejects payload when full name is empty', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        fullName: '   ',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors.some((e) => e.path.includes('fullName'))).toBe(true);
      }
    });

    it('rejects payload when date of birth is missing or empty', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        dateOfBirth: '',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors.some((e) => e.path.includes('dateOfBirth'))).toBe(true);
      }
    });

    it('rejects payload when requested entry level is missing or empty', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        requestedEntryLevel: '   ',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors.some((e) => e.path.includes('requestedEntryLevel'))).toBe(true);
      }
    });

    it('rejects invalid applicant email format', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        email: 'invalid-applicant-email',
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.errors.some(
            (e) => e.path.includes('email') && e.message.includes('valid email address'),
          ),
        ).toBe(true);
      }
    });

    it('accepts valid applicant email format', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        email: 'applicant.valid@example.com',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.email).toBe('applicant.valid@example.com');
      }
    });

    it('rejects invalid contact email format', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        contacts: [
          {
            fullName: 'Guardian One',
            relationship: 'Mother',
            phone: '+6281234567890',
            email: 'bad-email-format',
            isPrimary: true,
          },
        ],
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.errors.some(
            (e) => e.path.includes('email') && e.message.includes('valid email address'),
          ),
        ).toBe(true);
      }
    });

    it('rejects invalid NIK / government ID number exceeding max characters', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        governmentIdNumber: '1'.repeat(65),
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.errors.some(
            (e) =>
              e.path.includes('governmentIdNumber') &&
              e.message.includes('cannot exceed 64 characters'),
          ),
        ).toBe(true);
      }
    });

    it('accepts valid NIK format within character limits', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        governmentIdNumber: '3201234567890001',
      });
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.governmentIdNumber).toBe('3201234567890001');
      }
    });

    it('rejects future date of birth', () => {
      const futureDate = new Date();
      futureDate.setFullYear(futureDate.getFullYear() + 2);
      const isoDate = futureDate.toISOString().split('T')[0]!;

      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        dateOfBirth: isoDate,
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors.some((e) => e.message.includes('future'))).toBe(true);
      }
    });

    it('rejects empty contacts list', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        contacts: [],
      });
      expect(result.success).toBe(false);
    });

    it('rejects contacts list without a primary contact', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        contacts: [
          {
            fullName: 'Secondary Contact',
            relationship: 'Uncle',
            phone: '+6289999999',
            isPrimary: false,
          },
        ],
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.errors.some((e) => e.message.includes('primary guardian contact')),
        ).toBe(true);
      }
    });

    it('rejects invalid admission period UUID', () => {
      const result = registerApplicationSchema.safeParse({
        ...validPayload,
        admissionPeriodId: 'invalid-guid',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('recordDecisionSchema', () => {
    it('validates standard accepted decision', () => {
      const payload = {
        decisionType: 'accepted',
        decisionDate: '2026-05-15',
        effectiveDate: '2026-07-01',
        reason: 'Candidate meets all entrance requirements.',
        supportingReference: 'MEETING-MINUTES-2026-05',
      };

      const result = recordDecisionSchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('validates deferred decision with target deferred admission period', () => {
      const payload = {
        decisionType: 'deferred',
        decisionDate: '2026-05-15',
        effectiveDate: '2027-01-10',
        deferredAdmissionPeriodId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      };

      const result = recordDecisionSchema.safeParse(payload);
      expect(result.success).toBe(true);
    });

    it('rejects deferred decision when deferredAdmissionPeriodId is omitted', () => {
      const payload = {
        decisionType: 'deferred',
        decisionDate: '2026-05-15',
        effectiveDate: '2027-01-10',
      };

      const result = recordDecisionSchema.safeParse(payload);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.errors.some((e) =>
            e.message.includes("Deferred admission period is required when decision type is 'deferred'"),
          ),
        ).toBe(true);
      }
    });

    it('rejects deferred decision when deferredAdmissionPeriodId is not a valid UUID', () => {
      const payload = {
        decisionType: 'deferred',
        decisionDate: '2026-05-15',
        effectiveDate: '2027-01-10',
        deferredAdmissionPeriodId: 'non-uuid-string',
      };

      const result = recordDecisionSchema.safeParse(payload);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(
          result.error.errors.some(
            (e) =>
              e.path.includes('deferredAdmissionPeriodId') &&
              e.message.includes('valid UUID'),
          ),
        ).toBe(true);
      }
    });

    it('rejects invalid decision type', () => {
      const payload = {
        decisionType: 'unknown_status',
        decisionDate: '2026-05-15',
        effectiveDate: '2026-07-01',
      };

      const result = recordDecisionSchema.safeParse(payload);
      expect(result.success).toBe(false);
    });
  });

  describe('assessmentEvidenceSchema', () => {
    it('validates valid assessment evidence', () => {
      const payload = {
        assessmentType: 'reading_readiness',
        score: '85/100',
        source: 'External Diagnostic Center',
        assessmentDate: '2026-04-10',
        notes: 'Proficient in early literacy and phonics.',
      };

      const result = assessmentEvidenceSchema.safeParse(payload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.assessmentType).toBe('reading_readiness');
      }
    });

    it('rejects missing assessment type', () => {
      const payload = {
        assessmentType: '',
        score: 'Pass',
      };

      const result = assessmentEvidenceSchema.safeParse(payload);
      expect(result.success).toBe(false);
    });

    it('rejects score exceeding 50 characters', () => {
      const payload = {
        assessmentType: 'Math',
        score: 'A'.repeat(51),
      };

      const result = assessmentEvidenceSchema.safeParse(payload);
      expect(result.success).toBe(false);
    });
  });

  describe('verifyDocumentSchema', () => {
    it('accepts verified status', () => {
      const result = verifyDocumentSchema.safeParse({
        submissionStatus: 'verified',
        notes: 'Certificate is authentic.',
      });
      expect(result.success).toBe(true);
    });

    it('accepts rejected status with notes', () => {
      const result = verifyDocumentSchema.safeParse({
        submissionStatus: 'rejected',
        notes: 'Illegible photocopy.',
      });
      expect(result.success).toBe(true);
    });

    it('rejects invalid submission status', () => {
      const result = verifyDocumentSchema.safeParse({
        submissionStatus: 'pending',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('trackStatusSchema', () => {
    it('accepts valid application number and date of birth', () => {
      const result = trackStatusSchema.safeParse({
        applicationNumber: 'ADM-2026-0001',
        dateOfBirth: '2015-08-25',
      });
      expect(result.success).toBe(true);
    });

    it('rejects missing application number', () => {
      const result = trackStatusSchema.safeParse({
        applicationNumber: '',
        dateOfBirth: '2015-08-25',
      });
      expect(result.success).toBe(false);
    });

    it('rejects invalid date format', () => {
      const result = trackStatusSchema.safeParse({
        applicationNumber: 'ADM-2026-0001',
        dateOfBirth: '25-08-2015',
      });
      expect(result.success).toBe(false);
    });
  });
});
