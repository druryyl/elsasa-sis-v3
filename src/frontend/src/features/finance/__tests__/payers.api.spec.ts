import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getPayers,
  getPayerById,
  createPayer,
} from '../payers/api/payers.api';

describe('payers.api.spec.ts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getPayers queries GET /finance/payers with sanitized params', async () => {
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

    const result = await getPayers({
      searchTerm: 'Pertamina',
      payerType: 'sponsor',
      page: 1,
      pageSize: 20,
    });

    expect(getSpy).toHaveBeenCalledWith('/finance/payers', {
      params: {
        searchTerm: 'Pertamina',
        payerType: 'sponsor',
        page: 1,
        pageSize: 20,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getPayers queries GET /finance/payers with default pagination when no params provided', async () => {
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

    const result = await getPayers();

    expect(getSpy).toHaveBeenCalledWith('/finance/payers', {
      params: {
        page: 1,
        pageSize: 20,
      },
    });
    expect(result).toEqual(mockResponse);
  });

  it('getPayerById queries GET /finance/payers/{id} with encoded ID', async () => {
    const mockPayer = {
      id: 'pyr-123',
      payerNumber: 'PYR-2026-0001',
      payerName: 'PT Foundation Indonesia',
      payerType: 'sponsor',
      totalPaymentsCount: 2,
      totalPaidAmount: 50000000,
      payments: [],
    };
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPayer as never);

    const result = await getPayerById('pyr-123');

    expect(getSpy).toHaveBeenCalledWith('/finance/payers/pyr-123');
    expect(result).toEqual(mockPayer);
  });

  it('createPayer calls POST /finance/payers with payload', async () => {
    const mockCreated = {
      id: 'pyr-456',
      payerNumber: 'PYR-2026-0002',
      payerName: 'Kedutaan Besar',
      payerType: 'embassy',
    };
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockCreated as never);

    const payload = {
      name: 'Kedutaan Besar',
      type: 'embassy',
      identificationNumber: '123456789',
      email: 'finance@embassy.org',
    };
    const result = await createPayer(payload);

    expect(postSpy).toHaveBeenCalledWith('/finance/payers', payload);
    expect(result).toEqual(mockCreated);
  });
});
