<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import {
  Coins,
  Wand2,
  CheckCircle2,
  AlertCircle,
  HelpCircle,
  Calendar,
  Layers,
} from 'lucide-vue-next';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import {
  useStudentReceivablesQuery,
  useAllocatePaymentMutation,
} from '../../allocations/composables/useFinanceAllocations';
import type {
  PaymentListItemDto,
  PaymentDto,
} from '../types/payments.types';
import type {
  AllocatePaymentResponse,
  AllocationItemPayload,
  StudentReceivableDto,
} from '../../allocations/types/allocations.types';

const props = defineProps<{
  visible: boolean;
  payment: PaymentListItemDto | PaymentDto | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success', result: AllocatePaymentResponse): void;
  (e: 'close'): void;
}>();

const targetCandidateId = computed(() => {
  if (!props.payment) return null;
  return props.payment.studentId || props.payment.applicantId || null;
});

// Query open obligations for candidate
const receivablesQuery = useStudentReceivablesQuery(targetCandidateId, {
  enabled: computed(() => props.visible && !!targetCandidateId.value),
});

const openObligations = computed<StudentReceivableDto[]>(() => {
  const all = receivablesQuery.data.value?.receivables ?? [];
  return all.filter((o) => o.outstandingAmount > 0);
});

// Map of obligationId -> line allocation amount
const lineAllocations = ref<Record<string, number>>({});
const notes = ref<string>('');
const error = ref<string>('');

const availableFunds = computed<number>(() => {
  if (!props.payment) return 0;
  // If unallocatedAmount is provided, use it, else calculate gross - totalAllocated
  if (typeof props.payment.unallocatedAmount === 'number') {
    return props.payment.unallocatedAmount;
  }
  const gross = props.payment.paymentAmount || 0;
  const allocated = props.payment.totalAllocated || 0;
  return Math.max(0, gross - allocated);
});

const totalAllocating = computed<number>(() => {
  return Object.values(lineAllocations.value).reduce((sum, val) => sum + (Number(val) || 0), 0);
});

const remainingUnallocated = computed<number>(() => {
  return availableFunds.value - totalAllocating.value;
});

const isValidAllocation = computed<boolean>(() => {
  if (totalAllocating.value <= 0) return false;
  if (totalAllocating.value > availableFunds.value) return false;

  for (const [id, amt] of Object.entries(lineAllocations.value)) {
    if (amt > 0) {
      const ob = openObligations.value.find((o) => o.obligationId === id);
      if (ob && amt > ob.outstandingAmount) {
        return false;
      }
    }
  }

  return true;
});

watch(
  () => [props.visible, openObligations.value],
  ([isOpen]) => {
    if (isOpen) {
      lineAllocations.value = {};
      notes.value = '';
      error.value = '';
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 1-Click Smart Auto-Suggest Action
// ---------------------------------------------------------------------------
function handleAutoSuggest() {
  error.value = '';
  lineAllocations.value = {};

  let fundsLeft = availableFunds.value;
  if (fundsLeft <= 0) {
    error.value = 'No unallocated funds available to distribute.';
    return;
  }

  // Sort according to policy:
  // 1. Mandatory enrollment / admission fees
  // 2. Mandatory obligations FIFO by due date
  // 3. Non-mandatory / other fees FIFO by due date
  const sorted = [...openObligations.value].sort((a, b) => {
    const isEnrA =
      (a.chargeType === 'enrollment_fee' || a.chargeType === 'admission_fee') && a.isMandatory;
    const isEnrB =
      (b.chargeType === 'enrollment_fee' || b.chargeType === 'admission_fee') && b.isMandatory;

    if (isEnrA && !isEnrB) return -1;
    if (!isEnrA && isEnrB) return 1;

    if (a.isMandatory && !b.isMandatory) return -1;
    if (!a.isMandatory && b.isMandatory) return 1;

    return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
  });

  const nextAllocations: Record<string, number> = {};

  for (const item of sorted) {
    if (fundsLeft <= 0) break;
    const allocate = Math.min(fundsLeft, item.outstandingAmount);
    if (allocate > 0) {
      nextAllocations[item.obligationId] = allocate;
      fundsLeft -= allocate;
    }
  }

  lineAllocations.value = nextAllocations;
}

function setMaxAllocation(item: StudentReceivableDto) {
  const currentAssigned = lineAllocations.value[item.obligationId] || 0;
  const fundsAvailableForThisLine = remainingUnallocated.value + currentAssigned;
  const maxCanAllocate = Math.min(fundsAvailableForThisLine, item.outstandingAmount);
  lineAllocations.value[item.obligationId] = maxCanAllocate;
}

// ---------------------------------------------------------------------------
// Commit Allocation Mutation
// ---------------------------------------------------------------------------
const allocatePaymentMutation = useAllocatePaymentMutation({
  onSuccess: (data) => {
    emit('success', data);
    emit('update:visible', false);
  },
});

const isSubmitting = computed(() => allocatePaymentMutation.isPending.value);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleCommit() {
  error.value = '';

  if (!props.payment?.id) return;

  if (totalAllocating.value <= 0) {
    error.value = 'Please allocate an amount to at least one obligation.';
    return;
  }

  if (totalAllocating.value > availableFunds.value) {
    error.value = `Total allocated (${formatIDR(totalAllocating.value)}) exceeds available payment funds (${formatIDR(availableFunds.value)}).`;
    return;
  }

  const items: AllocationItemPayload[] = [];
  for (const [obligationId, amt] of Object.entries(lineAllocations.value)) {
    if (amt > 0) {
      const ob = openObligations.value.find((o) => o.obligationId === obligationId);
      if (ob && amt > ob.outstandingAmount) {
        error.value = `Allocation of ${formatIDR(amt)} exceeds obligation ${ob.obligationNumber} balance of ${formatIDR(ob.outstandingAmount)}.`;
        return;
      }
      items.push({
        obligationId,
        amount: Math.round(amt),
        notes: null,
      });
    }
  }

  allocatePaymentMutation.mutate({
    paymentId: props.payment.id,
    allocations: items,
    notes: notes.value.trim() || null,
  });
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Allocate Payment Funds"
    :style="{ width: '56rem' }"
    :closable="!isSubmitting"
    data-testid="allocate-payment-modal"
    @update:visible="handleClose"
  >
    <div v-if="payment" class="space-y-4 text-sm text-slate-700">
      <!-- Header Metrics Strip -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div class="rounded-lg border border-slate-200 bg-slate-50/70 p-3">
          <span class="text-xs text-slate-500 font-medium">Payment Gross</span>
          <p class="font-mono text-base font-bold text-slate-900 mt-0.5">
            {{ formatIDR(payment.paymentAmount) }}
          </p>
          <span class="text-[11px] font-mono text-slate-500">
            {{ payment.receiptNumber || payment.paymentNumber }}
          </span>
        </div>

        <div class="rounded-lg border border-indigo-100 bg-indigo-50/50 p-3">
          <span class="text-xs text-indigo-700 font-medium">Currently Allocating</span>
          <p class="font-mono text-base font-bold text-indigo-900 mt-0.5">
            {{ formatIDR(totalAllocating) }}
          </p>
          <span class="text-[11px] text-indigo-600">
            {{ Object.values(lineAllocations).filter((v) => v > 0).length }} line(s) selected
          </span>
        </div>

        <div
          class="rounded-lg border p-3 transition-colors"
          :class="
            remainingUnallocated < 0
              ? 'border-rose-200 bg-rose-50 text-rose-900'
              : 'border-emerald-200 bg-emerald-50 text-emerald-900'
          "
        >
          <span class="text-xs font-medium">Remaining Available</span>
          <p class="font-mono text-base font-bold mt-0.5">
            {{ formatIDR(remainingUnallocated) }}
          </p>
          <span class="text-[11px]">
            {{
              remainingUnallocated < 0
                ? 'Overallocated by ' + formatIDR(Math.abs(remainingUnallocated))
                : 'Will remain unallocated credit'
            }}
          </span>
        </div>
      </div>

      <!-- Action & Auto-Suggest Bar -->
      <div class="flex items-center justify-between bg-white border border-slate-200 rounded-lg p-3">
        <div>
          <h4 class="text-xs font-semibold uppercase tracking-wider text-slate-700 flex items-center gap-1.5">
            <Layers class="w-3.5 h-3.5 text-slate-500" /> Open Obligations &amp; Fee Liabilities
          </h4>
          <p class="text-[11px] text-slate-500 mt-0.5">
            Auto-suggest prioritizes mandatory enrollment fees first, then oldest tuition FIFO.
          </p>
        </div>
        <Button
          severity="help"
          size="small"
          variant="outlined"
          :disabled="isSubmitting || openObligations.length === 0 || availableFunds <= 0"
          data-testid="auto-suggest-button"
          @click="handleAutoSuggest"
        >
          <Wand2 class="w-3.5 h-3.5 mr-1.5" />
          <span>Auto-Suggest</span>
        </Button>
      </div>

      <!-- Error Alert -->
      <div
        v-if="error"
        class="rounded-lg border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800 flex items-center gap-2"
        data-testid="allocation-error-alert"
      >
        <AlertCircle class="w-4 h-4 text-rose-600 shrink-0" />
        <span>{{ error }}</span>
      </div>

      <!-- Open Obligations Table -->
      <div class="border border-slate-200 rounded-lg overflow-hidden">
        <DataTable
          :value="openObligations"
          :loading="receivablesQuery.isPending.value"
          size="small"
          class="text-xs"
          data-testid="open-obligations-table"
        >
          <template #empty>
            <div class="py-8 text-center text-slate-500">
              <p class="font-medium">No open obligations found for this candidate.</p>
              <p class="text-[11px] mt-1 text-slate-400">
                Payment funds will remain dynamically stored as unallocated credit balance.
              </p>
            </div>
          </template>

          <Column field="obligationNumber" header="Obligation #" style="width: 15%">
            <template #body="{ data }">
              <span class="font-mono font-medium text-slate-800">{{ data.obligationNumber }}</span>
              <span
                v-if="data.isMandatory"
                class="ml-1 text-[10px] uppercase font-semibold text-amber-700 bg-amber-50 px-1 py-0.5 rounded border border-amber-200"
              >
                Mandatory
              </span>
            </template>
          </Column>

          <Column field="description" header="Description / Charge" style="width: 25%">
            <template #body="{ data }">
              <p class="font-medium text-slate-800 truncate">{{ data.description }}</p>
              <span class="text-[10px] text-slate-500 capitalize">{{ data.chargeType.replace(/_/g, ' ') }}</span>
            </template>
          </Column>

          <Column field="dueDate" header="Due Date" style="width: 15%">
            <template #body="{ data }">
              <span class="text-slate-600">{{ formatDisplayDate(data.dueDate) }}</span>
            </template>
          </Column>

          <Column field="outstandingAmount" header="Outstanding" style="width: 20%">
            <template #body="{ data }">
              <span class="font-mono font-semibold text-rose-700">{{ formatIDR(data.outstandingAmount) }}</span>
            </template>
          </Column>

          <Column header="Allocate (IDR)" style="width: 25%">
            <template #body="{ data }">
              <div class="flex items-center gap-1.5">
                <InputText
                  v-model.number="lineAllocations[data.obligationId]"
                  type="number"
                  min="0"
                  :max="data.outstandingAmount"
                  step="1000"
                  class="w-28 font-mono text-xs py-1"
                  placeholder="0"
                  :invalid="
                    (lineAllocations[data.obligationId] || 0) > data.outstandingAmount
                  "
                  :data-testid="`line-allocation-input-${data.obligationId}`"
                />
                <Button
                  label="Max"
                  size="small"
                  severity="secondary"
                  variant="text"
                  class="text-[11px] px-1.5 py-0.5"
                  :data-testid="`line-max-button-${data.obligationId}`"
                  @click="setMaxAllocation(data)"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-between pt-3">
        <span class="text-xs text-slate-500">
          Unallocated funds remain attached to the account as available credit.
        </span>
        <div class="flex items-center gap-2">
          <Button
            label="Cancel"
            severity="secondary"
            variant="outlined"
            :disabled="isSubmitting"
            data-testid="cancel-allocation-button"
            @click="handleClose"
          />
          <Button
            :loading="isSubmitting"
            :disabled="!isValidAllocation"
            severity="primary"
            data-testid="commit-allocations-button"
            @click="handleCommit"
          >
            <CheckCircle2 class="w-4 h-4 mr-1.5" />
            <span>Commit Allocations</span>
          </Button>
        </div>
      </div>
    </template>
  </Dialog>
</template>
