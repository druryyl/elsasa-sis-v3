<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  BookOpen,
  Calendar,
  Clock,
  Layers,
  GraduationCap,
  Users,
  FileText,
  ShieldCheck,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  proposeAssignmentSchema,
  type ProposeAssignmentFormValues,
} from '../api/schemas';
import {
  useProposeAssignmentMutation,
  useAssignmentRoles,
  useDemands,
} from '../composables/useAssignments';
import {
  PERIOD_PRESETS,
  DEFAULT_FACULTY_CANDIDATES,
  SCOPE_CATEGORY_OPTIONS,
} from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingDemandDto,
  ProposeTeachingAssignmentResponse,
} from '../types';

export interface ProposeAssignmentModalProps {
  visible: boolean;
  preselectedDemand?: TeachingDemandDto | null;
  preselectedTeacherId?: string | null;
  academicPeriodId?: string;
}

const props = withDefaults(defineProps<ProposeAssignmentModalProps>(), {
  visible: false,
  preselectedDemand: null,
  preselectedTeacherId: null,
  academicPeriodId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'submitted', res: ProposeTeachingAssignmentResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Queries for demands & roles
const { data: demandsData } = useDemands(
  computed(() => ({
    academicPeriodId: props.academicPeriodId || PERIOD_PRESETS[0].id,
  })),
);

const demandOptions = computed(() => {
  const items: TeachingDemandDto[] = Array.isArray(demandsData.value)
    ? demandsData.value
    : (demandsData.value as { items?: TeachingDemandDto[] })?.items || [];

  if (props.preselectedDemand && !items.some((d) => d.id === props.preselectedDemand?.id)) {
    items.unshift(props.preselectedDemand);
  }

  return items.map((d) => ({
    label: `${d.demandCode} — ${d.sourceWorkLabel} (${d.requiredCapabilityCode}, ${d.expectedWorkloadHours} hrs)`,
    value: d.id,
    demand: d,
  }));
});

const { data: rolesData } = useAssignmentRoles();

const assignmentRoleOptions = computed(() => {
  if (rolesData.value && rolesData.value.length > 0) {
    return rolesData.value.map((role) => ({
      label: `${role.name} (${role.code})${role.isLeadRole ? ' ⭐ Lead' : ''}`,
      value: role.id,
    }));
  }
  return [
    { label: 'Guru Utama (LEAD_TEACHER) ⭐ Lead', value: '00000000-0000-0000-0004-000000000001' },
    { label: 'Guru Pendamping (CO_TEACHER)', value: '00000000-0000-0000-0004-000000000002' },
    { label: 'Wali Kelas (HOMEROOM_ADVISOR)', value: '00000000-0000-0000-0004-000000000005' },
    { label: 'Guru Pengganti (SUBSTITUTE_TEACHER)', value: '00000000-0000-0000-0004-000000000008' },
  ];
});

const facultyOptions = computed(() =>
  DEFAULT_FACULTY_CANDIDATES.map((f) => ({
    label: `${f.fullName} (${f.teacherNumber}) — ${f.primaryCapability}`,
    value: f.id,
  })),
);

// Form state
const teachingDemandId = ref<string>('');
const teacherProfileId = ref<string>(DEFAULT_FACULTY_CANDIDATES[0].id);
const assignmentRoleId = ref<string>('00000000-0000-0000-0004-000000000001');
const assignmentScope = ref<string>('learning_unit');
const scopeReferenceId = ref<string>('00000000-0000-0000-0001-000000000001');
const scopeLabel = ref<string>('');
const academicPeriodId = ref<string>(PERIOD_PRESETS[0].id);
const effectiveStartDate = ref<string>(PERIOD_PRESETS[0].startDate);
const effectiveEndDate = ref<string>(PERIOD_PRESETS[0].endDate);
const workloadCommitmentValue = ref<number>(5);
const workloadUnit = ref<string>('Teaching Hours');
const notes = ref<string>('');
const formErrors = ref<Record<string, string>>({});

// Watch demand change to autofill scope and hours
watch(teachingDemandId, (newDemandId) => {
  const selected = demandOptions.value.find((opt) => opt.value === newDemandId)?.demand;
  if (selected) {
    scopeLabel.value = selected.sourceWorkLabel;
    scopeReferenceId.value = selected.sourceWorkReference;
    workloadCommitmentValue.value = selected.expectedWorkloadHours;
    effectiveStartDate.value = selected.requestedStartDate;
    effectiveEndDate.value = selected.requestedEndDate;
    if (selected.requiredAssignmentRoleId) {
      assignmentRoleId.value = selected.requiredAssignmentRoleId;
    }
  }
});

// Watch modal open
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      formErrors.value = {};
      if (props.preselectedDemand) {
        teachingDemandId.value = props.preselectedDemand.id;
        scopeLabel.value = props.preselectedDemand.sourceWorkLabel;
        scopeReferenceId.value = props.preselectedDemand.sourceWorkReference;
        workloadCommitmentValue.value = props.preselectedDemand.expectedWorkloadHours;
        effectiveStartDate.value = props.preselectedDemand.requestedStartDate;
        effectiveEndDate.value = props.preselectedDemand.requestedEndDate;
        if (props.preselectedDemand.requiredAssignmentRoleId) {
          assignmentRoleId.value = props.preselectedDemand.requiredAssignmentRoleId;
        }
      } else if (demandOptions.value.length > 0 && !teachingDemandId.value) {
        teachingDemandId.value = demandOptions.value[0].value;
      }

      if (props.preselectedTeacherId) {
        teacherProfileId.value = props.preselectedTeacherId;
      }
      if (props.academicPeriodId) {
        academicPeriodId.value = props.academicPeriodId;
      }
    }
  },
  { immediate: true },
);

// Mutation
const proposeMutation = useProposeAssignmentMutation();

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}

function handleSubmit() {
  formErrors.value = {};

  const rawValues = {
    teachingDemandId: teachingDemandId.value,
    teacherProfileId: teacherProfileId.value,
    assignmentRoleId: assignmentRoleId.value,
    assignmentScope: assignmentScope.value,
    scopeReferenceId: scopeReferenceId.value,
    scopeLabel: scopeLabel.value.trim(),
    academicPeriodId: academicPeriodId.value,
    effectiveStartDate: effectiveStartDate.value,
    effectiveEndDate: effectiveEndDate.value,
    workloadCommitmentValue: workloadCommitmentValue.value,
    workloadUnit: workloadUnit.value,
    notes: notes.value.trim() || undefined,
  };

  const validationResult = proposeAssignmentSchema.safeParse(rawValues);

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
    onSuccess: (response) => {
      emit('submitted', response);
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
    header="Propose Teaching Assignment"
    class="w-full max-w-2xl"
    data-testid="propose-assignment-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-primary-50 dark:bg-primary-950 flex items-center justify-center text-primary-600 dark:text-primary-400">
          <BookOpen class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-0">
            Propose Teaching Assignment
          </h2>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Staff an eligible faculty candidate against an accepted teaching demand
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-2" @submit.prevent="handleSubmit">
      <!-- Teaching Demand Selector -->
      <FormField
        label="Target Teaching Demand"
        :required="true"
        :error="formErrors.teachingDemandId"
        description="Select the teaching capacity request being staffed."
      >
        <Select
          v-model="teachingDemandId"
          :options="demandOptions"
          option-label="label"
          option-value="value"
          placeholder="Select an accepted teaching demand..."
          class="w-full"
          data-testid="input-target-demand"
        />
      </FormField>

      <!-- Faculty Candidate Selector -->
      <FormField
        label="Assigned Teacher"
        :required="true"
        :error="formErrors.teacherProfileId"
        description="Select the faculty member being assigned."
      >
        <Select
          v-model="teacherProfileId"
          :options="facultyOptions"
          option-label="label"
          option-value="value"
          placeholder="Select faculty candidate..."
          class="w-full"
          data-testid="input-target-teacher"
        />
      </FormField>

      <!-- Role & Scope Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <!-- Assignment Role -->
        <FormField
          label="Assignment Role"
          :required="true"
          :error="formErrors.assignmentRoleId"
        >
          <Select
            v-model="assignmentRoleId"
            :options="assignmentRoleOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="input-assignment-role"
          />
        </FormField>

        <!-- Scope Category -->
        <FormField
          label="Scope Category"
          :required="true"
          :error="formErrors.assignmentScope"
        >
          <Select
            v-model="assignmentScope"
            :options="SCOPE_CATEGORY_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="input-assignment-scope"
          />
        </FormField>
      </div>

      <!-- Scope Label & Reference -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          label="Scope Label / Course Name"
          :required="true"
          :error="formErrors.scopeLabel"
        >
          <InputText
            v-model="scopeLabel"
            placeholder="e.g. Matematika SD Kelas 4A / Wali Kelas 4A"
            class="w-full"
            data-testid="input-scope-label"
          />
        </FormField>

        <FormField
          label="Scope Reference UUID"
          :required="true"
          :error="formErrors.scopeReferenceId"
        >
          <InputText
            v-model="scopeReferenceId"
            placeholder="e.g. 00000000-0000-0000-0001-000000000001"
            class="w-full font-mono text-xs"
            data-testid="input-scope-reference"
          />
        </FormField>
      </div>

      <!-- Workload Commitment & Unit -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          label="Committed Hours / Week"
          :required="true"
          :error="formErrors.workloadCommitmentValue"
          description="Hours counted toward weekly faculty workload."
        >
          <InputNumber
            v-model="workloadCommitmentValue"
            :min="0"
            :max="100"
            :min-fraction-digits="1"
            :max-fraction-digits="2"
            suffix=" hrs/wk"
            class="w-full"
            data-testid="input-workload-hours"
          />
        </FormField>

        <FormField
          label="Workload Unit"
          :error="formErrors.workloadUnit"
        >
          <InputText
            v-model="workloadUnit"
            class="w-full"
            data-testid="input-workload-unit"
          />
        </FormField>
      </div>

      <!-- Effective Date Range -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          label="Effective Start Date"
          :required="true"
          :error="formErrors.effectiveStartDate"
        >
          <InputText
            v-model="effectiveStartDate"
            type="date"
            class="w-full"
            data-testid="input-effective-start"
          />
        </FormField>

        <FormField
          label="Effective End Date"
          :required="true"
          :error="formErrors.effectiveEndDate"
        >
          <InputText
            v-model="effectiveEndDate"
            type="date"
            class="w-full"
            data-testid="input-effective-end"
          />
        </FormField>
      </div>

      <!-- Notes -->
      <FormField
        label="Staffing Remarks (Optional)"
        :error="formErrors.notes"
      >
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Additional notes for instructional leadership review..."
          class="w-full"
          data-testid="input-propose-notes"
        />
      </FormField>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="proposeMutation.isPending.value"
          @click="handleClose"
          data-testid="btn-cancel-propose"
        />
        <Button
          type="button"
          label="Submit Proposed Assignment"
          severity="primary"
          :loading="proposeMutation.isPending.value"
          @click="handleSubmit"
          data-testid="btn-submit-propose"
        >
          <template #icon>
            <BookOpen class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
