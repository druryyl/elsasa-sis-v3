import { describe, it, expect } from 'vitest';
import {
  createPayerSchema,
  searchPayersSchema,
  createPayerTypedSchema,
  searchPayersTypedSchema,
} from '../payers/schemas/payers.schema';

describe('payers.schema.spec.ts', () => {
  describe('createPayerSchema', () => {
    it('validates a complete valid payer creation payload', () => {
      const valid = {
        name: 'Yayasan Beasiswa Prestasi',
        type: 'foundation',
        identificationNumber: 'NPWP-99.123.456.7-001.000',
        email: 'treasury@yayasan-prestasi.org',
        phone: '+62 21 555 1234',
        billingAddress: 'Jl. Sudirman No. 45, Jakarta Selatan',
        bankAccountName: 'Yayasan Beasiswa Prestasi',
        bankAccountNumber: '0123456789',
        bankName: 'BCA',
        notes: 'Annual scholarship donor',
      };

      const result = createPayerSchema.safeParse(valid);
      expect(result.success).toBe(true);
    });

    it('validates minimal payload with name only', () => {
      const minimal = {
        name: 'Budi Santoso',
      };

      const result = createPayerSchema.safeParse(minimal);
      expect(result.success).toBe(true);
    });

    it('rejects empty payer name', () => {
      const invalid = {
        name: '',
      };

      const result = createPayerSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('Payer name is required');
      }
    });

    it('rejects invalid email format', () => {
      const invalid = {
        name: 'John Doe',
        email: 'not-an-email',
      };

      const result = createPayerSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects invalid phone format', () => {
      const invalid = {
        name: 'John Doe',
        phone: '123', // too short (<7 chars)
      };

      const result = createPayerSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('provides VeeValidate typed schema wrapper', () => {
      expect(createPayerTypedSchema).toBeDefined();
      expect(typeof createPayerTypedSchema.parse).toBe('function');
    });
  });

  describe('searchPayersSchema', () => {
    it('validates default search parameters', () => {
      const result = searchPayersSchema.safeParse({});
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.page).toBe(1);
        expect(result.data.pageSize).toBe(20);
      }
    });

    it('provides VeeValidate typed schema wrapper for search', () => {
      expect(searchPayersTypedSchema).toBeDefined();
    });
  });
});
