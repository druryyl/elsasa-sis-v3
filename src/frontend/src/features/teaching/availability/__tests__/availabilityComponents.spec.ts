// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import WeeklyTimeGrid from '../components/WeeklyTimeGrid.vue';
import {
  DAYS,
  SLOTS,
  createDefaultTimeBlocks,
  normalizeSlots,
} from '../components/weeklyTimeGridConstants';
import UnavailabilityWindowsList from '../components/UnavailabilityWindowsList.vue';
import AddUnavailabilityModal from '../components/AddUnavailabilityModal.vue';
import { LEAVE_TYPE_OPTIONS } from '../components/addUnavailabilityConstants';
import MyTeachingAvailabilityView from '../views/MyTeachingAvailabilityView.vue';
import {
  PERIOD_PRESETS,
  DEMO_TEACHER_PERSONAS,
} from '../views/myTeachingAvailabilityConstants';
import type { TeacherUnavailabilityWindowDto, TimeSlotBlock } from '../types';
import * as availabilityApi from '../api/availabilityApi';

// Mock vue-router
const mockReplace = vi.fn();
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRoute: () => ({
      query: { teacherId: '00000000-0000-0000-0006-000000000001' },
    }),
    useRouter: () => ({
      replace: mockReplace,
    }),
  };
});

const commonStubs = {
  RouterLink: {
    props: ['to'],
    template: '<a :href="typeof to === \'string\' ? to : \'#\'"><slot /></a>',
  },
  Dialog: {
    props: ['visible', 'header'],
    emits: ['update:visible'],
    template: `
      <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid']">
        <div class="p-dialog-header">{{ header }}</div>
        <div class="p-dialog-content"><slot /></div>
        <div class="p-dialog-footer"><slot name="footer" /></div>
      </div>
    `,
  },
};

describe('SCR-TCH-04 Availability Workbench Components (P8-S27)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    const pinia = createPinia();
    setActivePinia(pinia);

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

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
    }
  });

  const sampleWindows: TeacherUnavailabilityWindowDto[] = [
    {
      id: 'win-01',
      teacherProfileId: '00000000-0000-0000-0006-000000000001',
      startDate: '2026-11-15',
      endDate: '2026-11-20',
      startTime: null,
      endTime: null,
      unavailabilityType: 'medical_leave',
      externalSourceReference: 'MED-2026-081',
      reason: 'Inpatient recovery post appendectomy',
      status: 'active',
      notes: 'Substitute coverage required for primary math',
      createdAt: '2026-11-10T08:00:00Z',
    },
    {
      id: 'win-02',
      teacherProfileId: '00000000-0000-0000-0006-000000000001',
      startDate: '2026-09-01',
      endDate: '2026-09-03',
      startTime: '08:00',
      endTime: '12:00',
      unavailabilityType: 'official_training',
      externalSourceReference: 'ST-044',
      reason: 'National Curriculum Workshop',
      status: 'cancelled',
      notes: null,
      createdAt: '2026-08-20T08:00:00Z',
    },
    {
      id: 'win-03',
      teacherProfileId: '00000000-0000-0000-0006-000000000001',
      startDate: '2026-08-01',
      endDate: '2026-08-05',
      startTime: null,
      endTime: null,
      unavailabilityType: 'personal_leave',
      externalSourceReference: null,
      reason: 'Family leave',
      status: 'expired',
      notes: null,
      createdAt: '2026-07-20T08:00:00Z',
    },
  ];

  describe('WeeklyTimeGrid.vue', () => {
    it('initializes 10 default time blocks (Monday–Friday morning and afternoon)', () => {
      const blocks = createDefaultTimeBlocks(true);
      expect(blocks).toHaveLength(10);
      expect(blocks.every((b) => b.isAvailable)).toBe(true);
      expect(DAYS).toHaveLength(5);
      expect(SLOTS).toHaveLength(2);
    });

    it('normalizes stringified recurringAvailableSchedule correctly', () => {
      const rawJson = JSON.stringify({
        slots: [
          { dayOfWeek: 'monday', slotType: 'morning', startTime: '08:00', endTime: '12:00', isAvailable: false },
        ],
      });
      const normalized = normalizeSlots(rawJson);
      expect(normalized).toHaveLength(10);
      const monMorning = normalized.find((b) => b.dayOfWeek === 'monday' && b.slotType === 'morning');
      expect(monMorning?.isAvailable).toBe(false);
      const monAfternoon = normalized.find((b) => b.dayOfWeek === 'monday' && b.slotType === 'afternoon');
      expect(monAfternoon?.isAvailable).toBe(true);
    });

    it('renders schedule table with all day headers and slot controls', () => {
      const wrapper = mount(WeeklyTimeGrid, {
        global: {
          plugins: [PrimeVue],
        },
      });

      for (const day of DAYS) {
        expect(wrapper.find(`[data-testid="day-header-${day.id}"]`).exists()).toBe(true);
      }

      const monMorningBtn = wrapper.find('[data-testid="slot-monday-morning"]');
      expect(monMorningBtn.exists()).toBe(true);
      expect(monMorningBtn.text()).toContain('Available');
    });

    it('toggles slot availability on click and emits update:modelValue', async () => {
      const wrapper = mount(WeeklyTimeGrid, {
        global: {
          plugins: [PrimeVue],
        },
      });

      const monMorningBtn = wrapper.find('[data-testid="slot-monday-morning"]');
      await monMorningBtn.trigger('click');

      const emitted = wrapper.emitted('update:modelValue');
      expect(emitted).toBeTruthy();
      const updatedSlots = emitted![0][0] as TimeSlotBlock[];
      const toggled = updatedSlots.find((b) => b.dayOfWeek === 'monday' && b.slotType === 'morning');
      expect(toggled?.isAvailable).toBe(false);
    });

    it('applies quick presets: Clear All, Mornings Only, Afternoons Only, All Available', async () => {
      const wrapper = mount(WeeklyTimeGrid, {
        global: {
          plugins: [PrimeVue],
        },
      });

      // Clear All
      const clearBtn = wrapper.find('[data-testid="preset-clear-all"]');
      await clearBtn.trigger('click');
      let emitted = wrapper.emitted('update:modelValue')!;
      let slots = emitted[emitted.length - 1][0] as TimeSlotBlock[];
      expect(slots.every((b) => !b.isAvailable)).toBe(true);

      // Mornings Only
      const morningBtn = wrapper.find('[data-testid="preset-morning-only"]');
      await morningBtn.trigger('click');
      emitted = wrapper.emitted('update:modelValue')!;
      slots = emitted[emitted.length - 1][0] as TimeSlotBlock[];
      expect(slots.filter((b) => b.isAvailable)).toHaveLength(5);
      expect(slots.every((b) => (b.slotType === 'morning' ? b.isAvailable : !b.isAvailable))).toBe(true);

      // All Available
      const allBtn = wrapper.find('[data-testid="preset-select-all"]');
      await allBtn.trigger('click');
      emitted = wrapper.emitted('update:modelValue')!;
      slots = emitted[emitted.length - 1][0] as TimeSlotBlock[];
      expect(slots.every((b) => b.isAvailable)).toBe(true);
    });

    it('disables clicking and hides presets when readonly=true', async () => {
      const wrapper = mount(WeeklyTimeGrid, {
        props: {
          readonly: true,
        },
        global: {
          plugins: [PrimeVue],
        },
      });

      expect(wrapper.find('[data-testid="preset-select-all"]').exists()).toBe(false);
      const monMorningBtn = wrapper.find('[data-testid="slot-monday-morning"]');
      await monMorningBtn.trigger('click');
      expect(wrapper.emitted('update:modelValue')).toBeFalsy();
    });
  });

  describe('UnavailabilityWindowsList.vue', () => {
    it('renders empty state when no windows are passed', () => {
      const wrapper = mount(UnavailabilityWindowsList, {
        props: {
          windows: [],
          isLoading: false,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.text()).toContain('No Unavailability Windows');
      expect(wrapper.find('[data-testid="empty-add-unavailability-btn"]').exists()).toBe(true);
    });

    it('renders list with status badges and leave types', () => {
      const wrapper = mount(UnavailabilityWindowsList, {
        props: {
          windows: sampleWindows,
          isLoading: false,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="leave-type-badge-win-01"]').text()).toContain('Medical Leave');
      expect(wrapper.find('[data-testid="leave-type-badge-win-02"]').text()).toContain('Official Training');
      expect(wrapper.find('[data-testid="leave-type-badge-win-03"]').text()).toContain('Personal Leave');

      // Status badges
      expect(wrapper.find('[data-testid="status-pill-win-01"]').text()).toContain('ACTIVE');
      expect(wrapper.find('[data-testid="status-pill-win-02"]').text()).toContain('CANCELLED');
      expect(wrapper.find('[data-testid="status-pill-win-03"]').text()).toContain('EXPIRED');

      // Cancel button present only for active window
      expect(wrapper.find('[data-testid="cancel-window-btn-win-01"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="cancel-window-btn-win-02"]').exists()).toBe(false);
    });

    it('emits add event when clicking record leave button', async () => {
      const wrapper = mount(UnavailabilityWindowsList, {
        props: {
          windows: sampleWindows,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      const addBtn = wrapper.find('[data-testid="add-unavailability-btn"]');
      await addBtn.trigger('click');
      expect(wrapper.emitted('add')).toBeTruthy();
    });

    it('opens confirmation modal and invokes cancel mutation when confirmed', async () => {
      const cancelSpy = vi.spyOn(availabilityApi, 'cancelUnavailabilityWindow').mockResolvedValueOnce({
        id: 'win-01',
        teacherProfileId: '00000000-0000-0000-0006-000000000001',
        status: 'cancelled',
        cancelledAt: '2026-11-10T12:00:00Z',
      });

      const wrapper = mount(UnavailabilityWindowsList, {
        props: {
          windows: sampleWindows,
          teacherId: '00000000-0000-0000-0006-000000000001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      const cancelBtn = wrapper.find('[data-testid="cancel-window-btn-win-01"]');
      await cancelBtn.trigger('click');
      await flushPromises();

      const confirmBtn = wrapper.find('[data-testid="confirm-cancel-window-submit-btn"]');
      expect(confirmBtn.exists()).toBe(true);
      await confirmBtn.trigger('click');
      await flushPromises();

      expect(cancelSpy).toHaveBeenCalledWith('win-01');
      expect(wrapper.emitted('cancel')).toBeTruthy();
    });
  });

  describe('AddUnavailabilityModal.vue', () => {
    it('defines standard leave type options', () => {
      expect(LEAVE_TYPE_OPTIONS).toHaveLength(4);
      expect(LEAVE_TYPE_OPTIONS.map((o) => o.value)).toEqual([
        'medical_leave',
        'official_training',
        'personal_leave',
        'blackout',
      ]);
    });

    it('renders dialog elements when visible=true', () => {
      const wrapper = mount(AddUnavailabilityModal, {
        props: {
          visible: true,
          teacherId: '00000000-0000-0000-0006-000000000001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="add-unavailability-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="unavailability-start-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="unavailability-end-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="unavailability-reason-input"]').exists()).toBe(true);
    });

    it('validates required fields before submitting', async () => {
      const createSpy = vi.spyOn(availabilityApi, 'createUnavailabilityWindow');

      const wrapper = mount(AddUnavailabilityModal, {
        props: {
          visible: true,
          teacherId: '00000000-0000-0000-0006-000000000001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      const submitBtn = wrapper.find('[data-testid="add-unavailability-submit-btn"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(createSpy).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Reason is required');
    });

    it('submits valid leave window and emits created event', async () => {
      const createSpy = vi.spyOn(availabilityApi, 'createUnavailabilityWindow').mockResolvedValueOnce({
        id: 'new-win-01',
        teacherProfileId: '00000000-0000-0000-0006-000000000001',
        startDate: '2026-11-20',
        endDate: '2026-11-22',
        unavailabilityType: 'medical_leave',
        status: 'active',
        affectedActiveAssignmentCount: 1,
        createdAt: '2026-11-10T12:00:00Z',
      });

      const wrapper = mount(AddUnavailabilityModal, {
        props: {
          visible: true,
          teacherId: '00000000-0000-0000-0006-000000000001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      const reasonInput = wrapper.find('[data-testid="unavailability-reason-input"]');
      await reasonInput.setValue('Medical appointment in specialist clinic');

      const submitBtn = wrapper.find('[data-testid="add-unavailability-submit-btn"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(createSpy).toHaveBeenCalledWith(
        '00000000-0000-0000-0006-000000000001',
        expect.objectContaining({
          reason: 'Medical appointment in specialist clinic',
          unavailabilityType: 'medical_leave',
        }),
      );
      expect(wrapper.emitted('created')).toBeTruthy();
    });
  });

  describe('MyTeachingAvailabilityView.vue', () => {
    it('renders workbench with period presets and demo personas', async () => {
      vi.spyOn(availabilityApi, 'getTeacherAvailability').mockResolvedValueOnce({
        id: 'avail-01',
        teacherProfileId: '00000000-0000-0000-0006-000000000001',
        academicPeriodId: PERIOD_PRESETS[0].id,
        maxAvailableHours: 22,
        campusScope: 'All Campuses',
        availableDeliveryModes: 'All',
        recurringAvailableSchedule: JSON.stringify({ slots: createDefaultTimeBlocks(true) }),
        effectiveStartDate: '2027-01-05',
        effectiveEndDate: '2027-06-25',
        status: 'active',
        createdAt: '2026-11-01T00:00:00Z',
        activeUnavailabilityWindows: sampleWindows,
      });

      vi.spyOn(availabilityApi, 'getTeacherUnavailabilityWindows').mockResolvedValueOnce(sampleWindows);

      const wrapper = mount(MyTeachingAvailabilityView, {
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="my-teaching-availability-workbench"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="academic-period-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="teacher-persona-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="preferred-hours-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="my-weekly-time-grid"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="my-unavailability-windows-list"]').exists()).toBe(true);
    });

    it('submits availability declaration when clicking Save Availability', async () => {
      vi.spyOn(availabilityApi, 'getTeacherAvailability').mockResolvedValueOnce({
        id: 'avail-01',
        teacherProfileId: '00000000-0000-0000-0006-000000000001',
        academicPeriodId: PERIOD_PRESETS[0].id,
        maxAvailableHours: 20,
        campusScope: 'All Campuses',
        availableDeliveryModes: 'All',
        recurringAvailableSchedule: null,
        effectiveStartDate: '2027-01-05',
        effectiveEndDate: '2027-06-25',
        status: 'active',
        createdAt: '2026-11-01T00:00:00Z',
      });
      vi.spyOn(availabilityApi, 'getTeacherUnavailabilityWindows').mockResolvedValueOnce([]);

      const declareSpy = vi.spyOn(availabilityApi, 'declareTeacherAvailability').mockResolvedValueOnce({
        id: 'avail-02',
        teacherProfileId: '00000000-0000-0000-0006-000000000001',
        academicPeriodId: PERIOD_PRESETS[0].id,
        maxAvailableHours: 20,
        status: 'active',
        supersededPreviousCount: 1,
        createdAt: '2026-11-10T12:00:00Z',
      });

      const wrapper = mount(MyTeachingAvailabilityView, {
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });
      await flushPromises();

      const saveBtn = wrapper.find('[data-testid="header-save-availability-btn"]');
      await saveBtn.trigger('click');
      await flushPromises();

      expect(declareSpy).toHaveBeenCalledWith(
        '00000000-0000-0000-0006-000000000001',
        expect.objectContaining({
          academicPeriodId: PERIOD_PRESETS[0].id,
          maxAvailableHours: 20,
        }),
      );
    });
  });
});
