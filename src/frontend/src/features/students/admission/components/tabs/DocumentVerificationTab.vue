<script setup lang="ts">
/* global window */
import { computed } from 'vue';
import Button from 'primevue/button';
import {
  FileCheck,
  Download,
  CheckCircle2,
  FileQuestion,
  File,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useAuthStore } from '@/app/stores/auth.store';
import { ADMISSION_PERMISSIONS } from '../../constants/permissions';
import { downloadApplicantDocumentUrl } from '../../api/admission.api';
import type { ApplicantDocumentDto } from '../../types/admission.types';

const props = defineProps<{
  applicationId: string;
  documents: ApplicantDocumentDto[];
  isReadOnly?: boolean;
}>();

const emit = defineEmits<{
  (e: 'verify', document: ApplicantDocumentDto): void;
}>();

const authStore = useAuthStore();
const canVerify = computed(() =>
  authStore.hasPermission(ADMISSION_PERMISSIONS.VERIFY_DOCUMENT) && !props.isReadOnly,
);

function formatDocType(type: string): string {
  return (type || '')
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

function handleDownloadFile(doc: ApplicantDocumentDto) {
  if (!doc.fileName && !doc.fileReference) return;
  const url = downloadApplicantDocumentUrl(props.applicationId, doc.id);
  window.open(url, '_blank');
}

function handleOpenVerifyModal(doc: ApplicantDocumentDto) {
  emit('verify', doc);
}
</script>

<template>
  <div class="space-y-4" data-testid="document-verification-tab">
    <div
      class="border border-slate-200 rounded-lg p-4 space-y-3 bg-white shadow-2xs"
      data-testid="documents-checklist-card"
    >
      <div class="flex items-center justify-between border-b border-slate-100 pb-2.5">
        <div class="flex items-center gap-2">
          <FileCheck class="w-4 h-4 text-teal-600" />
          <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
            Credential Documents Checklist ({{ documents.length }})
          </h3>
        </div>
      </div>

      <div v-if="documents && documents.length > 0" class="overflow-x-auto">
        <table class="w-full text-xs text-left text-slate-600 border border-slate-200 rounded-lg overflow-hidden" data-testid="documents-table">
          <thead class="bg-slate-50 text-[11px] font-semibold text-slate-700 uppercase border-b border-slate-200">
            <tr>
              <th scope="col" class="py-2.5 px-3">Document Type</th>
              <th scope="col" class="py-2.5 px-3">Status</th>
              <th scope="col" class="py-2.5 px-3">File Name</th>
              <th scope="col" class="py-2.5 px-3">Submitted</th>
              <th scope="col" class="py-2.5 px-3">Verification Details</th>
              <th scope="col" class="py-2.5 px-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr
              v-for="doc in documents"
              :key="doc.id"
              class="hover:bg-slate-50/70 transition-colors"
              :data-testid="`document-row-${doc.id}`"
            >
              <!-- Document Type -->
              <td class="py-3 px-3 font-medium text-slate-900">
                <div class="flex items-center gap-1.5">
                  <File class="w-3.5 h-3.5 text-slate-400 shrink-0" />
                  <span>{{ formatDocType(doc.documentType) }}</span>
                </div>
              </td>

              <!-- Status -->
              <td class="py-3 px-3">
                <StatusPill :status="doc.submissionStatus" size="sm" />
              </td>

              <!-- File Name -->
              <td class="py-3 px-3">
                <span v-if="doc.fileName" class="font-mono text-slate-700 break-all">
                  {{ doc.fileName }}
                </span>
                <span v-else class="text-slate-400 italic">No file attached</span>
              </td>

              <!-- Submitted Date -->
              <td class="py-3 px-3 whitespace-nowrap">
                <span v-if="doc.submittedDate" class="text-slate-600">
                  {{ formatDisplayDate(doc.submittedDate) }}
                </span>
                <span v-else class="text-slate-400 italic">—</span>
              </td>

              <!-- Verification Details -->
              <td class="py-3 px-3">
                <div v-if="doc.verifiedBy" class="space-y-0.5">
                  <div class="font-medium text-slate-800">
                    {{ doc.verifiedBy }}
                  </div>
                  <div v-if="doc.verifiedDate" class="text-[11px] text-slate-500">
                    {{ formatDisplayDate(doc.verifiedDate) }}
                  </div>
                  <div v-if="doc.notes" class="text-[11px] text-slate-600 italic mt-0.5">
                    "{{ doc.notes }}"
                  </div>
                </div>
                <span v-else class="text-slate-400 italic">Pending Verification</span>
              </td>

              <!-- Actions -->
              <td class="py-3 px-3 text-right whitespace-nowrap">
                <div class="inline-flex items-center gap-1.5 justify-end">
                  <!-- Download / View Button -->
                  <Button
                    size="small"
                    severity="secondary"
                    variant="outlined"
                    :disabled="!doc.fileName && !doc.fileReference"
                    class="!text-xs !py-1 !px-2 cursor-pointer"
                    title="Download / View Document File"
                    data-testid="download-document-button"
                    @click="handleDownloadFile(doc)"
                  >
                    <Download class="w-3.5 h-3.5 mr-1" />
                    <span>View</span>
                  </Button>

                  <!-- Verify / Audit Button -->
                  <Button
                    size="small"
                    severity="primary"
                    variant="outlined"
                    :disabled="!canVerify || doc.submissionStatus === 'not_submitted'"
                    class="!text-xs !py-1 !px-2 cursor-pointer !border-teal-600 !text-teal-700 hover:!bg-teal-50"
                    title="Verify or Reject Document"
                    data-testid="verify-document-button"
                    @click="handleOpenVerifyModal(doc)"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5 mr-1 text-teal-600" />
                    <span>Verify</span>
                  </Button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div
        v-else
        class="text-center py-8 px-4 border border-dashed border-slate-200 rounded-lg text-slate-500"
        data-testid="no-documents-empty"
      >
        <FileQuestion class="w-8 h-8 text-slate-300 mx-auto mb-2" />
        <p class="text-xs font-medium">No credential documents attached.</p>
        <p class="text-[11px] text-slate-400 mt-0.5">Documents uploaded by the applicant will appear here.</p>
      </div>
    </div>
  </div>
</template>
