import { describe, it, expect } from 'vitest';
import {
  searchStudentAccountsSchema,
  studentAccountParamsSchema,
} from '../accounts/schemas/accounts.schema';

describe('accounts.schema.spec.ts', () => {
  describe('searchStudentAccountsSchema', () => {
    it('validates searchStudentAccountsSchema with defaults', () => {
      const result = searchStudentAccountsSchema.safeParse({});
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.pageNumber).toBe(1);
        expect(result.data.pageSize).toBe(20);
      }
    });

    it('validates valid search filter parameters', () => {
      const valid = {
        searchTerm: 'Budi',
        campus: 'Main Campus',
        grade: 'Grade 10',
        status: 'active',
        pageNumber: 2,
        pageSize: 50,
      };

      const result = searchStudentAccountsSchema.safeParse(valid);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.searchTerm).toBe('Budi');
        expect(result.data.pageNumber).toBe(2);
      }
    });

    it('rejects status exceeding maximum length', () => {
      const invalid = {
        status: 'A'.repeat(33),
      };

      const result = searchStudentAccountsSchema.safeParse(invalid);
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('cannot exceed 32 characters');
      }
    });

    it('rejects pageSize exceeding maximum limit', () => {
      const invalid = {
        pageSize: 500,
      };

      const result = searchStudentAccountsSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });

    it('rejects pageNumber less than 1', () => {
      const invalid = {
        pageNumber: 0,
      };

      const result = searchStudentAccountsSchema.safeParse(invalid);
      expect(result.success).toBe(false);
    });
  });

  describe('studentAccountParamsSchema', () => {
    it('validates studentAccountParamsSchema with valid UUID', () => {
      expect(
        studentAccountParamsSchema.safeParse({
          studentId: '11111111-1111-1111-1111-111111111111',
        }).success,
      ).toBe(true);
    });

    it('rejects non-UUID studentId', () => {
      expect(
        studentAccountParamsSchema.safeParse({
          studentId: 'invalid-id',
        }).success,
      ).toBe(false);
    });
  });
});
