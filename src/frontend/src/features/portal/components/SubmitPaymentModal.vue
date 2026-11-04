<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import {
  CreditCard,
  Building,
  Calendar,
  FileCheck,
  FileText,
  Upload,
  AlertCircle,
  CheckCircle2,
  Clock,
  ShieldCheck,
  User,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatIdr } from '../../finance/shared/currency.utils';
import { useSubmitPaymentMutation } from '../../finance/payments/composables/useFinancePayments';
import {
  submitPaymentSchema,
  PAYMENT_METHODS,
} from '../../finance/payments/schemas/payments.schema';
import type {
  SubmitPaymentPayload,
  SubmitPaymentResponse,
} from '../../finance/payments/types/payments.types';
import type { BillingDocumentDto } from '../../finance/billing/types/billing.types';
import { useAuthStore } from '@/app/stores/auth.store';

interface Props {
  visible: boolean;
  studentId?: string;
  studentName?: string;
  payerName?: string;
  payerEmail?: string;
  payerPhone?: string;
  bill?: BillingDocumentDto | null;
  openBills?: BillingDocumentDto[];
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  studentId: '',
  studentName: '',
  payerName: '',
  payerEmail: '',
  payerPhone: '',
  bill: null,
  openBills: () => [],
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'submitted', response: SubmitPaymentResponse): void;
  (e: 'close'): void;
}>();

const authStore = useAuthStore();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Payment method options for parent portal
const portalPaymentMethods = [
  { label: 'Bank Transfer (BCA / Mandiri / BSI)', value: 'bank_transfer' },
  { label: 'Virtual Account (VA)', value: 'virtual_account' },
  { label: 'QRIS Instant Transfer', value: 'qris' },
  { label: 'Other Electronic Transfer', value: 'other' },
];

// Form state
interface FormState {
  studentId: string;
  studentName: string;
  selectedBillId: string;
  amount: number;
  paymentMethod: string;
  externalReference: string;
  paymentDate: string;
  proofAttachmentRef: string;
  payerName: string;
  payerEmail: string;
  payerPhone: string;
  notes: string;
}

const form = ref<FormState>({
  studentId: '',
  studentName: '',
  selectedBillId: '',
  amount: 0,
  paymentMethod: 'bank_transfer',
  externalReference: '',
  paymentDate: new Date().toISOString().slice(0, 10),
  proofAttachmentRef: '',
  payerName: '',
  payerEmail: '',
  payerPhone: '',
  notes: '',
});

const errors = ref<Record<string, string>>({});
const submissionSuccess = ref<SubmitPaymentResponse | null>(null);

function getTodayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function initForm() {
  errors.value = {};
  submissionSuccess.value = null;

  const currentBill = props.bill;
  const initialAmount = currentBill?.totalBilledAmount ?? 0;
  const initialStudentId = props.studentId || currentBill?.studentId || '';
  const initialStudentName = props.studentName || currentBill?.studentFullName || '';
  const initialPayerName =
    props.payerName || currentBill?.recipientName || authStore.user?.fullName || '';
  const initialPayerEmail = props.payerEmail || authStore.user?.email || '';

  form.value = {
    studentId: initialStudentId,
    studentName: initialStudentName,
    selectedBillId: currentBill?.id ?? (props.openBills[0]?.id || ''),
    amount: initialAmount,
    paymentMethod: 'bank_transfer',
    externalReference: '',
    paymentDate: getTodayIso(),
    proofAttachmentRef: '',
    payerName: initialPayerName,
    payerEmail: initialPayerEmail,
    payerPhone: props.payerPhone || '',
    notes: currentBill?.documentNumber ? `Payment for Bill #${currentBill.documentNumber}` : '',
  };
}

watch(
  () => [props.visible, props.bill, props.studentId] as const,
  ([isOpen]) => {
    if (isOpen) {
      initForm();
    }
  },
  { immediate: true },
);

function handleBillSelectionChange(event: Event) {
  const target = event.target as HTMLSelectElement;
  const billId = target.value;
  form.value.selectedBillId = billId;

  const matched = props.openBills.find((b) => b.id === billId);
  if (matched) {
    form.value.amount = matched.totalBilledAmount;
    form.value.notes = `Payment for Bill #${matched.documentNumber}`;
    if (!form.value.studentId && matched.studentId) {
      form.value.studentId = matched.studentId;
    }
    if (!form.value.studentName && matched.studentFullName) {
      form.value.studentName = matched.studentFullName;
    }
  }
}

// Mutation
const submitMutation = useSubmitPaymentMutation({
  onSuccess: (data) => {
    submissionSuccess.value = data;
    emit('submitted', data);
  },
});

function handleSubmit() {
  errors.value = {};

  const notesWithProof = [
    form.value.notes?.trim() || null,
    form.value.proofAttachmentRef
      ? `Proof Reference: ${form.value.proofAttachmentRef.trim()}`
      : null,
  ]
    .filter(Boolean)
    .join(' | ');

  const rawPayload: SubmitPaymentPayload = {
    studentId: form.value.studentId.trim() || null,
    amount: Number(form.value.amount),
    paymentMethod: form.value.paymentMethod,
    paymentDate: form.value.paymentDate || getTodayIso(),
    externalReference: form.value.externalReference.trim() || null,
    payerName: form.value.payerName.trim(),
    payerEmail: form.value.payerEmail?.trim() || null,
    payerPhone: form.value.payerPhone?.trim() || null,
    payerType: 'parent_guardian',
    notes: notesWithProof || null,
  };

  const validation = submitPaymentSchema.safeParse(rawPayload);
  if (!validation.success) {
    for (const issue of validation.error.issues) {
      const path = issue.path.join('.');
      if (!errors.value[path]) {
        errors.value[path] = issue.message;
      }
    }
    return;
  }

  submitMutation.mutate(rawPayload);
}

function handleClose() {
  emit('update:visible', false);
  emit('close');
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    class="w-full max-w-xl mx-4"
    data-testid="submit-payment-modal"
    @hide="handleClose"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <CreditCard class="w-5 h-5 text-indigo-600" />
        <span class="font-semibold text-slate-900 text-lg">
          Submit Bank Transfer Payment
        </span>
      </div>
    </template>

    <!-- Success Confirmation View -->
    <div
      v-if="submissionSuccess"
      class="py-6 px-4 text-center space-y-4"
      data-testid="payment-submission-success"
    >
      <div class="w-14 h-14 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
        <CheckCircle2 class="w-8 h-8" />
      </div>

      <div class="space-y-1">
        <h3 class="text-lg font-bold text-slate-900">
          Payment Successfully Submitted!
        </h3>
        <p class="text-xs text-slate-500 max-w-md mx-auto">
          Your payment transaction reference
          <span class="font-mono font-bold text-slate-800">{{ submissionSuccess.paymentNumber }}</span>
          has been queued for cashier verification.
        </p>
      </div>

      <!-- Queue Notice Alert -->
      <div class="p-3 bg-amber-50 border border-amber-200 rounded-xl text-left flex items-start gap-3 text-xs text-amber-900">
        <Clock class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div class="space-y-0.5">
          <span class="font-semibold block">Pending Cashier Verification</span>
          <p class="text-[11px] text-amber-800">
            A finance officer will verify your bank transfer against school account statements. Once confirmed, an official sequential receipt voucher (<code class="font-mono">REC-YYYY-XXXXXX</code>) will be generated and available for download.
          </p>
        </div>
      </div>

      <div class="pt-2">
        <Button
          size="small"
          severity="primary"
          label="Done"
          class="px-6"
          data-testid="dismiss-success-btn"
          @click="handleClose"
        />
      </div>
    </div>

    <!-- Payment Submission Form -->
    <form
      v-else
      class="space-y-4 text-xs"
      data-testid="submit-payment-form"
      @submit.prevent="handleSubmit"
    >
      <!-- School Bank Account Destination Card -->
      <div class="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
        <div class="flex items-center gap-1.5 font-semibold text-slate-800 text-[11px] uppercase tracking-wider">
          <Building class="w-4 h-4 text-indigo-600" />
          <span>Destination School Bank Account</span>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 text-[11px] bg-white p-2.5 rounded-lg border border-slate-200">
          <div>
            <span class="text-slate-400 block text-[10px]">Bank:</span>
            <span class="font-bold text-slate-800">Bank Central Asia (BCA)</span>
          </div>
          <div>
            <span class="text-slate-400 block text-[10px]">Account Name:</span>
            <span class="font-bold text-slate-800">Yayasan Pendidikan Elsasa</span>
          </div>
          <div>
            <span class="text-slate-400 block text-[10px]">Account Number:</span>
            <span class="font-mono font-bold text-indigo-700">882-019-2810</span>
          </div>
          <div>
            <span class="text-slate-400 block text-[10px]">Branch:</span>
            <span class="text-slate-700">KCP Kebayoran Baru</span>
          </div>
        </div>
      </div>

      <!-- General Form Error Banner -->
      <div
        v-if="errors.studentId || errors.payerName || errors.amount"
        class="flex items-center gap-2 p-2.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-[11px]"
      >
        <AlertCircle class="w-4 h-4 shrink-0 text-rose-600" />
        <span>{{ errors.studentId || errors.payerName || errors.amount }}</span>
      </div>

      <!-- Bill / Student Information -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Student Selection / Display -->
        <FormField label="Student / Beneficiary" required :error="errors.studentId">
          <InputText
            :value="form.studentName || form.studentId || 'Selected Student'"
            disabled
            class="w-full !text-xs !py-1.5 bg-slate-100 text-slate-700 cursor-not-allowed"
            data-testid="payment-student-input"
          />
        </FormField>

        <!-- Bill Selection -->
        <FormField
          label="Target Bill / Statement"
          hint="Select the invoice being settled"
        >
          <select
            v-if="openBills.length > 0"
            :value="form.selectedBillId"
            class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            data-testid="bill-selection-dropdown"
            @change="handleBillSelectionChange"
          >
            <option
              v-for="b in openBills"
              :key="b.id"
              :value="b.id"
            >
              {{ b.documentNumber }} — {{ formatIdr(b.totalBilledAmount) }} (Due: {{ b.paymentDueDate || '-' }})
            </option>
          </select>
          <InputText
            v-else
            :value="bill?.documentNumber ? `Bill #${bill.documentNumber}` : 'General School Fee'"
            disabled
            class="w-full !text-xs !py-1.5 bg-slate-100 text-slate-700 cursor-not-allowed"
          />
        </FormField>
      </div>

      <!-- Amount & Payment Method -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- Transfer Amount -->
        <FormField
          label="Transfer Amount (IDR)"
          required
          :error="errors.amount"
          hint="Exact integer rupiah transferred"
        >
          <div class="relative">
            <span class="absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400 font-medium">Rp</span>
            <input
              v-model.number="form.amount"
              type="number"
              min="1"
              step="1"
              class="w-full border border-slate-300 rounded pl-9 pr-2.5 py-1.5 text-xs font-mono font-bold bg-white text-slate-900 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              data-testid="payment-amount-input"
            />
          </div>
        </FormField>

        <!-- Payment Method -->
        <FormField label="Payment Method" required :error="errors.paymentMethod">
          <select
            v-model="form.paymentMethod"
            class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            data-testid="payment-method-select"
          >
            <option
              v-for="opt in portalPaymentMethods"
              :key="opt.value"
              :value="opt.value"
            >
              {{ opt.label }}
            </option>
          </select>
        </FormField>
      </div>

      <!-- Transaction Reference & Transfer Date -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <!-- External Reference -->
        <FormField
          label="Transaction Reference / Slip #"
          required
          :error="errors.externalReference"
          hint="Bank transaction ID, transfer code, or slip number"
        >
          <InputText
            v-model="form.externalReference"
            placeholder="e.g. TRX-BCA-9281928"
            class="w-full !text-xs !py-1.5 font-mono"
            data-testid="external-reference-input"
          />
        </FormField>

        <!-- Transfer Date -->
        <FormField label="Transfer Date" required :error="errors.paymentDate">
          <input
            v-model="form.paymentDate"
            type="date"
            class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            data-testid="payment-date-input"
          />
        </FormField>
      </div>

      <!-- Proof of Transfer Reference / Filename -->
      <FormField
        label="Proof of Transfer Attachment Reference"
        hint="File name or document reference of transfer receipt screenshot"
      >
        <div class="relative">
          <InputText
            v-model="form.proofAttachmentRef"
            placeholder="e.g. bca_transfer_screenshot_20261104.jpg"
            class="w-full !text-xs !py-1.5 font-mono"
            data-testid="proof-attachment-input"
          />
        </div>
      </FormField>

      <!-- Payer Contact Info -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1 border-t border-slate-100">
        <!-- Payer Name -->
        <FormField label="Sender / Payer Name" required :error="errors.payerName">
          <InputText
            v-model="form.payerName"
            placeholder="e.g. Budi Santoso"
            class="w-full !text-xs !py-1.5"
            data-testid="payer-name-input"
          />
        </FormField>

        <!-- Payer Email -->
        <FormField label="Contact Email" :error="errors.payerEmail">
          <InputText
            v-model="form.payerEmail"
            type="email"
            placeholder="e.g. budi@gmail.com"
            class="w-full !text-xs !py-1.5"
            data-testid="payer-email-input"
          />
        </FormField>
      </div>

      <!-- Notes / Additional Details -->
      <FormField label="Payment Notes (Optional)" :error="errors.notes">
        <Textarea
          v-model="form.notes"
          rows="2"
          placeholder="Any additional information regarding this transfer..."
          class="w-full !text-xs"
          data-testid="payment-notes-input"
        />
      </FormField>

      <!-- Queue Clearance Advisory -->
      <div class="p-3 bg-indigo-50/70 border border-indigo-200 rounded-lg flex items-start gap-2.5 text-[11px] text-indigo-900">
        <ShieldCheck class="w-4 h-4 text-indigo-600 shrink-0 mt-0.5" />
        <p>
          Submitting this payment places it in the <strong>Cashier Verification Queue</strong>. Your official sequential receipt voucher will be issued immediately upon verification by the Finance Office.
        </p>
      </div>

      <!-- Footer Buttons -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          data-testid="cancel-submit-payment-btn"
          @click="handleClose"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="small"
          severity="primary"
          :loading="submitMutation.isPending.value"
          data-testid="confirm-submit-payment-btn"
        >
          <CreditCard class="w-3.5 h-3.5 mr-1.5" />
          Submit for Verification
        </Button>
      </div>
    </form>
  </Dialog>
</template>
