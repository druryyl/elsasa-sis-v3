<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import {
  Sparkles,
  Plus,
  Trash2,
  AlertTriangle,
  FolderTree,
  BookOpen,
  Layers,
  Globe,
  Radio,
} from 'lucide-vue-next';
import { EmptyState, StatusPill } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { useCapabilityTypesQuery } from '../composables/useFacultyQueries';
import { useRemoveCapabilityMutation } from '../composables/useFacultyMutations';
import AddCapabilityModal from './AddCapabilityModal.vue';
import type {
  TeacherProfileDto,
  TeachingCapabilityDto,
  AddTeachingCapabilityResponse,
  CapabilityTypeDto,
} from '../api/teachingTypes';

export interface TeachingCapabilitiesTabProps {
  teacher: TeacherProfileDto | null;
  canManage?: boolean;
}

const props = withDefaults(defineProps<TeachingCapabilitiesTabProps>(), {
  teacher: null,
  canManage: false,
});

const emit = defineEmits<{
  (e: 'capabilityAdded', response: AddTeachingCapabilityResponse): void;
  (e: 'capabilityRemoved', capabilityId: string): void;
  (e: 'refresh'): void;
}>();

const authStore = useAuthStore();
const canManageAction = computed(() => {
  return props.canManage || authStore.hasPermission(TEACHING_PERMISSIONS.MANAGE);
});

// ---------------------------------------------------------------------------
// 1. Capability Taxonomy & Grouping
// ---------------------------------------------------------------------------
const { data: capabilityTypes } = useCapabilityTypesQuery();

const capabilitiesList = computed<TeachingCapabilityDto[]>(() => {
  return props.teacher?.capabilities ?? [];
});

interface CapabilityGroup {
  typeId: string;
  typeName: string;
  typeCode: string;
  items: TeachingCapabilityDto[];
}

const categorizedCapabilities = computed<CapabilityGroup[]>(() => {
  const list = capabilitiesList.value;
  if (!list.length) return [];

  const typesMap = new Map<string, CapabilityTypeDto>();
  if (capabilityTypes.value) {
    for (const t of capabilityTypes.value) {
      typesMap.set(t.id, t);
    }
  }

  const groupsMap = new Map<string, CapabilityGroup>();

  for (const cap of list) {
    const typeInfo = typesMap.get(cap.capabilityTypeId);
    const key = cap.capabilityTypeId || 'general';
    const typeName = typeInfo?.name || 'General Capabilities';
    const typeCode = typeInfo?.code || 'GEN';

    if (!groupsMap.has(key)) {
      groupsMap.set(key, {
        typeId: key,
        typeName,
        typeCode,
        items: [],
      });
    }

    groupsMap.get(key)!.items.push(cap);
  }

  return Array.from(groupsMap.values());
});

// ---------------------------------------------------------------------------
// 2. Add Capability Modal State
// ---------------------------------------------------------------------------
const isAddModalOpen = ref(false);

function handleOpenAddModal() {
  isAddModalOpen.value = true;
}

function handleCapabilityAdded(response: AddTeachingCapabilityResponse) {
  emit('capabilityAdded', response);
  emit('refresh');
}

// ---------------------------------------------------------------------------
// 3. Retire Capability Confirmation Dialog State
// ---------------------------------------------------------------------------
const capabilityToRetire = ref<TeachingCapabilityDto | null>(null);
const isRetireDialogOpen = ref(false);

const removeCapabilityMutation = useRemoveCapabilityMutation();

function handlePromptRetire(cap: TeachingCapabilityDto) {
  capabilityToRetire.value = cap;
  isRetireDialogOpen.value = true;
}

async function handleConfirmRetire() {
  if (!props.teacher?.id || !capabilityToRetire.value) return;

  const capId = capabilityToRetire.value.id;
  try {
    await removeCapabilityMutation.mutateAsync({
      teacherId: props.teacher.id,
      capabilityId: capId,
    });

    isRetireDialogOpen.value = false;
    capabilityToRetire.value = null;
    emit('capabilityRemoved', capId);
    emit('refresh');
  } catch {
    // Handled by toast
  }
}
</script>

<template>
  <div
    class="space-y-6"
    data-testid="teaching-capabilities-tab"
  >
    <!-- Section Header & Action Toolbar -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-surface-200 dark:border-surface-700">
      <div>
        <div class="flex items-center gap-2">
          <Sparkles class="w-4 h-4 text-primary-600 dark:text-primary-400" />
          <h3 class="text-sm font-semibold uppercase tracking-wider text-surface-900 dark:text-surface-100">
            Teaching Capabilities (TD-02)
          </h3>
          <span
            v-if="capabilitiesList.length"
            class="text-[11px] px-2 py-0.5 rounded-full bg-primary-50 dark:bg-primary-950/60 border border-primary-200 dark:border-primary-800 text-primary-700 dark:text-primary-300 font-medium"
            data-testid="capabilities-count-badge"
          >
            {{ capabilitiesList.length }} Registered
          </span>
        </div>
        <p class="text-xs text-surface-500 mt-0.5">
          Pedagogical suitability across academic subjects, educational stages, delivery modes, and language proficiencies.
        </p>
      </div>

      <Button
        v-if="canManageAction"
        severity="primary"
        size="small"
        class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium shrink-0"
        data-testid="open-add-capability-modal-btn"
        @click="handleOpenAddModal"
      >
        <Plus class="w-4 h-4" />
        <span>Add Capability</span>
      </Button>
    </div>

    <!-- Empty State -->
    <div
      v-if="!capabilitiesList || capabilitiesList.length === 0"
      class="p-8 text-center"
      data-testid="empty-capabilities"
    >
      <EmptyState
        title="No Teaching Capabilities Recorded"
        description="Potential capabilities represent pedagogical suitability across subjects, grade levels, and instructional delivery modes without granting formal authorizations."
        bordered
      >
        <template #actions>
          <Button
            v-if="canManageAction"
            severity="primary"
            size="small"
            class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
            data-testid="empty-add-capability-btn"
            @click="handleOpenAddModal"
          >
            <Plus class="w-4 h-4" />
            <span>Add First Capability</span>
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- Categorized Capabilities List -->
    <div
      v-else
      class="space-y-6"
      data-testid="categorized-capabilities-list"
    >
      <div
        v-for="group in categorizedCapabilities"
        :key="group.typeId"
        class="space-y-3"
        :data-testid="`capability-group-${group.typeId}`"
      >
        <!-- Group Header Pill -->
        <div class="flex items-center gap-2">
          <FolderTree class="w-4 h-4 text-surface-400" />
          <h4 class="text-xs font-semibold uppercase tracking-wide text-surface-700 dark:text-surface-300">
            {{ group.typeName }}
          </h4>
          <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-surface-100 dark:bg-surface-800 text-surface-500 font-mono">
            {{ group.items.length }}
          </span>
        </div>

        <!-- Capability Cards Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div
            v-for="cap in group.items"
            :key="cap.id"
            class="p-4 bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl space-y-3 shadow-2xs hover:border-surface-300 transition-colors"
            :data-testid="`capability-card-${cap.id}`"
          >
            <!-- Card Top: Name, Code & Status -->
            <div class="flex items-start justify-between gap-2">
              <div>
                <div class="flex items-center gap-2">
                  <span class="font-semibold text-sm text-surface-900 dark:text-surface-100">
                    {{ cap.capabilityName }}
                  </span>
                  <StatusPill
                    :status="cap.status || 'active'"
                    size="sm"
                    :data-testid="`capability-status-${cap.id}`"
                  />
                </div>
                <span class="font-mono text-[10px] text-surface-400 mt-0.5 block">
                  {{ cap.capabilityCode }}
                </span>
              </div>

              <!-- Retire / Remove Action -->
              <Button
                v-if="canManageAction && cap.status !== 'inactive'"
                severity="danger"
                variant="text"
                size="small"
                class="!p-1.5 text-surface-400 hover:text-rose-600 shrink-0"
                :title="`Retire ${cap.capabilityName}`"
                :data-testid="`retire-capability-btn-${cap.id}`"
                @click="handlePromptRetire(cap)"
              >
                <Trash2 class="w-4 h-4" />
              </Button>
            </div>

            <!-- Parameters: Level, Mode, Language -->
            <div class="flex flex-wrap gap-2 text-xs">
              <span
                v-if="cap.applicableLevel"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300"
              >
                <Layers class="w-3 h-3 text-surface-400" />
                <span>{{ cap.applicableLevel }}</span>
              </span>

              <span
                v-if="cap.deliveryMode"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300"
              >
                <Radio class="w-3 h-3 text-surface-400" />
                <span>{{ cap.deliveryMode }}</span>
              </span>

              <span
                v-if="cap.languageOfInstruction"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300"
              >
                <Globe class="w-3 h-3 text-surface-400" />
                <span>{{ cap.languageOfInstruction }}</span>
              </span>
            </div>

            <!-- Notes -->
            <p
              v-if="cap.notes"
              class="text-xs text-surface-500 italic border-l-2 border-surface-200 dark:border-surface-700 pl-2"
            >
              {{ cap.notes }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Capability Modal -->
    <AddCapabilityModal
      v-model:visible="isAddModalOpen"
      :teacher-id="teacher?.id"
      :capability-types="capabilityTypes"
      @added="handleCapabilityAdded"
    />

    <!-- Retire Capability Confirmation Dialog -->
    <Dialog
      v-model:visible="isRetireDialogOpen"
      modal
      :draggable="false"
      class="w-full max-w-md mx-4"
      data-testid="retire-capability-dialog"
    >
      <template #header>
        <div class="flex items-center gap-2 text-rose-600 dark:text-rose-400">
          <AlertTriangle class="w-5 h-5" />
          <h2 class="text-base font-semibold">
            Retire Teaching Capability
          </h2>
        </div>
      </template>

      <div class="space-y-3 pt-1 text-xs leading-relaxed text-surface-600 dark:text-surface-300">
        <p>
          Are you sure you want to retire capability
          <strong class="text-surface-900 dark:text-surface-100">
            {{ capabilityToRetire?.capabilityName }} ({{ capabilityToRetire?.capabilityCode }})
          </strong>?
        </p>
        <p class="text-surface-500">
          This soft-retires the capability record (setting status to <code>inactive</code>). Existing historical authorizations and session assignments are not deleted.
        </p>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2 pt-2">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="removeCapabilityMutation.isPending.value"
            data-testid="cancel-retire-capability-btn"
            @click="isRetireDialogOpen = false"
          />
          <Button
            type="button"
            label="Retire Capability"
            icon="pi pi-trash"
            severity="danger"
            size="small"
            :loading="removeCapabilityMutation.isPending.value"
            data-testid="confirm-retire-capability-btn"
            @click="handleConfirmRetire"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
