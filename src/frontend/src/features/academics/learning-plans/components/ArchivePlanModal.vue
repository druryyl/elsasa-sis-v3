<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import {
  Archive,
  AlertCircle,
  FileText,
  User,
  GraduationCap,
  Layers,
  Award,
  Info,
} from 'lucide-vue-next';
import { useArchivePlanMutation } from '../composables/useLearningPlans';
import type { StudentLearningPlan } from '../types';

const props = defineProps<{
  visible: boolean;
  plan: StudentLearningPlan | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'archived'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const statusOptions = [
  {
    label: 'Archive Plan (Retire to Archived)',
    value: 'archived' as const,
    description: 'Retires the plan from active operations.',
  },
  {
    label: 'Complete Plan (Mark Completed)',
    value: 'completed' as const,
    description: 'Marks individualized academic goals as completed.',
  },
];

const selectedTargetStatus = ref<'archived' | 'completed'>('archived');
const notes = ref<string>('');
const validationError = ref<string | null>(null);

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      selectedTargetStatus.value = 'archived';
      notes.value = '';
      validationError.value = null;
    }
  },
);

const { mutateAsync: archivePlan, isPending: isArchiving } = useArchivePlanMutation();

function handleClose() {
  if (isArchiving.value) return;
  isDialogVisible.value = false;
}

async function handleArchive() {
  if (!props.plan) return;

  validationError.value = null;

  try {
    await archivePlan({
      id: props.plan.id,
      request: {
        status: selectedTargetStatus.value,
      },
    });

    emit('archived');
    handleClose();
  } catch (err: unknown) {
    const error = err as { response?: { data?: { detail?: string; title?: string } } };
    validationError.value =
      error.response?.data?.detail ||
      error.response?.data?.title ||
      'Failed to archive learning plan.';
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg mx-4"
    :header="'Archive Learning Plan'"
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
          <div class="w-10 h-10 rounded-xl bg-slate-700 text-white flex items-center justify-center font-bold text-sm shadow-xs flex-shrink-0">
            <Archive class="w-5 h-5" />
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

        <span class="px-2.5 py-1 rounded-full text-xs font-bold uppercase bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
          {{ plan.status }} v{{ plan.version }}
        </span>
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

    <!-- Target Status Selector -->
    <div class="space-y-1.5">
      <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
        Archival Lifecycle Transition <span class="text-rose-500">*</span>
      </label>
      <Select
        v-model="selectedTargetStatus"
        :options="statusOptions"
        option-label="label"
        option-value="value"
        class="w-full text-xs"
        :disabled="isArchiving"
      />
      <p class="text-[11px] text-slate-400">
        {{ selectedTargetStatus === 'completed' ? 'Marking as completed indicates all individualized academic targets were accomplished.' : 'Archiving removes the plan from active classroom operations and locks modifications.' }}
      </p>
    </div>

    <template #footer>
      <Button
        label="Cancel"
        severity="secondary"
        text
        size="small"
        class="text-xs"
        :disabled="isArchiving"
        @click="handleClose"
      />
      <Button
        label="Confirm Archival"
        icon="pi pi-box"
        severity="danger"
        size="small"
        class="text-xs font-semibold"
        :loading="isArchiving"
        :disabled="isArchiving"
        @click="handleArchive"
      />
    </template>
  </Dialog>
</template>
