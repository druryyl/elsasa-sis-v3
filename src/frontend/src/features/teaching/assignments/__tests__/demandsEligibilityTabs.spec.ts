// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import CreateDemandModal from '../components/CreateDemandModal.vue';
import DeclineDemandModal from '../components/DeclineDemandModal.vue';
import RequestClarificationModal from '../components/RequestClarificationModal.vue';
import DemandsIntakeTab from '../components/DemandsIntakeTab.vue';
import CandidateEligibilityTab from '../components/CandidateEligibilityTab.vue';
import TeachingAssignmentWorkspaceView from '../views/TeachingAssignmentWorkspaceView.vue';
import * as assignmentsApi from '../api/assignmentsApi';
import type {
  TeachingDemandDto,
  TeacherEligibilityEvaluationDto,
  CandidateEvaluationSummaryDto,
  CreateTeachingDemandResponse,
} from '../types';

// Mock vue-router
const mockReplace = vi.fn();
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRoute: () => ({
      query: {
        tab: 'demands',
        periodId: '00000000-0000-0000-0002-000000000002',
        campus: 'all',
      },
    }),
    useRouter: () => ({
      replace: mockReplace,
    }),
  };
});

const commonStubs = {
  RouterLink: {
    props: ['to'],
    template: '<a :href="typeof to === \'string\' ? to : \'#\'"><slot /></a>',
  },
  Dialog: {
    props: ['visible', 'header'],
    emits: ['update:visible'],
    template: `
      <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid']">
        <div class="p-dialog-header">{{ header }}</div>
        <div class="p-dialog-content"><slot /></div>
        <div class="p-dialog-footer"><slot name="footer" /></div>
      </div>
    `,
  },
};

const sampleDemands: TeachingDemandDto[] = [
  {
    id: '00000000-0000-0000-0003-000000000001',
    demandCode: 'DMD-2027-001',
    sourceDomain: 'Academics',
    sourceWorkReference: '00000000-0000-0000-0001-000000000001',
    sourceWorkLabel: 'Matematika SD Kelas 4A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    requiredCapabilityCode: 'CAP-MATH-PRI',
    requiredAssignmentRoleId: '00000000-0000-0000-0004-000000000001',
    requiredAssignmentRoleName: 'Guru Utama',
    expectedWorkloadHours: 5.0,
    workloadUnit: 'Teaching Hours',
    requiresAssessmentAuthority: true,
    requestedStartDate: '2027-01-05',
    requestedEndDate: '2027-06-25',
    campusScope: 'Kampus Utama',
    requiredTeachersCount: 1,
    status: 'received',
    requestedBy: 'coordinator@hagios.sch.id',
    requestedAt: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
  {
    id: '00000000-0000-0000-0003-000000000002',
    demandCode: 'DMD-2027-002',
    sourceDomain: 'Academics',
    sourceWorkReference: '00000000-0000-0000-0001-000000000002',
    sourceWorkLabel: 'Bahasa Inggris SMP Kelas 7A',
    academicPeriodId: '00000000-0000-0000-0002-000000000002',
    requiredCapabilityCode: 'CAP-ENG-SEC',
    requiredAssignmentRoleId: '00000000-0000-0000-0004-000000000002',
    requiredAssignmentRoleName: 'Guru Pendamping',
    expectedWorkloadHours: 4.0,
    workloadUnit: 'Teaching Hours',
    requiresAssessmentAuthority: true,
    requestedStartDate: '2027-01-05',
    requestedEndDate: '2027-06-25',
    campusScope: 'Kampus Utama',
    requiredTeachersCount: 1,
    status: 'accepted_for_staffing',
    requestedBy: 'coordinator@hagios.sch.id',
    requestedAt: '2026-11-10T00:00:00Z',
    createdAt: '2026-11-10T00:00:00Z',
  },
];

const sampleEvaluation: TeacherEligibilityEvaluationDto = {
  id: '00000000-0000-0000-0005-000000000001',
  teachingDemandId: '00000000-0000-0000-0003-000000000002',
  teacherProfileId: '00000000-0000-0000-0006-000000000002',
  evaluationDate: '2026-11-10',
  evaluatedBy: 'coordinator@hagios.sch.id',
  isEligible: true,
  eligibilityStatus: 'eligible',
  profileActiveCheck: true,
  authorizationCheckPassed: true,
  qualificationCheckPassed: true,
  availabilityCheckPassed: true,
  capacityCheckPassed: true,
  explanation: 'Memiliki otorisasi aktif Bahasa Inggris SMP, ketersediaan jadwal terverifikasi, sisa kapasitas 6 jam.',
  notes: 'Recommended for co-teaching.',
  createdAt: '2026-11-10T00:00:00Z',
};

const sampleEvaluationSummary: CandidateEvaluationSummaryDto[] = [
  {
    id: sampleEvaluation.id,
    teachingDemandId: sampleEvaluation.teachingDemandId,
    demandCode: 'DMD-2027-002',
    teacherProfileId: sampleEvaluation.teacherProfileId,
    teacherName: 'Siti Nurhaliza, S.Pd.',
    teacherNumber: 'TCH-002',
    evaluationDate: sampleEvaluation.evaluationDate,
    evaluatedBy: sampleEvaluation.evaluatedBy,
    isEligible: sampleEvaluation.isEligible,
    eligibilityStatus: sampleEvaluation.eligibilityStatus,
    profileActiveCheck: sampleEvaluation.profileActiveCheck,
    authorizationCheckPassed: sampleEvaluation.authorizationCheckPassed,
    qualificationCheckPassed: sampleEvaluation.qualificationCheckPassed,
    availabilityCheckPassed: sampleEvaluation.availabilityCheckPassed,
    capacityCheckPassed: sampleEvaluation.capacityCheckPassed,
    explanation: sampleEvaluation.explanation,
    notes: sampleEvaluation.notes,
    createdAt: sampleEvaluation.createdAt,
  },
];

describe('SCR-TCH-02 Assignments Workbench - Demands Intake & Eligibility Tabs (P8-S24)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    setActivePinia(createPinia());

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

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

    // Default mock implementations
    vi.spyOn(assignmentsApi, 'getTeachingDemands').mockResolvedValue(sampleDemands);
    vi.spyOn(assignmentsApi, 'getAssignmentRoles').mockResolvedValue([
      {
        id: '00000000-0000-0000-0004-000000000001',
        code: 'LEAD_TEACHER',
        name: 'Guru Utama',
        isLeadRole: true,
        countsTowardWorkload: true,
        isActive: true,
        createdAt: '2026-11-10T00:00:00Z',
      },
    ]);
    vi.spyOn(assignmentsApi, 'getCandidateEvaluations').mockResolvedValue(sampleEvaluationSummary);
    vi.spyOn(assignmentsApi, 'getTeachingAssignments').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'getCapacityConflicts').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'getTeachingSubstitutions').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'getTeachingReassignments').mockResolvedValue([]);
    vi.spyOn(assignmentsApi, 'getAssignmentCompletions').mockResolvedValue([]);
  });

  function createWrapper(component: any, props: any = {}) {
    return mount(component, {
      props,
      global: {
        plugins: [
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
          ToastService,
        ],
        stubs: commonStubs,
      },
    });
  }

  describe('CreateDemandModal.vue', () => {
    it('renders form fields when visible is true', async () => {
      const wrapper = createWrapper(CreateDemandModal, { visible: true });
      await flushPromises();

      expect(wrapper.find('[data-testid="create-demand-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-source-work-label"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-capability-code"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="submit-create-demand-btn"]').exists()).toBe(true);
    });

    it('submits mutation with valid inputs and emits submitted', async () => {
      const mockCreateResponse: CreateTeachingDemandResponse = {
        id: 'new-demand-id',
        demandCode: 'DMD-2027-NEW',
        status: 'received',
        createdAt: '2026-11-10T00:00:00Z',
      };
      const createSpy = vi.spyOn(assignmentsApi, 'createTeachingDemand').mockResolvedValue(mockCreateResponse);

      const wrapper = createWrapper(CreateDemandModal, { visible: true });
      await flushPromises();

      // Fill in required sourceWorkLabel
      const labelInput = wrapper.find('[data-testid="input-source-work-label"]');
      await labelInput.setValue('Matematika Peminatan SMA');

      // Click submit
      const submitBtn = wrapper.find('[data-testid="submit-create-demand-btn"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(createSpy).toHaveBeenCalledTimes(1);
      expect(createSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          sourceWorkLabel: 'Matematika Peminatan SMA',
          requiredCapabilityCode: 'CAP-MATH-PRI',
        }),
      );
      expect(wrapper.emitted('submitted')).toBeTruthy();
    });
  });

  describe('DeclineDemandModal.vue', () => {
    it('requires decline reason and submits status decline', async () => {
      const updateStatusSpy = vi.spyOn(assignmentsApi, 'updateTeachingDemandStatus').mockResolvedValue({
        id: sampleDemands[0].id,
        status: 'declined',
        updatedAt: '2026-11-10T00:00:00Z',
      });

      const wrapper = createWrapper(DeclineDemandModal, {
        visible: true,
        demand: sampleDemands[0],
      });
      await flushPromises();

      const submitBtn = wrapper.find('[data-testid="submit-decline-btn"]');

      // Click without typing reason -> should not call mutation
      await submitBtn.trigger('click');
      await flushPromises();
      expect(updateStatusSpy).not.toHaveBeenCalled();

      // Enter decline reason
      const reasonInput = wrapper.find('[data-testid="input-decline-reason"]');
      await reasonInput.setValue('No qualified primary teacher available for this semester.');

      await submitBtn.trigger('click');
      await flushPromises();

      expect(updateStatusSpy).toHaveBeenCalledWith(
        sampleDemands[0].id,
        expect.objectContaining({
          status: 'declined',
          declineReason: 'No qualified primary teacher available for this semester.',
        }),
      );
      expect(wrapper.emitted('submitted')).toBeTruthy();
    });
  });

  describe('RequestClarificationModal.vue', () => {
    it('requires notes and submits status returned_for_clarification', async () => {
      const updateStatusSpy = vi.spyOn(assignmentsApi, 'updateTeachingDemandStatus').mockResolvedValue({
        id: sampleDemands[0].id,
        status: 'returned_for_clarification',
        updatedAt: '2026-11-10T00:00:00Z',
      });

      const wrapper = createWrapper(RequestClarificationModal, {
        visible: true,
        demand: sampleDemands[0],
      });
      await flushPromises();

      const submitBtn = wrapper.find('[data-testid="submit-clarification-btn"]');

      // Enter notes
      const notesInput = wrapper.find('[data-testid="input-clarification-notes"]');
      await notesInput.setValue('Please provide specific weekly timetable schedule.');

      await submitBtn.trigger('click');
      await flushPromises();

      expect(updateStatusSpy).toHaveBeenCalledWith(
        sampleDemands[0].id,
        expect.objectContaining({
          status: 'returned_for_clarification',
          clarificationNotes: 'Please provide specific weekly timetable schedule.',
        }),
      );
      expect(wrapper.emitted('submitted')).toBeTruthy();
    });
  });

  describe('DemandsIntakeTab.vue', () => {
    it('renders demands table with status pills and action buttons', async () => {
      const wrapper = createWrapper(DemandsIntakeTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        campusScope: 'all',
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="demands-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('DMD-2027-001');
      expect(wrapper.text()).toContain('Matematika SD Kelas 4A');
      expect(wrapper.text()).toContain('Bahasa Inggris SMP Kelas 7A');

      // Check actions: Accept button exists for received demand
      expect(wrapper.find('[data-testid="btn-accept-demand"]').exists()).toBe(true);
      // Evaluate candidates exists for accepted demand
      expect(wrapper.find('[data-testid="btn-evaluate-candidates"]').exists()).toBe(true);
    });

    it('quick accepts demand when Accept button is clicked', async () => {
      const updateStatusSpy = vi.spyOn(assignmentsApi, 'updateTeachingDemandStatus').mockResolvedValue({
        id: sampleDemands[0].id,
        status: 'accepted_for_staffing',
        updatedAt: '2026-11-10T00:00:00Z',
      });

      const wrapper = createWrapper(DemandsIntakeTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
      });
      await flushPromises();

      const acceptBtn = wrapper.find('[data-testid="btn-accept-demand"]');
      await acceptBtn.trigger('click');
      await flushPromises();

      expect(updateStatusSpy).toHaveBeenCalledWith(
        sampleDemands[0].id,
        expect.objectContaining({ status: 'accepted_for_staffing' }),
      );
    });

    it('emits evaluateDemand event when Evaluate Candidates button is clicked', async () => {
      const wrapper = createWrapper(DemandsIntakeTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
      });
      await flushPromises();

      const evalBtn = wrapper.find('[data-testid="btn-evaluate-candidates"]');
      await evalBtn.trigger('click');

      expect(wrapper.emitted('evaluateDemand')).toBeTruthy();
      expect(wrapper.emitted('evaluateDemand')![0][0]).toEqual(sampleDemands[1]);
    });
  });

  describe('CandidateEligibilityTab.vue', () => {
    it('renders demand selector, candidate selector, and runs evaluation', async () => {
      const evaluateSpy = vi.spyOn(assignmentsApi, 'evaluateCandidate').mockResolvedValue(sampleEvaluation);

      const wrapper = createWrapper(CandidateEligibilityTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        preselectedDemandId: sampleDemands[1].id,
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="candidate-eligibility-tab"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="selected-demand-summary"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="btn-run-evaluation"]').exists()).toBe(true);

      // Run evaluation
      await wrapper.find('[data-testid="btn-run-evaluation"]').trigger('click');
      await flushPromises();

      expect(evaluateSpy).toHaveBeenCalledTimes(1);
      expect(evaluateSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          teachingDemandId: sampleDemands[1].id,
        }),
      );

      // 5 Policy check breakdown cards must render
      expect(wrapper.find('[data-testid="evaluation-results-card"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="check-card-standing"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="check-card-authorization"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="check-card-qualification"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="check-card-availability"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="check-card-capacity"]').exists()).toBe(true);

      // Natural language explanation card
      expect(wrapper.find('[data-testid="natural-language-explanation-card"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="explanation-text"]').text()).toContain(
        'Memiliki otorisasi aktif Bahasa Inggris SMP',
      );

      // Shortcut button to propose assignment
      const proposeBtn = wrapper.find('[data-testid="btn-propose-assignment-shortcut"]');
      expect(proposeBtn.exists()).toBe(true);
      await proposeBtn.trigger('click');

      expect(wrapper.emitted('proposeAssignment')).toBeTruthy();
      expect(wrapper.emitted('proposeAssignment')![0][0]).toEqual(
        expect.objectContaining({
          demand: sampleDemands[1],
          evaluation: sampleEvaluation,
        }),
      );
    });

    it('renders candidate evaluation history table', async () => {
      const wrapper = createWrapper(CandidateEligibilityTab, {
        academicPeriodId: '00000000-0000-0000-0002-000000000002',
        preselectedDemandId: sampleDemands[1].id,
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="evaluations-history-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Siti Nurhaliza, S.Pd.');
    });
  });

  describe('TeachingAssignmentWorkspaceView.vue', () => {
    it('renders workspace shell with page header and 5 tabs', async () => {
      const wrapper = createWrapper(TeachingAssignmentWorkspaceView);
      await flushPromises();

      expect(wrapper.find('[data-testid="teaching-assignment-workspace"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="header-period-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="header-campus-select"]').exists()).toBe(true);

      // Check all 5 tabs exist in header
      expect(wrapper.find('[data-testid="tab-nav-demands"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-nav-eligibility"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-nav-assignments"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-nav-coverage"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-nav-conclusions"]').exists()).toBe(true);

      // Active tab panel defaults to demands
      expect(wrapper.find('[data-testid="panel-demands"]').exists()).toBe(true);
    });
  });
});
