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
import { useAuthStore } from '@/app/stores/auth.store';
import { useStudentResults } from '@/features/academics/assessment/composables/useStudentResults';
import { useReportCards } from '@/features/academics/records/composables/useReportCards';
import type { StudentPublishedResult, ReportCard } from '@/features/academics/records/types';
import ReportCardPrintModal from './components/ReportCardPrintModal.vue';

interface Props {
  studentId?: string;
}

const props = withDefaults(defineProps<Props>(), {
  studentId: undefined,
});

const authStore = useAuthStore();

// Fall back to the linked student identity carried on the authenticated session.
const effectiveStudentId = computed(() => {
  if (props.studentId) return props.studentId;
  if (authStore.user?.studentId) return authStore.user.studentId;
  if (authStore.roles.includes('Student')) return authStore.user?.userId ?? '';
  return '';
});

// Academic period filter
const selectedPeriodId = ref<string>('2026-TERM-1');

const academicPeriods = [
  { label: 'Semester 1 (Ganjil) 2026/2027', value: '2026-TERM-1' },
  { label: 'Semester 2 (Genap) 2026/2027', value: '2026-TERM-2' },
  { label: 'Semester 1 (Ganjil) 2025/2026', value: '2025-TERM-1' },
  { label: 'Semester 2 (Genap) 2025/2026', value: '2025-TERM-2' },
];

// 1. Published student evaluations query
const {
  results: publishedResults,
  isLoading: isLoadingResults,
  isError: isResultsError,
  error: resultsError,
  refetch: refetchResults,
} = useStudentResults(selectedPeriodId, effectiveStudentId);

// 2. Report cards query
const {
  reportCards,
  isLoading: isLoadingReportCards,
  isError: isReportCardsError,
  refetch: refetchReportCards,
} = useReportCards(selectedPeriodId, effectiveStudentId);

// Group results by subject
interface FamilySubjectGroup {
  learningUnitId: string;
  learningUnitCode: string;
  learningUnitTitle: string;
  results: StudentPublishedResult[];
  averageScore?: number | null;
}

const groupedSubjects = computed<FamilySubjectGroup[]>(() => {
  const list = publishedResults.value || [];
  const map = new Map<string, FamilySubjectGroup>();

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

// Modal state for print preview
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
  <div class="space-y-6" data-testid="family-academic-tab">
    <!-- Header & Filter Row -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 bg-white rounded-xl border border-slate-200 shadow-xs">
      <div>
        <h3 class="text-sm font-bold text-slate-900 leading-tight">
          Academic Progress &amp; Report Cards
        </h3>
        <p class="text-xs text-slate-500 mt-0.5">
          Follow your child's academic development, review teacher remarks, and download official Buku Rapor.
        </p>
      </div>

      <div class="flex items-center gap-2.5 w-full sm:w-auto">
        <label for="family-period-select" class="text-xs font-medium text-slate-600 shrink-0">
          Period:
        </label>
        <Select
          id="family-period-select"
          v-model="selectedPeriodId"
          :options="academicPeriods"
          option-label="label"
          option-value="value"
          class="w-full sm:w-64 !text-xs"
          data-testid="family-select-period"
        />
        <Button
          type="button"
          rounded
          text
          severity="secondary"
          size="small"
          class="!p-2 text-slate-500 hover:text-slate-800"
          title="Refresh Progress"
          data-testid="btn-family-refresh"
          @click="handleRefreshAll"
        >
          <RefreshCw class="w-4 h-4" />
        </Button>
      </div>
    </div>

    <!-- Error State -->
    <div
      v-if="isResultsError || isReportCardsError"
      class="p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 flex items-start gap-3"
      data-testid="family-academic-error"
    >
      <AlertTriangle class="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
      <div>
        <h4 class="text-sm font-semibold text-rose-950">Unable to load child's academic records</h4>
        <p class="text-xs text-rose-700 mt-0.5">
          {{ resultsError instanceof Error ? resultsError.message : 'An error occurred querying academic records.' }}
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
      data-testid="family-academic-loading"
    >
      <LoadingSpinner size="lg" message="Loading academic progress and report cards..." />
    </div>

    <template v-else>
      <!-- Section 1: Official Certified Report Cards (Buku Rapor Merdeka) -->
      <div class="space-y-3" data-testid="family-report-cards-section">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <Award class="w-4 h-4 text-teal-700" />
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
              Official Buku Rapor Merdeka
            </h4>
          </div>
          <span class="text-xs text-slate-500 font-medium">
            {{ reportCards.length }} Certified Report(s)
          </span>
        </div>

        <div
          v-if="reportCards.length > 0"
          class="grid grid-cols-1 md:grid-cols-2 gap-4"
          data-testid="family-report-cards-grid"
        >
          <div
            v-for="card in reportCards"
            :key="card.id"
            class="p-5 rounded-xl bg-white border border-slate-200 hover:border-teal-300 shadow-xs transition-all space-y-4"
            :data-testid="`family-report-card-${card.id}`"
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

            <!-- Homeroom Teacher Comments Preview -->
            <div
              v-if="card.narrativeComments"
              class="text-xs text-slate-700 bg-slate-50 p-3 rounded-lg border border-slate-200/90 italic leading-relaxed"
            >
              "{{ card.narrativeComments }}"
            </div>

            <!-- Footer Action -->
            <div class="pt-2 border-t border-slate-100 flex items-center justify-between gap-2">
              <span class="text-[11px] text-slate-500">
                Certified by <strong>{{ card.approvedBy || 'Academic Authority' }}</strong>
              </span>

              <Button
                type="button"
                label="View &amp; Print Buku Rapor"
                size="small"
                class="!text-xs !bg-teal-700 hover:!bg-teal-800 text-white font-medium cursor-pointer"
                :data-testid="`btn-print-buku-rapor-${card.id}`"
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
          data-testid="family-no-report-cards"
        >
          <Award class="w-8 h-8 text-slate-300 mx-auto" />
          <p class="text-xs font-semibold text-slate-700">No certified Buku Rapor published yet for this period</p>
          <p class="text-[11px] text-slate-500 max-w-md mx-auto">
            Official term report cards will appear here once approved by the school leadership at the end of the term.
          </p>
        </div>
      </div>

      <!-- Section 2: Child's Published Academic Progress & Teacher Feedback -->
      <div class="space-y-4 pt-4 border-t border-slate-200" data-testid="family-progress-section">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <BookOpen class="w-4 h-4 text-teal-700" />
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
              Academic Progress &amp; Teacher Remarks
            </h4>
          </div>
          <span class="text-xs text-slate-500">
            {{ groupedSubjects.length }} Subject(s) Active
          </span>
        </div>

        <!-- Empty state when no progress records -->
        <div
          v-if="groupedSubjects.length === 0"
          class="py-12 bg-white rounded-xl border border-slate-200"
          data-testid="family-no-progress-empty"
        >
          <EmptyState
            title="No Academic Progress Recorded"
            description="Your child has no published evaluation records for this academic term yet."
          />
        </div>

        <!-- Subjects Progress List -->
        <div v-else class="space-y-4" data-testid="family-subjects-list">
          <div
            v-for="group in groupedSubjects"
            :key="group.learningUnitId"
            class="bg-white border border-slate-200 rounded-xl shadow-xs overflow-hidden"
            :data-testid="`family-subject-group-${group.learningUnitId}`"
          >
            <!-- Subject Header -->
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

              <!-- Subject Average Score -->
              <div class="flex items-center gap-3">
                <span
                  v-if="group.averageScore !== null && group.averageScore !== undefined"
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-teal-50 border border-teal-200 text-xs font-bold text-teal-800"
                  data-testid="family-subject-avg-score"
                >
                  Average: {{ group.averageScore }}
                </span>
                <span class="text-xs text-slate-500">
                  {{ group.results.length }} task(s) evaluated
                </span>
              </div>
            </div>

            <!-- Subject Assessment Line Items -->
            <div class="divide-y divide-slate-100">
              <div
                v-for="res in group.results"
                :key="res.resultId"
                class="p-4 sm:p-5 hover:bg-slate-50/50 transition-colors space-y-3"
                :data-testid="`family-result-item-${res.resultId}`"
              >
                <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                  <div class="space-y-1">
                    <div class="flex items-center gap-2">
                      <span class="font-bold text-sm text-slate-900">
                        {{ res.assessmentTitle }}
                      </span>
                      <Tag
                        :value="res.assessmentType"
                        :severity="res.assessmentType === 'formative' ? 'info' : 'warn'"
                        class="!text-[10px] !py-0.2 uppercase"
                      />
                    </div>
                    <div class="text-xs text-slate-500">
                      Evaluated on {{ formatDisplayDate(res.evaluatedDate) }}
                    </div>
                  </div>

                  <!-- Score & Badges -->
                  <div class="flex items-center gap-3 self-start sm:self-auto">
                    <div
                      v-if="res.scoreNumeric !== null && res.scoreNumeric !== undefined"
                      class="text-right"
                    >
                      <span class="font-bold text-base text-slate-900">
                        {{ res.scoreNumeric }}
                      </span>
                      <span v-if="res.scoreMax" class="text-xs text-slate-500">
                        / {{ res.scoreMax }}
                      </span>
                    </div>

                    <div
                      v-if="res.gradeLetter"
                      class="w-8 h-8 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center font-bold text-sm text-teal-800"
                    >
                      {{ res.gradeLetter }}
                    </div>

                    <span
                      v-if="res.gradeLabel"
                      class="px-2 py-0.5 rounded bg-slate-100 text-slate-700 text-xs font-semibold"
                    >
                      {{ res.gradeLabel }}
                    </span>
                  </div>
                </div>

                <!-- Teacher Feedback Remarks (Highlighted for Parent) -->
                <div
                  v-if="res.narrativeFeedback"
                  class="flex items-start gap-2.5 p-3 rounded-lg bg-teal-50/60 border border-teal-200 text-xs text-teal-950"
                  data-testid="family-teacher-remark"
                >
                  <MessageSquareQuote class="w-4 h-4 text-teal-700 shrink-0 mt-0.5" />
                  <div class="leading-relaxed">
                    <strong class="font-semibold text-teal-900">Teacher Remarks:</strong>
                    {{ res.narrativeFeedback }}
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
