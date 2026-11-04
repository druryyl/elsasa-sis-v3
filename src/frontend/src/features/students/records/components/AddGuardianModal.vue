<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  Users,
  UserPlus,
  Phone,
  ShieldCheck,
} from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { addGuardianSchema, GUARDIAN_RELATIONSHIP_TYPES } from '../schemas/records.schema';
import { useAddGuardianMutation } from '../composables/useStudentRecordsMutations';
import type { AddGuardianPayload } from '../types/records.types';

const props = defineProps<{
  visible: boolean;
  studentId: string;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved'): void;
}>();

// Form State
const fullName = ref('');
const relationshipType = ref('mother');
const governmentIdType = ref('NIK');
const governmentIdNumber = ref('');
const phone = ref('');
const email = ref('');
const address = ref('');
const occupation = ref('');
const isPrimaryContact = ref(false);
const isEmergencyContact = ref(true);
const isFinancialContact = ref(false);
const isAuthorizedPickup = ref(true);
const notes = ref('');

// Validation Errors
const errors = ref<Record<string, string>>({});

const relationshipOptions = GUARDIAN_RELATIONSHIP_TYPES.map((type) => ({
  label: type.replace('_', ' ').replace(/\b\w/g, (c) => c.toUpperCase()),
  value: type,
}));

const govIdTypeOptions = [
  { label: 'NIK (Nomor Induk Kependudukan)', value: 'NIK' },
  { label: 'Passport', value: 'Passport' },
  { label: 'KITAS / Other', value: 'Other' },
];

function resetForm() {
  fullName.value = '';
  relationshipType.value = 'mother';
  governmentIdType.value = 'NIK';
  governmentIdNumber.value = '';
  phone.value = '';
  email.value = '';
  address.value = '';
  occupation.value = '';
  isPrimaryContact.value = false;
  isEmergencyContact.value = true;
  isFinancialContact.value = false;
  isAuthorizedPickup.value = true;
  notes.value = '';
  errors.value = {};
}

watch(
  () => props.visible,
  (newVisible) => {
    if (newVisible) {
      resetForm();
    }
  },
);

function handleClose() {
  emit('update:visible', false);
}

// Mutation
const studentIdRef = computed(() => props.studentId);
const { mutateAsync: addGuardian, isPending } = useAddGuardianMutation(studentIdRef);

async function handleSubmit() {
  errors.value = {};

  const payload: AddGuardianPayload = {
    fullName: fullName.value.trim(),
    relationshipType: relationshipType.value,
    governmentIdType: governmentIdType.value.trim() || null,
    governmentIdNumber: governmentIdNumber.value.trim() || null,
    phone: phone.value.trim() || null,
    email: email.value.trim() || null,
    address: address.value.trim() || null,
    occupation: occupation.value.trim() || null,
    isPrimaryContact: isPrimaryContact.value,
    isEmergencyContact: isEmergencyContact.value,
    isFinancialContact: isFinancialContact.value,
    isAuthorizedPickup: isAuthorizedPickup.value,
    notes: notes.value.trim() || null,
  };

  const validation = addGuardianSchema.safeParse(payload);
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
    await addGuardian(payload);
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
    data-testid="add-guardian-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-xs">
          <UserPlus class="w-5 h-5" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2.5">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Add Guardian Relationship
            </h3>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800">
              Custody Registry
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Register a parent or legal guardian and designate custodial and operational responsibilities.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-5 pt-3 pb-1" @submit.prevent="handleSubmit">
      <!-- Section 1: Guardian Biographical Identity -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <Users class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Guardian Biographical Identity</span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <!-- Guardian Full Name -->
          <div class="md:col-span-2">
            <FormField
              label="Guardian Full Legal Name"
              required
              :error="errors.fullName"
              description="Full legal name of parent or designated guardian matching official identity card."
            >
              <InputText
                v-model="fullName"
                placeholder="e.g. Siti Nurhaliza"
                class="w-full text-sm"
                :invalid="Boolean(errors.fullName)"
                data-testid="input-guardian-name"
              />
            </FormField>
          </div>

          <!-- Relationship Type -->
          <div>
            <FormField label="Relationship Type" required :error="errors.relationshipType">
              <Select
                v-model="relationshipType"
                :options="relationshipOptions"
                option-label="label"
                option-value="value"
                placeholder="Select Relationship"
                class="w-full text-sm"
                :invalid="Boolean(errors.relationshipType)"
                data-testid="select-relationship-type"
              />
            </FormField>
          </div>

          <!-- Occupation -->
          <div>
            <FormField label="Occupation / Profession" :error="errors.occupation">
              <InputText
                v-model="occupation"
                placeholder="e.g. Accountant, Doctor, Business Owner"
                class="w-full text-sm"
                :invalid="Boolean(errors.occupation)"
                data-testid="input-guardian-occupation"
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
                class="w-full text-sm"
                :invalid="Boolean(errors.governmentIdType)"
                data-testid="select-guardian-gov-id-type"
              />
            </FormField>
          </div>

          <!-- Government ID Number -->
          <div>
            <FormField label="National ID Number (NIK)" :error="errors.governmentIdNumber">
              <InputText
                v-model="governmentIdNumber"
                placeholder="e.g. 3201010000000002"
                class="w-full text-sm font-mono"
                :invalid="Boolean(errors.governmentIdNumber)"
                data-testid="input-guardian-nik"
              />
            </FormField>
          </div>
        </div>
      </div>

      <!-- Section 2: Contact & Residential Location -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <Phone class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Contact Channels &amp; Residential Location</span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <!-- Contact Phone -->
          <div>
            <FormField label="Contact Phone / WhatsApp" :error="errors.phone">
              <InputText
                v-model="phone"
                placeholder="e.g. +62 812 3456 7890"
                class="w-full text-sm"
                :invalid="Boolean(errors.phone)"
                data-testid="input-guardian-phone"
              />
            </FormField>
          </div>

          <!-- Contact Email -->
          <div>
            <FormField label="Contact Email" :error="errors.email">
              <InputText
                v-model="email"
                type="email"
                placeholder="guardian@example.com"
                class="w-full text-sm"
                :invalid="Boolean(errors.email)"
                data-testid="input-guardian-email"
              />
            </FormField>
          </div>

          <!-- Residential Address -->
          <div class="md:col-span-2">
            <FormField label="Residential Address" :error="errors.address">
              <Textarea
                v-model="address"
                rows="2"
                placeholder="Complete street address or household location."
                class="w-full text-sm leading-relaxed"
                :invalid="Boolean(errors.address)"
                data-testid="input-guardian-address"
              />
            </FormField>
          </div>
        </div>
      </div>

      <!-- Section 3: Operational Authorities & Responsibility Badges -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <ShieldCheck class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Custodial Authorities &amp; Designated Responsibilities</span>
        </div>

        <p class="text-xs text-surface-500 dark:text-surface-400">
          Configure operational rights for communication, emergencies, billing, and campus pickup. Setting primary contact automatically transfers primacy.
        </p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <!-- Primary Contact -->
          <div class="flex items-start gap-3 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-800 shadow-2xs hover:border-emerald-400 transition-colors">
            <Checkbox
              v-model="isPrimaryContact"
              :binary="true"
              input-id="add-primary-contact"
              data-testid="checkbox-primary"
              class="mt-0.5"
            />
            <label for="add-primary-contact" class="text-xs cursor-pointer select-none space-y-1">
              <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-emerald-50 text-emerald-800 dark:bg-emerald-950/70 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800">
                Primary Contact
              </span>
              <span class="text-surface-500 dark:text-surface-400 text-xs block leading-normal">First point of official contact; transfers primacy from previous primary.</span>
            </label>
          </div>

          <!-- Emergency Contact -->
          <div class="flex items-start gap-3 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-800 shadow-2xs hover:border-amber-400 transition-colors">
            <Checkbox
              v-model="isEmergencyContact"
              :binary="true"
              input-id="add-emergency-contact"
              data-testid="checkbox-emergency"
              class="mt-0.5"
            />
            <label for="add-emergency-contact" class="text-xs cursor-pointer select-none space-y-1">
              <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-amber-50 text-amber-800 dark:bg-amber-950/70 dark:text-amber-300 border border-amber-200 dark:border-amber-800">
                Emergency Contact
              </span>
              <span class="text-surface-500 dark:text-surface-400 text-xs block leading-normal">Authorized to receive critical medical and urgent campus notifications.</span>
            </label>
          </div>

          <!-- Financial Contact -->
          <div class="flex items-start gap-3 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-800 shadow-2xs hover:border-blue-400 transition-colors">
            <Checkbox
              v-model="isFinancialContact"
              :binary="true"
              input-id="add-financial-contact"
              data-testid="checkbox-financial"
              class="mt-0.5"
            />
            <label for="add-financial-contact" class="text-xs cursor-pointer select-none space-y-1">
              <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-blue-50 text-blue-800 dark:bg-blue-950/70 dark:text-blue-300 border border-blue-200 dark:border-blue-800">
                Financial Contact
              </span>
              <span class="text-surface-500 dark:text-surface-400 text-xs block leading-normal">Receives tuition invoices, billing announcements, and payment notices.</span>
            </label>
          </div>

          <!-- Authorized Pickup -->
          <div class="flex items-start gap-3 p-3.5 rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-800 shadow-2xs hover:border-purple-400 transition-colors">
            <Checkbox
              v-model="isAuthorizedPickup"
              :binary="true"
              input-id="add-authorized-pickup"
              data-testid="checkbox-pickup"
              class="mt-0.5"
            />
            <label for="add-authorized-pickup" class="text-xs cursor-pointer select-none space-y-1">
              <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-purple-50 text-purple-800 dark:bg-purple-950/70 dark:text-purple-300 border border-purple-200 dark:border-purple-800">
                Authorized Pickup
              </span>
              <span class="text-surface-500 dark:text-surface-400 text-xs block leading-normal">Permitted for school gate student release and custody identity verification.</span>
            </label>
          </div>
        </div>

        <!-- Notes -->
        <div class="pt-2">
          <FormField label="Custody &amp; Administrative Notes" :error="errors.notes">
            <Textarea
              v-model="notes"
              rows="2"
              placeholder="Custody arrangements, legal documentation, court decrees, or special instructions."
              class="w-full text-sm leading-relaxed"
              :invalid="Boolean(errors.notes)"
              data-testid="input-guardian-notes"
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
          data-testid="btn-cancel-guardian"
          class="!px-4 !py-2 !text-xs font-medium cursor-pointer"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          label="Save Guardian Profile"
          icon="pi pi-check"
          :loading="isPending"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs"
          data-testid="btn-save-guardian"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
