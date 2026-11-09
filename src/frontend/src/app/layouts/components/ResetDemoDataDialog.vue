<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import { AlertTriangle } from 'lucide-vue-next';
import { useResetDemoData } from '@/features/operations/demo-data/composables/useDemoData';

const props = defineProps<{
  visible: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const resetMutation = useResetDemoData({
  onSuccess: () => {
    emit('success');
    isDialogVisible.value = false;
  },
});

const isSubmitting = computed(() => resetMutation.isPending.value);
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    header="Reset All Demo Data"
    :style="{ width: '500px' }"
    :closable="!isSubmitting"
  >
    <div class="space-y-4">
      <div class="p-4 rounded-lg bg-rose-50 border border-rose-200 flex items-start gap-3">
        <AlertTriangle class="w-6 h-6 text-rose-600 shrink-0 mt-0.5" />
        <div>
          <h4 class="font-semibold text-rose-900">Warning: Destructive Action</h4>
          <p class="text-sm text-rose-700 mt-1">
            This will permanently purge all current demo data domains and re-seed the system to the initial state. 
            All current progress, changes, and manual entries will be lost. This action cannot be undone.
          </p>
        </div>
      </div>

      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="text"
          :disabled="isSubmitting"
          @click="isDialogVisible = false"
        />
        <Button
          type="button"
          label="Reset All Demo Data"
          severity="danger"
          icon="pi pi-refresh"
          :loading="isSubmitting"
          @click="resetMutation.mutate()"
        />
      </div>
    </div>
  </Dialog>
</template>
