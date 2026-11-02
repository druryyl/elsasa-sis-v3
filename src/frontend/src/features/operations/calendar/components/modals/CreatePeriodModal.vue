<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  Layers,
  Calendar,
  Sparkles,
  Clock,
  PlusCircle,
  School,
  GitFork,
  FileText,
} from 'lucide-vue-next';
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
  { label: 'Semester', value: 'semester', desc: 'Standard 6-month academic term', color: 'bg-emerald-500' },
  { label: 'Term', value: 'term', desc: 'Trimester or custom school term', color: 'bg-teal-500' },
  { label: 'Quarter', value: 'quarter', desc: '9-week quarterly division', color: 'bg-cyan-500' },
  { label: 'Admission Window', value: 'admission_window', desc: 'New student registration window (SPMB)', color: 'bg-indigo-500' },
  { label: 'Assessment Window', value: 'assessment_window', desc: 'Midterm or final examination window', color: 'bg-purple-500' },
  { label: 'Reporting Period', value: 'reporting_period', desc: 'Grade finalization and report card issuance', color: 'bg-amber-500' },
  { label: 'Event Period', value: 'event_period', desc: 'Extracurricular or orientation program', color: 'bg-sky-500' },
  { label: 'Custom Period', value: 'custom', desc: 'User-defined operational timeline', color: 'bg-slate-500' },
];

const PERIOD_PRESETS = [
  { label: 'Semester 1 (Ganjil)', code: 'SEM-1', type: 'semester' as const },
  { label: 'Semester 2 (Genap)', code: 'SEM-2', type: 'semester' as const },
  { label: 'Penerimaan Siswa Baru (SPMB)', code: 'ADM-SPMB', type: 'admission_window' as const },
  { label: 'Penilaian Tengah Semester (PTS)', code: 'PTS-1', type: 'assessment_window' as const },
];

const CAMPUS_SUGGESTIONS = ['All', 'Kampus Utama', 'Kampus 2', 'Primary Campus'];

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

const durationDays = computed(() => {
  if (!startDate.value || !endDate.value) return null;
  const start = new Date(startDate.value);
  const end = new Date(endDate.value);
  const diffTime = end.getTime() - start.getTime();
  const diff = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
  return diff;
});

function applyPeriodPreset(preset: typeof PERIOD_PRESETS[number]) {
  periodCode.value = preset.code;
  name.value = preset.label;
  periodType.value = preset.type;
}

function selectCampus(campus: string) {
  campusContext.value = campus;
}

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
    class="max-w-xl w-full"
    data-testid="create-period-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center shrink-0 text-teal-600 shadow-xs">
          <Layers class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">Create Operational Period</h3>
            <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-teal-50 text-teal-700 border border-teal-200">
              Period Scaffolding
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Establish a semester, term, examination window, or administrative cycle within this academic year.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Quick Preset Toolbar -->
      <div class="p-2.5 bg-slate-50/90 border border-slate-200/80 rounded-lg text-xs space-y-1.5 shadow-2xs">
        <div class="flex items-center gap-1.5 text-slate-700 font-medium">
          <Sparkles class="w-3.5 h-3.5 text-amber-500" />
          <span>Quick Period Presets:</span>
        </div>
        <div class="flex flex-wrap items-center gap-1.5">
          <button
            v-for="preset in PERIOD_PRESETS"
            :key="preset.code"
            type="button"
            class="px-2.5 py-1 text-xs font-medium rounded-md bg-white border border-slate-200 text-slate-700 hover:border-teal-400 hover:text-teal-700 hover:bg-teal-50/50 transition cursor-pointer shadow-2xs"
            @click="applyPeriodPreset(preset)"
          >
            {{ preset.label }}
          </button>
        </div>
      </div>

      <!-- Section 1: Period Identity & Classification Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="period-code"
            label="Period Code"
            required
            :error="errors.periodCode"
            helpText="Unique short code (e.g. SEM-1, ADM-2026)"
          >
            <InputText
              id="period-code"
              v-model="periodCode"
              placeholder="e.g. SEM-1"
              class="w-full text-sm font-mono tracking-wide"
              :class="{ 'p-invalid': errors.periodCode }"
              data-testid="period-code-input"
            />
          </FormField>

          <FormField
            id="period-type"
            label="Period Type"
            required
            :error="errors.periodType"
            helpText="Operational taxonomy"
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
            >
              <template #option="slotProps">
                <div class="flex items-start gap-2 py-0.5">
                  <span
                    class="w-2 h-2 rounded-full mt-1.5 shrink-0"
                    :class="slotProps.option.color"
                  />
                  <div>
                    <div class="text-xs font-medium text-slate-800">{{ slotProps.option.label }}</div>
                    <div class="text-[11px] text-slate-500">{{ slotProps.option.desc }}</div>
                  </div>
                </div>
              </template>
            </Select>
          </FormField>
        </div>

        <FormField
          id="period-name"
          label="Descriptive Name"
          required
          :error="errors.name"
          helpText="Display name for institutional calendars and report cards"
        >
          <InputText
            id="period-name"
            v-model="name"
            placeholder="e.g. Semester 1 (Ganjil)"
            class="w-full text-sm font-medium"
            :class="{ 'p-invalid': errors.name }"
            data-testid="period-name-input"
          />
        </FormField>
      </div>

      <!-- Section 2: Operational Schedule & Span Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center justify-between pb-1 border-b border-slate-100">
          <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wider">
            <Clock class="w-3.5 h-3.5 text-teal-600" />
            <span>Effective Period Span</span>
          </div>
          <div
            v-if="durationDays !== null"
            class="text-xs px-2 py-0.5 rounded font-medium"
            :class="durationDays >= 1 ? 'bg-teal-50 text-teal-700 border border-teal-200' : 'bg-rose-50 text-rose-700 border border-rose-200'"
          >
            <span v-if="durationDays >= 1">Duration: {{ durationDays }} Days (~{{ Math.round(durationDays / 30) }} mos)</span>
            <span v-else>⚠️ End date cannot precede Start date</span>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
          <FormField
            id="period-start-date"
            label="Start Date"
            required
            :error="errors.startDate"
            helpText="Beginning of operational period"
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
            helpText="Conclusion of operational period"
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
      </div>

      <!-- Section 3: Hierarchy & Campus Context Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3 shadow-2xs">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="parent-period"
            label="Parent Period (Hierarchy)"
            optional
            :error="errors.parentPeriodId"
            helpText="Nest this period under a parent semester or term"
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

          <div>
            <FormField
              id="campus-context"
              label="Campus Context"
              optional
              :error="errors.campusContext"
              helpText="Campus boundary (Default: All)"
            >
              <InputText
                id="campus-context"
                v-model="campusContext"
                placeholder="All"
                class="w-full text-sm"
                data-testid="campus-context-input"
              />
            </FormField>
            <div class="flex flex-wrap items-center gap-1.5 mt-2">
              <span class="text-[11px] text-slate-500 mr-0.5">Quick scope:</span>
              <button
                v-for="campus in CAMPUS_SUGGESTIONS"
                :key="campus"
                type="button"
                class="px-2 py-0.5 text-xs font-medium rounded border transition cursor-pointer"
                :class="campusContext === campus ? 'bg-teal-50 border-teal-300 text-teal-800' : 'bg-slate-50 border-slate-200 text-slate-600 hover:bg-slate-100'"
                @click="selectCampus(campus)"
              >
                {{ campus }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Section 4: Operational Notes -->
      <FormField
        id="period-notes"
        label="Operational Notes"
        optional
        :error="errors.notes"
        helpText="Administrative guidelines, enrollment restrictions, or policy references"
      >
        <Textarea
          id="period-notes"
          v-model="notes"
          rows="2"
          placeholder="Add any administrative instructions or policy context..."
          class="w-full text-sm resize-none"
          data-testid="period-notes-input"
        />
      </FormField>

      <!-- Modal Footer Actions -->
      <div class="flex items-center justify-between pt-3 border-t border-slate-200/80">
        <span class="text-xs text-slate-500 flex items-center gap-1">
          <span class="text-rose-500 font-bold">*</span> Required fields
        </span>
        <div class="flex items-center gap-2">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            class="text-slate-600 hover:text-slate-800"
            data-testid="modal-cancel-btn"
            @click="isDialogVisible = false"
          />
          <Button
            type="submit"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white shadow-xs px-4"
            :loading="createMutation.isPending.value"
            data-testid="modal-submit-create-period-btn"
            @click="onSubmit"
          >
            <Layers class="w-4 h-4 mr-1.5" />
            <span>Create Period</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
