<script setup lang="ts">
import { CheckCircle2, Award } from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
</script>

<template>
  <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs" data-testid="conclusions-stub">
    <EmptyState
      title="Assignment Conclusions & Capacity Release (Tab 5)"
      description="Formal assignment completion, academic deliverables verification, and atomic capacity release scheduled for delivery in Slice P8-S25."
    >
      <template #icon>
        <div class="w-14 h-14 rounded-full bg-emerald-50 dark:bg-emerald-950 flex items-center justify-center mx-auto mb-4 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-7 h-7" />
        </div>
      </template>
    </EmptyState>
  </div>
</template>
