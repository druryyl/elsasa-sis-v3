// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  ApplicationInfoTab,
  DocumentVerificationTab,
  AssessmentEvidenceTab,
  DecisionHistoryTab,
} from '../tabs';
import type {
  AdmissionApplicationDetailDto,
  AdmissionPeriodOptionDto,
} from '../../types/admission.types';

const mockPeriods: AdmissionPeriodOptionDto[] = [
  {
    id: 'period-1',
    academicYearId: 'ay-2026',
    periodCode: 'PPDB-2026-G1',
    name: 'PPDB 2026/2027 Gelombang 1',
    periodType: 'admission',
    startDate: '2026-01-01',
    endDate: '2026-06-30',
    status: 'active',
  },
  {
    id: 'period-deferred',
    academicYearId: 'ay-2027',
    periodCode: 'PPDB-2027-G1',
    name: 'PPDB 2027/2028 Gelombang 1',
    periodType: 'admission',
    startDate: '2027-01-01',
    endDate: '2027-06-30',
    status: 'planned',
  },
];

const mockDossier: AdmissionApplicationDetailDto = {
  id: 'app-1',
  applicationNumber: 'ADM-2026-0001',
  applicantId: 'applicant-1',
  admissionPeriodId: 'period-1',
  academicYearId: 'ay-2026',
  applicationType: 'new',
  requestedEntryLevel: 'Grade 1',
  requestedCampus: 'Main Campus',
  requestedProgram: 'Bilingual',
  registrationDate: '2026-02-01',
  status: 'under_review',
  notes: 'Priority consideration requested by counselor.',
  createdAt: '2026-02-01T08:00:00Z',
  applicant: {
    id: 'applicant-1',
    fullName: 'Budi Santoso',
    preferredName: 'Budi',
    dateOfBirth: '2019-05-12',
    placeOfBirth: 'Jakarta',
    gender: 'Male',
    nationality: 'Indonesian',
    governmentIdType: 'NIK',
    governmentIdNumber: '3171010000000001',
    email: 'budi.santoso@example.com',
    phone: '081234567890',
    address: 'Jl. Merdeka No. 10, Jakarta Pusat',
    previousSchoolName: 'TK Al-Falah',
    previousSchoolAddress: 'Jl. Melati No. 5, Jakarta',
    notes: 'Mild peanut allergy noted.',
  },
  contacts: [
    {
      id: 'c-1',
      applicationId: 'app-1',
      fullName: 'Santoso Wijaya',
      relationship: 'Father',
      phone: '08111222333',
      email: 'santoso@example.com',
      address: 'Jl. Merdeka No. 10, Jakarta Pusat',
      isPrimary: true,
    },
    {
      id: 'c-2',
      applicationId: 'app-1',
      fullName: 'Dewi Lestari',
      relationship: 'Mother',
      phone: '08111444555',
      email: 'dewi@example.com',
      address: 'Jl. Merdeka No. 10, Jakarta Pusat',
      isPrimary: false,
    },
  ],
  documents: [
    {
      id: 'doc-1',
      applicationId: 'app-1',
      documentType: 'birth_certificate',
      submissionStatus: 'submitted',
      fileName: 'akta_budi.pdf',
      fileReference: 'storage/documents/akta_budi.pdf',
      submittedDate: '2026-02-01',
    },
    {
      id: 'doc-2',
      applicationId: 'app-1',
      documentType: 'family_card',
      submissionStatus: 'verified',
      fileName: 'kk_keluarga.pdf',
      fileReference: 'storage/documents/kk_keluarga.pdf',
      submittedDate: '2026-02-01',
      verifiedDate: '2026-02-02',
      verifiedBy: 'officer.admin',
      notes: 'Matches Dukcapil registry perfectly.',
    },
  ],
  assessmentEvidence: [
    {
      id: 'ev-1',
      applicationId: 'app-1',
      assessmentType: 'Reading Readiness',
      score: '88/100',
      source: 'Internal Diagnostic Center',
      assessmentDate: '2026-02-05',
      notes: 'Strong reading readiness and linguistic comprehension.',
    },
  ],
  decision: null,
};

describe('Dossier Tabs Components', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'officer-1',
      username: 'admission.officer',
      fullName: 'Admission Officer',
      roles: ['AdmissionOfficer'],
      permissions: [
        'Permissions.Students.Admission.View',
        'Permissions.Students.Admission.Register',
        'Permissions.Students.Admission.Review',
        'Permissions.Students.Admission.VerifyDocument',
        'Permissions.Students.Admission.Decide',
      ],
      campusId: 'campus-1',
    });
  });

  describe('ApplicationInfoTab.vue', () => {
    it('renders applicant biographical information and preferences', () => {
      const wrapper = mount(ApplicationInfoTab, {
        props: {
          dossier: mockDossier,
          periods: mockPeriods,
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="application-info-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="applicant-summary-card"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Budi Santoso');
      expect(wrapper.text()).toContain('3171010000000001');
      expect(wrapper.text()).toContain('TK Al-Falah');
      expect(wrapper.text()).toContain('PPDB 2026/2027 Gelombang 1');
      expect(wrapper.text()).toContain('Bilingual');
      expect(wrapper.text()).toContain('Mild peanut allergy noted.');
    });

    it('renders guardian and household contacts with primary indicator', () => {
      const wrapper = mount(ApplicationInfoTab, {
        props: {
          dossier: mockDossier,
          periods: mockPeriods,
        },
        global: { plugins: [PrimeVue] },
      });

      const contacts = wrapper.findAll('[data-testid="guardian-contact-item"]');
      expect(contacts.length).toBe(2);
      expect(wrapper.text()).toContain('Santoso Wijaya');
      expect(wrapper.text()).toContain('Primary Guardian');
      expect(wrapper.text()).toContain('Dewi Lestari');
    });

    it('renders empty notice when no contacts are attached', () => {
      const wrapper = mount(ApplicationInfoTab, {
        props: {
          dossier: {
            ...mockDossier,
            contacts: [],
          },
          periods: mockPeriods,
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.text()).toContain('No guardian contacts recorded.');
    });
  });

  describe('DocumentVerificationTab.vue', () => {
    it('renders document checklist table with metadata and verification details', () => {
      const wrapper = mount(DocumentVerificationTab, {
        props: {
          applicationId: 'app-1',
          documents: mockDossier.documents,
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="document-verification-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="documents-checklist-card"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Birth Certificate');
      expect(wrapper.text()).toContain('akta_budi.pdf');
      expect(wrapper.text()).toContain('Family Card');
      expect(wrapper.text()).toContain('officer.admin');
      expect(wrapper.text()).toContain('Matches Dukcapil registry perfectly.');
    });

    it('opens file when Download/View button is clicked', () => {
      const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null);

      const wrapper = mount(DocumentVerificationTab, {
        props: {
          applicationId: 'app-1',
          documents: mockDossier.documents,
        },
        global: { plugins: [PrimeVue] },
      });

      const viewBtns = wrapper.findAll('[data-testid="download-document-button"]');
      expect(viewBtns.length).toBe(2);
      viewBtns[0].trigger('click');

      expect(openSpy).toHaveBeenCalledWith(
        expect.stringContaining('/students/admission/applications/app-1/documents/doc-1/file'),
        '_blank',
      );
    });

    it('emits verify event when Verify action button is clicked', async () => {
      const wrapper = mount(DocumentVerificationTab, {
        props: {
          applicationId: 'app-1',
          documents: mockDossier.documents,
        },
        global: { plugins: [PrimeVue] },
      });

      const verifyBtns = wrapper.findAll('[data-testid="verify-document-button"]');
      expect(verifyBtns.length).toBe(2);
      await verifyBtns[0].trigger('click');

      expect(wrapper.emitted('verify')).toBeTruthy();
      expect(wrapper.emitted('verify')![0][0]).toEqual(mockDossier.documents[0]);
    });

    it('renders empty placeholder when no documents are uploaded', () => {
      const wrapper = mount(DocumentVerificationTab, {
        props: {
          applicationId: 'app-1',
          documents: [],
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="no-documents-empty"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No credential documents attached.');
    });
  });

  describe('AssessmentEvidenceTab.vue', () => {
    it('renders manual diagnostic assessment evidence score cards', () => {
      const wrapper = mount(AssessmentEvidenceTab, {
        props: {
          applicationId: 'app-1',
          assessmentEvidence: mockDossier.assessmentEvidence,
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="assessment-evidence-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="assessment-evidence-card"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Reading Readiness');
      expect(wrapper.text()).toContain('88/100');
      expect(wrapper.text()).toContain('Internal Diagnostic Center');
      expect(wrapper.text()).toContain('Strong reading readiness');
    });

    it('emits add event when Add Score button is clicked', async () => {
      const wrapper = mount(AssessmentEvidenceTab, {
        props: {
          applicationId: 'app-1',
          assessmentEvidence: mockDossier.assessmentEvidence,
        },
        global: { plugins: [PrimeVue] },
      });

      const addBtn = wrapper.find('[data-testid="add-score-button"]');
      await addBtn.trigger('click');

      expect(wrapper.emitted('add')).toBeTruthy();
    });

    it('emits edit and delete events when score action buttons are clicked', async () => {
      const wrapper = mount(AssessmentEvidenceTab, {
        props: {
          applicationId: 'app-1',
          assessmentEvidence: mockDossier.assessmentEvidence,
        },
        global: { plugins: [PrimeVue] },
      });

      const editBtn = wrapper.find('[data-testid="edit-score-button"]');
      await editBtn.trigger('click');
      expect(wrapper.emitted('edit')).toBeTruthy();
      expect(wrapper.emitted('edit')![0][0]).toEqual(mockDossier.assessmentEvidence[0]);

      const deleteBtn = wrapper.find('[data-testid="delete-score-button"]');
      await deleteBtn.trigger('click');
      expect(wrapper.emitted('delete')).toBeTruthy();
      expect(wrapper.emitted('delete')![0][0]).toBe('ev-1');
    });

    it('renders empty placeholder when no diagnostic evidence exists', () => {
      const wrapper = mount(AssessmentEvidenceTab, {
        props: {
          applicationId: 'app-1',
          assessmentEvidence: [],
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="no-assessment-evidence-empty"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No diagnostic assessment evidence recorded yet.');
    });
  });

  describe('DecisionHistoryTab.vue', () => {
    it('renders lifecycle guidance when no decision has been recorded yet', () => {
      const wrapper = mount(DecisionHistoryTab, {
        props: {
          dossier: mockDossier,
          periods: mockPeriods,
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="decision-history-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="no-decision-guidance"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Decision Pending');
      expect(wrapper.text()).toContain('Under Review');
    });

    it('renders official immutable decision details with deferred period link when deferred', () => {
      const decidedDossier: AdmissionApplicationDetailDto = {
        ...mockDossier,
        status: 'decided',
        decision: {
          id: 'dec-1',
          applicationId: 'app-1',
          decisionType: 'deferred',
          decisionDate: '2026-03-01',
          effectiveDate: '2026-07-01',
          decidedBy: 'Admission Committee Head',
          reason: 'Applicant will reach minimum required age for 2027 intake cohort.',
          supportingReference: 'SK-PPDB-DEFER-042',
          deferredAdmissionPeriodId: 'period-deferred',
        },
      };

      const wrapper = mount(DecisionHistoryTab, {
        props: {
          dossier: decidedDossier,
          periods: mockPeriods,
        },
        global: { plugins: [PrimeVue] },
      });

      expect(wrapper.find('[data-testid="recorded-decision-content"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Admission Committee Head');
      expect(wrapper.text()).toContain('SK-PPDB-DEFER-042');
      expect(wrapper.text()).toContain('Applicant will reach minimum required age');
      expect(wrapper.text()).toContain('PPDB 2027/2028 Gelombang 1');
      expect(wrapper.text()).toContain('legally immutable');
    });
  });
});
