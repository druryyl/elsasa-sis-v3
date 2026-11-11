<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { refDebounced } from '@vueuse/core';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import {
  Search,
  UserPlus,
  RotateCcw,
  AlertTriangle,
  Building2,
  GraduationCap,
  X,
  FileCheck2,
  ShieldAlert,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import type {
  FacultyQueueFilter,
  FacultyQueryParams,
  TeacherStanding,
  TeacherSummaryDto,
} from '../api/teachingTypes';
import { useTeacherProfilesQuery } from '../composables/useFacultyQueries';

// ---------------------------------------------------------------------------
// 1. Component Props & Emits
// ---------------------------------------------------------------------------
export interface FacultyQueueSidebarProps {
  selectedTeacherId?: string | null;
  canRegister?: boolean;
  /** Optional override for testing or external state management */
  teachers?: TeacherSummaryDto[];
  isLoading?: boolean;
  initialQueueFilter?: FacultyQueueFilter;
}

const props = withDefaults(defineProps<FacultyQueueSidebarProps>(), {
  selectedTeacherId: null,
  canRegister: true,
  teachers: undefined,
  isLoading: undefined,
  initialQueueFilter: 'all',
});

const emit = defineEmits<{
  (e: 'select', teacherId: string): void;
  (e: 'register'): void;
}>();

// ---------------------------------------------------------------------------
// 2. Queue Category Filter Pills (TD-07)
// ---------------------------------------------------------------------------
interface QueueFilterPill {
  id: FacultyQueueFilter;
  label: string;
}

const queueFilterPills: QueueFilterPill[] = [
  { id: 'all', label: 'All Faculty' },
  { id: 'pending_approval', label: 'Pending Profile Approval' },
  { id: 'pending_verification', label: 'Pending Verification' },
  { id: 'pending_authorization', label: 'Pending Authorization' },
  { id: 'corrections', label: 'Corrections' },
];

const activeQueueFilter = ref<FacultyQueueFilter>(props.initialQueueFilter || 'all');

watch(
  () => props.initialQueueFilter,
  (val) => {
    if (val) {
      activeQueueFilter.value = val;
    }
  },
  { immediate: true },
);

function setQueueFilter(filter: FacultyQueueFilter) {
  activeQueueFilter.value = filter;
}

// ---------------------------------------------------------------------------
// 3. Search & Standing Filters State
// ---------------------------------------------------------------------------
const searchInput = ref('');
const debouncedSearch = refDebounced(searchInput, 300);

const selectedStanding = ref<string>('all');

const standingOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Active', value: 'active' },
  { label: 'Pending Approval', value: 'pending_approval' },
  { label: 'Restricted', value: 'restricted' },
  { label: 'Suspended', value: 'suspended' },
  { label: 'Inactive', value: 'inactive' },
];

function resetFilters() {
  searchInput.value = '';
  selectedStanding.value = 'all';
  activeQueueFilter.value = 'all';
}

function clearSearch() {
  searchInput.value = '';
}

// ---------------------------------------------------------------------------
// 4. Data Query via TanStack Query
// ---------------------------------------------------------------------------
const queryParams = computed<FacultyQueryParams>(() => {
  const params: FacultyQueryParams = {
    queueFilter: activeQueueFilter.value,
    page: 1,
    pageSize: 100,
  };

  if (selectedStanding.value !== 'all') {
    params.standing = selectedStanding.value as TeacherStanding;
  }

  const term = debouncedSearch.value?.trim();
  if (term) {
    params.search = term;
  }

  return params;
});

const queryResult = useTeacherProfilesQuery(queryParams);

const effectiveLoading = computed(() => {
  if (typeof props.isLoading === 'boolean') return props.isLoading;
  if (props.teachers !== undefined) return false;
  return queryResult.isLoading.value;
});

const effectiveError = computed(() => {
  if (props.teachers !== undefined) return false;
  return queryResult.isError.value;
});

const facultyList = computed<TeacherSummaryDto[]>(() => {
  if (props.teachers !== undefined) return props.teachers;
  return queryResult.data.value?.items ?? [];
});

const totalCount = computed(() => {
  if (props.teachers !== undefined) return props.teachers.length;
  return queryResult.data.value?.totalCount ?? facultyList.value.length;
});

// ---------------------------------------------------------------------------
// 5. Actions
// ---------------------------------------------------------------------------
function handleSelect(id: string) {
  emit('select', id);
}

function handleRegister() {
  emit('register');
}
</script>

<template>
  <aside
    class="flex flex-col bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden h-full"
    data-testid="faculty-queue-sidebar"
  >
    <!-- Top Action & Header Bar -->
    <div class="p-3.5 border-b border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/40 space-y-3">
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <GraduationCap class="w-5 h-5 text-primary-600 dark:text-primary-400" />
          <h2 class="text-sm font-semibold text-surface-900 dark:text-surface-100">
            Work Queue
          </h2>
          <span
            class="text-[11px] font-medium bg-surface-200 dark:bg-surface-700 text-surface-700 dark:text-surface-300 px-1.5 py-0.2 rounded-full"
            data-testid="queue-count-badge"
          >
            {{ totalCount }}
          </span>
        </div>

        <!-- Register Faculty Button -->
        <Button
          v-if="canRegister"
          size="small"
          severity="primary"
          class="!text-xs !py-1 !px-2.5 !gap-1.5 font-medium shrink-0"
          data-testid="register-faculty-button"
          @click="handleRegister"
        >
          <UserPlus class="w-3.5 h-3.5" />
          <span>+ Register Faculty</span>
        </Button>
      </div>

      <!-- Queue Category Filter Pills (Horizontal Scrollable) -->
      <div class="space-y-1">
        <span class="text-[10px] font-semibold tracking-wider uppercase text-surface-400 block">
          Filter by Category
        </span>
        <div
          class="flex items-center gap-1.5 overflow-x-auto pb-1 no-scrollbar text-xs"
          data-testid="queue-filter-pills"
        >
          <button
            v-for="pill in queueFilterPills"
            :key="pill.id"
            type="button"
            :class="[
              'px-2.5 py-1 rounded-full text-xs font-medium whitespace-nowrap transition-all select-none shrink-0 cursor-pointer',
              activeQueueFilter === pill.id
                ? 'bg-primary-600 text-white shadow-xs'
                : 'bg-surface-100 dark:bg-surface-800 text-surface-600 dark:text-surface-400 hover:bg-surface-200 dark:hover:bg-surface-700',
            ]"
            :data-testid="`queue-pill-${pill.id}`"
            @click="setQueueFilter(pill.id)"
          >
            {{ pill.label }}
          </button>
        </div>
      </div>

      <!-- Live Search Input with Debounce & Clear -->
      <div class="relative">
        <Search class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400 pointer-events-none" />
        <InputText
          v-model="searchInput"
          type="text"
          placeholder="Search name, teacher #, NIK..."
          class="w-full !pl-8 !pr-8 !py-1.5 !text-xs rounded-lg"
          data-testid="queue-search-input"
        />
        <button
          v-if="searchInput"
          type="button"
          class="absolute right-2.5 top-1/2 -translate-y-1/2 text-surface-400 hover:text-surface-600 dark:hover:text-surface-200"
          aria-label="Clear search"
          data-testid="clear-search-button"
          @click="clearSearch"
        >
          <X class="w-3.5 h-3.5" />
        </button>
      </div>

      <!-- Status Dropdown & Reset Bar -->
      <div class="flex items-center gap-2">
        <Select
          v-model="selectedStanding"
          :options="standingOptions"
          option-label="label"
          option-value="value"
          placeholder="Status"
          class="w-full !text-xs"
          data-testid="status-filter-select"
        />

        <Button
          v-if="searchInput || selectedStanding !== 'all' || activeQueueFilter !== 'all'"
          size="small"
          severity="secondary"
          variant="text"
          class="!p-1.5 text-surface-500 hover:text-surface-700 shrink-0"
          title="Reset filters"
          data-testid="reset-filters-button"
          @click="resetFilters"
        >
          <RotateCcw class="w-3.5 h-3.5" />
        </Button>
      </div>
    </div>

    <!-- Scrollable Teacher Cards Queue List -->
    <div
      class="flex-1 overflow-y-auto p-2.5 space-y-2 min-h-[300px]"
      data-testid="faculty-cards-list"
    >
      <!-- Loading State -->
      <div
        v-if="effectiveLoading"
        class="py-12 flex flex-col items-center justify-center text-center space-y-2"
        data-testid="queue-loading"
      >
        <LoadingSpinner size="md" color="teal" />
        <p class="text-xs text-surface-500">Loading faculty queue...</p>
      </div>

      <!-- Error State -->
      <div
        v-else-if="effectiveError"
        class="p-4 text-center space-y-2 bg-rose-50/50 dark:bg-rose-950/20 border border-rose-200 dark:border-rose-900 rounded-lg"
        data-testid="queue-error"
      >
        <AlertTriangle class="w-6 h-6 text-rose-500 mx-auto" />
        <p class="text-xs font-medium text-rose-700 dark:text-rose-300">
          Failed to load faculty records.
        </p>
        <Button
          size="small"
          severity="danger"
          variant="outlined"
          class="!text-xs !py-1 !px-2.5"
          data-testid="retry-queue-button"
          @click="queryResult.refetch()"
        >
          Retry
        </Button>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="facultyList.length === 0"
        class="py-8"
        data-testid="queue-empty"
      >
        <EmptyState
          title="No faculty found"
          description="Try adjusting your queue category filter, status, or search query."
          bordered
          class="!p-6"
        >
          <template #actions>
            <Button
              size="small"
              severity="secondary"
              class="!text-xs !py-1 !px-2.5"
              data-testid="empty-reset-filters-button"
              @click="resetFilters"
            >
              Reset Filters
            </Button>
          </template>
        </EmptyState>
      </div>

      <!-- Populated Teacher Cards -->
      <div
        v-for="teacher in facultyList"
        :key="teacher.id"
        :class="[
          'p-3 rounded-lg border transition-all cursor-pointer select-none space-y-2 relative',
          selectedTeacherId === teacher.id
            ? 'border-primary-500 bg-primary-50/70 dark:bg-primary-950/30 ring-1 ring-primary-500 shadow-xs'
            : 'border-surface-200 dark:border-surface-700 hover:border-surface-300 dark:hover:border-surface-600 bg-surface-0 dark:bg-surface-800/40 hover:bg-surface-50 dark:hover:bg-surface-800',
        ]"
        :data-testid="`faculty-card-${teacher.id}`"
        @click="handleSelect(teacher.id)"
      >
        <!-- Top Row: Teacher Number + Standing Badge -->
        <div class="flex items-center justify-between gap-2">
          <span
            class="font-mono text-[11px] font-semibold text-primary-700 dark:text-primary-300 bg-primary-100/70 dark:bg-primary-950 px-1.5 py-0.5 rounded border border-primary-200 dark:border-primary-800"
            data-testid="faculty-card-number"
          >
            {{ teacher.teacherNumber }}
          </span>

          <StatusPill
            :status="teacher.standing"
            size="sm"
            data-testid="faculty-card-standing"
          />
        </div>

        <!-- Middle: Full Name & Display Name -->
        <div class="space-y-0.5">
          <div
            class="font-semibold text-xs sm:text-sm text-surface-900 dark:text-surface-100 line-clamp-1"
            data-testid="faculty-card-name"
          >
            {{ teacher.displayName || teacher.fullName }}
          </div>
          <div
            v-if="teacher.displayName && teacher.displayName !== teacher.fullName"
            class="text-[11px] text-surface-500 dark:text-surface-400 line-clamp-1 italic"
            data-testid="faculty-card-legal-name"
          >
            Legal: {{ teacher.fullName }}
          </div>
        </div>

        <!-- Role Pill & Campus Affiliation -->
        <div class="flex flex-wrap items-center gap-1.5 text-[11px] text-surface-600 dark:text-surface-300">
          <span
            class="inline-flex items-center px-1.5 py-0.5 rounded bg-surface-100 dark:bg-surface-700 text-surface-700 dark:text-surface-300 font-medium"
            data-testid="faculty-card-role"
          >
            {{ teacher.teachingRoleName }}
          </span>

          <span
            class="inline-flex items-center gap-1 text-surface-500 dark:text-surface-400 truncate"
            data-testid="faculty-card-campus"
          >
            <Building2 class="w-3 h-3 text-surface-400 shrink-0" />
            <span class="truncate">{{ teacher.campusAffiliation }}</span>
          </span>
        </div>

        <!-- Bottom Indicators / Warning Badges (TD-05 Permissive Warning) -->
        <div
          v-if="teacher.hasUnverifiedWarning || teacher.activeAuthorizationsCount === 0"
          class="flex flex-wrap items-center gap-1.5 pt-1 border-t border-surface-200/60 dark:border-surface-700/60 text-[10px]"
          data-testid="faculty-card-indicators"
        >
          <!-- Unverified Credential Warning Indicator -->
          <span
            v-if="teacher.hasUnverifiedWarning"
            class="inline-flex items-center gap-1 text-amber-700 dark:text-amber-300 bg-amber-50 dark:bg-amber-950/70 border border-amber-200 dark:border-amber-800 px-1.5 py-0.5 rounded font-semibold"
            data-testid="faculty-card-unverified-warning"
            title="Provisional authorization granted with submitted but unverified qualification evidence"
          >
            <AlertTriangle class="w-3 h-3 text-amber-600 shrink-0" />
            Unverified Evidence
          </span>

          <!-- No Active Authorization Indicator (Only when active standing) -->
          <span
            v-if="teacher.standing === 'active' && teacher.activeAuthorizationsCount === 0"
            class="inline-flex items-center gap-1 text-sky-700 dark:text-sky-300 bg-sky-50 dark:bg-sky-950/70 border border-sky-200 dark:border-sky-800 px-1.5 py-0.5 rounded font-medium"
            data-testid="faculty-card-no-authorizations"
            title="Profile active but has no active teaching authorizations"
          >
            <ShieldAlert class="w-3 h-3 text-sky-600 shrink-0" />
            No Authorizations
          </span>

          <!-- Active Authorizations Count Pill -->
          <span
            v-else-if="teacher.activeAuthorizationsCount > 0"
            class="inline-flex items-center gap-1 text-emerald-700 dark:text-emerald-300 bg-emerald-50 dark:bg-emerald-950/70 border border-emerald-200 dark:border-emerald-800 px-1.5 py-0.5 rounded font-medium"
            data-testid="faculty-card-auth-count"
          >
            <FileCheck2 class="w-3 h-3 text-emerald-600 shrink-0" />
            {{ teacher.activeAuthorizationsCount }} {{ teacher.activeAuthorizationsCount === 1 ? 'Auth' : 'Auths' }}
          </span>
        </div>
      </div>
    </div>
  </aside>
</template>

<style scoped>
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
