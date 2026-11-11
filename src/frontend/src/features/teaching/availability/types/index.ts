/**
 * TypeScript contract interfaces and DTOs for Teacher Availability & Unavailability Windows (P7-S23, SCR-TCH-04).
 * Strictly mirrors backend contracts Elsasa.Sis.Contracts.Teaching and Elsasa.Sis.Teaching.Features.Workload.
 */

export type UnavailabilityType =
  | 'medical_leave'
  | 'official_training'
  | 'personal_leave'
  | 'blackout';

export type UnavailabilityStatus = 'active' | 'cancelled' | 'expired';

export type AvailabilityStatus = 'active' | 'revoked' | 'superseded';

export type DeliveryMode = 'All' | 'in_person' | 'online' | 'hybrid';

/**
 * Weekly recurring time-block slot representing availability for a specific day and time period.
 */
export interface TimeSlotBlock {
  dayOfWeek: 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday' | 'sunday' | number;
  startTime: string; // HH:mm
  endTime: string; // HH:mm
  slotType?: 'morning' | 'afternoon' | 'evening';
  isAvailable: boolean;
}

/**
 * Structured weekly recurring schedule declared by faculty.
 */
export interface RecurringSchedule {
  slots: TimeSlotBlock[];
  notes?: string;
}

/**
 * Strongly-typed contract DTO representing a temporary leave, official training, illness, or blackout window.
 * Mirrors Elsasa.Sis.Contracts.Teaching.TeacherUnavailabilityWindowDto.
 */
export interface TeacherUnavailabilityWindowDto {
  id: string;
  teacherProfileId: string;
  startDate: string; // ISO-8601 date YYYY-MM-DD
  endDate: string; // ISO-8601 date YYYY-MM-DD
  startTime?: string | null; // HH:mm or HH:mm:ss
  endTime?: string | null; // HH:mm or HH:mm:ss
  unavailabilityType: UnavailabilityType | string;
  externalSourceReference?: string | null;
  reason: string;
  status: UnavailabilityStatus | string;
  notes?: string | null;
  createdAt: string; // ISO-8601 UTC
}

/**
 * Strongly-typed contract DTO representing a teacher's declared baseline weekly capacity,
 * preferred delivery modes, and recurring schedule slots for an academic period.
 * Mirrors Elsasa.Sis.Contracts.Teaching.TeacherAvailabilityDto.
 */
export interface TeacherAvailabilityDto {
  id: string;
  teacherProfileId: string;
  academicPeriodId: string;
  maxAvailableHours: number;
  campusScope: string;
  availableDeliveryModes: string;
  recurringAvailableSchedule?: string | null;
  effectiveStartDate: string; // ISO-8601 date YYYY-MM-DD
  effectiveEndDate: string; // ISO-8601 date YYYY-MM-DD
  status: AvailabilityStatus | string;
  notes?: string | null;
  createdAt: string; // ISO-8601 UTC
  activeUnavailabilityWindows?: TeacherUnavailabilityWindowDto[] | null;
}

/**
 * Request payload for declaring recurring weekly teacher availability.
 * Mirrors Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability.DeclareTeacherAvailabilityRequest.
 */
export interface DeclareTeacherAvailabilityRequest {
  academicPeriodId: string;
  maxAvailableHours: number;
  effectiveStartDate: string; // YYYY-MM-DD
  effectiveEndDate: string; // YYYY-MM-DD
  campusScope?: string | null;
  availableDeliveryModes?: string | null;
  recurringAvailableSchedule?: string | null;
  notes?: string | null;
}

/**
 * Response payload returned after successfully declaring teacher availability.
 * Mirrors Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability.DeclareTeacherAvailabilityResponse.
 */
export interface DeclareTeacherAvailabilityResponse {
  id: string;
  teacherProfileId: string;
  academicPeriodId: string;
  maxAvailableHours: number;
  status: string;
  supersededPreviousCount: number;
  createdAt: string;
}

/**
 * Request payload for creating a temporary leave or blackout window.
 * Mirrors Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow.CreateUnavailabilityWindowRequest.
 */
export interface CreateUnavailabilityWindowRequest {
  startDate: string; // YYYY-MM-DD
  endDate: string; // YYYY-MM-DD
  unavailabilityType: UnavailabilityType | string;
  reason: string;
  startTime?: string | null; // HH:mm
  endTime?: string | null; // HH:mm
  externalSourceReference?: string | null;
  emergencyCoverageRequired?: boolean;
  notes?: string | null;
}

/**
 * Response payload returned after creating an unavailability window.
 * Mirrors Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow.CreateUnavailabilityWindowResponse.
 */
export interface CreateUnavailabilityWindowResponse {
  id: string;
  teacherProfileId: string;
  startDate: string;
  endDate: string;
  unavailabilityType: string;
  status: string;
  affectedActiveAssignmentCount: number;
  createdAt: string;
}

/**
 * Response payload returned after cancelling an unavailability window.
 * Mirrors Elsasa.Sis.Teaching.Features.Workload.CancelUnavailabilityWindow.CancelUnavailabilityWindowResponse.
 */
export interface CancelUnavailabilityWindowResponse {
  id: string;
  teacherProfileId: string;
  status: string;
  cancelledAt: string;
}

/**
 * Query parameters for filtering teacher unavailability windows.
 */
export interface GetTeacherUnavailabilityWindowsParams {
  status?: string | null;
  fromDate?: string | null; // YYYY-MM-DD
  toDate?: string | null; // YYYY-MM-DD
}
