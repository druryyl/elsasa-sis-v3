// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  parseReportCardSnapshot,
  formatAttendanceSummary,
  formatScoreDisplay,
  formatGradeDisplay,
  calculateSnapshotAverage,
  sortSubjectEntries,
} from '../utils/snapshotFormatters';
import type {
  ReportCardSnapshot,
  ReportCardSubjectEntrySnapshot,
} from '@/features/academics/assessment/types';

describe('snapshotFormatters.spec.ts - Snapshot Data Formatters & Parsers (P10-S25, GAP-006, TD-05)', () => {
  const sampleSnapshot: ReportCardSnapshot = {
    reportCardId: '11111111-1111-1111-1111-111111111111',
    studentId: '22222222-2222-2222-2222-222222222222',
    studentName: 'Ahmad Faiz',
    studentNisn: '0012345678',
    academicPeriodId: '33333333-3333-3333-3333-333333333333',
    periodLabel: 'Semester 1 2026/2027',
    reportType: 'end_of_term',
    templateName: 'Buku Rapor Merdeka',
    status: 'published',
    approvedBy: 'coordinator@elsasa.sch.id',
    approvedDate: '2026-11-05T08:00:00Z',
    narrativeComments: 'Siswa menunjukkan ketekunan dan kepribadian yang sangat terpuji.',
    attendanceSummary: {
      presentDays: 95,
      absentDays: 2,
      lateDays: 3,
      excusedDays: 0,
    },
    subjectEntries: [
      {
        learningUnitId: 'u-1',
        subjectCode: 'MTK-101',
        subjectTitle: 'Matematika Dasar',
        scoreNumeric: 88,
        gradeLetter: 'A',
        gradeLabel: 'Sangat Kompeten',
        teacherComment: 'Pemahaman konsep aljabar sangat mendalam.',
        displayOrder: 2,
      },
      {
        learningUnitId: 'u-2',
        subjectCode: 'BIN-101',
        subjectTitle: 'Bahasa Indonesia',
        scoreNumeric: 92,
        gradeLetter: 'A',
        gradeLabel: 'Sangat Kompeten',
        teacherComment: 'Keterampilan menulis narasi sangat rapi.',
        displayOrder: 1,
      },
      {
        learningUnitId: 'u-3',
        subjectCode: 'IPA-101',
        subjectTitle: 'Ilmu Pengetahuan Alam',
        scoreNumeric: 78,
        gradeLetter: 'B',
        gradeLabel: 'Kompeten',
        teacherComment: 'Aktif dalam percobaan laboratorium.',
        displayOrder: 3,
      },
    ],
  };

  describe('parseReportCardSnapshot', () => {
    it('successfully parses valid JSON report card snapshot string', () => {
      const jsonStr = JSON.stringify(sampleSnapshot);
      const parsed = parseReportCardSnapshot(jsonStr);

      expect(parsed).not.toBeNull();
      expect(parsed?.reportCardId).toBe('11111111-1111-1111-1111-111111111111');
      expect(parsed?.studentName).toBe('Ahmad Faiz');
      expect(parsed?.subjectEntries.length).toBe(3);
    });

    it('returns null for null, undefined, or empty string', () => {
      expect(parseReportCardSnapshot(null)).toBeNull();
      expect(parseReportCardSnapshot(undefined)).toBeNull();
      expect(parseReportCardSnapshot('')).toBeNull();
      expect(parseReportCardSnapshot('   ')).toBeNull();
    });

    it('returns null for invalid JSON or malformed contracts', () => {
      expect(parseReportCardSnapshot('{not-valid-json}')).toBeNull();
      expect(parseReportCardSnapshot('12345')).toBeNull();
      expect(parseReportCardSnapshot('{"randomField": true}')).toBeNull(); // Missing required fields
    });
  });

  describe('formatAttendanceSummary', () => {
    it('calculates total days and attendance percentage correctly', () => {
      const formatted = formatAttendanceSummary(sampleSnapshot.attendanceSummary);

      expect(formatted.presentDays).toBe(95);
      expect(formatted.absentDays).toBe(2);
      expect(formatted.lateDays).toBe(3);
      expect(formatted.excusedDays).toBe(0);
      expect(formatted.totalDays).toBe(100);
      expect(formatted.attendanceRateFormatted).toBe('95.0%');
    });

    it('handles null or missing summary gracefully', () => {
      const empty = formatAttendanceSummary(null);
      expect(empty.totalDays).toBe(0);
      expect(empty.attendanceRateFormatted).toBe('0.0%');
    });
  });

  describe('formatScoreDisplay', () => {
    it('formats numeric scores with max score', () => {
      expect(formatScoreDisplay(88, 100)).toBe('88 / 100');
      expect(formatScoreDisplay(87.5, 100)).toBe('87.5 / 100');
      expect(formatScoreDisplay(75, null)).toBe('75');
    });

    it('returns hyphen for null or undefined score', () => {
      expect(formatScoreDisplay(null, 100)).toBe('-');
      expect(formatScoreDisplay(undefined, 100)).toBe('-');
    });
  });

  describe('formatGradeDisplay', () => {
    it('formats combined grade letter and competency label', () => {
      expect(formatGradeDisplay('A', 'Sangat Kompeten')).toBe('A (Sangat Kompeten)');
      expect(formatGradeDisplay('B', null)).toBe('B');
      expect(formatGradeDisplay(null, 'Kompeten')).toBe('(Kompeten)');
      expect(formatGradeDisplay(null, null)).toBe('-');
    });
  });

  describe('calculateSnapshotAverage', () => {
    it('calculates rounded arithmetic average across valid numeric entries', () => {
      // 88 + 92 + 78 = 258 / 3 = 86.0
      const avg = calculateSnapshotAverage(sampleSnapshot.subjectEntries);
      expect(avg).toBe(86);
    });

    it('ignores entries without numeric score (e.g. qualitative only)', () => {
      const mixed: ReportCardSubjectEntrySnapshot[] = [
        {
          learningUnitId: 'u-1',
          subjectCode: 'MTK-101',
          subjectTitle: 'Math',
          scoreNumeric: 80,
          displayOrder: 1,
        },
        {
          learningUnitId: 'u-2',
          subjectCode: 'AGM-101',
          subjectTitle: 'Agama',
          gradeLabel: 'Sangat Baik',
          scoreNumeric: null,
          displayOrder: 2,
        },
      ];
      expect(calculateSnapshotAverage(mixed)).toBe(80);
    });

    it('returns null for empty entries', () => {
      expect(calculateSnapshotAverage([])).toBeNull();
      expect(calculateSnapshotAverage(null)).toBeNull();
    });
  });

  describe('sortSubjectEntries', () => {
    it('sorts subjects deterministically by displayOrder then title', () => {
      const sorted = sortSubjectEntries(sampleSnapshot.subjectEntries);
      expect(sorted[0].subjectCode).toBe('BIN-101'); // displayOrder: 1
      expect(sorted[1].subjectCode).toBe('MTK-101'); // displayOrder: 2
      expect(sorted[2].subjectCode).toBe('IPA-101'); // displayOrder: 3
    });

    it('returns empty array when input is null or undefined', () => {
      expect(sortSubjectEntries(null)).toEqual([]);
      expect(sortSubjectEntries(undefined)).toEqual([]);
    });
  });
});
