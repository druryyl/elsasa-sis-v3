<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { Gavel } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import {
  recordDecisionSchema,
  type RecordDecisionFormData,
} from '../../schemas/admission.schema';
import { useRecordDecisionMutation } from '../../composables/useAdmissionMutations';
import type {
  AdmissionPeriodOptionDto,
  RecordAdmissionDecisionResultDto,
  DecisionType,
} from '../../types/admission.types';

const props = defineProps<{
  visible: boolean;
  applicationId: string;
  periods?: AdmissionPeriodOptionDto[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved', result: RecordAdmissionDecisionResultDto): void;
}>();

const decisionTypeOptions: { label: string; value: DecisionType }[] = [
  { label: 'Accepted (Lolos / Diterima)', value: 'accepted' },
  { label: 'Rejected (Tidak Diterima)', value: 'rejected' },
  { label: 'Waitlisted (Daftar Tunggu)', value: 'waitlisted' },
  { label: 'Deferred (Ditunda ke Periode Berikutnya)', value: 'deferred' },
];

const periodOptions = computed(() =>
  (props.periods ?? []).map((p) => ({
    id: p.id,
    name: `${p.name} (${p.periodCode})`,
  })),
);

const form = ref<RecordDecisionFormData>({
  decisionType: 'accepted',
  decisionDate: formatDateOnly(new Date()),
  effectiveDate: formatDateOnly(new Date()),
  reason: '',
  supportingReference: '',
  deferredAdmissionPeriodId: '',
});

const errors = ref<Record<string, string>>({});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      errors.value = {};
      form.value = {
        decisionType: 'accepted',
        decisionDate: formatDateOnly(new Date()),
        effectiveDate: formatDateOnly(new Date()),
        reason: '',
        supportingReference: '',
        deferredAdmissionPeriodId: '',
      };
    }
  },
  { immediate: true },
);

const recordDecisionMutation = useRecordDecisionMutation({
  onSuccess: (data) => {
    emit('saved', data);
    emit('update:visible', false);
  },
});

function handleSubmit() {
  errors.value = {};
  const result = recordDecisionSchema.safeParse(form.value);
  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] as string;
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return;
  }

  recordDecisionMutation.mutate({
    id: props.applicationId,
    payload: {
      decisionType: result.data.decisionType,
      decisionDate: result.data.decisionDate,
      effectiveDate: result.data.effectiveDate,
      reason: result.data.reason?.trim() || null,
      supportingReference: result.data.supportingReference?.trim() || null,
      deferredAdmissionPeriodId:
        result.data.decisionType === 'deferred'
          ? result.data.deferredAdmissionPeriodId?.trim() || null
          : null,
    },
  });
}

function handleClose() {
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Record Official Admission Decision"
    class="max-w-md w-full"
    data-testid="record-decision-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 pt-1 text-xs" @submit.prevent="handleSubmit">
      <div class="flex items-start gap-2.5 p-3 rounded-lg bg-teal-50 border border-teal-200 text-teal-900">
        <Gavel class="w-4 h-4 shrink-0 text-teal-700 mt-0.5" />
        <p>
          Recording an admission decision transitions the application to <strong>Decided</strong>. This official action becomes permanent and immutable.
        </p>
      </div>

      <!-- Decision Outcome -->
      <FormField label="Decision Outcome" required :error="errors.decisionType">
        <select
          v-model="form.decisionType"
          class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800"
          data-testid="decision-outcome-select"
        >
          <option
            v-for="opt in decisionTypeOptions"
            :key="opt.value"
            :value="opt.value"
          >
            {{ opt.label }}
          </option>
        </select>
      </FormField>

      <!-- Effective Date -->
      <FormField label="Effective Date" required :error="errors.effectiveDate">
        <InputText
          v-model="form.effectiveDate"
          type="date"
          class="w-full !text-xs !py-1.5"
          data-testid="decision-effective-date-input"
        />
      </FormField>

      <!-- Deferred Period Dropdown (Conditional) -->
      <FormField
        v-if="form.decisionType === 'deferred'"
        label="Deferred Target Period"
        required
        :error="errors.deferredAdmissionPeriodId"
      >
        <Select
          v-model="form.deferredAdmissionPeriodId"
          :options="periodOptions"
          option-label="name"
          option-value="id"
          placeholder="Select Deferred Period"
          class="w-full !text-xs"
          data-testid="decision-deferred-period-select"
        />
      </FormField>

      <!-- Reason / Justification -->
      <FormField label="Reason / Justification Notes" :error="errors.reason">
        <Textarea
          v-model="form.reason"
          rows="3"
          placeholder="Document committee rationale or specific admission considerations..."
          class="w-full !text-xs"
          data-testid="decision-reason-textarea"
        />
      </FormField>

      <!-- Supporting Reference Code -->
      <FormField label="Supporting Reference Code" :error="errors.supportingReference">
        <InputText
          v-model="form.supportingReference"
          type="text"
          placeholder="e.g. SK-PPDB-2026-001 or Committee Minutes #42"
          class="w-full !text-xs !py-1.5 font-mono"
          data-testid="decision-reference-input"
        />
      </FormField>

      <!-- Dialog Footer Actions -->
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
          type="submit"
          size="small"
          severity="success"
          :loading="recordDecisionMutation.isPending.value"
          class="!text-xs cursor-pointer"
          data-testid="confirm-decision-button"
        >
          Confirm Decision
        </Button>
      </div>
    </form>
  </Dialog>
</template>
