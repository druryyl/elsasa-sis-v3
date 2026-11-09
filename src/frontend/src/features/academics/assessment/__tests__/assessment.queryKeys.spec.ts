// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  assessmentKeys,
  evidenceKeys,
  resultKeys,
} from '../constants/queryKeys';

describe('assessment.queryKeys.spec.ts - Assessment Domain Query Keys (P10-S25, P8-S20)', () => {
  const assessmentId = '11111111-1111-1111-1111-111111111111';
  const unitId = '22222222-2222-2222-2222-222222222222';
  const studentId = '33333333-3333-3333-3333-333333333333';
  const periodId = '44444444-4444-4444-4444-444444444444';

  describe('assessmentKeys', () => {
    it('generates consistent hierarchical query keys', () => {
      expect(assessmentKeys.all).toEqual(['academics', 'assessments']);
      expect(assessmentKeys.lists()).toEqual(['academics', 'assessments', 'list']);
      expect(assessmentKeys.list(unitId, { status: 'planned' })).toEqual([
        'academics',
        'assessments',
        'list',
        unitId,
        { status: 'planned' },
      ]);
      expect(assessmentKeys.details()).toEqual(['academics', 'assessments', 'detail']);
      expect(assessmentKeys.detail(assessmentId)).toEqual(['academics', 'assessments', 'detail', assessmentId]);
    });
  });

  describe('evidenceKeys', () => {
    it('generates evidence query keys scoped to assessment', () => {
      expect(evidenceKeys.all).toEqual(['academics', 'evidence']);
      expect(evidenceKeys.lists()).toEqual(['academics', 'evidence', 'list']);
      expect(evidenceKeys.byAssessment(assessmentId, { studentId })).toEqual([
        'academics',
        'evidence',
        'list',
        assessmentId,
        { studentId },
      ]);
    });
  });

  describe('resultKeys', () => {
    it('generates academic results query keys scoped to assessment and student portal', () => {
      expect(resultKeys.all).toEqual(['academics', 'results']);
      expect(resultKeys.lists()).toEqual(['academics', 'results', 'list']);
      expect(resultKeys.byAssessment(assessmentId, { status: 'approved' })).toEqual([
        'academics',
        'results',
        'list',
        assessmentId,
        { status: 'approved' },
      ]);
      expect(resultKeys.studentResults(periodId, studentId)).toEqual([
        'academics',
        'results',
        'student',
        periodId,
        studentId,
      ]);
    });
  });
});
