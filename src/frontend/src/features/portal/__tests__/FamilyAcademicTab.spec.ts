// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import FamilyAcademicTab from '../FamilyAcademicTab.vue';
import ReportCardPrintModal from '../components/ReportCardPrintModal.vue';
import * as assessmentComposables from '@/features/academics/assessment/composables/useStudentResults';
import * as recordsComposables from '@/features/academics/records/composables/useReportCards';
import type { StudentPublishedResult, ReportCard } from '@/features/academics/records/types';
import type { UseStudentResultsReturn } from '@/features/academics/assessment/composables/useStudentResults';
import type { UseReportCardsReturn } from '@/features/academics/records/composables/useReportCards';
import { ref } from 'vue';

const mockChildResults: StudentPublishedResult[] = [
  {
    resultId: 'child-res-1',
    assessmentId: 'asm-science',
    assessmentTitle: 'Praktikum Sains & Lingkungan',
    assessmentType: 'formative',
    learningUnitId: 'lu-science',
    learningUnitCode: 'SCI-01',
    learningUnitTitle: 'Ilmu Pengetahuan Alam',
    attemptNumber: 1,
    scoreNumeric: 95,
    scoreMax: 100,
    gradeLetter: 'A',
    gradeLabel: 'Sangat Baik',
    narrativeFeedback: 'Ananda sangat antusias dalam eksperimen dan mampu berkolaborasi aktif dengan kelompok.',
    evaluationModel: 'numeric',
    evaluatedDate: '2026-10-25',
    publishedDate: '2026-10-26',
  },
];

const mockChildReportCards: ReportCard[] = [
  {
    id: 'rc-child-1',
    studentId: 'child-101',
    studentName: 'Zahra Salsabila',
    studentNisn: '0098765432',
    academicPeriodId: '2026-TERM-1',
    periodLabel: 'Semester Ganjil 2026/2027',
    academicPeriodLabel: 'Semester Ganjil 2026/2027',
    reportType: 'end_of_term',
    templateName: 'Buku Rapor Merdeka',
    status: 'published',
    approvedBy: 'Kepala Sekolah',
    approvedDate: '2026-11-03',
    publishedDate: '2026-11-04',
    narrativeComments: 'Perkembangan karakter dan ketuntasan belajar ananda sangat membanggakan.',
    createdAt: '2026-11-03T08:00:00Z',
    createdBy: 'teacher.fatimah',
  },
];

describe('FamilyAcademicTab.vue (SCR-SELF-02)', () => {
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
      results: ref(mockChildResults),
      studentResults: ref(mockChildResults),
      isLoading: ref(false),
      isPending: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
    });

    vi.spyOn(recordsComposables, 'useReportCards').mockReturnValue({
      reportCardsQuery: {} as UseReportCardsReturn['reportCardsQuery'],
      reportCards: ref(mockChildReportCards),
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
    return mount(FamilyAcademicTab, {
      props: {
        studentId: 'child-101',
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

  it('renders family academic tab with period select and section titles', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="family-academic-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="family-select-period"]').exists()).toBe(true);
  });

  it('displays child academic progress with prominent teacher remarks', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="family-progress-section"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="family-subject-group-lu-science"]').exists()).toBe(true);

    const scienceGroup = wrapper.find('[data-testid="family-subject-group-lu-science"]');
    expect(scienceGroup.text()).toContain('Ilmu Pengetahuan Alam');
    expect(scienceGroup.text()).toContain('SCI-01');
    expect(scienceGroup.text()).toContain('95');
    expect(scienceGroup.text()).toContain('A');

    const teacherRemark = wrapper.find('[data-testid="family-teacher-remark"]');
    expect(teacherRemark.exists()).toBe(true);
    expect(teacherRemark.text()).toContain('Ananda sangat antusias dalam eksperimen');
  });

  it('displays official Buku Rapor and opens print modal on click', async () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="family-report-cards-section"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="family-report-card-rc-child-1"]').exists()).toBe(true);

    const printBtn = wrapper.find('[data-testid="btn-print-buku-rapor-rc-child-1"]');
    expect(printBtn.exists()).toBe(true);

    await printBtn.trigger('click');
    await flushPromises();

    expect(wrapper.findComponent(ReportCardPrintModal).props('visible')).toBe(true);
  });

  it('displays empty states when child has no records for the period', () => {
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
    vi.spyOn(recordsComposables, 'useReportCards').mockReturnValue({
      reportCardsQuery: {} as UseReportCardsReturn['reportCardsQuery'],
      reportCards: ref([]),
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

    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="family-no-progress-empty"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="family-no-report-cards"]').exists()).toBe(true);
  });
});
