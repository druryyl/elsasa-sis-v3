<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  ArrowLeftRight,
  RotateCcw,
  Plus,
  ShieldCheck,
  ShieldAlert,
  AlertTriangle,
  CheckCircle2,
  Calendar,
  Clock,
  UserX,
  FileCheck2,
  XCircle,
  HelpCircle,
  Info,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner, FormField } from '@/shared/components';
import {
  useSubstitutions,
  useReassignments,
  useApproveSubstituteExceptionMutation,
  useCancelSubstitutionMutation,
  useApproveReassignmentMutation,
  useConfirmReassignmentHandoverMutation,
} from '../composables/useAssignments';
import ArrangeSubstituteModal from './ArrangeSubstituteModal.vue';
import ReassignmentModal from './ReassignmentModal.vue';
import {
  SUBSTITUTION_STATUS_MAP,
  REASSIGNMENT_STATUS_MAP,
  HANDOVER_STATUS_MAP,
  DEFAULT_FACULTY_CANDIDATES,
  PERIOD_PRESETS,
} from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingSubstitutionDto,
  TeachingReassignmentDto,
} from '../types';

export interface CoverageSubstitutionsTabProps {
  academicPeriodId?: string;
  campusScope?: string;
  search?: string;
  canManage?: boolean;
  canApprove?: boolean;
}

const props = withDefaults(defineProps<CoverageSubstitutionsTabProps>(), {
  academicPeriodId: undefined,
  campusScope: undefined,
  search: '',
  canManage: true,
  canApprove: true,
});

// View mode: 'substitutions' or 'reassignments'
const activeSubView = ref<'substitutions' | 'reassignments'>('substitutions');

// Queries
const {
  data: substitutionsData,
  isLoading: isSubsLoading,
  refetch: refetchSubs,
} = useSubstitutions();

const substitutions = computed<TeachingSubstitutionDto[]>(() => {
  if (!substitutionsData.value) return [];
  return Array.isArray(substitutionsData.value)
    ? substitutionsData.value
    : (substitutionsData.value as { items?: TeachingSubstitutionDto[] }).items || [];
});

const {
  data: reassignmentsData,
  isLoading: isReasLoading,
  refetch: refetchReas,
} = useReassignments();

const reassignments = computed<TeachingReassignmentDto[]>(() => {
  if (!reassignmentsData.value) return [];
  return Array.isArray(reassignmentsData.value)
    ? reassignmentsData.value
    : (reassignmentsData.value as { items?: TeachingReassignmentDto[] }).items || [];
});

// Teacher Name Resolver
function getTeacherName(teacherId: string): string {
  const match = DEFAULT_FACULTY_CANDIDATES.find((f) => f.id === teacherId);
  return match ? `${match.fullName} (${match.teacherNumber})` : teacherId.slice(0, 8);
}

// Modals State
const isArrangeModalVisible = ref<boolean>(false);
const isReassignModalVisible = ref<boolean>(false);

// Approve Exception Dialog
const isApproveExceptionModalVisible = ref<boolean>(false);
const selectedSubForException = ref<TeachingSubstitutionDto | null>(null);
const exceptionDecisionRemarks = ref<string>('');

// Confirm Handover Dialog
const isHandoverModalVisible = ref<boolean>(false);
const selectedReassignmentForHandover = ref<TeachingReassignmentDto | null>(null);
const handoverStatusChoice = ref<string>('completed');
const handoverNotes = ref<string>('');

// Mutations
const approveSubExceptionMutation = useApproveSubstituteExceptionMutation();
const cancelSubMutation = useCancelSubstitutionMutation();
const approveReassignmentMutation = useApproveReassignmentMutation();
const confirmHandoverMutation = useConfirmReassignmentHandoverMutation();

function handleOpenApproveException(sub: TeachingSubstitutionDto) {
  selectedSubForException.value = sub;
  exceptionDecisionRemarks.value = '';
  isApproveExceptionModalVisible.value = true;
}

function handleConfirmApproveException() {
  if (!selectedSubForException.value) return;

  approveSubExceptionMutation.mutate(
    {
      id: selectedSubForException.value.id,
      payload: {
        decisionRemarks: exceptionDecisionRemarks.value.trim() || undefined,
      },
    },
    {
      onSuccess: () => {
        isApproveExceptionModalVisible.value = false;
        refetchSubs();
      },
    },
  );
}

function handleCancelSubstitution(sub: TeachingSubstitutionDto) {
  cancelSubMutation.mutate(
    {
      id: sub.id,
      payload: { reason: 'Absence ended early / cancelled by coordinator' },
    },
    {
      onSuccess: () => {
        refetchSubs();
      },
    },
  );
}

function handleApproveReassignment(reas: TeachingReassignmentDto) {
  approveReassignmentMutation.mutate(
    {
      id: reas.id,
      payload: { notes: 'Approved by Teaching Authority' },
    },
    {
      onSuccess: () => {
        refetchReas();
      },
    },
  );
}

function handleOpenConfirmHandover(reas: TeachingReassignmentDto) {
  selectedReassignmentForHandover.value = reas;
  handoverStatusChoice.value = 'completed';
  handoverNotes.value = '';
  isHandoverModalVisible.value = true;
}

function handleConfirmHandover() {
  if (!selectedReassignmentForHandover.value) return;

  confirmHandoverMutation.mutate(
    {
      id: selectedReassignmentForHandover.value.id,
      payload: {
        handoverStatus: handoverStatusChoice.value,
        notes: handoverNotes.value.trim() || undefined,
      },
    },
    {
      onSuccess: () => {
        isHandoverModalVisible.value = false;
        refetchReas();
      },
    },
  );
}
</script>

<template>
  <div class="space-y-6" data-testid="coverage-substitutions-tab">
    <!-- Faculty Unavailability Windows Summary Card -->
    <div class="p-4 rounded-xl bg-surface-50 dark:bg-surface-800/40 border border-surface-200 dark:border-surface-700">
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-amber-50 dark:bg-amber-950 flex items-center justify-center text-amber-600 dark:text-amber-400 shrink-0">
            <UserX class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100">
              Active Faculty Leave & Unavailability Windows
            </h3>
            <p class="text-xs text-surface-500 dark:text-surface-400">
              Approved medical leave, official training, and temporary blackout windows requiring coverage
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <Button
            v-if="canManage"
            label="Arrange Emergency Substitute"
            severity="warning"
            size="small"
            @click="isArrangeModalVisible = true"
            data-testid="btn-open-arrange-substitute"
          >
            <template #icon>
              <ArrowLeftRight class="w-3.5 h-3.5 mr-1.5" />
            </template>
          </Button>

          <Button
            v-if="canManage"
            label="Propose Reassignment"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isReassignModalVisible = true"
            data-testid="btn-open-propose-reassignment"
          >
            <template #icon>
              <RotateCcw class="w-3.5 h-3.5 mr-1.5" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Sample Demo Leave Window Alert -->
      <div class="mt-3.5 p-3 rounded-lg bg-amber-50/70 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 text-xs flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="w-2 h-2 rounded-full bg-amber-500 animate-pulse"></span>
          <span class="font-semibold text-amber-950 dark:text-amber-200">Budi Hartono, M.Pd. (TCH-001)</span>
          <span class="text-amber-800 dark:text-amber-300">— Medical Leave (Cuti Sakit): 2027-02-01 to 2027-02-14 (10 work days)</span>
        </div>
        <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-amber-200 text-amber-900 dark:bg-amber-900 dark:text-amber-100">
          Covered by Siti Nurhaliza
        </span>
      </div>
    </div>

    <!-- Section Switcher Tabs -->
    <div class="flex items-center justify-between border-b border-surface-200 dark:border-surface-700 pb-2">
      <div class="flex items-center gap-2">
        <Button
          :label="`Emergency Substitutions (${substitutions.length})`"
          :severity="activeSubView === 'substitutions' ? 'primary' : 'secondary'"
          :variant="activeSubView === 'substitutions' ? undefined : 'text'"
          size="small"
          class="!text-xs"
          @click="activeSubView = 'substitutions'"
          data-testid="btn-view-substitutions"
        />
        <Button
          :label="`Permanent Reassignments (${reassignments.length})`"
          :severity="activeSubView === 'reassignments' ? 'primary' : 'secondary'"
          :variant="activeSubView === 'reassignments' ? undefined : 'text'"
          size="small"
          class="!text-xs"
          @click="activeSubView = 'reassignments'"
          data-testid="btn-view-reassignments"
        />
      </div>

      <!-- Resolution 3.8 Helper Indicator -->
      <div class="hidden sm:flex items-center gap-1.5 text-xs text-surface-500">
        <Info class="w-3.5 h-3.5 text-sky-500" />
        <span>Resolution 3.8: Pre-Authorized Pool Delegation Active</span>
      </div>
    </div>

    <!-- -------------------------------------------------------------------- -->
    <!-- VIEW A: Emergency Substitutions Worklist                            -->
    <!-- -------------------------------------------------------------------- -->
    <div v-if="activeSubView === 'substitutions'" class="space-y-4">
      <div v-if="isSubsLoading" class="p-12 flex justify-center">
        <LoadingSpinner />
      </div>

      <div
        v-else-if="substitutions.length === 0"
        class="p-8 text-center bg-surface-50/50 dark:bg-surface-800/20 border border-surface-200 dark:border-surface-700 rounded-xl"
        data-testid="empty-substitutions-state"
      >
        <EmptyState
          title="No Emergency Substitutions Recorded"
          description="Emergency absences requiring temporary classroom coverage will be listed here."
        >
          <template #icon>
            <div class="w-12 h-12 rounded-full bg-amber-50 dark:bg-amber-950 flex items-center justify-center mx-auto mb-3 text-amber-600 dark:text-amber-400">
              <ArrowLeftRight class="w-6 h-6" />
            </div>
          </template>
        </EmptyState>
      </div>

      <div
        v-else
        class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs"
        data-testid="substitutions-table-container"
      >
        <DataTable
          :value="substitutions"
          responsive-layout="scroll"
          class="!text-xs"
          data-testid="substitutions-table"
        >
          <!-- Substitution # -->
          <Column header="Substitution #" class="w-36 font-mono font-medium">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="text-surface-900 dark:text-surface-100 font-semibold">{{ data.substitutionNumber }}</span>
                <span class="block text-[10px] text-surface-400">{{ data.createdAt?.slice(0, 10) }}</span>
              </div>
            </template>
          </Column>

          <!-- Original Teacher & Reason -->
          <Column header="Absent Teacher" class="min-w-44">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="font-medium text-surface-900 dark:text-surface-100 block">
                  {{ getTeacherName(data.originalTeacherId) }}
                </span>
                <span class="text-[10px] text-amber-700 dark:text-amber-400 font-medium block capitalize">
                  {{ data.reason?.replace('_', ' ') }}
                </span>
              </div>
            </template>
          </Column>

          <!-- Substitute Teacher -->
          <Column header="Substitute Assigned" class="min-w-44">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="font-medium text-surface-900 dark:text-surface-100 block">
                  {{ getTeacherName(data.substituteTeacherId) }}
                </span>
                <span class="text-[10px] text-surface-400 block font-mono">
                  Impact: {{ data.workloadImpactHours }} hrs/wk
                </span>
              </div>
            </template>
          </Column>

          <!-- Coverage Dates & Scope -->
          <Column header="Coverage Window" class="w-48">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="font-medium text-surface-900 dark:text-surface-100 block">
                  {{ data.coverageStartDate }} to {{ data.coverageEndDate }}
                </span>
                <span class="text-[10px] text-surface-400 capitalize block">
                  Scope: {{ data.coverageScopeType }}
                </span>
              </div>
            </template>
          </Column>

          <!-- Governance Delegation Indicator (Resolution 3.8) -->
          <Column header="Delegation" class="w-44">
            <template #body="{ data }">
              <div v-if="data.status === 'approved'" class="flex items-center gap-1 text-[11px] text-emerald-600 dark:text-emerald-400">
                <ShieldCheck class="w-3.5 h-3.5" />
                <span>Pre-Authorized (Direct)</span>
              </div>
              <div v-else-if="data.status === 'proposed'" class="flex items-center gap-1 text-[11px] text-amber-600 dark:text-amber-400">
                <ShieldAlert class="w-3.5 h-3.5" />
                <span>Exception Escalation</span>
              </div>
              <div v-else class="text-[11px] text-surface-400">
                <span>{{ data.status }}</span>
              </div>
            </template>
          </Column>

          <!-- Status Pill -->
          <Column header="Status" class="w-36">
            <template #body="{ data }">
              <StatusPill
                :status="data.status"
                :variant="SUBSTITUTION_STATUS_MAP[data.status]?.variant || 'slate'"
                :label="SUBSTITUTION_STATUS_MAP[data.status]?.label || data.status"
              />
            </template>
          </Column>

          <!-- Actions -->
          <Column header="Actions" class="w-40 text-right">
            <template #body="{ data }">
              <div class="flex items-center justify-end gap-1">
                <!-- If proposed: Authority Exception Approval -->
                <Button
                  v-if="data.status === 'proposed' && canApprove"
                  label="Approve"
                  severity="success"
                  size="small"
                  class="!py-1 !px-2 !text-xs"
                  @click="handleOpenApproveException(data)"
                  data-testid="btn-approve-sub-exception"
                >
                  <template #icon>
                    <ShieldCheck class="w-3 h-3 mr-1" />
                  </template>
                </Button>

                <!-- Cancel Substitution -->
                <Button
                  v-if="['proposed', 'approved', 'active'].includes(data.status) && canManage"
                  label="Cancel"
                  severity="danger"
                  variant="outlined"
                  size="small"
                  class="!py-1 !px-2 !text-xs"
                  @click="handleCancelSubstitution(data)"
                  data-testid="btn-cancel-substitution"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>
    </div>

    <!-- -------------------------------------------------------------------- -->
    <!-- VIEW B: Permanent Reassignments Worklist                             -->
    <!-- -------------------------------------------------------------------- -->
    <div v-else class="space-y-4">
      <div v-if="isReasLoading" class="p-12 flex justify-center">
        <LoadingSpinner />
      </div>

      <div
        v-else-if="reassignments.length === 0"
        class="p-8 text-center bg-surface-50/50 dark:bg-surface-800/20 border border-surface-200 dark:border-surface-700 rounded-xl"
        data-testid="empty-reassignments-state"
      >
        <EmptyState
          title="No Permanent Reassignments Found"
          description="Permanent responsibility transfers linking predecessors and instructional handover will appear here."
        >
          <template #icon>
            <div class="w-12 h-12 rounded-full bg-indigo-50 dark:bg-indigo-950 flex items-center justify-center mx-auto mb-3 text-indigo-600 dark:text-indigo-400">
              <RotateCcw class="w-6 h-6" />
            </div>
          </template>
        </EmptyState>
      </div>

      <div
        v-else
        class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs"
        data-testid="reassignments-table-container"
      >
        <DataTable
          :value="reassignments"
          responsive-layout="scroll"
          class="!text-xs"
          data-testid="reassignments-table"
        >
          <!-- Reassignment # -->
          <Column header="Reassignment #" class="w-36 font-mono font-medium">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="text-surface-900 dark:text-surface-100 font-semibold">{{ data.reassignmentNumber }}</span>
                <span class="block text-[10px] text-surface-400">{{ data.effectiveDate }}</span>
              </div>
            </template>
          </Column>

          <!-- Predecessor Teacher -->
          <Column header="Predecessor Faculty" class="min-w-44">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="font-medium text-surface-900 dark:text-surface-100 block">
                  {{ getTeacherName(data.predecessorTeacherId) }}
                </span>
                <span class="text-[10px] text-surface-400 block font-mono">
                  Orig Asn: {{ data.originalAssignmentId?.slice(0, 8) }}...
                </span>
              </div>
            </template>
          </Column>

          <!-- Replacement Teacher -->
          <Column header="Incoming Faculty" class="min-w-44">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span class="font-medium text-surface-900 dark:text-surface-100 block">
                  {{ getTeacherName(data.newTeacherId) }}
                </span>
                <span class="text-[10px] text-indigo-700 dark:text-indigo-400 block font-mono">
                  New Asn: {{ data.replacementAssignmentId ? data.replacementAssignmentId.slice(0, 8) + '...' : 'Pending Approval' }}
                </span>
              </div>
            </template>
          </Column>

          <!-- Handover Status -->
          <Column header="Handover Status" class="w-40">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <span
                  class="px-2 py-0.5 rounded-full text-[10px] font-medium inline-block"
                  :class="HANDOVER_STATUS_MAP[data.handoverStatus]?.badgeClass"
                >
                  {{ HANDOVER_STATUS_MAP[data.handoverStatus]?.label || data.handoverStatus }}
                </span>
                <span v-if="data.handoverCompletedDate" class="text-[9px] text-surface-400 block">
                  {{ data.handoverCompletedDate }}
                </span>
              </div>
            </template>
          </Column>

          <!-- Status -->
          <Column header="Status" class="w-32">
            <template #body="{ data }">
              <StatusPill
                :status="data.status"
                :variant="REASSIGNMENT_STATUS_MAP[data.status]?.variant || 'slate'"
                :label="REASSIGNMENT_STATUS_MAP[data.status]?.label || data.status"
              />
            </template>
          </Column>

          <!-- Actions -->
          <Column header="Actions" class="w-48 text-right">
            <template #body="{ data }">
              <div class="flex items-center justify-end gap-1">
                <!-- If proposed: Authority Approve -->
                <Button
                  v-if="data.status === 'proposed' && canApprove"
                  label="Approve"
                  severity="success"
                  size="small"
                  class="!py-1 !px-2 !text-xs"
                  @click="handleApproveReassignment(data)"
                  data-testid="btn-approve-reassignment"
                >
                  <template #icon>
                    <ShieldCheck class="w-3 h-3 mr-1" />
                  </template>
                </Button>

                <!-- Handover Signoff Action -->
                <Button
                  v-if="data.handoverStatus === 'pending' && canManage"
                  label="Handover"
                  severity="secondary"
                  variant="outlined"
                  size="small"
                  class="!py-1 !px-2 !text-xs"
                  @click="handleOpenConfirmHandover(data)"
                  title="Confirm Instructional Handover"
                  data-testid="btn-confirm-handover"
                >
                  <template #icon>
                    <FileCheck2 class="w-3 h-3 mr-1" />
                  </template>
                </Button>
              </div>
            </template>
          </Column>
        </DataTable>
      </div>
    </div>

    <!-- Modals -->
    <ArrangeSubstituteModal
      v-model:visible="isArrangeModalVisible"
      :academic-period-id="props.academicPeriodId"
      @submitted="() => refetchSubs()"
    />

    <ReassignmentModal
      v-model:visible="isReassignModalVisible"
      :academic-period-id="props.academicPeriodId"
      @submitted="() => refetchReas()"
    />

    <!-- Approve Exception Dialog for Substitution -->
    <Dialog
      v-model:visible="isApproveExceptionModalVisible"
      modal
      :draggable="false"
      header="Approve Escalated Substitute Exception"
      class="w-full max-w-md"
      data-testid="approve-sub-exception-dialog"
    >
      <div v-if="selectedSubForException" class="space-y-3 pt-2 text-xs">
        <p class="text-surface-600 dark:text-surface-300">
          The proposed substitute requires Principal / Authority exception approval under <strong>Resolution 3.8</strong> because of authorization mismatch or workload threshold limits.
        </p>

        <FormField label="Decision Remarks (Optional)">
          <Textarea
            v-model="exceptionDecisionRemarks"
            rows="2"
            placeholder="Authority justification for granting emergency coverage exception..."
            class="w-full text-xs"
            data-testid="input-sub-exception-remarks"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
          <Button
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isApproveExceptionModalVisible = false"
          />
          <Button
            label="Grant Authority Approval"
            severity="success"
            size="small"
            :loading="approveSubExceptionMutation.isPending.value"
            @click="handleConfirmApproveException"
            data-testid="btn-confirm-approve-sub-exception"
          />
        </div>
      </template>
    </Dialog>

    <!-- Confirm Handover Dialog -->
    <Dialog
      v-model:visible="isHandoverModalVisible"
      modal
      :draggable="false"
      header="Confirm Instructional Handover"
      class="w-full max-w-md"
      data-testid="confirm-handover-dialog"
    >
      <div v-if="selectedReassignmentForHandover" class="space-y-3 pt-2 text-xs">
        <p class="text-surface-600 dark:text-surface-300">
          Confirm that syllabus, instructional plans, grade records, and classroom duties have been handed over to
          <strong>{{ getTeacherName(selectedReassignmentForHandover.newTeacherId) }}</strong>.
        </p>

        <FormField label="Handover Status">
          <Select
            v-model="handoverStatusChoice"
            :options="[
              { label: 'Completed (Handover Sign-off Complete)', value: 'completed' },
              { label: 'Waived (No Handover Required)', value: 'waived' },
            ]"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="select-handover-status"
          />
        </FormField>

        <FormField label="Handover Notes (Optional)">
          <Textarea
            v-model="handoverNotes"
            rows="2"
            placeholder="Curriculum transition notes..."
            class="w-full text-xs"
            data-testid="input-handover-dialog-notes"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2 border-t border-surface-200 dark:border-surface-700">
          <Button
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            @click="isHandoverModalVisible = false"
          />
          <Button
            label="Save Handover Sign-off"
            severity="primary"
            size="small"
            :loading="confirmHandoverMutation.isPending.value"
            @click="handleConfirmHandover"
            data-testid="btn-save-handover"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
