<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import {
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  Plus,
  Trash2,
  CalendarCheck,
  CalendarX,
  AlertTriangle,
  Info,
  ShieldCheck,
  CalendarPlus,
  Sparkles,
} from 'lucide-vue-next';
import { ContentCard, LoadingSpinner } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../../constants/permissions';
import { useAcademicYearsQuery } from '../../composables/useAcademicYearsQuery';
import {
  useCalendarsQuery,
  useCalendarMatrixQuery,
  usePublishCalendarMutation,
  useCreateCalendarMutation,
  useDeleteCalendarEntryMutation,
} from '../../composables/useCalendarMatrixQuery';
import type { SchoolCalendarDto } from '../../api/calendar.api';
import CreateCalendarEntryModal from '../modals/CreateCalendarEntryModal.vue';

const props = defineProps<{
  academicYearId?: string | null;
  calendarId?: string | null;
}>();

const authStore = useAuthStore();
const canManage = computed(() => authStore.hasPermission(CALENDAR_PERMISSIONS.MANAGE));
const canPublish = computed(() => authStore.hasPermission(CALENDAR_PERMISSIONS.PUBLISH));

// ----------------------------------------------------------------------------
// 1. Academic Year & Calendar Resolution
// ----------------------------------------------------------------------------
const { data: academicYears } = useAcademicYearsQuery();

const selectedAcademicYear = computed(() => {
  if (!props.academicYearId || !academicYears.value) return null;
  return academicYears.value.find((ay) => ay.id === props.academicYearId) ?? null;
});

const { data: calendars, isLoading: isLoadingCalendars } = useCalendarsQuery(
  computed(() => props.academicYearId),
);

const resolvedCalendar = computed<SchoolCalendarDto | null>(() => {
  if (!calendars.value || calendars.value.length === 0) return null;
  if (props.calendarId) {
    const found = calendars.value.find((c) => c.id === props.calendarId);
    if (found) return found;
  }
  const published = calendars.value.find((c) => c.publicationStatus === 'published');
  return published ?? calendars.value[0] ?? null;
});

const resolvedCalendarId = computed(() => resolvedCalendar.value?.id ?? null);

const isCalendarPublished = computed(
  () => resolvedCalendar.value?.publicationStatus === 'published',
);

// ----------------------------------------------------------------------------
// 2. Month Navigation & Date Range Calculation
// ----------------------------------------------------------------------------
const currentDate = new Date();
const currentYear = ref(currentDate.getFullYear());
const currentMonth = ref(currentDate.getMonth()); // 0-indexed: 0 = Jan, 11 = Dec

const MONTH_NAMES = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
];

const WEEKDAY_NAMES = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

// Initialize month from academic year startDate when year is selected
watch(
  () => selectedAcademicYear.value,
  (ay) => {
    if (!ay?.startDate) return;
    const now = new Date();
    const nowIso = now.toISOString().substring(0, 10);
    // If today is within academic year span, view today's month; otherwise jump to start date
    if (nowIso >= ay.startDate && nowIso <= ay.endDate) {
      currentYear.value = now.getFullYear();
      currentMonth.value = now.getMonth();
    } else {
      const parts = ay.startDate.split('-');
      if (parts.length === 3) {
        currentYear.value = Number(parts[0]);
        currentMonth.value = Number(parts[1]) - 1;
      }
    }
  },
  { immediate: true },
);

function goToPreviousMonth(): void {
  if (currentMonth.value === 0) {
    currentMonth.value = 11;
    currentYear.value -= 1;
  } else {
    currentMonth.value -= 1;
  }
}

function goToNextMonth(): void {
  if (currentMonth.value === 11) {
    currentMonth.value = 0;
    currentYear.value += 1;
  } else {
    currentMonth.value += 1;
  }
}

function goToCurrentMonth(): void {
  const now = new Date();
  currentYear.value = now.getFullYear();
  currentMonth.value = now.getMonth();
}

const formattedMonthYear = computed(() => {
  return `${MONTH_NAMES[currentMonth.value]} ${currentYear.value}`;
});

const startDateStr = computed(() => {
  const y = currentYear.value;
  const m = String(currentMonth.value + 1).padStart(2, '0');
  return `${y}-${m}-01`;
});

const endDateStr = computed(() => {
  const y = currentYear.value;
  const m = currentMonth.value + 1;
  const lastDay = new Date(y, m, 0).getDate();
  return `${y}-${String(m).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`;
});

const matrixParams = computed(() => ({
  startDate: startDateStr.value,
  endDate: endDateStr.value,
  campus: 'All',
}));

// ----------------------------------------------------------------------------
// 3. Matrix Projection Query & Transformations
// ----------------------------------------------------------------------------
const { data: matrixDays, isLoading: isLoadingMatrix } = useCalendarMatrixQuery(
  computed(() => resolvedCalendarId.value),
  matrixParams,
  {
    enabled: computed(() => Boolean(resolvedCalendarId.value)),
  },
);

export interface CalendarDayCell {
  date: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isInstructional: boolean;
  designation: string;
  entryName?: string | null;
  entryDescription?: string | null;
  entryId?: string | null;
}

// Compute blank padding cells before the 1st day of the month
// Monday is index 0, Sunday is index 6
const paddingDaysCount = computed(() => {
  const firstDayOfWeek = new Date(currentYear.value, currentMonth.value, 1).getDay();
  return (firstDayOfWeek + 6) % 7;
});

const todayIso = new Date().toISOString().substring(0, 10);

const dayCells = computed<CalendarDayCell[]>(() => {
  const cells: CalendarDayCell[] = [];
  const totalDays = new Date(currentYear.value, currentMonth.value + 1, 0).getDate();

  for (let day = 1; day <= totalDays; day++) {
    const dateStr = `${currentYear.value}-${String(currentMonth.value + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    const projected = matrixDays.value?.find((p) => p.date === dateStr);

    if (projected) {
      cells.push({
        date: dateStr,
        dayNumber: day,
        isCurrentMonth: true,
        isToday: dateStr === todayIso,
        isInstructional: projected.isInstructional,
        designation: projected.designation,
        entryName: projected.entryName ?? null,
        entryDescription: projected.entryDescription ?? null,
        entryId: projected.entryId ?? null,
      });
    } else {
      // Fallback baseline when query has not returned or day is not explicit
      const dayCol = (paddingDaysCount.value + day - 1) % 7;
      const isWeekend = dayCol >= 5;
      cells.push({
        date: dateStr,
        dayNumber: day,
        isCurrentMonth: true,
        isToday: dateStr === todayIso,
        isInstructional: !isWeekend,
        designation: isWeekend ? 'weekend' : 'instructional_day',
        entryName: null,
        entryDescription: null,
        entryId: null,
      });
    }
  }

  return cells;
});

// Month Summary Metrics
const totalDaysInMonth = computed(() => dayCells.value.length);
const instructionalDaysCount = computed(
  () => dayCells.value.filter((d) => d.isInstructional).length,
);
const nonInstructionalDaysCount = computed(
  () => dayCells.value.filter((d) => !d.isInstructional).length,
);
const exceptionEntriesCount = computed(
  () => dayCells.value.filter((d) => d.entryName || d.designation === 'school_closure').length,
);

// ----------------------------------------------------------------------------
// 4. Day Designation Badge Styling
// ----------------------------------------------------------------------------
interface DesignationStyle {
  label: string;
  badgeClass: string;
  dotClass: string;
}

function getDesignationStyle(designation: string, isInstructional: boolean): DesignationStyle {
  switch (designation) {
    case 'instructional_day':
      return {
        label: 'Instructional',
        badgeClass: 'bg-slate-100 text-slate-700 border-slate-200',
        dotClass: 'bg-slate-400',
      };
    case 'holiday':
      return {
        label: 'Holiday',
        badgeClass: 'bg-rose-100 text-rose-800 border-rose-200',
        dotClass: 'bg-rose-500',
      };
    case 'school_closure':
      return {
        label: 'Closure',
        badgeClass: 'bg-amber-100 text-amber-800 border-amber-300 font-semibold',
        dotClass: 'bg-amber-600',
      };
    case 'staff_only_day':
      return {
        label: 'Staff Day',
        badgeClass: 'bg-sky-100 text-sky-800 border-sky-200',
        dotClass: 'bg-sky-500',
      };
    case 'exam_day':
      return {
        label: 'Exam Window',
        badgeClass: 'bg-purple-100 text-purple-800 border-purple-200',
        dotClass: 'bg-purple-500',
      };
    case 'weekend':
      return {
        label: 'Weekend',
        badgeClass: 'bg-slate-50 text-slate-400 border-slate-200',
        dotClass: 'bg-slate-300',
      };
    case 'significant_date':
      return {
        label: 'Significant Date',
        badgeClass: 'bg-indigo-50 text-indigo-700 border-indigo-200',
        dotClass: 'bg-indigo-500',
      };
    case 'domain_window':
      return {
        label: 'Domain Window',
        badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
        dotClass: 'bg-emerald-500',
      };
    default:
      return {
        label: designation.replace(/_/g, ' '),
        badgeClass: isInstructional
          ? 'bg-slate-100 text-slate-700 border-slate-200'
          : 'bg-amber-50 text-amber-700 border-amber-200',
        dotClass: isInstructional ? 'bg-slate-400' : 'bg-amber-500',
      };
  }
}

// ----------------------------------------------------------------------------
// 5. Actions & Modals State
// ----------------------------------------------------------------------------
const isAddEntryModalOpen = ref(false);
const selectedDateForEntry = ref<string | null>(null);

function handleOpenAddEntryModal(initialDate?: string): void {
  selectedDateForEntry.value = initialDate ?? null;
  isAddEntryModalOpen.value = true;
}

// Publish Calendar Modal State
const isPublishModalOpen = ref(false);
const publishMutation = usePublishCalendarMutation();

function handleConfirmPublish(): void {
  if (!resolvedCalendarId.value) return;
  publishMutation.mutate(resolvedCalendarId.value, {
    onSuccess: () => {
      isPublishModalOpen.value = false;
    },
  });
}

// Create Initial Calendar (if none exists)
const createCalendarMutation = useCreateCalendarMutation();

function handleCreateInitialCalendar(): void {
  if (!props.academicYearId || !selectedAcademicYear.value) return;
  createCalendarMutation.mutate({
    academicYearId: props.academicYearId,
    payload: {
      name: `Kalender Sekolah ${selectedAcademicYear.value.code}`,
      scopeType: 'institution',
      scopeReference: 'All',
      notes: 'Initial school calendar baseline',
    },
  });
}

// Delete Entry Confirmation State
const isDeleteConfirmOpen = ref(false);
const entryToDelete = ref<{ entryId: string; entryName: string } | null>(null);
const deleteEntryMutation = useDeleteCalendarEntryMutation();

function confirmDeleteEntry(entryId: string, entryName: string): void {
  entryToDelete.value = { entryId, entryName };
  isDeleteConfirmOpen.value = true;
}

function handleDeleteEntry(): void {
  if (!resolvedCalendarId.value || !entryToDelete.value) return;
  deleteEntryMutation.mutate(
    {
      calendarId: resolvedCalendarId.value,
      entryId: entryToDelete.value.entryId,
    },
    {
      onSuccess: () => {
        isDeleteConfirmOpen.value = false;
        entryToDelete.value = null;
      },
    },
  );
}
</script>

<template>
  <div class="space-y-4" data-testid="calendar-matrix-tab">
    <!-- ---------------------------------------------------------------------- -->
    <!-- 1. Publication Status Banner with Publish Trigger                       -->
    <!-- ---------------------------------------------------------------------- -->
    <div v-if="resolvedCalendar">
      <!-- Published Status Banner -->
      <div
        v-if="isCalendarPublished"
        class="flex items-center justify-between p-3.5 bg-emerald-50/90 border border-emerald-200 rounded-lg text-xs leading-relaxed"
        data-testid="calendar-published-banner"
      >
        <div class="flex items-center gap-3">
          <div class="p-2 bg-emerald-100 text-emerald-700 rounded-md shrink-0">
            <ShieldCheck class="w-5 h-5" />
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="font-semibold text-emerald-950 text-sm">
                Published Calendar (v{{ resolvedCalendar.version }})
              </span>
              <span
                class="px-2 py-0.5 bg-emerald-100 text-emerald-800 rounded-full text-[10px] font-bold"
              >
                AUTHORITATIVE BASELINE
              </span>
            </div>
            <p class="text-emerald-800 text-xs">
              {{ resolvedCalendar.name }} is active. Adding or editing entries will automatically
              generate an auditable Calendar Amendment and bump the minor version.
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <Button
            v-if="canManage"
            type="button"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5"
            data-testid="open-add-entry-modal-btn"
            @click="handleOpenAddEntryModal()"
          >
            <CalendarPlus class="w-4 h-4" />
            <span>Add Entry</span>
          </Button>
        </div>
      </div>

      <!-- Draft Status Banner with Publish Trigger -->
      <div
        v-else
        class="flex items-center justify-between p-3.5 bg-slate-50 border border-slate-200 rounded-lg text-xs leading-relaxed"
        data-testid="calendar-draft-banner"
      >
        <div class="flex items-center gap-3">
          <div class="p-2 bg-amber-100 text-amber-700 rounded-md shrink-0">
            <AlertTriangle class="w-5 h-5" />
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="font-semibold text-slate-800 text-sm">
                Calendar Status: Draft (v{{ resolvedCalendar.version }})
              </span>
              <span
                class="px-2 py-0.5 bg-amber-100 text-amber-800 rounded-full text-[10px] font-bold"
              >
                UNPUBLISHED
              </span>
            </div>
            <p class="text-slate-600 text-xs">
              Schedule entries and instructional days are mutable. Publish calendar to establish an
              authoritative baseline for attendance and scheduling.
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2 flex-wrap">
          <Button
            v-if="canManage"
            type="button"
            size="small"
            outlined
            class="text-slate-700 hover:text-teal-700 flex items-center gap-1"
            data-testid="open-add-entry-modal-btn"
            @click="handleOpenAddEntryModal()"
          >
            <CalendarPlus class="w-3.5 h-3.5 text-teal-600" />
            <span>Add Entry</span>
          </Button>

          <Button
            v-if="canPublish"
            type="button"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5"
            data-testid="publish-calendar-banner-btn"
            @click="isPublishModalOpen = true"
          >
            <Sparkles class="w-4 h-4" />
            <span>Publish Calendar</span>
          </Button>
        </div>
      </div>
    </div>

    <!-- Empty Calendar State: Prompt to Create Calendar -->
    <div
      v-else-if="!isLoadingCalendars"
      class="p-6 bg-slate-50 border border-dashed border-slate-200 rounded-lg text-center space-y-3"
      data-testid="no-calendar-prompt"
    >
      <CalendarX class="w-10 h-10 mx-auto text-slate-400" />
      <div class="space-y-1">
        <h4 class="font-semibold text-slate-800">No School Calendar Found</h4>
        <p class="text-xs text-slate-500 max-w-md mx-auto">
          An operational school calendar container is required to project instructional days,
          statutory holidays, and campus closure windows.
        </p>
      </div>
      <Button
        v-if="canManage && academicYearId"
        type="button"
        size="small"
        :loading="createCalendarMutation.isPending.value"
        class="bg-teal-600 hover:bg-teal-700 text-white inline-flex items-center gap-1.5"
        data-testid="create-initial-calendar-btn"
        @click="handleCreateInitialCalendar"
      >
        <CalendarPlus class="w-4 h-4" />
        <span>Create School Calendar</span>
      </Button>
    </div>

    <!-- ---------------------------------------------------------------------- -->
    <!-- 2. Month Navigator Bar & Month Statistics                              -->
    <!-- ---------------------------------------------------------------------- -->
    <ContentCard>
      <div class="space-y-4">
        <!-- Navigator Header Controls -->
        <div class="flex items-center justify-between flex-wrap gap-4 pb-2 border-b border-slate-100">
          <div class="flex items-center gap-3">
            <div class="flex items-center gap-1 border border-slate-200 rounded-md p-0.5 bg-white shadow-xs">
              <Button
                type="button"
                text
                size="small"
                aria-label="Previous Month"
                data-testid="prev-month-btn"
                class="p-1.5 text-slate-600 hover:text-slate-900 cursor-pointer"
                @click="goToPreviousMonth"
              >
                <ChevronLeft class="w-4 h-4" />
              </Button>
              <Button
                type="button"
                text
                size="small"
                aria-label="Current Month"
                data-testid="current-month-btn"
                class="px-2 py-1 text-xs font-medium text-slate-700 hover:text-teal-700 cursor-pointer"
                @click="goToCurrentMonth"
              >
                Current Month
              </Button>
              <Button
                type="button"
                text
                size="small"
                aria-label="Next Month"
                data-testid="next-month-btn"
                class="p-1.5 text-slate-600 hover:text-slate-900 cursor-pointer"
                @click="goToNextMonth"
              >
                <ChevronRight class="w-4 h-4" />
              </Button>
            </div>

            <!-- Current Month Label -->
            <div class="flex items-center gap-2">
              <CalendarDays class="w-5 h-5 text-teal-600" />
              <h3
                class="text-base font-bold text-slate-800 tracking-tight"
                data-testid="current-month-label"
              >
                {{ formattedMonthYear }}
              </h3>
            </div>
          </div>

          <!-- Month Summary Metrics -->
          <div class="flex items-center gap-3 text-xs flex-wrap" data-testid="month-summary-metrics">
            <div class="px-2.5 py-1 bg-slate-100 rounded text-slate-700 flex items-center gap-1.5">
              <span class="text-slate-500">Total:</span>
              <span class="font-bold text-slate-900" data-testid="metric-total-days">
                {{ totalDaysInMonth }}d
              </span>
            </div>
            <div class="px-2.5 py-1 bg-emerald-50 border border-emerald-200/60 rounded text-emerald-800 flex items-center gap-1.5">
              <CalendarCheck class="w-3.5 h-3.5 text-emerald-600" />
              <span>Instructional:</span>
              <span class="font-bold text-emerald-950" data-testid="metric-instructional-days">
                {{ instructionalDaysCount }}d
              </span>
            </div>
            <div class="px-2.5 py-1 bg-rose-50 border border-rose-200/60 rounded text-rose-800 flex items-center gap-1.5">
              <CalendarX class="w-3.5 h-3.5 text-rose-600" />
              <span>Non-Instructional:</span>
              <span class="font-bold text-rose-950" data-testid="metric-non-instructional-days">
                {{ nonInstructionalDaysCount }}d
              </span>
            </div>
            <div
              v-if="exceptionEntriesCount > 0"
              class="px-2.5 py-1 bg-purple-50 border border-purple-200/60 rounded text-purple-800 flex items-center gap-1.5"
            >
              <Info class="w-3.5 h-3.5 text-purple-600" />
              <span>Exceptions:</span>
              <span class="font-bold text-purple-950" data-testid="metric-exception-entries">
                {{ exceptionEntriesCount }}
              </span>
            </div>
          </div>
        </div>

        <!-- ------------------------------------------------------------------ -->
        <!-- 3. Visual Calendar Matrix Grid                                     -->
        <!-- ------------------------------------------------------------------ -->
        <div v-if="isLoadingMatrix" class="py-16 text-center">
          <LoadingSpinner size="lg" message="Computing calendar matrix projection..." />
        </div>

        <div v-else class="space-y-2" data-testid="calendar-matrix-grid">
          <!-- Weekday Columns Header -->
          <div class="grid grid-cols-7 gap-2 text-center text-xs font-semibold text-slate-600">
            <div
              v-for="(dayName, idx) in WEEKDAY_NAMES"
              :key="dayName"
              :class="[
                'py-2 px-1 rounded bg-slate-50 border border-slate-100',
                idx >= 5 ? 'text-rose-600 bg-rose-50/40' : 'text-slate-700',
              ]"
              :data-testid="`weekday-header-${idx}`"
            >
              <span class="hidden md:inline">{{ dayName }}</span>
              <span class="md:hidden">{{ dayName.substring(0, 3) }}</span>
            </div>
          </div>

          <!-- Date Matrix Cells -->
          <div class="grid grid-cols-7 gap-2">
            <!-- Padding cells before first day of month -->
            <div
              v-for="pad in paddingDaysCount"
              :key="`pad-${pad}`"
              class="min-h-24 p-2 rounded-lg bg-slate-50/50 border border-dashed border-slate-200/60 opacity-40 select-none"
              aria-hidden="true"
            />

            <!-- Active Day Cells -->
            <div
              v-for="cell in dayCells"
              :key="cell.date"
              :class="[
                'min-h-24 p-2.5 rounded-lg border flex flex-col justify-between transition-all duration-150 relative group bg-white shadow-2xs',
                cell.isToday
                  ? 'border-teal-500 ring-2 ring-teal-500/20'
                  : 'border-slate-200/80 hover:border-slate-300',
                !cell.isInstructional && cell.designation !== 'weekend'
                  ? 'bg-rose-50/20'
                  : '',
              ]"
              :data-testid="`day-cell-${cell.date}`"
              :data-date="cell.date"
              :data-instructional="cell.isInstructional"
              :data-designation="cell.designation"
            >
              <!-- Cell Top Row: Date Number & Badges -->
              <div class="flex items-center justify-between gap-1 mb-1.5">
                <div class="flex items-center gap-1.5">
                  <span
                    :class="[
                      'text-sm font-semibold tabular-nums',
                      cell.isToday
                        ? 'text-white bg-teal-600 rounded-full w-6 h-6 flex items-center justify-center text-xs'
                        : 'text-slate-800',
                    ]"
                  >
                    {{ cell.dayNumber }}
                  </span>

                  <span
                    v-if="cell.isToday"
                    class="text-[10px] font-bold text-teal-700 bg-teal-50 px-1.5 py-0.2 rounded border border-teal-200 hidden sm:inline"
                  >
                    Today
                  </span>
                </div>

                <!-- Add Entry Button (hover trigger) -->
                <button
                  v-if="canManage && resolvedCalendar"
                  type="button"
                  title="Add Calendar Entry on this date"
                  class="opacity-0 group-hover:opacity-100 text-slate-400 hover:text-teal-600 p-0.5 rounded hover:bg-slate-100 transition-opacity cursor-pointer"
                  :data-testid="`cell-add-btn-${cell.date}`"
                  @click="handleOpenAddEntryModal(cell.date)"
                >
                  <Plus class="w-3.5 h-3.5" />
                </button>
              </div>

              <!-- Cell Content: Designation Badge & Entry Title -->
              <div class="space-y-1 my-auto">
                <!-- Color-Coded Day Badge -->
                <div class="flex flex-wrap gap-1">
                  <span
                    :class="[
                      'inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium border leading-tight select-none',
                      getDesignationStyle(cell.designation, cell.isInstructional).badgeClass,
                    ]"
                    :data-testid="`day-badge-${cell.date}`"
                    :data-designation="cell.designation"
                  >
                    <span
                      class="w-1.5 h-1.5 rounded-full shrink-0"
                      :class="getDesignationStyle(cell.designation, cell.isInstructional).dotClass"
                    />
                    <span class="truncate max-w-[120px]">
                      {{ getDesignationStyle(cell.designation, cell.isInstructional).label }}
                    </span>
                  </span>
                </div>

                <!-- Explicit Entry Title & Actions -->
                <div
                  v-if="cell.entryName"
                  class="p-1.5 bg-white border border-slate-200 rounded text-xs shadow-2xs space-y-0.5 group/entry"
                  :data-testid="`entry-card-${cell.date}`"
                >
                  <div class="flex items-start justify-between gap-1">
                    <span class="font-semibold text-slate-900 text-[11px] leading-tight line-clamp-2">
                      {{ cell.entryName }}
                    </span>
                    <button
                      v-if="canManage && cell.entryId"
                      type="button"
                      title="Delete entry"
                      class="text-slate-400 hover:text-rose-600 p-0.5 shrink-0 opacity-0 group-hover/entry:opacity-100 transition-opacity cursor-pointer"
                      :data-testid="`delete-entry-btn-${cell.entryId}`"
                      @click="confirmDeleteEntry(cell.entryId, cell.entryName)"
                    >
                      <Trash2 class="w-3 h-3" />
                    </button>
                  </div>
                  <p
                    v-if="cell.entryDescription"
                    class="text-[10px] text-slate-500 line-clamp-1 italic"
                  >
                    {{ cell.entryDescription }}
                  </p>
                </div>
              </div>

              <!-- Cell Footer: Instructional Indicator -->
              <div class="mt-1 pt-1 border-t border-slate-100 flex items-center justify-between text-[10px] text-slate-400">
                <span v-if="cell.isInstructional" class="text-emerald-700 font-medium flex items-center gap-0.5">
                  <span class="w-1 h-1 rounded-full bg-emerald-500 inline-block" />
                  Teaching Day
                </span>
                <span v-else class="text-slate-400 italic">
                  Non-Teaching
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Designation Legend Footer -->
        <div
          class="flex items-center gap-4 pt-3 border-t border-slate-100 text-xs flex-wrap justify-between text-slate-600"
          data-testid="matrix-legend"
        >
          <span class="font-medium text-slate-700">Designation Legend:</span>
          <div class="flex items-center gap-3 flex-wrap">
            <span class="inline-flex items-center gap-1.5 text-xs text-slate-700">
              <span class="w-2.5 h-2.5 rounded bg-slate-200 border border-slate-300" />
              Instructional Day (Neutral/Slate)
            </span>
            <span class="inline-flex items-center gap-1.5 text-xs text-rose-800">
              <span class="w-2.5 h-2.5 rounded bg-rose-200 border border-rose-300" />
              Statutory/Religious Holiday (Rose/Red)
            </span>
            <span class="inline-flex items-center gap-1.5 text-xs text-amber-800">
              <span class="w-2.5 h-2.5 rounded bg-amber-200 border border-amber-300" />
              Campus Closure (Amber Alert)
            </span>
            <span class="inline-flex items-center gap-1.5 text-xs text-sky-800">
              <span class="w-2.5 h-2.5 rounded bg-sky-200 border border-sky-300" />
              Staff-Only Day (Sky/Blue)
            </span>
            <span class="inline-flex items-center gap-1.5 text-xs text-purple-800">
              <span class="w-2.5 h-2.5 rounded bg-purple-200 border border-purple-300" />
              Exam Window (Purple)
            </span>
          </div>
        </div>
      </div>
    </ContentCard>

    <!-- ---------------------------------------------------------------------- -->
    <!-- 4. Modals                                                              -->
    <!-- ---------------------------------------------------------------------- -->

    <!-- Create Calendar Entry Modal -->
    <CreateCalendarEntryModal
      v-if="resolvedCalendarId"
      v-model:visible="isAddEntryModalOpen"
      :calendar-id="resolvedCalendarId"
      :is-published="isCalendarPublished"
      :initial-date="selectedDateForEntry"
    />

    <!-- Publish Calendar Confirmation Dialog -->
    <Dialog
      v-model:visible="isPublishModalOpen"
      header="Publish School Calendar"
      modal
      class="max-w-md w-full"
      data-testid="publish-calendar-dialog"
    >
      <div class="space-y-3 pt-2">
        <div class="flex items-start gap-3 p-3 bg-amber-50 border border-amber-200 text-amber-900 rounded-md text-xs leading-relaxed">
          <AlertTriangle class="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
          <div>
            <p class="font-semibold text-amber-950">Publish Baseline Version</p>
            <p>
              Publishing will freeze this calendar as the official institutional baseline (v1.0).
              Any future date modifications, additions, or emergency closures will generate auditable
              Calendar Amendments.
            </p>
          </div>
        </div>
        <p class="text-xs text-slate-600">
          Are you sure you want to publish
          <span class="font-semibold text-slate-900">{{ resolvedCalendar?.name }}</span>?
        </p>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            data-testid="publish-cancel-btn"
            @click="isPublishModalOpen = false"
          />
          <Button
            type="button"
            size="small"
            :loading="publishMutation.isPending.value"
            data-testid="publish-confirm-btn"
            class="bg-teal-600 hover:bg-teal-700 text-white"
            @click="handleConfirmPublish"
          >
            <Sparkles class="w-4 h-4 mr-1.5" />
            <span>Confirm Publication</span>
          </Button>
        </div>
      </div>
    </Dialog>

    <!-- Delete Calendar Entry Confirmation Dialog -->
    <Dialog
      v-model:visible="isDeleteConfirmOpen"
      header="Delete Calendar Entry"
      modal
      class="max-w-md w-full"
      data-testid="delete-calendar-entry-dialog"
    >
      <div class="space-y-3 pt-2">
        <p class="text-xs text-slate-600">
          Are you sure you want to remove entry
          <span class="font-semibold text-slate-900">'{{ entryToDelete?.entryName }}'</span>?
          The day will revert to its baseline instructional status.
        </p>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            data-testid="delete-cancel-btn"
            @click="isDeleteConfirmOpen = false"
          />
          <Button
            type="button"
            size="small"
            severity="danger"
            :loading="deleteEntryMutation.isPending.value"
            data-testid="delete-confirm-btn"
            class="bg-rose-600 hover:bg-rose-700 text-white"
            @click="handleDeleteEntry"
          >
            <Trash2 class="w-4 h-4 mr-1.5" />
            <span>Delete Entry</span>
          </Button>
        </div>
      </div>
    </Dialog>
  </div>
</template>
