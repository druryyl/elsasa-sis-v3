import { describe, it, expect } from 'vitest';
import {
  formatIdr,
  parseIdr,
  getPolicyStatusDescriptor,
  getPolicyStatusSeverity,
  getPolicyStatusLabel,
  getObligationStatusDescriptor,
  getObligationStatusSeverity,
  getObligationStatusLabel,
  getBillingStatusDescriptor,
  getBillingStatusSeverity,
  getBillingStatusLabel,
  getAccountStatusDescriptor,
  getAccountStatusSeverity,
  getAccountStatusLabel,
} from '../shared';

describe('Finance Shared Utilities', () => {
  describe('currency.utils.ts', () => {
    it('formats positive numbers as IDR with standard dot thousand separators', () => {
      expect(formatIdr(750000)).toBe('Rp 750.000');
      expect(formatIdr(15000000)).toBe('Rp 15.000.000');
      expect(formatIdr(500000)).toBe('Rp 500.000');
    });

    it('formats BigInt values accurately without decimal loss', () => {
      expect(formatIdr(BigInt(15000000))).toBe('Rp 15.000.000');
      expect(formatIdr(BigInt(0))).toBe('Rp 0');
    });

    it('formats numeric string inputs properly', () => {
      expect(formatIdr('750000')).toBe('Rp 750.000');
      expect(formatIdr('Rp 750.000')).toBe('Rp 750.000');
    });

    it('formats zero, null, undefined, and empty string as Rp 0', () => {
      expect(formatIdr(0)).toBe('Rp 0');
      expect(formatIdr(null)).toBe('Rp 0');
      expect(formatIdr(undefined)).toBe('Rp 0');
      expect(formatIdr('')).toBe('Rp 0');
      expect(formatIdr(Number.NaN)).toBe('Rp 0');
    });

    it('formats negative integer amounts', () => {
      expect(formatIdr(-50000)).toBe('-Rp 50.000');
    });

    it('parses formatted IDR string back to integer number', () => {
      expect(parseIdr('Rp 750.000')).toBe(750000);
      expect(parseIdr('15.000.000')).toBe(15000000);
      expect(parseIdr('-Rp 50.000')).toBe(-50000);
      expect(parseIdr('')).toBe(0);
      expect(parseIdr(null)).toBe(0);
    });
  });

  describe('financeStatus.utils.ts', () => {
    it('returns appropriate descriptors and severities for policy statuses', () => {
      expect(getPolicyStatusDescriptor('active')).toEqual({ label: 'Active', severity: 'success' });
      expect(getPolicyStatusDescriptor('draft')).toEqual({ label: 'Draft', severity: 'warn' });
      expect(getPolicyStatusDescriptor('superseded')).toEqual({ label: 'Superseded', severity: 'secondary' });
      expect(getPolicyStatusSeverity('active')).toBe('success');
      expect(getPolicyStatusLabel('active')).toBe('Active');
      expect(getPolicyStatusSeverity('unknown')).toBe('secondary');
    });

    it('returns appropriate descriptors and severities for obligation statuses', () => {
      expect(getObligationStatusDescriptor('assessed')).toEqual({ label: 'Assessed', severity: 'info' });
      expect(getObligationStatusDescriptor('issued')).toEqual({ label: 'Issued', severity: 'warn' });
      expect(getObligationStatusDescriptor('settled')).toEqual({ label: 'Settled', severity: 'success' });
      expect(getObligationStatusDescriptor('cancelled')).toEqual({ label: 'Cancelled', severity: 'danger' });
      expect(getObligationStatusSeverity('settled')).toBe('success');
      expect(getObligationStatusLabel('settled')).toBe('Settled');
    });

    it('returns appropriate descriptors and severities for billing document statuses', () => {
      expect(getBillingStatusDescriptor('issued')).toEqual({ label: 'Issued', severity: 'info' });
      expect(getBillingStatusDescriptor('sent')).toEqual({ label: 'Sent', severity: 'success' });
      expect(getBillingStatusDescriptor('paid')).toEqual({ label: 'Paid', severity: 'success' });
      expect(getBillingStatusDescriptor('overdue')).toEqual({ label: 'Overdue', severity: 'danger' });
      expect(getBillingStatusSeverity('paid')).toBe('success');
      expect(getBillingStatusLabel('paid')).toBe('Paid');
    });

    it('returns appropriate descriptors and severities for account statuses', () => {
      expect(getAccountStatusDescriptor('active')).toEqual({ label: 'Active', severity: 'success' });
      expect(getAccountStatusDescriptor('suspended')).toEqual({ label: 'Suspended', severity: 'warn' });
      expect(getAccountStatusDescriptor('closed')).toEqual({ label: 'Closed', severity: 'secondary' });
      expect(getAccountStatusSeverity('active')).toBe('success');
      expect(getAccountStatusLabel('active')).toBe('Active');
    });
  });
});
