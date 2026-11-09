<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { refDebounced } from '@vueuse/core';
import {
  UserPlus,
  Search,
  User,
  GraduationCap,
  Calendar,
  FileText,
  AlertCircle,
  X,
  CheckCircle2,
} from 'lucide-vue-next';
import { LoadingSpinner } from '@/shared/components';
import { useStudentSearchQuery } from '@/features/students/records/composables/useStudentRecordsQueries';
import { useCreatePlanMutation } from '../composables/useLearningPlans';
import type { StudentListItem } from '@/features/students/records/types/records.types';

export interface AcademicPeriodOption {
  id: string;
  label: string;
  code?: string;
}

const props = defineProps<{
  visible: boolean;
  periods: AcademicPeriodOption[];
  initialPeriodId?: string | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'created', newPlanId: string): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Student Search State
const studentSearch = ref('');
const debouncedStudentSearch = refDebounced(studentSearch, 300);
const selectedStudent = ref<StudentListItem | null>(null);

// Form Fields State
const selectedPeriodId = ref<string>('');
const notes = ref<string>('');
const validationError = ref<string | null>(null);

// Sync initialPeriodId when opened or changed
watch(
  () => [props.visible, props.initialPeriodId, props.periods],
  ([visible]) => {
    if (visible) {
      validationError.value = null;
      if (props.initialPeriodId) {
        selectedPeriodId.value = props.initialPeriodId;
      } else if (props.periods.length > 0 && !selectedPeriodId.value) {
        selectedPeriodId.value = props.periods[0].id;
      }
    }
  },
  { immediate: true },
);

// Student Search Query
const { data: searchResults, isLoading: isSearchingStudents } = useStudentSearchQuery(
  computed(() => ({
    search: debouncedStudentSearch.value.trim(),
    pageSize: 8,
  })),
  {
    enabled: computed(
      () => isDialogVisible.value && !selectedStudent.value && debouncedStudentSearch.value.trim().length >= 2,
    ),
  },
);

const studentResultsList = computed(() => searchResults.value?.items ?? []);

function handleSelectStudent(student: StudentListItem) {
  selectedStudent.value = student;
  studentSearch.value = '';
  validationError.value = null;
}

function handleClearStudent() {
  selectedStudent.value = null;
  studentSearch.value = '';
}

function handleClose() {
  isDialogVisible.value = false;
  selectedStudent.value = null;
  studentSearch.value = '';
  notes.value = '';
  validationError.value = null;
}

const createPlanMutation = useCreatePlanMutation();

const selectedPeriodLabel = computed(() => {
  const found = props.periods.find((p) => p.id === selectedPeriodId.value);
  return found ? found.label : '';
});

async function handleSubmit() {
  validationError.value = null;

  if (!selectedStudent.value) {
    validationError.value = 'Please select a student for the learning plan.';
    return;
  }

  if (!selectedPeriodId.value) {
    validationError.value = 'Please select an academic period.';
    return;
  }

  try {
    const result = await createPlanMutation.mutateAsync({
      studentId: selectedStudent.value.id,
      academicPeriodId: selectedPeriodId.value,
      academicPeriodLabel: selectedPeriodLabel.value,
      notes: notes.value.trim() || null,
    });

    emit('created', result.id);
    handleClose();
  } catch (err: unknown) {
    // Error notification handled by mutation toast
    if (err && typeof err === 'object' && 'message' in err) {
      validationError.value = String((err as { message: string }).message);
    }
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :header="'Formulate Student Learning Plan'"
    :style="{ width: '560px' }"
    :closable="!createPlanMutation.isPending.value"
    @hide="handleClose"
  >
    <div class="space-y-5 py-2">
      <!-- Error Message Banner -->
      <div
        v-if="validationError"
        class="flex items-start gap-2.5 p-3 rounded-lg bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/60 text-sm text-red-700 dark:text-red-300"
      >
        <AlertCircle class="w-4 h-4 mt-0.5 flex-shrink-0 text-red-500" />
        <div class="flex-1">{{ validationError }}</div>
      </div>

      <!-- 1. Student Selection -->
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300">
          Target Student <span class="text-red-500">*</span>
        </label>

        <!-- Selected Student Card -->
        <div
          v-if="selectedStudent"
          class="flex items-center justify-between p-3 rounded-lg border border-indigo-200 dark:border-indigo-800 bg-indigo-50/60 dark:bg-indigo-950/30"
        >
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-indigo-600 text-white flex items-center justify-center font-bold text-sm shadow-sm">
              {{ selectedStudent.fullName.charAt(0).toUpperCase() }}
            </div>
            <div>
              <div class="font-semibold text-sm text-slate-900 dark:text-slate-100">
                {{ selectedStudent.fullName }}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400 flex items-center gap-2">
                <span>NIS: {{ selectedStudent.studentNumber }}</span>
                <span>•</span>
                <span>Grade: {{ selectedStudent.initialLevel || 'N/A' }}</span>
              </div>
            </div>
          </div>
          <Button
            type="button"
            icon="pi pi-times"
            severity="secondary"
            text
            rounded
            size="small"
            title="Change Student"
            :disabled="createPlanMutation.isPending.value"
            @click="handleClearStudent"
          />
        </div>

        <!-- Student Search Input -->
        <div v-else class="space-y-2">
          <div class="relative">
            <Search class="absolute left-3 top-2.5 w-4 h-4 text-slate-400" />
            <InputText
              v-model="studentSearch"
              placeholder="Search by student name or NIS..."
              class="w-full pl-9 text-sm"
              :disabled="createPlanMutation.isPending.value"
            />
            <div v-if="isSearchingStudents" class="absolute right-3 top-2.5">
              <LoadingSpinner size="sm" />
            </div>
          </div>

          <!-- Search Results Dropdown -->
          <div
            v-if="studentSearch.trim().length >= 2 && !selectedStudent"
            class="border border-slate-200 dark:border-slate-800 rounded-lg max-h-48 overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-900 shadow-md"
          >
            <div
              v-for="student in studentResultsList"
              :key="student.id"
              class="p-2.5 hover:bg-indigo-50/60 dark:hover:bg-indigo-950/30 cursor-pointer flex items-center justify-between transition-colors"
              @click="handleSelectStudent(student)"
            >
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-full bg-slate-200 dark:bg-slate-700 flex items-center justify-center text-xs font-semibold">
                  {{ student.fullName.charAt(0) }}
                </div>
                <div>
                  <div class="text-xs font-semibold text-slate-800 dark:text-slate-200">
                    {{ student.fullName }}
                  </div>
                  <div class="text-[11px] text-slate-500">
                    NIS: {{ student.studentNumber }} • {{ student.initialLevel || 'Grade N/A' }}
                  </div>
                </div>
              </div>
              <Button
                label="Select"
                size="small"
                text
                class="text-xs font-medium"
              />
            </div>

            <div
              v-if="!isSearchingStudents && studentResultsList.length === 0"
              class="p-3 text-center text-xs text-slate-500"
            >
              No students found matching "{{ debouncedStudentSearch }}"
            </div>
          </div>
          <p class="text-[11px] text-slate-500">
            Type at least 2 characters to search Hagios enrolled students.
          </p>
        </div>
      </div>

      <!-- 2. Academic Period -->
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300">
          Governing Academic Period <span class="text-red-500">*</span>
        </label>
        <Select
          v-model="selectedPeriodId"
          :options="periods"
          option-label="label"
          option-value="id"
          placeholder="Select Academic Period"
          class="w-full text-sm"
          :disabled="createPlanMutation.isPending.value || periods.length === 0"
        />
        <p class="text-[11px] text-slate-500">
          Each student may have at most one active learning plan per academic period.
        </p>
      </div>

      <!-- 3. Formulation Justification & Notes -->
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300">
          Formulation Justification & Notes
        </label>
        <Textarea
          v-model="notes"
          rows="3"
          placeholder="Document the academic assessment rationale, developmental observations, or referral background..."
          class="w-full text-sm resize-none"
          :disabled="createPlanMutation.isPending.value"
        />
      </div>
    </div>

    <!-- Modal Footer Actions -->
    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
        <Button
          label="Cancel"
          severity="secondary"
          text
          :disabled="createPlanMutation.isPending.value"
          @click="handleClose"
        />
        <Button
          label="Formulate Draft Plan"
          icon="pi pi-check"
          :loading="createPlanMutation.isPending.value"
          :disabled="!selectedStudent || !selectedPeriodId"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
