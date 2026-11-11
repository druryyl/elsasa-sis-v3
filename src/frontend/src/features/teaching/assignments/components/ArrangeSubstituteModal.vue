<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  ArrowLeftRight,
  ShieldAlert,
  ShieldCheck,
  Calendar,
  Clock,
  Info,
  AlertTriangle,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  arrangeSubstituteSchema,
  type ArrangeSubstituteFormValues,
} from '../api/schemas';
import {
  useArrangeSubstituteMutation,
  useAssignments,
} from '../composables/useAssignments';
import {
  DEFAULT_FACULTY_CANDIDATES,
  PERIOD_PRESETS,
} from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingAssignmentDto,
  ArrangeEmergencySubstitutionResponse,
} from '../types';

export interface ArrangeSubstituteModalProps {
  visible: boolean;
  preselectedAssignment?: TeachingAssignmentDto | null;
  academicPeriodId?: string;
}

const props = withDefaults(defineProps<ArrangeSubstituteModalProps>(), {
  visible: false,
  preselectedAssignment: null,
  academicPeriodId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'submitted', res: ArrangeEmergencySubstitutionResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Queries for assignments to cover
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

const substituteOptions = computed(() =>
  DEFAULT_FACULTY_CANDIDATES.map((f) => ({
    label: `${f.fullName} (${f.teacherNumber}) — ${f.primaryCapability} [${f.campusAffiliation}]`,
    value: f.id,
  })),
);

const REASON_OPTIONS = [
  { label: 'Medical Leave (Cuti Sakit)', value: 'medical_leave' },
  { label: 'Personal Emergency (Urusan Keluarga Mendesak)', value: 'personal_emergency' },
  { label: 'Official Training (Pelatihan Kedinasan / Diklat)', value: 'official_training' },
  { label: 'Other Operational Coverage', value: 'other' },
];

const SCOPE_TYPE_OPTIONS = [
  { label: 'Full Scope Coverage (All Hours & Duties)', value: 'full' },
  { label: 'Partial Scope Coverage (Specific Modules / Hours)', value: 'partial' },
];

// Form state
const originalAssignmentId = ref<string>('');
const substituteTeacherId = ref<string>(DEFAULT_FACULTY_CANDIDATES[1].id); // Siti Nurhaliza
const startDate = ref<string>('2027-02-01');
const endDate = ref<string>('2027-02-14');
const coverageScopeType = ref<'full' | 'partial'>('full');
const coveredHoursPerWeek = ref<number>(5);
const workloadImpactHours = ref<number>(5);
const reason = ref<string>('medical_leave');
const partialScopeDetails = ref<string>('');
const handoverNotes = ref<string>('');
const formErrors = ref<Record<string, string>>({});

// Watch assignment selection
watch(originalAssignmentId, (asnId) => {
  const selected = assignmentOptions.value.find((opt) => opt.value === asnId)?.assignment;
  if (selected) {
    coveredHoursPerWeek.value = selected.workloadCommitmentValue;
    workloadImpactHours.value = selected.workloadCommitmentValue;
  }
});

// Watch modal open
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      formErrors.value = {};
      if (props.preselectedAssignment) {
        originalAssignmentId.value = props.preselectedAssignment.id;
        coveredHoursPerWeek.value = props.preselectedAssignment.workloadCommitmentValue;
        workloadImpactHours.value = props.preselectedAssignment.workloadCommitmentValue;
      } else if (assignmentOptions.value.length > 0 && !originalAssignmentId.value) {
        originalAssignmentId.value = assignmentOptions.value[0].value;
      }
    }
  },
  { immediate: true },
);

// Mutation
const arrangeMutation = useArrangeSubstituteMutation();

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}

function handleSubmit() {
  formErrors.value = {};

  const rawValues = {
    originalAssignmentId: originalAssignmentId.value,
    substituteTeacherId: substituteTeacherId.value,
    startDate: startDate.value,
    endDate: endDate.value,
    coverageScopeType: coverageScopeType.value,
    coveredHoursPerWeek: coveredHoursPerWeek.value,
    workloadImpactHours: workloadImpactHours.value,
    reason: reason.value,
    partialScopeDetails: partialScopeDetails.value.trim() || undefined,
    handoverNotes: handoverNotes.value.trim() || undefined,
  };

  const validationResult = arrangeSubstituteSchema.safeParse(rawValues);

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const fieldName = issue.path[0] as string;
      if (!formErrors.value[fieldName]) {
        formErrors.value[fieldName] = issue.message;
      }
    }
    return;
  }

  arrangeMutation.mutate(validationResult.data, {
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
    header="Arrange Emergency Substitution"
    class="w-full max-w-2xl"
    data-testid="arrange-substitute-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-amber-50 dark:bg-amber-950 flex items-center justify-center text-amber-600 dark:text-amber-400">
          <ArrowLeftRight class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-0">
            Arrange Emergency Substitution
          </h2>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Deploy temporary faculty coverage under Resolution 3.8 Pre-Authorized Pool Delegation
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-2">
      <!-- Resolution 3.8 Governance Callout -->
      <div class="p-3.5 rounded-lg bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800 text-xs text-sky-900 dark:text-sky-300 space-y-1">
        <div class="flex items-center gap-2 font-semibold text-sky-950 dark:text-sky-200">
          <Info class="w-4 h-4 text-sky-600" />
          <span>Resolution 3.8 Pre-Authorized Pool Delegation:</span>
        </div>
        <p class="leading-relaxed">
          The Teaching Coordinator is unilaterally empowered to commit substitutes who hold active matching authorizations and have available workload capacity.
          If the candidate lacks authorization or would exceed policy capacity ceilings, the arrangement is saved as <strong>proposed</strong> and an exception alert is escalated to the School Principal.
        </p>
      </div>

      <form class="space-y-4" @submit.prevent="handleSubmit">
        <!-- Target Assignment to Cover -->
        <FormField
          label="Original Assignment to Cover"
          :required="true"
          :error="formErrors.originalAssignmentId"
        >
          <Select
            v-model="originalAssignmentId"
            :options="assignmentOptions"
            option-label="label"
            option-value="value"
            placeholder="Select assignment needing coverage..."
            class="w-full"
            data-testid="input-substitute-assignment"
          />
        </FormField>

        <!-- Substitute Teacher Candidate -->
        <FormField
          label="Substitute Faculty Candidate"
          :required="true"
          :error="formErrors.substituteTeacherId"
          description="Teachers in the pre-authorized pool commit immediately without delaying classes."
        >
          <Select
            v-model="substituteTeacherId"
            :options="substituteOptions"
            option-label="label"
            option-value="value"
            placeholder="Select substitute teacher..."
            class="w-full"
            data-testid="input-substitute-teacher"
          />
        </FormField>

        <!-- Coverage Date Range -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            label="Coverage Start Date"
            :required="true"
            :error="formErrors.startDate"
          >
            <InputText
              v-model="startDate"
              type="date"
              class="w-full"
              data-testid="input-substitute-start"
            />
          </FormField>

          <FormField
            label="Coverage End Date"
            :required="true"
            :error="formErrors.endDate"
          >
            <InputText
              v-model="endDate"
              type="date"
              class="w-full"
              data-testid="input-substitute-end"
            />
          </FormField>
        </div>

        <!-- Scope & Hours Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            label="Coverage Scope"
            :required="true"
            :error="formErrors.coverageScopeType"
          >
            <Select
              v-model="coverageScopeType"
              :options="SCOPE_TYPE_OPTIONS"
              option-label="label"
              option-value="value"
              class="w-full"
              data-testid="input-coverage-scope-type"
            />
          </FormField>

          <FormField
            label="Workload Hours Covered"
            :required="true"
            :error="formErrors.coveredHoursPerWeek"
          >
            <InputNumber
              v-model="coveredHoursPerWeek"
              :min="0"
              :max="100"
              suffix=" hrs/wk"
              class="w-full"
              data-testid="input-covered-hours"
            />
          </FormField>
        </div>

        <!-- Absence Reason -->
        <FormField
          label="Absence Justification / Reason"
          :required="true"
          :error="formErrors.reason"
        >
          <Select
            v-model="reason"
            :options="REASON_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="input-substitute-reason"
          />
        </FormField>

        <!-- Partial Scope Details (if partial) -->
        <FormField
          v-if="coverageScopeType === 'partial'"
          label="Partial Scope Details"
          :error="formErrors.partialScopeDetails"
        >
          <InputText
            v-model="partialScopeDetails"
            placeholder="e.g. Wednesday and Friday morning sessions only"
            class="w-full text-xs"
            data-testid="input-partial-details"
          />
        </FormField>

        <!-- Handover Notes -->
        <FormField
          label="Classroom Handover Notes (Optional)"
          :error="formErrors.handoverNotes"
        >
          <Textarea
            v-model="handoverNotes"
            rows="2"
            placeholder="Instructional plan notes, lesson topics, or syllabus state..."
            class="w-full text-xs"
            data-testid="input-handover-notes"
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
          :disabled="arrangeMutation.isPending.value"
          @click="handleClose"
          data-testid="btn-cancel-substitute"
        />
        <Button
          type="button"
          label="Commit Substitution"
          severity="primary"
          :loading="arrangeMutation.isPending.value"
          @click="handleSubmit"
          data-testid="btn-submit-substitute"
        >
          <template #icon>
            <ArrowLeftRight class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
