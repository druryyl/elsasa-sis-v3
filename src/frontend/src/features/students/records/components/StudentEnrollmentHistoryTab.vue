<script setup lang="ts">
import { RouterLink } from 'vue-router';
import {
  GraduationCap,
  Shield,
  CheckCircle2,
  ExternalLink,
  Building2,
  Calendar,
} from 'lucide-vue-next';
import { EmptyState } from '@/shared/components';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { StudentDetail } from '../types/records.types';

defineProps<{
  student: StudentDetail;
}>();
</script>

<template>
  <div class="space-y-4" data-testid="student-enrollment-history-tab">
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
      <p class="text-xs text-surface-500">
        Historical placement snapshot and payment clearance audit captured at the time of official enrollment.
      </p>

      <!-- Originating Application Link -->
      <RouterLink
        v-if="student.applicantId"
        to="/students/admission"
        class="inline-flex items-center gap-1.5 text-xs font-semibold text-teal-700 hover:text-teal-800 dark:text-teal-400 dark:hover:text-teal-300 py-1 px-2.5 rounded border border-teal-200 dark:border-teal-800 bg-teal-50 dark:bg-teal-950 transition-colors self-start sm:self-auto"
        data-testid="link-originating-application"
      >
        <ExternalLink class="w-3.5 h-3.5" />
        <span>Originating Application Dossier</span>
      </RouterLink>
    </div>

    <!-- Empty State -->
    <div
      v-if="!student.enrollmentHistory || student.enrollmentHistory.length === 0"
      data-testid="empty-enrollment-history"
    >
      <EmptyState
        title="No enrollment history found"
        description="Initial administrative placement record is not available."
        size="sm"
      >
        <template #icon>
          <div class="w-12 h-12 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center mx-auto mb-2 text-surface-400">
            <GraduationCap class="w-6 h-6" />
          </div>
        </template>
      </EmptyState>
    </div>

    <!-- History Items -->
    <div v-else class="space-y-3" data-testid="enrollment-history-list">
      <div
        v-for="(hist, idx) in student.enrollmentHistory"
        :key="idx"
        class="p-4 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50/50 dark:bg-surface-800/40 space-y-3"
        :data-testid="`enrollment-history-card-${idx}`"
      >
        <div class="flex items-center justify-between">
          <span class="font-bold text-sm text-surface-900 dark:text-surface-100 flex items-center gap-1.5">
            <GraduationCap class="w-4 h-4 text-teal-600" />
            Enrollment Event — {{ formatDisplayDate(hist.enrollmentDate) }}
          </span>
          <span class="text-xs text-surface-500" data-testid="history-approved-by">
            Approved by: <strong class="text-surface-700 dark:text-surface-300">{{ hist.approvedBy }}</strong>
          </span>
        </div>

        <!-- Read-only Placement Parameters -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs pt-2 border-t border-surface-200/60 dark:border-surface-700/60">
          <div>
            <span class="text-surface-500 block">Campus</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="history-placement-campus">
              {{ hist.campus }}
            </span>
          </div>
          <div>
            <span class="text-surface-500 block">School Level</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="history-placement-level">
              {{ hist.level }}
            </span>
          </div>
          <div>
            <span class="text-surface-500 block">Academic Year</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="history-placement-year">
              {{ hist.academicYear }}
            </span>
          </div>
          <div>
            <span class="text-surface-500 block">Program / Cohort</span>
            <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="history-placement-program">
              {{ hist.program || 'Standard' }} {{ hist.cohort ? `(${hist.cohort})` : '' }}
            </span>
          </div>
        </div>

        <!-- Payment Clearance Snapshot -->
        <div
          class="p-2.5 rounded bg-surface-100/70 dark:bg-surface-800 text-xs flex flex-wrap items-center justify-between gap-2"
          data-testid="history-payment-gate"
        >
          <span class="font-medium text-surface-700 dark:text-surface-300">
            Payment Clearance Gate:
          </span>
          <span
            v-if="hist.paymentWaived"
            class="font-semibold text-purple-700 dark:text-purple-300 flex items-center gap-1"
            data-testid="payment-gate-waived"
          >
            <Shield class="w-3.5 h-3.5" />
            Authorized Fee Waiver (Decree: {{ hist.waiverReference || 'Recorded' }})
          </span>
          <span
            v-else-if="hist.paymentConfirmed"
            class="font-semibold text-emerald-700 dark:text-emerald-300 flex items-center gap-1"
            data-testid="payment-gate-confirmed"
          >
            <CheckCircle2 class="w-3.5 h-3.5" />
            Confirmed (Ref: {{ hist.paymentReference || 'Cleared' }})
          </span>
          <span v-else class="text-surface-500 italic" data-testid="payment-gate-unrecorded">
            Unrecorded
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
