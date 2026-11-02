import { describe, it, expect } from 'vitest';
import {
  academicYearKeys,
  operationalPeriodKeys,
  calendarKeys,
  schoolClosureKeys,
  amendmentKeys,
  recordCorrectionKeys,
} from '../queryKeys';

describe('Query Key Factories (queryKeys.ts)', () => {
  describe('academicYearKeys', () => {
    it('produces root key', () => {
      expect(academicYearKeys.all).toEqual(['academicYears']);
    });

    it('produces list keys with and without parameters', () => {
      expect(academicYearKeys.lists()).toEqual(['academicYears', 'list']);
      expect(academicYearKeys.list()).toEqual(['academicYears', 'list']);
      expect(academicYearKeys.list({ status: 'active' })).toEqual([
        'academicYears',
        'list',
        { status: 'active' },
      ]);
    });

    it('produces detail key', () => {
      expect(academicYearKeys.details()).toEqual(['academicYears', 'detail']);
      expect(academicYearKeys.detail('ay-123')).toEqual(['academicYears', 'detail', 'ay-123']);
    });
  });

  describe('operationalPeriodKeys', () => {
    it('produces root key', () => {
      expect(operationalPeriodKeys.all).toEqual(['operationalPeriods']);
    });

    it('produces academicYear-scoped period key', () => {
      expect(operationalPeriodKeys.byAcademicYear('ay-123')).toEqual([
        'operationalPeriods',
        'ay-123',
      ]);
    });
  });

  describe('calendarKeys', () => {
    it('produces root calendars key', () => {
      expect(calendarKeys.all).toEqual(['calendars']);
    });

    it('produces academicYear-scoped calendars and active calendar key', () => {
      expect(calendarKeys.byAcademicYear('ay-123')).toEqual(['calendars', 'ay-123']);
      expect(calendarKeys.active('ay-123')).toEqual(['calendars', 'ay-123', 'active']);
    });

    it('produces matrix projection keys', () => {
      expect(calendarKeys.matrixAll).toEqual(['calendarMatrix']);
      expect(calendarKeys.matrix('cal-1')).toEqual(['calendarMatrix', 'cal-1']);
      expect(
        calendarKeys.matrix('cal-1', {
          startDate: '2026-07-01',
          endDate: '2026-07-31',
        }),
      ).toEqual([
        'calendarMatrix',
        'cal-1',
        { startDate: '2026-07-01', endDate: '2026-07-31' },
      ]);
    });
  });

  describe('schoolClosureKeys', () => {
    it('produces root and academicYear-scoped keys', () => {
      expect(schoolClosureKeys.all).toEqual(['schoolClosures']);
      expect(schoolClosureKeys.byAcademicYear('ay-123')).toEqual(['schoolClosures', 'ay-123']);
    });
  });

  describe('amendmentKeys & recordCorrectionKeys', () => {
    it('produces calendar-scoped amendment keys', () => {
      expect(amendmentKeys.all).toEqual(['calendarAmendments']);
      expect(amendmentKeys.byCalendar('cal-1')).toEqual(['calendarAmendments', 'cal-1']);
    });

    it('produces academicYear-scoped correction keys', () => {
      expect(recordCorrectionKeys.all).toEqual(['recordCorrections']);
      expect(recordCorrectionKeys.byAcademicYear('ay-123')).toEqual([
        'recordCorrections',
        'ay-123',
      ]);
    });
  });
});
