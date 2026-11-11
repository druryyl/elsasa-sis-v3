<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import Button from 'primevue/button';
import {
  Activity,
  AlertTriangle,
  Building2,
  Calendar,
  CreditCard,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  UserPlus,
} from 'lucide-vue-next';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useAuthStore } from '@/app/stores/auth.store';
import PageHeader from '@/shared/components/layout/PageHeader.vue';
import StatusPill from '@/shared/components/StatusPill.vue';
import { useExecutiveDashboardQuery } from '../api/useDashboardQueries';
import KpiDomainHealthCard from '../components/KpiDomainHealthCard.vue';
import KpiFilterBar from '../components/KpiFilterBar.vue';
import KpiMetricCard from '../components/KpiMetricCard.vue';
import NeedsAttentionPanel from '../components/NeedsAttentionPanel.vue';
import type { GlobalKpiFilterDto } from '../types/dashboard.types';

const authStore = useAuthStore();
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
} = useExecutiveDashboardQuery(filter);

const greetingMessage = computed(() => {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
});

const userDisplayName = computed(() => {
  return authStore.currentUser?.fullName || authStore.currentUser?.username || 'Administrator';
});

const userRoleBadge = computed(() => {
  if (authStore.currentUser?.roles && authStore.currentUser.roles.length > 0) {
    return authStore.currentUser.roles.join(', ');
  }
  return 'Administrator';
});

const activeCampusDisplay = computed(() => {
  const campus = data.value?.context.campus ?? appContextStore.activeCampusId;
  if (campus === 'campus-main') return 'Main Campus (Kampus A)';
  if (campus === 'campus-east') return 'East Campus (Kampus B)';
  return 'All Campuses (Semua Kampus)';
});

const activePeriodDisplay = computed(() => {
  if (data.value?.context) {
    return `${data.value.context.academicYearCode} · ${data.value.context.academicPeriodName}`;
  }
  return appContextStore.activeAcademicPeriodId ?? 'Active Academic Semester';
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
  return 'Unable to load executive KPI metrics. Please check your connection and try again.';
});

const headlineKpis = computed(() => data.value?.headlineKpis ?? []);
const domainSummaries = computed(() => data.value?.domainSummaries ?? []);
const needsAttentionQueue = computed(() => data.value?.needsAttentionQueue ?? []);
const availablePeriods = computed(() => data.value?.availablePeriods ?? []);
const resolvedContext = computed(() => data.value?.context ?? null);

function handleRefresh() {
  void refetch();
}
</script>

<template>
  <div data-testid="dashboard-view" class="space-y-6">
    <!-- View Header -->
    <PageHeader
      title="Executive Overview"
      subtitle="Live institutional health command center across Students, Academics, Finance, Teaching, and Operations."
    >
      <template #actions>
        <div class="flex items-center gap-2">
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
            data-testid="dashboard-generated-at"
          >
            <Activity class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
            <span>Updated {{ generatedAtLabel }}</span>
          </div>
        </div>
      </template>
    </PageHeader>

    <!-- Institutional Greeting Hero & Quick Action Launchers -->
    <section
      class="relative overflow-hidden rounded-2xl bg-gradient-to-r from-slate-900 via-slate-800 to-teal-950 p-5 sm:p-6 text-white shadow-md border border-slate-700/60"
      data-testid="session-greeting-card"
    >
      <div
        class="absolute -right-12 -top-12 w-64 h-64 rounded-full bg-teal-500/15 blur-3xl pointer-events-none"
        aria-hidden="true"
      />
      <div
        class="absolute right-48 -bottom-10 w-64 h-64 rounded-full bg-indigo-500/15 blur-3xl pointer-events-none"
        aria-hidden="true"
      />

      <div class="relative z-10 flex flex-col lg:flex-row lg:items-center justify-between gap-5">
        <div class="space-y-2">
          <div class="flex items-center gap-2 flex-wrap">
            <span
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium bg-teal-500/20 text-teal-300 border border-teal-500/30"
            >
              <Sparkles class="w-3 h-3 text-teal-300" aria-hidden="true" />
              <span>Level 1 Executive Command Center</span>
            </span>
            <span
              class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-white/10 text-slate-200 border border-white/15"
            >
              <ShieldCheck class="w-3 h-3 text-emerald-300" aria-hidden="true" />
              <span>{{ userRoleBadge }}</span>
            </span>
          </div>

          <h2
            class="text-xl sm:text-2xl font-bold tracking-tight text-white"
            data-testid="session-greeting-title"
          >
            {{ greetingMessage }}, {{ userDisplayName }}
          </h2>

          <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-300">
            <span class="inline-flex items-center gap-1.5" data-testid="active-campus-label">
              <Building2 class="w-3.5 h-3.5 text-teal-300" aria-hidden="true" />
              <span>{{ activeCampusDisplay }}</span>
            </span>
            <span class="inline-flex items-center gap-1.5" data-testid="active-period-label">
              <Calendar class="w-3.5 h-3.5 text-sky-300" aria-hidden="true" />
              <span>{{ activePeriodDisplay }}</span>
            </span>
          </div>
        </div>

        <!-- Preserved Quick Access Action Launchers -->
        <div class="flex flex-wrap items-center gap-2 shrink-0">
          <RouterLink
            to="/admission/apply"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-gradient-to-r from-teal-500 to-emerald-500 text-white shadow-sm hover:from-teal-400 hover:to-emerald-400 transition-all"
          >
            <UserPlus class="w-4 h-4" aria-hidden="true" />
            <span>New Admission</span>
          </RouterLink>
          <RouterLink
            to="/operations/calendar"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-white/10 text-white border border-white/20 hover:bg-white/20 transition-all"
          >
            <Calendar class="w-4 h-4 text-amber-300" aria-hidden="true" />
            <span>View Calendar</span>
          </RouterLink>
          <RouterLink
            to="/finance/payments"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-white/10 text-white border border-white/20 hover:bg-white/20 transition-all"
          >
            <CreditCard class="w-4 h-4 text-emerald-300" aria-hidden="true" />
            <span>Receive Payment</span>
          </RouterLink>
        </div>
      </div>
    </section>

    <!-- Global KPI Filter Bar (Campus, Academic Period, School Level) -->
    <KpiFilterBar
      :available-periods="availablePeriods"
      :resolved-context="resolvedContext"
      :school-level="selectedSchoolLevel"
      :is-loading="isFetching"
      @update:school-level="selectedSchoolLevel = $event"
      @refresh="handleRefresh"
    />

    <!-- Error State Banner / Card with Retry -->
    <div
      v-if="isError && !data"
      class="bg-rose-50 border border-rose-200 rounded-xl p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
      role="alert"
      data-testid="dashboard-error-state"
    >
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-xl bg-rose-100 text-rose-700 border border-rose-200 flex items-center justify-center shrink-0"
        >
          <AlertTriangle class="w-5 h-5" aria-hidden="true" />
        </div>
        <div class="space-y-1">
          <h3 class="text-sm font-bold text-rose-900">Failed to load Executive KPI Dashboard</h3>
          <p class="text-xs text-rose-700 leading-relaxed">{{ errorMessage }}</p>
        </div>
      </div>

      <Button
        type="button"
        label="Retry"
        severity="danger"
        size="small"
        :loading="isFetching"
        data-testid="dashboard-retry-button"
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
      data-testid="dashboard-refresh-error"
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
        data-testid="dashboard-retry-button"
        @click="handleRefresh"
      />
    </div>

    <!-- Initial Skeleton Loading State -->
    <template v-if="isPending && !data">
      <!-- 5 Headline KPI Skeletons -->
      <section class="space-y-3" aria-label="Loading headline KPIs" data-testid="headline-kpis-skeleton">
        <div class="h-5 w-48 rounded bg-slate-200 animate-pulse" />
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
          <div
            v-for="idx in 5"
            :key="`kpi-skel-${idx}`"
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

      <!-- 6 Domain Health Skeletons -->
      <section class="space-y-3" aria-label="Loading domain health" data-testid="domain-health-skeleton">
        <div class="h-5 w-52 rounded bg-slate-200 animate-pulse" />
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          <div
            v-for="idx in 6"
            :key="`domain-skel-${idx}`"
            class="bg-white border border-slate-200 rounded-xl p-5 space-y-4 animate-pulse"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-slate-200" />
                <div class="space-y-1.5">
                  <div class="h-4 w-28 rounded bg-slate-200" />
                  <div class="h-3 w-16 rounded bg-slate-100" />
                </div>
              </div>
              <div class="h-5 w-20 rounded-full bg-slate-200" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div class="h-14 rounded-lg bg-slate-100" />
              <div class="h-14 rounded-lg bg-slate-100" />
            </div>
          </div>
        </div>
      </section>

      <!-- Needs Attention Skeleton -->
      <section
        class="bg-white border border-slate-200 rounded-xl p-5 space-y-3 animate-pulse"
        aria-label="Loading attention queue"
        data-testid="needs-attention-skeleton"
      >
        <div class="h-5 w-40 rounded bg-slate-200" />
        <div class="h-14 rounded-lg bg-slate-100" />
        <div class="h-14 rounded-lg bg-slate-100" />
      </section>
    </template>

    <!-- Live Executive Dashboard Sections -->
    <template v-else-if="data">
      <!-- 1. 5 Headline Institutional KPI Cards -->
      <section class="space-y-3" data-testid="headline-kpis-section">
        <div class="flex items-center justify-between gap-2 flex-wrap">
          <div>
            <h2 class="text-base font-bold text-slate-900 tracking-tight">
              Headline Institutional KPIs
            </h2>
            <p class="text-xs text-slate-500">
              Primary health indicators across Students, Academics, Finance, Teaching, and Operations.
            </p>
          </div>
          <span
            v-if="resolvedContext?.priorAcademicPeriodName"
            class="text-xs font-medium text-slate-500 bg-slate-100 px-2.5 py-1 rounded-full border border-slate-200"
          >
            Trend baseline: {{ resolvedContext.priorAcademicPeriodName }}
          </span>
        </div>

        <div
          class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4"
          data-testid="headline-kpis-grid"
        >
          <KpiMetricCard
            v-for="metric in headlineKpis"
            :key="metric.code"
            :metric="metric"
            show-domain-badge
          />
        </div>
      </section>

      <!-- 2. 6 Domain Health Summary Cards (incl. Stage 3 Development Placeholder) -->
      <section class="space-y-3" data-testid="domain-health-section">
        <div class="flex items-center justify-between gap-2 flex-wrap">
          <div>
            <h2 class="text-base font-bold text-slate-900 tracking-tight">
              Domain Health &amp; Readiness
            </h2>
            <p class="text-xs text-slate-500">
              Drill into any accessible domain for Level 2 diagnostic breakdowns and period comparisons.
            </p>
          </div>
          <span class="text-xs font-semibold text-slate-600 bg-white px-2.5 py-1 rounded-full border border-slate-200">
            {{ domainSummaries.length }} Domains
          </span>
        </div>

        <div
          class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4"
          data-testid="domain-health-grid"
        >
          <KpiDomainHealthCard
            v-for="summary in domainSummaries"
            :key="summary.domainKey"
            :summary="summary"
          />
        </div>
      </section>

      <!-- 3. Prioritized Needs Attention Queue -->
      <section data-testid="needs-attention-section">
        <NeedsAttentionPanel :signals="needsAttentionQueue" />
      </section>
    </template>
  </div>
</template>
