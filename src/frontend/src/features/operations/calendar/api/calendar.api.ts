import { apiClient } from '@/shared/services/http';

export type CalendarPublicationStatus = 'draft' | 'published' | 'withdrawn';

export interface SchoolCalendarDto {
  id: string;
  academicYearId: string;
  name: string;
  scopeType: string;
  scopeReference: string;
  version: string;
  status: string;
  publicationStatus: CalendarPublicationStatus | string;
  publishedAt?: string | null | undefined;
  publishedBy?: string | null | undefined;
  approvedBy?: string | null | undefined;
  approvedDate?: string | null | undefined;
  notes?: string | null | undefined;
  createdAt?: string | undefined;
  createdBy?: string | null | undefined;
  updatedAt?: string | null | undefined;
  updatedBy?: string | null | undefined;
}

export interface CalendarEntryDto {
  id: string;
  calendarId: string;
  entryDate: string;
  endDate?: string | null | undefined;
  designation: string;
  name: string;
  description?: string | null | undefined;
  isInstructional: boolean;
  closureId?: string | null | undefined;
  appliesTo: string;
  createdAt?: string | undefined;
  createdBy?: string | null | undefined;
}

export interface CalendarMatrixDayDto {
  date: string;
  dayOfWeek: number | string;
  isInstructional: boolean;
  designation: string;
  entryName?: string | null | undefined;
  entryDescription?: string | null | undefined;
  entryId?: string | null | undefined;
}

export interface CreateCalendarPayload {
  name: string;
  scopeType?: string | undefined;
  scopeReference?: string | undefined;
  notes?: string | null | undefined;
}

export interface CreateCalendarEntryPayload {
  entryDate: string;
  endDate?: string | null | undefined;
  designation: string;
  name: string;
  description?: string | null | undefined;
  isInstructional?: boolean | undefined;
  appliesTo?: string | undefined;
  amendmentReason?: string | null | undefined;
}

export interface MatrixProjectionParams {
  startDate: string;
  endDate: string;
  campus?: string | undefined;
}

/**
 * Retrieves school calendars for an academic year.
 * GET /api/operations/academic-years/{academicYearId}/calendars
 */
export async function getCalendars(academicYearId: string): Promise<SchoolCalendarDto[]> {
  const response = (await apiClient.get(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/calendars`,
  )) as unknown as SchoolCalendarDto[];

  return Array.isArray(response) ? response : [];
}

/**
 * Retrieves the currently active published school calendar for an academic year.
 * GET /api/operations/academic-years/{academicYearId}/calendars/active
 */
export async function getActiveCalendar(
  academicYearId: string,
): Promise<SchoolCalendarDto | null> {
  const response = (await apiClient.get(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/calendars/active`,
  )) as unknown as SchoolCalendarDto | null;

  return response;
}

/**
 * Creates a new school calendar container in draft status.
 * POST /api/operations/academic-years/{academicYearId}/calendars
 */
export async function createCalendar(
  academicYearId: string,
  payload: CreateCalendarPayload,
): Promise<SchoolCalendarDto> {
  const response = (await apiClient.post(
    `/operations/academic-years/${encodeURIComponent(academicYearId)}/calendars`,
    payload,
  )) as unknown as SchoolCalendarDto;

  return response;
}

/**
 * Publishes a school calendar, establishing immutable date baselines.
 * POST /api/operations/calendars/{id}/publish
 */
export async function publishCalendar(id: string): Promise<SchoolCalendarDto> {
  const response = (await apiClient.post(
    `/operations/calendars/${encodeURIComponent(id)}/publish`,
    {},
  )) as unknown as SchoolCalendarDto;

  return response;
}

/**
 * Withdraws a published school calendar.
 * POST /api/operations/calendars/{id}/withdraw
 */
export async function withdrawCalendar(id: string): Promise<SchoolCalendarDto> {
  const response = (await apiClient.post(
    `/operations/calendars/${encodeURIComponent(id)}/withdraw`,
    {},
  )) as unknown as SchoolCalendarDto;

  return response;
}

/**
 * Computes dynamic instructional projection overlaid with calendar exceptions.
 * GET /api/operations/calendars/{calendarId}/matrix?startDate=...&endDate=...
 */
export async function getMatrixProjection(
  calendarId: string,
  params: MatrixProjectionParams,
): Promise<CalendarMatrixDayDto[]> {
  const response = (await apiClient.get(
    `/operations/calendars/${encodeURIComponent(calendarId)}/matrix`,
    { params },
  )) as unknown as CalendarMatrixDayDto[] | { days?: CalendarMatrixDayDto[] };

  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && Array.isArray((response as { days?: CalendarMatrixDayDto[] }).days)) {
    return (response as { days: CalendarMatrixDayDto[] }).days;
  }

  return [];
}

/**
 * Creates an exception entry in a school calendar (e.g. holiday, closure, exam day).
 * POST /api/operations/calendars/{calendarId}/entries
 */
export async function createCalendarEntry(
  calendarId: string,
  payload: CreateCalendarEntryPayload,
): Promise<CalendarEntryDto> {
  const response = (await apiClient.post(
    `/operations/calendars/${encodeURIComponent(calendarId)}/entries`,
    payload,
  )) as unknown as CalendarEntryDto;

  return response;
}

/**
 * Deletes a calendar exception entry.
 * DELETE /api/operations/calendars/{calendarId}/entries/{entryId}
 */
export async function deleteCalendarEntry(
  calendarId: string,
  entryId: string,
): Promise<void> {
  await apiClient.delete(
    `/operations/calendars/${encodeURIComponent(calendarId)}/entries/${encodeURIComponent(entryId)}`,
  );
}
