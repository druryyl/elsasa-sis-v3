<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import {
  Users,
  UserPlus,
  Award,
  BookOpen,
  Calendar,
  Layers,
  Filter,
  Search,
  RefreshCw,
  CheckCircle2,
  Clock,
  AlertTriangle,
  Activity,
  FileCheck2,
  Building,
  GraduationCap,
  Sparkles,
} from 'lucide-vue-next';
import {
  PageHeader,
  ContentCard,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { PARTICIPATION_PERMISSIONS } from '../constants/permissions';
import { useUnitParticipationsQuery } from '../composables/useAcademicParticipation';
import { useAcademicYearsQuery } from '@/features/operations/calendar/composables/useAcademicYearsQuery';
import {
  useLearningStructures,
  useLearningUnits,
} from '@/features/academics/curriculum/composables/useCurriculum';
import { useLearningPlans } from '@/features/academics/learning-plans/composables/useLearningPlans';
import type { AcademicParticipation } from '../types';
import BatchEnrollCohortModal from '../components/BatchEnrollCohortModal.vue';
import RegisterStudentModal from '../components/RegisterStudentModal.vue';
import RecognizePriorCreditModal from '../components/RecognizePriorCreditModal.vue';
import ConcludeParticipationDialog from '../components/ConcludeParticipationDialog.vue';
import UpdateParticipationStatusDialog from '../components/UpdateParticipationStatusDialog.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Participation & Rosters', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Permissions (ADR-009, TD-07)
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canManage = computed(() =>
  authStore.hasPermission(PARTICIPATION_PERMISSIONS.MANAGE),
);
const canRecognize = computed(() =>
  authStore.hasPermission(PARTICIPATION_PERMISSIONS.RECOGNIZE_PRIOR),
);
const canConclude = computed(() =>
  authStore.hasPermission(PARTICIPATION_PERMISSIONS.CONCLUDE),
);

// ---------------------------------------------------------------------------
// 3. Selection & Context Filter State (SCR-ACD-05)
// ---------------------------------------------------------------------------
const selectedPeriodId = ref<string | null>(null);
const selectedUnitId = ref<string | null>(null);
const sectionFilter = ref<string>('');
const statusFilter = ref<string>('all');
const studentSearch = ref<string>('');

// Modals & Dialogs State
const isBatchEnrollOpen = ref(false);
const isRegisterStudentOpen = ref(false);
const isRecognizeCreditOpen = ref(false);
const isConcludeDialogOpen = ref(false);
const isStatusUpdateDialogOpen = ref(false);
const activeParticipation = ref<AcademicParticipation | null>(null);

// ---------------------------------------------------------------------------
// 4. Data Queries: Academic Periods & Learning Units
// ---------------------------------------------------------------------------
const { data: academicYears, isLoading: isLoadingPeriods } = useAcademicYearsQuery();

const periodOptions = computed(() => {
  if (!academicYears.value || academicYears.value.length === 0) return [];
  return academicYears.value.map((ay) => ({
    id: ay.id,
    label: `${ay.name} (${ay.code})`,
    code: ay.code,
  }));
});

// Auto-select first period when options load
watch(
  () => periodOptions.value,
  (periods) => {
    if (periods.length > 0 && !selectedPeriodId.value) {
      selectedPeriodId.value = periods[0].id;
    }
  },
  { immediate: true },
);

// Fetch learning structures and active framework units
const { data: structuresData, isLoading: isLoadingStructures } = useLearningStructures();

const activeStructure = computed(() => {
  const list = Array.isArray(structuresData.value)
    ? structuresData.value
    : structuresData.value?.items ?? [];
  if (list.length === 0) return null;
  const active = list.find((s) => s.status === 'active');
  return active ?? list[0];
});

const activeStructureId = computed(() => activeStructure.value?.id ?? null);

const { data: unitsData, isLoading: isLoadingUnits } = useLearningUnits(activeStructureId);

const unitOptions = computed(() => {
  if (!unitsData.value || unitsData.value.length === 0) return [];
  return unitsData.value.map((u) => ({
    id: u.id,
    code: u.code,
    name: `${u.code} - ${u.name}`,
    rawName: u.name,
    level: u.level,
    creditWeight: u.creditWeight,
  }));
});

// Auto-select first unit when options load
watch(
  () => unitOptions.value,
  (units) => {
    if (units.length > 0 && !selectedUnitId.value) {
      selectedUnitId.value = units[0].id;
    }
  },
  { immediate: true },
);

const selectedUnit = computed(() => {
  if (!selectedUnitId.value || !unitsData.value) return null;
  return unitsData.value.find((u) => u.id === selectedUnitId.value) ?? null;
});

// ---------------------------------------------------------------------------
// 5. Data Queries: Unit Participations Master Worklist
// ---------------------------------------------------------------------------
const {
  data: participationsData,
  isLoading: isLoadingParticipations,
  isError: isParticipationsError,
  error: participationsError,
  refetch: refetchParticipations,
} = useUnitParticipationsQuery(
  selectedUnitId,
  selectedPeriodId,
  sectionFilter,
  statusFilter.value === 'all' ? undefined : statusFilter,
  {
    enabled: computed(() => Boolean(selectedUnitId.value)),
  },
);

// Active Student Learning Plans Query for Contextual Badging (SCR-ACD-05, P7-S24)
const { data: activePlansData } = useLearningPlans(
  computed(() => ({
    academicPeriodId: selectedPeriodId.value ?? undefined,
    status: 'active',
    pageSize: 100,
  })),
  {
    enabled: computed(() => Boolean(selectedPeriodId.value)),
  },
);

const activePlanStudentIds = computed(() => {
  const list = activePlansData.value?.items ?? [];
  return new Set(list.map((p) => p.studentId));
});

function isStudentOnActivePlan(
  studentId: string,
  studentLearningPlanId?: string | null,
  source?: string | null,
): boolean {
  if (studentLearningPlanId || source === 'learning_plan') return true;
  return activePlanStudentIds.value.has(studentId);
}

const statusFilterOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'In Progress (Active)', value: 'in_progress' },
  { label: 'Paused (On Hold)', value: 'paused' },
  { label: 'Withdrawn (Dropped)', value: 'withdrawn' },
  { label: 'Completed (Concluded)', value: 'completed' },
];

// Client-side text filter on student name, student number, or UUID
const filteredParticipations = computed(() => {
  const list = participationsData.value ?? [];
  if (!studentSearch.value.trim()) return list;
  const term = studentSearch.value.trim().toLowerCase();
  return list.filter((p) => {
    const nameMatch = p.studentName?.toLowerCase().includes(term);
    const numMatch = p.studentNumber?.toLowerCase().includes(term);
    const idMatch = p.studentId.toLowerCase().includes(term);
    const sectionMatch = p.sectionLabel?.toLowerCase().includes(term);
    return nameMatch || numMatch || idMatch || sectionMatch;
  });
});

// Summary Metrics
const totalParticipants = computed(() => participationsData.value?.length ?? 0);
const inProgressCount = computed(
  () =>
    participationsData.value?.filter((p) => p.status === 'in_progress')
      .length ?? 0,
);
const completedCount = computed(
  () =>
    participationsData.value?.filter((p) => p.status === 'completed')
      .length ?? 0,
);
const otherStatusCount = computed(
  () =>
    participationsData.value?.filter(
      (p) => p.status === 'paused' || p.status === 'withdrawn',
    ).length ?? 0,
);

// ---------------------------------------------------------------------------
// 6. Modal Launch Handlers
// ---------------------------------------------------------------------------
function openBatchEnroll() {
  isBatchEnrollOpen.value = true;
}

function openRegisterStudent() {
  isRegisterStudentOpen.value = true;
}

function openRecognizeCredit() {
  isRecognizeCreditOpen.value = true;
}

function openConcludeDialog(participation: AcademicParticipation) {
  activeParticipation.value = participation;
  isConcludeDialogOpen.value = true;
}

function openStatusDialog(participation: AcademicParticipation) {
  activeParticipation.value = participation;
  isStatusUpdateDialogOpen.value = true;
}

function onParticipationConcluded() {
  refetchParticipations();
}

function onParticipationStatusUpdated() {
  refetchParticipations();
}

function onCohortEnrolled() {
  refetchParticipations();
}

function onStudentRegistered() {
  refetchParticipations();
}

function onCreditRecognized() {
  refetchParticipations();
}

// Helpers for presentation
function formatParticipationType(type: string): string {
  switch (type) {
    case 'standard':
      return 'Standard';
    case 'repeat':
      return 'Repeat Attempt';
    case 'transferred_in':
      return 'Transfer Credit';
    case 'recognized_prior':
      return 'Prior Learning';
    default:
      return type;
  }
}

function getStatusPillVariant(status: string): 'emerald' | 'amber' | 'rose' | 'sky' | 'slate' {
  switch (status) {
    case 'in_progress':
      return 'sky';
    case 'completed':
      return 'emerald';
    case 'paused':
      return 'amber';
    case 'withdrawn':
      return 'rose';
    default:
      return 'slate';
  }
}

function formatStatusLabel(status: string): string {
  switch (status) {
    case 'in_progress':
      return 'In Progress';
    case 'completed':
      return 'Completed';
    case 'paused':
      return 'Paused';
    case 'withdrawn':
      return 'Withdrawn';
    default:
      return status;
  }
}
</script>

<template>
  <div class="space-y-6" data-testid="participation-workbench-view">
    <!-- 1. Page Header -->
    <PageHeader
      title="Participation & Roster Workbench"
      subtitle="Manage student unit enrollments, cohort batch placements, credit recognition, and roster conclusions (SCR-ACD-05, GAP-008)."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2 flex-wrap">
          <Button
            v-if="canManage"
            type="button"
            label="Batch Enroll Cohort"
            icon="pi pi-users"
            severity="primary"
            class="cursor-pointer"
            data-testid="btn-batch-enroll"
            :disabled="!selectedPeriodId || unitOptions.length === 0"
            @click="openBatchEnroll"
          />
          <Button
            v-if="canManage"
            type="button"
            label="Register Student"
            icon="pi pi-user-plus"
            severity="secondary"
            variant="outlined"
            class="cursor-pointer"
            data-testid="btn-register-student"
            :disabled="!selectedPeriodId || !selectedUnitId"
            @click="openRegisterStudent"
          />
          <Button
            v-if="canRecognize"
            type="button"
            label="Recognize Prior Credit"
            icon="pi pi-award"
            severity="success"
            variant="outlined"
            class="cursor-pointer text-emerald-700 hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-950/30"
            data-testid="btn-recognize-credit"
            :disabled="!selectedPeriodId || !selectedUnitId"
            @click="openRecognizeCredit"
          />
        </div>
      </template>
    </PageHeader>

    <!-- 2. Top Context Selector Ribbon -->
    <ContentCard no-padding class="bg-white border border-slate-200/90 shadow-xs dark:bg-surface-900 dark:border-surface-800">
      <div class="p-4 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <!-- Selectors Group: Period, Unit, Section Filter -->
        <div class="flex items-center gap-4 flex-wrap">
          <!-- Academic Period Selector -->
          <div class="flex items-center gap-2">
            <Calendar class="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
            <span class="text-xs font-medium text-slate-600 dark:text-slate-300">Period:</span>
            <Select
              v-model="selectedPeriodId"
              :options="periodOptions"
              option-label="label"
              option-value="id"
              placeholder="Select Academic Period"
              data-testid="select-academic-period"
              class="w-60 text-sm"
              :loading="isLoadingPeriods"
            />
          </div>

          <!-- Learning Unit Selector -->
          <div class="flex items-center gap-2">
            <BookOpen class="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
            <span class="text-xs font-medium text-slate-600 dark:text-slate-300">Learning Unit:</span>
            <Select
              v-model="selectedUnitId"
              :options="unitOptions"
              option-label="name"
              option-value="id"
              placeholder="Select Learning Unit"
              data-testid="select-learning-unit"
              class="w-72 text-sm"
              filter
              filter-placeholder="Search units..."
              :loading="isLoadingUnits || isLoadingStructures"
            />
          </div>

          <!-- Section Filter Input -->
          <div class="flex items-center gap-2">
            <Layers class="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
            <span class="text-xs font-medium text-slate-600 dark:text-slate-300">Section:</span>
            <InputText
              v-model="sectionFilter"
              placeholder="Filter section (e.g. 10-A)"
              data-testid="input-section-filter"
              class="w-44 text-sm"
            />
          </div>
        </div>

        <!-- Selected Unit Info Badge -->
        <div v-if="selectedUnit" class="flex items-center gap-2 text-xs">
          <span
            v-if="selectedUnit.creditWeight"
            class="px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-semibold dark:bg-surface-800 dark:text-surface-200"
          >
            {{ selectedUnit.creditWeight }} Credits
          </span>
          <span
            v-if="selectedUnit.level"
            class="px-2 py-0.5 rounded bg-teal-50 text-teal-800 font-medium border border-teal-200/80 dark:bg-teal-950/40 dark:text-teal-300 dark:border-teal-800"
          >
            Level: {{ selectedUnit.level }}
          </span>
        </div>
      </div>
    </ContentCard>

    <!-- 3. Summary Metrics Ribbon -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <div
        class="bg-white rounded-xl border border-slate-200/80 p-3 shadow-xs dark:bg-surface-900 dark:border-surface-800"
        data-testid="metric-total-participants"
      >
        <span class="text-[11px] font-medium uppercase tracking-wider text-slate-500">Total Enrolled</span>
        <div class="mt-1 flex items-baseline gap-2">
          <span class="text-2xl font-bold text-slate-900 dark:text-surface-50">{{ totalParticipants }}</span>
          <span class="text-xs text-slate-400">students</span>
        </div>
      </div>

      <div
        class="bg-white rounded-xl border border-slate-200/80 p-3 shadow-xs dark:bg-surface-900 dark:border-surface-800"
        data-testid="metric-in-progress"
      >
        <span class="text-[11px] font-medium uppercase tracking-wider text-sky-600 dark:text-sky-400">In Progress</span>
        <div class="mt-1 flex items-baseline gap-2">
          <span class="text-2xl font-bold text-sky-700 dark:text-sky-300">{{ inProgressCount }}</span>
          <span class="text-xs text-slate-400">active</span>
        </div>
      </div>

      <div
        class="bg-white rounded-xl border border-slate-200/80 p-3 shadow-xs dark:bg-surface-900 dark:border-surface-800"
        data-testid="metric-completed"
      >
        <span class="text-[11px] font-medium uppercase tracking-wider text-emerald-600 dark:text-emerald-400">Completed</span>
        <div class="mt-1 flex items-baseline gap-2">
          <span class="text-2xl font-bold text-emerald-700 dark:text-emerald-300">{{ completedCount }}</span>
          <span class="text-xs text-slate-400">concluded</span>
        </div>
      </div>

      <div
        class="bg-white rounded-xl border border-slate-200/80 p-3 shadow-xs dark:bg-surface-900 dark:border-surface-800"
        data-testid="metric-paused-withdrawn"
      >
        <span class="text-[11px] font-medium uppercase tracking-wider text-amber-600 dark:text-amber-400">Paused / Withdrawn</span>
        <div class="mt-1 flex items-baseline gap-2">
          <span class="text-2xl font-bold text-amber-700 dark:text-amber-300">{{ otherStatusCount }}</span>
          <span class="text-xs text-slate-400">inactive</span>
        </div>
      </div>
    </div>

    <!-- 4. Table Controls Bar: Search & Status Filter -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <!-- Search Input -->
      <div class="relative w-full sm:w-80">
        <Search class="absolute left-3 top-2.5 w-4 h-4 text-slate-400" />
        <InputText
          v-model="studentSearch"
          placeholder="Search student by name or ID..."
          class="w-full pl-9 text-xs"
          data-testid="input-student-search"
        />
      </div>

      <!-- Right Controls: Status Filter & Refresh -->
      <div class="flex items-center gap-2.5">
        <div class="flex items-center gap-1.5 text-xs text-slate-500">
          <Filter class="w-3.5 h-3.5" />
          <span>Status:</span>
        </div>
        <Select
          v-model="statusFilter"
          :options="statusFilterOptions"
          option-label="label"
          option-value="value"
          class="w-48 text-xs"
          data-testid="select-status-filter"
        />
        <Button
          type="button"
          icon="pi pi-refresh"
          severity="secondary"
          variant="text"
          size="small"
          title="Refresh roster"
          data-testid="btn-refresh-roster"
          :loading="isLoadingParticipations"
          @click="() => refetchParticipations()"
        />
      </div>
    </div>

    <!-- 5. Master Worklist DataTable -->
    <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs overflow-hidden dark:bg-surface-900 dark:border-surface-800">
      <!-- Loading State -->
      <div v-if="isLoadingParticipations" class="py-16 flex justify-center items-center">
        <LoadingSpinner label="Loading student participation roster..." />
      </div>

      <!-- Error State -->
      <div v-else-if="isParticipationsError" class="p-8 text-center space-y-3">
        <AlertTriangle class="w-10 h-10 text-rose-500 mx-auto" />
        <h4 class="text-sm font-semibold text-slate-800 dark:text-surface-100">Unable to load participation roster</h4>
        <p class="text-xs text-slate-500">
          {{ (participationsError as Error)?.message || 'An unexpected error occurred while fetching unit participations.' }}
        </p>
        <Button
          size="small"
          severity="secondary"
          variant="outlined"
          @click="() => refetchParticipations()"
        >
          Retry
        </Button>
      </div>

      <!-- DataTable -->
      <DataTable
        v-else
        :value="filteredParticipations"
        data-key="id"
        striped-rows
        responsive-layout="scroll"
        paginator
        :rows="15"
        :rows-per-page-options="[10, 15, 25, 50]"
        class="text-xs"
        data-testid="participations-datatable"
      >
        <template #empty>
          <div class="py-12">
            <EmptyState
              title="No student participations found"
              message="No student enrollments match the selected unit, period, and filter criteria."
            >
              <template #action>
                <div v-if="canManage" class="mt-4 flex gap-2 justify-center">
                  <Button
                    size="small"
                    label="Batch Enroll Cohort"
                    icon="pi pi-users"
                    @click="openBatchEnroll"
                  />
                  <Button
                    size="small"
                    label="Register Student"
                    icon="pi pi-user-plus"
                    variant="outlined"
                    @click="openRegisterStudent"
                  />
                </div>
              </template>
            </EmptyState>
          </div>
        </template>

        <!-- Column 1: Student Information -->
        <Column header="Student Participant" sortable field="studentName" style="min-width: 220px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span class="font-medium text-slate-900 dark:text-surface-100 block leading-tight">
                  {{ data.studentName ?? 'Unknown Student' }}
                </span>
                <!-- Active Learning Plan Badge (SCR-ACD-05, P7-S24) -->
                <span
                  v-if="isStudentOnActivePlan(data.studentId, data.studentLearningPlanId, data.source)"
                  class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-semibold bg-indigo-50 text-indigo-700 border border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800"
                  data-testid="badge-active-slp"
                  title="Governed by Active Student Learning Plan"
                >
                  <Sparkles class="w-3 h-3 text-indigo-600 dark:text-indigo-400" />
                  Active Learning Plan
                </span>
              </div>
              <div class="flex items-center gap-1.5 text-[11px] text-slate-500">
                <span class="font-mono">{{ data.studentNumber ?? data.studentId.slice(0, 8) }}</span>
                <span v-if="data.sectionLabel">&bull;</span>
                <span
                  v-if="data.sectionLabel"
                  class="inline-flex items-center px-1.5 py-0.2 rounded bg-slate-100 text-slate-700 font-medium dark:bg-surface-800 dark:text-surface-200"
                >
                  Section {{ data.sectionLabel }}
                </span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 2: Participation Type & Source -->
        <Column header="Type & Source" style="min-width: 170px">
          <template #body="{ data }">
            <div class="space-y-1">
              <div class="flex items-center gap-1.5">
                <span
                  v-if="data.participationType === 'repeat'"
                  class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800 dark:bg-amber-950/50 dark:text-amber-300"
                >
                  Repeat Retake
                </span>
                <span
                  v-else-if="data.participationType === 'transferred_in' || data.participationType === 'recognized_prior'"
                  class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-100 text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300"
                >
                  Transfer / RPL
                </span>
                <span
                  v-else
                  class="text-slate-800 dark:text-surface-200 font-medium"
                >
                  {{ formatParticipationType(data.participationType) }}
                </span>
              </div>
              <p class="text-[10px] text-slate-500 capitalize">
                Source: {{ (data.source || 'assigned').replace('_', ' ') }}
                <span v-if="data.studentLearningPlanId" class="text-indigo-600 dark:text-indigo-400 font-medium"> (SLP Linked)</span>
              </p>
            </div>
          </template>
        </Column>

        <!-- Column 3: Attempt Counter -->
        <Column header="Attempt" field="attemptNumber" sortable style="min-width: 100px">
          <template #body="{ data }">
            <span
              class="inline-flex items-center px-2 py-0.5 rounded-full font-mono font-semibold text-[11px]"
              :class="
                data.attemptNumber > 1
                  ? 'bg-rose-50 text-rose-700 border border-rose-200 dark:bg-rose-950/40 dark:text-rose-300 dark:border-rose-800'
                  : 'bg-slate-100 text-slate-700 border border-slate-200 dark:bg-surface-800 dark:text-surface-300 dark:border-surface-700'
              "
            >
              #{{ data.attemptNumber }}
            </span>
          </template>
        </Column>

        <!-- Column 4: Timeline / Start & End Dates -->
        <Column header="Commenced / Concluded" sortable field="startDate" style="min-width: 160px">
          <template #body="{ data }">
            <div class="space-y-0.5 text-slate-700 dark:text-surface-300">
              <div class="flex items-center gap-1 text-[11px]">
                <Clock class="w-3 h-3 text-slate-400" />
                <span>Started: {{ formatDisplayDate(data.startDate) }}</span>
              </div>
              <div
                v-if="data.completionDate"
                class="text-[10px] text-emerald-700 dark:text-emerald-400 font-medium"
              >
                Done: {{ formatDisplayDate(data.completionDate) }}
              </div>
              <div
                v-else-if="data.endDate"
                class="text-[10px] text-rose-600 dark:text-rose-400"
              >
                Ended: {{ formatDisplayDate(data.endDate) }}
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 5: Status Badge -->
        <Column header="Status" field="status" sortable style="min-width: 130px">
          <template #body="{ data }">
            <StatusPill
              :status="data.status"
              :variant="getStatusPillVariant(data.status)"
              :label="formatStatusLabel(data.status)"
              size="sm"
            />
          </template>
        </Column>

        <!-- Column 6: Recognition Metadata / Notes -->
        <Column header="Notes & Recognition" style="min-width: 180px">
          <template #body="{ data }">
            <div v-if="data.sourceInstitution || data.externalGradeOrCredit" class="space-y-0.5 text-[11px]">
              <span class="text-slate-800 dark:text-surface-200 font-medium">
                {{ data.sourceInstitution }}
              </span>
              <p v-if="data.externalGradeOrCredit" class="text-slate-500">
                Grade: <strong class="text-emerald-700 dark:text-emerald-400">{{ data.externalGradeOrCredit }}</strong>
              </p>
            </div>
            <p
              v-else-if="data.notes"
              class="text-[11px] text-slate-500 line-clamp-2 max-w-xs"
              :title="data.notes"
            >
              {{ data.notes }}
            </p>
            <span v-else class="text-[11px] text-slate-400 italic">—</span>
          </template>
        </Column>

        <!-- Column 7: Actions -->
        <Column header="Actions" align-frozen="right" frozen style="min-width: 160px">
          <template #body="{ data }">
            <div class="flex items-center gap-1.5 justify-end">
              <!-- Conclude Participation Button (Only for in_progress status) -->
              <Button
                v-if="canConclude"
                type="button"
                size="small"
                severity="success"
                variant="outlined"
                icon="pi pi-check"
                label="Conclude"
                title="Conclude unit participation upon term finish"
                :disabled="data.status !== 'in_progress'"
                class="text-xs py-1 px-2"
                data-testid="btn-conclude-row"
                @click="openConcludeDialog(data)"
              />

              <!-- Update Status Button -->
              <Button
                v-if="canManage"
                type="button"
                size="small"
                severity="secondary"
                variant="outlined"
                icon="pi pi-file-edit"
                title="Update participation status (in_progress, paused, withdrawn)"
                :disabled="data.status === 'completed'"
                class="text-xs p-1"
                data-testid="btn-update-status-row"
                @click="openStatusDialog(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- 6. Modals & Dialogs (SCR-ACD-05) -->
    <BatchEnrollCohortModal
      v-model:visible="isBatchEnrollOpen"
      :periods="periodOptions"
      :units="unitOptions"
      :initial-period-id="selectedPeriodId"
      :initial-section-label="sectionFilter"
      @enrolled="onCohortEnrolled"
    />

    <RegisterStudentModal
      v-model:visible="isRegisterStudentOpen"
      :periods="periodOptions"
      :units="unitOptions"
      :initial-period-id="selectedPeriodId"
      :initial-unit-id="selectedUnitId"
      @registered="onStudentRegistered"
    />

    <RecognizePriorCreditModal
      v-model:visible="isRecognizeCreditOpen"
      :periods="periodOptions"
      :units="unitOptions"
      :initial-period-id="selectedPeriodId"
      :initial-unit-id="selectedUnitId"
      @recognized="onCreditRecognized"
    />

    <ConcludeParticipationDialog
      v-model:visible="isConcludeDialogOpen"
      :participation="activeParticipation"
      @concluded="onParticipationConcluded"
    />

    <UpdateParticipationStatusDialog
      v-model:visible="isStatusUpdateDialogOpen"
      :participation="activeParticipation"
      @updated="onParticipationStatusUpdated"
    />
  </div>
</template>
