<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  Calendar,
  Layers,
  Scale,
  Award,
  UserCheck,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { z } from 'zod';
import { FormField } from '@/shared/components';
import {
  ISO_DATE_REGEX,
  isValidCalendarDate,
} from '../schemas/assessment.schema';
import {
  useCreateAssessmentMutation,
  useUpdateAssessmentMutation,
} from '../composables/useAssessments';
import type {
  Assessment,
  AssessmentMethod,
  AssessmentType,
  CreateAssessmentResponse,
  ReassessmentPolicy,
  UpdateAssessmentResponse,
} from '../types';

export interface LearningUnitOption {
  id: string;
  name: string;
  code?: string;
}

export interface LearningActivityOption {
  id: string;
  title: string;
  activityType?: string;
  activityDate?: string;
}

interface Props {
  visible: boolean;
  assessment?: Assessment | null;
  learningUnitId?: string | null;
  learningUnitTitle?: string | null;
  unitOptions?: LearningUnitOption[];
  learningActivities?: LearningActivityOption[];
}

const props = withDefaults(defineProps<Props>(), {
  assessment: null,
  learningUnitId: null,
  learningUnitTitle: null,
  unitOptions: () => [],
  learningActivities: () => [],
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved', response: CreateAssessmentResponse | UpdateAssessmentResponse): void;
  (e: 'created', response: CreateAssessmentResponse): void;
  (e: 'updated', response: UpdateAssessmentResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const isEditing = computed(() => Boolean(props.assessment?.id));

const dialogTitle = computed(() => {
  return isEditing.value ? 'Edit Assessment Configuration' : 'Plan New Assessment';
});

// Options Lists
const assessmentTypeOptions: Array<{ label: string; value: AssessmentType }> = [
  { label: 'Formative (Developmental / Routine Feedback)', value: 'formative' },
  { label: 'Summative (Certification / Official Grade)', value: 'summative' },
  { label: 'Diagnostic (Entrance / Baseline Readiness)', value: 'diagnostic' },
  { label: 'Portfolio (Cumulative Work Review)', value: 'portfolio' },
  { label: 'Project (Applied Practical Deliverable)', value: 'project' },
  { label: 'Other', value: 'other' },
];

const assessmentMethodOptions: Array<{ label: string; value: AssessmentMethod }> = [
  { label: 'Written Exam / Test Paper', value: 'written_exam' },
  { label: 'Oral Exam / Interview', value: 'oral_exam' },
  { label: 'Practical / Lab Demonstration', value: 'practical' },
  { label: 'Direct Classroom Observation', value: 'observation' },
  { label: 'Coursework Assignment', value: 'assignment' },
  { label: 'Student Presentation / Defense', value: 'presentation' },
  { label: 'Portfolio Review', value: 'portfolio' },
  { label: 'Peer Review', value: 'peer_review' },
  { label: 'Self Evaluation', value: 'self_evaluation' },
  { label: 'Other', value: 'other' },
];

const reassessmentPolicyOptions: Array<{
  label: string;
  value: ReassessmentPolicy;
  description: string;
}> = [
  {
    label: 'None (Single Attempt Only)',
    value: 'none',
    description: 'No retakes permitted for this evaluation task.',
  },
  {
    label: 'Capped at Passing (KKM)',
    value: 'capped_at_passing',
    description: 'Student may retake, but the operative score cannot exceed the passing score (KKM).',
  },
  {
    label: 'Highest Score',
    value: 'highest_score',
    description: 'Best score recorded across all attempts becomes operative.',
  },
  {
    label: 'Latest Attempt',
    value: 'latest_attempt',
    description: 'Most recent retake attempt unconditionally replaces previous scores.',
  },
  {
    label: 'Average of Attempts',
    value: 'average',
    description: 'Operative score is the arithmetic mean across all evaluation attempts.',
  },
];

// Unified form schema
const plannerFormSchema = z
  .object({
    learningUnitId: z.string().trim().min(1, 'Learning unit is required.'),
    learningActivityId: z.string().trim().optional().nullable().or(z.literal('')),
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(3, 'Title must be at least 3 characters.')
      .max(200, 'Title cannot exceed 200 characters.'),
    assessmentType: z.string().min(1, 'Assessment type is required.'),
    assessmentMethod: z.string().min(1, 'Assessment method is required.'),
    reassessmentPolicy: z.string().default('none'),
    maxScore: z
      .union([z.number(), z.string().transform((v) => (v === '' ? null : Number(v)))])
      .optional()
      .nullable(),
    passingScore: z
      .union([z.number(), z.string().transform((v) => (v === '' ? null : Number(v)))])
      .optional()
      .nullable(),
    criteria: z.string().trim().max(2000, 'Criteria cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
    assessmentDate: z
      .string()
      .trim()
      .optional()
      .nullable()
      .or(z.literal(''))
      .refine(
        (val) => {
          if (!val) return true;
          return ISO_DATE_REGEX.test(val) && isValidCalendarDate(val);
        },
        { message: 'Assessment date must be a valid calendar date in YYYY-MM-DD format.' },
      ),
    assessorReference: z.string().trim().max(200, 'Assessor reference cannot exceed 200 characters.').optional().nullable().or(z.literal('')),
    notes: z.string().trim().max(2000, 'Notes cannot exceed 2000 characters.').optional().nullable().or(z.literal('')),
  })
  .refine(
    (data) => {
      const max = data.maxScore != null ? Number(data.maxScore) : null;
      const pass = data.passingScore != null ? Number(data.passingScore) : null;
      if (max != null && pass != null && !isNaN(max) && !isNaN(pass)) {
        return pass <= max;
      }
      return true;
    },
    {
      message: 'Passing threshold (KKM) must be less than or equal to maximum score.',
      path: ['passingScore'],
    },
  );

type PlannerFormValues = z.infer<typeof plannerFormSchema>;

const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<PlannerFormValues>({
    validationSchema: toTypedSchema(plannerFormSchema),
    initialValues: {
      learningUnitId: props.learningUnitId ?? '',
      learningActivityId: '',
      title: '',
      assessmentType: 'formative',
      assessmentMethod: 'written_exam',
      reassessmentPolicy: 'none',
      maxScore: 100,
      passingScore: 75,
      criteria: '',
      assessmentDate: '',
      assessorReference: '',
      notes: '',
    },
  });

const [formLearningUnitId] = defineField('learningUnitId');
const [learningActivityId] = defineField('learningActivityId');
const [title] = defineField('title');
const [assessmentType] = defineField('assessmentType');
const [assessmentMethod] = defineField('assessmentMethod');
const [reassessmentPolicy] = defineField('reassessmentPolicy');
const [maxScore] = defineField('maxScore');
const [passingScore] = defineField('passingScore');
const [criteria] = defineField('criteria');
const [assessmentDate] = defineField('assessmentDate');
const [assessorReference] = defineField('assessorReference');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      if (props.assessment) {
        resetForm({
          values: {
            learningUnitId: props.assessment.learningUnitId,
            learningActivityId: props.assessment.learningActivityId ?? '',
            title: props.assessment.title,
            assessmentType: props.assessment.assessmentType,
            assessmentMethod: props.assessment.assessmentMethod,
            reassessmentPolicy: props.assessment.reassessmentPolicy ?? 'none',
            maxScore: props.assessment.maxScore ?? 100,
            passingScore: props.assessment.passingScore ?? 75,
            criteria: props.assessment.criteria ?? '',
            assessmentDate: props.assessment.assessmentDate ?? '',
            assessorReference: props.assessment.assessorReference ?? '',
            notes: props.assessment.notes ?? '',
          },
        });
      } else {
        resetForm({
          values: {
            learningUnitId: props.learningUnitId ?? (props.unitOptions[0]?.id ?? ''),
            learningActivityId: '',
            title: '',
            assessmentType: 'formative',
            assessmentMethod: 'written_exam',
            reassessmentPolicy: 'none',
            maxScore: 100,
            passingScore: 75,
            criteria: '',
            assessmentDate: new Date().toISOString().slice(0, 10),
            assessorReference: '',
            notes: '',
          },
        });
      }
    }
  },
  { immediate: true },
);

const createMutation = useCreateAssessmentMutation();
const updateMutation = useUpdateAssessmentMutation();

const onSubmit = handleSubmit(async (values) => {
  const max = values.maxScore != null && values.maxScore !== '' ? Number(values.maxScore) : null;
  const pass = values.passingScore != null && values.passingScore !== '' ? Number(values.passingScore) : null;

  if (isEditing.value && props.assessment) {
    const res = await updateMutation.mutateAsync({
      id: props.assessment.id,
      request: {
        title: values.title.trim(),
        criteria: values.criteria ? values.criteria.trim() : null,
        maxScore: max,
        passingScore: pass,
        reassessmentPolicy: values.reassessmentPolicy as ReassessmentPolicy,
        assessmentDate: values.assessmentDate ? values.assessmentDate.trim() : null,
        assessorReference: values.assessorReference ? values.assessorReference.trim() : null,
        notes: values.notes ? values.notes.trim() : null,
      },
    });
    emit('updated', res);
    emit('saved', res);
    isDialogVisible.value = false;
  } else {
    const res = await createMutation.mutateAsync({
      learningUnitId: values.learningUnitId,
      learningActivityId: values.learningActivityId ? values.learningActivityId.trim() : null,
      title: values.title.trim(),
      assessmentType: values.assessmentType as AssessmentType,
      assessmentMethod: values.assessmentMethod as AssessmentMethod,
      maxScore: max,
      passingScore: pass,
      reassessmentPolicy: values.reassessmentPolicy as ReassessmentPolicy,
      criteria: values.criteria ? values.criteria.trim() : null,
      assessmentDate: values.assessmentDate ? values.assessmentDate.trim() : null,
      assessorReference: values.assessorReference ? values.assessorReference.trim() : null,
      notes: values.notes ? values.notes.trim() : null,
    });
    emit('created', res);
    emit('saved', res);
    isDialogVisible.value = false;
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :header="dialogTitle"
    class="w-full max-w-2xl"
    :dismissable-mask="true"
    data-testid="assessment-planner-modal"
  >
    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Learning Unit (Selectable if not locked) -->
      <div v-if="!learningUnitTitle && unitOptions.length > 0 && !isEditing">
        <FormField
          id="planner-learning-unit"
          label="Target Learning Unit"
          :error="errors.learningUnitId"
          required
        >
          <Select
            v-model="formLearningUnitId"
            :options="unitOptions"
            option-label="name"
            option-value="id"
            placeholder="Select learning unit"
            class="w-full"
            data-testid="planner-unit-select"
          />
        </FormField>
      </div>
      <div
        v-else-if="learningUnitTitle"
        class="bg-slate-50 border border-slate-200 rounded-lg p-2.5 flex items-center gap-2 text-xs text-slate-700"
      >
        <Layers class="w-4 h-4 text-teal-600 shrink-0" />
        <span class="font-medium text-slate-900">Learning Unit:</span>
        <span>{{ learningUnitTitle }}</span>
      </div>

      <!-- Title -->
      <FormField
        id="planner-title"
        label="Assessment Title"
        :error="errors.title"
        required
        help="Give this evaluation task a clear, recognizable name (e.g., Mid-Term Diagnostic Exam, Essay Rubric Evaluation)."
      >
        <InputText
          v-model="title"
          placeholder="e.g. Unit 3 Formative Comprehension Quiz"
          class="w-full"
          data-testid="planner-title-input"
        />
      </FormField>

      <!-- Row: Assessment Type & Method -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <FormField
          id="planner-type"
          label="Assessment Type"
          :error="errors.assessmentType"
          required
        >
          <Select
            v-model="assessmentType"
            :options="assessmentTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select type"
            class="w-full"
            data-testid="planner-type-select"
          />
        </FormField>

        <FormField
          id="planner-method"
          label="Assessment Method"
          :error="errors.assessmentMethod"
          required
        >
          <Select
            v-model="assessmentMethod"
            :options="assessmentMethodOptions"
            option-label="label"
            option-value="value"
            placeholder="Select method"
            class="w-full"
            data-testid="planner-method-select"
          />
        </FormField>
      </div>

      <!-- Row: Scoring Thresholds (Max Score & KKM Passing Score) -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <FormField
          id="planner-max-score"
          label="Maximum Score"
          :error="errors.maxScore"
          help="Target scale maximum (e.g., 100)."
        >
          <div class="relative">
            <Scale class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="maxScore"
              type="number"
              placeholder="100"
              class="w-full pl-9"
              data-testid="planner-max-score-input"
            />
          </div>
        </FormField>

        <FormField
          id="planner-passing-score"
          label="Passing Threshold (KKM)"
          :error="errors.passingScore"
          help="Standard passing threshold (must be <= max score)."
        >
          <div class="relative">
            <Award class="w-4 h-4 text-amber-500 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="passingScore"
              type="number"
              placeholder="75"
              class="w-full pl-9"
              data-testid="planner-passing-score-input"
            />
          </div>
        </FormField>
      </div>

      <!-- Reassessment Policy Dropdown -->
      <FormField
        id="planner-reassessment-policy"
        label="Reassessment Policy"
        :error="errors.reassessmentPolicy"
        help="Governs how retakes and remediation attempts are scored (GAP-004, TD-03)."
      >
        <Select
          v-model="reassessmentPolicy"
          :options="reassessmentPolicyOptions"
          option-label="label"
          option-value="value"
          placeholder="Select policy"
          class="w-full"
          data-testid="planner-policy-select"
        />
      </FormField>

      <!-- Criteria / Rubric Descriptors -->
      <FormField
        id="planner-criteria"
        label="Criteria & Rubric Descriptors"
        :error="errors.criteria"
        help="Specify evaluation standards, competence criteria, or rubric grade band descriptors."
      >
        <Textarea
          v-model="criteria"
          rows="3"
          placeholder="e.g. Criterion A: Concept mastery (40%), Criterion B: Application and reasoning (40%), Criterion C: Presentation clarity (20%)."
          class="w-full text-sm"
          data-testid="planner-criteria-textarea"
        />
      </FormField>

      <!-- Row: Assessment Date & Assessor Reference -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <FormField
          id="planner-date"
          label="Scheduled Date"
          :error="errors.assessmentDate"
          help="YYYY-MM-DD"
        >
          <div class="relative">
            <Calendar class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="assessmentDate"
              placeholder="YYYY-MM-DD"
              class="w-full pl-9"
              data-testid="planner-date-input"
            />
          </div>
        </FormField>

        <FormField
          id="planner-assessor"
          label="Assessor Reference"
          :error="errors.assessorReference"
          help="Lead teacher, examiner, or room label."
        >
          <div class="relative">
            <UserCheck class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="assessorReference"
              placeholder="e.g. Dr. H. Drury"
              class="w-full pl-9"
              data-testid="planner-assessor-input"
            />
          </div>
        </FormField>
      </div>

      <!-- Parent Learning Activity (Optional) -->
      <div v-if="learningActivities.length > 0 && !isEditing">
        <FormField
          id="planner-activity"
          label="Linked Learning Activity (Optional)"
          :error="errors.learningActivityId"
          help="Optionally associate this assessment event with a scheduled class session or activity."
        >
          <Select
            v-model="learningActivityId"
            :options="learningActivities"
            option-label="title"
            option-value="id"
            placeholder="None (Independent assessment)"
            show-clear
            class="w-full"
            data-testid="planner-activity-select"
          />
        </FormField>
      </div>

      <!-- Notes -->
      <FormField
        id="planner-notes"
        label="Administrative Notes"
        :error="errors.notes"
      >
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Additional context or teacher instructions..."
          class="w-full text-sm"
          data-testid="planner-notes-textarea"
        />
      </FormField>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          :label="isEditing ? 'Save Changes' : 'Plan Assessment'"
          icon="pi pi-check"
          size="small"
          :loading="isSubmitting"
          data-testid="planner-submit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
