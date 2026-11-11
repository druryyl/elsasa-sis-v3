// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory } from 'vue-router';
import { createAppRouter } from '@/app/router';
import { useAuthStore, type UserSession } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useUIStore } from '@/app/stores/ui.store';
import { storage } from '@/shared/services/storage';
import AppLayout from '@/app/layouts/AppLayout.vue';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ToastService from 'primevue/toastservice';
import { primevueConfig } from '@/app/config/primevue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';

function createTestUser(overrides?: Partial<UserSession>): UserSession {
  return {
    userId: '11111111-1111-1111-1111-111111111111',
    username: 'teaching.admin',
    fullName: 'Teaching Admin',
    roles: ['TeachingCoordinator'],
    permissions: [
      'Permissions.Teaching.Faculty.View',
      'Permissions.Teaching.Assignments.View',
      'Permissions.Teaching.Workload.View',
      'Permissions.Teaching.Availability.Declare',
    ],
    campusId: 'campus-main',
    ...overrides,
  };
}

describe('Stage 2 Teaching Domain Navigation & Route Integration (P8-S28)', () => {
  let router: ReturnType<typeof createAppRouter>;
  let queryClient: QueryClient;

  beforeEach(async () => {
    if (typeof window !== 'undefined') {
      window.scrollTo = () => {};
      window.localStorage?.clear();
    }
    storage.clear();
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const authStore = useAuthStore();
    authStore.setSession('test-token', createTestUser());
    const uiStore = useUIStore();
    uiStore.resetPreferences();
    const appContextStore = useAppContextStore();
    appContextStore.resetContext();
    router = createAppRouter({ history: createMemoryHistory() });
    await router.push('/dashboard');
    await router.isReady();
  });

  function mountLayout() {
    return mount(AppLayout, {
      global: {
        plugins: [
          router,
          [PrimeVue, primevueConfig],
          ToastService,
          ConfirmationService,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterView: {
            template:
              '<div data-testid="router-view-content"><slot :Component="{ template: `<div>View</div>` }" :route="$route" /></div>',
          },
        },
      },
    });
  }

  describe('Route Registration & Permission Guards', () => {
    describe('/teaching/assignments', () => {
      it('redirects unauthenticated user accessing /teaching/assignments to login', async () => {
        const authStore = useAuthStore();
        authStore.logout();

        await router.push('/teaching/assignments');

        expect(router.currentRoute.value.path).toBe('/auth/login');
        expect(router.currentRoute.value.query.redirect).toBe('/teaching/assignments');
      });

      it('redirects unauthorized user without Assignments View permission to /403', async () => {
        const authStore = useAuthStore();
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: ['Guest'],
            permissions: ['academics.curriculum.view'],
          }),
        );

        await router.push('/teaching/assignments');

        expect(router.currentRoute.value.path).toBe('/403');
      });

      it('allows navigation to /teaching/assignments for user with Permissions.Teaching.Assignments.View', async () => {
        const authStore = useAuthStore();
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: ['TeachingCoordinator'],
            permissions: ['Permissions.Teaching.Assignments.View'],
          }),
        );

        await router.push('/teaching/assignments');

        expect(router.currentRoute.value.path).toBe('/teaching/assignments');
        expect(router.currentRoute.value.name).toBe('teaching-assignments');
        expect(document.title).toBe('Assignments & Coverage — Elsasa SIS');
      });
    });

    describe('/teaching/workload', () => {
      it('redirects unauthenticated user accessing /teaching/workload to login', async () => {
        const authStore = useAuthStore();
        authStore.logout();

        await router.push('/teaching/workload');

        expect(router.currentRoute.value.path).toBe('/auth/login');
        expect(router.currentRoute.value.query.redirect).toBe('/teaching/workload');
      });

      it('redirects unauthorized user without Workload View permission to /403', async () => {
        const authStore = useAuthStore();
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: ['Guest'],
            permissions: ['Permissions.Students.Admission.View'],
          }),
        );

        await router.push('/teaching/workload');

        expect(router.currentRoute.value.path).toBe('/403');
      });

      it('allows navigation to /teaching/workload for user with Permissions.Teaching.Workload.View', async () => {
        const authStore = useAuthStore();
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: ['TeachingCoordinator'],
            permissions: ['Permissions.Teaching.Workload.View'],
          }),
        );

        await router.push('/teaching/workload');

        expect(router.currentRoute.value.path).toBe('/teaching/workload');
        expect(router.currentRoute.value.name).toBe('teaching-workload');
        expect(document.title).toBe('Workload Monitoring — Elsasa SIS');
      });
    });

    describe('/teaching/my-availability', () => {
      it('redirects unauthenticated user accessing /teaching/my-availability to login', async () => {
        const authStore = useAuthStore();
        authStore.logout();

        await router.push('/teaching/my-availability');

        expect(router.currentRoute.value.path).toBe('/auth/login');
        expect(router.currentRoute.value.query.redirect).toBe('/teaching/my-availability');
      });

      it('redirects unauthorized user without Availability Declare permission to /403', async () => {
        const authStore = useAuthStore();
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: ['Guest'],
            permissions: ['Permissions.Finance.Policies.View'],
          }),
        );

        await router.push('/teaching/my-availability');

        expect(router.currentRoute.value.path).toBe('/403');
      });

      it('allows navigation to /teaching/my-availability for user with Permissions.Teaching.Availability.Declare', async () => {
        const authStore = useAuthStore();
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: ['Teacher'],
            permissions: ['Permissions.Teaching.Availability.Declare'],
          }),
        );

        await router.push('/teaching/my-availability');

        expect(router.currentRoute.value.path).toBe('/teaching/my-availability');
        expect(router.currentRoute.value.name).toBe('teaching-availability');
        expect(document.title).toBe('My Teaching Availability — Elsasa SIS');
      });
    });
  });

  describe('AppLayout Sidebar & Navigation Tree', () => {
    it('renders all Stage 2 Teaching menu items when user has permissions', () => {
      const wrapper = mountLayout();

      const teachingGroup = wrapper.find('[data-testid="nav-group-teaching"]');
      expect(teachingGroup.exists()).toBe(true);

      const assignmentsItem = wrapper.find('[data-testid="nav-item-teaching-assignments"]');
      expect(assignmentsItem.exists()).toBe(true);
      expect(assignmentsItem.text()).toContain('Assignments & Coverage');
      expect(assignmentsItem.attributes('href')).toBe('/teaching/assignments');
      expect(assignmentsItem.find('i.pi.pi-briefcase').exists()).toBe(true);

      const workloadItem = wrapper.find('[data-testid="nav-item-teaching-workload"]');
      expect(workloadItem.exists()).toBe(true);
      expect(workloadItem.text()).toContain('Workload Monitoring');
      expect(workloadItem.attributes('href')).toBe('/teaching/workload');
      expect(workloadItem.find('i.pi.pi-chart-bar').exists()).toBe(true);

      const availabilityItem = wrapper.find('[data-testid="nav-item-teaching-my-availability"]');
      expect(availabilityItem.exists()).toBe(true);
      expect(availabilityItem.text()).toContain('My Availability');
      expect(availabilityItem.attributes('href')).toBe('/teaching/my-availability');
      expect(availabilityItem.find('i.pi.pi-clock').exists()).toBe(true);
    });

    it('highlights active item when navigated to /teaching/assignments', async () => {
      await router.push('/teaching/assignments');
      const wrapper = mountLayout();

      const assignmentsItem = wrapper.find('[data-testid="nav-item-teaching-assignments"]');
      expect(assignmentsItem.exists()).toBe(true);
      expect(assignmentsItem.classes()).toContain('border-l-2');
      expect(assignmentsItem.classes()).toContain('border-teal-600');
    });

    it('highlights active item when navigated to /teaching/workload', async () => {
      await router.push('/teaching/workload');
      const wrapper = mountLayout();

      const workloadItem = wrapper.find('[data-testid="nav-item-teaching-workload"]');
      expect(workloadItem.exists()).toBe(true);
      expect(workloadItem.classes()).toContain('border-l-2');
      expect(workloadItem.classes()).toContain('border-teal-600');
    });

    it('highlights active item when navigated to /teaching/my-availability', async () => {
      await router.push('/teaching/my-availability');
      const wrapper = mountLayout();

      const availabilityItem = wrapper.find('[data-testid="nav-item-teaching-my-availability"]');
      expect(availabilityItem.exists()).toBe(true);
      expect(availabilityItem.classes()).toContain('border-l-2');
      expect(availabilityItem.classes()).toContain('border-teal-600');
    });
  });
});
