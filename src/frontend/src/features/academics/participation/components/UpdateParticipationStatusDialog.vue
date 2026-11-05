<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { User, Activity, AlertCircle } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  updateParticipationStatusSchema,
  type UpdateParticipationStatusFormValues,
} from '../schemas/participation.schema';
import { useUpdateParticipationStatusMutation } from '../composables/useAcademicParticipation';
import type { AcademicParticipation, UpdateParticipationStatusResponse } from '../types';

const props = defineProps<{
  visible: boolean;
  participation: AcademicParticipation | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'updated', response: UpdateParticipationStatusResponse): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const statusOptions = [
  { label: 'In Progress (Active Participation)', value: 'in_progress' },
  { label: 'Paused (Temporary Leave / Hold)', value: 'paused' },
  { label: 'Withdrawn (Dropped Unit)', value: 'withdrawn' },
];

const { handleSubmit, errors, defineField, resetForm } =
  useForm<UpdateParticipationStatusFormValues>({
    validationSchema: toTypedSchema(updateParticipationStatusSchema),
    initialValues: {
      newStatus: 'in_progress',
      notes: '',
    },
  });

const [newStatus] = defineField('newStatus');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen && props.participation) {
      const current = props.participation.status;
      const initial =
        current === 'in_progress' || current === 'paused' || current === 'withdrawn'
          ? current
          : 'in_progress';
      resetForm({
        values: {
          newStatus: initial,
          notes: props.participation.notes ?? '',
        },
      });
    }
  },
);

const updateMutation = useUpdateParticipationStatusMutation();
const isSubmitting = computed(() => updateMutation.isPending.value);

const onSubmit = handleSubmit(async (values) => {
  if (!props.participation) return;

  try {
    const result = await updateMutation.mutateAsync({
      id: props.participation.id,
      request: {
        newStatus: values.newStatus,
        notes: values.notes || null,
      },
    });

    emit('updated', result);
    isDialogVisible.value = false;
  } catch {
    // Handled by toast notification
  }
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Update Participation Status"
    :style="{ width: '500px' }"
    :closable="!isSubmitting"
  >
    <div class="space-y-4">
      <div
        v-if="participation"
        class="rounded-lg border border-surface-200 bg-surface-50 p-3 text-sm dark:border-surface-700 dark:bg-surface-900/60"
      >
        <div class="flex items-center gap-2 font-medium text-surface-900 dark:text-surface-50">
          <User class="size-4 text-primary-500" />
          <span>{{ participation.studentName ?? 'Student' }}</span>
          <span v-if="participation.studentNumber" class="text-xs text-surface-500">
            ({{ participation.studentNumber }})
          </span>
        </div>
        <p class="mt-1 text-xs text-surface-500 dark:text-surface-400">
          Current Status: <span class="font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">{{ participation.status }}</span>
        </p>
      </div>

      <form class="space-y-4" @submit.prevent="onSubmit">
        <FormField
          label="Target Status"
          required
          :error="errors.newStatus"
          help-text="Transition learning unit participation lifecycle state"
        >
          <Select
            v-model="newStatus"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Status Reason / Notes"
          :error="errors.notes"
          help-text="Optional justification or context for this status modification"
        >
          <Textarea
            v-model="notes"
            rows="3"
            class="w-full text-sm"
            placeholder="e.g. Student granted medical leave until midterm."
            :disabled="isSubmitting"
          />
        </FormField>

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
            label="Update Status"
            icon="pi pi-check"
            :loading="isSubmitting"
          />
        </div>
      </form>
    </div>
  </Dialog>
</template>
