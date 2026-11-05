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
} from 'lucide-vue-next';
import {
  PageHeader,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
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
import type { LearningStructure, AcademicRequirement } from '../types/curriculum.types';

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
      selectedAcademicYearId.value =
        years.find((y) => y.status === 'active')?.id ?? years[0].id;
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
  <div class="space-y-6" data-testid="curriculum-workspace-view">
    <!-- Institutional Header -->
    <PageHeader
      title="Curriculum & Learning Structure Workspace"
      subtitle="Curriculum framework lifecycle management, neutral learning unit composition, and policy versioning (SCR-ACD-03)."
      :breadcrumbs="breadcrumbs"
    />

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
      class="p-4 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-sm flex items-start gap-3"
      data-testid="workspace-error-banner"
    >
      <AlertTriangle class="w-5 h-5 text-rose-600 mt-0.5 shrink-0" />
      <div>
        <h4 class="font-semibold text-rose-900">Failed to load curriculum framework</h4>
        <p class="text-xs text-rose-700 mt-0.5">
          An error occurred communicating with the Academics domain API. Please retry or verify network connectivity.
        </p>
      </div>
    </div>

    <!-- Workspace State: Loading / Empty / Master Tabs -->
    <div
      v-if="isLoadingPeriods || (selectedPeriodId && isLoadingStructures && structures.length === 0)"
      class="py-12 flex justify-center"
      data-testid="workspace-loading"
    >
      <LoadingSpinner size="lg" message="Loading curriculum frameworks and operational periods..." />
    </div>

    <!-- Empty State: No Period Selected -->
    <div
      v-else-if="!selectedPeriodId"
      class="py-8"
      data-testid="workspace-no-period-empty"
    >
      <EmptyState
        title="No Academic Period Selected"
        description="Select an academic period from the context bar above to view and manage curriculum frameworks."
      />
    </div>

    <!-- Empty State: No Structures for Period -->
    <div
      v-else-if="structures.length === 0"
      class="py-8"
      data-testid="workspace-no-structures-empty"
    >
      <EmptyState
        title="No Curriculum Framework Configured"
        description="There are currently no curriculum frameworks defined for this academic period. Create a new draft framework to begin configuring learning units and requirements."
      >
        <template #actions>
          <Button
            v-if="canManage"
            type="button"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white"
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
    <div v-else class="space-y-4" data-testid="workspace-tabs-container">
      <Tabs v-model:value="activeTab" class="w-full" data-testid="curriculum-workspace-tabs">
        <TabList class="border-b border-slate-200">
          <Tab value="overview" data-testid="tab-overview">
            <div class="flex items-center gap-2 py-1">
              <FileText class="w-4 h-4 text-teal-600" />
              <span class="font-medium text-sm">Overview & Lineage</span>
            </div>
          </Tab>
          <Tab value="units" data-testid="tab-units">
            <div class="flex items-center gap-2 py-1">
              <Layers class="w-4 h-4 text-teal-600" />
              <span class="font-medium text-sm">Learning Units</span>
            </div>
          </Tab>
          <Tab value="requirements" data-testid="tab-requirements">
            <div class="flex items-center gap-2 py-1">
              <ShieldCheck class="w-4 h-4 text-teal-600" />
              <span class="font-medium text-sm">Academic Requirements</span>
            </div>
          </Tab>
        </TabList>

        <TabPanels class="pt-4">
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
