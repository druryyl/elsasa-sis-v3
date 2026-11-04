import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getStudentAccount,
  searchStudentAccounts,
} from '../accounts/api/accounts.api';

describe('accounts.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getStudentAccount calls GET /finance/accounts/{studentId} with encoded student ID', async () => {
    const mockSummary = {
      account: {
        id: 'acc-1',
        accountNumber: 'ACC-2026-0001',
        studentId: 'stu-123',
        accountStatus: 'active' as const,
      },
      balance: {
        totalAssessed: 1500000,
        totalSettled: 750000,
        totalAdjusted: 0,
        currentBalance: 750000,
        currencyCode: 'IDR',
      },
      openObligations: [],
      recentBillingDocuments: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockSummary as never);

    const result = await getStudentAccount('stu-123');

    expect(getSpy).toHaveBeenCalledWith('/finance/accounts/stu-123');
    expect(result).toEqual(mockSummary);
  });

  it('searchStudentAccounts queries GET /finance/accounts with sanitized params', async () => {
    const mockResponse = {
      items: [],
      pageNumber: 1,
      pageSize: 15,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await searchStudentAccounts({
      searchTerm: 'Ahmad',
      campus: 'all',
      grade: 'Grade 1',
      status: 'active',
      pageNumber: 1,
      pageSize: 15,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/accounts', {
      params: {
        searchTerm: 'Ahmad',
        grade: 'Grade 1',
        status: 'active',
        pageNumber: 1,
        pageSize: 15,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('searchStudentAccounts applies default pageNumber and pageSize when omitted', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ items: [] } as never);

    await searchStudentAccounts();

    expect(getSpy).toHaveBeenCalledWith('/finance/accounts', {
      params: {
        pageNumber: 1,
        pageSize: 20,
      },
    });
  });
});
