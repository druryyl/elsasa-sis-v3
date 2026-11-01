// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/shared/services/http';
import { ApiError } from '@/shared/types/common';
import {
  checkBackendHealth,
  createAcademicYear,
  type CreateAcademicYearPayload,
  type AcademicYearDto,
} from '../api/academicYear.api';

const UUID_V4_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

describe('Academic Year API Integration (academicYear.api.ts)', () => {
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

  describe('checkBackendHealth', () => {
    it('calls GET /health and unboxes string response into status object', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: 'Healthy',
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await checkBackendHealth();

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.url).toBe('/health');
      expect(capturedConfig!.method?.toLowerCase()).toBe('get');
      expect(result).toEqual({ status: 'Healthy' });
    });

    it('unboxes object payload with status property', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: { status: 'Healthy', duration: '00:00:00.005' },
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      const result = await checkBackendHealth();
      expect(result).toEqual({ status: 'Healthy' });
    });

    it('injects X-Correlation-ID tracing header when checking health', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: 'Healthy',
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await checkBackendHealth();

      expect(capturedConfig).not.toBeNull();
      const correlationId = capturedConfig!.headers['X-Correlation-ID'];
      expect(typeof correlationId).toBe('string');
      expect(correlationId).toMatch(UUID_V4_REGEX);
    });
  });

  describe('createAcademicYear', () => {
    const validPayload: CreateAcademicYearPayload = {
      code: 'AY-2026-2027',
      name: 'Academic Year 2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
    };

    const mockResponseDto: AcademicYearDto = {
      id: 'e107df43-162e-4b47-b50a-e8d1a1005fb8',
      code: 'AY-2026-2027',
      name: 'Academic Year 2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      isActive: false,
    };

    it('successfully transmits payload, propagates tracing header, and unboxes 201 Created response', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockResponseDto,
          status: 201,
          statusText: 'Created',
          headers: {
            Location: `/api/operations/academic-years/${mockResponseDto.id}`,
          },
          config,
        } as AxiosResponse;
      };

      const created = await createAcademicYear(validPayload);

      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.url).toBe('/operations/academic-years');
      expect(capturedConfig!.method?.toLowerCase()).toBe('post');
      expect(JSON.parse(capturedConfig!.data)).toEqual(validPayload);

      // Verifies correlation ID header propagation
      const correlationId = capturedConfig!.headers['X-Correlation-ID'];
      expect(typeof correlationId).toBe('string');
      expect(correlationId).toMatch(UUID_V4_REGEX);

      // Verifies unboxed DTO
      expect(created).toEqual(mockResponseDto);
      expect(created.id).toBe('e107df43-162e-4b47-b50a-e8d1a1005fb8');
      expect(created.code).toBe('AY-2026-2027');
      expect(created.isActive).toBe(false);
    });

    it('catches and transforms RFC 7807 422 ProblemDetails into typed ApiError with field error unbundling', async () => {
      const problemDetails = {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
        title: 'Validation Error',
        status: 422,
        detail: 'One or more validation errors occurred.',
        errorCode: 'Operations.Calendar.AcademicYear.InvalidDateRange',
        errors: {
          code: ['Code cannot exceed 32 characters.'],
          endDate: ['End date must be greater than start date.'],
        },
      };

      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
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
        await createAcademicYear({
          code: 'TOO_LONG_CODE_THAT_EXCEEDS_32_CHARACTERS_FOR_TEST',
          name: 'Academic Year Invalid',
          startDate: '2027-01-01',
          endDate: '2026-01-01',
        });
        expect.unreachable('Should have thrown ApiError');
      } catch (err: unknown) {
        expect(err).toBeInstanceOf(ApiError);
        const apiError = err as ApiError;

        expect(apiError.statusCode).toBe(422);
        expect(apiError.status).toBe(422);
        expect(apiError.title).toBe('Validation Error');
        expect(apiError.detail).toBe('One or more validation errors occurred.');
        expect(apiError.errorCode).toBe('Operations.Calendar.AcademicYear.InvalidDateRange');
        expect(apiError.hasValidationErrors()).toBe(true);

        // Field error unbundling
        expect(apiError.errors).toEqual(problemDetails.errors);
        expect(apiError.errors?.code).toContain('Code cannot exceed 32 characters.');
        expect(apiError.errors?.endDate).toContain('End date must be greater than start date.');

        const flatMessages = apiError.getValidationMessages();
        expect(flatMessages).toContain('Code cannot exceed 32 characters.');
        expect(flatMessages).toContain('End date must be greater than start date.');
      }
    });

    it('propagates custom X-Correlation-ID if already attached to request', async () => {
      apiClient.defaults.adapter = async (config) => {
        capturedConfig = config;
        return {
          data: mockResponseDto,
          status: 201,
          statusText: 'Created',
          headers: {},
          config,
        } as AxiosResponse;
      };

      await createAcademicYear(validPayload);
      expect(capturedConfig).not.toBeNull();
      expect(capturedConfig!.headers['X-Correlation-ID']).toMatch(UUID_V4_REGEX);
    });
  });
});
