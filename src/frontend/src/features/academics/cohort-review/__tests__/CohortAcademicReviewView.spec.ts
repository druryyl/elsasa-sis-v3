// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { ref } from 'vue';
import CohortAcademicReviewView from '../views/CohortAcademicReviewView.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import * as cohortReviewComposables from '../composables/useCohortReview';
import type {
  CohortStandingProposal,
  CohortProgressionProposal,
  StandingItem,
  ProgressionItem,
  AcademicAchievement,
} from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

// Dialog stub allowing header and footer slots
const DialogStub = {
  props: ['visible', 'header', 'modal', 'draggable', 'dataTestid'],
  emits: ['update:visible'],
  template: `
    <div v-if="visible" class="p-dialog" :data-testid="dataTestid || 'p-dialog'">
      <div class="p-dialog-header">{{ header }}<slot name="header" /></div>
      <div class="p-dialog-content"><slot /></div>
      <div class="p-dialog-footer"><slot name="footer" /></div>
    </div>
  `,
};

// Child modal stubs
const MockStandingOverrideModal = {
  name: 'StandingOverrideModal',
  props: ['visible', 'student'],
  emits: ['update:visible', 'applied'],
  template: `
    <div v-if="visible" data-testid="mock-override-modal">
      <span data-testid="override-student-name">{{ student?.studentName }}</span>
      <button
        data-testid="btn-apply-mock-override"
        @click="$emit('applied', {
          studentId: student?.studentId,
          standing: 'conditional_standing',
          policyReference: 'SK-AKAD/2026/POLICY-01',
          conditions: 'Pass makeup assessment',
          recommendation: 'conditional_promotion',
          fromLevel: 'Grade 4',
          recommendedToLevel: 'Grade 5',
          supportingEvidence: 'Council decree exception granted',
          isException: true,
          exceptionReason: 'Special council recommendation',
          committeeReference: 'SK-DG/2026/06-01'
        }); $emit('update:visible', false)"
      >
        Apply Mock Override
      </button>
      <button data-testid="btn-close-override" @click="$emit('update:visible', false)">Close</button>
    </div>
  `,
};

const MockConferAchievementModal = {
  name: 'ConferAchievementModal',
  props: ['visible', 'academicPeriodId', 'initialStudentId', 'initialStudentName'],
  emits: ['update:visible', 'conferred'],
  template: `
    <div v-if="visible" data-testid="mock-confer-modal">
      <span data-testid="confer-student-name">{{ initialStudentName }}</span>
      <button data-testid="btn-submit-mock-confer" @click="$emit('conferred', { id: 'ach-new' }); $emit('update:visible', false)">Confer</button>
    </div>
  `,
};

const MockStandingCorrectionModal = {
  name: 'StandingCorrectionModal',
  props: ['visible', 'targetType', 'targetId', 'studentName'],
  emits: ['update:visible', 'corrected'],
  template: `
    <div v-if="visible" data-testid="mock-correction-modal">
      <span data-testid="correction-target">{{ targetType }}</span>
      <button data-testid="btn-submit-mock-correct" @click="$emit('corrected', {}); $emit('update:visible', false)">Correct</button>
    </div>
  `,
};

const mockStandingProposals: CohortStandingProposal[] = [
  {
    studentId: 'stu-rina',
    studentName: 'Rina Kusuma',
    studentIdentifier: 'STD-4001',
    gradeLevel: 'Grade 4',
    totalUnitsCount: 10,
    passedUnitsCount: 10,
    failedUnitsCount: 0,
    attendancePercentage: 96.5,
    proposedStanding: 'good_standing',
    policyReference: 'SK-AKAD/2026/POLICY-01',
    proposedConditions: null,
    isException: false,
    exceptionReason: null,
    committeeReference: null,
  },
  {
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentIdentifier: 'STD-4002',
    gradeLevel: 'Grade 4',
    totalUnitsCount: 10,
    passedUnitsCount: 9,
    failedUnitsCount: 1,
    attendancePercentage: 88.0,
    proposedStanding: 'conditional_standing',
    policyReference: 'SK-AKAD/2026/POLICY-01',
    proposedConditions: 'Complete math tutoring',
    isException: false,
    exceptionReason: null,
    committeeReference: null,
  },
  {
    studentId: 'stu-timotius',
    studentName: 'Timotius',
    studentIdentifier: 'STD-4003',
    gradeLevel: 'Grade 4',
    totalUnitsCount: 10,
    passedUnitsCount: 6,
    failedUnitsCount: 4,
    attendancePercentage: 68.0,
    proposedStanding: 'not_in_good_standing',
    policyReference: 'SK-AKAD/2026/POLICY-01',
    proposedConditions: 'Exceeds failing threshold',
    isException: false,
    exceptionReason: null,
    committeeReference: null,
  },
];

const mockProgressionProposals: CohortProgressionProposal[] = [
  {
    studentId: 'stu-rina',
    studentName: 'Rina Kusuma',
    studentIdentifier: 'STD-4001',
    proposedRecommendation: 'promotion_eligible',
    fromLevel: 'Grade 4',
    recommendedToLevel: 'Grade 5',
    conditions: null,
    supportingEvidence: 'Evaluation: 10/10 units passed, 96.5% attendance.',
    isException: false,
    exceptionReason: null,
    committeeReference: null,
  },
  {
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentIdentifier: 'STD-4002',
    proposedRecommendation: 'conditional_promotion',
    fromLevel: 'Grade 4',
    recommendedToLevel: 'Grade 5',
    conditions: 'Complete math tutoring',
    supportingEvidence: 'Evaluation: 9/10 units passed, 88.0% attendance.',
    isException: false,
    exceptionReason: null,
    committeeReference: null,
  },
  {
    studentId: 'stu-timotius',
    studentName: 'Timotius',
    studentIdentifier: 'STD-4003',
    proposedRecommendation: 'retention_recommended',
    fromLevel: 'Grade 4',
    recommendedToLevel: 'Grade 4',
    conditions: null,
    supportingEvidence: 'Evaluation: 6/10 units passed, 68.0% attendance.',
    isException: false,
    exceptionReason: null,
    committeeReference: null,
  },
];

const mockAchievements: AcademicAchievement[] = [
  {
    id: 'ach-01',
    studentId: 'stu-rina',
    studentName: 'Rina Kusuma',
    studentIdentifier: 'STD-4001',
    academicPeriodId: '2026-ganjil',
    learningUnitId: 'unit-math-04',
    achievementType: 'subject_distinction',
    title: 'Nilai Tertinggi Matematika',
    criteriaMet: 'Highest final summative score (98.5) in cohort',
    supportingEvidence: 'Official summative assessment record',
    recognizedBy: 'Academic Authority',
    recognizedAt: '2026-11-05',
    createdAt: '2026-11-05T08:00:00Z',
  },
];

describe('CohortAcademicReviewView.vue (SCR-ACD-09, ACD-WF-13, UC-ACD-17)', () => {
  let authStore: ReturnType<typeof useAuthStore>;
  let appContextStore: ReturnType<typeof useAppContextStore>;
  let mockMutateBatchCommit: ReturnType<typeof vi.fn>;
  let mockRefetchStandingEval: ReturnType<typeof vi.fn>;
  let mockRefetchProgressionEval: ReturnType<typeof vi.fn>;

  beforeAll(() => {
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

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();

    mockMutateBatchCommit = vi.fn();
    mockRefetchStandingEval = vi.fn();
    mockRefetchProgressionEval = vi.fn();

    authStore = useAuthStore();
    authStore.setSession('mock-token', {
      id: 'auth-1',
      username: 'academic.authority',
      email: 'authority@school.test',
      fullName: 'Academic Authority',
      isActive: true,
      roles: ['AcademicAuthority'],
      permissions: [
        'academics.standings.view',
        'academics.standings.determine',
        'academics.standings.batch_determine',
        'academics.progression.view',
        'academics.progression.issue',
        'academics.progression.batch_issue',
        'academics.achievements.view',
        'academics.achievements.confer',
        'academics.corrections.standing',
        'academics.corrections.progression',
      ],
      campusId: null,
    });

    appContextStore = useAppContextStore();
    appContextStore.activeAcademicPeriodId = '2026-ganjil';

    vi.spyOn(cohortReviewComposables, 'useEvaluateStandingQuery').mockReturnValue({
      data: ref(mockStandingProposals),
      isLoading: ref(false),
      isFetching: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: mockRefetchStandingEval,
    } as any);

    vi.spyOn(cohortReviewComposables, 'useEvaluateProgressionQuery').mockReturnValue({
      data: ref(mockProgressionProposals),
      isLoading: ref(false),
      isFetching: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: mockRefetchProgressionEval,
    } as any);

    vi.spyOn(cohortReviewComposables, 'useCohortStandingsQuery').mockReturnValue({
      data: ref([]),
      isLoading: ref(false),
      isFetching: ref(false),
      refetch: vi.fn(),
    } as any);

    vi.spyOn(cohortReviewComposables, 'useCohortRecommendationsQuery').mockReturnValue({
      data: ref([]),
      isLoading: ref(false),
      isFetching: ref(false),
      refetch: vi.fn(),
    } as any);

    vi.spyOn(cohortReviewComposables, 'useCohortAchievementsQuery').mockReturnValue({
      data: ref(mockAchievements),
      isLoading: ref(false),
      isFetching: ref(false),
      refetch: vi.fn(),
    } as any);

    vi.spyOn(cohortReviewComposables, 'useBatchCommitCohortMutation').mockImplementation((options) => {
      return {
        mutate: vi.fn((vars) => {
          mockMutateBatchCommit(vars);
          options?.onSuccess?.({
            standingsResult: { totalProcessed: 3, determinations: [] },
            progressionResult: { totalProcessed: 3, recommendations: [] },
          } as any, vars);
        }),
        isPending: ref(false),
      } as any;
    });
  });

  function createWrapper() {
    return mount(CohortAcademicReviewView, {
      global: {
        plugins: [PrimeVue, ToastService],
        stubs: {
          'router-link': true,
          Dialog: DialogStub,
          StandingOverrideModal: MockStandingOverrideModal,
          ConferAchievementModal: MockConferAchievementModal,
          StandingCorrectionModal: MockStandingCorrectionModal,
        },
      },
    });
  }

  it('renders cohort preview candidates, proposals, and summary action bar', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('Cohort Academic Review');
    expect(wrapper.find('[data-testid="cohort-review-tabs"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="table-cohort-review"]').exists()).toBe(true);

    // Candidates rendered
    expect(wrapper.text()).toContain('Rina Kusuma');
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('Timotius');

    // Units and metrics rendered
    expect(wrapper.text()).toContain('Passed: 10 / 10');
    expect(wrapper.text()).toContain('Failed: 1 units');
    expect(wrapper.text()).toContain('96.5%');
    expect(wrapper.text()).toContain('88%');
    expect(wrapper.text()).toContain('68%');

    // Sticky footer summary counts
    expect(wrapper.find('[data-testid="cohort-review-footer-bar"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="kpi-eligible"]').text()).toContain('Eligible: 1');
    expect(wrapper.find('[data-testid="kpi-conditional"]').text()).toContain('Conditional: 1');
    expect(wrapper.find('[data-testid="kpi-retention"]').text()).toContain('Retention: 1');
  });

  it('triggers exception override modal and reflects decree override inline', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Click Override / Decree on Timotius's row
    const overrideBtn = wrapper.find('[data-testid="btn-override-stu-timotius"]');
    expect(overrideBtn.exists()).toBe(true);
    await overrideBtn.trigger('click');
    await flushPromises();

    // Verify modal opened with Timotius
    expect(wrapper.find('[data-testid="mock-override-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="override-student-name"]').text()).toBe('Timotius');

    // Apply mock override (upgrading from retention to conditional promotion with SK-DG decree)
    await wrapper.find('[data-testid="btn-apply-mock-override"]').trigger('click');
    await flushPromises();

    // Overrides badge and decree reference reflected
    expect(wrapper.text()).toContain('Overridden');
    expect(wrapper.text()).toContain('SK-DG/2026/06-01');
    expect(wrapper.text()).toContain('Decree Overrides: 1');
    expect(wrapper.find('[data-testid="kpi-conditional"]').text()).toContain('Conditional: 2');
    expect(wrapper.find('[data-testid="kpi-retention"]').text()).toContain('Retention: 0');

    // Reset overrides button reverts back to proposals
    const resetBtn = wrapper.find('[data-testid="btn-reset-overrides"]');
    expect(resetBtn.exists()).toBe(true);
    await resetBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="kpi-retention"]').text()).toContain('Retention: 1');
  });

  it('triggers confirmation dialog and invokes batch commit action with cohort determinations', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const commitBtn = wrapper.find('[data-testid="btn-approve-commit-cohort"]');
    expect(commitBtn.exists()).toBe(true);
    await commitBtn.trigger('click');
    await flushPromises();

    // Confirmation dialog appears
    const confirmDialog = wrapper.find('[data-testid="dialog-confirm-cohort-commit"]');
    expect(confirmDialog.exists()).toBe(true);
    expect(confirmDialog.text()).toContain('3 students');
    expect(confirmDialog.text()).toContain('Grade 4');

    // Confirm batch commit
    const confirmCommitBtn = wrapper.find('[data-testid="btn-confirm-batch-commit"]');
    expect(confirmCommitBtn.exists()).toBe(true);
    await confirmCommitBtn.trigger('click');
    await flushPromises();

    // Verifies batch commit was invoked with both standing and progression items
    expect(mockMutateBatchCommit).toHaveBeenCalledWith(
      expect.objectContaining({
        standingRequest: expect.objectContaining({
          academicPeriodId: '2026-ganjil',
          items: expect.arrayContaining([
            expect.objectContaining({ studentId: 'stu-rina', standing: 'good_standing' }),
            expect.objectContaining({ studentId: 'stu-budi', standing: 'conditional_standing' }),
            expect.objectContaining({ studentId: 'stu-timotius', standing: 'not_in_good_standing' }),
          ]),
        }),
        progressionRequest: expect.objectContaining({
          academicPeriodId: '2026-ganjil',
          items: expect.arrayContaining([
            expect.objectContaining({ studentId: 'stu-rina', recommendation: 'promotion_eligible' }),
            expect.objectContaining({ studentId: 'stu-budi', recommendation: 'conditional_promotion' }),
            expect.objectContaining({ studentId: 'stu-timotius', recommendation: 'retention_recommended' }),
          ]),
        }),
      }),
    );
  });

  it('switches to Academic Honors & Achievements tab and displays period honors roster', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const tabHonors = wrapper.find('[data-testid="tab-honors-achievements"]');
    await tabHonors.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="panel-honors-achievements"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="table-achievements"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Nilai Tertinggi Matematika');
    expect(wrapper.text()).toContain('Subject Distinction');
    expect(wrapper.text()).toContain('Rina Kusuma');

    // Quick confer achievement button
    const conferBtn = wrapper.find('[data-testid="btn-confer-achievement-tab"]');
    expect(conferBtn.exists()).toBe(true);
    await conferBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="mock-confer-modal"]').exists()).toBe(true);
  });
});
