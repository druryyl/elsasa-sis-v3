<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from 'primevue/button';
import {
  Users,
  UserPlus,
  Phone,
  Mail,
  MapPin,
  Pencil,
  CheckCircle,
} from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
import { useUpdateGuardianMutation } from '../composables/useStudentRecordsMutations';
import type { StudentDetail, GuardianRelationshipItem } from '../types/records.types';
import AddGuardianModal from './AddGuardianModal.vue';
import EditGuardianModal from './EditGuardianModal.vue';

const props = defineProps<{
  student: StudentDetail;
  canEdit?: boolean;
}>();

const emit = defineEmits<{
  (e: 'addGuardian'): void;
}>();

const isAddModalOpen = ref(false);
const isEditModalOpen = ref(false);
const selectedGuardianForEdit = ref<GuardianRelationshipItem | null>(null);

function handleOpenAdd() {
  isAddModalOpen.value = true;
  emit('addGuardian');
}

function handleOpenEdit(guardian: GuardianRelationshipItem) {
  selectedGuardianForEdit.value = guardian;
  isEditModalOpen.value = true;
}

// Transfer Primary Contact directly
const studentIdRef = computed(() => props.student.id);
const { mutateAsync: updateGuardian, isPending: isUpdatingPrimary } = useUpdateGuardianMutation(studentIdRef);

async function handleSetPrimary(guardian: GuardianRelationshipItem) {
  try {
    await updateGuardian({
      relationshipId: guardian.relationshipId,
      payload: {
        relationshipType: guardian.relationshipType,
        isPrimaryContact: true,
        isEmergencyContact: guardian.isEmergencyContact,
        isFinancialContact: guardian.isFinancialContact,
        isAuthorizedPickup: guardian.isAuthorizedPickup,
        isActive: guardian.isActive,
        phone: guardian.phone,
        email: guardian.email,
        address: guardian.address,
        occupation: guardian.occupation,
      },
    });
  } catch {
    // Handled by toast notification
  }
}
</script>

<template>
  <div class="space-y-4" data-testid="student-guardians-tab">
    <!-- Header with Action -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <p class="text-xs text-surface-500">
        Associated parent and guardian profiles with operational responsibility authorities.
      </p>
      <Button
        v-if="canEdit"
        size="small"
        severity="primary"
        class="!text-xs cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700 self-start sm:self-auto"
        data-testid="tab-add-guardian-button"
        @click="handleOpenAdd"
      >
        <UserPlus class="w-3.5 h-3.5 mr-1" />
        <span>Add Guardian</span>
      </Button>
    </div>

    <!-- Empty State -->
    <div
      v-if="!student.guardianRelationships || student.guardianRelationships.length === 0"
      data-testid="empty-guardians"
    >
      <EmptyState
        title="No guardians on record"
        description="This student currently has no guardian relationships configured."
        size="sm"
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center mx-auto mb-2 text-surface-400">
            <Users class="w-6 h-6" />
          </div>
        </template>
      </EmptyState>
    </div>

    <!-- Guardian Cards List -->
    <div v-else class="grid grid-cols-1 gap-3" data-testid="guardians-list">
      <div
        v-for="rel in student.guardianRelationships"
        :key="rel.relationshipId"
        class="p-4 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/40 space-y-3"
        :data-testid="`guardian-card-${rel.relationshipId}`"
      >
        <!-- Top Row: Name, Relationship & Authority Badges -->
        <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2">
          <div>
            <div class="flex items-center gap-2">
              <span class="font-bold text-sm text-surface-900 dark:text-surface-100" data-testid="guardian-name">
                {{ rel.fullName }}
              </span>
              <span
                class="capitalize text-xs font-medium text-surface-600 dark:text-surface-400 bg-surface-200 dark:bg-surface-700 px-2 py-0.5 rounded"
                data-testid="guardian-relationship-type"
              >
                {{ rel.relationshipType.replace('_', ' ') }}
              </span>
              <span
                v-if="!rel.isActive"
                class="text-[10px] font-semibold text-rose-700 dark:text-rose-300 bg-rose-50 dark:bg-rose-950 px-1.5 py-0.5 rounded border border-rose-200 dark:border-rose-800"
              >
                Inactive
              </span>
            </div>
            <span v-if="rel.occupation" class="text-xs text-surface-500 block">
              {{ rel.occupation }}
            </span>
            <span v-if="rel.governmentIdNumber" class="text-[11px] font-mono text-surface-400 block">
              {{ rel.governmentIdType || 'NIK' }}: {{ rel.governmentIdNumber }}
            </span>
          </div>

          <!-- Authority Badges -->
          <div class="flex flex-wrap items-center gap-1.5" data-testid="guardian-badges">
            <span
              v-if="rel.isPrimaryContact"
              class="text-[11px] font-semibold text-emerald-700 dark:text-emerald-300 bg-emerald-50 dark:bg-emerald-950 px-2 py-0.5 rounded-full border border-emerald-300 dark:border-emerald-800 flex items-center gap-1"
              data-testid="badge-primary-contact"
            >
              ★ Primary Contact
            </span>
            <span
              v-if="rel.isEmergencyContact"
              class="text-[11px] font-semibold text-amber-700 dark:text-amber-300 bg-amber-50 dark:bg-amber-950 px-2 py-0.5 rounded-full border border-amber-300 dark:border-amber-800"
              data-testid="badge-emergency-contact"
            >
              Emergency
            </span>
            <span
              v-if="rel.isFinancialContact"
              class="text-[11px] font-semibold text-blue-700 dark:text-blue-300 bg-blue-50 dark:bg-blue-950 px-2 py-0.5 rounded-full border border-blue-300 dark:border-blue-800"
              data-testid="badge-financial-contact"
            >
              Financial
            </span>
            <span
              v-if="rel.isAuthorizedPickup"
              class="text-[11px] font-semibold text-purple-700 dark:text-purple-300 bg-purple-50 dark:bg-purple-950 px-2 py-0.5 rounded-full border border-purple-300 dark:border-purple-800"
              data-testid="badge-pickup-contact"
            >
              Pickup
            </span>
          </div>
        </div>

        <!-- Contact Information Details -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs text-surface-600 dark:text-surface-400 pt-2 border-t border-surface-200/60 dark:border-surface-700/60">
          <span class="flex items-center gap-1.5" data-testid="guardian-phone">
            <Phone class="w-3.5 h-3.5 text-surface-400 shrink-0" />
            {{ rel.phone || 'No phone recorded' }}
          </span>
          <span class="flex items-center gap-1.5" data-testid="guardian-email">
            <Mail class="w-3.5 h-3.5 text-surface-400 shrink-0" />
            {{ rel.email || 'No email recorded' }}
          </span>
          <span v-if="rel.address" class="sm:col-span-2 flex items-center gap-1.5" data-testid="guardian-address">
            <MapPin class="w-3.5 h-3.5 text-surface-400 shrink-0" />
            {{ rel.address }}
          </span>
        </div>

        <!-- Actions: Primary Contact Transfer & Edit Responsibilities -->
        <div v-if="canEdit" class="flex flex-wrap items-center justify-end gap-2 pt-2 border-t border-surface-200/60 dark:border-surface-700/60">
          <Button
            v-if="!rel.isPrimaryContact"
            size="small"
            variant="text"
            severity="secondary"
            class="!text-xs cursor-pointer !text-emerald-700 hover:!bg-emerald-50 dark:hover:!bg-emerald-950"
            :disabled="isUpdatingPrimary"
            data-testid="btn-set-primary"
            @click="handleSetPrimary(rel)"
          >
            <CheckCircle class="w-3.5 h-3.5 mr-1" />
            <span>Set as Primary Contact</span>
          </Button>

          <Button
            size="small"
            variant="outlined"
            severity="secondary"
            class="!text-xs cursor-pointer"
            data-testid="btn-edit-guardian"
            @click="handleOpenEdit(rel)"
          >
            <Pencil class="w-3.5 h-3.5 mr-1 text-teal-600" />
            <span>Edit Responsibilities</span>
          </Button>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <AddGuardianModal
      v-model:visible="isAddModalOpen"
      :student-id="student.id"
    />

    <EditGuardianModal
      v-model:visible="isEditModalOpen"
      :student-id="student.id"
      :guardian="selectedGuardianForEdit"
    />
  </div>
</template>
