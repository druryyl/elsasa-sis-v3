<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { ArrowRight, Minus, TrendingDown, TrendingUp } from 'lucide-vue-next';
import StatusPill from '@/shared/components/StatusPill.vue';
import type { KpiMetricCardDto } from '../types/dashboard.types';
import { resolveKpiSemanticStyle } from './kpiStatusStyles';

export interface KpiMetricCardProps {
  metric: KpiMetricCardDto;
  showDomainBadge?: boolean | undefined;
}

const props = withDefaults(defineProps<KpiMetricCardProps>(), {
  showDomainBadge: false,
});

const semanticStyle = computed(() => resolveKpiSemanticStyle(props.metric.status));

const trendIcon = computed(() => {
  switch (props.metric.trendDirection) {
    case 'up':
      return TrendingUp;
    case 'down':
      return TrendingDown;
    default:
      return Minus;
  }
});

const trendChipClass = computed(() => {
  switch (props.metric.trendDirection) {
    case 'up':
      return 'bg-emerald-50 text-emerald-700 border-emerald-200/80';
    case 'down':
      return 'bg-rose-50 text-rose-700 border-rose-200/80';
    default:
      return 'bg-slate-100 text-slate-600 border-slate-200';
  }
});
</script>

<template>
  <article
    class="group relative bg-white border border-slate-200 rounded-xl shadow-xs hover:shadow-md hover:border-slate-300 transition-all duration-200 overflow-hidden flex flex-col justify-between"
    :data-testid="`kpi-metric-card-${metric.code}`"
  >
    <!-- Semantic top accent stripe -->
    <div :class="['h-1 w-full bg-gradient-to-r', semanticStyle.accentBarClass]" />

    <div class="p-4 sm:p-5 flex-1 flex flex-col justify-between gap-3">
      <!-- Top row: Code + Domain + Status Badge -->
      <div class="flex items-start justify-between gap-2">
        <div class="flex items-center gap-1.5 flex-wrap">
          <span
            class="font-mono text-[11px] font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-700 border border-slate-200/80"
            data-testid="kpi-metric-code"
          >
            {{ metric.code }}
          </span>
          <span
            v-if="showDomainBadge && metric.domain"
            class="text-[11px] font-semibold uppercase tracking-wider px-2 py-0.5 rounded bg-slate-50 text-slate-500 border border-slate-200/70"
            data-testid="kpi-metric-domain"
          >
            {{ metric.domain }}
          </span>
        </div>

        <StatusPill
          :status="metric.status"
          :label="semanticStyle.statusLabel"
          :variant="semanticStyle.pillVariant"
          size="sm"
          data-testid="kpi-metric-status"
        />
      </div>

      <!-- Label & Headline Value -->
      <div class="space-y-1.5">
        <h3 class="text-xs font-semibold text-slate-600 leading-snug" data-testid="kpi-metric-label">
          {{ metric.label }}
        </h3>

        <div class="flex items-baseline justify-between gap-2 flex-wrap">
          <div
            class="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900 tabular-nums"
            data-testid="kpi-metric-value"
          >
            {{ metric.formattedValue }}
          </div>

          <span
            v-if="metric.deltaLabel"
            :class="[
              'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold border',
              trendChipClass,
            ]"
            data-testid="kpi-metric-delta"
          >
            <component :is="trendIcon" class="w-3 h-3 shrink-0" aria-hidden="true" />
            <span>{{ metric.deltaLabel }}</span>
          </span>
        </div>
      </div>

      <!-- Subtitle & Target Threshold -->
      <div class="space-y-1 text-xs">
        <p v-if="metric.subtitle" class="text-slate-500 leading-relaxed" data-testid="kpi-metric-subtitle">
          {{ metric.subtitle }}
        </p>
        <p
          v-if="metric.targetLabel"
          class="text-[11px] font-medium text-slate-400"
          data-testid="kpi-metric-target"
        >
          {{ metric.targetLabel }}
        </p>
      </div>
    </div>

    <!-- Footer Drill-Down Link -->
    <div
      v-if="metric.drillDownRoute"
      class="px-4 sm:px-5 py-2.5 bg-slate-50/80 border-t border-slate-100 flex items-center justify-between"
    >
      <RouterLink
        :to="metric.drillDownRoute"
        class="inline-flex items-center justify-between w-full text-xs font-semibold text-teal-700 hover:text-teal-900 transition-colors"
        data-testid="kpi-metric-drilldown"
      >
        <span>Inspect Workbench</span>
        <ArrowRight
          class="w-3.5 h-3.5 text-teal-600 group-hover:translate-x-0.5 transition-transform"
          aria-hidden="true"
        />
      </RouterLink>
    </div>
  </article>
</template>
