<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Dialog from 'primevue/dialog';
import ToggleSwitch from 'primevue/toggleswitch';
import {
  Search,
  Award,
  CheckCircle2,
  AlertTriangle,
  History,
  Sparkles,
  Scale,
} from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  AcademicResult,
  Assessment,
  EvaluationModel,
} from '../types';
import ResultStatusBadge from './ResultStatusBadge.vue';
import {
  useEvaluateLearningMutation,
  usePublishAcademicResultsMutation,
} from '../composables/useAcademicResults';

export interface EnrolledStudent {
  id: string;
  name: string;
  studentNumber?: string | null;
  applicantId?: string | null;
}

interface Props {
  assessment: Assessment;
  enrolledStudents: EnrolledStudent[];
  results: AcademicResult[];
  isLoading?: boolean;
  canEvaluate?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  isLoading: false,
  canEvaluate: true,
});

const emit = defineEmits<{
  (e: 'recordEvidence', studentId: string): void;
  (e: 'evaluated'): void;
}>();

// Search and Filter State
const searchQuery = ref('');
const statusFilter = ref<'all' | 'provisional' | 'approved' | 'published' | 'unscored' | 'failing'>('all');

// Evaluation Modal State
const isScoreModalOpen = ref(false);
const activeStudent = ref<EnrolledStudent | null>(null);
const selectedEvaluationModel = ref<EvaluationModel>('numeric');
const inputScoreNumeric = ref<number | null>(null);
const inputGradeLetter = ref('');
const inputGradeLabel = ref('');
const inputNarrativeFeedback = ref('');
const togglePublishImmediately = ref(props.assessment.assessmentType === 'formative');
const scoringNotes = ref('');

// Attempt History Inspection Modal
const isHistoryModalOpen = ref(false);
const historyStudent = ref<EnrolledStudent | null>(null);

// Group results by candidate ID (studentId or applicantId)
const resultsByCandidate = computed<Map<string, AcademicResult[]>>(() => {
  const map = new Map<string, AcademicResult[]>();
  for (const res of props.results) {
    const key = res.studentId ?? res.applicantId ?? '';
    if (!key) continue;
    const existing = map.get(key) ?? [];
    existing.push(res);
    // Sort attempts descending so attempt 1 is earliest, attempt N is latest
    existing.sort((a, b) => a.attemptNumber - b.attemptNumber);
    map.set(key, existing);
  }
  return map;
});

// Candidate Row Structure
export interface CandidateRosterRow {
  student: EnrolledStudent;
  candidateKey: string;
  attempts: AcademicResult[];
  operativeResult: AcademicResult | null;
  attemptCount: number;
  hasScore: boolean;
  isPassing: boolean | null;
  status: string;
}

const rosterRows = computed<CandidateRosterRow[]>(() => {
  return props.enrolledStudents.map((student) => {
    const candidateKey = student.id || student.applicantId || '';
    const attempts = resultsByCandidate.value.get(candidateKey) ?? [];
    // The operative result is the one with isFinal = true; fallback to latest
    const operative = attempts.find((a) => a.isFinal) ?? attempts[attempts.length - 1] ?? null;
    const hasScore = operative != null;

    let isPassing: boolean | null = null;
    if (hasScore && props.assessment.passingScore != null && operative.scoreNumeric != null) {
      isPassing = operative.scoreNumeric >= props.assessment.passingScore;
    }

    return {
      student,
      candidateKey,
      attempts,
      operativeResult: operative,
      attemptCount: attempts.length,
      hasScore,
      isPassing,
      status: operative?.resultStatus ?? 'unscored',
    };
  });
});

// Filtered Rows
const filteredRows = computed(() => {
  return rosterRows.value.filter((row) => {
    // Search matching
    if (searchQuery.value.trim()) {
      const q = searchQuery.value.trim().toLowerCase();
      const nameMatch = row.student.name.toLowerCase().includes(q);
      const numMatch = (row.student.studentNumber ?? '').toLowerCase().includes(q);
      if (!nameMatch && !numMatch) return false;
    }

    // Status filter
    if (statusFilter.value === 'unscored') {
      return !row.hasScore;
    }
    if (statusFilter.value === 'failing') {
      return row.isPassing === false;
    }
    if (statusFilter.value !== 'all') {
      return row.operativeResult?.resultStatus === statusFilter.value;
    }
    return true;
  });
});

// Summary Statistics
const summaryStats = computed(() => {
  const total = rosterRows.value.length;
  const scored = rosterRows.value.filter((r) => r.hasScore).length;
  const passing = rosterRows.value.filter((r) => r.isPassing === true).length;
  const failing = rosterRows.value.filter((r) => r.isPassing === false).length;

  const numericScores = rosterRows.value
    .map((r) => r.operativeResult?.scoreNumeric)
    .filter((s): s is number => s != null && !isNaN(s));

  const averageScore =
    numericScores.length > 0
      ? Math.round((numericScores.reduce((acc, v) => acc + v, 0) / numericScores.length) * 10) / 10
      : null;

  const passRate =
    scored > 0 && props.assessment.passingScore != null
      ? Math.round((passing / scored) * 100)
      : null;

  return { total, scored, passing, failing, averageScore, passRate };
});

// Evaluation Mutations
const evaluateMutation = useEvaluateLearningMutation();
const publishMutation = usePublishAcademicResultsMutation();

// Open Scoring Modal
function openScoreModal(student: EnrolledStudent) {
  activeStudent.value = student;
  const candidateKey = student.id || student.applicantId || '';
  const attempts = resultsByCandidate.value.get(candidateKey) ?? [];
  const operative = attempts.find((a) => a.isFinal) ?? attempts[attempts.length - 1];

  if (operative) {
    selectedEvaluationModel.value = operative.evaluationModel ?? 'numeric';
    inputScoreNumeric.value = operative.scoreNumeric ?? null;
    inputGradeLetter.value = operative.gradeLetter ?? '';
    inputGradeLabel.value = operative.gradeLabel ?? '';
    inputNarrativeFeedback.value = operative.narrativeFeedback ?? '';
  } else {
    selectedEvaluationModel.value = 'numeric';
    inputScoreNumeric.value = null;
    inputGradeLetter.value = '';
    inputGradeLabel.value = '';
    inputNarrativeFeedback.value = '';
  }

  togglePublishImmediately.value = props.assessment.assessmentType === 'formative';
  scoringNotes.value = '';
  isScoreModalOpen.value = true;
}

// Open History Modal
function openHistoryModal(student: EnrolledStudent) {
  historyStudent.value = student;
  isHistoryModalOpen.value = true;
}

// Submit Evaluation
async function handleSaveEvaluation() {
  if (!activeStudent.value) return;

  const candidateKey = activeStudent.value.id || activeStudent.value.applicantId;
  const isStudent = Boolean(activeStudent.value.id);

  try {
    await evaluateMutation.mutateAsync({
      assessmentId: props.assessment.id,
      request: {
        studentId: isStudent ? candidateKey : null,
        applicantId: !isStudent ? candidateKey : null,
        scoreNumeric: inputScoreNumeric.value != null ? Number(inputScoreNumeric.value) : null,
        scoreMax: props.assessment.maxScore ?? 100,
        gradeLetter: inputGradeLetter.value ? inputGradeLetter.value.trim() : null,
        gradeLabel: inputGradeLabel.value ? inputGradeLabel.value.trim() : null,
        narrativeFeedback: inputNarrativeFeedback.value ? inputNarrativeFeedback.value.trim() : null,
        evaluationModel: selectedEvaluationModel.value,
        publishImmediately: togglePublishImmediately.value,
        notes: scoringNotes.value ? scoringNotes.value.trim() : null,
      },
    });

    isScoreModalOpen.value = false;
    emit('evaluated');
  } catch {
    // Handled by toast
  }
}

// Autonomous Formative Quick Release
async function handleQuickPublishFormative(resultId: string) {
  try {
    await publishMutation.mutateAsync({
      assessmentId: props.assessment.id,
      request: {
        resultIds: [resultId],
      },
    });
    emit('evaluated');
  } catch {
    // Handled by toast
  }
}

const evaluationModelOptions: Array<{ label: string; value: EvaluationModel }> = [
  { label: 'Numeric Score (0 - 100)', value: 'numeric' },
  { label: 'Letter Grade (A, B, C...)', value: 'letter_grade' },
  { label: 'Pass / Fail', value: 'pass_fail' },
  { label: 'Competency Based', value: 'competency' },
  { label: 'Rubric Criteria Band', value: 'rubric' },
  { label: 'Narrative Feedback Only', value: 'narrative' },
  { label: 'Completion Check', value: 'completion' },
];

const gradeLetterOptions = [
  { label: 'A (Distinction / Sangat Baik)', value: 'A' },
  { label: 'B (Proficient / Baik)', value: 'B' },
  { label: 'C (Developing / Cukup)', value: 'C' },
  { label: 'D (Below Basic / Kurang)', value: 'D' },
  { label: 'F (Failing / Tidak Tuntas)', value: 'F' },
];
</script>

<template>
  <div class="space-y-4" data-testid="assessment-scoring-grid">
    <!-- 1. Header Banner & Metrics Summary Bar -->
    <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-4 space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <div class="flex items-center gap-2">
            <h3 class="text-base font-semibold text-slate-900">
              Evaluation Matrix & Rubric Scoring
            </h3>
            <span
              :class="[
                'text-[11px] font-semibold uppercase px-2 py-0.5 rounded-full border',
                assessment.assessmentType === 'formative'
                  ? 'bg-teal-50 text-teal-700 border-teal-200'
                  : 'bg-indigo-50 text-indigo-700 border-indigo-200',
              ]"
            >
              {{ assessment.assessmentType }}
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Evaluate enrolled student work against criteria.
            <span v-if="assessment.assessmentType === 'formative'" class="font-medium text-teal-700">
              Teachers possess autonomous authority to publish formative feedback directly (UC-ACD-06).
            </span>
            <span v-else class="font-medium text-indigo-700">
              Summative marks remain provisional until certified by Academic Authority (UC-ACD-16).
            </span>
          </p>
        </div>

        <div class="flex items-center gap-2">
          <!-- Reassessment Policy Tag -->
          <div
            class="text-xs bg-slate-100 text-slate-700 rounded-lg px-2.5 py-1.5 border border-slate-200/80 flex items-center gap-1.5"
            title="Reassessment policy governing multi-attempt retakes"
          >
            <History class="w-3.5 h-3.5 text-slate-500" />
            <span class="text-slate-500">Retake Policy:</span>
            <span class="font-medium capitalize text-slate-900">
              {{ assessment.reassessmentPolicy.replace(/_/g, ' ') }}
            </span>
          </div>

          <!-- KKM Badge -->
          <div
            v-if="assessment.passingScore != null"
            class="text-xs bg-amber-50 text-amber-800 rounded-lg px-2.5 py-1.5 border border-amber-200/80 flex items-center gap-1.5 font-medium"
          >
            <Award class="w-3.5 h-3.5 text-amber-600" />
            <span>KKM Threshold: {{ assessment.passingScore }}</span>
          </div>
        </div>
      </div>

      <!-- Key Performance Metrics Strip -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-3 border-t border-slate-100">
        <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
          <div class="text-[11px] font-medium text-slate-500 uppercase tracking-wide">
            Enrolled Cohort
          </div>
          <div class="text-lg font-bold text-slate-900 mt-0.5">
            {{ summaryStats.total }}
          </div>
        </div>

        <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
          <div class="text-[11px] font-medium text-slate-500 uppercase tracking-wide">
            Evaluated
          </div>
          <div class="text-lg font-bold text-teal-700 mt-0.5">
            {{ summaryStats.scored }} / {{ summaryStats.total }}
          </div>
        </div>

        <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
          <div class="text-[11px] font-medium text-slate-500 uppercase tracking-wide">
            Average Score
          </div>
          <div class="text-lg font-bold text-slate-900 mt-0.5">
            {{ summaryStats.averageScore != null ? summaryStats.averageScore : '—' }}
            <span v-if="assessment.maxScore" class="text-xs font-normal text-slate-400">
              / {{ assessment.maxScore }}
            </span>
          </div>
        </div>

        <div class="p-2.5 rounded-lg bg-slate-50 border border-slate-100">
          <div class="text-[11px] font-medium text-slate-500 uppercase tracking-wide">
            KKM Pass Rate
          </div>
          <div
            :class="[
              'text-lg font-bold mt-0.5',
              summaryStats.passRate != null && summaryStats.passRate >= 75
                ? 'text-emerald-700'
                : 'text-amber-700',
            ]"
          >
            {{ summaryStats.passRate != null ? `${summaryStats.passRate}%` : '—' }}
          </div>
        </div>
      </div>
    </div>

    <!-- 2. Controls Bar: Search & Status Filters -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white p-3 rounded-xl border border-slate-200/80 shadow-xs">
      <div class="relative w-full sm:w-72">
        <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
        <InputText
          v-model="searchQuery"
          placeholder="Search student or number..."
          class="w-full pl-9 text-xs"
          data-testid="scoring-search-input"
        />
      </div>

      <div class="flex items-center gap-1.5 flex-wrap">
        <button
          type="button"
          :class="[
            'text-xs font-medium px-2.5 py-1 rounded-lg border transition-colors',
            statusFilter === 'all'
              ? 'bg-slate-900 text-white border-slate-900'
              : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50',
          ]"
          @click="statusFilter = 'all'"
        >
          All ({{ rosterRows.length }})
        </button>
        <button
          type="button"
          :class="[
            'text-xs font-medium px-2.5 py-1 rounded-lg border transition-colors',
            statusFilter === 'unscored'
              ? 'bg-slate-900 text-white border-slate-900'
              : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50',
          ]"
          @click="statusFilter = 'unscored'"
        >
          Unscored ({{ summaryStats.total - summaryStats.scored }})
        </button>
        <button
          type="button"
          :class="[
            'text-xs font-medium px-2.5 py-1 rounded-lg border transition-colors',
            statusFilter === 'provisional'
              ? 'bg-amber-600 text-white border-amber-600'
              : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50',
          ]"
          @click="statusFilter = 'provisional'"
        >
          Provisional
        </button>
        <button
          type="button"
          :class="[
            'text-xs font-medium px-2.5 py-1 rounded-lg border transition-colors',
            statusFilter === 'published'
              ? 'bg-teal-600 text-white border-teal-600'
              : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50',
          ]"
          @click="statusFilter = 'published'"
        >
          Published
        </button>
        <button
          v-if="assessment.passingScore != null"
          type="button"
          :class="[
            'text-xs font-medium px-2.5 py-1 rounded-lg border transition-colors',
            statusFilter === 'failing'
              ? 'bg-rose-600 text-white border-rose-600'
              : 'bg-white text-rose-700 border-rose-200 hover:bg-rose-50',
          ]"
          @click="statusFilter = 'failing'"
        >
          Remedial ({{ summaryStats.failing }})
        </button>
      </div>
    </div>

    <!-- 3. Interactive Evaluation Table -->
    <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs overflow-hidden">
      <div v-if="filteredRows.length === 0" class="p-8">
        <EmptyState
          title="No Candidates Found"
          message="No candidates match your current search or status filter."
        />
      </div>

      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-slate-50/80 border-b border-slate-200/80 text-slate-600 uppercase font-semibold text-[11px] tracking-wider">
              <th class="py-3 px-4">Student / Candidate</th>
              <th class="py-3 px-4">Attempts & Lineage</th>
              <th class="py-3 px-4">Operative Result</th>
              <th class="py-3 px-4">Status</th>
              <th class="py-3 px-4">Feedback / Rubric Notes</th>
              <th class="py-3 px-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr
              v-for="row in filteredRows"
              :key="row.candidateKey"
              class="hover:bg-slate-50/60 transition-colors"
              :data-testid="`scoring-row-${row.candidateKey}`"
            >
              <!-- Candidate Identity -->
              <td class="py-3 px-4">
                <div class="font-medium text-slate-900 text-sm">
                  {{ row.student.name }}
                </div>
                <div class="text-slate-400 text-[11px] font-mono mt-0.5">
                  {{ row.student.studentNumber ?? row.student.applicantId ?? row.student.id }}
                </div>
              </td>

              <!-- Attempt Lineage -->
              <td class="py-3 px-4">
                <div v-if="row.attemptCount > 0" class="space-y-1">
                  <div class="flex items-center gap-1.5">
                    <span class="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 border border-slate-200">
                      Attempt #{{ row.operativeResult?.attemptNumber ?? row.attemptCount }}
                    </span>
                    <button
                      v-if="row.attemptCount > 1"
                      type="button"
                      class="text-[11px] text-teal-600 hover:text-teal-700 underline font-medium cursor-pointer"
                      @click="openHistoryModal(row.student)"
                    >
                      View all {{ row.attemptCount }}
                    </button>
                  </div>
                  <div v-if="row.attemptCount > 1" class="text-[10px] text-slate-400">
                    Remediation history preserved (TD-03)
                  </div>
                </div>
                <span v-else class="text-slate-400 text-xs italic">
                  Not attempted
                </span>
              </td>

              <!-- Current Operative Score Badge -->
              <td class="py-3 px-4">
                <div v-if="row.operativeResult" class="space-y-1">
                  <!-- Operative Score Value -->
                  <div class="flex items-center gap-1.5">
                    <!-- Numeric Score -->
                    <span
                      v-if="row.operativeResult.scoreNumeric != null"
                      class="text-sm font-bold text-slate-900"
                    >
                      {{ row.operativeResult.scoreNumeric }}
                      <span v-if="assessment.maxScore" class="text-xs font-normal text-slate-400">
                        / {{ assessment.maxScore }}
                      </span>
                    </span>

                    <!-- Letter Grade -->
                    <span
                      v-if="row.operativeResult.gradeLetter"
                      class="text-xs font-bold px-1.5 py-0.5 rounded bg-slate-800 text-white"
                    >
                      {{ row.operativeResult.gradeLetter }}
                    </span>

                    <!-- Grade Label -->
                    <span
                      v-if="row.operativeResult.gradeLabel"
                      class="text-xs font-medium text-slate-600"
                    >
                      ({{ row.operativeResult.gradeLabel }})
                    </span>

                    <!-- Final Flag Badge -->
                    <span
                      v-if="row.operativeResult.isFinal"
                      class="text-[10px] font-semibold text-emerald-700 bg-emerald-50 border border-emerald-200 px-1.5 py-0.2 rounded"
                      title="Operative final mark adjudicated by policy evaluator"
                    >
                      Operative
                    </span>
                  </div>

                  <!-- KKM Threshold Indicator -->
                  <div v-if="row.isPassing !== null">
                    <span
                      v-if="row.isPassing"
                      class="inline-flex items-center gap-1 text-[10px] font-semibold text-emerald-700"
                    >
                      <CheckCircle2 class="w-3 h-3 text-emerald-500" />
                      <span>Meets Passing Standard (KKM)</span>
                    </span>
                    <span
                      v-else
                      class="inline-flex items-center gap-1 text-[10px] font-semibold text-rose-700"
                    >
                      <AlertTriangle class="w-3 h-3 text-rose-500" />
                      <span>Below KKM (Remediation Needed)</span>
                    </span>
                  </div>
                </div>
                <div v-else class="text-slate-400 text-xs italic">
                  Pending evaluation
                </div>
              </td>

              <!-- Status Badge -->
              <td class="py-3 px-4">
                <ResultStatusBadge
                  v-if="row.operativeResult"
                  :status="row.operativeResult.resultStatus"
                  size="sm"
                />
                <span
                  v-else
                  class="text-[11px] font-medium text-slate-400 bg-slate-100 px-2 py-0.5 rounded-full"
                >
                  Unscored
                </span>
              </td>

              <!-- Feedback / Rubric Notes -->
              <td class="py-3 px-4 max-w-xs">
                <div
                  v-if="row.operativeResult?.narrativeFeedback"
                  class="text-xs text-slate-600 line-clamp-2"
                  :title="row.operativeResult.narrativeFeedback"
                >
                  "{{ row.operativeResult.narrativeFeedback }}"
                </div>
                <span v-else class="text-slate-400 text-xs italic">
                  No qualitative feedback
                </span>
              </td>

              <!-- Actions -->
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <!-- Autonomous Formative Quick-Publish Action -->
                  <Button
                    v-if="
                      assessment.assessmentType === 'formative' &&
                      row.operativeResult &&
                      row.operativeResult.resultStatus === 'provisional'
                    "
                    type="button"
                    label="Release"
                    icon="pi pi-send"
                    severity="success"
                    variant="outlined"
                    size="small"
                    title="Teacher Autonomous Formative Release (UC-ACD-06)"
                    :data-testid="`quick-release-btn-${row.candidateKey}`"
                    @click="handleQuickPublishFormative(row.operativeResult.id)"
                  />

                  <!-- Record Evidence Quick Trigger -->
                  <Button
                    type="button"
                    icon="pi pi-paperclip"
                    severity="secondary"
                    variant="text"
                    size="small"
                    title="Record raw student evidence"
                    :data-testid="`record-evidence-btn-${row.candidateKey}`"
                    @click="emit('recordEvidence', row.student.id)"
                  />

                  <!-- Score / Record Attempt Button -->
                  <Button
                    type="button"
                    :label="row.hasScore ? 'Score Retake' : 'Score'"
                    icon="pi pi-pencil"
                    size="small"
                    :severity="row.hasScore ? 'secondary' : 'primary'"
                    :disabled="!canEvaluate"
                    :data-testid="`score-student-btn-${row.candidateKey}`"
                    @click="openScoreModal(row.student)"
                  />
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 4. Score Input Modal -->
    <Dialog
      v-model:visible="isScoreModalOpen"
      modal
      :header="`Evaluate Learning: ${activeStudent?.name ?? ''}`"
      class="w-full max-w-lg"
      :dismissable-mask="true"
      data-testid="score-input-modal"
    >
      <form class="space-y-4 pt-1" @submit.prevent="handleSaveEvaluation">
        <!-- Candidate Context -->
        <div class="bg-slate-50 border border-slate-200 rounded-lg p-3 text-xs space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-slate-500">Student:</span>
            <span class="font-semibold text-slate-900">{{ activeStudent?.name }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-slate-500">Task:</span>
            <span class="font-medium text-slate-800">{{ assessment.title }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-slate-500">Reassessment Policy:</span>
            <span class="font-medium capitalize text-teal-700">
              {{ assessment.reassessmentPolicy.replace(/_/g, ' ') }}
            </span>
          </div>
        </div>

        <!-- Evaluation Model -->
        <div class="space-y-1">
          <label class="block text-xs font-medium text-slate-700">Evaluation Model</label>
          <Select
            v-model="selectedEvaluationModel"
            :options="evaluationModelOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="scoring-model-select"
          />
        </div>

        <!-- Numeric Score Input -->
        <div
          v-if="selectedEvaluationModel === 'numeric' || selectedEvaluationModel === 'rubric'"
          class="space-y-1"
        >
          <div class="flex items-center justify-between">
            <label class="block text-xs font-medium text-slate-700">
              Numeric Score (0 - {{ assessment.maxScore ?? 100 }})
            </label>
            <span
              v-if="assessment.passingScore != null"
              class="text-[11px] text-amber-700 font-medium"
            >
              Passing Threshold (KKM): {{ assessment.passingScore }}
            </span>
          </div>
          <div class="relative">
            <Scale class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="inputScoreNumeric"
              type="number"
              placeholder="e.g. 85"
              class="w-full pl-9"
              data-testid="scoring-numeric-input"
            />
          </div>
        </div>

        <!-- Letter Grade Input -->
        <div
          v-if="selectedEvaluationModel === 'letter_grade' || selectedEvaluationModel === 'rubric'"
          class="space-y-1"
        >
          <label class="block text-xs font-medium text-slate-700">Letter Grade</label>
          <Select
            v-model="inputGradeLetter"
            :options="gradeLetterOptions"
            option-label="label"
            option-value="value"
            placeholder="Select letter grade"
            class="w-full text-xs"
            data-testid="scoring-letter-select"
          />
        </div>

        <!-- Grade Label Input -->
        <div
          v-if="selectedEvaluationModel === 'competency' || selectedEvaluationModel === 'pass_fail'"
          class="space-y-1"
        >
          <label class="block text-xs font-medium text-slate-700">Competency Descriptor / Label</label>
          <InputText
            v-model="inputGradeLabel"
            placeholder="e.g. Memenuhi Syarat, Sangat Memuaskan"
            class="w-full"
            data-testid="scoring-label-input"
          />
        </div>

        <!-- Qualitative Narrative Feedback -->
        <div class="space-y-1">
          <label class="block text-xs font-medium text-slate-700">
            Rubric & Formative Narrative Feedback
          </label>
          <Textarea
            v-model="inputNarrativeFeedback"
            rows="3"
            placeholder="Actionable developmental feedback for student learning progression..."
            class="w-full text-sm"
            data-testid="scoring-feedback-textarea"
          />
        </div>

        <!-- Release Formative Feedback Toggle Button (UC-ACD-06, GAP-005) -->
        <div class="bg-teal-50/70 border border-teal-200/80 rounded-xl p-3 space-y-2">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <Sparkles class="w-4 h-4 text-teal-600" />
              <div>
                <div class="text-xs font-semibold text-teal-950">
                  Release Formative Feedback Immediately
                </div>
                <div class="text-[11px] text-teal-700">
                  Autonomously publishes feedback to student & parent portal without authority gate (UC-ACD-06)
                </div>
              </div>
            </div>
            <ToggleSwitch
              v-model="togglePublishImmediately"
              data-testid="scoring-publish-toggle"
            />
          </div>
          <div
            v-if="togglePublishImmediately"
            class="text-[10px] text-teal-800 bg-teal-100/60 rounded px-2 py-1 font-mono"
          >
            ✓ Result status will transition to 'published' upon save.
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isScoreModalOpen = false"
          />
          <Button
            type="submit"
            label="Record Evaluation"
            icon="pi pi-check"
            size="small"
            :loading="evaluateMutation.isPending.value"
            data-testid="scoring-save-btn"
          />
        </div>
      </form>
    </Dialog>

    <!-- 5. Attempt Lineage History Modal -->
    <Dialog
      v-model:visible="isHistoryModalOpen"
      modal
      :header="`Evaluation History & Retakes: ${historyStudent?.name ?? ''}`"
      class="w-full max-w-md"
      :dismissable-mask="true"
      data-testid="attempt-history-modal"
    >
      <div v-if="historyStudent" class="space-y-3 pt-1">
        <p class="text-xs text-slate-500">
          Full remediation history preserved for regulatory audit (GAP-004, TD-03):
        </p>

        <div class="space-y-2">
          <div
            v-for="attempt in resultsByCandidate.get(historyStudent.id || historyStudent.applicantId || '') ?? []"
            :key="attempt.id"
            :class="[
              'p-3 rounded-xl border text-xs space-y-1',
              attempt.isFinal
                ? 'bg-emerald-50/60 border-emerald-200'
                : 'bg-slate-50 border-slate-200 opacity-80',
            ]"
          >
            <div class="flex items-center justify-between">
              <span class="font-bold text-slate-900">
                Attempt #{{ attempt.attemptNumber }}
              </span>
              <span
                v-if="attempt.isFinal"
                class="text-[10px] font-bold uppercase tracking-wider text-emerald-800 bg-emerald-100 px-2 py-0.5 rounded-full"
              >
                Winning Operative Mark
              </span>
              <span
                v-else
                class="text-[10px] text-slate-400 bg-slate-200 px-2 py-0.5 rounded-full"
              >
                Historical Retake
              </span>
            </div>

            <div class="flex items-center gap-2 text-slate-700">
              <span v-if="attempt.scoreNumeric != null" class="font-bold text-sm">
                Score: {{ attempt.scoreNumeric }}
              </span>
              <span v-if="attempt.gradeLetter" class="font-bold">
                ({{ attempt.gradeLetter }})
              </span>
              <span class="text-slate-400">•</span>
              <span class="text-slate-500">{{ formatDisplayDate(attempt.evaluatedDate) }}</span>
            </div>

            <div v-if="attempt.narrativeFeedback" class="text-slate-600 italic">
              "{{ attempt.narrativeFeedback }}"
            </div>
          </div>
        </div>

        <div class="pt-3 border-t border-slate-100 text-right">
          <Button
            type="button"
            label="Close"
            size="small"
            severity="secondary"
            variant="outlined"
            @click="isHistoryModalOpen = false"
          />
        </div>
      </div>
    </Dialog>
  </div>
</template>
