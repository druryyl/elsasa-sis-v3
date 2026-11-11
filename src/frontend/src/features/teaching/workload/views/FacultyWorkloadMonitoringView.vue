<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import Select from 'primevue/select';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import {
  RotateCcw,
  Sparkles,
  ShieldAlert,
  Info,
  CalendarDays,
  FileCheck2,
  AlertTriangle,
  Users,
} from 'lucide-vue-next';
import { PageHeader, StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  useCapacityConflicts,
  useWorkloadCommitments,
  useWorkloadPolicies,
  useRecalculateWorkloadMutation,
} from '../composables/useWorkload';
import WorkloadKpiCards from '../components/WorkloadKpiCards.vue';
import FacultyWorkloadTable from '../components/FacultyWorkloadTable.vue';
import WorkloadDrillDownDrawer from '../components/WorkloadDrillDownDrawer.vue';
import RequestExceptionModal from '../components/RequestExceptionModal.vue';
import ApproveExceptionModal from '../components/ApproveExceptionModal.vue';
import {
  PERIOD_PRESETS,
  WORKLOAD_PERMISSIONS,
  type PeriodOption,
} from '../components/workloadConstants';
import type {
  CapacityConflictDto,
  TeacherWorkloadCommitmentDto,
  WorkloadCommitmentStatus,
  WorkloadPolicyDto,
} from '../types';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Teaching', to: '/teaching/faculty' },
  { label: 'Workload Monitoring', current: true },
];

// ---------------------------------------------------------------------------
// 2. Period Selection & Route Sync
// ---------------------------------------------------------------------------
const selectedPeriodId = ref<string>(
  typeof route.query.periodId === 'string' && route.query.periodId.trim()
    ? route.query.periodId.trim()
    : PERIOD_PRESETS[0].id,
);

function handlePeriodChange(periodId: string) {
  selectedPeriodId.value = periodId;
  router.replace({
    query: {
      ...route.query,
      periodId,
    },
  });
}

// ---------------------------------------------------------------------------
// 3. Permissions Evaluation
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canRecalculate = computed(
  () =>
    authStore.hasPermission(WORKLOAD_PERMISSIONS.RECALCULATE) ||
    authStore.hasAnyRole(['SuperAdmin', 'AcademicDirector', 'TeachingCoordinator']),
);

const canRequestException = computed(
  () =>
    authStore.hasPermission(WORKLOAD_PERMISSIONS.REQUEST_EXCEPTION) ||
    authStore.hasAnyRole(['SuperAdmin', 'AcademicDirector', 'TeachingCoordinator']),
);

const canApproveException = computed(
  () =>
    authStore.hasPermission(WORKLOAD_PERMISSIONS.APPROVE_EXCEPTION) ||
    authStore.hasAnyRole(['SuperAdmin', 'AcademicDirector', 'SchoolPrincipal']),
);

// ---------------------------------------------------------------------------
// 4. Server Queries (Policies & Commitments)
// ---------------------------------------------------------------------------
const {
  data: policiesData,
  isLoading: isLoadingPolicies,
} = useWorkloadPolicies({ status: 'active' });

const activePolicy = computed<WorkloadPolicyDto | null>(() => {
  if (!policiesData.value || policiesData.value.length === 0) return null;
  return policiesData.value[0];
});

const {
  data: commitmentsData,
  isLoading: isLoadingCommitments,
  isError: isCommitmentsError,
  error: commitmentsErrorObj,
  refetch: refetchCommitments,
} = useWorkloadCommitments(
  computed(() => ({
    academicPeriodId: selectedPeriodId.value,
  })),
);

const commitments = computed<TeacherWorkloadCommitmentDto[]>(
  () => commitmentsData.value ?? [],
);

const commitmentsErrorMessage = computed(() => {
  if (!commitmentsErrorObj.value) return null;
  return (commitmentsErrorObj.value as Error).message || 'Failed to load workload commitments.';
});

// ---------------------------------------------------------------------------
// 5. Filter & Tab State + Route Query Hydration
// ---------------------------------------------------------------------------
const activeTab = ref<'workload' | 'conflicts'>('workload');
const selectedStatusFilter = ref<string>('all');
const conflictSeverityFilter = ref<string>('all');

const severityFilterOptions = [
  { label: 'All Severities', value: 'all' },
  { label: 'Blocking Only', value: 'blocking' },
  { label: 'Advisory Only', value: 'advisory' },
];

useRouteQueryHydration((query) => {
  const periodId = getQueryString(query, 'periodId');
  if (periodId) {
    selectedPeriodId.value = periodId;
  }

  const status = getQueryString(query, 'status');
  if (status) {
    selectedStatusFilter.value = status;
  }

  const tab = getQueryString(query, 'tab');
  if (tab === 'conflicts' || tab === 'workload') {
    activeTab.value = tab;
  }

  const severity = getQueryString(query, 'severity')?.toLowerCase();
  if (severity === 'blocking' || severity === 'advisory' || severity === 'all') {
    conflictSeverityFilter.value = severity;
    if (!tab && severity !== 'all') {
      activeTab.value = 'conflicts';
    }
  }
});

// Capacity Conflicts Query (enabled only when Conflicts tab is active)
const {
  data: conflictsResponse,
  isLoading: isLoadingConflicts,
  refetch: refetchConflicts,
} = useCapacityConflicts(
  computed(() => ({
    status: 'unresolved',
    severity:
      conflictSeverityFilter.value !== 'all' ? conflictSeverityFilter.value : undefined,
    page: 1,
    pageSize: 100,
  })),
  { enabled: computed(() => activeTab.value === 'conflicts') },
);

const capacityConflicts = computed<CapacityConflictDto[]>(() => {
  const raw = conflictsResponse.value;
  if (!raw) return [];
  const list = Array.isArray(raw) ? raw : (raw.items ?? []);
  if (conflictSeverityFilter.value === 'all') return list;
  return list.filter(
    (c) => c.severity?.toLowerCase() === conflictSeverityFilter.value.toLowerCase(),
  );
});

function handleKpiCardFilter(status: WorkloadCommitmentStatus | 'all') {
  selectedStatusFilter.value = status;
}

// ---------------------------------------------------------------------------
// 6. Recalculate Period Workload Mutation
// ---------------------------------------------------------------------------
const { mutate: recalculatePeriodWorkload, isPending: isRecalculating } =
  useRecalculateWorkloadMutation({
    onSuccess: () => {
      refetchCommitments();
    },
  });

function handleRecalculate() {
  if (!selectedPeriodId.value || isRecalculating.value) return;
  recalculatePeriodWorkload(selectedPeriodId.value);
}

// ---------------------------------------------------------------------------
// 7. Modals & Drawer State
// ---------------------------------------------------------------------------
const isDrillDownOpen = ref<boolean>(false);
const isRequestModalOpen = ref<boolean>(false);
const isApproveModalOpen = ref<boolean>(false);

const activeSelectedTeacher = ref<TeacherWorkloadCommitmentDto | null>(null);

function openDrillDown(item: TeacherWorkloadCommitmentDto) {
  activeSelectedTeacher.value = item;
  isDrillDownOpen.value = true;
}

function openRequestException(item: TeacherWorkloadCommitmentDto) {
  activeSelectedTeacher.value = item;
  isRequestModalOpen.value = true;
}

function openApproveException(item: TeacherWorkloadCommitmentDto) {
  activeSelectedTeacher.value = item;
  isApproveModalOpen.value = true;
}

function handleExceptionSubmitted() {
  refetchCommitments();
}

function handleExceptionApproved() {
  refetchCommitments();
}
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6" data-testid="faculty-workload-workbench">
    <!-- 1. Page Header with Title & Breadcrumbs -->
    <PageHeader
      title="Faculty Workload Monitoring"
      subtitle="Institutional faculty capacity management, weekly teaching effort monitoring, and exception governance."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <!-- On-Demand Recalculate Button -->
        <Button
          type="button"
          severity="primary"
          size="small"
          :loading="isRecalculating"
          :disabled="!canRecalculate || isRecalculating"
          data-testid="recalculate-workload-btn"
          @click="handleRecalculate"
        >
          <RotateCcw
            class="w-4 h-4 mr-1.5"
            :class="{ 'animate-spin': isRecalculating }"
          />
          <span>Recalculate Workload</span>
        </Button>
      </template>
    </PageHeader>

    <!-- 2. Academic Period & Active Workload Policy Bar -->
    <div
      class="bg-white rounded-xl border border-slate-200 p-4 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4"
    >
      <!-- Academic Period Selector -->
      <div class="flex items-center gap-3 w-full md:w-auto">
        <div class="p-2 bg-slate-100 text-slate-600 rounded-lg shrink-0">
          <CalendarDays class="w-5 h-5" />
        </div>
        <div class="flex-1 md:w-80">
          <label class="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
            Academic Period
          </label>
          <Select
            v-model="selectedPeriodId"
            :options="PERIOD_PRESETS"
            option-label="label"
            option-value="id"
            size="small"
            class="w-full"
            data-testid="period-selector-dropdown"
            @update:model-value="handlePeriodChange"
          />
        </div>
      </div>

      <!-- Active Policy Summary Chip -->
      <div class="w-full md:w-auto flex items-center gap-3">
        <div
          v-if="activePolicy"
          class="flex items-center gap-2.5 px-3 py-2 bg-indigo-50/70 border border-indigo-200 rounded-xl text-xs text-indigo-900 w-full md:w-auto"
          data-testid="active-policy-chip"
        >
          <Sparkles class="w-4 h-4 text-indigo-600 shrink-0" />
          <div class="space-y-0.5">
            <div class="font-bold flex items-center gap-2">
              <span>{{ activePolicy.name }}</span>
              <span class="font-mono text-[11px] bg-indigo-100 text-indigo-700 px-1 rounded">
                {{ activePolicy.code }}
              </span>
            </div>
            <div class="text-[11px] text-indigo-700/90 flex items-center gap-2">
              <span>Standard: <strong>{{ activePolicy.maxStandardCapacity }}h</strong></span>
              <span>&bull;</span>
              <span>Warning: <strong>{{ activePolicy.warningThreshold }}h</strong></span>
              <span>&bull;</span>
              <span>Ceiling: <strong>{{ activePolicy.prohibitedThreshold }}h</strong></span>
            </div>
          </div>
        </div>

        <div
          v-else-if="isLoadingPolicies"
          class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-500 animate-pulse"
        >
          Loading institutional policy...
        </div>

        <div
          v-else
          class="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-600 flex items-center gap-2"
        >
          <Info class="w-4 h-4 text-slate-400" />
          <span>Standard Baseline Policy (24h limit)</span>
        </div>
      </div>
    </div>

    <!-- 3. Tabs: Workload Commitments & Capacity Conflicts -->
    <div class="space-y-4" data-testid="workload-monitoring-tabs">
      <div
        role="tablist"
        aria-label="Workload Monitoring Sections"
        class="inline-flex rounded-xl bg-slate-100 p-1 border border-slate-200 text-xs font-semibold"
      >
        <button
          type="button"
          role="tab"
          :aria-selected="activeTab === 'workload'"
          class="px-4 py-2 rounded-lg transition-all flex items-center gap-2 cursor-pointer"
          :class="
            activeTab === 'workload'
              ? 'bg-white text-indigo-700 shadow-2xs'
              : 'text-slate-600 hover:text-slate-900'
          "
          data-testid="tab-workload-commitments"
          @click="activeTab = 'workload'"
        >
          <Users class="w-4 h-4" />
          <span>Workload Commitments</span>
        </button>
        <button
          type="button"
          role="tab"
          :aria-selected="activeTab === 'conflicts'"
          class="px-4 py-2 rounded-lg transition-all flex items-center gap-2 cursor-pointer"
          :class="
            activeTab === 'conflicts'
              ? 'bg-white text-rose-700 shadow-2xs'
              : 'text-slate-600 hover:text-slate-900'
          "
          data-testid="tab-capacity-conflicts"
          @click="activeTab = 'conflicts'"
        >
          <AlertTriangle class="w-4 h-4" />
          <span>Capacity & Qualification Conflicts</span>
        </button>
      </div>

      <div
        v-if="activeTab === 'workload'"
        role="tabpanel"
        data-testid="panel-workload-commitments"
        class="space-y-6"
      >
        <!-- 3. KPI Threshold Cards -->
        <WorkloadKpiCards
          :commitments="commitments"
          :selected-status="selectedStatusFilter"
          :is-loading="isLoadingCommitments"
          @select-status="handleKpiCardFilter"
        />

        <!-- 4. Faculty Workload Table -->
        <FacultyWorkloadTable
          :commitments="commitments"
          :is-loading="isLoadingCommitments"
          :is-error="isCommitmentsError"
          :error-message="commitmentsErrorMessage"
          :selected-status="selectedStatusFilter"
          :can-request-exception="canRequestException"
          :can-approve-exception="canApproveException"
          @drill-down="openDrillDown"
          @request-exception="openRequestException"
          @approve-exception="openApproveException"
          @update:selected-status="(status) => (selectedStatusFilter = status)"
          @refresh="() => refetchCommitments()"
        />
      </div>

      <div
        v-else-if="activeTab === 'conflicts'"
        role="tabpanel"
        data-testid="panel-capacity-conflicts"
      >
        <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
          <div class="px-5 py-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 class="text-sm font-bold text-slate-900">Unresolved Capacity Conflicts</h3>
              <p class="text-xs text-slate-500">
                Review blocking and advisory faculty capacity conflicts detected during assignment evaluation.
              </p>
            </div>
            <div class="flex items-center gap-2">
              <Select
                v-model="conflictSeverityFilter"
                :options="severityFilterOptions"
                option-label="label"
                option-value="value"
                size="small"
                class="w-44 text-xs"
                data-testid="select-conflict-severity"
              />
              <Button
                size="small"
                severity="secondary"
                outlined
                class="!text-xs"
                data-testid="refresh-conflicts-btn"
                @click="() => refetchConflicts()"
              >
                <RotateCcw class="w-3.5 h-3.5 mr-1" />
                Refresh
              </Button>
            </div>
          </div>

          <div v-if="isLoadingConflicts" class="py-12 flex justify-center">
            <LoadingSpinner label="Loading capacity conflicts..." />
          </div>

          <DataTable
            v-else
            :value="capacityConflicts"
            data-key="id"
            responsive-layout="scroll"
            class="text-xs"
            data-testid="capacity-conflicts-table"
          >
            <template #empty>
              <div class="py-10">
                <EmptyState
                  title="No Capacity Conflicts Found"
                  description="No unresolved faculty capacity conflicts match the selected severity filter."
                  size="sm"
                />
              </div>
            </template>

            <Column field="severity" header="Severity" style="min-width: 110px">
              <template #body="{ data }">
                <StatusPill
                  :status="data.severity === 'blocking' ? 'rejected' : 'pending'"
                  :label="data.severity"
                  size="sm"
                />
              </template>
            </Column>

            <Column field="conflictType" header="Conflict Type" style="min-width: 160px">
              <template #body="{ data }">
                <span class="font-semibold text-slate-800">{{ data.conflictType }}</span>
              </template>
            </Column>

            <Column field="description" header="Description" style="min-width: 260px">
              <template #body="{ data }">
                <span class="text-slate-600">{{ data.description }}</span>
              </template>
            </Column>

            <Column field="detectedDate" header="Detected Date" style="min-width: 120px" />

            <Column field="status" header="Status" style="min-width: 110px">
              <template #body="{ data }">
                <StatusPill :status="data.status" size="sm" />
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </div>

    <!-- 5. Line-Item Assignment Drill-Down Drawer -->
    <WorkloadDrillDownDrawer
      v-model:visible="isDrillDownOpen"
      :commitment="activeSelectedTeacher"
      :academic-period-id="selectedPeriodId"
      @request-exception="openRequestException"
      @close="isDrillDownOpen = false"
    />

    <!-- 6. Request Exception Modal (Coordinator Recommendation) -->
    <RequestExceptionModal
      v-model:visible="isRequestModalOpen"
      :commitment="activeSelectedTeacher"
      :academic-period-id="selectedPeriodId"
      @submitted="handleExceptionSubmitted"
      @close="isRequestModalOpen = false"
    />

    <!-- 7. Approve Exception Modal (Authority Approval) -->
    <ApproveExceptionModal
      v-model:visible="isApproveModalOpen"
      :commitment="activeSelectedTeacher"
      :academic-period-id="selectedPeriodId"
      @approved="handleExceptionApproved"
      @close="isApproveModalOpen = false"
    />
  </div>
</template>
