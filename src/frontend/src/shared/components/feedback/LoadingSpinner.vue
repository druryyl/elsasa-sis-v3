<script setup lang="ts">
import { computed } from 'vue';

export type SpinnerSize = 'sm' | 'md' | 'lg';
export type SpinnerColor = 'teal' | 'primary' | 'white' | 'slate';

export interface LoadingSpinnerProps {
  size?: SpinnerSize | undefined;
  label?: string | undefined;
  text?: string | undefined;
  color?: SpinnerColor | undefined;
  center?: boolean | undefined;
  inline?: boolean | undefined;
  class?: string | undefined;
}

const props = withDefaults(defineProps<LoadingSpinnerProps>(), {
  size: 'md',
  label: 'Loading...',
  text: undefined,
  color: 'teal',
  center: false,
  inline: false,
  class: undefined,
});

const sizeClasses: Record<SpinnerSize, string> = {
  sm: 'h-4 w-4',
  md: 'h-6 w-6',
  lg: 'h-10 w-10',
};

const textSizeClasses: Record<SpinnerSize, string> = {
  sm: 'text-xs',
  md: 'text-sm',
  lg: 'text-base',
};

const colorClasses: Record<SpinnerColor, string> = {
  teal: 'text-elsasa-teal-600',
  primary: 'text-elsasa-navy-900',
  white: 'text-white',
  slate: 'text-slate-500',
};

const spinnerSizeClass = computed(() => sizeClasses[props.size ?? 'md']);
const textSizeClass = computed(() => textSizeClasses[props.size ?? 'md']);
const colorClass = computed(() => colorClasses[props.color ?? 'teal']);

const containerClasses = computed(() => {
  if (props.center) {
    return 'flex flex-col items-center justify-center p-4 gap-2.5 w-full';
  }
  if (props.inline) {
    return 'inline-flex items-center gap-2';
  }
  return 'flex items-center gap-2';
});
</script>

<template>
  <div
    :class="[containerClasses, props.class]"
    role="status"
    aria-live="polite"
    data-testid="loading-spinner"
  >
    <svg
      :class="['animate-spin shrink-0', spinnerSizeClass, colorClass]"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
      <path
        class="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
      />
    </svg>
    <span class="sr-only">{{ label }}</span>
    <span
      v-if="text"
      :class="['font-medium text-slate-600 select-none', textSizeClass]"
      data-testid="loading-spinner-text"
    >
      {{ text }}
    </span>
  </div>
</template>
