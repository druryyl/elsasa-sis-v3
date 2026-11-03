import { useMutation, useQueryClient } from '@tanstack/vue-query';
import {
  registerApplication,
  startApplicationReview,
  recordAdmissionDecision,
  withdrawApplication,
  uploadApplicantDocument,
  verifyApplicantDocument,
  addAssessmentEvidence,
  updateAssessmentEvidence,
  deleteAssessmentEvidence,
} from '../api/admission.api';
import type {
  RegisterApplicationPayload,
  RegisterApplicationResultDto,
  StartApplicationReviewResultDto,
  RecordAdmissionDecisionPayload,
  RecordAdmissionDecisionResultDto,
  WithdrawApplicationPayload,
  WithdrawApplicationResultDto,
  UploadApplicantDocumentResultDto,
  VerifyApplicantDocumentPayload,
  VerifyApplicantDocumentResultDto,
  AddAssessmentEvidencePayload,
  AddAssessmentEvidenceResultDto,
  UpdateAssessmentEvidencePayload,
  UpdateAssessmentEvidenceResultDto,
} from '../types/admission.types';
import { admissionKeys } from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export interface AdmissionMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface RegisterApplicationMutationVariables {
  payload: RegisterApplicationPayload;
  isPublicPortal?: boolean;
}

export type RegisterVariables = RegisterApplicationPayload | RegisterApplicationMutationVariables;

/**
 * Mutation hook registering an admission application (staff intake or public portal).
 * Invalidation pipeline automatically triggers refetching of all admission caches.
 */
export function useRegisterApplicationMutation(
  options?: AdmissionMutationOptions<RegisterApplicationResultDto, RegisterVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: RegisterVariables) => {
      if ('payload' in variables) {
        return registerApplication(variables.payload, variables.isPublicPortal ?? false);
      }
      return registerApplication(variables, false);
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Application Registered',
        `Admission application registered successfully with number '${data.applicationNumber}'.`,
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

export type StartReviewVariables = string | { id: string };

/**
 * Mutation hook transitioning an admission application from 'registered' to 'under_review'.
 */
export function useStartReviewMutation(
  options?: AdmissionMutationOptions<StartApplicationReviewResultDto, StartReviewVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: StartReviewVariables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return startApplicationReview(id);
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Review Started',
        'Application status updated to under review.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Start Review Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface RecordDecisionMutationVariables {
  id: string;
  payload: RecordAdmissionDecisionPayload;
}

/**
 * Mutation hook recording an official immutable admission decision.
 */
export function useRecordDecisionMutation(
  options?: AdmissionMutationOptions<RecordAdmissionDecisionResultDto, RecordDecisionMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, payload }: RecordDecisionMutationVariables) =>
      recordAdmissionDecision(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Decision Recorded',
        `Admission decision recorded successfully as '${data.decisionType}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Decision Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface WithdrawApplicationMutationVariables {
  id: string;
  payload: WithdrawApplicationPayload;
}

/**
 * Mutation hook withdrawing an active admission application.
 */
export function useWithdrawApplicationMutation(
  options?: AdmissionMutationOptions<WithdrawApplicationResultDto, WithdrawApplicationMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, payload }: WithdrawApplicationMutationVariables) =>
      withdrawApplication(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Application Withdrawn',
        'Admission application withdrawn successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Withdrawal Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface UploadDocumentMutationVariables {
  applicationId: string;
  documentType: string;
  file: File;
}

/**
 * Mutation hook uploading an applicant credential document.
 */
export function useUploadDocumentMutation(
  options?: AdmissionMutationOptions<UploadApplicantDocumentResultDto, UploadDocumentMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ applicationId, documentType, file }: UploadDocumentMutationVariables) =>
      uploadApplicantDocument(applicationId, documentType, file),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Document Uploaded',
        `Document '${data.fileName}' uploaded successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Document Upload Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface VerifyDocumentMutationVariables {
  applicationId: string;
  documentId: string;
  payload: VerifyApplicantDocumentPayload;
}

/**
 * Mutation hook verifying or rejecting an applicant credential document.
 */
export function useVerifyDocumentMutation(
  options?: AdmissionMutationOptions<VerifyApplicantDocumentResultDto, VerifyDocumentMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ applicationId, documentId, payload }: VerifyDocumentMutationVariables) =>
      verifyApplicantDocument(applicationId, documentId, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Document Verified',
        `Document status updated to '${data.submissionStatus}'.`,
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

export interface AddAssessmentEvidenceMutationVariables {
  applicationId: string;
  payload: AddAssessmentEvidencePayload;
}

/**
 * Mutation hook recording a manual entrance assessment diagnostic result.
 */
export function useAddAssessmentEvidenceMutation(
  options?: AdmissionMutationOptions<AddAssessmentEvidenceResultDto, AddAssessmentEvidenceMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ applicationId, payload }: AddAssessmentEvidenceMutationVariables) =>
      addAssessmentEvidence(applicationId, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Evidence Added',
        'Diagnostic assessment evidence recorded successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Add Evidence Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface UpdateAssessmentEvidenceMutationVariables {
  applicationId: string;
  evidenceId: string;
  payload: UpdateAssessmentEvidencePayload;
}

/**
 * Mutation hook updating an existing entrance assessment evidence record.
 */
export function useUpdateAssessmentEvidenceMutation(
  options?: AdmissionMutationOptions<UpdateAssessmentEvidenceResultDto, UpdateAssessmentEvidenceMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ applicationId, evidenceId, payload }: UpdateAssessmentEvidenceMutationVariables) =>
      updateAssessmentEvidence(applicationId, evidenceId, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Evidence Updated',
        'Assessment evidence updated successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Update Evidence Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface DeleteAssessmentEvidenceMutationVariables {
  applicationId: string;
  evidenceId: string;
}

/**
 * Mutation hook deleting an entrance assessment evidence record.
 */
export function useDeleteAssessmentEvidenceMutation(
  options?: AdmissionMutationOptions<void, DeleteAssessmentEvidenceMutationVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ applicationId, evidenceId }: DeleteAssessmentEvidenceMutationVariables) =>
      deleteAssessmentEvidence(applicationId, evidenceId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: admissionKeys.all });
      notifySuccess(
        toast,
        'Evidence Removed',
        'Assessment evidence removed successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Delete Evidence Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface AssessmentEvidenceMutationsOptions {
  add?: AdmissionMutationOptions<AddAssessmentEvidenceResultDto, AddAssessmentEvidenceMutationVariables>;
  update?: AdmissionMutationOptions<UpdateAssessmentEvidenceResultDto, UpdateAssessmentEvidenceMutationVariables>;
  delete?: AdmissionMutationOptions<void, DeleteAssessmentEvidenceMutationVariables>;
}

/**
 * Composite hook exposing all 3 assessment evidence mutations (add, update, delete).
 */
export function useAssessmentEvidenceMutations(options?: AssessmentEvidenceMutationsOptions) {
  const addMutation = useAddAssessmentEvidenceMutation(options?.add);
  const updateMutation = useUpdateAssessmentEvidenceMutation(options?.update);
  const deleteMutation = useDeleteAssessmentEvidenceMutation(options?.delete);

  return {
    addMutation,
    updateMutation,
    deleteMutation,
    addEvidence: addMutation.mutateAsync,
    updateEvidence: updateMutation.mutateAsync,
    deleteEvidence: deleteMutation.mutateAsync,
  };
}
