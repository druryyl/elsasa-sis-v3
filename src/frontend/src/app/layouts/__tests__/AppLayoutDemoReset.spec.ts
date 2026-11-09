// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory } from 'vue-router';
import { createAppRouter } from '@/app/router';
import { useAuthStore, type UserSession } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useUIStore } from '@/app/stores/ui.store';
import { storage } from '@/shared/services/storage';
import AppLayout from '../AppLayout.vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { primevueConfig } from '@/app/config/primevue';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';

// Mock primevue/usetoast
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

// Mock the useResetDemoData composable to avoid actual API calls
vi.mock('@/features/operations/demo-data/composables/useDemoData', () => ({
  useResetDemoData: () => ({
    resetDemoDataAsync: vi.fn(),
    isPending: { value: false },
    error: { value: null },
    isSuccess: { value: false },
    mutate: vi.fn(),
  }),
}));

// Stub for RouterView
const RouterViewStub = {
  template: '<div data-testid="router-view-content"><slot :Component="{ template: `<div>View</div>` }" :route="$route" /></div>',
};

function createTestUser(overrides?: Partial<UserSession>): UserSession {
  return {
    userId: '11111111-1111-1111-1111-111111111111',
    username: 'super.admin',
    fullName: 'Administrator User',
    roles: ['SuperAdmin'],
    permissions: ['*'],
    campusId: 'campus-main',
    ...overrides,
  };
}

describe('AppLayout.vue - Reset Demo Data Role Guards (TD-08, TD-10)', () => {
  let router: ReturnType<typeof createAppRouter>;
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        })),
      });

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

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

  function mountLayout(authOverrides?: Partial<UserSession>) {
    if (authOverrides) {
      const authStore = useAuthStore();
      authStore.setSession('test-token', createTestUser(authOverrides));
    }

    return mount(AppLayout, {
      global: {
        plugins: [
          router,
          [PrimeVue, primevueConfig],
          ToastService,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterView: RouterViewStub,
          teleport: true,
        },
      },
    });
  }

  async function openUserProfileMenu(wrapper: ReturnType<typeof mount>) {
    const profileBtn = wrapper.find('[data-testid="user-profile-menu-button"]');
    await profileBtn.trigger('click');
    await flushPromises();
  }

  function findResetButton(wrapper: ReturnType<typeof mount>) {
    return wrapper.find('[data-testid="reset-demo-data-button"]');
  }

  function findLogoutButton(wrapper: ReturnType<typeof mount>) {
    return wrapper.find('[data-testid="logout-button"]');
  }

  describe('Reset Demo Data Menu Item Visibility', () => {
    it('shows "Reset Demo Data" menu item when authenticated as SuperAdmin in non-production', async () => {
      // In test environment, import.meta.env.PROD is false
      // SuperAdmin should see the reset button
      const wrapper = mountLayout({ roles: ['SuperAdmin'] });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(true);
      expect(resetBtn.text()).toContain('Reset Demo Data');
    });

    it('hides "Reset Demo Data" menu item when authenticated as regular Staff', async () => {
      const wrapper = mountLayout({
        roles: ['Staff'],
        permissions: ['Permissions.Operations.Calendar.Manage'],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('hides "Reset Demo Data" menu item when authenticated as Student', async () => {
      const wrapper = mountLayout({
        roles: ['Student'],
        permissions: [],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('hides "Reset Demo Data" menu item when authenticated as AcademicCoordinator', async () => {
      const wrapper = mountLayout({
        roles: ['AcademicCoordinator'],
        permissions: [],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('hides "Reset Demo Data" menu item when authenticated as Teacher', async () => {
      const wrapper = mountLayout({
        roles: ['Teacher'],
        permissions: [],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('hides "Reset Demo Data" menu item when authenticated as Admin (non-SuperAdmin)', async () => {
      const wrapper = mountLayout({
        roles: ['Admin'],
        permissions: [],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('hides "Reset Demo Data" menu item when authenticated as OperationsStaff', async () => {
      const wrapper = mountLayout({
        roles: ['OperationsStaff'],
        permissions: ['Permissions.Operations.Calendar.Manage'],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('hides "Reset Demo Data" menu item when user has no roles', async () => {
      const wrapper = mountLayout({
        roles: ['Guest'],
        permissions: [],
      });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(false);
    });

    it('shows "Reset Demo Data" button with amber/warning styling', async () => {
      const wrapper = mountLayout({ roles: ['SuperAdmin'] });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      expect(resetBtn.exists()).toBe(true);
      expect(resetBtn.classes()).toContain('text-amber-700');
      expect(resetBtn.classes()).toContain('hover:bg-amber-50');
    });
  });

  describe('Reset Demo Data Dialog Integration', () => {
    it('opens ResetDemoDataDialog when clicking "Reset Demo Data" button', async () => {
      const wrapper = mountLayout({ roles: ['SuperAdmin'] });

      await openUserProfileMenu(wrapper);

      const resetBtn = findResetButton(wrapper);
      await resetBtn.trigger('click');
      await flushPromises();

      // The dialog should be rendered in the template
      const dialog = wrapper.findComponent({ name: 'ResetDemoDataDialog' });
      expect(dialog.exists()).toBe(true);
    });

    it('closes user profile menu when "Reset Demo Data" is clicked', async () => {
      const wrapper = mountLayout({ roles: ['SuperAdmin'] });

      await openUserProfileMenu(wrapper);
      const menuBefore = wrapper.find('[data-testid="user-profile-menu"]');
      expect(menuBefore.exists()).toBe(true);

      const resetBtn = findResetButton(wrapper);
      await resetBtn.trigger('click');
      await flushPromises();

      // Menu should close
      const menuAfter = wrapper.find('[data-testid="user-profile-menu"]');
      expect(menuAfter.exists()).toBe(false);
    });
  });

  describe('Environment Guard (Non-Production Only)', () => {
    it('canResetDemoData computed returns true for SuperAdmin in test (non-prod) environment', () => {
      const wrapper = mountLayout({ roles: ['SuperAdmin'] });

      // Access the component's computed property
      const vm = wrapper.vm as any;
      expect(vm.canResetDemoData).toBe(true);
    });

    it('canResetDemoData computed returns false for non-SuperAdmin even in test environment', () => {
      const wrapper = mountLayout({ roles: ['Staff'] });

      const vm = wrapper.vm as any;
      expect(vm.canResetDemoData).toBe(false);
    });
  });

  describe('Logout Button Always Visible', () => {
    it('shows logout button for all authenticated users', async () => {
      // Test SuperAdmin
      let wrapper = mountLayout({ roles: ['SuperAdmin'] });
      await openUserProfileMenu(wrapper);
      let logoutBtn = findLogoutButton(wrapper);
      expect(logoutBtn.exists()).toBe(true);

      // Test Staff
      wrapper = mountLayout({ roles: ['Staff'] });
      await openUserProfileMenu(wrapper);
      logoutBtn = findLogoutButton(wrapper);
      expect(logoutBtn.exists()).toBe(true);

      // Test Student
      wrapper = mountLayout({ roles: ['Student'] });
      await openUserProfileMenu(wrapper);
      logoutBtn = findLogoutButton(wrapper);
      expect(logoutBtn.exists()).toBe(true);
    });

    it('logout button has rose/danger styling', async () => {
      const wrapper = mountLayout({ roles: ['SuperAdmin'] });

      await openUserProfileMenu(wrapper);

      const logoutBtn = findLogoutButton(wrapper);
      expect(logoutBtn.classes()).toContain('text-rose-600');
      expect(logoutBtn.classes()).toContain('hover:bg-rose-50');
    });
  });
});