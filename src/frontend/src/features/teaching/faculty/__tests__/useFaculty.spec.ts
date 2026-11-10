// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as teachingApiClient from '../api/teachingApiClient';
import {
  facultyKeys,
  useTeacherProfilesQuery,
  useFacultyWorkQueue,
  useTeacherProfileQuery,
  useTeacherDetail,
  useTeachingRolesQuery,
  useTeachingRoles,
  useCapabilityTypesQuery,
  useCapabilityTypes,
  useQualificationTypesQuery,
  useQualificationTypes,
  useRecordCorrectionsQuery,
  useRegisterTeacherMutation,
  useUpdateTeacherMutation,
  useApproveTeacherMutation,
  useChangeStandingMutation,
  useAddCapabilityMutation,
  useRemoveCapabilityMutation,
  useSubmitEvidenceMutation,
  useVerifyEvidenceMutation,
  useRejectEvidenceMutation,
  useGrantAuthorizationMutation,
  useUpdateAuthorizationStatusMutation,
  useAddSupervisionMutation,
  useResolveSupervisionMutation,
  useApplyRecordCorrectionMutation,
  useApplyCorrectionMutation,
  useApproveRecordCorrectionMutation,
  useApproveCorrectionMutation,
} from '../composables/useFaculty';
import type {
  CapabilityTypeDto,
  FacultyQueryParams,
  PaginatedResponse,
  QualificationTypeResponse,
  TeacherProfileDto,
  TeacherSummaryDto,
  TeachingRecordCorrectionDto,
  TeachingRoleDto,
} from '../api/teachingTypes';

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

describe('Faculty Management TanStack Query Composables (useFaculty)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Query Hooks', () => {
    const mockTeacherSummaries: PaginatedResponse<TeacherSummaryDto> = {
      items: [
        {
          id: 'tch-001',
          teacherNumber: 'TCH-2026-0001',
          fullName: 'Budi Hartono, M.Pd.',
          displayName: 'Pak Budi',
          standing: 'active',
          teachingRoleName: 'Guru Tetap',
          campusAffiliation: 'Main Campus',
          department: 'Mathematics',
          activeAuthorizationsCount: 2,
          pendingVerificationsCount: 0,
          supervisionRequirementsCount: 0,
        },
      ],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    it('useFacultyWorkQueue / useTeacherProfilesQuery fetches faculty list with reactive filters', async () => {
      vi.spyOn(teachingApiClient, 'getTeachers').mockResolvedValue(mockTeacherSummaries);

      const filters = ref<FacultyQueryParams>({
        queueFilter: 'all',
        standing: 'active',
        campus: 'Main Campus',
      });

      const { result } = withSetup(() => useFacultyWorkQueue(filters));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(teachingApiClient.getTeachers).toHaveBeenCalledWith(filters.value);
      expect(result.data.value).toEqual(mockTeacherSummaries);
      expect(useFacultyWorkQueue).toBe(useTeacherProfilesQuery);
    });

    it('useTeacherDetail / useTeacherProfileQuery fetches full dossier and honors enabled guard', async () => {
      const mockDossier: TeacherProfileDto = {
        id: 'tch-001',
        teacherNumber: 'TCH-2026-0001',
        fullName: 'Budi Hartono, M.Pd.',
        displayName: 'Pak Budi',
        standing: 'active',
        campusAffiliation: 'Main Campus',
        effectiveFrom: '2026-01-01',
        teachingRoleId: 'role-1',
        teachingRoleName: 'Guru Tetap',
        teachingCapabilities: [],
        qualificationEvidence: [],
        teachingAuthorizations: [],
        createdAt: '2026-01-01T00:00:00Z',
        createdBy: 'admin',
      };

      vi.spyOn(teachingApiClient, 'getTeacherById').mockResolvedValue(mockDossier);

      const teacherId = ref<string | null>(null);
      const { result } = withSetup(() => useTeacherDetail(teacherId));

      expect(result.fetchStatus.value).toBe('idle');
      expect(teachingApiClient.getTeacherById).not.toHaveBeenCalled();

      teacherId.value = 'tch-001';
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(teachingApiClient.getTeacherById).toHaveBeenCalledWith('tch-001');
      expect(result.data.value).toEqual(mockDossier);
      expect(useTeacherDetail).toBe(useTeacherProfileQuery);
    });

    it('useTeachingRoles / useTeachingRolesQuery fetches recognized roles', async () => {
      const mockRoles: TeachingRoleDto[] = [
        {
          id: 'role-1',
          code: 'PERMANENT',
          name: 'Guru Tetap',
          requiresQualificationCheck: true,
          isActive: true,
          createdAt: '2026-01-01T00:00:00Z',
          createdBy: 'system',
        },
      ];

      vi.spyOn(teachingApiClient, 'getRoles').mockResolvedValue(mockRoles);

      const { result } = withSetup(() => useTeachingRoles());

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(teachingApiClient.getRoles).toHaveBeenCalled();
      expect(result.data.value).toEqual(mockRoles);
      expect(useTeachingRoles).toBe(useTeachingRolesQuery);
    });

    it('useCapabilityTypes / useCapabilityTypesQuery fetches capability taxonomy', async () => {
      const mockTypes: CapabilityTypeDto[] = [
        {
          id: 'cap-type-1',
          code: 'MATH_PRIMARY',
          name: 'Primary Mathematics',
          isActive: true,
          createdAt: '2026-01-01T00:00:00Z',
          createdBy: 'system',
        },
      ];

      vi.spyOn(teachingApiClient, 'getCapabilityTypes').mockResolvedValue(mockTypes);

      const { result } = withSetup(() => useCapabilityTypes());

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(teachingApiClient.getCapabilityTypes).toHaveBeenCalled();
      expect(result.data.value).toEqual(mockTypes);
      expect(useCapabilityTypes).toBe(useCapabilityTypesQuery);
    });

    it('useQualificationTypes / useQualificationTypesQuery fetches qualification categories', async () => {
      const mockQualTypes: QualificationTypeResponse[] = [
        {
          id: 'qual-type-1',
          code: 'SERDIK',
          name: 'Sertifikat Pendidik',
          category: 'professional_license',
          validityPeriodYears: 5,
          renewalRequired: true,
          isActive: true,
        },
      ];

      vi.spyOn(teachingApiClient, 'getQualificationTypes').mockResolvedValue(mockQualTypes);

      const { result } = withSetup(() => useQualificationTypes());

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(teachingApiClient.getQualificationTypes).toHaveBeenCalled();
      expect(result.data.value).toEqual(mockQualTypes);
      expect(useQualificationTypes).toBe(useQualificationTypesQuery);
    });

    it('useRecordCorrectionsQuery fetches audit trail of corrections', async () => {
      const mockCorrections: TeachingRecordCorrectionDto[] = [
        {
          id: 'cor-1',
          correctionNumber: 'COR-2026-0001',
          teacherProfileId: 'tch-001',
          targetTableName: 'teacher_profiles',
          targetRecordId: 'tch-001',
          fieldName: 'display_name',
          oldValue: 'Budi',
          newValue: 'Pak Budi',
          justificationReason: 'Staff preference update',
          status: 'applied',
          requestedBy: 'coordinator',
          requestedAt: '2026-02-01T10:00:00Z',
          appliedBy: 'authority',
          appliedAt: '2026-02-01T10:05:00Z',
        },
      ];

      vi.spyOn(teachingApiClient, 'getCorrections').mockResolvedValue(mockCorrections);

      const { result } = withSetup(() =>
        useRecordCorrectionsQuery({ teacherProfileId: 'tch-001' }),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(teachingApiClient.getCorrections).toHaveBeenCalledWith({ teacherProfileId: 'tch-001' });
      expect(result.data.value).toEqual(mockCorrections);
    });
  });

  describe('Mutation Hooks & Cache Invalidation', () => {
    it('useRegisterTeacherMutation registers teacher and invalidates cache', async () => {
      const mockResponse = {
        teacherProfileId: 'tch-001',
        teacherNumber: 'TCH-2026-0001',
        standing: 'pending_approval' as const,
        requiresApproval: true,
      };
      vi.spyOn(teachingApiClient, 'registerTeacher').mockResolvedValue(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useRegisterTeacherMutation({ onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        fullName: 'Budi Hartono',
        teachingRoleId: 'role-1',
        campusAffiliation: 'Main Campus',
        effectiveFrom: '2026-01-01',
      };

      await result.mutateAsync(payload);

      expect(teachingApiClient.registerTeacher).toHaveBeenCalledWith(payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: facultyKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Faculty Registered',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, payload);
    });

    it('useUpdateTeacherMutation updates profile details and invalidates cache', async () => {
      vi.spyOn(teachingApiClient, 'updateTeacher').mockResolvedValue();

      const { result, queryClient } = withSetup(() => useUpdateTeacherMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const vars = {
        id: 'tch-001',
        payload: {
          fullName: 'Budi Hartono, M.Pd.',
          displayName: 'Pak Budi',
          campusAffiliation: 'Main Campus',
        },
      };

      await result.mutateAsync(vars);

      expect(teachingApiClient.updateTeacher).toHaveBeenCalledWith(vars.id, vars.payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: facultyKeys.detail('tch-001') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Profile Updated',
        }),
      );
    });

    it('useApproveTeacherMutation approves teacher profile and triggers notification', async () => {
      const mockResponse = {
        teacherProfileId: 'tch-001',
        teacherNumber: 'TCH-2026-0001',
        standing: 'active' as const,
        approvedBy: 'kepala_sekolah',
        approvedDate: '2026-02-01',
      };
      vi.spyOn(teachingApiClient, 'approveTeacher').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useApproveTeacherMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('tch-001');

      expect(teachingApiClient.approveTeacher).toHaveBeenCalledWith('tch-001');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: facultyKeys.detail('tch-001') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Faculty Approved',
        }),
      );
    });

    it('useChangeStandingMutation changes standing with reason and invalidates cache', async () => {
      const mockResponse = {
        teacherProfileId: 'tch-001',
        standing: 'restricted' as const,
        standingReason: 'Audit review ongoing',
        changedBy: 'authority',
      };
      vi.spyOn(teachingApiClient, 'changeStanding').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useChangeStandingMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const vars = {
        id: 'tch-001',
        payload: {
          standing: 'restricted' as const,
          reason: 'Audit review ongoing',
        },
      };

      await result.mutateAsync(vars);

      expect(teachingApiClient.changeStanding).toHaveBeenCalledWith(vars.id, vars.payload);
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: facultyKeys.detail('tch-001') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Standing Updated',
        }),
      );
    });

    it('useAddCapabilityMutation and useRemoveCapabilityMutation manage capabilities', async () => {
      const mockAddResponse = {
        capabilityId: 'cap-1',
        teacherProfileId: 'tch-001',
        capabilityCode: 'MATH_PRIMARY',
        capabilityName: 'Primary Math',
        status: 'active' as const,
      };
      vi.spyOn(teachingApiClient, 'addCapability').mockResolvedValue(mockAddResponse);
      vi.spyOn(teachingApiClient, 'removeCapability').mockResolvedValue();

      const { result: addResult, queryClient } = withSetup(() => useAddCapabilityMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await addResult.mutateAsync({
        teacherId: 'tch-001',
        payload: {
          capabilityTypeId: 'cap-type-1',
          capabilityCode: 'MATH_PRIMARY',
          capabilityName: 'Primary Math',
        },
      });

      expect(teachingApiClient.addCapability).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: facultyKeys.detail('tch-001') });

      const { result: removeResult } = withSetup(() => useRemoveCapabilityMutation());
      await removeResult.mutateAsync({
        teacherId: 'tch-001',
        capabilityId: 'cap-1',
      });

      expect(teachingApiClient.removeCapability).toHaveBeenCalledWith('tch-001', 'cap-1');
    });

    it('useSubmitEvidenceMutation, useVerifyEvidenceMutation, and useRejectEvidenceMutation manage qualification evidence', async () => {
      const mockSubmitResponse = {
        evidenceId: 'ev-1',
        teacherProfileId: 'tch-001',
        verificationState: 'submitted' as const,
      };
      const mockVerifyResponse = {
        evidenceId: 'ev-1',
        verificationState: 'verified' as const,
        verifiedBy: 'verifier',
        verifiedAt: '2026-02-01T00:00:00Z',
      };
      const mockRejectResponse = {
        evidenceId: 'ev-1',
        verificationState: 'rejected' as const,
        rejectionReason: 'Illegible photocopy',
      };

      vi.spyOn(teachingApiClient, 'submitEvidence').mockResolvedValue(mockSubmitResponse);
      vi.spyOn(teachingApiClient, 'verifyEvidence').mockResolvedValue(mockVerifyResponse);
      vi.spyOn(teachingApiClient, 'rejectEvidence').mockResolvedValue(mockRejectResponse);

      const { result: submitResult } = withSetup(() => useSubmitEvidenceMutation());
      await submitResult.mutateAsync({
        teacherId: 'tch-001',
        payload: {
          qualificationTypeId: 'qt-1',
          documentTitle: 'S1 Certificate',
        },
      });
      expect(teachingApiClient.submitEvidence).toHaveBeenCalled();

      const { result: verifyResult } = withSetup(() => useVerifyEvidenceMutation());
      await verifyResult.mutateAsync({
        id: 'ev-1',
        teacherId: 'tch-001',
      });
      expect(teachingApiClient.verifyEvidence).toHaveBeenCalledWith('ev-1', undefined);

      const { result: rejectResult } = withSetup(() => useRejectEvidenceMutation());
      await rejectResult.mutateAsync({
        id: 'ev-1',
        teacherId: 'tch-001',
        payload: { rejectionReason: 'Illegible photocopy' },
      });
      expect(teachingApiClient.rejectEvidence).toHaveBeenCalledWith('ev-1', {
        rejectionReason: 'Illegible photocopy',
      });
    });

    it('useGrantAuthorizationMutation handles warning and invalidates cache', async () => {
      const mockGrantResponse = {
        authorizationId: 'auth-1',
        teacherProfileId: 'tch-001',
        authorizationNumber: 'AUTH-2026-0001',
        status: 'active' as const,
        unverifiedCredentialWarning: true,
        supervisionRequirementCreated: false,
      };
      vi.spyOn(teachingApiClient, 'grantAuthorization').mockResolvedValue(mockGrantResponse);

      const { result, queryClient } = withSetup(() => useGrantAuthorizationMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        teacherId: 'tch-001',
        payload: {
          teachingCapabilityId: 'cap-1',
          campusAffiliation: 'Main Campus',
          scopeDescription: 'Math Primary Tatap Muka',
          effectiveStartDate: '2026-01-01',
          supportingEvidenceIds: ['ev-1'],
        },
      });

      expect(teachingApiClient.grantAuthorization).toHaveBeenCalled();
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: facultyKeys.detail('tch-001') });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          detail: expect.stringContaining('Permissive warning: unverified credentials attached'),
        }),
      );
    });

    it('useUpdateAuthorizationStatusMutation updates authorization status', async () => {
      const mockResponse = {
        authorizationId: 'auth-1',
        status: 'suspended' as const,
        statusReason: 'Under review',
      };
      vi.spyOn(teachingApiClient, 'updateAuthorizationStatus').mockResolvedValue(mockResponse);

      const { result } = withSetup(() => useUpdateAuthorizationStatusMutation());
      await result.mutateAsync({
        id: 'auth-1',
        teacherId: 'tch-001',
        payload: { status: 'suspended', reason: 'Under review' },
      });

      expect(teachingApiClient.updateAuthorizationStatus).toHaveBeenCalledWith('auth-1', {
        status: 'suspended',
        reason: 'Under review',
      });
    });

    it('useAddSupervisionMutation and useResolveSupervisionMutation manage supervision conditions', async () => {
      const mockAddResponse = {
        supervisionId: 'sup-1',
        teachingAuthorizationId: 'auth-1',
        supervisionType: 'co_teaching' as const,
        status: 'active' as const,
      };
      const mockResolveResponse = {
        supervisionId: 'sup-1',
        status: 'completed' as const,
        resolutionNotes: 'Completed 1 semester mentoring',
      };

      vi.spyOn(teachingApiClient, 'addSupervision').mockResolvedValue(mockAddResponse);
      vi.spyOn(teachingApiClient, 'resolveSupervision').mockResolvedValue(mockResolveResponse);

      const { result: addResult } = withSetup(() => useAddSupervisionMutation());
      await addResult.mutateAsync({
        authorizationId: 'auth-1',
        teacherId: 'tch-001',
        payload: {
          supervisionType: 'co_teaching',
          mentorTeacherId: 'tch-mentor-1',
        },
      });
      expect(teachingApiClient.addSupervision).toHaveBeenCalled();

      const { result: resolveResult } = withSetup(() => useResolveSupervisionMutation());
      await resolveResult.mutateAsync({
        id: 'sup-1',
        teacherId: 'tch-001',
        payload: {
          status: 'completed',
          resolutionNotes: 'Completed 1 semester mentoring',
        },
      });
      expect(teachingApiClient.resolveSupervision).toHaveBeenCalledWith('sup-1', {
        status: 'completed',
        resolutionNotes: 'Completed 1 semester mentoring',
      });
    });

    it('useApplyRecordCorrectionMutation and useApproveRecordCorrectionMutation apply and approve corrections', async () => {
      const mockCorrection: TeachingRecordCorrectionDto = {
        id: 'cor-1',
        correctionNumber: 'COR-2026-0001',
        teacherProfileId: 'tch-001',
        targetTableName: 'teacher_profiles',
        targetRecordId: 'tch-001',
        fieldName: 'display_name',
        oldValue: 'Budi',
        newValue: 'Pak Budi',
        justificationReason: 'Direct correction',
        status: 'applied',
        requestedBy: 'authority',
        requestedAt: '2026-02-01T00:00:00Z',
      };

      vi.spyOn(teachingApiClient, 'applyCorrection').mockResolvedValue(mockCorrection);
      vi.spyOn(teachingApiClient, 'approveCorrection').mockResolvedValue(mockCorrection);

      const { result: applyResult } = withSetup(() => useApplyRecordCorrectionMutation());
      await applyResult.mutateAsync({
        targetTableName: 'teacher_profiles',
        targetRecordId: 'tch-001',
        fieldName: 'display_name',
        newValue: 'Pak Budi',
        justificationReason: 'Direct correction',
      });
      expect(teachingApiClient.applyCorrection).toHaveBeenCalled();
      expect(useApplyCorrectionMutation).toBe(useApplyRecordCorrectionMutation);

      const { result: approveResult } = withSetup(() => useApproveRecordCorrectionMutation());
      await approveResult.mutateAsync('cor-1');
      expect(teachingApiClient.approveCorrection).toHaveBeenCalledWith('cor-1');
      expect(useApproveCorrectionMutation).toBe(useApproveRecordCorrectionMutation);
    });
  });
});
