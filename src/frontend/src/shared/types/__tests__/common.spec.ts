import { describe, it, expect } from 'vitest';
import { ApiError, Result } from '../common';

describe('common outcome types', () => {
  describe('ApiError', () => {
    it('instantiates with basic error information', () => {
      const error = new ApiError(
        404,
        'Not Found',
        'The resource was not found',
        'RESOURCE_NOT_FOUND',
      );
      expect(error.statusCode).toBe(404);
      expect(error.title).toBe('Not Found');
      expect(error.detail).toBe('The resource was not found');
      expect(error.errorCode).toBe('RESOURCE_NOT_FOUND');
      expect(error.name).toBe('ApiError');
      expect(error.message).toBe('The resource was not found');
      expect(error.hasValidationErrors()).toBe(false);
      expect(error.getValidationMessages()).toEqual([]);
    });

    it('handles validation errors correctly', () => {
      const validationErrors: Record<string, string[]> = {
        name: ['Name is required', 'Name must be at least 3 characters'],
        email: ['Invalid email address'],
      };

      const error = new ApiError(
        422,
        'Validation Failed',
        'One or more validation errors occurred',
        'VALIDATION_FAILED',
        validationErrors,
      );

      expect(error.hasValidationErrors()).toBe(true);
      expect(error.getValidationMessages()).toEqual([
        'Name is required',
        'Name must be at least 3 characters',
        'Invalid email address',
      ]);
    });

    it('falls back to title if detail is undefined', () => {
      const error = new ApiError(500, 'Server Error');
      expect(error.message).toBe('Server Error');
    });
  });

  describe('Result', () => {
    it('creates a success result', () => {
      const res = Result.success({ id: '123', name: 'John Doe' });
      expect(res.isSuccess).toBe(true);
      expect(res.isFailure).toBe(false);
      expect(res.value).toEqual({ id: '123', name: 'John Doe' });
      expect(res.error).toBeUndefined();
    });

    it('creates a failure result', () => {
      const apiError = new ApiError(400, 'Bad Request', 'Malformed request payload');
      const res = Result.failure(apiError);
      expect(res.isSuccess).toBe(false);
      expect(res.isFailure).toBe(true);
      expect(res.value).toBeUndefined();
      expect(res.error).toBe(apiError);
    });
  });
});
