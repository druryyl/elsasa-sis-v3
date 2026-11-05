<script setup lang="ts">
import { ref, computed } from 'vue';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import {
  BookOpen,
  Calendar,
  Clock,
  AlertTriangle,
  CheckCircle2,
  Send,
  History,
  RotateCcw,
  Search,
  Filter,
  Layers,
  Sparkles,
  ExternalLink,
  UploadCloud,
  FileText,
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
import {
  useStudentCourseworkWorkspaceQuery,
} from '../composables/useStudentCoursework';
import type {
  CourseworkStatusFilter,
  StudentCourseworkTask,
  SubmitCourseworkResponse,
} from '../types';
import CourseworkSubmissionModal from '../components/CourseworkSubmissionModal.vue';
import SubmissionHistoryDrawer from '../components/SubmissionHistoryDrawer.vue';

interface Props {
  studentId?: string;
}

const props = withDefaults(defineProps<Props>(), {
  studentId: undefined,
});

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Student Portal', to: '/portal' },
  { label: 'Learning Workspace', current: true },
];

const authStore = useAuthStore();
const toast = useToast();

const effectiveStudentId = computed(() => {
  return props.studentId || authStore.user?.userId || '';
});

// ---------------------------------------------------------------------------
// 2. Active Filter & Query
// ---------------------------------------------------------------------------
const activeFilter = ref<CourseworkStatusFilter>('upcoming');
const searchQuery = ref('');

// Primary tasks query for active tab
const {
  data: tasksData,
  isLoading,
  isFetching,
  error: queryError,
  refetch,
} = useStudentCourseworkWorkspaceQuery(activeFilter);

// All tasks query for calculating tab counter badges
const {
  data: allTasksData,
} = useStudentCourseworkWorkspaceQuery('all');

// Tab badge counters calculated from all tasks
const countDueSoon = computed(() => {
  return (
    allTasksData.value?.filter(
      (t) =>
        t.displayStatus === 'pending' &&
        t.countdownDays !== null &&
        t.countdownDays !== undefined &&
        t.countdownDays >= 0 &&
        t.countdownDays <= 7,
    ).length ?? 0
  );
});

const countUpcoming = computed(() => {
  return (
    allTasksData.value?.filter(
      (t) =>
        t.displayStatus === 'pending' &&
        (t.countdownDays === null || t.countdownDays === undefined || t.countdownDays >= 0),
    ).length ?? 0
  );
});

const countSubmitted = computed(() => {
  return (
    allTasksData.value?.filter((t) =>
      ['submitted', 'late', 'excused', 'exempted'].includes(t.displayStatus),
    ).length ?? 0
  );
});

const countMissing = computed(() => {
  return allTasksData.value?.filter((t) => t.displayStatus === 'missing').length ?? 0;
});

const countAll = computed(() => {
  return allTasksData.value?.length ?? 0;
});

// Filtered tasks by search query
const filteredTasks = computed<StudentCourseworkTask[]>(() => {
  const tasks = tasksData.value ?? [];
  const q = searchQuery.value.trim().toLowerCase();
  if (!q) return tasks;

  return tasks.filter(
    (t) =>
      t.title.toLowerCase().includes(q) ||
      t.learningUnitName.toLowerCase().includes(q) ||
      t.learningUnitCode.toLowerCase().includes(q) ||
      t.learningActivityTitle.toLowerCase().includes(q) ||
      (t.description && t.description.toLowerCase().includes(q)),
  );
});

// ---------------------------------------------------------------------------
// 3. Selection & Modal/Drawer State
// ---------------------------------------------------------------------------
const selectedTask = ref<StudentCourseworkTask | null>(null);
const isSubmissionModalOpen = ref(false);
const isHistoryDrawerOpen = ref(false);

function handleOpenSubmitModal(task: StudentCourseworkTask) {
  selectedTask.value = task;
  isSubmissionModalOpen.value = true;
}

function handleOpenHistoryDrawer(task: StudentCourseworkTask) {
  selectedTask.value = task;
  isHistoryDrawerOpen.value = true;
}

function handleResubmitFromDrawer(task: StudentCourseworkTask) {
  selectedTask.value = task;
  isHistoryDrawerOpen.value = false;
  isSubmissionModalOpen.value = true;
}

function handleCourseworkSubmitted(_response: SubmitCourseworkResponse) {
  refetch();
  toast.add({
    severity: 'success',
    summary: 'Deliverable Received',
    detail: 'Your coursework was successfully recorded in the SIS repository.',
    life: 5000,
  });
}

// ---------------------------------------------------------------------------
// 4. Deadline Countdown Formatting Helper
// ---------------------------------------------------------------------------
interface CountdownBadge {
  text: string;
  badgeClass: string;
  icon: typeof Clock | typeof AlertTriangle;
}

function getCountdownBadge(task: StudentCourseworkTask): CountdownBadge {
  if (task.countdownDays === null || task.countdownDays === undefined) {
    return {
      text: 'No deadline',
      badgeClass: 'bg-slate-100 text-slate-600 border-slate-200',
      icon: Clock,
    };
  }

  const days = task.countdownDays;
  if (days < 0) {
    const absDays = Math.abs(days);
    return {
      text: `Overdue by ${absDays} ${absDays === 1 ? 'day' : 'days'}`,
      badgeClass: 'bg-rose-50 text-rose-700 border-rose-200',
      icon: AlertTriangle,
    };
  }

  if (days === 0) {
    return {
      text: 'Due today',
      badgeClass: 'bg-amber-50 text-amber-800 border-amber-300 font-bold',
      icon: Clock,
    };
  }

  if (days === 1) {
    return {
      text: 'Due tomorrow',
      badgeClass: 'bg-amber-50 text-amber-700 border-amber-200',
      icon: Clock,
    };
  }

  if (days <= 3) {
    return {
      text: `Due in ${days} days`,
      badgeClass: 'bg-amber-50 text-amber-700 border-amber-200',
      icon: Clock,
    };
  }

  return {
    text: `Due in ${days} days`,
    badgeClass: 'bg-teal-50 text-teal-700 border-teal-200',
    icon: Clock,
  };
}
</script>

<template>
  <div class="space-y-6" data-testid="student-learning-workspace-view">
    <!-- Page Header -->
    <PageHeader
      title="Student Learning Workspace"
      subtitle="Inspect coursework assignments, track deadlines, and submit deliverables (SCR-SELF-03)."
      :breadcrumbs="breadcrumbs"
    />

    <!-- Summary Metrics Bar -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4" data-testid="workspace-metrics-bar">
      <!-- Metric 1: Due Soon -->
      <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-sm flex items-center gap-3.5">
        <div class="w-10 h-10 rounded-lg bg-amber-50 border border-amber-200 flex items-center justify-center text-amber-600 shrink-0">
          <Clock class="w-5 h-5" />
        </div>
        <div>
          <div class="text-xl font-bold text-slate-900 leading-tight">
            {{ countDueSoon }}
          </div>
          <div class="text-xs font-medium text-slate-500 mt-0.5">
            Due Soon (7 Days)
          </div>
        </div>
      </div>

      <!-- Metric 2: Missing -->
      <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-sm flex items-center gap-3.5">
        <div class="w-10 h-10 rounded-lg bg-rose-50 border border-rose-200 flex items-center justify-center text-rose-600 shrink-0">
          <AlertTriangle class="w-5 h-5" />
        </div>
        <div>
          <div class="text-xl font-bold text-slate-900 leading-tight">
            {{ countMissing }}
          </div>
          <div class="text-xs font-medium text-slate-500 mt-0.5">
            Missing Tasks
          </div>
        </div>
      </div>

      <!-- Metric 3: Submitted -->
      <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-sm flex items-center gap-3.5">
        <div class="w-10 h-10 rounded-lg bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-600 shrink-0">
          <CheckCircle2 class="w-5 h-5" />
        </div>
        <div>
          <div class="text-xl font-bold text-slate-900 leading-tight">
            {{ countSubmitted }}
          </div>
          <div class="text-xs font-medium text-slate-500 mt-0.5">
            Completed / Submitted
          </div>
        </div>
      </div>

      <!-- Metric 4: Total Coursework -->
      <div class="p-4 rounded-xl bg-white border border-slate-200 shadow-sm flex items-center gap-3.5">
        <div class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-600 shrink-0">
          <BookOpen class="w-5 h-5" />
        </div>
        <div>
          <div class="text-xl font-bold text-slate-900 leading-tight">
            {{ countAll }}
          </div>
          <div class="text-xs font-medium text-slate-500 mt-0.5">
            Active Assignments
          </div>
        </div>
      </div>
    </div>

    <!-- Master Filter Tabs & Search Controls Container -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-4 space-y-4">
      <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
        <!-- PrimeVue Filter Tabs -->
        <Tabs v-model:value="activeFilter" class="w-full md:w-auto">
          <TabList class="border-b border-transparent">
            <!-- Tab 1: Upcoming -->
            <Tab value="upcoming" data-testid="tab-upcoming">
              <div class="flex items-center gap-2 py-0.5">
                <span class="text-xs font-medium">Upcoming</span>
                <span
                  v-if="countUpcoming > 0"
                  class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold bg-teal-100 text-teal-700"
                >
                  {{ countUpcoming }}
                </span>
              </div>
            </Tab>

            <!-- Tab 2: Due Soon -->
            <Tab value="due_soon" data-testid="tab-due-soon">
              <div class="flex items-center gap-2 py-0.5">
                <span class="text-xs font-medium">Due Soon</span>
                <span
                  v-if="countDueSoon > 0"
                  class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold bg-amber-100 text-amber-800"
                >
                  {{ countDueSoon }}
                </span>
              </div>
            </Tab>

            <!-- Tab 3: Submitted -->
            <Tab value="submitted" data-testid="tab-submitted">
              <div class="flex items-center gap-2 py-0.5">
                <span class="text-xs font-medium">Submitted</span>
                <span
                  v-if="countSubmitted > 0"
                  class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-100 text-emerald-800"
                >
                  {{ countSubmitted }}
                </span>
              </div>
            </Tab>

            <!-- Tab 4: Missing -->
            <Tab value="missing" data-testid="tab-missing">
              <div class="flex items-center gap-2 py-0.5">
                <span class="text-xs font-medium">Missing</span>
                <span
                  v-if="countMissing > 0"
                  class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold bg-rose-100 text-rose-800"
                >
                  {{ countMissing }}
                </span>
              </div>
            </Tab>

            <!-- Tab 5: All -->
            <Tab value="all" data-testid="tab-all">
              <div class="flex items-center gap-2 py-0.5">
                <span class="text-xs font-medium">All Tasks</span>
                <span
                  v-if="countAll > 0"
                  class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold bg-slate-100 text-slate-700"
                >
                  {{ countAll }}
                </span>
              </div>
            </Tab>
          </TabList>
        </Tabs>

        <!-- Search Input -->
        <div class="relative w-full md:w-72">
          <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
          <InputText
            v-model="searchQuery"
            placeholder="Search tasks, units..."
            class="w-full !pl-9 text-xs"
            data-testid="input-task-search"
          />
        </div>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="queryError"
      class="p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 flex items-start gap-3"
      data-testid="workspace-error-banner"
    >
      <AlertTriangle class="w-5 h-5 text-rose-600 mt-0.5 shrink-0" />
      <div>
        <h4 class="text-sm font-semibold text-rose-950">Failed to load coursework tasks</h4>
        <p class="text-xs text-rose-700 mt-0.5">
          {{ queryError instanceof Error ? queryError.message : 'An error occurred fetching student tasks.' }}
        </p>
        <Button
          type="button"
          label="Retry"
          size="small"
          severity="danger"
          text
          class="!p-0 !mt-2 text-xs font-semibold"
          @click="refetch"
        />
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="isLoading"
      class="py-16 flex justify-center"
      data-testid="workspace-loading-spinner"
    >
      <LoadingSpinner size="lg" message="Loading student coursework assignments..." />
    </div>

    <!-- Empty State -->
    <div
      v-else-if="filteredTasks.length === 0"
      class="py-12 bg-white rounded-xl border border-slate-200"
      data-testid="workspace-empty-state"
    >
      <EmptyState
        title="No Coursework Tasks Found"
        :description="
          searchQuery
            ? `No coursework assignments match '${searchQuery}'. Try adjusting your search query.`
            : `There are currently no coursework assignments matching filter '${activeFilter}'.`
        "
      >
        <template #actions>
          <Button
            v-if="searchQuery"
            type="button"
            label="Clear Search"
            size="small"
            severity="secondary"
            @click="searchQuery = ''"
          />
        </template>
      </EmptyState>
    </div>

    <!-- Assignment Task Cards Grid -->
    <div
      v-else
      class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5"
      data-testid="task-cards-grid"
    >
      <div
        v-for="task in filteredTasks"
        :key="task.assignmentId"
        class="bg-white border border-slate-200 hover:border-teal-300 transition-all duration-200 shadow-sm hover:shadow rounded-xl p-5 flex flex-col justify-between"
        :data-testid="`task-card-${task.assignmentId}`"
      >
        <!-- Card Header: Unit Badge & Submission Status Pill -->
        <div>
          <div class="flex items-start justify-between gap-2 mb-3">
            <div class="min-w-0">
              <span class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md bg-teal-50 border border-teal-200/80 text-[11px] font-semibold text-teal-800 truncate">
                <BookOpen class="w-3 h-3 text-teal-600 shrink-0" />
                <span class="truncate">{{ task.learningUnitName }}</span>
                <span class="font-mono text-[10px] text-teal-600">({{ task.learningUnitCode }})</span>
              </span>
            </div>

            <!-- Submission Status Badge -->
            <StatusPill
              :status="task.displayStatus"
              size="sm"
              class="shrink-0"
            />
          </div>

          <!-- Activity Context & Section Label -->
          <div class="flex items-center gap-2 text-[11px] text-slate-500 mb-1.5">
            <span class="truncate">Activity: {{ task.learningActivityTitle }}</span>
            <span
              v-if="task.sectionLabel"
              class="px-1.5 py-0.2 rounded bg-slate-100 text-slate-600 font-medium shrink-0"
            >
              {{ task.sectionLabel }}
            </span>
          </div>

          <!-- Task Title -->
          <h3 class="text-sm font-bold text-slate-900 leading-snug line-clamp-2">
            {{ task.title }}
          </h3>

          <!-- Task Description -->
          <p
            v-if="task.description"
            class="text-xs text-slate-600 line-clamp-2 mt-1.5 leading-relaxed"
          >
            {{ task.description }}
          </p>
        </div>

        <!-- Card Bottom Section: Due Date, Countdown Badge & Action Buttons -->
        <div class="pt-4 mt-4 border-t border-slate-100 space-y-3">
          <!-- Due Date & Deadline Countdown Row -->
          <div class="flex items-center justify-between text-xs gap-2">
            <!-- Due Date -->
            <div class="flex items-center gap-1.5 text-slate-600 text-[11px] min-w-0">
              <Calendar class="w-3.5 h-3.5 text-slate-400 shrink-0" />
              <span class="truncate">
                {{ task.dueDate ? formatDisplayDate(task.dueDate) : 'No due date' }}
              </span>
            </div>

            <!-- Color-Coded Deadline Countdown Badge -->
            <span
              :class="[
                'inline-flex items-center gap-1 px-2 py-0.5 rounded-full border text-[11px] font-medium shrink-0',
                getCountdownBadge(task).badgeClass,
              ]"
              :data-testid="`countdown-badge-${task.assignmentId}`"
            >
              <component :is="getCountdownBadge(task).icon" class="w-3 h-3" />
              <span>{{ getCountdownBadge(task).text }}</span>
            </span>
          </div>

          <!-- Card Action Buttons -->
          <div class="flex items-center justify-between gap-2 pt-1">
            <!-- When Task is already submitted / has submission ID -->
            <template v-if="task.displayStatus === 'submitted' || task.displayStatus === 'late' || task.submissionId">
              <Button
                type="button"
                label="View Submission"
                severity="secondary"
                size="small"
                outlined
                class="!text-xs !py-1.5 flex-1"
                :data-testid="`btn-view-submission-${task.assignmentId}`"
                @click="handleOpenHistoryDrawer(task)"
              >
                <template #icon>
                  <History class="w-3.5 h-3.5 mr-1" />
                </template>
              </Button>
              <Button
                type="button"
                label="Resubmit"
                size="small"
                class="bg-teal-600 hover:bg-teal-700 text-white !text-xs !py-1.5 flex-1 font-medium"
                :data-testid="`btn-resubmit-${task.assignmentId}`"
                @click="handleOpenSubmitModal(task)"
              >
                <template #icon>
                  <RotateCcw class="w-3.5 h-3.5 mr-1" />
                </template>
              </Button>
            </template>

            <!-- When Task is Pending or Missing (Unsubmitted) -->
            <template v-else>
              <Button
                type="button"
                label="Submit Work"
                size="small"
                class="bg-teal-600 hover:bg-teal-700 text-white !text-xs !py-1.5 w-full font-medium"
                :data-testid="`btn-submit-work-${task.assignmentId}`"
                @click="handleOpenSubmitModal(task)"
              >
                <template #icon>
                  <Send class="w-3.5 h-3.5 mr-1.5" />
                </template>
              </Button>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Coursework Submission Modal -->
    <CourseworkSubmissionModal
      v-model:visible="isSubmissionModalOpen"
      :task="selectedTask"
      :student-id="effectiveStudentId"
      @submitted="handleCourseworkSubmitted"
    />

    <!-- Submission History Drawer -->
    <SubmissionHistoryDrawer
      v-model:visible="isHistoryDrawerOpen"
      :task="selectedTask"
      @resubmit="handleResubmitFromDrawer"
    />
  </div>
</template>
