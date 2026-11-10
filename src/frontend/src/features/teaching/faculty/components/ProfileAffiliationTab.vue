<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  User,
  Mail,
  Phone,
  Building2,
  Calendar,
  ShieldCheck,
  Clock,
  Sparkles,
  Lock,
  Pencil,
  X,
  Check,
  AlertCircle,
  FileText,
} from 'lucide-vue-next';
import { FormField, StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { updateTeacherSchema } from '../api/teachingSchemas';
import { useUpdateTeacherMutation } from '../composables/useFacultyMutations';
import type { TeacherProfileDto, UpdateTeacherProfileRequest } from '../api/teachingTypes';

export interface ProfileAffiliationTabProps {
  teacher: TeacherProfileDto;
  /** Explicit override for edit permission (useful for testing) */
  canEdit?: boolean;
}

const props = withDefaults(defineProps<ProfileAffiliationTabProps>(), {
  canEdit: undefined,
});

const emit = defineEmits<{
  (e: 'updated', teacher: TeacherProfileDto): void;
}>();

// ---------------------------------------------------------------------------
// 1. Permissions & Edit State
// ---------------------------------------------------------------------------
const authStore = useAuthStore();

const isEditable = computed(() => {
  if (props.canEdit !== undefined) {
    return props.canEdit;
  }
  return authStore.hasPermission(TEACHING_PERMISSIONS.MANAGE);
});

const isEditing = ref(false);

const campusOptions = [
  'Kampus Utama',
  'Kampus Barat',
  'Kampus Timur',
  'Kampus Selatan',
  'Online / Distance Learning',
];

// ---------------------------------------------------------------------------
// 2. Mutable Edit Form State
// ---------------------------------------------------------------------------
const form = ref({
  displayName: '',
  email: '',
  phone: '',
  campusAffiliation: '',
  department: '',
  notes: '',
});

const errors = ref<Record<string, string>>({});

function initForm() {
  form.value = {
    displayName: props.teacher.displayName ?? '',
    email: props.teacher.email ?? '',
    phone: props.teacher.phone ?? '',
    campusAffiliation: props.teacher.campusAffiliation ?? '',
    department: props.teacher.department ?? '',
    notes: props.teacher.notes ?? '',
  };
  errors.value = {};
}

// Reset form when teacher prop changes or edit mode toggles
watch(
  () => props.teacher,
  () => {
    if (!isEditing.value) {
      initForm();
    }
  },
  { immediate: true },
);

function startEditing() {
  initForm();
  isEditing.value = true;
}

function cancelEditing() {
  initForm();
  isEditing.value = false;
}

// ---------------------------------------------------------------------------
// 3. Validation & Mutation
// ---------------------------------------------------------------------------
const updateMutation = useUpdateTeacherMutation({
  onSuccess: () => {
    isEditing.value = false;
    emit('updated', props.teacher);
  },
});

const isSaving = computed(() => updateMutation.isPending.value);

function validate(): boolean {
  errors.value = {};

  const payload = {
    displayName: form.value.displayName?.trim() || null,
    email: form.value.email?.trim() || null,
    phone: form.value.phone?.trim() || null,
    campusAffiliation: form.value.campusAffiliation?.trim() || null,
    department: form.value.department?.trim() || null,
    notes: form.value.notes?.trim() || null,
  };

  const result = updateTeacherSchema.safeParse(payload);
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

function handleSave() {
  if (!validate()) return;

  const payload: UpdateTeacherProfileRequest = {
    displayName: form.value.displayName?.trim() || undefined,
    email: form.value.email?.trim() || undefined,
    phone: form.value.phone?.trim() || undefined,
    campusAffiliation: form.value.campusAffiliation?.trim() || undefined,
    department: form.value.department?.trim() || undefined,
    notes: form.value.notes?.trim() || undefined,
  };

  updateMutation.mutate({
    id: props.teacher.id,
    payload,
  });
}
</script>

<template>
  <div class="space-y-6" data-testid="profile-affiliation-tab">
    <!-- Tab Action Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-surface-200 dark:border-surface-700">
      <div>
        <h3 class="text-sm sm:text-base font-bold text-surface-900 dark:text-surface-100 flex items-center gap-2">
          <span>Faculty Profile & Institutional Affiliation</span>
          <span
            v-if="isEditing"
            class="text-[11px] font-semibold text-primary-700 dark:text-primary-300 bg-primary-50 dark:bg-primary-950 px-2 py-0.5 rounded border border-primary-200 dark:border-primary-800"
          >
            Editing Mode
          </span>
        </h3>
        <p class="text-xs text-surface-500 mt-0.5">
          General faculty identity, contact channels, HR decoupling reference, and formal approval records.
        </p>
      </div>

      <!-- Action Button Group -->
      <div class="flex items-center gap-2 shrink-0">
        <template v-if="!isEditing">
          <Button
            v-if="isEditable"
            severity="secondary"
            variant="outlined"
            size="small"
            class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
            data-testid="btn-edit-profile"
            @click="startEditing"
          >
            <Pencil class="w-3.5 h-3.5" />
            <span>Edit Profile</span>
          </Button>
        </template>
        <template v-else>
          <Button
            type="button"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="isSaving"
            class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
            data-testid="btn-cancel-edit"
            @click="cancelEditing"
          >
            <X class="w-3.5 h-3.5" />
            <span>Cancel</span>
          </Button>

          <Button
            type="button"
            severity="primary"
            size="small"
            :loading="isSaving"
            :disabled="isSaving"
            class="!text-xs !py-1.5 !px-3.5 !gap-1.5 font-medium"
            data-testid="btn-save-profile"
            @click="handleSave"
          >
            <Check class="w-3.5 h-3.5" />
            <span>Save Changes</span>
          </Button>
        </template>
      </div>
    </div>

    <!-- Section 1: Decoupled Identity & HR Demarcation Block (TD-04) -->
    <div
      class="p-4 bg-surface-50 dark:bg-surface-800/40 rounded-xl border border-surface-200 dark:border-surface-700/80 space-y-3"
      data-testid="decoupled-identity-block"
    >
      <div class="flex items-center justify-between flex-wrap gap-2">
        <div class="flex items-center gap-2 text-xs font-bold text-surface-900 dark:text-surface-100">
          <div class="w-6 h-6 rounded-md bg-primary-100 dark:bg-primary-950 flex items-center justify-center text-primary-600 dark:text-primary-400">
            <Sparkles class="w-3.5 h-3.5" />
          </div>
          <span>Faculty Identity & HR Decoupling (TD-04)</span>
        </div>

        <span
          class="text-[11px] text-surface-500 flex items-center gap-1"
          title="Teaching profiles maintain academic credentials independently of employment/payroll systems."
        >
          <Lock class="w-3 h-3 text-surface-400" />
          <span>Core identity anchored independently from HR payroll</span>
        </span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 text-xs">
        <!-- National ID / Passport -->
        <div class="p-3 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700/60 space-y-1">
          <span class="text-surface-400 font-medium block">National ID / Passport (NIK)</span>
          <div class="flex items-center gap-2">
            <span class="font-mono font-semibold text-surface-900 dark:text-surface-100" data-testid="detail-national-id">
              {{ teacher.nationalIdOrExternalRef || '—' }}
            </span>
            <span
              v-if="teacher.nationalIdOrExternalRef"
              class="text-[10px] px-1.5 py-0.2 rounded bg-surface-100 dark:bg-surface-800 text-surface-600 dark:text-surface-400 font-medium"
            >
              Unique Anchor
            </span>
          </div>
        </div>

        <!-- Employee Reference -->
        <div class="p-3 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700/60 space-y-1 sm:col-span-2">
          <span class="text-surface-400 font-medium block">HR Employee Reference</span>
          <div class="flex flex-wrap items-center gap-2" data-testid="detail-employee-ref">
            <template v-if="teacher.employeeRef">
              <span class="font-mono font-semibold text-surface-900 dark:text-surface-100">
                {{ teacher.employeeRef }}
              </span>
              <span class="text-[10px] px-2 py-0.5 rounded bg-surface-100 dark:bg-surface-800 text-surface-600 dark:text-surface-400 font-medium">
                Internal Employee
              </span>
            </template>
            <template v-else>
              <span
                class="inline-flex items-center gap-1.5 text-xs font-semibold text-amber-700 dark:text-amber-300 bg-amber-50 dark:bg-amber-950/60 px-2.5 py-1 rounded-md border border-amber-200 dark:border-amber-800"
                data-testid="badge-non-employee"
              >
                <span>Independent Teaching Resource / Non-Employee</span>
              </span>
              <span class="text-[11px] text-surface-500">
                (Visiting faculty, guest lecturer, or external assessor)
              </span>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Section 2: Personal Information & Institutional Role -->
    <div class="space-y-3">
      <h4 class="text-xs font-semibold uppercase tracking-wider text-surface-500">
        Personal & Role Details
      </h4>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 text-xs">
        <!-- Legal Full Name (Immutable via inline edit) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-surface-400 font-medium">Legal Full Name</span>
            <Lock class="w-3 h-3 text-surface-400" title="Modify via Auditable Record Correction" />
          </div>
          <span class="font-semibold text-surface-900 dark:text-surface-100 block truncate" data-testid="detail-full-name">
            {{ teacher.fullName }}
          </span>
          <span class="text-[10px] text-surface-400 block">
            Immutable official identity
          </span>
        </div>

        <!-- Institutional Teacher Number (Immutable via inline edit) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-surface-400 font-medium">Teacher Number</span>
            <Lock class="w-3 h-3 text-surface-400" title="Autogenerated / fixed institutional identifier" />
          </div>
          <span class="font-mono font-bold text-primary-700 dark:text-primary-300 block" data-testid="detail-teacher-number">
            {{ teacher.teacherNumber }}
          </span>
          <span class="text-[10px] text-surface-400 block">
            System registration number
          </span>
        </div>

        <!-- Teaching Role (Immutable via inline edit) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-surface-400 font-medium">Teaching Role</span>
            <Lock class="w-3 h-3 text-surface-400" title="Configured at candidate intake" />
          </div>
          <span class="font-semibold text-surface-900 dark:text-surface-100 block" data-testid="detail-role-name">
            {{ teacher.teachingRoleName }}
          </span>
          <span class="text-[10px] text-surface-400 block">
            Primary pedagogical assignment role
          </span>
        </div>

        <!-- Professional Display Name (Editable) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1 sm:col-span-2 lg:col-span-3">
          <template v-if="!isEditing">
            <span class="text-surface-400 font-medium block">Professional Display Name</span>
            <span class="font-semibold text-surface-900 dark:text-surface-100 block" data-testid="detail-display-name">
              {{ teacher.displayName || '—' }}
            </span>
            <span class="text-[10px] text-surface-400 block">
              Used on public schedules, syllabi, and official student-facing documentation.
            </span>
          </template>
          <template v-else>
            <FormField
              id="edit-displayName"
              label="Professional Display Name"
              optional
              help-text="Academic title, credentials, or preferred display name (e.g. Budi Hartono, M.Pd.)."
              :error="errors.displayName"
            >
              <InputText
                id="edit-displayName"
                v-model="form.displayName"
                class="w-full text-xs"
                :invalid="Boolean(errors.displayName)"
                data-testid="input-edit-display-name"
              />
            </FormField>
          </template>
        </div>
      </div>
    </div>

    <!-- Section 3: Contact Channels & Campus Affiliation -->
    <div class="space-y-3">
      <h4 class="text-xs font-semibold uppercase tracking-wider text-surface-500">
        Contact & Institutional Affiliation
      </h4>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
        <!-- Campus Affiliation -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <template v-if="!isEditing">
            <span class="text-surface-400 font-medium block">Campus Affiliation</span>
            <div class="flex items-center gap-1.5 text-surface-900 dark:text-surface-100 font-semibold" data-testid="detail-campus">
              <Building2 class="w-4 h-4 text-surface-400 shrink-0" />
              <span>{{ teacher.campusAffiliation }}</span>
            </div>
          </template>
          <template v-else>
            <FormField
              id="edit-campus"
              label="Campus Affiliation"
              required
              :error="errors.campusAffiliation"
            >
              <Select
                id="edit-campus"
                v-model="form.campusAffiliation"
                :options="campusOptions"
                editable
                class="w-full text-xs"
                :invalid="Boolean(errors.campusAffiliation)"
                data-testid="select-edit-campus"
              />
            </FormField>
          </template>
        </div>

        <!-- Subject Department -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <template v-if="!isEditing">
            <span class="text-surface-400 font-medium block">Department / Faculty Area</span>
            <span class="font-semibold text-surface-900 dark:text-surface-100 block" data-testid="detail-department">
              {{ teacher.department || '—' }}
            </span>
          </template>
          <template v-else>
            <FormField
              id="edit-department"
              label="Department / Faculty Area"
              optional
              :error="errors.department"
            >
              <InputText
                id="edit-department"
                v-model="form.department"
                placeholder="e.g. Matematika & IPA"
                class="w-full text-xs"
                :invalid="Boolean(errors.department)"
                data-testid="input-edit-department"
              />
            </FormField>
          </template>
        </div>

        <!-- Institutional Email -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <template v-if="!isEditing">
            <span class="text-surface-400 font-medium block">Institutional Email</span>
            <div class="flex items-center gap-1.5" data-testid="detail-email">
              <Mail class="w-3.5 h-3.5 text-surface-400 shrink-0" />
              <a
                v-if="teacher.email"
                :href="`mailto:${teacher.email}`"
                class="hover:underline font-mono text-[11px] text-primary-600 dark:text-primary-400"
              >
                {{ teacher.email }}
              </a>
              <span v-else class="text-surface-400">—</span>
            </div>
          </template>
          <template v-else>
            <FormField
              id="edit-email"
              label="Institutional Email"
              optional
              :error="errors.email"
            >
              <InputText
                id="edit-email"
                v-model="form.email"
                type="email"
                class="w-full text-xs"
                :invalid="Boolean(errors.email)"
                data-testid="input-edit-email"
              />
            </FormField>
          </template>
        </div>

        <!-- Phone Number -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <template v-if="!isEditing">
            <span class="text-surface-400 font-medium block">Phone Number</span>
            <div class="flex items-center gap-1.5" data-testid="detail-phone">
              <Phone class="w-3.5 h-3.5 text-surface-400 shrink-0" />
              <span v-if="teacher.phone" class="font-mono text-[11px] text-surface-800 dark:text-surface-200">
                {{ teacher.phone }}
              </span>
              <span v-else class="text-surface-400">—</span>
            </div>
          </template>
          <template v-else>
            <FormField
              id="edit-phone"
              label="Phone Number"
              optional
              :error="errors.phone"
            >
              <InputText
                id="edit-phone"
                v-model="form.phone"
                placeholder="e.g. +62 812-3456-7890"
                class="w-full text-xs"
                :invalid="Boolean(errors.phone)"
                data-testid="input-edit-phone"
              />
            </FormField>
          </template>
        </div>
      </div>
    </div>

    <!-- Section 4: Tenure, Dates, and Formal Authority Approval Stamp -->
    <div class="space-y-3">
      <h4 class="text-xs font-semibold uppercase tracking-wider text-surface-500">
        Tenure & Authority Approval Governance
      </h4>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 text-xs">
        <!-- Effective Dates -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <span class="text-surface-400 font-medium block">Effective Term Dates</span>
          <div class="flex items-center gap-1.5 text-surface-900 dark:text-surface-100 font-medium" data-testid="detail-effective-dates">
            <Calendar class="w-3.5 h-3.5 text-surface-400 shrink-0" />
            <span>{{ formatDisplayDate(teacher.effectiveFrom) }}</span>
            <span v-if="teacher.effectiveTo"> to {{ formatDisplayDate(teacher.effectiveTo) }}</span>
            <span v-else class="text-surface-400"> (Indefinite)</span>
          </div>
        </div>

        <!-- Standing State -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <span class="text-surface-400 font-medium block">Teacher Standing</span>
          <div>
            <StatusPill :status="teacher.standing" size="sm" data-testid="detail-standing" />
          </div>
        </div>

        <!-- Authority Approval Stamp (TD-03) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1">
          <span class="text-surface-400 font-medium block">Authority Approval Stamp</span>
          <div data-testid="detail-approval-stamp">
            <div
              v-if="teacher.approvedBy"
              class="flex items-start gap-1.5 text-emerald-700 dark:text-emerald-300 font-medium"
            >
              <ShieldCheck class="w-4 h-4 text-emerald-600 dark:text-emerald-400 shrink-0 mt-0.5" />
              <div class="text-[11px] leading-snug">
                <span>{{ teacher.approvedBy }}</span>
                <span class="block text-surface-500 text-[10px]">{{ formatDisplayDate(teacher.approvedDate) }}</span>
              </div>
            </div>
            <div
              v-else
              class="flex items-center gap-1.5 text-amber-600 dark:text-amber-400 font-medium text-[11px]"
            >
              <Clock class="w-3.5 h-3.5 shrink-0" />
              <span>Pending Authority Approval</span>
            </div>
          </div>
        </div>

        <!-- Standing Reason (if any) -->
        <div
          v-if="teacher.standingReason"
          class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1 sm:col-span-2 lg:col-span-3"
        >
          <span class="text-surface-400 font-medium block">Standing Justification Rationale</span>
          <p class="text-surface-800 dark:text-surface-200" data-testid="detail-standing-reason">
            {{ teacher.standingReason }}
          </p>
        </div>

        <!-- Notes (View or Edit) -->
        <div class="p-3 bg-surface-50 dark:bg-surface-800/30 rounded-lg border border-surface-200/80 dark:border-surface-700/60 space-y-1 sm:col-span-2 lg:col-span-3">
          <template v-if="!isEditing">
            <span class="text-surface-400 font-medium block">Administrative Notes</span>
            <p v-if="teacher.notes" class="text-surface-800 dark:text-surface-200 whitespace-pre-wrap" data-testid="detail-notes">
              {{ teacher.notes }}
            </p>
            <span v-else class="text-surface-400 italic">No administrative notes recorded.</span>
          </template>
          <template v-else>
            <FormField
              id="edit-notes"
              label="Administrative Notes"
              optional
              :error="errors.notes"
            >
              <Textarea
                id="edit-notes"
                v-model="form.notes"
                rows="3"
                class="w-full text-xs"
                :invalid="Boolean(errors.notes)"
                data-testid="textarea-edit-notes"
              />
            </FormField>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>
