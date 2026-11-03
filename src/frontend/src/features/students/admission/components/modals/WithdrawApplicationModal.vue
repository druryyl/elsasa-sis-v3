<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { AlertCircle, Ban } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useWithdrawApplicationMutation } from '../../composables/useAdmissionMutations';
import type { WithdrawApplicationResultDto } from '../../types/admission.types';

const props = defineProps<{
  visible: boolean;
  applicationId: string;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'withdrawn', result: WithdrawApplicationResultDto): void;
}>();

const withdrawReason = ref('');
const error = ref('');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      withdrawReason.value = '';
      error.value = '';
    }
  },
);

const withdrawMutation = useWithdrawApplicationMutation({
  onSuccess: (data) => {
    emit('withdrawn', data);
    emit('update:visible', false);
  },
});

function handleClose() {
  emit('update:visible', false);
}

function handleSubmit() {
  const reason = withdrawReason.value.trim();
  if (!reason) {
    error.value = 'Withdrawal reason is required.';
    return;
  }

  error.value = '';
  withdrawMutation.mutate({
    id: props.applicationId,
    payload: {
      withdrawalReason: reason,
    },
  });
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Withdraw Admission Application"
    class="max-w-md w-full"
    data-testid="withdraw-application-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="space-y-4 pt-1 text-xs">
      <div class="flex items-start gap-2.5 p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-800">
        <AlertCircle class="w-5 h-5 shrink-0 text-rose-600 mt-0.5" />
        <p>
          Withdrawing an application marks it as inactive. All documents and records remain preserved for compliance audit trail.
        </p>
      </div>

      <FormField
        label="Cancellation / Withdrawal Reason"
        required
        :error="error"
      >
        <Textarea
          v-model="withdrawReason"
          rows="3"
          placeholder="Explain reason for withdrawal (e.g. family relocated, duplicate entry)..."
          class="w-full !text-xs"
          data-testid="withdraw-reason-textarea"
        />
      </FormField>

      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="!text-xs cursor-pointer"
          @click="handleClose"
        >
          Cancel
        </Button>
        <Button
          type="button"
          size="small"
          severity="danger"
          :disabled="!withdrawReason.trim()"
          :loading="withdrawMutation.isPending.value"
          class="!text-xs cursor-pointer"
          data-testid="confirm-withdraw-button"
          @click="handleSubmit"
        >
          <Ban class="w-3.5 h-3.5 mr-1" />
          <span>Confirm Withdrawal</span>
        </Button>
      </div>
    </div>
  </Dialog>
</template>
