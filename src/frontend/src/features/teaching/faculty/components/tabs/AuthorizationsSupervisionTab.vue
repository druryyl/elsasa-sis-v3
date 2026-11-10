<script setup lang="ts">
import AuthorizationsSupervisionTab, {
  type AuthorizationsSupervisionTabProps,
} from '../AuthorizationsSupervisionTab.vue';
import type {
  GrantTeachingAuthorizationResponse,
  AddSupervisionRequirementResponse,
  ResolveSupervisionRequirementResponse,
  UpdateTeachingAuthorizationStatusResponse,
} from '../../api/teachingTypes';

const props = defineProps<AuthorizationsSupervisionTabProps>();

const emit = defineEmits<{
  (e: 'authorizationGranted', response: GrantTeachingAuthorizationResponse): void;
  (e: 'supervisionAdded', response: AddSupervisionRequirementResponse): void;
  (e: 'supervisionResolved', response: ResolveSupervisionRequirementResponse): void;
  (e: 'statusUpdated', response: UpdateTeachingAuthorizationStatusResponse): void;
  (e: 'refresh'): void;
}>();
</script>

<template>
  <AuthorizationsSupervisionTab
    v-bind="props"
    @authorization-granted="(r) => emit('authorizationGranted', r)"
    @supervision-added="(r) => emit('supervisionAdded', r)"
    @supervision-resolved="(r) => emit('supervisionResolved', r)"
    @status-updated="(r) => emit('statusUpdated', r)"
    @refresh="() => emit('refresh')"
  />
</template>
