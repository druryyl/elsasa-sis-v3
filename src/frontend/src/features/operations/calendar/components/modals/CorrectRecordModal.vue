<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import {
  ShieldAlert,
  ShieldCheck,
  FileEdit,
  History,
  Lock,
  Sparkles,
  ArrowRight,
  Database,
  UserCheck,
} from 'lucide-vue-next';
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

const JUSTIFICATION_PRESETS = [
  'Adjustment per Ministry of Education / Religious Affairs decree',
  'Rectification of clerical entry date in approved schedule',
  'Academic Senate resolution on revised instructional calendar',
  'Institutional campus holiday realignment',
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

function applyJustificationPreset(preset: string): void {
  formJustificationReason.value = preset;
}

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
    class="max-w-xl w-full"
    data-testid="correct-record-modal"
  >
    <!-- Custom Header -->
    <template #header>
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-xl bg-amber-50 border border-amber-200/80 flex items-center justify-center shrink-0 text-amber-700 shadow-2xs"
        >
          <ShieldAlert class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-900 text-base leading-snug">
              Authorized Record Correction (UC-OPS-13)
            </h3>
            <span
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200"
            >
              <Lock class="w-3 h-3 text-amber-600" />
              Audit Controlled
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Submit formal modifications to locked Academic Years or active Operational Periods.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1 text-sm text-slate-700" @submit.prevent="onSubmit">
      <!-- Institutional Warning Banner -->
      <div
        class="p-3.5 bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200/90 rounded-xl flex items-start gap-3 text-xs text-amber-900 shadow-2xs leading-relaxed"
      >
        <div class="p-1 rounded bg-amber-100/90 text-amber-800 shrink-0 mt-0.5">
          <ShieldAlert class="w-4 h-4 text-amber-700" />
        </div>
        <div class="space-y-1">
          <p class="font-semibold text-amber-950">Formal Audit Trail Required</p>
          <p class="text-amber-800/95 leading-normal">
            Approved operational records cannot be modified without an auditable justification reason.
            This correction will be permanently logged in
            <code class="px-1.5 py-0.5 bg-amber-100/70 border border-amber-200 rounded font-mono text-[11px] text-amber-950">operations.operations_record_corrections</code>.
          </p>
        </div>
      </div>

      <!-- Section 1: Target Entity Card -->
      <div class="bg-white border border-slate-200/90 rounded-xl p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wider">
          <Database class="w-3.5 h-3.5 text-indigo-600" />
          <span>1. Target Record Identification</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="target-table"
            label="Target Table"
            required
            :error="errors.targetTableName"
            help-text="Database entity scope"
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
            help-text="Specific entity instance"
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
      </div>

      <!-- Section 2: Before & After Diff Card -->
      <div class="bg-white border border-slate-200/90 rounded-xl p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wider">
            <FileEdit class="w-3.5 h-3.5 text-teal-600" />
            <span>2. Canonical Field & Value Transformation</span>
          </div>
          <span class="text-[11px] text-slate-500 font-medium">Exact Value Replacement</span>
        </div>

        <!-- Field to Correct Selection -->
        <FormField
          id="field-name"
          label="Field to Correct"
          required
          :error="errors.fieldName"
          help-text="Select attribute or enter schema column"
        >
          <Select
            id="field-name"
            v-model="formFieldName"
            :options="fieldOptions"
            option-label="label"
            option-value="value"
            editable
            placeholder="e.g. startDate or name"
            class="w-full text-xs"
            data-testid="field-name-select"
          />
        </FormField>

        <!-- Before & After Comparison Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
          <FormField
            id="old-value"
            label="Current / Old Value (Snapshot)"
            optional
            help-text="Immutable baseline state"
          >
            <div class="relative">
              <InputText
                id="old-value"
                v-model="localOldValue"
                placeholder="(Current value snapshot)"
                class="w-full text-xs bg-slate-50/90 text-slate-600 font-mono pl-7 border-slate-200"
                data-testid="old-value-input"
              />
              <Lock class="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
            </div>
          </FormField>

          <FormField
            id="new-value"
            label="New Corrected Value"
            required
            :error="errors.newValue"
            help-text="Authoritative replacement"
          >
            <InputText
              id="new-value"
              v-model="formNewValue"
              placeholder="Enter new value"
              class="w-full text-xs font-medium"
              :class="{ 'p-invalid': errors.newValue }"
              data-testid="new-value-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Section 3: Justification & Authorization Card -->
      <div class="bg-white border border-slate-200/90 rounded-xl p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wider">
            <History class="w-3.5 h-3.5 text-amber-600" />
            <span>3. Audit Justification & Sign-off</span>
          </div>
          <span class="text-[11px] text-amber-700 font-medium">Mandatory & Geolocation Audited</span>
        </div>

        <!-- Quick Reason Presets -->
        <div class="p-2 bg-slate-50 border border-slate-200/70 rounded-lg space-y-1.5">
          <div class="flex items-center gap-1 text-[11px] font-medium text-slate-600">
            <Sparkles class="w-3 h-3 text-amber-500" />
            <span>Quick Justification Presets:</span>
          </div>
          <div class="flex flex-wrap gap-1">
            <button
              v-for="preset in JUSTIFICATION_PRESETS"
              :key="preset"
              type="button"
              class="text-[11px] px-2 py-0.5 rounded bg-white border border-slate-200 text-slate-700 hover:border-amber-400 hover:text-amber-800 hover:bg-amber-50/50 transition cursor-pointer text-left truncate max-w-full"
              @click="applyJustificationPreset(preset)"
            >
              {{ preset }}
            </button>
          </div>
        </div>

        <!-- Mandatory Justification Reason -->
        <FormField
          id="justification-reason"
          label="Justification Reason"
          required
          :error="errors.justificationReason"
          help-text="Formal compliance explanation (min. 5 characters)"
        >
          <Textarea
            id="justification-reason"
            v-model="formJustificationReason"
            rows="3"
            placeholder="e.g. Corrected start date to align with Ministry of Education decree No. 14/2026..."
            class="w-full text-xs leading-relaxed"
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
          help-text="Authenticated Operations Authority credential"
        >
          <div class="relative">
            <InputText
              id="authorized-by"
              v-model="formAuthorizedBy"
              placeholder="Operations Authority username"
              class="w-full text-xs pl-7"
              data-testid="authorized-by-input"
            />
            <UserCheck class="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
          </div>
        </FormField>
      </div>

      <!-- Modal Footer Actions -->
      <div class="flex items-center justify-between pt-3 border-t border-slate-100">
        <div class="flex items-center gap-1 text-xs text-slate-500">
          <ShieldCheck class="w-3.5 h-3.5 text-amber-600" />
          <span>Audit Record Append Only</span>
        </div>

        <div class="flex items-center gap-2">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            class="text-xs text-slate-600 hover:text-slate-800"
            data-testid="modal-cancel-btn"
            @click="isDialogVisible = false"
          />
          <Button
            type="submit"
            size="small"
            label="Submit Correction"
            icon="pi pi-shield"
            class="bg-amber-600 hover:bg-amber-700 text-white font-medium text-xs px-3.5 py-1.5 shadow-xs"
            :loading="correctionMutation.isPending.value"
            data-testid="modal-submit-correction-btn"
            @click="onSubmit"
          />
        </div>
      </div>
    </form>
  </Dialog>
</template>
