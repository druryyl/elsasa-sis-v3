<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import {
  GraduationCap,
  CreditCard,
  Users,
  UserCheck,
  AlertCircle,
  ShieldCheck,
  CheckCircle2,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { formatIdr } from '@/features/finance/shared/currency.utils';
import { useStudentReceivablesQuery } from '@/features/finance/allocations/composables/useFinanceAllocations';
import { usePaymentsQuery } from '@/features/finance/payments/composables/useFinancePayments';
import {
  enrollAcceptedApplicantSchema,
  type EnrollAcceptedApplicantFormValues,
} from '../../enrollment/schemas/enrollment.schema';
import { useEnrollAcceptedApplicantMutation } from '../../enrollment/composables/useEnrollmentMutations';
import { useActiveAdmissionPeriodsQuery } from '../composables/useAdmissionQueries';
import type {
  AdmissionApplicationDetailDto,
  AdmissionPeriodOptionDto,
} from '../types/admission.types';

export interface EnrollApplicantSuccessResult {
  studentId: string;
  studentNumber: string;
  enrollmentId?: string;
}

const props = defineProps<{
  visible: boolean;
  application?: AdmissionApplicationDetailDto | null;
  dossier?: AdmissionApplicationDetailDto | null;
  periods?: AdmissionPeriodOptionDto[];
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'success', result: EnrollApplicantSuccessResult): void;
  (e: 'close'): void;
}>();

const activeApp = computed(() => props.application ?? props.dossier ?? null);

// ---------------------------------------------------------------------------
// Placement Options
// ---------------------------------------------------------------------------
const campusOptions = [
  { label: 'Main Campus (Kampus A)', value: 'Main Campus' },
  { label: 'East Campus (Kampus B)', value: 'East Campus' },
  { label: 'South Campus', value: 'South Campus' },
];

const schoolLevelOptions = [
  { label: 'TK-A', value: 'TK-A' },
  { label: 'TK-B', value: 'TK-B' },
  { label: 'Grade 1', value: 'Grade 1' },
  { label: 'Grade 2', value: 'Grade 2' },
  { label: 'Grade 3', value: 'Grade 3' },
  { label: 'Grade 4', value: 'Grade 4' },
  { label: 'Grade 5', value: 'Grade 5' },
  { label: 'Grade 6', value: 'Grade 6' },
  { label: 'Grade 7', value: 'Grade 7' },
  { label: 'Grade 8', value: 'Grade 8' },
  { label: 'Grade 9', value: 'Grade 9' },
  { label: 'Grade 10', value: 'Grade 10' },
  { label: 'Grade 11', value: 'Grade 11' },
  { label: 'Grade 12', value: 'Grade 12' },
];

const programOptions = [
  { label: 'Reguler (National Curriculum)', value: 'Reguler' },
  { label: 'Bilingual (English Intensive)', value: 'Bilingual' },
  { label: 'International (Cambridge Track)', value: 'International' },
  { label: 'Tahfiz (Integrated Islamic)', value: 'Tahfiz' },
];

// Fallback academic year choices if operations calendar is pending
const defaultAcademicYearOptions = [
  { label: '2026/2027 (PPDB 2026/2027)', value: '2026/2027', academicYearId: '00000000-0000-0000-0000-000000002026' },
  { label: '2025/2026 (TA 2025/2026)', value: '2025/2026', academicYearId: '00000000-0000-0000-0000-000000002025' },
  { label: '2027/2028 (PPDB 2027/2028)', value: '2027/2028', academicYearId: '00000000-0000-0000-0000-000000002027' },
];

const activePeriodsQuery = useActiveAdmissionPeriodsQuery({
  enabled: computed(() => props.visible && (!props.periods || props.periods.length === 0)),
});

const periodsList = computed(() => {
  if (props.periods && props.periods.length > 0) {
    return props.periods;
  }
  return activePeriodsQuery.data.value ?? [];
});

const academicYearOptions = computed(() => {
  if (periodsList.value.length > 0) {
    return periodsList.value.map((p) => {
      const match = p.name.match(/\d{4}\/\d{4}/);
      const code = match ? match[0] : p.periodCode;
      return {
        label: `${p.name} (${p.periodCode})`,
        value: code,
        academicYearId: p.academicYearId || p.id,
      };
    });
  }
  return defaultAcademicYearOptions;
});

// ---------------------------------------------------------------------------
// Form State
// ---------------------------------------------------------------------------
export interface GuardianItemState {
  fullName: string;
  relationshipType: string;
  phone: string;
  email: string;
  address: string;
  governmentIdType: string;
  governmentIdNumber: string;
  isPrimaryContact: boolean;
  isEmergencyContact: boolean;
  isFinancialContact: boolean;
  isAuthorizedPickup: boolean;
}

export interface EnrollApplicantFormState {
  applicantId: string;
  academicYearId: string;
  initialCampus: string;
  initialLevel: string;
  initialAcademicYear: string;
  initialProgram: string;
  initialCohort: string;
  paymentWaived: boolean;
  paymentReference: string;
  waiverReference: string;
  guardians: GuardianItemState[];
  notes: string;
}

const form = ref<EnrollApplicantFormState>({
  applicantId: '',
  academicYearId: '00000000-0000-0000-0000-000000002026',
  initialCampus: 'Main Campus',
  initialLevel: 'Grade 1',
  initialAcademicYear: '2026/2027',
  initialProgram: 'Reguler',
  initialCohort: '',
  paymentWaived: false,
  paymentReference: '',
  waiverReference: '',
  guardians: [],
  notes: '',
});

const errors = ref<Record<string, string>>({});

// ---------------------------------------------------------------------------
// Synchronization & Lifecycle
// ---------------------------------------------------------------------------
function initForm() {
  errors.value = {};
  const app = activeApp.value;

  const resolvedAyId =
    app?.academicYearId ||
    periodsList.value[0]?.academicYearId ||
    defaultAcademicYearOptions[0].academicYearId;

  const matchedAy = academicYearOptions.value.find((o) => o.academicYearId === resolvedAyId);
  const resolvedAyValue = matchedAy ? matchedAy.value : '2026/2027';

  // Map guardian contacts
  let guardianList: GuardianItemState[] = [];
  if (app?.contacts && app.contacts.length > 0) {
    guardianList = app.contacts.map((c) => ({
      fullName: c.fullName ?? '',
      relationshipType: c.relationship ?? 'mother',
      phone: c.phone ?? '',
      email: c.email ?? '',
      address: c.address ?? '',
      governmentIdType: '',
      governmentIdNumber: '',
      isPrimaryContact: Boolean(c.isPrimary),
      isEmergencyContact: false,
      isFinancialContact: false,
      isAuthorizedPickup: false,
    }));

    // Ensure at least one primary contact is designated
    if (!guardianList.some((g) => g.isPrimaryContact)) {
      guardianList[0].isPrimaryContact = true;
    }
  } else {
    // Fallback if no contacts attached
    guardianList = [
      {
        fullName: app?.applicant?.fullName ? `${app.applicant.fullName}'s Guardian` : 'Primary Guardian',
        relationshipType: 'legal_guardian',
        phone: app?.applicant?.phone ?? '',
        email: app?.applicant?.email ?? '',
        address: app?.applicant?.address ?? '',
        governmentIdType: '',
        governmentIdNumber: '',
        isPrimaryContact: true,
        isEmergencyContact: false,
        isFinancialContact: false,
        isAuthorizedPickup: false,
      },
    ];
  }

  form.value = {
    applicantId: app?.applicantId ?? app?.applicant?.id ?? '',
    academicYearId: resolvedAyId,
    initialCampus: app?.requestedCampus ?? 'Main Campus',
    initialLevel: app?.requestedEntryLevel ?? 'Grade 1',
    initialAcademicYear: resolvedAyValue,
    initialProgram: app?.requestedProgram ?? 'Reguler',
    initialCohort: '',
    paymentWaived: false,
    paymentReference: '',
    waiverReference: '',
    guardians: guardianList,
    notes: '',
  };
}

watch(
  () => [props.visible, props.application, props.dossier] as const,
  ([isOpen]) => {
    if (isOpen) {
      initForm();
    }
  },
  { immediate: true },
);

function handleAcademicYearChange(event: Event) {
  const target = event.target as HTMLSelectElement;
  const ayVal = target.value;
  form.value.initialAcademicYear = ayVal;
  const match = academicYearOptions.value.find((opt) => opt.value === ayVal);
  if (match) {
    form.value.academicYearId = match.academicYearId;
  }
}

function setPrimaryGuardian(index: number) {
  form.value.guardians.forEach((g, idx) => {
    g.isPrimaryContact = idx === index;
  });
}

function formatRelationship(type: string): string {
  const map: Record<string, string> = {
    mother: 'Mother (Ibu)',
    father: 'Father (Ayah)',
    legal_guardian: 'Legal Guardian (Wali)',
    grandparent: 'Grandparent (Kakek/Nenek)',
    sibling: 'Sibling (Kakak/Adik)',
    other: 'Other (Lainnya)',
  };
  return map[type.toLowerCase()] ?? type;
}

// ---------------------------------------------------------------------------
// Real-Time Finance Mandatory Enrollment Fee Clearance Gate (GAP-01, TD-06)
// ---------------------------------------------------------------------------
const candidateId = computed(() => {
  const app = activeApp.value;
  return app?.applicantId || app?.applicant?.id || '';
});

// Real-time student receivables query (checking mandatory enrollment fees)
const receivablesQuery = useStudentReceivablesQuery(candidateId, undefined, {
  enabled: computed(() => Boolean(props.visible && candidateId.value)),
});

// Real-time payments query (retrieving confirmed sequential receipt number)
const candidatePaymentsQuery = usePaymentsQuery(
  computed(() => ({
    applicantId: candidateId.value || undefined,
    status: 'confirmed',
  })),
  {
    enabled: computed(() => Boolean(props.visible && candidateId.value)),
  },
);

const mandatoryEnrollmentObligations = computed(() => {
  const list = receivablesQuery.data.value?.receivables ?? [];
  return list.filter(
    (r) =>
      r.isMandatory &&
      (r.chargeType === 'enrollment_fee' || r.chargeType === 'admission_fee'),
  );
});

const isFeeGateSettled = computed<boolean | null>(() => {
  if (!receivablesQuery.data.value) {
    return null;
  }
  const obligations = mandatoryEnrollmentObligations.value;
  if (obligations.length === 0) {
    return true;
  }
  return obligations.every(
    (o) => o.outstandingAmount <= 0 || o.receivableStatus === 'settled',
  );
});

const outstandingMandatoryFee = computed<number>(() => {
  return mandatoryEnrollmentObligations.value.reduce(
    (sum, o) => sum + (o.outstandingAmount || 0),
    0,
  );
});

const verifiedReceiptNumber = computed<string | null>(() => {
  const confirmedItems = candidatePaymentsQuery.data.value?.items ?? [];
  const confirmedPayment = confirmedItems[0];
  if (confirmedPayment?.receiptNumber) {
    return confirmedPayment.receiptNumber;
  }
  if (confirmedPayment?.externalReference) {
    return confirmedPayment.externalReference;
  }
  return null;
});

// Watch fee gate status: if settled, pre-select Payment Confirmed and set receipt ref if available
watch(
  () => [isFeeGateSettled.value, verifiedReceiptNumber.value] as const,
  ([settled, receiptRef]) => {
    if (settled === true) {
      form.value.paymentWaived = false;
      if (receiptRef && !form.value.paymentReference) {
        form.value.paymentReference = receiptRef;
      }
    }
  },
);

// ---------------------------------------------------------------------------
// Submission Pipeline
// ---------------------------------------------------------------------------
const enrollMutation = useEnrollAcceptedApplicantMutation({
  onSuccess: (data) => {
    emit('success', {
      studentId: data.studentId,
      studentNumber: data.studentNumber,
      enrollmentId: data.enrollmentId,
    });
    emit('update:visible', false);
  },
});

function handleSubmit() {
  errors.value = {};

  const app = activeApp.value;
  const applicantId = form.value.applicantId || app?.applicantId || app?.applicant?.id || '';
  const academicYearId = form.value.academicYearId || app?.academicYearId || '00000000-0000-0000-0000-000000002026';

  const rawPayload = {
    applicantId,
    academicYearId,
    initialCampus: form.value.initialCampus,
    initialLevel: form.value.initialLevel,
    initialAcademicYear: form.value.initialAcademicYear,
    initialProgram: form.value.initialProgram?.trim() || null,
    initialCohort: form.value.initialCohort?.trim() || null,
    paymentWaived: form.value.paymentWaived,
    paymentReference: form.value.paymentWaived ? null : form.value.paymentReference?.trim() || '',
    waiverReference: form.value.paymentWaived ? form.value.waiverReference?.trim() || '' : null,
    checkFinanceModule: null,
    guardians: form.value.guardians.map((g) => ({
      fullName: g.fullName.trim(),
      relationshipType: g.relationshipType.trim(),
      governmentIdType: g.governmentIdType?.trim() || null,
      governmentIdNumber: g.governmentIdNumber?.trim() || null,
      phone: g.phone?.trim() || null,
      email: g.email?.trim() || null,
      address: g.address?.trim() || null,
      isPrimaryContact: g.isPrimaryContact,
      isEmergencyContact: g.isEmergencyContact,
      isFinancialContact: g.isFinancialContact,
      isAuthorizedPickup: g.isAuthorizedPickup,
    })),
    notes: form.value.notes?.trim() || null,
    applicationId: app?.id ?? null,
  };

  const validationResult = enrollAcceptedApplicantSchema.safeParse(rawPayload);

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const path = issue.path.join('.');
      if (!errors.value[path]) {
        errors.value[path] = issue.message;
      }
    }
    return;
  }

  enrollMutation.mutate(validationResult.data);
}

function handleClose() {
  emit('update:visible', false);
  emit('close');
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Enroll Accepted Applicant"
    class="max-w-2xl w-full"
    data-testid="enroll-applicant-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 pt-1 text-xs" @submit.prevent="handleSubmit">
      <!-- Info Banner -->
      <div class="flex items-start gap-2.5 p-3 rounded-lg bg-teal-50 border border-teal-200 text-teal-900">
        <UserCheck class="w-4 h-4 shrink-0 text-teal-700 mt-0.5" />
        <div class="space-y-0.5">
          <p class="font-medium">
            Enrolling Accepted Candidate:
            <span class="font-semibold text-teal-950">{{ activeApp?.applicant?.fullName || 'Selected Applicant' }}</span>
          </p>
          <p class="text-[11px] text-teal-800">
            Finalizes administrative enrollment, issues official sequential Student Number (STD-YYYY-XXXX), promotes verified credentials, and transfers primary guardian authority.
          </p>
        </div>
      </div>

      <!-- General Form Error Message if any -->
      <div
        v-if="errors.applicantId || errors.academicYearId"
        class="flex items-center gap-2 p-2.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-[11px]"
      >
        <AlertCircle class="w-4 h-4 shrink-0 text-rose-600" />
        <span>{{ errors.applicantId || errors.academicYearId }}</span>
      </div>

      <!-- 1. Placement Parameters -->
      <div class="space-y-3 p-3.5 bg-slate-50/70 border border-slate-200 rounded-lg">
        <div class="flex items-center gap-1.5 font-semibold text-slate-800 text-xs border-b border-slate-200 pb-2">
          <GraduationCap class="w-4 h-4 text-teal-600" />
          <span>Placement Parameters</span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
          <!-- Campus -->
          <FormField label="Target Campus" required :error="errors.initialCampus">
            <select
              v-model="form.initialCampus"
              class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-teal-500"
              data-testid="enroll-campus-select"
            >
              <option v-for="c in campusOptions" :key="c.value" :value="c.value">
                {{ c.label }}
              </option>
            </select>
          </FormField>

          <!-- School Level (pre-filled from application) -->
          <FormField
            label="School Level"
            required
            :error="errors.initialLevel"
            hint="Pre-filled from application"
          >
            <select
              v-model="form.initialLevel"
              class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-teal-500"
              data-testid="enroll-level-select"
            >
              <option v-for="lvl in schoolLevelOptions" :key="lvl.value" :value="lvl.value">
                {{ lvl.label }}
              </option>
            </select>
          </FormField>

          <!-- Academic Year -->
          <FormField label="Academic Year" required :error="errors.initialAcademicYear">
            <select
              :value="form.initialAcademicYear"
              class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-teal-500"
              data-testid="enroll-academic-year-select"
              @change="handleAcademicYearChange"
            >
              <option v-for="ay in academicYearOptions" :key="ay.value" :value="ay.value">
                {{ ay.label }}
              </option>
            </select>
          </FormField>

          <!-- Program Track -->
          <FormField label="Program / Track" :error="errors.initialProgram">
            <select
              v-model="form.initialProgram"
              class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white text-slate-800 focus:outline-none focus:ring-1 focus:ring-teal-500"
              data-testid="enroll-program-select"
            >
              <option v-for="prog in programOptions" :key="prog.value" :value="prog.value">
                {{ prog.label }}
              </option>
            </select>
          </FormField>

          <!-- Initial Cohort (text) -->
          <FormField
            label="Initial Cohort / Homeroom"
            :error="errors.initialCohort"
            hint="Optional placement group (e.g. 1A, 7-A)"
            class="md:col-span-2"
          >
            <InputText
              v-model="form.initialCohort"
              type="text"
              placeholder="e.g. 1A or Cohort-2026-A"
              class="w-full !text-xs !py-1.5"
              data-testid="enroll-cohort-input"
            />
          </FormField>
        </div>
      </div>

      <!-- 2. Payment Gate (GAP-01) -->
      <div class="space-y-3 p-3.5 bg-slate-50/70 border border-slate-200 rounded-lg">
        <div class="flex items-center justify-between border-b border-slate-200 pb-2">
          <div class="flex items-center gap-1.5 font-semibold text-slate-800 text-xs">
            <CreditCard class="w-4 h-4 text-teal-600" />
            <span>Payment Clearance Gate (GAP-01)</span>
          </div>
          <!-- Real-Time Clearance Status Badge -->
          <span
            v-if="isFeeGateSettled === true"
            class="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200"
            data-testid="gate-status-pill"
          >
            <CheckCircle2 class="w-3 h-3 text-emerald-600" />
            Payment Verified &amp; Settled
          </span>
          <span
            v-else-if="isFeeGateSettled === false"
            class="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold rounded-full bg-amber-100 text-amber-800 border border-amber-200"
            data-testid="gate-status-pill"
          >
            <AlertCircle class="w-3 h-3 text-amber-600" />
            Fees Outstanding
          </span>
        </div>

        <!-- Clearance Banner: Prominent Green Clearance Badge vs Amber Warning -->
        <div
          v-if="isFeeGateSettled === true"
          class="flex items-center justify-between p-3 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-900"
          data-testid="payment-verified-badge"
        >
          <div class="flex items-center gap-2.5">
            <div class="w-7 h-7 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0">
              <CheckCircle2 class="w-4 h-4" />
            </div>
            <div>
              <span class="font-bold text-xs text-emerald-950 block">Payment Verified &amp; Settled</span>
              <p class="text-[11px] text-emerald-800">
                Mandatory enrollment fees for this candidate are verified and settled in the Finance ledger.
              </p>
            </div>
          </div>
          <div v-if="verifiedReceiptNumber" class="text-right shrink-0">
            <span class="text-[10px] uppercase font-semibold text-emerald-700 block">Receipt Reference</span>
            <span
              class="font-mono font-bold text-xs bg-emerald-100 px-2 py-0.5 rounded text-emerald-900 border border-emerald-300"
              data-testid="verified-receipt-reference"
            >
              {{ verifiedReceiptNumber }}
            </span>
          </div>
        </div>

        <div
          v-else-if="isFeeGateSettled === false"
          class="flex items-start gap-2.5 p-3 rounded-lg bg-amber-50 border border-amber-200 text-amber-900"
          data-testid="payment-outstanding-warning"
        >
          <AlertCircle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
          <div class="space-y-1">
            <span class="font-bold text-xs text-amber-950 block">
              Enrollment Fees Outstanding: {{ formatIdr(outstandingMandatoryFee) }}
            </span>
            <p class="text-[11px] text-amber-800 leading-relaxed">
              Mandatory enrollment obligations are unsettled. Please verify payment intake in
              <a
                href="/finance/payments"
                target="_blank"
                class="font-semibold underline text-amber-900 hover:text-amber-950"
              >
                SCR-FIN-02: Payment Verification Queue
              </a>
              or enter an authorized administrative fee waiver justification below.
            </p>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
          <!-- Option: Payment Confirmed -->
          <label
            class="flex items-start gap-2.5 p-2.5 rounded-lg border cursor-pointer transition-colors"
            :class="!form.paymentWaived ? 'bg-teal-50/60 border-teal-300' : 'bg-white border-slate-200 hover:bg-slate-50'"
          >
            <input
              type="radio"
              name="paymentMode"
              :checked="!form.paymentWaived"
              class="mt-0.5 text-teal-600 focus:ring-teal-500"
              data-testid="payment-mode-confirmed"
              @change="form.paymentWaived = false"
            />
            <div class="space-y-0.5">
              <span class="font-medium text-slate-800 block">Payment Confirmed</span>
              <p class="text-[11px] text-slate-500 leading-tight">
                Mandatory enrollment fee received via bank transfer or direct cashier receipt.
              </p>
            </div>
          </label>

          <!-- Option: Authorized Fee Waiver -->
          <label
            class="flex items-start gap-2.5 p-2.5 rounded-lg border cursor-pointer transition-colors"
            :class="form.paymentWaived ? 'bg-amber-50/60 border-amber-300' : 'bg-white border-slate-200 hover:bg-slate-50'"
          >
            <input
              type="radio"
              name="paymentMode"
              :checked="form.paymentWaived"
              class="mt-0.5 text-amber-600 focus:ring-amber-500"
              data-testid="payment-mode-waived"
              @change="form.paymentWaived = true"
            />
            <div class="space-y-0.5">
              <span class="font-medium text-slate-800 block">Authorized Fee Waiver</span>
              <p class="text-[11px] text-slate-500 leading-tight">
                Institutional scholarship, foundation decree, or formal tuition exemption.
              </p>
            </div>
          </label>
        </div>

        <!-- Conditional Reference Input -->
        <div v-if="!form.paymentWaived" class="pt-1">
          <FormField
            label="Payment Reference / Receipt #"
            required
            :error="errors.paymentReference"
            hint="Bank transfer reference ID, teller slip number, or cashier receipt #"
          >
            <InputText
              v-model="form.paymentReference"
              type="text"
              placeholder="e.g. TRX-BCA-2026-98124 or Receipt #CR-2026-0042"
              class="w-full !text-xs !py-1.5 font-mono"
              data-testid="payment-reference-input"
            />
          </FormField>
        </div>

        <div v-else class="pt-1">
          <FormField
            label="Waiver Reference / Decree #"
            required
            :error="errors.waiverReference"
            hint="Decree number, foundation scholarship certificate, or board approval ref"
          >
            <InputText
              v-model="form.waiverReference"
              type="text"
              placeholder="e.g. SK-BEASISWA-2026-001 or Yayasan Decree #42"
              class="w-full !text-xs !py-1.5 font-mono"
              data-testid="waiver-reference-input"
            />
          </FormField>
        </div>
      </div>

      <!-- 3. Guardian Contact Confirmation -->
      <div class="space-y-3 p-3.5 bg-slate-50/70 border border-slate-200 rounded-lg">
        <div class="flex items-center justify-between border-b border-slate-200 pb-2">
          <div class="flex items-center gap-1.5 font-semibold text-slate-800 text-xs">
            <Users class="w-4 h-4 text-teal-600" />
            <span>Guardian Contact Confirmation</span>
          </div>
          <span class="text-[11px] text-slate-500">Designate exactly one Primary Contact</span>
        </div>

        <div class="space-y-2">
          <div
            v-for="(g, idx) in form.guardians"
            :key="idx"
            class="flex items-center justify-between p-2.5 rounded-lg border transition-colors"
            :class="g.isPrimaryContact ? 'bg-teal-50/60 border-teal-300' : 'bg-white border-slate-200'"
            :data-testid="'guardian-item-' + idx"
          >
            <div class="flex items-center gap-3">
              <input
                :id="'guardian-radio-' + idx"
                type="radio"
                name="primaryGuardianRadio"
                :checked="g.isPrimaryContact"
                class="text-teal-600 focus:ring-teal-500 cursor-pointer"
                :data-testid="'primary-guardian-radio-' + idx"
                @change="setPrimaryGuardian(idx)"
              />
              <label :for="'guardian-radio-' + idx" class="cursor-pointer space-y-0.5">
                <div class="flex items-center gap-2">
                  <span class="font-medium text-slate-800">{{ g.fullName }}</span>
                  <span class="text-[10px] text-slate-500 px-1.5 py-0.2 bg-slate-100 rounded border border-slate-200">
                    {{ formatRelationship(g.relationshipType) }}
                  </span>
                </div>
                <div class="text-[11px] text-slate-500">
                  <span>{{ g.phone || 'No phone' }}</span>
                  <span v-if="g.email" class="mx-1">•</span>
                  <span v-if="g.email">{{ g.email }}</span>
                </div>
              </label>
            </div>

            <div class="shrink-0">
              <span
                v-if="g.isPrimaryContact"
                class="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-medium rounded-full bg-teal-100 text-teal-800 border border-teal-200"
              >
                <ShieldCheck class="w-3 h-3 text-teal-700" />
                Primary Contact
              </span>
            </div>
          </div>
        </div>

        <div v-if="errors.guardians" class="text-rose-600 text-[11px]">
          {{ errors.guardians }}
        </div>
      </div>

      <!-- 4. Administrative Notes -->
      <FormField label="Administrative Enrollment Notes" :error="errors.notes">
        <Textarea
          v-model="form.notes"
          rows="2"
          placeholder="Optional placement notes or special enrollment considerations..."
          class="w-full !text-xs"
          data-testid="enroll-notes-textarea"
        />
      </FormField>

      <!-- Dialog Footer -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="!text-xs cursor-pointer"
          data-testid="enroll-cancel-button"
          @click="handleClose"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="small"
          severity="success"
          :loading="enrollMutation.isPending.value"
          class="!text-xs cursor-pointer !bg-teal-600 hover:!bg-teal-700 !border-teal-600"
          data-testid="enroll-submit-button"
        >
          <UserCheck class="w-3.5 h-3.5 mr-1.5" />
          Confirm & Enroll Student
        </Button>
      </div>
    </form>
  </Dialog>
</template>
