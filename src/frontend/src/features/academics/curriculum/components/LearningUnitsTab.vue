<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import TreeTable from 'primevue/treetable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import ConfirmDialog from 'primevue/confirmdialog';
import { useConfirm } from 'primevue/useconfirm';
import {
  Layers,
  Plus,
  PlusCircle,
  Edit,
  Trash2,
  BookOpen,
  FolderTree,
  AlertTriangle,
} from 'lucide-vue-next';
import { ContentCard, EmptyState, LoadingSpinner, StatusPill } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import { useLearningUnits, useDeleteUnit } from '../composables/useCurriculum';
import type { LearningUnit, LearningUnitTreeNode } from '../types/curriculum.types';
import CreateLearningUnitModal from './CreateLearningUnitModal.vue';
import EditLearningUnitModal from './EditLearningUnitModal.vue';

const props = defineProps<{
  structureId: string | null;
  isStructureActive?: boolean;
}>();

const emit = defineEmits<{
  (e: 'addUnit', parentUnitId?: string | null): void;
  (e: 'editUnit', unit: LearningUnit): void;
}>();

const authStore = useAuthStore();
const canManage = computed(() => authStore.hasPermission(CURRICULUM_PERMISSIONS.MANAGE));

const structureIdRef = computed(() => props.structureId ?? '');
const { data: units, learningUnitTree, isLoading, error } = useLearningUnits(structureIdRef);

function tryGetConfirm() {
  try {
    return useConfirm();
  } catch {
    return null;
  }
}

const confirm = tryGetConfirm();
const deleteMutation = useDeleteUnit();

// --------------------------------------------------------------------------
// 1. Hierarchy & Expansion State
// --------------------------------------------------------------------------
const expandedKeys = ref<Record<string, boolean>>({});

// Keep all root nodes expanded by default for rapid visibility of 2-level hierarchy
watch(
  learningUnitTree,
  (tree) => {
    if (tree && tree.length > 0) {
      const keys: Record<string, boolean> = {};
      tree.forEach((node) => {
        keys[node.key ?? node.id] = true;
      });
      expandedKeys.value = keys;
    }
  },
  { immediate: true },
);

// Filter exclusively root units for modal parent selection
const rootUnits = computed<LearningUnit[]>(() => {
  return (units.value ?? []).filter((u) => !u.parentUnitId);
});

// --------------------------------------------------------------------------
// 2. Metrics & Summaries
// --------------------------------------------------------------------------
const totalUnitCount = computed(() => units.value?.length ?? 0);
const rootUnitCount = computed(() => rootUnits.value.length);
const childUnitCount = computed(() => Math.max(0, totalUnitCount.value - rootUnitCount.value));

const cumulativeCreditWeight = computed(() => {
  if (!units.value || units.value.length === 0) return 0;
  const sum = units.value.reduce((acc, u) => acc + (Number(u.creditWeight) || 0), 0);
  return Math.round(sum * 100) / 100;
});

// --------------------------------------------------------------------------
// 3. Modal State Management
// --------------------------------------------------------------------------
const isCreateModalOpen = ref(false);
const selectedParentUnitId = ref<string | null>(null);

const isEditModalOpen = ref(false);
const selectedUnitForEdit = ref<LearningUnit | null>(null);

function handleOpenCreateRoot() {
  selectedParentUnitId.value = null;
  isCreateModalOpen.value = true;
  emit('addUnit', null);
}

function handleOpenCreateChild(parentUnit: LearningUnit) {
  selectedParentUnitId.value = parentUnit.id;
  isCreateModalOpen.value = true;
  emit('addUnit', parentUnit.id);
}

function handleOpenEdit(unit: LearningUnit) {
  selectedUnitForEdit.value = unit;
  isEditModalOpen.value = true;
  emit('editUnit', unit);
}

function handleDeleteUnit(unit: LearningUnit) {
  if (confirm) {
    confirm.require({
      message: `Are you sure you want to delete learning unit "${unit.code} - ${unit.name}"? This action cannot be undone.`,
      header: 'Delete Learning Unit',
      icon: 'pi pi-exclamation-triangle',
      rejectProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true,
      },
      acceptProps: {
        label: 'Delete Unit',
        severity: 'danger',
      },
      accept: () => {
        deleteMutation.mutate({
          id: unit.id,
          structureId: props.structureId ?? undefined,
        });
      },
    });
  } else {
    deleteMutation.mutate({
      id: unit.id,
      structureId: props.structureId ?? undefined,
    });
  }
}

// --------------------------------------------------------------------------
// 4. Badges & Formatter Helpers
// --------------------------------------------------------------------------
function getUnitTypeBadgeClass(unitType: string): string {
  switch (unitType?.toLowerCase()) {
    case 'subject':
      return 'bg-teal-50 text-teal-700 border-teal-200';
    case 'course':
      return 'bg-blue-50 text-blue-700 border-blue-200';
    case 'module':
      return 'bg-indigo-50 text-indigo-700 border-indigo-200';
    case 'pace':
      return 'bg-purple-50 text-purple-700 border-purple-200';
    case 'project':
      return 'bg-amber-50 text-amber-700 border-amber-200';
    case 'competency':
      return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    case 'unit':
      return 'bg-sky-50 text-sky-700 border-sky-200';
    default:
      return 'bg-slate-50 text-slate-700 border-slate-200';
  }
}
</script>

<template>
  <div class="space-y-4" data-testid="learning-units-tab">
    <ContentCard title="Curriculum Learning Units Hierarchy" class="bg-white">
      <template #header-actions>
        <Button
          v-if="canManage"
          type="button"
          size="small"
          class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5 cursor-pointer"
          data-testid="btn-add-unit"
          :disabled="!structureId"
          @click="handleOpenCreateRoot"
        >
          <Plus class="w-3.5 h-3.5" />
          <span>Add Learning Unit</span>
        </Button>
      </template>

      <!-- Loading State -->
      <div v-if="isLoading" class="py-12 flex justify-center" data-testid="units-loading">
        <LoadingSpinner size="md" message="Loading learning units hierarchy..." />
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="p-4 rounded-md bg-rose-50 text-rose-700 text-sm" data-testid="units-error">
        Failed to load learning units for this framework.
      </div>

      <!-- Empty State -->
      <div v-else-if="!units || units.length === 0" class="py-6" data-testid="units-empty">
        <EmptyState
          title="No Learning Units Defined"
          description="Curriculum frameworks must define at least one learning unit (subjects, courses, modules, or competencies) before approval."
        >
          <template #actions>
            <Button
              v-if="canManage"
              type="button"
              size="small"
              class="bg-teal-600 hover:bg-teal-700 text-white"
              data-testid="empty-btn-add-unit"
              @click="handleOpenCreateRoot"
            >
              <Plus class="w-4 h-4 mr-1.5" />
              <span>Add Root Learning Unit</span>
            </Button>
          </template>
        </EmptyState>
      </div>

      <!-- Units TreeTable & Summary Container -->
      <div v-else class="space-y-3" data-testid="units-list">
        <!-- Summary Strip: Total Unit Count & Cumulative Credit Weight (Completion Criterion 1) -->
        <div
          class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-lg text-xs"
          data-testid="units-summary-bar"
        >
          <div class="flex items-center gap-4 text-slate-600 flex-wrap">
            <div>
              Total Units:
              <span class="font-bold text-slate-800 font-mono" data-testid="summary-total-units">
                {{ totalUnitCount }}
              </span>
            </div>
            <div class="text-slate-300">|</div>
            <div>
              Root Units:
              <span class="font-bold text-slate-800 font-mono" data-testid="summary-root-units">
                {{ rootUnitCount }}
              </span>
            </div>
            <div class="text-slate-300">|</div>
            <div>
              Child Modules:
              <span class="font-bold text-slate-800 font-mono" data-testid="summary-child-units">
                {{ childUnitCount }}
              </span>
            </div>
          </div>

          <div class="flex items-center gap-1.5 text-teal-800 font-medium">
            <span class="text-slate-500">Cumulative Credit Weight:</span>
            <span
              class="px-2 py-0.5 rounded bg-teal-100/70 border border-teal-200 font-mono font-bold text-xs"
              data-testid="summary-credit-weight"
            >
              {{ cumulativeCreditWeight }}
            </span>
          </div>
        </div>

        <!-- 2-Level TreeTable Hierarchy -->
        <div class="border border-slate-200 rounded-lg overflow-hidden" data-testid="units-treetable-container">
          <TreeTable
            v-model:expanded-keys="expandedKeys"
            :value="learningUnitTree"
            data-key="id"
            class="p-treetable-sm text-xs w-full"
            data-testid="learning-units-treetable"
          >
            <!-- Code Column with Expander -->
            <Column field="code" header="Code" expander style="min-width: 170px">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <div class="inline-flex items-center gap-1.5 font-mono">
                  <span class="font-bold text-teal-900" data-testid="unit-code">
                    {{ node.data?.code ?? node.code }}
                  </span>
                  <span
                    v-if="node.data?.parentUnitId || node.parentUnitId"
                    class="text-[10px] text-slate-400 font-sans"
                    title="Child Module"
                  >
                    (child)
                  </span>
                </div>
              </template>
            </Column>

            <!-- Unit Name Column -->
            <Column field="name" header="Unit Name" style="min-width: 220px">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <div>
                  <div class="font-medium text-slate-900" data-testid="unit-name">
                    {{ node.data?.name ?? node.name }}
                  </div>
                  <div
                    v-if="node.data?.description || node.description"
                    class="text-[11px] text-slate-400 truncate max-w-sm mt-0.5"
                    :title="node.data?.description ?? node.description ?? ''"
                  >
                    {{ node.data?.description ?? node.description }}
                  </div>
                </div>
              </template>
            </Column>

            <!-- Unit Type Badge Column -->
            <Column field="unitType" header="Type" style="min-width: 120px">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <span
                  :class="[
                    'inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wide border',
                    getUnitTypeBadgeClass(node.data?.unitType ?? node.unitType),
                  ]"
                  data-testid="unit-type-badge"
                >
                  {{ node.data?.unitType ?? node.unitType }}
                </span>
              </template>
            </Column>

            <!-- Level Column -->
            <Column field="level" header="Level" style="min-width: 110px">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <span class="text-slate-600" data-testid="unit-level">
                  {{ node.data?.level || node.level || '—' }}
                </span>
              </template>
            </Column>

            <!-- Credit Weight Column -->
            <Column field="creditWeight" header="Credit Weight" style="min-width: 110px">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <span class="font-mono text-slate-700" data-testid="unit-credit-weight">
                  {{ node.data?.creditWeight != null ? node.data.creditWeight : (node.creditWeight ?? '0') }}
                </span>
              </template>
            </Column>

            <!-- Status Pill Column -->
            <Column field="status" header="Status" style="min-width: 100px">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <StatusPill
                  :status="node.data?.status ?? node.status"
                  size="sm"
                  data-testid="unit-status-pill"
                />
              </template>
            </Column>

            <!-- Row Actions Column -->
            <Column header="Actions" style="min-width: 140px; text-align: right">
              <template #body="{ node }: { node: LearningUnitTreeNode }">
                <div class="flex items-center justify-end gap-1" data-testid="unit-row-actions">
                  <!-- Add Child Module (STRICTLY for root units only: parentUnitId is NULL) -->
                  <Button
                    v-if="canManage && !(node.data?.parentUnitId || node.parentUnitId)"
                    type="button"
                    text
                    rounded
                    size="small"
                    class="p-1 text-teal-700 hover:bg-teal-50 cursor-pointer"
                    title="Add Child Module"
                    data-testid="row-add-child-btn"
                    @click="handleOpenCreateChild(node.data ?? node)"
                  >
                    <PlusCircle class="w-4 h-4 text-teal-600" />
                  </Button>

                  <!-- Edit Learning Unit -->
                  <Button
                    v-if="canManage"
                    type="button"
                    text
                    rounded
                    size="small"
                    class="p-1 text-slate-500 hover:text-slate-800 hover:bg-slate-100 cursor-pointer"
                    title="Edit Learning Unit"
                    data-testid="row-edit-unit-btn"
                    @click="handleOpenEdit(node.data ?? node)"
                  >
                    <Edit class="w-4 h-4" />
                  </Button>

                  <!-- Delete Unit (Allowed in draft frameworks only) -->
                  <Button
                    v-if="canManage && (node.data?.status === 'draft' || node.status === 'draft')"
                    type="button"
                    text
                    rounded
                    size="small"
                    class="p-1 text-rose-500 hover:bg-rose-50 cursor-pointer"
                    title="Delete Learning Unit"
                    data-testid="row-delete-unit-btn"
                    @click="handleDeleteUnit(node.data ?? node)"
                  >
                    <Trash2 class="w-4 h-4" />
                  </Button>
                </div>
              </template>
            </Column>
          </TreeTable>
        </div>
      </div>
    </ContentCard>

    <!-- Modals for Learning Unit Creation & Editing -->
    <CreateLearningUnitModal
      v-model:visible="isCreateModalOpen"
      :structure-id="structureId"
      :parent-unit-id="selectedParentUnitId"
      :root-units="rootUnits"
    />

    <EditLearningUnitModal
      v-model:visible="isEditModalOpen"
      :structure-id="structureId"
      :unit="selectedUnitForEdit"
      :is-structure-active="isStructureActive"
      :root-units="rootUnits"
    />

    <!-- Confirmation Dialog for Deletions -->
    <ConfirmDialog />
  </div>
</template>
