<script setup lang="ts">
/* global navigator, setTimeout, console, File, Event, HTMLInputElement */
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  Check,
  CheckCircle2,
  Upload,
  FileText,
  Trash2,
  Copy,
  Calendar,
  User as UserIcon,
  Users,
  FileCheck,
  AlertCircle,
  Info,
  Plus,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  useActiveAdmissionPeriodsQuery,
  useRegisterApplicationMutation,
} from '../composables';
import { uploadApplicantDocument } from '../api';
import {
  wizardStep1Schema,
  wizardStep2Schema,
  wizardStep3Schema,
  type ContactFormData,
} from '../schemas';
import type {
  RegisterApplicationPayload,
  RegisterApplicationResultDto,
} from '../types';

const router = useRouter();

// ==========================================
// 1. Wizard Steps Definition
// ==========================================
const STEPS = [
  { number: 1, label: 'Period & Grade', shortLabel: 'Target' },
  { number: 2, label: 'Applicant Details', shortLabel: 'Applicant' },
  { number: 3, label: 'Family Contacts', shortLabel: 'Guardians' },
  { number: 4, label: 'Documents', shortLabel: 'Documents' },
  { number: 5, label: 'Review & Submit', shortLabel: 'Review' },
] as const;

const currentStep = ref<number>(1);
const maxStepReached = ref<number>(1);

// ==========================================
// 2. Active Admission Periods Query
// ==========================================
const {
  data: periods,
  isLoading: isLoadingPeriods,
} = useActiveAdmissionPeriodsQuery();

const periodOptions = computed(() => {
  if (!periods.value) return [];
  return periods.value.map((p) => ({
    label: `${p.name} (${p.startDate} - ${p.endDate})`,
    value: p.id,
    periodCode: p.periodCode,
  }));
});

// Standard grade options
const GRADE_OPTIONS = [
  { label: 'TK-A (Playgroup / Kindergarten A)', value: 'TK-A' },
  { label: 'TK-B (Kindergarten B)', value: 'TK-B' },
  { label: 'Grade 1 (Primary 1)', value: 'Grade 1' },
  { label: 'Grade 2 (Primary 2)', value: 'Grade 2' },
  { label: 'Grade 3 (Primary 3)', value: 'Grade 3' },
  { label: 'Grade 4 (Primary 4)', value: 'Grade 4' },
  { label: 'Grade 5 (Primary 5)', value: 'Grade 5' },
  { label: 'Grade 6 (Primary 6)', value: 'Grade 6' },
  { label: 'Grade 7 (Junior High 7)', value: 'Grade 7' },
  { label: 'Grade 8 (Junior High 8)', value: 'Grade 8' },
  { label: 'Grade 9 (Junior High 9)', value: 'Grade 9' },
  { label: 'Grade 10 (Senior High 10)', value: 'Grade 10' },
  { label: 'Grade 11 (Senior High 11)', value: 'Grade 11' },
  { label: 'Grade 12 (Senior High 12)', value: 'Grade 12' },
];

const GENDER_OPTIONS = [
  { label: 'Male (Laki-laki)', value: 'Male' },
  { label: 'Female (Perempuan)', value: 'Female' },
];

const GOV_ID_TYPES = [
  { label: 'NIK (Nomor Induk Kependudukan)', value: 'NIK' },
  { label: 'KIA (Kartu Identitas Anak)', value: 'KIA' },
  { label: 'Passport', value: 'Passport' },
  { label: 'Other', value: 'Other' },
];

const RELATIONSHIP_OPTIONS = [
  { label: 'Father (Ayah)', value: 'Father' },
  { label: 'Mother (Ibu)', value: 'Mother' },
  { label: 'Guardian (Wali)', value: 'Guardian' },
  { label: 'Other', value: 'Other' },
];

const PROGRAM_OPTIONS = [
  { label: 'Reguler (Standard Curriculum)', value: 'Reguler' },
  { label: 'Bilingual (English Immersion)', value: 'Bilingual' },
  { label: 'Tahfidz (Quranic Studies)', value: 'Tahfidz' },
];

// ==========================================
// 3. Step Form States & Validation Errors
// ==========================================

// Step 1 State: Period & Entry Level
const step1 = ref({
  admissionPeriodId: '',
  requestedEntryLevel: '',
  applicationType: 'new',
  requestedCampus: 'Main Campus',
  requestedProgram: 'Reguler',
});
const step1Errors = ref<Record<string, string>>({});

// Step 2 State: Applicant Biographical Info
const step2 = ref({
  fullName: '',
  preferredName: '',
  dateOfBirth: '',
  placeOfBirth: '',
  gender: 'Male',
  nationality: 'Indonesian',
  governmentIdType: 'NIK',
  governmentIdNumber: '',
  email: '',
  phone: '',
  address: '',
  previousSchoolName: '',
  previousSchoolAddress: '',
});
const step2Errors = ref<Record<string, string>>({});

// Step 3 State: Guardian Contacts
const contacts = ref<ContactFormData[]>([
  {
    fullName: '',
    relationship: 'Father',
    phone: '',
    email: '',
    address: '',
    isPrimary: true,
  },
]);
const step3Errors = ref<Record<string, string>>({});

// Step 4 State: Credential Documents
interface DocumentSlot {
  type: string;
  title: string;
  description: string;
  required: boolean;
}

const DOCUMENT_SLOTS: DocumentSlot[] = [
  {
    type: 'birth_certificate',
    title: 'Birth Certificate (Akta Kelahiran)',
    description: 'Government-issued birth certificate of the applicant.',
    required: false,
  },
  {
    type: 'family_card',
    title: 'Family Card (Kartu Keluarga)',
    description: 'Official family registry card (KK) showing applicant and guardians.',
    required: false,
  },
  {
    type: 'previous_school_report',
    title: 'Previous School Report (Rapor)',
    description: 'Latest academic report card or developmental assessment.',
    required: false,
  },
];

interface SelectedFileRecord {
  file: File;
  name: string;
  size: number;
}

const selectedFiles = ref<Record<string, SelectedFileRecord | null>>({
  birth_certificate: null,
  family_card: null,
  previous_school_report: null,
});
const fileErrors = ref<Record<string, string>>({});

// Step 5 State: Review & Submit
const confirmationAccepted = ref(false);
const step5Error = ref<string | null>(null);
const submissionError = ref<string | null>(null);
const isUploadingDocuments = ref(false);

// Post-submission success state
const isSubmitted = ref(false);
const submittedApplication = ref<RegisterApplicationResultDto | null>(null);
const copiedCode = ref(false);
const uploadedDocumentTypes = ref<string[]>([]);

// ==========================================
// 4. Step Navigation & Validation Methods
// ==========================================

function validateStep1(): boolean {
  step1Errors.value = {};
  const result = wizardStep1Schema.safeParse(step1.value);
  if (!result.success) {
    for (const issue of result.error.issues) {
      const key = String(issue.path[0] || 'root');
      step1Errors.value[key] = issue.message;
    }
    return false;
  }
  return true;
}

function validateStep2(): boolean {
  step2Errors.value = {};
  const result = wizardStep2Schema.safeParse(step2.value);
  if (!result.success) {
    for (const issue of result.error.issues) {
      const key = String(issue.path[0] || 'root');
      step2Errors.value[key] = issue.message;
    }
    return false;
  }
  return true;
}

function validateStep3(): boolean {
  step3Errors.value = {};
  const result = wizardStep3Schema.safeParse({ contacts: contacts.value });
  if (!result.success) {
    for (const issue of result.error.issues) {
      const path = issue.path.join('.');
      step3Errors.value[path] = issue.message;
    }
    return false;
  }
  return true;
}

function addContact(): void {
  contacts.value.push({
    fullName: '',
    relationship: 'Mother',
    phone: '',
    email: '',
    address: step2.value.address || '',
    isPrimary: false,
  });
}

function removeContact(index: number): void {
  if (contacts.value.length > 1) {
    contacts.value.splice(index, 1);
  }
}

function copyAddressToGuardian(index: number): void {
  if (contacts.value[index] && step2.value.address) {
    contacts.value[index].address = step2.value.address;
  }
}

// Document file selection handlers
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
const ALLOWED_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png'];

function handleFileChange(docType: string, event: Event): void {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  if (!file) return;

  fileErrors.value[docType] = '';

  const ext = '.' + (file.name.split('.').pop() || '').toLowerCase();
  if (!ALLOWED_EXTENSIONS.includes(ext)) {
    fileErrors.value[docType] = `Invalid format. Allowed: ${ALLOWED_EXTENSIONS.join(', ')}`;
    target.value = '';
    return;
  }

  if (file.size > MAX_FILE_SIZE) {
    fileErrors.value[docType] = 'File exceeds the 5 MB maximum size limit.';
    target.value = '';
    return;
  }

  selectedFiles.value[docType] = {
    file,
    name: file.name,
    size: file.size,
  };
}

function removeFile(docType: string): void {
  selectedFiles.value[docType] = null;
  fileErrors.value[docType] = '';
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`;
}

// Wizard Step Advancement
function goToNextStep(): void {
  if (currentStep.value === 1) {
    if (!validateStep1()) return;
    currentStep.value = 2;
  } else if (currentStep.value === 2) {
    if (!validateStep2()) return;
    currentStep.value = 3;
  } else if (currentStep.value === 3) {
    if (!validateStep3()) return;
    currentStep.value = 4;
  } else if (currentStep.value === 4) {
    currentStep.value = 5;
  }

  if (currentStep.value > maxStepReached.value) {
    maxStepReached.value = currentStep.value;
  }
}

function goToPrevStep(): void {
  if (currentStep.value > 1) {
    currentStep.value--;
  }
}

function jumpToStep(stepNumber: number): void {
  // Allow jumping to steps that were already validated/reached
  if (stepNumber <= maxStepReached.value) {
    currentStep.value = stepNumber;
  }
}

// ==========================================
// 5. Final Submission Mutation
// ==========================================
const registerMutation = useRegisterApplicationMutation();

const isSubmitting = computed(
  () => registerMutation.isPending.value || isUploadingDocuments.value,
);

const selectedPeriodLabel = computed(() => {
  const match = periodOptions.value.find((p) => p.value === step1.value.admissionPeriodId);
  return match?.label || step1.value.admissionPeriodId;
});

async function handleSubmitApplication(): Promise<void> {
  step5Error.value = null;
  submissionError.value = null;

  if (!confirmationAccepted.value) {
    step5Error.value = 'Please check the box below to certify and confirm your application details.';
    return;
  }

  const payload: RegisterApplicationPayload = {
    admissionPeriodId: step1.value.admissionPeriodId,
    requestedEntryLevel: step1.value.requestedEntryLevel,
    applicationType: step1.value.applicationType || 'new',
    requestedCampus: step1.value.requestedCampus || undefined,
    requestedProgram: step1.value.requestedProgram || undefined,
    fullName: step2.value.fullName.trim(),
    preferredName: step2.value.preferredName?.trim() || undefined,
    dateOfBirth: step2.value.dateOfBirth,
    placeOfBirth: step2.value.placeOfBirth?.trim() || undefined,
    gender: step2.value.gender || undefined,
    nationality: step2.value.nationality || 'Indonesian',
    governmentIdType: step2.value.governmentIdType || undefined,
    governmentIdNumber: step2.value.governmentIdNumber?.trim() || undefined,
    email: step2.value.email?.trim() || undefined,
    phone: step2.value.phone?.trim() || undefined,
    address: step2.value.address?.trim() || undefined,
    previousSchoolName: step2.value.previousSchoolName?.trim() || undefined,
    previousSchoolAddress: step2.value.previousSchoolAddress?.trim() || undefined,
    contacts: contacts.value.map((c) => ({
      fullName: c.fullName.trim(),
      relationship: c.relationship,
      phone: c.phone?.trim() || null,
      email: c.email?.trim() || null,
      address: c.address?.trim() || null,
      isPrimary: c.isPrimary,
    })),
    bypassWindowCheck: false,
  };

  try {
    const result = await registerMutation.mutateAsync({
      payload,
      isPublicPortal: true,
    });

    submittedApplication.value = result;

    // Upload attached credential documents
    const docKeys = Object.keys(selectedFiles.value);
    const filesToUpload = docKeys.filter((k) => selectedFiles.value[k] !== null);

    if (filesToUpload.length > 0) {
      isUploadingDocuments.value = true;
      for (const docType of filesToUpload) {
        const fileRecord = selectedFiles.value[docType];
        if (fileRecord) {
          try {
            await uploadApplicantDocument(result.id, docType, fileRecord.file);
            uploadedDocumentTypes.value.push(docType);
          } catch (uploadError) {
            console.error(`Document upload error for ${docType}:`, uploadError);
          }
        }
      }
      isUploadingDocuments.value = false;
    }

    isSubmitted.value = true;
  } catch (error: unknown) {
    isUploadingDocuments.value = false;
    if (error && typeof error === 'object' && 'response' in error) {
      const axiosErr = error as { response?: { data?: { detail?: string; title?: string } } };
      submissionError.value =
        axiosErr.response?.data?.detail ||
        axiosErr.response?.data?.title ||
        'An error occurred while submitting your application. Please check your details and try again.';
    } else if (error instanceof Error) {
      submissionError.value = error.message;
    } else {
      submissionError.value = 'Failed to submit application. Please verify your connection and try again.';
    }
  }
}

async function copyApplicationNumber(): Promise<void> {
  if (submittedApplication.value?.applicationNumber) {
    await navigator.clipboard.writeText(submittedApplication.value.applicationNumber);
    copiedCode.value = true;
    setTimeout(() => {
      copiedCode.value = false;
    }, 2500);
  }
}

function resetWizard(): void {
  currentStep.value = 1;
  maxStepReached.value = 1;
  isSubmitted.value = false;
  submittedApplication.value = null;
  uploadedDocumentTypes.value = [];
  confirmationAccepted.value = false;
  step1.value = {
    admissionPeriodId: '',
    requestedEntryLevel: '',
    applicationType: 'new',
    requestedCampus: 'Main Campus',
    requestedProgram: 'Reguler',
  };
  step2.value = {
    fullName: '',
    preferredName: '',
    dateOfBirth: '',
    placeOfBirth: '',
    gender: 'Male',
    nationality: 'Indonesian',
    governmentIdType: 'NIK',
    governmentIdNumber: '',
    email: '',
    phone: '',
    address: '',
    previousSchoolName: '',
    previousSchoolAddress: '',
  };
  contacts.value = [
    {
      fullName: '',
      relationship: 'Father',
      phone: '',
      email: '',
      address: '',
      isPrimary: true,
    },
  ];
  selectedFiles.value = {
    birth_certificate: null,
    family_card: null,
    previous_school_report: null,
  };
}

function navigateToTracker(): void {
  router.push('/admission/status');
}
</script>

<template>
  <div class="w-full" data-testid="admission-portal-wizard">
    <!-- Header title -->
    <div class="mb-6 text-center">
      <h2 class="text-xl font-bold tracking-tight text-slate-900 sm:text-2xl">
        Admission Application Portal
      </h2>
      <p class="mt-1 text-sm text-slate-500">
        Register for student intake in 5 simple steps. You will receive an official Application Number
        to track your admission progress.
      </p>
    </div>

    <!-- ==================================================== -->
    <!-- SUCCESS CONFIRMATION CARD (Post-Submission) -->
    <!-- ==================================================== -->
    <div
      v-if="isSubmitted && submittedApplication"
      class="space-y-6"
      data-testid="confirmation-card"
    >
      <div class="rounded-xl border border-teal-200 bg-teal-50/70 p-6 text-center shadow-xs">
        <div
          class="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-teal-100 text-teal-600 mb-4"
        >
          <CheckCircle2 class="h-8 w-8" />
        </div>

        <h3 class="text-xl font-bold text-slate-900">Application Submitted Successfully!</h3>
        <p class="mt-1 text-sm text-slate-600">
          Your admission dossier has been registered and is pending document review.
        </p>

        <!-- Prominent Application Number Display -->
        <div class="mt-6 rounded-lg border border-teal-300 bg-white p-4 shadow-2xs">
          <p class="text-xs font-semibold uppercase tracking-wider text-teal-700">
            Assigned Application Number
          </p>
          <div class="mt-1 flex items-center justify-center gap-3">
            <span
              class="font-mono text-2xl font-extrabold tracking-widest text-slate-900 sm:text-3xl"
              data-testid="assigned-application-number"
            >
              {{ submittedApplication.applicationNumber }}
            </span>
            <button
              type="button"
              class="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-200 focus:outline-hidden cursor-pointer"
              title="Copy Application Number"
              @click="copyApplicationNumber"
            >
              <Check v-if="copiedCode" class="h-4 w-4 text-teal-600" />
              <Copy v-else class="h-4 w-4 text-slate-500" />
              <span>{{ copiedCode ? 'Copied!' : 'Copy' }}</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Key intake summary -->
      <div class="rounded-lg border border-slate-200 bg-slate-50 p-4 text-xs space-y-2">
        <div class="flex justify-between border-b border-slate-200 pb-2">
          <span class="text-slate-500">Applicant:</span>
          <span class="font-semibold text-slate-800">{{ step2.fullName }}</span>
        </div>
        <div class="flex justify-between border-b border-slate-200 pb-2">
          <span class="text-slate-500">Target Grade Level:</span>
          <span class="font-semibold text-slate-800">{{ step1.requestedEntryLevel }}</span>
        </div>
        <div class="flex justify-between border-b border-slate-200 pb-2">
          <span class="text-slate-500">Registration Date:</span>
          <span class="font-semibold text-slate-800">{{ submittedApplication.registrationDate }}</span>
        </div>
        <div class="flex justify-between">
          <span class="text-slate-500">Documents Attached:</span>
          <span class="font-semibold text-teal-700">
            {{ uploadedDocumentTypes.length }} document(s) uploaded
          </span>
        </div>
      </div>

      <!-- Important Guidance / Next Steps -->
      <div class="rounded-lg border border-sky-200 bg-sky-50/80 p-4 text-xs text-sky-900 space-y-2">
        <div class="flex items-center gap-2 font-semibold">
          <Info class="h-4 w-4 text-sky-600" />
          <span>Next Steps & Important Instructions:</span>
        </div>
        <ol class="list-decimal pl-5 space-y-1 text-sky-800">
          <li>
            <strong>Save your Application Number</strong> ({{ submittedApplication.applicationNumber }})
            and applicant's Date of Birth ({{ step2.dateOfBirth }}).
          </li>
          <li>
            Our Admissions Committee will verify your credentials and schedule entrance diagnostic
            assessments.
          </li>
          <li>
            You can check your status, view assessment appointments, or upload pending documents at
            any time using the public Status Tracker.
          </li>
        </ol>
      </div>

      <!-- Action Buttons -->
      <div class="flex flex-col gap-3 sm:flex-row sm:justify-between pt-2">
        <Button
          label="Register Another Student"
          severity="secondary"
          variant="outlined"
          class="w-full sm:w-auto"
          @click="resetWizard"
        />
        <Button
          label="Go to Status Tracker"
          icon="pi pi-arrow-right"
          icon-pos="right"
          class="w-full sm:w-auto bg-teal-600 text-white hover:bg-teal-700 border-none"
          data-testid="btn-go-to-tracker"
          @click="navigateToTracker"
        />
      </div>
    </div>

    <!-- ==================================================== -->
    <!-- ACTIVE WIZARD (Steps 1 to 5) -->
    <!-- ==================================================== -->
    <div v-else class="space-y-6">
      <!-- Visual Step Indicator -->
      <nav aria-label="Registration Progress" class="mb-8" data-testid="step-indicator">
        <!-- Desktop / Tablet Steps -->
        <ol class="hidden sm:flex items-center justify-between w-full">
          <li
            v-for="(s, idx) in STEPS"
            :key="s.number"
            class="relative flex-1 flex flex-col items-center"
          >
            <!-- Connecting line between steps -->
            <div
              v-if="idx > 0"
              class="absolute top-4 -left-1/2 w-full h-0.5"
              :class="s.number <= currentStep ? 'bg-teal-600' : 'bg-slate-200'"
              aria-hidden="true"
            />

            <!-- Step Circle Badge -->
            <button
              type="button"
              class="relative z-10 flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold transition-all focus:outline-hidden"
              :class="[
                s.number < currentStep
                  ? 'bg-teal-600 text-white cursor-pointer hover:bg-teal-700'
                  : s.number === currentStep
                    ? 'border-2 border-teal-600 bg-white text-teal-700 shadow-xs ring-4 ring-teal-100'
                    : 'border border-slate-300 bg-slate-100 text-slate-400 cursor-not-allowed',
              ]"
              :disabled="s.number > maxStepReached"
              @click="jumpToStep(s.number)"
            >
              <Check v-if="s.number < currentStep" class="h-4 w-4" />
              <span v-else>{{ s.number }}</span>
            </button>

            <!-- Step Label -->
            <span
              class="mt-2 text-center text-xs font-medium"
              :class="s.number === currentStep ? 'text-teal-700 font-semibold' : 'text-slate-500'"
            >
              {{ s.label }}
            </span>
          </li>
        </ol>

        <!-- Mobile Step Summary Indicator -->
        <div class="sm:hidden flex flex-col gap-2">
          <div class="flex items-center justify-between text-xs">
            <span class="font-semibold text-teal-700">Step {{ currentStep }} of 5</span>
            <span class="text-slate-600 font-medium">{{ STEPS[currentStep - 1]?.label }}</span>
          </div>
          <div class="w-full bg-slate-200 h-2 rounded-full overflow-hidden">
            <div
              class="bg-teal-600 h-full transition-all duration-300"
              :style="{ width: `${(currentStep / 5) * 100}%` }"
            />
          </div>
        </div>
      </nav>

      <!-- ==================================================== -->
      <!-- STEP 1: Admission Period & Grade Level -->
      <!-- ==================================================== -->
      <section v-if="currentStep === 1" class="space-y-4" data-testid="wizard-step-1">
        <div class="border-b border-slate-200 pb-3 mb-4">
          <h3 class="text-base font-semibold text-slate-800 flex items-center gap-2">
            <Calendar class="h-4 w-4 text-teal-600" />
            Step 1: Select Admission Period & Requested Grade
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Choose the operational intake window and the grade level you are applying for.
          </p>
        </div>

        <!-- Informational banner if periods are loading or empty -->
        <div
          v-if="isLoadingPeriods"
          class="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600 flex items-center gap-2"
        >
          <span class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-teal-600 border-t-transparent" />
          <span>Loading available admission periods...</span>
        </div>

        <div
          v-else-if="periodOptions.length === 0"
          class="rounded-md border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800 flex items-start gap-2"
        >
          <AlertCircle class="h-4 w-4 shrink-0 text-amber-600 mt-0.5" />
          <div>
            <p class="font-semibold">No Open Admission Windows</p>
            <p class="mt-0.5 text-amber-700">
              There are currently no active operational periods accepting public registration. Please contact
              the school admissions office.
            </p>
          </div>
        </div>

        <!-- Admission Period Selection -->
        <FormField
          id="admission-period"
          label="Admission Period"
          required
          help-text="Official operational window for upcoming intake"
          :error="step1Errors.admissionPeriodId"
        >
          <Select
            id="admission-period"
            v-model="step1.admissionPeriodId"
            :options="periodOptions"
            option-label="label"
            option-value="value"
            placeholder="Select open admission period"
            class="w-full text-sm"
            data-testid="period-select"
          />
        </FormField>

        <!-- Requested Grade Level -->
        <FormField
          id="requested-entry-level"
          label="Requested Grade Level"
          required
          help-text="Grade or class level intended for entry"
          :error="step1Errors.requestedEntryLevel"
        >
          <Select
            id="requested-entry-level"
            v-model="step1.requestedEntryLevel"
            :options="GRADE_OPTIONS"
            option-label="label"
            option-value="value"
            placeholder="Select target grade level"
            class="w-full text-sm"
            data-testid="grade-level-select"
          />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <!-- Program Track -->
          <FormField
            id="requested-program"
            label="Curriculum / Program Track"
            optional
            :error="step1Errors.requestedProgram"
          >
            <Select
              id="requested-program"
              v-model="step1.requestedProgram"
              :options="PROGRAM_OPTIONS"
              option-label="label"
              option-value="value"
              placeholder="Select program track"
              class="w-full text-sm"
            />
          </FormField>

          <!-- Campus -->
          <FormField
            id="requested-campus"
            label="Campus Location"
            optional
            :error="step1Errors.requestedCampus"
          >
            <InputText
              id="requested-campus"
              v-model="step1.requestedCampus"
              placeholder="e.g. Main Campus"
              class="w-full text-sm"
            />
          </FormField>
        </div>
      </section>

      <!-- ==================================================== -->
      <!-- STEP 2: Applicant Biographical Information -->
      <!-- ==================================================== -->
      <section v-if="currentStep === 2" class="space-y-4" data-testid="wizard-step-2">
        <div class="border-b border-slate-200 pb-3 mb-4">
          <h3 class="text-base font-semibold text-slate-800 flex items-center gap-2">
            <UserIcon class="h-4 w-4 text-teal-600" />
            Step 2: Applicant Biographical Information
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Enter the prospective student's legal name, birth details, and residential information.
          </p>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <!-- Full Name -->
          <div class="sm:col-span-2">
            <FormField
              id="applicant-full-name"
              label="Applicant Full Legal Name"
              required
              help-text="As printed on government birth certificate"
              :error="step2Errors.fullName"
            >
              <InputText
                id="applicant-full-name"
                v-model="step2.fullName"
                placeholder="e.g. Ahmad Rizki Pratama"
                class="w-full text-sm"
                data-testid="applicant-name-input"
              />
            </FormField>
          </div>

          <!-- Date of Birth -->
          <FormField
            id="applicant-dob"
            label="Date of Birth"
            required
            help-text="Format: YYYY-MM-DD"
            :error="step2Errors.dateOfBirth"
          >
            <InputText
              id="applicant-dob"
              v-model="step2.dateOfBirth"
              type="date"
              class="w-full text-sm"
              data-testid="applicant-dob-input"
            />
          </FormField>

          <!-- Gender -->
          <FormField
            id="applicant-gender"
            label="Gender"
            optional
            :error="step2Errors.gender"
          >
            <Select
              id="applicant-gender"
              v-model="step2.gender"
              :options="GENDER_OPTIONS"
              option-label="label"
              option-value="value"
              placeholder="Select gender"
              class="w-full text-sm"
              data-testid="applicant-gender-select"
            />
          </FormField>

          <!-- Government ID Type -->
          <FormField
            id="applicant-gov-id-type"
            label="Government ID Type"
            optional
            :error="step2Errors.governmentIdType"
          >
            <Select
              id="applicant-gov-id-type"
              v-model="step2.governmentIdType"
              :options="GOV_ID_TYPES"
              option-label="label"
              option-value="value"
              class="w-full text-sm"
            />
          </FormField>

          <!-- Government ID Number (NIK) -->
          <FormField
            id="applicant-nik"
            label="Government ID / NIK"
            optional
            help-text="16-digit National ID or KIA"
            :error="step2Errors.governmentIdNumber"
          >
            <InputText
              id="applicant-nik"
              v-model="step2.governmentIdNumber"
              placeholder="e.g. 3201234567890001"
              class="w-full text-sm"
              data-testid="applicant-nik-input"
            />
          </FormField>

          <!-- Residential Address -->
          <div class="sm:col-span-2">
            <FormField
              id="applicant-address"
              label="Residential Address"
              optional
              help-text="Current street, neighborhood, city"
              :error="step2Errors.address"
            >
              <Textarea
                id="applicant-address"
                v-model="step2.address"
                rows="2"
                placeholder="e.g. Jl. Melati No. 12, Kel. Menteng, Jakarta Pusat"
                class="w-full text-sm"
              />
            </FormField>
          </div>

          <!-- Previous School Name -->
          <FormField
            id="applicant-prev-school"
            label="Previous School / Origin"
            optional
            help-text="Name of kindergarten or former school (if transferring)"
            :error="step2Errors.previousSchoolName"
          >
            <InputText
              id="applicant-prev-school"
              v-model="step2.previousSchoolName"
              placeholder="e.g. TK Al-Azhar 1"
              class="w-full text-sm"
            />
          </FormField>

          <!-- Place of Birth -->
          <FormField
            id="applicant-pob"
            label="Place of Birth"
            optional
            :error="step2Errors.placeOfBirth"
          >
            <InputText
              id="applicant-pob"
              v-model="step2.placeOfBirth"
              placeholder="e.g. Jakarta"
              class="w-full text-sm"
            />
          </FormField>
        </div>
      </section>

      <!-- ==================================================== -->
      <!-- STEP 3: Guardian & Family Contact Details -->
      <!-- ==================================================== -->
      <section v-if="currentStep === 3" class="space-y-4" data-testid="wizard-step-3">
        <div class="border-b border-slate-200 pb-3 mb-4">
          <h3 class="text-base font-semibold text-slate-800 flex items-center gap-2">
            <Users class="h-4 w-4 text-teal-600" />
            Step 3: Guardian & Family Contact Details
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Provide details for primary and secondary guardian contacts for official admission notifications.
          </p>
        </div>

        <div
          v-if="step3Errors['contacts']"
          class="rounded-md border border-rose-200 bg-rose-50 p-2.5 text-xs text-rose-700"
        >
          {{ step3Errors['contacts'] }}
        </div>

        <!-- Contacts Card List -->
        <div
          v-for="(contact, index) in contacts"
          :key="index"
          class="rounded-lg border border-slate-200 bg-white p-4 space-y-3.5 shadow-2xs relative"
        >
          <div class="flex items-center justify-between border-b border-slate-100 pb-2">
            <div class="flex items-center gap-2">
              <span
                class="inline-flex h-5 w-5 items-center justify-center rounded-full text-xs font-bold"
                :class="contact.isPrimary ? 'bg-teal-100 text-teal-800' : 'bg-slate-100 text-slate-600'"
              >
                {{ index + 1 }}
              </span>
              <span class="text-xs font-semibold text-slate-800">
                {{ contact.isPrimary ? 'Primary Guardian / Contact' : `Secondary Contact (${contact.relationship})` }}
              </span>
            </div>

            <button
              v-if="contacts.length > 1 && !contact.isPrimary"
              type="button"
              class="text-xs text-rose-600 hover:text-rose-800 flex items-center gap-1 cursor-pointer"
              @click="removeContact(index)"
            >
              <Trash2 class="h-3.5 w-3.5" />
              <span>Remove</span>
            </button>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            <!-- Full Name -->
            <FormField
              :id="`contact-name-${index}`"
              label="Guardian Full Legal Name"
              required
              :error="step3Errors[`contacts.${index}.fullName`]"
            >
              <InputText
                :id="`contact-name-${index}`"
                v-model="contact.fullName"
                placeholder="e.g. Budi Pratama"
                class="w-full text-sm"
                data-testid="guardian-name-input"
              />
            </FormField>

            <!-- Relationship -->
            <FormField
              :id="`contact-rel-${index}`"
              label="Relationship to Applicant"
              required
              :error="step3Errors[`contacts.${index}.relationship`]"
            >
              <Select
                :id="`contact-rel-${index}`"
                v-model="contact.relationship"
                :options="RELATIONSHIP_OPTIONS"
                option-label="label"
                option-value="value"
                class="w-full text-sm"
                data-testid="guardian-relationship-select"
              />
            </FormField>

            <!-- Phone -->
            <FormField
              :id="`contact-phone-${index}`"
              label="Phone / WhatsApp Number"
              help-text="Primary notification phone"
              :error="step3Errors[`contacts.${index}.phone`]"
            >
              <InputText
                :id="`contact-phone-${index}`"
                v-model="contact.phone"
                placeholder="e.g. +6281234567890"
                class="w-full text-sm"
                data-testid="guardian-phone-input"
              />
            </FormField>

            <!-- Email -->
            <FormField
              :id="`contact-email-${index}`"
              label="Email Address"
              help-text="For official admission decisions"
              :error="step3Errors[`contacts.${index}.email`]"
            >
              <InputText
                :id="`contact-email-${index}`"
                v-model="contact.email"
                type="email"
                placeholder="e.g. budi@example.com"
                class="w-full text-sm"
                data-testid="guardian-email-input"
              />
            </FormField>

            <!-- Address with copy shortcut -->
            <div class="sm:col-span-2">
              <div class="flex items-center justify-between mb-1">
                <label :for="`contact-addr-${index}`" class="text-xs font-medium text-slate-700">
                  Residential Address (Optional)
                </label>
                <button
                  v-if="step2.address"
                  type="button"
                  class="text-xs text-teal-600 hover:text-teal-800 cursor-pointer"
                  @click="copyAddressToGuardian(index)"
                >
                  Same as applicant
                </button>
              </div>
              <Textarea
                :id="`contact-addr-${index}`"
                v-model="contact.address"
                rows="2"
                placeholder="Guardian residential address"
                class="w-full text-sm"
              />
            </div>
          </div>
        </div>

        <!-- Add Another Guardian Button -->
        <button
          v-if="contacts.length < 3"
          type="button"
          class="flex items-center gap-1.5 text-xs font-semibold text-teal-700 hover:text-teal-800 cursor-pointer py-1"
          @click="addContact"
        >
          <Plus class="h-4 w-4" />
          <span>Add Another Guardian / Parent</span>
        </button>
      </section>

      <!-- ==================================================== -->
      <!-- STEP 4: Credential Document Uploads -->
      <!-- ==================================================== -->
      <section v-if="currentStep === 4" class="space-y-4" data-testid="wizard-step-4">
        <div class="border-b border-slate-200 pb-3 mb-4">
          <h3 class="text-base font-semibold text-slate-800 flex items-center gap-2">
            <Upload class="h-4 w-4 text-teal-600" />
            Step 4: Credential Document Uploads
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Attach credential documents to speed up admission verification. Supported formats: PDF, JPG, PNG (Max 5 MB each).
          </p>
        </div>

        <div class="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
          <strong>Note:</strong> Uploading documents now is recommended for faster processing. You can also upload or update documents later via the public status tracking desk.
        </div>

        <!-- Document Upload Cards -->
        <div class="space-y-3">
          <div
            v-for="slot in DOCUMENT_SLOTS"
            :key="slot.type"
            class="rounded-lg border border-slate-200 bg-white p-3.5 space-y-2 shadow-2xs"
          >
            <div class="flex items-start justify-between">
              <div>
                <h4 class="text-xs font-bold text-slate-800">{{ slot.title }}</h4>
                <p class="text-2xs text-slate-500 mt-0.5">{{ slot.description }}</p>
              </div>

              <!-- Uploaded state badge -->
              <span
                v-if="selectedFiles[slot.type]"
                class="inline-flex items-center gap-1 rounded-full bg-teal-50 px-2 py-0.5 text-2xs font-semibold text-teal-700 border border-teal-200"
              >
                <Check class="h-3 w-3" />
                Attached
              </span>
            </div>

            <!-- If File Selected: display file pill with remove action -->
            <div
              v-if="selectedFiles[slot.type]"
              class="flex items-center justify-between rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs"
            >
              <div class="flex items-center gap-2 truncate pr-2">
                <FileCheck class="h-4 w-4 text-teal-600 shrink-0" />
                <span class="font-medium text-slate-800 truncate">
                  {{ selectedFiles[slot.type]?.name }}
                </span>
                <span class="text-2xs text-slate-400">
                  ({{ formatBytes(selectedFiles[slot.type]?.size || 0) }})
                </span>
              </div>
              <button
                type="button"
                class="text-slate-400 hover:text-rose-600 cursor-pointer"
                title="Remove file"
                @click="removeFile(slot.type)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>

            <!-- If Not Selected: dropzone / file picker -->
            <div v-else class="flex items-center gap-2 pt-1">
              <label
                :for="`file-upload-${slot.type}`"
                class="inline-flex items-center gap-1.5 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 shadow-2xs hover:bg-slate-50 cursor-pointer"
              >
                <Upload class="h-3.5 w-3.5 text-slate-500" />
                <span>Choose File</span>
              </label>
              <input
                :id="`file-upload-${slot.type}`"
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                class="hidden"
                @change="handleFileChange(slot.type, $event)"
              />
              <span class="text-2xs text-slate-400">PDF, JPG, PNG up to 5MB</span>
            </div>

            <!-- Error message if file rejected -->
            <p
              v-if="fileErrors[slot.type]"
              class="text-2xs font-medium text-rose-600 flex items-center gap-1"
            >
              <AlertCircle class="h-3.5 w-3.5" />
              {{ fileErrors[slot.type] }}
            </p>
          </div>
        </div>
      </section>

      <!-- ==================================================== -->
      <!-- STEP 5: Summary Review & Confirmation -->
      <!-- ==================================================== -->
      <section v-if="currentStep === 5" class="space-y-4" data-testid="wizard-step-5">
        <div class="border-b border-slate-200 pb-3 mb-4">
          <h3 class="text-base font-semibold text-slate-800 flex items-center gap-2">
            <FileText class="h-4 w-4 text-teal-600" />
            Step 5: Summary Review & Confirmation
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            Review all entered details before officially submitting your registration.
          </p>
        </div>

        <!-- Submission Error Alert Banner -->
        <div
          v-if="submissionError"
          class="rounded-md border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800 flex items-start gap-2"
        >
          <AlertCircle class="h-4 w-4 shrink-0 text-rose-600 mt-0.5" />
          <div>
            <p class="font-semibold">Application Submission Failed</p>
            <p class="mt-0.5 text-rose-700">{{ submissionError }}</p>
          </div>
        </div>

        <!-- Summary Dossier Cards -->
        <div class="space-y-3 text-xs">
          <!-- 1. Intake Target -->
          <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-3.5 space-y-1.5">
            <div class="flex items-center justify-between text-slate-500 font-semibold uppercase tracking-wider text-2xs">
              <span>Intake Target</span>
              <button
                type="button"
                class="text-teal-600 hover:text-teal-800 font-normal cursor-pointer"
                @click="jumpToStep(1)"
              >
                Edit
              </button>
            </div>
            <div class="grid grid-cols-2 gap-2 text-slate-800 pt-1">
              <div>
                <span class="text-slate-500">Period: </span>
                <span class="font-semibold">{{ selectedPeriodLabel }}</span>
              </div>
              <div>
                <span class="text-slate-500">Target Grade: </span>
                <span class="font-semibold">{{ step1.requestedEntryLevel }}</span>
              </div>
              <div>
                <span class="text-slate-500">Program: </span>
                <span>{{ step1.requestedProgram || 'Standard' }}</span>
              </div>
              <div>
                <span class="text-slate-500">Campus: </span>
                <span>{{ step1.requestedCampus || 'Main' }}</span>
              </div>
            </div>
          </div>

          <!-- 2. Applicant Biographical Details -->
          <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-3.5 space-y-1.5">
            <div class="flex items-center justify-between text-slate-500 font-semibold uppercase tracking-wider text-2xs">
              <span>Applicant Profile</span>
              <button
                type="button"
                class="text-teal-600 hover:text-teal-800 font-normal cursor-pointer"
                @click="jumpToStep(2)"
              >
                Edit
              </button>
            </div>
            <div class="grid grid-cols-2 gap-2 text-slate-800 pt-1">
              <div>
                <span class="text-slate-500">Full Name: </span>
                <span class="font-semibold">{{ step2.fullName }}</span>
              </div>
              <div>
                <span class="text-slate-500">Date of Birth: </span>
                <span>{{ step2.dateOfBirth }}</span>
              </div>
              <div>
                <span class="text-slate-500">Gender: </span>
                <span>{{ step2.gender }}</span>
              </div>
              <div>
                <span class="text-slate-500">ID / NIK: </span>
                <span>{{ step2.governmentIdNumber || '—' }}</span>
              </div>
              <div class="col-span-2">
                <span class="text-slate-500">Address: </span>
                <span>{{ step2.address || '—' }}</span>
              </div>
              <div v-if="step2.previousSchoolName" class="col-span-2">
                <span class="text-slate-500">Origin School: </span>
                <span>{{ step2.previousSchoolName }}</span>
              </div>
            </div>
          </div>

          <!-- 3. Guardian Contacts -->
          <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-3.5 space-y-1.5">
            <div class="flex items-center justify-between text-slate-500 font-semibold uppercase tracking-wider text-2xs">
              <span>Guardian & Family Contacts</span>
              <button
                type="button"
                class="text-teal-600 hover:text-teal-800 font-normal cursor-pointer"
                @click="jumpToStep(3)"
              >
                Edit
              </button>
            </div>
            <div
              v-for="(c, cIdx) in contacts"
              :key="cIdx"
              class="border-t border-slate-200/80 pt-1.5 first:border-t-0 first:pt-0"
            >
              <div class="font-semibold text-slate-800">
                {{ c.fullName }} ({{ c.relationship }})
                <span v-if="c.isPrimary" class="text-2xs text-teal-600 font-normal ml-1">
                  [Primary]
                </span>
              </div>
              <div class="text-slate-600">
                Phone: {{ c.phone || '—' }} &bull; Email: {{ c.email || '—' }}
              </div>
            </div>
          </div>

          <!-- 4. Attached Documents -->
          <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-3.5 space-y-1.5">
            <div class="flex items-center justify-between text-slate-500 font-semibold uppercase tracking-wider text-2xs">
              <span>Attached Documents</span>
              <button
                type="button"
                class="text-teal-600 hover:text-teal-800 font-normal cursor-pointer"
                @click="jumpToStep(4)"
              >
                Edit
              </button>
            </div>
            <ul class="list-disc pl-4 space-y-0.5 text-slate-700 pt-1">
              <li v-for="slot in DOCUMENT_SLOTS" :key="slot.type">
                <span>{{ slot.title }}: </span>
                <span
                  :class="selectedFiles[slot.type] ? 'font-semibold text-teal-700' : 'text-slate-400 italic'"
                >
                  {{ selectedFiles[slot.type]?.name || 'Not attached (can submit later)' }}
                </span>
              </li>
            </ul>
          </div>
        </div>

        <!-- Official Certification Checkbox -->
        <div class="pt-2">
          <div class="flex items-start gap-2.5 rounded-lg border border-teal-200 bg-teal-50/60 p-3.5">
            <Checkbox
              id="confirm-application"
              v-model="confirmationAccepted"
              :binary="true"
              class="mt-0.5"
            />
            <label for="confirm-application" class="text-xs text-slate-800 leading-relaxed cursor-pointer">
              <strong>Applicant Certification:</strong> I hereby certify that all information and
              credential documents provided in this registration form are truthful, complete, and accurate.
              I agree to abide by the admissions policies and entrance assessment procedures of Elsasa SIS.
            </label>
          </div>
          <p v-if="step5Error" class="mt-1.5 text-xs font-medium text-rose-600">
            {{ step5Error }}
          </p>
        </div>
      </section>

      <!-- ==================================================== -->
      <!-- Wizard Navigation Action Bar -->
      <!-- ==================================================== -->
      <div class="flex items-center justify-between pt-4 border-t border-slate-200">
        <!-- Back Button -->
        <Button
          v-if="currentStep > 1"
          type="button"
          label="Back"
          icon="pi pi-chevron-left"
          severity="secondary"
          variant="text"
          class="text-xs text-slate-600 hover:text-slate-800"
          data-testid="btn-back"
          :disabled="isSubmitting"
          @click="goToPrevStep"
        />
        <div v-else />

        <!-- Next / Submit Button -->
        <div class="flex items-center gap-2">
          <Button
            v-if="currentStep < 5"
            type="button"
            label="Next Step"
            icon="pi pi-chevron-right"
            icon-pos="right"
            class="bg-teal-600 text-white hover:bg-teal-700 border-none text-xs px-4 py-2"
            data-testid="btn-next"
            @click="goToNextStep"
          />

          <Button
            v-else
            type="button"
            :label="isSubmitting ? 'Submitting Application...' : 'Submit Application'"
            icon="pi pi-check"
            class="bg-teal-600 text-white hover:bg-teal-700 border-none text-xs px-5 py-2"
            data-testid="btn-submit"
            :loading="isSubmitting"
            :disabled="isSubmitting || !confirmationAccepted"
            @click="handleSubmitApplication"
          />
        </div>
      </div>
    </div>
  </div>
</template>
