<script setup lang="ts">
import { ref, computed } from 'vue';
import { refDebounced } from '@vueuse/core';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import Drawer from 'primevue/drawer';
import {
  Banknote,
  Search,
  RotateCcw,
  RefreshCw,
  Plus,
  CheckCircle2,
  XCircle,
  FileCheck,
  CreditCard,
  Building2,
  User,
  Calendar,
  Layers,
  ArrowRightLeft,
  Receipt,
  Eye,
  Clock,
  AlertTriangle,
  QrCode,
  Shield,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { formatDisplayDate, formatDisplayDateTime, formatDateOnly } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  FINANCE_PAYMENT_PERMISSIONS,
  FINANCE_ALLOCATION_PERMISSIONS,
} from '../constants/permissions';
import { usePaymentsQuery } from '../composables/useFinancePayments';
import type {
  PaymentListItemDto,
  PaymentConfirmationStatus,
  GetPaymentsParams,
} from '../types/payments.types';
import {
  RecordPaymentModal,
  ConfirmPaymentModal,
  RejectPaymentModal,
  AllocatePaymentModal,
  ReallocatePaymentModal,
  ReceiptVoucherViewer,
} from '../components';

// ---------------------------------------------------------------------------
// 1. Breadcrumbs & Permissions
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Finance', to: '/finance' },
  { label: 'Payment Verification Queue', current: true },
];

const authStore = useAuthStore();

const canRecord = computed(() =>
  authStore.hasPermission(FINANCE_PAYMENT_PERMISSIONS.RECORD),
);
const canConfirm = computed(() =>
  authStore.hasPermission(FINANCE_PAYMENT_PERMISSIONS.CONFIRM),
);
const canReject = computed(() =>
  authStore.hasPermission(FINANCE_PAYMENT_PERMISSIONS.REJECT),
);
const canAllocate = computed(() =>
  authStore.hasPermission(FINANCE_ALLOCATION_PERMISSIONS.EXECUTE),
);
const canReallocate = computed(() =>
  authStore.hasPermission(FINANCE_ALLOCATION_PERMISSIONS.REALLOCATE),
);

// ---------------------------------------------------------------------------
// 2. Filters & Search State
// ---------------------------------------------------------------------------
const selectedStatusTab = ref<string>('all');
const searchInput = ref<string>('');
const debouncedSearch = refDebounced(searchInput, 300);
const startDate = ref<string>('');
const endDate = ref<string>('');

const pageNumber = ref<number>(1);
const pageSize = ref<number>(20);

const statusTabs = [
  { label: 'All Payments', value: 'all' },
  { label: 'Submitted (Queue)', value: 'submitted' },
  { label: 'Confirmed', value: 'confirmed' },
  { label: 'Rejected', value: 'rejected' },
] as const;

const validStatusValues = new Set(['all', 'submitted', 'confirmed', 'rejected']);

useRouteQueryHydration((query) => {
  const status = getQueryString(query, 'status')?.toLowerCase();
  if (status && validStatusValues.has(status)) {
    selectedStatusTab.value = status;
    pageNumber.value = 1;
  }
});

const queryFilters = computed<GetPaymentsParams>(() => {
  const params: GetPaymentsParams = {
    pageNumber: pageNumber.value,
    pageSize: pageSize.value,
  };

  if (selectedStatusTab.value !== 'all') {
    params.status = selectedStatusTab.value;
  }
  if (debouncedSearch.value.trim()) {
    params.searchTerm = debouncedSearch.value.trim();
  }
  if (startDate.value) {
    params.startDate = startDate.value;
  }
  if (endDate.value) {
    params.endDate = endDate.value;
  }

  return params;
});

// Primary list query
const paymentsQuery = usePaymentsQuery(queryFilters);

// Dedicated metrics query fetching unfiltered list for summary statistics
const metricsQuery = usePaymentsQuery(computed(() => ({ pageSize: 100 })));

const paymentsList = computed<PaymentListItemDto[]>(() => {
  return paymentsQuery.data.value?.items ?? [];
});

const totalRecords = computed<number>(() => {
  return paymentsQuery.data.value?.totalCount ?? 0;
});

// Summary Metrics calculations
const todayDateStr = formatDateOnly(new Date());

const pendingSubmissionsCount = computed<number>(() => {
  const all = metricsQuery.data.value?.items ?? paymentsList.value;
  return all.filter((p) => p.confirmationStatus === 'submitted').length;
});

const confirmedTodayCount = computed<number>(() => {
  const all = metricsQuery.data.value?.items ?? paymentsList.value;
  return all.filter(
    (p) =>
      p.confirmationStatus === 'confirmed' &&
      (p.paymentDate === todayDateStr ||
        (p.confirmationDate && p.confirmationDate.startsWith(todayDateStr))),
  ).length;
});

const unallocatedTotalAmount = computed<number>(() => {
  const all = metricsQuery.data.value?.items ?? paymentsList.value;
  return all
    .filter((p) => p.confirmationStatus === 'confirmed')
    .reduce((sum, p) => sum + (p.unallocatedAmount || 0), 0);
});

function handleResetFilters() {
  selectedStatusTab.value = 'all';
  searchInput.value = '';
  startDate.value = '';
  endDate.value = '';
  pageNumber.value = 1;
}

// ---------------------------------------------------------------------------
// 3. Modals & Drawer State
// ---------------------------------------------------------------------------
const showRecordModal = ref<boolean>(false);

const selectedPayment = ref<PaymentListItemDto | null>(null);
const showConfirmModal = ref<boolean>(false);
const showRejectModal = ref<boolean>(false);
const showAllocateModal = ref<boolean>(false);
const showReallocateModal = ref<boolean>(false);

const activeReceiptPaymentId = ref<string | null>(null);
const showReceiptModal = ref<boolean>(false);

const showDetailDrawer = ref<boolean>(false);
const drawerPayment = ref<PaymentListItemDto | null>(null);

function openRecordModal() {
  showRecordModal.value = true;
}

function openConfirmModal(payment: PaymentListItemDto) {
  selectedPayment.value = payment;
  showConfirmModal.value = true;
}

function openRejectModal(payment: PaymentListItemDto) {
  selectedPayment.value = payment;
  showRejectModal.value = true;
}

function openAllocateModal(payment: PaymentListItemDto) {
  selectedPayment.value = payment;
  showAllocateModal.value = true;
}

function openReallocateModal(payment: PaymentListItemDto) {
  selectedPayment.value = payment;
  showReallocateModal.value = true;
}

function openReceiptModal(paymentId: string) {
  activeReceiptPaymentId.value = paymentId;
  showReceiptModal.value = true;
}

function openDetailDrawer(payment: PaymentListItemDto) {
  drawerPayment.value = payment;
  showDetailDrawer.value = true;
}

function handleMutationSuccess() {
  paymentsQuery.refetch();
  metricsQuery.refetch();
}
</script>

<template>
  <div class="space-y-6" data-testid="payment-verification-queue-view">
    <!-- Page Header -->
    <PageHeader
      title="Payment Verification Queue"
      subtitle="SCR-FIN-02: Cashier counter payment recording, portal verification triage, sequential receipting, and obligation fund allocation."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <Button
            severity="secondary"
            variant="outlined"
            aria-label="Refresh Queue"
            :loading="paymentsQuery.isFetching.value"
            @click="paymentsQuery.refetch"
          >
            <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': paymentsQuery.isFetching.value }" />
          </Button>
          <Button
            v-if="canRecord"
            severity="primary"
            data-testid="record-walkin-button"
            @click="openRecordModal"
          >
            <Plus class="w-4 h-4 mr-1.5" />
            <span>Record Walk-in Payment</span>
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Top Metrics Row -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4" data-testid="queue-metrics-strip">
      <!-- Metric 1: Pending Submissions -->
      <div class="rounded-xl border border-sky-100 bg-white p-5 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Pending Submissions
          </span>
          <p class="text-2xl font-extrabold text-slate-900 mt-1" data-testid="pending-count">
            {{ pendingSubmissionsCount }}
          </p>
          <span class="text-xs text-sky-700 font-medium mt-0.5 block">
            Awaiting cashier verification
          </span>
        </div>
        <div class="w-12 h-12 rounded-lg bg-sky-50 text-sky-600 flex items-center justify-center">
          <Clock class="w-6 h-6" />
        </div>
      </div>

      <!-- Metric 2: Confirmed Today -->
      <div class="rounded-xl border border-emerald-100 bg-white p-5 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Confirmed Today
          </span>
          <p class="text-2xl font-extrabold text-slate-900 mt-1" data-testid="confirmed-today-count">
            {{ confirmedTodayCount }}
          </p>
          <span class="text-xs text-emerald-700 font-medium mt-0.5 block">
            Sequential receipts issued
          </span>
        </div>
        <div class="w-12 h-12 rounded-lg bg-emerald-50 text-emerald-600 flex items-center justify-center">
          <CheckCircle2 class="w-6 h-6" />
        </div>
      </div>

      <!-- Metric 3: Unallocated Total -->
      <div class="rounded-xl border border-indigo-100 bg-white p-5 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Unallocated Total
          </span>
          <p class="text-2xl font-extrabold text-slate-900 mt-1 font-mono" data-testid="unallocated-total">
            {{ formatIDR(unallocatedTotalAmount) }}
          </p>
          <span class="text-xs text-indigo-700 font-medium mt-0.5 block">
            Available credit balances
          </span>
        </div>
        <div class="w-12 h-12 rounded-lg bg-indigo-50 text-indigo-600 flex items-center justify-center">
          <Layers class="w-6 h-6" />
        </div>
      </div>
    </div>

    <!-- Filter Bar & Status Tabs -->
    <div class="bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-4">
      <!-- Status Tabs -->
      <div class="flex items-center justify-between border-b border-slate-100 pb-3">
        <div class="inline-flex rounded-lg bg-slate-100 p-1 text-xs font-medium text-slate-600">
          <button
            v-for="tab in statusTabs"
            :key="tab.value"
            type="button"
            class="px-3.5 py-1.5 rounded-md transition-all flex items-center gap-1.5"
            :class="
              selectedStatusTab === tab.value
                ? 'bg-white text-slate-900 font-semibold shadow-xs'
                : 'hover:text-slate-900 text-slate-600'
            "
            :data-testid="`status-tab-${tab.value}`"
            @click="selectedStatusTab = tab.value"
          >
            <span>{{ tab.label }}</span>
            <span
              v-if="tab.value === 'submitted' && pendingSubmissionsCount > 0"
              class="px-1.5 py-0.2 rounded-full text-[10px] bg-amber-100 text-amber-800 font-bold"
            >
              {{ pendingSubmissionsCount }}
            </span>
          </button>
        </div>

        <Button
          severity="secondary"
          variant="text"
          size="small"
          class="text-xs text-slate-500"
          data-testid="reset-filters-button"
          @click="handleResetFilters"
        >
          <RotateCcw class="w-3.5 h-3.5 mr-1" />
          <span>Reset Filters</span>
        </Button>
      </div>

      <!-- Search & Date Range Filters -->
      <div class="grid grid-cols-1 sm:grid-cols-12 gap-3 text-xs">
        <!-- Search Input -->
        <div class="sm:col-span-6">
          <div class="relative">
            <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="searchInput"
              class="w-full pl-9 text-xs"
              placeholder="Search by student, applicant, payer name, receipt or payment #..."
              data-testid="search-input"
            />
          </div>
        </div>

        <!-- Start Date -->
        <div class="sm:col-span-3">
          <InputText
            v-model="startDate"
            type="date"
            class="w-full text-xs"
            placeholder="From Date"
            data-testid="start-date-input"
          />
        </div>

        <!-- End Date -->
        <div class="sm:col-span-3">
          <InputText
            v-model="endDate"
            type="date"
            class="w-full text-xs"
            placeholder="To Date"
            data-testid="end-date-input"
          />
        </div>
      </div>
    </div>

    <!-- Master Data Table -->
    <div class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-xs">
      <DataTable
        :value="paymentsList"
        :loading="paymentsQuery.isPending.value"
        paginator
        :rows="pageSize"
        :total-records="totalRecords"
        :rows-per-page-options="[10, 20, 50]"
        data-key="id"
        class="text-xs"
        data-testid="payments-table"
      >
        <template #empty>
          <EmptyState
            title="No payments found"
            description="No transactions match the selected filter criteria or queue status."
            :icon="Banknote"
          />
        </template>

        <!-- Submission / Payment Date -->
        <Column header="Date" style="width: 12%">
          <template #body="{ data }">
            <div>
              <p class="font-medium text-slate-800">
                {{ formatDisplayDate(data.paymentDate) }}
              </p>
              <span class="text-[10px] text-slate-500">
                {{ formatDisplayDateTime(data.submissionDate) }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Receipt / Payment Number -->
        <Column header="Receipt / Payment #" style="width: 16%">
          <template #body="{ data }">
            <div>
              <div v-if="data.receiptNumber" class="flex items-center gap-1.5">
                <span
                  class="font-mono font-bold text-emerald-800 bg-emerald-50 px-1.5 py-0.5 rounded border border-emerald-200"
                  data-testid="receipt-badge"
                >
                  {{ data.receiptNumber }}
                </span>
              </div>
              <p class="font-mono text-[11px] text-slate-500 mt-0.5">
                {{ data.paymentNumber }}
              </p>
            </div>
          </template>
        </Column>

        <!-- Student / Applicant Beneficiary -->
        <Column header="Student / Beneficiary" style="width: 20%">
          <template #body="{ data }">
            <div>
              <p class="font-semibold text-slate-900 truncate">
                {{ data.studentFullName || 'Pre-Enrollment Candidate' }}
              </p>
              <div class="flex items-center gap-1.5 text-[10px] text-slate-500 mt-0.5">
                <span v-if="data.studentNumber" class="font-mono">
                  NIM: {{ data.studentNumber }}
                </span>
                <span v-else-if="data.applicantId" class="text-indigo-600 font-medium bg-indigo-50 px-1 rounded">
                  Applicant
                </span>
                <span>• Acc: {{ data.accountNumber }}</span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Payer -->
        <Column header="Payer" style="width: 14%">
          <template #body="{ data }">
            <div>
              <p class="font-medium text-slate-800 truncate">
                {{ data.payerName }}
              </p>
              <span class="text-[10px] text-slate-500 font-mono">
                {{ data.payerNumber }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Payment Method -->
        <Column header="Method" style="width: 10%">
          <template #body="{ data }">
            <span class="capitalize font-medium text-slate-700">
              {{ data.paymentMethod.replace(/_/g, ' ') }}
            </span>
          </template>
        </Column>

        <!-- Amount (IDR) -->
        <Column header="Amount (IDR)" style="width: 14%" align-frozen="right">
          <template #body="{ data }">
            <div>
              <p class="font-mono font-bold text-slate-900">
                {{ formatIDR(data.paymentAmount) }}
              </p>
              <span
                v-if="data.confirmationStatus === 'confirmed' && data.unallocatedAmount > 0"
                class="text-[10px] text-amber-700 font-mono font-semibold"
              >
                Unallocated: {{ formatIDR(data.unallocatedAmount) }}
              </span>
              <span
                v-else-if="data.confirmationStatus === 'confirmed'"
                class="text-[10px] text-emerald-700 font-medium"
              >
                Fully Allocated
              </span>
            </div>
          </template>
        </Column>

        <!-- Status Pill -->
        <Column header="Status" style="width: 10%">
          <template #body="{ data }">
            <StatusPill
              :status="data.confirmationStatus"
              :variant="data.confirmationStatus === 'confirmed' ? 'emerald' : undefined"
              size="sm"
            />
          </template>
        </Column>

        <!-- Actions -->
        <Column header="Actions" style="width: 14%" frozen align-frozen="right">
          <template #body="{ data }">
            <div class="flex items-center gap-1">
              <!-- Submitted queue actions: Confirm / Reject -->
              <template v-if="data.confirmationStatus === 'submitted'">
                <Button
                  v-if="canConfirm"
                  v-tooltip.top="'Confirm &amp; Issue Receipt'"
                  severity="success"
                  size="small"
                  variant="text"
                  class="text-emerald-700 hover:bg-emerald-50 p-1"
                  :data-testid="`confirm-action-button-${data.id}`"
                  @click="openConfirmModal(data)"
                >
                  <CheckCircle2 class="w-4 h-4" />
                </Button>
                <Button
                  v-if="canReject"
                  v-tooltip.top="'Reject Submission'"
                  severity="danger"
                  size="small"
                  variant="text"
                  class="text-rose-700 hover:bg-rose-50 p-1"
                  :data-testid="`reject-action-button-${data.id}`"
                  @click="openRejectModal(data)"
                >
                  <XCircle class="w-4 h-4" />
                </Button>
              </template>

              <!-- Confirmed payment actions: View Receipt, Allocate, Reallocate -->
              <template v-else-if="data.confirmationStatus === 'confirmed'">
                <Button
                  v-tooltip.top="'View Official Receipt'"
                  severity="secondary"
                  size="small"
                  variant="text"
                  class="text-slate-700 hover:bg-slate-100 p-1"
                  :data-testid="`view-receipt-button-${data.id}`"
                  @click="openReceiptModal(data.id)"
                >
                  <Receipt class="w-4 h-4" />
                </Button>
                <Button
                  v-if="canAllocate && data.unallocatedAmount > 0"
                  v-tooltip.top="'Allocate Funds'"
                  severity="primary"
                  size="small"
                  variant="text"
                  class="text-indigo-700 hover:bg-indigo-50 p-1"
                  :data-testid="`allocate-action-button-${data.id}`"
                  @click="openAllocateModal(data)"
                >
                  <Layers class="w-4 h-4" />
                </Button>
                <Button
                  v-if="canReallocate"
                  v-tooltip.top="'Reallocate / Correct'"
                  severity="secondary"
                  size="small"
                  variant="text"
                  class="text-slate-500 hover:bg-slate-100 p-1"
                  :data-testid="`reallocate-action-button-${data.id}`"
                  @click="openReallocateModal(data)"
                >
                  <ArrowRightLeft class="w-4 h-4" />
                </Button>
              </template>

              <!-- Quick Dossier Drawer -->
              <Button
                v-tooltip.top="'Inspect Details'"
                severity="secondary"
                size="small"
                variant="text"
                class="text-slate-400 hover:text-slate-800 p-1"
                :data-testid="`view-details-button-${data.id}`"
                @click="openDetailDrawer(data)"
              >
                <Eye class="w-4 h-4" />
              </Button>
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Quick Detail Inspection Drawer -->
    <Drawer
      v-model:visible="showDetailDrawer"
      position="right"
      header="Payment Transaction Dossier"
      class="w-full sm:w-[28rem]"
      data-testid="payment-detail-drawer"
    >
      <div v-if="drawerPayment" class="space-y-4 text-xs text-slate-700">
        <!-- Top Status Card -->
        <div class="rounded-lg border border-slate-200 bg-slate-50 p-3.5 space-y-2">
          <div class="flex items-center justify-between">
            <span class="font-mono font-bold text-slate-900 text-sm">
              {{ drawerPayment.receiptNumber || drawerPayment.paymentNumber }}
            </span>
            <StatusPill
              :status="drawerPayment.confirmationStatus"
              :variant="drawerPayment.confirmationStatus === 'confirmed' ? 'emerald' : undefined"
              size="sm"
            />
          </div>
          <div class="flex items-center justify-between pt-1 border-t border-slate-200">
            <span class="text-slate-500">Gross Remittance</span>
            <span class="font-mono font-bold text-slate-900 text-sm">
              {{ formatIDR(drawerPayment.paymentAmount) }}
            </span>
          </div>
        </div>

        <!-- Candidate & Payer Details -->
        <div class="rounded-lg border border-slate-200 p-3.5 space-y-2.5">
          <h4 class="font-semibold text-slate-800 flex items-center gap-1.5">
            <User class="w-3.5 h-3.5 text-slate-500" /> Beneficiary &amp; Payer
          </h4>
          <div class="space-y-1.5 text-[11px]">
            <div class="flex justify-between">
              <span class="text-slate-500">Student:</span>
              <span class="font-medium text-slate-900">{{ drawerPayment.studentFullName || 'Pre-Enrollment Candidate' }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">Account #:</span>
              <span class="font-mono text-slate-700">{{ drawerPayment.accountNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">Payer Legal Name:</span>
              <span class="font-medium text-slate-900">{{ drawerPayment.payerName }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">Payer Code:</span>
              <span class="font-mono text-slate-700">{{ drawerPayment.payerNumber }}</span>
            </div>
          </div>
        </div>

        <!-- Remittance & Bank References -->
        <div class="rounded-lg border border-slate-200 p-3.5 space-y-2.5">
          <h4 class="font-semibold text-slate-800 flex items-center gap-1.5">
            <CreditCard class="w-3.5 h-3.5 text-slate-500" /> Remittance Proof &amp; Dates
          </h4>
          <div class="space-y-1.5 text-[11px]">
            <div class="flex justify-between">
              <span class="text-slate-500">Payment Method:</span>
              <span class="font-medium capitalize text-slate-900">{{ drawerPayment.paymentMethod.replace(/_/g, ' ') }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">External Slip / Ref:</span>
              <span class="font-mono font-medium text-slate-900">{{ drawerPayment.externalReference || '—' }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">Payment Date:</span>
              <span class="text-slate-900">{{ formatDisplayDate(drawerPayment.paymentDate) }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">Submission Date:</span>
              <span class="text-slate-900">{{ formatDisplayDateTime(drawerPayment.submissionDate) }}</span>
            </div>
            <div v-if="drawerPayment.confirmationDate" class="flex justify-between">
              <span class="text-slate-500">Confirmed At:</span>
              <span class="text-slate-900">{{ formatDisplayDateTime(drawerPayment.confirmationDate) }}</span>
            </div>
            <div v-if="drawerPayment.confirmedBy" class="flex justify-between">
              <span class="text-slate-500">Confirmed By:</span>
              <span class="text-slate-900 font-medium">{{ drawerPayment.confirmedBy }}</span>
            </div>
          </div>
        </div>

        <!-- Rejection Reason if Rejected -->
        <div
          v-if="drawerPayment.confirmationStatus === 'rejected' && drawerPayment.rejectionReason"
          class="rounded-lg border border-rose-200 bg-rose-50 p-3.5 space-y-1 text-rose-800"
        >
          <span class="font-semibold flex items-center gap-1.5">
            <AlertTriangle class="w-3.5 h-3.5 text-rose-600" /> Rejection Reason
          </span>
          <p class="text-[11px] leading-relaxed">{{ drawerPayment.rejectionReason }}</p>
        </div>

        <!-- Cashier Notes -->
        <div v-if="drawerPayment.notes" class="rounded-lg border border-slate-200 p-3 space-y-1">
          <span class="text-[11px] font-semibold text-slate-600">Internal Cashier Notes:</span>
          <p class="text-[11px] text-slate-700 italic">{{ drawerPayment.notes }}</p>
        </div>

        <!-- Quick Action Buttons inside Drawer -->
        <div class="pt-3 border-t border-slate-200 flex flex-col gap-2">
          <Button
            v-if="drawerPayment.confirmationStatus === 'confirmed'"
            severity="secondary"
            class="w-full text-xs"
            @click="openReceiptModal(drawerPayment.id)"
          >
            <Receipt class="w-4 h-4 mr-1.5" />
            <span>View Printable Receipt</span>
          </Button>
          <Button
            v-if="drawerPayment.confirmationStatus === 'confirmed' && drawerPayment.unallocatedAmount > 0"
            severity="primary"
            class="w-full text-xs"
            @click="openAllocateModal(drawerPayment)"
          >
            <Layers class="w-4 h-4 mr-1.5" />
            <span>Allocate Available Funds</span>
          </Button>
        </div>
      </div>
    </Drawer>

    <!-- Modal Dialogs -->
    <RecordPaymentModal
      v-model:visible="showRecordModal"
      @success="handleMutationSuccess"
    />

    <ConfirmPaymentModal
      v-model:visible="showConfirmModal"
      :payment="selectedPayment"
      @success="handleMutationSuccess"
    />

    <RejectPaymentModal
      v-model:visible="showRejectModal"
      :payment="selectedPayment"
      @success="handleMutationSuccess"
    />

    <AllocatePaymentModal
      v-model:visible="showAllocateModal"
      :payment="selectedPayment"
      @success="handleMutationSuccess"
    />

    <ReallocatePaymentModal
      v-model:visible="showReallocateModal"
      :payment="selectedPayment"
      @success="handleMutationSuccess"
    />

    <ReceiptVoucherViewer
      v-model:visible="showReceiptModal"
      :payment-id="activeReceiptPaymentId"
    />
  </div>
</template>
