// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { createMemoryHistory } from 'vue-router';
import { useAuthStore, type UserSession } from '@/app/stores/auth.store';
import { createAppRouter } from '../index';
import { resolveDocumentTitle, APP_TITLE_SUFFIX } from '../guards';

function createTestUser(overrides?: Partial<UserSession>): UserSession {
  return {
    userId: '11111111-1111-1111-1111-111111111111',
    username: 'test.officer',
    fullName: 'Test Officer',
    roles: ['AdmissionOfficer'],
    permissions: ['Permissions.Students.Admission.View'],
    campusId: 'campus-1',
    ...overrides,
  };
}

describe('Navigation Guards & Router Integration', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    document.title = 'Default Title';
    if (typeof window !== 'undefined') {
      window.scrollTo = () => {};
    }
  });

  describe('resolveDocumentTitle', () => {
    it('returns default app title when title is undefined', () => {
      expect(resolveDocumentTitle()).toBe(APP_TITLE_SUFFIX);
      expect(resolveDocumentTitle('')).toBe(APP_TITLE_SUFFIX);
    });

    it('formats title with Elsasa SIS suffix when not already suffixed', () => {
      expect(resolveDocumentTitle('Dashboard')).toBe('Dashboard — Elsasa SIS');
      expect(resolveDocumentTitle('Calendar & Periods Workspace')).toBe(
        'Calendar & Periods Workspace — Elsasa SIS',
      );
    });

    it('preserves title when it already ends with Elsasa SIS', () => {
      expect(resolveDocumentTitle('Sign In — Elsasa SIS')).toBe('Sign In — Elsasa SIS');
    });
  });

  describe('Unauthenticated Redirection (requiresAuth)', () => {
    it('redirects unauthenticated user accessing /dashboard to /auth/login with redirect param', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      expect(authStore.isAuthenticated).toBe(false);

      await router.push('/dashboard');

      expect(router.currentRoute.value.path).toBe('/auth/login');
      expect(router.currentRoute.value.query.redirect).toBe('/dashboard');
      expect(router.currentRoute.value.fullPath).toContain('/auth/login?redirect=');
      expect(document.title).toBe('Sign In — Elsasa SIS');
    });

    it('preserves query parameters in fullPath when redirecting to login', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });

      await router.push('/dashboard?view=dense&tab=overview');

      expect(router.currentRoute.value.path).toBe('/auth/login');
      expect(router.currentRoute.value.query.redirect).toBe('/dashboard?view=dense&tab=overview');
      expect(router.currentRoute.value.fullPath).toContain('/auth/login?redirect=');
    });

    it('redirects root / through redirect to /dashboard and then to /auth/login', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });

      await router.push('/');

      expect(router.currentRoute.value.path).toBe('/auth/login');
      expect(router.currentRoute.value.query.redirect).toBe('/dashboard');
    });

    it('allows unauthenticated navigation to public route /auth/login without redirect', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });

      await router.push('/auth/login');

      expect(router.currentRoute.value.path).toBe('/auth/login');
      expect(router.currentRoute.value.name).toBe('auth.login');
      expect(document.title).toBe('Sign In — Elsasa SIS');
    });
  });

  describe('Permission Navigation Enforcement', () => {
    it('allows authenticated user to navigate to /dashboard without specific permissions', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession('valid-token', createTestUser({ permissions: [] }));

      await router.push('/dashboard');

      expect(router.currentRoute.value.path).toBe('/dashboard');
      expect(router.currentRoute.value.name).toBe('dashboard');
      expect(document.title).toBe('Dashboard — Elsasa SIS');
    });

    it('redirects authenticated user lacking permission to /403', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          permissions: ['Permissions.Students.Admission.View'],
        }),
      );

      // Attempting to visit operations calendar which requires Permissions.Operations.Calendar.Manage
      await router.push('/operations/calendar');

      expect(router.currentRoute.value.path).toBe('/403');
      expect(router.currentRoute.value.name).toBe('forbidden');
      expect(document.title).toBe('Access Forbidden — Elsasa SIS');
    });

    it('allows authenticated user with required permission to access /operations/calendar', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          permissions: ['Permissions.Operations.Calendar.Manage'],
        }),
      );

      await router.push('/operations/calendar');

      expect(router.currentRoute.value.path).toBe('/operations/calendar');
      expect(router.currentRoute.value.name).toBe('operations.calendar.workspace');
      expect(document.title).toBe('Calendar & Periods Workspace — Elsasa SIS');
    });

    it('allows SuperAdmin role to access permission-protected route without explicit permission', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['SuperAdmin'],
          permissions: [],
        }),
      );

      await router.push('/operations/calendar');

      expect(router.currentRoute.value.path).toBe('/operations/calendar');
      expect(router.currentRoute.value.name).toBe('operations.calendar.workspace');
    });

    it('allows wildcard permission (*) to access any permission-protected route', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: ['*'],
        }),
      );

      await router.push('/operations/calendar');

      expect(router.currentRoute.value.path).toBe('/operations/calendar');
    });
  });

  describe('Hierarchical Route Groups & Catch-All Matching', () => {
    it('enforces permissions on Students domain routes', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // User has admission view permission only
      authStore.setSession(
        'valid-token',
        createTestUser({
          permissions: ['Permissions.Students.Admission.View'],
        }),
      );

      // Can access admission queue
      await router.push('/students/admission');
      expect(router.currentRoute.value.path).toBe('/students/admission');
      expect(router.currentRoute.value.name).toBe('students.admission.queue');
      expect(document.title).toBe('Admission Work Queue — Elsasa SIS');

      // Cannot access enrollment desk without Permissions.Students.Enrollment.Execute
      await router.push('/students/enrollment');
      expect(router.currentRoute.value.path).toBe('/403');
    });

    it('enforces permissions on Finance domain routes', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      authStore.setSession(
        'valid-token',
        createTestUser({
          permissions: ['Permissions.Finance.Payments.View'],
        }),
      );

      // Can access payment verification queue
      await router.push('/finance/payments');
      expect(router.currentRoute.value.path).toBe('/finance/payments');
      expect(router.currentRoute.value.name).toBe('finance.payments.queue');
      expect(document.title).toBe('Payment Verification Queue — Elsasa SIS');

      // Cannot access obligations list without Permissions.Finance.Obligations.View
      await router.push('/finance/obligations');
      expect(router.currentRoute.value.path).toBe('/403');
    });

    it('matches catch-all not-found route when authenticated', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession('valid-token', createTestUser());

      await router.push('/random/non-existent/route');

      expect(router.currentRoute.value.name).toBe('not-found');
      expect(document.title).toBe('Page Not Found — Elsasa SIS');
    });

    it('redirects authenticated user from / to /dashboard', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession('valid-token', createTestUser());

      await router.push('/');

      expect(router.currentRoute.value.path).toBe('/dashboard');
      expect(router.currentRoute.value.name).toBe('dashboard');
      expect(document.title).toBe('Dashboard — Elsasa SIS');
    });

    it('falls back to default app title when navigating to a route without title meta', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession('valid-token', createTestUser());

      router.addRoute({
        path: '/un-titled-test',
        name: 'untitled',
        component: { template: '<div>Untitled</div>' },
      });

      await router.push('/un-titled-test');

      expect(router.currentRoute.value.path).toBe('/un-titled-test');
      expect(document.title).toBe(APP_TITLE_SUFFIX);
    });
  });

  describe('Router Instance & Scroll Behavior', () => {
    it('scrollBehavior restores savedPosition if available', () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const scrollBehavior = router.options.scrollBehavior;
      expect(typeof scrollBehavior).toBe('function');

      if (scrollBehavior) {
        const to = router.currentRoute.value;
        const from = router.currentRoute.value;
        const savedPosition = { left: 10, top: 200 };
        const result = scrollBehavior(to, from, savedPosition);
        expect(result).toEqual({ left: 10, top: 200 });
      }
    });

    it('scrollBehavior scrolls to top { top: 0 } when no savedPosition is present', () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const scrollBehavior = router.options.scrollBehavior;

      if (scrollBehavior) {
        const to = router.currentRoute.value;
        const from = router.currentRoute.value;
        const result = scrollBehavior(to, from, null);
        expect(result).toEqual({ top: 0 });
      }
    });
  });
});
