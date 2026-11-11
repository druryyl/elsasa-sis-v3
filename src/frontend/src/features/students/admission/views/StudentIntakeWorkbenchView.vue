<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { refDebounced } from '@vueuse/core';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import Drawer from 'primevue/drawer';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  Users,
  UserPlus,
  Search,
  RotateCcw,
  RefreshCw,
  UserCheck,
  XCircle,
  Play,
  Gavel,
  Ban,
  GraduationCap,
  Eye,
  AlertCircle,
  FileCheck,
  Shield,
  CheckCircle2,
  ExternalLink,
  Calendar,
  Filter,
} from 'lucide-vue-next';
import { useToast } from 'primevue/usetoast';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { useAuthStore } from '@/app/stores/auth.store';
import { ADMISSION_PERMISSIONS } from '../constants/permissions';
// eslint-disable-next-line no-restricted-imports
import { ENROLLMENT_PERMISSIONS } from '../../enrollment/constants/permissions';
import { tryGetToast } from '../composables/toastNotifications';
import type {
  AdmissionQueueFilterParams,
  ApplicantDocumentDto,
  AssessmentEvidenceDto,
} from '../types/admission.types';
import {
  useAdmissionQueueQuery,
  useAdmissionDossierQuery,
  useActiveAdmissionPeriodsQuery,
} from '../composables/useAdmissionQueries';
import {
  useStartReviewMutation,
  useDeleteAssessmentEvidenceMutation,
} from '../composables/useAdmissionMutations';
import {
  ApplicationInfoTab,
  DocumentVerificationTab,
  AssessmentEvidenceTab,
  ApplicantAssessmentTab,
  DecisionHistoryTab,
  RecordDecisionModal,
  VerifyDocumentModal,
  AssessmentEvidenceModal,
  WithdrawApplicationModal,
  EnrollApplicantModal,
  type EnrollApplicantSuccessResult,
} from '../components';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Students', to: '/students' },
  { label: 'Student Intake Workbench', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canRegister = computed(() =>
  authStore.hasPermission(ADMISSION_PERMISSIONS.REGISTER),
);
const canReview = computed(() =>
  authStore.hasPermission(ADMISSION_PERMISSIONS.REVIEW),
);
const canDecide = computed(() =>
  authStore.hasPermission(ADMISSION_PERMISSIONS.DECIDE),
);
const canWithdraw = computed(() =>
  authStore.hasPermission(ADMISSION_PERMISSIONS.REGISTER),
);
const canEnroll = computed(() =>
  authStore.hasPermission(ENROLLMENT_PERMISSIONS.EXECUTE),
);

let toastInstance: ReturnType<typeof tryGetToast> = null;
try {
  toastInstance = useToast();
} catch {
  toastInstance = tryGetToast();
}

// ---------------------------------------------------------------------------
// 3. Filters & Search State
// ---------------------------------------------------------------------------
const selectedPeriodId = ref<string>('');
const selectedStatus = ref<string>('');
const selectedDocStatus = ref<string>('');
const selectedEntryLevel = ref<string>('');
const searchInput = ref<string>('');
const debouncedSearch = refDebounced(searchInput, 300);

const statusFilterPills = [
  { label: 'All', value: '' },
  { label: 'Registered', value: 'registered' },
  { label: 'Submitted', value: 'submitted' },
  { label: 'Under Review', value: 'under_review' },
  { label: 'Entrance Assessed', value: 'entrance_assessed' },
  { label: 'Waitlisted', value: 'waitlisted' },
  { label: 'Accepted', value: 'accepted' },
  { label: 'Decided', value: 'decided' },
  { label: 'Withdrawn', value: 'withdrawn' },
] as const;

useRouteQueryHydration((query) => {
  const status = getQueryString(query, 'status');
  if (status) {
    selectedStatus.value = status;
  }

  const docStatus = getQueryString(query, 'docStatus');
  if (docStatus) {
    selectedDocStatus.value = docStatus;
  }
});

const entryLevelOptions = [
  { label: 'All Entry Levels', value: '' },
  { label: 'TK-A', value: 'TK-A' },
  { label: 'TK-B', value: 'TK-B' },
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

// ---------------------------------------------------------------------------
// 4. Reactive TanStack Queries
// ---------------------------------------------------------------------------
const { data: admissionPeriods, isLoading: isLoadingPeriods } =
  useActiveAdmissionPeriodsQuery();

const periodOptions = computed(() => [
  { id: '', name: 'All Admission Periods' },
  ...(admissionPeriods.value ?? []).map((p) => ({
    id: p.id,
    name: `${p.name} (${p.periodCode})`,
  })),
]);

const periodsMap = computed(() => {
  const map: Record<string, string> = {};
  if (admissionPeriods.value) {
    for (const p of admissionPeriods.value) {
      map[p.id] = p.name;
    }
  }
  return map;
});

function getPeriodName(periodId: string): string {
  return periodsMap.value[periodId] ?? periodId;
}

const queueFilters = computed<AdmissionQueueFilterParams>(() => ({
  admissionPeriodId: selectedPeriodId.value || null,
  status: selectedStatus.value || null,
  requestedEntryLevel: selectedEntryLevel.value || null,
  searchTerm: debouncedSearch.value ? debouncedSearch.value.trim() : null,
}));

const {
  data: applications,
  isLoading: isLoadingQueue,
  isFetching: isFetchingQueue,
  refetch: refetchQueue,
} = useAdmissionQueueQuery(queueFilters);

// Active intake count (registered or under_review)
const totalActiveCount = computed(() => {
  if (!applications.value) return 0;
  return applications.value.filter(
    (app) => app.status === 'registered' || app.status === 'under_review',
  ).length;
});

// Status pill counts from current loaded applications
const statusCounts = computed(() => {
  const counts: Record<string, number> = {
    all: applications.value?.length ?? 0,
    registered: 0,
    submitted: 0,
    under_review: 0,
    entrance_assessed: 0,
    waitlisted: 0,
    accepted: 0,
    decided: 0,
    withdrawn: 0,
  };
  if (applications.value) {
    for (const app of applications.value) {
      const s = (app.status ?? '').toLowerCase();
      if (s in counts) {
        counts[s]++;
      }
    }
  }
  return counts;
});

function resetFilters() {
  selectedPeriodId.value = '';
  selectedStatus.value = '';
  selectedDocStatus.value = '';
  selectedEntryLevel.value = '';
  searchInput.value = '';
}

// Visual helpers for avatar initials and level badges
function getInitials(name?: string | null): string {
  if (!name) return 'ST';
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function getLevelBadgeStyle(level?: string | null): string {
  if (!level) return 'bg-slate-100 text-slate-700 border-slate-200';
  const l = level.toUpperCase();
  if (l.startsWith('TK')) return 'bg-amber-50 text-amber-800 border-amber-200';
  if (
    l.includes('GRADE 1') ||
    l.includes('GRADE 2') ||
    l.includes('GRADE 3') ||
    l.includes('GRADE 4') ||
    l.includes('GRADE 5') ||
    l.includes('GRADE 6')
  ) {
    return 'bg-emerald-50 text-emerald-800 border-emerald-200';
  }
  if (l.includes('GRADE 7') || l.includes('GRADE 8') || l.includes('GRADE 9')) {
    return 'bg-sky-50 text-sky-800 border-sky-200';
  }
  return 'bg-indigo-50 text-indigo-800 border-indigo-200';
}

// ---------------------------------------------------------------------------
// 5. Selected Application Dossier Drawer State, Tabs & Query
// ---------------------------------------------------------------------------
const selectedApplicationId = ref<string | null>(null);
const isDossierOpen = ref(false);
const activeDossierTab = ref<string>('info');

function openDossier(applicationId: string) {
  selectedApplicationId.value = applicationId;
  activeDossierTab.value = selectedDocStatus.value === 'pending' ? 'documents' : 'info';
  lastEnrolledStudent.value = null;
  isDossierOpen.value = true;
}

function closeDossier() {
  isDossierOpen.value = false;
  selectedApplicationId.value = null;
  lastEnrolledStudent.value = null;
}

const {
  data: dossier,
  isLoading: isLoadingDossier,
  isError: isDossierError,
  refetch: refetchDossier,
} = useAdmissionDossierQuery(selectedApplicationId, {
  enabled: computed(() => isDossierOpen.value && Boolean(selectedApplicationId.value)),
});

// Auto-open dossier if route contains :id param
watch(
  () => route.params.id,
  (idParam) => {
    if (typeof idParam === 'string' && idParam.trim()) {
      openDossier(idParam.trim());
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 6. Workflow Mutations & Modals
// ---------------------------------------------------------------------------
const startReviewMutation = useStartReviewMutation({
  onSuccess: () => {
    refetchDossier();
  },
});

function handleStartReview(applicationId: string) {
  startReviewMutation.mutate(applicationId);
}

// Record Decision Modal State
const isRecordDecisionModalOpen = ref(false);

function openRecordDecisionModal() {
  isRecordDecisionModalOpen.value = true;
}

// Withdraw Modal State
const isWithdrawModalOpen = ref(false);

function openWithdrawModal() {
  isWithdrawModalOpen.value = true;
}

// Document Verification Modal State
const isVerifyDocumentModalOpen = ref(false);
const selectedDocumentForVerification = ref<ApplicantDocumentDto | null>(null);

function handleOpenVerifyDocument(doc: ApplicantDocumentDto) {
  selectedDocumentForVerification.value = doc;
  isVerifyDocumentModalOpen.value = true;
}

// Assessment Evidence Modal & Deletion State
const isAssessmentModalOpen = ref(false);
const selectedEvidenceForEdit = ref<AssessmentEvidenceDto | null>(null);

function handleAddAssessmentScore() {
  selectedEvidenceForEdit.value = null;
  isAssessmentModalOpen.value = true;
}

function handleEditAssessmentScore(ev: AssessmentEvidenceDto) {
  selectedEvidenceForEdit.value = ev;
  isAssessmentModalOpen.value = true;
}

const deleteEvidenceMutation = useDeleteAssessmentEvidenceMutation({
  onSuccess: () => {
    refetchDossier();
  },
});

function handleDeleteAssessmentScore(evidenceId: string) {
  if (!dossier.value) return;
  deleteEvidenceMutation.mutate({
    applicationId: dossier.value.id,
    evidenceId,
  });
}

function isApplicationActive(status: string | undefined): boolean {
  if (!status) return false;
  const s = status.toLowerCase();
  return s === 'registered' || s === 'under_review';
}

// Enroll Applicant Modal State
const isEnrollModalOpen = ref(false);
const lastEnrolledStudent = ref<EnrollApplicantSuccessResult | null>(null);

function openEnrollModal() {
  isEnrollModalOpen.value = true;
}

function handleEnrollSuccess(result: EnrollApplicantSuccessResult) {
  isEnrollModalOpen.value = false;
  lastEnrolledStudent.value = result;

  const currentToast = toastInstance ?? tryGetToast();
  currentToast?.add({
    severity: 'success',
    summary: 'Enrollment Successful',
    detail: `Student successfully enrolled with Student Number: ${result.studentNumber}`,
    life: 5000,
  });

  refetchQueue();
  refetchDossier();
}
</script>

<template>
  <div class="space-y-6" data-testid="student-intake-workbench-view">
    <!-- 1. Page Header -->
    <PageHeader
      title="Student Intake Workbench"
      subtitle="Triage prospective student applications, audit credential verification, and govern admission decisions."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2.5">
          <div
            class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-teal-50 border border-teal-200/90 text-teal-800 text-xs font-semibold shadow-2xs"
            data-testid="active-intake-count-badge"
          >
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-teal-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2 w-2 bg-teal-600"></span>
            </span>
            <span>{{ totalActiveCount }} Active Intake Applications</span>
          </div>

          <Button
            size="small"
            severity="secondary"
            variant="outlined"
            :loading="isFetchingQueue"
            class="!text-xs !py-1.5 cursor-pointer hover:!bg-slate-100"
            data-testid="refresh-queue-button"
            @click="() => refetchQueue()"
          >
            <RefreshCw class="w-3.5 h-3.5 mr-1.5" :class="{ 'animate-spin': isFetchingQueue }" />
            <span>Refresh</span>
          </Button>

          <Button
            size="small"
            :disabled="!canRegister"
            class="!text-xs !py-1.5 cursor-pointer bg-teal-700 hover:bg-teal-800 text-white font-medium shadow-xs"
            data-testid="new-application-button"
            @click="router.push('/admission/apply')"
          >
            <UserPlus class="w-3.5 h-3.5 mr-1.5" />
            <span>New Application</span>
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- 2. Executive KPI Overview Metrics -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3.5" data-testid="intake-kpi-summary">
      <!-- KPI 1: Active Intake -->
      <div
        class="bg-white p-4 rounded-xl border border-slate-200/90 shadow-2xs hover:shadow-xs transition-shadow flex items-center justify-between"
      >
        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider block">Active Intake</span>
          <div class="flex items-baseline gap-2">
            <span class="text-2xl font-bold text-slate-900">{{ totalActiveCount }}</span>
            <span class="text-[11px] font-medium text-teal-700 bg-teal-50 px-2 py-0.5 rounded-full border border-teal-200">In Pipeline</span>
          </div>
          <span class="text-[11px] text-slate-400 block">Registered &amp; under review</span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-teal-50 border border-teal-200/70 text-teal-700 flex items-center justify-center shrink-0">
          <Users class="w-5 h-5" />
        </div>
      </div>

      <!-- KPI 2: Under Review -->
      <div
        class="bg-white p-4 rounded-xl border border-slate-200/90 shadow-2xs hover:shadow-xs transition-shadow flex items-center justify-between cursor-pointer"
        @click="selectedStatus = 'under_review'"
      >
        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider block">Under Review</span>
          <div class="flex items-baseline gap-2">
            <span class="text-2xl font-bold text-slate-900">{{ statusCounts.under_review }}</span>
            <span class="text-[11px] font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full border border-amber-200">In Triage</span>
          </div>
          <span class="text-[11px] text-slate-400 block">Reviewing credentials</span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-amber-50 border border-amber-200/70 text-amber-700 flex items-center justify-center shrink-0">
          <Shield class="w-5 h-5" />
        </div>
      </div>

      <!-- KPI 3: Decided & Accepted -->
      <div
        class="bg-white p-4 rounded-xl border border-slate-200/90 shadow-2xs hover:shadow-xs transition-shadow flex items-center justify-between cursor-pointer"
        @click="selectedStatus = 'decided'"
      >
        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider block">Decided / Accepted</span>
          <div class="flex items-baseline gap-2">
            <span class="text-2xl font-bold text-slate-900">{{ (statusCounts.decided || 0) + (statusCounts.accepted || 0) }}</span>
            <span class="text-[11px] font-medium text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200">Outcome Set</span>
          </div>
          <span class="text-[11px] text-slate-400 block">Ready for enrollment</span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-emerald-50 border border-emerald-200/70 text-emerald-700 flex items-center justify-center shrink-0">
          <CheckCircle2 class="w-5 h-5" />
        </div>
      </div>

      <!-- KPI 4: Total Applications -->
      <div
        class="bg-white p-4 rounded-xl border border-slate-200/90 shadow-2xs hover:shadow-xs transition-shadow flex items-center justify-between cursor-pointer"
        @click="selectedStatus = ''"
      >
        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider block">Total Cohort</span>
          <div class="flex items-baseline gap-2">
            <span class="text-2xl font-bold text-slate-900">{{ statusCounts.all }}</span>
            <span class="text-[11px] font-medium text-slate-600 bg-slate-100 px-2 py-0.5 rounded-full border border-slate-200">All Time</span>
          </div>
          <span class="text-[11px] text-slate-400 block">Full intake roster</span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-slate-100 border border-slate-200/70 text-slate-700 flex items-center justify-center shrink-0">
          <GraduationCap class="w-5 h-5" />
        </div>
      </div>
    </div>

    <!-- 3. Filter & Search Panel -->
    <div
      class="bg-white p-4 sm:p-5 rounded-xl border border-slate-200/80 shadow-2xs space-y-4"
      data-testid="intake-filter-panel"
    >
      <!-- Row 1: Status Filter Pills -->
      <div class="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-3">
        <div class="flex flex-wrap items-center gap-1.5" data-testid="status-pills-bar">
          <span class="text-xs font-bold text-slate-400 uppercase tracking-wider mr-2 flex items-center gap-1">
            <Filter class="w-3.5 h-3.5 text-slate-400" />
            <span>Status:</span>
          </span>
          <button
            v-for="pill in statusFilterPills"
            :key="pill.value"
            type="button"
            :class="[
              'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium transition cursor-pointer',
              selectedStatus === pill.value
                ? 'bg-teal-700 text-white shadow-xs font-semibold ring-2 ring-teal-700/20'
                : 'bg-slate-100/90 text-slate-600 hover:bg-slate-200/80 hover:text-slate-900',
            ]"
            :data-testid="`filter-status-${pill.value || 'all'}`"
            @click="selectedStatus = pill.value"
          >
            <span>{{ pill.label }}</span>
            <span
              v-if="pill.value === '' && statusCounts.all > 0"
              class="px-1.5 py-0.2 rounded-full text-[10px] font-semibold"
              :class="selectedStatus === '' ? 'bg-teal-800 text-teal-100' : 'bg-slate-200 text-slate-700'"
            >
              {{ statusCounts.all }}
            </span>
            <span
              v-else-if="pill.value && statusCounts[pill.value] !== undefined"
              class="px-1.5 py-0.2 rounded-full text-[10px] font-semibold"
              :class="selectedStatus === pill.value ? 'bg-teal-800 text-teal-100' : 'bg-slate-200 text-slate-700'"
            >
              {{ statusCounts[pill.value] }}
            </span>
          </button>
        </div>

        <div class="flex items-center gap-2.5">
          <span
            v-if="selectedDocStatus"
            class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200 shadow-2xs"
            data-testid="filter-doc-status-chip"
          >
            <FileCheck class="w-3.5 h-3.5 text-amber-600" />
            <span>Documents: {{ selectedDocStatus }}</span>
            <button
              type="button"
              class="text-amber-600 hover:text-amber-900 cursor-pointer ml-0.5"
              title="Clear document filter"
              @click="selectedDocStatus = ''"
            >
              <XCircle class="w-3.5 h-3.5" />
            </button>
          </span>

          <button
            v-if="selectedPeriodId || selectedStatus || selectedDocStatus || selectedEntryLevel || searchInput"
            type="button"
            class="inline-flex items-center gap-1.5 text-xs text-slate-600 hover:text-slate-900 cursor-pointer px-2.5 py-1 rounded-md bg-slate-100/80 hover:bg-slate-200/80 border border-slate-200 transition-colors"
            data-testid="reset-filters-button"
            @click="resetFilters"
          >
            <RotateCcw class="w-3 h-3 text-slate-400" />
            <span>Reset Filters</span>
          </button>
        </div>
      </div>

      <!-- Row 2: Secondary Dropdown Filters & Search Input -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3.5 pt-1">
        <!-- Admission Period Dropdown -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1.5 flex items-center gap-1">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            <span>Admission Period</span>
          </label>
          <Select
            v-model="selectedPeriodId"
            :options="periodOptions"
            option-label="name"
            option-value="id"
            placeholder="All Admission Periods"
            :loading="isLoadingPeriods"
            class="w-full !text-xs !rounded-lg"
            data-testid="period-filter-select"
          />
        </div>

        <!-- Requested Entry Level Dropdown -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1.5 flex items-center gap-1">
            <GraduationCap class="w-3.5 h-3.5 text-slate-400" />
            <span>Requested Entry Level</span>
          </label>
          <Select
            v-model="selectedEntryLevel"
            :options="entryLevelOptions"
            option-label="label"
            option-value="value"
            placeholder="All Entry Levels"
            class="w-full !text-xs !rounded-lg"
            data-testid="entry-level-filter-select"
          />
        </div>

        <!-- Search Input -->
        <div class="sm:col-span-2">
          <label class="block text-xs font-semibold text-slate-700 mb-1.5 flex items-center justify-between">
            <span class="flex items-center gap-1">
              <Search class="w-3.5 h-3.5 text-slate-400" />
              <span>Search Applicants</span>
            </span>
            <span class="text-[11px] font-normal text-slate-400">Search by ID, Name, or NIK</span>
          </label>
          <div class="relative flex items-center">
            <Search class="w-4 h-4 text-slate-400 absolute left-3 pointer-events-none" />
            <InputText
              v-model="searchInput"
              type="text"
              placeholder="Search by application number, applicant name, or NIK..."
              class="w-full !pl-9 !pr-8 !text-xs !py-2 !rounded-lg"
              data-testid="search-applicants-input"
            />
            <button
              v-if="searchInput"
              type="button"
              class="absolute right-2.5 text-slate-400 hover:text-slate-600 cursor-pointer p-0.5"
              title="Clear search"
              @click="searchInput = ''"
            >
              <XCircle class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 4. PrimeVue DataTable Work Queue -->
    <div
      class="bg-white rounded-xl border border-slate-200/80 shadow-2xs overflow-hidden"
      data-testid="intake-queue-card"
    >
      <DataTable
        :value="applications ?? []"
        :loading="isLoadingQueue"
        paginator
        :rows="10"
        :rows-per-page-options="[10, 20, 50]"
        striped-rows
        responsive-layout="scroll"
        data-key="id"
        class="text-xs"
        data-testid="intake-datatable"
      >
        <template #empty>
          <EmptyState
            title="No admission applications found"
            description="No applications match the current filter criteria or search query."
            data-testid="queue-empty-state"
          >
            <template #action>
              <Button
                size="small"
                severity="secondary"
                variant="outlined"
                class="!text-xs"
                @click="resetFilters"
              >
                Clear Filters
              </Button>
            </template>
          </EmptyState>
        </template>

        <!-- Column 1: Application Number -->
        <Column field="applicationNumber" header="Application #" sortable style="min-width: 150px">
          <template #body="{ data }">
            <button
              type="button"
              class="inline-flex items-center gap-1 font-mono text-xs font-semibold text-teal-700 hover:text-teal-900 hover:underline bg-teal-50/70 hover:bg-teal-100 px-2 py-0.5 rounded border border-teal-200/80 transition-colors cursor-pointer text-left"
              data-testid="app-number-link"
              @click="openDossier(data.id)"
            >
              <span>{{ data.applicationNumber }}</span>
            </button>
          </template>
        </Column>

        <!-- Column 2: Applicant Name -->
        <Column field="applicantName" header="Applicant Name" sortable style="min-width: 220px">
          <template #body="{ data }">
            <div class="flex items-center gap-2.5 py-0.5">
              <div class="w-7 h-7 rounded-full bg-teal-50 text-teal-800 border border-teal-200/80 flex items-center justify-center font-bold text-[11px] shrink-0 shadow-2xs">
                {{ getInitials(data.applicantName) }}
              </div>
              <div class="min-w-0">
                <span class="font-medium text-slate-900 block truncate leading-tight">{{ data.applicantName }}</span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 3: NIK / Gov ID -->
        <Column field="governmentIdNumber" header="NIK / Gov ID" sortable style="min-width: 140px">
          <template #body="{ data }">
            <span v-if="data.governmentIdNumber" class="font-mono text-xs text-slate-700 bg-slate-50 px-2 py-0.5 rounded border border-slate-200/70">
              {{ data.governmentIdNumber }}
            </span>
            <span v-else class="text-slate-400 italic text-xs">—</span>
          </template>
        </Column>

        <!-- Column 4: Entry Level -->
        <Column field="requestedEntryLevel" header="Entry Level" sortable style="min-width: 115px">
          <template #body="{ data }">
            <span
              :class="[
                'inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold border',
                getLevelBadgeStyle(data.requestedEntryLevel),
              ]"
            >
              {{ data.requestedEntryLevel }}
            </span>
          </template>
        </Column>

        <!-- Column 5: Target Period -->
        <Column field="admissionPeriodId" header="Target Period" style="min-width: 160px">
          <template #body="{ data }">
            <span class="text-slate-700 truncate max-w-[200px] block text-xs" :title="getPeriodName(data.admissionPeriodId)">
              {{ getPeriodName(data.admissionPeriodId) }}
            </span>
          </template>
        </Column>

        <!-- Column 6: Registration Date -->
        <Column field="registrationDate" header="Reg. Date" sortable style="min-width: 120px">
          <template #body="{ data }">
            <span class="text-slate-600 text-xs">{{ formatDisplayDate(data.registrationDate) }}</span>
          </template>
        </Column>

        <!-- Column 7: Status -->
        <Column field="status" header="Status" sortable style="min-width: 125px">
          <template #body="{ data }">
            <StatusPill :status="data.status" size="sm" />
          </template>
        </Column>

        <!-- Column 8: Actions -->
        <Column header="Actions" style="min-width: 125px; text-align: right">
          <template #body="{ data }">
            <Button
              size="small"
              severity="secondary"
              variant="outlined"
              class="!text-xs !py-1 !px-2.5 cursor-pointer hover:!bg-teal-50 hover:!text-teal-700 hover:!border-teal-300 transition-colors"
              data-testid="view-dossier-button"
              @click="openDossier(data.id)"
            >
              <Eye class="w-3.5 h-3.5 mr-1 text-teal-600" />
              <span>View Dossier</span>
            </Button>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- 5. Slide-over Drawer for Selected Application Dossier -->
    <Drawer
      v-model:visible="isDossierOpen"
      position="right"
      class="!w-full md:!w-[680px] lg:!w-[760px]"
      data-testid="dossier-drawer"
      @hide="closeDossier"
    >
      <template #header>
        <div class="flex items-start justify-between w-full pr-2" data-testid="dossier-header">
          <div class="flex items-start gap-3.5">
            <div class="w-11 h-11 rounded-xl bg-teal-100 text-teal-800 border border-teal-200 flex items-center justify-center font-bold text-base shrink-0 shadow-2xs mt-0.5">
              {{ getInitials(dossier?.applicant?.fullName) }}
            </div>
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="font-mono text-xs font-semibold text-teal-700 bg-teal-50 px-2 py-0.5 rounded border border-teal-200">
                  {{ dossier?.applicationNumber ?? 'Loading...' }}
                </span>
                <StatusPill v-if="dossier" :status="dossier.status" size="sm" />
              </div>
              <h2 class="text-xl font-bold text-slate-900 leading-tight">
                {{ dossier?.applicant.fullName ?? 'Intake Dossier' }}
              </h2>
              <p v-if="dossier" class="text-xs text-slate-500 flex flex-wrap items-center gap-2">
                <span>Entry: <strong class="text-slate-700">{{ dossier.requestedEntryLevel }}</strong></span>
                <span>•</span>
                <span>Target: <strong class="text-slate-700">{{ getPeriodName(dossier.admissionPeriodId) }}</strong></span>
                <span>•</span>
                <span>Registered: <strong class="text-slate-700">{{ formatDisplayDate(dossier.registrationDate) }}</strong></span>
              </p>
            </div>
          </div>
        </div>
      </template>

      <!-- Dossier Content Body -->
      <div v-if="isLoadingDossier" class="py-12 flex justify-center items-center">
        <LoadingSpinner label="Loading application dossier..." />
      </div>

      <div v-else-if="isDossierError || !dossier" class="p-6 text-center space-y-3">
        <AlertCircle class="w-10 h-10 text-rose-500 mx-auto" />
        <h4 class="text-sm font-semibold text-slate-800">Unable to load intake dossier</h4>
        <p class="text-xs text-slate-500">The application may have been removed or is temporarily unreachable.</p>
        <Button size="small" severity="secondary" variant="outlined" @click="() => refetchDossier()">
          Retry
        </Button>
      </div>

      <div v-else class="space-y-6 pt-2 pb-8" data-testid="dossier-content">
        <!-- Enrollment Success Banner -->
        <div
          v-if="lastEnrolledStudent"
          class="p-4 rounded-xl bg-teal-50 border border-teal-200 text-teal-900 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 shadow-2xs"
          data-testid="enrollment-success-banner"
        >
          <div class="flex items-center gap-3">
            <div class="p-2 bg-teal-100 text-teal-700 rounded-lg shrink-0">
              <CheckCircle2 class="w-5 h-5 text-teal-700" />
            </div>
            <div>
              <p class="text-sm font-semibold text-teal-900">
                Student Enrolled Successfully
              </p>
              <p class="text-xs text-teal-700">
                Student Number:
                <span class="font-mono font-bold" data-testid="enrolled-student-number">{{
                  lastEnrolledStudent.studentNumber
                }}</span>
              </p>
            </div>
          </div>
          <Button
            size="small"
            class="!text-xs !py-1.5 cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white font-medium shrink-0"
            data-testid="open-records-desk-button"
            @click="router.push('/students/records')"
          >
            <ExternalLink class="w-3.5 h-3.5 mr-1.5" />
            <span>Open in Student Records Desk</span>
          </Button>
        </div>

        <!-- Dossier Workflow Actions Bar -->
        <div
          class="flex flex-wrap items-center justify-between gap-3 p-3.5 bg-slate-50 rounded-xl border border-slate-200/90 shadow-2xs"
          data-testid="dossier-actions-bar"
        >
          <div class="flex items-center gap-2 text-xs text-slate-700 font-semibold">
            <div class="w-6 h-6 rounded-md bg-teal-100 text-teal-700 flex items-center justify-center">
              <Shield class="w-3.5 h-3.5" />
            </div>
            <span>Workflow Governance:</span>
          </div>

          <div class="flex items-center gap-2">
            <!-- Start Review (Only when registered) -->
            <Button
              v-if="dossier.status === 'registered'"
              size="small"
              severity="primary"
              :disabled="!canReview || startReviewMutation.isPending.value"
              :loading="startReviewMutation.isPending.value"
              class="!text-xs cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700 shadow-xs"
              data-testid="start-review-button"
              @click="handleStartReview(dossier.id)"
            >
              <Play class="w-3.5 h-3.5 mr-1" />
              <span>Start Review</span>
            </Button>

            <!-- Record Decision (Only when under_review) -->
            <Button
              v-if="dossier.status === 'under_review'"
              size="small"
              severity="success"
              :disabled="!canDecide"
              class="!text-xs cursor-pointer shadow-xs"
              data-testid="record-decision-button"
              @click="openRecordDecisionModal"
            >
              <Gavel class="w-3.5 h-3.5 mr-1" />
              <span>Record Decision</span>
            </Button>

            <!-- Enroll Applicant (Only when decided & accepted) -->
            <Button
              v-if="dossier.status === 'decided' && dossier.decision?.decisionType === 'accepted' && canEnroll"
              size="small"
              class="!text-xs cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white font-medium shadow-xs"
              data-testid="enroll-applicant-button"
              @click="openEnrollModal"
            >
              <UserCheck class="w-3.5 h-3.5 mr-1" />
              <span>Enroll Applicant</span>
            </Button>

            <!-- Withdraw (Only when active: registered or under_review) -->
            <Button
              v-if="isApplicationActive(dossier.status)"
              size="small"
              severity="danger"
              variant="outlined"
              :disabled="!canWithdraw"
              class="!text-xs cursor-pointer hover:!bg-rose-50"
              data-testid="withdraw-application-button"
              @click="openWithdrawModal"
            >
              <Ban class="w-3.5 h-3.5 mr-1" />
              <span>Withdraw</span>
            </Button>
          </div>
        </div>

        <!-- 4 Dossier Tabs Container -->
        <Tabs v-model:value="activeDossierTab" class="w-full" data-testid="dossier-tabs">
          <TabList class="border-b border-slate-200">
            <Tab value="info" data-testid="tab-info">
              <div class="flex items-center gap-1.5 py-1 text-xs">
                <UserCheck class="w-3.5 h-3.5 text-teal-600" />
                <span class="font-medium">Application Info</span>
              </div>
            </Tab>
            <Tab value="documents" data-testid="tab-documents">
              <div class="flex items-center gap-1.5 py-1 text-xs">
                <FileCheck class="w-3.5 h-3.5 text-teal-600" />
                <span class="font-medium">Documents</span>
                <span
                  v-if="dossier.documents?.length"
                  class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-slate-200 text-slate-700 font-semibold"
                  data-testid="documents-tab-count"
                >
                  {{ dossier.documents.length }}
                </span>
              </div>
            </Tab>
            <Tab value="assessment" data-testid="tab-assessment">
              <div class="flex items-center gap-1.5 py-1 text-xs">
                <GraduationCap class="w-3.5 h-3.5 text-teal-600" />
                <span class="font-medium">Assessment Evidence</span>
                <span
                  v-if="dossier.assessmentEvidence?.length"
                  class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-slate-200 text-slate-700 font-semibold"
                  data-testid="assessment-tab-count"
                >
                  {{ dossier.assessmentEvidence.length }}
                </span>
              </div>
            </Tab>
            <Tab value="entrance-assessment" data-testid="tab-entrance-assessment">
              <div class="flex items-center gap-1.5 py-1 text-xs">
                <GraduationCap class="w-3.5 h-3.5 text-teal-600" />
                <span class="font-medium">Entrance Assessment</span>
              </div>
            </Tab>
            <Tab value="decision" data-testid="tab-decision">
              <div class="flex items-center gap-1.5 py-1 text-xs">
                <Gavel class="w-3.5 h-3.5 text-teal-600" />
                <span class="font-medium">Decision History</span>
                <span
                  v-if="dossier.decision"
                  class="w-2 h-2 rounded-full bg-emerald-500 ml-0.5"
                  title="Decision Recorded"
                />
              </div>
            </Tab>
          </TabList>

          <TabPanels class="pt-4">
            <!-- Tab 1: Application Info & Guardian Contacts -->
            <TabPanel value="info" data-testid="panel-info">
              <ApplicationInfoTab
                :dossier="dossier"
                :periods="admissionPeriods"
              />
            </TabPanel>

            <!-- Tab 2: Document Verification Checklist -->
            <TabPanel value="documents" data-testid="panel-documents">
              <DocumentVerificationTab
                :application-id="dossier.id"
                :documents="dossier.documents ?? []"
                :is-read-only="!isApplicationActive(dossier.status)"
                @verify="handleOpenVerifyDocument"
              />
            </TabPanel>

            <!-- Tab 3: Entrance Assessment Diagnostic Evidence (Manual) -->
            <TabPanel value="assessment" data-testid="panel-assessment">
              <AssessmentEvidenceTab
                :application-id="dossier.id"
                :assessment-evidence="dossier.assessmentEvidence ?? []"
                :is-read-only="!isApplicationActive(dossier.status)"
                @add="handleAddAssessmentScore"
                @edit="handleEditAssessmentScore"
                @delete="handleDeleteAssessmentScore"
              />
            </TabPanel>

            <!-- Tab 4: Academics Diagnostic Entrance Exam Results -->
            <TabPanel value="entrance-assessment" data-testid="panel-entrance-assessment">
              <ApplicantAssessmentTab
                :applicant-id="dossier.applicantId"
                :application-id="dossier.id"
                :applicant-name="dossier.applicant.fullName"
              />
            </TabPanel>

            <!-- Tab 5: Decision & Governance History -->
            <TabPanel value="decision" data-testid="panel-decision">
              <DecisionHistoryTab
                :dossier="dossier"
                :periods="admissionPeriods"
              />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
    </Drawer>

    <!-- Modals -->
    <!-- 1. Record Decision Modal -->
    <RecordDecisionModal
      v-model:visible="isRecordDecisionModalOpen"
      :application-id="dossier?.id ?? ''"
      :periods="admissionPeriods"
      @saved="() => refetchDossier()"
    />

    <!-- 2. Withdraw Application Modal -->
    <WithdrawApplicationModal
      v-model:visible="isWithdrawModalOpen"
      :application-id="dossier?.id ?? ''"
      @withdrawn="() => refetchDossier()"
    />

    <!-- 3. Verify Document Modal -->
    <VerifyDocumentModal
      v-model:visible="isVerifyDocumentModalOpen"
      :application-id="dossier?.id ?? ''"
      :document="selectedDocumentForVerification"
      @verified="() => refetchDossier()"
    />

    <!-- 4. Assessment Evidence Modal -->
    <AssessmentEvidenceModal
      v-model:visible="isAssessmentModalOpen"
      :application-id="dossier?.id ?? ''"
      :evidence="selectedEvidenceForEdit"
      @saved="() => refetchDossier()"
    />

    <!-- 5. Enroll Applicant Modal -->
    <EnrollApplicantModal
      v-model:visible="isEnrollModalOpen"
      :dossier="dossier"
      :periods="admissionPeriods"
      @success="handleEnrollSuccess"
      @close="isEnrollModalOpen = false"
    />
  </div>
</template>
