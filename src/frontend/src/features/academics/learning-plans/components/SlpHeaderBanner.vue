<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  Calendar,
  Clock,
  Layers,
  Award,
  Send,
  ShieldCheck,
  GitFork,
  RefreshCw,
  Archive,
  GraduationCap,
  Building2,
  Users,
  User,
} from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { useStudentDossierQuery } from '@/features/students/records/composables/useStudentRecordsQueries';
import {
  LEARNING_PLAN_PERMISSIONS,
  LEARNING_PLAN_ROLES,
} from '../constants/permissions';
import type {
  StudentLearningPlan,
  StudentLearningPlanStatus,
  SlpApproval,
  SyncParticipationsResponse,
} from '../types';
import SubmitPlanModal from './SubmitPlanModal.vue';
import ApprovePlanModal from './ApprovePlanModal.vue';
import RevisePlanModal from './RevisePlanModal.vue';
import SyncParticipationsModal from './SyncParticipationsModal.vue';
import ArchivePlanModal from './ArchivePlanModal.vue';

const props = withDefaults(
  defineProps<{
    plan: StudentLearningPlan;
    gradeLevel?: string | null;
    campus?: string | null;
    cohort?: string | null;
    homeroom?: string | null;
  }>(),
  {
    gradeLevel: null,
    campus: null,
    cohort: null,
    homeroom: null,
  },
);

const emit = defineEmits<{
  (e: 'planSubmitted'): void;
  (e: 'planApproved', approval: SlpApproval): void;
  (e: 'planRevised', newPlanId: string): void;
  (e: 'participationsSynced', response: SyncParticipationsResponse): void;
  (e: 'planArchived'): void;
}>();

// ---------------------------------------------------------------------------
// 1. Authorization & Role-Based Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const isCoordinator = computed(() => {
  return (
    authStore.hasPermission(LEARNING_PLAN_PERMISSIONS.MANAGE) ||
    authStore.hasPermission(LEARNING_PLAN_PERMISSIONS.REVISE) ||
    authStore.hasRole(LEARNING_PLAN_ROLES.COORDINATOR)
  );
});

const isAuthority = computed(() => {
  return (
    authStore.hasPermission(LEARNING_PLAN_PERMISSIONS.APPROVE) ||
    authStore.hasRole(LEARNING_PLAN_ROLES.AUTHORITY)
  );
});

// Action Visibility & Enabled States
const showSubmit = computed(() => props.plan.status === 'draft');
const canSubmit = computed(() => isCoordinator.value);

const showApprove = computed(() => props.plan.status === 'draft');
const canApprove = computed(() => isAuthority.value);

const showRevise = computed(() => props.plan.status === 'active');
const canRevise = computed(() => isCoordinator.value);

const showSync = computed(() => props.plan.status === 'active');
const canSync = computed(() => isCoordinator.value);

const showArchive = computed(
  () => props.plan.status === 'active' || props.plan.status === 'revised',
);
const canArchive = computed(() => isAuthority.value);

// ---------------------------------------------------------------------------
// 2. Student 360° Dossier Query for Placement Context
// ---------------------------------------------------------------------------
const studentIdRef = computed(() => props.plan?.studentId);
const { data: studentDossier } = useStudentDossierQuery(studentIdRef, {
  enabled: computed(() => Boolean(props.plan?.studentId)),
});

// Contextual Placement Projections
const resolvedGradeLevel = computed(() => {
  return (
    props.gradeLevel ||
    studentDossier.value?.currentPlacement?.grade ||
    (props.plan as unknown as { gradeLevel?: string }).gradeLevel ||
    null
  );
});

const resolvedCampus = computed(() => {
  return (
    props.campus ||
    studentDossier.value?.currentPlacement?.campus ||
    null
  );
});

const resolvedCohortHomeroom = computed(() => {
  const placement = studentDossier.value?.currentPlacement;
  if (props.homeroom || props.cohort) {
    return [props.homeroom, props.cohort].filter(Boolean).join(' • ');
  }
  if (placement) {
    const parts = [placement.homeroom, placement.cohort].filter(Boolean);
    return parts.length > 0 ? parts.join(' • ') : null;
  }
  return null;
});

// ---------------------------------------------------------------------------
// 3. Status Badge Helpers
// ---------------------------------------------------------------------------
function getStatusTagSeverity(
  status: StudentLearningPlanStatus,
): 'success' | 'warn' | 'info' | 'contrast' | 'secondary' {
  switch (status) {
    case 'active':
      return 'success';
    case 'draft':
      return 'warn';
    case 'revised':
      return 'info';
    case 'completed':
      return 'contrast';
    case 'archived':
    default:
      return 'secondary';
  }
}

function getStatusLabel(status: StudentLearningPlanStatus): string {
  switch (status) {
    case 'active':
      return 'Active';
    case 'draft':
      return 'Draft';
    case 'revised':
      return 'Revised';
    case 'completed':
      return 'Completed';
    case 'archived':
      return 'Archived';
    default:
      return status;
  }
}

// ---------------------------------------------------------------------------
// 4. Action Modals State
// ---------------------------------------------------------------------------
const isSubmitModalOpen = ref(false);
const isApproveModalOpen = ref(false);
const isReviseModalOpen = ref(false);
const isSyncModalOpen = ref(false);
const isArchiveModalOpen = ref(false);

function handlePlanSubmitted() {
  emit('planSubmitted');
}

function handlePlanApproved(approval: SlpApproval) {
  emit('planApproved', approval);
}

function handlePlanRevised(newPlanId: string) {
  emit('planRevised', newPlanId);
}

function handleParticipationsSynced(response: SyncParticipationsResponse) {
  emit('participationsSynced', response);
}

function handlePlanArchived() {
  emit('planArchived');
}
</script>

<template>
  <div
    id="slp-header-banner"
    class="bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 p-5 md:p-6 shadow-2xs space-y-4"
  >
    <!-- Top Row: Student Profile, Status & Role-Based Actions -->
    <div class="flex flex-col xl:flex-row xl:items-center justify-between gap-4">
      <!-- Student Context & Plan Lifecycle Info -->
      <div class="flex items-start gap-4 min-w-0">
        <!-- Student Avatar Circle -->
        <div
          class="w-14 h-14 rounded-2xl bg-indigo-600 text-white flex items-center justify-center font-bold text-xl shadow-sm flex-shrink-0"
        >
          {{ (plan.studentName || 'S').charAt(0).toUpperCase() }}
        </div>

        <div class="space-y-1.5 min-w-0">
          <!-- Student Full Name & Status Badge -->
          <div class="flex items-center gap-2.5 flex-wrap">
            <h2 class="text-xl font-bold text-slate-900 dark:text-slate-100 truncate">
              {{ plan.studentName || 'Student Name' }}
            </h2>

            <!-- Plan Version Pill -->
            <span
              class="px-2 py-0.5 rounded-md text-xs font-bold font-mono bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700"
            >
              v{{ plan.version }}
            </span>

            <!-- PrimeVue Status Badge -->
            <Tag
              :value="getStatusLabel(plan.status)"
              :severity="getStatusTagSeverity(plan.status)"
              class="text-xs font-bold px-2.5 py-0.5"
            />
          </div>

          <!-- Administrative Placement & Academic Context Context Bar -->
          <div class="flex items-center gap-2.5 text-xs text-slate-500 dark:text-slate-400 flex-wrap">
            <span class="font-medium text-slate-700 dark:text-slate-300">
              NIS: {{ plan.studentNis || '—' }}
            </span>

            <template v-if="resolvedGradeLevel">
              <span>•</span>
              <span class="inline-flex items-center gap-1">
                <GraduationCap class="w-3.5 h-3.5 text-slate-400" />
                <span>{{ resolvedGradeLevel }}</span>
              </span>
            </template>

            <template v-if="resolvedCampus">
              <span>•</span>
              <span class="inline-flex items-center gap-1">
                <Building2 class="w-3.5 h-3.5 text-slate-400" />
                <span>{{ resolvedCampus }}</span>
              </span>
            </template>

            <template v-if="resolvedCohortHomeroom">
              <span>•</span>
              <span class="inline-flex items-center gap-1">
                <Users class="w-3.5 h-3.5 text-slate-400" />
                <span>{{ resolvedCohortHomeroom }}</span>
              </span>
            </template>

            <span>•</span>
            <span class="inline-flex items-center gap-1">
              <Calendar class="w-3.5 h-3.5 text-slate-400" />
              <span>{{ plan.academicPeriodLabel }}</span>
            </span>

            <span>•</span>
            <span class="inline-flex items-center gap-1">
              <Clock class="w-3.5 h-3.5 text-slate-400" />
              <span>Updated {{ formatDisplayDate(plan.updatedAt || plan.createdAt) }}</span>
            </span>
          </div>

          <!-- Plan Notes / Formulation Rationale (if any) -->
          <div v-if="plan.notes" class="text-xs text-slate-600 dark:text-slate-400 pt-0.5 italic max-w-3xl line-clamp-2">
            "{{ plan.notes }}"
          </div>
        </div>
      </div>

      <!-- Action Buttons Bar & Counts Summary -->
      <div class="flex flex-col sm:flex-row items-start sm:items-center gap-3 self-start xl:self-center">
        <!-- Quick Summary Metrics -->
        <div class="flex items-center gap-2">
          <div class="px-3 py-1.5 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs font-medium text-slate-700 dark:text-slate-300">
            <span class="text-slate-400 mr-1.5">Constituents:</span>
            <span class="font-bold text-indigo-600 dark:text-indigo-400">{{ plan.constituentUnits?.length || 0 }} Units</span>
          </div>
          <div class="px-3 py-1.5 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs font-medium text-slate-700 dark:text-slate-300">
            <span class="text-slate-400 mr-1.5">Adjustments:</span>
            <span class="font-bold text-purple-600 dark:text-purple-400">{{ plan.adjustments?.length || 0 }} Active</span>
          </div>
        </div>

        <!-- Role-Based Action Buttons Group -->
        <div class="flex items-center gap-2 flex-wrap">
          <!-- 1. Submit for Review (visible when draft, enabled for Academic Coordinator) -->
          <Button
            v-if="showSubmit"
            label="Submit for Review"
            icon="pi pi-send"
            severity="primary"
            size="small"
            class="text-xs font-semibold shadow-xs"
            :disabled="!canSubmit"
            @click="isSubmitModalOpen = true"
          />

          <!-- 2. Approve Plan (visible when draft (submitted), enabled for Academic Authority) -->
          <Button
            v-if="showApprove"
            label="Approve Plan"
            icon="pi pi-check-circle"
            severity="success"
            size="small"
            class="text-xs font-semibold shadow-xs"
            :disabled="!canApprove"
            @click="isApproveModalOpen = true"
          />

          <!-- 3. Revise Plan (v+1) (visible when active, enabled for Academic Coordinator) -->
          <Button
            v-if="showRevise"
            label="Revise Plan (v+1)"
            icon="pi pi-copy"
            severity="primary"
            size="small"
            class="text-xs font-semibold shadow-xs"
            :disabled="!canRevise"
            @click="isReviseModalOpen = true"
          />

          <!-- 4. Sync Participations (visible when active, enabled for Academic Coordinator) -->
          <Button
            v-if="showSync"
            label="Sync Participations"
            icon="pi pi-sync"
            severity="secondary"
            size="small"
            class="text-xs font-semibold shadow-xs"
            :disabled="!canSync"
            @click="isSyncModalOpen = true"
          />

          <!-- 5. Archive Plan (visible when active or revised, enabled for Academic Authority) -->
          <Button
            v-if="showArchive"
            label="Archive Plan"
            icon="pi pi-box"
            severity="danger"
            variant="outlined"
            size="small"
            class="text-xs font-semibold shadow-xs"
            :disabled="!canArchive"
            @click="isArchiveModalOpen = true"
          />
        </div>
      </div>
    </div>

    <!-- Action Modals -->
    <SubmitPlanModal
      v-model:visible="isSubmitModalOpen"
      :plan="plan"
      @submitted="handlePlanSubmitted"
    />

    <ApprovePlanModal
      v-model:visible="isApproveModalOpen"
      :plan="plan"
      @approved="handlePlanApproved"
    />

    <RevisePlanModal
      v-model:visible="isReviseModalOpen"
      :plan="plan"
      @revised="handlePlanRevised"
    />

    <SyncParticipationsModal
      v-model:visible="isSyncModalOpen"
      :plan="plan"
      @synced="handleParticipationsSynced"
    />

    <ArchivePlanModal
      v-model:visible="isArchiveModalOpen"
      :plan="plan"
      @archived="handlePlanArchived"
    />
  </div>
</template>
