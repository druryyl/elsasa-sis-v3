<script setup lang="ts">
import { computed, ref } from 'vue';
import Timeline from 'primevue/timeline';
import Select from 'primevue/select';
import Button from 'primevue/button';
import {
  History,
  FileText,
  CheckCircle2,
  GraduationCap,
  UserCheck,
  GitCommit,
  ShieldAlert,
  Clock,
  Calendar,
  AlertCircle,
} from 'lucide-vue-next';
import { LoadingSpinner, EmptyState } from '@/shared/components';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { useStudentAdministrativeHistoryQuery } from '../composables/useStudentRecordsQueries';
import type { AdministrativeHistoryEntryDto, StudentDetail } from '../types/records.types';

const props = defineProps<{
  studentId?: string;
  student?: StudentDetail | { id: string };
}>();

const resolvedStudentId = computed(() => props.studentId || props.student?.id || '');

const {
  data: historyData,
  isLoading,
  isError,
  error,
  refetch,
} = useStudentAdministrativeHistoryQuery(resolvedStudentId);

const entries = computed<AdministrativeHistoryEntryDto[]>(
  () => historyData.value?.entries ?? [],
);

const filterOptions = [
  { label: 'All Events', value: 'all' },
  { label: 'Placement', value: 'placement' },
  { label: 'Status', value: 'status' },
  { label: 'Intake', value: 'intake' },
];

const selectedFilter = ref('all');

const filteredEntries = computed(() => {
  const list = entries.value;
  if (selectedFilter.value === 'all') return list;

  if (selectedFilter.value === 'placement') {
    return list.filter((e) => e.eventType.toLowerCase().includes('placement'));
  }

  if (selectedFilter.value === 'status') {
    return list.filter(
      (e) =>
        e.eventType.toLowerCase().includes('status') ||
        e.eventType.toLowerCase().includes('activated'),
    );
  }

  if (selectedFilter.value === 'intake') {
    return list.filter(
      (e) =>
        e.eventType.toLowerCase().includes('application') ||
        e.eventType.toLowerCase().includes('enrollment') ||
        e.eventType.toLowerCase().includes('intake'),
    );
  }

  return list;
});

function getEventBadge(eventType: string): {
  label: string;
  bgClass: string;
  textClass: string;
  borderClass: string;
  dotClass: string;
  markerBgClass: string;
} {
  switch (eventType.toLowerCase()) {
    case 'application_submitted':
      return {
        label: 'Application Submitted',
        bgClass: 'bg-slate-100 dark:bg-slate-800',
        textClass: 'text-slate-700 dark:text-slate-300',
        borderClass: 'border-slate-200 dark:border-slate-700',
        dotClass: 'bg-slate-500',
        markerBgClass: 'bg-slate-600',
      };
    case 'application_accepted':
      return {
        label: 'Application Accepted',
        bgClass: 'bg-emerald-50 dark:bg-emerald-950',
        textClass: 'text-emerald-700 dark:text-emerald-300',
        borderClass: 'border-emerald-200 dark:border-emerald-800',
        dotClass: 'bg-emerald-500',
        markerBgClass: 'bg-emerald-600',
      };
    case 'enrollment_completed':
      return {
        label: 'Enrollment Completed',
        bgClass: 'bg-teal-50 dark:bg-teal-950',
        textClass: 'text-teal-700 dark:text-teal-300',
        borderClass: 'border-teal-200 dark:border-teal-800',
        dotClass: 'bg-teal-500',
        markerBgClass: 'bg-teal-600',
      };
    case 'student_activated':
      return {
        label: 'Student Activated',
        bgClass: 'bg-emerald-50 dark:bg-emerald-950',
        textClass: 'text-emerald-700 dark:text-emerald-300',
        borderClass: 'border-emerald-200 dark:border-emerald-800',
        dotClass: 'bg-emerald-500',
        markerBgClass: 'bg-emerald-600',
      };
    case 'placement_changed':
      return {
        label: 'Placement Changed',
        bgClass: 'bg-indigo-50 dark:bg-indigo-950',
        textClass: 'text-indigo-700 dark:text-indigo-300',
        borderClass: 'border-indigo-200 dark:border-indigo-800',
        dotClass: 'bg-indigo-500',
        markerBgClass: 'bg-indigo-600',
      };
    case 'status_adjusted':
      return {
        label: 'Status Adjusted',
        bgClass: 'bg-amber-50 dark:bg-amber-950',
        textClass: 'text-amber-700 dark:text-amber-300',
        borderClass: 'border-amber-200 dark:border-amber-800',
        dotClass: 'bg-amber-500',
        markerBgClass: 'bg-amber-600',
      };
    default: {
      const formatted = eventType.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
      return {
        label: formatted,
        bgClass: 'bg-slate-100 dark:bg-slate-800',
        textClass: 'text-slate-700 dark:text-slate-300',
        borderClass: 'border-slate-200 dark:border-slate-700',
        dotClass: 'bg-slate-500',
        markerBgClass: 'bg-slate-600',
      };
    }
  }
}

function getEventIcon(eventType: string) {
  switch (eventType.toLowerCase()) {
    case 'application_submitted':
      return FileText;
    case 'application_accepted':
      return CheckCircle2;
    case 'enrollment_completed':
      return GraduationCap;
    case 'student_activated':
      return UserCheck;
    case 'placement_changed':
      return GitCommit;
    case 'status_adjusted':
      return ShieldAlert;
    default:
      return Clock;
  }
}
</script>

<template>
  <div class="space-y-4" data-testid="student-administrative-history-tab">
    <!-- Header & Event Filter -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-1 border-b border-surface-200/60 dark:border-surface-700/60">
      <div>
        <h4 class="font-semibold text-sm text-surface-900 dark:text-surface-100 flex items-center gap-1.5">
          <History class="w-4 h-4 text-teal-600" />
          <span>Administrative Lifecycle History</span>
        </h4>
        <p class="text-xs text-surface-500 mt-0.5">
          Authoritative, append-only chronological lifecycle and administrative event log (ADR-008).
        </p>
      </div>

      <div class="flex items-center gap-2 self-start sm:self-auto">
        <label for="history-filter-select" class="text-xs text-surface-500 font-medium shrink-0">Filter Category:</label>
        <Select
          id="history-filter-select"
          v-model="selectedFilter"
          :options="filterOptions"
          option-label="label"
          option-value="value"
          class="text-xs w-36"
          data-testid="history-filter-select"
        />
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="isLoading"
      class="py-12 flex justify-center"
      data-testid="history-loading"
    >
      <LoadingSpinner size="md" message="Loading administrative history audit log..." />
    </div>

    <!-- Error State -->
    <div
      v-else-if="isError"
      class="p-6 rounded-lg border border-rose-200 bg-rose-50 dark:bg-rose-950/40 dark:border-rose-900 text-center space-y-3"
      data-testid="history-error"
    >
      <div class="w-10 h-10 rounded-full bg-rose-100 dark:bg-rose-900/60 flex items-center justify-center mx-auto text-rose-600 dark:text-rose-400">
        <AlertCircle class="w-5 h-5" />
      </div>
      <div>
        <h5 class="text-rose-700 dark:text-rose-300 font-semibold text-sm">Failed to load administrative history</h5>
        <p class="text-xs text-rose-600 dark:text-rose-400 mt-0.5">
          {{ error?.message || 'An unexpected error occurred while fetching the audit log.' }}
        </p>
      </div>
      <Button
        label="Retry"
        icon="pi pi-refresh"
        severity="secondary"
        size="small"
        data-testid="history-retry-btn"
        @click="() => refetch()"
      />
    </div>

    <!-- Empty State (No entries in history) -->
    <div
      v-else-if="entries.length === 0"
      data-testid="history-empty"
    >
      <EmptyState
        title="No Administrative History Found"
        description="No lifecycle audit records or placement events have been logged for this student."
        size="sm"
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center mx-auto mb-2 text-surface-400">
            <History class="w-6 h-6" />
          </div>
        </template>
      </EmptyState>
    </div>

    <!-- Empty State (No matching filter) -->
    <div
      v-else-if="filteredEntries.length === 0"
      class="py-8 text-center text-surface-500 dark:text-surface-400 text-xs bg-surface-50/50 dark:bg-surface-800/30 rounded-lg border border-surface-200 dark:border-surface-700"
      data-testid="history-empty-filter"
    >
      No administrative history entries match the selected filter "{{ selectedFilter }}".
    </div>

    <!-- Timeline Presentation -->
    <div v-else class="pt-2 pb-4" data-testid="history-timeline-wrapper">
      <Timeline
        :value="filteredEntries"
        class="customized-timeline"
        data-testid="history-timeline"
      >
        <!-- Marker Slot -->
        <template #marker="{ item }">
          <span
            class="flex w-7 h-7 items-center justify-center rounded-full text-white shadow-xs ring-4 ring-surface-0 dark:ring-surface-900"
            :class="getEventBadge(item.eventType).markerBgClass"
          >
            <component :is="getEventIcon(item.eventType)" class="w-3.5 h-3.5" />
          </span>
        </template>

        <!-- Content Slot -->
        <template #content="{ item }">
          <div
            class="p-4 bg-surface-50/80 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700 rounded-lg shadow-2xs mb-4 space-y-2.5 text-xs"
            :data-testid="`history-entry-${item.id}`"
          >
            <!-- Top Row: Event Badge, Date, and Correction Indicator -->
            <div class="flex items-center justify-between gap-2 flex-wrap">
              <div class="flex items-center gap-2">
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border"
                  :class="[getEventBadge(item.eventType).bgClass, getEventBadge(item.eventType).textClass, getEventBadge(item.eventType).borderClass]"
                  data-testid="history-event-badge"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="getEventBadge(item.eventType).dotClass" />
                  {{ getEventBadge(item.eventType).label }}
                </span>

                <!-- Correction Badge / Link -->
                <span
                  v-if="item.isCorrection"
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 border border-amber-300 dark:border-amber-700"
                  data-testid="history-correction-badge"
                >
                  Correction
                  <span v-if="item.correctsEntryId" class="font-mono text-[10px]">
                    (Entry: {{ item.correctsEntryId.slice(0, 8) }})
                  </span>
                </span>
              </div>

              <!-- Event Date -->
              <div class="flex items-center gap-1.5 text-surface-500 font-medium" data-testid="history-event-date">
                <Calendar class="w-3.5 h-3.5 text-surface-400" />
                <span>{{ formatDisplayDate(item.eventDate) }}</span>
              </div>
            </div>

            <!-- Detail Message -->
            <p
              v-if="item.detail"
              class="text-surface-800 dark:text-surface-200 font-normal leading-relaxed text-sm"
              data-testid="history-event-detail"
            >
              {{ item.detail }}
            </p>

            <!-- Metadata Footer: Authorizer, Decision Reference, Created At -->
            <div
              v-if="item.authorizedBy || item.decisionReference || item.createdBy || item.createdAt"
              class="pt-2 border-t border-surface-200/60 dark:border-surface-700/60 flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-surface-500"
            >
              <span v-if="item.authorizedBy" data-testid="history-authorized-by">
                Authorized by: <strong class="text-surface-700 dark:text-surface-300">{{ item.authorizedBy }}</strong>
              </span>

              <span v-if="item.decisionReference" data-testid="history-decision-reference">
                Decision Ref:
                <code class="font-mono text-surface-700 dark:text-surface-300 bg-surface-100 dark:bg-surface-800 px-1 py-0.5 rounded">
                  {{ item.decisionReference }}
                </code>
              </span>

              <span v-if="item.createdAt" class="text-surface-400 ml-auto" data-testid="history-created-at">
                Logged {{ formatDisplayDateTime(item.createdAt) }}
                <span v-if="item.createdBy">by {{ item.createdBy }}</span>
              </span>
            </div>
          </div>
        </template>
      </Timeline>
    </div>
  </div>
</template>
