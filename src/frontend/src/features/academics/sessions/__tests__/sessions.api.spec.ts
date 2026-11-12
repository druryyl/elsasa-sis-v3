// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import * as sessionsApi from '../api/sessions.api';
import type {
  CancelActivityRequest,
  ConductActivityRequest,
  CorrectAttendanceRequest,
  PlanActivityRequest,
  RecordAttendanceRequest,
  UnitActivitiesFilter,
} from '../types';

describe('Sessions & Attendance API Client (P9-S24)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const activityId = '11111111-1111-1111-1111-111111111111';
  const learningUnitId = '22222222-2222-2222-2222-222222222222';
  const studentId = '33333333-3333-3333-3333-333333333333';
  const attendanceRecordId = '44444444-4444-4444-4444-444444444444';
  const periodId = '55555555-5555-5555-5555-555555555555';

  describe('getAvailableLearningUnits', () => {
    it('issues GET /v1/academics/learning-units/available with trimmed search param', async () => {
      const mockResponse = [
        {
          id: learningUnitId,
          learningStructureId: '66666666-6666-6666-6666-666666666666',
          code: 'MATH-101',
          name: 'Algebra I',
          unitType: 'course',
          level: 'Grade 10',
          status: 'active',
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getAvailableLearningUnits('  MATH  ');

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/learning-units/available', {
        params: { search: 'MATH' },
      });
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/academics/learning-units/available without params when search is omitted', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce([] as never);

      const result = await sessionsApi.getAvailableLearningUnits();

      expect(getSpy).toHaveBeenCalledWith('/v1/academics/learning-units/available', {
        params: {},
      });
      expect(result).toEqual([]);
    });

    it('returns an empty array when the response payload is not an array', async () => {
      vi.spyOn(apiClient, 'get').mockResolvedValueOnce({ items: [] } as never);

      const result = await sessionsApi.getAvailableLearningUnits();

      expect(result).toEqual([]);
    });
  });

  describe('planLearningActivity', () => {
    it('issues POST /v1/academics/learning-activities with request payload', async () => {
      const mockRequest: PlanActivityRequest = {
        learningUnitId,
        activityType: 'lesson',
        title: 'Photosynthesis Lecture',
        description: 'Plant biology chapter 3',
        sectionLabel: '10-IPA-1',
        activityDate: '2026-08-20',
        startTime: '08:00',
        endTime: '09:30',
        teacherName: 'Dr. Maria Curie',
        isAttendanceRequired: true,
      };
      const mockResponse = { id: activityId, title: 'Photosynthesis Lecture', status: 'planned' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.planLearningActivity(mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith('/v1/academics/learning-activities', mockRequest);
      expect(result).toEqual(mockResponse);
    });
  });

  describe('conductLearningActivity', () => {
    it('issues POST /v1/academics/learning-activities/{id}/conduct with delivery notes', async () => {
      const mockRequest: ConductActivityRequest = {
        deliveryNotes: 'Covered all scheduled milestones and practical lab demonstration.',
        actualTeacherName: 'Dr. Maria Curie',
      };
      const mockResponse = { id: activityId, status: 'conducted', conductedAt: '2026-08-20T10:00:00Z' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.conductLearningActivity(activityId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/conduct`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('cancelLearningActivity', () => {
    it('issues POST /v1/academics/learning-activities/{id}/cancel with cancellation reason', async () => {
      const mockRequest: CancelActivityRequest = {
        cancellationReason: 'Campus closed due to severe weather advisory.',
      };
      const mockResponse = { id: activityId, status: 'cancelled' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.cancelLearningActivity(activityId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/cancel`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getActivitiesByUnit', () => {
    it('issues GET /v1/academics/learning-units/{id}/activities with empty params when filter is omitted', async () => {
      const mockResponse = [{ id: activityId, title: 'Lecture 1' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getActivitiesByUnit(learningUnitId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/activities`,
        { params: {} },
      );
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/academics/learning-units/{id}/activities with complete filter params', async () => {
      const mockResponse = [{ id: activityId, title: 'Lecture 1' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);
      const filter: UnitActivitiesFilter = {
        fromDate: '2026-08-01',
        toDate: '2026-08-31',
        sectionLabel: '10-IPA-1',
        status: 'planned',
      };

      const result = await sessionsApi.getActivitiesByUnit(learningUnitId, filter);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/activities`,
        {
          params: {
            fromDate: '2026-08-01',
            toDate: '2026-08-31',
            sectionLabel: '10-IPA-1',
            status: 'planned',
          },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getActivityById', () => {
    it('issues GET /v1/academics/learning-activities/{id}', async () => {
      const mockResponse = { id: activityId, title: 'Session Details', totalAttendees: 28 };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getActivityById(activityId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getTeacherDailyActivities', () => {
    it('issues GET /v1/academics/teacher/activities without params when date is omitted', async () => {
      const mockResponse = [{ id: activityId, title: 'Today Class' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getTeacherDailyActivities();

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/teacher/activities', { params: {} });
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/academics/teacher/activities with date parameter', async () => {
      const mockResponse = [{ id: activityId, title: 'Class on Date' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getTeacherDailyActivities('2026-08-25');

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/academics/teacher/activities', {
        params: { date: '2026-08-25' },
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe('recordSessionAttendance', () => {
    it('issues POST /v1/academics/learning-activities/{id}/attendance with roll call entries', async () => {
      const mockRequest: RecordAttendanceRequest = {
        entries: [
          {
            studentId,
            status: 'present',
            evidence: 'Present in class',
          },
        ],
      };
      const mockResponse = { recordedCount: 1, activityId };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.recordSessionAttendance(activityId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/attendance`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('correctAttendance', () => {
    it('issues POST /v1/academics/attendance-records/{id}/correct with mandatory reason', async () => {
      const mockRequest: CorrectAttendanceRequest = {
        newStatus: 'excused_absence',
        reason: 'Medical certificate submitted retroactively from hospital',
        evidence: 'MED-CERT-2026.pdf',
      };
      const mockResponse = {
        attendanceRecordId,
        newStatus: 'excused_absence',
        correctionId: 'corr-123',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.correctAttendance(attendanceRecordId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/attendance-records/${encodeURIComponent(attendanceRecordId)}/correct`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getActivityAttendance', () => {
    it('issues GET /v1/academics/learning-activities/{id}/attendance', async () => {
      const mockResponse = [{ id: attendanceRecordId, studentId, status: 'present' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getActivityAttendance(activityId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/attendance`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getStudentAttendanceSummary', () => {
    it('issues GET /v1/academics/students/{id}/attendance-summary without period when omitted', async () => {
      const mockResponse = { studentId, presentCount: 18, absentCount: 1, attendanceRate: 94.7 };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getStudentAttendanceSummary(studentId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/attendance-summary`,
        { params: {} },
      );
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/academics/students/{id}/attendance-summary with academicPeriodId param', async () => {
      const mockResponse = { studentId, presentCount: 18, absentCount: 1, attendanceRate: 94.7 };
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await sessionsApi.getStudentAttendanceSummary(studentId, periodId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/students/${encodeURIComponent(studentId)}/attendance-summary`,
        { params: { academicPeriodId: periodId } },
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
