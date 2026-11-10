<script setup lang="ts">
import ProfileAffiliationTab, {
  type ProfileAffiliationTabProps,
} from '../ProfileAffiliationTab.vue';
import type { TeacherProfileDto } from '../../api/teachingTypes';

const props = defineProps<ProfileAffiliationTabProps>();

const emit = defineEmits<{
  (e: 'updated', teacher: TeacherProfileDto): void;
}>();
</script>

<template>
  <ProfileAffiliationTab
    v-bind="props"
    @updated="(t) => emit('updated', t)"
  />
</template>
