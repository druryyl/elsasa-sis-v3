// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, beforeAll } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import StudentPlacementTab from '../components/StudentPlacementTab.vue';
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
      campus: 'Kampus Barat',
      schoolLevel: 'Primary School',
      grade: 'Grade 2',
      academicYear: '2025/2026',
      effectiveDate: '2025-09-01',
      endDate: '2026-08-31',
      isCurrent: false,
      program: 'Standard',
      stream: null,
      cohort: '2025-B',
      homeroom: 'Room 204',
      reason: 'Initial enrollment placement',
      authorizedBy: 'Admissions Officer Jane',
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

describe('StudentPlacementTab.spec.ts', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  const getPlugins = () => [PrimeVue, ToastService];

  it('asserts current placement card displays campus, grade, level, academic year, homeroom', () => {
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
    expect(wrapper.find('[data-testid="active-homeroom"]').text()).toBe('Room 301');
    expect(wrapper.find('[data-testid="active-cohort"]').text()).toBe('2026-A');
    expect(wrapper.find('[data-testid="active-program"]').text()).toBe('Bilingual Immersion');
    expect(wrapper.find('[data-testid="active-stream"]').text()).toBe('Science Track');
    expect(wrapper.find('[data-testid="active-authorized-by"]').text()).toBe('Principal Sarah');
    expect(wrapper.find('[data-testid="active-decision-reference"]').text()).toBe('DEC-2026-301');
    expect(wrapper.find('[data-testid="active-reason"]').text()).toBe('Promoted to Grade 3');
  });

  it('asserts historical placement list renders correct timeline items', () => {
    const wrapper = mount(StudentPlacementTab, {
      props: {
        student: mockStudentWithPlacements,
        canEdit: true,
      },
      global: { plugins: getPlugins() },
    });

    expect(wrapper.find('[data-testid="placement-history-section"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="prior-placements-list"]').exists()).toBe(true);

    const historyCard = wrapper.find('[data-testid="placement-history-card-0"]');
    expect(historyCard.exists()).toBe(true);
    expect(historyCard.text()).toContain('Kampus Barat • Primary School — Grade 2 (2025/2026)');
    expect(historyCard.find('[data-testid="history-authorized-by"]').text()).toBe('Admissions Officer Jane');
    expect(historyCard.find('[data-testid="history-reason"]').text()).toContain('Initial enrollment placement');
    expect(historyCard.find('[data-testid="history-decision-reference"]').text()).toContain('ADM-2025-01');
    expect(historyCard.text()).toContain('Room 204');
    expect(historyCard.text()).toContain('2025-B');
  });

  it('asserts clicking Change Placement emits event', async () => {
    const wrapper = mount(StudentPlacementTab, {
      props: {
        student: mockStudentWithPlacements,
        canEdit: true,
      },
      global: { plugins: getPlugins() },
    });

    const changeBtn = wrapper.find('[data-testid="btn-change-placement"]');
    expect(changeBtn.exists()).toBe(true);
    expect(changeBtn.attributes('disabled')).toBeUndefined();

    await changeBtn.trigger('click');
    expect(wrapper.emitted('changePlacement')).toHaveLength(1);
  });

  it('asserts empty state when student has no placements and clicking Assign Placement emits event', async () => {
    const wrapper = mount(StudentPlacementTab, {
      props: {
        student: mockStudentNoPlacements,
        canEdit: true,
      },
      global: { plugins: getPlugins() },
    });

    expect(wrapper.find('[data-testid="empty-placement-state"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="active-placement-card"]').exists()).toBe(false);

    const assignBtn = wrapper.find('[data-testid="btn-change-placement"]');
    expect(assignBtn.exists()).toBe(true);
    await assignBtn.trigger('click');
    expect(wrapper.emitted('changePlacement')).toHaveLength(1);
  });

  it('disables Change Placement button when canEdit is false', () => {
    const wrapper = mount(StudentPlacementTab, {
      props: {
        student: mockStudentWithPlacements,
        canEdit: false,
      },
      global: { plugins: getPlugins() },
    });

    const changeBtn = wrapper.find('[data-testid="btn-change-placement"]');
    expect(changeBtn.exists()).toBe(true);
    expect(changeBtn.attributes('disabled')).toBeDefined();
  });
});
