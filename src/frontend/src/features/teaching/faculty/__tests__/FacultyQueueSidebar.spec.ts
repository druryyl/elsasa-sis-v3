// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import FacultyQueueSidebar from '../components/FacultyQueueSidebar.vue';
import type { TeacherSummaryDto } from '../api/teachingTypes';

const mockTeachers: TeacherSummaryDto[] = [
  {
    id: 'tch-01',
    teacherNumber: 'TCH-2026-0001',
    fullName: 'Budi Hartono',
    displayName: 'Budi Hartono, M.Pd.',
    teachingRoleId: 'role-01',
    teachingRoleName: 'Guru Tetap',
    campusAffiliation: 'Kampus Utama',
    department: 'Matematika',
    standing: 'active',
    email: 'budi.hartono@elsasa.sch.id',
    phone: '081234567890',
    hasUnverifiedWarning: false,
    activeAuthorizationsCount: 2,
  },
  {
    id: 'tch-02',
    teacherNumber: 'TCH-2026-0002',
    fullName: 'Siti Nurhaliza',
    displayName: 'Siti Nurhaliza, S.Pd.',
    teachingRoleId: 'role-02',
    teachingRoleName: 'Guru Kontrak',
    campusAffiliation: 'Kampus Barat',
    department: 'Bahasa',
    standing: 'active',
    email: 'siti.nurhaliza@elsasa.sch.id',
    phone: '081298765432',
    hasUnverifiedWarning: true,
    activeAuthorizationsCount: 1,
  },
  {
    id: 'tch-03',
    teacherNumber: 'TCH-2026-0003',
    fullName: 'Hendra Setiawan',
    displayName: null,
    teachingRoleId: 'role-02',
    teachingRoleName: 'Guru Kontrak',
    campusAffiliation: 'Kampus Utama',
    department: 'Olahraga',
    standing: 'pending_approval',
    email: 'hendra.setiawan@example.com',
    phone: '081122334455',
    hasUnverifiedWarning: false,
    activeAuthorizationsCount: 0,
  },
];

describe('FacultyQueueSidebar.vue', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      window.ResizeObserver =
        window.ResizeObserver ||
        vi.fn().mockImplementation(() => ({
          observe: vi.fn(),
          unobserve: vi.fn(),
          disconnect: vi.fn(),
        }));
    }

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });
  });

  function createWrapper(props = {}) {
    return mount(FacultyQueueSidebar, {
      props: {
        teachers: mockTeachers,
        selectedTeacherId: 'tch-01',
        ...props,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          Select: true,
        },
      },
    });
  }

  it('renders the sidebar container and count badge', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="faculty-queue-sidebar"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="queue-count-badge"]').text()).toBe('3');
  });

  it('renders all 5 queue category filter pills', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="queue-pill-all"]').text()).toBe('All Faculty');
    expect(wrapper.find('[data-testid="queue-pill-pending_approval"]').text()).toBe('Pending Profile Approval');
    expect(wrapper.find('[data-testid="queue-pill-pending_verification"]').text()).toBe('Pending Verification');
    expect(wrapper.find('[data-testid="queue-pill-pending_authorization"]').text()).toBe('Pending Authorization');
    expect(wrapper.find('[data-testid="queue-pill-corrections"]').text()).toBe('Corrections');
  });

  it('switches queue category filter pill on click', async () => {
    const wrapper = createWrapper();
    const pill = wrapper.find('[data-testid="queue-pill-pending_approval"]');
    await pill.trigger('click');
    expect(pill.classes()).toContain('bg-primary-600');
  });

  it('renders teacher cards with proper meta attributes', () => {
    const wrapper = createWrapper();
    const firstCard = wrapper.find('[data-testid="faculty-card-tch-01"]');
    expect(firstCard.exists()).toBe(true);
    expect(firstCard.find('[data-testid="faculty-card-number"]').text()).toBe('TCH-2026-0001');
    expect(firstCard.find('[data-testid="faculty-card-name"]').text()).toContain('Budi Hartono, M.Pd.');
    expect(firstCard.find('[data-testid="faculty-card-role"]').text()).toBe('Guru Tetap');
    expect(firstCard.find('[data-testid="faculty-card-campus"]').text()).toContain('Kampus Utama');
  });

  it('emits select event with teacherId when a teacher card is clicked', async () => {
    const wrapper = createWrapper();
    const secondCard = wrapper.find('[data-testid="faculty-card-tch-02"]');
    await secondCard.trigger('click');
    expect(wrapper.emitted('select')).toBeTruthy();
    expect(wrapper.emitted('select')?.[0]).toEqual(['tch-02']);
  });

  it('renders unverified evidence warning badge when hasUnverifiedWarning is true', () => {
    const wrapper = createWrapper();
    const secondCard = wrapper.find('[data-testid="faculty-card-tch-02"]');
    const warningBadge = secondCard.find('[data-testid="faculty-card-unverified-warning"]');
    expect(warningBadge.exists()).toBe(true);
    expect(warningBadge.text()).toContain('Unverified Evidence');
  });

  it('emits register event when + Register Faculty button is clicked', async () => {
    const wrapper = createWrapper({ canRegister: true });
    const regButton = wrapper.find('[data-testid="register-faculty-button"]');
    expect(regButton.exists()).toBe(true);
    await regButton.trigger('click');
    expect(wrapper.emitted('register')).toBeTruthy();
  });

  it('renders empty state when teachers list is empty', () => {
    const wrapper = createWrapper({ teachers: [] });
    expect(wrapper.find('[data-testid="queue-empty"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No faculty found');
  });
});
