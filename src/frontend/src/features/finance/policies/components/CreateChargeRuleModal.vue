<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import { Layers, PlusCircle, Sparkles } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  createChargeRuleSchema,
  type CreateChargeRuleSchemaInput,
  RECURRENCE_PATTERNS,
  DUE_RULE_TYPES,
} from '../schemas/policies.schema';
import { useCreateChargeRuleMutation } from '../composables/useFinancePolicies';
import type { CreateChargeRuleResponse } from '../types/policies.types';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    policyId: string;
    policyCode?: string;
    policyName?: string;
  }>(),
  {
    visible: false,
    policyCode: '',
    policyName: '',
  },
);

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', response: CreateChargeRuleResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const recurrenceOptions = [
  { value: 'monthly', label: 'Monthly (Bulanan)' },
  { value: 'one_time', label: 'One Time (Sekali Bayar)' },
  { value: 'term', label: 'Term (Per Semester / Term)' },
  { value: 'annual', label: 'Annual (Tahunan)' },
];

const dueRuleTypeOptions = [
  { value: 'fixed_day_of_month', label: 'Fixed Day of Month (e.g. day 10)' },
  { value: 'days_after_issuance', label: 'Days After Issuance (e.g. 14 days)' },
  { value: 'fixed_calendar_date', label: 'Fixed Calendar Date (YYYY-MM-DD)' },
];

const chargeTypeOptions = [
  { value: 'tuition', label: 'Tuition (SPP / Biaya Pendidikan)' },
  { value: 'admission', label: 'Admission (Pendaftaran)' },
  { value: 'enrollment', label: 'Enrollment (Uang Pangkal)' },
  { value: 'facilities', label: 'Facilities (Sarana & Gedung)' },
  { value: 'activities', label: 'Activities (Kegiatan)' },
  { value: 'general', label: 'General / Miscellaneous' },
];

const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<CreateChargeRuleSchemaInput>({
    validationSchema: toTypedSchema(createChargeRuleSchema),
    initialValues: {
      ruleCode: '',
      name: '',
      chargeType: 'tuition',
      baseAmount: 0,
      currencyCode: 'IDR',
      recurrencePattern: 'monthly',
      dueRuleType: 'fixed_day_of_month',
      dueRuleValue: '10',
      isMandatory: true,
      determinationMethod: 'fixed_amount',
      applicableGrade: '',
      applicableProgram: '',
      notes: '',
    },
  });

const [ruleCode] = defineField('ruleCode');
const [name] = defineField('name');
const [chargeType] = defineField('chargeType');
const [baseAmount] = defineField('baseAmount');
const [recurrencePattern] = defineField('recurrencePattern');
const [dueRuleType] = defineField('dueRuleType');
const [dueRuleValue] = defineField('dueRuleValue');
const [isMandatory] = defineField('isMandatory');
const [applicableGrade] = defineField('applicableGrade');
const [applicableProgram] = defineField('applicableProgram');
const [notes] = defineField('notes');

const dueRulePlaceholder = computed(() => {
  switch (dueRuleType.value) {
    case 'fixed_day_of_month':
      return 'e.g. 10 (day of month, 1-31)';
    case 'days_after_issuance':
      return 'e.g. 14 (days after issuance)';
    case 'fixed_calendar_date':
      return 'e.g. 2026-07-31 (YYYY-MM-DD)';
    default:
      return 'Due rule parameter value';
  }
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          ruleCode: '',
          name: '',
          chargeType: 'tuition',
          baseAmount: 0,
          currencyCode: 'IDR',
          recurrencePattern: 'monthly',
          dueRuleType: 'fixed_day_of_month',
          dueRuleValue: '10',
          isMandatory: true,
          determinationMethod: 'fixed_amount',
          applicableGrade: '',
          applicableProgram: '',
          notes: '',
        },
      });
    }
  },
);

const createChargeRuleMutation = useCreateChargeRuleMutation({
  onSuccess: (data) => {
    emit('created', data);
    emit('update:visible', false);
    resetForm();
  },
});

const isPending = computed(
  () => createChargeRuleMutation.isPending.value || isSubmitting.value,
);

const onSubmit = handleSubmit(async (values) => {
  if (!props.policyId) return;

  await createChargeRuleMutation.mutateAsync({
    policyId: props.policyId,
    request: {
      ruleCode: values.ruleCode.trim(),
      name: values.name.trim(),
      chargeType: values.chargeType.trim(),
      baseAmount: Math.round(Number(values.baseAmount) || 0),
      currencyCode: 'IDR',
      recurrencePattern: values.recurrencePattern,
      dueRuleType: values.dueRuleType,
      dueRuleValue: values.dueRuleValue.trim(),
      isMandatory: Boolean(values.isMandatory),
      determinationMethod: 'fixed_amount',
      applicableGrade: values.applicableGrade?.trim() || null,
      applicableProgram: values.applicableProgram?.trim() || null,
      notes: values.notes?.trim() || null,
    },
  });
});

function handleClose() {
  if (isPending.value) return;
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Add Charge Rule"
    :style="{ width: '580px' }"
    :closable="!isPending"
    class="p-dialog-sm"
    data-testid="create-charge-rule-dialog"
  >
    <form class="space-y-4 pt-2" @submit.prevent="onSubmit">
      <!-- Target Policy Context Card -->
      <div class="p-3 rounded-lg border border-slate-200 bg-slate-50/80 text-xs text-slate-700 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <Layers class="w-4 h-4 text-teal-600 shrink-0" />
          <span class="font-medium text-slate-500">Governing Policy:</span>
          <span class="font-bold text-slate-900">{{ props.policyCode || 'Selected Policy' }}</span>
        </div>
        <span v-if="props.policyName" class="text-slate-500 truncate max-w-[200px]" :title="props.policyName">
          {{ props.policyName }}
        </span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Rule Code -->
        <FormField
          label="Rule Code"
          required
          :error="errors.ruleCode"
          help-text="e.g. CR-2026-SPP-SD"
        >
          <InputText
            id="rule-code-input"
            v-model="ruleCode"
            type="text"
            placeholder="CR-2026-SPP-SD"
            class="w-full !text-xs !py-2"
            data-testid="rule-code-input"
          />
        </FormField>

        <!-- Charge Type -->
        <FormField
          label="Charge Type"
          required
          :error="errors.chargeType"
        >
          <Select
            id="rule-charge-type-select"
            v-model="chargeType"
            :options="chargeTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Charge Type"
            class="w-full !text-xs"
            data-testid="rule-charge-type-select"
          />
        </FormField>
      </div>

      <!-- Rule Name -->
      <FormField
        label="Charge Rule Name"
        required
        :error="errors.name"
        help-text="Descriptive fee title (e.g. SPP Bulanan SD Kelas 1-6)"
      >
        <InputText
          id="rule-name-input"
          v-model="name"
          type="text"
          placeholder="e.g. SPP Bulanan SD"
          class="w-full !text-xs !py-2"
          data-testid="rule-name-input"
        />
      </FormField>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Base Amount (IDR) -->
        <FormField
          label="Base Amount (IDR)"
          required
          :error="errors.baseAmount"
          help-text="Exact integer amount (Rp)"
        >
          <InputNumber
            id="rule-base-amount-input"
            v-model="baseAmount"
            mode="currency"
            currency="IDR"
            locale="id-ID"
            :min="0"
            :fraction-digits="0"
            placeholder="Rp 0"
            class="w-full !text-xs"
            data-testid="rule-base-amount-input"
          />
        </FormField>

        <!-- Recurrence Pattern -->
        <FormField
          label="Recurrence Pattern"
          required
          :error="errors.recurrencePattern"
        >
          <Select
            id="rule-recurrence-select"
            v-model="recurrencePattern"
            :options="recurrenceOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Pattern"
            class="w-full !text-xs"
            data-testid="rule-recurrence-select"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Due Rule Type -->
        <FormField
          label="Due Rule Type"
          required
          :error="errors.dueRuleType"
        >
          <Select
            id="rule-due-type-select"
            v-model="dueRuleType"
            :options="dueRuleTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Due Rule"
            class="w-full !text-xs"
            data-testid="rule-due-type-select"
          />
        </FormField>

        <!-- Due Rule Value -->
        <FormField
          label="Due Rule Value"
          required
          :error="errors.dueRuleValue"
          help-text="Parameter for due date calculation"
        >
          <InputText
            id="rule-due-value-input"
            v-model="dueRuleValue"
            type="text"
            :placeholder="dueRulePlaceholder"
            class="w-full !text-xs !py-2"
            data-testid="rule-due-value-input"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Applicable Grade -->
        <FormField
          label="Applicable Grade"
          optional
          :error="errors.applicableGrade"
          help-text="e.g. Grade 1, SD, or blank for all"
        >
          <InputText
            id="rule-grade-input"
            v-model="applicableGrade"
            type="text"
            placeholder="All Grades"
            class="w-full !text-xs !py-2"
            data-testid="rule-grade-input"
          />
        </FormField>

        <!-- Applicable Program -->
        <FormField
          label="Applicable Program"
          optional
          :error="errors.applicableProgram"
          help-text="e.g. regular, cambridge, or blank"
        >
          <InputText
            id="rule-program-input"
            v-model="applicableProgram"
            type="text"
            placeholder="All Programs"
            class="w-full !text-xs !py-2"
            data-testid="rule-program-input"
          />
        </FormField>
      </div>

      <!-- Mandatory Checkbox -->
      <div class="flex items-center gap-2 pt-1">
        <Checkbox
          id="rule-mandatory-checkbox"
          v-model="isMandatory"
          binary
          data-testid="rule-mandatory-checkbox"
        />
        <label for="rule-mandatory-checkbox" class="text-xs font-medium text-slate-700 cursor-pointer select-none">
          Mandatory fee obligation (automatically included in standard billing assessment)
        </label>
      </div>

      <!-- Notes -->
      <FormField
        label="Internal Notes"
        optional
        :error="errors.notes"
      >
        <Textarea
          id="rule-notes-input"
          v-model="notes"
          rows="2"
          placeholder="Optional notes or references for this rule..."
          class="w-full !text-xs"
          data-testid="rule-notes-input"
        />
      </FormField>

      <!-- Modal Footer Actions -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          :disabled="isPending"
          class="!text-xs cursor-pointer"
          data-testid="cancel-rule-button"
          @click="handleClose"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="small"
          severity="primary"
          :loading="isPending"
          class="!text-xs cursor-pointer bg-teal-700 hover:bg-teal-800 text-white font-medium inline-flex items-center gap-1.5"
          data-testid="submit-rule-button"
        >
          <PlusCircle class="w-3.5 h-3.5" />
          <span>Add Charge Rule</span>
        </Button>
      </div>
    </form>
  </Dialog>
</template>
