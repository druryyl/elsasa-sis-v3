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
    :style="{ width: '600px' }"
    :breakpoints="{ '960px': '75vw', '640px': '95vw' }"
    data-testid="change-status-modal"
  >
    <template #header>
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-xs">
          <Activity class="w-5 h-5" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2.5">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Adjust Operational Status
            </h3>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800">
              TD-05
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            <span v-if="studentDisplayName">Target student: <strong class="text-surface-700 dark:text-surface-300">{{ studentDisplayName }}</strong>. </span>
            Update operational student status and record chronological audit transition.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-5 pt-3 pb-1" @submit.prevent="onSubmit">
      <!-- Current vs Target Status Context Banner -->
      <div class="p-3.5 rounded-xl border border-teal-200 dark:border-teal-800/80 bg-teal-50/60 dark:bg-teal-950/30 text-xs">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
          <div class="flex items-center gap-2 text-surface-600 dark:text-surface-300">
            <span class="font-medium text-surface-500">Current Status:</span>
            <span class="inline-flex items-center px-2 py-0.5 rounded-md font-semibold text-xs capitalize bg-surface-100 dark:bg-surface-800 text-surface-800 dark:text-surface-200 border border-surface-300 dark:border-surface-700">
              {{ currentStatus || (student && 'currentStatus' in student ? student.currentStatus : '') || 'enrolled' }}
            </span>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-surface-400 text-xs">➔ Next Target:</span>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-md font-bold text-xs uppercase bg-teal-100 dark:bg-teal-900/60 text-teal-800 dark:text-teal-200 border border-teal-300 dark:border-teal-700">
              {{ status || 'active' }}
            </span>
          </div>
        </div>
      </div>

      <!-- Section 1: Transition Specification -->
      <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <Activity class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Operational Status Parameters</span>
        </div>

        <!-- Target Status -->
        <FormField
          label="Target Operational Status"
          required
          :error="errors.status"
          description="Select the official target operational status according to administrative decree."
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
              <div class="py-1">
                <div class="flex items-center gap-2">
                  <span class="font-semibold text-xs text-surface-800 dark:text-surface-100">
                    {{ slotProps.option.label }}
                  </span>
                  <span
                    class="text-[11px] uppercase font-bold px-1.5 py-0.5 rounded border"
                    :class="slotProps.option.badgeClass"
                  >
                    {{ slotProps.option.value }}
                  </span>
                </div>
                <div class="text-xs text-surface-500 mt-1 leading-normal">{{ slotProps.option.desc }}</div>
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
      </div>

      <!-- Section 2: Administrative Governance & Authorization -->
      <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <ShieldAlert class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Governance &amp; Authorization Rationale</span>
        </div>

        <!-- Reason / Justification -->
        <FormField
          label="Adjustment Reason &amp; Justification"
          :error="errors.reason"
          description="Authoritative operational rationale or decree justification."
        >
          <Textarea
            id="status-reason"
            v-model="reason"
            rows="3"
            placeholder="e.g. Cleared for regular class attendance following enrollment verification and tuition settlement."
            class="w-full text-sm leading-relaxed"
            :invalid="Boolean(errors.reason)"
            data-testid="reason-input"
          />
        </FormField>

        <!-- Authorizer & Decision Reference Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
            description="Formal decree, memo or decree SK number."
          >
            <InputText
              id="status-decision-reference"
              v-model="decisionReference"
              placeholder="e.g. SK-STATUS-2026-004"
              class="w-full text-sm font-mono"
              :invalid="Boolean(errors.decisionReference)"
              data-testid="decision-reference-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="flex justify-end items-center gap-3 pt-4 border-t border-surface-200 dark:border-surface-800">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="isPending"
          data-testid="btn-cancel-change-status"
          class="!px-4 !py-2 !text-xs font-medium cursor-pointer"
          @click="handleClose"
        />
        <Button
          type="submit"
          label="Confirm Status Adjustment"
          icon="pi pi-check"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs"
          :loading="isPending"
          data-testid="btn-submit-change-status"
        />
      </div>
    </form>
  </Dialog>
</template>
