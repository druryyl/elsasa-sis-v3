import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import {
  getClosures,
  declareClosure,
  liftClosure,
} from '../api/closure.api';

describe('Closure API Integration (closure.api.ts)', () => {
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

  it('getClosures calls GET /operations/academic-years/{academicYearId}/closures', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: [{ id: 'cls-1', closureNumber: 'CLS-2026-001', status: 'authorized' }],
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await getClosures('ay-1');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/closures');
    expect(res).toHaveLength(1);
    expect(res[0].closureNumber).toBe('CLS-2026-001');
  });

  it('declareClosure calls POST /operations/closures', async () => {
    const payload = {
      academicYearId: 'ay-1',
      reasonCategory: 'weather_disaster',
      reasonDetails: 'Typhoon warning issued by meteorology agency.',
      startDate: '2026-10-01',
      endDate: '2026-10-02',
    };

    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cls-2', closureNumber: 'CLS-2026-002', status: 'authorized', ...payload },
        status: 201,
        statusText: 'Created',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await declareClosure(payload);
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/closures');
    expect(res.closureNumber).toBe('CLS-2026-002');
  });

  it('liftClosure calls POST /operations/closures/{id}/lift', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cls-2', status: 'lifted' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await liftClosure('cls-2');
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/closures/cls-2/lift');
    expect(res.status).toBe('lifted');
  });
});
