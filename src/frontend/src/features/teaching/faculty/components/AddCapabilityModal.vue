<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { Sparkles, Info } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { addCapabilitySchema } from '../api/teachingSchemas';
import { useAddCapabilityMutation } from '../composables/useFacultyMutations';
import { useCapabilityTypesQuery } from '../composables/useFacultyQueries';
import type {
  AddTeachingCapabilityRequest,
  AddTeachingCapabilityResponse,
  CapabilityTypeDto,
} from '../api/teachingTypes';

export interface AddCapabilityModalProps {
  visible: boolean;
  teacherId?: string;
  capabilityTypes?: CapabilityTypeDto[];
  appendTo?: string;
}

const props = withDefaults(defineProps<AddCapabilityModalProps>(), {
  visible: false,
  teacherId: undefined,
  capabilityTypes: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'added', response: AddTeachingCapabilityResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Capability Taxonomy Query
// ---------------------------------------------------------------------------
const { data: fetchedCapabilityTypes, isLoading: isLoadingTypes } = useCapabilityTypesQuery({
  enabled: computed(() => props.visible && !props.capabilityTypes),
});

const availableCapabilityTypes = computed<CapabilityTypeDto[]>(() => {
  return props.capabilityTypes ?? fetchedCapabilityTypes.value ?? [];
});

const capabilityTypeOptions = computed(() =>
  availableCapabilityTypes.value.map((type) => ({
    label: type.name,
    value: type.id,
    code: type.code,
  })),
);

// Preset option lists for ergonomic selection
const applicableLevelOptions = [
  'Early Childhood (PAUD/TK)',
  'SD Kelas 1-3 (Lower Primary)',
  'SD Kelas 4-6 (Upper Primary)',
  'SMP Kelas 7-9 (Junior High)',
  'SMA Kelas 10-12 (Senior High)',
  'Vocational / Specialized',
  'All Grades / Universal',
];

const deliveryModeOptions = [
  'In-Person / Face-to-Face',
  'Online / Remote Synchronous',
  'Hybrid / Blended',
  'Field Practicum / Laboratory',
];

const languageOptions = [
  'Bahasa Indonesia',
  'English',
  'Bilingual (ID/EN)',
  'Mandarin',
  'Arabic',
];

// ---------------------------------------------------------------------------
// 2. Reactive Form State
// ---------------------------------------------------------------------------
const form = ref<AddTeachingCapabilityRequest>({
  capabilityTypeId: '',
  capabilityCode: '',
  capabilityName: '',
  applicableLevel: '',
  deliveryMode: '',
  languageOfInstruction: '',
  notes: '',
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    capabilityTypeId: capabilityTypeOptions.value[0]?.value ?? '',
    capabilityCode: '',
    capabilityName: '',
    applicableLevel: applicableLevelOptions[2], // Default Upper Primary
    deliveryMode: deliveryModeOptions[0], // In-Person
    languageOfInstruction: languageOptions[0], // Bahasa Indonesia
    notes: '',
  };
  errors.value = {};
}

watch(
  () => [props.visible, capabilityTypeOptions.value],
  ([isOpen]) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 3. Mutation & Form Submission
// ---------------------------------------------------------------------------
const addCapabilityMutation = useAddCapabilityMutation();

async function handleSubmit() {
  errors.value = {};

  if (!props.teacherId) {
    errors.value.general = 'Teacher profile identifier is required.';
    return;
  }

  const payload: AddTeachingCapabilityRequest = {
    capabilityTypeId: form.value.capabilityTypeId?.trim() ?? '',
    capabilityCode: form.value.capabilityCode?.trim().toUpperCase() ?? '',
    capabilityName: form.value.capabilityName?.trim() ?? '',
    applicableLevel: form.value.applicableLevel?.trim() || null,
    deliveryMode: form.value.deliveryMode?.trim() || null,
    languageOfInstruction: form.value.languageOfInstruction?.trim() || null,
    notes: form.value.notes?.trim() || null,
  };

  const validationResult = addCapabilitySchema.safeParse(payload);
  if (!validationResult.success) {
    const formatted = validationResult.error.format();
    for (const [key, val] of Object.entries(formatted)) {
      if (key !== '_errors' && val && typeof val === 'object' && '_errors' in val) {
        errors.value[key] = (val as { _errors: string[] })._errors[0] || 'Invalid field value.';
      }
    }
    return;
  }

  try {
    const result = await addCapabilityMutation.mutateAsync({
      teacherId: props.teacherId,
      payload: validationResult.data,
    });

    emit('added', result);
    emit('update:visible', false);
    emit('close');
  } catch (err: unknown) {
    // API errors handled by mutation toast
    if (err && typeof err === 'object' && 'response' in err) {
      const resp = (err as { response?: { data?: { detail?: string; message?: string } } }).response;
      errors.value.general = resp?.data?.detail || resp?.data?.message || 'Failed to add teaching capability.';
    }
  }
}

function handleClose() {
  emit('update:visible', false);
  emit('close');
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-xl mx-4"
    :append-to="appendTo"
    data-testid="add-capability-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <div class="p-2 rounded-lg bg-primary-50 dark:bg-primary-950/60 text-primary-600 dark:text-primary-400">
          <Sparkles class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-100">
            Add Potential Teaching Capability
          </h2>
          <p class="text-xs text-surface-500">
            Record subject, stage, or delivery capability for faculty profile (TD-02).
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <!-- TD-02 Architectural Disclaimer Banner -->
      <div
        class="p-3 rounded-lg bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800 text-sky-800 dark:text-sky-300 flex items-start gap-2.5 text-xs leading-relaxed"
        data-testid="capability-disclaimer-banner"
      >
        <Info class="w-4 h-4 shrink-0 mt-0.5 text-sky-600 dark:text-sky-400" />
        <span>
          <strong>Pedagogical Suitability Note:</strong> Teaching capabilities describe subject and delivery competence. Adding capabilities does not alter teacher standing or grant formal teaching authorizations (which require formal Teaching Authority approval).
        </span>
      </div>

      <!-- General Error Alert -->
      <div
        v-if="errors.general"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
        data-testid="add-capability-error"
      >
        {{ errors.general }}
      </div>

      <!-- Form Grid -->
      <form
        class="space-y-3.5"
        data-testid="add-capability-form"
        @submit.prevent="handleSubmit"
      >
        <!-- Capability Type -->
        <FormField
          label="Capability Category / Taxonomy"
          required
          :error="errors.capabilityTypeId"
          hint="Taxonomic category representing subject domain, stage, or specialized methodology."
        >
          <Select
            v-model="form.capabilityTypeId"
            :options="capabilityTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select capability type..."
            class="w-full"
            :loading="isLoadingTypes"
            :invalid="Boolean(errors.capabilityTypeId)"
            data-testid="select-capability-type"
          />
        </FormField>

        <!-- Code & Name -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <FormField
            label="Capability Code"
            required
            :error="errors.capabilityCode"
            hint="e.g. MATH_SEC, ENG_ADV"
            class="sm:col-span-1"
          >
            <InputText
              v-model="form.capabilityCode"
              placeholder="e.g. MATH_SD"
              class="w-full font-mono uppercase"
              :invalid="Boolean(errors.capabilityCode)"
              data-testid="input-capability-code"
            />
          </FormField>

          <FormField
            label="Capability Name"
            required
            :error="errors.capabilityName"
            hint="Official capability title"
            class="sm:col-span-2"
          >
            <InputText
              v-model="form.capabilityName"
              placeholder="e.g. Matematika SD Kelas 4-6"
              class="w-full"
              :invalid="Boolean(errors.capabilityName)"
              data-testid="input-capability-name"
            />
          </FormField>
        </div>

        <!-- Applicable Level & Delivery Mode -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Applicable Level / Stage"
            :error="errors.applicableLevel"
            hint="Educational grade or target stage"
          >
            <Select
              v-model="form.applicableLevel"
              :options="applicableLevelOptions"
              editable
              placeholder="Select or enter level..."
              class="w-full"
              :invalid="Boolean(errors.applicableLevel)"
              data-testid="select-applicable-level"
            />
          </FormField>

          <FormField
            label="Delivery Mode"
            :error="errors.deliveryMode"
            hint="Instructional delivery format"
          >
            <Select
              v-model="form.deliveryMode"
              :options="deliveryModeOptions"
              editable
              placeholder="Select or enter mode..."
              class="w-full"
              :invalid="Boolean(errors.deliveryMode)"
              data-testid="select-delivery-mode"
            />
          </FormField>
        </div>

        <!-- Language of Instruction -->
        <FormField
          label="Language of Instruction"
          :error="errors.languageOfInstruction"
          hint="Primary teaching language medium"
        >
          <Select
            v-model="form.languageOfInstruction"
            :options="languageOptions"
            editable
            placeholder="Select or enter language..."
            class="w-full"
            :invalid="Boolean(errors.languageOfInstruction)"
            data-testid="select-language"
          />
        </FormField>

        <!-- Notes -->
        <FormField
          label="Pedagogical Notes (Optional)"
          :error="errors.notes"
          hint="Additional instructional observations or context (max 2000 characters)"
        >
          <Textarea
            v-model="form.notes"
            rows="2"
            auto-resize
            placeholder="Optional notes regarding syllabus alignment, specialized certifications..."
            class="w-full text-xs"
            :invalid="Boolean(errors.notes)"
            data-testid="textarea-capability-notes"
          />
        </FormField>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="addCapabilityMutation.isPending.value"
          data-testid="cancel-add-capability-btn"
          @click="handleClose"
        />
        <Button
          type="button"
          label="Add Capability"
          icon="pi pi-check"
          severity="primary"
          size="small"
          :loading="addCapabilityMutation.isPending.value"
          data-testid="submit-add-capability-btn"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
