<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import { Upload, FileText, AlertCircle } from 'lucide-vue-next';
import { FormField } from '@/shared/components';
import { useUploadStudentDocumentMutation } from '../composables/useStudentRecordsMutations';

const props = defineProps<{
  visible: boolean;
  studentId: string;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'saved'): void;
}>();

const documentType = ref('Birth Certificate');
const selectedFile = ref<File | null>(null);
const fileError = ref('');
const fileInputRef = ref<HTMLInputElement | null>(null);

const documentTypeOptions = [
  { label: 'Birth Certificate (Akta Kelahiran)', value: 'Birth Certificate' },
  { label: 'Family Card (Kartu Keluarga)', value: 'Family Card' },
  { label: 'Academic Transcript / Report', value: 'Academic Transcript' },
  { label: 'Immunization / Medical Record', value: 'Medical Record' },
  { label: 'National ID / KIA', value: 'Identity Card' },
  { label: 'Transfer / Graduation Certificate', value: 'Graduation Certificate' },
  { label: 'Other Official Document', value: 'Other' },
];

function resetForm() {
  documentType.value = 'Birth Certificate';
  selectedFile.value = null;
  fileError.value = '';
  if (fileInputRef.value) {
    fileInputRef.value.value = '';
  }
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

function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement;
  fileError.value = '';
  if (target.files && target.files.length > 0) {
    const file = target.files[0];
    if (file.size > 10 * 1024 * 1024) {
      fileError.value = 'File size must not exceed 10 MB.';
      selectedFile.value = null;
      return;
    }
    selectedFile.value = file;
  }
}

// Mutation
const studentIdRef = computed(() => props.studentId);
const { mutateAsync: uploadDocument, isPending } = useUploadStudentDocumentMutation(studentIdRef);

async function handleSubmit() {
  fileError.value = '';
  if (!selectedFile.value) {
    fileError.value = 'Please select a document file to upload.';
    return;
  }

  try {
    await uploadDocument({
      file: selectedFile.value,
      documentType: documentType.value,
    });
    emit('saved');
    handleClose();
  } catch {
    // Toast notification handles error
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Upload Official Student Document"
    class="w-full max-w-lg"
    data-testid="upload-document-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4 py-2" @submit.prevent="handleSubmit">
      <!-- Document Type Select -->
      <FormField label="Document Type" required description="Category of certified official record.">
        <Select
          v-model="documentType"
          :options="documentTypeOptions"
          option-label="label"
          option-value="value"
          class="w-full"
          data-testid="select-doc-type"
        />
      </FormField>

      <!-- File Upload Input -->
      <div>
        <label class="block text-xs font-medium text-surface-700 dark:text-surface-300 mb-1.5">
          Select Document File <span class="text-rose-500">*</span>
        </label>

        <div
          class="border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors"
          :class="[
            fileError
              ? 'border-rose-400 bg-rose-50/30'
              : selectedFile
                ? 'border-teal-500 bg-teal-50/40 dark:bg-teal-950/20'
                : 'border-surface-300 dark:border-surface-700 hover:border-teal-400'
          ]"
          data-testid="file-dropzone"
          @click="fileInputRef?.click()"
        >
          <input
            ref="fileInputRef"
            type="file"
            accept=".pdf,.png,.jpg,.jpeg"
            class="hidden"
            data-testid="input-doc-file"
            @change="handleFileChange"
          />

          <div v-if="selectedFile" class="space-y-1">
            <FileText class="w-8 h-8 text-teal-600 mx-auto" />
            <p class="text-sm font-semibold text-surface-900 dark:text-surface-100" data-testid="selected-file-name">
              {{ selectedFile.name }}
            </p>
            <p class="text-xs text-surface-500">
              {{ Math.round(selectedFile.size / 1024) }} KB • Click to change file
            </p>
          </div>

          <div v-else class="space-y-1">
            <Upload class="w-8 h-8 text-surface-400 mx-auto" />
            <p class="text-xs font-medium text-surface-700 dark:text-surface-300">
              Click to choose PDF or Image (max 10MB)
            </p>
            <p class="text-[11px] text-surface-400">
              Supported formats: PDF, PNG, JPG, JPEG
            </p>
          </div>
        </div>

        <p v-if="fileError" class="text-xs text-rose-600 mt-1.5 flex items-center gap-1" data-testid="file-upload-error">
          <AlertCircle class="w-3.5 h-3.5" />
          {{ fileError }}
        </p>
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
          data-testid="btn-cancel-upload-doc"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          label="Upload Document"
          :loading="isPending"
          :disabled="!selectedFile"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700"
          data-testid="btn-submit-upload-doc"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
