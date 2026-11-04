<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  CreditCard,
  Banknote,
  Building2,
  CheckCircle2,
  User,
  Hash,
  AlertCircle,
  QrCode,
  FileText,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatIDR } from '@/shared/utils/currencyUtils';
import { formatDateOnly } from '@/shared/utils/dateUtils';
import {
  useRecordPaymentMutation,
  useSubmitPaymentMutation,
} from '../composables/useFinancePayments';
import { usePayersQuery } from '../../payers/composables/useFinancePayers';
import type {
  RecordPaymentPayload,
  RecordPaymentResponse,
  SubmitPaymentPayload,
  SubmitPaymentResponse,
  PaymentMethodType,
} from '../types/payments.types';

const props = defineProps<{
  visible: boolean;
  preselectedCandidateId?: string | null;
  preselectedCandidateType?: 'student' | 'applicant' | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success', result: RecordPaymentResponse | SubmitPaymentResponse): void;
  (e: 'close'): void;
}>();

// ---------------------------------------------------------------------------
// Method Options
// ---------------------------------------------------------------------------
const paymentMethodOptions: { label: string; value: PaymentMethodType; icon?: string }[] = [
  { label: 'Cash (Tunai di Kasir)', value: 'cash' },
  { label: 'POS Card / EDC (Kartu Debit / Kredit)', value: 'pos_card' },
  { label: 'QRIS (Standard Pembayaran Digital)', value: 'qris' },
  { label: 'Bank Transfer (Transfer Rekening Sekolah)', value: 'bank_transfer' },
  { label: 'Virtual Account (VA Bank)', value: 'virtual_account' },
  { label: 'Cheque / Bilyet Giro', value: 'cheque' },
  { label: 'Other (Lainnya)', value: 'other' },
];

const payerTypeOptions = [
  { label: 'Parent / Guardian (Orang Tua / Wali)', value: 'parent_guardian' },
  { label: 'Corporate Sponsor (Perusahaan Sponsor)', value: 'corporate_sponsor' },
  { label: 'Embassy / Diplomatic (Kedutaan)', value: 'embassy' },
  { label: 'Foundation / NGO (Yayasan / Lembaga)', value: 'foundation' },
  { label: 'Other', value: 'other' },
];

// ---------------------------------------------------------------------------
// Form State
// ---------------------------------------------------------------------------
const candidateType = ref<'student' | 'applicant'>('student');
const candidateId = ref<string>('');
const isImmediateConfirm = ref<boolean>(true);

const payerMode = ref<'new' | 'existing'>('new');
const selectedPayerId = ref<string>('');
const payerName = ref<string>('');
const payerType = ref<string>('parent_guardian');
const payerIdNumber = ref<string>('');
const payerEmail = ref<string>('');
const payerPhone = ref<string>('');

const paymentMethod = ref<PaymentMethodType>('cash');
const amount = ref<number | null>(null);
const paymentDate = ref<string>(formatDateOnly(new Date()));
const externalReference = ref<string>('');
const notes = ref<string>('');

const errors = ref<Record<string, string>>({});

// Payers lookup query
const payerSearchTerm = ref<string>('');
const payersQuery = usePayersQuery(
  computed(() => ({
    searchTerm: payerSearchTerm.value || undefined,
    pageSize: 20,
  })),
  { enabled: computed(() => props.visible && payerMode.value === 'existing') },
);

const payerOptions = computed(() => {
  const items = payersQuery.data.value?.items ?? [];
  return items.map((p) => ({
    label: `${p.payerName} (${p.payerNumber})${p.payerType ? ` • ${p.payerType}` : ''}`,
    value: p.id,
    name: p.payerName,
    type: p.payerType,
    identificationNumber: p.identificationNumber,
    email: p.email,
    phoneNumber: p.phoneNumber,
  }));
});

watch(selectedPayerId, (newId) => {
  if (newId) {
    const found = payerOptions.value.find((p) => p.value === newId);
    if (found) {
      payerName.value = found.name;
      payerType.value = found.type || 'parent_guardian';
      payerIdNumber.value = found.identificationNumber || '';
      payerEmail.value = found.email || '';
      payerPhone.value = found.phoneNumber || '';
    }
  }
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      errors.value = {};
      candidateType.value = props.preselectedCandidateType ?? 'student';
      candidateId.value = props.preselectedCandidateId ?? '';
      isImmediateConfirm.value = true;
      payerMode.value = 'new';
      selectedPayerId.value = '';
      payerName.value = '';
      payerType.value = 'parent_guardian';
      payerIdNumber.value = '';
      payerEmail.value = '';
      payerPhone.value = '';
      paymentMethod.value = 'cash';
      amount.value = null;
      paymentDate.value = formatDateOnly(new Date());
      externalReference.value = '';
      notes.value = '';
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------
const recordPaymentMutation = useRecordPaymentMutation({
  onSuccess: (data) => {
    emit('success', data);
    emit('update:visible', false);
  },
});

const submitPaymentMutation = useSubmitPaymentMutation({
  onSuccess: (data) => {
    emit('success', data);
    emit('update:visible', false);
  },
});

const isSubmitting = computed(
  () => recordPaymentMutation.isPending.value || submitPaymentMutation.isPending.value,
);

// ---------------------------------------------------------------------------
// Submission Handler
// ---------------------------------------------------------------------------
function validateForm(): boolean {
  errors.value = {};

  if (!candidateId.value.trim()) {
    errors.value.candidateId = 'Candidate identifier (Student ID or Applicant ID) is required.';
  }

  if (!payerName.value.trim()) {
    errors.value.payerName = 'Payer name is required.';
  }

  if (!amount.value || amount.value <= 0) {
    errors.value.amount = 'Payment amount must be greater than zero IDR.';
  } else if (!Number.isInteger(amount.value)) {
    errors.value.amount = 'Payment amount must be an exact integer IDR value.';
  }

  if (!paymentMethod.value) {
    errors.value.paymentMethod = 'Payment method is required.';
  }

  return Object.keys(errors.value).length === 0;
}

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleSubmit() {
  if (!validateForm()) return;

  const payload: RecordPaymentPayload | SubmitPaymentPayload = {
    studentId: candidateType.value === 'student' ? candidateId.value.trim() : null,
    applicantId: candidateType.value === 'applicant' ? candidateId.value.trim() : null,
    amount: Math.round(amount.value!),
    paymentMethod: paymentMethod.value,
    paymentDate: paymentDate.value || null,
    externalReference: externalReference.value.trim() || null,
    payerName: payerName.value.trim(),
    payerType: payerType.value || null,
    payerIdentificationNumber: payerIdNumber.value.trim() || null,
    payerEmail: payerEmail.value.trim() || null,
    payerPhone: payerPhone.value.trim() || null,
    payerId: payerMode.value === 'existing' && selectedPayerId.value ? selectedPayerId.value : null,
    notes: notes.value.trim() || null,
  };

  if (isImmediateConfirm.value) {
    recordPaymentMutation.mutate(payload as RecordPaymentPayload);
  } else {
    submitPaymentMutation.mutate(payload as SubmitPaymentPayload);
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Record Payment Intake"
    :style="{ width: '42rem' }"
    :closable="!isSubmitting"
    data-testid="record-payment-modal"
    @update:visible="handleClose"
  >
    <div class="space-y-5 text-sm text-slate-700">
      <!-- Info Header Banner -->
      <div class="rounded-lg border border-sky-100 bg-sky-50/70 p-3.5 flex items-start gap-3">
        <div class="rounded-full bg-sky-100 p-1.5 text-sky-700 mt-0.5 shrink-0">
          <Banknote class="w-4 h-4" />
        </div>
        <div>
          <h4 class="font-semibold text-slate-800">Cashier Counter Intake</h4>
          <p class="text-xs text-slate-600 mt-0.5 leading-relaxed">
            Record physical cash, POS EDC, QRIS, or cleared transfer payments. Confirmed records
            atomically issue an official sequential receipt voucher (<code class="font-mono text-sky-800">REC-{Year}-{Seq:D6}</code>).
          </p>
        </div>
      </div>

      <!-- Candidate Selection Section -->
      <div class="rounded-lg border border-slate-200 bg-slate-50/50 p-4 space-y-3">
        <div class="flex items-center justify-between">
          <label class="text-xs font-semibold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
            <User class="w-3.5 h-3.5 text-slate-500" /> Candidate Identification
          </label>
          <div class="inline-flex rounded-md border border-slate-200 bg-white p-0.5 text-xs shadow-xs">
            <button
              type="button"
              class="px-2.5 py-1 rounded-sm font-medium transition-colors"
              :class="candidateType === 'student' ? 'bg-indigo-600 text-white shadow-xs' : 'text-slate-600 hover:text-slate-900'"
              @click="candidateType = 'student'"
            >
              Enrolled Student
            </button>
            <button
              type="button"
              class="px-2.5 py-1 rounded-sm font-medium transition-colors"
              :class="candidateType === 'applicant' ? 'bg-indigo-600 text-white shadow-xs' : 'text-slate-600 hover:text-slate-900'"
              @click="candidateType = 'applicant'"
            >
              Pre-Enrollment Applicant
            </button>
          </div>
        </div>

        <FormField
          :label="candidateType === 'student' ? 'Student ID / UUID' : 'Applicant ID / UUID'"
          required
          :error="errors.candidateId"
        >
          <div class="relative">
            <InputText
              v-model="candidateId"
              class="w-full font-mono text-xs"
              :placeholder="candidateType === 'student' ? 'e.g. 00000000-0000-0000-0000-000000000001' : 'e.g. 00000000-0000-0000-0000-000000000002'"
              :invalid="!!errors.candidateId"
              data-testid="candidate-id-input"
            />
          </div>
          <span class="text-[11px] text-slate-500 mt-1 block">
            Dual Reference pattern supports pre-enrollment applicants (<code class="text-xs">GAP-001</code>) and matriculated students.
          </span>
        </FormField>
      </div>

      <!-- Payer Information Section -->
      <div class="rounded-lg border border-slate-200 bg-white p-4 space-y-3.5 shadow-2xs">
        <div class="flex items-center justify-between">
          <label class="text-xs font-semibold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
            <Building2 class="w-3.5 h-3.5 text-slate-500" /> Payer Details
          </label>
          <div class="inline-flex rounded-md border border-slate-200 bg-slate-50 p-0.5 text-xs">
            <button
              type="button"
              class="px-2.5 py-1 rounded-sm font-medium transition-colors"
              :class="payerMode === 'new' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600 hover:text-slate-900'"
              @click="payerMode = 'new'"
            >
              Enter Details
            </button>
            <button
              type="button"
              class="px-2.5 py-1 rounded-sm font-medium transition-colors"
              :class="payerMode === 'existing' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600 hover:text-slate-900'"
              @click="payerMode = 'existing'"
            >
              Search Existing
            </button>
          </div>
        </div>

        <!-- Search existing payer -->
        <div v-if="payerMode === 'existing'" class="space-y-2">
          <FormField label="Select Payer" required :error="errors.payerName">
            <Select
              v-model="selectedPayerId"
              :options="payerOptions"
              option-label="label"
              option-value="value"
              placeholder="Search by payer name or code..."
              class="w-full text-sm"
              filter
              :loading="payersQuery.isPending.value"
              data-testid="payer-select"
            />
          </FormField>
        </div>

        <!-- Enter new payer details -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField label="Payer Legal Name" required :error="errors.payerName" class="sm:col-span-2">
            <InputText
              v-model="payerName"
              class="w-full"
              placeholder="e.g. Budi Santoso / PT Mitra Edukasi"
              :invalid="!!errors.payerName"
              data-testid="payer-name-input"
            />
          </FormField>

          <FormField label="Payer Type" required>
            <Select
              v-model="payerType"
              :options="payerTypeOptions"
              option-label="label"
              option-value="value"
              class="w-full text-sm"
              data-testid="payer-type-select"
            />
          </FormField>

          <FormField label="Identification (NIK / NPWP)">
            <InputText
              v-model="payerIdNumber"
              class="w-full"
              placeholder="e.g. 3171012345670001"
              data-testid="payer-id-number-input"
            />
          </FormField>

          <FormField label="Phone Number">
            <InputText
              v-model="payerPhone"
              class="w-full"
              placeholder="e.g. +6281234567890"
              data-testid="payer-phone-input"
            />
          </FormField>

          <FormField label="Email Address">
            <InputText
              v-model="payerEmail"
              type="email"
              class="w-full"
              placeholder="e.g. payer@example.com"
              data-testid="payer-email-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Monetary & Payment Details -->
      <div class="rounded-lg border border-slate-200 bg-white p-4 space-y-3.5 shadow-2xs">
        <label class="text-xs font-semibold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
          <CreditCard class="w-3.5 h-3.5 text-slate-500" /> Transaction &amp; Payment Details
        </label>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <FormField label="Payment Method" required :error="errors.paymentMethod">
            <Select
              v-model="paymentMethod"
              :options="paymentMethodOptions"
              option-label="label"
              option-value="value"
              class="w-full text-sm"
              data-testid="payment-method-select"
            />
          </FormField>

          <FormField label="Payment Amount (IDR)" required :error="errors.amount">
            <div class="relative">
              <InputText
                v-model.number="amount"
                type="number"
                min="1"
                step="1"
                class="w-full font-mono text-base font-medium"
                placeholder="0"
                :invalid="!!errors.amount"
                data-testid="payment-amount-input"
              />
            </div>
            <span v-if="amount && amount > 0" class="text-xs font-semibold text-emerald-700 mt-1 block">
              Formatted: {{ formatIDR(amount) }}
            </span>
          </FormField>

          <FormField label="Payment Date">
            <InputText
              v-model="paymentDate"
              type="date"
              class="w-full"
              data-testid="payment-date-input"
            />
          </FormField>

          <FormField label="External Reference / Slip #">
            <InputText
              v-model="externalReference"
              class="w-full"
              placeholder="e.g. EDC-98218 / TRF-BCA-449"
              data-testid="external-reference-input"
            />
          </FormField>

          <FormField label="Cashier Notes" class="sm:col-span-2">
            <Textarea
              v-model="notes"
              rows="2"
              class="w-full text-xs"
              placeholder="Optional remarks, cashier booth memo, or teller confirmation..."
              data-testid="payment-notes-input"
            />
          </FormField>
        </div>
      </div>

      <!-- Immediate Confirmation Toggle -->
      <div class="rounded-lg border border-amber-200/80 bg-amber-50/50 p-3.5 flex items-center justify-between">
        <div>
          <p class="font-medium text-slate-800 text-xs">Confirm Immediately at Counter</p>
          <p class="text-[11px] text-slate-600 mt-0.5">
            When enabled, issues official receipt voucher immediately (<code class="text-[10px] text-amber-900 font-mono">REC-...</code>).
            If disabled, places record in queue as <em>Submitted</em>.
          </p>
        </div>
        <label class="relative inline-flex items-center cursor-pointer ml-3 shrink-0">
          <input
            v-model="isImmediateConfirm"
            type="checkbox"
            class="sr-only peer"
            data-testid="immediate-confirm-toggle"
          />
          <div class="w-11 h-6 bg-slate-200 peer-focus:outline-hidden rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-emerald-600"></div>
        </label>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          :disabled="isSubmitting"
          data-testid="cancel-record-button"
          @click="handleClose"
        />
        <Button
          :loading="isSubmitting"
          severity="primary"
          data-testid="submit-record-payment-button"
          @click="handleSubmit"
        >
          <CheckCircle2 class="w-4 h-4 mr-1.5" />
          <span>{{ isImmediateConfirm ? 'Record &amp; Issue Receipt' : 'Queue as Submitted' }}</span>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
