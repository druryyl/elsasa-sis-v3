<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { HelpCircle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useUpdateDemandStatusMutation } from '../composables/useAssignments';
import type { TeachingDemandDto } from '../types';

export interface RequestClarificationModalProps {
  visible: boolean;
  demand?: TeachingDemandDto | null;
}

const props = withDefaults(defineProps<RequestClarificationModalProps>(), {
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

const clarificationNotes = ref<string>('');
const error = ref<string | null>(null);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      clarificationNotes.value = props.demand?.clarificationNotes || '';
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

function handleClarify() {
  if (!clarificationNotes.value.trim()) {
    error.value = 'Clarification notes are required to explain what information is missing.';
    return;
  }
  if (!props.demand) return;

  error.value = null;
  updateStatusMutation.mutate({
    id: props.demand.id,
    payload: {
      status: 'returned_for_clarification',
      clarificationNotes: clarificationNotes.value.trim(),
    },
  });
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Request Demand Clarification"
    class="w-full max-w-lg"
    :dismissable-mask="true"
    data-testid="request-clarification-modal"
  >
    <div class="space-y-4 pt-2">
      <div class="bg-amber-50/60 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 rounded-lg p-3 text-xs text-amber-900 dark:text-amber-200 flex items-start gap-2.5">
        <HelpCircle class="w-4 h-4 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
        <div>
          <p class="font-medium">Return for Clarification</p>
          <p class="text-amber-700 dark:text-amber-300">
            Send this capacity demand back to the requesting department to request additional schedule specifications or syllabus details.
          </p>
        </div>
      </div>

      <div v-if="demand" class="text-xs bg-surface-50 dark:bg-surface-800/40 p-2.5 rounded-lg border border-surface-200 dark:border-surface-700 space-y-1">
        <p><span class="font-medium text-surface-600 dark:text-surface-400">Demand:</span> {{ demand.demandCode }} — {{ demand.sourceWorkLabel }}</p>
        <p><span class="font-medium text-surface-600 dark:text-surface-400">Expected Hours:</span> {{ demand.expectedWorkloadHours }} {{ demand.workloadUnit }}</p>
      </div>

      <FormField
        label="Clarification Request Notes"
        name="clarificationNotes"
        :error="error || undefined"
        required
      >
        <Textarea
          v-model="clarificationNotes"
          rows="3"
          placeholder="e.g. Please clarify exact class timetable blocks or specify if co-teaching is required..."
          class="w-full text-sm"
          data-testid="input-clarification-notes"
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
          data-testid="cancel-clarification-btn"
        />
        <Button
          label="Send Clarification Request"
          severity="warn"
          size="small"
          :loading="updateStatusMutation.isPending.value"
          @click="handleClarify"
          data-testid="submit-clarification-btn"
        />
      </div>
    </template>
  </Dialog>
</template>
