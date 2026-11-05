<script setup lang="ts">
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import { X } from 'lucide-vue-next';
import ReportCardPrintPreview from '@/features/academics/records/components/ReportCardPrintPreview.vue';
import type { ReportCard, ReportCardSnapshot } from '@/features/academics/records/types';

interface Props {
  visible: boolean;
  reportCard?: ReportCard | null;
  snapshot?: ReportCardSnapshot | null;
}

defineProps<Props>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
}>();

function handleClose() {
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    dismissable-mask
    :closable="false"
    class="!w-full !max-w-5xl !rounded-2xl overflow-hidden"
    content-class="!p-0"
    data-testid="report-card-print-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center justify-between w-full px-6 py-4 border-b border-slate-200 bg-slate-50 no-print">
        <div>
          <h3 class="text-base font-bold text-slate-900 leading-tight">
            Buku Rapor Merdeka - Print Preview
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Official statutory academic report card and learning competencies summary.
          </p>
        </div>

        <Button
          type="button"
          rounded
          text
          severity="secondary"
          size="small"
          class="!p-2 text-slate-400 hover:text-slate-600"
          data-testid="btn-close-print-modal"
          @click="handleClose"
        >
          <X class="w-5 h-5" />
        </Button>
      </div>
    </template>

    <div class="p-6 bg-slate-100 max-h-[80vh] overflow-y-auto">
      <ReportCardPrintPreview
        :report-card="reportCard"
        :snapshot="snapshot"
        :show-print-button="true"
      />
    </div>
  </Dialog>
</template>
