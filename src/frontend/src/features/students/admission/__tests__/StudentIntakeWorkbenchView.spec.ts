// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { computed, provide, inject, type Ref } from 'vue';
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import { useAuthStore } from '@/app/stores/auth.store';
import StudentIntakeWorkbenchView from '../views/StudentIntakeWorkbenchView.vue';
import * as admissionApi from '../api/admission.api';
import {
  RecordDecisionModal,
  WithdrawApplicationModal,
  VerifyDocumentModal,
  AssessmentEvidenceModal,
  DocumentVerificationTab,
  AssessmentEvidenceTab,
} from '../components';
import type {
  AdmissionApplicationListItemDto,
  AdmissionApplicationDetailDto,
  AdmissionPeriodOptionDto,
  ApplicantDocumentDto,
  AssessmentEvidenceDto,
} from '../types/admission.types';

import type * as VueRouter from 'vue-router';

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof VueRouter>();
  return {
    ...actual,
    useRoute: () => ({ params: {}, query: {} }),
    useRouter: () => ({ push: vi.fn() }),
  };
});

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
];

const mockApplications: AdmissionApplicationListItemDto[] = [
  {
    id: 'app-1',
    applicationNumber: 'ADM-2026-0001',
    applicantName: 'Budi Santoso',
    governmentIdNumber: '3171010000000001',
    admissionPeriodId: 'period-1',
    requestedEntryLevel: 'Grade 1',
    status: 'registered',
    registrationDate: '2026-02-01',
  },
  {
    id: 'app-2',
    applicationNumber: 'ADM-2026-0002',
    applicantName: 'Siti Rahma',
    governmentIdNumber: '3171010000000002',
    admissionPeriodId: 'period-1',
    requestedEntryLevel: 'Grade 7',
    status: 'under_review',
    registrationDate: '2026-02-02',
  },
  {
    id: 'app-3',
    applicationNumber: 'ADM-2026-0003',
    applicantName: 'Ahmad Dahlan',
    governmentIdNumber: '3171010000000003',
    admissionPeriodId: 'period-1',
    requestedEntryLevel: 'Grade 10',
    status: 'decided',
    registrationDate: '2026-02-03',
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
  requestedCampus: 'Kampus Utama',
  requestedProgram: 'Reguler',
  registrationDate: '2026-02-01',
  status: 'registered',
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
  },
  contacts: [
    {
      id: 'c-1',
      applicationId: 'app-1',
      fullName: 'Santoso Wijaya',
      relationship: 'Father',
      phone: '08111222333',
      email: 'santoso@example.com',
      isPrimary: true,
    },
  ],
  documents: [
    {
      id: 'doc-1',
      applicationId: 'app-1',
      documentType: 'birth_certificate',
      submissionStatus: 'submitted',
      fileName: 'akta_budi.pdf',
      submittedDate: '2026-02-01',
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

describe('StudentIntakeWorkbenchView.vue (SCR-STU-01)', () => {
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

    vi.spyOn(admissionApi, 'getActiveAdmissionPeriods').mockResolvedValue(mockPeriods);
    vi.spyOn(admissionApi, 'getAdmissionApplicationsList').mockResolvedValue(mockApplications);
    vi.spyOn(admissionApi, 'getAdmissionApplicationById').mockResolvedValue(mockDossier);
    vi.spyOn(admissionApi, 'startApplicationReview').mockResolvedValue({
      id: 'app-1',
      status: 'under_review',
    });
    vi.spyOn(admissionApi, 'recordAdmissionDecision').mockResolvedValue({
      decisionId: 'dec-1',
      applicationId: 'app-1',
      decisionType: 'accepted',
      effectiveDate: '2026-03-01',
    });
    vi.spyOn(admissionApi, 'withdrawApplication').mockResolvedValue({
      id: 'app-1',
      status: 'withdrawn',
    });
    vi.spyOn(admissionApi, 'deleteAssessmentEvidence').mockResolvedValue(undefined);

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
        'Permissions.Students.Admission.Decide',
      ],
      campusId: 'campus-1',
    });
  });

  function mountView() {
    return mount(StudentIntakeWorkbenchView, {
      global: {
        plugins: [
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
        ],
        stubs: {
          RouterLink: RouterLinkStub,
          Drawer: {
            template: `
              <div v-if="visible" data-testid="dossier-drawer" class="drawer-stub">
                <slot name="header" />
                <slot />
              </div>
            `,
            props: ['visible'],
          },
          Dialog: {
            template: `
              <div v-if="visible" class="dialog-stub" :data-testid="$attrs['data-testid']">
                <slot name="header" />
                <slot />
                <slot name="footer" />
              </div>
            `,
            props: ['visible'],
          },
          Tabs: {
            props: ['value'],
            emits: ['update:value'],
            template: '<div data-testid="dossier-tabs" class="tabs-stub"><slot /></div>',
            setup(props: any, { emit }: any) {
              const currentVal = computed(() => props.value);
              provide('tabsContext', {
                value: currentVal,
                setValue: (val: string) => emit('update:value', val),
              });
              return {};
            },
          },
          TabList: {
            template: '<div class="tablist-stub"><slot /></div>',
          },
          Tab: {
            props: ['value'],
            template: `
              <button
                type="button"
                class="tab-stub"
                :data-testid="$attrs['data-testid']"
                @click="handleClick"
              >
                <slot />
              </button>
            `,
            setup(props: any) {
              const ctx = inject<{ setValue: (val: string) => void }>('tabsContext');
              const handleClick = () => {
                ctx?.setValue(props.value);
              };
              return { handleClick };
            },
          },
          TabPanels: {
            template: '<div class="tabpanels-stub"><slot /></div>',
          },
          TabPanel: {
            props: ['value'],
            template: `
              <div
                v-if="isActive"
                class="tabpanel-stub"
                :data-testid="$attrs['data-testid']"
              >
                <slot />
              </div>
            `,
            setup(props: any) {
              const ctx = inject<{ value: Ref<string> }>('tabsContext');
              const isActive = computed(() => ctx?.value.value === props.value);
              return { isActive };
            },
          },
        },
      },
    });
  }

  // -------------------------------------------------------------------------
  // 1. Work Queue Rendering Tests
  // -------------------------------------------------------------------------
  describe('Work Queue Rendering', () => {
    it('renders page header with title and total active count badge', async () => {
      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="student-intake-workbench-view"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Student Intake Workbench');

      const activeCountBadge = wrapper.find('[data-testid="active-intake-count-badge"]');
      expect(activeCountBadge.exists()).toBe(true);
      // mockApplications has 2 active (registered and under_review) out of 3
      expect(activeCountBadge.text()).toContain('2 Active Intake Applications');
    });

    it('renders data table populated with admission applications', async () => {
      const wrapper = mountView();
      await flushPromises();

      const dataTable = wrapper.find('[data-testid="intake-datatable"]');
      expect(dataTable.exists()).toBe(true);

      expect(wrapper.text()).toContain('ADM-2026-0001');
      expect(wrapper.text()).toContain('Budi Santoso');
      expect(wrapper.text()).toContain('3171010000000001');
      expect(wrapper.text()).toContain('Grade 1');
      expect(wrapper.text()).toContain('PPDB 2026/2027 Gelombang 1');

      expect(wrapper.text()).toContain('ADM-2026-0002');
      expect(wrapper.text()).toContain('Siti Rahma');

      expect(wrapper.text()).toContain('ADM-2026-0003');
      expect(wrapper.text()).toContain('Ahmad Dahlan');
    });

    it('renders refresh button and triggers queue refetch when clicked', async () => {
      const wrapper = mountView();
      await flushPromises();

      const refreshBtn = wrapper.find('[data-testid="refresh-queue-button"]');
      expect(refreshBtn.exists()).toBe(true);

      const initialCalls = vi.mocked(admissionApi.getAdmissionApplicationsList).mock.calls.length;
      await refreshBtn.trigger('click');
      await flushPromises();

      expect(
        vi.mocked(admissionApi.getAdmissionApplicationsList).mock.calls.length,
      ).toBeGreaterThan(initialCalls);
    });
  });

  // -------------------------------------------------------------------------
  // 2. Filter Reactions Tests
  // -------------------------------------------------------------------------
  describe('Filter Reactions', () => {
    it('renders filter panel with status pills, period select, and search input', async () => {
      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="intake-filter-panel"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="status-pills-bar"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="filter-status-all"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="filter-status-registered"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="filter-status-under_review"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="filter-status-decided"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="filter-status-withdrawn"]').exists()).toBe(true);

      expect(wrapper.find('[data-testid="period-filter-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="entry-level-filter-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="search-applicants-input"]').exists()).toBe(true);
    });

    it('reacts to status pill clicks and updates query parameters', async () => {
      const wrapper = mountView();
      await flushPromises();

      // Click "Registered" pill
      const registeredPill = wrapper.find('[data-testid="filter-status-registered"]');
      await registeredPill.trigger('click');
      await flushPromises();

      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith(
        expect.objectContaining({ status: 'registered' }),
      );

      // Click "Under Review" pill
      const underReviewPill = wrapper.find('[data-testid="filter-status-under_review"]');
      await underReviewPill.trigger('click');
      await flushPromises();

      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith(
        expect.objectContaining({ status: 'under_review' }),
      );

      // Click "All" pill
      const allPill = wrapper.find('[data-testid="filter-status-all"]');
      await allPill.trigger('click');
      await flushPromises();

      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith(
        expect.objectContaining({ status: null }),
      );
    });

    it('reacts to debounced search input changes', async () => {
      vi.useFakeTimers();
      const wrapper = mountView();
      await flushPromises();

      const searchInput = wrapper.find('[data-testid="search-applicants-input"]');
      await searchInput.setValue('Santoso');

      // Fast-forward debounce time (300ms)
      vi.advanceTimersByTime(350);
      await flushPromises();

      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith(
        expect.objectContaining({ searchTerm: 'Santoso' }),
      );

      vi.useRealTimers();
    });

    it('resets all filters when reset filters button is clicked', async () => {
      const wrapper = mountView();
      await flushPromises();

      // Set a filter by clicking a status pill
      await wrapper.find('[data-testid="filter-status-registered"]').trigger('click');
      await flushPromises();

      const resetBtn = wrapper.find('[data-testid="reset-filters-button"]');
      expect(resetBtn.exists()).toBe(true);

      await resetBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="reset-filters-button"]').exists()).toBe(false);
      expect(admissionApi.getAdmissionApplicationsList).toHaveBeenCalledWith(
        expect.objectContaining({
          admissionPeriodId: null,
          status: null,
          requestedEntryLevel: null,
          searchTerm: null,
        }),
      );
    });
  });

  // -------------------------------------------------------------------------
  // 3. Tab Navigation Tests
  // -------------------------------------------------------------------------
  describe('Tab Navigation', () => {
    it('opens dossier drawer and displays applicant summary when View Dossier is clicked', async () => {
      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="dossier-drawer"]').exists()).toBe(false);

      const viewButtons = wrapper.findAll('[data-testid="view-dossier-button"]');
      expect(viewButtons.length).toBeGreaterThan(0);
      await viewButtons[0].trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="dossier-drawer"]').exists()).toBe(true);
      expect(admissionApi.getAdmissionApplicationById).toHaveBeenCalledWith('app-1');

      const dossierHeader = wrapper.find('[data-testid="dossier-header"]');
      expect(dossierHeader.exists()).toBe(true);
      expect(dossierHeader.text()).toContain('ADM-2026-0001');
      expect(dossierHeader.text()).toContain('Budi Santoso');
    });

    it('switches between dossier tabs and activates corresponding panels', async () => {
      const wrapper = mountView();
      await flushPromises();

      // Open dossier
      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      // Initial tab is "info"
      expect(wrapper.find('[data-testid="panel-info"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="panel-documents"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="applicant-summary-card"]').exists()).toBe(true);

      // Navigate to Documents tab
      const tabDocuments = wrapper.find('[data-testid="tab-documents"]');
      expect(tabDocuments.exists()).toBe(true);
      await tabDocuments.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="panel-documents"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="panel-info"]').exists()).toBe(false);

      // Navigate to Assessment tab
      const tabAssessment = wrapper.find('[data-testid="tab-assessment"]');
      expect(tabAssessment.exists()).toBe(true);
      await tabAssessment.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="panel-assessment"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="panel-documents"]').exists()).toBe(false);

      // Navigate to Decision tab
      const tabDecision = wrapper.find('[data-testid="tab-decision"]');
      expect(tabDecision.exists()).toBe(true);
      await tabDecision.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="panel-decision"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="panel-assessment"]').exists()).toBe(false);

      // Navigate back to Info tab
      const tabInfo = wrapper.find('[data-testid="tab-info"]');
      await tabInfo.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="panel-info"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="panel-decision"]').exists()).toBe(false);
    });
  });

  // -------------------------------------------------------------------------
  // 4. Action Modal Triggers Tests
  // -------------------------------------------------------------------------
  describe('Action Modal Triggers & Workflow Actions', () => {
    it('displays Start Review button for registered application and invokes mutation', async () => {
      const wrapper = mountView();
      await flushPromises();

      // Open dossier for registered app-1
      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      const startReviewBtn = wrapper.find('[data-testid="start-review-button"]');
      expect(startReviewBtn.exists()).toBe(true);
      expect(startReviewBtn.text()).toContain('Start Review');

      // Withdraw button also visible for active application
      expect(wrapper.find('[data-testid="withdraw-application-button"]').exists()).toBe(true);

      // Record Decision button not visible for registered application
      expect(wrapper.find('[data-testid="record-decision-button"]').exists()).toBe(false);

      // Click Start Review
      await startReviewBtn.trigger('click');
      await flushPromises();

      expect(admissionApi.startApplicationReview).toHaveBeenCalledWith('app-1');
    });

    it('triggers Record Decision modal when Record Decision button is clicked', async () => {
      vi.spyOn(admissionApi, 'getAdmissionApplicationById').mockResolvedValue({
        ...mockDossier,
        id: 'app-2',
        applicationNumber: 'ADM-2026-0002',
        status: 'under_review',
      });

      const wrapper = mountView();
      await flushPromises();

      // Open dossier for under_review application
      await wrapper.findAll('[data-testid="view-dossier-button"]')[1].trigger('click');
      await flushPromises();

      const recordDecisionBtn = wrapper.find('[data-testid="record-decision-button"]');
      expect(recordDecisionBtn.exists()).toBe(true);

      const decisionModal = wrapper.findComponent(RecordDecisionModal);
      expect(decisionModal.props('visible')).toBe(false);

      await recordDecisionBtn.trigger('click');
      await flushPromises();

      expect(decisionModal.props('visible')).toBe(true);
      expect(decisionModal.props('applicationId')).toBe('app-2');
    });

    it('triggers Withdraw Application modal when Withdraw button is clicked', async () => {
      const wrapper = mountView();
      await flushPromises();

      // Open dossier for registered app-1
      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      const withdrawBtn = wrapper.find('[data-testid="withdraw-application-button"]');
      expect(withdrawBtn.exists()).toBe(true);

      const withdrawModal = wrapper.findComponent(WithdrawApplicationModal);
      expect(withdrawModal.props('visible')).toBe(false);

      await withdrawBtn.trigger('click');
      await flushPromises();

      expect(withdrawModal.props('visible')).toBe(true);
      expect(withdrawModal.props('applicationId')).toBe('app-1');
    });

    it('triggers Verify Document modal when verify event is emitted from DocumentVerificationTab', async () => {
      const wrapper = mountView();
      await flushPromises();

      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      // Switch to documents tab
      await wrapper.find('[data-testid="tab-documents"]').trigger('click');
      await flushPromises();

      const docTab = wrapper.findComponent(DocumentVerificationTab);
      expect(docTab.exists()).toBe(true);

      const verifyModal = wrapper.findComponent(VerifyDocumentModal);
      expect(verifyModal.props('visible')).toBe(false);

      const targetDoc: ApplicantDocumentDto = mockDossier.documents![0];
      docTab.vm.$emit('verify', targetDoc);
      await flushPromises();

      expect(verifyModal.props('visible')).toBe(true);
      expect(verifyModal.props('document')).toEqual(targetDoc);
    });

    it('triggers Assessment Evidence modal for adding new diagnostic score', async () => {
      const wrapper = mountView();
      await flushPromises();

      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      // Switch to assessment tab
      await wrapper.find('[data-testid="tab-assessment"]').trigger('click');
      await flushPromises();

      const assessmentTab = wrapper.findComponent(AssessmentEvidenceTab);
      expect(assessmentTab.exists()).toBe(true);

      const assessmentModal = wrapper.findComponent(AssessmentEvidenceModal);
      expect(assessmentModal.props('visible')).toBe(false);

      assessmentTab.vm.$emit('add');
      await flushPromises();

      expect(assessmentModal.props('visible')).toBe(true);
      expect(assessmentModal.props('evidence')).toBeNull();
    });

    it('triggers Assessment Evidence modal for editing existing diagnostic score', async () => {
      const wrapper = mountView();
      await flushPromises();

      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      // Switch to assessment tab
      await wrapper.find('[data-testid="tab-assessment"]').trigger('click');
      await flushPromises();

      const assessmentTab = wrapper.findComponent(AssessmentEvidenceTab);
      const assessmentModal = wrapper.findComponent(AssessmentEvidenceModal);

      const existingEvidence: AssessmentEvidenceDto = mockDossier.assessmentEvidence![0];
      assessmentTab.vm.$emit('edit', existingEvidence);
      await flushPromises();

      expect(assessmentModal.props('visible')).toBe(true);
      expect(assessmentModal.props('evidence')).toEqual(existingEvidence);
    });

    it('handles deleting diagnostic score and triggers delete mutation', async () => {
      const wrapper = mountView();
      await flushPromises();

      await wrapper.findAll('[data-testid="view-dossier-button"]')[0].trigger('click');
      await flushPromises();

      // Switch to assessment tab
      await wrapper.find('[data-testid="tab-assessment"]').trigger('click');
      await flushPromises();

      const assessmentTab = wrapper.findComponent(AssessmentEvidenceTab);
      assessmentTab.vm.$emit('delete', 'ev-1');
      await flushPromises();

      expect(admissionApi.deleteAssessmentEvidence).toHaveBeenCalledWith('app-1', 'ev-1');
    });
  });
});
