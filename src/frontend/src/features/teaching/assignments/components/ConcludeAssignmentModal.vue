<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  CheckCircle2,
  Award,
  Sparkles,
  ShieldCheck,
  FileCheck,
  Calendar,
  AlertTriangle,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  concludeAssignmentSchema,
  type ConcludeAssignmentFormValues,
} from '../api/schemas';
import { useConcludeAssignmentMutation } from '../composables/useAssignments';
import { UNRESOLVED_ITEMS_OPTIONS } from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingAssignmentDto,
  ConcludeTeachingAssignmentResponse,
} from '../types';

export interface ConcludeAssignmentModalProps {
  visible: boolean;
  assignment?: TeachingAssignmentDto | null;
}

const props = withDefaults(defineProps<ConcludeAssignmentModalProps>(), {
  visible: false,
  assignment: null,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'concluded', res: ConcludeTeachingAssignmentResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Form state
const completionDate = ref<string>('2027-06-25');
const academicConfirmationReceived = ref<boolean>(true);
const academicConfirmationSource = ref<string>('SIAKAD Final Grade Submission Protocol');
const handoverConfirmed = ref<boolean>(true);
const unresolvedItemsStatus = ref<'none' | 'minor_pending' | 'escalated_defect' | 'resolved' | 'escalated'>('none');
const unresolvedItemsDescription = ref<string>('');
const notes = ref<string>('');
const formErrors = ref<Record<string, string>>({});

// Watch modal open
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      formErrors.value = {};
      if (props.assignment?.effectiveEndDate) {
        completionDate.value = props.assignment.effectiveEndDate;
      }
    }
  },
  { immediate: true },
);

// Mutation
const concludeMutation = useConcludeAssignmentMutation();

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}

function handleSubmit() {
  if (!props.assignment) return;
  formErrors.value = {};

  const rawValues = {
    completionDate: completionDate.value,
    academicConfirmationReceived: academicConfirmationReceived.value,
    academicConfirmationSource: academicConfirmationSource.value.trim() || undefined,
    handoverConfirmed: handoverConfirmed.value,
    unresolvedItemsStatus: unresolvedItemsStatus.value,
    unresolvedItemsDescription: unresolvedItemsDescription.value.trim() || undefined,
    notes: notes.value.trim() || undefined,
  };

  const validationResult = concludeAssignmentSchema.safeParse(rawValues);

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const fieldName = issue.path[0] as string;
      if (!formErrors.value[fieldName]) {
        formErrors.value[fieldName] = issue.message;
      }
    }
    return;
  }

  concludeMutation.mutate(
    {
      id: props.assignment.id,
      payload: validationResult.data,
    },
    {
      onSuccess: (res) => {
        emit('concluded', res);
        handleClose();
      },
    },
  );
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    header="Conclude Teaching Assignment"
    class="w-full max-w-xl"
    data-testid="conclude-assignment-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-emerald-50 dark:bg-emerald-950 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-0">
            Conclude Assignment & Release Capacity
          </h2>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Verify academic deliverable completion and atomically release committed faculty hours
          </p>
        </div>
      </div>
    </template>

    <div v-if="assignment" class="space-y-4 pt-2">
      <!-- Assignment Details Card -->
      <div class="p-3.5 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 space-y-2 text-xs">
        <div class="flex items-center justify-between">
          <span class="font-semibold text-surface-900 dark:text-surface-100 font-mono">
            {{ assignment.assignmentNumber }}
          </span>
          <span class="px-2 py-0.5 rounded-md text-[11px] font-medium bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300">
            Current: {{ assignment.status }}
          </span>
        </div>

        <div class="grid grid-cols-2 gap-2 text-surface-600 dark:text-surface-300">
          <div>
            <span class="text-surface-400 block text-[10px] uppercase">Scope Label</span>
            <span class="font-medium text-surface-900 dark:text-surface-100">{{ assignment.scopeLabel }}</span>
          </div>
          <div>
            <span class="text-surface-400 block text-[10px] uppercase">Role</span>
            <span class="font-medium text-surface-900 dark:text-surface-100">{{ assignment.assignmentRoleName || 'Lead Teacher' }}</span>
          </div>
          <div>
            <span class="text-surface-400 block text-[10px] uppercase">Capacity to Release</span>
            <span class="font-bold text-emerald-600 dark:text-emerald-400 font-mono text-sm">
              {{ assignment.workloadCommitmentValue }} hours
            </span>
          </div>
          <div>
            <span class="text-surface-400 block text-[10px] uppercase">Scheduled End</span>
            <span class="font-medium">{{ assignment.effectiveEndDate }}</span>
          </div>
        </div>
      </div>

      <form class="space-y-4" @submit.prevent="handleSubmit">
        <!-- Completion Date -->
        <FormField
          label="Formal Completion Date"
          :required="true"
          :error="formErrors.completionDate"
        >
          <InputText
            v-model="completionDate"
            type="date"
            class="w-full"
            data-testid="input-completion-date"
          />
        </FormField>

        <!-- Academic Deliverable Verification Checkbox -->
        <div class="p-3 rounded-lg bg-emerald-50/50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-800 space-y-2.5">
          <div class="flex items-start gap-2.5">
            <Checkbox
              v-model="academicConfirmationReceived"
              :binary="true"
              input-id="academic-confirm-cb"
              data-testid="cb-academic-confirm"
            />
            <label for="academic-confirm-cb" class="text-xs cursor-pointer select-none text-emerald-950 dark:text-emerald-200">
              <span class="font-semibold block">Academic Deliverables Confirmed</span>
              <span class="text-emerald-800/80 dark:text-emerald-400 text-[11px]">
                Student grades submitted to Academics (SIAKAD), attendance finalized, and learning goals evaluated.
              </span>
            </label>
          </div>

          <FormField
            v-if="academicConfirmationReceived"
            label="Confirmation Protocol / Source"
            :error="formErrors.academicConfirmationSource"
          >
            <InputText
              v-model="academicConfirmationSource"
              placeholder="e.g. SIAKAD Grade Submission Protocol Ref #GK-2027-04"
              class="w-full text-xs"
              data-testid="input-academic-source"
            />
          </FormField>
        </div>

        <!-- Handover Sign-off Checkbox -->
        <div class="flex items-start gap-2.5 p-3 rounded-lg bg-surface-50 dark:bg-surface-800/60 border border-surface-200 dark:border-surface-700">
          <Checkbox
            v-model="handoverConfirmed"
            :binary="true"
            input-id="handover-confirm-cb"
            data-testid="cb-handover-confirm"
          />
          <label for="handover-confirm-cb" class="text-xs cursor-pointer select-none text-surface-800 dark:text-surface-200">
            <span class="font-medium block">Instructional Records & Handover Confirmed</span>
            <span class="text-surface-500 text-[11px]">
              Classroom materials, gradebooks, and physical/digital resources handed over cleanly.
            </span>
          </label>
        </div>

        <!-- Unresolved Items Status -->
        <FormField
          label="Unresolved Items Standing"
          :error="formErrors.unresolvedItemsStatus"
        >
          <Select
            v-model="unresolvedItemsStatus"
            :options="UNRESOLVED_ITEMS_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="input-unresolved-status"
          />
        </FormField>

        <!-- Unresolved Items Description -->
        <FormField
          v-if="unresolvedItemsStatus !== 'none'"
          label="Pending Items Description"
          :error="formErrors.unresolvedItemsDescription"
        >
          <InputText
            v-model="unresolvedItemsDescription"
            placeholder="Details of remedial grading, make-up exams, or items to be reconciled..."
            class="w-full text-xs"
            data-testid="input-unresolved-description"
          />
        </FormField>

        <!-- Completion Notes -->
        <FormField
          label="Completion Notes (Optional)"
          :error="formErrors.notes"
        >
          <Textarea
            v-model="notes"
            rows="2"
            placeholder="Final instructional remarks..."
            class="w-full text-xs"
            data-testid="input-conclusion-notes"
          />
        </FormField>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="concludeMutation.isPending.value"
          @click="handleClose"
          data-testid="btn-cancel-conclude"
        />
        <Button
          type="button"
          label="Conclude & Release Capacity"
          severity="success"
          :loading="concludeMutation.isPending.value"
          @click="handleSubmit"
          data-testid="btn-confirm-conclude"
        >
          <template #icon>
            <CheckCircle2 class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
