import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  suggestAllocations,
  allocatePayment,
  reallocatePayment,
  getStudentReceivables,
} from '../allocations/api/allocations.api';

describe('allocations.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('suggestAllocations queries GET /finance/allocations/suggest with query parameters', async () => {
    const mockResponse = {
      studentId: 'stu-123',
      applicantId: null,
      paymentAmount: 5000000,
      totalSuggestedAllocated: 5000000,
      remainingUnallocatedAmount: 0,
      suggestions: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await suggestAllocations({
      studentId: 'stu-123',
      paymentAmount: 5000000,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/allocations/suggest', {
      params: {
        paymentAmount: 5000000,
        studentId: 'stu-123',
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('suggestAllocations queries GET /finance/allocations/suggest with applicantId', async () => {
    const mockResponse = {
      studentId: null,
      applicantId: 'app-456',
      paymentAmount: 15000000,
      totalSuggestedAllocated: 15000000,
      remainingUnallocatedAmount: 0,
      suggestions: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await suggestAllocations({
      applicantId: 'app-456',
      paymentAmount: 15000000,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/allocations/suggest', {
      params: {
        paymentAmount: 15000000,
        applicantId: 'app-456',
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('allocatePayment calls POST /finance/allocations with payload', async () => {
    const mockResponse = {
      paymentId: 'pay-123',
      paymentNumber: 'PAY-2026-000001',
      paymentAmount: 5000000,
      totalAllocatedInRequest: 5000000,
      cumulativeAllocatedAmount: 5000000,
      remainingUnallocatedAmount: 0,
      allocations: [],
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const payload = {
      paymentId: 'pay-123',
      allocations: [
        {
          obligationId: 'ob-1',
          amount: 5000000,
        },
      ],
    };
    const result = await allocatePayment(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/allocations', payload);
    expect(result).toEqual(mockResponse);
  });

  it('reallocatePayment calls POST /finance/allocations/reallocate with payload', async () => {
    const mockResponse = {
      paymentId: 'pay-123',
      paymentNumber: 'PAY-2026-000001',
      reversalReason: 'Mistakenly applied to wrong student obligation',
      supersededAllocationIds: ['alc-old'],
      reversedAllocationIds: [],
      paymentAmount: 5000000,
      activeAllocatedAmount: 5000000,
      remainingUnallocatedAmount: 0,
      replacementAllocations: [],
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const payload = {
      paymentId: 'pay-123',
      priorAllocationIds: ['alc-old'],
      replacementAllocations: [
        {
          obligationId: 'ob-2',
          amount: 5000000,
        },
      ],
      reversalReason: 'Mistakenly applied to wrong student obligation',
    };
    const result = await reallocatePayment(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/allocations/reallocate', payload);
    expect(result).toEqual(mockResponse);
  });

  it('getStudentReceivables queries GET /finance/accounts/{studentId}/receivables with encoded ID and params', async () => {
    const mockResponse = {
      studentId: 'stu-123',
      totalNetObligation: 10000000,
      totalPaid: 5000000,
      totalOutstanding: 5000000,
      totalOverdueCount: 1,
      receivables: [],
      openObligations: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await getStudentReceivables('stu-123', { openOnly: true });

    expect(getSpy).toHaveBeenCalledWith('/finance/accounts/stu-123/receivables', {
      params: { openOnly: true },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getStudentReceivables queries GET /finance/accounts/{studentId}/receivables without optional params', async () => {
    const mockResponse = {
      studentId: 'stu-123',
      totalNetObligation: 5000000,
      totalPaid: 0,
      totalOutstanding: 5000000,
      totalOverdueCount: 0,
      receivables: [],
      openObligations: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await getStudentReceivables('stu-123');

    expect(getSpy).toHaveBeenCalledWith('/finance/accounts/stu-123/receivables', {
      params: {},
    });
    expect(result).toEqual(mockResponse);
  });
});
