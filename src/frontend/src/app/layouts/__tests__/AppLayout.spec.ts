// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory } from 'vue-router';
import { createAppRouter } from '@/app/router';
import { useAuthStore, type UserSession } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useUIStore } from '@/app/stores/ui.store';
import { storage } from '@/shared/services/storage';
import AppLayout from '../AppLayout.vue';
import AuthLayout from '../AuthLayout.vue';
import App from '@/App.vue';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ToastService from 'primevue/toastservice';
import { primevueConfig } from '@/app/config/primevue';

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

describe('AppLayout.vue', () => {
  let router: ReturnType<typeof createAppRouter>;

  beforeEach(async () => {
    if (typeof window !== 'undefined') {
      window.scrollTo = () => {};
      window.localStorage?.clear();
    }
    storage.clear();
    setActivePinia(createPinia());
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
        plugins: [router],
        stubs: {
          RouterView: {
            template:
              '<div data-testid="router-view-content"><slot :Component="{ template: `<div>View</div>` }" :route="$route" /></div>',
          },
        },
      },
    });
  }

  describe('Top Navigation Bar', () => {
    it('renders institution branding with logo and title', () => {
      const wrapper = mountLayout();
      const branding = wrapper.find('[data-testid="institution-branding"]');

      expect(branding.exists()).toBe(true);
      expect(branding.text()).toContain('Elsasa SIS');
      expect(branding.attributes('href')).toBe('/dashboard');
    });

    it('renders active academic period indicator with default or context period', async () => {
      const appContextStore = useAppContextStore();
      appContextStore.setActiveAcademicPeriod('2026/2027 Ganjil');

      const wrapper = mountLayout();
      const periodIndicator = wrapper.find('[data-testid="academic-period-indicator"]');

      expect(periodIndicator.exists()).toBe(true);
      expect(periodIndicator.text()).toContain('2026/2027 Ganjil');
    });

    it('renders campus selector placeholder and opens dropdown to select campus', async () => {
      const appContextStore = useAppContextStore();
      appContextStore.setActiveCampus('campus-main');

      const wrapper = mountLayout();
      const campusBtn = wrapper.find('[data-testid="campus-selector"]');

      expect(campusBtn.exists()).toBe(true);
      expect(campusBtn.text()).toContain('Main Campus (Kampus A)');

      // Open campus selector dropdown
      await campusBtn.trigger('click');
      const dropdown = wrapper.find('[data-testid="campus-selector-dropdown"]');
      expect(dropdown.exists()).toBe(true);

      // Select East Campus
      const options = dropdown.findAll('button');
      const eastCampusBtn = options.find((btn) => btn.text().includes('East Campus'));
      expect(eastCampusBtn).toBeDefined();
      await eastCampusBtn!.trigger('click');

      expect(appContextStore.activeCampusId).toBe('campus-east');
      expect(wrapper.find('[data-testid="campus-selector"]').text()).toContain(
        'East Campus (Kampus B)',
      );
    });

    it('renders user profile menu button with user full name and role', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          fullName: 'Budi Santoso',
          username: 'bsantoso',
          roles: ['AcademicDirector'],
        }),
      );

      const wrapper = mountLayout();
      const profileBtn = wrapper.find('[data-testid="user-profile-menu-button"]');

      expect(profileBtn.exists()).toBe(true);
      expect(profileBtn.text()).toContain('Budi Santoso');
      expect(profileBtn.text()).toContain('AcademicDirector');
    });

    it('opens user profile menu and triggers logout', async () => {
      const authStore = useAuthStore();
      expect(authStore.isAuthenticated).toBe(true);

      const wrapper = mountLayout();
      const profileBtn = wrapper.find('[data-testid="user-profile-menu-button"]');

      await profileBtn.trigger('click');
      const menu = wrapper.find('[data-testid="user-profile-menu"]');
      expect(menu.exists()).toBe(true);

      const logoutBtn = wrapper.find('[data-testid="logout-button"]');
      expect(logoutBtn.exists()).toBe(true);
      await logoutBtn.trigger('click');

      expect(authStore.isAuthenticated).toBe(false);
      expect(authStore.user).toBeNull();
      expect(authStore.token).toBeNull();
    });
  });

  describe('Collapsible Sidebar & Mini-Sidebar Mode', () => {
    it('toggles sidebar between expanded (w-64) and collapsed (w-20) modes via topbar button', async () => {
      const uiStore = useUIStore();
      expect(uiStore.isSidebarCollapsed).toBe(false);

      const wrapper = mountLayout();
      const sidebar = wrapper.find('[data-testid="app-sidebar"]');
      expect(sidebar.classes()).toContain('w-64');
      expect(sidebar.attributes('aria-expanded')).toBe('true');

      // Click topbar sidebar toggle button
      const toggleBtn = wrapper.find('[data-testid="sidebar-toggle-btn"]');
      await toggleBtn.trigger('click');

      expect(uiStore.isSidebarCollapsed).toBe(true);
      expect(sidebar.classes()).toContain('w-20');
      expect(sidebar.attributes('aria-expanded')).toBe('false');

      // Toggle back to expanded
      await toggleBtn.trigger('click');
      expect(uiStore.isSidebarCollapsed).toBe(false);
      expect(sidebar.classes()).toContain('w-64');
    });

    it('toggles sidebar via footer collapse button', async () => {
      const uiStore = useUIStore();
      const wrapper = mountLayout();
      const collapseBtn = wrapper.find('[data-testid="sidebar-collapse-btn"]');

      expect(collapseBtn.exists()).toBe(true);
      await collapseBtn.trigger('click');

      expect(uiStore.isSidebarCollapsed).toBe(true);
      expect(wrapper.find('[data-testid="app-sidebar"]').classes()).toContain('w-20');
    });
  });

  describe('Dynamic Permission-Driven Hierarchical Menu Filtering', () => {
    it('renders all sections and submenus for SuperAdmin', () => {
      const authStore = useAuthStore();
      authStore.setSession('test-token', createTestUser({ roles: ['SuperAdmin'] }));

      const wrapper = mountLayout();

      // Dashboard
      expect(wrapper.find('[data-testid="nav-item-dashboard"]').exists()).toBe(true);

      // Operations
      expect(wrapper.find('[data-testid="nav-group-operations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-operations-calendar"]').exists()).toBe(true);

      // Students
      expect(wrapper.find('[data-testid="nav-group-students"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-students-admission"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-students-enrollment"]').exists()).toBe(true);

      // Finance
      expect(wrapper.find('[data-testid="nav-group-finance"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-finance-obligations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-finance-payments"]').exists()).toBe(true);
    });

    it('renders only Operations section when user has Operations calendar permission', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: ['Permissions.Operations.Calendar.Manage'],
        }),
      );

      const wrapper = mountLayout();

      // Dashboard is accessible
      expect(wrapper.find('[data-testid="nav-item-dashboard"]').exists()).toBe(true);

      // Operations is visible with Calendar child
      expect(wrapper.find('[data-testid="nav-group-operations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-operations-calendar"]').exists()).toBe(true);

      // Students and Finance groups are completely hidden
      expect(wrapper.find('[data-testid="nav-group-students"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-finance"]').exists()).toBe(false);
    });

    it('renders only Students admission when user has only Admission View permission', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AdmissionOfficer'],
          permissions: ['Permissions.Students.Admission.View'],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-item-dashboard"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-group-students"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-students-admission"]').exists()).toBe(true);

      // Enrollment Desk is hidden because user lacks permission
      expect(wrapper.find('[data-testid="nav-item-students-enrollment"]').exists()).toBe(false);

      // Other groups are hidden
      expect(wrapper.find('[data-testid="nav-group-operations"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-finance"]').exists()).toBe(false);
    });

    it('renders only Dashboard when user has no permissions', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Guest'],
          permissions: [],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-item-dashboard"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-group-operations"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-students"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-finance"]').exists()).toBe(false);
    });

    it('renders Academics navigation group and Curriculum & Requirements item for user with permission', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Staff'],
          permissions: ['academics.curriculum.view'],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(true);
      const curriculumItem = wrapper.find('[data-testid="nav-item-academics-curriculum"]');
      expect(curriculumItem.exists()).toBe(true);
      expect(curriculumItem.text()).toContain('Curriculum & Requirements');
      expect(curriculumItem.attributes('href')).toBe('/academics/curriculum');
      expect(curriculumItem.find('i.pi.pi-book').exists()).toBe(true);
    });

    it('renders Academics navigation for roles AcademicCoordinator, AcademicAuthority, and Admin', () => {
      const authStore = useAuthStore();

      for (const role of ['AcademicCoordinator', 'AcademicAuthority', 'Admin']) {
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: [role],
            permissions: [],
          }),
        );

        const wrapper = mountLayout();
        expect(
          wrapper.find('[data-testid="nav-group-academics"]').exists(),
          `Expected nav-group-academics to exist for role ${role}`,
        ).toBe(true);
        expect(
          wrapper.find('[data-testid="nav-item-academics-curriculum"]').exists(),
          `Expected nav-item-academics-curriculum to exist for role ${role}`,
        ).toBe(true);
      }
    });

    it('renders expanded Academics navigation items (Participation, Sessions, Coursework) for AcademicCoordinator', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AcademicCoordinator'],
          permissions: [],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(true);

      const curriculumItem = wrapper.find('[data-testid="nav-item-academics-curriculum"]');
      expect(curriculumItem.exists()).toBe(true);
      expect(curriculumItem.attributes('href')).toBe('/academics/curriculum');

      const participationItem = wrapper.find('[data-testid="nav-item-academics-participation"]');
      expect(participationItem.exists()).toBe(true);
      expect(participationItem.text()).toContain('Participation & Rosters');
      expect(participationItem.attributes('href')).toBe('/academics/participation');

      const sessionsItem = wrapper.find('[data-testid="nav-item-academics-sessions"]');
      expect(sessionsItem.exists()).toBe(true);
      expect(sessionsItem.text()).toContain('Class Sessions');
      expect(sessionsItem.attributes('href')).toBe('/academics/sessions');

      const courseworkItem = wrapper.find('[data-testid="nav-item-academics-coursework"]');
      expect(courseworkItem.exists()).toBe(true);
      expect(courseworkItem.text()).toContain('Coursework');
      expect(courseworkItem.attributes('href')).toBe('/academics/coursework');
    });

    it('renders Teacher navigation showing Class Sessions and Coursework while hiding coordinator-only items and portal', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Teacher'],
          permissions: [],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-sessions"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-coursework"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-participation"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-portal"]').exists()).toBe(false);
    });

    it('renders Student Portal navigation with My Learning & Coursework for Student role and hides Academics', () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Student'],
          permissions: [],
        }),
      );

      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="nav-group-portal"]').exists()).toBe(true);
      const learningItem = wrapper.find('[data-testid="nav-item-portal-learning"]');
      expect(learningItem.exists()).toBe(true);
      expect(learningItem.text()).toContain('My Learning & Coursework');
      expect(learningItem.attributes('href')).toBe('/portal/learning');

      expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(false);
    });

    it('toggles group accordion in expanded sidebar mode', async () => {
      const authStore = useAuthStore();
      authStore.setSession('test-token', createTestUser());

      const wrapper = mountLayout();
      const operationsGroupBtn = wrapper.find('[data-testid="nav-group-operations"]');
      expect(wrapper.find('[data-testid="nav-item-operations-calendar"]').exists()).toBe(true);

      // Click group header to collapse
      await operationsGroupBtn.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="nav-item-operations-calendar"]').exists()).toBe(false);

      // Click again to expand
      await operationsGroupBtn.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="nav-item-operations-calendar"]').exists()).toBe(true);
    });
  });

  describe('Dynamic Breadcrumbs and Page Header Region', () => {
    it('renders dynamic breadcrumbs and title derived from current route', () => {
      const wrapper = mountLayout();

      const pageHeader = wrapper.find('[data-testid="layout-page-header"]');
      expect(pageHeader.exists()).toBe(true);

      const title = wrapper.find('[data-testid="layout-page-title"]');
      expect(title.text()).toBe('Dashboard');

      const breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');
      expect(breadcrumbs.exists()).toBe(true);
      expect(breadcrumbs.text()).toContain('Dashboard');
    });

    it('renders custom breadcrumbs when uiStore breadcrumbs are configured', async () => {
      const uiStore = useUIStore();
      uiStore.setBreadcrumbs([
        { label: 'Home', to: '/dashboard' },
        { label: 'Operations', to: '/operations' },
        { label: 'Academic Calendar', current: true },
      ]);

      const wrapper = mountLayout();
      const breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');

      expect(breadcrumbs.text()).toContain('Home');
      expect(breadcrumbs.text()).toContain('Operations');
      expect(breadcrumbs.text()).toContain('Academic Calendar');
    });

    it('resolves breadcrumbs correctly for Submodule 2 routes', async () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['SuperAdmin'],
        }),
      );

      // 1. Academics Participation
      await router.push('/academics/participation');
      let wrapper = mountLayout();
      let breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');
      expect(breadcrumbs.text()).toContain('Home');
      expect(breadcrumbs.text()).toContain('Academics');
      expect(breadcrumbs.text()).toContain('Participation & Rosters');

      // 2. Academics Sessions
      await router.push('/academics/sessions');
      wrapper = mountLayout();
      breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');
      expect(breadcrumbs.text()).toContain('Home');
      expect(breadcrumbs.text()).toContain('Academics');
      expect(breadcrumbs.text()).toContain('Class Sessions');

      // 3. Academics Coursework
      await router.push('/academics/coursework');
      wrapper = mountLayout();
      breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');
      expect(breadcrumbs.text()).toContain('Home');
      expect(breadcrumbs.text()).toContain('Academics');
      expect(breadcrumbs.text()).toContain('Coursework');

      // 4. Portal Learning
      await router.push('/portal/learning');
      wrapper = mountLayout();
      breadcrumbs = wrapper.find('[data-testid="layout-breadcrumbs"]');
      expect(breadcrumbs.text()).toContain('Home');
      expect(breadcrumbs.text()).toContain('Student Portal');
      expect(breadcrumbs.text()).toContain('My Learning & Coursework');
    });
  });

  describe('Main Content Region', () => {
    it('renders main container hosting RouterView', () => {
      const wrapper = mountLayout();
      const main = wrapper.find('[data-testid="main-content"]');
      expect(main.exists()).toBe(true);
    });
  });
});

describe('AuthLayout.vue', () => {
  it('renders centered distraction-free layout with branding and card container', () => {
    const router = createAppRouter({ history: createMemoryHistory() });
    const wrapper = mount(AuthLayout, {
      global: {
        plugins: [router],
      },
    });

    expect(wrapper.find('[data-testid="auth-layout"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Elsasa SIS');
    expect(wrapper.text()).toContain('Integrated School Information System');
    expect(wrapper.find('[data-testid="auth-card-container"]').exists()).toBe(true);
  });
});

describe('App.vue', () => {
  it('renders global Toast, ConfirmDialog, and RouterView', () => {
    setActivePinia(createPinia());
    const router = createAppRouter({ history: createMemoryHistory() });

    const wrapper = mount(App, {
      global: {
        plugins: [router, [PrimeVue, primevueConfig], ToastService, ConfirmationService],
      },
    });

    expect(wrapper.findComponent({ name: 'Toast' }).exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'ConfirmDialog' }).exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'RouterView' }).exists()).toBe(true);
  });
});
