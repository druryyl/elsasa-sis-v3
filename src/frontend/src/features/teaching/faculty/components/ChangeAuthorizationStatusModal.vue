<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { ShieldAlert, AlertTriangle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { updateAuthorizationStatusSchema } from '../api/teachingSchemas';
import { useUpdateAuthorizationStatusMutation } from '../composables/useFacultyMutations';
import type {
  TeachingAuthorizationDto,
  AuthorizationStatus,
  UpdateTeachingAuthorizationStatusRequest,
  UpdateTeachingAuthorizationStatusResponse,
} from '../api/teachingTypes';

export interface ChangeAuthorizationStatusModalProps {
  visible: boolean;
  authorization: TeachingAuthorizationDto | null;
  teacherId?: string;
  appendTo?: string;
}

const props = withDefaults(defineProps<ChangeAuthorizationStatusModalProps>(), {
  visible: false,
  authorization: null,
  teacherId: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'changed', response: UpdateTeachingAuthorizationStatusResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const statusOptions = [
  { label: 'Active (Fully Authorized)', value: 'active' },
  { label: 'Authorized Under Supervision (Conditional)', value: 'authorized_under_supervision' },
  { label: 'Restricted (Limited Academic Scope)', value: 'restricted' },
  { label: 'Suspended (Temporarily Barred from Teaching)', value: 'suspended' },
  { label: 'Revoked (Formally Cancelled)', value: 'revoked' },
  { label: 'Expired (Term Concluded)', value: 'expired' },
];

const form = ref<{
  status: AuthorizationStatus;
  statusReason: string;
}>({
  status: 'active',
  statusReason: '',
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    status: (props.authorization?.status as AuthorizationStatus) ?? 'active',
    statusReason: '',
  };
  errors.value = {};
}

watch(
  () => [props.visible, props.authorization],
  ([isOpen]) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

const statusMutation = useUpdateAuthorizationStatusMutation();

async function handleSubmit() {
  if (!props.authorization) return;
  errors.value = {};

  const validationResult = updateAuthorizationStatusSchema.safeParse({
    status: form.value.status,
    statusReason: form.value.statusReason,
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

  const payload: UpdateTeachingAuthorizationStatusRequest = {
    status: form.value.status,
    statusReason: form.value.statusReason,
  };

  try {
    const result = await statusMutation.mutateAsync({
      id: props.authorization.id,
      payload,
      teacherId: props.teacherId,
    });
    emit('changed', result);
    isDialogVisible.value = false;
  } catch (err: unknown) {
    const errorObj = err as { response?: { data?: { detail?: string; title?: string } } };
    errors.value.general =
      errorObj.response?.data?.detail ||
      errorObj.response?.data?.title ||
      'Failed to update authorization status.';
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
    data-testid="change-authorization-status-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-rose-50 dark:bg-rose-950/60 border border-rose-200 dark:border-rose-800 flex items-center justify-center text-rose-600 dark:text-rose-400">
          <ShieldAlert class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-bold text-surface-900 dark:text-surface-100">
            Change Authorization Status
          </h2>
          <p class="text-xs text-surface-500">
            Authorization {{ authorization?.authorizationNumber }}
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <div
        v-if="authorization"
        class="p-2.5 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 text-xs text-surface-600 dark:text-surface-300"
      >
        <span class="font-semibold block text-surface-700 dark:text-surface-200">Scope:</span>
        {{ authorization.scopeDescription }}
      </div>

      <!-- Impact warning for restrictive states -->
      <div
        v-if="form.status === 'suspended' || form.status === 'revoked'"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/40 border border-rose-300 dark:border-rose-800 text-rose-800 dark:text-rose-300 text-xs flex items-start gap-2"
        data-testid="status-impact-warning"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 mt-0.5 text-rose-600 dark:text-rose-400" />
        <div>
          <span class="font-bold block">Downstream Scheduling Impact:</span>
          Marking this authorization as <em>{{ form.status }}</em> will prevent this faculty member from being scheduled or assigned to learning units under this scope in Academics and Operations.
        </div>
      </div>

      <div
        v-if="errors.general"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
        data-testid="change-auth-status-error"
      >
        {{ errors.general }}
      </div>

      <form
        class="space-y-3.5"
        data-testid="change-auth-status-form"
        @submit.prevent="handleSubmit"
      >
        <FormField
          label="New Authorization Status"
          required
          :error="errors.status"
          hint="Target governance lifecycle state"
        >
          <Select
            v-model="form.status"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="select-auth-status"
          />
        </FormField>

        <FormField
          label="Justification Reason"
          required
          :error="errors.statusReason"
          hint="Mandatory institutional rationale for modifying this authorization state"
        >
          <Textarea
            v-model="form.statusReason"
            rows="3"
            placeholder="e.g. Temporary suspension pending administrative inquiry into course delivery compliance."
            class="w-full"
            :invalid="Boolean(errors.statusReason)"
            data-testid="input-auth-status-reason"
          />
        </FormField>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-200 dark:border-surface-700">
        <Button
          label="Cancel"
          severity="secondary"
          variant="text"
          size="small"
          :disabled="statusMutation.isPending.value"
          data-testid="cancel-change-auth-status-button"
          @click="handleClose"
        />
        <Button
          label="Update Status"
          icon="pi pi-check"
          severity="primary"
          size="small"
          :loading="statusMutation.isPending.value"
          data-testid="submit-change-auth-status-button"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
