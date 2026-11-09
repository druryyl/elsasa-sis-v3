<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import ConfirmDialog from 'primevue/confirmdialog';
import { useConfirm } from 'primevue/useconfirm';
import { format, parseISO, isValid } from 'date-fns';
import {
  Plus,
  Award,
  Trash2,
  Clock,
  ShieldCheck,
  Calendar,
  AlertCircle,
  FileText,
} from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
import { useRemoveAdjustmentMutation } from '../composables/useLearningPlans';
import type {
  StudentLearningPlan,
  AcademicPlanAdjustment,
  StudentLearningPlanStatus,
  AdjustmentType,
} from '../types';
import AddAdjustmentModal from './AddAdjustmentModal.vue';

interface Props {
  plan?: StudentLearningPlan | null;
  planId?: string;
  planStatus?: StudentLearningPlanStatus;
  adjustments?: AcademicPlanAdjustment[];
  isLoading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  plan: null,
  planId: undefined,
  planStatus: undefined,
  adjustments: undefined,
  isLoading: false,
});

const emit = defineEmits<{
  (e: 'adjustmentAdded', adjustmentId: string): void;
  (e: 'adjustmentRemoved', adjustmentId: string): void;
}>();

// ---------------------------------------------------------------------------
// 1. Reactive Plan Resolution
// ---------------------------------------------------------------------------
const effectivePlanId = computed<string>(() => props.plan?.id ?? props.planId ?? '');
const effectivePlanStatus = computed<StudentLearningPlanStatus>(
  () => props.plan?.status ?? props.planStatus ?? 'draft',
);
const effectiveAdjustments = computed<AcademicPlanAdjustment[]>(
  () => props.plan?.adjustments ?? props.adjustments ?? [],
);
const isDraft = computed(() => effectivePlanStatus.value === 'draft');

// ---------------------------------------------------------------------------
// 2. Modal and Confirmation State
// ---------------------------------------------------------------------------
const isAddModalOpen = ref(false);
const removingAdjustmentId = ref<string | null>(null);

function tryGetConfirm() {
  try {
    return useConfirm();
  } catch {
    return null;
  }
}
const confirm = tryGetConfirm();

const removeAdjustmentMutation = useRemoveAdjustmentMutation();

// ---------------------------------------------------------------------------
// 3. Date Formatting via date-fns
// ---------------------------------------------------------------------------
function formatIsoDate(dateString: string | null | undefined): string {
  if (!dateString) return '';
  if (dateString.startsWith('1900-01-01')) return '';
  try {
    const parsed = parseISO(dateString);
    if (!isValid(parsed)) return dateString;
    return format(parsed, 'dd MMM yyyy');
  } catch {
    return dateString;
  }
}

function formatDateRange(effectiveDate: string, endDate?: string | null): string {
  const start = formatIsoDate(effectiveDate);
  const end = formatIsoDate(endDate);
  if (start && end) {
    return `${start} – ${end}`;
  }
  if (start) {
    return `${start} – Ongoing`;
  }
  return '—';
}

function isAdjustmentApproved(adj: AcademicPlanAdjustment): boolean {
  return Boolean(
    adj.approvedBy &&
      adj.approvedBy.trim() !== '' &&
      adj.approvedDate &&
      !adj.approvedDate.startsWith('1900-01-01'),
  );
}

// ---------------------------------------------------------------------------
// 4. Removal Handling
// ---------------------------------------------------------------------------
async function executeRemove(adjustmentId: string) {
  if (!effectivePlanId.value) return;
  removingAdjustmentId.value = adjustmentId;

  try {
    await removeAdjustmentMutation.mutateAsync({
      planId: effectivePlanId.value,
      adjustmentId,
    });
    emit('adjustmentRemoved', adjustmentId);
  } finally {
    removingAdjustmentId.value = null;
  }
}

function handleRemoveAdjustment(adj: AcademicPlanAdjustment) {
  if (!isDraft.value) return;

  if (confirm) {
    confirm.require({
      message: `Are you sure you want to remove the ${adj.adjustmentType} adjustment "${adj.description}"? This action cannot be undone.`,
      header: 'Remove Academic Plan Adjustment',
      icon: 'pi pi-exclamation-triangle',
      rejectProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true,
      },
      acceptProps: {
        label: 'Remove Adjustment',
        severity: 'danger',
      },
      accept: () => executeRemove(adj.id),
    });
  } else {
    executeRemove(adj.id);
  }
}

function handleAdjustmentAdded(adjustmentId: string) {
  emit('adjustmentAdded', adjustmentId);
}
</script>

<template>
  <div class="space-y-4" data-testid="slp-adjustments-tab">
    <!-- Optional in-component confirmation dialog host -->
    <ConfirmDialog />

    <!-- Tab Header & Quick Action Toolbar -->
    <div class="flex items-center justify-between gap-3 flex-wrap">
      <div>
        <h3 class="text-xs font-bold text-slate-800 dark:text-slate-200 uppercase tracking-wider flex items-center gap-2">
          <Award class="w-4 h-4 text-purple-600 dark:text-purple-400" />
          <span>Academic Plan Adjustments</span>
          <span class="px-2 py-0.5 rounded-full text-[11px] bg-purple-100 dark:bg-purple-900/50 text-purple-700 dark:text-purple-300 font-bold">
            {{ effectiveAdjustments.length }}
          </span>
        </h3>
        <p class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
          Formal accommodations, course substitutions, and requirement exemptions approved by Academic Authority.
        </p>
      </div>

      <!-- "+ Add Adjustment" button (visible only when plan is draft) -->
      <Button
        v-if="isDraft"
        label="Add Adjustment"
        icon="pi pi-plus"
        size="small"
        class="text-xs font-semibold shadow-xs"
        data-testid="open-add-adjustment-btn"
        @click="isAddModalOpen = true"
      />
    </div>

    <!-- Empty State when no adjustments exist -->
    <div
      v-if="effectiveAdjustments.length === 0"
      class="p-8 text-center bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800"
      data-testid="adjustments-empty-state"
    >
      <EmptyState
        title="No academic adjustments recorded"
        description="No accommodations, course substitutions, or exemptions have been established for this student's plan."
      />
      <div v-if="isDraft" class="mt-4 flex justify-center">
        <Button
          label="Add First Adjustment"
          icon="pi pi-plus"
          size="small"
          severity="primary"
          class="text-xs font-semibold"
          data-testid="empty-add-adjustment-btn"
          @click="isAddModalOpen = true"
        />
      </div>
    </div>

    <!-- Adjustments DataTable -->
    <div
      v-else
      class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-2xs overflow-hidden"
    >
      <DataTable
        :value="effectiveAdjustments"
        data-key="id"
        striped-rows
        responsive-layout="scroll"
        class="text-xs"
        data-testid="adjustments-datatable"
      >
        <!-- 1. Type Badge Column: Accommodation (blue), Substitution (purple), Exemption (orange) -->
        <Column header="Type" field="adjustmentType" style="width: 140px; min-width: 130px">
          <template #body="{ data }">
            <!-- Accommodation: Blue -->
            <Tag
              v-if="data.adjustmentType === 'accommodation'"
              value="Accommodation"
              severity="info"
              class="font-bold text-[10px] tracking-wide uppercase !bg-blue-50 !text-blue-700 !border-blue-200 dark:!bg-blue-950/40 dark:!text-blue-300 dark:!border-blue-800 border"
              data-testid="badge-accommodation"
            />
            <!-- Substitution: Purple -->
            <Tag
              v-else-if="data.adjustmentType === 'substitution'"
              value="Substitution"
              class="font-bold text-[10px] tracking-wide uppercase !bg-purple-50 !text-purple-700 !border-purple-200 dark:!bg-purple-950/40 dark:!text-purple-300 dark:!border-purple-800 border"
              data-testid="badge-substitution"
            />
            <!-- Exemption: Orange -->
            <Tag
              v-else-if="data.adjustmentType === 'exemption'"
              value="Exemption"
              severity="warn"
              class="font-bold text-[10px] tracking-wide uppercase !bg-amber-50 !text-amber-800 !border-amber-200 dark:!bg-amber-950/40 dark:!text-amber-300 dark:!border-amber-800 border"
              data-testid="badge-exemption"
            />
            <Tag
              v-else
              :value="data.adjustmentType"
              severity="secondary"
              class="font-bold text-[10px] tracking-wide uppercase"
            />
          </template>
        </Column>

        <!-- 2. Description Column -->
        <Column header="Description" field="description" style="min-width: 220px">
          <template #body="{ data }">
            <div class="space-y-1">
              <span class="font-semibold text-xs text-slate-900 dark:text-slate-100 block">
                {{ data.description }}
              </span>
              <div v-if="data.notes" class="text-[11px] text-slate-500 dark:text-slate-400 italic">
                Notes: {{ data.notes }}
              </div>
            </div>
          </template>
        </Column>

        <!-- 3. Justification Rationale Column -->
        <Column header="Justification Rationale" field="rationale" style="min-width: 220px">
          <template #body="{ data }">
            <div class="text-xs text-slate-700 dark:text-slate-300 pl-2.5 border-l-2 border-slate-200 dark:border-slate-700 py-0.5 leading-relaxed">
              {{ data.rationale }}
            </div>
          </template>
        </Column>

        <!-- 4. Effective Date & End Date Column (formatted via date-fns) -->
        <Column header="Effective Dates" style="width: 190px; min-width: 170px">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="flex items-center gap-1.5 font-medium text-xs text-slate-800 dark:text-slate-200">
                <Calendar class="w-3.5 h-3.5 text-slate-400" />
                <span>{{ formatDateRange(data.effectiveDate, data.endDate) }}</span>
              </div>
              <div class="text-[10px] text-slate-400 dark:text-slate-500 pl-5">
                Effective: {{ formatIsoDate(data.effectiveDate) }}
                <span v-if="data.endDate"> | End: {{ formatIsoDate(data.endDate) }}</span>
              </div>
            </div>
          </template>
        </Column>

        <!-- 5. Approving Authority & Approval Date Stamp Column -->
        <Column header="Authority Approval" style="width: 200px; min-width: 180px">
          <template #body="{ data }">
            <div v-if="isAdjustmentApproved(data)" class="space-y-0.5">
              <div class="flex items-center gap-1.5 font-semibold text-xs text-slate-900 dark:text-slate-100">
                <ShieldCheck class="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400" />
                <span>{{ data.approvedBy }}</span>
              </div>
              <div class="text-[11px] text-slate-400 dark:text-slate-500 pl-5">
                Approved {{ formatIsoDate(data.approvedDate) }}
              </div>
            </div>
            <div v-else class="flex items-center gap-1.5 text-xs text-amber-600 dark:text-amber-400 italic">
              <Clock class="w-3.5 h-3.5" />
              <span>Pending Plan Approval</span>
            </div>
          </template>
        </Column>

        <!-- 6. Actions Column (Remove adjustment, visible only when plan is draft) -->
        <Column
          v-if="isDraft"
          header="Actions"
          align-frozen="right"
          style="width: 70px"
          header-class="text-right"
          body-class="text-right"
        >
          <template #body="{ data }">
            <Button
              icon="pi pi-trash"
              severity="danger"
              text
              rounded
              size="small"
              class="w-7 h-7 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/30"
              title="Remove Adjustment"
              aria-label="Remove Adjustment"
              :data-testid="`remove-adjustment-${data.id}`"
              :loading="removingAdjustmentId === data.id"
              @click="handleRemoveAdjustment(data)"
            />
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Add Adjustment Modal -->
    <AddAdjustmentModal
      v-model:visible="isAddModalOpen"
      :plan-id="effectivePlanId"
      @added="handleAdjustmentAdded"
    />
  </div>
</template>
