<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { CheckCircle2, AlertTriangle, Calendar, User, BookOpen } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  concludeParticipationSchema,
  type ConcludeParticipationFormValues,
} from '../schemas/participation.schema';
import { useConcludeParticipationMutation } from '../composables/useAcademicParticipation';
import type { AcademicParticipation, ConcludeParticipationResponse } from '../types';

const props = defineProps<{
  visible: boolean;
  participation: AcademicParticipation | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'concluded', response: ConcludeParticipationResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const todayIso = new Date().toISOString().slice(0, 10);

const { handleSubmit, errors, defineField, resetForm } =
  useForm<ConcludeParticipationFormValues>({
    validationSchema: toTypedSchema(concludeParticipationSchema),
    initialValues: {
      completionDate: todayIso,
      notes: '',
    },
  });

const [completionDate] = defineField('completionDate');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          completionDate: todayIso,
          notes: '',
        },
      });
    }
  },
);

const concludeMutation = useConcludeParticipationMutation();
const isSubmitting = computed(() => concludeMutation.isPending.value);

const onSubmit = handleSubmit(async (values) => {
  if (!props.participation) return;

  try {
    const result = await concludeMutation.mutateAsync({
      id: props.participation.id,
      request: {
        completionDate: values.completionDate,
        notes: values.notes || null,
      },
    });

    emit('concluded', result);
    isDialogVisible.value = false;
  } catch {
    // Errors are surfaced via mutation toast notification
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Conclude Participation"
    :style="{ width: '520px' }"
    :closable="!isSubmitting"
  >
    <div class="space-y-4">
      <!-- Context Information Card -->
      <div
        v-if="participation"
        class="rounded-lg border border-surface-200 bg-surface-50 p-4 text-sm dark:border-surface-700 dark:bg-surface-900/60"
      >
        <div class="flex items-center gap-2 font-medium text-surface-900 dark:text-surface-50">
          <User class="size-4 text-primary-500" />
          <span>{{ participation.studentName ?? 'Student' }}</span>
          <span
            v-if="participation.studentNumber"
            class="text-xs text-surface-500 dark:text-surface-400"
          >
            ({{ participation.studentNumber }})
          </span>
        </div>
        <div class="mt-2 flex items-center gap-2 text-xs text-surface-600 dark:text-surface-300">
          <BookOpen class="size-3.5 text-surface-400" />
          <span>Period: {{ participation.academicPeriodLabel }}</span>
          <span v-if="participation.sectionLabel" class="ml-2 font-medium text-surface-700 dark:text-surface-200">
            • Section: {{ participation.sectionLabel }}
          </span>
        </div>
        <div class="mt-1 flex items-center gap-2 text-xs text-surface-500 dark:text-surface-400">
          <Calendar class="size-3.5 text-surface-400" />
          <span>Enrolled on: {{ participation.startDate }}</span>
          <span class="ml-2">• Attempt #{{ participation.attemptNumber }}</span>
        </div>
      </div>

      <!-- Warning Prompt -->
      <div
        class="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50/70 p-3 text-xs text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/30 dark:text-amber-200"
      >
        <AlertTriangle class="size-4 shrink-0 text-amber-600 dark:text-amber-400" />
        <div>
          <p class="font-semibold">Formal Term Conclusion (UC-ACD-12)</p>
          <p class="mt-0.5 leading-relaxed">
            Concluding this participation will set its status to <strong>Completed</strong> and lock the learning outcome record for downstream evaluation and progression.
          </p>
        </div>
      </div>

      <!-- Form Inputs -->
      <form class="space-y-4" @submit.prevent="onSubmit">
        <FormField
          label="Completion Date"
          required
          :error="errors.completionDate"
          help-text="Date on which unit requirements were officially concluded"
        >
          <InputText
            v-model="completionDate"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Conclusion Notes"
          :error="errors.notes"
          help-text="Optional notes regarding completion, honors, or final coordinator remarks"
        >
          <Textarea
            v-model="notes"
            rows="3"
            class="w-full text-sm"
            placeholder="e.g. Completed all curriculum deliverables with distinction."
            :disabled="isSubmitting"
          />
        </FormField>

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
            label="Conclude Participation"
            icon="pi pi-check"
            severity="success"
            :loading="isSubmitting"
          />
        </div>
      </form>
    </div>
  </Dialog>
</template>
