<script setup lang="ts">
import { computed } from 'vue';

export type StatusVariant = 'emerald' | 'amber' | 'rose' | 'slate' | 'sky';
export type StatusPillSize = 'sm' | 'md';

export interface StatusPillProps {
  status: string;
  label?: string | undefined;
  variant?: StatusVariant | undefined;
  size?: StatusPillSize | undefined;
  dot?: boolean | undefined;
  class?: string | undefined;
}

const props = withDefaults(defineProps<StatusPillProps>(), {
  label: undefined,
  variant: undefined,
  size: 'md',
  dot: true,
  class: undefined,
});

defineSlots<{
  default?: () => unknown;
}>();

const STATUS_VARIANT_MAP: Record<string, StatusVariant> = {
  // Emerald
  active: 'emerald',
  accepted: 'emerald',
  paid: 'emerald',
  enrolled: 'emerald',
  approved: 'emerald',
  completed: 'emerald',
  success: 'emerald',
  decided: 'emerald',
  verified: 'emerald',

  // Amber
  pending: 'amber',
  underreview: 'amber',
  under_review: 'amber',
  'under-review': 'amber',
  installment: 'amber',
  onleave: 'amber',
  on_leave: 'amber',
  'on-leave': 'amber',
  warning: 'amber',
  waitlisted: 'amber',
  deferred: 'amber',

  // Rose
  rejected: 'rose',
  overdue: 'rose',
  withdrawn: 'rose',
  expelled: 'rose',
  cancelled: 'rose',
  canceled: 'rose',
  danger: 'rose',
  failed: 'rose',

  // Slate
  closed: 'slate',
  archived: 'slate',
  inactive: 'slate',
  draft: 'slate',
  slate: 'slate',
  not_submitted: 'slate',
  'not-submitted': 'slate',

  // Sky
  scheduled: 'sky',
  submitted: 'sky',
  registered: 'sky',
  info: 'sky',
};

const resolvedVariant = computed<StatusVariant>(() => {
  if (props.variant) return props.variant;
  const normalized = (props.status || '').toLowerCase().trim();
  return STATUS_VARIANT_MAP[normalized] ?? 'slate';
});

const variantClasses = {
  emerald: 'bg-emerald-50 text-emerald-700 border-emerald-200/80',
  amber: 'bg-amber-50 text-amber-700 border-amber-200/80',
  rose: 'bg-rose-50 text-rose-700 border-rose-200/80',
  slate: 'bg-slate-100 text-slate-700 border-slate-200',
  sky: 'bg-sky-50 text-sky-700 border-sky-200/80',
} as const;

const dotClasses = {
  emerald: 'bg-emerald-500',
  amber: 'bg-amber-500',
  rose: 'bg-rose-500',
  slate: 'bg-slate-400',
  sky: 'bg-sky-500',
} as const;

const sizeClasses: Record<StatusPillSize, string> = {
  sm: 'text-xs px-2 py-0.5 gap-1',
  md: 'text-xs px-2.5 py-1 gap-1.5',
};

const displayLabel = computed(() => {
  if (props.label) return props.label;
  if (!props.status) return '';
  return props.status
    .replace(/[-_]+/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .split(' ')
    .filter(Boolean)
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
});
</script>

<template>
  <span
    :class="[
      'inline-flex items-center rounded-full border font-medium transition-colors select-none',
      sizeClasses[props.size ?? 'md'],
      variantClasses[resolvedVariant],
      props.class,
    ]"
    data-testid="status-pill"
    :data-variant="resolvedVariant"
    :data-status="status"
  >
    <span
      v-if="dot"
      :class="['h-1.5 w-1.5 rounded-full shrink-0', dotClasses[resolvedVariant]]"
      aria-hidden="true"
      data-testid="status-pill-dot"
    />
    <slot>{{ displayLabel }}</slot>
  </span>
</template>
