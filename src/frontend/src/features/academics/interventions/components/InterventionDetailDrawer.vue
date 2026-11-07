<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import {
  AlertTriangle,
  Calendar,
  CheckCircle2,
  Clock,
  FileText,
  History,
  Layers,
  Sparkles,
  User,
  XCircle,
} from 'lucide-vue-next';
import { StatusPill, FormField } from '@/shared/components';
import { formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { updateInterventionStatusSchema } from '../schemas/intervention.schema';
import { useUpdateInterventionStatusMutation } from '../composables/useAcademicInterventions';
import type {
  AcademicIntervention,
  InterventionStatus,
  UpdateInterventionStatusRequest,
} from '../types';

interface Props {
  visible: boolean;
  intervention: AcademicIntervention | null;
  canUpdateStatus?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  canUpdateStatus: true,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'updated', intervention: AcademicIntervention): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Update Status Modal / Confirmation State
const isStatusModalOpen = ref(false);
const targetStatus = ref<InterventionStatus>('in_progress');
const outcomeNotes = ref('');
const statusError = ref<string | null>(null);

const updateStatusMutation = useUpdateInterventionStatusMutation();

// Available status transitions based on current status
const availableTransitions = computed<Array<{ status: InterventionStatus; label: string; severity: 'primary' | 'success' | 'danger' | 'info' | 'warn' }>>(() => {
  if (!props.intervention) return [];
  const current = props.intervention.status;

  if (current === 'planned') {
    return [
      { status: 'in_progress', label: 'Start Intervention (In Progress)', severity: 'primary' },
      { status: 'cancelled', label: 'Cancel Intervention', severity: 'danger' },
    ];
  }

  if (current === 'in_progress') {
    return [
      { status: 'completed', label: 'Conclude & Complete Intervention', severity: 'success' },
      { status: 'cancelled', label: 'Cancel Intervention', severity: 'danger' },
    ];
  }

  return [];
});

function openStatusChangeDialog(newStatus: InterventionStatus) {
  targetStatus.value = newStatus;
  outcomeNotes.value = '';
  statusError.value = null;
  isStatusModalOpen.value = true;
}

function getStatusBadge(status: InterventionStatus) {
  switch (status) {
    case 'planned':
      return { severity: 'info', label: 'Planned' };
    case 'in_progress':
      return { severity: 'warn', label: 'In Progress' };
    case 'completed':
      return { severity: 'success', label: 'Completed' };
    case 'cancelled':
      return { severity: 'secondary', label: 'Cancelled' };
    default:
      return { severity: 'contrast', label: status };
  }
}

function formatInterventionType(type: string): string {
  return type
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

function formatTriggerType(trigger: string): string {
  switch (trigger) {
    case 'assessment_below_threshold':
      return 'Failing Assessment Score';
    case 'missing_work':
      return 'Missing Assignments';
    case 'attendance_concern':
      return 'Low Attendance (< 75%)';
    case 'standing_concern':
      return 'Standing Concern';
    case 'teacher_referral':
      return 'Teacher Referral';
    default:
      return trigger.replace(/_/g, ' ');
  }
}

async function handleConfirmStatusChange() {
  if (!props.intervention) return;
  statusError.value = null;

  const payload: UpdateInterventionStatusRequest = {
    status: targetStatus.value,
    outcomeNotes: outcomeNotes.value.trim() ? outcomeNotes.value.trim() : null,
  };

  const validation = updateInterventionStatusSchema.safeParse(payload);
  if (!validation.success) {
    statusError.value = validation.error.issues[0]?.message ?? 'Validation failed.';
    return;
  }

  try {
    const updated = await updateStatusMutation.mutateAsync({
      id: props.intervention.id,
      request: payload,
    });

    isStatusModalOpen.value = false;
    emit('updated', updated);
  } catch {
    // Handled by toast
  }
}
</script>

<template>
  <div>
    <!-- Main Detail Drawer -->
    <Drawer
      v-model:visible="isDrawerVisible"
      position="right"
      class="!w-[520px] max-w-full"
      data-testid="intervention-detail-drawer"
    >
      <template #header>
        <div class="flex items-center gap-3">
          <div class="p-2 rounded-lg bg-teal-50 dark:bg-teal-950/40 text-teal-600 dark:text-teal-400">
            <Sparkles class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-bold text-surface-900 dark:text-surface-100">
              Intervention Plan Details
            </h3>
            <p class="text-xs text-surface-500">
              UC-ACD-11 Academic Learning Support Action
            </p>
          </div>
        </div>
      </template>

      <div v-if="intervention" class="space-y-6 py-2 text-xs">
        <!-- Status & Context Card -->
        <div class="p-4 rounded-xl bg-surface-50 dark:bg-surface-800/60 border border-surface-200 dark:border-surface-700 space-y-3">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="font-semibold text-surface-700 dark:text-surface-300">Status:</span>
              <Tag
                :severity="getStatusBadge(intervention.status).severity"
                :value="getStatusBadge(intervention.status).label"
                class="uppercase text-[10px] font-bold tracking-wider"
                data-testid="intervention-status-badge"
              />
            </div>
            <span class="text-[11px] text-surface-500">
              Initiated: {{ formatDisplayDateTime(intervention.initiatedAt) }}
            </span>
          </div>

          <div class="grid grid-cols-2 gap-2 pt-2 border-t border-surface-200/60 dark:border-surface-700/60">
            <div>
              <span class="text-[10px] uppercase font-bold text-surface-400 block">Student</span>
              <span class="font-semibold text-surface-900 dark:text-surface-100">
                {{ intervention.studentName || intervention.studentId }}
              </span>
              <span v-if="intervention.studentIdentifier" class="text-surface-400 block text-[11px]">
                NISN: {{ intervention.studentIdentifier }}
              </span>
            </div>
            <div>
              <span class="text-[10px] uppercase font-bold text-surface-400 block">Learning Unit</span>
              <span class="font-semibold text-surface-900 dark:text-surface-100">
                {{ intervention.learningUnitName || intervention.learningUnitId }}
              </span>
              <span v-if="intervention.learningUnitCode" class="text-surface-400 block text-[11px]">
                {{ intervention.learningUnitCode }}
              </span>
            </div>
          </div>
        </div>

        <!-- Section: Support Strategy & Trigger -->
        <div class="space-y-3">
          <h4 class="font-bold text-surface-800 dark:text-surface-200 uppercase tracking-wider text-[11px]">
            Support Strategy & Trigger
          </h4>
          <div class="p-3 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700 space-y-2">
            <div class="flex items-center justify-between">
              <span class="text-surface-500">Intervention Type:</span>
              <span class="font-semibold text-teal-700 dark:text-teal-400 bg-teal-50 dark:bg-teal-950/40 px-2 py-0.5 rounded text-[11px]">
                {{ formatInterventionType(intervention.interventionType) }}
              </span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-surface-500">Trigger Source:</span>
              <span class="font-semibold text-surface-800 dark:text-surface-200">
                {{ formatTriggerType(intervention.triggerType) }}
              </span>
            </div>
            <div class="pt-2 border-t border-surface-100 dark:border-surface-800">
              <span class="text-surface-500 block mb-1">Trigger Description:</span>
              <p class="text-surface-800 dark:text-surface-200 bg-surface-50 dark:bg-surface-800/40 p-2 rounded whitespace-pre-line leading-relaxed">
                {{ intervention.triggerDescription }}
              </p>
            </div>
          </div>
        </div>

        <!-- Section: Action Plan -->
        <div class="space-y-2">
          <h4 class="font-bold text-surface-800 dark:text-surface-200 uppercase tracking-wider text-[11px]">
            Detailed Support Action Plan
          </h4>
          <div class="p-3 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700">
            <p class="text-surface-800 dark:text-surface-200 whitespace-pre-line leading-relaxed font-sans" data-testid="drawer-plan-description">
              {{ intervention.planDescription }}
            </p>
          </div>
        </div>

        <!-- Section: Outcome & Historical Conclusion -->
        <div v-if="intervention.concludedAt || intervention.outcomeNotes" class="space-y-2">
          <h4 class="font-bold text-surface-800 dark:text-surface-200 uppercase tracking-wider text-[11px]">
            Outcome Record
          </h4>
          <div class="p-3 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700 space-y-2">
            <div v-if="intervention.concludedAt" class="flex items-center justify-between text-surface-500">
              <span>Concluded At:</span>
              <span class="font-semibold text-surface-800 dark:text-surface-200">
                {{ formatDisplayDateTime(intervention.concludedAt) }}
              </span>
            </div>
            <div v-if="intervention.outcomeNotes" class="pt-2 border-t border-surface-100 dark:border-surface-800">
              <span class="text-surface-500 block mb-1">Outcome Narrative / Notes:</span>
              <p class="text-surface-800 dark:text-surface-200 bg-emerald-50/50 dark:bg-emerald-950/20 border border-emerald-200 dark:border-emerald-800 p-2 rounded whitespace-pre-line leading-relaxed" data-testid="drawer-outcome-notes">
                {{ intervention.outcomeNotes }}
              </p>
            </div>
          </div>
        </div>

        <!-- Section: Lifecycle Actions -->
        <div v-if="canUpdateStatus && availableTransitions.length > 0" class="pt-4 border-t border-surface-200 dark:border-surface-700 space-y-3">
          <h4 class="font-bold text-surface-800 dark:text-surface-200 uppercase tracking-wider text-[11px]">
            Progress Lifecycle Transition
          </h4>
          <div class="flex flex-wrap gap-2">
            <Button
              v-for="transition in availableTransitions"
              :key="transition.status"
              :label="transition.label"
              :severity="transition.severity"
              size="small"
              class="text-xs"
              @click="openStatusChangeDialog(transition.status)"
              :data-testid="`btn-transition-${transition.status}`"
            />
          </div>
        </div>

        <!-- Section: Audit Traceability -->
        <div class="pt-4 border-t border-surface-200 dark:border-surface-700 text-[11px] text-surface-400 space-y-1">
          <div>Initiated by: {{ intervention.initiatedBy }}</div>
          <div>Intervention ID: {{ intervention.id }}</div>
        </div>
      </div>
    </Drawer>

    <!-- Transition Status Confirmation / Outcome Dialog -->
    <Dialog
      v-model:visible="isStatusModalOpen"
      modal
      :header="targetStatus === 'completed' ? 'Conclude & Complete Intervention' : targetStatus === 'cancelled' ? 'Cancel Intervention' : 'Start Intervention'"
      :style="{ width: '480px', maxWidth: '95vw' }"
      data-testid="status-transition-dialog"
    >
      <div class="space-y-4 py-2 text-xs">
        <p class="text-surface-600 dark:text-surface-300">
          Transitioning intervention plan status to
          <span class="font-bold uppercase tracking-wider">{{ targetStatus }}</span>.
        </p>

        <!-- Mandatory Outcome Notes for Complete / Cancel -->
        <FormField
          v-if="targetStatus === 'completed' || targetStatus === 'cancelled'"
          :label="targetStatus === 'completed' ? 'Outcome Narrative & Mastery Result' : 'Reason for Cancellation'"
          required
          :error="statusError || undefined"
          :hint="targetStatus === 'completed' ? 'Document whether learning expectations were met, final test score, or ongoing recommendations (mandatory).' : 'Reason why support action was discontinued (mandatory).'"
        >
          <Textarea
            v-model="outcomeNotes"
            rows="4"
            :placeholder="targetStatus === 'completed' ? 'e.g. Student attended 4 remediation sessions and passed re-assessment with 78/100. Learning gaps resolved.' : 'e.g. Student transferred or plan superseded by alternative program.'"
            class="w-full text-xs"
            :invalid="Boolean(statusError)"
            maxlength="2000"
            data-testid="intervention-outcome-notes-input"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
          <Button
            label="Cancel"
            severity="secondary"
            variant="text"
            class="text-xs"
            @click="isStatusModalOpen = false"
          />
          <Button
            :label="targetStatus === 'completed' ? 'Complete Intervention' : targetStatus === 'cancelled' ? 'Confirm Cancellation' : 'Set In-Progress'"
            :severity="targetStatus === 'cancelled' ? 'danger' : 'primary'"
            icon="pi pi-check"
            class="text-xs"
            :loading="updateStatusMutation.isPending.value"
            @click="handleConfirmStatusChange"
            data-testid="btn-confirm-status-transition"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
