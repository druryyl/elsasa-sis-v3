<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import InputNumber from 'primevue/inputnumber';
import ToggleSwitch from 'primevue/toggleswitch';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  useLearningStructures,
  useLearningUnits,
} from '@/features/academics/curriculum/composables/useCurriculum';
import {
  addUnitSchema,
  type AddUnitFormValues,
} from '../schemas/learningPlan.schema';
import { useAddUnitMutation } from '../composables/useLearningPlans';

export interface LearningUnitOption {
  id: string;
  code: string;
  name?: string;
  title?: string;
  level?: string | null;
  status?: string;
  unitType?: string;
}

export interface AddSlpUnitModalProps {
  visible: boolean;
  planId: string;
  units?: LearningUnitOption[];
}

const props = withDefaults(defineProps<AddSlpUnitModalProps>(), {
  units: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'added', unitId: string): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Learning Units Options Query (Fallback if not provided via props)
// ---------------------------------------------------------------------------
const { data: structuresData, isLoading: isLoadingStructures } = useLearningStructures(undefined, {
  enabled: computed(() => isDialogVisible.value && (!props.units || props.units.length === 0)),
});

const activeStructureId = computed(() => {
  const list = Array.isArray(structuresData.value)
    ? structuresData.value
    : structuresData.value?.items ?? [];
  if (list.length === 0) return null;
  const active = list.find((s) => s.status === 'active');
  return active?.id ?? list[0]?.id ?? null;
});

const { data: queriedUnits, isLoading: isLoadingUnits } = useLearningUnits(activeStructureId, undefined, {
  enabled: computed(() => isDialogVisible.value && (!props.units || props.units.length === 0) && Boolean(activeStructureId.value)),
});

const isLoadingUnitsData = computed(
  () =>
    (!props.units || props.units.length === 0) &&
    (isLoadingStructures.value || isLoadingUnits.value),
);

const unitDropdownOptions = computed(() => {
  const source = props.units && props.units.length > 0
    ? props.units
    : (queriedUnits.value ?? []);

  return source
    .filter((u: any) => !u.status || u.status === 'active')
    .map((u: any) => ({
      id: u.id,
      code: u.code,
      name: u.name || u.title || '',
      label: `${u.code} — ${u.name || u.title || ''}`,
      value: u.id,
    }));
});

// ---------------------------------------------------------------------------
// 2. Form Setup & Validation
// ---------------------------------------------------------------------------
const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<AddUnitFormValues>({
    validationSchema: toTypedSchema(addUnitSchema),
    initialValues: {
      learningUnitId: '',
      sequenceOrder: null,
      isRequired: true,
      notes: '',
    },
  });

const [learningUnitId] = defineField('learningUnitId');
const [sequenceOrder] = defineField('sequenceOrder');
const [isRequired] = defineField('isRequired');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          learningUnitId: '',
          sequenceOrder: null,
          isRequired: true,
          notes: '',
        },
      });
    }
  },
);

// ---------------------------------------------------------------------------
// 3. Mutation & Submit Handler
// ---------------------------------------------------------------------------
const addMutation = useAddUnitMutation({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('added', data.id);
    resetForm();
  },
});

const onSubmit = handleSubmit((values) => {
  if (!props.planId) return;

  const seq =
    values.sequenceOrder != null && values.sequenceOrder !== ''
      ? Number(values.sequenceOrder)
      : null;

  addMutation.mutate({
    planId: props.planId,
    request: {
      learningUnitId: values.learningUnitId,
      sequenceOrder: seq,
      isRequired: values.isRequired ?? true,
      notes: values.notes?.trim() || null,
    },
  });
});

function handleClose() {
  emit('update:visible', false);
  resetForm();
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Add Constituent Learning Unit"
    :style="{ width: '560px', maxWidth: '95vw' }"
    class="p-dialog-sm"
    data-testid="add-slp-unit-modal"
  >
    <form class="space-y-4 pt-2" @submit.prevent="onSubmit">
      <!-- 1. Learning Unit Dropdown -->
      <FormField
        id="slp-unit-select"
        label="Learning Unit"
        required
        :error="errors.learningUnitId"
        help-text="Select an active curriculum learning unit to link to this individualized plan."
      >
        <Select
          id="slp-unit-select"
          v-model="learningUnitId"
          :options="unitDropdownOptions"
          option-label="label"
          option-value="value"
          filter
          filter-placeholder="Search by unit code or title..."
          placeholder="Select a learning unit..."
          :loading="isLoadingUnitsData"
          :invalid="Boolean(errors.learningUnitId)"
          class="w-full text-xs"
          data-testid="slp-unit-select"
        >
          <template #option="{ option }">
            <div class="flex items-center justify-between w-full py-0.5">
              <span class="font-medium text-xs">{{ option.name }}</span>
              <span class="font-mono text-[11px] text-slate-500 bg-slate-100 dark:bg-slate-800 px-1.5 py-0.5 rounded">
                {{ option.code }}
              </span>
            </div>
          </template>
        </Select>
      </FormField>

      <!-- 2. Sequence Order Input -->
      <FormField
        id="slp-sequence-order"
        label="Sequence Order"
        optional
        :error="errors.sequenceOrder"
        help-text="Recommended sequence or term order (e.g. 1, 2, 3). Leave blank if unsequenced."
      >
        <InputNumber
          id="slp-sequence-order"
          v-model="sequenceOrder"
          :min="1"
          :max="999"
          placeholder="e.g. 1"
          class="w-full text-xs"
          :invalid="Boolean(errors.sequenceOrder)"
          data-testid="slp-sequence-order-input"
        />
      </FormField>

      <!-- 3. Is Required Switch -->
      <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-200 dark:border-slate-700 flex items-center justify-between gap-4">
        <div class="space-y-0.5">
          <label for="slp-is-required-toggle" class="text-xs font-semibold text-slate-800 dark:text-slate-200 cursor-pointer">
            {{ isRequired ? 'Mandatory Core Requirement' : 'Elective Course Unit' }}
          </label>
          <p class="text-[11px] text-slate-500 dark:text-slate-400">
            {{ isRequired ? 'This unit is required for the student to achieve their learning plan objectives.' : 'This unit is an elective enrichment or optional component.' }}
          </p>
        </div>
        <ToggleSwitch
          id="slp-is-required-toggle"
          v-model="isRequired"
          data-testid="slp-is-required-switch"
        />
      </div>

      <!-- 4. Adaptation / Level Notes Textarea -->
      <FormField
        id="slp-unit-notes"
        label="Adaptation / Level Notes"
        optional
        :error="errors.notes"
        help-text="Explain remediation rationale, grade-level adaptation, or adaptive pacing arrangements."
      >
        <Textarea
          id="slp-unit-notes"
          v-model="notes"
          rows="3"
          placeholder="e.g., Remedial pacing at Grade 3 level while administratively placed in Grade 4, or adaptive curriculum modifications..."
          class="w-full text-xs"
          :invalid="Boolean(errors.notes)"
          data-testid="slp-unit-notes-input"
        />
      </FormField>

      <!-- Dialog Footer Actions -->
      <div class="pt-3 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end gap-2.5">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="text"
          size="small"
          class="text-xs"
          @click="handleClose"
        />
        <Button
          type="submit"
          label="Add Learning Unit"
          icon="pi pi-plus"
          size="small"
          :loading="addMutation.isPending.value || isSubmitting"
          class="text-xs font-semibold"
          data-testid="submit-add-unit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
