<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { ShieldAlert } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  recordCorrectionSchema,
  type CorrectionTargetTable,
  type RecordCorrectionFormData,
} from '../../schemas/recordCorrection.schema';
import {
  useSubmitCorrectionMutation,
} from '../../composables/useAmendmentsQuery';
import type { OperationalPeriodDto } from '../../api/operationalPeriod.api';
import type { RecordCorrectionDto } from '../../api/amendment.api';

const props = defineProps<{
  visible: boolean;
  academicYearId?: string | null | undefined;
  targetTableName?: CorrectionTargetTable | undefined;
  targetRecordId?: string | undefined;
  fieldName?: string | undefined;
  oldValue?: string | null | undefined;
  periods?: OperationalPeriodDto[] | undefined;
  academicYearCode?: string | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'corrected', result: RecordCorrectionDto): void;
}>();

const authStore = useAuthStore();

const targetTableOptions = [
  { label: 'Operational Periods (operations.operational_periods)', value: 'operational_periods' },
  { label: 'Academic Years (operations.academic_years)', value: 'academic_years' },
];

const standardPeriodFields = [
  { label: 'Period Name (name)', value: 'name' },
  { label: 'Start Date (start_date)', value: 'startDate' },
  { label: 'End Date (end_date)', value: 'endDate' },
  { label: 'Campus Context (campus_context)', value: 'campusContext' },
  { label: 'Notes (notes)', value: 'notes' },
];

const standardYearFields = [
  { label: 'Year Name (name)', value: 'name' },
  { label: 'Start Date (start_date)', value: 'startDate' },
  { label: 'End Date (end_date)', value: 'endDate' },
  { label: 'Notes (notes)', value: 'notes' },
];

const { handleSubmit, errors, defineField, setValues } =
  useForm<RecordCorrectionFormData>({
    validationSchema: toTypedSchema(recordCorrectionSchema),
    initialValues: {
      academicYearId: props.academicYearId || undefined,
      targetTableName: props.targetTableName || 'operational_periods',
      targetRecordId: props.targetRecordId || '',
      fieldName: props.fieldName || '',
      newValue: '',
      justificationReason: '',
      authorizedBy: authStore.user?.username || '',
    },
  });

const [formTargetTableName] = defineField('targetTableName');
const [formTargetRecordId] = defineField('targetRecordId');
const [formFieldName] = defineField('fieldName');
const [formNewValue] = defineField('newValue');
const [formJustificationReason] = defineField('justificationReason');
const [formAuthorizedBy] = defineField('authorizedBy');

const localOldValue = ref<string>(props.oldValue ?? '');

// Available target records based on selected table
const recordOptions = computed(() => {
  if (formTargetTableName.value === 'academic_years') {
    return [
      {
        label: `Academic Year: ${props.academicYearCode || props.academicYearId || 'Current Year'}`,
        value: props.academicYearId || '',
      },
    ];
  }

  return (props.periods ?? []).map((p) => ({
    label: `${p.periodCode} — ${p.name} (${p.startDate} to ${p.endDate})`,
    value: p.id,
  }));
});

const fieldOptions = computed(() => {
  return formTargetTableName.value === 'academic_years'
    ? standardYearFields
    : standardPeriodFields;
});

// Sync initial values when dialog is triggered or props change
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      const initialTable = props.targetTableName || 'operational_periods';
      const initialRecordId =
        props.targetRecordId ||
        (initialTable === 'academic_years'
          ? (props.academicYearId || '')
          : (props.periods?.[0]?.id || ''));

      localOldValue.value = props.oldValue ?? '';

      setValues({
        academicYearId: props.academicYearId || undefined,
        targetTableName: initialTable,
        targetRecordId: initialRecordId,
        fieldName: props.fieldName || 'name',
        newValue: '',
        justificationReason: '',
        authorizedBy: authStore.user?.username || '',
      });
    }
  },
);

// When formTargetTableName changes, default formTargetRecordId appropriately
watch(
  () => formTargetTableName.value,
  (newTable) => {
    if (newTable === 'academic_years') {
      formTargetRecordId.value = props.academicYearId || '';
      formFieldName.value = 'name';
    } else {
      if (!props.periods?.some((p) => p.id === formTargetRecordId.value)) {
        formTargetRecordId.value = props.periods?.[0]?.id || '';
      }
      formFieldName.value = 'name';
    }
  },
);

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const correctionMutation = useSubmitCorrectionMutation({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('corrected', data);
  },
});

const onSubmit = handleSubmit((values) => {
  correctionMutation.mutate({
    academicYearId: props.academicYearId || undefined,
    targetTableName: values.targetTableName,
    targetRecordId: values.targetRecordId,
    fieldName: values.fieldName.trim(),
    newValue: values.newValue.trim(),
    justificationReason: values.justificationReason.trim(),
    authorizedBy: values.authorizedBy?.trim() || authStore.user?.username || null,
  });
});

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Authorized Record Correction (UC-OPS-13)"
    modal
    class="max-w-lg w-full"
    data-testid="correct-record-modal"
  >
    <form class="space-y-4 pt-2 text-sm text-slate-700" @submit.prevent="onSubmit">
      <!-- Institutional Warning Banner -->
      <div
        class="p-3 bg-amber-50 border border-amber-200 rounded-lg flex items-start gap-2.5 text-xs text-amber-900"
      >
        <ShieldAlert class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div class="space-y-0.5">
          <p class="font-semibold">Formal Audit Trail Required</p>
          <p class="text-amber-800">
            Approved operational records cannot be modified without an auditable justification reason.
            This correction will be permanently logged in <code>operations.operations_record_corrections</code>.
          </p>
        </div>
      </div>

      <!-- Target Table & Target Record -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="target-table"
          label="Target Table"
          required
          :error="errors.targetTableName"
        >
          <Select
            id="target-table"
            v-model="formTargetTableName"
            :options="targetTableOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="target-table-select"
          />
        </FormField>

        <FormField
          id="target-record"
          label="Target Record"
          required
          :error="errors.targetRecordId"
        >
          <Select
            id="target-record"
            v-model="formTargetRecordId"
            :options="recordOptions"
            option-label="label"
            option-value="value"
            placeholder="Select target record"
            class="w-full text-xs"
            data-testid="target-record-select"
          />
        </FormField>
      </div>

      <!-- Field Name & Old Value (Reference) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="field-name"
          label="Field to Correct"
          required
          :error="errors.fieldName"
        >
          <Select
            id="field-name"
            v-model="formFieldName"
            :options="fieldOptions"
            option-label="label"
            option-value="value"
            editable
            placeholder="e.g. startDate or name"
            class="w-full text-sm"
            data-testid="field-name-select"
          />
        </FormField>

        <FormField
          id="old-value"
          label="Current / Old Value"
          optional
          hint="Reference snapshot"
        >
          <InputText
            id="old-value"
            v-model="localOldValue"
            placeholder="(optional reference)"
            class="w-full text-sm bg-slate-50 text-slate-600"
            data-testid="old-value-input"
          />
        </FormField>
      </div>

      <!-- New Value -->
      <FormField
        id="new-value"
        label="New Corrected Value"
        required
        :error="errors.newValue"
        hint="Authoritative replacement value"
      >
        <InputText
          id="new-value"
          v-model="formNewValue"
          placeholder="Enter new value"
          class="w-full text-sm"
          :class="{ 'p-invalid': errors.newValue }"
          data-testid="new-value-input"
        />
      </FormField>

      <!-- Mandatory Justification Reason -->
      <FormField
        id="justification-reason"
        label="Justification Reason"
        required
        :error="errors.justificationReason"
        hint="Mandatory audit explanation (min. 5 characters)"
      >
        <Textarea
          id="justification-reason"
          v-model="formJustificationReason"
          rows="3"
          placeholder="e.g. Corrected start date to align with Ministry of Education decree No. 14/2026..."
          class="w-full text-sm"
          :class="{ 'p-invalid': errors.justificationReason }"
          data-testid="justification-reason-input"
        />
      </FormField>

      <!-- Authorized By -->
      <FormField
        id="authorized-by"
        label="Authorized By"
        optional
        :error="errors.authorizedBy"
        hint="Identity authorizing this correction"
      >
        <InputText
          id="authorized-by"
          v-model="formAuthorizedBy"
          placeholder="Operations Authority username"
          class="w-full text-sm"
          data-testid="authorized-by-input"
        />
      </FormField>

      <!-- Modal Footer Actions -->
      <div class="flex justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          text
          size="small"
          label="Cancel"
          data-testid="modal-cancel-btn"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          size="small"
          label="Submit Correction"
          class="bg-amber-600 hover:bg-amber-700 text-white"
          :loading="correctionMutation.isPending.value"
          data-testid="modal-submit-correction-btn"
          @click="onSubmit"
        />
      </div>
    </form>
  </Dialog>
</template>
