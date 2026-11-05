// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import * as courseworkApi from '../api/coursework.api';
import type {
  CreateAssignmentRequest,
  RecordExceptionRequest,
} from '../types';

describe('Coursework API Client (P9-S24)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const activityId = '11111111-1111-1111-1111-111111111111';
  const assignmentId = '22222222-2222-2222-2222-222222222222';
  const unitId = '33333333-3333-3333-3333-333333333333';
  const studentId = '44444444-4444-4444-4444-444444444444';

  describe('createAssignment', () => {
    it('issues POST /v1/academics/learning-activities/{activityId}/assignments with payload', async () => {
      const mockRequest: CreateAssignmentRequest = {
        title: 'Laboratory Experiment Report',
        description: 'Complete synthesis analysis.',
        releaseDate: '2026-09-01',
        dueDate: '2026-09-08',
        notes: 'Max score 100',
      };
      const mockResponse = { id: assignmentId, status: 'draft', title: 'Laboratory Experiment Report' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.createAssignment(activityId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/assignments`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('releaseAssignment', () => {
    it('issues POST /v1/academics/assignments/{id}/release with empty body', async () => {
      const mockResponse = { id: assignmentId, status: 'released' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.releaseAssignment(assignmentId);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/release`,
        {},
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('closeAssignment', () => {
    it('issues POST /v1/academics/assignments/{id}/close with empty body', async () => {
      const mockResponse = { id: assignmentId, status: 'closed' };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.closeAssignment(assignmentId);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/close`,
        {},
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getAssignmentsByActivity', () => {
    it('issues GET /v1/academics/learning-activities/{activityId}/assignments', async () => {
      const mockResponse = [{ id: assignmentId, title: 'In-Class Task' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.getAssignmentsByActivity(activityId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/assignments`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getAssignmentsByUnit', () => {
    it('issues GET /v1/academics/learning-units/{unitId}/assignments', async () => {
      const mockResponse = [{ id: assignmentId, title: 'Midterm Project' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.getAssignmentsByUnit(unitId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/learning-units/${encodeURIComponent(unitId)}/assignments`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getAssignmentSubmissions', () => {
    it('issues GET /v1/academics/assignments/{assignmentId}/submissions', async () => {
      const mockResponse = [
        {
          id: 'sub-1',
          assignmentId,
          studentId,
          status: 'submitted',
          displayStatus: 'submitted',
        },
      ];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.getAssignmentSubmissions(assignmentId);

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith(
        `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/submissions`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('recordSubmissionException', () => {
    it('issues POST /v1/academics/assignments/{assignmentId}/submissions/exception with payload', async () => {
      const mockRequest: RecordExceptionRequest = {
        studentId,
        exceptionType: 'excused',
        reason: 'Student excused due to regional track and field championship.',
      };
      const mockResponse = {
        assignmentId,
        studentId,
        exceptionType: 'excused',
        status: 'excused',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await courseworkApi.recordSubmissionException(assignmentId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/submissions/exception`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
