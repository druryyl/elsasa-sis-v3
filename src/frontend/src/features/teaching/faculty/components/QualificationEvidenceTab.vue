<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import {
  Award,
  Plus,
  ShieldCheck,
  ShieldAlert,
  FileText,
  ExternalLink,
  Calendar,
  Building2,
  Clock,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Info,
} from 'lucide-vue-next';
import { EmptyState, StatusPill, FormField } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { useQualificationTypesQuery } from '../composables/useFacultyQueries';
import {
  useVerifyEvidenceMutation,
  useRejectEvidenceMutation,
} from '../composables/useFacultyMutations';
import { rejectEvidenceSchema } from '../api/teachingSchemas';
import SubmitEvidenceModal from './SubmitEvidenceModal.vue';
import type {
  TeacherProfileDto,
  QualificationEvidenceDto,
  QualificationTypeResponse,
  SubmitQualificationEvidenceResponse,
  VerifyQualificationEvidenceResponse,
  RejectQualificationEvidenceResponse,
} from '../api/teachingTypes';

export interface QualificationEvidenceTabProps {
  teacher: TeacherProfileDto | null;
  canManage?: boolean;
  canVerify?: boolean;
}

const props = withDefaults(defineProps<QualificationEvidenceTabProps>(), {
  teacher: null,
  canManage: false,
  canVerify: false,
});

const emit = defineEmits<{
  (e: 'evidenceSubmitted', response: SubmitQualificationEvidenceResponse): void;
  (e: 'evidenceVerified', response: VerifyQualificationEvidenceResponse): void;
  (e: 'evidenceRejected', response: RejectQualificationEvidenceResponse): void;
  (e: 'refresh'): void;
}>();

// ---------------------------------------------------------------------------
// 1. Role-Gated Authorization Checks
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canManageAction = computed(() => {
  return (
    props.canManage ||
    authStore.hasPermission(TEACHING_PERMISSIONS.MANAGE)
  );
});

const canVerifyAction = computed(() => {
  return (
    props.canVerify ||
    authStore.hasPermission(TEACHING_PERMISSIONS.VERIFY) ||
    authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE)
  );
});

// ---------------------------------------------------------------------------
// 2. Qualification Types & Credentials List
// ---------------------------------------------------------------------------
const { data: qualificationTypes, isLoading: isLoadingTypes } = useQualificationTypesQuery();

const qualTypesMap = computed(() => {
  const map = new Map<string, QualificationTypeResponse>();
  if (qualificationTypes.value) {
    for (const type of qualificationTypes.value) {
      map.set(type.id, type);
    }
  }
  return map;
});

function getQualificationTypeName(typeId: string): string {
  const type = qualTypesMap.value.get(typeId);
  return type ? `${type.name} (${type.code})` : typeId;
}

const qualificationsList = computed<QualificationEvidenceDto[]>(() => {
  return props.teacher?.qualifications ?? [];
});

const pendingVerificationCount = computed(() => {
  return qualificationsList.value.filter(
    (q) => q.verificationState === 'submitted' || q.verificationState === 'claimed',
  ).length;
});

const verifiedCount = computed(() => {
  return qualificationsList.value.filter((q) => q.verificationState === 'verified').length;
});

// ---------------------------------------------------------------------------
// 3. Submit Evidence Modal State
// ---------------------------------------------------------------------------
const isSubmitModalOpen = ref(false);

function handleOpenSubmitModal() {
  isSubmitModalOpen.value = true;
}

function handleEvidenceSubmitted(response: SubmitQualificationEvidenceResponse) {
  emit('evidenceSubmitted', response);
  emit('refresh');
}

// ---------------------------------------------------------------------------
// 4. Verify Credential Evidence Dialog State
// ---------------------------------------------------------------------------
const evidenceToVerify = ref<QualificationEvidenceDto | null>(null);
const isVerifyDialogOpen = ref(false);
const verifyNotes = ref('');

const verifyEvidenceMutation = useVerifyEvidenceMutation();

function handlePromptVerify(evidence: QualificationEvidenceDto) {
  evidenceToVerify.value = evidence;
  verifyNotes.value = '';
  isVerifyDialogOpen.value = true;
}

async function handleConfirmVerify() {
  if (!evidenceToVerify.value) return;

  try {
    const response = await verifyEvidenceMutation.mutateAsync({
      id: evidenceToVerify.value.id,
      payload: {
        verificationNotes: verifyNotes.value?.trim() || undefined,
      },
      teacherId: props.teacher?.id,
    });

    isVerifyDialogOpen.value = false;
    evidenceToVerify.value = null;
    verifyNotes.value = '';
    emit('evidenceVerified', response);
    emit('refresh');
  } catch {
    // Handled by mutation toast notification
  }
}

// ---------------------------------------------------------------------------
// 5. Reject Credential Evidence Dialog State
// ---------------------------------------------------------------------------
const evidenceToReject = ref<QualificationEvidenceDto | null>(null);
const isRejectDialogOpen = ref(false);
const rejectionReason = ref('');
const rejectionError = ref('');

const rejectEvidenceMutation = useRejectEvidenceMutation();

function handlePromptReject(evidence: QualificationEvidenceDto) {
  evidenceToReject.value = evidence;
  rejectionReason.value = '';
  rejectionError.value = '';
  isRejectDialogOpen.value = true;
}

async function handleConfirmReject() {
  if (!evidenceToReject.value) return;

  rejectionError.value = '';
  const validation = rejectEvidenceSchema.safeParse({
    rejectionReason: rejectionReason.value,
  });

  if (!validation.success) {
    rejectionError.value =
      validation.error.errors[0]?.message || 'Rejection reason is mandatory.';
    return;
  }

  try {
    const response = await rejectEvidenceMutation.mutateAsync({
      id: evidenceToReject.value.id,
      payload: {
        rejectionReason: validation.data.rejectionReason,
      },
      teacherId: props.teacher?.id,
    });

    isRejectDialogOpen.value = false;
    evidenceToReject.value = null;
    rejectionReason.value = '';
    emit('evidenceRejected', response);
    emit('refresh');
  } catch {
    // Handled by mutation toast notification
  }
}

// ---------------------------------------------------------------------------
// 6. Visual Presentation Helper
// ---------------------------------------------------------------------------
function isAwaitingVerification(state: string): boolean {
  return state === 'submitted' || state === 'claimed';
}
</script>

<template>
  <div
    class="space-y-6"
    data-testid="qualification-evidence-tab"
  >
    <!-- Section Header & Action Toolbar -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-surface-200 dark:border-surface-700">
      <div>
        <div class="flex items-center gap-2">
          <Award class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
          <h3 class="text-sm font-semibold uppercase tracking-wider text-surface-900 dark:text-surface-100">
            Qualification Documentary Evidence (TD-05)
          </h3>
          <span
            v-if="qualificationsList.length"
            class="text-[11px] px-2 py-0.5 rounded-full bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300 font-medium"
            data-testid="qualifications-count-badge"
          >
            {{ qualificationsList.length }} Total
          </span>
          <span
            v-if="pendingVerificationCount > 0"
            class="text-[11px] px-2 py-0.5 rounded-full bg-amber-50 dark:bg-amber-950/60 border border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-300 font-medium flex items-center gap-1"
            data-testid="pending-verification-badge"
          >
            <Clock class="w-3 h-3 text-amber-600" />
            {{ pendingVerificationCount }} Awaiting Verification
          </span>
        </div>
        <p class="text-xs text-surface-500 mt-0.5">
          Documentary credentials, certification authenticity verification, and expiry tracking. Verified qualifications confirm authenticity without automatically granting teaching authorizations.
        </p>
      </div>

      <Button
        v-if="canManageAction"
        severity="primary"
        size="small"
        class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium shrink-0"
        data-testid="open-submit-evidence-modal-btn"
        @click="handleOpenSubmitModal"
      >
        <Plus class="w-4 h-4" />
        <span>Submit Evidence</span>
      </Button>
    </div>

    <!-- Empty State -->
    <div
      v-if="!qualificationsList || qualificationsList.length === 0"
      class="p-8 text-center"
      data-testid="empty-qualifications"
    >
      <EmptyState
        title="No Qualification Documentary Evidence"
        description="Submit certificates, academic degrees, or professional credentials for independent authenticity verification."
        bordered
      >
        <template #actions>
          <Button
            v-if="canManageAction"
            severity="primary"
            size="small"
            class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
            data-testid="empty-submit-evidence-btn"
            @click="handleOpenSubmitModal"
          >
            <Plus class="w-4 h-4" />
            <span>Submit First Credential</span>
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- Credentials Table / Cards List -->
    <div
      v-else
      class="space-y-4"
      data-testid="qualifications-list"
    >
      <!-- Submissions Awaiting Verification Alert -->
      <div
        v-if="pendingVerificationCount > 0 && canVerifyAction"
        class="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 text-amber-900 dark:text-amber-200 flex items-start gap-3 text-xs leading-relaxed"
        data-testid="pending-review-banner"
      >
        <ShieldAlert class="w-5 h-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
        <div>
          <span class="font-semibold">Review Action Required:</span>
          There are {{ pendingVerificationCount }} qualification documentary submission(s) awaiting authenticity validation. As a designated verifier or teaching authority, inspect documentary proofs and record a verification or rejection decision.
        </div>
      </div>

      <!-- Structured Credential Cards -->
      <div class="space-y-3">
        <div
          v-for="qual in qualificationsList"
          :key="qual.id"
          :class="[
            'p-4 rounded-xl border transition-all duration-200 text-xs shadow-2xs space-y-3',
            isAwaitingVerification(qual.verificationState)
              ? 'bg-amber-50/20 dark:bg-amber-950/10 border-amber-200 dark:border-amber-800/80 ring-1 ring-amber-300/40'
              : qual.verificationState === 'verified'
              ? 'bg-surface-0 dark:bg-surface-900 border-surface-200 dark:border-surface-700'
              : qual.verificationState === 'rejected'
              ? 'bg-rose-50/20 dark:bg-rose-950/10 border-rose-200 dark:border-rose-800/70'
              : 'bg-surface-0 dark:bg-surface-900 border-surface-200 dark:border-surface-700',
          ]"
          :data-testid="`qualification-card-${qual.id}`"
        >
          <!-- Card Top: Title, Category & Verification State -->
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-2.5">
            <div class="space-y-1">
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-semibold text-sm text-surface-900 dark:text-surface-100">
                  {{ qual.title }}
                </span>
                <StatusPill
                  :status="qual.verificationState"
                  size="sm"
                  :data-testid="`qual-state-pill-${qual.id}`"
                />
                <span
                  v-if="isAwaitingVerification(qual.verificationState)"
                  class="text-[10px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded bg-amber-100 dark:bg-amber-900/60 text-amber-800 dark:text-amber-200 border border-amber-200 dark:border-amber-700"
                  data-testid="awaiting-verification-tag"
                >
                  Awaiting Verification
                </span>
              </div>

              <!-- Qualification Type Category -->
              <div class="flex items-center gap-1.5 text-surface-500 text-xs">
                <FileText class="w-3.5 h-3.5 text-surface-400" />
                <span class="font-medium">
                  {{ getQualificationTypeName(qual.qualificationTypeId) }}
                </span>
              </div>
            </div>

            <!-- Role-Gated Action Buttons: Verify / Reject -->
            <div
              v-if="canVerifyAction"
              class="flex items-center gap-1.5 shrink-0"
              data-testid="qual-actions-container"
            >
              <Button
                v-if="qual.verificationState !== 'verified'"
                type="button"
                severity="success"
                size="small"
                variant="outlined"
                class="!text-xs !py-1 !px-2.5 !gap-1 font-medium"
                :data-testid="`verify-evidence-btn-${qual.id}`"
                @click="handlePromptVerify(qual)"
              >
                <CheckCircle2 class="w-3.5 h-3.5" />
                <span>Verify</span>
              </Button>

              <Button
                v-if="qual.verificationState !== 'rejected'"
                type="button"
                severity="danger"
                size="small"
                variant="outlined"
                class="!text-xs !py-1 !px-2.5 !gap-1 font-medium"
                :data-testid="`reject-evidence-btn-${qual.id}`"
                @click="handlePromptReject(qual)"
              >
                <XCircle class="w-3.5 h-3.5" />
                <span>Reject</span>
              </Button>
            </div>
          </div>

          <!-- Attributes Grid: Issuing Authority, Credential ID, Validity, Document Reference -->
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 py-2 border-t border-b border-surface-100 dark:border-surface-800 text-surface-600 dark:text-surface-300">
            <!-- Issuer -->
            <div class="space-y-0.5">
              <span class="text-[10px] uppercase font-semibold text-surface-400 block">
                Issuing Authority
              </span>
              <div class="flex items-center gap-1.5 font-medium text-surface-800 dark:text-surface-200">
                <Building2 class="w-3.5 h-3.5 text-surface-400 shrink-0" />
                <span>{{ qual.issuingAuthority }}</span>
              </div>
            </div>

            <!-- Credential ID -->
            <div class="space-y-0.5">
              <span class="text-[10px] uppercase font-semibold text-surface-400 block">
                Credential Identifier
              </span>
              <div class="font-mono text-surface-800 dark:text-surface-200">
                {{ qual.credentialIdentifier || '—' }}
              </div>
            </div>

            <!-- Dates -->
            <div class="space-y-0.5">
              <span class="text-[10px] uppercase font-semibold text-surface-400 block">
                Validity Period
              </span>
              <div class="flex items-center gap-1.5 text-surface-800 dark:text-surface-200">
                <Calendar class="w-3.5 h-3.5 text-surface-400 shrink-0" />
                <span>
                  {{ formatDisplayDate(qual.issueDate) }}
                  <span v-if="qual.expiryDate"> to {{ formatDisplayDate(qual.expiryDate) }}</span>
                  <span v-else class="text-surface-400"> (Permanent)</span>
                </span>
              </div>
            </div>

            <!-- Document Reference -->
            <div class="space-y-0.5">
              <span class="text-[10px] uppercase font-semibold text-surface-400 block">
                Document Archive
              </span>
              <div v-if="qual.documentReference" class="truncate">
                <a
                  :href="qual.documentReference"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="inline-flex items-center gap-1 text-primary-600 dark:text-primary-400 hover:underline max-w-full font-medium"
                  :title="qual.documentReference"
                  data-testid="evidence-doc-link"
                >
                  <ExternalLink class="w-3.5 h-3.5 shrink-0" />
                  <span class="truncate">{{ qual.documentReference }}</span>
                </a>
              </div>
              <span v-else class="text-surface-400 italic">No document link</span>
            </div>
          </div>

          <!-- Conditions or Restrictions -->
          <div
            v-if="qual.restrictionsOrConditions"
            class="text-xs text-surface-600 dark:text-surface-400 italic"
          >
            <span class="font-semibold not-italic text-surface-700 dark:text-surface-300">
              Conditions:
            </span>
            {{ qual.restrictionsOrConditions }}
          </div>

          <!-- Verification Audit Trail (When Verified) -->
          <div
            v-if="qual.verifiedBy"
            class="p-2.5 rounded-lg bg-emerald-50/60 dark:bg-emerald-950/30 border border-emerald-200/70 dark:border-emerald-800/50 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs"
            data-testid="qual-verified-audit"
          >
            <div class="flex items-center gap-1.5 text-emerald-800 dark:text-emerald-300">
              <ShieldCheck class="w-4 h-4 text-emerald-600 shrink-0" />
              <span>
                Verified by <strong>{{ qual.verifiedBy }}</strong>
                <span v-if="qual.verifiedDate"> on {{ formatDisplayDate(qual.verifiedDate) }}</span>
              </span>
            </div>
            <div
              v-if="qual.verificationNotes"
              class="text-surface-600 dark:text-surface-400 italic"
            >
              Notes: {{ qual.verificationNotes }}
            </div>
          </div>

          <!-- Rejection Audit Trail (When Rejected) -->
          <div
            v-if="qual.rejectionReason"
            class="p-2.5 rounded-lg bg-rose-50/60 dark:bg-rose-950/30 border border-rose-200/70 dark:border-rose-800/50 space-y-1 text-xs text-rose-800 dark:text-rose-300"
            data-testid="qual-rejected-audit"
          >
            <div class="flex items-center gap-1.5 font-semibold">
              <XCircle class="w-4 h-4 text-rose-600 shrink-0" />
              <span>Rejection Rationale:</span>
            </div>
            <p class="text-surface-700 dark:text-surface-300 pl-5">
              {{ qual.rejectionReason }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- Submit Credential Evidence Modal -->
    <SubmitEvidenceModal
      v-model:visible="isSubmitModalOpen"
      :teacher-id="teacher?.id"
      :qualification-types="qualificationTypes"
      @submitted="handleEvidenceSubmitted"
    />

    <!-- Role-Gated Verify Dialog -->
    <Dialog
      v-model:visible="isVerifyDialogOpen"
      modal
      :draggable="false"
      class="w-full max-w-lg mx-4"
      data-testid="verify-evidence-dialog"
    >
      <template #header>
        <div class="flex items-center gap-2 text-emerald-600 dark:text-emerald-400">
          <ShieldCheck class="w-5 h-5" />
          <h2 class="text-base font-semibold">
            Verify Qualification Documentary Evidence
          </h2>
        </div>
      </template>

      <div class="space-y-4 pt-1 text-xs">
        <!-- Credential Summary Card -->
        <div class="p-3 rounded-lg bg-surface-50 dark:bg-surface-800/60 border border-surface-200 dark:border-surface-700 space-y-1">
          <div class="font-semibold text-sm text-surface-900 dark:text-surface-100">
            {{ evidenceToVerify?.title }}
          </div>
          <div class="text-surface-500">
            {{ evidenceToVerify?.issuingAuthority }}
            <span v-if="evidenceToVerify?.credentialIdentifier"> • ID: {{ evidenceToVerify.credentialIdentifier }}</span>
          </div>
          <div class="text-surface-500">
            Issued: {{ formatDisplayDate(evidenceToVerify?.issueDate) }}
            <span v-if="evidenceToVerify?.expiryDate"> • Exp: {{ formatDisplayDate(evidenceToVerify.expiryDate) }}</span>
          </div>
        </div>

        <!-- TD-05 Architectural Principle Notice -->
        <div
          class="p-3 rounded-lg bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800 text-sky-800 dark:text-sky-300 flex items-start gap-2.5 leading-relaxed"
          data-testid="verify-disclaimer-banner"
        >
          <Info class="w-4 h-4 shrink-0 mt-0.5 text-sky-600 dark:text-sky-400" />
          <span>
            <strong>Authenticity Validation (TD-05):</strong> Verifying this credential records that the documentary evidence has been authenticated. It does not automatically grant authorization to teach (which requires formal Teaching Authority decree).
          </span>
        </div>

        <!-- Verification Notes Textarea -->
        <FormField
          label="Verification Remarks / Audit Notes (Optional)"
          hint="Record registration check details or observations (max 4000 characters)"
        >
          <Textarea
            v-model="verifyNotes"
            rows="3"
            auto-resize
            placeholder="e.g. Scanned copy verified against national registry database..."
            class="w-full text-xs"
            data-testid="textarea-verification-notes"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="verifyEvidenceMutation.isPending.value"
            data-testid="cancel-verify-btn"
            @click="isVerifyDialogOpen = false"
          />
          <Button
            type="button"
            label="Confirm Verification"
            icon="pi pi-check"
            severity="success"
            size="small"
            :loading="verifyEvidenceMutation.isPending.value"
            data-testid="confirm-verify-btn"
            @click="handleConfirmVerify"
          />
        </div>
      </template>
    </Dialog>

    <!-- Role-Gated Reject Dialog -->
    <Dialog
      v-model:visible="isRejectDialogOpen"
      modal
      :draggable="false"
      class="w-full max-w-lg mx-4"
      data-testid="reject-evidence-dialog"
    >
      <template #header>
        <div class="flex items-center gap-2 text-rose-600 dark:text-rose-400">
          <AlertTriangle class="w-5 h-5" />
          <h2 class="text-base font-semibold">
            Reject Qualification Evidence
          </h2>
        </div>
      </template>

      <div class="space-y-4 pt-1 text-xs">
        <!-- Target Credential Summary -->
        <div class="p-3 rounded-lg bg-surface-50 dark:bg-surface-800/60 border border-surface-200 dark:border-surface-700 space-y-1">
          <div class="font-semibold text-sm text-surface-900 dark:text-surface-100">
            {{ evidenceToReject?.title }}
          </div>
          <div class="text-surface-500">
            {{ evidenceToReject?.issuingAuthority }}
          </div>
        </div>

        <p class="text-surface-600 dark:text-surface-300">
          Rejecting credential evidence flags the submission as invalid or unverified. A mandatory justification rationale must be recorded for audit traceability.
        </p>

        <!-- Rejection Reason Form Field -->
        <FormField
          label="Rejection Reason"
          required
          :error="rejectionError"
          hint="Mandatory justification for rejecting this credential submission (max 4000 characters)"
        >
          <Textarea
            v-model="rejectionReason"
            rows="3"
            auto-resize
            placeholder="e.g. Credential registration number does not match Kemendikbud database records..."
            class="w-full text-xs"
            :invalid="Boolean(rejectionError)"
            data-testid="textarea-rejection-reason"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="rejectEvidenceMutation.isPending.value"
            data-testid="cancel-reject-btn"
            @click="isRejectDialogOpen = false"
          />
          <Button
            type="button"
            label="Reject Credential"
            icon="pi pi-times"
            severity="danger"
            size="small"
            :loading="rejectEvidenceMutation.isPending.value"
            data-testid="confirm-reject-btn"
            @click="handleConfirmReject"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
