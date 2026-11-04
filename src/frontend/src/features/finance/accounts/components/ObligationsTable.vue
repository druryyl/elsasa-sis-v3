<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  Receipt,
  FileText,
  Search,
  PlusCircle,
  Calendar,
  AlertCircle,
  CheckCircle2,
} from 'lucide-vue-next';
import { StatusPill, EmptyState } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { getObligationStatusDescriptor } from '../../shared/financeStatus.utils';
import type { OpenObligationDto } from '../types/accounts.types';

interface Props {
  obligations: OpenObligationDto[];
  loading?: boolean;
  canGenerateBill?: boolean;
  canAssessFee?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  obligations: () => [],
  loading: false,
  canGenerateBill: true,
  canAssessFee: true,
});

const emit = defineEmits<{
  (e: 'generateBill', selectedIds: string[]): void;
  (e: 'assessFee'): void;
}>();

const selectedObligations = ref<OpenObligationDto[]>([]);
const searchFilter = ref('');

const filteredObligations = computed(() => {
  if (!searchFilter.value.trim()) {
    return props.obligations;
  }
  const term = searchFilter.value.trim().toLowerCase();
  return props.obligations.filter((obl) => {
    return (
      obl.obligationNumber.toLowerCase().includes(term) ||
      obl.description.toLowerCase().includes(term) ||
      obl.chargeType.toLowerCase().includes(term) ||
      obl.status.toLowerCase().includes(term)
    );
  });
});

const openCount = computed(() => {
  return props.obligations.filter(
    (o) => o.status === 'assessed' || o.status === 'issued' || o.status === 'partially_settled',
  ).length;
});

function handleCompileSelected() {
  const ids = selectedObligations.value.map((o) => o.id);
  emit('generateBill', ids);
}

function handleCompileAllOpen() {
  const openIds = props.obligations
    .filter((o) => o.status === 'assessed' || o.status === 'issued')
    .map((o) => o.id);
  emit('generateBill', openIds);
}
</script>

<template>
  <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden" data-testid="obligations-table-container">
    <!-- Header & Controls -->
    <div class="px-6 py-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-2">
        <Receipt class="w-5 h-5 text-indigo-600" />
        <h3 class="text-base font-semibold text-slate-800">
          Financial Obligations
        </h3>
        <span
          class="ml-1 text-xs font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600"
          data-testid="obligations-count-badge"
        >
          {{ obligations.length }} total
        </span>
      </div>

      <div class="flex items-center gap-2.5 flex-wrap">
        <!-- Quick search -->
        <div class="relative w-48 sm:w-60">
          <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
          <InputText
            v-model="searchFilter"
            placeholder="Search obligations..."
            class="w-full pl-9! text-xs"
            size="small"
            data-testid="obligations-search-input"
          />
        </div>

        <Button
          v-if="canAssessFee"
          size="small"
          severity="secondary"
          outlined
          data-testid="table-assess-fee-btn"
          @click="emit('assessFee')"
        >
          <template #icon>
            <PlusCircle class="w-3.5 h-3.5 mr-1 text-indigo-600" />
          </template>
          Assess Fee
        </Button>

        <Button
          v-if="canGenerateBill"
          size="small"
          severity="primary"
          :disabled="obligations.length === 0"
          data-testid="table-generate-bill-btn"
          @click="selectedObligations.length > 0 ? handleCompileSelected() : handleCompileAllOpen()"
        >
          <template #icon>
            <FileText class="w-3.5 h-3.5 mr-1" />
          </template>
          <span v-if="selectedObligations.length > 0">
            Compile Selected ({{ selectedObligations.length }})
          </span>
          <span v-else>
            Compile All Open
          </span>
        </Button>
      </div>
    </div>

    <!-- DataTable -->
    <DataTable
      v-model:selection="selectedObligations"
      :value="filteredObligations"
      :loading="loading"
      data-key="id"
      responsive-layout="scroll"
      striped-rows
      class="text-sm"
      data-testid="obligations-datatable"
    >
      <template #empty>
        <div class="p-8">
          <EmptyState
            title="No Obligations Found"
            message="No financial obligations have been assessed for this student account."
            data-testid="obligations-empty-state"
          />
        </div>
      </template>

      <!-- Selection column -->
      <Column selection-mode="multiple" header-style="width: 3rem" />

      <!-- Obligation Number -->
      <Column field="obligationNumber" header="Obligation #" style="min-width: 140px">
        <template #body="{ data }">
          <span class="font-mono font-medium text-slate-900 text-xs">
            {{ data.obligationNumber }}
          </span>
        </template>
      </Column>

      <!-- Description -->
      <Column field="description" header="Description" style="min-width: 200px">
        <template #body="{ data }">
          <div class="font-medium text-slate-900">
            {{ data.description }}
          </div>
          <div v-if="data.academicPeriodLabel" class="text-xs text-slate-500">
            {{ data.academicPeriodLabel }}
          </div>
        </template>
      </Column>

      <!-- Charge Type -->
      <Column field="chargeType" header="Charge Type" style="min-width: 130px">
        <template #body="{ data }">
          <span
            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 capitalize"
          >
            {{ data.chargeType.replace(/_/g, ' ') }}
          </span>
        </template>
      </Column>

      <!-- Original Amount in IDR -->
      <Column field="originalAmount" header="Amount" style="min-width: 130px" class="text-right">
        <template #body="{ data }">
          <span class="font-mono font-semibold text-slate-900">
            {{ formatIdr(data.originalAmount) }}
          </span>
        </template>
      </Column>

      <!-- Due Date -->
      <Column field="dueDate" header="Due Date" style="min-width: 110px">
        <template #body="{ data }">
          <span class="text-xs text-slate-700 font-medium flex items-center gap-1">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            {{ data.dueDate }}
          </span>
        </template>
      </Column>

      <!-- Status -->
      <Column field="status" header="Status" style="min-width: 110px">
        <template #body="{ data }">
          <StatusPill
            :status="data.status"
            :label="getObligationStatusDescriptor(data.status).label"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>
