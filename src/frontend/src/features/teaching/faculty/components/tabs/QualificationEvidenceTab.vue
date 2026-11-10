<script setup lang="ts">
import QualificationEvidenceTab, {
  type QualificationEvidenceTabProps,
} from '../QualificationEvidenceTab.vue';
import type {
  SubmitQualificationEvidenceResponse,
  VerifyQualificationEvidenceResponse,
  RejectQualificationEvidenceResponse,
} from '../../api/teachingTypes';

const props = defineProps<QualificationEvidenceTabProps>();

const emit = defineEmits<{
  (e: 'evidenceSubmitted', response: SubmitQualificationEvidenceResponse): void;
  (e: 'evidenceVerified', response: VerifyQualificationEvidenceResponse): void;
  (e: 'evidenceRejected', response: RejectQualificationEvidenceResponse): void;
  (e: 'refresh'): void;
}>();
</script>

<template>
  <QualificationEvidenceTab
    v-bind="props"
    @evidence-submitted="(r) => emit('evidenceSubmitted', r)"
    @evidence-verified="(r) => emit('evidenceVerified', r)"
    @evidence-rejected="(r) => emit('evidenceRejected', r)"
    @refresh="emit('refresh')"
  />
</template>
