import { describe, it, expect } from 'vitest';
import { queryKeys, studentRecordsKeys } from '../queryKeys';

describe('studentRecordsKeys & queryKeys (TanStack Query Key Factories)', () => {
  it('generates the root key', () => {
    expect(studentRecordsKeys.all).toEqual(['students', 'records']);
    expect(queryKeys.students.all).toEqual(['students', 'records']);
  });

  it('generates lists base key', () => {
    expect(studentRecordsKeys.lists()).toEqual(['students', 'records', 'list']);
    expect(queryKeys.students.lists()).toEqual(['students', 'records', 'list']);
  });

  it('generates list key without params', () => {
    expect(studentRecordsKeys.list()).toEqual(['students', 'records', 'list']);
    expect(queryKeys.students.list()).toEqual(['students', 'records', 'list']);
  });

  it('generates list key with search params', () => {
    const params = { searchTerm: 'Ahmad', campus: 'Main Campus', pageNumber: 1, pageSize: 20 };
    expect(studentRecordsKeys.list(params)).toEqual(['students', 'records', 'list', params]);
    expect(queryKeys.students.list(params)).toEqual(['students', 'records', 'list', params]);
  });

  it('generates details base key', () => {
    expect(studentRecordsKeys.details()).toEqual(['students', 'records', 'detail']);
    expect(queryKeys.students.details()).toEqual(['students', 'records', 'detail']);
  });

  it('generates detail key with student id', () => {
    const studentId = 'stu-123';
    expect(studentRecordsKeys.detail(studentId)).toEqual(['students', 'records', 'detail', 'stu-123']);
    expect(queryKeys.students.detail(studentId)).toEqual(['students', 'records', 'detail', 'stu-123']);
  });

  it('generates history key nested under detail key', () => {
    const studentId = 'stu-123';
    expect(queryKeys.students.history(studentId)).toEqual([
      ...queryKeys.students.detail(studentId),
      'history',
    ]);
    expect(queryKeys.students.history(studentId)).toEqual([
      'students',
      'records',
      'detail',
      'stu-123',
      'history',
    ]);
    expect(studentRecordsKeys.history(studentId)).toEqual([
      'students',
      'records',
      'detail',
      'stu-123',
      'history',
    ]);
  });
});
