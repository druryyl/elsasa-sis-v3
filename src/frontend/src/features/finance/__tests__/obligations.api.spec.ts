import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getObligations,
  batchAssessObligations,
  assessAdHocObligation,
} from '../obligations/api/obligations.api';

describe('obligations.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getObligations passes query filters to GET /finance/obligations with defaults', async () => {
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

    const result = await getObligations({
      studentId: 'stu-1',
      status: 'assessed',
      chargeType: 'all',
      page: 1,
      pageSize: 10,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/obligations', {
      params: {
        studentId: 'stu-1',
        status: 'assessed',
        pageNumber: 1,
        pageSize: 10,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getObligations uses fallback defaults when params are undefined', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ items: [] } as never);

    await getObligations();

    expect(getSpy).toHaveBeenCalledWith('/finance/obligations', {
      params: {
        pageNumber: 1,
        pageSize: 20,
      },
    });
  });

  it('batchAssessObligations posts preview mode request to /finance/obligations/batch-assess', async () => {
    const payload = {
      chargeRuleId: 'cr-1',
      academicPeriodId: 'ap-1',
      billingCycleLabel: 'Cycle 1',
      mode: 'preview' as const,
    };
    const mockPreviewResponse = {
      totalCandidatesCount: 150,
      assessedCount: 140,
      duplicateCount: 10,
      totalGrossAmount: 112500000,
      candidates: [],
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockPreviewResponse as never);

    const result = await batchAssessObligations(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/obligations/batch-assess', payload);
    expect(result).toEqual(mockPreviewResponse);
  });

  it('batchAssessObligations posts commit mode request to /finance/obligations/batch-assess', async () => {
    const payload = {
      chargeRuleId: 'cr-1',
      academicPeriodId: 'ap-1',
      billingCycleLabel: 'Cycle 1',
      mode: 'commit' as const,
    };
    const mockCommitResponse = {
      assessedCount: 140,
      skippedCount: 10,
      totalGrossAmount: 105000000,
      billingCycleLabel: 'Cycle 1',
      assessedObligationIds: ['ob-1', 'ob-2'],
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCommitResponse as never);

    const result = await batchAssessObligations(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/obligations/batch-assess', payload);
    expect(result).toEqual(mockCommitResponse);
  });

  it('assessAdHocObligation posts to /finance/obligations', async () => {
    const payload = {
      studentId: 'stu-1',
      chargeType: 'fine',
      description: 'Late fee',
      amount: 50000,
      dueDate: '2026-08-01',
      academicPeriodId: 'ap-1',
    };
    const mockCreated = {
      id: 'obl-1',
      obligationNumber: 'OBL-2026-0001',
      ...payload,
      status: 'assessed' as const,
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

    const result = await assessAdHocObligation(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/obligations', payload);
    expect(result).toEqual(mockCreated);
  });
});
