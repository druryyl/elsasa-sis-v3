<script setup lang="ts">
import { computed } from 'vue';
import Tag from 'primevue/tag';
import {
  Calendar,
  Layers,
  FileText,
  UserCheck,
  GitBranch,
  Clock,
} from 'lucide-vue-next';
import { ContentCard } from '@/shared/components';
import type { LearningStructure } from '../types/curriculum.types';

const props = defineProps<{
  structure: LearningStructure | null;
  onSelectPredecessor?: (predecessorId: string) => void;
}>();

const emit = defineEmits<{
  (e: 'selectPredecessor', id: string): void;
}>();

const hasPredecessor = computed(() => {
  return Boolean(props.structure?.supersedesId);
});

const statusSeverity = computed(() => {
  switch (props.structure?.status) {
    case 'draft':
      return 'secondary';
    case 'submitted':
      return 'info';
    case 'active':
      return 'success';
    case 'superseded':
      return 'warn';
    case 'archived':
      return 'danger';
    default:
      return undefined;
  }
});

function handlePredecessorClick() {
  if (props.structure?.supersedesId) {
    emit('selectPredecessor', props.structure.supersedesId);
  }
}
</script>

<template>
  <div class="space-y-6" data-testid="structure-overview-tab">
    <!-- Basic Metadata Card -->
    <ContentCard title="Curriculum Framework Metadata" class="bg-white">
      <div v-if="structure" class="grid grid-cols-1 md:grid-cols-2 gap-6 text-sm">
        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Framework Name</span>
          <p class="text-base font-semibold text-slate-800" data-testid="overview-name">
            {{ structure.name }}
          </p>
        </div>

        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Academic Period</span>
          <p class="text-base text-slate-700" data-testid="overview-period">
            {{ structure.academicPeriodLabel }}
          </p>
        </div>

        <div class="space-y-1 md:col-span-2">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Description</span>
          <p class="text-slate-600 whitespace-pre-line" data-testid="overview-description">
            {{ structure.description || 'No description provided.' }}
          </p>
        </div>

        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Version & Status</span>
          <div class="flex items-center gap-2 pt-0.5">
            <span class="font-mono text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 text-slate-700 border border-slate-200">
              v{{ structure.version }}
            </span>
            <Tag
              :value="structure.status.toUpperCase()"
              :severity="statusSeverity"
              class="text-xs uppercase"
            />
          </div>
        </div>

        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Effective Dating</span>
          <p class="text-slate-700 font-mono text-xs pt-1">
            {{ structure.effectiveDate }}
            <span class="text-slate-400 font-sans mx-1">to</span>
            {{ structure.endDate || 'Active Indefinitely' }}
          </p>
        </div>

        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Approval Audit</span>
          <div v-if="structure.approvedBy" class="flex items-center gap-1.5 text-xs text-slate-700 pt-1">
            <UserCheck class="w-3.5 h-3.5 text-emerald-600" />
            <span>Approved by <strong>{{ structure.approvedBy }}</strong> on {{ structure.approvedDate }}</span>
          </div>
          <p v-else class="text-xs text-slate-400 italic pt-1">Pending formal approval</p>
        </div>

        <div class="space-y-1">
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Internal Notes</span>
          <p class="text-xs text-slate-600 pt-1">{{ structure.notes || '—' }}</p>
        </div>
      </div>

      <div v-else class="text-slate-400 italic text-sm">
        No structure selected to display overview.
      </div>
    </ContentCard>

    <!-- Lineage & Predecessor Card -->
    <ContentCard title="Curriculum Lineage & History" class="bg-white">
      <div v-if="structure && hasPredecessor" class="flex items-start gap-3 p-4 rounded-lg bg-indigo-50/50 border border-indigo-100">
        <GitBranch class="w-5 h-5 text-indigo-600 mt-0.5 shrink-0" />
        <div class="space-y-1 flex-1 text-sm">
          <h4 class="font-medium text-indigo-950">Successor Framework</h4>
          <p class="text-xs text-indigo-800">
            This framework is a deep-cloned successor of previous framework record
            <span v-if="structure.predecessorName" class="font-semibold text-indigo-950">
              {{ structure.predecessorName }} (v{{ structure.predecessorVersion }})
            </span>
            <code v-else class="px-1.5 py-0.5 bg-indigo-100/70 rounded text-[11px] font-mono text-indigo-900">
              {{ structure.supersedesId }}
            </code>.
          </p>
          <div class="pt-2">
            <button
              type="button"
              class="text-xs font-semibold text-indigo-700 hover:text-indigo-900 underline cursor-pointer inline-flex items-center gap-1"
              data-testid="link-view-predecessor"
              @click="handlePredecessorClick"
            >
              <span>Switch to Predecessor Framework Context</span>
              <span>&rarr;</span>
            </button>
          </div>
        </div>
      </div>

      <div v-else class="flex items-center gap-2 text-slate-500 text-sm py-2">
        <Layers class="w-4 h-4 text-slate-400" />
        <span>This is an initial baseline framework (no predecessor lineage).</span>
      </div>
    </ContentCard>
  </div>
</template>
