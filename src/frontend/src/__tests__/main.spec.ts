// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { bootstrapApplication, appVersion } from '@/main';
import { logger } from '@/shared/services/logger';
import { storage } from '@/shared/services/storage';
import { AUTH_STORAGE_KEY } from '@/app/stores/auth.store';

describe('main.ts bootstrap lifecycle', () => {
  let appContainer: HTMLDivElement;

  beforeEach(() => {
    localStorage.clear();
    appContainer = document.createElement('div');
    appContainer.id = 'app';
    document.body.appendChild(appContainer);
  });

  afterEach(() => {
    if (document.body.contains(appContainer)) {
      document.body.removeChild(appContainer);
    }
  });

  it('exports valid application version', () => {
    expect(appVersion).toBe('0.1.0');
  });

  it('executes complete bootstrap lifecycle and mounts to DOM container', () => {
    const result = bootstrapApplication('#app');

    expect(result).toBeDefined();
    expect(result.app).toBeDefined();
    expect(result.router).toBeDefined();
    expect(result.pinia).toBeDefined();
    expect(result.queryClient).toBeDefined();
    expect(result.mountedElement).toBe(appContainer);
  });

  it('hydrates auth session from storage during bootstrap', () => {
    storage.setItem(AUTH_STORAGE_KEY, {
      token: 'persisted-jwt-token',
      user: {
        userId: 'usr-persisted-1',
        username: 'persisted_user',
        fullName: 'Persisted Administrator',
        roles: ['SuperAdmin'],
        permissions: ['*'],
        campusId: 'campus-main',
      },
    });

    const result = bootstrapApplication('#app');
    const authStore = result.app.config.globalProperties.$pinia
      ? result.pinia.state.value.auth
      : null;

    expect(authStore).toBeDefined();
    expect(authStore?.token).toBe('persisted-jwt-token');
    expect(authStore?.user?.username).toBe('persisted_user');
  });

  it('wires global error handler that dispatches to logger', () => {
    const loggerSpy = vi.spyOn(logger, 'error').mockImplementation(() => {});
    const result = bootstrapApplication('#app');

    expect(typeof result.app.config.errorHandler).toBe('function');

    const testError = new Error('Test Vue crash');
    result.app.config.errorHandler!(testError, null, 'renderHook');

    expect(loggerSpy).toHaveBeenCalledWith(
      'Unhandled Vue application error',
      expect.objectContaining({
        error: 'Test Vue crash',
        info: 'renderHook',
      }),
    );

    loggerSpy.mockRestore();
  });
});
