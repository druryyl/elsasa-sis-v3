<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import DatePicker from 'primevue/datepicker';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import {
  GraduationCap,
  Building2,
  Calendar,
  Layers,
  ArrowRight,
  ShieldCheck,
  CheckCircle2,
  AlertCircle,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatDateOnly, formatDisplayDate, parseIsoDate } from '@/shared/utils/dateUtils';
import { changePlacementSchema, type ChangePlacementFormData } from '@/features/students/records/schemas/records.schema';
import { useChangePlacementMutation } from '@/features/students/records/composables/useStudentRecordsMutations';
import type { StudentListItem, StudentDetail, AdministrativePlacementDto } from '@/features/students/records/types/records.types';
import ProgressionRecommendationBadge from './ProgressionRecommendationBadge.vue';
import { useStudentProgressionRecommendation } from '../composables/useProgressionRecommendation';
import type { ProgressionRecommendationDto } from '../types';

const props = defineProps<{
  visible: boolean;
  student: StudentListItem | StudentDetail | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success'): void;
}>();

const studentId = computed(() => props.student?.id || '');

const { latestRecommendation, isLoading: isLoadingRecommendation } =
  useStudentProgressionRecommendation(studentId);

const currentPlacement = computed(() => {
  if (!props.student) return null;
  if ('currentPlacement' in props.student && props.student.currentPlacement) {
    return props.student.currentPlacement;
  }
  return null;
});

const isPromotionUnblocked = computed(() => {
  const rec = latestRecommendation.value;
  if (!rec) return true; // manual transitions allowed if no block
  return rec.recommendation === 'promotion_eligible' || rec.recommendation === 'conditional_promotion';
});

const isRetention = computed(() => {
  return latestRecommendation.value?.recommendation === 'retention_recommended';
});

const { handleSubmit, errors, defineField, resetForm } =
  useForm<ChangePlacementFormData>({
    validationSchema: toTypedSchema(changePlacementSchema),
    initialValues: {
      campus: '',
      schoolLevel: '',
      grade: '',
      academicYear: '',
      effectiveDate: formatDateOnly(new Date()),
      program: '',
      stream: '',
      cohort: '',
      homeroom: '',
      reason: '',
      authorizedBy: '',
      decisionReference: '',
    },
  });

const [campus] = defineField('campus');
const [schoolLevel] = defineField('schoolLevel');
const [grade] = defineField('grade');
const [academicYear] = defineField('academicYear');
const [effectiveDate] = defineField('effectiveDate');
const [program] = defineField('program');
const [stream] = defineField('stream');
const [cohort] = defineField('cohort');
const [homeroom] = defineField('homeroom');
const [reason] = defineField('reason');
const [authorizedBy] = defineField('authorizedBy');
const [decisionReference] = defineField('decisionReference');

const datePickerDate = ref<Date | null>(new Date());

watch(
  () => effectiveDate.value,
  (val) => {
    if (val) {
      const parsed = parseIsoDate(val);
      if (!isNaN(parsed.getTime())) {
        datePickerDate.value = parsed;
      }
    }
  },
);

watch(datePickerDate, (newDate) => {
  if (newDate instanceof Date && !isNaN(newDate.getTime())) {
    effectiveDate.value = formatDateOnly(newDate);
  }
});

function populateForm() {
  if (!props.student) return;
  const s = props.student as any;
  const rec = latestRecommendation.value;

  const targetGrade = rec?.recommendedToLevel || s.currentPlacement?.grade || s.grade || '';
  const initialReason = rec
    ? `Administrative grade transition based on authoritative Academic progression recommendation (${rec.recommendation}).`
    : 'Administrative placement change.';

  resetForm({
    values: {
      campus: s.currentPlacement?.campus || s.campus || '',
      schoolLevel: s.currentPlacement?.schoolLevel || s.schoolLevel || s.level || '',
      grade: targetGrade,
      academicYear: s.currentPlacement?.academicYear || '',
      effectiveDate: formatDateOnly(new Date()),
      program: s.currentPlacement?.program || '',
      stream: s.currentPlacement?.stream || '',
      cohort: s.currentPlacement?.cohort || '',
      homeroom: s.currentPlacement?.homeroom || '',
      reason: initialReason,
      authorizedBy: rec?.authorizedBy || '',
      decisionReference: rec?.committeeReference || '',
    },
  });
}

watch(
  () => [props.visible, latestRecommendation.value],
  ([isOpen]) => {
    if (isOpen) {
      populateForm();
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:visible', false);
}

const { mutateAsync: changePlacement, isPending: isSaving } = useChangePlacementMutation(studentId);
const formErrorMessage = ref<string | null>(null);

const onSubmit = handleSubmit(async (formValues) => {
  if (!studentId.value) return;
  formErrorMessage.value = null;

  try {
    await changePlacement({
      studentId: studentId.value,
      payload: {
        campus: formValues.campus.trim(),
        schoolLevel: formValues.schoolLevel.trim(),
        grade: formValues.grade.trim(),
        academicYear: formValues.academicYear.trim(),
        effectiveDate: formValues.effectiveDate,
        program: formValues.program?.trim() || null,
        stream: formValues.stream?.trim() || null,
        cohort: formValues.cohort?.trim() || null,
        homeroom: formValues.homeroom?.trim() || null,
        reason: formValues.reason?.trim() || null,
        authorizedBy: formValues.authorizedBy?.trim() || null,
        decisionReference: formValues.decisionReference?.trim() || null,
      },
    });

    emit('update:visible', false);
    emit('success');
  } catch (err: any) {
    formErrorMessage.value = err?.message || 'Failed to execute administrative placement.';
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="!w-full sm:!w-[560px] md:!w-[640px]"
    data-testid="student-placement-drawer"
    @update:visible="handleClose"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal-50 dark:bg-teal-950 text-teal-600 dark:text-teal-400 flex items-center justify-center border border-teal-200 dark:border-teal-800">
          <GraduationCap class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-surface-900 dark:text-surface-100">
            Administrative Placement &amp; Rollover
          </h3>
          <p class="text-xs text-surface-500">
            Execute official lifecycle transition (STD-WF-08 / SCR-STU-03)
          </p>
        </div>
      </div>
    </template>

    <div v-if="student" class="space-y-5 p-1">
      <!-- Student Context Summary Header -->
      <div class="p-3.5 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-800/40 text-xs flex items-center justify-between">
        <div>
          <span class="font-bold text-surface-900 dark:text-surface-100 text-sm block">
            {{ student.fullName }}
          </span>
          <span class="text-surface-500 font-mono">
            {{ student.studentNumber }} • {{ (student as any).nisn || 'NISN —' }}
          </span>
        </div>
        <div class="text-right">
          <span class="text-[11px] text-surface-400 block">Current Placement</span>
          <strong class="text-surface-800 dark:text-surface-200">
            {{ (student as any).campus || '—' }} • {{ (student as any).grade || '—' }}
          </strong>
        </div>
      </div>

      <!-- Authoritative Academic Progression Recommendation Section -->
      <div class="space-y-2" data-testid="placement-progression-recommendation-section">
        <div class="flex items-center justify-between">
          <h4 class="text-xs font-bold uppercase tracking-wider text-surface-700 dark:text-surface-300">
            Authoritative Academic Progression Recommendation
          </h4>
          <span class="text-[11px] text-teal-600 dark:text-teal-400 font-medium">Academics Domain</span>
        </div>

        <ProgressionRecommendationBadge
          :student-id="studentId"
          :recommendation="latestRecommendation"
          :show-details="true"
        />
      </div>

      <!-- Form Error Alert -->
      <div
        v-if="formErrorMessage"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-200 text-xs flex items-start gap-2"
        data-testid="placement-error-banner"
      >
        <AlertCircle class="w-4 h-4 shrink-0 mt-0.5" />
        <span>{{ formErrorMessage }}</span>
      </div>

      <!-- Administrative Placement Form -->
      <form class="space-y-4 text-xs" @submit.prevent="onSubmit">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <FormField label="Campus" :error="errors.campus" required>
            <InputText
              v-model="campus"
              placeholder="e.g. Hagios Main Campus"
              class="w-full text-xs"
              data-testid="input-campus"
            />
          </FormField>

          <FormField label="School Level" :error="errors.schoolLevel" required>
            <InputText
              v-model="schoolLevel"
              placeholder="e.g. Primary School / SD"
              class="w-full text-xs"
              data-testid="input-school-level"
            />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <FormField label="Target Grade Level" :error="errors.grade" required>
            <div class="space-y-1">
              <InputText
                v-model="grade"
                placeholder="e.g. Grade 5"
                class="w-full text-xs font-semibold"
                data-testid="input-grade"
              />
              <span
                v-if="latestRecommendation?.recommendedToLevel"
                class="text-[11px] text-teal-600 dark:text-teal-400 cursor-pointer hover:underline inline-flex items-center gap-1"
                @click="grade = latestRecommendation.recommendedToLevel!"
              >
                Use recommended: <strong>{{ latestRecommendation.recommendedToLevel }}</strong>
              </span>
            </div>
          </FormField>

          <FormField label="Academic Year" :error="errors.academicYear" required>
            <InputText
              v-model="academicYear"
              placeholder="e.g. 2026/2027"
              class="w-full text-xs"
              data-testid="input-academic-year"
            />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <FormField label="Effective Date" :error="errors.effectiveDate" required>
            <DatePicker
              v-model="datePickerDate"
              date-format="yy-mm-dd"
              show-icon
              fluid
              class="w-full text-xs"
              data-testid="datepicker-effective-date"
            />
          </FormField>

          <FormField label="Homeroom Allocation" :error="errors.homeroom">
            <InputText
              v-model="homeroom"
              placeholder="e.g. Class 5A"
              class="w-full text-xs"
              data-testid="input-homeroom"
            />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <FormField label="Program / Stream" :error="errors.program">
            <InputText
              v-model="program"
              placeholder="e.g. PACE Accelerated Curriculum"
              class="w-full text-xs"
              data-testid="input-program"
            />
          </FormField>

          <FormField label="Cohort Identifier" :error="errors.cohort">
            <InputText
              v-model="cohort"
              placeholder="e.g. Cohort 2026-B"
              class="w-full text-xs"
              data-testid="input-cohort"
            />
          </FormField>
        </div>

        <FormField label="Authorizing Identity (Authorized By)" :error="errors.authorizedBy">
          <InputText
            v-model="authorizedBy"
            placeholder="e.g. Academic Authority / Principal"
            class="w-full text-xs"
            data-testid="input-authorized-by"
          />
        </FormField>

        <FormField label="Decision Reference / Decree (*SK Dewan Guru*)" :error="errors.decisionReference">
          <InputText
            v-model="decisionReference"
            placeholder="e.g. SK-DG/2026/06-01"
            class="w-full text-xs font-mono"
            data-testid="input-decision-reference"
          />
        </FormField>

        <FormField label="Reason for Placement / Rollover" :error="errors.reason">
          <Textarea
            v-model="reason"
            rows="2"
            class="w-full text-xs"
            placeholder="Institutional justification for this placement transition..."
            data-testid="textarea-reason"
          />
        </FormField>

        <!-- Actions Bar -->
        <div class="flex items-center justify-end gap-2.5 pt-4 border-t border-surface-200 dark:border-surface-700">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            text
            size="small"
            class="!text-xs cursor-pointer"
            data-testid="btn-cancel-placement"
            @click="handleClose"
          />

          <Button
            type="submit"
            :label="isRetention ? 'Execute Administrative Retention' : 'Execute Administrative Rollover'"
            severity="primary"
            size="small"
            :loading="isSaving"
            :disabled="isSaving"
            class="!text-xs cursor-pointer inline-flex items-center gap-1.5"
            data-testid="btn-execute-placement"
          >
            <template #icon>
              <CheckCircle2 class="w-4 h-4" />
            </template>
          </Button>
        </div>
      </form>
    </div>
  </Drawer>
</template>
