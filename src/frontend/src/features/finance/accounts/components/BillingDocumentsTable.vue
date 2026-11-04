<script setup lang="ts">
import { ref, computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  FileText,
  Eye,
  Search,
  Calendar,
  User,
  Send,
} from 'lucide-vue-next';
import { StatusPill, EmptyState } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { getBillingStatusDescriptor } from '../../shared/financeStatus.utils';
import type { RecentBillingDocumentDto } from '../types/accounts.types';

interface Props {
  documents: RecentBillingDocumentDto[];
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  documents: () => [],
  loading: false,
});

const emit = defineEmits<{
  (e: 'viewInvoice', documentId: string): void;
}>();

const searchFilter = ref('');

const filteredDocuments = computed(() => {
  if (!searchFilter.value.trim()) {
    return props.documents;
  }
  const term = searchFilter.value.trim().toLowerCase();
  return props.documents.filter((doc) => {
    return (
      doc.documentNumber.toLowerCase().includes(term) ||
      doc.recipientName.toLowerCase().includes(term) ||
      doc.documentType.toLowerCase().includes(term) ||
      doc.deliveryStatus.toLowerCase().includes(term)
    );
  });
});
</script>

<template>
  <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden" data-testid="billing-documents-table-container">
    <!-- Header & Controls -->
    <div class="px-6 py-4 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4">
      <div class="flex items-center gap-2">
        <FileText class="w-5 h-5 text-emerald-600" />
        <h3 class="text-base font-semibold text-slate-800">
          Billing Documents &amp; Statements
        </h3>
        <span
          class="ml-1 text-xs font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600"
          data-testid="billing-documents-count-badge"
        >
          {{ documents.length }} issued
        </span>
      </div>

      <!-- Quick search -->
      <div class="relative w-48 sm:w-60">
        <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <InputText
          v-model="searchFilter"
          placeholder="Search documents..."
          class="w-full pl-9! text-xs"
          size="small"
          data-testid="billing-search-input"
        />
      </div>
    </div>

    <!-- DataTable -->
    <DataTable
      :value="filteredDocuments"
      :loading="loading"
      data-key="id"
      responsive-layout="scroll"
      striped-rows
      class="text-sm"
      data-testid="billing-documents-datatable"
    >
      <template #empty>
        <div class="p-8">
          <EmptyState
            title="No Billing Documents"
            message="No bills or periodic statements have been compiled for this student account."
            data-testid="billing-documents-empty-state"
          />
        </div>
      </template>

      <!-- Document Number -->
      <Column field="documentNumber" header="Document #" style="min-width: 140px">
        <template #body="{ data }">
          <span class="font-mono font-medium text-slate-900 text-xs">
            {{ data.documentNumber }}
          </span>
        </template>
      </Column>

      <!-- Type -->
      <Column field="documentType" header="Type" style="min-width: 100px">
        <template #body="{ data }">
          <span
            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium uppercase"
            :class="
              data.documentType === 'statement'
                ? 'bg-purple-50 text-purple-700 border border-purple-200'
                : 'bg-blue-50 text-blue-700 border border-blue-200'
            "
          >
            {{ data.documentType }}
          </span>
        </template>
      </Column>

      <!-- Recipient -->
      <Column field="recipientName" header="Recipient" style="min-width: 180px">
        <template #body="{ data }">
          <div class="font-medium text-slate-900 flex items-center gap-1.5">
            <User class="w-3.5 h-3.5 text-slate-400" />
            {{ data.recipientName }}
          </div>
          <div v-if="data.recipientRelationship" class="text-xs text-slate-500 capitalize">
            {{ data.recipientRelationship }} • {{ data.recipientContact || 'No phone' }}
          </div>
        </template>
      </Column>

      <!-- Issue Date -->
      <Column field="issueDate" header="Issue Date" style="min-width: 110px">
        <template #body="{ data }">
          <span class="text-xs text-slate-700 flex items-center gap-1">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            {{ data.issueDate }}
          </span>
        </template>
      </Column>

      <!-- Payment Due Date -->
      <Column field="paymentDueDate" header="Due Date" style="min-width: 110px">
        <template #body="{ data }">
          <span v-if="data.paymentDueDate" class="text-xs text-slate-700 font-medium">
            {{ data.paymentDueDate }}
          </span>
          <span v-else class="text-xs text-slate-400 italic">
            N/A
          </span>
        </template>
      </Column>

      <!-- Total Billed Amount -->
      <Column field="totalBilledAmount" header="Total Billed" style="min-width: 130px" class="text-right">
        <template #body="{ data }">
          <span class="font-mono font-semibold text-slate-900">
            {{ formatIdr(data.totalBilledAmount) }}
          </span>
        </template>
      </Column>

      <!-- Delivery Status -->
      <Column field="deliveryStatus" header="Status" style="min-width: 100px">
        <template #body="{ data }">
          <StatusPill
            :status="data.deliveryStatus"
            :label="getBillingStatusDescriptor(data.deliveryStatus).label"
          />
        </template>
      </Column>

      <!-- Actions -->
      <Column header="Action" style="width: 80px" class="text-center">
        <template #body="{ data }">
          <Button
            size="small"
            severity="secondary"
            text
            rounded
            title="View Invoice Details"
            data-testid="view-invoice-action-btn"
            @click="emit('viewInvoice', data.id)"
          >
            <template #icon>
              <Eye class="w-4 h-4 text-slate-600" />
            </template>
          </Button>
        </template>
      </Column>
    </DataTable>
  </div>
</template>
