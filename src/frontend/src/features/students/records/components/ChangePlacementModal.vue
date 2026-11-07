<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import DatePicker from 'primevue/datepicker';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import {
  Building2,
  Calendar,
  Layers,
  GraduationCap,
  ShieldCheck,
  AlertCircle,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatDateOnly, formatDisplayDate, parseIsoDate } from '@/shared/utils/dateUtils';
import { changePlacementSchema, type ChangePlacementFormData } from '../schemas/records.schema';
import { useChangePlacementMutation } from '../composables/useStudentRecordsMutations';
import type {
  StudentDetail,
  AdministrativePlacementDto,
  ChangeAdministrativePlacementResponse,
} from '../types/records.types';
import ProgressionRecommendationBadge from '@/features/students/lifecycle/components/ProgressionRecommendationBadge.vue';

const props = defineProps<{
  visible: boolean;
  student: StudentDetail;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved', data: ChangeAdministrativePlacementResponse): void;
  (e: 'success', data: ChangeAdministrativePlacementResponse): void;
}>();

const currentActivePlacement = computed<AdministrativePlacementDto | null>(() => {
  if (props.student?.currentPlacement) return props.student.currentPlacement;
  if (props.student?.placementHistory && props.student.placementHistory.length > 0) {
    return props.student.placementHistory.find((p) => p.isCurrent) ?? null;
  }
  return null;
});

const minEffectiveDateStr = computed(() => currentActivePlacement.value?.effectiveDate || '');
const minEffectiveDate = computed<Date | undefined>(() => {
  if (!minEffectiveDateStr.value) return undefined;
  const parsed = parseIsoDate(minEffectiveDateStr.value);
  return isNaN(parsed.getTime()) ? undefined : parsed;
});

const { handleSubmit, errors, defineField, resetForm, setFieldError } =
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
  const active = currentActivePlacement.value;
  const todayStr = formatDateOnly(new Date());
  let initialEffDate = todayStr;
  if (minEffectiveDateStr.value && initialEffDate < minEffectiveDateStr.value) {
    initialEffDate = minEffectiveDateStr.value;
  }

  resetForm({
    values: {
      campus: active?.campus || (props.student as any)?.campus || '',
      schoolLevel: active?.schoolLevel || (props.student as any)?.schoolLevel || '',
      grade: active?.grade || (props.student as any)?.grade || '',
      academicYear: active?.academicYear || (props.student as any)?.academicYear || '',
      effectiveDate: initialEffDate,
      program: active?.program || '',
      stream: active?.stream || '',
      cohort: active?.cohort || '',
      homeroom: active?.homeroom || '',
      reason: '',
      authorizedBy: '',
      decisionReference: '',
    },
  });

  const parsed = parseIsoDate(initialEffDate);
  if (!isNaN(parsed.getTime())) {
    datePickerDate.value = parsed;
  }
}

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      populateForm();
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:visible', false);
}

const studentIdRef = computed(() => props.student?.id);
const { mutateAsync: changePlacement, isPending } = useChangePlacementMutation(studentIdRef);

const formErrorMessage = ref<string | null>(null);

const onSubmit = handleSubmit(async (formValues) => {
  formErrorMessage.value = null;

  if (minEffectiveDateStr.value && formValues.effectiveDate < minEffectiveDateStr.value) {
    setFieldError(
      'effectiveDate',
      `Effective date cannot be prior to current placement effective date (${minEffectiveDateStr.value}).`,
    );
    return;
  }

  const payload = {
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
  };

  try {
    const response = await changePlacement({
      studentId: props.student.id,
      payload,
    });

    emit('update:visible', false);
    emit('saved', response);
    emit('success', response);
    resetForm();
  } catch (err: any) {
    formErrorMessage.value = err?.message || 'Failed to update administrative placement.';
  }
});

function handleEffectiveDateInput(e: Event) {
  const target = e.target as HTMLInputElement;
  if (target && target.value) {
    effectiveDate.value = target.value;
  }
}

async function handleFormSubmit() {
  await onSubmit();
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :style="{ width: '760px' }"
    :breakpoints="{ '960px': '85vw', '640px': '95vw' }"
    data-testid="change-placement-modal"
    @update:visible="handleClose"
  >
    <template #header>
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-xs">
          <GraduationCap class="w-5 h-5" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2.5">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Change Administrative Placement
            </h3>
            <span v-if="student?.studentNumber" class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800 font-mono">
              {{ student.studentNumber }}
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Reassign academic placement, grade progression, cohort allocation, and term parameters for <strong class="text-surface-700 dark:text-surface-300">{{ student?.fullName || 'student' }}</strong>.
          </p>
        </div>
      </div>
    </template>

    <!-- Modal Content -->
    <div class="space-y-5 pt-3 pb-1">
      <!-- Active placement context banner -->
      <div
        v-if="currentActivePlacement"
        class="p-4 rounded-xl border border-teal-200 dark:border-teal-800/80 bg-teal-50/60 dark:bg-teal-950/30 text-xs text-teal-900 dark:text-teal-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs"
        data-testid="active-placement-banner"
      >
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-lg bg-teal-100 dark:bg-teal-900/60 flex items-center justify-center text-teal-700 dark:text-teal-300 shrink-0">
            <Building2 class="w-4 h-4" />
          </div>
          <div>
            <span class="font-medium text-surface-500 dark:text-surface-400 block text-[11px]">Current Active Assignment</span>
            <span class="font-bold text-surface-900 dark:text-surface-100 text-sm">
              {{ currentActivePlacement.campus }} • {{ currentActivePlacement.grade }} ({{ currentActivePlacement.schoolLevel }})
            </span>
          </div>
        </div>
        <div class="shrink-0 text-left sm:text-right bg-white dark:bg-surface-800 px-3 py-1.5 rounded-lg border border-teal-200 dark:border-teal-800">
          <span class="text-[11px] text-surface-500 dark:text-surface-400 block">Effective Since</span>
          <strong class="text-teal-800 dark:text-teal-300 font-semibold">{{ formatDisplayDate(currentActivePlacement.effectiveDate) }}</strong>
        </div>
      </div>

      <!-- Authoritative Academic Progression Recommendation Section -->
      <div class="space-y-1.5" data-testid="modal-progression-recommendation-section">
        <ProgressionRecommendationBadge
          :student-id="student?.id"
          :show-details="true"
        />
      </div>

      <!-- General Form Error Message -->
      <div
        v-if="formErrorMessage"
        class="p-3.5 rounded-xl border border-rose-200 bg-rose-50 text-rose-800 text-xs flex items-center gap-2.5"
        data-testid="form-error-message"
      >
        <AlertCircle class="w-4 h-4 shrink-0 text-rose-600" />
        <span class="font-medium">{{ formErrorMessage }}</span>
      </div>

      <form class="space-y-5" @submit.prevent="handleFormSubmit">
        <!-- 1. Primary Placement Attributes -->
        <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
          <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
            <GraduationCap class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
            <span>Target Institutional Placement Details</span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              id="placement-campus"
              label="Campus"
              required
              :error="errors.campus"
            >
              <InputText
                id="placement-campus"
                v-model="campus"
                placeholder="e.g., Main Campus"
                class="w-full text-sm"
                :invalid="Boolean(errors.campus)"
                data-testid="input-campus"
              />
            </FormField>

            <FormField
              id="placement-school-level"
              label="School Level"
              required
              :error="errors.schoolLevel"
            >
              <InputText
                id="placement-school-level"
                v-model="schoolLevel"
                placeholder="e.g., Primary, Middle, High School"
                class="w-full text-sm"
                :invalid="Boolean(errors.schoolLevel)"
                data-testid="input-school-level"
              />
            </FormField>

            <FormField
              id="placement-grade"
              label="Grade"
              required
              :error="errors.grade"
            >
              <InputText
                id="placement-grade"
                v-model="grade"
                placeholder="e.g., Grade 1, Grade 7"
                class="w-full text-sm"
                :invalid="Boolean(errors.grade)"
                data-testid="input-grade"
              />
            </FormField>

            <FormField
              id="placement-academic-year"
              label="Academic Year"
              required
              :error="errors.academicYear"
            >
              <InputText
                id="placement-academic-year"
                v-model="academicYear"
                placeholder="e.g., 2026/2027"
                class="w-full text-sm font-mono"
                :invalid="Boolean(errors.academicYear)"
                data-testid="input-academic-year"
              />
            </FormField>
          </div>
        </div>

        <!-- 2. Effective Date & Scheduling -->
        <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
          <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
            <Calendar class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
            <span>Effective Date &amp; Reassignment Scheduling</span>
          </div>

          <FormField
            id="placement-effective-date"
            label="Effective Transition Date"
            required
            :error="errors.effectiveDate"
            :help-text="minEffectiveDateStr ? `Must be on or after current placement date (${minEffectiveDateStr})` : 'Date from which this placement becomes active'"
          >
            <DatePicker
              id="placement-effective-date"
              input-id="placement-effective-date-input"
              v-model="datePickerDate"
              date-format="yy-mm-dd"
              placeholder="YYYY-MM-DD"
              show-icon
              :min-date="minEffectiveDate"
              class="w-full text-sm"
              :invalid="Boolean(errors.effectiveDate)"
              :pt="{ pcInputText: { root: { 'data-testid': 'input-effective-date', onInput: handleEffectiveDateInput } } }"
              data-testid="effective-date-picker"
            />
          </FormField>
        </div>

        <!-- 3. Academic Categorization & Homeroom (Optional) -->
        <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
          <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
            <Layers class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
            <span>Academic Track, Cohort &amp; Homeroom (Optional)</span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              id="placement-program"
              label="Academic Program"
              optional
              :error="errors.program"
            >
              <InputText
                id="placement-program"
                v-model="program"
                placeholder="e.g., Standard, Cambridge, IB"
                class="w-full text-sm"
                :invalid="Boolean(errors.program)"
                data-testid="input-program"
              />
            </FormField>

            <FormField
              id="placement-stream"
              label="Academic Stream"
              optional
              :error="errors.stream"
            >
              <InputText
                id="placement-stream"
                v-model="stream"
                placeholder="e.g., Science, Social, General"
                class="w-full text-sm"
                :invalid="Boolean(errors.stream)"
                data-testid="input-stream"
              />
            </FormField>

            <FormField
              id="placement-cohort"
              label="Assigned Cohort"
              optional
              :error="errors.cohort"
            >
              <InputText
                id="placement-cohort"
                v-model="cohort"
                placeholder="e.g., 2026-A"
                class="w-full text-sm"
                :invalid="Boolean(errors.cohort)"
                data-testid="input-cohort"
              />
            </FormField>

            <FormField
              id="placement-homeroom"
              label="Homeroom / Classroom"
              optional
              :error="errors.homeroom"
            >
              <InputText
                id="placement-homeroom"
                v-model="homeroom"
                placeholder="e.g., Room 101, Class 1A"
                class="w-full text-sm"
                :invalid="Boolean(errors.homeroom)"
                data-testid="input-homeroom"
              />
            </FormField>
          </div>
        </div>

        <!-- 4. Governance, Authorization & Reason -->
        <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
          <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
            <ShieldCheck class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
            <span>Governance, Decree &amp; Reason (Optional)</span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              id="placement-authorized-by"
              label="Authorized By"
              optional
              :error="errors.authorizedBy"
            >
              <InputText
                id="placement-authorized-by"
                v-model="authorizedBy"
                placeholder="e.g., Principal / Registrar"
                class="w-full text-sm"
                :invalid="Boolean(errors.authorizedBy)"
                data-testid="input-authorized-by"
              />
            </FormField>

            <FormField
              id="placement-decision-reference"
              label="Decision Decree Reference"
              optional
              :error="errors.decisionReference"
            >
              <InputText
                id="placement-decision-reference"
                v-model="decisionReference"
                placeholder="e.g., Decree SK/2026/042"
                class="w-full text-sm font-mono"
                :invalid="Boolean(errors.decisionReference)"
                data-testid="input-decision-reference"
              />
            </FormField>
          </div>

          <FormField
            id="placement-reason"
            label="Reason for Reassignment"
            optional
            :error="errors.reason"
          >
            <Textarea
              id="placement-reason"
              v-model="reason"
              rows="3"
              placeholder="Provide justification for administrative placement reassignment..."
              class="w-full text-sm leading-relaxed"
              :invalid="Boolean(errors.reason)"
              data-testid="input-reason"
            />
          </FormField>
        </div>
      </form>
    </div>

    <!-- Modal Footer Actions -->
    <template #footer>
      <div class="flex items-center justify-end gap-3 pt-3 border-t border-surface-200 dark:border-surface-800">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="isPending"
          data-testid="btn-cancel-placement"
          class="!px-4 !py-2 !text-xs font-medium cursor-pointer"
          @click="handleClose"
        />
        <Button
          type="button"
          label="Confirm Placement Change"
          severity="primary"
          icon="pi pi-check"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs"
          :loading="isPending"
          data-testid="btn-submit-placement"
          @click="handleFormSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
