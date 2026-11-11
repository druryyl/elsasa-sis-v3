<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import {
  Check,
  X,
  Sun,
  Sunset,
  CalendarCheck,
  RotateCcw,
  CheckCheck,
} from 'lucide-vue-next';
import type { TimeSlotBlock, RecurringSchedule } from '../types';
import {
  DAYS,
  SLOTS,
  createDefaultTimeBlocks,
  normalizeSlots,
  type DayDefinition,
  type SlotDefinition,
} from './weeklyTimeGridConstants';

export interface WeeklyTimeGridProps {
  /**
   * Current schedule slots, structured recurring schedule, or JSON string.
   */
  modelValue?: TimeSlotBlock[] | RecurringSchedule | string | null;
  /**
   * Whether the grid is in read-only mode (interactions disabled).
   */
  readonly?: boolean;
  /**
   * Disabled state for whole grid.
   */
  disabled?: boolean;
}

const props = withDefaults(defineProps<WeeklyTimeGridProps>(), {
  modelValue: undefined,
  readonly: false,
  disabled: false,
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: TimeSlotBlock[]): void;
  (e: 'change', value: TimeSlotBlock[]): void;
}>();

const currentBlocks = ref<TimeSlotBlock[]>(normalizeSlots(props.modelValue));

watch(
  () => props.modelValue,
  (newVal) => {
    currentBlocks.value = normalizeSlots(newVal);
  },
  { deep: true },
);

function getBlock(dayId: string, slotId: string): TimeSlotBlock | undefined {
  return currentBlocks.value.find(
    (b) => String(b.dayOfWeek).toLowerCase() === dayId && b.slotType === slotId,
  );
}

function isSlotAvailable(dayId: string, slotId: string): boolean {
  const block = getBlock(dayId, slotId);
  return block ? block.isAvailable : false;
}

function toggleSlot(dayId: string, slotId: string): void {
  if (props.readonly || props.disabled) return;

  const updated = currentBlocks.value.map((b) => {
    if (String(b.dayOfWeek).toLowerCase() === dayId && b.slotType === slotId) {
      return { ...b, isAvailable: !b.isAvailable };
    }
    return { ...b };
  });

  currentBlocks.value = updated;
  emit('update:modelValue', updated);
  emit('change', updated);
}

function setAllSlots(available: boolean): void {
  if (props.readonly || props.disabled) return;

  const updated = currentBlocks.value.map((b) => ({
    ...b,
    isAvailable: available,
  }));

  currentBlocks.value = updated;
  emit('update:modelValue', updated);
  emit('change', updated);
}

function setSlotTypeOnly(targetSlotType: 'morning' | 'afternoon'): void {
  if (props.readonly || props.disabled) return;

  const updated = currentBlocks.value.map((b) => ({
    ...b,
    isAvailable: b.slotType === targetSlotType,
  }));

  currentBlocks.value = updated;
  emit('update:modelValue', updated);
  emit('change', updated);
}

const availableBlockCount = computed(() => {
  return currentBlocks.value.filter((b) => b.isAvailable).length;
});

const totalBlockCount = computed(() => DAYS.length * SLOTS.length);

const estimatedAvailableHours = computed(() => {
  return currentBlocks.value
    .filter((b) => b.isAvailable)
    .reduce((total, b) => {
      const slotDef = SLOTS.find((s) => s.id === b.slotType);
      return total + (slotDef?.hours ?? 4);
    }, 0);
});
</script>

<template>
  <div class="space-y-4" data-testid="weekly-time-grid">
    <!-- Controls & Quick Presets Header -->
    <div class="flex flex-wrap items-center justify-between gap-3 bg-slate-50 dark:bg-slate-800/50 p-3 rounded-lg border border-slate-200 dark:border-slate-700">
      <div class="flex items-center gap-2">
        <CalendarCheck class="w-5 h-5 text-emerald-600 dark:text-emerald-400" />
        <div>
          <span class="text-sm font-semibold text-slate-800 dark:text-slate-100">
            Selected Availability:
          </span>
          <span class="ml-1.5 text-sm font-bold text-emerald-600 dark:text-emerald-400">
            {{ availableBlockCount }} / {{ totalBlockCount }} blocks
          </span>
          <span class="ml-1 text-xs text-slate-500 dark:text-slate-400">
            (~{{ estimatedAvailableHours }} hrs/week capacity)
          </span>
        </div>
      </div>

      <div v-if="!readonly && !disabled" class="flex flex-wrap items-center gap-1.5">
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="text-xs py-1 px-2.5"
          data-testid="preset-select-all"
          @click="setAllSlots(true)"
        >
          <CheckCheck class="w-3.5 h-3.5 mr-1" />
          All Available
        </Button>
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="text-xs py-1 px-2.5"
          data-testid="preset-morning-only"
          @click="setSlotTypeOnly('morning')"
        >
          <Sun class="w-3.5 h-3.5 mr-1 text-amber-500" />
          Mornings Only
        </Button>
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="outlined"
          class="text-xs py-1 px-2.5"
          data-testid="preset-afternoon-only"
          @click="setSlotTypeOnly('afternoon')"
        >
          <Sunset class="w-3.5 h-3.5 mr-1 text-indigo-500" />
          Afternoons Only
        </Button>
        <Button
          type="button"
          size="small"
          severity="secondary"
          variant="text"
          class="text-xs py-1 px-2 text-slate-500 hover:text-slate-700"
          data-testid="preset-clear-all"
          @click="setAllSlots(false)"
        >
          <RotateCcw class="w-3.5 h-3.5 mr-1" />
          Clear All
        </Button>
      </div>
    </div>

    <!-- Monday–Friday Interactive Schedule Grid -->
    <div class="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 shadow-sm">
      <table class="w-full text-left border-collapse min-w-[560px]">
        <thead>
          <tr class="bg-slate-100 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 text-xs font-semibold text-slate-600 dark:text-slate-300 uppercase tracking-wider">
            <th class="py-3 px-4 w-44">Time Block</th>
            <th
              v-for="day in DAYS"
              :key="day.id"
              class="py-3 px-3 text-center"
              :data-testid="`day-header-${day.id}`"
            >
              <div>{{ day.label }}</div>
              <div class="text-[10px] font-normal text-slate-500 dark:text-slate-400">
                {{ day.shortLabel }}
              </div>
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-200 dark:divide-slate-800 text-sm">
          <tr
            v-for="slot in SLOTS"
            :key="slot.id"
            class="hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-colors"
          >
            <!-- Slot Header Label -->
            <td class="py-3.5 px-4 bg-slate-50/70 dark:bg-slate-800/40 border-r border-slate-200 dark:border-slate-700">
              <div class="flex items-center gap-2">
                <Sun v-if="slot.id === 'morning'" class="w-4 h-4 text-amber-500 flex-shrink-0" />
                <Sunset v-else class="w-4 h-4 text-indigo-500 flex-shrink-0" />
                <div>
                  <div class="font-medium text-slate-900 dark:text-slate-100">
                    {{ slot.label }}
                  </div>
                  <div class="text-xs text-slate-500 dark:text-slate-400">
                    {{ slot.timeRange }}
                  </div>
                </div>
              </div>
            </td>

            <!-- Day Cells -->
            <td
              v-for="day in DAYS"
              :key="`${day.id}-${slot.id}`"
              class="py-2.5 px-3 text-center"
            >
              <button
                type="button"
                :disabled="readonly || disabled"
                :aria-pressed="isSlotAvailable(day.id, slot.id)"
                :aria-label="`${day.label} ${slot.label}: ${isSlotAvailable(day.id, slot.id) ? 'Available' : 'Unavailable'}`"
                :data-testid="`slot-${day.id}-${slot.id}`"
                :class="[
                  'w-full py-2.5 px-2 rounded-lg text-xs font-semibold transition-all border flex flex-col items-center justify-center gap-1 cursor-pointer select-none',
                  readonly || disabled ? 'cursor-default opacity-85' : 'hover:scale-[1.02] active:scale-[0.98]',
                  isSlotAvailable(day.id, slot.id)
                    ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-200 border-emerald-300 dark:border-emerald-700 shadow-xs'
                    : 'bg-slate-50 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500 border-dashed border-slate-300 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800',
                ]"
                @click="toggleSlot(day.id, slot.id)"
              >
                <div class="flex items-center gap-1">
                  <Check
                    v-if="isSlotAvailable(day.id, slot.id)"
                    class="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400 stroke-[2.5]"
                  />
                  <X
                    v-else
                    class="w-3.5 h-3.5 text-slate-400 dark:text-slate-500 stroke-[2]"
                  />
                  <span>{{ isSlotAvailable(day.id, slot.id) ? 'Available' : 'Unavailable' }}</span>
                </div>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Footnote / Legend -->
    <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 px-1">
      <div class="flex items-center gap-4">
        <div class="flex items-center gap-1.5">
          <span class="inline-block w-3 h-3 rounded bg-emerald-100 dark:bg-emerald-900 border border-emerald-400" />
          <span>Available for Teaching Assignments</span>
        </div>
        <div class="flex items-center gap-1.5">
          <span class="inline-block w-3 h-3 rounded bg-slate-100 dark:bg-slate-800 border border-dashed border-slate-300 dark:border-slate-700" />
          <span>Blocked / Unavailable</span>
        </div>
      </div>
      <div v-if="!readonly">
        Click any block to toggle availability status.
      </div>
    </div>
  </div>
</template>
