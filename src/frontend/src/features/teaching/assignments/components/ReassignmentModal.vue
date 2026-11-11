<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  RotateCcw,
  ShieldCheck,
  History,
  FileCheck2,
  Calendar,
  AlertCircle,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  reassignmentSchema,
  type ReassignmentFormValues,
} from '../api/schemas';
import {
  useProposeReassignmentMutation,
  useAssignments,
} from '../composables/useAssignments';
import {
  DEFAULT_FACULTY_CANDIDATES,
  PERIOD_PRESETS,
} from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingAssignmentDto,
  ProposePermanentReassignmentResponse,
} from '../types';

export interface ReassignmentModalProps {
  visible: boolean;
  preselectedAssignment?: TeachingAssignmentDto | null;
  academicPeriodId?: string;
}

const props = withDefaults(defineProps<ReassignmentModalProps>(), {
  visible: false,
  preselectedAssignment: null,
  academicPeriodId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'submitted', res: ProposePermanentReassignmentResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Queries for active assignments
const { data: assignmentsData } = useAssignments(
  computed(() => ({
    academicPeriodId: props.academicPeriodId || PERIOD_PRESETS[0].id,
    status: 'approved',
  })),
);

const assignmentOptions = computed(() => {
  const items: TeachingAssignmentDto[] = Array.isArray(assignmentsData.value)
    ? assignmentsData.value
    : (assignmentsData.value as { items?: TeachingAssignmentDto[] })?.items || [];

  if (props.preselectedAssignment && !items.some((a) => a.id === props.preselectedAssignment?.id)) {
    items.unshift(props.preselectedAssignment);
  }

  return items.map((a) => ({
    label: `${a.assignmentNumber} — ${a.scopeLabel} (${a.workloadCommitmentValue} hrs/wk)`,
    value: a.id,
    assignment: a,
  }));
});

const facultyOptions = computed(() =>
  DEFAULT_FACULTY_CANDIDATES.map((f) => ({
    label: `${f.fullName} (${f.teacherNumber}) — ${f.primaryCapability}`,
    value: f.id,
  })),
);

const REASSIGNMENT_TYPES = [
  { label: 'Permanent Transfer (Alih Tugas Pengampu)', value: 'permanent_transfer' },
  { label: 'Curriculum Restructure (Penataan Kurikulum)', value: 'curriculum_restructure' },
  { label: 'Faculty Departure / Resignation (Pengunduran Diri Guru)', value: 'faculty_departure' },
  { label: 'Administrative Adjustment (Penyesuaian Beban Kerja)', value: 'administrative_adjustment' },
];

// Form state
const originalAssignmentId = ref<string>('');
const newTeacherId = ref<string>(DEFAULT_FACULTY_CANDIDATES[2].id); // John Miller
const effectiveDate = ref<string>('2027-02-15');
const reason = ref<string>('');
const requiresHandover = ref<boolean>(true);
const reassignmentType = ref<string>('permanent_transfer');
const formErrors = ref<Record<string, string>>({});

// Watch modal open
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      formErrors.value = {};
      if (props.preselectedAssignment) {
        originalAssignmentId.value = props.preselectedAssignment.id;
      } else if (assignmentOptions.value.length > 0 && !originalAssignmentId.value) {
        originalAssignmentId.value = assignmentOptions.value[0].value;
      }
    }
  },
  { immediate: true },
);

// Mutation
const proposeMutation = useProposeReassignmentMutation();

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}

function handleSubmit() {
  formErrors.value = {};

  const rawValues = {
    originalAssignmentId: originalAssignmentId.value,
    newTeacherId: newTeacherId.value,
    effectiveDate: effectiveDate.value,
    reason: reason.value.trim(),
    requiresHandover: requiresHandover.value,
    reassignmentType: reassignmentType.value,
  };

  const validationResult = reassignmentSchema.safeParse(rawValues);

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const fieldName = issue.path[0] as string;
      if (!formErrors.value[fieldName]) {
        formErrors.value[fieldName] = issue.message;
      }
    }
    return;
  }

  proposeMutation.mutate(validationResult.data, {
    onSuccess: (res) => {
      emit('submitted', res);
      handleClose();
    },
  });
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    header="Propose Permanent Reassignment"
    class="w-full max-w-2xl"
    data-testid="reassignment-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-indigo-50 dark:bg-indigo-950 flex items-center justify-center text-indigo-600 dark:text-indigo-400">
          <RotateCcw class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-0">
            Permanent Teaching Reassignment
          </h2>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Transfer responsibility to a replacement faculty member with predecessor link and handover tracking
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-2">
      <!-- Historical Preservation Governance Notice -->
      <div class="p-3.5 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 text-xs text-surface-700 dark:text-surface-300 space-y-1">
        <div class="flex items-center gap-2 font-semibold text-surface-900 dark:text-surface-100">
          <History class="w-4 h-4 text-indigo-600 shrink-0" />
          <span>Historical Integrity & Predecessor Linkage:</span>
        </div>
        <p class="leading-relaxed">
          Permanent transfers take effect prospectively from the effective date. Past academic records, attendance logs, and student evaluations prior to this date remain strictly preserved and attributed to the predecessor teacher.
        </p>
      </div>

      <form class="space-y-4" @submit.prevent="handleSubmit">
        <!-- Target Assignment to Transfer -->
        <FormField
          label="Original Assignment to Transfer"
          :required="true"
          :error="formErrors.originalAssignmentId"
        >
          <Select
            v-model="originalAssignmentId"
            :options="assignmentOptions"
            option-label="label"
            option-value="value"
            placeholder="Select assignment to transfer..."
            class="w-full"
            data-testid="input-reassignment-assignment"
          />
        </FormField>

        <!-- Replacement Faculty Candidate -->
        <FormField
          label="Replacement Teacher"
          :required="true"
          :error="formErrors.newTeacherId"
          description="Select incoming faculty member assuming responsibility."
        >
          <Select
            v-model="newTeacherId"
            :options="facultyOptions"
            option-label="label"
            option-value="value"
            placeholder="Select replacement teacher..."
            class="w-full"
            data-testid="input-reassignment-teacher"
          />
        </FormField>

        <!-- Reassignment Type & Effective Date -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            label="Reassignment Category"
            :required="true"
            :error="formErrors.reassignmentType"
          >
            <Select
              v-model="reassignmentType"
              :options="REASSIGNMENT_TYPES"
              option-label="label"
              option-value="value"
              class="w-full"
              data-testid="input-reassignment-type"
            />
          </FormField>

          <FormField
            label="Prospective Effective Date"
            :required="true"
            :error="formErrors.effectiveDate"
          >
            <InputText
              v-model="effectiveDate"
              type="date"
              class="w-full"
              data-testid="input-reassignment-effective-date"
            />
          </FormField>
        </div>

        <!-- Handover Mandate Checkbox -->
        <div class="flex items-start gap-2.5 p-3 rounded-lg bg-surface-50 dark:bg-surface-800/60 border border-surface-200 dark:border-surface-700">
          <Checkbox
            v-model="requiresHandover"
            :binary="true"
            input-id="requires-handover-cb"
            data-testid="cb-requires-handover"
          />
          <label for="requires-handover-cb" class="text-xs cursor-pointer select-none text-surface-800 dark:text-surface-200">
            <span class="font-medium block">Mandate Syllabus & Grading Handover</span>
            <span class="text-surface-500 text-[11px]">
              Flag reassignment as requiring formal instructional handover verification (curriculum, assessments, grades) before conclusion.
            </span>
          </label>
        </div>

        <!-- Reason -->
        <FormField
          label="Transfer Justification & Reason"
          :required="true"
          :error="formErrors.reason"
        >
          <Textarea
            v-model="reason"
            rows="3"
            placeholder="Formal justification for reassignment submitted to the Teaching Authority..."
            class="w-full text-xs"
            data-testid="input-reassignment-reason"
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
          :disabled="proposeMutation.isPending.value"
          @click="handleClose"
          data-testid="btn-cancel-reassignment"
        />
        <Button
          type="button"
          label="Submit Reassignment Proposal"
          severity="primary"
          :loading="proposeMutation.isPending.value"
          @click="handleSubmit"
          data-testid="btn-submit-reassignment"
        >
          <template #icon>
            <RotateCcw class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
