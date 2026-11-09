import { apiClient } from '@/shared/services/http';

export interface DemoResetSummary {
  purgedSchemas: number;
  seededDomains: number;
  elapsedMilliseconds: number;
  anchoredDate: string;
  baseAcademicYearCode: string;
  totalStudentsSeeded: number;
  totalInvoicesSeeded: number;
  totalAttendanceSeeded: number;
  totalResultsSeeded: number;
  totalReportCardsSeeded: number;
}

/**
 * Calls POST /api/operations/demo-data/reset via the configured Axios instance.
 *
 * Error handling for 403 (production) and 400 (disabled) responses is managed
 * by the HTTP response interceptor, which throws `ApiError` instances.
 * The TanStack Query mutation's `onError` handler surfaces these to the user.
 */
export async function resetDemoData(): Promise<DemoResetSummary> {
  const response = (await apiClient.post('/operations/demo-data/reset')) as DemoResetSummary;
  return response;
}