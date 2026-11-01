<script setup lang="ts">
import { computed, useId } from 'vue';

export interface FormFieldProps {
  id?: string | undefined;
  label?: string | undefined;
  required?: boolean | undefined;
  optional?: boolean | undefined;
  helpText?: string | undefined;
  error?: string | string[] | null | undefined;
  class?: string | undefined;
  labelClass?: string | undefined;
  errorClass?: string | undefined;
  helpClass?: string | undefined;
}

const props = withDefaults(defineProps<FormFieldProps>(), {
  id: undefined,
  label: undefined,
  required: false,
  optional: false,
  helpText: undefined,
  error: undefined,
  class: undefined,
  labelClass: undefined,
  errorClass: undefined,
  helpClass: undefined,
});

defineSlots<{
  default?: (props: {
    id: string;
    ariaDescribedby: string | undefined;
    ariaInvalid: boolean;
  }) => unknown;
  label?: () => unknown;
  help?: () => unknown;
  error?: (props: { error: string | string[] | null | undefined }) => unknown;
}>();

const autoId = useId();
const fieldId = computed(() => props.id || autoId);
const errorId = computed(() => `${fieldId.value}-error`);
const helpId = computed(() => `${fieldId.value}-help`);

const errorList = computed<string[]>(() => {
  if (!props.error) return [];
  if (Array.isArray(props.error)) {
    return props.error.filter((e): e is string => typeof e === 'string' && e.trim().length > 0);
  }
  return typeof props.error === 'string' && props.error.trim().length > 0 ? [props.error] : [];
});

const hasError = computed(() => errorList.value.length > 0);

const ariaDescribedby = computed(() => {
  const ids: string[] = [];
  if (hasError.value) ids.push(errorId.value);
  if (props.helpText) ids.push(helpId.value);
  return ids.length > 0 ? ids.join(' ') : undefined;
});
</script>

<template>
  <div :class="['w-full flex flex-col', props.class]">
    <label
      v-if="label || $slots.label"
      :for="fieldId"
      :class="['block text-sm font-medium text-slate-700 mb-1.5', labelClass]"
    >
      <slot name="label">{{ label }}</slot>
      <span
        v-if="required"
        class="text-rose-600 font-semibold ml-0.5"
        aria-hidden="true"
        data-testid="required-indicator"
        >*</span
      >
      <span
        v-else-if="optional"
        class="text-xs text-slate-400 font-normal ml-1"
        data-testid="optional-indicator"
        >(Optional)</span
      >
    </label>

    <div class="relative">
      <slot :id="fieldId" :aria-describedby="ariaDescribedby" :aria-invalid="hasError" />
    </div>

    <!-- Help text rendered when available -->
    <p
      v-if="helpText || $slots.help"
      :id="helpId"
      :class="['mt-1 text-xs text-slate-500', helpClass]"
      data-testid="form-field-help"
    >
      <slot name="help">
        {{ helpText }}
      </slot>
    </p>

    <!-- Error message rendered with red validation text -->
    <div
      v-if="hasError || $slots.error"
      :id="errorId"
      role="alert"
      aria-live="polite"
      :class="['mt-1.5 text-xs font-medium text-rose-600', errorClass]"
      data-testid="form-field-error"
    >
      <slot name="error" :error="props.error">
        <p v-for="(errMsg, idx) in errorList" :key="idx" class="flex items-center gap-1">
          <span>{{ errMsg }}</span>
        </p>
      </slot>
    </div>
  </div>
</template>
