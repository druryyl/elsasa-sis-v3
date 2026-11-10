<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { Users, Info } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { addSupervisionSchema } from '../api/teachingSchemas';
import { useAddSupervisionMutation } from '../composables/useFacultyMutations';
import type {
  AddSupervisionRequirementRequest,
  AddSupervisionRequirementResponse,
  SupervisionType,
} from '../api/teachingTypes';

export interface AddSupervisionModalProps {
  visible: boolean;
  authorizationId: string;
  authorizationNumber?: string;
  teacherId?: string;
  appendTo?: string;
}

const props = withDefaults(defineProps<AddSupervisionModalProps>(), {
  visible: false,
  authorizationNumber: '',
  teacherId: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'added', response: AddSupervisionRequirementResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

function getTodayIso(): string {
  return new Date().toISOString().substring(0, 10);
}

const supervisionTypeOptions = [
  { label: 'Co-Teaching (Teaching alongside Senior Mentor)', value: 'co_teaching' },
  { label: 'Periodic Classroom Observation', value: 'periodic_observation' },
  { label: 'Lesson Plan & Syllabi Review', value: 'lesson_plan_review' },
  { label: 'General Pedagogical Mentorship', value: 'general_mentorship' },
];

const form = ref<AddSupervisionRequirementRequest>({
  supervisionType: 'co_teaching',
  description: '',
  startDate: getTodayIso(),
  endDate: '',
  supervisingTeacherId: '',
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    supervisionType: 'co_teaching',
    description: '',
    startDate: getTodayIso(),
    endDate: '',
    supervisingTeacherId: '',
  };
  errors.value = {};
}

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

const addMutation = useAddSupervisionMutation();

async function handleSubmit() {
  errors.value = {};

  const validationResult = addSupervisionSchema.safeParse({
    supervisionType: form.value.supervisionType,
    description: form.value.description,
    startDate: form.value.startDate,
    endDate: form.value.endDate || undefined,
    supervisingTeacherId: form.value.supervisingTeacherId || undefined,
  });

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const field = issue.path[0] ? String(issue.path[0]) : 'general';
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return;
  }

  const payload: AddSupervisionRequirementRequest = {
    supervisionType: form.value.supervisionType,
    description: form.value.description,
    startDate: form.value.startDate,
    endDate: form.value.endDate || null,
    supervisingTeacherId: form.value.supervisingTeacherId || null,
  };

  try {
    const result = await addMutation.mutateAsync({
      authorizationId: props.authorizationId,
      payload,
      teacherId: props.teacherId,
    });
    emit('added', result);
    isDialogVisible.value = false;
  } catch (err: unknown) {
    const errorObj = err as { response?: { data?: { detail?: string; title?: string } } };
    errors.value.general =
      errorObj.response?.data?.detail ||
      errorObj.response?.data?.title ||
      'Failed to attach supervision requirement.';
  }
}

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    :append-to="appendTo"
    class="w-full max-w-lg mx-4"
    data-testid="add-supervision-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-amber-50 dark:bg-amber-950/60 border border-amber-200 dark:border-amber-800 flex items-center justify-center text-amber-600 dark:text-amber-400">
          <Users class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-bold text-surface-900 dark:text-surface-100">
            Attach Supervision Requirement
          </h2>
          <p class="text-xs text-surface-500">
            Authorization {{ authorizationNumber || authorizationId }}
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <div
        v-if="errors.general"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
        data-testid="add-supervision-error"
      >
        {{ errors.general }}
      </div>

      <form
        class="space-y-3.5"
        data-testid="add-supervision-form"
        @submit.prevent="handleSubmit"
      >
        <FormField
          label="Supervision Modality"
          required
          :error="errors.supervisionType"
          hint="Required mentoring or supervision protocol"
        >
          <Select
            v-model="form.supervisionType"
            :options="supervisionTypeOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="select-supervision-type"
          />
        </FormField>

        <FormField
          label="Supervision Description & Deliverables"
          required
          :error="errors.description"
          hint="Concrete observation schedule, review checkpoints, or guidance notes"
        >
          <Textarea
            v-model="form.description"
            rows="3"
            placeholder="e.g. Conduct monthly classroom observation and review syllabus planning."
            class="w-full"
            :invalid="Boolean(errors.description)"
            data-testid="input-supervision-description"
          />
        </FormField>

        <FormField
          label="Designating Supervising Teacher / Mentor Reference"
          :error="errors.supervisingTeacherId"
          hint="Name or identifier of assigned senior mentor"
        >
          <InputText
            v-model="form.supervisingTeacherId"
            placeholder="e.g. Budi Hartono, M.Pd. or Teacher Profile ID"
            class="w-full"
            data-testid="input-mentor-reference"
          />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Start Date"
            required
            :error="errors.startDate"
          >
            <InputText
              v-model="form.startDate"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.startDate)"
              data-testid="input-start-date"
            />
          </FormField>

          <FormField
            label="Target End Date"
            :error="errors.endDate"
          >
            <InputText
              v-model="form.endDate"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.endDate)"
              data-testid="input-end-date"
            />
          </FormField>
        </div>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-200 dark:border-surface-700">
        <Button
          label="Cancel"
          severity="secondary"
          variant="text"
          size="small"
          :disabled="addMutation.isPending.value"
          data-testid="cancel-supervision-button"
          @click="handleClose"
        />
        <Button
          label="Attach Supervision"
          icon="pi pi-check"
          severity="primary"
          size="small"
          :loading="addMutation.isPending.value"
          data-testid="submit-supervision-button"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
