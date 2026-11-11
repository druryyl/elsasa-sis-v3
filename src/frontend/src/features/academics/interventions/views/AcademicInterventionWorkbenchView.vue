<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  AlertTriangle,
  ArrowRight,
  BookOpen,
  Calendar,
  CheckCircle2,
  Clock,
  ExternalLink,
  Eye,
  Filter,
  GraduationCap,
  Layers,
  Plus,
  RefreshCw,
  Search,
  ShieldAlert,
  Sparkles,
  UserCheck,
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
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import {
  useActiveInterventionsQuery,
  useAtRiskLearnersQuery,
} from '../composables/useAcademicInterventions';
import { INTERVENTION_PERMISSIONS, INTERVENTION_ROLES } from '../constants/permissions';
import type {
  AcademicIntervention,
  AtRiskLearner,
  InterventionStatus,
  TriggerType,
} from '../types';
import InterventionPlanModal, {
  type InterventionPlanInitialData,
} from '../components/InterventionPlanModal.vue';
import InterventionDetailDrawer from '../components/InterventionDetailDrawer.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Academic Interventions', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Role Guards (SCR-ACD-06, ACD-WF-10, UC-ACD-11)
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const appContextStore = useAppContextStore();

const isAuthorizedUser = computed(() =>
  authStore.hasAnyRole([...INTERVENTION_ROLES]) ||
  authStore.hasPermission(INTERVENTION_PERMISSIONS.VIEW),
);

const canInitiate = computed(() =>
  authStore.hasPermission(INTERVENTION_PERMISSIONS.INITIATE) ||
  authStore.hasRole('AcademicCoordinator') ||
  authStore.hasRole('Teacher') ||
  authStore.hasRole('SuperAdmin') ||
  authStore.hasRole('Admin'),
);

const canUpdateStatus = computed(() =>
  authStore.hasPermission(INTERVENTION_PERMISSIONS.UPDATE_STATUS) ||
  authStore.hasRole('AcademicCoordinator') ||
  authStore.hasRole('Teacher') ||
  authStore.hasRole('SuperAdmin') ||
  authStore.hasRole('Admin'),
);

// ---------------------------------------------------------------------------
// 3. Filter & Period Context State
// ---------------------------------------------------------------------------
// Default to active context period or Hagios demo period UUID
const defaultPeriodId = computed(
  () => appContextStore.activeAcademicPeriodId || '00000000-0000-0000-0000-000000000001',
);

const selectedPeriodId = ref<string>(defaultPeriodId.value);
const selectedGradeLevel = ref<string>('all');
const activeTab = ref<'interventions' | 'atRisk'>('interventions');
const searchFilter = ref('');
const statusFilter = ref<string>('all');
const onlyUnaddressed = ref<boolean>(false);

useRouteQueryHydration((query) => {
  const filter = getQueryString(query, 'filter');
  const status = getQueryString(query, 'status');

  if (filter === 'unaddressed') {
    activeTab.value = 'atRisk';
    onlyUnaddressed.value = true;
  } else {
    onlyUnaddressed.value = false;
  }

  if (status) {
    statusFilter.value = status;
    if (!filter) {
      activeTab.value = 'interventions';
    }
  }
});

// Keep synchronized if context period changes
watch(
  () => appContextStore.activeAcademicPeriodId,
  (newPeriod) => {
    if (newPeriod) {
      selectedPeriodId.value = newPeriod;
    }
  },
);

const gradeLevelOptions = [
  { label: 'All Grade Levels', value: 'all' },
  { label: 'Grade 1', value: 'Grade 1' },
  { label: 'Grade 2', value: 'Grade 2' },
  { label: 'Grade 3', value: 'Grade 3' },
  { label: 'Grade 4', value: 'Grade 4' },
  { label: 'Grade 5', value: 'Grade 5' },
  { label: 'Grade 6', value: 'Grade 6' },
  { label: 'Grade 7', value: 'Grade 7' },
  { label: 'Grade 8', value: 'Grade 8' },
  { label: 'Grade 9', value: 'Grade 9' },
  { label: 'Grade 10', value: 'Grade 10' },
  { label: 'Grade 11', value: 'Grade 11' },
  { label: 'Grade 12', value: 'Grade 12' },
];

const statusOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Planned', value: 'planned' },
  { label: 'In Progress', value: 'in_progress' },
  { label: 'Completed', value: 'completed' },
  { label: 'Cancelled', value: 'cancelled' },
];

// ---------------------------------------------------------------------------
// 4. Data Queries (TanStack Query)
// ---------------------------------------------------------------------------
// Query 1: Active Interventions Work Queue
const {
  data: interventionsData,
  isLoading: isLoadingInterventions,
  isFetching: isFetchingInterventions,
  refetch: refetchInterventions,
} = useActiveInterventionsQuery(
  computed(() => selectedPeriodId.value),
  { enabled: computed(() => Boolean(selectedPeriodId.value)) },
);

// Query 2: Live Early-Warning At-Risk Learners
const {
  data: atRiskData,
  isLoading: isLoadingAtRisk,
  isFetching: isFetchingAtRisk,
  refetch: refetchAtRisk,
} = useAtRiskLearnersQuery(
  computed(() => selectedPeriodId.value),
  computed(() => (selectedGradeLevel.value !== 'all' ? selectedGradeLevel.value : undefined)),
  { enabled: computed(() => Boolean(selectedPeriodId.value)) },
);

// Filtered Active Interventions
const filteredInterventions = computed(() => {
  let list = interventionsData.value ?? [];

  if (statusFilter.value !== 'all') {
    list = list.filter((i) => i.status?.toLowerCase() === statusFilter.value.toLowerCase());
  }

  if (searchFilter.value.trim()) {
    const term = searchFilter.value.trim().toLowerCase();
    list = list.filter((i) => {
      const matchStudent =
        i.studentName?.toLowerCase().includes(term) ||
        i.studentIdentifier?.toLowerCase().includes(term) ||
        i.studentId.toLowerCase().includes(term);
      const matchUnit =
        i.learningUnitName?.toLowerCase().includes(term) ||
        i.learningUnitCode?.toLowerCase().includes(term);
      const matchType = i.interventionType?.toLowerCase().includes(term);
      const matchDesc = i.planDescription?.toLowerCase().includes(term);
      return matchStudent || matchUnit || matchType || matchDesc;
    });
  }

  return list;
});

// Filtered At-Risk Learners
const filteredAtRisk = computed(() => {
  let list = atRiskData.value ?? [];

  if (onlyUnaddressed.value) {
    const coveredStudentIds = new Set(
      (interventionsData.value ?? [])
        .filter((i) => i.status === 'planned' || i.status === 'in_progress')
        .map((i) => i.studentId),
    );
    list = list.filter((l) => !coveredStudentIds.has(l.studentId));
  }

  if (searchFilter.value.trim()) {
    const term = searchFilter.value.trim().toLowerCase();
    list = list.filter((l) => {
      const matchStudent =
        l.studentName?.toLowerCase().includes(term) ||
        l.studentIdentifier?.toLowerCase().includes(term) ||
        l.studentId.toLowerCase().includes(term);
      const matchUnit =
        l.learningUnitName?.toLowerCase().includes(term) ||
        l.learningUnitCode?.toLowerCase().includes(term);
      const matchTrigger = l.triggerDescription?.toLowerCase().includes(term);
      return matchStudent || matchUnit || matchTrigger;
    });
  }

  return list;
});

// Counts
const activeInterventionsCount = computed(
  () =>
    (interventionsData.value ?? []).filter(
      (i) => i.status === 'planned' || i.status === 'in_progress',
    ).length,
);

const atRiskCount = computed(() => (atRiskData.value ?? []).length);

// ---------------------------------------------------------------------------
// 5. Modal & Drawer State
// ---------------------------------------------------------------------------
const isPlanModalOpen = ref(false);
const planModalInitialData = ref<InterventionPlanInitialData | AtRiskLearner | null>(null);

const isDetailDrawerOpen = ref(false);
const selectedIntervention = ref<AcademicIntervention | null>(null);

function handleOpenReferralModal() {
  planModalInitialData.value = {
    academicPeriodId: selectedPeriodId.value,
    triggerType: 'teacher_referral',
    triggerDescription: '',
    suggestedType: 'remediation',
  };
  isPlanModalOpen.value = true;
}

function handleInitiateFromAtRisk(learner: AtRiskLearner) {
  planModalInitialData.value = learner;
  isPlanModalOpen.value = true;
}

function handleInspectIntervention(intervention: AcademicIntervention) {
  selectedIntervention.value = intervention;
  isDetailDrawerOpen.value = true;
}

function onInterventionCreated(newIntervention: AcademicIntervention) {
  refetchInterventions();
  refetchAtRisk();
}

function onInterventionUpdated(updated: AcademicIntervention) {
  selectedIntervention.value = updated;
  refetchInterventions();
}

function handleRefreshAll() {
  refetchInterventions();
  refetchAtRisk();
}

// ---------------------------------------------------------------------------
// 6. Formatting Helpers
// ---------------------------------------------------------------------------
function getStatusBadge(status: InterventionStatus) {
  switch (status) {
    case 'planned':
      return { severity: 'info', label: 'Planned' };
    case 'in_progress':
      return { severity: 'warn', label: 'In Progress' };
    case 'completed':
      return { severity: 'success', label: 'Completed' };
    case 'cancelled':
      return { severity: 'secondary', label: 'Cancelled' };
    default:
      return { severity: 'contrast', label: status };
  }
}

function formatInterventionType(type: string): string {
  return type
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

function getTriggerBadge(trigger: TriggerType) {
  switch (trigger) {
    case 'assessment_below_threshold':
      return { severity: 'danger', label: 'Failing Score' };
    case 'missing_work':
      return { severity: 'warn', label: 'Missing Work' };
    case 'attendance_concern':
      return { severity: 'danger', label: 'Attendance < 75%' };
    case 'standing_concern':
      return { severity: 'warn', label: 'Standing Concern' };
    case 'teacher_referral':
      return { severity: 'info', label: 'Teacher Referral' };
    default:
      return { severity: 'contrast', label: trigger };
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header with Breadcrumbs & Action -->
    <PageHeader
      title="Academic Intervention Workbench"
      subtitle="Early-warning detection, active support work queues, and targeted intervention management (SCR-ACD-06)"
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <Button
            variant="outlined"
            severity="secondary"
            class="text-xs"
            :loading="isFetchingInterventions || isFetchingAtRisk"
            @click="handleRefreshAll"
            data-testid="btn-refresh-interventions"
          >
            <RefreshCw class="w-3.5 h-3.5 mr-1" />
            Refresh
          </Button>
          <Button
            v-if="canInitiate"
            class="text-xs"
            @click="handleOpenReferralModal"
            data-testid="btn-new-referral"
          >
            <Plus class="w-3.5 h-3.5 mr-1" />
            New Intervention Referral
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Global Period Context & Filters Bar -->
    <div class="flex flex-wrap items-center justify-between gap-3 bg-surface-0 dark:bg-surface-900 p-4 rounded-xl border border-surface-200 dark:border-surface-700 shadow-xs">
      <div class="flex flex-wrap items-center gap-3">
        <!-- Academic Period Filter / Input -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Academic Period ID
          </label>
          <InputText
            v-model="selectedPeriodId"
            placeholder="Academic Period UUID"
            class="w-64 text-xs"
            data-testid="input-filter-period"
          />
        </div>

        <!-- Grade Level Filter (for At-Risk tab) -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Grade Level
          </label>
          <Select
            v-model="selectedGradeLevel"
            :options="gradeLevelOptions"
            option-label="label"
            option-value="value"
            class="w-44 text-xs"
            data-testid="select-filter-grade-level"
          />
        </div>

        <!-- Status Filter (for Interventions tab) -->
        <div v-if="activeTab === 'interventions'">
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Intervention Status
          </label>
          <Select
            v-model="statusFilter"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-40 text-xs"
            data-testid="select-filter-status"
          />
        </div>
      </div>

      <!-- Search Input -->
      <div class="w-full sm:w-64">
        <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
          Search
        </label>
        <div class="relative">
          <Search class="w-3.5 h-3.5 text-surface-400 absolute left-3 top-2.5" />
          <InputText
            v-model="searchFilter"
            placeholder="Student, unit, keyword..."
            class="w-full pl-8 text-xs"
            data-testid="input-search-interventions"
          />
        </div>
      </div>
    </div>

    <!-- Main Tabs Navigation (SCR-ACD-06) -->
    <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden">
      <Tabs v-model:value="activeTab" class="w-full" data-testid="interventions-tabs">
        <TabList class="border-b border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/50 px-4">
          <!-- Tab 1: Active Interventions Work Queue -->
          <Tab value="interventions" data-testid="tab-active-interventions">
            <div class="flex items-center gap-2 py-1">
              <Sparkles class="w-4 h-4 text-teal-600" />
              <span class="font-semibold text-xs">Active Interventions Work Queue</span>
              <span
                v-if="activeInterventionsCount > 0"
                class="px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-teal-100 text-teal-800 dark:bg-teal-900/60 dark:text-teal-200"
              >
                {{ activeInterventionsCount }}
              </span>
            </div>
          </Tab>

          <!-- Tab 2: Early-Warning At-Risk Learners -->
          <Tab value="atRisk" data-testid="tab-at-risk-learners">
            <div class="flex items-center gap-2 py-1">
              <ShieldAlert class="w-4 h-4 text-amber-600" />
              <span class="font-semibold text-xs">At-Risk Learners (Early Warning)</span>
              <span
                v-if="atRiskCount > 0"
                class="px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800 dark:bg-amber-900/60 dark:text-amber-200"
              >
                {{ atRiskCount }}
              </span>
            </div>
          </Tab>
        </TabList>

        <TabPanels class="p-6">
          <!-- =============================================================== -->
          <!-- TAB 1: ACTIVE INTERVENTIONS WORK QUEUE                          -->
          <!-- =============================================================== -->
          <TabPanel value="interventions" data-testid="panel-active-interventions">
            <div class="space-y-4">
              <!-- Loading State -->
              <div v-if="isLoadingInterventions" class="py-12 flex justify-center">
                <LoadingSpinner />
              </div>

              <!-- Empty State -->
              <div v-else-if="filteredInterventions.length === 0" class="py-12">
                <EmptyState
                  title="No Interventions Found"
                  description="No academic support plans match the active filters. Initiate an educator referral or review at-risk learners to formulate a plan."
                >
                  <template #actions>
                    <Button
                      v-if="canInitiate"
                      label="New Intervention Referral"
                      icon="pi pi-plus"
                      size="small"
                      class="text-xs"
                      @click="handleOpenReferralModal"
                    />
                  </template>
                </EmptyState>
              </div>

              <!-- Active Interventions DataTable -->
              <DataTable
                v-else
                :value="filteredInterventions"
                data-key="id"
                responsive-layout="scroll"
                striped-rows
                class="text-xs"
                data-testid="table-active-interventions"
              >
                <!-- Student -->
                <Column header="Student" style="min-width: 180px">
                  <template #body="{ data }">
                    <div>
                      <span class="font-semibold text-surface-900 dark:text-surface-100 block">
                        {{ data.studentName || data.studentId }}
                      </span>
                      <span v-if="data.studentIdentifier" class="text-surface-400 text-[11px] block">
                        NISN: {{ data.studentIdentifier }}
                      </span>
                    </div>
                  </template>
                </Column>

                <!-- Learning Unit -->
                <Column header="Learning Unit" style="min-width: 170px">
                  <template #body="{ data }">
                    <div>
                      <span class="font-semibold text-surface-800 dark:text-surface-200 block">
                        {{ data.learningUnitName || data.learningUnitId }}
                      </span>
                      <span v-if="data.learningUnitCode" class="text-surface-400 text-[11px] block">
                        {{ data.learningUnitCode }}
                      </span>
                    </div>
                  </template>
                </Column>

                <!-- Strategy Type -->
                <Column header="Intervention Type" style="min-width: 160px">
                  <template #body="{ data }">
                    <span class="font-medium text-teal-700 dark:text-teal-400 bg-teal-50 dark:bg-teal-950/40 px-2 py-0.5 rounded text-[11px]">
                      {{ formatInterventionType(data.interventionType) }}
                    </span>
                  </template>
                </Column>

                <!-- Trigger & Context -->
                <Column header="Trigger & Context" style="min-width: 220px">
                  <template #body="{ data }">
                    <div class="space-y-1">
                      <Tag
                        :severity="getTriggerBadge(data.triggerType).severity"
                        :value="getTriggerBadge(data.triggerType).label"
                        class="text-[10px] font-bold"
                      />
                      <p class="text-surface-600 dark:text-surface-300 line-clamp-2 text-[11px]">
                        {{ data.triggerDescription }}
                      </p>
                    </div>
                  </template>
                </Column>

                <!-- Status Badge -->
                <Column header="Status" style="width: 120px">
                  <template #body="{ data }">
                    <Tag
                      :severity="getStatusBadge(data.status).severity"
                      :value="getStatusBadge(data.status).label"
                      class="uppercase text-[10px] font-bold tracking-wider"
                    />
                  </template>
                </Column>

                <!-- Milestone Dates -->
                <Column header="Milestone Dates" style="min-width: 150px">
                  <template #body="{ data }">
                    <div class="space-y-0.5 text-[11px] text-surface-500">
                      <div>
                        <span class="text-surface-400">Initiated:</span>
                        {{ formatDisplayDate(data.initiatedAt) }}
                      </div>
                      <div v-if="data.concludedAt">
                        <span class="text-surface-400">Concluded:</span>
                        {{ formatDisplayDate(data.concludedAt) }}
                      </div>
                      <div v-else class="text-amber-600 dark:text-amber-400 font-medium">
                        Target Pending
                      </div>
                    </div>
                  </template>
                </Column>

                <!-- Actions -->
                <Column header="Actions" style="width: 120px" align-frozen="right" frozen>
                  <template #body="{ data }">
                    <Button
                      label="Inspect / Update"
                      icon="pi pi-external-link"
                      variant="outlined"
                      severity="secondary"
                      size="small"
                      class="text-xs"
                      @click="handleInspectIntervention(data)"
                      :data-testid="`btn-inspect-${data.id}`"
                    />
                  </template>
                </Column>
              </DataTable>
            </div>
          </TabPanel>

          <!-- =============================================================== -->
          <!-- TAB 2: AT-RISK LEARNERS (LIVE ON-DEMAND EARLY WARNING ENGINE)   -->
          <!-- =============================================================== -->
          <TabPanel value="atRisk" data-testid="panel-at-risk-learners">
            <div class="space-y-4">
              <!-- Early Warning Explanation Banner -->
              <div class="flex items-start justify-between gap-3 p-3.5 bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800 rounded-xl text-xs text-amber-900 dark:text-amber-200">
                <div class="flex items-start gap-3">
                  <AlertTriangle class="w-5 h-5 text-amber-600 mt-0.5 shrink-0" />
                  <div class="space-y-1">
                    <span class="font-bold">Dynamic On-Demand Early-Warning Engine (TD-04, GAP-004)</span>
                    <p class="text-amber-800 dark:text-amber-300">
                      Students automatically surfaced by real-time academic indicators: failing assessment score below unit threshold, overdue unsubmitted assignments, attendance presence rate &lt; 75%, or prior non-good academic standing.
                    </p>
                  </div>
                </div>
                <Button
                  v-if="onlyUnaddressed"
                  label="Unaddressed Only (Show All)"
                  size="small"
                  severity="warn"
                  variant="outlined"
                  class="text-xs shrink-0"
                  data-testid="btn-clear-unaddressed-filter"
                  @click="onlyUnaddressed = false"
                />
              </div>

              <!-- Loading State -->
              <div v-if="isLoadingAtRisk" class="py-12 flex justify-center">
                <LoadingSpinner />
              </div>

              <!-- Empty State -->
              <div v-else-if="filteredAtRisk.length === 0" class="py-12">
                <EmptyState
                  title="No At-Risk Learners Flagged"
                  description="All students in this academic period and grade cohort are currently meeting learning expectations and attendance thresholds."
                />
              </div>

              <!-- At-Risk Learners DataTable -->
              <DataTable
                v-else
                :value="filteredAtRisk"
                data-key="studentId"
                responsive-layout="scroll"
                striped-rows
                class="text-xs"
                data-testid="table-at-risk-learners"
              >
                <!-- Student -->
                <Column header="Student" style="min-width: 170px">
                  <template #body="{ data }">
                    <div>
                      <span class="font-semibold text-surface-900 dark:text-surface-100 block">
                        {{ data.studentName }}
                      </span>
                      <span v-if="data.studentIdentifier" class="text-surface-400 text-[11px] block">
                        NISN: {{ data.studentIdentifier }}
                      </span>
                    </div>
                  </template>
                </Column>

                <!-- Grade Level -->
                <Column header="Grade" style="width: 100px">
                  <template #body="{ data }">
                    <span class="font-medium text-surface-700 dark:text-surface-300">
                      {{ data.gradeLevel }}
                    </span>
                  </template>
                </Column>

                <!-- Learning Unit -->
                <Column header="Learning Unit" style="min-width: 170px">
                  <template #body="{ data }">
                    <div>
                      <span class="font-semibold text-surface-800 dark:text-surface-200 block">
                        {{ data.learningUnitName }}
                      </span>
                      <span v-if="data.learningUnitCode" class="text-surface-400 text-[11px] block">
                        {{ data.learningUnitCode }}
                      </span>
                    </div>
                  </template>
                </Column>

                <!-- Early Warning Indicator & Metrics -->
                <Column header="Trigger Indicator & Metrics" style="min-width: 240px">
                  <template #body="{ data }">
                    <div class="space-y-1">
                      <Tag
                        :severity="getTriggerBadge(data.triggerType).severity"
                        :value="getTriggerBadge(data.triggerType).label"
                        class="text-[10px] font-bold"
                      />
                      
                      <!-- Operational Metrics Badges -->
                      <div class="flex flex-wrap gap-2 text-[11px]">
                        <span v-if="data.currentScore !== null && data.currentScore !== undefined" class="text-red-600 dark:text-red-400 font-semibold">
                          Score: {{ data.currentScore }} / {{ data.passingScore || 65 }}
                        </span>
                        <span v-if="data.attendancePercentage !== null && data.attendancePercentage !== undefined" class="text-amber-600 dark:text-amber-400 font-semibold">
                          Att: {{ data.attendancePercentage }}%
                        </span>
                        <span v-if="data.missingAssignmentsCount !== null && data.missingAssignmentsCount !== undefined && data.missingAssignmentsCount > 0" class="text-purple-600 dark:text-purple-400 font-semibold">
                          Missing: {{ data.missingAssignmentsCount }} tasks
                        </span>
                        <span v-if="data.currentStanding" class="text-surface-500 font-medium">
                          Standing: {{ data.currentStanding }}
                        </span>
                      </div>

                      <p class="text-surface-600 dark:text-surface-300 text-[11px]">
                        {{ data.triggerDescription }}
                      </p>
                    </div>
                  </template>
                </Column>

                <!-- Actions: 1-Click Initiate Intervention -->
                <Column header="Action" style="width: 160px" align-frozen="right" frozen>
                  <template #body="{ data }">
                    <Button
                      v-if="canInitiate"
                      label="Initiate Intervention"
                      icon="pi pi-bolt"
                      severity="primary"
                      size="small"
                      class="text-xs"
                      @click="handleInitiateFromAtRisk(data)"
                      :data-testid="`btn-initiate-${data.studentId}`"
                    />
                  </template>
                </Column>
              </DataTable>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- Intervention Plan Formulation Modal -->
    <InterventionPlanModal
      v-model:visible="isPlanModalOpen"
      :initial-data="planModalInitialData"
      :active-period-id="selectedPeriodId"
      @created="onInterventionCreated"
    />

    <!-- Intervention Detail Drawer -->
    <InterventionDetailDrawer
      v-model:visible="isDetailDrawerOpen"
      :intervention="selectedIntervention"
      :can-update-status="canUpdateStatus"
      @updated="onInterventionUpdated"
    />
  </div>
</template>
