<script setup lang="ts">
import { computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import {
  GitBranch,
  Copy,
  Calendar,
  AlertCircle,
  Layers,
} from 'lucide-vue-next';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { FormField } from '@/shared/components';
import {
  supersedeStructureSchema,
  type SupersedeStructureFormData,
} from '../schemas/curriculum.schemas';
import { useSupersedeStructure } from '../composables/useCurriculum';
import type {
  LearningStructure,
  LearningStructureSummary,
} from '../types/curriculum.types';
import type { AcademicPeriodOption } from './CurriculumContextBar.vue';

const props = defineProps<{
  visible: boolean;
  structure: LearningStructure | LearningStructureSummary | null | undefined;
  periods?: AcademicPeriodOption[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'superseded', structure: LearningStructure): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

const periodOptions = computed(() => {
  return (props.periods ?? []).map((p) => ({
    label: p.label,
    value: p.id,
  }));
});

function computeNextVersion(currentVersion?: string | null): string {
  if (!currentVersion) return '2025.1';
  const parts = currentVersion.split('.');
  if (parts.length === 2 && !Number.isNaN(Number(parts[1]))) {
    return `${parts[0]}.${Number(parts[1]) + 1}`;
  }
  return `${currentVersion}.1`;
}

const { handleSubmit, errors, defineField, resetForm } =
  useForm<SupersedeStructureFormData>({
    validationSchema: toTypedSchema(supersedeStructureSchema),
    initialValues: {
      newVersion: '2025.1',
      effectiveDate: new Date().toISOString().slice(0, 10),
      academicPeriodId: props.structure?.academicPeriodId ?? '',
      academicPeriodLabel: props.structure?.academicPeriodLabel ?? '',
    },
  });

const [newVersion] = defineField('newVersion');
const [effectiveDate] = defineField('effectiveDate');
const [academicPeriodId] = defineField('academicPeriodId');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen && props.structure) {
      resetForm({
        values: {
          newVersion: computeNextVersion(props.structure.version),
          effectiveDate: new Date().toISOString().slice(0, 10),
          academicPeriodId: props.structure.academicPeriodId ?? (props.periods?.[0]?.id ?? ''),
          academicPeriodLabel: props.structure.academicPeriodLabel ?? '',
        },
      });
    }
  },
  { immediate: true },
);

const selectedPeriodLabel = computed(() => {
  if (!academicPeriodId.value) return undefined;
  const match = periodOptions.value.find((p) => p.value === academicPeriodId.value);
  return match?.label ?? undefined;
});

const supersedeMutation = useSupersedeStructure({
  onSuccess: (data) => {
    emit('update:visible', false);
    emit('superseded', data);
  },
});

const onSubmit = handleSubmit((values) => {
  if (!props.structure) return;

  supersedeMutation.mutate({
    id: props.structure.id,
    request: {
      newVersion: values.newVersion.trim(),
      effectiveDate: values.effectiveDate,
      academicPeriodId: values.academicPeriodId ? values.academicPeriodId : null,
      academicPeriodLabel: selectedPeriodLabel.value || null,
    },
  });
});

defineExpose({
  onSubmit,
});
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    header="Supersede Curriculum Framework"
    modal
    class="max-w-xl w-full"
    data-testid="supersede-structure-modal"
  >
    <template #header>
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 rounded-xl bg-indigo-50 border border-indigo-100 flex items-center justify-center shrink-0 text-indigo-600 shadow-xs">
          <GitBranch class="w-5 h-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h3 class="font-semibold text-slate-800 text-base leading-snug">Supersede Curriculum Framework</h3>
            <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700 border border-indigo-200">
              Deep Clone Versioning
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Branch the active framework into a new successor draft version with deep-cloned units and requirements.
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <!-- Deep Clone Architectural Explanation Card (TD-03) -->
      <div class="p-3.5 bg-indigo-50/70 border border-indigo-200 rounded-lg text-xs space-y-2 text-indigo-950" data-testid="supersede-explanation-card">
        <div class="flex items-center gap-1.5 font-semibold text-indigo-900">
          <Copy class="w-4 h-4 text-indigo-600" />
          <span>Automated Deep Clone Engine</span>
        </div>
        <p class="text-indigo-800 leading-relaxed">
          All existing learning units (including parent-child hierarchy) and academic requirements from
          <strong v-if="structure" class="text-indigo-950">{{ structure.name }} (v{{ structure.version }})</strong>
          will be automatically duplicated into a new <strong>draft</strong> curriculum framework.
        </p>
        <div class="flex items-start gap-1.5 text-[11px] text-indigo-700 pt-0.5">
          <AlertCircle class="w-3.5 h-3.5 mt-0.5 shrink-0 text-indigo-600" />
          <span>
            The current active framework will continue operating without interruption until the new draft is formally approved and activated by an Academic Authority.
          </span>
        </div>
      </div>

      <!-- Section 1: Predecessor Context Snapshot -->
      <div v-if="structure" class="bg-slate-50/80 border border-slate-200 rounded-lg p-3 text-xs grid grid-cols-2 gap-2 text-slate-600">
        <div>
          <span class="font-semibold text-slate-700">Predecessor Framework:</span>
          <p class="font-medium text-slate-900 truncate">{{ structure.name }}</p>
        </div>
        <div>
          <span class="font-semibold text-slate-700">Predecessor Version:</span>
          <p class="font-mono text-slate-900">v{{ structure.version }} ({{ structure.status }})</p>
        </div>
      </div>

      <!-- Section 2: Successor Specification Card -->
      <div class="bg-white border border-slate-200 rounded-lg p-3.5 space-y-3.5 shadow-2xs">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField
            id="supersede-version"
            label="New Version Identifier"
            required
            :error="errors.newVersion"
            help-text="Successor version string (e.g. 2025.1, 2.0)"
          >
            <InputText
              id="supersede-version"
              v-model="newVersion"
              placeholder="e.g. 2025.1"
              class="w-full text-sm font-mono tracking-wide"
              :class="{ 'p-invalid': errors.newVersion }"
              data-testid="supersede-version-input"
            />
          </FormField>

          <FormField
            id="supersede-effective-date"
            label="Effective Date"
            required
            :error="errors.effectiveDate"
            help-text="Date new framework will take effect"
          >
            <InputText
              id="supersede-effective-date"
              v-model="effectiveDate"
              type="date"
              class="w-full text-sm"
              :class="{ 'p-invalid': errors.effectiveDate }"
              data-testid="supersede-effective-date-input"
            />
          </FormField>
        </div>

        <FormField
          id="supersede-period"
          label="Target Academic Period"
          optional
          :error="errors.academicPeriodId"
          help-text="Assign successor to the same or a future academic period"
        >
          <Select
            id="supersede-period"
            v-model="academicPeriodId"
            :options="periodOptions"
            option-label="label"
            option-value="value"
            placeholder="Select target academic period"
            class="w-full text-sm"
            data-testid="supersede-period-select"
          />
        </FormField>
      </div>

      <!-- Modal Footer Actions -->
      <div class="flex items-center justify-between pt-3 border-t border-slate-200/80">
        <span class="text-xs text-slate-500 flex items-center gap-1">
          <span class="text-rose-500 font-bold">*</span> Required fields
        </span>
        <div class="flex items-center gap-2">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            class="text-slate-600 hover:text-slate-800"
            data-testid="supersede-modal-cancel-btn"
            @click="isDialogVisible = false"
          />
          <Button
            type="submit"
            size="small"
            class="bg-indigo-600 hover:bg-indigo-700 text-white shadow-xs px-4"
            :loading="supersedeMutation.isPending.value"
            data-testid="modal-submit-supersede-btn"
            @click="onSubmit"
          >
            <GitBranch class="w-4 h-4 mr-1.5" />
            <span>Branch Successor Framework</span>
          </Button>
        </div>
      </div>
    </form>
  </Dialog>
</template>
