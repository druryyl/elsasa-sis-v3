<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import {
  Search,
  Plus,
  CheckCircle2,
  HelpCircle,
  XCircle,
  UserCheck,
  Building2,
  Calendar,
  Clock,
  Layers,
  RotateCcw,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import { useDemands, useUpdateDemandStatusMutation } from '../composables/useAssignments';
import CreateDemandModal from './CreateDemandModal.vue';
import DeclineDemandModal from './DeclineDemandModal.vue';
import RequestClarificationModal from './RequestClarificationModal.vue';
import { DEMAND_STATUS_MAP } from '../constants/assignmentWorkbenchConstants';
import type { TeachingDemandDto, TeachingDemandStatus } from '../types';

export interface DemandsIntakeTabProps {
  academicPeriodId?: string;
  campusScope?: string;
  search?: string;
  canManage?: boolean;
  initialStatus?: string;
  initialCapability?: string;
}

const props = withDefaults(defineProps<DemandsIntakeTabProps>(), {
  academicPeriodId: undefined,
  campusScope: undefined,
  search: '',
  canManage: true,
  initialStatus: 'all',
  initialCapability: '',
});

const emit = defineEmits<{
  (e: 'evaluateDemand', demand: TeachingDemandDto): void;
}>();

// Filters state
const searchQuery = ref<string>('');
const selectedStatus = ref<string>(props.initialStatus || 'all');
const selectedCapability = ref<string>(props.initialCapability || '');

watch(
  () => props.initialStatus,
  (val) => {
    if (val) selectedStatus.value = val;
  },
  { immediate: true },
);

watch(
  () => props.initialCapability,
  (val) => {
    selectedCapability.value = val ?? '';
  },
  { immediate: true },
);

const statusFilterOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Unstaffed (Open)', value: 'unstaffed' },
  { label: 'Received (Awaiting Action)', value: 'received' },
  { label: 'Accepted for Staffing', value: 'accepted_for_staffing' },
  { label: 'Clarification Needed', value: 'returned_for_clarification' },
  { label: 'Staffed', value: 'staffed' },
  { label: 'Declined', value: 'declined' },
];

// Server query
const queryParams = computed(() => ({
  academicPeriodId: props.academicPeriodId,
  campusScope: props.campusScope && props.campusScope !== 'all' ? props.campusScope : undefined,
  status:
    selectedStatus.value !== 'all' && selectedStatus.value !== 'unstaffed'
      ? selectedStatus.value
      : undefined,
  search: searchQuery.value.trim() || (props.search ? props.search.trim() : undefined),
}));

const {
  data: demandsData,
  isLoading,
  isError,
  refetch,
} = useDemands(queryParams);

const unstaffedStatuses = new Set(['received', 'accepted_for_staffing', 'changed']);

const demands = computed<TeachingDemandDto[]>(() => {
  if (!demandsData.value) return [];
  const raw = Array.isArray(demandsData.value)
    ? demandsData.value
    : (demandsData.value as { items?: TeachingDemandDto[] }).items || [];

  return raw.filter((d) => {
    if (selectedStatus.value === 'unstaffed' && !unstaffedStatuses.has(d.status)) {
      return false;
    }
    if (selectedCapability.value.trim()) {
      const cap = selectedCapability.value.trim().toLowerCase();
      const codeMatch = d.requiredCapabilityCode?.toLowerCase() === cap;
      const nameMatch = d.requiredCapabilityName?.toLowerCase().includes(cap);
      if (!codeMatch && !nameMatch) {
        return false;
      }
    }
    return true;
  });
});

// Modal state
const isCreateModalVisible = ref<boolean>(false);
const isDeclineModalVisible = ref<boolean>(false);
const isClarificationModalVisible = ref<boolean>(false);
const selectedDemandForAction = ref<TeachingDemandDto | null>(null);

// Quick accept action
const updateStatusMutation = useUpdateDemandStatusMutation();

function handleAcceptForStaffing(demand: TeachingDemandDto) {
  updateStatusMutation.mutate({
    id: demand.id,
    payload: {
      status: 'accepted_for_staffing',
    },
  });
}

function handleOpenDecline(demand: TeachingDemandDto) {
  selectedDemandForAction.value = demand;
  isDeclineModalVisible.value = true;
}

function handleOpenClarification(demand: TeachingDemandDto) {
  selectedDemandForAction.value = demand;
  isClarificationModalVisible.value = true;
}

function handleEvaluate(demand: TeachingDemandDto) {
  emit('evaluateDemand', demand);
}

function getDemandStatusMeta(status: string) {
  return DEMAND_STATUS_MAP[status as TeachingDemandStatus] || {
    label: status,
    variant: 'slate' as const,
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-200',
    description: '',
  };
}
</script>

<template>
  <div class="space-y-4" data-testid="demands-intake-tab">
    <!-- Filters & Action Bar -->
    <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-surface-50 dark:bg-surface-800/40 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700">
      <div class="flex flex-wrap items-center gap-2.5 flex-1">
        <!-- Search Input -->
        <div class="relative w-full sm:w-64">
          <Search class="w-4 h-4 text-surface-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <InputText
            v-model="searchQuery"
            placeholder="Search demand code, subject..."
            class="w-full !pl-9 !text-xs !py-2"
            data-testid="demands-search-input"
          />
        </div>

        <!-- Status Filter -->
        <Select
          v-model="selectedStatus"
          :options="statusFilterOptions"
          option-label="label"
          option-value="value"
          class="!text-xs w-full sm:w-56"
          data-testid="demands-status-filter"
        />

        <Button
          severity="secondary"
          variant="outlined"
          size="small"
          class="!text-xs !py-1.5"
          data-testid="demands-refresh-btn"
          @click="() => refetch()"
        >
          <RotateCcw class="w-3.5 h-3.5 mr-1" />
          Refresh
        </Button>

        <Button
          v-if="selectedCapability"
          severity="info"
          variant="outlined"
          size="small"
          class="!text-xs !py-1.5"
          data-testid="btn-clear-capability-filter"
          @click="selectedCapability = ''"
        >
          <span>Capability: {{ selectedCapability }}</span>
          <XCircle class="w-3.5 h-3.5 ml-1.5" />
        </Button>
      </div>

      <!-- Action Button -->
      <div class="flex items-center gap-2 shrink-0">
        <Button
          v-if="canManage"
          severity="primary"
          size="small"
          class="!text-xs font-medium !py-2 !px-3"
          data-testid="btn-open-create-demand"
          @click="isCreateModalVisible = true"
        >
          <Plus class="w-4 h-4 mr-1.5" />
          Ingest Demand
        </Button>
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="isLoading"
      class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-16 flex flex-col items-center justify-center space-y-3 shadow-xs min-h-[300px]"
      data-testid="demands-loading"
    >
      <LoadingSpinner size="lg" color="teal" />
      <p class="text-sm font-medium text-surface-600 dark:text-surface-300">
        Loading teaching capacity demands...
      </p>
    </div>

    <!-- Error State -->
    <div
      v-else-if="isError"
      class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs"
      data-testid="demands-error"
    >
      <EmptyState
        title="Unable to load demands"
        description="Could not fetch teaching capacity requests. Please check your connectivity and try again."
      >
        <template #actions>
          <Button
            severity="danger"
            size="small"
            class="!text-xs !py-1.5 !px-3"
            @click="() => refetch()"
          >
            <RotateCcw class="w-4 h-4 mr-1" />
            Retry
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="demands.length === 0"
      class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs"
      data-testid="demands-empty"
    >
      <EmptyState
        title="No Teaching Demands Found"
        description="There are currently no capacity demands matching your period, campus, or status filters."
      >
        <template #actions>
          <Button
            v-if="canManage"
            severity="primary"
            size="small"
            class="!text-xs !py-1.5 !px-3"
            @click="isCreateModalVisible = true"
          >
            <Plus class="w-4 h-4 mr-1" />
            Create First Demand
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- Demands Data Table -->
    <div
      v-else
      class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden"
      data-testid="demands-table-wrapper"
    >
      <DataTable
        :value="demands"
        responsive-layout="scroll"
        class="text-xs"
        data-testid="demands-table"
      >
        <!-- Demand Code & Origin -->
        <Column header="Demand Code" style="min-width: 140px">
          <template #body="{ data }">
            <div class="font-mono font-medium text-surface-900 dark:text-surface-100 flex items-center gap-1.5">
              <Layers class="w-3.5 h-3.5 text-primary-500 shrink-0" />
              <span>{{ data.demandCode }}</span>
            </div>
            <div class="text-[11px] text-surface-500 dark:text-surface-400 capitalize">
              From: {{ data.sourceDomain }}
            </div>
          </template>
        </Column>

        <!-- Work Label & Subject -->
        <Column header="Subject / Scope" style="min-width: 180px">
          <template #body="{ data }">
            <div class="font-medium text-surface-900 dark:text-surface-100">
              {{ data.sourceWorkLabel }}
            </div>
            <div class="text-[11px] text-surface-500 dark:text-surface-400 flex items-center gap-2 mt-0.5">
              <span class="inline-flex items-center gap-1">
                <Building2 class="w-3 h-3" />
                {{ data.campusScope }}
              </span>
              <span class="font-mono text-[10px] bg-surface-100 dark:bg-surface-800 px-1 py-0.5 rounded">
                {{ data.requiredCapabilityCode }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Effort & Schedule -->
        <Column header="Workload Effort" style="min-width: 130px">
          <template #body="{ data }">
            <div class="flex items-center gap-1 text-surface-800 dark:text-surface-200 font-medium">
              <Clock class="w-3.5 h-3.5 text-amber-500" />
              <span>{{ data.expectedWorkloadHours }} {{ data.workloadUnit || 'Hours' }}</span>
            </div>
            <div class="text-[11px] text-surface-500 dark:text-surface-400 flex items-center gap-1 mt-0.5">
              <Calendar class="w-3 h-3" />
              <span>{{ data.requestedStartDate }}</span>
            </div>
          </template>
        </Column>

        <!-- Status -->
        <Column header="Status" style="min-width: 140px">
          <template #body="{ data }">
            <StatusPill
              :status="data.status"
              :label="getDemandStatusMeta(data.status).label"
              :variant="getDemandStatusMeta(data.status).variant"
              size="sm"
            />
            <p v-if="data.declineReason" class="text-[10px] text-rose-600 dark:text-rose-400 mt-1 line-clamp-1" :title="data.declineReason">
              Reason: {{ data.declineReason }}
            </p>
            <p v-else-if="data.clarificationNotes" class="text-[10px] text-amber-600 dark:text-amber-400 mt-1 line-clamp-1" :title="data.clarificationNotes">
              Note: {{ data.clarificationNotes }}
            </p>
          </template>
        </Column>

        <!-- Actions -->
        <Column header="Actions" align-frozen="right" style="min-width: 220px">
          <template #body="{ data }">
            <div class="flex items-center gap-1.5 flex-wrap">
              <!-- When received: Accept, Clarify, Decline -->
              <template v-if="data.status === 'received' && canManage">
                <Button
                  severity="success"
                  size="small"
                  class="!text-[11px] !py-1 !px-2 font-medium"
                  :loading="updateStatusMutation.isPending.value"
                  data-testid="btn-accept-demand"
                  @click="handleAcceptForStaffing(data)"
                >
                  <CheckCircle2 class="w-3.5 h-3.5 mr-1" />
                  Accept
                </Button>
                <Button
                  severity="warn"
                  variant="outlined"
                  size="small"
                  class="!text-[11px] !py-1 !px-2"
                  data-testid="btn-clarify-demand"
                  @click="handleOpenClarification(data)"
                >
                  <HelpCircle class="w-3.5 h-3.5 mr-1" />
                  Clarify
                </Button>
                <Button
                  severity="danger"
                  variant="outlined"
                  size="small"
                  class="!text-[11px] !py-1 !px-2"
                  data-testid="btn-decline-demand"
                  @click="handleOpenDecline(data)"
                >
                  <XCircle class="w-3.5 h-3.5 mr-1" />
                  Decline
                </Button>
              </template>

              <!-- When accepted_for_staffing: Shortcut to evaluate candidates -->
              <template v-else-if="data.status === 'accepted_for_staffing'">
                <Button
                  severity="primary"
                  size="small"
                  class="!text-[11px] !py-1 !px-2.5 font-medium"
                  data-testid="btn-evaluate-candidates"
                  @click="handleEvaluate(data)"
                >
                  <UserCheck class="w-3.5 h-3.5 mr-1" />
                  Evaluate Candidates
                </Button>
                <Button
                  severity="danger"
                  variant="text"
                  size="small"
                  class="!text-[11px] !py-1 !px-1.5 text-rose-500"
                  data-testid="btn-decline-accepted-demand"
                  @click="handleOpenDecline(data)"
                >
                  Decline
                </Button>
              </template>

              <!-- Other statuses -->
              <template v-else>
                <Button
                  severity="secondary"
                  variant="text"
                  size="small"
                  class="!text-[11px] !py-1 !px-2"
                  data-testid="btn-inspect-demand"
                  @click="handleEvaluate(data)"
                >
                  Inspect
                </Button>
              </template>
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Modals -->
    <CreateDemandModal
      v-model:visible="isCreateModalVisible"
      :academic-period-id="academicPeriodId"
      :campus-scope="campusScope"
      @submitted="() => refetch()"
    />

    <DeclineDemandModal
      v-model:visible="isDeclineModalVisible"
      :demand="selectedDemandForAction"
      @submitted="() => refetch()"
    />

    <RequestClarificationModal
      v-model:visible="isClarificationModalVisible"
      :demand="selectedDemandForAction"
      @submitted="() => refetch()"
    />
  </div>
</template>
