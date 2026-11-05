<script setup lang="ts">
import { computed, watch } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { BookOpen, Calendar, FileText, Sparkles } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { z } from 'zod';
import { FormField } from '@/shared/components';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import { useCreateAssignmentMutation } from '../composables/useAssignments';
import { useUnitActivitiesQuery } from '../../sessions/composables/useLearningActivities';
import type { CreateAssignmentResponse } from '../types';

interface Props {
  visible: boolean;
  unitId?: string | null | undefined;
  unitName?: string | undefined;
  initialActivityId?: string | null | undefined;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', assignment: CreateAssignmentResponse): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Query activities under the selected unit to populate parent activity dropdown
const { data: activities, isLoading: isLoadingActivities } = useUnitActivitiesQuery(
  computed(() => props.unitId),
  undefined,
  {
    enabled: computed(() => props.visible && Boolean(props.unitId)),
  },
);

const activityOptions = computed(() => {
  return (activities.value ?? [])
    .filter((a) => a.status !== 'cancelled')
    .map((a) => ({
      label: `${a.title} (${a.activityType.replace('_', ' ')}) — ${a.activityDate}`,
      value: a.id,
    }));
});

const formSchema = z
  .object({
    activityId: z
      .string({ required_error: 'Parent learning activity is required.' })
      .trim()
      .min(1, 'Please select a parent learning activity.'),
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(1, 'Title is required.')
      .max(200, 'Title cannot exceed 200 characters.'),
    description: z
      .string()
      .trim()
      .max(2000, 'Description cannot exceed 2000 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    releaseDate: z
      .string({ required_error: 'Release date is required.' })
      .trim()
      .min(1, 'Release date is required.')
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Release date must be in YYYY-MM-DD format.'),
    dueDate: z
      .string()
      .trim()
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Due date must be in YYYY-MM-DD format.')
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
      if (data.releaseDate && data.dueDate && data.dueDate.length > 0) {
        return data.dueDate >= data.releaseDate;
      }
      return true;
    },
    {
      message: 'Due date must be on or after release date.',
      path: ['dueDate'],
    },
  );

type FormValues = z.infer<typeof formSchema>;

const todayIso = formatDateOnly(new Date());

const { handleSubmit, errors, defineField, resetForm, isSubmitting } = useForm<FormValues>({
  validationSchema: toTypedSchema(formSchema),
  initialValues: {
    activityId: props.initialActivityId ?? '',
    title: '',
    description: '',
    releaseDate: todayIso,
    dueDate: '',
    notes: '',
  },
});

const [activityId] = defineField('activityId');
const [title] = defineField('title');
const [description] = defineField('description');
const [releaseDate] = defineField('releaseDate');
const [dueDate] = defineField('dueDate');
const [notes] = defineField('notes');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          activityId: props.initialActivityId ?? '',
          title: '',
          description: '',
          releaseDate: todayIso,
          dueDate: '',
          notes: '',
        },
      });
    }
  },
);

const createMutation = useCreateAssignmentMutation();

const onSubmit = handleSubmit(async (values) => {
  try {
    const result = await createMutation.mutateAsync({
      activityId: values.activityId,
      request: {
        title: values.title.trim(),
        description: values.description?.trim() ? values.description.trim() : null,
        releaseDate: values.releaseDate,
        dueDate: values.dueDate?.trim() ? values.dueDate.trim() : null,
        notes: values.notes?.trim() ? values.notes.trim() : null,
      },
    });

    emit('created', result);
    isDrawerVisible.value = false;
  } catch {
    // Errors handled by mutation toast
  }
});
</script>

<template>
  <Drawer
    v-model:visible="isDrawerVisible"
    position="right"
    class="!w-full md:!w-[560px] lg:!w-[620px]"
    data-testid="create-assignment-drawer"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-700">
          <BookOpen class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-lg font-bold text-slate-900 leading-tight">Create Assignment</h2>
          <p class="text-xs text-slate-500 mt-0.5">
            Plan a coursework task linked to an instructional activity (SCR-ACD-02)
            <span v-if="unitName" class="font-medium text-slate-700"> &bull; {{ unitName }}</span>
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-5 py-2" @submit.prevent="onSubmit">
      <!-- 1. Parent Learning Activity -->
      <FormField
        label="Parent Learning Activity"
        required
        :error="errors.activityId"
        hint="Select the instructional activity or session from which this coursework originates."
      >
        <Select
          v-model="activityId"
          :options="activityOptions"
          option-label="label"
          option-value="value"
          placeholder="Choose learning activity..."
          class="w-full"
          :loading="isLoadingActivities"
          :invalid="Boolean(errors.activityId)"
          data-testid="assignment-activity-select"
        />
        <p v-if="!isLoadingActivities && activityOptions.length === 0" class="text-xs text-amber-600 mt-1">
          No scheduled or conducted activities found for this unit. Please plan a session first.
        </p>
      </FormField>

      <!-- 2. Assignment Title -->
      <FormField
        label="Assignment Title"
        required
        :error="errors.title"
        hint="Clear title describing the coursework task (e.g. 'Lab Report 1: DNA Extraction')."
      >
        <InputText
          v-model="title"
          placeholder="e.g. Module 3 Problem Set"
          class="w-full"
          :invalid="Boolean(errors.title)"
          maxlength="200"
          data-testid="assignment-title-input"
        />
      </FormField>

      <!-- 3. Description / Instructions -->
      <FormField
        label="Description & Instructions"
        :error="errors.description"
        hint="Detailed instructions, submission requirements, or rubric overview."
      >
        <Textarea
          v-model="description"
          rows="4"
          placeholder="Specify formatting, expected word count, deliverables, or criteria..."
          class="w-full"
          :invalid="Boolean(errors.description)"
          data-testid="assignment-description-input"
        />
      </FormField>

      <!-- 4. Date Row: Release Date & Due Date -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          label="Release Date"
          required
          :error="errors.releaseDate"
          hint="Date students can see task."
        >
          <div class="relative">
            <InputText
              v-model="releaseDate"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.releaseDate)"
              data-testid="assignment-release-date-input"
            />
          </div>
        </FormField>

        <FormField
          label="Due Date"
          :error="errors.dueDate"
          hint="Deadline for submissions."
        >
          <div class="relative">
            <InputText
              v-model="dueDate"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.dueDate)"
              data-testid="assignment-due-date-input"
            />
          </div>
        </FormField>
      </div>

      <!-- 5. Internal Notes -->
      <FormField
        label="Internal Teacher Notes"
        :error="errors.notes"
        hint="Internal remarks, co-teacher notes, or grading key references (not visible to students)."
      >
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="e.g. Solutions guide uploaded to department drive."
          class="w-full"
          :invalid="Boolean(errors.notes)"
          data-testid="assignment-notes-input"
        />
      </FormField>

      <!-- Footer Action Buttons -->
      <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          @click="isDrawerVisible = false"
        />
        <Button
          type="submit"
          label="Create Assignment"
          icon="pi pi-check"
          :loading="isSubmitting || createMutation.isPending.value"
          data-testid="submit-create-assignment-btn"
        />
      </div>
    </form>
  </Drawer>
</template>
