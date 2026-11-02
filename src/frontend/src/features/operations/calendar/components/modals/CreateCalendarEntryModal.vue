<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import { AlertTriangle, CalendarPlus } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useCreateCalendarEntryMutation } from '../../composables/useCalendarMatrixQuery';
import {
  CALENDAR_DESIGNATIONS,
  type CalendarDesignation,
} from '../../schemas/calendarEntry.schema';
import type { CalendarEntryDto } from '../../api/calendar.api';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    calendarId: string;
    isPublished?: boolean;
    initialDate?: string | null;
    appendTo?: string;
  }>(),
  {
    isPublished: false,
    initialDate: null,
    appendTo: 'self',
  },
);

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', entry: CalendarEntryDto): void;
}>();

const createEntryMutation = useCreateCalendarEntryMutation();

const DESIGNATION_OPTIONS: { label: string; value: CalendarDesignation }[] = [
  { label: 'Standard Instructional Day', value: 'instructional_day' },
  { label: 'Statutory / Religious Holiday', value: 'holiday' },
  { label: 'Campus Closure', value: 'school_closure' },
  { label: 'Staff-Only Professional Day', value: 'staff_only_day' },
  { label: 'Examination Window / Day', value: 'exam_day' },
  { label: 'Significant Institutional Date', value: 'significant_date' },
  { label: 'Domain Coordination Window', value: 'domain_window' },
];

const form = ref({
  entryDate: '',
  endDate: '',
  designation: 'holiday' as CalendarDesignation,
  name: '',
  description: '',
  isInstructional: false,
  appliesTo: 'All',
  amendmentReason: '',
});

const errors = ref<Record<string, string>>({});

// Watch initialDate and visible to prefill or reset form
watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      errors.value = {};
      form.value = {
        entryDate: props.initialDate || new Date().toISOString().substring(0, 10),
        endDate: '',
        designation: 'holiday',
        name: '',
        description: '',
        isInstructional: false,
        appliesTo: 'All',
        amendmentReason: '',
      };
    }
  },
  { immediate: true },
);

// Auto-adjust isInstructional when designation changes
watch(
  () => form.value.designation,
  (newDesignation) => {
    if (newDesignation === 'instructional_day') {
      form.value.isInstructional = true;
    } else if (['holiday', 'school_closure', 'staff_only_day'].includes(newDesignation)) {
      form.value.isInstructional = false;
    }
  },
);

function validate(): boolean {
  const errs: Record<string, string> = {};

  if (!form.value.entryDate) {
    errs.entryDate = 'Entry date is required.';
  } else if (!/^\d{4}-\d{2}-\d{2}$/.test(form.value.entryDate)) {
    errs.entryDate = 'Entry date must be in YYYY-MM-DD format.';
  }

  if (form.value.endDate) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(form.value.endDate)) {
      errs.endDate = 'End date must be in YYYY-MM-DD format.';
    } else if (form.value.entryDate && form.value.endDate < form.value.entryDate) {
      errs.endDate = 'End date must be on or after entry date.';
    }
  }

  if (!form.value.name || form.value.name.trim().length < 2) {
    errs.name = 'Title must be at least 2 characters.';
  } else if (form.value.name.trim().length > 200) {
    errs.name = 'Title cannot exceed 200 characters.';
  }

  if (
    !form.value.designation ||
    !CALENDAR_DESIGNATIONS.includes(form.value.designation as CalendarDesignation)
  ) {
    errs.designation = 'A valid designation is required.';
  }

  errors.value = errs;
  return Object.keys(errs).length === 0;
}

const isSubmitting = computed(() => createEntryMutation.isPending.value);

function handleClose(): void {
  emit('update:visible', false);
}

function handleSubmit(): void {
  if (!validate()) return;
  if (!props.calendarId) return;

  createEntryMutation.mutate(
    {
      calendarId: props.calendarId,
      payload: {
        entryDate: form.value.entryDate,
        endDate: form.value.endDate || undefined,
        designation: form.value.designation,
        name: form.value.name.trim(),
        description: form.value.description?.trim() || undefined,
        isInstructional: form.value.isInstructional,
        appliesTo: form.value.appliesTo?.trim() || 'All',
        amendmentReason: props.isPublished
          ? form.value.amendmentReason?.trim() || undefined
          : undefined,
      },
    },
    {
      onSuccess: (data) => {
        emit('created', data);
        handleClose();
      },
    },
  );
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :append-to="appendTo"
    class="max-w-lg w-full"
    data-testid="create-calendar-entry-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <CalendarPlus class="w-5 h-5 text-teal-600" />
        <span class="font-semibold text-slate-800 text-base">Add Calendar Entry</span>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <!-- Published Calendar Warning Banner -->
      <div
        v-if="isPublished"
        class="flex items-start gap-3 p-3 bg-amber-50 border border-amber-200 text-amber-900 rounded-md text-xs leading-relaxed"
        data-testid="published-calendar-warning-banner"
      >
        <AlertTriangle class="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
        <div>
          <p class="font-semibold text-amber-950">Published Calendar</p>
          <p>
            Adding or editing an entry on a published calendar will generate an auditable Calendar
            Amendment and bump version.
          </p>
        </div>
      </div>

      <form class="space-y-3.5" @submit.prevent="handleSubmit">
        <!-- Date Spans -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="entry-date"
            label="Entry Date"
            required
            :error="errors.entryDate"
            hint="YYYY-MM-DD"
          >
            <InputText
              id="entry-date"
              v-model="form.entryDate"
              type="date"
              required
              class="w-full text-sm"
              data-testid="entry-date-input"
            />
          </FormField>

          <FormField
            id="end-date"
            label="End Date (Optional)"
            :error="errors.endDate"
            hint="Leave blank for single day"
          >
            <InputText
              id="end-date"
              v-model="form.endDate"
              type="date"
              class="w-full text-sm"
              data-testid="entry-end-date-input"
            />
          </FormField>
        </div>

        <!-- Designation Selector -->
        <FormField
          id="entry-designation"
          label="Day Designation"
          required
          :error="errors.designation"
        >
          <Select
            id="entry-designation"
            v-model="form.designation"
            :options="DESIGNATION_OPTIONS"
            option-label="label"
            option-value="value"
            placeholder="Select Day Designation"
            class="w-full text-sm"
            data-testid="entry-designation-select"
          />
        </FormField>

        <!-- Title / Name -->
        <FormField
          id="entry-name"
          label="Title / Name"
          required
          hint="e.g. Hari Raya Idul Fitri, Staff Workshop"
          :error="errors.name"
        >
          <InputText
            id="entry-name"
            v-model="form.name"
            required
            placeholder="Descriptive entry title"
            class="w-full text-sm"
            data-testid="entry-name-input"
          />
        </FormField>

        <!-- Description -->
        <FormField
          id="entry-description"
          label="Description"
          hint="Optional guidelines, affected grades, or schedule modifications"
        >
          <Textarea
            id="entry-description"
            v-model="form.description"
            rows="2"
            placeholder="Additional context or policy notes..."
            class="w-full text-sm"
            data-testid="entry-description-input"
          />
        </FormField>

        <!-- Target Population / Scope -->
        <FormField
          id="entry-applies-to"
          label="Applies To"
          hint="Scope (e.g. All, Primary, Secondary, Staff)"
        >
          <InputText
            id="entry-applies-to"
            v-model="form.appliesTo"
            placeholder="All"
            class="w-full text-sm"
            data-testid="entry-applies-to-input"
          />
        </FormField>

        <!-- Instructional Flag -->
        <div class="flex items-center gap-2 pt-1 pb-1">
          <Checkbox
            v-model="form.isInstructional"
            :binary="true"
            input-id="entry-is-instructional"
            data-testid="entry-is-instructional-checkbox"
          />
          <label for="entry-is-instructional" class="text-xs text-slate-700 cursor-pointer select-none">
            <span class="font-medium">Instructional Day:</span> Standard teaching and learning
            activities occur on this date.
          </label>
        </div>

        <!-- Optional Amendment Reason (when published) -->
        <FormField
          v-if="isPublished"
          id="amendment-reason"
          label="Amendment Justification"
          hint="Auditable reason for adding an entry to a published calendar"
        >
          <InputText
            id="amendment-reason"
            v-model="form.amendmentReason"
            placeholder="e.g. National holiday calendar decreed by government"
            class="w-full text-sm"
            data-testid="entry-amendment-reason-input"
          />
        </FormField>

        <!-- Modal Footer Actions -->
        <div class="flex justify-end gap-2 pt-3 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            data-testid="entry-modal-cancel-btn"
            @click="handleClose"
          />
          <Button
            type="submit"
            size="small"
            :loading="isSubmitting"
            data-testid="entry-modal-submit-btn"
            class="bg-teal-600 hover:bg-teal-700 text-white"
          >
            <CalendarPlus class="w-4 h-4 mr-1.5" />
            <span>Add Entry</span>
          </Button>
        </div>
      </form>
    </div>
  </Dialog>
</template>
