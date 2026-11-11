<script setup lang="ts">
import { BookOpen, Sparkles } from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
</script>

<template>
  <div class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs" data-testid="active-assignments-stub">
    <EmptyState
      title="Assignments & Homerooms (Tab 3)"
      description="Active teaching assignments, co-teaching groupings, and decoupled homeroom advisories worklist scheduled for delivery in Slice P8-S25."
    >
      <template #icon>
        <div class="w-14 h-14 rounded-full bg-primary-50 dark:bg-primary-950 flex items-center justify-center mx-auto mb-4 text-primary-600 dark:text-primary-400">
          <BookOpen class="w-7 h-7" />
        </div>
      </template>
    </EmptyState>
  </div>
</template>
