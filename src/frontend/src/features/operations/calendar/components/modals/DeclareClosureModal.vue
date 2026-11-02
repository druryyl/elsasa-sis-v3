<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { AlertTriangle, ShieldAlert } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  useDeclareSchoolClosureMutation,
  type SchoolClosureDto,
} from '../../composables/useSchoolClosuresQuery';
import type {
  ClosureReasonCategory,
  AffectedPopulation,
} from '../../schemas/schoolClosure.schema';

const props = defineProps<{
  visible: boolean;
  academicYearId?: string | null | undefined;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  declared: [closure: SchoolClosureDto];
}>();

const authStore = useAuthStore();

const reasonCategoryOptions: { value: ClosureReasonCategory; label: string }[] = [
  { value: 'weather_disaster', label: 'Weather Emergency / Natural Disaster' },
  { value: 'infrastructure_hazard', label: 'Infrastructure Failure / Safety Hazard' },
  { value: 'health_safety', label: 'Public Health & Safety Directive' },
  { value: 'government_mandate', label: 'Government / Ministry Mandate' },
  { value: 'institutional_policy', label: 'Institutional Policy / Board Action' },
  { value: 'emergency', label: 'Emergency Incident' },
];

const affectedPopulationOptions: { value: AffectedPopulation; label: string }[] = [
  { value: 'all', label: 'All Campus (Students & Staff)' },
  { value: 'students_only', label: 'Students Only' },
  { value: 'staff_and_students', label: 'Staff & Students' },
];

// Form fields
const reasonCategory = ref<ClosureReasonCategory>('weather_disaster');
const reasonDetails = ref('');
const startDate = ref('');
const endDate = ref('');
const affectedCampus = ref('All');
const affectedPopulation = ref<AffectedPopulation>('all');
const recoveryPlanNotes = ref('');
const formError = ref<string | null>(null);

function resetForm(): void {
  reasonCategory.value = 'weather_disaster';
  reasonDetails.value = '';
  startDate.value = '';
  endDate.value = '';
  affectedCampus.value = 'All';
  affectedPopulation.value = 'all';
  recoveryPlanNotes.value = '';
  formError.value = null;
}

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      resetForm();
    }
  },
);

const isModalVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const declareMutation = useDeclareSchoolClosureMutation({
  onSuccess: (data) => {
    emit('declared', data);
    isModalVisible.value = false;
    resetForm();
  },
  onError: (error: unknown) => {
    formError.value = error instanceof Error ? error.message : 'Failed to declare campus closure.';
  },
});

function handleSubmit(): void {
  formError.value = null;

  if (!props.academicYearId) {
    formError.value = 'An Academic Year must be selected before declaring a closure.';
    return;
  }

  if (!startDate.value || !endDate.value) {
    formError.value = 'Both start date and end date are required.';
    return;
  }

  if (endDate.value < startDate.value) {
    formError.value = 'End date cannot be earlier than start date.';
    return;
  }

  if (!reasonDetails.value || reasonDetails.value.trim().length < 5) {
    formError.value = 'Detailed justification reason must be at least 5 characters.';
    return;
  }

  declareMutation.mutate({
    academicYearId: props.academicYearId,
    reasonCategory: reasonCategory.value,
    reasonDetails: reasonDetails.value.trim(),
    startDate: startDate.value,
    endDate: endDate.value,
    affectedCampus: affectedCampus.value.trim() || 'All',
    affectedPopulation: affectedPopulation.value,
    authorizedBy: authStore.user?.username ?? 'Operations Authority',
    recoveryPlanNotes: recoveryPlanNotes.value.trim() || null,
  });
}

function handleClose(): void {
  isModalVisible.value = false;
}
</script>

<template>
  <Dialog
    v-model:visible="isModalVisible"
    header="Declare Campus Closure"
    modal
    append-to="self"
    class="max-w-lg w-full"
    data-testid="declare-closure-modal"
  >
    <form class="space-y-4 pt-2" @submit.prevent="handleSubmit">
      <!-- Information Notice -->
      <div
        class="p-3 bg-amber-50 border border-amber-200 rounded-md flex items-start gap-2.5 text-xs text-amber-800"
        data-testid="closure-warning-banner"
      >
        <ShieldAlert class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
        <div>
          <span class="font-semibold">Authoritative Schedule Impact:</span>
          Declaring a campus closure will atomically mark all affected dates non-instructional,
          record a versioned calendar amendment if published, and alert operational schedules.
        </div>
      </div>

      <!-- Error Alert -->
      <div
        v-if="formError"
        class="p-3 bg-rose-50 border border-rose-200 rounded-md flex items-center gap-2 text-xs text-rose-700"
        data-testid="closure-error-banner"
      >
        <AlertTriangle class="w-4 h-4 text-rose-500 shrink-0" />
        <span>{{ formError }}</span>
      </div>

      <!-- Reason Category -->
      <FormField
        id="closure-category"
        label="Reason Category"
        required
        hint="Institutional classification of the closure"
      >
        <select
          id="closure-category"
          v-model="reasonCategory"
          required
          class="w-full h-9 px-3 py-1.5 text-sm border border-slate-300 rounded-md focus:outline-none focus:ring-2 focus:ring-rose-500 bg-white"
          data-testid="modal-closure-category-select"
        >
          <option
            v-for="opt in reasonCategoryOptions"
            :key="opt.value"
            :value="opt.value"
          >
            {{ opt.label }}
          </option>
        </select>
      </FormField>

      <!-- Date Range -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField id="closure-start" label="Start Date" required>
          <InputText
            id="closure-start"
            v-model="startDate"
            type="date"
            required
            class="w-full text-sm"
            data-testid="modal-closure-start-input"
          />
        </FormField>
        <FormField id="closure-end" label="End Date" required>
          <InputText
            id="closure-end"
            v-model="endDate"
            type="date"
            required
            class="w-full text-sm"
            data-testid="modal-closure-end-input"
          />
        </FormField>
      </div>

      <!-- Campus and Population Scope -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <FormField
          id="closure-campus"
          label="Affected Campus"
          hint="e.g. All, Campus A, Primary Campus"
        >
          <InputText
            id="closure-campus"
            v-model="affectedCampus"
            placeholder="All"
            class="w-full text-sm"
            data-testid="modal-closure-campus-input"
          />
        </FormField>

        <FormField id="closure-population" label="Affected Population" required>
          <select
            id="closure-population"
            v-model="affectedPopulation"
            required
            class="w-full h-9 px-3 py-1.5 text-sm border border-slate-300 rounded-md focus:outline-none focus:ring-2 focus:ring-rose-500 bg-white"
            data-testid="modal-closure-population-select"
          >
            <option
              v-for="pop in affectedPopulationOptions"
              :key="pop.value"
              :value="pop.value"
            >
              {{ pop.label }}
            </option>
          </select>
        </FormField>
      </div>

      <!-- Justification Details -->
      <FormField
        id="closure-reason-details"
        label="Detailed Justification Reason"
        required
        hint="Administrative background, authority orders, or hazard summary (min 5 chars)"
      >
        <Textarea
          id="closure-reason-details"
          v-model="reasonDetails"
          required
          rows="3"
          placeholder="Provide explicit operational rationale for this closure..."
          class="w-full text-sm"
          data-testid="modal-closure-justification-input"
        />
      </FormField>

      <!-- Recovery Plan Notes (Optional) -->
      <FormField
        id="closure-recovery-notes"
        label="Recovery Plan Notes"
        hint="Optional plan for makeup days or remote instructional continuity"
      >
        <Textarea
          id="closure-recovery-notes"
          v-model="recoveryPlanNotes"
          rows="2"
          placeholder="e.g. Two makeup instructional days to be scheduled in Term 2."
          class="w-full text-sm"
          data-testid="modal-closure-recovery-input"
        />
      </FormField>

      <!-- Action Buttons -->
      <div class="flex justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          text
          size="small"
          label="Cancel"
          data-testid="modal-cancel-closure-btn"
          @click="handleClose"
        />
        <Button
          type="submit"
          size="small"
          label="Declare Closure"
          severity="danger"
          class="bg-rose-600 hover:bg-rose-700 text-white cursor-pointer"
          :loading="declareMutation.isPending.value"
          data-testid="modal-submit-closure-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
