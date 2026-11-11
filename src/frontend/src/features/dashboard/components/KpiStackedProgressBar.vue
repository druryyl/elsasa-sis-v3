<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { ArrowUpRight } from 'lucide-vue-next';
import type { KpiBreakdownItemDto } from '../types/dashboard.types';
import { clampPercentage, resolveKpiSemanticStyle } from './kpiStatusStyles';

export interface KpiStackedProgressBarProps {
  title?: string | undefined;
  subtitle?: string | undefined;
  items: KpiBreakdownItemDto[];
  totalLabel?: string | undefined;
}

const props = withDefaults(defineProps<KpiStackedProgressBarProps>(), {
  title: undefined,
  subtitle: undefined,
  totalLabel: undefined,
});

const hoveredKey = ref<string | null>(null);

const nonZeroItems = computed<KpiBreakdownItemDto[]>(() => {
  return (props.items ?? []).filter((item) => item.percentage > 0 || item.count > 0);
});

const totalPercentage = computed<number>(() => {
  return nonZeroItems.value.reduce((sum, item) => sum + Math.max(0, item.percentage), 0);
});

function segmentWidth(item: KpiBreakdownItemDto): number {
  const pct = Math.max(0, item.percentage);
  if (totalPercentage.value <= 0) return 0;
  return clampPercentage((pct / Math.max(totalPercentage.value, 100)) * 100);
}
</script>

<template>
  <div
    class="bg-white border border-slate-200 rounded-xl shadow-xs p-4 sm:p-5 space-y-4"
    data-testid="kpi-stacked-progress-bar"
  >
    <!-- Header -->
    <div v-if="title || subtitle || totalLabel" class="flex items-start justify-between gap-2">
      <div>
        <h3 v-if="title" class="text-sm font-bold text-slate-900">{{ title }}</h3>
        <p v-if="subtitle" class="text-xs text-slate-500 mt-0.5">{{ subtitle }}</p>
      </div>
      <span
        v-if="totalLabel"
        class="text-xs font-semibold text-slate-700 bg-slate-100 px-2.5 py-0.5 rounded-full border border-slate-200 shrink-0"
      >
        {{ totalLabel }}
      </span>
    </div>

    <!-- Empty State -->
    <div v-if="items.length === 0" class="py-4 text-center text-xs text-slate-500">
      No distribution data available.
    </div>

    <template v-else>
      <!-- Interactive Segmented Horizontal Bar (SVG segments wrapped in RouterLinks) -->
      <div
        class="w-full h-4 rounded-full bg-slate-100 overflow-hidden flex items-stretch gap-0.5 p-0.5 border border-slate-200/70"
        role="group"
        :aria-label="title ?? 'Segmented distribution bar'"
        data-testid="stacked-bar-track"
      >
        <component
          :is="item.drillDownRoute ? RouterLink : 'div'"
          v-for="item in nonZeroItems"
          :key="item.key"
          :to="item.drillDownRoute || undefined"
          :style="{ width: `${segmentWidth(item)}%` }"
          :title="`${item.label}: ${item.count} (${clampPercentage(item.percentage).toFixed(1)}%)`"
          :data-testid="`stacked-segment-${item.key}`"
          :class="[
            'block h-full first:rounded-l-full last:rounded-r-full transition-opacity duration-150 min-w-[4px]',
            hoveredKey && hoveredKey !== item.key ? 'opacity-45' : 'opacity-100',
            item.drillDownRoute ? 'cursor-pointer' : '',
          ]"
          @mouseenter="hoveredKey = item.key"
          @mouseleave="hoveredKey = null"
        >
          <svg class="w-full h-full block" viewBox="0 0 100 10" preserveAspectRatio="none">
            <rect
              x="0"
              y="0"
              width="100"
              height="10"
              :fill="resolveKpiSemanticStyle(item.status).svgFillHex"
            />
          </svg>
        </component>
      </div>

      <!-- Interactive Legend -->
      <div
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2"
        data-testid="stacked-bar-legend"
      >
        <component
          :is="item.drillDownRoute ? RouterLink : 'div'"
          v-for="item in items"
          :key="item.key"
          :to="item.drillDownRoute || undefined"
          :data-testid="`stacked-legend-item-${item.key}`"
          :class="[
            'group flex items-center justify-between gap-2 px-2.5 py-2 rounded-lg border text-xs transition-all',
            hoveredKey === item.key
              ? 'border-slate-300 bg-slate-50 shadow-2xs'
              : 'border-slate-200/70 bg-white hover:bg-slate-50 hover:border-slate-300',
            item.drillDownRoute ? 'cursor-pointer' : '',
          ]"
          @mouseenter="hoveredKey = item.key"
          @mouseleave="hoveredKey = null"
        >
          <div class="flex items-center gap-2 min-w-0">
            <span
              :class="['w-2.5 h-2.5 rounded-full shrink-0', resolveKpiSemanticStyle(item.status).dotClass]"
              aria-hidden="true"
            />
            <span class="font-medium text-slate-700 group-hover:text-slate-900 truncate">
              {{ item.label }}
            </span>
          </div>

          <div class="flex items-center gap-1.5 shrink-0 tabular-nums">
            <span class="font-bold text-slate-900">{{ item.count }}</span>
            <span class="text-slate-500 text-[11px]">
              ({{ clampPercentage(item.percentage).toFixed(0) }}%)
            </span>
            <ArrowUpRight
              v-if="item.drillDownRoute"
              class="w-3 h-3 text-slate-400 group-hover:text-teal-600 transition-colors"
              aria-hidden="true"
            />
          </div>
        </component>
      </div>
    </template>
  </div>
</template>
