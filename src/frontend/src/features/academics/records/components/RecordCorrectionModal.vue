<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Checkbox from 'primevue/checkbox';
import Accordion from 'primevue/accordion';
import AccordionPanel from 'primevue/accordionpanel';
import AccordionHeader from 'primevue/accordionheader';
import AccordionContent from 'primevue/accordioncontent';
import {
  AlertTriangle,
  History,
  ShieldAlert,
} from 'lucide-vue-next';
import { useAuthStore } from '@/app/stores/auth.store';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { AcademicResult, CorrectResultRequest } from '../types';
import {
  useCorrectResultMutation,
  useCorrectionHistoryQuery,
} from '../composables/useRecordCorrections';
import { RECORDS_PERMISSIONS, AUTHORITY_ROLES } from '../constants/permissions';

const props = defineProps<{
  isOpen: boolean;
  result: AcademicResult | null;
}>();

const emit = defineEmits<{
  (e: 'update:isOpen', value: boolean): void;
  (e: 'corrected', result: AcademicResult): void;
}>();

const authStore = useAuthStore();
const isAuthority = computed(() =>
  authStore.hasAnyRole([...AUTHORITY_ROLES]) ||
  authStore.hasPermission(RECORDS_PERMISSIONS.RESULTS_APPROVE)
);

// Form state
const scoreNumeric = ref<number | null>(null);
const scoreMax = ref<number | null>(null);
const gradeLetter = ref<string>('');
const gradeLabel = ref<string>('');
const narrativeFeedback = ref<string>('');
const reason = ref<string>('');
const evidence = ref<string>('');
const isMaterial = ref<boolean>(false);
const validationError = ref<string | null>(null);

// Reset form when result changes or modal opens
watch(
  () => [props.result, props.isOpen] as [AcademicResult | null, boolean],
  ([newResult, isOpen]) => {
    if (newResult && isOpen) {
      scoreNumeric.value = newResult.scoreNumeric ?? null;
      scoreMax.value = newResult.scoreMax ?? 100;
      gradeLetter.value = newResult.gradeLetter ?? '';
      gradeLabel.value = newResult.gradeLabel ?? '';
      narrativeFeedback.value = newResult.narrativeFeedback ?? '';
      reason.value = '';
      evidence.value = '';
      isMaterial.value = false;
      validationError.value = null;
    }
  },
  { immediate: true },
);

// Query past correction history
const resultId = computed(() => props.result?.id ?? null);
const {
  data: historyList,
  isLoading: isLoadingHistory,
} = useCorrectionHistoryQuery(resultId, {
  enabled: computed(() => props.isOpen && Boolean(resultId.value)),
});

// Mutation hook
const correctMutation = useCorrectResultMutation();

const isReasonValid = computed(() => reason.value.trim().length >= 10);

const hasChangedValues = computed(() => {
  if (!props.result) return false;
  const original = props.result;
  const scoreChanged = (scoreNumeric.value ?? null) !== (original.scoreNumeric ?? null);
  const letterChanged = (gradeLetter.value.trim() || null) !== (original.gradeLetter || null);
  const labelChanged = (gradeLabel.value.trim() || null) !== (original.gradeLabel || null);
  const feedbackChanged =
    (narrativeFeedback.value.trim() || null) !== (original.narrativeFeedback || null);
  return scoreChanged || letterChanged || labelChanged || feedbackChanged;
});

const isSubmitDisabled = computed(() => {
  return (
    !isReasonValid.value ||
    !hasChangedValues.value ||
    correctMutation.isPending.value ||
    (isMaterial.value && !isAuthority.value)
  );
});

function handleClose() {
  emit('update:isOpen', false);
}

async function handleSubmit() {
  if (!props.result) return;
  validationError.value = null;

  if (!isReasonValid.value) {
    validationError.value = 'A valid justification reason of at least 10 characters is mandatory.';
    return;
  }

  if (!hasChangedValues.value) {
    validationError.value = 'Please provide at least one altered value (score, letter grade, label, or feedback).';
    return;
  }

  if (isMaterial.value && !isAuthority.value) {
    validationError.value = 'Only Academic Authorities are permitted to certify material grade alterations.';
    return;
  }

  if (scoreNumeric.value !== null && scoreNumeric.value < 0) {
    validationError.value = 'Numeric score cannot be negative.';
    return;
  }

  const payload: CorrectResultRequest = {
    score_numeric: scoreNumeric.value,
    score_max: scoreMax.value,
    grade_letter: gradeLetter.value.trim() || null,
    grade_label: gradeLabel.value.trim() || null,
    narrative_feedback: narrativeFeedback.value.trim() || null,
    reason: reason.value.trim(),
    evidence: evidence.value.trim() || null,
    is_material: isMaterial.value,
  };

  try {
    const updated = await correctMutation.mutateAsync({
      id: props.result.id,
      request: payload,
    });
    emit('corrected', updated);
    handleClose();
  } catch (err: unknown) {
    if (err instanceof Error) {
      validationError.value = err.message;
    } else {
      validationError.value = 'Failed to record auditable correction.';
    }
  }
}
</script>

<template>
  <Dialog
    :visible="isOpen"
    modal
    header="Auditable Grade Correction"
    :style="{ width: '680px' }"
    :breakpoints="{ '960px': '75vw', '640px': '95vw' }"
    data-testid="record-correction-modal"
    @update:visible="(val) => emit('update:isOpen', val)"
  >
    <div v-if="result" class="space-y-5 py-1">
      <!-- Target Result Header Card -->
      <div class="p-3.5 bg-surface-50 dark:bg-surface-800/60 rounded-lg border border-surface-200 dark:border-surface-700 space-y-2">
        <div class="flex items-center justify-between">
          <div>
            <span class="text-xs text-surface-500 block">Candidate / Student</span>
            <span class="font-semibold text-sm text-surface-900 dark:text-surface-100" data-testid="target-candidate-name">
              {{ result.candidateName || result.studentName || 'Student ID: ' + result.studentId }}
            </span>
          </div>
          <div class="text-right">
            <span class="text-xs text-surface-500 block">Attempt #</span>
            <span class="font-mono text-xs font-semibold px-2 py-0.5 rounded bg-surface-200 dark:bg-surface-700">
              Attempt {{ result.attemptNumber }}
            </span>
          </div>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-2 border-t border-surface-200 dark:border-surface-700 text-xs">
          <div>
            <span class="text-surface-500 block">Current Score:</span>
            <span class="font-bold text-surface-900 dark:text-surface-100" data-testid="original-score">
              {{ result.scoreNumeric !== null && result.scoreNumeric !== undefined ? result.scoreNumeric : '—' }}
              <span v-if="result.scoreMax" class="font-normal text-surface-500">/ {{ result.scoreMax }}</span>
            </span>
          </div>
          <div>
            <span class="text-surface-500 block">Letter Grade:</span>
            <span class="font-bold text-surface-900 dark:text-surface-100" data-testid="original-grade">
              {{ result.gradeLetter || '—' }}
            </span>
          </div>
          <div>
            <span class="text-surface-500 block">Status:</span>
            <span class="font-medium capitalize text-teal-600 dark:text-teal-400">
              {{ result.resultStatus }}
            </span>
          </div>
          <div>
            <span class="text-surface-500 block">Operative (Final):</span>
            <span :class="result.isFinal ? 'text-emerald-600 font-semibold' : 'text-surface-400'">
              {{ result.isFinal ? 'Yes (Final)' : 'No' }}
            </span>
          </div>
        </div>
      </div>

      <!-- Legal Audit Notice -->
      <div class="p-3 bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2.5 text-xs text-amber-800 dark:text-amber-200">
        <ShieldAlert class="w-4 h-4 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
        <div>
          <span class="font-semibold block">Statutory Audit Requirement (UC-ACD-19 / TD-06)</span>
          In-place modifications to approved academic results require an immutable audit trail attributing who made the alteration, when, why, and all previous values.
        </div>
      </div>

      <!-- Error alert -->
      <div
        v-if="validationError"
        class="p-3 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-lg flex items-start gap-2 text-xs text-rose-700 dark:text-rose-300"
        data-testid="correction-error-alert"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 mt-0.5" />
        <span>{{ validationError }}</span>
      </div>

      <!-- Correction Inputs -->
      <div class="space-y-4">
        <h4 class="text-xs font-semibold uppercase tracking-wider text-surface-600 dark:text-surface-400">
          Corrected Values
        </h4>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label for="corr-score" class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1">
              Corrected Numeric Score
            </label>
            <input
              id="corr-score"
              v-model.number="scoreNumeric"
              type="number"
              step="0.01"
              min="0"
              class="w-full px-3 py-2 text-sm rounded-lg border border-surface-300 dark:border-surface-600 bg-surface-0 dark:bg-surface-900 focus:outline-none focus:ring-2 focus:ring-teal-500"
              placeholder="e.g. 88.5"
              data-testid="input-score-numeric"
            />
          </div>

          <div>
            <label for="corr-max" class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1">
              Score Max
            </label>
            <input
              id="corr-max"
              v-model.number="scoreMax"
              type="number"
              step="0.01"
              min="1"
              class="w-full px-3 py-2 text-sm rounded-lg border border-surface-300 dark:border-surface-600 bg-surface-0 dark:bg-surface-900 focus:outline-none focus:ring-2 focus:ring-teal-500"
              placeholder="e.g. 100"
              data-testid="input-score-max"
            />
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label for="corr-letter" class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1">
              Corrected Letter Grade
            </label>
            <InputText
              id="corr-letter"
              v-model="gradeLetter"
              class="w-full text-sm"
              placeholder="e.g. A, B+, Sangat Baik"
              data-testid="input-grade-letter"
            />
          </div>

          <div>
            <label for="corr-label" class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1">
              Corrected Competency Label
            </label>
            <InputText
              id="corr-label"
              v-model="gradeLabel"
              class="w-full text-sm"
              placeholder="e.g. Tercapai dengan optimal"
              data-testid="input-grade-label"
            />
          </div>
        </div>

        <div>
          <label for="corr-feedback" class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1">
            Corrected Narrative Feedback
          </label>
          <Textarea
            id="corr-feedback"
            v-model="narrativeFeedback"
            rows="2"
            class="w-full text-sm"
            placeholder="Updated qualitative feedback or remarks..."
            data-testid="input-narrative-feedback"
          />
        </div>

        <!-- Mandatory Justification Reason -->
        <div>
          <div class="flex items-center justify-between mb-1">
            <label for="corr-reason" class="text-xs font-semibold text-surface-800 dark:text-surface-200">
              Mandatory Justification Reason <span class="text-rose-500">*</span>
            </label>
            <span
              class="text-[11px]"
              :class="isReasonValid ? 'text-teal-600 font-medium' : 'text-surface-400'"
            >
              {{ reason.trim().length }}/10 min chars
            </span>
          </div>
          <Textarea
            id="corr-reason"
            v-model="reason"
            rows="3"
            class="w-full text-sm"
            placeholder="Explain specifically why this grade is being corrected (e.g. 'Scoring tab arithmetic error found in final exam paper #3', min 10 characters)..."
            data-testid="input-correction-reason"
          />
        </div>

        <!-- Evidence Reference -->
        <div>
          <label for="corr-evidence" class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1">
            Supporting Evidence Reference / Document (Optional)
          </label>
          <InputText
            id="corr-evidence"
            v-model="evidence"
            class="w-full text-sm"
            placeholder="e.g. Ticket #402 / Regraded exam sheet signed by teacher"
            data-testid="input-correction-evidence"
          />
        </div>

        <!-- Materiality Checkbox -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/40 rounded-lg border border-surface-200 dark:border-surface-700 flex items-start gap-3">
          <Checkbox
            v-model="isMaterial"
            :binary="true"
            input-id="corr-material"
            data-testid="checkbox-is-material"
          />
          <div class="text-xs">
            <label for="corr-material" class="font-semibold text-surface-900 dark:text-surface-100 cursor-pointer block">
              Material Grade Alteration
            </label>
            <span class="text-surface-500 text-[11px] block mt-0.5">
              Check this if the correction alters official GPA, honors standing, or student report card qualification.
              <span v-if="!isAuthority" class="text-rose-500 block font-medium">
                (Requires Academic Authority authorization)
              </span>
            </span>
          </div>
        </div>
      </div>

      <!-- Past Correction History Accordion -->
      <div class="pt-2">
        <Accordion>
          <AccordionPanel value="0">
            <AccordionHeader>
              <div class="flex items-center gap-2 text-xs font-semibold text-surface-700 dark:text-surface-300">
                <History class="w-4 h-4 text-teal-600" />
                <span>Audit History ({{ historyList?.length ?? 0 }} corrections recorded)</span>
              </div>
            </AccordionHeader>
            <AccordionContent>
              <div v-if="isLoadingHistory" class="py-3 text-center text-xs text-surface-500">
                Loading correction history...
              </div>
              <div v-else-if="!historyList || historyList.length === 0" class="py-3 text-center text-xs text-surface-400">
                No past corrections recorded for this result.
              </div>
              <div v-else class="space-y-2.5 max-h-48 overflow-y-auto pr-1 text-xs" data-testid="correction-history-list">
                <div
                  v-for="item in historyList"
                  :key="item.id"
                  class="p-2.5 bg-surface-50 dark:bg-surface-800/50 rounded-lg border border-surface-200 dark:border-surface-700 space-y-1"
                >
                  <div class="flex items-center justify-between text-[11px] text-surface-500">
                    <span class="font-semibold text-surface-800 dark:text-surface-200">{{ item.correctedBy }}</span>
                    <span>{{ formatDisplayDate(item.correctedDate) }}</span>
                  </div>
                  <p class="font-medium text-surface-900 dark:text-surface-100">
                    Reason: {{ item.reason }}
                  </p>
                  <div class="flex items-center gap-2 text-[11px] text-surface-600 dark:text-surface-400">
                    <span v-if="item.isMaterial" class="px-1.5 py-0.2 rounded bg-rose-100 dark:bg-rose-950 text-rose-700 dark:text-rose-300 font-semibold">
                      Material
                    </span>
                    <span v-if="item.evidence" class="truncate">
                      Ref: {{ item.evidence }}
                    </span>
                  </div>
                </div>
              </div>
            </AccordionContent>
          </AccordionPanel>
        </Accordion>
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
          data-testid="btn-cancel-correction"
          @click="handleClose"
        />
        <Button
          label="Commit Auditable Correction"
          severity="primary"
          size="small"
          :disabled="isSubmitDisabled"
          :loading="correctMutation.isPending.value"
          class="cursor-pointer"
          data-testid="btn-submit-correction"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
