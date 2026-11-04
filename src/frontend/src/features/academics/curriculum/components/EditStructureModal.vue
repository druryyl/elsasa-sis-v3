<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Tag from 'primevue/tag';
import {
  FileEdit,
  ShieldAlert,
  Layers,
  Calendar,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  updateLearningStructureSchema,
  type UpdateLearningStructureFormData,
} from '../schemas/curriculum.schemas';
import { useUpdateStructure } from '../composables/useCurriculum';
import type {
  LearningStructure,
  LearningStructureSummary,
} from '../types/curriculum.types';

const props = defineProps<{
  visible: boolean;
  structure: LearningStructure | LearningStructureSummary | null | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'updated', structure: LearningStructure): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const isActive = computed(() => props.structure?.status === 'active');

const { handleSubmit, errors, defineField, resetForm } =
  useForm<UpdateLearningStructureFormData>({
    validationSchema: toTypedSchema(updateLearningStructureSchema),
    initialValues: {
      name: props.structure?.name ?? '',
      description: props.structure?.description ?? '',
      notes: props.structure?.notes ?? '',
    },
  });

const [name] = defineField('name');
const [description] = defineField('description');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen && props.structure) {
      resetForm({
        values: {
          name: props.structure.name ?? '',
          description: props.structure.description ?? '',
          notes: props.structure.notes ?? '',
        },
      });
    }
  },
  { immediate: true },
);

const updateMutation = useUpdateStructure({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('updated', data);
  },
});

const onSubmit = handleSubmit((values) => {
  if (!props.structure) return;

  updateMutation.mutate({
    id: props.structure.id,
    request: {
      name: values.name.trim(),
      description: values.description && values.description.trim().length > 0 ? values.description.trim() : null,
      notes: values.notes && values.notes.trim().length > 0 ? values.notes.trim() : null,
    },
  });
});

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Edit Curriculum Framework Metadata"
    modal
    class="max-w-xl w-full"
    data-testid="edit-structure-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center shrink-0 text-teal-600 shadow-xs">
          <FileEdit class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">Edit Framework Metadata</h3>
            <span
              v-if="isActive"
              class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200"
            >
              Active Framework
            </span>
            <span
              v-else
              class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200"
            >
              {{ structure?.status ? structure.status.toUpperCase() : 'DRAFT' }}
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Update descriptive title, pedagogical details, and internal coordinator notes.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Active Structure Lock Alert (TD-06) -->
      <div
        v-if="isActive"
        class="p-3 bg-amber-50 border border-amber-200 rounded-lg text-amber-900 text-xs flex items-start gap-2.5"
        data-testid="edit-locked-alert"
      >
        <ShieldAlert class="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
        <div>
          <span class="font-semibold">Structural Fields Locked:</span>
          <p class="mt-0.5 text-amber-800">
            This curriculum framework is currently <strong>active</strong>. Structural fields (version, academic period, effective dates, learning unit hierarchy) cannot be modified in place. Only non-structural metadata (name, description, notes) can be revised. To change structure, create a new version via <strong>Supersede</strong>.
          </p>
        </div>
      </div>

      <!-- Framework Overview Context Snapshot -->
      <div v-if="structure" class="bg-slate-50/80 border border-slate-200 rounded-lg p-3 text-xs grid grid-cols-2 gap-2 text-slate-600">
        <div>
          <span class="font-semibold text-slate-700">Academic Period:</span>
          <p class="truncate">{{ structure.academicPeriodLabel }}</p>
        </div>
        <div>
          <span class="font-semibold text-slate-700">Version:</span>
          <p class="font-mono">v{{ structure.version }}</p>
        </div>
        <div>
          <span class="font-semibold text-slate-700">Effective Date:</span>
          <p class="font-mono">{{ structure.effectiveDate }}</p>
        </div>
        <div>
          <span class="font-semibold text-slate-700">End Date:</span>
          <p class="font-mono">{{ structure.endDate || 'Active Indefinitely' }}</p>
        </div>
      </div>

      <!-- Metadata Fields Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <FormField
          id="edit-structure-name"
          label="Framework Name"
          required
          :error="errors.name"
          help-text="Descriptive title of this curriculum framework"
        >
          <InputText
            id="edit-structure-name"
            v-model="name"
            placeholder="e.g. Kurikulum Merdeka Fase A 2024"
            class="w-full text-sm font-medium"
            :class="{ 'p-invalid': errors.name }"
            data-testid="edit-structure-name-input"
          />
        </FormField>

        <FormField
          id="edit-structure-description"
          label="Description"
          optional
          :error="errors.description"
          help-text="Institutional purpose, pedagogical objectives, and learning outcomes"
        >
          <Textarea
            id="edit-structure-description"
            v-model="description"
            rows="3"
            placeholder="Describe the scope and objectives of this framework..."
            class="w-full text-sm resize-none"
            data-testid="edit-structure-description-input"
          />
        </FormField>

        <FormField
          id="edit-structure-notes"
          label="Internal Governance Notes"
          optional
          :error="errors.notes"
          help-text="Internal notes for academic coordinators and authorities"
        >
          <Textarea
            id="edit-structure-notes"
            v-model="notes"
            rows="2"
            placeholder="Add internal notes or syllabus references..."
            class="w-full text-sm resize-none"
            data-testid="edit-structure-notes-input"
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
            data-testid="edit-modal-cancel-btn"
            @click="isDialogVisible = false"
          />
          <Button
            type="submit"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white shadow-xs px-4"
            :loading="updateMutation.isPending.value"
            data-testid="modal-submit-edit-structure-btn"
            @click="onSubmit"
          >
            <FileEdit class="w-4 h-4 mr-1.5" />
            <span>Save Changes</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
