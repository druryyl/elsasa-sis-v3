import { describe, it, expect } from 'vitest';
import { calendarEntrySchema } from '../calendarEntry.schema';

describe('calendarEntry.schema', () => {
  const validEntry = {
    entryDate: '2026-08-17',
    designation: 'holiday' as const,
    name: 'Hari Kemerdekaan Republik Indonesia',
    description: 'National Independence Day public holiday',
    isInstructional: false,
    appliesTo: 'All',
  };

  it('validates single-day calendar entry', () => {
    const result = calendarEntrySchema.safeParse(validEntry);
    expect(result.success).toBe(true);
  });

  it('validates multi-day span entry where endDate >= entryDate', () => {
    const result = calendarEntrySchema.safeParse({
      ...validEntry,
      endDate: '2026-08-18',
    });
    expect(result.success).toBe(true);
  });

  it('fails when endDate is before entryDate', () => {
    const result = calendarEntrySchema.safeParse({
      ...validEntry,
      endDate: '2026-08-16',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message === 'End date must be greater than or equal to entry date.')).toBe(true);
    }
  });

  it('fails on unrecognized designation', () => {
    const result = calendarEntrySchema.safeParse({
      ...validEntry,
      designation: 'party_day',
    });
    expect(result.success).toBe(false);
  });
});
