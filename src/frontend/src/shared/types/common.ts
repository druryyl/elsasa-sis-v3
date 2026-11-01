/**
 * Domain error categories aligned with backend ErrorType (ResultsExtensions.cs).
 */
export type DomainErrorType =
  'NotFound' | 'Conflict' | 'Validation' | 'Unauthorized' | 'Forbidden' | 'Failure';

/**
 * Standard RFC 7807 ProblemDetails payload representation emitted by the backend.
 */
export interface ProblemDetails {
  type?: string | undefined;
  title?: string | undefined;
  status?: number | undefined;
  detail?: string | undefined;
  instance?: string | undefined;
  errorCode?: string | undefined;
  errors?: Record<string, string[]> | undefined;
  extensions?: Record<string, unknown> | undefined;
  [key: string]: unknown;
}

/**
 * Application-level error class representing failed API responses.
 * Encapsulates RFC 7807 problem details and validation failure dictionaries.
 */
export class ApiError extends Error {
  readonly statusCode: number;
  readonly title: string;
  readonly detail?: string | undefined;
  readonly errorCode?: string | undefined;
  readonly errors?: Record<string, string[]> | undefined;

  /**
   * HTTP status code alias for statusCode.
   */
  get status(): number {
    return this.statusCode;
  }

  constructor(
    statusCode: number,
    title: string,
    detail?: string,
    errorCode?: string,
    errors?: Record<string, string[]>,
  ) {
    super(detail || title);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.title = title;
    this.detail = detail;
    this.errorCode = errorCode;
    this.errors = errors;

    // Restore prototype chain for ES5/ES6 inheritance
    Object.setPrototypeOf(this, new.target.prototype);
  }

  /**
   * Indicates whether this error contains field-level validation errors.
   */
  hasValidationErrors(): boolean {
    return Boolean(this.errors && Object.keys(this.errors).length > 0);
  }

  /**
   * Extracts a flattened array of error messages from the validation errors dictionary.
   */
  getValidationMessages(): string[] {
    if (!this.errors) {
      return [];
    }
    return Object.values(this.errors).flat();
  }
}

/**
 * Generic functional outcome wrapper representing either a successful value or an ApiError.
 */
export interface Result<T = void> {
  readonly isSuccess: boolean;
  readonly isFailure: boolean;
  readonly value?: T | undefined;
  readonly error?: ApiError | undefined;
}

/**
 * Helper factory functions for creating Result instances.
 */
export const Result = {
  success<T>(value: T): Result<T> {
    return {
      isSuccess: true,
      isFailure: false,
      value,
    };
  },

  failure<T = void>(error: ApiError): Result<T> {
    return {
      isSuccess: false,
      isFailure: true,
      error,
    };
  },
};

/**
 * Standard paginated query response envelope matching backend PaginatedList<T>.
 */
export interface PaginatedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
