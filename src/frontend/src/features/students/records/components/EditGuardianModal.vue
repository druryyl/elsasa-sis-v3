<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import { FormField } from '@/shared/components';
import { updateGuardianSchema, GUARDIAN_RELATIONSHIP_TYPES } from '../schemas/records.schema';
import { useUpdateGuardianMutation } from '../composables/useStudentRecordsMutations';
import type { GuardianRelationshipItem, UpdateGuardianPayload } from '../types/records.types';

const props = defineProps<{
  visible: boolean;
  studentId: string;
  guardian: GuardianRelationshipItem | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved'): void;
}>();

// Form State
const relationshipType = ref('mother');
const phone = ref('');
const email = ref('');
const address = ref('');
const occupation = ref('');
const isPrimaryContact = ref(false);
const isEmergencyContact = ref(false);
const isFinancialContact = ref(false);
const isAuthorizedPickup = ref(false);
const isActive = ref(true);
const notes = ref('');

// Validation Errors
const errors = ref<Record<string, string>>({});

const relationshipOptions = GUARDIAN_RELATIONSHIP_TYPES.map((type) => ({
  label: type.replace('_', ' ').replace(/\b\w/g, (c) => c.toUpperCase()),
  value: type,
}));

function populateForm() {
  if (!props.guardian) return;
  relationshipType.value = props.guardian.relationshipType || 'mother';
  phone.value = props.guardian.phone || '';
  email.value = props.guardian.email || '';
  address.value = props.guardian.address || '';
  occupation.value = props.guardian.occupation || '';
  isPrimaryContact.value = Boolean(props.guardian.isPrimaryContact);
  isEmergencyContact.value = Boolean(props.guardian.isEmergencyContact);
  isFinancialContact.value = Boolean(props.guardian.isFinancialContact);
  isAuthorizedPickup.value = Boolean(props.guardian.isAuthorizedPickup);
  isActive.value = props.guardian.isActive ?? true;
  notes.value = '';
  errors.value = {};
}

watch(
  () => [props.visible, props.guardian],
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
const studentIdRef = computed(() => props.studentId);
const { mutateAsync: updateGuardian, isPending } = useUpdateGuardianMutation(studentIdRef);

async function handleSubmit() {
  if (!props.guardian) return;
  errors.value = {};

  const payload: UpdateGuardianPayload = {
    relationshipType: relationshipType.value,
    phone: phone.value.trim() || null,
    email: email.value.trim() || null,
    address: address.value.trim() || null,
    occupation: occupation.value.trim() || null,
    isPrimaryContact: isPrimaryContact.value,
    isEmergencyContact: isEmergencyContact.value,
    isFinancialContact: isFinancialContact.value,
    isAuthorizedPickup: isAuthorizedPickup.value,
    isActive: isActive.value,
    notes: notes.value.trim() || null,
  };

  const validation = updateGuardianSchema.safeParse(payload);
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
    await updateGuardian({
      relationshipId: props.guardian.relationshipId,
      payload,
    });
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
    :header="`Edit Responsibilities — ${guardian?.fullName || 'Guardian'}`"
    class="w-full max-w-xl"
    data-testid="edit-guardian-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 py-2" @submit.prevent="handleSubmit">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Relationship Type -->
        <div>
          <FormField label="Relationship Type" required :error="errors.relationshipType">
            <Select
              v-model="relationshipType"
              :options="relationshipOptions"
              option-label="label"
              option-value="value"
              class="w-full"
              :invalid="Boolean(errors.relationshipType)"
              data-testid="select-edit-relationship-type"
            />
          </FormField>
        </div>

        <!-- Occupation -->
        <div>
          <FormField label="Occupation" :error="errors.occupation">
            <InputText
              v-model="occupation"
              placeholder="e.g. Architect"
              class="w-full"
              :invalid="Boolean(errors.occupation)"
              data-testid="input-edit-guardian-occupation"
            />
          </FormField>
        </div>

        <!-- Phone -->
        <div>
          <FormField label="Phone / WhatsApp" :error="errors.phone">
            <InputText
              v-model="phone"
              class="w-full"
              :invalid="Boolean(errors.phone)"
              data-testid="input-edit-guardian-phone"
            />
          </FormField>
        </div>

        <!-- Email -->
        <div>
          <FormField label="Email" :error="errors.email">
            <InputText
              v-model="email"
              type="email"
              class="w-full"
              :invalid="Boolean(errors.email)"
              data-testid="input-edit-guardian-email"
            />
          </FormField>
        </div>

        <!-- Address -->
        <div class="md:col-span-2">
          <FormField label="Address" :error="errors.address">
            <Textarea
              v-model="address"
              rows="2"
              class="w-full"
              :invalid="Boolean(errors.address)"
              data-testid="input-edit-guardian-address"
            />
          </FormField>
        </div>

        <!-- Responsibility Badges -->
        <div class="md:col-span-2 p-3 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/60 dark:bg-surface-800/40 space-y-3">
          <span class="text-xs font-semibold text-surface-900 dark:text-surface-100 block">
            Designated Responsibilities &amp; Contact Roles
          </span>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isPrimaryContact"
                :binary="true"
                input-id="edit-primary-contact"
                data-testid="checkbox-edit-primary"
              />
              <label for="edit-primary-contact" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-emerald-700 dark:text-emerald-400 block">Primary Contact</span>
                <span class="text-surface-500 text-[11px] block">Transfers primary designation to this guardian</span>
              </label>
            </div>

            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isEmergencyContact"
                :binary="true"
                input-id="edit-emergency-contact"
                data-testid="checkbox-edit-emergency"
              />
              <label for="edit-emergency-contact" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-amber-700 dark:text-amber-400 block">Emergency Contact</span>
                <span class="text-surface-500 text-[11px] block">Authorized emergency contact</span>
              </label>
            </div>

            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isFinancialContact"
                :binary="true"
                input-id="edit-financial-contact"
                data-testid="checkbox-edit-financial"
              />
              <label for="edit-financial-contact" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-blue-700 dark:text-blue-400 block">Financial Contact</span>
                <span class="text-surface-500 text-[11px] block">Receives invoices &amp; fee statements</span>
              </label>
            </div>

            <div class="flex items-center gap-2">
              <Checkbox
                v-model="isAuthorizedPickup"
                :binary="true"
                input-id="edit-authorized-pickup"
                data-testid="checkbox-edit-pickup"
              />
              <label for="edit-authorized-pickup" class="text-xs cursor-pointer select-none">
                <span class="font-medium text-purple-700 dark:text-purple-400 block">Authorized Pickup</span>
                <span class="text-surface-500 text-[11px] block">Authorized for pickup clearance</span>
              </label>
            </div>
          </div>
        </div>

        <!-- Relationship Active Status -->
        <div class="md:col-span-2 flex items-center gap-2">
          <Checkbox
            v-model="isActive"
            :binary="true"
            input-id="edit-active-status"
            data-testid="checkbox-edit-active"
          />
          <label for="edit-active-status" class="text-xs cursor-pointer select-none">
            <span class="font-medium text-surface-800 dark:text-surface-200">Active Relationship</span>
            <span class="text-surface-500 text-[11px] block">Inactive guardians are retained for historical audit only</span>
          </label>
        </div>

        <!-- Notes -->
        <div class="md:col-span-2">
          <FormField label="Notes" :error="errors.notes">
            <Textarea
              v-model="notes"
              rows="2"
              placeholder="Administrative remarks..."
              class="w-full"
              :invalid="Boolean(errors.notes)"
              data-testid="input-edit-guardian-notes"
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
          data-testid="btn-cancel-edit-guardian"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          label="Save Responsibilities"
          :loading="isPending"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700"
          data-testid="btn-save-edit-guardian"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
