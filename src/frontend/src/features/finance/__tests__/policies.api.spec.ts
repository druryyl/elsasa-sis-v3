import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getPolicies,
  getPolicyById,
  createPolicy,
  approvePolicy,
  createChargeRule,
} from '../policies/api/policies.api';

describe('policies.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getPolicies passes sanitized parameters to GET /finance/policies', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([{ id: 'p-1' }] as never);

    const result = await getPolicies({ status: 'active', category: 'all' });

    expect(getSpy).toHaveBeenCalledWith('/finance/policies', {
      params: { status: 'active' },
    });
    expect(result).toEqual([{ id: 'p-1' }]);
  });

  it('getPolicies handles empty or undefined filters cleanly', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([] as never);

    const result = await getPolicies();

    expect(getSpy).toHaveBeenCalledWith('/finance/policies', {
      params: {},
    });
    expect(result).toEqual([]);
  });

  it('getPolicyById encodes policy ID parameter', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ id: 'p-123' } as never);

    const result = await getPolicyById('p-123');

    expect(getSpy).toHaveBeenCalledWith('/finance/policies/p-123');
    expect(result).toEqual({ id: 'p-123' });
  });

  it('createPolicy posts request to /finance/policies', async () => {
    const payload = {
      policyCode: 'FP-2026-001',
      name: 'SPP Policy',
      category: 'tuition' as const,
      businessPurpose: 'Purpose',
      effectiveDate: '2026-07-01',
      version: '1.0',
    };
    const mockCreated = { id: 'p-new', ...payload };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

    const result = await createPolicy(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/policies', payload);
    expect(result).toEqual(mockCreated);
  });

  it('approvePolicy posts to /finance/policies/{id}/approve with request payload', async () => {
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: 'p-1', status: 'active' } as never);

    const result = await approvePolicy('p-1', { approvingAuthority: 'Director', notes: 'Approved' });

    expect(postSpy).toHaveBeenCalledWith('/finance/policies/p-1/approve', {
      approvingAuthority: 'Director',
      notes: 'Approved',
    });
    expect(result).toEqual({ id: 'p-1', status: 'active' });
  });

  it('approvePolicy posts empty object if request is omitted', async () => {
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({ id: 'p-1', status: 'active' } as never);

    const result = await approvePolicy('p-1');

    expect(postSpy).toHaveBeenCalledWith('/finance/policies/p-1/approve', {});
    expect(result).toEqual({ id: 'p-1', status: 'active' });
  });

  it('createChargeRule posts to /finance/policies/{id}/rules', async () => {
    const payload = {
      ruleCode: 'CR-SPP',
      name: 'SPP',
      chargeType: 'tuition' as const,
      baseAmount: 750000,
      currencyCode: 'IDR',
      recurrencePattern: 'monthly' as const,
      dueRuleType: 'fixed_day_of_month' as const,
      dueRuleValue: '10',
    };
    const mockCreated = { id: 'cr-1', ...payload };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

    const result = await createChargeRule('p-1', payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/policies/p-1/rules', payload);
    expect(result).toEqual(mockCreated);
  });
});
