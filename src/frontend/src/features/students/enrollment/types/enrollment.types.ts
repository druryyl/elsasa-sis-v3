/**
 * Authoritative TypeScript interfaces and types for the Students Enrollment module.
 * Reflects backend vertical slice DTOs and database schema (ADR-004, ADR-005, P3-S04).
 */

/**
 * Guardian details payload supplied during applicant enrollment.
 */
export interface EnrollGuardianPayload {
  fullName: string;
  relationshipType: string;
  governmentIdType?: string | null;
  governmentIdNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  occupation?: string | null;
  notes?: string | null;
  guardianId?: string | null;
  isPrimaryContact?: boolean;
  isEmergencyContact?: boolean;
  isFinancialContact?: boolean;
  isAuthorizedPickup?: boolean;
}

/**
 * Payload to enroll an accepted applicant as an official student (POST /api/students/enrollments).
 */
export interface EnrollAcceptedApplicantPayload {
  applicantId: string;
  academicYearId: string;
  initialCampus: string;
  initialLevel: string;
  initialAcademicYear: string;
  initialProgram?: string | null;
  initialCohort?: string | null;
  paymentWaived?: boolean;
  paymentReference?: string | null;
  waiverReference?: string | null;
  checkFinanceModule?: boolean | null;
  guardians?: EnrollGuardianPayload[] | null;
  notes?: string | null;
  applicationId?: string | null;
}

/**
 * Response payload returned upon successfully enrolling an accepted applicant.
 */
export interface EnrollAcceptedApplicantResponse {
  studentId: string;
  studentNumber: string;
  enrollmentId: string;
}
