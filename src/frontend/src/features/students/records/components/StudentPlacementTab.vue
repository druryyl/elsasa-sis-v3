<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  Building2,
  Calendar,
  ArrowRightLeft,
  GraduationCap,
  History,
  ShieldCheck,
  FileText,
  UserCheck,
} from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { RECORDS_PERMISSIONS } from '../constants/permissions';
import type { StudentDetail, AdministrativePlacementDto } from '../types/records.types';

const props = withDefaults(
  defineProps<{
    student: StudentDetail;
    canEdit?: boolean;
  }>(),
  {
    canEdit: undefined,
  },
);

const emit = defineEmits<{
  (e: 'changePlacement'): void;
}>();

const authStore = useAuthStore();

const canEditPlacement = computed(() => {
  if (props.canEdit !== undefined) {
    return props.canEdit;
  }
  return authStore.hasPermission(RECORDS_PERMISSIONS.EDIT);
});

const currentPlacement = computed<AdministrativePlacementDto | null>(() => {
  if (props.student?.currentPlacement) {
    return props.student.currentPlacement;
  }
  if (props.student?.placementHistory && props.student.placementHistory.length > 0) {
    return props.student.placementHistory.find((p) => p.isCurrent) ?? null;
  }
  return null;
});

const priorPlacements = computed<AdministrativePlacementDto[]>(() => {
  const history = props.student?.placementHistory ?? [];
  return history.filter((p) => !p.isCurrent);
});

const hasPlacements = computed(() => {
  return Boolean(currentPlacement.value || priorPlacements.value.length > 0);
});
</script>

<template>
  <div class="space-y-6" data-testid="student-placement-tab">
    <!-- Header Context Description -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
      <p class="text-xs text-surface-500">
        Institutional administrative placement, grade progression, cohort allocation, and academic history.
      </p>
    </div>

    <!-- Empty State: No placements exist at all -->
    <div
      v-if="!hasPlacements"
      data-testid="empty-placement-state"
    >
      <EmptyState
        title="No administrative placement recorded"
        description="No organizational placement, campus, or grade level has been assigned to this student."
        size="sm"
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center mx-auto mb-2 text-surface-400">
            <Building2 class="w-6 h-6" />
          </div>
        </template>
        <template v-if="canEditPlacement" #actions>
          <Button
            size="small"
            severity="primary"
            data-testid="btn-change-placement"
            class="!text-xs cursor-pointer inline-flex items-center gap-1.5 mt-2"
            @click="emit('changePlacement')"
          >
            <ArrowRightLeft class="w-3.5 h-3.5" />
            <span>Assign Placement</span>
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- Active Placement & History Section -->
    <div v-else class="space-y-6">
      <!-- 1. Active Placement Card -->
      <div
        v-if="currentPlacement"
        class="p-5 rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-900 shadow-xs space-y-4"
        data-testid="active-placement-card"
      >
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-3 border-b border-surface-200/70 dark:border-surface-700/70">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-lg bg-teal-50 dark:bg-teal-950 text-teal-600 dark:text-teal-400 flex items-center justify-center shrink-0 border border-teal-200 dark:border-teal-800">
              <Building2 class="w-5 h-5" />
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h3 class="text-sm font-bold text-surface-900 dark:text-surface-100">
                  Current Active Placement
                </h3>
                <Tag severity="success" value="Active" class="!text-[10px] !px-2 !py-0.5" />
              </div>
              <p class="text-xs text-surface-500">
                Authoritative organizational and academic assignment
              </p>
            </div>
          </div>

          <!-- Change Placement Action -->
          <Button
            size="small"
            severity="primary"
            :disabled="!canEditPlacement"
            data-testid="btn-change-placement"
            class="!text-xs cursor-pointer inline-flex items-center gap-1.5 self-start sm:self-auto"
            @click="emit('changePlacement')"
          >
            <ArrowRightLeft class="w-3.5 h-3.5" />
            <span>Change Placement</span>
          </Button>
        </div>

        <!-- Current Placement Parameters Grid -->
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 text-xs">
          <div>
            <span class="text-surface-500 block mb-0.5">Campus</span>
            <span class="font-semibold text-surface-900 dark:text-surface-100 text-sm" data-testid="active-campus">
              {{ currentPlacement.campus }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">School Level</span>
            <span class="font-semibold text-surface-900 dark:text-surface-100 text-sm" data-testid="active-level">
              {{ currentPlacement.schoolLevel }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Grade</span>
            <span class="font-semibold text-surface-900 dark:text-surface-100 text-sm" data-testid="active-grade">
              {{ currentPlacement.grade }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Academic Year</span>
            <span class="font-semibold text-surface-900 dark:text-surface-100 text-sm" data-testid="active-academic-year">
              {{ currentPlacement.academicYear }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Effective Date</span>
            <span class="font-semibold text-teal-700 dark:text-teal-400 text-sm" data-testid="active-effective-date">
              {{ formatDisplayDate(currentPlacement.effectiveDate) }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Homeroom</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="active-homeroom">
              {{ currentPlacement.homeroom || '—' }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Cohort</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="active-cohort">
              {{ currentPlacement.cohort || '—' }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Program</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="active-program">
              {{ currentPlacement.program || 'Standard' }}
            </span>
          </div>

          <div>
            <span class="text-surface-500 block mb-0.5">Stream</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="active-stream">
              {{ currentPlacement.stream || '—' }}
            </span>
          </div>

          <div v-if="currentPlacement.authorizedBy">
            <span class="text-surface-500 block mb-0.5">Authorized By</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="active-authorized-by">
              {{ currentPlacement.authorizedBy }}
            </span>
          </div>

          <div v-if="currentPlacement.decisionReference">
            <span class="text-surface-500 block mb-0.5">Decision Reference</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="active-decision-reference">
              {{ currentPlacement.decisionReference }}
            </span>
          </div>

          <div v-if="currentPlacement.reason" class="col-span-2 sm:col-span-3 lg:col-span-4">
            <span class="text-surface-500 block mb-0.5">Reason for Placement</span>
            <span class="text-surface-700 dark:text-surface-300 italic" data-testid="active-reason">
              {{ currentPlacement.reason }}
            </span>
          </div>
        </div>
      </div>

      <!-- 2. Historical Placement Timeline / Table -->
      <div class="space-y-3" data-testid="placement-history-section">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <History class="w-4 h-4 text-teal-600" />
            <h4 class="text-xs font-bold uppercase tracking-wider text-surface-700 dark:text-surface-300">
              Placement History &amp; Prior Periods
            </h4>
          </div>
          <span class="text-xs text-surface-400">
            {{ priorPlacements.length }} prior {{ priorPlacements.length === 1 ? 'placement' : 'placements' }}
          </span>
        </div>

        <!-- No prior placements state -->
        <div
          v-if="priorPlacements.length === 0"
          class="p-4 rounded-lg border border-dashed border-surface-200 dark:border-surface-700 text-center text-xs text-surface-500 bg-surface-50/50 dark:bg-surface-800/20"
          data-testid="empty-prior-placements"
        >
          No prior placement periods recorded. The current assignment is the student's initial administrative placement.
        </div>

        <!-- Prior placements list -->
        <div v-else class="space-y-3" data-testid="prior-placements-list">
          <div
            v-for="(hist, idx) in priorPlacements"
            :key="hist.id || idx"
            class="p-4 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/60 dark:bg-surface-800/40 space-y-3"
            :data-testid="`placement-history-card-${idx}`"
          >
            <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1 text-xs">
              <span class="font-bold text-surface-900 dark:text-surface-100 flex items-center gap-1.5">
                <GraduationCap class="w-4 h-4 text-surface-400" />
                {{ hist.campus }} • {{ hist.schoolLevel }} — {{ hist.grade }} ({{ hist.academicYear }})
              </span>
              <div class="flex items-center gap-2 text-surface-500">
                <span data-testid="history-effective-date">
                  From: <strong class="text-surface-700 dark:text-surface-300">{{ formatDisplayDate(hist.effectiveDate) }}</strong>
                </span>
                <span>—</span>
                <span data-testid="history-end-date">
                  To: <strong class="text-surface-700 dark:text-surface-300">{{ formatDisplayDate(hist.endDate || '') || 'Closed' }}</strong>
                </span>
              </div>
            </div>

            <!-- Parameters Grid -->
            <div class="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-2 border-t border-surface-200/60 dark:border-surface-700/60 text-xs">
              <div>
                <span class="text-surface-500 block">Homeroom</span>
                <span class="font-medium text-surface-700 dark:text-surface-300">{{ hist.homeroom || '—' }}</span>
              </div>
              <div>
                <span class="text-surface-500 block">Cohort</span>
                <span class="font-medium text-surface-700 dark:text-surface-300">{{ hist.cohort || '—' }}</span>
              </div>
              <div>
                <span class="text-surface-500 block">Program / Stream</span>
                <span class="font-medium text-surface-700 dark:text-surface-300">
                  {{ hist.program || 'Standard' }} {{ hist.stream ? `• ${hist.stream}` : '' }}
                </span>
              </div>
              <div>
                <span class="text-surface-500 block">Authorized By</span>
                <span class="font-medium text-surface-700 dark:text-surface-300" data-testid="history-authorized-by">
                  {{ hist.authorizedBy || '—' }}
                </span>
              </div>
            </div>

            <!-- Reason and Reference -->
            <div class="flex flex-wrap items-center justify-between gap-2 pt-1 text-xs bg-surface-100/60 dark:bg-surface-800/60 p-2 rounded">
              <div class="text-surface-600 dark:text-surface-400" data-testid="history-reason">
                <span class="font-medium text-surface-700 dark:text-surface-300">Reason:</span>
                {{ hist.reason || 'Not specified' }}
              </div>
              <div class="text-surface-500 text-[11px]" data-testid="history-decision-reference">
                <span>Ref:</span> <strong class="text-surface-700 dark:text-surface-300">{{ hist.decisionReference || '—' }}</strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
