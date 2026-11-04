<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { AlertTriangle, XCircle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { useRejectPaymentMutation } from '../composables/useFinancePayments';
import { rejectPaymentSchema } from '../schemas/payments.schema';
import type {
  PaymentListItemDto,
  PaymentDto,
  RejectPaymentResponse,
} from '../types/payments.types';

const props = defineProps<{
  visible: boolean;
  payment: PaymentListItemDto | PaymentDto | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success', result: RejectPaymentResponse): void;
  (e: 'close'): void;
}>();

const rejectionReason = ref<string>('');
const notes = ref<string>('');
const error = ref<string>('');

const rejectPaymentMutation = useRejectPaymentMutation({
  onSuccess: (data) => {
    emit('success', data);
    emit('update:visible', false);
  },
});

const isSubmitting = computed(() => rejectPaymentMutation.isPending.value);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      rejectionReason.value = '';
      notes.value = '';
      error.value = '';
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleReject() {
  error.value = '';

  const parsed = rejectPaymentSchema.safeParse({
    rejectionReason: rejectionReason.value.trim(),
    notes: notes.value.trim() || null,
  });

  if (!parsed.success) {
    error.value = parsed.error.issues[0]?.message ?? 'Mandatory rejection reason is required.';
    return;
  }

  if (!props.payment?.id) return;

  rejectPaymentMutation.mutate({
    paymentId: props.payment.id,
    data: {
      rejectionReason: parsed.data.rejectionReason,
      notes: parsed.data.notes,
    },
  });
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Reject Payment Submission"
    :style="{ width: '32rem' }"
    :closable="!isSubmitting"
    data-testid="reject-payment-modal"
    @update:visible="handleClose"
  >
    <div v-if="payment" class="space-y-4 text-sm text-slate-700">
      <!-- Warning Banner -->
      <div class="rounded-lg border border-rose-200 bg-rose-50/70 p-3.5 flex items-start gap-3">
        <div class="rounded-full bg-rose-100 p-1.5 text-rose-700 mt-0.5 shrink-0">
          <AlertTriangle class="w-4 h-4" />
        </div>
        <div>
          <h4 class="font-semibold text-rose-900">Mandatory Queue Rejection</h4>
          <p class="text-xs text-rose-700 mt-0.5 leading-relaxed">
            Rejecting this payment marks the record as <code class="font-semibold">rejected</code> and
            leaves it unconfirmed. A clear audit reason is required for institutional records and payer notification.
          </p>
        </div>
      </div>

      <!-- Payment Reference Summary -->
      <div class="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs space-y-1">
        <div class="flex justify-between">
          <span class="text-slate-500">Payment Ref:</span>
          <span class="font-mono font-medium text-slate-800">{{ payment.paymentNumber }}</span>
        </div>
        <div class="flex justify-between">
          <span class="text-slate-500">Beneficiary:</span>
          <span class="font-medium text-slate-800">{{ payment.studentFullName || 'Pre-Enrollment Candidate' }}</span>
        </div>
        <div class="flex justify-between">
          <span class="text-slate-500">Amount:</span>
          <span class="font-mono font-semibold text-slate-800">{{ formatIDR(payment.paymentAmount) }}</span>
        </div>
      </div>

      <!-- Form Inputs -->
      <FormField label="Rejection Reason" required :error="error">
        <Textarea
          v-model="rejectionReason"
          rows="3"
          class="w-full text-sm"
          placeholder="State why this submission was rejected (e.g. remittance not found in bank statement, transfer slip unreadable, incorrect amount)..."
          :invalid="!!error"
          data-testid="rejection-reason-input"
        />
      </FormField>

      <FormField label="Internal Memo / Additional Notes">
        <Textarea
          v-model="notes"
          rows="2"
          class="w-full text-xs"
          placeholder="Optional notes for cashier desk records..."
          data-testid="rejection-notes-input"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="isSubmitting"
          data-testid="cancel-reject-payment-button"
          @click="handleClose"
        />
        <Button
          :loading="isSubmitting"
          severity="danger"
          data-testid="commit-reject-payment-button"
          @click="handleReject"
        >
          <XCircle class="w-4 h-4 mr-1.5" />
          <span>Reject Submission</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
