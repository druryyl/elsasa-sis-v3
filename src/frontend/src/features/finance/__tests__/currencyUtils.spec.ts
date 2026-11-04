import { describe, it, expect } from 'vitest';
import {
  formatIdr,
  parseIdr,
} from '../shared/currency.utils';

describe('currencyUtils.spec.ts', () => {
  describe('formatIdr', () => {
    it('formats standard positive numbers as IDR with standard dot thousand separators', () => {
      expect(formatIdr(750000)).toBe('Rp 750.000');
      expect(formatIdr(15000000)).toBe('Rp 15.000.000');
      expect(formatIdr(500000)).toBe('Rp 500.000');
      expect(formatIdr(1000)).toBe('Rp 1.000');
    });

    it('formats BigInt integer values accurately without precision loss', () => {
      expect(formatIdr(BigInt(15000000))).toBe('Rp 15.000.000');
      expect(formatIdr(BigInt(750000))).toBe('Rp 750.000');
      expect(formatIdr(BigInt(0))).toBe('Rp 0');
      expect(formatIdr(BigInt(1000000000))).toBe('Rp 1.000.000.000');
    });

    it('formats numeric string inputs properly', () => {
      expect(formatIdr('750000')).toBe('Rp 750.000');
      expect(formatIdr('15000000')).toBe('Rp 15.000.000');
      expect(formatIdr('Rp 750.000')).toBe('Rp 750.000');
      expect(formatIdr('Rp 15.000.000')).toBe('Rp 15.000.000');
    });

    it('formats zero, null, undefined, empty string, and NaN as Rp 0', () => {
      expect(formatIdr(0)).toBe('Rp 0');
      expect(formatIdr(null)).toBe('Rp 0');
      expect(formatIdr(undefined)).toBe('Rp 0');
      expect(formatIdr('')).toBe('Rp 0');
      expect(formatIdr(Number.NaN)).toBe('Rp 0');
    });

    it('formats negative integer amounts with leading minus sign', () => {
      expect(formatIdr(-50000)).toBe('-Rp 50.000');
      expect(formatIdr(-750000)).toBe('-Rp 750.000');
      expect(formatIdr(BigInt(-15000000))).toBe('-Rp 15.000.000');
    });

    it('rounds fractional floating point amounts to exact integer (zero decimals in IDR)', () => {
      expect(formatIdr(750000.4)).toBe('Rp 750.000');
      expect(formatIdr(750000.6)).toBe('Rp 750.001');
    });
  });

  describe('parseIdr', () => {
    it('parses formatted IDR string back to integer number', () => {
      expect(parseIdr('Rp 750.000')).toBe(750000);
      expect(parseIdr('15.000.000')).toBe(15000000);
      expect(parseIdr('Rp 500.000')).toBe(500000);
    });

    it('parses negative IDR strings back to negative integer number', () => {
      expect(parseIdr('-Rp 50.000')).toBe(-50000);
      expect(parseIdr('-750000')).toBe(-750000);
    });

    it('handles empty, null, undefined, and non-numeric strings returning 0', () => {
      expect(parseIdr('')).toBe(0);
      expect(parseIdr(null)).toBe(0);
      expect(parseIdr(undefined)).toBe(0);
      expect(parseIdr('abc')).toBe(0);
      expect(parseIdr('Rp ')).toBe(0);
    });
  });
});
