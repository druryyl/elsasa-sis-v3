// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import SubmitPlanModal from '../SubmitPlanModal.vue';
import ApprovePlanModal from '../ApprovePlanModal.vue';
import RevisePlanModal from '../RevisePlanModal.vue';
import SyncParticipationsModal from '../SyncParticipationsModal.vue';
import ArchivePlanModal from '../ArchivePlanModal.vue';
import * as learningPlansApi from '../../api/learningPlansApi';
import type { StudentLearningPlan, SlpApproval, SyncParticipationsResponse } from '../../types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

const mockDraftPlanWithItems: StudentLearningPlan = {
  id: 'plan-draft-001',
  studentId: 'student-timotius-1',
  studentName: 'Timotius Siregar',
  studentNis: '0079876543',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'draft',
  version: 1,
  notes: 'Draft remediation pathway',
  createdAt: '2029-07-15T00:00:00Z',
  createdBy: 'user-coordinator',
  updatedAt: null,
  updatedBy: null,
  constituentUnits: [
    {
      id: 'unit-1',
      learningPlanId: 'plan-draft-001',
      learningUnitId: 'lu-mat-3',
      unitCode: 'MAT-03-S1',
      title: 'Matematika Remedial Kelas 3',
      sequenceOrder: 1,
      isRequired: true,
      notes: null,
    },
  ],
  adjustments: [
    {
      id: 'adj-1',
      learningPlanId: 'plan-draft-001',
      adjustmentType: 'accommodation',
      description: '50% extra exam time',
      rationale: 'Processing speed adaptation',
      effectiveDate: '2029-07-15',
      endDate: null,
      approvedBy: null,
      approvedDate: null,
      notes: null,
    },
  ],
  approvals: [],
};

const mockDraftPlanEmpty: StudentLearningPlan = {
  ...mockDraftPlanWithItems,
  id: 'plan-empty-002',
  constituentUnits: [],
  adjustments: [],
};

const mockActivePlanV1: StudentLearningPlan = {
  ...mockDraftPlanWithItems,
  id: 'plan-active-001',
  status: 'active',
  version: 1,
};

const mockActivePlanV2: StudentLearningPlan = {
  ...mockDraftPlanWithItems,
  id: 'plan-active-002',
  status: 'active',
  version: 2,
};

const mockDraftRevisionV2: StudentLearningPlan = {
  ...mockDraftPlanWithItems,
  id: 'plan-draft-v2',
  status: 'draft',
  version: 2,
};

describe('Learning Plan Action Modals (P8-S27 Integration Tests)', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const commonGlobal = {
    plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
    stubs: {
      Dialog: {
        props: ['visible', 'header'],
        emits: ['update:visible'],
        template: `
          <div v-if="visible" data-testid="dialog-container">
            <h3 data-testid="dialog-header">{{ header }}</h3>
            <div data-testid="dialog-content"><slot /></div>
            <div data-testid="dialog-footer"><slot name="footer" /></div>
          </div>
        `,
      },
      Select: {
        props: ['options', 'modelValue'],
        emits: ['update:modelValue'],
        template: `
          <select
            data-testid="mock-select"
            :value="modelValue"
            @change="$emit('update:modelValue', $event.target.value)"
          >
            <option v-for="o in options" :key="o.value" :value="o.value">
              {{ o.label }}
            </option>
          </select>
        `,
      },
    },
  };

  // =========================================================================
  // 1. SubmitPlanModal.vue
  // =========================================================================
  describe('SubmitPlanModal.vue', () => {
    function mountSubmitModal(props: {
      visible: boolean;
      plan: StudentLearningPlan | null;
    }) {
      return mount(SubmitPlanModal, {
        props,
        global: commonGlobal,
      });
    }

    it('renders plan summary (student name, NIS, units and adjustments counts) when visible', () => {
      const wrapper = mountSubmitModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      expect(wrapper.text()).toContain('Submit Plan for Review');
      expect(wrapper.text()).toContain('Timotius Siregar');
      expect(wrapper.text()).toContain('0079876543');
      expect(wrapper.text()).toContain('Draft v1');
      expect(wrapper.text()).toContain('1 Constituent Units');
      expect(wrapper.text()).toContain('1 Adjustments');
    });

    it('displays error banner and disables submission button when plan has no constituent units and no adjustments', () => {
      const wrapper = mountSubmitModal({
        visible: true,
        plan: mockDraftPlanEmpty,
      });

      expect(wrapper.text()).toContain('Cannot submit an empty plan');
      expect(wrapper.text()).toContain('At least one constituent learning unit or academic adjustment must be established');

      const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Submit for Review'));
      expect(submitBtn).toBeDefined();
      expect(submitBtn?.attributes('disabled')).toBeDefined();
    });

    it('submits successfully, invokes API, emits "submitted", and closes modal', async () => {
      const spySubmit = vi
        .spyOn(learningPlansApi.learningPlansApi, 'submitPlan')
        .mockResolvedValue();

      const wrapper = mountSubmitModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      const textarea = wrapper.find('textarea');
      await textarea.setValue('Ready for authority review');

      const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Submit for Review'));
      expect(submitBtn).toBeDefined();
      expect(submitBtn?.attributes('disabled')).toBeUndefined();

      await submitBtn?.trigger('click');
      await flushPromises();

      expect(spySubmit).toHaveBeenCalledWith('plan-draft-001');
      expect(wrapper.emitted('submitted')).toBeTruthy();
      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
    });

    it('displays error banner when submit API returns ProblemDetails error', async () => {
      vi.spyOn(learningPlansApi.learningPlansApi, 'submitPlan').mockRejectedValue({
        response: {
          data: {
            detail: 'Plan status must be draft to submit.',
            title: 'Conflict',
          },
        },
      });

      const wrapper = mountSubmitModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Submit for Review'));
      await submitBtn?.trigger('click');
      await flushPromises();

      expect(wrapper.text()).toContain('Plan status must be draft to submit.');
      expect(wrapper.emitted('submitted')).toBeFalsy();
    });

    it('closes modal when Cancel button is clicked', async () => {
      const wrapper = mountSubmitModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      const cancelBtn = wrapper.findAll('button').find((b) => b.text().includes('Cancel'));
      await cancelBtn?.trigger('click');

      expect(wrapper.emitted('update:visible')).toBeTruthy();
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
    });
  });

  // =========================================================================
  // 2. ApprovePlanModal.vue
  // =========================================================================
  describe('ApprovePlanModal.vue', () => {
    const mockApprovalResult: SlpApproval = {
      id: 'app-999',
      studentLearningPlanId: 'plan-draft-001',
      planVersion: 1,
      approvedBy: 'Dr. Sarah Curriculum Lead',
      approvedDate: '2029-08-01',
      approvalType: 'initial_approval',
      notes: 'Formally approved',
    };

    function mountApproveModal(props: {
      visible: boolean;
      plan: StudentLearningPlan | null;
    }) {
      return mount(ApprovePlanModal, {
        props,
        global: commonGlobal,
      });
    }

    it('renders plan summary and initializes with initial_approval for version 1 plan', () => {
      const wrapper = mountApproveModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      expect(wrapper.text()).toContain('Approve Student Learning Plan');
      expect(wrapper.text()).toContain('Timotius Siregar');
      expect(wrapper.text()).toContain('v1 Draft');
      expect(wrapper.text()).toContain('Initial Plan Approval');

      const select = wrapper.find('select');
      expect((select.element as HTMLSelectElement).value).toBe('initial_approval');
    });

    it('initializes with revision_approval for plan version > 1', () => {
      const wrapper = mountApproveModal({
        visible: true,
        plan: mockDraftRevisionV2,
      });

      const select = wrapper.find('select');
      expect((select.element as HTMLSelectElement).value).toBe('revision_approval');
      expect(wrapper.text()).toContain('Revision Approval');
    });

    it('submits approval payload, invokes API, emits "approved" with approval record, and closes modal', async () => {
      const spyApprove = vi
        .spyOn(learningPlansApi.learningPlansApi, 'approvePlan')
        .mockResolvedValue(mockApprovalResult);

      const wrapper = mountApproveModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      const textarea = wrapper.find('textarea');
      await textarea.setValue('Special committee approval verified');

      const approveBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Approve & Activate Plan'));
      expect(approveBtn).toBeDefined();

      await approveBtn?.trigger('click');
      await flushPromises();

      expect(spyApprove).toHaveBeenCalledWith('plan-draft-001', {
        approvalType: 'initial_approval',
        notes: 'Special committee approval verified',
      });
      expect(wrapper.emitted('approved')).toBeTruthy();
      expect(wrapper.emitted('approved')![0]).toEqual([mockApprovalResult]);
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
    });

    it('displays error banner when approve API rejects the operation', async () => {
      vi.spyOn(learningPlansApi.learningPlansApi, 'approvePlan').mockRejectedValue({
        response: {
          data: {
            detail: 'Conflicting active plan already exists for this student and period.',
          },
        },
      });

      const wrapper = mountApproveModal({
        visible: true,
        plan: mockDraftPlanWithItems,
      });

      const approveBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Approve & Activate Plan'));
      await approveBtn?.trigger('click');
      await flushPromises();

      expect(wrapper.text()).toContain('Conflicting active plan already exists');
      expect(wrapper.emitted('approved')).toBeFalsy();
    });
  });

  // =========================================================================
  // 3. RevisePlanModal.vue
  // =========================================================================
  describe('RevisePlanModal.vue', () => {
    function mountReviseModal(props: {
      visible: boolean;
      plan: StudentLearningPlan | null;
    }) {
      return mount(RevisePlanModal, {
        props,
        global: commonGlobal,
      });
    }

    it('renders modal with target next version number and clone-on-revise protection note', () => {
      const wrapper = mountReviseModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      expect(wrapper.text()).toContain('Revise Learning Plan (v2)');
      expect(wrapper.text()).toContain('Current: Active v1');
      expect(wrapper.text()).toContain('Clone-on-Revise Lifecycle Protection');
      expect(wrapper.text()).toContain('The current active plan (v1) remains active and operative');
    });

    it('disables create revision button when revision rationale notes are empty', async () => {
      const wrapper = mountReviseModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const reviseBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Create Revision'));
      expect(reviseBtn).toBeDefined();
      expect(reviseBtn?.attributes('disabled')).toBeDefined();
    });

    it('submits revision rationale, invokes API, emits "revised" with new plan ID, and closes modal', async () => {
      const spyRevise = vi
        .spyOn(learningPlansApi.learningPlansApi, 'revisePlan')
        .mockResolvedValue({ id: 'plan-cloned-v2-id' });

      const wrapper = mountReviseModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const textarea = wrapper.find('textarea');
      await textarea.setValue('Mid-year evaluation warrants addition of remedial language unit');

      const reviseBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Create Revision'));
      expect(reviseBtn?.attributes('disabled')).toBeUndefined();

      await reviseBtn?.trigger('click');
      await flushPromises();

      expect(spyRevise).toHaveBeenCalledWith('plan-active-001', {
        notes: 'Mid-year evaluation warrants addition of remedial language unit',
      });
      expect(wrapper.emitted('revised')).toBeTruthy();
      expect(wrapper.emitted('revised')![0]).toEqual(['plan-cloned-v2-id']);
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
    });

    it('displays error banner when revise API call fails', async () => {
      vi.spyOn(learningPlansApi.learningPlansApi, 'revisePlan').mockRejectedValue({
        response: {
          data: {
            detail: 'A pending draft revision already exists for this student and period.',
          },
        },
      });

      const wrapper = mountReviseModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const textarea = wrapper.find('textarea');
      await textarea.setValue('New revision attempt');

      const reviseBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Create Revision'));
      await reviseBtn?.trigger('click');
      await flushPromises();

      expect(wrapper.text()).toContain('A pending draft revision already exists');
      expect(wrapper.emitted('revised')).toBeFalsy();
    });
  });

  // =========================================================================
  // 4. SyncParticipationsModal.vue
  // =========================================================================
  describe('SyncParticipationsModal.vue', () => {
    const mockSyncResponse: SyncParticipationsResponse = {
      learningPlanId: 'plan-active-001',
      studentId: 'student-timotius-1',
      academicPeriodId: 'ay-2029-2030',
      syncedCount: 3,
      createdCount: 2,
      updatedCount: 1,
      totalUnits: 3,
      syncedAt: '2029-08-01T12:00:00Z',
    };

    function mountSyncModal(props: {
      visible: boolean;
      plan: StudentLearningPlan | null;
    }) {
      return mount(SyncParticipationsModal, {
        props,
        global: commonGlobal,
      });
    }

    it('renders plan summary, constituent count, and downstream alignment description', () => {
      const wrapper = mountSyncModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      expect(wrapper.text()).toContain('Synchronize Academic Participations');
      expect(wrapper.text()).toContain('Timotius Siregar');
      expect(wrapper.text()).toContain('Active v1');
      expect(wrapper.text()).toContain('1 Units');
      expect(wrapper.text()).toContain('Downstream Participation Alignment (UC-ACD-10)');
    });

    it('submits sync request, emits "synced", and displays success feedback banner', async () => {
      const spySync = vi
        .spyOn(learningPlansApi.learningPlansApi, 'syncParticipations')
        .mockResolvedValue(mockSyncResponse);

      const wrapper = mountSyncModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const syncBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Sync Participations'));
      expect(syncBtn).toBeDefined();

      await syncBtn?.trigger('click');
      await flushPromises();

      expect(spySync).toHaveBeenCalledWith('plan-active-001');
      expect(wrapper.emitted('synced')).toBeTruthy();
      expect(wrapper.emitted('synced')![0]).toEqual([mockSyncResponse]);

      // Verifies success confirmation card
      expect(wrapper.text()).toContain('Synchronization Successful');
      expect(wrapper.text()).toContain('Synced 3 participations (1 reconciled, 3 total units)');

      // Cancel button becomes 'Close'
      const closeBtn = wrapper.findAll('button').find((b) => b.text().includes('Close'));
      expect(closeBtn).toBeDefined();
    });

    it('displays error banner when sync participations fails', async () => {
      vi.spyOn(learningPlansApi.learningPlansApi, 'syncParticipations').mockRejectedValue({
        response: {
          data: {
            detail: 'Student does not have an active enrollment record for this period.',
          },
        },
      });

      const wrapper = mountSyncModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const syncBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Sync Participations'));
      await syncBtn?.trigger('click');
      await flushPromises();

      expect(wrapper.text()).toContain('Student does not have an active enrollment record');
      expect(wrapper.emitted('synced')).toBeFalsy();
    });
  });

  // =========================================================================
  // 5. ArchivePlanModal.vue
  // =========================================================================
  describe('ArchivePlanModal.vue', () => {
    function mountArchiveModal(props: {
      visible: boolean;
      plan: StudentLearningPlan | null;
    }) {
      return mount(ArchivePlanModal, {
        props,
        global: commonGlobal,
      });
    }

    it('renders archive options (archived vs completed) and plan context', () => {
      const wrapper = mountArchiveModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      expect(wrapper.text()).toContain('Archive Learning Plan');
      expect(wrapper.text()).toContain('Timotius Siregar');
      expect(wrapper.text()).toContain('Archival Lifecycle Transition');
      expect(wrapper.find('select').exists()).toBe(true);
    });

    it('submits archive transition, invokes API, emits "archived", and closes modal', async () => {
      const spyArchive = vi
        .spyOn(learningPlansApi.learningPlansApi, 'archivePlan')
        .mockResolvedValue();

      const wrapper = mountArchiveModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const archiveBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Confirm Archival'));
      expect(archiveBtn).toBeDefined();

      await archiveBtn?.trigger('click');
      await flushPromises();

      expect(spyArchive).toHaveBeenCalledWith('plan-active-001', {
        status: 'archived',
      });
      expect(wrapper.emitted('archived')).toBeTruthy();
      expect(wrapper.emitted('update:visible')![0]).toEqual([false]);
    });

    it('submits completed transition when option is selected', async () => {
      const spyArchive = vi
        .spyOn(learningPlansApi.learningPlansApi, 'archivePlan')
        .mockResolvedValue();

      const wrapper = mountArchiveModal({
        visible: true,
        plan: mockActivePlanV1,
      });

      const select = wrapper.find('select');
      await select.setValue('completed');

      const archiveBtn = wrapper
        .findAll('button')
        .find((b) => b.text().includes('Confirm Archival'));
      await archiveBtn?.trigger('click');
      await flushPromises();

      expect(spyArchive).toHaveBeenCalledWith('plan-active-001', {
        status: 'completed',
      });
      expect(wrapper.emitted('archived')).toBeTruthy();
    });
  });
});
