import type { TimeSlotBlock, RecurringSchedule } from '../types';

export interface DayDefinition {
  id: 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday';
  label: string;
  shortLabel: string;
}

export interface SlotDefinition {
  id: 'morning' | 'afternoon';
  label: string;
  timeRange: string;
  startTime: string;
  endTime: string;
  hours: number;
}

export const DAYS: DayDefinition[] = [
  { id: 'monday', label: 'Monday', shortLabel: 'Mon' },
  { id: 'tuesday', label: 'Tuesday', shortLabel: 'Tue' },
  { id: 'wednesday', label: 'Wednesday', shortLabel: 'Wed' },
  { id: 'thursday', label: 'Thursday', shortLabel: 'Thu' },
  { id: 'friday', label: 'Friday', shortLabel: 'Fri' },
];

export const SLOTS: SlotDefinition[] = [
  {
    id: 'morning',
    label: 'Morning',
    timeRange: '08:00 – 12:00',
    startTime: '08:00',
    endTime: '12:00',
    hours: 4,
  },
  {
    id: 'afternoon',
    label: 'Afternoon',
    timeRange: '13:00 – 17:00',
    startTime: '13:00',
    endTime: '17:00',
    hours: 4,
  },
];

/**
 * Creates default 10 time blocks (Monday-Friday morning and afternoon, all available).
 */
export function createDefaultTimeBlocks(defaultAvailable = true): TimeSlotBlock[] {
  const blocks: TimeSlotBlock[] = [];
  for (const day of DAYS) {
    for (const slot of SLOTS) {
      blocks.push({
        dayOfWeek: day.id,
        startTime: slot.startTime,
        endTime: slot.endTime,
        slotType: slot.id,
        isAvailable: defaultAvailable,
      });
    }
  }
  return blocks;
}

/**
 * Parses raw input into a normalized array of TimeSlotBlocks.
 */
export function normalizeSlots(input?: TimeSlotBlock[] | RecurringSchedule | string | null): TimeSlotBlock[] {
  if (!input) {
    return createDefaultTimeBlocks(true);
  }

  let rawList: unknown[] = [];

  if (typeof input === 'string') {
    try {
      const parsed = JSON.parse(input);
      if (Array.isArray(parsed)) {
        rawList = parsed;
      } else if (parsed && typeof parsed === 'object' && Array.isArray((parsed as { slots?: unknown[] }).slots)) {
        rawList = (parsed as { slots: unknown[] }).slots;
      }
    } catch {
      return createDefaultTimeBlocks(true);
    }
  } else if (Array.isArray(input)) {
    rawList = input;
  } else if (input && typeof input === 'object' && Array.isArray(input.slots)) {
    rawList = input.slots;
  }

  const lookup = new Map<string, boolean>();
  for (const item of rawList) {
    if (item && typeof item === 'object') {
      const b = item as Record<string, unknown>;
      const day = String(b.dayOfWeek ?? '').toLowerCase();
      const slotType = b.slotType ? String(b.slotType).toLowerCase() : '';
      const startTime = String(b.startTime ?? '').slice(0, 5);
      const isAvailable = b.isAvailable !== false;

      if (slotType) {
        lookup.set(`${day}:${slotType}`, isAvailable);
      }
      if (startTime) {
        lookup.set(`${day}:${startTime}`, isAvailable);
      }
    }
  }

  const result: TimeSlotBlock[] = [];
  for (const day of DAYS) {
    for (const slot of SLOTS) {
      const byType = lookup.get(`${day.id}:${slot.id}`);
      const byTime = lookup.get(`${day.id}:${slot.startTime}`);
      const isAvailable = byType !== undefined ? byType : byTime !== undefined ? byTime : true;

      result.push({
        dayOfWeek: day.id,
        startTime: slot.startTime,
        endTime: slot.endTime,
        slotType: slot.id,
        isAvailable,
      });
    }
  }

  return result;
}
