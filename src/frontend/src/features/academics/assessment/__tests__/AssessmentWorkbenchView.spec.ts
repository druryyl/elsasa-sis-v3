// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import AssessmentWorkbenchView from '../views/AssessmentWorkbenchView.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as curriculumApi from '@/features/academics/curriculum/api/curriculumApi';
import * as assessmentsApiModule from '../api/assessmentsApi';
import * as resultsApiModule from '../api/resultsApi';
import * as participationApiModule from '@/features/academics/participation/api/participation.api';
import type { Assessment, AcademicResult } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

const mockPush = vi.fn();
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRouter: () => ({
      push: mockPush,
    }),
  };
});

describe('AssessmentWorkbenchView', () => {
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
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();

    const authStore = useAuthStore();
    authStore.user = {
      id: 'teacher-1',
      username: 'teacher',
      email: 'teacher@school.test',
      fullName: 'Teacher User',
      isActive: true,
      roles: ['Teacher'],
      permissions: [
        'academics.assessments.view',
        'academics.assessments.manage',
        'academics.assessments.conduct',
        'academics.results.evaluate',
      ],
    };
  });

  const mockStructures = [
    {
      id: 'struct-1',
      name: 'Kurikulum Merdeka 2024',
      academicPeriodId: 'ay-2024-2025',
      academicPeriodLabel: 'Tahun Pelajaran 2024/2025',
      version: '2024.1',
      status: 'active',
      effectiveDate: '2024-07-01',
      learningUnitsCount: 1,
      academicRequirementsCount: 0,
      createdAt: '2024-07-01T00:00:00Z',
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
      createdAt: '2024-07-01T00:00:00Z',
      createdBy: 'coord-1',
      updatedAt: null,
      updatedBy: null,
    },
  ];

  const mockAssessments: Assessment[] = [
    {
      id: 'assess-1',
      learningUnitId: 'unit-1',
      learningUnitCode: 'MATH-101',
      learningUnitTitle: 'Algebra I',
      title: 'Unit 1 Formative Mastery Check',
      assessmentType: 'formative',
      assessmentMethod: 'written_exam',
      maxScore: 100,
      passingScore: 75,
      reassessmentPolicy: 'capped_at_passing',
      criteria: 'Solving equations rubric',
      status: 'planned',
      evidenceCount: 1,
      resultCount: 1,
      createdAt: '2026-11-01T00:00:00Z',
      createdBy: 'teacher-1',
    },
  ];

  const mockParticipations = [
    {
      id: 'part-1',
      studentId: 'student-1',
      studentName: 'Alice Smith',
      studentNumber: 'STD-1001',
      learningUnitId: 'unit-1',
      academicPeriodId: 'ay-2024-2025',
      academicPeriodLabel: '2024/2025',
      participationType: 'standard',
      attemptNumber: 1,
      source: 'assigned',
      status: 'in_progress',
      startDate: '2026-07-01',
      createdAt: '2026-07-01T00:00:00Z',
      createdBy: 'coord-1',
    },
  ];

  const mockResults: AcademicResult[] = [
    {
      id: 'res-1',
      assessmentId: 'assess-1',
      studentId: 'student-1',
      attemptNumber: 1,
      isFinal: true,
      scoreNumeric: 88,
      scoreMax: 100,
      evaluationModel: 'numeric',
      evaluatedBy: 'teacher-1',
      evaluatedDate: '2026-11-02',
      resultStatus: 'provisional',
      createdAt: '2026-11-02T00:00:00Z',
      createdBy: 'teacher-1',
    },
  ];

  const commonStubs = {
    Select: {
      props: ['modelValue', 'options'],
      emits: ['update:modelValue'],
      template: `
        <select
          :value="modelValue"
          @change="$emit('update:modelValue', $event.target.value)"
        >
          <option v-for="opt in options" :key="opt.value" :value="opt.value">
            {{ opt.label }}
          </option>
        </select>
      `,
    },
    Dialog: {
      props: ['visible', 'header'],
      emits: ['update:visible'],
      template: `
        <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid'] || 'p-dialog'">
          <div class="p-dialog-header">{{ header }}</div>
          <slot />
        </div>
      `,
    },
    Drawer: {
      props: ['visible'],
      emits: ['update:visible'],
      template: `
        <div v-if="visible" class="p-drawer" data-testid="p-drawer">
          <slot name="header" />
          <slot />
        </div>
      `,
    },
  };

  it('renders workbench with learning units, assessment cards, and scoring grid', async () => {
    vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue(mockStructures as any);
    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits as any);
    vi.spyOn(assessmentsApiModule.assessmentsApi, 'listByUnit').mockResolvedValue(mockAssessments);
    vi.spyOn(participationApiModule, 'getParticipationsByUnit').mockResolvedValue(mockParticipations as any);
    vi.spyOn(resultsApiModule.resultsApi, 'listByAssessment').mockResolvedValue(mockResults);

    const wrapper = mount(AssessmentWorkbenchView, {
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          ...commonStubs,
          RouterLink: {
            props: ['to'],
            template: '<a :href="to"><slot /></a>',
          },
        },
      },
    });

    await flushPromises();

    expect(wrapper.find('[data-testid="assessment-workbench-view"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Coursework & Assessment Workbench');
    expect(wrapper.text()).toContain('Algebra I');
    expect(wrapper.text()).toContain('Unit 1 Formative Mastery Check');
    expect(wrapper.find('[data-testid="assessment-scoring-grid"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Alice Smith');
    expect(wrapper.text()).toContain('88');
  });

  it('navigates to coursework desk when switch button is clicked', async () => {
    vi.spyOn(curriculumApi, 'getLearningStructures').mockResolvedValue(mockStructures as any);
    vi.spyOn(curriculumApi, 'getLearningUnits').mockResolvedValue(mockUnits as any);
    vi.spyOn(assessmentsApiModule.assessmentsApi, 'listByUnit').mockResolvedValue(mockAssessments);
    vi.spyOn(participationApiModule, 'getParticipationsByUnit').mockResolvedValue(mockParticipations as any);
    vi.spyOn(resultsApiModule.resultsApi, 'listByAssessment').mockResolvedValue(mockResults);

    const wrapper = mount(AssessmentWorkbenchView, {
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          ...commonStubs,
          RouterLink: {
            props: ['to'],
            template: '<a :href="to"><slot /></a>',
          },
        },
      },
    });

    await flushPromises();

    const switchBtn = wrapper.find('[data-testid="switch-to-coursework-btn"]');
    await switchBtn.trigger('click');

    expect(mockPush).toHaveBeenCalledWith('/academics/coursework');
  });
});
