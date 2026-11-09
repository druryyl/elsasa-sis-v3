<script setup lang="ts">
import { computed, inject, ref } from 'vue';
import Tag from 'primevue/tag';
import {
  CheckCircle2,
  AlertTriangle,
  AlertCircle,
  FileCheck,
  UserCheck,
  ShieldAlert,
  ArrowRight,
  Info,
  HelpCircle,
} from 'lucide-vue-next';
import { useStudentProgressionRecommendation } from '../composables/useProgressionRecommendation';
import type { ProgressionRecommendationDto, PromotionRecommendationStatus } from '../types';

const props = withDefaults(
  defineProps<{
    studentId?: string;
    recommendation?: ProgressionRecommendationDto | null;
    compact?: boolean;
    showDetails?: boolean;
  }>(),
  {
    studentId: undefined,
    recommendation: undefined,
    compact: false,
    showDetails: true,
  },
);

const hasQueryClient = inject('VUE_QUERY_CLIENT', null) !== null;
const { latestRecommendation: queriedRecommendation, isLoading } =
  hasQueryClient && props.recommendation === undefined
    ? useStudentProgressionRecommendation(computed(() => props.studentId))
    : { latestRecommendation: ref<ProgressionRecommendationDto | null>(null), isLoading: ref(false) };

const activeRecommendation = computed<ProgressionRecommendationDto | null>(() => {
  if (props.recommendation !== undefined) {
    return props.recommendation;
  }
  return queriedRecommendation.value;
});

interface BadgeConfig {
  label: string;
  severity: 'success' | 'warn' | 'danger' | 'secondary';
  bgClass: string;
  borderClass: string;
  textClass: string;
  icon: any;
  unblockMessage: string;
}

const badgeConfig = computed<BadgeConfig>(() => {
  const rec = activeRecommendation.value;
  if (!rec) {
    return {
      label: 'No Recommendation Recorded',
      severity: 'secondary',
      bgClass: 'bg-surface-100 dark:bg-surface-800',
      borderClass: 'border-surface-200 dark:border-surface-700',
      textClass: 'text-surface-600 dark:text-surface-400',
      icon: HelpCircle,
      unblockMessage: 'No academic progression recommendation has been certified for this student.',
    };
  }

  switch (rec.recommendation) {
    case 'promotion_eligible':
      return {
        label: 'Promotion Eligible',
        severity: 'success',
        bgClass: 'bg-emerald-50 dark:bg-emerald-950/40',
        borderClass: 'border-emerald-200 dark:border-emerald-800',
        textClass: 'text-emerald-800 dark:text-emerald-300',
        icon: CheckCircle2,
        unblockMessage: `Authoritative academic recommendation unblocks administrative promotion to ${rec.recommendedToLevel || 'next grade level'} (STD-WF-08).`,
      };
    case 'conditional_promotion':
      return {
        label: 'Conditional Promotion',
        severity: 'warn',
        bgClass: 'bg-amber-50 dark:bg-amber-950/40',
        borderClass: 'border-amber-200 dark:border-amber-800',
        textClass: 'text-amber-800 dark:text-amber-300',
        icon: AlertTriangle,
        unblockMessage: `Authoritative recommendation unblocks conditional promotion to ${rec.recommendedToLevel || 'next grade level'} subject to academic stipulations (STD-WF-08).`,
      };
    case 'retention_recommended':
      return {
        label: 'Retention Recommended',
        severity: 'danger',
        bgClass: 'bg-rose-50 dark:bg-rose-950/40',
        borderClass: 'border-rose-200 dark:border-rose-800',
        textClass: 'text-rose-800 dark:text-rose-300',
        icon: AlertCircle,
        unblockMessage: `Retention in current level (${rec.fromLevel}) recommended by Academic Authority. Grade advancement is blocked unless authorized exception is granted.`,
      };
    default:
      return {
        label: rec.recommendation,
        severity: 'secondary',
        bgClass: 'bg-surface-100 dark:bg-surface-800',
        borderClass: 'border-surface-200 dark:border-surface-700',
        textClass: 'text-surface-600 dark:text-surface-400',
        icon: Info,
        unblockMessage: 'Academic progression recommendation is under review.',
      };
  }
});
</script>

<template>
  <div data-testid="progression-recommendation-badge" class="inline-block w-full">
    <!-- Loading state -->
    <div
      v-if="isLoading && !activeRecommendation"
      class="inline-flex items-center gap-2 text-xs text-surface-400 animate-pulse"
      data-testid="progression-badge-loading"
    >
      <div class="w-3.5 h-3.5 rounded-full border-2 border-surface-300 border-t-primary-500 animate-spin" />
      <span>Loading academic recommendation...</span>
    </div>

    <!-- Compact mode: Badge only (useful for tables and lists) -->
    <div
      v-else-if="compact"
      class="inline-flex flex-wrap items-center gap-1.5"
      data-testid="progression-badge-compact"
    >
      <Tag
        :severity="badgeConfig.severity"
        :value="badgeConfig.label"
        class="!text-xs !font-semibold !px-2.5 !py-1"
        :data-testid="`badge-${activeRecommendation?.recommendation || 'none'}`"
      >
        <template #icon>
          <component :is="badgeConfig.icon" class="w-3.5 h-3.5 mr-1" />
        </template>
      </Tag>

      <!-- Decree reference tag if exception or committee decree exists -->
      <span
        v-if="activeRecommendation?.committeeReference"
        class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-purple-50 dark:bg-purple-950/60 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800"
        :title="`Committee Decree: ${activeRecommendation.committeeReference}`"
        data-testid="decree-reference-tag"
      >
        <FileCheck class="w-3 h-3" />
        <span>{{ activeRecommendation.committeeReference }}</span>
      </span>
    </div>

    <!-- Full Detailed Card: Used in Workbenches, Placements, and Drawers -->
    <div
      v-else
      class="p-4 rounded-xl border text-xs space-y-3"
      :class="[badgeConfig.bgClass, badgeConfig.borderClass]"
      data-testid="progression-recommendation-card"
    >
      <!-- Top Bar: Badge, Recommendation Levels, Decree Reference -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
        <div class="flex items-center gap-2 flex-wrap">
          <Tag
            :severity="badgeConfig.severity"
            :value="badgeConfig.label"
            class="!text-xs !font-bold !px-3 !py-1 shadow-2xs"
            :data-testid="`badge-${activeRecommendation?.recommendation || 'none'}`"
          >
            <template #icon>
              <component :is="badgeConfig.icon" class="w-4 h-4 mr-1.5" />
            </template>
          </Tag>

          <!-- Level Transition Indicator -->
          <span
            v-if="activeRecommendation"
            class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-semibold bg-white/80 dark:bg-surface-800/80 border border-surface-200 dark:border-surface-700 text-surface-800 dark:text-surface-200"
            data-testid="progression-levels"
          >
            <span>{{ activeRecommendation.fromLevel }}</span>
            <ArrowRight class="w-3.5 h-3.5 text-surface-400" />
            <span :class="activeRecommendation.recommendedToLevel ? 'text-primary-700 dark:text-primary-300 font-bold' : 'text-rose-600 font-bold'">
              {{ activeRecommendation.recommendedToLevel || 'Retain' }}
            </span>
          </span>
        </div>

        <!-- Governing Academic Authority & Decree Reference -->
        <div class="flex items-center gap-2 flex-wrap text-[11px]">
          <span
            v-if="activeRecommendation?.committeeReference"
            class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full font-semibold bg-purple-100 dark:bg-purple-900/60 text-purple-800 dark:text-purple-200 border border-purple-200 dark:border-purple-700"
            data-testid="decree-reference-badge"
          >
            <FileCheck class="w-3.5 h-3.5" />
            <span>SK Dewan Guru: {{ activeRecommendation.committeeReference }}</span>
          </span>

          <span
            v-if="activeRecommendation?.authorizedBy"
            class="inline-flex items-center gap-1 text-surface-600 dark:text-surface-300"
            data-testid="authorizing-authority"
          >
            <UserCheck class="w-3.5 h-3.5 text-teal-600" />
            <span>Authorized by <strong>{{ activeRecommendation.authorizedBy }}</strong></span>
          </span>
        </div>
      </div>

      <!-- Exception Alert Banner -->
      <div
        v-if="activeRecommendation?.isException"
        class="p-2.5 rounded-lg bg-purple-50 dark:bg-purple-950/40 border border-purple-200 dark:border-purple-800 text-purple-900 dark:text-purple-200 flex items-start gap-2"
        data-testid="exception-override-banner"
      >
        <ShieldAlert class="w-4 h-4 text-purple-600 shrink-0 mt-0.5" />
        <div>
          <span class="font-bold block">Academic Authority Exception Override</span>
          <span class="text-xs">{{ activeRecommendation.exceptionReason || 'Authorized exception certified by committee.' }}</span>
        </div>
      </div>

      <!-- Conditional Promotion Stipulations Callout -->
      <div
        v-if="activeRecommendation?.recommendation === 'conditional_promotion' && activeRecommendation.conditions"
        class="p-2.5 rounded-lg bg-amber-100/70 dark:bg-amber-950/60 border border-amber-300 dark:border-amber-700 text-amber-900 dark:text-amber-200 flex items-start gap-2"
        data-testid="conditional-stipulations-callout"
      >
        <AlertTriangle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div class="space-y-0.5">
          <span class="font-bold block">Mandatory Promotion Conditions / Remedial Requirements:</span>
          <p class="text-xs italic">{{ activeRecommendation.conditions }}</p>
        </div>
      </div>

      <!-- Supporting Evidence Summary -->
      <div
        v-if="showDetails && activeRecommendation?.supportingEvidence"
        class="text-xs text-surface-600 dark:text-surface-300 pt-1 border-t border-surface-200/50 dark:border-surface-700/50"
        data-testid="supporting-evidence"
      >
        <span class="font-medium text-surface-700 dark:text-surface-200">Evidence Summary: </span>
        <span>{{ activeRecommendation.supportingEvidence }}</span>
      </div>

      <!-- Operational Unblocking State Message (STD-WF-08) -->
      <div
        class="flex items-center gap-2 pt-1 text-[11px]"
        :class="badgeConfig.textClass"
        data-testid="unblock-message"
      >
        <component :is="badgeConfig.icon" class="w-3.5 h-3.5 shrink-0" />
        <span class="font-medium">{{ badgeConfig.unblockMessage }}</span>
      </div>
    </div>
  </div>
</template>
