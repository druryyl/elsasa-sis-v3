// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient } from '@/shared/services/http';
import * as learningApi from '../api/learning.api';
import type { SubmitCourseworkRequest } from '../types';

describe('Student Learning API Client (P9-S24)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const assignmentId = '11111111-1111-1111-1111-111111111111';
  const studentId = '22222222-2222-2222-2222-222222222222';

  describe('getStudentCourseworkWorkspace', () => {
    it('issues GET /v1/portal/learning/coursework without statusFilter param when omitted', async () => {
      const mockResponse = [{ id: 'task-1', title: 'Calculus Assignment 1' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await learningApi.getStudentCourseworkWorkspace();

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/portal/learning/coursework', { params: {} });
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/portal/learning/coursework without statusFilter param when filter is "all"', async () => {
      const mockResponse = [{ id: 'task-1', title: 'Calculus Assignment 1' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await learningApi.getStudentCourseworkWorkspace('all');

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/portal/learning/coursework', { params: {} });
      expect(result).toEqual(mockResponse);
    });

    it('issues GET /v1/portal/learning/coursework with statusFilter param when filtered', async () => {
      const mockResponse = [{ id: 'task-1', title: 'Calculus Assignment 1', status: 'missing' }];
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockResponse as never);

      const result = await learningApi.getStudentCourseworkWorkspace('missing');

      expect(getSpy).toHaveBeenCalledTimes(1);
      expect(getSpy).toHaveBeenCalledWith('/v1/portal/learning/coursework', {
        params: { statusFilter: 'missing' },
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe('submitCoursework', () => {
    it('issues POST /v1/academics/assignments/{assignmentId}/submissions with payload', async () => {
      const mockRequest: SubmitCourseworkRequest = {
        studentId,
        submissionType: 'link',
        submissionReference: 'https://github.com/student/final-project',
        notes: 'Final build deployed.',
      };
      const mockResponse = {
        id: 'sub-1',
        assignmentId,
        studentId,
        status: 'submitted',
        submittedAt: '2026-09-10T14:00:00Z',
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await learningApi.submitCoursework(assignmentId, mockRequest);

      expect(postSpy).toHaveBeenCalledTimes(1);
      expect(postSpy).toHaveBeenCalledWith(
        `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/submissions`,
        mockRequest,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('uploadSubmissionFile', () => {
    it('issues POST /v1/academics/submissions/upload with FormData and multipart header', async () => {
      const dummyFile = new File(['file contents'], 'report.pdf', {
        type: 'application/pdf',
      });
      const mockResponse = {
        fileUrl: '/uploads/academics/report.pdf',
        fileName: 'report.pdf',
        fileSizeBytes: 13,
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await learningApi.uploadSubmissionFile(dummyFile, studentId);

      expect(postSpy).toHaveBeenCalledTimes(1);
      const [url, formData, config] = postSpy.mock.calls[0];
      expect(url).toBe('/v1/academics/submissions/upload');
      expect(formData).toBeInstanceOf(FormData);
      expect((formData as FormData).get('file')).toBe(dummyFile);
      expect((formData as FormData).get('studentId')).toBe(studentId);
      expect(config).toEqual({
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      expect(result).toEqual(mockResponse);
    });

    it('issues POST /v1/academics/submissions/upload without studentId when omitted', async () => {
      const dummyFile = new File(['file contents'], 'diagram.png', {
        type: 'image/png',
      });
      const mockResponse = {
        fileUrl: '/uploads/academics/diagram.png',
        fileName: 'diagram.png',
        fileSizeBytes: 13,
      };
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce(mockResponse as never);

      const result = await learningApi.uploadSubmissionFile(dummyFile);

      expect(postSpy).toHaveBeenCalledTimes(1);
      const [, formData] = postSpy.mock.calls[0];
      expect((formData as FormData).get('file')).toBe(dummyFile);
      expect((formData as FormData).get('studentId')).toBeNull();
      expect(result).toEqual(mockResponse);
    });
  });
});
