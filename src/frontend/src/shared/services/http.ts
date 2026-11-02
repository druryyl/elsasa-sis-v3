import axios, {
  type AxiosError,
  type AxiosInstance,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import { v4 as uuidv4 } from 'uuid';
import { storage } from '@/shared/services/storage';
import { ApiError, type ProblemDetails } from '@/shared/types/common';

export type AuthTokenProvider = () => string | null | undefined;
export type SessionExpiredHandler = () => void;

let authTokenProvider: AuthTokenProvider | null = null;
let sessionExpiredHandler: SessionExpiredHandler | null = null;

/**
 * Registers an auth token provider function (e.g. from Pinia auth store).
 */
export function setAuthTokenProvider(provider: AuthTokenProvider | null): void {
  authTokenProvider = provider;
}

/**
 * Registers a session expired callback invoked on HTTP 401 Unauthorized responses.
 */
export function setSessionExpiredHandler(handler: SessionExpiredHandler | null): void {
  sessionExpiredHandler = handler;
}

/**
 * Resolves the active bearer token from either the registered token provider
 * or persistent namespaced storage.
 */
export function getActiveAuthToken(): string | null {
  if (authTokenProvider) {
    const providerToken = authTokenProvider();
    if (providerToken) {
      return providerToken;
    }
  }

  // Fallback to local storage (elsasa:v3:session or elsasa:v3:token / auth_token)
  const session = storage.getItem<{ token?: string }>('session');
  if (session?.token) {
    return session.token;
  }

  const directToken = storage.getItem<string>('token') || storage.getItem<string>('auth_token');
  if (directToken) {
    return directToken;
  }

  return null;
}

/**
 * Triggers session expiration side-effects: executes registered handler
 * and dispatches a window event for decoupled listeners.
 */
export function triggerSessionExpired(): void {
  if (sessionExpiredHandler) {
    try {
      sessionExpiredHandler();
    } catch (error) {
      console.error('[http] Session expired handler encountered an error:', error);
    }
  }

  if (typeof window !== 'undefined' && typeof window.dispatchEvent === 'function') {
    window.dispatchEvent(new CustomEvent('elsasa:auth:session-expired'));
  }
}

/**
 * Returns standard RFC 7807 fallback title for known HTTP status codes.
 */
export function getDefaultTitleForStatus(status: number): string {
  switch (status) {
    case 400:
      return 'Bad Request';
    case 401:
      return 'Unauthorized';
    case 403:
      return 'Forbidden';
    case 404:
      return 'Resource Not Found';
    case 409:
      return 'Business Rule Conflict';
    case 422:
      return 'Validation Error';
    case 500:
      return 'Internal Server Error';
    case 502:
      return 'Bad Gateway';
    case 503:
      return 'Service Unavailable';
    case 504:
      return 'Gateway Timeout';
    default:
      return status >= 500 ? 'Internal Server Error' : 'Request Error';
  }
}

/**
 * Configures request and response interceptors on an AxiosInstance.
 */
export function setupInterceptors(instance: AxiosInstance): void {
  // 1. Request Interceptor: Attach JWT and Tracing Header
  instance.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      // Attach JWT Bearer token if token exists and not already provided
      const token = getActiveAuthToken();
      if (token && config.headers) {
        const existingAuth =
          config.headers.get?.('Authorization') ??
          config.headers['Authorization'] ??
          config.headers['authorization'];

        if (!existingAuth) {
          if (typeof config.headers.set === 'function') {
            config.headers.set('Authorization', `Bearer ${token}`);
          } else {
            config.headers['Authorization'] = `Bearer ${token}`;
          }
        }
      }

      // Attach X-Correlation-ID tracing header matching backend CorrelationIdMiddleware
      if (config.headers) {
        const existingCorrelationId =
          config.headers.get?.('X-Correlation-ID') ??
          config.headers['X-Correlation-ID'] ??
          config.headers['x-correlation-id'];

        if (!existingCorrelationId) {
          const correlationId = uuidv4();
          if (typeof config.headers.set === 'function') {
            config.headers.set('X-Correlation-ID', correlationId);
          } else {
            config.headers['X-Correlation-ID'] = correlationId;
          }
        }
      }

      return config;
    },
    (error: unknown) => Promise.reject(error),
  );

  // 2. Response Interceptor: Unwrap 2xx and map RFC 7807 ProblemDetails into ApiError
  instance.interceptors.response.use(
    (response: AxiosResponse) => {
      // Unwraps response.data on HTTP 2xx
      return response.data;
    },
    (error: AxiosError<unknown>) => {
      if (!error.response) {
        // Offline / network drop (status 0)
        const detail =
          error.message && error.message !== 'Network Error'
            ? error.message
            : 'Unable to reach the server. Please check your network connection.';
        return Promise.reject(new ApiError(0, 'Network Error', detail));
      }

      const { status, data } = error.response;

      const problemData =
        typeof data === 'object' && data !== null ? (data as ProblemDetails) : undefined;

      const title =
        problemData?.title || getDefaultTitleForStatus(status) || 'An unexpected error occurred';

      const detail =
        problemData?.detail || (typeof data === 'string' && data ? data : error.message);

      const errorCode =
        problemData?.errorCode || (problemData?.extensions?.errorCode as string | undefined);

      const errors =
        problemData?.errors ||
        (problemData?.extensions?.errors as Record<string, string[]> | undefined);

      // Handle 401 Unauthorized globally
      if (status === 401) {
        triggerSessionExpired();
        return Promise.reject(
          new ApiError(
            401,
            title,
            problemData?.detail || 'Session has expired. Please log in again.',
            errorCode,
            errors,
          ),
        );
      }

      // Map RFC 7807 ProblemDetails payload
      const apiError = new ApiError(status, title, detail, errorCode, errors);
      return Promise.reject(apiError);
    },
  );
}

/**
 * Creates and configures a new Axios client instance.
 */
export function createHttpClient(
  baseURL: string = import.meta.env?.VITE_API_BASE_URL || 'https://localhost:52390/api',
  timeout = 15000,
): AxiosInstance {
  const instance = axios.create({
    baseURL,
    timeout,
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
  });

  setupInterceptors(instance);
  return instance;
}

/**
 * Default centralized HTTP client instance for Elsasa SIS frontend.
 */
export const apiClient = createHttpClient();
