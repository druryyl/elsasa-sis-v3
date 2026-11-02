<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import { Layers, CalendarCheck2, AlertCircle } from 'lucide-vue-next';
import {
  useApplyPresetMutation,
  type ApplyPresetResultDto,
} from '../../composables/useAcademicYearsQuery';

const props = defineProps<{
  visible: boolean;
  academicYearId?: string | null | undefined;
  academicYearCode?: string | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'applied', result: ApplyPresetResultDto): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const presetMutation = useApplyPresetMutation({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('applied', data);
  },
});

function handleApply(): void {
  if (!props.academicYearId) return;
  presetMutation.mutate(props.academicYearId);
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Apply Institutional Preset"
    modal
    class="max-w-md w-full"
    data-testid="apply-preset-modal"
  >
    <div class="space-y-4 pt-2 text-sm text-slate-700">
      <div class="flex items-start gap-3 p-3 bg-indigo-50 border border-indigo-100 rounded-lg">
        <Layers class="w-5 h-5 text-indigo-600 shrink-0 mt-0.5" />
        <div class="space-y-1">
          <h4 class="font-semibold text-indigo-900 text-xs uppercase tracking-wide">
            Standard Indonesian 2-Semester Structure
          </h4>
          <p class="text-xs text-indigo-800 leading-relaxed">
            Automatically generates the standard two-semester operational periods for academic year
            <strong class="font-mono text-indigo-950">{{ academicYearCode || 'Selected Year' }}</strong>.
          </p>
        </div>
      </div>

      <div class="space-y-2 border border-slate-200 rounded-md p-3 bg-white">
        <span class="text-xs font-semibold text-slate-600 uppercase tracking-wider block">
          Preset Periods Created:
        </span>
        <ul class="space-y-2 text-xs text-slate-700">
          <li class="flex items-center justify-between border-b border-slate-100 pb-1.5">
            <div class="flex items-center gap-2">
              <CalendarCheck2 class="w-3.5 h-3.5 text-teal-600" />
              <span class="font-medium">Semester 1 (Ganjil)</span>
            </div>
            <span class="font-mono text-slate-500 text-[11px]">July 15 – December 20</span>
          </li>
          <li class="flex items-center justify-between pt-0.5">
            <div class="flex items-center gap-2">
              <CalendarCheck2 class="w-3.5 h-3.5 text-teal-600" />
              <span class="font-medium">Semester 2 (Genap)</span>
            </div>
            <span class="font-mono text-slate-500 text-[11px]">January 5 – June 25</span>
          </li>
        </ul>
      </div>

      <div class="flex items-center gap-2 text-xs text-slate-500">
        <AlertCircle class="w-4 h-4 text-amber-500 shrink-0" />
        <span>
          Periods will be created with status <em class="font-semibold text-slate-700">draft</em>.
          You can add nested terms, admission windows, or refine dates afterwards.
        </span>
      </div>

      <div class="flex justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          text
          size="small"
          label="Cancel"
          data-testid="modal-cancel-btn"
          @click="isDialogVisible = false"
        />
        <Button
          type="button"
          size="small"
          label="Apply Preset"
          class="bg-indigo-600 hover:bg-indigo-700 text-white"
          :loading="presetMutation.isPending.value"
          data-testid="modal-confirm-preset-btn"
          @click="handleApply"
        />
      </div>
    </div>
  </Dialog>
</template>
