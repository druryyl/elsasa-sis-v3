<script setup lang="ts">
import { computed, watch, ref } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import DatePicker from 'primevue/datepicker';
import {
  Award,
  Calendar,
  FileText,
  AlertCircle,
  HelpCircle,
  CheckCircle2,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  addAdjustmentSchema,
  type AddAdjustmentFormValues,
} from '../schemas/learningPlan.schema';
import { useAddAdjustmentMutation } from '../composables/useLearningPlans';
import type { AdjustmentType } from '../types';
import { formatDateOnly, parseIsoDate } from '@/shared/utils/dateUtils';

interface Props {
  visible: boolean;
  planId: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'added', adjustmentId: string): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const submissionError = ref<string | null>(null);

// Category options for the adjustment category selector
const categoryOptions = [
  {
    label: 'Accommodation',
    value: 'accommodation',
    hint: 'Assessment & instruction adaptations (e.g. 50% extra exam time, assistive tech)',
    badgeColor: 'text-blue-600 bg-blue-50 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800',
  },
  {
    label: 'Substitution',
    value: 'substitution',
    hint: 'Course or curriculum requirement replacement (e.g. Adaptive PJOK)',
    badgeColor: 'text-purple-600 bg-purple-50 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800',
  },
  {
    label: 'Exemption',
    value: 'exemption',
    hint: 'Formal waiver of specific requirements based on verified medical/external documentation',
    badgeColor: 'text-amber-600 bg-amber-50 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800',
  },
];

// VeeValidate form initialization backed by Zod schema
const {
  handleSubmit,
  errors,
  defineField,
  resetForm,
  isSubmitting,
} = useForm<AddAdjustmentFormValues>({
  validationSchema: toTypedSchema(addAdjustmentSchema),
  initialValues: {
    adjustmentType: 'accommodation',
    description: '',
    rationale: '',
    effectiveDate: formatDateOnly(new Date()),
    endDate: '',
    notes: '',
  },
});

const [adjustmentType] = defineField('adjustmentType');
const [description] = defineField('description');
const [rationale] = defineField('rationale');
const [effectiveDate] = defineField('effectiveDate');
const [endDate] = defineField('endDate');
const [notes] = defineField('notes');

// Computed two-way date picker models converting between DatePicker objects and YYYY-MM-DD strings
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

// Reset form fields when modal visibility toggles
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      submissionError.value = null;
      resetForm({
        values: {
          adjustmentType: 'accommodation',
          description: '',
          rationale: '',
          effectiveDate: formatDateOnly(new Date()),
          endDate: '',
          notes: '',
        },
      });
    }
  },
);

const addAdjustmentMutation = useAddAdjustmentMutation();

function handleClose() {
  isDialogVisible.value = false;
  submissionError.value = null;
}

const onSubmit = handleSubmit(async (values) => {
  if (!props.planId) {
    submissionError.value = 'Learning plan identifier is required.';
    return;
  }

  submissionError.value = null;

  try {
    const result = await addAdjustmentMutation.mutateAsync({
      planId: props.planId,
      request: {
        adjustmentType: values.adjustmentType as AdjustmentType,
        description: values.description.trim(),
        rationale: values.rationale.trim(),
        effectiveDate: values.effectiveDate.trim(),
        endDate: values.endDate?.trim() || null,
        notes: values.notes?.trim() || null,
      },
    });

    emit('added', result.id);
    handleClose();
  } catch (err: unknown) {
    if (err && typeof err === 'object' && 'message' in err) {
      submissionError.value = String((err as { message: string }).message);
    } else {
      submissionError.value = 'An unexpected error occurred while adding the academic adjustment.';
    }
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :header="'Add Academic Plan Adjustment'"
    :style="{ width: '560px', maxWidth: '95vw' }"
    :closable="!isSubmitting"
    data-testid="add-adjustment-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-lg bg-purple-50 dark:bg-purple-950/40 text-purple-600 dark:text-purple-400 flex items-center justify-center">
          <Award class="w-4 h-4" />
        </div>
        <div>
          <h2 class="text-sm font-bold text-slate-900 dark:text-slate-100">
            Add Academic Plan Adjustment
          </h2>
          <p class="text-[11px] text-slate-500 dark:text-slate-400">
            Establish accommodations, course substitutions, or requirement exemptions.
          </p>
        </div>
      </div>
    </template>

    <form @submit.prevent="onSubmit" class="space-y-4 pt-2">
      <!-- Error Banner -->
      <div
        v-if="submissionError"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs flex items-start gap-2"
        data-testid="adjustment-submission-error"
      >
        <AlertCircle class="w-4 h-4 mt-0.5 flex-shrink-0" />
        <span>{{ submissionError }}</span>
      </div>

      <!-- 1. Adjustment Category Selector -->
      <FormField
        label="Adjustment Category"
        required
        :error="errors.adjustmentType"
        help-text="Select accommodation, substitution, or exemption."
      >
        <Select
          id="adjustment-type-select"
          v-model="adjustmentType"
          :options="categoryOptions"
          option-label="label"
          option-value="value"
          placeholder="Select Adjustment Category"
          class="w-full text-xs"
          data-testid="adjustment-type-select"
        >
          <template #option="{ option }">
            <div class="py-1">
              <div class="flex items-center gap-2 font-semibold text-xs text-slate-900 dark:text-slate-100">
                <span
                  class="px-1.5 py-0.5 text-[10px] rounded border uppercase font-bold"
                  :class="option.badgeColor"
                >
                  {{ option.label }}
                </span>
              </div>
              <p class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5 leading-snug">
                {{ option.hint }}
              </p>
            </div>
          </template>
        </Select>
      </FormField>

      <!-- 2. Description Textarea -->
      <FormField
        label="Description"
        required
        :error="errors.description"
        help-text="Specific details of the adaptation or curriculum modification."
      >
        <Textarea
          id="adjustment-description-input"
          v-model="description"
          rows="3"
          placeholder="e.g. 50% extra exam time for written assessments, assistive screen reader software..."
          class="w-full text-xs"
          data-testid="adjustment-description-input"
        />
      </FormField>

      <!-- 3. Justification Rationale Textarea (min 10 chars) -->
      <FormField
        label="Justification Rationale"
        required
        :error="errors.rationale"
        help-text="Professional justification or documented diagnosis (minimum 10 characters)."
      >
        <Textarea
          id="adjustment-rationale-input"
          v-model="rationale"
          rows="3"
          placeholder="e.g. Diagnosed processing speed disparity documented in developmental assessment report..."
          class="w-full text-xs"
          data-testid="adjustment-rationale-input"
        />
      </FormField>

      <!-- 4. Date Pickers Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Effective Date Picker -->
        <FormField
          label="Effective Date"
          required
          :error="errors.effectiveDate"
          help-text="Date the adjustment becomes operative."
        >
          <DatePicker
            id="adjustment-effective-date"
            v-model="effectiveDatePickerModel"
            date-format="yy-mm-dd"
            placeholder="YYYY-MM-DD"
            show-icon
            class="w-full text-xs"
            data-testid="adjustment-effective-date"
          />
        </FormField>

        <!-- Optional End Date Picker -->
        <FormField
          label="End Date"
          optional
          :error="errors.endDate"
          help-text="Leave blank if adjustment is ongoing."
        >
          <DatePicker
            id="adjustment-end-date"
            v-model="endDatePickerModel"
            date-format="yy-mm-dd"
            placeholder="YYYY-MM-DD (Optional)"
            show-icon
            class="w-full text-xs"
            data-testid="adjustment-end-date"
          />
        </FormField>
      </div>

      <!-- 5. Additional Operational Notes (Optional) -->
      <FormField
        label="Administrative Notes"
        optional
        :error="errors.notes"
        help-text="Optional notes for evaluators, coordinators, and teachers."
      >
        <Textarea
          id="adjustment-notes-input"
          v-model="notes"
          rows="2"
          placeholder="e.g. Reviewed with student development officer on 2026-10-15..."
          class="w-full text-xs"
          data-testid="adjustment-notes-input"
        />
      </FormField>

      <!-- Action Footer -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          class="text-xs"
          :disabled="isSubmitting"
          @click="handleClose"
        />
        <Button
          type="submit"
          label="Add Adjustment"
          icon="pi pi-check"
          size="small"
          class="text-xs font-semibold"
          :loading="isSubmitting || addAdjustmentMutation.isPending.value"
          data-testid="submit-adjustment-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
