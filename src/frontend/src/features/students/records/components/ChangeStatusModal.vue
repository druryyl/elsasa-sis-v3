<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import DatePicker from 'primevue/datepicker';
import {
  Activity,
  Calendar,
  FileText,
  UserCheck,
  ShieldAlert,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  changeStatusSchema,
  type ChangeStatusFormData,
} from '../schemas/records.schema';
import { useChangeStatusMutation } from '../composables/useStudentRecordsMutations';
import { formatDateOnly, parseIsoDate } from '@/shared/utils/dateUtils';
import type { StudentDetail, ChangeStudentStatusResponse } from '../types/records.types';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studentId?: string;
    student?: StudentDetail | { id: string; currentStatus?: string; fullName?: string };
    currentStatus?: string;
  }>(),
  {
    studentId: '',
    student: undefined,
    currentStatus: '',
  },
);

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'changed', response: ChangeStudentStatusResponse): void;
  (e: 'saved'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const targetStudentId = computed(
  () => props.studentId || props.student?.id || '',
);

const studentDisplayName = computed(() => {
  if (props.student && 'fullName' in props.student) {
    return props.student.fullName;
  }
  return null;
});

const statusOptions = [
  {
    label: 'Active',
    value: 'active',
    badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    desc: 'Regular enrolled student actively attending academic sessions',
  },
  {
    label: 'Enrolled',
    value: 'enrolled',
    badgeClass: 'bg-sky-50 text-sky-700 border-sky-200',
    desc: 'Officially enrolled record pending placement or active term startup',
  },
];

const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<ChangeStatusFormData>({
    validationSchema: toTypedSchema(changeStatusSchema),
    initialValues: {
      status: (props.currentStatus as 'active' | 'enrolled') || 'active',
      effectiveDate: formatDateOnly(new Date()),
      reason: '',
      authorizedBy: '',
      decisionReference: '',
    },
  });

const [status] = defineField('status');
const [effectiveDate] = defineField('effectiveDate');
const [reason] = defineField('reason');
const [authorizedBy] = defineField('authorizedBy');
const [decisionReference] = defineField('decisionReference');

const datePickerModel = computed({
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

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      const derivedCurrent =
        props.currentStatus === 'enrolled' || props.currentStatus === 'active'
          ? (props.currentStatus as 'active' | 'enrolled')
          : props.student?.currentStatus === 'enrolled' || props.student?.currentStatus === 'active'
            ? (props.student.currentStatus as 'active' | 'enrolled')
            : 'active';

      resetForm({
        values: {
          status: derivedCurrent,
          effectiveDate: formatDateOnly(new Date()),
          reason: '',
          authorizedBy: '',
          decisionReference: '',
        },
      });
    }
  },
);

const changeStatusMutation = useChangeStatusMutation(targetStudentId, {
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('saved');
    emit('changed', data);
    resetForm();
  },
});

const isPending = computed(
  () => changeStatusMutation.isPending.value || isSubmitting.value,
);

const onSubmit = handleSubmit(async (values) => {
  const studentId = targetStudentId.value;
  if (!studentId) return;

  await changeStatusMutation.mutateAsync({
    studentId,
    payload: {
      status: values.status,
      effectiveDate: values.effectiveDate,
      reason: values.reason?.trim() || null,
      authorizedBy: values.authorizedBy?.trim() || null,
      decisionReference: values.decisionReference?.trim() || null,
    },
  });
});

function handleClose() {
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    class="max-w-lg w-full"
    data-testid="change-status-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-2xs">
          <Activity class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Adjust Operational Status
            </h3>
            <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800">
              TD-05
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400 mt-0.5">
            <span v-if="studentDisplayName">For <strong class="text-surface-700 dark:text-surface-300">{{ studentDisplayName }}</strong>. </span>
            Update operational student status and record chronological audit transition.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 py-2" @submit.prevent="onSubmit">
      <!-- Target Status -->
      <FormField
        label="Operational Status"
        required
        :error="errors.status"
        description="Select the official target operational status."
      >
        <Select
          id="status-select"
          v-model="status"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          placeholder="Select operational status"
          class="w-full text-sm"
          :invalid="Boolean(errors.status)"
          data-testid="status-select"
        >
          <template #option="slotProps">
            <div class="py-0.5">
              <div class="flex items-center gap-2">
                <span class="font-semibold text-xs text-surface-800 dark:text-surface-100">
                  {{ slotProps.option.label }}
                </span>
                <span
                  class="text-[10px] uppercase font-bold px-1.5 py-0.5 rounded border"
                  :class="slotProps.option.badgeClass"
                >
                  {{ slotProps.option.value }}
                </span>
              </div>
              <div class="text-[11px] text-surface-500 mt-0.5">{{ slotProps.option.desc }}</div>
            </div>
          </template>
        </Select>
      </FormField>

      <!-- Effective Date -->
      <FormField
        label="Effective Date"
        required
        :error="errors.effectiveDate"
        description="Date from which this operational status takes effect (YYYY-MM-DD)."
      >
        <DatePicker
          id="status-effective-date"
          v-model="datePickerModel"
          date-format="yy-mm-dd"
          placeholder="YYYY-MM-DD"
          show-icon
          class="w-full text-sm"
          :invalid="Boolean(errors.effectiveDate)"
          data-testid="effective-date-picker"
          input-id="effective-date-input"
        />
      </FormField>

      <!-- Reason / Justification -->
      <FormField
        label="Adjustment Reason"
        :error="errors.reason"
        description="Authoritative operational rationale or decree justification."
      >
        <Textarea
          id="status-reason"
          v-model="reason"
          rows="3"
          placeholder="e.g. Cleared for regular class attendance following enrollment verification."
          class="w-full text-sm"
          :invalid="Boolean(errors.reason)"
          data-testid="reason-input"
        />
      </FormField>

      <!-- Authorizer & Decision Reference Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Authorized By -->
        <FormField
          label="Authorized By"
          :error="errors.authorizedBy"
          description="Officer or authority executing change."
        >
          <InputText
            id="status-authorized-by"
            v-model="authorizedBy"
            placeholder="e.g. Head of Student Records"
            class="w-full text-sm"
            :invalid="Boolean(errors.authorizedBy)"
            data-testid="authorized-by-input"
          />
        </FormField>

        <!-- Decision Reference -->
        <FormField
          label="Decision Reference"
          :error="errors.decisionReference"
          description="Formal memo, decree or SK number."
        >
          <InputText
            id="status-decision-reference"
            v-model="decisionReference"
            placeholder="e.g. SK-STATUS-2026-004"
            class="w-full text-sm"
            :invalid="Boolean(errors.decisionReference)"
            data-testid="decision-reference-input"
          />
        </FormField>
      </div>

      <!-- Footer Actions -->
      <div class="flex justify-end items-center gap-2 pt-4 border-t border-surface-200 dark:border-surface-700">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isPending"
          data-testid="btn-cancel-change-status"
          @click="handleClose"
        />
        <Button
          type="submit"
          label="Adjust Status"
          icon="pi pi-check"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white"
          :loading="isPending"
          data-testid="btn-submit-change-status"
        />
      </div>
    </form>
  </Dialog>
</template>
