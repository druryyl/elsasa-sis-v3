<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import {
  ShieldCheck,
  ShieldAlert,
  Plus,
  AlertTriangle,
  Users,
  Award,
  CheckCircle2,
  Calendar,
  Building2,
  FileText,
  Clock,
  Sparkles,
  ExternalLink,
} from 'lucide-vue-next';
import { EmptyState, StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import GrantAuthorizationModal from './GrantAuthorizationModal.vue';
import AddSupervisionModal from './AddSupervisionModal.vue';
import ResolveSupervisionModal from './ResolveSupervisionModal.vue';
import ChangeAuthorizationStatusModal from './ChangeAuthorizationStatusModal.vue';
import type {
  TeacherProfileDto,
  TeachingAuthorizationDto,
  SupervisionRequirementDto,
  GrantTeachingAuthorizationResponse,
  AddSupervisionRequirementResponse,
  ResolveSupervisionRequirementResponse,
  UpdateTeachingAuthorizationStatusResponse,
} from '../api/teachingTypes';

export interface AuthorizationsSupervisionTabProps {
  teacher: TeacherProfileDto | null;
  canAuthorize?: boolean;
  canManage?: boolean;
}

const props = withDefaults(defineProps<AuthorizationsSupervisionTabProps>(), {
  teacher: null,
  canAuthorize: false,
  canManage: false,
});

const emit = defineEmits<{
  (e: 'authorizationGranted', response: GrantTeachingAuthorizationResponse): void;
  (e: 'supervisionAdded', response: AddSupervisionRequirementResponse): void;
  (e: 'supervisionResolved', response: ResolveSupervisionRequirementResponse): void;
  (e: 'statusUpdated', response: UpdateTeachingAuthorizationStatusResponse): void;
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

const canManageAction = computed(() => {
  return (
    props.canManage ||
    authStore.hasPermission(TEACHING_PERMISSIONS.MANAGE) ||
    canAuthorizeAction.value
  );
});

// ---------------------------------------------------------------------------
// 2. Computed Authorizations & Lookup
// ---------------------------------------------------------------------------
const authorizationsList = computed<TeachingAuthorizationDto[]>(() => {
  return props.teacher?.authorizations ?? [];
});

const credentialsMap = computed(() => {
  const map = new Map<string, { title: string; verificationState: string }>();
  if (props.teacher?.qualifications) {
    for (const q of props.teacher.qualifications) {
      map.set(q.id, {
        title: q.title,
        verificationState: q.verificationState,
      });
    }
  }
  return map;
});

// ---------------------------------------------------------------------------
// 3. Modal States & Handlers
// ---------------------------------------------------------------------------
// Grant Authorization Modal
const isGrantModalOpen = ref(false);

function handleOpenGrantModal() {
  isGrantModalOpen.value = true;
}

function handleGranted(response: GrantTeachingAuthorizationResponse) {
  emit('authorizationGranted', response);
  emit('refresh');
}

// Add Supervision Modal
const isAddSupervisionModalOpen = ref(false);
const activeAuthorizationForSupervision = ref<TeachingAuthorizationDto | null>(null);

function handleOpenAddSupervision(authItem: TeachingAuthorizationDto) {
  activeAuthorizationForSupervision.value = authItem;
  isAddSupervisionModalOpen.value = true;
}

function handleSupervisionAdded(response: AddSupervisionRequirementResponse) {
  emit('supervisionAdded', response);
  emit('refresh');
}

// Resolve Supervision Modal
const isResolveSupervisionModalOpen = ref(false);
const activeSupervisionForResolve = ref<SupervisionRequirementDto | null>(null);
const activeAuthNumberForResolve = ref('');

function handleOpenResolveSupervision(
  supervision: SupervisionRequirementDto,
  authorizationNumber: string,
) {
  activeSupervisionForResolve.value = supervision;
  activeAuthNumberForResolve.value = authorizationNumber;
  isResolveSupervisionModalOpen.value = true;
}

function handleSupervisionResolved(response: ResolveSupervisionRequirementResponse) {
  emit('supervisionResolved', response);
  emit('refresh');
}

// Change Status Modal
const isChangeStatusModalOpen = ref(false);
const activeAuthorizationForStatus = ref<TeachingAuthorizationDto | null>(null);

function handleOpenChangeStatus(authItem: TeachingAuthorizationDto) {
  activeAuthorizationForStatus.value = authItem;
  isChangeStatusModalOpen.value = true;
}

function handleStatusChanged(response: UpdateTeachingAuthorizationStatusResponse) {
  emit('statusUpdated', response);
  emit('refresh');
}

function formatDeliveryModeLabel(mode?: string | null): string {
  if (!mode) return 'All Formats';
  switch (mode) {
    case 'in_person':
      return 'Tatap Muka (In-Person)';
    case 'hybrid':
      return 'Hybrid / Blended';
    case 'online':
      return 'Online / Distance';
    case 'field_practicum':
      return 'Field Practicum';
    default:
      return mode;
  }
}

function formatSupervisionTypeLabel(type?: string | null): string {
  if (!type) return 'Supervision';
  switch (type) {
    case 'co_teaching':
      return 'Co-Teaching';
    case 'periodic_observation':
      return 'Periodic Observation';
    case 'lesson_plan_review':
      return 'Lesson Plan Review';
    case 'general_mentorship':
      return 'General Mentorship';
    default:
      return type;
  }
}
</script>

<template>
  <div class="space-y-4" data-testid="authorizations-supervision-tab">
    <!-- Header Banner & Action Bar -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 bg-surface-50 dark:bg-surface-800/40 rounded-xl border border-surface-200 dark:border-surface-700">
      <div class="space-y-0.5">
        <div class="flex items-center gap-2">
          <ShieldCheck class="w-5 h-5 text-primary-600 dark:text-primary-400" />
          <h3 class="text-sm font-bold text-surface-900 dark:text-surface-100">
            Authoritative Permissions to Teach (TD-05)
          </h3>
        </div>
        <p class="text-xs text-surface-500">
          Formal institutional permissions granting faculty authorization to deliver coursework within defined scopes, campuses, and term dates.
        </p>
      </div>

      <Button
        v-if="canAuthorizeAction"
        label="Grant Authorization"
        icon="pi pi-plus"
        severity="primary"
        size="small"
        class="!text-xs shrink-0 !py-1.5 !px-3 font-medium"
        data-testid="grant-authorization-button"
        @click="handleOpenGrantModal"
      />
    </div>

    <!-- Empty State -->
    <div
      v-if="!teacher || authorizationsList.length === 0"
      class="p-8 text-center"
      data-testid="empty-authorizations"
    >
      <EmptyState
        title="No active teaching authorizations"
        description="Teaching Authorizations establish legal permission for faculty to deliver specific subjects, stages, and campuses. Grant an authorization to enable course scheduling."
        bordered
      >
        <template #actions>
          <Button
            v-if="canAuthorizeAction"
            label="Grant Teaching Authorization"
            icon="pi pi-plus"
            severity="primary"
            size="small"
            class="!text-xs !py-1.5 !px-3 font-medium"
            data-testid="empty-grant-button"
            @click="handleOpenGrantModal"
          />
        </template>
      </EmptyState>
    </div>

    <!-- Authorizations List -->
    <div v-else class="space-y-4" data-testid="authorizations-list">
      <div
        v-for="authItem in authorizationsList"
        :key="authItem.id"
        class="p-4 bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs space-y-3 transition-colors"
        :data-testid="`authorization-card-${authItem.id}`"
      >
        <!-- Top Row: Auth Number, Status Badge, Warning Pill, Effective Dates & Actions -->
        <div class="flex flex-wrap items-center justify-between gap-2.5 pb-2 border-b border-surface-100 dark:border-surface-800">
          <div class="flex flex-wrap items-center gap-2">
            <span class="font-mono font-bold text-xs text-surface-900 dark:text-surface-100 bg-surface-100 dark:bg-surface-800 px-2 py-0.5 rounded">
              {{ authItem.authorizationNumber }}
            </span>

            <StatusPill
              :status="authItem.status"
              size="sm"
              :variant="
                authItem.status === 'active'
                  ? 'emerald'
                  : authItem.status === 'authorized_under_supervision'
                    ? 'amber'
                    : authItem.status === 'suspended' || authItem.status === 'revoked'
                      ? 'rose'
                      : 'slate'
              "
            />

            <!-- TD-05 Permissive Warning Badge -->
            <span
              v-if="authItem.unverifiedCredentialWarning"
              class="inline-flex items-center gap-1 text-[11px] font-semibold text-amber-700 dark:text-amber-300 bg-amber-100/70 dark:bg-amber-950/80 px-2 py-0.5 rounded border border-amber-300 dark:border-amber-800"
              data-testid="auth-unverified-warning"
              title="Permissive Guidance Gate: unverified credentials linked"
            >
              <AlertTriangle class="w-3.5 h-3.5 text-amber-600 dark:text-amber-400" />
              <span>Unverified Credential</span>
            </span>
          </div>

          <div class="flex items-center gap-2">
            <div class="text-[11px] text-surface-500 flex items-center gap-1">
              <Calendar class="w-3.5 h-3.5" />
              <span>Effective: {{ formatDisplayDate(authItem.effectiveStartDate) }}</span>
              <span v-if="authItem.effectiveEndDate"> to {{ formatDisplayDate(authItem.effectiveEndDate) }}</span>
              <span v-else> (Permanent)</span>
            </div>

            <!-- Action Buttons -->
            <div class="flex items-center gap-1.5 ml-2">
              <Button
                v-if="canManageAction"
                label="+ Add Supervision"
                severity="secondary"
                variant="outlined"
                size="small"
                class="!text-[11px] !py-1 !px-2 font-medium"
                :data-testid="`add-supervision-btn-${authItem.id}`"
                @click="handleOpenAddSupervision(authItem)"
              />

              <Button
                v-if="canAuthorizeAction"
                label="Change Status"
                severity="secondary"
                variant="outlined"
                size="small"
                class="!text-[11px] !py-1 !px-2 font-medium"
                :data-testid="`change-status-btn-${authItem.id}`"
                @click="handleOpenChangeStatus(authItem)"
              />
            </div>
          </div>
        </div>

        <!-- Scope Description -->
        <div>
          <h4 class="text-xs font-semibold text-surface-900 dark:text-surface-100 leading-snug">
            {{ authItem.scopeDescription }}
          </h4>
        </div>

        <!-- Scope Boundaries Tags -->
        <div class="flex flex-wrap items-center gap-1.5 text-[11px]">
          <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300">
            <Building2 class="w-3 h-3 text-surface-400" />
            Campus: <strong>{{ authItem.campusScope }}</strong>
          </span>

          <span
            v-if="authItem.programScope"
            class="px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300"
          >
            Program: <strong>{{ authItem.programScope }}</strong>
          </span>

          <span
            v-if="authItem.levelScope"
            class="px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300"
          >
            Level: <strong>{{ authItem.levelScope }}</strong>
          </span>

          <span
            v-if="authItem.deliveryModeScope"
            class="px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300"
          >
            Mode: <strong>{{ formatDeliveryModeLabel(authItem.deliveryModeScope) }}</strong>
          </span>

          <span class="text-surface-400 ml-auto text-[10px]">
            Approved by <strong>{{ authItem.decisionAuthority }}</strong> on {{ formatDisplayDate(authItem.approvedDate) }}
          </span>
        </div>

        <!-- Conditions / Restrictions if present -->
        <div
          v-if="authItem.conditionsOrRestrictions"
          class="p-2 rounded bg-amber-50/40 dark:bg-amber-950/20 border border-amber-200/50 dark:border-amber-800/40 text-[11px] text-amber-800 dark:text-amber-300"
        >
          <span class="font-semibold">Conditions/Restrictions:</span> {{ authItem.conditionsOrRestrictions }}
        </div>

        <!-- Suspension or Revocation reason if present -->
        <div
          v-if="authItem.suspensionOrRevocationReason"
          class="p-2 rounded bg-rose-50/50 dark:bg-rose-950/20 border border-rose-200/60 dark:border-rose-900/40 text-[11px] text-rose-700 dark:text-rose-300"
          data-testid="auth-revocation-reason"
        >
          <span class="font-semibold">Reason for {{ authItem.status }}:</span> {{ authItem.suspensionOrRevocationReason }}
        </div>

        <!-- Linked Supporting Credentials (TD-05 Traceability) -->
        <div
          v-if="authItem.linkedEvidenceIds && authItem.linkedEvidenceIds.length > 0"
          class="pt-2 border-t border-surface-100 dark:border-surface-800 space-y-1.5"
          data-testid="linked-evidence-section"
        >
          <span class="text-[10px] font-semibold uppercase tracking-wider text-surface-400 flex items-center gap-1">
            <Award class="w-3 h-3" />
            <span>Supporting Qualification Evidence ({{ authItem.linkedEvidenceIds.length }})</span>
          </span>

          <div class="flex flex-wrap gap-1.5">
            <div
              v-for="evId in authItem.linkedEvidenceIds"
              :key="evId"
              class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md text-[11px] bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700"
              :data-testid="`linked-evidence-pill-${evId}`"
            >
              <Award class="w-3 h-3 text-primary-500" />
              <span>{{ credentialsMap.get(evId)?.title || `Evidence ${evId.substring(0, 8)}...` }}</span>
              <StatusPill
                v-if="credentialsMap.get(evId)"
                :status="credentialsMap.get(evId)!.verificationState"
                size="sm"
                :variant="credentialsMap.get(evId)!.verificationState === 'verified' ? 'emerald' : 'amber'"
              />
            </div>
          </div>
        </div>

        <!-- Supervision Requirements Sub-Section -->
        <div
          v-if="authItem.supervisionRequirements && authItem.supervisionRequirements.length > 0"
          class="mt-2 pt-2 border-t border-surface-200 dark:border-surface-700 space-y-2"
          data-testid="supervision-requirements-section"
        >
          <div class="flex items-center justify-between">
            <span class="text-[10px] font-semibold uppercase tracking-wider text-amber-700 dark:text-amber-400 flex items-center gap-1.5">
              <Users class="w-3.5 h-3.5" />
              <span>Mentorship & Supervision Conditions ({{ authItem.supervisionRequirements.length }})</span>
            </span>
          </div>

          <div class="space-y-2">
            <div
              v-for="sup in authItem.supervisionRequirements"
              :key="sup.id"
              class="p-3 bg-amber-50/40 dark:bg-amber-950/20 border border-amber-200/80 dark:border-amber-800/60 rounded-lg text-xs space-y-1.5"
              :data-testid="`supervision-card-${sup.id}`"
            >
              <div class="flex flex-wrap items-center justify-between gap-2">
                <div class="flex items-center gap-2">
                  <span class="font-semibold text-amber-900 dark:text-amber-200">
                    {{ formatSupervisionTypeLabel(sup.supervisionType) }}
                  </span>
                  <StatusPill
                    :status="sup.status"
                    size="sm"
                    :variant="
                      sup.status === 'completed'
                        ? 'emerald'
                        : sup.status === 'active'
                          ? 'amber'
                          : sup.status === 'escalated'
                            ? 'rose'
                            : 'slate'
                    "
                  />
                </div>

                <div class="flex items-center gap-2 text-[11px] text-surface-500">
                  <span>{{ formatDisplayDate(sup.startDate) }}</span>
                  <span v-if="sup.endDate"> to {{ formatDisplayDate(sup.endDate) }}</span>

                  <Button
                    v-if="sup.status === 'active' && canManageAction"
                    label="Resolve Supervision"
                    severity="success"
                    size="small"
                    class="!text-[10px] !py-0.5 !px-2 font-medium"
                    :data-testid="`resolve-supervision-btn-${sup.id}`"
                    @click="handleOpenResolveSupervision(sup, authItem.authorizationNumber)"
                  />
                </div>
              </div>

              <p class="text-surface-700 dark:text-surface-300 text-[11px]">
                {{ sup.description }}
              </p>

              <div
                v-if="sup.supervisingTeacherName || sup.supervisingTeacherId"
                class="text-[11px] text-surface-500 flex items-center gap-1"
              >
                <Users class="w-3 h-3 text-surface-400" />
                <span>Mentor: <strong>{{ sup.supervisingTeacherName || sup.supervisingTeacherId }}</strong></span>
              </div>

              <!-- Resolution Notes if completed -->
              <div
                v-if="sup.resolutionNotes"
                class="mt-1 pt-1 border-t border-amber-200/50 dark:border-amber-800/40 text-[11px] text-emerald-800 dark:text-emerald-300 flex items-start gap-1"
                data-testid="supervision-resolution-notes"
              >
                <CheckCircle2 class="w-3.5 h-3.5 shrink-0 mt-0.5 text-emerald-600" />
                <span><strong>Resolution:</strong> {{ sup.resolutionNotes }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <GrantAuthorizationModal
      v-if="teacher"
      v-model:visible="isGrantModalOpen"
      :teacher="teacher"
      @granted="handleGranted"
    />

    <AddSupervisionModal
      v-if="activeAuthorizationForSupervision"
      v-model:visible="isAddSupervisionModalOpen"
      :authorization-id="activeAuthorizationForSupervision.id"
      :authorization-number="activeAuthorizationForSupervision.authorizationNumber"
      :teacher-id="teacher?.id"
      @added="handleSupervisionAdded"
    />

    <ResolveSupervisionModal
      v-if="activeSupervisionForResolve"
      v-model:visible="isResolveSupervisionModalOpen"
      :supervision-id="activeSupervisionForResolve.id"
      :authorization-number="activeAuthNumberForResolve"
      :supervision-description="activeSupervisionForResolve.description"
      :teacher-id="teacher?.id"
      @resolved="handleSupervisionResolved"
    />

    <ChangeAuthorizationStatusModal
      v-if="activeAuthorizationForStatus"
      v-model:visible="isChangeStatusModalOpen"
      :authorization="activeAuthorizationForStatus"
      :teacher-id="teacher?.id"
      @changed="handleStatusChanged"
    />
  </div>
</template>
