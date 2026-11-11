<script setup lang="ts">
import { computed } from 'vue';
import Select from 'primevue/select';
import Button from 'primevue/button';
import { Building2, Calendar, GraduationCap, RefreshCw } from 'lucide-vue-next';
import { useAppContextStore } from '@/app/stores/appContext.store';
import type {
  KpiPeriodOptionDto,
  ResolvedKpiContextDto,
  SchoolLevelCode,
} from '../types/dashboard.types';

export interface KpiFilterBarProps {
  availablePeriods?: KpiPeriodOptionDto[] | undefined;
  resolvedContext?: ResolvedKpiContextDto | null | undefined;
  schoolLevel?: string | null | undefined;
  academicPeriodId?: string | null | undefined;
  campus?: string | null | undefined;
  isLoading?: boolean | undefined;
}

const props = withDefaults(defineProps<KpiFilterBarProps>(), {
  availablePeriods: () => [],
  resolvedContext: null,
  schoolLevel: null,
  academicPeriodId: undefined,
  campus: undefined,
  isLoading: false,
});

const emit = defineEmits<{
  'update:schoolLevel': [value: string | null];
  'update:academicPeriodId': [value: string | null];
  'update:campus': [value: string | null];
  refresh: [];
}>();

const appContextStore = useAppContextStore();

interface CampusSelectOption {
  id: string | null;
  label: string;
}

const campusSelectOptions: CampusSelectOption[] = [
  { id: null, label: 'All Campuses (Semua Kampus)' },
  { id: 'campus-main', label: 'Main Campus (Kampus A)' },
  { id: 'campus-east', label: 'East Campus (Kampus B)' },
];

interface SchoolLevelOption {
  value: SchoolLevelCode | null;
  label: string;
}

const schoolLevelOptions: SchoolLevelOption[] = [
  { value: null, label: 'All Levels' },
  { value: 'ELC', label: 'ELC' },
  { value: 'JHLC', label: 'JHLC' },
  { value: 'HSLC', label: 'HSLC' },
];

interface PeriodSelectOption {
  id: string;
  label: string;
}

const periodSelectOptions = computed<PeriodSelectOption[]>(() => {
  if (props.availablePeriods && props.availablePeriods.length > 0) {
    return props.availablePeriods.map((period) => ({
      id: period.id,
      label:
        period.status === 'active'
          ? `${period.name} (Active)`
          : `${period.name} (${period.status === 'closed' ? 'Closed' : period.status})`,
    }));
  }

  if (props.resolvedContext) {
    return [
      {
        id: props.resolvedContext.academicPeriodId,
        label: props.resolvedContext.academicPeriodName,
      },
    ];
  }

  return [];
});

const selectedCampus = computed<string | null>(() => {
  return props.campus !== undefined ? props.campus : appContextStore.activeCampusId;
});

const selectedPeriodId = computed<string | null>(() => {
  if (props.academicPeriodId !== undefined) {
    return props.academicPeriodId;
  }
  return appContextStore.activeAcademicPeriodId ?? props.resolvedContext?.academicPeriodId ?? null;
});

function handleCampusChange(value: string | null) {
  appContextStore.setActiveCampus(value);
  emit('update:campus', value);
}

function handlePeriodChange(value: string | null) {
  appContextStore.setActiveAcademicPeriod(value);
  emit('update:academicPeriodId', value);
}

function handleSchoolLevelSelect(level: SchoolLevelCode | null) {
  emit('update:schoolLevel', level);
}
</script>

<template>
  <div
    class="bg-white border border-slate-200 rounded-xl shadow-xs p-3.5 sm:p-4 flex flex-col lg:flex-row lg:items-center justify-between gap-3.5"
    data-testid="kpi-filter-bar"
  >
    <!-- Period & Campus Selectors -->
    <div class="flex flex-col sm:flex-row sm:items-center gap-3 flex-wrap">
      <!-- Period Selector -->
      <div class="flex items-center gap-2 min-w-[220px]">
        <div
          class="w-8 h-8 rounded-lg bg-sky-50 text-sky-600 border border-sky-200/70 flex items-center justify-center shrink-0"
          aria-hidden="true"
        >
          <Calendar class="w-4 h-4" />
        </div>
        <div class="flex-1 min-w-0">
          <label
            for="kpi-period-select"
            class="block text-[11px] font-semibold uppercase tracking-wider text-slate-400"
          >
            Academic Period
          </label>
          <Select
            v-if="periodSelectOptions.length > 0"
            input-id="kpi-period-select"
            :model-value="selectedPeriodId"
            :options="periodSelectOptions"
            option-label="label"
            option-value="id"
            placeholder="Select Semester"
            class="w-full sm:w-52 text-xs"
            size="small"
            data-testid="kpi-period-selector"
            @update:model-value="handlePeriodChange"
          />
          <div
            v-else
            class="text-xs font-semibold text-slate-700 py-1"
            data-testid="kpi-period-fallback"
          >
            {{ resolvedContext?.academicPeriodName ?? 'Active Semester' }}
          </div>
        </div>
      </div>

      <div class="hidden sm:block h-8 w-px bg-slate-200" aria-hidden="true" />

      <!-- Campus Selector (Synced with AppContextStore) -->
      <div class="flex items-center gap-2 min-w-[220px]">
        <div
          class="w-8 h-8 rounded-lg bg-teal-50 text-teal-600 border border-teal-200/70 flex items-center justify-center shrink-0"
          aria-hidden="true"
        >
          <Building2 class="w-4 h-4" />
        </div>
        <div class="flex-1 min-w-0">
          <label
            for="kpi-campus-select"
            class="block text-[11px] font-semibold uppercase tracking-wider text-slate-400"
          >
            Campus Scope
          </label>
          <Select
            input-id="kpi-campus-select"
            :model-value="selectedCampus"
            :options="campusSelectOptions"
            option-label="label"
            option-value="id"
            class="w-full sm:w-56 text-xs"
            size="small"
            data-testid="kpi-campus-selector"
            @update:model-value="handleCampusChange"
          />
        </div>
      </div>
    </div>

    <!-- School Level Pills + Prior Context + Refresh -->
    <div class="flex flex-wrap items-center justify-between lg:justify-end gap-2.5">
      <!-- School Level Filter Pills -->
      <div
        class="inline-flex items-center gap-1 bg-slate-100/80 p-1 rounded-lg border border-slate-200/70"
        role="group"
        aria-label="School level filter"
        data-testid="kpi-school-level-pills"
      >
        <span class="px-1.5 text-slate-400 flex items-center" aria-hidden="true">
          <GraduationCap class="w-3.5 h-3.5" />
        </span>
        <button
          v-for="option in schoolLevelOptions"
          :key="option.label"
          type="button"
          :aria-pressed="(props.schoolLevel ?? null) === option.value"
          :data-testid="`school-level-pill-${option.value ?? 'all'}`"
          :class="[
            'px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer',
            (props.schoolLevel ?? null) === option.value
              ? 'bg-slate-900 text-white shadow-2xs'
              : 'text-slate-600 hover:text-slate-900 hover:bg-white/70',
          ]"
          @click="handleSchoolLevelSelect(option.value)"
        >
          {{ option.label }}
        </button>
      </div>

      <!-- Comparison Baseline Pill -->
      <span
        v-if="resolvedContext?.priorAcademicPeriodName"
        class="hidden xl:inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-medium bg-slate-50 text-slate-600 border border-slate-200"
        data-testid="kpi-prior-period-badge"
      >
        vs {{ resolvedContext.priorAcademicPeriodName }}
      </span>

      <!-- Refresh Button -->
      <Button
        type="button"
        severity="secondary"
        outlined
        size="small"
        :loading="isLoading"
        aria-label="Refresh KPI data"
        data-testid="kpi-refresh-button"
        @click="emit('refresh')"
      >
        <template #icon>
          <RefreshCw :class="['w-3.5 h-3.5', isLoading ? 'animate-spin' : '']" />
        </template>
      </Button>
    </div>
  </div>
</template>
