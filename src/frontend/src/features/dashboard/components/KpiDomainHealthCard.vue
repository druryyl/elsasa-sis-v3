<script setup lang="ts">
import { computed, type Component as VueComponent } from 'vue';
import { RouterLink } from 'vue-router';
import {
  AlertTriangle,
  ArrowRight,
  BookOpen,
  CalendarDays,
  CheckCircle2,
  GraduationCap,
  Lock,
  ShieldAlert,
  Sparkles,
  Users,
  Wallet,
} from 'lucide-vue-next';
import StatusPill from '@/shared/components/StatusPill.vue';
import type { DomainHealthSummaryDto } from '../types/dashboard.types';
import { resolveKpiSemanticStyle } from './kpiStatusStyles';

export interface KpiDomainHealthCardProps {
  summary: DomainHealthSummaryDto;
}

const props = defineProps<KpiDomainHealthCardProps>();

const semanticStyle = computed(() => resolveKpiSemanticStyle(props.summary.status));

const DOMAIN_ICON_MAP: Record<string, VueComponent> = {
  students: Users,
  academics: BookOpen,
  finance: Wallet,
  teaching: GraduationCap,
  operations: CalendarDays,
  development: Sparkles,
};

const DOMAIN_ICON_THEME_MAP: Record<string, string> = {
  students: 'bg-teal-50 text-teal-700 border-teal-200/80',
  academics: 'bg-indigo-50 text-indigo-700 border-indigo-200/80',
  finance: 'bg-emerald-50 text-emerald-700 border-emerald-200/80',
  teaching: 'bg-sky-50 text-sky-700 border-sky-200/80',
  operations: 'bg-amber-50 text-amber-700 border-amber-200/80',
  development: 'bg-slate-100 text-slate-500 border-slate-200',
};

const domainIcon = computed(() => {
  const key = (props.summary.domainKey || '').toLowerCase();
  return DOMAIN_ICON_MAP[key] ?? Sparkles;
});

const domainIconTheme = computed(() => {
  const key = (props.summary.domainKey || '').toLowerCase();
  return DOMAIN_ICON_THEME_MAP[key] ?? 'bg-slate-100 text-slate-600 border-slate-200';
});

const isPendingStage = computed(() => props.summary.status === 'pending_stage');
</script>

<template>
  <article
    :class="[
      'group relative bg-white border rounded-xl shadow-xs transition-all duration-200 overflow-hidden flex flex-col justify-between',
      isPendingStage
        ? 'border-dashed border-slate-300 bg-slate-50/50'
        : 'border-slate-200 hover:shadow-md hover:border-slate-300',
    ]"
    :data-testid="`domain-health-card-${summary.domainKey}`"
  >
    <!-- Top semantic accent stripe -->
    <div :class="['h-1 w-full bg-gradient-to-r', semanticStyle.accentBarClass]" />

    <div class="p-4 sm:p-5 flex-1 flex flex-col gap-4">
      <!-- Header: Icon + Name + Status Pill -->
      <div class="flex items-start justify-between gap-2">
        <div class="flex items-center gap-2.5 min-w-0">
          <div
            :class="[
              'w-10 h-10 rounded-xl border flex items-center justify-center shrink-0',
              domainIconTheme,
            ]"
          >
            <component :is="domainIcon" class="w-5 h-5" aria-hidden="true" />
          </div>
          <div class="min-w-0">
            <h3
              class="text-sm font-bold text-slate-900 truncate"
              data-testid="domain-health-name"
            >
              {{ summary.domainName }}
            </h3>
            <p class="text-[11px] font-medium text-slate-400 uppercase tracking-wider">
              {{ summary.domainKey }}
            </p>
          </div>
        </div>

        <StatusPill
          :status="summary.status"
          :label="summary.statusLabel || semanticStyle.statusLabel"
          :variant="semanticStyle.pillVariant"
          size="sm"
          data-testid="domain-health-status"
        />
      </div>

      <!-- Primary & Secondary Metrics -->
      <div class="grid grid-cols-2 gap-3 pt-1">
        <div class="bg-slate-50/80 rounded-lg p-2.5 border border-slate-200/60">
          <div class="text-[11px] font-medium text-slate-500 truncate">
            {{ summary.headlineLabel }}
          </div>
          <div
            class="text-base font-bold text-slate-900 tabular-nums mt-0.5 truncate"
            data-testid="domain-health-headline-value"
          >
            {{ summary.headlineValue }}
          </div>
        </div>

        <div class="bg-slate-50/80 rounded-lg p-2.5 border border-slate-200/60">
          <div class="text-[11px] font-medium text-slate-500 truncate">
            {{ summary.secondaryLabel }}
          </div>
          <div
            class="text-base font-bold text-slate-900 tabular-nums mt-0.5 truncate"
            data-testid="domain-health-secondary-value"
          >
            {{ summary.secondaryValue }}
          </div>
        </div>
      </div>

      <!-- Active Warning / Critical Counters -->
      <div class="flex items-center gap-2 flex-wrap text-xs">
        <template v-if="isPendingStage">
          <span class="inline-flex items-center gap-1.5 text-slate-500 font-medium">
            <Sparkles class="w-3.5 h-3.5 text-slate-400" aria-hidden="true" />
            <span>Holistic Student Development (Planned Stage 3)</span>
          </span>
        </template>
        <template v-else-if="summary.criticalCount > 0 || summary.warningCount > 0">
          <span
            v-if="summary.criticalCount > 0"
            class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full font-semibold bg-rose-50 text-rose-700 border border-rose-200/80"
            data-testid="domain-health-critical-count"
          >
            <ShieldAlert class="w-3.5 h-3.5" aria-hidden="true" />
            <span>{{ summary.criticalCount }} Critical</span>
          </span>
          <span
            v-if="summary.warningCount > 0"
            class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full font-semibold bg-amber-50 text-amber-700 border border-amber-200/80"
            data-testid="domain-health-warning-count"
          >
            <AlertTriangle class="w-3.5 h-3.5" aria-hidden="true" />
            <span>{{ summary.warningCount }} Warning</span>
          </span>
        </template>
        <template v-else>
          <span class="inline-flex items-center gap-1 text-emerald-700 font-medium">
            <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" aria-hidden="true" />
            <span>No active alerts</span>
          </span>
        </template>
      </div>
    </div>

    <!-- Footer Deep-Link or Restricted Badge -->
    <div class="px-4 sm:px-5 py-2.5 bg-slate-50/80 border-t border-slate-100">
      <div
        v-if="!summary.isAccessible"
        class="inline-flex items-center gap-1.5 text-xs font-medium text-slate-400"
        data-testid="domain-health-restricted"
      >
        <Lock class="w-3.5 h-3.5" aria-hidden="true" />
        <span>Domain view restricted for your role</span>
      </div>

      <RouterLink
        v-else-if="summary.dashboardRoute"
        :to="summary.dashboardRoute"
        class="inline-flex items-center justify-between w-full text-xs font-semibold text-teal-700 hover:text-teal-900 transition-colors"
        data-testid="domain-health-link"
      >
        <span>{{ isPendingStage ? 'View Stage 3 Roadmap' : `Open ${summary.domainName} Dashboard` }}</span>
        <ArrowRight
          class="w-3.5 h-3.5 text-teal-600 group-hover:translate-x-0.5 transition-transform"
          aria-hidden="true"
        />
      </RouterLink>
    </div>
  </article>
</template>
