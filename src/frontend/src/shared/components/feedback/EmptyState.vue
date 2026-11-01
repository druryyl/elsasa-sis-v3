<script setup lang="ts">
import type { Component } from 'vue';
import { Inbox } from 'lucide-vue-next';

export interface EmptyStateProps {
  title: string;
  description?: string | undefined;
  icon?: Component | object | undefined;
  bordered?: boolean | undefined;
  class?: string | undefined;
}

withDefaults(defineProps<EmptyStateProps>(), {
  description: undefined,
  icon: undefined,
  bordered: false,
  class: undefined,
});

defineSlots<{
  default?: () => unknown;
  icon?: () => unknown;
  title?: () => unknown;
  description?: () => unknown;
  actions?: () => unknown;
}>();
</script>

<template>
  <div
    :class="[
      'flex flex-col items-center justify-center text-center p-8 sm:p-12',
      bordered ? 'border-2 border-dashed border-slate-200 rounded-xl bg-slate-50/50' : '',
      $props.class,
    ]"
    data-testid="empty-state"
  >
    <!-- Icon Container -->
    <div
      class="w-12 h-12 sm:w-14 sm:h-14 rounded-full bg-slate-100 flex items-center justify-center text-slate-500 mb-4 shrink-0"
      data-testid="empty-state-icon-container"
    >
      <slot name="icon">
        <component
          :is="icon || Inbox"
          class="w-6 h-6 sm:w-7 sm:h-7 text-slate-400"
          aria-hidden="true"
        />
      </slot>
    </div>

    <!-- Title -->
    <h3 class="text-base sm:text-lg font-semibold text-slate-800" data-testid="empty-state-title">
      <slot name="title">
        {{ title }}
      </slot>
    </h3>

    <!-- Description -->
    <p
      v-if="description || $slots.description"
      class="mt-1 text-sm text-slate-500 max-w-sm"
      data-testid="empty-state-description"
    >
      <slot name="description">
        {{ description }}
      </slot>
    </p>

    <!-- Action Buttons Slot -->
    <div
      v-if="$slots.actions || $slots.default"
      class="mt-6 flex flex-wrap items-center justify-center gap-3"
      data-testid="empty-state-actions"
    >
      <slot name="actions">
        <slot />
      </slot>
    </div>
  </div>
</template>
