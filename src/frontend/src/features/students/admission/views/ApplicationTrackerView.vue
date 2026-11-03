<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import InputText from 'primevue/inputtext';
import DatePicker from 'primevue/datepicker';
import Button from 'primevue/button';
import ProgressBar from 'primevue/progressbar';
import {
  GraduationCap,
  Search,
  RotateCcw,
  Calendar,
  FileText,
  CheckCircle2,
  Clock,
  AlertCircle,
  XCircle,
  ArrowRight,
  Home,
  ShieldCheck,
  FilePlus,
  HelpCircle,
} from 'lucide-vue-next';
import { StatusPill, FormField, EmptyState, LoadingSpinner } from '@/shared/components';
import { formatDateOnly, formatDisplayDate, parseIsoDate } from '@/shared/utils/dateUtils';
import { trackStatusSchema } from '../schemas/admission.schema';
import { useApplicationStatusQuery } from '../composables/useAdmissionQueries';
import type { PublicDocumentStatusDto, DecisionType } from '../types/admission.types';

const route = useRoute();

// ---------------------------------------------------------------------------
// Form State & Validation
// ---------------------------------------------------------------------------
const formApplicationNumber = ref('');
const formDateOfBirth = ref<Date | null>(null);
const validationErrors = ref<{ applicationNumber?: string; dateOfBirth?: string }>({});

// Active submitted search parameters triggering TanStack Query
const activeNumber = ref<string | null>(null);
const activeDob = ref<string | null>(null);
const hasSearched = ref(false);

const isSearchActive = computed(() => Boolean(activeNumber.value && activeDob.value));

// ---------------------------------------------------------------------------
// Reactive Query Hook
// ---------------------------------------------------------------------------
const {
  data: applicationStatus,
  isLoading,
  isFetching,
  isError,
} = useApplicationStatusQuery(activeNumber, activeDob, {
  enabled: isSearchActive,
});

// ---------------------------------------------------------------------------
// Helpers & Formatters
// ---------------------------------------------------------------------------
function resolveDobString(val: Date | null | undefined): string {
  if (!val) return '';
  return formatDateOnly(val);
}

function handleSearch(): void {
  validationErrors.value = {};

  const trimmedNumber = formApplicationNumber.value.trim();
  const formattedDob = resolveDobString(formDateOfBirth.value);

  const result = trackStatusSchema.safeParse({
    applicationNumber: trimmedNumber,
    dateOfBirth: formattedDob,
  });

  if (!result.success) {
    const errors: { applicationNumber?: string; dateOfBirth?: string } = {};
    for (const issue of result.error.issues) {
      const field = issue.path[0] as 'applicationNumber' | 'dateOfBirth';
      if (!errors[field]) {
        errors[field] = issue.message;
      }
    }
    validationErrors.value = errors;
    return;
  }

  // Update query state
  activeNumber.value = result.data.applicationNumber;
  activeDob.value = result.data.dateOfBirth;
  hasSearched.value = true;
}

function handleReset(): void {
  formApplicationNumber.value = '';
  formDateOfBirth.value = null;
  validationErrors.value = {};
  activeNumber.value = null;
  activeDob.value = null;
  hasSearched.value = false;
}

// ---------------------------------------------------------------------------
// Document Progress Computation
// ---------------------------------------------------------------------------
const documentStats = computed(() => {
  const docs: PublicDocumentStatusDto[] = applicationStatus.value?.documentStatuses ?? [];
  const total = docs.length;
  const verified = docs.filter((d) => d.submissionStatus.toLowerCase() === 'verified').length;
  const submitted = docs.filter(
    (d) =>
      d.submissionStatus.toLowerCase() === 'submitted' ||
      d.submissionStatus.toLowerCase() === 'verified',
  ).length;
  const percentage = total > 0 ? Math.round((verified / total) * 100) : 0;

  return { total, verified, submitted, percentage };
});

function formatDocumentTypeName(rawType: string): string {
  const knownTypes: Record<string, string> = {
    birth_certificate: 'Birth Certificate (Akta Kelahiran)',
    family_card: 'Family Card (Kartu Keluarga)',
    previous_report: 'Previous School Academic Report / Transcript',
    medical_record: 'Health & Immunization Record',
    id_card: 'Government Identification Card (KTP / KIA)',
    photo: 'Passport Size Photograph',
  };

  if (knownTypes[rawType.toLowerCase()]) {
    return knownTypes[rawType.toLowerCase()];
  }

  return rawType
    .replace(/[_-]+/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

// ---------------------------------------------------------------------------
// Decision Notice Presentation
// ---------------------------------------------------------------------------
interface DecisionPresentation {
  title: string;
  badgeLabel: string;
  badgeVariant: 'emerald' | 'amber' | 'rose' | 'sky';
  containerClass: string;
  iconClass: string;
  message: string;
}

const decisionPresentation = computed<DecisionPresentation | null>(() => {
  const notice = applicationStatus.value?.decisionNotice;
  if (!notice) return null;

  const type = (notice.decisionType || '').toLowerCase() as DecisionType;

  switch (type) {
    case 'accepted':
      return {
        title: 'Admission Decision: Accepted',
        badgeLabel: 'Accepted',
        badgeVariant: 'emerald',
        containerClass: 'bg-emerald-50 border-emerald-300 text-emerald-950',
        iconClass: 'text-emerald-600',
        message:
          'Congratulations! Your application for admission has been officially accepted by the admissions committee. Further student enrollment instructions and onboarding schedules will be communicated by the school office.',
      };
    case 'rejected':
      return {
        title: 'Admission Decision: Application Not Accepted',
        badgeLabel: 'Not Accepted',
        badgeVariant: 'rose',
        containerClass: 'bg-rose-50 border-rose-300 text-rose-950',
        iconClass: 'text-rose-600',
        message:
          'Thank you for your interest in Elsasa SIS. At this time, the admissions committee is unable to offer an admission placement for the requested term.',
      };
    case 'waitlisted':
      return {
        title: 'Admission Decision: Placed on Waitlist',
        badgeLabel: 'Waitlisted',
        badgeVariant: 'amber',
        containerClass: 'bg-amber-50 border-amber-300 text-amber-950',
        iconClass: 'text-amber-600',
        message:
          'Your application has been placed on the official admissions waitlist. Should an enrollment seat become available, the admissions desk will contact you immediately.',
      };
    case 'deferred':
      return {
        title: 'Admission Decision: Deferred',
        badgeLabel: 'Deferred',
        badgeVariant: 'sky',
        containerClass: 'bg-sky-50 border-sky-300 text-sky-950',
        iconClass: 'text-sky-600',
        message:
          'Your admission application has been deferred to a subsequent operational period. No further action is required on your part at this time.',
      };
    case 'withdrawn':
      return {
        title: 'Admission Status: Withdrawn',
        badgeLabel: 'Withdrawn',
        badgeVariant: 'rose',
        containerClass: 'bg-slate-50 border-slate-300 text-slate-800',
        iconClass: 'text-slate-500',
        message: 'This application for admission has been formally withdrawn.',
      };
    default:
      return {
        title: `Admission Decision: ${notice.decisionType}`,
        badgeLabel: notice.decisionType,
        badgeVariant: 'sky',
        containerClass: 'bg-slate-50 border-slate-300 text-slate-800',
        iconClass: 'text-slate-600',
        message: 'Official decision notice recorded for this admission application.',
      };
  }
});

// ---------------------------------------------------------------------------
// Lifecycle Stepper Calculation
// ---------------------------------------------------------------------------
const lifecycleStage = computed<number>(() => {
  const status = (applicationStatus.value?.status || '').toLowerCase();
  if (status === 'decided') return 3;
  if (status === 'under_review') return 2;
  return 1; // 'registered'
});

// Pre-fill from query parameters when navigated with ?number=...&dob=...
onMounted(() => {
  const qNumber = (route.query.number as string)?.trim();
  const qDob = (route.query.dob as string)?.trim();

  if (qNumber) {
    formApplicationNumber.value = qNumber;
  }
  if (qDob) {
    try {
      const parsed = parseIsoDate(qDob);
      if (!Number.isNaN(parsed.getTime())) {
        formDateOfBirth.value = parsed;
      }
    } catch {
      // Ignore unparseable date query param
    }
  }
  if (qNumber && qDob) {
    handleSearch();
  }
});
</script>

<template>
  <div
    class="min-h-screen bg-slate-50 text-slate-900 flex flex-col"
    data-testid="application-tracker-view"
  >
    <!-- Public Portal Top Navigation Bar -->
    <header class="bg-elsasa-navy-900 text-white border-b border-elsasa-navy-800 sticky top-0 z-30 shadow-xs">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center text-elsasa-teal-400">
            <GraduationCap class="w-6 h-6" aria-hidden="true" />
          </div>
          <div>
            <h1 class="text-base sm:text-lg font-bold tracking-tight text-white flex items-center gap-2">
              Elsasa SIS
              <span class="text-xs font-normal text-slate-300 bg-white/10 px-2 py-0.5 rounded-full">
                Admission Portal
              </span>
            </h1>
            <p class="text-xs text-slate-300 hidden sm:block">Public Application Inquiry Desk</p>
          </div>
        </div>

        <div class="flex items-center gap-2 sm:gap-3">
          <RouterLink
            to="/admission/apply"
            class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-white bg-elsasa-teal-600 hover:bg-elsasa-teal-500 px-3 py-1.5 rounded-lg transition-colors shadow-xs"
            data-testid="nav-apply-link"
          >
            <FilePlus class="w-4 h-4" aria-hidden="true" />
            <span>New Application</span>
          </RouterLink>
          <RouterLink
            to="/"
            class="inline-flex items-center gap-1 text-xs sm:text-sm text-slate-300 hover:text-white px-2.5 py-1.5 rounded-lg transition-colors"
            data-testid="nav-home-link"
          >
            <Home class="w-4 h-4" aria-hidden="true" />
            <span class="hidden sm:inline">Home</span>
          </RouterLink>
        </div>
      </div>
    </header>

    <!-- Main Container -->
    <main class="flex-1 max-w-4xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 space-y-8">
      <!-- Page Hero Header -->
      <section class="text-center sm:text-left space-y-2">
        <div class="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-elsasa-navy-700 bg-sky-100/80 px-3 py-1 rounded-full">
          <ShieldCheck class="w-3.5 h-3.5 text-sky-700" aria-hidden="true" />
          <span>Self-Service Status Tracking</span>
        </div>
        <h2 class="text-2xl sm:text-3xl font-extrabold text-elsasa-navy-900 tracking-tight">
          Application Status Tracker
        </h2>
        <p class="text-sm sm:text-base text-slate-600 max-w-2xl">
          Enter your Application Number and Date of Birth to check real-time admission progress,
          document verification status, and view your official decision notice.
        </p>
      </section>

      <!-- Search Inquiry Form Card -->
      <section
        class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 sm:p-8 space-y-6"
        data-testid="tracker-search-card"
      >
        <div class="border-b border-slate-100 pb-4">
          <h3 class="text-base font-semibold text-slate-800 flex items-center gap-2">
            <Search class="w-4 h-4 text-elsasa-navy-900" aria-hidden="true" />
            <span>Search Application Record</span>
          </h3>
          <p class="text-xs text-slate-500 mt-1">
            Both fields are required to verify applicant identity before displaying status information.
          </p>
        </div>

        <form class="space-y-5" @submit.prevent="handleSearch">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <!-- Application Number Input -->
            <FormField
              id="tracker-application-number"
              label="Application Number"
              required
              help-text="Format: ADM-YYYY-XXXX (e.g. ADM-2026-0001)"
              :error="validationErrors.applicationNumber"
            >
              <InputText
                id="tracker-application-number"
                v-model="formApplicationNumber"
                placeholder="ADM-2026-0001"
                class="w-full text-sm uppercase tracking-wide"
                :invalid="Boolean(validationErrors.applicationNumber)"
                data-testid="application-number-input"
              />
            </FormField>

            <!-- Date of Birth Picker -->
            <FormField
              id="tracker-dob"
              label="Applicant Date of Birth"
              required
              help-text="Date of birth recorded on application"
              :error="validationErrors.dateOfBirth"
            >
              <DatePicker
                id="tracker-dob"
                v-model="formDateOfBirth"
                date-format="yy-mm-dd"
                placeholder="YYYY-MM-DD"
                show-icon
                class="w-full text-sm"
                :invalid="Boolean(validationErrors.dateOfBirth)"
                data-testid="date-of-birth-picker"
              />
            </FormField>
          </div>

          <!-- Form Buttons -->
          <div class="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-2">
            <Button
              v-if="hasSearched || formApplicationNumber || formDateOfBirth"
              type="button"
              label="Clear"
              severity="secondary"
              variant="outlined"
              class="text-sm px-4 py-2"
              data-testid="reset-btn"
              @click="handleReset"
            >
              <template #icon>
                <RotateCcw class="w-4 h-4 mr-1.5" aria-hidden="true" />
              </template>
            </Button>
            <Button
              type="submit"
              label="Check Status"
              class="bg-elsasa-navy-900 hover:bg-elsasa-navy-800 text-white text-sm px-6 py-2 shadow-xs"
              :loading="isFetching"
              data-testid="check-status-btn"
            >
              <template #icon>
                <Search class="w-4 h-4 mr-1.5" aria-hidden="true" />
              </template>
            </Button>
          </div>
        </form>
      </section>

      <!-- Loading Spinner Feedback -->
      <section
        v-if="isLoading || (isFetching && !applicationStatus)"
        class="bg-white rounded-xl border border-slate-200 p-12 text-center"
        data-testid="loading-state"
      >
        <LoadingSpinner text="Querying admission records..." size="lg" center />
      </section>

      <!-- Not Found / Error Feedback -->
      <section
        v-else-if="isError"
        class="bg-white rounded-xl border border-rose-200 shadow-sm p-8 text-center space-y-4"
        data-testid="error-state"
      >
        <div class="w-12 h-12 mx-auto rounded-full bg-rose-50 flex items-center justify-center text-rose-600">
          <AlertCircle class="w-7 h-7" aria-hidden="true" />
        </div>
        <div class="max-w-md mx-auto space-y-1">
          <h3 class="text-base font-bold text-slate-900">Application Record Not Found</h3>
          <p class="text-sm text-slate-600">
            No application was found matching number
            <span class="font-mono font-semibold text-slate-800">{{ activeNumber }}</span> and date of
            birth
            <span class="font-semibold text-slate-800">{{ activeDob }}</span
            >.
          </p>
          <p class="text-xs text-slate-400 pt-2">
            Please ensure you have entered the exact application number from your confirmation slip and
            the applicant's registered birth date.
          </p>
        </div>
        <div class="pt-2 flex justify-center gap-3">
          <Button
            label="Try Again"
            severity="secondary"
            class="text-xs"
            data-testid="try-again-btn"
            @click="formApplicationNumber = ''; formDateOfBirth = null; handleReset();"
          />
          <RouterLink
            to="/admission/apply"
            class="inline-flex items-center gap-1.5 text-xs font-medium text-white bg-elsasa-teal-600 hover:bg-elsasa-teal-500 px-3 py-2 rounded-lg transition-colors"
          >
            <FilePlus class="w-3.5 h-3.5" />
            <span>Submit New Application</span>
          </RouterLink>
        </div>
      </section>

      <!-- Search Results Presentation -->
      <div
        v-else-if="applicationStatus"
        class="space-y-6"
        data-testid="status-results"
      >
        <!-- 1. Application Overview Card -->
        <section
          class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden"
          data-testid="status-overview-card"
        >
          <!-- Header Banner -->
          <div class="bg-gradient-to-r from-elsasa-navy-900 to-slate-800 px-6 py-5 text-white flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-xs font-mono font-bold bg-white/20 text-white px-2 py-0.5 rounded tracking-wider">
                  {{ applicationStatus.applicationNumber }}
                </span>
                <span class="text-xs text-slate-300">Registered Application</span>
              </div>
              <h3 class="text-xl font-bold tracking-tight mt-1 text-white">
                {{ applicationStatus.applicantFullName }}
              </h3>
            </div>
            <div class="flex items-center gap-2">
              <StatusPill
                :status="applicationStatus.status"
                size="md"
                data-testid="application-status-pill"
              />
            </div>
          </div>

          <!-- Details Grid -->
          <div class="p-6 grid grid-cols-1 sm:grid-cols-3 gap-4 border-b border-slate-100 bg-slate-50/50">
            <div>
              <dt class="text-xs text-slate-500 font-medium">Requested Entry Level</dt>
              <dd class="text-sm font-semibold text-slate-800 mt-0.5">
                {{ applicationStatus.requestedEntryLevel }}
              </dd>
            </div>
            <div>
              <dt class="text-xs text-slate-500 font-medium">Registration Date</dt>
              <dd class="text-sm font-semibold text-slate-800 mt-0.5">
                {{ formatDisplayDate(applicationStatus.registrationDate) }}
              </dd>
            </div>
            <div>
              <dt class="text-xs text-slate-500 font-medium">Current Review Stage</dt>
              <dd class="text-sm font-semibold text-slate-800 mt-0.5 capitalize">
                {{ (applicationStatus.status || '').replace('_', ' ') }}
              </dd>
            </div>
          </div>

          <!-- Lifecycle Stepper Indicator -->
          <div class="p-6">
            <h4 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-4">
              Admission Progress Timeline
            </h4>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <!-- Step 1: Registered -->
              <div
                class="flex items-start gap-3 p-3 rounded-lg border transition-all"
                :class="lifecycleStage >= 1 ? 'bg-sky-50/70 border-sky-200' : 'bg-slate-50 border-slate-200 opacity-60'"
              >
                <div
                  class="w-7 h-7 rounded-full flex items-center justify-center shrink-0"
                  :class="lifecycleStage >= 1 ? 'bg-sky-600 text-white' : 'bg-slate-300 text-slate-600'"
                >
                  <CheckCircle2 class="w-4 h-4" />
                </div>
                <div>
                  <p class="text-xs font-semibold text-slate-800">1. Registered</p>
                  <p class="text-xs text-slate-500 mt-0.5">Application submitted</p>
                </div>
              </div>

              <!-- Step 2: Under Review -->
              <div
                class="flex items-start gap-3 p-3 rounded-lg border transition-all"
                :class="lifecycleStage >= 2 ? 'bg-amber-50/70 border-amber-200' : 'bg-slate-50 border-slate-200 opacity-60'"
              >
                <div
                  class="w-7 h-7 rounded-full flex items-center justify-center shrink-0"
                  :class="lifecycleStage >= 2 ? (lifecycleStage > 2 ? 'bg-emerald-600 text-white' : 'bg-amber-500 text-white') : 'bg-slate-300 text-slate-600'"
                >
                  <component :is="lifecycleStage > 2 ? CheckCircle2 : Clock" class="w-4 h-4" />
                </div>
                <div>
                  <p class="text-xs font-semibold text-slate-800">2. Under Review</p>
                  <p class="text-xs text-slate-500 mt-0.5">Documents & evaluation</p>
                </div>
              </div>

              <!-- Step 3: Decided -->
              <div
                class="flex items-start gap-3 p-3 rounded-lg border transition-all"
                :class="lifecycleStage >= 3 ? 'bg-emerald-50/70 border-emerald-200' : 'bg-slate-50 border-slate-200 opacity-60'"
              >
                <div
                  class="w-7 h-7 rounded-full flex items-center justify-center shrink-0"
                  :class="lifecycleStage >= 3 ? 'bg-emerald-600 text-white' : 'bg-slate-300 text-slate-600'"
                >
                  <CheckCircle2 class="w-4 h-4" />
                </div>
                <div>
                  <p class="text-xs font-semibold text-slate-800">3. Decision Issued</p>
                  <p class="text-xs text-slate-500 mt-0.5">Official notice posted</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- 2. Official Decision Notice Card (rendered if decided) -->
        <section
          v-if="decisionPresentation"
          class="rounded-xl border p-6 sm:p-8 shadow-sm transition-all"
          :class="decisionPresentation.containerClass"
          data-testid="decision-notice-card"
        >
          <div class="flex items-start gap-4">
            <div
              class="w-12 h-12 rounded-xl bg-white shadow-xs flex items-center justify-center shrink-0"
              :class="decisionPresentation.iconClass"
            >
              <CheckCircle2 v-if="decisionPresentation.badgeVariant === 'emerald'" class="w-7 h-7" />
              <XCircle v-else-if="decisionPresentation.badgeVariant === 'rose'" class="w-7 h-7" />
              <Clock v-else-if="decisionPresentation.badgeVariant === 'amber'" class="w-7 h-7" />
              <Calendar v-else class="w-7 h-7" />
            </div>

            <div class="flex-1 space-y-2">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <h3 class="text-lg font-bold tracking-tight">
                  {{ decisionPresentation.title }}
                </h3>
                <StatusPill
                  :status="applicationStatus.decisionNotice?.decisionType || 'decided'"
                  :label="decisionPresentation.badgeLabel"
                  :variant="decisionPresentation.badgeVariant"
                  size="md"
                  data-testid="decision-outcome-pill"
                />
              </div>

              <p class="text-sm leading-relaxed">
                {{ decisionPresentation.message }}
              </p>

              <div class="pt-3 border-t border-black/10 flex flex-wrap items-center gap-6 text-xs font-medium">
                <div>
                  <span class="opacity-70">Effective Date: </span>
                  <span class="font-bold">
                    {{ formatDisplayDate(applicationStatus.decisionNotice?.effectiveDate || '') }}
                  </span>
                </div>
                <div>
                  <span class="opacity-70">Application Ref: </span>
                  <span class="font-mono font-bold">{{ applicationStatus.applicationNumber }}</span>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- 3. Document Checklist Progress Card -->
        <section
          class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 sm:p-8 space-y-5"
          data-testid="document-checklist-card"
        >
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-slate-100 pb-4">
            <div>
              <h3 class="text-base font-semibold text-slate-900 flex items-center gap-2">
                <FileText class="w-4 h-4 text-elsasa-navy-900" aria-hidden="true" />
                <span>Credential Document Checklist</span>
              </h3>
              <p class="text-xs text-slate-500 mt-0.5">
                Verification status of required credentials submitted for review.
              </p>
            </div>
            <div class="text-left sm:text-right">
              <span class="text-xs font-semibold text-slate-700">
                {{ documentStats.verified }} of {{ documentStats.total }} Verified
              </span>
              <span class="text-xs text-slate-400 ml-1">({{ documentStats.submitted }} submitted)</span>
            </div>
          </div>

          <!-- Progress Bar -->
          <div class="space-y-1.5">
            <div class="flex justify-between text-xs text-slate-500 font-medium">
              <span>Document Verification Progress</span>
              <span>{{ documentStats.percentage }}% Complete</span>
            </div>
            <ProgressBar
              :value="documentStats.percentage"
              :show-value="false"
              class="h-2 rounded-full overflow-hidden bg-slate-100"
              data-testid="document-progress-bar"
            />
          </div>

          <!-- Document List Table -->
          <div
            v-if="applicationStatus.documentStatuses && applicationStatus.documentStatuses.length > 0"
            class="divide-y divide-slate-100 rounded-lg border border-slate-200 overflow-hidden"
            data-testid="document-checklist-list"
          >
            <div
              v-for="(doc, idx) in applicationStatus.documentStatuses"
              :key="idx"
              class="px-4 py-3 flex items-center justify-between gap-4 hover:bg-slate-50/50 transition-colors"
              data-testid="document-checklist-row"
            >
              <div class="flex items-center gap-3 min-w-0">
                <div class="shrink-0 text-slate-400">
                  <CheckCircle2
                    v-if="doc.submissionStatus.toLowerCase() === 'verified'"
                    class="w-5 h-5 text-emerald-600"
                  />
                  <Clock
                    v-else-if="doc.submissionStatus.toLowerCase() === 'submitted'"
                    class="w-5 h-5 text-sky-600"
                  />
                  <AlertCircle
                    v-else-if="doc.submissionStatus.toLowerCase() === 'rejected'"
                    class="w-5 h-5 text-rose-600"
                  />
                  <HelpCircle v-else class="w-5 h-5 text-slate-400" />
                </div>
                <div class="min-w-0">
                  <p class="text-sm font-medium text-slate-800 truncate">
                    {{ formatDocumentTypeName(doc.documentType) }}
                  </p>
                  <p class="text-xs text-slate-400">
                    Type: <code class="font-mono">{{ doc.documentType }}</code>
                  </p>
                </div>
              </div>

              <div class="shrink-0">
                <StatusPill
                  :status="doc.submissionStatus"
                  size="sm"
                  data-testid="document-status-pill"
                />
              </div>
            </div>
          </div>

          <!-- Empty Documents Fallback -->
          <EmptyState
            v-else
            title="No Documents Registered"
            message="No credential documents have been attached to this admission application yet."
            class="py-6"
          />
        </section>

        <!-- 4. Next Steps & Assistance Card -->
        <section class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div class="space-y-1 text-center sm:text-left">
            <h4 class="text-sm font-bold text-slate-900">Need Assistance with Your Application?</h4>
            <p class="text-xs text-slate-500">
              For questions regarding admissions deadlines or document requirements, contact our Admissions Office at
              <a href="mailto:admissions@elsasa.sch.id" class="text-elsasa-teal-600 font-medium hover:underline">
                admissions@elsasa.sch.id
              </a>
              or call (021) 555-0199.
            </p>
          </div>
          <div class="flex items-center gap-2 shrink-0">
            <RouterLink
              to="/admission/apply"
              class="inline-flex items-center gap-1.5 text-xs font-semibold text-white bg-elsasa-navy-900 hover:bg-elsasa-navy-800 px-4 py-2 rounded-lg transition-colors shadow-xs"
              data-testid="bottom-apply-btn"
            >
              <span>Submit New Application</span>
              <ArrowRight class="w-3.5 h-3.5" />
            </RouterLink>
          </div>
        </section>
      </div>

      <!-- Initial Guidance State (Before search is performed) -->
      <section
        v-else
        class="bg-white rounded-xl border border-slate-200/80 shadow-xs p-8 text-center space-y-4"
        data-testid="initial-guidance-card"
      >
        <div class="w-12 h-12 mx-auto rounded-xl bg-elsasa-navy-50 text-elsasa-navy-700 flex items-center justify-center">
          <FileText class="w-6 h-6" aria-hidden="true" />
        </div>
        <div class="max-w-md mx-auto space-y-1">
          <h3 class="text-base font-semibold text-slate-800">Ready to Track Your Application</h3>
          <p class="text-xs text-slate-500">
            Locate your Application Number from your submission confirmation email or printed slip,
            select your registered Date of Birth, and click "Check Status".
          </p>
        </div>
      </section>
    </main>

    <!-- Public Institutional Footer -->
    <footer class="mt-auto border-t border-slate-200 bg-white py-6">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
        <p>&copy; 2026 Elsasa SIS. All rights reserved. &bull; Institutional Admission Portal</p>
        <div class="flex items-center gap-4">
          <RouterLink to="/admission/apply" class="hover:text-slate-800 transition-colors">
            Application Portal
          </RouterLink>
          <RouterLink to="/" class="hover:text-slate-800 transition-colors">
            School Homepage
          </RouterLink>
          <RouterLink to="/auth/login" class="hover:text-slate-800 transition-colors">
            Staff Sign In
          </RouterLink>
        </div>
      </div>
    </footer>
  </div>
</template>
