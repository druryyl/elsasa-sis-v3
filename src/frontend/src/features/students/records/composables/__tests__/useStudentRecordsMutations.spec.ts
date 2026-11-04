// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as recordsApi from '../../api/records.api';
import {
  useUpdateStudentIdentityMutation,
  useAddGuardianMutation,
  useUpdateGuardianMutation,
  useUploadStudentDocumentMutation,
  useVerifyStudentDocumentMutation,
  useChangePlacementMutation,
  useChangeStatusMutation,
} from '../useStudentRecordsMutations';
import type {
  AddGuardianPayload,
  ChangeAdministrativePlacementRequest,
  ChangeAdministrativePlacementResponse,
  ChangeStudentStatusRequest,
  ChangeStudentStatusResponse,
  UpdateGuardianPayload,
  UpdateIdentityPayload,
} from '../../types/records.types';
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

describe('useStudentRecordsMutations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useUpdateStudentIdentityMutation', () => {
    const studentId = 'stu-100';
    const payload: UpdateIdentityPayload = {
      fullName: 'Ahmad Dahlan Modified',
      preferredName: 'Dahlan',
      dateOfBirth: '2012-08-01',
      placeOfBirth: 'Yogyakarta',
      gender: 'male',
      nationality: 'Indonesian',
      governmentIdType: 'NIK',
      governmentIdNumber: '3471010108120001',
      address: 'Jl. Malioboro No. 45',
      phone: '+6281234567890',
      email: 'dahlan@example.com',
      isIndependent: true,
      notes: 'Updated certified identity',
    };

    it('updates identity, invalidates detail and list queries, and displays success toast', async () => {
      vi.spyOn(recordsApi, 'updateStudentIdentity').mockResolvedValue(undefined);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useUpdateStudentIdentityMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(payload);

      expect(recordsApi.updateStudentIdentity).toHaveBeenCalledWith(studentId, payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'list'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Identity Updated',
        }),
      );
      expect(onSuccess).toHaveBeenCalled();
    });

    it('supports reactive studentId ref', async () => {
      vi.spyOn(recordsApi, 'updateStudentIdentity').mockResolvedValue(undefined);
      const reactiveId = ref('stu-200');

      const { result, queryClient } = withSetup(() =>
        useUpdateStudentIdentityMutation(reactiveId),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync(payload);

      expect(recordsApi.updateStudentIdentity).toHaveBeenCalledWith('stu-200', payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', 'stu-200'],
      });
    });

    it('handles ApiError and triggers error toast', async () => {
      const apiError = new ApiError(
        400,
        'Validation Error',
        'Government ID already in use.',
      );

      vi.spyOn(recordsApi, 'updateStudentIdentity').mockRejectedValue(apiError);

      const onError = vi.fn();
      const { result } = withSetup(() =>
        useUpdateStudentIdentityMutation(studentId, { onError }),
      );

      await expect(result.mutateAsync(payload)).rejects.toThrow();

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Validation Error',
          detail: 'Government ID already in use.',
        }),
      );
      expect(onError).toHaveBeenCalledWith(apiError, payload);
    });
  });

  describe('useAddGuardianMutation', () => {
    const studentId = 'stu-100';
    const payload: AddGuardianPayload = {
      fullName: 'Siti Walidah',
      relationshipType: 'mother',
      governmentIdType: 'NIK',
      governmentIdNumber: '3471010108800001',
      phone: '+628111222333',
      email: 'walidah@example.com',
      isPrimaryContact: true,
      isEmergencyContact: true,
      isFinancialContact: true,
      isAuthorizedPickup: true,
    };

    it('links guardian relationship, invalidates detail query, and returns relationship ID', async () => {
      vi.spyOn(recordsApi, 'addGuardianRelationship').mockResolvedValue('rel-500');

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useAddGuardianMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const relId = await result.mutateAsync(payload);

      expect(recordsApi.addGuardianRelationship).toHaveBeenCalledWith(studentId, payload);
      expect(relId).toBe('rel-500');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Guardian Added',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith('rel-500', payload);
    });
  });

  describe('useUpdateGuardianMutation', () => {
    const studentId = 'stu-100';
    const relationshipId = 'rel-500';
    const payload: UpdateGuardianPayload = {
      relationshipType: 'mother',
      isPrimaryContact: true,
      isEmergencyContact: true,
      isFinancialContact: false,
      isAuthorizedPickup: true,
      isActive: true,
    };

    it('updates guardian relationship, invalidates detail query, and displays success toast', async () => {
      vi.spyOn(recordsApi, 'updateGuardianRelationship').mockResolvedValue(undefined);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useUpdateGuardianMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ relationshipId, payload });

      expect(recordsApi.updateGuardianRelationship).toHaveBeenCalledWith(
        studentId,
        relationshipId,
        payload,
      );
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Guardian Updated',
        }),
      );
      expect(onSuccess).toHaveBeenCalled();
    });
  });

  describe('useUploadStudentDocumentMutation', () => {
    const studentId = 'stu-100';
    const mockFile = new File(['mock content'], 'birth-cert.pdf', {
      type: 'application/pdf',
    });

    it('uploads student document, invalidates detail query, and returns document ID', async () => {
      vi.spyOn(recordsApi, 'uploadStudentDocument').mockResolvedValue('doc-800');

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useUploadStudentDocumentMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const docId = await result.mutateAsync({
        file: mockFile,
        documentType: 'Birth Certificate',
      });

      expect(recordsApi.uploadStudentDocument).toHaveBeenCalledWith(
        studentId,
        mockFile,
        'Birth Certificate',
      );
      expect(docId).toBe('doc-800');
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Document Uploaded',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith('doc-800', {
        file: mockFile,
        documentType: 'Birth Certificate',
      });
    });
  });

  describe('useVerifyStudentDocumentMutation', () => {
    const studentId = 'stu-100';

    it('records document verification decision, invalidates detail query, and displays success toast', async () => {
      vi.spyOn(recordsApi, 'verifyStudentDocument').mockResolvedValue(undefined);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useVerifyStudentDocumentMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        documentId: 'doc-800',
        status: 'verified',
        notes: 'Original document inspected and verified.',
      });

      expect(recordsApi.verifyStudentDocument).toHaveBeenCalledWith(
        studentId,
        'doc-800',
        {
          submissionStatus: 'verified',
          notes: 'Original document inspected and verified.',
        },
      );
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Verification Recorded',
        }),
      );
      expect(onSuccess).toHaveBeenCalled();
    });
  });

  describe('useChangePlacementMutation', () => {
    const studentId = 'stu-500';
    const payload: ChangeAdministrativePlacementRequest = {
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

    const mockResponse: ChangeAdministrativePlacementResponse = {
      placementId: 'plc-900',
      studentId,
      campus: 'North Campus',
      schoolLevel: 'Secondary',
      grade: 'Grade 8',
      academicYear: '2026/2027',
      effectiveDate: '2026-08-01',
      isCurrent: true,
    };

    it('changes placement, invalidates detail and history queries, and triggers success toast', async () => {
      vi.spyOn(recordsApi, 'changeAdministrativePlacement').mockResolvedValue(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useChangePlacementMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync(payload);

      expect(recordsApi.changeAdministrativePlacement).toHaveBeenCalledWith(studentId, payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId, 'history'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Placement Updated',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, payload);
      expect(response).toEqual(mockResponse);
    });

    it('supports wrapped variables with studentId and payload', async () => {
      vi.spyOn(recordsApi, 'changeAdministrativePlacement').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useChangePlacementMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ studentId: 'stu-999', payload });

      expect(recordsApi.changeAdministrativePlacement).toHaveBeenCalledWith('stu-999', payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', 'stu-999'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', 'stu-999', 'history'],
      });
    });

    it('throws error and shows toast on mutation failure', async () => {
      const error = new Error('Reassignment failed due to invalid effective date');
      vi.spyOn(recordsApi, 'changeAdministrativePlacement').mockRejectedValue(error);

      const onError = vi.fn();
      const { result } = withSetup(() =>
        useChangePlacementMutation(studentId, { onError }),
      );

      await expect(result.mutateAsync(payload)).rejects.toThrow('Reassignment failed');
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Placement Update Failed',
        }),
      );
      expect(onError).toHaveBeenCalled();
    });
  });

  describe('useChangeStatusMutation', () => {
    const studentId = 'stu-600';
    const payload: ChangeStudentStatusRequest = {
      status: 'active',
      effectiveDate: '2026-08-01',
      reason: 'Returned from leave of absence',
      authorizedBy: 'officer@elsasa.sch.id',
      decisionReference: 'STA-2026-004',
    };

    const mockResponse: ChangeStudentStatusResponse = {
      statusId: 'sta-901',
      studentId,
      status: 'active',
      effectiveDate: '2026-08-01',
      isCurrent: true,
    };

    it('changes status, invalidates detail, history, and list queries, and triggers success toast', async () => {
      vi.spyOn(recordsApi, 'changeStudentStatus').mockResolvedValue(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useChangeStatusMutation(studentId, { onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const response = await result.mutateAsync(payload);

      expect(recordsApi.changeStudentStatus).toHaveBeenCalledWith(studentId, payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', studentId, 'history'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'list'],
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Status Updated',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, payload);
      expect(response).toEqual(mockResponse);
    });

    it('supports wrapped variables with studentId and payload', async () => {
      vi.spyOn(recordsApi, 'changeStudentStatus').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useChangeStatusMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ studentId: 'stu-777', payload });

      expect(recordsApi.changeStudentStatus).toHaveBeenCalledWith('stu-777', payload);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', 'stu-777'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'detail', 'stu-777', 'history'],
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ['students', 'records', 'list'],
      });
    });

    it('throws error and shows toast on status change failure', async () => {
      const error = new Error('Invalid status transition');
      vi.spyOn(recordsApi, 'changeStudentStatus').mockRejectedValue(error);

      const onError = vi.fn();
      const { result } = withSetup(() =>
        useChangeStatusMutation(studentId, { onError }),
      );

      await expect(result.mutateAsync(payload)).rejects.toThrow('Invalid status transition');
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Status Update Failed',
        }),
      );
      expect(onError).toHaveBeenCalled();
    });
  });
});

