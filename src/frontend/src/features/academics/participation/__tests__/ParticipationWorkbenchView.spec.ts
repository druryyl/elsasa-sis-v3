// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ParticipationWorkbenchView from '../views/ParticipationWorkbenchView.vue';
import BatchEnrollCohortModal from '../components/BatchEnrollCohortModal.vue';
import RegisterStudentModal from '../components/RegisterStudentModal.vue';
import RecognizePriorCreditModal from '../components/RecognizePriorCreditModal.vue';
import ConcludeParticipationDialog from '../components/ConcludeParticipationDialog.vue';
import UpdateParticipationStatusDialog from '../components/UpdateParticipationStatusDialog.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { PARTICIPATION_PERMISSIONS } from '../constants/permissions';
import * as academicYearApi from '@/features/operations/calendar/api/academicYear.api';
import * as curriculumApi from '@/features/academics/curriculum/api/curriculumApi';
import * as learningPlansApi from '@/features/academics/learning-plans/api/learningPlansApi';
import * as participationApi from '../api/participation.api';
import type { AcademicParticipation } from '../types';

const mockAcademicYears: academicYearApi.AcademicYearDto[] = [
  {
    id: 'ay-2024-2025',
    code: 'AY-2024-2025',
    name: 'Tahun Pelajaran 2024/2025',
    startDate: '2024-07-01',
    endDate: '2025-06-30',
    status: 'active',
    isActive: true,
  },
];

const mockStructures = [
  {
    id: 'struct-1',
    name: 'Kurikulum Merdeka 2024',
    academicPeriodId: 'ay-2024-2025',
    academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
    version: '2024.1',
    status: 'active',
    effectiveDate: '2024-07-01',
    endDate: null,
    supersedesId: null,
    learningUnitsCount: 3,
    academicRequirementsCount: 1,
    createdAt: '2024-06-01T00:00:00Z',
    updatedAt: null,
  },
];

const mockUnits = [
  {
    id: 'unit-1',
    learningStructureId: 'struct-1',
    code: 'MATH-101',
    name: 'Algebra I',
    unitType: 'subject',
    parentUnitId: null,
    level: 'Grade 10',
    creditWeight: 4.0,
    status: 'active',
    description: 'Foundational algebra',
    notes: null,
    createdAt: '2024-06-01T00:00:00Z',
    createdBy: 'coord-1',
    updatedAt: null,
    updatedBy: null,
  },
];

const mockParticipations: AcademicParticipation[] = [
  {
    id: 'part-1',
    studentId: 'student-uuid-1',
    studentNumber: 'STD-1001',
    studentName: 'Ahmad Faiz',
    learningUnitId: 'unit-1',
    academicPeriodId: 'ay-2024-2025',
    sectionLabel: '10-A',
    attemptNumber: 1,
    status: 'in_progress',
    participationType: 'standard',
    source: 'assigned',
    startDate: '2024-07-15',
    completionDate: null,
    endDate: null,
    sourceInstitution: null,
    externalGradeOrCredit: null,
    notes: 'Regular enrollment',
    createdAt: '2024-07-01T00:00:00Z',
    updatedAt: null,
  },
  {
    id: 'part-2',
    studentId: 'student-uuid-2',
    studentNumber: 'STD-1002',
    studentName: 'Siti Rahma',
    learningUnitId: 'unit-1',
    academicPeriodId: 'ay-2024-2025',
    sectionLabel: '10-B',
    attemptNumber: 2,
    status: 'completed',
    participationType: 'repeat',
    source: 'assigned',
    startDate: '2024-07-15',
    completionDate: '2024-12-20',
    endDate: '2024-12-20',
    sourceInstitution: null,
    externalGradeOrCredit: null,
    notes: 'Repeat attempt passed',
    createdAt: '2024-07-01T00:00:00Z',
    updatedAt: null,
  },
  {
    id: 'part-3',
    studentId: 'student-uuid-3',
    studentNumber: 'STD-1003',
    studentName: 'Budi Santoso',
    learningUnitId: 'unit-1',
    academicPeriodId: 'ay-2024-2025',
    sectionLabel: '10-A',
    attemptNumber: 1,
    status: 'paused',
    participationType: 'standard',
    source: 'learning_plan',
    studentLearningPlanId: 'slp-1',
    startDate: '2024-07-15',
    completionDate: null,
    endDate: null,
    sourceInstitution: null,
    externalGradeOrCredit: null,
    notes: 'Medical leave',
    createdAt: '2024-07-01T00:00:00Z',
    updatedAt: null,
  },
];

describe('ParticipationWorkbenchView.vue (SCR-ACD-05)', () => {
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
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          gcTime: 0,
        },
      },
    });

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue(mockAcademicYears);
    vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue({
      items: mockStructures,
      totalCount: mockStructures.length,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });
    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits);
    vi.spyOn(participationApi, 'getParticipationsByUnit').mockResolvedValue(mockParticipations);
    const mockLearningPlans = {
      items: [
        {
          id: 'slp-1',
          studentId: 'stu-1',
          academicPeriodId: 'ay-2024-2025',
          academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
          version: 1,
          status: 'active' as const,
          constituentUnitsCount: 1,
          adjustmentsCount: 0,
          createdAt: '2024-07-01T00:00:00Z',
          updatedAt: null,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 100,
      totalPages: 1,
    };
    vi.spyOn(learningPlansApi, 'getLearningPlans').mockResolvedValue(mockLearningPlans);
    vi.spyOn(learningPlansApi.learningPlansApi, 'getLearningPlans').mockResolvedValue(mockLearningPlans);

    const authStore = useAuthStore();
    authStore.setSession('token-coord', {
      userId: 'coord-1',
      username: 'coordinator',
      fullName: 'Academic Coordinator',
      roles: ['AcademicCoordinator'],
      permissions: [
        PARTICIPATION_PERMISSIONS.VIEW,
        PARTICIPATION_PERMISSIONS.MANAGE,
        PARTICIPATION_PERMISSIONS.RECOGNIZE_PRIOR,
        PARTICIPATION_PERMISSIONS.CONCLUDE,
      ],
      campusId: null,
    });
  });

  function createWrapper() {
    return mount(ParticipationWorkbenchView, {
      global: {
        plugins: [
          PrimeVue,
          ConfirmationService,
          [VueQueryPlugin, { queryClient }],
        ],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
          Select: {
            props: ['modelValue', 'options', 'placeholder', 'loading', 'disabled'],
            emits: ['update:modelValue'],
            template: `
              <div class="p-select" :data-loading="loading">
                <select
                  :value="modelValue"
                  @change="$emit('update:modelValue', $event.target.value)"
                  :disabled="disabled"
                >
                  <option v-for="opt in options" :key="opt.id || opt.value" :value="opt.id || opt.value">
                    {{ opt.label || opt.name }}
                  </option>
                </select>
              </div>
            `,
          },
          BatchEnrollCohortModal: {
            props: ['visible', 'periods', 'units', 'initialPeriodId', 'initialSectionLabel'],
            emits: ['update:visible', 'enrolled'],
            template: '<div v-if="visible" data-testid="mock-batch-enroll-modal"><slot /></div>',
          },
          RegisterStudentModal: {
            props: ['visible', 'periods', 'units', 'initialPeriodId', 'initialUnitId'],
            emits: ['update:visible', 'registered'],
            template: '<div v-if="visible" data-testid="mock-register-student-modal"><slot /></div>',
          },
          RecognizePriorCreditModal: {
            props: ['visible', 'periods', 'units', 'initialPeriodId', 'initialUnitId'],
            emits: ['update:visible', 'recognized'],
            template: '<div v-if="visible" data-testid="mock-recognize-credit-modal"><slot /></div>',
          },
          ConcludeParticipationDialog: {
            props: ['visible', 'participation'],
            emits: ['update:visible', 'concluded'],
            template: '<div v-if="visible" data-testid="mock-conclude-dialog"><slot /></div>',
          },
          UpdateParticipationStatusDialog: {
            props: ['visible', 'participation'],
            emits: ['update:visible', 'updated'],
            template: '<div v-if="visible" data-testid="mock-update-status-dialog"><slot /></div>',
          },
        },
      },
    });
  }

  it('renders master workbench, summary metrics, and participant roster table', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Verify view container and header
    expect(wrapper.find('[data-testid="participation-workbench-view"]').exists()).toBe(true);

    // Verify summary metrics ribbon
    const totalMetric = wrapper.find('[data-testid="metric-total-participants"]');
    expect(totalMetric.exists()).toBe(true);
    expect(totalMetric.text()).toContain('3');

    const inProgressMetric = wrapper.find('[data-testid="metric-in-progress"]');
    expect(inProgressMetric.text()).toContain('1');

    const completedMetric = wrapper.find('[data-testid="metric-completed"]');
    expect(completedMetric.text()).toContain('1');

    const pausedMetric = wrapper.find('[data-testid="metric-paused-withdrawn"]');
    expect(pausedMetric.text()).toContain('1');

    // Verify roster table rendered with participants
    const datatable = wrapper.find('[data-testid="participations-datatable"]');
    expect(datatable.exists()).toBe(true);
    expect(datatable.text()).toContain('Ahmad Faiz');
    expect(datatable.text()).toContain('Siti Rahma');
    expect(datatable.text()).toContain('Budi Santoso');
    expect(datatable.text()).toContain('Section 10-A');
    expect(datatable.text()).toContain('Section 10-B');
  });

  it('filters participants via search input (cohort filter interaction)', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const searchInput = wrapper.find('[data-testid="input-student-search"]');
    expect(searchInput.exists()).toBe(true);

    // Search for "Faiz"
    await searchInput.setValue('Faiz');
    await flushPromises();

    const tableText = wrapper.find('[data-testid="participations-datatable"]').text();
    expect(tableText).toContain('Ahmad Faiz');
    expect(tableText).not.toContain('Siti Rahma');
    expect(tableText).not.toContain('Budi Santoso');

    // Clear search
    await searchInput.setValue('');
    await flushPromises();
    const fullTableText = wrapper.find('[data-testid="participations-datatable"]').text();
    expect(fullTableText).toContain('Ahmad Faiz');
    expect(fullTableText).toContain('Siti Rahma');
  });

  it('launches modals: Batch Enroll Cohort, Register Student, and Recognize Prior Credit', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // 1. Batch Enroll Cohort
    const batchBtn = wrapper.find('[data-testid="btn-batch-enroll"]');
    expect(batchBtn.exists()).toBe(true);
    await batchBtn.trigger('click');
    await flushPromises();
    expect(wrapper.findComponent(BatchEnrollCohortModal).props('visible')).toBe(true);

    // 2. Register Student
    const registerBtn = wrapper.find('[data-testid="btn-register-student"]');
    expect(registerBtn.exists()).toBe(true);
    await registerBtn.trigger('click');
    await flushPromises();
    expect(wrapper.findComponent(RegisterStudentModal).props('visible')).toBe(true);

    // 3. Recognize Prior Credit
    const recognizeBtn = wrapper.find('[data-testid="btn-recognize-credit"]');
    expect(recognizeBtn.exists()).toBe(true);
    await recognizeBtn.trigger('click');
    await flushPromises();
    expect(wrapper.findComponent(RecognizePriorCreditModal).props('visible')).toBe(true);
  });

  it('launches Conclude and Update Status dialogs from row actions', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Conclude button for first row (Ahmad Faiz - in_progress)
    const concludeButtons = wrapper.findAll('[data-testid="btn-conclude-row"]');
    expect(concludeButtons.length).toBeGreaterThan(0);
    await concludeButtons[0].trigger('click');
    await flushPromises();

    const concludeDialog = wrapper.findComponent(ConcludeParticipationDialog);
    expect(concludeDialog.props('visible')).toBe(true);
    expect(concludeDialog.props('participation')?.studentName).toBe('Ahmad Faiz');

    // Update status button for first row
    const updateButtons = wrapper.findAll('[data-testid="btn-update-status-row"]');
    expect(updateButtons.length).toBeGreaterThan(0);
    await updateButtons[0].trigger('click');
    await flushPromises();

    const updateDialog = wrapper.findComponent(UpdateParticipationStatusDialog);
    expect(updateDialog.props('visible')).toBe(true);
    expect(updateDialog.props('participation')?.studentName).toBe('Ahmad Faiz');
  });

  it('displays "Active Learning Plan" badge on students with an active SLP', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const slpBadge = wrapper.find('[data-testid="badge-active-slp"]');
    expect(slpBadge.exists()).toBe(true);
    expect(slpBadge.text()).toContain('Active Learning Plan');
  });

  it('renders empty state when unit has no enrolled participants', async () => {
    vi.spyOn(participationApi, 'getParticipationsByUnit').mockResolvedValue([]);

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.text()).toContain('No student participations found');
  });
});
