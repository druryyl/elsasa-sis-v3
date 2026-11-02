<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import {
  AlertTriangle,
  ShieldAlert,
  AlertOctagon,
  Calendar,
  Clock,
  Sparkles,
  Users,
  School,
  FileText,
  LifeBuoy,
} from 'lucide-vue-next';
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

const reasonCategoryOptions: { value: ClosureReasonCategory; label: string; desc: string }[] = [
  { value: 'weather_disaster', label: 'Weather Emergency / Natural Disaster', desc: 'Flooding, typhoon, extreme weather alert' },
  { value: 'infrastructure_hazard', label: 'Infrastructure Failure / Safety Hazard', desc: 'Power grid outage, water failure, structural hazard' },
  { value: 'health_safety', label: 'Public Health & Safety Directive', desc: 'Sanitary outbreak, health authority emergency order' },
  { value: 'government_mandate', label: 'Government / Ministry Mandate', desc: 'Official ministerial or regional education decree' },
  { value: 'institutional_policy', label: 'Institutional Policy / Board Action', desc: 'Board of directors or school council declaration' },
  { value: 'emergency', label: 'Emergency Incident', desc: 'Unforeseen acute campus emergency' },
];

const affectedPopulationOptions: { value: AffectedPopulation; label: string }[] = [
  { value: 'all', label: 'All Campus (Students & Staff)' },
  { value: 'students_only', label: 'Students Only (Staff on Duty)' },
  { value: 'staff_and_students', label: 'Staff & Students' },
];

const EMERGENCY_PRESETS = [
  {
    label: 'Cuaca Ekstrem / Banjir',
    category: 'weather_disaster' as ClosureReasonCategory,
    details: 'Penutupan darurat kampus akibat cuaca ekstrem dan genangan banjir di akses jalan utama.',
    recovery: 'Pembelajaran dialihkan secara sinkron daring (PJJ) via LMS sekolah.',
  },
  {
    label: 'Gangguan Listrik & Trafo',
    category: 'infrastructure_hazard' as ClosureReasonCategory,
    details: 'Penutupan sementara untuk perbaikan instalasi kelistrikan utama kampus dan gardu distribusi.',
    recovery: 'Kegiatan administrasi dialihkan ke gedung pendukung; KBM dijadwalkan ulang.',
  },
  {
    label: 'Instruksi Kedinasan / KLB',
    category: 'government_mandate' as ClosureReasonCategory,
    details: 'Surat Edaran Dinas Pendidikan terkait mitigasi darurat dan keselamatan seluruh civitas sekolah.',
    recovery: 'Menyesuaikan kalender pemulihan pembelajaran sesuai ketentuan dinas.',
  },
];

const CAMPUS_SUGGESTIONS = ['All', 'Kampus Utama', 'Kampus Barat', 'Primary Campus'];

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

const closureDurationDays = computed(() => {
  if (!startDate.value || !endDate.value) return null;
  const start = new Date(startDate.value);
  const end = new Date(endDate.value);
  const diffTime = end.getTime() - start.getTime();
  const diff = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
  return diff;
});

function applyEmergencyPreset(preset: typeof EMERGENCY_PRESETS[number]) {
  reasonCategory.value = preset.category;
  reasonDetails.value = preset.details;
  if (!recoveryPlanNotes.value) {
    recoveryPlanNotes.value = preset.recovery;
  }
}

function selectCampus(campus: string) {
  affectedCampus.value = campus;
}

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
    class="max-w-xl w-full"
    data-testid="declare-closure-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-rose-50 border border-rose-100 flex items-center justify-center shrink-0 text-rose-600 shadow-xs">
          <ShieldAlert class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">Declare Campus Closure</h3>
            <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-rose-50 text-rose-700 border border-rose-200">
              <AlertOctagon class="w-3 h-3 text-rose-600" />
              Emergency Directive
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Declare scheduled or emergency campus closures with auditable schedule impact.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="handleSubmit">
      <!-- Authoritative Information Callout Banner -->
      <div
        class="p-3.5 bg-amber-50/90 border border-amber-200/90 rounded-lg flex items-start gap-3 text-xs text-amber-900 shadow-2xs leading-relaxed"
        data-testid="closure-warning-banner"
      >
        <div class="p-1 rounded bg-amber-100 text-amber-800 shrink-0 mt-0.5">
          <ShieldAlert class="w-4 h-4 text-amber-700" />
        </div>
        <div>
          <span class="font-semibold text-amber-950">Authoritative Schedule Impact:</span>
          Declaring a campus closure will atomically mark all affected dates non-instructional,
          record a versioned calendar amendment if published, and alert operational schedules.
        </div>
      </div>

      <!-- Error Alert Banner -->
      <div
        v-if="formError"
        class="p-3 bg-rose-50 border border-rose-200 rounded-lg flex items-center gap-2 text-xs text-rose-700 shadow-2xs"
        data-testid="closure-error-banner"
      >
        <AlertTriangle class="w-4 h-4 text-rose-500 shrink-0" />
        <span class="font-medium">{{ formError }}</span>
      </div>

      <!-- Quick Incident Presets Bar -->
      <div class="p-2.5 bg-slate-50/90 border border-slate-200/80 rounded-lg text-xs space-y-1.5 shadow-2xs">
        <div class="flex items-center gap-1.5 text-slate-700 font-medium">
          <Sparkles class="w-3.5 h-3.5 text-amber-500" />
          <span>Quick Incident Presets:</span>
        </div>
        <div class="flex flex-wrap items-center gap-1.5">
          <button
            v-for="preset in EMERGENCY_PRESETS"
            :key="preset.label"
            type="button"
            class="px-2.5 py-1 text-xs font-medium rounded-md bg-white border border-slate-200 text-slate-700 hover:border-rose-400 hover:text-rose-700 hover:bg-rose-50/50 transition cursor-pointer shadow-2xs"
            @click="applyEmergencyPreset(preset)"
          >
            {{ preset.label }}
          </button>
        </div>
      </div>

      <!-- Section 1: Classification & Scope Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <!-- Reason Category -->
        <FormField
          id="closure-category"
          label="Reason Category & Classification"
          required
          helpText="Formal institutional category of the emergency closure"
        >
          <div class="relative">
            <select
              id="closure-category"
              v-model="reasonCategory"
              required
              class="w-full h-10 px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500 bg-white font-medium text-slate-800 transition"
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
          </div>
        </FormField>

        <!-- Campus Context and Affected Population -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <FormField
              id="closure-campus"
              label="Affected Campus Scope"
              helpText="Specify campus boundary (Default: All)"
            >
              <InputText
                id="closure-campus"
                v-model="affectedCampus"
                placeholder="All"
                class="w-full text-sm"
                data-testid="modal-closure-campus-input"
              />
            </FormField>
            <div class="flex flex-wrap items-center gap-1.5 mt-2">
              <span class="text-[11px] text-slate-500 mr-0.5">Quick scope:</span>
              <button
                v-for="campus in CAMPUS_SUGGESTIONS"
                :key="campus"
                type="button"
                class="px-2 py-0.5 text-xs font-medium rounded border transition cursor-pointer"
                :class="affectedCampus === campus ? 'bg-rose-50 border-rose-300 text-rose-800' : 'bg-slate-50 border-slate-200 text-slate-600 hover:bg-slate-100'"
                @click="selectCampus(campus)"
              >
                {{ campus }}
              </button>
            </div>
          </div>

          <FormField
            id="closure-population"
            label="Affected Population"
            required
            helpText="Who is excused from reporting"
          >
            <select
              id="closure-population"
              v-model="affectedPopulation"
              required
              class="w-full h-10 px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500 bg-white font-medium text-slate-800 transition"
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
      </div>

      <!-- Section 2: Closure Date Span Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3 shadow-2xs">
        <div class="flex items-center justify-between pb-1 border-b border-slate-100">
          <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wider">
            <Clock class="w-3.5 h-3.5 text-rose-600" />
            <span>Closure Duration & Dates</span>
          </div>
          <div
            v-if="closureDurationDays !== null"
            class="text-xs px-2 py-0.5 rounded font-medium"
            :class="closureDurationDays >= 1 ? 'bg-rose-50 text-rose-700 border border-rose-200' : 'bg-rose-100 text-rose-900 border border-rose-300'"
          >
            <span v-if="closureDurationDays >= 1">{{ closureDurationDays }} Days Campus Closure</span>
            <span v-else>⚠️ End date cannot precede Start date</span>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
          <FormField
            id="closure-start"
            label="Start Date"
            required
            helpText="Beginning of campus shutdown"
          >
            <InputText
              id="closure-start"
              v-model="startDate"
              type="date"
              required
              class="w-full text-sm"
              data-testid="modal-closure-start-input"
            />
          </FormField>

          <FormField
            id="closure-end"
            label="End Date"
            required
            helpText="Conclusion of campus shutdown"
          >
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
      </div>

      <!-- Section 3: Justification & Recovery Plan Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <!-- Justification Details -->
        <FormField
          id="closure-reason-details"
          label="Detailed Justification Reason"
          required
          helpText="Administrative background, official order reference, or safety hazard summary (min 5 characters)"
        >
          <Textarea
            id="closure-reason-details"
            v-model="reasonDetails"
            required
            rows="2"
            placeholder="Provide operational rationale for this closure directive..."
            class="w-full text-sm resize-none"
            data-testid="modal-closure-justification-input"
          />
        </FormField>

        <!-- Recovery Plan Notes -->
        <FormField
          id="closure-recovery-notes"
          label="Recovery & Make-Up Plan Notes"
          optional
          helpText="Online learning contingency, reschedule dates, or syllabus adjustment strategy"
        >
          <Textarea
            id="closure-recovery-notes"
            v-model="recoveryPlanNotes"
            rows="2"
            placeholder="e.g. Asynchronous remote study via LMS; makeup instructional days scheduled in Term 2."
            class="w-full text-sm resize-none"
            data-testid="modal-closure-recovery-input"
          />
        </FormField>
      </div>

      <!-- Action Buttons -->
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
            data-testid="modal-cancel-closure-btn"
            @click="handleClose"
          />
          <Button
            type="submit"
            size="small"
            severity="danger"
            class="bg-rose-600 hover:bg-rose-700 text-white shadow-xs px-4 cursor-pointer"
            :loading="declareMutation.isPending.value"
            data-testid="modal-submit-closure-btn"
          >
            <ShieldAlert class="w-4 h-4 mr-1.5" />
            <span>Declare Closure</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
