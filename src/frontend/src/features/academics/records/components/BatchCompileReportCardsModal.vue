<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { Layers, AlertTriangle, CheckCircle2 } from 'lucide-vue-next';
import { useBatchCompileReportCardsMutation } from '../composables/useReportCards';
import type { ReportType, BatchCompilationSummary } from '../types';

const props = defineProps<{
  isOpen: boolean;
  defaultPeriodId?: string;
  defaultPeriodLabel?: string;
}>();

const emit = defineEmits<{
  (e: 'update:isOpen', value: boolean): void;
  (e: 'compiled', summary: BatchCompilationSummary): void;
}>();

const batchMutation = useBatchCompileReportCardsMutation();

const academicPeriodId = ref<string>('');
const academicPeriodLabel = ref<string>('');
const reportType = ref<ReportType>('end_of_term');
const templateName = ref<string>('Buku Rapor Merdeka');
const sectionLabel = ref<string>('');
const validationError = ref<string | null>(null);
const compilationResult = ref<BatchCompilationSummary | null>(null);

const reportTypeOptions = [
  { label: 'End of Semester — Buku Rapor Merdeka (end_of_term)', value: 'end_of_term' },
  { label: 'Mid-Term Progress Report (mid_term)', value: 'mid_term' },
];

watch(
  () => props.isOpen,
  (open) => {
    if (open) {
      academicPeriodId.value = props.defaultPeriodId || '2026-ganjil';
      academicPeriodLabel.value = props.defaultPeriodLabel || 'Semester Ganjil 2026/2027';
      reportType.value = 'end_of_term';
      templateName.value = 'Buku Rapor Merdeka';
      sectionLabel.value = '';
      validationError.value = null;
      compilationResult.value = null;
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:isOpen', false);
}

async function handleCompile() {
  validationError.value = null;
  compilationResult.value = null;

  if (!academicPeriodId.value.trim()) {
    validationError.value = 'Academic period identifier is required.';
    return;
  }

  if (!templateName.value.trim()) {
    validationError.value = 'Template name is required.';
    return;
  }

  try {
    const summary = await batchMutation.mutateAsync({
      academicPeriodId: academicPeriodId.value.trim(),
      academicPeriodLabel: academicPeriodLabel.value.trim() || 'Semester Ganjil 2026/2027',
      reportType: reportType.value,
      templateName: templateName.value.trim(),
      sectionLabel: sectionLabel.value.trim() || undefined,
    });
    compilationResult.value = summary;
    emit('compiled', summary);
  } catch (err: unknown) {
    if (err instanceof Error) {
      validationError.value = err.message;
    } else {
      validationError.value = 'Batch compilation failed. Please check inputs and try again.';
    }
  }
}
</script>

<template>
  <Dialog
    :visible="isOpen"
    modal
    header="Batch Compile Cohort Report Cards"
    :style="{ width: '560px' }"
    :breakpoints="{ '960px': '75vw', '640px': '95vw' }"
    data-testid="batch-compile-report-cards-modal"
    @update:visible="(val) => emit('update:isOpen', val)"
  >
    <div class="space-y-4 py-1 text-xs">
      <div class="p-3 bg-surface-50 dark:bg-surface-800/60 rounded-lg border border-surface-200 dark:border-surface-700 flex items-start gap-2.5">
        <Layers class="w-4 h-4 shrink-0 text-teal-600 mt-0.5" />
        <div class="text-surface-600 dark:text-surface-300">
          <span class="font-semibold text-surface-900 dark:text-surface-100 block mb-0.5">
            Idempotent Set-Based Compilation (TD-07)
          </span>
          Compiles draft report cards for all enrolled students across the cohort. Existing report cards will be safely skipped to preserve idempotency.
        </div>
      </div>

      <!-- Result summary banner -->
      <div
        v-if="compilationResult"
        class="p-3 bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 rounded-lg flex items-start gap-2 text-emerald-800 dark:text-emerald-200"
        data-testid="compilation-success-summary"
      >
        <CheckCircle2 class="w-4 h-4 shrink-0 mt-0.5 text-emerald-600" />
        <div>
          <span class="font-bold block">Cohort Compilation Complete!</span>
          <span>Successfully compiled <strong>{{ compilationResult.compiledCount }}</strong> report cards. Skipped <strong>{{ compilationResult.skippedExistingCount }}</strong> existing records.</span>
        </div>
      </div>

      <!-- Error alert -->
      <div
        v-if="validationError"
        class="p-3 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-lg flex items-start gap-2 text-rose-700 dark:text-rose-300"
        data-testid="compilation-error-alert"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 mt-0.5" />
        <span>{{ validationError }}</span>
      </div>

      <!-- Form Inputs -->
      <div class="space-y-3.5">
        <div>
          <label for="batch-period-id" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Academic Period ID <span class="text-rose-500">*</span>
          </label>
          <InputText
            id="batch-period-id"
            v-model="academicPeriodId"
            class="w-full text-xs"
            placeholder="e.g. 2026-ganjil"
            data-testid="input-batch-period-id"
          />
        </div>

        <div>
          <label for="batch-period-label" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Academic Period Label
          </label>
          <InputText
            id="batch-period-label"
            v-model="academicPeriodLabel"
            class="w-full text-xs"
            placeholder="e.g. Semester Ganjil 2026/2027"
            data-testid="input-batch-period-label"
          />
        </div>

        <div>
          <label for="batch-report-type" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Report Type <span class="text-rose-500">*</span>
          </label>
          <Select
            id="batch-report-type"
            v-model="reportType"
            :options="reportTypeOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="select-batch-report-type"
          />
        </div>

        <div>
          <label for="batch-template" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Template Name <span class="text-rose-500">*</span>
          </label>
          <InputText
            id="batch-template"
            v-model="templateName"
            class="w-full text-xs"
            placeholder="Buku Rapor Merdeka"
            data-testid="input-batch-template-name"
          />
        </div>

        <div>
          <label for="batch-section" class="block font-semibold text-surface-800 dark:text-surface-200 mb-1">
            Section / Class Cohort Filter (Optional)
          </label>
          <InputText
            id="batch-section"
            v-model="sectionLabel"
            class="w-full text-xs"
            placeholder="e.g. 10-A, 10-B (Leave empty to compile entire cohort)"
            data-testid="input-batch-section-label"
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
          data-testid="btn-cancel-batch-compile"
          @click="handleClose"
        />
        <Button
          label="Batch Compile Cohort"
          severity="primary"
          size="small"
          :loading="batchMutation.isPending.value"
          class="cursor-pointer"
          data-testid="btn-submit-batch-compile"
          @click="handleCompile"
        />
      </div>
    </template>
  </Dialog>
</template>
