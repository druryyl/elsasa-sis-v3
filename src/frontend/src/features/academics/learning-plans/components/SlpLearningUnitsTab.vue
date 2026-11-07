<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import { EmptyState } from '@/shared/components';
import type {
  StudentLearningPlan,
  StudentLearningPlanStatus,
  SlpLearningUnit,
} from '../types';
import { useRemoveUnitMutation } from '../composables/useLearningPlans';
import AddSlpUnitModal, {
  type LearningUnitOption,
} from './AddSlpUnitModal.vue';

export interface SlpLearningUnitsTabProps {
  plan?: StudentLearningPlan | null;
  planId?: string;
  status?: StudentLearningPlanStatus;
  units?: SlpLearningUnit[];
  availableUnits?: LearningUnitOption[];
  readonly?: boolean;
}

const props = withDefaults(defineProps<SlpLearningUnitsTabProps>(), {
  plan: null,
  planId: undefined,
  status: undefined,
  units: undefined,
  availableUnits: undefined,
  readonly: false,
});

const emit = defineEmits<{
  (e: 'unitAdded', unitId: string): void;
  (e: 'unitRemoved', unitId: string): void;
}>();

// ---------------------------------------------------------------------------
// 1. Reactive Plan Identification & State
// ---------------------------------------------------------------------------
const effectivePlanId = computed(() => props.plan?.id ?? props.planId ?? '');
const effectiveStatus = computed<StudentLearningPlanStatus>(
  () => props.plan?.status ?? props.status ?? 'draft',
);
const isDraft = computed(() => effectiveStatus.value === 'draft' && !props.readonly);

const unitsList = computed<SlpLearningUnit[]>(() => {
  if (props.units !== undefined) return props.units;
  return props.plan?.constituentUnits ?? [];
});

const sortedUnits = computed<SlpLearningUnit[]>(() => {
  return [...unitsList.value].sort((a, b) => {
    const seqA = a.sequenceOrder ?? 999999;
    const seqB = b.sequenceOrder ?? 999999;
    if (seqA !== seqB) return seqA - seqB;
    return a.unitCode.localeCompare(b.unitCode);
  });
});

const requiredCount = computed(
  () => unitsList.value.filter((u) => u.isRequired).length,
);
const electiveCount = computed(
  () => unitsList.value.filter((u) => !u.isRequired).length,
);

// ---------------------------------------------------------------------------
// 2. Add Unit Modal State
// ---------------------------------------------------------------------------
const isAddModalOpen = ref(false);

function handleUnitAdded(unitId: string) {
  emit('unitAdded', unitId);
}

// ---------------------------------------------------------------------------
// 3. Remove Unit Mutation & Handler
// ---------------------------------------------------------------------------
const removingUnitId = ref<string | null>(null);

const removeMutation = useRemoveUnitMutation({
  onSuccess: (_, variables) => {
    removingUnitId.value = null;
    emit('unitRemoved', variables.unitId);
  },
  onError: () => {
    removingUnitId.value = null;
  },
});

function handleRemoveUnit(unit: SlpLearningUnit) {
  if (!effectivePlanId.value) return;
  removingUnitId.value = unit.id;
  removeMutation.mutate({
    planId: effectivePlanId.value,
    unitId: unit.id,
  });
}
</script>

<template>
  <div class="space-y-4" data-testid="slp-learning-units-tab">
    <!-- Main Card Container -->
    <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-2xs overflow-hidden">
      <!-- Toolbar Header -->
      <div class="p-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between gap-4">
        <div class="flex items-center gap-2">
          <span class="text-xs font-semibold text-slate-800 dark:text-slate-200 uppercase tracking-wider">
            Constituent Learning Units ({{ unitsList.length }})
          </span>
          <span v-if="unitsList.length > 0" class="text-[11px] text-slate-500 dark:text-slate-400 hidden sm:inline">
            &bull; {{ requiredCount }} Required, {{ electiveCount }} Elective
          </span>
        </div>

        <!-- Inline "+ Add Learning Unit" action visible only when plan is draft -->
        <Button
          v-if="isDraft"
          label="Add Learning Unit"
          icon="pi pi-plus"
          size="small"
          class="text-xs font-semibold"
          data-testid="add-learning-unit-btn"
          @click="isAddModalOpen = true"
        />
      </div>

      <!-- PrimeVue DataTable -->
      <DataTable
        :value="sortedUnits"
        data-key="id"
        striped-rows
        responsive-layout="scroll"
        class="text-xs"
        data-testid="slp-learning-units-table"
      >
        <!-- Empty State with clear guidance -->
        <template #empty>
          <div class="p-8 text-center" data-testid="slp-units-empty-state">
            <EmptyState
              title="No constituent learning units configured"
              description="This learning plan has no linked curriculum units. Link learning units to establish the student's individualized academic course load."
            >
              <template #actions v-if="isDraft">
                <Button
                  label="Add Learning Unit"
                  icon="pi pi-plus"
                  size="small"
                  class="text-xs font-semibold"
                  data-testid="empty-add-unit-btn"
                  @click="isAddModalOpen = true"
                />
              </template>
            </EmptyState>
          </div>
        </template>

        <!-- Column 1: Sequence Order -->
        <Column
          field="sequenceOrder"
          header="Sequence Order"
          sortable
          style="width: 140px"
        >
          <template #body="{ data }">
            <div class="flex items-center gap-1.5 font-medium">
              <span
                v-if="data.sequenceOrder != null"
                class="w-6 h-6 rounded-md bg-indigo-50 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-300 flex items-center justify-center font-bold text-xs"
              >
                {{ data.sequenceOrder }}
              </span>
              <span v-else class="text-slate-400">—</span>
            </div>
          </template>
        </Column>

        <!-- Column 2: Unit Code -->
        <Column
          field="unitCode"
          header="Unit Code"
          sortable
          style="width: 140px"
        >
          <template #body="{ data }">
            <span class="font-mono font-semibold text-slate-800 dark:text-slate-200">
              {{ data.unitCode }}
            </span>
          </template>
        </Column>

        <!-- Column 3: Unit Title -->
        <Column
          field="title"
          header="Unit Title"
          sortable
          style="min-width: 200px"
        >
          <template #body="{ data }">
            <span class="font-medium text-slate-900 dark:text-slate-100">
              {{ data.title }}
            </span>
          </template>
        </Column>

        <!-- Column 4: Requirement (Required vs Elective Tag) -->
        <Column
          field="isRequired"
          header="Requirement"
          sortable
          style="width: 130px"
        >
          <template #body="{ data }">
            <Tag
              :value="data.isRequired ? 'Required' : 'Elective'"
              :severity="data.isRequired ? 'info' : 'secondary'"
              class="text-[10px] font-bold tracking-tight px-2 py-0.5"
              :data-testid="`unit-requirement-${data.id}`"
            />
          </template>
        </Column>

        <!-- Column 5: Adaptation Notes -->
        <Column
          field="notes"
          header="Adaptation Notes"
          style="min-width: 220px"
        >
          <template #body="{ data }">
            <p
              v-if="data.notes"
              class="text-xs text-slate-600 dark:text-slate-300 italic line-clamp-2"
              :title="data.notes"
            >
              {{ data.notes }}
            </p>
            <span v-else class="text-xs text-slate-400 italic">None</span>
          </template>
        </Column>

        <!-- Column 6: Actions (Remove Unit, visible only when plan is draft) -->
        <Column
          v-if="isDraft"
          header="Actions"
          style="width: 80px"
          class="text-right"
        >
          <template #body="{ data }">
            <div class="flex items-center justify-end">
              <Button
                icon="pi pi-trash"
                severity="danger"
                variant="text"
                rounded
                size="small"
                :loading="removingUnitId === data.id"
                title="Remove unit from plan"
                :data-testid="`remove-unit-btn-${data.id}`"
                @click="handleRemoveUnit(data)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Add Learning Unit Dialog Modal -->
    <AddSlpUnitModal
      v-model:visible="isAddModalOpen"
      :plan-id="effectivePlanId"
      :units="availableUnits"
      @added="handleUnitAdded"
    />
  </div>
</template>
