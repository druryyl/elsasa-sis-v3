<script setup lang="ts">
import { computed } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  History,
  FileText,
  ExternalLink,
  Download,
  Calendar,
  Clock,
  CheckCircle2,
  AlertTriangle,
  RotateCcw,
  Layers,
  Copy,
  Info,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import type { StudentCourseworkTask } from '../types';

interface Props {
  visible: boolean;
  task: StudentCourseworkTask | null;
}

const props = withDefaults(defineProps<Props>(), {
  task: null,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'resubmit', task: StudentCourseworkTask): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// Timeliness badge
const isLate = computed(() => props.task?.displayStatus === 'late');
const isMissing = computed(() => props.task?.displayStatus === 'missing');
const isSubmitted = computed(
  () =>
    props.task?.displayStatus === 'submitted' ||
    props.task?.displayStatus === 'late' ||
    Boolean(props.task?.submissionId),
);

// Deliverable reference inspection
const referenceType = computed<'url' | 'file' | 'text' | 'none'>(() => {
  const ref = props.task?.submissionReference?.trim();
  if (!ref) return 'none';
  if (/^https?:\/\//i.test(ref)) return 'url';
  if (/^uploads\//i.test(ref) || /\.(pdf|docx?|xlsx?|png|jpe?g|zip|txt)$/i.test(ref)) {
    return 'file';
  }
  return 'text';
});

const extractedFileName = computed(() => {
  if (referenceType.value !== 'file' || !props.task?.submissionReference) return '';
  const parts = props.task.submissionReference.split('/');
  return parts[parts.length - 1];
});

const fileDownloadUrl = computed(() => {
  if (referenceType.value !== 'file' || !props.task?.submissionReference) return '';
  const ref = props.task.submissionReference;
  return ref.startsWith('/') ? ref : `/${ref}`;
});

function handleResubmit() {
  if (props.task) {
    emit('resubmit', props.task);
    isDrawerVisible.value = false;
  }
}
</script>

<template>
  <Drawer
    v-model:visible="isDrawerVisible"
    position="right"
    class="!w-full md:!w-[560px] lg:!w-[600px]"
    data-testid="submission-history-drawer"
  >
    <!-- Header -->
    <template #header>
      <div class="flex items-start gap-3">
        <div
          class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-700 shrink-0"
        >
          <History class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-base font-bold text-slate-900 leading-tight">
            Submission History & Audit Trail
          </h2>
          <p class="text-xs text-slate-500 mt-0.5">
            {{ task?.learningUnitName }} ({{ task?.learningUnitCode }}) &bull; {{ task?.title }}
          </p>
        </div>
      </div>
    </template>

    <div v-if="task" class="space-y-6 pt-2 pb-6">
      <!-- Status & Timeliness Summary -->
      <div
        :class="[
          'p-4 rounded-xl border flex items-center justify-between gap-4',
          isLate
            ? 'bg-amber-50 border-amber-200'
            : isSubmitted
              ? 'bg-emerald-50 border-emerald-200'
              : 'bg-slate-50 border-slate-200',
        ]"
        data-testid="timeliness-summary-card"
      >
        <div class="flex items-center gap-3">
          <div
            :class="[
              'w-10 h-10 rounded-lg flex items-center justify-center shrink-0',
              isLate
                ? 'bg-amber-100 text-amber-700'
                : isSubmitted
                  ? 'bg-emerald-100 text-emerald-700'
                  : 'bg-slate-200 text-slate-600',
            ]"
          >
            <AlertTriangle v-if="isLate" class="w-5 h-5" />
            <CheckCircle2 v-else-if="isSubmitted" class="w-5 h-5" />
            <Clock v-else class="w-5 h-5" />
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="text-xs font-bold uppercase tracking-wider text-slate-600">
                Timeliness Status:
              </span>
              <StatusPill
                v-if="isLate"
                status="late"
                label="Late"
                variant="amber"
                size="sm"
              />
              <StatusPill
                v-else-if="task.displayStatus === 'submitted'"
                status="submitted"
                label="On Time"
                variant="emerald"
                size="sm"
              />
              <StatusPill
                v-else
                :status="task.displayStatus"
                size="sm"
              />
            </div>
            <p class="text-xs text-slate-600 mt-1">
              <span v-if="task.submittedDate">
                Delivered on {{ formatDisplayDateTime(task.submittedDate) }}
              </span>
              <span v-else>
                Not yet submitted
              </span>
            </p>
          </div>
        </div>

        <Tag
          v-if="task.isResubmission"
          severity="info"
          value="Revision v2+"
          class="shrink-0"
        />
      </div>

      <!-- Current Deliverable Reference -->
      <div v-if="isSubmitted" class="space-y-3" data-testid="deliverable-evidence-section">
        <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">
          Submitted Deliverable Evidence
        </h3>

        <!-- URL Link Reference -->
        <div
          v-if="referenceType === 'url'"
          class="p-4 rounded-lg bg-white border border-slate-200 shadow-sm space-y-2"
          data-testid="deliverable-link-card"
        >
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-700 flex items-center gap-1.5">
              <ExternalLink class="w-4 h-4 text-teal-600" />
              External Link Reference
            </span>
            <a
              :href="task.submissionReference || '#'"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1 text-xs text-teal-600 hover:text-teal-700 font-medium"
            >
              <span>Open Link</span>
              <ExternalLink class="w-3.5 h-3.5" />
            </a>
          </div>
          <div class="p-2.5 rounded bg-slate-50 border border-slate-100 font-mono text-xs text-slate-800 break-all select-all">
            {{ task.submissionReference }}
          </div>
        </div>

        <!-- File Upload Reference -->
        <div
          v-else-if="referenceType === 'file'"
          class="p-4 rounded-lg bg-white border border-slate-200 shadow-sm flex items-center justify-between gap-3"
          data-testid="deliverable-file-card"
        >
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-10 h-10 rounded-lg bg-teal-50 border border-teal-200 flex items-center justify-center text-teal-700 shrink-0">
              <FileText class="w-5 h-5" />
            </div>
            <div class="min-w-0">
              <p class="text-xs font-semibold text-slate-900 truncate">
                {{ extractedFileName }}
              </p>
              <p class="text-[11px] text-slate-500 mt-0.5 truncate font-mono">
                {{ task.submissionReference }}
              </p>
            </div>
          </div>
          <a
            :href="fileDownloadUrl"
            download
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-medium transition-colors shrink-0"
            data-testid="btn-download-submission"
          >
            <Download class="w-3.5 h-3.5" />
            <span>Download</span>
          </a>
        </div>

        <!-- Plain Text Response -->
        <div
          v-else-if="referenceType === 'text'"
          class="p-4 rounded-lg bg-white border border-slate-200 shadow-sm space-y-2"
          data-testid="deliverable-text-card"
        >
          <span class="text-xs font-semibold text-slate-700 flex items-center gap-1.5">
            <FileText class="w-4 h-4 text-teal-600" />
            Written Text Response
          </span>
          <div class="p-3 rounded bg-slate-50 border border-slate-100 text-xs text-slate-800 whitespace-pre-wrap leading-relaxed max-h-60 overflow-y-auto">
            {{ task.submissionReference }}
          </div>
        </div>

        <!-- Remarks / Notes -->
        <div v-if="task.submissionNotes" class="p-3.5 rounded-lg bg-slate-50 border border-slate-200 space-y-1">
          <span class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Student Submission Remarks:
          </span>
          <p class="text-xs text-slate-700 whitespace-pre-wrap leading-relaxed">
            {{ task.submissionNotes }}
          </p>
        </div>
      </div>

      <!-- Resubmission Historical Timeline (TD-07, previous_submission_id) -->
      <div class="space-y-3" data-testid="resubmission-timeline-section">
        <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">
          Coursework Lifecycle & Audit Lineage
        </h3>

        <div class="relative pl-6 space-y-6 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-200">
          <!-- Timeline Node 1: Latest Submission -->
          <div class="relative group" data-testid="timeline-node-latest">
            <div
              :class="[
                'absolute -left-6 top-0.5 w-5 h-5 rounded-full border-2 bg-white flex items-center justify-center',
                isSubmitted ? 'border-teal-600 text-teal-600' : 'border-slate-300 text-slate-400',
              ]"
            >
              <div class="w-2 h-2 rounded-full" :class="isSubmitted ? 'bg-teal-600' : 'bg-slate-300'" />
            </div>
            <div>
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-900">
                  {{ task.isResubmission ? 'Revision Submitted (Current)' : 'Deliverable Submitted' }}
                </span>
                <span v-if="task.submittedDate" class="text-[11px] text-slate-400">
                  {{ formatDisplayDateTime(task.submittedDate) }}
                </span>
              </div>
              <p class="text-xs text-slate-600 mt-0.5">
                <span v-if="isSubmitted">
                  Received by instructor with status:
                  <span class="font-semibold text-slate-800">{{ task.displayStatus }}</span>.
                </span>
                <span v-else class="text-slate-500 italic">
                  Awaiting coursework submission.
                </span>
              </p>
              <div v-if="task.submissionId" class="mt-1 font-mono text-[10px] text-slate-400">
                Submission ID: {{ task.submissionId }}
              </div>
            </div>
          </div>

          <!-- Timeline Node 2: Prior Superseded Submission (if isResubmission) -->
          <div
            v-if="task.isResubmission || task.previousSubmissionId"
            class="relative group"
            data-testid="timeline-node-prior"
          >
            <div
              class="absolute -left-6 top-0.5 w-5 h-5 rounded-full border-2 border-slate-400 bg-white flex items-center justify-center text-slate-500"
            >
              <RotateCcw class="w-3 h-3 text-slate-500" />
            </div>
            <div class="p-3 rounded-lg bg-slate-50 border border-slate-200">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-800">
                  Superseded Prior Submission
                </span>
                <Tag severity="secondary" value="Superseded" />
              </div>
              <p class="text-xs text-slate-600 mt-1">
                This deliverable superseded an earlier submission attempt. The institution retains full audit traceability under SIS coursework compliance (TD-07).
              </p>
              <div v-if="task.previousSubmissionId" class="mt-1.5 font-mono text-[10px] text-slate-500">
                Predecessor ID: {{ task.previousSubmissionId }}
              </div>
            </div>
          </div>

          <!-- Timeline Node 3: Due Date Milestone -->
          <div class="relative group" data-testid="timeline-node-due">
            <div class="absolute -left-6 top-0.5 w-5 h-5 rounded-full border-2 border-amber-400 bg-white flex items-center justify-center">
              <Clock class="w-3 h-3 text-amber-600" />
            </div>
            <div>
              <div class="flex items-center justify-between">
                <span class="text-xs font-semibold text-slate-800">Assignment Due Date</span>
                <span class="text-[11px] text-slate-500">
                  {{ task.dueDate ? formatDisplayDate(task.dueDate) : 'No due date specified' }}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-0.5">
                Official deadline for coursework completion without late assessment penalty.
              </p>
            </div>
          </div>

          <!-- Timeline Node 4: Assignment Released -->
          <div class="relative group" data-testid="timeline-node-released">
            <div class="absolute -left-6 top-0.5 w-5 h-5 rounded-full border-2 border-slate-300 bg-white flex items-center justify-center">
              <Calendar class="w-3 h-3 text-slate-400" />
            </div>
            <div>
              <div class="flex items-center justify-between">
                <span class="text-xs font-semibold text-slate-800">Coursework Released</span>
                <span class="text-[11px] text-slate-500">
                  {{ formatDisplayDate(task.releaseDate) }}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-0.5">
                Published by instructor under learning activity: {{ task.learningActivityTitle }}.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State if no task -->
    <div v-else class="py-12 text-center text-slate-500 text-sm">
      No coursework assignment selected.
    </div>

    <!-- Footer Actions -->
    <template #footer>
      <div class="flex items-center justify-between pt-3 border-t border-slate-100 w-full">
        <Button
          type="button"
          label="Close"
          severity="secondary"
          text
          size="small"
          data-testid="btn-close-drawer"
          @click="isDrawerVisible = false"
        />
        <Button
          v-if="task"
          type="button"
          :label="isSubmitted ? 'Resubmit Work' : 'Submit Work'"
          size="small"
          class="bg-teal-600 hover:bg-teal-700 text-white font-medium px-4"
          data-testid="btn-drawer-resubmit"
          @click="handleResubmit"
        >
          <template #icon>
            <RotateCcw v-if="isSubmitted" class="w-4 h-4 mr-1.5" />
            <CheckCircle2 v-else class="w-4 h-4 mr-1.5" />
          </template>
        </Button>
      </div>
    </template>
  </Drawer>
</template>
