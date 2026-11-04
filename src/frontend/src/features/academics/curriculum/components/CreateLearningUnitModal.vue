<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import InputNumber from 'primevue/inputnumber';
import { Layers, Plus, GitFork } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  createLearningUnitSchema,
  LEARNING_UNIT_TYPES,
  type CreateLearningUnitFormData,
} from '../schemas/curriculum.schemas';
import { useCreateUnit } from '../composables/useCurriculum';
import type { LearningUnit, LearningUnitType } from '../types/curriculum.types';

const props = defineProps<{
  visible: boolean;
  structureId?: string | null | undefined;
  parentUnitId?: string | null | undefined;
  rootUnits?: LearningUnit[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', unit: LearningUnit): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const isChildCreation = computed(() => Boolean(props.parentUnitId));

const parentUnit = computed(() => {
  if (!props.parentUnitId || !props.rootUnits) return null;
  return props.rootUnits.find((u) => u.id === props.parentUnitId) ?? null;
});

// Dropdown options filter exclusively to root units (parentUnitId is null/undefined)
const parentDropdownOptions = computed(() => [
  { label: 'None (Root Learning Unit)', value: null },
  ...(props.rootUnits ?? [])
    .filter((u) => !u.parentUnitId)
    .map((u) => ({
      label: `${u.code} — ${u.name} (${u.unitType})`,
      value: u.id,
    })),
]);

const unitTypeOptions = LEARNING_UNIT_TYPES.map((t) => ({
  label: t.charAt(0).toUpperCase() + t.slice(1),
  value: t,
}));

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<CreateLearningUnitFormData>({
    validationSchema: toTypedSchema(createLearningUnitSchema),
    initialValues: {
      code: '',
      name: '',
      unitType: (props.parentUnitId ? 'module' : 'subject') as LearningUnitType,
      parentUnitId: props.parentUnitId ?? null,
      level: '',
      creditWeight: null,
      description: '',
      notes: '',
    },
  });

const [code] = defineField('code');
const [name] = defineField('name');
const [unitType] = defineField('unitType');
const [parentUnitIdField] = defineField('parentUnitId');
const [level] = defineField('level');
const [creditWeight] = defineField('creditWeight');
const [description] = defineField('description');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          code: '',
          name: '',
          unitType: (props.parentUnitId ? 'module' : 'subject') as LearningUnitType,
          parentUnitId: props.parentUnitId ?? null,
          level: '',
          creditWeight: null,
          description: '',
          notes: '',
        },
      });
    }
  },
);

const createMutation = useCreateUnit({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('created', data);
  },
});

const onSubmit = handleSubmit((values) => {
  if (!props.structureId) return;

  createMutation.mutate({
    structureId: props.structureId,
    request: {
      code: values.code.trim(),
      name: values.name.trim(),
      unitType: values.unitType,
      parentUnitId: isChildCreation.value ? (props.parentUnitId ?? null) : (values.parentUnitId || null),
      level: values.level?.trim() || null,
      creditWeight: values.creditWeight != null && !Number.isNaN(values.creditWeight)
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
    :header="isChildCreation ? 'Add Child Learning Module' : 'Add Root Learning Unit'"
    :style="{ width: '600px', maxWidth: '95vw' }"
    class="p-dialog-sm"
    data-testid="create-learning-unit-modal"
  >
    <form class="space-y-4 pt-2" @submit.prevent="onSubmit">
      <!-- Child Module Context Banner (When adding a child to a specific root unit) -->
      <div
        v-if="isChildCreation"
        class="p-3 bg-teal-50 border border-teal-200 rounded-lg text-xs text-teal-800 flex items-start gap-2"
        data-testid="child-unit-parent-banner"
      >
        <GitFork class="w-4 h-4 text-teal-600 mt-0.5 shrink-0" />
        <div>
          <span class="font-semibold text-teal-900">Parent Root Unit (Locked):</span>
          <p class="font-mono font-medium text-teal-800 mt-0.5">
            {{ parentUnit ? `${parentUnit.code} — ${parentUnit.name}` : props.parentUnitId }}
          </p>
          <span class="text-[11px] text-teal-600">
            This module will be linked directly to the parent root unit above.
          </span>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Unit Code -->
        <FormField label="Unit Code" required :error="errors.code">
          <InputText
            v-model="code"
            placeholder="e.g. MATH-101, MOD-01"
            class="w-full font-mono text-sm"
            data-testid="input-unit-code"
          />
        </FormField>

        <!-- Unit Name -->
        <FormField label="Unit Name" required :error="errors.name">
          <InputText
            v-model="name"
            placeholder="e.g. Algebra Fundamentals"
            class="w-full text-sm"
            data-testid="input-unit-name"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Unit Type -->
        <FormField label="Unit Type" required :error="errors.unitType">
          <Select
            v-model="unitType"
            :options="unitTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select unit type"
            class="w-full text-sm"
            data-testid="select-unit-type"
          />
        </FormField>

        <!-- Parent Unit (Locked if isChildCreation, else root units dropdown) -->
        <FormField
          label="Parent Learning Unit"
          :help-text="isChildCreation ? 'Parent unit is locked' : 'Constrained to root units only (2-level cap)'"
          :error="errors.parentUnitId"
        >
          <Select
            v-model="parentUnitIdField"
            :options="parentDropdownOptions"
            option-label="label"
            option-value="value"
            :disabled="isChildCreation"
            placeholder="None (Root Learning Unit)"
            class="w-full text-sm"
            data-testid="select-parent-unit"
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
            data-testid="input-unit-level"
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
            data-testid="input-unit-credit-weight"
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
          data-testid="textarea-unit-description"
        />
      </FormField>

      <!-- Internal Notes -->
      <FormField label="Internal Administrative Notes" optional :error="errors.notes">
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Optional coordinator notes or internal references."
          class="w-full text-sm"
          data-testid="textarea-unit-notes"
        />
      </FormField>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          data-testid="btn-cancel-create-unit"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          :label="isChildCreation ? 'Add Child Module' : 'Create Learning Unit'"
          icon="pi pi-check"
          class="bg-teal-600 hover:bg-teal-700 text-white"
          data-testid="btn-submit-create-unit"
          :loading="createMutation.isPending.value"
        />
      </div>
    </form>
  </Dialog>
</template>
