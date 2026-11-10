<script setup lang="ts">
import CorrectionsAuditTab, {
  type CorrectionsAuditTabProps,
} from '../CorrectionsAuditTab.vue';
import type { TeachingRecordCorrectionDto } from '../../api/teachingTypes';

const props = defineProps<CorrectionsAuditTabProps>();

const emit = defineEmits<{
  (e: 'correctionApproved', correction: TeachingRecordCorrectionDto): void;
  (e: 'refresh'): void;
}>();
</script>

<template>
  <CorrectionsAuditTab
    v-bind="props"
    @correction-approved="(c) => emit('correctionApproved', c)"
    @refresh="() => emit('refresh')"
  />
</template>
