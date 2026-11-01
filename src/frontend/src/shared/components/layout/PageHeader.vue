<script setup lang="ts">
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { ChevronRight } from 'lucide-vue-next';
import { RouterLink } from 'vue-router';

export interface PageHeaderProps {
  title: string;
  subtitle?: string | undefined;
  breadcrumbs?: BreadcrumbItem[] | undefined;
  class?: string | undefined;
}

withDefaults(defineProps<PageHeaderProps>(), {
  subtitle: undefined,
  breadcrumbs: () => [],
  class: undefined,
});

defineSlots<{
  default?: () => unknown;
  breadcrumbs?: () => unknown;
  title?: () => unknown;
  'title-extra'?: () => unknown;
  subtitle?: () => unknown;
  actions?: () => unknown;
}>();
</script>

<template>
  <div :class="['mb-6 pb-4 border-b border-slate-200', $props.class]" data-testid="page-header">
    <!-- Breadcrumb Navigation -->
    <nav
      v-if="(breadcrumbs && breadcrumbs.length > 0) || $slots.breadcrumbs"
      aria-label="Breadcrumb"
      class="mb-2.5"
      data-testid="page-header-breadcrumbs"
    >
      <slot name="breadcrumbs">
        <ol class="flex items-center flex-wrap gap-1.5 text-xs text-slate-500">
          <li
            v-for="(crumb, idx) in breadcrumbs"
            :key="idx"
            class="inline-flex items-center gap-1.5"
          >
            <ChevronRight
              v-if="idx > 0"
              class="w-3.5 h-3.5 text-slate-400 shrink-0"
              aria-hidden="true"
            />
            <RouterLink
              v-if="crumb.to && !crumb.disabled && !crumb.current"
              :to="crumb.to"
              class="text-slate-600 hover:text-elsasa-teal-600 font-medium transition-colors"
            >
              {{ crumb.label }}
            </RouterLink>
            <span
              v-else
              :class="[crumb.current ? 'font-semibold text-slate-800' : 'text-slate-500']"
              :aria-current="crumb.current ? 'page' : undefined"
            >
              {{ crumb.label }}
            </span>
          </li>
        </ol>
      </slot>
    </nav>

    <!-- Header Row -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div class="min-w-0 flex-1">
        <div class="flex items-center gap-3">
          <slot name="title">
            <h1
              class="text-2xl font-bold tracking-tight text-elsasa-navy-900 truncate"
              data-testid="page-header-title"
            >
              {{ title }}
            </h1>
          </slot>
          <slot name="title-extra" />
        </div>
        <slot name="subtitle">
          <p v-if="subtitle" class="mt-1 text-sm text-slate-500" data-testid="page-header-subtitle">
            {{ subtitle }}
          </p>
        </slot>
      </div>

      <!-- Action Buttons Slot -->
      <div
        v-if="$slots.actions"
        class="flex items-center gap-2.5 flex-wrap sm:flex-nowrap shrink-0"
        data-testid="page-header-actions"
      >
        <slot name="actions" />
      </div>
    </div>

    <!-- Additional Content -->
    <div v-if="$slots.default" class="mt-4">
      <slot />
    </div>
  </div>
</template>
