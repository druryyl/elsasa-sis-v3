// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import SlpApprovalHistoryTab from '../SlpApprovalHistoryTab.vue';
import type { StudentLearningPlan, StudentLearningPlanListItem } from '../../types';
import * as learningPlansApi from '../../api/learningPlansApi';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockPlanV2Active: StudentLearningPlan = {
  id: 'plan-budi-v2',
  studentId: 'student-budi',
  studentName: 'Budi Santoso',
  studentNis: '0071234567',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'active',
  version: 2,
  notes: 'Exceptional pathway with adaptive PJOK and remedial math',
  createdAt: '2029-07-01T00:00:00Z',
  createdBy: 'user-coordinator',
  updatedAt: '2029-08-01T00:00:00Z',
  updatedBy: 'user-authority',
  constituentUnits: [
    {
      id: 'unit-1',
      learningUnitId: 'lu-mat-3',
      unitCode: 'MAT-03-S1',
      title: 'Matematika Remedial Kelas 3',
      sequenceOrder: 1,
      isRequired: true,
      notes: 'Remediation level at Grade 3',
    },
    {
      id: 'unit-2',
      learningUnitId: 'lu-pjok-4',
      unitCode: 'AD-PJK-04',
      title: 'Adaptive PJOK',
      sequenceOrder: 2,
      isRequired: true,
      notes: 'Low impact medical adaptation',
    },
  ],
  adjustments: [
    {
      id: 'adj-1',
      adjustmentType: 'accommodation',
      description: '50% Extra Exam Time',
      rationale: 'Processing speed adaptation recommended by educational psychologist',
      effectiveDate: '2029-07-01',
      endDate: null,
      approvedBy: 'Ibu Ratna',
      approvedDate: '2029-07-02',
      notes: null,
    },
  ],
  approvals: [
    {
      id: 'app-2',
      learningPlanId: 'plan-budi-v2',
      planVersion: 2,
      approvedBy: 'Ibu Ratna (VP Academics)',
      approvedDate: '2029-08-01',
      approvalType: 'revision_approval',
      notes: 'Approved amendment adding adaptive physical education and extra exam time',
    },
    {
      id: 'app-1',
      learningPlanId: 'plan-budi-v2',
      planVersion: 1,
      approvedBy: 'Pak Hendra (Curriculum Lead)',
      approvedDate: '2029-07-02',
      approvalType: 'initial_approval',
      notes: 'Initial learning plan formulation approved for term commencement',
    },
  ],
};

const mockPlanV1Revised: StudentLearningPlan = {
  ...mockPlanV2Active,
  id: 'plan-budi-v1',
  version: 1,
  status: 'revised',
  notes: 'Initial draft v1',
  approvals: [
    {
      id: 'app-1',
      learningPlanId: 'plan-budi-v1',
      planVersion: 1,
      approvedBy: 'Pak Hendra (Curriculum Lead)',
      approvedDate: '2029-07-02',
      approvalType: 'initial_approval',
      notes: 'Initial learning plan formulation approved for term commencement',
    },
  ],
};

const mockPlanV1Draft: StudentLearningPlan = {
  id: 'plan-timotius-v1',
  studentId: 'student-timotius',
  studentName: 'Timotius',
  studentNis: '0079876543',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'draft',
  version: 1,
  notes: 'Pending initial authority review',
  createdAt: '2029-07-15T00:00:00Z',
  createdBy: 'user-coordinator',
  updatedAt: null,
  updatedBy: null,
  constituentUnits: [],
  adjustments: [],
  approvals: [],
};

const mockAllPlans: StudentLearningPlanListItem[] = [
  {
    id: 'plan-budi-v2',
    studentId: 'student-budi',
    studentName: 'Budi Santoso',
    studentNis: '0071234567',
    academicPeriodId: 'ay-2029-2030',
    academicPeriodLabel: 'Semester 1 TP 2029/2030',
    status: 'active',
    version: 2,
    unitsCount: 2,
    adjustmentsCount: 1,
  },
  {
    id: 'plan-budi-v1',
    studentId: 'student-budi',
    studentName: 'Budi Santoso',
    studentNis: '0071234567',
    academicPeriodId: 'ay-2029-2030',
    academicPeriodLabel: 'Semester 1 TP 2029/2030',
    status: 'revised',
    version: 1,
    unitsCount: 1,
    adjustmentsCount: 0,
  },
];

describe('SlpApprovalHistoryTab.vue (SCR-ACD-04 Approval History & Versions Tab)', () => {
  let queryClient: QueryClient;

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
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.spyOn(learningPlansApi.learningPlansApi, 'getLearningPlanById').mockImplementation(
      async (id: string) => {
        if (id === 'plan-budi-v1') return mockPlanV1Revised;
        return mockPlanV2Active;
      },
    );
  });

  function createWrapper(props: {
    plan: StudentLearningPlan;
    allPlans?: StudentLearningPlanListItem[];
  }) {
    return mount(SlpApprovalHistoryTab, {
      props,
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          Dialog: {
            props: ['visible', 'header'],
            emits: ['update:visible'],
            template: `
              <div v-if="visible" data-testid="historical-inspect-dialog">
                <h3>{{ header }}</h3>
                <slot />
                <slot name="footer" />
              </div>
            `,
          },
        },
      },
    });
  }

  it('renders the approval history tab and plan versions switcher container', () => {
    const wrapper = createWrapper({
      plan: mockPlanV2Active,
      allPlans: mockAllPlans,
    });

    expect(wrapper.find('[data-testid="slp-approval-history-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="slp-version-switcher"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Plan Versions & Lifecycle Timeline');
  });

  it('displays approval events in chronological order (earliest v1 initial approval, then v2 revision)', () => {
    const wrapper = createWrapper({
      plan: mockPlanV2Active,
      allPlans: mockAllPlans,
    });

    const timeline = wrapper.find('[data-testid="slp-approvals-timeline"]');
    expect(timeline.exists()).toBe(true);

    const entries = wrapper.findAll('[data-testid^="approval-entry-"]');
    expect(entries.length).toBe(2);

    // Entry 1 should be v1 (Initial Approval) because sortedApprovals sorts by planVersion ASC
    const entry1 = entries[0];
    expect(entry1.find('[data-testid="approval-version-tag"]').text()).toBe('v1');
    expect(entry1.find('[data-testid="approval-type-tag"]').text()).toBe('Initial Approval');
    expect(entry1.find('[data-testid="approval-authority"]').text()).toContain('Pak Hendra (Curriculum Lead)');
    expect(entry1.find('[data-testid="approval-notes"]').text()).toContain('Initial learning plan formulation approved');

    // Entry 2 should be v2 (Revision Approval)
    const entry2 = entries[1];
    expect(entry2.find('[data-testid="approval-version-tag"]').text()).toBe('v2');
    expect(entry2.find('[data-testid="approval-type-tag"]').text()).toBe('Revision Approval');
    expect(entry2.find('[data-testid="approval-authority"]').text()).toContain('Ibu Ratna (VP Academics)');
    expect(entry2.find('[data-testid="approval-notes"]').text()).toContain('Approved amendment adding adaptive physical education');
  });

  it('displays empty state for draft plans awaiting initial approval', () => {
    const wrapper = createWrapper({
      plan: mockPlanV1Draft,
      allPlans: [
        {
          id: mockPlanV1Draft.id,
          studentId: mockPlanV1Draft.studentId,
          studentName: mockPlanV1Draft.studentName,
          academicPeriodId: mockPlanV1Draft.academicPeriodId,
          academicPeriodLabel: mockPlanV1Draft.academicPeriodLabel,
          status: 'draft',
          version: 1,
          unitsCount: 0,
          adjustmentsCount: 0,
        },
      ],
    });

    expect(wrapper.find('[data-testid="slp-approvals-empty"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Awaiting Initial Approval');
    expect(wrapper.text()).toContain('awaiting formal review and approval by an authorized Academic Authority');
    expect(wrapper.find('[data-testid="slp-approvals-timeline"]').exists()).toBe(false);
  });

  it('renders read-only banner when inspecting a historical revised plan', () => {
    const wrapper = createWrapper({
      plan: mockPlanV1Revised,
      allPlans: mockAllPlans,
    });

    const banner = wrapper.find('[data-testid="slp-readonly-banner"]');
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain('Read-Only Historical Plan Mode');
    expect(banner.text()).toContain('Version v1');
    expect(banner.text()).toContain('revised');

    // Switch to active button
    const switchActiveBtn = wrapper.find('[data-testid="switch-to-active-btn"]');
    expect(switchActiveBtn.exists()).toBe(true);
  });

  it('emits "select-plan" when clicking switch button in version switcher', async () => {
    const wrapper = createWrapper({
      plan: mockPlanV2Active,
      allPlans: mockAllPlans,
    });

    const switchBtnV1 = wrapper.find('[data-testid="version-btn-1"]');
    expect(switchBtnV1.exists()).toBe(true);

    await switchBtnV1.trigger('click');
    expect(wrapper.emitted('select-plan')).toBeTruthy();
    expect(wrapper.emitted('select-plan')![0]).toEqual(['plan-budi-v1']);
  });

  it('opens quick inspect dialog when clicking inspect button', async () => {
    const wrapper = createWrapper({
      plan: mockPlanV2Active,
      allPlans: mockAllPlans,
    });

    const inspectBtn = wrapper.find('[data-testid="quick-inspect-btn-1"]');
    expect(inspectBtn.exists()).toBe(true);

    await inspectBtn.trigger('click');
    await flushPromises();

    const dialog = wrapper.find('[data-testid="historical-inspect-dialog"]');
    expect(dialog.exists()).toBe(true);
    expect(dialog.text()).toContain('Historical Plan Snapshot');
  });

  it('emits "select-plan" from the inspect dialog switch button', async () => {
    const wrapper = createWrapper({
      plan: mockPlanV2Active,
      allPlans: mockAllPlans,
    });

    const inspectBtn = wrapper.find('[data-testid="quick-inspect-btn-1"]');
    await inspectBtn.trigger('click');
    await flushPromises();

    const dialogSwitchBtn = wrapper.find('[data-testid="dialog-switch-version-btn"]');
    expect(dialogSwitchBtn.exists()).toBe(true);

    await dialogSwitchBtn.trigger('click');
    expect(wrapper.emitted('select-plan')).toBeTruthy();
    expect(wrapper.emitted('select-plan')![0]).toEqual(['plan-budi-v1']);
  });
});
