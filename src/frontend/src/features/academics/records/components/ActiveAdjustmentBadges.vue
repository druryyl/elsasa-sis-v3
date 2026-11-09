<script setup lang="ts">
import { computed } from 'vue';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useActiveLearningPlan } from '@/features/academics/learning-plans/composables/useLearningPlans';
import type { AcademicPlanAdjustment } from '@/features/academics/learning-plans/types';

const props = defineProps<{
  studentId?: string | null;
  academicPeriodId?: string | null;
  adjustments?: AcademicPlanAdjustment[] | null;
}>();

const { data: activePlan } = useActiveLearningPlan(
  () => props.studentId,
  () => props.academicPeriodId,
  {
    enabled: computed(() => Boolean(props.studentId && !props.adjustments)),
  },
);

const activeAdjustments = computed<AcademicPlanAdjustment[]>(() => {
  if (props.adjustments && props.adjustments.length > 0) {
    return props.adjustments;
  }
  return activePlan.value?.adjustments ?? [];
});

function getTooltip(adj: AcademicPlanAdjustment): string {
  const parts: string[] = [];
  if (adj.rationale) {
    parts.push(`Rationale: ${adj.rationale}`);
  }
  const authority = adj.approvedBy || 'Pending Authority Approval';
  const dateStr =
    adj.approvedDate && adj.approvedDate !== '1900-01-01'
      ? ` (${formatDisplayDate(adj.approvedDate)})`
      : '';
  parts.push(`Approving Authority: ${authority}${dateStr}`);
  return parts.join(' | ');
}

function getBadgeLabel(adj: AcademicPlanAdjustment): string {
  switch (adj.adjustmentType) {
    case 'accommodation':
      return `⚡ Accommodation: ${adj.description}`;
    case 'substitution':
      return `🔄 Substitution: ${adj.description}`;
    case 'exemption':
      return `🛡️ Exemption: ${adj.description}`;
    default:
      return `${adj.adjustmentType}: ${adj.description}`;
  }
}

function getBadgeClasses(adj: AcademicPlanAdjustment): string {
  switch (adj.adjustmentType) {
    case 'accommodation':
      return 'bg-amber-50 text-amber-800 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800';
    case 'substitution':
      return 'bg-purple-50 text-purple-800 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800';
    case 'exemption':
      return 'bg-sky-50 text-sky-800 border-sky-200 dark:bg-sky-950/40 dark:text-sky-300 dark:border-sky-800';
    default:
      return 'bg-slate-50 text-slate-800 border-slate-200 dark:bg-surface-800 dark:text-surface-200 dark:border-surface-700';
  }
}
</script>

<template>
  <div
    v-if="activeAdjustments.length > 0"
    class="flex flex-wrap items-center gap-1.5"
    data-testid="student-active-adjustments"
  >
    <span
      v-for="adj in activeAdjustments"
      :key="adj.id"
      v-tooltip.top="getTooltip(adj)"
      :title="getTooltip(adj)"
      :data-testid="`badge-adjustment-${adj.id}`"
      :data-adjustment-type="adj.adjustmentType"
      class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-semibold border cursor-help transition-colors select-none"
      :class="getBadgeClasses(adj)"
    >
      <span>{{ getBadgeLabel(adj) }}</span>
    </span>
  </div>
</template>
