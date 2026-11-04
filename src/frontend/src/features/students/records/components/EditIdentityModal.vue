<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  IdCard,
  MapPin,
  ShieldCheck,
  UserCheck,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { updateIdentitySchema } from '../schemas/records.schema';
import { useUpdateStudentIdentityMutation } from '../composables/useStudentRecordsMutations';
import type { StudentDetail, UpdateIdentityPayload } from '../types/records.types';

const props = defineProps<{
  visible: boolean;
  student: StudentDetail;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved'): void;
}>();

// Form State
const fullName = ref('');
const preferredName = ref('');
const dateOfBirth = ref('');
const placeOfBirth = ref('');
const gender = ref('');
const nationality = ref('Indonesian');
const governmentIdType = ref('NIK');
const governmentIdNumber = ref('');
const address = ref('');
const phone = ref('');
const email = ref('');
const isIndependent = ref(false);
const notes = ref('');

// Validation Errors
const errors = ref<Record<string, string>>({});

const genderOptions = [
  { label: 'Male', value: 'Male' },
  { label: 'Female', value: 'Female' },
  { label: 'Other', value: 'Other' },
];

const govIdTypeOptions = [
  { label: 'NIK (Nomor Induk Kependudukan)', value: 'NIK' },
  { label: 'KIA (Kartu Identitas Anak)', value: 'KIA' },
  { label: 'Passport', value: 'Passport' },
  { label: 'KITAS / Other', value: 'Other' },
];

function populateForm() {
  if (!props.student) return;
  fullName.value = props.student.fullName || '';
  preferredName.value = props.student.preferredName || '';
  dateOfBirth.value = props.student.dateOfBirth || '';
  placeOfBirth.value = props.student.placeOfBirth || '';
  gender.value = props.student.gender || '';
  nationality.value = props.student.nationality || 'Indonesian';
  governmentIdType.value = props.student.governmentIdType || 'NIK';
  governmentIdNumber.value = props.student.governmentIdNumber || '';
  address.value = props.student.address || '';
  phone.value = props.student.phone || '';
  email.value = props.student.email || '';
  isIndependent.value = Boolean(props.student.isIndependent);
  notes.value = props.student.notes || '';
  errors.value = {};
}

watch(
  () => [props.visible, props.student],
  ([newVisible]) => {
    if (newVisible) {
      populateForm();
    }
  },
  { immediate: true },
);

function handleClose() {
  emit('update:visible', false);
}

// Mutation
const studentIdRef = computed(() => props.student?.id);
const { mutateAsync: updateIdentity, isPending } = useUpdateStudentIdentityMutation(studentIdRef);

async function handleSubmit() {
  errors.value = {};

  const payload: UpdateIdentityPayload = {
    fullName: fullName.value.trim(),
    preferredName: preferredName.value.trim() || null,
    dateOfBirth: dateOfBirth.value.trim() || null,
    placeOfBirth: placeOfBirth.value.trim() || null,
    gender: gender.value || null,
    nationality: nationality.value.trim() || null,
    governmentIdType: governmentIdType.value.trim() || null,
    governmentIdNumber: governmentIdNumber.value.trim() || null,
    address: address.value.trim() || null,
    phone: phone.value.trim() || null,
    email: email.value.trim() || null,
    isIndependent: isIndependent.value,
    notes: notes.value.trim() || null,
  };

  const validation = updateIdentitySchema.safeParse(payload);
  if (!validation.success) {
    const formatted = validation.error.format();
    for (const [key, val] of Object.entries(formatted)) {
      if (key !== '_errors' && (val as { _errors?: string[] })._errors?.length) {
        errors.value[key] = (val as { _errors: string[] })._errors[0];
      }
    }
    return;
  }

  try {
    await updateIdentity(payload);
    emit('saved');
    handleClose();
  } catch {
    // Toast handles error display
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :style="{ width: '760px' }"
    :breakpoints="{ '960px': '85vw', '640px': '95vw' }"
    data-testid="edit-identity-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-xs">
          <IdCard class="w-5 h-5" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2.5">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Edit Certified Student Identity
            </h3>
            <span v-if="student?.studentNumber" class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800 font-mono">
              {{ student.studentNumber }}
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Update certified biographical, civil identification, and contact records for <strong class="text-surface-700 dark:text-surface-300">{{ student?.fullName || 'student' }}</strong>.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-5 pt-3 pb-1" @submit.prevent="handleSubmit">
      <!-- Section 1: Civil & Biographical Identity -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <UserCheck class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Civil &amp; Biographical Identification</span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <!-- Full Name -->
          <div class="md:col-span-2">
            <FormField
              label="Full Legal Name"
              required
              :error="errors.fullName"
              description="Authoritative full name matching certified birth record / government ID."
            >
              <InputText
                v-model="fullName"
                placeholder="e.g. Ahmad Faiz Pratama"
                class="w-full text-sm"
                :invalid="Boolean(errors.fullName)"
                data-testid="input-fullname"
              />
            </FormField>
          </div>

          <!-- Preferred Name -->
          <div>
            <FormField label="Preferred / Calling Name" :error="errors.preferredName">
              <InputText
                v-model="preferredName"
                placeholder="e.g. Faiz"
                class="w-full text-sm"
                :invalid="Boolean(errors.preferredName)"
                data-testid="input-preferred-name"
              />
            </FormField>
          </div>

          <!-- Gender -->
          <div>
            <FormField label="Legal Gender" :error="errors.gender">
              <Select
                v-model="gender"
                :options="genderOptions"
                option-label="label"
                option-value="value"
                placeholder="Select Gender"
                class="w-full text-sm"
                :invalid="Boolean(errors.gender)"
                data-testid="select-gender"
              />
            </FormField>
          </div>

          <!-- Date of Birth -->
          <div>
            <FormField
              label="Date of Birth"
              :error="errors.dateOfBirth"
              description="Format: YYYY-MM-DD"
            >
              <InputText
                v-model="dateOfBirth"
                placeholder="2012-05-15"
                class="w-full text-sm font-mono"
                :invalid="Boolean(errors.dateOfBirth)"
                data-testid="input-dob"
              />
            </FormField>
          </div>

          <!-- Place of Birth -->
          <div>
            <FormField label="Place of Birth" :error="errors.placeOfBirth">
              <InputText
                v-model="placeOfBirth"
                placeholder="e.g. Jakarta"
                class="w-full text-sm"
                :invalid="Boolean(errors.placeOfBirth)"
                data-testid="input-pob"
              />
            </FormField>
          </div>

          <!-- Government ID Type -->
          <div>
            <FormField label="Government ID Type" :error="errors.governmentIdType">
              <Select
                v-model="governmentIdType"
                :options="govIdTypeOptions"
                option-label="label"
                option-value="value"
                placeholder="Select ID Type"
                class="w-full text-sm"
                :invalid="Boolean(errors.governmentIdType)"
                data-testid="select-gov-id-type"
              />
            </FormField>
          </div>

          <!-- Government ID Number (NIK) -->
          <div>
            <FormField label="Government ID Number (NIK / Passport)" :error="errors.governmentIdNumber">
              <InputText
                v-model="governmentIdNumber"
                placeholder="e.g. 3171010000000001"
                class="w-full text-sm font-mono"
                :invalid="Boolean(errors.governmentIdNumber)"
                data-testid="input-nik"
              />
            </FormField>
          </div>

          <!-- Nationality -->
          <div class="md:col-span-2">
            <FormField label="Nationality" :error="errors.nationality">
              <InputText
                v-model="nationality"
                placeholder="Indonesian"
                class="w-full text-sm"
                :invalid="Boolean(errors.nationality)"
                data-testid="input-nationality"
              />
            </FormField>
          </div>
        </div>
      </div>

      <!-- Section 2: Contact Information & Address -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <MapPin class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Contact Information &amp; Residential Location</span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <!-- Personal Phone -->
          <div>
            <FormField label="Student Phone / WhatsApp" :error="errors.phone">
              <InputText
                v-model="phone"
                placeholder="e.g. +62 812 3456 7890"
                class="w-full text-sm"
                :invalid="Boolean(errors.phone)"
                data-testid="input-phone"
              />
            </FormField>
          </div>

          <!-- Personal Email -->
          <div>
            <FormField label="Student Email" :error="errors.email">
              <InputText
                v-model="email"
                placeholder="student@example.com"
                type="email"
                class="w-full text-sm"
                :invalid="Boolean(errors.email)"
                data-testid="input-email"
              />
            </FormField>
          </div>

          <!-- Residential Address -->
          <div class="md:col-span-2">
            <FormField label="Residential Address" :error="errors.address">
              <Textarea
                v-model="address"
                rows="2"
                placeholder="Complete street address, RT/RW, kelurahan, etc."
                class="w-full text-sm leading-relaxed"
                :invalid="Boolean(errors.address)"
                data-testid="input-address"
              />
            </FormField>
          </div>
        </div>
      </div>

      <!-- Section 3: Governance & Independent Learner -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <ShieldCheck class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Governance &amp; Administrative Classification</span>
        </div>

        <!-- Independent Learner Toggle -->
        <div class="p-3.5 rounded-lg border border-teal-200 dark:border-teal-800/80 bg-teal-50/50 dark:bg-teal-950/20">
          <div class="flex items-start gap-3">
            <Checkbox
              v-model="isIndependent"
              :binary="true"
              input-id="is-independent-checkbox"
              data-testid="checkbox-independent"
              class="mt-0.5"
            />
            <label for="is-independent-checkbox" class="text-xs cursor-pointer select-none space-y-1">
              <span class="font-semibold text-surface-900 dark:text-surface-100 block">
                Independent Adult Learner
              </span>
              <span class="text-surface-600 dark:text-surface-400 block leading-normal">
                Enables self-managed administrative status. Independent students do not require primary guardian sign-off for attendance, decisions, or approvals.
              </span>
            </label>
          </div>
        </div>

        <!-- Notes -->
        <div>
          <FormField label="Administrative &amp; Audit Notes" :error="errors.notes">
            <Textarea
              v-model="notes"
              rows="2"
              placeholder="Internal record governance or identity verification remarks."
              class="w-full text-sm leading-relaxed"
              :invalid="Boolean(errors.notes)"
              data-testid="input-notes"
            />
          </FormField>
        </div>
      </div>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-3 pt-3 border-t border-surface-200 dark:border-surface-800">
        <Button
          size="small"
          variant="outlined"
          severity="secondary"
          label="Cancel"
          :disabled="isPending"
          data-testid="btn-cancel-identity"
          class="!px-4 !py-2 !text-xs font-medium cursor-pointer"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          label="Save Identity Changes"
          icon="pi pi-check"
          :loading="isPending"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs"
          data-testid="btn-save-identity"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
