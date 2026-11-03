<script setup lang="ts">
import { ref } from 'vue';
import Button from 'primevue/button';
import {
  FileText,
  Upload,
  Download,
  ShieldCheck,
  CheckCircle2,
} from 'lucide-vue-next';
import { StatusPill, EmptyState } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { getStudentDocumentDownloadUrl } from '../api/records.api';
import type { StudentDetail, StudentDocumentItem } from '../types/records.types';
import UploadDocumentModal from './UploadDocumentModal.vue';
import VerifyDocumentModal from './VerifyDocumentModal.vue';

const props = defineProps<{
  student: StudentDetail;
  canEdit?: boolean;
}>();

const emit = defineEmits<{
  (e: 'uploadDocument'): void;
}>();

const isUploadModalOpen = ref(false);
const isVerifyModalOpen = ref(false);
const selectedDocForVerify = ref<StudentDocumentItem | null>(null);

function handleOpenUpload() {
  isUploadModalOpen.value = true;
  emit('uploadDocument');
}

function handleOpenVerify(doc: StudentDocumentItem) {
  selectedDocForVerify.value = doc;
  isVerifyModalOpen.value = true;
}
</script>

<template>
  <div class="space-y-4" data-testid="student-documents-tab">
    <!-- Header with Action -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <p class="text-xs text-surface-500">
        Official student records documents, promoted certificates, and verification audit trail.
      </p>
      <Button
        v-if="canEdit"
        size="small"
        severity="primary"
        class="!text-xs cursor-pointer !bg-teal-700 hover:!bg-teal-800 !border-teal-700 self-start sm:self-auto"
        data-testid="tab-upload-document-button"
        @click="handleOpenUpload"
      >
        <Upload class="w-3.5 h-3.5 mr-1" />
        <span>Upload Document</span>
      </Button>
    </div>

    <!-- Empty State -->
    <div
      v-if="!student.documents || student.documents.length === 0"
      data-testid="empty-documents"
    >
      <EmptyState
        title="No official documents"
        description="No documents have been uploaded or promoted for this student yet."
        size="sm"
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center mx-auto mb-2 text-surface-400">
            <FileText class="w-6 h-6" />
          </div>
        </template>
      </EmptyState>
    </div>

    <!-- Documents Checklist Table -->
    <div v-else class="space-y-2" data-testid="documents-checklist">
      <div
        v-for="doc in student.documents"
        :key="doc.id"
        class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3.5 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/40"
        :data-testid="`document-item-${doc.id}`"
      >
        <!-- Info Column -->
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <FileText class="w-4 h-4 text-teal-600 shrink-0" />
            <span class="font-semibold text-sm text-surface-900 dark:text-surface-100" data-testid="document-type">
              {{ doc.documentType }}
            </span>
            <StatusPill :status="doc.submissionStatus" size="sm" data-testid="document-status" />
          </div>

          <div class="text-xs text-surface-500 flex flex-wrap items-center gap-x-2 gap-y-1">
            <span v-if="doc.fileName" class="font-mono text-surface-700 dark:text-surface-300" data-testid="document-filename">
              {{ doc.fileName }}
            </span>
            <span v-if="doc.fileSizeBytes" class="text-surface-400">
              ({{ Math.round(doc.fileSizeBytes / 1024) }} KB)
            </span>
            <span v-if="doc.submittedDate" data-testid="document-submitted-date">
              • Submitted {{ formatDisplayDate(doc.submittedDate) }}
            </span>
            <span v-if="doc.verifiedDate && doc.verifiedBy" class="text-teal-700 dark:text-teal-400" data-testid="document-verified-audit">
              • Verified {{ formatDisplayDate(doc.verifiedDate) }} by {{ doc.verifiedBy }}
            </span>
          </div>
        </div>

        <!-- Actions Column -->
        <div class="flex items-center gap-2 shrink-0">
          <!-- Download Stream Action -->
          <a
            :href="getStudentDocumentDownloadUrl(student.id, doc.id)"
            target="_blank"
            download
            class="inline-flex items-center gap-1 text-xs font-medium text-teal-700 hover:text-teal-800 dark:text-teal-400 dark:hover:text-teal-300 py-1 px-2.5 rounded border border-teal-200 dark:border-teal-800 bg-teal-50 dark:bg-teal-950 transition-colors"
            data-testid="btn-download-document"
          >
            <Download class="w-3.5 h-3.5" />
            <span>Download</span>
          </a>

          <!-- Verify / Reject Action -->
          <Button
            v-if="canEdit"
            size="small"
            variant="outlined"
            severity="secondary"
            class="!text-xs cursor-pointer !py-1 !px-2.5"
            data-testid="btn-verify-document"
            @click="handleOpenVerify(doc)"
          >
            <ShieldCheck class="w-3.5 h-3.5 mr-1 text-teal-600" />
            <span>Verify</span>
          </Button>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <UploadDocumentModal
      v-model:visible="isUploadModalOpen"
      :student-id="student.id"
    />

    <VerifyDocumentModal
      v-model:visible="isVerifyModalOpen"
      :student-id="student.id"
      :document="selectedDocForVerify"
    />
  </div>
</template>
