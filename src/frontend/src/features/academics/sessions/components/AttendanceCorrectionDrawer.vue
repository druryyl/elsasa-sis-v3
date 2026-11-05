<script setup lang="ts">
import { computed, watch } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import {
  ShieldAlert,
  History,
  FileCheck2,
  AlertTriangle,
  ArrowRight,
  User,
  Clock,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  correctAttendanceSchema,
  ATTENDANCE_STATUSES,
  type CorrectAttendanceFormValues,
} from '../schemas/sessions.schema';
import { useCorrectAttendanceMutation } from '../composables/useAttendance';
import type {
  AttendanceRecord,
  AttendanceStatus,
  CorrectAttendanceResponse,
} from '../types';

const props = defineProps<{
  visible: boolean;
  record: AttendanceRecord | null;
  activityId?: string;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'corrected', response: CorrectAttendanceResponse): void;
}>();

const isDrawerVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const statusOptions = ATTENDANCE_STATUSES.map((st) => ({
  label: st
    .split('_')
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' '),
  value: st,
}));

function getStatusSeverity(status: string): 'success' | 'danger' | 'warn' | 'info' | 'secondary' {
  switch (status) {
    case 'present':
      return 'success';
    case 'absent':
      return 'danger';
    case 'late':
      return 'warn';
    case 'excused_absence':
      return 'info';
    case 'partial':
      return 'warn';
    default:
      return 'secondary';
  }
}

const { handleSubmit, errors, defineField, resetForm } =
  useForm<CorrectAttendanceFormValues>({
    validationSchema: toTypedSchema(correctAttendanceSchema),
    initialValues: {
      newStatus: (props.record?.status as AttendanceStatus) ?? 'present',
      reason: '',
      evidence: '',
    },
  });

const [newStatus] = defineField('newStatus');
const [reason] = defineField('reason');
const [evidence] = defineField('evidence');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen && props.record) {
      resetForm({
        values: {
          newStatus: (props.record.status as AttendanceStatus) ?? 'present',
          reason: '',
          evidence: props.record.evidence ?? '',
        },
      });
    }
  },
);

const correctMutation = useCorrectAttendanceMutation();

const onSubmit = handleSubmit((values) => {
  if (!props.record) return;

  correctMutation.mutate(
    {
      attendanceRecordId: props.record.id,
      activityId: props.activityId ?? props.record.learningActivityId,
      studentId: props.record.studentId,
      request: {
        newStatus: values.newStatus,
        reason: values.reason.trim(),
        evidence: values.evidence ? values.evidence.trim() : null,
      },
    },
    {
      onSuccess: (data) => {
        emit('corrected', data);
        isDrawerVisible.value = false;
      },
    },
  );
});
</script>

<template>
  <Drawer
    v-model:visible="isDrawerVisible"
    position="right"
    class="w-full sm:w-[480px] md:w-[540px]"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <ShieldAlert class="h-5 w-5 text-amber-600" />
        <span class="font-semibold text-base text-slate-800">
          Auditable Attendance Correction
        </span>
      </div>
    </template>

    <div v-if="record" class="flex flex-col h-full space-y-5 pb-6">
      <!-- Statutory Grace Window Expired Warning -->
      <div class="rounded-lg border border-amber-200 bg-amber-50/80 p-3.5 text-xs text-amber-900 flex items-start gap-2.5">
        <AlertTriangle class="h-4 w-4 shrink-0 text-amber-600 mt-0.5" />
        <div>
          <span class="font-semibold">Grace Window Expired (Regulatory Audit Mode):</span>
          Same-day roll-call grace window has elapsed. Every alteration writes an immutable audit record to
          <code class="font-mono bg-amber-100 px-1 py-0.5 rounded text-[11px]">academics.attendance_corrections</code>
          requiring a mandatory justification reason.
        </div>
      </div>

      <!-- Student Record Summary -->
      <div class="rounded-xl border border-slate-200 bg-slate-50/70 p-4 space-y-2">
        <div class="flex items-center justify-between">
          <div>
            <h4 class="font-semibold text-slate-900 text-sm">
              {{ record.studentName || 'Student ID: ' + record.studentId }}
            </h4>
            <p v-if="record.studentNumber" class="text-xs text-slate-500 font-mono">
              {{ record.studentNumber }}
            </p>
          </div>
          <div class="flex items-center gap-1.5">
            <span class="text-xs text-slate-500 font-medium">Recorded:</span>
            <Tag
              :value="record.status.toUpperCase()"
              :severity="getStatusSeverity(record.status)"
            />
          </div>
        </div>

        <div class="text-[11px] text-slate-500 pt-2 border-t border-slate-200 flex flex-wrap items-center justify-between gap-2">
          <span>By: {{ record.recordedBy }}</span>
          <span>Date: {{ record.recordedDate }}</span>
        </div>
      </div>

      <!-- Correction Form -->
      <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Status Selector -->
        <FormField
          label="New Attendance Status"
          required
          :error="errors.newStatus"
          hint="Select the updated official attendance classification"
        >
          <div class="flex items-center gap-2 mb-2">
            <span class="text-xs text-slate-500">From:</span>
            <Tag :value="record.status.toUpperCase()" :severity="getStatusSeverity(record.status)" />
            <ArrowRight class="h-3.5 w-3.5 text-slate-400" />
            <span class="text-xs text-slate-500">To:</span>
          </div>

          <Select
            v-model="newStatus"
            :options="statusOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Select new status"
            class="w-full"
            :invalid="Boolean(errors.newStatus)"
          />
        </FormField>

        <!-- Mandatory Justification Reason -->
        <FormField
          label="Mandatory Justification Reason"
          required
          :error="errors.reason"
          hint="Minimum 5 characters. State parent notice, medical cert, or administrative reason."
        >
          <Textarea
            v-model="reason"
            rows="3"
            placeholder="e.g. Parent provided medical certificate dated 2026-11-04 from St. Jude Clinic."
            class="w-full"
            :invalid="Boolean(errors.reason)"
          />
        </FormField>

        <!-- Evidence Reference -->
        <FormField
          label="Evidence Reference (Optional)"
          :error="errors.evidence"
          hint="Document number, medical certificate ID, or administrative reference"
        >
          <InputText
            v-model="evidence"
            placeholder="e.g. MedCert-2026-0042 or Parent email notice"
            class="w-full"
            :invalid="Boolean(errors.evidence)"
          />
        </FormField>

        <!-- Submit Button -->
        <div class="pt-2">
          <Button
            type="submit"
            label="Save Auditable Correction"
            icon="pi pi-shield"
            class="w-full bg-amber-600 hover:bg-amber-700 text-white font-medium shadow-sm"
            :loading="correctMutation.isPending.value"
          />
        </div>
      </form>

      <!-- Past Audit History Trail -->
      <div class="pt-4 border-t border-slate-200 space-y-3">
        <div class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-slate-600">
          <History class="h-4 w-4 text-slate-400" />
          <span>Past Correction Audit Trail ({{ record.corrections?.length ?? 0 }})</span>
        </div>

        <div
          v-if="!record.corrections || record.corrections.length === 0"
          class="rounded-lg border border-dashed border-slate-200 bg-slate-50/50 p-4 text-center text-xs text-slate-500"
        >
          No prior corrections recorded. Original entry remains unmodified.
        </div>

        <div v-else class="space-y-2.5 max-h-60 overflow-y-auto pr-1">
          <div
            v-for="c in record.corrections"
            :key="c.id"
            class="rounded-lg border border-slate-200 bg-white p-3 text-xs shadow-xs space-y-1.5"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-1.5 font-medium">
                <Tag :value="c.previousStatus.toUpperCase()" severity="secondary" class="text-[10px]" />
                <ArrowRight class="h-3 w-3 text-slate-400" />
                <Tag :value="c.newStatus.toUpperCase()" :severity="getStatusSeverity(c.newStatus)" class="text-[10px]" />
              </div>
              <span class="text-[11px] text-slate-400 font-mono">
                {{ c.correctedDate }}
              </span>
            </div>

            <p class="text-slate-700 text-xs italic bg-slate-50 p-1.5 rounded border border-slate-100">
              "{{ c.reason }}"
            </p>

            <div class="flex items-center justify-between text-[11px] text-slate-400 pt-1">
              <span>By: {{ c.correctedBy }}</span>
              <span v-if="c.evidence" class="truncate max-w-[200px]" :title="c.evidence">
                Ref: {{ c.evidence }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Drawer>
</template>
