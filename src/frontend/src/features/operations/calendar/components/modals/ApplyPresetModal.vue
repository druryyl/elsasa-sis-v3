<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import {
  Layers,
  CalendarCheck2,
  AlertCircle,
  Sparkles,
  Calendar,
  Clock,
  ShieldCheck,
  CheckCircle2,
  ArrowRight,
} from 'lucide-vue-next';
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
    class="max-w-xl w-full"
    data-testid="apply-preset-modal"
  >
    <!-- Custom Header -->
    <template #header>
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-xl bg-indigo-50 border border-indigo-100 flex items-center justify-center shrink-0 text-indigo-600 shadow-2xs"
        >
          <Layers class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-900 text-base leading-snug">
              Apply Institutional Preset
            </h3>
            <span
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700 border border-indigo-200"
            >
              <Sparkles class="w-3 h-3 text-indigo-600" />
              Standard Blueprint
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Initialize canonical academic periods aligned with national curriculum standards.
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1 text-sm text-slate-700">
      <!-- Target Academic Year Context Strip -->
      <div
        class="flex items-center justify-between p-3 bg-slate-50 border border-slate-200/90 rounded-lg text-xs"
      >
        <div class="flex items-center gap-2">
          <Calendar class="w-4 h-4 text-slate-500" />
          <span class="text-slate-600 font-medium">Target Academic Year:</span>
          <span
            class="font-mono font-bold text-slate-900 bg-white px-2 py-0.5 rounded border border-slate-200 shadow-2xs"
          >
            {{ academicYearCode || 'Selected Year' }}
          </span>
        </div>
        <span
          class="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200"
        >
          <CheckCircle2 class="w-3 h-3 text-emerald-600" />
          Target Ready
        </span>
      </div>

      <!-- Institutional Structure Blueprint Card -->
      <div
        class="p-4 bg-gradient-to-br from-indigo-50/70 to-slate-50/80 border border-indigo-100 rounded-xl space-y-3 shadow-2xs"
      >
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <div class="p-1 rounded bg-indigo-100/80 text-indigo-700">
              <Layers class="w-4 h-4" />
            </div>
            <h4 class="font-semibold text-indigo-950 text-xs uppercase tracking-wide">
              Standard Indonesian 2-Semester Structure
            </h4>
          </div>
          <span class="text-[11px] font-medium text-indigo-600 bg-indigo-100/60 px-2 py-0.5 rounded">
            Kemendikbud / Kemenag
          </span>
        </div>

        <p class="text-xs text-indigo-900/90 leading-relaxed">
          Automatically generates the standard two-semester operational periods for academic year
          <strong class="font-mono text-indigo-950">{{ academicYearCode || 'Selected Year' }}</strong>.
        </p>

        <!-- Semester Timeline Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-2.5 pt-1">
          <!-- Semester 1 Card -->
          <div
            class="bg-white border border-slate-200/90 rounded-lg p-3 space-y-2 hover:border-teal-300 transition-colors shadow-2xs"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-1.5">
                <span
                  class="font-mono text-[11px] font-bold px-1.5 py-0.5 bg-teal-50 text-teal-700 border border-teal-200 rounded"
                >
                  SEM-1
                </span>
                <span class="font-semibold text-xs text-slate-800">Semester 1 (Ganjil)</span>
              </div>
              <span class="text-[11px] text-slate-500 font-medium">~22 Weeks</span>
            </div>

            <div class="flex items-center gap-1.5 text-xs text-slate-600 font-mono">
              <CalendarCheck2 class="w-3.5 h-3.5 text-teal-600 shrink-0" />
              <span>July 15 – December 20</span>
            </div>

            <div class="pt-1.5 border-t border-slate-100 flex items-center gap-1.5 text-[11px] text-slate-500">
              <span class="w-1.5 h-1.5 rounded-full bg-teal-400"></span>
              <span>Includes PTS 1 & PAS Assessment windows</span>
            </div>
          </div>

          <!-- Semester 2 Card -->
          <div
            class="bg-white border border-slate-200/90 rounded-lg p-3 space-y-2 hover:border-teal-300 transition-colors shadow-2xs"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-1.5">
                <span
                  class="font-mono text-[11px] font-bold px-1.5 py-0.5 bg-teal-50 text-teal-700 border border-teal-200 rounded"
                >
                  SEM-2
                </span>
                <span class="font-semibold text-xs text-slate-800">Semester 2 (Genap)</span>
              </div>
              <span class="text-[11px] text-slate-500 font-medium">~23 Weeks</span>
            </div>

            <div class="flex items-center gap-1.5 text-xs text-slate-600 font-mono">
              <CalendarCheck2 class="w-3.5 h-3.5 text-teal-600 shrink-0" />
              <span>January 5 – June 25</span>
            </div>

            <div class="pt-1.5 border-t border-slate-100 flex items-center gap-1.5 text-[11px] text-slate-500">
              <span class="w-1.5 h-1.5 rounded-full bg-teal-400"></span>
              <span>Includes PTS 2, PAT Exams & Graduation</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Compliance & Workflow Guarantee Banner -->
      <div
        class="p-3 bg-amber-50/70 border border-amber-200/80 rounded-lg flex items-start gap-2.5 text-xs text-amber-900"
      >
        <AlertCircle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div class="space-y-0.5 leading-relaxed">
          <p class="font-semibold text-amber-950">Draft Scaffolding & Non-Destructive Operation</p>
          <p class="text-amber-800">
            Periods will be created with status <em class="font-semibold text-amber-950">draft</em>.
            You can add nested terms, admission windows, or refine dates afterwards before activation.
          </p>
        </div>
      </div>

      <!-- Modal Footer Actions -->
      <div class="flex items-center justify-between pt-3 border-t border-slate-100">
        <div class="flex items-center gap-1.5 text-xs text-slate-500">
          <ShieldCheck class="w-3.5 h-3.5 text-slate-400" />
          <span>Operations Authority Action</span>
        </div>

        <div class="flex items-center gap-2">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            class="text-xs text-slate-600 hover:text-slate-800"
            data-testid="modal-cancel-btn"
            @click="isDialogVisible = false"
          />
          <Button
            type="button"
            size="small"
            label="Apply Preset"
            icon="pi pi-check"
            class="bg-indigo-600 hover:bg-indigo-700 text-white font-medium text-xs px-3.5 py-1.5 shadow-xs"
            :loading="presetMutation.isPending.value"
            data-testid="modal-confirm-preset-btn"
            @click="handleApply"
          />
        </div>
      </div>
    </div>
  </Dialog>
</template>
