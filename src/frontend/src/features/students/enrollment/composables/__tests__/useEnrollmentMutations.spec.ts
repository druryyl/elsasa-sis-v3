// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as enrollmentApi from '../../api/enrollment.api';
import { useEnrollAcceptedApplicantMutation } from '../useEnrollmentMutations';
import type {
  EnrollAcceptedApplicantPayload,
  EnrollAcceptedApplicantResponse,
} from '../../types/enrollment.types';
import { ApiError } from '@/shared/types/common';

const mockToast = {
  add: vi.fn(),
  removeGroup: vi.fn(),
  removeAllGroups: vi.fn(),
};

vi.mock('primevue/usetoast', () => ({
  useToast: () => mockToast,
}));

function withSetup<T>(composable: () => T) {
  let result!: T;
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const app = createApp({
    setup() {
      result = composable();
      return () => {};
    },
  });

  app.use(VueQueryPlugin, { queryClient });
  app.mount(document.createElement('div'));

  return { result, queryClient, app };
}

describe('useEnrollmentMutations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useEnrollAcceptedApplicantMutation', () => {
    const mockPayload: EnrollAcceptedApplicantPayload = {
      applicantId: 'apl-1001',
      academicYearId: 'ay-2026',
      initialCampus: 'Main Campus',
      initialLevel: 'Grade 1',
      initialAcademicYear: '2026/2027',
      paymentWaived: false,
      paymentReference: 'PAY-REC-99881',
      guardians: [],
    };

    const mockResponse: EnrollAcceptedApplicantResponse = {
      studentId: 'stu-999',
      studentNumber: 'STD-2026-0042',
      enrollmentId: 'enr-111',
    };

    it('executes enrollment, invalidates queue and dossier, and displays success toast with student number', async () => {
      vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant').mockResolvedValue(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useEnrollAcceptedApplicantMutation({ onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync(mockPayload);

      expect(enrollmentApi.enrollAcceptedApplicant).toHaveBeenCalledWith(mockPayload);
      expect(response).toEqual(mockResponse);

      // Verify required cache invalidations
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'admission', 'queue'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'admission', 'dossier'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records'],
      });

      // Verify success toast with student number
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Enrollment Successful',
          detail: expect.stringContaining('STD-2026-0042'),
        }),
      );

      expect(onSuccess).toHaveBeenCalledWith(mockResponse, mockPayload);
    });

    it('handles ApiError and triggers error toast and onError callback', async () => {
      const apiError = new ApiError(
        409,
        'Applicant Already Enrolled',
        'This applicant has already been enrolled into the student directory.',
      );

      vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant').mockRejectedValue(apiError);

      const onError = vi.fn();
      const { result } = withSetup(() =>
        useEnrollAcceptedApplicantMutation({ onError }),
      );

      await expect(result.mutateAsync(mockPayload)).rejects.toThrow();

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Applicant Already Enrolled',
          detail: 'This applicant has already been enrolled into the student directory.',
        }),
      );
      expect(onError).toHaveBeenCalledWith(apiError, mockPayload);
    });

    it('suppresses toast notifications when showToast is false', async () => {
      vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant').mockResolvedValue(mockResponse);

      const { result } = withSetup(() =>
        useEnrollAcceptedApplicantMutation({ showToast: false }),
      );

      await result.mutateAsync(mockPayload);

      expect(mockToast.add).not.toHaveBeenCalled();
    });
  });
});
