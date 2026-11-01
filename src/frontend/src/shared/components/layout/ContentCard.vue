<script setup lang="ts">
export interface ContentCardProps {
  title?: string | undefined;
  subtitle?: string | undefined;
  noPadding?: boolean | undefined;
  class?: string | undefined;
  bodyClass?: string | undefined;
  headerClass?: string | undefined;
  footerClass?: string | undefined;
}

const props = withDefaults(defineProps<ContentCardProps>(), {
  title: undefined,
  subtitle: undefined,
  noPadding: false,
  class: undefined,
  bodyClass: undefined,
  headerClass: undefined,
  footerClass: undefined,
});

defineSlots<{
  default?: () => unknown;
  header?: () => unknown;
  title?: () => unknown;
  'header-actions'?: () => unknown;
  footer?: () => unknown;
}>();
</script>

<template>
  <section
    :class="['bg-white rounded-lg border border-slate-200 shadow-xs overflow-hidden', props.class]"
    data-testid="content-card"
  >
    <!-- Card Header -->
    <header
      v-if="title || $slots.title || $slots.header || $slots['header-actions']"
      :class="[
        'px-6 py-4 border-b border-slate-200 flex items-center justify-between gap-4',
        headerClass,
      ]"
      data-testid="content-card-header"
    >
      <slot name="header">
        <div class="min-w-0 flex-1">
          <slot name="title">
            <h3
              class="text-base font-semibold text-slate-800 truncate"
              data-testid="content-card-title"
            >
              {{ title }}
            </h3>
          </slot>
          <p
            v-if="subtitle"
            class="text-xs text-slate-500 mt-0.5"
            data-testid="content-card-subtitle"
          >
            {{ subtitle }}
          </p>
        </div>

        <div
          v-if="$slots['header-actions']"
          class="flex items-center gap-2 shrink-0"
          data-testid="content-card-header-actions"
        >
          <slot name="header-actions" />
        </div>
      </slot>
    </header>

    <!-- Card Body -->
    <div :class="[noPadding ? '' : 'p-6', bodyClass]" data-testid="content-card-body">
      <slot />
    </div>

    <!-- Card Footer -->
    <footer
      v-if="$slots.footer"
      :class="[
        'px-6 py-3 bg-slate-50/75 border-t border-slate-200 flex items-center justify-end gap-2',
        footerClass,
      ]"
      data-testid="content-card-footer"
    >
      <slot name="footer" />
    </footer>
  </section>
</template>
