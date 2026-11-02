import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import {
  getAmendments,
  getCorrections,
  submitCorrection,
} from '../api/amendment.api';

describe('Amendment & Correction API Integration (amendment.api.ts)', () => {
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

  it('getAmendments calls GET /operations/calendars/{calendarId}/amendments', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: [{ id: 'amd-1', amendmentNumber: 'AMD-2026-001', fromVersion: '1.0', toVersion: '1.1' }],
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await getAmendments('cal-1');
    expect(capturedConfig?.url).toBe('/operations/calendars/cal-1/amendments');
    expect(res).toHaveLength(1);
    expect(res[0].amendmentNumber).toBe('AMD-2026-001');
  });

  it('getCorrections calls GET /operations/academic-years/{academicYearId}/corrections', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: [{ id: 'cor-1', fieldName: 'name', oldValue: 'AY 26/27', newValue: 'AY 2026/2027' }],
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await getCorrections('ay-1');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/corrections');
    expect(res).toHaveLength(1);
    expect(res[0].fieldName).toBe('name');
  });

  it('submitCorrection calls POST /operations/records/correct', async () => {
    const payload = {
      academicYearId: 'ay-1',
      targetTableName: 'academic_years',
      targetRecordId: 'ay-1',
      fieldName: 'name',
      newValue: 'Academic Year 2026/2027 Official',
      justificationReason: 'Official naming standardization decree.',
    };

    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cor-2', ...payload, authorizedAt: '2026-11-01T00:00:00Z' },
        status: 201,
        statusText: 'Created',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await submitCorrection(payload);
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/records/correct');
    expect(res.newValue).toBe('Academic Year 2026/2027 Official');
  });
});
