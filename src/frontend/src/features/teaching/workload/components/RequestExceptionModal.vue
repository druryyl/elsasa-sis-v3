<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  FilePlus2,
  Calendar,
  Clock,
  AlertCircle,
  FileText,
  User,
  ShieldAlert,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  requestExceptionSchema,
  type RequestExceptionFormValues,
} from '../api/schemas';
import { useRequestExceptionMutation } from '../composables/useWorkload';
import type {
  TeacherWorkloadCommitmentDto,
  RequestWorkloadExceptionRequest,
  RequestWorkloadExceptionResponse,
} from '../types';

export interface RequestExceptionModalProps {
  visible: boolean;
  commitment?: TeacherWorkloadCommitmentDto | null;
  academicPeriodId?: string;
}

const props = withDefaults(defineProps<RequestExceptionModalProps>(), {
  visible: false,
  commitment: null,
  academicPeriodId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'submitted', response: RequestWorkloadExceptionResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const requestedHours = ref<number | null>(4);
const coordinatorJustification = ref<string>('');
const effectiveStartDate = ref<string>('');
const effectiveEndDate = ref<string>('');
const recommendationStatus = ref<'requested' | 'recommended'>('recommended');
const formErrors = ref<Record<string, string>>({});

const statusOptions = [
  { label: 'Recommended by Coordinator', value: 'recommended' },
  { label: 'Formally Requested', value: 'requested' },
];

// Initialize default dates from commitment or reset form when modal opens
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      formErrors.value = {};
      coordinatorJustification.value = '';
      recommendationStatus.value = 'recommended';

      if (props.commitment) {
        // Calculate recommended overload hours if breached
        const excess = props.commitment.committedWorkloadHours - (props.commitment.maxCapacityLimitHours || 24);
        requestedHours.value = excess > 0 ? excess : 4;
      } else {
        requestedHours.value = 4;
      }

      // Default dates (e.g. today to ~6 months ahead)
      const now = new Date();
      effectiveStartDate.value = now.toISOString().split('T')[0];
      const future = new Date(now.getFullYear(), now.getMonth() + 6, now.getDate());
      effectiveEndDate.value = future.toISOString().split('T')[0];
    }
  },
  { immediate: true },
);

const { mutate: submitException, isPending: isSubmitting } = useRequestExceptionMutation({
  onSuccess: (data) => {
    emit('submitted', data);
    handleClose();
  },
});

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleSubmit() {
  formErrors.value = {};

  const teacherProfileId = props.commitment?.teacherProfileId;
  const academicPeriodId = props.academicPeriodId || props.commitment?.academicPeriodId;

  if (!teacherProfileId) {
    formErrors.value.teacherProfileId = 'Teacher profile is required.';
    return;
  }
  if (!academicPeriodId) {
    formErrors.value.academicPeriodId = 'Academic period is required.';
    return;
  }

  const payloadToValidate = {
    teacherProfileId,
    academicPeriodId,
    teachingAssignmentId: undefined,
    requestedOverloadHours: Number(requestedHours.value),
    coordinatorJustification: coordinatorJustification.value.trim(),
    effectiveStartDate: effectiveStartDate.value.trim() || undefined,
    effectiveEndDate: effectiveEndDate.value.trim() || undefined,
    status: recommendationStatus.value,
  };

  const validationResult = requestExceptionSchema.safeParse(payloadToValidate);

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const field = issue.path[0] as string;
      if (field && !formErrors.value[field]) {
        formErrors.value[field] = issue.message;
      }
    }
    return;
  }

  const parsed = validationResult.data as RequestExceptionFormValues;

  const requestPayload: RequestWorkloadExceptionRequest = {
    teacherProfileId: parsed.teacherProfileId,
    academicPeriodId: parsed.academicPeriodId,
    teachingAssignmentId: parsed.teachingAssignmentId ?? null,
    requestedOverloadHours: parsed.requestedOverloadHours,
    coordinatorJustification: parsed.coordinatorJustification,
    effectiveStartDate: parsed.effectiveStartDate ?? null,
    effectiveEndDate: parsed.effectiveEndDate ?? null,
    status: parsed.status ?? 'recommended',
  };

  submitException({ payload: requestPayload });
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    header="Request Workload Exception"
    class="w-full max-w-xl mx-4"
    data-testid="request-exception-modal"
  >
    <div class="space-y-4">
      <!-- 1. Teacher Information Banner -->
      <div
        v-if="commitment"
        class="p-3 bg-amber-50/70 border border-amber-200 rounded-xl flex items-start gap-3"
      >
        <div class="p-2 bg-amber-100 text-amber-700 rounded-lg shrink-0 mt-0.5">
          <ShieldAlert class="w-4 h-4" />
        </div>
        <div class="space-y-0.5 flex-1 min-w-0">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-amber-900 truncate">
              {{ commitment.fullName }}
            </span>
            <span class="text-xs font-mono bg-amber-100/80 text-amber-800 px-1.5 py-0.5 rounded">
              {{ commitment.teacherNumber }}
            </span>
          </div>
          <p class="text-xs text-amber-800">
            Current committed workload: <strong class="text-amber-950">{{ commitment.committedWorkloadHours }} hrs</strong> / max capacity {{ commitment.maxCapacityLimitHours || 24 }} hrs.
          </p>
        </div>
      </div>

      <!-- 2. Form Fields -->
      <form class="space-y-4" @submit.prevent="handleSubmit">
        <!-- Requested Overload Hours -->
        <FormField
          label="Requested Overload Effort (Weekly Hours)"
          required
          :error="formErrors.requestedOverloadHours"
          hint="Number of hours requested above the standard capacity threshold (1–80 hrs)."
        >
          <div class="relative">
            <Clock class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
            <InputNumber
              v-model="requestedHours"
              :min="1"
              :max="80"
              :step="0.5"
              mode="decimal"
              class="w-full"
              input-class="!pl-9"
              placeholder="e.g. 4"
              data-testid="overload-hours-input"
            />
          </div>
        </FormField>

        <!-- Dates Range -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Effective Start Date"
            required
            :error="formErrors.effectiveStartDate"
          >
            <div class="relative">
              <Calendar class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
              <InputText
                v-model="effectiveStartDate"
                type="date"
                class="w-full !pl-9"
                data-testid="exception-start-date"
              />
            </div>
          </FormField>

          <FormField
            label="Effective End Date"
            required
            :error="formErrors.effectiveEndDate"
          >
            <div class="relative">
              <Calendar class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
              <InputText
                v-model="effectiveEndDate"
                type="date"
                class="w-full !pl-9"
                data-testid="exception-end-date"
              />
            </div>
          </FormField>
        </div>

        <!-- Recommendation Status -->
        <FormField
          label="Submission Type"
          required
          :error="formErrors.status"
        >
          <Select
            v-model="recommendationStatus"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="exception-status-select"
          />
        </FormField>

        <!-- Coordinator Justification -->
        <FormField
          label="Coordinator Justification & Academic Rationale"
          required
          :error="formErrors.coordinatorJustification"
          hint="Detail the operational necessity, staffing constraints, or curriculum requirements."
        >
          <div class="relative">
            <Textarea
              v-model="coordinatorJustification"
              rows="4"
              class="w-full"
              placeholder="Provide clear justification for exceeding prohibited workload ceiling..."
              data-testid="coordinator-justification-input"
            />
          </div>
        </FormField>

        <!-- General Form Level Error -->
        <div
          v-if="formErrors.teacherProfileId || formErrors.academicPeriodId"
          class="p-2.5 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700 flex items-center gap-1.5"
        >
          <AlertCircle class="w-4 h-4 shrink-0" />
          <span>{{ formErrors.teacherProfileId || formErrors.academicPeriodId }}</span>
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
          :disabled="isSubmitting"
          @click="handleClose"
        />

        <Button
          type="button"
          label="Submit Exception Recommendation"
          severity="warn"
          size="small"
          :loading="isSubmitting"
          data-testid="submit-exception-btn"
          @click="handleSubmit"
        >
          <FilePlus2 class="w-4 h-4 mr-1.5" />
          <span>Submit Exception</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
