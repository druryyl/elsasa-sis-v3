<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  AlertTriangle,
  Calendar,
  FileText,
  GraduationCap,
  Sparkles,
  UserCheck,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { initiateInterventionSchema } from '../schemas/intervention.schema';
import { useInitiateInterventionMutation } from '../composables/useAcademicInterventions';
import type {
  AcademicIntervention,
  AtRiskLearner,
  InitiateInterventionRequest,
  InterventionType,
  TriggerType,
} from '../types';
import { INTERVENTION_TYPES, TRIGGER_TYPES } from '../types';

export interface InterventionPlanInitialData {
  studentId?: string;
  studentName?: string;
  studentIdentifier?: string;
  learningUnitId?: string;
  learningUnitName?: string;
  learningUnitCode?: string;
  academicPeriodId?: string;
  triggerType?: TriggerType;
  triggerDescription?: string;
  suggestedType?: InterventionType;
}

interface Props {
  visible: boolean;
  initialData?: InterventionPlanInitialData | AtRiskLearner | null;
  activePeriodId?: string | null;
  activePeriodLabel?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  initialData: null,
  activePeriodId: null,
  activePeriodLabel: null,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', intervention: AcademicIntervention): void;
}>();

const isVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Dropdown options
const interventionTypeOptions: Array<{ label: string; value: InterventionType; description: string }> = [
  {
    label: 'Targeted Remediation',
    value: 'remediation',
    description: 'Structured remedial lessons focused on learning gaps',
  },
  {
    label: 'Reassessment / Retest',
    value: 'reassessment',
    description: 'Opportunity to demonstrate mastery on reassessment',
  },
  {
    label: 'Tutoring Recommendation',
    value: 'tutoring_recommendation',
    description: 'Assigned peer or specialized faculty tutoring sessions',
  },
  {
    label: 'Extension Work (Enrichment)',
    value: 'extension_work',
    description: 'Advanced enrichment work for high-potential or Olympiad preparation',
  },
  {
    label: 'Credit Recovery Plan',
    value: 'recovery_plan',
    description: 'Structured comprehensive plan for unit credit recovery',
  },
  {
    label: 'Learning Plan Adjustment',
    value: 'learning_plan_adjustment',
    description: 'Accommodation or pacing adjustments to individualized plan',
  },
  {
    label: 'Academic Warning',
    value: 'academic_warning',
    description: 'Formal notification regarding persistent standing concern',
  },
];

const triggerTypeOptions: Array<{ label: string; value: TriggerType }> = [
  { label: 'Assessment Below Threshold (Failing Score)', value: 'assessment_below_threshold' },
  { label: 'Missing / Overdue Assignments', value: 'missing_work' },
  { label: 'Attendance Concern (< 75%)', value: 'attendance_concern' },
  { label: 'Academic Standing Concern', value: 'standing_concern' },
  { label: 'Teacher / Educator Referral', value: 'teacher_referral' },
];

// Form fields
const studentId = ref('');
const studentDisplayName = ref('');
const learningUnitId = ref('');
const learningUnitDisplayName = ref('');
const academicPeriodId = ref('');
const interventionType = ref<InterventionType>('remediation');
const triggerType = ref<TriggerType>('teacher_referral');
const triggerDescription = ref('');
const milestoneTargetDate = ref('');
const planDescription = ref('');

// Field validation errors
const errors = ref<Record<string, string>>({});

// Mutation
const initiateMutation = useInitiateInterventionMutation();

const isPrepopulatedFromAtRisk = computed(() => {
  return Boolean(props.initialData?.studentId && props.initialData?.triggerType !== 'teacher_referral');
});

// Populate or reset form whenever dialog becomes visible or initialData changes
watch(
  [() => props.visible, () => props.initialData],
  ([visible, init]) => {
    if (visible) {
      errors.value = {};
      const fallbackPeriodId = props.activePeriodId || '00000000-0000-0000-0000-000000000001';
      
      if (init) {
        studentId.value = init.studentId || '';
        studentDisplayName.value =
          'studentName' in init && init.studentName
            ? `${init.studentName} ${init.studentIdentifier ? `(${init.studentIdentifier})` : ''}`
            : init.studentId || '';

        learningUnitId.value = init.learningUnitId || '';
        learningUnitDisplayName.value =
          'learningUnitName' in init && init.learningUnitName
            ? `${init.learningUnitName} ${init.learningUnitCode ? `(${init.learningUnitCode})` : ''}`
            : init.learningUnitId || '';

        academicPeriodId.value = init.academicPeriodId || fallbackPeriodId;
        triggerType.value = init.triggerType || 'teacher_referral';
        triggerDescription.value = init.triggerDescription || '';
        
        // Choose intelligent default intervention type based on trigger
        if ('suggestedType' in init && init.suggestedType) {
          interventionType.value = init.suggestedType;
        } else if (init.triggerType === 'assessment_below_threshold') {
          interventionType.value = 'remediation';
        } else if (init.triggerType === 'attendance_concern') {
          interventionType.value = 'recovery_plan';
        } else if (init.triggerType === 'missing_work') {
          interventionType.value = 'learning_plan_adjustment';
        } else {
          interventionType.value = 'remediation';
        }

        milestoneTargetDate.value = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
          .toISOString()
          .slice(0, 10);
        planDescription.value = '';
      } else {
        studentId.value = '';
        studentDisplayName.value = '';
        learningUnitId.value = '';
        learningUnitDisplayName.value = '';
        academicPeriodId.value = fallbackPeriodId;
        interventionType.value = 'remediation';
        triggerType.value = 'teacher_referral';
        triggerDescription.value = '';
        milestoneTargetDate.value = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
          .toISOString()
          .slice(0, 10);
        planDescription.value = '';
      }
    }
  },
  { immediate: true },
);

function validateForm(): boolean {
  errors.value = {};

  // Build the effective plan description including milestone date if specified
  const effectivePlan = milestoneTargetDate.value
    ? `${planDescription.value.trim()}\n\n[Milestone Target Date: ${milestoneTargetDate.value}]`
    : planDescription.value.trim();

  const payloadToValidate = {
    studentId: studentId.value.trim(),
    learningUnitId: learningUnitId.value.trim(),
    academicPeriodId: academicPeriodId.value.trim(),
    interventionType: interventionType.value,
    triggerType: triggerType.value,
    triggerDescription: triggerDescription.value.trim(),
    planDescription: effectivePlan,
  };

  const result = initiateInterventionSchema.safeParse(payloadToValidate);

  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0]?.toString() ?? 'general';
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return false;
  }

  return true;
}

async function handleSubmit() {
  if (!validateForm()) return;

  const effectivePlan = milestoneTargetDate.value
    ? `${planDescription.value.trim()}\n\n[Milestone Target Date: ${milestoneTargetDate.value}]`
    : planDescription.value.trim();

  const payload: InitiateInterventionRequest = {
    studentId: studentId.value.trim(),
    learningUnitId: learningUnitId.value.trim(),
    academicPeriodId: academicPeriodId.value.trim(),
    interventionType: interventionType.value,
    triggerType: triggerType.value,
    triggerDescription: triggerDescription.value.trim(),
    planDescription: effectivePlan,
  };

  try {
    const created = await initiateMutation.mutateAsync(payload);
    emit('created', created);
    isVisible.value = false;
  } catch {
    // Handled by toast in composable
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    modal
    :header="isPrepopulatedFromAtRisk ? 'Formulate Academic Intervention Plan' : 'Initiate Educator Intervention Referral'"
    :style="{ width: '640px', maxWidth: '95vw' }"
    data-testid="intervention-plan-modal"
  >
    <div class="space-y-4 py-2">
      <!-- Context Notification for Prepopulated At-Risk Row -->
      <div
        v-if="isPrepopulatedFromAtRisk"
        class="flex items-start gap-3 p-3 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 rounded-lg text-xs text-amber-800 dark:text-amber-200"
      >
        <AlertTriangle class="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
        <div>
          <span class="font-semibold">Early Warning Indicator Detected:</span>
          This intervention is initiated from dynamic early-warning metrics. Student and trigger context have been pre-filled.
        </div>
      </div>

      <!-- Student & Unit Selection / Display -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
        <!-- Target Student -->
        <FormField
          label="Target Student"
          required
          :error="errors.studentId"
          hint="Student ID (UUID)"
        >
          <div v-if="studentDisplayName && isPrepopulatedFromAtRisk" class="p-2.5 bg-surface-100 dark:bg-surface-800 rounded border border-surface-200 dark:border-surface-700 text-xs font-semibold text-surface-900 dark:text-surface-100">
            {{ studentDisplayName }}
          </div>
          <InputText
            v-else
            v-model="studentId"
            placeholder="Student UUID (e.g. 11111111-1111-1111-1111-111111111111)"
            class="w-full text-xs"
            :invalid="Boolean(errors.studentId)"
            data-testid="intervention-student-id-input"
          />
        </FormField>

        <!-- Target Learning Unit -->
        <FormField
          label="Target Learning Unit"
          required
          :error="errors.learningUnitId"
          hint="Learning Unit ID (UUID)"
        >
          <div v-if="learningUnitDisplayName && isPrepopulatedFromAtRisk" class="p-2.5 bg-surface-100 dark:bg-surface-800 rounded border border-surface-200 dark:border-surface-700 text-xs font-semibold text-surface-900 dark:text-surface-100">
            {{ learningUnitDisplayName }}
          </div>
          <InputText
            v-else
            v-model="learningUnitId"
            placeholder="Learning Unit UUID"
            class="w-full text-xs"
            :invalid="Boolean(errors.learningUnitId)"
            data-testid="intervention-unit-id-input"
          />
        </FormField>
      </div>

      <!-- Academic Period Context -->
      <FormField
        label="Academic Period"
        required
        :error="errors.academicPeriodId"
        hint="Operational period for intervention"
      >
        <InputText
          v-model="academicPeriodId"
          placeholder="Academic Period UUID"
          class="w-full text-xs"
          :invalid="Boolean(errors.academicPeriodId)"
          data-testid="intervention-period-id-input"
        />
      </FormField>

      <!-- Row: Intervention Type & Trigger Type -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
        <!-- Intervention Type -->
        <FormField
          label="Intervention Type"
          required
          :error="errors.interventionType"
          hint="Structured learning support strategy"
        >
          <Select
            v-model="interventionType"
            :options="interventionTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Intervention Type"
            class="w-full text-xs"
            :invalid="Boolean(errors.interventionType)"
            data-testid="intervention-type-select"
          />
        </FormField>

        <!-- Trigger Type -->
        <FormField
          label="Trigger Source"
          required
          :error="errors.triggerType"
          hint="Operational basis or referral"
        >
          <Select
            v-model="triggerType"
            :options="triggerTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Trigger Type"
            class="w-full text-xs"
            :invalid="Boolean(errors.triggerType)"
            data-testid="intervention-trigger-type-select"
          />
        </FormField>
      </div>

      <!-- Trigger Description -->
      <FormField
        label="Trigger Description / Justification"
        required
        :error="errors.triggerDescription"
        hint="Contextual narrative explaining why intervention is needed (max 1000 chars)."
      >
        <Textarea
          v-model="triggerDescription"
          rows="2"
          placeholder="e.g. Scored 52/100 on Summative Unit Test (Passing: 65), missing 3 homework assignments."
          class="w-full text-xs"
          :invalid="Boolean(errors.triggerDescription)"
          maxlength="1000"
          data-testid="intervention-trigger-description-input"
        />
      </FormField>

      <!-- Milestone Target Dates -->
      <FormField
        label="Milestone Target Date"
        hint="Target milestone or evaluation review date"
      >
        <div class="relative">
          <InputText
            v-model="milestoneTargetDate"
            type="date"
            class="w-full text-xs"
            data-testid="intervention-milestone-date-input"
          />
        </div>
      </FormField>

      <!-- Detailed Action Plan -->
      <FormField
        label="Detailed Action Plan"
        required
        :error="errors.planDescription"
        hint="Pedagogical action steps, schedule, materials, and expected outcomes (min 5 chars)."
      >
        <Textarea
          v-model="planDescription"
          rows="4"
          placeholder="Outline specific remedial steps (e.g. 2 weekly tutoring sessions, review chapters 3-4, reassessment on target date)."
          class="w-full text-xs"
          :invalid="Boolean(errors.planDescription)"
          maxlength="2000"
          data-testid="intervention-plan-description-input"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
        <Button
          label="Cancel"
          severity="secondary"
          variant="text"
          class="text-xs"
          @click="isVisible = false"
          data-testid="btn-cancel-intervention"
        />
        <Button
          label="Formulate & Save Plan"
          icon="pi pi-check"
          class="text-xs"
          :loading="initiateMutation.isPending.value"
          @click="handleSubmit"
          data-testid="btn-submit-intervention"
        />
      </div>
    </template>
  </Dialog>
</template>
