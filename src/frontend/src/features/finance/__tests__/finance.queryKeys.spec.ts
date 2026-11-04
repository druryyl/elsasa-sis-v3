import { describe, it, expect } from 'vitest';
import { financeKeys } from '../shared/queryKeys';

describe('financeKeys query key factories', () => {
  it('generates expected cache keys for payers', () => {
    expect(financeKeys.payers.all).toEqual(['finance', 'payers']);
    expect(financeKeys.payers.lists()).toEqual(['finance', 'payers', 'list']);
    expect(financeKeys.payers.list({ searchTerm: 'Budi' })).toEqual([
      'finance',
      'payers',
      'list',
      { searchTerm: 'Budi' },
    ]);
    expect(financeKeys.payers.details()).toEqual(['finance', 'payers', 'detail']);
    expect(financeKeys.payers.detail('pyr-1')).toEqual(['finance', 'payers', 'detail', 'pyr-1']);
  });

  it('generates expected cache keys for payments', () => {
    expect(financeKeys.payments.all).toEqual(['finance', 'payments']);
    expect(financeKeys.payments.lists()).toEqual(['finance', 'payments', 'list']);
    expect(financeKeys.payments.list({ status: 'submitted' })).toEqual([
      'finance',
      'payments',
      'list',
      { status: 'submitted' },
    ]);
    expect(financeKeys.payments.details()).toEqual(['finance', 'payments', 'detail']);
    expect(financeKeys.payments.detail('pay-1')).toEqual(['finance', 'payments', 'detail', 'pay-1']);
    expect(financeKeys.payments.receipts()).toEqual(['finance', 'payments', 'receipt']);
    expect(financeKeys.payments.receipt('pay-1')).toEqual([
      'finance',
      'payments',
      'receipt',
      'pay-1',
    ]);
  });

  it('generates expected cache keys for allocations', () => {
    expect(financeKeys.allocations.all).toEqual(['finance', 'allocations']);
    expect(financeKeys.allocations.suggestions()).toEqual([
      'finance',
      'allocations',
      'suggest',
    ]);
    expect(
      financeKeys.allocations.suggestion({
        studentId: 'stu-1',
        paymentAmount: 5000000,
      }),
    ).toEqual([
      'finance',
      'allocations',
      'suggest',
      { studentId: 'stu-1', paymentAmount: 5000000 },
    ]);
  });

  it('generates expected cache keys for receivables', () => {
    expect(financeKeys.receivables.all).toEqual(['finance', 'receivables']);
    expect(financeKeys.receivables.byStudent('stu-1', true)).toEqual([
      'finance',
      'receivables',
      'stu-1',
      { openOnly: true },
    ]);
    expect(financeKeys.receivables.byStudent('stu-1')).toEqual([
      'finance',
      'receivables',
      'stu-1',
      { openOnly: undefined },
    ]);
  });
});
