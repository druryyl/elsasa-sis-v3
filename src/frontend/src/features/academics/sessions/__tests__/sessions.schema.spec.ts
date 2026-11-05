// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import {
  planActivitySchema,
  conductActivitySchema,
  cancelActivitySchema,
  studentAttendanceEntrySchema,
  recordAttendanceSchema,
  correctAttendanceSchema,
  ACTIVITY_TYPES,
  ACTIVITY_STATUSES,
  ATTENDANCE_STATUSES,
} from '../schemas/sessions.schema';

describe('Sessions & Attendance Zod Schemas (P9-S24)', () => {
  // ==========================================================================
  // 1. planActivitySchema
  // ==========================================================================
  describe('planActivitySchema', () => {
    const validPlanPayload = {
      learningUnitId: '11111111-1111-1111-1111-111111111111',
      activityType: 'lesson' as const,
      title: 'Newtonian Mechanics Introduction',
      description: 'First lecture on dynamics and kinematics.',
      sectionLabel: '10-A',
      activityDate: '2026-08-15',
      startTime: '08:00',
      endTime: '09:30',
      teacherAssignmentId: '22222222-2222-2222-2222-222222222222',
      teacherName: 'Dr. Maria Curie',
      isAttendanceRequired: true,
    };

    it('passes for a fully populated valid planned activity', () => {
      const result = planActivitySchema.safeParse(validPlanPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.title).toBe('Newtonian Mechanics Introduction');
        expect(result.data.activityType).toBe('lesson');
      }
    });

    it('passes for minimal payload omitting optional fields', () => {
      const minimalPayload = {
        learningUnitId: '11111111-1111-1111-1111-111111111111',
        activityType: 'homeroom_session' as const,
        title: 'Morning Roll-Call & Announcements',
        activityDate: '2026-08-15',
      };
      const result = planActivitySchema.safeParse(minimalPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.isAttendanceRequired).toBe(true);
      }
    });

    it('accepts all valid ACTIVITY_TYPES literals including homeroom_session', () => {
      for (const type of ACTIVITY_TYPES) {
        const result = planActivitySchema.safeParse({
          ...validPlanPayload,
          activityType: type,
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects invalid activityType', () => {
      const result = planActivitySchema.safeParse({
        ...validPlanPayload,
        activityType: 'unsupported_session_type',
      });
      expect(result.success).toBe(false);
    });

    it('rejects missing or empty title and titles exceeding 200 characters', () => {
      const emptyTitle = planActivitySchema.safeParse({
        ...validPlanPayload,
        title: '   ',
      });
      expect(emptyTitle.success).toBe(false);

      const tooLongTitle = planActivitySchema.safeParse({
        ...validPlanPayload,
        title: 'T'.repeat(201),
      });
      expect(tooLongTitle.success).toBe(false);
    });

    it('rejects invalid activityDate format and impossible calendar date', () => {
      const nonIso = planActivitySchema.safeParse({
        ...validPlanPayload,
        activityDate: '15-08-2026',
      });
      expect(nonIso.success).toBe(false);

      const invalidCalendar = planActivitySchema.safeParse({
        ...validPlanPayload,
        activityDate: '2026-02-30',
      });
      expect(invalidCalendar.success).toBe(false);
    });

    it('validates start and end time format (HH:mm)', () => {
      const badStartTime = planActivitySchema.safeParse({
        ...validPlanPayload,
        startTime: '25:99',
      });
      expect(badStartTime.success).toBe(false);
    });

    it('passes when startTime is before endTime', () => {
      const result = planActivitySchema.safeParse({
        ...validPlanPayload,
        startTime: '08:00',
        endTime: '09:30',
      });
      expect(result.success).toBe(true);
    });

    it('rejects when endTime is equal to or before startTime', () => {
      const equalTime = planActivitySchema.safeParse({
        ...validPlanPayload,
        startTime: '09:00',
        endTime: '09:00',
      });
      expect(equalTime.success).toBe(false);
      if (!equalTime.success) {
        expect(equalTime.error.issues[0].message).toContain('End time must be after start time');
      }

      const reverseTime = planActivitySchema.safeParse({
        ...validPlanPayload,
        startTime: '10:00',
        endTime: '09:00',
      });
      expect(reverseTime.success).toBe(false);
    });
  });

  // ==========================================================================
  // 2. conductActivitySchema & cancelActivitySchema
  // ==========================================================================
  describe('conductActivitySchema', () => {
    it('passes for valid delivery notes and teacher override', () => {
      const result = conductActivitySchema.safeParse({
        deliveryNotes: 'Covered chapters 1-3 kinematics exercises.',
        actualTeacherName: 'Substitute Teacher Budi',
      });
      expect(result.success).toBe(true);
    });

    it('passes when fields are empty or omitted', () => {
      const result = conductActivitySchema.safeParse({});
      expect(result.success).toBe(true);
    });

    it('rejects delivery notes exceeding 2000 characters', () => {
      const result = conductActivitySchema.safeParse({
        deliveryNotes: 'D'.repeat(2001),
      });
      expect(result.success).toBe(false);
    });
  });

  describe('cancelActivitySchema', () => {
    it('passes for cancellation reason >= 3 characters', () => {
      const result = cancelActivitySchema.safeParse({
        cancellationReason: 'Inclement weather campus closure',
      });
      expect(result.success).toBe(true);
    });

    it('rejects missing or shorter than 3 characters cancellation reason', () => {
      const empty = cancelActivitySchema.safeParse({
        cancellationReason: '',
      });
      expect(empty.success).toBe(false);

      const tooShort = cancelActivitySchema.safeParse({
        cancellationReason: 'No',
      });
      expect(tooShort.success).toBe(false);
    });
  });

  // ==========================================================================
  // 3. studentAttendanceEntrySchema & recordAttendanceSchema
  // ==========================================================================
  describe('studentAttendanceEntrySchema & recordAttendanceSchema', () => {
    const validEntry = {
      studentId: '11111111-1111-1111-1111-111111111111',
      status: 'present' as const,
      evidence: 'Camera ON',
      notes: 'Active in group discussion',
    };

    it('passes for all valid ATTENDANCE_STATUSES literals', () => {
      for (const st of ATTENDANCE_STATUSES) {
        const result = studentAttendanceEntrySchema.safeParse({
          ...validEntry,
          status: st,
        });
        expect(result.success).toBe(true);
      }
    });

    it('rejects invalid attendance status', () => {
      const result = studentAttendanceEntrySchema.safeParse({
        ...validEntry,
        status: 'truant',
      });
      expect(result.success).toBe(false);
    });

    it('recordAttendanceSchema passes with valid non-empty entries', () => {
      const result = recordAttendanceSchema.safeParse({
        entries: [validEntry],
      });
      expect(result.success).toBe(true);
    });

    it('recordAttendanceSchema rejects empty entries list', () => {
      const result = recordAttendanceSchema.safeParse({
        entries: [],
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues[0].message).toContain('At least one student attendance entry is required');
      }
    });
  });

  // ==========================================================================
  // 4. correctAttendanceSchema (UC-ACD-15)
  // ==========================================================================
  describe('correctAttendanceSchema', () => {
    const validCorrection = {
      newStatus: 'excused_absence' as const,
      reason: 'Doctor note provided retroactively after medical checkup',
      evidence: 'DOC-NOTE-9921.pdf',
    };

    it('passes for valid correction payload with reason >= 5 characters', () => {
      const result = correctAttendanceSchema.safeParse(validCorrection);
      expect(result.success).toBe(true);
    });

    it('rejects reason with less than 5 characters or empty string', () => {
      const tooShort = correctAttendanceSchema.safeParse({
        ...validCorrection,
        reason: 'Sick',
      });
      expect(tooShort.success).toBe(false);
      if (!tooShort.success) {
        expect(tooShort.error.issues[0].message).toContain('Reason must be at least 5 characters');
      }

      const emptyReason = correctAttendanceSchema.safeParse({
        ...validCorrection,
        reason: '   ',
      });
      expect(emptyReason.success).toBe(false);
    });

    it('rejects invalid newStatus', () => {
      const result = correctAttendanceSchema.safeParse({
        ...validCorrection,
        newStatus: 'unknown_status',
      });
      expect(result.success).toBe(false);
    });
  });
});
