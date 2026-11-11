<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { refDebounced } from '@vueuse/core';
import AutoComplete, { type AutoCompleteCompleteEvent } from 'primevue/autocomplete';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  Wallet,
  Search,
  User,
  GraduationCap,
  Receipt,
  FileText,
  RefreshCw,
  PlusCircle,
  Building2,
  Calendar,
  AlertCircle,
  CheckCircle2,
  ArrowRight,
  CreditCard,
  Layers,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
  FormField,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { formatIdr } from '../../shared/currency.utils';
import { getAccountStatusDescriptor } from '../../shared/financeStatus.utils';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  FINANCE_ACCOUNT_PERMISSIONS,
  FINANCE_OBLIGATION_PERMISSIONS,
  FINANCE_BILLING_PERMISSIONS,
  FINANCE_PAYMENT_PERMISSIONS,
  FINANCE_ALLOCATION_PERMISSIONS,
} from '../constants/permissions';
import {
  useStudentAccountQuery,
  useSearchStudentAccountsQuery,
} from '../composables/useFinanceAccounts';
import { useGenerateBillMutation } from '../../billing/composables/useFinanceBilling';
import { usePaymentsQuery } from '../../payments/composables/useFinancePayments';
import { useStudentReceivablesQuery } from '../../allocations/composables/useFinanceAllocations';
import { getPaymentById } from '../../payments/api/payments.api';
import type {
  PaymentListItemDto,
  PaymentAllocationSummaryDto,
} from '../../payments/types/payments.types';
import type { PaymentAllocationDto } from '../../allocations/types/allocations.types';
import type {
  StudentFinancialAccountDto,
  AccountSummaryDto,
} from '../types/accounts.types';
import AccountBalanceCard from '../components/AccountBalanceCard.vue';
import ObligationsTable from '../components/ObligationsTable.vue';
import BillingDocumentsTable from '../components/BillingDocumentsTable.vue';
import PaymentsHistoryTable from '../components/PaymentsHistoryTable.vue';
import AllocationsTable from '../components/AllocationsTable.vue';
import AssessAdHocFeeModal from '../components/AssessAdHocFeeModal.vue';
import InvoiceDetailModal from '../components/InvoiceDetailModal.vue';
import ReceiptVoucherViewer from '../../payments/components/ReceiptVoucherViewer.vue';
import ReallocatePaymentModal from '../../payments/components/ReallocatePaymentModal.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Route Parameters
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Finance', to: '/finance/obligations' },
  { label: 'Student Accounts Workbench', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canView = computed(() =>
  authStore.hasPermission(FINANCE_ACCOUNT_PERMISSIONS.VIEW),
);
const canAssess = computed(() =>
  authStore.hasPermission(FINANCE_OBLIGATION_PERMISSIONS.ASSESS),
);
const canGenerateBill = computed(() =>
  authStore.hasPermission(FINANCE_BILLING_PERMISSIONS.GENERATE),
);
const canReallocate = computed(() =>
  authStore.hasPermission(FINANCE_ALLOCATION_PERMISSIONS.REALLOCATE) ||
  authStore.hasRole(['FinanceOfficer', 'FinanceAuthority', 'Admin']),
);

// ---------------------------------------------------------------------------
// 3. Student Search & Selection State
// ---------------------------------------------------------------------------
const selectedStudentId = ref<string>('');
const searchInput = ref('');
const debouncedSearch = refDebounced(searchInput, 300);
const directoryFilter = ref<'all' | 'unallocated' | 'overdue'>('all');

useRouteQueryHydration((query) => {
  const filter = getQueryString(query, 'filter')?.toLowerCase();
  if (filter === 'unallocated' || filter === 'overdue' || filter === 'all') {
    directoryFilter.value = filter;
  }

  const queryStudentId = getQueryString(query, 'studentId');
  if (queryStudentId) {
    selectedStudentId.value = queryStudentId;
  }
});

// AutoComplete selection model
const selectedStudentSuggestion = ref<StudentFinancialAccountDto | null>(null);

// Search Query
const searchParams = computed(() => ({
  searchTerm: debouncedSearch.value || undefined,
  pageSize: directoryFilter.value !== 'all' ? 100 : 20,
}));

const {
  data: accountsListResponse,
  isLoading: isSearchingAccounts,
  refetch: refetchSearch,
} = useSearchStudentAccountsQuery(searchParams);

// Query confirmed payments only when unallocated filter is active
const { data: unallocatedPaymentsResponse } = usePaymentsQuery(
  computed(() => ({ status: 'confirmed', pageSize: 100 })),
  { enabled: computed(() => directoryFilter.value === 'unallocated') },
);

const unallocatedStudentIds = computed(() => {
  const ids = new Set<string>();
  const items = unallocatedPaymentsResponse.value?.items ?? [];
  for (const p of items) {
    if ((p.unallocatedAmount || 0) > 0 && p.studentId) {
      ids.add(p.studentId);
    }
  }
  return ids;
});

const studentAccountsList = computed<StudentFinancialAccountDto[]>(() => {
  const items = accountsListResponse.value?.items ?? [];
  if (directoryFilter.value === 'overdue') {
    return items.filter((acc) => (acc.currentBalance || 0) > 0);
  }
  if (directoryFilter.value === 'unallocated') {
    if (unallocatedPaymentsResponse.value === undefined) return items;
    return items.filter((acc) => unallocatedStudentIds.value.has(acc.studentId));
  }
  return items;
});

// ---------------------------------------------------------------------------
// 4. Student Financial Account Dossier Query (TD-02)
// ---------------------------------------------------------------------------
const {
  data: accountDossier,
  isLoading: isLoadingDossier,
  refetch: refetchDossier,
  isFetching: isFetchingDossier,
} = useStudentAccountQuery(selectedStudentId, {
  enabled: computed(() => Boolean(selectedStudentId.value)),
});

// Auto-select student from route param or query
onMounted(() => {
  const routeStudentId = (route.params.studentId as string) || (route.query.studentId as string);
  if (routeStudentId) {
    selectedStudentId.value = routeStudentId;
  }
});

watch(
  () => route.params.studentId,
  (newId) => {
    if (newId && typeof newId === 'string') {
      selectedStudentId.value = newId;
    }
  },
);

function selectStudent(student: StudentFinancialAccountDto) {
  selectedStudentId.value = student.studentId;
  selectedStudentSuggestion.value = student;
  try {
    const p = router.push({
      name: 'finance.accounts.workbench',
      params: { studentId: student.studentId },
    });
    if (p && typeof (p as any)?.catch === 'function') {
      (p as Promise<any>).catch(() => {});
    }
  } catch {
    // ignore
  }
}

function handleSearchSuggestionSelect(e: { value: StudentFinancialAccountDto }) {
  if (e.value && e.value.studentId) {
    selectStudent(e.value);
  }
}

// ---------------------------------------------------------------------------
// 5. Active Tab
// ---------------------------------------------------------------------------
const activeTab = ref('obligations');

// ---------------------------------------------------------------------------
// 6. Modal States
// ---------------------------------------------------------------------------
const isAssessModalOpen = ref(false);
const isInvoiceDetailModalOpen = ref(false);
const activeInvoiceDocumentId = ref<string | null>(null);

// Bill Generation Dialog State
const isGenerateBillModalOpen = ref(false);
const billTargetObligationIds = ref<string[]>([]);
const billPaymentDueDate = ref('');
const billNotes = ref('');

function getDefaultPaymentDueDate(): string {
  const d = new Date();
  d.setDate(d.getDate() + 14);
  return formatDateOnly(d);
}

function openAssessFeeModal() {
  isAssessModalOpen.value = true;
}

function openInvoiceDetailModal(documentId: string) {
  activeInvoiceDocumentId.value = documentId;
  isInvoiceDetailModalOpen.value = true;
}

function openGenerateBillDialog(obligationIds?: string[]) {
  billTargetObligationIds.value = obligationIds ?? [];
  billPaymentDueDate.value = getDefaultPaymentDueDate();
  billNotes.value = '';
  isGenerateBillModalOpen.value = true;
}

// Bill generation mutation
const { mutateAsync: generateBillMutate, isPending: isGeneratingBill } =
  useGenerateBillMutation();

async function handleConfirmGenerateBill() {
  if (!selectedStudentId.value) return;
  try {
    const payload = {
      studentId: selectedStudentId.value,
      obligationIds:
        billTargetObligationIds.value.length > 0
          ? billTargetObligationIds.value
          : undefined,
      paymentDueDate: billPaymentDueDate.value || undefined,
      notes: billNotes.value || undefined,
    };
    const res = await generateBillMutate(payload);
    isGenerateBillModalOpen.value = false;
    // Switch to billing documents tab and open invoice detail modal
    activeTab.value = 'documents';
    if (res.id) {
      openInvoiceDetailModal(res.id);
    }
  } catch {
    // Handled by toast notification
  }
}

// ---------------------------------------------------------------------------
// 7. Payments & Allocations Extensions (P6-S16, SCR-FIN-03)
// ---------------------------------------------------------------------------
// 7.1 Student Payments Query
const paymentsParams = computed(() => ({
  studentId: selectedStudentId.value || undefined,
  pageSize: 100,
}));

const {
  data: paymentsResponse,
  isLoading: isLoadingPayments,
  refetch: refetchPayments,
} = usePaymentsQuery(paymentsParams, {
  enabled: computed(() => Boolean(selectedStudentId.value)),
});

const studentPayments = computed<PaymentListItemDto[]>(
  () => paymentsResponse.value?.items ?? [],
);

// 7.2 Student Receivables Query (finance.v_student_receivables)
const {
  data: receivablesResponse,
  isLoading: isLoadingReceivables,
  refetch: refetchReceivables,
} = useStudentReceivablesQuery(selectedStudentId, undefined, {
  enabled: computed(() => Boolean(selectedStudentId.value)),
});

// 7.3 Derived Total Paid and Unallocated Credit Balance Metrics (Criterion 3)
const totalPaidAmount = computed<number>(() => {
  const confirmedPaid = studentPayments.value
    .filter((p) => p.confirmationStatus?.toLowerCase() === 'confirmed')
    .reduce((sum, p) => sum + (p.paymentAmount || 0), 0);
  return confirmedPaid > 0 ? confirmedPaid : (accountDossier.value?.totalSettled ?? 0);
});

const unallocatedCreditAmount = computed<number>(() => {
  return studentPayments.value
    .filter((p) => p.confirmationStatus?.toLowerCase() === 'confirmed')
    .reduce((sum, p) => sum + (p.unallocatedAmount || 0), 0);
});

// 7.4 Student Allocations Aggregation & Lineage
const studentAllocations = ref<
  (PaymentAllocationSummaryDto & { dueDate?: string; paymentId?: string })[]
>([]);
const isLoadingAllocations = ref(false);

async function syncStudentAllocations(payments: PaymentListItemDto[]) {
  const allocatedPayments = payments.filter((p) => (p.totalAllocated || 0) > 0);
  if (allocatedPayments.length === 0) {
    studentAllocations.value = [];
    return;
  }

  isLoadingAllocations.value = true;
  try {
    const details = await Promise.all(
      allocatedPayments.map(async (p) => {
        try {
          return await getPaymentById(p.id);
        } catch {
          return null;
        }
      }),
    );

    const oblMap = new Map<string, string>();
    accountDossier.value?.openObligations?.forEach((o) => {
      if (o.dueDate) oblMap.set(o.id, o.dueDate);
    });
    receivablesResponse.value?.receivables?.forEach((r) => {
      if (r.dueDate) oblMap.set(r.obligationId, r.dueDate);
    });

    const list: (PaymentAllocationSummaryDto & {
      dueDate?: string;
      paymentId?: string;
    })[] = [];
    for (const detail of details) {
      if (!detail || !detail.allocations) continue;
      for (const alloc of detail.allocations) {
        list.push({
          ...alloc,
          dueDate: oblMap.get(alloc.obligationId),
          paymentId: detail.id,
        });
      }
    }
    studentAllocations.value = list;
  } catch {
    // Preserve existing allocations on background fetch error
  } finally {
    isLoadingAllocations.value = false;
  }
}

watch(
  () => studentPayments.value,
  (newPayments) => {
    syncStudentAllocations(newPayments);
  },
  { immediate: true },
);

// 7.5 Modal State: Receipt Voucher Viewer
const isReceiptModalOpen = ref(false);
const selectedReceiptPaymentId = ref<string | null>(null);

function openReceiptViewer(paymentId: string) {
  selectedReceiptPaymentId.value = paymentId;
  isReceiptModalOpen.value = true;
}

// 7.6 Modal State: Reallocate Payment Funds
const isReallocateModalOpen = ref(false);
const selectedAllocationForReallocate = ref<
  (PaymentAllocationDto | PaymentAllocationSummaryDto) | null
>(null);
const selectedAllocationPaymentId = ref<string | null>(null);

function openReallocateModal(
  allocation: PaymentAllocationDto | PaymentAllocationSummaryDto,
) {
  selectedAllocationForReallocate.value = allocation;
  selectedAllocationPaymentId.value = (allocation as any).paymentId || null;
  isReallocateModalOpen.value = true;
}

function handleReallocated() {
  refetchDossier();
  refetchPayments();
  refetchReceivables();
}

function handleRefreshAll() {
  if (selectedStudentId.value) {
    refetchDossier();
    refetchPayments();
    refetchReceivables();
  } else {
    refetchSearch();
  }
}
</script>

<template>
  <div class="space-y-6" data-testid="student-accounts-workbench-view">
    <!-- Page Header -->
    <PageHeader
      title="Student Accounts Workbench"
      subtitle="Comprehensive student financial ledger, derived balance projection, ad-hoc fee assessments, and point-in-time billing document snapshots."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <Button
            size="small"
            severity="secondary"
            outlined
            data-testid="refresh-workbench-btn"
            :loading="isFetchingDossier || isLoadingPayments || isLoadingReceivables"
            @click="handleRefreshAll"
          >
            <template #icon>
              <RefreshCw class="w-4 h-4 mr-1.5" />
            </template>
            Refresh
          </Button>

          <Button
            v-if="selectedStudentId && canAssess"
            size="small"
            severity="secondary"
            outlined
            data-testid="header-assess-fee-btn"
            @click="openAssessFeeModal"
          >
            <template #icon>
              <PlusCircle class="w-4 h-4 mr-1.5 text-indigo-600" />
            </template>
            Assess Fee
          </Button>

          <Button
            v-if="selectedStudentId && canGenerateBill"
            size="small"
            severity="primary"
            data-testid="header-generate-bill-btn"
            @click="openGenerateBillDialog()"
          >
            <template #icon>
              <FileText class="w-4 h-4 mr-1.5" />
            </template>
            Compile Bill
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Student Search & Selection Bar -->
    <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-3 flex-1 min-w-[280px] max-w-xl">
        <div class="w-9 h-9 rounded-lg bg-indigo-50 text-indigo-600 flex items-center justify-center shrink-0">
          <Search class="w-4 h-4" />
        </div>
        <div class="flex-1">
          <AutoComplete
            v-model="selectedStudentSuggestion"
            :suggestions="studentAccountsList"
            option-label="studentFullName"
            placeholder="Search student by name, NIS, campus..."
            class="w-full"
            input-class="w-full text-sm"
            data-testid="student-search-autocomplete"
            @item-select="handleSearchSuggestionSelect"
            @complete="(e: AutoCompleteCompleteEvent) => { searchInput = e.query; }"
          >
            <template #option="{ option }">
              <div class="py-1 flex items-center justify-between gap-4 w-full">
                <div>
                  <div class="font-semibold text-slate-900 text-sm">
                    {{ option.studentFullName }}
                  </div>
                  <div class="text-xs text-slate-500 font-mono">
                    NIS: {{ option.studentNumber || '—' }} • {{ option.campus }} ({{ option.grade }})
                  </div>
                </div>
                <div class="text-right">
                  <div class="font-mono text-xs font-bold text-slate-800">
                    {{ formatIdr(option.currentBalance) }}
                  </div>
                  <span
                    class="text-[10px] uppercase font-bold px-1.5 py-0.5 rounded"
                    :class="option.currentBalance > 0 ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'"
                  >
                    {{ option.currentBalance > 0 ? 'Due' : 'Settled' }}
                  </span>
                </div>
              </div>
            </template>
          </AutoComplete>
        </div>
      </div>

      <!-- Quick Action / Status if Student is Selected -->
      <div v-if="selectedStudentId && accountDossier" class="flex items-center gap-3">
        <div class="text-right">
          <div class="text-xs text-slate-500">Active Dossier:</div>
          <div class="text-sm font-bold text-slate-900 flex items-center gap-1.5">
            <GraduationCap class="w-4 h-4 text-indigo-600" />
            {{ accountDossier.studentFullName || 'Student' }}
          </div>
        </div>
        <Button
          size="small"
          severity="secondary"
          text
          data-testid="clear-selected-student-btn"
          @click="selectedStudentId = ''; selectedStudentSuggestion = null;"
        >
          Change
        </Button>
      </div>
    </div>

    <!-- MAIN CONTENT AREA -->
    <!-- Case 1: Student is Selected & Dossier Loading -->
    <div v-if="selectedStudentId && isLoadingDossier" class="p-16 flex justify-center items-center">
      <LoadingSpinner />
    </div>

    <!-- Case 2: Student is Selected & Dossier Loaded -->
    <div v-else-if="selectedStudentId && accountDossier" class="space-y-6">
      <!-- 2.1 Ledger Balance Card (TD-02, SCR-FIN-03) -->
      <AccountBalanceCard
        :account="accountDossier"
        :can-assess-fee="canAssess"
        :can-generate-bill="canGenerateBill"
        :total-paid="totalPaidAmount"
        :unallocated-credit="unallocatedCreditAmount"
        @assess-fee="openAssessFeeModal"
        @generate-bill="openGenerateBillDialog()"
      />

      <!-- 2.2 Dossier Tabs: Obligations, Invoices, Payments, Allocations (SCR-FIN-03) -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <Tabs v-model:value="activeTab">
          <TabList class="px-6 border-b border-slate-200">
            <Tab value="obligations" class="flex items-center gap-2 py-3" data-testid="tab-obligations">
              <Receipt class="w-4 h-4" />
              <span>Assessed Obligations</span>
              <span
                class="ml-1 text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 font-semibold"
              >
                {{ accountDossier.openObligations?.length ?? 0 }}
              </span>
            </Tab>
            <Tab value="documents" class="flex items-center gap-2 py-3" data-testid="tab-documents">
              <FileText class="w-4 h-4" />
              <span>Billing Documents &amp; Statements</span>
              <span
                class="ml-1 text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 font-semibold"
              >
                {{ accountDossier.recentBillingDocuments?.length ?? 0 }}
              </span>
            </Tab>
            <Tab value="payments" class="flex items-center gap-2 py-3" data-testid="tab-payments">
              <CreditCard class="w-4 h-4" />
              <span>Payments &amp; Receipts</span>
              <span
                class="ml-1 text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 font-semibold"
                data-testid="payments-tab-count"
              >
                {{ studentPayments.length }}
              </span>
            </Tab>
            <Tab value="allocations" class="flex items-center gap-2 py-3" data-testid="tab-allocations">
              <Layers class="w-4 h-4" />
              <span>Allocations &amp; Lineage</span>
              <span
                class="ml-1 text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 font-semibold"
                data-testid="allocations-tab-count"
              >
                {{ studentAllocations.length }}
              </span>
            </Tab>
          </TabList>

          <TabPanels class="p-0">
            <!-- Obligations Tab -->
            <TabPanel value="obligations" class="p-0">
              <ObligationsTable
                :obligations="accountDossier.openObligations"
                :loading="isLoadingDossier"
                :can-assess-fee="canAssess"
                :can-generate-bill="canGenerateBill"
                @generate-bill="openGenerateBillDialog"
                @assess-fee="openAssessFeeModal"
              />
            </TabPanel>

            <!-- Billing Documents Tab -->
            <TabPanel value="documents" class="p-0">
              <BillingDocumentsTable
                :documents="accountDossier.recentBillingDocuments"
                :loading="isLoadingDossier"
                @view-invoice="openInvoiceDetailModal"
              />
            </TabPanel>

            <!-- Payments Tab (Criterion 1) -->
            <TabPanel value="payments" class="p-0">
              <PaymentsHistoryTable
                :payments="studentPayments"
                :loading="isLoadingPayments"
                @view-receipt="openReceiptViewer"
              />
            </TabPanel>

            <!-- Allocations Tab (Criterion 2) -->
            <TabPanel value="allocations" class="p-0">
              <AllocationsTable
                :allocations="studentAllocations"
                :loading="isLoadingAllocations || isLoadingPayments"
                :can-reallocate="canReallocate"
                @reallocate="openReallocateModal"
              />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
    </div>

    <!-- Case 3: No Student Selected - Show Directory / Overview Table -->
    <div v-else class="space-y-4">
      <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <Wallet class="w-5 h-5 text-indigo-600" />
            <h3 class="text-base font-semibold text-slate-800">
              Enrolled Student Financial Accounts Directory
            </h3>
          </div>
          <div class="flex items-center gap-2 flex-wrap">
            <div class="inline-flex rounded-lg bg-slate-100 p-1 text-xs font-medium text-slate-600">
              <button
                type="button"
                class="px-3 py-1 rounded-md transition cursor-pointer"
                :class="directoryFilter === 'all' ? 'bg-white text-slate-900 shadow-2xs font-semibold' : 'hover:text-slate-900'"
                data-testid="accounts-filter-all"
                @click="directoryFilter = 'all'"
              >
                All Accounts
              </button>
              <button
                type="button"
                class="px-3 py-1 rounded-md transition cursor-pointer"
                :class="directoryFilter === 'overdue' ? 'bg-white text-amber-800 shadow-2xs font-semibold' : 'hover:text-slate-900'"
                data-testid="accounts-filter-overdue"
                @click="directoryFilter = 'overdue'"
              >
                Overdue / Balance Due
              </button>
              <button
                type="button"
                class="px-3 py-1 rounded-md transition cursor-pointer"
                :class="directoryFilter === 'unallocated' ? 'bg-white text-indigo-700 shadow-2xs font-semibold' : 'hover:text-slate-900'"
                data-testid="accounts-filter-unallocated"
                @click="directoryFilter = 'unallocated'"
              >
                Unallocated Cash
              </button>
            </div>
          </div>
        </div>

        <DataTable
          :value="studentAccountsList"
          :loading="isSearchingAccounts"
          data-key="studentId"
          responsive-layout="scroll"
          striped-rows
          class="text-sm"
          data-testid="accounts-directory-table"
        >
          <template #empty>
            <div class="p-12">
              <EmptyState
                title="No Student Accounts Found"
                message="Try adjusting your search criteria to locate enrolled student financial accounts."
              />
            </div>
          </template>

          <!-- Student Name & NIS -->
          <Column field="studentFullName" header="Student" style="min-width: 200px">
            <template #body="{ data }">
              <div class="font-semibold text-slate-900 flex items-center gap-1.5">
                <User class="w-4 h-4 text-slate-400" />
                {{ data.studentFullName }}
              </div>
              <div class="text-xs text-slate-500 font-mono">
                NIS: {{ data.studentNumber || '—' }}
              </div>
            </template>
          </Column>

          <!-- Campus & Grade -->
          <Column header="Cohort / Campus" style="min-width: 140px">
            <template #body="{ data }">
              <div class="text-xs font-medium text-slate-800 flex items-center gap-1">
                <Building2 class="w-3.5 h-3.5 text-slate-400" />
                {{ data.campus }}
              </div>
              <div class="text-xs text-slate-500">
                Grade: {{ data.grade }}
              </div>
            </template>
          </Column>

          <!-- Account Number -->
          <Column field="accountNumber" header="Account #" style="min-width: 150px">
            <template #body="{ data }">
              <span class="font-mono text-xs text-slate-700 bg-slate-50 px-2 py-0.5 rounded border border-slate-200">
                {{ data.accountNumber }}
              </span>
            </template>
          </Column>

          <!-- Total Assessed -->
          <Column field="totalAssessed" header="Assessed" style="min-width: 120px" class="text-right">
            <template #body="{ data }">
              <span class="font-mono text-slate-700">
                {{ formatIdr(data.totalAssessed) }}
              </span>
            </template>
          </Column>

          <!-- Total Settled -->
          <Column field="totalSettled" header="Settled" style="min-width: 120px" class="text-right">
            <template #body="{ data }">
              <span class="font-mono text-emerald-700 font-medium">
                {{ formatIdr(data.totalSettled) }}
              </span>
            </template>
          </Column>

          <!-- Derived Balance (TD-02) -->
          <Column field="currentBalance" header="Current Balance" style="min-width: 140px" class="text-right">
            <template #body="{ data }">
              <span
                class="font-mono font-bold"
                :class="data.currentBalance > 0 ? 'text-amber-700' : 'text-emerald-700'"
              >
                {{ formatIdr(data.currentBalance) }}
              </span>
            </template>
          </Column>

          <!-- Status -->
          <Column field="accountStatus" header="Status" style="min-width: 100px">
            <template #body="{ data }">
              <StatusPill
                :status="data.accountStatus"
                :label="getAccountStatusDescriptor(data.accountStatus).label"
              />
            </template>
          </Column>

          <!-- Action -->
          <Column header="Action" style="width: 120px" class="text-center">
            <template #body="{ data }">
              <Button
                size="small"
                severity="primary"
                outlined
                data-testid="select-student-action-btn"
                @click="selectStudent(data)"
              >
                Inspect
                <ArrowRight class="w-3.5 h-3.5 ml-1" />
              </Button>
            </template>
          </Column>
        </DataTable>
      </div>
    </div>

    <!-- Modals -->
    <!-- 1. Assess Ad-Hoc Fee Modal -->
    <AssessAdHocFeeModal
      v-model:visible="isAssessModalOpen"
      :student-id="selectedStudentId"
      :student-name="accountDossier?.studentFullName"
      :student-number="accountDossier?.studentNumber"
      @assessed="refetchDossier"
    />

    <!-- 2. Invoice Detail Snapshot Modal -->
    <InvoiceDetailModal
      v-model:visible="isInvoiceDetailModalOpen"
      :document-id="activeInvoiceDocumentId"
    />

    <!-- 3. Compile Bill Dialog -->
    <Dialog
      v-model:visible="isGenerateBillModalOpen"
      modal
      :draggable="false"
      header="Compile Authorized Bill / Invoice"
      class="w-full max-w-md mx-4"
      data-testid="compile-bill-dialog"
    >
      <div class="space-y-4 text-sm">
        <p class="text-slate-600">
          Compile open financial obligations into an official invoice document with frozen snapshot line items.
        </p>

        <div v-if="billTargetObligationIds.length > 0" class="p-3 bg-indigo-50 border border-indigo-100 rounded-lg text-xs text-indigo-800">
          Compiling <strong>{{ billTargetObligationIds.length }}</strong> specifically selected obligation(s).
        </div>
        <div v-else class="p-3 bg-slate-50 border border-slate-200 rounded-lg text-xs text-slate-700">
          Compiling <strong>all open obligations</strong> for this student.
        </div>

        <FormField label="Payment Due Date" required>
          <InputText
            v-model="billPaymentDueDate"
            type="date"
            class="w-full"
            data-testid="bill-due-date-input"
          />
        </FormField>

        <FormField label="Invoice Notes (Optional)">
          <Textarea
            v-model="billNotes"
            rows="2"
            placeholder="e.g., Tagihan SPP dan Uang Pangkal Semester Ganjil"
            class="w-full text-sm"
            maxlength="2000"
            data-testid="bill-notes-input"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-3">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            outlined
            :disabled="isGeneratingBill"
            @click="isGenerateBillModalOpen = false"
          />
          <Button
            type="button"
            label="Generate Bill"
            severity="primary"
            :loading="isGeneratingBill"
            data-testid="confirm-generate-bill-btn"
            @click="handleConfirmGenerateBill"
          />
        </div>
      </template>
    </Dialog>

    <!-- 4. Receipt Voucher Viewer Modal (TD-04, SCR-FIN-02, SCR-FIN-03) -->
    <ReceiptVoucherViewer
      v-model:visible="isReceiptModalOpen"
      :payment-id="selectedReceiptPaymentId"
    />

    <!-- 5. Reallocate Payment Modal (TD-05, SCR-FIN-03) -->
    <ReallocatePaymentModal
      v-model:visible="isReallocateModalOpen"
      :allocation="selectedAllocationForReallocate"
      :payment-id="selectedAllocationPaymentId"
      :student-id="selectedStudentId"
      @reallocated="handleReallocated"
    />
  </div>
</template>
