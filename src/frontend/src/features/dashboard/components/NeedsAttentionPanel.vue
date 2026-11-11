<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import { AlertTriangle, ArrowRight, CheckCircle2, ShieldAlert } from 'lucide-vue-next';
import ContentCard from '@/shared/components/layout/ContentCard.vue';
import StatusPill from '@/shared/components/StatusPill.vue';
import type { AttentionSignalDto } from '../types/dashboard.types';
import { resolveKpiSemanticStyle } from './kpiStatusStyles';

export interface NeedsAttentionPanelProps {
  signals: AttentionSignalDto[];
  title?: string | undefined;
  subtitle?: string | undefined;
  emptyMessage?: string | undefined;
  maxVisible?: number | undefined;
}

const props = withDefaults(defineProps<NeedsAttentionPanelProps>(), {
  title: 'Needs Attention',
  subtitle: 'Prioritized operational exceptions requiring review or follow-up.',
  emptyMessage: 'All monitored operational thresholds are currently healthy.',
  maxVisible: undefined,
});

const visibleSignals = computed<AttentionSignalDto[]>(() => {
  if (props.maxVisible && props.maxVisible > 0) {
    return props.signals.slice(0, props.maxVisible);
  }
  return props.signals;
});

const criticalCount = computed(
  () => props.signals.filter((s) => s.severity.toLowerCase() === 'critical').length,
);

const warningCount = computed(
  () => props.signals.filter((s) => s.severity.toLowerCase() !== 'critical').length,
);

function isCritical(severity: string): boolean {
  return severity.toLowerCase() === 'critical';
}
</script>

<template>
  <ContentCard :title="title" :subtitle="subtitle" data-testid="needs-attention-panel">
    <template #header-actions>
      <div class="flex items-center gap-1.5 flex-wrap">
        <span
          v-if="criticalCount > 0"
          class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200/80"
          data-testid="attention-critical-summary"
        >
          <ShieldAlert class="w-3.5 h-3.5" aria-hidden="true" />
          <span>{{ criticalCount }} Critical</span>
        </span>
        <span
          v-if="warningCount > 0"
          class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200/80"
          data-testid="attention-warning-summary"
        >
          <AlertTriangle class="w-3.5 h-3.5" aria-hidden="true" />
          <span>{{ warningCount }} Warning</span>
        </span>
        <span
          v-if="signals.length === 0"
          class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200/80"
        >
          <CheckCircle2 class="w-3.5 h-3.5" aria-hidden="true" />
          <span>Clear</span>
        </span>
      </div>
    </template>

    <!-- Empty Healthy State -->
    <div
      v-if="visibleSignals.length === 0"
      class="flex items-center gap-3 p-4 rounded-xl bg-emerald-50/60 border border-emerald-200/70 text-emerald-800"
      data-testid="needs-attention-empty"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-600 shrink-0" aria-hidden="true" />
      <p class="text-xs font-medium">{{ emptyMessage }}</p>
    </div>

    <!-- Exception Queue -->
    <div v-else class="divide-y divide-slate-100" role="list" data-testid="needs-attention-list">
      <div
        v-for="signal in visibleSignals"
        :key="signal.signalCode"
        role="listitem"
        :data-testid="`attention-item-${signal.signalCode}`"
        class="py-3.5 first:pt-0 last:pb-0 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
      >
        <div class="space-y-1.5 min-w-0 flex-1">
          <div class="flex items-center gap-2 flex-wrap">
            <StatusPill
              :status="signal.severity"
              :label="isCritical(signal.severity) ? 'Critical' : 'Warning'"
              :variant="resolveKpiSemanticStyle(signal.severity).pillVariant"
              size="sm"
              data-testid="attention-severity-badge"
            />
            <span
              class="text-[11px] font-semibold uppercase tracking-wider px-2 py-0.5 rounded bg-slate-100 text-slate-600 border border-slate-200/80"
              data-testid="attention-domain-badge"
            >
              {{ signal.domain }}
            </span>
            <span
              class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-slate-900 text-white tabular-nums"
              data-testid="attention-affected-count"
            >
              {{ signal.affectedCount }} affected
            </span>
            <span
              v-if="signal.impactLabel"
              class="text-[11px] font-medium text-slate-500"
              data-testid="attention-impact-label"
            >
              {{ signal.impactLabel }}
            </span>
          </div>

          <h4 class="text-sm font-bold text-slate-900" data-testid="attention-title">
            {{ signal.title }}
          </h4>
          <p class="text-xs text-slate-600 leading-relaxed" data-testid="attention-description">
            {{ signal.description }}
          </p>
        </div>

        <div class="shrink-0">
          <RouterLink
            v-if="signal.drillDownRoute"
            :to="signal.drillDownRoute"
            :class="[
              'inline-flex items-center gap-1.5 px-3.5 py-2 rounded-lg text-xs font-semibold border transition-all',
              isCritical(signal.severity)
                ? 'bg-rose-600 text-white border-rose-600 hover:bg-rose-700 shadow-2xs'
                : 'bg-white text-slate-800 border-slate-300 hover:bg-slate-50 hover:border-slate-400',
            ]"
            data-testid="attention-action-link"
          >
            <span>{{ signal.actionLabel }}</span>
            <ArrowRight class="w-3.5 h-3.5" aria-hidden="true" />
          </RouterLink>
        </div>
      </div>
    </div>
  </ContentCard>
</template>
