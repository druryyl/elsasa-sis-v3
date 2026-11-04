<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import DatePicker from 'primevue/datepicker';
import { FileText, Calendar, PlusCircle } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  createFinancialPolicySchema,
  type CreateFinancialPolicySchemaInput,
  POLICY_CATEGORIES,
} from '../schemas/policies.schema';
import { useCreatePolicyMutation } from '../composables/useFinancePolicies';
import { formatDateOnly, parseIsoDate } from '@/shared/utils/dateUtils';
import type { CreateFinancialPolicyResponse } from '../types/policies.types';

const props = withDefaults(
  defineProps<{
    visible: boolean;
  }>(),
  {
    visible: false,
  },
);

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', response: CreateFinancialPolicyResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const categoryOptions = POLICY_CATEGORIES.map((cat) => ({
  value: cat,
  label: cat.charAt(0).toUpperCase() + cat.slice(1),
}));

const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<CreateFinancialPolicySchemaInput>({
    validationSchema: toTypedSchema(createFinancialPolicySchema),
    initialValues: {
      policyCode: '',
      name: '',
      category: 'tuition',
      businessPurpose: '',
      applicableContext: '',
      effectiveDate: formatDateOnly(new Date()),
      endDate: '',
      policyVersion: '1.0',
      notes: '',
    },
  });

const [policyCode] = defineField('policyCode');
const [name] = defineField('name');
const [category] = defineField('category');
const [businessPurpose] = defineField('businessPurpose');
const [applicableContext] = defineField('applicableContext');
const [effectiveDate] = defineField('effectiveDate');
const [endDate] = defineField('endDate');
const [policyVersion] = defineField('policyVersion');
const [notes] = defineField('notes');

const effectiveDatePickerModel = computed({
  get: () => {
    if (!effectiveDate.value) return null;
    return typeof effectiveDate.value === 'string'
      ? parseIsoDate(effectiveDate.value)
      : (effectiveDate.value as unknown as Date);
  },
  set: (val: Date | string | null) => {
    effectiveDate.value = val ? formatDateOnly(val) : '';
  },
});

const endDatePickerModel = computed({
  get: () => {
    if (!endDate.value) return null;
    return typeof endDate.value === 'string'
      ? parseIsoDate(endDate.value)
      : (endDate.value as unknown as Date);
  },
  set: (val: Date | string | null) => {
    endDate.value = val ? formatDateOnly(val) : '';
  },
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          policyCode: '',
          name: '',
          category: 'tuition',
          businessPurpose: '',
          applicableContext: '',
          effectiveDate: formatDateOnly(new Date()),
          endDate: '',
          policyVersion: '1.0',
          notes: '',
        },
      });
    }
  },
);

const createPolicyMutation = useCreatePolicyMutation({
  onSuccess: (data) => {
    emit('created', data);
    emit('update:visible', false);
    resetForm();
  },
});

const isPending = computed(
  () => createPolicyMutation.isPending.value || isSubmitting.value,
);

const onSubmit = handleSubmit(async (values) => {
  await createPolicyMutation.mutateAsync({
    policyCode: values.policyCode.trim(),
    name: values.name.trim(),
    category: values.category,
    businessPurpose: values.businessPurpose.trim(),
    applicableContext: values.applicableContext?.trim() || null,
    effectiveDate: values.effectiveDate,
    endDate: values.endDate?.trim() || null,
    policyVersion: values.policyVersion?.trim() || '1.0',
    notes: values.notes?.trim() || null,
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
    header="Create Financial Policy Draft"
    :style="{ width: '560px' }"
    :closable="!isPending"
    class="p-dialog-sm"
    data-testid="create-policy-dialog"
  >
    <form class="space-y-4 pt-2" @submit.prevent="onSubmit">
      <!-- Top Banner Notice -->
      <div class="p-3 rounded-lg border border-teal-200 bg-teal-50/70 text-xs text-teal-800 space-y-1">
        <div class="flex items-center gap-1.5 font-semibold text-teal-900">
          <FileText class="w-4 h-4 text-teal-700 shrink-0" />
          <span>New Policy Initialization</span>
        </div>
        <p>
          Policies are created in <span class="font-semibold text-teal-950">Draft</span> status. Once charge rules are attached, the policy requires formal approval by the Finance Authority before taking effect.
        </p>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Policy Code -->
        <FormField
          label="Policy Code"
          required
          :error="errors.policyCode"
          help-text="Unique identifier (e.g., FP-2026-002)"
        >
          <InputText
            id="policy-code-input"
            v-model="policyCode"
            type="text"
            placeholder="FP-2026-002"
            class="w-full !text-xs !py-2"
            data-testid="policy-code-input"
          />
        </FormField>

        <!-- Version -->
        <FormField
          label="Version"
          optional
          :error="errors.policyVersion"
          help-text="Initial version label"
        >
          <InputText
            id="policy-version-input"
            v-model="policyVersion"
            type="text"
            placeholder="1.0"
            class="w-full !text-xs !py-2"
            data-testid="policy-version-input"
          />
        </FormField>
      </div>

      <!-- Policy Name -->
      <FormField
        label="Policy Name"
        required
        :error="errors.name"
        help-text="Official descriptive title of the policy"
      >
        <InputText
          id="policy-name-input"
          v-model="name"
          type="text"
          placeholder="e.g. Kebijakan Biaya Kegiatan dan Fasilitas TP 2026/2027"
          class="w-full !text-xs !py-2"
          data-testid="policy-name-input"
        />
      </FormField>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Category -->
        <FormField
          label="Category"
          required
          :error="errors.category"
        >
          <Select
            id="policy-category-select"
            v-model="category"
            :options="categoryOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Category"
            class="w-full !text-xs"
            data-testid="policy-category-select"
          />
        </FormField>

        <!-- Applicable Context -->
        <FormField
          label="Applicable Context"
          optional
          :error="errors.applicableContext"
          help-text="e.g. Seluruh Kampus, TK-SD"
        >
          <InputText
            id="policy-context-input"
            v-model="applicableContext"
            type="text"
            placeholder="e.g. Seluruh Kampus"
            class="w-full !text-xs !py-2"
            data-testid="policy-context-input"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Effective Date -->
        <FormField
          label="Effective Date"
          required
          :error="errors.effectiveDate"
        >
          <DatePicker
            id="policy-effective-date"
            v-model="effectiveDatePickerModel"
            date-format="yy-mm-dd"
            placeholder="YYYY-MM-DD"
            show-icon
            class="w-full !text-xs"
            data-testid="policy-effective-date"
          />
        </FormField>

        <!-- End Date -->
        <FormField
          label="End Date"
          optional
          :error="errors.endDate"
          help-text="Leave blank if indefinite"
        >
          <DatePicker
            id="policy-end-date"
            v-model="endDatePickerModel"
            date-format="yy-mm-dd"
            placeholder="YYYY-MM-DD"
            show-icon
            class="w-full !text-xs"
            data-testid="policy-end-date"
          />
        </FormField>
      </div>

      <!-- Business Purpose -->
      <FormField
        label="Business Purpose"
        required
        :error="errors.businessPurpose"
        help-text="Describe governance rationale and billing scope"
      >
        <Textarea
          id="policy-purpose-input"
          v-model="businessPurpose"
          rows="3"
          placeholder="Detailed explanation of the policy intent and financial obligations governance..."
          class="w-full !text-xs"
          data-testid="policy-purpose-input"
        />
      </FormField>

      <!-- Administrative Notes -->
      <FormField
        label="Administrative Notes"
        optional
        :error="errors.notes"
      >
        <Textarea
          id="policy-notes-input"
          v-model="notes"
          rows="2"
          placeholder="Optional internal governance notes..."
          class="w-full !text-xs"
          data-testid="policy-notes-input"
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
          data-testid="cancel-policy-button"
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
          data-testid="submit-policy-button"
        >
          <PlusCircle class="w-3.5 h-3.5" />
          <span>Create Policy Draft</span>
        </Button>
      </div>
    </form>
  </Dialog>
</template>
