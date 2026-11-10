<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import {
  ShieldCheck,
  AlertTriangle,
  Award,
  Users,
  Info,
  CheckCircle2,
  Clock,
} from 'lucide-vue-next';
import { FormField, StatusPill } from '@/shared/components';
import { grantAuthorizationSchema } from '../api/teachingSchemas';
import { useGrantAuthorizationMutation } from '../composables/useFacultyMutations';
import type {
  TeacherProfileDto,
  GrantTeachingAuthorizationRequest,
  GrantTeachingAuthorizationResponse,
  AuthorizationStatus,
  SupervisionType,
} from '../api/teachingTypes';

export interface GrantAuthorizationModalProps {
  visible: boolean;
  teacher: TeacherProfileDto;
  appendTo?: string;
}

const props = withDefaults(defineProps<GrantAuthorizationModalProps>(), {
  visible: false,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'granted', response: GrantTeachingAuthorizationResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Helpers & Select Options
// ---------------------------------------------------------------------------
function getTodayIso(): string {
  return new Date().toISOString().substring(0, 10);
}

const statusOptions = [
  { label: 'Active (Fully Authorized)', value: 'active' },
  { label: 'Authorized Under Supervision (Conditional)', value: 'authorized_under_supervision' },
];

const deliveryModeOptions = [
  { label: 'Tatap Muka (In-Person Classroom)', value: 'in_person' },
  { label: 'Hybrid / Blended Learning', value: 'hybrid' },
  { label: 'Online / Distance Learning', value: 'online' },
  { label: 'Praktik Lapangan (Field Practicum)', value: 'field_practicum' },
  { label: 'All Delivery Formats', value: 'all' },
];

const supervisionTypeOptions = [
  { label: 'Co-Teaching (Teaching alongside Senior Mentor)', value: 'co_teaching' },
  { label: 'Periodic Classroom Observation', value: 'periodic_observation' },
  { label: 'Lesson Plan & Syllabi Review', value: 'lesson_plan_review' },
  { label: 'General Pedagogical Mentorship', value: 'general_mentorship' },
];

const campusOptions = [
  'Kampus Utama',
  'Kampus Barat',
  'Kampus Timur',
  'Kampus Selatan',
  'Online / Distance Learning',
];

// ---------------------------------------------------------------------------
// 2. Capabilities & Credentials Mapping
// ---------------------------------------------------------------------------
const capabilityOptions = computed(() => {
  if (!props.teacher?.capabilities?.length) return [];
  return props.teacher.capabilities.map((cap) => ({
    label: `${cap.capabilityName} (${cap.capabilityCode}) - ${cap.applicableLevel || 'All'} [${cap.deliveryMode || 'General'}]`,
    value: cap.id,
    name: cap.capabilityName,
    code: cap.capabilityCode,
    level: cap.applicableLevel,
    mode: cap.deliveryMode,
  }));
});

const credentialsList = computed(() => {
  return props.teacher?.qualifications ?? [];
});

// ---------------------------------------------------------------------------
// 3. Form Reactive State
// ---------------------------------------------------------------------------
interface FormState {
  scopeDescription: string;
  campusScope: string;
  programScope: string;
  levelScope: string;
  deliveryModeScope: string;
  authorizedCapabilityId: string;
  effectiveStartDate: string;
  effectiveEndDate: string;
  status: 'active' | 'authorized_under_supervision';
  conditionsOrRestrictions: string;
  notes: string;
  linkedEvidenceIds: string[];
  // Conditional supervision fields
  supervisionType: SupervisionType;
  supervisionDescription: string;
  supervisingTeacherId: string;
  supervisionStartDate: string;
  supervisionEndDate: string;
}

const form = ref<FormState>({
  scopeDescription: '',
  campusScope: props.teacher.campusAffiliation || 'Kampus Utama',
  programScope: '',
  levelScope: '',
  deliveryModeScope: 'in_person',
  authorizedCapabilityId: '',
  effectiveStartDate: getTodayIso(),
  effectiveEndDate: '',
  status: 'active',
  conditionsOrRestrictions: '',
  notes: '',
  linkedEvidenceIds: [],
  supervisionType: 'co_teaching',
  supervisionDescription: '',
  supervisingTeacherId: '',
  supervisionStartDate: getTodayIso(),
  supervisionEndDate: '',
});

const errors = ref<Record<string, string>>({});

function resetForm() {
  form.value = {
    scopeDescription: '',
    campusScope: props.teacher.campusAffiliation || 'Kampus Utama',
    programScope: '',
    levelScope: '',
    deliveryModeScope: 'in_person',
    authorizedCapabilityId: capabilityOptions.value[0]?.value ?? '',
    effectiveStartDate: getTodayIso(),
    effectiveEndDate: '',
    status: 'active',
    conditionsOrRestrictions: '',
    notes: '',
    linkedEvidenceIds: credentialsList.value
      .filter((q) => q.verificationState === 'verified')
      .map((q) => q.id),
    supervisionType: 'co_teaching',
    supervisionDescription: '',
    supervisingTeacherId: '',
    supervisionStartDate: getTodayIso(),
    supervisionEndDate: '',
  };
  errors.value = {};
}

// Auto-fill scope when capability is selected
watch(
  () => form.value.authorizedCapabilityId,
  (capId) => {
    if (!capId) return;
    const selectedCap = capabilityOptions.value.find((c) => c.value === capId);
    if (selectedCap) {
      if (!form.value.scopeDescription) {
        form.value.scopeDescription = `Authorized to teach ${selectedCap.name} (${selectedCap.code})`;
      }
      if (selectedCap.level && !form.value.levelScope) {
        form.value.levelScope = selectedCap.level;
      }
      if (selectedCap.mode && !form.value.deliveryModeScope) {
        form.value.deliveryModeScope = selectedCap.mode;
      }
    }
  },
);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 4. Permissive Guidance Detection (TD-05)
// ---------------------------------------------------------------------------
const selectedCredentials = computed(() => {
  return credentialsList.value.filter((q) =>
    form.value.linkedEvidenceIds.includes(q.id),
  );
});

const hasUnverifiedSelected = computed(() => {
  return selectedCredentials.value.some((q) => q.verificationState !== 'verified');
});

const unverifiedCredentialsList = computed(() => {
  return selectedCredentials.value.filter((q) => q.verificationState !== 'verified');
});

function toggleCredential(credentialId: string) {
  const index = form.value.linkedEvidenceIds.indexOf(credentialId);
  if (index > -1) {
    form.value.linkedEvidenceIds.splice(index, 1);
  } else {
    form.value.linkedEvidenceIds.push(credentialId);
  }
}

// ---------------------------------------------------------------------------
// 5. Mutation & Submission
// ---------------------------------------------------------------------------
const grantMutation = useGrantAuthorizationMutation();

async function handleSubmit() {
  errors.value = {};

  const validationResult = grantAuthorizationSchema.safeParse({
    scopeDescription: form.value.scopeDescription,
    campusScope: form.value.campusScope,
    programScope: form.value.programScope || undefined,
    levelScope: form.value.levelScope || undefined,
    deliveryModeScope: form.value.deliveryModeScope || undefined,
    authorizedCapabilityId: form.value.authorizedCapabilityId || undefined,
    effectiveStartDate: form.value.effectiveStartDate,
    effectiveEndDate: form.value.effectiveEndDate || undefined,
    status: form.value.status,
    conditionsOrRestrictions: form.value.conditionsOrRestrictions || undefined,
    notes: form.value.notes || undefined,
    linkedEvidenceIds: form.value.linkedEvidenceIds,
    supervisionType: form.value.status === 'authorized_under_supervision' ? form.value.supervisionType : undefined,
    supervisionDescription: form.value.status === 'authorized_under_supervision' ? form.value.supervisionDescription : undefined,
    supervisionStartDate: form.value.status === 'authorized_under_supervision' ? form.value.supervisionStartDate : undefined,
    supervisionEndDate: form.value.status === 'authorized_under_supervision' ? form.value.supervisionEndDate || undefined : undefined,
    supervisingTeacherId: form.value.status === 'authorized_under_supervision' ? form.value.supervisingTeacherId || undefined : undefined,
  });

  if (!validationResult.success) {
    for (const issue of validationResult.error.issues) {
      const field = issue.path[0] ? String(issue.path[0]) : 'general';
      if (!errors.value[field]) {
        errors.value[field] = issue.message;
      }
    }
    return;
  }

  const payload: GrantTeachingAuthorizationRequest = {
    scopeDescription: form.value.scopeDescription,
    campusScope: form.value.campusScope,
    programScope: form.value.programScope || null,
    levelScope: form.value.levelScope || null,
    deliveryModeScope: form.value.deliveryModeScope || null,
    authorizedCapabilityId: form.value.authorizedCapabilityId || null,
    effectiveStartDate: form.value.effectiveStartDate,
    effectiveEndDate: form.value.effectiveEndDate || null,
    status: form.value.status,
    conditionsOrRestrictions: form.value.conditionsOrRestrictions || null,
    notes: form.value.notes || null,
    linkedEvidenceIds: form.value.linkedEvidenceIds,
    ...(form.value.status === 'authorized_under_supervision'
      ? {
          supervision: {
            supervisionType: form.value.supervisionType,
            description: form.value.supervisionDescription,
            startDate: form.value.supervisionStartDate || form.value.effectiveStartDate,
            endDate: form.value.supervisionEndDate || null,
            supervisingTeacherId: form.value.supervisingTeacherId || null,
          },
          supervisionType: form.value.supervisionType,
          supervisionDescription: form.value.supervisionDescription,
          supervisionStartDate: form.value.supervisionStartDate || form.value.effectiveStartDate,
          supervisionEndDate: form.value.supervisionEndDate || null,
          supervisingTeacherId: form.value.supervisingTeacherId || null,
        }
      : {}),
  };

  try {
    const result = await grantMutation.mutateAsync({
      teacherId: props.teacher.id,
      payload,
    });
    emit('granted', result);
    isDialogVisible.value = false;
  } catch (err: unknown) {
    const errorObj = err as { response?: { data?: { detail?: string; title?: string } } };
    errors.value.general =
      errorObj.response?.data?.detail ||
      errorObj.response?.data?.title ||
      'Failed to grant teaching authorization. Please try again.';
  }
}

function handleClose() {
  isDialogVisible.value = false;
  emit('close');
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :draggable="false"
    :append-to="appendTo"
    class="w-full max-w-2xl mx-4"
    data-testid="grant-authorization-modal"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="w-9 h-9 rounded-lg bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center text-teal-600 dark:text-teal-400">
          <ShieldCheck class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-bold text-surface-900 dark:text-surface-100">
            Grant Teaching Authorization
          </h2>
          <p class="text-xs text-surface-500">
            Establish authoritative institutional permission for {{ teacher.fullName }} ({{ teacher.teacherNumber }}).
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1 max-h-[75vh] overflow-y-auto px-1">
      <!-- TD-05 Permissive Gate Notice Banner -->
      <div
        v-if="hasUnverifiedSelected"
        class="p-3 rounded-lg bg-amber-50 dark:bg-amber-950/40 border border-amber-300 dark:border-amber-800 text-amber-900 dark:text-amber-200 text-xs flex items-start gap-2.5"
        data-testid="permissive-warning-banner"
      >
        <AlertTriangle class="w-4 h-4 shrink-0 mt-0.5 text-amber-600 dark:text-amber-400" />
        <div class="space-y-1">
          <p class="font-semibold">
            Permissive Guidance Warning (TD-05):
          </p>
          <p class="text-amber-800 dark:text-amber-300">
            {{ unverifiedCredentialsList.length }} linked credential(s) have not been formally verified.
            This authorization will be granted with an <strong>⚠️ Unverified Credential</strong> warning indicator until verification is completed by the Qualification Verifier.
          </p>
        </div>
      </div>

      <!-- General Error Alert -->
      <div
        v-if="errors.general"
        class="p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
        data-testid="grant-authorization-error"
      >
        {{ errors.general }}
      </div>

      <!-- Form Grid -->
      <form
        class="space-y-3.5"
        data-testid="grant-authorization-form"
        @submit.prevent="handleSubmit"
      >
        <!-- Capability Link (Optional) -->
        <FormField
          v-if="capabilityOptions.length > 0"
          label="Pre-Populate from Registered Teaching Capability"
          hint="Select a capability to auto-fill scope, educational level, and delivery format."
        >
          <Select
            v-model="form.authorizedCapabilityId"
            :options="capabilityOptions"
            option-label="label"
            option-value="value"
            placeholder="Select capability to link (optional)..."
            class="w-full"
            data-testid="select-authorized-capability"
          />
        </FormField>

        <!-- Scope Description -->
        <FormField
          label="Authorization Scope Description"
          required
          :error="errors.scopeDescription"
          hint="Clear description of the teaching remit (e.g. Matematika SD Kelas 4–6 Tatap Muka)."
        >
          <Textarea
            v-model="form.scopeDescription"
            rows="2"
            placeholder="e.g. Guru Kelas Matematika Tingkat Dasar (SD) Semester Ganjil/Genap"
            class="w-full"
            :invalid="Boolean(errors.scopeDescription)"
            data-testid="input-scope-description"
          />
        </FormField>

        <!-- Scope Boundaries: Campus & Program -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Campus Boundary"
            required
            :error="errors.campusScope"
            hint="Permitted campus location"
          >
            <Select
              v-model="form.campusScope"
              :options="campusOptions"
              editable
              placeholder="e.g. Kampus Utama"
              class="w-full"
              :invalid="Boolean(errors.campusScope)"
              data-testid="select-campus-scope"
            />
          </FormField>

          <FormField
            label="Educational Program Scope"
            :error="errors.programScope"
            hint="e.g. Primary School, Junior High, IB DP"
          >
            <InputText
              v-model="form.programScope"
              placeholder="e.g. Primary School"
              class="w-full"
              data-testid="input-program-scope"
            />
          </FormField>
        </div>

        <!-- Level & Delivery Mode -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Grade Level Scope"
            :error="errors.levelScope"
            hint="e.g. Grade 1-3, Kelas 4-6, All"
          >
            <InputText
              v-model="form.levelScope"
              placeholder="e.g. Grade 4-6"
              class="w-full"
              data-testid="input-level-scope"
            />
          </FormField>

          <FormField
            label="Delivery Mode"
            :error="errors.deliveryModeScope"
            hint="Authorized instruction format"
          >
            <Select
              v-model="form.deliveryModeScope"
              :options="deliveryModeOptions"
              option-label="label"
              option-value="value"
              placeholder="Select delivery format..."
              class="w-full"
              data-testid="select-delivery-mode"
            />
          </FormField>
        </div>

        <!-- Effective Start & End Dates -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            label="Effective Start Date"
            required
            :error="errors.effectiveStartDate"
            hint="YYYY-MM-DD format"
          >
            <InputText
              v-model="form.effectiveStartDate"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.effectiveStartDate)"
              data-testid="input-effective-start-date"
            />
          </FormField>

          <FormField
            label="Effective End Date"
            :error="errors.effectiveEndDate"
            hint="Leave blank if permanent / open-ended"
          >
            <InputText
              v-model="form.effectiveEndDate"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.effectiveEndDate)"
              data-testid="input-effective-end-date"
            />
          </FormField>
        </div>

        <!-- Authorization Status -->
        <FormField
          label="Authorization Status"
          required
          :error="errors.status"
          hint="Select conditional authorization if mentoring or co-teaching is required."
        >
          <Select
            v-model="form.status"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            data-testid="select-authorization-status"
          />
        </FormField>

        <!-- ============================================================= -->
        <!-- Supporting Qualification Evidence Checklist                   -->
        <!-- ============================================================= -->
        <div class="space-y-2 pt-2 border-t border-surface-200 dark:border-surface-700">
          <div class="flex items-center justify-between">
            <label class="text-xs font-semibold text-surface-800 dark:text-surface-200 flex items-center gap-1.5">
              <Award class="w-4 h-4 text-primary-500" />
              <span>Supporting Qualification Credentials</span>
            </label>
            <span class="text-[11px] text-surface-500">
              {{ form.linkedEvidenceIds.length }} linked
            </span>
          </div>

          <div
            v-if="credentialsList.length === 0"
            class="p-3 rounded-lg bg-surface-50 dark:bg-surface-800 text-xs text-surface-500 italic text-center"
            data-testid="empty-credentials-notice"
          >
            No qualification evidence records registered yet for this teacher.
          </div>

          <div
            v-else
            class="space-y-1.5 max-h-40 overflow-y-auto p-1"
            data-testid="credentials-checklist"
          >
            <div
              v-for="cred in credentialsList"
              :key="cred.id"
              :class="[
                'p-2.5 rounded-lg border text-xs flex items-center justify-between cursor-pointer transition-colors',
                form.linkedEvidenceIds.includes(cred.id)
                  ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-950/20'
                  : 'border-surface-200 dark:border-surface-700 hover:bg-surface-50 dark:hover:bg-surface-800/60',
              ]"
              :data-testid="`credential-item-${cred.id}`"
              @click="toggleCredential(cred.id)"
            >
              <div class="flex items-center gap-2">
                <input
                  type="checkbox"
                  :checked="form.linkedEvidenceIds.includes(cred.id)"
                  class="rounded text-primary-600 focus:ring-primary-500 h-3.5 w-3.5 cursor-pointer"
                  @click.stop="toggleCredential(cred.id)"
                />
                <div>
                  <span class="font-medium text-surface-900 dark:text-surface-100">
                    {{ cred.title }}
                  </span>
                  <span v-if="cred.credentialIdentifier" class="text-surface-500 font-mono text-[11px] ml-1.5">
                    ({{ cred.credentialIdentifier }})
                  </span>
                </div>
              </div>

              <div class="flex items-center gap-2">
                <StatusPill
                  :status="cred.verificationState"
                  size="sm"
                  :variant="cred.verificationState === 'verified' ? 'emerald' : 'amber'"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- ============================================================= -->
        <!-- Conditional Supervision Fields (Mandatory if under supervision)-->
        <!-- ============================================================= -->
        <div
          v-if="form.status === 'authorized_under_supervision'"
          class="space-y-3 p-3.5 rounded-xl bg-amber-50/50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800/60"
          data-testid="conditional-supervision-section"
        >
          <div class="flex items-center gap-2 text-amber-800 dark:text-amber-300">
            <Users class="w-4 h-4" />
            <h3 class="text-xs font-bold uppercase tracking-wider">
              Mandatory Supervision Conditions (TD-05)
            </h3>
          </div>

          <div
            v-if="errors.supervision"
            class="text-xs text-rose-600 dark:text-rose-400 font-medium"
            data-testid="supervision-error"
          >
            {{ errors.supervision }}
          </div>

          <FormField
            label="Supervision Type"
            required
            :error="errors.supervisionType"
            hint="Primary mentoring modality"
          >
            <Select
              v-model="form.supervisionType"
              :options="supervisionTypeOptions"
              option-label="label"
              option-value="value"
              class="w-full"
              data-testid="select-supervision-type"
            />
          </FormField>

          <FormField
            label="Supervision Description & Requirements"
            required
            :error="errors.supervisionDescription"
            hint="Describe specific mentoring cadence, weekly review deliverables, or milestones."
          >
            <Textarea
              v-model="form.supervisionDescription"
              rows="2"
              placeholder="e.g. Co-teaching twice a week with senior mentor and bi-weekly lesson plan review."
              class="w-full"
              :invalid="Boolean(errors.supervisionDescription)"
              data-testid="input-supervision-description"
            />
          </FormField>

          <FormField
            label="Designated Mentor / Supervising Teacher Reference"
            :error="errors.supervisingTeacherId"
            hint="Designated senior teacher or mentor"
          >
            <InputText
              v-model="form.supervisingTeacherId"
              placeholder="e.g. Budi Hartono, M.Pd. or Teacher ID"
              class="w-full"
              data-testid="input-supervising-teacher"
            />
          </FormField>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <FormField
              label="Supervision Start Date"
              required
              :error="errors.supervisionStartDate"
            >
              <InputText
                v-model="form.supervisionStartDate"
                type="date"
                class="w-full"
                data-testid="input-supervision-start-date"
              />
            </FormField>

            <FormField
              label="Supervision Target End Date"
              :error="errors.supervisionEndDate"
            >
              <InputText
                v-model="form.supervisionEndDate"
                type="date"
                class="w-full"
                data-testid="input-supervision-end-date"
              />
            </FormField>
          </div>
        </div>

        <!-- Optional Conditions or Restrictions -->
        <FormField
          label="Conditions or Operational Restrictions"
          :error="errors.conditionsOrRestrictions"
          hint="Any additional institutional limitations (e.g. maximum teaching load, specific classrooms)."
        >
          <Textarea
            v-model="form.conditionsOrRestrictions"
            rows="2"
            placeholder="e.g. Limited to maximum 16 teaching hours per week."
            class="w-full"
            data-testid="input-conditions"
          />
        </FormField>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-200 dark:border-surface-700">
        <Button
          label="Cancel"
          severity="secondary"
          variant="text"
          size="small"
          :disabled="grantMutation.isPending.value"
          data-testid="cancel-grant-button"
          @click="handleClose"
        />
        <Button
          label="Grant Authorization"
          icon="pi pi-check"
          severity="primary"
          size="small"
          :loading="grantMutation.isPending.value"
          data-testid="submit-grant-button"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
