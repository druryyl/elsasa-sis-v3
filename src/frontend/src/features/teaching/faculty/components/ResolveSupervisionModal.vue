<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { CheckCircle2, AlertCircle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { resolveSupervisionSchema } from '../api/teachingSchemas';
import { useResolveSupervisionMutation } from '../composables/useFacultyMutations';
import type {
  ResolveSupervisionRequirementRequest,
  ResolveSupervisionRequirementResponse,
} from '../api/teachingTypes';

export interface ResolveSupervisionModalProps {
  visible: boolean;
  supervisionId: string;
  authorizationNumber?: string;
  supervisionDescription?: string;
  teacherId?: string;
  appendTo?: string;
}

const props = withDefaults(defineProps<ResolveSupervisionModalProps>(), {
  visible: false,
  authorizationNumber: '',
  supervisionDescription: '',
  teacherId: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'resolved', response: ResolveSupervisionRequirementResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const resolutionStatusOptions = [
  { label: 'Completed (Successfully Fulfilled)', value: 'completed' },
  { label: 'Waived (Exempted by Teaching Authority)', value: 'waived' },
  { label: 'Escalated (Requires Further Administrative Review)', value: 'escalated' },
];

const form = ref<ResolveSupervisionRequirementRequest>({
  resolutionStatus: 'completed',
  resolutionNotes: '',
  updateAuthorizationToActive: true,
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    resolutionStatus: 'completed',
    resolutionNotes: '',
    updateAuthorizationToActive: true,
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

const resolveMutation = useResolveSupervisionMutation();

async function handleSubmit() {
  errors.value = {};

  const validationResult = resolveSupervisionSchema.safeParse({
    resolutionStatus: form.value.resolutionStatus,
    resolutionNotes: form.value.resolutionNotes,
    updateAuthorizationToActive: form.value.updateAuthorizationToActive,
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

  const payload: ResolveSupervisionRequirementRequest = {
    resolutionStatus: form.value.resolutionStatus,
    resolutionNotes: form.value.resolutionNotes,
    updateAuthorizationToActive: form.value.updateAuthorizationToActive,
  };

  try {
    const result = await resolveMutation.mutateAsync({
      id: props.supervisionId,
      payload,
      teacherId: props.teacherId,
    });
    emit('resolved', result);
    isDialogVisible.value = false;
  } catch (err: unknown) {
    const errorObj = err as { response?: { data?: { detail?: string; title?: string } } };
    errors.value.general =
      errorObj.response?.data?.detail ||
      errorObj.response?.data?.title ||
      'Failed to resolve supervision requirement.';
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
    data-testid="resolve-supervision-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-bold text-surface-900 dark:text-surface-100">
            Resolve Supervision Condition
          </h2>
          <p class="text-xs text-surface-500">
            Conclude mentorship requirement for {{ authorizationNumber || 'authorization' }}
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <div
        v-if="supervisionDescription"
        class="p-2.5 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 text-xs text-surface-600 dark:text-surface-300"
        data-testid="supervision-context-box"
      >
        <span class="font-semibold block text-surface-700 dark:text-surface-200">Condition Description:</span>
        {{ supervisionDescription }}
      </div>

      <div
        v-if="errors.general"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
        data-testid="resolve-supervision-error"
      >
        {{ errors.general }}
      </div>

      <form
        class="space-y-3.5"
        data-testid="resolve-supervision-form"
        @submit.prevent="handleSubmit"
      >
        <FormField
          label="Resolution Outcome"
          required
          :error="errors.resolutionStatus"
          hint="Evaluation outcome for this mentoring period"
        >
          <Select
            v-model="form.resolutionStatus"
            :options="resolutionStatusOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="select-resolution-status"
          />
        </FormField>

        <FormField
          label="Resolution Assessment & Notes"
          required
          :error="errors.resolutionNotes"
          hint="Detail the mentor feedback, classroom observation findings, or basis for waiving"
        >
          <Textarea
            v-model="form.resolutionNotes"
            rows="3"
            placeholder="e.g. Mentor confirmed satisfactory lesson delivery and independent teaching readiness."
            class="w-full"
            :invalid="Boolean(errors.resolutionNotes)"
            data-testid="input-resolution-notes"
          />
        </FormField>

        <div class="p-3 rounded-lg bg-surface-50 dark:bg-surface-800/60 border border-surface-200 dark:border-surface-700 flex items-start gap-2.5">
          <input
            id="updateAuthActiveCheck"
            v-model="form.updateAuthorizationToActive"
            type="checkbox"
            class="rounded text-emerald-600 focus:ring-emerald-500 h-4 w-4 mt-0.5 cursor-pointer"
            data-testid="check-update-authorization"
          />
          <label for="updateAuthActiveCheck" class="text-xs text-surface-700 dark:text-surface-300 cursor-pointer">
            <span class="font-semibold block">Promote authorization status to 'Active'</span>
            <span>If checked, the parent authorization status will automatically transition from 'authorized_under_supervision' to 'active'.</span>
          </label>
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
          :disabled="resolveMutation.isPending.value"
          data-testid="cancel-resolve-button"
          @click="handleClose"
        />
        <Button
          label="Resolve Supervision"
          icon="pi pi-check"
          severity="success"
          size="small"
          :loading="resolveMutation.isPending.value"
          data-testid="submit-resolve-button"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
