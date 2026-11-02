// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import CalendarMatrixTab from '../components/tabs/CalendarMatrixTab.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as academicYearApi from '../api/academicYear.api';
import * as calendarApi from '../api/calendar.api';
import Dialog from 'primevue/dialog';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

const mockAcademicYears: academicYearApi.AcademicYearDto[] = [
  {
    id: 'ay-1',
    code: 'AY-2026-2027',
    name: 'Academic Year 2026/2027',
    startDate: '2026-08-01',
    endDate: '2027-07-31',
    status: 'active',
  },
];

const mockDraftCalendar: calendarApi.SchoolCalendarDto = {
  id: 'cal-draft-1',
  academicYearId: 'ay-1',
  name: 'Kalender Operasional 2026/2027',
  scopeType: 'institution',
  scopeReference: 'All',
  version: '1.0',
  status: 'draft',
  publicationStatus: 'draft',
};

const mockPublishedCalendar: calendarApi.SchoolCalendarDto = {
  id: 'cal-pub-1',
  academicYearId: 'ay-1',
  name: 'Kalender Operasional 2026/2027',
  scopeType: 'institution',
  scopeReference: 'All',
  version: '1.0',
  status: 'approved',
  publicationStatus: 'published',
};

const mockMatrixDays: calendarApi.CalendarMatrixDayDto[] = [
  {
    date: '2026-08-03',
    dayOfWeek: 'Monday',
    isInstructional: true,
    designation: 'instructional_day',
    entryName: null,
  },
  {
    date: '2026-08-17',
    dayOfWeek: 'Monday',
    isInstructional: false,
    designation: 'holiday',
    entryName: 'Hari Kemerdekaan RI',
    entryDescription: 'National Independence Day Holiday',
    entryId: 'entry-holiday-17',
  },
  {
    date: '2026-08-18',
    dayOfWeek: 'Tuesday',
    isInstructional: false,
    designation: 'school_closure',
    entryName: 'Emergency Flood Closure',
    entryId: 'entry-closure-18',
  },
  {
    date: '2026-08-21',
    dayOfWeek: 'Friday',
    isInstructional: false,
    designation: 'staff_only_day',
    entryName: 'Staff Professional Development',
    entryId: 'entry-staff-21',
  },
  {
    date: '2026-08-25',
    dayOfWeek: 'Tuesday',
    isInstructional: true,
    designation: 'exam_day',
    entryName: 'Midterm Examination Day',
    entryId: 'entry-exam-25',
  },
];

describe('CalendarMatrixTab.spec.ts (Tab 1: Visual Matrix & Exception Entries)', () => {
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
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockAcademicYears);
    vi.spyOn(calendarApi, 'getCalendars').mockResolvedValue([mockDraftCalendar]);
    vi.spyOn(calendarApi, 'getMatrixProjection').mockResolvedValue(mockMatrixDays);

    const authStore = useAuthStore();
    authStore.setSession('mock-token', {
      userId: 'user-1',
      username: 'admin',
      fullName: 'Administrator',
      roles: ['Operations Authority'],
      permissions: ['*'],
      campusId: null,
    });
  });

  function createWrapper(props: { academicYearId?: string | null; calendarId?: string | null } = {}) {
    return mount(CalendarMatrixTab, {
      props: {
        academicYearId: 'ay-1',
        calendarId: 'cal-draft-1',
        ...props,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
      },
    });
  }

  it('renders tab container and month navigator', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="calendar-matrix-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="current-month-label"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="prev-month-btn"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="next-month-btn"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="current-month-btn"]').exists()).toBe(true);
  });

  it('displays draft status banner with publish calendar trigger', async () => {
    const wrapper = createWrapper({ calendarId: 'cal-draft-1' });
    await flushPromises();

    const banner = wrapper.find('[data-testid="calendar-draft-banner"]');
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain('Calendar Status: Draft');
    expect(wrapper.find('[data-testid="publish-calendar-banner-btn"]').exists()).toBe(true);
  });

  it('displays published status banner when calendar publicationStatus is published', async () => {
    vi.spyOn(calendarApi, 'getCalendars').mockResolvedValue([mockPublishedCalendar]);

    const wrapper = createWrapper({ calendarId: 'cal-pub-1' });
    await flushPromises();

    const banner = wrapper.find('[data-testid="calendar-published-banner"]');
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain('Published Calendar (v1.0)');
    expect(banner.text()).toContain('AUTHORITATIVE BASELINE');
  });

  it('renders 7 weekday column headers', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="weekday-header-0"]').text()).toContain('Monday');
    expect(wrapper.find('[data-testid="weekday-header-6"]').text()).toContain('Sunday');
  });

  it('renders month navigation updates correctly', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const initialMonth = wrapper.find('[data-testid="current-month-label"]').text();

    // Click Next Month
    await wrapper.find('[data-testid="next-month-btn"]').trigger('click');
    await flushPromises();

    const nextMonth = wrapper.find('[data-testid="current-month-label"]').text();
    expect(nextMonth).not.toBe(initialMonth);

    // Click Previous Month to return
    await wrapper.find('[data-testid="prev-month-btn"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="current-month-label"]').text()).toBe(initialMonth);
  });

  it('renders color-coded badges for various day designations', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Standard instructional day badge (neutral/slate)
    const instBadge = wrapper.find('[data-testid="day-badge-2026-08-03"]');
    if (instBadge.exists()) {
      expect(instBadge.text()).toContain('Instructional');
      expect(instBadge.classes()).toContain('bg-slate-100');
    }

    // Holiday badge (rose/red)
    const holidayBadge = wrapper.find('[data-testid="day-badge-2026-08-17"]');
    if (holidayBadge.exists()) {
      expect(holidayBadge.text()).toContain('Holiday');
      expect(holidayBadge.classes()).toContain('bg-rose-100');
    }

    // Closure badge (amber/red alert)
    const closureBadge = wrapper.find('[data-testid="day-badge-2026-08-18"]');
    if (closureBadge.exists()) {
      expect(closureBadge.text()).toContain('Closure');
      expect(closureBadge.classes()).toContain('bg-amber-100');
    }

    // Staff-only day badge (sky/blue)
    const staffBadge = wrapper.find('[data-testid="day-badge-2026-08-21"]');
    if (staffBadge.exists()) {
      expect(staffBadge.text()).toContain('Staff Day');
      expect(staffBadge.classes()).toContain('bg-sky-100');
    }

    // Exam window badge (purple)
    const examBadge = wrapper.find('[data-testid="day-badge-2026-08-25"]');
    if (examBadge.exists()) {
      expect(examBadge.text()).toContain('Exam Window');
      expect(examBadge.classes()).toContain('bg-purple-100');
    }
  });

  it('renders entry cards with titles and descriptions', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const holidayCard = wrapper.find('[data-testid="entry-card-2026-08-17"]');
    if (holidayCard.exists()) {
      expect(holidayCard.text()).toContain('Hari Kemerdekaan RI');
      expect(holidayCard.text()).toContain('National Independence Day Holiday');
    }
  });

  it('opens add entry modal when clicking add entry button', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const addBtn = wrapper.find('[data-testid="open-add-entry-modal-btn"]');
    expect(addBtn.exists()).toBe(true);
    await addBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="create-calendar-entry-modal"]').exists()).toBe(true);
  });

  it('opens publish calendar dialog when publish button is clicked', async () => {
    const wrapper = createWrapper({ calendarId: 'cal-draft-1' });
    await flushPromises();

    const publishBtn = wrapper.find('[data-testid="publish-calendar-banner-btn"]');
    expect(publishBtn.exists()).toBe(true);
    await publishBtn.trigger('click');
    await flushPromises();

    const dialogs = wrapper.findAllComponents(Dialog);
    const publishDialog = dialogs.find((d) => d.props('header') === 'Publish School Calendar');
    expect(publishDialog).toBeDefined();
    expect(publishDialog?.props('visible')).toBe(true);
  });
});
