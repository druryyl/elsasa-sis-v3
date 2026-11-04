<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import InputNumber from 'primevue/inputnumber';
import {
  RefreshCw,
  AlertTriangle,
  FileText,
  DollarSign,
  ArrowRight,
  ShieldAlert,
} from 'lucide-vue-next';
import { FormField, LoadingSpinner } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { useReallocatePaymentMutation, useStudentReceivablesQuery } from '../../allocations/composables/useFinanceAllocations';
import { usePaymentByIdQuery } from '../composables/useFinancePayments';
import type { PaymentAllocationDto } from '../../allocations/types/allocations.types';
import type { PaymentAllocationSummaryDto, PaymentListItemDto, PaymentDto } from '../types/payments.types';

interface Props {
  visible: boolean;
  allocation?: (PaymentAllocationDto | PaymentAllocationSummaryDto) | null;
  payment?: PaymentListItemDto | PaymentDto | null;
  paymentId?: string | null;
  studentId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  allocation: null,
  payment: null,
  paymentId: null,
  studentId: null,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'reallocated'): void;
  (e: 'success', data?: any): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const resolvedPaymentId = computed(() => {
  return props.paymentId || props.payment?.id || (props.allocation as any)?.paymentId || null;
});

const resolvedStudentId = computed(() => {
  return props.studentId || props.payment?.studentId || props.payment?.applicantId || '';
});

// If no direct allocation was passed, load payment details to select from its active allocations
const paymentDetailQuery = usePaymentByIdQuery(resolvedPaymentId, {
  enabled: computed(() => Boolean(props.visible && !props.allocation && resolvedPaymentId.value)),
});

const activeAllocationsFromPayment = computed<PaymentAllocationSummaryDto[]>(() => {
  const all = paymentDetailQuery.data.value?.allocations ?? [];
  return all.filter((a) => a.status === 'active');
});

const selectedAllocationId = ref<string | null>(null);

watch(
  () => [props.visible, activeAllocationsFromPayment.value],
  ([isOpen]) => {
    if (isOpen && !props.allocation && activeAllocationsFromPayment.value.length > 0) {
      if (!selectedAllocationId.value) {
        selectedAllocationId.value = activeAllocationsFromPayment.value[0]?.id || null;
      }
    }
  },
  { immediate: true },
);

const effectiveAllocation = computed<(PaymentAllocationDto | PaymentAllocationSummaryDto) | null>(() => {
  if (props.allocation) return props.allocation;
  if (!selectedAllocationId.value) return activeAllocationsFromPayment.value[0] || null;
  return (
    activeAllocationsFromPayment.value.find((a) => a.id === selectedAllocationId.value) ||
    activeAllocationsFromPayment.value[0] ||
    null
  );
});

// Form state
const reversalReason = ref('');
const reallocationMode = ref<'credit' | 'replacement'>('credit');
const selectedTargetObligationId = ref<string | null>(null);
const replacementAmount = ref<number>(0);
const notes = ref('');

// Receivables for student to populate replacement dropdown
const { data: receivablesData, isLoading: isLoadingReceivables } = useStudentReceivablesQuery(
  resolvedStudentId,
  { openOnly: true },
  { enabled: computed(() => Boolean(props.visible && resolvedStudentId.value)) },
);

const availableReplacementObligations = computed(() => {
  const currentOblId = effectiveAllocation.value?.obligationId;
  const list = receivablesData.value?.openObligations || receivablesData.value?.receivables || [];
  return list.filter((o) => o.obligationId !== currentOblId);
});

// Watch allocation to set default replacement amount
watch(
  () => effectiveAllocation.value,
  (alloc) => {
    if (alloc) {
      reversalReason.value = '';
      reallocationMode.value = 'credit';
      selectedTargetObligationId.value = null;
      notes.value = '';
      replacementAmount.value = alloc.allocatedAmount || 0;
    }
  },
  { immediate: true },
);

const { mutateAsync: reallocateMutate, isPending: isSubmitting } =
  useReallocatePaymentMutation();

const isFormValid = computed(() => {
  if (!reversalReason.value.trim() || reversalReason.value.trim().length < 5) {
    return false;
  }
  if (!effectiveAllocation.value) {
    return false;
  }
  if (reallocationMode.value === 'replacement') {
    if (!selectedTargetObligationId.value) return false;
    if (!replacementAmount.value || replacementAmount.value <= 0) return false;
    if (replacementAmount.value > (effectiveAllocation.value?.allocatedAmount || 0)) return false;
  }
  return true;
});

async function handleSubmitReallocation() {
  if (!isFormValid.value || !effectiveAllocation.value) return;

  const paymentIdToUse = resolvedPaymentId.value;
  if (!paymentIdToUse) {
    return;
  }

  const priorAllocationId =
    effectiveAllocation.value.id || (effectiveAllocation.value as any).allocationId;

  const payload: any = {
    paymentId: paymentIdToUse,
    priorAllocationIds: [priorAllocationId],
    reversalReason: reversalReason.value.trim(),
    notes: notes.value.trim() || undefined,
  };

  if (reallocationMode.value === 'replacement' && selectedTargetObligationId.value) {
    payload.replacementAllocations = [
      {
        obligationId: selectedTargetObligationId.value,
        amount: replacementAmount.value,
        notes: `Reallocated from ${effectiveAllocation.value.allocationNumber}`,
      },
    ];
  }

  try {
    const res = await reallocateMutate(payload);
    emit('reallocated');
    emit('success', res);
    isDialogVisible.value = false;
  } catch {
    // Handled by toast notification
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-lg mx-4"
    data-testid="reallocate-payment-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 flex items-center justify-center">
          <RefreshCw class="w-4 h-4" />
        </div>
        <div>
          <span class="font-bold text-slate-900 text-base">Reallocate Payment Funds</span>
          <p class="text-xs text-slate-500">Correct allocation with append-only audit lineage (TD-05)</p>
        </div>
      </div>
    </template>

    <!-- Allocation Selector if opening from payment queue directly -->
    <div
      v-if="!allocation && activeAllocationsFromPayment.length > 1"
      class="mb-4"
    >
      <FormField label="Target Active Allocation" required>
        <Select
          v-model="selectedAllocationId"
          :options="activeAllocationsFromPayment"
          option-label="description"
          option-value="id"
          class="w-full text-xs"
          data-testid="allocation-picker-select"
        >
          <template #option="{ option }">
            <div class="text-xs">
              <span class="font-medium text-slate-900">{{ option.description }}</span>
              <span class="text-slate-500 ml-1">({{ formatIdr(option.allocatedAmount) }})</span>
            </div>
          </template>
        </Select>
      </FormField>
    </div>

    <div v-if="effectiveAllocation" class="space-y-4 text-sm">
      <!-- Target Allocation Dossier Card -->
      <div class="p-3.5 bg-slate-50 border border-slate-200 rounded-lg space-y-2">
        <div class="flex items-center justify-between text-xs">
          <span class="font-semibold text-slate-500 uppercase tracking-wider">Prior Allocation</span>
          <span class="font-mono font-bold text-slate-800 bg-white px-2 py-0.5 rounded border border-slate-200">
            {{ effectiveAllocation.allocationNumber }}
          </span>
        </div>
        <div class="flex items-center justify-between">
          <div class="text-slate-800 font-medium">
            {{ effectiveAllocation.description || 'Assessed Obligation' }}
          </div>
          <div class="font-mono font-bold text-slate-900 text-base">
            {{ formatIdr(effectiveAllocation.allocatedAmount) }}
          </div>
        </div>
        <div class="text-[11px] text-slate-500 font-mono">
          Obligation #: {{ effectiveAllocation.obligationNumber }}
        </div>
      </div>

      <!-- Warning Callout -->
      <div class="p-3 bg-amber-50 border border-amber-200 rounded-lg flex items-start gap-2 text-xs text-amber-800">
        <AlertTriangle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div>
          <strong>Reversal Audit Notice:</strong> This action will mark allocation
          <code>{{ effectiveAllocation.allocationNumber }}</code> as <em>superseded</em> or <em>reversed</em>.
          Historical lineage will be permanently preserved.
        </div>
      </div>

      <!-- Mandatory Reversal Reason Input -->
      <FormField label="Audit Reversal Reason" required error-message="Please provide a detailed reason (minimum 5 characters)">
        <Textarea
          v-model="reversalReason"
          rows="2"
          placeholder="e.g., Alokasi keliru pada SPP bulan berikutnya; harus dialihkan ke Uang Gedung."
          class="w-full text-sm"
          data-testid="reversal-reason-input"
        />
      </FormField>

      <!-- Reallocation Target Action Selector -->
      <FormField label="Reallocation Action">
        <div class="grid grid-cols-2 gap-2 text-xs">
          <button
            type="button"
            class="p-2.5 rounded-lg border text-left transition-colors cursor-pointer"
            :class="
              reallocationMode === 'credit'
                ? 'border-indigo-600 bg-indigo-50 text-indigo-900 font-semibold'
                : 'border-slate-200 hover:bg-slate-50 text-slate-700'
            "
            @click="reallocationMode = 'credit'"
          >
            <div class="font-bold">Hold as Credit</div>
            <div class="text-[11px] text-slate-500 mt-0.5">Release to unallocated balance</div>
          </button>
          <button
            type="button"
            class="p-2.5 rounded-lg border text-left transition-colors cursor-pointer"
            :class="
              reallocationMode === 'replacement'
                ? 'border-indigo-600 bg-indigo-50 text-indigo-900 font-semibold'
                : 'border-slate-200 hover:bg-slate-50 text-slate-700'
            "
            @click="reallocationMode = 'replacement'"
          >
            <div class="font-bold">Redirect to Other Fee</div>
            <div class="text-[11px] text-slate-500 mt-0.5">Apply to another open obligation</div>
          </button>
        </div>
      </FormField>

      <!-- Replacement Obligation Selector if Mode === replacement -->
      <div v-if="reallocationMode === 'replacement'" class="space-y-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
        <FormField label="Target Open Obligation" required>
          <Select
            v-model="selectedTargetObligationId"
            :options="availableReplacementObligations"
            option-label="description"
            option-value="obligationId"
            placeholder="Select replacement obligation..."
            class="w-full text-xs"
            data-testid="target-obligation-select"
          >
            <template #option="{ option }">
              <div class="py-1">
                <div class="font-medium text-slate-900 text-xs">{{ option.description }}</div>
                <div class="text-[11px] text-slate-500 font-mono">
                  {{ option.obligationNumber }} • Due: {{ formatIdr(option.outstandingAmount) }}
                </div>
              </div>
            </template>
          </Select>
        </FormField>

        <FormField label="Replacement Amount (IDR)" required>
          <InputNumber
            v-model="replacementAmount"
            mode="currency"
            currency="IDR"
            locale="id-ID"
            :min="1"
            :max="effectiveAllocation.allocatedAmount"
            class="w-full text-xs"
            data-testid="replacement-amount-input"
          />
          <p class="text-[11px] text-slate-500 mt-0.5">
            Maximum available: {{ formatIdr(effectiveAllocation.allocatedAmount) }}
          </p>
        </FormField>
      </div>

      <!-- Optional Notes -->
      <FormField label="Additional Notes (Optional)">
        <Textarea
          v-model="notes"
          rows="1"
          placeholder="Optional notes for cashier record..."
          class="w-full text-xs"
          data-testid="reallocate-notes-input"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="isSubmitting"
          @click="isDialogVisible = false"
        />
        <Button
          type="button"
          label="Confirm Reallocation"
          severity="primary"
          size="small"
          :loading="isSubmitting"
          :disabled="!isFormValid"
          data-testid="confirm-reallocate-btn"
          @click="handleSubmitReallocation"
        />
      </div>
    </template>
  </Dialog>
</template>
