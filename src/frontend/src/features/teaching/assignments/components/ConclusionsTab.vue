<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  CheckCircle2,
  Award,
  Clock,
  Sparkles,
  Calendar,
  FileCheck2,
  AlertTriangle,
  RotateCcw,
  Plus,
  BookOpen,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import {
  useCompletions,
  useAssignments,
} from '../composables/useAssignments';
import ConcludeAssignmentModal from './ConcludeAssignmentModal.vue';
import {
  COMPLETION_STATUS_MAP,
  DEFAULT_FACULTY_CANDIDATES,
  PERIOD_PRESETS,
} from '../constants/assignmentWorkbenchConstants';
import type {
  AssignmentCompletionDto,
  TeachingAssignmentDto,
} from '../types';

export interface ConclusionsTabProps {
  academicPeriodId?: string;
  campusScope?: string;
  search?: string;
  canManage?: boolean;
}

const props = withDefaults(defineProps<ConclusionsTabProps>(), {
  academicPeriodId: undefined,
  campusScope: undefined,
  search: '',
  canManage: true,
});

// Queries
const {
  data: completionsData,
  isLoading,
  isError,
  refetch,
} = useCompletions(
  computed(() => ({
    academicPeriodId: props.academicPeriodId || PERIOD_PRESETS[0].id,
  })),
);

const completions = computed<AssignmentCompletionDto[]>(() => {
  if (!completionsData.value) return [];
  return Array.isArray(completionsData.value)
    ? completionsData.value
    : (completionsData.value as { items?: AssignmentCompletionDto[] }).items || [];
});

// Query active approved assignments that can be concluded
const { data: assignmentsData } = useAssignments(
  computed(() => ({
    academicPeriodId: props.academicPeriodId || PERIOD_PRESETS[0].id,
    status: 'approved',
  })),
);

const candidateAssignmentsToConclude = computed<TeachingAssignmentDto[]>(() => {
  if (!assignmentsData.value) return [];
  return Array.isArray(assignmentsData.value)
    ? assignmentsData.value
    : (assignmentsData.value as { items?: TeachingAssignmentDto[] }).items || [];
});

// Summary metrics computation
const totalConcluded = computed(() => completions.value.length);

const totalAcademicVerified = computed(
  () => completions.value.filter((c) => c.academicConfirmationReceived).length,
);

const totalHandoverConfirmed = computed(
  () => completions.value.filter((c) => c.handoverConfirmed).length,
);

const verificationRate = computed(() => {
  if (totalConcluded.value === 0) return '100%';
  const pct = Math.round((totalAcademicVerified.value / totalConcluded.value) * 100);
  return `${pct}%`;
});

// Conclude Modal State
const isConcludeModalVisible = ref<boolean>(false);
const selectedAssignmentToConclude = ref<TeachingAssignmentDto | null>(null);

function handleOpenConcludeModal(asn?: TeachingAssignmentDto) {
  selectedAssignmentToConclude.value = asn || (candidateAssignmentsToConclude.value[0] || null);
  isConcludeModalVisible.value = true;
}
</script>

<template>
  <div class="space-y-6" data-testid="conclusions-tab">
    <!-- Top Completion Metrics KPI Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4" data-testid="conclusions-metrics">
      <!-- Total Concluded -->
      <div class="p-4 rounded-xl bg-surface-50 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700 shadow-2xs">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-surface-500 uppercase tracking-wider">Concluded Assignments</span>
          <div class="w-8 h-8 rounded-lg bg-emerald-50 dark:bg-emerald-950 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
            <CheckCircle2 class="w-4 h-4" />
          </div>
        </div>
        <div class="mt-2 flex items-baseline gap-2">
          <span class="text-2xl font-bold font-mono text-surface-900 dark:text-surface-100" data-testid="metric-total-concluded">
            {{ totalConcluded }}
          </span>
          <span class="text-xs text-surface-500">assignments</span>
        </div>
      </div>

      <!-- Academic Verification Rate -->
      <div class="p-4 rounded-xl bg-surface-50 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700 shadow-2xs">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-surface-500 uppercase tracking-wider">Academic Sign-off Rate</span>
          <div class="w-8 h-8 rounded-lg bg-primary-50 dark:bg-primary-950 flex items-center justify-center text-primary-600 dark:text-primary-400">
            <Award class="w-4 h-4" />
          </div>
        </div>
        <div class="mt-2 flex items-baseline gap-2">
          <span class="text-2xl font-bold font-mono text-primary-600 dark:text-primary-400" data-testid="metric-verification-rate">
            {{ verificationRate }}
          </span>
          <span class="text-xs text-surface-500">grades verified</span>
        </div>
      </div>

      <!-- Handover Confirmations -->
      <div class="p-4 rounded-xl bg-surface-50 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700 shadow-2xs">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-surface-500 uppercase tracking-wider">Handover Confirmed</span>
          <div class="w-8 h-8 rounded-lg bg-indigo-50 dark:bg-indigo-950 flex items-center justify-center text-indigo-600 dark:text-indigo-400">
            <FileCheck2 class="w-4 h-4" />
          </div>
        </div>
        <div class="mt-2 flex items-baseline gap-2">
          <span class="text-2xl font-bold font-mono text-surface-900 dark:text-surface-100" data-testid="metric-total-handover">
            {{ totalHandoverConfirmed }}
          </span>
          <span class="text-xs text-surface-500">sign-offs</span>
        </div>
      </div>

      <!-- Capacity Released Pool Status -->
      <div class="p-4 rounded-xl bg-surface-50 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700 shadow-2xs">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-surface-500 uppercase tracking-wider">Active Concluding Pool</span>
          <div class="w-8 h-8 rounded-lg bg-amber-50 dark:bg-amber-950 flex items-center justify-center text-amber-600 dark:text-amber-400">
            <Sparkles class="w-4 h-4" />
          </div>
        </div>
        <div class="mt-2 flex items-baseline gap-2">
          <span class="text-2xl font-bold font-mono text-amber-600 dark:text-amber-400" data-testid="metric-ready-to-conclude">
            {{ candidateAssignmentsToConclude.length }}
          </span>
          <span class="text-xs text-surface-500">awaiting release</span>
        </div>
      </div>
    </div>

    <!-- Toolbar with Action to Conclude -->
    <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-surface-50/60 dark:bg-surface-800/40 p-3 rounded-lg border border-surface-200 dark:border-surface-700">
      <div>
        <h4 class="text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          Formal Assignment Conclusions & Deliverables Archive
        </h4>
        <p class="text-[11px] text-surface-500">
          Audit trail of finalized assignments with verified grade submissions and released faculty capacity
        </p>
      </div>

      <div class="flex items-center gap-2">
        <Button
          v-if="canManage && candidateAssignmentsToConclude.length > 0"
          label="Conclude Assignment"
          severity="success"
          size="small"
          class="!text-xs"
          @click="() => handleOpenConcludeModal()"
          data-testid="btn-open-conclude-modal"
        >
          <template #icon>
            <CheckCircle2 class="w-3.5 h-3.5 mr-1.5" />
          </template>
        </Button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="p-12 flex justify-center">
      <LoadingSpinner />
    </div>

    <!-- Error State -->
    <div
      v-else-if="isError"
      class="p-8 text-center bg-rose-50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 rounded-xl"
    >
      <AlertTriangle class="w-8 h-8 text-rose-500 mx-auto mb-2" />
      <p class="text-sm font-semibold text-rose-900 dark:text-rose-200">Failed to load assignment completions</p>
      <Button
        label="Try Again"
        severity="danger"
        variant="text"
        size="small"
        class="mt-2"
        @click="() => refetch()"
      />
    </div>

    <!-- Empty State -->
    <div
      v-else-if="completions.length === 0"
      class="p-12 text-center bg-surface-50/50 dark:bg-surface-800/20 border border-surface-200 dark:border-surface-700 rounded-xl"
      data-testid="empty-conclusions-state"
    >
      <EmptyState
        title="No Concluded Assignments in this Period"
        description="When teaching terms conclude and grades are submitted in SIAKAD, assignments are closed and committed hours are released."
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-emerald-50 dark:bg-emerald-950 flex items-center justify-center mx-auto mb-3 text-emerald-600 dark:text-emerald-400">
            <CheckCircle2 class="w-6 h-6" />
          </div>
        </template>
        <template #actions>
          <Button
            v-if="canManage && candidateAssignmentsToConclude.length > 0"
            label="Conclude First Assignment"
            severity="success"
            size="small"
            @click="() => handleOpenConcludeModal()"
          />
        </template>
      </EmptyState>
    </div>

    <!-- Completed Assignments Archive Table -->
    <div
      v-else
      class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs"
      data-testid="conclusions-table-container"
    >
      <DataTable
        :value="completions"
        responsive-layout="scroll"
        class="!text-xs"
        data-testid="conclusions-table"
      >
        <!-- Assignment Reference -->
        <Column header="Assignment Ref" class="w-40 font-mono font-medium">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="text-surface-900 dark:text-surface-100 font-semibold">{{ data.teachingAssignmentId?.slice(0, 8) }}...</span>
              <span class="block text-[10px] text-surface-400">Concluded: {{ data.completionDate }}</span>
            </div>
          </template>
        </Column>

        <!-- Academic Confirmation -->
        <Column header="Academic Deliverables" class="min-w-56">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="flex items-center gap-1.5">
                <CheckCircle2
                  v-if="data.academicConfirmationReceived"
                  class="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400 shrink-0"
                />
                <span class="font-medium text-surface-900 dark:text-surface-100">
                  {{ data.academicConfirmationSource || 'Grades Confirmed' }}
                </span>
              </div>
              <span class="block text-[10px] text-surface-500">
                Sign-off: {{ data.completedBy || 'Coordinator' }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Handover Sign-off -->
        <Column header="Handover" class="w-32">
          <template #body="{ data }">
            <span
              v-if="data.handoverConfirmed"
              class="inline-flex items-center gap-1 text-[11px] text-emerald-600 dark:text-emerald-400 font-medium"
            >
              <FileCheck2 class="w-3.5 h-3.5" /> Confirmed
            </span>
            <span v-else class="text-[11px] text-surface-400">Waived</span>
          </template>
        </Column>

        <!-- Unresolved Items Status -->
        <Column header="Unresolved Items" class="w-36">
          <template #body="{ data }">
            <span
              class="px-2 py-0.5 rounded-full text-[10px] font-medium inline-block capitalize"
              :class="
                data.unresolvedItemsStatus === 'none'
                  ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                  : 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300'
              "
            >
              {{ data.unresolvedItemsStatus?.replace('_', ' ') }}
            </span>
          </template>
        </Column>

        <!-- Capacity Released -->
        <Column header="Capacity Released" class="w-40 font-mono">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="font-semibold text-emerald-700 dark:text-emerald-400 block">
                Released
              </span>
              <span class="block text-[10px] text-surface-400">
                {{ data.capacityReleasedDate }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Status -->
        <Column header="Status" class="w-32">
          <template #body="{ data }">
            <StatusPill
              :status="data.status"
              :variant="COMPLETION_STATUS_MAP[data.status]?.variant || 'emerald'"
              :label="COMPLETION_STATUS_MAP[data.status]?.label || data.status"
            />
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Conclude Modal -->
    <ConcludeAssignmentModal
      v-model:visible="isConcludeModalVisible"
      :assignment="selectedAssignmentToConclude"
      @concluded="() => refetch()"
    />
  </div>
</template>
