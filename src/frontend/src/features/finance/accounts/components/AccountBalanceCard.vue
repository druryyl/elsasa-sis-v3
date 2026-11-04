<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import {
  Wallet,
  CreditCard,
  CheckCircle2,
  AlertCircle,
  FileText,
  PlusCircle,
  Calendar,
  Layers,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import { formatIdr } from '../../shared/currency.utils';
import { getAccountStatusDescriptor } from '../../shared/financeStatus.utils';
import type { AccountSummaryDto } from '../types/accounts.types';

interface Props {
  account: AccountSummaryDto | null | undefined;
  loading?: boolean;
  canAssessFee?: boolean;
  canGenerateBill?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  account: null,
  loading: false,
  canAssessFee: true,
  canGenerateBill: true,
});

const emit = defineEmits<{
  (e: 'assessFee'): void;
  (e: 'generateBill'): void;
  (e: 'generateStatement'): void;
}>();

const statusDescriptor = computed(() =>
  getAccountStatusDescriptor(props.account?.accountStatus),
);

const balanceVariant = computed(() => {
  const bal = props.account?.currentBalance ?? 0;
  if (bal > 0) return 'warn';
  if (bal < 0) return 'info';
  return 'success';
});

const openObligationsCount = computed(
  () => props.account?.openObligations?.length ?? 0,
);
</script>

<template>
  <div
    class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden"
    data-testid="account-balance-card"
  >
    <!-- Header banner -->
    <div
      class="px-6 py-4 bg-linear-to-r from-slate-50 to-slate-100 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4"
    >
      <div class="flex items-center gap-3">
        <div
          class="w-10 h-10 rounded-lg bg-emerald-100 text-emerald-700 flex items-center justify-center font-semibold"
        >
          <Wallet class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
              Account Number
            </span>
            <span
              class="font-mono text-sm font-bold text-slate-900 bg-white px-2 py-0.5 rounded border border-slate-200"
              data-testid="account-number"
            >
              {{ account?.accountNumber || '—' }}
            </span>
          </div>
          <div class="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
            <span v-if="account?.openedDate" class="flex items-center gap-1">
              <Calendar class="w-3.5 h-3.5 text-slate-400" />
              Opened: {{ account.openedDate }}
            </span>
            <span v-if="account?.currencyCode" class="font-medium text-slate-600">
              • {{ account.currencyCode }} (Zero Decimal)
            </span>
          </div>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <StatusPill
          :status="account?.accountStatus || 'active'"
          :label="statusDescriptor.label"
          data-testid="account-status-badge"
        />

        <div class="flex items-center gap-1.5 ml-2">
          <Button
            v-if="canAssessFee"
            size="small"
            severity="secondary"
            outlined
            data-testid="assess-fee-btn"
            @click="emit('assessFee')"
          >
            <template #icon>
              <PlusCircle class="w-4 h-4 mr-1.5 text-indigo-600" />
            </template>
            Assess Fee
          </Button>

          <Button
            v-if="canGenerateBill"
            size="small"
            severity="primary"
            data-testid="generate-bill-btn"
            :disabled="openObligationsCount === 0"
            @click="emit('generateBill')"
          >
            <template #icon>
              <FileText class="w-4 h-4 mr-1.5" />
            </template>
            Compile Bill
            <span
              v-if="openObligationsCount > 0"
              class="ml-1 px-1.5 py-0.2 rounded-full text-xs bg-emerald-800 text-white"
            >
              {{ openObligationsCount }}
            </span>
          </Button>
        </div>
      </div>
    </div>

    <!-- Core Financial Ledger Metrics -->
    <div class="p-6 grid grid-cols-1 md:grid-cols-3 gap-6">
      <!-- 1. Total Assessed -->
      <div
        class="bg-slate-50/70 rounded-lg p-4 border border-slate-100 flex flex-col justify-between"
      >
        <div class="flex items-center justify-between text-slate-500 text-xs font-semibold uppercase tracking-wider mb-2">
          <span>Total Assessed</span>
          <Layers class="w-4 h-4 text-slate-400" />
        </div>
        <div>
          <div
            class="text-2xl font-bold font-mono text-slate-800"
            data-testid="total-assessed-amount"
          >
            {{ formatIdr(account?.totalAssessed ?? 0) }}
          </div>
          <p class="text-xs text-slate-500 mt-1">
            Aggregate historical fee obligations assessed.
          </p>
        </div>
      </div>

      <!-- 2. Total Settled -->
      <div
        class="bg-emerald-50/50 rounded-lg p-4 border border-emerald-100/70 flex flex-col justify-between"
      >
        <div class="flex items-center justify-between text-emerald-800 text-xs font-semibold uppercase tracking-wider mb-2">
          <span>Total Settled</span>
          <CheckCircle2 class="w-4 h-4 text-emerald-600" />
        </div>
        <div>
          <div
            class="text-2xl font-bold font-mono text-emerald-700"
            data-testid="total-settled-amount"
          >
            {{ formatIdr(account?.totalSettled ?? 0) }}
          </div>
          <p class="text-xs text-emerald-600/80 mt-1">
            Fully settled receipts and cleared payments.
          </p>
        </div>
      </div>

      <!-- 3. Prominent Derived Current Balance (TD-02) -->
      <div
        class="rounded-lg p-4 border flex flex-col justify-between transition-colors shadow-xs"
        :class="{
          'bg-amber-50/70 border-amber-200 text-amber-900': balanceVariant === 'warn',
          'bg-emerald-50/70 border-emerald-200 text-emerald-900': balanceVariant === 'success',
          'bg-blue-50/70 border-blue-200 text-blue-900': balanceVariant === 'info',
        }"
      >
        <div class="flex items-center justify-between text-xs font-bold uppercase tracking-wider mb-2">
          <span>Derived Current Balance</span>
          <CreditCard
            class="w-4 h-4"
            :class="{
              'text-amber-600': balanceVariant === 'warn',
              'text-emerald-600': balanceVariant === 'success',
              'text-blue-600': balanceVariant === 'info',
            }"
          />
        </div>
        <div>
          <div
            class="text-3xl font-extrabold font-mono tracking-tight"
            :class="{
              'text-amber-800': balanceVariant === 'warn',
              'text-emerald-800': balanceVariant === 'success',
              'text-blue-800': balanceVariant === 'info',
            }"
            data-testid="current-balance-amount"
          >
            {{ formatIdr(account?.currentBalance ?? 0) }}
          </div>
          <div class="flex items-center gap-1.5 mt-1 text-xs font-medium">
            <span v-if="(account?.currentBalance ?? 0) > 0" class="text-amber-700 flex items-center gap-1">
              <AlertCircle class="w-3.5 h-3.5" /> Outstanding balance payable
            </span>
            <span v-else-if="(account?.currentBalance ?? 0) === 0" class="text-emerald-700 flex items-center gap-1">
              <CheckCircle2 class="w-3.5 h-3.5" /> All liabilities settled
            </span>
            <span v-else class="text-blue-700">
              Account has available credit
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
