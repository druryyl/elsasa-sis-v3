<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  FileText,
  Calendar,
  Layers,
  Sparkles,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  createLearningStructureSchema,
  type CreateLearningStructureFormData,
} from '../schemas/curriculum.schemas';
import { useCreateStructure } from '../composables/useCurriculum';
import type { LearningStructure } from '../types/curriculum.types';
import type { AcademicPeriodOption } from './CurriculumContextBar.vue';

const props = defineProps<{
  visible: boolean;
  periodId?: string | null | undefined;
  periods?: AcademicPeriodOption[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', structure: LearningStructure): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const periodOptions = computed(() => {
  return (props.periods ?? []).map((p) => ({
    label: p.label,
    value: p.id,
  }));
});

const { handleSubmit, errors, defineField, resetForm } =
  useForm<CreateLearningStructureFormData>({
    validationSchema: toTypedSchema(createLearningStructureSchema),
    initialValues: {
      name: '',
      academicPeriodId: props.periodId ?? '',
      version: '1.0',
      effectiveDate: new Date().toISOString().slice(0, 10),
      endDate: null,
      description: '',
      notes: '',
    },
  });

const [name] = defineField('name');
const [academicPeriodId] = defineField('academicPeriodId');
const [version] = defineField('version');
const [effectiveDate] = defineField('effectiveDate');
const [endDate] = defineField('endDate');
const [description] = defineField('description');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          name: '',
          academicPeriodId: props.periodId ?? (props.periods?.[0]?.id ?? ''),
          version: '1.0',
          effectiveDate: new Date().toISOString().slice(0, 10),
          endDate: null,
          description: '',
          notes: '',
        },
      });
    }
  },
);

const createMutation = useCreateStructure({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('created', data);
  },
});

const onSubmit = handleSubmit((values) => {
  createMutation.mutate({
    name: values.name.trim(),
    academicPeriodId: values.academicPeriodId,
    version: values.version.trim(),
    effectiveDate: values.effectiveDate,
    endDate: values.endDate && values.endDate.trim().length > 0 ? values.endDate : null,
    description: values.description && values.description.trim().length > 0 ? values.description.trim() : null,
    notes: values.notes && values.notes.trim().length > 0 ? values.notes.trim() : null,
  });
});

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Create Curriculum Framework"
    modal
    class="max-w-xl w-full"
    data-testid="create-structure-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center shrink-0 text-teal-600 shadow-xs">
          <Layers class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">Create Curriculum Framework</h3>
            <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-teal-50 text-teal-700 border border-teal-200">
              Draft Framework
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Establish a baseline curriculum framework for learning units, subjects, courses, and graduation rules.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Section 1: Framework Identity & Academic Period -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <FormField
          id="structure-name"
          label="Framework Name"
          required
          :error="errors.name"
          help-text="Descriptive title of the curriculum framework (e.g. Kurikulum Merdeka Fase A 2024)"
        >
          <InputText
            id="structure-name"
            v-model="name"
            placeholder="e.g. Kurikulum Nasional 2024 / Cambridge IGCSE"
            class="w-full text-sm font-medium"
            :class="{ 'p-invalid': errors.name }"
            data-testid="structure-name-input"
          />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="structure-period"
            label="Academic Period"
            required
            :error="errors.academicPeriodId"
            help-text="Operational period context"
          >
            <Select
              id="structure-period"
              v-model="academicPeriodId"
              :options="periodOptions"
              option-label="label"
              option-value="value"
              placeholder="Select academic period"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.academicPeriodId }"
              data-testid="structure-period-select"
            />
          </FormField>

          <FormField
            id="structure-version"
            label="Version Identifier"
            required
            :error="errors.version"
            help-text="Semantic version string (e.g. 1.0, 2024.1)"
          >
            <InputText
              id="structure-version"
              v-model="version"
              placeholder="e.g. 1.0"
              class="w-full text-sm font-mono tracking-wide"
              :class="{ 'p-invalid': errors.version }"
              data-testid="structure-version-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Section 2: Effective Dating -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center gap-1.5 pb-1 border-b border-slate-100 text-xs font-semibold text-slate-700 uppercase tracking-wider">
          <Calendar class="w-3.5 h-3.5 text-teal-600" />
          <span>Operational Effective Dating</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
          <FormField
            id="structure-effective-date"
            label="Effective Date"
            required
            :error="errors.effectiveDate"
            help-text="Date this framework becomes operational"
          >
            <InputText
              id="structure-effective-date"
              v-model="effectiveDate"
              type="date"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.effectiveDate }"
              data-testid="structure-effective-date-input"
            />
          </FormField>

          <FormField
            id="structure-end-date"
            label="End Date"
            optional
            :error="errors.endDate"
            help-text="Expiration date (leave blank for indefinite)"
          >
            <InputText
              id="structure-end-date"
              v-model="endDate"
              type="date"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.endDate }"
              data-testid="structure-end-date-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Section 3: Description & Governance Notes -->
      <div class="space-y-3">
        <FormField
          id="structure-description"
          label="Curriculum Description"
          optional
          :error="errors.description"
          help-text="Institutional purpose, pedagogical rationale, and core educational competencies"
        >
          <Textarea
            id="structure-description"
            v-model="description"
            rows="3"
            placeholder="Describe the scope, objectives, and pedagogical approach of this framework..."
            class="w-full text-sm resize-none"
            data-testid="structure-description-input"
          />
        </FormField>

        <FormField
          id="structure-notes"
          label="Internal Governance Notes"
          optional
          :error="errors.notes"
          help-text="Internal notes for academic coordinators and authorities"
        >
          <Textarea
            id="structure-notes"
            v-model="notes"
            rows="2"
            placeholder="Add internal notes or accreditation references..."
            class="w-full text-sm resize-none"
            data-testid="structure-notes-input"
          />
        </FormField>
      </div>

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
            data-testid="modal-submit-create-structure-btn"
            @click="onSubmit"
          >
            <Layers class="w-4 h-4 mr-1.5" />
            <span>Create Framework</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
