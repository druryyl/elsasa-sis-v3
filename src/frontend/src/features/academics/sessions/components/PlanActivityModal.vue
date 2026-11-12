<script setup lang="ts">
import { computed, watch, ref } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import { CalendarPlus, Clock, User, BookOpen, AlertCircle } from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useQuery } from '@tanstack/vue-query';
import { FormField } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { getAvailableLearningUnits } from '../api/sessions.api';
import {
  planActivitySchema,
  ACTIVITY_TYPES,
  type PlanActivityFormValues,
} from '../schemas/sessions.schema';
import { usePlanActivityMutation } from '../composables/useLearningActivities';
import type { ActivityType, PlanActivityResponse } from '../types';

export interface LearningUnitOption {
  id: string;
  code: string;
  name: string;
}

const props = withDefaults(
  defineProps<{
    visible: boolean;
    initialDate?: string;
    units?: LearningUnitOption[];
    preselectedUnitId?: string | null;
    preselectedSectionLabel?: string | null;
  }>(),
  {
    initialDate: '',
    units: undefined,
    preselectedUnitId: null,
    preselectedSectionLabel: null,
  },
);

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'planned', activity: PlanActivityResponse): void;
}>();

const authStore = useAuthStore();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Fetch active learning units if not provided via props
const { data: fetchedUnits, isLoading: isLoadingUnits } = useQuery({
  queryKey: ['academics', 'learning-units', 'available'],
  queryFn: () => getAvailableLearningUnits(),
  enabled: computed(
    () => isDialogVisible.value && (!props.units || props.units.length === 0),
  ),
});

const unitOptions = computed<LearningUnitOption[]>(() => {
  if (props.units && props.units.length > 0) {
    return props.units;
  }
  return (fetchedUnits.value ?? []).map((u) => ({
    id: u.id,
    code: u.code,
    name: u.name,
  }));
});

const formattedUnitOptions = computed(() =>
  unitOptions.value.map((u) => ({
    label: `${u.code} — ${u.name}`,
    value: u.id,
  })),
);

const activityTypeOptions = ACTIVITY_TYPES.map((type) => {
  if (type === 'homeroom_session') {
    return {
      label: 'Homeroom Session (Statutory Attendance)',
      value: type,
    };
  }
  return {
    label: type
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' '),
    value: type,
  };
});

function getTodayIso(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const defaultDate = computed(() => props.initialDate || getTodayIso());
const defaultTeacher = computed(() => authStore.user?.fullName ?? authStore.user?.username ?? '');

const { handleSubmit, errors, defineField, resetForm, setFieldValue } =
  useForm<PlanActivityFormValues>({
    validationSchema: toTypedSchema(planActivitySchema),
    initialValues: {
      learningUnitId: props.preselectedUnitId ?? '',
      activityType: 'lesson' as ActivityType,
      title: '',
      description: '',
      sectionLabel: props.preselectedSectionLabel ?? '',
      activityDate: defaultDate.value,
      startTime: '08:00',
      endTime: '09:30',
      teacherName: defaultTeacher.value,
      isAttendanceRequired: true,
    },
  });

const [learningUnitId] = defineField('learningUnitId');
const [activityType] = defineField('activityType');
const [title] = defineField('title');
const [description] = defineField('description');
const [sectionLabel] = defineField('sectionLabel');
const [activityDate] = defineField('activityDate');
const [startTime] = defineField('startTime');
const [endTime] = defineField('endTime');
const [teacherName] = defineField('teacherName');
const [isAttendanceRequired] = defineField('isAttendanceRequired');

// Auto-fill title helper when activity type or unit changes if title is empty
function onActivityTypeChange(newType: string) {
  if (newType === 'homeroom_session') {
    setFieldValue('isAttendanceRequired', true);
    if (!title.value || title.value.trim() === '') {
      setFieldValue('title', 'Morning Homeroom & Statutory Roll Call');
    }
  }
}

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm({
        values: {
          learningUnitId: props.preselectedUnitId ?? (unitOptions.value[0]?.id || ''),
          activityType: 'lesson',
          title: '',
          description: '',
          sectionLabel: props.preselectedSectionLabel ?? '',
          activityDate: props.initialDate || getTodayIso(),
          startTime: '08:00',
          endTime: '09:30',
          teacherName: defaultTeacher.value,
          isAttendanceRequired: true,
        },
      });
    }
  },
);

// Auto-select the first available unit once the lookup resolves (options load asynchronously).
watch(unitOptions, (units) => {
  if (!isDialogVisible.value || units.length === 0) return;
  if (!learningUnitId.value) {
    setFieldValue('learningUnitId', props.preselectedUnitId ?? units[0].id, false);
  }
});

const planMutation = usePlanActivityMutation();

const onSubmit = handleSubmit((values) => {
  planMutation.mutate(
    {
      learningUnitId: values.learningUnitId,
      activityType: values.activityType,
      title: values.title.trim(),
      description: values.description ? values.description.trim() : null,
      sectionLabel: values.sectionLabel ? values.sectionLabel.trim() : null,
      activityDate: values.activityDate,
      startTime: values.startTime ? values.startTime.trim() : null,
      endTime: values.endTime ? values.endTime.trim() : null,
      teacherName: values.teacherName ? values.teacherName.trim() : null,
      isAttendanceRequired: Boolean(values.isAttendanceRequired),
    },
    {
      onSuccess: (data) => {
        emit('planned', data);
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
    header="Plan Class Session"
    class="w-full max-w-2xl"
    :closable="!planMutation.isPending.value"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <CalendarPlus class="h-5 w-5 text-indigo-600" />
        <span class="font-semibold text-lg text-slate-800">Plan Class Session</span>
      </div>
    </template>

    <form @submit.prevent="onSubmit" class="space-y-4 pt-2">
      <!-- Statutory Homeroom Callout Banner -->
      <div
        v-if="activityType === 'homeroom_session'"
        class="flex items-start gap-2.5 rounded-lg border border-indigo-200 bg-indigo-50/80 p-3 text-xs text-indigo-900"
      >
        <AlertCircle class="h-4 w-4 shrink-0 text-indigo-600 mt-0.5" />
        <div>
          <span class="font-semibold">Statutory Daily Attendance Mode:</span>
          Homeroom sessions record compulsory daily statutory attendance. Attendance is strictly enforced.
        </div>
      </div>

      <!-- Learning Unit Selector -->
      <FormField
        label="Learning Unit"
        required
        :error="errors.learningUnitId"
        hint="Curriculum learning unit or subject container"
      >
        <Select
          v-model="learningUnitId"
          :options="formattedUnitOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Select learning unit"
          class="w-full"
          :loading="isLoadingUnits"
          :invalid="Boolean(errors.learningUnitId)"
        />
      </FormField>

      <!-- Row: Activity Type & Section Label -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <FormField
          label="Activity Type"
          required
          :error="errors.activityType"
        >
          <Select
            v-model="activityType"
            :options="activityTypeOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Select type"
            class="w-full"
            :invalid="Boolean(errors.activityType)"
            @update:model-value="onActivityTypeChange"
          />
        </FormField>

        <FormField
          label="Cohort / Section Label"
          :error="errors.sectionLabel"
          hint="e.g. 4A, 10B, or Grade 1 Blue"
        >
          <InputText
            v-model="sectionLabel"
            placeholder="e.g. 4A"
            class="w-full"
            :invalid="Boolean(errors.sectionLabel)"
          />
        </FormField>
      </div>

      <!-- Title -->
      <FormField
        label="Session Title"
        required
        :error="errors.title"
      >
        <InputText
          v-model="title"
          placeholder="e.g. Morning Homeroom Roll Call / Linear Equations & Graphing"
          class="w-full"
          :invalid="Boolean(errors.title)"
        />
      </FormField>

      <!-- Row: Date & Times -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <FormField
          label="Activity Date"
          required
          :error="errors.activityDate"
        >
          <InputText
            v-model="activityDate"
            type="date"
            class="w-full"
            :invalid="Boolean(errors.activityDate)"
          />
        </FormField>

        <FormField
          label="Start Time"
          :error="errors.startTime"
        >
          <InputText
            v-model="startTime"
            type="time"
            class="w-full"
            :invalid="Boolean(errors.startTime)"
          />
        </FormField>

        <FormField
          label="End Time"
          :error="errors.endTime"
        >
          <InputText
            v-model="endTime"
            type="time"
            class="w-full"
            :invalid="Boolean(errors.endTime)"
          />
        </FormField>
      </div>

      <!-- Provisional Teacher Name -->
      <FormField
        label="Conducting Teacher (Provisional)"
        :error="errors.teacherName"
        hint="Instructor in charge for timetable display & conducting logs"
      >
        <div class="relative">
          <InputText
            v-model="teacherName"
            placeholder="e.g. Dr. Ada Lovelace"
            class="w-full"
            :invalid="Boolean(errors.teacherName)"
          />
        </div>
      </FormField>

      <!-- Description / Agenda -->
      <FormField
        label="Description / Planned Agenda"
        :error="errors.description"
      >
        <Textarea
          v-model="description"
          rows="3"
          placeholder="Optional instructional agenda, learning objectives, or materials needed..."
          class="w-full"
          :invalid="Boolean(errors.description)"
        />
      </FormField>

      <!-- Attendance Required Checkbox -->
      <div class="flex items-center gap-2 pt-1">
        <Checkbox
          v-model="isAttendanceRequired"
          :binary="true"
          inputId="isAttendanceRequired"
        />
        <label for="isAttendanceRequired" class="text-sm font-medium text-slate-700 cursor-pointer">
          Roll-Call Attendance Required for this Session
        </label>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="planMutation.isPending.value"
          @click="isDialogVisible = false"
        />
        <Button
          type="submit"
          label="Plan Session"
          icon="pi pi-check"
          class="bg-indigo-600 hover:bg-indigo-700 text-white"
          :loading="planMutation.isPending.value"
        />
      </div>
    </form>
  </Dialog>
</template>
