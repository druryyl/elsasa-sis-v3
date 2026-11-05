<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import ProgressBar from 'primevue/progressbar';
import Dialog from 'primevue/dialog';
import {
  BookOpen,
  Calendar,
  Clock,
  Plus,
  Play,
  Lock,
  Search,
  Filter,
  CheckCircle2,
  AlertTriangle,
  FolderOpen,
  Layers,
  Sparkles,
  ShieldAlert,
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
import { COURSEWORK_PERMISSIONS } from '../constants/permissions';
import {
  useUnitAssignmentsQuery,
  useAssignmentSubmissionsQuery,
  useReleaseAssignmentMutation,
  useCloseAssignmentMutation,
} from '../composables/useAssignments';
import {
  useLearningStructures,
  useLearningUnits,
} from '../../curriculum/composables/useCurriculum';
import type {
  Assignment,
  AssignmentStatus,
  AssignmentSubmission,
  CreateAssignmentResponse,
} from '../types';
import CreateAssignmentDrawer from '../components/CreateAssignmentDrawer.vue';
import SubmissionsReviewTable from '../components/SubmissionsReviewTable.vue';
import GrantExceptionModal from '../components/GrantExceptionModal.vue';
import SubmissionDetailDrawer from '../components/SubmissionDetailDrawer.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
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
  authStore.hasPermission(COURSEWORK_PERMISSIONS.ASSIGNMENTS_MANAGE),
);
const canRelease = computed(() =>
  authStore.hasPermission(COURSEWORK_PERMISSIONS.ASSIGNMENTS_RELEASE),
);
const canClose = computed(() =>
  authStore.hasPermission(COURSEWORK_PERMISSIONS.ASSIGNMENTS_CLOSE),
);
const canException = computed(() =>
  authStore.hasPermission(COURSEWORK_PERMISSIONS.SUBMISSIONS_EXCEPTION),
);

// ---------------------------------------------------------------------------
// 3. Selection & Filtering State
// ---------------------------------------------------------------------------
const unitSearch = ref('');
const statusFilter = ref<string>('all');
const selectedUnitId = ref<string | null>(null);
const selectedAssignmentId = ref<string | null>(null);

// Modal & Drawer State
const isCreateDrawerOpen = ref(false);
const isDetailDrawerOpen = ref(false);
const isExceptionModalOpen = ref(false);
const isReleaseConfirmOpen = ref(false);
const isCloseConfirmOpen = ref(false);

const selectedSubmission = ref<AssignmentSubmission | null>(null);
const exceptionInitialStudentId = ref<string | null>(null);

// ---------------------------------------------------------------------------
// 4. Data Queries: Curriculum Structures & Learning Units
// ---------------------------------------------------------------------------
const { data: structuresData, isLoading: isLoadingStructures } = useLearningStructures();

// Pick active or first structure
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
// 5. Data Queries: Unit Assignments
// ---------------------------------------------------------------------------
const {
  data: assignmentsData,
  isLoading: isLoadingAssignments,
  isError: isAssignmentsError,
  refetch: refetchAssignments,
} = useUnitAssignmentsQuery(selectedUnitId, {
  enabled: computed(() => Boolean(selectedUnitId.value)),
});

// Filter assignments by status ('draft' | 'released' | 'closed' | 'all')
const filteredAssignments = computed(() => {
  if (!assignmentsData.value) return [];
  if (statusFilter.value === 'all') return assignmentsData.value;
  return assignmentsData.value.filter(
    (a) => a.status?.toLowerCase() === statusFilter.value.toLowerCase(),
  );
});

// Auto-select first assignment when assignments load or filter changes
watch(
  () => filteredAssignments.value,
  (assignments) => {
    if (assignments && assignments.length > 0) {
      if (
        !selectedAssignmentId.value ||
        !assignments.some((a) => a.id === selectedAssignmentId.value)
      ) {
        selectedAssignmentId.value = assignments[0].id;
      }
    } else {
      selectedAssignmentId.value = null;
    }
  },
  { immediate: true },
);

const selectedAssignment = computed<Assignment | null>(() => {
  if (!selectedAssignmentId.value || !assignmentsData.value) return null;
  return assignmentsData.value.find((a) => a.id === selectedAssignmentId.value) ?? null;
});

// ---------------------------------------------------------------------------
// 6. Data Queries: Submissions for Selected Assignment
// ---------------------------------------------------------------------------
const {
  data: submissionsData,
  isLoading: isLoadingSubmissions,
  isError: isSubmissionsError,
  error: submissionsError,
  refetch: refetchSubmissions,
} = useAssignmentSubmissionsQuery(selectedAssignmentId, {
  enabled: computed(() => Boolean(selectedAssignmentId.value)),
});

const submissionsList = computed<AssignmentSubmission[]>(
  () => submissionsData.value ?? [],
);

// Progress metrics for selected assignment
const progressMetrics = computed(() => {
  const total = submissionsList.value.length;
  if (total === 0) return { total: 0, submitted: 0, percent: 0 };
  const submitted = submissionsList.value.filter(
    (s) =>
      s.displayStatus === 'submitted' ||
      s.displayStatus === 'late' ||
      s.displayStatus === 'excused' ||
      s.displayStatus === 'exempted',
  ).length;
  const percent = Math.round((submitted / total) * 100);
  return { total, submitted, percent };
});

const studentsOptionList = computed(() => {
  return submissionsList.value.map((s) => ({
    studentId: s.studentId,
    studentName: s.studentName,
    studentNumber: s.studentNumber,
  }));
});

// ---------------------------------------------------------------------------
// 7. Mutations: Release & Close Assignment
// ---------------------------------------------------------------------------
const releaseMutation = useReleaseAssignmentMutation();
const closeMutation = useCloseAssignmentMutation();

async function handleConfirmRelease() {
  if (!selectedAssignment.value) return;
  try {
    await releaseMutation.mutateAsync(selectedAssignment.value.id);
    isReleaseConfirmOpen.value = false;
  } catch {
    // Handled by toast
  }
}

async function handleConfirmClose() {
  if (!selectedAssignment.value) return;
  try {
    await closeMutation.mutateAsync(selectedAssignment.value.id);
    isCloseConfirmOpen.value = false;
  } catch {
    // Handled by toast
  }
}

// ---------------------------------------------------------------------------
// 8. Event Handlers for Inspection & Exception
// ---------------------------------------------------------------------------
function openInspectDrawer(submission: AssignmentSubmission) {
  selectedSubmission.value = submission;
  isDetailDrawerOpen.value = true;
}

function openExceptionModal(studentId?: string) {
  exceptionInitialStudentId.value = studentId ?? null;
  isExceptionModalOpen.value = true;
}

function onAssignmentCreated(created: CreateAssignmentResponse) {
  selectedAssignmentId.value = created.id;
  refetchAssignments();
}

function onExceptionSaved() {
  refetchSubmissions();
}
</script>

<template>
  <div class="space-y-6 pb-12" data-testid="coursework-workbench-view">
    <!-- 1. Page Header -->
    <PageHeader
      title="Coursework & Assessment Workbench"
      subtitle="Teacher workbench for managing assignments, monitoring student submissions, and granting policy exceptions (SCR-ACD-02)"
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <!-- Release Button -->
          <Button
            v-if="selectedAssignment?.status === 'draft'"
            type="button"
            label="Release"
            icon="pi pi-send"
            severity="success"
            size="small"
            :disabled="!canRelease"
            data-testid="header-release-btn"
            @click="isReleaseConfirmOpen = true"
          />

          <!-- Close Button -->
          <Button
            v-if="selectedAssignment?.status === 'released'"
            type="button"
            label="Close"
            icon="pi pi-lock"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="!canClose"
            data-testid="header-close-btn"
            @click="isCloseConfirmOpen = true"
          />

          <!-- Create Assignment Button -->
          <Button
            type="button"
            label="Create Assignment"
            icon="pi pi-plus"
            size="small"
            :disabled="!canManage || !selectedUnitId"
            data-testid="header-create-assignment-btn"
            @click="isCreateDrawerOpen = true"
          />
        </div>
      </template>
    </PageHeader>

    <!-- 2. Main Workbench Layout Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
      <!-- ================================================================= -->
      <!-- Left Pane: Learning Units & Status Filter (cols 1-3)               -->
      <!-- ================================================================= -->
      <aside class="lg:col-span-3 space-y-4">
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
              class="w-full !pl-8 text-xs !py-1.5"
              size="small"
              data-testid="unit-search-input"
            />
          </div>

          <!-- Unit List Items -->
          <div v-if="isLoadingUnits" class="py-6 flex justify-center">
            <LoadingSpinner size="sm" label="Loading units..." />
          </div>

          <div
            v-else-if="filteredUnits.length === 0"
            class="text-xs text-slate-500 text-center py-4 italic"
          >
            No learning units found.
          </div>

          <div v-else class="space-y-1 max-h-[300px] overflow-y-auto pr-1">
            <button
              v-for="unit in filteredUnits"
              :key="unit.id"
              type="button"
              class="w-full text-left p-2.5 rounded-lg border transition text-xs flex flex-col gap-0.5 cursor-pointer"
              :class="
                selectedUnitId === unit.id
                  ? 'bg-teal-50/70 border-teal-300 text-teal-900 font-semibold shadow-2xs'
                  : 'bg-white border-transparent hover:bg-slate-50 text-slate-700'
              "
              :data-testid="`unit-item-${unit.id}`"
              @click="selectedUnitId = unit.id"
            >
              <div class="flex items-center justify-between">
                <span class="font-mono text-[11px] text-slate-500">{{ unit.code }}</span>
                <span class="text-[10px] uppercase font-medium px-1.5 py-0.2 rounded bg-slate-100 text-slate-600">
                  {{ unit.unitType }}
                </span>
              </div>
              <span class="truncate font-medium text-slate-900">{{ unit.name }}</span>
            </button>
          </div>
        </div>

        <!-- Status Filter Panel -->
        <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-4 space-y-2.5">
          <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
            <Filter class="w-4 h-4 text-teal-600" />
            <span>Assignment Status</span>
          </h3>

          <div class="space-y-1">
            <button
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-medium transition cursor-pointer"
              :class="statusFilter === 'all' ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-100'"
              @click="statusFilter = 'all'"
            >
              <span>All Assignments</span>
              <span class="text-[11px] opacity-80">{{ assignmentsData?.length ?? 0 }}</span>
            </button>

            <button
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-medium transition cursor-pointer"
              :class="statusFilter === 'draft' ? 'bg-slate-700 text-white' : 'text-slate-600 hover:bg-slate-100'"
              @click="statusFilter = 'draft'"
            >
              <span class="flex items-center gap-1.5">
                <span class="w-2 h-2 rounded-full bg-slate-400"></span>
                <span>Draft</span>
              </span>
              <span class="text-[11px] opacity-80">
                {{ assignmentsData?.filter((a) => a.status === 'draft').length ?? 0 }}
              </span>
            </button>

            <button
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-medium transition cursor-pointer"
              :class="statusFilter === 'released' ? 'bg-teal-700 text-white' : 'text-slate-600 hover:bg-slate-100'"
              @click="statusFilter = 'released'"
            >
              <span class="flex items-center gap-1.5">
                <span class="w-2 h-2 rounded-full bg-teal-400"></span>
                <span>Released (Active)</span>
              </span>
              <span class="text-[11px] opacity-80">
                {{ assignmentsData?.filter((a) => a.status === 'released').length ?? 0 }}
              </span>
            </button>

            <button
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-medium transition cursor-pointer"
              :class="statusFilter === 'closed' ? 'bg-slate-800 text-white' : 'text-slate-600 hover:bg-slate-100'"
              @click="statusFilter = 'closed'"
            >
              <span class="flex items-center gap-1.5">
                <span class="w-2 h-2 rounded-full bg-slate-400"></span>
                <span>Closed</span>
              </span>
              <span class="text-[11px] opacity-80">
                {{ assignmentsData?.filter((a) => a.status === 'closed').length ?? 0 }}
              </span>
            </button>
          </div>
        </div>
      </aside>

      <!-- ================================================================= -->
      <!-- Middle Pane: Assignment Task List Cards (cols 4-6)                -->
      <!-- ================================================================= -->
      <section class="lg:col-span-4 space-y-3">
        <div class="flex items-center justify-between px-1">
          <h2 class="text-sm font-bold text-slate-900 flex items-center gap-2">
            <span>Assignments List</span>
            <span class="text-xs px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 font-normal">
              {{ filteredAssignments.length }}
            </span>
          </h2>
          <Button
            type="button"
            icon="pi pi-plus"
            label="New"
            size="small"
            severity="secondary"
            variant="text"
            class="text-xs !py-1"
            :disabled="!canManage || !selectedUnitId"
            @click="isCreateDrawerOpen = true"
          />
        </div>

        <div v-if="isLoadingAssignments" class="py-12 flex justify-center bg-white rounded-xl border border-slate-200">
          <LoadingSpinner label="Loading assignments..." />
        </div>

        <div
          v-else-if="filteredAssignments.length === 0"
          class="p-6 bg-white rounded-xl border border-dashed border-slate-300 text-center space-y-2"
        >
          <FolderOpen class="w-8 h-8 text-slate-400 mx-auto" />
          <p class="text-xs font-semibold text-slate-700">No coursework assignments</p>
          <p class="text-[11px] text-slate-500">
            {{ selectedUnit ? `No assignments found for ${selectedUnit.name}.` : 'Select a learning unit to inspect tasks.' }}
          </p>
          <Button
            v-if="canManage && selectedUnitId"
            label="Create First Assignment"
            size="small"
            icon="pi pi-plus"
            class="!mt-2"
            @click="isCreateDrawerOpen = true"
          />
        </div>

        <!-- Assignment Cards -->
        <div v-else class="space-y-3 max-h-[750px] overflow-y-auto pr-1">
          <div
            v-for="assignment in filteredAssignments"
            :key="assignment.id"
            class="p-4 rounded-xl border transition cursor-pointer space-y-3"
            :class="
              selectedAssignmentId === assignment.id
                ? 'bg-teal-50/40 border-teal-500 ring-1 ring-teal-400/50 shadow-xs'
                : 'bg-white border-slate-200/80 hover:border-slate-300 shadow-2xs'
            "
            :data-testid="`assignment-card-${assignment.id}`"
            @click="selectedAssignmentId = assignment.id"
          >
            <!-- Card Header: Title & Status -->
            <div class="flex items-start justify-between gap-2">
              <h3 class="font-bold text-sm text-slate-900 leading-snug">
                {{ assignment.title }}
              </h3>
              <StatusPill :status="assignment.status" size="sm" />
            </div>

            <!-- Parent Activity Context -->
            <div class="text-[11px] text-slate-500 flex items-center gap-1.5">
              <span class="w-1.5 h-1.5 rounded-full bg-teal-500"></span>
              <span class="font-medium text-slate-700 truncate max-w-[200px]">
                {{ assignment.learningActivityTitle ?? 'Instructional Session' }}
              </span>
              <span v-if="assignment.learningActivityDate">&bull;</span>
              <span v-if="assignment.learningActivityDate">{{ assignment.learningActivityDate }}</span>
            </div>

            <!-- Dates Line -->
            <div class="grid grid-cols-2 gap-2 text-[11px] text-slate-500 pt-1 border-t border-slate-100">
              <div>
                <span class="text-slate-400 block text-[10px] uppercase font-semibold">Release</span>
                <span class="font-medium text-slate-700">{{ formatDisplayDate(assignment.releaseDate) }}</span>
              </div>
              <div>
                <span class="text-slate-400 block text-[10px] uppercase font-semibold">Due Date</span>
                <span v-if="assignment.dueDate" class="font-medium text-slate-700">
                  {{ formatDisplayDate(assignment.dueDate) }}
                </span>
                <span v-else class="text-slate-400 italic">No deadline</span>
              </div>
            </div>

            <!-- Submission Progress Bar (X / Y submitted) -->
            <div class="space-y-1 pt-1">
              <div class="flex items-center justify-between text-[11px]">
                <span class="text-slate-500">Submissions</span>
                <span
                  v-if="selectedAssignmentId === assignment.id"
                  class="font-semibold text-slate-800"
                >
                  {{ progressMetrics.submitted }} / {{ progressMetrics.total }} submitted
                </span>
                <span v-else class="text-slate-400 text-[10px] italic">
                  Select to view roster
                </span>
              </div>
              <ProgressBar
                v-if="selectedAssignmentId === assignment.id"
                :value="progressMetrics.percent"
                :show-value="false"
                class="!h-1.5 !bg-slate-200"
              />
            </div>
          </div>
        </div>
      </section>

      <!-- ================================================================= -->
      <!-- Right Pane: Submissions Roster & Detail View (cols 7-12)          -->
      <!-- ================================================================= -->
      <main class="lg:col-span-5 space-y-4">
        <!-- Selected Assignment Detail Header -->
        <div
          v-if="selectedAssignment"
          class="p-5 bg-white rounded-xl border border-slate-200/80 shadow-xs space-y-4"
          data-testid="selected-assignment-header"
        >
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <StatusPill :status="selectedAssignment.status" size="sm" />
                <span class="font-mono text-xs text-slate-400">ID: {{ selectedAssignment.id.slice(0, 8) }}...</span>
              </div>
              <h2 class="text-xl font-bold text-slate-900 leading-tight">
                {{ selectedAssignment.title }}
              </h2>
              <p class="text-xs text-slate-500">
                Activity: <strong class="text-slate-700">{{ selectedAssignment.learningActivityTitle ?? 'Session Activity' }}</strong>
                &bull; Released: <strong class="text-slate-700">{{ formatDisplayDate(selectedAssignment.releaseDate) }}</strong>
                <span v-if="selectedAssignment.dueDate"> &bull; Due: <strong class="text-slate-700">{{ formatDisplayDate(selectedAssignment.dueDate) }}</strong></span>
              </p>
            </div>

            <!-- Action Buttons for Selected Assignment -->
            <div class="flex items-center gap-2">
              <Button
                v-if="selectedAssignment.status === 'draft'"
                type="button"
                label="Release Assignment"
                icon="pi pi-send"
                severity="success"
                size="small"
                :disabled="!canRelease"
                :loading="releaseMutation.isPending.value"
                data-testid="assignment-release-action-btn"
                @click="isReleaseConfirmOpen = true"
              />

              <Button
                v-if="selectedAssignment.status === 'released'"
                type="button"
                label="Close Assignment"
                icon="pi pi-lock"
                severity="secondary"
                variant="outlined"
                size="small"
                :disabled="!canClose"
                :loading="closeMutation.isPending.value"
                data-testid="assignment-close-action-btn"
                @click="isCloseConfirmOpen = true"
              />

              <Button
                type="button"
                label="Grant Exception"
                icon="pi pi-shield"
                severity="warn"
                variant="outlined"
                size="small"
                :disabled="!canException"
                data-testid="assignment-grant-exception-btn"
                @click="openExceptionModal()"
              />
            </div>
          </div>

          <!-- Description Block if available -->
          <div
            v-if="selectedAssignment.description"
            class="p-3 bg-slate-50 rounded-lg border border-slate-200 text-xs text-slate-700 whitespace-pre-wrap"
          >
            {{ selectedAssignment.description }}
          </div>
        </div>

        <!-- Submissions Review Table Component (SCR-ACD-02, TD-06) -->
        <div v-if="selectedAssignment">
          <SubmissionsReviewTable
            :assignment="selectedAssignment"
            :submissions="submissionsList"
            :is-loading="isLoadingSubmissions"
            :is-error="isSubmissionsError"
            :error-message="submissionsError instanceof Error ? submissionsError.message : null"
            @inspect="openInspectDrawer"
            @grant-exception="openExceptionModal"
            @refresh="refetchSubmissions"
          />
        </div>

        <div
          v-else
          class="p-12 bg-white rounded-xl border border-dashed border-slate-300 text-center space-y-3"
        >
          <FolderOpen class="w-12 h-12 text-slate-300 mx-auto" />
          <h3 class="text-sm font-bold text-slate-800">No Assignment Selected</h3>
          <p class="text-xs text-slate-500 max-w-sm mx-auto">
            Select an assignment from the list on the left, or create a new assignment to track student submissions.
          </p>
        </div>
      </main>
    </div>

    <!-- =================================================================== -->
    <!-- Drawers & Dialog Modals                                             -->
    <!-- =================================================================== -->

    <!-- 1. Create Assignment Drawer -->
    <CreateAssignmentDrawer
      v-model:visible="isCreateDrawerOpen"
      :unit-id="selectedUnitId"
      :unit-name="selectedUnit?.name"
      @created="onAssignmentCreated"
    />

    <!-- 2. Submission Detail Inspection Drawer -->
    <SubmissionDetailDrawer
      v-model:visible="isDetailDrawerOpen"
      :submission="selectedSubmission"
      :assignment-title="selectedAssignment?.title"
      @grant-exception="openExceptionModal"
    />

    <!-- 3. Grant Exception Modal -->
    <GrantExceptionModal
      v-model:visible="isExceptionModalOpen"
      :assignment-id="selectedAssignmentId"
      :assignment-title="selectedAssignment?.title"
      :students="studentsOptionList"
      :initial-student-id="exceptionInitialStudentId"
      @saved="onExceptionSaved"
    />

    <!-- 4. Release Confirmation Dialog -->
    <Dialog
      v-model:visible="isReleaseConfirmOpen"
      modal
      header="Release Assignment to Students"
      :draggable="false"
      class="w-full max-w-md"
    >
      <div class="space-y-3 py-2 text-xs text-slate-600">
        <p>
          Are you sure you want to release <strong>{{ selectedAssignment?.title }}</strong>?
        </p>
        <p class="p-3 bg-teal-50 rounded-lg border border-teal-200 text-teal-800">
          Once released, enrolled students can view the assignment and submit deliverables in their learning portal workspace.
        </p>
      </div>
      <template #footer>
        <div class="flex items-center justify-end gap-2">
          <Button
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isReleaseConfirmOpen = false"
          />
          <Button
            label="Confirm Release"
            severity="success"
            size="small"
            :loading="releaseMutation.isPending.value"
            @click="handleConfirmRelease"
          />
        </div>
      </template>
    </Dialog>

    <!-- 5. Close Confirmation Dialog -->
    <Dialog
      v-model:visible="isCloseConfirmOpen"
      modal
      header="Close Assignment"
      :draggable="false"
      class="w-full max-w-md"
    >
      <div class="space-y-3 py-2 text-xs text-slate-600">
        <p>
          Are you sure you want to close <strong>{{ selectedAssignment?.title }}</strong>?
        </p>
        <p class="p-3 bg-amber-50 rounded-lg border border-amber-200 text-amber-800">
          Closing will lock the assignment against further standard submissions. Students will no longer be able to submit unless an administrative exception is recorded.
        </p>
      </div>
      <template #footer>
        <div class="flex items-center justify-end gap-2">
          <Button
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isCloseConfirmOpen = false"
          />
          <Button
            label="Confirm Close"
            severity="warn"
            size="small"
            :loading="closeMutation.isPending.value"
            @click="handleConfirmClose"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
