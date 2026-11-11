<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import {
  GraduationCap,
  Search,
  Filter,
  RefreshCw,
  Building2,
  Calendar,
  Layers,
  ArrowRightLeft,
  FileCheck,
  UserCheck,
} from 'lucide-vue-next';
import { PageHeader, EmptyState } from '@/shared/components';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { useStudentSearchQuery } from '@/features/students/records/composables/useStudentRecordsQueries';
import type { StudentListItem, StudentSearchParams } from '@/features/students/records/types/records.types';
import ProgressionRecommendationBadge from '../components/ProgressionRecommendationBadge.vue';
import StudentPlacementDrawer from '../components/StudentPlacementDrawer.vue';

const searchFilters = reactive<StudentSearchParams>({
  searchTerm: '',
  campus: 'all',
  level: 'all',
  status: 'active',
  pageNumber: 1,
  pageSize: 25,
});

useRouteQueryHydration((query) => {
  const status = getQueryString(query, 'status');
  if (status) {
    searchFilters.status = status;
  }

  const campus = getQueryString(query, 'campus');
  if (campus) {
    searchFilters.campus = campus;
  }

  const level = getQueryString(query, 'schoolLevel') ?? getQueryString(query, 'level');
  if (level) {
    searchFilters.level = level;
  }
});

const {
  data: searchResults,
  isLoading,
  refetch,
} = useStudentSearchQuery(computed(() => searchFilters));

const students = computed<StudentListItem[]>(() => searchResults.value?.items || []);
const totalRecords = computed(() => searchResults.value?.totalCount || 0);

const selectedStudent = ref<StudentListItem | null>(null);
const isDrawerOpen = ref(false);

function openPlacementDrawer(student: StudentListItem) {
  selectedStudent.value = student;
  isDrawerOpen.value = true;
}

function handlePlacementSuccess() {
  refetch();
}

const campusOptions = [
  { label: 'All Campuses', value: 'all' },
  { label: 'Main Campus (campus-main)', value: 'campus-main' },
  { label: 'East Campus (campus-east)', value: 'campus-east' },
  { label: 'Hagios Main Campus', value: 'Hagios Main Campus' },
  { label: 'West Campus', value: 'West Campus' },
];

const levelOptions = [
  { label: 'All Levels', value: 'all' },
  { label: 'ELC (Early Learning)', value: 'ELC' },
  { label: 'JHLC (Junior High)', value: 'JHLC' },
  { label: 'HSLC (Senior High)', value: 'HSLC' },
  { label: 'Primary School / SD', value: 'Primary' },
  { label: 'Junior High / SMP', value: 'Junior High' },
  { label: 'Senior High / SMA', value: 'Senior High' },
];

const statusOptions = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Active', value: 'active' },
  { label: 'Enrolled', value: 'enrolled' },
  { label: 'On Leave', value: 'on_leave' },
  { label: 'Withdrawn', value: 'withdrawn' },
  { label: 'Transferred Out', value: 'transferred_out' },
  { label: 'Graduated', value: 'graduated' },
];
</script>

<template>
  <div class="space-y-6" data-testid="student-lifecycle-workbench-view">
    <!-- Workbench Header -->
    <PageHeader
      title="Student Lifecycle Workbench"
      subtitle="SCR-STU-03 — Official administrative grade progression, promotion, retention, and cohort rollover (STD-WF-08)"
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <Button
            size="small"
            severity="secondary"
            variant="outlined"
            class="!text-xs cursor-pointer inline-flex items-center gap-1.5"
            :loading="isLoading"
            data-testid="btn-refresh-lifecycle"
            @click="refetch()"
          >
            <RefreshCw class="w-3.5 h-3.5" />
            <span>Refresh</span>
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Context Explanatory Banner -->
    <div class="p-4 rounded-xl border border-teal-200 dark:border-teal-800 bg-teal-50/70 dark:bg-teal-950/30 text-xs text-teal-900 dark:text-teal-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs">
      <div class="flex items-center gap-3">
        <div class="w-9 h-9 rounded-lg bg-teal-100 dark:bg-teal-900/60 flex items-center justify-center text-teal-700 dark:text-teal-300 shrink-0">
          <GraduationCap class="w-5 h-5" />
        </div>
        <div class="space-y-0.5">
          <span class="font-bold text-surface-900 dark:text-surface-100 text-sm">
            Cross-Domain Progression Governance
          </span>
          <p class="text-xs text-surface-600 dark:text-surface-400">
            Academics certifies authoritative promotion and retention recommendations with committee decrees (*SK Dewan Guru*). Students domain executes the official administrative grade rollover.
          </p>
        </div>
      </div>
    </div>

    <!-- Filter Bar -->
    <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-900 shadow-xs space-y-3">
      <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-5 gap-3 text-xs">
        <!-- Search Input -->
        <div class="sm:col-span-2">
          <span class="text-surface-500 block mb-1 font-medium">Search Student</span>
          <div class="relative">
            <Search class="w-4 h-4 text-surface-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <InputText
              v-model="searchFilters.searchTerm"
              placeholder="Search by student name or student number..."
              class="w-full !pl-9 text-xs"
              data-testid="input-search-student"
            />
          </div>
        </div>

        <!-- Campus Selector -->
        <div>
          <span class="text-surface-500 block mb-1 font-medium">Campus</span>
          <Select
            v-model="searchFilters.campus"
            :options="campusOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="select-campus-filter"
          />
        </div>

        <!-- School Level Selector -->
        <div>
          <span class="text-surface-500 block mb-1 font-medium">School Level</span>
          <Select
            v-model="searchFilters.level"
            :options="levelOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="select-level-filter"
          />
        </div>

        <!-- Status Selector -->
        <div>
          <span class="text-surface-500 block mb-1 font-medium">Student Status</span>
          <Select
            v-model="searchFilters.status"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-full text-xs"
            data-testid="select-status-filter"
          />
        </div>
      </div>
    </div>

    <!-- Student Roster Table -->
    <div class="rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-900 shadow-xs overflow-hidden">
      <DataTable
        :value="students"
        :loading="isLoading"
        data-key="id"
        class="text-xs"
        responsive-layout="scroll"
        data-testid="lifecycle-students-table"
      >
        <template #empty>
          <div class="py-12">
            <EmptyState
              title="No students found"
              description="No enrolled students match the active search and filter criteria."
              size="sm"
            >
              <template #icon>
                <GraduationCap class="w-8 h-8 text-surface-400" />
              </template>
            </EmptyState>
          </div>
        </template>

        <!-- Student Identifier Column -->
        <Column header="Student" style="min-width: 200px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="font-bold text-surface-900 dark:text-surface-100 block">
                {{ data.fullName }}
              </span>
              <span class="text-[11px] text-surface-500 font-mono">
                {{ data.studentNumber }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Current Placement Column -->
        <Column header="Current Placement" style="min-width: 170px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <span class="font-semibold text-surface-800 dark:text-surface-200 block">
                {{ data.grade || '—' }} ({{ data.level || data.schoolLevel || '—' }})
              </span>
              <span class="text-[11px] text-surface-500">
                {{ data.campus || '—' }}
              </span>
            </div>
          </template>
        </Column>

        <!-- Authoritative Academic Progression Recommendation Badge Column -->
        <Column header="Academic Progression Recommendation" style="min-width: 260px">
          <template #body="{ data }">
            <ProgressionRecommendationBadge
              :student-id="data.id"
              :compact="true"
            />
          </template>
        </Column>

        <!-- Status Column -->
        <Column header="Enrollment Status" style="min-width: 120px">
          <template #body="{ data }">
            <Tag
              :value="data.currentStatus || data.status || 'Active'"
              :severity="(data.currentStatus || data.status) === 'withdrawn' || (data.currentStatus || data.status) === 'transferred_out' ? 'danger' : (data.currentStatus || data.status) === 'on_leave' ? 'warn' : 'success'"
              class="!text-[10px] !px-2 !py-0.5"
            />
          </template>
        </Column>

        <!-- Actions Column -->
        <Column header="Actions" align-frozen="right" style="min-width: 140px; text-align: right">
          <template #body="{ data }">
            <Button
              size="small"
              severity="primary"
              variant="outlined"
              class="!text-xs cursor-pointer inline-flex items-center gap-1.5"
              data-testid="btn-open-rollover-drawer"
              @click="openPlacementDrawer(data)"
            >
              <ArrowRightLeft class="w-3.5 h-3.5" />
              <span>Rollover</span>
            </Button>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Student Placement & Rollover Drawer -->
    <StudentPlacementDrawer
      v-model:visible="isDrawerOpen"
      :student="selectedStudent"
      @success="handlePlacementSuccess"
    />
  </div>
</template>
