<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Checkbox from 'primevue/checkbox';
import {
  FileEdit,
  ShieldCheck,
  Send,
  AlertCircle,
  History,
  Lock,
  ArrowRight,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { recordCorrectionSchema } from '../api/teachingSchemas';
import { useApplyRecordCorrectionMutation } from '../composables/useFacultyMutations';
import type {
  TeacherProfileDto,
  TeachingRecordCorrectionDto,
  ApplyRecordCorrectionRequest,
} from '../api/teachingTypes';

export interface RecordCorrectionModalProps {
  visible: boolean;
  teacher: TeacherProfileDto | null;
  targetTableName?: 'teacher_profiles' | 'teaching_authorizations';
  targetRecordId?: string;
  initialFieldName?: string;
  canAuthorize?: boolean;
  appendTo?: string;
}

const props = withDefaults(defineProps<RecordCorrectionModalProps>(), {
  visible: false,
  teacher: null,
  targetTableName: 'teacher_profiles',
  targetRecordId: undefined,
  initialFieldName: undefined,
  canAuthorize: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'corrected', response: TeachingRecordCorrectionDto): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Authority Role Branching (TD-06)
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const isAuthority = computed(() => {
  if (props.canAuthorize !== undefined) {
    return props.canAuthorize;
  }
  return authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE);
});

// ---------------------------------------------------------------------------
// 2. Target Tables & Field Definitions
// ---------------------------------------------------------------------------
const tableOptions = [
  {
    label: 'Teacher Profile (Identity & Affiliation)',
    value: 'teacher_profiles',
  },
  {
    label: 'Teaching Authorization (Legal Scope)',
    value: 'teaching_authorizations',
  },
];

const teacherProfileFields = [
  { label: 'Legal Full Name', value: 'full_name' },
  { label: 'Professional Display Name', value: 'display_name' },
  { label: 'National ID / Passport (NIK)', value: 'national_id_or_external_ref' },
  { label: 'HR Employee Reference', value: 'employee_ref' },
  { label: 'Campus Affiliation', value: 'campus_affiliation' },
  { label: 'Department / Faculty Area', value: 'department' },
  { label: 'Effective From Date', value: 'effective_from' },
  { label: 'Effective To Date', value: 'effective_to' },
  { label: 'Administrative Notes', value: 'notes' },
];

const authorizationFields = [
  { label: 'Scope Description', value: 'scope_description' },
  { label: 'Campus Scope Boundary', value: 'campus_scope' },
  { label: 'Educational Program Scope', value: 'program_scope' },
  { label: 'Grade Level Scope', value: 'level_scope' },
  { label: 'Delivery Mode Scope', value: 'delivery_mode_scope' },
  { label: 'Effective Start Date', value: 'effective_start_date' },
  { label: 'Effective End Date', value: 'effective_end_date' },
  { label: 'Conditions or Restrictions', value: 'conditions_or_restrictions' },
  { label: 'Authorization Notes', value: 'notes' },
];

// ---------------------------------------------------------------------------
// 3. Form Reactive State
// ---------------------------------------------------------------------------
const targetTable = ref<'teacher_profiles' | 'teaching_authorizations'>('teacher_profiles');
const selectedRecordId = ref('');
const targetField = ref('full_name');
const correctedValue = ref('');
const reasonForCorrection = ref('');
const supportingEvidence = ref('');
const downstreamImpactReviewed = ref(true);
const effectiveDateImplications = ref('');
const errors = ref<Record<string, string>>({});

// Authorization records options
const authorizationRecordOptions = computed(() => {
  if (!props.teacher?.authorizations) return [];
  return props.teacher.authorizations.map((auth) => ({
    label: `${auth.authorizationNumber} — ${auth.scopeDescription} (${auth.campusScope})`,
    value: auth.id,
  }));
});

// Dynamic field options depending on chosen table
const fieldOptions = computed(() => {
  return targetTable.value === 'teacher_profiles'
    ? teacherProfileFields
    : authorizationFields;
});

// Resolve current / original value from loaded teacher object
const resolvedOriginalValue = computed<string>(() => {
  if (!props.teacher) return '';

  if (targetTable.value === 'teacher_profiles') {
    switch (targetField.value) {
      case 'full_name':
        return props.teacher.fullName ?? '';
      case 'display_name':
        return props.teacher.displayName ?? '';
      case 'national_id_or_external_ref':
        return props.teacher.nationalIdOrExternalRef ?? '';
      case 'employee_ref':
        return props.teacher.employeeRef ?? '';
      case 'campus_affiliation':
        return props.teacher.campusAffiliation ?? '';
      case 'department':
        return props.teacher.department ?? '';
      case 'effective_from':
        return props.teacher.effectiveFrom ?? '';
      case 'effective_to':
        return props.teacher.effectiveTo ?? '';
      case 'notes':
        return props.teacher.notes ?? '';
      default:
        return '';
    }
  }

  if (targetTable.value === 'teaching_authorizations') {
    const auth = props.teacher.authorizations?.find((a) => a.id === selectedRecordId.value);
    if (!auth) return '';

    switch (targetField.value) {
      case 'scope_description':
        return auth.scopeDescription ?? '';
      case 'campus_scope':
        return auth.campusScope ?? '';
      case 'program_scope':
        return auth.programScope ?? '';
      case 'level_scope':
        return auth.levelScope ?? '';
      case 'delivery_mode_scope':
        return auth.deliveryModeScope ?? '';
      case 'effective_start_date':
        return auth.effectiveStartDate ?? '';
      case 'effective_end_date':
        return auth.effectiveEndDate ?? '';
      case 'conditions_or_restrictions':
        return auth.conditionsOrRestrictions ?? '';
      case 'notes':
        return auth.notes ?? '';
      default:
        return '';
    }
  }

  return '';
});

// Reset form when modal opens
function resetForm() {
  targetTable.value = props.targetTableName || 'teacher_profiles';
  selectedRecordId.value =
    targetTable.value === 'teacher_profiles'
      ? props.teacher?.id ?? ''
      : props.targetRecordId || authorizationRecordOptions.value[0]?.value || '';
  targetField.value =
    props.initialFieldName ||
    (targetTable.value === 'teacher_profiles' ? 'full_name' : 'scope_description');
  correctedValue.value = '';
  reasonForCorrection.value = '';
  supportingEvidence.value = '';
  downstreamImpactReviewed.value = true;
  effectiveDateImplications.value = '';
  errors.value = {};
}

watch(
  () => [props.visible, props.teacher],
  ([isOpen]) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

// Keep selected record ID valid when target table toggles
watch(targetTable, (newTable) => {
  if (newTable === 'teacher_profiles') {
    selectedRecordId.value = props.teacher?.id ?? '';
    if (!teacherProfileFields.some((f) => f.value === targetField.value)) {
      targetField.value = 'full_name';
    }
  } else {
    selectedRecordId.value = authorizationRecordOptions.value[0]?.value ?? '';
    if (!authorizationFields.some((f) => f.value === targetField.value)) {
      targetField.value = 'scope_description';
    }
  }
  correctedValue.value = '';
});

// ---------------------------------------------------------------------------
// 4. Validation via recordCorrectionSchema
// ---------------------------------------------------------------------------
function validate(): boolean {
  errors.value = {};

  const payload = {
    targetTableName: targetTable.value,
    targetRecordId: selectedRecordId.value,
    fieldName: targetField.value,
    originalValue: resolvedOriginalValue.value,
    correctedValue: correctedValue.value.trim(),
    reasonForCorrection: reasonForCorrection.value.trim(),
    supportingEvidence: supportingEvidence.value?.trim() || null,
    downstreamImpactReviewed: downstreamImpactReviewed.value,
    effectiveDateImplications: effectiveDateImplications.value?.trim() || null,
    teacherProfileId: props.teacher?.id ?? null,
  };

  const result = recordCorrectionSchema.safeParse(payload);
  if (!result.success) {
    const newErrors: Record<string, string> = {};
    for (const issue of result.error.issues) {
      const field = String(issue.path[0] ?? 'form');
      if (!newErrors[field]) {
        newErrors[field] = issue.message;
      }
    }
    errors.value = newErrors;
    return false;
  }

  return true;
}

// ---------------------------------------------------------------------------
// 5. Mutation & Submission
// ---------------------------------------------------------------------------
const applyMutation = useApplyRecordCorrectionMutation({
  onSuccess: (data) => {
    emit('corrected', data);
    handleClose();
  },
});

const isSubmitting = computed(() => applyMutation.isPending.value);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleSubmit() {
  if (!props.teacher) return;
  if (!validate()) return;

  const payload: ApplyRecordCorrectionRequest = {
    targetTableName: targetTable.value,
    targetRecordId: selectedRecordId.value,
    fieldName: targetField.value,
    originalValue: resolvedOriginalValue.value,
    correctedValue: correctedValue.value.trim(),
    reasonForCorrection: reasonForCorrection.value.trim(),
    supportingEvidence: supportingEvidence.value?.trim() || undefined,
    downstreamImpactReviewed: downstreamImpactReviewed.value,
    effectiveDateImplications: effectiveDateImplications.value?.trim() || undefined,
    teacherProfileId: props.teacher.id,
  };

  applyMutation.mutate(payload);
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :append-to="appendTo"
    :style="{ width: '42rem', maxWidth: '95vw' }"
    :closable="!isSubmitting"
    :close-on-escape="!isSubmitting"
    :dismissable-mask="!isSubmitting"
    data-testid="record-correction-modal"
    @hide="handleClose"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="w-9 h-9 rounded-lg bg-teal-50 dark:bg-teal-950 border border-teal-200 dark:border-teal-800 flex items-center justify-center text-teal-600 dark:text-teal-400 shrink-0"
        >
          <FileEdit class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-sm sm:text-base font-bold text-surface-900 dark:text-surface-100 flex items-center gap-2">
            <span>Auditable Record Correction</span>
            <span
              :class="[
                'text-[10px] px-2 py-0.5 rounded font-semibold uppercase tracking-wider',
                isAuthority
                  ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-200 border border-emerald-300 dark:border-emerald-800'
                  : 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-200 border border-amber-300 dark:border-amber-800',
              ]"
              data-testid="authority-mode-badge"
            >
              {{ isAuthority ? 'Authority Direct Apply' : 'Coordinator Request' }}
            </span>
          </h2>
          <p class="text-xs text-surface-500">
            Immutable delta correction preserving "No Silent Overwrites" protocol (TD-06).
          </p>
        </div>
      </div>
    </template>

    <div v-if="teacher" class="space-y-4 pt-2">
      <!-- Authority Protocol Guidance Alert -->
      <div
        :class="[
          'p-3 rounded-lg border text-xs flex items-start gap-2.5',
          isAuthority
            ? 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-200 dark:border-emerald-800 text-emerald-900 dark:text-emerald-200'
            : 'bg-amber-50 dark:bg-amber-950/40 border-amber-200 dark:border-amber-800 text-amber-900 dark:text-amber-200',
        ]"
        data-testid="correction-mode-guidance"
      >
        <ShieldCheck v-if="isAuthority" class="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
        <History v-else class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div class="space-y-0.5">
          <span class="font-bold block">
            {{ isAuthority ? 'Teaching Authority Direct Correction' : 'Teaching Coordinator Correction Request' }}
          </span>
          <p v-if="isAuthority">
            As a Teaching Authority, your correction will be applied immediately to the official record in an atomic transaction and permanently memorialized in the immutable audit log.
          </p>
          <p v-else>
            As a Teaching Coordinator, your amendment will be registered as a pending request awaiting formal Teaching Authority review and approval before mutating the target record.
          </p>
        </div>
      </div>

      <form
        class="space-y-4"
        data-testid="record-correction-form"
        @submit.prevent="handleSubmit"
      >
        <!-- Target Table & Target Record Selectors -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="targetTable"
            label="Target Domain Entity"
            required
            :error="errors.targetTableName"
          >
            <Select
              id="targetTable"
              v-model="targetTable"
              :options="tableOptions"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              :invalid="Boolean(errors.targetTableName)"
              data-testid="select-target-table"
            />
          </FormField>

          <FormField
            id="targetField"
            label="Field to Correct"
            required
            :error="errors.fieldName"
          >
            <Select
              id="targetField"
              v-model="targetField"
              :options="fieldOptions"
              option-label="label"
              option-value="value"
              class="w-full text-xs"
              :invalid="Boolean(errors.fieldName)"
              data-testid="select-target-field"
            />
          </FormField>
        </div>

        <!-- Target Authorization Record Selector (when table is teaching_authorizations) -->
        <FormField
          v-if="targetTable === 'teaching_authorizations'"
          id="selectedRecordId"
          label="Target Authorization Record"
          required
          :error="errors.targetRecordId"
        >
          <Select
            id="selectedRecordId"
            v-model="selectedRecordId"
            :options="authorizationRecordOptions"
            option-label="label"
            option-value="value"
            placeholder="Select authorization"
            class="w-full text-xs"
            :invalid="Boolean(errors.targetRecordId)"
            data-testid="select-target-record"
          />
        </FormField>

        <!-- Before / After Delta Display Box (TD-06) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/40 rounded-lg border border-surface-200 dark:border-surface-700/80 space-y-3">
          <div class="flex items-center justify-between text-xs font-semibold text-surface-700 dark:text-surface-300">
            <span>Audit Delta Tracking</span>
            <span class="text-[10px] text-surface-400">Strict Before / After Comparison</span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <!-- Current / Original Value Display (Read-only) -->
            <div>
              <span class="text-[11px] font-medium text-rose-600 dark:text-rose-400 block mb-1">
                Current Original Value:
              </span>
              <div
                class="p-2.5 bg-rose-50/50 dark:bg-rose-950/20 border border-rose-200 dark:border-rose-900 rounded font-mono text-xs text-surface-800 dark:text-surface-200 min-h-[2.5rem] break-all flex items-center"
                data-testid="display-original-value"
              >
                {{ resolvedOriginalValue || '(Empty / Null)' }}
              </div>
            </div>

            <!-- Corrected Value Input -->
            <FormField
              id="correctedValue"
              label="New Corrected Value"
              required
              :error="errors.correctedValue"
            >
              <InputText
                id="correctedValue"
                v-model="correctedValue"
                placeholder="Enter corrected value"
                class="w-full text-xs font-mono"
                :invalid="Boolean(errors.correctedValue)"
                data-testid="input-corrected-value"
              />
            </FormField>
          </div>
        </div>

        <!-- Mandatory Justification Reason -->
        <FormField
          id="reasonForCorrection"
          label="Reason for Correction"
          required
          help-text="Mandatory governance justification documenting why this finalized record is amended."
          :error="errors.reasonForCorrection"
        >
          <Textarea
            id="reasonForCorrection"
            v-model="reasonForCorrection"
            rows="3"
            placeholder="Explain error source (e.g. Typographical error during candidate intake, amended national identity certificate)..."
            class="w-full text-xs"
            :invalid="Boolean(errors.reasonForCorrection)"
            data-testid="textarea-correction-reason"
          />
        </FormField>

        <!-- Supporting Evidence Reference & Impact Checkbox -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="supportingEvidence"
            label="Supporting Evidence Reference"
            optional
            help-text="Document reference or ticket number (e.g. DOC-2026-992)."
            :error="errors.supportingEvidence"
          >
            <InputText
              id="supportingEvidence"
              v-model="supportingEvidence"
              placeholder="e.g. Memo No. 42/SK/2026"
              class="w-full text-xs font-mono"
              :invalid="Boolean(errors.supportingEvidence)"
              data-testid="input-supporting-evidence"
            />
          </FormField>

          <div class="flex items-center gap-2 pt-5">
            <Checkbox
              id="downstreamImpactReviewed"
              v-model="downstreamImpactReviewed"
              :binary="true"
              data-testid="checkbox-impact-reviewed"
            />
            <label
              for="downstreamImpactReviewed"
              class="text-xs text-surface-700 dark:text-surface-300 font-medium cursor-pointer"
            >
              Downstream timetable and authorization impact reviewed
            </label>
          </div>
        </div>

        <!-- Action Buttons -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-200 dark:border-surface-700">
          <Button
            type="button"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="isSubmitting"
            class="!text-xs !py-1.5 !px-3 font-medium"
            data-testid="btn-cancel-correction"
            @click="handleClose"
          >
            Cancel
          </Button>

          <Button
            type="submit"
            :severity="isAuthority ? 'primary' : 'warn'"
            size="small"
            :loading="isSubmitting"
            :disabled="isSubmitting"
            class="!text-xs !py-1.5 !px-4 !gap-1.5 font-medium"
            data-testid="btn-submit-correction"
          >
            <ShieldCheck v-if="isAuthority" class="w-4 h-4" />
            <Send v-else class="w-4 h-4" />
            <span>{{ isAuthority ? 'Apply Correction Directly' : 'Submit Correction Request' }}</span>
          </Button>
        </div>
      </form>
    </div>
  </Dialog>
</template>
