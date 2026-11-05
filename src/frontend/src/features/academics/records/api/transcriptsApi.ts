import { apiClient } from '@/shared/services/http';
import type {
  GenerateInterimTranscriptRequest,
  IssueTranscriptResponse,
  Transcript,
} from '../types';

/**
 * REST API client module for cumulative transcript generation, certification, and issuance (UC-ACD-13, P5-S13).
 */

/**
 * Compiles a draft cumulative interim transcript for a student.
 * POST /api/v1/academics/transcripts/generate
 */
export async function generate(
  request: GenerateInterimTranscriptRequest,
): Promise<Transcript> {
  const response = (await apiClient.post(
    '/v1/academics/transcripts/generate',
    request,
  )) as unknown as Transcript;
  return response;
}

/**
 * Formally certifies and issues an official or unofficial transcript, freezing its structured JSON snapshot.
 * POST /api/v1/academics/transcripts/{id}/issue
 */
export async function issue(id: string): Promise<IssueTranscriptResponse> {
  const response = (await apiClient.post(
    `/v1/academics/transcripts/${encodeURIComponent(id)}/issue`,
    {},
  )) as unknown as IssueTranscriptResponse;
  return response;
}

/**
 * Retrieves a student's active cumulative transcript and completed periods history.
 * GET /api/v1/academics/students/{studentId}/transcript
 */
export async function getByStudent(
  studentId: string,
  transcriptType?: string,
): Promise<Transcript> {
  const params: Record<string, string> = {};
  if (transcriptType) params.transcriptType = transcriptType;

  const response = (await apiClient.get(
    `/v1/academics/students/${encodeURIComponent(studentId)}/transcript`,
    { params },
  )) as unknown as Transcript;
  return response;
}

/**
 * Retrieves transcript details by record ID.
 * GET /api/v1/academics/transcripts/{id}
 */
export async function getById(id: string): Promise<Transcript> {
  const response = (await apiClient.get(
    `/v1/academics/transcripts/${encodeURIComponent(id)}`,
  )) as unknown as Transcript;
  return response;
}

export const transcriptsApi = {
  generate,
  issue,
  getByStudent,
  getById,
};
