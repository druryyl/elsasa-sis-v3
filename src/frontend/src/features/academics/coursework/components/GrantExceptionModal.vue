<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { ShieldAlert, AlertCircle } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import { recordExceptionSchema, type RecordExceptionFormValues } from '../schemas/coursework.schema';
import { useRecordExceptionMutation } from '../composables/useAssignments';
import type { RecordExceptionResponse } from '../types';

interface StudentOption {
  studentId: string;
  studentName?: string | null;
  studentNumber?: string | null;
}

interface Props {
  visible: boolean;
  assignmentId?: string | null | undefined;
  assignmentTitle?: string | null | undefined;
  students: StudentOption[];
  initialStudentId?: string | null | undefined;
}

const props = withDefaults(defineProps<Props>(), {
  assignmentId: undefined,
  assignmentTitle: undefined,
  students: () => [],
  initialStudentId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved', response: RecordExceptionResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const studentDropdownOptions = computed(() => {
  return props.students.map((s) => ({
    label: `${s.studentName ?? 'Unknown Student'} (${s.studentNumber ?? s.studentId})`,
    value: s.studentId,
  }));
});

const exceptionTypeOptions = [
  {
    label: 'Excused (Medical or Approved Absence)',
    value: 'excused',
    hint: 'Excludes assignment from overdue/missing penalty without counting against participation.',
  },
  {
    label: 'Exempted (Curriculum Waiver or Transfer)',
    value: 'exempted',
    hint: 'Formally exempts student from completing this assessment task.',
  },
];

const { handleSubmit, errors, defineField, resetForm, isSubmitting } =
  useForm<RecordExceptionFormValues>({
    validationSchema: toTypedSchema(recordExceptionSchema),
    initialValues: {
      studentId: props.initialStudentId ?? '',
      exceptionType: 'excused',
      reason: '',
    },
  });

const [studentId] = defineField('studentId');
const [exceptionType] = defineField('exceptionType');
const [reason] = defineField('reason');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          studentId: props.initialStudentId ?? (props.students[0]?.studentId ?? ''),
          exceptionType: 'excused',
          reason: '',
        },
      });
    }
  },
);

watch(
  () => props.initialStudentId,
  (newId) => {
    if (newId) {
      studentId.value = newId;
    }
  },
);

const recordMutation = useRecordExceptionMutation();

const onSubmit = handleSubmit(async (values) => {
  if (!props.assignmentId) return;

  try {
    const result = await recordMutation.mutateAsync({
      assignmentId: props.assignmentId,
      request: {
        studentId: values.studentId,
        exceptionType: values.exceptionType,
        reason: values.reason.trim(),
      },
    });

    emit('saved', result);
    isDialogVisible.value = false;
  } catch {
    // Handled by toast in mutation hook
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg"
    data-testid="grant-exception-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-lg bg-amber-50 border border-amber-200 flex items-center justify-center text-amber-700">
          <ShieldAlert class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 leading-tight">Grant Policy Exception</h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Record an administrative excuse or exemption for coursework (SCR-ACD-02, TD-06)
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1 pb-2" @submit.prevent="onSubmit">
      <!-- Target Assignment Context Banner -->
      <div v-if="assignmentTitle" class="p-3 bg-slate-50 rounded-lg border border-slate-200 text-xs text-slate-700">
        <span class="font-semibold text-slate-900">Assignment:</span> {{ assignmentTitle }}
      </div>

      <!-- 1. Student Selector -->
      <FormField
        label="Student Participant"
        required
        :error="errors.studentId"
        hint="Select enrolled student to grant exception."
      >
        <Select
          v-model="studentId"
          :options="studentDropdownOptions"
          option-label="label"
          option-value="value"
          placeholder="Select student..."
          class="w-full"
          :invalid="Boolean(errors.studentId)"
          data-testid="exception-student-select"
        />
      </FormField>

      <!-- 2. Exception Type -->
      <FormField
        label="Exception Type"
        required
        :error="errors.exceptionType"
        hint="Choose whether the student is excused or completely exempted."
      >
        <Select
          v-model="exceptionType"
          :options="exceptionTypeOptions"
          option-label="label"
          option-value="value"
          class="w-full"
          :invalid="Boolean(errors.exceptionType)"
          data-testid="exception-type-select"
        />
      </FormField>

      <!-- 3. Mandatory Justification Reason -->
      <FormField
        label="Mandatory Justification Reason"
        required
        :error="errors.reason"
        hint="Provide audit explanation (e.g. medical certificate #MC-202, approved bereavement)."
      >
        <Textarea
          v-model="reason"
          rows="3"
          placeholder="Enter justification rationale..."
          class="w-full"
          :invalid="Boolean(errors.reason)"
          data-testid="exception-reason-input"
        />
      </FormField>

      <!-- Regulatory Notice -->
      <div class="flex items-start gap-2 p-3 bg-amber-50/60 rounded-lg border border-amber-200/60 text-xs text-amber-800">
        <AlertCircle class="w-4 h-4 shrink-0 text-amber-600 mt-0.5" />
        <span>Exceptions are permanently recorded for auditability and replace missing status projections.</span>
      </div>

      <!-- Footer Buttons -->
      <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-200">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          label="Grant Exception"
          icon="pi pi-check"
          severity="warn"
          :loading="isSubmitting || recordMutation.isPending.value"
          data-testid="submit-exception-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
