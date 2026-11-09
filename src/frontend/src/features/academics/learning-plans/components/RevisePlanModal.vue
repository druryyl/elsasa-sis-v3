<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import {
  GitFork,
  AlertCircle,
  FileText,
  User,
  GraduationCap,
  Layers,
  Award,
  Info,
} from 'lucide-vue-next';
import { useRevisePlanMutation } from '../composables/useLearningPlans';
import { revisePlanSchema } from '../schemas/learningPlan.schema';
import type { StudentLearningPlan } from '../types';

const props = defineProps<{
  visible: boolean;
  plan: StudentLearningPlan | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'revised', newPlanId: string): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const nextVersion = computed(() => (props.plan?.version ?? 1) + 1);
const notes = ref<string>('');
const validationError = ref<string | null>(null);

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      notes.value = '';
      validationError.value = null;
    }
  },
);

const { mutateAsync: revisePlan, isPending: isRevising } = useRevisePlanMutation();

function handleClose() {
  if (isRevising.value) return;
  isDialogVisible.value = false;
}

async function handleRevise() {
  if (!props.plan) return;

  const parseResult = revisePlanSchema.safeParse({
    notes: notes.value,
  });

  if (!parseResult.success) {
    validationError.value =
      parseResult.error.errors[0]?.message ?? 'Revision notes are required.';
    return;
  }

  validationError.value = null;

  try {
    const result = await revisePlan({
      id: props.plan.id,
      request: {
        notes: notes.value.trim(),
      },
    });

    emit('revised', result.id);
    handleClose();
  } catch (err: unknown) {
    const error = err as { response?: { data?: { detail?: string; title?: string } } };
    validationError.value =
      error.response?.data?.detail ||
      error.response?.data?.title ||
      'Failed to create plan revision.';
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg mx-4"
    :header="`Revise Learning Plan (v${nextVersion})`"
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
            <GitFork class="w-5 h-5" />
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

        <span class="px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300">
          Current: Active v{{ plan.version }}
        </span>
      </div>

      <div class="grid grid-cols-2 gap-2 pt-2 border-t border-slate-200/60 dark:border-slate-800/60 text-xs">
        <div class="flex items-center gap-2 p-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80">
          <Layers class="w-4 h-4 text-indigo-500" />
          <span class="font-semibold text-slate-700 dark:text-slate-200">{{ plan.constituentUnits?.length || 0 }} Units Cloned</span>
        </div>
        <div class="flex items-center gap-2 p-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80">
          <Award class="w-4 h-4 text-purple-500" />
          <span class="font-semibold text-slate-700 dark:text-slate-200">{{ plan.adjustments?.length || 0 }} Adjustments Cloned</span>
        </div>
      </div>
    </div>

    <!-- Clone-on-Revise Architecture Explanatory Banner -->
    <div class="p-3.5 rounded-lg bg-indigo-50 dark:bg-indigo-950/40 border border-indigo-100 dark:border-indigo-900/60 text-xs text-indigo-900 dark:text-indigo-200 flex items-start gap-2.5">
      <Info class="w-4 h-4 text-indigo-600 flex-shrink-0 mt-0.5" />
      <div class="space-y-1">
        <p class="font-semibold">Clone-on-Revise Lifecycle Protection</p>
        <p class="text-[11px] opacity-90 leading-relaxed">
          Formulating this revision will create a new draft plan (<strong>v{{ nextVersion }}</strong>) pre-populated with all current learning units and adjustments.
          The current active plan (<strong>v{{ plan?.version }}</strong>) remains active and operative in classrooms until this revision is formally approved.
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

    <!-- Revision Rationale Notes -->
    <div class="space-y-1.5">
      <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
        Revision Rationale <span class="text-rose-500">*</span>
      </label>
      <Textarea
        v-model="notes"
        rows="3"
        class="w-full text-xs"
        placeholder="Explain why this plan requires revision (e.g., educational evaluation, updated psychological assessments, course substitution change)..."
        :disabled="isRevising"
      />
      <p class="text-[11px] text-slate-400">
        Clear revision justification is required to guide the Academic Authority during re-approval.
      </p>
    </div>

    <template #footer>
      <Button
        label="Cancel"
        severity="secondary"
        text
        size="small"
        class="text-xs"
        :disabled="isRevising"
        @click="handleClose"
      />
      <Button
        :label="`Create Revision (v${nextVersion})`"
        icon="pi pi-copy"
        severity="primary"
        size="small"
        class="text-xs font-semibold"
        :loading="isRevising"
        :disabled="isRevising || !notes.trim()"
        @click="handleRevise"
      />
    </template>
  </Dialog>
</template>
