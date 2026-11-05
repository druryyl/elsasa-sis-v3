<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  Users,
  BookOpen,
  Calendar,
  Layers,
  CheckCircle2,
  AlertCircle,
  Search,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  batchRegisterCohortSchema,
  type BatchRegisterCohortFormValues,
} from '../schemas/participation.schema';
import { useBatchRegisterCohortMutation } from '../composables/useAcademicParticipation';
import type { BatchRegisterCohortResponse } from '../types';

export interface AcademicPeriodOption {
  id: string;
  label: string;
  code?: string;
}

export interface LearningUnitOption {
  id: string;
  code: string;
  name: string;
  level?: string | null;
  creditWeight?: number | null;
}

const props = defineProps<{
  visible: boolean;
  periods: AcademicPeriodOption[];
  units: LearningUnitOption[];
  initialPeriodId?: string | null;
  initialSectionLabel?: string | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'enrolled', response: BatchRegisterCohortResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const todayIso = new Date().toISOString().slice(0, 10);
const unitSearchFilter = ref('');
const isPreviewConfirmed = ref(false);

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<BatchRegisterCohortFormValues>({
    validationSchema: toTypedSchema(batchRegisterCohortSchema),
    initialValues: {
      academicPeriodId: props.initialPeriodId ?? '',
      academicPeriodLabel: '',
      learningUnitIds: [],
      sectionLabel: props.initialSectionLabel ?? '',
      startDate: todayIso,
      studentIds: undefined,
    },
  });

const [academicPeriodId] = defineField('academicPeriodId');
const [academicPeriodLabel] = defineField('academicPeriodLabel');
const [learningUnitIds] = defineField('learningUnitIds');
const [sectionLabel] = defineField('sectionLabel');
const [startDate] = defineField('startDate');

// Update period label when period ID changes
watch(
  academicPeriodId,
  (newId) => {
    const selected = props.periods.find((p) => p.id === newId);
    if (selected) {
      setFieldValue('academicPeriodLabel', selected.label);
    }
  },
  { immediate: true },
);

// Reset form when dialog opens
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      const defaultPeriod =
        props.periods.find((p) => p.id === props.initialPeriodId) ??
        props.periods[0] ??
        null;

      resetForm({
        values: {
          academicPeriodId: defaultPeriod?.id ?? '',
          academicPeriodLabel: defaultPeriod?.label ?? '',
          learningUnitIds: [],
          sectionLabel: props.initialSectionLabel ?? '',
          startDate: todayIso,
          studentIds: undefined,
        },
      });
      unitSearchFilter.value = '';
      isPreviewConfirmed.value = false;
    }
  },
);

const filteredUnits = computed(() => {
  const query = unitSearchFilter.value.trim().toLowerCase();
  if (!query) return props.units;
  return props.units.filter(
    (u) =>
      u.name.toLowerCase().includes(query) ||
      u.code.toLowerCase().includes(query),
  );
});

const isAllUnitsSelected = computed(() => {
  if (props.units.length === 0) return false;
  const current = learningUnitIds.value ?? [];
  return props.units.every((u) => current.includes(u.id));
});

function toggleSelectAllUnits() {
  if (isAllUnitsSelected.value) {
    setFieldValue('learningUnitIds', []);
  } else {
    setFieldValue(
      'learningUnitIds',
      props.units.map((u) => u.id),
    );
  }
}

const batchMutation = useBatchRegisterCohortMutation();
const isSubmitting = computed(() => batchMutation.isPending.value);

const selectedUnitNames = computed(() => {
  const ids = new Set(learningUnitIds.value ?? []);
  return props.units.filter((u) => ids.has(u.id));
});

const onSubmit = handleSubmit(async (values) => {
  try {
    const result = await batchMutation.mutateAsync({
      academicPeriodId: values.academicPeriodId,
      academicPeriodLabel: values.academicPeriodLabel,
      learningUnitIds: values.learningUnitIds,
      sectionLabel: values.sectionLabel.trim(),
      startDate: values.startDate,
      studentIds: values.studentIds && values.studentIds.length > 0 ? values.studentIds : undefined,
    });

    emit('enrolled', result);
    isDialogVisible.value = false;
  } catch {
    // Errors handled in mutation toast
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Batch Enroll Cohort"
    :style="{ width: '680px' }"
    :closable="!isSubmitting"
  >
    <form class="space-y-4" @submit.prevent="onSubmit">
      <!-- Cohort Registration Description -->
      <div
        class="flex items-start gap-3 rounded-lg border border-primary-100 bg-primary-50/60 p-3 text-xs text-primary-900 dark:border-primary-900/40 dark:bg-primary-950/20 dark:text-primary-200"
      >
        <Users class="size-4 shrink-0 text-primary-600 dark:text-primary-400" />
        <p class="leading-relaxed">
          Batch cohort enrollment registers all active students in the specified section into the chosen learning units for the term (UC-ACD-10, GAP-003). Administrative active enrollment is verified automatically.
        </p>
      </div>

      <!-- Top Form Inputs: Period & Section -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Academic Period"
          required
          :error="errors.academicPeriodId"
          help-text="Target academic operational term"
        >
          <Select
            v-model="academicPeriodId"
            :options="periods"
            option-label="label"
            option-value="id"
            placeholder="Select Academic Period"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Cohort Section Label"
          required
          :error="errors.sectionLabel"
          help-text="Cohort partition (e.g. 10-A, Grade 7-B)"
        >
          <InputText
            v-model="sectionLabel"
            placeholder="e.g. 10-A"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>

      <!-- Start Date Input -->
      <FormField
        label="Participation Start Date"
        required
        :error="errors.startDate"
        help-text="Official instruction commencement date (YYYY-MM-DD)"
      >
        <InputText
          v-model="startDate"
          type="date"
          class="w-full"
          :disabled="isSubmitting"
        />
      </FormField>

      <!-- Learning Units Multi-Selection -->
      <div class="space-y-2">
        <div class="flex items-center justify-between">
          <label class="block text-sm font-medium text-surface-900 dark:text-surface-100">
            Learning Units <span class="text-rose-500">*</span>
          </label>
          <Button
            type="button"
            size="small"
            variant="text"
            :label="isAllUnitsSelected ? 'Deselect All' : 'Select All Units'"
            class="p-0 text-xs text-primary-600 dark:text-primary-400"
            @click="toggleSelectAllUnits"
          />
        </div>

        <!-- Filter box -->
        <div class="relative">
          <Search class="absolute left-3 top-2.5 size-4 text-surface-400" />
          <InputText
            v-model="unitSearchFilter"
            placeholder="Search learning units by code or title..."
            class="w-full pl-9 text-xs"
            :disabled="isSubmitting"
          />
        </div>

        <div
          class="max-h-52 overflow-y-auto rounded-lg border border-surface-200 bg-surface-50 p-2 dark:border-surface-700 dark:bg-surface-900/60"
        >
          <div
            v-if="filteredUnits.length === 0"
            class="py-4 text-center text-xs text-surface-500"
          >
            No learning units available.
          </div>
          <div v-else class="space-y-1">
            <label
              v-for="unit in filteredUnits"
              :key="unit.id"
              class="flex cursor-pointer items-center justify-between rounded p-2 text-xs hover:bg-surface-200/60 dark:hover:bg-surface-800"
            >
              <div class="flex items-center gap-2">
                <Checkbox
                  v-model="learningUnitIds"
                  :value="unit.id"
                  :disabled="isSubmitting"
                />
                <span class="font-semibold text-surface-800 dark:text-surface-200">
                  {{ unit.code }}
                </span>
                <span class="text-surface-600 dark:text-surface-400">— {{ unit.name }}</span>
              </div>
              <span
                v-if="unit.creditWeight"
                class="rounded bg-surface-200 px-1.5 py-0.5 text-[10px] font-medium text-surface-700 dark:bg-surface-800 dark:text-surface-300"
              >
                {{ unit.creditWeight }} cr
              </span>
            </label>
          </div>
        </div>
        <p v-if="errors.learningUnitIds" class="text-xs text-rose-500">
          {{ errors.learningUnitIds }}
        </p>
        <p class="text-[11px] text-surface-500">
          Selected: {{ (learningUnitIds ?? []).length }} unit(s)
        </p>
      </div>

      <!-- Preview Summary Card -->
      <div
        v-if="sectionLabel && (learningUnitIds ?? []).length > 0"
        class="rounded-lg border border-surface-300 bg-surface-100 p-3 text-xs dark:border-surface-700 dark:bg-surface-800/80"
      >
        <div class="flex items-center gap-1.5 font-semibold text-surface-900 dark:text-surface-100">
          <CheckCircle2 class="size-4 text-teal-600 dark:text-teal-400" />
          <span>Batch Registration Preview</span>
        </div>
        <p class="mt-1 text-surface-600 dark:text-surface-300">
          Enrolling active students of Section <strong>{{ sectionLabel }}</strong> into <strong>{{ selectedUnitNames.length }} unit(s)</strong> for <em>{{ academicPeriodLabel || 'Selected Period' }}</em> beginning <strong>{{ startDate }}</strong>.
        </p>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="text"
          :disabled="isSubmitting"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          label="Submit Cohort Enrollment"
          icon="pi pi-users"
          :loading="isSubmitting"
          :disabled="(learningUnitIds ?? []).length === 0"
        />
      </div>
    </form>
  </Dialog>
</template>
