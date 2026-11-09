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

const statusOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Active', value: 'active' },
  { label: 'Draft', value: 'draft' },
  { label: 'Revised / Archived', value: 'revised_archived' },
];
</script>

<template>
  <div class="flex flex-col flex-1 gap-6 p-4 md:p-6 max-w-[1600px] mx-auto w-full">
    <!-- Top Level Page Header -->
    <PageHeader
      title="Student Learning Plans"
      subtitle="Individualized academic pathways, exceptional curriculum adaptations, and authority governance."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <Button
          v-if="canManage"
          label="Formulate New Plan"
          icon="pi pi-plus"
          size="small"
          class="font-semibold text-xs shadow-sm"
          @click="isCreateModalOpen = true"
        />
      </template>
    </PageHeader>

    <!-- Master-Detail Layout Container -->
    <div
      class="flex flex-col md:flex-row flex-1 min-h-[720px] bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-hidden"
    >
      <!-- ================================================================= -->
      <!-- MASTER SIDEBAR (Left Pane, ~320-380px)                            -->
      <!-- ================================================================= -->
      <aside
        :class="[
          'w-full md:w-80 lg:w-88 xl:w-96 flex-shrink-0 flex flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 transition-all',
          showMobileDetail ? 'hidden md:flex' : 'flex',
        ]"
      >
        <!-- Sidebar Top Header & Actions -->
        <div class="p-3.5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between gap-2 bg-slate-50/50 dark:bg-slate-950/20">
          <div class="flex items-center gap-2 text-slate-800 dark:text-slate-200 font-semibold text-xs uppercase tracking-wider">
            <SlidersHorizontal class="w-4 h-4 text-indigo-500" />
            <span>Learning Plans ({{ filteredPlans.length }})</span>
          </div>
          <Button
            v-if="canManage"
            label="New Plan"
            icon="pi pi-plus"
            size="small"
            severity="primary"
            class="text-xs px-2.5 py-1 font-semibold"
            @click="isCreateModalOpen = true"
          />
        </div>

        <!-- Sidebar Filter Controls -->
        <div class="p-3.5 space-y-3 border-b border-slate-100 dark:border-slate-800 bg-slate-50/30 dark:bg-slate-950/10">
          <!-- 1. Academic Period Dropdown -->
          <div class="space-y-1">
            <label class="block text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              Academic Period
            </label>
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

          <!-- 2. Status Filter Dropdown -->
          <div class="space-y-1">
            <label class="block text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
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
            <label class="block text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              Search Student
            </label>
            <div class="relative">
              <Search class="absolute left-3 top-2.5 w-3.5 h-3.5 text-slate-400" />
              <InputText
                v-model="searchQuery"
                placeholder="Filter by student name or NIS..."
                class="w-full pl-8 text-xs py-1.5"
              />
            </div>
          </div>
        </div>

        <!-- Scrollable Student Plans List -->
        <div class="flex-1 overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800/60">
          <!-- Loading State -->
          <div v-if="isLoadingPlans" class="py-12 flex flex-col items-center justify-center gap-2 text-xs text-slate-400">
            <LoadingSpinner size="sm" />
            <span>Loading learning plans...</span>
          </div>

          <!-- Empty State -->
          <div
            v-else-if="filteredPlans.length === 0"
            class="p-6 text-center space-y-3"
          >
            <EmptyState
              title="No learning plans found"
              description="No students match the selected period, status, or search query."
            />
            <Button
              v-if="canManage"
              label="Formulate New Plan"
              icon="pi pi-plus"
              size="small"
              text
              class="text-xs"
              @click="isCreateModalOpen = true"
            />
          </div>

          <!-- Student Plan Cards -->
          <div
            v-for="plan in filteredPlans"
            v-else
            :key="plan.id"
            :class="[
              'p-3.5 cursor-pointer transition-all border-l-4 text-left select-none',
              plan.id === selectedPlanId
                ? 'border-indigo-600 bg-indigo-50/70 dark:bg-indigo-950/40'
                : 'border-transparent hover:bg-slate-50/80 dark:hover:bg-slate-800/40',
            ]"
            @click="handleSelectPlan(plan.id)"
          >
            <div class="flex items-start justify-between gap-2">
              <div class="flex items-center gap-2.5 min-w-0">
                <!-- Student Avatar -->
                <div
                  :class="[
                    'w-9 h-9 rounded-full flex items-center justify-center font-bold text-xs flex-shrink-0 shadow-xs',
                    plan.id === selectedPlanId
                      ? 'bg-indigo-600 text-white'
                      : 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-200',
                  ]"
                >
                  {{ (plan.studentName || 'S').charAt(0).toUpperCase() }}
                </div>
                <div class="min-w-0">
                  <div
                    :class="[
                      'text-xs font-semibold truncate',
                      plan.id === selectedPlanId
                        ? 'text-indigo-950 dark:text-indigo-100'
                        : 'text-slate-900 dark:text-slate-100',
                    ]"
                  >
                    {{ plan.studentName || 'Student Name' }}
                  </div>
                  <div class="text-[11px] text-slate-500 dark:text-slate-400 truncate flex items-center gap-1.5">
                    <span>NIS: {{ plan.studentNis || '—' }}</span>
                    <template v-if="plan.gradeLevel">
                      <span>•</span>
                      <span>{{ plan.gradeLevel }}</span>
                    </template>
                  </div>
                </div>
              </div>

              <!-- Status Tag Badge -->
              <Tag
                :value="getStatusLabel(plan.status, plan.version)"
                :severity="getStatusTagSeverity(plan.status)"
                class="text-[10px] font-bold tracking-tight px-1.5 py-0.5 flex-shrink-0"
              />
            </div>

            <!-- Context Line (Units, Adjustments, Period) -->
            <div class="mt-2.5 flex items-center justify-between text-[11px] text-slate-500 dark:text-slate-400">
              <span class="truncate max-w-[170px]">
                {{ plan.academicPeriodLabel }}
              </span>
              <span class="flex items-center gap-1.5 font-medium flex-shrink-0">
                <span>{{ plan.unitsCount }} units</span>
                <span>•</span>
                <span>{{ plan.adjustmentsCount }} adj.</span>
              </span>
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
        <!-- Mobile Return Button (<md) -->
        <div class="md:hidden p-3 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex items-center gap-2">
          <Button
            icon="pi pi-arrow-left"
            label="Back to Student List"
            text
            size="small"
            class="text-xs"
            @click="handleBackToSidebar"
          />
        </div>

        <!-- No Selection State -->
        <div
          v-if="!selectedPlanId"
          class="flex-1 flex flex-col items-center justify-center p-8 text-center"
        >
          <div class="w-16 h-16 rounded-2xl bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center mb-4 shadow-xs">
            <BookOpen class="w-8 h-8" />
          </div>
          <h3 class="text-base font-semibold text-slate-800 dark:text-slate-200">
            No Student Learning Plan Selected
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400 max-w-md mt-1 mb-6">
            Select a student from the sidebar roster to inspect constituent units, academic adjustments, and approval governance history, or formulate a new plan.
          </p>
          <Button
            v-if="canManage"
            label="Formulate New Plan"
            icon="pi pi-plus"
            size="small"
            class="text-xs font-semibold"
            @click="isCreateModalOpen = true"
          />
        </div>

        <!-- Loading Detail State -->
        <div
          v-else-if="isLoadingDetail"
          class="flex-1 flex flex-col items-center justify-center p-12 gap-3 text-xs text-slate-400"
        >
          <LoadingSpinner size="md" />
          <span>Loading plan workspace details...</span>
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
              <TabList class="border-b border-slate-200 dark:border-slate-800 mb-4 bg-transparent">
                <Tab value="units" class="flex items-center gap-2 text-xs font-semibold py-2.5 px-4">
                  <BookOpen class="w-4 h-4" />
                  <span>Constituent Units</span>
                  <span class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-indigo-100 dark:bg-indigo-900/60 text-indigo-700 dark:text-indigo-300 font-bold">
                    {{ planDetail.constituentUnits?.length || 0 }}
                  </span>
                </Tab>
                <Tab value="adjustments" class="flex items-center gap-2 text-xs font-semibold py-2.5 px-4">
                  <Award class="w-4 h-4" />
                  <span>Plan Adjustments</span>
                  <span class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-purple-100 dark:bg-purple-900/60 text-purple-700 dark:text-purple-300 font-bold">
                    {{ planDetail.adjustments?.length || 0 }}
                  </span>
                </Tab>
                <Tab value="history" class="flex items-center gap-2 text-xs font-semibold py-2.5 px-4">
                  <History class="w-4 h-4" />
                  <span>Approval History & Versions</span>
                  <span class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold">
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
