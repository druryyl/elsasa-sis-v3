// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import AttendanceCorrectionDrawer from '../components/AttendanceCorrectionDrawer.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as sessionsApi from '../api/sessions.api';
import type { AttendanceRecord, CorrectAttendanceResponse } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

const mockRecordWithCorrections: AttendanceRecord = {
  id: 'att-rec-1',
  learningActivityId: 'act-100',
  studentId: 'student-uuid-1',
  studentNumber: 'STD-001',
  studentName: 'Ali Reza',
  status: 'absent',
  recordedBy: 'teacher_jane',
  recordedDate: '2026-11-04',
  evidence: null,
  notes: 'Unexcused roll call',
  createdAt: '2026-11-04T08:00:00Z',
  createdBy: 'teacher_jane',
  corrections: [
    {
      id: 'corr-1',
      previousStatus: 'absent',
      newStatus: 'excused_absence',
      reason: 'Parent provided clinic note from Dr. Smith',
      correctedBy: 'admin_user',
      correctedDate: '2026-11-05',
      evidence: 'MedCert-9981',
      createdAt: '2026-11-05T10:00:00Z',
    },
    {
      id: 'corr-2',
      previousStatus: 'excused_absence',
      newStatus: 'present',
      reason: 'Student arrived late and completed oral check-in',
      correctedBy: 'coord_user',
      correctedDate: '2026-11-05',
      evidence: null,
      createdAt: '2026-11-05T11:00:00Z',
    },
  ],
};

const mockRecordNoCorrections: AttendanceRecord = {
  id: 'att-rec-2',
  learningActivityId: 'act-100',
  studentId: 'student-uuid-2',
  studentNumber: 'STD-002',
  studentName: 'Bima Sakti',
  status: 'present',
  recordedBy: 'teacher_jane',
  recordedDate: '2026-11-04',
  evidence: null,
  notes: null,
  createdAt: '2026-11-04T08:00:00Z',
  createdBy: 'teacher_jane',
  corrections: [],
};

const mockCorrectResponse: CorrectAttendanceResponse = {
  id: 'att-rec-1',
  learningActivityId: 'act-100',
  studentId: 'student-uuid-1',
  status: 'late',
  recordedBy: 'teacher_jane',
  recordedDate: '2026-11-04',
  evidence: 'BusDelay-Notice',
  notes: 'Updated via correction',
  latestCorrection: {
    id: 'corr-3',
    previousStatus: 'absent',
    newStatus: 'late',
    reason: 'School bus mechanical failure on route 4',
    correctedBy: 'authority',
    correctedDate: '2026-11-05',
    evidence: 'BusDelay-Notice',
    createdAt: '2026-11-05T12:00:00Z',
  },
};

describe('AttendanceCorrectionDrawer.spec.ts (UC-ACD-15)', () => {
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
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.spyOn(sessionsApi, 'correctAttendance').mockResolvedValue(mockCorrectResponse);

    const authStore = useAuthStore();
    authStore.setSession('token-coord', {
      userId: 'coord-1',
      username: 'coordinator',
      fullName: 'Coordinator User',
      roles: ['AcademicCoordinator'],
      permissions: ['academics.attendance.correct', 'academics.attendance.view'],
      campusId: null,
    });
  });

  function createWrapper(props = { visible: true, record: mockRecordWithCorrections }) {
    return mount(AttendanceCorrectionDrawer, {
      props,
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          Drawer: {
            props: ['visible'],
            emits: ['update:visible'],
            template: `
              <div v-if="visible" class="p-drawer" data-testid="p-drawer">
                <slot name="header" />
                <slot />
              </div>
            `,
          },
          Select: {
            props: ['modelValue', 'options'],
            emits: ['update:modelValue'],
            template: `
              <select
                :value="modelValue"
                data-testid="correction-status-select"
                @change="$emit('update:modelValue', $event.target.value)"
              >
                <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
              </select>
            `,
          },
          Textarea: {
            props: ['modelValue'],
            emits: ['update:modelValue'],
            template: '<textarea :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />',
          },
          InputText: {
            props: ['modelValue'],
            emits: ['update:modelValue'],
            template: '<input :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />',
          },
        },
      },
    });
  }

  it('renders drawer header, regulatory warning banner, and student record details', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Auditable Attendance Correction');
    expect(wrapper.text()).toContain('Grace Window Expired (Regulatory Audit Mode)');
    expect(wrapper.text()).toContain('Ali Reza');
    expect(wrapper.text()).toContain('STD-001');
    expect(wrapper.text()).toContain('Recorded:');
    expect(wrapper.text()).toContain('ABSENT');
    expect(wrapper.text()).toContain('By: teacher_jane');
  });

  it('verifies audit trail display when grace window is closed and corrections exist', async () => {
    const wrapper = createWrapper({ visible: true, record: mockRecordWithCorrections });
    await flushPromises();

    // Check count header
    expect(wrapper.text()).toContain('Past Correction Audit Trail (2)');

    // Verify first correction entry
    expect(wrapper.text()).toContain('Parent provided clinic note from Dr. Smith');
    expect(wrapper.text()).toContain('By: admin_user');
    expect(wrapper.text()).toContain('Ref: MedCert-9981');

    // Verify second correction entry
    expect(wrapper.text()).toContain('Student arrived late and completed oral check-in');
    expect(wrapper.text()).toContain('By: coord_user');
  });

  it('displays empty audit trail placeholder when no corrections exist', async () => {
    const wrapper = createWrapper({ visible: true, record: mockRecordNoCorrections });
    await flushPromises();

    expect(wrapper.text()).toContain('Past Correction Audit Trail (0)');
    expect(wrapper.text()).toContain('No prior corrections recorded. Original entry remains unmodified.');
  });

  it('enforces mandatory reason requirement (>= 5 chars) on form submission', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Try submitting with empty reason
    const form = wrapper.find('form');
    await form.trigger('submit');
    await flushPromises();

    // API should not be called with empty reason
    expect(sessionsApi.correctAttendance).not.toHaveBeenCalled();

    // Enter short reason (< 5 characters)
    const textarea = wrapper.find('textarea');
    expect(textarea.exists()).toBe(true);
    await textarea.setValue('Sick');
    await form.trigger('submit');
    await flushPromises();

    // Still should NOT have called API
    expect(sessionsApi.correctAttendance).not.toHaveBeenCalled();
  });

  it('submits valid correction (>= 5 chars reason) and emits corrected event', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Set new status to 'late'
    const select = wrapper.find('[data-testid="correction-status-select"]');
    await select.setValue('late');

    // Enter valid reason >= 5 characters
    const textarea = wrapper.find('textarea');
    await textarea.setValue('School bus mechanical failure on route 4');

    // Enter optional evidence
    const evidenceInput = wrapper.findAll('input').find((i) => i.attributes('placeholder')?.includes('MedCert'));
    if (evidenceInput) {
      await evidenceInput.setValue('BusDelay-Notice');
    }

    const form = wrapper.find('form');
    await form.trigger('submit');
    await flushPromises();
    await new Promise((r) => setTimeout(r, 20));
    await flushPromises();

    // API called with valid payload
    expect(sessionsApi.correctAttendance).toHaveBeenCalledWith(
      'att-rec-1',
      expect.objectContaining({
        newStatus: 'late',
        reason: 'School bus mechanical failure on route 4',
      }),
    );

    // Emitted 'corrected' event with response
    const correctedEvents = wrapper.emitted('corrected');
    expect(correctedEvents).toBeDefined();
    expect(correctedEvents![0][0]).toEqual(mockCorrectResponse);

    // Closed drawer
    expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
  });
});
