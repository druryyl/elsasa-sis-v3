// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import { ref, computed } from 'vue';

import FacultyResourceWorkspaceView from '../views/FacultyResourceWorkspaceView.vue';
import AuthorizationsSupervisionTab from '../components/AuthorizationsSupervisionTab.vue';
import AddCapabilityModal from '../components/AddCapabilityModal.vue';
import SubmitEvidenceModal from '../components/SubmitEvidenceModal.vue';
import GrantAuthorizationModal from '../components/GrantAuthorizationModal.vue';
import AddSupervisionModal from '../components/AddSupervisionModal.vue';
import ResolveSupervisionModal from '../components/ResolveSupervisionModal.vue';
import ChangeAuthorizationStatusModal from '../components/ChangeAuthorizationStatusModal.vue';
import ChangeStandingModal from '../components/ChangeStandingModal.vue';
import RecordCorrectionModal from '../components/RecordCorrectionModal.vue';

import * as facultyQueries from '../composables/useFacultyQueries';
import type {
  TeacherProfileDto,
  TeachingAuthorizationDto,
  SupervisionRequirementDto,
  QualificationEvidenceDto,
} from '../api/teachingTypes';

const mockSupervision: SupervisionRequirementDto = {
  id: 'sup-001',
  authorizationId: 'auth-002',
  supervisingTeacherId: 'tch-002',
  supervisingTeacherName: 'Budi Hartono, M.Pd.',
  supervisionType: 'co_teaching',
  description: 'Co-teaching twice weekly with senior mentor.',
  startDate: '2026-07-01',
  endDate: '2026-12-31',
  status: 'active',
  resolutionNotes: null,
};

const mockAuthorizations: TeachingAuthorizationDto[] = [
  {
    id: 'auth-001',
    authorizationNumber: 'AUTH-2026-001',
    teacherProfileId: 'tch-001',
    scopeDescription: 'Matematika SD Kelas 4–6 Tatap Muka',
    campusScope: 'Kampus Utama',
    programScope: 'Primary School',
    levelScope: 'Kelas 4-6',
    deliveryModeScope: 'in_person',
    status: 'active',
    unverifiedCredentialWarning: false,
    effectiveStartDate: '2026-07-01',
    effectiveEndDate: '2027-06-30',
    decisionAuthority: 'Kepala Sekolah',
    approvedDate: '2026-06-25',
    linkedEvidenceIds: ['qual-001'],
    supervisionRequirements: [],
  },
  {
    id: 'auth-002',
    authorizationNumber: 'AUTH-2026-002',
    teacherProfileId: 'tch-001',
    scopeDescription: 'Bahasa Inggris SMP dengan Supervisi Senior',
    campusScope: 'Kampus Barat',
    programScope: 'Junior High',
    levelScope: 'Kelas 7-9',
    deliveryModeScope: 'hybrid',
    status: 'authorized_under_supervision',
    unverifiedCredentialWarning: true,
    effectiveStartDate: '2026-07-01',
    effectiveEndDate: null,
    decisionAuthority: 'Kepala Sekolah',
    approvedDate: '2026-06-28',
    linkedEvidenceIds: ['qual-002'],
    supervisionRequirements: [mockSupervision],
  },
];

const mockQualifications: QualificationEvidenceDto[] = [
  {
    id: 'qual-001',
    teacherProfileId: 'tch-001',
    qualificationTypeId: 'qtype-01',
    title: 'Sertifikat Pendidik Matematika',
    issuingAuthority: 'Kemendikbud',
    credentialIdentifier: 'SERDIK-2024-9988',
    issueDate: '2024-01-01',
    verificationState: 'verified',
  },
  {
    id: 'qual-002',
    teacherProfileId: 'tch-001',
    qualificationTypeId: 'qtype-02',
    title: 'S1 Sastra Inggris',
    issuingAuthority: 'Universitas Indonesia',
    credentialIdentifier: 'IJAZAH-2025-001',
    issueDate: '2025-06-01',
    verificationState: 'submitted',
  },
];

const mockTeacherProfile: TeacherProfileDto = {
  id: 'tch-001',
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
  capabilities: [
    {
      id: 'cap-001',
      teacherProfileId: 'tch-001',
      capabilityTypeId: 'type-01',
      capabilityCode: 'MATH-SD',
      capabilityName: 'Matematika SD',
      applicableLevel: 'SD',
      deliveryMode: 'Tatap Muka',
      languageOfInstruction: 'Indonesian',
      status: 'active',
    },
  ],
  qualifications: mockQualifications,
  authorizations: mockAuthorizations,
  recentCorrections: [
    {
      id: 'corr-001',
      correctionNumber: 'CORR-2026-0001',
      targetTableName: 'teacher_profiles',
      targetRecordId: 'tch-001',
      fieldName: 'display_name',
      originalValue: 'Budi Hartono',
      correctedValue: 'Budi Hartono, M.Pd.',
      reasonForCorrection: 'Academic degree update',
      downstreamImpactReviewed: false,
      status: 'applied',
      requestedBy: 'Coordinator',
      requestedDate: '2026-07-02',
      approvedBy: 'Kepala Sekolah',
      approvedDate: '2026-07-02',
    },
  ],
};

const commonStubs = {
  Dialog: {
    props: ['visible', 'header', 'appendTo'],
    emits: ['update:visible', 'hide'],
    template: `
      <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid'] || 'dialog-root'">
        <slot name="header" />
        <slot />
        <slot name="footer" />
      </div>
    `,
  },
  Select: {
    props: ['modelValue', 'options'],
    emits: ['update:modelValue'],
    template: `
      <select
        :value="modelValue"
        @change="$emit('update:modelValue', $event.target.value)"
      >
        <option v-for="opt in options" :key="opt.value || opt" :value="opt.value || opt">
          {{ opt.label || opt }}
        </option>
      </select>
    `,
  },
};

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

describe('SCR-TCH-01 Tabs & Modal Workflows (TabsAndModals.spec.ts)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    mockQuery = {};
    const pinia = createPinia();
    setActivePinia(pinia);

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

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.spyOn(facultyQueries, 'useTeacherProfileQuery').mockReturnValue({
      data: computed(() => mockTeacherProfile),
      isLoading: computed(() => false),
      isError: computed(() => false),
      refetch: vi.fn(),
    } as any);
  });

  function createWorkspaceWrapper() {
    mockQuery = { teacherId: 'tch-001' };
    return mount(FacultyResourceWorkspaceView, {
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: RouterLinkStub,
          Select: true,
          FacultyQueueSidebar: {
            name: 'FacultyQueueSidebar',
            template: '<div data-testid="mock-queue-sidebar"><slot /></div>',
            props: ['selectedTeacherId', 'canRegister'],
            emits: ['select', 'register'],
          },
        },
      },
    });
  }

  // ---------------------------------------------------------------------------
  // 1. Switching Between the 5 Tabs
  // ---------------------------------------------------------------------------
  describe('Tab Switching in Workspace View', () => {
    it('renders all 5 tabs and switches active tab panel on click', async () => {
      const wrapper = createWorkspaceWrapper();
      await flushPromises();

      // Verify all 5 tab buttons exist
      const tabProfile = wrapper.find('[data-testid="tab-profile"]');
      const tabCapabilities = wrapper.find('[data-testid="tab-capabilities"]');
      const tabQualifications = wrapper.find('[data-testid="tab-qualifications"]');
      const tabAuthorizations = wrapper.find('[data-testid="tab-authorizations"]');
      const tabAudit = wrapper.find('[data-testid="tab-audit"]');

      expect(tabProfile.exists()).toBe(true);
      expect(tabCapabilities.exists()).toBe(true);
      expect(tabQualifications.exists()).toBe(true);
      expect(tabAuthorizations.exists()).toBe(true);
      expect(tabAudit.exists()).toBe(true);

      // Initially on Profile tab (Tab 1)
      expect(wrapper.find('[data-testid="panel-profile"]').exists()).toBe(true);

      // Switch to Capabilities (Tab 2)
      await tabCapabilities.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="panel-capabilities"]').exists()).toBe(true);

      // Switch to Qualifications (Tab 3)
      await tabQualifications.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="panel-qualifications"]').exists()).toBe(true);

      // Switch to Authorizations & Supervision (Tab 4)
      await tabAuthorizations.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="panel-authorizations"]').exists()).toBe(true);

      // Switch to Corrections Audit (Tab 5)
      await tabAudit.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="panel-audit"]').exists()).toBe(true);

      // Switch back to Profile (Tab 1)
      await tabProfile.trigger('click');
      await flushPromises();
      expect(wrapper.find('[data-testid="panel-profile"]').exists()).toBe(true);
    });
  });

  // ---------------------------------------------------------------------------
  // 2. AuthorizationsSupervisionTab Amber Warning Badge
  // ---------------------------------------------------------------------------
  describe('AuthorizationsSupervisionTab Warning Badges (TD-05)', () => {
    it('renders amber warning badge when unverifiedCredentialWarning is true', () => {
      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: mockTeacherProfile,
          canAuthorize: true,
          canManage: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        },
      });

      // Authorization auth-002 has unverifiedCredentialWarning: true
      const authCard2 = wrapper.find('[data-testid="authorization-card-auth-002"]');
      expect(authCard2.exists()).toBe(true);

      const warningBadge = authCard2.find('[data-testid="auth-unverified-warning"]');
      expect(warningBadge.exists()).toBe(true);
      expect(warningBadge.text()).toContain('Unverified Credential');

      // Authorization auth-001 has unverifiedCredentialWarning: false
      const authCard1 = wrapper.find('[data-testid="authorization-card-auth-001"]');
      expect(authCard1.exists()).toBe(true);
      expect(authCard1.find('[data-testid="auth-unverified-warning"]').exists()).toBe(false);
    });

    it('renders amber warning badge when authorization item has unverifiedCredentialWarning flag', () => {
      const customAuthorizations: TeachingAuthorizationDto[] = [
        {
          id: 'auth-003',
          authorizationNumber: 'AUTH-2026-003',
          teacherProfileId: 'tch-001',
          scopeDescription: 'Kimia SMA Tatap Muka',
          campusScope: 'Kampus Utama',
          status: 'active',
          unverifiedCredentialWarning: true,
          effectiveStartDate: '2026-07-01',
          decisionAuthority: 'Kepala Sekolah',
          approvedDate: '2026-07-01',
          supervisionRequirements: [],
        },
      ];

      const profileWithCustomAuth = {
        ...mockTeacherProfile,
        authorizations: customAuthorizations,
      };

      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: profileWithCustomAuth,
          canAuthorize: true,
          canManage: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        },
      });

      const authCard = wrapper.find('[data-testid="authorization-card-auth-003"]');
      expect(authCard.exists()).toBe(true);
      const warningBadge = authCard.find('[data-testid="auth-unverified-warning"]');
      expect(warningBadge.exists()).toBe(true);
      expect(warningBadge.text()).toContain('Unverified Credential');
    });
  });

  // ---------------------------------------------------------------------------
  // 3. Modal Open/Close & Submit Triggers
  // ---------------------------------------------------------------------------
  describe('Modal Open/Close & Submit Triggers', () => {
    it('AddCapabilityModal: opens, closes, and emits close event', async () => {
      const wrapper = mount(AddCapabilityModal, {
        props: {
          visible: true,
          teacherId: 'tch-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="add-capability-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="cancel-add-capability-btn"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('SubmitEvidenceModal: opens, closes, and emits close event', async () => {
      const wrapper = mount(SubmitEvidenceModal, {
        props: {
          visible: true,
          teacherId: 'tch-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="submit-evidence-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="cancel-submit-evidence-btn"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('GrantAuthorizationModal: opens, validates, and closes', async () => {
      const wrapper = mount(GrantAuthorizationModal, {
        props: {
          visible: true,
          teacher: mockTeacherProfile,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="grant-authorization-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="cancel-grant-button"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('AddSupervisionModal: opens, closes, and validates modality options', async () => {
      const wrapper = mount(AddSupervisionModal, {
        props: {
          visible: true,
          authorizationId: 'auth-001',
          authorizationNumber: 'AUTH-2026-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="add-supervision-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="cancel-supervision-button"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('ResolveSupervisionModal: opens, renders active requirement, and closes', async () => {
      const wrapper = mount(ResolveSupervisionModal, {
        props: {
          visible: true,
          supervisionId: 'sup-001',
          authorizationNumber: 'AUTH-2026-002',
          supervisionDescription: 'Co-teaching observation',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="resolve-supervision-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="cancel-resolve-button"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('ChangeAuthorizationStatusModal: opens, renders status choices, and closes', async () => {
      const wrapper = mount(ChangeAuthorizationStatusModal, {
        props: {
          visible: true,
          authorization: mockAuthorizations[0],
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="change-authorization-status-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="cancel-change-auth-status-button"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('ChangeStandingModal: opens, renders standing select, and closes', async () => {
      const wrapper = mount(ChangeStandingModal, {
        props: {
          visible: true,
          teacher: mockTeacherProfile,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="change-standing-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="btn-cancel-standing"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });

    it('RecordCorrectionModal: opens, toggles authority mode, and closes', async () => {
      const wrapper = mount(RecordCorrectionModal, {
        props: {
          visible: true,
          teacher: mockTeacherProfile,
          canAuthorize: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      expect(wrapper.find('[data-testid="record-correction-modal"]').exists()).toBe(true);

      const cancelBtn = wrapper.find('[data-testid="btn-cancel-correction"]');
      expect(cancelBtn.exists()).toBe(true);
      await cancelBtn.trigger('click');
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });
  });
});
