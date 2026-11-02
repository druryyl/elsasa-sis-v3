<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  createOperationalPeriodSchema,
  type CreateOperationalPeriodFormData,
} from '../../schemas/operationalPeriod.schema';
import {
  useCreateOperationalPeriodMutation,
} from '../../composables/useOperationalPeriodsQuery';
import type { OperationalPeriodDto } from '../../api/operationalPeriod.api';

const props = defineProps<{
  visible: boolean;
  academicYearId?: string | null | undefined;
  periods?: OperationalPeriodDto[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', period: OperationalPeriodDto): void;
}>();

const periodTypeOptions = [
  { label: 'Semester', value: 'semester' },
  { label: 'Term', value: 'term' },
  { label: 'Quarter', value: 'quarter' },
  { label: 'Admission Window', value: 'admission_window' },
  { label: 'Assessment Window', value: 'assessment_window' },
  { label: 'Reporting Period', value: 'reporting_period' },
  { label: 'Event Period', value: 'event_period' },
  { label: 'Custom Period', value: 'custom' },
];

const parentOptions = computed(() => [
  { label: 'None (Top-Level Root Period)', value: '' },
  ...(props.periods ?? []).map((p) => ({
    label: `${p.periodCode} — ${p.name}`,
    value: p.id,
  })),
]);

const { handleSubmit, errors, defineField, resetForm } =
  useForm<CreateOperationalPeriodFormData>({
    validationSchema: toTypedSchema(createOperationalPeriodSchema),
    initialValues: {
      periodCode: '',
      name: '',
      periodType: 'semester',
      startDate: '',
      endDate: '',
      parentPeriodId: '',
      campusContext: 'All',
      notes: '',
    },
  });

const [periodCode] = defineField('periodCode');
const [name] = defineField('name');
const [periodType] = defineField('periodType');
const [startDate] = defineField('startDate');
const [endDate] = defineField('endDate');
const [parentPeriodId] = defineField('parentPeriodId');
const [campusContext] = defineField('campusContext');
const [notes] = defineField('notes');

// Reset form when dialog opens
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          periodCode: '',
          name: '',
          periodType: 'semester',
          startDate: '',
          endDate: '',
          parentPeriodId: '',
          campusContext: 'All',
          notes: '',
        },
      });
    }
  },
);

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const createMutation = useCreateOperationalPeriodMutation({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('created', data);
  },
});

const onSubmit = handleSubmit(
  (values) => {
    if (!props.academicYearId) return;

    createMutation.mutate({
      academicYearId: props.academicYearId,
      payload: {
        periodCode: values.periodCode.trim(),
        name: values.name.trim(),
        periodType: values.periodType,
        startDate: values.startDate,
        endDate: values.endDate,
        parentPeriodId:
          values.parentPeriodId && values.parentPeriodId.trim().length > 0
            ? values.parentPeriodId
            : null,
        campusContext: values.campusContext?.trim() || 'All',
        notes: values.notes?.trim() || null,
      },
    });
  },
);

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Create Operational Period"
    modal
    class="max-w-lg w-full"
    data-testid="create-period-modal"
  >
    <form class="space-y-4 pt-2" @submit.prevent="onSubmit">
      <!-- Period Code & Name -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="period-code"
          label="Period Code"
          required
          :error="errors.periodCode"
          hint="e.g. SEM-1, ADM-2026"
        >
          <InputText
            id="period-code"
            v-model="periodCode"
            placeholder="SEM-1"
            class="w-full text-sm font-mono"
            :class="{ 'p-invalid': errors.periodCode }"
            data-testid="period-code-input"
          />
        </FormField>

        <FormField
          id="period-type"
          label="Period Type"
          required
          :error="errors.periodType"
        >
          <Select
            id="period-type"
            v-model="periodType"
            :options="periodTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select period type"
            class="w-full text-sm"
            data-testid="period-type-select"
          />
        </FormField>
      </div>

      <FormField
        id="period-name"
        label="Descriptive Name"
        required
        :error="errors.name"
        hint="e.g. Semester 1 (Ganjil)"
      >
        <InputText
          id="period-name"
          v-model="name"
          placeholder="Semester 1 (Ganjil)"
          class="w-full text-sm"
          :class="{ 'p-invalid': errors.name }"
          data-testid="period-name-input"
        />
      </FormField>

      <!-- Date Range -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="period-start-date"
          label="Start Date"
          required
          :error="errors.startDate"
        >
          <InputText
            id="period-start-date"
            v-model="startDate"
            type="date"
            class="w-full text-sm"
            :class="{ 'p-invalid': errors.startDate }"
            data-testid="period-start-date-input"
          />
        </FormField>

        <FormField
          id="period-end-date"
          label="End Date"
          required
          :error="errors.endDate"
        >
          <InputText
            id="period-end-date"
            v-model="endDate"
            type="date"
            class="w-full text-sm"
            :class="{ 'p-invalid': errors.endDate }"
            data-testid="period-end-date-input"
          />
        </FormField>
      </div>

      <!-- Parent Period & Campus Context -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="parent-period"
          label="Parent Period"
          optional
          :error="errors.parentPeriodId"
          hint="Select parent for nested terms/windows"
        >
          <Select
            id="parent-period"
            v-model="parentPeriodId"
            :options="parentOptions"
            option-label="label"
            option-value="value"
            placeholder="None (Root Period)"
            class="w-full text-sm"
            data-testid="parent-period-select"
          />
        </FormField>

        <FormField
          id="campus-context"
          label="Campus Context"
          optional
          :error="errors.campusContext"
          hint="Default: All"
        >
          <InputText
            id="campus-context"
            v-model="campusContext"
            placeholder="All"
            class="w-full text-sm"
            data-testid="campus-context-input"
          />
        </FormField>
      </div>

      <!-- Internal Notes -->
      <FormField
        id="period-notes"
        label="Operational Notes"
        optional
        :error="errors.notes"
      >
        <Textarea
          id="period-notes"
          v-model="notes"
          rows="2"
          placeholder="Optional notes or administrative instructions..."
          class="w-full text-sm"
          data-testid="period-notes-input"
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
          label="Create Period"
          class="bg-teal-600 hover:bg-teal-700 text-white"
          :loading="createMutation.isPending.value"
          data-testid="modal-submit-create-period-btn"
          @click="onSubmit"
        />
      </div>
    </form>
  </Dialog>
</template>
