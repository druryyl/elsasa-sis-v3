import { z } from 'zod';

export const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;
export const TIME_REGEX = /^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/;

export const ACTIVITY_TYPES = [
  'lesson',
  'tutorial',
  'laboratory',
  'independent_work',
  'project',
  'discussion',
  'practice',
  'examination',
  'remediation',
  'homeroom_session',
  'other',
] as const;

export const ACTIVITY_STATUSES = ['planned', 'conducted', 'cancelled'] as const;

export const ATTENDANCE_STATUSES = [
  'present',
  'absent',
  'late',
  'excused_absence',
  'partial',
] as const;

function isValidDate(val: string): boolean {
  if (!ISO_DATE_REGEX.test(val)) return false;
  const [y, m, d] = val.split('-').map(Number);
  const date = new Date(Date.UTC(y, m - 1, d));
  return (
    date.getUTCFullYear() === y &&
    date.getUTCMonth() === m - 1 &&
    date.getUTCDate() === d
  );
}

// ============================================================================
// Activity Planning Schema
// ============================================================================

export const planActivitySchema = z
  .object({
    learningUnitId: z
      .string({ required_error: 'Learning unit is required.' })
      .trim()
      .min(1, 'Learning unit is required.'),
    activityType: z.enum(ACTIVITY_TYPES, {
      required_error: 'Activity type is required.',
    }),
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(1, 'Title is required.')
      .max(200, 'Title cannot exceed 200 characters.'),
    description: z
      .string()
      .trim()
      .max(2000, 'Description cannot exceed 2000 characters.')
      .optional()
      .nullable(),
    sectionLabel: z
      .string()
      .trim()
      .max(50, 'Section label cannot exceed 50 characters.')
      .optional()
      .nullable(),
    activityDate: z
      .string({ required_error: 'Activity date is required.' })
      .trim()
      .min(1, 'Activity date is required.')
      .regex(ISO_DATE_REGEX, 'Activity date must be in YYYY-MM-DD format.')
      .refine(isValidDate, {
        message: 'Activity date must be a valid calendar date.',
      }),
    startTime: z
      .string()
      .trim()
      .regex(TIME_REGEX, 'Start time must be in HH:mm format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    endTime: z
      .string()
      .trim()
      .regex(TIME_REGEX, 'End time must be in HH:mm format.')
      .optional()
      .nullable()
      .or(z.literal('')),
    teacherAssignmentId: z.string().trim().optional().nullable().or(z.literal('')),
    teacherName: z
      .string()
      .trim()
      .max(200, 'Teacher name cannot exceed 200 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    isAttendanceRequired: z.boolean().default(true),
  })
  .refine(
    (data) => {
      if (data.startTime && data.endTime) {
        return data.startTime < data.endTime;
      }
      return true;
    },
    {
      message: 'End time must be after start time.',
      path: ['endTime'],
    },
  );

export type PlanActivityFormValues = z.infer<typeof planActivitySchema>;

// ============================================================================
// Activity Conducting & Delivery Notes Schema
// ============================================================================

export const conductActivitySchema = z.object({
  deliveryNotes: z
    .string()
    .trim()
    .max(2000, 'Delivery notes cannot exceed 2000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  actualTeacherName: z
    .string()
    .trim()
    .max(200, 'Teacher name cannot exceed 200 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type ConductActivityFormValues = z.infer<typeof conductActivitySchema>;

// ============================================================================
// Cancel Activity Schema
// ============================================================================

export const cancelActivitySchema = z.object({
  cancellationReason: z
    .string({ required_error: 'Cancellation reason is required.' })
    .trim()
    .min(3, 'Cancellation reason must be at least 3 characters.')
    .max(1000, 'Cancellation reason cannot exceed 1000 characters.'),
});

export type CancelActivityFormValues = z.infer<typeof cancelActivitySchema>;

// ============================================================================
// Student Attendance Entry Schema
// ============================================================================

export const studentAttendanceEntrySchema = z.object({
  studentId: z
    .string({ required_error: 'Student ID is required.' })
    .trim()
    .min(1, 'Student ID is required.'),
  status: z.enum(ATTENDANCE_STATUSES, {
    required_error: 'Attendance status is required.',
  }),
  evidence: z
    .string()
    .trim()
    .max(1000, 'Evidence cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
  notes: z
    .string()
    .trim()
    .max(1000, 'Notes cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type StudentAttendanceEntryValues = z.infer<typeof studentAttendanceEntrySchema>;

// ============================================================================
// Roll-Call Recording Schema
// ============================================================================

export const recordAttendanceSchema = z.object({
  entries: z
    .array(studentAttendanceEntrySchema, {
      required_error: 'At least one student attendance entry is required.',
    })
    .min(1, 'At least one student attendance entry is required.'),
});

export type RecordAttendanceFormValues = z.infer<typeof recordAttendanceSchema>;

// ============================================================================
// Auditable Correction Schema (UC-ACD-15)
// ============================================================================

export const correctAttendanceSchema = z.object({
  newStatus: z.enum(ATTENDANCE_STATUSES, {
    required_error: 'New attendance status is required.',
  }),
  reason: z
    .string({ required_error: 'Mandatory justification reason is required.' })
    .trim()
    .min(5, 'Reason must be at least 5 characters.')
    .max(1000, 'Reason cannot exceed 1000 characters.'),
  evidence: z
    .string()
    .trim()
    .max(1000, 'Evidence cannot exceed 1000 characters.')
    .optional()
    .nullable()
    .or(z.literal('')),
});

export type CorrectAttendanceFormValues = z.infer<typeof correctAttendanceSchema>;
