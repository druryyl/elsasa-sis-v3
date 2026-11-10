// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import AuthorizationsSupervisionTab from '../components/AuthorizationsSupervisionTab.vue';
import GrantAuthorizationModal from '../components/GrantAuthorizationModal.vue';
import AddSupervisionModal from '../components/AddSupervisionModal.vue';
import ResolveSupervisionModal from '../components/ResolveSupervisionModal.vue';
import ChangeAuthorizationStatusModal from '../components/ChangeAuthorizationStatusModal.vue';
import CorrectionsAuditTab from '../components/CorrectionsAuditTab.vue';
import type {
  TeacherProfileDto,
  TeachingAuthorizationDto,
  SupervisionRequirementDto,
  TeachingRecordCorrectionDto,
  QualificationEvidenceDto,
} from '../api/teachingTypes';
import * as facultyApiClient from '../api/teachingApiClient';

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

const mockCorrections: TeachingRecordCorrectionDto[] = [
  {
    id: 'corr-001',
    correctionNumber: 'CORR-2026-001',
    targetTableName: 'teaching.teacher_profiles',
    targetRecordId: 'tch-001',
    teacherProfileId: 'tch-001',
    fieldName: 'displayName',
    originalValue: 'Budi H.',
    correctedValue: 'Budi Hartono, M.Pd.',
    reasonForCorrection: 'Updated official academic post-nominal title',
    downstreamImpactReviewed: true,
    requestedBy: 'Teaching Coordinator',
    requestedDate: '2026-08-01',
    approvedBy: 'Kepala Sekolah',
    approvedDate: '2026-08-02',
    status: 'approved',
  },
  {
    id: 'corr-002',
    correctionNumber: 'CORR-2026-002',
    targetTableName: 'teaching.teaching_authorizations',
    targetRecordId: 'auth-001',
    teacherProfileId: 'tch-001',
    fieldName: 'campusScope',
    originalValue: 'Kampus Barat',
    correctedValue: 'Kampus Utama',
    reasonForCorrection: 'Corrected administrative typo in campus boundary',
    downstreamImpactReviewed: true,
    requestedBy: 'Teaching Coordinator',
    requestedDate: '2026-08-05',
    approvedBy: null,
    approvedDate: null,
    status: 'requested',
  },
];

const mockTeacher: TeacherProfileDto = {
  id: 'tch-001',
  teacherNumber: 'TCH-2026-001',
  fullName: 'Budi Hartono',
  displayName: 'Budi Hartono, M.Pd.',
  teachingRoleId: 'role-01',
  teachingRoleName: 'Guru Tetap',
  requiresQualificationCheck: true,
  campusAffiliation: 'Kampus Utama',
  department: 'Matematika & IPA',
  standing: 'active',
  email: 'budi.hartono@hagios.sch.id',
  phone: '081234567890',
  employeeRef: 'EMP-001',
  nationalIdOrExternalRef: '3201234567890001',
  effectiveFrom: '2026-01-01',
  effectiveTo: null,
  standingReason: 'Formal intake approval',
  approvedBy: 'Kepala Sekolah',
  approvedDate: '2026-01-02',
  hasUnverifiedWarning: true,
  activeAuthorizationsCount: 2,
  capabilities: [],
  qualifications: mockQualifications,
  authorizations: mockAuthorizations,
  recentCorrections: mockCorrections,
};

const commonStubs = {
  Dialog: {
    props: ['visible', 'header', 'appendTo'],
    emits: ['update:visible', 'hide'],
    template: `
      <div v-if="visible" class="p-dialog" v-bind="$attrs">
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
        v-bind="$attrs"
        @change="$emit('update:modelValue', $event.target.value)"
      >
        <option v-for="opt in options" :key="opt.value !== undefined ? opt.value : opt" :value="opt.value !== undefined ? opt.value : opt">
          {{ opt.label || opt }}
        </option>
      </select>
    `,
  },
  Textarea: {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    template: `
      <textarea
        :value="modelValue"
        v-bind="$attrs"
        @input="$emit('update:modelValue', $event.target.value)"
      />
    `,
  },
};

describe('Authorizations & Supervision Tab and Modals (P8-S27)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.restoreAllMocks();

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
  });

  // =========================================================================
  // 1. AuthorizationsSupervisionTab.vue
  // =========================================================================
  describe('AuthorizationsSupervisionTab.vue', () => {
    it('renders authorizations list with authorization numbers, status badges, and scope pills', async () => {
      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: mockTeacher,
          canAuthorize: true,
          canManage: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="authorizations-supervision-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="authorization-card-auth-001"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="authorization-card-auth-002"]').exists()).toBe(true);

      expect(wrapper.text()).toContain('AUTH-2026-001');
      expect(wrapper.text()).toContain('Matematika SD Kelas 4–6 Tatap Muka');
      expect(wrapper.text()).toContain('Kampus Utama');
      expect(wrapper.text()).toContain('Primary School');
    });

    it('renders prominent permissive warning indicator (⚠️ Unverified Credential) on authorization with unverified credentials', async () => {
      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: mockTeacher,
          canAuthorize: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const card2 = wrapper.find('[data-testid="authorization-card-auth-002"]');
      expect(card2.find('[data-testid="auth-unverified-warning"]').exists()).toBe(true);
      expect(card2.text()).toContain('Unverified Credential');
    });

    it('renders linked supporting qualification credentials pills with verification status', async () => {
      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: mockTeacher,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const card1 = wrapper.find('[data-testid="authorization-card-auth-001"]');
      expect(card1.find('[data-testid="linked-evidence-section"]').exists()).toBe(true);
      expect(card1.find('[data-testid="linked-evidence-pill-qual-001"]').exists()).toBe(true);
      expect(card1.text()).toContain('Sertifikat Pendidik Matematika');
    });

    it('renders supervision requirements sub-section with mentor card, modality, dates, and Resolve Supervision action', async () => {
      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: mockTeacher,
          canManage: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const card2 = wrapper.find('[data-testid="authorization-card-auth-002"]');
      const supSection = card2.find('[data-testid="supervision-requirements-section"]');
      expect(supSection.exists()).toBe(true);
      expect(supSection.find('[data-testid="supervision-card-sup-001"]').exists()).toBe(true);

      expect(supSection.text()).toContain('Co-Teaching');
      expect(supSection.text()).toContain('Budi Hartono, M.Pd.');
      expect(supSection.text()).toContain('Co-teaching twice weekly');

      const resolveBtn = supSection.find('[data-testid="resolve-supervision-btn-sup-001"]');
      expect(resolveBtn.exists()).toBe(true);
    });

    it('renders "+ Grant Authorization" button when user has Teaching Authority permission', async () => {
      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: mockTeacher,
          canAuthorize: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const grantBtn = wrapper.find('[data-testid="grant-authorization-button"]');
      expect(grantBtn.exists()).toBe(true);

      await grantBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="grant-authorization-modal"]').exists()).toBe(true);
    });

    it('renders empty state when faculty has no authorizations', async () => {
      const teacherWithoutAuth = {
        ...mockTeacher,
        authorizations: [],
      };

      const wrapper = mount(AuthorizationsSupervisionTab, {
        props: {
          teacher: teacherWithoutAuth,
          canAuthorize: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="empty-authorizations"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No active teaching authorizations');
    });
  });

  // =========================================================================
  // 2. GrantAuthorizationModal.vue
  // =========================================================================
  describe('GrantAuthorizationModal.vue', () => {
    it('shows permissive guidance warning banner when unverified credential is selected', async () => {
      const wrapper = mount(GrantAuthorizationModal, {
        props: {
          visible: true,
          teacher: mockTeacher,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();
      expect(wrapper.find('[data-testid="grant-authorization-modal"]').exists()).toBe(true);

      // Select unverified credential qual-002
      const item2 = wrapper.find('[data-testid="credential-item-qual-002"]');
      await item2.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="permissive-warning-banner"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Permissive Guidance Warning (TD-05)');
    });

    it('displays conditional supervision fields when status is set to authorized_under_supervision', async () => {
      const wrapper = mount(GrantAuthorizationModal, {
        props: {
          visible: true,
          teacher: mockTeacher,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      // Default status is 'active' -> no supervision section
      expect(wrapper.find('[data-testid="conditional-supervision-section"]').exists()).toBe(false);

      // Change status to authorized_under_supervision via select
      const statusSelect = wrapper.find('[data-testid="select-authorization-status"]');
      await statusSelect.setValue('authorized_under_supervision');
      await flushPromises();

      expect(wrapper.find('[data-testid="conditional-supervision-section"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="select-supervision-type"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-supervision-description"]').exists()).toBe(true);
    });

    it('validates required scope description and dispatches useGrantAuthorizationMutation', async () => {
      const grantSpy = vi.spyOn(facultyApiClient, 'grantAuthorization').mockResolvedValueOnce({
        id: 'auth-999',
        authorizationNumber: 'AUTH-2026-999',
        teacherProfileId: 'tch-001',
        scopeDescription: 'Guru Sains SD',
        campusScope: 'Kampus Utama',
        status: 'active',
        unverifiedCredentialWarning: false,
        effectiveStartDate: '2026-07-01',
        decisionAuthority: 'Kepala Sekolah',
        approvedDate: '2026-07-01',
        linkedEvidenceIds: ['qual-001'],
      } as any);

      const wrapper = mount(GrantAuthorizationModal, {
        props: {
          visible: true,
          teacher: mockTeacher,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const scopeTextarea = wrapper.find('[data-testid="input-scope-description"]');
      await scopeTextarea.setValue('Guru Sains SD');

      await wrapper.find('[data-testid="submit-grant-button"]').trigger('click');
      await flushPromises();

      expect(grantSpy).toHaveBeenCalledWith(
        'tch-001',
        expect.objectContaining({
          scopeDescription: 'Guru Sains SD',
          campusScope: 'Kampus Utama',
        }),
      );
      expect(wrapper.emitted('granted')).toBeTruthy();
    });
  });

  // =========================================================================
  // 3. AddSupervisionModal.vue & ResolveSupervisionModal.vue
  // =========================================================================
  describe('Supervision Modals', () => {
    it('AddSupervisionModal.vue validates and calls addSupervision API', async () => {
      const addSpy = vi.spyOn(facultyApiClient, 'addSupervision').mockResolvedValueOnce({
        id: 'sup-999',
        authorizationId: 'auth-001',
        supervisionType: 'periodic_observation',
        description: 'Bi-weekly classroom observation',
        startDate: '2026-07-01',
        status: 'active',
        createdAt: '2026-07-01',
      } as any);

      const wrapper = mount(AddSupervisionModal, {
        props: {
          visible: true,
          authorizationId: 'auth-001',
          authorizationNumber: 'AUTH-2026-001',
          teacherId: 'tch-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const descTextarea = wrapper.find('[data-testid="input-supervision-description"]');
      await descTextarea.setValue('Bi-weekly classroom observation');

      await wrapper.find('[data-testid="submit-supervision-button"]').trigger('click');
      await flushPromises();

      expect(addSpy).toHaveBeenCalledWith(
        'auth-001',
        expect.objectContaining({
          description: 'Bi-weekly classroom observation',
        }),
      );
      expect(wrapper.emitted('added')).toBeTruthy();
    });

    it('ResolveSupervisionModal.vue validates and calls resolveSupervision API', async () => {
      const resolveSpy = vi.spyOn(facultyApiClient, 'resolveSupervision').mockResolvedValueOnce({
        id: 'sup-001',
        authorizationId: 'auth-002',
        status: 'completed',
        resolutionNotes: 'Successfully completed mentoring program.',
        resolvedDate: '2026-12-15',
        resolvedBy: 'Kepala Sekolah',
        authorizationStatus: 'active',
        authorizationUpdatedToActive: true,
      } as any);

      const wrapper = mount(ResolveSupervisionModal, {
        props: {
          visible: true,
          supervisionId: 'sup-001',
          authorizationNumber: 'AUTH-2026-002',
          teacherId: 'tch-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const notesTextarea = wrapper.find('[data-testid="input-resolution-notes"]');
      await notesTextarea.setValue('Successfully completed mentoring program.');

      await wrapper.find('[data-testid="submit-resolve-button"]').trigger('click');
      await flushPromises();

      expect(resolveSpy).toHaveBeenCalledWith(
        'sup-001',
        expect.objectContaining({
          resolutionStatus: 'completed',
          resolutionNotes: 'Successfully completed mentoring program.',
          updateAuthorizationToActive: true,
        }),
      );
      expect(wrapper.emitted('resolved')).toBeTruthy();
    });

    it('ChangeAuthorizationStatusModal.vue validates and calls updateAuthorizationStatus API', async () => {
      const statusSpy = vi.spyOn(facultyApiClient, 'updateAuthorizationStatus').mockResolvedValueOnce({
        id: 'auth-001',
        teacherProfileId: 'tch-001',
        authorizationNumber: 'AUTH-2026-001',
        previousStatus: 'active',
        status: 'suspended',
        statusReason: 'Administrative review',
        updatedBy: 'Kepala Sekolah',
        updatedAt: '2026-08-01',
      } as any);

      const wrapper = mount(ChangeAuthorizationStatusModal, {
        props: {
          visible: true,
          authorization: mockAuthorizations[0],
          teacherId: 'tch-001',
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const selectStatus = wrapper.find('[data-testid="select-auth-status"]');
      await selectStatus.setValue('suspended');

      const reasonTextarea = wrapper.find('[data-testid="input-auth-status-reason"]');
      await reasonTextarea.setValue('Administrative review');

      await wrapper.find('[data-testid="submit-change-auth-status-button"]').trigger('click');
      await flushPromises();

      expect(statusSpy).toHaveBeenCalledWith(
        'auth-001',
        expect.objectContaining({
          status: 'suspended',
          statusReason: 'Administrative review',
        }),
      );
      expect(wrapper.emitted('changed')).toBeTruthy();
    });
  });

  // =========================================================================
  // 4. CorrectionsAuditTab.vue
  // =========================================================================
  describe('CorrectionsAuditTab.vue', () => {
    it('renders chronological list of corrections with before/after delta boxes and actor stamps', async () => {
      const wrapper = mount(CorrectionsAuditTab, {
        props: {
          teacher: mockTeacher,
          canAuthorize: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="corrections-audit-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="correction-card-corr-001"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="correction-card-corr-002"]').exists()).toBe(true);

      // CORR-001 details
      expect(wrapper.text()).toContain('CORR-2026-001');
      expect(wrapper.text()).toContain('Budi H.');
      expect(wrapper.text()).toContain('Budi Hartono, M.Pd.');
      expect(wrapper.text()).toContain('Updated official academic post-nominal title');

      // CORR-002 details
      expect(wrapper.text()).toContain('CORR-2026-002');
      expect(wrapper.text()).toContain('Kampus Barat');
      expect(wrapper.text()).toContain('Kampus Utama');
    });

    it('renders "Approve Correction" button for requested corrections when user has authority permission and triggers mutation', async () => {
      const approveSpy = vi.spyOn(facultyApiClient, 'approveCorrection').mockResolvedValueOnce({
        id: 'corr-002',
        correctionNumber: 'CORR-2026-002',
        targetTableName: 'teaching.teaching_authorizations',
        targetRecordId: 'auth-001',
        fieldName: 'campusScope',
        originalValue: 'Kampus Barat',
        correctedValue: 'Kampus Utama',
        reasonForCorrection: 'Corrected administrative typo',
        downstreamImpactReviewed: true,
        requestedBy: 'Teaching Coordinator',
        requestedDate: '2026-08-05',
        approvedBy: 'Kepala Sekolah',
        approvedDate: '2026-08-05',
        status: 'approved',
      } as any);

      const wrapper = mount(CorrectionsAuditTab, {
        props: {
          teacher: mockTeacher,
          canAuthorize: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const approveBtn = wrapper.find('[data-testid="approve-correction-btn-corr-002"]');
      expect(approveBtn.exists()).toBe(true);

      await approveBtn.trigger('click');
      await flushPromises();

      expect(approveSpy).toHaveBeenCalledWith('corr-002');
      expect(wrapper.emitted('correctionApproved')).toBeTruthy();
      expect(wrapper.emitted('refresh')).toBeTruthy();
    });

    it('renders empty state when dossier has no logged corrections', async () => {
      const teacherWithoutCorrections = {
        ...mockTeacher,
        recentCorrections: [],
      };

      const wrapper = mount(CorrectionsAuditTab, {
        props: {
          teacher: teacherWithoutCorrections,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="empty-corrections"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No record corrections logged');
    });
  });
});
