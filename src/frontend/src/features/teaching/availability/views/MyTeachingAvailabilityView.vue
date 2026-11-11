<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import Select from 'primevue/select';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import {
  Calendar,
  Clock,
  Save,
  Plus,
  User,
  CheckCircle2,
  AlertCircle,
  HelpCircle,
  Sparkles,
  Info,
  CalendarDays,
  Layers,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  LoadingSpinner,
  FormField,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  useTeacherAvailability,
  useUnavailabilityWindows,
  useDeclareAvailabilityMutation,
} from '../composables/useAvailability';
import WeeklyTimeGrid from '../components/WeeklyTimeGrid.vue';
import UnavailabilityWindowsList from '../components/UnavailabilityWindowsList.vue';
import AddUnavailabilityModal from '../components/AddUnavailabilityModal.vue';
import type {
  TimeSlotBlock,
  DeclareTeacherAvailabilityRequest,
  DeliveryMode,
} from '../types';

import {
  PERIOD_PRESETS,
  DEMO_TEACHER_PERSONAS,
  type PeriodOption,
  type TeacherPersonaOption,
} from './myTeachingAvailabilityConstants';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Teaching', to: '/teaching/faculty' },
  { label: 'My Availability', current: true },
];

// ---------------------------------------------------------------------------
// 3. Active State (Teacher, Period, Form Fields)
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

// Default teacher resolution: route query -> auth store -> demo teacher persona
const currentTeacherId = ref<string>(
  (typeof route.query.teacherId === 'string' && route.query.teacherId.trim())
    ? route.query.teacherId.trim()
    : authStore.user?.userId || DEMO_TEACHER_PERSONAS[0].id,
);

const selectedPeriodId = ref<string>(PERIOD_PRESETS[0].id);

// Synchronize teacher query parameter with router
function handleTeacherChange(teacherId: string) {
  currentTeacherId.value = teacherId;
  router.replace({
    query: {
      ...route.query,
      teacherId,
    },
  });
}

// Form state for declaring availability
const maxAvailableHours = ref<number>(20);
const effectiveStartDate = ref<string>(PERIOD_PRESETS[0].startDate);
const effectiveEndDate = ref<string>(PERIOD_PRESETS[0].endDate);
const campusScope = ref<string>('All Campuses');
const availableDeliveryModes = ref<string>('All');
const notes = ref<string>('');
const scheduleSlots = ref<TimeSlotBlock[]>([]);

// Dialog state
const isAddLeaveModalOpen = ref<boolean>(false);

// Keep period dates synchronized when switching periods
watch(
  selectedPeriodId,
  (periodId) => {
    const period = PERIOD_PRESETS.find((p) => p.id === periodId);
    if (period) {
      effectiveStartDate.value = period.startDate;
      effectiveEndDate.value = period.endDate;
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 4. Server Queries (Teacher Availability & Unavailability Windows)
// ---------------------------------------------------------------------------
const {
  data: availabilityDto,
  isLoading: isLoadingAvailability,
  isError: isAvailabilityError,
  refetch: refetchAvailability,
} = useTeacherAvailability(currentTeacherId, selectedPeriodId);

const {
  data: unavailabilityWindows,
  isLoading: isLoadingWindows,
  refetch: refetchWindows,
} = useUnavailabilityWindows(currentTeacherId, {
  academicPeriodId: selectedPeriodId.value,
});

// Hydrate form fields when existing availability DTO arrives
watch(
  availabilityDto,
  (dto) => {
    if (dto) {
      maxAvailableHours.value = dto.maxAvailableHours ?? 20;
      effectiveStartDate.value = dto.effectiveStartDate ?? effectiveStartDate.value;
      effectiveEndDate.value = dto.effectiveEndDate ?? effectiveEndDate.value;
      campusScope.value = dto.campusScope ?? 'All Campuses';
      availableDeliveryModes.value = dto.availableDeliveryModes ?? 'All';
      notes.value = dto.notes ?? '';

      if (dto.recurringAvailableSchedule) {
        try {
          const parsed = JSON.parse(dto.recurringAvailableSchedule);
          if (Array.isArray(parsed)) {
            scheduleSlots.value = parsed;
          } else if (parsed && typeof parsed === 'object' && Array.isArray((parsed as { slots?: TimeSlotBlock[] }).slots)) {
            scheduleSlots.value = (parsed as { slots: TimeSlotBlock[] }).slots;
          }
        } catch {
          // Keep current schedule slots if parse fails
        }
      }
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 5. Declaration Mutation
// ---------------------------------------------------------------------------
const declareMutation = useDeclareAvailabilityMutation({
  onSuccess: () => {
    refetchAvailability();
  },
});

const isDeclared = computed(() => {
  return Boolean(availabilityDto.value && availabilityDto.value.status === 'active');
});

function handleSaveAvailability() {
  if (!currentTeacherId.value || !selectedPeriodId.value) return;

  const payload: DeclareTeacherAvailabilityRequest = {
    academicPeriodId: selectedPeriodId.value,
    maxAvailableHours: Number(maxAvailableHours.value) || 20,
    effectiveStartDate: effectiveStartDate.value,
    effectiveEndDate: effectiveEndDate.value,
    campusScope: campusScope.value || undefined,
    availableDeliveryModes: availableDeliveryModes.value || undefined,
    recurringAvailableSchedule: JSON.stringify({
      slots: scheduleSlots.value,
      notes: notes.value || undefined,
    }),
    notes: notes.value || undefined,
  };

  declareMutation.mutate({
    teacherId: currentTeacherId.value,
    payload,
  });
}

function handleLeaveCreated() {
  refetchWindows();
  refetchAvailability();
}
</script>

<template>
  <div class="space-y-6" data-testid="my-teaching-availability-workbench">
    <!-- Breadcrumbs & Header -->
    <PageHeader
      title="My Teaching Availability"
      subtitle="Personal planning workbench for faculty members to declare recurring weekly availability and manage leave/blackout windows (SCR-TCH-04)."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <Button
            type="button"
            severity="secondary"
            variant="outlined"
            size="small"
            class="text-xs"
            data-testid="header-add-leave-btn"
            @click="isAddLeaveModalOpen = true"
          >
            <Plus class="w-3.5 h-3.5 mr-1" />
            Record Leave Window
          </Button>

          <Button
            type="button"
            size="small"
            :loading="declareMutation.isPending.value"
            class="text-xs"
            data-testid="header-save-availability-btn"
            @click="handleSaveAvailability"
          >
            <Save class="w-3.5 h-3.5 mr-1" />
            Save Availability
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Context Bar: Academic Period & Teacher Switcher -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Academic Period Selector -->
      <div>
        <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1.5 flex items-center gap-1.5">
          <CalendarDays class="w-3.5 h-3.5 text-indigo-500" />
          Academic Period
        </label>
        <Select
          v-model="selectedPeriodId"
          :options="PERIOD_PRESETS"
          option-label="label"
          option-value="id"
          placeholder="Select period"
          class="w-full text-xs"
          data-testid="academic-period-select"
        />
      </div>

      <!-- Teacher Context Selector -->
      <div>
        <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1.5 flex items-center gap-1.5">
          <User class="w-3.5 h-3.5 text-sky-500" />
          Faculty Persona
        </label>
        <Select
          :model-value="currentTeacherId"
          :options="DEMO_TEACHER_PERSONAS"
          option-label="name"
          option-value="id"
          placeholder="Select faculty member"
          class="w-full text-xs"
          data-testid="teacher-persona-select"
          @update:model-value="handleTeacherChange"
        />
      </div>

      <!-- Preferred Weekly Teaching Hours -->
      <div>
        <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1.5 flex items-center gap-1.5">
          <Clock class="w-3.5 h-3.5 text-emerald-500" />
          Preferred Weekly Hours (Capacity)
        </label>
        <InputNumber
          v-model="maxAvailableHours"
          :min="1"
          :max="60"
          suffix=" hrs/week"
          class="w-full text-xs"
          data-testid="preferred-hours-input"
        />
      </div>

      <!-- Effective Date Range -->
      <div>
        <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1.5 flex items-center gap-1.5">
          <Calendar class="w-3.5 h-3.5 text-amber-500" />
          Effective Date Span
        </label>
        <div class="grid grid-cols-2 gap-1.5">
          <InputText
            v-model="effectiveStartDate"
            type="date"
            class="w-full text-xs py-1.5"
            data-testid="effective-start-date-input"
          />
          <InputText
            v-model="effectiveEndDate"
            type="date"
            class="w-full text-xs py-1.5"
            data-testid="effective-end-date-input"
          />
        </div>
      </div>
    </div>

    <!-- Submission Status Banner -->
    <div
      v-if="isLoadingAvailability"
      class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-700 flex items-center gap-3"
    >
      <LoadingSpinner size="sm" />
      <span class="text-xs text-slate-500 dark:text-slate-400">Loading teacher availability declaration...</span>
    </div>

    <div
      v-else-if="isDeclared && availabilityDto"
      class="p-4 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 flex flex-wrap items-center justify-between gap-3 shadow-xs"
      data-testid="submission-status-banner-active"
    >
      <div class="flex items-start gap-3">
        <CheckCircle2 class="w-5 h-5 text-emerald-600 dark:text-emerald-400 mt-0.5 flex-shrink-0" />
        <div class="space-y-0.5">
          <div class="flex items-center gap-2">
            <span class="text-sm font-semibold text-emerald-900 dark:text-emerald-200">
              Active Recurring Availability Declared
            </span>
            <StatusPill status="active" label="ACTIVE" size="sm" />
          </div>
          <p class="text-xs text-emerald-700 dark:text-emerald-300">
            Declared capacity: <strong class="font-bold">{{ availabilityDto.maxAvailableHours }} hours/week</strong>
            across {{ formatDisplayDate(availabilityDto.effectiveStartDate) }} to {{ formatDisplayDate(availabilityDto.effectiveEndDate) }}.
          </p>
          <div class="text-[11px] text-emerald-600/90 dark:text-emerald-400">
            Last registered: {{ formatDisplayDateTime(availabilityDto.createdAt) }}
          </div>
        </div>
      </div>

      <Button
        type="button"
        size="small"
        severity="success"
        variant="outlined"
        :loading="declareMutation.isPending.value"
        class="text-xs py-1.5 px-3 bg-white/70 dark:bg-slate-900/60"
        data-testid="update-availability-btn"
        @click="handleSaveAvailability"
      >
        <Save class="w-3.5 h-3.5 mr-1" />
        Update Declaration
      </Button>
    </div>

    <div
      v-else
      class="p-4 rounded-xl bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800 flex flex-wrap items-center justify-between gap-3 shadow-xs"
      data-testid="submission-status-banner-draft"
    >
      <div class="flex items-start gap-3">
        <Info class="w-5 h-5 text-sky-600 dark:text-sky-400 mt-0.5 flex-shrink-0" />
        <div class="space-y-0.5">
          <div class="flex items-center gap-2">
            <span class="text-sm font-semibold text-sky-900 dark:text-sky-200">
              Draft / No Availability Declared for this Period
            </span>
            <StatusPill status="draft" label="DRAFT" size="sm" />
          </div>
          <p class="text-xs text-sky-700 dark:text-sky-300">
            You have not yet registered a recurring weekly schedule for this academic period. Select your preferred hours and time blocks below, then save.
          </p>
        </div>
      </div>

      <Button
        type="button"
        size="small"
        :loading="declareMutation.isPending.value"
        class="text-xs py-1.5 px-3"
        data-testid="initial-declare-availability-btn"
        @click="handleSaveAvailability"
      >
        <Save class="w-3.5 h-3.5 mr-1" />
        Save & Submit Declaration
      </Button>
    </div>

    <!-- Main Workbench Sections -->
    <div class="space-y-6">
      <!-- Section 1: Weekly Time Grid Selector -->
      <div class="p-5 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
        <div class="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 dark:border-slate-800 pb-3">
          <div>
            <h2 class="text-base font-semibold text-slate-900 dark:text-slate-100 flex items-center gap-2">
              <Calendar class="w-4 h-4 text-emerald-600" />
              Weekly Recurring Schedule
            </h2>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              Select which morning (08:00–12:00) and afternoon (13:00–17:00) time blocks you are available for teaching assignments.
            </p>
          </div>
        </div>

        <WeeklyTimeGrid
          v-model="scheduleSlots"
          data-testid="my-weekly-time-grid"
        />
      </div>

      <!-- Section 2: Temporary Leaves & Blackout Windows -->
      <div class="p-5 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
        <UnavailabilityWindowsList
          :windows="unavailabilityWindows ?? availabilityDto?.activeUnavailabilityWindows ?? []"
          :is-loading="isLoadingWindows"
          :teacher-id="currentTeacherId"
          data-testid="my-unavailability-windows-list"
          @add="isAddLeaveModalOpen = true"
          @cancel="refetchWindows"
        />
      </div>
    </div>

    <!-- Add Unavailability Modal -->
    <AddUnavailabilityModal
      v-model:visible="isAddLeaveModalOpen"
      :teacher-id="currentTeacherId"
      data-testid="workbench-add-unavailability-modal"
      @created="handleLeaveCreated"
    />
  </div>
</template>
