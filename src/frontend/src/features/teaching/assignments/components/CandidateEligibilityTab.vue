<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import {
  UserCheck,
  ShieldCheck,
  Award,
  GraduationCap,
  CalendarCheck2,
  Gauge,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  ArrowRight,
  Sparkles,
  Info,
  Clock,
  Building2,
  Layers,
  History,
  RotateCcw,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner, FormField } from '@/shared/components';
import {
  useDemands,
  useEvaluations,
  useEvaluateCandidateMutation,
} from '../composables/useAssignments';
import {
  ELIGIBILITY_STATUS_MAP,
  DEFAULT_FACULTY_CANDIDATES,
} from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingDemandDto,
  TeacherEligibilityEvaluationDto,
  TeacherEligibilityStatus,
  CandidateEvaluationSummaryDto,
} from '../types';

export interface CandidateEligibilityTabProps {
  academicPeriodId?: string;
  campusScope?: string;
  preselectedDemandId?: string | null;
  canManage?: boolean;
}

const props = withDefaults(defineProps<CandidateEligibilityTabProps>(), {
  academicPeriodId: undefined,
  campusScope: undefined,
  preselectedDemandId: null,
  canManage: true,
});

const emit = defineEmits<{
  (e: 'proposeAssignment', data: {
    demand: TeachingDemandDto;
    teacherProfileId: string;
    evaluation: TeacherEligibilityEvaluationDto;
  }): void;
}>();

// Demands query for selector
const { data: demandsData, isLoading: isLoadingDemands } = useDemands(
  computed(() => ({
    academicPeriodId: props.academicPeriodId,
    campusScope: props.campusScope && props.campusScope !== 'all' ? props.campusScope : undefined,
  })),
);

const demandsList = computed<TeachingDemandDto[]>(() => {
  if (!demandsData.value) return [];
  return Array.isArray(demandsData.value)
    ? demandsData.value
    : (demandsData.value as { items?: TeachingDemandDto[] }).items || [];
});

const demandOptions = computed(() => {
  return demandsList.value.map((d) => ({
    label: `${d.demandCode} — ${d.sourceWorkLabel} (${d.requiredCapabilityCode}, ${d.expectedWorkloadHours}h)`,
    value: d.id,
    demand: d,
  }));
});

// Selected demand state
const selectedDemandId = ref<string>('');

watch(
  () => [props.preselectedDemandId, demandOptions.value],
  ([preselected]) => {
    if (preselected && typeof preselected === 'string') {
      selectedDemandId.value = preselected;
    } else if (!selectedDemandId.value && demandOptions.value.length > 0) {
      // Pick first accepted demand or first demand
      const accepted = demandOptions.value.find((opt) => opt.demand.status === 'accepted_for_staffing');
      selectedDemandId.value = accepted ? accepted.value : demandOptions.value[0].value;
    }
  },
  { immediate: true },
);

const selectedDemand = computed<TeachingDemandDto | null>(() => {
  if (!selectedDemandId.value) return null;
  const match = demandOptions.value.find((opt) => opt.value === selectedDemandId.value);
  return match ? match.demand : null;
});

// Candidate selector state
const facultyCandidateOptions = computed(() => {
  return DEFAULT_FACULTY_CANDIDATES.map((f) => ({
    label: `${f.fullName} (${f.teacherNumber}) — ${f.primaryCapability} [${f.campusAffiliation}]`,
    value: f.id,
    faculty: f,
  }));
});

const selectedTeacherId = ref<string>(DEFAULT_FACULTY_CANDIDATES[0].id);
const coordinatorRemarks = ref<string>('');

// Evaluation result state
const latestEvaluation = ref<TeacherEligibilityEvaluationDto | null>(null);

// Query evaluations history for selected demand
const {
  data: evaluationsData,
  isLoading: isLoadingEvaluations,
  refetch: refetchEvaluations,
} = useEvaluations(
  computed(() => ({
    teachingDemandId: selectedDemandId.value || undefined,
  })),
  {
    enabled: computed(() => Boolean(selectedDemandId.value)),
  },
);

const evaluationsHistory = computed<CandidateEvaluationSummaryDto[]>(() => {
  if (!evaluationsData.value) return [];
  return Array.isArray(evaluationsData.value)
    ? evaluationsData.value
    : (evaluationsData.value as { items?: CandidateEvaluationSummaryDto[] }).items || [];
});

// Mutation
const evaluateMutation = useEvaluateCandidateMutation({
  onSuccess: (response) => {
    latestEvaluation.value = response;
    refetchEvaluations();
  },
});

function handleRunEvaluation() {
  if (!selectedDemandId.value || !selectedTeacherId.value) return;

  evaluateMutation.mutate({
    teachingDemandId: selectedDemandId.value,
    teacherProfileId: selectedTeacherId.value,
    coordinatorRemarks: coordinatorRemarks.value.trim() || undefined,
  });
}

function handlePropose() {
  if (!selectedDemand.value || !selectedTeacherId.value || !latestEvaluation.value) return;
  emit('proposeAssignment', {
    demand: selectedDemand.value,
    teacherProfileId: selectedTeacherId.value,
    evaluation: latestEvaluation.value,
  });
}

const currentEligibilityStatusMeta = computed(() => {
  if (!latestEvaluation.value) return null;
  return (
    ELIGIBILITY_STATUS_MAP[latestEvaluation.value.eligibilityStatus as TeacherEligibilityStatus] || {
      label: latestEvaluation.value.eligibilityStatus,
      variant: 'slate' as const,
      colorClass: 'text-slate-700 bg-slate-50 border-slate-200',
      description: '',
    }
  );
});
</script>

<template>
  <div class="space-y-6" data-testid="candidate-eligibility-tab">
    <!-- Top Step: Demand Selector Banner -->
    <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 shadow-xs space-y-3">
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-3">
        <div class="flex items-center gap-2">
          <div class="w-8 h-8 rounded-lg bg-primary-100 dark:bg-primary-950 flex items-center justify-center text-primary-600 dark:text-primary-400">
            <Layers class="w-4 h-4" />
          </div>
          <div>
            <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100">
              1. Select Target Teaching Demand
            </h3>
            <p class="text-xs text-surface-500 dark:text-surface-400">
              Choose the capacity demand to staff and evaluate eligible faculty candidates against.
            </p>
          </div>
        </div>

        <!-- Demand Select Dropdown -->
        <div class="w-full md:w-96">
          <Select
            v-model="selectedDemandId"
            :options="demandOptions"
            option-label="label"
            option-value="value"
            placeholder="Select a teaching demand..."
            class="w-full !text-xs"
            data-testid="select-target-demand"
          />
        </div>
      </div>

      <!-- Demand Summary Chip -->
      <div
        v-if="selectedDemand"
        class="bg-surface-50 dark:bg-surface-800/40 border border-surface-200 dark:border-surface-700 rounded-lg p-3 text-xs grid grid-cols-2 sm:grid-cols-4 gap-3"
        data-testid="selected-demand-summary"
      >
        <div>
          <span class="text-surface-500 dark:text-surface-400 block text-[11px]">Subject / Scope</span>
          <span class="font-medium text-surface-900 dark:text-surface-100">{{ selectedDemand.sourceWorkLabel }}</span>
        </div>
        <div>
          <span class="text-surface-500 dark:text-surface-400 block text-[11px]">Required Capability</span>
          <span class="font-mono text-primary-600 dark:text-primary-400 font-medium">{{ selectedDemand.requiredCapabilityCode }}</span>
        </div>
        <div>
          <span class="text-surface-500 dark:text-surface-400 block text-[11px]">Expected Workload</span>
          <span class="font-medium text-surface-900 dark:text-surface-100 flex items-center gap-1">
            <Clock class="w-3 h-3 text-amber-500" />
            {{ selectedDemand.expectedWorkloadHours }} {{ selectedDemand.workloadUnit || 'Hours' }}
          </span>
        </div>
        <div>
          <span class="text-surface-500 dark:text-surface-400 block text-[11px]">Campus & Period</span>
          <span class="font-medium text-surface-900 dark:text-surface-100 flex items-center gap-1">
            <Building2 class="w-3 h-3 text-surface-400" />
            {{ selectedDemand.campusScope }}
          </span>
        </div>
      </div>
    </div>

    <!-- Middle Step: Candidate Evaluation Form -->
    <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-5 shadow-xs space-y-4">
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-lg bg-teal-100 dark:bg-teal-950 flex items-center justify-center text-teal-600 dark:text-teal-400">
          <UserCheck class="w-4 h-4" />
        </div>
        <div>
          <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100">
            2. Evaluate Candidate Faculty
          </h3>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Execute the automated 5-check institutional policy evaluation engine (standing, authorization, qualification, availability, capacity).
          </p>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Faculty Selector -->
        <FormField label="Candidate Faculty Member" name="candidateFaculty" required>
          <Select
            v-model="selectedTeacherId"
            :options="facultyCandidateOptions"
            option-label="label"
            option-value="value"
            placeholder="Select teacher candidate..."
            class="w-full !text-xs"
            data-testid="select-candidate-faculty"
          />
        </FormField>

        <!-- Coordinator Remarks -->
        <FormField label="Coordinator Remarks / Context (Optional)" name="coordinatorRemarks">
          <Textarea
            v-model="coordinatorRemarks"
            rows="1"
            placeholder="e.g. Recommended based on previous high student satisfaction scores..."
            class="w-full !text-xs"
            data-testid="input-coordinator-remarks"
          />
        </FormField>
      </div>

      <div class="flex justify-end pt-1">
        <Button
          severity="primary"
          size="small"
          class="!text-xs !py-2 !px-4 font-medium"
          :loading="evaluateMutation.isPending.value"
          :disabled="!selectedDemandId || !selectedTeacherId"
          data-testid="btn-run-evaluation"
          @click="handleRunEvaluation"
        >
          <Sparkles class="w-4 h-4 mr-1.5" />
          Run Eligibility Evaluation Engine
        </Button>
      </div>
    </div>

    <!-- Bottom Step: Visual 5-Check Breakdown Cards (TD-03) -->
    <div
      v-if="latestEvaluation"
      class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-5 shadow-xs space-y-4"
      data-testid="evaluation-results-card"
    >
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-surface-200 dark:border-surface-700 pb-3">
        <div class="flex items-center gap-2">
          <div class="w-8 h-8 rounded-lg bg-sky-100 dark:bg-sky-950 flex items-center justify-center text-sky-600 dark:text-sky-400">
            <ShieldCheck class="w-4 h-4" />
          </div>
          <div>
            <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100 flex items-center gap-2">
              <span>3. Policy Evaluation Breakdown</span>
              <StatusPill
                v-if="currentEligibilityStatusMeta"
                :status="latestEvaluation.eligibilityStatus"
                :label="currentEligibilityStatusMeta.label"
                :variant="currentEligibilityStatusMeta.variant"
                size="sm"
                data-testid="overall-eligibility-pill"
              />
            </h3>
            <p class="text-xs text-surface-500 dark:text-surface-400">
              Evaluated on {{ latestEvaluation.evaluationDate }} by {{ latestEvaluation.evaluatedBy }}
            </p>
          </div>
        </div>

        <!-- Propose Assignment Action Shortcut -->
        <Button
          v-if="latestEvaluation.isEligible"
          severity="success"
          size="small"
          class="!text-xs !py-2 !px-3 font-medium"
          data-testid="btn-propose-assignment-shortcut"
          @click="handlePropose"
        >
          Propose Assignment
          <ArrowRight class="w-3.5 h-3.5 ml-1.5" />
        </Button>
      </div>

      <!-- 5-Check Cards Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3" data-testid="check-cards-grid">
        <!-- Check 1: Profile Standing -->
        <div
          :class="[
            'border rounded-xl p-3 space-y-1.5 text-xs transition-colors',
            latestEvaluation.profileActiveCheck
              ? 'bg-emerald-50/60 dark:bg-emerald-950/20 border-emerald-200 dark:border-emerald-800 text-emerald-950 dark:text-emerald-200'
              : 'bg-rose-50/60 dark:bg-rose-950/20 border-rose-200 dark:border-rose-800 text-rose-950 dark:text-rose-200',
          ]"
          data-testid="check-card-standing"
        >
          <div class="flex items-center justify-between">
            <span class="font-semibold flex items-center gap-1.5">
              <UserCheck class="w-3.5 h-3.5" />
              1. Standing
            </span>
            <CheckCircle2 v-if="latestEvaluation.profileActiveCheck" class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <XCircle v-else class="w-4 h-4 text-rose-600 dark:text-rose-400" />
          </div>
          <p class="text-[11px] opacity-80 leading-snug">
            {{ latestEvaluation.profileActiveCheck ? 'Active profile in good standing.' : 'Profile is pending, suspended, or inactive.' }}
          </p>
        </div>

        <!-- Check 2: Matching Authorization -->
        <div
          :class="[
            'border rounded-xl p-3 space-y-1.5 text-xs transition-colors',
            latestEvaluation.authorizationCheckPassed
              ? 'bg-emerald-50/60 dark:bg-emerald-950/20 border-emerald-200 dark:border-emerald-800 text-emerald-950 dark:text-emerald-200'
              : 'bg-rose-50/60 dark:bg-rose-950/20 border-rose-200 dark:border-rose-800 text-rose-950 dark:text-rose-200',
          ]"
          data-testid="check-card-authorization"
        >
          <div class="flex items-center justify-between">
            <span class="font-semibold flex items-center gap-1.5">
              <Award class="w-3.5 h-3.5" />
              2. Authorization
            </span>
            <CheckCircle2 v-if="latestEvaluation.authorizationCheckPassed" class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <XCircle v-else class="w-4 h-4 text-rose-600 dark:text-rose-400" />
          </div>
          <p class="text-[11px] opacity-80 leading-snug">
            {{ latestEvaluation.authorizationCheckPassed ? 'Matching active capability and campus scope.' : 'Lacks required teaching authorization.' }}
          </p>
        </div>

        <!-- Check 3: Qualification Evidence -->
        <div
          :class="[
            'border rounded-xl p-3 space-y-1.5 text-xs transition-colors',
            latestEvaluation.qualificationCheckPassed
              ? 'bg-emerald-50/60 dark:bg-emerald-950/20 border-emerald-200 dark:border-emerald-800 text-emerald-950 dark:text-emerald-200'
              : 'bg-rose-50/60 dark:bg-rose-950/20 border-rose-200 dark:border-rose-800 text-rose-950 dark:text-rose-200',
          ]"
          data-testid="check-card-qualification"
        >
          <div class="flex items-center justify-between">
            <span class="font-semibold flex items-center gap-1.5">
              <GraduationCap class="w-3.5 h-3.5" />
              3. Qualification
            </span>
            <CheckCircle2 v-if="latestEvaluation.qualificationCheckPassed" class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <XCircle v-else class="w-4 h-4 text-rose-600 dark:text-rose-400" />
          </div>
          <p class="text-[11px] opacity-80 leading-snug">
            {{ latestEvaluation.qualificationCheckPassed ? 'Verified Serdik / S1 qualification on file.' : 'Missing verified qualification degree.' }}
          </p>
        </div>

        <!-- Check 4: Availability & Schedule -->
        <div
          :class="[
            'border rounded-xl p-3 space-y-1.5 text-xs transition-colors',
            latestEvaluation.availabilityCheckPassed
              ? 'bg-emerald-50/60 dark:bg-emerald-950/20 border-emerald-200 dark:border-emerald-800 text-emerald-950 dark:text-emerald-200'
              : 'bg-rose-50/60 dark:bg-rose-950/20 border-rose-200 dark:border-rose-800 text-rose-950 dark:text-rose-200',
          ]"
          data-testid="check-card-availability"
        >
          <div class="flex items-center justify-between">
            <span class="font-semibold flex items-center gap-1.5">
              <CalendarCheck2 class="w-3.5 h-3.5" />
              4. Availability
            </span>
            <CheckCircle2 v-if="latestEvaluation.availabilityCheckPassed" class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <XCircle v-else class="w-4 h-4 text-rose-600 dark:text-rose-400" />
          </div>
          <p class="text-[11px] opacity-80 leading-snug">
            {{ latestEvaluation.availabilityCheckPassed ? 'Conflict-free schedule, no leave blackout.' : 'Conflict in schedule or leave window recorded.' }}
          </p>
        </div>

        <!-- Check 5: Workload Capacity -->
        <div
          :class="[
            'border rounded-xl p-3 space-y-1.5 text-xs transition-colors',
            latestEvaluation.capacityCheckPassed
              ? 'bg-emerald-50/60 dark:bg-emerald-950/20 border-emerald-200 dark:border-emerald-800 text-emerald-950 dark:text-emerald-200'
              : 'bg-rose-50/60 dark:bg-rose-950/20 border-rose-200 dark:border-rose-800 text-rose-950 dark:text-rose-200',
          ]"
          data-testid="check-card-capacity"
        >
          <div class="flex items-center justify-between">
            <span class="font-semibold flex items-center gap-1.5">
              <Gauge class="w-3.5 h-3.5" />
              5. Capacity
            </span>
            <CheckCircle2 v-if="latestEvaluation.capacityCheckPassed" class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <XCircle v-else class="w-4 h-4 text-rose-600 dark:text-rose-400" />
          </div>
          <p class="text-[11px] opacity-80 leading-snug">
            {{ latestEvaluation.capacityCheckPassed ? 'Sufficient remaining hours within policy limit.' : 'Committed hours exceed limit without exception.' }}
          </p>
        </div>
      </div>

      <!-- Natural Language Explanation Card (TD-03) -->
      <div
        class="bg-surface-50 dark:bg-surface-800/40 border border-surface-200 dark:border-surface-700 rounded-lg p-3.5 text-xs space-y-1"
        data-testid="natural-language-explanation-card"
      >
        <span class="font-semibold text-surface-900 dark:text-surface-100 flex items-center gap-1.5">
          <Info class="w-3.5 h-3.5 text-primary-500" />
          Natural Language Engine Explanation:
        </span>
        <p class="text-surface-700 dark:text-surface-300 italic pl-5 leading-relaxed" data-testid="explanation-text">
          "{{ latestEvaluation.explanation }}"
        </p>
        <p v-if="latestEvaluation.notes" class="text-surface-500 dark:text-surface-400 pl-5 text-[11px] mt-1">
          Remarks: {{ latestEvaluation.notes }}
        </p>
      </div>
    </div>

    <!-- History: Recent Candidate Evaluations for Selected Demand -->
    <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4 shadow-xs space-y-3">
      <div class="flex items-center justify-between">
        <h4 class="text-xs font-semibold text-surface-900 dark:text-surface-100 flex items-center gap-1.5">
          <History class="w-3.5 h-3.5 text-surface-500" />
          Historical Candidate Evaluations for this Demand
        </h4>
        <Button
          severity="secondary"
          variant="text"
          size="small"
          class="!text-[11px]"
          data-testid="refresh-evaluations-btn"
          @click="() => refetchEvaluations()"
        >
          <RotateCcw class="w-3 h-3 mr-1" />
          Refresh History
        </Button>
      </div>

      <div v-if="isLoadingEvaluations" class="py-6 text-center text-xs text-surface-400">
        Loading evaluations history...
      </div>

      <div v-else-if="evaluationsHistory.length === 0" class="py-6 text-center text-xs text-surface-400" data-testid="evaluations-history-empty">
        No candidate evaluations recorded yet for this demand. Run an evaluation above to record the first assessment.
      </div>

      <DataTable
        v-else
        :value="evaluationsHistory"
        responsive-layout="scroll"
        class="text-xs"
        data-testid="evaluations-history-table"
      >
        <Column header="Candidate Faculty" style="min-width: 150px">
          <template #body="{ data }">
            <span class="font-medium text-surface-900 dark:text-surface-100">{{ data.teacherName }}</span>
            <span class="text-[10px] text-surface-400 block font-mono">{{ data.teacherNumber }}</span>
          </template>
        </Column>

        <Column header="Date" style="min-width: 100px">
          <template #body="{ data }">
            <span class="text-surface-600 dark:text-surface-400">{{ data.evaluationDate }}</span>
          </template>
        </Column>

        <Column header="Determination" style="min-width: 130px">
          <template #body="{ data }">
            <StatusPill
              :status="data.eligibilityStatus"
              :label="ELIGIBILITY_STATUS_MAP[data.eligibilityStatus as TeacherEligibilityStatus]?.label || data.eligibilityStatus"
              :variant="ELIGIBILITY_STATUS_MAP[data.eligibilityStatus as TeacherEligibilityStatus]?.variant || 'slate'"
              size="sm"
            />
          </template>
        </Column>

        <Column header="Explanation" style="min-width: 260px">
          <template #body="{ data }">
            <span class="text-surface-600 dark:text-surface-300 text-[11px] line-clamp-2" :title="data.explanation">
              {{ data.explanation }}
            </span>
          </template>
        </Column>

        <Column header="Evaluator" style="min-width: 110px">
          <template #body="{ data }">
            <span class="text-surface-500 text-[11px]">{{ data.evaluatedBy }}</span>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>
