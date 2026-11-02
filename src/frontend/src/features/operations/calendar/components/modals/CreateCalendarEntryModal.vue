<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Checkbox from 'primevue/checkbox';
import {
  AlertTriangle,
  CalendarPlus,
  Calendar,
  Sparkles,
  BookOpen,
  Users,
  Clock,
  ShieldAlert,
  GraduationCap,
  Briefcase,
  AlertOctagon,
  CalendarDays,
  Bookmark,
} from 'lucide-vue-next';
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

interface DesignationOption {
  label: string;
  value: CalendarDesignation;
  description: string;
  colorClass: string;
  badgeClass: string;
}

const DESIGNATION_OPTIONS: DesignationOption[] = [
  {
    label: 'Standard Instructional Day',
    value: 'instructional_day',
    description: 'Active learning & instructional schedule',
    colorClass: 'text-emerald-600 bg-emerald-500',
    badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  },
  {
    label: 'Statutory / Religious Holiday',
    value: 'holiday',
    description: 'School closed; official holiday or festival',
    colorClass: 'text-rose-600 bg-rose-500',
    badgeClass: 'bg-rose-50 text-rose-700 border-rose-200',
  },
  {
    label: 'Campus Closure',
    value: 'school_closure',
    description: 'Campus closed due to weather or emergency',
    colorClass: 'text-amber-600 bg-amber-500',
    badgeClass: 'bg-amber-50 text-amber-700 border-amber-200',
  },
  {
    label: 'Staff-Only Professional Day',
    value: 'staff_only_day',
    description: 'Teacher workshop, training, or planning day',
    colorClass: 'text-purple-600 bg-purple-500',
    badgeClass: 'bg-purple-50 text-purple-700 border-purple-200',
  },
  {
    label: 'Examination Window / Day',
    value: 'exam_day',
    description: 'Midterm, semester, or national assessment',
    colorClass: 'text-indigo-600 bg-indigo-500',
    badgeClass: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  },
  {
    label: 'Significant Institutional Date',
    value: 'significant_date',
    description: 'Orientation, sports festival, or ceremony',
    colorClass: 'text-sky-600 bg-sky-500',
    badgeClass: 'bg-sky-50 text-sky-700 border-sky-200',
  },
  {
    label: 'Domain Coordination Window',
    value: 'domain_window',
    description: 'Admissions, billing, or grading periods',
    colorClass: 'text-teal-600 bg-teal-500',
    badgeClass: 'bg-teal-50 text-teal-700 border-teal-200',
  },
];

const QUICK_PRESETS = [
  { label: 'Hari Raya Idul Fitri', designation: 'holiday' as CalendarDesignation, instructional: false },
  { label: 'Tahun Baru Masehi', designation: 'holiday' as CalendarDesignation, instructional: false },
  { label: 'Penilaian Akhir Semester (PAS)', designation: 'exam_day' as CalendarDesignation, instructional: true },
  { label: 'Rapat Kerja Guru', designation: 'staff_only_day' as CalendarDesignation, instructional: false },
  { label: 'Masa Orientasi Siswa (MPLS)', designation: 'significant_date' as CalendarDesignation, instructional: true },
];

const SCOPE_SUGGESTIONS = [
  'All',
  'Primary (SD)',
  'Junior High (SMP)',
  'Senior High (SMA)',
  'Staff Only',
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

const currentDesignation = computed(() => {
  return DESIGNATION_OPTIONS.find((opt) => opt.value === form.value.designation) || DESIGNATION_OPTIONS[0];
});

// Duration in days
const entryDurationDays = computed(() => {
  if (!form.value.entryDate) return null;
  if (!form.value.endDate) return 1;
  const start = new Date(form.value.entryDate);
  const end = new Date(form.value.endDate);
  const diffTime = end.getTime() - start.getTime();
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
  return diffDays;
});

function applyPreset(preset: typeof QUICK_PRESETS[number]) {
  form.value.name = preset.label;
  form.value.designation = preset.designation;
  form.value.isInstructional = preset.instructional;
}

function selectScope(scope: string) {
  form.value.appliesTo = scope;
}

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
    class="max-w-xl w-full"
    data-testid="create-calendar-entry-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center shrink-0 text-teal-600 shadow-xs">
          <CalendarPlus class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">Add Calendar Entry</h3>
            <span
              v-if="isPublished"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200"
            >
              <ShieldAlert class="w-3 h-3 text-amber-600" />
              Published Amendment
            </span>
            <span
              v-else
              class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-teal-50 text-teal-700 border border-teal-200"
            >
              Draft Calendar
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Schedule an institutional holiday, instructional day, examination, or milestone.
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <!-- Published Calendar Warning Banner -->
      <div
        v-if="isPublished"
        class="flex items-start gap-3 p-3.5 bg-amber-50/80 border border-amber-200/90 text-amber-900 rounded-lg text-xs leading-relaxed shadow-2xs"
        data-testid="published-calendar-warning-banner"
      >
        <div class="p-1 rounded bg-amber-100 text-amber-800 shrink-0 mt-0.5">
          <AlertTriangle class="w-4 h-4 text-amber-700" />
        </div>
        <div>
          <p class="font-semibold text-amber-950">Published Calendar Amendment Notice</p>
          <p class="text-amber-800 mt-0.5">
            Adding or editing an entry on a published calendar will generate an auditable Calendar
            Amendment and bump version.
          </p>
        </div>
      </div>

      <form class="space-y-4" @submit.prevent="handleSubmit">
        <!-- Quick Title Presets Bar -->
        <div class="p-3 bg-slate-50/90 border border-slate-200/80 rounded-lg text-xs space-y-2 shadow-2xs">
          <div class="flex items-center gap-1.5 text-slate-700 font-medium">
            <Sparkles class="w-3.5 h-3.5 text-amber-500" />
            <span>Quick Event Presets:</span>
          </div>
          <div class="flex flex-wrap items-center gap-1.5">
            <button
              v-for="preset in QUICK_PRESETS"
              :key="preset.label"
              type="button"
              class="px-2.5 py-1 text-xs font-medium rounded-md bg-white border border-slate-200 text-slate-700 hover:border-teal-400 hover:text-teal-700 hover:bg-teal-50/50 transition cursor-pointer shadow-2xs"
              @click="applyPreset(preset)"
            >
              {{ preset.label }}
            </button>
          </div>
        </div>

        <!-- Section 1: Title & Designation -->
        <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
          <!-- Title / Name -->
          <FormField
            id="entry-name"
            label="Entry Title / Name"
            required
            helpText="Descriptive label shown on calendars and schedules"
            :error="errors.name"
          >
            <InputText
              id="entry-name"
              v-model="form.name"
              required
              placeholder="e.g. Hari Raya Idul Fitri, Staff Workshop, Ujian Semester"
              class="w-full text-sm font-medium"
              data-testid="entry-name-input"
            />
          </FormField>

          <!-- Designation Selector -->
          <FormField
            id="entry-designation"
            label="Day Designation & Classification"
            required
            helpText="Categorizes the day for instructional counting and compliance"
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
            >
              <template #value="slotProps">
                <div v-if="slotProps.value" class="flex items-center gap-2">
                  <span
                    class="w-2.5 h-2.5 rounded-full shrink-0"
                    :class="currentDesignation.colorClass.split(' ')[1]"
                  />
                  <span class="font-medium text-slate-800">{{ currentDesignation.label }}</span>
                </div>
                <span v-else class="text-slate-400">{{ slotProps.placeholder }}</span>
              </template>
              <template #option="slotProps">
                <div class="flex items-start gap-2.5 py-1">
                  <span
                    class="w-2.5 h-2.5 rounded-full shrink-0 mt-1.5"
                    :class="slotProps.option.colorClass.split(' ')[1]"
                  />
                  <div>
                    <div class="text-xs font-semibold text-slate-800">{{ slotProps.option.label }}</div>
                    <div class="text-xs text-slate-500">{{ slotProps.option.description }}</div>
                  </div>
                </div>
              </template>
            </Select>
          </FormField>
        </div>

        <!-- Section 2: Date Scheduling & Duration -->
        <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3 shadow-2xs">
          <div class="flex items-center justify-between pb-1 border-b border-slate-100">
            <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wider">
              <Calendar class="w-3.5 h-3.5 text-teal-600" />
              <span>Schedule Span</span>
            </div>
            <div
              v-if="entryDurationDays !== null"
              class="text-xs px-2 py-0.5 rounded font-medium"
              :class="entryDurationDays >= 1 ? 'bg-teal-50 text-teal-700 border border-teal-200' : 'bg-rose-50 text-rose-700 border border-rose-200'"
            >
              <span v-if="entryDurationDays === 1">Single Day (1 day)</span>
              <span v-else-if="entryDurationDays > 1">{{ entryDurationDays }} Days Duration</span>
              <span v-else>⚠️ End date cannot precede Entry date</span>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
            <FormField
              id="entry-date"
              label="Entry Date"
              required
              :error="errors.entryDate"
              helpText="Format: YYYY-MM-DD"
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
              label="End Date"
              optional
              :error="errors.endDate"
              helpText="Leave blank for single day event"
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
        </div>

        <!-- Section 3: Scope, Audience & Instructional Toggle -->
        <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
          <!-- Target Population / Scope -->
          <div>
            <FormField
              id="entry-applies-to"
              label="Applies To (Target Audience)"
              helpText="Specify academic grade, level, or staff scope"
            >
              <InputText
                id="entry-applies-to"
                v-model="form.appliesTo"
                placeholder="All"
                class="w-full text-sm"
                data-testid="entry-applies-to-input"
              />
            </FormField>
            <div class="flex flex-wrap items-center gap-1.5 mt-2">
              <span class="text-xs text-slate-500 mr-0.5">Suggestions:</span>
              <button
                v-for="scope in SCOPE_SUGGESTIONS"
                :key="scope"
                type="button"
                class="px-2 py-0.5 text-xs font-medium rounded border transition cursor-pointer"
                :class="form.appliesTo === scope ? 'bg-teal-50 border-teal-300 text-teal-800' : 'bg-slate-50 border-slate-200 text-slate-600 hover:bg-slate-100'"
                @click="selectScope(scope)"
              >
                {{ scope }}
              </button>
            </div>
          </div>

          <!-- Instructional Toggle Card -->
          <div
            class="flex items-start gap-3 p-3 rounded-lg border transition cursor-pointer select-none"
            :class="form.isInstructional ? 'bg-emerald-50/60 border-emerald-200' : 'bg-slate-50/70 border-slate-200'"
            @click="form.isInstructional = !form.isInstructional"
          >
            <div
              class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 transition"
              :class="form.isInstructional ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-500'"
            >
              <BookOpen class="w-4 h-4" />
            </div>
            <div class="flex-1">
              <div class="flex items-center justify-between">
                <span class="text-xs font-semibold text-slate-800">Instructional Day</span>
                <Checkbox
                  v-model="form.isInstructional"
                  :binary="true"
                  input-id="entry-is-instructional"
                  data-testid="entry-is-instructional-checkbox"
                  @click.stop
                />
              </div>
              <p class="text-xs text-slate-500 mt-0.5">
                Standard curriculum teaching and learning activities occur. Counts toward total instructional hours.
              </p>
            </div>
          </div>

          <!-- Description -->
          <FormField
            id="entry-description"
            label="Additional Guidelines & Context"
            optional
            helpText="Optional administrative context, affected classes, or operational guidelines"
          >
            <Textarea
              id="entry-description"
              v-model="form.description"
              rows="2"
              placeholder="Provide policy notes or schedule modifications..."
              class="w-full text-sm resize-none"
              data-testid="entry-description-input"
            />
          </FormField>
        </div>

        <!-- Optional Amendment Reason (when published) -->
        <div v-if="isPublished" class="bg-amber-50/50 border border-amber-200 rounded-lg p-3.5">
          <FormField
            id="amendment-reason"
            label="Amendment Justification"
            required
            helpText="Auditable reason for adding an entry to a published calendar"
          >
            <InputText
              id="amendment-reason"
              v-model="form.amendmentReason"
              placeholder="e.g. National holiday calendar decreed by government"
              class="w-full text-sm"
              data-testid="entry-amendment-reason-input"
            />
          </FormField>
        </div>

        <!-- Modal Footer Actions -->
        <div class="flex items-center justify-between pt-3 border-t border-slate-200/80">
          <span class="text-xs text-slate-500 flex items-center gap-1">
            <span class="text-rose-500 font-bold">*</span> Required fields
          </span>
          <div class="flex items-center gap-2">
            <Button
              type="button"
              text
              size="small"
              label="Cancel"
              class="text-slate-600 hover:text-slate-800"
              data-testid="entry-modal-cancel-btn"
              @click="handleClose"
            />
            <Button
              type="submit"
              size="small"
              :loading="isSubmitting"
              data-testid="entry-modal-submit-btn"
              class="bg-teal-600 hover:bg-teal-700 text-white shadow-xs px-4"
            >
              <CalendarPlus class="w-4 h-4 mr-1.5" />
              <span>Add Entry</span>
            </Button>
          </div>
        </div>
      </form>
    </div>
  </Dialog>
</template>
