<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import InputText from 'primevue/inputtext';
import {
  Users,
  CheckCircle2,
  XCircle,
  Clock,
  ShieldCheck,
  ShieldAlert,
  AlertTriangle,
  History,
  Save,
  Search,
  Sparkles,
  School,
} from 'lucide-vue-next';
import { LoadingSpinner, EmptyState } from '@/shared/components';
import {
  useActivityAttendanceQuery,
  useRecordAttendanceMutation,
} from '../composables/useAttendance';
import { useUnitParticipationsQuery } from '@/features/academics/participation/composables/useAcademicParticipation';
import AttendanceCorrectionDrawer from './AttendanceCorrectionDrawer.vue';
import type {
  ActivityDetail,
  AttendanceRecord,
  AttendanceStatus,
  LearningActivity,
  StudentAttendanceEntry,
  TeacherDailyActivity,
} from '../types';

export interface AttendanceRowModel {
  studentId: string;
  studentNumber: string;
  studentName: string;
  status: AttendanceStatus | string;
  recordId?: string | null;
  recordedBy?: string | null;
  recordedDate?: string | null;
  evidence?: string | null;
  notes?: string | null;
  correctionsCount: number;
  rawRecord?: AttendanceRecord | null;
}

const props = defineProps<{
  activity: TeacherDailyActivity | ActivityDetail | LearningActivity;
}>();

const emit = defineEmits<{
  (e: 'saved'): void;
}>();

// --------------------------------------------------------------------------
// 1. Grace Window & Homeroom Calculation
// --------------------------------------------------------------------------
function getTodayIso(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const isHomeroomSession = computed(() => {
  return props.activity.activityType === 'homeroom_session';
});

const isGraceWindowActive = computed(() => {
  const today = getTodayIso();
  if (props.activity.activityDate === today) {
    return true;
  }
  if ('createdAt' in props.activity && props.activity.createdAt) {
    const createdTime = new Date(props.activity.createdAt).getTime();
    const twentyFourHoursMs = 24 * 60 * 60 * 1000;
    if (Date.now() - createdTime <= twentyFourHoursMs) {
      return true;
    }
  }
  return false;
});

// --------------------------------------------------------------------------
// 2. Data Queries (Existing Records + Enrolled Cohort Roster)
// --------------------------------------------------------------------------
const activityIdRef = computed(() => props.activity.id);
const {
  data: attendanceRecords,
  isLoading: isLoadingAttendance,
  refetch: refetchAttendance,
} = useActivityAttendanceQuery(activityIdRef);

const unitIdRef = computed(() => props.activity.learningUnitId);
const sectionFilterRef = computed(() => ({
  sectionLabel: props.activity.sectionLabel ?? undefined,
}));

const { data: unitParticipations, isLoading: isLoadingRoster } =
  useUnitParticipationsQuery(unitIdRef, sectionFilterRef, {
    enabled: computed(() => Boolean(props.activity.learningUnitId)),
  });

// --------------------------------------------------------------------------
// 3. Unified Roster State
// --------------------------------------------------------------------------
const searchQuery = ref('');
const localRoster = ref<AttendanceRowModel[]>([]);
const hasUnsavedChanges = ref(false);

// Initialize / sync unified roster from queries
watch(
  [attendanceRecords, unitParticipations],
  ([records, participations]) => {
    const map = new Map<string, AttendanceRowModel>();

    // 1. First add enrolled cohort students (if available)
    if (participations) {
      for (const p of participations) {
        if (p.status === 'in_progress' || p.status === 'completed') {
          map.set(p.studentId, {
            studentId: p.studentId,
            studentNumber: p.studentNumber ?? '',
            studentName: p.studentName ?? `Student (${p.studentId.slice(0, 8)})`,
            status: 'present', // Default candidate status for roll call
            recordId: null,
            correctionsCount: 0,
            rawRecord: null,
          });
        }
      }
    }

    // 2. Overlay existing recorded attendance
    if (records) {
      for (const r of records) {
        map.set(r.studentId, {
          studentId: r.studentId,
          studentNumber: r.studentNumber ?? '',
          studentName: r.studentName ?? `Student (${r.studentId.slice(0, 8)})`,
          status: r.status,
          recordId: r.id,
          recordedBy: r.recordedBy,
          recordedDate: r.recordedDate,
          evidence: r.evidence,
          notes: r.notes,
          correctionsCount: r.corrections?.length ?? 0,
          rawRecord: r,
        });
      }
    }

    localRoster.value = Array.from(map.values()).sort((a, b) =>
      a.studentName.localeCompare(b.studentName),
    );
    hasUnsavedChanges.value = false;
  },
  { immediate: true },
);

// --------------------------------------------------------------------------
// 4. Filtered Rows & Aggregates
// --------------------------------------------------------------------------
const filteredRoster = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return localRoster.value;

  return localRoster.value.filter(
    (row) =>
      row.studentName.toLowerCase().includes(query) ||
      row.studentNumber.toLowerCase().includes(query),
  );
});

const summaryStats = computed(() => {
  const total = localRoster.value.length;
  let present = 0;
  let absent = 0;
  let late = 0;
  let excused = 0;
  let partial = 0;

  for (const r of localRoster.value) {
    if (r.status === 'present') present++;
    else if (r.status === 'absent') absent++;
    else if (r.status === 'late') late++;
    else if (r.status === 'excused_absence') excused++;
    else if (r.status === 'partial') partial++;
  }

  return { total, present, absent, late, excused, partial };
});

// --------------------------------------------------------------------------
// 5. Roll-Call Status Modification
// --------------------------------------------------------------------------
const selectedRecordForCorrection = ref<AttendanceRecord | null>(null);
const isCorrectionDrawerOpen = ref(false);

function setStudentStatus(studentId: string, newStatus: AttendanceStatus) {
  if (!isGraceWindowActive.value) {
    // If grace window expired, prompt audit correction drawer
    openCorrectionForStudent(studentId);
    return;
  }

  const row = localRoster.value.find((r) => r.studentId === studentId);
  if (row && row.status !== newStatus) {
    row.status = newStatus;
    hasUnsavedChanges.value = true;
  }
}

function markAllPresent() {
  if (!isGraceWindowActive.value) return;

  for (const r of localRoster.value) {
    r.status = 'present';
  }
  hasUnsavedChanges.value = true;
}

function openCorrectionForStudent(studentId: string) {
  const row = localRoster.value.find((r) => r.studentId === studentId);
  if (!row) return;

  if (row.rawRecord) {
    selectedRecordForCorrection.value = row.rawRecord;
  } else {
    // Synthetic record for opening drawer
    selectedRecordForCorrection.value = {
      id: row.recordId ?? '',
      learningActivityId: props.activity.id,
      studentId: row.studentId,
      studentNumber: row.studentNumber,
      studentName: row.studentName,
      status: row.status,
      recordedBy: row.recordedBy ?? 'System',
      recordedDate: row.recordedDate ?? props.activity.activityDate,
      evidence: row.evidence ?? null,
      notes: row.notes ?? null,
      createdAt: new Date().toISOString(),
      createdBy: 'System',
      corrections: [],
    };
  }
  isCorrectionDrawerOpen.value = true;
}

function onCorrectionComplete() {
  refetchAttendance();
  hasUnsavedChanges.value = false;
}

// --------------------------------------------------------------------------
// 6. Save Attendance Roll Call
// --------------------------------------------------------------------------
const recordMutation = useRecordAttendanceMutation();

function saveAttendance() {
  if (!isGraceWindowActive.value) return;

  const entries: StudentAttendanceEntry[] = localRoster.value.map((r) => ({
    studentId: r.studentId,
    status: r.status,
    notes: r.notes ?? null,
    evidence: r.evidence ?? null,
  }));

  if (entries.length === 0) return;

  recordMutation.mutate(
    {
      activityId: props.activity.id,
      request: { entries },
    },
    {
      onSuccess: () => {
        hasUnsavedChanges.value = false;
        refetchAttendance();
        emit('saved');
      },
    },
  );
}
</script>

<template>
  <div class="space-y-4">
    <!-- Header / Context Bar -->
    <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs space-y-3">
      <!-- Statutory Homeroom Banner if applicable -->
      <div
        v-if="isHomeroomSession"
        class="flex items-start gap-2.5 rounded-lg border border-indigo-200 bg-indigo-50/90 p-3 text-xs text-indigo-900"
      >
        <School class="h-4 w-4 shrink-0 text-indigo-600 mt-0.5" />
        <div>
          <span class="font-semibold text-indigo-950">Statutory Homeroom Daily Attendance Session:</span>
          Roll-call entries in this session satisfy institutional and statutory daily attendance requirements.
        </div>
      </div>

      <!-- Grace Window Indicator Banner -->
      <div
        v-if="isGraceWindowActive"
        class="flex items-center justify-between gap-3 rounded-lg border border-emerald-200 bg-emerald-50/80 px-3.5 py-2.5 text-xs text-emerald-900"
      >
        <div class="flex items-center gap-2">
          <ShieldCheck class="h-4 w-4 text-emerald-600" />
          <span>
            <strong class="font-semibold text-emerald-950">Grace Window Active:</strong>
            Same-day live roll call enabled. Status changes save directly to the roster.
          </span>
        </div>
        <Tag value="LIVE ROLL CALL" severity="success" class="text-[10px] tracking-wider" />
      </div>

      <div
        v-else
        class="flex items-center justify-between gap-3 rounded-lg border border-amber-200 bg-amber-50/80 px-3.5 py-2.5 text-xs text-amber-900"
      >
        <div class="flex items-center gap-2">
          <ShieldAlert class="h-4 w-4 text-amber-600" />
          <span>
            <strong class="font-semibold text-amber-950">Grace Window Expired (Audit Mode):</strong>
            Direct roll-call updates are locked. Individual student corrections write an auditable trail with mandatory justification.
          </span>
        </div>
        <Tag value="AUDIT LOCKED" severity="warn" class="text-[10px] tracking-wider" />
      </div>

      <!-- Aggregated Metric Chips & Actions -->
      <div class="flex flex-wrap items-center justify-between gap-3 pt-1 border-t border-slate-100">
        <div class="flex flex-wrap items-center gap-2 text-xs">
          <span class="font-semibold text-slate-700">Roster ({{ summaryStats.total }}):</span>
          <span class="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 font-medium text-emerald-700 border border-emerald-200">
            Present: {{ summaryStats.present }}
          </span>
          <span class="inline-flex items-center gap-1 rounded-full bg-rose-50 px-2 py-0.5 font-medium text-rose-700 border border-rose-200">
            Absent: {{ summaryStats.absent }}
          </span>
          <span class="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 font-medium text-amber-700 border border-amber-200">
            Late: {{ summaryStats.late }}
          </span>
          <span class="inline-flex items-center gap-1 rounded-full bg-sky-50 px-2 py-0.5 font-medium text-sky-700 border border-sky-200">
            Excused: {{ summaryStats.excused }}
          </span>
          <span class="inline-flex items-center gap-1 rounded-full bg-purple-50 px-2 py-0.5 font-medium text-purple-700 border border-purple-200">
            Partial: {{ summaryStats.partial }}
          </span>
        </div>

        <div class="flex items-center gap-2">
          <Button
            v-if="isGraceWindowActive"
            type="button"
            label="Mark All Present"
            icon="pi pi-check-circle"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="markAllPresent"
          />
          <Button
            v-if="isGraceWindowActive"
            type="button"
            :label="hasUnsavedChanges ? 'Save Changes' : 'Save Attendance'"
            icon="pi pi-save"
            size="small"
            :class="hasUnsavedChanges ? 'bg-indigo-600 hover:bg-indigo-700 text-white font-medium ring-2 ring-indigo-400/40' : 'bg-slate-800 hover:bg-slate-900 text-white'"
            :loading="recordMutation.isPending.value"
            @click="saveAttendance"
          />
        </div>
      </div>
    </div>

    <!-- Search & Quick Filter Bar -->
    <div class="flex items-center justify-between gap-3">
      <div class="relative w-full max-w-sm">
        <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
        <InputText
          v-model="searchQuery"
          placeholder="Search by student name or ID..."
          class="w-full pl-9 text-xs"
        />
      </div>
      <div v-if="hasUnsavedChanges" class="text-xs font-semibold text-amber-600 flex items-center gap-1">
        <AlertTriangle class="h-3.5 w-3.5" />
        <span>Unsaved roll-call changes</span>
      </div>
    </div>

    <!-- Roster Grid Table -->
    <div v-if="isLoadingAttendance || isLoadingRoster" class="flex justify-center py-12">
      <LoadingSpinner message="Loading session roll-call roster..." />
    </div>

    <div v-else-if="filteredRoster.length === 0" class="py-8">
      <EmptyState
        title="No students found"
        :description="searchQuery ? 'No enrolled student matched your search query.' : 'No active student participations recorded for this learning unit and section.'"
      />
    </div>

    <div v-else class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
      <table class="w-full text-left text-xs text-slate-600 border-collapse">
        <thead class="bg-slate-50 text-[11px] font-semibold uppercase tracking-wider text-slate-500 border-b border-slate-200">
          <tr>
            <th class="py-3 px-4">#</th>
            <th class="py-3 px-4">Student</th>
            <th class="py-3 px-4">Current Status</th>
            <th class="py-3 px-4 text-center">One-Click Roll Call</th>
            <th class="py-3 px-4 text-right">Audit & Corrections</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr
            v-for="(row, idx) in filteredRoster"
            :key="row.studentId"
            class="hover:bg-slate-50/70 transition-colors"
          >
            <!-- Index -->
            <td class="py-3 px-4 text-slate-400 font-mono text-[11px]">
              {{ idx + 1 }}
            </td>

            <!-- Student Name & ID -->
            <td class="py-3 px-4">
              <div class="font-semibold text-slate-900 text-sm">
                {{ row.studentName }}
              </div>
              <div v-if="row.studentNumber" class="text-slate-400 font-mono text-[11px]">
                {{ row.studentNumber }}
              </div>
            </td>

            <!-- Current Status Badge -->
            <td class="py-3 px-4">
              <span
                v-if="row.status === 'present'"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800"
              >
                <CheckCircle2 class="h-3.5 w-3.5 text-emerald-600" />
                Present
              </span>
              <span
                v-else-if="row.status === 'absent'"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-rose-100 text-rose-800"
              >
                <XCircle class="h-3.5 w-3.5 text-rose-600" />
                Absent
              </span>
              <span
                v-else-if="row.status === 'late'"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-100 text-amber-800"
              >
                <Clock class="h-3.5 w-3.5 text-amber-600" />
                Late
              </span>
              <span
                v-else-if="row.status === 'excused_absence'"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-sky-100 text-sky-800"
              >
                <ShieldCheck class="h-3.5 w-3.5 text-sky-600" />
                Excused
              </span>
              <span
                v-else-if="row.status === 'partial'"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-purple-100 text-purple-800"
              >
                Partial
              </span>
              <span
                v-else
                class="inline-flex items-center px-2 py-0.5 rounded text-xs bg-slate-100 text-slate-500"
              >
                {{ row.status }}
              </span>
            </td>

            <!-- One-Click Roll Call Pills -->
            <td class="py-3 px-4 text-center">
              <div class="inline-flex items-center rounded-lg border border-slate-200 bg-slate-50 p-0.5 gap-1">
                <button
                  type="button"
                  title="Mark Present"
                  class="px-2 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="row.status === 'present' ? 'bg-emerald-600 text-white shadow-xs' : 'text-slate-600 hover:bg-slate-200'"
                  @click="setStudentStatus(row.studentId, 'present')"
                >
                  Present
                </button>
                <button
                  type="button"
                  title="Mark Absent"
                  class="px-2 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="row.status === 'absent' ? 'bg-rose-600 text-white shadow-xs' : 'text-slate-600 hover:bg-slate-200'"
                  @click="setStudentStatus(row.studentId, 'absent')"
                >
                  Absent
                </button>
                <button
                  type="button"
                  title="Mark Late"
                  class="px-2 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="row.status === 'late' ? 'bg-amber-600 text-white shadow-xs' : 'text-slate-600 hover:bg-slate-200'"
                  @click="setStudentStatus(row.studentId, 'late')"
                >
                  Late
                </button>
                <button
                  type="button"
                  title="Mark Excused"
                  class="px-2 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="row.status === 'excused_absence' ? 'bg-sky-600 text-white shadow-xs' : 'text-slate-600 hover:bg-slate-200'"
                  @click="setStudentStatus(row.studentId, 'excused_absence')"
                >
                  Excused
                </button>
                <button
                  type="button"
                  title="Mark Partial"
                  class="px-2 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="row.status === 'partial' ? 'bg-purple-600 text-white shadow-xs' : 'text-slate-600 hover:bg-slate-200'"
                  @click="setStudentStatus(row.studentId, 'partial')"
                >
                  Partial
                </button>
              </div>
            </td>

            <!-- Audit Trail & Correction Button -->
            <td class="py-3 px-4 text-right">
              <div class="flex items-center justify-end gap-2">
                <span
                  v-if="row.correctionsCount > 0"
                  class="inline-flex items-center gap-1 text-[11px] font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded border border-amber-200"
                  title="Historical corrections count"
                >
                  <History class="h-3 w-3" />
                  {{ row.correctionsCount }} corr.
                </span>

                <Button
                  type="button"
                  size="small"
                  variant="outlined"
                  :severity="isGraceWindowActive ? 'secondary' : 'warn'"
                  :icon="isGraceWindowActive ? 'pi pi-pencil' : 'pi pi-shield'"
                  :label="isGraceWindowActive ? 'Edit...' : 'Audit Correct...'"
                  class="text-xs"
                  @click="openCorrectionForStudent(row.studentId)"
                />
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Attendance Correction Drawer Component -->
    <AttendanceCorrectionDrawer
      v-model:visible="isCorrectionDrawerOpen"
      :record="selectedRecordForCorrection"
      :activityId="activity.id"
      @corrected="onCorrectionComplete"
    />
  </div>
</template>
