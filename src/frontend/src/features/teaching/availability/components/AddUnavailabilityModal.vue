<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  Calendar,
  Clock,
  HeartPulse,
  GraduationCap,
  User,
  Ban,
  FileText,
  AlertCircle,
  ShieldAlert,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  createUnavailabilityWindowSchema,
  UNAVAILABILITY_TYPES,
  type CreateUnavailabilityWindowFormValues,
} from '../api/schemas';
import { useCreateUnavailabilityMutation } from '../composables/useAvailability';
import type {
  CreateUnavailabilityWindowRequest,
  CreateUnavailabilityWindowResponse,
  UnavailabilityType,
} from '../types';

import {
  LEAVE_TYPE_OPTIONS,
  type LeaveTypeOption,
} from './addUnavailabilityConstants';

export interface AddUnavailabilityModalProps {
  visible: boolean;
  teacherId?: string;
  appendTo?: string;
}

const props = withDefaults(defineProps<AddUnavailabilityModalProps>(), {
  visible: false,
  teacherId: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', response: CreateUnavailabilityWindowResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

function getTodayIsoDate(): string {
  const d = new Date();
  return d.toISOString().split('T')[0];
}

// ---------------------------------------------------------------------------
// Form State & Validation
// ---------------------------------------------------------------------------
const unavailabilityType = ref<UnavailabilityType>('medical_leave');
const startDate = ref<string>(getTodayIsoDate());
const endDate = ref<string>(getTodayIsoDate());
const startTime = ref<string>('');
const endTime = ref<string>('');
const isPartialDay = ref<boolean>(false);
const reason = ref<string>('');
const externalSourceReference = ref<string>('');
const emergencyCoverageRequired = ref<boolean>(false);
const notes = ref<string>('');

const errors = ref<Record<string, string>>({});

function resetForm() {
  unavailabilityType.value = 'medical_leave';
  const today = getTodayIsoDate();
  startDate.value = today;
  endDate.value = today;
  startTime.value = '';
  endTime.value = '';
  isPartialDay.value = false;
  reason.value = '';
  externalSourceReference.value = '';
  emergencyCoverageRequired.value = false;
  notes.value = '';
  errors.value = {};
}

watch(
  () => props.visible,
  (val) => {
    if (val) {
      resetForm();
    }
  },
);

watch(isPartialDay, (enabled) => {
  if (!enabled) {
    startTime.value = '';
    endTime.value = '';
  } else {
    if (!startTime.value) startTime.value = '08:00';
    if (!endTime.value) endTime.value = '17:00';
  }
});

// Auto-align endDate when startDate moves past endDate
watch(startDate, (newStart) => {
  if (newStart && endDate.value && endDate.value < newStart) {
    endDate.value = newStart;
  }
});

// ---------------------------------------------------------------------------
// Mutation
// ---------------------------------------------------------------------------
const createMutation = useCreateUnavailabilityMutation({
  onSuccess: (data) => {
    emit('created', data);
    isDialogVisible.value = false;
    emit('close');
    resetForm();
  },
});

function validateForm(): boolean {
  errors.value = {};

  const payloadToValidate = {
    startDate: startDate.value,
    endDate: endDate.value,
    unavailabilityType: unavailabilityType.value,
    reason: reason.value,
    startTime: isPartialDay.value && startTime.value ? startTime.value : undefined,
    endTime: isPartialDay.value && endTime.value ? endTime.value : undefined,
    externalSourceReference: externalSourceReference.value || undefined,
    emergencyCoverageRequired: emergencyCoverageRequired.value,
    notes: notes.value || undefined,
  };

  const result = createUnavailabilityWindowSchema.safeParse(payloadToValidate);

  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] ? String(issue.path[0]) : 'general';
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return false;
  }

  return true;
}

function handleSubmit() {
  if (!validateForm()) return;

  if (!props.teacherId) {
    errors.value.general = 'Teacher Profile ID is required to record unavailability.';
    return;
  }

  const payload: CreateUnavailabilityWindowRequest = {
    startDate: startDate.value,
    endDate: endDate.value,
    unavailabilityType: unavailabilityType.value,
    reason: reason.value.trim(),
    startTime: isPartialDay.value && startTime.value ? startTime.value : undefined,
    endTime: isPartialDay.value && endTime.value ? endTime.value : undefined,
    externalSourceReference: externalSourceReference.value.trim() || undefined,
    emergencyCoverageRequired: emergencyCoverageRequired.value,
    notes: notes.value.trim() || undefined,
  };

  createMutation.mutate({
    teacherId: props.teacherId,
    payload,
  });
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
    header="Record Temporary Unavailability / Leave"
    :style="{ width: '560px', maxWidth: '95vw' }"
    :append-to="appendTo"
    data-testid="add-unavailability-modal"
  >
    <form class="space-y-4 pt-2" @submit.prevent="handleSubmit">
      <!-- General Error Alert -->
      <div
        v-if="errors.general"
        class="p-3 text-xs rounded-lg bg-rose-50 text-rose-700 border border-rose-200 flex items-start gap-2"
        data-testid="add-unavailability-general-error"
      >
        <AlertCircle class="w-4 h-4 flex-shrink-0 mt-0.5" />
        <span>{{ errors.general }}</span>
      </div>

      <!-- Leave Type Selector -->
      <FormField
        label="Unavailability Type"
        required
        :error="errors.unavailabilityType"
      >
        <Select
          v-model="unavailabilityType"
          :options="LEAVE_TYPE_OPTIONS"
          option-label="label"
          option-value="value"
          placeholder="Select leave type"
          class="w-full text-sm"
          data-testid="leave-type-select"
        >
          <template #option="{ option }">
            <div class="py-1">
              <div class="text-sm font-medium text-slate-800 dark:text-slate-100 flex items-center gap-2">
                <HeartPulse v-if="option.value === 'medical_leave'" class="w-4 h-4 text-rose-500" />
                <GraduationCap v-else-if="option.value === 'official_training'" class="w-4 h-4 text-sky-500" />
                <User v-else-if="option.value === 'personal_leave'" class="w-4 h-4 text-amber-500" />
                <Ban v-else class="w-4 h-4 text-purple-500" />
                <span>{{ option.label }}</span>
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {{ option.description }}
              </div>
            </div>
          </template>
        </Select>
      </FormField>

      <!-- Date Range (Start Date & End Date) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          label="Start Date"
          required
          :error="errors.startDate"
        >
          <InputText
            v-model="startDate"
            type="date"
            class="w-full text-sm"
            data-testid="unavailability-start-date-input"
          />
        </FormField>

        <FormField
          label="End Date"
          required
          :error="errors.endDate"
        >
          <InputText
            v-model="endDate"
            type="date"
            class="w-full text-sm"
            data-testid="unavailability-end-date-input"
          />
        </FormField>
      </div>

      <!-- Partial Day Time Block Toggle & Inputs -->
      <div class="space-y-2 rounded-lg p-3 bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-700">
        <div class="flex items-center gap-2">
          <Checkbox
            v-model="isPartialDay"
            :binary="true"
            input-id="partial-day-checkbox"
            data-testid="partial-day-toggle"
          />
          <label for="partial-day-checkbox" class="text-xs font-medium text-slate-700 dark:text-slate-300 cursor-pointer select-none">
            Partial day absence (specify hours)
          </label>
        </div>

        <div v-if="isPartialDay" class="grid grid-cols-2 gap-3 pt-2">
          <FormField
            label="Start Time (HH:mm)"
            :error="errors.startTime"
          >
            <InputText
              v-model="startTime"
              type="time"
              class="w-full text-sm"
              placeholder="08:00"
              data-testid="unavailability-start-time-input"
            />
          </FormField>
          <FormField
            label="End Time (HH:mm)"
            :error="errors.endTime"
          >
            <InputText
              v-model="endTime"
              type="time"
              class="w-full text-sm"
              placeholder="17:00"
              data-testid="unavailability-end-time-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Reason / Justification -->
      <FormField
        label="Reason / Formal Justification"
        required
        :error="errors.reason"
      >
        <Textarea
          v-model="reason"
          rows="3"
          placeholder="State the reason or medical diagnosis / institutional purpose..."
          class="w-full text-sm"
          data-testid="unavailability-reason-input"
        />
      </FormField>

      <!-- External Document / Reference Number -->
      <FormField
        label="External Reference / Document ID (Optional)"
        :error="errors.externalSourceReference"
      >
        <InputText
          v-model="externalSourceReference"
          placeholder="e.g. Surat Sakit #MED-2026-081 or Surat Tugas #ST-044"
          class="w-full text-sm"
          data-testid="unavailability-external-ref-input"
        />
      </FormField>

      <!-- Emergency Substitute Coverage Required Checkbox -->
      <div class="flex items-start gap-2.5 p-3 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800">
        <Checkbox
          v-model="emergencyCoverageRequired"
          :binary="true"
          input-id="emergency-coverage-checkbox"
          data-testid="emergency-coverage-toggle"
          class="mt-0.5"
        />
        <div>
          <label for="emergency-coverage-checkbox" class="text-xs font-semibold text-amber-900 dark:text-amber-200 cursor-pointer select-none">
            Flag Emergency Substitute Coverage Required
          </label>
          <p class="text-[11px] text-amber-700 dark:text-amber-300">
            Alerts Teaching Coordinators to arrange emergency coverage from the pre-authorized pool during this window.
          </p>
        </div>
      </div>

      <!-- Internal Remarks / Notes -->
      <FormField
        label="Coordinator / Faculty Notes (Optional)"
        :error="errors.notes"
      >
        <Textarea
          v-model="notes"
          rows="2"
          placeholder="Additional administrative context or handover notes..."
          class="w-full text-sm"
          data-testid="unavailability-notes-input"
        />
      </FormField>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-200 dark:border-slate-700">
        <Button
          type="button"
          severity="secondary"
          variant="text"
          label="Cancel"
          size="small"
          class="text-xs"
          data-testid="add-unavailability-cancel-btn"
          @click="handleClose"
        />
        <Button
          type="button"
          label="Save Leave Window"
          size="small"
          class="text-xs"
          :loading="createMutation.isPending.value"
          data-testid="add-unavailability-submit-btn"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
