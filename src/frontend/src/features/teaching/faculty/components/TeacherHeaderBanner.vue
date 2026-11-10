<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import {
  Building2,
  Mail,
  Phone,
  AlertTriangle,
  UserCheck,
  ShieldAlert,
  FileEdit,
  User,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import type { StatusVariant } from '@/shared/components/StatusPill.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { useApproveTeacherMutation } from '../composables/useFacultyMutations';
import type { TeacherProfileDto, TeacherStanding } from '../api/teachingTypes';

export interface TeacherHeaderBannerProps {
  teacher: TeacherProfileDto;
  /** Explicit unverified warning flag (TD-05). Falls back to inspection of authorizations. */
  hasUnverifiedWarning?: boolean;
  /** Snake_case alias matching completion criteria. */
  has_unverified_warning?: boolean;
  /** Optional authority permission override (e.g. For unit testing) */
  canAuthorize?: boolean;
  /** Optional correction permission override */
  canCorrect?: boolean;
  /** Optional loading state override */
  isApproving?: boolean;
}

const props = withDefaults(defineProps<TeacherHeaderBannerProps>(), {
  hasUnverifiedWarning: undefined,
  has_unverified_warning: undefined,
  canAuthorize: undefined,
  canCorrect: undefined,
  isApproving: undefined,
});

const emit = defineEmits<{
  (e: 'approve', teacher: TeacherProfileDto): void;
  (e: 'change-standing', teacher: TeacherProfileDto): void;
  (e: 'changeStanding', teacher: TeacherProfileDto): void;
  (e: 'audit-correction', teacher: TeacherProfileDto): void;
  (e: 'auditCorrection', teacher: TeacherProfileDto): void;
  (e: 'record-correction', teacher: TeacherProfileDto): void;
}>();

// ---------------------------------------------------------------------------
// 1. Permissions & Authority Checks
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const isAuthorityAuthorized = computed(() => {
  if (props.canAuthorize !== undefined) {
    return props.canAuthorize;
  }
  return authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE);
});

const isCorrectionAllowed = computed(() => {
  if (props.canCorrect !== undefined) {
    return props.canCorrect;
  }
  return (
    authStore.hasPermission(TEACHING_PERMISSIONS.CORRECT) ||
    authStore.hasPermission(TEACHING_PERMISSIONS.MANAGE) ||
    authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE)
  );
});

// ---------------------------------------------------------------------------
// 2. Standing & Status Visual Mapping
// ---------------------------------------------------------------------------
function getStandingVariant(standing: TeacherStanding | string): StatusVariant {
  switch (standing) {
    case 'active':
      return 'emerald';
    case 'pending_approval':
      return 'amber';
    case 'restricted':
      return 'amber';
    case 'suspended':
      return 'rose';
    case 'inactive':
    default:
      return 'slate';
  }
}

function getStandingLabel(standing: TeacherStanding | string): string {
  switch (standing) {
    case 'active':
      return 'Active';
    case 'pending_approval':
      return 'Pending Approval';
    case 'restricted':
      return 'Restricted';
    case 'suspended':
      return 'Suspended';
    case 'inactive':
      return 'Inactive';
    default:
      return standing.replace(/_/g, ' ');
  }
}

// ---------------------------------------------------------------------------
// 3. Unverified Credential Warning Resolution (TD-05)
// ---------------------------------------------------------------------------
const showUnverifiedWarning = computed(() => {
  if (props.has_unverified_warning !== undefined) {
    return Boolean(props.has_unverified_warning);
  }
  if (props.hasUnverifiedWarning !== undefined) {
    return Boolean(props.hasUnverifiedWarning);
  }
  const rawTeacher = props.teacher as unknown as Record<string, unknown>;
  if (rawTeacher.has_unverified_warning !== undefined) {
    return Boolean(rawTeacher.has_unverified_warning);
  }
  if (rawTeacher.hasUnverifiedWarning !== undefined) {
    return Boolean(rawTeacher.hasUnverifiedWarning);
  }
  return Boolean(
    props.teacher.authorizations?.some((auth) => auth.unverifiedCredentialWarning),
  );
});

// ---------------------------------------------------------------------------
// 4. Approve Profile Mutation
// ---------------------------------------------------------------------------
const approveMutation = useApproveTeacherMutation();

const isApproveLoading = computed(() => {
  if (props.isApproving !== undefined) {
    return props.isApproving;
  }
  return approveMutation.isPending.value;
});

function handleApprove() {
  emit('approve', props.teacher);
  if (isAuthorityAuthorized.value && !isApproveLoading.value) {
    approveMutation.mutate(props.teacher.id);
  }
}

function handleChangeStanding() {
  emit('change-standing', props.teacher);
  emit('changeStanding', props.teacher);
}

function handleAuditCorrection() {
  emit('audit-correction', props.teacher);
  emit('auditCorrection', props.teacher);
  emit('record-correction', props.teacher);
}
</script>

<template>
  <section
    class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-5 shadow-xs space-y-4"
    data-testid="teacher-header-banner"
  >
    <!-- Top Row: Avatar, Identity, Badges & Primary Authority Actions -->
    <div class="flex flex-col lg:flex-row lg:items-start justify-between gap-4">
      <!-- Left: Identity Information -->
      <div class="flex items-start gap-3.5 min-w-0">
        <!-- Avatar Initial Badge -->
        <div
          class="w-12 h-12 rounded-xl bg-primary-100 dark:bg-primary-950/80 border border-primary-200 dark:border-primary-800 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-base shrink-0 shadow-xs"
          data-testid="header-avatar"
        >
          <User class="w-6 h-6" />
        </div>

        <div class="space-y-1 min-w-0">
          <!-- Numbers, Standing & Role Pills -->
          <div class="flex flex-wrap items-center gap-2">
            <span
              class="font-mono text-xs font-bold text-primary-700 dark:text-primary-300 bg-primary-50 dark:bg-primary-950 px-2 py-0.5 rounded border border-primary-200 dark:border-primary-800"
              data-testid="header-teacher-number"
            >
              {{ teacher.teacherNumber }}
            </span>

            <StatusPill
              :status="teacher.standing"
              :label="getStandingLabel(teacher.standing)"
              :variant="getStandingVariant(teacher.standing)"
              data-testid="header-standing-badge"
            />

            <span
              class="text-xs px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300 font-medium"
              data-testid="header-role-badge"
            >
              {{ teacher.teachingRoleName }}
            </span>
          </div>

          <!-- Professional Display Name -->
          <h1
            class="text-xl sm:text-2xl font-bold text-surface-900 dark:text-surface-100 truncate"
            data-testid="header-teacher-name"
          >
            {{ teacher.displayName || teacher.fullName }}
          </h1>

          <!-- Legal Full Name (shown if different from display name or for audit clarity) -->
          <div
            class="text-xs text-surface-500 italic"
            data-testid="header-legal-name"
          >
            <span class="font-normal not-italic text-surface-400">Legal Name:</span> {{ teacher.fullName }}
          </div>
        </div>
      </div>

      <!-- Right: Action Buttons Group (TD-07) -->
      <div
        class="flex flex-wrap items-center gap-2 lg:self-start shrink-0"
        data-testid="header-actions"
      >
        <!-- Action 1: Approve Profile (visible only when standing is pending_approval) -->
        <Button
          v-if="teacher.standing === 'pending_approval'"
          severity="success"
          size="small"
          :disabled="!isAuthorityAuthorized || isApproveLoading"
          :loading="isApproveLoading"
          class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
          data-testid="header-approve-button"
          :title="
            !isAuthorityAuthorized
              ? 'Requires Teaching Authority permission to approve profile'
              : 'Formally approve candidate profile into active standing'
          "
          @click="handleApprove"
        >
          <UserCheck class="w-4 h-4" />
          <span>Approve Profile</span>
        </Button>

        <!-- Action 2: Change Standing (opens ChangeStandingModal in P8-S25) -->
        <Button
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="!isAuthorityAuthorized"
          class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
          data-testid="header-change-standing-button"
          :title="
            !isAuthorityAuthorized
              ? 'Requires Teaching Authority permission to modify standing'
              : 'Modify teacher standing state (TD-03)'
          "
          @click="handleChangeStanding"
        >
          <ShieldAlert class="w-4 h-4 text-surface-600 dark:text-surface-400" />
          <span>Change Standing</span>
        </Button>

        <!-- Action 3: Audit Correction (opens RecordCorrectionModal in P8-S25) -->
        <Button
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="!isCorrectionAllowed"
          class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
          data-testid="header-audit-correction-button"
          :title="
            !isCorrectionAllowed
              ? 'Insufficient permissions to request or apply corrections'
              : 'Apply or request auditable record correction (TD-06)'
          "
          @click="handleAuditCorrection"
        >
          <FileEdit class="w-4 h-4 text-surface-600 dark:text-surface-400" />
          <span>Audit Correction</span>
        </Button>
      </div>
    </div>

    <!-- Middle Row: Affiliation Quick Meta (Campus, Department, Email, Phone) -->
    <div
      class="pt-2 border-t border-surface-100 dark:border-surface-800 flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-surface-600 dark:text-surface-400"
      data-testid="header-meta"
    >
      <!-- Campus Affiliation & Department -->
      <div class="flex items-center gap-1.5" data-testid="header-campus">
        <Building2 class="w-4 h-4 text-surface-400 shrink-0" />
        <span class="font-medium text-surface-800 dark:text-surface-200">
          {{ teacher.campusAffiliation }}
        </span>
        <span
          v-if="teacher.department"
          class="text-surface-500"
          data-testid="header-department"
        >
          • {{ teacher.department }}
        </span>
      </div>

      <!-- Institutional Email -->
      <div
        v-if="teacher.email"
        class="flex items-center gap-1.5 text-surface-500"
        data-testid="header-email"
      >
        <Mail class="w-3.5 h-3.5 text-surface-400 shrink-0" />
        <a
          :href="`mailto:${teacher.email}`"
          class="hover:underline text-surface-700 dark:text-surface-300 font-mono text-[11px]"
        >
          {{ teacher.email }}
        </a>
      </div>

      <!-- Phone Number -->
      <div
        v-if="teacher.phone"
        class="flex items-center gap-1.5 text-surface-500"
        data-testid="header-phone"
      >
        <Phone class="w-3.5 h-3.5 text-surface-400 shrink-0" />
        <span class="font-mono text-[11px]">{{ teacher.phone }}</span>
      </div>
    </div>

    <!-- Permissive Guidance Gate Alert Banner (TD-05) -->
    <div
      v-if="showUnverifiedWarning"
      class="flex items-start gap-3 p-3 bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 rounded-lg text-amber-900 dark:text-amber-200 text-xs"
      data-testid="unverified-warning-banner"
    >
      <AlertTriangle class="w-4 h-4 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
      <div>
        <span class="font-semibold block">Provisional Authorization Warning</span>
        <span>
          One or more teaching authorizations have been granted under provisional guidance with submitted qualification evidence pending formal verification.
        </span>
      </div>
    </div>
  </section>
</template>
