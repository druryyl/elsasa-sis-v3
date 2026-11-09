// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import SlpHeaderBanner from '../SlpHeaderBanner.vue';
import SubmitPlanModal from '../SubmitPlanModal.vue';
import ApprovePlanModal from '../ApprovePlanModal.vue';
import RevisePlanModal from '../RevisePlanModal.vue';
import SyncParticipationsModal from '../SyncParticipationsModal.vue';
import ArchivePlanModal from '../ArchivePlanModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import {
  LEARNING_PLAN_PERMISSIONS,
  LEARNING_PLAN_ROLES,
} from '../../constants/permissions';
import * as learningPlansApi from '../../api/learningPlansApi';
import * as studentRecordsApi from '@/features/students/records/api/records.api';
import type { StudentLearningPlan } from '../../types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

const mockDraftPlan: StudentLearningPlan = {
  id: 'plan-draft-1',
  studentId: 'student-timotius-1',
  studentName: 'Timotius Siregar',
  studentNis: '0079876543',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'draft',
  version: 1,
  notes: 'Draft remediation pathway for initial review',
  createdAt: '2029-07-15T00:00:00Z',
  createdBy: 'user-coordinator',
  updatedAt: null,
  updatedBy: null,
  constituentUnits: [
    {
      id: 'unit-entry-1',
      learningPlanId: 'plan-draft-1',
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
      learningPlanId: 'plan-draft-1',
      adjustmentType: 'accommodation',
      description: 'Extended Time for Mathematics Assessment',
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

const mockActivePlan: StudentLearningPlan = {
  id: 'plan-active-2',
  studentId: 'student-budi-1',
  studentName: 'Budi Santoso',
  studentNis: '0071234567',
  academicPeriodId: 'ay-2029-2030',
  academicPeriodLabel: 'Semester 1 TP 2029/2030',
  status: 'active',
  version: 2,
  notes: 'Approved individualized acceleration pathway',
  createdAt: '2029-07-01T00:00:00Z',
  createdBy: 'user-coordinator',
  updatedAt: '2029-08-01T00:00:00Z',
  updatedBy: 'user-authority',
  constituentUnits: [
    {
      id: 'unit-entry-1',
      learningPlanId: 'plan-active-2',
      learningUnitId: 'lu-mat-4',
      unitCode: 'MAT-04-S1',
      title: 'Matematika Kelas 4',
      sequenceOrder: 1,
      isRequired: true,
      notes: null,
    },
    {
      id: 'unit-entry-2',
      learningPlanId: 'plan-active-2',
      learningUnitId: 'lu-pjok-4',
      unitCode: 'PJK-04-S1',
      title: 'PJOK Adaptif Kelas 4',
      sequenceOrder: 2,
      isRequired: true,
      notes: 'Adaptive physical education',
    },
  ],
  adjustments: [
    {
      id: 'adj-1',
      learningPlanId: 'plan-active-2',
      adjustmentType: 'accommodation',
      description: '50% Extra Exam Time',
      rationale: 'Processing speed adaptation',
      effectiveDate: '2029-07-01',
      endDate: null,
      approvedBy: 'Ibu Ratna',
      approvedDate: '2029-08-01',
      notes: null,
    },
  ],
  approvals: [
    {
      id: 'app-1',
      learningPlanId: 'plan-active-2',
      planVersion: 2,
      approvedBy: 'Ibu Ratna (VP Academics)',
      approvedDate: '2029-08-01',
      approvalType: 'revision_approval',
      notes: 'Approved revision adding adaptive PJOK',
    },
  ],
};

const mockRevisedPlan: StudentLearningPlan = {
  ...mockDraftPlan,
  id: 'plan-revised-1',
  status: 'revised',
  version: 1,
};

describe('SlpHeaderBanner.vue (SCR-ACD-04, P7-S20)', () => {
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

    vi.spyOn(studentRecordsApi, 'getStudentById').mockResolvedValue({
      id: 'student-timotius-1',
      studentNumber: '0079876543',
      applicantId: 'app-1',
      fullName: 'Timotius Siregar',
      preferredName: 'Timo',
      currentStatus: 'active',
      enrollmentDate: '2029-07-01',
      isIndependent: false,
      createdAt: '2029-07-01T00:00:00Z',
      enrollmentHistory: [],
      guardianRelationships: [],
      documents: [],
      currentPlacement: {
        id: 'place-1',
        campus: 'Kampus Barat',
        schoolLevel: 'Elementary',
        grade: 'Grade 3',
        academicYear: '2029/2030',
        effectiveDate: '2029-07-01',
        isCurrent: true,
        homeroom: 'Kelas 3-B',
        cohort: 'Cohort 2029',
      },
    });
  });

  function createWrapper(
    plan: StudentLearningPlan = mockDraftPlan,
    props: Partial<{
      gradeLevel: string | null;
      campus: string | null;
      cohort: string | null;
      homeroom: string | null;
    }> = {},
  ) {
    return mount(SlpHeaderBanner, {
      props: {
        plan,
        ...props,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          Dialog: {
            props: ['visible', 'header'],
            template:
              '<div v-if="visible" data-testid="dialog"><h3>{{ header }}</h3><slot /><slot name="footer" /></div>',
          },
          Select: {
            props: ['options', 'modelValue'],
            emits: ['update:modelValue'],
            template: `
              <select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)">
                <option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option>
              </select>
            `,
          },
        },
      },
    });
  }

  function setupAuth(roles: string[] = [], permissions: string[] = []) {
    const authStore = useAuthStore();
    authStore.user = {
      userId: 'test-user',
      username: 'testuser',
      fullName: 'Test User',
      roles,
      permissions,
      campusId: null,
    };
    authStore.roles = roles;
    authStore.permissions = permissions;
    authStore.token = 'mock-jwt-token';
  }

  it('renders student photo/avatar initial, full name, NIS, version pill, and PrimeVue status badge', async () => {
    setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
    const wrapper = createWrapper(mockDraftPlan);
    await flushPromises();

    expect(wrapper.text()).toContain('Timotius Siregar');
    expect(wrapper.text()).toContain('NIS: 0079876543');
    expect(wrapper.text()).toContain('v1');
    expect(wrapper.text()).toContain('Draft');
    expect(wrapper.text()).toContain('T'); // Avatar initial
  });

  it('renders administrative placement context (grade level, campus, and homeroom cohort)', async () => {
    setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
    const wrapper = createWrapper(mockDraftPlan);
    await flushPromises();

    expect(wrapper.text()).toContain('Grade 3');
    expect(wrapper.text()).toContain('Kampus Barat');
    expect(wrapper.text()).toContain('Kelas 3-B');
  });

  it('allows overriding administrative placement via explicit props', async () => {
    setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
    const wrapper = createWrapper(mockDraftPlan, {
      gradeLevel: 'Grade 5 Custom',
      campus: 'Kampus Timur',
      homeroom: 'Kelas 5-A',
    });
    await flushPromises();

    expect(wrapper.text()).toContain('Grade 5 Custom');
    expect(wrapper.text()).toContain('Kampus Timur');
    expect(wrapper.text()).toContain('Kelas 5-A');
  });

  describe('Role-Based Action Buttons for Draft Plans', () => {
    it('shows Submit for Review button enabled for Academic Coordinator on draft plan', async () => {
      setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Submit for Review'));
      expect(submitBtn).toBeDefined();
      expect(submitBtn?.attributes('disabled')).toBeUndefined();
    });

    it('shows Submit for Review button disabled when actor lacks Coordinator authorization', async () => {
      setupAuth(['Teacher'], ['academics:records:view']);
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Submit for Review'));
      expect(submitBtn).toBeDefined();
      expect(submitBtn?.attributes('disabled')).toBeDefined();
    });

    it('shows Approve Plan button enabled for Academic Authority on draft plan', async () => {
      setupAuth([LEARNING_PLAN_ROLES.AUTHORITY], [LEARNING_PLAN_PERMISSIONS.APPROVE]);
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      const approveBtn = wrapper.findAll('button').find((b) => b.text().includes('Approve Plan'));
      expect(approveBtn).toBeDefined();
      expect(approveBtn?.attributes('disabled')).toBeUndefined();
    });

    it('shows Approve Plan button disabled when actor lacks Authority authorization', async () => {
      setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      const approveBtn = wrapper.findAll('button').find((b) => b.text().includes('Approve Plan'));
      expect(approveBtn).toBeDefined();
      expect(approveBtn?.attributes('disabled')).toBeDefined();
    });

    it('does NOT display Revise Plan or Sync Participations buttons on draft plan', async () => {
      setupAuth(
        [LEARNING_PLAN_ROLES.COORDINATOR, LEARNING_PLAN_ROLES.AUTHORITY],
        [LEARNING_PLAN_PERMISSIONS.MANAGE, LEARNING_PLAN_PERMISSIONS.APPROVE],
      );
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      expect(wrapper.text()).not.toContain('Revise Plan (v+1)');
      expect(wrapper.text()).not.toContain('Sync Participations');
    });
  });

  describe('Role-Based Action Buttons for Active Plans', () => {
    it('shows Revise Plan (v+1) and Sync Participations enabled for Academic Coordinator on active plan', async () => {
      setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE, LEARNING_PLAN_PERMISSIONS.REVISE]);
      const wrapper = createWrapper(mockActivePlan);
      await flushPromises();

      const reviseBtn = wrapper.findAll('button').find((b) => b.text().includes('Revise Plan (v+1)'));
      const syncBtn = wrapper.findAll('button').find((b) => b.text().includes('Sync Participations'));

      expect(reviseBtn).toBeDefined();
      expect(reviseBtn?.attributes('disabled')).toBeUndefined();
      expect(syncBtn).toBeDefined();
      expect(syncBtn?.attributes('disabled')).toBeUndefined();
    });

    it('shows Archive Plan enabled for Academic Authority on active plan', async () => {
      setupAuth([LEARNING_PLAN_ROLES.AUTHORITY], [LEARNING_PLAN_PERMISSIONS.APPROVE]);
      const wrapper = createWrapper(mockActivePlan);
      await flushPromises();

      const archiveBtn = wrapper.findAll('button').find((b) => b.text().includes('Archive Plan'));
      expect(archiveBtn).toBeDefined();
      expect(archiveBtn?.attributes('disabled')).toBeUndefined();
    });

    it('does NOT display Submit for Review or Approve Plan buttons on active plan', async () => {
      setupAuth(
        [LEARNING_PLAN_ROLES.COORDINATOR, LEARNING_PLAN_ROLES.AUTHORITY],
        [LEARNING_PLAN_PERMISSIONS.MANAGE, LEARNING_PLAN_PERMISSIONS.APPROVE],
      );
      const wrapper = createWrapper(mockActivePlan);
      await flushPromises();

      expect(wrapper.text()).not.toContain('Submit for Review');
      expect(wrapper.text()).not.toContain('Approve Plan');
    });
  });

  describe('Role-Based Action Buttons for Revised Plans', () => {
    it('shows Archive Plan button on revised plan for Academic Authority', async () => {
      setupAuth([LEARNING_PLAN_ROLES.AUTHORITY], [LEARNING_PLAN_PERMISSIONS.APPROVE]);
      const wrapper = createWrapper(mockRevisedPlan);
      await flushPromises();

      const archiveBtn = wrapper.findAll('button').find((b) => b.text().includes('Archive Plan'));
      expect(archiveBtn).toBeDefined();
      expect(archiveBtn?.attributes('disabled')).toBeUndefined();

      expect(wrapper.text()).not.toContain('Submit for Review');
      expect(wrapper.text()).not.toContain('Approve Plan');
      expect(wrapper.text()).not.toContain('Revise Plan (v+1)');
      expect(wrapper.text()).not.toContain('Sync Participations');
    });
  });

  describe('Modal Triggers', () => {
    it('clicking Submit for Review opens SubmitPlanModal', async () => {
      setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Submit for Review'));
      await submitBtn?.trigger('click');
      await flushPromises();

      const modal = wrapper.findComponent(SubmitPlanModal);
      expect(modal.props('visible')).toBe(true);
    });

    it('clicking Approve Plan opens ApprovePlanModal', async () => {
      setupAuth([LEARNING_PLAN_ROLES.AUTHORITY], [LEARNING_PLAN_PERMISSIONS.APPROVE]);
      const wrapper = createWrapper(mockDraftPlan);
      await flushPromises();

      const approveBtn = wrapper.findAll('button').find((b) => b.text().includes('Approve Plan'));
      await approveBtn?.trigger('click');
      await flushPromises();

      const modal = wrapper.findComponent(ApprovePlanModal);
      expect(modal.props('visible')).toBe(true);
    });

    it('clicking Revise Plan (v+1) opens RevisePlanModal', async () => {
      setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
      const wrapper = createWrapper(mockActivePlan);
      await flushPromises();

      const reviseBtn = wrapper.findAll('button').find((b) => b.text().includes('Revise Plan (v+1)'));
      await reviseBtn?.trigger('click');
      await flushPromises();

      const modal = wrapper.findComponent(RevisePlanModal);
      expect(modal.props('visible')).toBe(true);
    });

    it('clicking Sync Participations opens SyncParticipationsModal', async () => {
      setupAuth([LEARNING_PLAN_ROLES.COORDINATOR], [LEARNING_PLAN_PERMISSIONS.MANAGE]);
      const wrapper = createWrapper(mockActivePlan);
      await flushPromises();

      const syncBtn = wrapper.findAll('button').find((b) => b.text().includes('Sync Participations'));
      await syncBtn?.trigger('click');
      await flushPromises();

      const modal = wrapper.findComponent(SyncParticipationsModal);
      expect(modal.props('visible')).toBe(true);
    });

    it('clicking Archive Plan opens ArchivePlanModal', async () => {
      setupAuth([LEARNING_PLAN_ROLES.AUTHORITY], [LEARNING_PLAN_PERMISSIONS.APPROVE]);
      const wrapper = createWrapper(mockActivePlan);
      await flushPromises();

      const archiveBtn = wrapper.findAll('button').find((b) => b.text().includes('Archive Plan'));
      await archiveBtn?.trigger('click');
      await flushPromises();

      const modal = wrapper.findComponent(ArchivePlanModal);
      expect(modal.props('visible')).toBe(true);
    });
  });
});
