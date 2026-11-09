// @vitest-environment jsdom
import { describe, it, expect, vi, beforeAll } from 'vitest';
import { mount } from '@vue/test-utils';
import ReportCardPrintPreview from '../components/ReportCardPrintPreview.vue';
import type { ReportCard, ReportCardSnapshot } from '../types';

const mockSnapshot: ReportCardSnapshot = {
  reportCardId: 'rc-snap-100',
  studentId: 'stu-500',
  studentName: 'Muhammad Ilham',
  studentNisn: '0076543210',
  academicPeriodId: '2026-TERM-1',
  periodLabel: 'Semester Ganjil 2026/2027',
  reportType: 'end_of_term',
  templateName: 'Buku Rapor Merdeka',
  status: 'published',
  approvedBy: 'Drs. H. Salim M.Pd.',
  approvedDate: '2026-11-04',
  narrativeComments: 'Siswa menunjukkan integritas tinggi dan kepemimpinan yang baik.',
  subjectEntries: [
    {
      learningUnitId: 'lu-math',
      subjectCode: 'MTK-7',
      subjectTitle: 'Matematika',
      scoreNumeric: 86,
      gradeLetter: 'A',
      gradeLabel: 'Sangat Mahir',
      teacherComment: 'Mampu menyelesaikan masalah numerik dengan tepat.',
      displayOrder: 1,
    },
    {
      learningUnitId: 'lu-eng',
      subjectCode: 'ENG-7',
      subjectTitle: 'Bahasa Inggris',
      scoreNumeric: 90,
      gradeLetter: 'A',
      gradeLabel: 'Mahir',
      teacherComment: 'Active in group conversations and presentations.',
      displayOrder: 2,
    },
  ],
  attendanceSummary: {
    presentDays: 85,
    absentDays: 0,
    lateDays: 1,
    excusedDays: 2,
  },
};

const mockReportCard: ReportCard = {
  id: 'rc-snap-100',
  studentId: 'stu-500',
  studentName: 'Muhammad Ilham',
  studentNisn: '0076543210',
  academicPeriodId: '2026-TERM-1',
  academicPeriodLabel: 'Semester Ganjil 2026/2027',
  reportType: 'end_of_term',
  templateName: 'Buku Rapor Merdeka',
  status: 'published',
  contentSnapshot: JSON.stringify(mockSnapshot),
  approvedBy: 'Drs. H. Salim M.Pd.',
  approvedDate: '2026-11-04',
  createdAt: '2026-11-04T08:00:00Z',
  createdBy: 'authority.salim',
};

describe('ReportCardPrintPreview.vue (Buku Rapor Merdeka)', () => {
  beforeAll(() => {
    if (typeof window !== 'undefined') {
      window.print = vi.fn();
    }
  });

  function createWrapper(props = {}) {
    return mount(ReportCardPrintPreview, {
      props: {
        reportCard: mockReportCard,
        ...props,
      },
    });
  }

  it('renders official school header and Buku Rapor title', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="report-card-print-preview"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Sekolah Menengah Elsasa');
    expect(wrapper.text()).toContain('Yayasan Pendidikan Elsasa Islamic School');
    expect(wrapper.text()).toContain('Laporan Hasil Belajar');
  });

  it('displays student details and period from frozen snapshot', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="print-student-name"]').text()).toBe('Muhammad Ilham');
    expect(wrapper.find('[data-testid="print-student-nisn"]').text()).toBe('0076543210');
    expect(wrapper.find('[data-testid="print-period-label"]').text()).toBe('Semester Ganjil 2026/2027');
  });

  it('displays subjects table with numeric score, grade letter, and teacher remarks', () => {
    const wrapper = createWrapper();
    const table = wrapper.find('[data-testid="print-subjects-table"]');
    expect(table.exists()).toBe(true);
    expect(table.text()).toContain('Matematika');
    expect(table.text()).toContain('MTK-7');
    expect(table.text()).toContain('86');
    expect(table.text()).toContain('Bahasa Inggris');
    expect(table.text()).toContain('90');
    expect(table.text()).toContain('Active in group conversations');
  });

  it('displays attendance tallies and homeroom remarks', () => {
    const wrapper = createWrapper();
    const attendanceTable = wrapper.find('[data-testid="print-attendance-table"]');
    expect(attendanceTable.exists()).toBe(true);
    expect(attendanceTable.text()).toContain('2 hari'); // Excused
    expect(attendanceTable.text()).toContain('1 hari'); // Late/Izin
    expect(attendanceTable.text()).toContain('0 hari'); // Absent

    const remarks = wrapper.find('[data-testid="print-homeroom-remarks"]');
    expect(remarks.text()).toContain('Siswa menunjukkan integritas tinggi');
  });

  it('triggers window.print and emits print event when print button is clicked', async () => {
    const wrapper = createWrapper();
    const printBtn = wrapper.find('[data-testid="btn-execute-print"]');
    expect(printBtn.exists()).toBe(true);

    await printBtn.trigger('click');

    expect(wrapper.emitted('print')).toBeTruthy();
    expect(window.print).toHaveBeenCalled();
  });
});
