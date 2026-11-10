<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import {
  History,
  ShieldCheck,
  Calendar,
  User,
  ArrowRight,
  CheckCircle2,
  AlertCircle,
  FileText,
  Clock,
  Sparkles,
} from 'lucide-vue-next';
import { EmptyState, StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { useApproveRecordCorrectionMutation } from '../composables/useFacultyMutations';
import type {
  TeacherProfileDto,
  TeachingRecordCorrectionDto,
} from '../api/teachingTypes';

export interface CorrectionsAuditTabProps {
  teacher: TeacherProfileDto | null;
  canAuthorize?: boolean;
}

const props = withDefaults(defineProps<CorrectionsAuditTabProps>(), {
  teacher: null,
  canAuthorize: false,
});

const emit = defineEmits<{
  (e: 'correctionApproved', correction: TeachingRecordCorrectionDto): void;
  (e: 'refresh'): void;
}>();

// ---------------------------------------------------------------------------
// 1. Role-Gated Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canAuthorizeAction = computed(() => {
  return (
    props.canAuthorize ||
    authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE)
  );
});

// ---------------------------------------------------------------------------
// 2. Chronological Corrections List
// ---------------------------------------------------------------------------
const correctionsList = computed<TeachingRecordCorrectionDto[]>(() => {
  if (!props.teacher?.recentCorrections?.length) return [];
  // Sort descending by date / correctionNumber
  return [...props.teacher.recentCorrections].sort((a, b) => {
    const dateComp = (b.requestedDate || '').localeCompare(a.requestedDate || '');
    if (dateComp !== 0) return dateComp;
    return b.correctionNumber.localeCompare(a.correctionNumber);
  });
});

const requestedCount = computed(() => {
  return correctionsList.value.filter((c) => c.status === 'requested').length;
});

// ---------------------------------------------------------------------------
// 3. Approval Mutation
// ---------------------------------------------------------------------------
const approveMutation = useApproveRecordCorrectionMutation();
const approvingCorrectionId = ref<string | null>(null);

async function handleApproveCorrection(correction: TeachingRecordCorrectionDto) {
  approvingCorrectionId.value = correction.id;
  try {
    const result = await approveMutation.mutateAsync({
      id: correction.id,
      teacherId: props.teacher?.id,
    });
    emit('correctionApproved', result);
    emit('refresh');
  } finally {
    approvingCorrectionId.value = null;
  }
}

function formatTargetTableLabel(tableName?: string): string {
  if (!tableName) return 'Unknown Record';
  const normalized = tableName.replace(/^teaching\./, '');
  if (normalized === 'teacher_profiles') return 'Faculty Profile';
  if (normalized === 'teaching_authorizations') return 'Teaching Authorization';
  return normalized;
}

function formatFieldNameLabel(field: string): string {
  // Convert camelCase or snake_case to human words
  return field
    .replace(/_/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .toLowerCase()
    .replace(/\b\w/g, (c) => c.toUpperCase());
}
</script>

<template>
  <div class="space-y-4" data-testid="corrections-audit-tab">
    <!-- Header Banner -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 bg-surface-50 dark:bg-surface-800/40 rounded-xl border border-surface-200 dark:border-surface-700">
      <div class="space-y-0.5">
        <div class="flex items-center gap-2">
          <History class="w-5 h-5 text-primary-600 dark:text-primary-400" />
          <h3 class="text-sm font-bold text-surface-900 dark:text-surface-100">
            Auditable Record Corrections Log (TD-06)
          </h3>
        </div>
        <p class="text-xs text-surface-500">
          "No Silent Overwrites" protocol: immutable chronological ledger of modifications with before/after delta tracking.
        </p>
      </div>

      <div v-if="requestedCount > 0" class="flex items-center gap-2">
        <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-100 dark:bg-amber-950/70 text-amber-800 dark:text-amber-200 border border-amber-300 dark:border-amber-800">
          <Clock class="w-3.5 h-3.5 text-amber-600" />
          <span>{{ requestedCount }} Pending Authority Approval</span>
        </span>
      </div>
    </div>

    <!-- Empty State -->
    <div
      v-if="!teacher || correctionsList.length === 0"
      class="p-8 text-center"
      data-testid="empty-corrections"
    >
      <EmptyState
        title="No record corrections logged"
        description="All field corrections on approved teacher profiles and authorizations are logged immutably with before/after audit tracking. No modifications have been made to this dossier yet."
        bordered
      />
    </div>

    <!-- Corrections List -->
    <div v-else class="space-y-3" data-testid="corrections-list">
      <div
        v-for="corr in correctionsList"
        :key="corr.id"
        class="p-4 bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs space-y-3 transition-colors"
        :data-testid="`correction-card-${corr.id}`"
      >
        <!-- Top Row: Number, Entity, Field & Status -->
        <div class="flex flex-wrap items-center justify-between gap-2 pb-2 border-b border-surface-100 dark:border-surface-800">
          <div class="flex flex-wrap items-center gap-2">
            <span class="font-mono font-bold text-xs text-surface-900 dark:text-surface-100 bg-surface-100 dark:bg-surface-800 px-2 py-0.5 rounded">
              {{ corr.correctionNumber }}
            </span>

            <span class="text-xs font-medium text-surface-600 dark:text-surface-300">
              {{ formatTargetTableLabel(corr.targetTableName) }} &bull;
              <strong>{{ formatFieldNameLabel(corr.fieldName) }}</strong>
            </span>

            <StatusPill
              :status="corr.status"
              size="sm"
              :variant="
                corr.status === 'applied' || corr.status === 'approved'
                  ? 'emerald'
                  : corr.status === 'requested'
                    ? 'amber'
                    : corr.status === 'rejected'
                      ? 'rose'
                      : 'slate'
              "
            />
          </div>

          <div class="flex items-center gap-2">
            <!-- Approve Button for requested corrections -->
            <Button
              v-if="corr.status === 'requested' && canAuthorizeAction"
              label="Approve Correction"
              icon="pi pi-check"
              severity="success"
              size="small"
              class="!text-xs !py-1 !px-2.5 font-medium"
              :loading="approvingCorrectionId === corr.id"
              :data-testid="`approve-correction-btn-${corr.id}`"
              @click="handleApproveCorrection(corr)"
            />

            <span class="text-[11px] text-surface-400">
              {{ formatDisplayDate(corr.requestedDate) }}
            </span>
          </div>
        </div>

        <!-- Before / After Delta Display -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3" data-testid="delta-comparison">
          <!-- Original Value Box -->
          <div class="p-3 rounded-lg bg-rose-50/50 dark:bg-rose-950/20 border border-rose-200/60 dark:border-rose-900/40 space-y-1">
            <span class="text-[10px] font-bold uppercase tracking-wider text-rose-600 dark:text-rose-400 block">
              Original Value
            </span>
            <div class="font-mono text-xs text-surface-800 dark:text-surface-200 break-words">
              {{ corr.originalValue || '(empty / null)' }}
            </div>
          </div>

          <!-- Corrected Value Box -->
          <div class="p-3 rounded-lg bg-emerald-50/50 dark:bg-emerald-950/20 border border-emerald-200/60 dark:border-emerald-900/40 space-y-1">
            <span class="text-[10px] font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 block">
              Corrected Value
            </span>
            <div class="font-mono text-xs text-surface-900 dark:text-surface-100 font-semibold break-words">
              {{ corr.correctedValue }}
            </div>
          </div>
        </div>

        <!-- Governance Justification Reason -->
        <div class="text-xs space-y-0.5">
          <span class="font-semibold text-surface-700 dark:text-surface-300">Justification Reason:</span>
          <p class="text-surface-600 dark:text-surface-400 text-xs italic">
            "{{ corr.reasonForCorrection }}"
          </p>
        </div>

        <!-- Supporting Evidence & Downstream Impact -->
        <div
          v-if="corr.supportingEvidence || corr.effectiveDateImplications"
          class="flex flex-wrap gap-2 text-[11px] text-surface-500 pt-1"
        >
          <span v-if="corr.supportingEvidence" class="px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800">
            Evidence: {{ corr.supportingEvidence }}
          </span>
          <span v-if="corr.effectiveDateImplications" class="px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800">
            Implications: {{ corr.effectiveDateImplications }}
          </span>
        </div>

        <!-- Audit Stamps Footer -->
        <div class="pt-2 border-t border-surface-100 dark:border-surface-800 flex flex-wrap items-center justify-between text-[11px] text-surface-400">
          <div class="flex items-center gap-1.5">
            <User class="w-3.5 h-3.5" />
            <span>Requested by <strong>{{ corr.requestedBy }}</strong> on {{ formatDisplayDate(corr.requestedDate) }}</span>
          </div>

          <div v-if="corr.approvedBy" class="flex items-center gap-1.5 text-emerald-700 dark:text-emerald-400 font-medium">
            <ShieldCheck class="w-3.5 h-3.5" />
            <span>Approved by <strong>{{ corr.approvedBy }}</strong> on {{ formatDisplayDate(corr.approvedDate) }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
