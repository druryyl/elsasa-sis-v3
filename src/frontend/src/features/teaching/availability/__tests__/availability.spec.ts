// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import {
  getTeacherAvailability,
  declareTeacherAvailability,
  getTeacherUnavailabilityWindows,
  getUnavailabilityWindows,
  createUnavailabilityWindow,
  cancelUnavailabilityWindow,
  availabilityApi,
} from '../api/availabilityApi';
import {
  declareAvailabilitySchema,
  createUnavailabilityWindowSchema,
} from '../api/schemas';
import { availabilityKeys } from '../api/queryKeys';

describe('Teaching Availability API Client & Schemas (P7-S23)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const teacherId = '11111111-1111-1111-1111-111111111111';
  const periodId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const windowId = '22222222-2222-2222-2222-222222222222';

  describe('availabilityApi Client Functions', () => {
    it('getTeacherAvailability invokes GET /v1/teaching/teachers/{teacherId}/availability with period param', async () => {
      const mockResult = {
        id: '33333333-3333-3333-3333-333333333333',
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        maxAvailableHours: 24,
        campusScope: 'All',
        availableDeliveryModes: 'in_person',
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        status: 'active',
        createdAt: '2026-07-01T00:00:00Z',
      };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResult as never);

      const res = await getTeacherAvailability(teacherId, periodId);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/availability`,
        { params: { academicPeriodId: periodId } },
      );
      expect(res).toEqual(mockResult);
    });

    it('declareTeacherAvailability invokes POST /v1/teaching/teachers/{teacherId}/availability', async () => {
      const payload = {
        academicPeriodId: periodId,
        maxAvailableHours: 20,
        effectiveStartDate: '2026-07-01',
        effectiveEndDate: '2026-12-31',
        campusScope: 'Main',
        availableDeliveryModes: 'All',
      };
      const mockResult = {
        id: '44444444-4444-4444-4444-444444444444',
        teacherProfileId: teacherId,
        academicPeriodId: periodId,
        maxAvailableHours: 20,
        status: 'active',
        supersededPreviousCount: 1,
        createdAt: '2026-07-01T00:00:00Z',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResult as never);

      const res = await declareTeacherAvailability(teacherId, payload);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/availability`,
        payload,
      );
      expect(res).toEqual(mockResult);
    });

    it('getTeacherUnavailabilityWindows invokes GET /v1/teaching/teachers/{teacherId}/unavailability with params', async () => {
      const mockWindows = [
        {
          id: windowId,
          teacherProfileId: teacherId,
          startDate: '2026-09-01',
          endDate: '2026-09-05',
          unavailabilityType: 'medical_leave',
          reason: 'Medical surgery and recovery',
          status: 'active',
          createdAt: '2026-08-30T00:00:00Z',
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockWindows as never);

      const params = { status: 'active', fromDate: '2026-09-01', toDate: '2026-09-30' };
      const res = await getTeacherUnavailabilityWindows(teacherId, params);

      expect(getSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
        { params },
      );
      expect(res).toEqual(mockWindows);
    });

    it('getUnavailabilityWindows alias behaves identically', async () => {
      expect(getUnavailabilityWindows).toBe(getTeacherUnavailabilityWindows);
    });

    it('createUnavailabilityWindow invokes POST /v1/teaching/teachers/{teacherId}/unavailability', async () => {
      const payload = {
        startDate: '2026-10-01',
        endDate: '2026-10-03',
        unavailabilityType: 'official_training',
        reason: 'National teacher conference',
        emergencyCoverageRequired: false,
      };
      const mockResult = {
        id: windowId,
        teacherProfileId: teacherId,
        startDate: '2026-10-01',
        endDate: '2026-10-03',
        unavailabilityType: 'official_training',
        status: 'active',
        affectedActiveAssignmentCount: 0,
        createdAt: '2026-09-25T00:00:00Z',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResult as never);

      const res = await createUnavailabilityWindow(teacherId, payload);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/teaching/teachers/${encodeURIComponent(teacherId)}/unavailability`,
        payload,
      );
      expect(res).toEqual(mockResult);
    });

    it('cancelUnavailabilityWindow invokes POST /v1/teaching/unavailability/{id}/cancel', async () => {
      const mockResult = {
        id: windowId,
        teacherProfileId: teacherId,
        status: 'cancelled',
        cancelledAt: '2026-10-01T12:00:00Z',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResult as never);

      const res = await cancelUnavailabilityWindow(windowId);

      expect(postSpy).toHaveBeenCalledWith(
        `/v1/teaching/unavailability/${encodeURIComponent(windowId)}/cancel`,
      );
      expect(res).toEqual(mockResult);
    });

    it('availabilityApi object aggregates all functions', () => {
      expect(availabilityApi.getTeacherAvailability).toBe(getTeacherAvailability);
      expect(availabilityApi.declareTeacherAvailability).toBe(declareTeacherAvailability);
      expect(availabilityApi.getTeacherUnavailabilityWindows).toBe(getTeacherUnavailabilityWindows);
      expect(availabilityApi.getUnavailabilityWindows).toBe(getUnavailabilityWindows);
      expect(availabilityApi.createUnavailabilityWindow).toBe(createUnavailabilityWindow);
      expect(availabilityApi.cancelUnavailabilityWindow).toBe(cancelUnavailabilityWindow);
    });
  });

  describe('Zod Validation Schemas', () => {
    describe('declareAvailabilitySchema', () => {
      it('validates valid availability declaration data', () => {
        const validData = {
          academicPeriodId: '11111111-1111-1111-1111-111111111111',
          maxAvailableHours: 24,
          effectiveStartDate: '2026-07-01',
          effectiveEndDate: '2026-12-31',
          campusScope: 'Main Campus',
          availableDeliveryModes: 'in_person',
        };

        const result = declareAvailabilitySchema.safeParse(validData);
        expect(result.success).toBe(true);
      });

      it('fails when maxAvailableHours is <= 0 or > 168', () => {
        const invalidDataZero = {
          academicPeriodId: '11111111-1111-1111-1111-111111111111',
          maxAvailableHours: 0,
          effectiveStartDate: '2026-07-01',
          effectiveEndDate: '2026-12-31',
        };
        const resZero = declareAvailabilitySchema.safeParse(invalidDataZero);
        expect(resZero.success).toBe(false);

        const invalidDataExceeded = {
          academicPeriodId: '11111111-1111-1111-1111-111111111111',
          maxAvailableHours: 169,
          effectiveStartDate: '2026-07-01',
          effectiveEndDate: '2026-12-31',
        };
        const resExceeded = declareAvailabilitySchema.safeParse(invalidDataExceeded);
        expect(resExceeded.success).toBe(false);
      });

      it('fails when effectiveEndDate is before effectiveStartDate', () => {
        const invalidRange = {
          academicPeriodId: '11111111-1111-1111-1111-111111111111',
          maxAvailableHours: 20,
          effectiveStartDate: '2026-12-31',
          effectiveEndDate: '2026-07-01',
        };
        const result = declareAvailabilitySchema.safeParse(invalidRange);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toContain('Effective end date must be on or after effective start date.');
        }
      });
    });

    describe('createUnavailabilityWindowSchema', () => {
      it('validates valid unavailability window data', () => {
        const validData = {
          startDate: '2026-08-01',
          endDate: '2026-08-10',
          unavailabilityType: 'medical_leave',
          reason: 'Hospitalization and bed rest',
          startTime: '08:00',
          endTime: '17:00',
          emergencyCoverageRequired: true,
        };

        const result = createUnavailabilityWindowSchema.safeParse(validData);
        expect(result.success).toBe(true);
      });

      it('fails on invalid unavailabilityType', () => {
        const invalidType = {
          startDate: '2026-08-01',
          endDate: '2026-08-10',
          unavailabilityType: 'invalid_type',
          reason: 'Test reason',
        };

        const result = createUnavailabilityWindowSchema.safeParse(invalidType);
        expect(result.success).toBe(false);
      });

      it('fails when endDate is before startDate', () => {
        const invalidRange = {
          startDate: '2026-08-10',
          endDate: '2026-08-01',
          unavailabilityType: 'personal_leave',
          reason: 'Vacation',
        };

        const result = createUnavailabilityWindowSchema.safeParse(invalidRange);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toContain('End date must be on or after start date.');
        }
      });

      it('fails when endTime is before startTime', () => {
        const invalidTimes = {
          startDate: '2026-08-01',
          endDate: '2026-08-01',
          unavailabilityType: 'blackout',
          reason: 'School maintenance',
          startTime: '14:00',
          endTime: '10:00',
        };

        const result = createUnavailabilityWindowSchema.safeParse(invalidTimes);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toContain('End time must be on or after start time.');
        }
      });
    });
  });

  describe('Query Keys', () => {
    it('produces expected key hierarchy', () => {
      expect(availabilityKeys.all).toEqual(['teaching', 'availability']);
      expect(availabilityKeys.teacher(teacherId)).toEqual(['teaching', 'availability', 'teacher', teacherId]);
      expect(availabilityKeys.teacherAvailability(teacherId, periodId)).toEqual([
        'teaching',
        'availability',
        'teacher',
        teacherId,
        'declared',
        periodId,
      ]);
      expect(availabilityKeys.windows(teacherId)).toEqual([
        'teaching',
        'availability',
        'teacher',
        teacherId,
        'windows',
      ]);
      expect(availabilityKeys.window(windowId)).toEqual([
        'teaching',
        'availability',
        'window',
        windowId,
      ]);
    });
  });
});
