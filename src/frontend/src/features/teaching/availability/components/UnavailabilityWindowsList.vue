<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Dialog from 'primevue/dialog';
import {
  Calendar,
  Clock,
  Plus,
  AlertTriangle,
  Ban,
  HeartPulse,
  GraduationCap,
  User,
  ShieldAlert,
  FileText,
} from 'lucide-vue-next';
import { StatusPill, EmptyState } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { TeacherUnavailabilityWindowDto, UnavailabilityType } from '../types';
import { useCancelUnavailabilityMutation } from '../composables/useAvailability';

export interface UnavailabilityWindowsListProps {
  windows?: TeacherUnavailabilityWindowDto[];
  isLoading?: boolean;
  teacherId?: string;
  canCancel?: boolean;
}

const props = withDefaults(defineProps<UnavailabilityWindowsListProps>(), {
  windows: () => [],
  isLoading: false,
  teacherId: undefined,
  canCancel: true,
});

const emit = defineEmits<{
  (e: 'add'): void;
  (e: 'cancel', window: TeacherUnavailabilityWindowDto): void;
}>();

const cancellingWindow = ref<TeacherUnavailabilityWindowDto | null>(null);
const isConfirmCancelVisible = ref(false);

const cancelMutation = useCancelUnavailabilityMutation({
  onSuccess: () => {
    isConfirmCancelVisible.value = false;
    cancellingWindow.value = null;
  },
});

function openCancelConfirmation(win: TeacherUnavailabilityWindowDto) {
  cancellingWindow.value = win;
  isConfirmCancelVisible.value = true;
}

function handleConfirmCancel() {
  if (!cancellingWindow.value) return;
  const win = cancellingWindow.value;
  emit('cancel', win);

  cancelMutation.mutate({
    windowId: win.id,
    teacherId: props.teacherId ?? win.teacherProfileId,
  });
}

function getLeaveTypeLabel(type: UnavailabilityType | string): string {
  switch (type) {
    case 'medical_leave':
      return 'Medical Leave';
    case 'official_training':
      return 'Official Training';
    case 'personal_leave':
      return 'Personal Leave';
    case 'blackout':
      return 'Blackout Window';
    default:
      return String(type).replace(/_/g, ' ');
  }
}

function getLeaveTypeBadgeClass(type: UnavailabilityType | string): string {
  switch (type) {
    case 'medical_leave':
      return 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-950/40 dark:text-rose-300 dark:border-rose-800';
    case 'official_training':
      return 'bg-sky-50 text-sky-700 border-sky-200 dark:bg-sky-950/40 dark:text-sky-300 dark:border-sky-800';
    case 'personal_leave':
      return 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800';
    case 'blackout':
      return 'bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800';
    default:
      return 'bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700';
  }
}

function isWindowActive(status: string): boolean {
  return status.toLowerCase() === 'active';
}

const sortedWindows = computed(() => {
  return [...props.windows].sort((a, b) => {
    // Active windows first, then by startDate descending
    if (a.status.toLowerCase() === 'active' && b.status.toLowerCase() !== 'active') return -1;
    if (a.status.toLowerCase() !== 'active' && b.status.toLowerCase() === 'active') return 1;
    return b.startDate.localeCompare(a.startDate);
  });
});
</script>

<template>
  <div class="space-y-4" data-testid="unavailability-windows-list">
    <!-- Header with Add Window Action -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h3 class="text-base font-semibold text-slate-900 dark:text-slate-100">
          Leave & Blackout Windows
        </h3>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          Recorded leaves, medical absences, and blackout dates. Existing teaching assignments remain intact but flag coverage requirements.
        </p>
      </div>

      <Button
        type="button"
        size="small"
        data-testid="add-unavailability-btn"
        class="text-xs py-1.5 px-3"
        @click="emit('add')"
      >
        <Plus class="w-3.5 h-3.5 mr-1" />
        Record Leave Window
      </Button>
    </div>

    <!-- Empty State -->
    <EmptyState
      v-if="!isLoading && (!sortedWindows || sortedWindows.length === 0)"
      title="No Unavailability Windows"
      description="You have no recorded leaves, official training, or blackout windows for this period."
      class="border border-dashed border-slate-200 dark:border-slate-700 py-10"
    >
      <template #actions>
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="text-xs"
          data-testid="empty-add-unavailability-btn"
          @click="emit('add')"
        >
          <Plus class="w-3.5 h-3.5 mr-1" />
          Record Leave Window
        </Button>
      </template>
    </EmptyState>

    <!-- Windows Data Table -->
    <div
      v-else
      class="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 shadow-sm"
    >
      <DataTable
        :value="sortedWindows"
        :loading="isLoading"
        data-key="id"
        class="text-sm"
        responsive-layout="scroll"
      >
        <!-- Type Column -->
        <Column header="Type" style="min-width: 170px">
          <template #body="{ data }">
            <span
              :class="[
                'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium border',
                getLeaveTypeBadgeClass(data.unavailabilityType),
              ]"
              :data-testid="`leave-type-badge-${data.id}`"
            >
              <HeartPulse v-if="data.unavailabilityType === 'medical_leave'" class="w-3.5 h-3.5" />
              <GraduationCap v-else-if="data.unavailabilityType === 'official_training'" class="w-3.5 h-3.5" />
              <User v-else-if="data.unavailabilityType === 'personal_leave'" class="w-3.5 h-3.5" />
              <Ban v-else class="w-3.5 h-3.5" />
              <span>{{ getLeaveTypeLabel(data.unavailabilityType) }}</span>
            </span>
          </template>
        </Column>

        <!-- Date & Time Range -->
        <Column header="Period / Time" style="min-width: 220px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-800 dark:text-slate-200">
                <Calendar class="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
                <span>{{ formatDisplayDate(data.startDate) }} – {{ formatDisplayDate(data.endDate) }}</span>
              </div>
              <div
                v-if="data.startTime || data.endTime"
                class="flex items-center gap-1 text-[11px] text-slate-500 dark:text-slate-400 pl-5"
              >
                <Clock class="w-3 h-3 text-slate-400 flex-shrink-0" />
                <span>{{ data.startTime || '00:00' }} – {{ data.endTime || '23:59' }}</span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Justification / Reason -->
        <Column header="Reason & Justification" style="min-width: 220px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="text-xs font-medium text-slate-800 dark:text-slate-200 line-clamp-2">
                {{ data.reason }}
              </div>
              <div
                v-if="data.notes"
                class="text-[11px] text-slate-500 dark:text-slate-400 italic line-clamp-1"
              >
                {{ data.notes }}
              </div>
            </div>
          </template>
        </Column>

        <!-- Status Badge -->
        <Column header="Status" style="width: 120px">
          <template #body="{ data }">
            <StatusPill
              :status="data.status"
              :label="data.status.toUpperCase()"
              size="sm"
              :data-testid="`status-pill-${data.id}`"
            />
          </template>
        </Column>

        <!-- Actions -->
        <Column header="Action" style="width: 120px" class="text-right">
          <template #body="{ data }">
            <Button
              v-if="isWindowActive(data.status) && canCancel"
              type="button"
              severity="danger"
              variant="outlined"
              size="small"
              class="text-xs py-1 px-2.5"
              :data-testid="`cancel-window-btn-${data.id}`"
              @click="openCancelConfirmation(data)"
            >
              Cancel
            </Button>
            <span
              v-else
              class="text-xs text-slate-400 dark:text-slate-500 italic"
            >
              No actions
            </span>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Cancel Confirmation Dialog -->
    <Dialog
      v-model:visible="isConfirmCancelVisible"
      modal
      header="Cancel Unavailability Window"
      :style="{ width: '440px' }"
      data-testid="cancel-confirm-dialog"
    >
      <div class="space-y-3 pt-1">
        <div class="flex items-start gap-3">
          <AlertTriangle class="w-6 h-6 text-amber-500 flex-shrink-0 mt-0.5" />
          <div class="space-y-1">
            <p class="text-sm text-slate-800 dark:text-slate-200 font-medium">
              Are you sure you want to cancel this leave window?
            </p>
            <p v-if="cancellingWindow" class="text-xs text-slate-600 dark:text-slate-400">
              {{ getLeaveTypeLabel(cancellingWindow.unavailabilityType) }}:
              {{ formatDisplayDate(cancellingWindow.startDate) }} to {{ formatDisplayDate(cancellingWindow.endDate) }}.
            </p>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              This will restore your scheduling availability for this period and cancel any pending emergency coverage requirements.
            </p>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            type="button"
            severity="secondary"
            variant="text"
            label="Dismiss"
            size="small"
            class="text-xs"
            @click="isConfirmCancelVisible = false"
          />
          <Button
            type="button"
            severity="danger"
            label="Confirm Cancellation"
            size="small"
            class="text-xs"
            :loading="cancelMutation.isPending.value"
            data-testid="confirm-cancel-window-submit-btn"
            @click="handleConfirmCancel"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
