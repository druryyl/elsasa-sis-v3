<script setup lang="ts">
import TeachingCapabilitiesTab, {
  type TeachingCapabilitiesTabProps,
} from '../TeachingCapabilitiesTab.vue';
import type { AddTeachingCapabilityResponse } from '../../api/teachingTypes';

const props = defineProps<TeachingCapabilitiesTabProps>();

const emit = defineEmits<{
  (e: 'capabilityAdded', response: AddTeachingCapabilityResponse): void;
  (e: 'capabilityRemoved', capabilityId: string): void;
  (e: 'refresh'): void;
}>();
</script>

<template>
  <TeachingCapabilitiesTab
    v-bind="props"
    @capability-added="(r) => emit('capabilityAdded', r)"
    @capability-removed="(id) => emit('capabilityRemoved', id)"
    @refresh="emit('refresh')"
  />
</template>
