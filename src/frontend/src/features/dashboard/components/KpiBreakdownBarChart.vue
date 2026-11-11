<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { ArrowUpRight, BarChart3 } from 'lucide-vue-next';
import ContentCard from '@/shared/components/layout/ContentCard.vue';
import type { KpiBreakdownDto, KpiBreakdownItemDto } from '../types/dashboard.types';
import { clampPercentage, resolveKpiSemanticStyle } from './kpiStatusStyles';

export interface KpiBreakdownBarChartProps {
  breakdown: KpiBreakdownDto;
  maxItems?: number | undefined;
}

const props = withDefaults(defineProps<KpiBreakdownBarChartProps>(), {
  maxItems: undefined,
});

const visibleItems = computed<KpiBreakdownItemDto[]>(() => {
  const items = props.breakdown.items ?? [];
  if (props.maxItems && props.maxItems > 0) {
    return items.slice(0, props.maxItems);
  }
  return items;
});

const maxCount = computed<number>(() => {
  return visibleItems.value.reduce((max, item) => Math.max(max, item.count), 0);
});

const isFunnel = computed(() => props.breakdown.chartType === 'funnel');

const chartTypeLabel = computed(() => {
  switch (props.breakdown.chartType) {
    case 'funnel':
      return 'Funnel';
    case 'stacked-bar':
      return 'Distribution';
    default:
      return 'Breakdown';
  }
});

function resolveBarWidth(item: KpiBreakdownItemDto): number {
  if (isFunnel.value && maxCount.value > 0) {
    return clampPercentage((item.count / maxCount.value) * 100);
  }
  return clampPercentage(item.percentage);
}

function formatCount(count: number): string {
  return Number.isInteger(count) ? count.toLocaleString() : count.toFixed(1);
}
</script>

<template>
  <ContentCard
    :title="breakdown.title"
    :subtitle="breakdown.subtitle"
    :data-testid="`kpi-breakdown-chart-${breakdown.code}`"
  >
    <template #header-actions>
      <span
        class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600 border border-slate-200"
      >
        <BarChart3 class="w-3 h-3 text-slate-500" aria-hidden="true" />
        <span>{{ chartTypeLabel }}</span>
      </span>
    </template>

    <div v-if="visibleItems.length === 0" class="py-6 text-center text-xs text-slate-500">
      No breakdown records match the current filter criteria.
    </div>

    <div v-else class="space-y-2.5" role="list" :aria-label="breakdown.title">
      <component
        :is="item.drillDownRoute ? RouterLink : 'div'"
        v-for="(item, index) in visibleItems"
        :key="item.key"
        :to="item.drillDownRoute || undefined"
        role="listitem"
        :data-testid="`breakdown-row-${item.key}`"
        :class="[
          'group block rounded-lg p-2.5 -mx-2 transition-colors',
          item.drillDownRoute ? 'hover:bg-slate-50 cursor-pointer' : '',
        ]"
      >
        <div class="flex items-center justify-between gap-2 text-xs mb-1.5">
          <div class="flex items-center gap-2 min-w-0">
            <span
              v-if="isFunnel"
              class="w-5 h-5 rounded-full bg-slate-100 text-slate-600 font-mono text-[10px] font-bold flex items-center justify-center shrink-0"
            >
              {{ index + 1 }}
            </span>
            <span
              v-else
              :class="['w-2 h-2 rounded-full shrink-0', resolveKpiSemanticStyle(item.status).dotClass]"
              aria-hidden="true"
            />
            <span
              class="font-semibold text-slate-800 group-hover:text-teal-800 transition-colors truncate"
            >
              {{ item.label }}
            </span>
          </div>

          <div class="flex items-center gap-2 shrink-0 tabular-nums">
            <span v-if="item.formattedAmount" class="font-semibold text-slate-700">
              {{ item.formattedAmount }}
            </span>
            <span class="font-bold text-slate-900">
              {{ formatCount(item.count) }}
            </span>
            <span class="text-[11px] font-medium text-slate-500 w-12 text-right">
              ({{ clampPercentage(item.percentage).toFixed(1) }}%)
            </span>
            <ArrowUpRight
              v-if="item.drillDownRoute"
              class="w-3.5 h-3.5 text-slate-400 group-hover:text-teal-600 transition-colors"
              aria-hidden="true"
            />
          </div>
        </div>

        <!-- Zero-dependency inline SVG progress bar -->
        <div class="w-full h-2.5 rounded-full overflow-hidden bg-slate-100">
          <svg
            class="w-full h-full block"
            viewBox="0 0 100 10"
            preserveAspectRatio="none"
            role="img"
            :aria-label="`${item.label}: ${clampPercentage(item.percentage).toFixed(1)}%`"
          >
            <rect x="0" y="0" width="100" height="10" rx="5" fill="#f1f5f9" />
            <rect
              x="0"
              y="0"
              :width="resolveBarWidth(item)"
              height="10"
              rx="5"
              :fill="resolveKpiSemanticStyle(item.status).svgFillHex"
            />
          </svg>
        </div>
      </component>
    </div>
  </ContentCard>
</template>
