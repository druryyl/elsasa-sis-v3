// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { useAuthStore } from '@/app/stores/auth.store';
import { StudentPlacementTab, ChangePlacementModal } from '../components';
import * as recordsApi from '../api/records.api';
import type { StudentDetail } from '../types/records.types';

const mockMatchMedia = (query?: string) => ({
  matches: false,
  media: query || '',
  onchange: null,
  addListener: vi.fn(),
  removeListener: vi.fn(),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  dispatchEvent: vi.fn(),
});

// Mock window.matchMedia and globalThis.matchMedia for PrimeVue components in jsdom
beforeAll(() => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation(mockMatchMedia),
  });
  (globalThis as any).matchMedia = window.matchMedia;
});

const mockStudentWithPlacements: StudentDetail = {
  id: 'student-placement-1',
  studentNumber: 'STD-2026-0099',
  fullName: 'Aisyah Putri',
  campus: 'Kampus Utama',
  schoolLevel: 'Primary School',
  grade: 'Grade 3',
  academicYear: '2026/2027',
  currentStatus: 'active',
  enrollmentDate: '2026-07-01',
  createdAt: '2026-07-01T00:00:00Z',
  currentPlacement: {
    id: 'placement-active-1',
    campus: 'Kampus Utama',
    schoolLevel: 'Primary School',
    grade: 'Grade 3',
    academicYear: '2026/2027',
    effectiveDate: '2026-09-01',
    endDate: null,
    isCurrent: true,
    program: 'Bilingual Immersion',
    stream: 'Science Track',
    cohort: '2026-A',
    homeroom: 'Room 301',
    reason: 'Promoted to Grade 3',
    authorizedBy: 'Principal Sarah',
    decisionReference: 'DEC-2026-301',
  },
  placementHistory: [
    {
      id: 'placement-active-1',
      campus: 'Kampus Utama',
      schoolLevel: 'Primary School',
      grade: 'Grade 3',
      academicYear: '2026/2027',
      effectiveDate: '2026-09-01',
      endDate: null,
      isCurrent: true,
      program: 'Bilingual Immersion',
      stream: 'Science Track',
      cohort: '2026-A',
      homeroom: 'Room 301',
      reason: 'Promoted to Grade 3',
      authorizedBy: 'Principal Sarah',
      decisionReference: 'DEC-2026-301',
    },
    {
      id: 'placement-prior-1',
      campus: 'Kampus Utama',
      schoolLevel: 'Primary School',
      grade: 'Grade 2',
      academicYear: '2025/2026',
      effectiveDate: '2025-09-01',
      endDate: '2026-08-31',
      isCurrent: false,
      program: 'Standard',
      stream: null,
      cohort: '2025-A',
      homeroom: 'Room 201',
      reason: 'Initial enrollment placement',
      authorizedBy: 'Admin Admissions',
      decisionReference: 'ADM-2025-01',
    },
  ],
};

const mockStudentNoPlacements: StudentDetail = {
  id: 'student-no-placement',
  studentNumber: 'STD-2026-0000',
  fullName: 'Baru Masuk',
  campus: 'Kampus Utama',
  schoolLevel: 'Primary School',
  grade: 'Grade 1',
  academicYear: '2026/2027',
  currentStatus: 'enrolled',
  enrollmentDate: '2026-11-01',
  createdAt: '2026-11-01T00:00:00Z',
  currentPlacement: null,
  placementHistory: [],
};

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

describe('StudentPlacementComponents (P6-S14)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    const pinia = createPinia();
    setActivePinia(pinia);
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const getPlugins = () => [
    [VueQueryPlugin, { queryClient }],
    PrimeVue,
    ToastService,
  ];

  describe('StudentPlacementTab.vue', () => {
    it('renders current active placement card with all parameters', () => {
      const wrapper = mount(StudentPlacementTab, {
        props: {
          student: mockStudentWithPlacements,
          canEdit: true,
        },
        global: { plugins: getPlugins() },
      });

      expect(wrapper.find('[data-testid="student-placement-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="active-placement-card"]').exists()).toBe(true);

      expect(wrapper.find('[data-testid="active-campus"]').text()).toBe('Kampus Utama');
      expect(wrapper.find('[data-testid="active-level"]').text()).toBe('Primary School');
      expect(wrapper.find('[data-testid="active-grade"]').text()).toBe('Grade 3');
      expect(wrapper.find('[data-testid="active-academic-year"]').text()).toBe('2026/2027');
      expect(wrapper.find('[data-testid="active-program"]').text()).toBe('Bilingual Immersion');
      expect(wrapper.find('[data-testid="active-stream"]').text()).toBe('Science Track');
      expect(wrapper.find('[data-testid="active-cohort"]').text()).toBe('2026-A');
      expect(wrapper.find('[data-testid="active-homeroom"]').text()).toBe('Room 301');
      expect(wrapper.find('[data-testid="active-authorized-by"]').text()).toBe('Principal Sarah');
      expect(wrapper.find('[data-testid="active-decision-reference"]').text()).toBe('DEC-2026-301');
      expect(wrapper.find('[data-testid="active-reason"]').text()).toBe('Promoted to Grade 3');
    });

    it('enables Change Placement button when user has edit permission', async () => {
      const wrapper = mount(StudentPlacementTab, {
        props: {
          student: mockStudentWithPlacements,
          canEdit: true,
        },
        global: { plugins: getPlugins() },
      });

      const btn = wrapper.find('[data-testid="btn-change-placement"]');
      expect(btn.exists()).toBe(true);
      expect(btn.attributes('disabled')).toBeUndefined();

      await btn.trigger('click');
      expect(wrapper.emitted('changePlacement')).toHaveLength(1);
    });

    it('disables Change Placement button when user lacks edit permission', () => {
      const wrapper = mount(StudentPlacementTab, {
        props: {
          student: mockStudentWithPlacements,
          canEdit: false,
        },
        global: { plugins: getPlugins() },
      });

      const btn = wrapper.find('[data-testid="btn-change-placement"]');
      expect(btn.attributes('disabled')).toBeDefined();
    });

    it('renders historical placement list with effective and end dates, reason, authorizer, and reference', () => {
      const wrapper = mount(StudentPlacementTab, {
        props: {
          student: mockStudentWithPlacements,
          canEdit: true,
        },
        global: { plugins: getPlugins() },
      });

      expect(wrapper.find('[data-testid="placement-history-section"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="placement-history-card-0"]').exists()).toBe(true);

      const histCard = wrapper.find('[data-testid="placement-history-card-0"]');
      expect(histCard.text()).toContain('Kampus Utama • Primary School — Grade 2');
      expect(histCard.find('[data-testid="history-authorized-by"]').text()).toBe('Admin Admissions');
      expect(histCard.find('[data-testid="history-reason"]').text()).toContain('Initial enrollment placement');
      expect(histCard.find('[data-testid="history-decision-reference"]').text()).toContain('ADM-2025-01');
    });

    it('displays empty state when no placements exist', () => {
      const wrapper = mount(StudentPlacementTab, {
        props: {
          student: mockStudentNoPlacements,
          canEdit: true,
        },
        global: { plugins: getPlugins() },
      });

      expect(wrapper.find('[data-testid="empty-placement-state"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="active-placement-card"]').exists()).toBe(false);
    });
  });

  describe('ChangePlacementModal.vue', () => {
    it('pre-populates form fields with active placement values', async () => {
      const wrapper = mount(ChangePlacementModal, {
        props: {
          visible: true,
          student: mockStudentWithPlacements,
        },
        global: {
          plugins: getPlugins(),
          stubs: { Dialog: dialogStub },
        },
      });

      await flushPromises();

      const campusInput = wrapper.find('[data-testid="input-campus"]');
      expect((campusInput.element as HTMLInputElement).value).toBe('Kampus Utama');

      const gradeInput = wrapper.find('[data-testid="input-grade"]');
      expect((gradeInput.element as HTMLInputElement).value).toBe('Grade 3');

      const yearInput = wrapper.find('[data-testid="input-academic-year"]');
      expect((yearInput.element as HTMLInputElement).value).toBe('2026/2027');
    });

    it('submits mutation on valid form inputs and emits saved and update:visible', async () => {
      const changePlacementSpy = vi.spyOn(recordsApi, 'changeAdministrativePlacement').mockResolvedValueOnce({
        placementId: 'new-placement-123',
        studentId: 'student-placement-1',
        isCurrent: true,
      });

      const wrapper = mount(ChangePlacementModal, {
        props: {
          visible: true,
          student: mockStudentWithPlacements,
        },
        global: {
          plugins: getPlugins(),
          stubs: { Dialog: dialogStub },
        },
      });

      await flushPromises();

      // Update grade to Grade 4
      const gradeInput = wrapper.find('[data-testid="input-grade"]');
      await gradeInput.setValue('Grade 4');

      // Click submit
      const submitBtn = wrapper.find('[data-testid="btn-submit-placement"]');
      await submitBtn.trigger('click');
      await new Promise((r) => setTimeout(r, 50));
      await flushPromises();

      expect(changePlacementSpy).toHaveBeenCalledTimes(1);
      expect(changePlacementSpy).toHaveBeenCalledWith(
        'student-placement-1',
        expect.objectContaining({
          grade: 'Grade 4',
          campus: 'Kampus Utama',
        }),
      );

      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
      expect(wrapper.emitted('saved')).toBeTruthy();
    });

    it('rejects effective dates prior to current placement effective date', async () => {
      const changePlacementSpy = vi.spyOn(recordsApi, 'changeAdministrativePlacement');

      const wrapper = mount(ChangePlacementModal, {
        props: {
          visible: true,
          student: mockStudentWithPlacements,
        },
        global: {
          plugins: getPlugins(),
          stubs: { Dialog: dialogStub },
        },
      });

      await flushPromises();

      // Active placement effectiveDate is 2026-09-01. Set earlier date 2026-01-01.
      const effDateInput = wrapper.find('[data-testid="input-effective-date"]');
      await effDateInput.setValue('2026-01-01');

      const submitBtn = wrapper.find('[data-testid="btn-submit-placement"]');
      await submitBtn.trigger('click');
      await new Promise((r) => setTimeout(r, 50));
      await flushPromises();

      expect(changePlacementSpy).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Effective date cannot be prior to current placement effective date');
    });
  });
});
