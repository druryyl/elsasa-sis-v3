<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import { refDebounced } from '@vueuse/core';
import {
  Search,
  Plus,
  Filter,
  Layers,
  Calendar,
  Award,
  ShieldCheck,
  FileText,
  CheckCircle2,
  Clock,
  ArrowLeft,
  AlertCircle,
  Sparkles,
  BookOpen,
  User,
  GraduationCap,
  History,
  SlidersHorizontal,
  X,
  RotateCcw,
  Users,
} from 'lucide-vue-next';
import {
  PageHeader,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAcademicYearsQuery } from '@/features/operations/calendar/composables/useAcademicYearsQuery';
import { useOperationalPeriodsQuery } from '@/features/operations/calendar/composables/useOperationalPeriodsQuery';
import {
  useLearningPlans,
  useLearningPlanDetail,
} from '../composables/useLearningPlans';
import { LEARNING_PLAN_PERMISSIONS } from '../constants/permissions';
import type {
  StudentLearningPlanListItem,
  StudentLearningPlanStatus,
} from '../types';
import CreatePlanModal, {
  type AcademicPeriodOption,
} from '../components/CreatePlanModal.vue';
import SlpHeaderBanner from '../components/SlpHeaderBanner.vue';
import SlpLearningUnitsTab from '../components/SlpLearningUnitsTab.vue';
import SlpAdjustmentsTab from '../components/SlpAdjustmentsTab.vue';
import SlpApprovalHistoryTab from '../components/SlpApprovalHistoryTab.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Student Learning Plans', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Role Guards
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canManage = computed(() =>
  authStore.hasPermission(LEARNING_PLAN_PERMISSIONS.MANAGE),
);
const canApprove = computed(() =>
  authStore.hasPermission(LEARNING_PLAN_PERMISSIONS.APPROVE),
);
const canRevise = computed(() =>
  authStore.hasPermission(LEARNING_PLAN_PERMISSIONS.REVISE),
);

// ---------------------------------------------------------------------------
// 3. Selection & Context Filter State
// ---------------------------------------------------------------------------
const appContextStore = useAppContextStore();
const selectedPeriodId = ref<string | null>(null);
const statusFilter = ref<string>('all');
const searchQuery = ref<string>('');
const debouncedSearch = refDebounced(searchQuery, 250);

const selectedPlanId = ref<string | null>(null);
const isCreateModalOpen = ref<boolean>(false);
const activeTab = ref<string>('units');

// Mobile view toggle state (sidebar vs detail on small viewports)
const showMobileDetail = ref<boolean>(false);

// ---------------------------------------------------------------------------
// 4. Academic Periods Query
// ---------------------------------------------------------------------------
const { data: academicYears, isLoading: isLoadingAcademicYears } = useAcademicYearsQuery();

// Active or primary academic year for querying operational periods (semesters)
const activeAcademicYear = computed(() => {
  if (!academicYears.value || academicYears.value.length === 0) return null;
  return academicYears.value.find((y) => y.status === 'active') ?? academicYears.value[0];
});

const { data: operationalPeriods, isLoading: isLoadingOperationalPeriods } = useOperationalPeriodsQuery(
  () => activeAcademicYear.value?.id,
  {
    enabled: computed(() => Boolean(activeAcademicYear.value?.id)),
  },
);

const isLoadingPeriods = computed(
  () => isLoadingAcademicYears.value || isLoadingOperationalPeriods.value,
);

// Options for Formulate Plan modal (excludes filter empty option)
const modalPeriodOptions = computed<AcademicPeriodOption[]>(() => {
  const options: AcademicPeriodOption[] = [];

  // 1. Include operational periods (semesters) if available
  if (operationalPeriods.value && operationalPeriods.value.length > 0) {
    for (const p of operationalPeriods.value) {
      options.push({
        id: p.id,
        label: p.name,
        code: p.periodCode,
      });
    }
  }

  // 2. Include academic years
  if (academicYears.value && academicYears.value.length > 0) {
    for (const ay of academicYears.value) {
      if (!options.some((o) => o.id === ay.id)) {
        options.push({
          id: ay.id,
          label: `${ay.name} (${ay.code})`,
          code: ay.code,
        });
      }
    }
  }

  return options;
});

// Options for Master Sidebar filter dropdown
const periodOptions = computed<AcademicPeriodOption[]>(() => {
  const options: AcademicPeriodOption[] = [
    { id: '', label: 'All Academic Periods' },
  ];
  return options.concat(modalPeriodOptions.value);
});

// Auto-select initial academic period from appContext or default to all
watch(
  () => periodOptions.value,
  (options) => {
    if (options.length > 0 && selectedPeriodId.value === null) {
      if (
        appContextStore.activeAcademicPeriodId &&
        options.some((o) => o.id === appContextStore.activeAcademicPeriodId)
      ) {
        selectedPeriodId.value = appContextStore.activeAcademicPeriodId;
      } else {
        selectedPeriodId.value = '';
      }
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 5. Learning Plans Roster Query (Master Sidebar)
// ---------------------------------------------------------------------------
const statusFilterForBackend = computed(() => {
  if (statusFilter.value === 'all' || statusFilter.value === 'revised_archived') {
    return undefined;
  }
  return statusFilter.value;
});

const queryFilters = computed(() => ({
  academicPeriodId: selectedPeriodId.value || undefined,
  status: statusFilterForBackend.value,
  studentSearch: debouncedSearch.value.trim() || undefined,
  page: 1,
  pageSize: 100,
}));

const {
  data: learningPlansData,
  isLoading: isLoadingPlans,
  refetch: refetchPlans,
} = useLearningPlans(queryFilters);

// Filter plans list locally if 'revised_archived' compound filter is selected
const rawPlansList = computed<StudentLearningPlanListItem[]>(
  () => learningPlansData.value?.items ?? [],
);

const filteredPlans = computed<StudentLearningPlanListItem[]>(() => {
  if (statusFilter.value === 'revised_archived') {
    return rawPlansList.value.filter(
      (p) => p.status === 'revised' || p.status === 'archived',
    );
  }
  return rawPlansList.value;
});

// Summary metrics computed across all fetched plans in the roster
const summaryMetrics = computed(() => {
  const all = rawPlansList.value;
  const total = all.length;
  const active = all.filter((p) => p.status === 'active').length;
  const draft = all.filter((p) => p.status === 'draft').length;
  const revisedOrArchived = all.filter(
    (p) => p.status === 'revised' || p.status === 'archived',
  ).length;
  return { total, active, draft, revisedOrArchived };
});

const isFiltered = computed(() => {
  return (
    Boolean(searchQuery.value.trim()) ||
    statusFilter.value !== 'all' ||
    Boolean(selectedPeriodId.value)
  );
});

function clearFilters() {
  searchQuery.value = '';
  statusFilter.value = 'all';
  selectedPeriodId.value = '';
}

// Auto-select first plan when list loads and none currently selected
watch(
  () => filteredPlans.value,
  (list) => {
    if (list.length > 0) {
      const alreadySelected = list.some((p) => p.id === selectedPlanId.value);
      if (!alreadySelected) {
        // Prefer active plan, otherwise first plan
        const activePlan = list.find((p) => p.status === 'active');
        selectedPlanId.value = activePlan ? activePlan.id : list[0].id;
      }
    } else {
      selectedPlanId.value = null;
      showMobileDetail.value = false;
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 6. Selected Plan Detail Query
// ---------------------------------------------------------------------------
const planIdRef = computed(() => selectedPlanId.value ?? '');
const {
  data: planDetail,
  isLoading: isLoadingDetail,
  error: detailError,
  refetch: refetchDetail,
} = useLearningPlanDetail(planIdRef, {
  enabled: computed(() => Boolean(selectedPlanId.value)),
});

// ---------------------------------------------------------------------------
// 7. Interaction Handlers
// ---------------------------------------------------------------------------
function handleSelectPlan(id: string) {
  selectedPlanId.value = id;
  showMobileDetail.value = true;
}

function handleBackToSidebar() {
  showMobileDetail.value = false;
}

function handlePlanCreated(newPlanId: string) {
  selectedPlanId.value = newPlanId;
  showMobileDetail.value = true;
  refetchPlans();
}

function handlePlanAction() {
  refetchDetail();
  refetchPlans();
}

function handlePlanRevised(newPlanId: string) {
  selectedPlanId.value = newPlanId;
  refetchPlans();
}

// ---------------------------------------------------------------------------
// 8. Visual Presentation Helpers
// ---------------------------------------------------------------------------
function getStatusTagSeverity(
  status: StudentLearningPlanStatus,
): 'success' | 'warn' | 'info' | 'contrast' | 'secondary' {
  switch (status) {
    case 'active':
      return 'success';
    case 'draft':
      return 'warn';
    case 'revised':
      return 'info';
    case 'completed':
      return 'contrast';
    case 'archived':
    default:
      return 'secondary';
  }
}

function getStatusLabel(status: StudentLearningPlanStatus, version?: number): string {
  const versionSuffix = version ? ` v${version}` : '';
  switch (status) {
    case 'active':
      return `Active${versionSuffix}`;
    case 'draft':
      return `Draft${versionSuffix}`;
    case 'revised':
      return `Revised${versionSuffix}`;
    case 'completed':
      return `Completed${versionSuffix}`;
    case 'archived':
      return `Archived${versionSuffix}`;
    default:
      return status;
  }
}

function getAvatarColorClass(status: StudentLearningPlanStatus, isSelected: boolean): string {
  if (isSelected) {
    return 'bg-gradient-to-br from-indigo-600 to-indigo-700 text-white shadow-xs';
  }
  switch (status) {
    case 'active':
      return 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200/80 dark:border-emerald-800/80';
    case 'draft':
      return 'bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200/80 dark:border-amber-800/80';
    case 'revised':
      return 'bg-sky-50 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 border border-sky-200/80 dark:border-sky-800/80';
    default:
      return 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700';
  }
}

const statusOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Active', value: 'active' },
  { label: 'Draft', value: 'draft' },
  { label: 'Revised / Archived', value: 'revised_archived' },
];
</script>

<template>
  <div class="flex flex-col flex-1 gap-5 p-4 md:p-6 max-w-[1600px] mx-auto w-full">
    <!-- Top Level Page Header -->
    <PageHeader
      title="Student Learning Plans"
      subtitle="Individualized academic pathways, exceptional curriculum adaptations, and authority governance."
      :breadcrumbs="breadcrumbs"
      class="pb-2"
    >
      <template #title-extra>
        <span
          class="hidden sm:inline-flex items-center px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 border border-slate-200/80 dark:border-slate-700/80 text-[11px] font-mono font-medium text-slate-500 dark:text-slate-400 shrink-0"
        >
          SCR-ACD-04
        </span>
      </template>

      <template #actions>
        <Button
          v-if="canManage"
          label="Formulate New Plan"
          icon="pi pi-plus"
          size="small"
          class="font-semibold text-xs shadow-xs transition-all duration-150"
          @click="isCreateModalOpen = true"
        />
      </template>
    </PageHeader>

    <!-- Top Summary Metrics Strip -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <!-- Total Plans -->
      <div
        class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200/80 dark:border-slate-800/80 p-3.5 flex items-center justify-between shadow-2xs hover:border-slate-300 dark:hover:border-slate-700 transition-colors"
      >
        <div class="space-y-0.5">
          <p class="text-xs font-medium text-slate-500 dark:text-slate-400">Total Plans</p>
          <p class="text-xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            {{ summaryMetrics.total }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-xl bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shrink-0">
          <BookOpen class="w-5 h-5" />
        </div>
      </div>

      <!-- Active Plans -->
      <div
        class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200/80 dark:border-slate-800/80 p-3.5 flex items-center justify-between shadow-2xs hover:border-emerald-300 dark:hover:border-emerald-800 transition-colors cursor-pointer"
        @click="statusFilter = 'active'"
      >
        <div class="space-y-0.5">
          <div class="flex items-center gap-1.5">
            <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
            <p class="text-xs font-medium text-slate-500 dark:text-slate-400">Active Pathways</p>
          </div>
          <p class="text-xl font-bold tracking-tight text-emerald-600 dark:text-emerald-400">
            {{ summaryMetrics.active }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0">
          <CheckCircle2 class="w-5 h-5" />
        </div>
      </div>

      <!-- In Formulation (Draft) -->
      <div
        class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200/80 dark:border-slate-800/80 p-3.5 flex items-center justify-between shadow-2xs hover:border-amber-300 dark:hover:border-amber-800 transition-colors cursor-pointer"
        @click="statusFilter = 'draft'"
      >
        <div class="space-y-0.5">
          <div class="flex items-center gap-1.5">
            <span class="w-2 h-2 rounded-full bg-amber-500"></span>
            <p class="text-xs font-medium text-slate-500 dark:text-slate-400">Draft / Formulation</p>
          </div>
          <p class="text-xl font-bold tracking-tight text-amber-600 dark:text-amber-400">
            {{ summaryMetrics.draft }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-xl bg-amber-50 dark:bg-amber-950/50 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0">
          <FileText class="w-5 h-5" />
        </div>
      </div>

      <!-- Revised / History -->
      <div
        class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200/80 dark:border-slate-800/80 p-3.5 flex items-center justify-between shadow-2xs hover:border-sky-300 dark:hover:border-sky-800 transition-colors cursor-pointer"
        @click="statusFilter = 'revised_archived'"
      >
        <div class="space-y-0.5">
          <p class="text-xs font-medium text-slate-500 dark:text-slate-400">Revised / Archived</p>
          <p class="text-xl font-bold tracking-tight text-slate-700 dark:text-slate-300">
            {{ summaryMetrics.revisedOrArchived }}
          </p>
        </div>
        <div class="w-10 h-10 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 flex items-center justify-center shrink-0">
          <History class="w-5 h-5" />
        </div>
      </div>
    </div>

    <!-- Master-Detail Layout Frame -->
    <div
      class="flex flex-col md:flex-row flex-1 min-h-[720px] bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-sm overflow-hidden"
    >
      <!-- ================================================================= -->
      <!-- MASTER SIDEBAR (Left Pane)                                        -->
      <!-- ================================================================= -->
      <aside
        :class="[
          'w-full md:w-80 lg:w-88 xl:w-96 flex-shrink-0 flex flex-col border-r border-slate-200/80 dark:border-slate-800 bg-white dark:bg-slate-900 transition-all',
          showMobileDetail ? 'hidden md:flex' : 'flex',
        ]"
      >
        <!-- Sidebar Top Header -->
        <div
          class="p-4 border-b border-slate-100 dark:border-slate-800/80 flex items-center justify-between gap-2 bg-slate-50/60 dark:bg-slate-950/30"
        >
          <div class="flex items-center gap-2.5 text-slate-900 dark:text-slate-100 font-semibold text-xs uppercase tracking-wider">
            <div class="p-1.5 rounded-lg bg-indigo-50 dark:bg-indigo-950/60 text-indigo-600 dark:text-indigo-400">
              <SlidersHorizontal class="w-4 h-4" />
            </div>
            <span>Learning Plans ({{ filteredPlans.length }})</span>
          </div>
          <Button
            v-if="canManage"
            label="New Plan"
            icon="pi pi-plus"
            size="small"
            severity="primary"
            class="text-xs px-2.5 py-1 font-semibold shadow-2xs"
            @click="isCreateModalOpen = true"
          />
        </div>

        <!-- Sidebar Filter Controls -->
        <div class="p-3.5 space-y-3 border-b border-slate-100 dark:border-slate-800/80 bg-slate-50/30 dark:bg-slate-950/10">
          <!-- 1. Academic Period Dropdown (Preserved for compatibility) -->
          <div class="space-y-1">
            <div class="flex items-center justify-between">
              <label class="block text-xs font-semibold text-slate-600 dark:text-slate-300">
                Academic Period
              </label>
            </div>
            <div class="relative">
              <Select
                v-model="selectedPeriodId"
                :options="periodOptions"
                option-label="label"
                option-value="id"
                placeholder="Select Academic Period"
                class="w-full text-xs"
                :disabled="isLoadingPeriods || periodOptions.length === 0"
              />
            </div>
          </div>

          <!-- 2. Status Filter Dropdown (Preserved for compatibility) -->
          <div class="space-y-1">
            <label class="block text-xs font-semibold text-slate-600 dark:text-slate-300">
              Status Filter
            </label>
            <Select
              v-model="statusFilter"
              :options="statusOptions"
              option-label="label"
              option-value="value"
              placeholder="All Statuses"
              class="w-full text-xs"
            />
          </div>

          <!-- 3. Search Input -->
          <div class="space-y-1">
            <div class="flex items-center justify-between">
              <label class="block text-xs font-semibold text-slate-600 dark:text-slate-300">
                Search Student
              </label>
              <button
                v-if="isFiltered"
                type="button"
                class="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 font-medium inline-flex items-center gap-1 transition-colors"
                @click="clearFilters"
              >
                <RotateCcw class="w-3 h-3" />
                <span>Reset</span>
              </button>
            </div>
            <div class="relative flex items-center">
              <Search class="absolute left-3 w-3.5 h-3.5 text-slate-400 pointer-events-none" />
              <InputText
                v-model="searchQuery"
                placeholder="Filter by student name or NIS..."
                class="w-full pl-8.5 pr-8 text-xs py-1.5 rounded-lg border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
              />
              <button
                v-if="searchQuery"
                type="button"
                class="absolute right-2.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
                @click="searchQuery = ''"
              >
                <X class="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        </div>

        <!-- Scrollable Student Plans List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1.5 divide-y-0">
          <!-- Loading State -->
          <div v-if="isLoadingPlans" class="py-16 flex flex-col items-center justify-center gap-2.5 text-xs text-slate-400">
            <LoadingSpinner size="sm" />
            <span class="font-medium">Loading learning plans...</span>
          </div>

          <!-- Empty State -->
          <div
            v-else-if="filteredPlans.length === 0"
            class="p-6 text-center space-y-3.5 my-4"
          >
            <div class="w-12 h-12 rounded-2xl bg-slate-100 dark:bg-slate-800 text-slate-400 flex items-center justify-center mx-auto">
              <Users class="w-6 h-6" />
            </div>
            <EmptyState
              title="No learning plans found"
              description="No students match the selected period, status, or search query."
            />
            <div class="flex items-center justify-center gap-2 pt-1">
              <Button
                v-if="isFiltered"
                label="Clear Filters"
                icon="pi pi-filter-slash"
                size="small"
                outlined
                severity="secondary"
                class="text-xs"
                @click="clearFilters"
              />
              <Button
                v-if="canManage"
                label="Formulate New Plan"
                icon="pi pi-plus"
                size="small"
                class="text-xs font-semibold"
                @click="isCreateModalOpen = true"
              />
            </div>
          </div>

          <!-- Student Plan Cards -->
          <div
            v-for="plan in filteredPlans"
            v-else
            :key="plan.id"
            :class="[
              'p-3 rounded-xl cursor-pointer transition-all duration-150 border text-left select-none relative group',
              plan.id === selectedPlanId
                ? 'bg-indigo-50/80 dark:bg-indigo-950/50 border-indigo-300 dark:border-indigo-700/80 shadow-xs ring-1 ring-indigo-500/20'
                : 'bg-white dark:bg-slate-900 border-slate-100 dark:border-slate-800/80 hover:border-slate-200 dark:hover:border-slate-700 hover:bg-slate-50/70 dark:hover:bg-slate-800/40',
            ]"
            @click="handleSelectPlan(plan.id)"
          >
            <!-- Left Active Indicator Accent Bar -->
            <div
              v-if="plan.id === selectedPlanId"
              class="absolute left-0 top-3 bottom-3 w-1 bg-indigo-600 rounded-r-full"
            />

            <!-- Top Row: Avatar + Name/NIS + Status Tag -->
            <div class="flex items-start justify-between gap-2.5">
              <div class="flex items-center gap-2.5 min-w-0">
                <!-- Stylized Avatar -->
                <div
                  :class="[
                    'w-9 h-9 rounded-xl flex items-center justify-center font-bold text-xs flex-shrink-0 transition-transform duration-150 group-hover:scale-105',
                    getAvatarColorClass(plan.status, plan.id === selectedPlanId),
                  ]"
                >
                  {{ (plan.studentName || 'S').charAt(0).toUpperCase() }}
                </div>

                <div class="min-w-0">
                  <div
                    :class="[
                      'text-xs font-semibold truncate leading-tight',
                      plan.id === selectedPlanId
                        ? 'text-indigo-950 dark:text-indigo-100 font-bold'
                        : 'text-slate-900 dark:text-slate-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors',
                    ]"
                  >
                    {{ plan.studentName || 'Student Name' }}
                  </div>
                  <div class="text-xs text-slate-500 dark:text-slate-400 truncate flex items-center gap-1.5 mt-0.5">
                    <span class="font-mono">NIS: {{ plan.studentNis || '—' }}</span>
                    <template v-if="plan.gradeLevel">
                      <span class="text-slate-300 dark:text-slate-600">•</span>
                      <span class="font-medium text-slate-600 dark:text-slate-300">{{ plan.gradeLevel }}</span>
                    </template>
                  </div>
                </div>
              </div>

              <!-- Status Tag Badge -->
              <Tag
                :value="getStatusLabel(plan.status, plan.version)"
                :severity="getStatusTagSeverity(plan.status)"
                class="text-xs font-bold tracking-tight px-2 py-0.5 flex-shrink-0 shadow-2xs"
              />
            </div>

            <!-- Bottom Row: Academic Period & Adaptation Badges -->
            <div class="mt-2.5 pt-2 border-t border-slate-100 dark:border-slate-800/60 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
              <span class="truncate max-w-[160px] flex items-center gap-1">
                <Calendar class="w-3 h-3 text-slate-400 shrink-0" />
                <span class="truncate">{{ plan.academicPeriodLabel }}</span>
              </span>

              <!-- Micro metrics indicators -->
              <div class="flex items-center gap-2 font-medium flex-shrink-0">
                <span
                  class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 font-semibold text-xs"
                  title="Constituent Learning Units"
                >
                  <BookOpen class="w-3 h-3 text-indigo-500" />
                  <span>{{ plan.unitsCount }}</span>
                </span>
                <span
                  class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 font-semibold text-xs"
                  title="Approved Adjustments"
                >
                  <Award class="w-3 h-3 text-purple-500" />
                  <span>{{ plan.adjustmentsCount }}</span>
                </span>
              </div>
            </div>
          </div>
        </div>
      </aside>

      <!-- ================================================================= -->
      <!-- DETAIL WORKSPACE (Right Pane)                                     -->
      <!-- ================================================================= -->
      <main
        :class="[
          'flex-1 flex flex-col min-w-0 bg-slate-50/50 dark:bg-slate-950/30 overflow-y-auto',
          !showMobileDetail ? 'hidden md:flex' : 'flex',
        ]"
      >
        <!-- Mobile Return Navigation Bar (<md) -->
        <div class="md:hidden p-3 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <Button
            icon="pi pi-arrow-left"
            label="Back to Student List"
            text
            size="small"
            class="text-xs font-semibold"
            @click="handleBackToSidebar"
          />
          <span
            v-if="planDetail"
            class="text-xs font-semibold text-slate-700 dark:text-slate-300 truncate max-w-[180px]"
          >
            {{ planDetail.studentName }}
          </span>
        </div>

        <!-- No Selection State -->
        <div
          v-if="!selectedPlanId"
          class="flex-1 flex flex-col items-center justify-center p-8 text-center"
        >
          <div class="relative mb-5">
            <div class="w-20 h-20 rounded-3xl bg-gradient-to-tr from-indigo-100 to-indigo-50 dark:from-indigo-950/70 dark:to-indigo-900/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shadow-sm">
              <BookOpen class="w-10 h-10" />
            </div>
            <div class="absolute -bottom-1 -right-1 w-7 h-7 rounded-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 flex items-center justify-center text-indigo-600 shadow-2xs">
              <Sparkles class="w-3.5 h-3.5" />
            </div>
          </div>
          <h3 class="text-base font-bold text-slate-800 dark:text-slate-200 tracking-tight">
            No Student Learning Plan Selected
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400 max-w-md mt-1.5 mb-6 leading-relaxed">
            Select a student from the sidebar roster to inspect constituent units, academic adjustments, and approval governance history, or formulate a new plan.
          </p>
          <Button
            v-if="canManage"
            label="Formulate New Plan"
            icon="pi pi-plus"
            size="small"
            class="text-xs font-semibold shadow-xs"
            @click="isCreateModalOpen = true"
          />
        </div>

        <!-- Loading Detail State -->
        <div
          v-else-if="isLoadingDetail"
          class="flex-1 flex flex-col items-center justify-center p-12 gap-3 text-xs text-slate-400"
        >
          <LoadingSpinner size="md" />
          <span class="font-medium">Loading plan workspace details...</span>
        </div>

        <!-- Detail Loaded State -->
        <div v-else-if="planDetail" class="flex-1 flex flex-col min-w-0">
          <!-- 1. Detail Header Banner Container -->
          <SlpHeaderBanner
            :plan="planDetail"
            @plan-submitted="handlePlanAction"
            @plan-approved="handlePlanAction"
            @plan-revised="handlePlanRevised"
            @participations-synced="handlePlanAction"
            @plan-archived="handlePlanAction"
          />

          <!-- 2. Workspace Navigation Tabs -->
          <div class="flex-1 flex flex-col p-4 md:p-6 min-h-0">
            <Tabs v-model:value="activeTab" class="flex-1 flex flex-col">
              <TabList class="border-b border-slate-200/90 dark:border-slate-800 mb-5 bg-transparent gap-2">
                <!-- Tab 1: Units -->
                <Tab
                  value="units"
                  class="flex items-center gap-2 text-xs font-semibold py-2.5 px-4 rounded-t-lg transition-all"
                >
                  <BookOpen class="w-4 h-4 text-indigo-500" />
                  <span>Constituent Units</span>
                  <span
                    class="ml-1 px-2 py-0.5 rounded-full text-xs bg-indigo-100 dark:bg-indigo-900/60 text-indigo-700 dark:text-indigo-300 font-bold"
                  >
                    {{ planDetail.constituentUnits?.length || 0 }}
                  </span>
                </Tab>

                <!-- Tab 2: Adjustments -->
                <Tab
                  value="adjustments"
                  class="flex items-center gap-2 text-xs font-semibold py-2.5 px-4 rounded-t-lg transition-all"
                >
                  <Award class="w-4 h-4 text-purple-500" />
                  <span>Plan Adjustments</span>
                  <span
                    class="ml-1 px-2 py-0.5 rounded-full text-xs bg-purple-100 dark:bg-purple-900/60 text-purple-700 dark:text-purple-300 font-bold"
                  >
                    {{ planDetail.adjustments?.length || 0 }}
                  </span>
                </Tab>

                <!-- Tab 3: History & Versions -->
                <Tab
                  value="history"
                  class="flex items-center gap-2 text-xs font-semibold py-2.5 px-4 rounded-t-lg transition-all"
                >
                  <History class="w-4 h-4 text-slate-500" />
                  <span>Approval History &amp; Versions</span>
                  <span
                    class="ml-1 px-2 py-0.5 rounded-full text-xs bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold"
                  >
                    {{ planDetail.approvals?.length || 0 }}
                  </span>
                </Tab>
              </TabList>

              <TabPanels class="flex-1 bg-transparent p-0">
                <!-- Tab 1: Constituent Units Tab (SCR-ACD-04, P7-S21) -->
                <TabPanel value="units" class="p-0">
                  <SlpLearningUnitsTab
                    :plan="planDetail"
                    @unit-added="refetchDetail"
                    @unit-removed="refetchDetail"
                  />
                </TabPanel>

                <!-- Tab 2: Academic Plan Adjustments Tab (SCR-ACD-04, P7-S22) -->
                <TabPanel value="adjustments" class="p-0">
                  <SlpAdjustmentsTab
                    :plan="planDetail"
                    @adjustment-added="refetchDetail"
                    @adjustment-removed="refetchDetail"
                  />
                </TabPanel>

                <!-- Tab 3: History & Governance Tab Container -->
                <TabPanel value="history" class="p-0">
                  <SlpApprovalHistoryTab
                    :plan="planDetail"
                    :all-plans="rawPlansList"
                    @select-plan="handleSelectPlan"
                  />
                </TabPanel>
              </TabPanels>
            </Tabs>
          </div>
        </div>
      </main>
    </div>

    <!-- Create / Formulate New Plan Modal -->
    <CreatePlanModal
      v-model:visible="isCreateModalOpen"
      :periods="modalPeriodOptions"
      :initial-period-id="selectedPeriodId"
      @created="handlePlanCreated"
    />
  </div>
</template>
