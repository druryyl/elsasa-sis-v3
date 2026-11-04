<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import {
  CheckCircle2,
  AlertCircle,
  FileCheck,
  CreditCard,
  User,
  Building2,
  Calendar,
  Hash,
} from 'lucide-vue-next';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { useConfirmPaymentMutation } from '../composables/useFinancePayments';
import type {
  PaymentListItemDto,
  PaymentDto,
  ConfirmPaymentResponse,
} from '../types/payments.types';

const props = defineProps<{
  visible: boolean;
  payment: PaymentListItemDto | PaymentDto | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success', result: ConfirmPaymentResponse): void;
  (e: 'close'): void;
}>();

const currentYear = new Date().getFullYear();

const confirmPaymentMutation = useConfirmPaymentMutation({
  onSuccess: (data) => {
    emit('success', data);
    emit('update:visible', false);
  },
});

const isSubmitting = computed(() => confirmPaymentMutation.isPending.value);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleConfirm() {
  if (!props.payment?.id) return;

  confirmPaymentMutation.mutate({
    paymentId: props.payment.id,
  });
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Confirm Payment Submission"
    :style="{ width: '36rem' }"
    :closable="!isSubmitting"
    data-testid="confirm-payment-modal"
    @update:visible="handleClose"
  >
    <div v-if="payment" class="space-y-4 text-sm text-slate-700">
      <!-- Notice banner -->
      <div class="rounded-lg border border-emerald-200 bg-emerald-50/70 p-3.5 flex items-start gap-3">
        <div class="rounded-full bg-emerald-100 p-1.5 text-emerald-700 mt-0.5 shrink-0">
          <FileCheck class="w-4 h-4" />
        </div>
        <div>
          <h4 class="font-semibold text-slate-800">Verification &amp; Receipt Assignment</h4>
          <p class="text-xs text-slate-600 mt-0.5 leading-relaxed">
            Approving this submission permanently confirms the transaction and assigns the next sequential
            official receipt number (<code class="font-mono text-emerald-800 font-semibold">REC-{{ currentYear }}-XXXXXX</code>).
            Confirmed receipts are immutable and immediately available for fund allocation.
          </p>
        </div>
      </div>

      <!-- Payment Summary Card -->
      <div class="rounded-lg border border-slate-200 bg-slate-50/50 p-4 space-y-3">
        <div class="flex items-center justify-between border-b border-slate-200 pb-2.5">
          <div>
            <span class="text-xs text-slate-500 font-medium">Payment Identifier</span>
            <p class="font-mono font-semibold text-slate-900 text-sm mt-0.5">
              {{ payment.paymentNumber }}
            </p>
          </div>
          <div class="text-right">
            <span class="text-xs text-slate-500 font-medium">Gross Amount</span>
            <p class="font-mono font-bold text-emerald-700 text-base mt-0.5">
              {{ formatIDR(payment.paymentAmount) }}
            </p>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3 text-xs pt-1">
          <div>
            <span class="text-slate-500 flex items-center gap-1 font-medium">
              <User class="w-3.5 h-3.5 text-slate-400" /> Beneficiary / Candidate
            </span>
            <p class="font-medium text-slate-800 mt-0.5">
              {{ payment.studentFullName || 'Pre-Enrollment Candidate' }}
            </p>
            <p class="text-[11px] font-mono text-slate-500">
              {{ payment.studentNumber ? `NIM: ${payment.studentNumber}` : `Acc: ${payment.accountNumber}` }}
            </p>
          </div>

          <div>
            <span class="text-slate-500 flex items-center gap-1 font-medium">
              <Building2 class="w-3.5 h-3.5 text-slate-400" /> Remitting Payer
            </span>
            <p class="font-medium text-slate-800 mt-0.5">
              {{ payment.payerName }}
            </p>
            <p class="text-[11px] font-mono text-slate-500">
              {{ payment.payerNumber }}
            </p>
          </div>

          <div>
            <span class="text-slate-500 flex items-center gap-1 font-medium">
              <CreditCard class="w-3.5 h-3.5 text-slate-400" /> Payment Method
            </span>
            <p class="font-medium text-slate-800 capitalize mt-0.5">
              {{ payment.paymentMethod.replace(/_/g, ' ') }}
            </p>
          </div>

          <div>
            <span class="text-slate-500 flex items-center gap-1 font-medium">
              <Hash class="w-3.5 h-3.5 text-slate-400" /> Bank / External Reference
            </span>
            <p class="font-mono text-slate-800 mt-0.5">
              {{ payment.externalReference || '—' }}
            </p>
          </div>

          <div>
            <span class="text-slate-500 flex items-center gap-1 font-medium">
              <Calendar class="w-3.5 h-3.5 text-slate-400" /> Transaction Date
            </span>
            <p class="text-slate-800 mt-0.5">
              {{ formatDisplayDate(payment.paymentDate) }}
            </p>
          </div>

          <div>
            <span class="text-slate-500 flex items-center gap-1 font-medium">
              <Calendar class="w-3.5 h-3.5 text-slate-400" /> Submission Date
            </span>
            <p class="text-slate-800 mt-0.5">
              {{ formatDisplayDateTime(payment.submissionDate) }}
            </p>
          </div>
        </div>
      </div>

      <!-- Audit disclaimer -->
      <p class="text-[11px] text-slate-500 italic">
        By clicking confirm, you certify that the remittance has cleared the institution's bank
        account or counter cash drawer. This action cannot be undone.
      </p>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="isSubmitting"
          data-testid="cancel-confirm-payment-button"
          @click="handleClose"
        />
        <Button
          :loading="isSubmitting"
          severity="success"
          data-testid="commit-confirm-payment-button"
          @click="handleConfirm"
        >
          <CheckCircle2 class="w-4 h-4 mr-1.5" />
          <span>Confirm &amp; Assign Receipt</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
