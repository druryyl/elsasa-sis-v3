<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { CheckCircle2 } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import {
  assessmentEvidenceSchema,
  type AssessmentEvidenceFormData,
} from '../../schemas/admission.schema';
import {
  useAddAssessmentEvidenceMutation,
  useUpdateAssessmentEvidenceMutation,
} from '../../composables/useAdmissionMutations';
import type { AssessmentEvidenceDto } from '../../types/admission.types';

const props = defineProps<{
  visible: boolean;
  applicationId: string;
  evidence?: AssessmentEvidenceDto | null | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved'): void;
}>();

const isEditMode = computed(() => Boolean(props.evidence?.id));

const form = ref<AssessmentEvidenceFormData>({
  assessmentType: '',
  score: '',
  source: 'External Assessment Center',
  assessmentDate: formatDateOnly(new Date()),
  notes: '',
});

const errors = ref<Record<string, string>>({});

watch(
  () => [props.visible, props.evidence] as const,
  ([isOpen, ev]) => {
    if (isOpen) {
      errors.value = {};
      form.value = {
        assessmentType: ev?.assessmentType ?? '',
        score: ev?.score ?? '',
        source: ev?.source ?? 'External Assessment Center',
        assessmentDate: ev?.assessmentDate ?? formatDateOnly(new Date()),
        notes: ev?.notes ?? '',
      };
    }
  },
  { immediate: true },
);

const addMutation = useAddAssessmentEvidenceMutation({
  onSuccess: () => {
    emit('saved');
    emit('update:visible', false);
  },
});

const updateMutation = useUpdateAssessmentEvidenceMutation({
  onSuccess: () => {
    emit('saved');
    emit('update:visible', false);
  },
});

const isSubmitting = computed(
  () => addMutation.isPending.value || updateMutation.isPending.value,
);

function handleSubmit() {
  errors.value = {};
  const result = assessmentEvidenceSchema.safeParse(form.value);
  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] as string;
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return;
  }

  if (isEditMode.value && props.evidence?.id) {
    updateMutation.mutate({
      applicationId: props.applicationId,
      evidenceId: props.evidence.id,
      payload: {
        assessmentType: result.data.assessmentType,
        score: result.data.score?.trim() || null,
        source: result.data.source?.trim() || 'External Assessment Center',
        assessmentDate: result.data.assessmentDate?.trim() || null,
        notes: result.data.notes?.trim() || null,
      },
    });
  } else {
    addMutation.mutate({
      applicationId: props.applicationId,
      payload: {
        assessmentType: result.data.assessmentType,
        score: result.data.score?.trim() || null,
        source: result.data.source?.trim() || 'External Assessment Center',
        assessmentDate: result.data.assessmentDate?.trim() || null,
        notes: result.data.notes?.trim() || null,
      },
    });
  }
}

function handleClose() {
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :header="isEditMode ? 'Edit Entrance Assessment Diagnostic Evidence' : 'Add Entrance Assessment Diagnostic Evidence'"
    class="max-w-md w-full"
    data-testid="assessment-evidence-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 pt-1 text-xs" @submit.prevent="handleSubmit">
      <!-- Assessment Type -->
      <FormField label="Assessment Type / Instrument" required :error="errors.assessmentType">
        <InputText
          v-model="form.assessmentType"
          type="text"
          placeholder="e.g. Reading Readiness, Math Readiness, Psychological Evaluation"
          class="w-full !text-xs !py-1.5"
          data-testid="evidence-type-input"
        />
      </FormField>

      <!-- Score / Result -->
      <FormField label="Score / Result" :error="errors.score">
        <InputText
          v-model="form.score"
          type="text"
          placeholder="e.g. 88/100, Band 5, Passed, Recommended"
          class="w-full !text-xs !py-1.5 font-mono"
          data-testid="evidence-score-input"
        />
      </FormField>

      <!-- Source of Assessment -->
      <FormField label="Source / Administration Body" required :error="errors.source">
        <InputText
          v-model="form.source"
          type="text"
          placeholder="e.g. External Assessment Center, School Clinical Psychologist"
          class="w-full !text-xs !py-1.5"
          data-testid="evidence-source-input"
        />
      </FormField>

      <!-- Assessment Date -->
      <FormField label="Date Administered" :error="errors.assessmentDate">
        <InputText
          v-model="form.assessmentDate"
          type="date"
          class="w-full !text-xs !py-1.5"
          data-testid="evidence-date-input"
        />
      </FormField>

      <!-- Evaluator Notes -->
      <FormField label="Evaluator Observations / Notes" :error="errors.notes">
        <Textarea
          v-model="form.notes"
          rows="3"
          placeholder="Enter qualitative observations, diagnostic findings, or learning recommendations..."
          class="w-full !text-xs"
          data-testid="evidence-notes-textarea"
        />
      </FormField>

      <!-- Footer Actions -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="!text-xs cursor-pointer"
          @click="handleClose"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="small"
          severity="primary"
          :loading="isSubmitting"
          class="!text-xs cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700"
          data-testid="confirm-evidence-button"
        >
          <CheckCircle2 class="w-3.5 h-3.5 mr-1" />
          <span>{{ isEditMode ? 'Update Evidence' : 'Save Evidence' }}</span>
        </Button>
      </div>
    </form>
  </Dialog>
</template>
