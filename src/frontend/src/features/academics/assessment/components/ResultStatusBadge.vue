<script setup lang="ts">
import { computed } from 'vue';
import { StatusPill, type StatusVariant } from '@/shared/components';
import type { ResultStatus } from '../types';

interface Props {
  status: ResultStatus | string;
  size?: 'sm' | 'md';
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
});

const config = computed<{ label: string; variant: StatusVariant }>(() => {
  switch (props.status?.toLowerCase()) {
    case 'provisional':
      return { label: 'Provisional', variant: 'amber' };
    case 'approved':
      return { label: 'Approved', variant: 'sky' };
    case 'published':
      return { label: 'Published', variant: 'emerald' };
    default:
      return { label: props.status || 'Unknown', variant: 'slate' };
  }
});
</script>

<template>
  <StatusPill
    :status="status"
    :label="config.label"
    :variant="config.variant"
    :size="size"
    data-testid="result-status-badge"
  />
</template>
