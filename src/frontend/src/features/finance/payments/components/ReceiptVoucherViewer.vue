<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import {
  Printer,
  FileCheck2,
  Building,
  School,
  CheckCircle,
  Download,
  AlertCircle,
} from 'lucide-vue-next';
import { LoadingSpinner } from '@/shared/components';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { usePaymentReceiptQuery } from '../composables/useFinancePayments';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    paymentId?: string | null;
  }>(),
  {
    paymentId: null,
  },
);

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'close'): void;
}>();

const receiptQuery = usePaymentReceiptQuery(
  computed(() => props.paymentId),
  { enabled: computed(() => props.visible && !!props.paymentId) },
);

const receipt = computed(() => receiptQuery.data.value ?? null);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handlePrint() {
  window.print();
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Official Receipt Voucher"
    :style="{ width: '52rem' }"
    data-testid="receipt-voucher-modal"
    @update:visible="handleClose"
  >
    <div v-if="receiptQuery.isPending.value" class="py-16 flex justify-center items-center">
      <LoadingSpinner />
    </div>

    <div
      v-else-if="receiptQuery.isError.value || !receipt"
      class="py-12 text-center text-rose-600 space-y-2"
    >
      <AlertCircle class="w-8 h-8 mx-auto" />
      <p class="font-medium text-sm">Failed to load official receipt voucher.</p>
      <p class="text-xs text-slate-500">
        {{ (receiptQuery.error.value as Error)?.message || 'Payment may not have confirmed status or sequential receipt issued.' }}
      </p>
    </div>

    <!-- Printable Receipt Paper -->
    <div
      v-else
      class="p-6 bg-white border border-slate-300 rounded-lg shadow-xs text-slate-800 space-y-6 printable-receipt"
      data-testid="printable-receipt-content"
    >
      <!-- School Letterhead Header -->
      <div class="border-b-2 border-slate-900 pb-4 flex items-start justify-between">
        <div class="flex items-center gap-3.5">
          <div class="w-12 h-12 rounded-full bg-slate-900 text-white flex items-center justify-center shrink-0">
            <School class="w-7 h-7" />
          </div>
          <div>
            <h2 class="text-lg font-bold tracking-tight text-slate-900 uppercase">
              {{ receipt.header?.schoolName || 'ELSASA SCHOOL OF EXCELLENCE' }}
            </h2>
            <p class="text-xs text-slate-600">
              {{ receipt.header?.schoolAddress || 'Jl. Pendidikan Utama No. 88, Jakarta Selatan' }}
            </p>
            <p class="text-[11px] text-slate-500">
              {{ receipt.header?.contactInfo || 'Tel: (021) 789-0123 • info@elsasa.sch.id • www.elsasa.sch.id' }}
            </p>
          </div>
        </div>

        <div class="text-right">
          <span class="inline-block px-2.5 py-1 text-xs font-bold uppercase rounded tracking-wider bg-emerald-100 text-emerald-800 border border-emerald-300">
            OFFICIAL RECEIPT
          </span>
          <p class="font-mono text-base font-extrabold text-slate-900 mt-1.5" data-testid="receipt-number">
            {{ receipt.receiptNumber }}
          </p>
          <span class="text-[11px] text-slate-500">
            Ref: {{ receipt.paymentNumber }}
          </span>
        </div>
      </div>

      <!-- Payer & Beneficiary Information Grid -->
      <div class="grid grid-cols-2 gap-6 text-xs border border-slate-200 rounded-md p-4 bg-slate-50/60">
        <!-- Beneficiary Candidate -->
        <div class="space-y-1">
          <span class="font-semibold uppercase tracking-wider text-slate-500 text-[10px]">
            Student / Candidate
          </span>
          <p class="font-bold text-sm text-slate-900">
            {{ receipt.candidate?.fullName || receipt.candidate?.studentFullName || 'Pre-Enrollment Candidate' }}
          </p>
          <p class="text-slate-600">
            {{ (receipt.candidate?.identifier || receipt.candidate?.studentNumber) ? `ID: ${receipt.candidate?.identifier || receipt.candidate?.studentNumber}` : `Account: ${receipt.candidate?.accountNumber}` }}
          </p>
          <p class="text-slate-500 text-[11px]">
            Account No: {{ receipt.candidate?.accountNumber }}
          </p>
        </div>

        <!-- Remitting Payer -->
        <div class="space-y-1">
          <span class="font-semibold uppercase tracking-wider text-slate-500 text-[10px]">
            Remitted By / Payer
          </span>
          <p class="font-bold text-sm text-slate-900">
            {{ receipt.payer?.payerName }}
          </p>
          <p class="text-slate-600">
            Payer ID: {{ receipt.payer?.payerNumber }}
            <span v-if="receipt.payer?.identificationNumber"> • NIK: {{ receipt.payer.identificationNumber }}</span>
          </p>
          <p class="text-slate-500 text-[11px]">
            {{ receipt.payer?.phoneNumber || receipt.payer?.email || 'Counter Remittance' }}
          </p>
        </div>

        <!-- Payment Meta Details -->
        <div class="border-t border-slate-200 pt-2 space-y-0.5">
          <span class="text-slate-500 font-medium">Payment Method:</span>
          <p class="font-semibold capitalize text-slate-800">
            {{ receipt.paymentMethod?.replace(/_/g, ' ') }}
          </p>
          <p v-if="receipt.externalReference" class="text-slate-600 font-mono text-[11px]">
            Ref Slip: {{ receipt.externalReference }}
          </p>
        </div>

        <div class="border-t border-slate-200 pt-2 space-y-0.5">
          <span class="text-slate-500 font-medium">Payment Date:</span>
          <p class="font-semibold text-slate-800">
            {{ formatDisplayDate(receipt.paymentDate) }}
          </p>
          <p class="text-slate-500 text-[11px]">
            Issued: {{ formatDisplayDateTime(receipt.receiptIssuedAt) }}
          </p>
        </div>
      </div>

      <!-- Line Allocations Table -->
      <div class="border border-slate-200 rounded-md overflow-hidden text-xs">
        <table class="w-full border-collapse">
          <thead>
            <tr class="bg-slate-100 text-slate-700 text-left border-b border-slate-200">
              <th class="py-2 px-3 font-semibold">Allocation #</th>
              <th class="py-2 px-3 font-semibold">Charge Type / Item Description</th>
              <th class="py-2 px-3 font-semibold">Obligation #</th>
              <th class="py-2 px-3 font-semibold text-right">Amount (IDR)</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr
              v-for="line in receipt.allocations"
              :key="line.allocationNumber"
              class="hover:bg-slate-50"
            >
              <td class="py-2.5 px-3 font-mono text-slate-700">{{ line.allocationNumber }}</td>
              <td class="py-2.5 px-3">
                <p class="font-medium text-slate-900">{{ line.description }}</p>
                <span class="text-[10px] text-slate-500 uppercase">{{ line.chargeType?.replace(/_/g, ' ') }}</span>
              </td>
              <td class="py-2.5 px-3 font-mono text-slate-600">{{ line.obligationNumber }}</td>
              <td class="py-2.5 px-3 font-mono font-semibold text-slate-900 text-right">
                {{ formatIDR(line.amount) }}
              </td>
            </tr>
            <tr v-if="!receipt.allocations || receipt.allocations.length === 0">
              <td colspan="4" class="py-4 px-3 text-center text-slate-400 italic">
                Receipt funds unallocated (Held as available student credit balance).
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Financial Totals & Terbilang In Words -->
      <div class="border border-slate-200 rounded-md p-4 bg-slate-50/70 space-y-2">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
            Total Paid (Jumlah Pembayaran)
          </span>
          <span
            class="font-mono text-lg font-extrabold text-slate-900"
            data-testid="receipt-amount"
            data-test="receipt-total-amount"
          >
            {{ formatIDR(receipt.amount) }}
          </span>
        </div>
        <div class="border-t border-slate-200 pt-2">
          <span class="text-[10px] font-semibold uppercase text-slate-500">Terbilang (In Words):</span>
          <p class="text-xs font-semibold italic text-slate-800 capitalize mt-0.5">
            # {{ receipt.amountInWords || 'Satu Juta Rupiah' }} #
          </p>
        </div>
      </div>

      <!-- Signature & Cashier Authorization Footer -->
      <div class="pt-2 grid grid-cols-2 gap-8 text-xs">
        <div class="space-y-1 text-slate-500 text-[11px]">
          <p class="font-semibold text-slate-700">Official Note:</p>
          <p>
            This document is an authentic computerized receipt issued by Elsasa SIS Finance.
            Valid as proof of payment and tax compliance without manual adhesive revenue stamp (Meterai).
          </p>
        </div>

        <div class="text-right space-y-1">
          <p class="text-slate-600">
            Jakarta, {{ formatDisplayDate(receipt.cashier?.confirmationDate || receipt.receiptIssuedAt) }}
          </p>
          <div class="inline-block border-2 border-emerald-600 text-emerald-700 px-3 py-1 font-bold rounded text-xs uppercase tracking-widest my-2">
            {{ receipt.cashier?.officialStampText || 'LUNAS / DITERIMA' }}
          </div>
          <p class="font-bold text-slate-900 underline mt-2">
            {{ receipt.cashier?.confirmedBy || 'Finance Cashier' }}
          </p>
          <p class="text-[11px] text-slate-500">
            {{ receipt.cashier?.title || 'Finance Officer / Kasir' }}
          </p>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3">
        <Button
          label="Close"
          severity="secondary"
          variant="outlined"
          data-testid="close-receipt-button"
          @click="handleClose"
        />
        <Button
          label="Print Receipt"
          severity="primary"
          :disabled="!receipt"
          data-testid="print-receipt-button"
          @click="handlePrint"
        >
          <template #icon>
            <Printer class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>

<style scoped>
@media print {
  body * {
    visibility: hidden;
  }
  .printable-receipt,
  .printable-receipt * {
    visibility: visible;
  }
  .printable-receipt {
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    margin: 0;
    padding: 1.5rem;
    box-shadow: none;
    border: none;
  }
}
</style>
