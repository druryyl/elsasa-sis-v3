<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Dialog from 'primevue/dialog';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import Checkbox from 'primevue/checkbox';
import {
  CheckCircle2,
  FileCheck,
  GraduationCap,
  Layers,
  Printer,
  RefreshCw,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  UserCheck,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';

// Records Domain Composables & Types
import {
  useReportCardsQuery,
  useApproveReportCardMutation,
  usePublishReportCardMutation,
} from '../composables/useReportCards';
import {
  useStudentTranscriptQuery,
  useIssueTranscriptMutation,
} from '../composables/useTranscripts';
import {
  useAcademicResultsQuery,
  useApproveSummativeMutation,
  usePublishResultsMutation,
} from '@/features/academics/assessment/composables/useAcademicResults';
import { useAssessmentsQuery } from '@/features/academics/assessment/composables/useAssessments';
import { useLearningUnits } from '@/features/academics/curriculum/composables/useCurriculum';

import type {
  AcademicResult,
  ReportCard,
} from '../types';
import { RECORDS_PERMISSIONS, AUTHORITY_ROLES } from '../constants/permissions';

// Modals
import ReportCardPrintPreview from '../components/ReportCardPrintPreview.vue';
import RecordCorrectionModal from '../components/RecordCorrectionModal.vue';
import BatchCompileReportCardsModal from '../components/BatchCompileReportCardsModal.vue';
import GenerateInterimTranscriptModal from '../components/GenerateInterimTranscriptModal.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Academic Records Workbench', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Role Guards (UC-ACD-16, UC-ACD-19, TD-06)
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const isAuthority = computed(() =>
  authStore.hasAnyRole([...AUTHORITY_ROLES]) ||
  authStore.hasPermission(RECORDS_PERMISSIONS.RESULTS_APPROVE)
);

const canApproveResults = computed(() =>
  isAuthority.value || authStore.hasPermission(RECORDS_PERMISSIONS.RESULTS_APPROVE)
);

const canPublishResults = computed(() =>
  isAuthority.value || authStore.hasPermission(RECORDS_PERMISSIONS.RESULTS_PUBLISH)
);

const canCompileReportCards = computed(() =>
  authStore.hasPermission(RECORDS_PERMISSIONS.REPORT_CARDS_COMPILE) ||
  authStore.hasRole('AcademicCoordinator') ||
  isAuthority.value
);

const canApproveReportCards = computed(() =>
  isAuthority.value || authStore.hasPermission(RECORDS_PERMISSIONS.REPORT_CARDS_APPROVE)
);

const canPublishReportCards = computed(() =>
  isAuthority.value || authStore.hasPermission(RECORDS_PERMISSIONS.REPORT_CARDS_PUBLISH)
);

const canGenerateTranscripts = computed(() =>
  authStore.hasPermission(RECORDS_PERMISSIONS.TRANSCRIPTS_GENERATE) ||
  isAuthority.value
);

const canIssueTranscripts = computed(() =>
  isAuthority.value || authStore.hasPermission(RECORDS_PERMISSIONS.TRANSCRIPTS_ISSUE)
);

// ---------------------------------------------------------------------------
// 3. Tab State
// ---------------------------------------------------------------------------
const activeTab = ref<'moderation' | 'reportCards' | 'transcripts'>('moderation');

// ---------------------------------------------------------------------------
// 4. Tab 1: Summative Moderation Queue State & Queries
// ---------------------------------------------------------------------------
const { learningUnits } = useLearningUnits();

const selectedUnitId = ref<string>('');
const selectedAssessmentId = ref<string>('');
const moderationStatusFilter = ref<string>('provisional');
const selectedResultIds = ref<string[]>([]);

// Set initial unit when available
watch(
  learningUnits,
  (units) => {
    if (units.length > 0 && !selectedUnitId.value) {
      selectedUnitId.value = units[0].id;
    }
  },
  { immediate: true },
);

// Fetch assessments for selected unit
const {
  data: unitAssessments,
} = useAssessmentsQuery(selectedUnitId, { status: undefined }, {
  enabled: computed(() => Boolean(selectedUnitId.value)),
});

// Set initial assessment when unitAssessments change
watch(
  unitAssessments,
  (assessments) => {
    if (assessments && assessments.length > 0) {
      selectedAssessmentId.value = assessments[0].id;
    } else {
      selectedAssessmentId.value = '';
    }
  },
  { immediate: true },
);

// Results query for selected assessment
const {
  data: assessmentResults,
  isLoading: isLoadingResults,
  refetch: refetchResults,
} = useAcademicResultsQuery(
  selectedAssessmentId,
  computed(() => ({
    status: moderationStatusFilter.value !== 'all' ? moderationStatusFilter.value : undefined,
    onlyFinal: true,
  })),
  { enabled: computed(() => Boolean(selectedAssessmentId.value)) },
);

// Moderation mutations
const approveSummativeMutation = useApproveSummativeMutation();
const publishResultsMutation = usePublishResultsMutation();

// Selection logic
const allResults = computed(() => assessmentResults.value ?? []);
const areAllSelected = computed(
  () =>
    allResults.value.length > 0 &&
    selectedResultIds.value.length === allResults.value.length,
);

function toggleSelectAll(checked: boolean) {
  if (checked) {
    selectedResultIds.value = allResults.value.map((r) => r.id);
  } else {
    selectedResultIds.value = [];
  }
}

function toggleResultSelection(id: string) {
  const index = selectedResultIds.value.indexOf(id);
  if (index >= 0) {
    selectedResultIds.value.splice(index, 1);
  } else {
    selectedResultIds.value.push(id);
  }
}

async function handleBatchApproveResults() {
  if (!selectedAssessmentId.value) return;
  const ids = selectedResultIds.value.length > 0 ? selectedResultIds.value : undefined;
  await approveSummativeMutation.mutateAsync({
    assessmentId: selectedAssessmentId.value,
    request: ids ? { resultIds: ids } : undefined,
  });
  selectedResultIds.value = [];
  refetchResults();
}

async function handleBatchPublishResults() {
  if (!selectedAssessmentId.value) return;
  const ids = selectedResultIds.value.length > 0 ? selectedResultIds.value : undefined;
  await publishResultsMutation.mutateAsync({
    assessmentId: selectedAssessmentId.value,
    request: ids ? { resultIds: ids } : undefined,
  });
  selectedResultIds.value = [];
  refetchResults();
}

// Correction modal state
const isCorrectionModalOpen = ref(false);
const resultToCorrect = ref<AcademicResult | null>(null);

function openCorrectionModal(result: AcademicResult) {
  resultToCorrect.value = result;
  isCorrectionModalOpen.value = true;
}

function onResultCorrected() {
  refetchResults();
}

// ---------------------------------------------------------------------------
// 5. Tab 2: Report Cards Desk State & Queries
// ---------------------------------------------------------------------------
const selectedPeriodId = ref<string>('2026-ganjil');
const selectedSectionFilter = ref<string>('all');
const isBatchCompileModalOpen = ref(false);
const isPrintPreviewModalOpen = ref(false);
const activeReportCardForPrint = ref<ReportCard | null>(null);

const {
  data: reportCardsList,
  isLoading: isLoadingReportCards,
  refetch: refetchReportCards,
} = useReportCardsQuery(
  computed(() => ({ academicPeriodId: selectedPeriodId.value })),
  undefined,
  { enabled: computed(() => Boolean(selectedPeriodId.value)) },
);

const filteredReportCards = computed(() => {
  const list = reportCardsList.value ?? [];
  if (selectedSectionFilter.value === 'all') {
    return list;
  }
  return list.filter((rc) => rc.periodLabel?.includes(selectedSectionFilter.value));
});

const approveReportCardMutation = useApproveReportCardMutation();
const publishReportCardMutation = usePublishReportCardMutation();

async function handleApproveReportCard(card: ReportCard) {
  await approveReportCardMutation.mutateAsync({ id: card.id });
  refetchReportCards();
}

async function handlePublishReportCard(card: ReportCard) {
  await publishReportCardMutation.mutateAsync(card.id);
  refetchReportCards();
}

function openPrintPreview(card: ReportCard) {
  activeReportCardForPrint.value = card;
  isPrintPreviewModalOpen.value = true;
}

function onBatchCompilationFinished() {
  refetchReportCards();
}

// ---------------------------------------------------------------------------
// 6. Tab 3: Transcripts Desk State & Queries
// ---------------------------------------------------------------------------
const transcriptStudentSearch = ref<string>('STU-2024-001');
const activeStudentIdForTranscript = ref<string>('STU-2024-001');
const isGenerateTranscriptModalOpen = ref(false);

const {
  data: studentTranscript,
  isLoading: isLoadingTranscript,
  refetch: refetchTranscript,
} = useStudentTranscriptQuery(activeStudentIdForTranscript, undefined, {
  enabled: computed(() => Boolean(activeStudentIdForTranscript.value)),
});

const issueTranscriptMutation = useIssueTranscriptMutation();

function handleSearchTranscript() {
  if (transcriptStudentSearch.value.trim()) {
    activeStudentIdForTranscript.value = transcriptStudentSearch.value.trim();
  }
}

async function handleIssueTranscript(transcriptId: string) {
  await issueTranscriptMutation.mutateAsync(transcriptId);
  refetchTranscript();
}

function handlePrintTranscript() {
  if (typeof window !== 'undefined') {
    // eslint-disable-next-line no-undef
    window.print();
  }
}
</script>

<template>
  <div class="space-y-6" data-testid="academic-records-workbench-view">
    <!-- Page Header -->
    <PageHeader
      title="Academic Records Workbench"
      description="Moderate summative scores across cohorts, compile and certify Buku Rapor Merdeka, print official report cards, and issue interim academic transcripts."
      :breadcrumbs="breadcrumbs"
    />

    <!-- Authority Mode Status Banner -->
    <div
      v-if="isAuthority"
      class="p-3 bg-teal-50 dark:bg-teal-950/40 border border-teal-200 dark:border-teal-800 rounded-xl flex items-center justify-between text-xs text-teal-800 dark:text-teal-200 shadow-xs"
      data-testid="authority-mode-banner"
    >
      <div class="flex items-center gap-2.5">
        <ShieldCheck class="w-4 h-4 text-teal-600 dark:text-teal-400 shrink-0" />
        <div>
          <span class="font-bold">Academic Authority Session Active:</span>
          Full certification, summative approval, frozen snapshot publishing, and material grade alteration privileges enabled.
        </div>
      </div>
      <span class="font-mono text-[11px] font-semibold uppercase bg-teal-100 dark:bg-teal-900 px-2 py-0.5 rounded">
        Authorized
      </span>
    </div>

    <!-- Main Tabs Navigation (SCR-ACD-07) -->
    <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden">
      <Tabs v-model:value="activeTab" class="w-full" data-testid="records-tabs">
        <TabList class="border-b border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/50 px-4">
          <Tab value="moderation" data-testid="tab-moderation">
            <div class="flex items-center gap-2 py-1">
              <UserCheck class="w-4 h-4 text-teal-600" />
              <span class="font-semibold text-xs">Summative Moderation Queue</span>
            </div>
          </Tab>
          <Tab value="reportCards" data-testid="tab-report-cards">
            <div class="flex items-center gap-2 py-1">
              <FileCheck class="w-4 h-4 text-teal-600" />
              <span class="font-semibold text-xs">Report Cards Desk (Buku Rapor)</span>
            </div>
          </Tab>
          <Tab value="transcripts" data-testid="tab-transcripts">
            <div class="flex items-center gap-2 py-1">
              <GraduationCap class="w-4 h-4 text-teal-600" />
              <span class="font-semibold text-xs">Cumulative Transcripts Desk</span>
            </div>
          </Tab>
        </TabList>

        <TabPanels class="p-6">
          <!-- =============================================================== -->
          <!-- TAB 1: SUMMATIVE MODERATION QUEUE (UC-ACD-16, UC-ACD-19)        -->
          <!-- =============================================================== -->
          <TabPanel value="moderation" data-testid="panel-moderation">
            <div class="space-y-4">
              <!-- Filter & Context Toolbar -->
              <div class="flex flex-wrap items-center justify-between gap-3 bg-surface-50 dark:bg-surface-800/40 p-4 rounded-xl border border-surface-200 dark:border-surface-700">
                <div class="flex flex-wrap items-center gap-3">
                  <!-- Learning Unit Dropdown -->
                  <div>
                    <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
                      Learning Unit
                    </label>
                    <Select
                      v-model="selectedUnitId"
                      :options="learningUnits"
                      option-label="title"
                      option-value="id"
                      placeholder="Select Learning Unit"
                      class="w-56 text-xs"
                      data-testid="select-moderation-unit"
                    />
                  </div>

                  <!-- Assessment Dropdown -->
                  <div>
                    <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
                      Assessment Event
                    </label>
                    <Select
                      v-model="selectedAssessmentId"
                      :options="unitAssessments || []"
                      option-label="title"
                      option-value="id"
                      placeholder="Select Assessment"
                      class="w-64 text-xs"
                      data-testid="select-moderation-assessment"
                    />
                  </div>

                  <!-- Status Filter -->
                  <div>
                    <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
                      Result Status
                    </label>
                    <Select
                      v-model="moderationStatusFilter"
                      :options="[
                        { label: 'Provisional (Pending Approval)', value: 'provisional' },
                        { label: 'Approved (Certified)', value: 'approved' },
                        { label: 'Published (Public)', value: 'published' },
                        { label: 'All Lifecycle Statuses', value: 'all' },
                      ]"
                      option-label="label"
                      option-value="value"
                      class="w-52 text-xs"
                      data-testid="select-moderation-status"
                    />
                  </div>
                </div>

                <!-- Batch Actions (Authority Guarded) -->
                <div class="flex items-center gap-2 self-end">
                  <Button
                    size="small"
                    severity="secondary"
                    variant="outlined"
                    class="cursor-pointer"
                    data-testid="btn-refetch-moderation"
                    @click="refetchResults"
                  >
                    <RefreshCw class="w-3.5 h-3.5 mr-1" />
                    <span>Refresh</span>
                  </Button>

                  <Button
                    size="small"
                    severity="primary"
                    :disabled="!canApproveResults || allResults.length === 0"
                    :loading="approveSummativeMutation.isPending.value"
                    class="cursor-pointer"
                    data-testid="btn-batch-approve"
                    @click="handleBatchApproveResults"
                  >
                    <ShieldCheck class="w-3.5 h-3.5 mr-1" />
                    <span>
                      {{ selectedResultIds.length > 0 ? `Approve Selected (${selectedResultIds.length})` : 'Approve All Provisional' }}
                    </span>
                  </Button>

                  <Button
                    size="small"
                    severity="success"
                    :disabled="!canPublishResults || allResults.length === 0"
                    :loading="publishResultsMutation.isPending.value"
                    class="cursor-pointer"
                    data-testid="btn-batch-publish"
                    @click="handleBatchPublishResults"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5 mr-1" />
                    <span>Release / Publish</span>
                  </Button>
                </div>
              </div>

              <!-- Moderation Table -->
              <div v-if="isLoadingResults" class="py-12 flex justify-center items-center">
                <LoadingSpinner label="Loading assessment results for moderation..." />
              </div>

              <div v-else-if="allResults.length === 0" class="py-12">
                <EmptyState
                  title="No provisional results in moderation queue"
                  description="All summative marks for this evaluation event have been reviewed, or no submissions have been graded yet."
                />
              </div>

              <div v-else class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs">
                <table class="w-full text-xs text-left border-collapse" data-testid="moderation-results-table">
                  <thead class="bg-surface-50 dark:bg-surface-800 text-surface-600 dark:text-surface-300 border-b border-surface-200 dark:border-surface-700">
                    <tr>
                      <th class="p-3 w-10 text-center">
                        <Checkbox
                          :model-value="areAllSelected"
                          :binary="true"
                          data-testid="checkbox-select-all"
                          @update:model-value="toggleSelectAll"
                        />
                      </th>
                      <th class="p-3">Candidate / Student</th>
                      <th class="p-3">Attempt Lineage</th>
                      <th class="p-3 text-center">Evaluation Model</th>
                      <th class="p-3 text-center">Raw Score</th>
                      <th class="p-3 text-center">Letter Grade</th>
                      <th class="p-3">Status</th>
                      <th class="p-3 text-right">Auditable Actions</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-surface-100 dark:divide-surface-800">
                    <tr
                      v-for="res in allResults"
                      :key="res.id"
                      class="hover:bg-surface-50/70 dark:hover:bg-surface-800/40 transition-colors"
                      :data-testid="`row-result-${res.id}`"
                    >
                      <td class="p-3 text-center">
                        <Checkbox
                          :model-value="selectedResultIds.includes(res.id)"
                          :binary="true"
                          :data-testid="`checkbox-result-${res.id}`"
                          @update:model-value="toggleResultSelection(res.id)"
                        />
                      </td>
                      <td class="p-3">
                        <span class="font-bold text-surface-900 dark:text-surface-100 block">
                          {{ res.candidateName || res.studentName || 'Student ' + res.studentId }}
                        </span>
                        <span class="text-[11px] text-surface-400 font-mono">
                          {{ res.registrationNumber || res.studentNumber || 'Enrolled Student' }}
                        </span>
                      </td>
                      <td class="p-3">
                        <div class="flex items-center gap-1.5">
                          <span class="font-mono text-[11px] px-1.5 py-0.5 rounded bg-surface-100 dark:bg-surface-800 font-semibold">
                            Attempt {{ res.attemptNumber }}
                          </span>
                          <span
                            v-if="res.isFinal"
                            class="text-[10px] font-bold text-emerald-700 dark:text-emerald-300 bg-emerald-50 dark:bg-emerald-950 px-1 py-0.5 rounded border border-emerald-200 dark:border-emerald-800"
                          >
                            Operative
                          </span>
                        </div>
                      </td>
                      <td class="p-3 text-center capitalize font-medium text-surface-600 dark:text-surface-400">
                        {{ res.evaluationModel }}
                      </td>
                      <td class="p-3 text-center font-bold text-surface-900 dark:text-surface-100">
                        {{ res.scoreNumeric !== null && res.scoreNumeric !== undefined ? res.scoreNumeric : '—' }}
                        <span v-if="res.scoreMax" class="text-surface-400 font-normal">/ {{ res.scoreMax }}</span>
                      </td>
                      <td class="p-3 text-center">
                        <span class="font-bold text-surface-900 dark:text-surface-100">
                          {{ res.gradeLetter || '—' }}
                        </span>
                      </td>
                      <td class="p-3">
                        <StatusPill :status="res.resultStatus" size="sm" />
                      </td>
                      <td class="p-3 text-right">
                        <Button
                          size="small"
                          severity="secondary"
                          variant="outlined"
                          class="!text-xs cursor-pointer"
                          data-testid="btn-correct-grade"
                          @click="openCorrectionModal(res)"
                        >
                          <SlidersHorizontal class="w-3.5 h-3.5 mr-1 text-teal-600" />
                          <span>Auditable Correction</span>
                        </Button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </TabPanel>

          <!-- =============================================================== -->
          <!-- TAB 2: REPORT CARDS DESK (UC-ACD-13, P5-S11, P5-S12)            -->
          <!-- =============================================================== -->
          <TabPanel value="reportCards" data-testid="panel-report-cards">
            <div class="space-y-4">
              <!-- Desk Context Bar -->
              <div class="flex flex-wrap items-center justify-between gap-3 bg-surface-50 dark:bg-surface-800/40 p-4 rounded-xl border border-surface-200 dark:border-surface-700">
                <div class="flex flex-wrap items-center gap-3">
                  <div>
                    <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
                      Academic Period
                    </label>
                    <Select
                      v-model="selectedPeriodId"
                      :options="[
                        { label: 'Semester Ganjil 2026/2027 (2026-ganjil)', value: '2026-ganjil' },
                        { label: 'Semester Genap 2025/2026 (2025-genap)', value: '2025-genap' },
                      ]"
                      option-label="label"
                      option-value="value"
                      class="w-64 text-xs"
                      data-testid="select-report-card-period"
                    />
                  </div>

                  <div>
                    <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
                      Section / Cohort Filter
                    </label>
                    <Select
                      v-model="selectedSectionFilter"
                      :options="[
                        { label: 'All Cohort Sections', value: 'all' },
                        { label: 'Kelas 10-A', value: '10-A' },
                        { label: 'Kelas 10-B', value: '10-B' },
                      ]"
                      option-label="label"
                      option-value="value"
                      class="w-48 text-xs"
                      data-testid="select-report-card-section"
                    />
                  </div>
                </div>

                <!-- Action Button for Batch Compilation -->
                <div class="flex items-center gap-2 self-end">
                  <Button
                    size="small"
                    severity="secondary"
                    variant="outlined"
                    class="cursor-pointer"
                    data-testid="btn-refetch-report-cards"
                    @click="refetchReportCards"
                  >
                    <RefreshCw class="w-3.5 h-3.5 mr-1" />
                    <span>Refresh</span>
                  </Button>

                  <Button
                    size="small"
                    severity="primary"
                    :disabled="!canCompileReportCards"
                    class="cursor-pointer flex items-center gap-1.5"
                    data-testid="btn-open-batch-compile"
                    @click="isBatchCompileModalOpen = true"
                  >
                    <Layers class="w-4 h-4" />
                    <span>Batch Compile Cohort</span>
                  </Button>
                </div>
              </div>

              <!-- Report Cards Table -->
              <div v-if="isLoadingReportCards" class="py-12 flex justify-center items-center">
                <LoadingSpinner label="Loading compiled report cards..." />
              </div>

              <div v-else-if="filteredReportCards.length === 0" class="py-12">
                <EmptyState
                  title="No report cards compiled for this period"
                  description="Click 'Batch Compile Cohort' above to generate report cards for enrolled students from approved operative results."
                />
              </div>

              <div v-else class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs">
                <table class="w-full text-xs text-left border-collapse" data-testid="report-cards-table">
                  <thead class="bg-surface-50 dark:bg-surface-800 text-surface-600 dark:text-surface-300 border-b border-surface-200 dark:border-surface-700">
                    <tr>
                      <th class="p-3">Student / NISN</th>
                      <th class="p-3">Period & Report Type</th>
                      <th class="p-3">Template</th>
                      <th class="p-3 text-center">Status</th>
                      <th class="p-3">Approved By</th>
                      <th class="p-3 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-surface-100 dark:divide-surface-800">
                    <tr
                      v-for="card in filteredReportCards"
                      :key="card.id"
                      class="hover:bg-surface-50/70 dark:hover:bg-surface-800/40 transition-colors"
                      :data-testid="`row-card-${card.id}`"
                    >
                      <td class="p-3">
                        <span class="font-bold text-surface-900 dark:text-surface-100 block">
                          {{ card.studentName || 'Student ID: ' + card.studentId }}
                        </span>
                        <span class="font-mono text-[11px] text-surface-400">
                          NISN: {{ card.studentNisn || '—' }}
                        </span>
                      </td>
                      <td class="p-3">
                        <span class="font-medium text-surface-800 dark:text-surface-200 block">
                          {{ card.periodLabel || card.academicPeriodLabel || card.academicPeriodId }}
                        </span>
                        <span class="text-[11px] text-surface-500 capitalize">
                          {{ card.reportType === 'end_of_term' ? 'End of Semester (Buku Rapor)' : 'Mid-Term Progress' }}
                        </span>
                      </td>
                      <td class="p-3 text-surface-600 dark:text-surface-400">
                        {{ card.templateName }}
                      </td>
                      <td class="p-3 text-center">
                        <StatusPill :status="card.status" size="sm" />
                      </td>
                      <td class="p-3 text-surface-500">
                        <span v-if="card.approvedBy" class="flex items-center gap-1 font-medium text-emerald-600 dark:text-emerald-400">
                          <ShieldCheck class="w-3.5 h-3.5" />
                          {{ card.approvedBy }}
                        </span>
                        <span v-else class="text-surface-400 italic">Pending Certification</span>
                      </td>
                      <td class="p-3 text-right">
                        <div class="flex items-center justify-end gap-1.5">
                          <!-- Preview & Print Button -->
                          <Button
                            size="small"
                            severity="secondary"
                            variant="outlined"
                            class="!text-xs cursor-pointer"
                            data-testid="btn-preview-report-card"
                            @click="openPrintPreview(card)"
                          >
                            <Printer class="w-3.5 h-3.5 mr-1 text-teal-600" />
                            <span>Preview & Print</span>
                          </Button>

                          <!-- Authority Certify Button -->
                          <Button
                            v-if="card.status === 'draft'"
                            size="small"
                            severity="primary"
                            :disabled="!canApproveReportCards"
                            class="!text-xs cursor-pointer"
                            data-testid="btn-certify-report-card"
                            @click="handleApproveReportCard(card)"
                          >
                            <ShieldCheck class="w-3.5 h-3.5 mr-1" />
                            <span>Approve & Freeze</span>
                          </Button>

                          <!-- Authority Publish Button -->
                          <Button
                            v-else-if="card.status === 'approved'"
                            size="small"
                            severity="success"
                            :disabled="!canPublishReportCards"
                            class="!text-xs cursor-pointer"
                            data-testid="btn-publish-report-card"
                            @click="handlePublishReportCard(card)"
                          >
                            <CheckCircle2 class="w-3.5 h-3.5 mr-1" />
                            <span>Publish</span>
                          </Button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </TabPanel>

          <!-- =============================================================== -->
          <!-- TAB 3: TRANSCRIPTS DESK (UC-ACD-13, GAP-08)                     -->
          <!-- =============================================================== -->
          <TabPanel value="transcripts" data-testid="panel-transcripts">
            <div class="space-y-4">
              <!-- Search & Generate Toolbar -->
              <div class="flex flex-wrap items-center justify-between gap-3 bg-surface-50 dark:bg-surface-800/40 p-4 rounded-xl border border-surface-200 dark:border-surface-700">
                <div class="flex items-center gap-2">
                  <div class="w-72">
                    <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
                      Student Search
                    </label>
                    <div class="flex gap-1.5">
                      <InputText
                        v-model="transcriptStudentSearch"
                        placeholder="Enter Student ID or NISN..."
                        class="w-full text-xs"
                        data-testid="input-search-student-transcript"
                        @keydown.enter="handleSearchTranscript"
                      />
                      <Button
                        size="small"
                        severity="secondary"
                        class="cursor-pointer"
                        data-testid="btn-search-transcript"
                        @click="handleSearchTranscript"
                      >
                        <Search class="w-3.5 h-3.5" />
                      </Button>
                    </div>
                  </div>
                </div>

                <div class="flex items-center gap-2 self-end">
                  <Button
                    size="small"
                    severity="primary"
                    :disabled="!canGenerateTranscripts"
                    class="cursor-pointer flex items-center gap-1.5"
                    data-testid="btn-open-generate-transcript"
                    @click="isGenerateTranscriptModalOpen = true"
                  >
                    <GraduationCap class="w-4 h-4" />
                    <span>Generate Interim Transcript</span>
                  </Button>
                </div>
              </div>

              <!-- Student Transcript Record Card -->
              <div v-if="isLoadingTranscript" class="py-12 flex justify-center items-center">
                <LoadingSpinner label="Loading cumulative transcript..." />
              </div>

              <div v-else-if="!studentTranscript" class="py-12">
                <EmptyState
                  title="No transcript found for student"
                  description="Use 'Generate Interim Transcript' to compile cumulative courses and transfer credits."
                />
              </div>

              <div v-else class="space-y-4" data-testid="student-transcript-card">
                <!-- Transcript Header Summary -->
                <div class="p-5 bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs flex flex-wrap items-center justify-between gap-4">
                  <div>
                    <div class="flex items-center gap-2 mb-1">
                      <span class="font-bold text-base text-surface-900 dark:text-surface-100">
                        {{ studentTranscript.studentName || 'Student ID: ' + studentTranscript.studentId }}
                      </span>
                      <StatusPill :status="studentTranscript.status" size="sm" />
                      <span class="uppercase font-mono text-[10px] px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-600">
                        {{ studentTranscript.transcriptType }}
                      </span>
                    </div>
                    <div class="flex items-center gap-4 text-xs text-surface-500">
                      <span>NISN: <strong>{{ studentTranscript.studentNisn || '—' }}</strong></span>
                      <span>Range: <strong>{{ formatDisplayDate(studentTranscript.periodStart) }}</strong> to <strong>{{ formatDisplayDate(studentTranscript.periodEnd) }}</strong></span>
                      <span v-if="studentTranscript.approvedBy" class="text-emerald-600 font-medium">
                        Issued by: {{ studentTranscript.approvedBy }} ({{ formatDisplayDate(studentTranscript.issuedDate || '') }})
                      </span>
                    </div>
                  </div>

                  <div class="flex items-center gap-2">
                    <Button
                      v-if="studentTranscript.status === 'draft'"
                      size="small"
                      severity="primary"
                      :disabled="!canIssueTranscripts"
                      :loading="issueTranscriptMutation.isPending.value"
                      class="cursor-pointer"
                      data-testid="btn-issue-transcript"
                      @click="handleIssueTranscript(studentTranscript.id)"
                    >
                      <ShieldCheck class="w-3.5 h-3.5 mr-1" />
                      <span>Issue Official Transcript</span>
                    </Button>
                    <Button
                      size="small"
                      severity="secondary"
                      variant="outlined"
                      class="cursor-pointer"
                      data-testid="btn-print-transcript"
                      @click="handlePrintTranscript"
                    >
                      <Printer class="w-3.5 h-3.5 mr-1 text-teal-600" />
                      <span>Print Transcript</span>
                    </Button>
                  </div>
                </div>

                <!-- Completed Periods Roster -->
                <div class="space-y-4">
                  <div
                    v-for="period in studentTranscript.completedPeriods || []"
                    :key="period.academicPeriodId"
                    class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 shadow-xs space-y-3"
                  >
                    <div class="flex items-center justify-between pb-2 border-b border-surface-100 dark:border-surface-800">
                      <span class="font-bold text-xs text-surface-900 dark:text-surface-100">
                        {{ period.periodLabel }}
                      </span>
                      <span class="font-semibold text-xs text-teal-700 dark:text-teal-400 font-mono">
                        Period GPA: {{ period.gpa?.toFixed(2) ?? '—' }}
                      </span>
                    </div>

                    <table class="w-full text-xs text-left border-collapse">
                      <thead class="text-surface-500 font-medium border-b border-surface-100 dark:border-surface-800">
                        <tr>
                          <th class="py-1.5">Subject</th>
                          <th class="py-1.5 text-center">Credits (SKS)</th>
                          <th class="py-1.5 text-center">Numeric Score</th>
                          <th class="py-1.5 text-center">Letter Grade</th>
                          <th class="py-1.5 text-center">Transfer Indicator</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-surface-100 dark:divide-surface-800">
                        <tr v-for="c in period.courses" :key="c.learningUnitId" class="hover:bg-surface-50/50">
                          <td class="py-2">
                            <span class="font-semibold text-surface-800 dark:text-surface-200 block">{{ c.subjectTitle }}</span>
                            <span class="font-mono text-[10px] text-surface-400">{{ c.subjectCode }}</span>
                          </td>
                          <td class="py-2 text-center font-mono">{{ c.credits }}</td>
                          <td class="py-2 text-center font-bold">{{ c.scoreNumeric ?? '—' }}</td>
                          <td class="py-2 text-center font-bold">{{ c.gradeLetter ?? '—' }}</td>
                          <td class="py-2 text-center">
                            <!-- Transfer credit indicator [T] (GAP-08) -->
                            <span
                              v-if="c.isTransferred"
                              class="font-mono text-teal-800 bg-teal-100 dark:bg-teal-950 dark:text-teal-300 font-bold px-1.5 py-0.5 rounded text-[11px]"
                              data-testid="transfer-credit-tag"
                              title="Recognized Prior Learning / Transfer Credit"
                            >
                              [T]
                            </span>
                            <span v-else class="text-surface-400">—</span>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- Modals & Drawers -->

    <!-- 1. Auditable Grade Correction Modal -->
    <RecordCorrectionModal
      v-model:is-open="isCorrectionModalOpen"
      :result="resultToCorrect"
      @corrected="onResultCorrected"
    />

    <!-- 2. Batch Compile Report Cards Modal -->
    <BatchCompileReportCardsModal
      v-model:is-open="isBatchCompileModalOpen"
      :default-period-id="selectedPeriodId"
      @compiled="onBatchCompilationFinished"
    />

    <!-- 3. Generate Interim Transcript Modal -->
    <GenerateInterimTranscriptModal
      v-model:is-open="isGenerateTranscriptModalOpen"
      :initial-student-id="transcriptStudentSearch"
      @generated="refetchTranscript"
    />

    <!-- 4. Buku Rapor Merdeka Print Preview Modal -->
    <Dialog
      v-model:visible="isPrintPreviewModalOpen"
      modal
      header="Buku Rapor Merdeka — Statutory Layout"
      :style="{ width: '920px' }"
      :breakpoints="{ '1024px': '90vw', '640px': '98vw' }"
      data-testid="print-preview-dialog"
    >
      <ReportCardPrintPreview
        :report-card="activeReportCardForPrint"
        :is-open="isPrintPreviewModalOpen"
        @close="isPrintPreviewModalOpen = false"
      />
    </Dialog>
  </div>
</template>
