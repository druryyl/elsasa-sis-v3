<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import InputText from 'primevue/inputtext';
import SelectButton from 'primevue/selectbutton';
import {
  Calendar as CalendarIcon,
  ChevronLeft,
  ChevronRight,
  PlusCircle,
  Clock,
  CheckCircle2,
  Users,
  FileSpreadsheet,
  FilePlus,
  BookOpen,
  Filter,
  Sparkles,
  School,
  AlertCircle,
  RotateCw,
} from 'lucide-vue-next';
import { PageHeader, EmptyState, LoadingSpinner } from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import { useTeacherDailyActivitiesQuery } from '../composables/useLearningActivities';
import PlanActivityModal from '../components/PlanActivityModal.vue';
import ConductActivityModal from '../components/ConductActivityModal.vue';
import SessionAttendanceGrid from '../components/SessionAttendanceGrid.vue';
import type { TeacherDailyActivity } from '../types';

// --------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// --------------------------------------------------------------------------
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Class Sessions & Attendance', current: true },
];

// --------------------------------------------------------------------------
// 2. Authorization
// --------------------------------------------------------------------------
const authStore = useAuthStore();
const canConduct = computed(() => {
  return (
    authStore.hasPermission('academics.activities.conduct') ||
    authStore.hasPermission('academics.activities.manage') ||
    authStore.user?.roles?.includes('Teacher') ||
    authStore.user?.roles?.includes('Admin')
  );
});

// --------------------------------------------------------------------------
// 3. Date Navigation & Calendar Selector
// --------------------------------------------------------------------------
function formatDateIso(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

const selectedDate = ref<string>(formatDateIso(new Date()));

const friendlyDateLabel = computed(() => {
  if (!selectedDate.value) return '';
  const [year, month, day] = selectedDate.value.split('-').map(Number);
  const dateObj = new Date(year, month - 1, day);
  return dateObj.toLocaleDateString(undefined, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
});

const isSelectedDateToday = computed(() => {
  return selectedDate.value === formatDateIso(new Date());
});

function goToPreviousDay() {
  const [year, month, day] = selectedDate.value.split('-').map(Number);
  const prev = new Date(year, month - 1, day - 1);
  selectedDate.value = formatDateIso(prev);
}

function goToNextDay() {
  const [year, month, day] = selectedDate.value.split('-').map(Number);
  const next = new Date(year, month - 1, day + 1);
  selectedDate.value = formatDateIso(next);
}

function goToToday() {
  selectedDate.value = formatDateIso(new Date());
}

// --------------------------------------------------------------------------
// 4. Session Filter & Queries
// --------------------------------------------------------------------------
const filterMode = ref<'all' | 'homeroom'>('all');
const filterOptions = [
  { label: 'All Sessions', value: 'all' },
  { label: 'Homeroom Daily Roll Call', value: 'homeroom' },
];

const {
  data: activitiesData,
  isLoading,
  isFetching,
  refetch,
} = useTeacherDailyActivitiesQuery(selectedDate);

const activities = computed(() => activitiesData.value ?? []);

const filteredActivities = computed(() => {
  if (filterMode.value === 'homeroom') {
    return activities.value.filter((a) => a.activityType === 'homeroom_session');
  }
  return activities.value;
});

// --------------------------------------------------------------------------
// 5. Active Modals & Selection State
// --------------------------------------------------------------------------
const isPlanModalOpen = ref(false);

const isConductModalOpen = ref(false);
const activityForConducting = ref<TeacherDailyActivity | null>(null);

const activeAttendanceSession = ref<TeacherDailyActivity | null>(null);

function openPlanModal() {
  isPlanModalOpen.value = true;
}

function openConductModal(activity: TeacherDailyActivity) {
  activityForConducting.value = activity;
  isConductModalOpen.value = true;
}

function openAttendanceView(activity: TeacherDailyActivity) {
  activeAttendanceSession.value = activity;
}

function closeAttendanceView() {
  activeAttendanceSession.value = null;
}

function navigateToCreateAssignment(activity: TeacherDailyActivity) {
  router.push({
    path: '/academics/coursework',
    query: {
      activityId: activity.id,
      unitId: activity.learningUnitId,
    },
  });
}

function onActivityPlanned() {
  refetch();
}

function onActivityConducted() {
  refetch();
}

function getActivityTypePill(type: string): { label: string; icon: any; isHomeroom: boolean } {
  if (type === 'homeroom_session') {
    return {
      label: 'Homeroom Session',
      icon: School,
      isHomeroom: true,
    };
  }
  return {
    label: type
      .split('_')
      .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' '),
    icon: BookOpen,
    isHomeroom: false,
  };
}

function getStatusSeverity(status: string): 'info' | 'success' | 'warn' | 'danger' | 'secondary' {
  switch (status) {
    case 'planned':
      return 'info';
    case 'conducted':
      return 'success';
    case 'cancelled':
      return 'danger';
    default:
      return 'secondary';
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader
      title="Class Session Workbench"
      subtitle="Schedule instructional sessions, record curriculum milestones, take daily roll-call attendance, and manage auditable corrections"
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-3">
          <Button
            type="button"
            icon="pi pi-refresh"
            severity="secondary"
            variant="outlined"
            :loading="isFetching"
            @click="refetch"
            title="Refresh schedule"
          />
          <Button
            type="button"
            label="Plan Session"
            icon="pi pi-plus"
            class="bg-indigo-600 hover:bg-indigo-700 text-white font-medium"
            @click="openPlanModal"
          />
        </div>
      </template>
    </PageHeader>

    <!-- Daily Calendar / Date Navigator Control Bar -->
    <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
      <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <!-- Date Navigator Controls -->
        <div class="flex flex-wrap items-center gap-2">
          <div class="inline-flex rounded-lg border border-slate-200 bg-slate-50 p-0.5">
            <Button
              type="button"
              icon="pi pi-chevron-left"
              severity="secondary"
              text
              size="small"
              @click="goToPreviousDay"
              title="Previous Day"
            />
            <Button
              type="button"
              label="Today"
              severity="secondary"
              text
              size="small"
              :class="isSelectedDateToday ? 'font-bold text-indigo-700 bg-indigo-50/70' : 'text-slate-700'"
              @click="goToToday"
            />
            <Button
              type="button"
              icon="pi pi-chevron-right"
              severity="secondary"
              text
              size="small"
              @click="goToNextDay"
              title="Next Day"
            />
          </div>

          <div class="flex items-center gap-2">
            <InputText
              v-model="selectedDate"
              type="date"
              class="text-xs h-9"
            />
            <span class="text-sm font-semibold text-slate-800 ml-1">
              {{ friendlyDateLabel }}
            </span>
          </div>
        </div>

        <!-- Filter Mode: All Sessions vs Homeroom Roll Call -->
        <div class="flex items-center gap-3">
          <SelectButton
            v-model="filterMode"
            :options="filterOptions"
            optionLabel="label"
            optionValue="value"
            size="small"
            class="text-xs"
          />
        </div>
      </div>
    </div>

    <!-- Active Attendance Roll-Call Panel (If a session is opened for attendance) -->
    <div
      v-if="activeAttendanceSession"
      class="rounded-xl border border-indigo-200 bg-white p-5 shadow-sm space-y-4 ring-2 ring-indigo-500/20"
    >
      <div class="flex flex-wrap items-start justify-between gap-3 pb-3 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-2 mb-1">
            <Tag value="ROLL-CALL ATTENDANCE WORKBENCH" severity="info" class="text-[10px]" />
            <Tag
              v-if="activeAttendanceSession.activityType === 'homeroom_session'"
              value="HOMEROOM STATUTORY SESSION"
              class="bg-indigo-700 text-white text-[10px]"
            />
          </div>
          <h2 class="text-lg font-bold text-slate-900">
            {{ activeAttendanceSession.title }}
          </h2>
          <p class="text-xs text-slate-500 font-mono">
            {{ activeAttendanceSession.learningUnitCode }} — {{ activeAttendanceSession.learningUnitName }}
            <span v-if="activeAttendanceSession.sectionLabel" class="ml-2 font-sans text-indigo-700 font-semibold bg-indigo-50 px-1.5 py-0.5 rounded border border-indigo-100">
              Section {{ activeAttendanceSession.sectionLabel }}
            </span>
            <span v-if="activeAttendanceSession.startTime || activeAttendanceSession.endTime" class="ml-3 font-sans text-slate-600">
              {{ activeAttendanceSession.startTime ?? '' }} – {{ activeAttendanceSession.endTime ?? '' }}
            </span>
          </p>
        </div>

        <Button
          type="button"
          label="Close Roll-Call View"
          icon="pi pi-times"
          severity="secondary"
          variant="outlined"
          size="small"
          @click="closeAttendanceView"
        />
      </div>

      <!-- Attendance Grid -->
      <SessionAttendanceGrid
        :activity="activeAttendanceSession"
        @saved="refetch"
      />
    </div>

    <!-- Daily Scheduled Sessions List -->
    <div v-if="isLoading" class="flex justify-center py-16">
      <LoadingSpinner message="Loading daily instructional sessions..." />
    </div>

    <div v-else-if="filteredActivities.length === 0" class="py-12">
      <EmptyState
        title="No class sessions scheduled for this date"
        :description="filterMode === 'homeroom' ? 'No homeroom statutory sessions found for ' + friendlyDateLabel + '.' : 'No instructional activities or sessions planned for ' + friendlyDateLabel + '.'"
      >
        <template #actions>
          <Button
            type="button"
            label="Plan a Session for Today"
            icon="pi pi-plus"
            class="bg-indigo-600 hover:bg-indigo-700 text-white font-medium"
            @click="openPlanModal"
          />
        </template>
      </EmptyState>
    </div>

    <div v-else class="space-y-4">
      <div class="flex items-center justify-between text-xs text-slate-500 px-1">
        <span>Showing {{ filteredActivities.length }} session(s) scheduled for {{ friendlyDateLabel }}</span>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div
          v-for="act in filteredActivities"
          :key="act.id"
          class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs hover:border-slate-300 transition-all flex flex-col justify-between space-y-4"
          :class="{
            'ring-2 ring-indigo-500/40 bg-indigo-50/20': activeAttendanceSession?.id === act.id,
            'border-l-4 border-l-indigo-600': act.activityType === 'homeroom_session',
          }"
        >
          <!-- Top Row: Title, Badges, Types -->
          <div class="space-y-2">
            <div class="flex items-start justify-between gap-3">
              <div class="flex items-center gap-2 flex-wrap">
                <!-- Activity Type Pill -->
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold"
                  :class="getActivityTypePill(act.activityType).isHomeroom ? 'bg-indigo-100 text-indigo-900 border border-indigo-200' : 'bg-slate-100 text-slate-700'"
                >
                  <component
                    :is="getActivityTypePill(act.activityType).icon"
                    class="h-3.5 w-3.5 text-indigo-600"
                  />
                  {{ getActivityTypePill(act.activityType).label }}
                </span>

                <!-- Section Label -->
                <span
                  v-if="act.sectionLabel"
                  class="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-indigo-50 text-indigo-800 border border-indigo-100"
                >
                  Section {{ act.sectionLabel }}
                </span>
              </div>

              <!-- Status Badge -->
              <Tag
                :value="act.status.toUpperCase()"
                :severity="getStatusSeverity(act.status)"
                class="text-[10px]"
              />
            </div>

            <!-- Title & Unit Header -->
            <h3 class="text-base font-bold text-slate-900 leading-snug">
              {{ act.title }}
            </h3>

            <p class="text-xs text-slate-600 font-mono">
              {{ act.learningUnitCode }} — {{ act.learningUnitName }}
            </p>

            <!-- Time & Provisional Teacher Badges -->
            <div class="flex flex-wrap items-center gap-4 text-xs text-slate-500 pt-2 border-t border-slate-100">
              <div v-if="act.startTime || act.endTime" class="flex items-center gap-1.5 font-medium text-slate-700">
                <Clock class="h-3.5 w-3.5 text-slate-400" />
                <span>{{ act.startTime ?? '—' }} – {{ act.endTime ?? '—' }}</span>
              </div>
              <div v-else class="text-slate-400 italic">
                Unscheduled time
              </div>

              <div v-if="act.teacherName" class="flex items-center gap-1.5">
                <span class="text-slate-400">Teacher:</span>
                <span class="font-medium text-slate-700">{{ act.teacherName }}</span>
              </div>
            </div>

            <!-- Delivery Notes Snippet (If Conducted) -->
            <div
              v-if="act.deliveryNotes"
              class="rounded-lg bg-emerald-50/70 border border-emerald-100 p-2.5 text-xs text-emerald-950 space-y-1"
            >
              <div class="font-semibold text-emerald-900 flex items-center gap-1.5">
                <CheckCircle2 class="h-3.5 w-3.5 text-emerald-600" />
                <span>Curriculum Milestones Completed:</span>
              </div>
              <p class="italic text-emerald-800 text-[11px] line-clamp-2">
                "{{ act.deliveryNotes }}"
              </p>
            </div>
          </div>

          <!-- Bottom Action Buttons Strip -->
          <div class="flex flex-wrap items-center justify-between gap-2 pt-3 border-t border-slate-100">
            <!-- Left secondary action: Create Assignment -->
            <Button
              type="button"
              label="Create Assignment"
              icon="pi pi-file-plus"
              severity="secondary"
              text
              size="small"
              class="text-xs text-slate-600 hover:text-indigo-600"
              @click="navigateToCreateAssignment(act)"
            />

            <!-- Right primary action buttons -->
            <div class="flex items-center gap-2">
              <Button
                v-if="act.status === 'planned'"
                type="button"
                label="Conduct Session"
                icon="pi pi-check-circle"
                severity="success"
                variant="outlined"
                size="small"
                class="text-xs"
                @click="openConductModal(act)"
              />

              <Button
                v-if="act.isAttendanceRequired"
                type="button"
                :label="activeAttendanceSession?.id === act.id ? 'Viewing Roll Call' : 'Take Attendance'"
                icon="pi pi-users"
                size="small"
                :class="activeAttendanceSession?.id === act.id ? 'bg-indigo-800 text-white font-medium' : 'bg-indigo-600 hover:bg-indigo-700 text-white font-medium'"
                @click="openAttendanceView(act)"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <PlanActivityModal
      v-model:visible="isPlanModalOpen"
      :initialDate="selectedDate"
      @planned="onActivityPlanned"
    />

    <ConductActivityModal
      v-model:visible="isConductModalOpen"
      :activity="activityForConducting"
      @conducted="onActivityConducted"
    />
  </div>
</template>
