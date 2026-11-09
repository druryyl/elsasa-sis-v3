<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  ShieldCheck,
  AlertCircle,
  FileText,
  User,
  GraduationCap,
  Layers,
  Award,
  CheckCircle2,
  Info,
} from 'lucide-vue-next';
import { useApprovePlanMutation } from '../composables/useLearningPlans';
import { approvePlanSchema } from '../schemas/learningPlan.schema';
import type { ApprovalType, SlpApproval, StudentLearningPlan } from '../types';

const props = defineProps<{
  visible: boolean;
  plan: StudentLearningPlan | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'approved', approval: SlpApproval): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const approvalTypeOptions = [
  {
    label: 'Initial Plan Approval (v1 Activation)',
    value: 'initial_approval' as ApprovalType,
    description: 'Activates initial draft plan for the student and period.',
  },
  {
    label: 'Revision Approval (v+1 Hand-off)',
    value: 'revision_approval' as ApprovalType,
    description: 'Activates revision and transitions previous active plan to revised.',
  },
];

const selectedApprovalType = ref<ApprovalType>('initial_approval');
const notes = ref<string>('');
const validationError = ref<string | null>(null);

watch(
  () => [props.visible, props.plan],
  ([visible]) => {
    if (visible && props.plan) {
      selectedApprovalType.value =
        props.plan.version > 1 ? 'revision_approval' : 'initial_approval';
      notes.value = '';
      validationError.value = null;
    }
  },
  { immediate: true },
);

const { mutateAsync: approvePlan, isPending: isApproving } = useApprovePlanMutation();

function handleClose() {
  if (isApproving.value) return;
  isDialogVisible.value = false;
}

async function handleApprove() {
  if (!props.plan) return;

  const parseResult = approvePlanSchema.safeParse({
    approvalType: selectedApprovalType.value,
    notes: notes.value.trim() ? notes.value.trim() : null,
  });

  if (!parseResult.success) {
    validationError.value = parseResult.error.errors[0]?.message ?? 'Invalid approval data.';
    return;
  }

  validationError.value = null;

  try {
    const result = await approvePlan({
      id: props.plan.id,
      request: {
        approvalType: selectedApprovalType.value,
        notes: notes.value.trim() ? notes.value.trim() : null,
      },
    });

    emit('approved', result as SlpApproval);
    handleClose();
  } catch (err: unknown) {
    const error = err as { response?: { data?: { detail?: string; title?: string } } };
    validationError.value =
      error.response?.data?.detail ||
      error.response?.data?.title ||
      'Failed to approve and activate learning plan.';
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg mx-4"
    :header="'Approve Student Learning Plan'"
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
          <div class="w-10 h-10 rounded-xl bg-emerald-600 text-white flex items-center justify-center font-bold text-sm shadow-xs flex-shrink-0">
            <ShieldCheck class="w-5 h-5" />
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

        <span class="px-2.5 py-1 rounded-full text-xs font-bold bg-indigo-100 dark:bg-indigo-950/60 text-indigo-800 dark:text-indigo-300">
          v{{ plan.version }} Draft
        </span>
      </div>

      <div class="grid grid-cols-2 gap-2 pt-2 border-t border-slate-200/60 dark:border-slate-800/60 text-xs">
        <div class="flex items-center gap-2 p-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80">
          <Layers class="w-4 h-4 text-indigo-500" />
          <span class="font-semibold text-slate-700 dark:text-slate-200">{{ plan.constituentUnits?.length || 0 }} Units</span>
        </div>
        <div class="flex items-center gap-2 p-2 rounded-lg bg-white dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80">
          <Award class="w-4 h-4 text-purple-500" />
          <span class="font-semibold text-slate-700 dark:text-slate-200">{{ plan.adjustments?.length || 0 }} Adjustments</span>
        </div>
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

    <!-- Approval Type Selector -->
    <div class="space-y-1.5">
      <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
        Approval Decision Type <span class="text-rose-500">*</span>
      </label>
      <Select
        v-model="selectedApprovalType"
        :options="approvalTypeOptions"
        option-label="label"
        option-value="value"
        class="w-full text-xs"
        :disabled="isApproving"
      />
      <div class="p-3 rounded-lg bg-blue-50 dark:bg-blue-950/30 border border-blue-100 dark:border-blue-900/60 text-xs text-blue-800 dark:text-blue-300 flex items-start gap-2">
        <Info class="w-4 h-4 text-blue-500 flex-shrink-0 mt-0.5" />
        <span v-if="selectedApprovalType === 'revision_approval'">
          Approving this revision will activate plan <strong>v{{ plan?.version }}</strong> and atomically retire the previous active plan to <strong>revised</strong> status.
        </span>
        <span v-else>
          Approving will establish and activate this initial learning plan (<strong>v1</strong>) for active student participations.
        </span>
      </div>
    </div>

    <!-- Governance Audit Notes -->
    <div class="space-y-1.5">
      <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
        Governance / Approval Notes <span class="text-slate-400 font-normal">(Optional)</span>
      </label>
      <Textarea
        v-model="notes"
        rows="3"
        class="w-full text-xs"
        placeholder="Document the academic authority rationale, accommodations review, or institutional approval reference..."
        :disabled="isApproving"
      />
      <p class="text-[11px] text-slate-400">
        This note will be stamped into the immutable slp_approvals governance audit ledger.
      </p>
    </div>

    <template #footer>
      <Button
        label="Cancel"
        severity="secondary"
        text
        size="small"
        class="text-xs"
        :disabled="isApproving"
        @click="handleClose"
      />
      <Button
        label="Approve & Activate Plan"
        icon="pi pi-check-circle"
        severity="success"
        size="small"
        class="text-xs font-semibold"
        :loading="isApproving"
        :disabled="isApproving"
        @click="handleApprove"
      />
    </template>
  </Dialog>
</template>
