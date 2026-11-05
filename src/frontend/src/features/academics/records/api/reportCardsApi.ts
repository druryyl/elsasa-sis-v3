import { apiClient } from '@/shared/services/http';
import type {
  ApproveReportCardRequest,
  BatchCompilationSummary,
  BatchCompileReportCardsRequest,
  CompileReportCardRequest,
  ReportCard,
} from '../types';

/**
 * REST API client module for report card compilation, approval, and portal publication (UC-ACD-13, P5-S11, P5-S12).
 */

/**
 * Compiles a draft report card for an individual student.
 * POST /api/v1/academics/report-cards/compile
 */
export async function compile(
  request: CompileReportCardRequest,
): Promise<ReportCard> {
  const response = (await apiClient.post(
    '/v1/academics/report-cards/compile',
    request,
  )) as unknown as ReportCard;
  return response;
}

/**
 * Executes high-performance set-based batch compilation of report cards for a cohort or section.
 * POST /api/v1/academics/report-cards/batch-compile
 */
export async function batchCompile(
  request: BatchCompileReportCardsRequest,
): Promise<BatchCompilationSummary> {
  const response = (await apiClient.post(
    '/v1/academics/report-cards/batch-compile',
    request,
  )) as unknown as BatchCompilationSummary;
  return response;
}

/**
 * Formally certifies a draft report card, freezing the structured JSON snapshot.
 * POST /api/v1/academics/report-cards/{id}/approve
 */
export async function approve(
  id: string,
  request?: ApproveReportCardRequest,
): Promise<ReportCard> {
  const response = (await apiClient.post(
    `/v1/academics/report-cards/${encodeURIComponent(id)}/approve`,
    request ?? {},
  )) as unknown as ReportCard;
  return response;
}

/**
 * Releases an approved report card to the student and family self-service portals.
 * POST /api/v1/academics/report-cards/{id}/publish
 */
export async function publish(id: string): Promise<ReportCard> {
  const response = (await apiClient.post(
    `/v1/academics/report-cards/${encodeURIComponent(id)}/publish`,
    {},
  )) as unknown as ReportCard;
  return response;
}

/**
 * Retrieves full report card details including subject entries and snapshot contract.
 * GET /api/v1/academics/report-cards/{id}
 */
export async function getById(id: string): Promise<ReportCard> {
  const response = (await apiClient.get(
    `/v1/academics/report-cards/${encodeURIComponent(id)}`,
  )) as unknown as ReportCard;
  return response;
}

/**
 * Retrieves certified published report cards for student/parent self-service portals.
 * GET /api/v1/portal/learning/report-cards
 */
export async function getStudentReportCards(params?: {
  studentId?: string;
  academicPeriodId?: string;
}): Promise<ReportCard[]> {
  const queryParams: Record<string, string> = {};
  if (params?.studentId) queryParams.studentId = params.studentId;
  if (params?.academicPeriodId) queryParams.academicPeriodId = params.academicPeriodId;

  const response = (await apiClient.get(
    '/v1/portal/learning/report-cards',
    { params: queryParams },
  )) as unknown as ReportCard[];
  return response;
}

export const reportCardsApi = {
  compile,
  batchCompile,
  approve,
  publish,
  getById,
  getStudentReportCards,
};
