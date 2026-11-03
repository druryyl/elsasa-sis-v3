// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  RecordDecisionModal,
  VerifyDocumentModal,
  AssessmentEvidenceModal,
  WithdrawApplicationModal,
} from '../modals';
import * as admissionApi from '../../api/admission.api';
import type {
  AdmissionPeriodOptionDto,
  ApplicantDocumentDto,
  AssessmentEvidenceDto,
} from '../../types/admission.types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

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
    id: 'period-2',
    academicYearId: 'ay-2027',
    periodCode: 'PPDB-2027-G1',
    name: 'PPDB 2027/2028 Gelombang 1',
    periodType: 'admission',
    startDate: '2027-01-01',
    endDate: '2027-06-30',
    status: 'planned',
  },
];

const mockDocument: ApplicantDocumentDto = {
  id: 'doc-1',
  applicationId: 'app-1',
  documentType: 'birth_certificate',
  submissionStatus: 'submitted',
  fileName: 'akta_budi.pdf',
  fileReference: 'storage/documents/akta_budi.pdf',
  submittedDate: '2026-02-01',
};

const mockEvidence: AssessmentEvidenceDto = {
  id: 'ev-1',
  applicationId: 'app-1',
  assessmentType: 'Reading Readiness',
  score: '88/100',
  source: 'Internal Diagnostic Center',
  assessmentDate: '2026-02-05',
  notes: 'Strong linguistic abilities.',
};

describe('Admission Action Modals', () => {
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
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

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

  const dialogStub = {
    template: `
      <div class="dialog-stub" :data-testid="$attrs['data-testid']">
        <slot />
      </div>
    `,
    props: ['visible'],
  };

  describe('RecordDecisionModal.vue', () => {
    it('renders form fields: outcome, effective date, reason, reference', async () => {
      const wrapper = mount(RecordDecisionModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          periods: mockPeriods,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="record-decision-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="decision-outcome-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="decision-effective-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="decision-reason-textarea"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="decision-reference-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="confirm-decision-button"]').exists()).toBe(true);
    });

    it('displays deferred admission period select when outcome is deferred', async () => {
      const wrapper = mount(RecordDecisionModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          periods: mockPeriods,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="decision-deferred-period-select"]').exists()).toBe(false);

      const outcomeSelect = wrapper.find('[data-testid="decision-outcome-select"]');
      await outcomeSelect.setValue('deferred');
      await flushPromises();

      expect(wrapper.find('[data-testid="decision-deferred-period-select"]').exists()).toBe(true);
    });

    it('submits decision mutation and emits saved event upon confirmation', async () => {
      const decisionSpy = vi.spyOn(admissionApi, 'recordAdmissionDecision').mockResolvedValue({
        decisionId: 'dec-1',
        applicationId: 'app-1',
        decisionType: 'accepted',
        effectiveDate: '2026-03-01',
      });

      const wrapper = mount(RecordDecisionModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          periods: mockPeriods,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="decision-outcome-select"]').setValue('accepted');
      await wrapper.find('[data-testid="decision-effective-date-input"]').setValue('2026-03-01');
      await wrapper.find('[data-testid="decision-reason-textarea"]').setValue('Applicant passed diagnostic testing.');
      await wrapper.find('[data-testid="decision-reference-input"]').setValue('SK-2026-01');

      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(decisionSpy).toHaveBeenCalledWith(
        'app-1',
        expect.objectContaining({
          decisionType: 'accepted',
          effectiveDate: '2026-03-01',
          reason: 'Applicant passed diagnostic testing.',
          supportingReference: 'SK-2026-01',
        }),
      );

      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('update:visible')).toBeTruthy();
    });
  });

  describe('VerifyDocumentModal.vue', () => {
    it('renders document summary and verification status select', async () => {
      const wrapper = mount(VerifyDocumentModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          document: mockDocument,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="verify-document-modal"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Birth Certificate');
      expect(wrapper.text()).toContain('akta_budi.pdf');
      expect(wrapper.find('[data-testid="verify-status-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="verify-notes-textarea"]').exists()).toBe(true);
    });

    it('submits verification determination and emits verified event', async () => {
      const verifySpy = vi.spyOn(admissionApi, 'verifyApplicantDocument').mockResolvedValue({
        id: 'doc-1',
        applicationId: 'app-1',
        documentType: 'birth_certificate',
        submissionStatus: 'verified',
        verifiedBy: 'admission.officer',
        verifiedDate: '2026-02-06',
        notes: 'Document is valid and authentic.',
      });

      const wrapper = mount(VerifyDocumentModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          document: mockDocument,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="verify-status-select"]').setValue('verified');
      await wrapper.find('[data-testid="verify-notes-textarea"]').setValue('Document is valid and authentic.');

      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(verifySpy).toHaveBeenCalledWith(
        'app-1',
        'doc-1',
        {
          submissionStatus: 'verified',
          notes: 'Document is valid and authentic.',
        },
      );

      expect(wrapper.emitted('verified')).toBeTruthy();
      expect(wrapper.emitted('update:visible')).toBeTruthy();
    });
  });

  describe('AssessmentEvidenceModal.vue', () => {
    it('renders empty form for adding new diagnostic evidence', async () => {
      const wrapper = mount(AssessmentEvidenceModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          evidence: null,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="assessment-evidence-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="evidence-type-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="evidence-score-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="evidence-source-input"]').exists()).toBe(true);
    });

    it('submits add evidence mutation and emits saved event', async () => {
      const addSpy = vi.spyOn(admissionApi, 'addAssessmentEvidence').mockResolvedValue({
        id: 'ev-new',
        applicationId: 'app-1',
        assessmentType: 'Math Readiness',
        score: '95/100',
        source: 'Math Olympiad Center',
        assessmentDate: '2026-02-04',
        notes: 'Exceptional quantitative logic.',
        createdAt: '2026-02-05T00:00:00Z',
      });

      const wrapper = mount(AssessmentEvidenceModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          evidence: null,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="evidence-type-input"]').setValue('Math Readiness');
      await wrapper.find('[data-testid="evidence-score-input"]').setValue('95/100');
      await wrapper.find('[data-testid="evidence-source-input"]').setValue('Math Olympiad Center');
      await wrapper.find('[data-testid="evidence-date-input"]').setValue('2026-02-04');
      await wrapper.find('[data-testid="evidence-notes-textarea"]').setValue('Exceptional quantitative logic.');

      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(addSpy).toHaveBeenCalledWith(
        'app-1',
        expect.objectContaining({
          assessmentType: 'Math Readiness',
          score: '95/100',
          source: 'Math Olympiad Center',
          assessmentDate: '2026-02-04',
          notes: 'Exceptional quantitative logic.',
        }),
      );

      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('update:visible')).toBeTruthy();
    });

    it('submits update evidence mutation when evidence prop is provided', async () => {
      const updateSpy = vi.spyOn(admissionApi, 'updateAssessmentEvidence').mockResolvedValue({
        id: 'ev-1',
        applicationId: 'app-1',
        assessmentType: 'Reading Readiness',
        score: '92/100',
        source: 'Internal Diagnostic Center',
        assessmentDate: '2026-02-05',
        notes: 'Score adjusted after re-evaluation.',
      });

      const wrapper = mount(AssessmentEvidenceModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
          evidence: mockEvidence,
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="evidence-score-input"]').setValue('92/100');
      await wrapper.find('[data-testid="evidence-notes-textarea"]').setValue('Score adjusted after re-evaluation.');

      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(updateSpy).toHaveBeenCalledWith(
        'app-1',
        'ev-1',
        expect.objectContaining({
          assessmentType: 'Reading Readiness',
          score: '92/100',
          notes: 'Score adjusted after re-evaluation.',
        }),
      );

      expect(wrapper.emitted('saved')).toBeTruthy();
    });
  });

  describe('WithdrawApplicationModal.vue', () => {
    it('renders withdrawal reason textarea and enforces input', async () => {
      const wrapper = mount(WithdrawApplicationModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="withdraw-application-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="withdraw-reason-textarea"]').exists()).toBe(true);

      const confirmBtn = wrapper.find('[data-testid="confirm-withdraw-button"]');
      expect(confirmBtn.attributes('disabled')).toBeDefined();
    });

    it('submits withdrawal mutation and emits withdrawn event', async () => {
      const withdrawSpy = vi.spyOn(admissionApi, 'withdrawApplication').mockResolvedValue({
        id: 'app-1',
        status: 'withdrawn',
        notes: 'Family relocated out of country.',
      });

      const wrapper = mount(WithdrawApplicationModal, {
        props: {
          visible: true,
          applicationId: 'app-1',
        },
        global: {
          plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
          stubs: { Dialog: dialogStub },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="withdraw-reason-textarea"]').setValue('Family relocated out of country.');
      await wrapper.find('[data-testid="confirm-withdraw-button"]').trigger('click');
      await flushPromises();

      expect(withdrawSpy).toHaveBeenCalledWith(
        'app-1',
        { withdrawalReason: 'Family relocated out of country.' },
      );

      expect(wrapper.emitted('withdrawn')).toBeTruthy();
      expect(wrapper.emitted('update:visible')).toBeTruthy();
    });
  });
});
