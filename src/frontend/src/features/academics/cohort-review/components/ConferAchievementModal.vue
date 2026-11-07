<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  Award,
  BookOpen,
  Calendar,
  CheckCircle2,
  FileText,
  Sparkles,
  Trophy,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { conferAchievementSchema } from '../schemas/cohortReview.schema';
import { useConferAchievementMutation } from '../composables/useCohortReview';
import type {
  AcademicAchievement,
  AcademicAchievementType,
  ConferAchievementRequest,
} from '../types';
import { ACHIEVEMENT_TYPES } from '../types';

interface Props {
  visible: boolean;
  academicPeriodId: string;
  initialStudentId?: string | null;
  initialStudentName?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  initialStudentId: null,
  initialStudentName: null,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'conferred', achievement: AcademicAchievement): void;
}>();

const isVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Dropdown options
const achievementTypeOptions: Array<{ label: string; value: AcademicAchievementType; description: string }> = [
  {
    label: 'Academic Honors (Juara Umum / Honor Roll)',
    value: 'academic_honors',
    description: 'Cohort-wide academic distinction or top overall GPA recognition',
  },
  {
    label: 'Subject Distinction (Nilai Tertinggi)',
    value: 'subject_distinction',
    description: 'Outstanding mastery or highest final result in a specific subject',
  },
  {
    label: 'Mastery Recognition',
    value: 'mastery_recognition',
    description: 'Exemplary competency milestone achievement',
  },
  {
    label: 'Academic Competition Award',
    value: 'academic_competition',
    description: 'Curriculum-recognized academic Olympiad or inter-school competition',
  },
  {
    label: 'Academic Certification',
    value: 'academic_certification',
    description: 'Formal academic certification conferred for specialized track',
  },
];

// Form fields
const studentId = ref('');
const studentDisplayName = ref('');
const periodId = ref('');
const learningUnitId = ref('');
const achievementType = ref<AcademicAchievementType>('academic_honors');
const title = ref('');
const description = ref('');
const criteriaMet = ref('');
const supportingEvidence = ref('');
const recognizedAt = ref(new Date().toISOString().slice(0, 10));

const errors = ref<Record<string, string>>({});

// Mutation
const { mutate: conferMutation, isPending: isSubmitting } = useConferAchievementMutation({
  onSuccess: (data) => {
    emit('conferred', data);
    isVisible.value = false;
  },
});

watch(
  () => props.visible,
  (val) => {
    if (val) {
      errors.value = {};
      studentId.value = props.initialStudentId || '';
      studentDisplayName.value = props.initialStudentName || '';
      periodId.value = props.academicPeriodId || '';
      learningUnitId.value = '';
      achievementType.value = 'academic_honors';
      title.value = '';
      description.value = '';
      criteriaMet.value = '';
      supportingEvidence.value = '';
      recognizedAt.value = new Date().toISOString().slice(0, 10);
    }
  },
);

function handleSubmit() {
  errors.value = {};

  const payload: ConferAchievementRequest = {
    studentId: studentId.value.trim(),
    academicPeriodId: periodId.value.trim(),
    learningUnitId: learningUnitId.value ? learningUnitId.value.trim() : null,
    achievementType: achievementType.value,
    title: title.value.trim(),
    description: description.value ? description.value.trim() : null,
    criteriaMet: criteriaMet.value.trim(),
    supportingEvidence: supportingEvidence.value ? supportingEvidence.value.trim() : null,
    recognizedAt: recognizedAt.value.trim(),
  };

  const validation = conferAchievementSchema.safeParse(payload);
  if (!validation.success) {
    for (const issue of validation.error.issues) {
      const field = issue.path[0] as string;
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return;
  }

  conferMutation(payload);
}
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    modal
    :draggable="false"
    class="w-full max-w-xl mx-4"
    data-testid="confer-achievement-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <Trophy class="w-5 h-5 text-amber-500" />
        <span class="font-bold text-base text-surface-900 dark:text-surface-100">
          Confer Academic Achievement (UC-ACD-21)
        </span>
      </div>
    </template>

    <div class="space-y-4 pt-2">
      <!-- Banner -->
      <div class="flex items-start gap-3 p-3 bg-amber-50/60 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800 rounded-xl text-xs text-amber-900 dark:text-amber-200">
        <Sparkles class="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
        <p>
          Formally confers an academic honor, subject distinction (*Nilai Tertinggi*), or curriculum competition award to be recorded permanently on the student's authoritative academic transcript.
        </p>
      </div>

      <!-- Student & Period IDs -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          label="Student ID (UUID)"
          :error="errors.studentId"
          :help="studentDisplayName ? `Selected: ${studentDisplayName}` : undefined"
          required
        >
          <InputText
            v-model="studentId"
            placeholder="Student UUID"
            class="w-full text-xs"
            data-testid="input-achievement-student-id"
          />
        </FormField>

        <FormField label="Academic Period ID (UUID)" :error="errors.academicPeriodId" required>
          <InputText
            v-model="periodId"
            placeholder="Period UUID"
            class="w-full text-xs"
            data-testid="input-achievement-period-id"
          />
        </FormField>
      </div>

      <!-- Achievement Type -->
      <FormField label="Achievement Category" :error="errors.achievementType" required>
        <Select
          v-model="achievementType"
          :options="achievementTypeOptions"
          option-label="label"
          option-value="value"
          class="w-full text-xs"
          data-testid="select-achievement-type"
        />
      </FormField>

      <!-- Achievement Title & Recognition Date -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div class="sm:col-span-2">
          <FormField label="Achievement Title" :error="errors.title" required>
            <InputText
              v-model="title"
              placeholder="e.g. Juara Umum Grade 4, Nilai Tertinggi Matematika"
              class="w-full text-xs"
              data-testid="input-achievement-title"
            />
          </FormField>
        </div>

        <div>
          <FormField label="Recognition Date" :error="errors.recognizedAt" required>
            <InputText
              v-model="recognizedAt"
              placeholder="YYYY-MM-DD"
              class="w-full text-xs"
              data-testid="input-achievement-date"
            />
          </FormField>
        </div>
      </div>

      <!-- Optional Learning Unit ID -->
      <FormField
        v-if="achievementType === 'subject_distinction' || achievementType === 'mastery_recognition'"
        label="Linked Learning Unit ID (Optional)"
        :error="errors.learningUnitId"
      >
        <InputText
          v-model="learningUnitId"
          placeholder="Learning Unit UUID (if applicable)"
          class="w-full text-xs"
          data-testid="input-achievement-unit-id"
        />
      </FormField>

      <!-- Criteria Met -->
      <FormField label="Evaluation Criteria Met" :error="errors.criteriaMet" required>
        <Textarea
          v-model="criteriaMet"
          rows="2"
          placeholder="e.g. Attained the highest cumulative score in cohort with 96.5% average and no remediation."
          class="w-full text-xs"
          data-testid="textarea-achievement-criteria"
        />
      </FormField>

      <!-- Supporting Academic Evidence -->
      <FormField label="Supporting Result Evidence (Optional)" :error="errors.supportingEvidence">
        <Textarea
          v-model="supportingEvidence"
          rows="2"
          placeholder="e.g. Official S1 Final Approved Result Grade 4 Report Card."
          class="w-full text-xs"
          data-testid="textarea-achievement-evidence"
        />
      </FormField>

      <!-- General Description -->
      <FormField label="Citation / Description (Optional)" :error="errors.description">
        <Textarea
          v-model="description"
          rows="2"
          placeholder="Additional ceremonial citation or commendation notes..."
          class="w-full text-xs"
          data-testid="textarea-achievement-description"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          label="Cancel"
          variant="text"
          severity="secondary"
          size="small"
          class="text-xs"
          @click="isVisible = false"
        />
        <Button
          label="Confer Achievement"
          icon="pi pi-check"
          severity="primary"
          size="small"
          class="text-xs"
          :loading="isSubmitting"
          @click="handleSubmit"
          data-testid="btn-submit-achievement"
        />
      </div>
    </template>
  </Dialog>
</template>
