// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import type * as VueRouter from 'vue-router';
import MyTeachingAvailabilityView from '@/features/teaching/availability/views/MyTeachingAvailabilityView.vue';
import * as availabilityApi from '@/features/teaching/availability/api/availabilityApi';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  PERIOD_PRESETS,
} from '@/features/teaching/availability/views/myTeachingAvailabilityConstants';
import { createDefaultTimeBlocks } from '@/features/teaching/availability/components/weeklyTimeGridConstants';
import type { TeacherAvailabilityDto, TeacherUnavailabilityWindowDto } from '@/features/teaching/availability/types';

// Mock vue-router
const mockRouteQuery = ref<Record<string, string>>({
  teacherId: '00000000-0000-0000-0006-000000000001',
});
const mockReplace = vi.fn().mockImplementation((target: { query?: Record<string, string> }) => {
  if (target.query) {
    mockRouteQuery.value = { ...target.query };
  }
});

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof VueRouter>();
  return {
    ...actual,
    useRoute: () => ({
      query: mockRouteQuery.value,
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
    reason: 'Rawat inap dan pemulihan pasca operasi.',
    emergencyCoverageRequired: true,
    status: 'active',
    notes: 'Surat dokter terlampir di HR.',
    createdAt: '2026-11-01T08:00:00Z',
  },
  {
    id: 'win-02',
    teacherProfileId: '00000000-0000-0000-0006-000000000001',
    startDate: '2026-12-01',
    endDate: '2026-12-05',
    startTime: '08:00',
    endTime: '12:00',
    unavailabilityType: 'official_training',
    externalSourceReference: 'ST-2026-44',
    reason: 'Pelatihan kurikulum nasional di dinas pendidikan.',
    emergencyCoverageRequired: false,
    status: 'active',
    notes: null,
    createdAt: '2026-11-05T09:30:00Z',
  },
];

const sampleAvailability: TeacherAvailabilityDto = {
  id: 'avail-01',
  teacherProfileId: '00000000-0000-0000-0006-000000000001',
  academicPeriodId: PERIOD_PRESETS[0].id,
  maxAvailableHours: 20,
  campusScope: 'All Campuses',
  availableDeliveryModes: 'All',
  recurringAvailableSchedule: JSON.stringify({ slots: createDefaultTimeBlocks(true) }),
  effectiveStartDate: '2027-01-05',
  effectiveEndDate: '2027-06-25',
  status: 'active',
  notes: 'Tersedia untuk jam mengajar reguler.',
  createdAt: '2026-11-01T00:00:00Z',
  activeUnavailabilityWindows: sampleWindows,
};

describe('MyTeachingAvailabilityView.vue (SCR-TCH-04 Workbench)', () => {
  let queryClient: QueryClient;

  function createWrapper() {
    return mount(MyTeachingAvailabilityView, {
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });
  }

  beforeEach(() => {
    setActivePinia(createPinia());
    const authStore = useAuthStore();
    authStore.setSession('mock-token', {
      userId: '00000000-0000-0000-0006-000000000001',
      username: 'budi.hartono',
      fullName: 'Budi Hartono, M.Pd.',
      roles: ['Teacher'],
      permissions: ['teaching:availability:declare'],
      campusId: null,
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
    mockRouteQuery.value = {
      teacherId: '00000000-0000-0000-0006-000000000001',
    };

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

    vi.spyOn(availabilityApi, 'getTeacherAvailability').mockResolvedValue(sampleAvailability);
    vi.spyOn(availabilityApi, 'getTeacherUnavailabilityWindows').mockResolvedValue(sampleWindows);
  });

  // ---------------------------------------------------------------------------
  // 1. Rendering Context & Banner Status
  // ---------------------------------------------------------------------------
  it('renders workbench with period presets, teacher switcher, and active status banner', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="my-teaching-availability-workbench"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="academic-period-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="teacher-persona-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="preferred-hours-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="effective-start-date-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="effective-end-date-input"]').exists()).toBe(true);

    // Active status banner is displayed
    const activeBanner = wrapper.find('[data-testid="submission-status-banner-active"]');
    expect(activeBanner.exists()).toBe(true);
    expect(activeBanner.text()).toContain('Active Recurring Availability Declared');
    expect(activeBanner.text()).toContain('20 hours/week');
  });

  // ---------------------------------------------------------------------------
  // 2. Weekly Recurring 10-Block Grid Selection & Preset Toggles
  // ---------------------------------------------------------------------------
  it('manages 10-block weekly schedule grid with preset toggles and individual slot selection', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const grid = wrapper.find('[data-testid="my-weekly-time-grid"]');
    expect(grid.exists()).toBe(true);

    // 1. Preset: Clear All -> 0 blocks available
    const clearBtn = wrapper.find('[data-testid="preset-clear-all"]');
    expect(clearBtn.exists()).toBe(true);
    await clearBtn.trigger('click');
    await flushPromises();
    expect(grid.text()).toContain('0 / 10 blocks');
    expect(grid.text()).toContain('~0 hrs/week capacity');

    // 2. Preset: Mornings Only -> 5 blocks available (~20 hrs)
    const morningBtn = wrapper.find('[data-testid="preset-morning-only"]');
    expect(morningBtn.exists()).toBe(true);
    await morningBtn.trigger('click');
    await flushPromises();
    expect(grid.text()).toContain('5 / 10 blocks');
    expect(grid.text()).toContain('~20 hrs/week capacity');

    // 3. Preset: Afternoons Only -> 5 blocks available (~20 hrs)
    const afternoonBtn = wrapper.find('[data-testid="preset-afternoon-only"]');
    expect(afternoonBtn.exists()).toBe(true);
    await afternoonBtn.trigger('click');
    await flushPromises();
    expect(grid.text()).toContain('5 / 10 blocks');
    expect(grid.text()).toContain('~20 hrs/week capacity');

    // 4. Preset: All Available -> 10 blocks available (~40 hrs)
    const allBtn = wrapper.find('[data-testid="preset-select-all"]');
    expect(allBtn.exists()).toBe(true);
    await allBtn.trigger('click');
    await flushPromises();
    expect(grid.text()).toContain('10 / 10 blocks');
    expect(grid.text()).toContain('~40 hrs/week capacity');

    // 5. Toggle individual cell (click Monday morning to unselect)
    const monMorning = wrapper.find('[data-testid="slot-monday-morning"]');
    expect(monMorning.exists()).toBe(true);
    await monMorning.trigger('click');
    await flushPromises();
    expect(grid.text()).toContain('9 / 10 blocks');
  });

  // ---------------------------------------------------------------------------
  // 3. Save Availability Declaration
  // ---------------------------------------------------------------------------
  it('submits updated availability declaration to API when clicking Save Availability', async () => {
    const declareSpy = vi.spyOn(availabilityApi, 'declareTeacherAvailability').mockResolvedValue({
      id: 'avail-02',
      teacherProfileId: '00000000-0000-0000-0006-000000000001',
      academicPeriodId: PERIOD_PRESETS[0].id,
      maxAvailableHours: 20,
      status: 'active',
      supersededPreviousCount: 1,
      createdAt: '2026-11-10T12:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    const saveBtn = wrapper.find('[data-testid="header-save-availability-btn"]');
    expect(saveBtn.exists()).toBe(true);

    await saveBtn.trigger('click');
    await flushPromises();

    expect(declareSpy).toHaveBeenCalledWith(
      '00000000-0000-0000-0006-000000000001',
      expect.objectContaining({
        academicPeriodId: PERIOD_PRESETS[0].id,
        maxAvailableHours: 20,
        recurringAvailableSchedule: expect.any(String),
      }),
    );
  });

  // ---------------------------------------------------------------------------
  // 4. Unavailability Windows DataTable & Cancel Action
  // ---------------------------------------------------------------------------
  it('renders unavailability windows list and handles cancellation confirmation', async () => {
    const cancelSpy = vi.spyOn(availabilityApi, 'cancelUnavailabilityWindow').mockResolvedValue({
      id: 'win-01',
      status: 'cancelled',
      cancelledAt: '2026-11-10T10:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    const windowsList = wrapper.find('[data-testid="my-unavailability-windows-list"]');
    expect(windowsList.exists()).toBe(true);

    // Displays both windows
    expect(wrapper.text()).toContain('Rawat inap dan pemulihan pasca operasi.');
    expect(wrapper.text()).toContain('Pelatihan kurikulum nasional di dinas pendidikan.');

    // Cancel action button for active window win-01
    const cancelBtn = wrapper.find('[data-testid="cancel-window-btn-win-01"]');
    expect(cancelBtn.exists()).toBe(true);

    await cancelBtn.trigger('click');
    await flushPromises();

    // Confirmation dialog is shown
    expect(wrapper.find('[data-testid="cancel-confirm-dialog"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Are you sure you want to cancel this leave window?');

    // Confirm cancellation
    const confirmBtn = wrapper.find('[data-testid="confirm-cancel-window-submit-btn"]');
    await confirmBtn.trigger('click');
    await flushPromises();

    expect(cancelSpy).toHaveBeenCalledWith('win-01');
  });

  // ---------------------------------------------------------------------------
  // 5. AddUnavailabilityModal Submission
  // ---------------------------------------------------------------------------
  it('opens and submits AddUnavailabilityModal to create temporary leave window', async () => {
    const createSpy = vi.spyOn(availabilityApi, 'createUnavailabilityWindow').mockResolvedValue({
      id: 'win-new',
      teacherProfileId: '00000000-0000-0000-0006-000000000001',
      startDate: '2026-12-10',
      endDate: '2026-12-12',
      unavailabilityType: 'personal_leave',
      status: 'active',
      createdAt: '2026-11-10T00:00:00Z',
    });

    const wrapper = createWrapper();
    await flushPromises();

    // Open Add Leave Window modal from header button
    const addLeaveBtn = wrapper.find('[data-testid="header-add-leave-btn"]');
    expect(addLeaveBtn.exists()).toBe(true);
    await addLeaveBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="workbench-add-unavailability-modal"]').exists()).toBe(true);

    // Fill form
    await wrapper.find('[data-testid="unavailability-start-date-input"]').setValue('2026-12-10');
    await wrapper.find('[data-testid="unavailability-end-date-input"]').setValue('2026-12-12');
    await wrapper.find('[data-testid="unavailability-reason-input"]').setValue('Family emergency bereavement leave.');
    await wrapper.find('[data-testid="unavailability-external-ref-input"]').setValue('DOC-LEAVE-2026-12');

    // Submit
    const submitBtn = wrapper.find('[data-testid="add-unavailability-submit-btn"]');
    await submitBtn.trigger('click');
    await flushPromises();

    expect(createSpy).toHaveBeenCalledWith(
      '00000000-0000-0000-0006-000000000001',
      expect.objectContaining({
        startDate: '2026-12-10',
        endDate: '2026-12-12',
        reason: 'Family emergency bereavement leave.',
        externalSourceReference: 'DOC-LEAVE-2026-12',
      }),
    );
  });
});
