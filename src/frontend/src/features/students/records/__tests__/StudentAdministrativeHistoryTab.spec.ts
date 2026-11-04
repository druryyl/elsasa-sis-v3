// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import StudentAdministrativeHistoryTab from '../components/StudentAdministrativeHistoryTab.vue';
import ChangeStatusModal from '../components/ChangeStatusModal.vue';
import * as recordsApi from '../api/records.api';
import type {
  GetStudentAdministrativeHistoryResponse,
  AdministrativeHistoryEntryDto,
} from '../types/records.types';

beforeAll(() => {
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
});

const mockHistoryEntries: AdministrativeHistoryEntryDto[] = [
  {
    id: 'entry-1',
    eventType: 'status_adjusted',
    eventDate: '2026-09-01',
    detail: "Status adjusted to 'active'. Cleared for attendance.",
    authorizedBy: 'Principal Sarah',
    decisionReference: 'SK-STATUS-2026-01',
    isCorrection: false,
    correctsEntryId: null,
    createdAt: '2026-09-01T08:30:00Z',
    createdBy: 'officer_jane',
  },
  {
    id: 'entry-2',
    eventType: 'placement_changed',
    eventDate: '2026-08-15',
    detail: 'Reassigned to Main Campus, Primary, Grade 2, Cohort: 2A',
    authorizedBy: 'Officer John',
    decisionReference: 'MEMO-PLC-2026-42',
    isCorrection: true,
    correctsEntryId: 'entry-old-01',
    createdAt: '2026-08-15T09:00:00Z',
    createdBy: 'admin_officer',
  },
  {
    id: 'entry-3',
    eventType: 'enrollment_completed',
    eventDate: '2026-07-01',
    detail: 'Official enrollment finalized. Payment verified.',
    authorizedBy: 'Admission Lead',
    decisionReference: 'ADM-2026-099',
    isCorrection: false,
    correctsEntryId: null,
    createdAt: '2026-07-01T07:00:00Z',
    createdBy: 'system_intake',
  },
];

describe('StudentAdministrativeHistoryTab.vue (P6-S15)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  function mountComponent(props = { studentId: 'student-100' }) {
    return mount(StudentAdministrativeHistoryTab, {
      props,
      global: {
        plugins: [
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
          ToastService,
        ],
      },
    });
  }

  it('renders chronological timeline and event details when data is loaded', async () => {
    vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValueOnce({
      studentId: 'student-100',
      entries: mockHistoryEntries,
    });

    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="student-administrative-history-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="history-timeline"]').exists()).toBe(true);

    const entry1 = wrapper.find('[data-testid="history-entry-entry-1"]');
    expect(entry1.exists()).toBe(true);
    expect(entry1.find('[data-testid="history-event-badge"]').text()).toContain('Status Adjusted');
    expect(entry1.find('[data-testid="history-event-date"]').text()).toContain('01 Sep 2026');
    expect(entry1.find('[data-testid="history-event-detail"]').text()).toContain("Status adjusted to 'active'");
    expect(entry1.find('[data-testid="history-authorized-by"]').text()).toContain('Principal Sarah');
    expect(entry1.find('[data-testid="history-decision-reference"]').text()).toContain('SK-STATUS-2026-01');

    const entry2 = wrapper.find('[data-testid="history-entry-entry-2"]');
    expect(entry2.exists()).toBe(true);
    expect(entry2.find('[data-testid="history-correction-badge"]').exists()).toBe(true);
    expect(entry2.find('[data-testid="history-correction-badge"]').text()).toContain('Correction');
  });

  it('filters entries when event filter category changes', async () => {
    vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValueOnce({
      studentId: 'student-100',
      entries: mockHistoryEntries,
    });

    const wrapper = mountComponent();
    await flushPromises();

    // Initially 3 items
    expect(wrapper.findAll('[data-testid^="history-entry-"]')).toHaveLength(3);

    // Filter to 'placement'
    const vm = wrapper.vm as any;
    vm.selectedFilter = 'placement';
    await wrapper.vm.$nextTick();

    const placementEntries = wrapper.findAll('[data-testid^="history-entry-"]');
    expect(placementEntries).toHaveLength(1);
    expect(placementEntries[0].find('[data-testid="history-event-badge"]').text()).toContain('Placement Changed');

    // Filter to 'intake'
    vm.selectedFilter = 'intake';
    await wrapper.vm.$nextTick();
    const intakeEntries = wrapper.findAll('[data-testid^="history-entry-"]');
    expect(intakeEntries).toHaveLength(1);
    expect(intakeEntries[0].find('[data-testid="history-event-badge"]').text()).toContain('Enrollment Completed');
  });

  it('renders empty state when history has no entries', async () => {
    vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValueOnce({
      studentId: 'student-100',
      entries: [],
    });

    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="history-empty"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="history-timeline"]').exists()).toBe(false);
  });
});

describe('ChangeStatusModal.vue (P6-S15)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const dialogStub = {
    template: `
      <div v-if="visible" class="dialog-stub" :data-testid="$attrs['data-testid']">
        <slot name="header" />
        <slot />
        <slot name="footer" />
      </div>
    `,
    props: ['visible'],
  };

  function mountModal(props: any) {
    return mount(ChangeStatusModal, {
      props,
      global: {
        plugins: [
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
          ToastService,
        ],
        stubs: {
          Dialog: dialogStub,
        },
      },
    });
  }

  it('renders modal dialog form fields with pre-filled status and today date', async () => {
    const wrapper = mountModal({
      visible: true,
      studentId: 'student-100',
      currentStatus: 'enrolled',
    });
    await flushPromises();

    expect(wrapper.find('[data-testid="change-status-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="status-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="effective-date-picker"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="reason-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="authorized-by-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="decision-reference-input"]').exists()).toBe(true);
  });

  it('submits changeStatusMutation on form submit and emits saved event', async () => {
    const changeStatusSpy = vi.spyOn(recordsApi, 'changeStudentStatus').mockResolvedValueOnce({
      statusId: 'status-new-1',
      studentId: 'student-100',
      status: 'active',
      isCurrent: true,
    });

    const wrapper = mountModal({
      visible: true,
      studentId: 'student-100',
      currentStatus: 'enrolled',
    });
    await flushPromises();

    const vm = wrapper.vm as any;
    vm.status = 'active';
    vm.effectiveDate = '2026-11-04';
    vm.reason = 'Cleared for academic classes.';
    vm.authorizedBy = 'Head of Admissions';
    vm.decisionReference = 'SK-ACT-2026-09';
    await wrapper.vm.$nextTick();

    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(changeStatusSpy).toHaveBeenCalledWith('student-100', {
      status: 'active',
      effectiveDate: '2026-11-04',
      reason: 'Cleared for academic classes.',
      authorizedBy: 'Head of Admissions',
      decisionReference: 'SK-ACT-2026-09',
    });

    expect(wrapper.emitted('saved')).toBeTruthy();
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
  });
});
