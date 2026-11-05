<script setup lang="ts">
import { computed } from 'vue';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  Calendar,
  Layers,
  Sparkles,
  Send,
  CheckCircle2,
  CopyPlus,
  Archive,
  PlusCircle,
  Pencil,
} from 'lucide-vue-next';
import { ContentCard } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import type {
  LearningStructureSummary,
  LearningStructureStatus,
} from '../types/curriculum.types';

export interface AcademicPeriodOption {
  id: string;
  label: string;
  code?: string;
}

const props = defineProps<{
  years: AcademicPeriodOption[];
  selectedYearId: string | null;
  periods: AcademicPeriodOption[];
  selectedPeriodId: string | null;
  structures: LearningStructureSummary[];
  selectedStructureId: string | null;
  selectedStructure: LearningStructureSummary | null;
  isLoadingPeriods?: boolean;
  isLoadingStructures?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:selectedYearId', value: string | null): void;
  (e: 'update:selectedPeriodId', value: string | null): void;
  (e: 'update:selectedStructureId', value: string | null): void;
  (e: 'createStructure'): void;
  (e: 'editStructure'): void;
  (e: 'submitStructure'): void;
  (e: 'approveStructure'): void;
  (e: 'supersedeStructure'): void;
  (e: 'archiveStructure'): void;
}>();

const authStore = useAuthStore();

// Permissions
const canManage = computed(() =>
  authStore.hasPermission(CURRICULUM_PERMISSIONS.MANAGE)
);
const canSubmit = computed(() =>
  authStore.hasPermission(CURRICULUM_PERMISSIONS.SUBMIT)
);
const canApprove = computed(() =>
  authStore.hasPermission(CURRICULUM_PERMISSIONS.APPROVE)
);
const canArchive = computed(() =>
  authStore.hasPermission(CURRICULUM_PERMISSIONS.ARCHIVE)
);

// Model bindings
const yearModel = computed({
  get: () => props.selectedYearId,
  set: (val) => emit('update:selectedYearId', val),
});
const periodModel = computed({
  get: () => props.selectedPeriodId,
  set: (val) => emit('update:selectedPeriodId', val),
});

const structureModel = computed({
  get: () => props.selectedStructureId,
  set: (val) => emit('update:selectedStructureId', val),
});

// Options formatting
const structureOptions = computed(() => {
  return props.structures.map((s) => ({
    id: s.id,
    label: `${s.name} (${s.version}) - ${s.status.toUpperCase()}`,
    status: s.status,
    version: s.version,
    name: s.name,
  }));
});

// Status and Version styling
const currentStatus = computed<LearningStructureStatus | null>(() => {
  return (props.selectedStructure?.status as LearningStructureStatus | null | undefined) ?? null;
});

const statusSeverity = computed<'secondary' | 'info' | 'success' | 'warn' | 'danger' | 'contrast'>(() => {
  switch (currentStatus.value) {
    case 'draft':
      return 'secondary';
    case 'submitted':
      return 'info';
    case 'active':
      return 'success';
    case 'superseded':
      return 'warn';
    case 'archived':
      return 'danger';
    default:
      return 'secondary';
  }
});

// Action button validity states
const canSubmitAction = computed(() => {
  return currentStatus.value === 'draft';
});

const canEditAction = computed(() => {
  // Metadata can be edited in draft, submitted, or active status
  return currentStatus.value === 'draft' || currentStatus.value === 'submitted' || currentStatus.value === 'active';
});

const canApproveAction = computed(() => {
  // Stage 2: Authority can approve draft or submitted structures
  return currentStatus.value === 'submitted' || currentStatus.value === 'draft';
});

const canSupersedeAction = computed(() => {
  // Deep clone-on-supersede is strictly allowed only on active structures (TD-03)
  return currentStatus.value === 'active';
});

const canArchiveAction = computed(() => {
  // Can archive active or superseded structures
  return currentStatus.value === 'active' || currentStatus.value === 'superseded';
});
</script>

<template>
  <div class="space-y-3" data-testid="curriculum-context-bar">
    <!-- Top Selector & Identification Ribbon -->
    <ContentCard no-padding class="bg-white border border-slate-200 shadow-xs">
      <div class="px-5 py-3 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <!-- Selectors Group -->
        <div class="flex items-center gap-4 flex-wrap">
          <!-- Academic Year Selector (drives the operational-period query) -->
          <div class="flex items-center gap-2">
            <Calendar class="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
            <span class="text-xs font-medium text-slate-600">Year:</span>
            <Select
              v-model="yearModel"
              :options="years"
              option-label="label"
              option-value="id"
              placeholder="Select Academic Year"
              data-testid="academic-year-select"
              class="w-56 text-sm"
            />
          </div>

          <!-- Academic Period Selector (operational periods nested under the year) -->
          <div class="flex items-center gap-2">
            <Calendar class="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
            <span class="text-xs font-medium text-slate-600">Period:</span>
            <Select
              v-model="periodModel"
              :options="periods"
              option-label="label"
              option-value="id"
              placeholder="Select Academic Period"
              data-testid="academic-period-select"
              class="w-56 text-sm"
              :loading="isLoadingPeriods"
              :disabled="!selectedYearId || periods.length === 0"
            />
          </div>

          <!-- Learning Structure Selector -->
          <div class="flex items-center gap-2">
            <Layers class="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
            <span class="text-xs font-medium text-slate-600">Structure:</span>
            <Select
              v-model="structureModel"
              :options="structureOptions"
              option-label="label"
              option-value="id"
              placeholder="Select Learning Structure"
              data-testid="learning-structure-select"
              class="w-72 text-sm"
              :loading="isLoadingStructures"
              :disabled="!selectedPeriodId || structures.length === 0"
            />
          </div>
        </div>

        <!-- Badges & Identity Group (Version pill & Status badge) -->
        <div
          v-if="selectedStructure"
          class="flex items-center gap-2.5"
          data-testid="structure-identity-badges"
        >
          <!-- Version Pill -->
          <span
            class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 text-slate-800 border border-slate-300 font-mono"
            data-testid="version-pill"
          >
            v{{ selectedStructure.version }}
          </span>

          <!-- Status Badge -->
          <Tag
            :value="selectedStructure.status.toUpperCase()"
            :severity="statusSeverity"
            data-testid="status-badge"
            class="text-xs uppercase tracking-wider px-2 py-0.5"
          />
        </div>
      </div>
    </ContentCard>

    <!-- Context Action Toolbar -->
    <ContentCard
      no-padding
      class="bg-slate-50/80 border border-slate-200"
      data-testid="context-action-toolbar"
    >
      <div class="px-5 py-2.5 flex flex-col md:flex-row md:items-center justify-between gap-3 text-xs">
        <!-- Left: Summary / Info -->
        <div class="flex items-center gap-2">
          <span class="text-slate-500">Actions for Framework:</span>
          <span v-if="selectedStructure" class="font-semibold text-slate-800">
            {{ selectedStructure.name }}
          </span>
          <span v-else class="text-slate-400 italic">No structure selected</span>
        </div>

        <!-- Right: Action Buttons with Segregation of Duties -->
        <div class="flex items-center gap-2 flex-wrap" data-testid="curriculum-action-buttons">
          <!-- Coordinator Actions -->
          <Button
            v-if="canManage"
            type="button"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5 cursor-pointer"
            data-testid="btn-new-structure"
            :disabled="!selectedPeriodId"
            title="Create a new draft curriculum framework for this period"
            @click="emit('createStructure')"
          >
            <PlusCircle class="w-3.5 h-3.5" />
            <span>New Structure</span>
          </Button>

          <Button
            v-if="canManage"
            type="button"
            size="small"
            outlined
            data-testid="btn-edit-metadata"
            :disabled="!selectedStructure || !canEditAction"
            title="Edit curriculum metadata (name, description, notes)"
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-slate-900 disabled:opacity-50"
            @click="emit('editStructure')"
          >
            <Pencil class="w-3.5 h-3.5" />
            <span>Edit Metadata</span>
          </Button>

          <Button
            v-if="canSubmit || canManage"
            type="button"
            size="small"
            outlined
            data-testid="btn-submit-approval"
            :disabled="!selectedStructure || !canSubmitAction"
            :title="
              canSubmitAction
                ? 'Submit draft curriculum framework for formal authority review'
                : 'Structure must be in draft status to submit for approval'
            "
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-sky-700 disabled:opacity-50"
            @click="emit('submitStructure')"
          >
            <Send class="w-3.5 h-3.5 text-sky-600" />
            <span>Submit for Approval</span>
          </Button>

          <!-- Authority Actions -->
          <Button
            v-if="canApprove"
            type="button"
            size="small"
            outlined
            data-testid="btn-approve-activate"
            :disabled="!selectedStructure || !canApproveAction"
            :title="
              canApproveAction
                ? 'Formally approve and activate framework (requires ≥1 unit and ≥1 requirement)'
                : 'Structure must be in draft or submitted status to approve'
            "
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-emerald-700 disabled:opacity-50"
            @click="emit('approveStructure')"
          >
            <CheckCircle2 class="w-3.5 h-3.5 text-emerald-600" />
            <span>Approve & Activate</span>
          </Button>

          <Button
            v-if="canApprove || canManage"
            type="button"
            size="small"
            outlined
            data-testid="btn-supersede-structure"
            :disabled="!selectedStructure || !canSupersedeAction"
            :title="
              canSupersedeAction
                ? 'Deep-clone active framework into a new draft version for structural revisions'
                : 'Only active curriculum frameworks can be superseded into a new version'
            "
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-indigo-700 disabled:opacity-50"
            @click="emit('supersedeStructure')"
          >
            <CopyPlus class="w-3.5 h-3.5 text-indigo-600" />
            <span>Supersede / New Version</span>
          </Button>

          <Button
            v-if="canArchive"
            type="button"
            size="small"
            outlined
            severity="danger"
            data-testid="btn-archive-structure"
            :disabled="!selectedStructure || !canArchiveAction"
            :title="
              canArchiveAction
                ? 'Archive framework to read-only historical record'
                : 'Only active or superseded frameworks can be archived'
            "
            class="flex items-center gap-1 cursor-pointer text-rose-700 hover:bg-rose-50 disabled:opacity-50"
            @click="emit('archiveStructure')"
          >
            <Archive class="w-3.5 h-3.5 text-rose-600" />
            <span>Archive</span>
          </Button>
        </div>
      </div>
    </ContentCard>
  </div>
</template>
