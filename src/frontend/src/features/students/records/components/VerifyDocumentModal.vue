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
    :style="{ width: '560px' }"
    :breakpoints="{ '960px': '75vw', '640px': '95vw' }"
    data-testid="verify-document-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-xs">
          <CheckCircle2 class="w-5 h-5" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2.5">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Verify Document — {{ document?.documentType || 'Official Document' }}
            </h3>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800">
              Audit Review
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Audit submitted document authenticity and record verification determination.
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-5 pt-3 pb-1">
      <!-- Document Summary Card -->
      <div v-if="document" class="p-4 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/60 dark:bg-surface-900/40 text-xs space-y-2">
        <div class="flex items-center justify-between">
          <span class="text-surface-500 font-medium">Document Type:</span>
          <span class="font-semibold text-surface-900 dark:text-surface-100">{{ document.documentType }}</span>
        </div>
        <div v-if="document.fileName" class="flex items-center justify-between">
          <span class="text-surface-500 font-medium">Uploaded File:</span>
          <span class="font-mono text-surface-700 dark:text-surface-300 font-semibold">{{ document.fileName }}</span>
        </div>
      </div>

      <!-- Decision Radios Card -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-3">
        <label class="block text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          Verification Decision <span class="text-rose-500">*</span>
        </label>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div
            class="flex items-center gap-3 p-3.5 rounded-xl border cursor-pointer transition-all shadow-2xs"
            :class="[
              submissionStatus === 'verified'
                ? 'border-emerald-500 bg-emerald-50/80 dark:bg-emerald-950/40 text-emerald-900 dark:text-emerald-200 ring-2 ring-emerald-500/30'
                : 'border-surface-200 dark:border-surface-700 hover:bg-white dark:hover:bg-surface-800 bg-surface-100/40'
            ]"
            data-testid="decision-verified"
            @click="submissionStatus = 'verified'"
          >
            <CheckCircle2 class="w-5 h-5 text-emerald-600 shrink-0" />
            <div class="text-xs">
              <span class="font-bold block">Verified &amp; Accepted</span>
              <span class="text-surface-500 text-[11px] block">Document validated as genuine</span>
            </div>
          </div>

          <div
            class="flex items-center gap-3 p-3.5 rounded-xl border cursor-pointer transition-all shadow-2xs"
            :class="[
              submissionStatus === 'rejected'
                ? 'border-rose-500 bg-rose-50/80 dark:bg-rose-950/40 text-rose-900 dark:text-rose-200 ring-2 ring-rose-500/30'
                : 'border-surface-200 dark:border-surface-700 hover:bg-white dark:hover:bg-surface-800 bg-surface-100/40'
            ]"
            data-testid="decision-rejected"
            @click="submissionStatus = 'rejected'"
          >
            <XCircle class="w-5 h-5 text-rose-600 shrink-0" />
            <div class="text-xs">
              <span class="font-bold block">Rejected</span>
              <span class="text-surface-500 text-[11px] block">Illegible, expired, or invalid</span>
            </div>
          </div>
        </div>
        <p v-if="errors.submissionStatus" class="text-xs text-rose-600 mt-1 font-medium">
          {{ errors.submissionStatus }}
        </p>

        <!-- Verification Remarks -->
        <div class="pt-2">
          <FormField label="Remarks / Verification Notes" :error="errors.notes">
            <Textarea
              v-model="notes"
              rows="3"
              :placeholder="submissionStatus === 'rejected' ? 'State specific reason for rejection (e.g. illegible photocopy, mismatched civil name, expired certificate)...' : 'Remarks or certification notes...'"
              class="w-full text-sm leading-relaxed"
              :invalid="Boolean(errors.notes)"
              data-testid="input-verify-notes"
            />
          </FormField>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-3 pt-3 border-t border-surface-200 dark:border-surface-800">
        <Button
          size="small"
          variant="outlined"
          severity="secondary"
          label="Cancel"
          :disabled="isPending"
          data-testid="btn-cancel-verify"
          class="!px-4 !py-2 !text-xs font-medium cursor-pointer"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          :label="submissionStatus === 'verified' ? 'Confirm Verification' : 'Confirm Rejection'"
          icon="pi pi-check"
          :loading="isPending"
          :class="submissionStatus === 'verified' ? '!bg-emerald-700 hover:!bg-emerald-800 !border-emerald-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs' : '!bg-rose-700 hover:!bg-rose-800 !border-rose-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs'"
          data-testid="btn-submit-verify"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
