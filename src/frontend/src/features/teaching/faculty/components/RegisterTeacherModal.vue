<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { UserPlus, Sparkles, Building2, ShieldAlert } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { registerTeacherSchema } from '../api/teachingSchemas';
import { useRegisterTeacherMutation } from '../composables/useFacultyMutations';
import { useTeachingRolesQuery } from '../composables/useFacultyQueries';
import type {
  RegisterTeacherProfileRequest,
  RegisterTeacherProfileResponse,
  TeachingRoleDto,
} from '../api/teachingTypes';

export interface RegisterTeacherModalProps {
  visible: boolean;
  /** Optional override for role options (e.g. in tests) */
  roles?: TeachingRoleDto[];
  defaultCampus?: string;
  appendTo?: string;
}

const props = withDefaults(defineProps<RegisterTeacherModalProps>(), {
  visible: false,
  roles: undefined,
  defaultCampus: 'Kampus Utama',
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'registered', response: RegisterTeacherProfileResponse): void;
  (e: 'created', response: RegisterTeacherProfileResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Roles & Taxonomy Data
// ---------------------------------------------------------------------------
const { data: fetchedRoles, isLoading: isLoadingRoles } = useTeachingRolesQuery({
  enabled: computed(() => props.visible && !props.roles),
});

const availableRoles = computed<TeachingRoleDto[]>(() => {
  return props.roles ?? fetchedRoles.value ?? [];
});

const roleOptions = computed(() =>
  availableRoles.value.map((role) => ({
    label: role.name,
    value: role.id,
    code: role.code,
  })),
);

const campusOptions = [
  'Kampus Utama',
  'Kampus Barat',
  'Kampus Timur',
  'Kampus Selatan',
  'Online / Distance Learning',
];

// ---------------------------------------------------------------------------
// 2. Reactive Form State
// ---------------------------------------------------------------------------
function getTodayIso(): string {
  return new Date().toISOString().substring(0, 10);
}

const form = ref({
  teacherNumber: '',
  fullName: '',
  displayName: '',
  employeeRef: '',
  nationalIdOrExternalRef: '',
  email: '',
  phone: '',
  teachingRoleId: '',
  campusAffiliation: props.defaultCampus,
  department: '',
  effectiveFrom: getTodayIso(),
  effectiveTo: '',
  notes: '',
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    teacherNumber: '',
    fullName: '',
    displayName: '',
    employeeRef: '',
    nationalIdOrExternalRef: '',
    email: '',
    phone: '',
    teachingRoleId: roleOptions.value[0]?.value ?? '',
    campusAffiliation: props.defaultCampus,
    department: '',
    effectiveFrom: getTodayIso(),
    effectiveTo: '',
    notes: '',
  };
  errors.value = {};
}

// Watch visible to prefill or reset form
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm();
      if (!form.value.teachingRoleId && roleOptions.value.length > 0) {
        form.value.teachingRoleId = roleOptions.value[0].value;
      }
    }
  },
  { immediate: true },
);

// Auto-select first role if empty when roles load
watch(
  roleOptions,
  (opts) => {
    if (props.visible && !form.value.teachingRoleId && opts.length > 0) {
      form.value.teachingRoleId = opts[0].value;
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 3. Validation via registerTeacherSchema
// ---------------------------------------------------------------------------
function validate(): boolean {
  errors.value = {};

  const parsePayload = {
    teacherNumber: form.value.teacherNumber?.trim() || null,
    fullName: form.value.fullName?.trim() ?? '',
    displayName: form.value.displayName?.trim() || null,
    employeeRef: form.value.employeeRef?.trim() || null,
    nationalIdOrExternalRef: form.value.nationalIdOrExternalRef?.trim() || null,
    email: form.value.email?.trim() || null,
    phone: form.value.phone?.trim() || null,
    teachingRoleId: form.value.teachingRoleId?.trim() ?? '',
    campusAffiliation: form.value.campusAffiliation?.trim() ?? '',
    department: form.value.department?.trim() || null,
    effectiveFrom: form.value.effectiveFrom?.trim() ?? '',
    effectiveTo: form.value.effectiveTo?.trim() || null,
    notes: form.value.notes?.trim() || null,
  };

  const result = registerTeacherSchema.safeParse(parsePayload);
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
// 4. Mutation & Submission
// ---------------------------------------------------------------------------
const registerMutation = useRegisterTeacherMutation({
  onSuccess: (data) => {
    emit('registered', data);
    emit('created', data);
    handleClose();
  },
});

const isSubmitting = computed(() => registerMutation.isPending.value);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleSubmit() {
  if (!validate()) {
    return;
  }

  const payload: RegisterTeacherProfileRequest = {
    teacherNumber: form.value.teacherNumber?.trim() || undefined,
    fullName: form.value.fullName.trim(),
    displayName: form.value.displayName?.trim() || undefined,
    employeeRef: form.value.employeeRef?.trim() || undefined,
    nationalIdOrExternalRef: form.value.nationalIdOrExternalRef?.trim() || undefined,
    email: form.value.email?.trim() || undefined,
    phone: form.value.phone?.trim() || undefined,
    teachingRoleId: form.value.teachingRoleId,
    campusAffiliation: form.value.campusAffiliation.trim(),
    department: form.value.department?.trim() || undefined,
    effectiveFrom: form.value.effectiveFrom,
    effectiveTo: form.value.effectiveTo?.trim() || undefined,
    notes: form.value.notes?.trim() || undefined,
  };

  registerMutation.mutate(payload);
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :append-to="appendTo"
    :style="{ width: '44rem', maxWidth: '95vw' }"
    :closable="!isSubmitting"
    :close-on-escape="!isSubmitting"
    :dismissable-mask="!isSubmitting"
    data-testid="register-teacher-modal"
    @hide="handleClose"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="w-9 h-9 rounded-lg bg-primary-50 dark:bg-primary-950 border border-primary-200 dark:border-primary-800 flex items-center justify-center text-primary-600 dark:text-primary-400 shrink-0"
        >
          <UserPlus class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-sm sm:text-base font-bold text-surface-900 dark:text-surface-100">
            Register Faculty Candidate
          </h2>
          <p class="text-xs text-surface-500">
            Onboard a new teaching candidate into the intake work queue awaiting Teaching Authority review.
          </p>
        </div>
      </div>
    </template>

    <form
      class="space-y-4 pt-2"
      data-testid="register-teacher-form"
      @submit.prevent="handleSubmit"
    >
      <!-- Identity Section: Full Name & Display Name -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="fullName"
          label="Legal Full Name"
          required
          :error="errors.fullName"
        >
          <InputText
            id="fullName"
            v-model="form.fullName"
            placeholder="e.g. Budi Hartono"
            class="w-full text-xs"
            :invalid="Boolean(errors.fullName)"
            data-testid="input-full-name"
          />
        </FormField>

        <FormField
          id="displayName"
          label="Professional Display Name"
          optional
          help-text="Title/honorifics for official schedules."
          :error="errors.displayName"
        >
          <InputText
            id="displayName"
            v-model="form.displayName"
            placeholder="e.g. Budi Hartono, M.Pd."
            class="w-full text-xs"
            :invalid="Boolean(errors.displayName)"
            data-testid="input-display-name"
          />
        </FormField>
      </div>

      <!-- Teacher Number & Teaching Role -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="teacherNumber"
          label="Teacher Number"
          optional
          help-text="Leave empty to autogenerate (e.g. TCH-2026-XXXX)."
          :error="errors.teacherNumber"
        >
          <InputText
            id="teacherNumber"
            v-model="form.teacherNumber"
            placeholder="e.g. TCH-2026-0042 (optional)"
            class="w-full text-xs font-mono"
            :invalid="Boolean(errors.teacherNumber)"
            data-testid="input-teacher-number"
          />
        </FormField>

        <FormField
          id="teachingRoleId"
          label="Teaching Role"
          required
          :error="errors.teachingRoleId"
        >
          <Select
            id="teachingRoleId"
            v-model="form.teachingRoleId"
            :options="roleOptions"
            option-label="label"
            option-value="value"
            placeholder="Select teaching role"
            class="w-full text-xs"
            :loading="isLoadingRoles"
            :invalid="Boolean(errors.teachingRoleId)"
            data-testid="select-teaching-role"
          />
        </FormField>
      </div>

      <!-- Campus Affiliation & Department -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="campusAffiliation"
          label="Campus Affiliation"
          required
          :error="errors.campusAffiliation"
        >
          <Select
            id="campusAffiliation"
            v-model="form.campusAffiliation"
            :options="campusOptions"
            editable
            placeholder="Select or enter campus"
            class="w-full text-xs"
            :invalid="Boolean(errors.campusAffiliation)"
            data-testid="select-campus"
          />
        </FormField>

        <FormField
          id="department"
          label="Department / Faculty Area"
          optional
          :error="errors.department"
        >
          <InputText
            id="department"
            v-model="form.department"
            placeholder="e.g. Matematika & IPA"
            class="w-full text-xs"
            :invalid="Boolean(errors.department)"
            data-testid="input-department"
          />
        </FormField>
      </div>

      <!-- Contact Info: Email & Phone -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="email"
          label="Institutional / Contact Email"
          optional
          :error="errors.email"
        >
          <InputText
            id="email"
            v-model="form.email"
            type="email"
            placeholder="e.g. budi.hartono@elsasa.sch.id"
            class="w-full text-xs"
            :invalid="Boolean(errors.email)"
            data-testid="input-email"
          />
        </FormField>

        <FormField
          id="phone"
          label="Phone Number"
          optional
          :error="errors.phone"
        >
          <InputText
            id="phone"
            v-model="form.phone"
            placeholder="e.g. +62 812-3456-7890"
            class="w-full text-xs"
            :invalid="Boolean(errors.phone)"
            data-testid="input-phone"
          />
        </FormField>
      </div>

      <!-- HR Identity Decoupling Section (TD-04) -->
      <div class="p-3 bg-surface-50 dark:bg-surface-800/40 rounded-lg border border-surface-200 dark:border-surface-700/80 space-y-3">
        <div class="flex items-center gap-1.5 text-xs font-semibold text-surface-700 dark:text-surface-300">
          <Sparkles class="w-3.5 h-3.5 text-primary-500" />
          <span>Faculty Identity & HR Decoupling (TD-04)</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="employeeRef"
            label="Employee Reference"
            optional
            help-text="Leave empty for visiting faculty, guest lecturers, or external assessors (TD-04)."
            :error="errors.employeeRef"
          >
            <InputText
              id="employeeRef"
              v-model="form.employeeRef"
              placeholder="e.g. EMP-2026-042 (optional)"
              class="w-full text-xs font-mono"
              :invalid="Boolean(errors.employeeRef)"
              data-testid="input-employee-ref"
            />
          </FormField>

          <FormField
            id="nationalIdOrExternalRef"
            label="National ID (NIK) / Passport"
            optional
            help-text="Unique anchor for non-employee faculty without an HR ID."
            :error="errors.nationalIdOrExternalRef"
          >
            <InputText
              id="nationalIdOrExternalRef"
              v-model="form.nationalIdOrExternalRef"
              placeholder="e.g. 3201234567890001 or P-USA-8849201"
              class="w-full text-xs font-mono"
              :invalid="Boolean(errors.nationalIdOrExternalRef)"
              data-testid="input-national-id"
            />
          </FormField>
        </div>
      </div>

      <!-- Effective Dates: From & To -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="effectiveFrom"
          label="Effective From Date"
          required
          :error="errors.effectiveFrom"
        >
          <InputText
            id="effectiveFrom"
            v-model="form.effectiveFrom"
            type="date"
            class="w-full text-xs"
            :invalid="Boolean(errors.effectiveFrom)"
            data-testid="input-effective-from"
          />
        </FormField>

        <FormField
          id="effectiveTo"
          label="Effective To Date"
          optional
          :error="errors.effectiveTo"
        >
          <InputText
            id="effectiveTo"
            v-model="form.effectiveTo"
            type="date"
            class="w-full text-xs"
            :invalid="Boolean(errors.effectiveTo)"
            data-testid="input-effective-to"
          />
        </FormField>
      </div>

      <!-- Notes / Intake Justification -->
      <FormField
        id="notes"
        label="Intake Notes & Justification"
        optional
        :error="errors.notes"
      >
        <Textarea
          id="notes"
          v-model="form.notes"
          rows="2"
          placeholder="Optional notes regarding recruitment or qualifications..."
          class="w-full text-xs"
          :invalid="Boolean(errors.notes)"
          data-testid="input-notes"
        />
      </FormField>

      <!-- Bottom Action Bar -->
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-200 dark:border-surface-700">
        <Button
          type="button"
          severity="secondary"
          variant="outlined"
          size="small"
          :disabled="isSubmitting"
          class="!text-xs !py-1.5 !px-3 font-medium"
          data-testid="btn-cancel-registration"
          @click="handleClose"
        >
          Cancel
        </Button>

        <Button
          type="submit"
          severity="primary"
          size="small"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          class="!text-xs !py-1.5 !px-4 !gap-1.5 font-medium"
          data-testid="btn-submit-registration"
        >
          <UserPlus class="w-4 h-4" />
          <span>Register Faculty Candidate</span>
        </Button>
      </div>
    </form>
  </Dialog>
</template>
