// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import FacultyResourceWorkspaceView from '../views/FacultyResourceWorkspaceView.vue';
import * as facultyQueries from '../composables/useFacultyQueries';
import type { TeacherProfileDto } from '../api/teachingTypes';
import { ref, computed } from 'vue';

const mockPush = vi.fn();
const mockReplace = vi.fn();
let mockQuery: Record<string, string> = {};

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRoute: () => ({ query: mockQuery, params: {} }),
    useRouter: () => ({ push: mockPush, replace: mockReplace }),
  };
});

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
  department: 'Matematika',
  standing: 'active',
  effectiveFrom: '2026-07-01',
  effectiveTo: null,
  standingReason: 'Senior faculty in good standing',
  approvedBy: 'Kepala Sekolah',
  approvedDate: '2026-07-01',
  capabilities: [
    {
      id: 'cap-01',
      teacherProfileId: 'tch-01',
      capabilityTypeId: 'type-01',
      capabilityCode: 'MATH-SD',
      capabilityName: 'Matematika SD Kelas 4–6',
      applicableLevel: 'SD',
      deliveryMode: 'Tatap Muka',
      languageOfInstruction: 'Indonesian',
      status: 'active',
    },
  ],
  qualifications: [
    {
      id: 'qual-01',
      teacherProfileId: 'tch-01',
      qualificationTypeId: 'qtype-01',
      title: 'Sertifikat Pendidik (Serdik)',
      issuingAuthority: 'Kemendikbud',
      credentialIdentifier: 'SERDIK-2024-8899',
      issueDate: '2024-05-10',
      verificationState: 'verified',
      verifiedBy: 'Verification Officer',
      verifiedDate: '2024-05-15',
    },
  ],
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

describe('FacultyResourceWorkspaceView.vue', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    mockQuery = {};
    const pinia = createPinia();
    setActivePinia(pinia);

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

  function createWrapper() {
    return mount(FacultyResourceWorkspaceView, {
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: RouterLinkStub,
          FacultyQueueSidebar: {
            name: 'FacultyQueueSidebar',
            template: '<div data-testid="mock-queue-sidebar"><slot /></div>',
            props: ['selectedTeacherId', 'canRegister'],
            emits: ['select', 'register'],
          },
          RegisterTeacherModal: {
            name: 'RegisterTeacherModal',
            template: '<div v-if="visible" data-testid="mock-register-modal"><slot /></div>',
            props: ['visible'],
            emits: ['update:visible', 'registered'],
          },
          ChangeStandingModal: {
            name: 'ChangeStandingModal',
            template: '<div v-if="visible" data-testid="mock-standing-modal"><slot /></div>',
            props: ['visible', 'teacher'],
            emits: ['update:visible', 'changed'],
          },
          RecordCorrectionModal: {
            name: 'RecordCorrectionModal',
            template: '<div v-if="visible" data-testid="mock-correction-modal"><slot /></div>',
            props: ['visible', 'teacher', 'canAuthorize'],
            emits: ['update:visible', 'corrected'],
          },
          Select: true,
        },
      },
    });
  }

  it('renders the master-detail workbench container and empty prompt when no teacher is selected', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="faculty-resource-workspace-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="no-faculty-selected-state"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No Faculty Selected');
  });

  it('toggles sidebar collapsibility when toggle button is clicked', async () => {
    const wrapper = createWrapper();
    const toggleBtn = wrapper.find('[data-testid="toggle-sidebar-button"]');
    expect(toggleBtn.exists()).toBe(true);
    await toggleBtn.trigger('click');
    expect(wrapper.find('[data-testid="workspace-sidebar-pane"]').classes()).toContain('lg:hidden');
  });

  it('renders loaded teacher dossier, summary header, and tabs when teacher is selected', async () => {
    vi.spyOn(facultyQueries, 'useTeacherProfileQuery').mockReturnValue({
      data: computed(() => mockTeacherProfile),
      isLoading: computed(() => false),
      isError: computed(() => false),
      refetch: vi.fn(),
    } as any);

    mockQuery = { teacherId: 'tch-01' };
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="faculty-detail-container"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="header-teacher-name"]').text()).toContain('Budi Hartono, M.Pd.');
    expect(wrapper.find('[data-testid="header-teacher-number"]').text()).toBe('TCH-2026-0001');
    expect(wrapper.find('[data-testid="header-standing-badge"]').exists()).toBe(true);

    // Verify unverified warning banner (TD-05)
    expect(wrapper.find('[data-testid="unverified-warning-banner"]').exists()).toBe(true);

    // Verify the 5 tabs
    expect(wrapper.find('[data-testid="tab-profile"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-capabilities"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-qualifications"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-authorizations"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-audit"]').exists()).toBe(true);
  });

  it('displays loading state when fetching teacher dossier', async () => {
    vi.spyOn(facultyQueries, 'useTeacherProfileQuery').mockReturnValue({
      data: computed(() => null),
      isLoading: computed(() => true),
      isError: computed(() => false),
      refetch: vi.fn(),
    } as any);

    mockQuery = { teacherId: 'tch-01' };
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="detail-loading"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Loading faculty dossier...');
  });

  it('displays error state when dossier query fails', async () => {
    vi.spyOn(facultyQueries, 'useTeacherProfileQuery').mockReturnValue({
      data: computed(() => null),
      isLoading: computed(() => false),
      isError: computed(() => true),
      refetch: vi.fn(),
    } as any);

    mockQuery = { teacherId: 'tch-01' };
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="detail-error"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Unable to load faculty dossier');
  });

  it('selects a teacher from sidebar, synchronizes route query, and loads detailed dossier', async () => {
    vi.spyOn(facultyQueries, 'useTeacherProfileQuery').mockReturnValue({
      data: computed(() => mockTeacherProfile),
      isLoading: computed(() => false),
      isError: computed(() => false),
      refetch: vi.fn(),
    } as any);

    const wrapper = createWrapper();
    const sidebar = wrapper.findComponent({ name: 'FacultyQueueSidebar' });
    expect(sidebar.exists()).toBe(true);

    // Simulate sidebar emitting 'select' with teacher ID
    await sidebar.vm.$emit('select', 'tch-01');
    await flushPromises();

    // Verify router.replace was called with teacherId
    expect(mockReplace).toHaveBeenCalledWith({
      query: { teacherId: 'tch-01' },
    });
  });

  it('filters active teacher list when queue filter changes in sidebar', async () => {
    const mockList = [
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
        standing: 'pending_approval',
        hasUnverifiedWarning: true,
        activeAuthorizationsCount: 0,
      },
    ];

    vi.spyOn(facultyQueries, 'useTeacherProfilesQuery').mockImplementation((params: any) => {
      return {
        data: computed(() => {
          const filter = params.value?.queueFilter;
          const filtered = filter === 'pending_approval'
            ? mockList.filter(t => t.standing === 'pending_approval')
            : mockList;
          return {
            items: filtered,
            totalCount: filtered.length,
            page: 1,
            pageSize: 100,
          };
        }),
        isLoading: computed(() => false),
        isError: computed(() => false),
        refetch: vi.fn(),
      } as any;
    });

    const fullWrapper = mount(FacultyResourceWorkspaceView, {
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: RouterLinkStub,
          Select: true,
        },
      },
    });

    await flushPromises();

    // Initial state: All Faculty pill selected -> 2 cards rendered
    expect(fullWrapper.find('[data-testid="faculty-card-tch-01"]').exists()).toBe(true);
    expect(fullWrapper.find('[data-testid="faculty-card-tch-02"]').exists()).toBe(true);
    expect(fullWrapper.find('[data-testid="queue-count-badge"]').text()).toBe('2');

    // Click 'Pending Profile Approval' queue filter pill
    const pendingPill = fullWrapper.find('[data-testid="queue-pill-pending_approval"]');
    expect(pendingPill.exists()).toBe(true);
    await pendingPill.trigger('click');
    await flushPromises();

    // Only tch-02 (pending_approval) should be rendered
    expect(fullWrapper.find('[data-testid="faculty-card-tch-01"]').exists()).toBe(false);
    expect(fullWrapper.find('[data-testid="faculty-card-tch-02"]').exists()).toBe(true);
    expect(fullWrapper.find('[data-testid="queue-count-badge"]').text()).toBe('1');
  });

  it('triggers register teacher modal and selects newly registered teacher upon completion', async () => {
    const wrapper = createWrapper();
    const sidebar = wrapper.findComponent({ name: 'FacultyQueueSidebar' });

    // Trigger register from sidebar
    await sidebar.vm.$emit('register');
    await flushPromises();

    const registerModal = wrapper.findComponent({ name: 'RegisterTeacherModal' });
    expect(registerModal.props('visible')).toBe(true);

    // Simulate completion event
    await registerModal.vm.$emit('registered', {
      id: 'tch-99',
      teacherNumber: 'TCH-2026-0099',
      fullName: 'New Candidate',
      standing: 'pending_approval',
    });
    await flushPromises();

    expect(registerModal.props('visible')).toBe(false);
    expect(mockReplace).toHaveBeenCalledWith({
      query: { teacherId: 'tch-99' },
    });
  });

  it('opens change standing and record correction modals from teacher header actions', async () => {
    vi.spyOn(facultyQueries, 'useTeacherProfileQuery').mockReturnValue({
      data: computed(() => mockTeacherProfile),
      isLoading: computed(() => false),
      isError: computed(() => false),
      refetch: vi.fn(),
    } as any);

    mockQuery = { teacherId: 'tch-01' };
    const wrapper = createWrapper();
    await flushPromises();

    const banner = wrapper.findComponent({ name: 'TeacherHeaderBanner' });
    expect(banner.exists()).toBe(true);

    // Trigger Change Standing
    await banner.vm.$emit('change-standing');
    await flushPromises();
    const standingModal = wrapper.findComponent({ name: 'ChangeStandingModal' });
    expect(standingModal.props('visible')).toBe(true);

    // Trigger Record Correction
    await banner.vm.$emit('audit-correction');
    await flushPromises();
    const correctionModal = wrapper.findComponent({ name: 'RecordCorrectionModal' });
    expect(correctionModal.props('visible')).toBe(true);
  });
});
