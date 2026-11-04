import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getBillingDocuments,
  getBillingDocumentById,
  generateBill,
  generateStatement,
} from '../billing/api/billing.api';

describe('billing.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getBillingDocuments queries GET /finance/billing-documents with clean filters', async () => {
    const mockResponse = {
      items: [],
      pageNumber: 2,
      pageSize: 25,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: true,
      hasNextPage: false,
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

    const result = await getBillingDocuments({
      studentId: 'stu-1',
      documentType: 'bill',
      deliveryStatus: 'all',
      page: 2,
      pageSize: 25,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/billing-documents', {
      params: {
        studentId: 'stu-1',
        documentType: 'bill',
        page: 2,
        pageSize: 25,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getBillingDocuments handles undefined parameters gracefully', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ items: [] } as never);

    await getBillingDocuments();

    expect(getSpy).toHaveBeenCalledWith('/finance/billing-documents', {
      params: {
        page: 1,
        pageSize: 20,
      },
    });
  });

  it('getBillingDocumentById calls GET /finance/billing-documents/{id} with encoded ID', async () => {
    const mockDoc = {
      id: 'bil-1',
      documentNumber: 'BIL-2026-0001',
      documentType: 'bill',
      items: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockDoc as never);

    const result = await getBillingDocumentById('bil-1');

    expect(getSpy).toHaveBeenCalledWith('/finance/billing-documents/bil-1');
    expect(result).toEqual(mockDoc);
  });

  it('generateBill posts to /finance/billing-documents/generate', async () => {
    const payload = {
      studentId: 'stu-1',
      obligationIds: ['ob-1', 'ob-2'],
      paymentDueDate: '2026-08-15',
    };
    const mockResponse = {
      id: 'bil-new',
      documentNumber: 'BIL-2026-0002',
      totalBilledAmount: 1500000,
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const result = await generateBill(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/billing-documents/generate', payload);
    expect(result).toEqual(mockResponse);
  });

  it('generateStatement posts to /finance/billing-documents/statement', async () => {
    const payload = {
      studentId: 'stu-1',
      billingPeriodStart: '2026-07-01',
      billingPeriodEnd: '2026-07-31',
    };
    const mockResponse = {
      id: 'stm-new',
      documentNumber: 'STM-2026-0001',
      totalBilledAmount: 750000,
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

    const result = await generateStatement(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/billing-documents/statement', payload);
    expect(result).toEqual(mockResponse);
  });
});
