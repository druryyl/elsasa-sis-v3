import { describe, it, expect } from 'vitest';
import { admissionKeys } from '../queryKeys';

describe('admissionKeys (TanStack Query Key Hierarchy)', () => {
  it('generates the root key', () => {
    expect(admissionKeys.all).toEqual(['admission']);
  });

  it('generates lists base key', () => {
    expect(admissionKeys.lists()).toEqual(['admission', 'list']);
  });

  it('generates list key without filters', () => {
    expect(admissionKeys.list()).toEqual(['admission', 'list']);
  });

  it('generates list key with filter parameters', () => {
    const filters = {
      admissionPeriodId: '11111111-1111-1111-1111-111111111111',
      status: 'under_review' as const,
      searchTerm: 'Budi',
      page: 1,
      pageSize: 20,
    };
    expect(admissionKeys.list(filters)).toEqual(['admission', 'list', filters]);
  });

  it('generates details base key', () => {
    expect(admissionKeys.details()).toEqual(['admission', 'detail']);
  });

  it('generates detail key by id', () => {
    const id = '22222222-2222-2222-2222-222222222222';
    expect(admissionKeys.detail(id)).toEqual(['admission', 'detail', id]);
  });

  it('generates status key with separate number and dob arguments', () => {
    expect(admissionKeys.status('ADM-2026-0001', '2015-05-20')).toEqual([
      'admission',
      'status',
      'ADM-2026-0001',
      '2015-05-20',
    ]);
  });

  it('generates status key with parameter object', () => {
    expect(
      admissionKeys.status({
        applicationNumber: 'ADM-2026-0002',
        dateOfBirth: '2016-08-14',
      }),
    ).toEqual(['admission', 'status', 'ADM-2026-0002', '2016-08-14']);
  });

  it('generates periods and activePeriods keys', () => {
    expect(admissionKeys.periods()).toEqual(['admission', 'periods']);
    expect(admissionKeys.activePeriods()).toEqual(['admission', 'periods', 'active']);
  });
});
