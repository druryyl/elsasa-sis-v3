<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import DatePicker from 'primevue/datepicker';
import Dialog from 'primevue/dialog';
import {
  CalendarDays,
  Sparkles,
  ArrowRight,
  ArrowLeft,
  CheckCircle2,
  AlertTriangle,
  RotateCcw,
  FileText,
  ShieldCheck,
  Building2,
  Layers,
  DollarSign,
  Users,
  Send,
  Info,
} from 'lucide-vue-next';
import {
  PageHeader,
  ContentCard,
  LoadingSpinner,
  EmptyState,
  FormField,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import { formatIdr } from '../../shared/currency.utils';
import { useFinancePoliciesQuery } from '../../policies/composables/useFinancePolicies';
import {
  useBatchAssessPreviewMutation,
  useBatchAssessCommitMutation,
} from '../../obligations/composables/useFinanceObligations';
import { useGenerateBillMutation } from '../composables/useFinanceBilling';
import { useAcademicYearsQuery } from '@/features/operations/calendar/composables/useAcademicYearsQuery';
import { useOperationalPeriodsQuery } from '@/features/operations/calendar/composables/useOperationalPeriodsQuery';
import type { ChargeRuleDto } from '../../policies/types/policies.types';
import type {
  BatchAssessPreviewResponse,
  BatchAssessCommitResponse,
} from '../../obligations/types/obligations.types';
import BillingCycleStepper from '../components/BillingCycleStepper.vue';
import CandidatePreviewTable from '../components/CandidatePreviewTable.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Finance', to: '/finance/obligations' },
  { label: 'Billing Cycle Workspace', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canAssess = computed(() =>
  authStore.hasPermission('Permissions.Finance.Obligations.Assess'),
);

// ---------------------------------------------------------------------------
// 3. Wizard State
// ---------------------------------------------------------------------------
const currentStep = ref<number>(1);

// Step 1 Form Inputs
const selectedAcademicYearId = ref<string>('');
const selectedPeriodId = ref<string>('');
const selectedChargeRuleId = ref<string>('');
const billingCycleLabel = ref<string>('');
const selectedCampus = ref<string>('');
const selectedGrade = ref<string>('');

// Assessment Results
const previewResult = ref<BatchAssessPreviewResponse | null>(null);
const commitResult = ref<BatchAssessCommitResponse | null>(null);

// Optional Bill Generation Modal State
const isBillModalOpen = ref(false);
const billDueDate = ref<Date | null>(null);
const isGeneratingBills = ref(false);
const generatedBillsCount = ref(0);

// Validation error messages
const formErrors = ref<Record<string, string>>({});

// ---------------------------------------------------------------------------
// 4. Reactive Data Queries
// ---------------------------------------------------------------------------
// 4.1 Academic Years
const { data: academicYears, isLoading: isLoadingAcademicYears } = useAcademicYearsQuery();

// Auto-select active or first academic year when loaded
watch(
  () => academicYears.value,
  (years) => {
    if (years && years.length > 0 && !selectedAcademicYearId.value) {
      const activeYear = years.find((y) => y.status === 'active') ?? years[0];
      selectedAcademicYearId.value = activeYear.id;
    }
  },
  { immediate: true },
);

// 4.2 Operational Periods for selected Academic Year
const { data: operationalPeriods, isLoading: isLoadingPeriods } = useOperationalPeriodsQuery(
  selectedAcademicYearId,
  {
    enabled: computed(() => Boolean(selectedAcademicYearId.value)),
  },
);

// 4.3 Policies and Charge Rules
const { data: policies, isLoading: isLoadingPolicies } = useFinancePoliciesQuery();

// Flatten active charge rules from active policies
const availableChargeRules = computed<ChargeRuleDto[]>(() => {
  if (!policies.value) return [];
  const rules: ChargeRuleDto[] = [];
  for (const policy of policies.value) {
    if (policy.status === 'active' && policy.rules) {
      for (const rule of policy.rules) {
        if (rule.status === 'active') {
          rules.push(rule);
        }
      }
    }
  }
  return rules;
});

// Selected charge rule object
const selectedChargeRule = computed<ChargeRuleDto | null>(() => {
  if (!selectedChargeRuleId.value) return null;
  return availableChargeRules.value.find((r) => r.id === selectedChargeRuleId.value) ?? null;
});

// Selected period object
const selectedPeriod = computed(() => {
  if (!selectedPeriodId.value || !operationalPeriods.value) return null;
  return operationalPeriods.value.find((p) => p.id === selectedPeriodId.value) ?? null;
});

// Auto-suggest billing cycle label when rule and period change
watch(
  [selectedChargeRule, selectedPeriod],
  ([rule, period]) => {
    if (rule && period && (!billingCycleLabel.value || billingCycleLabel.value.startsWith('Batch: '))) {
      billingCycleLabel.value = `Batch: ${rule.name} — ${period.name}`;
    }
  },
);

// Campus and Grade Filter Options
const campusOptions = [
  { label: 'All Campuses', value: '' },
  { label: 'Main Campus (Kampus A)', value: 'campus-main' },
  { label: 'East Campus (Kampus B)', value: 'campus-east' },
];

const gradeOptions = [
  { label: 'All Grades', value: '' },
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

// ---------------------------------------------------------------------------
// 5. Mutations
// ---------------------------------------------------------------------------
const previewMutation = useBatchAssessPreviewMutation();
const commitMutation = useBatchAssessCommitMutation();
const generateBillMutation = useGenerateBillMutation();

// ---------------------------------------------------------------------------
// 6. Wizard Actions
// ---------------------------------------------------------------------------
function validateStep1(): boolean {
  formErrors.value = {};
  if (!selectedChargeRuleId.value) {
    formErrors.value.chargeRule = 'Please select a charge rule to assess.';
  }
  if (!selectedPeriodId.value) {
    formErrors.value.period = 'Please select an academic/operational period.';
  }
  if (!billingCycleLabel.value.trim()) {
    formErrors.value.label = 'Billing cycle label is required.';
  }
  return Object.keys(formErrors.value).length === 0;
}

async function handleGeneratePreview() {
  if (!validateStep1()) return;

  try {
    const res = await previewMutation.mutateAsync({
      chargeRuleId: selectedChargeRuleId.value,
      academicPeriodId: selectedPeriodId.value,
      billingCycleLabel: billingCycleLabel.value.trim(),
      targetCampus: selectedCampus.value ? selectedCampus.value : null,
      targetGrade: selectedGrade.value ? selectedGrade.value : null,
      mode: 'preview',
    });
    previewResult.value = res;
    currentStep.value = 2;
  } catch {
    // Toast notification is handled in mutation composable
  }
}

function handleProceedToConfirmation() {
  if (!previewResult.value || previewResult.value.eligibleCandidatesCount === 0) return;
  currentStep.value = 3;
}

async function handleCommitAssessment() {
  if (!previewResult.value) return;

  try {
    const res = await commitMutation.mutateAsync({
      chargeRuleId: selectedChargeRuleId.value,
      academicPeriodId: selectedPeriodId.value,
      billingCycleLabel: billingCycleLabel.value.trim(),
      targetCampus: selectedCampus.value ? selectedCampus.value : null,
      targetGrade: selectedGrade.value ? selectedGrade.value : null,
      mode: 'commit',
    });
    commitResult.value = res;
    currentStep.value = 4;
  } catch {
    // Toast notification is handled in mutation composable
  }
}

function handleResetWizard() {
  currentStep.value = 1;
  previewResult.value = null;
  commitResult.value = null;
  selectedChargeRuleId.value = '';
  selectedPeriodId.value = '';
  billingCycleLabel.value = '';
  selectedCampus.value = '';
  selectedGrade.value = '';
  formErrors.value = {};
  generatedBillsCount.value = 0;
}

// ---------------------------------------------------------------------------
// 7. Optional Bill Generation
// ---------------------------------------------------------------------------
function openBillGenerationModal() {
  isBillModalOpen.value = true;
  billDueDate.value = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000); // Default 14 days
}

async function handleGenerateBatchBills() {
  if (!commitResult.value || !commitResult.value.candidates) return;

  isGeneratingBills.value = true;
  generatedBillsCount.value = 0;

  // Filter eligible non-duplicate candidates that were assessed
  const eligibleCandidates = commitResult.value.candidates.filter((c) => !c.isDuplicate);
  const formattedDueDate = billDueDate.value
    ? billDueDate.value.toISOString().split('T')[0]
    : undefined;

  try {
    for (const candidate of eligibleCandidates) {
      await generateBillMutation.mutateAsync({
        studentId: candidate.studentId,
        paymentDueDate: formattedDueDate,
        notes: `Generated from billing cycle: ${billingCycleLabel.value}`,
      });
      generatedBillsCount.value++;
    }
    isBillModalOpen.value = false;
  } catch {
    // Errors handled via mutation toast
  } finally {
    isGeneratingBills.value = false;
  }
}
</script>

<template>
  <div class="space-y-6" data-testid="billing-cycle-workspace-view">
    <!-- Header -->
    <PageHeader
      title="Billing Cycle Workspace"
      description="Prepare and execute batch fee assessments with candidate preview, exception validation, and bulk obligation posting (SCR-FIN-01, FIN-WF-02, UC-FIN-02)."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <Button
          v-if="currentStep > 1 && currentStep < 4"
          variant="outlined"
          severity="secondary"
          size="small"
          @click="handleResetWizard"
          data-testid="reset-wizard-btn"
        >
          <RotateCcw class="w-4 h-4 mr-1.5" />
          <span>Reset Wizard</span>
        </Button>
      </template>
    </PageHeader>

    <!-- Guided Stepper Navigation -->
    <BillingCycleStepper
      :current-step="currentStep"
      @update:current-step="(s) => (currentStep = s)"
    />

    <!-- ===================================================================== -->
    <!-- STEP 1: Scope & Parameters Configuration                             -->
    <!-- ===================================================================== -->
    <div v-if="currentStep === 1" class="space-y-6" data-testid="step-1-container">
      <ContentCard title="Select Assessment Scope & Charge Parameters">
        <template #description>
          Configure the active fee rule, target calendar period, and optional student filters.
        </template>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 pt-2">
          <!-- Academic Year -->
          <FormField
            label="Academic Year"
            required
            help-text="Governing academic year for the billing cycle."
          >
            <Select
              v-model="selectedAcademicYearId"
              :options="academicYears || []"
              optionLabel="name"
              optionValue="id"
              placeholder="Select Academic Year"
              class="w-full text-xs"
              :loading="isLoadingAcademicYears"
              data-testid="academic-year-select"
            />
          </FormField>

          <!-- Operational Period -->
          <FormField
            label="Academic / Operational Period"
            required
            :error="formErrors.period"
            help-text="The operational billing period (term, month, or cycle)."
          >
            <Select
              v-model="selectedPeriodId"
              :options="operationalPeriods || []"
              optionLabel="name"
              optionValue="id"
              placeholder="Select Operational Period"
              class="w-full text-xs"
              :disabled="!selectedAcademicYearId"
              :loading="isLoadingPeriods"
              data-testid="operational-period-select"
            />
          </FormField>

          <!-- Charge Rule -->
          <FormField
            label="Active Charge Rule"
            required
            :error="formErrors.chargeRule"
            help-text="Fee formula and base IDR amount defined in active Financial Policies."
            class="md:col-span-2"
          >
            <Select
              v-model="selectedChargeRuleId"
              :options="availableChargeRules"
              optionValue="id"
              placeholder="Select Active Charge Rule"
              class="w-full text-xs"
              :loading="isLoadingPolicies"
              data-testid="charge-rule-select"
            >
              <template #value="{ value }">
                <div v-if="selectedChargeRule" class="flex items-center justify-between w-full">
                  <span class="font-medium text-slate-800">
                    {{ selectedChargeRule.name }} ({{ selectedChargeRule.ruleCode }})
                  </span>
                  <span class="font-mono font-semibold text-primary-700 ml-2">
                    {{ formatIdr(selectedChargeRule.baseAmount) }}
                  </span>
                </div>
                <span v-else class="text-slate-400">Select Active Charge Rule</span>
              </template>

              <template #option="{ option }">
                <div class="flex items-center justify-between w-full py-1">
                  <div>
                    <div class="font-medium text-slate-800">{{ option.name }}</div>
                    <div class="text-[11px] text-slate-400 font-mono">
                      {{ option.ruleCode }} • {{ option.chargeType }} • {{ option.recurrencePattern }}
                    </div>
                  </div>
                  <span class="font-mono font-semibold text-slate-700 ml-4">
                    {{ formatIdr(option.baseAmount) }}
                  </span>
                </div>
              </template>
            </Select>

            <!-- Selected Rule Summary Info Box -->
            <div
              v-if="selectedChargeRule"
              class="mt-2.5 p-3 rounded-lg bg-slate-50 border border-slate-200 text-xs flex flex-wrap items-center gap-x-6 gap-y-2 text-slate-600"
              data-testid="selected-rule-details"
            >
              <div>
                <span class="text-slate-400">Charge Type:</span>
                <span class="font-semibold text-slate-700 ml-1">{{ selectedChargeRule.chargeType }}</span>
              </div>
              <div>
                <span class="text-slate-400">Recurrence:</span>
                <span class="font-semibold text-slate-700 ml-1">{{ selectedChargeRule.recurrencePattern }}</span>
              </div>
              <div>
                <span class="text-slate-400">Due Rule:</span>
                <span class="font-semibold text-slate-700 ml-1 font-mono">
                  {{ selectedChargeRule.dueRuleType }} ({{ selectedChargeRule.dueRuleValue }})
                </span>
              </div>
              <div>
                <span class="text-slate-400">Mandatory:</span>
                <span class="font-semibold text-slate-700 ml-1">
                  {{ selectedChargeRule.isMandatory ? 'Yes' : 'No' }}
                </span>
              </div>
            </div>
          </FormField>

          <!-- Billing Cycle Label -->
          <FormField
            label="Billing Cycle Label"
            required
            :error="formErrors.label"
            help-text="Auditable label attached to all obligations created in this cycle."
            class="md:col-span-2"
          >
            <InputText
              v-model="billingCycleLabel"
              placeholder="e.g. Batch: SPP Bulanan - Agustus 2026"
              class="w-full text-xs"
              data-testid="billing-cycle-label-input"
            />
          </FormField>

          <!-- Optional Filter: Campus -->
          <FormField
            label="Target Campus (Optional Scope)"
            help-text="Optionally restrict batch fee assessment to a single campus."
          >
            <Select
              v-model="selectedCampus"
              :options="campusOptions"
              optionLabel="label"
              optionValue="value"
              class="w-full text-xs"
              data-testid="campus-filter-select"
            />
          </FormField>

          <!-- Optional Filter: Grade -->
          <FormField
            label="Target Grade (Optional Scope)"
            help-text="Optionally restrict batch fee assessment to a specific grade cohort."
          >
            <Select
              v-model="selectedGrade"
              :options="gradeOptions"
              optionLabel="label"
              optionValue="value"
              class="w-full text-xs"
              data-testid="grade-filter-select"
            />
          </FormField>
        </div>

        <!-- Action Row -->
        <div class="mt-8 pt-4 border-t border-slate-200 flex items-center justify-between">
          <div class="text-xs text-slate-500">
            Preview mode does not insert or alter any financial records.
          </div>
          <Button
            size="small"
            :loading="previewMutation.isPending.value"
            :disabled="!canAssess"
            @click="handleGeneratePreview"
            data-testid="evaluate-preview-btn"
          >
            <span>Evaluate & Preview Candidates</span>
            <ArrowRight class="w-4 h-4 ml-1.5" />
          </Button>
        </div>
      </ContentCard>
    </div>

    <!-- ===================================================================== -->
    <!-- STEP 2: Candidate Preview & Exception Validation                      -->
    <!-- ===================================================================== -->
    <div v-else-if="currentStep === 2" class="space-y-6" data-testid="step-2-container">
      <!-- Preview Statistics Cards -->
      <div v-if="previewResult" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Total Candidates -->
        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-600 shrink-0">
            <Users class="w-5 h-5" />
          </div>
          <div>
            <div class="text-xs text-slate-500 font-medium">Total Evaluated</div>
            <div class="text-xl font-bold text-slate-900" data-testid="stat-total-candidates">
              {{ previewResult.totalCandidatesCount }}
            </div>
          </div>
        </div>

        <!-- Eligible Candidates -->
        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-emerald-50 flex items-center justify-center text-emerald-600 shrink-0">
            <CheckCircle2 class="w-5 h-5" />
          </div>
          <div>
            <div class="text-xs text-slate-500 font-medium">Eligible Candidates</div>
            <div class="text-xl font-bold text-emerald-700" data-testid="stat-eligible-candidates">
              {{ previewResult.eligibleCandidatesCount }}
            </div>
          </div>
        </div>

        <!-- Duplicate Exclusions -->
        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-amber-50 flex items-center justify-center text-amber-600 shrink-0">
            <AlertTriangle class="w-5 h-5" />
          </div>
          <div>
            <div class="text-xs text-slate-500 font-medium">Duplicate Exclusions</div>
            <div class="text-xl font-bold text-amber-700" data-testid="stat-duplicate-count">
              {{ previewResult.duplicateCount }}
            </div>
          </div>
        </div>

        <!-- Gross Assessed Liability -->
        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center text-blue-600 shrink-0">
            <DollarSign class="w-5 h-5" />
          </div>
          <div>
            <div class="text-xs text-slate-500 font-medium">Gross Assessment</div>
            <div class="text-lg font-bold text-blue-800 font-mono" data-testid="stat-gross-amount">
              {{ formatIdr(previewResult.grossAssessedAmount) }}
            </div>
          </div>
        </div>
      </div>

      <!-- Duplicate Alert Banner -->
      <div
        v-if="previewResult && previewResult.duplicateCount > 0"
        class="p-4 rounded-xl bg-amber-50 border border-amber-200 flex items-start gap-3"
        data-testid="duplicate-warning-banner"
      >
        <AlertTriangle class="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
        <div class="text-xs text-amber-800 space-y-1">
          <div class="font-semibold text-amber-900">
            Duplicate Obligation Conflict Detected ({{ previewResult.duplicateCount }} students)
          </div>
          <div>
            {{ previewResult.duplicateCount }} student(s) already have an active or issued obligation for this
            charge rule and period. Per institutional policy (ADR-003, GAP-003), duplicates are automatically
            flagged and will be safely excluded from posting upon commit.
          </div>
        </div>
      </div>

      <!-- Candidate Preview Table -->
      <ContentCard title="Candidate Student Review & Exception Breakdown">
        <template #description>
          Review individual students evaluated for fee assessment. Duplicates are flagged and excluded.
        </template>

        <CandidatePreviewTable
          :candidates="previewResult ? previewResult.candidates : []"
          :loading="previewMutation.isPending.value"
        />

        <!-- Action Row -->
        <div class="mt-8 pt-4 border-t border-slate-200 flex items-center justify-between">
          <Button
            variant="outlined"
            severity="secondary"
            size="small"
            @click="currentStep = 1"
            data-testid="back-to-step1-btn"
          >
            <ArrowLeft class="w-4 h-4 mr-1.5" />
            <span>Back to Scope Parameters</span>
          </Button>

          <Button
            size="small"
            :disabled="!previewResult || previewResult.eligibleCandidatesCount === 0 || !canAssess"
            @click="handleProceedToConfirmation"
            data-testid="proceed-to-step3-btn"
          >
            <span>Proceed to Confirmation ({{ previewResult?.eligibleCandidatesCount || 0 }} Students)</span>
            <ArrowRight class="w-4 h-4 ml-1.5" />
          </Button>
        </div>
      </ContentCard>
    </div>

    <!-- ===================================================================== -->
    <!-- STEP 3: Confirmation & Commit                                         -->
    <!-- ===================================================================== -->
    <div v-else-if="currentStep === 3" class="space-y-6" data-testid="step-3-container">
      <ContentCard title="Confirm Batch Assessment & Obligation Commit">
        <template #description>
          Review the final batch parameters before committing financial obligations to student accounts.
        </template>

        <div class="space-y-6 pt-2">
          <!-- Summary Review Grid -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 p-4 rounded-xl bg-slate-50 border border-slate-200 text-xs">
            <div>
              <span class="text-slate-400 block font-medium">Charge Rule:</span>
              <span class="font-semibold text-slate-800 text-sm">
                {{ selectedChargeRule?.name }} ({{ selectedChargeRule?.ruleCode }})
              </span>
              <span class="text-slate-500 block">
                Base Amount: {{ formatIdr(selectedChargeRule?.baseAmount || 0) }} • {{ selectedChargeRule?.chargeType }}
              </span>
            </div>

            <div>
              <span class="text-slate-400 block font-medium">Academic Period & Label:</span>
              <span class="font-semibold text-slate-800 text-sm">
                {{ selectedPeriod?.name }} ({{ selectedPeriod?.periodCode }})
              </span>
              <span class="text-slate-500 block font-mono">
                {{ billingCycleLabel }}
              </span>
            </div>

            <div>
              <span class="text-slate-400 block font-medium">Scope Constraints:</span>
              <span class="font-semibold text-slate-800">
                Campus: {{ selectedCampus || 'All Campuses' }} • Grade: {{ selectedGrade || 'All Grades' }}
              </span>
            </div>

            <div>
              <span class="text-slate-400 block font-medium">Commit Scope Metrics:</span>
              <span class="font-bold text-emerald-700 text-sm">
                {{ previewResult?.eligibleCandidatesCount }} Eligible Students
              </span>
              <span class="text-slate-500 block">
                Excluded Duplicates: {{ previewResult?.duplicateCount }}
              </span>
            </div>

            <div class="md:col-span-2 pt-2 border-t border-slate-200 flex items-center justify-between">
              <span class="font-semibold text-slate-700">Total Obligation Liability to be Posted:</span>
              <span class="font-mono font-bold text-lg text-primary-700" data-testid="confirm-gross-amount">
                {{ formatIdr(previewResult?.grossAssessedAmount || 0) }}
              </span>
            </div>
          </div>

          <!-- Formal Commit Warning Banner -->
          <div class="p-4 rounded-xl bg-blue-50 border border-blue-200 flex items-start gap-3">
            <ShieldCheck class="w-5 h-5 text-blue-600 shrink-0 mt-0.5" />
            <div class="text-xs text-blue-900 space-y-1.5">
              <div class="font-semibold text-blue-950">
                Authoritative Financial Posting (ADR-003, ADR-004)
              </div>
              <div>
                Clicking <strong>Post Obligations (Commit)</strong> will create permanent liability records in
                <code class="px-1 py-0.5 rounded bg-blue-100 font-mono text-[11px]">finance.financial_obligations</code>,
                attach sequence-1 due schedules, and publish post-commit domain events
                (<code class="px-1 py-0.5 rounded bg-blue-100 font-mono text-[11px]">ObligationAssessedEvent</code>).
                Duplicate students will be automatically excluded.
              </div>
            </div>
          </div>

          <!-- Action Row -->
          <div class="pt-4 border-t border-slate-200 flex items-center justify-between">
            <Button
              variant="outlined"
              severity="secondary"
              size="small"
              :disabled="commitMutation.isPending.value"
              @click="currentStep = 2"
              data-testid="back-to-step2-btn"
            >
              <ArrowLeft class="w-4 h-4 mr-1.5" />
              <span>Back to Preview</span>
            </Button>

            <Button
              size="small"
              severity="primary"
              :loading="commitMutation.isPending.value"
              :disabled="!canAssess"
              @click="handleCommitAssessment"
              data-testid="commit-assessment-btn"
            >
              <ShieldCheck class="w-4 h-4 mr-1.5" />
              <span>Post Obligations (Commit)</span>
            </Button>
          </div>
        </div>
      </ContentCard>
    </div>

    <!-- ===================================================================== -->
    <!-- STEP 4: Completion Summary & Optional Bill Generation Trigger         -->
    <!-- ===================================================================== -->
    <div v-else-if="currentStep === 4" class="space-y-6" data-testid="step-4-container">
      <!-- Success Header Card -->
      <div class="p-6 rounded-2xl bg-gradient-to-r from-emerald-50 via-teal-50 to-white border border-emerald-200 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <div class="w-14 h-14 rounded-full bg-emerald-600 text-white flex items-center justify-center shadow-sm shrink-0">
            <CheckCircle2 class="w-8 h-8 stroke-[2.5]" />
          </div>
          <div>
            <h2 class="text-lg font-bold text-slate-900">
              Batch Assessment Completed Successfully!
            </h2>
            <p class="text-xs text-slate-600 mt-0.5">
              Financial obligations and due schedules have been successfully posted to student accounts.
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <Button
            size="small"
            variant="outlined"
            severity="secondary"
            @click="handleResetWizard"
            data-testid="start-new-cycle-btn"
          >
            <RotateCcw class="w-4 h-4 mr-1.5" />
            <span>New Billing Cycle</span>
          </Button>

          <Button
            size="small"
            @click="router.push('/finance/obligations')"
            data-testid="view-obligations-btn"
          >
            <FileText class="w-4 h-4 mr-1.5" />
            <span>View Obligations Ledger</span>
          </Button>
        </div>
      </div>

      <!-- Commit Metrics -->
      <div v-if="commitResult" class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
          <div class="text-xs text-slate-500 font-medium">Assessed Obligations</div>
          <div class="text-2xl font-bold text-emerald-700 mt-1" data-testid="summary-eligible-count">
            {{ commitResult.eligibleCandidatesCount }}
          </div>
          <div class="text-[11px] text-slate-400 mt-0.5">Created & linked to accounts</div>
        </div>

        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
          <div class="text-xs text-slate-500 font-medium">Total Assessed Gross Value</div>
          <div class="text-2xl font-bold text-primary-700 font-mono mt-1" data-testid="summary-gross-amount">
            {{ formatIdr(commitResult.grossAssessedAmount) }}
          </div>
          <div class="text-[11px] text-slate-400 mt-0.5">IDR liabilities generated</div>
        </div>

        <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
          <div class="text-xs text-slate-500 font-medium">Excluded Duplicates</div>
          <div class="text-2xl font-bold text-amber-700 mt-1" data-testid="summary-duplicate-count">
            {{ commitResult.duplicateCount }}
          </div>
          <div class="text-[11px] text-slate-400 mt-0.5">Skipped without mutation</div>
        </div>
      </div>

      <!-- Optional Bill Generation Callout -->
      <ContentCard title="Generate Billing Statements (Optional Next Step)">
        <template #description>
          Compile the newly posted obligations into formal billing documents (bills) for immediate family notification.
        </template>

        <div class="space-y-4 pt-1">
          <p class="text-xs text-slate-600 leading-relaxed">
            Students assessed in this cycle can now receive itemized point-in-time billing documents.
            Generating bills resolves the primary financial guarantor contact and transitions obligations to
            <code class="px-1 py-0.5 rounded bg-slate-100 font-mono text-[11px]">issued</code> status.
          </p>

          <div
            v-if="generatedBillsCount > 0"
            class="p-3 rounded-lg bg-emerald-50 border border-emerald-200 text-xs text-emerald-800 font-medium flex items-center gap-2"
            data-testid="bills-generated-badge"
          >
            <CheckCircle2 class="w-4 h-4 text-emerald-600" />
            <span>Generated {{ generatedBillsCount }} billing documents for this batch.</span>
          </div>

          <div class="flex items-center gap-3">
            <Button
              size="small"
              severity="secondary"
              @click="openBillGenerationModal"
              data-testid="trigger-bill-generation-btn"
            >
              <Send class="w-4 h-4 mr-1.5" />
              <span>Compile Batch Bills</span>
            </Button>
          </div>
        </div>
      </ContentCard>
    </div>

    <!-- ===================================================================== -->
    <!-- Optional Bill Generation Confirmation Dialog                          -->
    <!-- ===================================================================== -->
    <Dialog
      v-model:visible="isBillModalOpen"
      header="Compile Point-in-Time Batch Bills"
      :modal="true"
      class="max-w-md w-full"
      data-testid="batch-bill-dialog"
    >
      <div class="space-y-4 py-2">
        <p class="text-xs text-slate-600">
          This action compiles open obligations for all
          <strong>{{ commitResult?.eligibleCandidatesCount || 0 }}</strong>
          assessed students into formal bills, freezing line item details and resolving primary guarantor contacts.
        </p>

        <FormField label="Payment Due Date (Optional)" help-text="Due date stated on the compiled billing documents.">
          <DatePicker
            v-model="billDueDate"
            date-format="yy-mm-dd"
            show-icon
            class="w-full text-xs"
            data-testid="bill-due-date-picker"
          />
        </FormField>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-2">
          <Button
            variant="text"
            severity="secondary"
            size="small"
            :disabled="isGeneratingBills"
            @click="isBillModalOpen = false"
          >
            Cancel
          </Button>

          <Button
            size="small"
            :loading="isGeneratingBills"
            @click="handleGenerateBatchBills"
            data-testid="confirm-generate-bills-btn"
          >
            <Send class="w-4 h-4 mr-1.5" />
            <span>Confirm & Generate</span>
          </Button>
        </div>
      </template>
    </Dialog>
  </div>
</template>
