// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import CalendarWorkspaceView from '../views/CalendarWorkspaceView.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../constants/permissions';
import * as academicYearApi from '../api/academicYear.api';
import * as calendarApi from '../api/calendar.api';
import * as closureApi from '../api/closure.api';
import * as amendmentApi from '../api/amendment.api';
import Dialog from 'primevue/dialog';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockAcademicYears: academicYearApi.AcademicYearDto[] = [
  {
    id: 'ay-active-1',
    code: 'AY-2025-2026',
    name: 'Tahun Pelajaran 2025/2026',
    startDate: '2025-07-01',
    endDate: '2026-06-30',
    status: 'active',
    isActive: true,
  },
  {
    id: 'ay-draft-2',
    code: 'AY-2026-2027',
    name: 'Tahun Pelajaran 2026/2027',
    startDate: '2026-07-01',
    endDate: '2027-06-30',
    status: 'draft',
    isActive: false,
  },
  {
    id: 'ay-approved-3',
    code: 'AY-2027-2028',
    name: 'Tahun Pelajaran 2027/2028',
    startDate: '2027-07-01',
    endDate: '2028-06-30',
    status: 'approved',
    isActive: false,
  },
];

const mockCalendar: calendarApi.SchoolCalendarDto = {
  id: 'cal-1',
  academicYearId: 'ay-active-1',
  name: 'Primary Academic Calendar',
  scopeType: 'institution',
  scopeReference: 'All',
  publicationStatus: 'published',
  status: 'published',
  version: '1.0',
  publishedBy: 'admin',
  publishedAt: '2025-07-01',
};

describe('CalendarWorkspaceView.spec.ts (SCR-OPS-01 Shell & Workflow Verification)', () => {
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

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      window.matchMedia = (query: string) =>
        ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }) as unknown as MediaQueryList;

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }

    vi.clearAllMocks();
    mockToastAdd.mockReset();

    const pinia = createPinia();
    setActivePinia(pinia);

    // Default SuperAdmin / full permissions actor
    const authStore = useAuthStore();
    authStore.setSession('mock-token', {
      userId: 'usr-1',
      username: 'admin',
      fullName: 'Operations Administrator',
      roles: ['OperationsAuthority'],
      permissions: ['*'],
      campusId: null,
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    // Default API mocks
    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockAcademicYears);
    vi.spyOn(calendarApi, 'getActiveCalendar').mockResolvedValue(mockCalendar);
    vi.spyOn(calendarApi, 'getCalendars').mockResolvedValue([mockCalendar]);
    vi.spyOn(closureApi, 'getClosures').mockResolvedValue([]);
    vi.spyOn(amendmentApi, 'getAmendments').mockResolvedValue([]);
    vi.spyOn(amendmentApi, 'getCorrections').mockResolvedValue([]);
  });

  afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
  });

  function createWrapper() {
    return mount(CalendarWorkspaceView, {
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
        },
      },
    });
  }

  describe('Header, Breadcrumbs & Institutional Context', () => {
    it('renders workspace view, header, and breadcrumbs', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="calendar-workspace-view"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="page-header"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="page-header-breadcrumbs"]').text()).toContain(
        'Calendar & Periods Workspace',
      );
      expect(wrapper.find('[data-testid="page-header-title"]').text()).toBe(
        'Calendar & Periods Workspace',
      );
    });

    it('automatically binds active year and displays status pills for year and calendar', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // Academic Year Status Pill (should be Active from mock)
      const yearStatusPill = wrapper.find('[data-testid="academic-year-status-pill"]');
      expect(yearStatusPill.exists()).toBe(true);
      expect(yearStatusPill.text()).toBe('Active');

      // Calendar Status Pill (should be Published v1.0 from mock)
      const calStatusPill = wrapper.find('[data-testid="calendar-status-pill"]');
      expect(calStatusPill.exists()).toBe(true);
      expect(calStatusPill.text()).toBe('Published v1.0');

      // Academic Year Select Dropdown
      const yearSelect = wrapper.find('[data-testid="academic-year-select"]');
      expect(yearSelect.exists()).toBe(true);
    });

    it('renders empty state when no academic years are configured', async () => {
      vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValueOnce([]);

      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="workspace-empty-state"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No Academic Years Configured');
    });
  });

  describe('Action Toolbar & Permission Enforcement', () => {
    it('renders primary action buttons when user has permissions', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="action-approve-year-btn"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="action-activate-year-btn"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="action-publish-calendar-btn"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="action-declare-closure-btn"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="action-apply-preset-btn"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="action-new-year-btn"]').exists()).toBe(true);
    });

    it('enforces button disabled states based on active year lifecycle status', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // Active year is currently selected (AY-2025-2026 status 'active')
      // Approve Year should be disabled because status !== 'draft'
      const approveBtn = wrapper.find<HTMLButtonElement>('[data-testid="action-approve-year-btn"]');
      expect(approveBtn.attributes('disabled')).toBeDefined();

      // Activate Year should be disabled because status !== 'approved'
      const activateBtn = wrapper.find<HTMLButtonElement>(
        '[data-testid="action-activate-year-btn"]',
      );
      expect(activateBtn.attributes('disabled')).toBeDefined();

      // Calendar is published, so Publish Calendar should be disabled
      const publishBtn = wrapper.find<HTMLButtonElement>(
        '[data-testid="action-publish-calendar-btn"]',
      );
      expect(publishBtn.attributes('disabled')).toBeDefined();

      // Declare Closure and Apply Preset should be enabled
      const closureBtn = wrapper.find<HTMLButtonElement>(
        '[data-testid="action-declare-closure-btn"]',
      );
      expect(closureBtn.attributes('disabled')).toBeUndefined();
    });

    it('hides restricted action buttons when user lacks permissions', async () => {
      const authStore = useAuthStore();
      authStore.setSession('mock-token', {
        userId: 'usr-2',
        username: 'viewer',
        fullName: 'Viewer Only',
        roles: ['Teacher'],
        permissions: [CALENDAR_PERMISSIONS.VIEW],
        campusId: null,
      });

      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="action-approve-year-btn"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="action-activate-year-btn"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="action-publish-calendar-btn"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="action-declare-closure-btn"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="action-apply-preset-btn"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="action-new-year-btn"]').exists()).toBe(false);
    });
  });

  describe('3-Tab Workspace Navigation', () => {
    it('renders all three tabs and defaults to Tab 1 (Matrix)', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="workspace-tabs"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-matrix"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-periods"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-closures"]').exists()).toBe(true);

      // Default active tab is matrix
      expect(wrapper.find('[data-testid="calendar-matrix-tab"]').exists()).toBe(true);
    });

    it('switches to Periods Hierarchy and Closures tabs', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // Switch to periods tab
      await wrapper.find('[data-testid="tab-periods"]').trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="periods-tree-tab"]').exists()).toBe(true);

      // Switch to closures tab
      await wrapper.find('[data-testid="tab-closures"]').trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="closures-amendments-tab"]').exists()).toBe(true);
    });
  });

  describe('Ephemeral Modal State & Triggers', () => {
    it('opens create academic year dialog on New Year click', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      await wrapper.find('[data-testid="action-new-year-btn"]').trigger('click');
      await flushPromises();

      const dialogs = wrapper.findAllComponents(Dialog);
      const createDialog = dialogs.find((d) => d.props('header') === 'Create Academic Year');
      expect(createDialog).toBeDefined();
      expect(createDialog?.props('visible')).toBe(true);
    });

    it('opens declare closure dialog on Declare Closure click', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      await wrapper.find('[data-testid="action-declare-closure-btn"]').trigger('click');
      await flushPromises();

      const dialogs = wrapper.findAllComponents(Dialog);
      const closureDialog = dialogs.find((d) => d.props('header') === 'Declare Campus Closure');
      expect(closureDialog).toBeDefined();
      expect(closureDialog?.props('visible')).toBe(true);
    });

    it('opens apply preset dialog on Apply Preset click', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      await wrapper.find('[data-testid="action-apply-preset-btn"]').trigger('click');
      await flushPromises();

      const dialogs = wrapper.findAllComponents(Dialog);
      const presetDialog = dialogs.find(
        (d) => d.props('header') === 'Apply Standard Institutional Preset',
      );
      expect(presetDialog).toBeDefined();
      expect(presetDialog?.props('visible')).toBe(true);
    });

    it('opens approve dialog when draft year is selected and Approve clicked', async () => {
      // Mock with draft year as selected
      vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValueOnce([mockAcademicYears[1]!]);

      const wrapper = createWrapper();
      await flushPromises();

      const approveBtn = wrapper.find('[data-testid="action-approve-year-btn"]');
      expect(approveBtn.attributes('disabled')).toBeUndefined();

      await approveBtn.trigger('click');
      await flushPromises();

      const dialogs = wrapper.findAllComponents(Dialog);
      const approveDialog = dialogs.find((d) => d.props('header') === 'Approve Academic Year');
      expect(approveDialog).toBeDefined();
      expect(approveDialog?.props('visible')).toBe(true);
    });

    it('opens activate dialog when approved year is selected and Activate clicked', async () => {
      // Mock with approved year as selected
      vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValueOnce([mockAcademicYears[2]!]);

      const wrapper = createWrapper();
      await flushPromises();

      const activateBtn = wrapper.find('[data-testid="action-activate-year-btn"]');
      expect(activateBtn.attributes('disabled')).toBeUndefined();

      await activateBtn.trigger('click');
      await flushPromises();

      const dialogs = wrapper.findAllComponents(Dialog);
      const activateDialog = dialogs.find((d) => d.props('header') === 'Activate Academic Year');
      expect(activateDialog).toBeDefined();
      expect(activateDialog?.props('visible')).toBe(true);
    });

    it('opens publish calendar dialog when draft calendar exists and Publish clicked', async () => {
      const draftCal: calendarApi.SchoolCalendarDto = {
        ...mockCalendar,
        publicationStatus: 'draft',
      };
      vi.spyOn(calendarApi, 'getActiveCalendar').mockResolvedValueOnce(draftCal);
      vi.spyOn(calendarApi, 'getCalendars').mockResolvedValueOnce([draftCal]);

      const wrapper = createWrapper();
      await flushPromises();

      const publishBtn = wrapper.find('[data-testid="action-publish-calendar-btn"]');
      expect(publishBtn.attributes('disabled')).toBeUndefined();

      await publishBtn.trigger('click');
      await flushPromises();

      const dialogs = wrapper.findAllComponents(Dialog);
      const publishDialog = dialogs.find((d) => d.props('header') === 'Publish Calendar');
      expect(publishDialog).toBeDefined();
      expect(publishDialog?.props('visible')).toBe(true);
    });
  });
});
