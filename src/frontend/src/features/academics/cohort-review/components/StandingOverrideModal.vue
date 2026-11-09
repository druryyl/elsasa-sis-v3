<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import Tag from 'primevue/tag';
import {
  AlertTriangle,
  Award,
  CheckCircle2,
  FileCheck2,
  FileText,
  GraduationCap,
  Scale,
  ShieldAlert,
  Sparkles,
  UserCheck,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  standingOverrideSchema,
  progressionOverrideSchema,
} from '../schemas/cohortReview.schema';
import type {
  AcademicStandingStatus,
  PromotionRecommendationStatus,
} from '../types';

export interface StudentReviewItem {
  studentId: string;
  studentName: string;
  studentIdentifier?: string;
  gradeLevel?: string;
  totalUnitsCount: number;
  passedUnitsCount: number;
  failedUnitsCount: number;
  attendancePercentage: number;
  // Standing
  standing: AcademicStandingStatus;
  policyReference: string;
  conditions?: string | null;
  // Progression
  recommendation: PromotionRecommendationStatus;
  fromLevel: string;
  recommendedToLevel?: string | null;
  supportingEvidence: string;
  // Exception
  isException: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
  // Flags
  isOverridden?: boolean;
  existingStandingId?: string | null;
  existingRecommendationId?: string | null;
}

export interface OverrideAppliedPayload {
  studentId: string;
  standing: AcademicStandingStatus;
  policyReference: string;
  conditions?: string | null;
  recommendation: PromotionRecommendationStatus;
  fromLevel: string;
  recommendedToLevel?: string | null;
  supportingEvidence: string;
  isException: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
}

interface Props {
  visible: boolean;
  student: StudentReviewItem | null;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'applied', payload: OverrideAppliedPayload): void;
}>();

const isVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Dropdown options
const standingOptions: Array<{ label: string; value: AcademicStandingStatus; severity: string }> = [
  { label: 'Good Standing', value: 'good_standing', severity: 'success' },
  { label: 'Conditional Standing', value: 'conditional_standing', severity: 'warn' },
  { label: 'Not in Good Standing', value: 'not_in_good_standing', severity: 'danger' },
];

const progressionOptions: Array<{ label: string; value: PromotionRecommendationStatus; severity: string }> = [
  { label: 'Promotion Eligible', value: 'promotion_eligible', severity: 'success' },
  { label: 'Conditional Promotion', value: 'conditional_promotion', severity: 'warn' },
  { label: 'Retention Recommended', value: 'retention_recommended', severity: 'danger' },
];

// Form state
const standing = ref<AcademicStandingStatus>('good_standing');
const policyReference = ref('SK-AKAD/2026/POLICY-01');
const standingConditions = ref('');

const recommendation = ref<PromotionRecommendationStatus>('promotion_eligible');
const fromLevel = ref('');
const recommendedToLevel = ref('');
const supportingEvidence = ref('');

const isException = ref(false);
const exceptionReason = ref('');
const committeeReference = ref('');

// Field errors
const errors = ref<Record<string, string>>({});

// Initialize form from student prop
watch(
  () => props.student,
  (item) => {
    errors.value = {};
    if (item) {
      standing.value = item.standing;
      policyReference.value = item.policyReference || 'SK-AKAD/2026/POLICY-01';
      standingConditions.value = item.conditions || '';

      recommendation.value = item.recommendation;
      fromLevel.value = item.fromLevel || item.gradeLevel || 'Grade 4';
      recommendedToLevel.value = item.recommendedToLevel || '';
      supportingEvidence.value =
        item.supportingEvidence ||
        `Evaluated: ${item.passedUnitsCount}/${item.totalUnitsCount} passed, ${item.attendancePercentage}% attendance.`;

      isException.value = Boolean(item.isException);
      exceptionReason.value = item.exceptionReason || '';
      committeeReference.value = item.committeeReference || (item.isException ? 'SK-DG/2026/06-01' : '');
    }
  },
  { immediate: true },
);

// Automatic destination level suggestions
watch(
  [recommendation, fromLevel],
  ([newRec, curLevel]) => {
    if (newRec === 'retention_recommended') {
      recommendedToLevel.value = curLevel;
    } else if (!recommendedToLevel.value || recommendedToLevel.value === curLevel) {
      // derive next level if formatted as "Grade X"
      const match = curLevel?.match(/Grade\s+(\d+)/i);
      if (match) {
        const nextNum = parseInt(match[1], 10) + 1;
        recommendedToLevel.value = `Grade ${nextNum}`;
      }
    }
  },
);

function handleApply() {
  errors.value = {};

  // 1. Validate Standing Override
  const standingPayload = {
    standing: standing.value,
    policyReference: policyReference.value,
    conditions: standingConditions.value ? standingConditions.value.trim() : null,
    isException: isException.value,
    exceptionReason: exceptionReason.value ? exceptionReason.value.trim() : null,
    committeeReference: committeeReference.value ? committeeReference.value.trim() : null,
  };

  const standingResult = standingOverrideSchema.safeParse(standingPayload);
  if (!standingResult.success) {
    for (const issue of standingResult.error.issues) {
      const field = issue.path[0] as string;
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
  }

  // 2. Validate Progression Override
  const progressionPayload = {
    recommendation: recommendation.value,
    fromLevel: fromLevel.value,
    recommendedToLevel: recommendedToLevel.value ? recommendedToLevel.value.trim() : null,
    conditions: standingConditions.value ? standingConditions.value.trim() : null,
    supportingEvidence: supportingEvidence.value ? supportingEvidence.value.trim() : '',
    isException: isException.value,
    exceptionReason: exceptionReason.value ? exceptionReason.value.trim() : null,
    committeeReference: committeeReference.value ? committeeReference.value.trim() : null,
  };

  const progressionResult = progressionOverrideSchema.safeParse(progressionPayload);
  if (!progressionResult.success) {
    for (const issue of progressionResult.error.issues) {
      const field = issue.path[0] as string;
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
  }

  if (Object.keys(errors.value).length > 0) {
    return;
  }

  if (!props.student) {
    return;
  }

  emit('applied', {
    studentId: props.student.studentId,
    standing: standing.value,
    policyReference: policyReference.value.trim(),
    conditions: standingConditions.value ? standingConditions.value.trim() : null,
    recommendation: recommendation.value,
    fromLevel: fromLevel.value.trim(),
    recommendedToLevel: recommendedToLevel.value ? recommendedToLevel.value.trim() : null,
    supportingEvidence: supportingEvidence.value.trim(),
    isException: isException.value,
    exceptionReason: isException.value ? exceptionReason.value.trim() : null,
    committeeReference: isException.value ? committeeReference.value.trim() : null,
  });

  isVisible.value = false;
}
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    modal
    :draggable="false"
    class="w-full max-w-2xl mx-4"
    data-testid="standing-override-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <Scale class="w-5 h-5 text-indigo-600" />
        <span class="font-bold text-base text-surface-900 dark:text-surface-100">
          Academic Standing & Progression Override (SCR-ACD-09)
        </span>
      </div>
    </template>

    <div v-if="student" class="space-y-5 pt-2">
      <!-- Student Context Summary Card -->
      <div class="bg-surface-50 dark:bg-surface-800/60 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700 flex flex-wrap items-center justify-between gap-3 text-xs">
        <div>
          <span class="font-bold text-surface-900 dark:text-surface-100 text-sm block">
            {{ student.studentName }}
          </span>
          <span v-if="student.studentIdentifier" class="text-surface-500">
            NISN: {{ student.studentIdentifier }} &bull;
          </span>
          <span class="text-surface-500 ml-1">
            Current Level: <strong class="text-surface-700 dark:text-surface-300">{{ student.gradeLevel || student.fromLevel }}</strong>
          </span>
        </div>

        <div class="flex items-center gap-3">
          <div class="text-right">
            <span class="text-[11px] text-surface-400 block">Units Passed</span>
            <span class="font-bold text-surface-700 dark:text-surface-300">
              {{ student.passedUnitsCount }} / {{ student.totalUnitsCount }}
            </span>
          </div>
          <div class="text-right">
            <span class="text-[11px] text-surface-400 block">Attendance</span>
            <span
              class="font-bold"
              :class="student.attendancePercentage < 75 ? 'text-red-600 dark:text-red-400' : 'text-emerald-600 dark:text-emerald-400'"
            >
              {{ student.attendancePercentage }}%
            </span>
          </div>
        </div>
      </div>

      <!-- Section 1: Standing Override -->
      <div class="border border-surface-200 dark:border-surface-700 rounded-xl p-4 space-y-3 bg-surface-0 dark:bg-surface-900">
        <div class="flex items-center justify-between border-b border-surface-100 dark:border-surface-800 pb-2">
          <span class="font-semibold text-xs text-surface-800 dark:text-surface-200 uppercase tracking-wider flex items-center gap-1.5">
            <GraduationCap class="w-4 h-4 text-indigo-500" />
            1. Academic Standing Determination
          </span>
          <span class="text-[11px] text-surface-400">
            Governing Policy: {{ policyReference }}
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <!-- Standing Decision -->
          <FormField label="Standing Decision" :error="errors.standing" required>
            <Select
              v-model="standing"
              :options="standingOptions"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-override-standing"
            />
          </FormField>

          <!-- Policy Reference -->
          <FormField label="Policy Reference" :error="errors.policyReference" required>
            <InputText
              v-model="policyReference"
              placeholder="e.g. SK-AKAD/2026/POLICY-01"
              class="w-full text-xs"
              data-testid="input-override-policy-ref"
            />
          </FormField>
        </div>

        <!-- Conditional Standing Requirements -->
        <FormField
          v-if="standing === 'conditional_standing'"
          label="Stipulated Conditions (Mandatory for Conditional Standing)"
          :error="errors.conditions"
          required
        >
          <Textarea
            v-model="standingConditions"
            rows="2"
            placeholder="e.g., Must complete Mathematics remediation during holiday packet and retest prior to Term 1."
            class="w-full text-xs"
            data-testid="textarea-override-conditions"
          />
        </FormField>
      </div>

      <!-- Section 2: Progression Recommendation Override -->
      <div class="border border-surface-200 dark:border-surface-700 rounded-xl p-4 space-y-3 bg-surface-0 dark:bg-surface-900">
        <div class="flex items-center justify-between border-b border-surface-100 dark:border-surface-800 pb-2">
          <span class="font-semibold text-xs text-surface-800 dark:text-surface-200 uppercase tracking-wider flex items-center gap-1.5">
            <UserCheck class="w-4 h-4 text-teal-500" />
            2. Progression Recommendation
          </span>
          <span class="text-[11px] text-surface-400">
            Academic Recommendation to Students Domain
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <!-- Progression Recommendation -->
          <FormField label="Recommendation" :error="errors.recommendation" required>
            <Select
              v-model="recommendation"
              :options="progressionOptions"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-override-recommendation"
            />
          </FormField>

          <!-- Destination Grade Level -->
          <FormField
            label="Recommended Destination Level"
            :error="errors.recommendedToLevel"
            :required="recommendation === 'promotion_eligible'"
          >
            <InputText
              v-model="recommendedToLevel"
              placeholder="e.g. Grade 5"
              class="w-full text-xs"
              data-testid="input-override-destination-level"
            />
          </FormField>
        </div>

        <!-- Supporting Evidence Summary -->
        <FormField label="Supporting Evidence Summary" :error="errors.supportingEvidence" required>
          <Textarea
            v-model="supportingEvidence"
            rows="2"
            placeholder="Narrative summary supporting this progression recommendation..."
            class="w-full text-xs"
            data-testid="textarea-override-evidence"
          />
        </FormField>
      </div>

      <!-- Section 3: Decree / Exception Governance (SK Dewan Guru) -->
      <div class="border border-amber-200 dark:border-amber-800 bg-amber-50/50 dark:bg-amber-950/20 rounded-xl p-4 space-y-3">
        <div class="flex items-center gap-2">
          <Checkbox
            v-model="isException"
            :binary="true"
            input-id="chk-is-exception"
            data-testid="checkbox-is-exception"
          />
          <label for="chk-is-exception" class="text-xs font-bold text-amber-950 dark:text-amber-200 cursor-pointer flex items-center gap-1.5">
            <AlertTriangle class="w-4 h-4 text-amber-600" />
            Apply Formal Committee Decree Override (*SK Dewan Guru* Exception)
          </label>
        </div>

        <div v-if="isException" class="space-y-3 pt-2">
          <p class="text-[11px] text-amber-800 dark:text-amber-300">
            Exceptions override standard threshold evaluation rules and are permanently bound to a ratified faculty decree or council resolution.
          </p>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <!-- Committee Reference -->
            <FormField
              label="Decree Reference Number (*SK Dewan Guru*)"
              :error="errors.committeeReference"
              required
            >
              <InputText
                v-model="committeeReference"
                placeholder="e.g. SK-DG/2026/06-01"
                class="w-full text-xs"
                data-testid="input-override-committee-ref"
              />
            </FormField>

            <!-- Exception Reason -->
            <FormField
              label="Exception Rationale (Min 5 chars)"
              :error="errors.exceptionReason"
              required
            >
              <InputText
                v-model="exceptionReason"
                placeholder="e.g. Approved condoned promotion by faculty consensus."
                class="w-full text-xs"
                data-testid="input-override-exception-reason"
              />
            </FormField>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          label="Cancel"
          variant="text"
          severity="secondary"
          size="small"
          class="text-xs"
          @click="isVisible = false"
        />
        <Button
          label="Apply Override to Preview"
          icon="pi pi-check"
          severity="primary"
          size="small"
          class="text-xs"
          @click="handleApply"
          data-testid="btn-apply-override"
        />
      </div>
    </template>
  </Dialog>
</template>
