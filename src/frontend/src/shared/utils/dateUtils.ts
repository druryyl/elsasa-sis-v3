import { format, parseISO, isValid } from 'date-fns';

/**
 * Parses an ISO 8601 date string into a Date instance.
 */
export function parseIsoDate(dateString: string): Date {
  return parseISO(dateString);
}

/**
 * Internal helper to safely normalize Date | string inputs into a valid Date instance.
 */
function normalizeDate(input: Date | string | null | undefined): Date | null {
  if (!input) {
    return null;
  }
  if (input instanceof Date) {
    return isValid(input) ? input : null;
  }
  const parsed = parseIsoDate(input);
  return isValid(parsed) ? parsed : null;
}

/**
 * Formats a Date or ISO date string for display (e.g., "12 Nov 2026").
 */
export function formatDisplayDate(date: Date | string): string {
  const normalized = normalizeDate(date);
  if (!normalized) {
    return '';
  }
  return format(normalized, 'dd MMM yyyy');
}

/**
 * Formats a Date or ISO date string with time for display (e.g., "12 Nov 2026, 14:30").
 */
export function formatDisplayDateTime(date: Date | string): string {
  const normalized = normalizeDate(date);
  if (!normalized) {
    return '';
  }
  return format(normalized, 'dd MMM yyyy, HH:mm');
}

/**
 * Formats a Date or ISO string into a backend DateOnly string ("YYYY-MM-DD").
 */
export function formatDateOnly(date: Date | string): string {
  if (typeof date === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(date)) {
    return date;
  }
  const normalized = normalizeDate(date);
  if (!normalized) {
    return '';
  }
  return format(normalized, 'yyyy-MM-dd');
}

/**
 * Formats a Date or ISO string into a backend TimeOnly string ("HH:mm:ss").
 */
export function formatTimeOnly(date: Date | string): string {
  if (typeof date === 'string') {
    if (/^\d{2}:\d{2}:\d{2}$/.test(date)) {
      return date;
    }
    if (/^\d{2}:\d{2}$/.test(date)) {
      return `${date}:00`;
    }
  }
  const normalized = normalizeDate(date);
  if (!normalized) {
    return '';
  }
  return format(normalized, 'HH:mm:ss');
}
