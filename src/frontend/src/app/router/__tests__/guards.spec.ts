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

    it('enforces permissions on Academics curriculum workspace route', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // User lacking academics permission
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: ['Permissions.Operations.Calendar.Manage'],
        }),
      );

      await router.push('/academics/curriculum');
      expect(router.currentRoute.value.path).toBe('/403');

      // User with academics.curriculum.view permission
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Staff'],
          permissions: ['academics.curriculum.view'],
        }),
      );

      await router.push('/academics/curriculum');
      expect(router.currentRoute.value.path).toBe('/academics/curriculum');
      expect(router.currentRoute.value.name).toBe('curriculum-workspace');
      expect(document.title).toBe('Curriculum & Learning Structures — Elsasa SIS');

      // User with AcademicCoordinator role
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['AcademicCoordinator'],
          permissions: [],
        }),
      );

      await router.push('/academics/curriculum');
      expect(router.currentRoute.value.path).toBe('/academics/curriculum');
      expect(router.currentRoute.value.name).toBe('curriculum-workspace');
    });

    it('enforces permissions on Academics learning-plans workspace route (P7-S25)', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // 1. Unauthorized user lacking learning-plans permission redirected to /403
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: ['Permissions.Operations.Calendar.Manage'],
        }),
      );

      await router.push('/academics/learning-plans');
      expect(router.currentRoute.value.path).toBe('/403');
      expect(router.currentRoute.value.name).toBe('forbidden');

      // 2. User with permission 'academics:learning-plans:view'
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Staff'],
          permissions: ['academics:learning-plans:view'],
        }),
      );

      await router.push('/academics/learning-plans');
      expect(router.currentRoute.value.path).toBe('/academics/learning-plans');
      expect(router.currentRoute.value.name).toBe('academics.learning-plans');
      expect(document.title).toBe('Learning Plans — Elsasa SIS');

      // 3. User with PascalCase permission 'Permissions.Academics.LearningPlans.View'
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Staff'],
          permissions: ['Permissions.Academics.LearningPlans.View'],
        }),
      );

      await router.push('/academics/learning-plans');
      expect(router.currentRoute.value.path).toBe('/academics/learning-plans');

      // 4. Authorized roles (AcademicCoordinator, AcademicAuthority, Teacher, StudentDevelopmentOfficer, Admin)
      for (const role of [
        'AcademicCoordinator',
        'AcademicAuthority',
        'Teacher',
        'StudentDevelopmentOfficer',
        'Admin',
      ]) {
        authStore.setSession(
          'valid-token',
          createTestUser({
            roles: [role],
            permissions: [],
          }),
        );

        await router.push('/academics/learning-plans');
        expect(
          router.currentRoute.value.path,
          `Expected /academics/learning-plans to be accessible for role ${role}`,
        ).toBe('/academics/learning-plans');
      }
    });

    it('enforces permissions on Academics participation workspace route', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // User lacking participation permission
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: ['Permissions.Operations.Calendar.Manage'],
        }),
      );

      await router.push('/academics/participation');
      expect(router.currentRoute.value.path).toBe('/403');

      // User with academics.participations.view permission
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Staff'],
          permissions: ['academics.participations.view'],
        }),
      );

      await router.push('/academics/participation');
      expect(router.currentRoute.value.path).toBe('/academics/participation');
      expect(router.currentRoute.value.name).toBe('academics.participation');
      expect(document.title).toBe('Participation & Rosters — Elsasa SIS');

      // User with AcademicCoordinator role
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['AcademicCoordinator'],
          permissions: [],
        }),
      );

      await router.push('/academics/participation');
      expect(router.currentRoute.value.path).toBe('/academics/participation');
      expect(router.currentRoute.value.name).toBe('academics.participation');
    });

    it('enforces permissions on Academics sessions workspace route', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // Denied user
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: [],
        }),
      );

      await router.push('/academics/sessions');
      expect(router.currentRoute.value.path).toBe('/403');

      // Teacher role allowed
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Teacher'],
          permissions: [],
        }),
      );

      await router.push('/academics/sessions');
      expect(router.currentRoute.value.path).toBe('/academics/sessions');
      expect(router.currentRoute.value.name).toBe('academics.sessions');
      expect(document.title).toBe('Class Sessions — Elsasa SIS');
    });

    it('enforces permissions on Academics coursework workspace route', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // Denied user
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: [],
        }),
      );

      await router.push('/academics/coursework');
      expect(router.currentRoute.value.path).toBe('/403');

      // Teacher role allowed
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Teacher'],
          permissions: [],
        }),
      );

      await router.push('/academics/coursework');
      expect(router.currentRoute.value.path).toBe('/academics/coursework');
      expect(router.currentRoute.value.name).toBe('academics.coursework');
      expect(document.title).toBe('Coursework — Elsasa SIS');
    });

    it('enforces role barrier on Student learning workspace route (/portal/learning)', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      // Non-student role denied
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['AdmissionOfficer'],
          permissions: [],
        }),
      );

      await router.push('/portal/learning');
      expect(router.currentRoute.value.path).toBe('/403');

      // Student role allowed
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Student'],
          permissions: [],
        }),
      );

      await router.push('/portal/learning');
      expect(router.currentRoute.value.path).toBe('/portal/learning');
      expect(router.currentRoute.value.name).toBe('portal.learning');
      expect(document.title).toBe('My Learning & Coursework — Elsasa SIS');
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

  describe('Level 2 Domain Dashboard Authorization (P5-S11, TD-05)', () => {
    const ALL_DOMAIN_ROUTES = [
      '/dashboard/students',
      '/dashboard/academics',
      '/dashboard/finance',
      '/dashboard/teaching',
      '/dashboard/operations',
    ] as const;

    it('redirects unauthenticated users accessing Level 2 dashboards to /auth/login', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });

      await router.push('/dashboard/teaching');

      expect(router.currentRoute.value.path).toBe('/auth/login');
      expect(router.currentRoute.value.query.redirect).toBe('/dashboard/teaching');
    });

    it('grants SuperAdmin and role-only Admin access to all Level 2 domain dashboards', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      for (const role of ['SuperAdmin', 'Admin']) {
        authStore.setSession(
          'valid-token',
          createTestUser({
            roles: [role],
            permissions: [],
          }),
        );

        for (const path of ALL_DOMAIN_ROUTES) {
          await router.push(path);
          expect(
            router.currentRoute.value.path,
            `Expected ${role} to access ${path}`,
          ).toBe(path);
        }
      }
    });

    it('allows AcademicCoordinator (with academics permissions only) to access academics, teaching, and operations dashboards while blocking students and finance', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['AcademicCoordinator', 'AcademicAuthority'],
          permissions: ['academics.participations.view', 'academics.curriculum.view'],
        }),
      );

      for (const allowedPath of [
        '/dashboard/academics',
        '/dashboard/teaching',
        '/dashboard/operations',
      ]) {
        await router.push(allowedPath);
        expect(router.currentRoute.value.path).toBe(allowedPath);
      }

      for (const deniedPath of ['/dashboard/students', '/dashboard/finance']) {
        await router.push(deniedPath);
        expect(router.currentRoute.value.path).toBe('/403');
      }
    });

    it('allows Teacher to access academics and teaching dashboards while blocking students, finance, and operations', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Teacher', 'AcademicStaff'],
          permissions: ['academics.participations.view', 'Permissions.Academics.Coursework.View'],
        }),
      );

      for (const allowedPath of ['/dashboard/academics', '/dashboard/teaching']) {
        await router.push(allowedPath);
        expect(router.currentRoute.value.path).toBe(allowedPath);
      }

      for (const deniedPath of [
        '/dashboard/students',
        '/dashboard/finance',
        '/dashboard/operations',
      ]) {
        await router.push(deniedPath);
        expect(router.currentRoute.value.path).toBe('/403');
      }
    });

    it('grants role-only domain officers and authorities access to their authorized Level 2 dashboards without explicit permissions', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      const cases: Array<{ role: string; allowed: string[]; denied: string[] }> = [
        {
          role: 'AdmissionOfficer',
          allowed: ['/dashboard/students'],
          denied: ['/dashboard/academics', '/dashboard/finance', '/dashboard/teaching', '/dashboard/operations'],
        },
        {
          role: 'StudentAdminOfficer',
          allowed: ['/dashboard/students'],
          denied: ['/dashboard/finance', '/dashboard/operations'],
        },
        {
          role: 'SchoolLeadership',
          allowed: ['/dashboard/students'],
          denied: ['/dashboard/finance', '/dashboard/operations'],
        },
        {
          role: 'FinanceOfficer',
          allowed: ['/dashboard/finance'],
          denied: ['/dashboard/students', '/dashboard/academics', '/dashboard/teaching', '/dashboard/operations'],
        },
        {
          role: 'FinanceStaff',
          allowed: ['/dashboard/finance'],
          denied: ['/dashboard/students', '/dashboard/operations'],
        },
        {
          role: 'FinanceAuthority',
          allowed: ['/dashboard/finance'],
          denied: ['/dashboard/students', '/dashboard/operations'],
        },
        {
          role: 'TeachingAuthority',
          allowed: ['/dashboard/teaching'],
          denied: ['/dashboard/students', '/dashboard/finance', '/dashboard/operations'],
        },
        {
          role: 'TeachingCoordinator',
          allowed: ['/dashboard/teaching'],
          denied: ['/dashboard/students', '/dashboard/finance', '/dashboard/operations'],
        },
        {
          role: 'OperationsAuthority',
          allowed: ['/dashboard/operations'],
          denied: ['/dashboard/students', '/dashboard/academics', '/dashboard/finance', '/dashboard/teaching'],
        },
        {
          role: 'AcademicAdmin',
          allowed: ['/dashboard/academics', '/dashboard/operations'],
          denied: ['/dashboard/students', '/dashboard/finance'],
        },
      ];

      for (const testCase of cases) {
        authStore.setSession(
          'valid-token',
          createTestUser({
            roles: [testCase.role],
            permissions: [],
          }),
        );

        for (const path of testCase.allowed) {
          await router.push(path);
          expect(
            router.currentRoute.value.path,
            `Expected role ${testCase.role} to access ${path}`,
          ).toBe(path);
        }

        for (const path of testCase.denied) {
          await router.push(path);
          expect(
            router.currentRoute.value.path,
            `Expected role ${testCase.role} to be blocked from ${path}`,
          ).toBe('/403');
        }
      }
    });

    it('grants permission-bearing non-role staff access to their authorized Level 2 dashboards', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      const cases: Array<{ role: string; permission: string; allowed: string; denied: string }> = [
        {
          role: 'OperationsStaff',
          permission: 'Permissions.Operations.Calendar.Manage',
          allowed: '/dashboard/operations',
          denied: '/dashboard/students',
        },
        {
          role: 'OperationsStaff',
          permission: 'Permissions.Operations.Calendar.View',
          allowed: '/dashboard/operations',
          denied: '/dashboard/finance',
        },
        {
          role: 'Staff',
          permission: 'Permissions.Students.Records.View',
          allowed: '/dashboard/students',
          denied: '/dashboard/teaching',
        },
        {
          role: 'Staff',
          permission: 'Permissions.Academics.Coursework.View',
          allowed: '/dashboard/academics',
          denied: '/dashboard/finance',
        },
        {
          role: 'Staff',
          permission: 'Permissions.Finance.Obligations.View',
          allowed: '/dashboard/finance',
          denied: '/dashboard/academics',
        },
        {
          role: 'Staff',
          permission: 'Permissions.Teaching.Workload.View',
          allowed: '/dashboard/teaching',
          denied: '/dashboard/operations',
        },
      ];

      for (const testCase of cases) {
        authStore.setSession(
          'valid-token',
          createTestUser({
            roles: [testCase.role],
            permissions: [testCase.permission],
          }),
        );

        await router.push(testCase.allowed);
        expect(router.currentRoute.value.path).toBe(testCase.allowed);

        await router.push(testCase.denied);
        expect(router.currentRoute.value.path).toBe('/403');
      }
    });

    it('blocks Guest, Student, and Parent from all Level 2 operational domain dashboards even with self-service view permissions', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();

      const blockedUsers: Array<{ label: string; roles: string[]; permissions: string[] }> = [
        { label: 'Guest', roles: ['Guest'], permissions: [] },
        {
          label: 'Student',
          roles: ['Student'],
          permissions: ['academics.participations.view', 'Permissions.SelfService.Student'],
        },
        {
          label: 'Parent',
          roles: ['Parent'],
          permissions: [
            'academics.participations.view',
            'Permissions.Finance.Obligations.View',
            'Permissions.SelfService.Parent',
          ],
        },
      ];

      for (const user of blockedUsers) {
        authStore.setSession(
          'valid-token',
          createTestUser({
            roles: user.roles,
            permissions: user.permissions,
          }),
        );

        for (const path of ALL_DOMAIN_ROUTES) {
          await router.push(path);
          expect(
            router.currentRoute.value.path,
            `Expected ${user.label} to be blocked from ${path}`,
          ).toBe('/403');
        }
      }
    });

    it('allows authenticated users to access /dashboard/development Stage 3 placeholder', async () => {
      const router = createAppRouter({ history: createMemoryHistory() });
      const authStore = useAuthStore();
      authStore.setSession(
        'valid-token',
        createTestUser({
          roles: ['Guest'],
          permissions: [],
        }),
      );

      await router.push('/dashboard/development');
      expect(router.currentRoute.value.path).toBe('/dashboard/development');
      expect(router.currentRoute.value.name).toBe('dashboard.development');
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
