<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import Button from 'primevue/button';
import { Activity, AlertTriangle, ArrowLeft, RefreshCw } from 'lucide-vue-next';
import { useAppContextStore } from '@/app/stores/appContext.store';
import PageHeader from '@/shared/components/layout/PageHeader.vue';
import StatusPill from '@/shared/components/StatusPill.vue';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useDomainKpiDashboardQuery } from '../api/useDashboardQueries';
import type {
  GlobalKpiFilterDto,
  KpiBreakdownItemDto,
  OperationalKpiDomainKey,
} from '../types/dashboard.types';
import KpiBreakdownBarChart from './KpiBreakdownBarChart.vue';
import KpiFilterBar from './KpiFilterBar.vue';
import KpiMetricCard from './KpiMetricCard.vue';
import KpiStackedProgressBar from './KpiStackedProgressBar.vue';
import NeedsAttentionPanel from './NeedsAttentionPanel.vue';

export interface DomainKpiDashboardShellProps {
  domain: OperationalKpiDomainKey;
  title: string;
  subtitle: string;
}

const props = defineProps<DomainKpiDashboardShellProps>();

const appContextStore = useAppContextStore();
const selectedSchoolLevel = ref<string | null>(null);

const filter = computed<GlobalKpiFilterDto>(() => ({
  academicPeriodId: appContextStore.activeAcademicPeriodId,
  campus: appContextStore.activeCampusId,
  schoolLevel: selectedSchoolLevel.value,
}));

const {
  data,
  isPending,
  isFetching,
  isError,
  error,
  refetch,
} = useDomainKpiDashboardQuery(() => props.domain, filter);

const breadcrumbs = computed<BreadcrumbItem[]>(() => [
  { label: 'Executive Dashboard', to: '/dashboard' },
  { label: props.title, current: true },
]);

const headlineKpis = computed(() => data.value?.headlineKpis ?? []);
const breakdowns = computed(() => data.value?.breakdowns ?? []);
const attentionSignals = computed(() => data.value?.attentionSignals ?? []);
const availablePeriods = computed(() => data.value?.availablePeriods ?? []);
const resolvedContext = computed(() => data.value?.context ?? null);

const headlineGridClass = computed(() => {
  const count = headlineKpis.value.length;
  if (count <= 2) {
    return 'grid grid-cols-1 sm:grid-cols-2 gap-4';
  }
  if (count === 3) {
    return 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4';
  }
  if (count === 4) {
    return 'grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4';
  }
  return 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4';
});

const generatedAtLabel = computed(() => {
  if (!data.value?.generatedAtUtc) return null;
  const parsed = new Date(data.value.generatedAtUtc);
  if (Number.isNaN(parsed.getTime())) return null;
  return parsed.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
});

const errorMessage = computed(() => {
  if (error.value instanceof Error && error.value.message.trim().length > 0) {
    return error.value.message;
  }
  return `Unable to load ${props.title} metrics. Please check your connection and try again.`;
});

function formatTotalItemsLabel(items: KpiBreakdownItemDto[]): string {
  const total = items.reduce((sum, item) => sum + item.count, 0);
  const formatted = Number.isInteger(total) ? total.toLocaleString() : total.toFixed(1);
  return `${formatted} total`;
}

function handleRefresh() {
  void refetch();
}
</script>

<template>
  <div :data-testid="`${domain}-kpi-dashboard-view`" class="space-y-6">
    <!-- Domain Header with Breadcrumb & Back to Executive Overview -->
    <PageHeader :title="title" :subtitle="subtitle" :breadcrumbs="breadcrumbs">
      <template #actions>
        <div class="flex items-center gap-2 flex-wrap">
          <RouterLink
            to="/dashboard"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-white text-slate-700 border border-slate-200 hover:bg-slate-50 hover:border-slate-300 transition-colors"
            data-testid="back-to-executive-link"
          >
            <ArrowLeft class="w-3.5 h-3.5 text-slate-500" aria-hidden="true" />
            <span>Executive Overview</span>
          </RouterLink>

          <StatusPill
            :status="isError ? 'warning' : 'active'"
            :label="isFetching ? 'Syncing…' : isError ? 'Sync Warning' : 'Live KPIs'"
            :variant="isError ? 'amber' : 'emerald'"
            dot
            size="sm"
          />

          <div
            v-if="generatedAtLabel"
            class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-600 border border-slate-200/80"
            data-testid="domain-dashboard-generated-at"
          >
            <Activity class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
            <span>Updated {{ generatedAtLabel }}</span>
          </div>
        </div>
      </template>
    </PageHeader>

    <!-- Synchronized Global KPI Filter Bar -->
    <KpiFilterBar
      :available-periods="availablePeriods"
      :resolved-context="resolvedContext"
      :school-level="selectedSchoolLevel"
      :is-loading="isFetching"
      @update:school-level="selectedSchoolLevel = $event"
      @refresh="handleRefresh"
    />

    <!-- Initial Error State with Retry -->
    <div
      v-if="isError && !data"
      class="bg-rose-50 border border-rose-200 rounded-xl p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
      role="alert"
      data-testid="domain-dashboard-error-state"
    >
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-xl bg-rose-100 text-rose-700 border border-rose-200 flex items-center justify-center shrink-0"
        >
          <AlertTriangle class="w-5 h-5" aria-hidden="true" />
        </div>
        <div class="space-y-1">
          <h2 class="text-sm font-bold text-rose-900">Failed to load {{ title }}</h2>
          <p class="text-xs text-rose-700 leading-relaxed">{{ errorMessage }}</p>
        </div>
      </div>

      <Button
        type="button"
        label="Retry"
        severity="danger"
        size="small"
        :loading="isFetching"
        data-testid="domain-dashboard-retry-button"
        @click="handleRefresh"
      >
        <template #icon>
          <RefreshCw class="w-3.5 h-3.5 mr-1.5" aria-hidden="true" />
        </template>
      </Button>
    </div>

    <!-- Non-blocking Background Refetch Error Notice -->
    <div
      v-else-if="isError && data"
      class="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
      role="status"
      data-testid="domain-dashboard-refresh-error"
    >
      <div class="flex items-center gap-2 text-xs font-medium text-amber-800">
        <AlertTriangle class="w-4 h-4 text-amber-600 shrink-0" aria-hidden="true" />
        <span>Unable to refresh KPI metrics ({{ errorMessage }}). Showing last synced snapshot.</span>
      </div>
      <Button
        type="button"
        label="Retry"
        severity="warn"
        outlined
        size="small"
        :loading="isFetching"
        data-testid="domain-dashboard-retry-button"
        @click="handleRefresh"
      />
    </div>

    <!-- Skeleton Loading State -->
    <template v-if="isPending && !data">
      <section
        class="space-y-3"
        aria-label="Loading domain KPIs"
        data-testid="domain-headline-kpis-skeleton"
      >
        <div class="h-5 w-48 rounded bg-slate-200 animate-pulse" />
        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          <div
            v-for="idx in 4"
            :key="`domain-kpi-skel-${idx}`"
            class="bg-white border border-slate-200 rounded-xl p-5 space-y-4 animate-pulse"
          >
            <div class="flex items-center justify-between">
              <div class="h-4 w-16 rounded bg-slate-200" />
              <div class="h-5 w-16 rounded-full bg-slate-200" />
            </div>
            <div class="space-y-2">
              <div class="h-3.5 w-32 rounded bg-slate-200" />
              <div class="h-8 w-24 rounded bg-slate-200" />
            </div>
            <div class="h-3 w-40 rounded bg-slate-100" />
          </div>
        </div>
      </section>

      <section
        class="grid grid-cols-1 lg:grid-cols-2 gap-4"
        aria-label="Loading diagnostic breakdowns"
        data-testid="domain-breakdowns-skeleton"
      >
        <div
          v-for="idx in 2"
          :key="`breakdown-skel-${idx}`"
          class="bg-white border border-slate-200 rounded-xl p-5 space-y-4 animate-pulse"
        >
          <div class="h-4 w-44 rounded bg-slate-200" />
          <div class="space-y-3">
            <div class="h-6 rounded bg-slate-100" />
            <div class="h-6 rounded bg-slate-100" />
            <div class="h-6 rounded bg-slate-100" />
          </div>
        </div>
      </section>

      <section
        class="bg-white border border-slate-200 rounded-xl p-5 space-y-3 animate-pulse"
        aria-label="Loading domain attention signals"
        data-testid="domain-attention-skeleton"
      >
        <div class="h-5 w-44 rounded bg-slate-200" />
        <div class="h-14 rounded-lg bg-slate-100" />
      </section>
    </template>

    <!-- Live Level 2 Domain Dashboard Content -->
    <template v-else-if="data">
      <!-- 1. Domain Headline KPI Cards -->
      <section class="space-y-3" data-testid="domain-headline-kpis-section">
        <div class="flex items-center justify-between gap-2 flex-wrap">
          <div>
            <h2 class="text-base font-bold text-slate-900 tracking-tight">
              {{ data.domainName || title }} Key Indicators
            </h2>
            <p class="text-xs text-slate-500">
              Domain performance metrics with period-over-period comparison and direct workbench drill-down.
            </p>
          </div>
          <span
            v-if="resolvedContext?.priorAcademicPeriodName"
            class="text-xs font-medium text-slate-500 bg-slate-100 px-2.5 py-1 rounded-full border border-slate-200"
            data-testid="domain-prior-period-label"
          >
            Trend baseline: {{ resolvedContext.priorAcademicPeriodName }}
          </span>
        </div>

        <div :class="headlineGridClass" data-testid="domain-headline-kpis-grid">
          <KpiMetricCard
            v-for="metric in headlineKpis"
            :key="metric.code"
            :metric="metric"
          />
        </div>
      </section>

      <!-- 2. Diagnostic Breakdown Charts -->
      <section class="space-y-3" data-testid="domain-breakdowns-section">
        <div class="flex items-center justify-between gap-2 flex-wrap">
          <div>
            <h2 class="text-base font-bold text-slate-900 tracking-tight">
              Diagnostic Breakdowns
            </h2>
            <p class="text-xs text-slate-500">
              Select any bar or distribution segment to open the filtered operational workbench.
            </p>
          </div>
          <span class="text-xs font-semibold text-slate-600 bg-white px-2.5 py-1 rounded-full border border-slate-200">
            {{ breakdowns.length }} {{ breakdowns.length === 1 ? 'Chart' : 'Charts' }}
          </span>
        </div>

        <div
          v-if="breakdowns.length === 0"
          class="bg-white border border-slate-200 rounded-xl p-6 text-center text-xs text-slate-500"
          data-testid="domain-breakdowns-empty"
        >
          No diagnostic breakdowns available for the selected filter criteria.
        </div>

        <div
          v-else
          class="grid grid-cols-1 lg:grid-cols-2 gap-4"
          data-testid="domain-breakdowns-grid"
        >
          <template v-for="breakdown in breakdowns" :key="breakdown.code">
            <div
              v-if="breakdown.chartType === 'stacked-bar'"
              :data-testid="`kpi-breakdown-chart-${breakdown.code}`"
            >
              <KpiStackedProgressBar
                :title="breakdown.title"
                :subtitle="breakdown.subtitle"
                :items="breakdown.items"
                :total-label="formatTotalItemsLabel(breakdown.items)"
              />
            </div>
            <KpiBreakdownBarChart v-else :breakdown="breakdown" />
          </template>
        </div>
      </section>

      <!-- 3. Domain-Scoped Needs Attention Queue -->
      <section data-testid="domain-attention-section">
        <NeedsAttentionPanel
          :signals="attentionSignals"
          :title="`${data.domainName || title} Attention Queue`"
          :subtitle="`Operational exceptions requiring follow-up in ${data.domainName || domain}.`"
        />
      </section>
    </template>
  </div>
</template>
