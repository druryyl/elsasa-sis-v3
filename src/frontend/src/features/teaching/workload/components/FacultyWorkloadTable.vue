<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import {
  Search,
  Eye,
  FilePlus2,
  CheckCheck,
  AlertTriangle,
  Building2,
  UserCheck,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import type { TeacherWorkloadCommitmentDto, WorkloadCommitmentStatus } from '../types';
import {
  WORKLOAD_STATUS_MAP,
  STANDARD_BASELINE_HOURS,
  STANDARD_WARNING_HOURS,
} from './workloadConstants';

export interface FacultyWorkloadTableProps {
  commitments?: TeacherWorkloadCommitmentDto[];
  isLoading?: boolean;
  isError?: boolean;
  errorMessage?: string | null;
  selectedStatus?: string | null;
  canRequestException?: boolean;
  canApproveException?: boolean;
}

const props = withDefaults(defineProps<FacultyWorkloadTableProps>(), {
  commitments: () => [],
  isLoading: false,
  isError: false,
  errorMessage: null,
  selectedStatus: null,
  canRequestException: true,
  canApproveException: true,
});

const emit = defineEmits<{
  (e: 'drillDown', commitment: TeacherWorkloadCommitmentDto): void;
  (e: 'requestException', commitment: TeacherWorkloadCommitmentDto): void;
  (e: 'approveException', commitment: TeacherWorkloadCommitmentDto): void;
  (e: 'update:selectedStatus', status: string): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref<string>('');

const statusFilterOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Normal Load', value: 'normal' },
  { label: 'Under Allocated', value: 'under_allocated' },
  { label: 'Approaching Limit', value: 'approaching_limit' },
  { label: 'Overloaded (Exception)', value: 'overloaded_with_exception' },
  { label: 'Limit Breached', value: 'breached' },
];

const internalStatusFilter = computed({
  get: () => props.selectedStatus ?? 'all',
  set: (val: string) => emit('update:selectedStatus', val),
});

const filteredCommitments = computed(() => {
  return props.commitments.filter((item) => {
    // Status filter
    if (internalStatusFilter.value && internalStatusFilter.value !== 'all') {
      if (item.status !== internalStatusFilter.value) {
        // Also allow 'normal' to encompass 'under_allocated' if clicked from KPI
        if (
          internalStatusFilter.value === 'normal' &&
          (item.status === 'normal' || item.status === 'under_allocated')
        ) {
          // match
        } else {
          return false;
        }
      }
    }

    // Search query filter (matches name, teacher number, or campus)
    if (searchQuery.value.trim()) {
      const q = searchQuery.value.trim().toLowerCase();
      const matchName = item.fullName?.toLowerCase().includes(q);
      const matchNumber = item.teacherNumber?.toLowerCase().includes(q);
      const matchCampus = item.campusAffiliation?.toLowerCase().includes(q);
      if (!matchName && !matchNumber && !matchCampus) {
        return false;
      }
    }

    return true;
  });
});

function getCapacityPercentage(item: TeacherWorkloadCommitmentDto): number {
  const max = item.maxCapacityLimitHours || item.prohibitedThreshold || STANDARD_BASELINE_HOURS;
  if (max <= 0) return 0;
  return Math.min(100, Math.round((item.committedWorkloadHours / max) * 100));
}

function getProgressBarClass(item: TeacherWorkloadCommitmentDto): string {
  if (item.status === 'overloaded_with_exception') {
    return 'bg-purple-600';
  }
  if (item.status === 'breached') {
    return 'bg-rose-600';
  }

  const warning = item.warningThreshold || STANDARD_WARNING_HOURS;
  const max = item.maxCapacityLimitHours || item.prohibitedThreshold || STANDARD_BASELINE_HOURS;

  if (item.committedWorkloadHours > max) {
    return 'bg-rose-600';
  }
  if (item.committedWorkloadHours > warning) {
    return 'bg-amber-500';
  }
  return 'bg-emerald-500';
}
</script>

<template>
  <div class="space-y-4" data-testid="faculty-workload-table-container">
    <!-- Search & Filter Controls -->
    <div
      class="p-4 bg-white rounded-xl border border-slate-200 shadow-xs flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3"
    >
      <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 flex-1">
        <!-- Search Input -->
        <div class="relative flex-1 max-w-sm">
          <Search class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <InputText
            v-model="searchQuery"
            placeholder="Search teacher by name, NIP..."
            class="w-full !pl-9"
            size="small"
            data-testid="workload-search-input"
          />
        </div>

        <!-- Status Filter Dropdown -->
        <Select
          v-model="internalStatusFilter"
          :options="statusFilterOptions"
          option-label="label"
          option-value="value"
          size="small"
          class="w-56"
          data-testid="workload-status-filter"
        />
      </div>

      <div class="flex items-center gap-2">
        <span class="text-xs text-slate-500 font-medium">
          Showing <span class="font-bold text-slate-800">{{ filteredCommitments.length }}</span> of
          <span class="font-bold text-slate-800">{{ commitments.length }}</span> faculty
        </span>
      </div>
    </div>

    <!-- Table Container -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
      <!-- Loading State -->
      <div v-if="isLoading" class="py-16 flex justify-center items-center">
        <LoadingSpinner label="Loading faculty workload commitments..." />
      </div>

      <!-- Error State -->
      <div v-else-if="isError" class="p-8 text-center space-y-3">
        <AlertTriangle class="w-10 h-10 text-rose-500 mx-auto" />
        <h4 class="text-sm font-semibold text-slate-800">Unable to load workload records</h4>
        <p class="text-xs text-slate-500">
          {{ errorMessage ?? 'An error occurred while fetching faculty workload snapshots.' }}
        </p>
        <Button size="small" severity="secondary" variant="outlined" @click="emit('refresh')">
          Retry
        </Button>
      </div>

      <!-- PrimeVue DataTable -->
      <DataTable
        v-else
        :value="filteredCommitments"
        data-key="id"
        striped-rows
        responsive-layout="scroll"
        class="text-xs"
        data-testid="workload-datatable"
      >
        <template #empty>
          <div class="py-8">
            <EmptyState
              title="No faculty members found"
              message="No workload commitment snapshots match the specified filter or search criteria."
            />
          </div>
        </template>

        <!-- Column 1: Faculty Member Details -->
        <Column header="Faculty Member" sortable field="fullName" style="min-width: 220px">
          <template #body="{ data }">
            <div class="space-y-1">
              <span class="font-semibold text-slate-900 block leading-tight text-sm">
                {{ data.fullName }}
              </span>
              <div class="flex flex-wrap items-center gap-1.5 text-[11px] text-slate-500">
                <span class="font-mono bg-slate-100 px-1.5 py-0.5 rounded text-slate-700">
                  {{ data.teacherNumber }}
                </span>
                <span v-if="data.campusAffiliation" class="flex items-center gap-0.5">
                  <Building2 class="w-3 h-3 text-slate-400" />
                  {{ data.campusAffiliation }}
                </span>
                <span v-if="data.standing" class="inline-flex items-center gap-0.5 text-emerald-600">
                  <UserCheck class="w-3 h-3" />
                  {{ data.standing }}
                </span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 2: Status Badge -->
        <Column header="Status" sortable field="status" style="min-width: 150px">
          <template #body="{ data }">
            <StatusPill
              :status="data.status"
              :variant="WORKLOAD_STATUS_MAP[data.status as WorkloadCommitmentStatus]?.variant ?? 'slate'"
              :label="WORKLOAD_STATUS_MAP[data.status as WorkloadCommitmentStatus]?.label ?? data.status"
              size="sm"
            />
          </template>
        </Column>

        <!-- Column 3: Hours Breakdown (Committed / Planned / Remaining) -->
        <Column header="Hours (Committed / Planned)" style="min-width: 170px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="flex items-baseline gap-1.5">
                <span class="text-sm font-bold text-slate-900">
                  {{ data.committedWorkloadHours }} hrs
                </span>
                <span class="text-slate-400 text-[11px]">committed</span>
              </div>
              <div class="flex items-center gap-2 text-[11px] text-slate-500">
                <span>Planned: <strong class="text-slate-700">{{ data.plannedWorkloadHours }} hrs</strong></span>
                <span>&bull;</span>
                <span :class="data.remainingCapacityHours < 0 ? 'text-rose-600 font-semibold' : 'text-slate-600'">
                  Rem: {{ data.remainingCapacityHours > 0 ? `+${data.remainingCapacityHours}` : data.remainingCapacityHours }} hrs
                </span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 4: Capacity Progress Bar (0 - 24 hrs range) -->
        <Column header="Capacity Utilization (0–24 hrs)" style="min-width: 200px">
          <template #body="{ data }">
            <div class="space-y-1.5 py-1">
              <div class="flex items-center justify-between text-[11px] font-medium text-slate-600">
                <span>{{ data.committedWorkloadHours }} / {{ data.maxCapacityLimitHours || 24 }} hrs</span>
                <span :class="data.status === 'breached' ? 'text-rose-600 font-bold' : 'text-slate-700'">
                  {{ getCapacityPercentage(data) }}%
                </span>
              </div>
              <!-- Visual Bar -->
              <div class="w-full h-2.5 bg-slate-100 rounded-full overflow-hidden flex shadow-inner">
                <div
                  class="h-full rounded-full transition-all duration-300"
                  :class="getProgressBarClass(data)"
                  :style="{ width: `${getCapacityPercentage(data)}%` }"
                  :title="`${data.committedWorkloadHours} hrs committed`"
                  data-testid="capacity-progress-bar"
                />
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 5: Active Count & Exceptions -->
        <Column header="Assignments & Exceptions" style="min-width: 140px">
          <template #body="{ data }">
            <div class="space-y-0.5 text-[11px]">
              <div class="text-slate-700">
                <span class="font-bold text-slate-900">{{ data.activeAssignmentCount }}</span> active roles
              </div>
              <div v-if="data.approvedExceptionCount > 0" class="text-purple-700 font-medium">
                <span class="font-bold">{{ data.approvedExceptionCount }}</span> authorized exception(s)
              </div>
              <div v-else class="text-slate-400">
                0 exceptions
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 6: Actions -->
        <Column header="Actions" align-frozen="right" frozen style="min-width: 160px">
          <template #body="{ data }">
            <div class="flex items-center gap-1.5">
              <!-- Drill-down -->
              <Button
                type="button"
                severity="secondary"
                variant="outlined"
                size="small"
                class="!px-2.5 !py-1"
                title="View line-item assignment breakdown"
                data-testid="workload-drilldown-button"
                @click="emit('drillDown', data)"
              >
                <Eye class="w-3.5 h-3.5 mr-1" />
                <span>Details</span>
              </Button>

              <!-- Request Exception (Coordinator) -->
              <Button
                v-if="canRequestException"
                type="button"
                severity="warn"
                variant="outlined"
                size="small"
                class="!px-2.5 !py-1"
                title="Request workload exception recommendation"
                data-testid="workload-request-exception-button"
                @click="emit('requestException', data)"
              >
                <FilePlus2 class="w-3.5 h-3.5" />
              </Button>

              <!-- Approve Exception (Authority) -->
              <Button
                v-if="canApproveException && (data.status === 'overloaded_with_exception' || data.status === 'breached' || data.status === 'approaching_limit')"
                type="button"
                severity="success"
                variant="outlined"
                size="small"
                class="!px-2.5 !py-1"
                title="Approve formal workload exception"
                data-testid="workload-approve-exception-button"
                @click="emit('approveException', data)"
              >
                <CheckCheck class="w-3.5 h-3.5" />
              </Button>
            </div>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>
