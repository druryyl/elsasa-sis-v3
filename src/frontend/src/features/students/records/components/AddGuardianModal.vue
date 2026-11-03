<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
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
    header="Add Guardian Relationship"
    class="w-full max-w-2xl"
    data-testid="add-guardian-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 py-2" @submit.prevent="handleSubmit">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Guardian Full Name -->
        <div class="md:col-span-2">
          <FormField
            label="Guardian Full Legal Name"
            required
            :error="errors.fullName"
            description="Full legal name of parent or designated guardian."
          >
            <InputText
              v-model="fullName"
              placeholder="e.g. Siti Nurhaliza"
              class="w-full"
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
              class="w-full"
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
              class="w-full"
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
              class="w-full"
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
              class="w-full font-mono"
              :invalid="Boolean(errors.governmentIdNumber)"
              data-testid="input-guardian-nik"
            />
          </FormField>
        </div>

        <!-- Contact Phone -->
        <div>
          <FormField label="Contact Phone / WhatsApp" :error="errors.phone">
            <InputText
              v-model="phone"
              placeholder="e.g. +62 812 3456 7890"
              class="w-full"
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
              class="w-full"
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
              class="w-full"
              :invalid="Boolean(errors.address)"
              data-testid="input-guardian-address"
            />
          </FormField>
        </div>

        <!-- Operational Authorities & Responsibility Badges -->
        <div class="md:col-span-2 p-3 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/60 dark:bg-surface-800/40 space-y-3">
          <span class="text-xs font-semibold text-surface-900 dark:text-surface-100 block">
            Operational Authorities &amp; Responsibilities
          </span>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isPrimaryContact"
                :binary="true"
                input-id="add-primary-contact"
                data-testid="checkbox-primary"
              />
              <label for="add-primary-contact" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-emerald-700 dark:text-emerald-400 block">Primary Contact</span>
                <span class="text-surface-500 text-[11px] block">First point of contact; demotes previous primary</span>
              </label>
            </div>

            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isEmergencyContact"
                :binary="true"
                input-id="add-emergency-contact"
                data-testid="checkbox-emergency"
              />
              <label for="add-emergency-contact" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-amber-700 dark:text-amber-400 block">Emergency Contact</span>
                <span class="text-surface-500 text-[11px] block">Authorized to receive critical medical alerts</span>
              </label>
            </div>

            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isFinancialContact"
                :binary="true"
                input-id="add-financial-contact"
                data-testid="checkbox-financial"
              />
              <label for="add-financial-contact" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-blue-700 dark:text-blue-400 block">Financial Contact</span>
                <span class="text-surface-500 text-[11px] block">Receives invoices, tuition statements, billing notices</span>
              </label>
            </div>

            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isAuthorizedPickup"
                :binary="true"
                input-id="add-authorized-pickup"
                data-testid="checkbox-pickup"
              />
              <label for="add-authorized-pickup" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-purple-700 dark:text-purple-400 block">Authorized Pickup</span>
                <span class="text-surface-500 text-[11px] block">Permitted for school gate pickup identity verification</span>
              </label>
            </div>
          </div>
        </div>

        <!-- Notes -->
        <div class="md:col-span-2">
          <FormField label="Administrative Notes" :error="errors.notes">
            <Textarea
              v-model="notes"
              rows="2"
              placeholder="Custody arrangements, legal documentation, or special instructions."
              class="w-full"
              :invalid="Boolean(errors.notes)"
              data-testid="input-guardian-notes"
            />
          </FormField>
        </div>
      </div>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          size="small"
          variant="text"
          severity="secondary"
          label="Cancel"
          :disabled="isPending"
          data-testid="btn-cancel-guardian"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          label="Save Guardian Profile"
          :loading="isPending"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700"
          data-testid="btn-save-guardian"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
