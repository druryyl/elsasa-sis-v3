<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import {
  GraduationCap,
  Plus,
  Pencil,
  Trash2,
  Calendar,
  Building2,
  Sparkles,
} from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { ADMISSION_PERMISSIONS } from '../../constants/permissions';
import type { AssessmentEvidenceDto } from '../../types/admission.types';

const props = defineProps<{
  applicationId: string;
  assessmentEvidence: AssessmentEvidenceDto[];
  isReadOnly?: boolean;
}>();

const emit = defineEmits<{
  (e: 'add'): void;
  (e: 'edit', evidence: AssessmentEvidenceDto): void;
  (e: 'delete', evidenceId: string): void;
}>();

const authStore = useAuthStore();
const canReview = computed(() =>
  authStore.hasPermission(ADMISSION_PERMISSIONS.REVIEW) && !props.isReadOnly,
);

function handleAddScore() {
  emit('add');
}

function handleEditScore(ev: AssessmentEvidenceDto) {
  emit('edit', ev);
}

function handleDeleteScore(evId: string) {
  emit('delete', evId);
}
</script>

<template>
  <div class="space-y-4" data-testid="assessment-evidence-tab">
    <div
      class="border border-slate-200 rounded-lg p-4 space-y-3 bg-white shadow-2xs"
      data-testid="assessment-evidence-card"
    >
      <div class="flex items-center justify-between border-b border-slate-100 pb-2.5">
        <div class="flex items-center gap-2">
          <GraduationCap class="w-4 h-4 text-teal-600" />
          <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
            Entrance Assessment Diagnostic Evidence ({{ assessmentEvidence.length }})
          </h3>
        </div>

        <Button
          size="small"
          severity="primary"
          :disabled="!canReview"
          class="!text-xs !py-1 !px-2.5 cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700"
          data-testid="add-score-button"
          @click="handleAddScore"
        >
          <Plus class="w-3.5 h-3.5 mr-1" />
          <span>Add Score</span>
        </Button>
      </div>

      <!-- Score Cards List -->
      <div v-if="assessmentEvidence && assessmentEvidence.length > 0" class="space-y-3">
        <div
          v-for="ev in assessmentEvidence"
          :key="ev.id"
          class="p-3.5 rounded-lg border border-slate-200/90 bg-slate-50 text-xs space-y-2 hover:border-teal-200 transition-colors"
          :data-testid="`assessment-evidence-item-${ev.id}`"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="font-bold text-slate-900 text-sm" data-testid="evidence-type">
                  {{ ev.assessmentType }}
                </span>
                <span
                  class="font-mono font-bold text-teal-800 bg-teal-100 px-2.5 py-0.5 rounded border border-teal-200 text-xs"
                  data-testid="evidence-score"
                >
                  {{ ev.score || 'Recorded' }}
                </span>
              </div>
              <div class="flex flex-wrap items-center gap-3 text-[11px] text-slate-500">
                <span class="inline-flex items-center gap-1">
                  <Building2 class="w-3 h-3 text-slate-400" />
                  Source: <strong class="text-slate-700 font-medium">{{ ev.source }}</strong>
                </span>
                <span v-if="ev.assessmentDate" class="inline-flex items-center gap-1">
                  <Calendar class="w-3 h-3 text-slate-400" />
                  Administered: {{ formatDisplayDate(ev.assessmentDate) }}
                </span>
              </div>
            </div>

            <!-- Action buttons -->
            <div v-if="canReview" class="flex items-center gap-1 shrink-0">
              <button
                type="button"
                class="p-1 text-slate-400 hover:text-teal-700 rounded hover:bg-slate-200/60 transition cursor-pointer"
                title="Edit diagnostic score"
                data-testid="edit-score-button"
                @click="handleEditScore(ev)"
              >
                <Pencil class="w-3.5 h-3.5" />
              </button>
              <button
                type="button"
                class="p-1 text-slate-400 hover:text-rose-600 rounded hover:bg-rose-50 transition cursor-pointer"
                title="Remove diagnostic score"
                data-testid="delete-score-button"
                @click="handleDeleteScore(ev.id)"
              >
                <Trash2 class="w-3.5 h-3.5" />
              </button>
            </div>
          </div>

          <div
            v-if="ev.notes"
            class="text-xs text-slate-700 bg-white p-2.5 rounded border border-slate-200/70 italic"
            data-testid="evidence-notes"
          >
            "{{ ev.notes }}"
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div
        v-else
        class="text-center py-8 px-4 border border-dashed border-slate-200 rounded-lg text-slate-500"
        data-testid="no-assessment-evidence-empty"
      >
        <Sparkles class="w-8 h-8 text-slate-300 mx-auto mb-2" />
        <p class="text-xs font-medium">No diagnostic assessment evidence recorded yet.</p>
        <p class="text-[11px] text-slate-400 mt-0.5">Click "Add Score" to attach entrance exam or external assessment results.</p>
      </div>
    </div>
  </div>
</template>
