<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import {
  BookOpen,
  Plus,
  Search,
  CheckCircle2,
  AlertTriangle,
  RotateCcw,
  ArrowLeftRight,
  ShieldCheck,
  Users,
  Award,
  Filter,
  XCircle,
  HelpCircle,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner, FormField } from '@/shared/components';
import {
  useAssignments,
  useAssignmentRoles,
  useCapacityConflicts,
  useApproveAssignmentMutation,
  useAcknowledgeAssignmentMutation,
  useUpdateAssignmentStatusMutation,
  useResolveConflictMutation,
} from '../composables/useAssignments';
import ProposeAssignmentModal from './ProposeAssignmentModal.vue';
import ApproveAssignmentModal from './ApproveAssignmentModal.vue';
import ArrangeSubstituteModal from './ArrangeSubstituteModal.vue';
import ReassignmentModal from './ReassignmentModal.vue';
import ConcludeAssignmentModal from './ConcludeAssignmentModal.vue';
import {
  ASSIGNMENT_STATUS_MAP,
  DEFAULT_FACULTY_CANDIDATES,
  PERIOD_PRESETS,
} from '../constants/assignmentWorkbenchConstants';
import type {
  TeachingAssignmentDto,
  TeachingAssignmentStatus,
  CapacityConflictDto,
} from '../types';

export interface ActiveAssignmentsTabProps {
  academicPeriodId?: string;
  campusScope?: string;
  search?: string;
  canManage?: boolean;
  canApprove?: boolean;
}

const props = withDefaults(defineProps<ActiveAssignmentsTabProps>(), {
  academicPeriodId: undefined,
  campusScope: undefined,
  search: '',
  canManage: true,
  canApprove: true,
});

const emit = defineEmits<{
  (e: 'assignmentSelected', assignment: TeachingAssignmentDto): void;
}>();

// Filter states
const searchQuery = ref<string>('');
const selectedStatus = ref<string>('all');
const homeroomFilter = ref<string>('all'); // 'all' | 'homeroom_only' | 'teaching_only'

const statusFilterOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Proposed (Awaiting Approval)', value: 'proposed' },
  { label: 'Approved (Active)', value: 'approved' },
  { label: 'Completed', value: 'completed' },
  { label: 'Suspended', value: 'suspended' },
  { label: 'Cancelled', value: 'cancelled' },
];

const homeroomFilterOptions = [
  { label: 'All Assignments', value: 'all' },
  { label: 'Homeroom Advisories Only (Wali Kelas)', value: 'homeroom_only' },
  { label: 'Regular Teaching Only', value: 'teaching_only' },
];

// Query assignments
const queryParams = computed(() => ({
  academicPeriodId: props.academicPeriodId || PERIOD_PRESETS[0].id,
  status: selectedStatus.value !== 'all' ? selectedStatus.value : undefined,
  search: searchQuery.value.trim() || (props.search ? props.search.trim() : undefined),
}));

const {
  data: assignmentsData,
  isLoading,
  isError,
  refetch,
} = useAssignments(queryParams);

const assignments = computed<TeachingAssignmentDto[]>(() => {
  if (!assignmentsData.value) return [];
  const rawList: TeachingAssignmentDto[] = Array.isArray(assignmentsData.value)
    ? assignmentsData.value
    : (assignmentsData.value as { items?: TeachingAssignmentDto[] }).items || [];

  return rawList.filter((a) => {
    const isHomeroom =
      a.assignmentScope === 'cohort_group' ||
      a.assignmentRoleName?.toLowerCase().includes('wali kelas') ||
      a.scopeLabel.toLowerCase().includes('wali kelas');

    if (homeroomFilter.value === 'homeroom_only') return isHomeroom;
    if (homeroomFilter.value === 'teaching_only') return !isHomeroom;
    return true;
  });
});

// Capacity conflicts query to detect assignment / teacher conflicts
const { data: conflictsData } = useCapacityConflicts(
  computed(() => ({
    academicPeriodId: props.academicPeriodId || PERIOD_PRESETS[0].id,
    status: 'unresolved',
  })),
);

const conflicts = computed<CapacityConflictDto[]>(() => {
  if (!conflictsData.value) return [];
  return Array.isArray(conflictsData.value)
    ? conflictsData.value
    : (conflictsData.value as { items?: CapacityConflictDto[] }).items || [];
});

function getConflictForAssignment(assignment: TeachingAssignmentDto): CapacityConflictDto | undefined {
  return conflicts.value.find(
    (c) =>
      c.teachingAssignmentId === assignment.id ||
      c.teacherProfileId === assignment.teacherProfileId,
  );
}

// Co-teaching groupings helper: detects if another assignment shares the same scope reference
const coTeachingMap = computed(() => {
  const map = new Map<string, number>();
  for (const a of assignments.value) {
    const key = a.scopeReferenceId || a.scopeLabel;
    map.set(key, (map.get(key) || 0) + 1);
  }
  return map;
});

function isCoTeaching(assignment: TeachingAssignmentDto): boolean {
  const count = coTeachingMap.value.get(assignment.scopeReferenceId || assignment.scopeLabel) || 0;
  return count > 1;
}

// Teacher Name Resolver
function getTeacherName(teacherProfileId: string): string {
  const match = DEFAULT_FACULTY_CANDIDATES.find((f) => f.id === teacherProfileId);
  return match ? `${match.fullName} (${match.teacherNumber})` : teacherProfileId.slice(0, 8);
}

// Modals State
const isProposeModalVisible = ref<boolean>(false);
const isApproveModalVisible = ref<boolean>(false);
const isSubstituteModalVisible = ref<boolean>(false);
const isReassignModalVisible = ref<boolean>(false);
const isConcludeModalVisible = ref<boolean>(false);
const selectedAssignment = ref<TeachingAssignmentDto | null>(null);

// Status actions (suspend, cancel)
const isStatusModalVisible = ref<boolean>(false);
const targetStatus = ref<string>('suspended');
const statusReason = ref<string>('');

const updateStatusMutation = useUpdateAssignmentStatusMutation();
const acknowledgeMutation = useAcknowledgeAssignmentMutation();
const resolveConflictMutation = useResolveConflictMutation();

// Conflict resolution modal
const isResolveConflictModalVisible = ref<boolean>(false);
const selectedConflict = ref<CapacityConflictDto | null>(null);
const conflictResolutionAction = ref<string>('reallocated_hours');
const conflictResolutionNotes = ref<string>('');

function handleOpenPropose() {
  selectedAssignment.value = null;
  isProposeModalVisible.value = true;
}

function handleOpenApprove(assignment: TeachingAssignmentDto) {
  selectedAssignment.value = assignment;
  isApproveModalVisible.value = true;
}

function handleOpenSubstitute(assignment: TeachingAssignmentDto) {
  selectedAssignment.value = assignment;
  isSubstituteModalVisible.value = true;
}

function handleOpenReassign(assignment: TeachingAssignmentDto) {
  selectedAssignment.value = assignment;
  isReassignModalVisible.value = true;
}

function handleOpenConclude(assignment: TeachingAssignmentDto) {
  selectedAssignment.value = assignment;
  isConcludeModalVisible.value = true;
}

function handleAcknowledge(assignment: TeachingAssignmentDto) {
  acknowledgeMutation.mutate(assignment.id);
}

function handleOpenStatusChange(assignment: TeachingAssignmentDto, status: string) {
  selectedAssignment.value = assignment;
  targetStatus.value = status;
  statusReason.value = '';
  isStatusModalVisible.value = true;
}

function handleConfirmStatusChange() {
  if (!selectedAssignment.value) return;

  updateStatusMutation.mutate(
    {
      id: selectedAssignment.value.id,
      payload: {
        status: targetStatus.value,
        reason: statusReason.value.trim() || undefined,
      },
    },
    {
      onSuccess: () => {
        isStatusModalVisible.value = false;
      },
    },
  );
}

function handleOpenResolveConflict(conflict: CapacityConflictDto) {
  selectedConflict.value = conflict;
  conflictResolutionAction.value = 'reallocated_hours';
  conflictResolutionNotes.value = '';
  isResolveConflictModalVisible.value = true;
}

function handleConfirmResolveConflict() {
  if (!selectedConflict.value) return;

  resolveConflictMutation.mutate(
    {
      id: selectedConflict.value.id,
      payload: {
        status: 'resolved',
        resolutionAction: conflictResolutionAction.value,
        resolutionNotes: conflictResolutionNotes.value.trim() || undefined,
      },
    },
    {
      onSuccess: () => {
        isResolveConflictModalVisible.value = false;
      },
    },
  );
}
</script>

<template>
  <div class="space-y-4" data-testid="active-assignments-tab">
    <!-- Toolbar with Search, Status, and Homeroom Filter -->
    <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-surface-50/60 dark:bg-surface-800/40 p-3 rounded-lg border border-surface-200 dark:border-surface-700">
      <div class="flex flex-wrap items-center gap-2.5 flex-1">
        <!-- Search Input -->
        <div class="relative w-full sm:w-64">
          <Search class="w-4 h-4 text-surface-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <InputText
            v-model="searchQuery"
            placeholder="Search assignments or teacher..."
            class="w-full !pl-9 !text-xs"
            data-testid="input-assignment-search"
          />
        </div>

        <!-- Status Filter -->
        <Select
          v-model="selectedStatus"
          :options="statusFilterOptions"
          option-label="label"
          option-value="value"
          class="!text-xs w-48"
          data-testid="select-assignment-status-filter"
        />

        <!-- Homeroom Advisory Filter -->
        <Select
          v-model="homeroomFilter"
          :options="homeroomFilterOptions"
          option-label="label"
          option-value="value"
          class="!text-xs w-60"
          data-testid="select-homeroom-filter"
        />
      </div>

      <!-- Action Button -->
      <div class="flex items-center gap-2">
        <Button
          v-if="canManage"
          label="Propose Assignment"
          severity="primary"
          size="small"
          @click="handleOpenPropose"
          data-testid="btn-open-propose-assignment"
        >
          <template #icon>
            <Plus class="w-3.5 h-3.5 mr-1.5" />
          </template>
        </Button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="p-12 flex justify-center items-center">
      <LoadingSpinner />
    </div>

    <!-- Error State -->
    <div
      v-else-if="isError"
      class="p-8 text-center bg-rose-50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-800 rounded-xl"
    >
      <AlertTriangle class="w-8 h-8 text-rose-500 mx-auto mb-2" />
      <p class="text-sm font-semibold text-rose-900 dark:text-rose-200">Failed to load teaching assignments</p>
      <Button
        label="Try Again"
        severity="danger"
        variant="text"
        size="small"
        class="mt-2"
        @click="() => refetch()"
      />
    </div>

    <!-- Empty State -->
    <div
      v-else-if="assignments.length === 0"
      class="p-12 text-center bg-surface-50/50 dark:bg-surface-800/20 border border-surface-200 dark:border-surface-700 rounded-xl"
      data-testid="empty-assignments-state"
    >
      <EmptyState
        title="No Teaching Assignments Found"
        description="No assignments match the selected status and advisory criteria for this academic period."
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-primary-50 dark:bg-primary-950 flex items-center justify-center mx-auto mb-3 text-primary-600 dark:text-primary-400">
            <BookOpen class="w-6 h-6" />
          </div>
        </template>
        <template #actions>
          <Button
            v-if="canManage"
            label="Propose First Assignment"
            severity="primary"
            size="small"
            @click="handleOpenPropose"
          />
        </template>
      </EmptyState>
    </div>

    <!-- Assignments Table -->
    <div
      v-else
      class="border border-surface-200 dark:border-surface-700 rounded-xl overflow-hidden shadow-xs"
      data-testid="active-assignments-table-container"
    >
      <DataTable
        :value="assignments"
        responsive-layout="scroll"
        class="!text-xs"
        data-testid="active-assignments-table"
      >
        <!-- Assignment Number -->
        <Column header="Assignment #" class="w-36 font-mono font-medium">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="text-surface-900 dark:text-surface-100 font-semibold">{{ data.assignmentNumber }}</span>
              <span class="block text-[10px] text-surface-400">{{ data.effectiveStartDate }}</span>
            </div>
          </template>
        </Column>

        <!-- Assigned Teacher -->
        <Column header="Assigned Faculty" class="min-w-44">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="font-medium text-surface-900 dark:text-surface-100 block">
                {{ getTeacherName(data.teacherProfileId) }}
              </span>
              <span
                v-if="data.teacherAcknowledged"
                class="inline-flex items-center gap-1 text-[10px] text-emerald-600 dark:text-emerald-400"
              >
                <CheckCircle2 class="w-3 h-3" /> Acknowledged
              </span>
              <span
                v-else
                class="inline-flex items-center gap-1 text-[10px] text-surface-400"
              >
                Pending Acknowledgment
              </span>
            </div>
          </template>
        </Column>

        <!-- Scope & Co-Teaching Groupings -->
        <Column header="Scope & Grouping" class="min-w-52">
          <template #body="{ data }">
            <div class="space-y-1">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span class="font-medium text-surface-900 dark:text-surface-100">
                  {{ data.scopeLabel }}
                </span>
                <!-- Homeroom Advisory Decoupled Badge (TD-06) -->
                <span
                  v-if="data.assignmentScope === 'cohort_group' || data.assignmentRoleName?.includes('Wali Kelas')"
                  class="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-purple-100 text-purple-800 dark:bg-purple-950 dark:text-purple-300"
                >
                  Wali Kelas
                </span>
                <!-- Co-Teaching Grouping Badge -->
                <span
                  v-if="isCoTeaching(data)"
                  class="px-1.5 py-0.5 rounded text-[10px] font-medium bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300 flex items-center gap-1"
                >
                  <Users class="w-2.5 h-2.5" /> Co-teaching
                </span>
              </div>
              <span class="block text-[10px] text-surface-400 font-mono">
                Scope Ref: {{ data.scopeReferenceId?.slice(0, 8) }}...
              </span>
            </div>
          </template>
        </Column>

        <!-- Role Badge -->
        <Column header="Role" class="w-40">
          <template #body="{ data }">
            <span
              class="px-2 py-0.5 rounded-full text-[11px] font-medium inline-flex items-center gap-1"
              :class="
                data.assignmentRoleName?.toLowerCase().includes('utama') || data.assignmentRoleName?.toLowerCase().includes('lead')
                  ? 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-300'
                  : 'bg-surface-100 text-surface-800 dark:bg-surface-800 dark:text-surface-300'
              "
            >
              <Award v-if="data.assignmentRoleName?.toLowerCase().includes('utama')" class="w-3 h-3 text-amber-600" />
              {{ data.assignmentRoleName || 'Guru Utama' }}
            </span>
          </template>
        </Column>

        <!-- Workload Hours -->
        <Column header="Workload" class="w-28 text-right font-mono">
          <template #body="{ data }">
            <span class="font-semibold text-surface-900 dark:text-surface-100">
              {{ data.workloadCommitmentValue }}
            </span>
            <span class="text-[10px] text-surface-400 block">hrs/wk</span>
          </template>
        </Column>

        <!-- Capacity Conflict Warning Indicator -->
        <Column header="Capacity Alert" class="w-32">
          <template #body="{ data }">
            <div v-if="getConflictForAssignment(data)" class="space-y-0.5">
              <span
                class="px-1.5 py-0.5 rounded text-[10px] font-medium inline-flex items-center gap-1 cursor-pointer"
                :class="
                  getConflictForAssignment(data)?.severity === 'blocking'
                    ? 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300'
                    : 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300'
                "
                @click="handleOpenResolveConflict(getConflictForAssignment(data)!)"
              >
                <AlertTriangle class="w-2.5 h-2.5" />
                {{ getConflictForAssignment(data)?.severity }}
              </span>
              <span class="block text-[9px] text-surface-500 truncate max-w-28">
                {{ getConflictForAssignment(data)?.conflictType }}
              </span>
            </div>
            <span v-else class="text-[11px] text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
              <CheckCircle2 class="w-3 h-3" /> Clear
            </span>
          </template>
        </Column>

        <!-- Status Pill -->
        <Column header="Status" class="w-32">
          <template #body="{ data }">
            <StatusPill
              :status="data.status"
              :variant="ASSIGNMENT_STATUS_MAP[data.status]?.variant || 'slate'"
              :label="ASSIGNMENT_STATUS_MAP[data.status]?.label || data.status"
            />
          </template>
        </Column>

        <!-- Actions -->
        <Column header="Actions" class="w-56 text-right">
          <template #body="{ data }">
            <div class="flex items-center justify-end gap-1">
              <!-- Approve (if proposed) -->
              <Button
                v-if="data.status === 'proposed' && canApprove"
                label="Approve"
                severity="success"
                variant="outlined"
                size="small"
                class="!py-1 !px-2 !text-xs"
                @click="handleOpenApprove(data)"
                data-testid="btn-approve-assignment"
              >
                <template #icon>
                  <ShieldCheck class="w-3.5 h-3.5 mr-1" />
                </template>
              </Button>

              <!-- Acknowledge (if not acknowledged) -->
              <Button
                v-if="data.status === 'approved' && !data.teacherAcknowledged"
                label="Ack"
                severity="secondary"
                variant="outlined"
                size="small"
                class="!py-1 !px-2 !text-xs"
                @click="handleAcknowledge(data)"
                title="Acknowledge Assignment"
                data-testid="btn-ack-assignment"
              />

              <!-- Arrange Substitute Shortcut -->
              <Button
                v-if="data.status === 'approved' && canManage"
                severity="secondary"
                variant="text"
                size="small"
                class="!p-1.5"
                title="Arrange Substitute"
                @click="handleOpenSubstitute(data)"
                data-testid="btn-substitute-shortcut"
              >
                <ArrowLeftRight class="w-3.5 h-3.5 text-amber-600" />
              </Button>

              <!-- Permanent Reassign Shortcut -->
              <Button
                v-if="data.status === 'approved' && canManage"
                severity="secondary"
                variant="text"
                size="small"
                class="!p-1.5"
                title="Propose Reassignment"
                @click="handleOpenReassign(data)"
                data-testid="btn-reassign-shortcut"
              >
                <RotateCcw class="w-3.5 h-3.5 text-indigo-600" />
              </Button>

              <!-- Conclude Shortcut -->
              <Button
                v-if="data.status === 'approved' && canManage"
                severity="secondary"
                variant="text"
                size="small"
                class="!p-1.5"
                title="Conclude Assignment & Release Capacity"
                @click="handleOpenConclude(data)"
                data-testid="btn-conclude-shortcut"
              >
                <CheckCircle2 class="w-3.5 h-3.5 text-emerald-600" />
              </Button>

              <!-- Suspend / Cancel Action -->
              <Button
                v-if="data.status === 'approved' && canManage"
                severity="danger"
                variant="text"
                size="small"
                class="!p-1.5"
                title="Suspend Assignment"
                @click="handleOpenStatusChange(data, 'suspended')"
                data-testid="btn-suspend-shortcut"
              >
                <XCircle class="w-3.5 h-3.5 text-rose-500" />
              </Button>
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Modals -->
    <ProposeAssignmentModal
      v-model:visible="isProposeModalVisible"
      :academic-period-id="props.academicPeriodId"
      @submitted="() => refetch()"
    />

    <ApproveAssignmentModal
      v-model:visible="isApproveModalVisible"
      :assignment="selectedAssignment"
      @approved="() => refetch()"
    />

    <ArrangeSubstituteModal
      v-model:visible="isSubstituteModalVisible"
      :preselected-assignment="selectedAssignment"
      :academic-period-id="props.academicPeriodId"
      @submitted="() => refetch()"
    />

    <ReassignmentModal
      v-model:visible="isReassignModalVisible"
      :preselected-assignment="selectedAssignment"
      :academic-period-id="props.academicPeriodId"
      @submitted="() => refetch()"
    />

    <ConcludeAssignmentModal
      v-model:visible="isConcludeModalVisible"
      :assignment="selectedAssignment"
      @concluded="() => refetch()"
    />

    <!-- Quick Status Change Dialog (Suspend / Cancel) -->
    <Dialog
      v-model:visible="isStatusModalVisible"
      modal
      :draggable="false"
      :header="`Confirm Assignment ${targetStatus === 'suspended' ? 'Suspension' : 'Cancellation'}`"
      class="w-full max-w-md"
      data-testid="status-change-dialog"
    >
      <div class="space-y-3 pt-2 text-xs">
        <p class="text-surface-600 dark:text-surface-300">
          Are you sure you want to change status of assignment
          <strong class="font-mono text-surface-900 dark:text-surface-100">{{ selectedAssignment?.assignmentNumber }}</strong>
          to <strong>{{ targetStatus }}</strong>? This will remove committed hours from the teacher's active workload.
        </p>

        <FormField label="Reason (Optional)">
          <Textarea
            v-model="statusReason"
            rows="2"
            placeholder="Reason for suspension or cancellation..."
            class="w-full text-xs"
            data-testid="input-status-reason"
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
            @click="isStatusModalVisible = false"
          />
          <Button
            :label="`Confirm ${targetStatus === 'suspended' ? 'Suspension' : 'Cancellation'}`"
            severity="danger"
            size="small"
            :loading="updateStatusMutation.isPending.value"
            @click="handleConfirmStatusChange"
            data-testid="btn-confirm-status-change"
          />
        </div>
      </template>
    </Dialog>

    <!-- Conflict Resolution Dialog -->
    <Dialog
      v-model:visible="isResolveConflictModalVisible"
      modal
      :draggable="false"
      header="Resolve Capacity Conflict"
      class="w-full max-w-md"
      data-testid="resolve-conflict-dialog"
    >
      <div v-if="selectedConflict" class="space-y-3 pt-2 text-xs">
        <div class="p-3 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 space-y-1">
          <span class="font-semibold text-surface-900 dark:text-surface-100">{{ selectedConflict.conflictType }}</span>
          <p class="text-surface-600 dark:text-surface-300">{{ selectedConflict.description }}</p>
        </div>

        <FormField label="Resolution Action">
          <InputText
            v-model="conflictResolutionAction"
            placeholder="e.g. reallocated_hours / substitute_assigned / exception_granted"
            class="w-full text-xs"
            data-testid="input-conflict-action"
          />
        </FormField>

        <FormField label="Resolution Notes">
          <Textarea
            v-model="conflictResolutionNotes"
            rows="2"
            placeholder="Notes on resolution..."
            class="w-full text-xs"
            data-testid="input-conflict-notes"
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
            @click="isResolveConflictModalVisible = false"
          />
          <Button
            label="Mark Conflict Resolved"
            severity="success"
            size="small"
            :loading="resolveConflictMutation.isPending.value"
            @click="handleConfirmResolveConflict"
            data-testid="btn-confirm-resolve-conflict"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
