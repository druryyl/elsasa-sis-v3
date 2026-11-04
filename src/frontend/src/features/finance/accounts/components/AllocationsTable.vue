<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  Layers,
  Search,
  Calendar,
  AlertCircle,
  RefreshCw,
  Clock,
  History,
  FileText,
} from 'lucide-vue-next';
import { StatusPill, EmptyState } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import { getAllocationStatusDescriptor } from '../../shared/financeStatus.utils';
import type { PaymentAllocationDto } from '../../allocations/types/allocations.types';
import type { PaymentAllocationSummaryDto } from '../../payments/types/payments.types';

type AllocationItem = PaymentAllocationDto | PaymentAllocationSummaryDto;

interface Props {
  allocations: AllocationItem[];
  loading?: boolean;
  canReallocate?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  allocations: () => [],
  loading: false,
  canReallocate: true,
});

const emit = defineEmits<{
  (e: 'reallocate', allocation: AllocationItem): void;
}>();

const searchFilter = ref('');
const statusFilter = ref<'all' | 'active' | 'superseded' | 'reversed'>('all');

const filteredAllocations = computed(() => {
  return props.allocations.filter((item) => {
    // Status filter
    if (statusFilter.value !== 'all') {
      if (item.status?.toLowerCase() !== statusFilter.value) {
        return false;
      }
    }

    // Search filter
    if (!searchFilter.value.trim()) {
      return true;
    }
    const term = searchFilter.value.trim().toLowerCase();
    return (
      (item.allocationNumber && item.allocationNumber.toLowerCase().includes(term)) ||
      (item.obligationNumber && item.obligationNumber.toLowerCase().includes(term)) ||
      (item.description && item.description.toLowerCase().includes(term)) ||
      (item.reversalReason && item.reversalReason.toLowerCase().includes(term))
    );
  });
});

function handleReallocate(allocation: AllocationItem) {
  emit('reallocate', allocation);
}
</script>

<template>
  <div
    class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden"
    data-testid="allocations-table-container"
  >
    <!-- Header & Controls -->
    <div class="px-6 py-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-2">
        <Layers class="w-5 h-5 text-indigo-600" />
        <h3 class="text-base font-semibold text-slate-800">
          Payment Allocations &amp; Reallocation Lineage
        </h3>
        <span
          class="ml-1 text-xs font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600"
          data-testid="allocations-count-badge"
        >
          {{ allocations.length }} applied
        </span>
      </div>

      <div class="flex items-center gap-3 flex-wrap">
        <!-- Quick search -->
        <div class="relative w-48 sm:w-60">
          <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
          <InputText
            v-model="searchFilter"
            placeholder="Search allocations..."
            class="w-full pl-9! text-xs"
            size="small"
            data-testid="allocations-search-input"
          />
        </div>

        <!-- Status Filter Pills -->
        <div class="inline-flex rounded-lg border border-slate-200 p-0.5 bg-slate-50 text-xs">
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'all' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'all'"
          >
            All
          </button>
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'active' ? 'bg-white text-emerald-800 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'active'"
          >
            Active
          </button>
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'superseded' ? 'bg-white text-slate-800 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'superseded'"
          >
            Superseded
          </button>
          <button
            type="button"
            class="px-2.5 py-1 rounded-md font-medium transition-colors"
            :class="statusFilter === 'reversed' ? 'bg-white text-rose-800 shadow-xs font-semibold' : 'text-slate-600 hover:text-slate-900'"
            @click="statusFilter = 'reversed'"
          >
            Reversed
          </button>
        </div>
      </div>
    </div>

    <!-- DataTable -->
    <DataTable
      :value="filteredAllocations"
      :loading="loading"
      data-key="allocationNumber"
      responsive-layout="scroll"
      striped-rows
      class="text-sm"
      data-testid="allocations-datatable"
    >
      <template #empty>
        <div class="p-8">
          <EmptyState
            title="No Payment Allocations"
            message="No payment funds have been allocated to obligations for this student account."
            data-testid="allocations-empty-state"
          />
        </div>
      </template>

      <!-- 1. Allocation Number -->
      <Column header="Allocation #" style="min-width: 160px">
        <template #body="{ data }">
          <div
            class="font-mono text-xs font-bold text-slate-800 bg-slate-50 px-2 py-0.5 rounded border border-slate-200 inline-block"
            data-testid="allocation-number-cell"
          >
            {{ data.allocationNumber }}
          </div>
          <div class="text-[11px] text-slate-500 font-mono mt-0.5">
            {{ formatDateOnly(data.allocationDate) }}
          </div>
        </template>
      </Column>

      <!-- 2. Obligation Description -->
      <Column header="Obligation Description" style="min-width: 200px">
        <template #body="{ data }">
          <div class="font-semibold text-slate-900">
            {{ data.description || 'Assessed Obligation' }}
          </div>
          <div class="text-xs text-slate-500 font-mono">
            {{ data.obligationNumber }}
            <span v-if="data.chargeType" class="text-[10px] uppercase font-sans font-medium text-slate-400 ml-1">
              • {{ data.chargeType }}
            </span>
          </div>
        </template>
      </Column>

      <!-- 3. Due Date -->
      <Column header="Due Date" style="min-width: 120px">
        <template #body="{ data }">
          <div v-if="(data as any).dueDate" class="font-medium text-slate-700 flex items-center gap-1">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            {{ formatDateOnly((data as any).dueDate) }}
          </div>
          <span v-else class="text-slate-400 text-xs">—</span>
        </template>
      </Column>

      <!-- 4. Allocated Amount -->
      <Column header="Allocated Amount" style="min-width: 140px" class="text-right">
        <template #body="{ data }">
          <div
            class="font-mono font-bold text-sm"
            :class="data.status === 'active' ? 'text-emerald-700' : 'text-slate-500 line-through'"
          >
            {{ formatIdr(data.allocatedAmount) }}
          </div>
        </template>
      </Column>

      <!-- 5. Status -->
      <Column header="Status" style="min-width: 110px">
        <template #body="{ data }">
          <StatusPill
            :status="data.status"
            :label="getAllocationStatusDescriptor(data.status).label"
          />
        </template>
      </Column>

      <!-- 6. Reversal Reason -->
      <Column header="Reversal Reason" style="min-width: 180px">
        <template #body="{ data }">
          <div v-if="data.reversalReason" class="text-xs text-slate-600 italic flex items-start gap-1">
            <AlertCircle class="w-3.5 h-3.5 text-amber-500 shrink-0 mt-0.5" />
            <span class="line-clamp-2" :title="data.reversalReason">{{ data.reversalReason }}</span>
          </div>
          <span v-else class="text-slate-400 text-xs">—</span>
        </template>
      </Column>

      <!-- 7. Actions -->
      <Column header="Action" style="width: 120px" class="text-center">
        <template #body="{ data }">
          <Button
            size="small"
            severity="secondary"
            outlined
            data-testid="reallocate-action-btn"
            :disabled="!canReallocate || data.status?.toLowerCase() !== 'active'"
            @click="handleReallocate(data)"
          >
            <template #icon>
              <RefreshCw class="w-3.5 h-3.5 mr-1 text-amber-600" />
            </template>
            Reallocate
          </Button>
        </template>
      </Column>
    </DataTable>
  </div>
</template>
