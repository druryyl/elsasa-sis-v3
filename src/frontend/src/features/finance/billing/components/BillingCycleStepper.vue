<script setup lang="ts">
import { Check, CalendarDays, Eye, ShieldCheck, FileCheck } from 'lucide-vue-next';

export interface StepItem {
  id: number;
  title: string;
  subtitle: string;
  icon: unknown;
}

const props = withDefaults(
  defineProps<{
    currentStep: number;
    canNavigateBack?: boolean;
  }>(),
  {
    canNavigateBack: true,
  },
);

const emit = defineEmits<{
  (e: 'update:currentStep', step: number): void;
  (e: 'stepSelect', step: number): void;
}>();

const steps: StepItem[] = [
  {
    id: 1,
    title: 'Scope & Parameters',
    subtitle: 'Charge Rule & Period',
    icon: CalendarDays,
  },
  {
    id: 2,
    title: 'Candidate Preview',
    subtitle: 'Exceptions & Validation',
    icon: Eye,
  },
  {
    id: 3,
    title: 'Confirm & Commit',
    subtitle: 'Review & Post Obligations',
    icon: ShieldCheck,
  },
  {
    id: 4,
    title: 'Completion & Next Actions',
    subtitle: 'Summary & Bill Generation',
    icon: FileCheck,
  },
];

function handleStepClick(stepId: number) {
  if (props.canNavigateBack && stepId < props.currentStep) {
    emit('update:currentStep', stepId);
    emit('stepSelect', stepId);
  }
}
</script>

<template>
  <nav aria-label="Billing Cycle Progress" class="w-full bg-white rounded-xl border border-slate-200 p-4 shadow-xs">
    <ol class="grid grid-cols-1 md:grid-cols-4 gap-4 md:gap-2">
      <li
        v-for="(step, index) in steps"
        :key="step.id"
        class="relative flex items-center md:flex-col md:items-start group"
        :class="{
          'cursor-pointer': props.canNavigateBack && step.id < props.currentStep,
          'cursor-default': step.id >= props.currentStep,
        }"
        @click="handleStepClick(step.id)"
        :data-testid="`step-indicator-${step.id}`"
      >
        <!-- Horizontal connector line for desktop -->
        <div
          v-if="index < steps.length - 1"
          class="hidden md:block absolute top-5 left-10 -right-4 h-0.5 -z-0"
          :class="step.id < props.currentStep ? 'bg-emerald-500' : 'bg-slate-200'"
        />

        <div class="flex items-center gap-3 md:flex-col md:items-start md:gap-2 w-full">
          <!-- Step circle indicator -->
          <div class="relative z-10 flex items-center justify-center">
            <!-- Completed step -->
            <div
              v-if="step.id < props.currentStep"
              class="w-10 h-10 rounded-full bg-emerald-600 text-white flex items-center justify-center shadow-xs transition-colors"
              :title="`Step ${step.id} completed (Click to navigate back)`"
            >
              <Check class="w-5 h-5 text-white stroke-[2.5]" />
            </div>

            <!-- Active step -->
            <div
              v-else-if="step.id === props.currentStep"
              class="w-10 h-10 rounded-full bg-primary-50 border-2 border-primary-600 text-primary-700 flex items-center justify-center font-bold text-sm shadow-xs"
            >
              <component :is="step.icon" class="w-5 h-5 text-primary-600" />
            </div>

            <!-- Upcoming step -->
            <div
              v-else
              class="w-10 h-10 rounded-full bg-slate-100 border border-slate-300 text-slate-400 flex items-center justify-center font-semibold text-sm"
            >
              <span>{{ step.id }}</span>
            </div>
          </div>

          <!-- Step label and subtitle -->
          <div class="flex flex-col min-w-0">
            <div class="flex items-center gap-1.5">
              <span class="text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                Step {{ step.id }}
              </span>
            </div>
            <span
              class="text-sm font-semibold truncate"
              :class="{
                'text-primary-700 font-bold': step.id === props.currentStep,
                'text-emerald-700': step.id < props.currentStep,
                'text-slate-500': step.id > props.currentStep,
              }"
            >
              {{ step.title }}
            </span>
            <span class="text-xs text-slate-500 truncate hidden sm:block">
              {{ step.subtitle }}
            </span>
          </div>
        </div>
      </li>
    </ol>
  </nav>
</template>
