<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  Search,
  RefreshCw,
  ExternalLink,
  Paperclip,
  FileText,
  AlertTriangle,
  Eye,
  ShieldAlert,
  Users,
  CheckCircle2,
  Clock,
  RotateCcw,
} from 'lucide-vue-next';
import { StatusPill, EmptyState, LoadingSpinner } from '@/shared/components';
import { formatDisplayDateTime } from '@/shared/utils/dateUtils';
import type { Assignment, AssignmentSubmission, SubmissionDisplayStatus } from '../types';

interface Props {
  assignment: Assignment | null;
  submissions: AssignmentSubmission[];
  isLoading?: boolean;
  isError?: boolean;
  errorMessage?: string | null;
  initialStatusFilter?: string;
}

const props = withDefaults(defineProps<Props>(), {
  isLoading: false,
  isError: false,
  errorMessage: null,
  initialStatusFilter: 'all',
});

const emit = defineEmits<{
  (e: 'inspect', submission: AssignmentSubmission): void;
  (e: 'grantException', studentId: string): void;
  (e: 'refresh'): void;
}>();

const searchFilter = ref('');
const statusFilter = ref<string>(props.initialStatusFilter || 'all');

watch(
  () => props.initialStatusFilter,
  (val) => {
    if (val) {
      statusFilter.value = val;
    }
  },
  { immediate: true },
);

const statusOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Submitted (On-Time)', value: 'submitted' },
  { label: 'Late', value: 'late' },
  { label: 'Missing (Overdue)', value: 'missing' },
  { label: 'Excused', value: 'excused' },
  { label: 'Exempted', value: 'exempted' },
  { label: 'Pending', value: 'pending' },
];

const filteredSubmissions = computed(() => {
  return props.submissions.filter((item) => {
    // Status filter
    if (statusFilter.value !== 'all') {
      if (item.displayStatus?.toLowerCase() !== statusFilter.value.toLowerCase()) {
        return false;
      }
    }

    // Search filter
    if (searchFilter.value.trim()) {
      const term = searchFilter.value.trim().toLowerCase();
      const matchName = item.studentName?.toLowerCase().includes(term);
      const matchNumber = item.studentNumber?.toLowerCase().includes(term);
      const matchSection = item.sectionLabel?.toLowerCase().includes(term);
      if (!matchName && !matchNumber && !matchSection) {
        return false;
      }
    }

    return true;
  });
});

// Summary metrics
const totalCount = computed(() => props.submissions.length);
const submittedCount = computed(
  () =>
    props.submissions.filter(
      (s) => s.displayStatus === 'submitted' || s.displayStatus === 'late',
    ).length,
);
const missingCount = computed(
  () => props.submissions.filter((s) => s.displayStatus === 'missing').length,
);
const exceptionCount = computed(
  () =>
    props.submissions.filter(
      (s) => s.displayStatus === 'excused' || s.displayStatus === 'exempted',
    ).length,
);

function getReferenceKind(ref?: string | null): 'url' | 'file' | 'text' | null {
  if (!ref?.trim()) return null;
  if (/^https?:\/\//i.test(ref)) return 'url';
  if (/^uploads\//i.test(ref) || /\.(pdf|docx?|xlsx?|png|jpe?g|zip|txt)$/i.test(ref)) {
    return 'file';
  }
  return 'text';
}
</script>

<template>
  <div class="space-y-4" data-testid="submissions-review-table-container">
    <!-- 1. Stats Summary Bar -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <!-- Total Enrolled -->
      <div class="p-3 bg-white rounded-xl border border-slate-200/80 shadow-xs flex items-center justify-between">
        <div>
          <p class="text-xs text-slate-500 font-medium">Total Roster</p>
          <p class="text-xl font-bold text-slate-900 mt-0.5">{{ totalCount }}</p>
        </div>
        <div class="w-9 h-9 rounded-lg bg-slate-100 flex items-center justify-center text-slate-600">
          <Users class="w-4 h-4" />
        </div>
      </div>

      <!-- Submitted -->
      <div class="p-3 bg-white rounded-xl border border-slate-200/80 shadow-xs flex items-center justify-between">
        <div>
          <p class="text-xs text-emerald-600 font-medium">Submitted</p>
          <p class="text-xl font-bold text-slate-900 mt-0.5">{{ submittedCount }}</p>
        </div>
        <div class="w-9 h-9 rounded-lg bg-emerald-50 flex items-center justify-center text-emerald-600">
          <CheckCircle2 class="w-4 h-4" />
        </div>
      </div>

      <!-- Missing (Overdue Dynamic Projection) in Red -->
      <div
        class="p-3 bg-white rounded-xl border border-slate-200/80 shadow-xs flex items-center justify-between"
        :class="{ 'border-rose-300 ring-1 ring-rose-200': missingCount > 0 }"
      >
        <div>
          <p class="text-xs text-rose-600 font-medium">Missing (Overdue)</p>
          <p class="text-xl font-bold text-rose-700 mt-0.5">{{ missingCount }}</p>
        </div>
        <div
          class="w-9 h-9 rounded-lg flex items-center justify-center"
          :class="missingCount > 0 ? 'bg-rose-100 text-rose-700' : 'bg-slate-100 text-slate-500'"
        >
          <AlertTriangle class="w-4 h-4" />
        </div>
      </div>

      <!-- Policy Exceptions -->
      <div class="p-3 bg-white rounded-xl border border-slate-200/80 shadow-xs flex items-center justify-between">
        <div>
          <p class="text-xs text-amber-600 font-medium">Exceptions</p>
          <p class="text-xl font-bold text-slate-900 mt-0.5">{{ exceptionCount }}</p>
        </div>
        <div class="w-9 h-9 rounded-lg bg-amber-50 flex items-center justify-center text-amber-600">
          <ShieldAlert class="w-4 h-4" />
        </div>
      </div>
    </div>

    <!-- 2. Controls & Search Toolbar -->
    <div class="p-4 bg-white rounded-xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
      <div class="flex items-center gap-3 flex-1">
        <div class="relative flex-1 max-w-sm">
          <Search class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <InputText
            v-model="searchFilter"
            placeholder="Search student name or number..."
            class="w-full !pl-9"
            size="small"
            data-testid="submissions-search-input"
          />
        </div>

        <Select
          v-model="statusFilter"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          size="small"
          class="w-48"
          data-testid="submissions-status-filter"
        />
      </div>

      <div class="flex items-center gap-2">
        <Button
          type="button"
          icon="pi pi-refresh"
          severity="secondary"
          variant="outlined"
          size="small"
          :loading="isLoading"
          title="Refresh roster"
          @click="emit('refresh')"
        />
      </div>
    </div>

    <!-- 3. Submissions DataTable -->
    <div class="bg-white rounded-xl border border-slate-200/80 shadow-xs overflow-hidden">
      <div v-if="isLoading" class="py-12 flex justify-center items-center">
        <LoadingSpinner label="Loading student submission roster..." />
      </div>

      <div v-else-if="isError" class="p-8 text-center space-y-3">
        <AlertTriangle class="w-10 h-10 text-rose-500 mx-auto" />
        <h4 class="text-sm font-semibold text-slate-800">Unable to load submission roster</h4>
        <p class="text-xs text-slate-500">{{ errorMessage ?? 'An error occurred while projecting student submissions.' }}</p>
        <Button size="small" severity="secondary" variant="outlined" @click="emit('refresh')">
          Retry
        </Button>
      </div>

      <DataTable
        v-else
        :value="filteredSubmissions"
        data-key="studentId"
        striped-rows
        responsive-layout="scroll"
        class="text-xs"
        data-testid="submissions-datatable"
      >
        <template #empty>
          <div class="py-8">
            <EmptyState
              title="No participants found"
              message="No student submission records match the current filter criteria."
            />
          </div>
        </template>

        <!-- Column 1: Student Name & Identifier -->
        <Column header="Student Participant" sortable field="studentName" style="min-width: 200px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="font-medium text-slate-900 block leading-tight">
                {{ data.studentName ?? 'Unknown Student' }}
              </span>
              <div class="flex items-center gap-1.5 text-[11px] text-slate-500">
                <span class="font-mono">{{ data.studentNumber ?? data.studentId }}</span>
                <span v-if="data.sectionLabel">&bull;</span>
                <span
                  v-if="data.sectionLabel"
                  class="inline-flex items-center px-1.5 py-0.2 rounded bg-slate-100 text-slate-700 font-medium"
                >
                  {{ data.sectionLabel }}
                </span>
              </div>
            </div>
          </template>
        </Column>

        <!-- Column 2: Dynamic Submission Status Pill -->
        <Column header="Status" sortable field="displayStatus" style="min-width: 140px">
          <template #body="{ data }">
            <!-- Dynamic Missing indicator in red (GAP-007, TD-06) -->
            <StatusPill
              v-if="data.displayStatus === 'missing'"
              status="missing"
              variant="rose"
              label="Missing"
              class="font-semibold shadow-xs ring-1 ring-rose-200"
            />
            <StatusPill
              v-else-if="data.displayStatus === 'late'"
              status="late"
              variant="amber"
              label="Late"
            />
            <StatusPill
              v-else-if="data.displayStatus === 'submitted'"
              status="submitted"
              variant="sky"
              label="Submitted"
            />
            <StatusPill
              v-else-if="data.displayStatus === 'excused'"
              status="excused"
              variant="amber"
              label="Excused"
            />
            <StatusPill
              v-else-if="data.displayStatus === 'exempted'"
              status="exempted"
              variant="slate"
              label="Exempted"
            />
            <StatusPill
              v-else
              status="pending"
              variant="amber"
              label="Pending"
            />
          </template>
        </Column>

        <!-- Column 3: Submitted Date -->
        <Column header="Submitted Date" sortable field="submittedDate" style="min-width: 160px">
          <template #body="{ data }">
            <span v-if="data.submittedDate" class="text-slate-700 font-medium">
              {{ formatDisplayDateTime(data.submittedDate) }}
            </span>
            <span v-else class="text-slate-400 italic">—</span>
          </template>
        </Column>

        <!-- Column 4: Deliverable / Reference Preview -->
        <Column header="Deliverable" style="min-width: 150px">
          <template #body="{ data }">
            <div v-if="data.submissionReference" class="flex items-center gap-1.5">
              <!-- URL Reference -->
              <span
                v-if="getReferenceKind(data.submissionReference) === 'url'"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200 text-[11px] font-medium"
                title="External Link"
              >
                <ExternalLink class="w-3 h-3 text-blue-600" />
                <span class="truncate max-w-[100px]">Link</span>
              </span>

              <!-- File Attachment -->
              <span
                v-else-if="getReferenceKind(data.submissionReference) === 'file'"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-slate-100 text-slate-700 border border-slate-200 text-[11px] font-medium"
                title="Uploaded File"
              >
                <Paperclip class="w-3 h-3 text-slate-600" />
                <span class="truncate max-w-[100px]">File Attachment</span>
              </span>

              <!-- Plain Text -->
              <span
                v-else
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-slate-100 text-slate-700 border border-slate-200 text-[11px] font-medium"
                title="Text Response"
              >
                <FileText class="w-3 h-3 text-slate-600" />
                <span class="truncate max-w-[100px]">Text Response</span>
              </span>
            </div>
            <span v-else class="text-slate-400 italic">—</span>
          </template>
        </Column>

        <!-- Column 5: Resubmission Tag -->
        <Column header="Attempt" style="min-width: 110px">
          <template #body="{ data }">
            <Tag
              v-if="data.isResubmission"
              value="Resubmission"
              severity="warn"
              class="text-[10px] font-medium"
            />
            <span v-else class="text-slate-400 text-xs italic">First attempt</span>
          </template>
        </Column>

        <!-- Column 6: Actions -->
        <Column header="Actions" align-frozen="right" style="min-width: 140px">
          <template #body="{ data }">
            <div class="flex items-center gap-1.5">
              <Button
                type="button"
                icon="pi pi-eye"
                size="small"
                severity="secondary"
                variant="text"
                class="!p-1.5 text-slate-600 hover:text-slate-900"
                title="Inspect submission deliverable"
                data-testid="inspect-submission-btn"
                @click="emit('inspect', data)"
              />
              <Button
                type="button"
                icon="pi pi-shield"
                size="small"
                severity="warn"
                variant="text"
                class="!p-1.5 text-amber-600 hover:text-amber-800"
                title="Grant policy exception (excuse/exempt)"
                data-testid="table-exception-btn"
                @click="emit('grantException', data.studentId)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>
