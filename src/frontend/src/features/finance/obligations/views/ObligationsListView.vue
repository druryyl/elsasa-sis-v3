<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import {
  Receipt,
  Search,
  RotateCcw,
  RefreshCw,
  ArrowRight,
  AlertTriangle,
  Clock,
  CheckCircle2,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { useFinanceObligationsQuery } from '../composables/useFinanceObligations';
import type { FinancialObligationDto, GetObligationsParams } from '../types/obligations.types';

const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Finance', to: '/finance/accounts' },
  { label: 'Student Obligations & Billing', current: true },
];

// ---------------------------------------------------------------------------
// 1. Filters & Route Query Hydration
// ---------------------------------------------------------------------------
const selectedStatus = ref<string>('all');
const selectedAging = ref<string>('all');
const selectedChargeType = ref<string>('all');
const searchInput = ref<string>('');

const statusOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Unpaid / Open', value: 'unpaid' },
  { label: 'Overdue', value: 'overdue' },
  { label: 'Paid / Settled', value: 'paid' },
  { label: 'Assessed', value: 'assessed' },
  { label: 'Issued', value: 'issued' },
  { label: 'Partially Settled', value: 'partially_settled' },
  { label: 'Settled', value: 'settled' },
  { label: 'Waived', value: 'waived' },
  { label: 'Cancelled', value: 'cancelled' },
];

const agingOptions = [
  { label: 'All Aging Brackets', value: 'all' },
  { label: 'Current', value: 'Current' },
  { label: '1-30 Days', value: '1-30 Days' },
  { label: '31-60 Days', value: '31-60 Days' },
  { label: '61-90 Days', value: '61-90 Days' },
  { label: '90+ Days', value: '90+ Days' },
];

const chargeTypeOptions = [
  { label: 'All Charge Types', value: 'all' },
  { label: 'Tuition', value: 'tuition' },
  { label: 'Registration', value: 'registration' },
  { label: 'Facility', value: 'facility' },
  { label: 'Material', value: 'material' },
  { label: 'Activity', value: 'activity' },
  { label: 'Other', value: 'other' },
];

function normalizeAgingOption(val: string): string {
  const clean = val.replace(/%2B/gi, '+').replace(/%20/gi, ' ').trim().toLowerCase();
  if (clean === 'current') return 'Current';
  if (clean === '1-30' || clean === '1-30 days' || clean === '1-30-days') return '1-30 Days';
  if (clean === '31-60' || clean === '31-60 days' || clean === '31-60-days') return '31-60 Days';
  if (clean === '61-90' || clean === '61-90 days' || clean === '61-90-days') return '61-90 Days';
  if (clean === '90+' || clean === '90+ days' || clean === '90-plus-days' || clean === '90 plus days') return '90+ Days';
  return val.trim();
}

useRouteQueryHydration((query) => {
  const status = getQueryString(query, 'status');
  if (status) {
    selectedStatus.value = status.toLowerCase();
  }

  const aging = getQueryString(query, 'aging');
  if (aging) {
    selectedAging.value = normalizeAgingOption(aging);
  }

  const chargeType = getQueryString(query, 'chargeType');
  if (chargeType) {
    selectedChargeType.value = chargeType;
  }
});

const queryParams = computed<GetObligationsParams>(() => {
  const params: GetObligationsParams = {
    pageNumber: 1,
    pageSize: 100,
  };

  if (selectedStatus.value !== 'all') {
    params.status = selectedStatus.value;
  }

  if (selectedAging.value !== 'all') {
    params.aging = selectedAging.value;
  }

  if (selectedChargeType.value !== 'all') {
    params.chargeType = selectedChargeType.value;
  }

  if (searchInput.value.trim()) {
    params.search = searchInput.value.trim();
  }

  return params;
});

const {
  data: obligationsResponse,
  isLoading,
  isFetching,
  refetch,
} = useFinanceObligationsQuery(queryParams);

const allObligations = computed<FinancialObligationDto[]>(
  () => obligationsResponse.value?.items ?? [],
);

function isOpenStatus(status: string): boolean {
  const s = status.toLowerCase();
  return s !== 'settled' && s !== 'waived' && s !== 'cancelled';
}

function getDaysPastDue(dueDateStr?: string | null): number {
  if (!dueDateStr) return 0;
  const due = new Date(`${dueDateStr.slice(0, 10)}T00:00:00Z`);
  if (Number.isNaN(due.getTime())) return 0;
  const now = new Date();
  const todayUtc = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  return Math.floor((todayUtc.getTime() - due.getTime()) / (1000 * 60 * 60 * 24));
}

function computeAgingBracket(obligation: FinancialObligationDto): string {
  if (obligation.agingBracket) return obligation.agingBracket;
  if (!isOpenStatus(obligation.status)) return 'Settled';
  const days = getDaysPastDue(obligation.dueDate);
  if (days <= 0) return 'Current';
  if (days <= 30) return '1-30 Days';
  if (days <= 60) return '31-60 Days';
  if (days <= 90) return '61-90 Days';
  return '90+ Days';
}

function isOverdue(obligation: FinancialObligationDto): boolean {
  if (!isOpenStatus(obligation.status)) return false;
  if (obligation.schedules?.some((s) => s.status?.toLowerCase() === 'overdue')) {
    return true;
  }
  return getDaysPastDue(obligation.dueDate) > 0;
}

const filteredObligations = computed<FinancialObligationDto[]>(() => {
  return allObligations.value.filter((item) => {
    const statusLower = item.status?.toLowerCase() ?? '';

    if (selectedStatus.value === 'unpaid') {
      if (!isOpenStatus(statusLower) || (item.outstandingAmount !== undefined && item.outstandingAmount !== null && item.outstandingAmount <= 0)) {
        return false;
      }
    } else if (selectedStatus.value === 'paid') {
      if (statusLower !== 'settled' && (item.outstandingAmount === undefined || item.outstandingAmount === null || item.outstandingAmount > 0)) {
        return false;
      }
    } else if (selectedStatus.value === 'overdue') {
      if (!isOverdue(item)) return false;
    } else if (selectedStatus.value !== 'all') {
      if (statusLower !== selectedStatus.value.toLowerCase()) return false;
    }

    if (selectedAging.value !== 'all') {
      if (item.agingBracket) {
        if (item.agingBracket.toLowerCase() !== selectedAging.value.toLowerCase()) {
          return false;
        }
      } else {
        if (!isOpenStatus(statusLower)) return false;
        if (computeAgingBracket(item).toLowerCase() !== selectedAging.value.toLowerCase()) {
          return false;
        }
      }
    }

    if (selectedChargeType.value !== 'all') {
      if (item.chargeType?.toLowerCase() !== selectedChargeType.value.toLowerCase()) {
        return false;
      }
    }

    if (searchInput.value.trim()) {
      const term = searchInput.value.trim().toLowerCase();
      const matchNumber = item.obligationNumber?.toLowerCase().includes(term);
      const matchName = item.studentFullName?.toLowerCase().includes(term);
      const matchStudentNum = item.studentNumber?.toLowerCase().includes(term);
      const matchDesc = item.description?.toLowerCase().includes(term);
      if (!matchNumber && !matchName && !matchStudentNum && !matchDesc) {
        return false;
      }
    }

    return true;
  });
});

const totalAmount = computed(() =>
  filteredObligations.value.reduce((sum, item) => sum + (item.originalAmount || 0), 0),
);
const overdueCount = computed(
  () => filteredObligations.value.filter((item) => isOverdue(item)).length,
);

function resetFilters() {
  selectedStatus.value = 'all';
  selectedAging.value = 'all';
  selectedChargeType.value = 'all';
  searchInput.value = '';
}

function inspectStudentAccount(studentId: string) {
  router.push(`/finance/accounts/${encodeURIComponent(studentId)}`);
}
</script>

<template>
  <div class="space-y-6" data-testid="obligations-list-view">
    <PageHeader
      title="Student Obligations & Billing"
      description="Monitor assessed tuition and fee obligations, overdue schedules, and receivables aging brackets."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <Button
          size="small"
          severity="secondary"
          outlined
          :loading="isFetching"
          data-testid="refresh-obligations-btn"
          @click="() => refetch()"
        >
          <RefreshCw class="w-3.5 h-3.5 mr-1.5" :class="{ 'animate-spin': isFetching }" />
          <span>Refresh</span>
        </Button>
      </template>
    </PageHeader>

    <!-- Summary Strip -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div class="bg-white rounded-xl border border-slate-200 p-4 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Matching Obligations
          </span>
          <p class="text-2xl font-bold text-slate-900 mt-1" data-testid="obligations-count">
            {{ filteredObligations.length }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-lg bg-indigo-50 text-indigo-600 flex items-center justify-center">
          <Receipt class="w-5 h-5" />
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 p-4 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Assessed Value
          </span>
          <p class="text-2xl font-bold text-slate-900 mt-1 font-mono" data-testid="obligations-total-amount">
            {{ formatIDR(totalAmount) }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-lg bg-emerald-50 text-emerald-600 flex items-center justify-center">
          <CheckCircle2 class="w-5 h-5" />
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 p-4 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Overdue Items
          </span>
          <p class="text-2xl font-bold text-rose-700 mt-1" data-testid="obligations-overdue-count">
            {{ overdueCount }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-lg bg-rose-50 text-rose-600 flex items-center justify-center">
          <AlertTriangle class="w-5 h-5" />
        </div>
      </div>
    </div>

    <!-- Filter Bar -->
    <div class="bg-white rounded-xl border border-slate-200 p-4 shadow-xs space-y-3">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 text-xs">
        <div>
          <label class="block text-slate-500 font-medium mb-1">Search</label>
          <div class="relative">
            <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="searchInput"
              placeholder="Obligation #, student, description..."
              class="w-full !pl-9 text-xs"
              data-testid="obligations-search-input"
            />
          </div>
        </div>

        <div>
          <label class="block text-slate-500 font-medium mb-1">Obligation Status</label>
          <Select
            v-model="selectedStatus"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="obligations-status-select"
          />
        </div>

        <div>
          <label class="block text-slate-500 font-medium mb-1">Aging Bracket</label>
          <Select
            v-model="selectedAging"
            :options="agingOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="obligations-aging-select"
          />
        </div>

        <div>
          <label class="block text-slate-500 font-medium mb-1">Charge Type</label>
          <Select
            v-model="selectedChargeType"
            :options="chargeTypeOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="obligations-charge-type-select"
          />
        </div>
      </div>

      <div class="flex items-center justify-between pt-1 text-xs">
        <span class="text-slate-500">
          Showing <strong>{{ filteredObligations.length }}</strong> obligation{{ filteredObligations.length === 1 ? '' : 's' }}
        </span>
        <Button
          v-if="selectedStatus !== 'all' || selectedAging !== 'all' || selectedChargeType !== 'all' || searchInput"
          size="small"
          variant="text"
          severity="secondary"
          class="!text-xs !py-0.5 !px-2 cursor-pointer flex items-center gap-1"
          data-testid="reset-obligations-filters-btn"
          @click="resetFilters"
        >
          <RotateCcw class="w-3.5 h-3.5" />
          <span>Reset Filters</span>
        </Button>
      </div>
    </div>

    <!-- Obligations Table -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
      <div v-if="isLoading" class="py-12 flex justify-center">
        <LoadingSpinner label="Loading financial obligations..." />
      </div>

      <DataTable
        v-else
        :value="filteredObligations"
        data-key="id"
        responsive-layout="scroll"
        striped-rows
        class="text-xs"
        data-testid="obligations-data-table"
      >
        <template #empty>
          <div class="py-10">
            <EmptyState
              title="No matching obligations"
              description="No student obligations match the active status, aging bracket, or search filters."
              size="sm"
            />
          </div>
        </template>

        <Column field="obligationNumber" header="Obligation #" style="min-width: 140px">
          <template #body="{ data }">
            <span class="font-mono font-semibold text-slate-800">{{ data.obligationNumber }}</span>
          </template>
        </Column>

        <Column header="Student" style="min-width: 180px">
          <template #body="{ data }">
            <div>
              <div class="font-semibold text-slate-900">{{ data.studentFullName || '—' }}</div>
              <div class="text-[11px] font-mono text-slate-500">{{ data.studentNumber || '—' }}</div>
            </div>
          </template>
        </Column>

        <Column header="Description" style="min-width: 200px">
          <template #body="{ data }">
            <div>
              <div class="font-medium text-slate-800">{{ data.description }}</div>
              <div class="text-[11px] text-slate-500 capitalize">{{ data.chargeType }}</div>
            </div>
          </template>
        </Column>

        <Column header="Amount" style="min-width: 130px" class="text-right">
          <template #body="{ data }">
            <span class="font-mono font-semibold text-slate-900">
              {{ formatIDR(data.originalAmount) }}
            </span>
          </template>
        </Column>

        <Column header="Due Date & Aging" style="min-width: 150px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="flex items-center gap-1 text-slate-700">
                <Clock class="w-3 h-3 text-slate-400" />
                <span>{{ formatDisplayDate(data.dueDate) }}</span>
              </div>
              <span
                class="inline-block text-[10px] font-semibold px-1.5 py-0.5 rounded"
                :class="
                  computeAgingBracket(data) === '90+ Days' || computeAgingBracket(data) === '61-90 Days'
                    ? 'bg-rose-50 text-rose-700'
                    : computeAgingBracket(data) === 'Settled'
                      ? 'bg-emerald-50 text-emerald-700'
                      : 'bg-slate-100 text-slate-600'
                "
              >
                {{ computeAgingBracket(data) }}
              </span>
            </div>
          </template>
        </Column>

        <Column field="status" header="Status" style="min-width: 120px">
          <template #body="{ data }">
            <StatusPill :status="data.status" size="sm" />
          </template>
        </Column>

        <Column header="Action" style="min-width: 120px" class="text-right">
          <template #body="{ data }">
            <Button
              size="small"
              severity="primary"
              outlined
              class="!text-xs"
              data-testid="inspect-obligation-account-btn"
              @click="inspectStudentAccount(data.studentId)"
            >
              <span>Account</span>
              <ArrowRight class="w-3.5 h-3.5 ml-1" />
            </Button>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>
