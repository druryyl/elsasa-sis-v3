<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import {
  RefreshCw,
  AlertCircle,
  FileText,
  User,
  GraduationCap,
  Layers,
  CheckCircle2,
  Info,
} from 'lucide-vue-next';
import { useSyncParticipationsMutation } from '../composables/useLearningPlans';
import type { StudentLearningPlan, SyncParticipationsResponse } from '../types';

const props = defineProps<{
  visible: boolean;
  plan: StudentLearningPlan | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'synced', response: SyncParticipationsResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const validationError = ref<string | null>(null);
const syncResult = ref<SyncParticipationsResponse | null>(null);

const unitsCount = computed(() => props.plan?.constituentUnits?.length ?? 0);

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      validationError.value = null;
      syncResult.value = null;
    }
  },
);

const { mutateAsync: syncParticipations, isPending: isSyncing } =
  useSyncParticipationsMutation();

function handleClose() {
  if (isSyncing.value) return;
  isDialogVisible.value = false;
}

async function handleSync() {
  if (!props.plan) return;

  validationError.value = null;

  try {
    const result = await syncParticipations({ id: props.plan.id });
    syncResult.value = result;
    emit('synced', result);
  } catch (err: unknown) {
    const error = err as { response?: { data?: { detail?: string; title?: string } } };
    validationError.value =
      error.response?.data?.detail ||
      error.response?.data?.title ||
      'Failed to synchronize academic participations.';
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg mx-4"
    :header="'Synchronize Academic Participations'"
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
            <RefreshCw class="w-5 h-5" />
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
          Active v{{ plan.version }}
        </span>
      </div>

      <div class="p-2.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80 text-xs flex items-center justify-between">
        <span class="text-slate-600 dark:text-slate-400">Constituent Units to Sync:</span>
        <span class="font-bold text-indigo-600 dark:text-indigo-400">{{ unitsCount }} Units</span>
      </div>
    </div>

    <!-- Explanation Banner -->
    <div class="p-3.5 rounded-lg bg-indigo-50 dark:bg-indigo-950/40 border border-indigo-100 dark:border-indigo-900/60 text-xs text-indigo-900 dark:text-indigo-200 flex items-start gap-2.5">
      <Info class="w-4 h-4 text-indigo-600 flex-shrink-0 mt-0.5" />
      <div class="space-y-1">
        <p class="font-semibold">Downstream Participation Alignment (UC-ACD-10)</p>
        <p class="text-[11px] opacity-90 leading-relaxed">
          This operation merges all constituent units of this active plan into <strong>academics.academic_participations</strong> stamped with <code>source = 'learning_plan'</code> and this plan's ID. Existing linked participations will be reconciled idempotently.
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

    <!-- Success Feedback (When Done) -->
    <div
      v-if="syncResult"
      class="p-3.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-xs text-emerald-900 dark:text-emerald-200 flex items-start gap-2.5"
    >
      <CheckCircle2 class="w-4 h-4 text-emerald-600 flex-shrink-0 mt-0.5" />
      <div class="space-y-0.5">
        <p class="font-semibold">Synchronization Successful</p>
        <p class="text-[11px] opacity-90">
          Synced {{ syncResult.syncedCount }} participations ({{ syncResult.updatedCount }} reconciled, {{ syncResult.totalUnits }} total units).
        </p>
      </div>
    </div>

    <template #footer>
      <Button
        :label="syncResult ? 'Close' : 'Cancel'"
        severity="secondary"
        text
        size="small"
        class="text-xs"
        :disabled="isSyncing"
        @click="handleClose"
      />
      <Button
        v-if="!syncResult"
        label="Sync Participations"
        icon="pi pi-sync"
        severity="primary"
        size="small"
        class="text-xs font-semibold"
        :loading="isSyncing"
        :disabled="isSyncing || unitsCount === 0"
        @click="handleSync"
      />
    </template>
  </Dialog>
</template>
