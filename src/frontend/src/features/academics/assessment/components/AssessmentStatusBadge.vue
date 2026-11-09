<script setup lang="ts">
import { computed } from 'vue';
import { StatusPill, type StatusVariant } from '@/shared/components';
import type { AssessmentStatus } from '../types';

interface Props {
  status: AssessmentStatus | string;
  size?: 'sm' | 'md';
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
});

const config = computed<{ label: string; variant: StatusVariant }>(() => {
  switch (props.status?.toLowerCase()) {
    case 'planned':
      return { label: 'Planned', variant: 'sky' };
    case 'conducted':
      return { label: 'Conducted', variant: 'emerald' };
    case 'cancelled':
    case 'canceled':
      return { label: 'Cancelled', variant: 'rose' };
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
    data-testid="assessment-status-badge"
  />
</template>
