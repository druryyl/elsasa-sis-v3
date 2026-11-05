<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { GraduationCap, AlertTriangle } from 'lucide-vue-next';
import { useGenerateTranscriptMutation } from '../composables/useTranscripts';
import type { Transcript, TranscriptType } from '../types';

const props = defineProps<{
  isOpen: boolean;
  initialStudentId?: string;
}>();

const emit = defineEmits<{
  (e: 'update:isOpen', value: boolean): void;
  (e: 'generated', transcript: Transcript): void;
}>();

const generateMutation = useGenerateTranscriptMutation();

const studentId = ref<string>('');
const transcriptType = ref<TranscriptType>('official');
const periodStart = ref<string>('2024-07-01');
const periodEnd = ref<string>('2026-06-30');
const purpose = ref<string>('');
const notes = ref<string>('');
const validationError = ref<string | null>(null);

const typeOptions = [
  { label: 'Official Transcript (Resmi)', value: 'official' },
  { label: 'Unofficial / Student Copy (Tidak Resmi)', value: 'unofficial' },
];

watch(
  () => props.isOpen,
  (open) => {
    if (open) {
      studentId.value = props.initialStudentId || '';
      transcriptType.value = 'official';
      periodStart.value = '2024-07-01';
      periodEnd.value = '2026-06-30';
      purpose.value = '';
      notes.value = '';
      validationError.value = null;
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:isOpen', false);
}

async function handleGenerate() {
  validationError.value = null;

  if (!studentId.value.trim()) {
    validationError.value = 'Student identifier is required.';
    return;
  }

  if (!periodStart.value || !periodEnd.value) {
    validationError.value = 'Period start and end dates are required.';
    return;
  }

  if (periodStart.value > periodEnd.value) {
    validationError.value = 'Period end date must be on or after period start date.';
    return;
  }

  try {
    const transcript = await generateMutation.mutateAsync({
      studentId: studentId.value.trim(),
      transcriptType: transcriptType.value,
      periodStart: periodStart.value,
      periodEnd: periodEnd.value,
      purpose: purpose.value.trim() || undefined,
      notes: notes.value.trim() || undefined,
    });
    emit('generated', transcript);
    handleClose();
  } catch (err: unknown) {
    if (err instanceof Error) {
      validationError.value = err.message;
    } else {
      validationError.value = 'Failed to generate interim transcript.';
    }
  }
}
</script>

<template>
  <Dialog
    :visible="isOpen"
    modal
    header="Generate Interim Transcript"
    :style="{ width: '560px' }"
    :breakpoints="{ '960px': '75vw', '640px': '95vw' }"
    data-testid="generate-transcript-modal"
    @update:visible="(val) => emit('update:isOpen', val)"
  >
    <div class="space-y-4 py-1 text-xs">
      <div class="p-3 bg-surface-50 dark:bg-surface-800/60 rounded-lg border border-surface-200 dark:border-surface-700 flex items-start gap-2.5">
        <GraduationCap class="w-4 h-4 shrink-0 text-teal-600 mt-0.5" />
        <div class="text-surface-600 dark:text-surface-300">
          <span class="font-semibold text-surface-900 dark:text-surface-100 block mb-0.5">
            Cumulative Interim Transcript (UC-ACD-13 / GAP-08)
          </span>
          Generates a draft transcript compiling all completed academic periods and courses, tagging recognized prior learning or transfer credits with <strong class="font-mono text-teal-700 dark:text-teal-400">[T]</strong>.
        </div>
      </div>

      <!-- Error alert -->
      <div
        v-if="validationError"
        class="p-3 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-lg flex items-start gap-2 text-rose-700 dark:text-rose-300"
        data-testid="generate-transcript-error-alert"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 mt-0.5" />
        <span>{{ validationError }}</span>
      </div>

      <div class="space-y-3.5">
        <div>
          <label for="transcript-student-id" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Student ID <span class="text-rose-500">*</span>
          </label>
          <InputText
            id="transcript-student-id"
            v-model="studentId"
            class="w-full text-xs"
            placeholder="e.g. STU-2024-001 or UUID"
            data-testid="input-transcript-student-id"
          />
        </div>

        <div>
          <label for="transcript-type" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Transcript Type <span class="text-rose-500">*</span>
          </label>
          <Select
            id="transcript-type"
            v-model="transcriptType"
            :options="typeOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="select-transcript-type"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label for="transcript-start" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
              Period Start Date <span class="text-rose-500">*</span>
            </label>
            <input
              id="transcript-start"
              v-model="periodStart"
              type="date"
              class="w-full px-3 py-2 text-xs rounded-lg border border-surface-300 dark:border-surface-600 bg-surface-0 dark:bg-surface-900 focus:outline-none focus:ring-2 focus:ring-teal-500"
              data-testid="input-transcript-period-start"
            />
          </div>

          <div>
            <label for="transcript-end" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
              Period End Date <span class="text-rose-500">*</span>
            </label>
            <input
              id="transcript-end"
              v-model="periodEnd"
              type="date"
              class="w-full px-3 py-2 text-xs rounded-lg border border-surface-300 dark:border-surface-600 bg-surface-0 dark:bg-surface-900 focus:outline-none focus:ring-2 focus:ring-teal-500"
              data-testid="input-transcript-period-end"
            />
          </div>
        </div>

        <div>
          <label for="transcript-purpose" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Issuance Purpose (Optional)
          </label>
          <InputText
            id="transcript-purpose"
            v-model="purpose"
            class="w-full text-xs"
            placeholder="e.g. University Admission / Scholarship Application"
            data-testid="input-transcript-purpose"
          />
        </div>

        <div>
          <label for="transcript-notes" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Notes / Administrative Remarks (Optional)
          </label>
          <Textarea
            id="transcript-notes"
            v-model="notes"
            rows="2"
            class="w-full text-xs"
            placeholder="Internal registrar notes..."
            data-testid="input-transcript-notes"
          />
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-100 dark:border-surface-800">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          class="cursor-pointer"
          data-testid="btn-cancel-generate-transcript"
          @click="handleClose"
        />
        <Button
          label="Generate Interim Transcript"
          severity="primary"
          size="small"
          :loading="generateMutation.isPending.value"
          class="cursor-pointer"
          data-testid="btn-submit-generate-transcript"
          @click="handleGenerate"
        />
      </div>
    </template>
  </Dialog>
</template>
