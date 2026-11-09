<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import DatePicker from 'primevue/datepicker';
import {
  Award,
  BookOpen,
  Calendar,
  CheckCircle2,
  Clock,
  FileText,
  Filter,
  GraduationCap,
  Layers,
  RefreshCw,
  Search,
  ShieldCheck,
  UserCheck,
  XCircle,
} from 'lucide-vue-next';
import { useAuthStore } from '@/app/stores/auth.store';
import { EmptyState, LoadingSpinner, StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useLearningUnits } from '@/features/academics/curriculum/composables/useCurriculum';
import {
  useUnitProgressQuery,
  useRecordProgressMutation,
  useConcludeParticipationMutation,
} from '../composables/useLearningProgress';
import type {
  ConcludeParticipationRequest,
  ConcludeParticipationResponse,
  LearningProgress,
  LearningProgressBasis,
  LearningProgressStatus,
  RecordProgressRequest,
} from '../types';
import { RECORDS_PERMISSIONS, AUTHORITY_ROLES } from '../constants/permissions';

// ---------------------------------------------------------------------------
// 1. Props & Emits
// ---------------------------------------------------------------------------
const props = withDefaults(
  defineProps<{
    defaultPeriodId?: string;
    defaultUnitId?: string;
  }>(),
  {
    defaultPeriodId: '2026-ganjil',
    defaultUnitId: undefined,
  },
);

const emit = defineEmits<{
  (e: 'participationConcluded', response: ConcludeParticipationResponse): void;
  (e: 'progressRecorded', progress: LearningProgress): void;
}>();

// ---------------------------------------------------------------------------
// 2. Auth & Role Guards
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const isAuthority = computed(() =>
  authStore.hasAnyRole([...AUTHORITY_ROLES]) ||
  authStore.hasPermission(RECORDS_PERMISSIONS.RESULTS_APPROVE)
);

const canRecordProgress = computed(() =>
  isAuthority.value ||
  authStore.hasRole('AcademicCoordinator') ||
  authStore.hasPermission(RECORDS_PERMISSIONS.PROGRESS_RECORD)
);

const canConcludeParticipation = computed(() =>
  isAuthority.value ||
  authStore.hasRole('AcademicCoordinator') ||
  authStore.hasPermission(RECORDS_PERMISSIONS.PROGRESS_CONCLUDE)
);

// ---------------------------------------------------------------------------
// 3. Filter & Context State
// ---------------------------------------------------------------------------
const selectedPeriodId = ref<string>(props.defaultPeriodId || '2026-ganjil');
const selectedUnitId = ref<string>(props.defaultUnitId || '');
const statusFilter = ref<string>('all');
const searchQuery = ref<string>('');

const periodOptions = [
  { label: 'Semester Ganjil 2026/2027 (2026-ganjil)', value: '2026-ganjil' },
  { label: 'Semester Genap 2026/2027 (2026-genap)', value: '2026-genap' },
  { label: 'Semester Ganjil 2025/2026 (2025-ganjil)', value: '2025-ganjil' },
  { label: 'Semester Genap 2025/2026 (2025-genap)', value: '2025-genap' },
];

const statusFilterOptions = [
  { label: 'All Progress Statuses', value: 'all' },
  { label: 'Not Started', value: 'not_started' },
  { label: 'In Progress', value: 'in_progress' },
  { label: 'Completed', value: 'completed' },
  { label: 'Needs Remediation', value: 'needs_remediation' },
  { label: 'Exempted', value: 'exempted' },
];

// Fallback dummy units if API has not seeded any yet
const fallbackUnits = [
  { id: '3fa85f64-5717-4562-b3fc-2c963f66afa6', title: 'Grade 4 Mathematics (MATH-04)', code: 'MATH-04' },
  { id: '4fa85f64-5717-4562-b3fc-2c963f66afa7', title: 'Grade 4 Christian Education (PAKD-04)', code: 'PAKD-04' },
  { id: '5fa85f64-5717-4562-b3fc-2c963f66afa8', title: 'Grade 4 Bahasa Indonesia (BIND-04)', code: 'BIND-04' },
  { id: '6fa85f64-5717-4562-b3fc-2c963f66afa9', title: 'Grade 4 Science / IPAS (IPAS-04)', code: 'IPAS-04' },
];

const unitsResult = useLearningUnits();

const availableUnits = computed(() => {
  const list =
    (unitsResult as any)?.data?.value ??
    (unitsResult as any)?.learningUnits?.value ??
    [];
  if (list.length > 0) {
    return list.map((u: any) => ({
      id: u.id,
      title: `${u.title} (${u.code})`,
      code: u.code,
    }));
  }
  return fallbackUnits;
});

// Auto-select first unit if none selected
watch(
  availableUnits,
  (units) => {
    if (!selectedUnitId.value && units.length > 0) {
      selectedUnitId.value = units[0].id;
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 4. Data Query & Mutation Hooks
// ---------------------------------------------------------------------------
const {
  data: unitRoster,
  isLoading: isLoadingRoster,
  refetch: refetchRoster,
} = useUnitProgressQuery(
  selectedUnitId,
  selectedPeriodId,
  { enabled: computed(() => Boolean(selectedUnitId.value)) },
);

const recordProgressMutation = useRecordProgressMutation();
const concludeParticipationMutation = useConcludeParticipationMutation();

// ---------------------------------------------------------------------------
// 5. Computed Roster List
// ---------------------------------------------------------------------------
const filteredRoster = computed<LearningProgress[]>(() => {
  const roster = unitRoster.value ?? [];
  return roster.filter((item) => {
    const itemStatus = item.progressStatus || item.status || 'not_started';
    if (statusFilter.value !== 'all' && itemStatus !== statusFilter.value) {
      return false;
    }
    if (searchQuery.value.trim().length > 0) {
      const q = searchQuery.value.toLowerCase().trim();
      const name = (item.studentName ?? '').toLowerCase();
      const num = (item.studentNumber ?? item.studentIdentifier ?? '').toLowerCase();
      return name.includes(q) || num.includes(q);
    }
    return true;
  });
});

// Counts summary
const progressSummary = computed(() => {
  const roster = unitRoster.value ?? [];
  const total = roster.length;
  let completed = 0;
  let inProgress = 0;
  let needsRemediation = 0;
  let exempted = 0;
  let notStarted = 0;

  for (const item of roster) {
    const s = item.progressStatus || item.status || 'not_started';
    if (s === 'completed') completed++;
    else if (s === 'in_progress') inProgress++;
    else if (s === 'needs_remediation') needsRemediation++;
    else if (s === 'exempted') exempted++;
    else notStarted++;
  }

  return { total, completed, inProgress, needsRemediation, exempted, notStarted };
});

// ---------------------------------------------------------------------------
// 6. Conclude Participation Confirmation Dialog (UC-ACD-12)
// ---------------------------------------------------------------------------
const isConcludeDialogOpen = ref(false);
const activeStudentForConclusion = ref<LearningProgress | null>(null);
const concludeCompletionDate = ref<string>(new Date().toISOString().slice(0, 10));
const concludeNotes = ref<string>('');
const concludeErrorMessage = ref<string | null>(null);

function openConcludeDialog(student: LearningProgress) {
  activeStudentForConclusion.value = student;
  concludeCompletionDate.value = new Date().toISOString().slice(0, 10);
  concludeNotes.value = '';
  concludeErrorMessage.value = null;
  isConcludeDialogOpen.value = true;
}

async function handleConfirmConclude() {
  if (!activeStudentForConclusion.value) return;
  concludeErrorMessage.value = null;

  try {
    const payload: ConcludeParticipationRequest = {
      participationId: activeStudentForConclusion.value.participationId || undefined,
      studentId: activeStudentForConclusion.value.studentId,
      learningUnitId: selectedUnitId.value,
      academicPeriodId: selectedPeriodId.value,
      completionDate: concludeCompletionDate.value || undefined,
      notes: concludeNotes.value.trim() || undefined,
    };

    const res = await concludeParticipationMutation.mutateAsync(payload);
    emit('participationConcluded', res);
    isConcludeDialogOpen.value = false;
    refetchRoster();
  } catch (err: any) {
    concludeErrorMessage.value =
      err?.response?.data?.detail ||
      err?.message ||
      'Failed to conclude participation. Please verify passing criteria (KKM).';
  }
}

// ---------------------------------------------------------------------------
// 7. Record Prior Learning (RPL) Dialog (basis = 'authorized_decision')
// ---------------------------------------------------------------------------
const isRplDialogOpen = ref(false);
const rplStudentId = ref<string>('');
const rplStudentName = ref<string>('');
const rplStatus = ref<LearningProgressStatus>('completed');
const rplCompletionDate = ref<string>(new Date().toISOString().slice(0, 10));
const rplNotes = ref<string>('');
const rplErrorMessage = ref<string | null>(null);

function openRplDialog(student?: LearningProgress) {
  if (student) {
    rplStudentId.value = student.studentId;
    rplStudentName.value = student.studentName || student.studentNumber || student.studentId;
  } else {
    rplStudentId.value = '';
    rplStudentName.value = '';
  }
  rplStatus.value = 'completed';
  rplCompletionDate.value = new Date().toISOString().slice(0, 10);
  rplNotes.value = '';
  rplErrorMessage.value = null;
  isRplDialogOpen.value = true;
}

async function handleSubmitRpl() {
  rplErrorMessage.value = null;

  if (!rplStudentId.value.trim()) {
    rplErrorMessage.value = 'Student identifier is required.';
    return;
  }
  if (!rplNotes.value.trim()) {
    rplErrorMessage.value =
      'Explanatory notes/justification are mandatory when basis is authorized decision (e.g. credit transfer reference).';
    return;
  }

  try {
    const payload: RecordProgressRequest = {
      studentId: rplStudentId.value.trim(),
      learningUnitId: selectedUnitId.value,
      academicPeriodId: selectedPeriodId.value,
      status: rplStatus.value,
      basis: 'authorized_decision',
      completionDate: rplCompletionDate.value || null,
      notes: rplNotes.value.trim(),
    };

    const result = await recordProgressMutation.mutateAsync(payload);
    emit('progressRecorded', result);
    isRplDialogOpen.value = false;
    refetchRoster();
  } catch (err: any) {
    rplErrorMessage.value =
      err?.response?.data?.detail || err?.message || 'Failed to record prior learning.';
  }
}

// ---------------------------------------------------------------------------
// 8. Explicit Date Stamping / Progress Edit Modal
// ---------------------------------------------------------------------------
const isEditProgressDialogOpen = ref(false);
const editStudentProgress = ref<LearningProgress | null>(null);
const editStatus = ref<LearningProgressStatus>('in_progress');
const editBasis = ref<LearningProgressBasis>('assessment_evidence');
const editCompletionDate = ref<string>('');
const editNotes = ref<string>('');
const editErrorMessage = ref<string | null>(null);

function openEditProgressDialog(student: LearningProgress) {
  editStudentProgress.value = student;
  const currentStatus = student.progressStatus || student.status || 'not_started';
  editStatus.value = currentStatus;
  editBasis.value = (student.basis as LearningProgressBasis) || 'assessment_evidence';
  editCompletionDate.value =
    student.completionDate ||
    (currentStatus === 'completed' ? new Date().toISOString().slice(0, 10) : '');
  editNotes.value = student.notes || '';
  editErrorMessage.value = null;
  isEditProgressDialogOpen.value = true;
}

async function handleSaveProgress() {
  if (!editStudentProgress.value) return;
  editErrorMessage.value = null;

  if (editStatus.value === 'completed' && !editCompletionDate.value) {
    editErrorMessage.value = 'Completion date is mandatory when status is completed.';
    return;
  }
  if (editBasis.value === 'authorized_decision' && !editNotes.value.trim()) {
    editErrorMessage.value = 'Notes are mandatory when operational basis is authorized decision.';
    return;
  }

  try {
    const payload: RecordProgressRequest = {
      studentId: editStudentProgress.value.studentId,
      learningUnitId: selectedUnitId.value,
      academicPeriodId: selectedPeriodId.value,
      status: editStatus.value,
      basis: editBasis.value,
      completionDate: editStatus.value === 'completed' ? editCompletionDate.value : null,
      notes: editNotes.value.trim() || null,
    };

    const res = await recordProgressMutation.mutateAsync(payload);
    emit('progressRecorded', res);
    isEditProgressDialogOpen.value = false;
    refetchRoster();
  } catch (err: any) {
    editErrorMessage.value =
      err?.response?.data?.detail || err?.message || 'Failed to update progress record.';
  }
}

// Inline Quick Completion Date Stamping
async function handleQuickStampDate(student: LearningProgress, explicitDate: string) {
  if (!explicitDate) return;
  try {
    const payload: RecordProgressRequest = {
      studentId: student.studentId,
      learningUnitId: selectedUnitId.value,
      academicPeriodId: selectedPeriodId.value,
      status: 'completed',
      basis: (student.basis as LearningProgressBasis) || 'assessment_evidence',
      completionDate: explicitDate,
      notes: student.notes || null,
    };
    const res = await recordProgressMutation.mutateAsync(payload);
    emit('progressRecorded', res);
    refetchRoster();
  } catch (err: any) {
    // handled by toast
  }
}

// ---------------------------------------------------------------------------
// 9. Helpers & Badges
// ---------------------------------------------------------------------------
function getProgressStatusLabel(status: string): string {
  switch (status) {
    case 'completed':
      return 'Completed';
    case 'in_progress':
      return 'In Progress';
    case 'needs_remediation':
      return 'Needs Remediation';
    case 'exempted':
      return 'Exempted';
    case 'not_started':
    default:
      return 'Not Started';
  }
}

function getProgressBadgeVariant(status: string): 'emerald' | 'amber' | 'rose' | 'slate' | 'sky' {
  switch (status) {
    case 'completed':
      return 'emerald';
    case 'in_progress':
      return 'sky';
    case 'needs_remediation':
      return 'rose';
    case 'exempted':
      return 'amber';
    case 'not_started':
    default:
      return 'slate';
  }
}

function getBasisLabel(basis?: string | null): string {
  switch (basis) {
    case 'assessment_evidence':
      return 'Assessment Evidence';
    case 'learning_work':
      return 'Learning Work';
    case 'authorized_decision':
      return 'Authorized Decision';
    default:
      return 'Unspecified';
  }
}

function getBasisTagClasses(basis?: string | null): string {
  switch (basis) {
    case 'assessment_evidence':
      return 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800';
    case 'learning_work':
      return 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-900/30 dark:text-indigo-300 dark:border-indigo-800';
    case 'authorized_decision':
      return 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-300 dark:border-amber-800';
    default:
      return 'bg-surface-100 text-surface-600 border-surface-200 dark:bg-surface-800 dark:text-surface-400 dark:border-surface-700';
  }
}
</script>

<template>
  <div class="space-y-5" data-testid="learning-progress-tab-root">
    <!-- Header Summary KPI Cards -->
    <div class="grid grid-cols-2 md:grid-cols-6 gap-3">
      <div class="bg-surface-50 dark:bg-surface-800/60 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700">
        <span class="text-[11px] font-semibold text-surface-500 uppercase tracking-wider block">Total Roster</span>
        <span class="text-xl font-bold text-surface-900 dark:text-surface-100 mt-0.5 block" data-testid="summary-total">
          {{ progressSummary.total }}
        </span>
      </div>
      <div class="bg-emerald-50/60 dark:bg-emerald-950/20 p-3.5 rounded-xl border border-emerald-200/70 dark:border-emerald-800/50">
        <span class="text-[11px] font-semibold text-emerald-700 dark:text-emerald-400 uppercase tracking-wider block">Completed</span>
        <span class="text-xl font-bold text-emerald-700 dark:text-emerald-400 mt-0.5 block" data-testid="summary-completed">
          {{ progressSummary.completed }}
        </span>
      </div>
      <div class="bg-sky-50/60 dark:bg-sky-950/20 p-3.5 rounded-xl border border-sky-200/70 dark:border-sky-800/50">
        <span class="text-[11px] font-semibold text-sky-700 dark:text-sky-400 uppercase tracking-wider block">In Progress</span>
        <span class="text-xl font-bold text-sky-700 dark:text-sky-400 mt-0.5 block" data-testid="summary-in-progress">
          {{ progressSummary.inProgress }}
        </span>
      </div>
      <div class="bg-rose-50/60 dark:bg-rose-950/20 p-3.5 rounded-xl border border-rose-200/70 dark:border-rose-800/50">
        <span class="text-[11px] font-semibold text-rose-700 dark:text-rose-400 uppercase tracking-wider block">Remediation</span>
        <span class="text-xl font-bold text-rose-700 dark:text-rose-400 mt-0.5 block" data-testid="summary-remediation">
          {{ progressSummary.needsRemediation }}
        </span>
      </div>
      <div class="bg-amber-50/60 dark:bg-amber-950/20 p-3.5 rounded-xl border border-amber-200/70 dark:border-amber-800/50">
        <span class="text-[11px] font-semibold text-amber-700 dark:text-amber-400 uppercase tracking-wider block">Exempted / RPL</span>
        <span class="text-xl font-bold text-amber-700 dark:text-amber-400 mt-0.5 block" data-testid="summary-exempted">
          {{ progressSummary.exempted }}
        </span>
      </div>
      <div class="bg-surface-50 dark:bg-surface-800/60 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700">
        <span class="text-[11px] font-semibold text-surface-500 uppercase tracking-wider block">Not Started</span>
        <span class="text-xl font-bold text-surface-600 dark:text-surface-400 mt-0.5 block" data-testid="summary-not-started">
          {{ progressSummary.notStarted }}
        </span>
      </div>
    </div>

    <!-- Filter & Toolbar (SCR-ACD-07) -->
    <div class="flex flex-wrap items-center justify-between gap-3 bg-surface-50 dark:bg-surface-800/40 p-4 rounded-xl border border-surface-200 dark:border-surface-700">
      <div class="flex flex-wrap items-center gap-3">
        <!-- Academic Period Selector -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Academic Period
          </label>
          <Select
            v-model="selectedPeriodId"
            :options="periodOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Academic Period"
            class="w-64 text-xs"
            data-testid="select-progress-period"
          />
        </div>

        <!-- Learning Unit Selector -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Learning Unit
          </label>
          <Select
            v-model="selectedUnitId"
            :options="availableUnits"
            option-label="title"
            option-value="id"
            placeholder="Select Learning Unit"
            class="w-72 text-xs"
            data-testid="select-progress-unit"
          />
        </div>

        <!-- Progress Status Filter -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Progress Status
          </label>
          <Select
            v-model="statusFilter"
            :options="statusFilterOptions"
            option-label="label"
            option-value="value"
            class="w-48 text-xs"
            data-testid="select-progress-status-filter"
          />
        </div>

        <!-- Student Search -->
        <div>
          <label class="block text-[11px] font-semibold text-surface-500 uppercase tracking-wider mb-1">
            Find Student
          </label>
          <div class="relative">
            <Search class="w-3.5 h-3.5 text-surface-400 absolute left-2.5 top-2.5 pointer-events-none" />
            <InputText
              v-model="searchQuery"
              placeholder="Search by name or ID..."
              class="pl-8 text-xs w-52"
              data-testid="input-progress-search"
            />
          </div>
        </div>
      </div>

      <!-- Action Buttons -->
      <div class="flex items-center gap-2 self-end">
        <Button
          size="small"
          severity="secondary"
          variant="outlined"
          class="cursor-pointer"
          data-testid="btn-refetch-progress"
          @click="refetchRoster"
        >
          <RefreshCw class="w-3.5 h-3.5 mr-1" />
          <span>Refresh</span>
        </Button>

        <Button
          size="small"
          severity="help"
          variant="outlined"
          :disabled="!canRecordProgress || !selectedUnitId"
          class="cursor-pointer"
          data-testid="btn-record-prior-learning"
          @click="openRplDialog()"
        >
          <Award class="w-3.5 h-3.5 mr-1 text-purple-600 dark:text-purple-400" />
          <span>Record Prior Learning (RPL)</span>
        </Button>
      </div>
    </div>

    <!-- Roster Table / Loading / Empty States -->
    <div v-if="isLoadingRoster" class="py-12 flex justify-center items-center">
      <LoadingSpinner label="Loading unit participant learning progress roster..." />
    </div>

    <div v-else-if="filteredRoster.length === 0" class="py-12">
      <EmptyState
        title="No participant progress records found"
        description="No enrolled students match the active unit and filter criteria, or participations have not yet been synchronized."
      />
    </div>

    <div v-else class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs">
      <table class="w-full text-xs text-left border-collapse" data-testid="learning-progress-table">
        <thead class="bg-surface-50 dark:bg-surface-800 text-surface-600 dark:text-surface-300 border-b border-surface-200 dark:border-surface-700">
          <tr>
            <th class="p-3">Student / Candidate</th>
            <th class="p-3">Unit & Section</th>
            <th class="p-3 text-center">Operative Evaluation</th>
            <th class="p-3">Progress Status</th>
            <th class="p-3">Operational Basis</th>
            <th class="p-3">Completion Date</th>
            <th class="p-3">Pedagogical Notes</th>
            <th class="p-3 text-right">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-surface-100 dark:divide-surface-800">
          <tr
            v-for="item in filteredRoster"
            :key="item.studentId"
            class="hover:bg-surface-50/70 dark:hover:bg-surface-800/40 transition-colors"
            :data-testid="`row-progress-${item.studentId}`"
          >
            <!-- Student -->
            <td class="p-3 font-medium">
              <div class="text-surface-900 dark:text-surface-100 font-semibold">
                {{ item.studentName || 'Student ' + item.studentId.slice(0, 8) }}
              </div>
              <div class="text-surface-500 font-mono text-[11px]">
                {{ item.studentNumber || item.studentIdentifier || item.studentId.slice(0, 8) }}
              </div>
            </td>

            <!-- Unit & Section -->
            <td class="p-3">
              <div class="text-surface-700 dark:text-surface-300">
                {{ item.learningUnitCode || 'UNIT' }}
              </div>
              <span
                v-if="item.sectionLabel"
                class="inline-block mt-0.5 text-[10px] font-semibold bg-surface-100 dark:bg-surface-800 text-surface-600 dark:text-surface-300 px-1.5 py-0.2 rounded border border-surface-200 dark:border-surface-700"
              >
                Section {{ item.sectionLabel }}
              </span>
            </td>

            <!-- Operative Evaluation -->
            <td class="p-3 text-center">
              <div v-if="item.operativeScore !== undefined && item.operativeScore !== null" class="font-semibold text-surface-800 dark:text-surface-200">
                {{ Number(item.operativeScore).toFixed(1) }}
                <span v-if="item.gradeLetter" class="text-teal-600 font-bold ml-0.5">({{ item.gradeLetter }})</span>
              </div>
              <div v-else-if="item.operativeGrade" class="font-medium text-surface-700">
                {{ item.operativeGrade }}
              </div>
              <div v-else class="text-surface-400 italic text-[11px]">
                Pending
              </div>
            </td>

            <!-- Phase-2 Minimal Progress Badge -->
            <td class="p-3">
              <StatusPill
                :status="item.progressStatus || item.status || 'not_started'"
                :label="getProgressStatusLabel(item.progressStatus || item.status || 'not_started')"
                :variant="getProgressBadgeVariant(item.progressStatus || item.status || 'not_started')"
                size="sm"
                :data-testid="`badge-status-${item.studentId}`"
              />
            </td>

            <!-- Operational Basis Tag -->
            <td class="p-3">
              <span
                class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium border"
                :class="getBasisTagClasses(item.basis)"
                :data-testid="`tag-basis-${item.studentId}`"
              >
                {{ getBasisLabel(item.basis) }}
              </span>
            </td>

            <!-- Completion Date (Supports explicit out-of-period stamping) -->
            <td class="p-3 font-mono text-[11px]">
              <div class="flex items-center gap-1.5">
                <span v-if="item.completionDate" class="font-semibold text-emerald-700 dark:text-emerald-400">
                  {{ formatDisplayDate(item.completionDate) }}
                </span>
                <span v-else class="text-surface-400 italic">
                  Not stamped
                </span>

                <!-- Quick Date Input for Out-of-Period Stamping -->
                <input
                  v-if="canRecordProgress"
                  type="date"
                  :value="item.completionDate || ''"
                  class="ml-1 text-[11px] px-1 py-0.5 border border-surface-300 dark:border-surface-600 rounded bg-surface-0 dark:bg-surface-900 text-surface-800 dark:text-surface-200 cursor-pointer"
                  title="Stamp explicit out-of-period completion date"
                  :data-testid="`input-completion-date-${item.studentId}`"
                  @change="(e: any) => handleQuickStampDate(item, e.target.value)"
                />
              </div>
            </td>

            <!-- Pedagogical Notes -->
            <td class="p-3 text-surface-600 dark:text-surface-400 max-w-[200px] truncate" :title="item.notes || undefined">
              {{ item.notes || '-' }}
            </td>

            <!-- Actions -->
            <td class="p-3 text-right space-x-1.5 whitespace-nowrap">
              <!-- Conclude Participation Action (UC-ACD-12) -->
              <Button
                size="small"
                severity="success"
                variant="outlined"
                :disabled="!canConcludeParticipation || (item.progressStatus || item.status) === 'completed'"
                class="cursor-pointer"
                title="Verify passing criteria and formally conclude participation"
                :data-testid="`btn-conclude-${item.studentId}`"
                @click="openConcludeDialog(item)"
              >
                <CheckCircle2 class="w-3.5 h-3.5 mr-1 text-emerald-600" />
                <span>Conclude</span>
              </Button>

              <!-- Update Progress Action -->
              <Button
                size="small"
                severity="secondary"
                variant="outlined"
                :disabled="!canRecordProgress"
                class="cursor-pointer"
                title="Update progress state, basis, or completion date"
                :data-testid="`btn-edit-progress-${item.studentId}`"
                @click="openEditProgressDialog(item)"
              >
                <span>Edit</span>
              </Button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ===================================================================== -->
    <!-- DIALOG: Conclude Participation Confirmation (UC-ACD-12)               -->
    <!-- ===================================================================== -->
    <Dialog
      v-model:visible="isConcludeDialogOpen"
      header="Conclude Unit Participation (UC-ACD-12)"
      :modal="true"
      :style="{ width: '520px' }"
      data-testid="dialog-conclude-participation"
    >
      <div v-if="activeStudentForConclusion" class="space-y-4 pt-2">
        <!-- Candidate Banner -->
        <div class="bg-surface-50 dark:bg-surface-800/60 p-3 rounded-lg border border-surface-200 dark:border-surface-700 text-xs">
          <div class="font-semibold text-surface-900 dark:text-surface-100">
            {{ activeStudentForConclusion.studentName || 'Student ' + activeStudentForConclusion.studentId.slice(0, 8) }}
          </div>
          <div class="text-surface-500 font-mono text-[11px] mt-0.5">
            Unit: {{ activeStudentForConclusion.learningUnitCode || 'Selected Unit' }} | Period: {{ selectedPeriodId }}
          </div>
        </div>

        <!-- Passing Criteria Verification Notice -->
        <div class="bg-emerald-50/70 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-800 p-3 rounded-lg flex items-start gap-2 text-xs text-emerald-800 dark:text-emerald-300">
          <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
          <div>
            <div class="font-semibold">Passing Criteria Verified</div>
            <p class="text-[11px] text-emerald-700 dark:text-emerald-400 mt-0.5">
              Operative summative score
              <span v-if="activeStudentForConclusion.operativeScore" class="font-bold">
                ({{ Number(activeStudentForConclusion.operativeScore).toFixed(1) }})
              </span>
              verifies academic mastery. Concluding participation transitions record to
              <span class="font-bold">completed</span>, updates progress ledger, and publishes
              <code class="font-mono text-[10px]">ParticipationConcludedEvent</code>.
            </p>
          </div>
        </div>

        <!-- Explicit Out-of-Period Completion Date Picker -->
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
            Explicit Completion Date
          </label>
          <div class="text-[11px] text-surface-500 mb-1">
            Permits explicit out-of-period dates for PACE modules, holiday packets, or post-term remediation.
          </div>
          <InputText
            v-model="concludeCompletionDate"
            type="date"
            class="w-full text-xs"
            data-testid="input-conclude-completion-date"
          />
        </div>

        <!-- Optional Conclusion Notes -->
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
            Conclusion Remarks (Optional)
          </label>
          <Textarea
            v-model="concludeNotes"
            rows="3"
            placeholder="Administrative remarks, mastery notes, or verification details..."
            class="w-full text-xs"
            data-testid="input-conclude-notes"
          />
        </div>

        <!-- Error Alert -->
        <div
          v-if="concludeErrorMessage"
          class="bg-rose-50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 p-3 rounded-lg text-xs text-rose-700 dark:text-rose-400"
          data-testid="alert-conclude-error"
        >
          {{ concludeErrorMessage }}
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            size="small"
            severity="secondary"
            variant="outlined"
            data-testid="btn-cancel-conclude"
            @click="isConcludeDialogOpen = false"
          >
            Cancel
          </Button>
          <Button
            size="small"
            severity="success"
            :loading="concludeParticipationMutation.isPending.value"
            data-testid="btn-confirm-conclude"
            @click="handleConfirmConclude"
          >
            <CheckCircle2 class="w-3.5 h-3.5 mr-1" />
            <span>Confirm & Conclude</span>
          </Button>
        </div>
      </template>
    </Dialog>

    <!-- ===================================================================== -->
    <!-- DIALOG: Record Prior Learning (RPL / basis = 'authorized_decision')   -->
    <!-- ===================================================================== -->
    <Dialog
      v-model:visible="isRplDialogOpen"
      header="Record Prior Learning & External Recognition (RPL)"
      :modal="true"
      :style="{ width: '540px' }"
      data-testid="dialog-record-prior-learning"
    >
      <div class="space-y-4 pt-2">
        <div class="bg-amber-50/70 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 p-3 rounded-lg flex items-start gap-2 text-xs text-amber-800 dark:text-amber-300">
          <Award class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
          <div>
            <div class="font-semibold">Institutional Recognition (Authorized Decision)</div>
            <p class="text-[11px] text-amber-700 dark:text-amber-400 mt-0.5">
              Certifies completion or exemption via transferred credits, external examination, or committee determination.
              Explanatory notes and reference decree/transcript are strictly required.
            </p>
          </div>
        </div>

        <!-- Student Selector / ID -->
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
            Target Student ID or UUID *
          </label>
          <InputText
            v-model="rplStudentId"
            placeholder="e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6"
            class="w-full text-xs font-mono"
            data-testid="input-rpl-student-id"
          />
          <span v-if="rplStudentName" class="text-[11px] text-surface-500">
            Selected: {{ rplStudentName }}
          </span>
        </div>

        <!-- Status -->
        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1">
            <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
              Recognition Status *
            </label>
            <Select
              v-model="rplStatus"
              :options="[
                { label: 'Completed (Unit Credit Granted)', value: 'completed' },
                { label: 'Exempted (Requirement Waived)', value: 'exempted' },
              ]"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-rpl-status"
            />
          </div>

          <!-- Explicit Completion Date -->
          <div class="space-y-1">
            <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
              Recognition / Completion Date *
            </label>
            <InputText
              v-model="rplCompletionDate"
              type="date"
              class="w-full text-xs"
              data-testid="input-rpl-date"
            />
          </div>
        </div>

        <!-- Explanatory Notes (Mandatory for RPL) -->
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
            Explanatory Justification & External Evidence *
          </label>
          <Textarea
            v-model="rplNotes"
            rows="3"
            placeholder="e.g. Transferred credits from SMA Harapan Bangsa (Transcript ref: #TR-2026-99). Authorized per Academic Committee decree."
            class="w-full text-xs"
            data-testid="input-rpl-notes"
          />
          <span class="text-[11px] text-surface-400">
            Mandatory audit narrative (minimum 10 characters recommended).
          </span>
        </div>

        <!-- Error Alert -->
        <div
          v-if="rplErrorMessage"
          class="bg-rose-50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 p-3 rounded-lg text-xs text-rose-700 dark:text-rose-400"
          data-testid="alert-rpl-error"
        >
          {{ rplErrorMessage }}
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            size="small"
            severity="secondary"
            variant="outlined"
            data-testid="btn-cancel-rpl"
            @click="isRplDialogOpen = false"
          >
            Cancel
          </Button>
          <Button
            size="small"
            severity="help"
            :loading="recordProgressMutation.isPending.value"
            data-testid="btn-confirm-rpl"
            @click="handleSubmitRpl"
          >
            <Award class="w-3.5 h-3.5 mr-1" />
            <span>Record Prior Learning</span>
          </Button>
        </div>
      </template>
    </Dialog>

    <!-- ===================================================================== -->
    <!-- DIALOG: Update Progress & Stamp Date                                  -->
    <!-- ===================================================================== -->
    <Dialog
      v-model:visible="isEditProgressDialogOpen"
      header="Update Student Learning Progress"
      :modal="true"
      :style="{ width: '500px' }"
      data-testid="dialog-edit-progress"
    >
      <div v-if="editStudentProgress" class="space-y-4 pt-2">
        <div class="bg-surface-50 dark:bg-surface-800/60 p-3 rounded-lg border border-surface-200 dark:border-surface-700 text-xs">
          <div class="font-semibold text-surface-900 dark:text-surface-100">
            {{ editStudentProgress.studentName || 'Student ' + editStudentProgress.studentId.slice(0, 8) }}
          </div>
          <div class="text-surface-500 font-mono text-[11px] mt-0.5">
            Unit: {{ editStudentProgress.learningUnitCode || 'Selected Unit' }} | Period: {{ selectedPeriodId }}
          </div>
        </div>

        <!-- Status & Basis Selectors -->
        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1">
            <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
              Progress Status *
            </label>
            <Select
              v-model="editStatus"
              :options="[
                { label: 'Not Started', value: 'not_started' },
                { label: 'In Progress', value: 'in_progress' },
                { label: 'Completed', value: 'completed' },
                { label: 'Needs Remediation', value: 'needs_remediation' },
                { label: 'Exempted', value: 'exempted' },
              ]"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-edit-status"
            />
          </div>

          <div class="space-y-1">
            <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
              Operational Basis *
            </label>
            <Select
              v-model="editBasis"
              :options="[
                { label: 'Assessment Evidence', value: 'assessment_evidence' },
                { label: 'Learning Work / Coursework', value: 'learning_work' },
                { label: 'Authorized Decision', value: 'authorized_decision' },
              ]"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-edit-basis"
            />
          </div>
        </div>

        <!-- Explicit Out-of-Period Completion Date -->
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
            Completion Date (Mandatory when completed)
          </label>
          <InputText
            v-model="editCompletionDate"
            type="date"
            class="w-full text-xs"
            data-testid="input-edit-completion-date"
          />
        </div>

        <!-- Notes -->
        <div class="space-y-1">
          <label class="block text-xs font-semibold text-surface-700 dark:text-surface-300">
            Pedagogical Remarks / Evidence
          </label>
          <Textarea
            v-model="editNotes"
            rows="3"
            placeholder="Teacher observations, PACE completion notes, or remediation details..."
            class="w-full text-xs"
            data-testid="input-edit-notes"
          />
        </div>

        <!-- Error Alert -->
        <div
          v-if="editErrorMessage"
          class="bg-rose-50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 p-3 rounded-lg text-xs text-rose-700 dark:text-rose-400"
          data-testid="alert-edit-error"
        >
          {{ editErrorMessage }}
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            size="small"
            severity="secondary"
            variant="outlined"
            data-testid="btn-cancel-edit"
            @click="isEditProgressDialogOpen = false"
          >
            Cancel
          </Button>
          <Button
            size="small"
            severity="primary"
            :loading="recordProgressMutation.isPending.value"
            data-testid="btn-save-progress"
            @click="handleSaveProgress"
          >
            <span>Save Progress</span>
          </Button>
        </div>
      </template>
    </Dialog>
  </div>
</template>
