import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import {
  getCalendars,
  getActiveCalendar,
  createCalendar,
  publishCalendar,
  withdrawCalendar,
  getMatrixProjection,
  createCalendarEntry,
  deleteCalendarEntry,
} from '../api/calendar.api';

describe('Calendar API Integration (calendar.api.ts)', () => {
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

  it('getCalendars calls GET /operations/academic-years/{academicYearId}/calendars', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: [{ id: 'cal-1', name: 'Master Calendar', publicationStatus: 'draft' }],
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await getCalendars('ay-1');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/calendars');
    expect(res).toHaveLength(1);
  });

  it('getActiveCalendar calls GET /operations/academic-years/{academicYearId}/calendars/active', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cal-1', publicationStatus: 'published' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await getActiveCalendar('ay-1');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/calendars/active');
    expect(res?.publicationStatus).toBe('published');
  });

  it('createCalendar calls POST /operations/academic-years/{academicYearId}/calendars', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cal-2', name: 'New Calendar', version: '1.0' },
        status: 201,
        statusText: 'Created',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await createCalendar('ay-1', { name: 'New Calendar' });
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/academic-years/ay-1/calendars');
    expect(res.version).toBe('1.0');
  });

  it('publishCalendar calls POST /operations/calendars/{id}/publish', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cal-2', publicationStatus: 'published' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await publishCalendar('cal-2');
    expect(capturedConfig?.url).toBe('/operations/calendars/cal-2/publish');
    expect(res.publicationStatus).toBe('published');
  });

  it('withdrawCalendar calls POST /operations/calendars/{id}/withdraw', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'cal-2', publicationStatus: 'withdrawn' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await withdrawCalendar('cal-2');
    expect(capturedConfig?.url).toBe('/operations/calendars/cal-2/withdraw');
    expect(res.publicationStatus).toBe('withdrawn');
  });

  it('getMatrixProjection calls GET /operations/calendars/{calendarId}/matrix', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: [{ date: '2026-08-01', dayOfWeek: 6, isInstructional: false, designation: 'weekend' }],
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await getMatrixProjection('cal-1', { startDate: '2026-08-01', endDate: '2026-08-31' });
    expect(capturedConfig?.url).toBe('/operations/calendars/cal-1/matrix');
    expect(capturedConfig?.params).toEqual({ startDate: '2026-08-01', endDate: '2026-08-31' });
    expect(res).toHaveLength(1);
  });

  it('createCalendarEntry calls POST /operations/calendars/{calendarId}/entries', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: { id: 'entry-1', name: 'Independence Day', designation: 'holiday' },
        status: 201,
        statusText: 'Created',
        headers: {},
        config,
      } as AxiosResponse;
    };

    const res = await createCalendarEntry('cal-1', {
      entryDate: '2026-08-17',
      designation: 'holiday',
      name: 'Independence Day',
      isInstructional: false,
    });
    expect(capturedConfig?.method?.toLowerCase()).toBe('post');
    expect(capturedConfig?.url).toBe('/operations/calendars/cal-1/entries');
    expect(res.id).toBe('entry-1');
  });

  it('deleteCalendarEntry calls DELETE /operations/calendars/{calendarId}/entries/{entryId}', async () => {
    apiClient.defaults.adapter = async (config) => {
      capturedConfig = config;
      return {
        data: null,
        status: 204,
        statusText: 'No Content',
        headers: {},
        config,
      } as AxiosResponse;
    };

    await deleteCalendarEntry('cal-1', 'entry-1');
    expect(capturedConfig?.method?.toLowerCase()).toBe('delete');
    expect(capturedConfig?.url).toBe('/operations/calendars/cal-1/entries/entry-1');
  });
});
