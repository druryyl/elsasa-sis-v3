<script setup lang="ts">
import { ref, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { CheckCircle2, ShieldCheck, AlertCircle, Clock, Calendar, BookOpen } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useApproveAssignmentMutation } from '../composables/useAssignments';
import type { TeachingAssignmentDto, ApproveTeachingAssignmentResponse } from '../types';

export interface ApproveAssignmentModalProps {
  visible: boolean;
  assignment?: TeachingAssignmentDto | null;
}

const props = withDefaults(defineProps<ApproveAssignmentModalProps>(), {
  visible: false,
  assignment: null,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'approved', res: ApproveTeachingAssignmentResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const notes = ref<string>('');

const approveMutation = useApproveAssignmentMutation();

function handleClose() {
  isDialogVisible.value = false;
  notes.value = '';
  emit('close');
}

function handleApprove() {
  if (!props.assignment) return;

  approveMutation.mutate(
    {
      id: props.assignment.id,
      payload: {
        notes: notes.value.trim() || undefined,
      },
    },
    {
      onSuccess: (res) => {
        emit('approved', res);
        handleClose();
      },
    },
  );
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    header="Approve Teaching Assignment"
    class="w-full max-w-lg"
    data-testid="approve-assignment-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-emerald-50 dark:bg-emerald-950 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
          <ShieldCheck class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-0">
            Teaching Authority Approval
          </h2>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Confirm formal assignment commitment and workload capacity allocation
          </p>
        </div>
      </div>
    </template>

    <div v-if="assignment" class="space-y-4 pt-2">
      <!-- Assignment Summary Box -->
      <div class="p-3.5 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 space-y-2 text-xs">
        <div class="flex items-center justify-between">
          <span class="font-semibold text-surface-900 dark:text-surface-100 font-mono">
            {{ assignment.assignmentNumber }}
          </span>
          <span class="px-2 py-0.5 rounded-md text-[11px] font-medium bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300">
            Status: {{ assignment.status }}
          </span>
        </div>

        <div class="grid grid-cols-2 gap-2 text-surface-600 dark:text-surface-300 pt-1">
          <div>
            <span class="text-surface-400 block text-[10px] uppercase tracking-wider">Scope</span>
            <span class="font-medium text-surface-900 dark:text-surface-100">{{ assignment.scopeLabel }}</span>
          </div>
          <div>
            <span class="text-surface-400 block text-[10px] uppercase tracking-wider">Role</span>
            <span class="font-medium text-surface-900 dark:text-surface-100">{{ assignment.assignmentRoleName || 'Lead Teacher' }}</span>
          </div>
          <div>
            <span class="text-surface-400 block text-[10px] uppercase tracking-wider">Committed Load</span>
            <span class="font-medium text-emerald-700 dark:text-emerald-400 font-mono">{{ assignment.workloadCommitmentValue }} hrs/wk</span>
          </div>
          <div>
            <span class="text-surface-400 block text-[10px] uppercase tracking-wider">Effective Term</span>
            <span class="font-medium">{{ assignment.effectiveStartDate }} to {{ assignment.effectiveEndDate }}</span>
          </div>
        </div>
      </div>

      <!-- Capacity Notice -->
      <div class="flex items-start gap-2.5 p-3 rounded-lg bg-emerald-50/60 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-xs text-emerald-900 dark:text-emerald-300">
        <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
        <p>
          Approving this assignment will transition status to <strong>approved</strong>, commit
          <strong>{{ assignment.workloadCommitmentValue }} hours</strong> to faculty capacity snapshots in <code>teaching.workload_commitments</code>, and publish <code>TeachingAssignmentApprovedEvent</code>.
        </p>
      </div>

      <!-- Authority Decision Notes -->
      <FormField
        label="Authority Remarks / Approval Notes (Optional)"
        description="Recorded with approval timestamp in teaching audit logs."
      >
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="e.g. Approved under Semester 2 instructional plan."
          class="w-full text-xs"
          data-testid="input-approve-notes"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="approveMutation.isPending.value"
          @click="handleClose"
          data-testid="btn-cancel-approve"
        />
        <Button
          type="button"
          label="Grant Authority Approval"
          severity="success"
          :loading="approveMutation.isPending.value"
          @click="handleApprove"
          data-testid="btn-confirm-approve"
        >
          <template #icon>
            <ShieldCheck class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
