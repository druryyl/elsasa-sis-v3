import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import {
  getPeriods,
  createPeriod,
  updatePeriod,
  closePeriod,
  activatePeriod,
} from '../api/operationalPeriod.api';

describe('Operational Period API Integration (operationalPeriod.api.ts)', () => {
  let capturedConfig: InternalAxiosRequestConfig | null = null;
  const originalAdapter = apiClient.defaults.adapter;

  beforeEach(() => {
    capturedConfig = null;
  });

  afterEach(() => {
    if (originalAdapter) {
      apiClient.defaults.adapter = originalAdapter;
    } else {
      delete apiClient.defaults.adapter;
    }
    vi.restoreAllMocks();
  });

  it('getPeriods calls GET /operations/academic-years/{academicYearId}/periods', async () => {
    const mockList = [
      {
        id: 'p-1',
        academicYearId: 'ay-1',
        periodCode: 'SEM-1',
        name: 'Semester 1',
        periodType: 'semester',
        startDate: '2026-07-15',
        endDate: '2026-12-20',
        parentPeriodId: null,
        campusContext: 'All',
        status: 'active',
      },
    ];

    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: mockList,
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const result = await getPeriods('ay-1');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/periods');
    expect(result).toHaveLength(1);
    expect(result[0].periodCode).toBe('SEM-1');
  });

  it('createPeriod calls POST /operations/academic-years/{academicYearId}/periods', async () => {
    const payload = {
      periodCode: 'SEM-2',
      name: 'Semester 2',
      periodType: 'semester',
      startDate: '2027-01-05',
      endDate: '2027-06-25',
    };

    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'p-2', ...payload, status: 'draft' },
        status: 201,
        statusText: 'Created',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const result = await createPeriod('ay-1', payload);
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/periods');
    expect(result.id).toBe('p-2');
  });

  it('updatePeriod calls PUT /operations/periods/{id}', async () => {
    const payload = {
      name: 'Semester 2 Revised',
      startDate: '2027-01-10',
      endDate: '2027-06-30',
    };

    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'p-2', ...payload },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const result = await updatePeriod('p-2', payload);
    expect(capturedConfig?.method?.toLowerCase()).toBe('put');
    expect(capturedConfig?.url).toBe('/operations/periods/p-2');
    expect(result.name).toBe('Semester 2 Revised');
  });

  it('closePeriod calls POST /operations/periods/{id}/close', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'p-2', status: 'closed' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const result = await closePeriod('p-2');
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/periods/p-2/close');
    expect(result.status).toBe('closed');
  });

  it('activatePeriod calls POST /operations/periods/{id}/activate', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'p-2', periodCode: 'SEM-1', status: 'active' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const result = await activatePeriod('p-2');
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/periods/p-2/activate');
    expect(result.status).toBe('active');
    expect(result.periodCode).toBe('SEM-1');
  });
});
