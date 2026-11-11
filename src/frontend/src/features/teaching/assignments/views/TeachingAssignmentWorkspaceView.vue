<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  Inbox,
  UserCheck,
  BookOpen,
  ArrowLeftRight,
  CheckCircle2,
  Search,
  Building2,
  Calendar,
  Sparkles,
} from 'lucide-vue-next';
import { PageHeader } from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  PERIOD_PRESETS,
  CAMPUS_OPTIONS,
  TEACHING_ASSIGNMENT_PERMISSIONS,
  type PeriodOption,
  type CampusOption,
} from '../constants/assignmentWorkbenchConstants';
import DemandsIntakeTab from '../components/DemandsIntakeTab.vue';
import CandidateEligibilityTab from '../components/CandidateEligibilityTab.vue';
import ActiveAssignmentsTab from '../components/ActiveAssignmentsTab.vue';
import CoverageSubstitutionsTab from '../components/CoverageSubstitutionsTab.vue';
import ConclusionsTab from '../components/ConclusionsTab.vue';
import type { TeachingDemandDto, TeacherEligibilityEvaluationDto } from '../types';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Teaching', to: '/teaching/faculty' },
  { label: 'Assignments & Coverage', current: true },
];

// ---------------------------------------------------------------------------
// 2. Route Query State Synchronization
// ---------------------------------------------------------------------------
const validTabs = ['demands', 'eligibility', 'assignments', 'coverage', 'conclusions'];

const activeTab = ref<string>(
  typeof route.query.tab === 'string' && validTabs.includes(route.query.tab)
    ? route.query.tab
    : 'demands',
);

const selectedPeriodId = ref<string>(
  typeof route.query.periodId === 'string' && route.query.periodId.trim()
    ? route.query.periodId.trim()
    : PERIOD_PRESETS[0].id,
);

const selectedCampus = ref<string>(
  typeof route.query.campus === 'string' && route.query.campus.trim()
    ? route.query.campus.trim()
    : 'all',
);

const searchQuery = ref<string>(
  typeof route.query.q === 'string' ? route.query.q : '',
);

const preselectedDemandId = ref<string | null>(
  typeof route.query.demandId === 'string' ? route.query.demandId : null,
);

// Sync changes to URL
function syncRoute() {
  router.replace({
    query: {
      ...route.query,
      tab: activeTab.value,
      periodId: selectedPeriodId.value,
      campus: selectedCampus.value !== 'all' ? selectedCampus.value : undefined,
      q: searchQuery.value.trim() || undefined,
      demandId: preselectedDemandId.value || undefined,
    },
  });
}

watch([activeTab, selectedPeriodId, selectedCampus], () => {
  syncRoute();
});

// ---------------------------------------------------------------------------
// 3. Permissions Evaluation
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const canManage = computed(
  () =>
    authStore.hasPermission(TEACHING_ASSIGNMENT_PERMISSIONS.MANAGE) ||
    authStore.hasAnyRole(['SuperAdmin', 'AcademicDirector', 'TeachingCoordinator']),
);

const canApprove = computed(
  () =>
    authStore.hasPermission(TEACHING_ASSIGNMENT_PERMISSIONS.APPROVE) ||
    authStore.hasAnyRole(['SuperAdmin', 'AcademicDirector', 'SchoolPrincipal']),
);

// ---------------------------------------------------------------------------
// 4. Tab Transition Handlers
// ---------------------------------------------------------------------------
function handleEvaluateDemand(demand: TeachingDemandDto) {
  preselectedDemandId.value = demand.id;
  activeTab.value = 'eligibility';
  syncRoute();
}

function handleProposeAssignment(data: {
  demand: TeachingDemandDto;
  teacherProfileId: string;
  evaluation: TeacherEligibilityEvaluationDto;
}) {
  activeTab.value = 'assignments';
  syncRoute();
}
</script>

<template>
  <div class="space-y-6" data-testid="teaching-assignment-workspace">
    <!-- Page Header with Title and Global Filters -->
    <PageHeader
      title="Teaching Assignments & Coverage Workbench"
      subtitle="Process academic teaching demands, evaluate faculty eligibility with explainable policy checks, and manage classroom assignments"
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <div class="flex flex-wrap items-center gap-2">
          <!-- Academic Period Selector -->
          <div class="flex items-center gap-1.5">
            <Calendar class="w-4 h-4 text-surface-400 shrink-0" />
            <Select
              v-model="selectedPeriodId"
              :options="PERIOD_PRESETS"
              option-label="label"
              option-value="id"
              class="!text-xs w-56"
              data-testid="header-period-select"
            />
          </div>

          <!-- Campus Filter -->
          <div class="flex items-center gap-1.5">
            <Building2 class="w-4 h-4 text-surface-400 shrink-0" />
            <Select
              v-model="selectedCampus"
              :options="CAMPUS_OPTIONS"
              option-label="label"
              option-value="value"
              class="!text-xs w-40"
              data-testid="header-campus-select"
            />
          </div>
        </div>
      </template>
    </PageHeader>

    <!-- Master Tabs Shell (SCR-TCH-02, TD-06) -->
    <section
      class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden"
      data-testid="assignment-workbench-tabs-card"
    >
      <Tabs v-model:value="activeTab">
        <!-- Tab Navigation Header -->
        <TabList class="border-b border-surface-200 dark:border-surface-700 bg-surface-50/40 dark:bg-surface-800/20 px-2 overflow-x-auto flex-nowrap">
          <!-- Tab 1: Demands Intake -->
          <Tab value="demands" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-nav-demands">
            <div class="flex items-center gap-2">
              <Inbox class="w-4 h-4" />
              <span>Demands Intake</span>
            </div>
          </Tab>

          <!-- Tab 2: Candidate Eligibility -->
          <Tab value="eligibility" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-nav-eligibility">
            <div class="flex items-center gap-2">
              <UserCheck class="w-4 h-4" />
              <span>Candidate Eligibility</span>
            </div>
          </Tab>

          <!-- Tab 3: Assignments & Homerooms -->
          <Tab value="assignments" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-nav-assignments">
            <div class="flex items-center gap-2">
              <BookOpen class="w-4 h-4" />
              <span>Assignments & Homerooms</span>
            </div>
          </Tab>

          <!-- Tab 4: Coverage & Substitutions -->
          <Tab value="coverage" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-nav-coverage">
            <div class="flex items-center gap-2">
              <ArrowLeftRight class="w-4 h-4" />
              <span>Coverage & Substitutions</span>
            </div>
          </Tab>

          <!-- Tab 5: Conclusions -->
          <Tab value="conclusions" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-nav-conclusions">
            <div class="flex items-center gap-2">
              <CheckCircle2 class="w-4 h-4" />
              <span>Conclusions</span>
            </div>
          </Tab>
        </TabList>

        <!-- Tab Panels -->
        <TabPanels class="p-4 sm:p-6">
          <!-- Tab 1: Demands Intake -->
          <TabPanel value="demands" data-testid="panel-demands">
            <DemandsIntakeTab
              :academic-period-id="selectedPeriodId"
              :campus-scope="selectedCampus"
              :search="searchQuery"
              :can-manage="canManage"
              @evaluate-demand="handleEvaluateDemand"
            />
          </TabPanel>

          <!-- Tab 2: Candidate Eligibility -->
          <TabPanel value="eligibility" data-testid="panel-eligibility">
            <CandidateEligibilityTab
              :academic-period-id="selectedPeriodId"
              :campus-scope="selectedCampus"
              :preselected-demand-id="preselectedDemandId"
              :can-manage="canManage"
              @propose-assignment="handleProposeAssignment"
            />
          </TabPanel>

          <!-- Tab 3: Assignments & Homerooms -->
          <TabPanel value="assignments" data-testid="panel-assignments">
            <ActiveAssignmentsTab
              :academic-period-id="selectedPeriodId"
              :campus-scope="selectedCampus"
              :search="searchQuery"
              :can-manage="canManage"
              :can-approve="canApprove"
            />
          </TabPanel>

          <!-- Tab 4: Coverage & Substitutions -->
          <TabPanel value="coverage" data-testid="panel-coverage">
            <CoverageSubstitutionsTab
              :academic-period-id="selectedPeriodId"
              :campus-scope="selectedCampus"
              :search="searchQuery"
              :can-manage="canManage"
              :can-approve="canApprove"
            />
          </TabPanel>

          <!-- Tab 5: Conclusions -->
          <TabPanel value="conclusions" data-testid="panel-conclusions">
            <ConclusionsTab
              :academic-period-id="selectedPeriodId"
              :campus-scope="selectedCampus"
              :search="searchQuery"
              :can-manage="canManage"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </section>
  </div>
</template>
