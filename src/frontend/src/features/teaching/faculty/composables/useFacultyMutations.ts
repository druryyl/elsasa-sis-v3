import { useMutation, useQueryClient } from '@tanstack/vue-query';
import {
  registerTeacher,
  updateTeacher,
  approveTeacher,
  changeStanding,
  addCapability,
  removeCapability,
  submitEvidence,
  verifyEvidence,
  rejectEvidence,
  grantAuthorization,
  updateAuthorizationStatus,
  addSupervision,
  resolveSupervision,
  applyCorrection,
  approveCorrection,
} from '../api/teachingApiClient';
import type {
  AddSupervisionRequirementRequest,
  AddSupervisionRequirementResponse,
  AddTeachingCapabilityRequest,
  AddTeachingCapabilityResponse,
  ApplyRecordCorrectionRequest,
  ApproveTeacherProfileResponse,
  ChangeTeacherStandingRequest,
  ChangeTeacherStandingResponse,
  GrantTeachingAuthorizationRequest,
  GrantTeachingAuthorizationResponse,
  RegisterTeacherProfileRequest,
  RegisterTeacherProfileResponse,
  RejectQualificationEvidenceRequest,
  RejectQualificationEvidenceResponse,
  ResolveSupervisionRequirementRequest,
  ResolveSupervisionRequirementResponse,
  SubmitQualificationEvidenceRequest,
  SubmitQualificationEvidenceResponse,
  TeachingRecordCorrectionDto,
  UpdateTeachingAuthorizationStatusRequest,
  UpdateTeachingAuthorizationStatusResponse,
  UpdateTeacherProfileRequest,
  VerifyQualificationEvidenceRequest,
  VerifyQualificationEvidenceResponse,
} from '../api/teachingTypes';
import { facultyKeys } from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export interface FacultyMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ----------------------------------------------------------------------------
// 1. Register Teacher Mutation
// ----------------------------------------------------------------------------
export function useRegisterTeacherMutation(
  options?: FacultyMutationOptions<RegisterTeacherProfileResponse, RegisterTeacherProfileRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: RegisterTeacherProfileRequest) => registerTeacher(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Faculty Registered',
        `Teacher profile registered successfully with number '${data.teacherNumber}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Registration Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 2. Update Teacher Mutation
// ----------------------------------------------------------------------------
export interface UpdateTeacherVariables {
  id: string;
  payload: UpdateTeacherProfileRequest;
}

export function useUpdateTeacherMutation(
  options?: FacultyMutationOptions<void, UpdateTeacherVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: UpdateTeacherVariables) =>
      updateTeacher(variables.id, variables.payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      notifySuccess(
        toast,
        'Profile Updated',
        'Teacher profile updated successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 3. Approve Teacher Mutation
// ----------------------------------------------------------------------------
export type ApproveTeacherVariables = string | { id: string };

export function useApproveTeacherMutation(
  options?: FacultyMutationOptions<ApproveTeacherProfileResponse, ApproveTeacherVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ApproveTeacherVariables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return approveTeacher(id);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      notifySuccess(
        toast,
        'Faculty Approved',
        `Teacher profile '${data.teacherNumber}' formally approved into active standing.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Approval Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 4. Change Standing Mutation
// ----------------------------------------------------------------------------
export interface ChangeStandingVariables {
  id: string;
  payload: ChangeTeacherStandingRequest;
}

export function useChangeStandingMutation(
  options?: FacultyMutationOptions<ChangeTeacherStandingResponse, ChangeStandingVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ChangeStandingVariables) =>
      changeStanding(variables.id, variables.payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      notifySuccess(
        toast,
        'Standing Updated',
        `Teacher standing updated to '${data.standing}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Standing Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 5. Add Capability Mutation
// ----------------------------------------------------------------------------
export interface AddCapabilityVariables {
  teacherId: string;
  payload: AddTeachingCapabilityRequest;
}

export function useAddCapabilityMutation(
  options?: FacultyMutationOptions<AddTeachingCapabilityResponse, AddCapabilityVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: AddCapabilityVariables) =>
      addCapability(variables.teacherId, variables.payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      notifySuccess(
        toast,
        'Capability Added',
        `Teaching capability '${data.capabilityCode}' added successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Add Capability Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 6. Remove Capability Mutation
// ----------------------------------------------------------------------------
export interface RemoveCapabilityVariables {
  teacherId: string;
  capabilityId: string;
}

export function useRemoveCapabilityMutation(
  options?: FacultyMutationOptions<void, RemoveCapabilityVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: RemoveCapabilityVariables) =>
      removeCapability(variables.teacherId, variables.capabilityId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      notifySuccess(
        toast,
        'Capability Removed',
        'Teaching capability removed successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Remove Capability Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 7. Submit Evidence Mutation
// ----------------------------------------------------------------------------
export interface SubmitEvidenceVariables {
  teacherId: string;
  payload: SubmitQualificationEvidenceRequest;
}

export function useSubmitEvidenceMutation(
  options?: FacultyMutationOptions<SubmitQualificationEvidenceResponse, SubmitEvidenceVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: SubmitEvidenceVariables) =>
      submitEvidence(variables.teacherId, variables.payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      notifySuccess(
        toast,
        'Evidence Submitted',
        'Qualification documentary evidence submitted successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Submission Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 8. Verify Evidence Mutation
// ----------------------------------------------------------------------------
export interface VerifyEvidenceVariables {
  id: string;
  payload?: VerifyQualificationEvidenceRequest;
  teacherId?: string;
}

export function useVerifyEvidenceMutation(
  options?: FacultyMutationOptions<VerifyQualificationEvidenceResponse, VerifyEvidenceVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: VerifyEvidenceVariables) =>
      verifyEvidence(variables.id, variables.payload),
    onSuccess: (data, variables) => {
      if (variables.teacherId) {
        queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      }
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Evidence Verified',
        'Qualification credential evidence successfully verified.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Verification Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 9. Reject Evidence Mutation
// ----------------------------------------------------------------------------
export interface RejectEvidenceVariables {
  id: string;
  payload: RejectQualificationEvidenceRequest;
  teacherId?: string;
}

export function useRejectEvidenceMutation(
  options?: FacultyMutationOptions<RejectQualificationEvidenceResponse, RejectEvidenceVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: RejectEvidenceVariables) =>
      rejectEvidence(variables.id, variables.payload),
    onSuccess: (data, variables) => {
      if (variables.teacherId) {
        queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      }
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Evidence Rejected',
        'Qualification credential evidence marked as rejected.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Rejection Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 10. Grant Authorization Mutation
// ----------------------------------------------------------------------------
export interface GrantAuthorizationVariables {
  teacherId: string;
  payload: GrantTeachingAuthorizationRequest;
}

export function useGrantAuthorizationMutation(
  options?: FacultyMutationOptions<GrantTeachingAuthorizationResponse, GrantAuthorizationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: GrantAuthorizationVariables) =>
      grantAuthorization(variables.teacherId, variables.payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      queryClient.invalidateQueries({ queryKey: facultyKeys.lists() });
      const warningText = data.unverifiedCredentialWarning
        ? ' (Permissive warning: unverified credentials attached)'
        : '';
      notifySuccess(
        toast,
        'Authorization Granted',
        `Teaching authorization '${data.authorizationNumber}' granted.${warningText}`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Grant Authorization Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 11. Update Authorization Status Mutation
// ----------------------------------------------------------------------------
export interface UpdateAuthorizationStatusVariables {
  id: string;
  payload: UpdateTeachingAuthorizationStatusRequest;
  teacherId?: string;
}

export function useUpdateAuthorizationStatusMutation(
  options?: FacultyMutationOptions<
    UpdateTeachingAuthorizationStatusResponse,
    UpdateAuthorizationStatusVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: UpdateAuthorizationStatusVariables) =>
      updateAuthorizationStatus(variables.id, variables.payload),
    onSuccess: (data, variables) => {
      if (variables.teacherId) {
        queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      }
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Authorization Status Updated',
        `Authorization status changed to '${data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Status Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 12. Add Supervision Mutation
// ----------------------------------------------------------------------------
export interface AddSupervisionVariables {
  authorizationId: string;
  payload: AddSupervisionRequirementRequest;
  teacherId?: string;
}

export function useAddSupervisionMutation(
  options?: FacultyMutationOptions<AddSupervisionRequirementResponse, AddSupervisionVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: AddSupervisionVariables) =>
      addSupervision(variables.authorizationId, variables.payload),
    onSuccess: (data, variables) => {
      if (variables.teacherId) {
        queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      }
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Supervision Added',
        'Supervision requirement attached to authorization successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Add Supervision Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 13. Resolve Supervision Mutation
// ----------------------------------------------------------------------------
export interface ResolveSupervisionVariables {
  id: string;
  payload: ResolveSupervisionRequirementRequest;
  teacherId?: string;
}

export function useResolveSupervisionMutation(
  options?: FacultyMutationOptions<
    ResolveSupervisionRequirementResponse,
    ResolveSupervisionVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ResolveSupervisionVariables) =>
      resolveSupervision(variables.id, variables.payload),
    onSuccess: (data, variables) => {
      if (variables.teacherId) {
        queryClient.invalidateQueries({ queryKey: facultyKeys.detail(variables.teacherId) });
      }
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Supervision Resolved',
        `Supervision condition resolved (status: '${data.status}').`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Resolve Supervision Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ----------------------------------------------------------------------------
// 14. Apply Record Correction Mutation
// ----------------------------------------------------------------------------
export function useApplyRecordCorrectionMutation(
  options?: FacultyMutationOptions<TeachingRecordCorrectionDto, ApplyRecordCorrectionRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: ApplyRecordCorrectionRequest) => applyCorrection(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Record Correction Processed',
        `Correction '${data.correctionNumber}' processed (status: '${data.status}').`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Correction Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Alias for useApplyRecordCorrectionMutation.
 */
export const useApplyCorrectionMutation = useApplyRecordCorrectionMutation;

// ----------------------------------------------------------------------------
// 15. Approve Record Correction Mutation
// ----------------------------------------------------------------------------
export type ApproveCorrectionVariables = string | { id: string; teacherId?: string };

export function useApproveRecordCorrectionMutation(
  options?: FacultyMutationOptions<TeachingRecordCorrectionDto, ApproveCorrectionVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ApproveCorrectionVariables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return approveCorrection(id);
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: facultyKeys.all });
      notifySuccess(
        toast,
        'Correction Approved',
        `Correction '${data.correctionNumber}' approved and applied to target record.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Approval Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Alias for useApproveRecordCorrectionMutation.
 */
export const useApproveCorrectionMutation = useApproveRecordCorrectionMutation;
