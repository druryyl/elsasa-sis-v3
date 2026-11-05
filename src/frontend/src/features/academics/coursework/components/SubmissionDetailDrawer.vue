<script setup lang="ts">
import { computed } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  FileText,
  ExternalLink,
  Download,
  AlertTriangle,
  Clock,
  User,
  Paperclip,
  ShieldAlert,
  RotateCcw,
  CheckCircle2,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import { formatDisplayDateTime } from '@/shared/utils/dateUtils';
import type { AssignmentSubmission } from '../types';

interface Props {
  visible: boolean;
  submission: AssignmentSubmission | null;
  assignmentTitle?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  submission: null,
  assignmentTitle: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'grantException', studentId: string): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const referenceType = computed<'url' | 'file' | 'text' | 'none'>(() => {
  const ref = props.submission?.submissionReference?.trim();
  if (!ref) return 'none';
  if (/^https?:\/\//i.test(ref)) return 'url';
  if (/^uploads\//i.test(ref) || /\.(pdf|docx?|xlsx?|png|jpe?g|zip|txt)$/i.test(ref)) {
    return 'file';
  }
  return 'text';
});

const fileName = computed(() => {
  if (referenceType.value !== 'file' || !props.submission?.submissionReference) return '';
  const parts = props.submission.submissionReference.split('/');
  return parts[parts.length - 1];
});

const fileDownloadUrl = computed(() => {
  if (referenceType.value !== 'file' || !props.submission?.submissionReference) return '';
  // If absolute path or starts with /, otherwise prepend /
  const ref = props.submission.submissionReference;
  return ref.startsWith('/') ? ref : `/${ref}`;
});

const isMissing = computed(() => props.submission?.displayStatus === 'missing');
const isLate = computed(() => props.submission?.displayStatus === 'late');
const isExcusedOrExempted = computed(
  () => props.submission?.displayStatus === 'excused' || props.submission?.displayStatus === 'exempted',
);

function handleGrantException() {
  if (props.submission?.studentId) {
    emit('grantException', props.submission.studentId);
    isDrawerVisible.value = false;
  }
}
</script>

<template>
  <Drawer
    v-model:visible="isDrawerVisible"
    position="right"
    class="!w-full md:!w-[560px] lg:!w-[620px]"
    data-testid="submission-detail-drawer"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-700">
          <FileText class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-lg font-bold text-slate-900 leading-tight">Submission Inspection</h2>
          <p class="text-xs text-slate-500 mt-0.5">
            Detailed coursework deliverable & timeliness audit (SCR-ACD-02)
          </p>
        </div>
      </div>
    </template>

    <div v-if="submission" class="space-y-6 py-2">
      <!-- 1. Student Participant Identity Card -->
      <div class="p-4 rounded-xl bg-slate-50 border border-slate-200 flex items-start justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <User class="w-4 h-4 text-slate-500" />
            <h3 class="font-semibold text-slate-900 text-base">
              {{ submission.studentName ?? 'Student Participant' }}
            </h3>
          </div>
          <p class="text-xs text-slate-500 flex items-center gap-2">
            <span>Student #: <strong class="text-slate-700 font-mono">{{ submission.studentNumber ?? submission.studentId }}</strong></span>
            <span v-if="submission.sectionLabel">&bull;</span>
            <span v-if="submission.sectionLabel">Section: <strong class="text-slate-700">{{ submission.sectionLabel }}</strong></span>
          </p>
        </div>

        <div class="flex flex-col items-end gap-1.5">
          <!-- Dynamic Status Pill -->
          <StatusPill
            :status="submission.displayStatus"
            :variant="isMissing ? 'rose' : undefined"
            size="md"
          />
          <Tag
            v-if="submission.isResubmission"
            value="Resubmission"
            severity="warn"
            class="text-[10px]"
          />
        </div>
      </div>

      <!-- 2. Submission Timeliness Banner -->
      <div
        v-if="isMissing"
        class="p-4 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 space-y-1"
        data-testid="missing-warning-banner"
      >
        <div class="flex items-center gap-2 font-semibold text-sm text-rose-900">
          <AlertTriangle class="w-4 h-4 text-rose-600" />
          <span>Work Is Overdue (Missing Projection)</span>
        </div>
        <p class="text-xs text-rose-700">
          The assignment deadline has expired without receipt of student deliverable. This status is dynamically projected without locking table records.
        </p>
      </div>

      <div
        v-else-if="isLate"
        class="p-4 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 space-y-1"
      >
        <div class="flex items-center gap-2 font-semibold text-sm text-amber-900">
          <Clock class="w-4 h-4 text-amber-600" />
          <span>Submitted After Due Date</span>
        </div>
        <p class="text-xs text-amber-700">
          Submission was received after the scheduled due date. Late policy penalties may apply according to academic regulations.
        </p>
      </div>

      <div
        v-else-if="submission.displayStatus === 'submitted'"
        class="p-4 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 space-y-1"
      >
        <div class="flex items-center gap-2 font-semibold text-sm text-emerald-900">
          <CheckCircle2 class="w-4 h-4 text-emerald-600" />
          <span>On-Time Submission Received</span>
        </div>
        <p class="text-xs text-emerald-700">
          Deliverable was verified and received before the deadline.
        </p>
      </div>

      <!-- 3. Deliverable / Response Evidence -->
      <div class="space-y-2">
        <label class="text-xs font-semibold text-slate-700 uppercase tracking-wider block">
          Student Deliverable / Submission Reference
        </label>

        <!-- A: External URL Reference -->
        <div
          v-if="referenceType === 'url'"
          class="p-4 rounded-xl border border-blue-200 bg-blue-50/50 space-y-3"
          data-testid="deliverable-url-view"
        >
          <div class="flex items-center gap-2 text-blue-900 font-medium text-sm">
            <ExternalLink class="w-4 h-4 text-blue-600" />
            <span>External Resource Link</span>
          </div>
          <div class="bg-white p-2.5 rounded border border-blue-200 font-mono text-xs text-blue-800 break-all select-all">
            {{ submission.submissionReference }}
          </div>
          <a
            :href="submission.submissionReference!"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-medium transition"
          >
            <span>Open Link in New Tab</span>
            <ExternalLink class="w-3.5 h-3.5" />
          </a>
        </div>

        <!-- B: Uploaded Local File Attachment -->
        <div
          v-else-if="referenceType === 'file'"
          class="p-4 rounded-xl border border-slate-200 bg-slate-50 space-y-3"
          data-testid="deliverable-file-view"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2.5">
              <div class="w-9 h-9 rounded-lg bg-white border border-slate-200 flex items-center justify-center text-slate-700">
                <Paperclip class="w-4 h-4 text-teal-600" />
              </div>
              <div>
                <p class="text-xs font-semibold text-slate-900 leading-tight">
                  {{ fileName }}
                </p>
                <p class="text-[11px] text-slate-500 font-mono mt-0.5 truncate max-w-[280px]">
                  {{ submission.submissionReference }}
                </p>
              </div>
            </div>
            <a
              :href="fileDownloadUrl"
              target="_blank"
              download
              class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-teal-600 hover:bg-teal-700 text-white text-xs font-medium transition"
            >
              <Download class="w-3.5 h-3.5" />
              <span>Download</span>
            </a>
          </div>
        </div>

        <!-- C: Inline Student Plain Text Response -->
        <div
          v-else-if="referenceType === 'text'"
          class="p-4 rounded-xl border border-slate-200 bg-white space-y-2"
          data-testid="deliverable-text-view"
        >
          <div class="flex items-center gap-2 text-slate-700 font-medium text-xs">
            <FileText class="w-4 h-4 text-teal-600" />
            <span>Inline Text Response</span>
          </div>
          <div class="p-3 bg-slate-50 rounded-lg border border-slate-200 text-slate-800 text-xs whitespace-pre-wrap font-sans leading-relaxed">
            {{ submission.submissionReference }}
          </div>
        </div>

        <!-- D: No deliverable recorded -->
        <div
          v-else
          class="p-6 rounded-xl border border-dashed border-slate-300 text-center space-y-1.5 bg-slate-50/50"
        >
          <p class="text-xs font-semibold text-slate-700">No deliverable submitted yet</p>
          <p class="text-xs text-slate-400">Student response or file reference has not been recorded.</p>
        </div>
      </div>

      <!-- 4. Submission Metadata Key-Values -->
      <div class="space-y-2 border-t border-slate-200 pt-4 text-xs">
        <div class="flex items-center justify-between py-1 border-b border-slate-100">
          <span class="text-slate-500">Submitted Timestamp:</span>
          <span class="font-medium text-slate-800">
            {{ submission.submittedDate ? formatDisplayDateTime(submission.submittedDate) : '—' }}
          </span>
        </div>
        <div class="flex items-center justify-between py-1 border-b border-slate-100">
          <span class="text-slate-500">Resubmission History:</span>
          <span class="font-medium text-slate-800">
            {{ submission.isResubmission ? 'Yes (Successor Version)' : 'Original First Attempt' }}
          </span>
        </div>
        <div v-if="submission.notes" class="py-1">
          <span class="text-slate-500 block mb-1">Submission Notes:</span>
          <p class="p-2.5 rounded bg-slate-50 border border-slate-200 text-slate-800 italic">
            {{ submission.notes }}
          </p>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="flex items-center justify-between pt-4 border-t border-slate-200">
        <Button
          type="button"
          label="Close"
          severity="secondary"
          variant="outlined"
          @click="isDrawerVisible = false"
        />
        <Button
          type="button"
          label="Grant Policy Exception"
          icon="pi pi-shield"
          severity="warn"
          data-testid="drawer-grant-exception-btn"
          @click="handleGrantException"
        />
      </div>
    </div>
  </Drawer>
</template>
