<script setup lang="ts">
import { computed, watch, ref } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import SelectButton from 'primevue/selectbutton';
import {
  FileText,
  Calendar,
  User,
  Paperclip,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { z } from 'zod';
import { FormField } from '@/shared/components';
import {
  EVIDENCE_TYPES,
  type AssessmentEvidence,
  type EvidenceType,
} from '../types';
import {
  ISO_DATE_REGEX,
  isValidCalendarDate,
} from '../schemas/assessment.schema';
import { useRecordEvidenceMutation } from '../composables/useAssessmentEvidence';

export interface EnrolledStudentOption {
  id: string;
  name: string;
  studentNumber?: string | null;
}

interface Props {
  visible: boolean;
  assessmentId: string;
  assessmentTitle?: string | null;
  enrolledStudents?: EnrolledStudentOption[];
  preselectedStudentId?: string | null;
  preselectedApplicantId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  assessmentTitle: null,
  enrolledStudents: () => [],
  preselectedStudentId: null,
  preselectedApplicantId: null,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved', evidence: AssessmentEvidence): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Candidate selection mode (Student vs Applicant)
const candidateType = ref<'student' | 'applicant'>(
  props.preselectedApplicantId ? 'applicant' : 'student',
);

const candidateTypeOptions = [
  { label: 'Enrolled Student', value: 'student' },
  { label: 'Applicant / Candidate', value: 'applicant' },
];

const evidenceTypeOptions: Array<{ label: string; value: EvidenceType }> = [
  { label: 'Physical Test Paper / Exam Sheet', value: 'test_paper' },
  { label: 'Coursework Digital Submission', value: 'submission' },
  { label: 'Classroom / Practical Observation', value: 'observation' },
  { label: 'Oral Interview / Presentation', value: 'interview' },
  { label: 'Portfolio Piece', value: 'portfolio_piece' },
  { label: 'Project Artifact / Deliverable', value: 'project_artifact' },
  { label: 'Other Documentary Evidence', value: 'other' },
];

const studentOptions = computed(() => {
  return props.enrolledStudents.map((s) => ({
    label: `${s.name}${s.studentNumber ? ` (${s.studentNumber})` : ''}`,
    value: s.id,
  }));
});

// Schema for form
const evidenceFormSchema = z
  .object({
    candidateMode: z.enum(['student', 'applicant']),
    studentId: z.string().trim().optional().nullable().or(z.literal('')),
    applicantId: z.string().trim().optional().nullable().or(z.literal('')),
    submissionId: z.string().trim().optional().nullable().or(z.literal('')),
    evidenceType: z.enum(EVIDENCE_TYPES as unknown as [string, ...string[]], {
      required_error: 'Evidence type is required.',
    }),
    collectedDate: z
      .string({ required_error: 'Collected date is required.' })
      .trim()
      .regex(ISO_DATE_REGEX, 'Collected date must be in YYYY-MM-DD format.')
      .refine(isValidCalendarDate, {
        message: 'Collected date must be a valid calendar date.',
      }),
    evidenceReference: z
      .string()
      .trim()
      .max(500, 'Evidence reference cannot exceed 500 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    notes: z
      .string()
      .trim()
      .max(2000, 'Notes cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
  })
  .refine(
    (data) => {
      if (data.candidateMode === 'student') {
        return Boolean(data.studentId && data.studentId.trim().length > 0);
      } else {
        return Boolean(data.applicantId && data.applicantId.trim().length > 0);
      }
    },
    {
      message: 'Candidate identifier is required.',
      path: ['studentId'],
    },
  );

type EvidenceFormValues = z.infer<typeof evidenceFormSchema>;

const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<EvidenceFormValues>({
    validationSchema: toTypedSchema(evidenceFormSchema),
    initialValues: {
      candidateMode: 'student',
      studentId: props.preselectedStudentId ?? '',
      applicantId: props.preselectedApplicantId ?? '',
      submissionId: '',
      evidenceType: 'test_paper',
      collectedDate: new Date().toISOString().slice(0, 10),
      evidenceReference: '',
      notes: '',
    },
  });

const [candidateMode] = defineField('candidateMode');
const [studentId] = defineField('studentId');
const [applicantId] = defineField('applicantId');
const [submissionId] = defineField('submissionId');
const [evidenceType] = defineField('evidenceType');
const [collectedDate] = defineField('collectedDate');
const [evidenceReference] = defineField('evidenceReference');
const [notes] = defineField('notes');

watch(candidateType, (newType) => {
  candidateMode.value = newType;
  if (newType === 'student') {
    applicantId.value = '';
    if (!studentId.value && props.enrolledStudents.length > 0) {
      studentId.value = props.enrolledStudents[0].id;
    }
  } else {
    studentId.value = '';
  }
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      const isApp = Boolean(props.preselectedApplicantId);
      candidateType.value = isApp ? 'applicant' : 'student';
      candidateMode.value = isApp ? 'applicant' : 'student';
      resetForm({
        values: {
          candidateMode: isApp ? 'applicant' : 'student',
          studentId: props.preselectedStudentId ?? (props.enrolledStudents[0]?.id ?? ''),
          applicantId: props.preselectedApplicantId ?? '',
          submissionId: '',
          evidenceType: 'test_paper',
          collectedDate: new Date().toISOString().slice(0, 10),
          evidenceReference: '',
          notes: '',
        },
      });
    }
  },
  { immediate: true },
);

const recordEvidenceMutation = useRecordEvidenceMutation();

const onSubmit = handleSubmit(async (values) => {
  const isStudent = values.candidateMode === 'student';

  const res = await recordEvidenceMutation.mutateAsync({
    assessmentId: props.assessmentId,
    request: {
      studentId: isStudent ? values.studentId?.trim() : null,
      applicantId: !isStudent ? values.applicantId?.trim() : null,
      submissionId: values.submissionId ? values.submissionId.trim() : null,
      evidenceType: values.evidenceType,
      collectedDate: values.collectedDate.trim(),
      evidenceReference: values.evidenceReference ? values.evidenceReference.trim() : null,
      notes: values.notes ? values.notes.trim() : null,
    },
  });

  emit('saved', res);
  isDialogVisible.value = false;
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Record Learning Evidence"
    class="w-full max-w-lg"
    :dismissable-mask="true"
    data-testid="evidence-intake-modal"
  >
    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Target Assessment Context -->
      <div
        v-if="assessmentTitle"
        class="bg-teal-50/60 border border-teal-200/80 rounded-lg p-2.5 flex items-center gap-2 text-xs text-teal-800"
      >
        <FileText class="w-4 h-4 text-teal-600 shrink-0" />
        <span class="font-medium text-teal-950">Assessment:</span>
        <span class="truncate">{{ assessmentTitle }}</span>
      </div>

      <!-- Candidate Type Selector (Dual Candidate Reference: Student XOR Applicant) -->
      <div class="space-y-1.5">
        <label class="block text-xs font-medium text-slate-700">Candidate Type</label>
        <SelectButton
          v-model="candidateType"
          :options="candidateTypeOptions"
          option-label="label"
          option-value="value"
          class="w-full text-xs"
          data-testid="candidate-type-toggle"
        />
      </div>

      <!-- Student Selection -->
      <div v-if="candidateType === 'student'">
        <FormField
          id="evidence-student"
          label="Enrolled Student"
          :error="errors.studentId"
          required
        >
          <div v-if="studentOptions.length > 0">
            <Select
              v-model="studentId"
              :options="studentOptions"
              option-label="label"
              option-value="value"
              placeholder="Select student"
              class="w-full"
              data-testid="evidence-student-select"
            />
          </div>
          <div v-else class="relative">
            <User class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="studentId"
              placeholder="Enter student UUID"
              class="w-full pl-9"
              data-testid="evidence-student-input"
            />
          </div>
        </FormField>
      </div>

      <!-- Applicant ID Input -->
      <div v-else>
        <FormField
          id="evidence-applicant"
          label="Applicant / Candidate Identifier"
          :error="errors.studentId"
          required
          help="Enter the entrance candidate or applicant UUID (GAP-003, UC-ACD-07)."
        >
          <div class="relative">
            <User class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="applicantId"
              placeholder="e.g. 7f8a9b2c-..."
              class="w-full pl-9"
              data-testid="evidence-applicant-input"
            />
          </div>
        </FormField>
      </div>

      <!-- Row: Evidence Type & Collection Date -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <FormField
          id="evidence-type"
          label="Evidence Type"
          :error="errors.evidenceType"
          required
        >
          <Select
            v-model="evidenceType"
            :options="evidenceTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select evidence type"
            class="w-full"
            data-testid="evidence-type-select"
          />
        </FormField>

        <FormField
          id="evidence-date"
          label="Collected Date"
          :error="errors.collectedDate"
          required
          help="YYYY-MM-DD"
        >
          <div class="relative">
            <Calendar class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="collectedDate"
              placeholder="YYYY-MM-DD"
              class="w-full pl-9"
              data-testid="evidence-date-input"
            />
          </div>
        </FormField>
      </div>

      <!-- Evidence Reference -->
      <FormField
        id="evidence-reference"
        label="Physical / Digital Reference"
        :error="errors.evidenceReference"
        help="Physical filing drawer, exam booklet number, observation log ID, or digital link."
      >
        <div class="relative">
          <Paperclip class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <InputText
            v-model="evidenceReference"
            placeholder="e.g. Test Paper Box #2 - Folder A; or https://..."
            class="w-full pl-9"
            data-testid="evidence-reference-input"
          />
        </div>
      </FormField>

      <!-- Optional Submission ID -->
      <FormField
        id="evidence-submission-id"
        label="Linked Coursework Submission ID (Optional)"
        :error="errors.submissionId"
      >
        <InputText
          v-model="submissionId"
          placeholder="Optional UUID of digital coursework submission"
          class="w-full"
          data-testid="evidence-submission-id-input"
        />
      </FormField>

      <!-- Notes -->
      <FormField
        id="evidence-notes"
        label="Intake Notes / Teacher Remarks"
        :error="errors.notes"
      >
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Observation notes, special accommodations observed, etc."
          class="w-full text-sm"
          data-testid="evidence-notes-textarea"
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
          label="Record Evidence"
          icon="pi pi-check"
          size="small"
          :loading="isSubmitting"
          data-testid="evidence-submit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
