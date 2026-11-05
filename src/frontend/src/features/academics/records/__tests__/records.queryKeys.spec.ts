// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  assessmentKeys,
  evidenceKeys,
  resultKeys,
  reportCardKeys,
  transcriptKeys,
  correctionKeys,
} from '../constants/queryKeys';

describe('Query Key Factories (Academics Assessment & Records)', () => {
  describe('assessmentKeys', () => {
    it('produces expected key hierarchies', () => {
      expect(assessmentKeys.all).toEqual(['academics', 'assessments']);
      expect(assessmentKeys.lists()).toEqual(['academics', 'assessments', 'list']);
      expect(assessmentKeys.list('unit-1', { status: 'planned' })).toEqual([
        'academics',
        'assessments',
        'list',
        'unit-1',
        { status: 'planned' },
      ]);
      expect(assessmentKeys.detail('asm-1')).toEqual([
        'academics',
        'assessments',
        'detail',
        'asm-1',
      ]);
    });
  });

  describe('evidenceKeys', () => {
    it('produces expected key hierarchies', () => {
      expect(evidenceKeys.all).toEqual(['academics', 'evidence']);
      expect(evidenceKeys.byAssessment('asm-1', { studentId: 'stu-1' })).toEqual([
        'academics',
        'evidence',
        'list',
        'asm-1',
        { studentId: 'stu-1' },
      ]);
    });
  });

  describe('resultKeys', () => {
    it('produces expected key hierarchies', () => {
      expect(resultKeys.all).toEqual(['academics', 'results']);
      expect(resultKeys.byAssessment('asm-1', { onlyFinal: true })).toEqual([
        'academics',
        'results',
        'list',
        'asm-1',
        { onlyFinal: true },
      ]);
      expect(resultKeys.studentResults('period-1', 'stu-1')).toEqual([
        'academics',
        'results',
        'student',
        'period-1',
        'stu-1',
      ]);
    });
  });

  describe('reportCardKeys', () => {
    it('produces expected key hierarchies', () => {
      expect(reportCardKeys.all).toEqual(['academics', 'report-cards']);
      expect(reportCardKeys.list({ academicPeriodId: 'per-1' })).toEqual([
        'academics',
        'report-cards',
        'list',
        { academicPeriodId: 'per-1' },
      ]);
      expect(reportCardKeys.byPeriod('per-1', 'stu-1')).toEqual([
        'academics',
        'report-cards',
        'list',
        { periodId: 'per-1', studentId: 'stu-1' },
      ]);
      expect(reportCardKeys.detail('rc-1')).toEqual([
        'academics',
        'report-cards',
        'detail',
        'rc-1',
      ]);
    });
  });

  describe('transcriptKeys', () => {
    it('produces expected key hierarchies', () => {
      expect(transcriptKeys.all).toEqual(['academics', 'transcripts']);
      expect(transcriptKeys.byStudent('stu-1', 'official')).toEqual([
        'academics',
        'transcripts',
        'list',
        'student',
        'stu-1',
        'official',
      ]);
      expect(transcriptKeys.detail('tr-1')).toEqual([
        'academics',
        'transcripts',
        'detail',
        'tr-1',
      ]);
    });
  });

  describe('correctionKeys', () => {
    it('produces expected key hierarchies', () => {
      expect(correctionKeys.all).toEqual(['academics', 'corrections']);
      expect(correctionKeys.history('res-1')).toEqual([
        'academics',
        'corrections',
        'history',
        'res-1',
      ]);
      expect(correctionKeys.detail('res-1')).toEqual([
        'academics',
        'corrections',
        'history',
        'res-1',
      ]);
    });
  });
});
