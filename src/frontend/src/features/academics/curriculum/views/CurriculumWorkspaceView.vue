<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import Button from 'primevue/button';
import {
  FileText,
  Layers,
  ShieldCheck,
  PlusCircle,
  AlertTriangle,
  Calendar,
  BookOpen,
  Clock,
} from 'lucide-vue-next';
import { PageHeader, EmptyState, LoadingSpinner } from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import {
  useLearningStructures,
  useLearningStructureById,
  useSubmitStructure,
  useApproveStructure,
  useArchiveStructure,
} from '../composables/useCurriculum';
import {
  useAcademicYearsQuery,
  useOperationalPeriodsQuery,
} from '@/features/operations/calendar/composables';
import CurriculumContextBar from '../components/CurriculumContextBar.vue';
import type { AcademicPeriodOption } from '../components/CurriculumContextBar.vue';
import StructureOverviewTab from '../components/StructureOverviewTab.vue';
import LearningUnitsTab from '../components/LearningUnitsTab.vue';
import AcademicRequirementsTab from '../components/AcademicRequirementsTab.vue';
import CreateStructureModal from '../components/CreateStructureModal.vue';
import EditStructureModal from '../components/EditStructureModal.vue';
import SupersedeStructureModal from '../components/SupersedeStructureModal.vue';
import CreateRequirementModal from '../components/CreateRequirementModal.vue';
import EditRequirementModal from '../components/EditRequirementModal.vue';
import type {
  LearningStructure,
  LearningStructureStatus,
  AcademicRequirement,
} from '../types/curriculum.types';

// --------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// --------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Academics', to: '/academics' },
  { label: 'Curriculum Planning Workspace', current: true },
];

// --------------------------------------------------------------------------
// 2. Authorization & Permissions
// --------------------------------------------------------------------------
const authStore = useAuthStore();
const canManage = computed(() => authStore.hasPermission(CURRICULUM_PERMISSIONS.MANAGE));
const canApprove = computed(() => authStore.hasPermission(CURRICULUM_PERMISSIONS.APPROVE));

// --------------------------------------------------------------------------
// 3. Ephemeral UI State (Tabs & Active Context)
// --------------------------------------------------------------------------
const activeTab = ref<string>('overview');
const selectedAcademicYearId = ref<string | null>(null);
const selectedPeriodId = ref<string | null>(null);
const selectedStructureId = ref<string | null>(null);

// --------------------------------------------------------------------------
// 4. Reactive TanStack Queries
// --------------------------------------------------------------------------
// A learning structure must be anchored to an OPERATIONAL PERIOD
// (operations.operational_periods), never to an academic year. The period
// combo-box therefore lists periods, driven by a selected academic year.
const { data: academicYears, isLoading: isLoadingAcademicYears } = useAcademicYearsQuery();

const yearOptions = computed<AcademicPeriodOption[]>(() => {
  if (!academicYears.value || academicYears.value.length === 0) {
    return [];
  }
  return academicYears.value.map((ay) => ({
    id: ay.id,
    label: `${ay.name} (${ay.code})`,
    code: ay.code,
  }));
});

// Auto-select the active academic year (or the first available) when loaded
watch(
  () => academicYears.value,
  (years) => {
    if (!years || years.length === 0) return;
    if (!selectedAcademicYearId.value) {
      selectedAcademicYearId.value = years.find((y) => y.status === 'active')?.id ?? years[0].id;
    }
  },
  { immediate: true },
);

// Operational periods belong to an academic year; query them for the selected year.
const { data: operationalPeriods, isLoading: isLoadingPeriods } = useOperationalPeriodsQuery(
  selectedAcademicYearId,
  { enabled: computed(() => Boolean(selectedAcademicYearId.value)) },
);

const periodOptions = computed<AcademicPeriodOption[]>(() => {
  if (!operationalPeriods.value || operationalPeriods.value.length === 0) {
    return [];
  }
  return operationalPeriods.value.map((p) => ({
    id: p.id,
    label: `${p.name} (${p.periodCode})`,
    code: p.periodCode,
  }));
});

// Reset the selected period when the academic year changes (periods are re-fetched)
watch(selectedAcademicYearId, () => {
  selectedPeriodId.value = null;
});

// Auto-select first period when options load
watch(
  () => periodOptions.value,
  (options) => {
    if (options.length > 0 && !selectedPeriodId.value) {
      selectedPeriodId.value = options[0].id;
    }
  },
  { immediate: true },
);

// Query learning structures for selected period
const {
  data: structuresData,
  isLoading: isLoadingStructures,
  error: structuresError,
} = useLearningStructures({
  periodId: selectedPeriodId,
});

const structures = computed(() => structuresData.value?.items ?? []);

// Auto-select active or first structure for period
watch(
  () => structures.value,
  (list) => {
    if (list.length > 0) {
      const activeStructure = list.find((s) => s.status === 'active');
      const alreadySelected = list.some((s) => s.id === selectedStructureId.value);
      if (!alreadySelected) {
        selectedStructureId.value = activeStructure ? activeStructure.id : list[0].id;
      }
    } else {
      selectedStructureId.value = null;
    }
  },
  { immediate: true },
);

// Detail query for selected structure
const structureIdRef = computed(() => selectedStructureId.value ?? '');
const {
  data: selectedStructureDetail,
  isLoading: isLoadingDetail,
  error: detailError,
} = useLearningStructureById(structureIdRef);

const selectedStructureSummary = computed(() => {
  if (!selectedStructureId.value) return null;
  return structures.value.find((s) => s.id === selectedStructureId.value) ?? null;
});

interface StatusPresentation {
  label: string;
  pillClass: string;
  dotClass: string;
}

const STATUS_PRESENTATION: Record<LearningStructureStatus, StatusPresentation> = {
  active: {
    label: 'Active Framework',
    pillClass: 'bg-emerald-50/90 text-emerald-800 border-emerald-200/80',
    dotClass: 'bg-emerald-500',
  },
  submitted: {
    label: 'Awaiting Approval',
    pillClass: 'bg-sky-50/90 text-sky-800 border-sky-200/80',
    dotClass: 'bg-sky-500',
  },
  draft: {
    label: 'Draft Revision',
    pillClass: 'bg-slate-100/90 text-slate-700 border-slate-200/90',
    dotClass: 'bg-slate-400',
  },
  superseded: {
    label: 'Superseded',
    pillClass: 'bg-amber-50/90 text-amber-800 border-amber-200/80',
    dotClass: 'bg-amber-500',
  },
  archived: {
    label: 'Archived Record',
    pillClass: 'bg-rose-50/90 text-rose-800 border-rose-200/80',
    dotClass: 'bg-rose-500',
  },
};

const activeStatusPresentation = computed<StatusPresentation | null>(() => {
  const status = selectedStructureSummary.value?.status as LearningStructureStatus | undefined;
  if (!status) return null;
  return STATUS_PRESENTATION[status] ?? STATUS_PRESENTATION.draft;
});

const workspaceTabs = computed(() => [
  {
    value: 'overview',
    testId: 'tab-overview',
    label: 'Overview & Lineage',
    icon: FileText,
    count: null as number | null,
  },
  {
    value: 'units',
    testId: 'tab-units',
    label: 'Learning Units',
    icon: Layers,
    count: selectedStructureSummary.value?.learningUnitsCount ?? null,
  },
  {
    value: 'requirements',
    testId: 'tab-requirements',
    label: 'Academic Requirements',
    icon: ShieldCheck,
    count: selectedStructureSummary.value?.academicRequirementsCount ?? null,
  },
]);

// --------------------------------------------------------------------------
// 5. Governance Mutations
// --------------------------------------------------------------------------
const submitMutation = useSubmitStructure();
const approveMutation = useApproveStructure();
const archiveMutation = useArchiveStructure();

function handleSubmitStructure() {
  if (!selectedStructureId.value) return;
  submitMutation.mutate(selectedStructureId.value);
}

function handleApproveStructure() {
  if (!selectedStructureId.value) return;
  approveMutation.mutate(selectedStructureId.value);
}

function handleArchiveStructure() {
  if (!selectedStructureId.value) return;
  archiveMutation.mutate(selectedStructureId.value);
}

function handleSelectPredecessor(predecessorId: string) {
  selectedStructureId.value = predecessorId;
}

// Dialog triggers (to be implemented in S17 / S18 / S19)
const isCreateModalOpen = ref(false);
const isEditModalOpen = ref(false);
const isSupersedeModalOpen = ref(false);

function handleOpenCreateModal() {
  isCreateModalOpen.value = true;
}

function handleOpenEditModal() {
  isEditModalOpen.value = true;
}

function handleOpenSupersedeModal() {
  isSupersedeModalOpen.value = true;
}

function syncYearFromPeriod(periodId: string | null) {
  if (!periodId) return;
  const period = operationalPeriods.value?.find((p) => p.id === periodId);
  if (period) {
    selectedAcademicYearId.value = period.academicYearId;
  }
}

function handleStructureCreated(structure: LearningStructure) {
  selectedPeriodId.value = structure.academicPeriodId;
  syncYearFromPeriod(structure.academicPeriodId);
  selectedStructureId.value = structure.id;
}

function handleStructureUpdated(structure: LearningStructure) {
  selectedStructureId.value = structure.id;
}

function handleStructureSuperseded(structure: LearningStructure) {
  selectedPeriodId.value = structure.academicPeriodId;
  syncYearFromPeriod(structure.academicPeriodId);
  selectedStructureId.value = structure.id;
}

// Academic Requirements Dialog Triggers (P6-S19)
const isCreateRequirementModalOpen = ref(false);
const isEditRequirementModalOpen = ref(false);
const selectedRequirementToEdit = ref<AcademicRequirement | null>(null);

function handleOpenCreateRequirementModal() {
  isCreateRequirementModalOpen.value = true;
}

function handleOpenEditRequirementModal(req: AcademicRequirement) {
  selectedRequirementToEdit.value = req;
  isEditRequirementModalOpen.value = true;
}
</script>

<template>
  <div class="max-w-[1440px] mx-auto space-y-5 pb-8" data-testid="curriculum-workspace-view">
    <!-- Institutional Header -->
    <PageHeader
      title="Curriculum & Learning Structure Workspace"
      subtitle="Curriculum framework lifecycle management, neutral learning unit composition, and policy versioning."
      :breadcrumbs="breadcrumbs"
      class="pb-4 border-slate-200/80"
    >
      <template #title-extra>
        <span
          class="hidden sm:inline-flex items-center px-2 py-0.5 rounded-md bg-slate-100 border border-slate-200/80 text-[11px] font-mono font-medium text-slate-500 shrink-0"
        >
          SCR-ACD-03
        </span>
      </template>

      <template v-if="selectedStructureSummary && activeStatusPresentation" #actions>
        <div
          class="inline-flex items-center gap-2.5 px-3 py-1.5 rounded-full border text-xs font-medium shadow-2xs"
          :class="activeStatusPresentation.pillClass"
        >
          <span
            class="w-2 h-2 rounded-full shrink-0"
            :class="activeStatusPresentation.dotClass"
            aria-hidden="true"
          />
          <span>{{ activeStatusPresentation.label }}</span>
          <span class="text-slate-300" aria-hidden="true">•</span>
          <span class="font-mono text-[11px] opacity-85">
            v{{ selectedStructureSummary.version }}
          </span>
        </div>
      </template>
    </PageHeader>

    <!-- Top Context Bar (Year/Period selectors, Structure dropdown, Version pill, Status badge, Action Bar) -->
    <CurriculumContextBar
      :years="yearOptions"
      :selected-year-id="selectedAcademicYearId"
      :periods="periodOptions"
      :selected-period-id="selectedPeriodId"
      :structures="structures"
      :selected-structure-id="selectedStructureId"
      :selected-structure="selectedStructureSummary"
      :is-loading-periods="isLoadingPeriods"
      :is-loading-structures="isLoadingStructures"
      @update:selected-year-id="selectedAcademicYearId = $event"
      @update:selected-period-id="selectedPeriodId = $event"
      @update:selected-structure-id="selectedStructureId = $event"
      @create-structure="handleOpenCreateModal"
      @edit-structure="handleOpenEditModal"
      @submit-structure="handleSubmitStructure"
      @approve-structure="handleApproveStructure"
      @supersede-structure="handleOpenSupersedeModal"
      @archive-structure="handleArchiveStructure"
    />

    <!-- Global Error Display -->
    <div
      v-if="structuresError || detailError"
      class="p-4 sm:p-5 rounded-2xl bg-rose-50/90 border border-rose-200/90 border-l-4 border-l-rose-500 text-rose-800 text-sm flex items-start gap-3.5 shadow-2xs"
      data-testid="workspace-error-banner"
    >
      <div class="p-2 rounded-xl bg-rose-100/90 text-rose-600 shrink-0">
        <AlertTriangle class="w-5 h-5" aria-hidden="true" />
      </div>
      <div class="space-y-0.5">
        <h4 class="font-semibold text-rose-950">Failed to load curriculum framework</h4>
        <p class="text-xs text-rose-700 leading-relaxed">
          An error occurred communicating with the Academics domain API. Please retry or verify
          network connectivity.
        </p>
      </div>
    </div>

    <!-- Workspace State: Loading -->
    <div
      v-if="
        isLoadingPeriods || (selectedPeriodId && isLoadingStructures && structures.length === 0)
      "
      class="rounded-2xl border border-slate-200/80 bg-white shadow-xs py-16 px-6 flex justify-center"
      data-testid="workspace-loading"
    >
      <LoadingSpinner
        size="lg"
        message="Loading curriculum frameworks and operational periods..."
      />
    </div>

    <!-- Empty State: No Period Selected -->
    <div
      v-else-if="!selectedPeriodId"
      class="rounded-2xl border border-slate-200/80 bg-gradient-to-b from-white to-slate-50/60 shadow-xs"
      data-testid="workspace-no-period-empty"
    >
      <EmptyState
        title="No Academic Period Selected"
        description="Select an academic year and operational period from the context bar above to view and manage curriculum frameworks."
      >
        <template #icon>
          <Calendar class="w-6 h-6 sm:w-7 sm:h-7 text-teal-600" aria-hidden="true" />
        </template>
      </EmptyState>
    </div>

    <!-- Empty State: No Structures for Period -->
    <div
      v-else-if="structures.length === 0"
      class="rounded-2xl border border-slate-200/80 bg-gradient-to-b from-white to-slate-50/60 shadow-xs"
      data-testid="workspace-no-structures-empty"
    >
      <EmptyState
        title="No Curriculum Framework Configured"
        description="There are currently no curriculum frameworks defined for this academic period. Create a new draft framework to begin configuring learning units and requirements."
      >
        <template #icon>
          <BookOpen class="w-6 h-6 sm:w-7 sm:h-7 text-teal-600" aria-hidden="true" />
        </template>
        <template #actions>
          <Button
            v-if="canManage"
            type="button"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 border-0 text-white shadow-xs px-4 py-2 rounded-lg font-medium transition-colors"
            data-testid="empty-create-structure-btn"
            @click="handleOpenCreateModal"
          >
            <PlusCircle class="w-4 h-4 mr-1.5" />
            <span>Create Curriculum Framework</span>
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- Master 3-Tab Orchestration Container -->
    <div
      v-else
      class="curriculum-workspace-shell bg-white rounded-2xl border border-slate-200/90 shadow-xs overflow-hidden"
      data-testid="workspace-tabs-container"
    >
      <!-- Calm Framework Glance Strip -->
      <div
        v-if="selectedStructureSummary"
        class="px-5 sm:px-6 py-3.5 bg-gradient-to-r from-slate-50/90 via-white to-teal-50/35 border-b border-slate-200/80 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3"
      >
        <div class="min-w-0 flex items-center gap-3">
          <div
            class="w-9 h-9 rounded-xl bg-teal-50 border border-teal-200/70 flex items-center justify-center text-teal-700 shrink-0"
          >
            <BookOpen class="w-4 h-4" aria-hidden="true" />
          </div>
          <div class="min-w-0">
            <p class="text-sm font-semibold text-slate-800 truncate">
              {{ selectedStructureSummary.name }}
            </p>
            <div class="flex items-center gap-2 text-xs text-slate-500 mt-0.5 flex-wrap">
              <span>{{ selectedStructureSummary.academicPeriodLabel }}</span>
              <span class="text-slate-300" aria-hidden="true">•</span>
              <span class="inline-flex items-center gap-1 font-mono text-[11px] text-slate-600">
                <Clock class="w-3 h-3 text-slate-400" aria-hidden="true" />
                Effective {{ selectedStructureSummary.effectiveDate }}
              </span>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-2 flex-wrap shrink-0">
          <div
            class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-50 border border-slate-200/80 text-xs text-slate-600"
          >
            <Layers class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
            <span class="font-semibold text-slate-800 tabular-nums">
              {{ selectedStructureSummary.learningUnitsCount }}
            </span>
            <span>Units</span>
          </div>
          <div
            class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-50 border border-slate-200/80 text-xs text-slate-600"
          >
            <ShieldCheck class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
            <span class="font-semibold text-slate-800 tabular-nums">
              {{ selectedStructureSummary.academicRequirementsCount }}
            </span>
            <span>Requirements</span>
          </div>
        </div>
      </div>

      <Tabs v-model:value="activeTab" class="w-full" data-testid="curriculum-workspace-tabs">
        <TabList class="bg-slate-50/60 border-b border-slate-200/90 px-4 sm:px-6">
          <Tab
            v-for="tab in workspaceTabs"
            :key="tab.value"
            :value="tab.value"
            :data-testid="tab.testId"
            class="group"
          >
            <div class="flex items-center gap-2 py-1">
              <component
                :is="tab.icon"
                class="w-4 h-4 transition-colors"
                :class="
                  activeTab === tab.value
                    ? 'text-teal-600'
                    : 'text-slate-400 group-hover:text-slate-600'
                "
                aria-hidden="true"
              />
              <span
                class="text-sm transition-colors"
                :class="
                  activeTab === tab.value
                    ? 'font-semibold text-slate-900'
                    : 'font-medium text-slate-600 group-hover:text-slate-800'
                "
              >
                {{ tab.label }}
              </span>
              <span
                v-if="tab.count !== null"
                class="ml-0.5 px-1.5 py-0.5 rounded-full text-[11px] font-semibold tabular-nums transition-colors"
                :class="
                  activeTab === tab.value
                    ? 'bg-teal-50 text-teal-700 border border-teal-200/70'
                    : 'bg-slate-200/70 text-slate-600 border border-transparent'
                "
              >
                {{ tab.count }}
              </span>
            </div>
          </Tab>
        </TabList>

        <TabPanels class="p-5 sm:p-6 bg-white">
          <!-- Tab 1: Overview & Lineage -->
          <TabPanel value="overview" data-testid="panel-overview">
            <StructureOverviewTab
              :structure="selectedStructureDetail ?? null"
              @select-predecessor="handleSelectPredecessor"
            />
          </TabPanel>

          <!-- Tab 2: Learning Units Hierarchy -->
          <TabPanel value="units" data-testid="panel-units">
            <LearningUnitsTab
              :structure-id="selectedStructureId"
              :is-structure-active="selectedStructureSummary?.status === 'active'"
            />
          </TabPanel>

          <!-- Tab 3: Academic Requirements -->
          <TabPanel value="requirements" data-testid="panel-requirements">
            <AcademicRequirementsTab
              :structure-id="selectedStructureId"
              :is-structure-active="selectedStructureSummary?.status === 'active'"
              @add-requirement="handleOpenCreateRequirementModal"
              @edit-requirement="handleOpenEditRequirementModal"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- Framework Governance & Scaffolding Modals (P6-S17) -->
    <CreateStructureModal
      v-model:visible="isCreateModalOpen"
      :period-id="selectedPeriodId"
      :periods="periodOptions"
      @created="handleStructureCreated"
    />

    <EditStructureModal
      v-model:visible="isEditModalOpen"
      :structure="selectedStructureDetail ?? selectedStructureSummary"
      @updated="handleStructureUpdated"
    />

    <SupersedeStructureModal
      v-model:visible="isSupersedeModalOpen"
      :structure="selectedStructureDetail ?? selectedStructureSummary"
      :periods="periodOptions"
      @superseded="handleStructureSuperseded"
    />

    <!-- Academic Requirements Policy Modals (P6-S19) -->
    <CreateRequirementModal
      v-model:visible="isCreateRequirementModalOpen"
      :structure-id="selectedStructureId ?? ''"
    />

    <EditRequirementModal
      v-model:visible="isEditRequirementModalOpen"
      :requirement="selectedRequirementToEdit"
      :structure-id="selectedStructureId ?? ''"
      :is-structure-active="selectedStructureSummary?.status === 'active'"
    />
  </div>
</template>

<style scoped>
.curriculum-workspace-shell :deep(.p-tablist-tab-list) {
  background: transparent;
  border-color: transparent;
  gap: 0.25rem;
}

.curriculum-workspace-shell :deep(.p-tab) {
  padding: 0.75rem 1rem;
  border-color: transparent;
  transition:
    color 150ms ease,
    background-color 150ms ease;
}

.curriculum-workspace-shell :deep(.p-tab:hover) {
  background-color: rgba(241, 245, 249, 0.7);
}

.curriculum-workspace-shell :deep(.p-tablist-active-bar) {
  background-color: #0d9488;
  height: 2px;
}

.curriculum-workspace-shell :deep(.p-tabpanels) {
  background: transparent;
  padding: 1.25rem 1.5rem;
}
</style>
