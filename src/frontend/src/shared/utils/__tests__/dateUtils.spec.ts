import { describe, it, expect } from 'vitest';
import {
  parseIsoDate,
  formatDisplayDate,
  formatDisplayDateTime,
  formatDateOnly,
  formatTimeOnly,
} from '../dateUtils';

describe('dateUtils', () => {
  describe('parseIsoDate', () => {
    it('parses valid ISO date string into Date instance', () => {
      const parsed = parseIsoDate('2026-11-12');
      expect(parsed).toBeInstanceOf(Date);
      expect(parsed.getFullYear()).toBe(2026);
      expect(parsed.getMonth()).toBe(10); // 0-indexed November
      expect(parsed.getDate()).toBe(12);
    });
  });

  describe('formatDisplayDate', () => {
    it('formats a Date object as dd MMM yyyy', () => {
      const date = new Date(2026, 10, 12);
      expect(formatDisplayDate(date)).toBe('12 Nov 2026');
    });

    it('formats an ISO date string as dd MMM yyyy', () => {
      expect(formatDisplayDate('2026-11-12')).toBe('12 Nov 2026');
    });

    it('returns empty string for invalid or empty input', () => {
      expect(formatDisplayDate('')).toBe('');
      expect(formatDisplayDate('invalid-date')).toBe('');
    });
  });

  describe('formatDisplayDateTime', () => {
    it('formats a Date object with time as dd MMM yyyy, HH:mm', () => {
      const date = new Date(2026, 10, 12, 14, 30);
      expect(formatDisplayDateTime(date)).toBe('12 Nov 2026, 14:30');
    });

    it('formats an ISO date time string as dd MMM yyyy, HH:mm', () => {
      expect(formatDisplayDateTime('2026-11-12T14:30:00')).toBe('12 Nov 2026, 14:30');
    });

    it('returns empty string for invalid input', () => {
      expect(formatDisplayDateTime('')).toBe('');
      expect(formatDisplayDateTime('invalid-datetime')).toBe('');
    });
  });

  describe('formatDateOnly', () => {
    it('returns unchanged string if already in YYYY-MM-DD format', () => {
      expect(formatDateOnly('2026-11-12')).toBe('2026-11-12');
    });

    it('formats a Date object into YYYY-MM-DD', () => {
      const date = new Date(2026, 10, 12);
      expect(formatDateOnly(date)).toBe('2026-11-12');
    });

    it('returns empty string for invalid input', () => {
      expect(formatDateOnly('')).toBe('');
      expect(formatDateOnly('not-a-date')).toBe('');
    });
  });

  describe('formatTimeOnly', () => {
    it('returns HH:mm:ss when given HH:mm:ss string', () => {
      expect(formatTimeOnly('14:30:00')).toBe('14:30:00');
    });

    it('pads seconds when given HH:mm string', () => {
      expect(formatTimeOnly('14:30')).toBe('14:30:00');
    });

    it('formats a Date object into HH:mm:ss', () => {
      const date = new Date(2026, 10, 12, 14, 30, 45);
      expect(formatTimeOnly(date)).toBe('14:30:45');
    });

    it('extracts time from ISO date-time string', () => {
      expect(formatTimeOnly('2026-11-12T14:30:00')).toBe('14:30:00');
    });

    it('returns empty string for invalid input', () => {
      expect(formatTimeOnly('')).toBe('');
      expect(formatTimeOnly('invalid-time')).toBe('');
    });
  });
});
