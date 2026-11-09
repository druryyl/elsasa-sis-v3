<script setup lang="ts">
import { computed, ref } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import {
  Layers,
  Award,
  Scale,
  History,
  FileText,
  AlertTriangle,
} from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  Assessment,
  CancelAssessmentResponse,
  ConductAssessmentResponse,
} from '../types';
import AssessmentStatusBadge from './AssessmentStatusBadge.vue';
import {
  useCancelAssessmentMutation,
  useConductAssessmentMutation,
} from '../composables/useAssessments';

interface Props {
  visible: boolean;
  assessment: Assessment | null;
  canManage?: boolean;
  canConduct?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  canManage: true,
  canConduct: true,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'edit', assessment: Assessment): void;
  (e: 'conducted', response: ConductAssessmentResponse): void;
  (e: 'cancelled', response: CancelAssessmentResponse): void;
  (e: 'recordEvidence', assessmentId: string): void;
  (e: 'openScoring', assessment: Assessment): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Conduct Dialog State
const isConductDialogOpen = ref(false);
const conductDate = ref(new Date().toISOString().slice(0, 10));
const conductNotes = ref('');

// Cancel Dialog State
const isCancelDialogOpen = ref(false);
const cancelReason = ref('');

// Mutations
const conductMutation = useConductAssessmentMutation();
const cancelMutation = useCancelAssessmentMutation();

function openConductDialog() {
  conductDate.value = new Date().toISOString().slice(0, 10);
  conductNotes.value = '';
  isConductDialogOpen.value = true;
}

async function handleConfirmConduct() {
  if (!props.assessment) return;

  try {
    const res = await conductMutation.mutateAsync({
      id: props.assessment.id,
      request: {
        assessmentDate: conductDate.value ? conductDate.value.trim() : null,
        notes: conductNotes.value ? conductNotes.value.trim() : null,
      },
    });

    isConductDialogOpen.value = false;
    emit('conducted', res);
  } catch {
    // Handled by toast
  }
}

function openCancelDialog() {
  cancelReason.value = '';
  isCancelDialogOpen.value = true;
}

async function handleConfirmCancel() {
  if (!props.assessment || !cancelReason.value.trim()) return;

  try {
    const res = await cancelMutation.mutateAsync({
      id: props.assessment.id,
      reason: cancelReason.value.trim(),
    });

    isCancelDialogOpen.value = false;
    emit('cancelled', res);
  } catch {
    // Handled by toast
  }
}

const reassessmentPolicyDescription = computed(() => {
  switch (props.assessment?.reassessmentPolicy) {
    case 'capped_at_passing':
      return 'Retake scores cannot exceed the KKM passing threshold. All attempts are preserved for audit.';
    case 'highest_score':
      return 'Best score across all recorded attempts becomes operative.';
    case 'latest_attempt':
      return 'Most recent evaluation attempt unconditionally replaces previous mark.';
    case 'average':
      return 'Operative mark is the arithmetic mean across all evaluation attempts.';
    case 'none':
    default:
      return 'Single attempt evaluation. Retakes are not configured.';
  }
});
</script>

<template>
  <Drawer
    v-model:visible="isDrawerVisible"
    position="right"
    class="w-full max-w-xl"
    :dismissable="true"
    data-testid="assessment-detail-drawer"
  >
    <template #header>
      <div v-if="assessment" class="flex items-center gap-2">
        <AssessmentStatusBadge :status="assessment.status" size="sm" />
        <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
          {{ assessment.assessmentType }} Assessment
        </span>
      </div>
    </template>

    <div v-if="assessment" class="space-y-6 text-sm">
      <!-- Title & Unit Context -->
      <div class="space-y-1.5">
        <h2 class="text-xl font-bold text-slate-900 leading-snug">
          {{ assessment.title }}
        </h2>
        <div class="flex items-center gap-2 text-xs text-slate-600">
          <Layers class="w-3.5 h-3.5 text-teal-600" />
          <span class="font-medium text-slate-800">
            {{ assessment.learningUnitCode ?? '' }} {{ assessment.learningUnitTitle ?? '' }}
          </span>
        </div>
      </div>

      <!-- Quick Metrics Strip -->
      <div class="grid grid-cols-3 gap-2.5 p-3 rounded-xl bg-slate-50 border border-slate-200/80">
        <div class="text-center">
          <div class="text-[10px] uppercase font-bold text-slate-400">Participants</div>
          <div class="text-lg font-bold text-slate-900 mt-0.5">
            {{ assessment.participantCount ?? assessment.resultCount }}
          </div>
        </div>
        <div class="text-center border-x border-slate-200">
          <div class="text-[10px] uppercase font-bold text-slate-400">Evidence Collected</div>
          <div class="text-lg font-bold text-teal-700 mt-0.5">
            {{ assessment.evidenceCount }}
          </div>
        </div>
        <div class="text-center">
          <div class="text-[10px] uppercase font-bold text-slate-400">Results Evaluated</div>
          <div class="text-lg font-bold text-indigo-700 mt-0.5">
            {{ assessment.resultCount }}
          </div>
        </div>
      </div>

      <!-- Scoring Specifications Card -->
      <div class="rounded-xl border border-slate-200/80 p-4 space-y-3 bg-white shadow-2xs">
        <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center gap-1.5">
          <Scale class="w-4 h-4 text-slate-500" />
          <span>Evaluation & Scoring Specifications</span>
        </h3>

        <div class="grid grid-cols-2 gap-3 text-xs">
          <div>
            <span class="text-slate-400 block text-[11px]">Assessment Method</span>
            <span class="font-medium text-slate-800 capitalize">
              {{ assessment.assessmentMethod.replace(/_/g, ' ') }}
            </span>
          </div>

          <div>
            <span class="text-slate-400 block text-[11px]">Maximum Score</span>
            <span class="font-bold text-slate-900">
              {{ assessment.maxScore ?? 100 }} pts
            </span>
          </div>

          <div>
            <span class="text-slate-400 block text-[11px]">Passing Threshold (KKM)</span>
            <span
              v-if="assessment.passingScore != null"
              class="font-bold text-amber-700 inline-flex items-center gap-1"
            >
              <Award class="w-3.5 h-3.5 text-amber-500" />
              {{ assessment.passingScore }} pts
            </span>
            <span v-else class="text-slate-400 italic">Not configured</span>
          </div>

          <div>
            <span class="text-slate-400 block text-[11px]">Assessment Date</span>
            <span class="font-medium text-slate-800">
              {{ assessment.assessmentDate ? formatDisplayDate(assessment.assessmentDate) : 'Not scheduled' }}
            </span>
          </div>
        </div>

        <!-- Reassessment Policy Box -->
        <div class="pt-2 border-t border-slate-100 space-y-1">
          <div class="flex items-center gap-1.5 text-xs">
            <History class="w-3.5 h-3.5 text-teal-600" />
            <span class="text-slate-500">Reassessment Policy:</span>
            <span class="font-semibold text-slate-900 capitalize">
              {{ assessment.reassessmentPolicy.replace(/_/g, ' ') }}
            </span>
          </div>
          <p class="text-[11px] text-slate-500 leading-relaxed pl-5">
            {{ reassessmentPolicyDescription }}
          </p>
        </div>

        <!-- Assessor Reference -->
        <div v-if="assessment.assessorReference" class="pt-2 border-t border-slate-100 text-xs">
          <span class="text-slate-400 block text-[11px]">Assessor / Examiner Reference</span>
          <span class="font-medium text-slate-800">{{ assessment.assessorReference }}</span>
        </div>
      </div>

      <!-- Criteria & Rubric Descriptors -->
      <div class="rounded-xl border border-slate-200/80 p-4 space-y-2 bg-white shadow-2xs">
        <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center gap-1.5">
          <FileText class="w-4 h-4 text-slate-500" />
          <span>Criteria & Rubric Descriptors</span>
        </h3>
        <div
          v-if="assessment.criteria"
          class="text-xs text-slate-700 leading-relaxed whitespace-pre-wrap bg-slate-50 p-3 rounded-lg border border-slate-100"
        >
          {{ assessment.criteria }}
        </div>
        <div v-else class="text-xs text-slate-400 italic">
          No rubric descriptors or criteria entered.
        </div>
      </div>

      <!-- Administrative Notes -->
      <div v-if="assessment.notes" class="rounded-xl border border-slate-200/80 p-4 space-y-1 bg-white shadow-2xs">
        <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700">Administrative Notes</h3>
        <p class="text-xs text-slate-600 whitespace-pre-wrap">{{ assessment.notes }}</p>
      </div>

      <!-- Actions Strip -->
      <div class="pt-4 border-t border-slate-200 space-y-2">
        <div class="grid grid-cols-2 gap-2">
          <!-- Open Scoring Grid Button -->
          <Button
            type="button"
            label="Open Scoring Grid"
            icon="pi pi-table"
            size="small"
            class="w-full"
            data-testid="drawer-open-scoring-btn"
            @click="emit('openScoring', assessment)"
          />

          <!-- Record Evidence Button -->
          <Button
            type="button"
            label="Record Evidence"
            icon="pi pi-paperclip"
            severity="secondary"
            variant="outlined"
            size="small"
            class="w-full"
            data-testid="drawer-record-evidence-btn"
            @click="emit('recordEvidence', assessment.id)"
          />
        </div>

        <div class="flex items-center gap-2 pt-1">
          <!-- Conduct Action (if planned) -->
          <Button
            v-if="assessment.status === 'planned'"
            type="button"
            label="Conduct Assessment"
            icon="pi pi-play"
            severity="success"
            size="small"
            class="flex-1"
            :disabled="!canConduct"
            data-testid="drawer-conduct-btn"
            @click="openConductDialog"
          />

          <!-- Edit Action -->
          <Button
            v-if="assessment.status !== 'cancelled'"
            type="button"
            label="Edit"
            icon="pi pi-pencil"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="!canManage"
            data-testid="drawer-edit-btn"
            @click="emit('edit', assessment)"
          />

          <!-- Cancel Action -->
          <Button
            v-if="assessment.status === 'planned'"
            type="button"
            label="Cancel"
            icon="pi pi-times"
            severity="danger"
            variant="outlined"
            size="small"
            :disabled="!canManage"
            data-testid="drawer-cancel-btn"
            @click="openCancelDialog"
          />
        </div>
      </div>
    </div>

    <!-- Conduct Confirmation Modal -->
    <Dialog
      v-model:visible="isConductDialogOpen"
      modal
      header="Conduct Assessment"
      class="w-full max-w-sm"
      :dismissable-mask="true"
      data-testid="conduct-dialog"
    >
      <div class="space-y-4 pt-1">
        <p class="text-xs text-slate-600">
          Mark this assessment event as <strong>conducted</strong>. This signals that evaluation papers have been administered and student evidence is ready for rubric scoring.
        </p>

        <div class="space-y-1">
          <label class="block text-xs font-medium text-slate-700">Conducted Date</label>
          <InputText
            v-model="conductDate"
            placeholder="YYYY-MM-DD"
            class="w-full text-xs"
            data-testid="conduct-date-input"
          />
        </div>

        <div class="space-y-1">
          <label class="block text-xs font-medium text-slate-700">Notes (Optional)</label>
          <Textarea
            v-model="conductNotes"
            rows="2"
            placeholder="Delivery observations, room, etc."
            class="w-full text-xs"
            data-testid="conduct-notes-textarea"
          />
        </div>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            label="Back"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isConductDialogOpen = false"
          />
          <Button
            type="button"
            label="Confirm Conducted"
            icon="pi pi-check"
            severity="success"
            size="small"
            :loading="conductMutation.isPending.value"
            data-testid="confirm-conduct-btn"
            @click="handleConfirmConduct"
          />
        </div>
      </div>
    </Dialog>

    <!-- Cancel Confirmation Modal -->
    <Dialog
      v-model:visible="isCancelDialogOpen"
      modal
      header="Cancel Assessment"
      class="w-full max-w-sm"
      :dismissable-mask="true"
      data-testid="cancel-dialog"
    >
      <div class="space-y-4 pt-1">
        <div class="flex items-start gap-2 p-2.5 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-800">
          <AlertTriangle class="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
          <span>Cancelling this assessment will prevent further evidence intake or scoring. A mandatory reason is required.</span>
        </div>

        <div class="space-y-1">
          <label class="block text-xs font-medium text-slate-700">Cancellation Reason</label>
          <Textarea
            v-model="cancelReason"
            rows="3"
            placeholder="Provide reason for cancellation (e.g. Rescheduled due to campus closure)..."
            class="w-full text-xs"
            data-testid="cancel-reason-textarea"
          />
        </div>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            label="Back"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isCancelDialogOpen = false"
          />
          <Button
            type="button"
            label="Confirm Cancellation"
            icon="pi pi-trash"
            severity="danger"
            size="small"
            :disabled="!cancelReason.trim()"
            :loading="cancelMutation.isPending.value"
            data-testid="confirm-cancel-btn"
            @click="handleConfirmCancel"
          />
        </div>
      </div>
    </Dialog>
  </Drawer>
</template>
