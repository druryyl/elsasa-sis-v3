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
import { TEACHING_PERMISSIONS } from '../constants/permissions';

function createTestUser(overrides?: Partial<UserSession>): UserSession {
  return {
    userId: '11111111-1111-1111-1111-111111111111',
    username: 'teaching.admin',
    fullName: 'Teaching Admin',
    roles: ['TeachingAuthority'],
    permissions: [TEACHING_PERMISSIONS.VIEW],
    campusId: 'campus-main',
    ...overrides,
  };
}

describe('Teaching Domain Navigation & Route Integration (P8-S28)', () => {
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

  describe('Route Registration & Navigation Guards', () => {
    it('redirects unauthenticated user accessing /teaching/faculty to /auth/login with redirect param', async () => {
      const authStore = useAuthStore();
      authStore.logout();
      expect(authStore.isAuthenticated).toBe(false);

      await router.push('/teaching/faculty');

      expect(router.currentRoute.value.path).toBe('/auth/login');
      expect(router.currentRoute.value.query.redirect).toBe('/teaching/faculty');
    });

    it('redirects unauthorized user without Teaching View permission to /403', async () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Guest'],
          permissions: ['academics.curriculum.view'],
        }),
      );

      await router.push('/teaching/faculty');

      expect(router.currentRoute.value.path).toBe('/403');
    });

    it('allows navigation to /teaching/faculty for user with Permissions.Teaching.Faculty.View', async () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Staff'],
          permissions: [TEACHING_PERMISSIONS.VIEW],
        }),
      );

      await router.push('/teaching/faculty');

      expect(router.currentRoute.value.path).toBe('/teaching/faculty');
      expect(router.currentRoute.value.name).toBe('teaching-faculty');
      expect(document.title).toBe('Faculty Resources — Elsasa SIS');
    });

    it('redirects /teaching to /teaching/faculty', async () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Staff'],
          permissions: [TEACHING_PERMISSIONS.VIEW],
        }),
      );

      await router.push('/teaching');

      expect(router.currentRoute.value.path).toBe('/teaching/faculty');
      expect(router.currentRoute.value.name).toBe('teaching-faculty');
    });
  });

  describe('AppLayout Sidebar & Navigation Tree', () => {
    it('renders Teaching group and Faculty Resources menu item when user has permission', async () => {
      const wrapper = mountLayout();

      const teachingGroup = wrapper.find('[data-testid="nav-group-teaching"]');
      expect(teachingGroup.exists()).toBe(true);
      expect(teachingGroup.text()).toContain('Teaching');

      const facultyItem = wrapper.find('[data-testid="nav-item-teaching-faculty"]');
      expect(facultyItem.exists()).toBe(true);
      expect(facultyItem.text()).toContain('Faculty Resources');
      expect(facultyItem.attributes('href')).toBe('/teaching/faculty');
      expect(facultyItem.find('i.pi.pi-users').exists()).toBe(true);
    });

    it('hides Teaching group when user lacks Permissions.Teaching.Faculty.View', async () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AdmissionOfficer'],
          permissions: ['Permissions.Students.Admission.View'],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-group-teaching"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-item-teaching-faculty"]').exists()).toBe(false);
    });

    it('highlights active item when navigated to /teaching/faculty', async () => {
      await router.push('/teaching/faculty');
      const wrapper = mountLayout();

      const facultyItem = wrapper.find('[data-testid="nav-item-teaching-faculty"]');
      expect(facultyItem.exists()).toBe(true);
      expect(facultyItem.classes()).toContain('border-l-2');
      expect(facultyItem.classes()).toContain('border-teal-600');
    });
  });

  describe('Dynamic Breadcrumbs & Header Integration', () => {
    it('resolves breadcrumbs correctly for /teaching/faculty', async () => {
      await router.push('/teaching/faculty');
      const wrapper = mountLayout();

      const breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');
      expect(breadcrumbs.exists()).toBe(true);
      expect(breadcrumbs.text()).toContain('Home');
      expect(breadcrumbs.text()).toContain('Teaching');
      expect(breadcrumbs.text()).toContain('Faculty Resources');

      const pageTitle = wrapper.find('[data-testid="layout-page-title"]');
      expect(pageTitle.exists()).toBe(true);
      expect(pageTitle.text()).toBe('Faculty Resources');
    });
  });
});
