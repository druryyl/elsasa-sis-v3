// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  enrollAcceptedApplicantSchema,
  enrollGuardianSchema,
} from '../schemas/enrollment.schema';

describe('Enrollment Module Zod Schemas', () => {
  describe('enrollGuardianSchema', () => {
    it('validates a valid guardian payload', () => {
      const valid = {
        fullName: 'Jane Doe',
        relationshipType: 'mother',
        phone: '+628123456789',
        email: 'jane.doe@example.com',
        isPrimaryContact: true,
      };

      const result = enrollGuardianSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects missing guardian full name', () => {
      const invalid = {
        fullName: '',
        relationshipType: 'father',
      };

      const result = enrollGuardianSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors[0]?.message).toContain('Guardian full name is required');
      }
    });

    it('rejects invalid email format', () => {
      const invalid = {
        fullName: 'John Doe',
        relationshipType: 'father',
        email: 'not-an-email',
      };

      const result = enrollGuardianSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.errors[0]?.message).toContain('Email must be a valid email address');
      }
    });
  });

  describe('enrollAcceptedApplicantSchema', () => {
    it('validates a valid payload with payment reference when payment is not waived', () => {
      const valid = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: 'Main Campus',
        initialLevel: 'Grade 10',
        initialAcademicYear: '2026/2027',
        paymentWaived: false,
        paymentReference: 'BANK-TRANSFER-991',
      };

      const result = enrollAcceptedApplicantSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates a valid payload with waiver reference when payment is waived', () => {
      const valid = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: 'Main Campus',
        initialLevel: 'Grade 10',
        initialAcademicYear: '2026/2027',
        paymentWaived: true,
        waiverReference: 'SCHOLARSHIP-DECREE-2026-A',
      };

      const result = enrollAcceptedApplicantSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates when checkFinanceModule is true without explicit payment reference', () => {
      const valid = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: 'Main Campus',
        initialLevel: 'Grade 10',
        initialAcademicYear: '2026/2027',
        paymentWaived: false,
        checkFinanceModule: true,
      };

      const result = enrollAcceptedApplicantSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('rejects when paymentWaived is true but waiverReference is missing', () => {
      const invalid = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: 'Main Campus',
        initialLevel: 'Grade 10',
        initialAcademicYear: '2026/2027',
        paymentWaived: true,
        waiverReference: '',
      };

      const result = enrollAcceptedApplicantSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const waiverErr = result.error.errors.find((e) => e.path.includes('waiverReference'));
        expect(waiverErr?.message).toBe('Waiver reference is required when payment is waived.');
      }
    });

    it('rejects when paymentWaived is false and paymentReference is missing', () => {
      const invalid = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: 'Main Campus',
        initialLevel: 'Grade 10',
        initialAcademicYear: '2026/2027',
        paymentWaived: false,
        paymentReference: '',
      };

      const result = enrollAcceptedApplicantSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const payErr = result.error.errors.find((e) => e.path.includes('paymentReference'));
        expect(payErr?.message).toBe('Payment reference is required when payment is not waived.');
      }
    });

    it('rejects missing placement attributes', () => {
      const invalid = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: '',
        initialLevel: '',
        initialAcademicYear: '',
        paymentWaived: false,
        paymentReference: 'PAY-123',
      };

      const result = enrollAcceptedApplicantSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.errors.map((e) => e.path[0]);
        expect(paths).toContain('initialCampus');
        expect(paths).toContain('initialLevel');
        expect(paths).toContain('initialAcademicYear');
      }
    });
  });
});
