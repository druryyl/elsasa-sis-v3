<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { refDebounced } from '@vueuse/core';
import {
  Award,
  Search,
  User,
  BookOpen,
  Calendar,
  Building,
  ShieldCheck,
  CheckCircle2,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField, LoadingSpinner } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  recognizePriorCreditSchema,
  type RecognizePriorCreditFormValues,
} from '../schemas/participation.schema';
import { useRecognizePriorCreditMutation } from '../composables/useAcademicParticipation';
import { useStudentSearchQuery } from '@/features/students/records/composables/useStudentRecordsQueries';
import type { RecognizePriorCreditResponse } from '../types';

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
  (e: 'recognized', response: RecognizePriorCreditResponse): void;
}>();

const authStore = useAuthStore();
const currentUserName = computed(
  () =>
    authStore.user?.username ??
    authStore.user?.email ??
    'Academic Authority',
);

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

const typeOptions = [
  { label: 'Transferred from External Institution (transferred_in)', value: 'transferred_in' },
  { label: 'Prior Learning Recognition / Equivalency (recognized_prior)', value: 'recognized_prior' },
];

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<RecognizePriorCreditFormValues>({
    validationSchema: toTypedSchema(recognizePriorCreditSchema),
    initialValues: {
      studentId: '',
      learningUnitId: props.initialUnitId ?? '',
      academicPeriodId: props.initialPeriodId ?? '',
      academicPeriodLabel: '',
      participationType: 'transferred_in',
      sourceInstitution: '',
      externalGradeOrCredit: '',
      recognitionBasis: '',
      startDate: todayIso,
      completionDate: todayIso,
      notes: '',
    },
  });

const [studentId] = defineField('studentId');
const [learningUnitId] = defineField('learningUnitId');
const [academicPeriodId] = defineField('academicPeriodId');
const [academicPeriodLabel] = defineField('academicPeriodLabel');
const [participationType] = defineField('participationType');
const [sourceInstitution] = defineField('sourceInstitution');
const [externalGradeOrCredit] = defineField('externalGradeOrCredit');
const [recognitionBasis] = defineField('recognitionBasis');
const [startDate] = defineField('startDate');
const [completionDate] = defineField('completionDate');
const [notes] = defineField('notes');

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
          participationType: 'transferred_in',
          sourceInstitution: '',
          externalGradeOrCredit: '',
          recognitionBasis: '',
          startDate: todayIso,
          completionDate: todayIso,
          notes: '',
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
}

const recognizeMutation = useRecognizePriorCreditMutation();
const isSubmitting = computed(() => recognizeMutation.isPending.value);

const onSubmit = handleSubmit(async (values) => {
  try {
    const result = await recognizeMutation.mutateAsync({
      studentId: values.studentId,
      learningUnitId: values.learningUnitId,
      academicPeriodId: values.academicPeriodId,
      academicPeriodLabel: values.academicPeriodLabel,
      participationType: values.participationType,
      sourceInstitution: values.sourceInstitution?.trim() || null,
      externalGradeOrCredit: values.externalGradeOrCredit?.trim() || null,
      recognitionBasis: values.recognitionBasis?.trim() || null,
      startDate: values.startDate,
      completionDate: values.completionDate,
      studentLearningPlanId: values.studentLearningPlanId || null,
      notes: values.notes?.trim() || null,
    });

    emit('recognized', result);
    isDialogVisible.value = false;
  } catch {
    // Handled via toast
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Recognize Prior Credit / Transfer"
    :style="{ width: '680px' }"
    :closable="!isSubmitting"
  >
    <form class="space-y-4" @submit.prevent="onSubmit">
      <!-- Statutory Transfer Credit Notice -->
      <div
        class="flex items-start gap-3 rounded-lg border border-teal-200 bg-teal-50/70 p-3 text-xs text-teal-950 dark:border-teal-800/60 dark:bg-teal-950/30 dark:text-teal-200"
      >
        <Award class="size-4 shrink-0 text-teal-600 dark:text-teal-400" />
        <div class="space-y-1">
          <p class="font-semibold">Transfer Credit & Prior Learning Policy (GAP-08, UC-ACD-11)</p>
          <p class="leading-relaxed">
            Recognized prior learning or transfer credits are recorded with immediate <strong>Completed</strong> status and stamped with the reviewing Academic Authority.
          </p>
        </div>
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

        <!-- Student Search Input and Results -->
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

          <!-- Search results dropdown -->
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

          <!-- Direct UUID fallback input -->
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

      <!-- Academic Period & Learning Unit -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Academic Period"
          required
          :error="errors.academicPeriodId"
          help-text="Reporting academic period"
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
          label="Credited Learning Unit"
          required
          :error="errors.learningUnitId"
          help-text="Learning unit recognized"
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

      <!-- Recognition Type & Transfer Institution -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Recognition Classification"
          required
          :error="errors.participationType"
          help-text="Transfer or prior learning"
        >
          <Select
            v-model="participationType"
            :options="typeOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Source Institution"
          :error="errors.sourceInstitution"
          help-text="External issuing school or academy"
        >
          <InputText
            v-model="sourceInstitution"
            placeholder="e.g. St. Jude Academy"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>

      <!-- External Grade/Credit & Dates -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <FormField
          label="External Grade / Credit"
          :error="errors.externalGradeOrCredit"
          help-text="Grade from external transcript"
        >
          <InputText
            v-model="externalGradeOrCredit"
            placeholder="e.g. A / 3.0 credits"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Commencement Date"
          required
          :error="errors.startDate"
          help-text="Start date (YYYY-MM-DD)"
        >
          <InputText
            v-model="startDate"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Recognition Date"
          required
          :error="errors.completionDate"
          help-text="Date finalized (YYYY-MM-DD)"
        >
          <InputText
            v-model="completionDate"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>

      <!-- Recognition Basis Textarea -->
      <FormField
        label="Recognition Basis & Equivalency Rationale"
        :error="errors.recognitionBasis"
        help-text="Description of transcript verification, syllabus match, or examination equivalency"
      >
        <Textarea
          v-model="recognitionBasis"
          rows="2"
          class="w-full text-sm"
          placeholder="e.g. Transcript verified; syllabus reviewed and confirmed equivalent to Level 2 requirements by Academic Committee."
          :disabled="isSubmitting"
        />
      </FormField>

      <!-- Authority Approval Stamp Banner -->
      <div
        class="flex items-center justify-between rounded-lg border border-surface-200 bg-surface-50 p-3 text-xs text-surface-700 dark:border-surface-700 dark:bg-surface-900/60 dark:text-surface-300"
      >
        <div class="flex items-center gap-2">
          <ShieldCheck class="size-4 text-emerald-600 dark:text-emerald-400" />
          <span>Authority Stamp: <strong>{{ currentUserName }}</strong></span>
        </div>
        <span class="text-[11px] text-surface-500">Stamp will be recorded in audit log</span>
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
          label="Approve & Recognize Credit"
          icon="pi pi-check-circle"
          severity="success"
          :loading="isSubmitting"
          :disabled="!studentId"
        />
      </div>
    </form>
  </Dialog>
</template>
