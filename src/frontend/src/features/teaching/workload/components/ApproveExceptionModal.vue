<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import {
  CheckCircle2,
  AlertTriangle,
  Clock,
  Calendar,
  FileText,
  ShieldCheck,
  User,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  approveExceptionSchema,
  type ApproveExceptionFormValues,
} from '../api/schemas';
import {
  useApproveExceptionMutation,
  useWorkloadExceptions,
} from '../composables/useWorkload';
import type {
  TeacherWorkloadCommitmentDto,
  WorkloadExceptionDto,
  ApproveWorkloadExceptionResponse,
} from '../types';

export interface ApproveExceptionModalProps {
  visible: boolean;
  commitment?: TeacherWorkloadCommitmentDto | null;
  exception?: WorkloadExceptionDto | null;
  academicPeriodId?: string;
}

const props = withDefaults(defineProps<ApproveExceptionModalProps>(), {
  visible: false,
  commitment: null,
  exception: null,
  academicPeriodId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'approved', response: ApproveWorkloadExceptionResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const decisionRemarks = ref<string>('');
const formErrors = ref<Record<string, string>>({});

// If exception is not directly passed, query exceptions for this teacher
const teacherProfileId = computed(() => props.commitment?.teacherProfileId);
const periodId = computed(
  () => props.academicPeriodId || props.commitment?.academicPeriodId,
);

const { data: fetchedExceptions, isLoading: isLoadingExceptions } = useWorkloadExceptions(
  computed(() => ({
    teacherProfileId: teacherProfileId.value,
    academicPeriodId: periodId.value,
  })),
  {
    enabled: computed(() => Boolean(props.visible && !props.exception && teacherProfileId.value)),
  },
);

const activeException = computed<WorkloadExceptionDto | null>(() => {
  if (props.exception) return props.exception;
  if (fetchedExceptions.value && fetchedExceptions.value.length > 0) {
    // Find pending/requested/recommended exception first, else latest
    const pending = fetchedExceptions.value.find(
      (e) => e.status === 'requested' || e.status === 'recommended',
    );
    return pending || fetchedExceptions.value[0];
  }
  return null;
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      decisionRemarks.value = '';
      formErrors.value = {};
    }
  },
  { immediate: true },
);

const { mutate: approveException, isPending: isApproving } = useApproveExceptionMutation({
  onSuccess: (data) => {
    emit('approved', data);
    handleClose();
  },
});

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleApprove() {
  formErrors.value = {};

  const targetId = activeException.value?.id;
  if (!targetId) {
    formErrors.value.exceptionId = 'No workload exception found to approve.';
    return;
  }

  const payloadToValidate = {
    decisionRemarks: decisionRemarks.value.trim() || undefined,
  };

  const validationResult = approveExceptionSchema.safeParse(payloadToValidate);

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const field = issue.path[0] as string;
      if (field && !formErrors.value[field]) {
        formErrors.value[field] = issue.message;
      }
    }
    return;
  }

  const parsed = validationResult.data as ApproveExceptionFormValues;

  approveException({
    id: targetId,
    payload: {
      decisionRemarks: parsed.decisionRemarks ?? null,
    },
  });
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    header="Approve Workload Exception"
    class="w-full max-w-lg mx-4"
    data-testid="approve-exception-modal"
  >
    <div class="space-y-4">
      <!-- Loading exception details -->
      <div v-if="isLoadingExceptions" class="py-6 text-center text-xs text-slate-500">
        Loading pending exception records...
      </div>

      <!-- Exception summary card -->
      <div
        v-else-if="activeException"
        class="p-4 bg-purple-50/70 border border-purple-200 rounded-xl space-y-2.5"
      >
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <ShieldCheck class="w-4 h-4 text-purple-600" />
            <span class="text-xs font-mono font-bold text-purple-900" data-testid="exception-number">
              {{ activeException.exceptionNumber }}
            </span>
          </div>
          <span class="text-xs font-semibold uppercase px-2 py-0.5 rounded-full bg-purple-100 text-purple-800">
            {{ activeException.status }}
          </span>
        </div>

        <div class="space-y-1 text-xs text-slate-700">
          <div v-if="commitment" class="flex items-center justify-between">
            <span class="text-slate-500">Faculty:</span>
            <strong class="text-slate-900">{{ commitment.fullName }}</strong>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-slate-500">Authorized Exceeded Effort:</span>
            <span class="font-bold text-purple-700 text-sm">
              +{{ activeException.exceededHours }} hrs/week
            </span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-slate-500">Effective Period:</span>
            <span>{{ activeException.effectiveStartDate }} to {{ activeException.effectiveEndDate }}</span>
          </div>
          <div v-if="activeException.recommendedBy" class="flex items-center justify-between">
            <span class="text-slate-500">Recommended By:</span>
            <span class="text-slate-700">{{ activeException.recommendedBy }}</span>
          </div>
        </div>

        <!-- Justification Quote -->
        <div class="mt-2 pt-2 border-t border-purple-100/80 text-xs">
          <div class="text-[11px] font-semibold text-purple-900 uppercase">
            Coordinator Justification:
          </div>
          <p class="mt-1 text-slate-600 italic bg-white/70 p-2.5 rounded-lg border border-purple-100">
            &ldquo;{{ activeException.justification }}&rdquo;
          </p>
        </div>
      </div>

      <!-- No exception record found -->
      <div
        v-else
        class="p-4 bg-amber-50 border border-amber-200 rounded-xl text-xs text-amber-800 flex items-start gap-2.5"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 text-amber-600 mt-0.5" />
        <div>
          <p class="font-semibold text-amber-900">No pending exception request detected</p>
          <p class="mt-0.5 text-amber-700">
            A coordinator must first recommend a formal workload exception before authority approval can be finalized.
          </p>
        </div>
      </div>

      <!-- Decision Remarks Form -->
      <form v-if="activeException" class="space-y-3" @submit.prevent="handleApprove">
        <FormField
          label="Authority Decision Remarks"
          :error="formErrors.decisionRemarks"
          hint="Optional executive remarks stamped onto the approved exception audit record."
        >
          <Textarea
            v-model="decisionRemarks"
            rows="3"
            class="w-full"
            placeholder="e.g. Approved under institutional staffing contingency for Semester 2..."
            data-testid="decision-remarks-input"
          />
        </FormField>

        <div
          v-if="formErrors.exceptionId"
          class="p-2.5 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700"
        >
          {{ formErrors.exceptionId }}
        </div>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="isApproving"
          @click="handleClose"
        />

        <Button
          v-if="activeException"
          type="button"
          label="Approve Exception"
          severity="success"
          size="small"
          :loading="isApproving"
          data-testid="confirm-approve-btn"
          @click="handleApprove"
        >
          <CheckCircle2 class="w-4 h-4 mr-1.5" />
          <span>Approve Exception</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
