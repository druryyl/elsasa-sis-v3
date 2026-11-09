<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import Select from 'primevue/select';
import {
  Award,
  BookOpen,
  Calendar,
  Printer,
  FileText,
  MessageSquareQuote,
  AlertTriangle,
  RefreshCw,
} from 'lucide-vue-next';
import {
  LoadingSpinner,
  EmptyState,
} from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useStudentResults } from '@/features/academics/assessment/composables/useStudentResults';
import { useReportCards } from '@/features/academics/records/composables/useReportCards';
import type { StudentPublishedResult, ReportCard } from '@/features/academics/records/types';
import ReportCardPrintModal from '@/features/portal/components/ReportCardPrintModal.vue';

interface Props {
  studentId?: string;
  initialPeriodId?: string;
}

const props = withDefaults(defineProps<Props>(), {
  studentId: undefined,
  initialPeriodId: '2026-TERM-1',
});

// Academic Period state
const selectedPeriodId = ref<string>(props.initialPeriodId);

const academicPeriods = [
  { label: 'Semester 1 (Ganjil) 2026/2027', value: '2026-TERM-1' },
  { label: 'Semester 2 (Genap) 2026/2027', value: '2026-TERM-2' },
  { label: 'Semester 1 (Ganjil) 2025/2026', value: '2025-TERM-1' },
  { label: 'Semester 2 (Genap) 2025/2026', value: '2025-TERM-2' },
];

// 1. Published Student Results Query
const {
  results: publishedResults,
  isLoading: isLoadingResults,
  isError: isResultsError,
  error: resultsError,
  refetch: refetchResults,
} = useStudentResults(selectedPeriodId, props.studentId);

// 2. Report Cards Query
const {
  reportCards,
  isLoading: isLoadingReportCards,
  isError: isReportCardsError,
  refetch: refetchReportCards,
} = useReportCards(selectedPeriodId, props.studentId);

// Group published results by Learning Unit / Subject
interface SubjectGroup {
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitTitle: string;
  results: StudentPublishedResult[];
  averageScore?: number | null;
}

const groupedResults = computed<SubjectGroup[]>(() => {
  const list = publishedResults.value || [];
  const map = new Map<string, SubjectGroup>();

  for (const item of list) {
    let group = map.get(item.learningUnitId);
    if (!group) {
      group = {
        learningUnitId: item.learningUnitId,
        learningUnitCode: item.learningUnitCode,
        learningUnitTitle: item.learningUnitTitle,
        results: [],
      };
      map.set(item.learningUnitId, group);
    }
    group.results.push(item);
  }

  // Calculate average scores where numeric scores exist
  return Array.from(map.values()).map((group) => {
    const numericScores = group.results
      .map((r) => r.scoreNumeric)
      .filter((s): s is number => typeof s === 'number');

    const avg =
      numericScores.length > 0
        ? Math.round(
            (numericScores.reduce((acc, curr) => acc + curr, 0) / numericScores.length) * 10,
          ) / 10
        : null;

    return {
      ...group,
      averageScore: avg,
    };
  });
});

// Modal state for Report Card Print Preview
const selectedReportCardForPrint = ref<ReportCard | null>(null);
const isPrintModalOpen = ref(false);

function handleOpenPrintModal(card: ReportCard) {
  selectedReportCardForPrint.value = card;
  isPrintModalOpen.value = true;
}

function handleRefreshAll() {
  refetchResults();
  refetchReportCards();
}
</script>

<template>
  <div class="space-y-6" data-testid="student-results-tab">
    <!-- Header & Period Filter Control -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 bg-white rounded-xl border border-slate-200 shadow-xs">
      <div>
        <h3 class="text-sm font-bold text-slate-900 leading-tight">
          Grades &amp; Certified Report Cards
        </h3>
        <p class="text-xs text-slate-500 mt-0.5">
          Review your published marks, competency evaluations, and official Buku Rapor.
        </p>
      </div>

      <div class="flex items-center gap-2.5 w-full sm:w-auto">
        <label for="period-select" class="text-xs font-medium text-slate-600 shrink-0">
          Period:
        </label>
        <Select
          id="period-select"
          v-model="selectedPeriodId"
          :options="academicPeriods"
          option-label="label"
          option-value="value"
          class="w-full sm:w-64 !text-xs"
          data-testid="select-academic-period"
        />
        <Button
          type="button"
          rounded
          text
          severity="secondary"
          size="small"
          class="!p-2 text-slate-500 hover:text-slate-800"
          title="Refresh Grades"
          data-testid="btn-refresh-results"
          @click="handleRefreshAll"
        >
          <RefreshCw class="w-4 h-4" />
        </Button>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="isResultsError || isReportCardsError"
      class="p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 flex items-start gap-3"
      data-testid="results-error-banner"
    >
      <AlertTriangle class="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
      <div>
        <h4 class="text-sm font-semibold text-rose-950">Unable to load published results</h4>
        <p class="text-xs text-rose-700 mt-0.5">
          {{ resultsError instanceof Error ? resultsError.message : 'An error occurred querying academic results.' }}
        </p>
        <Button
          type="button"
          label="Retry"
          size="small"
          severity="danger"
          text
          class="!p-0 !mt-2 text-xs font-semibold"
          @click="handleRefreshAll"
        />
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="isLoadingResults || isLoadingReportCards"
      class="py-16 flex justify-center"
      data-testid="results-loading-spinner"
    >
      <LoadingSpinner size="lg" message="Loading published grades and report cards..." />
    </div>

    <template v-else>
      <!-- Section 1: Certified Report Cards Desk -->
      <div class="space-y-3" data-testid="report-cards-section">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <Award class="w-4 h-4 text-teal-700" />
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
              Certified Report Cards (Buku Rapor Merdeka)
            </h4>
          </div>
          <span class="text-xs text-slate-500 font-medium">
            {{ reportCards.length }} Certified Document(s)
          </span>
        </div>

        <!-- Report Cards Cards Grid -->
        <div
          v-if="reportCards.length > 0"
          class="grid grid-cols-1 md:grid-cols-2 gap-4"
          data-testid="report-cards-grid"
        >
          <div
            v-for="card in reportCards"
            :key="card.id"
            class="p-5 rounded-xl bg-white border border-slate-200 hover:border-teal-300 shadow-xs transition-all space-y-4"
            :data-testid="`report-card-item-${card.id}`"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="space-y-1">
                <div class="flex items-center gap-2">
                  <span class="font-bold text-sm text-slate-900 leading-snug">
                    {{ card.templateName || 'Buku Rapor Merdeka' }}
                  </span>
                  <Tag
                    value="Certified"
                    severity="success"
                    class="!text-[10px] !py-0.5"
                  />
                </div>
                <p class="text-xs text-slate-600 flex items-center gap-1.5">
                  <Calendar class="w-3.5 h-3.5 text-slate-400" />
                  <span>Period: <strong>{{ card.periodLabel || card.academicPeriodLabel || selectedPeriodId }}</strong></span>
                </p>
              </div>

              <div class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-700 shrink-0">
                <FileText class="w-5 h-5" />
              </div>
            </div>

            <!-- Card Approval & Teacher Comments Preview -->
            <div
              v-if="card.narrativeComments"
              class="text-xs text-slate-600 bg-slate-50 p-2.5 rounded-lg border border-slate-200/80 italic line-clamp-2"
            >
              "{{ card.narrativeComments }}"
            </div>

            <!-- Card Action Footer -->
            <div class="pt-2 border-t border-slate-100 flex items-center justify-between gap-2">
              <span class="text-[11px] text-slate-500">
                Approved by <strong>{{ card.approvedBy || 'Academic Authority' }}</strong>
              </span>

              <Button
                type="button"
                label="View &amp; Print Report Card"
                size="small"
                class="!text-xs !bg-teal-700 hover:!bg-teal-800 text-white font-medium cursor-pointer"
                :data-testid="`btn-print-report-card-${card.id}`"
                @click="handleOpenPrintModal(card)"
              >
                <template #icon>
                  <Printer class="w-3.5 h-3.5 mr-1" />
                </template>
              </Button>
            </div>
          </div>
        </div>

        <!-- No Report Cards Empty State -->
        <div
          v-else
          class="p-6 bg-slate-50 border border-dashed border-slate-200 rounded-xl text-center space-y-2"
          data-testid="no-report-cards-empty"
        >
          <Award class="w-8 h-8 text-slate-300 mx-auto" />
          <p class="text-xs font-semibold text-slate-700">No certified report card published yet</p>
          <p class="text-[11px] text-slate-500 max-w-md mx-auto">
            Periodic report cards are published by the Academic Authority at the conclusion of midterm and final grading cycles.
          </p>
        </div>
      </div>

      <!-- Section 2: Published Grades by Subject / Learning Unit -->
      <div class="space-y-4 pt-4 border-t border-slate-200" data-testid="published-grades-section">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <BookOpen class="w-4 h-4 text-teal-700" />
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
              Published Academic Grades by Subject ({{ groupedResults.length }} Subjects)
            </h4>
          </div>
          <span class="text-xs text-slate-500">
            Total Evaluations: <strong>{{ publishedResults.length }}</strong>
          </span>
        </div>

        <!-- Empty state when no grades published -->
        <div
          v-if="groupedResults.length === 0"
          class="py-12 bg-white rounded-xl border border-slate-200"
          data-testid="no-published-grades-empty"
        >
          <EmptyState
            title="No Published Grades Available"
            description="There are currently no published assessment results for this academic period. Check back once evaluations are released by your teachers."
          />
        </div>

        <!-- Subject Groups Accordion / List -->
        <div v-else class="space-y-5" data-testid="subject-groups-list">
          <div
            v-for="group in groupedResults"
            :key="group.learningUnitId"
            class="bg-white border border-slate-200 rounded-xl shadow-xs overflow-hidden"
            :data-testid="`learning-unit-group-${group.learningUnitId}`"
          >
            <!-- Subject Header Bar -->
            <div class="px-5 py-3.5 bg-slate-50 border-b border-slate-200 flex flex-wrap items-center justify-between gap-3">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-lg bg-teal-100/70 text-teal-800 flex items-center justify-center font-bold text-xs shrink-0">
                  <BookOpen class="w-4 h-4 text-teal-700" />
                </div>
                <div>
                  <h5 class="text-sm font-bold text-slate-900 leading-tight">
                    {{ group.learningUnitTitle }}
                  </h5>
                  <span class="font-mono text-[11px] text-slate-500">
                    {{ group.learningUnitCode }}
                  </span>
                </div>
              </div>

              <!-- Subject Summary Metrics -->
              <div class="flex items-center gap-3">
                <span
                  v-if="group.averageScore !== null && group.averageScore !== undefined"
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-teal-50 border border-teal-200 text-xs font-bold text-teal-800"
                  data-testid="subject-average-score"
                >
                  Avg: {{ group.averageScore }}
                </span>
                <span class="text-xs text-slate-500">
                  {{ group.results.length }} evaluation(s)
                </span>
              </div>
            </div>

            <!-- Subject Assessment Results Items -->
            <div class="divide-y divide-slate-100">
              <div
                v-for="result in group.results"
                :key="result.resultId"
                class="p-4 sm:p-5 hover:bg-slate-50/50 transition-colors space-y-3"
                :data-testid="`grade-item-${result.resultId}`"
              >
                <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                  <div class="space-y-1">
                    <div class="flex items-center gap-2">
                      <span class="font-bold text-sm text-slate-900">
                        {{ result.assessmentTitle }}
                      </span>
                      <Tag
                        :value="result.assessmentType"
                        :severity="result.assessmentType === 'formative' ? 'info' : 'warn'"
                        class="!text-[10px] !py-0.2 uppercase"
                      />
                      <span
                        v-if="result.attemptNumber > 1"
                        class="text-[10px] font-mono font-semibold px-1.5 py-0.2 rounded bg-amber-50 text-amber-800 border border-amber-200"
                      >
                        Attempt {{ result.attemptNumber }}
                      </span>
                    </div>
                    <div class="flex items-center gap-3 text-xs text-slate-500">
                      <span>Evaluated: {{ formatDisplayDate(result.evaluatedDate) }}</span>
                      <span>•</span>
                      <span>Model: {{ result.evaluationModel }}</span>
                    </div>
                  </div>

                  <!-- Score & Grade Badges Row -->
                  <div class="flex items-center gap-3 self-start sm:self-auto">
                    <!-- Numeric Score -->
                    <div
                      v-if="result.scoreNumeric !== null && result.scoreNumeric !== undefined"
                      class="text-right"
                    >
                      <span class="font-bold text-base text-slate-900" data-testid="score-numeric-display">
                        {{ result.scoreNumeric }}
                      </span>
                      <span v-if="result.scoreMax" class="text-xs text-slate-500 font-medium">
                        / {{ result.scoreMax }}
                      </span>
                    </div>

                    <!-- Letter Grade -->
                    <div
                      v-if="result.gradeLetter"
                      class="w-8 h-8 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center font-bold text-sm text-teal-800"
                      data-testid="grade-letter-badge"
                    >
                      {{ result.gradeLetter }}
                    </div>

                    <!-- Grade Label -->
                    <span
                      v-if="result.gradeLabel"
                      class="px-2 py-1 rounded bg-slate-100 text-slate-700 text-xs font-semibold"
                      data-testid="grade-label-tag"
                    >
                      {{ result.gradeLabel }}
                    </span>
                  </div>
                </div>

                <!-- Teacher Formative Remarks / Narrative Feedback -->
                <div
                  v-if="result.narrativeFeedback"
                  class="flex items-start gap-2.5 p-3 rounded-lg bg-teal-50/50 border border-teal-100 text-xs text-teal-950"
                  data-testid="teacher-feedback-remark"
                >
                  <MessageSquareQuote class="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
                  <div class="leading-relaxed">
                    <strong class="font-semibold text-teal-900">Teacher Remarks:</strong>
                    {{ result.narrativeFeedback }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Report Card Print Preview Modal -->
    <ReportCardPrintModal
      v-model:visible="isPrintModalOpen"
      :report-card="selectedReportCardForPrint"
    />
  </div>
</template>
