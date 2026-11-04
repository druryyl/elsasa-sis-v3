import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getPayments,
  getPaymentById,
  getPaymentReceipt,
  submitPayment,
  recordAndConfirmPayment,
  confirmPayment,
  rejectPayment,
} from '../payments/api/payments.api';

describe('payments.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getPayments queries GET /finance/payments with sanitized params', async () => {
    const mockResponse = {
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await getPayments({
      status: 'submitted',
      searchTerm: 'PAY-2026',
      page: 1,
      pageSize: 20,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/payments', {
      params: {
        status: 'submitted',
        searchTerm: 'PAY-2026',
        page: 1,
        pageSize: 20,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getPayments handles empty parameters and omits status all', async () => {
    const mockResponse = {
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await getPayments({ status: 'all' });

    expect(getSpy).toHaveBeenCalledWith('/finance/payments', {
      params: {
        page: 1,
        pageSize: 20,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getPaymentById queries GET /finance/payments/{id} with encoded ID', async () => {
    const mockPayment = {
      id: 'pay-123',
      paymentNumber: 'PAY-2026-000001',
      paymentAmount: 5000000,
      confirmationStatus: 'confirmed',
      allocations: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPayment as never);

    const result = await getPaymentById('pay-123');

    expect(getSpy).toHaveBeenCalledWith('/finance/payments/pay-123');
    expect(result).toEqual(mockPayment);
  });

  it('getPaymentReceipt queries GET /finance/payments/{id}/receipt with encoded ID', async () => {
    const mockReceipt = {
      paymentId: 'pay-123',
      receiptNumber: 'REC-2026-000001',
      amount: 5000000,
      amountInWords: 'Lima Juta Rupiah',
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockReceipt as never);

    const result = await getPaymentReceipt('pay-123');

    expect(getSpy).toHaveBeenCalledWith('/finance/payments/pay-123/receipt');
    expect(result).toEqual(mockReceipt);
  });

  it('submitPayment calls POST /finance/payments/submit with payload', async () => {
    const mockResponse = {
      paymentId: 'pay-123',
      paymentNumber: 'PAY-2026-000001',
      confirmationStatus: 'submitted',
      amount: 2500000,
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const payload = {
      studentId: 'stu-111',
      amount: 2500000,
      paymentMethod: 'bank_transfer',
      payerName: 'Budi Santoso',
    };
    const result = await submitPayment(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/payments/submit', payload);
    expect(result).toEqual(mockResponse);
  });

  it('recordAndConfirmPayment calls POST /finance/payments with payload', async () => {
    const mockResponse = {
      paymentId: 'pay-456',
      paymentNumber: 'PAY-2026-000002',
      receiptNumber: 'REC-2026-000002',
      confirmationStatus: 'confirmed',
      amount: 1500000,
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const payload = {
      studentId: 'stu-222',
      amount: 1500000,
      paymentMethod: 'cash',
      payerName: 'Siti Aminah',
    };
    const result = await recordAndConfirmPayment(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/payments', payload);
    expect(result).toEqual(mockResponse);
  });

  it('confirmPayment calls POST /finance/payments/{id}/confirm', async () => {
    const mockResponse = {
      paymentId: 'pay-123',
      paymentNumber: 'PAY-2026-000001',
      receiptNumber: 'REC-2026-000001',
      confirmationStatus: 'confirmed',
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const result = await confirmPayment('pay-123');

    expect(postSpy).toHaveBeenCalledWith('/finance/payments/pay-123/confirm');
    expect(result).toEqual(mockResponse);
  });

  it('rejectPayment calls POST /finance/payments/{id}/reject with reason payload', async () => {
    const mockResponse = {
      paymentId: 'pay-123',
      paymentNumber: 'PAY-2026-000001',
      confirmationStatus: 'rejected',
      rejectionReason: 'Slip transfer buram tidak terbaca',
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const payload = {
      rejectionReason: 'Slip transfer buram tidak terbaca',
    };
    const result = await rejectPayment('pay-123', payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/payments/pay-123/reject', payload);
    expect(result).toEqual(mockResponse);
  });
});
