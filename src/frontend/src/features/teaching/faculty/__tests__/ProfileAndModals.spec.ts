// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import ProfileAffiliationTab from '../components/ProfileAffiliationTab.vue';
import ChangeStandingModal from '../components/ChangeStandingModal.vue';
import RecordCorrectionModal from '../components/RecordCorrectionModal.vue';
import type { TeacherProfileDto } from '../api/teachingTypes';
import * as facultyApiClient from '../api/teachingApiClient';

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
  capabilities: [],
  qualifications: [],
  authorizations: [
    {
      id: 'auth-01',
      authorizationNumber: 'AUTH-2026-0001',
      teacherProfileId: 'tch-001',
      scopeDescription: 'Matematika SD Kelas 4-6',
      campusScope: 'Kampus Utama',
      status: 'active',
      unverifiedCredentialWarning: false,
      effectiveStartDate: '2026-07-01',
      effectiveEndDate: null,
      decisionAuthority: 'Kepala Sekolah',
      approvedDate: '2026-07-02',
    },
  ],
  recentCorrections: [],
};

const mockNonEmployeeTeacher: TeacherProfileDto = {
  ...mockTeacher,
  id: 'tch-002',
  teacherNumber: 'TCH-2026-0099',
  fullName: 'John Miller',
  displayName: 'John Miller, B.Ed.',
  employeeRef: null,
  nationalIdOrExternalRef: 'P-USA-9988221',
  teachingRoleName: 'Guru Tamu',
  standing: 'pending_approval',
  approvedBy: null,
  approvedDate: null,
};

const commonStubs = {
  Dialog: {
    props: ['visible', 'header', 'appendTo'],
    emits: ['update:visible', 'hide'],
    template: `
      <div v-if="visible" class="p-dialog">
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
        @change="$emit('update:modelValue', $event.target.value)"
      >
        <option v-for="opt in options" :key="opt.value || opt" :value="opt.value || opt">
          {{ opt.label || opt }}
        </option>
      </select>
    `,
  },
};

describe('P8-S25 Profile & Modals Component Suite', () => {
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
  });

  // =========================================================================
  // 1. ProfileAffiliationTab.vue
  // =========================================================================
  describe('ProfileAffiliationTab.vue', () => {
    function createTabWrapper(props: Partial<InstanceType<typeof ProfileAffiliationTab>['$props']> = {}) {
      return mount(ProfileAffiliationTab, {
        props: {
          teacher: mockTeacher,
          canEdit: true,
          ...props,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });
    }

    it('renders core personal, contact, affiliation and approval attributes', () => {
      const wrapper = createTabWrapper();

      expect(wrapper.find('[data-testid="detail-full-name"]').text()).toContain('Budi Hartono');
      expect(wrapper.find('[data-testid="detail-display-name"]').text()).toContain('Budi Hartono, M.Pd.');
      expect(wrapper.find('[data-testid="detail-teacher-number"]').text()).toContain('TCH-2026-0042');
      expect(wrapper.find('[data-testid="detail-role-name"]').text()).toContain('Guru Tetap');
      expect(wrapper.find('[data-testid="detail-campus"]').text()).toContain('Kampus Utama');
      expect(wrapper.find('[data-testid="detail-department"]').text()).toContain('Matematika & IPA');
      expect(wrapper.find('[data-testid="detail-email"]').text()).toContain('budi.hartono@elsasa.sch.id');
      expect(wrapper.find('[data-testid="detail-phone"]').text()).toContain('+6281234567890');
      expect(wrapper.find('[data-testid="detail-national-id"]').text()).toContain('3201234567890001');
      expect(wrapper.find('[data-testid="detail-employee-ref"]').text()).toContain('EMP-2026-042');
      expect(wrapper.find('[data-testid="detail-approval-stamp"]').text()).toContain('Dr. Hendra Gunawan');
      expect(wrapper.find('[data-testid="detail-standing-reason"]').text()).toContain('Senior faculty regular intake');
    });

    it('renders non-employee badge when employeeRef is null (TD-04 decoupling)', () => {
      const wrapper = createTabWrapper({ teacher: mockNonEmployeeTeacher });

      expect(wrapper.find('[data-testid="badge-non-employee"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="badge-non-employee"]').text()).toContain(
        'Independent Teaching Resource / Non-Employee',
      );
      expect(wrapper.find('[data-testid="detail-approval-stamp"]').text()).toContain(
        'Pending Authority Approval',
      );
    });

    it('enters inline edit mode and successfully submits mutation', async () => {
      const updateSpy = vi
        .spyOn(facultyApiClient, 'updateTeacher')
        .mockResolvedValueOnce();

      const wrapper = createTabWrapper();

      // Click Edit Profile
      const editBtn = wrapper.find('[data-testid="btn-edit-profile"]');
      expect(editBtn.exists()).toBe(true);
      await editBtn.trigger('click');

      expect(wrapper.find('[data-testid="input-edit-display-name"]').exists()).toBe(true);
      await wrapper.find('[data-testid="input-edit-display-name"]').setValue('Prof. Budi Hartono');
      await wrapper.find('[data-testid="input-edit-department"]').setValue('Fisika & Matematika');

      // Click Save Changes
      const saveBtn = wrapper.find('[data-testid="btn-save-profile"]');
      await saveBtn.trigger('click');
      await flushPromises();

      expect(updateSpy).toHaveBeenCalledWith(
        'tch-001',
        expect.objectContaining({
          displayName: 'Prof. Budi Hartono',
          department: 'Fisika & Matematika',
        }),
      );

      expect(wrapper.emitted('updated')).toBeTruthy();
    });

    it('cancels inline edit mode and reverts values without mutation', async () => {
      const updateSpy = vi.spyOn(facultyApiClient, 'updateTeacher');
      const wrapper = createTabWrapper();

      await wrapper.find('[data-testid="btn-edit-profile"]').trigger('click');
      await wrapper.find('[data-testid="input-edit-display-name"]').setValue('Changed Name');

      const cancelBtn = wrapper.find('[data-testid="btn-cancel-edit"]');
      await cancelBtn.trigger('click');

      expect(wrapper.find('[data-testid="btn-edit-profile"]').exists()).toBe(true);
      expect(updateSpy).not.toHaveBeenCalled();
    });
  });

  // =========================================================================
  // 2. ChangeStandingModal.vue
  // =========================================================================
  describe('ChangeStandingModal.vue', () => {
    function createStandingModalWrapper(props: Partial<InstanceType<typeof ChangeStandingModal>['$props']> = {}) {
      return mount(ChangeStandingModal, {
        props: {
          visible: true,
          teacher: mockTeacher,
          ...props,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });
    }

    it('renders current standing context and standing selection', () => {
      const wrapper = createStandingModalWrapper();

      expect(wrapper.find('[data-testid="change-standing-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="current-standing-pill"]').text().toLowerCase()).toContain('active');
      expect(wrapper.find('[data-testid="select-target-standing"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="textarea-standing-reason"]').exists()).toBe(true);
    });

    it('requires mandatory justification rationale before submitting', async () => {
      const wrapper = createStandingModalWrapper();

      const form = wrapper.find('[data-testid="change-standing-form"]');
      await form.trigger('submit.prevent');
      await flushPromises();

      expect(wrapper.text()).toContain('Standing justification reason is mandatory.');
    });

    it('submits valid standing change and emits changed event', async () => {
      const mockResponse = {
        id: 'tch-001',
        previousStanding: 'active',
        standing: 'restricted',
        standingReason: 'Restricted to Kampus Utama during probation',
        updatedBy: 'Kepala Sekolah',
        updatedAt: '2026-11-10T00:00:00Z',
      };

      const changeSpy = vi
        .spyOn(facultyApiClient, 'changeStanding')
        .mockResolvedValueOnce(mockResponse);

      const wrapper = createStandingModalWrapper();

      await wrapper.find('[data-testid="select-target-standing"]').setValue('restricted');
      await wrapper
        .find('[data-testid="textarea-standing-reason"]')
        .setValue('Restricted to Kampus Utama during probation');

      const form = wrapper.find('[data-testid="change-standing-form"]');
      await form.trigger('submit.prevent');
      await flushPromises();

      expect(changeSpy).toHaveBeenCalledWith('tch-001', {
        standing: 'restricted',
        standingReason: 'Restricted to Kampus Utama during probation',
      });

      expect(wrapper.emitted('changed')).toBeTruthy();
      expect(wrapper.emitted('changed')?.[0][0]).toEqual(mockResponse);
      expect(wrapper.emitted('update:visible')?.[0][0]).toBe(false);
    });
  });

  // =========================================================================
  // 3. RecordCorrectionModal.vue
  // =========================================================================
  describe('RecordCorrectionModal.vue', () => {
    function createCorrectionModalWrapper(props: Partial<InstanceType<typeof RecordCorrectionModal>['$props']> = {}) {
      return mount(RecordCorrectionModal, {
        props: {
          visible: true,
          teacher: mockTeacher,
          canAuthorize: true,
          ...props,
        },
        global: {
          plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
          stubs: commonStubs,
        },
      });
    }

    it('renders role-aware button for Teaching Authority (Apply Correction Directly)', () => {
      const wrapper = createCorrectionModalWrapper({ canAuthorize: true });

      expect(wrapper.find('[data-testid="authority-mode-badge"]').text()).toContain(
        'Authority Direct Apply',
      );
      expect(wrapper.find('[data-testid="btn-submit-correction"]').text()).toContain(
        'Apply Correction Directly',
      );
      expect(wrapper.find('[data-testid="display-original-value"]').text()).toContain('Budi Hartono');
    });

    it('renders role-aware button for Teaching Coordinator (Submit Correction Request)', () => {
      const wrapper = createCorrectionModalWrapper({ canAuthorize: false });

      expect(wrapper.find('[data-testid="authority-mode-badge"]').text()).toContain(
        'Coordinator Request',
      );
      expect(wrapper.find('[data-testid="btn-submit-correction"]').text()).toContain(
        'Submit Correction Request',
      );
    });

    it('fails validation when corrected value equals original value', async () => {
      const wrapper = createCorrectionModalWrapper();

      await wrapper.find('[data-testid="input-corrected-value"]').setValue('Budi Hartono'); // Same as original
      await wrapper.find('[data-testid="textarea-correction-reason"]').setValue('Fixed typo');

      const form = wrapper.find('[data-testid="record-correction-form"]');
      await form.trigger('submit.prevent');
      await flushPromises();

      expect(wrapper.text()).toContain('Corrected value must differ from the original value.');
    });

    it('submits valid correction payload and emits corrected event', async () => {
      const mockCorrectionResult = {
        id: 'corr-001',
        correctionNumber: 'CORR-2026-0001',
        targetTableName: 'teacher_profiles',
        targetRecordId: 'tch-001',
        teacherProfileId: 'tch-001',
        fieldName: 'full_name',
        originalValue: 'Budi Hartono',
        correctedValue: 'Budi Hartono, Ph.D.',
        reasonForCorrection: 'Updated legal doctoral credential title',
        supportingEvidence: 'DOC-2026-0042',
        downstreamImpactReviewed: true,
        effectiveDateImplications: null,
        requestedBy: 'Coordinator Hendra',
        requestedDate: '2026-11-10',
        approvedBy: 'Kepala Sekolah',
        approvedDate: '2026-11-10',
        status: 'applied' as const,
      };

      const applySpy = vi
        .spyOn(facultyApiClient, 'applyCorrection')
        .mockResolvedValueOnce(mockCorrectionResult);

      const wrapper = createCorrectionModalWrapper();

      await wrapper.find('[data-testid="input-corrected-value"]').setValue('Budi Hartono, Ph.D.');
      await wrapper
        .find('[data-testid="textarea-correction-reason"]')
        .setValue('Updated legal doctoral credential title');
      await wrapper.find('[data-testid="input-supporting-evidence"]').setValue('DOC-2026-0042');

      const form = wrapper.find('[data-testid="record-correction-form"]');
      await form.trigger('submit.prevent');
      await flushPromises();

      expect(applySpy).toHaveBeenCalledWith(
        expect.objectContaining({
          targetTableName: 'teacher_profiles',
          targetRecordId: 'tch-001',
          fieldName: 'full_name',
          originalValue: 'Budi Hartono',
          correctedValue: 'Budi Hartono, Ph.D.',
          reasonForCorrection: 'Updated legal doctoral credential title',
          supportingEvidence: 'DOC-2026-0042',
        }),
      );

      expect(wrapper.emitted('corrected')).toBeTruthy();
      expect(wrapper.emitted('corrected')?.[0][0]).toEqual(mockCorrectionResult);
      expect(wrapper.emitted('update:visible')?.[0][0]).toBe(false);
    });
  });
});
