<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Tag from 'primevue/tag';
import { CheckCircle2, BookOpen, Clock, Calendar, UserCheck } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  conductActivitySchema,
  type ConductActivityFormValues,
} from '../schemas/sessions.schema';
import { useConductActivityMutation } from '../composables/useLearningActivities';
import type {
  ConductActivityResponse,
  LearningActivity,
  TeacherDailyActivity,
} from '../types';

const props = defineProps<{
  visible: boolean;
  activity: TeacherDailyActivity | LearningActivity | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'conducted', response: ConductActivityResponse): void;
}>();

const authStore = useAuthStore();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const defaultTeacher = computed(() => {
  return (
    props.activity?.teacherName ??
    authStore.user?.fullName ??
    authStore.user?.username ??
    ''
  );
});

const { handleSubmit, errors, defineField, resetForm } =
  useForm<ConductActivityFormValues>({
    validationSchema: toTypedSchema(conductActivitySchema),
    initialValues: {
      deliveryNotes: props.activity?.deliveryNotes ?? '',
      actualTeacherName: defaultTeacher.value,
    },
  });

const [deliveryNotes] = defineField('deliveryNotes');
const [actualTeacherName] = defineField('actualTeacherName');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen && props.activity) {
      resetForm({
        values: {
          deliveryNotes: props.activity.deliveryNotes ?? '',
          actualTeacherName: defaultTeacher.value,
        },
      });
    }
  },
);

const conductMutation = useConductActivityMutation();

const onSubmit = handleSubmit((values) => {
  if (!props.activity) return;

  conductMutation.mutate(
    {
      id: props.activity.id,
      request: {
        deliveryNotes: values.deliveryNotes ? values.deliveryNotes.trim() : null,
        actualTeacherName: values.actualTeacherName ? values.actualTeacherName.trim() : null,
      },
    },
    {
      onSuccess: (data) => {
        emit('conducted', data);
        isDialogVisible.value = false;
      },
    },
  );
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Conduct & Log Delivery Notes"
    class="w-full max-w-xl"
    :closable="!conductMutation.isPending.value"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <CheckCircle2 class="h-5 w-5 text-emerald-600" />
        <span class="font-semibold text-lg text-slate-800">Conduct Session & Log Delivery</span>
      </div>
    </template>

    <div v-if="activity" class="space-y-4 pt-2">
      <!-- Session Header Context Card -->
      <div class="rounded-xl border border-slate-200 bg-slate-50/70 p-4 space-y-2.5">
        <div class="flex items-start justify-between gap-3">
          <div>
            <h3 class="font-semibold text-slate-900 text-base leading-snug">
              {{ activity.title }}
            </h3>
            <p class="text-xs text-slate-600 font-mono mt-0.5">
              <span v-if="'learningUnitCode' in activity">
                {{ activity.learningUnitCode }} — {{ activity.learningUnitName }}
              </span>
              <span v-if="activity.sectionLabel" class="ml-2 font-sans font-semibold text-indigo-700 bg-indigo-50 px-1.5 py-0.5 rounded border border-indigo-100">
                Section {{ activity.sectionLabel }}
              </span>
            </p>
          </div>
          <Tag
            :value="activity.status.toUpperCase()"
            :severity="activity.status === 'conducted' ? 'success' : 'info'"
          />
        </div>

        <div class="flex flex-wrap items-center gap-4 text-xs text-slate-600 pt-1 border-t border-slate-200/60">
          <div class="flex items-center gap-1.5">
            <Calendar class="h-3.5 w-3.5 text-slate-400" />
            <span>{{ activity.activityDate }}</span>
          </div>
          <div v-if="activity.startTime || activity.endTime" class="flex items-center gap-1.5">
            <Clock class="h-3.5 w-3.5 text-slate-400" />
            <span>{{ activity.startTime ?? '—' }} – {{ activity.endTime ?? '—' }}</span>
          </div>
        </div>
      </div>

      <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Curriculum Delivery Notes -->
        <FormField
          label="Curriculum Milestones & Delivery Notes"
          :error="errors.deliveryNotes"
          hint="Topics covered, textbook chapters, exercises completed, or instructional observations"
        >
          <Textarea
            v-model="deliveryNotes"
            rows="5"
            placeholder="e.g. Covered Chapter 4 (Quadratic Equations), exercises 1 through 15 completed by students. Homework assigned on problems 16-20."
            class="w-full"
            :invalid="Boolean(errors.deliveryNotes)"
          />
        </FormField>

        <!-- Teacher Name Confirmation -->
        <FormField
          label="Teacher Name (Conducted By)"
          :error="errors.actualTeacherName"
          hint="Confirmed instructor identity logged for audit trail and timetable reconciliation"
        >
          <InputText
            v-model="actualTeacherName"
            placeholder="e.g. Dr. Ada Lovelace"
            class="w-full"
            :invalid="Boolean(errors.actualTeacherName)"
          />
        </FormField>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            variant="outlined"
            :disabled="conductMutation.isPending.value"
            @click="isDialogVisible = false"
          />
          <Button
            type="submit"
            label="Complete & Log Session"
            icon="pi pi-check"
            class="bg-emerald-600 hover:bg-emerald-700 text-white"
            :loading="conductMutation.isPending.value"
          />
        </div>
      </form>
    </div>
  </Dialog>
</template>
