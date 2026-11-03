// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as admissionApi from '../../api/admission.api';
import {
  useRegisterApplicationMutation,
  useStartReviewMutation,
  useRecordDecisionMutation,
  useWithdrawApplicationMutation,
  useUploadDocumentMutation,
  useVerifyDocumentMutation,
  useAddAssessmentEvidenceMutation,
  useUpdateAssessmentEvidenceMutation,
  useDeleteAssessmentEvidenceMutation,
  useAssessmentEvidenceMutations,
} from '../useAdmissionMutations';
import { admissionKeys } from '../queryKeys';
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

describe('useAdmissionMutations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useRegisterApplicationMutation', () => {
    it('registers application, invalidates admissionKeys.all, and triggers success toast', async () => {
      const mockResult = {
        id: 'app-1',
        applicationNumber: 'ADM-2026-0001',
        applicantId: 'apl-1',
        status: 'registered',
        registrationDate: '2026-02-15',
      };
      vi.spyOn(admissionApi, 'registerApplication').mockResolvedValue(mockResult);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useRegisterApplicationMutation({ onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        fullName: 'Budi Santoso',
        dateOfBirth: '2013-05-10',
        admissionPeriodId: 'per-1',
        applicationType: 'new' as const,
        requestedEntryLevel: 'Grade 7',
        contacts: [
          {
            fullName: 'Hartono',
            relationship: 'Father',
            isPrimary: true,
          },
        ],
      };

      await result.mutateAsync(payload);

      expect(admissionApi.registerApplication).toHaveBeenCalledWith(payload, false);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Application Registered',
          detail: expect.stringContaining('ADM-2026-0001'),
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockResult, payload);
    });

    it('triggers error toast and calls onError on failure', async () => {
      const error = new ApiError(
        409,
        'Conflict',
        'An active application already exists for this applicant in the specified admission period.',
      );
      vi.spyOn(admissionApi, 'registerApplication').mockRejectedValue(error);

      const onError = vi.fn();
      const { result } = withSetup(() => useRegisterApplicationMutation({ onError }));

      const payload = {
        fullName: 'Budi Santoso',
        dateOfBirth: '2013-05-10',
        admissionPeriodId: 'per-1',
        applicationType: 'new' as const,
        requestedEntryLevel: 'Grade 7',
        contacts: [{ fullName: 'Hartono', relationship: 'Father', isPrimary: true }],
      };

      await expect(result.mutateAsync(payload)).rejects.toThrow();

      expect(onError).toHaveBeenCalledWith(error, payload);
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Conflict',
          detail: 'An active application already exists for this applicant in the specified admission period.',
        }),
      );
    });
  });

  describe('useStartReviewMutation', () => {
    it('starts review, invalidates admissionKeys.all, and triggers success toast', async () => {
      const mockResult = {
        id: 'app-1',
        status: 'under_review',
        updatedAt: '2026-02-16T10:00:00Z',
      };
      vi.spyOn(admissionApi, 'startApplicationReview').mockResolvedValue(mockResult);

      const { result, queryClient } = withSetup(() => useStartReviewMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('app-1');

      expect(admissionApi.startApplicationReview).toHaveBeenCalledWith('app-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Review Started',
          detail: 'Application status updated to under review.',
        }),
      );
    });
  });

  describe('useRecordDecisionMutation', () => {
    it('records decision, invalidates admissionKeys.all, and displays toast', async () => {
      const mockResult = {
        decisionId: 'dec-1',
        applicationId: 'app-1',
        decisionType: 'accepted',
        effectiveDate: '2026-07-01',
      };
      vi.spyOn(admissionApi, 'recordAdmissionDecision').mockResolvedValue(mockResult);

      const { result, queryClient } = withSetup(() => useRecordDecisionMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const variables = {
        id: 'app-1',
        payload: {
          decisionType: 'accepted' as const,
          decisionDate: '2026-02-20',
          effectiveDate: '2026-07-01',
          decidedBy: 'committee_chair',
          reason: 'Accepted based on high assessment results.',
        },
      };

      await result.mutateAsync(variables);

      expect(admissionApi.recordAdmissionDecision).toHaveBeenCalledWith('app-1', variables.payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Decision Recorded',
          detail: expect.stringContaining('accepted'),
        }),
      );
    });
  });

  describe('useWithdrawApplicationMutation', () => {
    it('withdraws application, invalidates admissionKeys.all, and displays toast', async () => {
      const mockResult = {
        id: 'app-1',
        status: 'withdrawn',
        notes: 'Family relocated out of state',
        updatedAt: '2026-02-21T00:00:00Z',
      };
      vi.spyOn(admissionApi, 'withdrawApplication').mockResolvedValue(mockResult);

      const { result, queryClient } = withSetup(() => useWithdrawApplicationMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const variables = {
        id: 'app-1',
        payload: { withdrawalReason: 'Family relocated out of state' },
      };

      await result.mutateAsync(variables);

      expect(admissionApi.withdrawApplication).toHaveBeenCalledWith('app-1', variables.payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Application Withdrawn',
        }),
      );
    });
  });

  describe('useUploadDocumentMutation', () => {
    it('uploads document file, invalidates admissionKeys.all, and displays toast', async () => {
      const mockResult = {
        id: 'doc-1',
        applicationId: 'app-1',
        documentType: 'birth_certificate',
        submissionStatus: 'submitted',
        fileReference: 'storage/documents/admission/app-1/birth_cert.pdf',
        fileName: 'birth_cert.pdf',
        fileSizeBytes: 204800,
        contentType: 'application/pdf',
      };
      vi.spyOn(admissionApi, 'uploadApplicantDocument').mockResolvedValue(mockResult);

      const { result, queryClient } = withSetup(() => useUploadDocumentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const fakeFile = new File(['content'], 'birth_cert.pdf', { type: 'application/pdf' });
      const variables = {
        applicationId: 'app-1',
        documentType: 'birth_certificate',
        file: fakeFile,
      };

      await result.mutateAsync(variables);

      expect(admissionApi.uploadApplicantDocument).toHaveBeenCalledWith(
        'app-1',
        'birth_certificate',
        fakeFile,
      );
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Document Uploaded',
          detail: expect.stringContaining('birth_cert.pdf'),
        }),
      );
    });
  });

  describe('useVerifyDocumentMutation', () => {
    it('verifies document, invalidates admissionKeys.all, and displays toast', async () => {
      const mockResult = {
        id: 'doc-1',
        applicationId: 'app-1',
        documentType: 'birth_certificate',
        submissionStatus: 'verified',
        verifiedDate: '2026-02-22',
        verifiedBy: 'admissions_staff',
        notes: 'Verified against civil registry copy',
      };
      vi.spyOn(admissionApi, 'verifyApplicantDocument').mockResolvedValue(mockResult);

      const { result, queryClient } = withSetup(() => useVerifyDocumentMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const variables = {
        applicationId: 'app-1',
        documentId: 'doc-1',
        payload: {
          submissionStatus: 'verified' as const,
          notes: 'Verified against civil registry copy',
        },
      };

      await result.mutateAsync(variables);

      expect(admissionApi.verifyApplicantDocument).toHaveBeenCalledWith(
        'app-1',
        'doc-1',
        variables.payload,
      );
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Document Verified',
          detail: expect.stringContaining('verified'),
        }),
      );
    });
  });

  describe('useAssessmentEvidenceMutations (Add, Update, Delete)', () => {
    it('executes add, update, delete mutations and triggers invalidations', async () => {
      const mockAddResult = {
        id: 'ev-1',
        applicationId: 'app-1',
        assessmentType: 'reading_readiness',
        score: '88/100',
        source: 'External Assessment Center',
        assessmentDate: '2026-02-18',
        notes: 'Proficient in English reading',
        createdAt: '2026-02-18T10:00:00Z',
      };
      const mockUpdateResult = {
        id: 'ev-1',
        applicationId: 'app-1',
        assessmentType: 'reading_readiness',
        score: '92/100',
        source: 'External Assessment Center',
        assessmentDate: '2026-02-18',
        notes: 'Adjusted after re-evaluation',
      };

      vi.spyOn(admissionApi, 'addAssessmentEvidence').mockResolvedValue(mockAddResult);
      vi.spyOn(admissionApi, 'updateAssessmentEvidence').mockResolvedValue(mockUpdateResult);
      vi.spyOn(admissionApi, 'deleteAssessmentEvidence').mockResolvedValue(undefined);

      const { result, queryClient } = withSetup(() => useAssessmentEvidenceMutations());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      // 1. Add
      await result.addEvidence({
        applicationId: 'app-1',
        payload: {
          assessmentType: 'reading_readiness',
          score: '88/100',
          source: 'External Assessment Center',
          assessmentDate: '2026-02-18',
          notes: 'Proficient in English reading',
        },
      });
      expect(admissionApi.addAssessmentEvidence).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: admissionKeys.all });

      // 2. Update
      await result.updateEvidence({
        applicationId: 'app-1',
        evidenceId: 'ev-1',
        payload: {
          assessmentType: 'reading_readiness',
          score: '92/100',
          source: 'External Assessment Center',
          assessmentDate: '2026-02-18',
          notes: 'Adjusted after re-evaluation',
        },
      });
      expect(admissionApi.updateAssessmentEvidence).toHaveBeenCalled();

      // 3. Delete
      await result.deleteEvidence({
        applicationId: 'app-1',
        evidenceId: 'ev-1',
      });
      expect(admissionApi.deleteAssessmentEvidence).toHaveBeenCalledWith('app-1', 'ev-1');
    });

    it('exercises individual useAddAssessmentEvidenceMutation, useUpdateAssessmentEvidenceMutation, and useDeleteAssessmentEvidenceMutation', async () => {
      vi.spyOn(admissionApi, 'addAssessmentEvidence').mockResolvedValue({
        id: 'ev-2',
        applicationId: 'app-2',
        assessmentType: 'math_readiness',
        score: '95/100',
        source: 'Math Test',
        assessmentDate: '2026-02-19',
        notes: null,
        createdAt: '2026-02-19T10:00:00Z',
      });
      vi.spyOn(admissionApi, 'updateAssessmentEvidence').mockResolvedValue({
        id: 'ev-2',
        applicationId: 'app-2',
        assessmentType: 'math_readiness',
        score: '98/100',
        source: 'Math Test',
        assessmentDate: '2026-02-19',
        notes: 'Bonus credited',
        updatedAt: '2026-02-20T00:00:00Z',
        updatedBy: 'tester',
      });
      vi.spyOn(admissionApi, 'deleteAssessmentEvidence').mockResolvedValue(undefined);

      const { result: addHook } = withSetup(() => useAddAssessmentEvidenceMutation());
      await addHook.mutateAsync({
        applicationId: 'app-2',
        payload: {
          assessmentType: 'math_readiness',
          score: '95/100',
          source: 'Math Test',
        },
      });
      expect(admissionApi.addAssessmentEvidence).toHaveBeenCalled();

      const { result: updateHook } = withSetup(() => useUpdateAssessmentEvidenceMutation());
      await updateHook.mutateAsync({
        applicationId: 'app-2',
        evidenceId: 'ev-2',
        payload: {
          assessmentType: 'math_readiness',
          score: '98/100',
        },
      });
      expect(admissionApi.updateAssessmentEvidence).toHaveBeenCalled();

      const { result: deleteHook } = withSetup(() => useDeleteAssessmentEvidenceMutation());
      await deleteHook.mutateAsync({
        applicationId: 'app-2',
        evidenceId: 'ev-2',
      });
      expect(admissionApi.deleteAssessmentEvidence).toHaveBeenCalled();
    });
  });
});
