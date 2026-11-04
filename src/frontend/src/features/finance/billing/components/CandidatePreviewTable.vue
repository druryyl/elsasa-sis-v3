<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import InputText from 'primevue/inputtext';
import { Search, AlertTriangle, CheckCircle2, User, Layers, Calendar } from 'lucide-vue-next';
import type { BatchAssessCandidateDto } from '../../obligations/types/obligations.types';
import { formatIdr } from '../../shared/currency.utils';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { EmptyState } from '@/shared/components';

const props = withDefaults(
  defineProps<{
    candidates?: BatchAssessCandidateDto[];
    loading?: boolean;
    rows?: number;
  }>(),
  {
    candidates: () => [],
    loading: false,
    rows: 10,
  },
);

const searchQuery = ref('');

const filteredCandidates = computed(() => {
  if (!searchQuery.value.trim()) {
    return props.candidates;
  }
  const q = searchQuery.value.trim().toLowerCase();
  return props.candidates.filter(
    (c) =>
      c.studentFullName.toLowerCase().includes(q) ||
      c.studentNumber.toLowerCase().includes(q) ||
      c.grade.toLowerCase().includes(q) ||
      c.campus.toLowerCase().includes(q),
  );
});
</script>

<template>
  <div class="space-y-3" data-testid="candidate-preview-container">
    <!-- Header with Search and Summary Count -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div class="text-xs text-slate-500 font-medium">
        Showing {{ filteredCandidates.length }} of {{ props.candidates.length }} evaluated students
      </div>
      <div class="relative w-full sm:w-72">
        <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <InputText
          v-model="searchQuery"
          placeholder="Filter by name, student #, grade..."
          class="w-full pl-9 text-xs py-1.5"
          data-testid="candidate-search-input"
        />
      </div>
    </div>

    <!-- Candidate DataTable -->
    <DataTable
      :value="filteredCandidates"
      :loading="props.loading"
      :paginator="filteredCandidates.length > props.rows"
      :rows="props.rows"
      :rowsPerPageOptions="[10, 20, 50, 100]"
      responsiveLayout="scroll"
      dataKey="studentId"
      class="text-xs border border-slate-200 rounded-lg overflow-hidden bg-white shadow-xs"
      data-testid="candidate-preview-table"
    >
      <template #empty>
        <div class="py-8">
          <EmptyState
            title="No Candidates Found"
            description="No student candidates match the current search filters."
          />
        </div>
      </template>

      <!-- Student Number -->
      <Column field="studentNumber" header="Student #" sortable style="min-width: 140px">
        <template #body="{ data }">
          <div class="flex items-center gap-2">
            <User class="w-3.5 h-3.5 text-slate-400 shrink-0" />
            <span class="font-mono font-medium text-slate-800" data-testid="student-number">
              {{ data.studentNumber }}
            </span>
          </div>
        </template>
      </Column>

      <!-- Student Full Name -->
      <Column field="studentFullName" header="Student Name" sortable style="min-width: 200px">
        <template #body="{ data }">
          <div class="font-semibold text-slate-900" data-testid="student-name">
            {{ data.studentFullName }}
          </div>
        </template>
      </Column>

      <!-- Campus -->
      <Column field="campus" header="Campus" sortable style="min-width: 130px">
        <template #body="{ data }">
          <span class="text-slate-700">
            {{ data.campus || '—' }}
          </span>
        </template>
      </Column>

      <!-- Grade -->
      <Column field="grade" header="Grade" sortable style="min-width: 100px">
        <template #body="{ data }">
          <div class="inline-flex items-center gap-1 text-slate-700">
            <Layers class="w-3 h-3 text-slate-400" />
            <span class="font-medium" data-testid="student-grade">{{ data.grade || '—' }}</span>
          </div>
        </template>
      </Column>

      <!-- Base Amount -->
      <Column field="baseAmount" header="Amount" sortable style="min-width: 140px">
        <template #body="{ data }">
          <span class="font-mono font-semibold text-slate-800" data-testid="candidate-amount">
            {{ formatIdr(data.baseAmount) }}
          </span>
        </template>
      </Column>

      <!-- Due Date -->
      <Column field="dueDate" header="Due Date" sortable style="min-width: 120px">
        <template #body="{ data }">
          <div class="inline-flex items-center gap-1.5 text-slate-600">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            <span>{{ formatDisplayDate(data.dueDate) }}</span>
          </div>
        </template>
      </Column>

      <!-- Status & Duplicate Badge -->
      <Column header="Validation Status" style="min-width: 150px">
        <template #body="{ data }">
          <div v-if="data.isDuplicate" class="flex items-center gap-1.5">
            <Tag
              severity="warn"
              value="Duplicate (Excluded)"
              class="text-[11px] font-medium"
              data-testid="duplicate-badge"
            >
              <template #icon>
                <AlertTriangle class="w-3 h-3 mr-1 text-amber-600" />
              </template>
            </Tag>
          </div>
          <div v-else class="flex items-center gap-1.5">
            <Tag
              severity="success"
              value="Eligible"
              class="text-[11px] font-medium"
              data-testid="eligible-badge"
            >
              <template #icon>
                <CheckCircle2 class="w-3 h-3 mr-1 text-emerald-600" />
              </template>
            </Tag>
          </div>
        </template>
      </Column>
    </DataTable>
  </div>
</template>
