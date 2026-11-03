<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import TreeTable from 'primevue/treetable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import {
  PlusCircle,
  Layers,
  ShieldAlert,
  Calendar,
  FolderTree,
  Lock,
  CheckCircle2,
} from 'lucide-vue-next';
import { ContentCard, StatusPill, LoadingSpinner, EmptyState } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../../constants/permissions';
import {
  useAcademicYearsQuery,
  useAcademicYearDetailQuery,
} from '../../composables/useAcademicYearsQuery';
import {
  useOperationalPeriodsQuery,
  useCloseOperationalPeriodMutation,
  useActivateOperationalPeriodMutation,
} from '../../composables/useOperationalPeriodsQuery';
import type { OperationalPeriodDto } from '../../api/operationalPeriod.api';
import type { CorrectionTargetTable } from '../../schemas/recordCorrection.schema';
import { CreatePeriodModal, ApplyPresetModal, CorrectRecordModal } from '../modals';

const props = defineProps<{
  academicYearId?: string | null | undefined;
}>();

const authStore = useAuthStore();
const canManage = computed(() => authStore.hasPermission(CALENDAR_PERMISSIONS.MANAGE));
const canCorrect = computed(
  () => authStore.hasPermission(CALENDAR_PERMISSIONS.CORRECT_RECORD) || canManage.value,
);

// --------------------------------------------------------------------------
// 1. Academic Year Query & Details
// --------------------------------------------------------------------------
const { data: academicYears } = useAcademicYearsQuery();
const { data: academicYearDetail } = useAcademicYearDetailQuery(() => props.academicYearId, {
  enabled: computed(() => Boolean(props.academicYearId)),
});

const currentAcademicYear = computed(() => {
  if (academicYearDetail.value) return academicYearDetail.value;
  if (!props.academicYearId || !academicYears.value) return null;
  return academicYears.value.find((y) => y.id === props.academicYearId) ?? null;
});

const academicYearStatusVariant = computed(() => {
  const s = currentAcademicYear.value?.status;
  if (s === 'active' || s === 'approved' || s === 'completed') return 'emerald';
  return 'slate';
});

// --------------------------------------------------------------------------
// 2. Operational Periods Query & Tree Construction
// --------------------------------------------------------------------------
const { data: operationalPeriods, isLoading: isLoadingPeriods } = useOperationalPeriodsQuery(
  () => props.academicYearId,
  { enabled: computed(() => Boolean(props.academicYearId)) },
);

export interface PeriodTreeNode {
  key: string;
  data: OperationalPeriodDto;
  children?: PeriodTreeNode[];
}

const expandedKeys = ref<Record<string, boolean>>({});

const periodTreeNodes = computed<PeriodTreeNode[]>(() => {
  const list = operationalPeriods.value;
  if (!list || list.length === 0) return [];

  const nodeMap = new Map<string, PeriodTreeNode>();
  list.forEach((p) => {
    nodeMap.set(p.id, {
      key: p.id,
      data: p,
      children: [],
    });
  });

  const roots: PeriodTreeNode[] = [];
  list.forEach((p) => {
    const node = nodeMap.get(p.id)!;
    if (p.parentPeriodId && nodeMap.has(p.parentPeriodId)) {
      nodeMap.get(p.parentPeriodId)!.children!.push(node);
    } else {
      roots.push(node);
    }
  });

  function cleanNodes(nodes: PeriodTreeNode[]): PeriodTreeNode[] {
    return nodes.map((n) => {
      if (n.children && n.children.length > 0) {
        return {
          ...n,
          children: cleanNodes(n.children),
        };
      }
      const { children: _children, ...rest } = n;
      return rest;
    });
  }

  return cleanNodes(roots);
});

// Synchronize expandedKeys automatically when new periods load
watch(
  periodTreeNodes,
  (nodes) => {
    const keys: Record<string, boolean> = {};
    function collectKeys(list: PeriodTreeNode[]): void {
      list.forEach((n) => {
        keys[n.key] = true;
        if (n.children && n.children.length > 0) {
          collectKeys(n.children);
        }
      });
    }
    collectKeys(nodes);
    expandedKeys.value = keys;
  },
  { immediate: true },
);

// --------------------------------------------------------------------------
// 3. UI Helpers & Formatters
// --------------------------------------------------------------------------
function getPeriodTypeBadge(type: string): { label: string; bgClass: string } {
  switch (type) {
    case 'semester':
      return { label: 'Semester', bgClass: 'bg-teal-50 text-teal-700 border-teal-200' };
    case 'term':
      return { label: 'Term', bgClass: 'bg-blue-50 text-blue-700 border-blue-200' };
    case 'quarter':
      return { label: 'Quarter', bgClass: 'bg-sky-50 text-sky-700 border-sky-200' };
    case 'admission_window':
      return {
        label: 'Admission Window',
        bgClass: 'bg-purple-50 text-purple-700 border-purple-200',
      };
    case 'assessment_window':
      return { label: 'Assessment Window', bgClass: 'bg-amber-50 text-amber-700 border-amber-200' };
    case 'reporting_period':
      return {
        label: 'Reporting Period',
        bgClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
      };
    case 'event_period':
      return { label: 'Event Period', bgClass: 'bg-pink-50 text-pink-700 border-pink-200' };
    default:
      return { label: type || 'Period', bgClass: 'bg-slate-50 text-slate-700 border-slate-200' };
  }
}

function getPeriodStatusVariant(status: string): 'slate' | 'emerald' | 'amber' | 'rose' | 'teal' {
  switch (status) {
    case 'active':
      return 'emerald';
    case 'approved':
      return 'teal';
    case 'closed':
      return 'slate';
    case 'draft':
    default:
      return 'slate';
  }
}

// --------------------------------------------------------------------------
// 4. Modal Triggers & State
// --------------------------------------------------------------------------
const isCreatePeriodModalOpen = ref(false);
const isApplyPresetModalOpen = ref(false);
const isCorrectRecordModalOpen = ref(false);

const correctionContext = ref<{
  tableName: CorrectionTargetTable;
  recordId: string;
  fieldName: string;
  oldValue: string;
}>({
  tableName: 'operational_periods',
  recordId: '',
  fieldName: 'name',
  oldValue: '',
});

function openCorrectRecordModal(
  tableName: CorrectionTargetTable,
  recordId: string,
  fieldName: string,
  oldValue: string,
): void {
  correctionContext.value = {
    tableName,
    recordId,
    fieldName,
    oldValue,
  };
  isCorrectRecordModalOpen.value = true;
}

function handleCorrectAcademicYear(): void {
  if (!currentAcademicYear.value) return;
  openCorrectRecordModal(
    'academic_years',
    currentAcademicYear.value.id,
    'name',
    currentAcademicYear.value.name,
  );
}

function handleCorrectPeriod(period: OperationalPeriodDto): void {
  openCorrectRecordModal('operational_periods', period.id, 'name', period.name);
}

// Activate Period Mutation
const activatePeriodMutation = useActivateOperationalPeriodMutation();

function handleActivatePeriod(period: OperationalPeriodDto): void {
  activatePeriodMutation.mutate(period.id);
}

// Close Period Mutation
const closePeriodMutation = useCloseOperationalPeriodMutation();

function handleClosePeriod(period: OperationalPeriodDto): void {
  closePeriodMutation.mutate(period.id);
}
</script>

<template>
  <div class="space-y-6" data-testid="periods-tree-tab">
    <!-- -------------------------------------------------------------------- -->
    <!-- 1. Academic Year Summary Card                                        -->
    <!-- -------------------------------------------------------------------- -->
    <ContentCard
      v-if="currentAcademicYear"
      no-padding
      class="border border-slate-200 bg-white shadow-xs"
      data-testid="academic-year-summary-card"
    >
      <div class="p-5 space-y-4">
        <!-- Card Header with Badges & Actions -->
        <div
          class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3"
        >
          <div class="flex items-center gap-3">
            <Calendar class="w-5 h-5 text-teal-600" />
            <div>
              <div class="flex items-center gap-2">
                <h3
                  class="text-base font-bold text-slate-900 font-mono"
                  data-testid="ay-summary-code"
                >
                  {{ currentAcademicYear.code }}
                </h3>
                <StatusPill
                  :status="academicYearStatusVariant"
                  :label="currentAcademicYear.status"
                  size="sm"
                  data-testid="ay-summary-status-pill"
                />
              </div>
              <p class="text-xs text-slate-500 font-medium" data-testid="ay-summary-name">
                {{ currentAcademicYear.name }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <Button
              v-if="canCorrect"
              type="button"
              size="small"
              outlined
              class="text-xs text-slate-700 hover:text-amber-700 border-slate-300 flex items-center gap-1.5 cursor-pointer"
              data-testid="action-correct-year-btn"
              @click="handleCorrectAcademicYear"
            >
              <ShieldAlert class="w-3.5 h-3.5 text-amber-600" />
              <span>Correct Year Record</span>
            </Button>
          </div>
        </div>

        <!-- Academic Year Details Grid -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs">
          <!-- Effective Date Span -->
          <div class="space-y-1">
            <span class="text-slate-400 font-medium uppercase tracking-wider text-[11px] block">
              Effective Date Span
            </span>
            <div
              class="flex items-center gap-1.5 text-slate-800 font-mono font-medium"
              data-testid="ay-summary-dates"
            >
              <span>{{ currentAcademicYear.startDate }}</span>
              <span class="text-slate-400">to</span>
              <span>{{ currentAcademicYear.endDate }}</span>
            </div>
          </div>

          <!-- Approver Information -->
          <div class="space-y-1">
            <span class="text-slate-400 font-medium uppercase tracking-wider text-[11px] block">
              Approval Authority
            </span>
            <div class="text-slate-800" data-testid="ay-summary-approver">
              <span v-if="currentAcademicYear.approvedBy" class="font-medium">
                {{ currentAcademicYear.approvedBy }}
                <span class="text-slate-400 font-normal"
                  >({{ currentAcademicYear.approvedDate || 'Recorded' }})</span
                >
              </span>
              <span v-else class="text-slate-400 italic"> Pending Institutional Approval </span>
            </div>
          </div>

          <!-- Policy & Governance Notes -->
          <div class="space-y-1">
            <span class="text-slate-400 font-medium uppercase tracking-wider text-[11px] block">
              Governance Notes
            </span>
            <p
              class="text-slate-600 truncate"
              :title="currentAcademicYear.notes || ''"
              data-testid="ay-summary-notes"
            >
              {{ currentAcademicYear.notes || 'No institutional notes recorded.' }}
            </p>
          </div>
        </div>
      </div>
    </ContentCard>

    <!-- -------------------------------------------------------------------- -->
    <!-- 2. Hierarchical Operational Periods TreeTable Card                   -->
    <!-- -------------------------------------------------------------------- -->
    <ContentCard
      no-padding
      class="border border-slate-200 bg-white shadow-xs overflow-hidden"
      data-testid="periods-tree-card"
    >
      <!-- Tree Header Toolbar -->
      <div
        class="px-5 py-4 border-b border-slate-200 bg-slate-50/70 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
      >
        <div class="space-y-0.5">
          <div class="flex items-center gap-2">
            <FolderTree class="w-4 h-4 text-teal-600" />
            <h3 class="text-sm font-semibold text-slate-800">Operational Periods Hierarchy</h3>
          </div>
          <p class="text-xs text-slate-500">
            Interactive multi-level periods tree (semesters, terms, assessment & admission windows).
          </p>
        </div>

        <!-- Action Buttons: Add Period, Apply Preset, Correct Record -->
        <div class="flex items-center gap-2 flex-wrap" data-testid="periods-tab-action-buttons">
          <!-- Add Period Button -->
          <Button
            v-if="canManage"
            type="button"
            size="small"
            data-testid="tab-action-add-period-btn"
            :disabled="!academicYearId"
            class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5 cursor-pointer text-xs"
            @click="isCreatePeriodModalOpen = true"
          >
            <PlusCircle class="w-3.5 h-3.5" />
            <span>Add Period</span>
          </Button>

          <!-- Apply Preset Button -->
          <Button
            v-if="canManage"
            type="button"
            size="small"
            outlined
            data-testid="tab-action-apply-preset-btn"
            :disabled="!academicYearId"
            class="text-indigo-700 hover:bg-indigo-50 border-indigo-200 flex items-center gap-1.5 cursor-pointer text-xs"
            @click="isApplyPresetModalOpen = true"
          >
            <Layers class="w-3.5 h-3.5 text-indigo-600" />
            <span>Apply Preset</span>
          </Button>

          <!-- Correct Record Button -->
          <Button
            v-if="canCorrect"
            type="button"
            size="small"
            outlined
            data-testid="tab-action-correct-record-btn"
            :disabled="!academicYearId"
            class="text-amber-700 hover:bg-amber-50 border-amber-200 flex items-center gap-1.5 cursor-pointer text-xs"
            @click="
              openCorrectRecordModal(
                'operational_periods',
                operationalPeriods?.[0]?.id || '',
                'name',
                operationalPeriods?.[0]?.name || '',
              )
            "
          >
            <ShieldAlert class="w-3.5 h-3.5 text-amber-600" />
            <span>Correct Record</span>
          </Button>
        </div>
      </div>

      <!-- State: Loading -->
      <div
        v-if="isLoadingPeriods"
        class="py-12 flex justify-center"
        data-testid="periods-tree-loading"
      >
        <LoadingSpinner size="md" message="Loading operational periods hierarchy..." />
      </div>

      <!-- State: Empty -->
      <div
        v-else-if="!periodTreeNodes || periodTreeNodes.length === 0"
        class="p-8"
        data-testid="periods-tree-empty"
      >
        <EmptyState
          title="No Operational Periods Defined"
          description="This academic year does not have any operational periods configured yet. Apply the standard Indonesian 2-semester preset or add custom periods manually."
        >
          <template #actions>
            <div class="flex items-center gap-2 justify-center">
              <Button
                v-if="canManage"
                type="button"
                size="small"
                outlined
                class="text-indigo-700 hover:bg-indigo-50 border-indigo-300"
                data-testid="empty-apply-preset-btn"
                @click="isApplyPresetModalOpen = true"
              >
                <Layers class="w-4 h-4 mr-1.5 text-indigo-600" />
                <span>Apply 2-Semester Preset</span>
              </Button>
              <Button
                v-if="canManage"
                type="button"
                size="small"
                class="bg-teal-600 hover:bg-teal-700 text-white"
                data-testid="empty-add-period-btn"
                @click="isCreatePeriodModalOpen = true"
              >
                <PlusCircle class="w-4 h-4 mr-1.5" />
                <span>Add Period</span>
              </Button>
            </div>
          </template>
        </EmptyState>
      </div>

      <!-- State: TreeTable of Operational Periods -->
      <div v-else class="overflow-x-auto" data-testid="periods-treetable-container">
        <TreeTable
          v-model:expanded-keys="expandedKeys"
          :value="periodTreeNodes"
          data-key="key"
          class="p-treetable-sm text-xs w-full"
          data-testid="periods-treetable"
        >
          <!-- Period Name with Expander -->
          <Column field="name" header="Period Name" expander style="min-width: 240px">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <div class="inline-flex items-center gap-2">
                <span class="font-semibold text-slate-800 text-xs">
                  {{ node.data.name }}
                </span>
                <span
                  v-if="node.data.notes"
                  class="text-[11px] text-slate-400 italic truncate max-w-[140px]"
                  :title="node.data.notes"
                >
                  — {{ node.data.notes }}
                </span>
              </div>
            </template>
          </Column>

          <!-- Period Code -->
          <Column field="periodCode" header="Code" style="min-width: 120px">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <span class="font-mono font-semibold text-slate-700 text-xs">
                {{ node.data.periodCode }}
              </span>
            </template>
          </Column>

          <!-- Period Type Badge -->
          <Column field="periodType" header="Type" style="min-width: 140px">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <span
                :class="[
                  'inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium border',
                  getPeriodTypeBadge(node.data.periodType).bgClass,
                ]"
              >
                {{ getPeriodTypeBadge(node.data.periodType).label }}
              </span>
            </template>
          </Column>

          <!-- Date Span -->
          <Column header="Date Span" style="min-width: 190px">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <div class="flex items-center gap-1.5 font-mono text-[11px] text-slate-700">
                <span>{{ node.data.startDate }}</span>
                <span class="text-slate-400">→</span>
                <span>{{ node.data.endDate }}</span>
              </div>
            </template>
          </Column>

          <!-- Campus Context -->
          <Column field="campusContext" header="Campus" style="min-width: 100px">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <span class="text-slate-600 text-xs">
                {{ node.data.campusContext || 'All' }}
              </span>
            </template>
          </Column>

          <!-- Status -->
          <Column field="status" header="Status" style="min-width: 100px">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <StatusPill
                :status="getPeriodStatusVariant(node.data.status)"
                :label="node.data.status"
                size="sm"
              />
            </template>
          </Column>

          <!-- Actions -->
          <Column header="Actions" style="min-width: 130px; text-align: right">
            <template #body="{ node }: { node: PeriodTreeNode }">
              <div class="flex items-center justify-end gap-1">
                <!-- Activate Period Action -->
                <Button
                  v-if="
                    canManage && (node.data.status === 'draft' || node.data.status === 'approved')
                  "
                  type="button"
                  text
                  rounded
                  size="small"
                  class="p-1 text-slate-500 hover:text-emerald-600 cursor-pointer"
                  title="Activate operational period"
                  data-testid="row-activate-period-btn"
                  :loading="activatePeriodMutation.isPending.value"
                  @click="handleActivatePeriod(node.data)"
                >
                  <CheckCircle2 class="w-3.5 h-3.5" />
                </Button>

                <!-- Correct Period Action -->
                <Button
                  v-if="canCorrect"
                  type="button"
                  text
                  rounded
                  size="small"
                  class="p-1 text-slate-500 hover:text-amber-600 cursor-pointer"
                  title="Submit authorized record correction (UC-OPS-13)"
                  data-testid="row-correct-period-btn"
                  @click="handleCorrectPeriod(node.data)"
                >
                  <ShieldAlert class="w-3.5 h-3.5" />
                </Button>

                <!-- Close Period Action -->
                <Button
                  v-if="
                    canManage && (node.data.status === 'active' || node.data.status === 'approved')
                  "
                  type="button"
                  text
                  rounded
                  size="small"
                  class="p-1 text-slate-500 hover:text-rose-600 cursor-pointer"
                  title="Close operational period"
                  data-testid="row-close-period-btn"
                  :loading="closePeriodMutation.isPending.value"
                  @click="handleClosePeriod(node.data)"
                >
                  <Lock class="w-3.5 h-3.5" />
                </Button>
              </div>
            </template>
          </Column>
        </TreeTable>
      </div>
    </ContentCard>

    <!-- -------------------------------------------------------------------- -->
    <!-- 3. Modals & Dialogs                                                  -->
    <!-- -------------------------------------------------------------------- -->

    <!-- Create Period Modal -->
    <CreatePeriodModal
      v-model:visible="isCreatePeriodModalOpen"
      :academic-year-id="academicYearId"
      :periods="operationalPeriods"
    />

    <!-- Apply Preset Modal -->
    <ApplyPresetModal
      v-model:visible="isApplyPresetModalOpen"
      :academic-year-id="academicYearId"
      :academic-year-code="currentAcademicYear?.code"
    />

    <!-- Correct Record Modal -->
    <CorrectRecordModal
      v-model:visible="isCorrectRecordModalOpen"
      :academic-year-id="academicYearId"
      :target-table-name="correctionContext.tableName"
      :target-record-id="correctionContext.recordId"
      :field-name="correctionContext.fieldName"
      :old-value="correctionContext.oldValue"
      :periods="operationalPeriods"
      :academic-year-code="currentAcademicYear?.code"
    />
  </div>
</template>
