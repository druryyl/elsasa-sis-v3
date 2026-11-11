// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import {
  createMemoryHistory,
  createRouter,
  type RouteRecordRaw,
} from 'vue-router';
import { routes, setupNavigationGuards } from '@/app/router';
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

import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';

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

const EmptyRouteView = { template: '<div />' };

function stubRouteComponents(records: RouteRecordRaw[]): RouteRecordRaw[] {
  return records.map((record) => {
    const cloned: RouteRecordRaw = {
      ...record,
      ...(record.component && typeof record.component === 'function'
        ? { component: () => Promise.resolve(EmptyRouteView) }
        : {}),
      ...(record.children ? { children: stubRouteComponents(record.children) } : {}),
    } as RouteRecordRaw;
    return cloned;
  });
}

function createTestRouter() {
  const testRouter = createRouter({
    history: createMemoryHistory(),
    routes: stubRouteComponents(routes),
  });
  setupNavigationGuards(testRouter);
  return testRouter;
}

describe('AppLayout.vue', () => {
  let router: ReturnType<typeof createTestRouter>;
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
    router = createTestRouter();
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

    it('renders Teacher navigation showing Class Sessions, Coursework and Interventions while hiding coordinator-only and authority items and portal', () => {
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
      expect(wrapper.find('[data-testid="nav-item-academics-interventions"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-cohort-review"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-item-academics-participation"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-group-portal"]').exists()).toBe(false);
    });

    it('renders Submodule 4 navigation items with strict RBAC filtering (SCR-ACD-06, SCR-ACD-09, P8-S25)', () => {
      const authStore = useAuthStore();

      // Academic Coordinator: sees Interventions, does NOT see Cohort Review
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AcademicCoordinator'],
          permissions: [],
        }),
      );
      let wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-academics-interventions"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-cohort-review"]').exists()).toBe(false);

      // Academic Authority: sees BOTH Interventions and Cohort Review
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AcademicAuthority'],
          permissions: [],
        }),
      );
      wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-academics-interventions"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-cohort-review"]').exists()).toBe(true);

      // Admin: sees BOTH Interventions and Cohort Review
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Admin'],
          permissions: [],
        }),
      );
      wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-academics-interventions"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-academics-cohort-review"]').exists()).toBe(true);

      // Unauthorized role (AdmissionOfficer): neither Academic item exists
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AdmissionOfficer'],
          permissions: ['Permissions.Students.Admission.View'],
        }),
      );
      wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(false);
    });

    it('renders Learning Plans navigation item with RBAC filtering and icon (P7-S25)', () => {
      const authStore = useAuthStore();

      // Authorized roles: AcademicCoordinator, AcademicAuthority, Teacher, StudentDevelopmentOfficer, Admin
      for (const role of [
        'AcademicCoordinator',
        'AcademicAuthority',
        'Teacher',
        'StudentDevelopmentOfficer',
        'Admin',
      ]) {
        authStore.setSession(
          'test-token',
          createTestUser({
            roles: [role],
            permissions: [],
          }),
        );
        const wrapper = mountLayout();
        expect(wrapper.find('[data-testid="nav-group-academics"]').exists()).toBe(true);
        const slpItem = wrapper.find('[data-testid="nav-item-academics-learning-plans"]');
        expect(
          slpItem.exists(),
          `Expected nav-item-academics-learning-plans to exist for role ${role}`,
        ).toBe(true);
        expect(slpItem.text()).toContain('Learning Plans');
        expect(slpItem.attributes('href')).toBe('/academics/learning-plans');
        expect(slpItem.find('i.pi.pi-id-card').exists()).toBe(true);
      }

      // Explicit permission: academics:learning-plans:view
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Staff'],
          permissions: ['academics:learning-plans:view'],
        }),
      );
      let wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-academics-learning-plans"]').exists()).toBe(true);

      // Unauthorized role: AdmissionOfficer
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AdmissionOfficer'],
          permissions: ['Permissions.Students.Admission.View'],
        }),
      );
      wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-academics-learning-plans"]').exists()).toBe(false);
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

    it('renders Level 2 domain dashboard sidebar links with TD-05 parity and hides them from Guest, Student, and Parent', () => {
      const authStore = useAuthStore();

      // 1. AcademicCoordinator sees academics, teaching, operations dashboards (hides students, finance)
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['AcademicCoordinator', 'AcademicAuthority'],
          permissions: ['academics.participations.view'],
        }),
      );
      let wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-dashboard-academics"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-dashboard-teaching"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-dashboard-operations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-dashboard-students"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="nav-item-dashboard-finance"]').exists()).toBe(false);
      wrapper.unmount();

      // 2. Teacher sees academics and teaching dashboards
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['Teacher', 'AcademicStaff'],
          permissions: ['academics.participations.view'],
        }),
      );
      wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-dashboard-academics"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-dashboard-teaching"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-dashboard-operations"]').exists()).toBe(false);
      wrapper.unmount();

      // 3. OperationsStaff with Calendar.Manage sees operations dashboard
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['OperationsStaff'],
          permissions: ['Permissions.Operations.Calendar.Manage'],
        }),
      );
      wrapper = mountLayout();
      expect(wrapper.find('[data-testid="nav-item-dashboard-operations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="nav-item-dashboard-students"]').exists()).toBe(false);
      wrapper.unmount();

      // 4. Student and Parent must NOT see Academics or Finance dashboards even with portal view permissions
      for (const selfServiceUser of [
        {
          roles: ['Student'],
          permissions: ['academics.participations.view', 'Permissions.SelfService.Student'],
        },
        {
          roles: ['Parent'],
          permissions: [
            'academics.participations.view',
            'Permissions.Finance.Obligations.View',
            'Permissions.SelfService.Parent',
          ],
        },
        {
          roles: ['Guest'],
          permissions: [],
        },
      ]) {
        authStore.setSession('test-token', createTestUser(selfServiceUser));
        wrapper = mountLayout();
        expect(wrapper.find('[data-testid="nav-item-dashboard-students"]').exists()).toBe(false);
        expect(wrapper.find('[data-testid="nav-item-dashboard-academics"]').exists()).toBe(false);
        expect(wrapper.find('[data-testid="nav-item-dashboard-finance"]').exists()).toBe(false);
        expect(wrapper.find('[data-testid="nav-item-dashboard-teaching"]').exists()).toBe(false);
        expect(wrapper.find('[data-testid="nav-item-dashboard-operations"]').exists()).toBe(false);
        wrapper.unmount();
      }
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

  describe('Page Content Maximization & Route Navigation State', () => {
    it('omits redundant layout-page-header so routed views own PageHeader', () => {
      const wrapper = mountLayout();

      expect(wrapper.find('[data-testid="layout-page-header"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="layout-page-title"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="layout-breadcrumbs"]').exists()).toBe(false);
    });

    it('highlights active sidebar items across Submodule 2 routes', async () => {
      const authStore = useAuthStore();
      authStore.setSession(
        'test-token',
        createTestUser({
          roles: ['SuperAdmin'],
        }),
      );

      // Mount AppLayout once and verify active nav items across route transitions
      await router.push('/academics/participation');
      const wrapper = mountLayout();

      // 1. Academics Participation
      const activeParticipation = wrapper.find('[data-testid="nav-item-academics-participation"]');
      expect(activeParticipation.exists()).toBe(true);
      expect(activeParticipation.classes()).toContain('border-l-2');
      expect(activeParticipation.classes()).toContain('border-teal-600');

      // 2. Academics Sessions
      await router.push('/academics/sessions');
      await flushPromises();
      const activeSessions = wrapper.find('[data-testid="nav-item-academics-sessions"]');
      expect(activeSessions.exists()).toBe(true);
      expect(activeSessions.classes()).toContain('border-l-2');
      expect(activeSessions.classes()).toContain('border-teal-600');

      // 3. Academics Coursework
      await router.push('/academics/coursework');
      await flushPromises();
      const activeCoursework = wrapper.find('[data-testid="nav-item-academics-coursework"]');
      expect(activeCoursework.exists()).toBe(true);
      expect(activeCoursework.classes()).toContain('border-l-2');
      expect(activeCoursework.classes()).toContain('border-teal-600');

      // 4. Portal Learning
      await router.push('/portal/learning');
      await flushPromises();
      const activePortal = wrapper.find('[data-testid="nav-item-portal-learning"]');
      expect(activePortal.exists()).toBe(true);
      expect(activePortal.classes()).toContain('border-l-2');
      expect(activePortal.classes()).toContain('border-teal-600');

      // 5. Academic Interventions (P8-S25)
      await router.push('/academics/interventions');
      await flushPromises();
      const activeInterventions = wrapper.find('[data-testid="nav-item-academics-interventions"]');
      expect(activeInterventions.exists()).toBe(true);
      expect(activeInterventions.classes()).toContain('border-l-2');
      expect(activeInterventions.classes()).toContain('border-teal-600');

      // 6. Cohort Academic Review (P8-S25)
      await router.push('/academics/cohort-review');
      await flushPromises();
      const activeCohortReview = wrapper.find('[data-testid="nav-item-academics-cohort-review"]');
      expect(activeCohortReview.exists()).toBe(true);
      expect(activeCohortReview.classes()).toContain('border-l-2');
      expect(activeCohortReview.classes()).toContain('border-teal-600');

      // 7. Student Learning Plans (P7-S25)
      await router.push('/academics/learning-plans');
      await flushPromises();
      const activeSlp = wrapper.find('[data-testid="nav-item-academics-learning-plans"]');
      expect(activeSlp.exists()).toBe(true);
      expect(activeSlp.classes()).toContain('border-l-2');
      expect(activeSlp.classes()).toContain('border-teal-600');

      wrapper.unmount();
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
    const router = createTestRouter();
    const wrapper = mount(AuthLayout, {
      global: {
        plugins: [router],
        stubs: {
          RouterView: true,
        },
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
    const router = createTestRouter();

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const wrapper = mount(App, {
      global: {
        plugins: [
          router,
          [PrimeVue, primevueConfig],
          ToastService,
          ConfirmationService,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterView: true,
        },
      },
    });

    expect(wrapper.findComponent({ name: 'Toast' }).exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'ConfirmDialog' }).exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'RouterView' }).exists()).toBe(true);
  });
});
