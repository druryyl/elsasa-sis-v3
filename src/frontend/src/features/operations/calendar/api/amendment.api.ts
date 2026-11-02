import { apiClient } from '@/shared/services/http';

export interface CalendarAmendmentDto {
  id: string;
  calendarId: string;
  amendmentNumber: string;
  fromVersion: string;
  toVersion: string;
  amendmentType: string;
  affectedEntryId?: string | null | undefined;
  changeSummary: string;
  previousValue?: string | null | undefined;
  newValue?: string | null | undefined;
  reason: string;
  effectiveDate: string;
  authorizedBy: string;
  authorizedDate: string;
  downstreamImpactReviewed: boolean;
  createdAt: string;
}

export interface RecordCorrectionDto {
  id: string;
  academicYearId: string;
  targetTableName: string;
  targetRecordId: string;
  fieldName: string;
  oldValue?: string | null | undefined;
  newValue?: string | null | undefined;
  justificationReason: string;
  authorizedBy: string;
  authorizedAt: string;
  createdAt?: string | undefined;
}

export interface SubmitCorrectionPayload {
  academicYearId?: string | undefined;
  targetTableName: string;
  targetRecordId: string;
  fieldName: string;
  newValue: string;
  justificationReason: string;
  authorizedBy?: string | null | undefined;
}

/**
 * Retrieves audit history of amendments for a published school calendar.
 * GET /api/operations/calendars/{calendarId}/amendments
 */
export async function getAmendments(calendarId: string): Promise<CalendarAmendmentDto[]> {
  const response = (await apiClient.get(
    `/operations/calendars/${encodeURIComponent(calendarId)}/amendments`,
  )) as unknown as CalendarAmendmentDto[];

  return Array.isArray(response) ? response : [];
}

/**
 * Retrieves audit history of record corrections for an academic year context.
 * GET /api/operations/academic-years/{academicYearId}/corrections
 */
export async function getCorrections(
  academicYearId: string,
): Promise<RecordCorrectionDto[]> {
  const response = (await apiClient.get(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/corrections`,
  )) as unknown as RecordCorrectionDto[];

  return Array.isArray(response) ? response : [];
}

/**
 * Submits an authorized record correction to an approved operational record (UC-OPS-13).
 * POST /api/operations/records/correct
 */
export async function submitCorrection(
  payload: SubmitCorrectionPayload,
): Promise<RecordCorrectionDto> {
  const response = (await apiClient.post(
    '/operations/records/correct',
    payload,
  )) as unknown as RecordCorrectionDto;

  return response;
}
