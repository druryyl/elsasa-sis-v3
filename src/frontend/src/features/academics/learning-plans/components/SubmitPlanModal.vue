<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import {
  Send,
  AlertCircle,
  FileText,
  User,
  GraduationCap,
  Layers,
  Award,
  CheckCircle2,
} from 'lucide-vue-next';
import { LoadingSpinner } from '@/shared/components';
import { useSubmitPlanMutation } from '../composables/useLearningPlans';
import type { StudentLearningPlan } from '../types';

const props = defineProps<{
  visible: boolean;
  plan: StudentLearningPlan | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'submitted'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const notes = ref<string>('');
const validationError = ref<string | null>(null);

const unitsCount = computed(() => props.plan?.constituentUnits?.length ?? 0);
const adjustmentsCount = computed(() => props.plan?.adjustments?.length ?? 0);
const hasConstituents = computed(() => unitsCount.value > 0 || adjustmentsCount.value > 0);

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      notes.value = '';
      validationError.value = null;
    }
  },
);

const { mutateAsync: submitPlan, isPending: isSubmitting } = useSubmitPlanMutation();

function handleClose() {
  if (isSubmitting.value) return;
  isDialogVisible.value = false;
}

async function handleSubmit() {
  if (!props.plan) return;

  if (!hasConstituents.value) {
    validationError.value =
      'A student learning plan must contain at least one constituent unit or academic adjustment before submission for review.';
    return;
  }

  if (notes.value.length > 4000) {
    validationError.value = 'Notes cannot exceed 4000 characters.';
    return;
  }

  validationError.value = null;

  try {
    await submitPlan({
      id: props.plan.id,
      notes: notes.value.trim() ? notes.value.trim() : undefined,
    });
    emit('submitted');
    handleClose();
  } catch (err: unknown) {
    const error = err as { response?: { data?: { detail?: string; title?: string } } };
    validationError.value =
      error.response?.data?.detail ||
      error.response?.data?.title ||
      'Failed to submit learning plan for review.';
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg mx-4"
    :header="'Submit Plan for Review'"
    :pt="{
      header: { class: 'p-4 md:p-6 border-b border-slate-100 dark:border-slate-800' },
      content: { class: 'p-4 md:p-6 space-y-5' },
      footer: { class: 'p-4 md:p-6 border-t border-slate-100 dark:border-slate-800 flex justify-end gap-2' },
    }"
  >
    <!-- Plan Summary Card -->
    <div
      v-if="plan"
      class="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-900/60 space-y-3"
    >
      <div class="flex items-center justify-between gap-3">
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-10 h-10 rounded-xl bg-indigo-600 text-white flex items-center justify-center font-bold text-sm shadow-xs flex-shrink-0">
            {{ (plan.studentName || 'S').charAt(0).toUpperCase() }}
          </div>
          <div class="min-w-0">
            <h4 class="font-bold text-sm text-slate-900 dark:text-slate-100 truncate">
              {{ plan.studentName || 'Student Name' }}
            </h4>
            <div class="text-xs text-slate-500 dark:text-slate-400">
              NIS: {{ plan.studentNis || '—' }} • {{ plan.academicPeriodLabel }}
            </div>
          </div>
        </div>

        <span class="px-2.5 py-1 rounded-full text-xs font-bold bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300">
          Draft v{{ plan.version }}
        </span>
      </div>

      <!-- Counts Summary -->
      <div class="grid grid-cols-2 gap-2 pt-2 border-t border-slate-200/60 dark:border-slate-800/60 text-xs">
        <div class="flex items-center gap-2 p-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80">
          <Layers class="w-4 h-4 text-indigo-500" />
          <span class="font-semibold text-slate-700 dark:text-slate-200">{{ unitsCount }} Constituent Units</span>
        </div>
        <div class="flex items-center gap-2 p-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80">
          <Award class="w-4 h-4 text-purple-500" />
          <span class="font-semibold text-slate-700 dark:text-slate-200">{{ adjustmentsCount }} Adjustments</span>
        </div>
      </div>
    </div>

    <!-- Empty Constituents Error Banner -->
    <div
      v-if="!hasConstituents"
      class="p-3.5 rounded-lg bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 flex items-start gap-2.5 text-xs text-amber-800 dark:text-amber-200"
    >
      <AlertCircle class="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
      <div>
        <p class="font-semibold">Cannot submit an empty plan</p>
        <p class="text-[11px] mt-0.5 opacity-90">
          At least one constituent learning unit or academic adjustment must be established before requesting formal review.
        </p>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="validationError"
      class="p-3.5 rounded-lg bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 flex items-start gap-2.5 text-xs text-rose-800 dark:text-rose-200"
    >
      <AlertCircle class="w-4 h-4 text-rose-600 flex-shrink-0 mt-0.5" />
      <span class="flex-1">{{ validationError }}</span>
    </div>

    <!-- Submission Notes -->
    <div class="space-y-1.5">
      <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
        Submission Notes <span class="text-slate-400 font-normal">(Optional)</span>
      </label>
      <Textarea
        v-model="notes"
        rows="3"
        class="w-full text-xs"
        placeholder="Add rationale, exceptional learning considerations, or instructions for the Academic Authority..."
        :disabled="isSubmitting"
      />
      <p class="text-[11px] text-slate-400">
        These notes will be logged in the plan audit trail and displayed to the approving authority.
      </p>
    </div>

    <template #footer>
      <Button
        label="Cancel"
        severity="secondary"
        text
        size="small"
        class="text-xs"
        :disabled="isSubmitting"
        @click="handleClose"
      />
      <Button
        label="Submit for Review"
        icon="pi pi-send"
        severity="primary"
        size="small"
        class="text-xs font-semibold"
        :loading="isSubmitting"
        :disabled="!hasConstituents || isSubmitting"
        @click="handleSubmit"
      />
    </template>
  </Dialog>
</template>
