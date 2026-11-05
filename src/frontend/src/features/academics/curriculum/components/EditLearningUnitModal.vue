<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import InputNumber from 'primevue/inputnumber';
import { AlertTriangle, Lock } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  updateLearningUnitSchema,
  LEARNING_UNIT_TYPES,
  type UpdateLearningUnitFormData,
} from '../schemas/curriculum.schemas';
import { useUpdateUnit } from '../composables/useCurriculum';
import type { LearningUnit, LearningUnitType } from '../types/curriculum.types';

const props = defineProps<{
  visible: boolean;
  structureId?: string | null | undefined;
  unit?: LearningUnit | null | undefined;
  isStructureActive?: boolean | undefined;
  rootUnits?: LearningUnit[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'updated', unit: LearningUnit): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Dropdown options filter exclusively to root units (excluding the current unit itself to prevent self-parenting)
const parentDropdownOptions = computed(() => [
  { label: 'None (Root Learning Unit)', value: null },
  ...(props.rootUnits ?? [])
    .filter((u) => !u.parentUnitId && u.id !== props.unit?.id)
    .map((u) => ({
      label: `${u.code} — ${u.name} (${u.unitType})`,
      value: u.id,
    })),
]);

const unitTypeOptions = LEARNING_UNIT_TYPES.map((t) => ({
  label: t.charAt(0).toUpperCase() + t.slice(1),
  value: t,
}));

const { handleSubmit, errors, defineField, resetForm } =
  useForm<UpdateLearningUnitFormData>({
    validationSchema: toTypedSchema(updateLearningUnitSchema),
    initialValues: {
      code: '',
      name: '',
      unitType: 'subject',
      parentUnitId: null,
      level: '',
      creditWeight: null,
      description: '',
      notes: '',
    },
  });

const [code] = defineField('code');
const [name] = defineField('name');
const [unitType] = defineField('unitType');
const [parentUnitId] = defineField('parentUnitId');
const [level] = defineField('level');
const [creditWeight] = defineField('creditWeight');
const [description] = defineField('description');
const [notes] = defineField('notes');

watch(
  () => props.unit,
  (unit) => {
    if (unit) {
      resetForm({
        values: {
          code: unit.code,
          name: unit.name,
          unitType: unit.unitType as LearningUnitType,
          parentUnitId: unit.parentUnitId ?? null,
          level: unit.level ?? '',
          creditWeight: unit.creditWeight ?? null,
          description: unit.description ?? '',
          notes: unit.notes ?? '',
        },
      });
    }
  },
  { immediate: true },
);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen && props.unit) {
      resetForm({
        values: {
          code: props.unit.code,
          name: props.unit.name,
          unitType: props.unit.unitType as LearningUnitType,
          parentUnitId: props.unit.parentUnitId ?? null,
          level: props.unit.level ?? '',
          creditWeight: props.unit.creditWeight ?? null,
          description: props.unit.description ?? '',
          notes: props.unit.notes ?? '',
        },
      });
    }
  },
);

const updateMutation = useUpdateUnit({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('updated', data);
  },
});

const onSubmit = handleSubmit((values) => {
  if (!props.unit?.id) return;

  updateMutation.mutate({
    id: props.unit.id,
    structureId: props.structureId ?? undefined,
    request: {
      code: values.code.trim(),
      name: values.name.trim(),
      unitType: values.unitType,
      parentUnitId: values.parentUnitId || null,
      level: values.level?.trim() || null,
      creditWeight:
        values.creditWeight != null && !Number.isNaN(values.creditWeight)
          ? Number(values.creditWeight)
          : null,
      description: values.description?.trim() || null,
      notes: values.notes?.trim() || null,
    },
  });
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Edit Learning Unit"
    :style="{ width: '600px', maxWidth: '95vw' }"
    class="p-dialog-sm"
    data-testid="edit-learning-unit-modal"
  >
    <!-- Warning Badge when Framework is Active (TD-06 Controlled In-Place Edit) -->
    <div
      v-if="isStructureActive"
      class="p-3 mb-4 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800 flex items-start gap-2.5"
      data-testid="active-framework-warning"
    >
      <AlertTriangle class="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
      <div>
        <span class="font-semibold text-amber-900">
          Structural fields are locked on active frameworks.
        </span>
        <p class="text-[11px] text-amber-700 mt-0.5">
          Unit code, hierarchy, and unit type cannot be modified once approved. Name, level, credit weight, description, and notes remain editable.
        </p>
      </div>
    </div>

    <form class="space-y-4" @submit.prevent="onSubmit">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Unit Code -->
        <FormField
          label="Unit Code"
          required
          :help-text="isStructureActive ? 'Code locked on active framework' : undefined"
          :error="errors.code"
        >
          <div class="relative">
            <InputText
              v-model="code"
              :disabled="isStructureActive"
              placeholder="e.g. MATH-101"
              class="w-full font-mono text-sm"
              :class="{ 'bg-slate-100 text-slate-500 cursor-not-allowed': isStructureActive }"
              data-testid="input-edit-unit-code"
            />
            <Lock
              v-if="isStructureActive"
              class="w-3.5 h-3.5 text-slate-400 absolute right-3 top-3 pointer-events-none"
            />
          </div>
        </FormField>

        <!-- Unit Name -->
        <FormField label="Unit Name" required :error="errors.name">
          <InputText
            v-model="name"
            placeholder="e.g. Algebra Fundamentals"
            class="w-full text-sm"
            data-testid="input-edit-unit-name"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Unit Type -->
        <FormField
          label="Unit Type"
          required
          :help-text="isStructureActive ? 'Type locked on active framework' : undefined"
          :error="errors.unitType"
        >
          <Select
            v-model="unitType"
            :options="unitTypeOptions"
            option-label="label"
            option-value="value"
            :disabled="isStructureActive"
            placeholder="Select unit type"
            class="w-full text-sm"
            data-testid="select-edit-unit-type"
          />
        </FormField>

        <!-- Parent Unit -->
        <FormField
          label="Parent Learning Unit"
          :help-text="isStructureActive ? 'Hierarchy locked on active framework' : 'Constrained to root units only (2-level cap)'"
          :error="errors.parentUnitId"
        >
          <Select
            v-model="parentUnitId"
            :options="parentDropdownOptions"
            option-label="label"
            option-value="value"
            :disabled="isStructureActive"
            placeholder="None (Root Learning Unit)"
            class="w-full text-sm"
            data-testid="select-edit-parent-unit"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Academic Level -->
        <FormField label="Level / Grade" optional :error="errors.level">
          <InputText
            v-model="level"
            placeholder="e.g. Grade 10, Intermediate"
            class="w-full text-sm"
            data-testid="input-edit-unit-level"
          />
        </FormField>

        <!-- Credit Weight -->
        <FormField label="Credit Weight" optional :error="errors.creditWeight">
          <InputNumber
            v-model="creditWeight"
            :min="0"
            :min-fraction-digits="0"
            :max-fraction-digits="2"
            placeholder="e.g. 1.0 or 3.0"
            class="w-full text-sm"
            data-testid="input-edit-unit-credit-weight"
          />
        </FormField>
      </div>

      <!-- Description -->
      <FormField label="Description / Learning Objectives" optional :error="errors.description">
        <Textarea
          v-model="description"
          rows="2"
          placeholder="Brief summary of learning competencies, syllabus topics, or unit goals."
          class="w-full text-sm"
          data-testid="textarea-edit-unit-description"
        />
      </FormField>

      <!-- Internal Notes -->
      <FormField label="Internal Administrative Notes" optional :error="errors.notes">
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Optional coordinator notes or internal references."
          class="w-full text-sm"
          data-testid="textarea-edit-unit-notes"
        />
      </FormField>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          data-testid="btn-cancel-edit-unit"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          label="Save Changes"
          icon="pi pi-check"
          class="bg-teal-600 hover:bg-teal-700 text-white"
          data-testid="btn-submit-edit-unit"
          :loading="updateMutation.isPending.value"
        />
      </div>
    </form>
  </Dialog>
</template>
