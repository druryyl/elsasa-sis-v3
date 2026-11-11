<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { refDebounced } from '@vueuse/core';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  Search,
  RotateCcw,
  GraduationCap,
  Users,
  UserPlus,
  UserCheck,
  FileText,
  Pencil,
  Upload,
  Shield,
  Mail,
  Phone,
  MapPin,
  Calendar,
  Building2,
  AlertCircle,
  CheckCircle2,
  Download,
  Info,
  Clock,
  IdCard,
  History,
  Activity,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
  type StatusVariant,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { useAuthStore } from '@/app/stores/auth.store';
import { RECORDS_PERMISSIONS } from '../constants/permissions';
import type { StudentSearchParams } from '../types/records.types';
import {
  useStudentSearchQuery,
  useStudentDossierQuery,
} from '../composables/useStudentRecordsQueries';
import {
  StudentIdentityTab,
  EditIdentityModal,
  StudentGuardiansTab,
  AddGuardianModal,
  StudentDocumentsTab,
  UploadDocumentModal,
  StudentEnrollmentHistoryTab,
  StudentPlacementTab,
  ChangePlacementModal,
  StudentAdministrativeHistoryTab,
  ChangeStatusModal,
} from '../components';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Students', to: '/students/records' },
  { label: 'Student Records Desk', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canEdit = computed(() => authStore.hasPermission(RECORDS_PERMISSIONS.EDIT));

// ---------------------------------------------------------------------------
// 3. Search & Filters State (Left Pane)
// ---------------------------------------------------------------------------
const searchInput = ref('');
const debouncedSearch = refDebounced(searchInput, 300);

const selectedCampus = ref('');
const selectedLevel = ref('');
const selectedStatus = ref('');
const selectedCohort = ref('');

const campusOptions = [
  { label: 'All Campuses', value: '' },
  { label: 'Main Campus (campus-main)', value: 'campus-main' },
  { label: 'East Campus (campus-east)', value: 'campus-east' },
  { label: 'Main Campus (Kampus A)', value: 'Main Campus (Kampus A)' },
  { label: 'East Campus (Kampus B)', value: 'East Campus (Kampus B)' },
];

const levelOptions = [
  { label: 'All School Levels', value: '' },
  { label: 'ELC (Early Learning)', value: 'ELC' },
  { label: 'JHLC (Junior High)', value: 'JHLC' },
  { label: 'HSLC (Senior High)', value: 'HSLC' },
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

const statusOptions = [
  { label: 'All Statuses', value: '' },
  { label: 'Enrolled', value: 'enrolled' },
  { label: 'Active', value: 'active' },
  { label: 'On Leave', value: 'on_leave' },
  { label: 'Withdrawn', value: 'withdrawn' },
  { label: 'Transferred Out', value: 'transferred_out' },
  { label: 'Graduated', value: 'graduated' },
];

function resetFilters() {
  searchInput.value = '';
  selectedCampus.value = '';
  selectedLevel.value = '';
  selectedStatus.value = '';
  selectedCohort.value = '';
}

useRouteQueryHydration((query) => {
  const status = getQueryString(query, 'status');
  if (status) selectedStatus.value = status;

  const campus = getQueryString(query, 'campus');
  if (campus) selectedCampus.value = campus;

  const schoolLevel = getQueryString(query, 'schoolLevel') ?? getQueryString(query, 'level');
  if (schoolLevel) selectedLevel.value = schoolLevel;

  const cohort = getQueryString(query, 'cohort');
  if (cohort) selectedCohort.value = cohort;
});

const searchParams = computed<StudentSearchParams>(() => {
  const params: StudentSearchParams = {
    pageNumber: 1,
    pageSize: 50,
  };
  const term = debouncedSearch.value?.trim();
  if (term) params.searchTerm = term;
  if (selectedCampus.value) params.campus = selectedCampus.value;
  if (selectedLevel.value) params.level = selectedLevel.value;
  if (selectedStatus.value) params.status = selectedStatus.value;
  return params;
});

// ---------------------------------------------------------------------------
// 4. Candidate Search Query & Metrics
// ---------------------------------------------------------------------------
const {
  data: searchResult,
  isLoading: isLoadingCandidates,
  isFetching: isFetchingCandidates,
} = useStudentSearchQuery(searchParams);

const candidates = computed(() => searchResult.value?.items ?? []);

// Summary Metrics Counters
const totalStudentsCount = computed(() => searchResult.value?.totalCount ?? candidates.value.length);

const enrolledCount = computed(() =>
  candidates.value.filter((s) => s.currentStatus.toLowerCase() === 'enrolled').length,
);

const activeCount = computed(() =>
  candidates.value.filter((s) => s.currentStatus.toLowerCase() === 'active').length,
);

// ---------------------------------------------------------------------------
// 5. Selected Student & Dossier Query (Right Pane)
// ---------------------------------------------------------------------------
const selectedStudentId = ref<string | null>(null);
const activeTab = ref<string>('identity');

watch(
  () => route.params.id,
  (idParam) => {
    if (typeof idParam === 'string' && idParam.trim()) {
      selectedStudentId.value = idParam.trim();
    } else {
      selectedStudentId.value = null;
    }
  },
  { immediate: true },
);

function selectStudent(id: string) {
  selectedStudentId.value = id;
  router.push({ name: 'students.records.detail', params: { id } });
}

const {
  data: dossier,
  isLoading: isLoadingDossier,
  isError: isDossierError,
  refetch: refetchDossier,
} = useStudentDossierQuery(selectedStudentId, {
  enabled: computed(() => Boolean(selectedStudentId.value)),
});

const student = computed(() => dossier.value);

const statusBadgeVariant = computed<StatusVariant | undefined>(() => {
  const s = dossier.value?.currentStatus?.toLowerCase().trim();
  if (s === 'active') return 'emerald';
  if (s === 'enrolled') return 'sky';
  return undefined;
});

// ---------------------------------------------------------------------------
// 6. Action Modals State
// ---------------------------------------------------------------------------
const isEditIdentityModalOpen = ref(false);
const isAddGuardianModalOpen = ref(false);
const isUploadDocumentModalOpen = ref(false);
const isChangePlacementModalOpen = ref(false);
const isChangeStatusModalOpen = ref(false);

function handleEditIdentity() {
  isEditIdentityModalOpen.value = true;
}

function handleAddGuardian() {
  isAddGuardianModalOpen.value = true;
}

function handleUploadDocument() {
  isUploadDocumentModalOpen.value = true;
}

function openChangePlacementModal() {
  isChangePlacementModalOpen.value = true;
}

function openChangeStatusModal() {
  isChangeStatusModalOpen.value = true;
}
</script>

<template>
  <div class="space-y-6" data-testid="student-records-desk-view">
    <!-- Header -->
    <PageHeader
      title="Student Records Desk"
      description="Authoritative student identity, certified biographical records, guardian governance, and official documents."
      :breadcrumbs="breadcrumbs"
    />

    <!-- Two-Pane Master-Detail Shell (SCR-STU-02) -->
    <div class="flex flex-col lg:flex-row gap-6 items-start">
      <!-- ================================================================= -->
      <!-- LEFT PANE: 35% Search, Filters, Metrics & Candidate List          -->
      <!-- ================================================================= -->
      <div class="w-full lg:w-[35%] space-y-4" data-testid="records-left-pane">
        <!-- Summary Counters -->
        <div class="grid grid-cols-3 gap-2" data-testid="summary-counters">
          <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-lg p-3 text-center shadow-xs">
            <span class="text-[11px] font-medium text-surface-500 uppercase tracking-wider block">Total</span>
            <span class="text-xl font-bold text-surface-900 dark:text-surface-100" data-testid="counter-total">
              {{ totalStudentsCount }}
            </span>
          </div>

          <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-lg p-3 text-center shadow-xs">
            <span class="text-[11px] font-medium text-teal-600 uppercase tracking-wider block">Enrolled</span>
            <span class="text-xl font-bold text-teal-700 dark:text-teal-400" data-testid="counter-enrolled">
              {{ enrolledCount }}
            </span>
          </div>

          <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-lg p-3 text-center shadow-xs">
            <span class="text-[11px] font-medium text-emerald-600 uppercase tracking-wider block">Active</span>
            <span class="text-xl font-bold text-emerald-700 dark:text-emerald-400" data-testid="counter-active">
              {{ activeCount }}
            </span>
          </div>
        </div>

        <!-- Search Input & Filters Card -->
        <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 shadow-xs space-y-3">
          <!-- Search Input with Debounce -->
          <div class="relative">
            <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400 pointer-events-none" />
            <InputText
              v-model="searchInput"
              placeholder="Search student #, name, NIK..."
              class="w-full !pl-9 !text-sm"
              data-testid="search-input"
            />
          </div>

          <!-- Filter Selects -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
            <!-- Campus Select -->
            <Select
              v-model="selectedCampus"
              :options="campusOptions"
              option-label="label"
              option-value="value"
              placeholder="All Campuses"
              class="w-full !text-xs"
              data-testid="filter-campus"
            />

            <!-- School Level Select -->
            <Select
              v-model="selectedLevel"
              :options="levelOptions"
              option-label="label"
              option-value="value"
              placeholder="All Levels"
              class="w-full !text-xs"
              data-testid="filter-level"
            />

            <!-- Status Select -->
            <Select
              v-model="selectedStatus"
              :options="statusOptions"
              option-label="label"
              option-value="value"
              placeholder="All Statuses"
              class="w-full !text-xs"
              data-testid="filter-status"
            />
          </div>

          <!-- Reset Filter Button -->
          <div class="flex items-center justify-between pt-1 text-xs">
            <div class="flex items-center gap-2 flex-wrap">
              <span class="text-surface-500">
                Showing <strong>{{ candidates.length }}</strong> match{{ candidates.length === 1 ? '' : 'es' }}
              </span>
              <span
                v-if="selectedCohort"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-teal-50 text-teal-700 border border-teal-200 text-[11px] font-medium"
                data-testid="active-cohort-chip"
              >
                Cohort: {{ selectedCohort }}
              </span>
            </div>
            <Button
              size="small"
              variant="text"
              severity="secondary"
              class="!text-xs !py-0.5 !px-2 cursor-pointer flex items-center gap-1"
              data-testid="reset-filters-button"
              @click="resetFilters"
            >
              <RotateCcw class="w-3.5 h-3.5" />
              <span>Reset Filters</span>
            </Button>
          </div>
        </div>

        <!-- Scrollable Candidate Match List -->
        <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-3 shadow-xs space-y-2">
          <div class="flex items-center justify-between pb-2 border-b border-surface-100 dark:border-surface-800 text-xs text-surface-500">
            <span class="font-medium">Candidate List</span>
            <span v-if="isFetchingCandidates" class="text-teal-600 animate-pulse text-[11px]">Updating...</span>
          </div>

          <!-- Loading state -->
          <div v-if="isLoadingCandidates" class="py-12 flex justify-center items-center" data-testid="loading-candidates">
            <LoadingSpinner label="Searching student records..." />
          </div>

          <!-- Empty state when no matches found -->
          <div v-else-if="candidates.length === 0" class="py-8" data-testid="empty-candidates">
            <EmptyState
              title="No students found"
              description="Try adjusting your search query, campus, or status filters."
              size="sm"
            />
          </div>

          <!-- Candidate Items -->
          <div v-else class="space-y-2 max-h-[calc(100vh-380px)] min-h-[220px] overflow-y-auto pr-1" data-testid="student-candidate-list">
            <div
              v-for="student in candidates"
              :key="student.id"
              class="p-3 rounded-lg border transition-all cursor-pointer select-none"
              :class="[
                selectedStudentId === student.id
                  ? 'border-teal-500 bg-teal-50/70 dark:bg-teal-950/40 ring-1 ring-teal-500 shadow-xs'
                  : 'border-surface-200 dark:border-surface-700 hover:border-surface-300 dark:hover:border-surface-600 bg-surface-50/50 dark:bg-surface-800/40 hover:bg-surface-50 dark:hover:bg-surface-800'
              ]"
              :data-testid="`candidate-card-${student.id}`"
              @click="selectStudent(student.id)"
            >
              <!-- Top Row: Student Number & Status Pill -->
              <div class="flex items-center justify-between gap-2 mb-1">
                <span
                  class="font-mono text-xs font-semibold text-teal-700 dark:text-teal-300 bg-teal-100/60 dark:bg-teal-950 px-2 py-0.5 rounded border border-teal-200 dark:border-teal-800"
                  data-testid="candidate-number"
                >
                  {{ student.studentNumber }}
                </span>
                <StatusPill :status="student.currentStatus" size="sm" data-testid="candidate-status" />
              </div>

              <!-- Middle: Full Name -->
              <div class="font-semibold text-sm text-surface-900 dark:text-surface-100 line-clamp-1" data-testid="candidate-name">
                {{ student.fullName }}
                <span v-if="student.preferredName" class="text-xs font-normal text-surface-500">
                  ({{ student.preferredName }})
                </span>
              </div>

              <!-- Bottom Row: Grade Level & Independent Badge -->
              <div class="mt-2 flex items-center justify-between text-xs text-surface-500 dark:text-surface-400">
                <span class="truncate flex items-center gap-1" data-testid="candidate-placement">
                  <GraduationCap class="w-3.5 h-3.5 text-surface-400" />
                  {{ student.initialLevel || 'N/A' }} • {{ student.initialCampus || 'N/A' }}
                </span>
                <span
                  v-if="student.isIndependent"
                  class="shrink-0 text-[10px] font-semibold text-purple-700 dark:text-purple-300 bg-purple-50 dark:bg-purple-950 px-1.5 py-0.5 rounded border border-purple-200 dark:border-purple-800"
                  data-testid="candidate-independent-badge"
                >
                  Independent
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ================================================================= -->
      <!-- RIGHT PANE: 65% 360° Student Dossier Panel                         -->
      <!-- ================================================================= -->
      <div class="w-full lg:w-[65%] space-y-4" data-testid="records-right-pane">
        <!-- Empty Prompt State when no student selected -->
        <div
          v-if="!selectedStudentId"
          class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs"
          data-testid="empty-dossier-prompt"
        >
          <EmptyState
            title="Select a student to inspect records"
            description="Choose an enrolled or active student from the left pane to view their 360° student dossier, manage guardians, edit certified identity, or inspect official records."
          >
            <template #icon>
              <div class="w-16 h-16 rounded-full bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center mx-auto mb-4 text-teal-600">
                <GraduationCap class="w-8 h-8" />
              </div>
            </template>
          </EmptyState>
        </div>

        <!-- Loading Dossier State -->
        <div
          v-else-if="isLoadingDossier"
          class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-16 flex justify-center items-center shadow-xs"
          data-testid="loading-dossier"
        >
          <LoadingSpinner label="Loading 360° student dossier..." />
        </div>

        <!-- Error Dossier State -->
        <div
          v-else-if="isDossierError || !dossier"
          class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-8 text-center space-y-3 shadow-xs"
          data-testid="error-dossier"
        >
          <AlertCircle class="w-10 h-10 text-rose-500 mx-auto" />
          <h3 class="text-base font-semibold text-surface-900 dark:text-surface-100">Unable to load student record</h3>
          <p class="text-xs text-surface-500">The student record may have been moved or is temporarily unavailable.</p>
          <Button size="small" severity="secondary" variant="outlined" class="cursor-pointer" @click="() => refetchDossier()">
            Retry
          </Button>
        </div>

        <!-- 360° Student Dossier Loaded -->
        <div
          v-else
          class="space-y-4"
          data-testid="student-dossier-panel"
        >
          <!-- Dossier Header Card -->
          <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-5 shadow-xs space-y-4" data-testid="dossier-header">
            <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
              <div class="space-y-1.5">
                <div class="flex flex-wrap items-center gap-2">
                  <!-- Student Number Badge -->
                  <span
                    class="font-mono text-xs font-bold text-teal-800 dark:text-teal-300 bg-teal-50 dark:bg-teal-950 px-2.5 py-1 rounded-md border border-teal-200 dark:border-teal-800"
                    data-testid="dossier-student-number"
                  >
                    {{ dossier.studentNumber }}
                  </span>

                  <!-- Status Badge -->
                  <StatusPill :status="dossier.currentStatus" :variant="statusBadgeVariant" size="md" data-testid="dossier-status-badge" />

                  <!-- Independent Student Badge -->
                  <span
                    v-if="dossier.isIndependent"
                    class="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full bg-purple-100 dark:bg-purple-950 text-purple-700 dark:text-purple-300 border border-purple-300 dark:border-purple-800"
                    data-testid="dossier-independent-badge"
                  >
                    <Shield class="w-3.5 h-3.5" />
                    Independent Student
                  </span>
                </div>

                <h1 class="text-2xl font-bold text-surface-900 dark:text-surface-50 tracking-tight" data-testid="dossier-full-name">
                  {{ dossier.fullName }}
                </h1>

                <p v-if="dossier.preferredName" class="text-xs text-surface-500">
                  Preferred Name: <strong class="text-surface-700 dark:text-surface-300">{{ dossier.preferredName }}</strong>
                </p>
              </div>

              <!-- Quick Action Buttons -->
              <div class="flex flex-wrap items-center gap-2" data-testid="dossier-quick-actions">
                <Button
                  size="small"
                  severity="secondary"
                  variant="outlined"
                  :disabled="!canEdit"
                  class="!text-xs cursor-pointer"
                  data-testid="change-status-button"
                  @click="openChangeStatusModal"
                >
                  <Activity class="w-3.5 h-3.5 mr-1 text-teal-600" />
                  <span>Change Status</span>
                </Button>

                <Button
                  size="small"
                  severity="secondary"
                  variant="outlined"
                  :disabled="!canEdit"
                  class="!text-xs cursor-pointer"
                  data-testid="edit-identity-button"
                  @click="handleEditIdentity"
                >
                  <Pencil class="w-3.5 h-3.5 mr-1 text-teal-600" />
                  <span>Edit Identity</span>
                </Button>

                <Button
                  size="small"
                  severity="secondary"
                  variant="outlined"
                  :disabled="!canEdit"
                  class="!text-xs cursor-pointer"
                  data-testid="add-guardian-button"
                  @click="handleAddGuardian"
                >
                  <UserPlus class="w-3.5 h-3.5 mr-1 text-teal-600" />
                  <span>Add Guardian</span>
                </Button>

                <Button
                  size="small"
                  severity="secondary"
                  variant="outlined"
                  :disabled="!canEdit"
                  class="!text-xs cursor-pointer"
                  data-testid="upload-document-button"
                  @click="handleUploadDocument"
                >
                  <Upload class="w-3.5 h-3.5 mr-1 text-teal-600" />
                  <span>Upload Document</span>
                </Button>
              </div>
            </div>
          </div>

          <!-- Dossier Tabs Navigation -->
          <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden">
            <Tabs v-model:value="activeTab" class="w-full" data-testid="dossier-tabs">
              <TabList class="border-b border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/50 px-3">
                <Tab value="identity" data-testid="tab-identity">
                  <div class="flex items-center gap-1.5 py-1 text-xs">
                    <UserCheck class="w-3.5 h-3.5 text-teal-600" />
                    <span class="font-medium">Certified Identity</span>
                  </div>
                </Tab>

                <Tab value="placement" data-testid="tab-placement">
                  <div class="flex items-center gap-1.5 py-1 text-xs">
                    <Building2 class="w-3.5 h-3.5 text-teal-600" />
                    <span class="font-medium">Placement</span>
                  </div>
                </Tab>

                <Tab value="guardians" data-testid="tab-guardians">
                  <div class="flex items-center gap-1.5 py-1 text-xs">
                    <Users class="w-3.5 h-3.5 text-teal-600" />
                    <span class="font-medium">Guardians &amp; Household</span>
                    <span
                      v-if="dossier.guardianRelationships?.length"
                      class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-surface-200 dark:bg-surface-700 text-surface-700 dark:text-surface-200 font-semibold"
                    >
                      {{ dossier.guardianRelationships.length }}
                    </span>
                  </div>
                </Tab>

                <Tab value="documents" data-testid="tab-documents">
                  <div class="flex items-center gap-1.5 py-1 text-xs">
                    <FileText class="w-3.5 h-3.5 text-teal-600" />
                    <span class="font-medium">Official Documents</span>
                    <span
                      v-if="dossier.documents?.length"
                      class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-surface-200 dark:bg-surface-700 text-surface-700 dark:text-surface-200 font-semibold"
                    >
                      {{ dossier.documents.length }}
                    </span>
                  </div>
                </Tab>

                <Tab value="enrollment" data-testid="tab-enrollment">
                  <div class="flex items-center gap-1.5 py-1 text-xs">
                    <GraduationCap class="w-3.5 h-3.5 text-teal-600" />
                    <span class="font-medium">Enrollment History</span>
                    <span
                      v-if="dossier.enrollmentHistory?.length"
                      class="ml-1 px-1.5 py-0.2 rounded-full text-[10px] bg-surface-200 dark:bg-surface-700 text-surface-700 dark:text-surface-200 font-semibold"
                    >
                      {{ dossier.enrollmentHistory.length }}
                    </span>
                  </div>
                </Tab>

                <Tab value="history" data-testid="tab-history">
                  <div class="flex items-center gap-1.5 py-1 text-xs" data-testid="tab-administrative-history">
                    <History class="w-3.5 h-3.5 text-teal-600" />
                    <span class="font-medium">Administrative History</span>
                  </div>
                </Tab>
              </TabList>

              <TabPanels class="p-5">
                <!-- ======================================================= -->
                <!-- TAB 1: Certified Identity                               -->
                <!-- ======================================================= -->
                <TabPanel value="identity" data-testid="panel-identity">
                  <StudentIdentityTab
                    :student="dossier"
                    :can-edit="canEdit"
                    @edit="handleEditIdentity"
                  />
                </TabPanel>

                <!-- ======================================================= -->
                <!-- TAB 2: Placement                                        -->
                <!-- ======================================================= -->
                <TabPanel value="placement" data-testid="panel-placement">
                  <StudentPlacementTab
                    v-if="student"
                    :student="student"
                    :can-edit="canEdit"
                    @change-placement="openChangePlacementModal"
                  />
                </TabPanel>

                <!-- ======================================================= -->
                <!-- TAB 3: Guardians & Household                           -->
                <!-- ======================================================= -->
                <TabPanel value="guardians" data-testid="panel-guardians">
                  <StudentGuardiansTab
                    :student="dossier"
                    :can-edit="canEdit"
                    @add-guardian="handleAddGuardian"
                  />
                </TabPanel>

                <!-- ======================================================= -->
                <!-- TAB 4: Official Documents                              -->
                <!-- ======================================================= -->
                <TabPanel value="documents" data-testid="panel-documents">
                  <StudentDocumentsTab
                    :student="dossier"
                    :can-edit="canEdit"
                    @upload-document="handleUploadDocument"
                  />
                </TabPanel>

                <!-- ======================================================= -->
                <!-- TAB 5: Enrollment History                              -->
                <!-- ======================================================= -->
                <TabPanel value="enrollment" data-testid="panel-enrollment">
                  <StudentEnrollmentHistoryTab
                    :student="dossier"
                  />
                </TabPanel>

                <!-- ======================================================= -->
                <!-- TAB 6: Administrative History                          -->
                <!-- ======================================================= -->
                <TabPanel value="history" data-testid="panel-history">
                  <div data-testid="panel-administrative-history">
                    <StudentAdministrativeHistoryTab
                      v-if="student"
                      :student-id="student.id"
                      :student="student"
                    />
                  </div>
                </TabPanel>
              </TabPanels>
            </Tabs>
          </div>
        </div>
      </div>
    </div>

    <!-- Action Modals -->
    <EditIdentityModal
      v-if="dossier"
      v-model:visible="isEditIdentityModalOpen"
      :student="dossier"
      @saved="() => refetchDossier()"
    />

    <AddGuardianModal
      v-if="dossier"
      v-model:visible="isAddGuardianModalOpen"
      :student-id="dossier.id"
      @saved="() => refetchDossier()"
    />

    <UploadDocumentModal
      v-if="dossier"
      v-model:visible="isUploadDocumentModalOpen"
      :student-id="dossier.id"
      @saved="() => refetchDossier()"
    />

    <ChangePlacementModal
      v-if="dossier"
      v-model:visible="isChangePlacementModalOpen"
      :student="dossier"
      @saved="() => refetchDossier()"
    />

    <ChangeStatusModal
      v-if="dossier"
      v-model:visible="isChangeStatusModalOpen"
      :student-id="dossier.id"
      :student="dossier"
      :current-status="dossier.currentStatus"
      @saved="() => refetchDossier()"
    />
  </div>
</template>
