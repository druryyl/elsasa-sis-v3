<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import {
  FileText,
  Calendar,
  CreditCard,
  Eye,
  AlertCircle,
  CheckCircle2,
  Clock,
  Download,
  DollarSign,
  ChevronRight,
  ShieldCheck,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import { formatIdr } from '../finance/shared/currency.utils';
import { getBillingStatusDescriptor } from '../finance/shared/financeStatus.utils';
import { useBillingDocumentsQuery } from '../finance/billing/composables/useFinanceBilling';
import InvoiceDetailModal from '../finance/accounts/components/InvoiceDetailModal.vue';
import type { BillingDocumentDto } from '../finance/billing/types/billing.types';

interface Props {
  studentId?: string;
  studentName?: string;
  payerName?: string;
  initialDocuments?: BillingDocumentDto[];
}

const props = withDefaults(defineProps<Props>(), {
  studentId: '',
  studentName: '',
  payerName: '',
  initialDocuments: () => [],
});

const emit = defineEmits<{
  (e: 'payNow', document: BillingDocumentDto): void;
  (e: 'viewInvoice', documentId: string): void;
}>();

// Selected document for invoice detail modal
const selectedInvoiceId = ref<string | null>(null);
const isInvoiceModalOpen = ref(false);

// Query billing documents for this student
const queryParams = computed(() => ({
  studentId: props.studentId || undefined,
  pageSize: 50,
}));

const {
  data: billingResponse,
  isLoading,
  refetch,
} = useBillingDocumentsQuery(queryParams, {
  enabled: computed(() => Boolean(props.studentId)),
});

const documentsList = computed<BillingDocumentDto[]>(() => {
  if (props.initialDocuments && props.initialDocuments.length > 0) {
    return props.initialDocuments;
  }
  return billingResponse.value?.items ?? [];
});

// Status filter: 'all' | 'unpaid' | 'paid'
const selectedFilter = ref<'all' | 'unpaid' | 'paid'>('all');

const filteredDocuments = computed(() => {
  if (selectedFilter.value === 'unpaid') {
    return documentsList.value.filter(
      (d) => d.deliveryStatus !== 'paid' && d.deliveryStatus !== 'cancelled',
    );
  }
  if (selectedFilter.value === 'paid') {
    return documentsList.value.filter((d) => d.deliveryStatus === 'paid');
  }
  return documentsList.value;
});

// Outstanding unpaid total
const totalOutstanding = computed(() => {
  return documentsList.value
    .filter((d) => d.deliveryStatus !== 'paid' && d.deliveryStatus !== 'cancelled')
    .reduce((sum, doc) => sum + (doc.totalBilledAmount ?? 0), 0);
});

function openInvoice(docId: string) {
  selectedInvoiceId.value = docId;
  isInvoiceModalOpen.value = true;
  emit('viewInvoice', docId);
}
</script>

<template>
  <div class="space-y-6" data-testid="family-billing-tab">
    <!-- Payer Outstanding Summary Banner -->
    <div
      class="rounded-xl border p-6 transition-colors shadow-xs"
      :class="
        totalOutstanding > 0
          ? 'bg-linear-to-r from-amber-500/10 via-amber-50/50 to-white border-amber-200'
          : 'bg-linear-to-r from-emerald-500/10 via-emerald-50/50 to-white border-emerald-200'
      "
    >
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <div
            class="w-12 h-12 rounded-xl flex items-center justify-center font-bold"
            :class="totalOutstanding > 0 ? 'bg-amber-100 text-amber-700' : 'bg-emerald-100 text-emerald-700'"
          >
            <CreditCard class="w-6 h-6" />
          </div>
          <div>
            <div class="text-xs uppercase tracking-wider font-semibold text-slate-500">
              Total Outstanding Balance
            </div>
            <div
              class="text-3xl font-extrabold font-mono tracking-tight mt-0.5"
              :class="totalOutstanding > 0 ? 'text-amber-800' : 'text-emerald-800'"
              data-testid="family-outstanding-amount"
            >
              {{ formatIdr(totalOutstanding) }}
            </div>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <div v-if="totalOutstanding > 0" class="flex items-center gap-1.5 text-xs text-amber-800 font-medium bg-amber-100/70 px-3 py-1.5 rounded-lg border border-amber-200">
            <AlertCircle class="w-4 h-4 text-amber-600 shrink-0" />
            <span>Please complete payment before the due date.</span>
          </div>
          <div v-else class="flex items-center gap-1.5 text-xs text-emerald-800 font-medium bg-emerald-100/70 px-3 py-1.5 rounded-lg border border-emerald-200">
            <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0" />
            <span>All tuition and fees are up to date!</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Filter Buttons & Controls -->
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div class="inline-flex p-1 rounded-lg bg-slate-100 border border-slate-200 text-xs font-medium">
        <button
          type="button"
          class="px-3 py-1.5 rounded-md transition-colors"
          :class="selectedFilter === 'all' ? 'bg-white text-slate-900 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
          data-testid="filter-all-btn"
          @click="selectedFilter = 'all'"
        >
          All Documents ({{ documentsList.length }})
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-md transition-colors"
          :class="selectedFilter === 'unpaid' ? 'bg-white text-slate-900 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
          data-testid="filter-unpaid-btn"
          @click="selectedFilter = 'unpaid'"
        >
          Open Bills
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-md transition-colors"
          :class="selectedFilter === 'paid' ? 'bg-white text-slate-900 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
          data-testid="filter-paid-btn"
          @click="selectedFilter = 'paid'"
        >
          Settled
        </button>
      </div>

      <Button
        size="small"
        severity="secondary"
        text
        class="text-xs"
        @click="refetch"
      >
        Refresh List
      </Button>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="p-12 flex justify-center items-center">
      <LoadingSpinner />
    </div>

    <!-- Documents List / Cards -->
    <div v-else-if="filteredDocuments.length === 0" class="p-12 bg-white rounded-xl border border-slate-200">
      <EmptyState
        title="No Billing Statements Found"
        message="There are no billing documents matching your selected filter."
        data-testid="family-billing-empty-state"
      />
    </div>

    <div v-else class="space-y-4" data-testid="family-bills-list">
      <div
        v-for="doc in filteredDocuments"
        :key="doc.id"
        class="bg-white rounded-xl border border-slate-200 p-5 shadow-xs hover:border-indigo-200 transition-all flex flex-wrap items-center justify-between gap-4"
        data-testid="family-bill-item"
      >
        <div class="flex items-start gap-4">
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0 mt-0.5"
            :class="doc.documentType === 'statement' ? 'bg-purple-50 text-purple-600' : 'bg-blue-50 text-blue-600'"
          >
            <FileText class="w-5 h-5" />
          </div>

          <div>
            <div class="flex items-center gap-2">
              <span class="font-mono text-xs font-bold text-slate-800 bg-slate-100 px-2 py-0.5 rounded">
                {{ doc.documentNumber }}
              </span>
              <span
                class="text-[10px] uppercase font-bold px-2 py-0.5 rounded border"
                :class="doc.documentType === 'statement' ? 'bg-purple-50 border-purple-200 text-purple-700' : 'bg-blue-50 border-blue-200 text-blue-700'"
              >
                {{ doc.documentType }}
              </span>
              <StatusPill
                :status="doc.deliveryStatus"
                :label="getBillingStatusDescriptor(doc.deliveryStatus).label"
              />
            </div>

            <div class="text-xs text-slate-500 flex flex-wrap items-center gap-3 mt-1.5">
              <span class="flex items-center gap-1">
                <Calendar class="w-3.5 h-3.5 text-slate-400" />
                Issued: {{ doc.issueDate }}
              </span>
              <span v-if="doc.paymentDueDate" class="flex items-center gap-1 font-medium text-amber-700">
                <Clock class="w-3.5 h-3.5 text-amber-500" />
                Due: {{ doc.paymentDueDate }}
              </span>
              <span v-if="doc.recipientName">
                Recipient: {{ doc.recipientName }}
              </span>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-4 ml-auto">
          <div class="text-right">
            <div class="text-xs text-slate-400 uppercase font-semibold">
              Amount
            </div>
            <div
              class="font-mono text-lg font-bold text-slate-900"
              data-testid="bill-amount-display"
            >
              {{ formatIdr(doc.totalBilledAmount) }}
            </div>
          </div>

          <Button
            size="small"
            severity="secondary"
            outlined
            data-testid="view-bill-details-btn"
            @click="openInvoice(doc.id)"
          >
            <template #icon>
              <Eye class="w-4 h-4 mr-1.5 text-indigo-600" />
            </template>
            View Details
          </Button>
        </div>
      </div>
    </div>

    <!-- Invoice Detail Snapshot Modal -->
    <InvoiceDetailModal
      v-model:visible="isInvoiceModalOpen"
      :document-id="selectedInvoiceId"
    />
  </div>
</template>
