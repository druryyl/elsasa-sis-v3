// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import TeachingCapabilitiesTab from '../components/TeachingCapabilitiesTab.vue';
import AddCapabilityModal from '../components/AddCapabilityModal.vue';
import QualificationEvidenceTab from '../components/QualificationEvidenceTab.vue';
import SubmitEvidenceModal from '../components/SubmitEvidenceModal.vue';
import type {
  TeacherProfileDto,
  TeachingCapabilityDto,
  QualificationEvidenceDto,
  CapabilityTypeDto,
  QualificationTypeResponse,
} from '../api/teachingTypes';
import * as facultyApiClient from '../api/teachingApiClient';

const mockCapabilityTypes: CapabilityTypeDto[] = [
  { id: 'ctype-01', code: 'SUBJ', name: 'Subject Areas', isActive: true },
  { id: 'ctype-02', code: 'STAGE', name: 'Educational Stages', isActive: true },
  { id: 'ctype-03', code: 'MODE', name: 'Delivery Modes', isActive: true },
  { id: 'ctype-04', code: 'LANG', name: 'Languages of Instruction', isActive: true },
];

const mockQualificationTypes: QualificationTypeResponse[] = [
  {
    id: 'qtype-01',
    code: 'SERDIK',
    name: 'Sertifikat Pendidik',
    requiresRenewal: false,
    defaultValidityMonths: null,
    isActive: true,
  },
  {
    id: 'qtype-02',
    code: 'IJAZAH_S1',
    name: 'Ijazah Sarjana (S1)',
    requiresRenewal: false,
    defaultValidityMonths: null,
    isActive: true,
  },
  {
    id: 'qtype-03',
    code: 'CHILD_PROT',
    name: 'Child Protection Certification',
    requiresRenewal: true,
    defaultValidityMonths: 24,
    isActive: true,
  },
];

const mockCapabilities: TeachingCapabilityDto[] = [
  {
    id: 'cap-01',
    teacherProfileId: 'tch-001',
    capabilityTypeId: 'ctype-01',
    capabilityCode: 'MATH_SD',
    capabilityName: 'Matematika SD Kelas 4-6',
    applicableLevel: 'SD Kelas 4-6',
    deliveryMode: 'In-Person / Face-to-Face',
    languageOfInstruction: 'Bahasa Indonesia',
    status: 'active',
    notes: 'Primary mathematics specialization',
  },
  {
    id: 'cap-02',
    teacherProfileId: 'tch-001',
    capabilityTypeId: 'ctype-04',
    capabilityCode: 'ENG_BIL',
    capabilityName: 'Bilingual English Instruction',
    applicableLevel: 'All Grades',
    deliveryMode: 'Hybrid',
    languageOfInstruction: 'English',
    status: 'active',
    notes: null,
  },
];

const mockQualifications: QualificationEvidenceDto[] = [
  {
    id: 'qual-01',
    teacherProfileId: 'tch-001',
    qualificationTypeId: 'qtype-01',
    title: 'Sertifikat Pendidik Guru Kelas SD',
    issuingAuthority: 'Kemendikbudristek',
    credentialIdentifier: 'SERDIK-2024-8899',
    issueDate: '2024-05-10',
    expiryDate: null,
    documentReference: 'https://storage.elsasa.sch.id/creds/serdik-8899.pdf',
    verificationState: 'verified',
    verifiedBy: 'Dr. Hendra Gunawan',
    verifiedDate: '2024-05-15',
    verificationNotes: 'Official Kemendikbud registration verified.',
    rejectionReason: null,
    restrictionsOrConditions: null,
  },
  {
    id: 'qual-02',
    teacherProfileId: 'tch-001',
    qualificationTypeId: 'qtype-03',
    title: 'Child Protection and Safeguarding 2026',
    issuingAuthority: 'International Child Safety Institute',
    credentialIdentifier: 'CPS-2026-0044',
    issueDate: '2026-01-15',
    expiryDate: '2028-01-15',
    documentReference: 'https://storage.elsasa.sch.id/creds/cps-0044.pdf',
    verificationState: 'submitted',
    verifiedBy: null,
    verifiedDate: null,
    verificationNotes: null,
    rejectionReason: null,
    restrictionsOrConditions: 'Valid only in Hagios Primary Campus',
  },
];

const mockTeacher: TeacherProfileDto = {
  id: 'tch-001',
  teacherNumber: 'TCH-2026-0042',
  fullName: 'Budi Hartono',
  displayName: 'Budi Hartono, M.Pd.',
  employeeRef: 'EMP-2026-042',
  nationalIdOrExternalRef: '3201234567890001',
  email: 'budi.hartono@elsasa.sch.id',
  phone: '+6281234567890',
  teachingRoleId: 'role-01',
  teachingRoleName: 'Guru Tetap',
  campusAffiliation: 'Kampus Utama',
  department: 'Matematika & IPA',
  standing: 'active',
  effectiveFrom: '2026-07-01',
  effectiveTo: null,
  standingReason: 'Senior faculty regular intake',
  approvedBy: 'Dr. Hendra Gunawan',
  approvedDate: '2026-07-02',
  notes: 'Senior mathematics teacher.',
  capabilities: mockCapabilities,
  qualifications: mockQualifications,
  authorizations: [],
  recentCorrections: [],
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

describe('P8-S26 Capabilities & Qualification Evidence Component Suite', () => {
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

    vi.restoreAllMocks();
    vi.spyOn(facultyApiClient, 'getCapabilityTypes').mockResolvedValue(mockCapabilityTypes);
    vi.spyOn(facultyApiClient, 'getQualificationTypes').mockResolvedValue(mockQualificationTypes);
  });

  // =========================================================================
  // 1. TeachingCapabilitiesTab.vue Tests
  // =========================================================================
  describe('TeachingCapabilitiesTab.vue', () => {
    it('renders categorized capabilities list with subject, level, delivery mode and language parameters', async () => {
      const wrapper = mount(TeachingCapabilitiesTab, {
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

      expect(wrapper.find('[data-testid="teaching-capabilities-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="capabilities-count-badge"]').text()).toContain('2 Registered');
      expect(wrapper.find('[data-testid="capability-card-cap-01"]').text()).toContain('Matematika SD Kelas 4-6');
      expect(wrapper.find('[data-testid="capability-card-cap-01"]').text()).toContain('MATH_SD');
      expect(wrapper.find('[data-testid="capability-card-cap-01"]').text()).toContain('SD Kelas 4-6');
      expect(wrapper.find('[data-testid="capability-card-cap-01"]').text()).toContain('In-Person / Face-to-Face');
      expect(wrapper.find('[data-testid="capability-card-cap-01"]').text()).toContain('Bahasa Indonesia');
      expect(wrapper.find('[data-testid="capability-card-cap-02"]').text()).toContain('Bilingual English Instruction');
    });

    it('renders empty state when faculty has no registered capabilities', async () => {
      const emptyTeacher = { ...mockTeacher, capabilities: [] };
      const wrapper = mount(TeachingCapabilitiesTab, {
        props: {
          teacher: emptyTeacher,
          canManage: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="empty-capabilities"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="empty-add-capability-btn"]').exists()).toBe(true);
    });

    it('opens AddCapabilityModal when Add Capability button is clicked', async () => {
      const wrapper = mount(TeachingCapabilitiesTab, {
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

      const addBtn = wrapper.find('[data-testid="open-add-capability-modal-btn"]');
      expect(addBtn.exists()).toBe(true);
      await addBtn.trigger('click');

      expect(wrapper.find('[data-testid="add-capability-modal"]').exists()).toBe(true);
    });

    it('prompts retire confirmation and calls removeCapability API upon confirmation', async () => {
      const removeSpy = vi.spyOn(facultyApiClient, 'removeCapability').mockResolvedValue(undefined);

      const wrapper = mount(TeachingCapabilitiesTab, {
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

      const retireBtn = wrapper.find('[data-testid="retire-capability-btn-cap-01"]');
      expect(retireBtn.exists()).toBe(true);
      await retireBtn.trigger('click');

      expect(wrapper.find('[data-testid="retire-capability-dialog"]').exists()).toBe(true);

      const confirmBtn = wrapper.find('[data-testid="confirm-retire-capability-btn"]');
      await confirmBtn.trigger('click');
      await flushPromises();

      expect(removeSpy).toHaveBeenCalledWith('tch-001', 'cap-01');
      expect(wrapper.emitted('capabilityRemoved')).toBeTruthy();
      expect(wrapper.emitted('capabilityRemoved')![0]).toEqual(['cap-01']);
    });
  });

  // =========================================================================
  // 2. AddCapabilityModal.vue Tests
  // =========================================================================
  describe('AddCapabilityModal.vue', () => {
    it('validates required fields and submits addCapability mutation', async () => {
      const addSpy = vi.spyOn(facultyApiClient, 'addCapability').mockResolvedValue({
        id: 'cap-new-01',
        teacherProfileId: 'tch-001',
        capabilityTypeId: 'ctype-01',
        capabilityCode: 'PHYS_SMA',
        capabilityName: 'Fisika SMA Kelas 10-12',
        status: 'active',
      });

      const wrapper = mount(AddCapabilityModal, {
        props: {
          visible: true,
          teacherId: 'tch-001',
          capabilityTypes: mockCapabilityTypes,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      // Submit with empty required inputs
      const submitBtn = wrapper.find('[data-testid="submit-add-capability-btn"]');
      await submitBtn.trigger('click');
      await flushPromises();

      // Fill in valid fields
      const codeInput = wrapper.find('[data-testid="input-capability-code"]');
      await codeInput.setValue('PHYS_SMA');

      const nameInput = wrapper.find('[data-testid="input-capability-name"]');
      await nameInput.setValue('Fisika SMA Kelas 10-12');

      await submitBtn.trigger('click');
      await flushPromises();

      expect(addSpy).toHaveBeenCalledWith(
        'tch-001',
        expect.objectContaining({
          capabilityCode: 'PHYS_SMA',
          capabilityName: 'Fisika SMA Kelas 10-12',
        }),
      );

      expect(wrapper.emitted('added')).toBeTruthy();
    });
  });

  // =========================================================================
  // 3. QualificationEvidenceTab.vue Tests
  // =========================================================================
  describe('QualificationEvidenceTab.vue', () => {
    it('renders credentials list with title, type, issuer, dates, badges and document link', async () => {
      const wrapper = mount(QualificationEvidenceTab, {
        props: {
          teacher: mockTeacher,
          canManage: true,
          canVerify: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="qualification-evidence-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="qualifications-count-badge"]').text()).toContain('2 Total');
      expect(wrapper.find('[data-testid="pending-verification-badge"]').text()).toContain('1 Awaiting Verification');

      // Verified evidence card
      const card1 = wrapper.find('[data-testid="qualification-card-qual-01"]');
      expect(card1.text()).toContain('Sertifikat Pendidik Guru Kelas SD');
      expect(card1.text()).toContain('Kemendikbudristek');
      expect(card1.text()).toContain('SERDIK-2024-8899');
      expect(card1.find('[data-testid="qual-verified-audit"]').exists()).toBe(true);
      expect(card1.find('[data-testid="qual-verified-audit"]').text()).toContain('Dr. Hendra Gunawan');

      // Submitted evidence card with visual distinction
      const card2 = wrapper.find('[data-testid="qualification-card-qual-02"]');
      expect(card2.text()).toContain('Child Protection and Safeguarding 2026');
      expect(card2.find('[data-testid="awaiting-verification-tag"]').exists()).toBe(true);
      expect(card2.find('[data-testid="evidence-doc-link"]').exists()).toBe(true);
    });

    it('renders empty state when faculty has no qualification evidence', async () => {
      const emptyTeacher = { ...mockTeacher, qualifications: [] };
      const wrapper = mount(QualificationEvidenceTab, {
        props: {
          teacher: emptyTeacher,
          canManage: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      expect(wrapper.find('[data-testid="empty-qualifications"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="empty-submit-evidence-btn"]').exists()).toBe(true);
    });

    it('opens SubmitEvidenceModal when Submit Evidence button is clicked', async () => {
      const wrapper = mount(QualificationEvidenceTab, {
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

      const openBtn = wrapper.find('[data-testid="open-submit-evidence-modal-btn"]');
      expect(openBtn.exists()).toBe(true);
      await openBtn.trigger('click');

      expect(wrapper.find('[data-testid="submit-evidence-modal"]').exists()).toBe(true);
    });

    it('role-gates Verify and Reject actions to authorized verifiers', async () => {
      // Non-verifier user
      const unprivilegedWrapper = mount(QualificationEvidenceTab, {
        props: {
          teacher: mockTeacher,
          canManage: true,
          canVerify: false,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();
      expect(unprivilegedWrapper.find('[data-testid="verify-evidence-btn-qual-02"]').exists()).toBe(false);
      expect(unprivilegedWrapper.find('[data-testid="reject-evidence-btn-qual-02"]').exists()).toBe(false);

      // Verifier user
      const verifierWrapper = mount(QualificationEvidenceTab, {
        props: {
          teacher: mockTeacher,
          canManage: true,
          canVerify: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();
      expect(verifierWrapper.find('[data-testid="verify-evidence-btn-qual-02"]').exists()).toBe(true);
      expect(verifierWrapper.find('[data-testid="reject-evidence-btn-qual-02"]').exists()).toBe(true);
    });

    it('executes inline Verify dialog and submits verification mutation', async () => {
      const verifySpy = vi.spyOn(facultyApiClient, 'verifyEvidence').mockResolvedValue({
        id: 'qual-02',
        verificationState: 'verified',
        verifiedBy: 'Test Verifier',
        verifiedDate: '2026-11-10',
      });

      const wrapper = mount(QualificationEvidenceTab, {
        props: {
          teacher: mockTeacher,
          canManage: true,
          canVerify: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const verifyBtn = wrapper.find('[data-testid="verify-evidence-btn-qual-02"]');
      await verifyBtn.trigger('click');

      expect(wrapper.find('[data-testid="verify-evidence-dialog"]').exists()).toBe(true);

      const notesInput = wrapper.find('[data-testid="textarea-verification-notes"]');
      await notesInput.setValue('Document authenticated against issuer verification portal.');

      const confirmBtn = wrapper.find('[data-testid="confirm-verify-btn"]');
      await confirmBtn.trigger('click');
      await flushPromises();

      expect(verifySpy).toHaveBeenCalledWith('qual-02', {
        verificationNotes: 'Document authenticated against issuer verification portal.',
      });

      expect(wrapper.emitted('evidenceVerified')).toBeTruthy();
      expect(wrapper.emitted('refresh')).toBeTruthy();
    });

    it('executes inline Reject dialog, requires mandatory reason, and submits rejection mutation', async () => {
      const rejectSpy = vi.spyOn(facultyApiClient, 'rejectEvidence').mockResolvedValue({
        id: 'qual-02',
        verificationState: 'rejected',
        rejectionReason: 'Serial number could not be validated.',
      });

      const wrapper = mount(QualificationEvidenceTab, {
        props: {
          teacher: mockTeacher,
          canManage: true,
          canVerify: true,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const rejectBtn = wrapper.find('[data-testid="reject-evidence-btn-qual-02"]');
      await rejectBtn.trigger('click');

      expect(wrapper.find('[data-testid="reject-evidence-dialog"]').exists()).toBe(true);

      // Attempt submit without reason
      const confirmBtn = wrapper.find('[data-testid="confirm-reject-btn"]');
      await confirmBtn.trigger('click');
      await flushPromises();

      expect(rejectSpy).not.toHaveBeenCalled();

      // Fill in reason
      const reasonInput = wrapper.find('[data-testid="textarea-rejection-reason"]');
      await reasonInput.setValue('Serial number could not be validated with issuing authority.');

      await confirmBtn.trigger('click');
      await flushPromises();

      expect(rejectSpy).toHaveBeenCalledWith('qual-02', {
        rejectionReason: 'Serial number could not be validated with issuing authority.',
      });

      expect(wrapper.emitted('evidenceRejected')).toBeTruthy();
      expect(wrapper.emitted('refresh')).toBeTruthy();
    });
  });

  // =========================================================================
  // 4. SubmitEvidenceModal.vue Tests
  // =========================================================================
  describe('SubmitEvidenceModal.vue', () => {
    it('validates fields and submits submitEvidence mutation', async () => {
      const submitSpy = vi.spyOn(facultyApiClient, 'submitEvidence').mockResolvedValue({
        id: 'qual-new-01',
        teacherProfileId: 'tch-001',
        title: 'Master of Education (M.Ed.)',
        verificationState: 'submitted',
      });

      const wrapper = mount(SubmitEvidenceModal, {
        props: {
          visible: true,
          teacherId: 'tch-001',
          qualificationTypes: mockQualificationTypes,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });

      await flushPromises();

      const titleInput = wrapper.find('[data-testid="input-evidence-title"]');
      await titleInput.setValue('Master of Education (M.Ed.)');

      const issuerInput = wrapper.find('[data-testid="input-issuing-authority"]');
      await issuerInput.setValue('Universitas Indonesia');

      const submitBtn = wrapper.find('[data-testid="submit-evidence-btn"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(submitSpy).toHaveBeenCalledWith(
        'tch-001',
        expect.objectContaining({
          title: 'Master of Education (M.Ed.)',
          issuingAuthority: 'Universitas Indonesia',
        }),
      );

      expect(wrapper.emitted('submitted')).toBeTruthy();
    });
  });
});
