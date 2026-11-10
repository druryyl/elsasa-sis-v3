<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { ShieldAlert, AlertTriangle, ShieldCheck, Check } from 'lucide-vue-next';
import { FormField, StatusPill } from '@/shared/components';
import { changeStandingSchema, CHANGEABLE_STANDINGS } from '../api/teachingSchemas';
import { useChangeStandingMutation } from '../composables/useFacultyMutations';
import type {
  TeacherProfileDto,
  TeacherStanding,
  ChangeTeacherStandingResponse,
} from '../api/teachingTypes';

export interface ChangeStandingModalProps {
  visible: boolean;
  teacher: TeacherProfileDto | null;
  appendTo?: string;
}

const props = withDefaults(defineProps<ChangeStandingModalProps>(), {
  visible: false,
  teacher: null,
  appendTo: undefined,
});

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'changed', response: ChangeTeacherStandingResponse): void;
  (e: 'close'): void;
}>();

const isDialogVisible = computed({
  get: () => props.visible,
  set: (val: boolean) => emit('update:visible', val),
});

// ---------------------------------------------------------------------------
// 1. Standing Options & Guidance
// ---------------------------------------------------------------------------
interface StandingOption {
  value: TeacherStanding;
  label: string;
  description: string;
}

const standingOptions: StandingOption[] = [
  {
    value: 'active',
    label: 'Active (Good Standing)',
    description: 'Faculty resource in good standing; eligible for academic teaching authorizations and scheduling.',
  },
  {
    value: 'restricted',
    label: 'Restricted (Limited Practice)',
    description: 'Operational boundary limits applied (e.g. Campus-bound, subject level cap, or pending review).',
  },
  {
    value: 'suspended',
    label: 'Suspended (Temporary Bar)',
    description: 'Temporarily barred from instructional duties; invalidates scheduling and assignments immediately.',
  },
  {
    value: 'inactive',
    label: 'Inactive (Concluded / Retired)',
    description: 'Concluded, resigned, or retired teaching relationship. No longer eligible for teaching.',
  },
];

// ---------------------------------------------------------------------------
// 2. Form State & Reset
// ---------------------------------------------------------------------------
const targetStanding = ref<TeacherStanding>('active');
const standingReason = ref('');
const errors = ref<Record<string, string>>({});

function resetForm() {
  if (props.teacher) {
    // Default to another status or current status
    const current = props.teacher.standing;
    const fallback = current === 'active' ? 'restricted' : 'active';
    targetStanding.value = (CHANGEABLE_STANDINGS.includes(current as any)
      ? current
      : fallback) as TeacherStanding;
  } else {
    targetStanding.value = 'active';
  }
  standingReason.value = '';
  errors.value = {};
}

watch(
  () => [props.visible, props.teacher],
  ([isOpen]) => {
    if (isOpen) {
      resetForm();
    }
  },
  { immediate: true },
);

// ---------------------------------------------------------------------------
// 3. Validation via changeStandingSchema
// ---------------------------------------------------------------------------
function validate(): boolean {
  errors.value = {};

  const parsePayload = {
    standing: targetStanding.value,
    standingReason: standingReason.value.trim(),
  };

  const result = changeStandingSchema.safeParse(parsePayload);
  if (!result.success) {
    const newErrors: Record<string, string> = {};
    for (const issue of result.error.issues) {
      const field = String(issue.path[0] ?? 'form');
      if (!newErrors[field]) {
        newErrors[field] = issue.message;
      }
    }
    errors.value = newErrors;
    return false;
  }

  return true;
}

// ---------------------------------------------------------------------------
// 4. Mutation & Submission
// ---------------------------------------------------------------------------
const changeMutation = useChangeStandingMutation({
  onSuccess: (data) => {
    emit('changed', data);
    handleClose();
  },
});

const isSubmitting = computed(() => changeMutation.isPending.value);

function handleClose() {
  emit('update:visible', false);
  emit('close');
}

function handleSubmit() {
  if (!props.teacher) return;
  if (!validate()) return;

  changeMutation.mutate({
    id: props.teacher.id,
    payload: {
      standing: targetStanding.value,
      standingReason: standingReason.value.trim(),
    },
  });
}
</script>

<template>
  <Dialog
    v-model:visible="isDialogVisible"
    modal
    :append-to="appendTo"
    :style="{ width: '36rem', maxWidth: '95vw' }"
    :closable="!isSubmitting"
    :close-on-escape="!isSubmitting"
    :dismissable-mask="!isSubmitting"
    data-testid="change-standing-modal"
    @hide="handleClose"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="w-9 h-9 rounded-lg bg-amber-50 dark:bg-amber-950 border border-amber-200 dark:border-amber-800 flex items-center justify-center text-amber-600 dark:text-amber-400 shrink-0"
        >
          <ShieldAlert class="w-5 h-5" />
        </div>
        <div>
          <h2 class="text-sm sm:text-base font-bold text-surface-900 dark:text-surface-100">
            Change Teacher Standing
          </h2>
          <p class="text-xs text-surface-500">
            Teaching Authority formal standing state modification (TD-03).
          </p>
        </div>
      </div>
    </template>

    <div v-if="teacher" class="space-y-4 pt-2">
      <!-- Target Teacher Context Banner -->
      <div class="p-3 bg-surface-50 dark:bg-surface-800/40 rounded-lg border border-surface-200 dark:border-surface-700/80 flex items-center justify-between gap-3 text-xs">
        <div>
          <span class="font-bold text-surface-900 dark:text-surface-100 block">
            {{ teacher.displayName || teacher.fullName }}
          </span>
          <span class="font-mono text-surface-500 text-[11px]">
            {{ teacher.teacherNumber }} • {{ teacher.teachingRoleName }}
          </span>
        </div>
        <div class="text-right shrink-0">
          <span class="text-[10px] text-surface-400 block uppercase font-medium">Current Standing</span>
          <StatusPill :status="teacher.standing" size="sm" data-testid="current-standing-pill" />
        </div>
      </div>

      <form
        class="space-y-4"
        data-testid="change-standing-form"
        @submit.prevent="handleSubmit"
      >
        <!-- Target Standing Selector -->
        <FormField
          id="targetStanding"
          label="New Teacher Standing"
          required
          :error="errors.standing"
          help-text="Select the authoritative compliance standing state for this faculty profile."
        >
          <Select
            id="targetStanding"
            v-model="targetStanding"
            :options="standingOptions"
            option-label="label"
            option-value="value"
            placeholder="Select new standing"
            class="w-full text-xs"
            :invalid="Boolean(errors.standing)"
            data-testid="select-target-standing"
          >
            <template #option="slotProps">
              <div class="py-1">
                <span class="font-semibold text-xs block text-surface-900 dark:text-surface-100">
                  {{ slotProps.option.label }}
                </span>
                <span class="text-[11px] text-surface-500 block">
                  {{ slotProps.option.description }}
                </span>
              </div>
            </template>
          </Select>
        </FormField>

        <!-- Consequence Warning Box -->
        <div
          v-if="targetStanding === 'suspended' || targetStanding === 'restricted'"
          class="p-3 bg-rose-50 dark:bg-rose-950/40 rounded-lg border border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-200 text-xs flex items-start gap-2.5"
          data-testid="consequence-warning"
        >
          <AlertTriangle class="w-4 h-4 text-rose-500 shrink-0 mt-0.5" />
          <div class="space-y-0.5">
            <span class="font-semibold block">Downstream Impact Notice</span>
            <p v-if="targetStanding === 'suspended'">
              Suspending this teacher profile will immediately prevent class assignment allocation and signal all scheduling modules of temporary bar from instructional duties.
            </p>
            <p v-else-if="targetStanding === 'restricted'">
              Restricting this profile mandates operational limitations. Ensure supervision or campus constraints are recorded on active authorizations.
            </p>
          </div>
        </div>

        <!-- Mandatory Justification Reason -->
        <FormField
          id="standingReason"
          label="Standing Justification Rationale"
          required
          help-text="Mandatory governance justification recorded in the immutable audit trail."
          :error="errors.standingReason"
        >
          <Textarea
            id="standingReason"
            v-model="standingReason"
            rows="3"
            placeholder="Detail the administrative or disciplinary rationale for this standing change..."
            class="w-full text-xs"
            :invalid="Boolean(errors.standingReason)"
            data-testid="textarea-standing-reason"
          />
        </FormField>

        <!-- Action Buttons -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-surface-200 dark:border-surface-700">
          <Button
            type="button"
            severity="secondary"
            variant="outlined"
            size="small"
            :disabled="isSubmitting"
            class="!text-xs !py-1.5 !px-3 font-medium"
            data-testid="btn-cancel-standing"
            @click="handleClose"
          >
            Cancel
          </Button>

          <Button
            type="submit"
            :severity="targetStanding === 'suspended' ? 'danger' : 'primary'"
            size="small"
            :loading="isSubmitting"
            :disabled="isSubmitting"
            class="!text-xs !py-1.5 !px-4 !gap-1.5 font-medium"
            data-testid="btn-submit-standing"
          >
            <Check class="w-4 h-4" />
            <span>Apply Standing Change</span>
          </Button>
        </div>
      </form>
    </div>
  </Dialog>
</template>
