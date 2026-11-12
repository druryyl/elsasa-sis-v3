import { apiClient } from '@/shared/services/http';
import type {
  CourseworkStatusFilter,
  FileUploadResponse,
  StudentCourseworkTask,
  SubmitCourseworkRequest,
  SubmitCourseworkResponse,
} from '../types';

/**
 * Retrieves the student self-service coursework workspace with dynamic missing projection (SCR-SELF-03, TD-06, P5-S12).
 * GET /api/v1/portal/learning/coursework
 */
export async function getStudentCourseworkWorkspace(
  statusFilter?: CourseworkStatusFilter | string,
  studentId?: string,
): Promise<StudentCourseworkTask[]> {
  const params: Record<string, string> = {};
  if (statusFilter && statusFilter !== 'all') {
    params.statusFilter = statusFilter;
  }
  if (studentId) {
    params.studentId = studentId;
  }

  const response = (await apiClient.get('/v1/portal/learning/coursework', {
    params,
  })) as unknown as StudentCourseworkTask[];
  return response;
}

/**
 * Submits coursework evidence for an assignment (TD-07, UC-ACD-01, P5-S11).
 * POST /api/v1/academics/assignments/{assignmentId}/submissions
 */
export async function submitCoursework(
  assignmentId: string,
  request: SubmitCourseworkRequest,
): Promise<SubmitCourseworkResponse> {
  const response = (await apiClient.post(
    `/v1/academics/assignments/${encodeURIComponent(assignmentId)}/submissions`,
    request,
  )) as unknown as SubmitCourseworkResponse;
  return response;
}

/**
 * Uploads a coursework submission file using multipart/form-data (TD-07, P5-S11).
 * POST /api/v1/academics/submissions/upload
 */
export async function uploadSubmissionFile(
  file: File,
  studentId?: string,
): Promise<FileUploadResponse> {
  const formData = new FormData();
  formData.append('file', file);
  if (studentId) {
    formData.append('studentId', studentId);
  }

  const response = (await apiClient.post(
    '/v1/academics/submissions/upload',
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    },
  )) as unknown as FileUploadResponse;
  return response;
}
