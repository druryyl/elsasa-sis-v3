// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import TeacherHeaderBanner from '../components/TeacherHeaderBanner.vue';
import type { TeacherProfileDto } from '../api/teachingTypes';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { useAuthStore } from '@/app/stores/auth.store';

const mockTeacherProfile: TeacherProfileDto = {
  id: 'tch-01',
  teacherNumber: 'TCH-2026-0001',
  fullName: 'Budi Hartono',
  displayName: 'Budi Hartono, M.Pd.',
  employeeRef: 'EMP-2026-012',
  nationalIdOrExternalRef: '3201010101900001',
  email: 'budi.hartono@elsasa.sch.id',
  phone: '081234567890',
  teachingRoleId: 'role-01',
  teachingRoleName: 'Guru Tetap',
  campusAffiliation: 'Kampus Utama',
  department: 'Matematika & IPA',
  standing: 'active',
  effectiveFrom: '2026-07-01',
  effectiveTo: null,
  standingReason: 'Senior faculty in good standing',
  approvedBy: 'Kepala Sekolah',
  approvedDate: '2026-07-01',
  capabilities: [],
  qualifications: [],
  authorizations: [
    {
      id: 'auth-01',
      authorizationNumber: 'AUTH-2026-0001',
      teacherProfileId: 'tch-01',
      scopeDescription: 'Matematika SD Tatap Muka Kampus Utama',
      campusScope: 'Kampus Utama',
      status: 'active',
      unverifiedCredentialWarning: true,
      effectiveStartDate: '2026-07-01',
      decisionAuthority: 'Kepala Sekolah',
      approvedDate: '2026-07-01',
    },
  ],
  recentCorrections: [],
};

import ToastService from 'primevue/toastservice';

describe('TeacherHeaderBanner.vue', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
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

      window.ResizeObserver =
        window.ResizeObserver ||
        vi.fn().mockImplementation(() => ({
          observe: vi.fn(),
          unobserve: vi.fn(),
          disconnect: vi.fn(),
        }));
    }

    const pinia = createPinia();
    setActivePinia(pinia);

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
  });

  function createWrapper(props: Partial<InstanceType<typeof TeacherHeaderBanner>['$props']> = {}) {
    return mount(TeacherHeaderBanner, {
      props: {
        teacher: mockTeacherProfile,
        ...props,
      },
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
      },
    });
  }

  it('renders professional display name, teacher number, legal full name, campus, department, and role badge', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="header-teacher-name"]').text()).toBe('Budi Hartono, M.Pd.');
    expect(wrapper.find('[data-testid="header-teacher-number"]').text()).toBe('TCH-2026-0001');
    expect(wrapper.find('[data-testid="header-legal-name"]').text()).toContain('Budi Hartono');
    expect(wrapper.find('[data-testid="header-campus"]').text()).toContain('Kampus Utama');
    expect(wrapper.find('[data-testid="header-department"]').text()).toContain('Matematika & IPA');
    expect(wrapper.find('[data-testid="header-role-badge"]').text()).toBe('Guru Tetap');
    expect(wrapper.find('[data-testid="header-standing-badge"]').text()).toContain('Active');
    expect(wrapper.find('[data-testid="header-email"]').text()).toContain('budi.hartono@elsasa.sch.id');
    expect(wrapper.find('[data-testid="header-phone"]').text()).toContain('081234567890');
  });

  it('renders unverified credential warning banner when authorizations have unverifiedCredentialWarning', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="unverified-warning-banner"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Provisional Authorization Warning');
  });

  it('respects explicit has_unverified_warning prop override', () => {
    const wrapper = createWrapper({
      has_unverified_warning: false,
    });
    expect(wrapper.find('[data-testid="unverified-warning-banner"]').exists()).toBe(false);

    const wrapperWithWarning = createWrapper({
      has_unverified_warning: true,
    });
    expect(wrapperWithWarning.find('[data-testid="unverified-warning-banner"]').exists()).toBe(true);
  });

  it('hides Approve Profile button when teacher standing is active', () => {
    const wrapper = createWrapper({
      teacher: { ...mockTeacherProfile, standing: 'active' },
    });
    expect(wrapper.find('[data-testid="header-approve-button"]').exists()).toBe(false);
  });

  it('shows Approve Profile button when teacher standing is pending_approval', () => {
    const wrapper = createWrapper({
      teacher: { ...mockTeacherProfile, standing: 'pending_approval' },
      canAuthorize: true,
    });
    const approveBtn = wrapper.find('[data-testid="header-approve-button"]');
    expect(approveBtn.exists()).toBe(true);
    expect(approveBtn.attributes('disabled')).toBeUndefined();
  });

  it('disables Approve Profile button when lacking authority permission', () => {
    const wrapper = createWrapper({
      teacher: { ...mockTeacherProfile, standing: 'pending_approval' },
      canAuthorize: false,
    });
    const approveBtn = wrapper.find('[data-testid="header-approve-button"]');
    expect(approveBtn.exists()).toBe(true);
    expect(approveBtn.attributes('disabled')).toBeDefined();
  });

  it('emits approve event when Approve Profile is clicked with authority', async () => {
    const wrapper = createWrapper({
      teacher: { ...mockTeacherProfile, standing: 'pending_approval' },
      canAuthorize: true,
    });

    const approveBtn = wrapper.find('[data-testid="header-approve-button"]');
    await approveBtn.trigger('click');

    expect(wrapper.emitted('approve')).toBeTruthy();
    expect(wrapper.emitted('approve')?.[0][0]).toEqual({
      ...mockTeacherProfile,
      standing: 'pending_approval',
    });
  });

  it('emits change-standing when Change Standing button is clicked', async () => {
    const wrapper = createWrapper({
      canAuthorize: true,
    });

    const changeStandingBtn = wrapper.find('[data-testid="header-change-standing-button"]');
    await changeStandingBtn.trigger('click');

    expect(wrapper.emitted('change-standing')).toBeTruthy();
    expect(wrapper.emitted('changeStanding')).toBeTruthy();
  });

  it('disables Change Standing button when lacking authority permission', () => {
    const wrapper = createWrapper({
      canAuthorize: false,
    });

    const changeStandingBtn = wrapper.find('[data-testid="header-change-standing-button"]');
    expect(changeStandingBtn.attributes('disabled')).toBeDefined();
  });

  it('emits audit-correction when Audit Correction button is clicked', async () => {
    const wrapper = createWrapper({
      canCorrect: true,
    });

    const auditBtn = wrapper.find('[data-testid="header-audit-correction-button"]');
    await auditBtn.trigger('click');

    expect(wrapper.emitted('audit-correction')).toBeTruthy();
    expect(wrapper.emitted('auditCorrection')).toBeTruthy();
  });

  describe('Standing Badge & Authority Action Matrix', () => {
    const standings: Array<{ standing: TeacherProfileDto['standing']; expectedText: string; expectedApproveVisible: boolean }> = [
      { standing: 'active', expectedText: 'Active', expectedApproveVisible: false },
      { standing: 'pending_approval', expectedText: 'Pending Approval', expectedApproveVisible: true },
      { standing: 'restricted', expectedText: 'Restricted', expectedApproveVisible: false },
      { standing: 'suspended', expectedText: 'Suspended', expectedApproveVisible: false },
      { standing: 'inactive', expectedText: 'Inactive', expectedApproveVisible: false },
    ];

    standings.forEach(({ standing, expectedText, expectedApproveVisible }) => {
      it(`renders correct badge and approve button visibility for standing: ${standing}`, () => {
        const wrapper = createWrapper({
          teacher: { ...mockTeacherProfile, standing },
          canAuthorize: true,
        });

        const badge = wrapper.find('[data-testid="header-standing-badge"]');
        expect(badge.exists()).toBe(true);
        expect(badge.text()).toContain(expectedText);

        const approveBtn = wrapper.find('[data-testid="header-approve-button"]');
        expect(approveBtn.exists()).toBe(expectedApproveVisible);
      });
    });

    it('enables authority actions when user has authority permission (ACT-13)', () => {
      const wrapper = createWrapper({
        teacher: { ...mockTeacherProfile, standing: 'pending_approval' },
        canAuthorize: true,
        canCorrect: true,
      });

      const approveBtn = wrapper.find('[data-testid="header-approve-button"]');
      expect(approveBtn.exists()).toBe(true);
      expect(approveBtn.attributes('disabled')).toBeUndefined();

      const changeStandingBtn = wrapper.find('[data-testid="header-change-standing-button"]');
      expect(changeStandingBtn.exists()).toBe(true);
      expect(changeStandingBtn.attributes('disabled')).toBeUndefined();

      const auditBtn = wrapper.find('[data-testid="header-audit-correction-button"]');
      expect(auditBtn.exists()).toBe(true);
      expect(auditBtn.attributes('disabled')).toBeUndefined();
    });

    it('disables authority actions when user lacks authority permission (ACT-12 without ACT-13)', () => {
      const wrapper = createWrapper({
        teacher: { ...mockTeacherProfile, standing: 'pending_approval' },
        canAuthorize: false,
        canCorrect: false,
      });

      const approveBtn = wrapper.find('[data-testid="header-approve-button"]');
      expect(approveBtn.exists()).toBe(true);
      expect(approveBtn.attributes('disabled')).toBeDefined();

      const changeStandingBtn = wrapper.find('[data-testid="header-change-standing-button"]');
      expect(changeStandingBtn.attributes('disabled')).toBeDefined();

      const auditBtn = wrapper.find('[data-testid="header-audit-correction-button"]');
      expect(auditBtn.attributes('disabled')).toBeDefined();
    });
  });
});
