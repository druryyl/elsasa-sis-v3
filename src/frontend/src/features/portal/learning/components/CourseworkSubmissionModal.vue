<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import FileUpload, { type FileUploadSelectEvent, type FileUploadUploaderEvent } from 'primevue/fileupload';
import ProgressBar from 'primevue/progressbar';
import {
  Link2,
  FileText,
  UploadCloud,
  CheckCircle2,
  AlertCircle,
  Trash2,
  Send,
  Calendar,
  Layers,
  Sparkles,
  Info,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  useSubmitCourseworkMutation,
  useUploadSubmissionFileMutation,
} from '../composables/useStudentCoursework';
import {
  submitCourseworkSchema,
  fileUploadSchema,
  MAX_FILE_SIZE_BYTES,
} from '../schemas/learning.schema';
import type {
  StudentCourseworkTask,
  SubmissionType,
  SubmitCourseworkResponse,
} from '../types';

interface Props {
  visible: boolean;
  task: StudentCourseworkTask | null;
  studentId?: string;
}

const props = withDefaults(defineProps<Props>(), {
  task: null,
  studentId: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'submitted', response: SubmitCourseworkResponse): void;
}>();

const authStore = useAuthStore();

// ---------------------------------------------------------------------------
// 1. Reactive State
// ---------------------------------------------------------------------------
const isModalVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const submissionType = ref<SubmissionType>('link');
const linkUrl = ref('');
const textContent = ref('');
const notes = ref('');

// File upload state
const selectedFile = ref<File | null>(null);
const uploadedStoragePath = ref<string | null>(null);
const uploadedFileName = ref<string | null>(null);
const uploadedFileSizeBytes = ref<number>(0);
const uploadProgress = ref<number>(0);
const isUploading = ref(false);
const uploadError = ref<string | null>(null);

// Form errors
const formError = ref<string | null>(null);

// Effective Student ID
const effectiveStudentId = computed(() => {
  if (props.studentId) return props.studentId;
  if (authStore.user?.studentId) return authStore.user.studentId;
  if (authStore.roles.includes('Student')) return authStore.user?.userId ?? '';
  return '';
});

// Resubmission state
const isResubmission = computed(() => {
  if (!props.task) return false;
  return (
    props.task.displayStatus === 'submitted' ||
    props.task.displayStatus === 'late' ||
    props.task.isResubmission ||
    Boolean(props.task.submissionId)
  );
});

// ---------------------------------------------------------------------------
// 2. Mutations
// ---------------------------------------------------------------------------
const submitMutation = useSubmitCourseworkMutation();
const uploadMutation = useUploadSubmissionFileMutation();

// Reset form when dialog opens
watch(
  () => props.visible,
  (newVal) => {
    if (newVal) {
      resetForm();
    }
  },
);

function resetForm() {
  submissionType.value = 'link';
  linkUrl.value = '';
  textContent.value = '';
  notes.value = '';
  selectedFile.value = null;
  uploadedStoragePath.value = null;
  uploadedFileName.value = null;
  uploadedFileSizeBytes.value = 0;
  uploadProgress.value = 0;
  isUploading.value = false;
  uploadError.value = null;
  formError.value = null;
}

// ---------------------------------------------------------------------------
// 3. File Upload Handling
// ---------------------------------------------------------------------------
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

async function handleFileSelect(event: FileUploadSelectEvent) {
  const file = event.files?.[0];
  if (!file) return;

  await processFile(file);
}

async function handleCustomUpload(event: FileUploadUploaderEvent) {
  const files = Array.isArray(event.files) ? event.files : [event.files];
  const file = files[0];
  if (!file) return;

  await processFile(file);
}

async function processFile(file: File) {
  formError.value = null;
  uploadError.value = null;

  // Validate via Zod schema
  const validation = fileUploadSchema.safeParse({ file });
  if (!validation.success) {
    uploadError.value = validation.error.errors[0]?.message || 'Selected file is invalid.';
    return;
  }

  selectedFile.value = file;
  isUploading.value = true;
  uploadProgress.value = 25;

  try {
    uploadProgress.value = 55;
    const response = await uploadMutation.mutateAsync({
      file,
      studentId: effectiveStudentId.value,
    });

    uploadProgress.value = 100;
    uploadedStoragePath.value = response.storagePath;
    uploadedFileName.value = response.fileName || response.originalFileName || file.name;
    uploadedFileSizeBytes.value = response.fileSizeBytes || file.size;
  } catch (err) {
    uploadError.value = err instanceof Error ? err.message : 'File upload failed. Please try again.';
    selectedFile.value = null;
    uploadedStoragePath.value = null;
  } finally {
    isUploading.value = false;
  }
}

function handleRemoveUploadedFile() {
  selectedFile.value = null;
  uploadedStoragePath.value = null;
  uploadedFileName.value = null;
  uploadedFileSizeBytes.value = 0;
  uploadProgress.value = 0;
  uploadError.value = null;
}

// ---------------------------------------------------------------------------
// 4. Submission Handling
// ---------------------------------------------------------------------------
const isSubmitting = computed(() => submitMutation.isPending.value || isUploading.value);

const resolvedReference = computed(() => {
  if (submissionType.value === 'link') {
    return linkUrl.value.trim();
  }
  if (submissionType.value === 'text') {
    return textContent.value.trim();
  }
  if (submissionType.value === 'file') {
    return uploadedStoragePath.value?.trim() || '';
  }
  return '';
});

async function handleSubmit() {
  formError.value = null;

  if (!props.task) {
    formError.value = 'No assignment selected for submission.';
    return;
  }

  // Pre-validate submission method requirements
  if (submissionType.value === 'link') {
    if (!linkUrl.value.trim()) {
      formError.value = 'Please provide a valid web URL reference.';
      return;
    }
  } else if (submissionType.value === 'text') {
    if (!textContent.value.trim()) {
      formError.value = 'Please provide your text response.';
      return;
    }
  } else if (submissionType.value === 'file') {
    if (!uploadedStoragePath.value) {
      formError.value = 'Please upload a coursework file before submitting.';
      return;
    }
  }

  // Validate entire submission contract using Zod schema
  const parsed = submitCourseworkSchema.safeParse({
    studentId: effectiveStudentId.value,
    submissionType: submissionType.value,
    submissionReference: resolvedReference.value,
    notes: notes.value ? notes.value.trim() : null,
  });

  if (!parsed.success) {
    formError.value = parsed.error.errors[0]?.message || 'Submission payload validation failed.';
    return;
  }

  try {
    const result = await submitMutation.mutateAsync({
      assignmentId: props.task.assignmentId,
      request: {
        studentId: effectiveStudentId.value,
        submissionReference: resolvedReference.value,
        notes: notes.value ? notes.value.trim() : null,
      },
    });

    emit('submitted', result);
    isModalVisible.value = false;
  } catch (err) {
    formError.value =
      err instanceof Error ? err.message : 'Failed to submit coursework. Please retry.';
  }
}
</script>

<template>
  <Dialog
    v-model:visible="isModalVisible"
    modal
    :draggable="false"
    class="!w-full md:!w-[640px] !max-w-2xl"
    data-testid="coursework-submission-modal"
  >
    <!-- Custom Modal Header -->
    <template #header>
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-700 shrink-0"
        >
          <UploadCloud class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 leading-tight">
            {{ isResubmission ? 'Resubmit Coursework Deliverable' : 'Submit Coursework Deliverable' }}
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            {{ task?.learningUnitName }} ({{ task?.learningUnitCode }}) &bull; {{ task?.title }}
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-5 pt-2">
      <!-- Resubmission Notice Banner -->
      <div
        v-if="isResubmission"
        class="p-3.5 rounded-lg bg-amber-50 border border-amber-200 flex items-start gap-2.5 text-xs text-amber-900"
        data-testid="resubmission-notice"
      >
        <Info class="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
        <div>
          <span class="font-semibold text-amber-950">Resubmission Notice:</span>
          Submitting a new deliverable will supersede your prior submission while preserving academic audit history under SIS coursework compliance (TD-07).
        </div>
      </div>

      <!-- Task Context Summary -->
      <div class="bg-slate-50 border border-slate-200 rounded-lg p-3.5 space-y-2">
        <div class="flex items-center justify-between text-xs">
          <div class="flex items-center gap-1.5 text-slate-600">
            <Calendar class="w-3.5 h-3.5 text-slate-500" />
            <span>Due Date:</span>
            <span class="font-semibold text-slate-800">
              {{ task?.dueDate ? formatDisplayDate(task.dueDate) : 'No deadline' }}
            </span>
          </div>
          <StatusPill
            v-if="task?.displayStatus"
            :status="task.displayStatus"
            size="sm"
          />
        </div>
        <p v-if="task?.description" class="text-xs text-slate-600 line-clamp-2">
          {{ task.description }}
        </p>
      </div>

      <!-- Submission Method Selection Cards -->
      <div>
        <label class="block text-xs font-semibold text-slate-700 mb-2">
          Choose Submission Method <span class="text-rose-500">*</span>
        </label>
        <div class="grid grid-cols-3 gap-3">
          <!-- Method 1: URL Link -->
          <button
            type="button"
            :class="[
              'flex flex-col items-center justify-center p-3 rounded-lg border text-center transition-all cursor-pointer',
              submissionType === 'link'
                ? 'bg-teal-50 border-teal-500 text-teal-800 shadow-sm'
                : 'bg-white border-slate-200 hover:border-slate-300 text-slate-600',
            ]"
            data-testid="select-method-link"
            @click="submissionType = 'link'"
          >
            <Link2 class="w-5 h-5 mb-1.5" :class="submissionType === 'link' ? 'text-teal-600' : 'text-slate-400'" />
            <span class="text-xs font-medium">URL Link</span>
            <span class="text-[10px] text-slate-400 mt-0.5">Docs, GitHub, cloud</span>
          </button>

          <!-- Method 2: Text Response -->
          <button
            type="button"
            :class="[
              'flex flex-col items-center justify-center p-3 rounded-lg border text-center transition-all cursor-pointer',
              submissionType === 'text'
                ? 'bg-teal-50 border-teal-500 text-teal-800 shadow-sm'
                : 'bg-white border-slate-200 hover:border-slate-300 text-slate-600',
            ]"
            data-testid="select-method-text"
            @click="submissionType = 'text'"
          >
            <FileText class="w-5 h-5 mb-1.5" :class="submissionType === 'text' ? 'text-teal-600' : 'text-slate-400'" />
            <span class="text-xs font-medium">Text Response</span>
            <span class="text-[10px] text-slate-400 mt-0.5">Type or paste text</span>
          </button>

          <!-- Method 3: File Upload -->
          <button
            type="button"
            :class="[
              'flex flex-col items-center justify-center p-3 rounded-lg border text-center transition-all cursor-pointer',
              submissionType === 'file'
                ? 'bg-teal-50 border-teal-500 text-teal-800 shadow-sm'
                : 'bg-white border-slate-200 hover:border-slate-300 text-slate-600',
            ]"
            data-testid="select-method-file"
            @click="submissionType = 'file'"
          >
            <UploadCloud class="w-5 h-5 mb-1.5" :class="submissionType === 'file' ? 'text-teal-600' : 'text-slate-400'" />
            <span class="text-xs font-medium">File Upload</span>
            <span class="text-[10px] text-slate-400 mt-0.5">PDF, DOCX, images</span>
          </button>
        </div>
      </div>

      <!-- Content Editor per Selected Method -->
      <!-- Mode 1: URL Link -->
      <div v-if="submissionType === 'link'" class="space-y-1.5" data-testid="field-submission-link">
        <label for="submission-link-input" class="block text-xs font-medium text-slate-700">
          URL Reference <span class="text-rose-500">*</span>
        </label>
        <InputText
          id="submission-link-input"
          v-model="linkUrl"
          placeholder="https://docs.google.com/... or https://github.com/..."
          class="w-full text-sm"
          data-testid="input-submission-link"
        />
        <p class="text-[11px] text-slate-500">
          Enter an accessible HTTP or HTTPS link to your coursework document or code repository.
        </p>
      </div>

      <!-- Mode 2: Text Response -->
      <div v-else-if="submissionType === 'text'" class="space-y-1.5" data-testid="field-submission-text">
        <div class="flex items-center justify-between">
          <label for="submission-text-input" class="block text-xs font-medium text-slate-700">
            Coursework Text Response <span class="text-rose-500">*</span>
          </label>
          <span class="text-[11px] text-slate-400">
            {{ textContent.length }}/1000 characters
          </span>
        </div>
        <Textarea
          id="submission-text-input"
          v-model="textContent"
          rows="6"
          maxlength="1000"
          placeholder="Type or paste your completed response or assignment answer here..."
          class="w-full text-sm"
          data-testid="input-submission-text"
        />
      </div>

      <!-- Mode 3: File Upload -->
      <div v-else-if="submissionType === 'file'" class="space-y-3" data-testid="field-submission-file">
        <label class="block text-xs font-medium text-slate-700">
          Coursework Document File <span class="text-rose-500">*</span>
        </label>

        <!-- Uploaded Success Card -->
        <div
          v-if="uploadedStoragePath"
          class="p-4 rounded-lg bg-emerald-50 border border-emerald-200 flex items-center justify-between gap-3"
          data-testid="upload-success-card"
        >
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-10 h-10 rounded-lg bg-emerald-100 flex items-center justify-center text-emerald-700 shrink-0">
              <CheckCircle2 class="w-6 h-6" />
            </div>
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-xs font-semibold text-emerald-950 truncate max-w-[280px]">
                  {{ uploadedFileName }}
                </span>
                <span class="text-[10px] px-1.5 py-0.5 rounded bg-emerald-200/60 text-emerald-800 font-medium">
                  Verified
                </span>
              </div>
              <p class="text-[11px] text-emerald-700 mt-0.5">
                {{ formatFileSize(uploadedFileSizeBytes) }} &bull; Ready for submission
              </p>
            </div>
          </div>
          <Button
            type="button"
            icon="pi pi-trash"
            severity="danger"
            text
            rounded
            size="small"
            title="Remove and choose different file"
            data-testid="btn-remove-uploaded-file"
            @click="handleRemoveUploadedFile"
          >
            <Trash2 class="w-4 h-4 text-rose-600" />
          </Button>
        </div>

        <!-- In-Progress Upload Bar -->
        <div v-else-if="isUploading" class="space-y-2 p-4 rounded-lg bg-slate-50 border border-slate-200" data-testid="upload-progress-container">
          <div class="flex items-center justify-between text-xs text-slate-700">
            <span class="font-medium">Uploading {{ selectedFile?.name }}...</span>
            <span class="font-bold text-teal-600">{{ uploadProgress }}%</span>
          </div>
          <ProgressBar :value="uploadProgress" class="!h-2" />
          <p class="text-[11px] text-slate-500">
            Uploading deliverable to institutional storage...
          </p>
        </div>

        <!-- Dropzone / PrimeVue FileUpload -->
        <div v-else>
          <FileUpload
            mode="advanced"
            name="file"
            url="/api/v1/academics/submissions/upload"
            :custom-upload="true"
            :auto="true"
            :max-file-size="MAX_FILE_SIZE_BYTES"
            accept=".pdf,.jpg,.jpeg,.png,.docx,application/pdf,image/jpeg,image/png,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            :show-upload-button="false"
            :show-cancel-button="false"
            data-testid="prime-file-upload"
            @uploader="handleCustomUpload"
            @select="handleFileSelect"
          >
            <template #empty>
              <div class="py-6 px-4 text-center cursor-pointer">
                <UploadCloud class="w-10 h-10 mx-auto text-teal-600 mb-2" />
                <p class="text-sm font-semibold text-slate-800">
                  Drag and drop coursework file here, or click to browse
                </p>
                <p class="text-xs text-slate-500 mt-1">
                  Supported formats: PDF, DOCX, PNG, JPEG (Max 25 MB)
                </p>
              </div>
            </template>
          </FileUpload>
        </div>

        <!-- Upload Error Message -->
        <div
          v-if="uploadError"
          class="p-2.5 rounded-lg bg-rose-50 border border-rose-200 text-xs text-rose-800 flex items-center gap-2"
          data-testid="upload-error-banner"
        >
          <AlertCircle class="w-4 h-4 text-rose-600 shrink-0" />
          <span>{{ uploadError }}</span>
        </div>
      </div>

      <!-- Notes to Teacher Field (Optional) -->
      <div class="space-y-1.5" data-testid="field-submission-notes">
        <div class="flex items-center justify-between">
          <label for="submission-notes-input" class="block text-xs font-medium text-slate-700">
            Submission Remarks or Notes for Instructor <span class="text-slate-400 font-normal">(optional)</span>
          </label>
          <span class="text-[11px] text-slate-400">{{ notes.length }}/2000</span>
        </div>
        <Textarea
          id="submission-notes-input"
          v-model="notes"
          rows="2"
          maxlength="2000"
          placeholder="Any comments, questions, or notes regarding your deliverable..."
          class="w-full text-xs"
          data-testid="input-submission-notes"
        />
      </div>

      <!-- Form Error Banner -->
      <div
        v-if="formError"
        class="p-3 rounded-lg bg-rose-50 border border-rose-200 text-xs text-rose-800 flex items-start gap-2"
        data-testid="form-error-banner"
      >
        <AlertCircle class="w-4 h-4 text-rose-600 mt-0.5 shrink-0" />
        <div>
          <span class="font-semibold text-rose-950">Submission Error:</span>
          {{ formError }}
        </div>
      </div>
    </div>

    <!-- Modal Footer Actions -->
    <template #footer>
      <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          size="small"
          :disabled="isSubmitting"
          data-testid="btn-cancel-submission"
          @click="isModalVisible = false"
        />
        <Button
          type="button"
          :label="isResubmission ? 'Submit Revision' : 'Submit Coursework'"
          size="small"
          class="bg-teal-600 hover:bg-teal-700 text-white font-medium px-4"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="btn-submit-coursework"
          @click="handleSubmit"
        >
          <template #icon>
            <Send class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Dialog>
</template>
