<script setup lang="ts">
import { computed } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import {
  Calendar,
  Clock,
  Briefcase,
  Layers,
  AlertTriangle,
  Building2,
  FilePlus2,
  ExternalLink,
} from 'lucide-vue-next';
import { apiClient } from '@/shared/services/http';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import type { TeacherWorkloadCommitmentDto, WorkloadCommitmentStatus } from '../types';
import { WORKLOAD_STATUS_MAP } from './workloadConstants';

export interface LineItemAssignment {
  id: string;
  assignmentNumber: string;
  teachingDemandId: string;
  teacherProfileId: string;
  assignmentRoleId: string;
  assignmentRoleName?: string | null;
  assignmentScope: string;
  scopeReferenceId: string;
  scopeLabel: string;
  academicPeriodId: string;
  effectiveStartDate: string;
  effectiveEndDate: string;
  workloadCommitmentValue: number;
  workloadUnit: string;
  status: string;
  approvedBy?: string | null;
  approvedDate?: string | null;
}

export interface WorkloadDrillDownDrawerProps {
  visible: boolean;
  commitment: TeacherWorkloadCommitmentDto | null;
  academicPeriodId?: string;
}

const props = withDefaults(defineProps<WorkloadDrillDownDrawerProps>(), {
  visible: false,
  commitment: null,
  academicPeriodId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', val: boolean): void;
  (e: 'requestException', commitment: TeacherWorkloadCommitmentDto): void;
  (e: 'close'): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const teacherId = computed(() => props.commitment?.teacherProfileId);
const periodId = computed(
  () => props.academicPeriodId || props.commitment?.academicPeriodId,
);

// Query line-item assignments for the selected teacher and academic period
const {
  data: assignmentsData,
  isLoading: isLoadingAssignments,
  isError: isAssignmentsError,
  refetch: refetchAssignments,
} = useQuery({
  queryKey: computed(() => [
    'teaching',
    'assignments',
    'drilldown',
    teacherId.value,
    periodId.value,
  ]),
  queryFn: async (): Promise<LineItemAssignment[]> => {
    if (!teacherId.value) return [];

    const params: Record<string, string> = {
      teacherProfileId: teacherId.value,
      pageSize: '50',
    };
    if (periodId.value) {
      params.academicPeriodId = periodId.value;
    }

    const response = (await apiClient.get('/v1/teaching/assignments', {
      params,
    })) as unknown as { items?: LineItemAssignment[] } | LineItemAssignment[];

    if (Array.isArray(response)) {
      return response;
    }
    if (response && Array.isArray(response.items)) {
      return response.items;
    }
    return [];
  },
  enabled: computed(() => Boolean(props.visible && teacherId.value)),
});

const assignments = computed(() => assignmentsData.value ?? []);

const totalLineItemCommitted = computed(() => {
  return assignments.value
    .filter((a) => a.status === 'approved' || a.status === 'completed')
    .reduce((sum, a) => sum + (Number(a.workloadCommitmentValue) || 0), 0);
});

const totalLineItemPlanned = computed(() => {
  return assignments.value
    .filter((a) => a.status === 'proposed')
    .reduce((sum, a) => sum + (Number(a.workloadCommitmentValue) || 0), 0);
});

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleRequestException() {
  if (props.commitment) {
    emit('requestException', props.commitment);
  }
}
</script>

<template>
  <Drawer
    v-model:visible="isDrawerVisible"
    position="right"
    class="w-full md:!w-[620px] lg:!w-[700px] !bg-slate-50"
    data-testid="workload-drilldown-drawer"
  >
    <template #header>
      <div v-if="commitment" class="flex items-center gap-3">
        <div class="p-2 bg-indigo-50 text-indigo-600 rounded-lg">
          <Layers class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 leading-tight">
            {{ commitment.fullName }}
          </h3>
          <div class="flex items-center gap-2 text-xs text-slate-500 mt-0.5">
            <span class="font-mono bg-slate-100 px-1.5 py-0.2 rounded text-slate-700">
              {{ commitment.teacherNumber }}
            </span>
            <span v-if="commitment.campusAffiliation" class="flex items-center gap-0.5">
              <Building2 class="w-3 h-3 text-slate-400" />
              {{ commitment.campusAffiliation }}
            </span>
          </div>
        </div>
      </div>
      <div v-else class="text-base font-bold text-slate-900">
        Workload Assignment Drill-Down
      </div>
    </template>

    <div v-if="commitment" class="space-y-6 pb-6">
      <!-- 1. Capacity Snapshot Ribbon -->
      <div class="bg-white rounded-xl border border-slate-200 p-4 shadow-xs space-y-3">
        <div class="flex items-center justify-between border-b border-slate-100 pb-2">
          <span class="text-xs font-semibold text-slate-600 uppercase tracking-wide">
            Capacity Snapshot
          </span>
          <StatusPill
            :status="commitment.status"
            :variant="WORKLOAD_STATUS_MAP[commitment.status as WorkloadCommitmentStatus]?.variant ?? 'slate'"
            :label="WORKLOAD_STATUS_MAP[commitment.status as WorkloadCommitmentStatus]?.label ?? commitment.status"
            size="sm"
          />
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-center">
          <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
            <div class="text-[11px] font-medium text-slate-500">Committed</div>
            <div class="text-lg font-bold text-slate-900 mt-0.5" data-testid="drawer-committed-hours">
              {{ commitment.committedWorkloadHours }} hrs
            </div>
          </div>
          <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
            <div class="text-[11px] font-medium text-slate-500">Planned</div>
            <div class="text-lg font-bold text-slate-900 mt-0.5">
              {{ commitment.plannedWorkloadHours }} hrs
            </div>
          </div>
          <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
            <div class="text-[11px] font-medium text-slate-500">Max Limit</div>
            <div class="text-lg font-bold text-slate-700 mt-0.5">
              {{ commitment.maxCapacityLimitHours || 24 }} hrs
            </div>
          </div>
          <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
            <div class="text-[11px] font-medium text-slate-500">Remaining</div>
            <div
              class="text-lg font-bold mt-0.5"
              :class="commitment.remainingCapacityHours < 0 ? 'text-rose-600' : 'text-emerald-600'"
            >
              {{ commitment.remainingCapacityHours > 0 ? `+${commitment.remainingCapacityHours}` : commitment.remainingCapacityHours }} hrs
            </div>
          </div>
        </div>

        <div class="text-xs text-slate-500 flex items-center justify-between pt-1">
          <span>Active Assignments: <strong class="text-slate-800">{{ commitment.activeAssignmentCount }}</strong></span>
          <span>Approved Exceptions: <strong class="text-purple-700">{{ commitment.approvedExceptionCount }}</strong></span>
        </div>
      </div>

      <!-- 2. Line-Item Assignments Header -->
      <div class="space-y-3">
        <div class="flex items-center justify-between">
          <h4 class="text-xs font-bold text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <Briefcase class="w-4 h-4 text-indigo-600" />
            <span>Assigned Teaching Duties &amp; Cohorts</span>
          </h4>
          <span class="text-xs text-slate-500">
            {{ assignments.length }} line items
          </span>
        </div>

        <!-- Loading -->
        <div v-if="isLoadingAssignments" class="py-12 bg-white rounded-xl border border-slate-200">
          <LoadingSpinner label="Loading assigned responsibilities..." />
        </div>

        <!-- Error -->
        <div v-else-if="isAssignmentsError" class="p-6 bg-white rounded-xl border border-slate-200 text-center space-y-2">
          <AlertTriangle class="w-8 h-8 text-rose-500 mx-auto" />
          <p class="text-xs text-slate-600">Failed to load assignment line items.</p>
          <Button size="small" severity="secondary" variant="outlined" @click="() => refetchAssignments()">
            Retry
          </Button>
        </div>

        <!-- Empty State -->
        <div v-else-if="assignments.length === 0" class="bg-white rounded-xl border border-slate-200 py-8">
          <EmptyState
            title="No individual assignments recorded"
            message="No teaching assignments have been authorized or planned for this faculty member in the selected period."
          />
        </div>

        <!-- Assignments List -->
        <div v-else class="space-y-2.5" data-testid="drawer-assignments-list">
          <div
            v-for="item in assignments"
            :key="item.id"
            class="p-3.5 bg-white rounded-xl border border-slate-200 hover:border-slate-300 shadow-xs transition-all space-y-2"
          >
            <div class="flex items-start justify-between gap-2">
              <div class="space-y-0.5">
                <span class="text-sm font-semibold text-slate-900 block leading-snug">
                  {{ item.scopeLabel }}
                </span>
                <div class="flex items-center gap-1.5 text-xs text-slate-500">
                  <span class="font-mono text-[11px] text-slate-600 bg-slate-100 px-1 py-0.5 rounded">
                    {{ item.assignmentNumber }}
                  </span>
                  <span>&bull;</span>
                  <span class="capitalize text-slate-700 font-medium">
                    {{ item.assignmentRoleName || item.assignmentScope.replace('_', ' ') }}
                  </span>
                </div>
              </div>

              <!-- Workload Hours Badge -->
              <div class="text-right">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-indigo-50 text-indigo-700 border border-indigo-100">
                  <Clock class="w-3 h-3 mr-1" />
                  {{ item.workloadCommitmentValue }} hrs/wk
                </span>
              </div>
            </div>

            <!-- Footer: Dates & Lifecycle Status -->
            <div class="flex items-center justify-between text-[11px] text-slate-500 pt-1 border-t border-slate-50">
              <span class="flex items-center gap-1">
                <Calendar class="w-3 h-3 text-slate-400" />
                {{ item.effectiveStartDate }} to {{ item.effectiveEndDate }}
              </span>
              <StatusPill
                :status="item.status"
                :variant="item.status === 'approved' ? 'emerald' : item.status === 'proposed' ? 'amber' : 'slate'"
                :label="item.status"
                size="sm"
              />
            </div>
          </div>
        </div>

        <!-- Breakdown reconciliation note -->
        <div
          v-if="assignments.length > 0"
          class="p-3 rounded-lg bg-indigo-50/50 border border-indigo-100 text-xs text-indigo-900 flex items-center justify-between"
        >
          <span>Line-Item Total (Approved):</span>
          <span class="font-bold text-sm">{{ totalLineItemCommitted }} hrs/wk</span>
        </div>
      </div>
    </div>

    <!-- Drawer Footer Actions -->
    <template #footer>
      <div class="flex items-center justify-between w-full pt-2">
        <Button
          type="button"
          label="Close"
          severity="secondary"
          variant="outlined"
          size="small"
          @click="handleClose"
        />

        <Button
          v-if="commitment"
          type="button"
          severity="warn"
          size="small"
          data-testid="drawer-request-exception-button"
          @click="handleRequestException"
        >
          <FilePlus2 class="w-4 h-4 mr-1.5" />
          <span>Request Exception</span>
        </Button>
      </div>
    </template>
  </Drawer>
</template>
