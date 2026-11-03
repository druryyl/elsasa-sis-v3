<script setup lang="ts">
import { computed } from 'vue';
import {
  UserCheck,
  Mail,
  Phone,
  MapPin,
  School,
  FileText,
  Calendar,
  Users,
  Compass,
} from 'lucide-vue-next';
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

function getPeriodName(periodId: string): string {
  return periodsMap.value[periodId] ?? periodId;
}
</script>

<template>
  <div class="space-y-6" data-testid="application-info-tab">
    <!-- Applicant Profile Summary Card -->
    <div
      class="border border-slate-200 rounded-lg p-4 space-y-4 bg-white shadow-2xs"
      data-testid="applicant-summary-card"
    >
      <div class="flex items-center justify-between border-b border-slate-100 pb-2.5">
        <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
          <UserCheck class="w-4 h-4 text-teal-600" />
          Applicant Biographical Information
        </h3>
        <span class="text-[11px] text-slate-500 font-mono">ID: {{ dossier.applicant.id }}</span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3.5 text-xs">
        <div>
          <span class="text-slate-500 block">Full Legal Name</span>
          <span class="font-medium text-slate-800" data-testid="applicant-fullname">
            {{ dossier.applicant.fullName }}
          </span>
        </div>
        <div>
          <span class="text-slate-500 block">Preferred Calling Name</span>
          <span class="text-slate-800">{{ dossier.applicant.preferredName || '—' }}</span>
        </div>
        <div>
          <span class="text-slate-500 block">Date &amp; Place of Birth</span>
          <span class="text-slate-800">
            {{ dossier.applicant.dateOfBirth ? formatDisplayDate(dossier.applicant.dateOfBirth) : '—' }}
            <span v-if="dossier.applicant.placeOfBirth">({{ dossier.applicant.placeOfBirth }})</span>
          </span>
        </div>
        <div>
          <span class="text-slate-500 block">Gender &amp; Nationality</span>
          <span class="text-slate-800">
            {{ dossier.applicant.gender || '—' }} / {{ dossier.applicant.nationality || 'Indonesian' }}
          </span>
        </div>
        <div>
          <span class="text-slate-500 block">National Identity (NIK)</span>
          <span class="font-mono text-slate-800 font-medium">
            {{ dossier.applicant.governmentIdNumber || '—' }}
            <span v-if="dossier.applicant.governmentIdType" class="text-slate-500 font-normal">
              ({{ dossier.applicant.governmentIdType }})
            </span>
          </span>
        </div>
        <div>
          <span class="text-slate-500 block">Contact Phone &amp; Email</span>
          <span class="text-slate-800">
            {{ dossier.applicant.phone || '—' }}
            <span v-if="dossier.applicant.email"> / {{ dossier.applicant.email }}</span>
          </span>
        </div>
        <div class="sm:col-span-2 lg:col-span-3">
          <span class="text-slate-500 block flex items-center gap-1">
            <MapPin class="w-3.5 h-3.5 text-slate-400" />
            Residential Mailing Address
          </span>
          <span class="text-slate-800 mt-0.5 block">{{ dossier.applicant.address || '—' }}</span>
        </div>
        <div class="sm:col-span-2 lg:col-span-3">
          <span class="text-slate-500 block flex items-center gap-1">
            <School class="w-3.5 h-3.5 text-slate-400" />
            Previous / Origin School
          </span>
          <span class="text-slate-800 mt-0.5 block">
            {{ dossier.applicant.previousSchoolName || 'None' }}
            <span v-if="dossier.applicant.previousSchoolAddress" class="text-slate-500">
              ({{ dossier.applicant.previousSchoolAddress }})
            </span>
          </span>
        </div>
        <div v-if="dossier.applicant.notes" class="sm:col-span-2 lg:col-span-3">
          <span class="text-slate-500 block flex items-center gap-1">
            <FileText class="w-3.5 h-3.5 text-slate-400" />
            Applicant Notes
          </span>
          <p class="text-xs text-slate-600 bg-slate-50 p-2.5 rounded border border-slate-100 mt-1 italic">
            "{{ dossier.applicant.notes }}"
          </p>
        </div>
      </div>
    </div>

    <!-- Application Preferences & Intake Metadata -->
    <div class="border border-slate-200 rounded-lg p-4 space-y-3 bg-white shadow-2xs">
      <div class="flex items-center justify-between border-b border-slate-100 pb-2">
        <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
          <Compass class="w-4 h-4 text-teal-600" />
          Application Preferences &amp; Target Details
        </h3>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 text-xs">
        <div>
          <span class="text-slate-500 block">Requested Entry Level</span>
          <span class="font-medium text-slate-800">{{ dossier.requestedEntryLevel }}</span>
        </div>
        <div>
          <span class="text-slate-500 block">Target Admission Period</span>
          <span class="font-medium text-slate-800">{{ getPeriodName(dossier.admissionPeriodId) }}</span>
        </div>
        <div>
          <span class="text-slate-500 block">Application Type</span>
          <span class="capitalize text-slate-800">{{ dossier.applicationType || 'new' }}</span>
        </div>
        <div>
          <span class="text-slate-500 block">Requested Campus</span>
          <span class="text-slate-800">{{ dossier.requestedCampus || 'All Campuses' }}</span>
        </div>
        <div>
          <span class="text-slate-500 block">Requested Program</span>
          <span class="text-slate-800">{{ dossier.requestedProgram || 'Regular' }}</span>
        </div>
        <div>
          <span class="text-slate-500 block flex items-center gap-1">
            <Calendar class="w-3.5 h-3.5 text-slate-400" />
            Registration Date
          </span>
          <span class="text-slate-800">{{ formatDisplayDate(dossier.registrationDate) }}</span>
        </div>
        <div v-if="dossier.notes" class="sm:col-span-2 lg:col-span-3">
          <span class="text-slate-500 block">Intake Notes</span>
          <p class="text-xs text-slate-600 bg-slate-50 p-2.5 rounded border border-slate-100 mt-1 italic">
            "{{ dossier.notes }}"
          </p>
        </div>
      </div>
    </div>

    <!-- Guardian & Household Contacts -->
    <div
      class="border border-slate-200 rounded-lg p-4 space-y-3 bg-white shadow-2xs"
      data-testid="guardian-contacts-card"
    >
      <div class="flex items-center justify-between border-b border-slate-100 pb-2">
        <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-1.5">
          <Users class="w-4 h-4 text-teal-600" />
          Guardian &amp; Household Contacts ({{ dossier.contacts?.length ?? 0 }})
        </h3>
      </div>

      <div v-if="dossier.contacts && dossier.contacts.length > 0" class="space-y-2.5">
        <div
          v-for="contact in dossier.contacts"
          :key="contact.id"
          class="p-3 rounded-lg bg-slate-50 border border-slate-200/80 text-xs flex flex-col md:flex-row md:items-start justify-between gap-2"
          data-testid="guardian-contact-item"
        >
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="font-semibold text-slate-900 text-sm">{{ contact.fullName }}</span>
              <span class="text-slate-500 text-xs font-medium">({{ contact.relationship }})</span>
              <span
                v-if="contact.isPrimary"
                class="px-2 py-0.5 rounded text-[10px] bg-teal-100 text-teal-800 font-semibold"
                data-testid="primary-guardian-badge"
              >
                Primary Guardian
              </span>
            </div>
            <div class="text-slate-600 flex flex-wrap gap-x-4 gap-y-1 text-xs pt-0.5">
              <span v-if="contact.phone" class="inline-flex items-center gap-1">
                <Phone class="w-3.5 h-3.5 text-slate-400" />
                {{ contact.phone }}
              </span>
              <span v-if="contact.email" class="inline-flex items-center gap-1">
                <Mail class="w-3.5 h-3.5 text-slate-400" />
                {{ contact.email }}
              </span>
            </div>
            <p v-if="contact.address" class="text-slate-600 flex items-start gap-1 text-[11px] pt-0.5">
              <MapPin class="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
              <span>{{ contact.address }}</span>
            </p>
          </div>
        </div>
      </div>
      <p v-else class="text-xs text-slate-400 italic">No guardian contacts recorded.</p>
    </div>
  </div>
</template>
