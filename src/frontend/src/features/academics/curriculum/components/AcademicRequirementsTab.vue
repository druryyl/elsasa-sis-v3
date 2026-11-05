<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import { useConfirm } from 'primevue/useconfirm';
import {
  ShieldCheck,
  Plus,
  Pencil,
  Trash2,
  Calendar,
  Layers,
  BookOpen,
  Lock,
} from 'lucide-vue-next';
import { ContentCard, EmptyState, LoadingSpinner } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { CURRICULUM_PERMISSIONS } from '../constants/permissions';
import {
  useAcademicRequirements,
  useDeleteRequirement,
} from '../composables/useCurriculum';
import type { AcademicRequirement } from '../types/curriculum.types';

const props = defineProps<{
  structureId: string | null;
  isStructureActive?: boolean;
}>();

const emit = defineEmits<{
  (e: 'addRequirement'): void;
  (e: 'editRequirement', requirement: AcademicRequirement): void;
}>();

const authStore = useAuthStore();
const canManage = computed(() => authStore.hasPermission(CURRICULUM_PERMISSIONS.MANAGE));

const structureIdRef = computed(() => props.structureId ?? '');
const { data: requirements, isLoading, error } = useAcademicRequirements(structureIdRef);

function tryGetConfirm() {
  try {
    return useConfirm();
  } catch {
    return null;
  }
}

const confirm = tryGetConfirm();
const deleteMutation = useDeleteRequirement();

const programRequirements = computed(() => {
  return requirements.value?.filter((r) => r.requirementScope === 'program') ?? [];
});

const levelRequirements = computed(() => {
  return requirements.value?.filter((r) => r.requirementScope === 'level') ?? [];
});

const unitRequirements = computed(() => {
  return requirements.value?.filter((r) => r.requirementScope === 'learning_unit') ?? [];
});

function getTypeSeverity(type: string): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
  switch (type) {
    case 'completion':
      return 'info';
    case 'prerequisite':
      return 'warn';
    case 'credit':
      return 'success';
    case 'progression':
      return 'secondary';
    case 'graduation':
      return 'success';
    case 'assessment_policy':
      return 'danger';
    default:
      return 'info';
  }
}

function getStatusSeverity(status: string): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
  switch (status) {
    case 'draft':
      return 'secondary';
    case 'active':
      return 'success';
    case 'superseded':
      return 'warn';
    case 'archived':
      return 'danger';
    default:
      return 'secondary';
  }
}

function handleDeleteRequirement(req: AcademicRequirement) {
  if (confirm) {
    confirm.require({
      message: `Are you sure you want to delete requirement "${req.name}"? This action cannot be undone.`,
      header: 'Delete Academic Requirement',
      icon: 'pi pi-exclamation-triangle',
      acceptClass: 'p-button-danger',
      accept: () => {
        if (!props.structureId) return;
        deleteMutation.mutate({ id: req.id, structureId: props.structureId });
      },
    });
  } else {
    if (!props.structureId) return;
    deleteMutation.mutate({ id: req.id, structureId: props.structureId });
  }
}
</script>

<template>
  <div class="space-y-6" data-testid="academic-requirements-tab">
    <ContentCard title="Academic Requirements & Policy Rules" class="bg-white">
      <template #header-actions>
        <Button
          v-if="canManage"
          type="button"
          size="small"
          class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5 cursor-pointer"
          data-testid="btn-add-requirement"
          :disabled="!structureId"
          @click="emit('addRequirement')"
        >
          <Plus class="w-3.5 h-3.5" />
          <span>Add Academic Requirement</span>
        </Button>
      </template>

      <!-- Loading State -->
      <div v-if="isLoading" class="py-12 flex justify-center" data-testid="requirements-loading">
        <LoadingSpinner size="md" message="Loading academic requirements..." />
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="p-4 rounded-md bg-rose-50 text-rose-700 text-sm" data-testid="requirements-error">
        Failed to load academic requirements for this framework.
      </div>

      <!-- Empty State -->
      <div v-else-if="!requirements || requirements.length === 0" class="py-6" data-testid="requirements-empty">
        <EmptyState
          title="No Academic Requirements Defined"
          description="Curriculum frameworks must define at least one academic policy requirement (e.g. completion, prerequisite, credit quota) before approval."
        >
          <template #actions>
            <Button
              v-if="canManage"
              type="button"
              size="small"
              class="bg-teal-600 hover:bg-teal-700 text-white"
              data-testid="empty-btn-add-requirement"
              @click="emit('addRequirement')"
            >
              <Plus class="w-4 h-4 mr-1.5" />
              <span>Add Policy Requirement</span>
            </Button>
          </template>
        </EmptyState>
      </div>

      <!-- Grouped Requirements Cards -->
      <div v-else class="space-y-8" data-testid="requirements-grouped-list">
        <!-- 1. Program-wide Scope -->
        <div class="space-y-3" data-testid="group-program-requirements">
          <div class="flex items-center justify-between pb-1 border-b border-slate-200">
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
              <span class="w-2.5 h-2.5 rounded-full bg-indigo-500"></span>
              <span>Program-Wide Requirements ({{ programRequirements.length }})</span>
            </h4>
            <span class="text-[11px] text-slate-500">Applies across all units and students</span>
          </div>

          <div v-if="programRequirements.length === 0" class="text-xs text-slate-400 italic py-2 pl-4">
            No program-wide requirements defined.
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div
              v-for="req in programRequirements"
              :key="req.id"
              class="border border-slate-200 rounded-lg p-4 bg-slate-50/50 hover:bg-slate-50 transition-colors space-y-3 text-xs shadow-2xs"
              :data-testid="`requirement-card-${req.id}`"
            >
              <!-- Card Header: Name + Badges -->
              <div class="flex items-start justify-between gap-2">
                <div>
                  <h5 class="font-bold text-slate-900 text-sm leading-snug">{{ req.name }}</h5>
                  <div class="flex items-center gap-1.5 mt-1">
                    <Tag value="PROGRAM" severity="info" class="text-[10px] uppercase font-bold" />
                    <Tag
                      :value="req.requirementType.toUpperCase()"
                      :severity="getTypeSeverity(req.requirementType)"
                      class="text-[10px] font-mono"
                    />
                    <span class="text-[11px] text-slate-400 font-mono">v{{ req.version }}</span>
                  </div>
                </div>
                <Tag
                  :value="(req.status || 'draft').toUpperCase()"
                  :severity="getStatusSeverity(req.status)"
                  class="text-[10px] font-semibold"
                />
              </div>

              <!-- Criteria Description -->
              <div class="bg-white border border-slate-200/80 rounded p-2.5 text-slate-700 text-xs">
                <p class="leading-relaxed whitespace-pre-line">{{ req.description }}</p>
              </div>

              <!-- Footer: Associated Target, Dates & Actions -->
              <div class="pt-1 border-t border-slate-200/60 flex items-center justify-between text-[11px] text-slate-500">
                <div class="flex items-center gap-3">
                  <span class="flex items-center gap-1">
                    <Calendar class="w-3.5 h-3.5 text-slate-400" />
                    <span>Effective: {{ req.effectiveDate }}</span>
                  </span>
                  <span v-if="req.endDate" class="text-slate-400">to {{ req.endDate }}</span>
                </div>

                <!-- Row Actions -->
                <div v-if="canManage" class="flex items-center gap-1">
                  <Button
                    type="button"
                    text
                    rounded
                    size="small"
                    class="w-7 h-7 p-0 text-slate-600 hover:text-teal-700 hover:bg-teal-50"
                    title="Edit Requirement"
                    :data-testid="`edit-req-btn-${req.id}`"
                    @click="emit('editRequirement', req)"
                  >
                    <Pencil class="w-3.5 h-3.5" />
                  </Button>
                  <Button
                    v-if="req.status === 'draft' || !isStructureActive"
                    type="button"
                    text
                    rounded
                    size="small"
                    class="w-7 h-7 p-0 text-slate-600 hover:text-rose-700 hover:bg-rose-50"
                    title="Delete Requirement"
                    :loading="deleteMutation.isPending.value"
                    :data-testid="`delete-req-btn-${req.id}`"
                    @click="handleDeleteRequirement(req)"
                  >
                    <Trash2 class="w-3.5 h-3.5" />
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 2. Level-specific Scope -->
        <div class="space-y-3" data-testid="group-level-requirements">
          <div class="flex items-center justify-between pb-1 border-b border-slate-200">
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
              <span class="w-2.5 h-2.5 rounded-full bg-teal-500"></span>
              <span>Level-Specific Requirements ({{ levelRequirements.length }})</span>
            </h4>
            <span class="text-[11px] text-slate-500">Applies to specific educational grade or stage</span>
          </div>

          <div v-if="levelRequirements.length === 0" class="text-xs text-slate-400 italic py-2 pl-4">
            No level-specific requirements defined.
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div
              v-for="req in levelRequirements"
              :key="req.id"
              class="border border-slate-200 rounded-lg p-4 bg-slate-50/50 hover:bg-slate-50 transition-colors space-y-3 text-xs shadow-2xs"
              :data-testid="`requirement-card-${req.id}`"
            >
              <!-- Card Header: Name + Badges -->
              <div class="flex items-start justify-between gap-2">
                <div>
                  <div class="flex items-center gap-2 flex-wrap">
                    <h5 class="font-bold text-slate-900 text-sm leading-snug">{{ req.name }}</h5>
                    <span
                      v-if="req.level"
                      class="px-2 py-0.5 rounded-md bg-teal-50 border border-teal-200 text-teal-800 text-[10px] font-semibold"
                    >
                      {{ req.level }}
                    </span>
                  </div>
                  <div class="flex items-center gap-1.5 mt-1">
                    <Tag value="LEVEL" severity="success" class="text-[10px] uppercase font-bold" />
                    <Tag
                      :value="req.requirementType.toUpperCase()"
                      :severity="getTypeSeverity(req.requirementType)"
                      class="text-[10px] font-mono"
                    />
                    <span class="text-[11px] text-slate-400 font-mono">v{{ req.version }}</span>
                  </div>
                </div>
                <Tag
                  :value="(req.status || 'draft').toUpperCase()"
                  :severity="getStatusSeverity(req.status)"
                  class="text-[10px] font-semibold"
                />
              </div>

              <!-- Criteria Description -->
              <div class="bg-white border border-slate-200/80 rounded p-2.5 text-slate-700 text-xs">
                <p class="leading-relaxed whitespace-pre-line">{{ req.description }}</p>
              </div>

              <!-- Footer: Associated Target, Dates & Actions -->
              <div class="pt-1 border-t border-slate-200/60 flex items-center justify-between text-[11px] text-slate-500">
                <div class="flex items-center gap-3">
                  <span class="flex items-center gap-1">
                    <Calendar class="w-3.5 h-3.5 text-slate-400" />
                    <span>Effective: {{ req.effectiveDate }}</span>
                  </span>
                  <span v-if="req.endDate" class="text-slate-400">to {{ req.endDate }}</span>
                </div>

                <!-- Row Actions -->
                <div v-if="canManage" class="flex items-center gap-1">
                  <Button
                    type="button"
                    text
                    rounded
                    size="small"
                    class="w-7 h-7 p-0 text-slate-600 hover:text-teal-700 hover:bg-teal-50"
                    title="Edit Requirement"
                    :data-testid="`edit-req-btn-${req.id}`"
                    @click="emit('editRequirement', req)"
                  >
                    <Pencil class="w-3.5 h-3.5" />
                  </Button>
                  <Button
                    v-if="req.status === 'draft' || !isStructureActive"
                    type="button"
                    text
                    rounded
                    size="small"
                    class="w-7 h-7 p-0 text-slate-600 hover:text-rose-700 hover:bg-rose-50"
                    title="Delete Requirement"
                    :loading="deleteMutation.isPending.value"
                    :data-testid="`delete-req-btn-${req.id}`"
                    @click="handleDeleteRequirement(req)"
                  >
                    <Trash2 class="w-3.5 h-3.5" />
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 3. Learning Unit Scope -->
        <div class="space-y-3" data-testid="group-unit-requirements">
          <div class="flex items-center justify-between pb-1 border-b border-slate-200">
            <h4 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
              <span class="w-2.5 h-2.5 rounded-full bg-amber-500"></span>
              <span>Learning Unit Prerequisites & Rules ({{ unitRequirements.length }})</span>
            </h4>
            <span class="text-[11px] text-slate-500">Prerequisites and rules tied to individual units</span>
          </div>

          <div v-if="unitRequirements.length === 0" class="text-xs text-slate-400 italic py-2 pl-4">
            No unit-specific requirements defined.
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div
              v-for="req in unitRequirements"
              :key="req.id"
              class="border border-slate-200 rounded-lg p-4 bg-slate-50/50 hover:bg-slate-50 transition-colors space-y-3 text-xs shadow-2xs"
              :data-testid="`requirement-card-${req.id}`"
            >
              <!-- Card Header: Name + Badges -->
              <div class="flex items-start justify-between gap-2">
                <div>
                  <div class="flex items-center gap-2 flex-wrap">
                    <h5 class="font-bold text-slate-900 text-sm leading-snug">{{ req.name }}</h5>
                    <span
                      v-if="req.learningUnitCode || req.learningUnitName"
                      class="px-2 py-0.5 rounded-md bg-amber-50 border border-amber-200 text-amber-800 text-[10px] font-semibold"
                    >
                      {{ req.learningUnitCode ? `${req.learningUnitCode} - ${req.learningUnitName}` : req.learningUnitName }}
                    </span>
                  </div>
                  <div class="flex items-center gap-1.5 mt-1">
                    <Tag value="LEARNING UNIT" severity="warn" class="text-[10px] uppercase font-bold" />
                    <Tag
                      :value="req.requirementType.toUpperCase()"
                      :severity="getTypeSeverity(req.requirementType)"
                      class="text-[10px] font-mono"
                    />
                    <span class="text-[11px] text-slate-400 font-mono">v{{ req.version }}</span>
                  </div>
                </div>
                <Tag
                  :value="(req.status || 'draft').toUpperCase()"
                  :severity="getStatusSeverity(req.status)"
                  class="text-[10px] font-semibold"
                />
              </div>

              <!-- Criteria Description -->
              <div class="bg-white border border-slate-200/80 rounded p-2.5 text-slate-700 text-xs">
                <p class="leading-relaxed whitespace-pre-line">{{ req.description }}</p>
              </div>

              <!-- Footer: Associated Target, Dates & Actions -->
              <div class="pt-1 border-t border-slate-200/60 flex items-center justify-between text-[11px] text-slate-500">
                <div class="flex items-center gap-3">
                  <span class="flex items-center gap-1">
                    <Calendar class="w-3.5 h-3.5 text-slate-400" />
                    <span>Effective: {{ req.effectiveDate }}</span>
                  </span>
                  <span v-if="req.endDate" class="text-slate-400">to {{ req.endDate }}</span>
                </div>

                <!-- Row Actions -->
                <div v-if="canManage" class="flex items-center gap-1">
                  <Button
                    type="button"
                    text
                    rounded
                    size="small"
                    class="w-7 h-7 p-0 text-slate-600 hover:text-teal-700 hover:bg-teal-50"
                    title="Edit Requirement"
                    :data-testid="`edit-req-btn-${req.id}`"
                    @click="emit('editRequirement', req)"
                  >
                    <Pencil class="w-3.5 h-3.5" />
                  </Button>
                  <Button
                    v-if="req.status === 'draft' || !isStructureActive"
                    type="button"
                    text
                    rounded
                    size="small"
                    class="w-7 h-7 p-0 text-slate-600 hover:text-rose-700 hover:bg-rose-50"
                    title="Delete Requirement"
                    :loading="deleteMutation.isPending.value"
                    :data-testid="`delete-req-btn-${req.id}`"
                    @click="handleDeleteRequirement(req)"
                  >
                    <Trash2 class="w-3.5 h-3.5" />
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </ContentCard>
  </div>
</template>
