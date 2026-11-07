<script setup lang="ts">
import { ref, computed } from 'vue';
import Timeline from 'primevue/timeline';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import {
  ShieldCheck,
  History,
  Calendar,
  Clock,
  UserCheck,
  FileText,
  AlertCircle,
  Eye,
  ArrowRight,
  Lock,
  BookOpen,
  Award,
  Layers,
  CheckCircle2,
} from 'lucide-vue-next';
import { EmptyState, LoadingSpinner } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useLearningPlanDetail } from '../composables/useLearningPlans';
import type {
  SlpApproval,
  StudentLearningPlan,
  StudentLearningPlanListItem,
  StudentLearningPlanStatus,
  ApprovalType,
} from '../types';

export interface SlpApprovalHistoryTabProps {
  plan: StudentLearningPlan;
  allPlans?: StudentLearningPlanListItem[];
}

const props = withDefaults(defineProps<SlpApprovalHistoryTabProps>(), {
  allPlans: () => [],
});

const emit = defineEmits<{
  (e: 'select-plan', planId: string): void;
}>();

// ---------------------------------------------------------------------------
// 1. Chronological Timeline Entries
// ---------------------------------------------------------------------------
const sortedApprovals = computed<SlpApproval[]>(() => {
  if (!props.plan?.approvals || props.plan.approvals.length === 0) {
    return [];
  }
  return [...props.plan.approvals].sort((a, b) => {
    if (a.planVersion !== b.planVersion) {
      return a.planVersion - b.planVersion;
    }
    const dateA = a.approvedDate ? new Date(a.approvedDate).getTime() : 0;
    const dateB = b.approvedDate ? new Date(b.approvedDate).getTime() : 0;
    return dateA - dateB;
  });
});

// ---------------------------------------------------------------------------
// 2. Student Historical Plan Versions
// ---------------------------------------------------------------------------
const studentVersions = computed<StudentLearningPlanListItem[]>(() => {
  if (!props.plan) return [];

  // Filter plans for the same student
  const matches = props.allPlans.filter(
    (p) =>
      p.studentId === props.plan.studentId &&
      (!props.plan.academicPeriodId || p.academicPeriodId === props.plan.academicPeriodId),
  );

  const hasCurrent = matches.some((p) => p.id === props.plan.id);
  const combined = hasCurrent
    ? [...matches]
    : [
        ...matches,
        {
          id: props.plan.id,
          studentId: props.plan.studentId,
          studentName: props.plan.studentName,
          studentNis: props.plan.studentNis,
          academicPeriodId: props.plan.academicPeriodId,
          academicPeriodLabel: props.plan.academicPeriodLabel,
          status: props.plan.status,
          version: props.plan.version,
          unitsCount: props.plan.constituentUnits?.length || 0,
          adjustmentsCount: props.plan.adjustments?.length || 0,
        } as StudentLearningPlanListItem,
      ];

  return combined.sort((a, b) => a.version - b.version);
});

const activePlanVersion = computed<StudentLearningPlanListItem | undefined>(() => {
  return studentVersions.value.find((p) => p.status === 'active');
});

const isReadOnlyHistorical = computed<boolean>(() => {
  return (
    props.plan.status === 'revised' ||
    props.plan.status === 'completed' ||
    props.plan.status === 'archived'
  );
});

// ---------------------------------------------------------------------------
// 3. Quick Inspection Dialog (Read-Only Snapshot)
// ---------------------------------------------------------------------------
const inspectingPlanId = ref<string | null>(null);
const isInspectDialogOpen = ref<boolean>(false);

const { data: inspectedPlanDetail, isLoading: isLoadingInspectedPlan } =
  useLearningPlanDetail(inspectingPlanId, {
    enabled: computed(() => Boolean(inspectingPlanId.value)),
  });

function handleOpenInspect(planId: string) {
  inspectingPlanId.value = planId;
  isInspectDialogOpen.value = true;
}

function handleSwitchToInspected(planId: string) {
  isInspectDialogOpen.value = false;
  inspectingPlanId.value = null;
  emit('select-plan', planId);
}

// ---------------------------------------------------------------------------
// 4. Badges & Labels Helpers
// ---------------------------------------------------------------------------
function getApprovalTypeLabel(type: ApprovalType | string): string {
  switch (type) {
    case 'initial_approval':
      return 'Initial Approval';
    case 'revision_approval':
      return 'Revision Approval';
    default:
      return 'Plan Approval';
  }
}

function getApprovalTagSeverity(
  type: ApprovalType | string,
): 'success' | 'info' | 'warn' | 'secondary' {
  switch (type) {
    case 'initial_approval':
      return 'success';
    case 'revision_approval':
      return 'info';
    default:
      return 'secondary';
  }
}

function getStatusTagSeverity(
  status: StudentLearningPlanStatus | string,
): 'success' | 'warn' | 'secondary' | 'danger' {
  switch (status) {
    case 'active':
      return 'success';
    case 'draft':
      return 'warn';
    case 'archived':
      return 'danger';
    case 'revised':
    case 'completed':
    default:
      return 'secondary';
  }
}

function findPlanForVersion(
  version: number,
): StudentLearningPlanListItem | undefined {
  return studentVersions.value.find((p) => p.version === version);
}
</script>

<template>
  <div class="space-y-6" data-testid="slp-approval-history-tab">
    <!-- Read-Only Historical Banner -->
    <div
      v-if="isReadOnlyHistorical"
      class="p-4 rounded-xl border border-amber-200 bg-amber-50/70 dark:bg-amber-950/30 dark:border-amber-900/60 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs"
      data-testid="slp-readonly-banner"
    >
      <div class="flex items-start gap-3">
        <div class="w-8 h-8 rounded-lg bg-amber-100 dark:bg-amber-900/60 text-amber-700 dark:text-amber-300 flex items-center justify-center flex-shrink-0 mt-0.5">
          <Lock class="w-4 h-4" />
        </div>
        <div class="space-y-0.5">
          <div class="text-xs font-bold text-amber-900 dark:text-amber-200">
            Read-Only Historical Plan Mode (Version v{{ plan.version }})
          </div>
          <p class="text-xs text-amber-700 dark:text-amber-300 leading-relaxed">
            You are viewing a historical
            <span class="font-semibold">{{ plan.status }}</span> plan snapshot.
            Historical plans cannot be edited or amended directly.
          </p>
        </div>
      </div>

      <Button
        v-if="activePlanVersion && activePlanVersion.id !== plan.id"
        label="Switch to Active Plan"
        icon="pi pi-arrow-right"
        size="small"
        severity="warn"
        class="text-xs font-semibold whitespace-nowrap self-start sm:self-center"
        data-testid="switch-to-active-btn"
        @click="emit('select-plan', activePlanVersion.id)"
      />
    </div>

    <!-- 1. Plan Versions Navigation Bar -->
    <div
      class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 p-4 shadow-2xs space-y-3"
      data-testid="slp-version-switcher"
    >
      <div class="flex items-center justify-between gap-2 flex-wrap">
        <div class="flex items-center gap-2">
          <div class="w-6 h-6 rounded-md bg-indigo-50 dark:bg-indigo-950 text-indigo-600 dark:text-indigo-400 flex items-center justify-center">
            <Layers class="w-3.5 h-3.5" />
          </div>
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-800 dark:text-slate-200">
            Plan Versions & Lifecycle Timeline
          </h3>
        </div>
        <span class="text-[11px] text-slate-500 dark:text-slate-400">
          Student: <strong class="text-slate-700 dark:text-slate-300">{{ plan.studentName || 'Student' }}</strong>
        </span>
      </div>

      <!-- Version Chips / Switcher Buttons -->
      <div class="flex items-center gap-2 flex-wrap pt-1">
        <div
          v-for="ver in studentVersions"
          :key="ver.id"
          :class="[
            'flex items-center gap-2 px-3 py-2 rounded-lg border text-xs transition-all select-none',
            ver.id === plan.id
              ? 'border-indigo-600 bg-indigo-50/80 dark:bg-indigo-950/50 text-indigo-950 dark:text-indigo-100 font-semibold ring-1 ring-indigo-600/30'
              : 'border-slate-200 dark:border-slate-800 bg-slate-50/60 dark:bg-slate-800/40 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800',
          ]"
          :data-testid="`version-chip-${ver.version}`"
        >
          <span class="font-mono font-bold text-xs">v{{ ver.version }}</span>
          <Tag
            :value="ver.status.toUpperCase()"
            :severity="getStatusTagSeverity(ver.status)"
            class="text-[10px] font-bold px-1.5 py-0.2"
          />

          <span
            v-if="ver.id === plan.id"
            class="text-[10px] uppercase font-bold text-indigo-600 dark:text-indigo-400 ml-1"
          >
            (Current)
          </span>

          <div v-else class="flex items-center gap-1 ml-1.5">
            <button
              type="button"
              class="text-indigo-600 dark:text-indigo-400 hover:underline font-medium text-[11px] focus:outline-hidden"
              :data-testid="`version-btn-${ver.version}`"
              @click="emit('select-plan', ver.id)"
            >
              Switch
            </button>
            <span class="text-slate-300 dark:text-slate-600">|</span>
            <button
              type="button"
              class="text-slate-500 hover:text-slate-700 dark:hover:text-slate-200 text-[11px] flex items-center gap-0.5 focus:outline-hidden"
              :data-testid="`quick-inspect-btn-${ver.version}`"
              title="Quick inspect read-only snapshot"
              @click="handleOpenInspect(ver.id)"
            >
              <Eye class="w-3 h-3" />
              <span>Inspect</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 2. Chronological Timeline of Approvals -->
    <div
      class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-2xs overflow-hidden"
    >
      <div class="p-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <History class="w-4 h-4 text-slate-500" />
          <h4 class="text-xs font-bold uppercase tracking-wider text-slate-800 dark:text-slate-200">
            Chronological Authority Approval Trail
          </h4>
        </div>
        <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">
          {{ sortedApprovals.length }} {{ sortedApprovals.length === 1 ? 'approval event' : 'approval events' }}
        </span>
      </div>

      <!-- Empty State for Draft Plans Awaiting Initial Approval -->
      <div
        v-if="sortedApprovals.length === 0"
        class="p-8 text-center space-y-4"
        data-testid="slp-approvals-empty"
      >
        <EmptyState
          title="Awaiting Initial Approval"
          :description="`This learning plan (v${plan.version}) is currently in ${plan.status} status awaiting formal review and approval by an authorized Academic Authority.`"
          size="sm"
        >
          <template #icon>
            <div class="w-12 h-12 rounded-full bg-amber-50 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400 flex items-center justify-center mx-auto mb-2">
              <Clock class="w-6 h-6" />
            </div>
          </template>
        </EmptyState>

        <div class="max-w-md mx-auto p-3.5 rounded-lg bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-700 text-left text-xs text-slate-600 dark:text-slate-400 space-y-1.5">
          <div class="font-semibold text-slate-800 dark:text-slate-200 flex items-center gap-1.5">
            <ShieldCheck class="w-3.5 h-3.5 text-indigo-500" />
            Academic Governance Requirement
          </div>
          <p class="leading-relaxed text-[11px]">
            Every individualized Student Learning Plan requires explicit sign-off from an Academic Authority
            (Curriculum Lead, Academic Head, or VP Academics) before it activates. Once approved, immutable audit
            stamps, authority credentials, and governance notes will be displayed chronologically here.
          </p>
        </div>
      </div>

      <!-- Timeline Display -->
      <div v-else class="p-6" data-testid="slp-approvals-timeline">
        <Timeline
          :value="sortedApprovals"
          class="customized-timeline"
        >
          <!-- Marker Slot -->
          <template #marker="{ item }">
            <span
              :class="[
                'flex w-8 h-8 items-center justify-center rounded-full text-white shadow-xs ring-4 ring-white dark:ring-slate-900',
                item.approvalType === 'initial_approval' ? 'bg-emerald-600' : 'bg-indigo-600',
              ]"
            >
              <ShieldCheck v-if="item.approvalType === 'initial_approval'" class="w-4 h-4" />
              <History v-else class="w-4 h-4" />
            </span>
          </template>

          <!-- Content Slot -->
          <template #content="{ item }">
            <div
              class="p-4 bg-slate-50/80 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-700 rounded-xl shadow-2xs mb-6 space-y-3 text-xs"
              :data-testid="`approval-entry-${item.id}`"
            >
              <!-- Top Row: Plan Version, Approval Type, and Date -->
              <div class="flex items-center justify-between gap-2 flex-wrap">
                <div class="flex items-center gap-2">
                  <span
                    class="font-mono font-bold text-xs px-2 py-0.5 rounded bg-slate-200 dark:bg-slate-700 text-slate-800 dark:text-slate-200"
                    data-testid="approval-version-tag"
                  >
                    v{{ item.planVersion }}
                  </span>

                  <Tag
                    :value="getApprovalTypeLabel(item.approvalType)"
                    :severity="getApprovalTagSeverity(item.approvalType)"
                    class="text-[10px] font-bold px-2 py-0.5"
                    data-testid="approval-type-tag"
                  />
                </div>

                <div
                  class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400 font-medium text-[11px]"
                  data-testid="approval-date"
                >
                  <Calendar class="w-3.5 h-3.5 text-slate-400" />
                  <span>{{ formatDisplayDate(item.approvedDate) }}</span>
                </div>
              </div>

              <!-- Authority Sign-Off -->
              <div class="flex items-center gap-2 text-xs" data-testid="approval-authority">
                <div class="w-6 h-6 rounded-full bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 flex items-center justify-center">
                  <UserCheck class="w-3.5 h-3.5" />
                </div>
                <div>
                  <span class="text-slate-500 dark:text-slate-400 mr-1">Approving Authority:</span>
                  <span class="font-bold text-slate-900 dark:text-slate-100">
                    {{ item.approvedBy }}
                  </span>
                </div>
              </div>

              <!-- Governance Notes -->
              <div
                v-if="item.notes"
                class="p-3 bg-white dark:bg-slate-900 rounded-lg border-l-3 border-indigo-500 dark:border-indigo-400 text-slate-700 dark:text-slate-300 text-xs italic space-y-1"
                data-testid="approval-notes"
              >
                <div class="text-[10px] uppercase font-bold text-slate-400 not-italic tracking-wider">
                  Governance Notes
                </div>
                <p class="leading-relaxed">"{{ item.notes }}"</p>
              </div>
              <div
                v-else
                class="text-[11px] text-slate-400 italic"
                data-testid="approval-notes-empty"
              >
                No governance notes recorded for this approval event.
              </div>

              <!-- Link / Action to inspect corresponding historical version -->
              <div class="pt-2 border-t border-slate-200/60 dark:border-slate-700/60 flex items-center justify-between text-[11px]">
                <span class="text-slate-400">
                  Target Plan State: Version {{ item.planVersion }}
                </span>

                <template v-if="findPlanForVersion(item.planVersion)">
                  <button
                    v-if="findPlanForVersion(item.planVersion)?.id !== plan.id"
                    type="button"
                    class="inline-flex items-center gap-1 text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 font-semibold focus:outline-hidden"
                    :data-testid="`inspect-version-btn-${item.planVersion}`"
                    @click="handleOpenInspect(findPlanForVersion(item.planVersion)!.id)"
                  >
                    <Eye class="w-3.5 h-3.5" />
                    <span>Inspect v{{ item.planVersion }} Snapshot</span>
                  </button>
                  <span
                    v-else
                    class="text-emerald-600 dark:text-emerald-400 font-medium flex items-center gap-1"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5" />
                    Currently active view
                  </span>
                </template>
              </div>
            </div>
          </template>
        </Timeline>
      </div>
    </div>

    <!-- Quick Inspect Historical Snapshot Dialog (Read-Only) -->
    <Dialog
      v-model:visible="isInspectDialogOpen"
      modal
      :header="`Historical Plan Snapshot — Version v${inspectedPlanDetail?.version || ''} (${(inspectedPlanDetail?.status || '').toUpperCase()})`"
      :style="{ width: '56rem', maxWidth: '95vw' }"
      data-testid="historical-inspect-dialog"
    >
      <div v-if="isLoadingInspectedPlan" class="py-12 flex justify-center text-xs text-slate-400">
        <LoadingSpinner size="md" message="Loading historical plan snapshot..." />
      </div>

      <div v-else-if="inspectedPlanDetail" class="space-y-6 text-xs">
        <!-- Overview Header Card -->
        <div class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 flex flex-col sm:flex-row justify-between gap-4">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="font-bold text-sm text-slate-900 dark:text-slate-100">
                {{ inspectedPlanDetail.studentName || 'Student' }}
              </span>
              <Tag
                :value="inspectedPlanDetail.status.toUpperCase()"
                :severity="getStatusTagSeverity(inspectedPlanDetail.status)"
                class="text-[10px] font-bold"
              />
            </div>
            <div class="text-slate-500 text-[11px] flex items-center gap-2">
              <span>NIS: {{ inspectedPlanDetail.studentNis || '—' }}</span>
              <span>•</span>
              <span>{{ inspectedPlanDetail.academicPeriodLabel }}</span>
            </div>
            <p v-if="inspectedPlanDetail.notes" class="text-slate-600 dark:text-slate-400 italic pt-1 text-[11px]">
              "{{ inspectedPlanDetail.notes }}"
            </p>
          </div>

          <div class="flex items-center gap-2 self-start sm:self-center">
            <Button
              v-if="inspectedPlanDetail.id !== plan.id"
              label="Switch Workspace to This Version"
              icon="pi pi-check"
              size="small"
              class="text-xs font-semibold"
              data-testid="dialog-switch-version-btn"
              @click="handleSwitchToInspected(inspectedPlanDetail.id)"
            />
          </div>
        </div>

        <!-- Constituent Units Section -->
        <div class="space-y-2">
          <div class="flex items-center gap-1.5 font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300">
            <BookOpen class="w-3.5 h-3.5 text-indigo-500" />
            <span>Constituent Units ({{ inspectedPlanDetail.constituentUnits?.length || 0 }})</span>
          </div>

          <div
            v-if="!inspectedPlanDetail.constituentUnits || inspectedPlanDetail.constituentUnits.length === 0"
            class="p-4 text-center text-slate-400 bg-slate-50 dark:bg-slate-800/30 rounded-lg border border-slate-200 dark:border-slate-700"
          >
            No constituent units linked to this plan version.
          </div>

          <div v-else class="divide-y divide-slate-100 dark:divide-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg overflow-hidden">
            <div
              v-for="unit in inspectedPlanDetail.constituentUnits"
              :key="unit.id"
              class="p-3 flex items-center justify-between gap-2 hover:bg-slate-50/50 dark:hover:bg-slate-800/20"
            >
              <div class="flex items-center gap-2.5">
                <span class="w-6 h-6 rounded bg-indigo-50 dark:bg-indigo-950 text-indigo-600 dark:text-indigo-400 flex items-center justify-center font-bold text-xs">
                  {{ unit.sequenceOrder || '—' }}
                </span>
                <div>
                  <div class="font-semibold text-slate-800 dark:text-slate-200">
                    {{ unit.title }} <span class="font-normal text-slate-400">({{ unit.unitCode }})</span>
                  </div>
                  <div v-if="unit.notes" class="text-[11px] text-slate-500">
                    Adaptation: {{ unit.notes }}
                  </div>
                </div>
              </div>
              <Tag
                :value="unit.isRequired ? 'REQUIRED' : 'ELECTIVE'"
                :severity="unit.isRequired ? 'info' : 'secondary'"
                class="text-[10px] font-bold"
              />
            </div>
          </div>
        </div>

        <!-- Academic Plan Adjustments Section -->
        <div class="space-y-2">
          <div class="flex items-center gap-1.5 font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300">
            <Award class="w-3.5 h-3.5 text-purple-500" />
            <span>Plan Adjustments ({{ inspectedPlanDetail.adjustments?.length || 0 }})</span>
          </div>

          <div
            v-if="!inspectedPlanDetail.adjustments || inspectedPlanDetail.adjustments.length === 0"
            class="p-4 text-center text-slate-400 bg-slate-50 dark:bg-slate-800/30 rounded-lg border border-slate-200 dark:border-slate-700"
          >
            No academic adjustments established for this plan version.
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="adj in inspectedPlanDetail.adjustments"
              :key="adj.id"
              class="p-3 bg-slate-50 dark:bg-slate-800/40 rounded-lg border border-slate-200 dark:border-slate-700 space-y-1.5"
            >
              <div class="flex items-center justify-between gap-2 flex-wrap">
                <div class="flex items-center gap-2">
                  <Tag
                    :value="adj.adjustmentType.toUpperCase()"
                    :severity="adj.adjustmentType === 'accommodation' ? 'info' : adj.adjustmentType === 'substitution' ? 'warn' : 'secondary'"
                    class="text-[10px] font-bold"
                  />
                  <span class="font-semibold text-slate-800 dark:text-slate-200">
                    {{ adj.description }}
                  </span>
                </div>
                <span class="text-[11px] text-slate-400">
                  Effective {{ formatDisplayDate(adj.effectiveDate) }}
                </span>
              </div>
              <p class="text-[11px] text-slate-600 dark:text-slate-400 pl-2 border-l-2 border-slate-300 dark:border-slate-600">
                Rationale: {{ adj.rationale }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-2">
          <Button
            label="Close"
            severity="secondary"
            size="small"
            class="text-xs"
            @click="isInspectDialogOpen = false"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
