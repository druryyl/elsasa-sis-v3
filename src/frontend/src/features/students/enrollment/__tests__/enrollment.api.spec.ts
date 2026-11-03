// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import { enrollAcceptedApplicant } from '../api/enrollment.api';
import type { EnrollAcceptedApplicantPayload } from '../types/enrollment.types';

describe('Enrollment API Client (enrollment.api.ts)', () => {
  let capturedConfig: InternalAxiosRequestConfig | null = null;
  const originalAdapter = apiClient.defaults.adapter;

  beforeEach(() => {
    capturedConfig = null;
  });

  afterEach(() => {
    if (originalAdapter) {
      apiClient.defaults.adapter = originalAdapter;
    } else {
      delete apiClient.defaults.adapter;
    }
    vi.restoreAllMocks();
  });

  describe('enrollAcceptedApplicant', () => {
    it('calls POST /students/enrollments with payload and returns response', async () => {
      const mockPayload: EnrollAcceptedApplicantPayload = {
        applicantId: '11111111-1111-1111-1111-111111111111',
        academicYearId: '22222222-2222-2222-2222-222222222222',
        initialCampus: 'Main Campus',
        initialLevel: 'Grade 10',
        initialAcademicYear: '2026/2027',
        paymentWaived: false,
        paymentReference: 'PAY-REC-9988',
      };

      const mockResponse = {
        studentId: '33333333-3333-3333-3333-333333333333',
        studentNumber: 'STD-2026-0001',
        enrollmentId: '44444444-4444-4444-4444-444444444444',
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockResponse,
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        };
      };

      const result = await enrollAcceptedApplicant(mockPayload);

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig?.method?.toLowerCase()).toBe('post');
      expect(capturedConfig?.url).toBe('/students/enrollments');
      expect(JSON.parse(capturedConfig?.data as string)).toEqual(mockPayload);
      expect(result).toEqual(mockResponse);
      expect(result.studentNumber).toBe('STD-2026-0001');
    });
  });
});
