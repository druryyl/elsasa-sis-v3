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
import Dialog from 'primevue/dialog';
import {
  AlertTriangle,
  Award,
  BookOpen,
  Calendar,
  CheckCircle2,
  FileCheck2,
  FileEdit,
  FileText,
  Filter,
  GraduationCap,
  Layers,
  Plus,
  RefreshCw,
  RotateCcw,
  Scale,
  Search,
  ShieldAlert,
  Sparkles,
  Trophy,
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
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import {
  useEvaluateStandingQuery,
  useEvaluateProgressionQuery,
  useCohortStandingsQuery,
  useCohortAchievementsQuery,
  useCohortRecommendationsQuery,
  useBatchCommitCohortMutation,
} from '../composables/useCohortReview';
import {
  COHORT_REVIEW_PERMISSIONS,
  COHORT_REVIEW_ROLES,
} from '../constants/permissions';
import type {
  AcademicAchievement,
  AcademicAchievementType,
  AcademicStandingStatus,
  BatchDetermineStandingRequest,
  BatchIssueProgressionRequest,
  CohortProgressionProposal,
  CohortStandingProposal,
  ProgressionItem,
  PromotionRecommendationStatus,
  StandingItem,
} from '../types';
import StandingOverrideModal, {
  type OverrideAppliedPayload,
  type StudentReviewItem,
} from '../components/StandingOverrideModal.vue';
import ConferAchievementModal from '../components/ConferAchievementModal.vue';
import StandingCorrectionModal from '../components/StandingCorrectionModal.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Cohort Academic Review', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Role Guards (Academic Authority Exclusive, UC-ACD-17)
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const appContextStore = useAppContextStore();

const isAcademicAuthority = computed(() =>
  authStore.hasRole('AcademicAuthority') ||
  authStore.hasRole('SuperAdmin') ||
  authStore.hasRole('Admin'),
);

const canApproveCohort = computed(() =>
  isAcademicAuthority.value ||
  authStore.hasPermission(COHORT_REVIEW_PERMISSIONS.STANDINGS_BATCH_DETERMINE) ||
  authStore.hasPermission(COHORT_REVIEW_PERMISSIONS.PROGRESSION_BATCH_ISSUE),
);

const canConferAchievement = computed(() =>
  isAcademicAuthority.value ||
  authStore.hasPermission(COHORT_REVIEW_PERMISSIONS.ACHIEVEMENTS_CONFER),
);

const canCorrectRecord = computed(() =>
  isAcademicAuthority.value ||
  authStore.hasPermission(COHORT_REVIEW_PERMISSIONS.CORRECTIONS_STANDING) ||
  authStore.hasPermission(COHORT_REVIEW_PERMISSIONS.CORRECTIONS_PROGRESSION),
);

// ---------------------------------------------------------------------------
// 3. Selection & Filter Bar State
// ---------------------------------------------------------------------------
const defaultPeriodId = computed(
  () => appContextStore.activeAcademicPeriodId || '00000000-0000-0000-0000-000000000001',
);

const selectedPeriodId = ref<string>(defaultPeriodId.value);
const selectedGradeLevel = ref<string>('Grade 4');
const activeTab = ref<'standingProgression' | 'achievements'>('standingProgression');
const searchFilter = ref('');

watch(
  () => appContextStore.activeAcademicPeriodId,
  (newPeriod) => {
    if (newPeriod) {
      selectedPeriodId.value = newPeriod;
    }
  },
);

const gradeLevelOptions = [
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
// 4. Data Queries (TanStack Query)
// ---------------------------------------------------------------------------
// Dynamic Evaluation Queries
const {
  data: evaluateStandingData,
  isLoading: isLoadingStandingEval,
  isFetching: isFetchingStandingEval,
  refetch: refetchStandingEval,
} = useEvaluateStandingQuery(
  computed(() => selectedPeriodId.value),
  computed(() => selectedGradeLevel.value),
  { enabled: computed(() => Boolean(selectedPeriodId.value && selectedGradeLevel.value)) },
);

const {
  data: evaluateProgressionData,
  isLoading: isLoadingProgressionEval,
  isFetching: isFetchingProgressionEval,
  refetch: refetchProgressionEval,
} = useEvaluateProgressionQuery(
  computed(() => selectedPeriodId.value),
  computed(() => selectedGradeLevel.value),
  { enabled: computed(() => Boolean(selectedPeriodId.value && selectedGradeLevel.value)) },
);

// Committed Records Queries
const {
  data: committedStandingsData,
  refetch: refetchCommittedStandings,
} = useCohortStandingsQuery(
  computed(() => ({
    academicPeriodId: selectedPeriodId.value || undefined,
    gradeLevel: selectedGradeLevel.value || undefined,
  })),
  { enabled: computed(() => Boolean(selectedPeriodId.value)) },
);

const {
  data: committedRecommendationsData,
  refetch: refetchCommittedRecommendations,
} = useCohortRecommendationsQuery(
  computed(() => ({
    academicPeriodId: selectedPeriodId.value || undefined,
    gradeLevel: selectedGradeLevel.value || undefined,
  })),
  { enabled: computed(() => Boolean(selectedPeriodId.value)) },
);

// Achievements Query (Tab 2)
const {
  data: achievementsData,
  isLoading: isLoadingAchievements,
  isFetching: isFetchingAchievements,
  refetch: refetchAchievements,
} = useCohortAchievementsQuery(
  computed(() => selectedPeriodId.value),
  computed(() => selectedGradeLevel.value),
  { enabled: computed(() => Boolean(selectedPeriodId.value)) },
);

// ---------------------------------------------------------------------------
// 5. In-Memory Local Overrides & Merged Review Roster
// ---------------------------------------------------------------------------
const overridesMap = ref<Record<string, OverrideAppliedPayload>>({});

function handleApplyOverride(payload: OverrideAppliedPayload) {
  overridesMap.value = {
    ...overridesMap.value,
    [payload.studentId]: payload,
  };
}

function handleResetOverrides() {
  overridesMap.value = {};
}

// Merged Table Rows
const combinedReviewRows = computed<StudentReviewItem[]>(() => {
  const standings = evaluateStandingData.value ?? [];
  const progressionList = evaluateProgressionData.value ?? [];
  const committedStandings = committedStandingsData.value ?? [];
  const committedRecs = committedRecommendationsData.value ?? [];

  const progMap = new Map<string, CohortProgressionProposal>();
  for (const p of progressionList) {
    progMap.set(p.studentId, p);
  }

  const existingStandingsMap = new Map<string, StandingItem>();
  for (const s of committedStandings) {
    existingStandingsMap.set(s.studentId, s);
  }

  const existingRecsMap = new Map<string, ProgressionItem>();
  for (const r of committedRecs) {
    existingRecsMap.set(r.studentId, r);
  }

  return standings.map((stdProposal) => {
    const progProposal = progMap.get(stdProposal.studentId);
    const existingStanding = existingStandingsMap.get(stdProposal.studentId);
    const existingRec = existingRecsMap.get(stdProposal.studentId);
    const override = overridesMap.value[stdProposal.studentId];

    const currentStanding =
      override?.standing ??
      existingStanding?.standing ??
      stdProposal.proposedStanding;

    const currentPolicyRef =
      override?.policyReference ??
      existingStanding?.policyReference ??
      stdProposal.policyReference;

    const currentConditions =
      override?.conditions !== undefined
        ? override.conditions
        : existingStanding?.conditions ?? stdProposal.proposedConditions;

    const currentRec =
      override?.recommendation ??
      existingRec?.recommendation ??
      progProposal?.proposedRecommendation ??
      'promotion_eligible';

    const currentFromLevel =
      override?.fromLevel ??
      existingRec?.fromLevel ??
      progProposal?.fromLevel ??
      selectedGradeLevel.value;

    const currentToLevel =
      override?.recommendedToLevel !== undefined
        ? override.recommendedToLevel
        : existingRec?.recommendedToLevel ?? progProposal?.recommendedToLevel;

    const currentEvidence =
      override?.supportingEvidence ??
      existingRec?.supportingEvidence ??
      progProposal?.supportingEvidence ??
      `Evaluation: ${stdProposal.passedUnitsCount}/${stdProposal.totalUnitsCount} units passed, ${stdProposal.attendancePercentage}% attendance.`;

    const isExc =
      override?.isException ??
      existingStanding?.isException ??
      existingRec?.isException ??
      stdProposal.isException ??
      progProposal?.isException ??
      false;

    const excReason =
      override?.exceptionReason ??
      existingStanding?.exceptionReason ??
      existingRec?.exceptionReason ??
      stdProposal.exceptionReason ??
      progProposal?.exceptionReason;

    const commRef =
      override?.committeeReference ??
      existingStanding?.committeeReference ??
      existingRec?.committeeReference ??
      stdProposal.committeeReference ??
      progProposal?.committeeReference;

    return {
      studentId: stdProposal.studentId,
      studentName: stdProposal.studentName,
      studentIdentifier: stdProposal.studentIdentifier,
      gradeLevel: stdProposal.gradeLevel || selectedGradeLevel.value,
      totalUnitsCount: stdProposal.totalUnitsCount,
      passedUnitsCount: stdProposal.passedUnitsCount,
      failedUnitsCount: stdProposal.failedUnitsCount,
      attendancePercentage: stdProposal.attendancePercentage,

      standing: currentStanding,
      policyReference: currentPolicyRef,
      conditions: currentConditions,

      recommendation: currentRec,
      fromLevel: currentFromLevel,
      recommendedToLevel: currentToLevel,
      supportingEvidence: currentEvidence,

      isException: isExc,
      exceptionReason: excReason,
      committeeReference: commRef,

      isOverridden: Boolean(override),
      existingStandingId: existingStanding?.id,
      existingRecommendationId: existingRec?.id,
    };
  });
});

// Search filter
const filteredReviewRows = computed(() => {
  let list = combinedReviewRows.value;
  if (searchFilter.value.trim()) {
    const term = searchFilter.value.trim().toLowerCase();
    list = list.filter((r) =>
      r.studentName.toLowerCase().includes(term) ||
      (r.studentIdentifier && r.studentIdentifier.toLowerCase().includes(term)) ||
      r.standing.toLowerCase().includes(term) ||
      r.recommendation.toLowerCase().includes(term),
    );
  }
  return list;
});

// Sticky Footer Summary Counts
const totalCount = computed(() => combinedReviewRows.value.length);

const eligibleCount = computed(
  () => combinedReviewRows.value.filter((r) => r.recommendation === 'promotion_eligible').length,
);

const conditionalCount = computed(
  () => combinedReviewRows.value.filter((r) => r.recommendation === 'conditional_promotion').length,
);

const retentionCount = computed(
  () => combinedReviewRows.value.filter((r) => r.recommendation === 'retention_recommended').length,
);

const overridesCount = computed(
  () => combinedReviewRows.value.filter((r) => r.isOverridden || r.isException).length,
);

const committedCount = computed(
  () => combinedReviewRows.value.filter((r) => Boolean(r.existingStandingId)).length,
);

// ---------------------------------------------------------------------------
// 6. Batch Cohort Commitment
// ---------------------------------------------------------------------------
const isConfirmCommitOpen = ref(false);

const { mutate: mutateBatchCommit, isPending: isSubmittingBatch } = useBatchCommitCohortMutation({
  onSuccess: () => {
    isConfirmCommitOpen.value = false;
    overridesMap.value = {};
    handleRefreshAll();
  },
});

function handleCommitCohort() {
  const periodId = selectedPeriodId.value;
  if (!periodId || combinedReviewRows.value.length === 0) return;

  const standingItems: StandingItem[] = combinedReviewRows.value.map((r) => ({
    studentId: r.studentId,
    academicPeriodId: periodId,
    standing: r.standing,
    policyReference: r.policyReference,
    conditions: r.conditions,
    isException: r.isException,
    exceptionReason: r.exceptionReason,
    committeeReference: r.committeeReference,
  }));

  const progressionItems: ProgressionItem[] = combinedReviewRows.value.map((r) => ({
    studentId: r.studentId,
    academicPeriodId: periodId,
    recommendation: r.recommendation,
    fromLevel: r.fromLevel,
    recommendedToLevel: r.recommendedToLevel,
    conditions: r.conditions,
    supportingEvidence: r.supportingEvidence,
    isException: r.isException,
    exceptionReason: r.exceptionReason,
    committeeReference: r.committeeReference,
  }));

  const standingRequest: BatchDetermineStandingRequest = {
    academicPeriodId: periodId,
    items: standingItems,
  };

  const progressionRequest: BatchIssueProgressionRequest = {
    academicPeriodId: periodId,
    items: progressionItems,
  };

  mutateBatchCommit({ standingRequest, progressionRequest });
}

// ---------------------------------------------------------------------------
// 7. Modals State
// ---------------------------------------------------------------------------
// Modal 1: Standing & Progression Override
const isOverrideModalOpen = ref(false);
const selectedStudentForOverride = ref<StudentReviewItem | null>(null);

function handleOpenOverride(student: StudentReviewItem) {
  selectedStudentForOverride.value = student;
  isOverrideModalOpen.value = true;
}

// Modal 2: Confer Achievement
const isConferModalOpen = ref(false);
const conferInitialStudentId = ref<string | null>(null);
const conferInitialStudentName = ref<string | null>(null);

function handleOpenConferAchievement(student?: StudentReviewItem) {
  if (student) {
    conferInitialStudentId.value = student.studentId;
    conferInitialStudentName.value = student.studentName;
  } else {
    conferInitialStudentId.value = null;
    conferInitialStudentName.value = null;
  }
  isConferModalOpen.value = true;
}

// Modal 3: Auditable Correction
const isCorrectionModalOpen = ref(false);
const correctionTargetType = ref<'standing' | 'progression'>('standing');
const correctionTargetId = ref('');
const correctionStudentName = ref('');
const correctionStudentIdentifier = ref('');
const correctionInitialStanding = ref<AcademicStandingStatus>('good_standing');
const correctionInitialPolicy = ref('SK-AKAD/2026/POLICY-01');
const correctionInitialConditions = ref<string | null>('');
const correctionInitialRec = ref<PromotionRecommendationStatus>('promotion_eligible');
const correctionInitialFromLevel = ref('Grade 4');
const correctionInitialToLevel = ref<string | null>('Grade 5');
const correctionInitialEvidence = ref('');
const correctionInitialIsException = ref(false);
const correctionInitialExceptionReason = ref<string | null>('');
const correctionInitialCommitteeRef = ref<string | null>('');

function handleOpenCorrection(student: StudentReviewItem, type: 'standing' | 'progression') {
  correctionTargetType.value = type;
  correctionTargetId.value = (type === 'standing' ? student.existingStandingId : student.existingRecommendationId) || '';
  correctionStudentName.value = student.studentName;
  correctionStudentIdentifier.value = student.studentIdentifier || '';

  correctionInitialStanding.value = student.standing;
  correctionInitialPolicy.value = student.policyReference;
  correctionInitialConditions.value = student.conditions || '';

  correctionInitialRec.value = student.recommendation;
  correctionInitialFromLevel.value = student.fromLevel;
  correctionInitialToLevel.value = student.recommendedToLevel || '';
  correctionInitialEvidence.value = student.supportingEvidence;

  correctionInitialIsException.value = student.isException;
  correctionInitialExceptionReason.value = student.exceptionReason || '';
  correctionInitialCommitteeRef.value = student.committeeReference || '';

  isCorrectionModalOpen.value = true;
}

function handleRefreshAll() {
  refetchStandingEval();
  refetchProgressionEval();
  refetchCommittedStandings();
  refetchCommittedRecommendations();
  refetchAchievements();
}

// ---------------------------------------------------------------------------
// 8. Badge & Formatting Helpers
// ---------------------------------------------------------------------------
function getStandingBadge(status: AcademicStandingStatus) {
  switch (status) {
    case 'good_standing':
      return { severity: 'success', label: 'Good Standing' };
    case 'conditional_standing':
      return { severity: 'warn', label: 'Conditional' };
    case 'not_in_good_standing':
      return { severity: 'danger', label: 'Not in Good Standing' };
    default:
      return { severity: 'contrast', label: status };
  }
}

function getProgressionBadge(rec: PromotionRecommendationStatus) {
  switch (rec) {
    case 'promotion_eligible':
      return { severity: 'success', label: 'Promotion Eligible' };
    case 'conditional_promotion':
      return { severity: 'warn', label: 'Conditional Promotion' };
    case 'retention_recommended':
      return { severity: 'danger', label: 'Retention Recommended' };
    default:
      return { severity: 'contrast', label: rec };
  }
}

function formatAchievementType(type: string): string {
  return type
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}
</script>

<template>
  <div class="space-y-6 pb-24">
    <!-- Header with Breadcrumbs & Actions -->
    <PageHeader
      title="Cohort Academic Review"
      subtitle="Standing determination preview, committee exception decrees, batch cohort certification, and honors conferral (SCR-ACD-09)"
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <Button
            variant="outlined"
            severity="secondary"
            class="text-xs"
            :loading="isFetchingStandingEval || isFetchingProgressionEval || isFetchingAchievements"
            @click="handleRefreshAll"
            data-testid="btn-refresh-cohort"
          >
            <RefreshCw class="w-3.5 h-3.5 mr-1" />
            Refresh
          </Button>

          <Button
            v-if="canConferAchievement"
            class="text-xs"
            severity="contrast"
            @click="handleOpenConferAchievement()"
            data-testid="btn-confer-achievement-header"
          >
            <Trophy class="w-3.5 h-3.5 mr-1 text-amber-400" />
            Confer Achievement
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Period & Grade Level Selector Bar (SCR-ACD-09) -->
    <div class="flex flex-wrap items-center justify-between gap-3 bg-surface-0 dark:bg-surface-900 p-4 rounded-xl border border-surface-200 dark:border-surface-700 shadow-xs">
      <div class="flex flex-wrap items-center gap-3">
        <!-- Academic Period -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Academic Period ID
          </label>
          <InputText
            v-model="selectedPeriodId"
            placeholder="Academic Period UUID"
            class="w-64 text-xs"
            data-testid="input-cohort-period-id"
          />
        </div>

        <!-- Grade Level (Triggers dynamic cohort evaluation) -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Grade Level Cohort
          </label>
          <Select
            v-model="selectedGradeLevel"
            :options="gradeLevelOptions"
            option-label="label"
            option-value="value"
            class="w-44 text-xs"
            data-testid="select-cohort-grade-level"
          />
        </div>
      </div>

      <!-- Search Input -->
      <div class="w-full sm:w-64">
        <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
          Search Cohort
        </label>
        <div class="relative">
          <Search class="w-3.5 h-3.5 text-surface-400 absolute left-3 top-2.5" />
          <InputText
            v-model="searchFilter"
            placeholder="Student name, NISN..."
            class="w-full pl-8 text-xs"
            data-testid="input-search-cohort"
          />
        </div>
      </div>
    </div>

    <!-- Main Tabs (SCR-ACD-09) -->
    <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden">
      <Tabs v-model:value="activeTab" class="w-full" data-testid="cohort-review-tabs">
        <TabList class="border-b border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/50 px-4">
          <!-- Tab 1: Standing & Progression Review -->
          <Tab value="standingProgression" data-testid="tab-standing-progression">
            <div class="flex items-center gap-2 py-1">
              <Scale class="w-4 h-4 text-indigo-600" />
              <span class="font-semibold text-xs">Standing & Progression Review</span>
              <span
                v-if="totalCount > 0"
                class="px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-indigo-100 text-indigo-800 dark:bg-indigo-900/60 dark:text-indigo-200"
              >
                {{ totalCount }} candidates
              </span>
            </div>
          </Tab>

          <!-- Tab 2: Academic Honors & Achievements -->
          <Tab value="achievements" data-testid="tab-honors-achievements">
            <div class="flex items-center gap-2 py-1">
              <Trophy class="w-4 h-4 text-amber-500" />
              <span class="font-semibold text-xs">Academic Honors & Achievements</span>
              <span
                v-if="(achievementsData ?? []).length > 0"
                class="px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800 dark:bg-amber-900/60 dark:text-amber-200"
              >
                {{ (achievementsData ?? []).length }}
              </span>
            </div>
          </Tab>
        </TabList>

        <TabPanels class="p-6">
          <!-- =============================================================== -->
          <!-- TAB 1: STANDING & PROGRESSION REVIEW                            -->
          <!-- =============================================================== -->
          <TabPanel value="standingProgression" data-testid="panel-standing-progression">
            <div class="space-y-4">
              <!-- Evaluation Status Banner -->
              <div class="flex items-center justify-between p-3.5 bg-indigo-50/60 dark:bg-indigo-950/20 border border-indigo-200 dark:border-indigo-800 rounded-xl text-xs">
                <div class="flex items-start gap-3">
                  <Sparkles class="w-4 h-4 text-indigo-600 mt-0.5 shrink-0" />
                  <div>
                    <span class="font-bold text-indigo-950 dark:text-indigo-200 block">
                      Dynamic Cohort Evaluation Engine (TD-03, Completion & Passing Threshold Rule)
                    </span>
                    <p class="text-indigo-900 dark:text-indigo-300">
                      Standings and progression recommendations are computed on-demand from operative approved final assessments and presence rates. Academic Authority may apply decree overrides prior to batch certification.
                    </p>
                  </div>
                </div>

                <div v-if="overridesCount > 0" class="shrink-0 flex items-center gap-2">
                  <Tag severity="warn" class="text-[11px] font-bold">
                    {{ overridesCount }} Overridden
                  </Tag>
                  <Button
                    label="Reset Overrides"
                    variant="text"
                    severity="secondary"
                    size="small"
                    class="text-xs"
                    @click="handleResetOverrides"
                    data-testid="btn-reset-overrides"
                  >
                    <RotateCcw class="w-3 h-3 mr-1" />
                    Reset
                  </Button>
                </div>
              </div>

              <!-- Loading State -->
              <div v-if="isLoadingStandingEval || isLoadingProgressionEval" class="py-12 flex justify-center">
                <LoadingSpinner />
              </div>

              <!-- Empty State -->
              <div v-else-if="filteredReviewRows.length === 0" class="py-12">
                <EmptyState
                  title="No Candidates Found"
                  description="No enrolled students found in this academic period and grade level cohort with active participation or administrative placements."
                />
              </div>

              <!-- Cohort Review DataTable -->
              <DataTable
                v-else
                :value="filteredReviewRows"
                data-key="studentId"
                responsive-layout="scroll"
                striped-rows
                class="text-xs"
                data-testid="table-cohort-review"
              >
                <!-- Student -->
                <Column header="Student Candidate" style="min-width: 180px">
                  <template #body="{ data }">
                    <div>
                      <span class="font-semibold text-surface-900 dark:text-surface-100 block">
                        {{ data.studentName }}
                      </span>
                      <span v-if="data.studentIdentifier" class="text-surface-400 text-[11px] block">
                        NISN: {{ data.studentIdentifier }}
                      </span>
                      <span v-if="data.existingStandingId" class="inline-flex items-center gap-1 text-[10px] text-emerald-600 dark:text-emerald-400 font-medium">
                        <CheckCircle2 class="w-3 h-3" /> Certified Record
                      </span>
                    </div>
                  </template>
                </Column>

                <!-- Units Metrics -->
                <Column header="Units Passed / Failed" style="width: 140px">
                  <template #body="{ data }">
                    <div class="space-y-0.5 text-[11px]">
                      <div class="text-emerald-600 dark:text-emerald-400 font-semibold">
                        Passed: {{ data.passedUnitsCount }} / {{ data.totalUnitsCount }}
                      </div>
                      <div
                        v-if="data.failedUnitsCount > 0"
                        class="text-red-600 dark:text-red-400 font-semibold"
                      >
                        Failed: {{ data.failedUnitsCount }} units
                      </div>
                      <div v-else class="text-surface-400">
                        No failing units
                      </div>
                    </div>
                  </template>
                </Column>

                <!-- Attendance % -->
                <Column header="Attendance" style="width: 100px">
                  <template #body="{ data }">
                    <span
                      class="font-bold text-xs"
                      :class="data.attendancePercentage < 75 ? 'text-red-600 dark:text-red-400' : 'text-emerald-600 dark:text-emerald-400'"
                    >
                      {{ data.attendancePercentage }}%
                    </span>
                  </template>
                </Column>

                <!-- Proposed / Determined Standing -->
                <Column header="Proposed Academic Standing" style="min-width: 180px">
                  <template #body="{ data }">
                    <div class="space-y-1">
                      <div class="flex items-center gap-1.5">
                        <Tag
                          :severity="getStandingBadge(data.standing).severity"
                          :value="getStandingBadge(data.standing).label"
                          class="text-[10px] font-bold"
                          data-testid="badge-standing"
                        />
                        <Tag
                          v-if="data.isOverridden"
                          severity="contrast"
                          value="Overridden"
                          class="text-[9px]"
                        />
                      </div>

                      <p v-if="data.conditions" class="text-surface-600 dark:text-surface-300 text-[11px] line-clamp-2">
                        <strong class="text-surface-500">Cond:</strong> {{ data.conditions }}
                      </p>
                    </div>
                  </template>
                </Column>

                <!-- Proposed / Issued Progression Recommendation -->
                <Column header="Progression Recommendation" style="min-width: 200px">
                  <template #body="{ data }">
                    <div class="space-y-1">
                      <Tag
                        :severity="getProgressionBadge(data.recommendation).severity"
                        :value="getProgressionBadge(data.recommendation).label"
                        class="text-[10px] font-bold"
                        data-testid="badge-progression"
                      />

                      <div class="text-[11px] text-surface-500">
                        <span>Target: </span>
                        <strong class="text-surface-800 dark:text-surface-200">
                          {{ data.recommendedToLevel || data.fromLevel }}
                        </strong>
                      </div>

                      <!-- Exception Decree Tag -->
                      <div v-if="data.isException" class="flex items-center gap-1 text-[10px] text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-950/40 px-1.5 py-0.5 rounded border border-amber-200 dark:border-amber-800">
                        <FileCheck2 class="w-3 h-3 shrink-0" />
                        <span class="font-bold">{{ data.committeeReference || 'Decree Decree' }}</span>
                      </div>
                    </div>
                  </template>
                </Column>

                <!-- Actions Column -->
                <Column header="Actions" style="width: 180px" align-frozen="right" frozen>
                  <template #body="{ data }">
                    <div class="flex items-center gap-1.5">
                      <!-- Override / Exception Button -->
                      <Button
                        label="Override / Decree"
                        icon="pi pi-sliders-h"
                        variant="outlined"
                        severity="secondary"
                        size="small"
                        class="text-xs"
                        :disabled="!isAcademicAuthority"
                        @click="handleOpenOverride(data)"
                        :data-testid="`btn-override-${data.studentId}`"
                      />

                      <!-- Auditable Correction (if certified) -->
                      <Button
                        v-if="data.existingStandingId && canCorrectRecord"
                        icon="pi pi-pencil"
                        variant="text"
                        severity="secondary"
                        size="small"
                        class="text-xs p-1"
                        title="Auditable In-Place Correction (UC-ACD-15)"
                        @click="handleOpenCorrection(data, 'standing')"
                        :data-testid="`btn-correct-${data.studentId}`"
                      />

                      <!-- Quick Confer Achievement -->
                      <Button
                        v-if="canConferAchievement"
                        icon="pi pi-trophy"
                        variant="text"
                        severity="warn"
                        size="small"
                        class="text-xs p-1"
                        title="Confer Academic Honor"
                        @click="handleOpenConferAchievement(data)"
                        :data-testid="`btn-quick-confer-${data.studentId}`"
                      />
                    </div>
                  </template>
                </Column>
              </DataTable>
            </div>
          </TabPanel>

          <!-- =============================================================== -->
          <!-- TAB 2: ACADEMIC HONORS & ACHIEVEMENTS                           -->
          <!-- =============================================================== -->
          <TabPanel value="achievements" data-testid="panel-honors-achievements">
            <div class="space-y-4">
              <!-- Achievements Banner -->
              <div class="flex items-center justify-between p-3.5 bg-amber-50/60 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800 rounded-xl text-xs">
                <div class="flex items-start gap-3">
                  <Trophy class="w-4 h-4 text-amber-500 mt-0.5 shrink-0" />
                  <div>
                    <span class="font-bold text-amber-950 dark:text-amber-200 block">
                      Academic Honors & Distinction Roster (UC-ACD-21, SCR-ACD-09)
                    </span>
                    <p class="text-amber-900 dark:text-amber-300">
                      Formal academic awards, subject distinctions (*Nilai Tertinggi*), and curriculum-recognized competition achievements earned within the selected academic period.
                    </p>
                  </div>
                </div>

                <Button
                  v-if="canConferAchievement"
                  label="Confer Achievement"
                  icon="pi pi-plus"
                  size="small"
                  class="text-xs shrink-0"
                  severity="primary"
                  @click="handleOpenConferAchievement()"
                  data-testid="btn-confer-achievement-tab"
                />
              </div>

              <!-- Loading State -->
              <div v-if="isLoadingAchievements" class="py-12 flex justify-center">
                <LoadingSpinner />
              </div>

              <!-- Empty State -->
              <div v-else-if="(achievementsData ?? []).length === 0" class="py-12">
                <EmptyState
                  title="No Academic Honors Conferred"
                  description="No academic achievements or subject distinctions have been conferred for this period yet."
                >
                  <template #actions>
                    <Button
                      v-if="canConferAchievement"
                      label="Confer Achievement"
                      icon="pi pi-trophy"
                      size="small"
                      class="text-xs"
                      @click="handleOpenConferAchievement()"
                    />
                  </template>
                </EmptyState>
              </div>

              <!-- Achievements DataTable -->
              <DataTable
                v-else
                :value="achievementsData"
                data-key="id"
                responsive-layout="scroll"
                striped-rows
                class="text-xs"
                data-testid="table-achievements"
              >
                <!-- Student -->
                <Column header="Student" style="min-width: 170px">
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

                <!-- Category -->
                <Column header="Honor Category" style="min-width: 160px">
                  <template #body="{ data }">
                    <span class="font-medium text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-950/40 px-2 py-0.5 rounded text-[11px]">
                      {{ formatAchievementType(data.achievementType) }}
                    </span>
                  </template>
                </Column>

                <!-- Title & Citation -->
                <Column header="Title & Citation" style="min-width: 240px">
                  <template #body="{ data }">
                    <div class="space-y-0.5">
                      <span class="font-bold text-surface-900 dark:text-surface-100 text-xs block">
                        {{ data.title }}
                      </span>
                      <p class="text-surface-600 dark:text-surface-300 text-[11px]">
                        {{ data.criteriaMet }}
                      </p>
                      <p v-if="data.supportingEvidence" class="text-surface-400 text-[10px]">
                        Evidence: {{ data.supportingEvidence }}
                      </p>
                    </div>
                  </template>
                </Column>

                <!-- Recognized At & By -->
                <Column header="Authority & Date" style="width: 160px">
                  <template #body="{ data }">
                    <div class="space-y-0.5 text-[11px] text-surface-500">
                      <div>
                        <span class="text-surface-400">Date:</span>
                        {{ formatDisplayDate(data.recognizedAt) }}
                      </div>
                      <div>
                        <span class="text-surface-400">By:</span>
                        <strong class="text-surface-700 dark:text-surface-300">{{ data.recognizedBy }}</strong>
                      </div>
                    </div>
                  </template>
                </Column>
              </DataTable>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- =================================================================== -->
    <!-- STICKY FOOTER ACTION BAR (SCR-ACD-09, ACD-WF-13)                    -->
    <!-- =================================================================== -->
    <div
      v-if="activeTab === 'standingProgression' && combinedReviewRows.length > 0"
      class="fixed bottom-0 left-0 right-0 z-40 bg-surface-0/95 dark:bg-surface-900/95 backdrop-blur-md border-t border-surface-200 dark:border-surface-700 px-6 py-3.5 shadow-lg flex flex-wrap items-center justify-between gap-4"
      data-testid="cohort-review-footer-bar"
    >
      <!-- Summary Counts KPIs -->
      <div class="flex flex-wrap items-center gap-4 text-xs">
        <span class="text-surface-500 font-semibold uppercase tracking-wider text-[11px]">
          Cohort Summary ({{ selectedGradeLevel }}):
        </span>

        <div class="flex items-center gap-1.5">
          <Tag severity="success" class="text-xs font-bold" data-testid="kpi-eligible">
            Eligible: {{ eligibleCount }}
          </Tag>
        </div>

        <div class="flex items-center gap-1.5">
          <Tag severity="warn" class="text-xs font-bold" data-testid="kpi-conditional">
            Conditional: {{ conditionalCount }}
          </Tag>
        </div>

        <div class="flex items-center gap-1.5">
          <Tag severity="danger" class="text-xs font-bold" data-testid="kpi-retention">
            Retention: {{ retentionCount }}
          </Tag>
        </div>

        <div v-if="overridesCount > 0" class="flex items-center gap-1 text-amber-700 dark:text-amber-400 font-medium text-[11px]">
          <span>Decree Overrides: {{ overridesCount }}</span>
        </div>
      </div>

      <!-- Batch Certification Action -->
      <div class="flex items-center gap-3">
        <Button
          label="Approve & Commit Cohort"
          icon="pi pi-check-circle"
          severity="primary"
          size="small"
          class="text-xs font-bold px-4 py-2"
          :disabled="!canApproveCohort || combinedReviewRows.length === 0"
          :loading="isSubmittingBatch"
          @click="isConfirmCommitOpen = true"
          data-testid="btn-approve-commit-cohort"
        />
      </div>
    </div>

    <!-- Confirmation Modal for Batch Cohort Commitment -->
    <Dialog
      v-model:visible="isConfirmCommitOpen"
      modal
      header="Confirm Cohort Standing & Progression Certification"
      :draggable="false"
      class="w-full max-w-lg mx-4"
      data-testid="dialog-confirm-cohort-commit"
    >
      <div class="space-y-4 pt-2 text-xs">
        <div class="flex items-start gap-3 p-3 bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800 rounded-xl text-amber-900 dark:text-amber-200">
          <AlertTriangle class="w-5 h-5 text-amber-600 mt-0.5 shrink-0" />
          <p>
            You are about to certify and commit formal academic standings and progression recommendations for <strong>{{ combinedReviewRows.length }} students</strong> in <strong>{{ selectedGradeLevel }}</strong>.
          </p>
        </div>

        <div class="space-y-2 bg-surface-50 dark:bg-surface-800/50 p-3 rounded-lg border border-surface-200 dark:border-surface-700">
          <div class="flex justify-between">
            <span class="text-surface-500">Academic Period:</span>
            <span class="font-mono text-surface-900 dark:text-surface-100">{{ selectedPeriodId }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-surface-500">Grade Level:</span>
            <span class="font-bold text-surface-900 dark:text-surface-100">{{ selectedGradeLevel }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-surface-500">Promotion Eligible:</span>
            <span class="font-bold text-emerald-600">{{ eligibleCount }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-surface-500">Conditional Promotion:</span>
            <span class="font-bold text-amber-600">{{ conditionalCount }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-surface-500">Retention Recommended:</span>
            <span class="font-bold text-red-600">{{ retentionCount }}</span>
          </div>
        </div>

        <p class="text-surface-500">
          Once committed, records become authoritative inputs for downstream administrative grade rollover in the Students domain. Subsequent alterations will require formal in-place record corrections (<code class="bg-surface-100 dark:bg-surface-800 px-1 py-0.5 rounded font-mono">UC-ACD-15</code>).
        </p>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            label="Cancel"
            variant="text"
            severity="secondary"
            size="small"
            class="text-xs"
            @click="isConfirmCommitOpen = false"
          />
          <Button
            label="Confirm & Commit Batch"
            icon="pi pi-check"
            severity="primary"
            size="small"
            class="text-xs"
            :loading="isSubmittingBatch"
            @click="handleCommitCohort"
            data-testid="btn-confirm-batch-commit"
          />
        </div>
      </template>
    </Dialog>

    <!-- Modal 1: Standing & Progression Override -->
    <StandingOverrideModal
      v-model:visible="isOverrideModalOpen"
      :student="selectedStudentForOverride"
      @applied="handleApplyOverride"
    />

    <!-- Modal 2: Confer Academic Achievement -->
    <ConferAchievementModal
      v-model:visible="isConferModalOpen"
      :academic-period-id="selectedPeriodId"
      :initial-student-id="conferInitialStudentId"
      :initial-student-name="conferInitialStudentName"
      @conferred="refetchAchievements"
    />

    <!-- Modal 3: Auditable Non-Grade Record Correction -->
    <StandingCorrectionModal
      v-model:visible="isCorrectionModalOpen"
      :target-type="correctionTargetType"
      :target-id="correctionTargetId"
      :student-name="correctionStudentName"
      :student-identifier="correctionStudentIdentifier"
      :initial-standing="correctionInitialStanding"
      :initial-policy-reference="correctionInitialPolicy"
      :initial-conditions="correctionInitialConditions"
      :initial-recommendation="correctionInitialRec"
      :initial-from-level="correctionInitialFromLevel"
      :initial-recommended-to-level="correctionInitialToLevel"
      :initial-supporting-evidence="correctionInitialEvidence"
      :initial-is-exception="correctionInitialIsException"
      :initial-exception-reason="correctionInitialExceptionReason"
      :initial-committee-reference="correctionInitialCommitteeRef"
      @corrected="handleRefreshAll"
    />
  </div>
</template>
