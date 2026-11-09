// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import AssessmentScoringGrid from '../components/AssessmentScoringGrid.vue';
import * as resultsApiModule from '../api/resultsApi';
import type { AcademicResult, Assessment } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

describe('AssessmentScoringGrid', () => {
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
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const sampleAssessment: Assessment = {
    id: 'assess-1',
    learningUnitId: 'unit-1',
    title: 'Unit 1 Formative Mastery Quiz',
    assessmentType: 'formative',
    assessmentMethod: 'written_exam',
    maxScore: 100,
    passingScore: 75,
    reassessmentPolicy: 'capped_at_passing',
    criteria: 'Concept definitions 50%, calculations 50%',
    status: 'conducted',
    evidenceCount: 2,
    resultCount: 2,
    createdAt: '2026-11-01T00:00:00Z',
    createdBy: 'teacher-1',
  };

  const enrolledStudents = [
    { id: 'student-1', name: 'Alice Smith', studentNumber: 'STD-1001' },
    { id: 'student-2', name: 'Bob Jones', studentNumber: 'STD-1002' },
  ];

  const sampleResults: AcademicResult[] = [
    {
      id: 'res-1',
      assessmentId: 'assess-1',
      studentId: 'student-1',
      attemptNumber: 1,
      isFinal: true,
      scoreNumeric: 85,
      scoreMax: 100,
      gradeLetter: 'A',
      gradeLabel: 'Proficient',
      evaluationModel: 'numeric',
      evaluatedBy: 'teacher-1',
      evaluatedDate: '2026-11-02',
      resultStatus: 'provisional',
      narrativeFeedback: 'Strong analytical reasoning.',
      createdAt: '2026-11-02T00:00:00Z',
      createdBy: 'teacher-1',
    },
    {
      id: 'res-2-attempt1',
      assessmentId: 'assess-1',
      studentId: 'student-2',
      attemptNumber: 1,
      isFinal: false,
      scoreNumeric: 60,
      scoreMax: 100,
      evaluationModel: 'numeric',
      evaluatedBy: 'teacher-1',
      evaluatedDate: '2026-11-02',
      resultStatus: 'provisional',
      createdAt: '2026-11-02T00:00:00Z',
      createdBy: 'teacher-1',
    },
    {
      id: 'res-2-attempt2',
      assessmentId: 'assess-1',
      studentId: 'student-2',
      attemptNumber: 2,
      replacesResultId: 'res-2-attempt1',
      isFinal: true,
      scoreNumeric: 75,
      scoreMax: 100,
      gradeLetter: 'C',
      evaluationModel: 'numeric',
      evaluatedBy: 'teacher-1',
      evaluatedDate: '2026-11-04',
      resultStatus: 'provisional',
      narrativeFeedback: 'Remediation completed successfully.',
      createdAt: '2026-11-04T00:00:00Z',
      createdBy: 'teacher-1',
    },
  ];

  const commonStubs = {
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
    ToggleSwitch: {
      props: ['modelValue'],
      emits: ['update:modelValue'],
      template: `
        <button
          type="button"
          role="switch"
          :aria-checked="modelValue"
          data-testid="scoring-publish-toggle"
          @click="$emit('update:modelValue', !modelValue)"
        />
      `,
    },
  };

  it('renders summary metrics strip with pass rate and enrolled count', async () => {
    const wrapper = mount(AssessmentScoringGrid, {
      props: {
        assessment: sampleAssessment,
        enrolledStudents,
        results: sampleResults,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('Enrolled Cohort');
    expect(wrapper.text()).toContain('2');
    expect(wrapper.text()).toContain('Evaluated');
    expect(wrapper.text()).toContain('KKM Pass Rate');
    expect(wrapper.text()).toContain('100%'); // both operative scores (85 and 75) meet KKM 75
  });

  it('displays candidate rows with operative mark badge and attempt lineage', async () => {
    const wrapper = mount(AssessmentScoringGrid, {
      props: {
        assessment: sampleAssessment,
        enrolledStudents,
        results: sampleResults,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    // Check Alice Smith
    const aliceRow = wrapper.find('[data-testid="scoring-row-student-1"]');
    expect(aliceRow.exists()).toBe(true);
    expect(aliceRow.text()).toContain('Alice Smith');
    expect(aliceRow.text()).toContain('85');
    expect(aliceRow.text()).toContain('Operative');
    expect(aliceRow.text()).toContain('Meets Passing Standard (KKM)');

    // Check Bob Jones (Attempt 2 retake)
    const bobRow = wrapper.find('[data-testid="scoring-row-student-2"]');
    expect(bobRow.exists()).toBe(true);
    expect(bobRow.text()).toContain('Bob Jones');
    expect(bobRow.text()).toContain('Attempt #2');
    expect(bobRow.text()).toContain('View all 2');
    expect(bobRow.text()).toContain('75');
  });

  it('filters roster by search query', async () => {
    const wrapper = mount(AssessmentScoringGrid, {
      props: {
        assessment: sampleAssessment,
        enrolledStudents,
        results: sampleResults,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const searchInput = wrapper.find('[data-testid="scoring-search-input"]');
    await searchInput.setValue('Alice');

    expect(wrapper.find('[data-testid="scoring-row-student-1"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="scoring-row-student-2"]').exists()).toBe(false);
  });

  it('opens evaluation modal and submits evaluation with publishImmediately autonomous formative release', async () => {
    const evaluateSpy = vi.spyOn(resultsApiModule.resultsApi, 'evaluate').mockResolvedValue({
      id: 'res-new',
      assessmentId: 'assess-1',
      studentId: 'student-1',
      attemptNumber: 2,
      isFinal: true,
      scoreNumeric: 95,
      resultStatus: 'published',
      evaluatedDate: '2026-11-05',
    } as any);

    const wrapper = mount(AssessmentScoringGrid, {
      props: {
        assessment: sampleAssessment,
        enrolledStudents,
        results: sampleResults,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    // Click score on Alice
    const scoreBtn = wrapper.find('[data-testid="score-student-btn-student-1"]');
    await scoreBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="score-input-modal"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Release Formative Feedback Immediately');

    // Submit score
    const form = wrapper.find('[data-testid="score-input-modal"] form');
    await form.trigger('submit.prevent');
    await flushPromises();

    expect(evaluateSpy).toHaveBeenCalledWith(
      'assess-1',
      expect.objectContaining({
        studentId: 'student-1',
        publishImmediately: true, // Formative defaults to true for autonomous release
      }),
    );
  });

  it('triggers quick release formative feedback button', async () => {
    const publishSpy = vi.spyOn(resultsApiModule.resultsApi, 'publish').mockResolvedValue({
      publishedCount: 1,
    });

    const wrapper = mount(AssessmentScoringGrid, {
      props: {
        assessment: sampleAssessment,
        enrolledStudents,
        results: sampleResults,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });

    await flushPromises();

    const quickReleaseBtn = wrapper.find('[data-testid="quick-release-btn-student-1"]');
    expect(quickReleaseBtn.exists()).toBe(true);
    await quickReleaseBtn.trigger('click');
    await flushPromises();

    expect(publishSpy).toHaveBeenCalledWith(
      'assess-1',
      expect.objectContaining({
        resultIds: ['res-1'],
      }),
    );
  });
});
