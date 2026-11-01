import { describe, it, expect } from 'vitest';
import { formatIDR } from '../currencyUtils';

describe('currencyUtils', () => {
  describe('formatIDR', () => {
    it('formats a standard positive number into Indonesian Rupiah', () => {
      expect(formatIDR(1500000)).toBe('Rp 1.500.000');
    });

    it('formats zero correctly', () => {
      expect(formatIDR(0)).toBe('Rp 0');
    });

    it('formats large numbers with correct dot separators', () => {
      expect(formatIDR(25000000)).toBe('Rp 25.000.000');
    });

    it('formats negative numbers correctly', () => {
      expect(formatIDR(-50000)).toBe('-Rp 50.000');
    });

    it('handles NaN and non-number inputs gracefully', () => {
      expect(formatIDR(NaN)).toBe('Rp 0');
      // @ts-expect-error - testing invalid runtime input
      expect(formatIDR(null)).toBe('Rp 0');
      // @ts-expect-error - testing invalid runtime input
      expect(formatIDR(undefined)).toBe('Rp 0');
    });
  });
});
