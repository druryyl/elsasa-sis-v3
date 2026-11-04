<script setup lang="ts">
import { computed, watch, ref } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  FileText,
  Calendar,
  DollarSign,
  PlusCircle,
  User,
  GraduationCap,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import { formatIdr } from '../../shared/currency.utils';
import { assessAdHocObligationSchema, type AssessAdHocObligationSchemaInput } from '../../obligations/schemas/obligations.schema';
import { useAdHocAssessMutation } from '../../obligations/composables/useFinanceObligations';
import { useAcademicYearsQuery } from '@/features/operations/calendar/composables/useAcademicYearsQuery';
import { useOperationalPeriodsQuery } from '@/features/operations/calendar/composables/useOperationalPeriodsQuery';
import type { AssessAdHocObligationResponse } from '../../obligations/types/obligations.types';

interface Props {
  visible: boolean;
  studentId: string;
  studentName?: string | null;
  studentNumber?: string | null;
  defaultAcademicPeriodId?: string;
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  studentId: '',
  studentName: '',
  studentNumber: '',
  defaultAcademicPeriodId: '',
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'assessed', response: AssessAdHocObligationResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Academic year & period resolution
const selectedAcademicYearId = ref<string>('');
const { data: academicYears } = useAcademicYearsQuery();

watch(
  () => academicYears.value,
  (years) => {
    if (years && years.length > 0 && !selectedAcademicYearId.value) {
      const activeYear = years.find((y) => y.status === 'active') ?? years[0];
      selectedAcademicYearId.value = activeYear.id;
    }
  },
  { immediate: true },
);

const { data: operationalPeriods } = useOperationalPeriodsQuery(
  selectedAcademicYearId,
  {
    enabled: computed(() => Boolean(selectedAcademicYearId.value)),
  },
);

const periodOptions = computed(() => {
  if (!operationalPeriods.value || operationalPeriods.value.length === 0) {
    if (props.defaultAcademicPeriodId) {
      return [{ label: 'Active Period', value: props.defaultAcademicPeriodId }];
    }
    return [];
  }
  return operationalPeriods.value.map((p) => ({
    label: `${p.name} (${p.academicYearName ?? ''})`,
    value: p.id,
  }));
});

// Common charge types
const chargeTypeOptions = [
  { label: 'General / Miscellaneous (Umum)', value: 'general' },
  { label: 'Monthly Tuition (SPP Bulanan)', value: 'tuition' },
  { label: 'Admission Fee (Biaya Pendaftaran)', value: 'admission_fee' },
  { label: 'Enrollment Fee (Uang Pangkal)', value: 'enrollment_fee' },
  { label: 'Library / Late Fine (Denda Perpustakaan)', value: 'library_fee' },
  { label: 'Card Replacement (Kartu Pelajar)', value: 'lost_id_card' },
  { label: 'Uniform & Materials (Seragam & Perlengkapan)', value: 'uniform' },
  { label: 'Textbooks (Buku Pelajaran)', value: 'books' },
  { label: 'Extracurricular (Ekstrakurikuler)', value: 'extracurricular' },
  { label: 'Field Trip (Study Tour)', value: 'field_trip' },
];

// Calculate default due date (14 days from today)
function getDefaultDueDate(): string {
  const d = new Date();
  d.setDate(d.getDate() + 14);
  return formatDateOnly(d);
}

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<AssessAdHocObligationSchemaInput>({
    validationSchema: toTypedSchema(assessAdHocObligationSchema),
    initialValues: {
      studentId: props.studentId,
      chargeType: 'general',
      description: '',
      amount: 100000,
      dueDate: getDefaultDueDate(),
      academicPeriodId: props.defaultAcademicPeriodId || '',
      notes: '',
    },
  });

const [studentId] = defineField('studentId');
const [chargeType] = defineField('chargeType');
const [description] = defineField('description');
const [amount] = defineField('amount');
const [dueDate] = defineField('dueDate');
const [academicPeriodId] = defineField('academicPeriodId');
const [notes] = defineField('notes');

// Keep studentId and default academic period synchronized
watch(
  () => props.studentId,
  (newId) => {
    if (newId) {
      setFieldValue('studentId', newId);
    }
  },
  { immediate: true },
);

watch(
  () => [props.defaultAcademicPeriodId, periodOptions.value],
  ([defaultPeriod, options]) => {
    if (defaultPeriod && !academicPeriodId.value) {
      setFieldValue('academicPeriodId', defaultPeriod);
    } else if (options && options.length > 0 && !academicPeriodId.value) {
      setFieldValue('academicPeriodId', options[0].value);
    }
  },
  { immediate: true },
);

// Reset form when modal opens
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          studentId: props.studentId,
          chargeType: 'general',
          description: '',
          amount: 100000,
          dueDate: getDefaultDueDate(),
          academicPeriodId:
            props.defaultAcademicPeriodId ||
            (periodOptions.value.length > 0 ? periodOptions.value[0].value : ''),
          notes: '',
        },
      });
    }
  },
  { immediate: true },
);

const { mutateAsync: assessAdHoc, isPending: isAssessing } = useAdHocAssessMutation();

const onSubmit = handleSubmit(async (values) => {
  try {
    const payload: AssessAdHocObligationSchemaInput = {
      ...values,
      amount: Math.round(Number(values.amount)),
    };
    const response = await assessAdHoc(payload);
    emit('assessed', response);
    isDialogVisible.value = false;
  } catch {
    // Handled by mutation toast notification
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    header="Assess Ad-Hoc Student Fee"
    class="w-full max-w-xl mx-4"
    data-testid="assess-ad-hoc-fee-modal"
  >
    <!-- Student Header Summary -->
    <div class="mb-5 p-3.5 bg-slate-50 border border-slate-200 rounded-lg flex items-center justify-between">
      <div class="flex items-center gap-3">
        <div class="w-9 h-9 rounded-full bg-indigo-100 text-indigo-700 flex items-center justify-center font-bold text-sm">
          <User class="w-4 h-4" />
        </div>
        <div>
          <div class="font-semibold text-slate-900 text-sm" data-testid="modal-student-name">
            {{ studentName || 'Selected Student' }}
          </div>
          <div class="text-xs text-slate-500 font-mono">
            {{ studentNumber ? `NIS: ${studentNumber}` : studentId }}
          </div>
        </div>
      </div>
      <span class="text-xs font-medium px-2 py-0.5 rounded bg-indigo-50 text-indigo-700 border border-indigo-100">
        Single Assessment
      </span>
    </div>

    <form @submit.prevent="onSubmit" class="space-y-4">
      <!-- Charge Type & Academic Period -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField label="Charge Type" required :error="errors.chargeType">
          <Select
            v-model="chargeType"
            :options="chargeTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select charge type"
            class="w-full"
            data-testid="ad-hoc-charge-type-select"
          />
        </FormField>

        <FormField label="Academic Period" required :error="errors.academicPeriodId">
          <Select
            v-model="academicPeriodId"
            :options="periodOptions"
            option-label="label"
            option-value="value"
            placeholder="Select academic period"
            class="w-full"
            data-testid="ad-hoc-period-select"
          />
        </FormField>
      </div>

      <!-- Description -->
      <FormField label="Fee Description" required :error="errors.description">
        <InputText
          v-model="description"
          placeholder="e.g., Penggantian Kartu Pelajar Hilang"
          class="w-full"
          maxlength="255"
          data-testid="ad-hoc-description-input"
        />
      </FormField>

      <!-- Amount in IDR & Due Date -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField label="Amount (IDR)" required :error="errors.amount">
          <div class="relative">
            <span class="absolute left-3 top-1/2 -translate-y-1/2 text-xs font-bold text-slate-400">
              Rp
            </span>
            <InputText
              v-model.number="amount"
              type="number"
              min="1"
              step="1"
              placeholder="100000"
              class="w-full pl-9!"
              data-testid="ad-hoc-amount-input"
            />
          </div>
          <p class="text-xs text-slate-500 mt-1">
            Formatted: <span class="font-mono font-semibold text-slate-800">{{ formatIdr(amount) }}</span>
          </p>
        </FormField>

        <FormField label="Payment Due Date" required :error="errors.dueDate">
          <InputText
            v-model="dueDate"
            type="date"
            class="w-full"
            data-testid="ad-hoc-due-date-input"
          />
        </FormField>
      </div>

      <!-- Notes -->
      <FormField label="Internal Notes (Optional)" :error="errors.notes">
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Reason or authorization reference..."
          class="w-full text-sm"
          maxlength="2000"
          data-testid="ad-hoc-notes-input"
        />
      </FormField>

      <!-- Actions -->
      <div class="pt-4 border-t border-slate-200 flex items-center justify-end gap-2.5">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          outlined
          :disabled="isAssessing"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          label="Assess Fee"
          icon="pi pi-check"
          :loading="isAssessing"
          severity="primary"
          data-testid="submit-ad-hoc-fee-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
