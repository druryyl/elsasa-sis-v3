<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import {
  FileText,
  Calendar,
  User,
  Printer,
  Phone,
  CheckCircle2,
  Clock,
  Layers,
  Building2,
} from 'lucide-vue-next';
import { StatusPill, LoadingSpinner, EmptyState } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { getBillingStatusDescriptor } from '../../shared/financeStatus.utils';
import { useBillingDocumentDetailQuery } from '../../billing/composables/useFinanceBilling';

interface Props {
  visible: boolean;
  documentId: string | null | undefined;
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  documentId: null,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const documentIdRef = computed(() => props.documentId);

const { data: document, isLoading, isError, refetch } = useBillingDocumentDetailQuery(
  documentIdRef,
  {
    enabled: computed(() => Boolean(props.visible && props.documentId)),
  },
);

const statusDescriptor = computed(() =>
  getBillingStatusDescriptor(document.value?.deliveryStatus),
);

function handlePrint() {
  window.print();
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-2xl mx-4"
    data-testid="invoice-detail-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <FileText class="w-5 h-5 text-indigo-600" />
        <span class="font-semibold text-slate-900 text-lg">
          {{ document?.documentType === 'statement' ? 'Billing Statement' : 'Invoice / Bill' }}
        </span>
        <span
          v-if="document?.documentNumber"
          class="font-mono text-sm px-2 py-0.5 rounded bg-slate-100 text-slate-800 font-semibold"
          data-testid="invoice-document-number"
        >
          {{ document.documentNumber }}
        </span>
      </div>
    </template>

    <!-- Loading State -->
    <div v-if="isLoading" class="p-12 flex justify-center items-center">
      <LoadingSpinner />
    </div>

    <!-- Error State -->
    <div v-else-if="isError || !document" class="p-8 text-center">
      <EmptyState
        title="Document Unavailable"
        message="Could not load the requested billing document snapshot."
      />
      <Button
        label="Retry"
        size="small"
        class="mt-4"
        severity="secondary"
        @click="refetch"
      />
    </div>

    <!-- Document Content Snapshot (Printable) -->
    <div v-else class="space-y-6 text-sm print:m-0" data-testid="invoice-content">
      <!-- Invoice Header Block -->
      <div class="p-4 bg-slate-50 border border-slate-200 rounded-lg flex flex-wrap items-center justify-between gap-4">
        <div>
          <div class="text-xs uppercase tracking-wider font-semibold text-slate-500 mb-1">
            Institutional Billing Snapshot
          </div>
          <div class="font-bold text-slate-900 text-base flex items-center gap-2">
            <span>Elsasa Integrated Islamic School</span>
          </div>
          <div class="text-xs text-slate-500 mt-0.5">
            Issued via SIS Finance Billing Pipeline
          </div>
        </div>

        <div class="flex flex-col items-end gap-1.5">
          <StatusPill
            :status="document.deliveryStatus"
            :label="statusDescriptor.label"
            data-testid="invoice-status-badge"
          />
          <span class="text-xs text-slate-500">
            Delivery: <span class="capitalize font-medium text-slate-700">{{ document.deliveryChannel }}</span>
          </span>
        </div>
      </div>

      <!-- Recipient & Dates Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <!-- Recipient Information -->
        <div class="p-4 rounded-lg border border-slate-200 bg-white">
          <div class="text-xs font-semibold uppercase tracking-wider text-slate-500 mb-2 flex items-center gap-1.5">
            <User class="w-3.5 h-3.5" />
            Addressed Recipient
          </div>
          <div class="font-bold text-slate-900 text-sm" data-testid="invoice-recipient-name">
            {{ document.recipientName }}
          </div>
          <div class="text-xs text-slate-600 mt-1 capitalize">
            Relationship: {{ document.recipientRelationship }}
          </div>
          <div v-if="document.recipientContact" class="text-xs text-slate-500 mt-0.5 flex items-center gap-1">
            <Phone class="w-3 h-3 text-slate-400" />
            {{ document.recipientContact }}
          </div>
        </div>

        <!-- Dates Information -->
        <div class="p-4 rounded-lg border border-slate-200 bg-white">
          <div class="text-xs font-semibold uppercase tracking-wider text-slate-500 mb-2 flex items-center gap-1.5">
            <Calendar class="w-3.5 h-3.5" />
            Billing Timeline
          </div>
          <div class="text-xs text-slate-700 flex justify-between py-0.5">
            <span class="text-slate-500">Issue Date:</span>
            <span class="font-medium" data-testid="invoice-issue-date">{{ document.issueDate }}</span>
          </div>
          <div v-if="document.paymentDueDate" class="text-xs text-slate-700 flex justify-between py-0.5">
            <span class="text-slate-500">Payment Due:</span>
            <span class="font-semibold text-amber-700" data-testid="invoice-due-date">{{ document.paymentDueDate }}</span>
          </div>
          <div v-if="document.billingPeriodStart && document.billingPeriodEnd" class="text-xs text-slate-700 flex justify-between py-0.5">
            <span class="text-slate-500">Period:</span>
            <span class="font-medium">{{ document.billingPeriodStart }} to {{ document.billingPeriodEnd }}</span>
          </div>
        </div>
      </div>

      <!-- Frozen Itemized Line Items Table (TD-05) -->
      <div>
        <div class="text-xs font-semibold uppercase tracking-wider text-slate-500 mb-2 flex items-center gap-1.5">
          <Layers class="w-3.5 h-3.5" />
          Itemized Charges (Frozen Snapshot)
        </div>
        <DataTable
          :value="document.items"
          responsive-layout="scroll"
          striped-rows
          class="text-sm border border-slate-200 rounded-lg overflow-hidden"
          data-testid="invoice-items-table"
        >
          <Column field="lineSequence" header="#" style="width: 3rem">
            <template #body="{ data }">
              <span class="text-xs text-slate-400 font-mono">{{ data.lineSequence }}</span>
            </template>
          </Column>

          <Column field="description" header="Item Description">
            <template #body="{ data }">
              <div class="font-medium text-slate-900">{{ data.description }}</div>
              <div class="text-xs text-slate-400 capitalize">{{ data.itemType.replace(/_/g, ' ') }}</div>
            </template>
          </Column>

          <Column field="amount" header="Amount" class="text-right" style="min-width: 140px">
            <template #body="{ data }">
              <span class="font-mono font-semibold text-slate-900">
                {{ formatIdr(data.amount) }}
              </span>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Total Billed Amount & Summary -->
      <div class="p-4 bg-slate-50 rounded-lg border border-slate-200 flex items-center justify-between">
        <div>
          <div class="text-xs uppercase tracking-wider text-slate-500 font-semibold">
            Total Payable Amount
          </div>
          <p v-if="document.notes" class="text-xs text-slate-500 mt-0.5">
            Notes: {{ document.notes }}
          </p>
        </div>
        <div
          class="text-2xl font-extrabold font-mono text-emerald-800"
          data-testid="invoice-total-amount"
        >
          {{ formatIdr(document.totalBilledAmount) }}
        </div>
      </div>
    </div>

    <!-- Modal Actions Footer -->
    <template #footer>
      <div class="flex items-center justify-between w-full pt-2">
        <Button
          type="button"
          size="small"
          severity="secondary"
          outlined
          data-testid="print-invoice-btn"
          @click="handlePrint"
        >
          <template #icon>
            <Printer class="w-4 h-4 mr-1.5" />
          </template>
          Print Invoice
        </Button>

        <Button
          type="button"
          label="Close"
          size="small"
          severity="primary"
          @click="isDialogVisible = false"
        />
      </div>
    </template>
  </Dialog>
</template>
