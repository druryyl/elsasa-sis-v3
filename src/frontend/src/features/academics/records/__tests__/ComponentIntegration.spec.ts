// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ReportCardPrintPreview from '../components/ReportCardPrintPreview.vue';
import AssessmentPlannerModal from '@/features/academics/assessment/components/AssessmentPlannerModal.vue';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

const globalPlugins = {
  plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
  stubs: {
    'router-link': true,
    Dialog: true,
  },
};

describe('SCR-ACD-07 & Portal Component Integration Tests (P10-S26)', () => {
  describe('ReportCardPrintPreview.vue', () => {
    const mockReportCard = {
      id: 'rc-1',
      studentId: 'stu-1',
      studentName: 'Budi Santoso',
      studentNisn: '123456',
      academicPeriodId: 'per-1',
      academicPeriodLabel: 'Semester 1',
      reportType: 'end_of_term' as const,
      templateName: 'Buku Rapor Merdeka',
      status: 'published' as const,
      contentSnapshot: JSON.stringify({
        reportCardId: 'rc-1',
        studentId: 'stu-1',
        studentName: 'Budi Santoso',
        studentNisn: '123456',
        academicPeriodId: 'per-1',
        periodLabel: 'Semester 1',
        reportType: 'end_of_term',
        templateName: 'Buku Rapor Merdeka',
        status: 'published',
        approvedBy: 'Kepala Sekolah',
        approvedDate: '2026-11-05',
        narrativeComments: 'Siswa sangat teladan.',
        subjectEntries: [
          {
            learningUnitId: 'lu-1',
            subjectCode: 'MAT',
            subjectTitle: 'Matematika',
            scoreNumeric: 85,
            gradeLetter: 'A',
            gradeLabel: 'Mahir',
            teacherComment: 'Sangat baik',
            displayOrder: 1,
          },
        ],
        attendanceSummary: {
          presentDays: 90,
          absentDays: 0,
          lateDays: 0,
          excusedDays: 1,
        },
      }),
    };

    it('renders frozen snapshot report card details and attendance', () => {
      const wrapper = mount(ReportCardPrintPreview, {
        props: { reportCard: mockReportCard },
        global: globalPlugins,
      });
      expect(wrapper.text()).toContain('Budi Santoso');
      expect(wrapper.text()).toContain('123456');
      expect(wrapper.text()).toContain('Matematika');
      expect(wrapper.text()).toContain('85');
    });
  });

  describe('AssessmentPlannerModal', () => {
    it('mounts successfully', () => {
      const wrapper = mount(AssessmentPlannerModal, {
        props: { visible: true, learningUnitId: 'lu-1' },
        global: globalPlugins,
      });
      expect(wrapper.exists()).toBe(true);
    });
  });
});