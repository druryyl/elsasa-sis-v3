<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  FilePlus2,
  Calendar,
  Clock,
  Building2,
  GraduationCap,
  Users,
  ShieldCheck,
  FileText,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  createDemandSchema,
  type CreateDemandFormValues,
} from '../api/schemas';
import { useCreateDemandMutation, useAssignmentRoles } from '../composables/useAssignments';
import { PERIOD_PRESETS, CAMPUS_OPTIONS } from '../constants/assignmentWorkbenchConstants';
import type { CreateTeachingDemandResponse } from '../api/types';

export interface CreateDemandModalProps {
  visible: boolean;
  academicPeriodId?: string;
  campusScope?: string;
}

const props = withDefaults(defineProps<CreateDemandModalProps>(), {
  visible: false,
  academicPeriodId: undefined,
  campusScope: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'submitted', res: CreateTeachingDemandResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Domain sources
const SOURCE_DOMAINS = [
  { label: 'Academics (Course Learning Unit)', value: 'Academics' },
  { label: 'Students (Homeroom Cohort Advisory)', value: 'Students' },
  { label: 'Development (Extracurricular / Program)', value: 'Development' },
];

// Query assignment roles
const { data: rolesData } = useAssignmentRoles();

const assignmentRoleOptions = computed(() => {
  if (rolesData.value && rolesData.value.length > 0) {
    return rolesData.value.map((role) => ({
      label: `${role.name} (${role.code})`,
      value: role.id,
    }));
  }
  // Fallbacks if roles table is empty
  return [
    { label: 'Guru Utama (LEAD_TEACHER)', value: '00000000-0000-0000-0004-000000000001' },
    { label: 'Guru Pendamping (CO_TEACHER)', value: '00000000-0000-0000-0004-000000000002' },
    { label: 'Wali Kelas (HOMEROOM_ADVISOR)', value: '00000000-0000-0000-0004-000000000005' },
    { label: 'Guru Pengganti (SUBSTITUTE_TEACHER)', value: '00000000-0000-0000-0004-000000000008' },
  ];
});

// Form state
const sourceDomain = ref<string>('Academics');
const sourceWorkLabel = ref<string>('');
const sourceWorkReference = ref<string>('');
const academicPeriodId = ref<string>(PERIOD_PRESETS[0].id);
const requiredCapabilityCode = ref<string>('CAP-MATH-PRI');
const requiredAssignmentRoleId = ref<string>('00000000-0000-0000-0004-000000000001');
const expectedWorkloadHours = ref<number>(5);
const workloadUnit = ref<string>('Teaching Hours');
const requestedStartDate = ref<string>(PERIOD_PRESETS[0].startDate);
const requestedEndDate = ref<string>(PERIOD_PRESETS[0].endDate);
const campusScope = ref<string>('Kampus Utama');
const requiredTeachersCount = ref<number>(1);
const requiresAssessmentAuthority = ref<boolean>(true);
const demandCode = ref<string>('');
const clarificationNotes = ref<string>('');
const formErrors = ref<Record<string, string>>({});

// Reset and populate on modal open
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      formErrors.value = {};
      sourceDomain.value = 'Academics';
      sourceWorkLabel.value = '';
      sourceWorkReference.value = crypto.randomUUID();
      academicPeriodId.value = props.academicPeriodId || PERIOD_PRESETS[0].id;
      campusScope.value = props.campusScope && props.campusScope !== 'all' ? props.campusScope : 'Kampus Utama';
      requiredCapabilityCode.value = 'CAP-MATH-PRI';
      requiredAssignmentRoleId.value = assignmentRoleOptions.value[0]?.value || '00000000-0000-0000-0004-000000000001';
      expectedWorkloadHours.value = 5;
      workloadUnit.value = 'Teaching Hours';
      requestedStartDate.value = PERIOD_PRESETS[0].startDate;
      requestedEndDate.value = PERIOD_PRESETS[0].endDate;
      requiredTeachersCount.value = 1;
      requiresAssessmentAuthority.value = true;
      demandCode.value = '';
      clarificationNotes.value = '';
    }
  },
);

const createMutation = useCreateDemandMutation({
  onSuccess: (response) => {
    emit('submitted', response);
    isDialogVisible.value = false;
    emit('close');
  },
});

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}

async function handleSubmit() {
  formErrors.value = {};

  const payload: CreateDemandFormValues = {
    sourceDomain: sourceDomain.value,
    sourceWorkReference: sourceWorkReference.value || crypto.randomUUID(),
    sourceWorkLabel: sourceWorkLabel.value.trim(),
    academicPeriodId: academicPeriodId.value,
    requiredCapabilityCode: requiredCapabilityCode.value.trim(),
    requiredAssignmentRoleId: requiredAssignmentRoleId.value,
    expectedWorkloadHours: expectedWorkloadHours.value,
    requestedStartDate: requestedStartDate.value,
    requestedEndDate: requestedEndDate.value,
    workloadUnit: workloadUnit.value,
    campusScope: campusScope.value,
    requiredTeachersCount: requiredTeachersCount.value,
    requiresAssessmentAuthority: requiresAssessmentAuthority.value,
    demandCode: demandCode.value.trim() || undefined,
    clarificationNotes: clarificationNotes.value.trim() || undefined,
  };

  const validationResult = createDemandSchema.safeParse(payload);
  if (!validationResult.success) {
    validationResult.error.errors.forEach((err) => {
      const field = err.path.join('.');
      formErrors.value[field] = err.message;
    });
    return;
  }

  createMutation.mutate(payload);
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Create Teaching Demand Intake"
    class="w-full max-w-2xl"
    :dismissable-mask="true"
    data-testid="create-demand-modal"
  >
    <div class="space-y-4 pt-2">
      <!-- Info Header -->
      <div class="bg-primary-50/50 dark:bg-primary-950/30 border border-primary-200 dark:border-primary-800 rounded-lg p-3 text-xs text-primary-900 dark:text-primary-200 flex items-start gap-2.5">
        <FilePlus2 class="w-4 h-4 text-primary-600 dark:text-primary-400 shrink-0 mt-0.5" />
        <div>
          <p class="font-medium">Teaching Capacity Request Intake</p>
          <p class="text-primary-700 dark:text-primary-300">
            Formally ingest teaching resource requirements originating from Academics (learning units), Students (homeroom cohorts), or Development.
          </p>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Source Domain -->
        <FormField
          label="Requesting Domain"
          name="sourceDomain"
          :error="formErrors.sourceDomain"
          required
        >
          <Select
            v-model="sourceDomain"
            :options="SOURCE_DOMAINS"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="input-source-domain"
          />
        </FormField>

        <!-- Source Work Label -->
        <FormField
          label="Work / Unit Label"
          name="sourceWorkLabel"
          :error="formErrors.sourceWorkLabel"
          required
        >
          <InputText
            v-model="sourceWorkLabel"
            placeholder="e.g. Matematika SD Kelas 4A"
            class="w-full text-sm"
            data-testid="input-source-work-label"
          />
        </FormField>

        <!-- Academic Period -->
        <FormField
          label="Academic Period"
          name="academicPeriodId"
          :error="formErrors.academicPeriodId"
          required
        >
          <Select
            v-model="academicPeriodId"
            :options="PERIOD_PRESETS"
            option-label="label"
            option-value="id"
            class="w-full"
            data-testid="input-academic-period"
          />
        </FormField>

        <!-- Campus Scope -->
        <FormField
          label="Campus Scope"
          name="campusScope"
          :error="formErrors.campusScope"
          required
        >
          <Select
            v-model="campusScope"
            :options="CAMPUS_OPTIONS.filter((c) => c.value !== 'all')"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="input-campus-scope"
          />
        </FormField>

        <!-- Required Capability Code -->
        <FormField
          label="Required Capability Code"
          name="requiredCapabilityCode"
          :error="formErrors.requiredCapabilityCode"
          required
        >
          <InputText
            v-model="requiredCapabilityCode"
            placeholder="e.g. CAP-MATH-PRI"
            class="w-full text-sm"
            data-testid="input-capability-code"
          />
        </FormField>

        <!-- Required Assignment Role -->
        <FormField
          label="Staffing Role"
          name="requiredAssignmentRoleId"
          :error="formErrors.requiredAssignmentRoleId"
          required
        >
          <Select
            v-model="requiredAssignmentRoleId"
            :options="assignmentRoleOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="input-assignment-role"
          />
        </FormField>

        <!-- Expected Workload Hours -->
        <FormField
          label="Expected Workload (Hours/Week)"
          name="expectedWorkloadHours"
          :error="formErrors.expectedWorkloadHours"
          required
        >
          <InputNumber
            v-model="expectedWorkloadHours"
            :min="0.5"
            :max="40"
            :step="0.5"
            :min-fraction-digits="1"
            :max-fraction-digits="2"
            class="w-full"
            data-testid="input-expected-workload"
          />
        </FormField>

        <!-- Required Headcount -->
        <FormField
          label="Faculty Headcount Needed"
          name="requiredTeachersCount"
          :error="formErrors.requiredTeachersCount"
          required
        >
          <InputNumber
            v-model="requiredTeachersCount"
            :min="1"
            :max="10"
            class="w-full"
            data-testid="input-required-teachers"
          />
        </FormField>

        <!-- Requested Dates -->
        <FormField
          label="Requested Start Date"
          name="requestedStartDate"
          :error="formErrors.requestedStartDate"
          required
        >
          <InputText
            v-model="requestedStartDate"
            type="date"
            class="w-full text-sm"
            data-testid="input-start-date"
          />
        </FormField>

        <FormField
          label="Requested End Date"
          name="requestedEndDate"
          :error="formErrors.requestedEndDate"
          required
        >
          <InputText
            v-model="requestedEndDate"
            type="date"
            class="w-full text-sm"
            data-testid="input-end-date"
          />
        </FormField>
      </div>

      <!-- Optional Demand Code & Notes -->
      <div class="grid grid-cols-1 gap-4 pt-1">
        <FormField
          label="Demand Code (Optional Auto-Generated if Blank)"
          name="demandCode"
          :error="formErrors.demandCode"
        >
          <InputText
            v-model="demandCode"
            placeholder="e.g. DMD-2027-MATH01"
            class="w-full text-sm font-mono"
            data-testid="input-demand-code"
          />
        </FormField>

        <FormField
          label="Clarification Notes / Delivery Constraints (Optional)"
          name="clarificationNotes"
          :error="formErrors.clarificationNotes"
        >
          <Textarea
            v-model="clarificationNotes"
            rows="2"
            placeholder="Special scheduling or syllabus requirements..."
            class="w-full text-sm"
            data-testid="input-clarification-notes"
          />
        </FormField>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="createMutation.isPending.value"
          @click="handleClose"
          data-testid="cancel-create-demand-btn"
        />
        <Button
          label="Ingest Demand"
          icon="pi pi-check"
          severity="primary"
          size="small"
          :loading="createMutation.isPending.value"
          @click="handleSubmit"
          data-testid="submit-create-demand-btn"
        />
      </div>
    </template>
  </Dialog>
</template>
