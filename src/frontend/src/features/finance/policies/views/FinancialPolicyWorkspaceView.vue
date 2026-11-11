<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { refDebounced } from '@vueuse/core';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import {
  Shield,
  Layers,
  PlusCircle,
  RefreshCw,
  Search,
  CheckCircle2,
  Calendar,
  FileText,
  Tag,
  Building2,
  AlertCircle,
  HelpCircle,
  Clock,
  RotateCcw,
  UserCheck,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
  FormField,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { formatIdr } from '../../shared/currency.utils';
import { useAuthStore } from '@/app/stores/auth.store';
import { FINANCE_POLICY_PERMISSIONS } from '../constants/permissions';
import {
  useFinancePolicyCategoriesQuery,
  useFinancePoliciesQuery,
  useFinancePolicyDetailQuery,
  useApprovePolicyMutation,
} from '../composables/useFinancePolicies';
import type { FinancialPolicyDto, ChargeRuleDto } from '../types/policies.types';
import CreatePolicyModal from '../components/CreatePolicyModal.vue';
import CreateChargeRuleModal from '../components/CreateChargeRuleModal.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Finance', to: '/finance/obligations' },
  { label: 'Financial Policy Workspace', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canManage = computed(() =>
  authStore.hasPermission(FINANCE_POLICY_PERMISSIONS.MANAGE),
);
const canApprove = computed(() =>
  authStore.hasPermission(FINANCE_POLICY_PERMISSIONS.APPROVE),
);

// ---------------------------------------------------------------------------
// 3. Filters & State
// ---------------------------------------------------------------------------
const selectedStatus = ref<string>('');
const selectedCategory = ref<string>('');
const searchInput = ref<string>('');
const debouncedSearch = refDebounced(searchInput, 300);

const selectedPolicyId = ref<string | null>(null);

// Modal visibility flags
const isCreatePolicyModalOpen = ref(false);
const isCreateChargeRuleModalOpen = ref(false);
const isApproveModalOpen = ref(false);

// Approval dialog inputs
const approvingAuthorityInput = ref('');
const approvalNotesInput = ref('');

const {
  data: policyCategories,
  isLoading: isLoadingCategories,
} = useFinancePolicyCategoriesQuery();

const categoryFilterOptions = computed(() => [
  { label: 'All Categories', value: '' },
  ...(policyCategories.value ?? []).map((c) => ({
    label: c.name,
    value: c.code,
  })),
]);

const categoryNameByCode = computed<Record<string, string>>(() => {
  const map: Record<string, string> = {};
  for (const category of policyCategories.value ?? []) {
    map[category.code] = category.name;
  }
  return map;
});

const statusFilterPills = [
  { label: 'All', value: '' },
  { label: 'Draft', value: 'draft' },
  { label: 'Active', value: 'active' },
  { label: 'Superseded', value: 'superseded' },
  { label: 'Archived', value: 'archived' },
];

// ---------------------------------------------------------------------------
// 4. Queries & Invalidation
// ---------------------------------------------------------------------------
const queryFilters = computed(() => ({
  status: selectedStatus.value || undefined,
  category: selectedCategory.value || undefined,
}));

const {
  data: policies,
  isLoading: isLoadingPolicies,
  isFetching: isFetchingPolicies,
  refetch: refetchPolicies,
} = useFinancePoliciesQuery(queryFilters);

// Filtered client policies (applying search text filter)
const filteredPolicies = computed<FinancialPolicyDto[]>(() => {
  if (!policies.value) return [];
  const search = debouncedSearch.value.trim().toLowerCase();
  if (!search) return policies.value;

  return policies.value.filter((p) => {
    return (
      p.policyCode.toLowerCase().includes(search) ||
      p.name.toLowerCase().includes(search) ||
      (p.businessPurpose && p.businessPurpose.toLowerCase().includes(search)) ||
      (p.applicableContext && p.applicableContext.toLowerCase().includes(search))
    );
  });
});

// Status counts derived from loaded policies
const statusCounts = computed(() => {
  const counts: Record<string, number> = {
    all: policies.value?.length ?? 0,
    draft: 0,
    active: 0,
    superseded: 0,
    archived: 0,
  };
  if (policies.value) {
    for (const p of policies.value) {
      const s = (p.status ?? '').toLowerCase();
      if (s in counts) {
        counts[s]++;
      }
    }
  }
  return counts;
});

// Automatically select first policy or preserve selection
watch(
  () => filteredPolicies.value,
  (currentList) => {
    if (!currentList || currentList.length === 0) {
      selectedPolicyId.value = null;
      return;
    }
    const exists = currentList.some((p) => p.id === selectedPolicyId.value);
    if (!exists) {
      // Pick first active, fallback to first in list
      const firstActive = currentList.find((p) => p.status === 'active');
      selectedPolicyId.value = firstActive ? firstActive.id : currentList[0].id;
    }
  },
  { immediate: true },
);

// Detail query for selected policy
const {
  data: selectedPolicyDetail,
  isLoading: isLoadingDetail,
  isFetching: isFetchingDetail,
  refetch: refetchDetail,
} = useFinancePolicyDetailQuery(selectedPolicyId);

// Selected policy object (prefers detail with rules, falls back to summary)
const selectedPolicy = computed<FinancialPolicyDto | null>(() => {
  if (selectedPolicyDetail.value && selectedPolicyDetail.value.id === selectedPolicyId.value) {
    return selectedPolicyDetail.value;
  }
  if (!selectedPolicyId.value || !policies.value) return null;
  return policies.value.find((p) => p.id === selectedPolicyId.value) ?? null;
});

// Attached charge rules list
const attachedChargeRules = computed<ChargeRuleDto[]>(() => {
  return selectedPolicy.value?.rules ?? [];
});

function selectPolicy(policy: FinancialPolicyDto) {
  selectedPolicyId.value = policy.id;
}

function resetFilters() {
  selectedStatus.value = '';
  selectedCategory.value = '';
  searchInput.value = '';
}

// ---------------------------------------------------------------------------
// 5. Policy Approval Mutation
// ---------------------------------------------------------------------------
const approveMutation = useApprovePolicyMutation({
  onSuccess: () => {
    isApproveModalOpen.value = false;
    refetchPolicies();
    refetchDetail();
  },
});

function openApproveModal() {
  if (!selectedPolicy.value) return;
  approvingAuthorityInput.value =
    authStore.user?.fullName || authStore.user?.username || 'Finance Authority';
  approvalNotesInput.value = '';
  isApproveModalOpen.value = true;
}

async function handleConfirmApprove() {
  if (!selectedPolicy.value) return;
  await approveMutation.mutateAsync({
    id: selectedPolicy.value.id,
    request: {
      approvingAuthority: approvingAuthorityInput.value.trim() || undefined,
      notes: approvalNotesInput.value.trim() || undefined,
    },
  });
}

function onPolicyCreated(createdPolicy: { id: string }) {
  selectedPolicyId.value = createdPolicy.id;
  refetchPolicies();
}

function onRuleCreated() {
  refetchDetail();
}
</script>

<template>
  <div class="space-y-5" data-testid="financial-policy-workspace">
    <!-- 1. Top Header -->
    <PageHeader
      title="Financial Policy Workspace"
      description="Define, version, and formally approve institutional financial policies and charge rules (FIN-WF-01, UC-FIN-10)."
      :breadcrumbs="breadcrumbs"
      data-testid="policies-page-header"
    >
      <template #actions>
        <div class="flex items-center gap-2.5">
          <!-- Refresh Button -->
          <Button
            size="small"
            severity="secondary"
            variant="outlined"
            :loading="isFetchingPolicies || isFetchingDetail"
            class="!text-xs !py-1.5 cursor-pointer"
            data-testid="refresh-policies-button"
            @click="() => { refetchPolicies(); refetchDetail(); }"
          >
            <RefreshCw class="w-3.5 h-3.5 mr-1.5" :class="{ 'animate-spin': isFetchingPolicies }" />
            <span>Refresh</span>
          </Button>

          <!-- New Policy Button -->
          <Button
            v-if="canManage || canApprove"
            size="small"
            class="!text-xs !py-1.5 cursor-pointer bg-teal-700 hover:bg-teal-800 text-white font-medium inline-flex items-center gap-1.5"
            data-testid="new-policy-button"
            @click="isCreatePolicyModalOpen = true"
          >
            <PlusCircle class="w-3.5 h-3.5" />
            <span>New Policy</span>
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- 2. Filter & Status Bar -->
    <div
      class="bg-white p-4 rounded-xl border border-slate-200/80 shadow-2xs space-y-4"
      data-testid="policy-filter-panel"
    >
      <!-- Row 1: Status Filter Pills -->
      <div class="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
        <div class="flex flex-wrap items-center gap-1.5" data-testid="status-pills-bar">
          <span class="text-xs font-semibold text-slate-500 mr-2 uppercase tracking-wider">Status:</span>
          <button
            v-for="pill in statusFilterPills"
            :key="pill.value"
            type="button"
            :class="[
              'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium transition cursor-pointer',
              selectedStatus === pill.value
                ? 'bg-teal-700 text-white shadow-xs font-semibold'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200/70',
            ]"
            :data-testid="`filter-status-${pill.value || 'all'}`"
            @click="selectedStatus = pill.value"
          >
            <span>{{ pill.label }}</span>
            <span
              v-if="pill.value === '' && statusCounts.all > 0"
              class="px-1.5 py-0.2 rounded-full text-[10px]"
              :class="selectedStatus === '' ? 'bg-teal-800 text-teal-100' : 'bg-slate-200 text-slate-700'"
            >
              {{ statusCounts.all }}
            </span>
            <span
              v-else-if="pill.value && statusCounts[pill.value] !== undefined"
              class="px-1.5 py-0.2 rounded-full text-[10px]"
              :class="selectedStatus === pill.value ? 'bg-teal-800 text-teal-100' : 'bg-slate-200 text-slate-700'"
            >
              {{ statusCounts[pill.value] }}
            </span>
          </button>
        </div>

        <button
          v-if="selectedStatus || selectedCategory || searchInput"
          type="button"
          class="inline-flex items-center gap-1 text-xs text-slate-500 hover:text-slate-800 hover:underline cursor-pointer"
          data-testid="reset-filters-button"
          @click="resetFilters"
        >
          <RotateCcw class="w-3 h-3" />
          <span>Reset Filters</span>
        </button>
      </div>

      <!-- Row 2: Category Dropdown & Search Input -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 pt-1">
        <!-- Category Filter -->
        <div>
          <label class="block text-xs font-medium text-slate-600 mb-1">Policy Category</label>
          <Select
            v-model="selectedCategory"
            :options="categoryFilterOptions"
            option-label="label"
            option-value="value"
            placeholder="All Categories"
            :loading="isLoadingCategories"
            class="w-full !text-xs"
            data-testid="category-filter-select"
          />
        </div>

        <!-- Search Input -->
        <div class="sm:col-span-2">
          <label class="block text-xs font-medium text-slate-600 mb-1">Search Policies</label>
          <div class="relative flex items-center">
            <Search class="w-4 h-4 text-slate-400 absolute left-3 pointer-events-none" />
            <InputText
              v-model="searchInput"
              type="text"
              placeholder="Search by code, title, purpose, or context..."
              class="w-full !pl-9 !pr-8 !text-xs !py-2"
              data-testid="search-policies-input"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- 3. Master-Detail Layout Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
      <!-- ================================================================= -->
      <!-- Left Column: Policies Master Table (5 cols on lg)                 -->
      <!-- ================================================================= -->
      <div class="lg:col-span-5 space-y-4">
        <div
          class="bg-white rounded-xl border border-slate-200/80 shadow-2xs overflow-hidden"
          data-testid="policies-master-card"
        >
          <div class="p-3.5 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
            <div class="flex items-center gap-2">
              <Shield class="w-4 h-4 text-teal-700" />
              <h2 class="text-xs font-bold uppercase tracking-wider text-slate-700">
                Financial Policies ({{ filteredPolicies.length }})
              </h2>
            </div>
            <span class="text-[11px] text-slate-400">Click to inspect</span>
          </div>

          <!-- Loading State -->
          <div v-if="isLoadingPolicies" class="p-8 flex justify-center">
            <LoadingSpinner size="md" color="teal" />
          </div>

          <!-- Empty Policies State -->
          <div v-else-if="filteredPolicies.length === 0" class="p-6">
            <EmptyState
              title="No financial policies found"
              description="No policies match the current filter or search criteria."
              data-testid="policies-empty-state"
            >
              <template v-if="canManage || canApprove" #actions>
                <Button
                  size="small"
                  class="!text-xs cursor-pointer bg-teal-700 text-white mt-2"
                  @click="isCreatePolicyModalOpen = true"
                >
                  <PlusCircle class="w-3.5 h-3.5 mr-1" />
                  <span>Create Policy Draft</span>
                </Button>
              </template>
            </EmptyState>
          </div>

          <!-- Master Policies List -->
          <div v-else class="divide-y divide-slate-100 max-h-[640px] overflow-y-auto">
            <div
              v-for="policy in filteredPolicies"
              :key="policy.id"
              :class="[
                'p-3.5 transition-colors cursor-pointer select-none',
                selectedPolicyId === policy.id
                  ? 'bg-teal-50/80 border-l-4 border-l-teal-600'
                  : 'hover:bg-slate-50/80 border-l-4 border-l-transparent',
              ]"
              :data-testid="`policy-item-${policy.policyCode}`"
              @click="selectPolicy(policy)"
            >
              <div class="flex items-start justify-between gap-2">
                <div class="space-y-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="font-bold text-xs text-slate-900 truncate">
                      {{ policy.policyCode }}
                    </span>
                    <span class="text-[10px] font-semibold px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 border border-slate-200">
                      v{{ policy.policyVersion || '1.0' }}
                    </span>
                  </div>
                  <h3 class="text-xs font-semibold text-slate-800 line-clamp-1" :title="policy.name">
                    {{ policy.name }}
                  </h3>
                </div>

                <!-- Status Pill -->
                <StatusPill :status="policy.status" size="sm" />
              </div>

              <div class="flex items-center justify-between text-[11px] text-slate-500 mt-2.5 pt-2 border-t border-slate-100/60">
                <span class="text-teal-700 font-medium">
                  {{ categoryNameByCode[policy.category] || policy.category }}
                </span>
                <span class="inline-flex items-center gap-1">
                  <Calendar class="w-3 h-3 text-slate-400" />
                  <span>Effective {{ formatDisplayDate(policy.effectiveDate) }}</span>
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ================================================================= -->
      <!-- Right Column: Policy Detail & Charge Rules Table (7 cols on lg)   -->
      <!-- ================================================================= -->
      <div class="lg:col-span-7 space-y-4">
        <!-- Empty Selection -->
        <div
          v-if="!selectedPolicy"
          class="bg-white rounded-xl border border-slate-200/80 shadow-2xs p-8"
          data-testid="no-policy-selected"
        >
          <EmptyState
            title="No policy selected"
            description="Select a policy from the list on the left to inspect its governance parameters and attached charge rules."
          />
        </div>

        <!-- Detail Dossier Card -->
        <div
          v-else
          class="bg-white rounded-xl border border-slate-200/80 shadow-2xs overflow-hidden space-y-0"
          data-testid="policy-detail-card"
        >
          <!-- Dossier Header -->
          <div class="p-4 sm:p-5 border-b border-slate-100 bg-slate-50/50">
            <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
              <div class="space-y-1">
                <div class="flex items-center gap-2">
                  <span class="text-sm font-bold text-teal-900">
                    {{ selectedPolicy.policyCode }}
                  </span>
                  <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-slate-200 text-slate-700">
                    Version {{ selectedPolicy.policyVersion || '1.0' }}
                  </span>
                  <StatusPill :status="selectedPolicy.status" size="sm" />
                </div>
                <h2 class="text-base font-bold text-slate-900">
                  {{ selectedPolicy.name }}
                </h2>
              </div>

              <!-- Action Buttons -->
              <div class="flex items-center gap-2 self-start sm:self-auto shrink-0">
                <!-- Formal Approval Action Button (Finance Authority) -->
                <Button
                  v-if="selectedPolicy.status === 'draft' && canApprove"
                  size="small"
                  severity="success"
                  class="!text-xs cursor-pointer inline-flex items-center gap-1.5 bg-emerald-700 hover:bg-emerald-800 text-white font-medium"
                  data-testid="approve-policy-button"
                  @click="openApproveModal"
                >
                  <CheckCircle2 class="w-3.5 h-3.5" />
                  <span>Approve Policy</span>
                </Button>

                <!-- Add Charge Rule Action Button -->
                <Button
                  v-if="canManage || canApprove"
                  size="small"
                  severity="primary"
                  class="!text-xs cursor-pointer inline-flex items-center gap-1.5 bg-teal-700 hover:bg-teal-800 text-white font-medium"
                  data-testid="add-charge-rule-button"
                  @click="isCreateChargeRuleModalOpen = true"
                >
                  <PlusCircle class="w-3.5 h-3.5" />
                  <span>Add Charge Rule</span>
                </Button>
              </div>
            </div>

            <!-- Approval Badge Banner if Active -->
            <div
              v-if="selectedPolicy.status === 'active' && selectedPolicy.approvedDate"
              class="mt-3 p-2.5 rounded-lg border border-emerald-200 bg-emerald-50 text-emerald-800 text-xs flex items-center justify-between"
              data-testid="active-approval-banner"
            >
              <div class="flex items-center gap-2">
                <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Formally approved and active for billing cycle assessment.</span>
              </div>
              <span class="font-medium">
                {{ selectedPolicy.approvingAuthority || 'Finance Authority' }} ({{ formatDisplayDate(selectedPolicy.approvedDate) }})
              </span>
            </div>
          </div>

          <!-- Dossier Metadata Grid -->
          <div class="p-4 sm:p-5 border-b border-slate-100 grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
            <div>
              <span class="text-slate-400 block mb-0.5">Category</span>
              <span class="font-semibold text-slate-800">
                {{ categoryNameByCode[selectedPolicy.category] || selectedPolicy.category }}
              </span>
            </div>

            <div>
              <span class="text-slate-400 block mb-0.5">Effective Validity Range</span>
              <span class="font-semibold text-slate-800">
                {{ formatDisplayDate(selectedPolicy.effectiveDate) }}
                {{ selectedPolicy.endDate ? `— ${formatDisplayDate(selectedPolicy.endDate)}` : '(Indefinite)' }}
              </span>
            </div>

            <div>
              <span class="text-slate-400 block mb-0.5">Applicable Institutional Context</span>
              <span class="font-semibold text-slate-800">
                {{ selectedPolicy.applicableContext || 'All Campuses & School Levels' }}
              </span>
            </div>

            <div>
              <span class="text-slate-400 block mb-0.5">Created At</span>
              <span class="font-semibold text-slate-800">
                {{ formatDisplayDate(selectedPolicy.createdAt) }}
                <span v-if="selectedPolicy.createdBy" class="text-slate-500 font-normal">by {{ selectedPolicy.createdBy }}</span>
              </span>
            </div>

            <div class="sm:col-span-2">
              <span class="text-slate-400 block mb-0.5">Business Purpose</span>
              <p class="text-slate-700 leading-relaxed bg-slate-50/80 p-3 rounded-lg border border-slate-200/60">
                {{ selectedPolicy.businessPurpose || 'No business purpose recorded.' }}
              </p>
            </div>

            <div v-if="selectedPolicy.notes" class="sm:col-span-2">
              <span class="text-slate-400 block mb-0.5">Internal Governance Notes</span>
              <p class="text-slate-600 italic">
                {{ selectedPolicy.notes }}
              </p>
            </div>
          </div>

          <!-- Attached Charge Rules Table Section -->
          <div class="p-4 sm:p-5 space-y-3" data-testid="attached-charge-rules-section">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <Layers class="w-4 h-4 text-teal-700" />
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700">
                  Attached Charge Rules ({{ attachedChargeRules.length }})
                </h3>
              </div>

              <Button
                v-if="canManage || canApprove"
                size="small"
                severity="secondary"
                variant="outlined"
                class="!text-xs !py-1 cursor-pointer inline-flex items-center gap-1"
                data-testid="header-add-rule-btn"
                @click="isCreateChargeRuleModalOpen = true"
              >
                <PlusCircle class="w-3.5 h-3.5" />
                <span>Add Rule</span>
              </Button>
            </div>

            <!-- Loading Detail State -->
            <div v-if="isLoadingDetail" class="p-6 flex justify-center">
              <LoadingSpinner size="sm" color="teal" />
            </div>

            <!-- Empty Rules State -->
            <div v-else-if="attachedChargeRules.length === 0" class="py-6">
              <EmptyState
                title="No charge rules defined"
                description="This policy does not have any charge rules attached yet. Add concrete rules to govern fee calculations."
                data-testid="rules-empty-state"
              >
                <template v-if="canManage || canApprove" #actions>
                  <Button
                    size="small"
                    class="!text-xs cursor-pointer bg-teal-700 text-white mt-2"
                    @click="isCreateChargeRuleModalOpen = true"
                  >
                    <PlusCircle class="w-3.5 h-3.5 mr-1" />
                    <span>Add First Charge Rule</span>
                  </Button>
                </template>
              </EmptyState>
            </div>

            <!-- Charge Rules DataTable -->
            <div v-else class="border border-slate-200/80 rounded-lg overflow-hidden">
              <DataTable
                :value="attachedChargeRules"
                responsive-layout="scroll"
                striped-rows
                data-key="id"
                class="text-xs"
                data-testid="charge-rules-datatable"
              >
                <Column field="ruleCode" header="Rule Code" sortable>
                  <template #body="{ data }">
                    <span class="font-bold text-slate-900 font-mono text-[11px]">
                      {{ data.ruleCode }}
                    </span>
                  </template>
                </Column>

                <Column field="name" header="Name" sortable>
                  <template #body="{ data }">
                    <div class="space-y-0.5">
                      <span class="font-semibold text-slate-800">{{ data.name }}</span>
                      <div class="text-[10px] text-slate-400 capitalize">Type: {{ data.chargeType }}</div>
                    </div>
                  </template>
                </Column>

                <Column field="baseAmount" header="Base Amount" sortable>
                  <template #body="{ data }">
                    <span class="font-bold text-teal-800">
                      {{ formatIdr(data.baseAmount) }}
                    </span>
                  </template>
                </Column>

                <Column field="recurrencePattern" header="Recurrence">
                  <template #body="{ data }">
                    <span class="capitalize text-slate-600 bg-slate-100 px-2 py-0.5 rounded text-[11px]">
                      {{ data.recurrencePattern }}
                    </span>
                  </template>
                </Column>

                <Column field="dueRuleType" header="Due Rule">
                  <template #body="{ data }">
                    <div class="text-[11px] text-slate-700 space-y-0.5">
                      <span class="font-medium">{{ data.dueRuleType }}</span>
                      <span class="text-slate-500 block text-[10px]">Val: {{ data.dueRuleValue }}</span>
                    </div>
                  </template>
                </Column>

                <Column header="Applicability">
                  <template #body="{ data }">
                    <div class="text-[10px] text-slate-600 space-y-0.5">
                      <div>Grade: <span class="font-semibold">{{ data.applicableGrade || 'All' }}</span></div>
                      <div>Program: <span class="font-semibold">{{ data.applicableProgram || 'All' }}</span></div>
                    </div>
                  </template>
                </Column>

                <Column field="status" header="Status">
                  <template #body="{ data }">
                    <StatusPill :status="data.status" size="sm" />
                  </template>
                </Column>
              </DataTable>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- =================================================================== -->
    <!-- 4. Modals                                                           -->
    <!-- =================================================================== -->

    <!-- Create Policy Modal -->
    <CreatePolicyModal
      v-model:visible="isCreatePolicyModalOpen"
      @created="onPolicyCreated"
    />

    <!-- Create Charge Rule Modal -->
    <CreateChargeRuleModal
      v-if="selectedPolicy"
      v-model:visible="isCreateChargeRuleModalOpen"
      :policy-id="selectedPolicy.id"
      :policy-code="selectedPolicy.policyCode"
      :policy-name="selectedPolicy.name"
      @created="onRuleCreated"
    />

    <!-- Formal Policy Approval Confirmation Dialog (Finance Authority) -->
    <Dialog
      v-model:visible="isApproveModalOpen"
      modal
      header="Approve Financial Policy"
      :style="{ width: '500px' }"
      :closable="!approveMutation.isPending.value"
      class="p-dialog-sm"
      data-testid="approve-policy-dialog"
    >
      <div v-if="selectedPolicy" class="space-y-4 pt-2">
        <!-- Notice Banner -->
        <div class="p-3.5 rounded-lg border border-amber-200 bg-amber-50/70 text-xs text-amber-900 space-y-1">
          <div class="flex items-center gap-1.5 font-bold text-amber-950">
            <AlertCircle class="w-4 h-4 text-amber-600 shrink-0" />
            <span>Formal Governance Approval</span>
          </div>
          <p>
            You are approving policy <span class="font-bold text-slate-900">{{ selectedPolicy.policyCode }}</span> ({{ selectedPolicy.name }}). Approving transitions this policy to <span class="font-bold text-emerald-800">Active</span> status and authorizes attached charge rules for fee assessments and billing cycles.
          </p>
        </div>

        <!-- Approving Authority Input -->
        <FormField
          label="Approving Authority Title / Actor"
          required
          help-text="Official authority recorded in the audit journal"
        >
          <InputText
            id="approving-authority-input"
            v-model="approvingAuthorityInput"
            type="text"
            placeholder="e.g. Finance Authority, Direktur Keuangan"
            class="w-full !text-xs !py-2"
            data-testid="approving-authority-input"
          />
        </FormField>

        <!-- Approval Notes Input -->
        <FormField
          label="Approval Notes / Decrees"
          optional
          help-text="Optional reference decree or approval meeting minutes"
        >
          <Textarea
            id="approval-notes-input"
            v-model="approvalNotesInput"
            rows="2"
            placeholder="e.g. SK Direktur Keuangan No. 042/Elsasa/2026"
            class="w-full !text-xs"
            data-testid="approval-notes-input"
          />
        </FormField>

        <!-- Dialog Action Buttons -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
          <Button
            type="button"
            size="small"
            severity="secondary"
            variant="outlined"
            :disabled="approveMutation.isPending.value"
            class="!text-xs cursor-pointer"
            data-testid="cancel-approval-button"
            @click="isApproveModalOpen = false"
          >
            Cancel
          </Button>

          <Button
            type="button"
            size="small"
            severity="success"
            :loading="approveMutation.isPending.value"
            class="!text-xs cursor-pointer bg-emerald-700 hover:bg-emerald-800 text-white font-medium inline-flex items-center gap-1.5"
            data-testid="confirm-approval-button"
            @click="handleConfirmApprove"
          >
            <CheckCircle2 class="w-3.5 h-3.5" />
            <span>Confirm & Approve</span>
          </Button>
        </div>
      </div>
    </Dialog>
  </div>
</template>
