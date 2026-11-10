<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { Award, FileText, Info } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { submitEvidenceSchema } from '../api/teachingSchemas';
import { useSubmitEvidenceMutation } from '../composables/useFacultyMutations';
import { useQualificationTypesQuery } from '../composables/useFacultyQueries';
import type {
  SubmitQualificationEvidenceRequest,
  SubmitQualificationEvidenceResponse,
  QualificationTypeResponse,
} from '../api/teachingTypes';

export interface SubmitEvidenceModalProps {
  visible: boolean;
  teacherId?: string;
  qualificationTypes?: QualificationTypeResponse[];
  appendTo?: string;
}

const props = withDefaults(defineProps<SubmitEvidenceModalProps>(), {
  visible: false,
  teacherId: undefined,
  qualificationTypes: undefined,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'submitted', response: SubmitQualificationEvidenceResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Qualification Types Query
// ---------------------------------------------------------------------------
const { data: fetchedQualTypes, isLoading: isLoadingTypes } = useQualificationTypesQuery({
  enabled: computed(() => props.visible && !props.qualificationTypes),
});

const availableQualTypes = computed<QualificationTypeResponse[]>(() => {
  return props.qualificationTypes ?? fetchedQualTypes.value ?? [];
});

const qualificationTypeOptions = computed(() =>
  availableQualTypes.value.map((type) => ({
    label: `${type.name} (${type.code})`,
    value: type.id,
    requiresRenewal: type.requiresRenewal,
    defaultValidityMonths: type.defaultValidityMonths,
  })),
);

// ---------------------------------------------------------------------------
// 2. Reactive Form State
// ---------------------------------------------------------------------------
function getTodayIso(): string {
  return new Date().toISOString().substring(0, 10);
}

const form = ref<SubmitQualificationEvidenceRequest>({
  qualificationTypeId: '',
  title: '',
  issuingAuthority: '',
  credentialIdentifier: '',
  issueDate: getTodayIso(),
  expiryDate: '',
  documentReference: '',
  restrictionsOrConditions: '',
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    qualificationTypeId: qualificationTypeOptions.value[0]?.value ?? '',
    title: '',
    issuingAuthority: '',
    credentialIdentifier: '',
    issueDate: getTodayIso(),
    expiryDate: '',
    documentReference: '',
    restrictionsOrConditions: '',
  };
  errors.value = {};
}

watch(
  () => [props.visible, qualificationTypeOptions.value],
  ([isOpen]) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 3. Mutation & Form Submission
// ---------------------------------------------------------------------------
const submitEvidenceMutation = useSubmitEvidenceMutation();

async function handleSubmit() {
  errors.value = {};

  if (!props.teacherId) {
    errors.value.general = 'Teacher profile identifier is required.';
    return;
  }

  const payload: SubmitQualificationEvidenceRequest = {
    qualificationTypeId: form.value.qualificationTypeId?.trim() ?? '',
    title: form.value.title?.trim() ?? '',
    issuingAuthority: form.value.issuingAuthority?.trim() ?? '',
    credentialIdentifier: form.value.credentialIdentifier?.trim() || null,
    issueDate: form.value.issueDate?.trim() ?? '',
    expiryDate: form.value.expiryDate?.trim() || null,
    documentReference: form.value.documentReference?.trim() || null,
    restrictionsOrConditions: form.value.restrictionsOrConditions?.trim() || null,
  };

  const validationResult = submitEvidenceSchema.safeParse(payload);
  if (!validationResult.success) {
    const formatted = validationResult.error.format();
    for (const [key, val] of Object.entries(formatted)) {
      if (key !== '_errors' && val && typeof val === 'object' && '_errors' in val) {
        errors.value[key] = (val as { _errors: string[] })._errors[0] || 'Invalid field value.';
      }
    }
    return;
  }

  try {
    const result = await submitEvidenceMutation.mutateAsync({
      teacherId: props.teacherId,
      payload: validationResult.data,
    });

    emit('submitted', result);
    emit('update:visible', false);
    emit('close');
  } catch (err: unknown) {
    if (err && typeof err === 'object' && 'response' in err) {
      const resp = (err as { response?: { data?: { detail?: string; message?: string } } }).response;
      errors.value.general = resp?.data?.detail || resp?.data?.message || 'Failed to submit qualification evidence.';
    }
  }
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
    :append-to="appendTo"
    data-testid="submit-evidence-modal"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <div class="p-2 rounded-lg bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400">
          <Award class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-surface-900 dark:text-surface-100">
            Submit Qualification Documentary Evidence
          </h2>
          <p class="text-xs text-surface-500">
            Record academic certificate or credential for independent verification (TD-05).
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <!-- TD-05 Verification Process Notice -->
      <div
        class="p-3 rounded-lg bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800 text-sky-800 dark:text-sky-300 flex items-start gap-2.5 text-xs leading-relaxed"
        data-testid="evidence-notice-banner"
      >
        <Info class="w-4 h-4 shrink-0 mt-0.5 text-sky-600 dark:text-sky-400" />
        <span>
          <strong>Credential Verification Policy:</strong> Submitted evidence enters <em>'submitted'</em> status and will appear in the academic work queue for verification by a designated verifier or teaching authority.
        </span>
      </div>

      <!-- General Error Alert -->
      <div
        v-if="errors.general"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
        data-testid="submit-evidence-error"
      >
        {{ errors.general }}
      </div>

      <!-- Form Grid -->
      <form
        class="space-y-3.5"
        data-testid="submit-evidence-form"
        @submit.prevent="handleSubmit"
      >
        <!-- Qualification Type -->
        <FormField
          label="Qualification Category"
          required
          :error="errors.qualificationTypeId"
          hint="Standard qualification classification defining validity duration & renewal policy."
        >
          <Select
            v-model="form.qualificationTypeId"
            :options="qualificationTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select qualification type..."
            class="w-full"
            :loading="isLoadingTypes"
            :invalid="Boolean(errors.qualificationTypeId)"
            data-testid="select-qualification-type"
          />
        </FormField>

        <!-- Title -->
        <FormField
          label="Credential / Degree Title"
          required
          :error="errors.title"
          hint="Official name of the academic degree, professional certificate, or license."
        >
          <InputText
            v-model="form.title"
            placeholder="e.g. Sertifikat Pendidik Matematika / S1 Pendidikan"
            class="w-full"
            :invalid="Boolean(errors.title)"
            data-testid="input-evidence-title"
          />
        </FormField>

        <!-- Issuing Authority & Credential Identifier -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Issuing Authority"
            required
            :error="errors.issuingAuthority"
            hint="Institution, university, or ministry"
          >
            <InputText
              v-model="form.issuingAuthority"
              placeholder="e.g. Universitas Pendidikan Indonesia"
              class="w-full"
              :invalid="Boolean(errors.issuingAuthority)"
              data-testid="input-issuing-authority"
            />
          </FormField>

          <FormField
            label="Credential ID / Registration Number"
            :error="errors.credentialIdentifier"
            hint="Serial, license, or diploma number"
          >
            <InputText
              v-model="form.credentialIdentifier"
              placeholder="e.g. SERDIK-2024-00129"
              class="w-full font-mono"
              :invalid="Boolean(errors.credentialIdentifier)"
              data-testid="input-credential-identifier"
            />
          </FormField>
        </div>

        <!-- Issue Date & Expiry Date -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Issue Date"
            required
            :error="errors.issueDate"
            hint="YYYY-MM-DD format"
          >
            <InputText
              v-model="form.issueDate"
              type="date"
              placeholder="YYYY-MM-DD"
              class="w-full"
              :invalid="Boolean(errors.issueDate)"
              data-testid="input-issue-date"
            />
          </FormField>

          <FormField
            label="Expiry Date (If Applicable)"
            :error="errors.expiryDate"
            hint="Leave blank if permanent / lifetime"
          >
            <InputText
              v-model="form.expiryDate"
              type="date"
              placeholder="YYYY-MM-DD"
              class="w-full"
              :invalid="Boolean(errors.expiryDate)"
              data-testid="input-expiry-date"
            />
          </FormField>
        </div>

        <!-- Document Reference -->
        <FormField
          label="Document Reference / Storage URI"
          :error="errors.documentReference"
          hint="URL or digital archive identifier for the scanned document"
        >
          <div class="relative flex items-center">
            <InputText
              v-model="form.documentReference"
              placeholder="e.g. https://storage.elsasa.sch.id/creds/serdik-00129.pdf"
              class="w-full"
              :invalid="Boolean(errors.documentReference)"
              data-testid="input-document-reference"
            />
          </div>
        </FormField>

        <!-- Restrictions / Conditions -->
        <FormField
          label="Conditions or Restrictions (Optional)"
          :error="errors.restrictionsOrConditions"
          hint="Specific endorsement conditions, specialization boundaries, or limitations"
        >
          <Textarea
            v-model="form.restrictionsOrConditions"
            rows="2"
            auto-resize
            placeholder="e.g. Valid only for upper secondary tier mathematics..."
            class="w-full text-xs"
            :invalid="Boolean(errors.restrictionsOrConditions)"
            data-testid="textarea-restrictions"
          />
        </FormField>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="submitEvidenceMutation.isPending.value"
          data-testid="cancel-submit-evidence-btn"
          @click="handleClose"
        />
        <Button
          type="button"
          label="Submit Credential Evidence"
          icon="pi pi-check"
          severity="primary"
          size="small"
          :loading="submitEvidenceMutation.isPending.value"
          data-testid="submit-evidence-btn"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
