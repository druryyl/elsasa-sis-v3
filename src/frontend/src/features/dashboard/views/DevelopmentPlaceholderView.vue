<script setup lang="ts">
import { RouterLink } from 'vue-router';
import {
  ArrowLeft,
  Compass,
  HeartHandshake,
  Lock,
  ShieldCheck,
  Sparkles,
  Trophy,
  Users,
} from 'lucide-vue-next';
import ContentCard from '@/shared/components/layout/ContentCard.vue';
import PageHeader from '@/shared/components/layout/PageHeader.vue';
import StatusPill from '@/shared/components/StatusPill.vue';
import type { BreadcrumbItem } from '@/shared/types/navigation';

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Executive Dashboard', to: '/dashboard' },
  { label: 'Student Development (Stage 3)', current: true },
];

interface PlannedDevelopmentMetric {
  code: string;
  title: string;
  description: string;
  targetArea: string;
}

const plannedMetrics: PlannedDevelopmentMetric[] = [
  {
    code: 'DEV-01',
    title: 'Holistic Growth & Character Index',
    description:
      'Tracks longitudinal student character formation, values reflection milestones, and advisory check-ins across school levels.',
    targetArea: 'Character Formation',
  },
  {
    code: 'DEV-02',
    title: 'Co-Curricular & Club Participation Rate',
    description:
      'Measures active learner engagement across athletics, arts, STEM societies, and student leadership organizations.',
    targetArea: 'Co-Curricular Engagement',
  },
  {
    code: 'DEV-03',
    title: 'Student Wellbeing & Pastoral Care Coverage',
    description:
      'Monitors counseling intake responsiveness, advisor check-in cadence, and early wellbeing support follow-ups.',
    targetArea: 'Pastoral Care & Wellbeing',
  },
  {
    code: 'DEV-04',
    title: 'Community Service & Leadership Milestones',
    description:
      'Aggregates verified service-learning hours, capstone reflections, and leadership competency completions.',
    targetArea: 'Service & Leadership',
  },
  {
    code: 'DEV-05',
    title: 'Family–Advisor Partnership Engagement',
    description:
      'Captures student-led conference attendance and advisor-family holistic progress alignment rates.',
    targetArea: 'Family Partnership',
  },
];
</script>

<template>
  <div data-testid="development-placeholder-view" class="space-y-6">
    <PageHeader
      title="Student Development Dashboard"
      subtitle="Holistic Student Development KPI intelligence planned for Stage 3."
      :breadcrumbs="breadcrumbs"
    >
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
            status="pending_stage"
            label="Stage 3 Pending"
            variant="slate"
            size="sm"
            data-testid="development-stage-status"
          />
        </div>
      </template>
    </PageHeader>

    <!-- Educational Banner -->
    <section
      class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-slate-900 via-slate-800 to-indigo-950 p-6 text-white border border-slate-700/60 shadow-md"
      data-testid="development-roadmap-banner"
    >
      <div
        class="absolute -right-10 -top-10 w-56 h-56 rounded-full bg-indigo-500/20 blur-3xl pointer-events-none"
        aria-hidden="true"
      />
      <div class="relative z-10 flex flex-col lg:flex-row lg:items-center justify-between gap-6">
        <div class="space-y-3 max-w-2xl">
          <div class="flex items-center gap-2 flex-wrap">
            <span
              class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-white/10 text-slate-200 border border-white/15"
            >
              <Lock class="w-3.5 h-3.5 text-amber-300" aria-hidden="true" />
              <span>Stage 3 Pending</span>
            </span>
            <span
              class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-indigo-500/20 text-indigo-200 border border-indigo-400/30"
            >
              <Sparkles class="w-3.5 h-3.5" aria-hidden="true" />
              <span>Holistic Student Development Module</span>
            </span>
          </div>

          <h2 class="text-xl font-bold tracking-tight text-white">
            Holistic Student Development Metrics Are Scheduled for Stage 3
          </h2>

          <p class="text-sm text-slate-300 leading-relaxed">
            Release 1 delivers live institutional telemetry across the five operational domains
            (Students, Academics, Finance, Teaching, and Operations). Once the Stage 3 Holistic
            Student Development module and pastoral care workflows are provisioned, this dashboard
            will illuminate character formation, wellbeing, co-curricular engagement, and service
            learning KPIs (<span class="font-mono text-indigo-200">DEV-01</span> –
            <span class="font-mono text-indigo-200">DEV-05</span>).
          </p>
        </div>

        <div
          class="bg-white/10 border border-white/15 rounded-xl p-4 sm:p-5 shrink-0 lg:w-72 space-y-2.5"
        >
          <div class="text-xs font-semibold uppercase tracking-wider text-indigo-200">
            Release Readiness
          </div>
          <div class="flex items-center justify-between text-sm">
            <span class="text-slate-300">Current Status</span>
            <span class="font-bold text-white">Stage 3 Pending</span>
          </div>
          <div class="flex items-center justify-between text-sm">
            <span class="text-slate-300">Target KPI Scope</span>
            <span class="font-mono font-semibold text-indigo-200">DEV-01 – DEV-05</span>
          </div>
          <div class="flex items-center justify-between text-sm">
            <span class="text-slate-300">Active Domains Live</span>
            <span class="font-semibold text-emerald-300">5 / 5 Operational</span>
          </div>
        </div>
      </div>
    </section>

    <!-- Preview of Planned Stage 3 Holistic Metrics -->
    <ContentCard
      title="Planned Stage 3 Holistic Student Development Indicators"
      subtitle="Educational preview of the metrics that will activate in this workspace once Stage 3 data pipelines are enabled."
      data-testid="development-planned-metrics-card"
    >
      <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        <article
          v-for="metric in plannedMetrics"
          :key="metric.code"
          class="rounded-xl border border-dashed border-slate-300 bg-slate-50/70 p-4 space-y-2.5"
          :data-testid="`planned-metric-${metric.code}`"
        >
          <div class="flex items-center justify-between gap-2">
            <span
              class="font-mono text-[11px] font-bold px-2 py-0.5 rounded bg-white text-slate-700 border border-slate-200"
            >
              {{ metric.code }}
            </span>
            <span
              class="text-[11px] font-semibold text-slate-500 bg-slate-200/70 px-2 py-0.5 rounded-full"
            >
              Stage 3 Pending
            </span>
          </div>

          <h3 class="text-sm font-bold text-slate-900">{{ metric.title }}</h3>
          <p class="text-xs text-slate-600 leading-relaxed">{{ metric.description }}</p>

          <div class="pt-1 flex items-center gap-1.5 text-[11px] font-medium text-slate-500">
            <Compass class="w-3.5 h-3.5 text-indigo-500" aria-hidden="true" />
            <span>{{ metric.targetArea }}</span>
          </div>
        </article>
      </div>

      <template #footer>
        <div class="w-full flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div class="flex items-center gap-4 text-xs text-slate-500 flex-wrap">
            <span class="inline-flex items-center gap-1.5">
              <HeartHandshake class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
              <span>Pastoral &amp; Advisory Care</span>
            </span>
            <span class="inline-flex items-center gap-1.5">
              <Trophy class="w-3.5 h-3.5 text-amber-600" aria-hidden="true" />
              <span>Co-Curricular Growth</span>
            </span>
            <span class="inline-flex items-center gap-1.5">
              <ShieldCheck class="w-3.5 h-3.5 text-indigo-600" aria-hidden="true" />
              <span>Zero-DDL Release 1 Isolation</span>
            </span>
            <span class="inline-flex items-center gap-1.5">
              <Users class="w-3.5 h-3.5 text-sky-600" aria-hidden="true" />
              <span>Family Partnership</span>
            </span>
          </div>

          <RouterLink
            to="/dashboard"
            class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-lg text-xs font-semibold bg-slate-900 text-white hover:bg-slate-800 transition-colors shrink-0"
          >
            <ArrowLeft class="w-3.5 h-3.5" aria-hidden="true" />
            <span>Return to Executive Overview</span>
          </RouterLink>
        </div>
      </template>
    </ContentCard>
  </div>
</template>
