<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { AlertTriangle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useUpdateDemandStatusMutation } from '../composables/useAssignments';
import type { TeachingDemandDto } from '../types';

export interface DeclineDemandModalProps {
  visible: boolean;
  demand?: TeachingDemandDto | null;
}

const props = withDefaults(defineProps<DeclineDemandModalProps>(), {
  visible: false,
  demand: null,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'submitted'): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const declineReason = ref<string>('');
const error = ref<string | null>(null);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      declineReason.value = '';
      error.value = null;
    }
  },
);

const updateStatusMutation = useUpdateDemandStatusMutation({
  onSuccess: () => {
    emit('submitted');
    isDialogVisible.value = false;
    emit('close');
  },
});

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}

function handleDecline() {
  if (!declineReason.value.trim()) {
    error.value = 'Decline reason is mandatory when declining a teaching demand.';
    return;
  }
  if (!props.demand) return;

  error.value = null;
  updateStatusMutation.mutate({
    id: props.demand.id,
    payload: {
      status: 'declined',
      declineReason: declineReason.value.trim(),
    },
  });
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Decline Teaching Demand"
    class="w-full max-w-lg"
    :dismissable-mask="true"
    data-testid="decline-demand-modal"
  >
    <div class="space-y-4 pt-2">
      <div class="bg-rose-50/60 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 rounded-lg p-3 text-xs text-rose-900 dark:text-rose-200 flex items-start gap-2.5">
        <AlertTriangle class="w-4 h-4 text-rose-600 dark:text-rose-400 shrink-0 mt-0.5" />
        <div>
          <p class="font-medium">Formal Demand Rejection</p>
          <p class="text-rose-700 dark:text-rose-300">
            Declining this demand notifies the requesting department that staffing cannot be fulfilled. Please specify the institutional justification.
          </p>
        </div>
      </div>

      <div v-if="demand" class="text-xs bg-surface-50 dark:bg-surface-800/40 p-2.5 rounded-lg border border-surface-200 dark:border-surface-700 space-y-1">
        <p><span class="font-medium text-surface-600 dark:text-surface-400">Demand:</span> {{ demand.demandCode }} — {{ demand.sourceWorkLabel }}</p>
        <p><span class="font-medium text-surface-600 dark:text-surface-400">Domain:</span> {{ demand.sourceDomain }} | <span class="font-medium text-surface-600 dark:text-surface-400">Campus:</span> {{ demand.campusScope }}</p>
      </div>

      <FormField
        label="Reason for Declining"
        name="declineReason"
        :error="error || undefined"
        required
      >
        <Textarea
          v-model="declineReason"
          rows="3"
          placeholder="e.g. Inadequate faculty capacity, course canceled, or duplicate demand..."
          class="w-full text-sm"
          data-testid="input-decline-reason"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="updateStatusMutation.isPending.value"
          @click="handleClose"
          data-testid="cancel-decline-btn"
        />
        <Button
          label="Confirm Decline"
          severity="danger"
          size="small"
          :loading="updateStatusMutation.isPending.value"
          @click="handleDecline"
          data-testid="submit-decline-btn"
        />
      </div>
    </template>
  </Dialog>
</template>
