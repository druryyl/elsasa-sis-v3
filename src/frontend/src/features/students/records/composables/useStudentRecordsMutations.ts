import { toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQueryClient } from '@tanstack/vue-query';
import {
  updateStudentIdentity,
  addGuardianRelationship,
  updateGuardianRelationship,
  uploadStudentDocument,
  verifyStudentDocument,
} from '../api/records.api';
import type {
  AddGuardianPayload,
  UpdateGuardianPayload,
  UpdateIdentityPayload,
  VerifyDocumentPayload,
} from '../types/records.types';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export interface RecordsMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export type UpdateStudentIdentityVariables =
  | UpdateIdentityPayload
  | { studentId?: string; id?: string; payload: UpdateIdentityPayload };

/**
 * Mutation composable for updating certified student identity attributes (SCR-STU-02, P5-S07, P8-S13).
 * Invalidates detail query `['students', 'records', 'detail', studentId]` on success.
 */
export function useUpdateStudentIdentityMutation(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordsMutationOptions<void, UpdateStudentIdentityVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: async (variables: UpdateStudentIdentityVariables) => {
      const isWrapped = 'payload' in variables;
      const targetId = isWrapped
        ? (variables.studentId ?? variables.id ?? toValue(studentId))
        : toValue(studentId);

      if (!targetId) {
        throw new Error('Student ID is required to update student identity.');
      }

      const payload = isWrapped ? variables.payload : variables;
      await updateStudentIdentity(targetId, payload);
    },
    onSuccess: (_, variables) => {
      const isWrapped = 'payload' in variables;
      const targetId = isWrapped
        ? (variables.studentId ?? variables.id ?? toValue(studentId))
        : toValue(studentId);

      if (targetId) {
        queryClient.invalidateQueries({ queryKey: ['students', 'records', 'detail', targetId] });
      }
      queryClient.invalidateQueries({ queryKey: ['students', 'records', 'list'] });

      notifySuccess(
        toast,
        'Identity Updated',
        'Certified student identity attributes have been saved.',
        options,
      );
      options?.onSuccess?.(undefined, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Identity Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export type AddGuardianVariables =
  | AddGuardianPayload
  | { studentId?: string; payload: AddGuardianPayload };

/**
 * Mutation composable for linking a new guardian relationship to a student (SCR-STU-02, P5-S08, P8-S13).
 * Invalidates detail query `['students', 'records', 'detail', studentId]` on success.
 */
export function useAddGuardianMutation(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordsMutationOptions<string, AddGuardianVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: async (variables: AddGuardianVariables) => {
      const isWrapped = 'payload' in variables;
      const targetId = isWrapped
        ? (variables.studentId ?? toValue(studentId))
        : toValue(studentId);

      if (!targetId) {
        throw new Error('Student ID is required to add a guardian relationship.');
      }

      const payload = isWrapped ? variables.payload : variables;
      return addGuardianRelationship(targetId, payload);
    },
    onSuccess: (data, variables) => {
      const isWrapped = 'payload' in variables;
      const targetId = isWrapped
        ? (variables.studentId ?? toValue(studentId))
        : toValue(studentId);

      if (targetId) {
        queryClient.invalidateQueries({ queryKey: ['students', 'records', 'detail', targetId] });
      }

      notifySuccess(
        toast,
        'Guardian Added',
        'Guardian relationship successfully linked to student.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Add Guardian Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface UpdateGuardianVariables {
  relationshipId: string;
  payload: UpdateGuardianPayload;
  studentId?: string;
}

/**
 * Mutation composable for updating an existing guardian relationship and authority flags (SCR-STU-02, P5-S08, P8-S13).
 * Invalidates detail query `['students', 'records', 'detail', studentId]` on success.
 */
export function useUpdateGuardianMutation(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordsMutationOptions<void, UpdateGuardianVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: async (variables: UpdateGuardianVariables) => {
      const targetId = variables.studentId ?? toValue(studentId);
      if (!targetId) {
        throw new Error('Student ID is required to update a guardian relationship.');
      }

      await updateGuardianRelationship(targetId, variables.relationshipId, variables.payload);
    },
    onSuccess: (_, variables) => {
      const targetId = variables.studentId ?? toValue(studentId);
      if (targetId) {
        queryClient.invalidateQueries({ queryKey: ['students', 'records', 'detail', targetId] });
      }

      notifySuccess(
        toast,
        'Guardian Updated',
        'Guardian relationship authorities and details updated.',
        options,
      );
      options?.onSuccess?.(undefined, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Update Guardian Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface UploadStudentDocumentVariables {
  file: File;
  documentType: string;
  studentId?: string;
}

/**
 * Mutation composable for uploading an official certified student document (SCR-STU-02, P6-S09, P8-S13).
 * Invalidates detail query `['students', 'records', 'detail', studentId]` on success.
 */
export function useUploadStudentDocumentMutation(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordsMutationOptions<string, UploadStudentDocumentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: async (variables: UploadStudentDocumentVariables) => {
      const targetId = variables.studentId ?? toValue(studentId);
      if (!targetId) {
        throw new Error('Student ID is required to upload a student document.');
      }

      return uploadStudentDocument(targetId, variables.file, variables.documentType);
    },
    onSuccess: (data, variables) => {
      const targetId = variables.studentId ?? toValue(studentId);
      if (targetId) {
        queryClient.invalidateQueries({ queryKey: ['students', 'records', 'detail', targetId] });
      }

      notifySuccess(
        toast,
        'Document Uploaded',
        'Official student document uploaded successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Upload Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface VerifyStudentDocumentVariables {
  documentId?: string;
  docId?: string;
  status?: string;
  submissionStatus?: string;
  notes?: string | null;
  studentId?: string;
}

/**
 * Mutation composable for verifying or rejecting an official student document (SCR-STU-02, P6-S09, P8-S13).
 * Invalidates detail query `['students', 'records', 'detail', studentId]` on success.
 */
export function useVerifyStudentDocumentMutation(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordsMutationOptions<void, VerifyStudentDocumentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: async (variables: VerifyStudentDocumentVariables) => {
      const targetId = variables.studentId ?? toValue(studentId);
      const docId = variables.documentId || variables.docId;
      const status = variables.submissionStatus || variables.status;

      if (!targetId) {
        throw new Error('Student ID is required to verify a student document.');
      }
      if (!docId) {
        throw new Error('Document ID is required to verify a student document.');
      }
      if (!status) {
        throw new Error('Verification status is required.');
      }

      const payload: VerifyDocumentPayload = {
        submissionStatus: status,
        notes: variables.notes ?? null,
      };

      await verifyStudentDocument(targetId, docId, payload);
    },
    onSuccess: (_, variables) => {
      const targetId = variables.studentId ?? toValue(studentId);
      if (targetId) {
        queryClient.invalidateQueries({ queryKey: ['students', 'records', 'detail', targetId] });
      }

      notifySuccess(
        toast,
        'Verification Recorded',
        'Document verification decision recorded successfully.',
        options,
      );
      options?.onSuccess?.(undefined, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Verification Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
