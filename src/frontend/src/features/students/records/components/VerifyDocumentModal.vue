<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { CheckCircle2, XCircle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { verifyDocumentSchema } from '../schemas/records.schema';
import { useVerifyStudentDocumentMutation } from '../composables/useStudentRecordsMutations';
import type { StudentDocumentItem } from '../types/records.types';

const props = defineProps<{
  visible: boolean;
  studentId: string;
  document: StudentDocumentItem | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved'): void;
}>();

const submissionStatus = ref<'verified' | 'rejected'>('verified');
const notes = ref('');
const errors = ref<Record<string, string>>({});

function resetForm() {
  submissionStatus.value = 'verified';
  notes.value = '';
  errors.value = {};
}

watch(
  () => [props.visible, props.document],
  ([newVisible]) => {
    if (newVisible) {
      resetForm();
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:visible', false);
}

// Mutation
const studentIdRef = computed(() => props.studentId);
const { mutateAsync: verifyDocument, isPending } = useVerifyStudentDocumentMutation(studentIdRef);

async function handleSubmit() {
  if (!props.document) return;
  errors.value = {};

  const payload = {
    submissionStatus: submissionStatus.value,
    notes: notes.value.trim() || null,
  };

  const validation = verifyDocumentSchema.safeParse(payload);
  if (!validation.success) {
    const formatted = validation.error.format();
    if (formatted.submissionStatus?._errors?.length) {
      errors.value.submissionStatus = formatted.submissionStatus._errors[0];
    }
    if (formatted.notes?._errors?.length) {
      errors.value.notes = formatted.notes._errors[0];
    }
    return;
  }

  try {
    await verifyDocument({
      documentId: props.document.id,
      submissionStatus: submissionStatus.value,
      notes: payload.notes,
    });
    emit('saved');
    handleClose();
  } catch {
    // Handled by toast notification
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :header="`Verify Document — ${document?.documentType || 'Official Document'}`"
    class="w-full max-w-md"
    data-testid="verify-document-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="space-y-4 py-2">
      <!-- Document Summary -->
      <div v-if="document" class="p-3 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/40 text-xs space-y-1">
        <div class="flex justify-between">
          <span class="text-surface-500">Document Type:</span>
          <span class="font-semibold text-surface-900 dark:text-surface-100">{{ document.documentType }}</span>
        </div>
        <div v-if="document.fileName" class="flex justify-between">
          <span class="text-surface-500">File Name:</span>
          <span class="font-mono text-surface-700 dark:text-surface-300">{{ document.fileName }}</span>
        </div>
      </div>

      <!-- Decision Radios -->
      <div class="space-y-2">
        <label class="block text-xs font-semibold text-surface-900 dark:text-surface-100">
          Verification Decision <span class="text-rose-500">*</span>
        </label>
        <div class="grid grid-cols-2 gap-3">
          <div
            class="flex items-center gap-2 p-3 rounded-lg border cursor-pointer transition-all"
            :class="[
              submissionStatus === 'verified'
                ? 'border-emerald-500 bg-emerald-50/60 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 ring-1 ring-emerald-500'
                : 'border-surface-200 dark:border-surface-700 hover:bg-surface-50'
            ]"
            data-testid="decision-verified"
            @click="submissionStatus = 'verified'"
          >
            <CheckCircle2 class="w-4 h-4 text-emerald-600" />
            <span class="text-xs font-semibold">Verified &amp; Accepted</span>
          </div>

          <div
            class="flex items-center gap-2 p-3 rounded-lg border cursor-pointer transition-all"
            :class="[
              submissionStatus === 'rejected'
                ? 'border-rose-500 bg-rose-50/60 dark:bg-rose-950/40 text-rose-800 dark:text-rose-300 ring-1 ring-rose-500'
                : 'border-surface-200 dark:border-surface-700 hover:bg-surface-50'
            ]"
            data-testid="decision-rejected"
            @click="submissionStatus = 'rejected'"
          >
            <XCircle class="w-4 h-4 text-rose-600" />
            <span class="text-xs font-semibold">Rejected</span>
          </div>
        </div>
        <p v-if="errors.submissionStatus" class="text-xs text-rose-600 mt-1">
          {{ errors.submissionStatus }}
        </p>
      </div>

      <!-- Verification Remarks -->
      <FormField label="Remarks / Verification Notes" :error="errors.notes">
        <Textarea
          v-model="notes"
          rows="3"
          :placeholder="submissionStatus === 'rejected' ? 'State reason for rejection (e.g. illegible copy, mismatched name)...' : 'Remarks or certification notes...'"
          class="w-full text-xs"
          :invalid="Boolean(errors.notes)"
          data-testid="input-verify-notes"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          size="small"
          variant="text"
          severity="secondary"
          label="Cancel"
          :disabled="isPending"
          data-testid="btn-cancel-verify"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          :label="submissionStatus === 'verified' ? 'Confirm Verification' : 'Confirm Rejection'"
          :loading="isPending"
          :class="submissionStatus === 'verified' ? '!bg-emerald-700 hover:!bg-emerald-800 !border-emerald-700' : '!bg-rose-700 hover:!bg-rose-800 !border-rose-700'"
          data-testid="btn-submit-verify"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
