<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  GraduationCap,
  Calendar,
  User,
  MessageSquareQuote,
  AlertTriangle,
  RefreshCw,
  Sparkles,
} from 'lucide-vue-next';
import {
  LoadingSpinner,
} from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import {
  useApplicantAssessment,
} from '../../composables/useApplicantAssessment';
import type { ApplicantEntranceAssessmentScoreDto } from '../../api/applicantAssessmentApi';

interface Props {
  applicantId: string;
  applicationId?: string;
  applicantName?: string;
  scores?: ApplicantEntranceAssessmentScoreDto[];
}

const props = withDefaults(defineProps<Props>(), {
  applicationId: undefined,
  applicantName: 'Applicant',
  scores: undefined,
});

const {
  scores: queriedScores,
  isLoading,
  isError,
  error,
  refetch,
} = useApplicantAssessment(computed(() => props.applicantId));

// Effective scores: props override (for testing or direct passing) or queried scores
const effectiveScores = computed<ApplicantEntranceAssessmentScoreDto[]>(() => {
  if (props.scores !== undefined) {
    return props.scores;
  }
  return queriedScores.value;
});

// Summary metrics
const totalExams = computed(() => effectiveScores.value.length);

const averageScore = computed<number | null>(() => {
  const numericScores = effectiveScores.value
    .map((s) => s.scoreNumeric)
    .filter((s): s is number => typeof s === 'number');

  if (numericScores.length === 0) return null;
  const avg = numericScores.reduce((acc, curr) => acc + curr, 0) / numericScores.length;
  return Math.round(avg * 10) / 10;
});

const highestScore = computed<number | null>(() => {
  const numericScores = effectiveScores.value
    .map((s) => s.scoreNumeric)
    .filter((s): s is number => typeof s === 'number');

  if (numericScores.length === 0) return null;
  return Math.max(...numericScores);
});
</script>

<template>
  <div class="space-y-4" data-testid="applicant-assessment-tab">
    <!-- Header Card -->
    <div class="border border-slate-200 rounded-lg p-4 bg-white shadow-2xs space-y-3">
      <div class="flex items-center justify-between border-b border-slate-100 pb-2.5">
        <div class="flex items-center gap-2">
          <GraduationCap class="w-4 h-4 text-teal-600" />
          <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
            Entrance Assessment Diagnostic Results ({{ totalExams }})
          </h3>
        </div>

        <div class="flex items-center gap-2">
          <Tag
            value="Academics Module Sync"
            severity="info"
            class="!text-[10px] !py-0.5"
          />
          <Button
            type="button"
            rounded
            text
            severity="secondary"
            size="small"
            class="!p-1.5 text-slate-400 hover:text-slate-600 cursor-pointer"
            title="Refresh Diagnostic Scores"
            data-testid="btn-refresh-entrance-scores"
            @click="() => refetch()"
          >
            <RefreshCw class="w-3.5 h-3.5" />
          </Button>
        </div>
      </div>

      <!-- Quick Metrics Bar (Shown when results exist) -->
      <div
        v-if="effectiveScores.length > 0"
        class="grid grid-cols-3 gap-3 p-3 bg-slate-50 border border-slate-200/80 rounded-lg text-center"
        data-testid="entrance-assessment-metrics"
      >
        <div>
          <div class="text-[10px] font-semibold text-slate-500 uppercase">Evaluations</div>
          <div class="text-base font-bold text-slate-900 mt-0.5">{{ totalExams }}</div>
        </div>
        <div>
          <div class="text-[10px] font-semibold text-slate-500 uppercase">Average Score</div>
          <div class="text-base font-bold text-teal-800 mt-0.5">
            {{ averageScore !== null ? averageScore : '-' }}
          </div>
        </div>
        <div>
          <div class="text-[10px] font-semibold text-slate-500 uppercase">Highest Score</div>
          <div class="text-base font-bold text-emerald-700 mt-0.5">
            {{ highestScore !== null ? highestScore : '-' }}
          </div>
        </div>
      </div>

      <!-- Error State -->
      <div
        v-if="isError && !props.scores"
        class="p-3 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 flex items-start gap-2.5 text-xs"
        data-testid="entrance-assessment-error"
      >
        <AlertTriangle class="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
        <div>
          <span class="font-semibold">Unable to query diagnostic scores from Academics:</span>
          <span class="ml-1">{{ error instanceof Error ? error.message : 'Connection error' }}</span>
        </div>
      </div>

      <!-- Loading State -->
      <div
        v-if="isLoading && !props.scores"
        class="py-8 flex justify-center"
        data-testid="entrance-assessment-loading"
      >
        <LoadingSpinner size="md" message="Loading diagnostic entrance assessment results..." />
      </div>

      <!-- Diagnostic Results List -->
      <div
        v-else-if="effectiveScores.length > 0"
        class="space-y-3 pt-1"
        data-testid="entrance-assessment-list"
      >
        <div
          v-for="score in effectiveScores"
          :key="score.resultId || score.assessmentId"
          class="p-4 rounded-lg border border-slate-200/90 bg-slate-50/70 hover:border-teal-300 transition-colors space-y-3"
          :data-testid="`entrance-assessment-card-${score.resultId || score.assessmentId}`"
        >
          <!-- Card Header Row -->
          <div class="flex items-start justify-between gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="font-bold text-slate-900 text-sm">
                  {{ score.assessmentTitle }}
                </span>
                <Tag
                  :value="score.assessmentType || 'Diagnostic'"
                  severity="info"
                  class="!text-[10px] !py-0.2 uppercase"
                />
                <Tag
                  v-if="score.resultStatus"
                  :value="score.resultStatus"
                  :severity="score.resultStatus === 'published' ? 'success' : score.resultStatus === 'approved' ? 'warn' : 'secondary'"
                  class="!text-[10px] !py-0.2 uppercase"
                />
              </div>
              <div class="flex flex-wrap items-center gap-3 text-xs text-slate-500">
                <span class="inline-flex items-center gap-1">
                  <User class="w-3.5 h-3.5 text-slate-400" />
                  Assessor: <strong class="text-slate-700 font-medium">{{ score.evaluatedBy }}</strong>
                </span>
                <span class="inline-flex items-center gap-1">
                  <Calendar class="w-3.5 h-3.5 text-slate-400" />
                  Evaluated: {{ formatDisplayDate(score.evaluatedDate) }}
                </span>
              </div>
            </div>

            <!-- Score Numeric & Grade Badges -->
            <div class="flex items-center gap-2.5 self-start shrink-0">
              <!-- Score Numeric -->
              <div
                v-if="score.scoreNumeric !== null && score.scoreNumeric !== undefined"
                class="text-right"
              >
                <span
                  class="font-mono font-bold text-base text-slate-900"
                  data-testid="entrance-score-numeric"
                >
                  {{ score.scoreNumeric }}
                </span>
                <span v-if="score.scoreMax" class="text-xs text-slate-500 font-medium">
                  / {{ score.scoreMax }}
                </span>
              </div>

              <!-- Grade Letter -->
              <div
                v-if="score.gradeLetter"
                class="w-8 h-8 rounded-lg bg-teal-100 text-teal-800 border border-teal-200 flex items-center justify-center font-bold text-sm"
                data-testid="entrance-grade-letter"
              >
                {{ score.gradeLetter }}
              </div>

              <!-- Grade Label -->
              <span
                v-if="score.gradeLabel"
                class="px-2 py-0.5 rounded bg-slate-200 text-slate-700 text-xs font-semibold"
                data-testid="entrance-grade-label"
              >
                {{ score.gradeLabel }}
              </span>
            </div>
          </div>

          <!-- Assessor Comments & Qualitative Observations -->
          <div
            v-if="score.assessorComments || score.notes"
            class="flex items-start gap-2.5 p-3 rounded-lg bg-white border border-slate-200/80 text-xs text-slate-800"
            data-testid="entrance-assessor-comments"
          >
            <MessageSquareQuote class="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
            <div class="leading-relaxed">
              <strong class="font-semibold text-slate-900">Assessor Comments:</strong>
              <span class="italic ml-1">"{{ score.assessorComments || score.notes }}"</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div
        v-else
        class="text-center py-8 px-4 border border-dashed border-slate-200 rounded-lg text-slate-500"
        data-testid="no-entrance-assessments-empty"
      >
        <Sparkles class="w-8 h-8 text-slate-300 mx-auto mb-2" />
        <p class="text-xs font-semibold text-slate-700">No Academics Diagnostic Exam Results Recorded</p>
        <p class="text-[11px] text-slate-400 mt-0.5 max-w-sm mx-auto">
          Diagnostic entrance assessments administered by the Academics department will be automatically synchronized here for admission committee review.
        </p>
      </div>
    </div>
  </div>
</template>
