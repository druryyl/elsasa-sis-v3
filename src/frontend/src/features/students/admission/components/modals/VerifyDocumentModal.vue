<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { CheckCircle2, ShieldCheck } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  verifyDocumentSchema,
  type VerifyDocumentFormData,
} from '../../schemas/admission.schema';
import { useVerifyDocumentMutation } from '../../composables/useAdmissionMutations';
import type {
  ApplicantDocumentDto,
  VerifyApplicantDocumentResultDto,
} from '../../types/admission.types';

const props = defineProps<{
  visible: boolean;
  applicationId: string;
  document: ApplicantDocumentDto | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'verified', result: VerifyApplicantDocumentResultDto): void;
}>();

const form = ref<VerifyDocumentFormData>({
  submissionStatus: 'verified',
  notes: '',
});

const errors = ref<Record<string, string>>({});

watch(
  () => [props.visible, props.document] as const,
  ([isOpen, doc]) => {
    if (isOpen) {
      errors.value = {};
      form.value = {
        submissionStatus: doc?.submissionStatus === 'rejected' ? 'rejected' : 'verified',
        notes: doc?.notes ?? '',
      };
    }
  },
  { immediate: true },
);

const verifyMutation = useVerifyDocumentMutation({
  onSuccess: (data) => {
    emit('verified', data);
    emit('update:visible', false);
  },
});

function handleSubmit() {
  errors.value = {};
  const result = verifyDocumentSchema.safeParse(form.value);
  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] as string;
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return;
  }

  if (!props.document) return;
  verifyMutation.mutate({
    applicationId: props.applicationId,
    documentId: props.document.id,
    payload: {
      submissionStatus: result.data.submissionStatus,
      notes: result.data.notes?.trim() || null,
    },
  });
}

function handleClose() {
  emit('update:visible', false);
}

function formatDocType(type?: string): string {
  if (!type) return 'Document';
  return type
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Verify Credential Document"
    class="max-w-md w-full"
    data-testid="verify-document-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 pt-1 text-xs" @submit.prevent="handleSubmit">
      <!-- Document Summary Callout -->
      <div class="p-3 bg-slate-50 rounded-lg border border-slate-200/80 space-y-1">
        <div class="flex items-center gap-1.5 text-slate-800 font-semibold">
          <ShieldCheck class="w-4 h-4 text-teal-600" />
          <span>{{ formatDocType(document?.documentType) }}</span>
        </div>
        <p class="text-[11px] text-slate-500 font-mono">
          File: {{ document?.fileName || 'Attached file' }}
        </p>
      </div>

      <!-- Verification Status -->
      <FormField label="Verification Determination" required :error="errors.submissionStatus">
        <select
          v-model="form.submissionStatus"
          class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800"
          data-testid="verify-status-select"
        >
          <option value="verified">Verified (Approved / Dokumen Sah)</option>
          <option value="rejected">Rejected (Needs Correction / Ditolak)</option>
        </select>
      </FormField>

      <!-- Justification Notes -->
      <FormField
        :label="form.submissionStatus === 'rejected' ? 'Rejection Reason / Guidance *' : 'Verification Notes (Optional)'"
        :error="errors.notes"
      >
        <Textarea
          v-model="form.notes"
          rows="3"
          :placeholder="form.submissionStatus === 'rejected' ? 'Explain what is missing or incorrect with the uploaded document...' : 'Add any audit notes regarding the verified document...'"
          class="w-full !text-xs"
          data-testid="verify-notes-textarea"
        />
      </FormField>

      <!-- Footer Actions -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="!text-xs cursor-pointer"
          @click="handleClose"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="small"
          :severity="form.submissionStatus === 'rejected' ? 'danger' : 'primary'"
          :loading="verifyMutation.isPending.value"
          class="!text-xs cursor-pointer"
          data-testid="confirm-verify-button"
        >
          <CheckCircle2 class="w-3.5 h-3.5 mr-1" />
          <span>Save Verification</span>
        </Button>
      </div>
    </form>
  </Dialog>
</template>
