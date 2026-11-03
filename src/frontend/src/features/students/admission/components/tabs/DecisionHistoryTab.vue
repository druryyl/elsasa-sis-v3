<script setup lang="ts">
import { computed } from 'vue';
import {
  Gavel,
  Calendar,
  UserCheck,
  Lock,
  ArrowRight,
  Info,
} from 'lucide-vue-next';
import { StatusPill } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  AdmissionApplicationDetailDto,
  AdmissionPeriodOptionDto,
} from '../../types/admission.types';

const props = defineProps<{
  dossier: AdmissionApplicationDetailDto;
  periods?: AdmissionPeriodOptionDto[] | undefined;
}>();

const periodsMap = computed(() => {
  const map: Record<string, string> = {};
  if (props.periods) {
    for (const p of props.periods) {
      map[p.id] = p.name;
    }
  }
  return map;
});

function getPeriodName(periodId: string | null | undefined): string {
  if (!periodId) return '';
  return periodsMap.value[periodId] ?? periodId;
}
</script>

<template>
  <div class="space-y-4" data-testid="decision-history-tab">
    <div
      class="border border-slate-200 rounded-lg p-4 space-y-4 bg-white shadow-2xs"
      data-testid="decision-history-card"
    >
      <div class="flex items-center justify-between border-b border-slate-100 pb-2.5">
        <div class="flex items-center gap-2">
          <Gavel class="w-4 h-4 text-teal-600" />
          <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
            Admission Decision &amp; Governance Audit Trail
          </h3>
        </div>
        <span
          v-if="dossier.decision"
          class="text-[11px] text-slate-500 font-mono flex items-center gap-1"
        >
          <Lock class="w-3 h-3 text-slate-400" />
          <span>Recorded {{ formatDisplayDate(dossier.decision.decisionDate) }}</span>
        </span>
      </div>

      <!-- Official Decision Recorded -->
      <div
        v-if="dossier.decision"
        class="p-4 bg-slate-50 rounded-lg border border-slate-200/90 space-y-3.5 text-xs"
        data-testid="recorded-decision-content"
      >
        <div class="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200/70 pb-3">
          <div class="flex items-center gap-2.5">
            <span class="text-slate-600 font-medium">Official Outcome:</span>
            <StatusPill :status="dossier.decision.decisionType" size="md" data-testid="decision-outcome-pill" />
          </div>
          <div class="flex items-center gap-1.5 text-slate-600 font-medium">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            <span>Effective Date: <strong>{{ formatDisplayDate(dossier.decision.effectiveDate) }}</strong></span>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <span class="text-slate-500 block">Decided By Authority</span>
            <div class="flex items-center gap-1.5 text-slate-900 font-medium mt-0.5" data-testid="decided-by">
              <UserCheck class="w-3.5 h-3.5 text-teal-600" />
              <span>{{ dossier.decision.decidedBy }}</span>
            </div>
          </div>
          <div v-if="dossier.decision.supportingReference">
            <span class="text-slate-500 block">Supporting Reference / Decree</span>
            <span class="font-mono text-slate-800 font-semibold block mt-0.5" data-testid="supporting-reference">
              {{ dossier.decision.supportingReference }}
            </span>
          </div>
        </div>

        <div v-if="dossier.decision.reason">
          <span class="text-slate-500 block mb-1">Authoritative Justification / Rationalization</span>
          <div class="bg-white p-3 rounded-md border border-slate-200 text-slate-700 italic" data-testid="decision-justification">
            "{{ dossier.decision.reason }}"
          </div>
        </div>

        <!-- Deferred Period Link / Callout -->
        <div
          v-if="dossier.decision.deferredAdmissionPeriodId"
          class="flex items-center gap-2 p-3 bg-amber-50 rounded-lg border border-amber-200 text-amber-900"
          data-testid="deferred-period-notice"
        >
          <ArrowRight class="w-4 h-4 text-amber-600 shrink-0" />
          <div>
            <span class="font-semibold">Deferred Admission Target Period:</span>
            <span class="ml-1.5 font-medium underline">
              {{ getPeriodName(dossier.decision.deferredAdmissionPeriodId) }}
            </span>
          </div>
        </div>

        <div class="pt-2 border-t border-slate-200/60 flex items-center gap-1.5 text-[11px] text-slate-400">
          <Lock class="w-3 h-3 text-slate-400 shrink-0" />
          <span>This admission decision is permanent and legally immutable under SIS admission policy.</span>
        </div>
      </div>

      <!-- No Decision Yet - Lifecycle Guidance -->
      <div
        v-else
        class="text-xs text-slate-600 bg-slate-50 p-4 rounded-lg border border-dashed border-slate-200 space-y-2"
        data-testid="no-decision-guidance"
      >
        <div class="flex items-center gap-2 font-medium text-slate-800">
          <Info class="w-4 h-4 text-teal-600 shrink-0" />
          <span>Decision Pending</span>
        </div>
        <p v-if="dossier.status === 'registered'">
          This application is currently in <strong>Registered</strong> status. Click <strong>Start Review</strong> in the action bar to begin credential verification and intake assessment.
        </p>
        <p v-else-if="dossier.status === 'under_review'">
          This application is actively <strong>Under Review</strong>. Once document checklist and diagnostic evidence are verified, authorized officers may click <strong>Record Decision</strong> to record the official outcome.
        </p>
        <p v-else-if="dossier.status === 'withdrawn'">
          This application was officially <strong>Withdrawn</strong> before an admission decision could be finalized. All records remain preserved for compliance audit trail.
        </p>
      </div>
    </div>
  </div>
</template>
