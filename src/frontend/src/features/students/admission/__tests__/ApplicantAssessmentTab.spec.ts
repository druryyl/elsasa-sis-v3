// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, beforeAll } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import ApplicantAssessmentTab from '../components/tabs/ApplicantAssessmentTab.vue';
import type { ApplicantEntranceAssessmentScoreDto } from '../api/applicantAssessmentApi';

const mockApplicantScores: ApplicantEntranceAssessmentScoreDto[] = [
  {
    resultId: 'app-res-001',
    assessmentId: 'asm-diag-math',
    assessmentTitle: 'Diagnostic Entrance Exam - Mathematics & Quantitative Logic',
    assessmentType: 'diagnostic',
    applicantId: 'applicant-99',
    scoreNumeric: 87.5,
    scoreMax: 100,
    gradeLetter: 'A',
    gradeLabel: 'Sangat Baik',
    evaluationModel: 'numeric',
    resultStatus: 'published',
    evaluatedBy: 'teacher.santoso',
    evaluatedDate: '2026-10-18',
    assessorComments: 'Calon peserta didik menguasai logika spasial dan aritmatika dasar dengan sangat memuaskan.',
  },
  {
    resultId: 'app-res-002',
    assessmentId: 'asm-diag-lit',
    assessmentTitle: 'Diagnostic Entrance Exam - English & Reading Comprehension',
    assessmentType: 'diagnostic',
    applicantId: 'applicant-99',
    scoreNumeric: 91,
    scoreMax: 100,
    gradeLetter: 'A',
    gradeLabel: 'Mahir',
    evaluationModel: 'numeric',
    resultStatus: 'published',
    evaluatedBy: 'teacher.clara',
    evaluatedDate: '2026-10-19',
    assessorComments: 'Vocabulary and textual comprehension are well above grade level.',
  },
];

describe('ApplicantAssessmentTab.vue (SCR-STU-01, GAP-07)', () => {
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
    }
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });
  });

  function createWrapper(props = {}) {
    return mount(ApplicantAssessmentTab, {
      props: {
        applicantId: 'applicant-99',
        applicationId: 'app-100',
        applicantName: 'Faris Ramadhan',
        scores: mockApplicantScores,
        ...props,
      },
      global: {
        plugins: [
          createPinia(),
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
          ToastService,
        ],
      },
    });
  }

  it('renders applicant assessment tab with header and score list', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="applicant-assessment-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="entrance-assessment-list"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="entrance-assessment-metrics"]').exists()).toBe(true);
  });

  it('displays authorized score numeric, max score, grade letter, and assessor comments', () => {
    const wrapper = createWrapper();

    const mathCard = wrapper.find('[data-testid="entrance-assessment-card-app-res-001"]');
    expect(mathCard.exists()).toBe(true);
    expect(mathCard.text()).toContain('Mathematics & Quantitative Logic');
    expect(mathCard.text()).toContain('87.5');
    expect(mathCard.text()).toContain('100');
    expect(mathCard.text()).toContain('A');
    expect(mathCard.text()).toContain('Sangat Baik');
    expect(mathCard.text()).toContain('Calon peserta didik menguasai logika spasial');

    const litCard = wrapper.find('[data-testid="entrance-assessment-card-app-res-002"]');
    expect(litCard.exists()).toBe(true);
    expect(litCard.text()).toContain('English & Reading Comprehension');
    expect(litCard.text()).toContain('91');
    expect(litCard.text()).toContain('Vocabulary and textual comprehension are well above grade level');
  });

  it('calculates average and highest scores accurately in metrics bar', () => {
    const wrapper = createWrapper();
    const metricsBar = wrapper.find('[data-testid="entrance-assessment-metrics"]');
    expect(metricsBar.text()).toContain('2'); // Total evaluations
    expect(metricsBar.text()).toContain('89.3'); // (87.5 + 91) / 2 = 89.25 rounded to 89.3
    expect(metricsBar.text()).toContain('91'); // Max score
  });

  it('displays empty state when no entrance exams have been recorded', () => {
    const wrapper = createWrapper({ scores: [] });
    expect(wrapper.find('[data-testid="no-entrance-assessments-empty"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No Academics Diagnostic Exam Results Recorded');
  });
});
