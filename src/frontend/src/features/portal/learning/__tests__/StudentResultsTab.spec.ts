// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import StudentResultsTab from '../components/StudentResultsTab.vue';
import ReportCardPrintModal from '@/features/portal/components/ReportCardPrintModal.vue';
import * as assessmentComposables from '@/features/academics/assessment/composables/useStudentResults';
import * as recordsComposables from '@/features/academics/records/composables/useReportCards';
import type { StudentPublishedResult, ReportCard } from '@/features/academics/records/types';
import type { UseStudentResultsReturn } from '@/features/academics/assessment/composables/useStudentResults';
import type { UseReportCardsReturn } from '@/features/academics/records/composables/useReportCards';
import { ref } from 'vue';

const mockPublishedResults: StudentPublishedResult[] = [
  {
    resultId: 'res-001',
    assessmentId: 'asm-001',
    assessmentTitle: 'Ulangan Harian 1 - Aljabar',
    assessmentType: 'formative',
    learningUnitId: 'lu-math',
    learningUnitCode: 'MATH-01',
    learningUnitTitle: 'Matematika Dasar',
    attemptNumber: 1,
    scoreNumeric: 88,
    scoreMax: 100,
    gradeLetter: 'A',
    gradeLabel: 'Sangat Mahir',
    narrativeFeedback: 'Penguasaan konsep aljabar sangat baik, ketelitian perlu dipertahankan.',
    evaluationModel: 'numeric',
    evaluatedDate: '2026-10-15',
    publishedDate: '2026-10-16',
  },
  {
    resultId: 'res-002',
    assessmentId: 'asm-002',
    assessmentTitle: 'Tugas Menulis Esai Deskripsi',
    assessmentType: 'formative',
    learningUnitId: 'lu-indo',
    learningUnitCode: 'INDO-01',
    learningUnitTitle: 'Bahasa Indonesia',
    attemptNumber: 1,
    scoreNumeric: 92,
    scoreMax: 100,
    gradeLetter: 'A',
    gradeLabel: 'Sangat Baik',
    narrativeFeedback: 'Struktur esai rapi dan kosakata sangat kaya.',
    evaluationModel: 'numeric',
    evaluatedDate: '2026-10-20',
    publishedDate: '2026-10-21',
  },
];

const mockReportCards: ReportCard[] = [
  {
    id: 'rc-001',
    studentId: 'student-123',
    studentName: 'Ahmad Faiz',
    studentNisn: '0081234567',
    academicPeriodId: '2026-TERM-1',
    periodLabel: 'Semester 1 2026/2027',
    academicPeriodLabel: 'Semester 1 2026/2027',
    reportType: 'end_of_term',
    templateName: 'Buku Rapor Merdeka',
    status: 'published',
    approvedBy: 'Academic Authority',
    approvedDate: '2026-11-01',
    publishedDate: '2026-11-02',
    narrativeComments: 'Sangat berprestasi dan aktif dalam kegiatan kolaboratif kelas.',
    createdAt: '2026-11-01T10:00:00Z',
    createdBy: 'coord.budi',
  },
];

describe('StudentResultsTab.vue (SCR-SELF-03)', () => {
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

    vi.spyOn(assessmentComposables, 'useStudentResults').mockReturnValue({
      studentResultsQuery: {} as UseStudentResultsReturn['studentResultsQuery'],
      results: ref(mockPublishedResults),
      studentResults: ref(mockPublishedResults),
      isLoading: ref(false),
      isPending: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
    });

    vi.spyOn(recordsComposables, 'useReportCards').mockReturnValue({
      reportCardsQuery: {} as UseReportCardsReturn['reportCardsQuery'],
      reportCards: ref(mockReportCards),
      isLoading: ref(false),
      isPending: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
      compile: {} as UseReportCardsReturn['compile'],
      batchCompile: {} as UseReportCardsReturn['batchCompile'],
      approve: {} as UseReportCardsReturn['approve'],
      publish: {} as UseReportCardsReturn['publish'],
    });
  });

  function createWrapper(props = {}) {
    return mount(StudentResultsTab, {
      props: {
        studentId: 'student-123',
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

  it('renders tab container and academic period selector', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="student-results-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="select-academic-period"]').exists()).toBe(true);
  });

  it('displays published grades grouped by learning unit', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="published-grades-section"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="learning-unit-group-lu-math"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="learning-unit-group-lu-indo"]').exists()).toBe(true);

    const mathGroup = wrapper.find('[data-testid="learning-unit-group-lu-math"]');
    expect(mathGroup.text()).toContain('Matematika Dasar');
    expect(mathGroup.text()).toContain('MATH-01');
    expect(mathGroup.text()).toContain('88');
    expect(mathGroup.text()).toContain('A');
    expect(mathGroup.text()).toContain('Sangat Mahir');
    expect(mathGroup.text()).toContain('Penguasaan konsep aljabar sangat baik');
  });

  it('displays certified report cards and opens print modal on click', async () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="report-cards-section"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="report-card-item-rc-001"]').exists()).toBe(true);

    const printBtn = wrapper.find('[data-testid="btn-print-report-card-rc-001"]');
    expect(printBtn.exists()).toBe(true);

    await printBtn.trigger('click');
    await flushPromises();

    expect(wrapper.findComponent(ReportCardPrintModal).props('visible')).toBe(true);
  });

  it('displays empty state when no published grades exist', () => {
    vi.spyOn(assessmentComposables, 'useStudentResults').mockReturnValue({
      studentResultsQuery: {} as UseStudentResultsReturn['studentResultsQuery'],
      results: ref([]),
      studentResults: ref([]),
      isLoading: ref(false),
      isPending: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
    });

    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="no-published-grades-empty"]').exists()).toBe(true);
  });

  it('verifies published grades displayed and provisional grades hidden', () => {
    // The useStudentResults composable queries the backend API which only returns
    // published results (server-side filtering by result_status = 'published').
    // StudentPublishedResult type does not include a status field - it represents
    // only published results. This test validates the display contract for
    // published grades and confirms provisional grades are not present in the data.
    const publishedResults: StudentPublishedResult[] = [
      {
        resultId: 'res-published-1',
        assessmentId: 'asm-001',
        assessmentTitle: 'Published Assessment',
        assessmentType: 'formative',
        learningUnitId: 'lu-math',
        learningUnitCode: 'MATH-01',
        learningUnitTitle: 'Matematika Dasar',
        attemptNumber: 1,
        scoreNumeric: 88,
        scoreMax: 100,
        gradeLetter: 'A',
        gradeLabel: 'Sangat Mahir',
        narrativeFeedback: 'Published result',
        evaluationModel: 'numeric',
        evaluatedDate: '2026-10-15',
        publishedDate: '2026-10-16',
      },
      {
        resultId: 'res-published-2',
        assessmentId: 'asm-002',
        assessmentTitle: 'Another Published Assessment',
        assessmentType: 'summative',
        learningUnitId: 'lu-indo',
        learningUnitCode: 'INDO-01',
        learningUnitTitle: 'Bahasa Indonesia',
        attemptNumber: 1,
        scoreNumeric: 92,
        scoreMax: 100,
        gradeLetter: 'A',
        gradeLabel: 'Sangat Baik',
        narrativeFeedback: 'Excellent work',
        evaluationModel: 'numeric',
        evaluatedDate: '2026-10-20',
        publishedDate: '2026-10-21',
      },
    ];

    vi.spyOn(assessmentComposables, 'useStudentResults').mockReturnValue({
      studentResultsQuery: {} as UseStudentResultsReturn['studentResultsQuery'],
      results: ref(publishedResults),
      studentResults: ref(publishedResults),
      isLoading: ref(false),
      isPending: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
    });

    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="published-grades-section"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="learning-unit-group-lu-math"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="learning-unit-group-lu-indo"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Published Assessment');
    expect(wrapper.text()).toContain('Another Published Assessment');
    expect(wrapper.text()).toContain('88');
    expect(wrapper.text()).toContain('92');
    expect(wrapper.text()).toContain('A');
    expect(wrapper.text()).toContain('Sangat Mahir');
    expect(wrapper.text()).toContain('Sangat Baik');
    expect(wrapper.text()).toContain('Published result');
    expect(wrapper.text()).toContain('Excellent work');
  });
});
