import { apiClient } from '@/shared/services/http';
import type {
  ActivityDetail,
  AttendanceRecord,
  AvailableLearningUnit,
  CancelActivityRequest,
  CancelActivityResponse,
  ConductActivityRequest,
  ConductActivityResponse,
  CorrectAttendanceRequest,
  CorrectAttendanceResponse,
  LearningActivity,
  PlanActivityRequest,
  PlanActivityResponse,
  RecordAttendanceRequest,
  RecordAttendanceResponse,
  StudentAttendanceSummary,
  TeacherActivitiesFilter,
  TeacherDailyActivity,
  UnitActivitiesFilter,
} from '../types';

/**
 * Retrieves active learning units across all structures for session planning selectors
 * (SCR-ACD-01, P3-S07).
 * GET /api/v1/academics/learning-units/available
 */
export async function getAvailableLearningUnits(
  search?: string,
): Promise<AvailableLearningUnit[]> {
  const params: Record<string, string> = {};
  if (search?.trim()) {
    params.search = search.trim();
  }

  const response = (await apiClient.get(
    '/v1/academics/learning-units/available',
    { params },
  )) as unknown as AvailableLearningUnit[];

  return Array.isArray(response) ? response : [];
}

/**
 * Plans a new instructional learning activity or homeroom session (P3-S07, TD-03).
 * POST /api/v1/academics/learning-activities
 */
export async function planLearningActivity(
  request: PlanActivityRequest,
): Promise<PlanActivityResponse> {
  const response = (await apiClient.post(
    '/v1/academics/learning-activities',
    request,
  )) as unknown as PlanActivityResponse;
  return response;
}

/**
 * Records session completion, delivery notes, and milestones (UC-ACD-02, P3-S07).
 * POST /api/v1/academics/learning-activities/{id}/conduct
 */
export async function conductLearningActivity(
  id: string,
  request: ConductActivityRequest,
): Promise<ConductActivityResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-activities/${encodeURIComponent(id)}/conduct`,
    request,
  )) as unknown as ConductActivityResponse;
  return response;
}

/**
 * Cancels a scheduled learning activity with cancellation reason (P3-S07).
 * POST /api/v1/academics/learning-activities/{id}/cancel
 */
export async function cancelLearningActivity(
  id: string,
  request: CancelActivityRequest,
): Promise<CancelActivityResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-activities/${encodeURIComponent(id)}/cancel`,
    request,
  )) as unknown as CancelActivityResponse;
  return response;
}

/**
 * Retrieves activities for a learning unit filtered by date range and section (P3-S07).
 * GET /api/v1/academics/learning-units/{learningUnitId}/activities
 */
export async function getActivitiesByUnit(
  learningUnitId: string,
  filter?: UnitActivitiesFilter,
): Promise<LearningActivity[]> {
  const params: Record<string, string> = {};
  if (filter?.fromDate) {
    params.fromDate = filter.fromDate;
  }
  if (filter?.toDate) {
    params.toDate = filter.toDate;
  }
  if (filter?.sectionLabel) {
    params.sectionLabel = filter.sectionLabel;
  }
  if (filter?.status) {
    params.status = filter.status;
  }

  const response = (await apiClient.get(
    `/v1/academics/learning-units/${encodeURIComponent(learningUnitId)}/activities`,
    { params },
  )) as unknown as LearningActivity[];
  return response;
}

/**
 * Retrieves detailed activity inspection with attendance counts and assignments (P3-S07).
 * GET /api/v1/academics/learning-activities/{id}
 */
export async function getActivityById(id: string): Promise<ActivityDetail> {
  const response = (await apiClient.get(
    `/v1/academics/learning-activities/${encodeURIComponent(id)}`,
  )) as unknown as ActivityDetail;
  return response;
}

/**
 * Retrieves activities scheduled/conducted for the authenticated teacher by date (SCR-ACD-01, P3-S07).
 * GET /api/v1/academics/teacher/activities
 */
export async function getTeacherDailyActivities(
  date?: string,
): Promise<TeacherDailyActivity[]> {
  const params: Record<string, string> = {};
  if (date) {
    params.date = date;
  }

  const response = (await apiClient.get('/v1/academics/teacher/activities', {
    params,
  })) as unknown as TeacherDailyActivity[];
  return response;
}

/**
 * Records live roll-call attendance for a learning activity session (P4-S08, UC-ACD-03).
 * POST /api/v1/academics/learning-activities/{id}/attendance
 */
export async function recordSessionAttendance(
  activityId: string,
  request: RecordAttendanceRequest,
): Promise<RecordAttendanceResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/attendance`,
    request,
  )) as unknown as RecordAttendanceResponse;
  return response;
}

/**
 * Submits an auditable historical attendance correction with mandatory reason (P4-S09, UC-ACD-15).
 * POST /api/v1/academics/attendance-records/{id}/correct
 */
export async function correctAttendance(
  attendanceRecordId: string,
  request: CorrectAttendanceRequest,
): Promise<CorrectAttendanceResponse> {
  const response = (await apiClient.post(
    `/v1/academics/attendance-records/${encodeURIComponent(attendanceRecordId)}/correct`,
    request,
  )) as unknown as CorrectAttendanceResponse;
  return response;
}

/**
 * Retrieves full session attendance roster with correction history (P4-S09, TD-04).
 * GET /api/v1/academics/learning-activities/{id}/attendance
 */
export async function getActivityAttendance(
  activityId: string,
): Promise<AttendanceRecord[]> {
  const response = (await apiClient.get(
    `/v1/academics/learning-activities/${encodeURIComponent(activityId)}/attendance`,
  )) as unknown as AttendanceRecord[];
  return response;
}

/**
 * Retrieves aggregate attendance summary statistics for a student (P4-S09, TD-04).
 * GET /api/v1/academics/students/{studentId}/attendance-summary
 */
export async function getStudentAttendanceSummary(
  studentId: string,
  academicPeriodId?: string,
): Promise<StudentAttendanceSummary> {
  const params: Record<string, string> = {};
  if (academicPeriodId) {
    params.academicPeriodId = academicPeriodId;
  }

  const response = (await apiClient.get(
    `/v1/academics/students/${encodeURIComponent(studentId)}/attendance-summary`,
    { params },
  )) as unknown as StudentAttendanceSummary;
  return response;
}
