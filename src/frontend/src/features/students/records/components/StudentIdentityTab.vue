<script setup lang="ts">
import Button from 'primevue/button';
import {
  IdCard,
  Phone,
  Shield,
  Pencil,
} from 'lucide-vue-next';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import type { StudentDetail } from '../types/records.types';

defineProps<{
  student: StudentDetail;
  canEdit?: boolean;
}>();

const emit = defineEmits<{
  (e: 'edit'): void;
}>();
</script>

<template>
  <div class="space-y-6" data-testid="student-identity-tab">
    <!-- Action Header when canEdit -->
    <div v-if="canEdit" class="flex justify-end">
      <Button
        size="small"
        severity="secondary"
        variant="outlined"
        class="!text-xs cursor-pointer"
        data-testid="tab-edit-identity-button"
        @click="emit('edit')"
      >
        <Pencil class="w-3.5 h-3.5 mr-1 text-teal-600" />
        <span>Edit Identity</span>
      </Button>
    </div>

    <!-- 1. Biographical Attributes -->
    <div>
      <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100 mb-3 flex items-center gap-1.5">
        <IdCard class="w-4 h-4 text-teal-600" />
        <span>Biographical &amp; Government Identification</span>
      </h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 bg-surface-50/60 dark:bg-surface-800/40 p-4 rounded-lg border border-surface-200 dark:border-surface-700">
        <div>
          <span class="text-xs text-surface-500 block">Full Legal Name</span>
          <span class="text-sm font-semibold text-surface-900 dark:text-surface-100" data-testid="identity-full-name">
            {{ student.fullName }}
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Preferred / Calling Name</span>
          <span class="text-sm text-surface-900 dark:text-surface-100" data-testid="identity-preferred-name">
            {{ student.preferredName || '—' }}
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Date &amp; Place of Birth</span>
          <span class="text-sm text-surface-900 dark:text-surface-100" data-testid="identity-dob-pob">
            {{ formatDisplayDate(student.dateOfBirth ?? '') || '—' }}
            <span v-if="student.placeOfBirth">({{ student.placeOfBirth }})</span>
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Gender &amp; Nationality</span>
          <span class="text-sm text-surface-900 dark:text-surface-100" data-testid="identity-gender-nationality">
            {{ student.gender || '—' }} • {{ student.nationality || 'Indonesian' }}
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Government ID Type</span>
          <span class="text-sm font-medium text-surface-900 dark:text-surface-100" data-testid="identity-gov-id-type">
            {{ student.governmentIdType || 'NIK' }}
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Government ID Number (NIK)</span>
          <span class="text-sm font-mono font-semibold text-surface-900 dark:text-surface-100" data-testid="identity-government-id">
            {{ student.governmentIdNumber || '—' }}
          </span>
        </div>
      </div>
    </div>

    <!-- 2. Contact Details -->
    <div>
      <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100 mb-3 flex items-center gap-1.5">
        <Phone class="w-4 h-4 text-teal-600" />
        <span>Residential &amp; Personal Contact</span>
      </h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 bg-surface-50/60 dark:bg-surface-800/40 p-4 rounded-lg border border-surface-200 dark:border-surface-700">
        <div class="md:col-span-2">
          <span class="text-xs text-surface-500 block">Residential Address</span>
          <span class="text-sm text-surface-900 dark:text-surface-100" data-testid="identity-address">
            {{ student.address || '—' }}
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Personal Phone</span>
          <span class="text-sm text-surface-900 dark:text-surface-100" data-testid="identity-phone">
            {{ student.phone || '—' }}
          </span>
        </div>
        <div>
          <span class="text-xs text-surface-500 block">Personal Email</span>
          <span class="text-sm text-surface-900 dark:text-surface-100" data-testid="identity-email">
            {{ student.email || '—' }}
          </span>
        </div>
      </div>
    </div>

    <!-- 3. Record Governance & Audit -->
    <div>
      <h3 class="text-sm font-semibold text-surface-900 dark:text-surface-100 mb-3 flex items-center gap-1.5">
        <Shield class="w-4 h-4 text-teal-600" />
        <span>Administrative Record Governance</span>
      </h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 bg-surface-50/60 dark:bg-surface-800/40 p-4 rounded-lg border border-surface-200 dark:border-surface-700 text-xs">
        <div>
          <span class="text-surface-500 block">Independent Learner Status</span>
          <span
            :class="student.isIndependent ? 'text-purple-700 dark:text-purple-400 font-semibold' : 'text-surface-700 dark:text-surface-300'"
            data-testid="identity-independent-badge"
          >
            {{ student.isIndependent ? 'Yes — Self-managed learner' : 'No — Family/guardian governance' }}
          </span>
        </div>
        <div>
          <span class="text-surface-500 block">Official Enrollment Date</span>
          <span class="font-medium text-surface-800 dark:text-surface-200" data-testid="identity-enrollment-date">
            {{ formatDisplayDate(student.enrollmentDate) }}
          </span>
        </div>
        <div>
          <span class="text-surface-500 block">Created</span>
          <span class="text-surface-700 dark:text-surface-300" data-testid="identity-created-audit">
            {{ formatDisplayDateTime(student.createdAt) }}
            <span v-if="student.createdBy">by {{ student.createdBy }}</span>
          </span>
        </div>
        <div>
          <span class="text-surface-500 block">Last Updated</span>
          <span class="text-surface-700 dark:text-surface-300" data-testid="identity-updated-audit">
            {{ formatDisplayDateTime(student.updatedAt ?? '') || 'Never updated' }}
            <span v-if="student.updatedBy">by {{ student.updatedBy }}</span>
          </span>
        </div>
        <div v-if="student.notes" class="md:col-span-2">
          <span class="text-surface-500 block">Administrative Notes</span>
          <span class="text-surface-800 dark:text-surface-200 italic" data-testid="identity-notes">
            {{ student.notes }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
