<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { refDebounced } from '@vueuse/core';
import {
  UserPlus,
  Search,
  User,
  BookOpen,
  Calendar,
  Layers,
  Repeat,
  CheckCircle2,
  Sparkles,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField, LoadingSpinner } from '@/shared/components';
import {
  registerParticipationSchema,
  type RegisterParticipationFormValues,
} from '../schemas/participation.schema';
import { useRegisterParticipationMutation } from '../composables/useAcademicParticipation';
import { useStudentSearchQuery } from '@/features/students/records/composables/useStudentRecordsQueries';
import { useActiveLearningPlan } from '@/features/academics/learning-plans/composables/useLearningPlans';
import type { RegisterParticipationResponse } from '../types';

export interface AcademicPeriodOption {
  id: string;
  label: string;
  code?: string;
}

export interface LearningUnitOption {
  id: string;
  code: string;
  name: string;
  level?: string | null;
  creditWeight?: number | null;
}

const props = defineProps<{
  visible: boolean;
  periods: AcademicPeriodOption[];
  units: LearningUnitOption[];
  initialPeriodId?: string | null;
  initialUnitId?: string | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'registered', response: RegisterParticipationResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const todayIso = new Date().toISOString().slice(0, 10);

// Student Search State
const studentSearchTerm = ref('');
const debouncedStudentSearch = refDebounced(studentSearchTerm, 300);
const selectedStudentDisplay = ref<{ id: string; name: string; studentNumber?: string | null } | null>(null);

const { data: studentSearchResults, isLoading: isSearchingStudents } =
  useStudentSearchQuery(
    computed(() => ({
      search: debouncedStudentSearch.value.trim(),
      pageSize: 8,
    })),
    {
      enabled: computed(() => debouncedStudentSearch.value.trim().length >= 2),
    },
  );

const studentResultsList = computed(() => studentSearchResults.value?.items ?? []);

const participationTypeOptions = [
  { label: 'Standard Enrollment (First Attempt / Normal Course)', value: 'standard' },
  { label: 'Repeat / Remediation (Retake Attempt)', value: 'repeat' },
];

const sourceOptions = [
  { label: 'Assigned by Academic Coordinator', value: 'assigned' },
  { label: 'Student Elective Selection', value: 'student_selected' },
  { label: 'Personalized Learning Plan (SLP)', value: 'learning_plan' },
];

const defaultPeriod =
  props.periods.find((p) => p.id === props.initialPeriodId) ??
  props.periods[0] ??
  null;

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<RegisterParticipationFormValues>({
    validationSchema: toTypedSchema(registerParticipationSchema),
    initialValues: {
      studentId: '',
      learningUnitId: props.initialUnitId ?? (props.units[0]?.id ?? ''),
      academicPeriodId: defaultPeriod?.id ?? '',
      academicPeriodLabel: defaultPeriod?.label ?? '',
      sectionLabel: null,
      participationType: 'standard',
      source: 'assigned',
      startDate: todayIso,
      studentLearningPlanId: null,
    },
  });

const [studentId] = defineField('studentId');
const [learningUnitId] = defineField('learningUnitId');
const [academicPeriodId] = defineField('academicPeriodId');
const [academicPeriodLabel] = defineField('academicPeriodLabel');
const [sectionLabel] = defineField('sectionLabel');
const [participationType] = defineField('participationType');
const [source] = defineField('source');
const [startDate] = defineField('startDate');
const [studentLearningPlanId] = defineField('studentLearningPlanId');

// Query active student learning plan for the selected student & period (SCR-ACD-05, P7-S24)
const { data: activeLearningPlan, isLoading: isLoadingActivePlan } = useActiveLearningPlan(
  studentId,
  academicPeriodId,
  {
    enabled: computed(() => Boolean(studentId.value && academicPeriodId.value)),
  },
);

// When an active SLP is detected for the student, pre-select source = 'learning_plan' and pre-link active plan ID
watch(
  () => activeLearningPlan.value,
  (plan) => {
    if (plan && plan.planId) {
      setFieldValue('source', 'learning_plan');
      setFieldValue('studentLearningPlanId', plan.planId);
    }
  },
  { immediate: true },
);

watch(
  academicPeriodId,
  (newId) => {
    const selected = props.periods.find((p) => p.id === newId);
    if (selected) {
      setFieldValue('academicPeriodLabel', selected.label);
    }
  },
  { immediate: true },
);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      const defaultPeriod =
        props.periods.find((p) => p.id === props.initialPeriodId) ??
        props.periods[0] ??
        null;

      resetForm({
        values: {
          studentId: '',
          learningUnitId: props.initialUnitId ?? (props.units[0]?.id ?? ''),
          academicPeriodId: defaultPeriod?.id ?? '',
          academicPeriodLabel: defaultPeriod?.label ?? '',
          sectionLabel: null,
          participationType: 'standard',
          source: 'assigned',
          startDate: todayIso,
          studentLearningPlanId: null,
        },
      });
      studentSearchTerm.value = '';
      selectedStudentDisplay.value = null;
    }
  },
);

function selectStudent(student: { id: string; name: string; studentNumber?: string | null }) {
  selectedStudentDisplay.value = student;
  setFieldValue('studentId', student.id);
  studentSearchTerm.value = '';
}

function clearSelectedStudent() {
  selectedStudentDisplay.value = null;
  setFieldValue('studentId', '');
  setFieldValue('studentLearningPlanId', null);
}

const registerMutation = useRegisterParticipationMutation();
const isSubmitting = computed(() => registerMutation.isPending.value);

const calculatedAttemptNotice = computed(() => {
  if (participationType.value === 'repeat') {
    return 'Attempt counter will be calculated automatically by querying previous attempts for this student and learning unit (Attempt ≥ 2).';
  }
  return 'Initial enrollment will register as Attempt #1.';
});

const onSubmit = handleSubmit(async (values) => {
  try {
    const result = await registerMutation.mutateAsync({
      studentId: values.studentId,
      learningUnitId: values.learningUnitId,
      academicPeriodId: values.academicPeriodId,
      academicPeriodLabel: values.academicPeriodLabel,
      sectionLabel: values.sectionLabel?.trim() || null,
      participationType: values.participationType,
      source: values.source,
      startDate: values.startDate,
      studentLearningPlanId: values.studentLearningPlanId || activeLearningPlan.value?.planId || null,
    });

    emit('registered', result);
    isDialogVisible.value = false;
  } catch {
    // Errors handled via toast
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Register Student Participation"
    :style="{ width: '620px' }"
    :closable="!isSubmitting"
  >
    <form class="space-y-4" @submit.prevent="onSubmit">
      <!-- Info callout -->
      <div
        class="flex items-start gap-3 rounded-lg border border-primary-100 bg-primary-50/60 p-3 text-xs text-primary-900 dark:border-primary-900/40 dark:bg-primary-950/20 dark:text-primary-200"
      >
        <UserPlus class="size-4 shrink-0 text-primary-600 dark:text-primary-400" />
        <p class="leading-relaxed">
          Register an individual student into a learning unit (UC-ACD-10, TD-02). Active enrollment is validated synchronously. Repeat attempts auto-increment retake counters.
        </p>
      </div>

      <!-- Student Selection -->
      <div class="space-y-2">
        <label class="block text-sm font-medium text-surface-900 dark:text-surface-100">
          Target Student <span class="text-rose-500">*</span>
        </label>

        <!-- Selected Student Display -->
        <div
          v-if="selectedStudentDisplay"
          class="flex items-center justify-between rounded-lg border border-teal-200 bg-teal-50/70 p-3 text-sm text-teal-900 dark:border-teal-800/60 dark:bg-teal-950/30 dark:text-teal-200"
        >
          <div class="flex items-center gap-2">
            <User class="size-4 text-teal-600 dark:text-teal-400" />
            <span class="font-semibold">{{ selectedStudentDisplay.name }}</span>
            <span
              v-if="selectedStudentDisplay.studentNumber"
              class="text-xs text-teal-700 dark:text-teal-300"
            >
              ({{ selectedStudentDisplay.studentNumber }})
            </span>
          </div>
          <Button
            type="button"
            label="Change"
            size="small"
            variant="text"
            severity="secondary"
            class="p-0 text-xs text-teal-800 dark:text-teal-300"
            @click="clearSelectedStudent"
          />
        </div>

        <!-- Student Search Input and Results Dropdown -->
        <div v-else class="space-y-1">
          <div class="relative">
            <Search class="absolute left-3 top-2.5 size-4 text-surface-400" />
            <InputText
              v-model="studentSearchTerm"
              placeholder="Search student by name, student number, or UUID..."
              class="w-full pl-9 text-sm"
              :disabled="isSubmitting"
            />
            <div
              v-if="isSearchingStudents"
              class="absolute right-3 top-2.5"
            >
              <LoadingSpinner size="sm" />
            </div>
          </div>

          <!-- Search results list -->
          <div
            v-if="studentSearchTerm.trim().length >= 2 && studentResultsList.length > 0"
            class="max-h-40 overflow-y-auto rounded-lg border border-surface-200 bg-white p-1 shadow-md dark:border-surface-700 dark:bg-surface-800"
          >
            <div
              v-for="s in studentResultsList"
              :key="s.id"
              class="flex cursor-pointer items-center justify-between rounded px-3 py-1.5 text-xs hover:bg-surface-100 dark:hover:bg-surface-700"
              @click="
                selectStudent({
                  id: s.id,
                  name: `${s.firstName} ${s.lastName}`,
                  studentNumber: s.studentNumber,
                })
              "
            >
              <span class="font-medium text-surface-900 dark:text-surface-100">
                {{ s.firstName }} {{ s.lastName }}
              </span>
              <span class="text-surface-500">
                {{ s.studentNumber ?? s.id.slice(0, 8) }}
              </span>
            </div>
          </div>
          <p
            v-else-if="studentSearchTerm.trim().length >= 2 && !isSearchingStudents"
            class="text-[11px] text-surface-500"
          >
            No student found matching "{{ studentSearchTerm }}". You may also paste the raw UUID below.
          </p>

          <!-- Direct UUID fallback input if not selected via search -->
          <InputText
            v-if="!selectedStudentDisplay"
            v-model="studentId"
            placeholder="Or enter Student UUID directly"
            class="w-full text-xs font-mono"
            :disabled="isSubmitting"
          />
          <p v-if="errors.studentId" class="text-xs text-rose-500">
            {{ errors.studentId }}
          </p>
        </div>
      </div>

      <!-- Active Student Learning Plan Context Banner (SCR-ACD-05, P7-S24) -->
      <div
        v-if="activeLearningPlan"
        class="flex items-start gap-2.5 rounded-lg border border-indigo-200 bg-indigo-50/70 p-3 text-xs text-indigo-900 dark:border-indigo-800/60 dark:bg-indigo-950/30 dark:text-indigo-200"
        data-testid="modal-active-slp-callout"
      >
        <Sparkles class="size-4 shrink-0 text-indigo-600 dark:text-indigo-400 mt-0.5" />
        <div>
          <div class="font-semibold">Governed by Active Student Learning Plan (v{{ activeLearningPlan.version }})</div>
          <div class="text-[11px] text-indigo-700 dark:text-indigo-300 mt-0.5">
            Registration source automatically pre-selected to <strong>learning_plan</strong> and pre-linked to Plan ID <span class="font-mono font-medium">{{ activeLearningPlan.planId.slice(0, 8) }}</span>.
          </div>
        </div>
      </div>

      <!-- Academic Period & Learning Unit -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Academic Period"
          required
          :error="errors.academicPeriodId"
          help-text="Target academic operational term"
        >
          <Select
            v-model="academicPeriodId"
            :options="periods"
            option-label="label"
            option-value="id"
            placeholder="Select Academic Period"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Learning Unit"
          required
          :error="errors.learningUnitId"
          help-text="Subject or module being enrolled"
        >
          <Select
            v-model="learningUnitId"
            :options="units"
            option-label="name"
            option-value="id"
            placeholder="Select Learning Unit"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>

      <!-- Participation Type & Attempt Number Calculation -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Participation Type"
          required
          :error="errors.participationType"
          help-text="Standard or Repeat Attempt"
        >
          <Select
            v-model="participationType"
            :options="participationTypeOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Registration Source"
          required
          :error="errors.source"
          help-text="Originating policy source"
        >
          <Select
            v-model="source"
            :options="sourceOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>

      <!-- Attempt Notice Box -->
      <div
        class="flex items-center gap-2 rounded border border-surface-200 bg-surface-50 p-2.5 text-xs text-surface-600 dark:border-surface-700 dark:bg-surface-900/60 dark:text-surface-300"
      >
        <Repeat class="size-4 shrink-0 text-primary-500" />
        <span>{{ calculatedAttemptNotice }}</span>
      </div>

      <!-- Section Label & Start Date -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Section Partition (Optional)"
          :error="errors.sectionLabel"
          help-text="Cohort section (e.g. 10-A)"
        >
          <InputText
            v-model="sectionLabel"
            placeholder="e.g. 10-A"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Start Date"
          required
          :error="errors.startDate"
          help-text="Enrolled date (YYYY-MM-DD)"
        >
          <InputText
            v-model="startDate"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="text"
          :disabled="isSubmitting"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          label="Register Student"
          icon="pi pi-user-plus"
          :loading="isSubmitting"
          :disabled="!studentId"
        />
      </div>
    </form>
  </Dialog>
</template>
