<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import {
  Upload,
  FileText,
  AlertCircle,
  FileCheck2,
  ShieldCheck,
} from 'lucide-vue-next';
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
  { label: 'Birth Certificate (Akta Kelahiran)', value: 'Birth Certificate', desc: 'Civil registry birth certificate' },
  { label: 'Family Card (Kartu Keluarga)', value: 'Family Card', desc: 'Civil registration family document' },
  { label: 'Academic Transcript / Report', value: 'Academic Transcript', desc: 'Previous school grade report card' },
  { label: 'Immunization / Medical Record', value: 'Medical Record', desc: 'Health and vaccination certificate' },
  { label: 'National ID / KIA', value: 'Identity Card', desc: 'Student identity card or KIA' },
  { label: 'Transfer / Graduation Certificate', value: 'Graduation Certificate', desc: 'Official diploma / graduation certificate' },
  { label: 'Other Official Document', value: 'Other', desc: 'Affidavit, court decree, or legal decree' },
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
    :style="{ width: '600px' }"
    :breakpoints="{ '960px': '75vw', '640px': '95vw' }"
    data-testid="upload-document-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-teal-50 dark:bg-teal-950/60 border border-teal-200 dark:border-teal-800 flex items-center justify-center shrink-0 text-teal-600 dark:text-teal-400 shadow-xs">
          <Upload class="w-5 h-5" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2.5">
            <h3 class="font-semibold text-surface-900 dark:text-surface-100 text-base leading-snug">
              Upload Official Student Document
            </h3>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 dark:bg-teal-950 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800">
              Dossier Audit
            </span>
          </div>
          <p class="text-xs text-surface-500 dark:text-surface-400">
            Attach verified civil records, certificates, or legal decrees to the official student dossier.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-5 pt-3 pb-1" @submit.prevent="handleSubmit">
      <!-- Guidance Callout -->
      <div class="p-3.5 rounded-xl border border-teal-200 dark:border-teal-800/80 bg-teal-50/60 dark:bg-teal-950/30 text-xs text-surface-700 dark:text-surface-300">
        <div class="flex items-start gap-2.5">
          <ShieldCheck class="w-4 h-4 text-teal-600 dark:text-teal-400 shrink-0 mt-0.5" />
          <div class="space-y-1">
            <p class="font-medium text-surface-900 dark:text-surface-100">
              Audited Document Submission
            </p>
            <p class="text-surface-600 dark:text-surface-400 leading-relaxed text-[11px]">
              Uploaded files are stored securely and marked as <span class="font-semibold text-teal-800 dark:text-teal-300">Submitted</span> pending formal registrar verification. Maximum file size is 10 MB.
            </p>
          </div>
        </div>
      </div>

      <!-- Section 1: Document Classification -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-4">
        <div class="flex items-center gap-2 pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <FileCheck2 class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
          <span>Document Classification</span>
        </div>

        <!-- Document Type Select -->
        <FormField label="Official Document Type" required description="Category of certified official record.">
          <Select
            v-model="documentType"
            :options="documentTypeOptions"
            option-label="label"
            option-value="value"
            class="w-full text-sm"
            data-testid="select-doc-type"
          >
            <template #option="slotProps">
              <div class="py-1">
                <div class="font-semibold text-xs text-surface-900 dark:text-surface-100">
                  {{ slotProps.option.label }}
                </div>
                <div class="text-xs text-surface-500 mt-0.5">
                  {{ slotProps.option.desc }}
                </div>
              </div>
            </template>
          </Select>
        </FormField>
      </div>

      <!-- Section 2: File Upload Dropzone -->
      <div class="p-4 sm:p-5 rounded-xl border border-surface-200 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-900/40 space-y-3">
        <div class="flex items-center justify-between pb-2 border-b border-surface-200 dark:border-surface-800 text-xs font-semibold uppercase tracking-wider text-surface-700 dark:text-surface-300">
          <div class="flex items-center gap-2">
            <Upload class="w-3.5 h-3.5 text-teal-600 dark:text-teal-400" />
            <span>Digital Document File</span>
          </div>
          <span class="text-[11px] font-normal text-surface-500 capitalize tracking-normal">PDF, PNG, JPG (Max 10MB)</span>
        </div>

        <div
          class="border-2 border-dashed rounded-xl p-6 sm:p-8 text-center cursor-pointer transition-all duration-150"
          :class="[
            fileError
              ? 'border-rose-400 bg-rose-50/30'
              : selectedFile
                ? 'border-teal-500 bg-teal-50/50 dark:bg-teal-950/20 shadow-xs'
                : 'border-surface-300 dark:border-surface-700 hover:border-teal-400 hover:bg-surface-100/50 dark:hover:bg-surface-800/50'
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

          <div v-if="selectedFile" class="space-y-2">
            <div class="w-12 h-12 rounded-full bg-teal-100 dark:bg-teal-900/50 text-teal-700 dark:text-teal-300 flex items-center justify-center mx-auto">
              <FileText class="w-6 h-6" />
            </div>
            <div>
              <p class="text-sm font-semibold text-surface-900 dark:text-surface-100" data-testid="selected-file-name">
                {{ selectedFile.name }}
              </p>
              <p class="text-xs text-surface-500 mt-0.5">
                {{ Math.round(selectedFile.size / 1024) }} KB • Click dropzone to choose another file
              </p>
            </div>
            <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium bg-teal-100 dark:bg-teal-900/60 text-teal-800 dark:text-teal-200">
              Ready for submission
            </span>
          </div>

          <div v-else class="space-y-2.5">
            <div class="w-12 h-12 rounded-full bg-surface-100 dark:bg-surface-800 text-surface-500 flex items-center justify-center mx-auto">
              <Upload class="w-6 h-6" />
            </div>
            <div class="space-y-1">
              <p class="text-xs font-semibold text-surface-800 dark:text-surface-200">
                Click to browse or drag and drop official document
              </p>
              <p class="text-xs text-surface-500">
                Supports clear scans of PDF, PNG, JPG or JPEG files
              </p>
            </div>
          </div>
        </div>

        <p v-if="fileError" class="text-xs text-rose-600 mt-1.5 flex items-center gap-1 font-medium" data-testid="file-upload-error">
          <AlertCircle class="w-3.5 h-3.5 shrink-0" />
          {{ fileError }}
        </p>
      </div>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-3 pt-3 border-t border-surface-200 dark:border-surface-800">
        <Button
          size="small"
          variant="outlined"
          severity="secondary"
          label="Cancel"
          :disabled="isPending"
          data-testid="btn-cancel-upload-doc"
          class="!px-4 !py-2 !text-xs font-medium cursor-pointer"
          @click="handleClose"
        />
        <Button
          size="small"
          severity="primary"
          label="Upload Document"
          icon="pi pi-upload"
          :loading="isPending"
          :disabled="!selectedFile"
          class="!bg-teal-700 hover:!bg-teal-800 !border-teal-700 text-white !px-4 !py-2 !text-xs font-semibold cursor-pointer shadow-xs disabled:!opacity-50"
          data-testid="btn-submit-upload-doc"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
