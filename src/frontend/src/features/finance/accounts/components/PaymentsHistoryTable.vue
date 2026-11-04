<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  CreditCard,
  Receipt,
  Search,
  Calendar,
  User,
  CheckCircle2,
  AlertCircle,
  FileText,
  Clock,
  ArrowUpRight,
} from 'lucide-vue-next';
import { StatusPill, EmptyState } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import { getPaymentStatusDescriptor } from '../../shared/financeStatus.utils';
import type { PaymentListItemDto } from '../../payments/types/payments.types';
import ReceiptVoucherViewer from '../../payments/components/ReceiptVoucherViewer.vue';

interface Props {
  payments: PaymentListItemDto[];
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  payments: () => [],
  loading: false,
});

const emit = defineEmits<{
  (e: 'viewReceipt', paymentId: string): void;
}>();

const searchFilter = ref('');
const statusFilter = ref<'all' | 'confirmed' | 'submitted' | 'rejected'>('all');

// Receipt modal state
const isReceiptModalOpen = ref(false);
const selectedReceiptPaymentId = ref<string | null>(null);

const filteredPayments = computed(() => {
  return props.payments.filter((payment) => {
    // Status filter
    if (statusFilter.value !== 'all') {
      if (payment.confirmationStatus?.toLowerCase() !== statusFilter.value) {
        return false;
      }
    }

    // Search filter
    if (!searchFilter.value.trim()) {
      return true;
    }
    const term = searchFilter.value.trim().toLowerCase();
    return (
      (payment.paymentNumber && payment.paymentNumber.toLowerCase().includes(term)) ||
      (payment.receiptNumber && payment.receiptNumber.toLowerCase().includes(term)) ||
      (payment.payerName && payment.payerName.toLowerCase().includes(term)) ||
      (payment.paymentMethod && payment.paymentMethod.toLowerCase().includes(term)) ||
      (payment.externalReference && payment.externalReference.toLowerCase().includes(term))
    );
  });
});

function openReceipt(paymentId: string) {
  selectedReceiptPaymentId.value = paymentId;
  isReceiptModalOpen.value = true;
  emit('viewReceipt', paymentId);
}

function formatPaymentMethod(method?: string | null): string {
  if (!method) return '—';
  switch (method.toLowerCase()) {
    case 'bank_transfer':
      return 'Bank Transfer';
    case 'cash':
      return 'Cash';
    case 'pos_card':
      return 'POS / Card';
    case 'qris':
      return 'QRIS';
    case 'virtual_account':
      return 'Virtual Account';
    default:
      return method.replace('_', ' ').toUpperCase();
  }
}
</script>

<template>
  <div
    class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden"
    data-testid="payments-history-table-container"
  >
    <!-- Header & Controls -->
    <div class="px-6 py-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-2">
        <CreditCard class="w-5 h-5 text-indigo-600" />
        <h3 class="text-base font-semibold text-slate-800">
          Payment Transactions &amp; Receipts
        </h3>
        <span
          class="ml-1 text-xs font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600"
          data-testid="payments-count-badge"
        >
          {{ payments.length }} recorded
        </span>
      </div>

      <div class="flex items-center gap-3 flex-wrap">
        <!-- Quick search -->
        <div class="relative w-48 sm:w-60">
          <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
          <InputText
            v-model="searchFilter"
            placeholder="Search payments, receipts..."
            class="w-full pl-9! text-xs"
            size="small"
            data-testid="payments-search-input"
          />
        </div>

        <!-- Status Filter Pills -->
        <div class="inline-flex rounded-lg border border-slate-200 p-0.5 bg-slate-50 text-xs">
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'all' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'all'"
          >
            All
          </button>
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'confirmed' ? 'bg-white text-emerald-800 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'confirmed'"
          >
            Confirmed
          </button>
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'submitted' ? 'bg-white text-amber-800 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'submitted'"
          >
            Submitted
          </button>
        </div>
      </div>
    </div>

    <!-- DataTable -->
    <DataTable
      :value="filteredPayments"
      :loading="loading"
      data-key="id"
      responsive-layout="scroll"
      striped-rows
      class="text-sm"
      data-testid="payments-history-datatable"
    >
      <template #empty>
        <div class="p-8">
          <EmptyState
            title="No Payment Transactions"
            message="No payments matching your criteria have been recorded for this student account."
            data-testid="payments-empty-state"
          />
        </div>
      </template>

      <!-- 1. Payment Date -->
      <Column header="Payment Date" style="min-width: 130px">
        <template #body="{ data }">
          <div class="font-medium text-slate-800 flex items-center gap-1.5">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            {{ formatDateOnly(data.paymentDate) }}
          </div>
          <div class="text-[11px] text-slate-500 font-mono">
            Submitted: {{ formatDateOnly(data.submissionDate) }}
          </div>
        </template>
      </Column>

      <!-- 2. Receipt / Payment Number -->
      <Column header="Receipt / Payment #" style="min-width: 170px">
        <template #body="{ data }">
          <div v-if="data.receiptNumber" class="space-y-0.5">
            <div
              class="font-mono text-xs font-bold text-emerald-800 bg-emerald-50 px-2 py-0.5 rounded border border-emerald-200 inline-flex items-center gap-1"
              data-testid="receipt-number-cell"
            >
              <Receipt class="w-3 h-3 text-emerald-600" />
              {{ data.receiptNumber }}
            </div>
            <div class="text-[10px] text-slate-500 font-mono">
              Intake: {{ data.paymentNumber }}
            </div>
          </div>
          <div v-else class="space-y-0.5">
            <span
              class="font-mono text-xs text-slate-700 bg-slate-50 px-2 py-0.5 rounded border border-slate-200 inline-block"
              data-testid="payment-number-cell"
            >
              {{ data.paymentNumber }}
            </span>
            <div class="text-[10px] text-amber-600 font-medium">
              (Pending Receipt)
            </div>
          </div>
        </template>
      </Column>

      <!-- 3. Payer -->
      <Column header="Payer" style="min-width: 160px">
        <template #body="{ data }">
          <div class="font-semibold text-slate-900 flex items-center gap-1">
            <User class="w-3.5 h-3.5 text-slate-400" />
            {{ data.payerName }}
          </div>
          <div class="text-xs text-slate-500 uppercase tracking-wider text-[10px]">
            {{ data.payerNumber }} • {{ data.payerType || 'Payer' }}
          </div>
        </template>
      </Column>

      <!-- 4. Method -->
      <Column header="Method" style="min-width: 140px">
        <template #body="{ data }">
          <div class="text-xs font-medium text-slate-800">
            {{ formatPaymentMethod(data.paymentMethod) }}
          </div>
          <div v-if="data.externalReference" class="text-[11px] text-slate-500 font-mono truncate max-w-[140px]" :title="data.externalReference">
            Ref: {{ data.externalReference }}
          </div>
        </template>
      </Column>

      <!-- 5. Amount (IDR) -->
      <Column header="Amount (IDR)" style="min-width: 140px" class="text-right">
        <template #body="{ data }">
          <div class="font-mono font-bold text-slate-900 text-sm">
            {{ formatIdr(data.paymentAmount) }}
          </div>
          <div
            v-if="data.unallocatedAmount > 0"
            class="text-[10px] font-mono text-blue-700 bg-blue-50 px-1.5 py-0.2 rounded border border-blue-200 mt-0.5 inline-block"
          >
            Credit: {{ formatIdr(data.unallocatedAmount) }}
          </div>
        </template>
      </Column>

      <!-- 6. Confirmation Status -->
      <Column header="Status" style="min-width: 110px">
        <template #body="{ data }">
          <StatusPill
            :status="data.confirmationStatus"
            :label="getPaymentStatusDescriptor(data.confirmationStatus).label"
          />
        </template>
      </Column>

      <!-- 7. Actions -->
      <Column header="Action" style="width: 140px" class="text-center">
        <template #body="{ data }">
          <Button
            size="small"
            severity="secondary"
            outlined
            data-testid="view-receipt-btn"
            :disabled="!data.receiptNumber && data.confirmationStatus !== 'confirmed'"
            @click="openReceipt(data.id)"
          >
            <template #icon>
              <Receipt class="w-3.5 h-3.5 mr-1 text-emerald-600" />
            </template>
            View Receipt
          </Button>
        </template>
      </Column>
    </DataTable>

    <!-- Embedded Receipt Voucher Viewer -->
    <ReceiptVoucherViewer
      v-model:visible="isReceiptModalOpen"
      :payment-id="selectedReceiptPaymentId"
    />
  </div>
</template>
