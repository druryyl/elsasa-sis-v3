<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import {
  Layers,
  Search,
  Calendar,
  Award,
} from 'lucide-vue-next';
import {
  PageHeader,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import {
  useLearningStructures,
  useLearningUnits,
} from '../../curriculum/composables/useCurriculum';
import { useUnitParticipationsQuery } from '../../participation/composables/useAcademicParticipation';
import {
  useAssessmentsQuery,
  useConductAssessmentMutation,
} from '../composables/useAssessments';
import { useAcademicResultsQuery } from '../composables/useAcademicResults';
import { ASSESSMENT_PERMISSIONS } from '../constants/permissions';
import type { Assessment } from '../types';
import AssessmentStatusBadge from '../components/AssessmentStatusBadge.vue';
import AssessmentPlannerModal from '../components/AssessmentPlannerModal.vue';
import EvidenceIntakeModal from '../components/EvidenceIntakeModal.vue';
import AssessmentScoringGrid, {
  type EnrolledStudent,
  type ResultStatusFilterValue,
} from '../components/AssessmentScoringGrid.vue';
import AssessmentDetailDrawer from '../components/AssessmentDetailDrawer.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Coursework & Assessment Workbench', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canManage = computed(() =>
  authStore.hasPermission(ASSESSMENT_PERMISSIONS.ASSESSMENTS_MANAGE) ||
  authStore.hasPermission('academics.curriculum.manage') ||
  authStore.user?.roles?.includes('Teacher') ||
  authStore.user?.roles?.includes('Admin'),
);

const canConduct = computed(() =>
  authStore.hasPermission(ASSESSMENT_PERMISSIONS.ASSESSMENTS_CONDUCT) ||
  authStore.user?.roles?.includes('Teacher') ||
  authStore.user?.roles?.includes('Admin'),
);

const canEvaluate = computed(() =>
  authStore.hasPermission(ASSESSMENT_PERMISSIONS.RESULTS_EVALUATE) ||
  authStore.user?.roles?.includes('Teacher') ||
  authStore.user?.roles?.includes('Admin'),
);

// ---------------------------------------------------------------------------
// 3. Curriculum Structures & Units Queries
// ---------------------------------------------------------------------------
const unitSearch = ref('');
const statusFilter = ref<string>('all');
const resultStatusFilter = ref<ResultStatusFilterValue>('all');
const typeFilter = ref<string>('all');
const selectedUnitId = ref<string | null>(null);
const selectedAssessmentId = ref<string | null>(null);

const resultStatusValues = new Set<ResultStatusFilterValue>([
  'provisional',
  'approved',
  'published',
  'unscored',
  'failing',
]);
const assessmentStatusValues = new Set(['all', 'planned', 'conducted', 'cancelled']);

useRouteQueryHydration((query) => {
  const unitId = getQueryString(query, 'unitId');
  if (unitId) {
    selectedUnitId.value = unitId;
  }

  const status = getQueryString(query, 'status')?.toLowerCase();
  if (status) {
    if (resultStatusValues.has(status as ResultStatusFilterValue)) {
      resultStatusFilter.value = status as ResultStatusFilterValue;
    } else if (assessmentStatusValues.has(status)) {
      statusFilter.value = status;
    }
  }
});

const { data: structuresData } = useLearningStructures();

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

const filteredUnits = computed(() => {
  if (!unitsData.value) return [];
  if (!unitSearch.value.trim()) return unitsData.value;
  const term = unitSearch.value.trim().toLowerCase();
  return unitsData.value.filter(
    (u) =>
      u.name.toLowerCase().includes(term) ||
      u.code.toLowerCase().includes(term) ||
      u.unitType.toLowerCase().includes(term),
  );
});

// Auto-select first unit
watch(
  () => unitsData.value,
  (units) => {
    if (units && units.length > 0 && !selectedUnitId.value) {
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
// 4. Assessments Query for Selected Unit
// ---------------------------------------------------------------------------
const {
  data: assessmentsData,
  isLoading: isLoadingAssessments,
  refetch: refetchAssessments,
} = useAssessmentsQuery(selectedUnitId, undefined, {
  enabled: computed(() => Boolean(selectedUnitId.value)),
});

const assessmentsList = computed<Assessment[]>(() => assessmentsData.value ?? []);

const filteredAssessments = computed(() => {
  let list = assessmentsList.value;

  if (statusFilter.value !== 'all') {
    list = list.filter((a) => a.status?.toLowerCase() === statusFilter.value.toLowerCase());
  }

  if (typeFilter.value !== 'all') {
    list = list.filter((a) => a.assessmentType?.toLowerCase() === typeFilter.value.toLowerCase());
  }

  return list;
});

// Auto-select first assessment
watch(
  () => filteredAssessments.value,
  (items) => {
    if (items && items.length > 0) {
      if (
        !selectedAssessmentId.value ||
        !items.some((a) => a.id === selectedAssessmentId.value)
      ) {
        selectedAssessmentId.value = items[0].id;
      }
    } else {
      selectedAssessmentId.value = null;
    }
  },
  { immediate: true },
);

const selectedAssessment = computed<Assessment | null>(() => {
  if (!selectedAssessmentId.value || !assessmentsList.value) return null;
  return assessmentsList.value.find((a) => a.id === selectedAssessmentId.value) ?? null;
});

// ---------------------------------------------------------------------------
// 5. Results & Roster Queries for Selected Assessment
// ---------------------------------------------------------------------------
const {
  data: resultsData,
  isLoading: isLoadingResults,
  refetch: refetchResults,
} = useAcademicResultsQuery(selectedAssessmentId, undefined, {
  enabled: computed(() => Boolean(selectedAssessmentId.value)),
});

const resultsList = computed(() => resultsData.value ?? []);

// Query participations under unit to populate roster
const { data: participationsData } = useUnitParticipationsQuery(selectedUnitId, {
  enabled: computed(() => Boolean(selectedUnitId.value)),
});

// Consolidate enrolled students from participations and results
const enrolledStudents = computed<EnrolledStudent[]>(() => {
  const map = new Map<string, EnrolledStudent>();

  // 1. Add students from participations
  const participations = participationsData.value ?? [];
  for (const p of participations) {
    if (p.studentId) {
      map.set(p.studentId, {
        id: p.studentId,
        name: p.studentName ?? 'Enrolled Student',
        studentNumber: p.studentNumber ?? null,
      });
    }
  }

  // 2. Add any candidates from results
  for (const r of resultsList.value) {
    const key = r.studentId ?? r.applicantId;
    if (key && !map.has(key)) {
      map.set(key, {
        id: r.studentId ?? key,
        applicantId: r.applicantId ?? null,
        name: r.candidateName ?? (r.applicantId ? `Applicant ${r.applicantId.slice(0, 8)}` : `Student ${key.slice(0, 8)}`),
        studentNumber: r.studentNumber ?? null,
      });
    }
  }

  return Array.from(map.values());
});

// ---------------------------------------------------------------------------
// 6. Modals & Drawers State
// ---------------------------------------------------------------------------
const isPlannerModalOpen = ref(false);
const editingAssessment = ref<Assessment | null>(null);

const isEvidenceModalOpen = ref(false);
const evidencePreselectedStudentId = ref<string | null>(null);

const isDetailDrawerOpen = ref(false);

function openCreatePlannerModal() {
  editingAssessment.value = null;
  isPlannerModalOpen.value = true;
}

function openEditPlannerModal(assessment: Assessment) {
  editingAssessment.value = assessment;
  isPlannerModalOpen.value = true;
}

function openEvidenceModalForStudent(studentId?: string) {
  evidencePreselectedStudentId.value = studentId ?? null;
  isEvidenceModalOpen.value = true;
}

function onAssessmentSaved() {
  refetchAssessments();
}

function onEvidenceSaved() {
  refetchAssessments();
  refetchResults();
}

function onEvaluationSaved() {
  refetchResults();
  refetchAssessments();
}

// Conduct Quick Action
const conductMutation = useConductAssessmentMutation();

async function handleQuickConduct(assessment: Assessment) {
  try {
    await conductMutation.mutateAsync({
      id: assessment.id,
      request: {
        assessmentDate: new Date().toISOString().slice(0, 10),
      },
    });
    refetchAssessments();
  } catch {
    // Handled by toast
  }
}
</script>

<template>
  <div class="space-y-6 pb-12" data-testid="assessment-workbench-view">
    <!-- 1. Page Header -->
    <PageHeader
      title="Coursework & Assessment Workbench"
      subtitle="Teacher assessment planning, evidence collection, rubric evaluation matrix, and autonomous formative release (SCR-ACD-02, UC-ACD-05, UC-ACD-06)"
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <!-- Switch to Coursework Tab Button -->
          <Button
            type="button"
            label="Assignments Desk"
            icon="pi pi-book"
            severity="secondary"
            variant="outlined"
            size="small"
            data-testid="switch-to-coursework-btn"
            @click="router.push('/academics/coursework')"
          />

          <!-- Plan Assessment Button -->
          <Button
            type="button"
            label="Plan Assessment"
            icon="pi pi-plus"
            size="small"
            :disabled="!canManage || !selectedUnitId"
            data-testid="header-plan-assessment-btn"
            @click="openCreatePlannerModal"
          />
        </div>
      </template>
    </PageHeader>

    <!-- Subheader Navigation / Tab Switcher (SCR-ACD-02 Unified Workbench) -->
    <div class="flex items-center gap-2 border-b border-slate-200 pb-2">
      <button
        type="button"
        class="text-xs font-medium px-3 py-1.5 rounded-lg border border-transparent text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-colors cursor-pointer"
        @click="router.push('/academics/coursework')"
      >
        Coursework Assignments (UC-ACD-04)
      </button>
      <button
        type="button"
        class="text-xs font-semibold px-3 py-1.5 rounded-lg bg-teal-600 text-white shadow-xs cursor-default"
      >
        Assessment Planning & Rubric Scoring (UC-ACD-05, UC-ACD-06)
      </button>
    </div>

    <!-- 2. Main Workbench Layout Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
      <!-- ================================================================= -->
      <!-- Left Pane: Learning Units & Assessments List (cols 1-4)           -->
      <!-- ================================================================= -->
      <aside class="lg:col-span-4 space-y-4">
        <!-- Learning Units Panel -->
        <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-4 space-y-3">
          <div class="flex items-center justify-between">
            <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
              <Layers class="w-4 h-4 text-teal-600" />
              <span>Learning Units</span>
            </h3>
            <span class="text-[11px] text-slate-400 font-mono">{{ filteredUnits.length }}</span>
          </div>

          <!-- Unit Search Filter -->
          <div class="relative">
            <Search class="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <InputText
              v-model="unitSearch"
              placeholder="Search units..."
              class="w-full pl-8 text-xs"
              data-testid="unit-search-input"
            />
          </div>

          <!-- Units List -->
          <div v-if="isLoadingUnits" class="py-6 flex justify-center">
            <LoadingSpinner size="sm" />
          </div>
          <div
            v-else-if="filteredUnits.length === 0"
            class="py-4 text-center text-xs text-slate-400 italic"
          >
            No learning units found.
          </div>
          <div v-else class="space-y-1 max-h-52 overflow-y-auto pr-1">
            <button
              v-for="unit in filteredUnits"
              :key="unit.id"
              type="button"
              :class="[
                'w-full text-left p-2.5 rounded-lg text-xs transition-colors cursor-pointer border',
                selectedUnitId === unit.id
                  ? 'bg-teal-50/80 border-teal-200 text-teal-950 font-semibold shadow-2xs'
                  : 'bg-white border-transparent text-slate-700 hover:bg-slate-50',
              ]"
              :data-testid="`unit-item-${unit.id}`"
              @click="selectedUnitId = unit.id"
            >
              <div class="flex items-center justify-between">
                <span class="font-mono text-[11px] text-slate-500 uppercase">{{ unit.code }}</span>
                <span class="text-[10px] uppercase font-semibold text-slate-400">{{ unit.unitType }}</span>
              </div>
              <div class="truncate font-medium mt-0.5">{{ unit.name }}</div>
            </button>
          </div>
        </div>

        <!-- Assessments List Panel -->
        <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-4 space-y-3">
          <div class="flex items-center justify-between">
            <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
              <Award class="w-4 h-4 text-teal-600" />
              <span>Assessments</span>
            </h3>
            <span class="text-[11px] text-slate-400 font-mono">{{ filteredAssessments.length }}</span>
          </div>

          <!-- Filters: Status & Type -->
          <div class="grid grid-cols-2 gap-2 text-xs">
            <Select
              v-model="statusFilter"
              :options="[
                { label: 'All Statuses', value: 'all' },
                { label: 'Planned', value: 'planned' },
                { label: 'Conducted', value: 'conducted' },
                { label: 'Cancelled', value: 'cancelled' },
              ]"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="status-filter-select"
            />
            <Select
              v-model="typeFilter"
              :options="[
                { label: 'All Types', value: 'all' },
                { label: 'Formative', value: 'formative' },
                { label: 'Summative', value: 'summative' },
                { label: 'Diagnostic', value: 'diagnostic' },
                { label: 'Project', value: 'project' },
                { label: 'Portfolio', value: 'portfolio' },
              ]"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="type-filter-select"
            />
          </div>

          <!-- Assessments Items List -->
          <div v-if="isLoadingAssessments" class="py-8 flex justify-center">
            <LoadingSpinner size="sm" />
          </div>
          <div
            v-else-if="filteredAssessments.length === 0"
            class="py-6 text-center text-xs text-slate-400 italic"
          >
            No assessments planned under this learning unit.
          </div>
          <div v-else class="space-y-2 max-h-96 overflow-y-auto pr-1">
            <div
              v-for="assessment in filteredAssessments"
              :key="assessment.id"
              :class="[
                'p-3 rounded-xl border text-xs transition-all cursor-pointer space-y-2',
                selectedAssessmentId === assessment.id
                  ? 'bg-teal-50/70 border-teal-300 shadow-2xs'
                  : 'bg-white border-slate-200/80 hover:border-slate-300 hover:bg-slate-50/60',
              ]"
              :data-testid="`assessment-card-${assessment.id}`"
              @click="selectedAssessmentId = assessment.id"
            >
              <div class="flex items-start justify-between gap-2">
                <div class="font-semibold text-slate-900 text-sm leading-tight">
                  {{ assessment.title }}
                </div>
                <AssessmentStatusBadge :status="assessment.status" size="sm" />
              </div>

              <div class="flex items-center gap-2 text-[11px] text-slate-500 flex-wrap">
                <span class="font-medium uppercase text-slate-600 bg-slate-100 px-1.5 py-0.5 rounded">
                  {{ assessment.assessmentType }}
                </span>
                <span>•</span>
                <span class="capitalize">{{ assessment.assessmentMethod.replace(/_/g, ' ') }}</span>
                <span v-if="assessment.maxScore">•</span>
                <span v-if="assessment.maxScore" class="font-medium text-slate-700">
                  Max: {{ assessment.maxScore }}
                </span>
              </div>

              <div class="flex items-center justify-between text-[11px] text-slate-400 pt-1 border-t border-slate-100">
                <span class="flex items-center gap-1">
                  <Calendar class="w-3 h-3" />
                  {{ assessment.assessmentDate ? formatDisplayDate(assessment.assessmentDate) : 'Not scheduled' }}
                </span>
                <span class="font-medium text-slate-600">
                  {{ assessment.resultCount }} scored
                </span>
              </div>
            </div>
          </div>
        </div>
      </aside>

      <!-- ================================================================= -->
      <!-- Right Pane: Assessment Detail & Scoring Grid (cols 5-12)          -->
      <!-- ================================================================= -->
      <main class="lg:col-span-8 space-y-4">
        <div v-if="selectedAssessment" class="space-y-4">
          <!-- Selected Assessment Header Card -->
          <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-5 space-y-3">
            <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
              <div class="space-y-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <AssessmentStatusBadge :status="selectedAssessment.status" size="sm" />
                  <span class="text-xs font-semibold uppercase tracking-wider text-teal-700 bg-teal-50 px-2 py-0.5 rounded-full border border-teal-200">
                    {{ selectedAssessment.assessmentType }}
                  </span>
                  <span class="text-xs text-slate-500 capitalize">
                    Method: {{ selectedAssessment.assessmentMethod.replace(/_/g, ' ') }}
                  </span>
                </div>
                <h2 class="text-lg font-bold text-slate-900">
                  {{ selectedAssessment.title }}
                </h2>
                <div class="text-xs text-slate-500 flex items-center gap-2">
                  <Layers class="w-3.5 h-3.5 text-teal-600" />
                  <span>{{ selectedUnit?.code }} — {{ selectedUnit?.name }}</span>
                </div>
              </div>

              <!-- Action Buttons -->
              <div class="flex items-center gap-2 flex-wrap">
                <!-- Conduct Action (if planned) -->
                <Button
                  v-if="selectedAssessment.status === 'planned'"
                  type="button"
                  label="Conduct"
                  icon="pi pi-play"
                  severity="success"
                  size="small"
                  :disabled="!canConduct"
                  data-testid="assessment-conduct-btn"
                  @click="handleQuickConduct(selectedAssessment)"
                />

                <!-- Record Evidence Button -->
                <Button
                  type="button"
                  label="Evidence Intake"
                  icon="pi pi-paperclip"
                  severity="secondary"
                  variant="outlined"
                  size="small"
                  data-testid="assessment-evidence-btn"
                  @click="openEvidenceModalForStudent()"
                />

                <!-- Inspect Details Drawer Trigger -->
                <Button
                  type="button"
                  label="Inspect"
                  icon="pi pi-info-circle"
                  severity="secondary"
                  variant="outlined"
                  size="small"
                  data-testid="assessment-inspect-btn"
                  @click="isDetailDrawerOpen = true"
                />

                <!-- Edit Button -->
                <Button
                  v-if="selectedAssessment.status !== 'cancelled'"
                  type="button"
                  icon="pi pi-pencil"
                  severity="secondary"
                  variant="outlined"
                  size="small"
                  :disabled="!canManage"
                  title="Edit Assessment"
                  data-testid="assessment-edit-btn"
                  @click="openEditPlannerModal(selectedAssessment)"
                />
              </div>
            </div>

            <!-- Rubric Criteria Snapshot -->
            <div
              v-if="selectedAssessment.criteria"
              class="text-xs text-slate-600 bg-slate-50 border border-slate-100 rounded-lg p-2.5 line-clamp-2"
            >
              <span class="font-semibold text-slate-800">Rubric Criteria:</span>
              {{ selectedAssessment.criteria }}
            </div>
          </div>

          <!-- Scoring Grid Component -->
          <AssessmentScoringGrid
            :assessment="selectedAssessment"
            :enrolled-students="enrolledStudents"
            :results="resultsList"
            :is-loading="isLoadingResults"
            :can-evaluate="canEvaluate"
            :initial-status-filter="resultStatusFilter"
            @record-evidence="openEvidenceModalForStudent"
            @evaluated="onEvaluationSaved"
          />
        </div>

        <!-- No Assessment Selected Empty State -->
        <div
          v-else
          class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-12 text-center"
        >
          <EmptyState
            title="No Assessment Selected"
            message="Select an assessment from the left panel or plan a new evaluation task to begin rubric scoring."
          >
            <template #actions>
              <Button
                v-if="selectedUnitId"
                type="button"
                label="Plan Assessment"
                icon="pi pi-plus"
                size="small"
                :disabled="!canManage"
                @click="openCreatePlannerModal"
              />
            </template>
          </EmptyState>
        </div>
      </main>
    </div>

    <!-- 3. Modals & Drawers -->
    <!-- Planner Modal -->
    <AssessmentPlannerModal
      v-model:visible="isPlannerModalOpen"
      :assessment="editingAssessment"
      :learning-unit-id="selectedUnitId"
      :learning-unit-title="selectedUnit ? `${selectedUnit.code} — ${selectedUnit.name}` : null"
      @saved="onAssessmentSaved"
    />

    <!-- Evidence Intake Modal -->
    <EvidenceIntakeModal
      v-if="selectedAssessment"
      v-model:visible="isEvidenceModalOpen"
      :assessment-id="selectedAssessment.id"
      :assessment-title="selectedAssessment.title"
      :enrolled-students="enrolledStudents"
      :preselected-student-id="evidencePreselectedStudentId"
      @saved="onEvidenceSaved"
    />

    <!-- Detail Drawer -->
    <AssessmentDetailDrawer
      v-model:visible="isDetailDrawerOpen"
      :assessment="selectedAssessment"
      :can-manage="canManage"
      :can-conduct="canConduct"
      @edit="openEditPlannerModal"
      @record-evidence="openEvidenceModalForStudent"
      @conducted="refetchAssessments"
      @cancelled="refetchAssessments"
      @open-scoring="isDetailDrawerOpen = false"
    />
  </div>
</template>
