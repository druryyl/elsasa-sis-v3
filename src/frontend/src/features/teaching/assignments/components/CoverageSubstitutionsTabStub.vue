<script setup lang="ts">
import { ShieldAlert, ArrowLeftRight } from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
</script>

<template>
  <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs" data-testid="coverage-substitutions-stub">
    <EmptyState
      title="Coverage & Substitutions (Tab 4)"
      description="Pre-Authorized Pool Delegation (Resolution 3.8), emergency substitutions, and permanent reassignments with handover verification scheduled for delivery in Slice P8-S25."
    >
      <template #icon>
        <div class="w-14 h-14 rounded-full bg-amber-50 dark:bg-amber-950 flex items-center justify-center mx-auto mb-4 text-amber-600 dark:text-amber-400">
          <ArrowLeftRight class="w-7 h-7" />
        </div>
      </template>
    </EmptyState>
  </div>
</template>
