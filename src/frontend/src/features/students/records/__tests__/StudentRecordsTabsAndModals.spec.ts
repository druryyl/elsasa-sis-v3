// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  StudentIdentityTab,
  EditIdentityModal,
  StudentGuardiansTab,
  AddGuardianModal,
  EditGuardianModal,
  StudentDocumentsTab,
  UploadDocumentModal,
  VerifyDocumentModal,
  StudentEnrollmentHistoryTab,
} from '../components';
import * as recordsApi from '../api/records.api';
import type { StudentDetail, GuardianRelationshipItem, StudentDocumentItem } from '../types/records.types';

const mockStudent: StudentDetail = {
  id: 'student-100',
  studentNumber: 'STD-2026-0042',
  applicantId: 'applicant-100',
  fullName: 'Farhan Muhammad',
  preferredName: 'Farhan',
  dateOfBirth: '2018-03-20',
  placeOfBirth: 'Bandung',
  gender: 'Male',
  nationality: 'Indonesian',
  governmentIdType: 'NIK',
  governmentIdNumber: '3273010000000042',
  address: 'Jl. Dago No. 42, Bandung',
  phone: '081299998888',
  email: 'farhan@example.com',
  isIndependent: true,
  currentStatus: 'enrolled',
  enrollmentDate: '2026-07-01',
  notes: 'Transferred with excellent marks.',
  createdAt: '2026-07-01T08:00:00Z',
  createdBy: 'admission_officer',
  updatedAt: '2026-07-02T10:00:00Z',
  updatedBy: 'records_officer',
  enrollmentHistory: [
    {
      enrollmentDate: '2026-07-01',
      campus: 'Main Campus',
      level: 'Grade 1',
      academicYear: '2026/2027',
      program: 'Bilingual',
      cohort: '1B',
      approvedBy: 'admission_officer',
      paymentConfirmed: false,
      paymentReference: null,
      paymentWaived: true,
      waiverReference: 'SK-YAYASAN-2026-09',
    },
  ],
  guardianRelationships: [
    {
      relationshipId: 'rel-1',
      guardianId: 'guard-1',
      fullName: 'Bambang Sudarsono',
      relationshipType: 'father',
      governmentIdType: 'NIK',
      governmentIdNumber: '3273010000000001',
      phone: '081211112222',
      email: 'bambang@example.com',
      address: 'Jl. Dago No. 42, Bandung',
      occupation: 'Lecturer',
      isPrimaryContact: true,
      isEmergencyContact: true,
      isFinancialContact: true,
      isAuthorizedPickup: true,
      isActive: true,
    },
    {
      relationshipId: 'rel-2',
      guardianId: 'guard-2',
      fullName: 'Dewi Lestari',
      relationshipType: 'mother',
      governmentIdType: 'NIK',
      governmentIdNumber: '3273010000000002',
      phone: '081233334444',
      email: 'dewi@example.com',
      address: 'Jl. Dago No. 42, Bandung',
      occupation: 'Doctor',
      isPrimaryContact: false,
      isEmergencyContact: true,
      isFinancialContact: false,
      isAuthorizedPickup: true,
      isActive: true,
    },
  ],
  documents: [
    {
      id: 'doc-1',
      documentType: 'Birth Certificate',
      submissionStatus: 'verified',
      submittedDate: '2026-07-01',
      verifiedDate: '2026-07-02',
      verifiedBy: 'officer_jane',
      fileName: 'farhan_birth_cert.pdf',
      fileSizeBytes: 1048576,
      contentType: 'application/pdf',
      fileReference: 'storage/doc1.pdf',
    },
    {
      id: 'doc-2',
      documentType: 'Family Card',
      submissionStatus: 'submitted',
      submittedDate: '2026-07-03',
      verifiedDate: null,
      verifiedBy: null,
      fileName: 'kk_farhan.jpg',
      fileSizeBytes: 524288,
      contentType: 'image/jpeg',
      fileReference: 'storage/doc2.jpg',
    },
  ],
};

describe('Student Records Presentation Tabs & Action Modals (P10-S17)', () => {
  let queryClient: QueryClient;
  let pinia: ReturnType<typeof createPinia>;

  beforeEach(() => {
    vi.clearAllMocks();

    if (typeof window !== 'undefined') {
      window.ResizeObserver =
        window.ResizeObserver ||
        vi.fn().mockImplementation(() => ({
          observe: vi.fn(),
          unobserve: vi.fn(),
          disconnect: vi.fn(),
        }));
      window.matchMedia =
        window.matchMedia ||
        vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        }));
    }

    pinia = createPinia();
    setActivePinia(pinia);

    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      id: 'u-1',
      username: 'officer_user',
      email: 'officer@example.com',
      displayName: 'Officer User',
      roles: ['StudentAdminOfficer'],
      permissions: [
        'Permissions.Students.Records.View',
        'Permissions.Students.Records.Edit',
      ],
      isActive: true,
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
  });

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

  // -------------------------------------------------------------------------
  // 1. StudentIdentityTab.vue
  // -------------------------------------------------------------------------
  describe('StudentIdentityTab.vue', () => {
    it('renders certified biographical attributes, national ID, address, and audit timestamps', () => {
      const wrapper = mount(StudentIdentityTab, {
        props: {
          student: mockStudent,
          canEdit: true,
        },
        global: {
          plugins: [pinia, PrimeVue],
        },
      });

      expect(wrapper.find('[data-testid="identity-full-name"]').text()).toBe('Farhan Muhammad');
      expect(wrapper.find('[data-testid="identity-preferred-name"]').text()).toBe('Farhan');
      expect(wrapper.find('[data-testid="identity-government-id"]').text()).toBe('3273010000000042');
      expect(wrapper.find('[data-testid="identity-address"]').text()).toBe('Jl. Dago No. 42, Bandung');
      expect(wrapper.find('[data-testid="identity-phone"]').text()).toBe('081299998888');
      expect(wrapper.find('[data-testid="identity-email"]').text()).toBe('farhan@example.com');
      expect(wrapper.find('[data-testid="identity-independent-badge"]').text()).toContain('Yes — Self-managed learner');
      expect(wrapper.find('[data-testid="identity-created-audit"]').text()).toContain('admission_officer');
      expect(wrapper.find('[data-testid="identity-updated-audit"]').text()).toContain('records_officer');
    });

    it('emits edit event when Edit Identity button is clicked', async () => {
      const wrapper = mount(StudentIdentityTab, {
        props: {
          student: mockStudent,
          canEdit: true,
        },
        global: {
          plugins: [pinia, PrimeVue],
        },
      });

      const editBtn = wrapper.find('[data-testid="tab-edit-identity-button"]');
      expect(editBtn.exists()).toBe(true);
      await editBtn.trigger('click');
      expect(wrapper.emitted('edit')).toHaveLength(1);
    });
  });

  // -------------------------------------------------------------------------
  // 2. EditIdentityModal.vue
  // -------------------------------------------------------------------------
  describe('EditIdentityModal.vue', () => {
    it('pre-populates form fields and submits updateIdentity mutation', async () => {
      const updateSpy = vi.spyOn(recordsApi, 'updateStudentIdentity').mockResolvedValue();

      const wrapper = mount(EditIdentityModal, {
        props: {
          visible: true,
          student: mockStudent,
        },
        global: {
          plugins: [pinia, [VueQueryPlugin, { queryClient }], PrimeVue, ToastService],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      const nameInput = wrapper.find('[data-testid="input-fullname"]');
      expect((nameInput.element as HTMLInputElement).value).toBe('Farhan Muhammad');

      const saveBtn = wrapper.find('[data-testid="btn-save-identity"]');
      await saveBtn.trigger('click');
      await flushPromises();

      expect(updateSpy).toHaveBeenCalledWith('student-100', expect.objectContaining({
        fullName: 'Farhan Muhammad',
        governmentIdNumber: '3273010000000042',
        isIndependent: true,
      }));
      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });
  });

  // -------------------------------------------------------------------------
  // 3. StudentGuardiansTab.vue
  // -------------------------------------------------------------------------
  describe('StudentGuardiansTab.vue', () => {
    it('renders guardian cards with designated responsibility badges (Primary, Emergency, Financial, Pickup)', () => {
      const wrapper = mount(StudentGuardiansTab, {
        props: {
          student: mockStudent,
          canEdit: true,
        },
        global: {
          plugins: [pinia, [VueQueryPlugin, { queryClient }], PrimeVue, ToastService],
          stubs: { Dialog: dialogStub },
        },
      });

      const cards = wrapper.findAll('[data-testid^="guardian-card-"]');
      expect(cards).toHaveLength(2);

      const firstCard = wrapper.find('[data-testid="guardian-card-rel-1"]');
      expect(firstCard.find('[data-testid="badge-primary-contact"]').exists()).toBe(true);
      expect(firstCard.find('[data-testid="badge-emergency-contact"]').exists()).toBe(true);
      expect(firstCard.find('[data-testid="badge-financial-contact"]').exists()).toBe(true);
      expect(firstCard.find('[data-testid="badge-pickup-contact"]').exists()).toBe(true);

      const secondCard = wrapper.find('[data-testid="guardian-card-rel-2"]');
      expect(secondCard.find('[data-testid="badge-primary-contact"]').exists()).toBe(false);
      expect(secondCard.find('[data-testid="btn-set-primary"]').exists()).toBe(true);
    });

    it('triggers primary contact transfer when Set as Primary Contact is clicked', async () => {
      const updateGuardianSpy = vi.spyOn(recordsApi, 'updateGuardianRelationship').mockResolvedValue({
        relationshipId: 'rel-2',
        guardianId: 'guard-2',
        isPrimaryContact: true,
        isActive: true,
      });

      const wrapper = mount(StudentGuardiansTab, {
        props: {
          student: mockStudent,
          canEdit: true,
        },
        global: {
          plugins: [pinia, [VueQueryPlugin, { queryClient }], PrimeVue, ToastService],
          stubs: { Dialog: dialogStub },
        },
      });

      const secondCard = wrapper.find('[data-testid="guardian-card-rel-2"]');
      const setPrimaryBtn = secondCard.find('[data-testid="btn-set-primary"]');
      await setPrimaryBtn.trigger('click');
      await flushPromises();

      expect(updateGuardianSpy).toHaveBeenCalledWith(
        'student-100',
        'rel-2',
        expect.objectContaining({ isPrimaryContact: true }),
      );
    });
  });

  // -------------------------------------------------------------------------
  // 4. AddGuardianModal.vue
  // -------------------------------------------------------------------------
  describe('AddGuardianModal.vue', () => {
    it('validates required fields and invokes addGuardian mutation', async () => {
      const addSpy = vi.spyOn(recordsApi, 'addGuardianRelationship').mockResolvedValue('rel-new-123');

      const wrapper = mount(AddGuardianModal, {
        props: {
          visible: true,
          studentId: 'student-100',
        },
        global: {
          plugins: [pinia, [VueQueryPlugin, { queryClient }], PrimeVue, ToastService],
          stubs: { Dialog: dialogStub },
        },
      });

      const nameInput = wrapper.find('[data-testid="input-guardian-name"]');
      await nameInput.setValue('Nenek Siti');

      const phoneInput = wrapper.find('[data-testid="input-guardian-phone"]');
      await phoneInput.setValue('081200001111');

      const saveBtn = wrapper.find('[data-testid="btn-save-guardian"]');
      await saveBtn.trigger('click');
      await flushPromises();

      expect(addSpy).toHaveBeenCalledWith(
        'student-100',
        expect.objectContaining({
          fullName: 'Nenek Siti',
          phone: '081200001111',
        }),
      );
      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });
  });

  // -------------------------------------------------------------------------
  // 5. StudentDocumentsTab.vue
  // -------------------------------------------------------------------------
  describe('StudentDocumentsTab.vue', () => {
    it('renders checklist table with document type, verification status, and stream download link', () => {
      const wrapper = mount(StudentDocumentsTab, {
        props: {
          student: mockStudent,
          canEdit: true,
        },
        global: {
          plugins: [pinia, [VueQueryPlugin, { queryClient }], PrimeVue, ToastService],
          stubs: { Dialog: dialogStub },
        },
      });

      const docRows = wrapper.findAll('[data-testid^="document-item-"]');
      expect(docRows).toHaveLength(2);

      const firstRow = wrapper.find('[data-testid="document-item-doc-1"]');
      expect(firstRow.find('[data-testid="document-type"]').text()).toBe('Birth Certificate');
      expect(firstRow.find('[data-testid="document-status"]').exists()).toBe(true);
      expect(firstRow.find('[data-testid="document-filename"]').text()).toBe('farhan_birth_cert.pdf');
      expect(firstRow.find('[data-testid="btn-download-document"]').attributes('href')).toContain('/api/students/student-100/documents/doc-1/file');
    });
  });

  // -------------------------------------------------------------------------
  // 6. VerifyDocumentModal.vue
  // -------------------------------------------------------------------------
  describe('VerifyDocumentModal.vue', () => {
    it('submits verification decision and notes', async () => {
      const verifySpy = vi.spyOn(recordsApi, 'verifyStudentDocument').mockResolvedValue();

      const docToVerify: StudentDocumentItem = {
        id: 'doc-2',
        documentType: 'Family Card',
        submissionStatus: 'submitted',
        submittedDate: '2026-07-03',
        verifiedDate: null,
        verifiedBy: null,
        fileName: 'kk_farhan.jpg',
      };

      const wrapper = mount(VerifyDocumentModal, {
        props: {
          visible: true,
          studentId: 'student-100',
          document: docToVerify,
        },
        global: {
          plugins: [pinia, [VueQueryPlugin, { queryClient }], PrimeVue, ToastService],
          stubs: { Dialog: dialogStub },
        },
      });

      const notesInput = wrapper.find('[data-testid="input-verify-notes"]');
      await notesInput.setValue('Verified authentic family card.');

      const submitBtn = wrapper.find('[data-testid="btn-submit-verify"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(verifySpy).toHaveBeenCalledWith('student-100', 'doc-2', {
        submissionStatus: 'verified',
        notes: 'Verified authentic family card.',
      });
      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
    });
  });

  // -------------------------------------------------------------------------
  // 7. StudentEnrollmentHistoryTab.vue
  // -------------------------------------------------------------------------
  describe('StudentEnrollmentHistoryTab.vue', () => {
    it('renders read-only placement parameters, payment clearance snapshot, and originating application link', () => {
      const wrapper = mount(StudentEnrollmentHistoryTab, {
        props: {
          student: mockStudent,
        },
        global: {
          plugins: [pinia, PrimeVue],
          stubs: {
            RouterLink: RouterLinkStub,
          },
        },
      });

      expect(wrapper.find('[data-testid="history-placement-campus"]').text()).toBe('Main Campus');
      expect(wrapper.find('[data-testid="history-placement-level"]').text()).toBe('Grade 1');
      expect(wrapper.find('[data-testid="history-placement-year"]').text()).toBe('2026/2027');
      expect(wrapper.find('[data-testid="history-placement-program"]').text()).toBe('Bilingual (1B)');
      expect(wrapper.find('[data-testid="history-approved-by"]').text()).toContain('admission_officer');
      expect(wrapper.find('[data-testid="payment-gate-waived"]').text()).toContain('SK-YAYASAN-2026-09');

      const link = wrapper.findComponent(RouterLinkStub);
      expect(link.exists()).toBe(true);
      expect(link.props().to).toBe('/students/admission');
    });
  });
});
