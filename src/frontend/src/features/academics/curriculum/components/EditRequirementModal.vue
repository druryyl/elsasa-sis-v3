<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import {
  ShieldCheck,
  Lock,
  Layers,
  Calendar,
  BookOpen,
  Info,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  updateAcademicRequirementSchema,
  type UpdateAcademicRequirementFormData,
} from '../schemas/curriculum.schemas';
import {
  useUpdateRequirement,
  useLearningUnits,
} from '../composables/useCurriculum';
import type {
  AcademicRequirement,
  AcademicRequirementScope,
  AcademicRequirementType,
} from '../types/curriculum.types';

const props = defineProps<{
  visible: boolean;
  requirement: AcademicRequirement | null;
  structureId?: string | undefined;
  isStructureActive?: boolean | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'updated', requirement: AcademicRequirement): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const isLocked = computed(() => {
  return props.isStructureActive || props.requirement?.status === 'active';
});

const structureIdRef = computed(() => props.structureId ?? '');
const { data: units, isLoading: isLoadingUnits } = useLearningUnits(structureIdRef);

const unitOptions = computed(() => {
  return (units.value ?? []).map((u) => ({
    label: `${u.code} - ${u.name} (${u.unitType})`,
    value: u.id,
  }));
});

const scopeOptions: { label: string; value: AcademicRequirementScope }[] = [
  { value: 'program', label: 'Program-Wide' },
  { value: 'level', label: 'Level-Specific' },
  { value: 'learning_unit', label: 'Learning Unit-Specific' },
];

const requirementTypeOptions: { label: string; value: AcademicRequirementType }[] = [
  { value: 'completion', label: 'Completion - Required unit completion rule' },
  { value: 'prerequisite', label: 'Prerequisite - Required prior unit or competency' },
  { value: 'credit', label: 'Credit Quota - Minimum credit weight rule' },
  { value: 'progression', label: 'Progression - Advancement to next stage' },
  { value: 'graduation', label: 'Graduation - Institutional exit criteria' },
  { value: 'assessment_policy', label: 'Assessment Policy - Passing thresholds & grading' },
];

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<UpdateAcademicRequirementFormData>({
    validationSchema: toTypedSchema(updateAcademicRequirementSchema),
    initialValues: {
      name: '',
      description: '',
      notes: '',
      requirementScope: null,
      learningUnitId: null,
      level: null,
      requirementType: null,
      version: null,
      effectiveDate: null,
      endDate: null,
    },
  });

const [name] = defineField('name');
const [description] = defineField('description');
const [notes] = defineField('notes');
const [requirementScope] = defineField('requirementScope');
const [learningUnitId] = defineField('learningUnitId');
const [level] = defineField('level');
const [requirementType] = defineField('requirementType');
const [version] = defineField('version');
const [effectiveDate] = defineField('effectiveDate');
const [endDate] = defineField('endDate');

watch(
  () => [props.visible, props.requirement] as const,
  ([isOpen, req]) => {
    if (isOpen && req) {
      resetForm({
        values: {
          name: req.name ?? '',
          description: req.description ?? '',
          notes: req.notes ?? '',
          requirementScope: (req.requirementScope as AcademicRequirementScope) ?? null,
          learningUnitId: req.learningUnitId ?? null,
          level: req.level ?? null,
          requirementType: (req.requirementType as AcademicRequirementType) ?? null,
          version: req.version ?? null,
          effectiveDate: req.effectiveDate ?? null,
          endDate: req.endDate ?? null,
        },
      });
    }
  },
  { immediate: true },
);

const updateMutation = useUpdateRequirement({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('updated', data);
  },
});

const onSubmit = handleSubmit(
  (values) => {
    if (!props.requirement) return;

    const payload: UpdateAcademicRequirementFormData = {
      name: values.name.trim(),
      description: values.description.trim(),
      notes: values.notes && values.notes.trim().length > 0 ? values.notes.trim() : null,
    };

    // If not locked, include structural fields
    if (!isLocked.value) {
      payload.requirementScope = values.requirementScope;
      payload.requirementType = values.requirementType;
      payload.learningUnitId =
        values.requirementScope === 'learning_unit' && values.learningUnitId
          ? values.learningUnitId.trim()
          : null;
      payload.level =
        values.requirementScope === 'level' && values.level
          ? values.level.trim()
          : null;
      payload.version = values.version?.trim() || null;
      payload.effectiveDate = values.effectiveDate?.trim() || null;
      payload.endDate = values.endDate && values.endDate.trim().length > 0 ? values.endDate : null;
    }

    updateMutation.mutate({
      id: props.requirement.id,
      structureId: props.structureId,
      request: payload,
    });
  },
  (ctx) => {
    console.error('EditRequirementModal validation failed:', ctx.errors);
  },
);

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Edit Academic Requirement"
    modal
    class="max-w-2xl w-full"
    data-testid="edit-requirement-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center shrink-0 text-teal-600 shadow-xs"
        >
          <ShieldCheck class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">
              Edit Academic Requirement
            </h3>
            <Tag
              v-if="requirement"
              :value="(requirement.status || 'draft').toUpperCase()"
              :severity="requirement.status === 'active' ? 'success' : 'secondary'"
              class="text-[10px]"
            />
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Modify requirement policy specifications, description, and internal notes.
          </p>
        </div>
      </div>
    </template>

    <!-- Immutability Alert when framework is active -->
    <div
      v-if="isLocked"
      class="mb-3 p-3 rounded-lg bg-amber-50 border border-amber-200 text-amber-900 text-xs flex items-start gap-2.5"
      data-testid="requirement-locked-banner"
    >
      <Lock class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
      <div>
        <p class="font-semibold">Active Framework Guard</p>
        <p class="text-amber-800 mt-0.5">
          Structural scope, requirement type, and effective dates are locked on active curriculum frameworks.
          Only Name, Description, and Governance Notes may be edited. To alter policy structure, create a successor version.
        </p>
      </div>
    </div>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Section 1: Scope & Association -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <div class="flex items-center gap-1.5 pb-1 border-b border-slate-100 text-xs font-semibold text-slate-700 uppercase tracking-wider">
          <Layers class="w-3.5 h-3.5 text-teal-600" />
          <span>Requirement Scope & Target</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="edit-req-scope"
            label="Requirement Scope"
            :error="errors.requirementScope"
            help-text="Architectural target of this rule"
          >
            <Select
              id="edit-req-scope"
              v-model="requirementScope"
              :options="scopeOptions"
              option-label="label"
              option-value="value"
              :disabled="isLocked"
              placeholder="Select scope"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.requirementScope }"
              data-testid="edit-req-scope-select"
            />
          </FormField>

          <FormField
            id="edit-req-type"
            label="Requirement Type"
            :error="errors.requirementType"
            help-text="Evaluation classification"
          >
            <Select
              id="edit-req-type"
              v-model="requirementType"
              :options="requirementTypeOptions"
              option-label="label"
              option-value="value"
              :disabled="isLocked"
              placeholder="Select type"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.requirementType }"
              data-testid="edit-req-type-select"
            />
          </FormField>
        </div>

        <!-- Conditional Field: Learning Unit Scope -->
        <div v-if="requirementScope === 'learning_unit'" class="pt-1">
          <FormField
            id="edit-req-learning-unit"
            label="Target Learning Unit"
            :error="errors.learningUnitId"
            help-text="Associated learning unit"
          >
            <Select
              id="edit-req-learning-unit"
              v-model="learningUnitId"
              :options="unitOptions"
              option-label="label"
              option-value="value"
              :disabled="isLocked"
              :loading="isLoadingUnits"
              placeholder="Select associated learning unit"
              class="w-full text-sm"
              data-testid="edit-req-learning-unit-select"
            />
          </FormField>
        </div>

        <!-- Conditional Field: Level Scope -->
        <div v-else-if="requirementScope === 'level'" class="pt-1">
          <FormField
            id="edit-req-level"
            label="Academic Level / Grade"
            :error="errors.level"
            help-text="e.g. Grade 1, Grade 10, Fase A, Year 1"
          >
            <InputText
              id="edit-req-level"
              v-model="level"
              :disabled="isLocked"
              placeholder="e.g. Grade 10"
              class="w-full text-sm font-medium"
              data-testid="edit-req-level-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Section 2: Identity & Criteria -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <div class="flex items-center gap-1.5 pb-1 border-b border-slate-100 text-xs font-semibold text-slate-700 uppercase tracking-wider">
          <BookOpen class="w-3.5 h-3.5 text-teal-600" />
          <span>Requirement Specification</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div class="sm:col-span-2">
            <FormField
              id="edit-req-name"
              label="Requirement Name"
              required
              :error="errors.name"
              help-text="Descriptive title of the academic rule"
            >
              <InputText
                id="edit-req-name"
                v-model="name"
                placeholder="e.g. Grade 10 Promotion Minimum Credits"
                class="w-full text-sm font-medium"
                :class="{ 'p-invalid': errors.name }"
                data-testid="edit-req-name-input"
              />
            </FormField>
          </div>

          <FormField
            id="edit-req-version"
            label="Version"
            :error="errors.version"
            help-text="Version string"
          >
            <InputText
              id="edit-req-version"
              v-model="version"
              :disabled="isLocked"
              placeholder="e.g. 1.0"
              class="w-full text-sm font-mono tracking-wide"
              data-testid="edit-req-version-input"
            />
          </FormField>
        </div>

        <FormField
          id="edit-req-description"
          label="Criteria Description"
          required
          :error="errors.description"
          help-text="Explicit rule explanation, passing score thresholds, or required prerequisites"
        >
          <Textarea
            id="edit-req-description"
            v-model="description"
            rows="3"
            placeholder="Specify the exact academic criteria..."
            class="w-full text-sm resize-none"
            :class="{ 'p-invalid': errors.description }"
            data-testid="edit-req-description-input"
          />
        </FormField>
      </div>

      <!-- Section 3: Effective Dates & Notes -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center gap-1.5 pb-1 border-b border-slate-100 text-xs font-semibold text-slate-700 uppercase tracking-wider">
          <Calendar class="w-3.5 h-3.5 text-teal-600" />
          <span>Effective Dates & Administrative Notes</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
          <FormField
            id="edit-req-effective-date"
            label="Effective Date"
            :error="errors.effectiveDate"
            help-text="Date this policy rule comes into effect"
          >
            <InputText
              id="edit-req-effective-date"
              v-model="effectiveDate"
              type="date"
              :disabled="isLocked"
              class="w-full text-sm"
              data-testid="edit-req-effective-date-input"
            />
          </FormField>

          <FormField
            id="edit-req-end-date"
            label="End Date"
            optional
            :error="errors.endDate"
            help-text="Expiration date"
          >
            <InputText
              id="edit-req-end-date"
              v-model="endDate"
              type="date"
              :disabled="isLocked"
              class="w-full text-sm"
              data-testid="edit-req-end-date-input"
            />
          </FormField>
        </div>

        <FormField
          id="edit-req-notes"
          label="Governance Notes"
          optional
          :error="errors.notes"
          help-text="Internal notes or regulatory citations"
        >
          <Textarea
            id="edit-req-notes"
            v-model="notes"
            rows="2"
            placeholder="Add internal notes or accreditation citations..."
            class="w-full text-sm resize-none"
            data-testid="edit-req-notes-input"
          />
        </FormField>
      </div>

      <!-- Footer Actions -->
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
            :loading="updateMutation.isPending.value"
            data-testid="modal-submit-edit-requirement-btn"
          >
            <ShieldCheck class="w-4 h-4 mr-1.5" />
            <span>Save Changes</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
