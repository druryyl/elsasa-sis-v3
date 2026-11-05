<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  ShieldCheck,
  Calendar,
  Layers,
  BookOpen,
  Info,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  createAcademicRequirementSchema,
  type CreateAcademicRequirementFormData,
} from '../schemas/curriculum.schemas';
import {
  useCreateRequirement,
  useLearningUnits,
} from '../composables/useCurriculum';
import type {
  AcademicRequirement,
  AcademicRequirementScope,
  AcademicRequirementType,
} from '../types/curriculum.types';

const props = defineProps<{
  visible: boolean;
  structureId: string;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', requirement: AcademicRequirement): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const structureIdRef = computed(() => props.structureId ?? '');
const { data: units, isLoading: isLoadingUnits } = useLearningUnits(structureIdRef);

const unitOptions = computed(() => {
  return (units.value ?? []).map((u) => ({
    label: `${u.code} - ${u.name} (${u.unitType})`,
    value: u.id,
  }));
});

const scopeOptions: { label: string; value: AcademicRequirementScope; description: string }[] = [
  {
    value: 'program',
    label: 'Program-Wide',
    description: 'Applies across the entire curriculum framework',
  },
  {
    value: 'level',
    label: 'Level-Specific',
    description: 'Applies to a specific academic grade or educational stage',
  },
  {
    value: 'learning_unit',
    label: 'Learning Unit-Specific',
    description: 'Prerequisite or policy rule bound to an individual learning unit',
  },
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
  useForm<CreateAcademicRequirementFormData>({
    validationSchema: toTypedSchema(createAcademicRequirementSchema),
    initialValues: {
      name: '',
      requirementScope: 'program',
      requirementType: 'completion',
      learningUnitId: null,
      level: null,
      description: '',
      version: '1.0',
      effectiveDate: new Date().toISOString().slice(0, 10),
      endDate: null,
      notes: '',
    },
  });

const [name] = defineField('name');
const [requirementScope] = defineField('requirementScope');
const [requirementType] = defineField('requirementType');
const [learningUnitId] = defineField('learningUnitId');
const [level] = defineField('level');
const [description] = defineField('description');
const [version] = defineField('version');
const [effectiveDate] = defineField('effectiveDate');
const [endDate] = defineField('endDate');
const [notes] = defineField('notes');

watch(
  () => requirementScope.value,
  (scope) => {
    if (scope === 'program') {
      setFieldValue('learningUnitId', null);
      setFieldValue('level', null);
    } else if (scope === 'level') {
      setFieldValue('learningUnitId', null);
    } else if (scope === 'learning_unit') {
      setFieldValue('level', null);
    }
  },
);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          name: '',
          requirementScope: 'program',
          requirementType: 'completion',
          learningUnitId: null,
          level: null,
          description: '',
          version: '1.0',
          effectiveDate: new Date().toISOString().slice(0, 10),
          endDate: null,
          notes: '',
        },
      });
    }
  },
);

const createMutation = useCreateRequirement({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('created', data);
  },
});

const onSubmit = handleSubmit(
  (values) => {
    createMutation.mutate({
      structureId: props.structureId,
      request: {
        name: values.name.trim(),
        requirementScope: values.requirementScope,
        requirementType: values.requirementType,
        learningUnitId:
          values.requirementScope === 'learning_unit' && values.learningUnitId
            ? values.learningUnitId.trim()
            : null,
        level:
          values.requirementScope === 'level' && values.level
            ? values.level.trim()
            : null,
        description: values.description.trim(),
        version: values.version.trim(),
        effectiveDate: values.effectiveDate,
        endDate: values.endDate && values.endDate.trim().length > 0 ? values.endDate : null,
        notes: values.notes && values.notes.trim().length > 0 ? values.notes.trim() : null,
      },
    });
  },
  (ctx) => {
    console.error('CreateRequirementModal validation failed:', ctx.errors);
  },
);

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Add Academic Requirement"
    modal
    class="max-w-2xl w-full"
    data-testid="create-requirement-modal"
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
              Add Academic Requirement
            </h3>
            <span
              class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-teal-50 text-teal-700 border border-teal-200"
            >
              Policy Rule
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Define completion, prerequisite, credit quota, or progression criteria for this framework.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Section 1: Scope & Association -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <div class="flex items-center gap-1.5 pb-1 border-b border-slate-100 text-xs font-semibold text-slate-700 uppercase tracking-wider">
          <Layers class="w-3.5 h-3.5 text-teal-600" />
          <span>Requirement Scope & Target</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="req-scope"
            label="Requirement Scope"
            required
            :error="errors.requirementScope"
            help-text="Architectural target of this academic rule"
          >
            <Select
              id="req-scope"
              v-model="requirementScope"
              :options="scopeOptions"
              option-label="label"
              option-value="value"
              placeholder="Select scope"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.requirementScope }"
              data-testid="req-scope-select"
            />
          </FormField>

          <FormField
            id="req-type"
            label="Requirement Type"
            required
            :error="errors.requirementType"
            help-text="Evaluation classification"
          >
            <Select
              id="req-type"
              v-model="requirementType"
              :options="requirementTypeOptions"
              option-label="label"
              option-value="value"
              placeholder="Select type"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.requirementType }"
              data-testid="req-type-select"
            />
          </FormField>
        </div>

        <!-- Conditional Dynamic Field: Learning Unit Scope -->
        <div v-if="requirementScope === 'learning_unit'" class="pt-1">
          <FormField
            id="req-learning-unit"
            label="Target Learning Unit"
            required
            :error="errors.learningUnitId"
            help-text="Select the unit or course to which this requirement applies"
          >
            <Select
              id="req-learning-unit"
              v-model="learningUnitId"
              :options="unitOptions"
              option-label="label"
              option-value="value"
              :loading="isLoadingUnits"
              placeholder="Select associated learning unit"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.learningUnitId }"
              data-testid="req-learning-unit-select"
            />
          </FormField>
        </div>

        <!-- Conditional Dynamic Field: Level Scope -->
        <div v-else-if="requirementScope === 'level'" class="pt-1">
          <FormField
            id="req-level"
            label="Academic Level / Grade"
            required
            :error="errors.level"
            help-text="e.g. Grade 1, Grade 10, Fase A, Year 1"
          >
            <InputText
              id="req-level"
              v-model="level"
              placeholder="e.g. Grade 10"
              class="w-full text-sm font-medium"
              :class="{ 'p-invalid': errors.level }"
              data-testid="req-level-input"
            />
          </FormField>
        </div>

        <!-- Scope Informational Hint -->
        <div class="flex items-center gap-2 p-2.5 rounded-md bg-slate-50 border border-slate-200 text-xs text-slate-600">
          <Info class="w-4 h-4 text-teal-600 shrink-0" />
          <span v-if="requirementScope === 'program'">
            Program-wide requirements apply uniformly to all units and enrolled students under this framework.
          </span>
          <span v-else-if="requirementScope === 'level'">
            Level-specific requirements govern progression and credit benchmarks for the specified grade level.
          </span>
          <span v-else-if="requirementScope === 'learning_unit'">
            Unit-specific requirements govern prerequisite completion or assessment thresholds for the chosen unit.
          </span>
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
              id="req-name"
              label="Requirement Name"
              required
              :error="errors.name"
              help-text="Descriptive title of the academic rule"
            >
              <InputText
                id="req-name"
                v-model="name"
                placeholder="e.g. Grade 10 Promotion Minimum Credits"
                class="w-full text-sm font-medium"
                :class="{ 'p-invalid': errors.name }"
                data-testid="req-name-input"
              />
            </FormField>
          </div>

          <FormField
            id="req-version"
            label="Version"
            required
            :error="errors.version"
            help-text="Version string"
          >
            <InputText
              id="req-version"
              v-model="version"
              placeholder="e.g. 1.0"
              class="w-full text-sm font-mono tracking-wide"
              :class="{ 'p-invalid': errors.version }"
              data-testid="req-version-input"
            />
          </FormField>
        </div>

        <FormField
          id="req-description"
          label="Criteria Description"
          required
          :error="errors.description"
          help-text="Explicit rule explanation, passing score thresholds, or required prerequisites"
        >
          <Textarea
            id="req-description"
            v-model="description"
            rows="3"
            placeholder="Specify the exact academic criteria (e.g. Student must complete at least 32 credit units with minimum GPA 2.50)..."
            class="w-full text-sm resize-none"
            :class="{ 'p-invalid': errors.description }"
            data-testid="req-description-input"
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
            id="req-effective-date"
            label="Effective Date"
            required
            :error="errors.effectiveDate"
            help-text="Date this policy rule comes into effect"
          >
            <InputText
              id="req-effective-date"
              v-model="effectiveDate"
              type="date"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.effectiveDate }"
              data-testid="req-effective-date-input"
            />
          </FormField>

          <FormField
            id="req-end-date"
            label="End Date"
            optional
            :error="errors.endDate"
            help-text="Expiration date (leave blank for indefinite)"
          >
            <InputText
              id="req-end-date"
              v-model="endDate"
              type="date"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.endDate }"
              data-testid="req-end-date-input"
            />
          </FormField>
        </div>

        <FormField
          id="req-notes"
          label="Governance Notes"
          optional
          :error="errors.notes"
          help-text="Internal notes or regulatory citations"
        >
          <Textarea
            id="req-notes"
            v-model="notes"
            rows="2"
            placeholder="Add internal notes or accreditation citations..."
            class="w-full text-sm resize-none"
            data-testid="req-notes-input"
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
            :loading="createMutation.isPending.value"
            data-testid="modal-submit-create-requirement-btn"
          >
            <ShieldCheck class="w-4 h-4 mr-1.5" />
            <span>Create Requirement</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
