// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import {
  apiClient,
  createHttpClient,
  setAuthTokenProvider,
  setSessionExpiredHandler,
  getActiveAuthToken,
  triggerSessionExpired,
  getDefaultTitleForStatus,
} from '../http';
import { storage } from '@/shared/services/storage';
import { ApiError } from '@/shared/types/common';

const UUID_V4_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

describe('HTTP Client & Interceptors (http.ts)', () => {
  beforeEach(() => {
    storage.clear();
    setAuthTokenProvider(null);
    setSessionExpiredHandler(null);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    storage.clear();
    setAuthTokenProvider(null);
    setSessionExpiredHandler(null);
  });

  describe('Client Defaults & Environment Configuration', () => {
    it('initializes with expected defaults', () => {
      expect(apiClient.defaults.timeout).toBe(15000);
      expect(apiClient.defaults.headers['Content-Type']).toBe('application/json');
      expect(apiClient.defaults.headers['Accept']).toBe('application/json');
    });

    it('createHttpClient allows custom baseURL and timeout', () => {
      const customClient = createHttpClient('https://custom-api.sch.id/api', 5000);
      expect(customClient.defaults.baseURL).toBe('https://custom-api.sch.id/api');
      expect(customClient.defaults.timeout).toBe(5000);
    });
  });

  describe('Request Interceptor: Header Injection', () => {
    it('injects X-Correlation-ID UUID v4 header when none provided', async () => {
      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { success: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/test');

      expect(capturedConfig).not.toBeNull();
      const correlationId = capturedConfig!.headers['X-Correlation-ID'];
      expect(typeof correlationId).toBe('string');
      expect(correlationId).toMatch(UUID_V4_REGEX);
    });

    it('preserves existing X-Correlation-ID header if caller already supplied one', async () => {
      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { success: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/test', {
        headers: { 'X-Correlation-ID': 'custom-correlation-1234' },
      });

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['X-Correlation-ID']).toBe('custom-correlation-1234');
    });

    it('does not inject Authorization header when no auth token is active', async () => {
      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { ok: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/public');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['Authorization']).toBeUndefined();
    });

    it('injects Authorization Bearer token from setAuthTokenProvider', async () => {
      setAuthTokenProvider(() => 'dynamic-jwt-token-999');
      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { ok: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/secure');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['Authorization']).toBe('Bearer dynamic-jwt-token-999');
    });

    it('injects Authorization Bearer token from storage session fallback', async () => {
      storage.setItem('session', { token: 'stored-session-token' });
      expect(getActiveAuthToken()).toBe('stored-session-token');

      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { ok: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/secure');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['Authorization']).toBe('Bearer stored-session-token');
    });

    it('injects Authorization Bearer token from storage token key fallback', async () => {
      storage.setItem('token', 'direct-token-key');
      expect(getActiveAuthToken()).toBe('direct-token-key');

      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { ok: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/secure');

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['Authorization']).toBe('Bearer direct-token-key');
    });

    it('preserves caller-supplied Authorization header without overwriting', async () => {
      setAuthTokenProvider(() => 'default-token');
      let capturedConfig: InternalAxiosRequestConfig | null = null;
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { ok: true },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await client.get('/custom', {
        headers: { Authorization: 'Basic dXNlcjpwYXNz' },
      });

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['Authorization']).toBe('Basic dXNlcjpwYXNz');
    });
  });

  describe('Response Interceptor: HTTP 2xx Data Unwrapping', () => {
    it('unwraps response.data on HTTP 200 OK', async () => {
      const client = createHttpClient('/api');
      const payload = { id: 'ay_1', name: '2026/2027' };

      client.defaults.adapter = async (config) => ({
        data: payload,
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      });

      const result = await client.get('/academic-years/ay_1');
      expect(result).toEqual(payload);
    });

    it('unwraps response.data on HTTP 201 Created', async () => {
      const client = createHttpClient('/api');
      const createdItem = { id: 'app_123', status: 'Pending' };

      client.defaults.adapter = async (config) => ({
        data: createdItem,
        status: 201,
        statusText: 'Created',
        headers: {},
        config,
      });

      const result = await client.post('/applications', { name: 'Student' });
      expect(result).toEqual(createdItem);
    });

    it('returns empty response on HTTP 204 No Content', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => ({
        data: undefined,
        status: 204,
        statusText: 'No Content',
        headers: {},
        config,
      });

      const result = await client.delete('/items/1');
      expect(result).toBeUndefined();
    });
  });

  describe('Response Interceptor: 401 Session Expiration', () => {
    it('intercepts 401 Unauthorized, triggers session expiration handler, and rejects with ApiError', async () => {
      const onExpired = vi.fn();
      setSessionExpiredHandler(onExpired);

      const eventListener = vi.fn();
      window.addEventListener('elsasa:auth:session-expired', eventListener);

      const client = createHttpClient('/api');
      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 401',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 401,
            statusText: 'Unauthorized',
            data: {
              title: 'Unauthorized',
              detail: 'JWT token has expired.',
              errorCode: 'Auth.TokenExpired',
            },
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      await expect(client.get('/protected')).rejects.toThrow(ApiError);

      try {
        await client.get('/protected');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(401);
        expect(apiError.title).toBe('Unauthorized');
        expect(apiError.detail).toBe('JWT token has expired.');
        expect(apiError.errorCode).toBe('Auth.TokenExpired');
      }

      expect(onExpired).toHaveBeenCalled();
      expect(eventListener).toHaveBeenCalled();

      window.removeEventListener('elsasa:auth:session-expired', eventListener);
    });

    it('provides sensible defaults on empty 401 response', async () => {
      const client = createHttpClient('/api');
      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 401',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 401,
            statusText: 'Unauthorized',
            data: null,
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.get('/protected');
        expect.unreachable('Should have rejected');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(401);
        expect(apiError.title).toBe('Unauthorized');
        expect(apiError.detail).toBe('Session has expired. Please log in again.');
      }
    });
  });

  describe('Response Interceptor: RFC 7807 ProblemDetails Mapping', () => {
    it('maps 422 Unprocessable Entity with validation errors', async () => {
      const client = createHttpClient('/api');
      const problemDetails = {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
        title: 'Validation Error',
        status: 422,
        detail: 'One or more validation errors occurred.',
        errorCode: 'Operations.Calendar.AcademicYear.InvalidDateRange',
        errors: {
          code: ['Code must be at least 3 characters.'],
          endDate: ['End date must be strictly after the start date.'],
        },
      };

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 422',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 422,
            statusText: 'Unprocessable Entity',
            data: problemDetails,
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.post('/academic-years', {});
        expect.unreachable('Should have thrown ApiError');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(422);
        expect(apiError.title).toBe('Validation Error');
        expect(apiError.detail).toBe('One or more validation errors occurred.');
        expect(apiError.errorCode).toBe('Operations.Calendar.AcademicYear.InvalidDateRange');
        expect(apiError.hasValidationErrors()).toBe(true);
        expect(apiError.errors).toEqual(problemDetails.errors);
        expect(apiError.getValidationMessages()).toEqual([
          'Code must be at least 3 characters.',
          'End date must be strictly after the start date.',
        ]);
      }
    });

    it('maps 409 Conflict with business rule error code', async () => {
      const client = createHttpClient('/api');
      const problemDetails = {
        title: 'Business Rule Conflict',
        status: 409,
        detail: 'An active academic year already spans this date range.',
        errorCode: 'Operations.Calendar.AcademicYear.DateCollision',
      };

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 409',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 409,
            statusText: 'Conflict',
            data: problemDetails,
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.post('/academic-years', {});
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(409);
        expect(apiError.title).toBe('Business Rule Conflict');
        expect(apiError.detail).toBe('An active academic year already spans this date range.');
        expect(apiError.errorCode).toBe('Operations.Calendar.AcademicYear.DateCollision');
        expect(apiError.hasValidationErrors()).toBe(false);
      }
    });

    it('maps 404 Resource Not Found with default title fallback', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 404',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 404,
            statusText: 'Not Found',
            data: { detail: 'Academic year with ID "123" was not found.' },
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.get('/academic-years/123');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(404);
        expect(apiError.title).toBe('Resource Not Found');
        expect(apiError.detail).toBe('Academic year with ID "123" was not found.');
      }
    });

    it('maps 403 Forbidden', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 403',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 403,
            statusText: 'Forbidden',
            data: {
              title: 'Forbidden',
              detail: 'You lack permission Permissions.Operations.Calendar.Manage.',
            },
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.delete('/academic-years/123');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(403);
        expect(apiError.title).toBe('Forbidden');
        expect(apiError.detail).toBe('You lack permission Permissions.Operations.Calendar.Manage.');
      }
    });

    it('maps 400 Bad Request', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 400',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 400,
            statusText: 'Bad Request',
            data: {
              detail: 'Invalid request body format.',
            },
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.post('/data', 'malformed');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(400);
        expect(apiError.title).toBe('Bad Request');
        expect(apiError.detail).toBe('Invalid request body format.');
      }
    });

    it('maps 500 Internal Server Error', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 500',
          'ERR_BAD_RESPONSE',
          config,
          null,
          {
            status: 500,
            statusText: 'Internal Server Error',
            data: {
              title: 'Internal Server Error',
              detail: 'An unhandled exception occurred.',
            },
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.get('/crash');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(500);
        expect(apiError.title).toBe('Internal Server Error');
      }
    });

    it('extracts errorCode and errors from extensions object when present', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 422',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 422,
            statusText: 'Unprocessable Entity',
            data: {
              title: 'Validation Error',
              detail: 'Validation failed',
              extensions: {
                errorCode: 'Common.Validation',
                errors: { field: ['Field is required'] },
              },
            },
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.post('/test', {});
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.errorCode).toBe('Common.Validation');
        expect(apiError.errors).toEqual({ field: ['Field is required'] });
      }
    });

    it('handles non-JSON string error responses gracefully', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError(
          'Request failed with status code 502',
          'ERR_BAD_RESPONSE',
          config,
          null,
          {
            status: 502,
            statusText: 'Bad Gateway',
            data: '<html><body>502 Bad Gateway</body></html>',
            headers: {},
            config,
          } as AxiosResponse,
        );
      };

      try {
        await client.get('/proxy-failure');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(502);
        expect(apiError.title).toBe('Bad Gateway');
        expect(apiError.detail).toBe('<html><body>502 Bad Gateway</body></html>');
      }
    });
  });

  describe('Response Interceptor: Offline & Network Drop (Status 0)', () => {
    it('handles network error when error.response is undefined', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError('Network Error', 'ERR_NETWORK', config, null);
      };

      try {
        await client.get('/offline');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(0);
        expect(apiError.title).toBe('Network Error');
        expect(apiError.detail).toBe(
          'Unable to reach the server. Please check your network connection.',
        );
      }
    });

    it('uses custom error message if error message differs from Network Error', async () => {
      const client = createHttpClient('/api');

      client.defaults.adapter = async (config) => {
        throw new AxiosError('Connection timed out', 'ETIMEDOUT', config, null);
      };

      try {
        await client.get('/timeout');
        expect.unreachable('Should have thrown');
      } catch (err) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;
        expect(apiError.statusCode).toBe(0);
        expect(apiError.title).toBe('Network Error');
        expect(apiError.detail).toBe('Connection timed out');
      }
    });
  });

  describe('Helper Functions', () => {
    it('getDefaultTitleForStatus maps all known statuses', () => {
      expect(getDefaultTitleForStatus(400)).toBe('Bad Request');
      expect(getDefaultTitleForStatus(401)).toBe('Unauthorized');
      expect(getDefaultTitleForStatus(403)).toBe('Forbidden');
      expect(getDefaultTitleForStatus(404)).toBe('Resource Not Found');
      expect(getDefaultTitleForStatus(409)).toBe('Business Rule Conflict');
      expect(getDefaultTitleForStatus(422)).toBe('Validation Error');
      expect(getDefaultTitleForStatus(500)).toBe('Internal Server Error');
      expect(getDefaultTitleForStatus(502)).toBe('Bad Gateway');
      expect(getDefaultTitleForStatus(503)).toBe('Service Unavailable');
      expect(getDefaultTitleForStatus(504)).toBe('Gateway Timeout');
      expect(getDefaultTitleForStatus(505)).toBe('Internal Server Error');
      expect(getDefaultTitleForStatus(418)).toBe('Request Error');
    });

    it('triggerSessionExpired catches and logs errors in callback safely', () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      setSessionExpiredHandler(() => {
        throw new Error('Boom');
      });

      expect(() => triggerSessionExpired()).not.toThrow();
      expect(consoleSpy).toHaveBeenCalled();
    });
  });
});
