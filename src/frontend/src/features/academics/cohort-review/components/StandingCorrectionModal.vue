<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  AlertTriangle,
  CheckCircle2,
  FileEdit,
  GraduationCap,
  Scale,
  ShieldAlert,
  UserCheck,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import {
  correctStandingSchema,
  correctProgressionSchema,
} from '../schemas/cohortReview.schema';
import {
  useCorrectStandingMutation,
  useCorrectProgressionMutation,
} from '../composables/useCohortReview';
import type {
  AcademicStandingStatus,
  PromotionRecommendationStatus,
  StandingItem,
  ProgressionItem,
} from '../types';

interface Props {
  visible: boolean;
  targetType: 'standing' | 'progression';
  targetId: string;
  studentName?: string;
  studentIdentifier?: string;
  // Standing initial fields
  initialStanding?: AcademicStandingStatus;
  initialPolicyReference?: string;
  initialConditions?: string | null;
  // Progression initial fields
  initialRecommendation?: PromotionRecommendationStatus;
  initialFromLevel?: string;
  initialRecommendedToLevel?: string | null;
  initialSupportingEvidence?: string;
  // Exception decree initial fields
  initialIsException?: boolean;
  initialExceptionReason?: string | null;
  initialCommitteeReference?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  studentName: '',
  studentIdentifier: '',
  initialStanding: 'good_standing',
  initialPolicyReference: 'SK-AKAD/2026/POLICY-01',
  initialConditions: '',
  initialRecommendation: 'promotion_eligible',
  initialFromLevel: 'Grade 4',
  initialRecommendedToLevel: 'Grade 5',
  initialSupportingEvidence: '',
  initialIsException: false,
  initialExceptionReason: '',
  initialCommitteeReference: '',
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'corrected', result: StandingItem | ProgressionItem): void;
}>();

const isVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Dropdowns
const standingOptions: Array<{ label: string; value: AcademicStandingStatus }> = [
  { label: 'Good Standing', value: 'good_standing' },
  { label: 'Conditional Standing', value: 'conditional_standing' },
  { label: 'Not in Good Standing', value: 'not_in_good_standing' },
];

const progressionOptions: Array<{ label: string; value: PromotionRecommendationStatus }> = [
  { label: 'Promotion Eligible', value: 'promotion_eligible' },
  { label: 'Conditional Promotion', value: 'conditional_promotion' },
  { label: 'Retention Recommended', value: 'retention_recommended' },
];

// Form fields
const standing = ref<AcademicStandingStatus>(props.initialStanding);
const policyReference = ref(props.initialPolicyReference);
const conditions = ref(props.initialConditions || '');

const recommendation = ref<PromotionRecommendationStatus>(props.initialRecommendation);
const fromLevel = ref(props.initialFromLevel);
const recommendedToLevel = ref(props.initialRecommendedToLevel || '');
const supportingEvidence = ref(props.initialSupportingEvidence || '');

const isException = ref(props.initialIsException);
const exceptionReason = ref(props.initialExceptionReason || '');
const committeeReference = ref(props.initialCommitteeReference || '');

const correctionReason = ref('');

const errors = ref<Record<string, string>>({});

// Mutations
const { mutate: mutateCorrectStanding, isPending: isPendingStanding } = useCorrectStandingMutation({
  onSuccess: (data) => {
    emit('corrected', data);
    isVisible.value = false;
  },
});

const { mutate: mutateCorrectProgression, isPending: isPendingProgression } = useCorrectProgressionMutation({
  onSuccess: (data) => {
    emit('corrected', data);
    isVisible.value = false;
  },
});

const isSubmitting = computed(() => isPendingStanding.value || isPendingProgression.value);

watch(
  () => props.visible,
  (val) => {
    if (val) {
      errors.value = {};
      standing.value = props.initialStanding;
      policyReference.value = props.initialPolicyReference;
      conditions.value = props.initialConditions || '';
      recommendation.value = props.initialRecommendation;
      fromLevel.value = props.initialFromLevel;
      recommendedToLevel.value = props.initialRecommendedToLevel || '';
      supportingEvidence.value = props.initialSupportingEvidence || '';
      isException.value = props.initialIsException;
      exceptionReason.value = props.initialExceptionReason || '';
      committeeReference.value = props.initialCommitteeReference || '';
      correctionReason.value = '';
    }
  },
);

function handleSubmit() {
  errors.value = {};

  if (props.targetType === 'standing') {
    const payload = {
      standing: standing.value,
      policyReference: policyReference.value ? policyReference.value.trim() : undefined,
      conditions: conditions.value ? conditions.value.trim() : null,
      isException: isException.value,
      exceptionReason: isException.value ? exceptionReason.value?.trim() || null : null,
      committeeReference: isException.value ? committeeReference.value?.trim() || null : null,
      reason: correctionReason.value.trim(),
    };

    const validation = correctStandingSchema.safeParse(payload);
    if (!validation.success) {
      for (const issue of validation.error.issues) {
        const field = issue.path[0] as string;
        if (!errors.value[field]) {
          errors.value[field] = issue.message;
        }
      }
      return;
    }

    mutateCorrectStanding({
      id: props.targetId,
      request: payload,
    });
  } else {
    const payload = {
      recommendation: recommendation.value,
      fromLevel: fromLevel.value ? fromLevel.value.trim() : undefined,
      recommendedToLevel: recommendedToLevel.value ? recommendedToLevel.value.trim() : null,
      conditions: conditions.value ? conditions.value.trim() : null,
      supportingEvidence: supportingEvidence.value ? supportingEvidence.value.trim() : undefined,
      isException: isException.value,
      exceptionReason: isException.value ? exceptionReason.value?.trim() || null : null,
      committeeReference: isException.value ? committeeReference.value?.trim() || null : null,
      reason: correctionReason.value.trim(),
    };

    const validation = correctProgressionSchema.safeParse(payload);
    if (!validation.success) {
      for (const issue of validation.error.issues) {
        const field = issue.path[0] as string;
        if (!errors.value[field]) {
          errors.value[field] = issue.message;
        }
      }
      return;
    }

    mutateCorrectProgression({
      id: props.targetId,
      request: payload,
    });
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isVisible"
    modal
    :draggable="false"
    class="w-full max-w-xl mx-4"
    data-testid="standing-correction-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <FileEdit class="w-5 h-5 text-indigo-600" />
        <span class="font-bold text-base text-surface-900 dark:text-surface-100">
          Auditable Record Correction (UC-ACD-15)
        </span>
      </div>
    </template>

    <div class="space-y-4 pt-2">
      <!-- Audit Warning Banner -->
      <div class="flex items-start gap-3 p-3 bg-indigo-50/60 dark:bg-indigo-950/20 border border-indigo-200 dark:border-indigo-800 rounded-xl text-xs text-indigo-950 dark:text-indigo-200">
        <ShieldAlert class="w-4 h-4 text-indigo-600 mt-0.5 shrink-0" />
        <div>
          <span class="font-bold block">Immutable Audit Protocol</span>
          <p class="text-indigo-900 dark:text-indigo-300">
            This in-place correction serializes before/after snapshots directly into <code class="bg-indigo-100 dark:bg-indigo-900 px-1 py-0.5 rounded font-mono">academics.academic_record_corrections</code>. A descriptive justification (min 10 characters) is legally mandatory.
          </p>
        </div>
      </div>

      <!-- Student Context Summary -->
      <div v-if="studentName" class="p-3 bg-surface-50 dark:bg-surface-800/50 rounded-lg text-xs flex justify-between items-center">
        <div>
          <span class="font-bold text-surface-900 dark:text-surface-100">{{ studentName }}</span>
          <span v-if="studentIdentifier" class="text-surface-400 ml-2">NISN: {{ studentIdentifier }}</span>
        </div>
        <span class="font-semibold text-surface-600 uppercase text-[10px] tracking-wider">
          Target: {{ targetType === 'standing' ? 'Standing Determination' : 'Progression Recommendation' }}
        </span>
      </div>

      <!-- Standing Correction Fields -->
      <template v-if="targetType === 'standing'">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField label="Corrected Standing" :error="errors.standing" required>
            <Select
              v-model="standing"
              :options="standingOptions"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-correction-standing"
            />
          </FormField>

          <FormField label="Policy Reference" :error="errors.policyReference">
            <InputText
              v-model="policyReference"
              placeholder="e.g. SK-AKAD/2026/POLICY-01"
              class="w-full text-xs"
              data-testid="input-correction-policy-ref"
            />
          </FormField>
        </div>

        <FormField label="Conditions (if conditional)" :error="errors.conditions">
          <Textarea
            v-model="conditions"
            rows="2"
            placeholder="Enter stipulated conditions..."
            class="w-full text-xs"
            data-testid="textarea-correction-conditions"
          />
        </FormField>
      </template>

      <!-- Progression Correction Fields -->
      <template v-else>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField label="Corrected Recommendation" :error="errors.recommendation" required>
            <Select
              v-model="recommendation"
              :options="progressionOptions"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              data-testid="select-correction-recommendation"
            />
          </FormField>

          <FormField label="Target Level" :error="errors.recommendedToLevel">
            <InputText
              v-model="recommendedToLevel"
              placeholder="e.g. Grade 5"
              class="w-full text-xs"
              data-testid="input-correction-target-level"
            />
          </FormField>
        </div>

        <FormField label="Supporting Evidence" :error="errors.supportingEvidence">
          <Textarea
            v-model="supportingEvidence"
            rows="2"
            placeholder="Supporting evidence rationale..."
            class="w-full text-xs"
            data-testid="textarea-correction-evidence"
          />
        </FormField>
      </template>

      <!-- Exception Attributes -->
      <div class="p-3 border border-surface-200 dark:border-surface-700 rounded-lg space-y-2">
        <div class="flex items-center gap-2">
          <Checkbox
            v-model="isException"
            :binary="true"
            input-id="chk-correction-exception"
            data-testid="checkbox-correction-exception"
          />
          <label for="chk-correction-exception" class="text-xs font-semibold text-surface-800 dark:text-surface-200 cursor-pointer">
            Mark as Exception Decree (*SK Dewan Guru*)
          </label>
        </div>

        <div v-if="isException" class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
          <FormField label="Decree Reference" :error="errors.committeeReference">
            <InputText
              v-model="committeeReference"
              placeholder="e.g. SK-DG/2026/06-01"
              class="w-full text-xs"
              data-testid="input-correction-committee-ref"
            />
          </FormField>

          <FormField label="Exception Reason" :error="errors.exceptionReason">
            <InputText
              v-model="exceptionReason"
              placeholder="Exception rationale..."
              class="w-full text-xs"
              data-testid="input-correction-exception-reason"
            />
          </FormField>
        </div>
      </div>

      <!-- Mandatory Audit Reason -->
      <FormField
        label="Auditable Reason for In-Place Record Correction"
        :error="errors.reason"
        help="Mandatory minimum 10 characters for audit compliance."
        required
      >
        <Textarea
          v-model="correctionReason"
          rows="3"
          placeholder="Detailed justification explaining why this record is being corrected post-approval..."
          class="w-full text-xs"
          data-testid="textarea-correction-reason"
        />
      </FormField>
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
          label="Commit Record Correction"
          icon="pi pi-check"
          severity="danger"
          size="small"
          class="text-xs"
          :loading="isSubmitting"
          @click="handleSubmit"
          data-testid="btn-submit-correction"
        />
      </div>
    </template>
  </Dialog>
</template>
